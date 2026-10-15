// Preflight for the A04 pipeline: one PASS/WARN/FAIL line per check, a one-line fix per failure, exit 1 on any FAIL.
// usage: bun run doctor. No model calls.
import { embeddingModel, embeddingUrl, isLocal, surrealUrl } from './rag/config';
import { hash, loadCorpus } from './rag/corpus';
import { corpusDb, sql } from './rag/database';

export const TESTED_CLAUDE = '2.1.296', MIN_BUN = '1.4.0', PORTS = [18504, 18505];
export const CLAUDE_FLAGS = ['--json-schema', '--effort', '--tools', '--setting-sources', '--strict-mcp-config', '--system-prompt', '--output-format'];

/** Compares dotted versions numerically: <0, 0, >0. */
export const cmpVersion = (a: string, b: string) => {
  const p = (v: string) => v.split('.').map(n => parseInt(n, 10) || 0);
  const x = p(a), y = p(b);
  for (let i = 0; i < Math.max(x.length, y.length); i++) if ((x[i] ?? 0) !== (y[i] ?? 0)) return (x[i] ?? 0) - (y[i] ?? 0);
  return 0;
};
export const parseVersion = (s: string) => s.match(/\d+\.\d+(?:\.\d+)*/)?.[0] ?? null;
/** `docker ps --format '{{.Ports}}|{{.Label "com.docker.compose.project"}}'` lines -> who publishes the host port. */
export const portOwner = (psLines: string[], port: number): 'ours' | 'other' | 'none' => {
  const re = new RegExp(`(?:^|[ ,:])${port}->`);
  const hit = psLines.filter(l => re.test(l.split('|')[0] ?? ''));
  return !hit.length ? 'none' : hit.every(l => l.split('|')[1]?.trim() === 'suborned') ? 'ours' : 'other';
};
export const missingFlags = (help: string, flags = CLAUDE_FLAGS) => flags.filter(f => !help.includes(f));

type Level = 'PASS' | 'WARN' | 'FAIL';
const run = async (cmd: string[]) => {
  try {
    const p = Bun.spawn(cmd, { stdout: 'pipe', stderr: 'pipe', stdin: 'ignore' });
    const [out, err, code] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
    return { ok: code === 0, out: out + err };
  } catch { return { ok: false, out: '' }; }
};
const portFree = async (port: number) => { try { Bun.listen({ hostname: '127.0.0.1', port, socket: { data() {} } }).stop(true); return true; } catch { return false; } };
const get = async (url: string) => { try { const r = await fetch(url, { signal: AbortSignal.timeout(5000) }); return r.ok ? r : null; } catch { return null; } };

