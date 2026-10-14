namespace Archregression

open System

/// Performance simulation targets evaluated by the surrogate engine
type SimulationTarget =
    | DaylightAutonomy
    | EarthworkBalance
    | AcousticComfort
    | ZoningFar
    | CoolingEnergy
    | StructuralStress
    | EmbodiedCarbon
    | MicroclimateCooling
    | BiophilicGreenView
    | TreeCarbonOffset

    member this.Key =
        match this with
        | DaylightAutonomy -> "sda_percent"
        | EarthworkBalance -> "earthwork_balance"
        | AcousticComfort -> "acoustic_rw"
        | ZoningFar -> "far_utilization"
        | CoolingEnergy -> "cooling_load"
        | StructuralStress -> "peak_stress"
        | EmbodiedCarbon -> "embodied_carbon"
        | MicroclimateCooling -> "microclimate_cooling"
        | BiophilicGreenView -> "biophilic_view"
        | TreeCarbonOffset -> "tree_carbon"

    member this.DisplayName =
        match this with
        | DaylightAutonomy -> "Daylight Autonomy (sDA)"
        | EarthworkBalance -> "Earthwork Cut/Fill Balance"
        | AcousticComfort -> "Acoustic Attenuation (R'w)"
        | ZoningFar -> "Zoning FAR Utilization"
        | CoolingEnergy -> "Annual Cooling Energy"
        | StructuralStress -> "Peak Structural Stress"
        | EmbodiedCarbon -> "Embodied Carbon"
        | MicroclimateCooling -> "Microclimate Cooling (ΔT)"
        | BiophilicGreenView -> "Biophilic Green View"
        | TreeCarbonOffset -> "Tree Biomass Carbon"

    member this.Unit =
        match this with
        | DaylightAutonomy -> "%"
        | EarthworkBalance -> "m³"
        | AcousticComfort -> "dB"
        | ZoningFar -> "ratio"
        | CoolingEnergy -> "kWh/m²"
        | StructuralStress -> "MPa"
        | EmbodiedCarbon -> "kgCO₂e"
        | MicroclimateCooling -> "°C"
        | BiophilicGreenView -> "%"
        | TreeCarbonOffset -> "kg/yr"

/// Architectural parametric envelope dimensions & massing program
type ArchitecturalParams =
    { RoomDepth: float
      RoomWidth: float
      StoryCount: int
      Wwr: float
      LouverAngle: float
      OverhangDepth: float
      OrientationAzimuth: float
      GlazingUVal: float
      PadElevationOffset: float }
    static member Default =
        { RoomDepth = 14.0
          RoomWidth = 12.0
          StoryCount = 2
          Wwr = 0.55
          LouverAngle = 35.0
          OverhangDepth = 1.0
          OrientationAzimuth = 0.0
          GlazingUVal = 1.8
          PadElevationOffset = 0.0 }

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
        | OceanCoastline -> "Coastal Bluff"
        | AlpineMountain -> "Alpine Ridge"
        | InlandLake -> "Lakefront"
        | ForestCanopy -> "Forest Canopy"
        | UrbanCanyon -> "Urban Canyon"
        | StudioPlinth -> "Studio Plinth"
        | CustomSite -> "Custom"

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

/// Road hierarchy & mobility classification
type RoadClassification =
    | LocalStreet
    | CollectorRoad
    | ArterialHighway

    member this.Key =
        match this with
        | LocalStreet -> "local"
        | CollectorRoad -> "collector"
        | ArterialHighway -> "arterial"

    member this.DisplayName =
        match this with
        | LocalStreet -> "Local Street (30 km/h)"
        | CollectorRoad -> "Collector Road (50 km/h)"
        | ArterialHighway -> "Arterial Highway (80 km/h)"

    static member FromKey (k: string) =
        match k.ToLowerInvariant() with
        | "arterial" -> ArterialHighway
        | "collector" -> CollectorRoad
        | _ -> LocalStreet

/// Urban zoning and density constraints
type UrbanZoningParams =
    { MaxFar: float             // Maximum Floor Area Ratio
      MaxBuildingHeightM: float // Height ceiling (m)
      TargetOccupancyPerM2: float // Occupant density (people/m²)
      LotAreaM2: float          // Site parcel footprint (m²)
    }
    static member Default =
        { MaxFar = 2.2
          MaxBuildingHeightM = 22.0
          TargetOccupancyPerM2 = 0.04
          LotAreaM2 = 1200.0 }

