// A minimal suborned.world/1 document, type-checked against the contract (bun run typecheck). Not generated.
import type { World } from './world.schema';

export const example = {
  schema: 'suborned.world/1',
  source: { planHash: '0'.repeat(64) },
  id: 'example', name: 'Example', planVersion: '0.0.0', planDate: '2026-01-01',
  view: { yaw: 90, pitch: 38 },
  frame: { units: 'm', x: 'east', y: 'north', unity: '(x, height, y)' },
  heights: { carriageway: 0, pavement: 0.18, skyRailBeam: 24 },
  roads: [{
    id: 'main', name: 'Main', class: 'district', width: 12, gyratory: false, closed: false,
    centreline: [[0, 0], [100, 0]],
    carriageway: [[0, -6], [100, -6], [100, 6], [0, 6]],
    kerb: [[0, -6.8], [100, -6.8], [100, 6.8], [0, 6.8]],
  }],
  medians: [], foot: [],
  plots: [{ id: 'plot', use: 'shops', area: 'centre', built: true, height: 15, polygon: [[10, 10], [40, 10], [40, 30], [10, 30]] }],
  areas: [],
  circus: {
    centre: [0, 0], garden: [[0, 3], [-3, 0], [0, -3], [3, 0]], island: [[0, 5], [-5, 0], [0, -5], [5, 0]],
    carriageway: { inner: [[0, 8], [-8, 0], [0, -8], [8, 0]], outer: [[0, 20], [-20, 0], [0, -20], [20, 0]] },
    pavement: [[[0, 24], [-24, 0], [0, -24], [24, 0]]],
    radii: { garden: 3, island: 5, carriagewayInner: 8, carriagewayOuter: 20, pavement: 24 },
  },
  water: { water: [], quay: [], beyond: [] },
  tram: [], stops: [], skyRail: [], pylons: [], stations: [],
  gates: [{ id: 'gate', name: 'Gate', at: [11, 0], dir: [1, 0], road: 'main', area: 'centre' }],
  landmarks: [],
  handover: { note: '', blocks: [], buildings: [], structures: [], objects: [], bays: [], crossing: [[0, 0], [5, 0], [5, 5], [0, 5]], points: [], crew: [] },
} satisfies World;
