// Motive 059 balance pass (2026-10-26): replay composed probe scripts (out/scratch/motive/059c/scripts/<file>) on the scratch engine with the
// environment's probe switches (PROBE_PLACES moves a point), one line per script: outcome, the Enforcer's hits taken and health left at Vale-in.
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
const HERE = '/Users/sky/Projects/suborned/out/scratch/motive/059c', TREE = join(HERE, 'tree');
const { engineAt, playAll } = await import(join(TREE, 'tools/missions/check/engine.ts'));
const e = engineAt('tree', 'tree');
const clock = (t: number) => `${Math.floor(t / 600)}:${((t % 600) / 10).toFixed(1).padStart(4, '0')}`;
const files = process.argv.slice(2);
// SHIFT=<ticks>: every fire order from FROM (tick) on comes that much later (a player slower to lock); the orders keep their sort
const shift = Number(process.env.SHIFT ?? 0), from = Number(process.env.FROM ?? 0);
const load = (f: string) => { const s = JSON.parse(readFileSync(join(HERE, 'scripts', f), 'utf8')); if (shift) { for (const o of s.orders) if (o.do === 'fire' && o.tick >= from) o.tick += shift; s.orders.sort((a: any, b: any) => a.tick - b.tick); } return s; };
const plays = await playAll(files.map(f => ({ name: f, script: load(f) })), e, 8, { replay: false });
plays.forEach((p: any, i: number) => {
  const first = (c: string) => { const l = p.log.find((x: any) => x.code === c); return l ? clock(l.tick) : '-'; };
  const eh = p.log.filter((l: any) => l.code === 'enforcer.hit.fight'); const vin = p.log.find((l: any) => l.code === 'board.in' && /Vale/.test(l.text));
  const hp = (ls: any[]) => ls.length ? ls[ls.length - 1].text.match(/(\d+) health left/)?.[1] : '6';
  const hpIn = vin ? hp(eh.filter((l: any) => l.tick <= vin.tick)) : '-';
  console.log(`${(process.env.TAG ?? '').padEnd(6)} ${files[i]!.padEnd(28)} ${p.outcome}${p.fail ? ' ' + p.fail : ''} at ${clock(p.tick)} T ${p.trail} A ${p.alert} St ${p.standing} | E hits ${eh.length} hp@in ${hpIn} | spring ${first('fight.ambush')} blocker.out ${first('blocker.out')} over ${first('fight.over')} vale-in ${vin ? clock(vin.tick) : '-'}`);
});
