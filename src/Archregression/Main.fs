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
                  IsOptimizing = false }

            initialModel, Cmd.none

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

            | SetTreeDensity v ->
                let t = { model.Site.Topography with TreeDensity = v }
                let s = { model.Site with Topography = t }
                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred }, Cmd.none

            | SelectSitePreset preset ->
                let s =
                    match preset with
                    | OceanCoastline -> SiteLocation.Sausalito
                    | AlpineMountain -> SiteLocation.Chamonix
                    | InlandLake -> SiteLocation.Zurich
                    | ForestCanopy ->
                        let t = { TopographyParams.DefaultLake with MountainHeight = 16.0; WaterType = NoWater; TreeDensity = 0.85; Biome = TemperateCanopy }
                        { Name = "Black Forest Canopy & Ridge, Germany"; Latitude = 48.1833; Longitude = 8.1667; Elevation = 780.0; Preset = ForestCanopy; Topography = t }
                    | UrbanCanyon ->
                        let t = { TopographyParams.DefaultOcean with MountainHeight = 4.0; TerrainSlope = 2.0; UrbanShadows = true }
                        { Name = "Marina Bay Waterfront, Singapore"; Latitude = 1.2847; Longitude = 103.8610; Elevation = 5.0; Preset = UrbanCanyon; Topography = t }
                    | StudioPlinth ->
                        let t = { TopographyParams.DefaultOcean with MountainHeight = 0.0; TerrainSlope = 0.0; WaterType = NoWater; TreeDensity = 0.0 }
                        { Name = "Minimal Engineering Plinth"; Latitude = 0.0; Longitude = 0.0; Elevation = 0.0; Preset = StudioPlinth; Topography = t }
                    | CustomSite ->
                        { model.Site with Preset = CustomSite }

                let pred = Surrogate.evaluate model.Architecture s
                bridge.ApplySite s
                { model with Site = s; Prediction = pred; ShowCustomPanel = (preset = CustomSite) }, Cmd.none

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

        let view (model: Model) (dispatch: Message -> unit) =
            let p = model.Architecture
            let s = model.Site
            let pred = model.Prediction
            let aspect = (p.RoomDepth / Math.Max(0.1, p.RoomWidth))

            concat {
                Components.renderHeader model dispatch

                div {
                    attr.``class`` "main-workspace"

                    // Left 3D Viewport Panel
                    section {
                        attr.``class`` "viewport-panel"

                        // Three.js Canvas Mount Container
                        div { attr.id "viewport-3d" }

                        // Left Floating Parameters Drawer
                        aside {
                            attr.``class`` "parameters-drawer"

                            div {
                                attr.``class`` "drawer-header"
                                div {
                                    attr.``class`` "drawer-title"
                                    span { "📐" }
                                    " Envelope Parameters"
                                }
                                button {
                                    attr.``class`` "btn-reset"
                                    on.click (fun _ -> dispatch ResetParameters)
                                    "Reset"
                                }
                            }

                            // 1. Room Depth
                            Components.renderSlider "Room Depth (North-South)" (sprintf "%.1f m" p.RoomDepth) 6.0 24.0 0.5 p.RoomDepth SetRoomDepth dispatch

                            // 2. Room Width
                            Components.renderSlider "Room Width (East-West)" (sprintf "%.1f m" p.RoomWidth) 6.0 24.0 0.5 p.RoomWidth SetRoomWidth dispatch

                            // Aspect Ratio Badge
                            div {
                                attr.``class`` "param-group"
                                div {
                                    attr.``class`` "param-label-row"
                                    span { attr.``class`` "param-name"; "Aspect Ratio (D/W)" }
                                    span { attr.``class`` "param-val"; sprintf "%.2f" aspect }
                                }
                            }

                            // 3. WWR
                            Components.renderSlider "Window-to-Wall Ratio" (sprintf "%d%%" (int (p.Wwr * 100.0))) 0.15 0.85 0.05 p.Wwr SetWwr dispatch

                            // 4. Louver Angle
                            Components.renderSlider "Louver Angle (Tilt)" (sprintf "%.0f°" p.LouverAngle) 0.0 75.0 5.0 p.LouverAngle SetLouverAngle dispatch

                            // 5. Overhang Depth
                            Components.renderSlider "Solar Overhang Depth" (sprintf "%.2f m" p.OverhangDepth) 0.2 2.5 0.1 p.OverhangDepth SetOverhangDepth dispatch

                            // 6. Solar Azimuth
                            Components.renderSlider "Solar Azimuth (S=0°)" (sprintf "%.0f°" p.OrientationAzimuth) -90.0 90.0 5.0 p.OrientationAzimuth SetOrientationAzimuth dispatch

                            // 7. Glazing U-Value
                            Components.renderSlider "Glazing U-Value" (sprintf "%.1f W/m²K" p.GlazingUVal) 0.8 3.2 0.1 p.GlazingUVal SetGlazingUVal dispatch

                            // Natural Site Context Header
                            div {
                                attr.``class`` "drawer-header"
                                attr.style "margin-top: 1.25rem;"
                                div {
                                    attr.``class`` "drawer-title"
                                    span { "🌿" }
                                    " Natural Site Context"
                                }
                            }

                            div {
                                attr.``class`` "param-group"
                                span { attr.``class`` "param-name"; "Site Environment Typology" }
                                select {
                                    attr.``class`` "site-preset-select"
                                    attr.value s.Preset.Key
                                    on.change (fun e -> dispatch (SelectSitePreset (SitePreset.FromKey (e.Value :?> string))))
                                    option { attr.value "ocean"; "🌊 Ocean Shoreline & Coastal Bluff" }
                                    option { attr.value "mountain"; "⛰️ Alpine Mountain Ridge & Peaks" }
                                    option { attr.value "lake"; "🏞️ Inland Freshwater Lakefront" }
                                    option { attr.value "forest"; "🌲 Forested Tree Canopy" }
                                    option { attr.value "urban"; "🏢 Dense Urban Canyon" }
                                    option { attr.value "studio"; "🏛️ Minimal Studio Plinth" }
                                    option { attr.value "custom"; "🛠️ Custom Site (Interactive Sliders)" }
                                }
                            }

                            // Custom Site Builder Panel
                            cond (model.ShowCustomPanel || s.Preset = CustomSite) <| function
                                | true -> Components.renderCustomSitePanel model dispatch
                                | false -> empty ()

                            // Tree Canopy Density
                            Components.renderSlider "Tree Canopy Density" (sprintf "%d%%" (int (s.Topography.TreeDensity * 100.0))) 0.0 1.0 0.05 s.Topography.TreeDensity SetTreeDensity dispatch

                            // Plant Trees Tool Button
                            div {
                                attr.``class`` "param-group"
                                let plantLabel = if model.PlantTreeMode then " Planting Active (Click Ground)" else " Plant Trees on Click"
                                button {
                                    attr.``class`` (if model.PlantTreeMode then "btn btn-primary" else "btn")
                                    attr.style "width: 100%; justify-content: center;"
                                    on.click (fun _ -> dispatch TogglePlantTreeMode)
                                    span { "🌲" }
                                    plantLabel
                                }
                            }
                        }

                        // Top HUD Camera & Toggles
                        div {
                            attr.``class`` "viewport-top-hud"
                            button { attr.``class`` (if model.ActiveCameraView = "iso" then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch (SetCameraView "iso")); "Iso" }
                            button { attr.``class`` (if model.ActiveCameraView = "top" then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch (SetCameraView "top")); "Top" }
                            button { attr.``class`` (if model.ActiveCameraView = "south" then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch (SetCameraView "south")); "South" }
                            button { attr.``class`` (if model.ActiveCameraView = "east" then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch (SetCameraView "east")); "East" }
                            button { attr.``class`` (if model.ShowSpatialGraph then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch ToggleSpatialGraph); "🕸️ Graph" }
                            button { attr.``class`` (if model.ShowWireframe then "hud-btn active" else "hud-btn"); on.click (fun _ -> dispatch ToggleWireframe); "📐 Wire" }
                        }
                    }

                    // Right Regression & Analytics Panel
                    section {
                        attr.``class`` "analytics-panel"

                        // Top Performance KPI Grid
                        div {
                            attr.``class`` "kpi-grid"
                            Components.renderKpiCard DaylightAutonomy "Daylight Autonomy (sDA)" (sprintf "%.1f" pred.SdaPercent) "%" "Target: > 55% (LEED v4)" model.ActiveTarget dispatch
                            Components.renderKpiCard CoolingEnergy "Annual Cooling Energy" (sprintf "%.1f" pred.CoolingLoad) "kWh/m²" "Surrogate: ±4.8 RMSE" model.ActiveTarget dispatch
                            Components.renderKpiCard StructuralStress "Peak Structural Stress" (sprintf "%.1f" pred.PeakStress) "MPa" "Capacity: 120 MPa" model.ActiveTarget dispatch
                            Components.renderKpiCard EmbodiedCarbon "Embodied Carbon" (sprintf "%.1f" pred.EmbodiedCarbon) "kgCO₂e" "A1-A3 Cradle-to-Gate" model.ActiveTarget dispatch
                        }

                        // Environmental Context Row
                        div {
                            attr.style "display: grid; grid-template-columns: repeat(3, 1fr); gap: 0.65rem; margin-bottom: 0.85rem;"
                            Components.renderKpiCard MicroclimateCooling "Microclimate Cooling" (sprintf "-%.1f" pred.MicroclimateDeltaT) "°C" "Tree Shade & Breeze" model.ActiveTarget dispatch
                            Components.renderKpiCard BiophilicGreenView "Biophilic Green View" (sprintf "%.0f" pred.BiophilicViewPct) "%" "Sightline Vegetation" model.ActiveTarget dispatch
                            Components.renderKpiCard TreeCarbonOffset "Tree Biomass Offset" (sprintf "%d" pred.TreeCarbonOffset) "kg/yr" "Annual Carbon Capture" model.ActiveTarget dispatch
                        }

                        // Analytics Header Bar with Target Tabs and Latency Chip
                        div {
                            attr.``class`` "analytics-header-bar"
                            div {
                                attr.``class`` "target-tabs"
                                button { attr.``class`` (if model.ActiveTarget = DaylightAutonomy then "target-tab active" else "target-tab"); on.click (fun _ -> dispatch (SelectTarget DaylightAutonomy)); "Daylight (sDA)" }
                                button { attr.``class`` (if model.ActiveTarget = CoolingEnergy then "target-tab active" else "target-tab"); on.click (fun _ -> dispatch (SelectTarget CoolingEnergy)); "Cooling Energy" }
                                button { attr.``class`` (if model.ActiveTarget = StructuralStress then "target-tab active" else "target-tab"); on.click (fun _ -> dispatch (SelectTarget StructuralStress)); "Structural Stress" }
                                button { attr.``class`` (if model.ActiveTarget = BiophilicGreenView then "target-tab active" else "target-tab"); on.click (fun _ -> dispatch (SelectTarget BiophilicGreenView)); "Biophilic View" }
                                button { attr.``class`` (if model.ActiveTarget = MicroclimateCooling then "target-tab active" else "target-tab"); on.click (fun _ -> dispatch (SelectTarget MicroclimateCooling)); "Microclimate (ΔT)" }
                            }

                            div {
                                attr.``class`` "latency-chip"
                                span { attr.``class`` "pulse-dot" }
                                span { sprintf "Inference: %.2f ms (F# WebAssembly)" pred.LatencyMs }
                            }
                        }

                        // Charts Mount Points
                        div {
                            attr.``class`` "charts-grid"
                            div {
                                attr.``class`` "chart-card"
                                div {
                                    attr.``class`` "chart-card-header"
                                    div { attr.``class`` "chart-title"; span { "🎯" }; " Parity Plot: Actual Simulation (y) vs. F# Surrogate (ŷ)" }
                                    span { attr.``class`` "chart-badge"; "R² = 0.964 | 160 Samples" }
                                }
                                div { attr.id "chart-parity"; attr.``class`` "chart-viewport"; attr.style "min-height: 240px;" }
                            }

                            div {
                                attr.``class`` "charts-row-split"
                                div {
                                    attr.``class`` "chart-card"
                                    div {
                                        attr.``class`` "chart-card-header"
                                        div { attr.``class`` "chart-title"; span { "⚡" }; " SHAP Feature Importance" }
                                        span { attr.``class`` "chart-badge"; "Mean |SHAP|" }
                                    }
                                    div { attr.id "chart-shap"; attr.``class`` "chart-viewport"; attr.style "min-height: 220px;" }
                                }
                                div {
                                    attr.``class`` "chart-card"
                                    div {
                                        attr.``class`` "chart-card-header"
                                        div { attr.``class`` "chart-title"; span { "📉" }; " Residual Distribution" }
                                        span { attr.``class`` "chart-badge"; "Centered at 0.0" }
                                    }
                                    div { attr.id "chart-residual"; attr.``class`` "chart-viewport"; attr.style "min-height: 220px;" }
                                }
                            }
                        }
                    }
                }

                // Bottom Status Bar
                footer {
                    attr.``class`` "app-status-bar"
                    div {
                        attr.``class`` "status-left"
                        div { attr.``class`` "status-item"; span { attr.style "color: #38bdf8;"; "●" }; span { "Engine: "; strong { "F# Bolero WebAssembly (.NET 10)" } } }
                        div { attr.``class`` "status-item"; span { attr.style "color: #10b981;"; "●" }; span { "Site Context: "; strong { s.Name } } }
                        div { attr.``class`` "status-item"; span { attr.style "color: #a855f7;"; "●" }; span { "Mesh Format: "; strong { "glTF 2.0 (Khronos)" } } }
                    }
                    div {
                        attr.``class`` "status-right"
                        div { attr.``class`` "status-item"; span { "Architecture: "; strong { "Pure F# MVU + Zero-License Browser Client" } } }
                    }
                }
            }

        Program.mkProgram init update view
