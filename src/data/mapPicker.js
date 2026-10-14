/**
 * src/data/mapPicker.js
 * =====================
 * Interactive Global Map Site Picker using Leaflet & OpenStreetMap.
 * 
 * Queries real-world elevation and topographic slope from Open-Meteo API,
 * automatically detects ocean coastlines, alpine mountains, inland lakes, and forests,
 * and generates the complete 3D natural site context in Three.js.
 */

export class GlobalMapPicker {
  constructor(mapContainerId, onSiteSelected) {
    this.containerId = mapContainerId;
    this.onSiteSelected = onSiteSelected;
    this.map = null;
    this.marker = null;
    this.queryDebounceTimer = null;

    // Active site state
    this.currentSite = {
      name: 'Lake Zurich Waterfront, Switzerland',
      lat: 47.3686,
      lng: 8.5391,
      elevation: 408,
      slope: 8.0,
      slopeDir: 'south',
      ruggedness: 45,
      preset: 'lake',
      waterType: 'lake',
      waterProximity: 22,
      mountainHeight: 18,
      treeDensity: 0.65,
      biome: 'temperate',
      hasWater: true,
      hasOcean: false,
      hasLake: true,
      urbanContext: false
    };
  }

  initialize() {
    if (typeof window.L === 'undefined') {
      console.warn('[MapPicker] Leaflet library not loaded.');
      return;
    }

    const container = document.getElementById(this.containerId);
    if (!container) return;

    // Create Leaflet map centered on Switzerland / Alpine lakes
    this.map = window.L.map(this.containerId).setView([this.currentSite.lat, this.currentSite.lng], 12);

    // Free OpenStreetMap Tile Layer
    window.L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 18,
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    // Initial Marker
    this.marker = window.L.marker([this.currentSite.lat, this.currentSite.lng], { draggable: true }).addTo(this.map);
    this.updateMarkerPopup();

    // Map Click Handler: Click anywhere on Earth to select a new project site
    this.map.on('click', (e) => {
      this.updateLocationWithAnalysis(e.latlng.lat, e.latlng.lng, 'Selected Global Location');
    });

    this.marker.on('dragend', (e) => {
      const pos = e.target.getLatLng();
      this.updateLocationWithAnalysis(pos.lat, pos.lng, 'Selected Global Location');
    });
  }

  updateMarkerPopup() {
    if (!this.marker) return;
    const s = this.currentSite;
    const biomeLabel = s.preset === 'mountain' ? '⛰️ Alpine Mountain' :
                       s.preset === 'ocean' ? '🌊 Ocean Coastline' :
                       s.preset === 'lake' ? '🏞️ Lake Shoreline' :
                       s.preset === 'forest' ? '🌲 Forested Canopy' :
                       s.preset === 'urban' ? '🏢 Urban Canyon' : '🛠️ Custom Site';

    this.marker.bindPopup(`
      <div style="font-family: sans-serif; font-size: 12px; line-height: 1.4;">
        <strong style="font-size: 13px; color: #0284c7;">${s.name}</strong><br>
        <b>Coordinates:</b> ${s.lat.toFixed(4)}°, ${s.lng.toFixed(4)}°<br>
        <b>Elevation:</b> ${Math.round(s.elevation)} m MSL<br>
        <b>Topography:</b> ${s.slope.toFixed(1)}° slope | Ruggedness: ${Math.round(s.ruggedness)}m<br>
        <b>Detected Biome:</b> ${biomeLabel}
      </div>
    `).openPopup();
  }

