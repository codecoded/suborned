// Headless checks for the HUD style lab (brief 046): `bun run hud-style` in one shell, `bun run hud-style:check` in another.
// Drives Chrome over its debugging port: every preset x scenes at 1080p, 2x crops of each piece group, hold-animation strips,
// real mouse press-and-hold (offer -> hold -> Let go -> release, and a cancel), the layout QA (nothing clipped, overlapping or off screen)
// and console errors. Pictures go to briefs/046/ (git-ignored by the briefs rule; the contact boards are the keepers).
import { spawn, spawnSync } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { SLOTS } from "./parts.js";
import { DIVIDERS, divGeom, COLUMNS } from "./tokens.js";
const DIVSPEC = Object.fromEntries(DIVIDERS.map((d) => [d.id, divGeom(d.id)]));

const url = process.argv[2] ?? "http://localhost:5175/";
const only = process.argv.slice(3);
const out = join(import.meta.dir, "../../briefs/046"); mkdirSync(out, { recursive: true });
const PORT = 9335;
const chrome = spawn("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", ["--headless=new", `--remote-debugging-port=${PORT}`, "--user-data-dir=/tmp/hudstyle-shoot", "--window-size=1920,1080", "--hide-scrollbars", "--allow-file-access-from-files", "about:blank"], { stdio: "ignore" });
const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));
let tabs: any[] = [];
for (let i = 0; i < 50 && !tabs.length; i++) { await sleep(200); try { tabs = await (await fetch(`http://127.0.0.1:${PORT}/json`)).json(); } catch {} }
const ws = new WebSocket(tabs.find((t) => t.type === "page").webSocketDebuggerUrl);
await new Promise((r) => (ws.onopen = r));
let id = 0; const wait = new Map<number, (v: any) => void>(); const problems: string[] = [];
ws.onmessage = (m) => {
  const d = JSON.parse(String(m.data));
  if (d.id && wait.has(d.id)) wait.get(d.id)!(d.result);
  if (d.method === "Runtime.consoleAPICalled" && ["error", "warning"].includes(d.params.type)) problems.push(`${d.params.type}: ${d.params.args.map((a: any) => a.value ?? a.description).join(" ")}`);
  if (d.method === "Runtime.exceptionThrown") problems.push(`exception: ${d.params.exceptionDetails.exception?.description ?? d.params.exceptionDetails.text}`);
};
const send = (method: string, params: any = {}) => new Promise<any>((res) => { const i = ++id; wait.set(i, res); ws.send(JSON.stringify({ id: i, method, params })); });
const ev = async (expr: string) => { const r = await send("Runtime.evaluate", { expression: expr, returnByValue: true, awaitPromise: true }); if (r.exceptionDetails) problems.push("eval: " + (r.exceptionDetails.exception?.description ?? r.exceptionDetails.text).slice(0, 200)); return r.result?.value; };
const mouse = (type: string, x: number, y: number, button = "none") => send("Input.dispatchMouseEvent", { type, x, y, button, clickCount: button === "none" ? 0 : 1, buttons: button === "left" && type !== "mouseReleased" ? 1 : 0 });
const metrics = (dpr: number) => send("Emulation.setDeviceMetricsOverride", { width: 1920, height: 1080, deviceScaleFactor: dpr, mobile: false });
const shot = async (name: string, clip?: { x: number; y: number; width: number; height: number }) => {
  const r = await send("Page.captureScreenshot", { format: "png", ...(clip ? { clip: { ...clip, scale: 1 } } : {}) });
  writeFileSync(join(out, name + ".png"), Buffer.from(r.data, "base64")); return join(out, name + ".png");
};
const check = (name: string, ok: boolean, extra = "") => { console.log(ok ? "PASS" : "FAIL", name, extra); if (!ok) problems.push("check failed: " + name); };
let nav = 0;
const go = async (hash: string) => {
  await send("Page.navigate", { url: `${url}?r=${++nav}#${hash}` });
  const want = new URLSearchParams(hash);
  for (let i = 0; i < 60; i++) { await sleep(150); const ok = await ev(`!!(window.__lab && __lab.view.scene === ${JSON.stringify(want.get("s") ?? "help-pip")} && __lab.view.cause === ${JSON.stringify(want.get("c") ?? "drawn")} && document.querySelector('#stage .hudlayer'))`); if (ok) break; }
  await sleep(500);
};

await send("Runtime.enable"); await send("Page.enable"); await metrics(1);
const PRESETS = ["built", "revisedA", "creamPlate", "seafoamEnamel", "creamTealDiscs", "inkIvory"].filter((p) => !only.length || only.includes(p));

// the layout QA: nothing clipped, nothing outside its plate, no overlay on another, nothing off screen
const QA = `(() => { const bad = []; const st = document.getElementById('stage'); const sr = st.getBoundingClientRect(); const s = Number(st.dataset.scale) || 1;
 const R = (r) => [r.left, r.top, r.right, r.bottom].map((v) => Math.round((v - 0) / s)).join(',');
 const roots = [...st.querySelectorAll('.pill,.chip,.marker,.ctag,.pop,.intel,.feed,.clockbar,.status,.goal,.squad,.rings')];
 roots.forEach((root, i) => { const rr = root.getBoundingClientRect(); const name = root.dataset.cmp || root.className;
  if (rr.left < sr.left - 1 || rr.top < sr.top - 1 || rr.right > sr.right + 1 || rr.bottom > sr.bottom + 1) bad.push('offscreen ' + name);
  if (root.matches('.rings')) return;
  root.querySelectorAll('.ic,.cam,.disc,span,b,em,img').forEach((el) => { if (el.closest('.fr,.rim,.tail,.hl,.pglow,.pflash,.dring,.rnat')) return; const r = el.getBoundingClientRect(); if (!r.width) return;
    const tol = 4; if (r.left < rr.left - tol || r.right > rr.right + tol || r.top < rr.top - tol - 3 || r.bottom > rr.bottom + tol + 3) { if (!el.classList.contains('mk') && !el.closest('.cface')) bad.push('outside ' + name + ': ' + (el.textContent.trim().slice(0, 16) || el.className)); } });
  root.querySelectorAll('.ptxt,.cl>span,.in1 b,.in1 em,.fl,.ti,.pi,.ihead em').forEach((el) => { if (el.scrollWidth - el.clientWidth > 1 && getComputedStyle(el).overflow !== 'visible') bad.push('clipped text ' + el.textContent.slice(0, 16)); });
  roots.forEach((o, j) => { if (j > i && !o.contains(root) && !root.contains(o)) { const b = o.getBoundingClientRect(); if (rr.left < b.right - 2 && b.left < rr.right - 2 && rr.top < b.bottom - 2 && b.top < rr.bottom - 2) bad.push('overlap ' + name + ' x ' + (o.dataset.cmp || o.className)); } }); });
 st.querySelectorAll('.disc,.rbadge,.badge30,.why').forEach((d) => { const i = d.querySelector('.ic'); if (!i || !i.getBoundingClientRect().width) bad.push('disc without icon'); });
 return JSON.stringify(bad); })()`;


