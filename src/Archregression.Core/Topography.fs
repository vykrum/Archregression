namespace Archregression

open System

/// Pure F# functional procedural 3D topography and heightfield generator
module Topography =

    let inline clamp (minVal: float) (maxVal: float) (v: float) =
        Math.Max(minVal, Math.Min(maxVal, v))

    let inline smoothstep (edge0: float) (edge1: float) (x: float) =
        let t = clamp 0.0 1.0 ((x - edge0) / (edge1 - edge0))
        t * t * (3.0 - 2.0 * t)

    /// Calculates procedural alpine mountain ridge elevation with multi-tier crags
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

    /// Calculates coastal bluff drop down to sandy beach and sea level
    let calculateOceanElevation (x: float) (z: float) (waterProx: float) (slope: float) : float =
        let bluffHeight = Math.Max(4.0, slope * 0.6)
        let shoreDrop = Math.Max(-1.8, Math.Min(bluffHeight, (z + waterProx) * 0.22))
        let wavesShore = Math.Sin(x * 0.08) * 0.8
        shoreDrop + wavesShore

    /// Calculates inland lake basin depression surrounded by rolling hills
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

    /// Altitude-dependent RGB vertex color shading in pure F#
    let getVertexColor (elevation: float) (preset: SitePreset) (topo: TopographyParams) : float * float * float =
        match preset with
        | AlpineMountain ->
            if elevation > 16.0 then 0.88, 0.92, 0.98   // Snow caps & glacier frost
            elif elevation > 8.0 then 0.38, 0.42, 0.46  // Alpine granite slate rock
            else 0.20, 0.35, 0.22                       // Alpine meadow grass
        | OceanCoastline ->
            if elevation < 0.2 then 0.72, 0.62, 0.44    // Wet shoreline sand
            elif elevation < 2.0 then 0.82, 0.74, 0.55  // Coastal sand dunes
            else 0.30, 0.40, 0.25                       // Coastal bluff scrub
        | InlandLake ->
            if elevation < 0.3 then 0.55, 0.55, 0.52    // Pebble beach rim
            else 0.16, 0.34, 0.20                       // Freshwater riparian grass
        | ForestCanopy ->
            0.11, 0.26, 0.13                            // Deep forest loam & moss
        | CustomSite ->
            if topo.WaterType = OceanShoreline && elevation < 1.0 then 0.80, 0.70, 0.50
            elif topo.MountainHeight > 18.0 && elevation > 16.0 then 0.88, 0.92, 0.98
            elif topo.MountainHeight > 18.0 && elevation > 8.0 then 0.38, 0.42, 0.46
            else 0.20, 0.34, 0.20
        | UrbanCanyon -> 0.25, 0.28, 0.32
        | StudioPlinth -> 0.10, 0.12, 0.16