async function main() {
  let failed = false;
  const say = (level: Level, name: string, msg: string, fix?: string) => { failed ||= level === 'FAIL'; console.log(`${level.padEnd(4)}  ${name.padEnd(11)} ${msg}${fix && level !== 'PASS' ? `  -> fix: ${fix}` : ''}`); };

  const bun = Bun.version;
  cmpVersion(bun, MIN_BUN) >= 0 ? say('PASS', 'bun', bun) : say('FAIL', 'bun', `${bun} < ${MIN_BUN}`, 'bun upgrade');

  const remote = !isLocal(surrealUrl) || !isLocal(embeddingUrl);
  if (remote) say('PASS', 'stack', `remote (${surrealUrl}, ${embeddingUrl}); no local docker needed`);
  const info = remote ? { ok: false, out: '' } : await run(['docker', 'info']), compose = remote ? info : await run(['docker', 'compose', 'version']);
  const dockerUp = info.ok;
  if (!remote) {
    if (!dockerUp) say('FAIL', 'docker', info.out ? 'daemon not reachable' : 'docker CLI not found', 'install Docker Desktop (or Engine) and start it');
    else say('PASS', 'docker', 'daemon reachable');
    compose.ok ? say('PASS', 'compose', parseVersion(compose.out) ?? 'ok') : say('FAIL', 'compose', 'docker compose not available', 'install the Docker Compose v2 plugin');
  }

  const ps = dockerUp ? (await run(['docker', 'ps', '--format', '{{.Ports}}|{{.Label "com.docker.compose.project"}}'])).out.split('\n').filter(Boolean) : [];
  const owned: Record<number, boolean> = {};
  for (const port of remote ? [] : PORTS) {
    const free = await portFree(port), owner = portOwner(ps, port);
    owned[port] = owner === 'ours';
    if (free) say('PASS', `port ${port}`, 'free');
    else if (owner === 'ours') say('PASS', `port ${port}`, 'held by compose project suborned');
    else say('FAIL', `port ${port}`, owner === 'other' ? 'held by another container' : 'in use by another process', `stop whatever holds 127.0.0.1:${port}, then bun run up`);
  }

  const up = remote || PORTS.every(p => owned[p]);
  if (!up) say('WARN', 'stack', 'not up', 'run bun run up / bun run ingest');
  else {
    const restart = remote ? 'run bun run up on the serving machine; check Tailscale and .env' : 'bun run down && bun run up';
    const h = await (await get(`${embeddingUrl}/health`))?.json().catch(() => null) as { model?: string; dimensions?: number } | null;
    h?.model === embeddingModel && h.dimensions === 384 ? say('PASS', 'embeddings', `${h.model}, 384 dims`)
      : say('FAIL', 'embeddings', h ? `reports ${h.model} / ${h.dimensions}` : '/health unreachable', restart);
    try {
      await sql('RETURN 1;', false); say('PASS', 'surrealdb', 'ready');
      const ch = hash(JSON.stringify(await loadCorpus())), db = corpusDb(ch);
      const m = (await sql<{ corpusHash: string; embeddingModel: string }[]>('SELECT * FROM corpus:current;', db).catch(() => [[]]))[0]?.[0];
      if (m?.corpusHash !== ch) say('WARN', 'corpus', `this tree's corpus ${ch.slice(0, 12)} not indexed`, 'run bun run ingest');
      else if (m.embeddingModel !== embeddingModel) say('WARN', 'corpus', 'stale (embedding model changed)', 'run bun run ingest');
      else say('PASS', 'corpus', `indexed and current (${ch.slice(0, 12)}, database ${db})`);
      // the people graph (tools/people-graph): a derived view of the promoted cast; reported, never a failure
      try { const g = await (await import('./people-graph/status')).promotedGraphLine(); say(g.level, 'people', g.msg, g.fix); } catch (e) { say('WARN', 'people', String((e as Error).message).slice(0, 80), 'bun run people:graph'); }
    } catch (e) { say('FAIL', 'surrealdb', String((e as Error).message).slice(0, 80), restart); }
  }

  const bin = process.env.CLAUDE_BIN ?? 'claude', ver = await run([bin, '--version']), v = parseVersion(ver.out);
  if (!ver.ok || !v) say('FAIL', 'claude', `${bin} not found or no version`, 'install Claude Code (npm i -g @anthropic-ai/claude-code) or set CLAUDE_BIN');
  else {
    cmpVersion(v, TESTED_CLAUDE) >= 0 ? say('PASS', 'claude', v) : say('WARN', 'claude', `${v} < tested ${TESTED_CLAUDE}`, 'update Claude Code');
    const miss = missingFlags((await run([bin, '--help'])).out);
    miss.length ? say('FAIL', 'claude flags', `missing ${miss.join(' ')}`, 'update Claude Code') : say('PASS', 'claude flags', 'all pipeline flags present');
  }
  const m = process.env.CLAUDE_MODEL;
  console.log(`NOTE  models      cards ask for opus (Datum, Vantage, Keel) and sonnet (Pulse); ${m ? `CLAUDE_MODEL=${m} overrides all` : 'no Opus in your Claude Code plan? set CLAUDE_MODEL=sonnet'}`);
  process.exit(failed ? 1 : 0);
}
if (import.meta.main) await main();
