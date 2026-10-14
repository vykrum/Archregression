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
                div { attr.``class`` "brand-logo"; "HYWE" }
                div {
                    attr.``class`` "brand-text"
                    h1 { "Archregression CAD" }
                    p { "Satellite DEM Topography & Generative Architectural Surrogates" }
                }
            }
            div {
                attr.``class`` "header-status"
                span { sprintf "%s | %.4f°N, %.4f°E | Elev: %.0f m MSL | Slope: %.1f° (%s)" model.Site.Name model.Site.Latitude model.Site.Longitude model.Site.Elevation model.Site.Topography.TerrainSlope model.Site.Topography.SlopeDirection.DisplayName }
            }
            div {
                attr.``class`` "header-actions"
                button {
                    attr.``class`` "btn"
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
                    attr.``class`` "btn"
                    on.click (fun _ -> ())
                    attr.id "btn-export-glb"
                    "Export GLB"
                }
                a {
                    attr.``class`` "btn"
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
                        attr.style "display: flex; gap: 0.6rem; align-items: center; margin-bottom: 0.6rem;"
                        button {
                            attr.``class`` "btn btn-primary"
                            on.click (fun _ -> dispatch DetectCurrentLocation)
                            "📍 Detect Current Location"
                        }
                        span {
                            attr.style "font-size: 0.78rem; color: #94a3b8;"
                            "Detects GPS / IP location and fetches real satellite elevation grid"
                        }
                    }
                    div {
                        attr.id "map-coords-readout"
                        attr.``class`` "map-coords-readout"
                        "Click anywhere on map or reposition marker to query real satellite DEM elevation grid."
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