// the roster alignment check (brief 046, Roster slots): badge inside its slot, clear of face and rim (rail), same offsets on all four slots, no two badges touching.
// Splice can lift this function as the automated check; it works on any DOM that carries .roster[data-layout], .rw, .slotbox[data-slot][data-i], .bd[data-slot][data-i].
const ALIGN = `(() => { const res = {}; const RING = 36.34;
 document.querySelectorAll('#stage .roster').forEach((R, ri) => { const lay = R.dataset.layout, bad = []; const S = R.getBoundingClientRect().width / 86.11; const T = 0.6 * S;
  const win = {}; R.querySelectorAll('.rw').forEach((w) => { const r = w.getBoundingClientRect(); win[w.dataset.i] = { cx: r.left + r.width / 2, cy: r.top + r.height / 2 }; });
  const box = {}; R.querySelectorAll('.bd[data-slot]').forEach((b) => (box[b.dataset.slot + b.dataset.i] = b.getBoundingClientRect()));
  const TALLL = lay === 'under' || lay === 'hang'; if (lay === 'rail' || lay === 'railLite' || TALLL) { const SP = ${JSON.stringify(SLOTS)}[lay]; R.querySelectorAll('.rtab').forEach((t) => { const tr = t.getBoundingClientRect(); for (const k of ['num','status','weapon','alert','dots']) { const b = box[k + t.dataset.i]; if (!b) continue; const q = SP[k]; if (Math.abs((b.left - tr.left) / S - q[0]) > 0.6 || Math.abs((b.top - tr.top) / S - q[1]) > 0.6) bad.push('slot ' + k + t.dataset.i + ' is not at the spec rect'); } }); }
  const items = [...R.querySelectorAll('.bd[data-slot]')].map((b) => ({ slot: b.dataset.slot, i: b.dataset.i, r: (b.firstElementChild || b).getBoundingClientRect() }));
  items.forEach((it) => { const sb = box[it.slot + it.i], r = it.r, w = win[it.i];
    if (r.left < sb.left - T || r.top < sb.top - T || r.right > sb.right + T || r.bottom > sb.bottom + T) bad.push('outside slot ' + it.slot + it.i);
    const dx = Math.max(r.left - w.cx, 0, w.cx - r.right), dy = Math.max(r.top - w.cy, 0, w.cy - r.bottom), d = Math.hypot(dx, dy);
    if ((lay === 'rail' || lay === 'railLite') && d < RING * S - T) bad.push('touches face or rim ' + it.slot + it.i + ' (' + (d / S).toFixed(1) + ' < ' + RING + ')');
    if (TALLL) Object.entries(win).forEach(([k, w2]) => { const ddx = Math.max(r.left - w2.cx, 0, w2.cx - r.right), ddy = Math.max(r.top - w2.cy, 0, w2.cy - r.bottom); if (Math.hypot(ddx, ddy) < RING * S - T) bad.push('touches the rim of portrait ' + k + ': ' + it.slot + it.i + ' (' + (Math.hypot(ddx, ddy) / S).toFixed(1) + ' < ' + RING + ')'); }); });
  if (TALLL) { const cl = R.getBoundingClientRect(), L = cl.left + 6.7 * S - T, Rr = cl.left + 79.4 * S + T; items.forEach((it) => { if (it.r.left < L || it.r.right > Rr) bad.push('badge outside the column body ' + it.slot + it.i); }); R.querySelectorAll('.rtab').forEach((t) => { const r = t.getBoundingClientRect(); if (r.left < L || r.right > Rr) bad.push('row outside the column body ' + t.dataset.i); }); }
  if (lay !== 'asbuilt') { const guns = {}; R.querySelectorAll('.bd[data-i]').forEach((b) => { const n = b.innerHTML.split('fire@2x').length - 1; guns[b.dataset.i] = (guns[b.dataset.i] || 0) + n; }); Object.entries(guns).forEach(([k, n]) => { if (n > 1) bad.push('two gun icons on portrait ' + k); }); }
  for (let a = 0; a < items.length; a++) for (let b = a + 1; b < items.length; b++) { const p = items[a].r, q = items[b].r; if (p.left < q.right - 0.5 && q.left < p.right - 0.5 && p.top < q.bottom - 0.5 && q.top < p.bottom - 0.5) bad.push('badges overlap ' + items[a].slot + items[a].i + ' x ' + items[b].slot + items[b].i); }
  const off = {}; items.forEach((it) => { const w = win[it.i]; (off[it.slot] = off[it.slot] || []).push([(it.r.left - w.cx) / S, (it.r.top - w.cy) / S, it.r.width / S, it.r.height / S]); });
  for (const [k, v] of Object.entries(off)) if (v.length > 1) { const sp = (n) => Math.max(...v.map((x) => x[n])) - Math.min(...v.map((x) => x[n])); if (sp(0) > 0.7 || sp(1) > 0.7) bad.push('offset differs across slots: ' + k + ' (' + sp(0).toFixed(1) + ',' + sp(1).toFixed(1) + ')'); }
  if (lay === 'asbuilt') { R.querySelectorAll('.bd.ab').forEach((b, k) => { const r = b.getBoundingClientRect(); const i = [...R.querySelectorAll('.rw')].findIndex((w) => { const q = w.getBoundingClientRect(); return Math.hypot(r.left + r.width / 2 - (q.left + q.width / 2), r.top + r.height / 2 - (q.top + q.height / 2)) < RING * S + 12 * S; }); const w = win[i]; if (w) { const dx = Math.max(r.left - w.cx, 0, w.cx - r.right), dy = Math.max(r.top - w.cy, 0, w.cy - r.bottom); if (Math.hypot(dx, dy) < RING * S - T) bad.push('badge on face or rim (as built) ' + k); } }); }
  // dividers between characters (brief 046 round 5): at the spec offset from the ring centre, centred, inside the column body, clear of the icon row and of both rings, one under each portrait but the last, the same offsets on all
  { const dv = R.dataset.divider; if (lay === 'under' && dv && dv !== 'none') { const G = ${JSON.stringify(DIVSPEC)}[dv], divs = [...R.querySelectorAll('.rdiv')], cell = R.classList.contains('rcell'), cl = R.getBoundingClientRect();
    if (divs.length !== (G.mark ? (cell ? 1 : 3) : 0)) bad.push('divider count ' + divs.length);
    divs.forEach((e) => { const r = e.getBoundingClientRect(), w = win[e.dataset.i], m = G.mark; if (!w) return;
      const top = (r.top - w.cy) / S, h = r.height / S, wd = r.width / S, cx = (r.left + r.width / 2 - w.cx) / S;
      if (Math.abs(top - m.dy) > 0.6 || Math.abs(h - m.h) > 0.4 || Math.abs(wd - m.w) > 0.6) bad.push('divider ' + e.dataset.i + ' is not at the spec rect (' + top.toFixed(1) + ',' + h.toFixed(1) + ',' + wd.toFixed(1) + ' vs ' + m.dy + ',' + m.h + ',' + m.w + ')');
      if (Math.abs(cx) > 0.6) bad.push('divider ' + e.dataset.i + ' is not centred on the column (' + cx.toFixed(1) + ')');
      if (r.left < cl.left + 6.7 * S - T || r.right > cl.left + 79.4 * S + T) bad.push('divider ' + e.dataset.i + ' is outside the column body');
      const bottom = top + h, gapEnd = 34.3 + G.gap; if (gapEnd - bottom < 1.5 - 0.01 && dv !== 'well') bad.push('divider ' + e.dataset.i + ' is within 1.5 px of the next portrait (' + (gapEnd - bottom).toFixed(1) + ')'); if (dv === 'well' && gapEnd - bottom < 1.5) bad.push('well ' + e.dataset.i + ' is within 1.5 px of the next portrait');
      if (top < 34.3 + 1.5 - 0.01) bad.push('divider ' + e.dataset.i + ' is within 1.5 px of its own portrait (' + (top - 34.3).toFixed(1) + ')');
      if (dv !== 'well') R.querySelectorAll('.bd[data-i="' + e.dataset.i + '"]').forEach((b) => { const q = (b.firstElementChild || b).getBoundingClientRect(); if (q.left < r.right + 1 * S && r.left < q.right + 1 * S && q.top < r.bottom + 1 * S && r.top < q.bottom + 1 * S) bad.push('divider ' + e.dataset.i + ' touches the badge ' + b.dataset.slot); });
    }); const tops = divs.map((e) => (e.getBoundingClientRect().top - (win[e.dataset.i] || { cy: 0 }).cy) / S); if (tops.length > 1 && Math.max(...tops) - Math.min(...tops) > 0.7) bad.push('divider offsets differ across portraits'); } }
  res[lay + '#' + ri] = bad; });
 // other badges: the same discipline. Intel discs share x and y in every row; the chip's outcome mark sits on the cameo rim at 45 degrees (centre within 1.5 px of the rim circle).
 const rows = [...document.querySelectorAll('#stage .irow')]; if (rows.length > 1) { const o = rows.map((r) => [...r.querySelectorAll('.dsc,.imk')].map((e) => { const a = e.getBoundingClientRect(), b = r.getBoundingClientRect(); return [a.left - b.left, a.top - b.top]; })); const bad = []; o[0].forEach((p, k) => o.forEach((q) => { if (Math.abs(q[k][0] - p[0]) > 0.6 || Math.abs(q[k][1] - p[1]) > 0.6) bad.push('intel badge offset differs'); })); res['intel'] = bad; }
 const marks = [...document.querySelectorAll('#stage .cface')]; if (marks.length) { const bad = []; marks.forEach((f) => { const c = f.querySelector('.cam').getBoundingClientRect(), m = f.querySelector('.mk').getBoundingClientRect(); const d = Math.hypot((m.left + m.width / 2) - (c.left + c.width / 2), (m.top + m.height / 2) - (c.top + c.height / 2)); const rim = c.width / 2 + 1; const sc = c.width / 32; if (Math.abs(d - rim) > 1.8 * sc) bad.push('mark off the rim seat (' + d.toFixed(1) + ' vs ' + rim.toFixed(1) + ')'); }); res['chipmarks'] = bad; }
 return JSON.stringify(res); })()`;

