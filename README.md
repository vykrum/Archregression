# ArchRegression.web 🏛️⚡

> **A completely open-source, browser-native architectural regression and 3D surrogate inspection lab.**  
> Eliminates proprietary CAD/BIM desktop software licenses (Rhino, Grasshopper, Revit) in favor of web-native open formats, client-side ML inference, and WebGL/WebGPU spatial tooling.

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

### 1. Open, Browser-Native File Formats
* **glTF / GLB (The "JPEG of 3D"):** Khronos Group open standard. Loads binary meshes, materials, and per-vertex scalar heatmaps (Daylight autonomy, solar irradiance, Von Mises structural stress) directly in WebGL. Includes one-click **GLB Export** in-browser.
* **Apache Parquet / Arrow IPC:** Queries tabular regression matrices ($X$ features and simulation targets $y$) in-browser via WebAssembly (DuckDB-Wasm) with zero database server requirements.
* **GeoJSON / TopoJSON (RFC 7946):** Encodes 2D floor plans, spatial graphs, room adjacency networks, and zoning footprints. Parses natively into JavaScript objects with zero dependencies.
* **CityJSON:** Semantic urban and architectural geometries.

### 2. Client-Side Surrogate Inference (<5ms)
* Export trained regression models (Ridge, MLP, Random Forest, or XGBoost) to **ONNX (Open Neural Network Exchange)**.
* Evaluates predictions locally in **under 2 milliseconds** using **ONNX Runtime Web** (`onnxruntime-web`) and optimized client tensor kernels.
* When the user drags geometric sliders (Depth, Width, WWR, Louvers, Overhang, Orientation), predictions update instantly without network roundtrips.

### 3. End-to-End Pipeline
| Layer | Open-Source Tool / Format | Execution Environment |
|---|---|---|
| **Geometry & Features** | `trimesh`, `shapely`, `networkx` | Headless Linux container (Cloud run / batch) |
| **Tabular Matrix** | Apache Parquet (`pyarrow` / `polars`) | Cloud Object Store / Hugging Face Datasets |
| **3D Asset Format** | GLB (Binary glTF) | Cloud Object Store |
| **Surrogate Model** | Scikit-learn / PyTorch $\rightarrow$ **ONNX** | Python (training) $\rightarrow$ Browser (inference) |
| **3D Rendering** | Three.js / WebGL | Web Browser |
| **Analytics & UI** | Plotly.js + DuckDB-Wasm | Web Browser |

---

## 📁 Repository Layout

```text
arch-regression-web/
├── public/
│   ├── models/
│   │   ├── surrogate.onnx          # Exported ONNX neural surrogate
│   │   └── surrogate_weights.json  # Calibrated weights & SHAP scores
│   └── data/
│       ├── benchmark_dataset.json  # Tabular validation matrix (160+ samples)
│       ├── floorplan.geojson       # 2D GeoJSON spatial zoning & adjacency
│       └── sample_lattice.glb      # Sample 3D GLB mesh with vertex attributes
├── src/
│   ├── viewer/
│   │   └── viewport.js             # Three.js 3D viewport & vertex color shaders
│   ├── charts/
│   │   └── analytics.js            # Parity, SHAP & residual distribution plots
│   ├── inference/
│   │   └── surrogate.js            # ONNX Runtime Web client wrapper
│   └── data/
│       └── geojsonViewer.js        # Zero-dependency 2D GeoJSON floor plan renderer
├── headless_pipeline/              # Non-proprietary Python batch scripts
│   ├── generate_glb.py             # Mesh/lattice export via trimesh
│   ├── train_surrogate.py          # Regression training and ONNX export
│   └── requirements.txt
├── index.html                      # Main browser application
├── index.css                       # Dark-mode architectural CAD studio styling
└── package.json
```

---

## 🚀 Running the Web Application

The application is fully client-side and can be run with any static HTTP server or deployed directly to **GitHub Pages**, **Vercel**, or **Hugging Face Spaces**:

### Quick Start:
```bash
# Using python:
python -m http.server 3000

# Or using npx:
npx serve . -l 3000
```
Open `http://localhost:3000` in any modern web browser.
