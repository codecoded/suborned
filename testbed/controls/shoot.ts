// Headless check: drives Chrome over its debugging port with real mouse events,
// loads each preset, saves 1920x1080 PNGs into shots/ and reports console errors and fps.
// Run with the server up: `bun run controls` in one shell, `bun testbed/controls/shoot.ts` in another.
import { spawn } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const url = process.argv[2] ?? "http://localhost:5173/";
const out = join(import.meta.dir, "shots"); mkdirSync(out, { recursive: true });
const chrome = spawn("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", ["--headless=new", "--remote-debugging-port=9333", "--user-data-dir=/tmp/controls-shoot", "--window-size=1920,1080", "--hide-scrollbars", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist", "about:blank"], { stdio: "ignore" });
const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));
let tabs: any[] = [];
for (let i = 0; i < 50 && !tabs.length; i++) { await sleep(200); try { tabs = await (await fetch("http://127.0.0.1:9333/json")).json(); } catch {} }
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
const ev = async (expr: string) => (await send("Runtime.evaluate", { expression: expr, returnByValue: true, awaitPromise: true })).result?.value;
const mouse = (type: string, x: number, y: number, button = "none", clickCount = 0) => send("Input.dispatchMouseEvent", { type, x, y, button, clickCount, buttons: button === "left" ? 1 : button === "right" ? 2 : 0 });
const click = async (x: number, y: number, button = "left") => { await mouse("mouseMoved", x, y); await mouse("mousePressed", x, y, button, 1); await sleep(60); await mouse("mouseReleased", x, y, button, 1); };
const at = async (name: string) => { const p = await ev(`JSON.stringify(__ctl.screenOf('${name}'))`); return JSON.parse(p) as { x: number; y: number }; };
const shot = async (name: string) => { await sleep(350); const r = await send("Page.captureScreenshot", { format: "png" }); writeFileSync(join(out, name + ".png"), Buffer.from(r.data, "base64")); console.log("shot", name); };
const key = async (code: string, k: string) => { await send("Input.dispatchKeyEvent", { type: "keyDown", code, key: k }); await sleep(50); await send("Input.dispatchKeyEvent", { type: "keyUp", code, key: k }); };

await send("Runtime.enable"); await send("Page.enable");
await send("Emulation.setDeviceMetricsOverride", { width: 1920, height: 1080, deviceScaleFactor: 1, mobile: false });
await send("Page.navigate", { url }); await sleep(2500);
console.log("errors element:", JSON.stringify(await ev("document.getElementById('errors').textContent")));

