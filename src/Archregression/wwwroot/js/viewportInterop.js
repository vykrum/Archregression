/**
 * wwwroot/js/viewportInterop.js
 * =============================
 * JavaScript Interop Bridge called directly by F# Bolero WebAssembly.
 * Connects F# Domain Models to Three.js WebGL rendering and Leaflet GIS.
 */

import { SpatialViewport } from '../../../../src/viewer/viewport.js';

let viewportInstance = null;

export function initViewport() {
  const container = document.getElementById('viewport-3d');
  if (container && !viewportInstance) {
    viewportInstance = new SpatialViewport(container);
  }
}

window.archInterop = {
  init: () => {
    initViewport();
  },

  updateArchitecture: (params) => {
    if (!viewportInstance) initViewport();
    if (viewportInstance) {
      viewportInstance.updateParameters(params);
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

// Initialize as soon as container is mounted
setTimeout(() => {
  initViewport();
}, 200);
