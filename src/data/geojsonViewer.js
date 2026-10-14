/**
 * src/data/geojsonViewer.js
 * =========================
 * GeoJSON 2D Floor Plan & Spatial Network Inspector.
 * 
 * Parses GeoJSON Polygon features natively in zero-dependency JavaScript
 * and renders an interactive architectural spatial layout with room boundaries,
 * adjacency vectors, and daylight zones.
 */

export class GeoJsonFloorplanViewer {
  constructor(containerElement) {
    this.container = containerElement;
    this.geojson = null;
  }

  async loadFloorplan() {
    try {
      const response = await fetch('./public/data/floorplan.geojson');
      if (response.ok) {
        this.geojson = await response.json();
        this.render();
      }
    } catch (err) {
      console.warn('[GeoJsonViewer] Could not load floorplan.geojson:', err);
    }
  }

  render() {
    if (!this.container || !this.geojson) return;

    const width = 460;
    const height = 340;
    const scale = 20; // 20px per meter
    const cx = width / 2;
    const cy = height / 2;

    const typeColors = {
      Atrium: 'rgba(56, 189, 248, 0.25)',
      Workplace: 'rgba(74, 222, 128, 0.25)',
      Meeting: 'rgba(168, 85, 247, 0.25)',
      Service: 'rgba(239, 68, 68, 0.25)',
      Research: 'rgba(245, 158, 11, 0.25)'
    };

    const typeBorders = {
      Atrium: '#38bdf8',
      Workplace: '#4ade80',
      Meeting: '#a855f7',
      Service: '#ef4444',
      Research: '#f59e0b'
    };

    let polygonsSvg = '';
    let labelsSvg = '';

    this.geojson.features.forEach(feature => {
      const coords = feature.geometry.coordinates[0];
      const props = feature.properties;
      const fillColor = typeColors[props.type] || 'rgba(148, 163, 184, 0.2)';
      const strokeColor = typeBorders[props.type] || '#94a3b8';

      // Convert geo coords to SVG path points
      const points = coords.map(([x, y]) => {
        const sx = cx + x * scale;
        const sy = cy - y * scale; // Invert Y for screen coordinates
        return `${sx.toFixed(1)},${sy.toFixed(1)}`;
      }).join(' ');

      // Compute centroid for label
      let avgX = 0, avgY = 0;
      coords.forEach(([x, y]) => {
        avgX += x;
        avgY += y;
      });
      avgX /= coords.length;
      avgY /= coords.length;
      const lx = cx + avgX * scale;
      const ly = cy - avgY * scale;

      polygonsSvg += `
        <polygon points="${points}" 
                 fill="${fillColor}" 
                 stroke="${strokeColor}" 
                 stroke-width="1.8" 
                 class="floorplan-poly"
                 data-zone="${props.name}">
          <title>${props.name} (${props.area_m2} m² | ${props.target_illuminance_lux} lux)</title>
        </polygon>
      `;

      labelsSvg += `
        <text x="${lx}" y="${ly - 4}" fill="#ffffff" font-size="10" font-weight="600" text-anchor="middle" pointer-events="none">${props.name}</text>
        <text x="${lx}" y="${ly + 10}" fill="#94a3b8" font-size="9" text-anchor="middle" pointer-events="none">${props.area_m2} m² | ${props.type}</text>
      `;
    });

    this.container.innerHTML = `
      <div class="geojson-viewer-card">
        <div class="geojson-header">
          <div class="geojson-title">
            <span class="icon">🗺️</span>
            <span>GeoJSON 2D Spatial Plan & Adjacencies</span>
          </div>
          <span class="badge badge-cyan">Zero Dependencies</span>
        </div>
        <svg viewBox="0 0 ${width} ${height}" class="floorplan-svg">
          <!-- Grid Lines -->
          <defs>
            <pattern id="smallGrid" width="20" height="20" patternUnits="userSpaceOnUse">
              <path d="M 20 0 L 0 0 0 20" fill="none" stroke="#1e293b" stroke-width="0.7"/>
            </pattern>
          </defs>
          <rect width="100%" height="100%" fill="url(#smallGrid)" />
          <!-- Polygons -->
          ${polygonsSvg}
          <!-- Labels -->
          ${labelsSvg}
        </svg>
        <div class="geojson-footer">
          <small>Parsed directly from <code>public/data/floorplan.geojson</code> into browser DOM.</small>
        </div>
      </div>
    `;
  }
}
