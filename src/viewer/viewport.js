/**
 * src/viewer/viewport.js
 * ======================
 * Three.js 3D Spatial Viewer for Architectural Parametric Exploration,
 * Live Per-Vertex Heatmap Color Mapping, and 3D Room Adjacency Graph Overlays.
 */

import * as THREE from 'https://esm.sh/three@0.165.0';
import { OrbitControls } from 'https://esm.sh/three@0.165.0/examples/jsm/controls/OrbitControls.js';
import { GLTFExporter } from 'https://esm.sh/three@0.165.0/examples/jsm/exporters/GLTFExporter.js';

export class SpatialViewport {
  constructor(containerElement) {
    this.container = containerElement;
    this.scene = null;
    this.camera = null;
    this.renderer = null;
    this.controls = null;
    this.sunLight = null;
    this.buildingGroup = null;
    this.heatmapMesh = null;
    this.spatialGraphGroup = null;
    this.sectionPlane = null;
    this.clippingEnabled = false;

    // View State
    this.activeMetric = 'sda_percent'; // 'sda_percent' | 'cooling_load' | 'peak_stress'
    this.colormap = 'turbo'; // 'turbo' | 'viridis' | 'plasma' | 'coolwarm'
    this.showSpatialGraph = true;
    this.showWireframe = false;
    this.params = {
      room_depth: 14.0,
      room_width: 12.0,
      wwr: 0.55,
      louver_angle: 35.0,
      overhang_depth: 1.0,
      orientation_azimuth: 0.0,
      glazing_u_val: 1.8
    };

    this.init();
  }

  init() {
    const width = this.container.clientWidth || 800;
    const height = this.container.clientHeight || 600;

    // 1. Scene
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x0e1117);
    this.scene.fog = new THREE.FogExp2(0x0e1117, 0.015);

