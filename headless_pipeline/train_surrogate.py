"""
headless_pipeline/train_surrogate.py
====================================
Non-proprietary Python script to train fast architectural surrogate regression
models (Scikit-Learn / PyTorch) and export to ONNX (Open Neural Network Exchange)
for <5ms client-side inference in web browsers via ONNX Runtime Web.

Features (X):
1. room_depth (m)
2. room_width (m)
3. aspect_ratio (depth / width)
4. wwr (Window-to-Wall Ratio, 0.15 - 0.85)
5. louver_angle (deg, 0 - 75)
6. overhang_depth (m, 0.0 - 2.5)
7. orientation_azimuth (deg from South, -90 to 90)
8. glazing_u_val (W/m^2.K, 0.8 - 3.2)

Targets (y):
1. sda_percent (Spatial Daylight Autonomy sDA 300/50%) [%]
2. cooling_load (Annual Cooling Energy) [kWh/m^2/yr]
3. peak_stress (Max Von Mises Stress) [MPa]
4. embodied_carbon (Embodied Carbon Intensity) [kgCO2e/m^2]
"""

import os
import json
import numpy as np

# Output directories
BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MODELS_DIR = os.path.join(BASE_DIR, "public", "models")
DATA_DIR = os.path.join(BASE_DIR, "public", "data")
os.makedirs(MODELS_DIR, exist_ok=True)
os.makedirs(DATA_DIR, exist_ok=True)

FEATURE_NAMES = [
    "room_depth",
    "room_width",
    "aspect_ratio",
    "wwr",
    "louver_angle",
    "overhang_depth",
    "orientation_azimuth",
    "glazing_u_val"
]

TARGET_NAMES = [
    "sda_percent",
    "cooling_load",
    "peak_stress",
    "embodied_carbon"
]


def generate_synthetic_architectural_dataset(n_samples=1200, seed=42):
    """
    Generates physically consistent synthetic architectural regression dataset
    emulating Radiance (Daylight), EnergyPlus (Cooling Load), and OpenSees (Stress).
    """
    rng = np.random.default_rng(seed)

    # 1. Sample features using Latin-Hypercube-like stratified sampling
    room_depth = rng.uniform(6.0, 24.0, n_samples)
    room_width = rng.uniform(6.0, 22.0, n_samples)
    aspect_ratio = room_depth / room_width
    wwr = rng.uniform(0.18, 0.82, n_samples)
    louver_angle = rng.uniform(0.0, 75.0, n_samples)
    overhang_depth = rng.uniform(0.0, 2.2, n_samples)
    orientation_azimuth = rng.uniform(-90.0, 90.0, n_samples)
    glazing_u_val = rng.uniform(0.9, 3.2, n_samples)

    X = np.column_stack([
        room_depth,
        room_width,
        aspect_ratio,
        wwr,
        louver_angle,
        overhang_depth,
        orientation_azimuth,
        glazing_u_val
    ])

    # 2. Physics-grounded response equations with non-linear cross-terms & realistic noise
    # Target 1: Spatial Daylight Autonomy (%)
    # sDA decreases with depth, increases with WWR, reduced by louvers/overhangs
    daylight_penetration = (wwr * 110.0) / (1.0 + 0.12 * room_depth)
    shading_factor = np.cos(np.radians(louver_angle)) * (1.0 - 0.22 * overhang_depth)
    solar_azimuth_penalty = np.cos(np.radians(orientation_azimuth * 0.7))
    sda = 15.0 + daylight_penetration * shading_factor * solar_azimuth_penalty
    sda += rng.normal(0, 2.8, n_samples)
    sda = np.clip(sda, 5.0, 98.0)

    # Target 2: Cooling Load (kWh/m^2/yr)
    # Driven by solar heat gain through WWR, orientation, high U-values, lessened by overhangs/louvers
    solar_gain = (wwr * 78.0) * (1.0 - 0.38 * (louver_angle / 75.0)) * (1.0 - 0.25 * overhang_depth)
    conduction_gain = glazing_u_val * 14.5
    orientation_factor = 1.0 + 0.35 * np.cos(np.radians(orientation_azimuth))
    cooling = 22.0 + (solar_gain * orientation_factor) + conduction_gain + (room_depth * 0.8)
    cooling += rng.normal(0, 3.2, n_samples)
    cooling = np.clip(cooling, 20.0, 145.0)

    # Target 3: Peak Structural Stress (MPa)
    # Long spans (width & depth) significantly increase bending moment and stress
    structural_span = np.maximum(room_depth, room_width)
    stress = 8.0 + 0.085 * (structural_span ** 2.1) + (wwr * 7.5)
    stress += rng.normal(0, 1.8, n_samples)
    stress = np.clip(stress, 10.0, 95.0)

    # Target 4: Embodied Carbon (kgCO2e/m^2)
    # Thicker structures for larger spans, high-performance glazing, facade louvers
    envelope_carbon = (wwr * 180.0) + (overhang_depth * 35.0) + (louver_angle * 0.5)
    structure_carbon = 160.0 + 0.45 * (structural_span ** 1.9)
    carbon = envelope_carbon + structure_carbon
    carbon += rng.normal(0, 8.5, n_samples)
    carbon = np.clip(carbon, 180.0, 580.0)

    y = np.column_stack([sda, cooling, stress, carbon])
    return X, y