for (const p of ["lead", "dbl", "built"]) {
  await ev(`__ctl.preset('${p}')`);
  await ev(`__ctl.G.world.agents[2].g.position.set(2, 0, 3.5)`); // the Tech stands by the cabinet
  await ev(`__ctl.select(2)`); await sleep(300);
  const cab = await at("cabinet");
  await mouse("mouseMoved", cab.x, cab.y); await sleep(700);
  await shot(`${p}-hover-cabinet`);
  const drv = await at("driver"); await ev(`__ctl.select(1)`);
  await mouse("mouseMoved", drv.x, drv.y); await sleep(300);
  await click(drv.x, drv.y, "right"); await sleep(300);
  await mouse("mouseMoved", drv.x + 70, drv.y - 40); await shot(`${p}-ring-driver`);
  await key("Escape", "Escape");
  await click(cab.x, cab.y); await sleep(400); // what a left click does here
  await shot(`${p}-after-left-click`);
  await ev(`__ctl.G.world.agents.forEach(a => a.order = null)`);
}
const reset = async () => { await ev(`document.querySelector('[data-reset]').click()`); await sleep(200); };
const logText = async () => await ev(`__ctl.G.log.map(l => l.text).join(' | ')`);
const check = (name: string, ok: boolean, extra = "") => { console.log(ok ? "PASS" : "FAIL", name, extra); if (!ok) problems.push("check failed: " + name); };
// squad: box select, group walk, group acts, portrait drag
const drag = async (x0: number, y0: number, x1: number, y1: number, mods = 0) => { await mouse("mouseMoved", x0, y0); await send("Input.dispatchMouseEvent", { type: "mousePressed", x: x0, y: y0, button: "left", clickCount: 1, buttons: 1, modifiers: mods }); for (let i = 1; i <= 6; i++) await send("Input.dispatchMouseEvent", { type: "mouseMoved", x: x0 + (x1 - x0) * i / 6, y: y0 + (y1 - y0) * i / 6, button: "left", buttons: 1, modifiers: mods }); await send("Input.dispatchMouseEvent", { type: "mouseReleased", x: x1, y: y1, button: "left", clickCount: 1, modifiers: mods }); };
const sel = async () => JSON.parse(await ev(`JSON.stringify(__ctl.G.group)`)) as boolean[];
await ev(`document.querySelector('[data-reset]').click()`); await sleep(300); await ev(`__ctl.preset('dbl'); __ctl.G.marker = 'pill'; __ctl.G.camMode = 'leash'`);
const pts = [] as { x: number; y: number }[]; for (let i = 0; i < 4; i++) pts.push(await at(`cell${i}`));
const xs = pts.map((p) => p.x), ys = pts.map((p) => p.y);
await drag(Math.min(...xs) - 60, Math.min(...ys) - 90, Math.max(...xs) + 50, Math.max(...ys) + 50); await sleep(100);
check("squad: a drag box selects every agent in it", (await sel()).every(Boolean), (await logText()).split(" | ").pop());
await shot("squad-box-select");
await ev(`__ctl.select(0)`); await key("Digit1", "1"); await send("Input.dispatchKeyEvent", { type: "keyDown", code: "Digit3", key: "3", modifiers: 8 }); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "Digit3", key: "3" }); await sleep(100);
check("squad: Shift + 3 adds the Tech", JSON.stringify(await sel()) === JSON.stringify([true, false, true, false]));
await key("Digit5", "5"); check("squad: 5 selects the whole squad", (await sel()).every(Boolean));
await click(xs[0] + 40, Math.max(...ys) + 160); await sleep(700);
const dests = JSON.parse(await ev(`JSON.stringify(__ctl.G.world.agents.map(a => a.order?.dest && [+a.order.dest.x.toFixed(1), +a.order.dest.z.toFixed(1)]))`));
const dmin = Math.min(...dests.flatMap((a: number[], i: number) => dests.slice(i + 1).map((b: number[]) => Math.hypot(a[0] - b[0], a[1] - b[1]))));
check("squad: a click on the ground sends all four, spaced apart", dests.every(Boolean) && dmin >= 0.9, `closest two destinations ${dmin.toFixed(2)} m apart; ${(await logText()).split(" | ").pop()}`);
await shot("squad-group-walk"); await sleep(5000);
// group act on a person: best placed does it, the rest hold; Everyone flips it
await ev(`__ctl.G.actScale = 20; __ctl.G.timeMode = 'total'`); const ine = await at("inez");
await mouse("mouseMoved", ine.x, ine.y); await sleep(500); await shot("squad-group-act-hover");
await click(ine.x, ine.y); await sleep(100); await click(ine.x, ine.y); await sleep(500);
let who = JSON.parse(await ev(`JSON.stringify(__ctl.G.world.agents.map(a => a.order?.kind === 'act'))`)) as boolean[];
check("squad act (best placed): one agent does it, the rest hold", who.filter(Boolean).length === 1, (await logText()).split(" | ").pop());
await shot("squad-group-act");
await ev(`__ctl.G.world.agents.forEach(a => { a.order = null }); __ctl.G.squadAct = 'all'`); const gd = await at("guard");
await click(gd.x, gd.y); await sleep(100); await click(gd.x, gd.y); await sleep(500);
who = JSON.parse(await ev(`JSON.stringify(__ctl.G.world.agents.map(a => a.order?.kind === 'act'))`));
check("squad act (everyone): all four Watch", who.every(Boolean), (await logText()).split(" | ").pop());
await ev(`__ctl.G.world.agents.forEach(a => { a.order = null }); __ctl.G.squadAct = 'best'`);
await ev(`__ctl.G.actScale = 1`); const po = await at("police1"); await click(po.x, po.y); await sleep(100); await click(po.x, po.y); await sleep(300);
who = JSON.parse(await ev(`JSON.stringify(__ctl.G.world.agents.map(a => a.order?.act?.id))`));
check("squad act on a hostile: everyone locks it", who.every((x: string) => x === "fire"), (await logText()).split(" | ").pop());
await shot("squad-hostile-lock");
// portrait drag: the Fixer alone (unselected) onto Inez
await ev(`__ctl.G.world.agents.forEach(a => { a.order = null }); __ctl.select(0)`); const r1 = JSON.parse(await ev(`JSON.stringify(document.querySelector('[data-agent="1"]').getBoundingClientRect())`));
await mouse("mouseMoved", r1.x + 40, r1.y + 20); await mouse("mousePressed", r1.x + 40, r1.y + 20, "left", 1);
for (let i = 1; i <= 6; i++) await send("Input.dispatchMouseEvent", { type: "mouseMoved", x: r1.x + 40 + (ine.x - r1.x - 40) * i / 6, y: r1.y + 20 + (ine.y - r1.y - 20) * i / 6, button: "left", buttons: 1 });
await shot("squad-portrait-drag"); await mouse("mouseReleased", ine.x, ine.y, "left", 1); await sleep(300);
check("portrait drag: the dragged agent alone gets the order", JSON.stringify(await sel()) === JSON.stringify([true, false, false, false]) && (await ev(`__ctl.G.world.agents[1].order?.act?.id`)) === "pay", (await logText()).split(" | ").pop());
// third person with a squad
await ev(`__ctl.G.world.agents.forEach(a => { a.order = null }); __ctl.squad()`); await key("KeyT", "t"); await sleep(500);
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "KeyW", key: "w" }); await sleep(2500); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "KeyW", key: "w" }); await sleep(800);
const far = JSON.parse(await ev(`JSON.stringify(__ctl.G.world.agents.map(a => Math.hypot(a.g.position.x - __ctl.G.world.agents[0].g.position.x, a.g.position.z - __ctl.G.world.agents[0].g.position.z)))`)) as number[];
check("third person: the squad follows the lead", far.slice(1).every((d) => d < 6), far.map((d) => d.toFixed(1)).join(" "));
await shot("squad-third-person"); await key("KeyT", "t"); await sleep(700);

