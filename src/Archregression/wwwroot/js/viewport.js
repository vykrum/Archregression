/**
 * wwwroot/js/viewport.js
 * ======================
 * Three.js 3D Spatial Viewer with Dynamic Topography & Variable Site Context:
 * - Real 3D Mountain Topography (Alpine ridgelines, rock cliffs, snow peaks)
 * - Ocean & Lake Water Bodies (Wave shimmers, beaches, coastal bluffs)
 * - Biome-Adaptive Vegetation (Alpine Conifers, Coastal Cypress & Palms, Temperate Canopies)
 * - Retaining Foundation Plinth & Terraced Building Parcel
 * - Urban Context Massings & Realistic Shading
 * - Custom Site Builder with dynamic terrain & water sliders
 * - Click-to-Plant Trees Tool
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
    this.foamMesh = null;
    this.foundationPlinth = null;
    this.treesGroup = null;
    this.urbanGroup = null;
    this.roadGroup = null;
    this.roadData = {
      name: 'Pine Street',
      distance: 17.6,
      type: 'local',
      lat: 37.8590,
      lng: -122.4853,
      coords: [[-122.4853, 37.8590], [-122.4851, 37.8589], [-122.4849, 37.8588]],
      ingressCoord: [-122.485133, 37.858913]
    };
    this.customTrees = [];

    // State
    this.activeMetric = 'sda_percent';
    this.colormap = 'turbo';
    this.showSpatialGraph = true;
    this.showWireframe = false;
    this.plantTreeToolActive = false;
    this.sitePreset = 'ocean';
    this.elevationGrid = [
      22.0, 22.0, 17.0, 15.0, 15.0, 14.0, 14.0, 26.0, 37.0,
      17.0, 17.0, 11.0, 10.0, 10.0, 6.0, 6.0, 6.0, 9.0,
      17.0, 17.0, 11.0, 10.0, 10.0, 6.0, 6.0, 6.0, 9.0,
      8.0, 8.0, 7.0, 6.0, 6.0, 4.0, 4.0, 2.0, 3.0,
      8.0, 8.0, 7.0, 6.0, 6.0, 4.0, 4.0, 2.0, 3.0,
      4.0, 4.0, 3.0, 2.0, 2.0, 0.0, 0.0, 0.0, 0.0,
      4.0, 4.0, 3.0, 2.0, 2.0, 0.0, 0.0, 0.0, 0.0,
      3.0, 3.0, 3.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
      3.0, 3.0, 3.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0
    ];
    this.centerElevation = 6.0;
    this.elevationExaggeration = 1.0;

    // Environmental Parameters (Supports both Map-Detected & Custom Site Sliders)
    this.envParams = {
      site_preset: 'ocean',
      mountain_height: 18.0, // Peak height (m)
      terrain_slope: 14.0,   // Slope angle (degrees)
      slope_direction: 'south', // 'south' | 'north' | 'east' | 'west'
      ruggedness: 0.55,      // 0.0 (smooth) to 1.0 (jagged)
      water_type: 'ocean',   // 'none' | 'ocean' | 'lake'
      water_proximity: 20.0, // distance in meters
      water_level: -0.6,
      tree_density: 0.60,
      vegetation_biome: 'coastal', // 'alpine' | 'coastal' | 'temperate'
      urban_shadows: false
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
    this.scene.fog = new THREE.FogExp2(0x0a0e17, 0.007);

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

    this.roadGroup = new THREE.Group();
    this.roadGroup.name = 'roadNetworkGroup';
    this.environmentGroup.add(this.roadGroup);

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

    this.sunLight = new THREE.DirectionalLight(0xfff5e4, 2.1);
    this.sunLight.castShadow = true;
    this.sunLight.shadow.mapSize.width = 2048;
    this.sunLight.shadow.mapSize.height = 2048;
    this.sunLight.shadow.camera.near = 0.5;
    this.sunLight.shadow.camera.far = 300;
    this.sunLight.shadow.camera.left = -60;
    this.sunLight.shadow.camera.right = 60;
    this.sunLight.shadow.camera.top = 60;
    this.sunLight.shadow.camera.bottom = -60;
    this.sunLight.shadow.bias = -0.0004;
    this.scene.add(this.sunLight);

    const fillLight = new THREE.DirectionalLight(0x60a5fa, 0.45);
    fillLight.position.set(-45, 25, -45);
    this.scene.add(fillLight);

    this.updateSunPosition();
  }

  updateSunPosition() {
    const azimuthRad = THREE.MathUtils.degToRad(this.params.orientation_azimuth + 180);
    const altitudeRad = THREE.MathUtils.degToRad(42);
    const radius = 70;

    const x = radius * Math.sin(azimuthRad) * Math.cos(altitudeRad);
    const y = radius * Math.sin(altitudeRad);
    const z = radius * Math.cos(azimuthRad) * Math.cos(altitudeRad);

    this.sunLight.position.set(x, y, z);
  }

  /**
   * 2D Bilinear Interpolation over satellite DEM elevation grid
   */
  sampleDEM(uNorm, vNorm) {
    if (!this.elevationGrid || this.elevationGrid.length < 9) return 0;
    const dim = Math.round(Math.sqrt(this.elevationGrid.length));
    const uClamped = Math.max(0, Math.min(1, uNorm));
    const vClamped = Math.max(0, Math.min(1, vNorm));
    const gx = uClamped * (dim - 1);
    const gz = vClamped * (dim - 1);
    const j0 = Math.floor(gx);
    const j1 = Math.min(dim - 1, j0 + 1);
    const i0 = Math.floor(gz);
    const i1 = Math.min(dim - 1, i0 + 1);
    const tx = gx - j0;
    const tz = gz - i0;
    const e00 = this.elevationGrid[i0 * dim + j0];
    const e10 = this.elevationGrid[i0 * dim + j1];
    const e01 = this.elevationGrid[i1 * dim + j0];
    const e11 = this.elevationGrid[i1 * dim + j1];
    return (1 - tx) * (1 - tz) * e00 + tx * (1 - tz) * e10 + (1 - tx) * tz * e01 + tx * tz * e11;
  }

  /**
   * Applies rich geographic site data from map selection or presets
   */
  applySiteData(site) {
    if (site.elevationGrid && site.elevationGrid.length >= 9) {
      this.elevationGrid = site.elevationGrid;
    }
    if (site.centerElevation !== undefined) {
      this.centerElevation = site.centerElevation;
    } else if (site.elevation !== undefined) {
      this.centerElevation = site.elevation;
    }
    if (site.elevationExaggeration !== undefined) {
      this.elevationExaggeration = site.elevationExaggeration;
    }
    this.sitePreset = site.preset || 'custom';
    this.envParams.site_preset = this.sitePreset;
    if (site.mountainHeight !== undefined) this.envParams.mountain_height = site.mountainHeight;
    if (site.slope !== undefined) this.envParams.terrain_slope = site.slope;
    if (site.slopeDir !== undefined) this.envParams.slope_direction = site.slopeDir;
    if (site.ruggedness !== undefined) this.envParams.ruggedness = Math.min(1.0, site.ruggedness / 300);
    if (site.waterType !== undefined) this.envParams.water_type = site.waterType;
    if (site.waterProximity !== undefined) this.envParams.water_proximity = site.waterProximity;
    if (site.treeDensity !== undefined) this.envParams.tree_density = site.treeDensity;
    if (site.biome !== undefined) this.envParams.vegetation_biome = site.biome;
    if (site.urbanContext !== undefined) this.envParams.urban_shadows = site.urbanContext;

    if (site.roadName !== undefined || site.roadDistance !== undefined) {
      this.roadData = {
        name: site.roadName || 'Pine Street',
        distance: typeof site.roadDistance === 'number' ? site.roadDistance : (this.roadData ? this.roadData.distance : 20.0),
        type: site.roadType || (this.roadData ? this.roadData.type : 'local'),
        lat: site.lat !== undefined ? site.lat : (this.roadData ? this.roadData.lat : 37.8590),
        lng: site.lng !== undefined ? site.lng : (this.roadData ? this.roadData.lng : -122.4853),
        coords: site.roadCoords || (this.roadData ? this.roadData.coords : []),
        ingressCoord: site.ingressCoord || (this.roadData ? this.roadData.ingressCoord : null)
      };
    }

    this.buildEnvironment();
    this.rebuildArchitecture();
  }

  /**
   * Updates individual custom site parameters from sliders
   */
  updateCustomSiteParams(partial) {
    Object.assign(this.envParams, partial);
    this.buildEnvironment();
    this.rebuildArchitecture();
  }

  /**
   * Builds the natural site context & 3D topography:
   * - Alpine Mountain Ridge (Sharp crags, snow caps, rocky slopes)
   * - Ocean Coastline & Bluff (Coastal cliffs, beach line, rolling ocean water)
   * - Inland Lakefront (Freshwater basin, cupping hills, sandy rim)
   * - Forest Canopy (Rolling wooded knolls, rich moss)
   * - Dense Urban Canyon (Shadow-casting high-rises)
   * - Studio Plinth (Minimal engineering floor)
   * - Custom Site (Fully parameterized topography & water)
   */
  buildEnvironment() {
    // 1. Clear previous environment meshes
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
    if (this.foamMesh) {
      this.foamMesh.geometry.dispose();
      this.environmentGroup.remove(this.foamMesh);
      this.foamMesh = null;
    }
    if (this.foundationPlinth) {
      this.foundationPlinth.geometry.dispose();
      this.environmentGroup.remove(this.foundationPlinth);
      this.foundationPlinth = null;
    }

    while (this.treesGroup.children.length > 0) {
      const t = this.treesGroup.children.pop();
      if (t.geometry) t.geometry.dispose();
    }
    while (this.urbanGroup.children.length > 0) {
      const u = this.urbanGroup.children.pop();
      if (u.geometry) u.geometry.dispose();
    }
    if (this.roadGroup) {
      while (this.roadGroup.children.length > 0) {
        const r = this.roadGroup.children.pop();
        if (r.geometry) r.geometry.dispose();
      }
    }

    const preset = this.sitePreset;

    if (preset === 'studio') {
      this.buildStudioPlinth();
      return;
    }

    // Parameters
    const mHeight = this.envParams.mountain_height;
    const slope = this.envParams.terrain_slope;
    const slopeDir = this.envParams.slope_direction;
    const rugged = this.envParams.ruggedness;
    const waterType = this.envParams.water_type;
    const waterProx = this.envParams.water_proximity;
    const biome = this.envParams.vegetation_biome;

    // 2. High-Resolution 3D Topography Mesh
    const terrainSize = 150;
    const segments = 80;
    const terrainGeo = new THREE.PlaneGeometry(terrainSize, terrainSize, segments, segments);
    terrainGeo.rotateX(-Math.PI / 2);

    const pos = terrainGeo.attributes.position;
    const colors = new Float32Array(pos.count * 3);

    // Directional slope vector
    let dirX = 0, dirZ = 0;
    if (slopeDir === 'north') dirZ = 1;
    else if (slopeDir === 'south') dirZ = -1;
    else if (slopeDir === 'east') dirX = 1;
    else if (slopeDir === 'west') dirX = -1;

    for (let i = 0; i < pos.count; i++) {
      const x = pos.getX(i);
      const z = pos.getZ(i);

      // Keep building parcel flat around (0,0) with smoothstep transition
      const distFromCenter = Math.sqrt(x * x + z * z);
      const parcelFlatten = THREE.MathUtils.smoothstep(distFromCenter, 13.0, 26.0);

      let elevation = 0;
      let absElev = this.centerElevation;
      const hasRealDEM = this.elevationGrid && this.elevationGrid.length >= 9;

      if (hasRealDEM) {
        // Satellite DEM Bilinear Sampling across parcel
        const u = (x + terrainSize / 2) / terrainSize;
        const v = (z + terrainSize / 2) / terrainSize;
        absElev = this.sampleDEM(u, v);
        const relElev = (absElev - this.centerElevation) * (this.elevationExaggeration || 1.0);
        elevation = relElev * parcelFlatten;
      } else if (preset === 'mountain' || (preset === 'custom' && mHeight > 10)) {
        const ridge1 = Math.abs(Math.sin(x * 0.045 + 1.2) * Math.cos(z * 0.038)) * mHeight;
        const crags = Math.sin(x * 0.11) * Math.cos(z * 0.12) * (mHeight * 0.28 * rugged);
        const slopeGrad = (z * dirZ + x * dirX) * (slope * 0.035);
        elevation = (ridge1 + crags + slopeGrad) * parcelFlatten;
      } else if (preset === 'ocean' || waterType === 'ocean') {
        const bluffHeight = Math.max(4.0, slope * 0.6);
        const shoreDrop = Math.max(-1.8, Math.min(bluffHeight, (z + waterProx) * 0.22));
        const wavesShore = Math.sin(x * 0.08) * 0.8;
        elevation = (shoreDrop + wavesShore) * parcelFlatten;
      } else if (preset === 'lake' || waterType === 'lake') {
        const distFromLake = Math.sqrt(x * x + Math.pow(z + waterProx, 2));
        const lakeShore = Math.min(12.0, Math.max(-1.2, (distFromLake - 18) * 0.22));
        const undulation = Math.sin(x * 0.06) * Math.cos(z * 0.06) * (mHeight * 0.4);
        elevation = (lakeShore + undulation) * parcelFlatten;
      } else if (preset === 'forest') {
        const undulation = Math.sin(x * 0.08) * Math.cos(z * 0.07) * 4.2 + (z * dirZ * 0.1);
        elevation = undulation * parcelFlatten;
      } else if (preset === 'urban') {
        elevation = 0;
      } else {
        elevation = ((z * dirZ + x * dirX) * (slope * 0.03)) * parcelFlatten;
      }

      pos.setY(i, elevation - 0.25);

      // Vertex color painting based on real elevation & local slope
      let r = 0.18, g = 0.30, b = 0.16;

      if (hasRealDEM) {
        if (absElev > 2400.0) {
          // Glacier snow caps & frost
          r = 0.90; g = 0.93; b = 0.98;
        } else if (absElev > 1300.0) {
          // Alpine slate / granite rock
          r = 0.38; g = 0.42; b = 0.46;
        } else if (absElev <= 2.0 && waterType === 'ocean') {
          // Coastal sand dunes & beach
          r = 0.78; g = 0.70; b = 0.52;
        } else if (biome === 'alpine') {
          r = 0.20; g = 0.35; b = 0.22;
        } else if (biome === 'coastal') {
          r = 0.28; g = 0.38; b = 0.24;
        }
      } else {
        if (preset === 'mountain') {
          if (elevation > 16.0) { r = 0.88; g = 0.92; b = 0.98; }
          else if (elevation > 8.0) { r = 0.38; g = 0.42; b = 0.46; }
          else { r = 0.20; g = 0.35; b = 0.22; }
        } else if (preset === 'ocean' || waterType === 'ocean') {
          if (elevation < 0.2) { r = 0.72; g = 0.62; b = 0.44; }
          else if (elevation < 2.0) { r = 0.82; g = 0.74; b = 0.55; }
          else { r = 0.30; g = 0.40; b = 0.25; }
        } else if (preset === 'lake' || waterType === 'lake') {
          if (elevation < 0.3) { r = 0.55; g = 0.55; b = 0.52; }
          else { r = 0.16; g = 0.34; b = 0.20; }
        } else if (preset === 'forest') {
          r = 0.11; g = 0.26; b = 0.13;
        }
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
      metalness: 0.08,
      flatShading: true
    });

    this.terrainMesh = new THREE.Mesh(terrainGeo, terrainMat);
    this.terrainMesh.receiveShadow = true;
    this.environmentGroup.add(this.terrainMesh);

    // 3. Terraced Foundation Plinth (for sloped sites)
    const plinthGeo = new THREE.BoxGeometry(this.params.room_width + 3.0, 0.6, this.params.room_depth + 3.0);
    const plinthMat = new THREE.MeshStandardMaterial({
      color: 0x1f2937,
      roughness: 0.85,
      metalness: 0.15
    });
    this.foundationPlinth = new THREE.Mesh(plinthGeo, plinthMat);
    this.foundationPlinth.position.set(0, -0.15, 0);
    this.foundationPlinth.receiveShadow = true;
    this.foundationPlinth.castShadow = true;
    this.environmentGroup.add(this.foundationPlinth);

    // 4. Dynamic Water Bodies (Ocean Coastline vs Inland Lake)
    if (waterType === 'ocean' || preset === 'ocean') {
      const oceanWidth = 260;
      const oceanDepth = 120;
      const oceanGeo = new THREE.PlaneGeometry(oceanWidth, oceanDepth, 48, 24);
      oceanGeo.rotateX(-Math.PI / 2);

      const oceanMat = new THREE.MeshStandardMaterial({
        color: 0x0284c7, // Vibrant coastal cyan/navy
        roughness: 0.08,
        metalness: 0.90,
        transparent: true,
        opacity: 0.90
      });

      this.waterMesh = new THREE.Mesh(oceanGeo, oceanMat);
      this.waterMesh.position.set(0, -0.45, -(waterProx + oceanDepth / 2));
      this.waterMesh.receiveShadow = true;
      this.environmentGroup.add(this.waterMesh);

      // Surf foam line along the beach
      const foamGeo = new THREE.PlaneGeometry(oceanWidth, 2.5, 32, 1);
      foamGeo.rotateX(-Math.PI / 2);
      const foamMat = new THREE.MeshBasicMaterial({
        color: 0xf8fafc,
        transparent: true,
        opacity: 0.65
      });
      this.foamMesh = new THREE.Mesh(foamGeo, foamMat);
      this.foamMesh.position.set(0, -0.42, -waterProx);
      this.environmentGroup.add(this.foamMesh);

    } else if (waterType === 'lake' || preset === 'lake') {
      const lakeGeo = new THREE.CylinderGeometry(36, 38, 0.5, 48);
      const lakeMat = new THREE.MeshStandardMaterial({
        color: 0x0369a1, // Tranquil alpine lake sapphire
        roughness: 0.12,
        metalness: 0.80,
        transparent: true,
        opacity: 0.88
      });

      this.waterMesh = new THREE.Mesh(lakeGeo, lakeMat);
      this.waterMesh.position.set(0, -0.45, -waterProx);
      this.waterMesh.receiveShadow = true;
      this.environmentGroup.add(this.waterMesh);
    }

    // 5. Adaptive Vegetation Generation by Biome
    const treeDensity = this.envParams.tree_density;
    if (treeDensity > 0.05 && preset !== 'urban') {
      const baseCount = preset === 'forest' ? 42 : preset === 'mountain' ? 18 : 24;
      const treeCount = Math.round(baseCount * (treeDensity / 0.65));
      this.spawnAdaptiveTrees(treeCount, biome);
    }

    // 6. Urban Context Buildings
    if (preset === 'urban' || this.envParams.urban_shadows) {
      this.spawnUrbanBuildings();
    }

    // 7. Real Road Network & Site Ingress Driveway
    this.buildRoadNetwork();
  }

  /**
   * Generates a 3D road ribbon mesh that conforms precisely to the bilinear
   * satellite DEM heightfield across the parcel, complete with concrete curbs,
   * dashed centerline stripes, and a connecting site ingress driveway.
   */
  buildRoadNetwork() {
    if (!this.roadGroup) {
      this.roadGroup = new THREE.Group();
      this.roadGroup.name = 'roadNetworkGroup';
      this.environmentGroup.add(this.roadGroup);
    } else {
      while (this.roadGroup.children.length > 0) {
        const obj = this.roadGroup.children.pop();
        if (obj.geometry) obj.geometry.dispose();
        if (obj.material) {
          if (Array.isArray(obj.material)) obj.material.forEach(m => m.dispose());
          else obj.material.dispose();
        }
      }
    }

    if (this.sitePreset === 'studio') return;

    const road = this.roadData;
    if (!road) return;

    const lat0 = road.lat || 37.8590;
    const lng0 = road.lng || -122.4853;
    const distM = typeof road.distance === 'number' ? road.distance : 22.0;
    const roadType = road.type || 'local';
    const roadName = road.name || 'Pine Street';

    // If site is far off-grid (>550m), show natural foot trail and off-grid directional sign
    if (distM > 550) {
      this.buildOffGridTrail(distM, roadName);
      return;
    }

    // Convert coordinates to local CAD meters
    const cosLat = Math.max(0.15, Math.cos(lat0 * Math.PI / 180.0));
    let pts = [];
    if (road.coords && road.coords.length >= 2) {
      pts = road.coords.map(c => {
        const x = (c[0] - lng0) * 111320.0 * cosLat;
        const z = -(c[1] - lat0) * 110540.0;
        return new THREE.Vector2(x, z);
      });
    }

    // Fallback: If no multi-point coords, generate road line perpendicular to ingress point
    if (pts.length < 2) {
      let rx = distM;
      let rz = 0;
      if (road.ingressCoord) {
        rx = (road.ingressCoord[0] - lng0) * 111320.0 * cosLat;
        rz = -(road.ingressCoord[1] - lat0) * 110540.0;
      }
      const len = Math.max(1.0, Math.sqrt(rx * rx + rz * rz));
      const tx = -rz / len;
      const tz = rx / len;
      pts = [
        new THREE.Vector2(rx - tx * 60, rz - tz * 60),
        new THREE.Vector2(rx, rz),
        new THREE.Vector2(rx + tx * 60, rz + tz * 60)
      ];
    }

    // Remove near duplicates
    const cleanPts = [];
    for (let i = 0; i < pts.length; i++) {
      if (i === 0 || pts[i].distanceTo(cleanPts[cleanPts.length - 1]) > 0.6) {
        cleanPts.push(pts[i]);
      }
    }
    if (cleanPts.length < 2) return;

    // Find closest point to site pad (0, 0)
    let closestPt = cleanPts[0];
    let minD = 99999;
    for (const p of cleanPts) {
      const d = p.length();
      if (d < minD) {
        minD = d;
        closestPt = p;
      }
    }

    // Compute primary road direction
    const pStart = cleanPts[0];
    const pEnd = cleanPts[cleanPts.length - 1];
    let dir = new THREE.Vector2().subVectors(pEnd, pStart).normalize();
    if (dir.length() < 0.001) dir = new THREE.Vector2(0, 1);

    // Extrapolate endpoints across the 150m terrain parcel (-70m to +70m)
    const extendedPts = [
      new THREE.Vector2().copy(closestPt).addScaledVector(dir, -72),
      ...cleanPts,
      new THREE.Vector2().copy(closestPt).addScaledVector(dir, 72)
    ];

    // Subdivide road points into small ~2m increments for smooth terrain conforming
    const sampledCenterline = [];
    for (let i = 0; i < extendedPts.length - 1; i++) {
      const p1 = extendedPts[i];
      const p2 = extendedPts[i + 1];
      const segLen = p1.distanceTo(p2);
      const steps = Math.max(1, Math.round(segLen / 2.0));
      for (let s = 0; s < steps; s++) {
        const t = s / steps;
        const x = THREE.MathUtils.lerp(p1.x, p2.x, t);
        const z = THREE.MathUtils.lerp(p1.y, p2.y, t);
        if (Math.abs(x) <= 73 && Math.abs(z) <= 73) {
          sampledCenterline.push(new THREE.Vector2(x, z));
        }
      }
    }

    if (sampledCenterline.length < 2) return;

    // Road dimensions
    const roadWidth = roadType === 'arterial' ? 9.2 : (roadType === 'collector' ? 7.2 : 5.4);
    const halfW = roadWidth / 2.0;
    const curbW = 0.35;
    const numPts = sampledCenterline.length;

    const roadVerts = [];
    const roadNorms = [];
    const roadUvs = [];
    const roadIndices = [];

    const curbVerts = [];
    const curbNorms = [];
    const curbIndices = [];

    const stripeVerts = [];
    const stripeIndices = [];

    for (let i = 0; i < numPts; i++) {
      const curr = sampledCenterline[i];
      let tan;
      if (i === 0) {
        tan = new THREE.Vector2().subVectors(sampledCenterline[1], curr).normalize();
      } else if (i === numPts - 1) {
        tan = new THREE.Vector2().subVectors(curr, sampledCenterline[numPts - 2]).normalize();
      } else {
        tan = new THREE.Vector2().subVectors(sampledCenterline[i + 1], sampledCenterline[i - 1]).normalize();
      }
      if (tan.length() < 0.001) tan = new THREE.Vector2(0, 1);
      const norm = new THREE.Vector2(-tan.y, tan.x).normalize();

      const cx = curr.x;
      const cz = curr.y;
      const cy = this.getTerrainHeightAt(cx, cz) + 0.14;

      const lx = cx - norm.x * halfW;
      const lz = cz - norm.y * halfW;
      const ly = this.getTerrainHeightAt(lx, lz) + 0.11;

      const rx = cx + norm.x * halfW;
      const rz = cz + norm.y * halfW;
      const ry = this.getTerrainHeightAt(rx, rz) + 0.11;

      const lCurbX = cx - norm.x * (halfW + curbW);
      const lCurbZ = cz - norm.y * (halfW + curbW);
      const lCurbY = this.getTerrainHeightAt(lCurbX, lCurbZ) + 0.18;

      const rCurbX = cx + norm.x * (halfW + curbW);
      const rCurbZ = cz + norm.y * (halfW + curbW);
      const rCurbY = this.getTerrainHeightAt(rCurbX, rCurbZ) + 0.18;

      // Road Surface (lx, ly, lz) -> (cx, cy, cz) -> (rx, ry, rz)
      const rBase = i * 3;
      roadVerts.push(lx, ly, lz,   cx, cy, cz,   rx, ry, rz);
      roadNorms.push(0, 1, 0,      0, 1, 0,      0, 1, 0);
      roadUvs.push(0, i / numPts,  0.5, i / numPts,  1, i / numPts);

      if (i < numPts - 1) {
        const nextBase = (i + 1) * 3;
        roadIndices.push(rBase, nextBase, rBase + 1);
        roadIndices.push(rBase + 1, nextBase, nextBase + 1);
        roadIndices.push(rBase + 1, nextBase + 1, rBase + 2);
        roadIndices.push(rBase + 2, nextBase + 1, nextBase + 2);
      }

      // Concrete Curb Ribbons
      const cBase = i * 4;
      curbVerts.push(lCurbX, lCurbY, lCurbZ,  lx, ly, lz,  rx, ry, rz,  rCurbX, rCurbY, rCurbZ);
      curbNorms.push(0, 1, 0,  0, 1, 0,  0, 1, 0,  0, 1, 0);
      if (i < numPts - 1) {
        const nextCBase = (i + 1) * 4;
        curbIndices.push(cBase, nextCBase, cBase + 1);
        curbIndices.push(cBase + 1, nextCBase, nextCBase + 1);
        curbIndices.push(cBase + 2, nextCBase + 2, cBase + 3);
        curbIndices.push(cBase + 3, nextCBase + 2, nextCBase + 3);
      }

      // Centerline stripe (dashed every 3 increments)
      if (i % 3 !== 2) {
        const sW = 0.11;
        const slx = cx - norm.x * sW;
        const slz = cz - norm.y * sW;
        const srx = cx + norm.x * sW;
        const srz = cz + norm.y * sW;
        const sIdx = stripeVerts.length / 3;
        stripeVerts.push(slx, cy + 0.02, slz,   srx, cy + 0.02, srz);
        if (i < numPts - 1 && (i + 1) % 3 !== 2) {
          stripeIndices.push(sIdx, sIdx + 2, sIdx + 1);
          stripeIndices.push(sIdx + 1, sIdx + 2, sIdx + 3);
        }
      }
    }

    // Road Mesh
    const roadGeo = new THREE.BufferGeometry();
    roadGeo.setAttribute('position', new THREE.Float32BufferAttribute(roadVerts, 3));
    roadGeo.setAttribute('normal', new THREE.Float32BufferAttribute(roadNorms, 3));
    roadGeo.setAttribute('uv', new THREE.Float32BufferAttribute(roadUvs, 2));
    roadGeo.setIndex(roadIndices);

    const roadMat = new THREE.MeshStandardMaterial({
      color: 0x1e293b,
      roughness: 0.85,
      metalness: 0.12,
      polygonOffset: true,
      polygonOffsetFactor: -1,
      polygonOffsetUnits: -1
    });
    const roadMesh = new THREE.Mesh(roadGeo, roadMat);
    roadMesh.receiveShadow = true;
    this.roadGroup.add(roadMesh);

    // Curbs Mesh
    const curbGeo = new THREE.BufferGeometry();
    curbGeo.setAttribute('position', new THREE.Float32BufferAttribute(curbVerts, 3));
    curbGeo.setAttribute('normal', new THREE.Float32BufferAttribute(curbNorms, 3));
    curbGeo.setIndex(curbIndices);
    const curbMat = new THREE.MeshStandardMaterial({
      color: 0x64748b,
      roughness: 0.9,
      metalness: 0.05
    });
    const curbMesh = new THREE.Mesh(curbGeo, curbMat);
    curbMesh.receiveShadow = true;
    this.roadGroup.add(curbMesh);

    // Centerline Stripe Mesh
    if (stripeVerts.length > 0 && stripeIndices.length > 0) {
      const stripeGeo = new THREE.BufferGeometry();
      stripeGeo.setAttribute('position', new THREE.Float32BufferAttribute(stripeVerts, 3));
      stripeGeo.setIndex(stripeIndices);
      stripeGeo.computeVertexNormals();
      const stripeMat = new THREE.MeshBasicMaterial({
        color: roadType === 'arterial' ? 0xfbbf24 : 0xf8fafc,
        side: THREE.DoubleSide
      });
      const stripeMesh = new THREE.Mesh(stripeGeo, stripeMat);
      this.roadGroup.add(stripeMesh);
    }

    // Site Ingress Driveway connecting building pad plinth to road
    this.buildSiteIngressDriveway(closestPt, roadName, distM);
  }

  buildSiteIngressDriveway(roadPt, roadName, distM) {
    const dir = new THREE.Vector2(roadPt.x, roadPt.y).normalize();
    const padEdgeDist = Math.max(6.0, (this.params.room_width || 12) / 2.0 + 1.5);
    const padPt = new THREE.Vector2().copy(dir).multiplyScalar(padEdgeDist);

    const drivewayLen = padPt.distanceTo(roadPt);
    if (drivewayLen < 1.0) return;

    const steps = Math.max(2, Math.round(drivewayLen / 1.6));
    const dWidth = 4.2;
    const dHalf = dWidth / 2.0;
    const norm = new THREE.Vector2(-dir.y, dir.x).normalize();

    const dVerts = [];
    const dIndices = [];

    for (let s = 0; s <= steps; s++) {
      const t = s / steps;
      const cx = THREE.MathUtils.lerp(padPt.x, roadPt.x, t);
      const cz = THREE.MathUtils.lerp(padPt.y, roadPt.y, t);
      const cy = this.getTerrainHeightAt(cx, cz) + 0.12;

      const lx = cx - norm.x * dHalf;
      const lz = cz - norm.y * dHalf;
      const rx = cx + norm.x * dHalf;
      const rz = cz + norm.y * dHalf;

      dVerts.push(lx, cy, lz,   rx, cy, rz);

      if (s < steps) {
        const base = s * 2;
        dIndices.push(base, base + 2, base + 1);
        dIndices.push(base + 1, base + 2, base + 3);
      }
    }

    const driveGeo = new THREE.BufferGeometry();
    driveGeo.setAttribute('position', new THREE.Float32BufferAttribute(dVerts, 3));
    driveGeo.setIndex(dIndices);
    driveGeo.computeVertexNormals();

    const driveMat = new THREE.MeshStandardMaterial({
      color: 0x334155,
      roughness: 0.88,
      metalness: 0.10,
      polygonOffset: true,
      polygonOffsetFactor: -1,
      polygonOffsetUnits: -1
    });
    const driveMesh = new THREE.Mesh(driveGeo, driveMat);
    driveMesh.receiveShadow = true;
    this.roadGroup.add(driveMesh);

    // 3D CAD Ingress Marker Post & Ring at Curb Cut
    const ingressY = this.getTerrainHeightAt(roadPt.x, roadPt.y) + 0.15;

    // Glowing base ring
    const ringGeo = new THREE.RingGeometry(0.8, 1.25, 24);
    ringGeo.rotateX(-Math.PI / 2);
    const ringMat = new THREE.MeshBasicMaterial({ color: 0x38bdf8, side: THREE.DoubleSide });
    const ringMesh = new THREE.Mesh(ringGeo, ringMat);
    ringMesh.position.set(roadPt.x, ingressY + 0.05, roadPt.y);
    this.roadGroup.add(ringMesh);

    // Sleek CAD Ingress Marker Post
    const postGeo = new THREE.CylinderGeometry(0.1, 0.1, 2.2, 12);
    const postMat = new THREE.MeshStandardMaterial({ color: 0x0284c7, metalness: 0.8, roughness: 0.2 });
    const postMesh = new THREE.Mesh(postGeo, postMat);
    postMesh.position.set(roadPt.x, ingressY + 1.1, roadPt.y);
    this.roadGroup.add(postMesh);

    // Ingress Beacon Orb
    const beaconGeo = new THREE.SphereGeometry(0.25, 12, 8);
    const beaconMat = new THREE.MeshBasicMaterial({ color: 0x38bdf8 });
    const beaconMesh = new THREE.Mesh(beaconGeo, beaconMat);
    beaconMesh.position.set(roadPt.x, ingressY + 2.3, roadPt.y);
    this.roadGroup.add(beaconMesh);
  }

  buildOffGridTrail(distM, roadName) {
    const trailDir = new THREE.Vector2(0.8, 0.6).normalize();
    const trailLen = 70.0;
    const steps = 35;
    const tWidth = 1.5;
    const norm = new THREE.Vector2(-trailDir.y, trailDir.x);

    const tVerts = [];
    const tIndices = [];

    for (let s = 0; s <= steps; s++) {
      const t = s / steps;
      const cx = trailDir.x * t * trailLen;
      const cz = trailDir.y * t * trailLen;
      const cy = this.getTerrainHeightAt(cx, cz) + 0.06;

      const lx = cx - norm.x * (tWidth / 2);
      const lz = cz - norm.y * (tWidth / 2);
      const rx = cx + norm.x * (tWidth / 2);
      const rz = cz + norm.y * (tWidth / 2);

      tVerts.push(lx, cy, lz,  rx, cy, rz);
      if (s < steps) {
        const base = s * 2;
        tIndices.push(base, base + 2, base + 1);
        tIndices.push(base + 1, base + 2, base + 3);
      }
    }

    const trailGeo = new THREE.BufferGeometry();
    trailGeo.setAttribute('position', new THREE.Float32BufferAttribute(tVerts, 3));
    trailGeo.setIndex(tIndices);
    trailGeo.computeVertexNormals();

    const trailMat = new THREE.MeshStandardMaterial({
      color: 0x78593a,
      roughness: 0.95,
      metalness: 0.02
    });
    const trailMesh = new THREE.Mesh(trailGeo, trailMat);
    this.roadGroup.add(trailMesh);
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
   * Spawns realistic 3D tree canopies tailored to the active biome:
   * - Alpine: Tall slender spruce & pine conifers with tapered needle tiers
   * - Coastal: Umbrella pines, Monterey cypress, wind-swept crowns
   * - Temperate: Mixed deciduous rounded canopies
   */
  spawnAdaptiveTrees(numTrees, biome) {
    const trunkMat = new THREE.MeshStandardMaterial({ color: 0x422006, roughness: 0.9 });

    for (let i = 0; i < numTrees; i++) {
      const angle = Math.random() * Math.PI * 2;
      const dist = 14.5 + Math.random() * 34.0;
      const x = Math.cos(angle) * dist;
      const z = Math.sin(angle) * dist;

      // Don't place inside ocean or lake water
      if ((this.sitePreset === 'ocean' || this.envParams.water_type === 'ocean') && z < -this.envParams.water_proximity) {
        continue;
      }
      if ((this.sitePreset === 'lake' || this.envParams.water_type === 'lake') && Math.sqrt(x * x + Math.pow(z + this.envParams.water_proximity, 2)) < 30) {
        continue;
      }

      const scale = 0.75 + Math.random() * 0.65;
      this.createBiomeTree(x, z, scale, biome, trunkMat);
    }

    // Re-add custom user-planted trees
    this.customTrees.forEach(t => {
      this.createBiomeTree(t.x, t.z, t.scale || 1.0, biome, trunkMat);
    });
  }

  createBiomeTree(x, z, scale, biome, trunkMat) {
    const treeGroup = new THREE.Group();

    if (biome === 'alpine') {
      // Alpine Conifer (Tall slender trunk, 3 tapered cone needle tiers)
      const trunkH = 2.8 * scale;
      const trunkGeo = new THREE.CylinderGeometry(0.14 * scale, 0.22 * scale, trunkH, 8);
      const trunk = new THREE.Mesh(trunkGeo, trunkMat);
      trunk.position.y = trunkH / 2;
      trunk.castShadow = true;
      treeGroup.add(trunk);

      const pineColors = [0x14532d, 0x166534, 0x0f3d21, 0x1b4332];
      const pineColor = pineColors[Math.floor(Math.random() * pineColors.length)];
      const foliageMat = new THREE.MeshStandardMaterial({ color: pineColor, roughness: 0.78, flatShading: true });

      // 3 Conical Tiers
      const tiers = [
        { r: 2.2 * scale, h: 3.2 * scale, y: trunkH + 0.6 },
        { r: 1.7 * scale, h: 2.8 * scale, y: trunkH + 2.0 },
        { r: 1.1 * scale, h: 2.2 * scale, y: trunkH + 3.2 }
      ];

      tiers.forEach(t => {
        const geo = new THREE.ConeGeometry(t.r, t.h, 8);
        const mesh = new THREE.Mesh(geo, foliageMat);
        mesh.position.y = t.y;
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        treeGroup.add(mesh);
      });

    } else if (biome === 'coastal') {
      // Coastal Cypress / Umbrella Pine (Twisted trunk, spread flat umbrella canopy)
      const trunkH = 2.2 * scale;
      const trunkGeo = new THREE.CylinderGeometry(0.18 * scale, 0.26 * scale, trunkH, 8);
      const trunk = new THREE.Mesh(trunkGeo, trunkMat);
      trunk.position.y = trunkH / 2;
      trunk.rotation.z = (Math.random() - 0.5) * 0.15; // Natural coastal wind lean
      trunk.castShadow = true;
      treeGroup.add(trunk);

      const coastalColors = [0x166534, 0x365314, 0x15803d, 0x4d7c0f];
      const foliageMat = new THREE.MeshStandardMaterial({
        color: coastalColors[Math.floor(Math.random() * coastalColors.length)],
        roughness: 0.72,
        flatShading: true
      });

      // Wide umbrella dome
      const domeGeo = new THREE.SphereGeometry(2.4 * scale, 8, 6);
      domeGeo.scale(1.3, 0.55, 1.3);
      const dome = new THREE.Mesh(domeGeo, foliageMat);
      dome.position.y = trunkH + 0.8 * scale;
      dome.castShadow = true;
      dome.receiveShadow = true;
      treeGroup.add(dome);

    } else {
      // Temperate Mixed (Standard double cone canopy)
      const trunkH = 2.4 * scale;
      const trunkGeo = new THREE.CylinderGeometry(0.18 * scale, 0.26 * scale, trunkH, 8);
      const trunk = new THREE.Mesh(trunkGeo, trunkMat);
      trunk.position.y = trunkH / 2;
      trunk.castShadow = true;
      treeGroup.add(trunk);

      const deciduousColors = [0x15803d, 0x166534, 0x22c55e, 0x84cc16];
      const foliageMat = new THREE.MeshStandardMaterial({
        color: deciduousColors[Math.floor(Math.random() * deciduousColors.length)],
        roughness: 0.75,
        flatShading: true
      });

      const canopyGeo = new THREE.ConeGeometry(2.0 * scale, 4.0 * scale, 8);
      const canopy = new THREE.Mesh(canopyGeo, foliageMat);
      canopy.position.y = trunkH + 1.8 * scale;
      canopy.castShadow = true;
      canopy.receiveShadow = true;
      treeGroup.add(canopy);
    }

    // Anchor tree to topography height
    const yPos = this.getTerrainHeightAt(x, z);
    treeGroup.position.set(x, yPos, z);

    this.treesGroup.add(treeGroup);
  }

  getTerrainHeightAt(x, z) {
    if (!this.terrainMesh || this.sitePreset === 'studio') return 0;
    const dist = Math.sqrt(x * x + z * z);
    const flatten = THREE.MathUtils.smoothstep(dist, 13.0, 26.0);

    if (this.elevationGrid && this.elevationGrid.length >= 9) {
      const u = (x + 75.0) / 150.0;
      const v = (z + 75.0) / 150.0;
      const absElev = this.sampleDEM(u, v);
      return (absElev - this.centerElevation) * (this.elevationExaggeration || 1.0) * flatten - 0.25;
    }

    const mHeight = this.envParams.mountain_height;
    const slope = this.envParams.terrain_slope;
    const preset = this.sitePreset;

    if (preset === 'mountain' || (preset === 'custom' && mHeight > 10)) {
      const ridge1 = Math.abs(Math.sin(x * 0.045 + 1.2) * Math.cos(z * 0.038)) * mHeight;
      return (ridge1 + (z * 0.25)) * flatten - 0.25;
    } else if (preset === 'ocean' || this.envParams.water_type === 'ocean') {
      const shoreDrop = Math.max(-1.8, Math.min(slope * 0.6, (z + this.envParams.water_proximity) * 0.22));
      return shoreDrop * flatten - 0.25;
    }
    return 0;
  }

  spawnUrbanBuildings() {
    const buildingMat = new THREE.MeshStandardMaterial({
      color: 0x334155,
      roughness: 0.4,
      metalness: 0.2
    });

    const contextBuildings = [
      { x: -30, z: -18, w: 18, d: 22, h: 32 },
      { x: 28, z: -20, w: 16, d: 20, h: 26 },
      { x: -32, z: 24, w: 20, d: 18, h: 38 },
      { x: 30, z: 26, w: 22, d: 24, h: 34 }
    ];

    contextBuildings.forEach(b => {
      const geo = new THREE.BoxGeometry(b.w, b.h, b.d);
      const mesh = new THREE.Mesh(geo, buildingMat);
      mesh.position.set(b.x, b.h / 2, b.z);
      mesh.castShadow = true;
      mesh.receiveShadow = true;

      const edges = new THREE.EdgesGeometry(geo);
      const line = new THREE.LineSegments(edges, new THREE.LineBasicMaterial({ color: 0x64748b, transparent: true, opacity: 0.4 }));
      mesh.add(line);

      this.urbanGroup.add(mesh);
    });
  }

  setSitePreset(presetKey) {
    this.sitePreset = presetKey;
    this.envParams.site_preset = presetKey;

    if (presetKey === 'mountain') {
      this.envParams.mountain_height = 36.0;
      this.envParams.terrain_slope = 24.0;
      this.envParams.water_type = 'none';
      this.envParams.vegetation_biome = 'alpine';
    } else if (presetKey === 'ocean') {
      this.envParams.mountain_height = 14.0;
      this.envParams.terrain_slope = 16.0;
      this.envParams.water_type = 'ocean';
      this.envParams.water_proximity = 20.0;
      this.envParams.vegetation_biome = 'coastal';
    } else if (presetKey === 'lake') {
      this.envParams.mountain_height = 14.0;
      this.envParams.terrain_slope = 8.0;
      this.envParams.water_type = 'lake';
      this.envParams.water_proximity = 18.0;
      this.envParams.vegetation_biome = 'temperate';
    } else if (presetKey === 'forest') {
      this.envParams.mountain_height = 16.0;
      this.envParams.terrain_slope = 10.0;
      this.envParams.water_type = 'none';
      this.envParams.vegetation_biome = 'temperate';
    }

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
      const trunkMat = new THREE.MeshStandardMaterial({ color: 0x422006, roughness: 0.9 });
      this.createBiomeTree(p.x, p.z, 1.0, this.envParams.vegetation_biome, trunkMat);

      if (window.dispatchEvent) {
        window.dispatchEvent(new CustomEvent('treePlanted', { detail: { count: this.customTrees.length } }));
      }
    }
  }

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
    const northWallGeo = new THREE.BoxGeometry(room_width, height, wallThick);
    const northWall = new THREE.Mesh(northWallGeo, concreteMat);
    northWall.position.set(0, height / 2 + slabThickness, room_depth / 2);
    northWall.castShadow = true;
    northWall.receiveShadow = true;
    this.buildingGroup.add(northWall);

    const sideWallGeo = new THREE.BoxGeometry(wallThick, height, room_depth);
    const leftWall = new THREE.Mesh(sideWallGeo, concreteMat);
    leftWall.position.set(-room_width / 2, height / 2 + slabThickness, 0);
    leftWall.castShadow = true;
    leftWall.receiveShadow = true;
    this.buildingGroup.add(leftWall);

    const rightWall = new THREE.Mesh(sideWallGeo, concreteMat);
    rightWall.position.set(room_width / 2, height / 2 + slabThickness, 0);
    rightWall.castShadow = true;
    rightWall.receiveShadow = true;
    this.buildingGroup.add(rightWall);

    // 6. South Glazed Facade & Parametric Louvers
    const glassW = room_width - 0.4;
    const glassH = height * Math.min(0.95, Math.max(0.2, wwr));
    const glassGeo = new THREE.BoxGeometry(glassW, glassH, 0.08);
    const glassMesh = new THREE.Mesh(glassGeo, glassMat);
    glassMesh.position.set(0, slabThickness + glassH / 2 + (height - glassH) / 2, -room_depth / 2);
    this.buildingGroup.add(glassMesh);

    const sillHeight = (height - glassH) / 2;
    if (sillHeight > 0.05) {
      const sillGeo = new THREE.BoxGeometry(room_width, sillHeight, wallThick);
      const sillMesh = new THREE.Mesh(sillGeo, concreteMat);
      sillMesh.position.set(0, slabThickness + sillHeight / 2, -room_depth / 2);
      sillMesh.castShadow = true;
      sillMesh.receiveShadow = true;
      this.buildingGroup.add(sillMesh);
    }

    // 7. Parametric Exterior Shading Louvers
    const numLouvers = 7;
    const louverSpacing = glassH / (numLouvers + 1);
    const louverDepth = 0.65;
    const louverThickness = 0.04;
    const louverAngleRad = THREE.MathUtils.degToRad(louver_angle);

    for (let i = 1; i <= numLouvers; i++) {
      const louverGeo = new THREE.BoxGeometry(glassW, louverThickness, louverDepth);
      const louverMesh = new THREE.Mesh(louverGeo, louverMat);
      const yLouver = slabThickness + sillHeight + (i * louverSpacing);
      louverMesh.position.set(0, yLouver, -room_depth / 2 - (louverDepth / 2) - 0.08);
      louverMesh.rotation.x = louverAngleRad;
      louverMesh.castShadow = true;
      louverMesh.receiveShadow = true;
      this.buildingGroup.add(louverMesh);
    }
  }

  createHeatmapSurface(width, depth, yLevel, clipPlanes) {
    const segX = 36;
    const segZ = 36;
    const geo = new THREE.PlaneGeometry(width - 0.4, depth - 0.4, segX, segZ);
    geo.rotateX(-Math.PI / 2);

    const count = geo.attributes.position.count;
    const colors = new Float32Array(count * 3);
    const positions = geo.attributes.position;

    const { wwr, room_depth, louver_angle, overhang_depth } = this.params;

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

    const mat = new THREE.MeshBasicMaterial({
      vertexColors: true,
      clippingPlanes: clipPlanes,
      side: THREE.DoubleSide
    });

    this.heatmapMesh = new THREE.Mesh(geo, mat);
    this.heatmapMesh.position.set(0, yLevel, 0);
    this.buildingGroup.add(this.heatmapMesh);
  }

  sampleColormap(val, mapType) {
    const t = Math.max(0, Math.min(1, val));
    if (mapType === 'viridis') {
      const r = Math.max(0, Math.min(1, 0.28 + 0.75 * t - 0.25 * t * t));
      const g = Math.max(0, Math.min(1, 0.05 + 1.25 * t - 0.45 * t * t));
      const b = Math.max(0, Math.min(1, 0.55 - 0.5 * t + 0.65 * (1 - t) * (1 - t)));
      return [r, g, b];
    } else if (mapType === 'coolwarm') {
      const r = Math.max(0, Math.min(1, 0.2 + 0.8 * t));
      const g = Math.max(0, Math.min(1, 0.9 * Math.sin(t * Math.PI)));
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
      { id: 'north_labs', label: 'North Research Core', pos: [0, 2.2, 4.5], color: 0xa855f7 },
      { id: 'east_collab', label: 'East Terrace', pos: [4.5, 2.2, 0], color: 0xf59e0b },
      { id: 'west_services', label: 'West Circulation', pos: [-4.5, 2.2, 0], color: 0xec4899 }
    ];

    const edges = [
      ['atrium', 'south_office'],
      ['atrium', 'north_labs'],
      ['atrium', 'east_collab'],
      ['atrium', 'west_services'],
      ['south_office', 'east_collab'],
      ['south_office', 'west_services']
    ];

    const nodeMap = new Map();

    nodes.forEach(node => {
      const nodeGeo = new THREE.SphereGeometry(0.42, 24, 24);
      const nodeMat = new THREE.MeshStandardMaterial({
        color: node.color,
        emissive: node.color,
        emissiveIntensity: 0.45,
        roughness: 0.3
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
        opacity: 0.5,
        linewidth: 2
      });
      const line = new THREE.Line(lineGeo, lineMat);
      this.spatialGraphGroup.add(line);
    });
  }

  updateParameters(newParams) {
    Object.assign(this.params, newParams);
    this.updateSunPosition();
    this.rebuildArchitecture();
    this.buildSpatialGraph();
  }

  setMetric(metricName) {
    this.activeMetric = metricName;
    this.rebuildArchitecture();
  }

  setColormap(mapName) {
    this.colormap = mapName;
    this.rebuildArchitecture();
  }

  toggleSpatialGraph(visible) {
    this.showSpatialGraph = visible;
    this.buildSpatialGraph();
  }

  toggleWireframe(visible) {
    this.showWireframe = visible;
    this.rebuildArchitecture();
  }

  setSectionClipping(enabled, depthRatio = 0.5) {
    this.clippingEnabled = enabled;
    if (enabled) {
      const planeZ = (depthRatio - 0.5) * this.params.room_depth;
      this.sectionPlane.constant = planeZ;
      this.renderer.localClippingEnabled = true;
    } else {
      this.renderer.localClippingEnabled = false;
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

    // Ocean & Lake water surface wave animation
    if (this.waterMesh) {
      const time = Date.now() * 0.001;
      this.waterMesh.rotation.z = Math.sin(time * 0.6) * 0.012;
      this.waterMesh.position.y = -0.45 + Math.sin(time * 1.2) * 0.05;
    }

    if (this.foamMesh) {
      this.foamMesh.material.opacity = 0.5 + Math.sin(Date.now() * 0.002) * 0.25;
    }

    if (this.renderer && this.scene && this.camera) {
      this.renderer.render(this.scene, this.camera);
    }
  }
}