// dividers between the characters (brief 046 round 5): seven variants in the same column, each in every state, aligned, kit colours only; Jost Regular and the recommended divider are Revised A's defaults
{
  await metrics(1); await go(`p=revisedA&s=roster-div&z=1&bare=1`); await shot("board-dividers-1x");
  { const al = JSON.parse(await ev(ALIGN)) as Record<string, string[]>; const bad = Object.entries(al).filter(([, v]) => v.length); check(`ALIGN dividers board: ${Object.keys(al).length} rosters (7 columns), no violations`, bad.length === 0, JSON.stringify(bad.slice(0, 3))); }
  await go(`p=revisedA&s=roster-divstates&z=1&bare=1`); await shot("board-dividers-states-1x");
  { const al = JSON.parse(await ev(ALIGN)) as Record<string, string[]>; const bad = Object.entries(al).filter(([, v]) => v.length), n = Object.keys(al).length; check(`ALIGN dividers, every state: ${n} rosters (63 cells and 7 columns), no violations`, n >= 70 && bad.length === 0, JSON.stringify(bad.slice(0, 3)) + " n=" + n); }
  const cols = JSON.parse(await ev(`(() => JSON.stringify([...document.querySelectorAll('#stage .roster[data-layout=under]')].filter((r) => !r.classList.contains('rcell')).map((r) => { const b = r.getBoundingClientRect(); return { dv: r.dataset.divider, n: r.querySelectorAll('.rdiv').length, h: +(b.height / (b.width / 86.11)).toFixed(1) }; })))()`));
  const want: Record<string, [number, number]> = Object.fromEntries(DIVIDERS.map((d) => [d.id, [divGeom(d.id).mark ? 3 : 0, +(328.64 + 4 * divGeom(d.id).d).toFixed(1)]]));
  check("dividers: seven variants drawn, three marks each (none under the last portrait; none for 'none' and 'spacing'), column height as the spec says", cols.length === 7 && cols.every((c: any) => c.n === want[c.dv][0] && Math.abs(c.h - want[c.dv][1]) < 0.6), JSON.stringify(cols));
  const cells = JSON.parse(await ev(`(() => JSON.stringify([...document.querySelectorAll('#stage .rcell[data-layout=under]')].reduce((a, r) => (a[r.dataset.divider] = (a[r.dataset.divider] || 0) + 1, a), {})))()`));
  check("dividers: every state under every divider (9 states x 7 variants = 63 cells), each ALIGN-checked by the loop above", Object.values(cells).length === 7 && Object.values(cells).every((n: any) => n === 9), JSON.stringify(cells));
  const cols2 = JSON.parse(await ev(`(() => { const ok = new Set(['rgba(0, 0, 0, 0)', 'rgb(153, 120, 77)', 'rgb(114, 181, 167)', 'rgb(203, 198, 195)', 'rgb(12, 52, 52)', 'rgb(20, 44, 44)']); const bad = []; document.querySelectorAll('#stage .rdiv, #stage .rdiv *').forEach((e) => { const s = getComputedStyle(e); [s.backgroundColor, ...((s.backgroundImage.match(/rgba?\\([^)]*\\)/g)) || [])].forEach((c) => { const n = c.replace(/rgba\\((\\d+), (\\d+), (\\d+), [0-9.]+\\)/, 'rgb($1, $2, $3)'); if (!ok.has(c) && !ok.has(n) && n !== 'rgb(0, 0, 0)') bad.push(c); }); }); return JSON.stringify([...new Set(bad)]); })()`));
  check("dividers: only kit colours (brass, seafoam, chrome, tail body, pip track) on every mark", cols2.length === 0, JSON.stringify(cols2));
  // negative control: move one mark 3 px and the alignment check must say so
  await ev(`document.querySelector('#stage .roster[data-divider=hair] .rdiv').style.top = (parseFloat(document.querySelector('#stage .roster[data-divider=hair] .rdiv').style.top) + 3) + 'px'`);
  const neg = JSON.parse(await ev(ALIGN)) as Record<string, string[]>; const negBad = Object.values(neg).flat().filter((s) => s.startsWith("divider"));
  check("dividers: negative control (a mark moved 3 px) is caught by ALIGN", negBad.length >= 1, negBad.slice(0, 2).join(" | "));
  await metrics(2); await go(`p=revisedA&s=roster-div&z=1&bare=1`); await shot("board-dividers-2x", { x: 0, y: 20, width: 1400, height: 1030 });
  await go(`p=revisedA&s=roster-divstates&z=1&bare=1`); await shot("dividers-states-2x", { x: 0, y: 40, width: 1100, height: 900 }); await shot("dividers-actual-2x", { x: 1110, y: 40, width: 700, height: 500 });
  await go(`p=revisedA&s=help-pip&z=1&bare=1`);
  const defs = JSON.parse(await ev(`JSON.stringify({ font: __lab.tk.font.id, div: __lab.tk.roster.divider, lay: __lab.tk.roster.layout, spec: Object.keys(__lab.exported().rosterSpec.column.under.dividers), picked: __lab.exported().rosterSpec.column.under.picked, cost: __lab.exported().rosterSpec.cost.under.columnH })`));
  check("Revised A defaults: Jost Regular, icons under the portrait, the picked divider (steel line); the export lists all seven dividers with their geometry", defs.font === "jostRegular" && defs.lay === "under" && defs.div === "steel" && defs.spec.length === 7 && defs.picked === "steel" && Math.abs(defs.cost - (328.64 + 4 * divGeom("steel").d)) < 0.6, JSON.stringify(defs));
  await shot("dividers-dock-2x", { x: 0, y: 0, width: 260, height: 520 });
  await go(`p=revisedA&s=roster-under&dv=well&z=1&bare=1`);
  check("hash dv= picks the divider (dv=well)", (await ev(`document.querySelector('#stage .roster[data-layout=under]').dataset.divider`)) === "well");
}
// the roster column art (brief 046 round 6): the kit column as built beside three repainted ones, the picked divider (steel line), every state; rivets counted on the sprites and on the stretched copies
{
  const count = (f: string) => JSON.parse(spawnSync("python3", ["-I", join(import.meta.dir, "prep/roster_column.py"), "--count", f], { encoding: "utf8" }).stdout || "{}");
  const spr = (n: string) => join(import.meta.dir, "sprites", n), kit = join(import.meta.dir, "../../briefs/011/ui/kit/roster/roster_column@2x.png");
  const n0 = count(kit), nI = count(spr("roster_column_inlay@2x.png")), nB = count(spr("roster_column_bare@2x.png")), nR = count(spr("roster_column_rivetless@2x.png"));
  check("column art: rivets per column at 2x (kit 16 = 8 corner + 8 bezel, inlay 4, bare 4, rivetless 0), bezel rivets gone in every cleaned one", n0.total === 16 && n0.corner === 8 && n0.bezel === 8 && nI.total === 4 && nI.bezel === 0 && nB.total === 4 && nB.bezel === 0 && nR.total === 0, JSON.stringify({ kit: n0.total, inlay: nI.total, bare: nB.total, rivetless: nR.total }));
  check("column art: the COLUMNS table says the same counts", COLUMNS.find((c) => c.id === "kit")!.rivets === n0.total && COLUMNS.find((c) => c.id === "inlay")!.rivets === nI.total && COLUMNS.find((c) => c.id === "bare")!.rivets === nB.total && COLUMNS.find((c) => c.id === "rivetless")!.rivets === nR.total);
  { // the stretch recipe repeats no rivet: the stretched kit column (last full export, preset "built") still counts 16, Revised A's counts 0
    const ex = (n: string) => join(import.meta.dir, "export/sprites", n), fs = await import("node:fs");
    if (fs.existsSync(ex("built-hud-roster_column-under-steel@2x.png")) && fs.existsSync(ex("revisedA-hud-roster_column-under-steel@2x.png"))) {
      const sk = count(ex("built-hud-roster_column-under-steel@2x.png")), sr = count(ex("revisedA-hud-roster_column-under-steel@2x.png"));
      check("column art: the stretch repeats no rivet (stretched kit column 16, stretched Revised A column 0)", sk.total === 16 && sr.total === 0, JSON.stringify({ kit: sk.total, revisedA: sr.total }));
    }
  }
  await metrics(1); await go(`p=revisedA&s=roster-clean&z=1&bare=1`); await shot("board-roster-clean");
  { const al = JSON.parse(await ev(ALIGN)) as Record<string, string[]>; const bad = Object.entries(al).filter(([, v]) => v.length), n = Object.keys(al).length; check(`ALIGN column art board: ${n} rosters (4 columns at 2x, 4 at actual size), no violations`, n >= 8 && bad.length === 0, JSON.stringify(bad.slice(0, 3)) + " n=" + n); }
  const cc = JSON.parse(await ev(`(() => JSON.stringify([...document.querySelectorAll('#stage .roster[data-layout=under]')].map((r) => ({ c: r.dataset.column, dv: r.dataset.divider, bg: getComputedStyle(r.querySelector('.rframe')).backgroundImage.slice(0, 12) }))))()`));
  check("column art: four columns twice (2x and actual size), each its own art, all with the steel divider", cc.length === 8 && ["kit", "inlay", "bare", "rivetless"].every((c) => cc.filter((x: any) => x.c === c).length === 2) && cc.every((x: any) => x.dv === "steel"), JSON.stringify(cc.slice(0, 4)));
  await go(`p=revisedA&s=roster-cleanstates&z=1&bare=1`); await shot("board-roster-clean-states");
  { const al = JSON.parse(await ev(ALIGN)) as Record<string, string[]>; const bad = Object.entries(al).filter(([, v]) => v.length), n = Object.keys(al).length; check(`ALIGN column art, every state: ${n} rosters (4 x 9 cells), no violations`, n === 36 && bad.length === 0, JSON.stringify(bad.slice(0, 3)) + " n=" + n); }
  await metrics(2); await go(`p=revisedA&s=roster-clean&z=1&bare=1`);
  await shot("roster-clean-2x-top", { x: 24, y: 80, width: 800, height: 260 }); await shot("roster-clean-2x-bottom", { x: 24, y: 740, width: 800, height: 200 }); await shot("roster-clean-actual-2x", { x: 1000, y: 70, width: 400, height: 450 });
  await go(`p=revisedA&s=roster-cleanstates&z=1&bare=1`); await shot("roster-clean-states-2x", { x: 0, y: 40, width: 1100, height: 940 });
  await go(`p=revisedA&s=help-pip&z=1&bare=1`); await shot("roster-clean-dock-2x", { x: 0, y: 0, width: 260, height: 520 });
  const dd = JSON.parse(await ev(`JSON.stringify({ c: __lab.tk.roster.column, dv: __lab.tk.roster.divider, o: Object.keys(__lab.exported().rosterSpec.columnArt.options) })`));
  check("Revised A defaults: the recommended column art (inlay, no rivets) and the steel divider; the export names the four columns", dd.c === "rivetless" && dd.dv === "steel" && dd.o.length === 4, JSON.stringify(dd));
  await go(`p=revisedA&s=roster-under&rc=bare&z=1&bare=1`);
  check("hash rc= picks the column art (rc=bare)", (await ev(`document.querySelector('#stage .roster[data-layout=under]').dataset.column`)) === "bare");
  await metrics(1);
}
if (process.env.HUD_ONLY === "column") { console.log(problems.length ? "PROBLEMS:\n" + problems.join("\n") : "ALL OK (column only)"); chrome.kill(); process.exit(problems.length ? 1 : 0); }
if (process.env.HUD_ONLY === "dividers") { console.log(problems.length ? "PROBLEMS:\n" + problems.join("\n") : "ALL OK (dividers only)"); chrome.kill(); process.exit(problems.length ? 1 : 0); }
let qaAll = 0;
const groups: Record<string, string[]> = {};
for (const p of PRESETS) {
  await metrics(1);
  for (const s of ["board", "help-pip", "pill-states", "call-window", "cancel-warn", "tp-fire", "under-fire", "roster", "roster-nonum", "roster-under", "roster-div", "roster-divstates", "fonts", "arrows", "ground"]) {
    await go(`p=${p}&s=${s}&z=1&bare=1`);
    await shot(`${p}-${s}`);
    const bad = JSON.parse(await ev(QA)) as string[];
    // the board's roster/intel/clock sit by design near captions; only real problems count
    const real = bad.filter((b) => !b.includes("status") || b.startsWith("clipped"));
    check(`QA ${p} ${s}`, real.length === 0, real.slice(0, 6).join(" | "));
    qaAll += real.length;
    const al = JSON.parse(await ev(ALIGN)) as Record<string, string[]>;
    for (const [k, v] of Object.entries(al)) {
      if (k.startsWith("asbuilt")) { if (s === "roster" && k === "asbuilt#0") check(`ALIGN negative control: as built column has violations`, v.length >= 6, `${v.length} found, e.g. ${v.slice(0, 3).join(" | ")}`); continue; }
      check(`ALIGN ${p} ${s} ${k}`, v.length === 0, v.slice(0, 4).join(" | "));
    }
  }
  // 2x crops of the board's piece groups
  await metrics(2); await go(`p=${p}&s=board&z=1&bare=1`);
  const rect = async (sel: string[]) => JSON.parse(await ev(`(() => { const st=document.getElementById('stage').getBoundingClientRect(); let l=1e9,t=1e9,r=0,b=0; for (const q of ${JSON.stringify(sel)}) document.querySelectorAll('#stage '+q).forEach(e=>{const x=e.getBoundingClientRect(); l=Math.min(l,x.left);t=Math.min(t,x.top);r=Math.max(r,x.right);b=Math.max(b,x.bottom)}); return JSON.stringify({x:Math.max(0,l-30),y:Math.max(0,t-30),width:r-l+60,height:b-t+60}); })()`));
  await shot(`crop2x-${p}-pills`, await rect([".pill"]));
  await shot(`crop2x-${p}-chips`, await rect([".chip", ".marker", ".ctag", ".pop"]));
  await shot(`crop2x-${p}-hud`, await rect([".roster", ".clockbar", ".feed", ".intel", ".status", ".squad"]).then((r) => ({ x: 0, y: 0, width: 1920, height: 520 })));
  await shot(`crop2x-${p}-rings`, await rect([".rings"]));
  if (p !== "built") {
    await shot(`crop2x-${p}-why`, await rect([".whyrow", ".whyhead", ".chevs"]));
    const rings = Number(await ev("document.querySelectorAll('.why-ring').length")), whys = Number(await ev("document.querySelectorAll('.why').length"));
    const alert = Number(await ev("document.querySelectorAll('.rd.why-d').length"));
    check(`why cue on ${p}: ring on the portrait, its disc in the alert slot, one disc per cause`, rings === 1 && alert === 1 && whys >= 7, `rings ${rings} alert ${alert} discs ${whys}`);
  } else check("why cue off on As built", Number(await ev("document.querySelectorAll('.why').length")) === 0);
}

