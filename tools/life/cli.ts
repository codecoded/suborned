// writes contracts/world/harbour-district.life.json from the plan (incl. its timetable, A04 r008 round 3) and rules.ts
import plan from '../../contracts/world/harbour-district.plan.json';
import { streets, frontage, plots } from './geom';
import { BANDS, DRAW, CAT, CATEGORY, K, RES_TRIPS, PROM, type Band } from './rules';
import { mixTerms, homesPop } from './model';
const P: any = plan, tt: { lines: any[] } = P.timetable, T: Record<string, any> = P.standards.frontage.trades, r2 = (n: number) => Math.round(n * 100) / 100;
const bands = BANDS.map(b => b.id);
const byBand = <T>(f: (b: Band) => T) => Object.fromEntries(bands.map(b => [b, f(b as Band)])) as Record<string, T>;
const midH = (b: Band) => { const h = BANDS.find(x => x.id === b)!.h; return (h[0] + h[1]) / 2; };
const hrs = (b: Band) => { const h = BANDS.find(x => x.id === b)!.h; return h[1] - h[0]; };

// ---- frontage metres per area and trade (non-primary streets, built plots only)
const metres: Record<string, Record<string, number>> = {};
for (const s of streets()) { if (s.cls === 'primary') continue;
  for (const x of frontage(s, K.pavement)) { const h = x.hit; if (!h || !h.mix || ['park', 'open', 'square'].includes(h.use)) continue;
    for (const [t, w] of Object.entries(h.mix)) { const m = (metres[h.area] ??= {}); m[t] = (m[t] ?? 0) + (w as number) / 100 * 4; } } }
const occ = Object.fromEntries(Object.entries(T).map(([t, v]) => {
  const mw = (v.w[0] + v.w[1]) / 2, cat = CATEGORY[t];
  return [t, { category: cat, hours: v.hours, meanWidth: mw, depth: v.depth, staffPerUnit: Math.max(1, Math.round(mw * v.depth / K.staffM2)), shifts: v.hours === 'service' ? 'station service hours' : { lengthH: K.shiftHours } }];
}));
const expected = Object.fromEntries(Object.entries(metres).map(([a, m]) => {
  let units = 0, staff = 0; const per: Record<string, any> = {};
  for (const [t, len] of Object.entries(m)) { if (t === 'tram-kiosk' || t === 'skyrail-kiosk') continue; const u = len / occ[t].meanWidth; units += u; staff += u * occ[t].staffPerUnit; per[t] = { units: r2(u) }; }
  return [a, { frontageMetres: Math.round(Object.values(m).reduce((s, v) => s + v, 0)), units: Math.round(units), staff: Math.round(staff), trades: per }];
}));
const homes = plots.map(p => ({ plot: p.id, area: p.area, use: p.use, residents: homesPop(p) })).filter(h => h.residents > 0);

// ---- character: lively/quiet from open hours x draw (street-level mix of each area)
const cls = (d: number) => d >= 0.45 ? 'lively' : d >= 0.2 ? 'steady' : 'quiet';
const character = Object.fromEntries(P.areas.map((a: any) => [a.id, byBand(b => {
  const d = mixTerms(P.standards.frontage.mix[a.id], midH(b)).draw; return { score: r2(d), mood: cls(d) };
})]));

// ---- transit services from the plan timetable (A04 r008 round 3)
const order: Record<string, string[]> = {};
for (const l of tt.lines) order[l.line] = [l.runs[0].from, ...l.runs.map((r: any) => r.to)];
const planStops = new Set<string>([...P.tram.flatMap((l: any) => l.stops.map((s: any) => `${l.id}/${s.id}`)), ...P.skyrail.flatMap((l: any) => l.stations.map((s: any) => `${l.id}/${s.id}`))]);
for (const l of tt.lines) for (const s of order[l.line]) if (!planStops.has(s)) throw new Error(`timetable stop ${s} is not a stop or station in the plan`);
const loads: Record<string, number> = { 'T1/comet': 6, 'T1/records': 6, 'T1/circus': 10, 'T2/harbour': 6, 'T2/harbour-court': 4, 'T2/docks': 6, 'T2/comet': 6, 'T2/lantern': 8, 'T2/rocket': 4, 'T2/terraces': 5, 'S1/gardens': 12, 'S1/circus': 24, 'S1/harbour': 14, 'S1/skyport': 20 };
const services = tt.lines.map((l: any) => { let t = 0; const off: Record<string, number> = {}; order[l.line].forEach((s, i) => { off[s.split('/')[1]] = t; if (i < l.runs.length) t += l.runs[i].sec + l.dwellSec; });
  return { line: l.line, headwaySec: l.headwaySec, dwellSec: l.dwellSec, speed: l.speed, stops: order[l.line].map(s => s.split('/')[1]), runSec: l.runs.map((r: any) => r.sec), offsetSec: off, loadPerService: Object.fromEntries(order[l.line].map(s => [s.split('/')[1], loads[s]])) }; });

