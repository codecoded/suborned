// Phone and tablet check for the controls lab (Sky, 2026-10-24): headless Chrome with mobile emulation and real touch events (CDP Input.dispatchTouchEvent).
// For each screen it checks the layout (no overflow, 44 px buttons, no panels on top of each other), drives the gestures (tap, hold, drag, pinch, twist, dock, stick, fire),
// shoots options C, E and F at two camera turns with the panels folded, then the panels open, into shots/mobile/. Console errors fail the run.
//   PORT=5191 bun testbed/controls/serve.ts    then    bun testbed/controls/shoot-mobile.ts [url] [outdir]      (SCREENS=portrait,landscape,tablet,tablet-land to pick)
import { spawn } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const url = process.argv[2] ?? "http://localhost:5191/";
const out = process.argv[3] ?? join(import.meta.dir, "shots", "mobile"); mkdirSync(out, { recursive: true });
const DP = 9394;
const SCREENS: Record<string, { w: number; h: number; insets: { top: number; left: number; bottom: number; right: number } }> = {
  portrait: { w: 390, h: 844, insets: { top: 47, left: 0, bottom: 34, right: 0 } },
  landscape: { w: 844, h: 390, insets: { top: 0, left: 47, bottom: 21, right: 47 } },
  tablet: { w: 820, h: 1180, insets: { top: 24, left: 0, bottom: 20, right: 0 } },
  "tablet-land": { w: 1180, h: 820, insets: { top: 24, left: 0, bottom: 20, right: 0 } },
};
const chrome = spawn("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", ["--headless=new", `--remote-debugging-port=${DP}`, "--user-data-dir=/tmp/controls-shoot-mobile", "--window-size=1200,900", "--hide-scrollbars", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist", "about:blank"], { stdio: "ignore" });
const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));
let tabs: any[] = [];
for (let i = 0; i < 50 && !tabs.length; i++) { await sleep(200); try { tabs = await (await fetch(`http://127.0.0.1:${DP}/json`)).json(); } catch {} }
const ws = new WebSocket(tabs.find((t) => t.type === "page").webSocketDebuggerUrl);
await new Promise((r) => (ws.onopen = r));
let id = 0; const wait = new Map<number, (v: any) => void>(); const problems: string[] = [];
ws.onmessage = (m) => {
  const d = JSON.parse(String(m.data));
  if (d.id && wait.has(d.id)) wait.get(d.id)!(d.result ?? { error: d.error });
  if (d.method === "Runtime.consoleAPICalled" && ["error", "warning"].includes(d.params.type)) problems.push(`${d.params.type}: ${d.params.args.map((a: any) => a.value ?? a.description).join(" ")}`);
  if (d.method === "Runtime.exceptionThrown") problems.push(`exception: ${d.params.exceptionDetails.exception?.description ?? d.params.exceptionDetails.text}`);
};
const send = (method: string, params: any = {}) => new Promise<any>((res) => { const i = ++id; wait.set(i, res); ws.send(JSON.stringify({ id: i, method, params })); });
const ev = async (expr: string) => (await send("Runtime.evaluate", { expression: expr, returnByValue: true, awaitPromise: true })).result?.value;
let tag = "", failed = 0;
const check = (name: string, ok: boolean, extra = "") => { console.log(ok ? "PASS" : "FAIL", `[${tag}]`, name, extra); if (!ok) { failed++; problems.push(`check failed [${tag}]: ${name}`); } };
const shot = async (name: string, wait = 900) => { await sleep(wait); const r = await send("Page.captureScreenshot", { format: "png" }); writeFileSync(join(out, `${tag}-${name}.png`), Buffer.from(r.data, "base64")); console.log("shot", `${tag}-${name}`); };

