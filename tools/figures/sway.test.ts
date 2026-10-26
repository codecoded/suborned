// Idles slid left and right (Sky on 0.2.122; brief 067, briefs/067/forma-sway.md). The figures play with applyRootMotion off, so a clip whose Root Transform
// Position (XZ) is not baked into the pose has its body's centre held still and the planted feet slide instead (Breathing_Idle shifted the hips 18.8 cm).
// The rule held here, in the generator (tools/figures/unitymeta.py bakes_xz + clip_meta, called by clips.py): every clip that stays in place bakes XZ;
// locomotion (`loco`) does not, nor do the one-shots whose body ends elsewhere (`travelsM`, measured in Blender). When a build or an install is on disk,
// the metas there are held to the same rule.
import { describe, expect, test } from 'bun:test';
import { existsSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

const root = resolve(import.meta.dir, '../..');
const fig = join(root, 'tools/figures');
type Clip = { name: string; src?: string; loop?: boolean; loco?: boolean; feet?: boolean; travelsM?: number };
const clips = (JSON.parse(readFileSync(join(fig, 'clips.json'), 'utf8')) as { clips: Clip[] }).clips;
const by = (n: string) => clips.find(c => c.name === n)!;
const stays = (c: Clip) => !c.loco && !c.travelsM;

/** the generator's own answer per clip: bakes_xz and the loopBlendPositionXZ line clip_meta writes (pure python, no Blender) */
const py = `
import importlib.util, json, sys
sp = importlib.util.spec_from_file_location('unitymeta', sys.argv[1]); um = importlib.util.module_from_spec(sp); sp.loader.exec_module(um)
out = {}
for c in json.load(open(sys.argv[2]))['clips']:
    b = um.bakes_xz(c); m = um.clip_meta('Clips/%s.fbx' % c['name'], c['name'], 'mixamo.com', 0, 10, c.get('loop', False), feet=c.get('feet', False), bake_xz=b)
    out[c['name']] = {'bake': b, 'xz': [l.strip() for l in m.splitlines() if 'loopBlendPositionXZ' in l]}
print(json.dumps(out))`;
const r = Bun.spawnSync(['python3', '-I', '-c', py, join(fig, 'unitymeta.py'), join(fig, 'clips.json')], { stdout: 'pipe', stderr: 'pipe' });
const gen = JSON.parse(r.stdout.toString() || '{}') as Record<string, { bake: boolean; xz: string[] }>;
const xzOf = (meta: string) => /loopBlendPositionXZ: (\d)/.exec(meta)?.[1];

describe('the clips that stay in place bake Root Transform Position (XZ); locomotion does not', () => {
  test('the generator runs', () => { expect(r.exitCode, r.stderr.toString()).toBe(0); expect(Object.keys(gen).length).toBe(clips.length); });

  test('every clip that stays in place writes loopBlendPositionXZ: 1; every walk and run writes 0', () => {
    for (const c of clips) {
      expect(gen[c.name]!.xz, c.name).toEqual([`loopBlendPositionXZ: ${stays(c) ? 1 : 0}`]);
      expect(gen[c.name]!.bake, c.name).toBe(stays(c));
    }
  });

  test('the ones Sky saw and the ones named in the brief bake; the walks and runs do not', () => {
    for (const n of ['idle', 'idle_alt', 'aim', 'aim_pistol', 'talk', 'serve', 'carry_shoulder_idle', 'look_around', 'crouch', 'phone', 'beckon', 'point', 'captor', 'restrained',
      'draw', 'shoot', 'hit', 'sit', 'sit_alt', 'sit_talk', 'stand_to_sit', 'sit_to_stand', 'lying', 'carried', 'carried_drape', 'carry', 'struggle', 'panic', 'melee', 'melee_whip', 'search', 'pickup'])
      expect(gen[n]?.bake, n).toBe(true);
    for (const n of ['walk', 'walk_f', 'walk_old', 'run', 'walk_armed', 'run_armed', 'crouch_walk', 'carry_shoulder', 'serve_walk', 'run_lean']) {
      expect(by(n).loco, n).toBe(true); expect(gen[n]!.bake, n).toBe(false);
    }
  });

  test('only a one-shot can be marked as travelling, with its measured travel (more than 0.25 m), and never a walk', () => {
    for (const c of clips.filter(c => c.travelsM !== undefined)) {
      expect(c.loop, c.name).toBeFalsy(); expect(c.loco, c.name).toBeFalsy(); expect(c.travelsM!, c.name).toBeGreaterThan(0.25);
    }
  });

  test('idle_alt is a calm idle, never Breathing_Idle (its 18.8 cm weight shift)', () => {
    expect(by('idle_alt').src).not.toContain('Breathing_Idle');
    expect(by('idle_alt').src).toBe('Anim/mixamo_library/Standing_Idle__c9c831cf.fbx');
  });

  test('clips.py writes each meta with the rule (bake_xz=um.bakes_xz(c))', () => {
    expect(readFileSync(join(fig, 'clips.py'), 'utf8')).toContain('bake_xz=um.bakes_xz(c)');
  });

  for (const [label, dir] of [['the build (out/people-models)', join(root, 'out/people-models/People/Clips')], ['the Unity install', join(root, 'unity/Assets/Suborned/People/Clips')]] as const) {
    test.skipIf(!existsSync(dir))(`the metas in ${label} follow the rule`, () => {
      const wrong: string[] = [];
      for (const c of clips) {
        const p = join(dir, `${c.name}.fbx.meta`); if (!existsSync(p)) { wrong.push(`${c.name}: missing`); continue; }
        if (xzOf(readFileSync(p, 'utf8')) !== (stays(c) ? '1' : '0')) wrong.push(c.name);
      }
      // the Unity install is Latch's to refresh: report it, fail only the build
      if (label.startsWith('the Unity') && wrong.length) { console.warn(`[sway] ${label} is not reinstalled yet (${wrong.length} clips): ${wrong.join(', ')}`); return; }
      expect(wrong).toEqual([]);
    });
  }
});
