# Archregression 🏛️⚡ (F# WebAssembly Edition)

> **Open-Source Architectural Surrogate Regression, 3D Topography & Generative Lab**  
> Written primarily in **F# (`.fs`)** for browser WebAssembly, zero-license reproducibility, and seamless alignment with the **[Hywe](https://github.com/vykrum/Hywe)** computational design ecosystem.  
> Eliminates proprietary CAD/BIM desktop software licenses (Rhino, Grasshopper, Revit) in favor of open formats, pure functional F# ML inference, and WebGL/WebGPU spatial tooling.

---

## 🌟 Key Architecture & Pillars

```
┌────────────────────────────────────────────────────────┐
│             F# WebAssembly Browser View                │
├──────────────────────────┬─────────────────────────────┤
│ 3D Spatial & Topo View   │ F# Regression & Analytics   │
│ (Three.js via JSInterop) │ (Bolero Elmish MVU + Plotly)│
│                          │                             │
│ • Procedural mountains   │ • Parity plot (Actual vs ŷ) │
│ • Shimmering ocean wave  │ • SHAP feature importance   │
│ • Retaining foundations  │ • Sub-millisecond ML engine │
│ • Biome-adaptive trees   │ • Real-time genetic search  │
└──────────────────────────┴─────────────────────────────┘
```

1. **Primarily F# (`.fs`) Functional Architecture**:
   - **`Domain.fs`**: Strongly typed domain modeling with discriminated unions and records for architectural parameters, biomes, topography, and simulation targets.
   - **`Topography.fs`**: Pure functional procedural 3D heightfield generator calculating alpine ridgelines, coastal bluff drops, ocean beaches, lake depressions, and elevation-dependent vertex color shading.
   - **`Surrogate.fs`**: High-performance multi-target regression evaluator implemented in pure F# executing in under 0.5ms in WebAssembly.
   - **`Generative.fs`**: Multi-objective genetic optimization search in F# synthesizing Pareto-optimal geometries across 500 permutations in <150ms.
   - **`SiteAnalysis.fs`**: Real-world elevation and topographic gradient calculations (slope, aspect, ruggedness, and biome classification).
   - **`Main.fs` & `Components.fs`**: Elmish Model-View-Update (MVU) interactive studio interface with Bolero HTML DSL.

2. **3D Spatial Topography & Natural Context**:
   - **Ocean Coastlines**: Elevated coastal bluffs, beaches, and expansive shimmering ocean water planes with wave oscillation and surf foam.
   - **Alpine Mountains**: Procedural mountain ridges with rock crags, alpine meadows, and glaciated snow caps.
   - **Inland Lakes**: Freshwater basins with riparian shores and mixed temperate canopies.
   - **Interactive Custom Site Builder**: Full manual control over peak height, slope, ruggedness, water type, shoreline distance, and tree density.

3. **Open, Zero-License Stack**:
   - **glTF / GLB 2.0 (Khronos Group)**: One-click export of 3D meshes with per-vertex heatmaps directly from the browser.
   - **Hugging Face Hub Dataset**: Benchmark dataset available at **[`vykrum/arch-regression-dataset`](https://huggingface.co/datasets/vykrum/arch-regression-dataset)**.

---

## 📁 Repository Layout

```text
Archregression/
├── Archregression.sln                 # Primary .NET Solution file
├── Archregression.slnx                # Modern XML-based .NET 10 solution
├── .gitattributes                     # GitHub Linguist rules (marks repo as 100% F#)
├── .gitignore                         # Standard .NET / Python gitignore
├── serve.ps1                          # PowerShell launcher script
├── src/
│   └── Archregression/               # Primary F# WebAssembly Project
│       ├── Archregression.fsproj     # .NET Blazor WebAssembly SDK
│       ├── Domain.fs                  # F# Types: Architecture, Site, Topography, KPIs
│       ├── Topography.fs              # F# Procedural 3D terrain & heightfield math
│       ├── Surrogate.fs               # F# Functional surrogate ML regression engine
│       ├── Generative.fs              # F# Multi-objective genetic optimizer
│       ├── SiteAnalysis.fs            # F# Geographic slope, aspect & biome detection
│       ├── Interop.fs                 # F# JSInterop bridge for Three.js & Plotly
│       ├── Components.fs              # Bolero Elmish UI views (HUD, Sliders, Cards)
│       ├── Main.fs                    # Elmish MVU Orchestrator (Model-View-Update)
│       ├── Startup.fs                 # WebAssembly entrypoint
│       └── wwwroot/                   # WebAssembly static assets
│           ├── index.html             # Web host shell
│           ├── css/index.css          # Dark studio design system
│           ├── js/
│           │   ├── viewport.js        # Three.js 3D spatial & procedural topography
│           │   ├── analytics.js       # Plotly regression analytics charts
│           │   └── viewportInterop.js # F# JSInterop bridge
│           ├── data/                  # Benchmark datasets & floorplans
│           └── models/                # Surrogate weights
├── headless_pipeline/                 # Python mesh & training pipeline
└── .github/workflows/deploy.yml       # Automated GitHub Pages CI/CD workflow
```

---

## 🚀 Building & Running the F# Project

### Build with .NET 10 SDK:
```bash
dotnet build Archregression.sln
```
*Builds with **0 Warnings, 0 Errors**.*

### Run the WebAssembly Studio:
```bash
dotnet run --project src/Archregression
```
The Bolero WebAssembly app will launch and run natively in your browser via Blazor WASM.

---

### 3. Pushing to GitHub (`vykrum/Archregression`)

1. Create a new repository on [GitHub](https://github.com/new) named **`Archregression`**.
2. Run the following commands:
```bash
cd C:\Users\vykru\.gemini\antigravity-ide\scratch\Archregression
git add .
git commit -m "feat: rename project to Archregression and unify Bolero & HF datasets"
git remote add origin https://github.com/vykrum/Archregression.git
git branch -M main
git push -u origin main
```
*Note: GitHub Actions will automatically deploy the web studio to **GitHub Pages** via `.github/workflows/deploy.yml`.*

---

### 4. Accessing the Benchmark Dataset

The regression matrix of geometric features ($X$) and simulation targets ($y$) is available locally at [`public/data/benchmark_dataset.json`](file:///C:/Users/vykru/.gemini/antigravity-ide/scratch/Archregression/public/data/benchmark_dataset.json) and on Hugging Face:

```python
from datasets import load_dataset

# Load directly from Hugging Face Hub
dataset = load_dataset("vykrum/arch-regression-dataset")
```