// ---- touch
type P = [number, number];
const tp = (pts: P[]) => pts.map(([x, y], i) => ({ x, y, id: i + 1, radiusX: 8, radiusY: 8, force: 1 }));
const tstart = (pts: P[]) => send("Input.dispatchTouchEvent", { type: "touchStart", touchPoints: tp(pts) });
const tmove = (pts: P[]) => send("Input.dispatchTouchEvent", { type: "touchMove", touchPoints: tp(pts) });
const tend = (rest: P[] = []) => send("Input.dispatchTouchEvent", { type: "touchEnd", touchPoints: tp(rest) });
const tap = async (x: number, y: number) => { await tstart([[x, y]]); await sleep(50); await tend(); await sleep(120); };
const hold = async (x: number, y: number, ms: number, release = true) => { await tstart([[x, y]]); await sleep(ms); if (release) { await tend(); await sleep(150); } };
const drag = async (x0: number, y0: number, x1: number, y1: number, steps = 10, release = true) => { await tstart([[x0, y0]]); for (let i = 1; i <= steps; i++) { await tmove([[x0 + (x1 - x0) * i / steps, y0 + (y1 - y0) * i / steps]]); await sleep(16); } if (release) { await tend(); await sleep(150); } };
const pinch = async (cx: number, cy: number, d0: number, d1: number, steps = 10) => {
  const at = (d: number): P[] => [[cx - d / 2, cy], [cx + d / 2, cy]];
  await tstart(at(d0)); for (let i = 1; i <= steps; i++) { await tmove(at(d0 + (d1 - d0) * i / steps)); await sleep(16); } await tend(); await sleep(150);
};
const twist = async (cx: number, cy: number, r: number, deg: number, steps = 14) => {
  const at = (a: number): P[] => [[cx + r * Math.cos(a), cy + r * Math.sin(a)], [cx - r * Math.cos(a), cy - r * Math.sin(a)]];
  await tstart(at(0)); for (let i = 1; i <= steps; i++) { await tmove(at(deg * Math.PI / 180 * i / steps)); await sleep(16); } await tend(); await sleep(150);
};
const rect = async (sel: string) => JSON.parse(await ev(`(() => { const e = document.querySelector(${JSON.stringify(sel)}); if (!e) return 'null'; const r = e.getBoundingClientRect(); return JSON.stringify({ x: r.x + r.width / 2, y: r.y + r.height / 2, w: r.width, h: r.height }); })()`));
const press = async (sel: string) => { const r = await rect(sel); await tap(r.x, r.y); };
const scr = async (id: string): Promise<{ x: number; y: number }> => JSON.parse(await ev(`JSON.stringify(__ctl.screenOf('${id}'))`));
const proj = async (x: number, y: number, z: number): Promise<{ x: number; y: number }> => JSON.parse(await ev(`JSON.stringify(__ctl.project(${x}, ${y}, ${z}))`));

