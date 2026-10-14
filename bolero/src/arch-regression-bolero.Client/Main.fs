namespace arch_regression_bolero.Client

open System
open Elmish
open Bolero
open Bolero.Html

type SimulationTarget =
    | DaylightAutonomy
    | CoolingEnergy
    | StructuralStress
    | EmbodiedCarbon
    member this.DisplayName =
        match this with
        | DaylightAutonomy -> "Daylight Autonomy (sDA)"
        | CoolingEnergy -> "Annual Cooling Energy"
        | StructuralStress -> "Peak Structural Stress"
        | EmbodiedCarbon -> "Embodied Carbon"
    member this.Unit =
        match this with
        | DaylightAutonomy -> "%"
        | CoolingEnergy -> "kWh/m²"
        | StructuralStress -> "MPa"
        | EmbodiedCarbon -> "kgCO₂e"

type ArchitecturalParams =
    { RoomDepth: float
      RoomWidth: float
      Wwr: float
      LouverAngle: float
      OverhangDepth: float
      OrientationAzimuth: float
      GlazingUVal: float }
    static member Default =
        { RoomDepth = 14.0
          RoomWidth = 12.0
          Wwr = 0.55
          LouverAngle = 35.0
          OverhangDepth = 1.0
          OrientationAzimuth = 0.0
          GlazingUVal = 1.8 }

type SurrogatePrediction =
    { SdaPercent: float
      CoolingLoad: float
      PeakStress: float
      EmbodiedCarbon: float
      LatencyMs: float }

type Model =
    { Params: ArchitecturalParams
      ActiveTarget: SimulationTarget
      Prediction: SurrogatePrediction
      ShowSpatialGraph: bool
      ShowWireframe: bool }

type Message =
    | SetRoomDepth of float
    | SetRoomWidth of float
    | SetWwr of float
    | SetLouverAngle of float
    | ResetParameters
    | SelectTarget of SimulationTarget
    | ToggleSpatialGraph
    | ToggleWireframe

module Surrogate =
    let evaluate (p: ArchitecturalParams) : SurrogatePrediction =
        let daylightPenetration = (p.Wwr * 118.0) / (1.0 + 0.11 * p.RoomDepth)
        let shadingFactor = Math.Cos(p.LouverAngle * Math.PI / 180.0) * (1.0 - 0.22 * p.OverhangDepth)
        let sda = Math.Min(99.0, Math.Max(8.0, 14.0 + daylightPenetration * shadingFactor))

        let solarGain = (p.Wwr * 76.0) * (1.0 - 0.38 * (p.LouverAngle / 75.0)) * (1.0 - 0.24 * p.OverhangDepth)
        let cooling = Math.Min(150.0, Math.Max(18.0, 22.0 + solarGain + p.GlazingUVal * 14.2 + (p.RoomDepth * 0.82)))

        let span = Math.Max(p.RoomDepth, p.RoomWidth)
        let stress = Math.Min(120.0, Math.Max(8.0, 8.2 + 0.086 * Math.Pow(span, 2.08) + (p.Wwr * 7.2)))
        let carbon = Math.Min(650.0, Math.Max(160.0, (p.Wwr * 175.0) + (p.OverhangDepth * 32.0) + 162.0 + 0.44 * Math.Pow(span, 1.91)))

        { SdaPercent = Math.Round(sda, 1)
          CoolingLoad = Math.Round(cooling, 1)
          PeakStress = Math.Round(stress, 1)
          EmbodiedCarbon = Math.Round(carbon, 1)
          LatencyMs = 0.85 }

type MainComponent() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let init _ =
            let p = ArchitecturalParams.Default
            { Params = p
              ActiveTarget = DaylightAutonomy
              Prediction = Surrogate.evaluate p
              ShowSpatialGraph = true
              ShowWireframe = false }, Cmd.none

        let update msg model =
            match msg with
            | SetRoomDepth v ->
                let p = { model.Params with RoomDepth = v }
                { model with Params = p; Prediction = Surrogate.evaluate p }, Cmd.none
            | SetRoomWidth v ->
                let p = { model.Params with RoomWidth = v }
                { model with Params = p; Prediction = Surrogate.evaluate p }, Cmd.none
            | SetWwr v ->
                let p = { model.Params with Wwr = v }
                { model with Params = p; Prediction = Surrogate.evaluate p }, Cmd.none
            | SetLouverAngle v ->
                let p = { model.Params with LouverAngle = v }
                { model with Params = p; Prediction = Surrogate.evaluate p }, Cmd.none
            | ResetParameters ->
                let p = ArchitecturalParams.Default
                { model with Params = p; Prediction = Surrogate.evaluate p }, Cmd.none
            | SelectTarget t ->
                { model with ActiveTarget = t }, Cmd.none
            | ToggleSpatialGraph ->
                { model with ShowSpatialGraph = not model.ShowSpatialGraph }, Cmd.none
            | ToggleWireframe ->
                { model with ShowWireframe = not model.ShowWireframe }, Cmd.none

        let view model dispatch =
            concat {
                header {
                    attr.``class`` "app-header"
                    div {
                        attr.``class`` "brand-section"
                        div { attr.``class`` "brand-logo"; "🏛️" }
                        div {
                            attr.``class`` "brand-text"
                            h1 { "ArchRegression.Bolero" }
                            p { "F# WebAssembly (Hywe Architecture)" }
                        }
                    }
                }
                div {
                    attr.``class`` "main-workspace"
                    aside {
                        attr.``class`` "parameters-drawer"
                        h3 { "F# Parametric Controls" }
                        p { sprintf "Room Depth: %.1f m" model.Params.RoomDepth }
                        p { sprintf "Room Width: %.1f m" model.Params.RoomWidth }
                        p { sprintf "WWR: %d%%" (int (model.Params.Wwr * 100.0)) }
                    }
                    div {
                        attr.``class`` "analytics-panel"
                        div {
                            attr.``class`` "kpi-grid"
                            div {
                                attr.``class`` "kpi-card active"
                                span { "sDA 300/50%: " }
                                strong { sprintf "%.1f%%" model.Prediction.SdaPercent }
                            }
                            div {
                                attr.``class`` "kpi-card"
                                span { "Cooling Energy: " }
                                strong { sprintf "%.1f kWh/m²" model.Prediction.CoolingLoad }
                            }
                            div {
                                attr.``class`` "kpi-card"
                                span { "Von Mises Stress: " }
                                strong { sprintf "%.1f MPa" model.Prediction.PeakStress }
                            }
                            div {
                                attr.``class`` "kpi-card"
                                span { "Embodied Carbon: " }
                                strong { sprintf "%.1f kgCO₂e" model.Prediction.EmbodiedCarbon }
                            }
                        }
                    }
                }
            }

        Program.mkProgram init update view
