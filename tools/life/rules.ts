// the constants behind contracts/world/harbour-district.life.json. [S] = starting value, tune by playtest.
export const BANDS = [
  { id: 'night', h: [0, 5] }, { id: 'early', h: [5, 8] }, { id: 'morning', h: [8, 12] }, { id: 'afternoon', h: [12, 16] },
  { id: 'peak', h: [16, 19] }, { id: 'evening', h: [19, 23] }, { id: 'late', h: [23, 24] },
] as const;
export type Band = typeof BANDS[number]['id'];
export const bandOf = (h: number): Band => BANDS.find(b => h >= b.h[0] && h < b.h[1])!.id;

// category of a frontage trade -> how much of its day each band draws (0..1)
export const DRAW: Record<string, Record<Band, number>> = {
  retail:    { night: 0, early: 0.1, morning: 0.6, afternoon: 0.8, peak: 1.0, evening: 0.3, late: 0 },
  food:      { night: 0.1, early: 0.5, morning: 0.6, afternoon: 0.8, peak: 0.9, evening: 1.0, late: 0.4 },
  nightlife: { night: 0.7, early: 0, morning: 0, afternoon: 0, peak: 0.3, evening: 1.0, late: 1.0 },
  cinema:    { night: 0.2, early: 0, morning: 0.2, afternoon: 0.6, peak: 0.8, evening: 1.0, late: 0.5 },
  service:   { night: 0, early: 0.2, morning: 0.8, afternoon: 0.8, peak: 0.7, evening: 0.1, late: 0 },
  kiosk:     { night: 0.1, early: 0.8, morning: 0.6, afternoon: 0.6, peak: 1.0, evening: 0.5, late: 0.2 },
  trade:     { night: 0, early: 0.6, morning: 1.0, afternoon: 0.8, peak: 0.7, evening: 0.2, late: 0 },
  lobby:     { night: 0.2, early: 0.5, morning: 0.8, afternoon: 0.5, peak: 1.0, evening: 0.6, late: 0.3 },
  market:    { night: 0, early: 0.2, morning: 0.7, afternoon: 1.0, peak: 0.8, evening: 0.2, late: 0 },
};
// category params: floorRho people/m2 inside at full draw; spillRho people/m2 on the 3 m spill at full draw; dwell s
export const CAT: Record<string, { floorRho: number; spillRho: number; dwell: number }> = {
  retail: { floorRho: 0.15, spillRho: 0.165, dwell: 900 }, food: { floorRho: 0.5, spillRho: 0.248, dwell: 2400 },
  nightlife: { floorRho: 0.6, spillRho: 0.33, dwell: 5400 }, cinema: { floorRho: 0.5, spillRho: 0.33, dwell: 7200 },
  service: { floorRho: 0.1, spillRho: 0.066, dwell: 900 }, kiosk: { floorRho: 0.5, spillRho: 0.44, dwell: 150 },
  trade: { floorRho: 0.04, spillRho: 0.044, dwell: 1200 }, lobby: { floorRho: 0.05, spillRho: 0.066, dwell: 600 },
  market: { floorRho: 0.4, spillRho: 0.275, dwell: 600 },
};
export const CATEGORY: Record<string, string> = {
  diner: 'food', 'soda-fountain': 'food', 'cocktail-lounge': 'food', 'tiki-bar': 'food', 'dockside-tavern': 'food',
  automat: 'kiosk', newsstand: 'kiosk', florist: 'kiosk', 'tram-kiosk': 'kiosk', 'skyrail-kiosk': 'kiosk',
  'jazz-club': 'nightlife', 'dance-hall': 'nightlife', bowling: 'nightlife', cinema: 'cinema',
  'appliance-showroom': 'retail', 'department-store': 'retail', 'couture-boutique': 'retail', 'record-shop': 'retail',
  'camera-shop': 'retail', tailor: 'retail', grocer: 'retail',
  barber: 'service', 'beauty-salon': 'service', launderette: 'service', 'radio-tv-repair': 'service', pawnbroker: 'service',
  'credit-bureau': 'service', 'employment-bureau': 'service', 'travel-bureau': 'service',
  'robot-repair': 'trade', 'hover-garage': 'trade', 'service-station': 'trade', 'rocket-parts-chandler': 'trade',
  'ships-chandler': 'trade', 'bonded-warehouse': 'trade', 'hiring-hall': 'trade',
  lobby: 'lobby', 'hotel-lobby': 'lobby', 'market-stall': 'market',
};
export const K = {
  pavement: 6,          // plan standards.frontage.placement.spill.minPavement
  spillDepth: 3,        // plan standards.frontage.placement.spill.depth
  walkSpeed: 1.4,       // r008 timetable openQuestions: 1.4 m/s [S]
  lastLeg: 60,          // plan standards.roads.junctionSpacing.district
  staffM2: 30,          // [S] one staff per 30 m2 of floor
  homeM2: 60, perHome: 2.2, efficiency: 0.8, // [S] gross floor per flat, people per flat, net/gross
  shiftHours: 8,        // [S]
  walkersMultiplier: 2, // in + out leg
  reach: 250,           // plan standards.transit.reach
  feederShare: 0.3,     // [S] share of an access street's trips that ride a main street
};
// residents' street trips per resident per hour, by band [S]
export const RES_TRIPS: Record<Band, number> = { night: 0.02, early: 0.25, morning: 0.35, afternoon: 0.15, peak: 0.45, evening: 0.25, late: 0.1 };
// leisure occupancy of public ground (people/m2), by place type and band [S]
export const PROM: Record<string, Record<Band, number>> = {
  mall:   { night: 0.01, early: 0.01, morning: 0.03, afternoon: 0.06, peak: 0.075, evening: 0.06, late: 0.02 },
  walk:   { night: 0, early: 0.01, morning: 0.02, afternoon: 0.03, peak: 0.04, evening: 0.03, late: 0.01 },
  square: { night: 0, early: 0.01, morning: 0.03, afternoon: 0.05, peak: 0.07, evening: 0.04, late: 0.01 },
  road:   { night: 0, early: 0, morning: 0.004, afternoon: 0.006, peak: 0.01, evening: 0.008, late: 0 },
};
export const inOpen = (hours: any, h: number) => hours === 'service' ? h >= 5 || h < 1 : hours[0] <= hours[1] ? h >= hours[0] && h < hours[1] : h >= hours[0] || h < hours[1];