// the marker styles on one target
await reset(); await ev(`__ctl.preset('lead'); __ctl.select(1)`);
for (const style of ["ring", "pill", "badge", "built"]) {
  await ev(`__ctl.G.marker = '${style}'`); const d = await at("inez"); await mouse("mouseMoved", d.x - 5, d.y); await mouse("mouseMoved", d.x, d.y); await sleep(900);
  await shot(`marker-${style}-inez`);
}
// the three time settings on one target
await reset(); await ev(`__ctl.preset('lead'); __ctl.select(1)`);
for (const mode of ["total", "split", "line"]) {
  await ev(`__ctl.G.timeMode = '${mode}'; __ctl.G.marker = 'pill'`); const d = await at("inez"); await mouse("mouseMoved", d.x - 5, d.y); await mouse("mouseMoved", d.x, d.y); await sleep(700);
  console.log(mode, "->", await ev(`document.querySelector('#marker').textContent`));
  await shot(`time-${mode}-inez`);
}
await ev(`__ctl.G.timeMode = 'total'`);
// behaviour: what each click does, with acts sped up
await reset(); await ev(`__ctl.preset('lead'); __ctl.G.actScale = 60; __ctl.select(2)`);
let c = await at("cabinet"); await click(c.x, c.y); await sleep(7000);
check("lead: left click on the cabinet loops the camera", (await ev(`__ctl.G.trail`)) === 1, await logText());
await reset(); await ev(`__ctl.preset('lead'); __ctl.select(2)`);
const g1 = await at("cabinet"); await click(g1.x - 120, g1.y + 90); await sleep(300);
check("lead: left click on the ground walks", (await ev(`__ctl.G.world.agents[2].order?.kind`)) === "walk");
await click(g1.x, g1.y, "right"); await sleep(200);
check("lead: a right tap opens the ring", (await ev(`!!__ctl.G.ring`)) === true);
await key("Escape", "Escape");
await mouse("mouseMoved", g1.x, g1.y); await mouse("mousePressed", g1.x, g1.y, "right", 1); await sleep(400); await mouse("mouseReleased", g1.x, g1.y, "right", 1); await sleep(100);
check("lead: a right hold opens nothing", (await ev(`!__ctl.G.ring`)) === true, (await logText()).split(" | ").pop());
await reset(); await ev(`__ctl.preset('dbl'); __ctl.G.actScale = 60; __ctl.select(2)`);
c = await at("cabinet"); await click(c.x, c.y); await sleep(300);
check("double click: one click only selects", (await ev(`__ctl.G.world.agents[2].order`)) === null && (await ev(`__ctl.G.selTarget?.id`)) === "cabinet");
await mouse("mousePressed", c.x, c.y, "left", 1); await mouse("mouseReleased", c.x, c.y, "left", 1); await sleep(7000);
check("double click: the second click does the main action", (await ev(`__ctl.G.trail`)) === 1, await logText());
// third person
await reset(); await ev(`__ctl.preset('lead'); __ctl.G.camMode = 'leash'; __ctl.select(3)`); await key("KeyT", "t"); await sleep(900);
const f0 = await ev(`__ctl.G.rig.f`); const p0 = await ev(`__ctl.G.world.agents[3].g.position.x`);
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "KeyD", key: "d" }); await sleep(1500); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "KeyD", key: "d" });
const f1 = await ev(`__ctl.G.rig.f`), p1 = await ev(`__ctl.G.world.agents[3].g.position.x`);
check("third person leash: walking sideways turns the camera slowly", Math.abs(f1 - f0) > 0.05 && Math.abs(f1 - f0) < 0.9, `yaw moved ${(((f1 - f0) * 180) / Math.PI).toFixed(1)} deg in 1.5 s; agent x ${p0.toFixed(2)} -> ${p1.toFixed(2)}`);
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "KeyW", key: "w" }); await sleep(600); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "KeyW", key: "w" });
check("third person: W moves the agent", Math.abs((await ev(`__ctl.G.world.agents[3].g.position.z`)) - 3.1) > 0.3);
await key("KeyT", "t"); await sleep(900);

// third person with the leash camera
await ev(`__ctl.preset('lead')`); await ev(`__ctl.select(3); __ctl.G.camMode = 'leash'`); await key("KeyT", "t"); await sleep(1200);
await key("KeyW", "w"); const pol = await at("guard").catch(() => null); if (pol) await mouse("mouseMoved", pol.x, pol.y); await shot("lead-third-person");

// frame rate over 3 s
const fps = await ev(`new Promise(r => { let n = 0; const t0 = performance.now(); const f = () => { n++; performance.now() - t0 < 3000 ? requestAnimationFrame(f) : r(Math.round(n * 1000 / (performance.now() - t0))); }; f(); })`);
console.log("fps over 3 s (headless, software render possible):", fps);
console.log("page errors:", JSON.stringify(await ev("document.getElementById('errors').textContent")));
console.log("console problems:", problems.length ? problems : "none");
ws.close(); chrome.kill();
process.exit(problems.length ? 1 : 0);
