// Headless checks for the engine 0.28 / 0.29 HUD pieces (brief 054, "HUD for 0.29"): `bun run hud-style` in one shell, `bun testbed/hud-style/check29.ts` in another.
// Per scene at 1x: nothing off screen or clipped, no two HUD pieces overlapping, every disc holds one centred icon, the HUD budget (union of every HUD piece, as a share of the 1080p frame) at most 15 %,
// only sourced colours on the world marks, console clean; a negative control (a disc moved 4 px off its centre) must be caught; the shadow rule's numbers (brief 055 §7.5) against the engine's examples.
// Pictures to briefs/054/hud/qa/.
import { spawn } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { shadowOf } from "./world29.js";

const url = process.argv[2] ?? "http://localhost:5175/";
const out = join(import.meta.dir, "../../briefs/054/hud/qa"); mkdirSync(out, { recursive: true });
const PORT = 9337;
const chrome = spawn("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", ["--headless=new", `--remote-debugging-port=${PORT}`, "--user-data-dir=/tmp/hudstyle-w29", "--window-size=1920,1080", "--hide-scrollbars", "about:blank"], { stdio: "ignore" });
const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));
let tabs: any[] = [];
for (let i = 0; i < 50 && !tabs.length; i++) { await sleep(200); try { tabs = await (await fetch(`http://127.0.0.1:${PORT}/json`)).json(); } catch {} }
const ws = new WebSocket(tabs.find((t) => t.type === "page").webSocketDebuggerUrl); await new Promise((r) => (ws.onopen = r));
let id = 0; const wait = new Map<number, (v: any) => void>(); const problems: string[] = [];
ws.onmessage = (m) => { const d = JSON.parse(String(m.data)); if (d.id && wait.has(d.id)) wait.get(d.id)!(d.result);
  if (d.method === "Runtime.consoleAPICalled" && ["error", "warning"].includes(d.params.type)) problems.push(`${d.params.type}: ${d.params.args.map((a: any) => a.value ?? a.description).join(" ")}`);
  if (d.method === "Runtime.exceptionThrown") problems.push(`exception: ${d.params.exceptionDetails.exception?.description ?? d.params.exceptionDetails.text}`); };
const send = (method: string, params: any = {}) => new Promise<any>((res) => { const i = ++id; wait.set(i, res); ws.send(JSON.stringify({ id: i, method, params })); });
const ev = async (expr: string) => { const r = await send("Runtime.evaluate", { expression: expr, returnByValue: true, awaitPromise: true }); if (r.exceptionDetails) problems.push("eval: " + (r.exceptionDetails.exception?.description ?? r.exceptionDetails.text).slice(0, 200)); return r.result?.value; };
const check = (name: string, ok: boolean, extra = "") => { console.log(ok ? "PASS" : "FAIL", name, extra); if (!ok) problems.push("check failed: " + name); };
let nav = 0;
const go = async (scene: string) => { await send("Page.navigate", { url: `${url}?r=${++nav}#p=revisedA&s=${scene}&z=1&bare=1` });
  for (let i = 0; i < 60; i++) { await sleep(150); if (await ev(`!!(window.__lab && __lab.view.scene === ${JSON.stringify(scene)} && document.querySelector('#stage .hudlayer'))`)) break; } await sleep(600); };
await send("Runtime.enable"); await send("Page.enable");
const metrics = (dpr: number) => send("Emulation.setDeviceMetricsOverride", { width: 1920, height: 1080, deviceScaleFactor: dpr, mobile: false });
const shot = async (name: string) => { const r = await send("Page.captureScreenshot", { format: "png" }); writeFileSync(join(out, name + ".png"), Buffer.from(r.data, "base64")); };
await metrics(1);

