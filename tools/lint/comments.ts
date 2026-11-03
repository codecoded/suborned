// The comment lint (Redline, brief 064: "the bug class Latch found 13 times today: a trailing `//` that swallows a statement"). A C# or TypeScript line fails when
//   (a) code is swallowed: a trailing `//` comment that ends in a statement (`foo(); // note bar.x = 3;`, `... // note Icon(a, b);`, `... // note return x;`, `... // note if (x) {`): prose does not end in `;` or `{`;
//   (b) a whole-line comment parses as a statement (`// x.style.left = 4;`, `// Foo(bar);`): the statement was commented out by accident, or meant to run;
//   (c) a `/*` is never closed.
// Prose is fine: a comment that ends in a full stop, a bracket or a word is not flagged however many `);` it holds. A deliberate hit goes in tools/lint/comments.allow.json with its reason (file, the line's trimmed text, why).
// Run: `bun tools/lint/comments.ts [--json]`; `bun run test` runs it through tools/lint/comments.test.ts.
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';

export type Hit = { file: string; line: number; kind: 'swallowed' | 'statement' | 'unclosed'; text: string };
export const ROOT = resolve(import.meta.dir, '../..');
/** Where the code lives: the tools, the engine and the Unity project's own scripts. */
export const ROOTS = ['tools', 'sim', 'unity/Assets/Suborned'];
const SKIP = new Set(['node_modules', 'obj', 'bin', 'out', 'Library', 'Temp', 'Kit', 'People', '.git']);

export function filesOf(root = ROOT, roots = ROOTS): string[] {
  const out: string[] = [];
  const walk = (d: string) => { for (const n of readdirSync(d)) { if (SKIP.has(n)) continue; const p = join(d, n), st = statSync(p); if (st.isDirectory()) walk(p); else if (/\.(cs|ts)$/.test(n) && !n.endsWith('.d.ts')) out.push(p); } };
  for (const r of roots) { try { walk(join(root, r)); } catch { /* a root that does not exist */ } }
  return out.sort();
}

