// Test helper: the files a piece of code imports, however far (relative value imports only: `import type` is erased at run time, so it is no code a verdict
// rests on). A hash of verdict code is tested against this, so a file the code reaches cannot be left out of the hash.
import { existsSync } from 'node:fs';
import { dirname, relative, resolve } from 'node:path';
import { root } from '../../rag/config';

const rel = `['"](\\.{1,2}\\/[^'"]+)['"]`;
const edges = new RegExp(`^(?:import|export)\\s(?!type\\s)[^;]*?from\\s*${rel}|import\\(\\s*${rel}`, 'gms');

/** The entry files and every file they import, as repo-relative paths. */
export async function reached(entries: string[]): Promise<Set<string>> {
  const todo = [...entries], seen = new Set<string>();
  while (todo.length) {
    const file = todo.pop()!;
    if (seen.has(file)) continue;
    seen.add(file);
    const text = await Bun.file(resolve(root, file)).text();
    for (const m of text.matchAll(edges)) {
      const base = resolve(root, dirname(file), m[1] ?? m[2]!);
      const next = [`${base}.ts`, `${base}/index.ts`, base].find(f => f.endsWith('.ts') && existsSync(f));
      if (next) todo.push(relative(root, next));
    }
  }
  return seen;
}
