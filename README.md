# Archregression 🏛️⚡

> **Open-Source Architectural Surrogate Regression & 3D Spatial Inspection Lab**  
> Built for web-native performance, zero-license reproducibility, and seamless integration with **[Hywe](https://github.com/vykrum/Hywe)**.  
> Eliminates proprietary CAD/BIM desktop software licenses (Rhino, Grasshopper, Revit) in favor of open formats, client-side ML inference, and WebGL/WebGPU spatial tooling.

---

## 🌟 Key Architecture & Pillars

```
┌────────────────────────────────────────────────────────┐
│                   Web Browser View                     │
├──────────────────────────┬─────────────────────────────┤
│ 3D Spatial Viewer        │ Regression & Analysis UI    │
│ (Three.js / WebGL)       │ (Observable Plot / Plotly)  │
│                          │                             │
│ • Mesh surface heatmap   │ • Parity plot (Actual vs ŷ) │
│ • Spatial graph overlay  │ • SHAP feature importance   │
│ • Interactive parameters │ • Residual distribution     │
└──────────────────────────┴─────────────────────────────┘
```

1. **Open, Browser-Native File Formats**:
   - **glTF / GLB 2.0 (Khronos Group)**: Loads binary meshes and per-vertex scalar heatmaps (Daylight autonomy, solar irradiance, Von Mises structural stress) directly in WebGL. Includes one-click GLB export in-browser.
   - **Apache Parquet / Arrow IPC**: Tabular regression matrices ($X$ features and simulation targets $y$) queried in-browser via WebAssembly (DuckDB-Wasm).
   - **GeoJSON (RFC 7946)**: Encodes 2D floor plans, spatial graphs, room adjacency networks, and zoning footprints.
2. **Client-Side Surrogate Inference (<2ms)**:
   - Evaluates multi-target architectural regression in under 2 milliseconds using **ONNX Runtime Web** (`onnxruntime-web`) and optimized client tensor kernels.
   - Parametric sliders (Room Depth, Width, WWR, Louvers, Overhang, Orientation, Glazing) compute predictions locally with zero backend server lag.
3. **F# Bolero WebAssembly (Hywe Pattern)**:
   - Strongly-typed functional domain modeling ([`SimulationTarget`](file:///C:/Users/vykru/.gemini/antigravity-ide/scratch/Archregression/bolero/src/arch-regression-bolero.Client/Main.fs#L9-L33), [`ArchitecturalParams`](file:///C:/Users/vykru/.gemini/antigravity-ide/scratch/Archregression/bolero/src/arch-regression-bolero.Client/Main.fs#L35-L53)) and Elmish MVU loop compiled to WebAssembly via .NET 10.
4. **End-to-End Headless Pipeline & Hugging Face Hub Dataset**:
   - Automated cloud batch Python scripts (`trimesh`, `scikit-learn`, `skl2onnx`, `pyarrow`).
   - Hugging Face Dataset Card and multi-target benchmark dataset package ready for publication.

---

## 📁 Repository Layout

```text
Archregression/
├── public/
│   ├── models/
│   │   ├── surrogate.onnx          # Exported ONNX neural surrogate
│   │   └── surrogate_weights.json  # Calibrated weights & SHAP scores
│   └── data/
│       ├── benchmark_dataset.json  # Tabular validation matrix (160+ samples)
│       └── floorplan.geojson       # 2D GeoJSON spatial zoning & adjacency
├── src/
│   ├── viewer/
│   │   └── viewport.js             # Three.js 3D viewport & vertex color shaders
│   ├── charts/
│   │   └── analytics.js            # Parity, SHAP & residual distribution plots
│   ├── inference/
│   │   └── surrogate.js            # ONNX Runtime Web client wrapper
│   └── data/
│       └── geojsonViewer.js        # Zero-dependency 2D GeoJSON floor plan renderer
├── bolero/                         # F# Bolero WebAssembly implementation (Hywe pattern)
│   ├── arch-regression-bolero.sln  # .NET 10 solution
│   └── src/arch-regression-bolero.Client/
│       ├── Main.fs                 # Elmish model, update, surrogate, and Bolero views
│       └── Startup.fs              # Blazor WebAssembly entry point
├── headless_pipeline/              # Non-proprietary Python batch scripts
│   ├── generate_glb.py             # Mesh/lattice export via trimesh
│   ├── train_surrogate.py          # Regression training and ONNX export
│   └── requirements.txt
├── huggingface_dataset/            # Hugging Face Hub dataset package
│   ├── README.md                   # Dataset Card with YAML metadata
│   ├── data/                       # Train & Test JSON/Parquet splits
│   └── upload_dataset.py           # Script to push to Hugging Face Hub
├── .github/workflows/
│   └── deploy.yml                  # GitHub Actions CI/CD for GitHub Pages
├── index.html                      # Main browser studio interface
├── index.css                       # Dark-mode architectural CAD studio styling
└── package.json
```

---

## 🚀 Instructions

### 1. Running the Interactive Web Studio Locally

Start any static HTTP server inside the `Archregression` root directory:

```bash
# Using PowerShell static server script:
powershell -ExecutionPolicy Bypass -File .\serve.ps1

# Or using Python:
python -m http.server 8080

# Or using Node / npx:
npx serve . -l 8080
```
Open **`http://localhost:8080`** in your browser.

---

### 2. Building & Running the F# Bolero WebAssembly Project

```bash
cd bolero
dotnet build
dotnet run --project src/arch-regression-bolero.Client
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

### 4. Uploading the Dataset to Hugging Face (`vykrum/arch-regression-dataset`)

1. Install the Hugging Face CLI and log in with your access token:
```bash
pip install huggingface_hub
huggingface-cli login
```
2. Run the upload script inside `huggingface_dataset`:
```bash
cd C:\Users\vykru\.gemini\antigravity-ide\scratch\Archregression\huggingface_dataset
python upload_dataset.py
```
Your dataset and interactive Dataset Card will be live at `https://huggingface.co/datasets/vykrum/arch-regression-dataset`.
