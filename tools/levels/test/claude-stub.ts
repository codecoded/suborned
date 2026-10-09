#!/usr/bin/env bun
// Stand-in for the `claude` CLI (set CLAUDE_BIN to this file). Emits the stream-json a real headless call would: an init event
// listing only StructuredOutput, then a result carrying the fixture for the objective on stdin. The nth call of an objective
// gets fixtures/<objective>.<n>.json (or the highest one below n). "__CHUNK__" becomes the first passage id in the prompt.
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { schemaProblems } from './schema-lint';

// Reject what the real CLI's strict validator rejects (draft-07 ajv, strict): 2020-only keywords, dangling $ref.
const schemaArg = process.argv[process.argv.indexOf('--json-schema') + 1];
if (schemaArg) {
  const bad = schemaProblems(JSON.parse(schemaArg));
  if (bad.length) { console.error(`Error: --json-schema is not a valid JSON Schema: ${bad.join('; ')}`); process.exit(2); }
}
const prompt = await Bun.stdin.text(), objective = prompt.match(/^Objective: (\S+)/m)?.[1] ?? 'unknown';
const state = process.env.STUB_STATE ?? '/tmp', counter = join(state, `${objective}.count`);
const n = (existsSync(counter) ? Number(readFileSync(counter, 'utf8')) : 0) + 1;
writeFileSync(counter, String(n));
let pick = n; const dir = process.env.STUB_FIXTURES ?? join(import.meta.dir, 'fixtures');
while (pick > 0 && !existsSync(join(dir, `${objective}.${pick}.json`))) pick--;
if (!pick) { console.error(`no fixture for ${objective}`); process.exit(3); }
const chunk = prompt.match(/^\[([0-9a-f]{24})\]/m)?.[1] ?? '0'.repeat(24);
const output = JSON.parse(readFileSync(join(dir, `${objective}.${pick}.json`), 'utf8').replaceAll('__CHUNK__', chunk));
console.log(JSON.stringify({ type: 'system', subtype: 'init', tools: ['StructuredOutput'], mcp_servers: [], model: 'stub', claude_code_version: '0.0.0-stub' }));
console.log(JSON.stringify({ type: 'result', subtype: 'success', is_error: false, result: '', structured_output: output, total_cost_usd: 0.01, usage: { input_tokens: 1, output_tokens: 1 }, modelUsage: { stub: {} } }));
