/**
 * wwwroot/js/mapPicker.js
 * =======================
 * Interactive Global Map Site Picker using Leaflet & OpenStreetMap.
 * 
 * Dynamically queries real satellite DEM elevation data (81-point grid) from Open-Meteo API
 * for ANY location clicked or searched on Earth. Replaces synthetic preset topography with
 * authentic real-world elevation, slope, aspect, relief, and hydrological features.
 */

export class GlobalMapPicker {
  constructor(mapContainerId, onSiteSelected) {
    this.containerId = mapContainerId;
    this.onSiteSelected = onSiteSelected;
    this.map = null;
    this.marker = null;
    this.queryDebounceTimer = null;

    // Active site state with default 81-point DEM grid (Sausalito, CA)
    this.currentSite = {
      name: 'Sausalito Coastal Bluff & Bay, California',
      lat: 37.8590,
      lng: -122.4853,
      elevation: 18.0,
      slope: 14.0,
      slopeDir: 'south',
      ruggedness: 37.0,
      preset: 'ocean',
      waterType: 'ocean',
      waterProximity: 20.0,
      mountainHeight: 37.0,
      treeDensity: 0.60,
      biome: 'coastal',
      hasWater: true,
      hasOcean: true,
      hasLake: false,
      urbanContext: false,
      elevationGrid: [
        22.0, 22.0, 17.0, 15.0, 15.0, 14.0, 14.0, 26.0, 37.0,
        17.0, 17.0, 11.0, 10.0, 10.0, 6.0, 6.0, 6.0, 9.0,
        17.0, 17.0, 11.0, 10.0, 10.0, 6.0, 6.0, 6.0, 9.0,
        8.0, 8.0, 7.0, 6.0, 6.0, 4.0, 4.0, 2.0, 3.0,
        8.0, 8.0, 7.0, 6.0, 6.0, 4.0, 4.0, 2.0, 3.0,
        4.0, 4.0, 3.0, 2.0, 2.0, 0.0, 0.0, 0.0, 0.0,
        4.0, 4.0, 3.0, 2.0, 2.0, 0.0, 0.0, 0.0, 0.0,
        3.0, 3.0, 3.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,
        3.0, 3.0, 3.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0
      ]
    };
  }

  initialize() {
    if (typeof window.L === 'undefined') {
      console.warn('[MapPicker] Leaflet library not loaded.');
      return;
    }

    const container = document.getElementById(this.containerId);
    if (!container) return;

    // Create Leaflet map centered on current site
    this.map = window.L.map(this.containerId).setView([this.currentSite.lat, this.currentSite.lng], 13);

    // Free OpenStreetMap Tile Layer
    window.L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 18,
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    // Initial Draggable Marker
    this.marker = window.L.marker([this.currentSite.lat, this.currentSite.lng], { draggable: true }).addTo(this.map);

    this.updateMarkerPopup();

    // Map Click Listener -> Move marker & analyze terrain
    this.map.on('click', (e) => {
      const { lat, lng } = e.latlng;
      this.marker.setLatLng([lat, lng]);
      this.analyzeSite(lat, lng);
    });

    // Marker Drag Listener
    this.marker.on('dragend', () => {
      const pos = this.marker.getLatLng();
      this.analyzeSite(pos.lat, pos.lng);
    });

    setTimeout(() => {
      if (this.map) this.map.invalidateSize();
    }, 200);
  }

  updateMarkerPopup() {
    if (!this.marker) return;
    const s = this.currentSite;
    const content = `
      <div style="font-family:'Inter',sans-serif; font-size:12px; color:#1e293b; min-width:200px;">
        <strong style="color:#0284c7;">${s.name}</strong><br>
        <strong>Elevation:</strong> ${Math.round(s.elevation)} m MSL<br>
        <strong>Slope:</strong> ${s.slope.toFixed(1)}° (${s.slopeDir})<br>
        <strong>Relief / Ruggedness:</strong> ${Math.round(s.ruggedness)} m<br>
        <strong>Satellite DEM:</strong> <span style="background:#e0f2fe; color:#0369a1; padding:2px 5px; border-radius:4px; font-weight:600;">81 Points (9×9)</span>
      </div>
    `;
    this.marker.bindPopup(content).openPopup();
  }

