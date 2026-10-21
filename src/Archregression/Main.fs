namespace Archregression

open System
open Elmish
open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop

type MainComponent() =
    inherit ProgramComponent<Model, Message>()

    [<Inject>]
    member val JSRuntime: IJSRuntime = null with get, set

    override this.Program =
        let bridge = Interop.ViewportBridge(this.JSRuntime)

        let init _ =
            let p = ArchitecturalParams.Default
            let s = SiteLocation.Sausalito
            let pred = Surrogate.evaluate p s
            bridge.UpdateArchitecture p
            bridge.ApplySite s

            let initialModel =
                { Architecture = p
                  Site = s
                  ActiveTarget = DaylightAutonomy
                  Prediction = pred
                  ShowCustomPanel = false
                  ShowMapModal = false
                  PlantTreeMode = false
                  ShowSpatialGraph = true
                  ShowWireframe = false
                  ActiveCameraView = "iso"
                  IsOptimizing = false
                  ActiveDrawer = NoDrawer
                  IsDashboardCollapsed = false
                  IsParametersCollapsed = false
                  Theme = Light }

            let detectCmd =
                Cmd.OfTask.perform
                    (fun () -> bridge.DetectLocationAsync())
                    ()
                    (function
                     | Some site -> ApplySiteFromMap site
                     | None -> CloseMapModal)

            let syncThemeCmd =
                Cmd.OfTask.perform
                    (fun () -> bridge.GetThemeAsync())
                    ()
                    (fun t -> SetTheme (ThemeMode.FromKey t))

            initialModel, Cmd.batch [ detectCmd; syncThemeCmd ]

        let update msg model =
            match msg with
            | SetRoomDepth v ->
                let p = { model.Architecture with RoomDepth = v }
                let pred = Surrogate.evaluate p model.Site
                bridge.UpdateArchitecture p
                { model with Architecture = p; Prediction = pred }, Cmd.none

            | SetRoomWidth v ->
                let p = { model.Architecture with RoomWidth = v }
                let pred = Surrogate.evaluate p model.Site
                bridge.UpdateArchitecture p
                { model with Architecture = p; Prediction = pred }, Cmd.none

            | SetStoryCount v ->
                let p = { model.Architecture with StoryCount = v }
                let pred = Surrogate.evaluate p model.Site
                bridge.UpdateArchitecture p
                { model with Architecture = p; Prediction = pred }, Cmd.none

            | SetWwr v ->
                let p = { model.Architecture with Wwr = v }
                let pred = Surrogate.evaluate p model.Site
                bridge.UpdateArchitecture p
                { model with Architecture = p; Prediction = pred }, Cmd.none

            | SetLouverAngle v ->
                let p = { model.Architecture with LouverAngle = v }
                let pred = Surrogate.evaluate p model.Site
                bridge.UpdateArchitecture p
                { model with Architecture = p; Prediction = pred }, Cmd.none

            | SetOverhangDepth v ->
                let p = { model.Architecture with OverhangDepth = v }
                let pred = Surrogate.evaluate p model.Site
                bridge.UpdateArchitecture p
                { model with Architecture = p; Prediction = pred }, Cmd.none

            | SetOrientationAzimuth v ->
                let p = { model.Architecture with OrientationAzimuth = v }
                let pred = Surrogate.evaluate p model.Site
                bridge.UpdateArchitecture p
                { model with Architecture = p; Prediction = pred }, Cmd.none

            | SetGlazingUVal v ->
                let p = { model.Architecture with GlazingUVal = v }
                let pred = Surrogate.evaluate p model.Site
                bridge.UpdateArchitecture p
                { model with Architecture = p; Prediction = pred }, Cmd.none

            | SetPadElevationOffset v ->
                let p = { model.Architecture with PadElevationOffset = v }
                let pred = Surrogate.evaluate p model.Site
                bridge.UpdateArchitecture p
                { model with Architecture = p; Prediction = pred }, Cmd.none

            | SetTreeDensity v ->
                let t = { model.Site.Topography with TreeDensity = v }
                let s = { model.Site with Topography = t }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | SetRoadDistance v ->
                let u = { model.Site.Urban with RoadDistanceM = v }
                let s = { model.Site with Urban = u }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | SetAmbientNoise v ->
                let u = { model.Site.Urban with AmbientNoiseLdenDb = v }
                let s = { model.Site with Urban = u }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | SetTrafficVolume v ->
                let u = { model.Site.Urban with TrafficVolumeAadt = v }
                let s = { model.Site with Urban = u }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | SetTransitDistance v ->
                let u = { model.Site.Urban with TransitDistanceM = v }
                let s = { model.Site with Urban = u }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | SetMaxFar v ->
                let z = { model.Site.Urban.Zoning with MaxFar = v }
                let u = { model.Site.Urban with Zoning = z }
                let s = { model.Site with Urban = u }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | UpdateRoadType rt ->
                let u = { model.Site.Urban with RoadType = rt }
                let s = { model.Site with Urban = u }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | SelectSitePreset preset ->
                let s =
                    match preset with
                    | OceanCoastline -> SiteLocation.Sausalito
                    | AlpineMountain -> SiteLocation.Chamonix
                    | InlandLake -> SiteLocation.Zurich
                    | ForestCanopy -> SiteLocation.BlackForest
                    | CustomSite -> SiteLocation.Atacama
                    | UrbanCanyon ->
                        let t = { TopographyParams.DefaultOcean with CenterElevation = 5.0; MountainHeight = 4.0; TerrainSlope = 2.0; UrbanShadows = true }
                        { Name = "Marina Bay, Singapore"; Latitude = 1.2847; Longitude = 103.8610; Elevation = 5.0; Preset = UrbanCanyon; Topography = t; Urban = UrbanContextParams.DefaultSingapore }
                    | StudioPlinth ->
                        let t = { TopographyParams.DefaultOcean with CenterElevation = 0.0; MountainHeight = 0.0; TerrainSlope = 0.0; WaterType = NoWater; TreeDensity = 0.0 }
                        { Name = "Engineering Plinth"; Latitude = 0.0; Longitude = 0.0; Elevation = 0.0; Preset = StudioPlinth; Topography = t; Urban = UrbanContextParams.DefaultSausalito }

                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred; ShowCustomPanel = (preset = CustomSite) }, Cmd.none

            | UpdateElevationExaggeration ex ->
                let t = { model.Site.Topography with ElevationExaggeration = ex }
                let s = { model.Site with Topography = t }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | RequestApplyMapSite ->
                let cmd =
                    Cmd.OfTask.perform
                        (fun () -> bridge.GetMapSiteAsync())
                        ()
                        (function
                         | Some site -> ApplySiteFromMap site
                         | None -> CloseMapModal)
                model, cmd

            | DetectCurrentLocation ->
                let cmd =
                    Cmd.OfTask.perform
                        (fun () -> bridge.DetectLocationAsync())
                        ()
                        (function
                         | Some site -> ApplySiteFromMap site
                         | None -> CloseMapModal)
                model, cmd

            | UpdateMountainHeight h ->
                let t = { model.Site.Topography with MountainHeight = h }
                let s = { model.Site with Topography = t }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | UpdateTerrainSlope sl ->
                let t = { model.Site.Topography with TerrainSlope = sl }
                let s = { model.Site with Topography = t }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | UpdateSlopeDirection dir ->
                let t = { model.Site.Topography with SlopeDirection = dir }
                let s = { model.Site with Topography = t }
                bridge.ApplySite s
                { model with Site = s }, Cmd.none

            | UpdateWaterType wt ->
                let t = { model.Site.Topography with WaterType = wt }
                let s = { model.Site with Topography = t }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | UpdateWaterProximity prox ->
                let t = { model.Site.Topography with WaterProximity = prox }
                let s = { model.Site with Topography = t }
                bridge.ApplySite s
                { model with Site = s }, Cmd.none

            | UpdateBiome biome ->
                let t = { model.Site.Topography with Biome = biome }
                let s = { model.Site with Topography = t }
                bridge.ApplySite s
                { model with Site = s }, Cmd.none

            | ApplySiteFromMap s ->
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred; ShowMapModal = false }, Cmd.none

            | OpenMapModal ->
                bridge.InitMap()
                { model with ShowMapModal = true }, Cmd.none

            | CloseMapModal ->
                { model with ShowMapModal = false }, Cmd.none

            | SelectTarget t ->
                bridge.SetMetric t
                { model with ActiveTarget = t }, Cmd.none

            | SetCameraView viewName ->
                bridge.SetCameraView viewName
                { model with ActiveCameraView = viewName }, Cmd.none

            | ToggleSpatialGraph ->
                let next = not model.ShowSpatialGraph
                bridge.ToggleSpatialGraph next
                { model with ShowSpatialGraph = next }, Cmd.none

            | ToggleWireframe ->
                let next = not model.ShowWireframe
                bridge.ToggleWireframe next
                { model with ShowWireframe = next }, Cmd.none

            | TogglePlantTreeMode ->
                let next = not model.PlantTreeMode
                bridge.TogglePlantMode next
                { model with PlantTreeMode = next }, Cmd.none

            | OnTreePlanted _ ->
                let curD = model.Site.Topography.TreeDensity
                let nextD = Math.Min(1.0, curD + 0.05)
                let t = { model.Site.Topography with TreeDensity = nextD }
                let s = { model.Site with Topography = t }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | TriggerGenerativeOptimize ->
                let best = Generative.optimize model.Architecture model.Site 500
                let pred = Surrogate.evaluate best model.Site
                bridge.UpdateArchitecture best
                { model with Architecture = best; Prediction = pred; IsOptimizing = false }, Cmd.none

            | ApplyOptimizedDesign best ->
                let pred = Surrogate.evaluate best model.Site
                bridge.UpdateArchitecture best
                { model with Architecture = best; Prediction = pred }, Cmd.none

            | ResetParameters ->
                let p = ArchitecturalParams.Default
                let pred = Surrogate.evaluate p model.Site
                bridge.UpdateArchitecture p
                { model with Architecture = p; Prediction = pred }, Cmd.none

            | ToggleDrawer drawer ->
                let next = if model.ActiveDrawer = drawer then NoDrawer else drawer
                if next = AnalyticsDrawer then
                    bridge.ResizeCharts()
                bridge.ResizeViewport()
                { model with ActiveDrawer = next }, Cmd.none

            | CloseDrawer ->
                bridge.ResizeViewport()
                { model with ActiveDrawer = NoDrawer }, Cmd.none

            | ToggleDashboard ->
                let next = not model.IsDashboardCollapsed
                bridge.ResizeCharts()
                bridge.ResizeViewport()
                { model with IsDashboardCollapsed = next }, Cmd.none

            | ToggleParameters ->
                let next = not model.IsParametersCollapsed
                bridge.ResizeViewport()
                { model with IsParametersCollapsed = next }, Cmd.none

            | ToggleTheme ->
                let next = match model.Theme with Light -> Dark | Dark -> Light
                bridge.SetTheme(next.Key)
                { model with Theme = next }, Cmd.none

            | SetTheme t ->
                bridge.SetTheme(t.Key)
                { model with Theme = t }, Cmd.none



        let view (model: Model) (dispatch: Message -> unit) =
            let p = model.Architecture
            let s = model.Site
            let pred = model.Prediction
            let aspect = (p.RoomDepth / Math.Max(0.1, p.RoomWidth))

            let isDrawerOpen = model.ActiveDrawer <> NoDrawer

            let workspaceClasses =
                [ "main-workspace"
                  if model.IsDashboardCollapsed then "dashboard-collapsed"
                  if model.IsParametersCollapsed then "params-collapsed"
                  match model.ActiveDrawer with
                  | ParametersDrawer -> "drawer-params-open"
                  | AnalyticsDrawer -> "drawer-analytics-open"
                  | CameraDrawer -> "drawer-camera-open"
                  | NoDrawer -> "" ]
                |> String.concat " "

            concat {
                Components.renderHeader model dispatch

                div {
                    attr.``class`` workspaceClasses

                    // Backdrop for mobile slide-up sheets
                    div {
                        attr.``class`` (if isDrawerOpen then "drawer-backdrop active" else "drawer-backdrop")
                        on.click (fun _ -> dispatch CloseDrawer)
                    }

                    // Left 3D Viewport Panel (Dominant Stage)
                    section {
                        attr.``class`` "viewport-panel"

                        // Three.js Canvas Mount Container (ALWAYS PERSISTENT IN DOM)
                        div { attr.id "viewport-3d" }

                        // Top Viewport HUD Camera & Toggles
                        div {
                            attr.``class`` "viewport-top-hud"

                            button {
                                attr.``class`` (if not model.IsParametersCollapsed then "hud-btn active desktop-only-btn" else "hud-btn desktop-only-btn")
                                attr.title "Toggle Parameters Drawer"
                                on.click (fun _ -> dispatch ToggleParameters)
                                "🎛️ Parameters"
                            }

                            button { attr.``class`` (if model.ActiveCameraView = "iso" then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch (SetCameraView "iso")); "Iso" }
                            button { attr.``class`` (if model.ActiveCameraView = "top" then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch (SetCameraView "top")); "Top" }
                            button { attr.``class`` (if model.ActiveCameraView = "south" then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch (SetCameraView "south")); "South" }
                            button { attr.``class`` (if model.ActiveCameraView = "east" then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch (SetCameraView "east")); "East" }
                            button { attr.``class`` (if model.ShowSpatialGraph then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch ToggleSpatialGraph); "Graph" }
                            button { attr.``class`` (if model.ShowWireframe then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch ToggleWireframe); "Wire" }

                            button {
                                attr.``class`` (if not model.IsDashboardCollapsed then "hud-btn active desktop-only-btn" else "hud-btn desktop-only-btn")
                                attr.title "Toggle Analytics Dashboard"
                                on.click (fun _ -> dispatch ToggleDashboard)
                                "📊 Analytics"
                            }
                        }

                        // Floating Live Quick-Metrics Strip Over 3D Canvas
                        Components.renderQuickMetricsBar pred s dispatch

                        // Parameters Inspector Drawer (Mobile bottom sheet or Desktop floating drawer)
                        aside {
                            attr.``class`` "parameters-drawer"

                            div {
                                attr.``class`` "sheet-header-mobile"
                                div { attr.``class`` "sheet-handle" }
                            }

                            div {
                                attr.``class`` "drawer-header"
                                div {
                                    attr.``class`` "drawer-title"
                                    "01. Massing & Program"
                                }
                                div {
                                    attr.style "display: flex; gap: 0.35rem; align-items: center;"
                                    button {
                                        attr.``class`` "btn-reset"
                                        on.click (fun _ -> dispatch ResetParameters)
                                        "Reset"
                                    }
                                    button {
                                        attr.``class`` "drawer-close-btn"
                                        attr.title "Close"
                                        on.click (fun _ ->
                                            dispatch CloseDrawer
                                            dispatch ToggleParameters)
                                        "✕"
                                    }
                                }
                            }

                            // 1. Room Depth & Width
                            Components.renderSlider "Footprint Depth" (sprintf "%.1f m" p.RoomDepth) 6.0 24.0 0.5 p.RoomDepth SetRoomDepth dispatch
                            Components.renderSlider "Footprint Width" (sprintf "%.1f m" p.RoomWidth) 6.0 24.0 0.5 p.RoomWidth SetRoomWidth dispatch

                            // Stories Slider
                            Components.renderSlider "Building Stories" (sprintf "%d Stories" p.StoryCount) 1.0 6.0 1.0 (float p.StoryCount) (fun v -> SetStoryCount (int v)) dispatch

                            // Aspect Ratio Badge
                            div {
                                attr.``class`` "param-group"
                                div {
                                    attr.``class`` "param-label-row"
                                    span { attr.``class`` "param-name"; "Aspect Ratio (D/W)" }
                                    span { attr.``class`` "param-val"; sprintf "%.2f" aspect }
                                }
                            }

                            // Pad Elevation Offset
                            Components.renderSlider "Pad Elevation Offset" (sprintf "%+.2f m" p.PadElevationOffset) -2.0 2.0 0.1 p.PadElevationOffset SetPadElevationOffset dispatch

                            // WWR & Envelope
                            Components.renderSlider "Window-to-Wall Ratio" (sprintf "%d%%" (int (p.Wwr * 100.0))) 0.15 0.85 0.05 p.Wwr SetWwr dispatch
                            Components.renderSlider "Louver Angle" (sprintf "%.0f°" p.LouverAngle) 0.0 75.0 5.0 p.LouverAngle SetLouverAngle dispatch
                            Components.renderSlider "Overhang Depth" (sprintf "%.2f m" p.OverhangDepth) 0.2 2.5 0.1 p.OverhangDepth SetOverhangDepth dispatch
                            Components.renderSlider "Solar Azimuth" (sprintf "%.0f°" p.OrientationAzimuth) -90.0 90.0 5.0 p.OrientationAzimuth SetOrientationAzimuth dispatch
                            Components.renderSlider "Glazing U-Value" (sprintf "%.1f W/m²K" p.GlazingUVal) 0.8 3.2 0.1 p.GlazingUVal SetGlazingUVal dispatch

                            // 02. Foundation & Compliance Status
                            div {
                                attr.``class`` "drawer-header"
                                attr.style "margin-top: 1.15rem;"
                                div {
                                    attr.``class`` "drawer-title"
                                    "02. Foundation & Zoning"
                                }
                            }

                            div {
                                attr.``class`` "param-group"
                                div {
                                    attr.``class`` "strategy-badge"
                                    span { attr.``class`` "strategy-label"; "Structural Foundation:" }
                                    span { attr.``class`` "strategy-value"; pred.FoundationType }
                                }
                            }

                            div {
                                attr.``class`` "param-group"
                                div {
                                    attr.``class`` "strategy-badge"
                                    span { attr.``class`` "strategy-label"; "Vehicular Access (Curb Cut):" }
                                    let curbText = if pred.CurbCutPermitted then "Permitted (Direct)" else "Restricted (Arterial Setback)"
                                    let curbColor = if pred.CurbCutPermitted then "#10b981" else "#f59e0b"
                                    span { attr.``class`` "strategy-value"; attr.style (sprintf "color: %s;" curbColor); curbText }
                                }
                            }

                            div {
                                attr.``class`` "param-group"
                                div {
                                    attr.``class`` "strategy-badge"
                                    span { attr.``class`` "strategy-label"; "Zoning Envelope Check:" }
                                    let zoneText = if pred.ZoningCompliant then "Compliant (Within FAR & Height)" else "Non-Compliant (Exceeds Max FAR)"
                                    let zoneColor = if pred.ZoningCompliant then "#10b981" else "#ef4444"
                                    span { attr.``class`` "strategy-value"; attr.style (sprintf "color: %s;" zoneColor); zoneText }
                                }
                            }

                            // 03. Topography & Environment
                            div {
                                attr.``class`` "drawer-header"
                                attr.style "margin-top: 1.15rem;"
                                div {
                                    attr.``class`` "drawer-title"
                                    "03. Real Satellite Topography"
                                }
                            }

                            // Interactive Site Location Card
                            div {
                                attr.``class`` "kpi-card"
                                attr.style "cursor: pointer; margin-bottom: 0.6rem; border: 1px solid #38bdf8;"
                                on.click (fun _ -> dispatch OpenMapModal)
                                div { attr.``class`` "kpi-title"; "Active Site Location" }
                                div { attr.``class`` "kpi-meta"; sprintf "%s" s.Name }
                                div {
                                    attr.``class`` "kpi-value-row"
                                    span { attr.``class`` "kpi-num"; sprintf "%.0f" s.Elevation }
                                    span { attr.``class`` "kpi-unit"; "m MSL" }
                                }
                                div {
                                    attr.``class`` "kpi-meta"
                                    sprintf "Lat: %.4f° | Lng: %.4f° | Slope: %.1f° (%s) | Relief: %.0f m" s.Latitude s.Longitude s.Topography.TerrainSlope s.Topography.SlopeDirection.DisplayName s.Topography.Ruggedness
                                }
                            }

                            div {
                                attr.``class`` "param-group"
                                attr.style "display: grid; grid-template-columns: 1fr 1fr; gap: 0.4rem; margin-bottom: 0.6rem;"
                                button {
                                    attr.``class`` "btn btn-primary"
                                    attr.style "justify-content: center;"
                                    on.click (fun _ -> dispatch DetectCurrentLocation)
                                    "📍 Detect Location"
                                }
                                button {
                                    attr.``class`` "btn"
                                    attr.style "justify-content: center;"
                                    on.click (fun _ -> dispatch OpenMapModal)
                                    "🗺 Open Map"
                                }
                            }

                            Components.renderCustomSitePanel model.Site dispatch

                            Components.renderSlider "Tree Canopy Density" (sprintf "%d%%" (int (s.Topography.TreeDensity * 100.0))) 0.0 1.0 0.05 s.Topography.TreeDensity SetTreeDensity dispatch

                            div {
                                attr.``class`` "param-group"
                                let plantLabel = if model.PlantTreeMode then "Planting Active" else "Plant Trees"
                                button {
                                    attr.``class`` (if model.PlantTreeMode then "btn btn-primary" else "btn")
                                    attr.style "width: 100%; justify-content: center;"
                                    on.click (fun _ -> dispatch TogglePlantTreeMode)
                                    plantLabel
                                }
                            }

                            // 04. Urban Mobility, Acoustics & Zoning
                            div {
                                attr.``class`` "drawer-header"
                                attr.style "margin-top: 1.15rem;"
                                div {
                                    attr.``class`` "drawer-title"
                                    "04. Urban Context & Zoning"
                                }
                            }

                            Components.renderUrbanContextBuilder model.Site dispatch
                        }
                    }

                    // Right Regression & Analytics Panel (Dashboard)
                    section {
                        attr.``class`` "analytics-panel"

                        // Mobile Sheet Drag Handle
                        div {
                            attr.``class`` "sheet-header-mobile"
                            div { attr.``class`` "sheet-handle" }
                        }

                        // Panel Header with Title & Close/Minimize Button
                        div {
                            attr.``class`` "analytics-panel-header"
                            div {
                                div { attr.``class`` "drawer-title"; "Surrogate Analytics & Performance" }
                                div { attr.``class`` "analytics-panel-sub"; "FastTree ML vs 3D Multi-Physics Ground Truth" }
                            }
                            button {
                                attr.``class`` "drawer-close-btn"
                                attr.title "Collapse Analytics Panel"
                                on.click (fun _ ->
                                    dispatch CloseDrawer
                                    dispatch ToggleDashboard)
                                "✕"
                            }
                        }

                        // Top 4 Primary Performance KPIs (Clickable Cards Switch Active Inspection Target)
                        div {
                            attr.``class`` "kpi-grid"
                            Components.renderKpiCard DaylightAutonomy "Daylight (sDA)" (sprintf "%.1f" pred.SdaPercent) "%" "Target: > 55%" model.ActiveTarget dispatch
                            let earthworkSign = if pred.NetEarthworkM3 > 0.0 then "+" else ""
                            Components.renderKpiCard EarthworkBalance "Net Earthwork" (sprintf "%s%.1f" earthworkSign pred.NetEarthworkM3) "m³" (sprintf "Cut: %.1f | Fill: %.1f" pred.CutVolumeM3 pred.FillVolumeM3) model.ActiveTarget dispatch
                            Components.renderKpiCard AcousticComfort "Acoustic (R'w)" (sprintf "%.0f" pred.RequiredAcousticRwDb) "dB" (sprintf "Incident: %.1f dB" pred.FacadeNoiseLdenDb) model.ActiveTarget dispatch
                            Components.renderKpiCard ZoningFar "Zoning FAR" (sprintf "%.2f" pred.FarRatio) "ratio" (sprintf "%.0f%% of Max %.1f" pred.FarUtilizationPct s.Urban.Zoning.MaxFar) model.ActiveTarget dispatch
                        }

                        // Topography & Terrain Telemetry Row (4 mini-boxes)
                        div {
                            attr.``class`` "telemetry-grid-4"
                            div {
                                attr.``class`` "telemetry-box"
                                span { attr.``class`` "telemetry-label"; "EXCAVATION CUT" }
                                span { attr.``class`` "telemetry-value"; sprintf "%.1f m³" pred.CutVolumeM3 }
                            }
                            div {
                                attr.``class`` "telemetry-box"
                                span { attr.``class`` "telemetry-label"; "EMBANKMENT FILL" }
                                span { attr.``class`` "telemetry-value"; sprintf "%.1f m³" pred.FillVolumeM3 }
                            }
                            div {
                                attr.``class`` "telemetry-box"
                                span { attr.``class`` "telemetry-label"; "HORIZON MASK" }
                                span { attr.``class`` "telemetry-value"; sprintf "%.1f°" pred.HorizonObstructionDeg }
                            }
                            div {
                                attr.``class`` "telemetry-box"
                                span { attr.``class`` "telemetry-label"; "RUNOFF (TWI)" }
                                span { attr.``class`` "telemetry-value"; sprintf "%.1f / 10" pred.RunoffRiskIndex }
                            }
                        }

                        // Urban Mobility & Demographics Telemetry Row (4 mini-boxes)
                        div {
                            attr.``class`` "telemetry-grid-4"
                            div {
                                attr.``class`` "telemetry-box"
                                span { attr.``class`` "telemetry-label"; "FACADE NOISE" }
                                span { attr.``class`` "telemetry-value"; sprintf "%.1f dB" pred.FacadeNoiseLdenDb }
                            }
                            div {
                                attr.``class`` "telemetry-box"
                                span { attr.``class`` "telemetry-label"; "TRANSIT SCORE" }
                                span { attr.``class`` "telemetry-value"; sprintf "%.0f / 100" pred.TransitConnectivityScore }
                            }
                            div {
                                attr.``class`` "telemetry-box"
                                span { attr.``class`` "telemetry-label"; "GROSS GFA" }
                                span { attr.``class`` "telemetry-value"; sprintf "%.0f m²" pred.GrossFloorAreaM2 }
                            }
                            div {
                                attr.``class`` "telemetry-box"
                                span { attr.``class`` "telemetry-label"; "OCCUPANT CAP" }
                                span { attr.``class`` "telemetry-value"; sprintf "%d Pers." pred.OccupantCapacity }
                            }
                        }

                        // Energy & Environmental Secondary Row
                        div {
                            attr.``class`` "telemetry-grid-3"
                            Components.renderKpiCard CoolingEnergy "Cooling Energy" (sprintf "%.1f" pred.CoolingLoad) "kWh/m²" "Surrogate: ±4.8 RMSE" model.ActiveTarget dispatch
                            Components.renderKpiCard StructuralStress "Structural Stress" (sprintf "%.1f" pred.PeakStress) "MPa" "Limit: 140 MPa" model.ActiveTarget dispatch
                            Components.renderKpiCard EmbodiedCarbon "Embodied Carbon" (sprintf "%.1f" pred.EmbodiedCarbon) "kgCO₂e" "A1-A3 with Acoustics" model.ActiveTarget dispatch
                        }

                        // Active Metric Header Bar with Latency Chip
                        div {
                            attr.``class`` "analytics-header-bar"
                            div {
                                attr.``class`` "active-target-badge"
                                span { attr.``class`` "active-target-label"; "Inspecting Target: " }
                                strong { attr.``class`` "active-target-name"; model.ActiveTarget.DisplayName }
                            }

                            div {
                                attr.``class`` "latency-chip"
                                span { sprintf "Surrogate: %.2f ms [F# WebAssembly]" pred.LatencyMs }
                            }
                        }

                        // Charts Mount Points
                        div {
                            attr.``class`` "charts-grid"
                            div {
                                attr.``class`` "chart-card"
                                div {
                                    attr.``class`` "chart-card-header"
                                    div { attr.``class`` "chart-title"; "Parity Plot: Physical Simulation (y) vs. F# Surrogate (ŷ)" }
                                    span { attr.``class`` "chart-badge"; "R² = 0.964 | 160 Samples" }
                                }
                                div { attr.id "chart-parity"; attr.``class`` "chart-viewport"; attr.style "min-height: 220px;" }
                            }

                            div {
                                attr.``class`` "charts-row-split"
                                div {
                                    attr.``class`` "chart-card"
                                    div {
                                        attr.``class`` "chart-card-header"
                                        div { attr.``class`` "chart-title"; "SHAP Feature Importance" }
                                        span { attr.``class`` "chart-badge"; "Mean |SHAP|" }
                                    }
                                    div { attr.id "chart-shap"; attr.``class`` "chart-viewport"; attr.style "min-height: 200px;" }
                                }
                                div {
                                    attr.``class`` "chart-card"
                                    div {
                                        attr.``class`` "chart-card-header"
                                        div { attr.``class`` "chart-title"; "Residual Distribution" }
                                        span { attr.``class`` "chart-badge"; "Centered at 0.0" }
                                    }
                                    div { attr.id "chart-residual"; attr.``class`` "chart-viewport"; attr.style "min-height: 200px;" }
                                }
                            }
                        }
                    }

                    // Mobile Camera Sheet
                    Components.renderCameraSheet model dispatch

                    // Mobile Bottom Action Dock
                    Components.renderMobileDock model dispatch
                }

                // Map Modal
                Components.renderMapModal model dispatch

                // Bottom Status Bar
                footer {
                    attr.``class`` "app-status-bar"
                    div {
                        attr.``class`` "status-left"
                        div { attr.``class`` "status-item"; span { "Engine: "; strong { "F# Bolero WebAssembly" } } }
                        div { attr.``class`` "status-item"; span { "Site: "; strong { s.Name } } }
                        div { attr.``class`` "status-item desktop-only"; span { "Zoning FAR: "; strong { sprintf "%.2f (Max %.1f)" pred.FarRatio s.Urban.Zoning.MaxFar } } }
                        div { attr.``class`` "status-item desktop-only"; span { "Noise: "; strong { sprintf "%.1f dB (R'w %.0f dB)" pred.FacadeNoiseLdenDb pred.RequiredAcousticRwDb } } }
                    }
                    div {
                        attr.``class`` "status-right"
                        div { attr.``class`` "status-item"; span { "Transit: "; strong { sprintf "%.0f/100" pred.TransitConnectivityScore } } }
                    }
                }
            }


        Program.mkProgram init update view