  /**
   * Fetches real-world elevation and terrain gradient from Open-Meteo API
   * using a batch 5-point cross sample (Center, North, South, East, West).
   */
  async updateLocationWithAnalysis(lat, lng, defaultName) {
    this.currentSite.lat = lat;
    this.currentSite.lng = lng;
    this.currentSite.name = defaultName;

    // Show loading state in UI
    const coordsElem = document.getElementById('map-coords-readout');
    if (coordsElem) {
      coordsElem.textContent = `Analyzing real topography for ${lat.toFixed(4)}°, ${lng.toFixed(4)}°...`;
    }

    // Debounce query
    if (this.queryDebounceTimer) clearTimeout(this.queryDebounceTimer);

    this.queryDebounceTimer = setTimeout(async () => {
      try {
        // Sample center + 4 offsets (~1.2km) to measure slope and water drop
        const dLat = 0.011;
        const dLng = 0.015;
        const lats = [lat, lat + dLat, lat - dLat, lat, lat].map(v => v.toFixed(5)).join(',');
        const lngs = [lng, lng, lng, lng + dLng, lng - dLng].map(v => v.toFixed(5)).join(',');

        const elevUrl = `https://api.open-meteo.com/v1/elevation?latitude=${lats}&longitude=${lngs}`;
        const elevRes = await fetch(elevUrl);
        let elevations = [350, 360, 340, 350, 350];

        if (elevRes.ok) {
          const elevData = await elevRes.json();
          if (elevData.elevation && elevData.elevation.length >= 5) {
            elevations = elevData.elevation;
          }
        }

        const centerElev = elevations[0];
        const northElev = elevations[1];
        const southElev = elevations[2];
        const eastElev = elevations[3];
        const westElev = elevations[4];

        // Gradient & Slope calculation
        const distNS = dLat * 111000; // ~1220m
        const distEW = dLng * 111000 * Math.cos(lat * Math.PI / 180);
        const gradY = (northElev - southElev) / (2 * distNS);
        const gradX = (eastElev - westElev) / (2 * distEW);
        const slopeRad = Math.atan(Math.sqrt(gradX * gradX + gradY * gradY));
        const slopeDeg = Math.min(35, Math.max(1, slopeRad * (180 / Math.PI)));

        const minElev = Math.min(...elevations);
        const maxElev = Math.max(...elevations);
        const relief = Math.max(10, maxElev - minElev);

        // Aspect / Slope direction
        let slopeDir = 'south';
        if (Math.abs(gradY) > Math.abs(gradX)) {
          slopeDir = gradY > 0 ? 'north' : 'south';
        } else {
          slopeDir = gradX > 0 ? 'east' : 'west';
        }

        // Ocean / Coastal Detection
        const isNearSeaLevel = centerElev < 45 && minElev <= 2;
        const dropsToZero = elevations.some(e => e <= 0);
        const isOcean = isNearSeaLevel || dropsToZero;

        // Mountain Detection
        const isMountain = centerElev > 650 || relief > 120 || slopeDeg > 15;

        // Populate site properties
        this.currentSite.elevation = centerElev;
        this.currentSite.slope = slopeDeg;
        this.currentSite.slopeDir = slopeDir;
        this.currentSite.ruggedness = relief;

        if (isOcean) {
          this.currentSite.preset = 'ocean';
          this.currentSite.waterType = 'ocean';
          this.currentSite.hasWater = true;
          this.currentSite.hasOcean = true;
          this.currentSite.hasLake = false;
          this.currentSite.waterProximity = 18;
          this.currentSite.mountainHeight = Math.min(22, Math.max(8, slopeDeg * 0.8));
          this.currentSite.treeDensity = 0.50;
          this.currentSite.biome = 'coastal';
        } else if (isMountain) {
          this.currentSite.preset = 'mountain';
          this.currentSite.waterType = relief > 200 && minElev > 400 ? 'lake' : 'none';
          this.currentSite.hasWater = this.currentSite.waterType === 'lake';
          this.currentSite.hasOcean = false;
          this.currentSite.hasLake = this.currentSite.waterType === 'lake';
          this.currentSite.waterProximity = 35;
          this.currentSite.mountainHeight = Math.min(50, Math.max(20, relief * 0.12));
          this.currentSite.treeDensity = centerElev > 2200 ? 0.25 : 0.60;
          this.currentSite.biome = 'alpine';
        } else {
          // Inland / Forested / Lake
          this.currentSite.preset = relief > 40 ? 'lake' : 'forest';
          this.currentSite.waterType = this.currentSite.preset === 'lake' ? 'lake' : 'none';
          this.currentSite.hasWater = this.currentSite.waterType === 'lake';
          this.currentSite.hasOcean = false;
          this.currentSite.hasLake = this.currentSite.hasWater;
          this.currentSite.mountainHeight = Math.min(18, Math.max(5, relief * 0.1));
          this.currentSite.treeDensity = 0.80;
          this.currentSite.biome = 'temperate';
        }

        // Reverse Geocode Place Name (graceful attempt)
        try {
          const geoRes = await fetch(`https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${lat}&lon=${lng}`, {
            headers: { 'Accept-Language': 'en' }
          });
          if (geoRes.ok) {
            const geoData = await geoRes.json();
            if (geoData && geoData.display_name) {
              const parts = geoData.display_name.split(',');
              this.currentSite.name = parts.slice(0, 3).join(',').trim();
            }
          }
        } catch (_) {
          // Keep default name
        }

        this.updateMarkerPopup();

        // Update UI labels
        if (coordsElem) {
          const typeBadge = this.currentSite.preset === 'mountain' ? '⛰️ Alpine Mountain' :
                            this.currentSite.preset === 'ocean' ? '🌊 Ocean Coastline' :
                            this.currentSite.preset === 'lake' ? '🏞️ Lake Shoreline' : '🌲 Forest Canopy';
          coordsElem.innerHTML = `
            <strong>${this.currentSite.name}</strong> (${lat.toFixed(4)}°, ${lng.toFixed(4)}°)<br>
            Elev: ${Math.round(this.currentSite.elevation)}m MSL | Slope: ${this.currentSite.slope.toFixed(1)}° (${slopeDir}) | <strong>${typeBadge}</strong>
          `;
        }
      } catch (err) {
        console.warn('[MapPicker] Terrain analysis error:', err);
        if (coordsElem) {
          coordsElem.textContent = `${lat.toFixed(4)}° N, ${lng.toFixed(4)}° E | Preset: ${this.currentSite.preset}`;
        }
      }
    }, 250);
  }

