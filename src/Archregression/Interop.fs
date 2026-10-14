namespace Archregression

open System
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
                        "wwr", box p.Wwr
                        "louver_angle", box p.LouverAngle
                        "overhang_depth", box p.OverhangDepth
                        "orientation_azimuth", box p.OrientationAzimuth
                        "glazing_u_val", box p.GlazingUVal
                    ]
                js.InvokeVoidAsync("archInterop.updateArchitecture", payload) |> ignore

        member this.ApplySite(site: SiteLocation) : unit =
            if not (isNull js) then
                let t = site.Topography
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
                        "name", box site.Name
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
