// A hash of the code that decides a plan's fate (the plan checker and apply), recorded in run.json so replay can say "the checker changed" apart from "the record changed".
import { readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { hash } from '../rag/corpus';
import { root } from '../rag/config';

export async function toolsHash(): Promise<string> {
  const files = [...readdirSync(resolve(root, 'tools/plan')).filter(f => f.endsWith('.ts') && !f.endsWith('.test.ts')).sort().map(f => `tools/plan/${f}`), 'tools/levels/apply.ts'];
  return hash((await Promise.all(files.map(async f => `${f}\n${await Bun.file(resolve(root, f)).text()}`))).join('\n--\n'));
}
