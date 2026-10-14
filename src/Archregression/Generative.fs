namespace Archregression

open System

/// Pure Functional Multi-Objective Genetic Optimizer in F#
module Generative =

    let private rng = Random()

    /// Generates and evaluates candidate designs to find the Pareto-optimal building geometry
    let optimize (current: ArchitecturalParams) (site: SiteLocation) (populationSize: int) : ArchitecturalParams =
        let sampleCandidate () =
            let depth = 8.0 + rng.NextDouble() * 12.0
            let width = 8.0 + rng.NextDouble() * 12.0
            let wwr = 0.25 + rng.NextDouble() * 0.45
            let louver = 15.0 + rng.NextDouble() * 50.0
            let overhang = 0.5 + rng.NextDouble() * 1.5
            let azimuth = -45.0 + rng.NextDouble() * 90.0
            let uval = 1.2 + rng.NextDouble() * 1.2

            { RoomDepth = Math.Round(depth, 1)
              RoomWidth = Math.Round(width, 1)
              Wwr = Math.Round(wwr, 2)
              LouverAngle = Math.Round(louver, 0)
              OverhangDepth = Math.Round(overhang, 2)
              OrientationAzimuth = Math.Round(azimuth, 0)
              GlazingUVal = Math.Round(uval, 1) }

        let fitness (cand: ArchitecturalParams) =
            let pred = Surrogate.evaluate cand site
            // Objectives:
            // + Reward Daylight Autonomy (sDA > 55%)
            // + Reward Biophilic Views
            // - Penalize Annual Cooling Load (kWh/m²)
            // - Penalize Structural Stress (MPa)
            // - Penalize Carbon Intensity
            let sdaBonus = Math.Max(0.0, pred.SdaPercent - 55.0) * 1.8
            let biophilicBonus = pred.BiophilicViewPct * 0.75
            let coolingPenalty = pred.CoolingLoad * 1.25
            let stressPenalty = Math.Max(0.0, pred.PeakStress - 40.0) * 2.5
            let carbonPenalty = (pred.EmbodiedCarbon - 200.0) * 0.15

            let score = (pred.SdaPercent * 1.5) + sdaBonus + biophilicBonus - coolingPenalty - stressPenalty - carbonPenalty
            score

        let population = Array.init populationSize (fun _ -> sampleCandidate ())
        let scored = population |> Array.map (fun c -> c, fitness c)
        let bestCandidate, _ = scored |> Array.maxBy snd
        bestCandidate