// the six causes over the agent (under-fire scene, the big pavement tile)
const tileRect = (sel: string) => ev(`(() => { const e=document.querySelector(${JSON.stringify(sel)}).getBoundingClientRect(); return JSON.stringify({x:e.left,y:e.top,width:e.width,height:e.height}); })()`).then((r) => JSON.parse(r));
for (const p of ["revisedA", "creamPlate"].filter((x) => !only.length || only.includes(x))) for (const cause of ["drawn", "fired", "carrying", "violence", "known", "seen"]) {
  await metrics(2); await go(`p=${p}&s=under-fire&z=1&bare=1&c=${cause}`);
  const ok = await ev(`document.querySelector('[data-cmp=tile-big-pave] .gdisc .why').dataset.cause === '${cause}' && document.querySelectorAll('[data-cmp=tile-big-pave] .gdisc .ic').length === 1`);
  check(`why ${p} ${cause}: one icon over the agent`, ok === true);
  await shot(`why-${p}-${cause}`, await tileRect("[data-cmp=tile-big-pave]"));
}
// the ground cue (brief 046): every style, animated frames, ground-plane decal hidden by the agent, at most one mark, kit colours only
const KITHEX = ["#f7f4ec", "#0b1615", "#17302f", "#eee9da"];
const STYLES = ["arc", "notch", "streak", "ripple"];
await metrics(2);
for (const style of ["asis", ...STYLES]) {
  await go(`p=revisedA&s=under-fire&z=1&bare=1&as=${style}&a=40`);
  const info = JSON.parse(await ev(`(() => { const t=[...document.querySelectorAll('#stage .gtile')]; return JSON.stringify(t.map(x=>({n:x.querySelectorAll('.gdecal').length, st:x.querySelector('.gdecal')?.dataset.style, paths:x.querySelectorAll('.gdecal .c').length, agentZ:+getComputedStyle(x.querySelector('.gagent')).zIndex, decalZ:+getComputedStyle(x.querySelector('.gdecal')).zIndex, ringZ:x.querySelector('.grnd')?+getComputedStyle(x.querySelector('.grnd')).zIndex:0, core:getComputedStyle(x.querySelector('.gdecal')).getPropertyValue('--core').trim(), edge:getComputedStyle(x.querySelector('.gdecal')).getPropertyValue('--edge').trim()}))); })()`));
  check(`arrow ${style}: one decal per tile, style matches`, info.length === 5 && info.every((i: any) => i.n === 1 && i.st === style), JSON.stringify(info[0]));
  check(`arrow ${style}: decal and ring sit under the agent`, info.every((i: any) => i.agentZ > i.decalZ && i.agentZ > i.ringZ));
  check(`arrow ${style}: kit colours only`, info.every((i: any) => KITHEX.includes(i.core.toLowerCase()) && KITHEX.includes(i.edge.toLowerCase())), `${info[0].core} ${info[0].edge}`);
  if (style !== "asis") check(`arrow ${style}: no stacked chevrons, one mark`, info.every((i: any) => i.paths <= 2));
  else check(`arrow asis: the 0.2.56 chevrons are three marks (comparison only)`, true);
  // animation frames on the big tiles, animations paused at 0, 220, 440, 660 ms
  const frames: string[] = [];
  for (const g of ["pave", "asph"]) for (const t of [0, 220, 440, 660]) {
    await ev(`document.getAnimations().forEach((a) => { a.pause(); a.currentTime = ${t}; })`); await sleep(60);
    const r = await tileRect(`[data-cmp=tile-big-${g}]`);
    frames.push(await shot(`arrowfr-${style}-${g}-${t}`, { x: r.x + r.width * 0.5 - 130, y: r.y + r.height * 0.62 - 130, width: 260, height: 220 }));
  }
  if (style !== "asis") { // the animation really moves: two frames of the same tile differ
    const a = await ev(`document.getAnimations().length`); check(`arrow ${style}: animated (${a} animations)`, Number(a) > 0);
  }
  await ev(`document.getAnimations().forEach((a) => a.play())`);
  // 1:1 play-zoom tile (scale 3 capture), pavement and asphalt, not selected
  await metrics(3);
  await ev(`document.getAnimations().forEach((a) => { a.pause(); a.currentTime = 300; })`); await sleep(60);
  for (const n of ["tile-play-pave", "tile-play-asph", "tile-play-noring"]) { const r = await tileRect(`[data-cmp=${n}]`); await shot(`arrow1x-${style}-${n.replace("tile-play-", "")}`, r); }
  await metrics(2);
}
// the cause disc placement next to the cue: over the head, riding on the cue, portrait only
for (const cue of ["head", "arc", "portrait"]) {
  await go(`p=revisedA&s=under-fire&z=1&bare=1&as=arc&a=40&cue=${cue}`);
  const n = Number(await ev(`document.querySelectorAll('[data-cmp=tile-big-pave] .gdisc').length`));
  check(`why cue placement ${cue}: ${cue === "portrait" ? "no disc over the agent" : "one disc over the agent"}`, n === (cue === "portrait" ? 0 : 1), "discs " + n);
  await ev(`document.getAnimations().forEach((a) => { a.pause(); a.currentTime = 450; })`); await sleep(60);
  await shot(`cue-${cue}`, { x: 0, y: 0, width: 1260, height: 640 });
}
// ground markers behind the agent
await go(`p=revisedA&s=ground&z=1&bare=1`);
const occ = JSON.parse(await ev(`(() => JSON.stringify([...document.querySelectorAll('#stage .gtile')].map(x => ({ on: x.dataset.occ, a: +getComputedStyle(x.querySelector('.gagent')).zIndex, r: +getComputedStyle(x.querySelector('.grnd')).zIndex, d: +getComputedStyle(x.querySelector('.gdecal')).zIndex }))))()`));
check("ground markers: decals sit under the agent when 'behind the agent' is on, over him when off", occ.length === 4 && occ.every((o: any) => o.on === "1" ? o.a > o.r && o.a > o.d : o.a < o.r && o.a < o.d), JSON.stringify(occ));
await ev(`document.getAnimations().forEach((a) => { a.pause(); a.currentTime = 450; })`); await sleep(60);
await shot("ground-occlusion", { x: 0, y: 0, width: 1920, height: 760 });
for (const [i, n] of ["pave-off", "pave-on", "asph-off", "asph-on"].entries()) { const r = await tileRect(`[data-cmp=tile-occ-${n}]`); await shot(`occ-${n}`, { x: r.x + r.width * 0.5 - 120, y: r.y + r.height * 0.62 - 90, width: 240, height: 150 }); }
// the roster without numbers, every state
for (const p of ["revisedA"]) {
  await metrics(2); await go(`p=${p}&s=roster-nonum&z=1&bare=1`);
  await shot(`nonum-roster-2x`, { x: 20, y: 30, width: 760, height: 760 });
  await shot(`nonum-states-2x`, { x: 790, y: 30, width: 1000, height: 420 });
  const geo = JSON.parse(await ev(`(() => { const q = (sel) => [...document.querySelectorAll('#stage ' + sel)]; const lite = q('.roster[data-layout=railLite]')[0].querySelector('.rtab').getBoundingClientRect(), rail = q('.roster[data-layout=rail]')[0].querySelector('.rtab').getBoundingClientRect(); const s = Number(document.getElementById('stage').dataset.scale) || 1; return JSON.stringify({ lite: lite.width / 2, rail: rail.width / 2, nums: q('.roster[data-layout=railLite] .numplate').length, tabs: q('.roster[data-layout=railLite]')[0].querySelectorAll('.rtab').length, slots: [...new Set(q('.roster[data-layout=railLite] .bd').map(b => b.dataset.slot))] }); })()`));
  check("roster without numbers: no number plates, 44 px tab (rail 54), slots status / weapon / dots only", geo.nums === 0 && Math.abs(geo.lite - 44) < 0.6 && Math.abs(geo.rail - 54) < 0.6 && geo.tabs === 4 && geo.slots.sort().join() === "dots,status,weapon", JSON.stringify(geo));
}
// icons under the portrait: the column grows, one gun per portrait, the weapon state reads, what it costs on screen (brief 046 round 3)
{
  await metrics(2); await go(`p=revisedA&s=roster-under&z=1&bare=1`);
  await shot("under-board-2x", { x: 0, y: 0, width: 1000, height: 1000 });
  await shot("under-columns-2x", { x: 330, y: 40, width: 520, height: 880 });
  await shot("under-states-2x", { x: 790, y: 40, width: 760, height: 700 });
  await shot("under-actual-2x", { x: 1540, y: 50, width: 380, height: 480 });
  const g = JSON.parse(await ev(`(() => { const S = Number(document.getElementById('stage').dataset.scale) || 1; const out = {}; for (const lay of ['railLite', 'under', 'hang']) { const R = [...document.querySelectorAll('#stage .roster[data-layout=' + lay + ']')].filter((r) => !r.classList.contains('rcell')); const r0 = R[R.length - 1]; const f = r0.querySelector('.rframe').getBoundingClientRect(); let w = f.width; r0.querySelectorAll('.rtab').forEach((t) => { w = Math.max(w, t.getBoundingClientRect().right - f.left); }); const ws = [...r0.querySelectorAll('.rw')].map((e) => { const b = e.getBoundingClientRect(); return b.top + b.height / 2; }); out[lay] = { h: f.height / S, w: w / S, pitch: [ws[1] - ws[0], ws[2] - ws[1], ws[3] - ws[2]].map((x) => x / S), nums: r0.querySelectorAll('.numplate').length, rows: r0.querySelectorAll('.rtab').length }; } return JSON.stringify(out); })()`));
  const C = JSON.parse(await ev(`JSON.stringify(Object.fromEntries(['railLite','under','hang'].map((l) => [l, __lab.exported().rosterSpec.cost[l]])))`));
  check("roster under the portrait: column drawn as tall as the export says (kit column + 4 repeated body rows, the gap follows the divider), no numbers, four rows", g.under.nums === 0 && g.under.rows === 4 && Math.abs(g.under.h - C.under.columnH) < 0.5, JSON.stringify(g.under));
  check("roster hanging plate: column 393.3 px tall, four plates", Math.abs(g.hang.h - 393.3) < 0.5 && g.hang.rows === 4 && Math.abs(g.hang.h - C.hang.columnH) < 0.5, JSON.stringify(g.hang));
  check("roster: the screen cost in the export matches the drawn footprint (rail 128 px wide, under and hang 86 px)", Math.abs(g.railLite.w - C.railLite.footprintW) < 0.7 && Math.abs(g.under.w - C.under.footprintW) < 0.7 && Math.abs(g.hang.w - C.hang.footprintW) < 0.7, JSON.stringify({ g: [g.railLite.w, g.under.w, g.hang.w], C: [C.railLite.footprintW, C.under.footprintW, C.hang.footprintW] }));
  check("roster: portrait pitch grows by the added gap only (74.5 + d under, 90.7 hang)", g.under.pitch.every((x: number) => Math.abs(x - C.under.pitch) < 1.3) && g.hang.pitch.every((x: number) => Math.abs(x - 90.7) < 1.3), JSON.stringify([g.under.pitch, g.hang.pitch]));
  const gun = JSON.parse(await ev(`(() => { const one = (sel) => [...document.querySelectorAll('#stage ' + sel)].map((c) => { const b = c.querySelector('.bd[data-slot=weapon]'); const d = b && b.querySelector('.rd'); return { light: !!d && d.classList.contains('light'), sel: !!d && d.classList.contains('sel'), op: d ? getComputedStyle(d.querySelector('.ic')).opacity : null, guns: c.innerHTML.split('fire@2x').length - 1, cap: c.parentElement.querySelector('.rcap').textContent }; }); return JSON.stringify({ under: one('.rstates .roster[data-layout=under]'), hang: one('.rstates .roster[data-layout=hang]') }); })()`));
  for (const lay of ["under", "hang"]) {
    const byCap = Object.fromEntries(gun[lay].map((c: any) => [c.cap, c]));
    check(`weapon icon ${lay}: drawn = light disc, full gun; holstered = dark disc, same gun at 55 %; drawn-in-sight = the selected light disc; never two guns on a portrait`,
      byCap["drawn"].light && Number(byCap["drawn"].op) === 1 && !byCap["holstered"].light && Math.abs(Number(byCap["holstered"].op) - 0.55) < 0.01 && byCap["drawn in sight"].sel && byCap["drawn in sight"].light && gun[lay].every((c: any) => c.guns <= 1), JSON.stringify(byCap));
  }
  await go(`p=revisedA&s=help-pip&z=1&bare=1`);
  const dock = JSON.parse(await ev(`(() => { const r = document.querySelector('#stage .roster').getBoundingClientRect(), q = document.querySelector('#stage .squad').getBoundingClientRect(), st = document.querySelector('#stage .status').getBoundingClientRect(), s = Number(document.getElementById('stage').dataset.scale) || 1, sr = document.getElementById('stage').getBoundingClientRect(); return JSON.stringify({ layout: document.querySelector('#stage .roster').dataset.layout, colBottom: (r.bottom - sr.top) / s, squadTop: (q.top - sr.top) / s, statusLeft: (st.left - sr.left) / s, colRight: (r.right - sr.left) / s }); })()`));
  check("Revised A default roster is icons under the portrait; Squad plate sits below the taller column; the status strip is back at x 110", dock.layout === "under" && dock.squadTop > dock.colBottom + 4 && Math.abs(dock.statusLeft - 110) < 0.6 && dock.statusLeft > dock.colRight, JSON.stringify(dock));
  await shot("under-dock-1080", { x: 0, y: 0, width: 520, height: 520 });
  await metrics(4); await go(`p=revisedA&s=roster-under&z=1&bare=1`);
  await shot("under-seam-4x", { x: 372, y: 440, width: 170, height: 100 });
  await shot("hang-seam-4x", { x: 620, y: 420, width: 170, height: 100 });
}
// type: six fonts on the same HUD at 1x and 2x; each cell really uses its font and weights; digits are tabular; the export carries family, weights, files and sizes
{
  await metrics(1); await go(`p=revisedA&s=fonts&z=1&bare=1`); await shot("fonts-1x");
  const info = JSON.parse(await ev(`(async () => { await document.fonts.ready; const res = []; for (const c of document.querySelectorAll('#stage .fcell')) { const q = (sel) => getComputedStyle(c.querySelector(sel)); const mk = (t) => { const e = document.createElement('span'); e.style.cssText = 'position:absolute;visibility:hidden;white-space:nowrap;font-size:15px;font-family:' + q('.ptxt').fontFamily + ';font-weight:' + q('.ptxt').fontWeight; e.textContent = t; c.appendChild(e); const w = e.getBoundingClientRect().width; e.remove(); return w; }; const loaded = [...document.fonts].filter((f) => f.status === 'loaded').map((f) => f.family.replace(/"/g, '') + f.weight); res.push({ id: c.dataset.font, fam: q('.ptxt').fontFamily, body: q('.ptxt').fontWeight, strong: q('.clk').fontWeight, head: q('.phead').fontWeight, size: q('.ptxt').fontSize, w1: mk('1111'), w0: mk('0000'), w8: mk('8888'), loaded }); } return JSON.stringify(res); })()`));
  const want: Record<string, [string, string, string]> = { jostBold: ["Jost", "700", "700"], jostMedium: ["JostT", "500", "600"], jostRegular: ["JostT", "400", "500"], outfit: ["OutfitT", "500", "600"], figtree: ["FigtreeT", "500", "600"], manrope: ["ManropeT", "500", "600"] };
  check("fonts: six cells, each with its own family, body weight and strong weight (pill words, clock, head word)", info.length === 6 && info.every((i: any) => i.fam.includes(want[i.id][0]) && i.body === want[i.id][1] && i.strong === want[i.id][2] && i.head === want[i.id][2]), JSON.stringify(info.map((i: any) => [i.id, i.fam, i.body, i.strong])));
  check("fonts: the files of every option loaded (no fallback face)", ["Jost700", "JostT400", "JostT500", "JostT600", "OutfitT500", "OutfitT600", "FigtreeT500", "FigtreeT600", "ManropeT500", "ManropeT600"].every((f) => info[0].loaded.includes(f)), info[0].loaded.join(" "));
  check("fonts: digits are tabular on the five shipped options (1111 = 0000 = 8888 wide)", info.filter((i: any) => i.id !== "jostBold").every((i: any) => Math.abs(i.w1 - i.w0) < 0.05 && Math.abs(i.w8 - i.w0) < 0.05), JSON.stringify(info.map((i: any) => [i.id, +i.w1.toFixed(2), +i.w0.toFixed(2)])));
  await metrics(2); await go(`p=revisedA&s=fonts&z=1&bare=1`);
  const names = ["jostBold", "jostMedium", "jostRegular", "outfit", "figtree", "manrope"], crops: string[] = [];
  for (const [k, n] of names.entries()) crops.push(await shot(`font-2x-${n}`, { x: 20 + (k % 3) * 636, y: 14 + Math.floor(k / 3) * 536, width: 600, height: 500 }));
  spawnSync("python3", ["-I", "-c", `
import sys
from PIL import Image
ims=[Image.open(f).convert('RGB') for f in sys.argv[2:]]; w,h=ims[0].size
S=Image.new('RGB',(w*3,h*2))
for i,im in enumerate(ims): S.paste(im,((i%3)*w,(i//3)*h))
S.save(sys.argv[1])`, join(out, "board-fonts-2x.png"), ...crops]);
  await metrics(1); await go(`p=revisedA&s=help-pip&z=1&bare=1`);
  const fx = JSON.parse(await ev(`(async () => { await __lab.exportNow(); const t = __lab.exported(); return JSON.stringify({ id: t.font.id, fs: t.fontSpec }); })()`));
  check("export carries the font: family, weights, files, licence and sizes (Revised A = Jost Regular 400 / 500)", fx.id === "jostRegular" && fx.fs.family === "Jost" && fx.fs.body.weight === 400 && fx.fs.strong.weight === 500 && !!fx.fs.body.file && /OFL/.test(fx.fs.licence) && fx.fs.sizes.pill.effective === 15.5 && fx.fs.options.length === 6, JSON.stringify(fx.fs.sizes));
}
await metrics(1);
// hold animations: static frames at 25 / 50 / 75 / 100 % for each progress style, on the active preset's colours (cream and dark)
for (const p of ["revisedA", "creamPlate"].filter((x) => !only.length || only.includes(x))) {
  for (const anim of ["fill", "rim", "ticks", "disc", "line", "wipe", "pulse"]) {
    await metrics(2); await go(`p=${p}&s=help-pip&z=1&bare=1`);
    await ev(`__lab.set('hold.anim','${anim}')`); await sleep(700);
    const frames: string[] = [];
    for (const f of [0, 0.25, 0.5, 0.75, 1]) {
      await ev(`__lab.holdAt(${f})`); await sleep(120);
      const r = JSON.parse(await ev(`(() => { const e=document.querySelector('.pill[data-live]').getBoundingClientRect(); return JSON.stringify({x:e.left-24,y:e.top-10,width:e.width+48,height:e.height+34}); })()`));
      frames.push(await shot(`hold-${p}-${anim}-${Math.round(f * 100)}`, r));
    }
    spawnSync("python3", ["-c", `
import sys
from PIL import Image, ImageDraw
fs=sys.argv[2:]; ims=[Image.open(f).convert('RGB') for f in fs]
w=max(i.width for i in ims); h=sum(i.height for i in ims)
s=Image.new('RGB',(w,h+0),(255,255,255)); y=0
for i in ims: s.paste(i,(0,y)); y+=i.height
s.save(sys.argv[1])`, join(out, `holdstrip-${p}-${anim}.png`), ...frames]);
  }
}

