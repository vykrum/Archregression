namespace Archregression.Trainer

open System
open System.IO
open Microsoft.ML

module Program =

    [<EntryPoint>]
    let main args =
        printfn "=============================================================="
        printfn " 🏛️⚡ Archregression ML.NET Surrogate & 3D Geometry Pipeline"
        printfn "    100%% Pure F# / .NET 10 Solution (Zero Python Required)"
        printfn "=============================================================="

        // Locate web root directory (src/Archregression/wwwroot)
        let baseDir = AppContext.BaseDirectory
        let targetWebRoot =
            let candidate1 = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Archregression", "wwwroot"))
            let candidate2 = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "wwwroot"))
            let candidate3 = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "src", "Archregression", "wwwroot"))
            if Directory.Exists(candidate1) then candidate1
            elif Directory.Exists(candidate2) then candidate2
            elif Directory.Exists(candidate3) then candidate3
            else
                let fallback = Path.Combine(Environment.CurrentDirectory, "output_wwwroot")
                Directory.CreateDirectory(fallback) |> ignore
                fallback

        printfn "📁 Target WebRoot Asset Destination: %s" targetWebRoot

        // 1. Generate Latin-Hypercube Space-Filling Parameter Matrix (600 samples)
        printfn "\n🔬 [Data Engine] Generating 600 spatial architectural permutations..."
        let samples = DatasetGenerator.generateDataset 600 42
        printfn "   ✓ Generated 600 multi-physics simulation points across 10 parametric dimensions."

        // 2. Initialize ML.NET Context & Train FastTree Surrogate Regressors
        let mlContext = MLContext(seed = Nullable 42)
        let evaluations = SurrogateTrainer.trainAndEvaluateAll mlContext samples targetWebRoot

        // 3. Generate 3D Spatial Lattice Mesh with Per-Vertex RGBA Heatmaps
        let glbPath = Path.Combine(targetWebRoot, "data", "sample_lattice.glb")
        MeshBuilder.createAtriumGLB glbPath

        // 4. Print Summary Report
        printfn "\n=============================================================="
        printfn " 📊 ML.NET SURROGATE EVALUATION REPORT"
        printfn "=============================================================="
        printfn " %-35s | %-8s | %-8s | %-8s" "Performance Target" "R² Score" "RMSE" "MAE"
        printfn " %s" (String.replicate 64 "-")
        for ev in evaluations do
            printfn " %-35s | %8.4f | %8.3f | %8.3f" ev.TargetName ev.R2 ev.Rmse ev.Mae
        printfn "=============================================================="
        printfn " ✨ All assets generated and synced to WebAssembly wwwroot!"
        printfn "    • %s" (Path.Combine(targetWebRoot, "data", "benchmark_dataset.json"))
        printfn "    • %s" (Path.Combine(targetWebRoot, "models", "surrogate_weights.json"))
        printfn "    • %s" glbPath
        printfn "=============================================================="

        0