  selectPreset(presetKey) {
    const presets = {
      zurich: {
        name: 'Lake Zurich Shoreline, Switzerland',
        lat: 47.3686,
        lng: 8.5391,
        elevation: 408,
        slope: 8.0,
        slopeDir: 'south',
        ruggedness: 85,
        preset: 'lake',
        waterType: 'lake',
        waterProximity: 18,
        mountainHeight: 14,
        treeDensity: 0.65,
        biome: 'temperate',
        hasWater: true,
        hasOcean: false,
        hasLake: true,
        urbanContext: false
      },
      chamonix: {
        name: 'Chamonix Mont-Blanc, France (Alpine Valley)',
        lat: 45.9237,
        lng: 6.8694,
        elevation: 1041,
        slope: 24.0,
        slopeDir: 'north',
        ruggedness: 420,
        preset: 'mountain',
        waterType: 'none',
        waterProximity: 45,
        mountainHeight: 38,
        treeDensity: 0.45,
        biome: 'alpine',
        hasWater: false,
        hasOcean: false,
        hasLake: false,
        urbanContext: false
      },
      sanfrancisco: {
        name: 'Sausalito Coastal Bluff & Pacific Bay, California',
        lat: 37.8590,
        lng: -122.4853,
        elevation: 18,
        slope: 16.0,
        slopeDir: 'south',
        ruggedness: 110,
        preset: 'ocean',
        waterType: 'ocean',
        waterProximity: 20,
        mountainHeight: 16,
        treeDensity: 0.60,
        biome: 'coastal',
        hasWater: true,
        hasOcean: true,
        hasLake: false,
        urbanContext: false
      },
      blackforest: {
        name: 'Black Forest Canopy & Ridge, Germany',
        lat: 48.1833,
        lng: 8.1667,
        elevation: 780,
        slope: 12.0,
        slopeDir: 'west',
        ruggedness: 160,
        preset: 'forest',
        waterType: 'none',
        waterProximity: 50,
        mountainHeight: 18,
        treeDensity: 0.90,
        biome: 'temperate',
        hasWater: false,
        hasOcean: false,
        hasLake: false,
        urbanContext: false
      },
      singapore: {
        name: 'Marina Bay Waterfront & Urban Core, Singapore',
        lat: 1.2847,
        lng: 103.8610,
        elevation: 5,
        slope: 2.0,
        slopeDir: 'south',
        ruggedness: 15,
        preset: 'urban',
        waterType: 'ocean',
        waterProximity: 25,
        mountainHeight: 4,
        treeDensity: 0.50,
        biome: 'coastal',
        hasWater: true,
        hasOcean: true,
        hasLake: false,
        urbanContext: true
      }
    };

    const s = presets[presetKey];
    if (s && this.map) {
      this.currentSite = { ...s };
      this.map.flyTo([s.lat, s.lng], 13, { duration: 1.2 });
      if (this.marker) {
        this.marker.setLatLng([s.lat, s.lng]);
        this.updateMarkerPopup();
      }

      const coordsElem = document.getElementById('map-coords-readout');
      if (coordsElem) {
        coordsElem.innerHTML = `
          <strong>${s.name}</strong> (${s.lat.toFixed(4)}°, ${s.lng.toFixed(4)}°)<br>
          Elev: ${s.elevation}m | Slope: ${s.slope}° (${s.slopeDir}) | <strong>${s.preset.toUpperCase()}</strong>
        `;
      }
    }
  }

  applyCurrentSite() {
    if (this.onSiteSelected) {
      this.onSiteSelected(this.currentSite);
    }
  }
}