const SCENES = ["w29-quiet", "w29-cone-a2", "w29-takedown", "w29-guard-ring", "w29-alt", "w29-fan-today", "w29-fan-cut", "w29-mood", "w29-price", "w29-watch-on", "w29-watch-off"];
// HUD pieces = everything a player reads that is not a world decal: the hudlayer's children (roster, status, squad, clock, minimap, feed, goal ...), pills, the verb plate, the over-head discs
const QA = `(() => { const bad = [], st = document.getElementById('stage'), sr = st.getBoundingClientRect(), s = Number(st.dataset.scale) || 1;
 const pieces = [...st.querySelectorAll('.hudlayer > *, .at > .pill, .at > .vplate, .at .ohead')].filter((e) => e.getBoundingClientRect().width > 2 && !e.classList.contains('cap'));
 const name = (e) => e.dataset.cmp || e.className.toString().slice(0, 24);
 pieces.forEach((e) => { const r = e.getBoundingClientRect(); if (r.left < sr.left - 1 || r.top < sr.top - 1 || r.right > sr.right + 1 || r.bottom > sr.bottom + 1) bad.push('offscreen ' + name(e)); });
 for (let i = 0; i < pieces.length; i++) for (let j = i + 1; j < pieces.length; j++) { const a = pieces[i], b = pieces[j]; if (a.contains(b) || b.contains(a)) continue; const A = a.getBoundingClientRect(), B = b.getBoundingClientRect();
   if (A.left < B.right - 2 && B.left < A.right - 2 && A.top < B.bottom - 2 && B.top < A.bottom - 2) bad.push('overlap ' + name(a) + ' x ' + name(b)); }
 st.querySelectorAll('.pill .ptxt').forEach((el) => { if (el.scrollWidth - el.clientWidth > 1 && getComputedStyle(el).overflow !== 'visible') bad.push('clipped text ' + el.textContent.slice(0, 16)); });
 st.querySelectorAll('.pill,.vplate').forEach((p) => { const pr = p.getBoundingClientRect(); p.querySelectorAll('.uplate,.faces,.disc,.vk,.vi').forEach((c) => { const r = c.getBoundingClientRect(); if (r.left < pr.left - 2 || r.right > pr.right + 2 || r.top < pr.top - 4 || r.bottom > pr.bottom + 4) bad.push('outside ' + name(p) + ': ' + (c.className.toString().slice(0, 18))); }); });
 // every over-head disc: one icon, centred in the disc's visible circle within 1 px
 st.querySelectorAll('.ohead .rd').forEach((d) => { const i = d.querySelector('.ic'); if (!i) { bad.push('disc without icon'); return; } const a = d.getBoundingClientRect(), b = i.getBoundingClientRect(); if (Math.abs((a.left + a.right) / 2 - (b.left + b.right) / 2) > 1 * s || Math.abs((a.top + a.bottom) / 2 - (b.top + b.bottom) / 2) > 1 * s) bad.push('icon off centre in ' + (d.dataset.cmp || 'disc')); });
 st.querySelectorAll('.ohead').forEach((h) => { if (h.children.length > 2) bad.push('more than two icons over a person'); });
 return JSON.stringify(bad); })()`;
// the budget: union of the pieces' rectangles on a 4 px grid (the roster's tabs, the clock bar, the pills ... whatever is drawn)
const BUDGET = `(() => { const st = document.getElementById('stage'), s = Number(st.dataset.scale) || 1, sr = st.getBoundingClientRect(), g = new Uint8Array(480 * 270);
 [...st.querySelectorAll('.hudlayer > *, .at > .pill, .at > .vplate, .at .ohead, .at > .chip')].forEach((e) => { const r = e.getBoundingClientRect(); if (r.width < 2) return;
   const x0 = Math.max(0, Math.floor((r.left - sr.left) / s / 4)), x1 = Math.min(480, Math.ceil((r.right - sr.left) / s / 4)), y0 = Math.max(0, Math.floor((r.top - sr.top) / s / 4)), y1 = Math.min(270, Math.ceil((r.bottom - sr.top) / s / 4));
   for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) g[y * 480 + x] = 1; });
 let n = 0; for (const v of g) n += v; return +(n / (480 * 270) * 100).toFixed(2); })()`;
// colours on the world marks: every stroke and fill in a .w29 layer must be one of the sourced colours
const COLS = `(() => { const ok = new Set(['#f7f4ec', '#0b1615', '#e03a2f', '#60e0ea', '#fff', '#000', 'none', 'url']); const bad = new Set(); document.querySelectorAll('#stage svg.w29 *').forEach((e) => { for (const a of ['stroke', 'fill']) { const v = e.getAttribute(a); if (!v) continue; if (v.startsWith('url(')) continue; if (!ok.has(v.toLowerCase())) bad.add(a + '=' + v); } }); return JSON.stringify([...bad]); })()`;

