// usage: bun tools/people-view/shots.ts <view.html> [--out out/people/view/shots] [--only name,name]
// Screenshots of each view of a built viewer page, by headless Google Chrome (the page is driven by its own url hash: #tab?p=<person>&t=<second of the day>&a=..&b=..),
// so nothing here opens a browser window. One png per view: overview, map (at the Handover's start), ties (a path between two people), roster (filtered), sheet, compare.
import { mkdirSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { root } from '../rag/config';

const CHROME = process.env.CHROME ?? '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const argv = process.argv.slice(2), file = argv.find(a => !a.startsWith('--') && argv[argv.indexOf(a) - 1] !== '--out' && argv[argv.indexOf(a) - 1] !== '--only');
if (!file) { console.error('usage: bun tools/people-view/shots.ts <view.html> [--out dir] [--only a,b]'); process.exit(2); }
if (!existsSync(CHROME)) { console.error(`no Chrome at ${CHROME} (set CHROME)`); process.exit(2); }
const opt = (k: string) => { const i = argv.indexOf(k); return i < 0 ? undefined : argv[i + 1]; };
const out = resolve(root, opt('--out') ?? 'out/people/view/shots'), only = opt('--only')?.split(',');
mkdirSync(out, { recursive: true });
const html = resolve(root, file);
const data = JSON.parse((await Bun.file(html).text()).match(/<script type="application\/json" id="data">([\s\S]*?)<\/script>/)![1]!) as { roles: { id: string; pinned?: string }[]; mission: { startMin: number }; compare?: unknown };
const inez = data.roles.find(r => r.id === 'waitress')?.pinned, guard = data.roles.find(r => r.id === 'guard')?.pinned, driver = data.roles.find(r => r.id === 'driver')?.pinned;
const t0 = Math.round(data.mission.startMin * 60) + 400; // 6:40 into the mission: the chime (5:30) has gone and the Office is letting out
const shots: { name: string; hash: string; w: number; h: number }[] = [
  { name: 'overview', hash: 'overview', w: 1440, h: 3400 },
  { name: 'map', hash: `map?t=${t0}`, w: 1440, h: 900 },
  { name: 'ties', hash: `graph?a=${guard ?? ''}&b=${driver ?? ''}&any=1`, w: 1440, h: 900 },
  { name: 'roster', hash: 'roster?q=server', w: 1440, h: 1000 },
  { name: 'sheet', hash: `roster?p=${inez ?? ''}`, w: 1440, h: 2300 },
  ...(data.compare ? [{ name: 'compare', hash: 'compare', w: 1440, h: 2200 }] : []),
].filter(s => !only || only.includes(s.name));
for (const s of shots) {
  const png = `${out}/${s.name}.png`;
  const p = Bun.spawnSync([CHROME, '--headless=new', '--disable-gpu', '--hide-scrollbars', `--window-size=${s.w},${s.h}`, '--virtual-time-budget=5000', `--screenshot=${png}`, `file://${html}#${s.hash}`], { stdout: 'ignore', stderr: 'ignore' });
  console.log(`${p.exitCode === 0 ? 'ok  ' : 'FAIL'} ${s.name} → ${png}`);
}
