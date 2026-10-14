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
  const container = document.getElementById('viewport-3d');
  if (container && !viewportInstance) {
    try {
      viewportInstance = new SpatialViewport(container);
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
    } else if (mapPickerInstance.map) {
      setTimeout(() => mapPickerInstance.map.invalidateSize(), 200);
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

  selectMapPreset: (presetKey) => {
    if (mapPickerInstance) {
      mapPickerInstance.selectPreset(presetKey);
    }
  },

  applyMapSite: () => {
    if (mapPickerInstance) {
      mapPickerInstance.applyCurrentSite();
    }
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