// ---- layout audit: nothing leaves the screen, buttons are 44 px, the chrome does not sit on itself
const CHROME = "#top, #top *, #stats, #occ, #occ *, #roster, #roster *, #dock, #dock *, #log, #log *, #paused, #hist, #test, #histkey, #menukey";
const audit = async (label: string) => {
  const r = JSON.parse(await ev(`(() => {
    const W = innerWidth, H = innerHeight, vis = (e) => { const r = e.getBoundingClientRect(), c = getComputedStyle(e); return r.width > 0 && r.height > 0 && c.display !== 'none' && c.visibility !== 'hidden' && c.opacity !== '0'; };
    const out = [], small = [], name = (e) => e.id ? '#' + e.id : e.tagName.toLowerCase() + (e.dataset.agent !== undefined ? '[agent ' + e.dataset.agent + ']' : '') + (e.className && typeof e.className === 'string' ? '.' + e.className.split(' ')[0] : '') + (e.textContent ? '"' + e.textContent.trim().slice(0, 14) + '"' : '');
    const top = document.getElementById('top'), menu = document.body.classList.contains('menu-open');
    for (const e of document.querySelectorAll(${JSON.stringify(CHROME)})) { if (!vis(e) || e.closest('svg') || (e !== top && top.contains(e) && top.scrollHeight > top.clientHeight)) continue; const r = e.getBoundingClientRect(); if (r.left < -1 || r.top < -1 || r.right > W + 1 || r.bottom > H + 1) out.push(name(e) + ' ' + [r.left, r.top, r.right, r.bottom].map(Math.round)); }
    for (const e of document.querySelectorAll('#top button, #occ button, #roster button, #dock button, #menukey')) { if (!vis(e) || e.closest('svg') || (top.contains(e) && !menu)) continue; const r = e.getBoundingClientRect(); if (e.closest('.aplate') || e.classList.contains('free')) continue; if (r.width < 43.5 || r.height < 43.5) small.push(name(e) + ' ' + Math.round(r.width) + 'x' + Math.round(r.height)); }
    const groups = { menu: '#top', stats: '#stats', occ: '#occ', roster: '#roster', dock: '#dock', log: '#log' }, rs = {};
    for (const [k, s] of Object.entries(groups)) { const e = document.querySelector(s); if (!e || !vis(e)) continue; if (k === 'log' && !e.children.length) continue; rs[k] = e.getBoundingClientRect(); }
    const over = [], ks = Object.keys(rs).filter((k) => !(menu && k === 'menu')); for (let i = 0; i < ks.length; i++) for (let j = i + 1; j < ks.length; j++) { const a = rs[ks[i]], b = rs[ks[j]]; if (a.left < b.right - 1 && b.left < a.right - 1 && a.top < b.bottom - 1 && b.top < a.bottom - 1) over.push(ks[i] + '/' + ks[j]); }
    const de = document.documentElement;
    return JSON.stringify({ scroll: [de.scrollWidth, de.scrollHeight, W, H], off: out, small, over, bodyScroll: [document.body.scrollWidth, document.body.scrollHeight], coarse: matchMedia('(pointer: coarse)').matches, cls: document.body.className, dpr: devicePixelRatio, pr: __ctl.G.dpr });
  })()`));
  check(`${label}: page does not scroll`, r.scroll[0] <= r.scroll[2] && r.scroll[1] <= r.scroll[3] && r.bodyScroll[0] <= r.scroll[2] && r.bodyScroll[1] <= r.scroll[3], JSON.stringify(r.scroll));
  check(`${label}: nothing leaves the screen`, r.off.length === 0, r.off.join(" | "));
  check(`${label}: every button is 44 px`, r.small.length === 0, r.small.join(" | "));
  check(`${label}: panels do not sit on each other`, r.over.length === 0, r.over.join(", "));
  return r;
};