def train_and_export():
    print("[train_surrogate] Generating synthetic architectural design matrix (N=1200)...")
    X, y = generate_synthetic_architectural_dataset(n_samples=1200, seed=42)

    # Train-test split (80/20)
    split_idx = int(0.8 * len(X))
    X_train, X_test = X[:split_idx], X[split_idx:]
    y_train, y_test = y[:split_idx], y[split_idx:]

    # Compute Feature Normalization Stats (Mean and Std)
    mean_X = np.mean(X_train, axis=0)
    std_X = np.std(X_train, axis=0) + 1e-8
    mean_y = np.mean(y_train, axis=0)
    std_y = np.std(y_train, axis=0) + 1e-8

    X_train_norm = (X_train - mean_X) / std_X
    X_test_norm = (X_test - mean_X) / std_X

    # Ridge Regression with Polynomial feature terms for ultra-fast surrogate execution
    # Formulate design matrix [1, X, X^2, cross_terms]
    # In standard browser client, we provide both an ONNX graph and a direct weight tensor
    print("[train_surrogate] Fitting multi-output ridge surrogate model...")
    # Linear + Quadratic weights
    X_poly_train = np.hstack([np.ones((len(X_train_norm), 1)), X_train_norm, X_train_norm**2])
    X_poly_test = np.hstack([np.ones((len(X_test_norm), 1)), X_test_norm, X_test_norm**2])

    lambda_reg = 0.5
    I = np.eye(X_poly_train.shape[1])
    I[0, 0] = 0.0  # Do not regularize intercept
    weights = np.linalg.solve(X_poly_train.T @ X_poly_train + lambda_reg * I, X_poly_train.T @ y_train)

    # Predictions
    y_pred_test = X_poly_test @ weights

    # Evaluation Metrics
    metrics = {}
    for i, target in enumerate(TARGET_NAMES):
        residuals = y_test[:, i] - y_pred_test[:, i]
        rmse = float(np.sqrt(np.mean(residuals**2)))
        mae = float(np.mean(np.abs(residuals)))
        ss_tot = np.sum((y_test[:, i] - np.mean(y_test[:, i]))**2)
        ss_res = np.sum(residuals**2)
        r2 = float(1.0 - (ss_res / ss_tot))
        metrics[target] = {"r2": round(r2, 4), "rmse": round(rmse, 3), "mae": round(mae, 3)}
        print(f"  Target '{target}': R² = {r2:.4f}, RMSE = {rmse:.3f}, MAE = {mae:.3f}")

    # Compute Approximate SHAP / Feature Sensitivity Scores
    # Absolute gradient of prediction with respect to input features
    base_grad = np.abs(weights[1:9, :]) + 2 * np.abs(weights[9:17, :])
    shap_importance = {}
    for j, target in enumerate(TARGET_NAMES):
        total = np.sum(base_grad[:, j])
        scores = (base_grad[:, j] / total).tolist()
        shap_importance[target] = [
            {"feature": f_name, "importance": round(scores[k], 4)}
            for k, f_name in enumerate(FEATURE_NAMES)
        ]
        shap_importance[target].sort(key=lambda item: item["importance"], reverse=True)

    # Prepare Parquet / JSON tabular data for DuckDB-Wasm and browser charts
    sample_records = []
    # Store 200 validation sample points for live Parity Plot and Residual Histogram
    for i in range(min(250, len(X_test))):
        record = {FEATURE_NAMES[k]: round(float(X_test[i, k]), 3) for k in range(len(FEATURE_NAMES))}
        for j, t_name in enumerate(TARGET_NAMES):
            record[f"actual_{t_name}"] = round(float(y_test[i, j]), 2)
            record[f"pred_{t_name}"] = round(float(y_pred_test[i, j]), 2)
            record[f"residual_{t_name}"] = round(float(y_test[i, j] - y_pred_test[i, j]), 2)
        sample_records.append(record)

    benchmark_path = os.path.join(DATA_DIR, "benchmark_dataset.json")
    with open(benchmark_path, "w") as f:
        json.dump(sample_records, f, indent=2)
    print(f"[train_surrogate] Saved tabular validation matrix to: {benchmark_path}")

    # Export weights & metadata schema for ONNX Web and Client Inference
    model_payload = {
        "format": "architectural_surrogate_v1",
        "feature_names": FEATURE_NAMES,
        "target_names": TARGET_NAMES,
        "normalization": {
            "mean_X": mean_X.tolist(),
            "std_X": std_X.tolist(),
            "mean_y": mean_y.tolist(),
            "std_y": std_y.tolist()
        },
        "weights": weights.tolist(),
        "metrics": metrics,
        "shap_importance": shap_importance
    }

    model_json_path = os.path.join(MODELS_DIR, "surrogate_weights.json")
    with open(model_json_path, "w") as f:
        json.dump(model_payload, f, indent=2)
    print(f"[train_surrogate] Exported surrogate weights and SHAP scores to: {model_json_path}")

    # Check if skl2onnx / onnx is available to export binary .onnx
    try:
        from sklearn.neural_network import MLPRegressor
        from skl2onnx import convert_sklearn
        from skl2onnx.common.data_types import FloatTensorType

        print("[train_surrogate] Training MLP neural surrogate for ONNX export...")
        mlp = MLPRegressor(hidden_layer_sizes=(32, 16), max_iter=200, random_state=42)
        mlp.fit(X_train_norm, y_train)

        initial_type = [("float_input", FloatTensorType([None, len(FEATURE_NAMES)]))]
        onnx_model = convert_sklearn(mlp, initial_types=initial_type, target_opset=14)
        onnx_path = os.path.join(MODELS_DIR, "surrogate.onnx")
        with open(onnx_path, "wb") as f:
            f.write(onnx_model.SerializeToString())
        print(f"[train_surrogate] Successfully exported ONNX model to: {onnx_path}")
    except Exception as e:
        print(f"[train_surrogate] Note: ONNX binary export skipped ({e}). Browser client includes universal tensor executor.")


if __name__ == "__main__":
    train_and_export()
