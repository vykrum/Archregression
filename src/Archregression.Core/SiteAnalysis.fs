namespace Archregression

open System

/// Pure F# Geographic & Topographic Analysis from Satellite DEM
module SiteAnalysis =

    let analyzeSiteTelemetry (name: string) (lat: float) (lng: float) (elevations: float array) : SiteLocation =
        let count = elevations.Length
        let centerElev =
            if count >= 81 then elevations.[40]
            elif count > 0 then elevations.[0]
            else 350.0

        let minElev = if count > 0 then Array.min elevations else centerElev
        let maxElev = if count > 0 then Array.max elevations else centerElev
        let relief = Math.Max(5.0, maxElev - minElev)

        let dLat = 0.0032
        let cosLat = Math.Max(0.15, Math.Cos(lat * Math.PI / 180.0))
        let distNS = dLat * 111000.0
        let distEW = (dLat / cosLat) * 111000.0 * cosLat

        let northElev, southElev, eastElev, westElev =
            if count >= 81 then
                elevations.[8 * 9 + 4], elevations.[0 * 9 + 4], elevations.[4 * 9 + 8], elevations.[4 * 9 + 0]
            elif count >= 5 then
                elevations.[1], elevations.[2], elevations.[3], elevations.[4]
            else
                centerElev, centerElev, centerElev, centerElev

        let gradY = (northElev - southElev) / (Math.Max(10.0, distNS))
        let gradX = (eastElev - westElev) / (Math.Max(10.0, distEW))
        let slopeRad = Math.Atan(Math.Sqrt(gradX * gradX + gradY * gradY))
        let slopeDeg = Math.Min(45.0, Math.Max(1.0, slopeRad * (180.0 / Math.PI)))

        let slopeDir =
            if Math.Abs(gradY) > Math.Abs(gradX) then
                if gradY > 0.0 then AspectNorth else AspectSouth
            else
                if gradX > 0.0 then AspectEast else AspectWest

        // Ocean & shoreline detection
        let isNearSea = centerElev < 45.0 && minElev <= 2.0
        let dropsToZero = elevations |> Array.exists (fun e -> e <= 0.0)
        let isOcean = isNearSea || dropsToZero

        // Freshwater lake detection (inland high-altitude depression)
        let isLake = not isOcean && relief > 40.0 && minElev > 150.0

        let waterType =
            if isOcean then OceanShoreline
            elif isLake then FreshwaterLake
            else NoWater

        let biome =
            if centerElev > 1800.0 then AlpineConifers
            elif isOcean then CoastalCypress
            else TemperateCanopy

        let preset =
            if isOcean then OceanCoastline
            elif centerElev > 650.0 || relief > 120.0 || slopeDeg > 18.0 then AlpineMountain
            elif isLake then InlandLake
            else ForestCanopy

        let treeDensity =
            if centerElev > 2400.0 then 0.15
            elif centerElev > 1800.0 then 0.40
            elif isOcean then 0.55
            else 0.80

        let topo =
            { ElevationGrid = elevations
              CenterElevation = centerElev
              MountainHeight = relief
              TerrainSlope = slopeDeg
              SlopeDirection = slopeDir
              Ruggedness = relief
              WaterType = waterType
              WaterProximity = if isOcean then 20.0 elif isLake then 25.0 else 99.0
              TreeDensity = treeDensity
              Biome = biome
              ElevationExaggeration = 1.0
              UrbanShadows = false }

        let urban =
            if isOcean then UrbanContextParams.DefaultSausalito
            elif centerElev > 650.0 then UrbanContextParams.DefaultChamonix
            elif isLake then UrbanContextParams.DefaultZurich
            else UrbanContextParams.DefaultBlackForest

        { Name = name
          Latitude = lat
          Longitude = lng
          Elevation = centerElev
          Preset = preset
          Topography = topo
          Urban = urban }