const trafficMult = { night: 0.08, early: 0.5, morning: 1.0, afternoon: 0.6, peak: 1.0, evening: 0.4, late: 0.15 };
const lanes = (w: number, med: number) => Math.max(1, Math.floor((w - med) / 2 / 3.3));
const delivVisits: Record<string, number> = { food: 2, retail: 1, kiosk: 1, nightlife: 1, cinema: 0, service: 0.5, trade: 3, lobby: 0.5, market: 2 };
const delivBand: Record<string, number> = { night: 0.02, early: 0.25, morning: 0.35, afternoon: 0.25, peak: 0.08, evening: 0.04, late: 0.01 };
const deliveries = Object.fromEntries(Object.entries(metres).map(([a, m]) => { let daily = 0;
  for (const [t, len] of Object.entries(m)) daily += len / occ[t].meanWidth * (delivVisits[CATEGORY[t]] ?? 0);
  return [a, { visitsPerDay: Math.round(daily), perHour: byBand(b => r2(daily * delivBand[b] / hrs(b))) }]; }));

const life = {
  schema: 'suborned.life/1',
  plan: { id: P.id, version: P.version, file: 'contracts/world/harbour-district.plan.json' },
  author: 'Pulse (brief 005 B); generated by tools/life/cli.ts from tools/life/rules.ts and the plan (timetable: plan.timetable, A04 r008 round 3)',
  rule: 'Rules, not individuals. Splice generates people and vehicles from these inputs at build time; Keel steps them. Nothing here runs an AI. Same plan + same orders = same city. Tags: [P] plan, [T] r008 timetable, [G] GDD, [S] starting value, tune by playtest.',
  clock: {
    src: 'GDD §01 mission 8-12 min on a live day clock; brief 005',
    unit: 's', scale: '1 mission s = 1 day-clock s (timetable headways are real seconds) [S]',
    day: { start: '00:00', end: '24:00' },
    mission: { windowMin: [8, 12], note: 'a mission sets its own start on the day clock (contracts/missions/<id>/); every rule here follows the clock [GDD §01]' },
    bands: BANDS.map(b => ({ id: b.id, from: b.h[0], to: b.h[1] })),
    bandSrc: '[S] seven bands; peak 16-19 brackets plan trade closings (showroom 19, department store 20) and opens (tiki-bar 16, cocktail-lounge 17)',
  },
  transit: {
    src: 'plan.timetable (A04 r008 round 3) + r008 pulse.json loads [T]; GDD §02 travel (price = wait + ride, time only); plan transit.reach/stopSpacing/stationSpacing [P]',
    schedule: 'departure k from a line first stop at phase + k x headwaySec on the day clock; phase 0 so a service leaves on every multiple of the headway; reverse direction phase = headway/2 [S]; stop i is reached at departure + offsetSec[stop]; dwellSec then runSec',
    serviceHours: { from: 5, to: 25, note: 'runs 05:00-01:00 [S]; none 01:00-05:00' },
    headwayMultiplierByBand: byBand(b => b === 'night' ? 0 : b === 'late' ? 2 : b === 'early' ? 1.5 : 1), headwayNote: '[S] multiplier on headwaySec; 0 = no service. Mission bands run at the timetable headway, so the timetable price (wait + ride) is the price in play.',
    walkSpeed: K.walkSpeed, walkSpeedSrc: 'r008 timetable openQuestions: 1.4 m/s [S]',
    interchange: { pairs: { 'T1/circus': 'S1/circus', 'T1/comet': 'T2/comet', 'T2/harbour': 'S1/harbour' }, maxWalkM: P.standards.skyrail.interchange, src: 'r008 notes; plan skyrail.interchange 150' },
    services,
    loadSrc: '[S] people alighting and boarding per service at the peak band, scaled by loadByBand',
    loadByBand: byBand(b => trafficMult[b as Band]),
    kiosks: 'every stop gets a tram-kiosk, every station a skyrail-kiosk, regardless of area [P frontage.mixNote]',
  },
  traffic: {
    src: 'plan roads classes and widths [P]; Buchanan note in plan frontage.placement; lane model [S]: lanes per direction = floor((w - median)/2/3.3)',
    laneWidth: 3.3,
    byClass: {
      primary: { lanes: lanes(24, 4), flowPerDirPeak: 600, speed: 11, mix: { car: 0.5, 'hover-car': 0.3, van: 0.15, lorry: 0.05 }, note: 'through traffic, no frontage access [P]' },
      district: { lanes: lanes(18, 6), lanesBoulevard: lanes(28, 10), flowPerDirPeak: 350, speed: 8, mix: { car: 0.45, 'hover-car': 0.3, van: 0.2, cart: 0.05 }, note: 'boulevard shares with the T1 reservation (10 m) [P]' },
      access: { lanes: lanes(12, 0), flowPerDirPeak: 80, speed: 5, mix: { car: 0.3, 'hover-car': 0.2, van: 0.3, cart: 0.2 }, note: 'service bays open here and on the quay [P]' },
      mall: { lanes: 0, flowPerDirPeak: 0, mix: { cart: 1 }, carts: { perHour: 20, windowHours: [5, 11] }, note: 'pedestrian; carts to service bays before opening [S]' },
      quay: { lanes: 1, flowPerDirPeak: 40, speed: 4, mix: { lorry: 0.6, van: 0.3, cart: 0.1 }, note: 'port working ground [P quay]' },
    },
    speedUnit: 'm/s', flowUnit: 'vehicles per hour per direction at band multiplier 1',
    byBand: byBand(b => trafficMult[b as Band]), bandSrc: '[S]',
    areaBias: { docks: { lorry: 2.0 }, lantern: { car: 1.3, 'hover-car': 1.3 } },
    rules: ['vehicles follow lanes; they leave only to jump clear (GDD §06 vehicle strike)', 'a stalled lorry or tram blocks (GDD §06 citizen actions)', 'spawn at the district edge gates [P gates], seed = plan id + clock'],
  },
  deliveries: {
    src: 'plan frontage service strings and placement.service [P]: roller bays (4.5 x 4.6) only on access roads, the quay or under a deck; never on a district road or the mall',
    visitsPerUnitPerDay: delivVisits, bandShare: delivBand, bandSrc: '[S]; most before opening',
    vehicle: { default: 'van', trade: 'lorry', dwellSec: { van: 360, lorry: 720, cart: 180 }, blocksLane: ['lorry'], dwellSrc: '[S]' },
    expected: deliveries, expectedSrc: 'visitsPerDay = sum over frontage units of visitsPerUnitPerDay[category]; perHour = visitsPerDay x bandShare / band hours; units from generator.expected',
  },
  pedestrians: {
    src: 'plan frontage trades (hours, w, depth, spill) [P]; GDD §15 density ~0.12 people/m2 on busy streets (as quoted in brief 005); [S] constants',
    walkSpeed: K.walkSpeed, pavement: K.pavement, pavementSrc: 'plan frontage.placement.spill.minPavement', spillDepth: K.spillDepth, spillDepthSrc: 'plan frontage.placement.spill.depth',
    lastLegM: K.lastLeg, lastLegSrc: 'plan roads junctionSpacing.district', reachM: K.reach, reachSrc: 'plan standards.transit.reach',
    densityTarget: { value: 0.12, unit: 'people/m2 of foot ground (both pavements + median; whole width on mall and walks)', where: 'busy streets: district, mall, walk, at the peak band', src: 'brief 005 / GDD §15' },
    densityNote: 'Primary roads: no frontage (plan), quiet pavements. Foot ground is the basis because crossing is allowed anywhere (plan walking.crossAnywhere). Over the whole corridor (carriageway included) the same crowd reads 0.03-0.10 on the district streets.',
    terms: {
      spill: 'people outside a trade = frontage m x spillDepth x spillRho x open(hours) x draw(category, band)',
      walkers: 'Little law on the last leg: 2 x customersInside / dwell x lastLeg / walkSpeed, customersInside = depth x floorRho x open x draw',
      feeders: 'main streets take feederShare of the walkers of access streets that join them, riding reach/2 to a stop',
      promenade: 'leisure occupancy of public ground by place type and band',
      surges: 'events below multiply spill and walkers in their area for their window',
    },
    feederShare: K.feederShare, feederShareSrc: '[S]', walkersMultiplier: K.walkersMultiplier,
    category: Object.fromEntries(Object.keys(CAT).map(c => [c, { ...CAT[c], draw: DRAW[c], trades: Object.keys(CATEGORY).filter(t => CATEGORY[t] === c) }])),
    categorySrc: 'floorRho, spillRho, dwell, draw are [S]; trade membership and open hours are [P]',
    promenade: PROM, promenadeSrc: '[S]; mall = Crown Parade, walk = Garden Walk and Skyport Pier, square = Records Square and the Crown Circus pavement (51 m) [P], road = other pavements',
    levels: {
      src: 'plan standards.levels, megastructures [P]; GDD §05 levels',
      street: { z: 0, mixFrom: 'area mix, or a megastructure street mix inside its footprint' },
      deck: { z: 9.43, mixFrom: 'megastructure deck mix (parade-galleries, market-deck, crown-terraces)', promenade: { night: 0, early: 0, morning: 0.01, afternoon: 0.03, peak: 0.04, evening: 0.04, late: 0.01 }, src: '[S]', tripShare: 0.15, tripShareNote: '[S] share of last-leg trips that take the deck where one covers the route' },
      concourse: { z: 24, mixFrom: 'megastructure concourse mix; stations', promenade: { night: 0, early: 0.02, morning: 0.04, afternoon: 0.03, peak: 0.05, evening: 0.03, late: 0.01 }, src: '[S]; plus the S1 loads, arriving by the station banks (>= 2 per station [P])' },
    },
  },
  residents: { src: 'plan plots + standards.buildings.bands [P]; [S] flat sizes', tripsPerHour: RES_TRIPS, grossPerHomeM2: K.homeM2, perHome: K.perHome, efficiency: K.efficiency, builtShare: 0.7, rule: 'residents = rect area x builtShare x storeys (mode; mixed uses mode - podium) x efficiency / grossPerHome x perHome' },
  events: {
    src: 'plan hiring-hall hours [5,14]; trade hours [P]; [S] surge numbers',
    list: [
      { id: 'docks-shift', area: 'docks', atHours: [6, 14, 22], windowS: 1200, surge: 2.0, who: 'port and industrial workers; hiring-hall crowd at 5 (plan "a crowd at 5")', src: 'plan hiring-hall hours; shift length 8 h [S]' },
      { id: 'lantern-open', area: 'lantern', atHours: [17, 20], windowS: 1800, surge: 1.5, who: 'cocktail lounges open 17, jazz club and dance hall 20, queues form', src: 'plan trade hours' },
      { id: 'cinema-out', area: 'crescent', atHours: [14, 17, 20, 23], windowS: 600, surge: 1.4, who: 'cinema lets out [S]', src: 'plan cinema hours [12,24]' },
    ],
  },
  character: {
    src: 'rule: score = sum over the area street mix of share x open(hour) x draw(category, band), lobbies excluded; lively >= 0.45, steady >= 0.2, else quiet [S]. Evaluated at mid-band.',
    thresholds: { lively: 0.45, steady: 0.2 }, table: character,
    effects: { lively: 'spill and walkers as computed; witnesses plentiful, crowds block sightlines (GDD §05)', steady: 'as computed', quiet: 'empty pavements: a witness stands out' },
    standing: 'Hero: spill x1.15; Menace: spill x0.7 [S] in that district (GDD §04 street row)',
  },
  generator: {
    src: 'GDD §04 person = routine + need + temperament + memory; §09 people: occupation x temperament x need x relationships x routine, fit to district schedules',
    occupations: occ,
    expected: { workersPerArea: expected, note: 'frontage metres from the plan streets (built plots, non-primary); units = metres x mix share / mean width; staff = units x staffPerUnit. Splice replaces with its exact placed frontage.' },
    stationWorkers: { src: 'plan frontage.mixNote', rule: 'one kiosk worker per stop and per station, service hours 5:00-1:00, shift 8 h' },
    civicEmployers: [{ plot: 'records-office', area: 'records', src: 'plan civic block', staff: 'Splice sets from the civic block', guard: 'door routine, café stop pre-shift (GDD §05)' }],
    homes,
    commuters: { src: 'plan transit stations; services above', rule: 'alighting per service per station x loadByBand; origin = beyond the sheet (plan gates); walk to home or work via the station banks', stations: services.flatMap(s => s.stops.map(st => `${s.line}/${st}`)) },
    routines: {
      worker: ['home', 'commute (station or walk)', 'work (shift)', 'meal (food trade nearby)', 'work', 'commute', 'home'],
      resident: ['home', 'errand (retail, service)', 'home', 'evening (food, nightlife, cinema)'],
      shopper: ['stop or station', 'retail frontage x n', 'food', 'stop or station'],
      reveller: ['food or bar at peak/evening', 'nightlife in evening/late', 'home by train or walk'],
      docker: ['home', 'hiring-hall at shift change', 'port work', 'tavern at shift end'],
    },
    routinesNote: 'skeletons by band; Motive and Fable add need and temperament; Pulse fits each person to these clocks',
    tiers: { simulated: 'persons the generator makes (routine + need + temperament + memory): the set near the player plus every person a plan or mission touches', background: 'walk, run and cheer only; tops the street up to the density target', maxCombatants: 30, src: 'GDD §06, §09; split and budget are Keel (brief 005 A)' },
  },
};
// optional argv[2] = output path (the determinism test uses it); default is the committed contract
const out = process.argv[2] ?? new URL('../../contracts/world/harbour-district.life.json', import.meta.url).pathname;
await Bun.write(out, JSON.stringify(life, null, 2) + '\n');
console.log('wrote ' + out);
