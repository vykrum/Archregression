namespace Archregression

open System

/// Pure Functional Multi-Objective Genetic Optimizer in F#
module Generative =

    let private rng = Random()

    /// Generates and evaluates candidate designs to find the Pareto-optimal building geometry
    let optimize (current: ArchitecturalParams) (site: SiteLocation) (populationSize: int) : ArchitecturalParams =
        let topo = site.Topography
        let urban = site.Urban
        let isSteep = topo.TerrainSlope > 14.0
        let maxAllowedStories = int (Math.Floor(urban.Zoning.MaxBuildingHeightM / 3.6))
        let maxStories = Math.Min(5, Math.Max(1, maxAllowedStories))

        let sampleCandidate () =
            // On steep terrain, prefer wider footprint along contours (width > depth)
            let depth =
                if isSteep then 7.0 + rng.NextDouble() * 7.0
                else 8.0 + rng.NextDouble() * 12.0
            let width =
                if isSteep then 12.0 + rng.NextDouble() * 10.0
                else 8.0 + rng.NextDouble() * 12.0

            let stories = rng.Next(1, maxStories + 1)
            let wwr = 0.25 + rng.NextDouble() * 0.45
            let louver = 15.0 + rng.NextDouble() * 50.0
            let overhang = 0.5 + rng.NextDouble() * 1.5
            let azimuth = -45.0 + rng.NextDouble() * 90.0
            let uval = 1.0 + rng.NextDouble() * 1.4
            let padOffset = -0.75 + rng.NextDouble() * 1.5

            { RoomDepth = Math.Round(depth, 1)
              RoomWidth = Math.Round(width, 1)
              StoryCount = stories
              Wwr = Math.Round(wwr, 2)
              LouverAngle = Math.Round(louver, 0)
              OverhangDepth = Math.Round(overhang, 2)
              OrientationAzimuth = Math.Round(azimuth, 0)
              GlazingUVal = Math.Round(uval, 1)
              PadElevationOffset = Math.Round(padOffset, 2) }

        let fitness (cand: ArchitecturalParams) =
            let pred = Surrogate.evaluate cand site
            // Objectives:
            // + Reward Daylight Autonomy (sDA > 55%)
            // + Reward Biophilic Views
            // + Reward FAR Utilization (healthy density)
            // - Penalize Zoning Non-compliance (Hard constraint)
            // - Penalize Acoustic Strain (Excessive required R'w)
            // - Penalize Annual Cooling Load (kWh/m²)
            // - Penalize Structural Stress (MPa)
            // - Penalize Carbon Intensity
            // - Penalize Net Earthwork Imbalance (|Cut - Fill|)
            let sdaBonus = Math.Max(0.0, pred.SdaPercent - 55.0) * 1.8
            let biophilicBonus = pred.BiophilicViewPct * 0.75
            let farBonus = if pred.FarUtilizationPct >= 50.0 && pred.FarUtilizationPct <= 100.0 then 18.0 else 0.0
            let zoningPenalty = if pred.ZoningCompliant then 0.0 else 180.0
            let acousticPenalty = Math.Max(0.0, pred.RequiredAcousticRwDb - 42.0) * 2.2
            let coolingPenalty = pred.CoolingLoad * 1.25
            let stressPenalty = Math.Max(0.0, pred.PeakStress - 45.0) * 2.5
            let carbonPenalty = (pred.EmbodiedCarbon - 200.0) * 0.15
            let earthworkPenalty = Math.Abs(pred.NetEarthworkM3) * 0.65

            let score =
                (pred.SdaPercent * 1.5)
                + sdaBonus
                + biophilicBonus
                + farBonus
                - zoningPenalty
                - acousticPenalty
                - coolingPenalty
                - stressPenalty
                - carbonPenalty
                - earthworkPenalty
            score

        let population = Array.init populationSize (fun _ -> sampleCandidate ())
        let scored = population |> Array.map (fun c -> c, fitness c)
        let bestCandidate, _ = scored |> Array.maxBy snd
        bestCandidate
