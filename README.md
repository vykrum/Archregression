# Archregression 

> **Open-Source Architectural Surrogate Regression, 3D Topography & Generative Lab**  
> Written **100% in F# (`.fs`)** powered by **ML.NET (`Microsoft.ML`)**, **Bolero WebAssembly (.NET 10)**, and **SharpGLTF (Khronos Group)**.  
> Eliminates Python, pip, virtualenvs, and proprietary CAD/BIM software (Rhino, Grasshopper, Revit) in favor of a single unified .NET toolchain, type-safe ML inference, and WebGL/WebGPU spatial tooling aligned with **[Hywe](https://github.com/vykrum/Hywe)**.

---

## Key Architecture & Pillars

```
┌────────────────────────────────────────────────────────┐
│             F# WebAssembly Browser View                │
├──────────────────────────┬─────────────────────────────┤
│ 3D Spatial & Topo View   │ F# Regression & Analytics   │
│ (Three.js via JSInterop) │ (Bolero Elmish MVU + Plotly)│
│                          │                             │
│ • Procedural mountains   │ • Parity plot (Actual vs ŷ) │
│ • Shimmering ocean wave  │ • ML.NET PFI / SHAP charts  │
│ • Retaining foundations  │ • Sub-millisecond ML engine │
│ • Biome-adaptive trees   │ • Real-time genetic search  │
└──────────────────────────┴─────────────────────────────┘
```

1. **100% F# / .NET 10 Monorepo**:
   - **`Archregression.Core`**: Shared domain models (`Domain.fs`), procedural terrain generator (`Topography.fs`), high-performance surrogate evaluator (`Surrogate.fs`), and Pareto genetic optimizer (`Generative.fs`).
   - **`Archregression`**: Interactive browser studio running in WebAssembly via Bolero and Elmish MVU.
   - **`Archregression.Trainer`**: Headless ML.NET training engine and SharpGLTF 3D mesh generator.

2. **ML.NET FastTree Surrogate Regression & PFI**:
   - Fits multi-target FastTree regression models across daylight autonomy ($sDA$), cooling energy, peak structural stress, embodied carbon, microclimate $\Delta T$, and biophilia.
   - Calculates **Permutation Feature Importance (PFI)** to quantify exact sensitivity for every architectural variable.
   - Replaces all legacy Python scripts (`scikit-learn`, `trimesh`, `pyarrow`) with pure, compiled F# executing at native speed.

3. **3D Spatial Topography & Natural Context**:
   - **Ocean Coastlines**: Elevated coastal bluffs, beaches, and expansive shimmering ocean water planes with wave oscillation and surf foam.
   - **Alpine Mountains**: Procedural mountain ridges with rock crags, alpine meadows, and glaciated snow caps.
   - **Inland Lakes**: Freshwater basins with riparian shores and mixed temperate canopies.
   - **Interactive Custom Site Builder**: Full manual control over peak height, slope, ruggedness, water type, shoreline distance, and tree density.

4. **Open, Zero-License Stack**:
   - **SharpGLTF (Khronos Group)**: Native .NET 3D binary glTF (GLB) mesh synthesis with per-vertex RGBA heatmaps.
   - **Hugging Face Hub Dataset**: Benchmark dataset live at **[`vykrum/arch-regression-dataset`](https://huggingface.co/datasets/vykrum/arch-regression-dataset)**.

---

## Repository Layout

```text
Archregression/
├── Archregression.sln                 # Complete Visual Studio / CLI Solution
├── Archregression.slnx                # Modern XML-based .NET 10 solution
├── .gitattributes                     # Linguist rules (marks repo as 100% F#)
├── .gitignore                         # Standard .NET / IDE gitignore
├── serve.ps1                          # PowerShell launcher script
├── src/
│   ├── Archregression.Core/          # Shared Pure F# Domain & Math Engine
│   │   ├── Archregression.Core.fsproj
│   │   ├── Domain.fs                  # F# Types: Architecture, Site, Topography, KPIs
│   │   ├── Topography.fs              # Pure F# procedural 3D heightfield math
│   │   ├── Surrogate.fs               # Pure F# surrogate ML regression engine (<0.5ms)
│   │   ├── Generative.fs              # Multi-objective genetic optimizer (500 perms in <150ms)
│   │   └── SiteAnalysis.fs            # Geographic slope, aspect & biome classification
│   │
│   ├── Archregression/               # Bolero WebAssembly Client (Blazor WASM)
│   │   ├── Archregression.fsproj
│   │   ├── Interop.fs                 # JSInterop bridge for Three.js & Plotly
│   │   ├── Components.fs              # Bolero Elmish UI views (HUD, Sliders, KPI Cards)
│   │   ├── Main.fs                    # Elmish MVU Orchestrator (Model-View-Update)
│   │   ├── Startup.fs                 # WebAssembly entrypoint
│   │   └── wwwroot/                   # WebAssembly static assets
│   │       ├── index.html             # Web host shell
│   │       ├── css/index.css          # Dark studio design system
│   │       ├── js/
│   │       │   ├── viewport.js        # Three.js 3D spatial & procedural topography
│   │       │   ├── analytics.js       # Plotly regression analytics charts
│   │       │   └── viewportInterop.js # F# JSInterop bridge
│   │       ├── data/                  # Benchmark datasets, GLB meshes & floorplans
│   │       └── models/                # ML.NET models & surrogate weights
│   │
│   └── Archregression.Trainer/       # ML.NET Surrogate Trainer & SharpGLTF Pipeline
│       ├── Archregression.Trainer.fsproj
│       ├── Types.fs                   # Strongly typed ML.NET schemas
│       ├── DatasetGenerator.fs        # Multi-physics parametric simulation sampler
│       ├── SurrogateTrainer.fs        # FastTree regressors, metrics & PFI feature rankings
│       ├── MeshBuilder.fs             # Headless 3D glTF/GLB generator via SharpGLTF
│       └── Program.fs                 # CLI tool generating weights, benchmarks & GLB
└── .github/workflows/deploy.yml       # Automated GitHub Pages CI/CD workflow
```

---

## Building & Running

### 1. Build the Entire Solution (.NET 10 SDK)
```bash
dotnet build Archregression.sln
```
*Builds with **0 Warnings, 0 Errors** across all 3 projects.*

### 2. Run the ML.NET Training Pipeline
```bash
dotnet run --project src/Archregression.Trainer
```
*Trains FastTree models for all 6 targets, computes Permutation Feature Importance, generates `sample_lattice.glb`, and updates `wwwroot` assets.*

### 3. Launch the WebAssembly Studio
```powershell
./serve.ps1
# or
dotnet run --project src/Archregression
```
*Launches the Bolero WebAssembly app natively in your browser.*

---

## License & Attribution

- Licensed under **Apache-2.0**.
- Built with **F#**, **ML.NET**, **Bolero**, **Three.js**, and **SharpGLTF**.
- Designed to integrate seamlessly with **[Hywe](https://github.com/vykrum/Hywe)**.
