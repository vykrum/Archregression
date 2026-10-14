# Archregression (100% F# & ML.NET Edition)

> **Open-Source Architectural Surrogate Regression, 3D Topography & Generative Lab**  
> Written **100% in F# (`.fs`)** powered by **ML.NET (`Microsoft.ML`)**, **Bolero WebAssembly (.NET 8/10)**, and **SharpGLTF (Khronos Group)**.  
> Eliminates Python, pip, virtual environments, and proprietary CAD/BIM software (Rhino, Grasshopper, Revit) in favor of a single unified .NET toolchain, type-safe ML inference, and WebGL spatial tooling aligned with **[Hywe](https://github.com/vykrum/Hywe)**.

---

## Key Architecture & Pillars

```text
+-------------------------------------------------------------+
|                F# WebAssembly Browser View                  |
+------------------------------+------------------------------+
|   3D Spatial & Topo View     |   F# Regression & Telemetry  |
|   (Three.js via JSInterop)   |   (Bolero Elmish MVU + Plotly)|
|                              |                              |
|   - Procedural topography    |   - Parity plot (Actual vs y)|
|   - Real-time cut & fill     |   - Acoustic R'w attenuation |
|   - Topographic solar mask   |   - Zoning FAR compliance    |
|   - Biome vegetation         |   - Real-time Pareto search  |
+------------------------------+------------------------------+
```

1. **100% F# / .NET Monorepo**:
   - **`Archregression.Core`**: Shared domain models (`Domain.fs`), procedural terrain generator (`Topography.fs`), high-performance surrogate evaluator (`Surrogate.fs`), and Pareto genetic optimizer (`Generative.fs`).
   - **`Archregression`**: Utilitarian browser CAD studio running in WebAssembly via Bolero and Elmish MVU.
   - **`Archregression.Trainer`**: Headless ML.NET training engine and SharpGLTF 3D mesh generator.

2. **Topographic Earthwork & Solar Engines**:
   - **Cut & Fill Earthwork Analysis**: Calculates excavation vs. embankment volumes and foundation pad elevation for net-zero earthwork balance.
   - **Slope & Aspect Analysis**: Automatically classifies buildable zones and structural strategies (`Slab-on-Grade`, `Split-Level Terraced`, `Elevated Piers`).
   - **Mountain Horizon Masking**: Evaluates mountain ridge solar occlusion angles ($\theta_H$) blocking low sun angles.
   - **Hydrology & Runoff Index**: Computes Topographic Wetness Index (TWI proxy) to identify water flow pooling risk.

3. **Urban Context & Mobility Layers**:
   - **Acoustic Noise Fields ($L_{\text{den}}$)**: Line-source road noise propagation with distance decay and vegetation buffers, calculating required facade sound insulation ($R'_w$ in dB).
   - **Connectivity & Transit**: Multimodal transit walkability scoring affecting embodied parking carbon credits.
   - **Traffic Volume & Logistics**: Checks AADT traffic thresholds to flag vehicular curb-cut restrictions on arterial corridors.
   - **Zoning & Population Density**: Enforces Floor Area Ratio (FAR/FSI) envelopes and calculates occupant capacity.

4. **Open, Zero-License Stack**:
   - **SharpGLTF (Khronos Group)**: Native .NET 3D binary glTF (GLB) mesh synthesis with per-vertex RGBA heatmaps.
   - **Hugging Face Hub Dataset**: Benchmark dataset live at **[`vykrum/arch-regression-dataset`](https://huggingface.co/datasets/vykrum/arch-regression-dataset)**.

---

## Repository Layout

```text
Archregression/
├── Archregression.sln                 # Visual Studio / CLI Solution
├── .gitattributes                     # Linguist rules (marks repo as 100% F#)
├── .gitignore                         # Standard .NET & local script gitignore
├── src/
│   ├── Archregression.Core/          # Shared Pure F# Domain & Math Engine
│   │   ├── Archregression.Core.fsproj
│   │   ├── Domain.fs                  # F# Types: Architecture, Site, Urban, Topography, KPIs
│   │   ├── Topography.fs              # Pure F# procedural 3D heightfield math
│   │   ├── Surrogate.fs               # Pure F# surrogate ML regression engine (<0.5ms)
│   │   ├── Generative.fs              # Multi-objective genetic optimizer (500 candidates in <150ms)
│   │   └── SiteAnalysis.fs            # Geographic slope, aspect, relief & urban presets
│   │
│   ├── Archregression/               # Bolero WebAssembly Client (Blazor WASM)
│   │   ├── Archregression.fsproj
│   │   ├── Interop.fs                 # JSInterop bridge for Three.js & Plotly
│   │   ├── Components.fs              # Bolero Elmish UI views (Clean HUD, Sliders, Telemetry)
│   │   ├── Main.fs                    # Elmish MVU Orchestrator (Model-View-Update)
│   │   ├── Startup.fs                 # WebAssembly entrypoint
│   │   └── wwwroot/                   # WebAssembly static assets
│   │       ├── index.html             # Web host shell
│   │       ├── css/index.css          # Utilitarian CAD design system
│   │       ├── js/
│   │       │   ├── viewport.js        # Three.js 3D spatial & procedural topography
│   │       │   ├── analytics.js       # Plotly regression analytics charts
│   │       │   └── mapPicker.js       # Global satellite elevation & terrain picker
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

## Building & Running (Cross-Platform CLI)

The project uses the standard cross-platform .NET CLI on Windows, Linux, and macOS.

### 1. Build the Entire Solution
```bash
dotnet build Archregression.sln
```
*Builds with **0 Warnings, 0 Errors** across all projects.*

### 2. Launch the WebAssembly Studio (Development Server)
```bash
dotnet run --project src/Archregression
```
*Automatically binds to `http://localhost:5050` (configured in `launchSettings.json`).*

To bind to all network interfaces on a remote host or container:
```bash
dotnet run --project src/Archregression --urls "http://0.0.0.0:5050"
```

### 3. Run the ML.NET Training Pipeline
```bash
dotnet run --project src/Archregression.Trainer
```
*Trains FastTree models, computes Permutation Feature Importance, generates `sample_lattice.glb`, and updates `wwwroot` assets.*

### 4. Publish for Production / Remote Static Hosting
Because the studio runs client-side in WebAssembly, it can be published and hosted on any static file server (Nginx, Caddy, GitHub Pages, Cloudflare Pages, S3):
```bash
dotnet publish src/Archregression -c Release -o dist
```
*The resulting `dist/wwwroot` folder contains static HTML, WebAssembly `.wasm`, JS, and CSS bundles.*

---

## License & Attribution

- Licensed under **Apache-2.0**.
- Built with **F#**, **ML.NET**, **Bolero**, **Three.js**, and **SharpGLTF**.
- Designed to integrate seamlessly with **[Hywe](https://github.com/vykrum/Hywe)**.
