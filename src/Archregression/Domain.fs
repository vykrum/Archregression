namespace Archregression

open System

/// Performance simulation targets evaluated by the surrogate engine
type SimulationTarget =
    | DaylightAutonomy
    | CoolingEnergy
    | StructuralStress
    | EmbodiedCarbon
    | MicroclimateCooling
    | BiophilicGreenView
    | TreeCarbonOffset

    member this.Key =
        match this with
        | DaylightAutonomy -> "sda_percent"
        | CoolingEnergy -> "cooling_load"
        | StructuralStress -> "peak_stress"
        | EmbodiedCarbon -> "embodied_carbon"
        | MicroclimateCooling -> "microclimate_cooling"
        | BiophilicGreenView -> "biophilic_view"
        | TreeCarbonOffset -> "tree_carbon"

    member this.DisplayName =
        match this with
        | DaylightAutonomy -> "Daylight Autonomy (sDA)"
        | CoolingEnergy -> "Annual Cooling Energy"
        | StructuralStress -> "Peak Structural Stress"
        | EmbodiedCarbon -> "Embodied Carbon"
        | MicroclimateCooling -> "Microclimate Cooling (ΔT)"
        | BiophilicGreenView -> "Biophilic Green View"
        | TreeCarbonOffset -> "Tree Biomass Carbon"

    member this.Unit =
        match this with
        | DaylightAutonomy -> "%"
        | CoolingEnergy -> "kWh/m²"
        | StructuralStress -> "MPa"
        | EmbodiedCarbon -> "kgCO₂e"
        | MicroclimateCooling -> "°C"
        | BiophilicGreenView -> "%"
        | TreeCarbonOffset -> "kg/yr"

/// Architectural parametric envelope dimensions
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

/// Natural site environment typology
type SitePreset =
    | OceanCoastline
    | AlpineMountain
    | InlandLake
    | ForestCanopy
    | UrbanCanyon
    | StudioPlinth
    | CustomSite

    member this.Key =
        match this with
        | OceanCoastline -> "ocean"
        | AlpineMountain -> "mountain"
        | InlandLake -> "lake"
        | ForestCanopy -> "forest"
        | UrbanCanyon -> "urban"
        | StudioPlinth -> "studio"
        | CustomSite -> "custom"

    member this.DisplayName =
        match this with
        | OceanCoastline -> "🌊 Ocean Shoreline & Coastal Bluff"
        | AlpineMountain -> "⛰️ Alpine Mountain Ridge & Peaks"
        | InlandLake -> "🏞️ Inland Freshwater Lakefront"
        | ForestCanopy -> "🌲 Forested Tree Canopy"
        | UrbanCanyon -> "🏢 Dense Urban Canyon"
        | StudioPlinth -> "🏛️ Minimal Studio Plinth"
        | CustomSite -> "🛠️ Custom Site (Interactive Sliders)"

    static member FromKey (key: string) =
        match key.ToLowerInvariant() with
        | "mountain" -> AlpineMountain
        | "lake" -> InlandLake
        | "forest" -> ForestCanopy
        | "urban" -> UrbanCanyon
        | "studio" -> StudioPlinth
        | "custom" -> CustomSite
        | _ -> OceanCoastline

type WaterBodyType =
    | NoWater
    | OceanShoreline
    | FreshwaterLake

    member this.Key =
        match this with
        | NoWater -> "none"
        | OceanShoreline -> "ocean"
        | FreshwaterLake -> "lake"

    static member FromKey (k: string) =
        match k.ToLowerInvariant() with
        | "ocean" -> OceanShoreline
        | "lake" -> FreshwaterLake
        | _ -> NoWater

type VegetationBiome =
    | AlpineConifers
    | CoastalCypress
    | TemperateCanopy

    member this.Key =
        match this with
        | AlpineConifers -> "alpine"
        | CoastalCypress -> "coastal"
        | TemperateCanopy -> "temperate"

    static member FromKey (k: string) =
        match k.ToLowerInvariant() with
        | "alpine" -> AlpineConifers
        | "coastal" -> CoastalCypress
        | _ -> TemperateCanopy

type SlopeAspect =
    | AspectSouth
    | AspectNorth
    | AspectEast
    | AspectWest

    member this.Key =
        match this with
        | AspectSouth -> "south"
        | AspectNorth -> "north"
        | AspectEast -> "east"
        | AspectWest -> "west"

    static member FromKey (k: string) =
        match k.ToLowerInvariant() with
        | "north" -> AspectNorth
        | "east" -> AspectEast
        | "west" -> AspectWest
        | _ -> AspectSouth

