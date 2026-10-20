/**
 * wwwroot/js/mapPicker.js
 * =======================
 * Interactive Global Map Site Picker using Leaflet & OpenStreetMap.
 * 
 * Automatically detects the user's current geographical location (via GPS / IP fallback),
 * places the marker on their site, and queries real satellite DEM elevation data (81-point grid)
 * from the Open-Meteo API. Users can also click or drag anywhere globally.
 */

export class GlobalMapPicker {
  constructor(mapContainerId, onSiteSelected) {
    this.containerId = mapContainerId;
    this.onSiteSelected = onSiteSelected;
    this.map = null;
    this.marker = null;
    this.roadLayer = null;
    this.queryDebounceTimer = null;
    this.searchAbortController = null;
    this._docClickListener = null;

    // Active site state with default 81-point DEM grid
    this.currentSite = {
      name: 'Current Site Location',
      lat: 37.8590,
      lng: -122.4853,
      elevation: 18.0,
      slope: 14.0,
      slopeDir: 'south',
      ruggedness: 37.0,
      preset: 'custom',
      waterType: 'none',
      waterProximity: 20.0,
      mountainHeight: 37.0,
      treeDensity: 0.60,
      biome: 'temperate',
      hasWater: false,
      hasOcean: false,
      hasLake: false,
      urbanContext: false,
      roadName: 'Pine Street',
      roadDistance: 17.6,
      roadType: 'local',
      trafficVolume: 1800,
      ambientNoise: 47.4,
      transitDistance: 250.0,
      roadCoords: [[-122.4853, 37.8590], [-122.4851, 37.8589], [-122.4849, 37.8588]],
      ingressCoord: [-122.485133, 37.858913],
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

    // Bind search bar, preset chips, and geolocation controls
    this.bindSearchControls();

    // Auto-detect user geographical location and place current site
    this.detectCurrentLocation();
  }

  updateMarkerPopup() {
    if (!this.marker) return;
    const s = this.currentSite;
    const content = `
      <div style="font-family:'Inter',sans-serif; font-size:12px; color:#1e293b; min-width:220px; line-height: 1.5;">
        <strong style="color:#0284c7; font-size:13px;">${s.name}</strong><br>
        <strong>Coordinates:</strong> ${s.lat.toFixed(4)}°, ${s.lng.toFixed(4)}°<br>
        <strong>Elevation:</strong> ${Math.round(s.elevation)} m MSL<br>
        <strong>Slope:</strong> ${s.slope.toFixed(1)}° (${s.slopeDir}) | <strong>Relief:</strong> ${Math.round(s.ruggedness)} m<br>
        <strong>Road Access:</strong> <span style="font-weight:600; color:#0f172a;">${s.roadName || 'Detected Access Road'}</span> (${s.roadDistance}m, ${(s.roadType || 'local').toUpperCase()})<br>
        <span style="display:inline-block; margin-top:4px; background:#e0f2fe; color:#0369a1; padding:2px 6px; border-radius:4px; font-weight:600; font-size:11px;">Satellite DEM (81 pts) + OSM Road Network</span>
      </div>
    `;
    this.marker.bindPopup(content).openPopup();
  }

  detectCurrentLocation(callback) {
    const coordsElem = document.getElementById('map-coords-readout');
    if (coordsElem) {
      coordsElem.textContent = 'Detecting current geographical position...';
    }

    const onLocationFound = (lat, lng, locName) => {
      this.currentSite.lat = lat;
      this.currentSite.lng = lng;
      if (locName) this.currentSite.name = locName;
      if (this.map) {
        this.map.flyTo([lat, lng], 14, { duration: 1.2 });
        if (this.marker) this.marker.setLatLng([lat, lng]);
      }
      this.analyzeSite(lat, lng, callback);
    };

    if ('geolocation' in navigator) {
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          const lat = pos.coords.latitude;
          const lng = pos.coords.longitude;
          onLocationFound(lat, lng, 'Current Detected Site');
        },
        async (err) => {
          console.warn('[MapPicker] Browser geolocation unavailable, checking IP location:', err);
          await this.fallbackIpLocation(onLocationFound);
        },
        { enableHighAccuracy: true, timeout: 6000, maximumAge: 60000 }
      );
    } else {
      this.fallbackIpLocation(onLocationFound);
    }
  }

  async fallbackIpLocation(onFound) {
    try {
      const res = await fetch('https://ipapi.co/json/');
      if (res.ok) {
        const data = await res.json();
        if (data && data.latitude && data.longitude) {
          const name = `${data.city || 'Detected Site'}, ${data.region || ''} ${data.country_name || ''}`.trim();
          onFound(data.latitude, data.longitude, name);
          return;
        }
      }
    } catch (e) {
      console.warn('[MapPicker] IP fallback failed:', e);
    }
    // Retain active site coordinates if fallback fails
    onFound(this.currentSite.lat, this.currentSite.lng, this.currentSite.name);
  }

  analyzeSite(lat, lng, callback) {
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
        let geoData = null;
        try {
          const geoRes = await fetch(`https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${lat}&lon=${lng}`, {
            headers: { 'Accept-Language': 'en' }
          });
          if (geoRes.ok) {
            geoData = await geoRes.json();
            if (geoData && geoData.display_name) {
              const parts = geoData.display_name.split(',');
              this.currentSite.name = parts.slice(0, 3).join(',').trim();
            }
          }
        } catch (_) {}

        // Query real road network from Open Source Routing Machine (OSRM)
        let roadName = '';
        let roadDistance = 22.0;
        let roadType = 'local';
        let trafficVolume = 1800;
        let ambientNoise = 48.0;
        let transitDistance = 250.0;
        let roadCoords = [];
        let ingressCoord = null;

        try {
          const osrmUrl = `https://router.project-osrm.org/nearest/v1/driving/${lng},${lat}?number=8`;
          const osrmRes = await fetch(osrmUrl);
          if (osrmRes.ok) {
            const osrmData = await osrmRes.json();
            if (osrmData && osrmData.waypoints && osrmData.waypoints.length > 0) {
              const wp0 = osrmData.waypoints[0];
              roadDistance = Math.round(wp0.distance * 10) / 10;
              roadName = wp0.name || '';
              ingressCoord = wp0.location;

              const matched = osrmData.waypoints.filter(w => !roadName || w.name === roadName || !w.name);
              roadCoords = matched.map(w => w.location);

              const nameLower = roadName.toLowerCase();
              const isHighway = /motorway|freeway|highway|hwy|expressway|interstate|bypass|autobahn|autoroute|autopista/.test(nameLower);
              const isCollector = /boulevard|blvd|avenue|ave|parkway|way|drive|route/.test(nameLower);

              if (roadDistance > 500) {
                roadType = 'local';
                trafficVolume = Math.max(50, Math.round(1200 / (roadDistance / 50)));
                ambientNoise = 36.0;
                transitDistance = Math.min(2500, Math.round(roadDistance * 1.5));
              } else if (isHighway) {
                roadType = 'arterial';
                trafficVolume = 28000;
                const decay = 10 * Math.log10(Math.max(1.0, roadDistance / 8.0));
                ambientNoise = Math.max(48, Math.min(78, 74.0 - decay));
                transitDistance = 120.0;
              } else if (isCollector) {
                roadType = 'collector';
                trafficVolume = 9500;
                const decay = 10 * Math.log10(Math.max(1.0, roadDistance / 8.0));
                ambientNoise = Math.max(42, Math.min(68, 62.0 - decay));
                transitDistance = 280.0;
              } else {
                roadType = 'local';
                trafficVolume = 2100;
                const decay = 10 * Math.log10(Math.max(1.0, roadDistance / 8.0));
                ambientNoise = Math.max(38, Math.min(58, 52.0 - decay));
                transitDistance = 450.0;
              }
            }
          }
        } catch (e) {
          console.warn('[MapPicker] OSRM road detection error:', e);
        }

        // Fallback road name from Nominatim if OSRM was unnamed
        if (!roadName && geoData && geoData.address && geoData.address.road) {
          roadName = geoData.address.road;
        }
        if (!roadName) {
          roadName = roadDistance > 500 ? 'Off-Grid Access Path' : 'Local Access Road';
        }

        this.currentSite.roadName = roadName;
        this.currentSite.roadDistance = roadDistance;
        this.currentSite.roadType = roadType;
        this.currentSite.trafficVolume = trafficVolume;
        this.currentSite.ambientNoise = Math.round(ambientNoise * 10) / 10;
        this.currentSite.transitDistance = transitDistance;
        this.currentSite.roadCoords = roadCoords;
        this.currentSite.ingressCoord = ingressCoord;

        // Render 2D Road Network & Ingress vector on Leaflet Map
        if (this.roadLayer && this.map) {
          this.map.removeLayer(this.roadLayer);
          this.roadLayer = null;
        }
        if (window.L && this.map) {
          this.roadLayer = window.L.layerGroup();
          if (ingressCoord) {
            // Dashed ingress vector from site marker to road
            const ingressLine = window.L.polyline([[lat, lng], [ingressCoord[1], ingressCoord[0]]], {
              color: '#0284c7',
              weight: 3,
              dashArray: '5, 5',
              opacity: 0.95
            });
            this.roadLayer.addLayer(ingressLine);

            const ingressCircle = window.L.circleMarker([ingressCoord[1], ingressCoord[0]], {
              radius: 6,
              color: '#0284c7',
              fillColor: '#38bdf8',
              fillOpacity: 1,
              weight: 2
            }).bindTooltip(`Ingress Point: ${roadName} (${roadDistance}m)`, { permanent: false, direction: 'top' });
            this.roadLayer.addLayer(ingressCircle);
          }

          if (roadCoords && roadCoords.length >= 2) {
            const roadPoints = roadCoords.map(c => [c[1], c[0]]);
            const roadLine = window.L.polyline(roadPoints, {
              color: '#1e293b',
              weight: 5,
              opacity: 0.85
            }).bindTooltip(`${roadName} (${roadType.toUpperCase()})`, { permanent: false, direction: 'top' });
            this.roadLayer.addLayer(roadLine);
          }
          this.roadLayer.addTo(this.map);
        }

        this.updateMarkerPopup();

        if (coordsElem) {
          coordsElem.innerHTML = `
            <strong>${this.currentSite.name}</strong> (${lat.toFixed(4)}°, ${lng.toFixed(4)}°)<br>
            Elev: ${Math.round(this.currentSite.elevation)}m MSL | Slope: ${this.currentSite.slope.toFixed(1)}° (${slopeDir}) | Relief: ${Math.round(relief)}m<br>
            <strong>Road:</strong> ${roadName} (${roadDistance}m, ${roadType.toUpperCase()}) | Traffic: ${trafficVolume} AADT | Noise: ${this.currentSite.ambientNoise} dB
          `;
        }

        // Live preview in 3D viewport while moving map
        if (this.onSiteSelected) {
          this.onSiteSelected(this.currentSite);
        }

        if (callback) {
          callback(this.currentSite);
        }
      } catch (err) {
        console.warn('[MapPicker] Satellite DEM query error:', err);
        if (coordsElem) {
          coordsElem.textContent = `${lat.toFixed(4)}° N, ${lng.toFixed(4)}° E | Elev: ${Math.round(this.currentSite.elevation)}m`;
        }
      }
    }, 280);
  }

  bindSearchControls() {
    const input = document.getElementById('map-search-input');
    const searchBtn = document.getElementById('map-search-btn');
    const clearBtn = document.getElementById('map-search-clear');
    const locateBtn = document.getElementById('map-locate-btn');

    if (input && input._boundPicker !== this) {
      input._boundPicker = this;
      input.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
          e.preventDefault();
          this.searchLocation(input.value);
        }
      });
      input.addEventListener('input', () => {
        if (clearBtn) {
          clearBtn.style.display = input.value.trim().length > 0 ? 'flex' : 'none';
        }
      });
    }

    if (searchBtn && searchBtn._boundPicker !== this) {
      searchBtn._boundPicker = this;
      searchBtn.addEventListener('click', (e) => {
        e.preventDefault();
        if (input) this.searchLocation(input.value);
      });
    }

    if (clearBtn && clearBtn._boundPicker !== this) {
      clearBtn._boundPicker = this;
      clearBtn.addEventListener('click', (e) => {
        e.preventDefault();
        if (input) {
          input.value = '';
          input.focus();
        }
        clearBtn.style.display = 'none';
        this.clearSearchResults();
      });
    }

    if (locateBtn && locateBtn._boundPicker !== this) {
      locateBtn._boundPicker = this;
      locateBtn.addEventListener('click', (e) => {
        e.preventDefault();
        this.detectCurrentLocation();
      });
    }

    const presets = [
      { id: 'preset-zurich', lat: 47.3769, lng: 8.5417, name: 'Zurich, Switzerland' },
      { id: 'preset-sf', lat: 37.7749, lng: -122.4194, name: 'San Francisco, USA' },
      { id: 'preset-tokyo', lat: 35.6762, lng: 139.6503, name: 'Tokyo, Japan' },
      { id: 'preset-stmoritz', lat: 46.4908, lng: 9.8355, name: 'St. Moritz, Switzerland' },
      { id: 'preset-reykjavik', lat: 64.1466, lng: -21.9426, name: 'Reykjavik, Iceland' },
      { id: 'preset-amalfi', lat: 40.6340, lng: 14.6027, name: 'Amalfi Coast, Italy' }
    ];

    presets.forEach(p => {
      const el = document.getElementById(p.id);
      if (el && el._boundPicker !== this) {
        el._boundPicker = this;
        el.addEventListener('click', (e) => {
          e.preventDefault();
          if (input) input.value = p.name;
          if (clearBtn) clearBtn.style.display = 'flex';
          this.flyToLocation(p.lat, p.lng, p.name);
        });
      }
    });

    if (!this._docClickListener) {
      this._docClickListener = (e) => {
        const resultsEl = document.getElementById('map-search-results');
        const toolbar = document.querySelector('.map-search-toolbar');
        if (resultsEl && !resultsEl.contains(e.target) && (!toolbar || !toolbar.contains(e.target))) {
          this.clearSearchResults();
        }
      };
      document.addEventListener('click', this._docClickListener);
    }
  }

  parseCoordinates(query) {
    if (!query) return null;
    const trimmed = query.trim();

    // Format: "37.7749, -122.4194" or "37.7749 -122.4194"
    const numRegex = /^([+-]?\d+(?:\.\d+)?)\s*[,;\s]\s*([+-]?\d+(?:\.\d+)?)$/;
    const m = trimmed.match(numRegex);
    if (m) {
      const lat = parseFloat(m[1]);
      const lng = parseFloat(m[2]);
      if (!isNaN(lat) && !isNaN(lng) && lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180) {
        return { lat, lng };
      }
    }

    // Format with N/S and E/W, e.g. "47.3769 N, 8.5417 E"
    const dirRegex = /^(\d+(?:\.\d+)?)\s*°?\s*([NSns])\s*[,;\s]\s*(\d+(?:\.\d+)?)\s*°?\s*([EWew])$/;
    const dm = trimmed.match(dirRegex);
    if (dm) {
      let lat = parseFloat(dm[1]);
      if (dm[2].toUpperCase() === 'S') lat = -lat;
      let lng = parseFloat(dm[3]);
      if (dm[4].toUpperCase() === 'W') lng = -lng;
      if (lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180) {
        return { lat, lng };
      }
    }

    return null;
  }

  flyToLocation(lat, lng, locName, zoom = 14) {
    this.currentSite.lat = lat;
    this.currentSite.lng = lng;
    if (locName) this.currentSite.name = locName;

    if (this.map) {
      this.map.flyTo([lat, lng], zoom, { duration: 1.2 });
    }
    if (this.marker) {
      this.marker.setLatLng([lat, lng]);
    }

    const input = document.getElementById('map-search-input');
    const clearBtn = document.getElementById('map-search-clear');
    if (input && locName && !input.value) {
      input.value = locName;
      if (clearBtn) clearBtn.style.display = 'flex';
    }

    this.clearSearchResults();
    this.analyzeSite(lat, lng);
  }

  async searchLocation(query) {
    if (!query || !query.trim()) return;
    const q = query.trim();

    const coordsElem = document.getElementById('map-coords-readout');
    const searchBtn = document.getElementById('map-search-btn');
    if (searchBtn) searchBtn.textContent = 'Searching...';

    // 1. Direct coordinate parsing
    const coords = this.parseCoordinates(q);
    if (coords) {
      if (searchBtn) searchBtn.textContent = 'Search';
      const name = `Coordinates (${coords.lat.toFixed(4)}°, ${coords.lng.toFixed(4)}°)`;
      if (coordsElem) {
        coordsElem.innerHTML = `<em>Targeting coordinates: ${coords.lat.toFixed(5)}°, ${coords.lng.toFixed(5)}°...</em>`;
      }
      this.flyToLocation(coords.lat, coords.lng, name);
      return;
    }

    // 2. OpenStreetMap Nominatim Geocoding
    if (coordsElem) {
      coordsElem.innerHTML = `<em>Searching for "${q}" via OpenStreetMap geocoding...</em>`;
    }

    try {
      if (this.searchAbortController) {
        this.searchAbortController.abort();
      }
      this.searchAbortController = new AbortController();

      const url = `https://nominatim.openstreetmap.org/search?format=jsonv2&q=${encodeURIComponent(q)}&limit=5&addressdetails=1`;
      const res = await fetch(url, {
        signal: this.searchAbortController.signal,
        headers: { 'Accept-Language': 'en' }
      });

      if (!res.ok) throw new Error(`Geocoding HTTP ${res.status}`);

      const data = await res.json();
      if (searchBtn) searchBtn.textContent = 'Search';

      if (!data || data.length === 0) {
        if (coordsElem) {
          coordsElem.innerHTML = `<span style="color:#f87171;">No location results found for "${q}". Try another query or coordinates.</span>`;
        }
        this.clearSearchResults();
        return;
      }

      // If 1 result, fly directly
      if (data.length === 1) {
        const first = data[0];
        const lat = parseFloat(first.lat);
        const lng = parseFloat(first.lon);
        const parts = first.display_name.split(',');
        const name = parts.slice(0, 3).join(',').trim();
        this.flyToLocation(lat, lng, name);
        this.clearSearchResults();
        return;
      }

      // If multiple results, navigate to first immediately and render suggestion dropdown for user refinement
      const first = data[0];
      const lat = parseFloat(first.lat);
      const lng = parseFloat(first.lon);
      const parts = first.display_name.split(',');
      const name = parts.slice(0, 3).join(',').trim();
      this.flyToLocation(lat, lng, name);
      this.renderSearchResults(data);

    } catch (err) {
      if (err.name === 'AbortError') return;
      console.warn('[MapPicker] Search geocoding error:', err);
      if (searchBtn) searchBtn.textContent = 'Search';
      if (coordsElem) {
        coordsElem.innerHTML = `<span style="color:#f87171;">Search request failed. Please check network connection.</span>`;
      }
    }
  }

  renderSearchResults(items) {
    const container = document.getElementById('map-search-results');
    if (!container) return;

    if (!items || items.length <= 1) {
      this.clearSearchResults();
      return;
    }

    container.innerHTML = '';
    container.style.display = 'block';

    const header = document.createElement('div');
    header.className = 'map-search-results-header';
    header.textContent = `Found ${items.length} matching locations (click to inspect):`;
    container.appendChild(header);

    items.forEach(item => {
      const row = document.createElement('div');
      row.className = 'map-search-result-item';

      const lat = parseFloat(item.lat);
      const lng = parseFloat(item.lon);
      const parts = item.display_name.split(',');
      const mainTitle = parts[0].trim();
      const subTitle = parts.slice(1, 4).join(',').trim();

      row.innerHTML = `
        <span class="msr-icon">📍</span>
        <div class="msr-text">
          <div class="msr-title">${mainTitle}</div>
          <div class="msr-sub">${subTitle} (${lat.toFixed(3)}°, ${lng.toFixed(3)}°)</div>
        </div>
      `;

      row.addEventListener('click', () => {
        const input = document.getElementById('map-search-input');
        const clearBtn = document.getElementById('map-search-clear');
        const fullName = `${mainTitle}, ${subTitle}`.trim();
        if (input) input.value = fullName;
        if (clearBtn) clearBtn.style.display = 'flex';
        this.flyToLocation(lat, lng, fullName);
        this.clearSearchResults();
      });

      container.appendChild(row);
    });
  }

  clearSearchResults() {
    const container = document.getElementById('map-search-results');
    if (container) {
      container.innerHTML = '';
      container.style.display = 'none';
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
