import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { resolveShots, fileResolver } from '/Users/sky/Projects/suborned/tools/game/shots.ts';
const ROOT = '/Users/sky/Projects/suborned';
const [inp, outDir, resolvedOut] = process.argv.slice(2);
const sc = (id: string) => {
  const dir = join(ROOT, 'out/scratch/glint/evening/scripts'); mkdirSync(dir, { recursive: true });
  const dll = join(ROOT, 'out/missions/engine/tree/bin/suborned-sim.dll');
  if (!existsSync(dll)) return undefined;
  const r = Bun.spawnSync(['dotnet', dll, 'script', id], { cwd: ROOT, stdout: 'pipe', stderr: 'pipe' });
  if (r.exitCode !== 0) return undefined;
  const p = join(dir, id + '.script.json'); writeFileSync(p, r.stdout.toString()); return p;
};
mkdirSync(outDir, { recursive: true });
writeFileSync(resolvedOut, JSON.stringify(resolveShots(JSON.parse(readFileSync(inp, 'utf8')), outDir, fileResolver(sc)), null, 1));
