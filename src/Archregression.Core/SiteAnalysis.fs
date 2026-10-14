namespace Archregression

open System

/// Pure F# Geographic & Topographic Analysis
module SiteAnalysis =

    let analyzeSiteTelemetry (name: string) (lat: float) (lng: float) (elevations: float array) : SiteLocation =
        let centerElev = if elevations.Length > 0 then elevations.[0] else 350.0
        let northElev = if elevations.Length > 1 then elevations.[1] else centerElev
        let southElev = if elevations.Length > 2 then elevations.[2] else centerElev
        let eastElev = if elevations.Length > 3 then elevations.[3] else centerElev
        let westElev = if elevations.Length > 4 then elevations.[4] else centerElev

        let dLat = 0.011
        let dLng = 0.015
        let distNS = dLat * 111000.0
        let distEW = dLng * 111000.0 * Math.Cos(lat * Math.PI / 180.0)

        let gradY = (northElev - southElev) / (2.0 * distNS)
        let gradX = (eastElev - westElev) / (2.0 * distEW)
        let slopeRad = Math.Atan(Math.Sqrt(gradX * gradX + gradY * gradY))
        let slopeDeg = Math.Min(35.0, Math.Max(1.0, slopeRad * (180.0 / Math.PI)))

        let minElev = Array.min elevations
        let maxElev = Array.max elevations
        let relief = Math.Max(10.0, maxElev - minElev)

        let slopeDir =
            if Math.Abs(gradY) > Math.Abs(gradX) then
                if gradY > 0.0 then AspectNorth else AspectSouth
            else
                if gradX > 0.0 then AspectEast else AspectWest

        // Ocean detection: Near sea level or negative drop
        let isNearSea = centerElev < 45.0 && minElev <= 2.0
        let dropsToZero = elevations |> Array.exists (fun e -> e <= 0.0)
        let isOcean = isNearSea || dropsToZero

        // Mountain detection: High altitude or steep relief
        let isMountain = centerElev > 650.0 || relief > 120.0 || slopeDeg > 15.0

        let preset, topo, urban =
            if isOcean then
                let topo =
                    { MountainHeight = Math.Min(22.0, Math.Max(8.0, slopeDeg * 0.8))
                      TerrainSlope = slopeDeg
                      SlopeDirection = slopeDir
                      Ruggedness = relief / 300.0
                      WaterType = OceanShoreline
                      WaterProximity = 20.0
                      TreeDensity = 0.55
                      Biome = CoastalCypress
                      UrbanShadows = false }
                OceanCoastline, topo, UrbanContextParams.DefaultSausalito
            elif isMountain then
                let topo =
                    { MountainHeight = Math.Min(50.0, Math.Max(22.0, relief * 0.12))
                      TerrainSlope = slopeDeg
                      SlopeDirection = slopeDir
                      Ruggedness = Math.Min(1.0, relief / 350.0)
                      WaterType = if relief > 200.0 && minElev > 400.0 then FreshwaterLake else NoWater
                      WaterProximity = 35.0
                      TreeDensity = if centerElev > 2200.0 then 0.25 else 0.50
                      Biome = AlpineConifers
                      UrbanShadows = false }
                AlpineMountain, topo, UrbanContextParams.DefaultChamonix
            else
                let isLake = relief > 40.0
                let topo =
                    { MountainHeight = Math.Min(18.0, Math.Max(6.0, relief * 0.1))
                      TerrainSlope = slopeDeg
                      SlopeDirection = slopeDir
                      Ruggedness = relief / 250.0
                      WaterType = if isLake then FreshwaterLake else NoWater
                      WaterProximity = 22.0
                      TreeDensity = 0.75
                      Biome = TemperateCanopy
                      UrbanShadows = false }
                let urbanPreset = if isLake then UrbanContextParams.DefaultZurich else UrbanContextParams.DefaultBlackForest
                (if isLake then InlandLake else ForestCanopy), topo, urbanPreset

        { Name = name
          Latitude = lat
          Longitude = lng
          Elevation = centerElev
          Preset = preset
          Topography = topo
          Urban = urban }
