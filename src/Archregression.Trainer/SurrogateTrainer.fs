namespace Archregression.Trainer

open System
open System.IO
open System.Text.Json
open Microsoft.ML
open Microsoft.ML.Data
open Microsoft.ML.Trainers.FastTree

module SurrogateTrainer =

    let featureNames =
        [| "RoomDepth"
           "RoomWidth"
           "Wwr"
           "LouverAngle"
           "OverhangDepth"
           "OrientationAzimuth"
           "GlazingUVal"
           "TreeDensity"
           "HasWater"
           "Slope" |]

    let friendlyNames =
        dict [
            "RoomDepth", "Room Depth (Span)"
            "RoomWidth", "Room Width (Span)"
            "Wwr", "Window-to-Wall Ratio (WWR)"
            "LouverAngle", "Louver Angle"
            "OverhangDepth", "Overhang Depth"
            "OrientationAzimuth", "Orientation Azimuth"
            "GlazingUVal", "Glazing U-Value"
            "TreeDensity", "Tree Canopy Density"
            "HasWater", "Water Body Proximity"
            "Slope", "Terrain Slope Elevation"
        ]

    let toRow (s: DatasetGenerator.EvaluatedSample) (targetSelector: DatasetGenerator.EvaluatedSample -> float) : SpatialDesignRow =
        let p = s.Parameters
        { RoomDepth = single p.RoomDepth
          RoomWidth = single p.RoomWidth
          Wwr = single p.Wwr
          LouverAngle = single p.LouverAngle
          OverhangDepth = single p.OverhangDepth
          OrientationAzimuth = single p.OrientationAzimuth
          GlazingUVal = single p.GlazingUVal
          TreeDensity = single p.TreeDensity
          HasWater = single p.HasWater
          Slope = single p.Slope
          Label = single (targetSelector s) }

    type TargetSpec =
        { Key: string
          Name: string
          Unit: string
          Selector: DatasetGenerator.EvaluatedSample -> float }

    let targetSpecs =
        [ { Key = "sda_percent"
            Name = "Spatial Daylight Autonomy (sDA 300/50%)"
            Unit = "%"
            Selector = fun s -> s.SdaPercent }
          { Key = "cooling_load"
            Name = "Annual Cooling Energy"
            Unit = "kWh/m²/yr"
            Selector = fun s -> s.CoolingLoad }
          { Key = "peak_stress"
            Name = "Peak Von Mises Stress"
            Unit = "MPa"
            Selector = fun s -> s.PeakStress }
          { Key = "embodied_carbon"
            Name = "Embodied Carbon Footprint"
            Unit = "kgCO₂e/m²"
            Selector = fun s -> s.EmbodiedCarbon }
          { Key = "biophilic_view"
            Name = "Biophilic & Green View Factor"
            Unit = "%"
            Selector = fun s -> s.BiophilicView }
          { Key = "microclimate_cooling"
            Name = "Microclimate Cooling Drop (ΔT)"
            Unit = "°C"
            Selector = fun s -> s.MicroclimateCooling } ]

    let trainAndEvaluateAll (mlContext: MLContext) (allSamples: DatasetGenerator.EvaluatedSample list) (outputDir: string) =
        let modelsDir = Path.Combine(outputDir, "models")
        let dataDir = Path.Combine(outputDir, "data")
        Directory.CreateDirectory(modelsDir) |> ignore
        Directory.CreateDirectory(dataDir) |> ignore

        let evaluations =
            targetSpecs
            |> List.map (fun spec ->
                printfn "\n⚡ [ML.NET] Training FastTree Regressor for target: %s (%s)..." spec.Name spec.Key

                let rows = allSamples |> List.map (fun s -> toRow s spec.Selector)
                let dataView = mlContext.Data.LoadFromEnumerable<SpatialDesignRow>(rows)
                let split = mlContext.Data.TrainTestSplit(dataView, testFraction = 0.2, seed = Nullable 42)

                let pConcat = mlContext.Transforms.Concatenate("Features", featureNames)
                let pNorm = mlContext.Transforms.NormalizeMinMax("Features")

                // Fit featurizer (Concatenate + Normalize)
                let featurizer = EstimatorChain().Append(pConcat).Append(pNorm).Fit(split.TrainSet)
                let featurizedTrain = featurizer.Transform(split.TrainSet)
                let featurizedTest = featurizer.Transform(split.TestSet)

                // Fit FastTree Regressor on featurized inputs
                let trainer =
                    mlContext.Regression.Trainers.FastTree(
                        labelColumnName = "Label",
                        featureColumnName = "Features",
                        numberOfLeaves = 20,
                        numberOfTrees = 100,
                        minimumExampleCountPerLeaf = 5
                    )
                let predictor = trainer.Fit(featurizedTrain)

                // Chain featurizer with predictor for end-to-end inference
                let fullModel = featurizer.Append(predictor)

                // Evaluate predictions on test set
                let predictions = fullModel.Transform(split.TestSet)
                let (metrics: RegressionMetrics) = mlContext.Regression.Evaluate(predictions, labelColumnName = "Label", scoreColumnName = "Score")

                // Save full pipeline model to .zip
                let modelPath = Path.Combine(modelsDir, sprintf "%s_fasttree.zip" spec.Key)
                mlContext.Model.Save(fullModel, split.TrainSet.Schema, modelPath)

                // Compute Permutation Feature Importance (PFI) on featurized test data
                let pfiResults = mlContext.Regression.PermutationFeatureImportance(predictor, featurizedTest, labelColumnName = "Label", permutationCount = 5)

                let rankedFeatures =
                    featureNames
                    |> Array.mapi (fun i name ->
                        let pfiMetric = pfiResults.[i]
                        let score = Math.Max(0.005, Math.Abs(pfiMetric.RSquared.Mean))
                        let friendly = if friendlyNames.ContainsKey(name) then friendlyNames.[name] else name
                        { Feature = friendly; Key = name; Score = Math.Round(score, 4) })
                    |> Array.sortByDescending (fun f -> f.Score)
                    |> Array.toList

                printfn "   ✓ R² Score: %.4f | RMSE: %.3f | MAE: %.3f" metrics.RSquared metrics.RootMeanSquaredError metrics.MeanAbsoluteError
                printfn "   ✓ Top Feature: %s (importance score: %.4f)" (List.head rankedFeatures).Feature (List.head rankedFeatures).Score

                { TargetKey = spec.Key
                  TargetName = spec.Name
                  Unit = spec.Unit
                  R2 = metrics.RSquared
                  Rmse = metrics.RootMeanSquaredError
                  Mae = metrics.MeanAbsoluteError
                  FeaturesRanked = rankedFeatures })

        // Export benchmark_dataset.json (validation points for parity plots)
        let benchmarkValidation =
            allSamples
            |> List.take 160
            |> List.map (fun s ->
                let p = s.Parameters
                dict [
                    "room_depth", box p.RoomDepth
                    "room_width", box p.RoomWidth
                    "wwr", box p.Wwr
                    "louver_angle", box p.LouverAngle
                    "overhang_depth", box p.OverhangDepth
                    "orientation_azimuth", box p.OrientationAzimuth
                    "glazing_u_val", box p.GlazingUVal
                    "tree_density", box p.TreeDensity
                    "has_water", box p.HasWater
                    "slope", box p.Slope
                    "actual_sda", box s.SdaPercent
                    "actual_cooling", box s.CoolingLoad
                    "actual_stress", box s.PeakStress
                    "actual_carbon", box s.EmbodiedCarbon
                    "actual_biophilia", box s.BiophilicView
                    "actual_microclimate", box s.MicroclimateCooling
                ])

        let jsonOptions = JsonSerializerOptions(WriteIndented = true)
        let benchmarkJson = JsonSerializer.Serialize(benchmarkValidation, jsonOptions)
        File.WriteAllText(Path.Combine(dataDir, "benchmark_dataset.json"), benchmarkJson)

        // Export surrogate_weights.json (model metadata and SHAP rankings for client UI)
        let shapDict =
            evaluations
            |> List.map (fun ev -> ev.TargetKey, ev.FeaturesRanked)
            |> dict

        let weightsJson = JsonSerializer.Serialize(dict [ "shap_rankings", box shapDict; "evaluations", box evaluations ], jsonOptions)
        File.WriteAllText(Path.Combine(modelsDir, "surrogate_weights.json"), weightsJson)

        evaluations
