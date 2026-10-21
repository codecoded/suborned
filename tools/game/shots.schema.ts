// suborned.shots/1 (brief 034 item 3): the data file tools/game/shots.json that the player's one shot runner (ShotRunner.cs, `The Suborned -shots <resolved.json> <dir>`)
// works through, in order. It replaces the per-round scratch scripts (out/latch/gamecap*.cs, curve2.cs, crowdcap.cs, shots.cs): a round that wants a new picture
// adds a row here; nothing is recompiled.
//
//   name      the output name (kebab-case): <dir>/<name>.png, with .hud.png, .manifest.json and .parts.json when the HUD is on (what tools/hud-budget reads); a clip is the folder <dir>/<name>/f0000.png…
//   start     where the run begins: nothing (the mission's own start), `script` (a route script in contracts/missions/handover/routes, by name or path: its orders are given as a click gives them),
//             `checkpoint` (a fight-lab checkpoint, briefs/027/checkpoints: cp-loud, cp-safe, with `preset`), or `scenario` (an engine scenario id, exported as a script; it must start from the
//             mission's own start). Shots that share a start go on in one run, in file order, while their ticks do not go back; a shot whose tick is earlier restarts the run.
//   tick      the game tick the shot is taken at (10 per second; 0 = at the start). The clock is held there for a still.
//   view      overhead (play zoom, the camera of the game) or follow (behind `agent`, facing `face`); `focus` is what the overhead camera looks at (a view key such as op/scout, waitress, car/getaway,
//             camera/entrance, patrol, or `at`: engine cm [x, y]); `yaw` the quarter turn 0..3 (the Records Office front is 2); `zoom` the orthographic size (play zoom is 14).
//   pose      a free camera (world metres) instead of the game's: `pos` and `look`, or `view` + `offset` + `lookOffset` in the local frame of a view key; `fov`. It draws the world only, so hud is false.
//   briefing  true: tick 0 with the briefing card up (nothing starts the clock); otherwise the shot starts the clock and the card goes.
//   night     the look of the district: day or night (look only; the run is the same).
//   hud       true: the frame and its HUD layer, manifest and parts for the HUD budget, and the layout audit run on it; false: the world with no HUD, png only.
//   state     the budget state of a HUD shot: rest, active or alt (briefing and log are chosen by the game itself).
//   select / arm / pointer / intel   what the player has done before the shot: the selected agent, the verb armed (plate id), the cursor ([0..1, 0..1] of the screen), the Intel sheet open.
//   hover     a view key the pointer rests on (its head, the camera's lens, a car's door height), placed after the camera: what a hover shows (the chip, the marker)
//   mark      false: the hover marker's action disc is not drawn (a before shot for brief 038's marker); default true
//   size      [w, h] of the capture (default 1920 x 1080, the size the HUD budget and the layout audit are defined at; the player is set to it for the run). Give another only for a detail shot.
//   format    png or jpg. Default: png for a HUD shot (the budget reads the frame and its layer lossless), jpg (quality 90) for a world-only shot and for clip frames. A HUD shot is always png.
//   settle    seconds of real time to let the camera and the HUD ease before the frame (default 0.35).
//   perf      seconds of frame times taken with the clock running before the shot (mean, p99, GPU), recorded in shots.json.
//   clip      { seconds, fps }: instead of a still, frames at real time with the clock running (the game clock advances 1/fps per frame).
//   skip      true: kept in the file, not run.
// Unknown keys are rejected.
import { z } from 'zod';

export const OPS = ['scout', 'fixer', 'tech', 'enforcer'] as const;
const num = z.number().finite();
const vec3 = z.tuple([num, num, num]);

export const Shot = z.object({
  name: z.string().regex(/^[a-z0-9][a-z0-9-]*$/, 'kebab-case: lower case letters, digits and hyphens'),
  note: z.string().optional(),
  script: z.string().optional(), checkpoint: z.string().optional(), scenario: z.string().optional(),
  preset: z.enum(['today', 'lab']).optional(),
  tick: z.number().int().min(0).max(36000).optional(),
  view: z.enum(['overhead', 'follow']).optional(),
  agent: z.enum(OPS).optional(), face: z.string().optional(),
  focus: z.string().optional(), at: z.tuple([num, num]).optional(),
  yaw: z.number().int().min(0).max(3).optional(), zoom: z.number().min(3).max(60).optional(),
  pose: z.object({
    pos: vec3.optional(), look: vec3.optional(), view: z.string().optional(), offset: vec3.optional(), lookOffset: vec3.optional(),
    fov: z.number().min(10).max(120).optional(),
  }).strict().optional(),
  night: z.boolean().optional(), briefing: z.boolean().optional(),
  hud: z.boolean().optional(), state: z.enum(['rest', 'active', 'alt']).optional(),
  select: z.enum(OPS).optional(), arm: z.string().optional(), pointer: z.tuple([z.number().min(0).max(1), z.number().min(0).max(1)]).optional(), intel: z.boolean().optional(),
  hover: z.string().optional(), mark: z.boolean().optional(),
  size: z.tuple([z.number().int().min(320).max(7680), z.number().int().min(180).max(4320)]).optional(), format: z.enum(['png', 'jpg']).optional(),
  settle: z.number().min(0).max(10).optional(), perf: z.number().min(0).max(60).optional(),
  clip: z.object({ seconds: z.number().min(0.5).max(60), fps: z.number().int().min(5).max(60).optional() }).strict().optional(),
  skip: z.boolean().optional(),
}).strict();
export type Shot = z.infer<typeof Shot>;

export const Shots = z.object({
  schema: z.literal('suborned.shots/1'),
  note: z.string().optional(),
  /** applied to every shot that does not say otherwise */
  defaults: Shot.partial().omit({ name: true }).optional(),
  shots: z.array(Shot).min(1),
}).strict();
export type Shots = z.infer<typeof Shots>;