await send("Runtime.enable"); await send("Page.enable");
const only = (process.env.SCREENS ?? "portrait,landscape,tablet,tablet-land").split(",");
for (const name of only) {
  const S = SCREENS[name]; tag = name;
  await send("Emulation.setDeviceMetricsOverride", { width: S.w, height: S.h, deviceScaleFactor: Number(process.env.DPR ?? 2), mobile: true, screenWidth: S.w, screenHeight: S.h });
  await send("Emulation.setTouchEmulationEnabled", { enabled: true, maxTouchPoints: 5 });
  const safe = await send("Emulation.setSafeAreaInsetsOverride", { insets: S.insets }); if (safe?.error) console.log("safe-area override not available:", JSON.stringify(safe.error));
  await send("Page.navigate", { url: url + "?pr=2" }); await sleep(2800);
  console.log(`== ${name} ${S.w}x${S.h}`, "errors element:", JSON.stringify(await ev("document.getElementById('errors').textContent")));
  const land = S.w > S.h;
  const a0 = await audit("folded"); check("the page sees a touch screen", a0.coarse && /\btouch\b/.test(a0.cls) && /\bm\b/.test(a0.cls), a0.cls);
  check("pixel ratio capped at 2", a0.pr <= 2, `renderer ${a0.pr}`);
  await shot("folded", 1200);
  const squad = (x: number, z: number) => ev(`(() => { const G = __ctl.G; const pos = [[-1, 0], [1.2, -0.8], [3.4, 0.2], [5.6, -0.6]]; G.world.agents.forEach((a, i) => { a.g.position.set(${x} + pos[i][0], 0, ${z} + pos[i][1]); a.order = null; }); G.rig.focus.set(${x} + 2, 0, ${z}); G.rig.focusGoal = null; })()`);

  // ---- gestures (cmd view)
  await ev(`__ctl.squad(); __ctl.select(0); __ctl.G.actScale = 60`); await squad(2, 3); await ev(`__ctl.yaw(30)`); await sleep(500);
  // the other three stand off to the east so a finger near the Scout or the cabinet has only that one under it
  await ev(`[1, 2, 3].forEach((i) => __ctl.G.world.agents[i].g.position.set(9 + i, 0, 2.5))`); await ev(`__ctl.G.world.agents[0].g.position.set(-7, 0, 2.4)`); await ev(`__ctl.rig.focus.set(-3, 0, 3.5)`); await sleep(700);
  const me = await ev(`JSON.stringify(__ctl.G.world.agents[0].g.position)`).then(JSON.parse);
  const free = JSON.parse(await ev(`JSON.stringify(__ctl.G.world.nav.nearestFree(${me.x + 2.5}, ${me.z + 1}))`)), g1 = await proj(free[0], 0, free[1]);
  await tap(g1.x, g1.y); await sleep(200);
  check("tap on the ground walks the selected agent", (await ev(`__ctl.G.world.agents[0].order?.kind`)) === "walk", await ev(`__ctl.G.log.slice(-1)[0]?.text`));
  const a1 = await proj(me.x, 0.9, me.z);
  await ev(`__ctl.select(1)`); await ev(`__ctl.G.world.agents[0].order = null; __ctl.G.world.agents[0].g.position.set(-7, 0, 2.4)`); await sleep(200); await tap(a1.x, a1.y); await sleep(150);
  check("tap on your own agent selects it", (await ev(`__ctl.G.sel`)) === 0 && (await ev(`__ctl.G.group.filter(Boolean).length`)) === 1);
  await ev(`__ctl.G.world.agents[2].g.position.set(2, 0, 3.5); __ctl.G.world.agents[2].order = null; __ctl.select(2); __ctl.rig.focus.set(3, 0, 3)`); await sleep(900);
  let c = await scr("cabinet");
  await tap(c.x, c.y); await sleep(250);
  check("tap on a thing opens its ring", (await ev(`__ctl.G.ring?.ent?.id`)) === "cabinet", await ev(`__ctl.G.log.slice(-1)[0]?.text`));
  await shot("ring-cabinet", 300);
  // the ring on a phone: tap a wedge to see its price, tap it again to take it
  const ring = JSON.parse(await ev(`(() => { const w = document.querySelector('#ring .wedge'); if (!w) return 'null'; const r = w.getBoundingClientRect(); return JSON.stringify({ x: r.x + r.width / 2, y: r.y + r.height / 2 }); })()`));
  const wedge2 = JSON.parse(await ev(`(() => { const w = document.querySelectorAll('#ring .wedge')[1]; if (!w) return 'null'; const r = w.getBoundingClientRect(); return JSON.stringify({ x: r.x + r.width / 2, y: r.y + r.height / 2 }); })()`));
  if (wedge2) { await tap(wedge2.x, wedge2.y); check("first tap on a ring wedge shows its price, takes nothing", (await ev(`__ctl.G.ring?.hover`)) === 1 && !(await ev(`__ctl.G.world.agents[2].order`))); }
  const empty = await proj(-6, 0, 6); await tap(empty.x, empty.y); await sleep(200);
  check("tap away closes the ring", (await ev(`!__ctl.G.ring`)) === true);
  // hold = the left hold (act)
  await ev(`__ctl.G.world.agents[2].g.position.set(2, 0, 3.5); __ctl.G.world.agents[2].order = null`); c = await scr("cabinet");
  await tstart([[c.x, c.y]]); await sleep(300);
  check("press and hold on a thing fills the pill", !!(await ev(`__ctl.G.charge && !__ctl.G.charge.done`)), await ev(`JSON.stringify(__ctl.G.charge && { t: __ctl.G.charge.t, ready: __ctl.G.charge.ready })`));
  await shot("hold-cabinet", 500);
  await sleep(600); await tend(); await sleep(400);
  check("lift after the pill is full gives the order", /^Hold · Tech/.test(await ev(`__ctl.G.log.slice(-1)[0]?.text`)), await ev(`__ctl.G.log.slice(-1)[0]?.text`));
  await ev(`__ctl.G.world.agents[2].order = null`);
  // a portrait dragged onto a thing: that agent goes (the drag starts on the roster, so the page must not scroll or cancel it)
  await ev(`__ctl.G.world.agents[2].g.position.set(2, 0, 3.5); __ctl.G.world.agents[2].order = null; __ctl.select(0); __ctl.rig.focus.set(3, 0, 3)`); await sleep(800); c = await scr("cabinet"); const pb = await rect('#roster [data-agent="2"]');
  await tstart([[pb.x, pb.y]]); const steps = 14; for (let i = 1; i <= steps; i++) { await tmove([[pb.x + (c.x - pb.x) * i / steps, pb.y + (c.y - pb.y) * i / steps]]); await sleep(16); } await tend(); await sleep(400);
  check("drag a portrait onto a thing sends that agent", /^Portrait drop/.test(await ev(`__ctl.G.log.slice(-1)[0]?.text`)), await ev(`__ctl.G.log.slice(-1)[0]?.text`)); await ev(`__ctl.G.world.agents[2].order = null`);
  // pan
  const f0 = JSON.parse(await ev(`JSON.stringify(__ctl.rig.focus)`)); const mid = { x: S.w / 2, y: S.h * 0.45 };
  await drag(mid.x, mid.y, mid.x - 90, mid.y + 40);
  const f1 = JSON.parse(await ev(`JSON.stringify(__ctl.rig.focus)`));
  check("one-finger drag on the ground pans the camera, orders nothing", Math.hypot(f1.x - f0.x, f1.z - f0.z) > 1 && !(await ev(`__ctl.G.world.agents.some(a => a.order && a.order.kind === 'walk' && a !== __ctl.G.world.agents[0])`)), `focus moved ${Math.hypot(f1.x - f0.x, f1.z - f0.z).toFixed(1)} m`);
  // pinch
  const d0 = await ev(`__ctl.rig.distGoal`); await pinch(mid.x, mid.y, 60, 160); const d1 = await ev(`__ctl.rig.distGoal`); await pinch(mid.x, mid.y, 200, 70); const d2 = await ev(`__ctl.rig.distGoal`);
  check("pinch out zooms in, pinch in zooms out", d1 < d0 && d2 > d1, `dist ${d0.toFixed(1)} -> ${d1.toFixed(1)} -> ${d2.toFixed(1)}`);
  await ev(`__ctl.rig.distGoal = ${d0}`);
  // twist
  const y0 = await ev(`__ctl.rig.yawTarget`); await twist(mid.x, mid.y, 60, 70); const y1 = await ev(`__ctl.rig.yawTarget`); await twist(mid.x, mid.y, 60, -70); const y2 = await ev(`__ctl.rig.yawTarget`);
  check("two-finger twist turns the camera a quarter, clockwise = E, back = Q", Math.abs(y1 - y0 - Math.PI / 2) < 0.01 && Math.abs(y2 - y0) < 0.01, `yaw ${(y0 * 180 / Math.PI).toFixed(0)} -> ${(y1 * 180 / Math.PI).toFixed(0)} -> ${(y2 * 180 / Math.PI).toFixed(0)}`);
  await ev(`__ctl.yaw(30)`);
  // the dock
  await press('#dock [data-code="KeyE"]'); const y3 = await ev(`__ctl.rig.yawTarget`); await press('#dock [data-code="KeyQ"]'); const y4 = await ev(`__ctl.rig.yawTarget`);
  check("dock E and Q turn the camera", y3 > y4 && Math.abs(y3 - y4 - Math.PI / 2) < 0.01, `${y3.toFixed(2)} ${y4.toFixed(2)}`);
  const o0 = await ev(`__ctl.G.occ`); await press('#occ [data-occstep="1"]'); const o1 = await ev(`__ctl.G.occ`); await press('#occ [data-occstep="-1"]'); const o2 = await ev(`__ctl.G.occ`);
  check("occlusion arrows step like O and Shift+O", o0 !== o1 && o2 === o0, `${o0} ${o1} ${o2}`);
  await press('#dock [data-code="Space"]'); check("dock Pause pauses, the button says Play", (await ev(`__ctl.G.paused`)) === true && (await ev(`document.querySelector('#dock [data-code="Space"]').textContent`)) === "Play");
  await press('#dock [data-code="Space"]'); check("dock Play resumes", (await ev(`__ctl.G.paused`)) === false);
  await press('#dock [data-code="KeyC"]'); check("dock Follow turns follow on", (await ev(`__ctl.G.camFollow`)) === true); await press('#dock [data-code="KeyC"]');
  await ev(`__ctl.squad()`); const sq = await ev(`__ctl.G.group.filter(Boolean).length`); check("Squad selects everyone", sq === 4);
  // Tab: two people close together under the touch
  // fire mode with officers out
  await ev(`__ctl.yaw(210)`); await ev(`document.querySelector('[data-vale]').click()`); await sleep(300); await ev(`__ctl.select(3); __ctl.G.world.agents[3].g.position.set(7.5, 0, 4)`); await ev(`__ctl.rig.focus.set(11, 0, 5)`); await sleep(900);
  const off = await scr("officerA");
  await press('#dock [data-tb="firemode"]'); check("Fire mode button arms fire", (await ev(`__ctl.G.touchFire`)) === true && (await ev(`document.body.classList.contains('fire-armed')`)));
  await tstart([[off.x, off.y]]); await sleep(900);
  check("fire mode: hold on an officer fills the fire pill", !!(await ev(`__ctl.G.charge && __ctl.G.charge.way.id === 'fire'`)) || !!(await ev(`__ctl.G.sessions.length`)), await ev(`JSON.stringify(__ctl.G.charge && { id: __ctl.G.charge.way.id, t: __ctl.G.charge.t, ready: __ctl.G.charge.ready })`));
  await shot("fire-mode-hold", 300); await tend(); await sleep(900);
  check("fire mode: letting go after it is full starts the fire order", (await ev(`__ctl.G.sessions.length`)) > 0 || (await ev(`__ctl.G.world.agents[3].order?.kind`)) === "fire", await ev(`__ctl.G.log.slice(-2).map(l => l.text).join(' | ')`));
  await press('#dock [data-tb="firemode"]'); check("Fire mode button disarms", (await ev(`__ctl.G.touchFire`)) === false);

  // ---- portrait drag from the roster onto the ground
  await ev(`__ctl.G.stopped = null; __ctl.G.sessions = []; __ctl.G.fightUntil = 0`); await ev(`__ctl.G.world.agents.forEach(a => { a.order = null; a.fireAt = null; })`);

  // ---- panels
  await press("#menukey"); await sleep(250); await audit("menu open"); await shot("menu-open", 300);
  await press("#top [data-settings]"); await sleep(250); const au = await audit("settings open"); await shot("settings-open", 300);
  const sc = JSON.parse(await ev(`(() => { const t = document.getElementById('top'); return JSON.stringify([t.scrollHeight, t.clientHeight]); })()`));
  check("settings panel scrolls inside the screen", sc[0] >= sc[1]);
  // scroll it with a finger
  const topR = await rect("#top"); await drag(topR.x, topR.y + topR.h / 3, topR.x, topR.y - topR.h / 3, 8); const st = await ev(`document.getElementById('top').scrollTop`);
  check("a finger scrolls the settings panel", st > 0 || sc[0] <= sc[1] + 1, `scrollTop ${st}`);
  await shot("settings-scrolled", 300);
  await press("#menukey"); await sleep(200); check("menu folds again", !(await ev(`document.body.classList.contains('menu-open')`)) && !(await ev(`document.body.classList.contains('opts-open')`)));
  await press('#occ [data-occopen]'); await sleep(250); await audit("occlusion open"); await shot("occ-open", 300);
  await press('#occ [data-occ="floors"]'); check("picking E in the panel sets Floors and folds the panel", (await ev(`__ctl.G.occ`)) === "floors" && !(await ev(`document.getElementById('occ').classList.contains('open')`)));
  await press('#dockkey'); await sleep(200); await audit("dock folded"); await shot("dock-folded", 300); await press('#dockkey');

  // ---- third person: stick, look, fire
  await ev(`document.querySelectorAll('[data-reset]')[0].click()`); await sleep(300); await ev(`__ctl.select(3)`); await sleep(100);
  await press('#dock [data-code="KeyT"]'); await sleep(1200);
  check("dock 3rd goes to third person", (await ev(`__ctl.G.view`)) === "tp" && (await ev(`document.body.dataset.view`)) === "tp");
  await audit("third person");
  const p0 = await ev(`JSON.stringify(__ctl.G.world.agents[3].g.position)`).then(JSON.parse), f0t = await ev(`__ctl.rig.f`);
  await tstart([[S.w * 0.25, S.h * 0.55]]); for (let i = 1; i <= 8; i++) { await tmove([[S.w * 0.25, S.h * 0.55 - i * 9]]); await sleep(16); } await sleep(900);
  const stickShown = await ev(`getComputedStyle(document.getElementById('stick')).display`);
  await shot("tp-stick", 100);
  await tend(); await sleep(200);
  const p1 = await ev(`JSON.stringify(__ctl.G.world.agents[3].g.position)`).then(JSON.parse);
  check("third person: dragging the left half walks the agent (stick drawn)", Math.hypot(p1.x - p0.x, p1.z - p0.z) > 0.5 && stickShown === "block", `moved ${Math.hypot(p1.x - p0.x, p1.z - p0.z).toFixed(2)} m`);
  check("third person: lifting stops the walk and clears the stick", (await ev(`getComputedStyle(document.getElementById('stick')).display`)) === "none" && (await ev(`['KeyW','KeyA','KeyS','KeyD','ShiftLeft'].some(k => __ctl.G.keys.has(k))`)) === false);
  const f1t0 = await ev(`__ctl.rig.f`); await drag(S.w * 0.75, S.h * 0.5, S.w * 0.75 + 80, S.h * 0.5, 8); const f1t = await ev(`__ctl.rig.f`);
  check("third person: dragging the right half looks around", Math.abs(f1t - f1t0) > 0.1, `${f1t0.toFixed(2)} -> ${f1t.toFixed(2)}`);
  const fb = await rect("#tpfire"); await tstart([[fb.x, fb.y]]); await sleep(500); const aim = await ev(`__ctl.G.aim`); await shot("tp-fire-held", 100); await tend(); await sleep(200);
  check("third person: Hold to fire raises the aim while held, drops it on release", aim === true && (await ev(`__ctl.G.aim`)) === false, `aim while held ${aim}`);
  await press('#dock [data-code="Escape"]'); await sleep(900); check("dock Back returns to the commander view", (await ev(`__ctl.G.view`)) === "cmd");

  // ---- the shots: options C, E, F at two turns, panels folded
  await ev(`document.querySelectorAll('[data-reset]')[0].click()`); await sleep(300); await ev(`__ctl.G.world.agents.forEach(a => { a.order = null; })`);
  await ev(`__ctl.squad()`);
  const stand = (yaw: number) => (yaw === 120 ? { x: 14.5, z: 3 } : { x: 2, z: 3 });
  for (const yaw of [30, 120]) {
    await squad(stand(yaw).x, stand(yaw).z); await ev(`__ctl.yaw(${yaw})`); await ev(`__ctl.rig.distGoal = __ctl.rig.dist = ${await ev("__ctl.rig.dist")}`);
    for (const m of ["circle", "floors", "mix"]) { await ev(`__ctl.setOcc('${m}')`); await shot(`${m}-yaw${yaw}`, 1500); }
  }
  // ---- and with the occlusion panel open and the menu open over a scene
  await ev(`__ctl.setOcc('floors'); __ctl.yaw(30)`); await squad(2, 3); await press('#occ [data-occopen]'); await shot("floors-occ-panel", 1500);
  console.log("fps", await ev(`new Promise(r => { let n = 0; const t0 = performance.now(); const f = () => { n++; performance.now() - t0 < 2000 ? requestAnimationFrame(f) : r(Math.round(n * 1000 / (performance.now() - t0))); }; f(); })`), "(software GL)");
  await audit("end");
  console.log(`page errors [${name}]:`, JSON.stringify(await ev("document.getElementById('errors').textContent")));
}
console.log("console problems:", problems.length ? problems : "none");
console.log(failed ? `${failed} check(s) failed` : "all checks passed");
ws.close(); chrome.kill();
process.exit(problems.length ? 1 : 0);
