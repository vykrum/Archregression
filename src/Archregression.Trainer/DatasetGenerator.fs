namespace Archregression.Trainer

open System
open Archregression

module DatasetGenerator =

    type SamplePoint =
        { RoomDepth: float
          RoomWidth: float
          Wwr: float
          LouverAngle: float
          OverhangDepth: float
          OrientationAzimuth: float
          GlazingUVal: float
          TreeDensity: float
          HasWater: float
          Slope: float }

    type EvaluatedSample =
        { Parameters: SamplePoint
          SdaPercent: float
          CoolingLoad: float
          PeakStress: float
          EmbodiedCarbon: float
          MicroclimateCooling: float
          BiophilicView: float }

    /// Ground-truth multi-physical simulation equations
    let evaluatePhysics (p: SamplePoint) : EvaluatedSample =
        let aspect = p.RoomDepth / Math.Max(1.0, p.RoomWidth)
        let area = p.RoomDepth * p.RoomWidth
        let orientationRad = p.OrientationAzimuth * (Math.PI / 180.0)
        let solarFactor = Math.Max(0.2, Math.Cos(orientationRad))

        // Daylight Autonomy (sDA 300/50%)
        let sdaBase = (p.Wwr * 105.0) - (p.RoomDepth * 2.8) - (p.LouverAngle * 0.38) + (solarFactor * 14.0) + (p.OverhangDepth * -5.0)
        let sda = Math.Max(12.0, Math.Min(98.0, sdaBase))

        // Annual Cooling Load (kWh/m²/yr)
        let coolingSolar = (p.Wwr * 65.0 * solarFactor) - (p.LouverAngle * 0.42) - (p.OverhangDepth * 8.5)
        let coolingConduction = (p.GlazingUVal * 16.0) + (p.RoomDepth * 1.8)
        let coolingMitigation = (p.TreeDensity * 12.0) + (p.HasWater * 6.5)
        let cooling = Math.Max(22.0, Math.Min(145.0, 35.0 + coolingSolar + coolingConduction - coolingMitigation))

        // Peak Structural Stress (MPa)
        let span = Math.Max(p.RoomDepth, p.RoomWidth)
        let stressBase = (span * span * 0.22) + (aspect * 4.5) + (p.Wwr * 6.0)
        let stress = Math.Max(12.0, Math.Min(118.0, stressBase))

        // Embodied Carbon Footprint (kgCO₂e/m²)
        let envelopeArea = (2.0 * p.RoomDepth * 3.8) + (2.0 * p.RoomWidth * 3.8)
        let glazingArea = envelopeArea * p.Wwr
        let frameCarbon = area * 145.0
        let glassCarbon = glazingArea * (if p.GlazingUVal < 1.4 then 68.0 else 42.0)
        let carbonPerM2 = (frameCarbon + glassCarbon) / Math.Max(1.0, area)
        let carbon = Math.Max(180.0, Math.Min(560.0, carbonPerM2))

        // Microclimate Cooling Drop (ΔT °C)
        let treeDelta = p.TreeDensity * 3.4
        let waterDelta = p.HasWater * 1.6
        let slopeBreeze = (p.Slope / 35.0) * 0.5
        let microclimate = Math.Max(0.2, Math.Min(5.2, treeDelta + waterDelta + slopeBreeze))

        // Biophilic & Green View Factor (%)
        let biophilicBase = (p.TreeDensity * 55.0) + (p.HasWater * 25.0) + (p.Wwr * 20.0) - (p.RoomDepth * 1.2)
        let biophilia = Math.Max(5.0, Math.Min(98.0, biophilicBase))

        { Parameters = p
          SdaPercent = sda
          CoolingLoad = cooling
          PeakStress = stress
          EmbodiedCarbon = carbon
          MicroclimateCooling = microclimate
          BiophilicView = biophilia }

    /// Generates Latin-hypercube-style space-filling parameter grid
    let generateDataset (count: int) (seed: int) : EvaluatedSample list =
        let rng = Random(seed)
        [ 0 .. (count - 1) ]
        |> List.map (fun _ ->
            let p =
                { RoomDepth = 6.0 + rng.NextDouble() * 18.0
                  RoomWidth = 6.0 + rng.NextDouble() * 14.0
                  Wwr = 0.20 + rng.NextDouble() * 0.65
                  LouverAngle = rng.NextDouble() * 60.0
                  OverhangDepth = rng.NextDouble() * 2.5
                  OrientationAzimuth = -90.0 + rng.NextDouble() * 180.0
                  GlazingUVal = 0.8 + rng.NextDouble() * 2.4
                  TreeDensity = rng.NextDouble()
                  HasWater = if rng.NextDouble() > 0.45 then 1.0 else 0.0
                  Slope = rng.NextDouble() * 35.0 }
            evaluatePhysics p)
