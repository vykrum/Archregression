/**
 * src/viewer/viewport.js
 * ======================
 * Three.js 3D Spatial Viewer with Variable Environmental Context:
 * - 3D Mountains & Sloped Topography Mesh
 * - Water Bodies (Lakes / Oceans) with Shimmering Surface
 * - Natural Tree Canopies & Vegetation with Dynamic Shadows
 * - Neighboring Urban Massings & Shading Obstacles
 * - Interactive Click-to-Plant Trees Tool
 * - Procedural Architectural Pavilion with Per-Vertex Surface Heatmap
 * - 3D Spatial Room Adjacency Graph Overlays
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
    this.hemiLight = null;
    this.buildingGroup = null;
    this.heatmapMesh = null;
    this.spatialGraphGroup = null;
    this.sectionPlane = null;
    this.clippingEnabled = false;

    // Environmental & Natural Context Groups
    this.environmentGroup = null;
    this.terrainMesh = null;
    this.waterMesh = null;
    this.treesGroup = null;
    this.urbanGroup = null;
    this.customTrees = [];

    // State
    this.activeMetric = 'sda_percent';
    this.colormap = 'turbo';
    this.showSpatialGraph = true;
    this.showWireframe = false;
    this.plantTreeToolActive = false;
    this.sitePreset = 'coastal'; // 'coastal' | 'mountain' | 'forest' | 'urban' | 'studio'

    // Environmental Parameters
    this.envParams = {
      site_preset: 'coastal',
      tree_density: 0.65,
      water_proximity: 18.0, // meters to shoreline
      terrain_slope: 12.0,   // degrees
      mountain_height: 25.0, // max ridge altitude
      urban_shadows: false,
      has_water: true
    };

    // Building Parameters
    this.params = {
      room_depth: 14.0,
      room_width: 12.0,
      wwr: 0.55,
      louver_angle: 35.0,
      overhang_depth: 1.0,
      orientation_azimuth: 0.0,
      glazing_u_val: 1.8
    };

    this.raycaster = new THREE.Raycaster();
    this.mouse = new THREE.Vector2();

    this.init();
  }

  init() {
    const width = this.container.clientWidth || 800;
    const height = this.container.clientHeight || 600;

    // 1. Scene
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x0a0e17);
    this.scene.fog = new THREE.FogExp2(0x0a0e17, 0.008);

    // 2. Camera
    this.camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1500);
    this.camera.position.set(28, 22, 34);

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
    this.controls.dampingFactor = 0.05;
    this.controls.maxPolarAngle = Math.PI / 2 + 0.02;
    this.controls.target.set(0, 2.5, 0);

    // 5. Section Clipping Plane
    this.sectionPlane = new THREE.Plane(new THREE.Vector3(0, 0, -1), 100);

    // 6. Lighting Rig
    this.setupLighting();

    // 7. Environment & Natural Elements Group
    this.environmentGroup = new THREE.Group();
    this.scene.add(this.environmentGroup);

    this.treesGroup = new THREE.Group();
    this.environmentGroup.add(this.treesGroup);

    this.urbanGroup = new THREE.Group();
    this.environmentGroup.add(this.urbanGroup);

    // 8. Architectural Geometry Container
    this.buildingGroup = new THREE.Group();
    this.scene.add(this.buildingGroup);

    // 9. Spatial Adjacency Graph Group
    this.spatialGraphGroup = new THREE.Group();
    this.scene.add(this.spatialGraphGroup);

    // 10. Generate Initial Environment and Geometry
    this.buildEnvironment();
    this.rebuildArchitecture();
    this.buildSpatialGraph();

    // 11. Event Listeners
    window.addEventListener('resize', () => this.onWindowResize());
    this.renderer.domElement.addEventListener('click', (e) => this.onCanvasClick(e));

    // 12. Animation Loop
    this.animate = this.animate.bind(this);
    requestAnimationFrame(this.animate);
  }

  setupLighting() {
    this.hemiLight = new THREE.HemisphereLight(0xdce7f9, 0x1a2130, 0.75);
    this.hemiLight.position.set(0, 80, 0);
    this.scene.add(this.hemiLight);

    this.sunLight = new THREE.DirectionalLight(0xfff5e4, 2.0);
    this.sunLight.castShadow = true;
    this.sunLight.shadow.mapSize.width = 2048;
    this.sunLight.shadow.mapSize.height = 2048;
    this.sunLight.shadow.camera.near = 0.5;
    this.sunLight.shadow.camera.far = 250;
    this.sunLight.shadow.camera.left = -50;
    this.sunLight.shadow.camera.right = 50;
    this.sunLight.shadow.camera.top = 50;
    this.sunLight.shadow.camera.bottom = -50;
    this.sunLight.shadow.bias = -0.0004;
    this.scene.add(this.sunLight);

    const fillLight = new THREE.DirectionalLight(0x60a5fa, 0.45);
    fillLight.position.set(-40, 25, -40);
    this.scene.add(fillLight);

    this.updateSunPosition();
  }

  updateSunPosition() {
    const azimuthRad = THREE.MathUtils.degToRad(this.params.orientation_azimuth + 180);
    const altitudeRad = THREE.MathUtils.degToRad(42);
    const radius = 60;

    const x = radius * Math.sin(azimuthRad) * Math.cos(altitudeRad);
    const y = radius * Math.sin(altitudeRad);
    const z = radius * Math.cos(azimuthRad) * Math.cos(altitudeRad);

    this.sunLight.position.set(x, y, z);
  }

  /**
   * Builds the natural site context based on the active preset:
   * - Alpine Mountain Ridge (Displaced terrain, snow peaks, rocky slope)
   * - Lakefront / Coastal Shoreline (Water plane with reflections, sandy shore)
   * - Forested Canopy (Dense tree belts, mossy grass, biophilic context)
   * - Urban Canyon (Surrounding context towers)
   * - Studio Plinth (Clean engineering floor)
   */
  buildEnvironment() {
    // Clear previous environment meshes
    if (this.terrainMesh) {
      this.terrainMesh.geometry.dispose();
      this.environmentGroup.remove(this.terrainMesh);
      this.terrainMesh = null;
    }
    if (this.waterMesh) {
      this.waterMesh.geometry.dispose();
      this.environmentGroup.remove(this.waterMesh);
      this.waterMesh = null;
    }
    while (this.treesGroup.children.length > 0) {
      const t = this.treesGroup.children.pop();
      if (t.geometry) t.geometry.dispose();
    }
    while (this.urbanGroup.children.length > 0) {
      const u = this.urbanGroup.children.pop();
      if (u.geometry) u.geometry.dispose();
    }

    const preset = this.sitePreset;

    if (preset === 'studio') {
      this.buildStudioPlinth();
      return;
    }

    // 1. Topography / Terrain Mesh
    const terrainSize = 120;
    const segments = 64;
    const terrainGeo = new THREE.PlaneGeometry(terrainSize, terrainSize, segments, segments);
    terrainGeo.rotateX(-Math.PI / 2);

    const pos = terrainGeo.attributes.position;
    const colors = new Float32Array(pos.count * 3);

    for (let i = 0; i < pos.count; i++) {
      const x = pos.getX(i);
      const z = pos.getZ(i);

      let elevation = 0;

      // Keep building parcel relatively flat around (0,0)
      const distFromCenter = Math.sqrt(x * x + z * z);
      const parcelFlatten = Math.min(1.0, Math.max(0.0, (distFromCenter - 14) / 25));

      if (preset === 'mountain') {
        // Alpine mountain ridge rising to the North and East
        const mountainRidge = Math.sin(x * 0.05 + 1.2) * Math.cos(z * 0.04) * 18.0;
        const slope = (z * 0.25) + (x * 0.15);
        elevation = (mountainRidge + slope + 8) * parcelFlatten;
      } else if (preset === 'coastal') {
        // Gentle slope towards water body on South (-Z)
        const coastalSlope = Math.max(-2.5, (z + 12) * 0.12);
        elevation = coastalSlope * parcelFlatten;
      } else if (preset === 'forest') {
        // Gentle rolling terrain
        const undulation = Math.sin(x * 0.1) * Math.cos(z * 0.1) * 3.5;
        elevation = undulation * parcelFlatten;
      } else if (preset === 'urban') {
        elevation = 0;
      }

      pos.setY(i, elevation - 0.2);

      // Color mapping based on elevation & slope
      let r = 0.12, g = 0.20, b = 0.14; // Default grass green
      if (preset === 'mountain' && elevation > 14.0) {
        // Rock & snow
        r = 0.75; g = 0.78; b = 0.85;
      } else if (preset === 'mountain' && elevation > 7.0) {
        // Alpine rock slate
        r = 0.32; g = 0.35; b = 0.40;
      } else if (preset === 'coastal' && elevation < 0.2) {
        // Coastal sandy shoreline
        r = 0.65; g = 0.60; b = 0.45;
      } else if (preset === 'forest') {
        // Rich deep forest soil & moss
        r = 0.08; g = 0.24; b = 0.12;
      }

      colors[i * 3] = r;
      colors[i * 3 + 1] = g;
      colors[i * 3 + 2] = b;
    }

    terrainGeo.setAttribute('color', new THREE.BufferAttribute(colors, 3));
    terrainGeo.computeVertexNormals();

    const terrainMat = new THREE.MeshStandardMaterial({
      vertexColors: true,
      roughness: 0.85,
      metalness: 0.05,
      flatShading: true
    });

    this.terrainMesh = new THREE.Mesh(terrainGeo, terrainMat);
    this.terrainMesh.receiveShadow = true;
    this.environmentGroup.add(this.terrainMesh);

    // 2. Water Feature (Lakes / Sea / Coastal Shoreline)
    if (preset === 'coastal' || this.envParams.has_water) {
      const waterSize = 140;
      const waterGeo = new THREE.PlaneGeometry(waterSize, 60, 32, 16);
      waterGeo.rotateX(-Math.PI / 2);

      const waterMat = new THREE.MeshStandardMaterial({
        color: 0x1e3a8a,
        roughness: 0.1,
        metalness: 0.85,
        transparent: true,
        opacity: 0.88
      });

      this.waterMesh = new THREE.Mesh(waterGeo, waterMat);
      this.waterMesh.position.set(0, -0.4, -45); // South water body
      this.waterMesh.receiveShadow = true;
      this.environmentGroup.add(this.waterMesh);
    }

    // 3. Tree Canopies & Vegetation
    if (preset === 'forest' || preset === 'coastal' || preset === 'mountain') {
      const count = preset === 'forest' ? 36 : preset === 'coastal' ? 18 : 12;
      this.spawnProceduralTrees(count);
    }

    // 4. Urban Context Buildings
    if (preset === 'urban' || this.envParams.urban_shadows) {
      this.spawnUrbanBuildings();
    }
  }

  buildStudioPlinth() {
    const plinthGeo = new THREE.CylinderGeometry(34, 36, 0.4, 64);
    const plinthMat = new THREE.MeshStandardMaterial({
      color: 0x141822,
      roughness: 0.85,
      metalness: 0.1
    });
    this.terrainMesh = new THREE.Mesh(plinthGeo, plinthMat);
    this.terrainMesh.position.y = -0.22;
    this.terrainMesh.receiveShadow = true;
    this.environmentGroup.add(this.terrainMesh);

    const grid = new THREE.GridHelper(50, 50, 0x334155, 0x1e293b);
    grid.position.y = 0.01;
    this.environmentGroup.add(grid);
  }

  /**
   * Spawns realistic 3D tree canopies (foliage + trunks) around the parcel.
   */
  spawnProceduralTrees(numTrees) {
    const trunkMat = new THREE.MeshStandardMaterial({ color: 0x4a2e18, roughness: 0.9 });
    const foliageColors = [0x15803d, 0x166534, 0x22c55e, 0x047857, 0x84cc16];

    for (let i = 0; i < numTrees; i++) {
      // Position around building perimeter (avoiding building footprint)
      const angle = Math.random() * Math.PI * 2;
      const dist = 14.0 + Math.random() * 32.0;
      const x = Math.cos(angle) * dist;
      const z = Math.sin(angle) * dist;

      // Don't place deep in South water if coastal
      if (this.sitePreset === 'coastal' && z < -18) continue;

      const scale = 0.7 + Math.random() * 0.6;
      this.createTree(x, z, scale, trunkMat, foliageColors);
    }

    // Re-add custom user-planted trees
    this.customTrees.forEach(t => {
      this.createTree(t.x, t.z, t.scale || 1.0, trunkMat, foliageColors);
    });
  }

  createTree(x, z, scale, trunkMat, foliageColors) {
    const treeGroup = new THREE.Group();

    // Trunk
    const trunkHeight = 2.4 * scale;
    const trunkGeo = new THREE.CylinderGeometry(0.18 * scale, 0.28 * scale, trunkHeight, 8);
    const trunkMesh = new THREE.Mesh(trunkGeo, trunkMat);
    trunkMesh.position.y = trunkHeight / 2;
    trunkMesh.castShadow = true;
    trunkMesh.receiveShadow = true;
    treeGroup.add(trunkMesh);

    // Foliage Canopy (Double tiered cone/sphere for natural canopy look)
    const foliageColor = foliageColors[Math.floor(Math.random() * foliageColors.length)];
    const foliageMat = new THREE.MeshStandardMaterial({
      color: foliageColor,
      roughness: 0.75,
      metalness: 0.1,
      flatShading: true
    });

    const canopyRadius = (1.8 + Math.random() * 0.8) * scale;
    const canopyHeight = 4.0 * scale;
    const canopyGeo = new THREE.ConeGeometry(canopyRadius, canopyHeight, 8);
    const canopyMesh = new THREE.Mesh(canopyGeo, foliageMat);
    canopyMesh.position.y = trunkHeight + canopyHeight / 2 - 0.4;
    canopyMesh.castShadow = true;
    canopyMesh.receiveShadow = true;
    treeGroup.add(canopyMesh);

    // Second lower foliage tier
    const tierGeo = new THREE.ConeGeometry(canopyRadius * 1.25, canopyHeight * 0.65, 8);
    const tierMesh = new THREE.Mesh(tierGeo, foliageMat);
    tierMesh.position.y = trunkHeight + canopyHeight * 0.25;
    tierMesh.castShadow = true;
    treeGroup.add(tierMesh);

    // Get terrain height at (x, z)
    const yPos = this.getTerrainHeightAt(x, z);
    treeGroup.position.set(x, yPos, z);

    this.treesGroup.add(treeGroup);
  }

  getTerrainHeightAt(x, z) {
    if (!this.terrainMesh || this.sitePreset === 'studio') return 0;
    // Approximation matching terrain generation function
    const dist = Math.sqrt(x * x + z * z);
    const flatten = Math.min(1.0, Math.max(0.0, (dist - 14) / 25));
    if (this.sitePreset === 'mountain') {
      const mountainRidge = Math.sin(x * 0.05 + 1.2) * Math.cos(z * 0.04) * 18.0;
      return (mountainRidge + (z * 0.25) + (x * 0.15) + 8) * flatten - 0.2;
    } else if (this.sitePreset === 'coastal') {
      return Math.max(-2.5, (z + 12) * 0.12) * flatten - 0.2;
    }
    return 0;
  }

  /**
   * Spawns surrounding urban towers casting real urban shadows onto the parcel.
   */
  spawnUrbanBuildings() {
    const buildingMat = new THREE.MeshStandardMaterial({
      color: 0x334155,
      roughness: 0.4,
      metalness: 0.2
    });

    const contextBuildings = [
      { x: -28, z: -15, w: 18, d: 22, h: 28 },
      { x: 26, z: -18, w: 16, d: 20, h: 24 },
      { x: -30, z: 22, w: 20, d: 18, h: 35 },
      { x: 28, z: 24, w: 22, d: 24, h: 32 }
    ];

    contextBuildings.forEach(b => {
      const geo = new THREE.BoxGeometry(b.w, b.h, b.d);
      const mesh = new THREE.Mesh(geo, buildingMat);
      mesh.position.set(b.x, b.h / 2, b.z);
      mesh.castShadow = true;
      mesh.receiveShadow = true;

      // Subtle edge line
      const edges = new THREE.EdgesGeometry(geo);
      const line = new THREE.LineSegments(edges, new THREE.LineBasicMaterial({ color: 0x64748b, transparent: true, opacity: 0.4 }));
      mesh.add(line);

      this.urbanGroup.add(mesh);
    });
  }

  setSitePreset(presetKey) {
    this.sitePreset = presetKey;
    this.envParams.site_preset = presetKey;
    this.buildEnvironment();
    this.rebuildArchitecture();
  }

  togglePlantTreeTool(active) {
    this.plantTreeToolActive = active;
  }

  onCanvasClick(event) {
    if (!this.plantTreeToolActive || !this.terrainMesh) return;

    const rect = this.renderer.domElement.getBoundingClientRect();
    this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

    this.raycaster.setFromCamera(this.mouse, this.camera);
    const intersects = this.raycaster.intersectObject(this.terrainMesh);

    if (intersects.length > 0) {
      const p = intersects[0].point;
      // Do not plant inside the building footprint
      if (Math.abs(p.x) < this.params.room_width / 2 + 1 && Math.abs(p.z) < this.params.room_depth / 2 + 1) {
        return;
      }
      this.customTrees.push({ x: p.x, z: p.z, scale: 0.9 + Math.random() * 0.4 });
      const trunkMat = new THREE.MeshStandardMaterial({ color: 0x4a2e18, roughness: 0.9 });
      const foliageColors = [0x15803d, 0x166534, 0x22c55e, 0x047857];
      this.createTree(p.x, p.z, 1.0, trunkMat, foliageColors);

      // Dispatch event for UI
      if (window.dispatchEvent) {
        window.dispatchEvent(new CustomEvent('treePlanted', { detail: { count: this.customTrees.length } }));
      }
    }
  }

  /**
   * Rebuilds the parametric building geometry based on current parameters.
   */
  rebuildArchitecture() {
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

    // 4. Structural Columns
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

    // 5. Opaque Walls
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

    // Spandrel wall
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

      const distFromWindow = z - (-depth / 2);
      const normDist = Math.max(0, Math.min(1, distFromWindow / depth));
      const normX = Math.abs(x) / (width / 2);

      let scalar = 0.5;

      if (this.activeMetric === 'sda_percent') {
        const windowTrans = Math.sqrt(wwr) * Math.cos(THREE.MathUtils.degToRad(louver_angle)) * (1.0 - 0.2 * overhang_depth);
        const dayLight = Math.exp(-normDist * (2.2 + room_depth * 0.05)) * windowTrans * 1.5;
        scalar = Math.max(0.05, Math.min(1.0, dayLight * (1.0 - normX * 0.25)));
      } else if (this.activeMetric === 'cooling_load') {
        const solarZone = Math.max(0, 1.0 - normDist * 1.8) * Math.sqrt(wwr);
        scalar = Math.max(0.1, Math.min(0.95, 0.2 + solarZone * 0.75 + (1 - normDist) * 0.1));
      } else if (this.activeMetric === 'peak_stress') {
        const spanDist = Math.sin((normDist) * Math.PI) * Math.cos((x / (width / 2)) * (Math.PI / 2.5));
        scalar = Math.max(0.1, Math.min(1.0, 0.15 + spanDist * 0.8));
      } else if (this.activeMetric === 'biophilic_view') {
        // Biophilic views highest near facade and corners looking at trees/water
        scalar = Math.max(0.1, Math.min(0.95, (1.0 - normDist * 0.8) * (0.8 + normX * 0.2)));
      } else {
        scalar = 0.35 + (normX * 0.3) + (1.0 - normDist) * 0.35;
      }

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

  sampleColormap(val, mapType) {
    const t = Math.max(0, Math.min(1, val));
    if (mapType === 'viridis') {
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
      const r = Math.max(0, Math.min(1, 0.15 + 2.2 * t - 1.35 * t * t));
      const g = Math.max(0, Math.min(1, 0.1 + 1.8 * Math.sin(t * Math.PI) - 0.2 * t));
      const b = Math.max(0, Math.min(1, 0.85 - 1.8 * t + 1.0 * t * t));
      return [r, g, b];
    }
  }

  buildSpatialGraph() {
    while (this.spatialGraphGroup.children.length > 0) {
      const child = this.spatialGraphGroup.children.pop();
      if (child.geometry) child.geometry.dispose();
      if (child.material) child.material.dispose();
    }

    if (!this.showSpatialGraph) return;

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

    edges.forEach(([idA, idB]) => {
      const posA = nodeMap.get(idA);
      const posB = nodeMap.get(idB);
      if (!posA || !posB) return;

      const lineGeo = new THREE.BufferGeometry().setFromPoints([posA, posB]);
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
        this.camera.position.set(28, 22, 34);
        this.controls.target.set(0, 2.5, 0);
        break;
      case 'top':
        this.camera.position.set(0, 48, 0.001);
        this.controls.target.set(0, 0, 0);
        break;
      case 'south':
        this.camera.position.set(0, 5, -34);
        this.controls.target.set(0, 2.5, 0);
        break;
      case 'east':
        this.camera.position.set(34, 5, 0);
        this.controls.target.set(0, 2.5, 0);
        break;
    }
    this.controls.update();
  }

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

    // Subtle water surface shimmer animation
    if (this.waterMesh) {
      this.waterMesh.rotation.z = Math.sin(Date.now() * 0.0008) * 0.015;
    }

    if (this.renderer && this.scene && this.camera) {
      this.renderer.render(this.scene, this.camera);
    }
  }
}
