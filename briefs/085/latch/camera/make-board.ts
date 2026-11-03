// Brief 085, the camera board: a self-contained page from the stills -cameraBoard wrote. One row a spot, one column a view. usage: bun briefs/085/latch/camera/make-board.ts <dir of stills and board.json>
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
const dir = process.argv[2] ?? 'briefs/085/latch/camera', out = join(import.meta.dir, 'board.html');
type Row = { spot: string; spotLabel: string; view: string; viewLabel: string; file: string; figurePx: number; triangles: number; camY: number };
const rows: Row[] = JSON.parse(readFileSync(join(dir, 'board.json'), 'utf8'));
const notes = existsNote(); function existsNote() { try { return readFileSync(join(import.meta.dir, 'notes.html'), 'utf8'); } catch { return ''; } }
const views = [...new Set(rows.map(r => r.view))], spots = [...new Set(rows.map(r => r.spot))];
const img = (r: Row) => { const jpg = join(dir, r.file.replace('.png', '.board.jpg')); Bun.spawnSync(['ffmpeg', '-y', '-v', 'error', '-i', join(dir, r.file), '-vf', 'scale=960:-1', '-q:v', '4', jpg]); return `data:image/jpeg;base64,${readFileSync(jpg).toString('base64')}`; };
let html = `<!doctype html><meta charset="utf-8"><title>Brief 085: the camera board</title><style>body{font:14px/1.4 -apple-system,Helvetica,sans-serif;background:#17302f;color:#f7f4ec;margin:24px}h1{font-size:20px}h2{font-size:16px;margin:28px 0 8px}.g{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}figure{margin:0}img{width:100%;display:block;border:1px solid #cbc6c3}figcaption{padding:4px 0 0;color:#d2e6e2}a{color:#60e0ea}.n{max-width:900px}</style><h1>The camera board: is it too high up?</h1><p class="n">Stills only; nothing in the game changed. The same paused moment (route A at 1:20), the real player's look with the HUD, 1080p, through the camera lab's values. Click a picture for the full size.</p>${notes}`;
for (const s of spots) {
  const sr = rows.filter(r => r.spot === s); html += `<h2>${sr[0]!.spotLabel}</h2><div class="g">`;
  for (const v of views) { const r = sr.find(x => x.view === v); if (!r) continue; html += `<figure><a href="${r.file}"><img src="${img(r)}"></a><figcaption><b>${r.viewLabel}</b><br>a figure ${Math.round(r.figurePx)} px · ${r.triangles >= 0 ? (r.triangles / 1e6).toFixed(2) + ' M triangles' : 'triangles not read'} · camera ${r.camY} m up</figcaption></figure>`; }
  html += '</div>';
}
writeFileSync(out, html); console.log(out, (html.length / 1e6).toFixed(1), 'MB');