    // 2. Camera
    this.camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000);
    this.camera.position.set(22, 18, 26);

    // 3. Renderer with antialiasing and local clipping
    this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, powerPreference: 'high-performance' });
    this.renderer.setSize(width, height);
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    this.renderer.localClippingEnabled = true;
    this.container.appendChild(this.renderer.domElement);

    // 4. Controls
    this.controls = new OrbitControls(this.camera, this.renderer.domElement);
    this.controls.enableDamping = true;
    this.controls.dampingFactor = 0.06;
    this.controls.maxPolarAngle = Math.PI / 2 + 0.02; // Prevent going deep below ground
    this.controls.target.set(0, 2.5, 0);

    // 5. Section Clipping Plane (along Y-Z axis)
    this.sectionPlane = new THREE.Plane(new THREE.Vector3(0, 0, -1), 100);

    // 6. Lighting Rig
    this.setupLighting();

    // 7. Ground Grid & Studio Plinth
    this.setupEnvironment();

    // 8. Architectural Geometry Container
    this.buildingGroup = new THREE.Group();
    this.scene.add(this.buildingGroup);

    // 9. Spatial Adjacency Graph Group
    this.spatialGraphGroup = new THREE.Group();
    this.scene.add(this.spatialGraphGroup);

    // 10. Generate Initial Geometry
    this.rebuildArchitecture();
    this.buildSpatialGraph();

    // 11. Handle Resize
    window.addEventListener('resize', () => this.onWindowResize());

    // 12. Animation Loop
    this.animate = this.animate.bind(this);
    requestAnimationFrame(this.animate);
  }

  setupLighting() {
    // Hemispherical ambient sky / ground light
    const hemiLight = new THREE.HemisphereLight(0xe8f0fe, 0x181a20, 0.7);
    hemiLight.position.set(0, 50, 0);
    this.scene.add(hemiLight);

    // Directional Sun Light
    this.sunLight = new THREE.DirectionalLight(0xfff5e6, 1.8);
    this.sunLight.castShadow = true;
    this.sunLight.shadow.mapSize.width = 2048;
    this.sunLight.shadow.mapSize.height = 2048;
    this.sunLight.shadow.camera.near = 0.5;
    this.sunLight.shadow.camera.far = 120;
    this.sunLight.shadow.camera.left = -25;
    this.sunLight.shadow.camera.right = 25;
    this.sunLight.shadow.camera.top = 25;
    this.sunLight.shadow.camera.bottom = -25;
    this.sunLight.shadow.bias = -0.0005;
    this.scene.add(this.sunLight);

    // Subtle blue fill light
    const fillLight = new THREE.DirectionalLight(0x738bd6, 0.5);
    fillLight.position.set(-20, 15, -20);
    this.scene.add(fillLight);

    this.updateSunPosition();
  }

  updateSunPosition() {
    const azimuthRad = THREE.MathUtils.degToRad(this.params.orientation_azimuth + 180);
    const altitudeRad = THREE.MathUtils.degToRad(42);
    const radius = 40;

    const x = radius * Math.sin(azimuthRad) * Math.cos(altitudeRad);
    const y = radius * Math.sin(altitudeRad);
    const z = radius * Math.cos(azimuthRad) * Math.cos(altitudeRad);

    this.sunLight.position.set(x, y, z);
  }

  setupEnvironment() {
    // Studio ground plinth
    const plinthGeo = new THREE.CylinderGeometry(32, 34, 0.4, 64);
    const plinthMat = new THREE.MeshStandardMaterial({
      color: 0x141822,
      roughness: 0.85,
      metalness: 0.1
    });
    const plinth = new THREE.Mesh(plinthGeo, plinthMat);
    plinth.position.y = -0.22;
    plinth.receiveShadow = true;
    this.scene.add(plinth);

    // Modern studio grid helper
    const grid = new THREE.GridHelper(50, 50, 0x334155, 0x1e293b);
    grid.position.y = 0.01;
    this.scene.add(grid);
  }

  /**
   * Rebuilds the parametric building geometry based on current parameters.
   */
  rebuildArchitecture() {
    // Clear previous children
    while (this.buildingGroup.children.length > 0) {
      const obj = this.buildingGroup.children.pop();
      if (obj.geometry) obj.geometry.dispose();
      if (obj.material) {
        if (Array.isArray(obj.material)) obj.material.forEach(m => m.dispose());
        else obj.material.dispose();
      }
    }

    const { room_depth, room_width, wwr, louver_angle, overhang_depth } = this.params;
    const height = 4.2;
    const clipPlanes = this.clippingEnabled ? [this.sectionPlane] : [];

    // Common materials
    const concreteMat = new THREE.MeshStandardMaterial({
      color: 0x384152,
      roughness: 0.7,
      metalness: 0.1,
      clippingPlanes: clipPlanes,
      wireframe: this.showWireframe
    });

    const glassMat = new THREE.MeshPhysicalMaterial({
      color: 0x88ccff,
      transmission: 0.88,
      opacity: 1,
      transparent: true,
      roughness: 0.12,
      ior: 1.52,
      thickness: 0.3,
      clippingPlanes: clipPlanes
    });

    const louverMat = new THREE.MeshStandardMaterial({
      color: 0x94a3b8,
      metalness: 0.8,
      roughness: 0.25,
      clippingPlanes: clipPlanes,
      wireframe: this.showWireframe
    });

    // 1. Lower Floor Slab with Heatmap Surface
    const slabThickness = 0.35;
    const slabGeo = new THREE.BoxGeometry(room_width + 1.2, slabThickness, room_depth + 1.2);
    const slabMesh = new THREE.Mesh(slabGeo, concreteMat);
    slabMesh.position.set(0, slabThickness / 2, 0);
    slabMesh.receiveShadow = true;
    slabMesh.castShadow = true;
    this.buildingGroup.add(slabMesh);

    // 2. Interior Floor Surface with Per-Vertex Color Heatmap
    this.createHeatmapSurface(room_width, room_depth, slabThickness + 0.01, clipPlanes);

    // 3. Roof Slab with Parametric Overhang
    const roofOverhang = overhang_depth + 0.6;
    const roofGeo = new THREE.BoxGeometry(room_width + 1.2, slabThickness, room_depth + roofOverhang);
    const roofMesh = new THREE.Mesh(roofGeo, concreteMat);
    roofMesh.position.set(0, height + slabThickness + slabThickness / 2, -roofOverhang / 2 + 0.3);
    roofMesh.castShadow = true;
    roofMesh.receiveShadow = true;
    this.buildingGroup.add(roofMesh);

    // 4. Structural Columns (Concrete / Steel circular columns)
    const colRadius = 0.24;
    const colGeo = new THREE.CylinderGeometry(colRadius, colRadius, height, 24);
    const colPositions = [
      [-room_width / 2 + 0.4, (room_depth / 2) - 0.4],
      [room_width / 2 - 0.4, (room_depth / 2) - 0.4],
      [-room_width / 2 + 0.4, -(room_depth / 2) + 0.4],
      [room_width / 2 - 0.4, -(room_depth / 2) + 0.4]
    ];
    colPositions.forEach(([x, z]) => {
      const col = new THREE.Mesh(colGeo, concreteMat);
      col.position.set(x, height / 2 + slabThickness, z);
      col.castShadow = true;
      col.receiveShadow = true;
      this.buildingGroup.add(col);
    });

    // 5. Opaque Walls (North / Rear Wall, Left and Right Side Walls)
    const wallThick = 0.28;
    // North Wall (+Z)
    const northWallGeo = new THREE.BoxGeometry(room_width, height, wallThick);
    const northWall = new THREE.Mesh(northWallGeo, concreteMat);
    northWall.position.set(0, height / 2 + slabThickness, room_depth / 2);
    northWall.castShadow = true;
    northWall.receiveShadow = true;
    this.buildingGroup.add(northWall);

    // Left Wall (-X)
    const sideWallGeo = new THREE.BoxGeometry(wallThick, height, room_depth);
    const leftWall = new THREE.Mesh(sideWallGeo, concreteMat);
    leftWall.position.set(-room_width / 2, height / 2 + slabThickness, 0);
    leftWall.castShadow = true;
    leftWall.receiveShadow = true;
    this.buildingGroup.add(leftWall);

    // Right Wall (+X)
    const rightWall = new THREE.Mesh(sideWallGeo, concreteMat);
    rightWall.position.set(room_width / 2, height / 2 + slabThickness, 0);
    rightWall.castShadow = true;
    rightWall.receiveShadow = true;
    this.buildingGroup.add(rightWall);

    // 6. South Facade Window Glazing (Parametric WWR)
    const glassW = room_width * Math.sqrt(wwr);
    const glassH = height * Math.sqrt(wwr);
    const glassGeo = new THREE.BoxGeometry(glassW, glassH, 0.08);
    const glassMesh = new THREE.Mesh(glassGeo, glassMat);
    glassMesh.position.set(0, slabThickness + glassH / 2 + (height - glassH) / 2, -room_depth / 2);
    this.buildingGroup.add(glassMesh);

    // South facade spandrel wall below/above glazing
    const sillHeight = (height - glassH) / 2;
    if (sillHeight > 0.05) {
      const sillGeo = new THREE.BoxGeometry(room_width, sillHeight, wallThick);
      const sillMesh = new THREE.Mesh(sillGeo, concreteMat);
      sillMesh.position.set(0, slabThickness + sillHeight / 2, -room_depth / 2);
      this.buildingGroup.add(sillMesh);

      const headerGeo = new THREE.BoxGeometry(room_width, sillHeight, wallThick);
      const headerMesh = new THREE.Mesh(headerGeo, concreteMat);
      headerMesh.position.set(0, slabThickness + height - sillHeight / 2, -room_depth / 2);
      this.buildingGroup.add(headerMesh);
    }

    // 7. Parametric Exterior Shading Louvers
    const numLouvers = 8;
    const louverSpacing = glassH / numLouvers;
    const louverRad = THREE.MathUtils.degToRad(louver_angle);
    const louverDepth = Math.max(0.6, overhang_depth * 0.85);

    for (let i = 0; i < numLouvers; i++) {
      const louverGeo = new THREE.BoxGeometry(glassW + 0.6, 0.05, louverDepth);
      const louverMesh = new THREE.Mesh(louverGeo, louverMat);
      const yPos = slabThickness + (height - glassH) / 2 + i * louverSpacing + louverSpacing / 2;
      louverMesh.position.set(0, yPos, -room_depth / 2 - louverDepth / 2 - 0.1);
      louverMesh.rotation.x = louverRad;
      louverMesh.castShadow = true;
      this.buildingGroup.add(louverMesh);
    }
  }

  /**
   * Creates a high-resolution grid surface mesh and calculates per-vertex false colors
   * corresponding to the active regression metric (Daylight, Solar, Stress).
   */
  createHeatmapSurface(width, depth, yLevel, clipPlanes) {
    const segX = 36;
    const segZ = 36;
    const geo = new THREE.PlaneGeometry(width - 0.2, depth - 0.2, segX, segZ);
    geo.rotateX(-Math.PI / 2);

    const count = geo.attributes.position.count;
    const colors = new Float32Array(count * 3);
    const positions = geo.attributes.position;

    const { wwr, louver_angle, overhang_depth, room_depth } = this.params;

    for (let i = 0; i < count; i++) {
      const x = positions.getX(i);
      const z = positions.getZ(i);

      // Distance from South Window (-depth / 2)
      const distFromWindow = z - (-depth / 2);
      const normDist = Math.max(0, Math.min(1, distFromWindow / depth));

      // Normalized lateral coordinate
      const normX = Math.abs(x) / (width / 2);

      let scalar = 0.5;

      if (this.activeMetric === 'sda_percent') {
        // Daylight illuminance decreases exponentially with depth from window
        const windowTrans = Math.sqrt(wwr) * Math.cos(THREE.MathUtils.degToRad(louver_angle)) * (1.0 - 0.2 * overhang_depth);
        const dayLight = Math.exp(-normDist * (2.2 + room_depth * 0.05)) * windowTrans * 1.5;
        // Corner falloff
        scalar = Math.max(0.05, Math.min(1.0, dayLight * (1.0 - normX * 0.25)));
      } else if (this.activeMetric === 'cooling_load') {
        // Solar heat gain concentration near window and periphery
        const solarZone = Math.max(0, 1.0 - normDist * 1.8) * Math.sqrt(wwr);
        scalar = Math.max(0.1, Math.min(0.95, 0.2 + solarZone * 0.75 + (1 - normDist) * 0.1));
      } else if (this.activeMetric === 'peak_stress') {
        // Bending moment peaks at mid-span, shear peaks at column supports
        const spanDist = Math.sin((normDist) * Math.PI) * Math.cos((x / (width / 2)) * (Math.PI / 2.5));
        scalar = Math.max(0.1, Math.min(1.0, 0.15 + spanDist * 0.8));
      } else {
        // Embodied carbon distribution
        scalar = 0.35 + (normX * 0.3) + (1.0 - normDist) * 0.35;
      }

      // Map scalar [0, 1] to RGB
      const [r, g, b] = this.sampleColormap(scalar, this.colormap);
      colors[i * 3] = r;
      colors[i * 3 + 1] = g;
      colors[i * 3 + 2] = b;
    }

    geo.setAttribute('color', new THREE.BufferAttribute(colors, 3));

    const mat = new THREE.MeshStandardMaterial({
      vertexColors: true,
      roughness: 0.35,
      metalness: 0.1,
      clippingPlanes: clipPlanes,
      wireframe: this.showWireframe
    });

    this.heatmapMesh = new THREE.Mesh(geo, mat);
    this.heatmapMesh.position.set(0, yLevel, 0);
    this.heatmapMesh.receiveShadow = true;
    this.buildingGroup.add(this.heatmapMesh);
  }

  /**
   * Colormap interpolator (Turbo, Viridis, Plasma, CoolWarm).
   */
  sampleColormap(val, mapType) {
    const t = Math.max(0, Math.min(1, val));
    if (mapType === 'viridis') {
      // Viridis polynomial
      const r = Math.max(0, Math.min(1, 0.28 + 0.75 * t - 0.25 * t * t));
      const g = Math.max(0, Math.min(1, 0.05 + 1.25 * t - 0.45 * t * t));
      const b = Math.max(0, Math.min(1, 0.55 - 0.5 * t + 0.65 * (1 - t) * (1 - t)));
      return [r, g, b];
    } else if (mapType === 'plasma') {
      const r = Math.max(0, Math.min(1, 0.05 + 1.35 * t - 0.4 * t * t));
      const g = Math.max(0, Math.min(1, 0.02 + 0.6 * t + 0.38 * t * t));
      const b = Math.max(0, Math.min(1, 0.55 + 0.8 * (1 - t) - 0.4 * (1 - t) * (1 - t)));
      return [r, g, b];
    } else {
      // Default: Turbo colormap
      const r = Math.max(0, Math.min(1, 0.15 + 2.2 * t - 1.35 * t * t));
      const g = Math.max(0, Math.min(1, 0.1 + 1.8 * Math.sin(t * Math.PI) - 0.2 * t));
      const b = Math.max(0, Math.min(1, 0.85 - 1.8 * t + 1.0 * t * t));
      return [r, g, b];
    }
  }

  /**
   * Builds the 3D Spatial Adjacency Graph Overlay with glowing nodes and connecting chords.
   */
  buildSpatialGraph() {
    while (this.spatialGraphGroup.children.length > 0) {
      const child = this.spatialGraphGroup.children.pop();
      if (child.geometry) child.geometry.dispose();
      if (child.material) child.material.dispose();
    }

    if (!this.showSpatialGraph) return;

    // Room nodes data
    const nodes = [
      { id: 'atrium', label: 'Central Atrium', pos: [0, 2.2, 0], color: 0x38bdf8 },
      { id: 'south_office', label: 'South Daylit Zone', pos: [0, 2.2, -4.5], color: 0x4ade80 },
      { id: 'east_conf', label: 'East Collab Suite', pos: [4.2, 2.2, 1.5], color: 0xa855f7 },
      { id: 'west_lab', label: 'West Comput. Lab', pos: [-4.2, 2.2, 1.5], color: 0xf59e0b },
      { id: 'north_core', label: 'Circulation Core', pos: [0, 2.2, 4.5], color: 0xef4444 }
    ];

    const edges = [
      ['atrium', 'south_office'],
      ['atrium', 'east_conf'],
      ['atrium', 'west_lab'],
      ['atrium', 'north_core'],
      ['east_conf', 'north_core'],
      ['west_lab', 'north_core']
    ];

    const nodeMap = new Map();

    // Create 3D Nodes
    nodes.forEach(node => {
      const nodeGeo = new THREE.SphereGeometry(0.42, 24, 24);
      const nodeMat = new THREE.MeshStandardMaterial({
        color: node.color,
        emissive: node.color,
        emissiveIntensity: 0.6,
        roughness: 0.2
      });
      const nodeMesh = new THREE.Mesh(nodeGeo, nodeMat);
      nodeMesh.position.set(node.pos[0], node.pos[1], node.pos[2]);

      // Glowing outer ring
      const ringGeo = new THREE.RingGeometry(0.55, 0.72, 32);
      ringGeo.rotateX(-Math.PI / 2);
      const ringMat = new THREE.MeshBasicMaterial({
        color: node.color,
        side: THREE.DoubleSide,
        transparent: true,
        opacity: 0.6
      });
      const ringMesh = new THREE.Mesh(ringGeo, ringMat);
      nodeMesh.add(ringMesh);

      this.spatialGraphGroup.add(nodeMesh);
      nodeMap.set(node.id, nodeMesh.position);
    });

    // Create 3D Connection Chords (Adjacency Graph Edges)
    edges.forEach(([idA, idB]) => {
      const posA = nodeMap.get(idA);
      const posB = nodeMap.get(idB);
      if (!posA || !posB) return;

      const points = [posA, posB];
      const lineGeo = new THREE.BufferGeometry().setFromPoints(points);
      const lineMat = new THREE.LineBasicMaterial({
        color: 0x38bdf8,
        transparent: true,
        opacity: 0.75,
        linewidth: 2
      });
      const line = new THREE.Line(lineGeo, lineMat);
      this.spatialGraphGroup.add(line);
    });
  }

  updateParameters(newParams) {
    this.params = { ...this.params, ...newParams };
    this.updateSunPosition();
    this.rebuildArchitecture();
    this.buildSpatialGraph();
  }

  setMetric(metricName) {
    this.activeMetric = metricName;
    this.rebuildArchitecture();
  }

  setColormap(cmapName) {
    this.colormap = cmapName;
    this.rebuildArchitecture();
  }

  toggleSpatialGraph(visible) {
    this.showSpatialGraph = visible;
    this.buildSpatialGraph();
  }

  toggleWireframe(wireframe) {
    this.showWireframe = wireframe;
    this.rebuildArchitecture();
  }

  setSectionClipping(enabled, depthRatio = 0.5) {
    this.clippingEnabled = enabled;
    if (enabled) {
      const zCut = (depthRatio - 0.5) * this.params.room_depth;
      this.sectionPlane.constant = zCut;
    }
    this.rebuildArchitecture();
  }

  setCameraView(viewPreset) {
    switch (viewPreset) {
      case 'iso':
        this.camera.position.set(22, 18, 26);
        this.controls.target.set(0, 2.5, 0);
        break;
      case 'top':
        this.camera.position.set(0, 36, 0.001);
        this.controls.target.set(0, 0, 0);
        break;
      case 'south':
        this.camera.position.set(0, 4, -28);
        this.controls.target.set(0, 2.5, 0);
        break;
      case 'east':
        this.camera.position.set(28, 4, 0);
        this.controls.target.set(0, 2.5, 0);
        break;
    }
    this.controls.update();
  }

  /**
   * Exports the live 3D building mesh with per-vertex attributes to binary glTF (.glb)
   */
  exportGLB() {
    const exporter = new GLTFExporter();
    exporter.parse(
      this.buildingGroup,
      (gltf) => {
        const blob = new Blob([gltf], { type: 'application/octet-stream' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `arch_regression_design_${Date.now()}.glb`;
        link.click();
        URL.revokeObjectURL(link.href);
      },
      (error) => {
        console.error('[SpatialViewport] GLTF Export failed:', error);
      },
      { binary: true }
    );
  }

  onWindowResize() {
    if (!this.container || !this.renderer || !this.camera) return;
    const width = this.container.clientWidth;
    const height = this.container.clientHeight;
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(width, height);
  }

  animate() {
    requestAnimationFrame(this.animate);
    if (this.controls) this.controls.update();
    if (this.renderer && this.scene && this.camera) {
      this.renderer.render(this.scene, this.camera);
    }
  }
}
