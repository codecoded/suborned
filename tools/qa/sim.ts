// Brief 060 (Suture): the engines a QA pass plays on ("lanes"), and the streaming player.
//   build   the built player's own engine: its Managed/Suborned.Sim.dll and its StreamingAssets/sim inputs and text (what Sky plays),
//           copied to out/qa/engine/build-<version>/ so a rebuild mid-pass changes nothing
//   tree    the working tree's engine and inputs (what the next build will play)
//   <rev>   a commit, pinned with git archive (tools/missions/check/engine.ts engineAt)
// Each lane builds tools/qa/host against that engine (out/qa/host/<lane>/) and plays a list of scripts on every core, one JSON line each,
// read as a stream (a pass never holds its plays on disk).
import { copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { createHash } from 'node:crypto';
import { ROOT, engineAt, engineHash, env } from '../missions/check/engine';
import type { Play } from './types';

export const BUILD_APP = join(ROOT, 'out/build/play/The Suborned.app');
export type Lane = { name: string; dll: string; cwd: string; inputsDir?: string; textFile: string; engine: string; label: string; inputs: string; build?: string };

const sha = (b: Buffer | string) => createHash('sha256').update(b).digest('hex');

/** Host dlls played from an earlier build because the engine did not compile now: dll → why. */
export const stale = new Map<string, string>();

function buildHost(key: string, props: string[]): string {
  const out = join(ROOT, 'out/qa/host', key), dll = join(out, 'suborned-qa.dll');
  const r = Bun.spawnSync(['dotnet', 'build', join(ROOT, 'tools/qa/host'), '-c', 'Release', '-o', out, '-v', 'q', '-nologo', `-p:QaKey=${key}`, ...props], { cwd: ROOT, env, stdout: 'pipe', stderr: 'pipe' });
  if (r.exitCode === 0 && existsSync(dll)) return dll;
  // the tree mid-edit (an engine owner's change that does not compile yet): play the last host that built, and say so
  const why = `${r.stdout}${r.stderr}`.split('\n').find(l => /error/.test(l))?.trim() ?? 'unknown';
  if (key === 'tree' && existsSync(dll)) { stale.set(dll, `${statSync(dll).mtime.toISOString()}; the tree does not compile now: ${why.slice(0, 160)}`); return dll; }
  throw new Error(`QA host build (${key}) failed: ${why}`);
}

function about(l: Omit<Lane, 'engine' | 'inputs'>): { engine: string; inputs: string } {
  const r = Bun.spawnSync(['dotnet', l.dll, '--about', ...(l.inputsDir ? ['--inputs-dir', l.inputsDir] : [])], { cwd: l.cwd, env, stdout: 'pipe', stderr: 'pipe' });
  if (r.exitCode !== 0) throw new Error(`QA host --about (${l.name}): ${r.stderr}`);
  return JSON.parse(r.stdout.toString());
}

export function lane(spec: string): Lane {
  if (spec === 'build') {
    const data = join(BUILD_APP, 'Contents/Resources/Data'), info = JSON.parse(readFileSync(join(BUILD_APP, '..', 'build.json'), 'utf8'));
    const lib = join(data, 'Managed/Suborned.Sim.dll'), key = `build-${info.version}-${sha(readFileSync(lib)).slice(0, 8)}`;
    const dir = join(ROOT, 'out/qa/engine', key), inputs = join(dir, 'inputs');
    if (!existsSync(join(dir, 'Suborned.Sim.dll'))) {
      mkdirSync(inputs, { recursive: true });
      copyFileSync(lib, join(dir, 'Suborned.Sim.dll'));
      for (const f of readdirSync(join(data, 'StreamingAssets/sim')).filter(f => f.endsWith('.json'))) copyFileSync(join(data, 'StreamingAssets/sim', f), join(inputs, f));
      writeFileSync(join(dir, 'build.json'), JSON.stringify(info, null, 1));
    }
    const dll = buildHost(key, [`-p:EngineDll=${join(dir, 'Suborned.Sim.dll')}`]);
    const l = { name: 'build', dll, cwd: ROOT, inputsDir: inputs, textFile: join(inputs, 'text.json'), label: `the built player ${info.version} (engine ${info.sim}, ${info.engine})`, build: String(info.version) };
    return { ...l, ...about(l) };
  }
  const e = engineAt(spec, 'commit'), key = spec === 'tree' ? 'tree' : `rev-${e.commit.slice(0, 12)}`;
  const dll = buildHost(key, [`-p:EngineDir=${e.dir}`]);
  const textFile = join(ROOT, 'contracts/missions/handover/text.json'); // a pinned engine carries no mission text: the tree's
  const l = { name: spec === 'tree' ? 'tree' : key, dll, cwd: e.dir, textFile, label: spec === 'tree' ? (stale.has(dll) ? `the working tree's last QA host that built (${stale.get(dll)})` : `the working tree (engine source ${engineHash().slice(0, 12)})`) : `commit ${e.commit.slice(0, 12)}` };
  return { ...l, ...about(l) };
}

/** Play the scripts in `listFile` on the lane; `onPlay` gets each play in list order as it arrives. Resolves with the host's summary line. */
export async function playStream(l: Lane, listFile: string, onPlay: (p: Play, i: number) => void, o: { jobs?: number; replay?: boolean; stuckMinS?: number } = {}): Promise<string> {
  const args = ['dotnet', l.dll, '--list', listFile, '--out', '-', '--stuck-min-s', String(o.stuckMinS ?? 5)];
  if (l.inputsDir) args.push('--inputs-dir', l.inputsDir);
  if (o.jobs) args.push('--jobs', String(o.jobs));
  if (o.replay === false) args.push('--no-replay');
  const proc = Bun.spawn(args, { cwd: l.cwd, env: { ...env, DOTNET_gcServer: '1', DOTNET_gcConcurrent: '0', DOTNET_TieredPGO: '0' }, stdout: 'pipe', stderr: 'pipe' });
  const errText = new Response(proc.stderr).text();
  const dec = new TextDecoder(); let buf = '', i = 0;
  for await (const chunk of proc.stdout as ReadableStream<Uint8Array>) {
    buf += dec.decode(chunk, { stream: true });
    let k: number;
    while ((k = buf.indexOf('\n')) >= 0) { const line = buf.slice(0, k); buf = buf.slice(k + 1); if (line.trim()) onPlay(JSON.parse(line) as Play, i++); }
  }
  if (buf.trim()) onPlay(JSON.parse(buf) as Play, i++);
  const code = await proc.exited, err = await errText;
  if (code !== 0) throw new Error(`QA host (${l.name}) exit ${code}: ${err.trim()}`);
  return err.trim();
}
