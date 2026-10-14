namespace Archregression

open System
open Bolero
open Bolero.Html

/// Reusable Bolero HTML Components for Archregression
module Components =

    /// Top Navigation Header
    let renderHeader (model: Model) (dispatch: Message -> unit) =
        header {
            attr.``class`` "app-header"
            div {
                attr.``class`` "brand-section"
                div { attr.``class`` "brand-logo"; "🏛️" }
                div {
                    attr.``class`` "brand-text"
                    h1 { "Archregression" }
                    p { "F# WebAssembly (Hywe Engine) • Zero-License Architectural Lab" }
                }
            }
            div {
                attr.``class`` "header-actions"
                button {
                    attr.``class`` "btn"
                    on.click (fun _ -> dispatch OpenMapModal)
                    span { "🗺️" }
                    " Select Site on Map"
                }
                let optIcon = if model.IsOptimizing then "⏳" else "⚡"
                let optLabel = if model.IsOptimizing then " Synthesizing..." else " Generative Optimize"
                button {
                    attr.``class`` "btn btn-primary"
                    on.click (fun _ -> dispatch TriggerGenerativeOptimize)
                    span { optIcon }
                    optLabel
                }
                button {
                    attr.``class`` "btn"
                    on.click (fun _ -> ())
                    attr.id "btn-export-glb"
                    span { "📦" }
                    " Export GLB"
                }
                a {
                    attr.``class`` "btn"
                    attr.href "https://huggingface.co/datasets/vykrum/arch-regression-dataset"
                    attr.target "_blank"
                    span { "🤗" }
                    " Dataset"
                }
            }
        }

    /// Parameter Slider Row
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

    /// KPI Metric Card with Target Indicator
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

    /// Custom Topography Builder Panel (Sliders for peak height, slope, relief, water, biome)
    let renderCustomTopographyBuilder (site: SiteLocation) (dispatch: Message -> unit) =
        let topo = site.Topography
        div {
            attr.id "custom-site-panel"
            attr.``class`` "custom-site-builder"
            attr.style "background: rgba(14, 165, 233, 0.05); border: 1px solid rgba(14, 165, 233, 0.25); border-radius: 8px; padding: 0.75rem; margin-top: 0.75rem;"

            div {
                attr.style "display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.5rem;"
                div {
                    attr.style "font-size: 0.75rem; font-weight: 600; color: #38bdf8;"
                    span { "🏔️" }
                    " 3D Natural Topography & Hydrology"
                }
                span { attr.style "font-size: 0.65rem; color: #94a3b8;"; "Procedural Mesh" }
            }

            // Mountain Ridge Peak Elevation
            renderSlider "Mountain Peak Height" (sprintf "%.0f m" topo.MountainHeight) 0.0 60.0 2.0 topo.MountainHeight UpdateMountainHeight dispatch

            // Terrain Slope
            renderSlider "Terrain Slope" (sprintf "%.1f°" topo.TerrainSlope) 0.0 35.0 1.0 topo.TerrainSlope UpdateTerrainSlope dispatch

            // Slope Direction
            div {
                attr.``class`` "param-group"
                attr.style "margin-bottom: 0.5rem;"
                span { attr.``class`` "param-name"; attr.style "font-size: 0.72rem;"; "Slope Aspect Direction" }
                select {
                    attr.``class`` "site-preset-select"
                    attr.style "padding: 0.25rem 0.45rem; font-size: 0.72rem;"
                    attr.value topo.SlopeDirection.Key
                    on.change (fun e -> dispatch (UpdateSlopeDirection (SlopeAspect.FromKey (e.Value :?> string))))
                    option { attr.value "south"; "South Facing (High Solar Exposure)" }
                    option { attr.value "north"; "North Facing (Shaded / Cool)" }
                    option { attr.value "east"; "East Facing (Morning Sun)" }
                    option { attr.value "west"; "West Facing (Afternoon Sun)" }
                }
            }

            // Water Feature Selection
            div {
                attr.``class`` "param-group"
                attr.style "margin-bottom: 0.5rem;"
                span { attr.``class`` "param-name"; attr.style "font-size: 0.72rem;"; "Water Body Feature" }
                select {
                    attr.``class`` "site-preset-select"
                    attr.style "padding: 0.25rem 0.45rem; font-size: 0.72rem;"
                    attr.value topo.WaterType.Key
                    on.change (fun e -> dispatch (UpdateWaterType (WaterBodyType.FromKey (e.Value :?> string))))
                    option { attr.value "ocean"; "🌊 Ocean Shoreline (Rolling Waves)" }
                    option { attr.value "lake"; "🏞️ Inland Freshwater Lake" }
                    option { attr.value "none"; "None (Inland / Dry Ridge)" }
                }
            }

            // Shoreline Distance
            renderSlider "Shoreline Distance" (sprintf "%.0f m" topo.WaterProximity) 12.0 60.0 2.0 topo.WaterProximity UpdateWaterProximity dispatch

            // Vegetation Biome
            div {
                attr.``class`` "param-group"
                span { attr.``class`` "param-name"; attr.style "font-size: 0.72rem;"; "Vegetation Biome" }
                select {
                    attr.``class`` "site-preset-select"
                    attr.style "padding: 0.25rem 0.45rem; font-size: 0.72rem;"
                    attr.value topo.Biome.Key
                    on.change (fun e -> dispatch (UpdateBiome (VegetationBiome.FromKey (e.Value :?> string))))
                    option { attr.value "coastal"; "🌴 Coastal Cypress & Pines" }
                    option { attr.value "alpine"; "🌲 Alpine Conifers & Firs" }
                    option { attr.value "temperate"; "🌳 Temperate Mixed Canopy" }
                }
            }
        }

    let renderCustomSitePanel = renderCustomTopographyBuilder

    /// Renders the Global Interactive Map Modal
    let renderMapModal (model: Model) (dispatch: Message -> unit) =
        div {
            attr.``class`` (if model.ShowMapModal then "modal-overlay active" else "modal-overlay")
            attr.id "modal-map"
            div {
                attr.``class`` "modal-dialog"
                attr.style "max-width: 820px; width: 90vw;"
                div {
                    attr.``class`` "modal-header"
                    div {
                        attr.``class`` "modal-title"
                        span { "🗺️" }
                        " Global Site Selector & Topographic Inspector"
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
                        attr.``class`` "map-preset-pills"
                        button { attr.``class`` "map-preset-btn"; on.click (fun _ -> dispatch (SelectSitePreset OceanCoastline)); "🌊 Pacific Ocean Coast (Sausalito)" }
                        button { attr.``class`` "map-preset-btn"; on.click (fun _ -> dispatch (SelectSitePreset AlpineMountain)); "🏔️ Alpine Mountain (Zermatt, Swiss Alps)" }
                        button { attr.``class`` "map-preset-btn"; on.click (fun _ -> dispatch (SelectSitePreset InlandLake)); "🌲 Mountain Lake (Lake Tahoe)" }
                        button { attr.``class`` "map-preset-btn"; on.click (fun _ -> dispatch (SelectSitePreset UrbanCanyon)); "🏜️ Desert Plateau (Atacama)" }
                        button { attr.``class`` "map-preset-btn"; on.click (fun _ -> dispatch (SelectSitePreset ForestCanopy)); "🌿 Temperate Woodland (Black Forest)" }
                    }
                    div {
                        attr.id "map-coords-readout"
                        attr.style "font-family: 'JetBrains Mono', monospace; font-size: 0.75rem; color: #38bdf8; margin-bottom: 0.5rem;"
                        "Click anywhere on the map or drag the pin to query real elevation, slope, relief & biomes."
                    }
                    div {
                        attr.id "map-picker-container"
                        attr.style "width: 100%; height: 380px; border-radius: 6px; border: 1px solid rgba(255,255,255,0.1); background: #0b0f19; z-index: 10;"
                    }
                    div {
                        attr.``class`` "modal-actions"
                        attr.style "margin-top: 1rem; display: flex; justify-content: flex-end; gap: 0.5rem;"
                        button {
                            attr.``class`` "btn"
                            attr.style "background: rgba(255,255,255,0.1); color: #cbd5e1; border: 1px solid rgba(255,255,255,0.2);"
                            on.click (fun _ -> dispatch CloseMapModal)
                            "Cancel"
                        }
                        button {
                            attr.``class`` "btn btn-primary"
                            attr.style "background: #0284c7; color: #fff; font-weight: 600;"
                            on.click (fun _ -> dispatch CloseMapModal)
                            "Apply Site to 3D Scene"
                        }
                    }
                }
            }
        }