const budgets: Record<string, number> = {};
for (const sc of SCENES) {
  await go(sc);
  const bad = JSON.parse(await ev(QA)) as string[]; check(`QA ${sc}: nothing off screen, clipped, overlapping, off centre`, bad.length === 0, bad.slice(0, 4).join(" | "));
  budgets[sc] = await ev(BUDGET); check(`budget ${sc}: HUD ${budgets[sc]} % of the frame (limit 15 %)`, budgets[sc] <= 15);
  const cols = JSON.parse(await ev(COLS)) as string[]; check(`colours ${sc}: only sourced colours on the world marks`, cols.length === 0, cols.join(","));
  await shot(sc);
}
// scene facts
await go("w29-quiet"); const near = (a: number, b: number, t: number) => Math.abs(a - b) <= t;
const wedge = async (sc: string) => { await go(sc); return await ev(`[...document.querySelectorAll('#stage svg.w29 path')].filter((p) => (p.getAttribute('d') || '').includes(' A')).length`); };
check("quiet: no wedge drawn", (await wedge("w29-quiet")) === 0);
check("cone at Alert 2: one wedge", (await wedge("w29-cone-a2")) >= 1);
check("takedown: a wedge and the dashed near-body ring (1.5 m)", (await wedge("w29-takedown")) >= 1 && (await ev(`!!document.querySelector('#stage svg.w29 ellipse[stroke-dasharray]')`)) === true);
await go("w29-takedown"); const nr = await ev(`(() => { const e = document.querySelector('#stage svg.w29 ellipse[stroke-dasharray]'); return JSON.stringify([+e.getAttribute('rx'), +e.getAttribute('ry')]); })()`);
check("near-body ring radius is 1.5 m at 38.6 px/m (57.9 px), flattened by the group", near(JSON.parse(nr)[0], 57.9, 0.01) && near(JSON.parse(nr)[1], 57.9, 0.01), nr);
await go("w29-price"); check("price: five pills; the dashed marks are cream rings; exact marks are the kit's own images", (await ev(`document.querySelectorAll('#stage .pill').length`)) === 5 && (await ev(`document.querySelectorAll('#stage .dm').length`)) === 2 && (await ev(`document.querySelectorAll('#stage .mk0').length`)) === 2);
await go("w29-watch-on"); const von = await ev(`document.querySelectorAll('#stage [data-cmp=verbs-watch] .vi').length`);
await go("w29-watch-off"); const voff = await ev(`document.querySelectorAll('#stage [data-cmp=verbs-nowatch] .vi').length`);
check("verb plate: Watch offered shows 4 rows with Watch first; no Watch: 4 rows close up (Listen first), none empty", von === 4 && voff === 4, `${von}/${voff}`);
// negative control: a mood disc's icon moved 4 px must be caught
await go("w29-mood"); await ev(`(() => { const i = document.querySelector('#stage .ohead .rd .ic'); i.style.position = 'relative'; i.style.left = '4px'; })()`);
check("negative control: a mood icon 4 px off centre is caught by QA", (JSON.parse(await ev(QA)) as string[]).some((b) => b.startsWith("icon off centre")));
await ev(`(() => { const h = document.querySelector('#stage .ohead'); h.insertAdjacentHTML('beforeend', h.innerHTML); h.insertAdjacentHTML('beforeend', h.innerHTML); })()`);
check("negative control: a third icon over a person is caught", (JSON.parse(await ev(QA)) as string[]).some((b) => b.startsWith("more than two")));

// the 2x crops: no clipping of the new pieces at 2x either
await metrics(2); await go("w29-price"); await shot("w29-price-2x"); await go("w29-mood"); await shot("w29-mood-2x"); await go("w29-takedown"); await shot("w29-takedown-2x");

// the shadow rule against the engine's own examples (brief 055 §7.5 and host-interface 0.28.0)
const sh = (L: number, h: number, d: number, reach = 20) => { const q = shadowOf({ x: d, y: 0, r: 0.5, h }, L, reach); return q ? { s0: Math.hypot(...q[0]), s1: Math.hypot(...q[1]) } : null; };
const cam = sh(479, 200, 7.9); check("shadow: the 4.79 m lens over a 2 m planting 7.9 m away hides about 1.1 m behind it (ends at 9.03 m)", !!cam && near(cam.s1, 9.03, 0.05), JSON.stringify(cam));
check("shadow: a person (eye 160) over a 1.1 m planter hides nothing", sh(160, 110, 4) === null);
const wall = sh(160, 200, 4); check("shadow: a person behind a 2 m solid: everything behind is hidden to the reach", !!wall && near(wall.s0, 4.5, 0.01) && near(wall.s1, 20, 0.01), JSON.stringify(wall));
const rob = sh(95, 110, 2, 20); check("shadow: the patrol robot (eye 95) and a 1.1 m planter 2 m away: hidden from 4.33 x 2 = 8.7 m on", !!rob && near(rob.s0, 8.67, 0.05), JSON.stringify(rob));
const landing = sh(235, 620, 7.8, 12); check("shadow: the guard on his landing (eye 235) behind the 6.2 m cypress: hidden from the cypress back to the reach", !!landing && near(landing.s1, 12, 0.01), JSON.stringify(landing));

writeFileSync(join(out, "budget.json"), JSON.stringify(budgets, null, 2) + "\n");
console.log(problems.length ? "\nPROBLEMS:\n" + problems.join("\n") : "\nall checks passed");
chrome.kill(); process.exit(problems.length ? 1 : 0);