/// Full 3D topography and natural context parameters
type TopographyParams =
    { MountainHeight: float
      TerrainSlope: float
      SlopeDirection: SlopeAspect
      Ruggedness: float
      WaterType: WaterBodyType
      WaterProximity: float
      TreeDensity: float
      Biome: VegetationBiome
      UrbanShadows: bool }
    static member DefaultOcean =
        { MountainHeight = 16.0
          TerrainSlope = 14.0
          SlopeDirection = AspectSouth
          Ruggedness = 0.55
          WaterType = OceanShoreline
          WaterProximity = 20.0
          TreeDensity = 0.60
          Biome = CoastalCypress
          UrbanShadows = false }
    static member DefaultMountain =
        { MountainHeight = 38.0
          TerrainSlope = 24.0
          SlopeDirection = AspectNorth
          Ruggedness = 0.85
          WaterType = NoWater
          WaterProximity = 40.0
          TreeDensity = 0.45
          Biome = AlpineConifers
          UrbanShadows = false }
    static member DefaultLake =
        { MountainHeight = 14.0
          TerrainSlope = 8.0
          SlopeDirection = AspectSouth
          Ruggedness = 0.40
          WaterType = FreshwaterLake
          WaterProximity = 18.0
          TreeDensity = 0.65
          Biome = TemperateCanopy
          UrbanShadows = false }

/// Geographic Project Site
type SiteLocation =
    { Name: string
      Latitude: float
      Longitude: float
      Elevation: float
      Preset: SitePreset
      Topography: TopographyParams }
    static member Sausalito =
        { Name = "Sausalito Coastal Bluff & Pacific Bay, California"
          Latitude = 37.8590
          Longitude = -122.4853
          Elevation = 18.0
          Preset = OceanCoastline
          Topography = TopographyParams.DefaultOcean }
    static member Chamonix =
        { Name = "Chamonix Mont-Blanc, France (Alpine Valley)"
          Latitude = 45.9237
          Longitude = 6.8694
          Elevation = 1041.0
          Preset = AlpineMountain
          Topography = TopographyParams.DefaultMountain }
    static member Zurich =
        { Name = "Lake Zurich Shoreline, Switzerland"
          Latitude = 47.3686
          Longitude = 8.5391
          Elevation = 408.0
          Preset = InlandLake
          Topography = TopographyParams.DefaultLake }

/// Real-time Surrogate Predictions
type SurrogatePredictions =
    { SdaPercent: float
      CoolingLoad: float
      PeakStress: float
      EmbodiedCarbon: float
      MicroclimateDeltaT: float
      BiophilicViewPct: float
      TreeCarbonOffset: int
      LatencyMs: float }

/// Elmish UI Model State
type Model =
    { Architecture: ArchitecturalParams
      Site: SiteLocation
      ActiveTarget: SimulationTarget
      Prediction: SurrogatePredictions
      ShowCustomPanel: bool
      ShowMapModal: bool
      PlantTreeMode: bool
      ShowSpatialGraph: bool
      ShowWireframe: bool
      ActiveCameraView: string
      IsOptimizing: bool }

/// Elmish Messages
type Message =
    | SetRoomDepth of float
    | SetRoomWidth of float
    | SetWwr of float
    | SetLouverAngle of float
    | SetOverhangDepth of float
    | SetOrientationAzimuth of float
    | SetGlazingUVal of float
    | SetTreeDensity of float
    | SelectSitePreset of SitePreset
    | UpdateMountainHeight of float
    | UpdateTerrainSlope of float
    | UpdateSlopeDirection of SlopeAspect
    | UpdateWaterType of WaterBodyType
    | UpdateWaterProximity of float
    | UpdateBiome of VegetationBiome
    | ApplySiteFromMap of SiteLocation
    | OpenMapModal
    | CloseMapModal
    | SelectTarget of SimulationTarget
    | SetCameraView of string
    | ToggleSpatialGraph
    | ToggleWireframe
    | TogglePlantTreeMode
    | OnTreePlanted of int
    | TriggerGenerativeOptimize
    | ApplyOptimizedDesign of ArchitecturalParams
    | ResetParameters
