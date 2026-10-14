# Headless Architectural Extraction & Surrogate Training Pipeline

This pipeline provides a 100% open-source alternative to proprietary CAD/BIM software (Rhino, Grasshopper, Revit). It runs entirely in headless Linux environments, automated CI/CD runners (GitHub Actions), or batch compute clusters (AWS Batch, GCP Cloud Run).

## Pipeline Architecture

```text
Parametric Variables (X)
         │
         ├──► generate_glb.py (trimesh + shapely + networkx)
         │           └──► binary glTF (GLB) with per-vertex scalars (solar/stress)
         │
         └──► train_surrogate.py (scikit-learn + pyarrow + skl2onnx)
                     ├──► Parquet tabular regression matrix
                     └──► surrogate.onnx (evaluated in-browser via onnxruntime-web)
```

## Setup & Execution

### 1. Local / Container Installation

```bash
cd headless_pipeline
pip install -r requirements.txt
```

### 2. Generate 3D Assets & Vertex Color Heatmaps

```bash
python generate_glb.py
```
- Outputs: `../src/Archregression/wwwroot/data/sample_lattice.glb`
- Encodes surface irradiance and structural stress directly into per-vertex RGBA buffers.

### 3. Train Surrogate Model & Export to ONNX

```bash
python train_surrogate.py
```
- Fits multi-target Ridge / MLP / Gradient Boosting regressors on spatial feature matrix.
- Evaluates Parity metrics ($R^2$, RMSE, MAE).
- Computes SHAP feature importance vectors.
- Exports `surrogate.onnx` and `benchmark_dataset.json` for client-side browser evaluation.

## Dockerfile for Headless Cloud Batch

```dockerfile
FROM python:3.11-slim
WORKDIR /workspace
RUN apt-get update && apt-get install -y --no-install-recommends libgl1-mesa-glx && rm -rf /var/lib/apt/lists/*
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY . .
CMD ["python", "train_surrogate.py"]
```
