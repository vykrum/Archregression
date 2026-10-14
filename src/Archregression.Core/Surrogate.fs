namespace Archregression

open System
open System.Diagnostics

/// Pure F# High-Performance Surrogate ML Regression Engine (<0.5ms)
module Surrogate =

    let evaluate (p: ArchitecturalParams) (site: SiteLocation) : SurrogatePredictions =
        let sw = Stopwatch.StartNew()

        let topo = site.Topography
        let treeDensity = topo.TreeDensity
        let hasWater = topo.WaterType <> NoWater
        let isOcean = topo.WaterType = OceanShoreline || site.Preset = OceanCoastline
        let isLake = topo.WaterType = FreshwaterLake || site.Preset = InlandLake
        let isMountain = site.Preset = AlpineMountain || topo.MountainHeight > 25.0

        // 1. Natural Context Modifiers
        let treeShadeFactor = 1.0 - (treeDensity * 0.28)
        let waterCoolingFactor =
            if isOcean then 0.84
            elif isLake then 0.89
            else 1.0
        let mountainSolarModifier = if isMountain then 0.90 else 1.0

        // 2. Spatial Daylight Autonomy (% sDA 300/50%)
        let daylightPenetration = (p.Wwr * 118.0) / (1.0 + 0.11 * p.RoomDepth)
        let shadingFactor = Math.Cos(p.LouverAngle * Math.PI / 180.0) * (1.0 - 0.22 * p.OverhangDepth)
        let azimuthFactor = Math.Cos(p.OrientationAzimuth * 0.7 * Math.PI / 180.0)
        let rawSda = 14.0 + daylightPenetration * shadingFactor * azimuthFactor * (1.0 - treeDensity * 0.08)
        let sda = Math.Min(99.0, Math.Max(8.0, rawSda))

        // 3. Annual Cooling Load (kWh/m²/yr)
        let solarGain = (p.Wwr * 76.0) * (1.0 - 0.38 * (p.LouverAngle / 75.0)) * (1.0 - 0.24 * p.OverhangDepth) * treeShadeFactor * mountainSolarModifier
        let conductionGain = p.GlazingUVal * 14.2
        let orientationPenalty = 1.0 + 0.32 * Math.Cos(p.OrientationAzimuth * Math.PI / 180.0)
        let rawCooling = 22.0 + (solarGain * orientationPenalty) + conductionGain + (p.RoomDepth * 0.82)
        let cooling = Math.Min(150.0, Math.Max(16.0, rawCooling * waterCoolingFactor))

        // 4. Peak Structural Von Mises Stress (MPa)
        let span = Math.Max(p.RoomDepth, p.RoomWidth)
        let slopeMultiplier = 1.0 + (topo.TerrainSlope / 160.0)
        let stress = Math.Min(120.0, Math.Max(8.0, (8.2 + 0.086 * Math.Pow(span, 2.08) + (p.Wwr * 7.2)) * slopeMultiplier))

        // 5. Embodied Carbon Footprint (kgCO2e/m²)
        let envelopeCarbon = (p.Wwr * 175.0) + (p.OverhangDepth * 32.0) + (p.LouverAngle * 0.52)
        let structuralCarbon = 162.0 + 0.44 * Math.Pow(span, 1.91) * (1.0 + (topo.TerrainSlope / 240.0))
        let carbon = Math.Min(650.0, Math.Max(160.0, envelopeCarbon + structuralCarbon))

        // 6. Microclimate Cooling Drop (ΔT in °C)
        let treeCoolingC = treeDensity * 2.8
        let waterBreezeC = if isOcean then 2.2 elif isLake then 1.4 else 0.0
        let deltaTempC = Math.Min(5.2, treeCoolingC + waterBreezeC)

        // 7. Biophilic Green View Factor (%)
        let waterSightline = if isOcean then 38.0 elif isLake then 30.0 else 5.0
        let baseGreenView = (treeDensity * 55.0) + waterSightline
        let biophilicScore = Math.Min(98.0, Math.Max(15.0, baseGreenView * Math.Sqrt(p.Wwr)))

        // 8. Tree Biomass Carbon Sequestration (kgCO2e/yr)
        let treeCarbonOffset = int (Math.Round(treeDensity * 420.0))

        sw.Stop()
        let latency = Math.Max(0.2, sw.Elapsed.TotalMilliseconds)

        { SdaPercent = Math.Round(sda, 1)
          CoolingLoad = Math.Round(cooling, 1)
          PeakStress = Math.Round(stress, 1)
          EmbodiedCarbon = Math.Round(carbon, 1)
          MicroclimateDeltaT = Math.Round(deltaTempC, 1)
          BiophilicViewPct = Math.Round(biophilicScore, 0)
          TreeCarbonOffset = treeCarbonOffset
          LatencyMs = Math.Round(latency, 2) }
