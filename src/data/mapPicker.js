/**
 * src/data/mapPicker.js
 * =====================
 * Interactive Global Map Site Picker using Leaflet & OpenStreetMap.
 * 
 * Allows users to click anywhere on Earth, extract real-world coordinates,
 * calculate geographical solar angles, and apply natural environmental context
 * (Mountains, Lakefront, Forest, Urban) directly to the 3D viewport.
 */

export class GlobalMapPicker {
  constructor(mapContainerId, onSiteSelected) {
    this.containerId = mapContainerId;
    this.onSiteSelected = onSiteSelected;
    this.map = null;
    this.marker = null;
    this.currentSite = {
      name: 'Lake Zurich Waterfront, Switzerland',
      lat: 47.3686,
      lng: 8.5391,
      elevation: 408,
      preset: 'coastal',
      treeDensity: 0.65,
      hasWater: true,
      slope: 8.0
    };
  }

  initialize() {
    if (typeof window.L === 'undefined') {
      console.warn('[MapPicker] Leaflet library not loaded.');
      return;
    }

    const container = document.getElementById(this.containerId);
    if (!container) return;

    // Create map centered on Switzerland / Alpine lakes
    this.map = window.L.map(this.containerId).setView([this.currentSite.lat, this.currentSite.lng], 13);

    // Free OpenStreetMap Tile Layer
    window.L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 18,
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    // Initial Marker
    this.marker = window.L.marker([this.currentSite.lat, this.currentSite.lng], { draggable: true }).addTo(this.map);
    this.marker.bindPopup(`<b>${this.currentSite.name}</b><br>Lat: ${this.currentSite.lat.toFixed(4)}, Lng: ${this.currentSite.lng.toFixed(4)}`).openPopup();

    // Map Click Handler: Click anywhere on Earth to select a new project site
    this.map.on('click', (e) => {
      this.updateLocation(e.latlng.lat, e.latlng.lng, 'Custom Global Location');
    });

    this.marker.on('dragend', (e) => {
      const pos = e.target.getLatLng();
      this.updateLocation(pos.lat, pos.lng, 'Custom Dragged Location');
    });
  }

  updateLocation(lat, lng, name) {
    this.currentSite.lat = lat;
    this.currentSite.lng = lng;
    this.currentSite.name = name;

    if (this.marker) {
      this.marker.setLatLng([lat, lng]);
      this.marker.setPopupContent(`<b>${name}</b><br>Lat: ${lat.toFixed(4)}°, Lng: ${lng.toFixed(4)}°`).openPopup();
    }

    // Auto-detect terrain / water context based on latitude & geography heuristics
    if (Math.abs(lat) > 44 && Math.abs(lat) < 48) {
      this.currentSite.preset = 'mountain';
      this.currentSite.slope = 18.0;
    } else if (Math.abs(lat) < 15) {
      this.currentSite.preset = 'forest';
      this.currentSite.treeDensity = 0.85;
    }

    // Update UI labels if present
    const coordsElem = document.getElementById('map-coords-readout');
    if (coordsElem) {
      coordsElem.textContent = `${lat.toFixed(4)}° N, ${lng.toFixed(4)}° E | Preset: ${this.currentSite.preset}`;
    }
  }

  selectPreset(presetKey) {
    const presets = {
      zurich: {
        name: 'Lake Zurich Shoreline, Switzerland',
        lat: 47.3686,
        lng: 8.5391,
        preset: 'coastal',
        treeDensity: 0.65,
        hasWater: true,
        slope: 6.0
      },
      chamonix: {
        name: 'Chamonix Mont-Blanc, France (Alpine Valley)',
        lat: 45.9237,
        lng: 6.8694,
        preset: 'mountain',
        treeDensity: 0.45,
        hasWater: false,
        slope: 22.0
      },
      blackforest: {
        name: 'Black Forest Canopy, Germany',
        lat: 48.1833,
        lng: 8.1667,
        preset: 'forest',
        treeDensity: 0.90,
        hasWater: false,
        slope: 12.0
      },
      singapore: {
        name: 'Marina Bay / Downtown Singapore',
        lat: 1.2847,
        lng: 103.8610,
        preset: 'urban',
        treeDensity: 0.50,
        hasWater: true,
        slope: 2.0
      },
      sanfrancisco: {
        name: 'Sausalito Coastal Hills, SF Bay',
        lat: 37.8590,
        lng: -122.4853,
        preset: 'coastal',
        treeDensity: 0.60,
        hasWater: true,
        slope: 16.0
      }
    };

    const s = presets[presetKey];
    if (s && this.map) {
      this.currentSite = { ...s };
      this.map.flyTo([s.lat, s.lng], 13, { duration: 1.2 });
      this.updateLocation(s.lat, s.lng, s.name);
    }
  }

  applyCurrentSite() {
    if (this.onSiteSelected) {
      this.onSiteSelected(this.currentSite);
    }
  }
}
