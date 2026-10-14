namespace Archregression.Trainer

open System
open System.IO
open System.Numerics
open SharpGLTF.Geometry
open SharpGLTF.Geometry.VertexTypes
open SharpGLTF.Materials
open SharpGLTF.Scenes

module MeshBuilder =

    type VBuilder = VertexBuilder<VertexPosition, VertexColor1, VertexEmpty>

    let inline makeVertex (p: Vector3) (c: Vector4) : VBuilder =
        let mutable pos = p
        let mutable g = VertexPosition(&pos)
        let mutable m = VertexColor1(c)
        VertexBuilder<VertexPosition, VertexColor1, VertexEmpty>(&g, &m)

    let createAtriumGLB (outputPath: string) =
        printfn "\n🏛️ [SharpGLTF] Generating 3D Parametric Atrium Lattice GLB with Per-Vertex Scalars..."

        let material =
            MaterialBuilder("LatticeMaterial")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.RoughnessFactor, 0.45f)
                .WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.MetallicFactor, 0.1f)

        let mesh = MeshBuilder<VertexPosition, VertexColor1, VertexEmpty>("AtriumPavilion")
        let prim = mesh.UsePrimitive(material)

        let width = 14.0f
        let depth = 20.0f
        let height = 4.8f

        // Helper to add a colored quad (2 triangles)
        let addQuad (p1: Vector3) (p2: Vector3) (p3: Vector3) (p4: Vector3) (c1: Vector4) (c2: Vector4) (c3: Vector4) (c4: Vector4) =
            let v1 = makeVertex p1 c1
            let v2 = makeVertex p2 c2
            let v3 = makeVertex p3 c3
            let v4 = makeVertex p4 c4
            prim.AddTriangle(v1, v2, v3) |> ignore
            prim.AddTriangle(v1, v3, v4) |> ignore

        // Color palettes (RGBA): Solar Irradiance Heatmap & Concrete/Steel
        let colorSlab = Vector4(0.18f, 0.22f, 0.28f, 1.0f)
        let colorLow = Vector4(0.22f, 0.74f, 0.97f, 1.0f)   // High daylight / cool
        let colorMid = Vector4(0.29f, 0.78f, 0.40f, 1.0f)   // Moderate
        let colorHigh = Vector4(0.96f, 0.62f, 0.13f, 1.0f)  // High solar heat gain
        let colorPeak = Vector4(0.94f, 0.27f, 0.24f, 1.0f)  // Peak stress

        // 1. Ground Foundation Slab
        let hw = width / 2.0f
        let hd = depth / 2.0f
        addQuad
            (Vector3(-hw, 0.0f, -hd))
            (Vector3(hw, 0.0f, -hd))
            (Vector3(hw, 0.0f, hd))
            (Vector3(-hw, 0.0f, hd))
            colorSlab colorSlab colorSlab colorSlab

        // 2. Structural Columns (4 corner columns)
        let colSize = 0.4f
        let colPositions = [ (-hw + 0.5f, -hd + 0.5f); (hw - 0.5f, -hd + 0.5f); (-hw + 0.5f, hd - 0.5f); (hw - 0.5f, hd - 0.5f) ]
        for (cx, cz) in colPositions do
            let cMin = Vector3(cx - colSize/2.0f, 0.0f, cz - colSize/2.0f)
            let cMax = Vector3(cx + colSize/2.0f, height, cz + colSize/2.0f)
            addQuad
                (Vector3(cMin.X, 0.0f, cMin.Z))
                (Vector3(cMax.X, 0.0f, cMin.Z))
                (Vector3(cMax.X, height, cMin.Z))
                (Vector3(cMin.X, height, cMin.Z))
                colorMid colorMid colorPeak colorPeak

        // 3. Roof Pergola & Parametric Louver Slats (with solar irradiance gradient)
        let louverCount = 14
        let stepZ = (depth - 2.0f) / single louverCount
        for i in 0 .. (louverCount - 1) do
            let z = -hd + 1.0f + single i * stepZ
            let t = single i / single louverCount
            let louverColor =
                if t < 0.33f then colorLow
                elif t < 0.66f then colorMid
                else colorHigh

            let angleRad = 0.5f // ~30 deg tilt
            let louverH = 0.5f * MathF.Sin(angleRad)
            let louverD = 0.5f * MathF.Cos(angleRad)

            addQuad
                (Vector3(-hw + 0.5f, height, z))
                (Vector3(hw - 0.5f, height, z))
                (Vector3(hw - 0.5f, height + louverH, z + louverD))
                (Vector3(-hw + 0.5f, height + louverH, z + louverD))
                louverColor louverColor louverColor louverColor

        // 4. Glazing Facade with Heatmap Gradients
        addQuad
            (Vector3(-hw, 0.2f, -hd))
            (Vector3(hw, 0.2f, -hd))
            (Vector3(hw, height, -hd))
            (Vector3(-hw, height, -hd))
            colorLow colorMid colorHigh colorHigh

        let scene = SceneBuilder()
        scene.AddRigidMesh(mesh, Matrix4x4.Identity) |> ignore
        let gltf = scene.ToGltf2()

        let dir = Path.GetDirectoryName(outputPath)
        if not (String.IsNullOrEmpty(dir)) then Directory.CreateDirectory(dir) |> ignore
        gltf.SaveGLB(outputPath)

        let fi = FileInfo(outputPath)
        printfn "   ✓ Exported valid glTF 2.0 binary to: %s (%.1f KB)" outputPath (float fi.Length / 1024.0)