/** A statement a comment must not end in: a call, an assignment, a return / var / const / let / new, or an opened block. */
const STMT = [
  // an assignment
  /(^|[\s(,])[A-Za-z_][\w]*(\.[A-Za-z_][\w]*|\[[^\]]*\])*\s*[+\-*/|&]?=\s*[^=;][^;]*;\s*$/,
  // a call
  /(^|[\s(,{])[A-Za-z_][\w]*(\.[A-Za-z_][\w]*)*\([^;]*\)\s*;\s*$/,
  /\b(return|throw|var|const|let|new)\b[^;]*;\s*$/,
  // an opened block
  /\b(if|for|foreach|while|switch|else|try|catch|finally)\b.*\{\s*$/,
  // a closed block
  /(^|\s)\}\s*(else\b.*)?\{?\s*$/,
];
/** A whole-line comment that is one statement from its first word: `x.y = 3;`, `Foo(bar);`, `return x;`, `var a = b;`, `if (x) {`, `}`. Prose that happens to end in `;` starts with words and does not match. */
const LINE_STMT = [
  /^[A-Za-z_][\w]*(\.[A-Za-z_][\w]*|\[[^\]]*\])*\s*[+\-*/|&]?=\s*[^=].*;$/,
  /^[A-Za-z_][\w]*(\.[A-Za-z_][\w]*)*\(.*\)\s*;$/,
  /^(return|throw|var|const|let|new|using)\b[^.;]*;$/,
  /^(if|for|foreach|while|switch|else|try|catch|finally)\b\s*\(.*\)\s*\{?$/,
  /^\}\s*(else\b.*)?$/,
];
const stmtTail = (text: string) => { const t = text.trim(); return (t.endsWith(';') || t.endsWith('{') || t === '}') && STMT.some(r => r.test(t)); };

/** Splits a line at the first `//` that is not inside a string or char literal (C#: "..", @"..", '.'; TS: '..', "..", `..`); returns the code before it and the comment after, and whether a block comment is open at the end. */
function split(line: string, cs: boolean, inBlock: boolean): { code: string; comment?: string; block: boolean } {
  let i = 0, code = '';
  if (inBlock) { const e = line.indexOf('*/'); if (e < 0) return { code: '', block: true }; i = e + 2; }
  while (i < line.length) {
    const c = line[i]!, n = line[i + 1];
    if (c === '/' && n === '/') return { code, comment: line.slice(i + 2), block: false };
    if (c === '/' && n === '*') { const e = line.indexOf('*/', i + 2); if (e < 0) return { code, block: true }; i = e + 2; code += ' '; continue; }
    if (c === '"' || c === '\'' || (!cs && c === '`')) {
      const verbatim = cs && c === '"' && code.endsWith('@'); let j = i + 1;
      for (; j < line.length; j++) { if (line[j] === '\\' && !verbatim) { j++; continue; } if (verbatim && line[j] === '"' && line[j + 1] === '"') { j++; continue; } if (line[j] === c) break; }
      code += line.slice(i, j + 1); i = j + 1; continue;
    }
    if (!cs && c === '/' && n !== '/' && n !== '*' && /(^|[=(,:;!&|?{}>[]|\breturn|\bcase)\s*$/.test(code)) { // a regex literal: skip to its closing slash
      let j = i + 1, cls = false; for (; j < line.length; j++) { if (line[j] === '\\') { j++; continue; } if (line[j] === '[') cls = true; else if (line[j] === ']') cls = false; else if (line[j] === '/' && !cls) break; }
      if (j < line.length) { code += line.slice(i, j + 1); i = j + 1; continue; }
    }
    code += c; i++;
  }
  return { code, block: false };
}

export function scan(file: string, text: string): Hit[] {
  const cs = file.endsWith('.cs'), hits: Hit[] = [], lines = text.split('\n'); let block = false, blockAt = 0, tpl = false;
  lines.forEach((raw, k) => {
    const line = raw.replace(/\r$/, '');
    if (!cs && tpl) { const e = line.indexOf('`'); if (e < 0) return; tpl = false; /* the template's tail is code */ }
    const before = block, s = split(!cs && tpl ? '' : line, cs, block); block = s.block; if (block && !before) blockAt = k + 1;
    if (!cs) { const ticks = (s.code.match(/(?<!\\)`/g) ?? []).length; if (ticks % 2 === 1) tpl = true; }
    if (s.comment === undefined) return;
    const c = s.comment, trimmedCode = s.code.trim();
    if (cs && c.startsWith('/')) return; // /// documentation
    if (trimmedCode) { if (stmtTail(c)) hits.push({ file, line: k + 1, kind: 'swallowed', text: line.trim() }); }
    else if (LINE_STMT.some(r => r.test(c.trim()))) hits.push({ file, line: k + 1, kind: 'statement', text: line.trim() });
  });
  if (block) hits.push({ file, line: blockAt, kind: 'unclosed', text: (lines[blockAt - 1] ?? '').trim() });
  return hits;
}

export type Allow = { file: string; text: string; why: string }[];
export function allowed(root = ROOT): Allow { try { return JSON.parse(readFileSync(join(root, 'tools/lint/comments.allow.json'), 'utf8')).allow as Allow; } catch { return []; } }

export function lint(root = ROOT, roots = ROOTS): { hits: Hit[]; files: number; lines: number; allowed: number } {
  const allow = allowed(root), hits: Hit[] = []; let lines = 0, ok = 0;
  const files = filesOf(root, roots);
  for (const f of files) {
    const text = readFileSync(f, 'utf8'); lines += text.split('\n').length;
    for (const h of scan(relative(root, f), text)) { if (allow.some(a => a.file === h.file && a.text === h.text)) ok++; else hits.push(h); }
  }
  return { hits, files: files.length, lines, allowed: ok };
}

if (import.meta.main) {
  const r = lint();
  if (process.argv.includes('--json')) console.log(JSON.stringify(r, null, 2));
  else { for (const h of r.hits) console.log(`${h.file}:${h.line} ${h.kind}: ${h.text}`); console.log(`comment lint: ${r.files} files, ${r.lines} lines, ${r.hits.length} hits, ${r.allowed} allowed`); }
  process.exit(r.hits.length ? 1 : 0);
}
