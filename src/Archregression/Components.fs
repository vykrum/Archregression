namespace Archregression

open System
open Bolero
open Bolero.Html

/// Utilitarian Bolero HTML Components for Archregression
module Components =

    /// Clean CAD Navigation Header
    let renderHeader (model: Model) (dispatch: Message -> unit) =
        header {
            attr.``class`` "app-header"
            div {
                attr.``class`` "brand-section"
                div { attr.``class`` "brand-logo"; "ARCH" }
                div {
                    attr.``class`` "brand-text"
                    h1 { "Archregression CAD" }
                    p { attr.``class`` "desktop-only"; "Satellite DEM Topography & Generative Architectural Surrogates" }
                }
            }
            div {
                attr.``class`` "header-status desktop-only"
                span { sprintf "%s | %.4f°N, %.4f°E | Elev: %.0f m MSL | Slope: %.1f° (%s)" model.Site.Name model.Site.Latitude model.Site.Longitude model.Site.Elevation model.Site.Topography.TerrainSlope model.Site.Topography.SlopeDirection.DisplayName }
            }
            div {
                attr.``class`` "header-actions"
                button {
                    attr.``class`` "btn desktop-only"
                    on.click (fun _ -> dispatch OpenMapModal)
                    "Map Location"
                }
                let optLabel = if model.IsOptimizing then "Optimizing..." else "Synthesize"
                button {
                    attr.``class`` "btn btn-primary"
                    on.click (fun _ -> dispatch TriggerGenerativeOptimize)
                    optLabel
                }
                button {
                    attr.``class`` (if not model.IsDashboardCollapsed then "btn btn-hud-toggle active desktop-only" else "btn btn-hud-toggle desktop-only")
                    on.click (fun _ -> dispatch ToggleDashboard)
                    if model.IsDashboardCollapsed then "📊 Analytics" else "📊 Hide Analytics"
                }
                button {
                    attr.``class`` "btn desktop-only"
                    on.click (fun _ -> ())
                    attr.id "btn-export-glb"
                    "Export GLB"
                }
                button {
                    attr.``class`` "btn btn-theme-toggle"
                    attr.title "Toggle Light / Dark mode"
                    on.click (fun _ -> dispatch ToggleTheme)
                    match model.Theme with
                    | Light -> "🌙 Dark"
                    | Dark -> "☀️ Light"
                }
                a {
                    attr.``class`` "btn desktop-only"
                    attr.href "https://huggingface.co/datasets/vykrum/arch-regression-dataset"
                    attr.target "_blank"
                    "Dataset"
                }
            }
        }


    /// Technical Parameter Slider Row
    let renderSlider (label: string) (displayVal: string) (minVal: float) (maxVal: float) (stepVal: float) (currentVal: float) (msgCtor: float -> Message) (dispatch: Message -> unit) =
        div {
            attr.``class`` "param-group"
            div {
                attr.``class`` "param-header"
                span { attr.``class`` "param-name"; label }
                span { attr.``class`` "param-val"; displayVal }
            }
            input {
                attr.``type`` "range"
                attr.``class`` "range-slider"
                attr.min (sprintf "%f" minVal)
                attr.max (sprintf "%f" maxVal)
                attr.step (sprintf "%f" stepVal)
                attr.value (sprintf "%f" currentVal)
                on.input (fun e ->
                    match Double.TryParse(e.Value :?> string) with
                    | true, v -> dispatch (msgCtor v)
                    | _ -> ())
            }
        }

    /// Telemetry & KPI Metric Card (Clickable to switch active target)
    let renderKpiCard (target: SimulationTarget) (title: string) (valueStr: string) (unitStr: string) (noteStr: string) (activeTarget: SimulationTarget) (dispatch: Message -> unit) =
        let isSelected = activeTarget = target
        let cardClass = if isSelected then "kpi-card active" else "kpi-card"
        div {
            attr.``class`` cardClass
            on.click (fun _ -> dispatch (SelectTarget target))
            div { attr.``class`` "kpi-title"; title }
            div {
                attr.``class`` "kpi-value-row"
                span { attr.``class`` "kpi-num"; valueStr }
                span { attr.``class`` "kpi-unit"; unitStr }
            }
            div { attr.``class`` "kpi-meta"; noteStr }
        }

    /// Clean Topography Parameters Panel
    let renderCustomTopographyBuilder (site: SiteLocation) (dispatch: Message -> unit) =
        let topo = site.Topography
        div {
            attr.id "custom-site-panel"
            attr.``class`` "custom-site-builder"

            div {
                attr.``class`` "panel-subheader"
                span { "Satellite Topography & Exaggeration" }
            }

            renderSlider "Terrain Vertical Scale" (sprintf "%.1fx" topo.ElevationExaggeration) 1.0 3.0 0.2 topo.ElevationExaggeration UpdateElevationExaggeration dispatch
            renderSlider "Local Relief Span" (sprintf "%.0f m" topo.Ruggedness) 0.0 600.0 10.0 topo.Ruggedness UpdateMountainHeight dispatch
            renderSlider "Terrain Slope" (sprintf "%.1f°" topo.TerrainSlope) 0.0 45.0 1.0 topo.TerrainSlope UpdateTerrainSlope dispatch

            div {
                attr.``class`` "param-group"
                span { attr.``class`` "param-name"; "Downhill Aspect" }
                select {
                    attr.``class`` "site-preset-select"
                    attr.value topo.SlopeDirection.Key
                    on.change (fun e -> dispatch (UpdateSlopeDirection (SlopeAspect.FromKey (e.Value :?> string))))
                    option { attr.value "south"; "South (Solar Exposed)" }
                    option { attr.value "north"; "North (Topographic Shadow)" }
                    option { attr.value "east"; "East (Morning Sun)" }
                    option { attr.value "west"; "West (Afternoon Sun)" }
                }
            }

            div {
                attr.``class`` "param-group"
                span { attr.``class`` "param-name"; "Water Feature" }
                select {
                    attr.``class`` "site-preset-select"
                    attr.value topo.WaterType.Key
                    on.change (fun e -> dispatch (UpdateWaterType (WaterBodyType.FromKey (e.Value :?> string))))
                    option { attr.value "ocean"; "Ocean Coast" }
                    option { attr.value "lake"; "Inland Lake" }
                    option { attr.value "none"; "None (Inland Ridge)" }
                }
            }

            renderSlider "Water Setback" (sprintf "%.0f m" topo.WaterProximity) 12.0 99.0 2.0 topo.WaterProximity UpdateWaterProximity dispatch

            div {
                attr.``class`` "param-group"
                span { attr.``class`` "param-name"; "Vegetation Biome" }
                select {
                    attr.``class`` "site-preset-select"
                    attr.value topo.Biome.Key
                    on.change (fun e -> dispatch (UpdateBiome (VegetationBiome.FromKey (e.Value :?> string))))
                    option { attr.value "coastal"; "Coastal Cypress" }
                    option { attr.value "alpine"; "Alpine Conifers" }
                    option { attr.value "temperate"; "Temperate Mixed" }
                }
            }
        }

    /// Urban Context & Mobility Parameters Panel
    let renderUrbanContextBuilder (site: SiteLocation) (dispatch: Message -> unit) =
        let u = site.Urban
        div {
            attr.``class`` "custom-site-builder"
            attr.style "margin-top: 0.75rem;"

            div {
                attr.``class`` "panel-subheader"
                span { "Mobility, Acoustics & Zoning" }
            }

            div {
                attr.style "background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 4px; padding: 7px 10px; margin-bottom: 0.75rem; font-size: 11px; display: flex; justify-content: space-between; align-items: center;"
                div {
                    attr.style "color: #1e293b; font-weight: 600;"
                    sprintf "Nearest Road: %s" u.RoadName
                }
                span {
                    attr.style "background: #e2e8f0; color: #0284c7; padding: 2px 7px; border-radius: 4px; font-weight: 700; font-size: 10px; letter-spacing: 0.3px;"
                    sprintf "%.1f m (%s)" u.RoadDistanceM (u.RoadType.Key.ToUpperInvariant())
                }
            }

            div {
                attr.``class`` "param-group"
                span { attr.``class`` "param-name"; "Road Network Hierarchy" }
                select {
                    attr.``class`` "site-preset-select"
                    attr.value u.RoadType.Key
                    on.change (fun e -> dispatch (UpdateRoadType (RoadClassification.FromKey (e.Value :?> string))))
                    option { attr.value "local"; "Local Street (30 km/h)" }
                    option { attr.value "collector"; "Collector Road (50 km/h)" }
                    option { attr.value "arterial"; "Arterial Highway (80 km/h)" }
                }
            }

            renderSlider "Road Setback Distance" (sprintf "%.0f m" u.RoadDistanceM) 6.0 100.0 2.0 u.RoadDistanceM SetRoadDistance dispatch
            renderSlider "Traffic Volume (AADT)" (sprintf "%d vpd" u.TrafficVolumeAadt) 500.0 40000.0 500.0 (float u.TrafficVolumeAadt) (fun v -> SetTrafficVolume (int v)) dispatch
            renderSlider "Ambient Noise (Lden)" (sprintf "%.1f dB" u.AmbientNoiseLdenDb) 35.0 82.0 1.0 u.AmbientNoiseLdenDb SetAmbientNoise dispatch
            renderSlider "Transit Distance" (sprintf "%.0f m" u.TransitDistanceM) 30.0 1500.0 20.0 u.TransitDistanceM SetTransitDistance dispatch
            renderSlider "Maximum Allowed FAR" (sprintf "%.1f" u.Zoning.MaxFar) 0.6 6.0 0.2 u.Zoning.MaxFar SetMaxFar dispatch
        }

    let renderCustomSitePanel = renderCustomTopographyBuilder

    /// Geographic Site Selection Modal (Satellite Topography)
    let renderMapModal (model: Model) (dispatch: Message -> unit) =
        div {
            attr.``class`` (if model.ShowMapModal then "modal-overlay active" else "modal-overlay")
            attr.id "modal-map"
            div {
                attr.``class`` "modal-dialog"
                div {
                    attr.``class`` "modal-header"
                    div {
                        attr.``class`` "modal-title"
                        "Geographic Site Selection & Real Satellite DEM Topography"
                    }
                    button {
                        attr.``class`` "modal-close-btn"
                        on.click (fun _ -> dispatch CloseMapModal)
                        "×"
                    }
                }
                div {
                    attr.``class`` "modal-body"
                    div {
                        attr.``class`` "map-search-toolbar"
                        div {
                            attr.``class`` "map-search-bar"
                            span { attr.``class`` "map-search-icon"; "🔍" }
                            input {
                                attr.id "map-search-input"
                                attr.``class`` "map-search-input"
                                attr.``type`` "text"
                                attr.placeholder "Search city, address, landmark, or lat, lng (e.g. Zurich, Tokyo, 47.37, 8.54)..."
                            }
                            button {
                                attr.id "map-search-clear"
                                attr.``class`` "map-search-clear-btn"
                                attr.``type`` "button"
                                attr.title "Clear search"
                                "✕"
                            }
                            button {
                                attr.id "map-search-btn"
                                attr.``class`` "btn btn-primary map-search-submit-btn"
                                attr.``type`` "button"
                                "Search"
                            }
                        }
                        button {
                            attr.id "map-locate-btn"
                            attr.``class`` "btn map-locate-btn"
                            attr.``type`` "button"
                            attr.title "Locate via GPS or IP"
                            "📍 Current Location"
                        }
                    }
                    div {
                        attr.id "map-search-results"
                        attr.``class`` "map-search-results"
                    }
                    div {
                        attr.``class`` "map-preset-chips"
                        span { attr.``class`` "map-chips-label"; "Quick Presets:" }
                        button { attr.id "preset-zurich"; attr.``class`` "map-preset-chip"; attr.``type`` "button"; "🏔️ Zurich" }
                        button { attr.id "preset-sf"; attr.``class`` "map-preset-chip"; attr.``type`` "button"; "🌉 San Francisco" }
                        button { attr.id "preset-tokyo"; attr.``class`` "map-preset-chip"; attr.``type`` "button"; "🗼 Tokyo" }
                        button { attr.id "preset-stmoritz"; attr.``class`` "map-preset-chip"; attr.``type`` "button"; "⛷️ St. Moritz" }
                        button { attr.id "preset-reykjavik"; attr.``class`` "map-preset-chip"; attr.``type`` "button"; "🌋 Reykjavik" }
                        button { attr.id "preset-amalfi"; attr.``class`` "map-preset-chip"; attr.``type`` "button"; "🌊 Amalfi" }
                    }
                    div {
                        attr.id "map-coords-readout"
                        attr.``class`` "map-coords-readout"
                        "Click anywhere on map, search a location, or reposition marker to query real satellite DEM elevation grid."
                    }
                    div {
                        attr.id "map-picker-container"
                        attr.``class`` "map-picker-container"
                    }
                    div {
                        attr.``class`` "modal-actions"
                        button {
                            attr.``class`` "btn"
                            on.click (fun _ -> dispatch CloseMapModal)
                            "Cancel"
                        }
                        button {
                            attr.``class`` "btn btn-primary"
                            on.click (fun _ -> dispatch RequestApplyMapSite)
                            "Apply Real Site Topography"
                        }
                    }
                }
            }
        }

    /// Floating Live Quick-Metrics Strip on 3D Viewport (Provides live feedback without dominating screen)
    let renderQuickMetricsBar (pred: SurrogatePredictions) (site: SiteLocation) (dispatch: Message -> unit) =
        div {
            attr.``class`` "viewport-quick-metrics"
            attr.title "Live surrogate metrics — Tap to open full analytics dashboard"
            on.click (fun _ -> dispatch (ToggleDrawer AnalyticsDrawer))

            div {
                attr.``class`` "quick-metric-pill"
                span { attr.``class`` "qm-label"; "sDA" }
                span { attr.``class`` "qm-val"; sprintf "%.1f%%" pred.SdaPercent }
            }
            div {
                attr.``class`` "quick-metric-pill"
                span { attr.``class`` "qm-label"; "EARTHWORK" }
                let sign = if pred.NetEarthworkM3 > 0.0 then "+" else ""
                span { attr.``class`` "qm-val"; sprintf "%s%.0f m³" sign pred.NetEarthworkM3 }
            }
            div {
                attr.``class`` "quick-metric-pill"
                span { attr.``class`` "qm-label"; "NOISE" }
                span { attr.``class`` "qm-val"; sprintf "%.0f dB" pred.FacadeNoiseLdenDb }
            }
            div {
                attr.``class`` "quick-metric-pill"
                span { attr.``class`` "qm-label"; "FAR" }
                span { attr.``class`` "qm-val"; sprintf "%.2f" pred.FarRatio }
            }
            div {
                attr.``class`` "quick-metric-pill qm-latency"
                span { sprintf "%.2f ms" pred.LatencyMs }
            }
            div {
                attr.``class`` "qm-expand-icon"
                "📊"
            }
        }

    /// Camera Views & Visual Overlays Sheet for Mobile
    let renderCameraSheet (model: Model) (dispatch: Message -> unit) =
        div {
            attr.``class`` (if model.ActiveDrawer = CameraDrawer then "camera-sheet active" else "camera-sheet")
            div {
                attr.``class`` "sheet-header-mobile"
                div { attr.``class`` "sheet-handle" }
            }
            div {
                attr.``class`` "drawer-header"
                div { attr.``class`` "drawer-title"; "Camera Views & 3D Overlays" }
                button {
                    attr.``class`` "drawer-close-btn"
                    on.click (fun _ -> dispatch CloseDrawer)
                    "✕"
                }
            }
            div {
                attr.``class`` "camera-sheet-body"
                div { attr.``class`` "camera-sheet-subtitle"; "CAMERA PRESETS" }
                div {
                    attr.``class`` "camera-btn-grid"
                    button {
                        attr.``class`` (if model.ActiveCameraView = "iso" then "btn btn-primary" else "btn")
                        on.click (fun _ ->
                            dispatch (SetCameraView "iso")
                            dispatch CloseDrawer)
                        "Isometric"
                    }
                    button {
                        attr.``class`` (if model.ActiveCameraView = "top" then "btn btn-primary" else "btn")
                        on.click (fun _ ->
                            dispatch (SetCameraView "top")
                            dispatch CloseDrawer)
                        "Top Plan"
                    }
                    button {
                        attr.``class`` (if model.ActiveCameraView = "south" then "btn btn-primary" else "btn")
                        on.click (fun _ ->
                            dispatch (SetCameraView "south")
                            dispatch CloseDrawer)
                        "South Elev."
                    }
                    button {
                        attr.``class`` (if model.ActiveCameraView = "east" then "btn btn-primary" else "btn")
                        on.click (fun _ ->
                            dispatch (SetCameraView "east")
                            dispatch CloseDrawer)
                        "East Elev."
                    }
                }
                div { attr.``class`` "camera-sheet-subtitle"; attr.style "margin-top: 0.85rem;"; "3D OVERLAYS & TOOLS" }
                div {
                    attr.``class`` "camera-btn-grid"
                    button {
                        attr.``class`` (if model.ShowSpatialGraph then "btn btn-primary" else "btn")
                        on.click (fun _ -> dispatch ToggleSpatialGraph)
                        if model.ShowSpatialGraph then "Graph: ON" else "Graph: OFF"
                    }
                    button {
                        attr.``class`` (if model.ShowWireframe then "btn btn-primary" else "btn")
                        on.click (fun _ -> dispatch ToggleWireframe)
                        if model.ShowWireframe then "Wire: ON" else "Wire: OFF"
                    }
                }
                div { attr.``class`` "camera-sheet-subtitle"; attr.style "margin-top: 0.85rem;"; "INTERACTIVE VEGETATION" }
                button {
                    attr.``class`` (if model.PlantTreeMode then "btn btn-primary" else "btn")
                    attr.style "width: 100%; justify-content: center;"
                    on.click (fun _ ->
                        dispatch TogglePlantTreeMode
                        dispatch CloseDrawer)
                    if model.PlantTreeMode then "🌳 Planting Active (Tap Ground)" else "🌳 Plant Tree Mode"
                }
                div { attr.``class`` "camera-sheet-subtitle"; attr.style "margin-top: 0.85rem;"; "STUDIO APPEARANCE" }
                button {
                    attr.``class`` "btn"
                    attr.style "width: 100%; justify-content: center;"
                    on.click (fun _ -> dispatch ToggleTheme)
                    match model.Theme with
                    | Light -> "🌙 Switch to Dark Mode"
                    | Dark -> "☀️ Switch to Light Mode"
                }
            }
        }

    /// Mobile Bottom Action Dock (Sticky tab navigation on small screens)
    let renderMobileDock (model: Model) (dispatch: Message -> unit) =
        nav {
            attr.``class`` "mobile-bottom-dock"
            button {
                attr.``class`` (if model.ActiveDrawer = ParametersDrawer then "dock-btn active" else "dock-btn")
                on.click (fun _ -> dispatch (ToggleDrawer ParametersDrawer))
                span { attr.``class`` "dock-icon"; "🎛️" }
                span { attr.``class`` "dock-text"; "Params" }
            }
            button {
                attr.``class`` (if model.ActiveDrawer = AnalyticsDrawer then "dock-btn active" else "dock-btn")
                on.click (fun _ -> dispatch (ToggleDrawer AnalyticsDrawer))
                span { attr.``class`` "dock-icon"; "📊" }
                span { attr.``class`` "dock-text"; "Metrics" }
            }
            button {
                attr.``class`` (if model.ActiveDrawer = CameraDrawer then "dock-btn active" else "dock-btn")
                on.click (fun _ -> dispatch (ToggleDrawer CameraDrawer))
                span { attr.``class`` "dock-icon"; "🧭" }
                span { attr.``class`` "dock-text"; "Views" }
            }
            button {
                attr.``class`` "dock-btn"
                on.click (fun _ -> dispatch OpenMapModal)
                span { attr.``class`` "dock-icon"; "🗺️" }
                span { attr.``class`` "dock-text"; "Map" }
            }
            button {
                attr.``class`` (if model.IsOptimizing then "dock-btn dock-btn-primary optimizing" else "dock-btn dock-btn-primary")
                on.click (fun _ -> dispatch TriggerGenerativeOptimize)
                span { attr.``class`` "dock-icon"; "⚡" }
                span { attr.``class`` "dock-text"; if model.IsOptimizing then "..." else "Synthesize" }
            }
        }

