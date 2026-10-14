namespace Archregression

open System
open System.Diagnostics

/// Pure F# High-Performance Surrogate ML Regression Engine & Urban/Site Telemetry (<0.5ms)
module Surrogate =

    let evaluate (p: ArchitecturalParams) (site: SiteLocation) : SurrogatePredictions =
        let sw = Stopwatch.StartNew()

        let topo = site.Topography
        let urban = site.Urban
        let treeDensity = topo.TreeDensity
        let hasWater = topo.WaterType <> NoWater
        let isOcean = topo.WaterType = OceanShoreline || site.Preset = OceanCoastline
        let isLake = topo.WaterType = FreshwaterLake || site.Preset = InlandLake
        let isMountain = site.Preset = AlpineMountain || topo.MountainHeight > 25.0

        // -------------------------------------------------------------
        // 1. Topographic Horizon Mask & Solar Occlusion
        // -------------------------------------------------------------
        let horizonDeg =
            if not (isNull topo.ElevationGrid) && topo.ElevationGrid.Length >= 81 then
                let dim = 9
                let mutable maxAngle = 0.0
                for r in 0 .. dim - 1 do
                    for c in 0 .. dim - 1 do
                        if r = 0 || r = dim - 1 || c = 0 || c = dim - 1 then
                            let elev = topo.ElevationGrid.[r * dim + c]
                            let diffElev = (elev - topo.CenterElevation) * topo.ElevationExaggeration
                            let xRel = (float c / float (dim - 1) - 0.5) * 150.0
                            let zRel = (float r / float (dim - 1) - 0.5) * 150.0
                            let dist = Math.Sqrt(xRel * xRel + zRel * zRel)
                            if dist > 5.0 && diffElev > 0.0 then
                                let ang = Math.Atan(diffElev / dist) * 180.0 / Math.PI
                                if ang > maxAngle then maxAngle <- ang
                Math.Min(45.0, maxAngle)
            else
                let mountainDist = Math.Max(25.0, topo.WaterProximity + 20.0)
                let horizonRad = Math.Atan(topo.MountainHeight / mountainDist)
                horizonRad * 180.0 / Math.PI

        let isNorthFacing = topo.SlopeDirection = AspectNorth
        let aspectSolarBonus = if isNorthFacing then -6.0 elif topo.SlopeDirection = AspectSouth then 4.0 else 0.0
        let mountainHorizonPenalty = Math.Max(0.0, (horizonDeg - 8.0) * 0.42)

        // -------------------------------------------------------------
        // 2. Cut & Fill Earthwork Analysis (2D Numerical Integration over DEM)
        // -------------------------------------------------------------
        let samplesX = 16
        let samplesZ = 16
        let dx = p.RoomWidth / float samplesX
        let dz = p.RoomDepth / float samplesZ
        let cellArea = dx * dz
        let mutable cutVol = 0.0
        let mutable fillVol = 0.0
        let mutable sumGroundElev = 0.0

        for ix in 0 .. samplesX - 1 do
            let xRel = -0.5 * p.RoomWidth + (float ix + 0.5) * dx
            for iz in 0 .. samplesZ - 1 do
                let zRel = -0.5 * p.RoomDepth + (float iz + 0.5) * dz
                let terrainZ = Topography.sampleRawGridOffset xRel zRel topo
                sumGroundElev <- sumGroundElev + terrainZ
                let diff = terrainZ - p.PadElevationOffset
                if diff > 0.0 then
                    cutVol <- cutVol + diff * cellArea
                else
                    fillVol <- fillVol + (-diff) * cellArea

        let netEarthwork = cutVol - fillVol
        let totalSamples = float (samplesX * samplesZ)
        let optimalPadZ = sumGroundElev / totalSamples

        // -------------------------------------------------------------
        // 3. Foundation Strategy Classification
        // -------------------------------------------------------------
        let foundationType =
            if topo.TerrainSlope < 7.0 then "Slab-on-Grade (Flat)"
            elif topo.TerrainSlope < 18.0 then "Split-Level Terraced (2 Steps)"
            elif topo.TerrainSlope < 28.0 then "Split-Level Terraced (3 Steps)"
            else "Elevated Pier Foundation"

        // -------------------------------------------------------------
        // 4. Hydrology & Runoff Risk (TWI Proxy)
        // -------------------------------------------------------------
        let baseRunoff = 1.0 + (topo.TerrainSlope / 4.2) + (Math.Min(1.0, topo.Ruggedness / 120.0) * 3.2)
        let waterBufferFactor = if hasWater && topo.WaterProximity < 25.0 then 2.0 else 0.0
        let runoffRisk = Math.Min(10.0, Math.Max(1.0, baseRunoff + waterBufferFactor))

        // -------------------------------------------------------------
        // 5. Urban Acoustics: Decibel Field & Facade Attenuation (R'w)
        // -------------------------------------------------------------
        let trafficSourceDb =
            urban.AmbientNoiseLdenDb + 10.0 * Math.Log10(Math.Max(1.0, float urban.TrafficVolumeAadt / 6000.0))
        let distanceDecayDb =
            10.0 * Math.Log10(Math.Max(1.0, urban.RoadDistanceM / 8.0))
        let treeAcousticBuffer = treeDensity * 4.5
        let facadeNoiseDb = Math.Max(34.0, trafficSourceDb - distanceDecayDb - treeAcousticBuffer)
        let requiredRw = Math.Min(56.0, Math.Max(26.0, facadeNoiseDb - 32.0 + (p.Wwr * 6.5)))

        // -------------------------------------------------------------
        // 6. Connectivity, Mobility & Logistics
        // -------------------------------------------------------------
        let transitScore = Math.Min(100.0, Math.Max(5.0, 100.0 - (urban.TransitDistanceM / 12.0)))
        let curbCutPermitted =
            not (urban.RoadType = ArterialHighway && urban.TrafficVolumeAadt > 18000 && urban.RoadDistanceM < 15.0)

        // -------------------------------------------------------------
        // 7. Population & Zoning Envelope
        // -------------------------------------------------------------
        let footprintArea = p.RoomDepth * p.RoomWidth
        let totalGfa = footprintArea * float p.StoryCount
        let buildingHeight = float p.StoryCount * 3.6
        let farRatio = totalGfa / urban.Zoning.LotAreaM2
        let farUtilPct = (farRatio / urban.Zoning.MaxFar) * 100.0
        let occupantCap = int (Math.Round(totalGfa * urban.Zoning.TargetOccupancyPerM2))
        let zoningCompliant = (farRatio <= urban.Zoning.MaxFar) && (buildingHeight <= urban.Zoning.MaxBuildingHeightM)

        // -------------------------------------------------------------
        // 8. Environmental Performance Surrogates
        // -------------------------------------------------------------
        let treeShadeFactor = 1.0 - (treeDensity * 0.28)
        let waterCoolingFactor =
            if isOcean then 0.84
            elif isLake then 0.89
            else 1.0
        let mountainSolarModifier = if isMountain then 0.90 else 1.0

        // Spatial Daylight Autonomy (% sDA 300/50%)
        let daylightPenetration = (p.Wwr * 118.0) / (1.0 + 0.11 * p.RoomDepth)
        let shadingFactor = Math.Cos(p.LouverAngle * Math.PI / 180.0) * (1.0 - 0.22 * p.OverhangDepth)
        let azimuthFactor = Math.Cos(p.OrientationAzimuth * 0.7 * Math.PI / 180.0)
        let rawSda = 14.0 + daylightPenetration * shadingFactor * azimuthFactor * (1.0 - treeDensity * 0.08) + aspectSolarBonus - mountainHorizonPenalty
        let sda = Math.Min(99.0, Math.Max(8.0, rawSda))

        // Annual Cooling Load (kWh/m²/yr)
        let solarGain = (p.Wwr * 76.0) * (1.0 - 0.38 * (p.LouverAngle / 75.0)) * (1.0 - 0.24 * p.OverhangDepth) * treeShadeFactor * mountainSolarModifier
        let conductionGain = p.GlazingUVal * 14.2
        let orientationPenalty = 1.0 + 0.32 * Math.Cos(p.OrientationAzimuth * Math.PI / 180.0)
        let storyCompactionBenefit = 1.0 - (float (p.StoryCount - 1) * 0.04)
        let rawCooling = (22.0 + (solarGain * orientationPenalty) + conductionGain + (p.RoomDepth * 0.82)) * storyCompactionBenefit
        let cooling = Math.Min(150.0, Math.Max(16.0, rawCooling * waterCoolingFactor))

        // Peak Structural Von Mises Stress (MPa)
        let span = Math.Max(p.RoomDepth, p.RoomWidth)
        let slopeMultiplier = 1.0 + (topo.TerrainSlope / 150.0)
        let storyStressMultiplier = 1.0 + (float (p.StoryCount - 1) * 0.20)
        let stress = Math.Min(140.0, Math.Max(8.0, ((8.2 + 0.086 * Math.Pow(span, 2.08) + (p.Wwr * 7.2)) * slopeMultiplier) * storyStressMultiplier))

        // Embodied Carbon Footprint (kgCO2e/m²)
        let envelopeCarbon = (p.Wwr * 175.0) + (p.OverhangDepth * 32.0) + (p.LouverAngle * 0.52)
        let acousticCarbonPenalty = if requiredRw > 36.0 then (requiredRw - 36.0) * 3.8 else 0.0
        let transitCarbonCredit = (transitScore / 100.0) * 16.0
        let structuralCarbon = (162.0 + 0.44 * Math.Pow(span, 1.91) * (1.0 + (topo.TerrainSlope / 240.0))) * (1.0 + float (p.StoryCount - 1) * 0.08)
        let carbon = Math.Min(700.0, Math.Max(140.0, envelopeCarbon + acousticCarbonPenalty + structuralCarbon - transitCarbonCredit))

        // Microclimate Cooling Drop (ΔT in °C)
        let treeCoolingC = treeDensity * 2.8
        let waterBreezeC = if isOcean then 2.2 elif isLake then 1.4 else 0.0
        let deltaTempC = Math.Min(5.2, treeCoolingC + waterBreezeC)

        // Biophilic Green View Factor (%)
        let waterSightline = if isOcean then 38.0 elif isLake then 30.0 else 5.0
        let baseGreenView = (treeDensity * 55.0) + waterSightline
        let biophilicScore = Math.Min(98.0, Math.Max(15.0, baseGreenView * Math.Sqrt(p.Wwr)))

        // Tree Biomass Carbon Sequestration (kgCO2e/yr)
        let treeCarbonOffset = int (Math.Round(treeDensity * 420.0))

        sw.Stop()
        let latency = Math.Max(0.20, sw.Elapsed.TotalMilliseconds)

        { SdaPercent = Math.Round(sda, 1)
          CoolingLoad = Math.Round(cooling, 1)
          PeakStress = Math.Round(stress, 1)
          EmbodiedCarbon = Math.Round(carbon, 1)
          MicroclimateDeltaT = Math.Round(deltaTempC, 1)
          BiophilicViewPct = Math.Round(biophilicScore, 0)
          TreeCarbonOffset = treeCarbonOffset
          LatencyMs = Math.Round(latency, 2)
          CutVolumeM3 = Math.Round(cutVol, 1)
          FillVolumeM3 = Math.Round(fillVol, 1)
          NetEarthworkM3 = Math.Round(netEarthwork, 1)
          OptimalPadElevationM = Math.Round(optimalPadZ, 2)
          FoundationType = foundationType
          HorizonObstructionDeg = Math.Round(horizonDeg, 1)
          RunoffRiskIndex = Math.Round(runoffRisk, 1)
          FacadeNoiseLdenDb = Math.Round(facadeNoiseDb, 1)
          RequiredAcousticRwDb = Math.Round(requiredRw, 1)
          TransitConnectivityScore = Math.Round(transitScore, 0)
          GrossFloorAreaM2 = Math.Round(totalGfa, 1)
          FarRatio = Math.Round(farRatio, 2)
          FarUtilizationPct = Math.Round(farUtilPct, 1)
          OccupantCapacity = occupantCap
          CurbCutPermitted = curbCutPermitted
          ZoningCompliant = zoningCompliant }
