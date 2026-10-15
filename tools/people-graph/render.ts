// Plain-text output for the command line: the Surrealist endpoint (never with credentials) and query answers as a table.
import { surrealUrl } from '../rag/config';

/** The Surrealist endpoint to print: the server's address without any credentials a URL might carry. */
export const endpointOf = (url: string) => { const u = new URL(url); u.username = ''; u.password = ''; return u.toString().replace(/\/$/, ''); };
export const endpoint = () => endpointOf(surrealUrl);

const table = (rows: Record<string, unknown>[]) => {
  const keys = [...new Set(rows.flatMap(r => Object.keys(r)))], cell = (v: unknown) => (v === null || v === undefined ? '' : typeof v === 'object' ? JSON.stringify(v) : String(v));
  const w = keys.map(k => Math.min(60, Math.max(k.length, ...rows.map(r => cell(r[k]).length))));
  const line = (c: string[]) => c.map((x, i) => (x.length > w[i]! ? `${x.slice(0, w[i]! - 1)}…` : x.padEnd(w[i]!))).join('  ').trimEnd();
  return [line(keys), line(w.map(n => '-'.repeat(n))), ...rows.map(r => line(keys.map(k => cell(r[k]))))].join('\n');
};
const rows = (x: unknown): x is Record<string, unknown>[] => Array.isArray(x) && x.length > 0 && x.every(r => r && typeof r === 'object' && !Array.isArray(r));
/** Rows as a table; an object as `key: value` lines with its rows as tables under their key; anything else as JSON. */
export function render(x: unknown, indent = ''): string {
  if (rows(x)) return table(x).split('\n').map(l => indent + l).join('\n');
  if (x && typeof x === 'object' && !Array.isArray(x)) {
    return Object.entries(x).map(([k, v]) => (rows(v) ? `${indent}${k}:\n${render(v, indent + '  ')}` : `${indent}${k}: ${typeof v === 'object' && v !== null ? JSON.stringify(v) : String(v ?? '')}`)).join('\n');
  }
  return JSON.stringify(x, null, 2) ?? 'null';
}
