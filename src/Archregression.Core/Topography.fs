namespace Archregression

open System

/// Pure F# functional procedural & satellite DEM 3D topography and heightfield generator
module Topography =

    let inline clamp (minVal: float) (maxVal: float) (v: float) =
        Math.Max(minVal, Math.Min(maxVal, v))

    let inline smoothstep (edge0: float) (edge1: float) (x: float) =
        let t = clamp 0.0 1.0 ((x - edge0) / (edge1 - edge0))
        t * t * (3.0 - 2.0 * t)

    /// 2D Bilinear interpolation over an NxN elevation heightfield grid
    let sampleElevationGrid (grid: float array) (gridDim: int) (uNorm: float) (vNorm: float) : float =
        if isNull grid || grid.Length = 0 then 0.0
        elif grid.Length = 1 then grid.[0]
        else
            let uClamped = clamp 0.0 1.0 uNorm
            let vClamped = clamp 0.0 1.0 vNorm
            let gx = uClamped * float (gridDim - 1)
            let gz = vClamped * float (gridDim - 1)
            let x0 = int (Math.Floor gx)
            let z0 = int (Math.Floor gz)
            let x1 = Math.Min(gridDim - 1, x0 + 1)
            let z1 = Math.Min(gridDim - 1, z0 + 1)
            let tx = gx - float x0
            let tz = gz - float z0
            let e00 = grid.[z0 * gridDim + x0]
            let e10 = grid.[z0 * gridDim + x1]
            let e01 = grid.[z1 * gridDim + x0]
            let e11 = grid.[z1 * gridDim + x1]
            (1.0 - tx) * (1.0 - tz) * e00 + tx * (1.0 - tz) * e10 + (1.0 - tx) * tz * e01 + tx * tz * e11

    /// Raw ground elevation relative to site center (used for Cut & Fill volume calculations)
    let sampleRawGridOffset (x: float) (z: float) (topo: TopographyParams) : float =
        if not (isNull topo.ElevationGrid) && topo.ElevationGrid.Length >= 9 then
            let dim = int (Math.Round(Math.Sqrt(float topo.ElevationGrid.Length)))
            let u = clamp 0.0 1.0 ((x + 75.0) / 150.0)
            let v = clamp 0.0 1.0 ((z + 75.0) / 150.0)
            let rawE = sampleElevationGrid topo.ElevationGrid dim u v
            (rawE - topo.CenterElevation) * topo.ElevationExaggeration
        else
            let slopeRad = topo.TerrainSlope * Math.PI / 180.0
            let dirX, dirZ =
                match topo.SlopeDirection with
                | AspectNorth -> 0.0, 1.0
                | AspectSouth -> 0.0, -1.0
                | AspectEast -> 1.0, 0.0
                | AspectWest -> -1.0, 0.0
            (z * dirZ + x * dirX) * Math.Tan(slopeRad)

    /// Calculates procedural alpine mountain ridge elevation with multi-tier crags (fallback)
    let calculateMountainElevation (x: float) (z: float) (mHeight: float) (ruggedness: float) (slope: float) (aspect: SlopeAspect) : float =
        let dirX, dirZ =
            match aspect with
            | AspectNorth -> 0.0, 1.0
            | AspectSouth -> 0.0, -1.0
            | AspectEast -> 1.0, 0.0
            | AspectWest -> -1.0, 0.0

        let ridge1 = Math.Abs(Math.Sin(x * 0.045 + 1.2) * Math.Cos(z * 0.038)) * mHeight
        let crags = Math.Sin(x * 0.11) * Math.Cos(z * 0.12) * (mHeight * 0.28 * ruggedness)
        let slopeGrad = (z * dirZ + x * dirX) * (slope * 0.035)
        ridge1 + crags + slopeGrad

    /// Calculates coastal bluff drop down to sandy beach and sea level (fallback)
    let calculateOceanElevation (x: float) (z: float) (waterProx: float) (slope: float) : float =
        let bluffHeight = Math.Max(4.0, slope * 0.6)
        let shoreDrop = Math.Max(-1.8, Math.Min(bluffHeight, (z + waterProx) * 0.22))
        let wavesShore = Math.Sin(x * 0.08) * 0.8
        shoreDrop + wavesShore

    /// Calculates inland lake basin depression surrounded by rolling hills (fallback)
    let calculateLakeElevation (x: float) (z: float) (waterProx: float) (mHeight: float) : float =
        let distFromLake = Math.Sqrt(x * x + Math.Pow(z + waterProx, 2.0))
        let lakeShore = Math.Min(12.0, Math.Max(-1.2, (distFromLake - 18.0) * 0.22))
        let undulation = Math.Sin(x * 0.06) * Math.Cos(z * 0.06) * (mHeight * 0.4)
        lakeShore + undulation

    /// Returns the ground elevation at any (x, z) coordinate, keeping the building parcel level
    let getElevationAt (x: float) (z: float) (topo: TopographyParams) (preset: SitePreset) : float =
        let distFromCenter = Math.Sqrt(x * x + z * z)
        let parcelFlatten = smoothstep 13.0 26.0 distFromCenter

        let rawElevation =
            if not (isNull topo.ElevationGrid) && topo.ElevationGrid.Length >= 9 then
                let dim = int (Math.Round(Math.Sqrt(float topo.ElevationGrid.Length)))
                let u = clamp 0.0 1.0 ((x + 75.0) / 150.0)
                let v = clamp 0.0 1.0 ((z + 75.0) / 150.0)
                let sampleVal = sampleElevationGrid topo.ElevationGrid dim u v
                (sampleVal - topo.CenterElevation) * topo.ElevationExaggeration
            else
                match preset with
                | AlpineMountain ->
                    calculateMountainElevation x z topo.MountainHeight topo.Ruggedness topo.TerrainSlope topo.SlopeDirection
                | OceanCoastline ->
                    calculateOceanElevation x z topo.WaterProximity topo.TerrainSlope
                | InlandLake ->
                    calculateLakeElevation x z topo.WaterProximity topo.MountainHeight
                | ForestCanopy ->
                    Math.Sin(x * 0.08) * Math.Cos(z * 0.07) * 4.2 + (z * 0.05)
                | CustomSite ->
                    match topo.WaterType with
                    | OceanShoreline -> calculateOceanElevation x z topo.WaterProximity topo.TerrainSlope
                    | FreshwaterLake -> calculateLakeElevation x z topo.WaterProximity topo.MountainHeight
                    | NoWater ->
                        if topo.MountainHeight > 10.0 then
                            calculateMountainElevation x z topo.MountainHeight topo.Ruggedness topo.TerrainSlope topo.SlopeDirection
                        else
                            (z * (topo.TerrainSlope * 0.03))
                | UrbanCanyon | StudioPlinth -> 0.0

        (rawElevation * parcelFlatten) - 0.25

    /// Altitude-dependent RGB vertex color shading based on real physical elevation & terrain context
    let getVertexColor (elevation: float) (preset: SitePreset) (topo: TopographyParams) : float * float * float =
        let absElevation = topo.CenterElevation + elevation
        if absElevation > 2400.0 then
            0.88, 0.92, 0.98   // Snow caps & glacier frost
        elif absElevation > 1400.0 then
            0.38, 0.42, 0.46  // Alpine granite slate rock
        elif topo.WaterType = OceanShoreline && elevation < 1.0 then
            0.78, 0.70, 0.52  // Shoreline sand beach
        elif topo.WaterType = FreshwaterLake && elevation < 0.5 then
            0.55, 0.55, 0.52  // Pebble lake shore
        elif topo.Biome = AlpineConifers then
            0.20, 0.35, 0.22  // Subalpine meadow & conifers
        elif topo.Biome = CoastalCypress then
            0.28, 0.38, 0.24  // Coastal scrub & cypress
        else
            0.18, 0.30, 0.16  // Temperate canopy greenery