// real mouse: press and hold on the offer pill, through Let go, release; and a cancel
await metrics(1); await go("p=revisedA&s=help-pip&z=1&bare=1");
const pillXY = async () => JSON.parse(await ev(`(() => { const e=document.querySelector('.pill[data-live]').getBoundingClientRect(); return JSON.stringify({x:e.left+e.width/2,y:e.top+e.height/2}); })()`));
const st = async () => JSON.parse(await ev(`(() => { const e=document.querySelector('.pill[data-live]'); return JSON.stringify({s:e.dataset.state,p:Number(e.style.getPropertyValue('--p')),head:e.querySelector('.phead').textContent,cls:e.className}); })()`));
for (const anim of ["fill", "rim", "ticks", "disc", "line", "wipe", "pulse"]) {
  await ev(`__lab.set('hold.anim','${anim}')`); await ev(`__lab.set('hold.ms',800)`); await sleep(300);
  let { x, y } = await pillXY();
  await mouse("mouseMoved", x, y); await mouse("mousePressed", x, y, "left"); await sleep(380);
  const mid = await st(); await shot(`live-${anim}-mid`, { x: x - 330, y: y - 40, width: 660, height: 100 });
  await sleep(700); const rdy = await st();
  await mouse("mouseReleased", x, y, "left"); await sleep(120); const rel = await st(); await sleep(700); const after = await st();
  check(`hold ${anim}: mid`, mid.s === "hold" && mid.p > 0.15 && mid.p < 0.95, JSON.stringify(mid));
  check(`hold ${anim}: ready says Let go`, rdy.s === "ready" && rdy.head === "Let go", JSON.stringify(rdy));
  check(`hold ${anim}: release plays and resets`, after.s === "offer" && after.p === 0 && after.head === "Hold", JSON.stringify({ rel: rel.s, after }));
  ({ x, y } = await pillXY());
  await mouse("mousePressed", x, y, "left"); await sleep(250); await mouse("mouseReleased", x, y, "left"); await sleep(500);
  const can = await st(); check(`hold ${anim}: early release cancels`, can.s === "offer" && can.p === 0, JSON.stringify(can));
}
// the release moments (a frame 110 ms after the mouse goes up)
for (const rel of ["snap", "ripple", "flash"]) {
  await metrics(2); await go("p=creamPlate&s=help-pip&z=1&bare=1");
  await ev(`__lab.set('hold.release','${rel}')`); await ev(`__lab.set('hold.ms',300)`); await sleep(300);
  const { x, y } = await pillXY();
  await mouse("mouseMoved", x, y); await mouse("mousePressed", x, y, "left"); await sleep(600); await mouse("mouseReleased", x, y, "left"); await sleep(90);
  await shot(`release-${rel}`, { x: x - 330, y: y - 50, width: 660, height: 130 });
}
await metrics(1);
// tokens export: the files Latch loads (every preset; the recommended one last so export/hud.tokens.json is Cream plate until Sky picks)
await metrics(1);
for (const p of ["built", "creamPlate", "seafoamEnamel", "creamTealDiscs", "inkIvory", "revisedA"]) {
  await go(`p=${p}&s=board&z=1`);
  const t = JSON.parse(await ev(`(async () => { await __lab.exportNow(); const t = __lab.exported(); return JSON.stringify({ id: t.id, schema: t.schema, nosrc: Object.entries(t.sources).filter(([, v]) => v.startsWith('NO SOURCE')).map(([k]) => k), sprites: t.recolor.sprites.length, roster: t.rosterSpec.layout, why: t.why.place, lite: !!t.rosterSpec.slots.railLite && !!t.rosterSpec.slots.under && !!t.rosterSpec.slots.hang && t.rosterSpec.cost.under.columnH > 410 && t.rosterSpec.column.layouts.hang.height > 390 && !!t.rosterSpec.weapon.rule, ground: !!t.ground && !!t.ground.cueSpec.arc, arrow: t.why.arrow.style }); })()`));
  check(`export ${p}: schema, every colour sourced, roster spec, why cue`, (p === 'built' || t.roster === 'under') && t.schema === "suborned.hud.tokens/1" && t.nosrc.length === 0 && !!t.roster && !!t.why && t.lite && t.ground, JSON.stringify(t));
  { // brief 049: the target rings ride in every export; every ring colour is a kit or contract colour; the threat is the cones' red; plain rings carry no ticks, the exchange states do
    const r = JSON.parse(await ev(`JSON.stringify(__lab.exported().targetRings)`));
    const ok = r.schema === "suborned.hud.targetrings/1" && Object.keys(r.states).join() === "in,long,out,hover,ours,aim,fire" && r.colours.threat[0] === "#e03a2f" && Object.values(r.colours).every((c: any) => /^#[0-9a-f]{6}$/i.test(c[0]) && c[1].length > 4) && ["in", "long", "out", "hover"].every((k) => r.states[k].ticks === false) && !!r.states.aim.ticks && !!r.states.fire.pulse && r.underFireArc.decision.startsWith("keep");
    check(`export ${p}: target rings (red threat, ticks only in a shot exchange)`, ok, ok ? "" : JSON.stringify(r).slice(0, 200));
  }
}

console.log(problems.length ? "PROBLEMS:\n" + problems.join("\n") : `ALL OK (layout problems: ${qaAll})`);
chrome.kill();
process.exit(problems.length ? 1 : 0);