  analyzeSite(lat, lng) {
    if (this.queryDebounceTimer) clearTimeout(this.queryDebounceTimer);

    const coordsElem = document.getElementById('map-coords-readout');
    if (coordsElem) {
      coordsElem.textContent = `Querying satellite DEM elevation grid at ${lat.toFixed(4)}° N, ${lng.toFixed(4)}° E...`;
    }

    this.queryDebounceTimer = setTimeout(async () => {
      this.currentSite.lat = lat;
      this.currentSite.lng = lng;

      try {
        // Query Open-Meteo free elevation API with a 9x9 (81 points) grid around the target (~350m parcel)
        const cosLat = Math.max(0.15, Math.cos(lat * Math.PI / 180));
        const dLat = 0.0032;
        const dLng = 0.0032 / cosLat;
        const lats = [];
        const lngs = [];

        for (let i = -4; i <= 4; i++) {
          for (let j = -4; j <= 4; j++) {
            const latVal = lat + (i / 4.0) * (dLat / 2.0);
            const lngVal = lng + (j / 4.0) * (dLng / 2.0);
            lats.push(latVal.toFixed(5));
            lngs.push(lngVal.toFixed(5));
          }
        }

        const elevUrl = `https://api.open-meteo.com/v1/elevation?latitude=${lats.join(',')}&longitude=${lngs.join(',')}`;
        const elevRes = await fetch(elevUrl);
        let elevations = this.currentSite.elevationGrid;

        if (elevRes.ok) {
          const elevData = await elevRes.json();
          if (elevData && elevData.elevation && elevData.elevation.length === 81) {
            elevations = elevData.elevation;
          }
        }

        const centerElev = elevations[40];
        const minElev = Math.min(...elevations);
        const maxElev = Math.max(...elevations);
        const relief = Math.max(5, maxElev - minElev);

        // Cardinal endpoints for slope & gradient calculation
        const northElev = elevations[8 * 9 + 4];
        const southElev = elevations[0 * 9 + 4];
        const eastElev = elevations[4 * 9 + 8];
        const westElev = elevations[4 * 9 + 0];

        const distNS = dLat * 111000;
        const distEW = dLng * 111000 * cosLat;
        const gradY = (northElev - southElev) / Math.max(10, distNS);
        const gradX = (eastElev - westElev) / Math.max(10, distEW);
        const slopeRad = Math.atan(Math.sqrt(gradX * gradX + gradY * gradY));
        const slopeDeg = Math.min(45, Math.max(1, slopeRad * (180 / Math.PI)));

        let slopeDir = 'south';
        if (Math.abs(gradY) > Math.abs(gradX)) {
          slopeDir = gradY > 0 ? 'north' : 'south';
        } else {
          slopeDir = gradX > 0 ? 'east' : 'west';
        }

        // Ocean & shoreline detection
        const isNearSeaLevel = centerElev < 45 && minElev <= 2;
        const dropsToZero = elevations.some(e => e <= 0);
        const isOcean = isNearSeaLevel || dropsToZero;

        // Freshwater lake detection
        const isLake = !isOcean && relief > 40 && minElev > 150;

        this.currentSite.elevationGrid = elevations;
        this.currentSite.elevation = centerElev;
        this.currentSite.slope = slopeDeg;
        this.currentSite.slopeDir = slopeDir;
        this.currentSite.ruggedness = relief;
        this.currentSite.waterType = isOcean ? 'ocean' : (isLake ? 'lake' : 'none');
        this.currentSite.hasWater = this.currentSite.waterType !== 'none';
        this.currentSite.hasOcean = isOcean;
        this.currentSite.hasLake = isLake;
        this.currentSite.waterProximity = isOcean ? 20.0 : (isLake ? 25.0 : 99.0);
        this.currentSite.mountainHeight = relief;
        this.currentSite.biome = centerElev > 1800 ? 'alpine' : (isOcean ? 'coastal' : 'temperate');
        this.currentSite.treeDensity = centerElev > 2400 ? 0.15 : (centerElev > 1800 ? 0.40 : (isOcean ? 0.55 : 0.75));
        this.currentSite.preset = isOcean ? 'ocean' : (centerElev > 650 || relief > 120 || slopeDeg > 18 ? 'mountain' : (isLake ? 'lake' : 'forest'));

        // Reverse Geocode site name via OpenStreetMap Nominatim
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
        } catch (_) {}

        this.updateMarkerPopup();

        if (coordsElem) {
          coordsElem.innerHTML = `
            <strong>${this.currentSite.name}</strong> (${lat.toFixed(4)}°, ${lng.toFixed(4)}°)<br>
            Elev: ${Math.round(this.currentSite.elevation)}m MSL | Slope: ${this.currentSite.slope.toFixed(1)}° (${slopeDir}) | Relief: ${Math.round(relief)}m | <strong>Satellite DEM (81 pts)</strong>
          `;
        }

        // Live preview in 3D viewport while moving map
        if (this.onSiteSelected) {
          this.onSiteSelected(this.currentSite);
        }
      } catch (err) {
        console.warn('[MapPicker] Satellite DEM query error:', err);
        if (coordsElem) {
          coordsElem.textContent = `${lat.toFixed(4)}° N, ${lng.toFixed(4)}° E | Elev: ${Math.round(this.currentSite.elevation)}m`;
        }
      }
    }, 280);
  }

  selectPreset(presetKey) {
    const bookmarks = {
      sausalito: {
        name: 'Sausalito Coastal Bluff & Bay, California',
        lat: 37.8590,
        lng: -122.4853
      },
      chamonix: {
        name: 'Chamonix Mont-Blanc, French Alps',
        lat: 45.9237,
        lng: 6.8694
      },
      zermatt: {
        name: 'Zermatt Matterhorn Valley, Swiss Alps',
        lat: 45.9763,
        lng: 7.7491
      },
      zurich: {
        name: 'Lake Zurich Waterfront, Switzerland',
        lat: 47.3686,
        lng: 8.5391
      },
      tahoe: {
        name: 'Emerald Bay, Lake Tahoe, California',
        lat: 38.9545,
        lng: -120.0984
      },
      atacama: {
        name: 'Valle de la Luna, Atacama Plateau, Chile',
        lat: -22.9234,
        lng: -68.2974
      },
      blackforest: {
        name: 'Black Forest Ridge & Canopy, Germany',
        lat: 48.1833,
        lng: 8.1667
      }
    };

    const s = bookmarks[presetKey] || bookmarks.sausalito;
    if (s && this.map) {
      this.map.flyTo([s.lat, s.lng], 13, { duration: 1.2 });
      if (this.marker) {
        this.marker.setLatLng([s.lat, s.lng]);
      }
      this.analyzeSite(s.lat, s.lng);
    }
  }

  applyCurrentSite() {
    if (this.onSiteSelected) {
      this.onSiteSelected(this.currentSite);
    }
  }

  getCurrentSiteJson() {
    return JSON.stringify(this.currentSite);
  }
}
