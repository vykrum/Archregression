namespace Archregression.Trainer

open System
open Microsoft.ML.Data

[<CLIMutable>]
type SpatialDesignRow =
    { [<LoadColumn(0)>] RoomDepth: single
      [<LoadColumn(1)>] RoomWidth: single
      [<LoadColumn(2)>] Wwr: single
      [<LoadColumn(3)>] LouverAngle: single
      [<LoadColumn(4)>] OverhangDepth: single
      [<LoadColumn(5)>] OrientationAzimuth: single
      [<LoadColumn(6)>] GlazingUVal: single
      [<LoadColumn(7)>] TreeDensity: single
      [<LoadColumn(8)>] HasWater: single
      [<LoadColumn(9)>] Slope: single
      [<LoadColumn(10)>] Label: single }

[<CLIMutable>]
type RegressionPrediction =
    { [<ColumnName("Score")>] Score: single }

type FeatureImportance =
    { Feature: string
      Key: string
      Score: float }

type TargetEvaluation =
    { TargetKey: string
      TargetName: string
      Unit: string
      R2: float
      Rmse: float
      Mae: float
      FeaturesRanked: FeatureImportance list }
