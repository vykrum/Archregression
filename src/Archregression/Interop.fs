namespace Archregression

open System
open System.Text.Json
open System.Threading.Tasks
open Microsoft.JSInterop

/// F# JavaScript Interop Bridge for Three.js Viewport, Plotly, and Leaflet
module Interop =

    type ViewportBridge(js: IJSRuntime) =

        member this.UpdateArchitecture(p: ArchitecturalParams) : unit =
            if not (isNull js) then
                let payload =
                    dict [
                        "room_depth", box p.RoomDepth
                        "room_width", box p.RoomWidth
                        "story_count", box p.StoryCount
                        "wwr", box p.Wwr
                        "louver_angle", box p.LouverAngle
                        "overhang_depth", box p.OverhangDepth
                        "orientation_azimuth", box p.OrientationAzimuth
                        "glazing_u_val", box p.GlazingUVal
                        "pad_elevation_offset", box p.PadElevationOffset
                    ]
                js.InvokeVoidAsync("archInterop.updateArchitecture", payload) |> ignore

        member this.ApplySite(site: SiteLocation) : unit =
            if not (isNull js) then
                let t = site.Topography
                let u = site.Urban
                let payload =
                    dict [
                        "preset", box site.Preset.Key
                        "mountainHeight", box t.MountainHeight
                        "slope", box t.TerrainSlope
                        "slopeDir", box t.SlopeDirection.Key
                        "ruggedness", box t.Ruggedness
                        "waterType", box t.WaterType.Key
                        "waterProximity", box t.WaterProximity
                        "treeDensity", box t.TreeDensity
                        "biome", box t.Biome.Key
                        "elevationGrid", box t.ElevationGrid
                        "centerElevation", box t.CenterElevation
                        "elevationExaggeration", box t.ElevationExaggeration
                        "name", box site.Name
                        "roadName", box u.RoadName
                        "roadType", box u.RoadType.Key
                        "roadDistance", box u.RoadDistanceM
                        "trafficVolume", box u.TrafficVolumeAadt
                        "ambientNoise", box u.AmbientNoiseLdenDb
                        "transitDistance", box u.TransitDistanceM
                        "maxFar", box u.Zoning.MaxFar
                    ]
                js.InvokeVoidAsync("archInterop.applySite", payload) |> ignore

        member this.SetMetric(target: SimulationTarget) : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.setMetric", target.Key) |> ignore

        member this.SetCameraView(viewName: string) : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.setCameraView", viewName) |> ignore

        member this.ToggleSpatialGraph(show: bool) : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.toggleSpatialGraph", show) |> ignore

        member this.ToggleWireframe(show: bool) : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.toggleWireframe", show) |> ignore

        member this.TogglePlantMode(active: bool) : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.togglePlantMode", active) |> ignore

        member this.ExportGLB() : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.exportGLB") |> ignore

        member this.InitMap() : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.initMap") |> ignore

        member this.DetectLocation() : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.detectLocation") |> ignore

        member this.ApplyMapSite() : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.applyMapSite") |> ignore

        member this.ResizeCharts() : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.resizeCharts") |> ignore

        member this.ResizeViewport() : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.resizeViewport") |> ignore

        member this.SetTheme(theme: string) : unit =
            if not (isNull js) then
                js.InvokeVoidAsync("archInterop.setTheme", theme) |> ignore

        member this.GetThemeAsync() : Task<string> =
            task {
                if isNull js then return "light"
                else
                    try
                        let! t = js.InvokeAsync<string>("archInterop.getTheme").AsTask()
                        return if String.IsNullOrWhiteSpace(t) then "light" else t
                    with _ ->
                        return "light"
            }


        member this.ParseSiteFromJson(jsonStr: string) : SiteLocation option =
            try
                if String.IsNullOrWhiteSpace jsonStr then None
                else
                    use doc = JsonDocument.Parse jsonStr
                    let root = doc.RootElement

                    let getPropString (name: string) (def: string) =
                        match root.TryGetProperty name with
                        | true, prop when prop.ValueKind = JsonValueKind.String -> prop.GetString()
                        | _ -> def

                    let getPropDouble (name: string) (def: float) =
                        match root.TryGetProperty name with
                        | true, prop when prop.ValueKind = JsonValueKind.Number -> prop.GetDouble()
                        | _ -> def

                    let name = getPropString "name" "Current Site Location"
                    let lat = getPropDouble "lat" 37.8590
                    let lng = getPropDouble "lng" -122.4853
                    let elev = getPropDouble "elevation" 18.0
                    let slope = getPropDouble "slope" 14.0
                    let slopeDir = SlopeAspect.FromKey (getPropString "slopeDir" "south")
                    let rugged = getPropDouble "ruggedness" 37.0
                    let waterKey = getPropString "waterType" "none"
                    let waterProx = getPropDouble "waterProximity" 20.0
                    let treeDensity = getPropDouble "treeDensity" 0.60
                    let biomeKey = getPropString "biome" "temperate"
                    let presetKey = getPropString "preset" "custom"

                    let elevGrid =
                        match root.TryGetProperty "elevationGrid" with
                        | true, arr when arr.ValueKind = JsonValueKind.Array ->
                            [| for item in arr.EnumerateArray() do
                                   if item.ValueKind = JsonValueKind.Number then
                                       yield item.GetDouble() |]
                        | _ -> [||]

                    let topo =
                        { ElevationGrid = if elevGrid.Length >= 9 then elevGrid else TopographyParams.DefaultOcean.ElevationGrid
                          CenterElevation = elev
                          MountainHeight = rugged
                          TerrainSlope = slope
                          SlopeDirection = slopeDir
                          Ruggedness = rugged
                          WaterType = WaterBodyType.FromKey waterKey
                          WaterProximity = waterProx
                          TreeDensity = treeDensity
                          Biome = VegetationBiome.FromKey biomeKey
                          ElevationExaggeration = 1.0
                          UrbanShadows = false }

                    let roadName = getPropString "roadName" "Detected Access Road"
                    let roadDist = getPropDouble "roadDistance" 25.0
                    let roadTypeStr = getPropString "roadType" "local"
                    let trafficVol = int (getPropDouble "trafficVolume" 2100.0)
                    let noiseDb = getPropDouble "ambientNoise" 48.0
                    let transitDist = getPropDouble "transitDistance" 350.0

                    let roadType = RoadClassification.FromKey roadTypeStr

                    let urban =
                        { RoadName = roadName
                          RoadType = roadType
                          RoadDistanceM = roadDist
                          TrafficVolumeAadt = trafficVol
                          AmbientNoiseLdenDb = noiseDb
                          TransitDistanceM = transitDist
                          Zoning =
                              if elev > 650.0 then UrbanZoningParams.Default
                              elif topo.WaterType = OceanShoreline then { UrbanZoningParams.Default with MaxFar = 1.8; LotAreaM2 = 1400.0 }
                              elif topo.WaterType = FreshwaterLake then { UrbanZoningParams.Default with MaxFar = 2.8; LotAreaM2 = 1200.0 }
                              else UrbanZoningParams.Default }

                    let site =
                        { Name = name
                          Latitude = lat
                          Longitude = lng
                          Elevation = elev
                          Preset = SitePreset.FromKey presetKey
                          Topography = topo
                          Urban = urban }

                    Some site
            with ex ->
                Console.WriteLine($"[Interop] ParseSiteFromJson error: {ex.Message}")
                None

        member this.GetMapSiteAsync() : Task<SiteLocation option> =
            task {
                if isNull js then return None
                else
                    try
                        let! jsonStr = js.InvokeAsync<string>("archInterop.getMapSiteJson").AsTask()
                        return this.ParseSiteFromJson jsonStr
                    with ex ->
                        Console.WriteLine($"[Interop] GetMapSiteAsync error: {ex.Message}")
                        return None
            }

        member this.DetectLocationAsync() : Task<SiteLocation option> =
            task {
                if isNull js then return None
                else
                    try
                        let! jsonStr = js.InvokeAsync<string>("archInterop.detectLocationAsync").AsTask()
                        return this.ParseSiteFromJson jsonStr
                    with ex ->
                        Console.WriteLine($"[Interop] DetectLocationAsync error: {ex.Message}")
                        return None
            }
