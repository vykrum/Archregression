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
                    span { "🌍" }
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
            }
        }

    /// Parameter Slider Row
    let renderSlider (label: string) (formattedVal: string) (minV: float) (maxV: float) (stepV: float) (curV: float) (onInput: float -> Message) (dispatch: Message -> unit) =
        div {
            attr.``class`` "param-group"
            div {
                attr.``class`` "param-label-row"
                span { attr.``class`` "param-name"; label }
                span { attr.``class`` "param-val"; formattedVal }
            }
            input {
                attr.``type`` "range"
                attr.``class`` "slider-input"
                attr.min minV
                attr.max maxV
                attr.step stepV
                attr.value curV
                on.input (fun e ->
                    match Double.TryParse(e.Value :?> string) with
                    | true, v -> dispatch (onInput v)
                    | _ -> ())
            }
        }

    /// KPI Telemetry Card
    let renderKpiCard (target: SimulationTarget) (title: string) (value: string) (unitStr: string) (subtext: string) (activeTarget: SimulationTarget) (dispatch: Message -> unit) =
        let isActive = target = activeTarget
        div {
            attr.``class`` (if isActive then "kpi-card active" else "kpi-card")
            on.click (fun _ -> dispatch (SelectTarget target))
            div { attr.``class`` "kpi-title"; title }
            div {
                attr.``class`` "kpi-value-row"
                span { attr.``class`` "kpi-value"; value }
                span { attr.``class`` "kpi-unit"; unitStr }
            }
            div { attr.``class`` "kpi-sub"; subtext }
        }

    /// Interactive Custom Topography & Water Builder Panel
    let renderCustomSitePanel (model: Model) (dispatch: Message -> unit) =
        let topo = model.Site.Topography
        div {
            attr.id "custom-site-panel"
            attr.style "background: rgba(15, 23, 42, 0.7); padding: 0.75rem; border-radius: 8px; border: 1px solid rgba(56, 189, 248, 0.25); margin-top: 0.5rem; margin-bottom: 0.75rem;"

            div {
                attr.style "font-size: 0.75rem; font-weight: 700; color: #38bdf8; margin-bottom: 0.5rem; display: flex; justify-content: space-between;"
                span { "🛠️ F# Custom Topography Builder" }
                span { attr.style "color: #94a3b8; font-size: 0.65rem;"; "Procedural 3D" }
            }

            // Mountain / Peak Height Slider
            renderSlider "Mountain / Peak Height" (sprintf "%.0f m" topo.MountainHeight) 0.0 50.0 2.0 topo.MountainHeight UpdateMountainHeight dispatch

            // Terrain Slope Slider
            renderSlider "Terrain Slope" (sprintf "%.0f°" topo.TerrainSlope) 0.0 35.0 1.0 topo.TerrainSlope UpdateTerrainSlope dispatch

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
