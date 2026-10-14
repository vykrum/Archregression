---
annotations_creators:
- machine-generated
language:
- en
license:
- apache-2.0
multilinguality:
- monolingual
size_categories:
- n<1K
source_datasets:
- original
task_categories:
- tabular-regression
task_ids:
- multi-target-regression
tags:
- architecture
- computational-design
- surrogate-modeling
- building-performance
- daylighting
- energyplus
- radiance
- hywe
- spatial-reasoning
dataset_info:
  features:
  - name: id
    dtype: int64
  - name: room_depth
    dtype: float64
  - name: room_width
    dtype: float64
  - name: aspect_ratio
    dtype: float64
  - name: wwr
    dtype: float64
  - name: louver_angle
    dtype: float64
  - name: overhang_depth
    dtype: float64
  - name: orientation_azimuth
    dtype: float64
  - name: glazing_u_val
    dtype: float64
  - name: actual_sda_percent
    dtype: float64
  - name: actual_cooling_load
    dtype: float64
  - name: actual_peak_stress
    dtype: float64
  - name: actual_embodied_carbon
    dtype: float64
  splits:
  - name: train
    num_bytes: 32768
    num_examples: 128
  - name: test
    num_bytes: 8192
    num_examples: 32
---

# Architectural Parametric Spatial Regression Dataset (HYWE Benchmark)

An open-source benchmark dataset designed for training and evaluating fast machine learning surrogate models for early-stage architectural design exploration.

Eliminates proprietary CAD/BIM software dependencies (Rhino, Grasshopper, Revit) by providing standardized tabular matrices and 3D glTF/GLB geometric meshes with per-vertex physical simulation scalars.

## Dataset Summary

* **Task:** Multi-Target Tabular Regression & 3D Spatial Feature Mapping
* **Design Space:** Parametric room massing, facade window-to-wall ratios (WWR), exterior shading louvers, structural spans, and envelope thermal conductance.
* **Physics Engines Emulated:** Radiance (Daylighting), EnergyPlus (Annual Cooling Loads), and OpenSees (Structural Von Mises Stress).

## Feature Matrix ($X$)

| Feature | Type | Range | Unit | Description |
|---|---|---|---|---|
| `room_depth` | float | 6.0 – 24.0 | m | Length of the architectural zone from facade to rear wall |
| `room_width` | float | 6.0 – 22.0 | m | Width of facade aperture and lateral structural span |
| `aspect_ratio` | float | 0.35 – 3.5 | ratio | Room depth divided by room width |
| `wwr` | float | 0.15 – 0.85 | ratio | Window-to-Wall Ratio on primary South aperture |
| `louver_angle` | float | 0 – 75 | deg | Tilt angle of exterior shading louvers |
| `overhang_depth`| float | 0.0 – 2.5 | m | Exterior roof overhang projection depth |
| `orientation_azimuth` | float | -90 – +90 | deg | Azimuth deviation from true South (0°) |
| `glazing_u_val`| float | 0.8 – 3.2 | W/m²K | Thermal transmittance coefficient of window glazing |

## Simulation Targets ($y$)

| Target | Unit | Higher is Better? | Typical Range | Physics Driver |
|---|---|---|---|---|
| `actual_sda_percent` | % | Yes | 10 – 95 % | Spatial Daylight Autonomy (sDA 300/50%) |
| `actual_cooling_load` | kWh/m²/yr | No | 20 – 145 kWh/m² | Annual cooling energy intensity |
| `actual_peak_stress` | MPa | No | 10 – 95 MPa | Peak structural bending & shear stress |
| `actual_embodied_carbon`| kgCO₂e/m²| No | 180 – 580 kg | Life-cycle modules A1–A3 (cradle-to-gate) |

## Usage with Hugging Face `datasets`

```python
from datasets import load_dataset

# Load directly from Hugging Face Hub
dataset = load_dataset("vykrum/arch-regression-dataset")

# Inspect sample design
train_df = dataset["train"].to_pandas()
print(train_df[["room_depth", "wwr", "actual_sda_percent", "actual_cooling_load"]].head())
```

## Citation & Attribution

If you use this dataset or the accompanying **Hywe** F# Bolero WebAssembly platform in your computational design research, please cite:

```bibtex
@misc{vykrum_hywe_regression_2026,
  author = {Vykrum},
  title = {Hywe: Open Computational Design & Architectural Surrogate Regression Environment},
  year = {2026},
  publisher = {Hugging Face / GitHub},
  howpublished = {\url{https://github.com/vykrum/Hywe}}
}
```