/// Complete urban mobility, environmental acoustic, and zoning context
type UrbanContextParams =
    { RoadType: RoadClassification
      RoadDistanceM: float            // Distance from primary road centerline (m)
      TrafficVolumeAadt: int          // Annual Average Daily Traffic (vehicles/day)
      AmbientNoiseLdenDb: float       // Decibel field level L_den (dB)
      TransitDistanceM: float         // Distance to nearest public transit hub (m)
      Zoning: UrbanZoningParams }
    static member DefaultSausalito =
        { RoadType = CollectorRoad
          RoadDistanceM = 18.0
          TrafficVolumeAadt = 8500
          AmbientNoiseLdenDb = 56.0
          TransitDistanceM = 350.0
          Zoning = { MaxFar = 1.8; MaxBuildingHeightM = 15.0; TargetOccupancyPerM2 = 0.035; LotAreaM2 = 1400.0 } }
    static member DefaultChamonix =
        { RoadType = LocalStreet
          RoadDistanceM = 35.0
          TrafficVolumeAadt = 2200
          AmbientNoiseLdenDb = 44.0
          TransitDistanceM = 650.0
          Zoning = { MaxFar = 1.2; MaxBuildingHeightM = 14.0; TargetOccupancyPerM2 = 0.025; LotAreaM2 = 1800.0 } }
    static member DefaultZurich =
        { RoadType = CollectorRoad
          RoadDistanceM = 22.0
          TrafficVolumeAadt = 11000
          AmbientNoiseLdenDb = 60.0
          TransitDistanceM = 180.0
          Zoning = { MaxFar = 2.8; MaxBuildingHeightM = 22.0; TargetOccupancyPerM2 = 0.05; LotAreaM2 = 1200.0 } }
    static member DefaultBlackForest =
        { RoadType = LocalStreet
          RoadDistanceM = 60.0
          TrafficVolumeAadt = 1200
          AmbientNoiseLdenDb = 38.0
          TransitDistanceM = 1200.0
          Zoning = { MaxFar = 1.0; MaxBuildingHeightM = 12.0; TargetOccupancyPerM2 = 0.02; LotAreaM2 = 2500.0 } }
    static member DefaultSingapore =
        { RoadType = ArterialHighway
          RoadDistanceM = 12.0
          TrafficVolumeAadt = 28000
          AmbientNoiseLdenDb = 74.0
          TransitDistanceM = 120.0
          Zoning = { MaxFar = 4.5; MaxBuildingHeightM = 45.0; TargetOccupancyPerM2 = 0.08; LotAreaM2 = 1000.0 } }

/// Geographic Project Site (Unifies Topography & Urban Context)
type SiteLocation =
    { Name: string
      Latitude: float
      Longitude: float
      Elevation: float
      Preset: SitePreset
      Topography: TopographyParams
      Urban: UrbanContextParams }
    static member Sausalito =
        { Name = "Sausalito, California"
          Latitude = 37.8590
          Longitude = -122.4853
          Elevation = 18.0
          Preset = OceanCoastline
          Topography = TopographyParams.DefaultOcean
          Urban = UrbanContextParams.DefaultSausalito }
    static member Chamonix =
        { Name = "Chamonix Mont-Blanc, France"
          Latitude = 45.9237
          Longitude = 6.8694
          Elevation = 1041.0
          Preset = AlpineMountain
          Topography = TopographyParams.DefaultMountain
          Urban = UrbanContextParams.DefaultChamonix }
    static member Zurich =
        { Name = "Lake Zurich, Switzerland"
          Latitude = 47.3686
          Longitude = 8.5391
          Elevation = 408.0
          Preset = InlandLake
          Topography = TopographyParams.DefaultLake
          Urban = UrbanContextParams.DefaultZurich }

/// Real-time Surrogate Predictions & Site Intelligence Telemetry
type SurrogatePredictions =
    { SdaPercent: float
      CoolingLoad: float
      PeakStress: float
      EmbodiedCarbon: float
      MicroclimateDeltaT: float
      BiophilicViewPct: float
      TreeCarbonOffset: int
      LatencyMs: float
      // Topography & Earthwork Metrics:
      CutVolumeM3: float
      FillVolumeM3: float
      NetEarthworkM3: float
      OptimalPadElevationM: float
      FoundationType: string
      HorizonObstructionDeg: float
      RunoffRiskIndex: float
      // Urban Context & Mobility Metrics:
      FacadeNoiseLdenDb: float
      RequiredAcousticRwDb: float
      TransitConnectivityScore: float
      GrossFloorAreaM2: float
      FarRatio: float
      FarUtilizationPct: float
      OccupantCapacity: int
      CurbCutPermitted: bool
      ZoningCompliant: bool }

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
    | SetStoryCount of int
    | SetWwr of float
    | SetLouverAngle of float
    | SetOverhangDepth of float
    | SetOrientationAzimuth of float
    | SetGlazingUVal of float
    | SetPadElevationOffset of float
    | SetTreeDensity of float
    | SelectSitePreset of SitePreset
    | UpdateMountainHeight of float
    | UpdateTerrainSlope of float
    | UpdateSlopeDirection of SlopeAspect
    | UpdateWaterType of WaterBodyType
    | UpdateWaterProximity of float
    | UpdateBiome of VegetationBiome
    | SetRoadDistance of float
    | SetAmbientNoise of float
    | SetTrafficVolume of int
    | SetTransitDistance of float
    | SetMaxFar of float
    | UpdateRoadType of RoadClassification
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
