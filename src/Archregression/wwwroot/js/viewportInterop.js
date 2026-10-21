/**
 * wwwroot/js/viewportInterop.js
 * =============================
 * JavaScript Interop Bridge called directly by F# Bolero WebAssembly.
 * Connects F# Domain Models to Three.js WebGL rendering, Plotly regression analytics, and Leaflet GIS.
 */

import { SpatialViewport } from './viewport.js';
import { RegressionAnalytics } from './analytics.js';
import { GlobalMapPicker } from './mapPicker.js';

let viewportInstance = null;
let analyticsInstance = null;
let mapPickerInstance = null;

const targetMap = {
  'sda': 'sda_percent',
  'cooling': 'cooling_load',
  'stress': 'peak_stress',
  'carbon': 'embodied_carbon',
  'biophilia': 'biophilic_view',
  'microclimate': 'microclimate_cooling'
};

export function initViewport() {
  const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
  const container = document.getElementById('viewport-3d');
  if (container && !viewportInstance) {
    try {
      viewportInstance = new SpatialViewport(container);
      viewportInstance.setTheme(currentTheme);
    } catch (e) {
      console.warn('[Interop] Viewport init error:', e);
    }
  }

  const parityEl = document.getElementById('chart-parity');
  const shapEl = document.getElementById('chart-shap');
  const residualEl = document.getElementById('chart-residual');

  if (parityEl && shapEl && residualEl && !analyticsInstance) {
    try {
      analyticsInstance = new RegressionAnalytics({
        parity: parityEl,
        shap: shapEl,
        residual: residualEl
      });
      analyticsInstance.setTheme(currentTheme);
      analyticsInstance.initialize();
    } catch (e) {
      console.warn('[Interop] Analytics init error:', e);
    }
  }
}

export function initMapPicker() {
  const container = document.getElementById('map-picker-container');
  if (container) {
    if (!mapPickerInstance) {
      mapPickerInstance = new GlobalMapPicker('map-picker-container', (selectedSite) => {
        if (viewportInstance) {
          viewportInstance.applySiteData(selectedSite);
        }
      });
      mapPickerInstance.initialize();
    } else {
      if (mapPickerInstance.map) {
        setTimeout(() => mapPickerInstance.map.invalidateSize(), 200);
      }
      mapPickerInstance.bindSearchControls();
    }
  }
}

window.archInterop = {
  init: () => {
    initViewport();
  },

  initMap: () => {
    initMapPicker();
  },

  searchLocation: (query) => {
    if (mapPickerInstance) {
      mapPickerInstance.searchLocation(query);
    }
  },

  flyToLocation: (lat, lng, name) => {
    if (mapPickerInstance) {
      mapPickerInstance.flyToLocation(lat, lng, name);
    }
  },

  detectLocation: () => {
    if (mapPickerInstance) {
      mapPickerInstance.detectCurrentLocation();
    }
  },

  detectLocationAsync: () => {
    return new Promise((resolve) => {
      if (mapPickerInstance) {
        mapPickerInstance.detectCurrentLocation((site) => {
          resolve(JSON.stringify(site));
        });
      } else {
        if ('geolocation' in navigator) {
          navigator.geolocation.getCurrentPosition(
            async (pos) => {
              const lat = pos.coords.latitude;
              const lng = pos.coords.longitude;
              try {
                const elevUrl = `https://api.open-meteo.com/v1/elevation?latitude=${lat}&longitude=${lng}`;
                const res = await fetch(elevUrl);
                const elevData = await res.json();
                const elev = elevData && elevData.elevation ? elevData.elevation[0] : 100.0;
                resolve(JSON.stringify({ name: 'Current Detected Site', lat, lng, elevation: elev, elevationGrid: [] }));
              } catch (_) {
                resolve(null);
              }
            },
            () => resolve(null),
            { timeout: 6000 }
          );
        } else {
          resolve(null);
        }
      }
    });
  },

  applyMapSite: () => {
    if (mapPickerInstance) {
      mapPickerInstance.applyCurrentSite();
    }
  },

  getMapSiteJson: () => {
    return mapPickerInstance ? mapPickerInstance.getCurrentSiteJson() : null;
  },

  updateArchitecture: (params) => {
    if (!viewportInstance) initViewport();
    if (viewportInstance) {
      viewportInstance.updateParameters(params);
    }
    if (analyticsInstance && params) {
      analyticsInstance.updateCurrentDesign(params);
    }
  },

  applySite: (siteData) => {
    if (!viewportInstance) initViewport();
    if (viewportInstance) {
      viewportInstance.applySiteData(siteData);
    }
  },

  setMetric: (metricKey) => {
    if (viewportInstance) {
      viewportInstance.setMetric(metricKey);
    }
    if (analyticsInstance) {
      const target = targetMap[metricKey] || metricKey;
      analyticsInstance.setTarget(target);
    }
  },

  setCameraView: (viewName) => {
    if (viewportInstance) {
      viewportInstance.setCameraView(viewName);
    }
  },

  toggleSpatialGraph: (show) => {
    if (viewportInstance) {
      viewportInstance.toggleSpatialGraph(show);
    }
  },

  toggleWireframe: (show) => {
    if (viewportInstance) {
      viewportInstance.toggleWireframe(show);
    }
  },

  togglePlantMode: (active) => {
    if (viewportInstance) {
      viewportInstance.togglePlantTreeTool(active);
    }
  },

  exportGLB: () => {
    if (viewportInstance) {
      viewportInstance.exportGLB();
    }
  },

  resizeCharts: () => {
    setTimeout(() => {
      ['chart-parity', 'chart-shap', 'chart-residual'].forEach(id => {
        const el = document.getElementById(id);
        if (el && window.Plotly) {
          try { window.Plotly.Plots.resize(el); } catch (_) {}
        }
      });
    }, 150);
  },

  resizeViewport: () => {
    if (viewportInstance) {
      setTimeout(() => viewportInstance.onWindowResize(), 100);
    }
  },

  setTheme: (theme) => {
    document.documentElement.setAttribute('data-theme', theme);
    try {
      localStorage.setItem('arch_theme', theme);
    } catch (_) {}
    if (viewportInstance) {
      viewportInstance.setTheme(theme);
    }
    if (analyticsInstance) {
      analyticsInstance.setTheme(theme);
    }
  },

  getTheme: () => {
    try {
      return localStorage.getItem('arch_theme') || document.documentElement.getAttribute('data-theme') || 'light';
    } catch (_) {
      return document.documentElement.getAttribute('data-theme') || 'light';
    }
  }
};



// Retry initialization until Blazor mounts DOM elements
let initAttempts = 0;
const pollInterval = setInterval(() => {
  initAttempts++;
  initViewport();
  if (viewportInstance && analyticsInstance) {
    clearInterval(pollInterval);
  } else if (initAttempts > 20) {
    clearInterval(pollInterval);
  }
}, 250);
