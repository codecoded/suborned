// suborned.presentation/1: presentation-only city life for scenes that are not play (the flyby, Flyby074, and the free-fly camera), brief 075
// golden view. Written by `bun run life:presentation` (tools/life/presentation.ts, Pulse) to out/world/<plan id>.presentation.json from the
// world export, the plan, contracts/world/<plan id>.life.json and tools/figures/figures.json. Deterministic and seeded by id: the same inputs
// give the same bytes.
//
// NEVER SIM DATA. Nothing here is a person, a car or a tram the simulation knows: the sim's site builder (tools/sim/site.ts), the engine (sim/)
// and the mission tools never read this file (tools/life/presentation.test.ts fails if one names it). Play and the Handover keep their own crowd.
//
// Frame: the world export's: x east, y north, metres; Unity position = (x, z, y). Heights are metres above the carriageway datum (0); z is a
// hint (pavement 0.18, carriageway 0, island 0.3, sky rail 24): snap to the drawn ground if it differs.
// Heading: radians in the plan frame, 0 = +x (east), pi/2 = +y (north). Every model here fronts +Z, so Unity eulerY (deg) = 90 - heading * 180 / pi.
//
// Clock: one loop of `clock.periodSec` seconds (720 = the timetable's T1 180 / T2 240 / S1 120 headways all divide it). Sample at any t >= 0:
// tau = t mod periodSec. Everything repeats exactly each period, so a camera can start anywhere and run as long as it likes.
export type V2 = [number, number];

export type Presentation = {
  schema: 'suborned.presentation/1';
  notSim: string; // the rule above, in the file
  source: {
    generator: string;
    world: { file: string; sha256: string }; plan: { id: string; version: string; sha256: string };
    life: { file: string; sha256: string }; figures: { file: string; sha256: string };
  };
  frame: string; heading: string;
  clock: { periodSec: number; hour: number; band: string; sample: string };
  /** Model keys used below. `kit` = a contracts/world/kit.json asset id; `figure` = a tools/figures/figures.json id (the crowd LODs).
   *  `status` is the kit status at generation: only `adopted` pieces are copied into Unity by `bun run kit`; draw a `candidate` with its `fallback`
   *  (or skip it when fallback is null) until it is adopted. */
  models: Record<string, Model>;
  /** Shared closed paths (walker loops, aircar lanes): flat [x, y, z, x, y, z, ...], closed (the last point joins the first). `len` = total
   *  length including the closing segment; `cum` is not stored: build it once at load. */
  loops: Loop[];
  /** Shared timed routes (one per tram direction, one per car trip, one per sky rail direction): flat xy [x, y, ...] and t [s, ...] since the
   *  route's start, one t per point, non-decreasing (a repeated point with a later t is a dwell). Linear between points is exact to 0.1 m. */
  routes: Route[];
  /** Things that move along a route: active while 0 <= ((t - t0) mod periodSec) <= the route's last t. Hidden otherwise. */
  timed: Timed[];
  /** Things that move round a loop forever: s = (s0 + v * tau) mod len (v < 0 walks the loop backwards); offset `lat` metres to the left of
   *  the direction of travel (left of the walker, not of the loop). Every loop mover returns to s0 at tau = periodSec. */
  looping: Looping[];
  /** Still things: parked cars in the world's kerbside bays, people standing, sitting or waiting. */
  still: Still[];
  counts: Record<string, number | Record<string, number>>;
  laterForma: string[]; // models wanted and not used yet (over budget or missing)
};

export type Model = {
  kind: 'figure' | 'car' | 'van' | 'tram' | 'skyrail' | 'aircar' | 'scooter';
  kit?: string; figure?: string; status: 'adopted' | 'candidate' | 'figure';
  tris: number | number[]; size?: [number, number, number]; // kit size [across, up, along] m
  pivot?: string; pivotOffset?: V2; // the kit's; positions here are the pivot (for the Tram: on the track centreline)
  fallback: string | null; note?: string;
};
export type Loop = { id: string; kind: 'walk' | 'sky'; len: number; xyz: number[]; where: string };
export type Route = { id: string; kind: 'tram' | 'car' | 'skyrail' | 'aircar'; z: number; xy: number[]; t: number[]; stops?: RouteStop[]; len: number };
export type RouteStop = { stop: string; tArrive: number; tDepart: number };
/** `consist`: further cars coupled behind the lead (a sky rail train): each at `back` metres behind the lead's pivot measured along the route
 *  polyline (find the lead's arc length, step back), `flip` = turned 180 degrees (a rear-facing head car). */
export type Timed = { id: string; model: string; route: number; t0: number; consist?: { model: string; back: number; flip: boolean }[] };
export type Looping = { id: string; model: string; loop: number; s0: number; v: number; lat: number; clip: 'walk' | 'fly'; bob?: number };
export type Still = { id: string; model: string; at: V2; z: number; heading: number; clip: 'parked' | 'idle' | 'idle_alt' | 'talk' | 'phone' | 'look_around' | 'sit'; where: string };
