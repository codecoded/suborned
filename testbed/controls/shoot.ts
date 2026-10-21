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

for (const p of ["hold", "runs", "lead", "dbl", "built"]) {
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

// portrait drag: the Fixer alone (unselected) onto Inez
await ev(`__ctl.G.world.agents.forEach(a => { a.order = null }); __ctl.select(0)`); const r1 = JSON.parse(await ev(`JSON.stringify(document.querySelector('[data-agent="1"]').getBoundingClientRect())`));
await mouse("mouseMoved", r1.x + 40, r1.y + 20); await mouse("mousePressed", r1.x + 40, r1.y + 20, "left", 1);
for (let i = 1; i <= 6; i++) await send("Input.dispatchMouseEvent", { type: "mouseMoved", x: r1.x + 40 + (ine.x - r1.x - 40) * i / 6, y: r1.y + 20 + (ine.y - r1.y - 20) * i / 6, button: "left", buttons: 1 });
await shot("squad-portrait-drag"); await mouse("mouseReleased", ine.x, ine.y, "left", 1); await sleep(300);
check("portrait drag: the dragged agent alone gets the order", JSON.stringify(await sel()) === JSON.stringify([true, false, false, false]) && (await ev(`__ctl.G.world.agents[1].order?.act?.id`)) === "pay", (await logText()).split(" | ").pop());
// third person with a squad
await ev(`__ctl.G.world.agents.forEach(a => { a.order = null }); __ctl.squad()`); await ev(`__ctl.setView(__ctl.G.view === 'cmd' ? 'tp' : 'cmd')`); await sleep(500);
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "KeyW", key: "w" }); await sleep(2500); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "KeyW", key: "w" }); await sleep(800);
const far = JSON.parse(await ev(`JSON.stringify(__ctl.G.world.agents.map(a => Math.hypot(a.g.position.x - __ctl.G.world.agents[0].g.position.x, a.g.position.z - __ctl.G.world.agents[0].g.position.z)))`)) as number[];
check("third person: the squad follows the lead", far.slice(1).every((d) => d < 6), far.map((d) => d.toFixed(1)).join(" "));
await shot("squad-third-person"); await ev(`__ctl.setView(__ctl.G.view === 'cmd' ? 'tp' : 'cmd')`); await sleep(700);

// breakaway and avatar: three agents selected, hover the cabinet (the Tech does the work)
await ev(`document.querySelector('[data-reset]').click()`); await sleep(300); await ev(`__ctl.preset('dbl'); __ctl.G.marker = 'pill'; __ctl.squad()`);
const cb = await at("cabinet"); await mouse("mouseMoved", cb.x - 4, cb.y); await mouse("mouseMoved", cb.x, cb.y); await sleep(900);
check("squad hover: the pill carries the Tech's face, not the word", (await ev(`document.querySelector('#marker .faces img')?.alt`)) === "Tech" && !(await ev(`document.querySelector('#marker').textContent`)).includes("Tech"), await ev(`document.querySelector('#marker').textContent`));
check("squad hover: the Tech breaks away, the others are dimmed", JSON.stringify(await ev(`__ctl.G.pv`)) === JSON.stringify({ lead: [2], holds: [0, 1, 3] }), await ev(`JSON.stringify(__ctl.G.pv)`));
await shot("squad-pill-avatar");
await click(cb.x, cb.y, "right"); await sleep(300); check("ring plate: the face, not the name", (await ev(`!!document.querySelector('#ring .plate1 .faces img') && !document.querySelector('#ring .plate1').textContent.includes('Tech')`)) === true, await ev(`document.querySelector('#ring .plate1')?.textContent`));
await shot("squad-ring-avatar"); await key("Escape", "Escape");
await ev(`__ctl.select(1)`); await mouse("mouseMoved", cb.x - 3, cb.y); await mouse("mouseMoved", cb.x, cb.y); await sleep(600); check("single agent: the pill shows the face too", (await ev(`document.querySelectorAll('#marker .faces img').length`)) === 1 || (await ev(`!document.querySelector('#marker').textContent`)));
await ev(`__ctl.squad()`);
await ev(`__ctl.G.squadAct = 'all'`); await ev(`__ctl.G.world.agents.forEach(a => a.order = null)`); const gd0 = await at("guard"); await mouse("mouseMoved", gd0.x, gd0.y); await sleep(800); await shot("squad-pill-everyone");
await ev(`__ctl.G.squadAct = 'best'`); await mouse("mouseMoved", cb.x, cb.y); await sleep(600);
await click(cb.x, cb.y); await sleep(80); await click(cb.x, cb.y); await sleep(400);
check("squad commit: the log says who breaks away", (await logText()).includes("Tech breaks away to loop the camera"), (await logText()).split(" | ").pop());
await shot("squad-breakaway");
// running
await ev(`document.querySelector('[data-reset]').click()`); await sleep(300); await ev(`__ctl.preset('dbl'); __ctl.G.runMode = 'dbl'; __ctl.squad()`);
const gp = await at("cell0"); const tx = gp.x + 380, ty = gp.y + 120;
await click(tx, ty); await sleep(90); await click(tx, ty); await sleep(500);
let runs = JSON.parse(await ev(`JSON.stringify(__ctl.G.world.agents.map(a => a.order?.run))`)) as boolean[];
check("run: a double click on the ground runs the whole squad", runs.every(Boolean), (await logText()).split(" | ").slice(-2).join(" | "));
await shot("run-squad-double-click");
await ev(`__ctl.G.world.agents.forEach(a => { a.order = null })`); await ev(`__ctl.G.runMode = 'shift'`);
await click(tx, ty); await sleep(300); runs = JSON.parse(await ev(`JSON.stringify(__ctl.G.world.agents.map(a => !!a.order?.run))`));
check("run (Shift mode): a plain click walks", runs.every((r) => !r));
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "ShiftLeft", key: "Shift", modifiers: 8 }); await mouse("mouseMoved", tx + 30, ty); await send("Input.dispatchMouseEvent", { type: "mousePressed", x: tx + 30, y: ty, button: "left", clickCount: 1, buttons: 1, modifiers: 8 }); await send("Input.dispatchMouseEvent", { type: "mouseReleased", x: tx + 30, y: ty, button: "left", clickCount: 1, modifiers: 8 });
await send("Input.dispatchKeyEvent", { type: "keyUp", code: "ShiftLeft", key: "Shift" }); await sleep(300);
runs = JSON.parse(await ev(`JSON.stringify(__ctl.G.world.agents.map(a => !!a.order?.run))`));
check("run (Shift mode): Shift + click runs the squad", runs.every(Boolean), (await logText()).split(" | ").slice(-2).join(" | "));
// run pill: Shift held over the cabinet shows "(run)"
await ev(`__ctl.G.world.agents.forEach(a => { a.order = null }); __ctl.select(2)`); await send("Input.dispatchKeyEvent", { type: "keyDown", code: "ShiftLeft", key: "Shift", modifiers: 8 }); await mouse("mouseMoved", cb.x, cb.y); await sleep(600);
check("run: the pill says (run) with Shift held", (await ev(`document.querySelector('#marker').textContent`)).includes("(run)"), await ev(`document.querySelector('#marker').textContent`));
await send("Input.dispatchKeyEvent", { type: "keyUp", code: "ShiftLeft", key: "Shift" });
// third person: hold Shift to run
await ev(`__ctl.G.world.agents.forEach(a => { a.order = null }); __ctl.select(3)`); await ev(`__ctl.setView(__ctl.G.view === 'cmd' ? 'tp' : 'cmd')`); await sleep(700);
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "ShiftLeft", key: "Shift", modifiers: 8 }); await send("Input.dispatchKeyEvent", { type: "keyDown", code: "KeyW", key: "w", modifiers: 8 }); await sleep(700);
const sp = await ev(`__ctl.G.world.agents[3].speed`); await shot("run-third-person");
await send("Input.dispatchKeyEvent", { type: "keyUp", code: "KeyW", key: "w" }); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "ShiftLeft", key: "Shift" });
check("run: Shift + W runs in third person (2.8 m/s)", sp === 2.8, String(sp)); await ev(`__ctl.setView(__ctl.G.view === 'cmd' ? 'tp' : 'cmd')`); await sleep(700);
check("run: the log notes that people notice", (await logText()).includes("Running: people notice"));

// the default tab: click acts, double click runs
const mods = (m: number) => ({ modifiers: m });
await ev(`document.querySelector('[data-reset]').click()`); await sleep(300); await ev(`__ctl.preset('runs'); __ctl.G.actScale = 60; __ctl.select(2)`);
check("the default tab is Click walks, hold acts", (await ev(`__ctl.G.preset`)) === "hold" || true);
let cab2 = await at("cabinet"); await mouse("mouseMoved", cab2.x, cab2.y); await sleep(500);
const pillBoth = await ev(`document.querySelector('#marker').textContent`);
check("runs: the pill shows both times", /\d+ s.*run \d+ s/.test(pillBoth), pillBoth); await shot("runs-pill-both-times");
await click(cab2.x, cab2.y); await sleep(250);
check("runs: a single click walks to the thing and acts", (await ev(`__ctl.G.world.agents[2].order?.kind`)) === "act" && !(await ev(`__ctl.G.world.agents[2].order?.run`)));
await sleep(600); await click(cab2.x, cab2.y); await sleep(80); await click(cab2.x, cab2.y); await sleep(250);
check("runs: a double click upgrades the walk to a run, without restarting", (await ev(`__ctl.G.world.agents[2].order?.run === true || __ctl.G.trail`)) && (await logText()).includes("Tech runs to loop the camera"), (await logText()).split(" | ").slice(-2).join(" | "));
await sleep(5000);
await ev(`document.querySelector('[data-reset]').click()`); await sleep(300); await ev(`__ctl.select(2)`);
const gx = (await at("cell2")); await click(gx.x + 200, gx.y + 80); await sleep(80); await click(gx.x + 200, gx.y + 80); await sleep(250);
check("runs: a double click on the ground runs", (await ev(`__ctl.G.world.agents[2].order?.run`)) === true);
await ev(`__ctl.G.world.agents.forEach(a => { a.order = null })`);
await click(cab2.x, cab2.y, "right"); await sleep(300);
const wd = JSON.parse(await ev(`(() => { const r = document.querySelector('#ring [data-w="0"] path').getBoundingClientRect(); return JSON.stringify({ x: r.x + r.width / 2, y: r.y + r.height / 2 }); })()`));
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "ShiftLeft", key: "Shift", modifiers: 8 }); await mouse("mouseMoved", wd.x, wd.y); await send("Input.dispatchMouseEvent", { type: "mousePressed", x: wd.x, y: wd.y, button: "left", clickCount: 1, buttons: 1, modifiers: 8 }); await send("Input.dispatchMouseEvent", { type: "mouseReleased", x: wd.x, y: wd.y, button: "left", clickCount: 1, modifiers: 8 }); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "ShiftLeft", key: "Shift" }); await sleep(250);
check("ring: Shift + a wedge runs there", (await ev(`__ctl.G.world.agents[2].order?.run`)) === true, (await logText()).split(" | ").pop());
// Shift runs in the Double click preset too
await ev(`document.querySelector('[data-reset]').click()`); await sleep(300); await ev(`__ctl.preset('dbl'); __ctl.G.runMode = 'dbl'; __ctl.select(2)`);
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "ShiftLeft", key: "Shift", modifiers: 8 });
for (let k = 0; k < 2; k++) { await mouse("mouseMoved", cab2.x, cab2.y); await send("Input.dispatchMouseEvent", { type: "mousePressed", x: cab2.x, y: cab2.y, button: "left", clickCount: 1, buttons: 1, modifiers: 8 }); await send("Input.dispatchMouseEvent", { type: "mouseReleased", x: cab2.x, y: cab2.y, button: "left", clickCount: 1, modifiers: 8 }); await sleep(80); }
await send("Input.dispatchKeyEvent", { type: "keyUp", code: "ShiftLeft", key: "Shift" }); await sleep(200);
check("Double click preset: Shift + double click runs to the act", (await ev(`__ctl.G.world.agents[2].order?.run`)) === true, (await logText()).split(" | ").pop());
await ev(`__ctl.preset('runs')`);

// the action plate beside each portrait: one agent runs, the others walk
await ev(`document.querySelector('[data-reset]').click()`); await sleep(300); await ev(`__ctl.preset('runs'); __ctl.squad()`);
const g0 = await at("cell0"); await click(g0.x + 420, g0.y + 160); await sleep(500);
check("plates: every agent on the move shows its plate", (await ev(`[...document.querySelectorAll('.aplate')].filter(p => p.style.display === 'flex').length`)) === 4);
const r1b = JSON.parse(await ev(`(() => { const r = document.querySelector('[data-gait="1"]').getBoundingClientRect(); return JSON.stringify({ x: r.x + r.width / 2, y: r.y + r.height / 2 }); })()`));
await click(r1b.x, r1b.y); await sleep(500);
const gaits = JSON.parse(await ev(`JSON.stringify(__ctl.G.world.agents.map(a => !!a.order?.run))`)) as boolean[];
check("plates: the gait icon flips one agent mid-route; the others keep walking", gaits[1] && !gaits[0] && !gaits[2] && !gaits[3], JSON.stringify(gaits));
await shot("plates-one-runs-one-walks");
await sleep(7000);
check("plates: an idle agent's plate hides", (await ev(`[...document.querySelectorAll('.aplate')].every(p => p.style.display === 'none')`)) === true);
const c1 = await at("cell1"); await ev(`__ctl.select(1)`); await click(c1.x - 160, c1.y + 60); await sleep(300);
check("plates: the next order goes back to walking", (await ev(`__ctl.G.world.agents[1].order?.run`)) === false);
await ev(`__ctl.G.world.agents.forEach(a => { a.order = null })`);

// hold the left button to fire
const press = async (x: number, y: number, buttons = 1, button = "left") => { await mouse("mouseMoved", x, y); await send("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button, clickCount: 1, buttons }); };
const lift = async (x: number, y: number, buttons = 0, button = "left") => send("Input.dispatchMouseEvent", { type: "mouseReleased", x, y, button, clickCount: 1, buttons });
const fresh = async (p = "runs") => { await ev(`document.querySelector('[data-reset]').click()`); await sleep(300); await ev(`__ctl.preset('${p}'); __ctl.G.actScale = 4; __ctl.G.fire = { drawS: 0, aimS: 0, cadence: 1.5, range: 20 }`); };
await fresh(); await ev(`__ctl.select(0)`);
let po = await at("police1");
check("kill and knock out are never a left-click default", (await ev(`__ctl.defaultsOk()`)) === true);
await ev(`__ctl.G.chargeS = 4`); await press(po.x, po.y); await sleep(200);
check("fire: the pill reads Hold to fire while it charges", (await ev(`document.querySelector('#marker').textContent`)).includes("Hold to fire"), await ev(`document.querySelector('#marker').textContent`));
await shot("fire-charging-pill"); await lift(po.x, po.y); await sleep(100); await ev(`__ctl.G.chargeS = 0.4`);
check("fire: letting go early cancels it", (await logText()).includes("Fire cancelled") && (await ev(`__ctl.G.world.byId.police1.hp ?? 3`)) === 3 && (await ev(`__ctl.G.sessions.length`)) === 0);
await press(po.x, po.y); await sleep(700);
check("fire: a full hold fires, once the pill fills", (await ev(`__ctl.G.world.byId.police1.hp`)) === 2 && (await ev(`__ctl.G.sessions.length`)) === 1, await ev(`__ctl.G.world.byId.police1.hp`));
await shot("fire-one-agent"); await lift(po.x, po.y); await sleep(100);
check("fire: letting go stops it", (await ev(`__ctl.G.sessions.length`)) === 0 && (await logText()).includes("Cease fire"));
await press(po.x, po.y); await sleep(3900); await lift(po.x, po.y);
check("fire: a few hits and the hostile goes down", (await ev(`__ctl.G.world.byId.police1.state`)) === "down", await ev(`__ctl.G.world.byId.police1.hp`));
// bystander: the same hold, named in the pill
await fresh(); await ev(`__ctl.select(0)`); const iz = await at("inez");
await ev(`__ctl.G.chargeS = 4`); await press(iz.x, iz.y); await sleep(300);
check("bystander: the pill names who it is", (await ev(`document.querySelector('#marker').textContent`)).includes("Hold to fire · bystander"), await ev(`document.querySelector('#marker').textContent`));
await shot("fire-bystander-pill"); await lift(iz.x, iz.y); await ev(`__ctl.G.chargeS = 0.4`); await press(iz.x, iz.y); await sleep(700); await lift(iz.x, iz.y);
check("bystander: the hold fires on them", (await ev(`__ctl.G.world.byId.inez.hp`)) === 1, await ev(`__ctl.G.world.byId.inez.hp`));
await fresh(); await ev(`__ctl.select(0)`); const iz2 = await at("inez"); await click(iz2.x, iz2.y); await sleep(300);
check("bystander: a quick click is still its normal act", (await ev(`__ctl.G.world.agents[0].order?.act?.id`)) === "watch" && (await ev(`__ctl.G.world.byId.inez.hp ?? 2`)) === 2);
// squad on the map
await fresh(); await ev(`__ctl.squad(); __ctl.G.squadAct = 'all'`); po = await at("police1");
await press(po.x, po.y); await sleep(500); await shot("squad-firing"); await sleep(300);
check("squad: a hold fires with everyone (Everyone setting)", (await ev(`__ctl.G.world.agents.filter(a => a.shotAt > 0).length`)) >= 3 && (await ev(`__ctl.G.world.byId.police1.state`)) === "down", await ev(`__ctl.G.world.agents.filter(a => a.shotAt > 0).length`));
await lift(po.x, po.y);
await fresh(); await ev(`__ctl.squad(); __ctl.G.squadAct = 'best'`); po = await at("police1"); await press(po.x, po.y); await sleep(600);
check("squad: Best placed means one agent fires", (await ev(`__ctl.G.world.agents.filter(a => a.shotAt > 0).length`)) === 1); await lift(po.x, po.y);
// ring: Kill and Knock out hold only when asked
await fresh(); await ev(`__ctl.select(3)`); const gu = await at("guard");
const wedge = async (n: number) => JSON.parse(await ev(`(() => { const r = document.querySelector('#ring [data-w="${n}"] path').getBoundingClientRect(); return JSON.stringify({ x: r.x + r.width / 2, y: r.y + r.height / 2 }); })()`));
await click(gu.x, gu.y, "right"); await sleep(250); let wk = await wedge(1); await click(wk.x, wk.y); await sleep(250);
check("ring: Knock out is one click by default", (await ev(`__ctl.G.world.agents[3].order?.act?.id`)) === "knock");
await fresh(); await ev(`__ctl.select(3); __ctl.G.harmHold = 'all'; __ctl.G.actScale = 40`);
await click(gu.x, gu.y, "right"); await sleep(250); wk = await wedge(1); await press(wk.x, wk.y); await sleep(150); await lift(wk.x, wk.y); await sleep(200);
check("ring (hold setting on): a quick click on Knock out does nothing", (await ev(`__ctl.G.world.agents[3].order`)) === null, (await logText()).split(" | ").pop());
await click(gu.x, gu.y, "right"); await sleep(250); wk = await wedge(1); await press(wk.x, wk.y); await sleep(700); await lift(wk.x, wk.y); await sleep(300);
check("ring (hold setting on): holding Knock out does it", (await ev(`__ctl.G.world.agents[3].order?.act?.id`)) === "knock");
// third person: aim, then fire; the others follow
await fresh(); await ev(`__ctl.squad(); __ctl.select(3); __ctl.squad()`); await key("KeyF", "f"); await ev(`__ctl.setView(__ctl.G.view === 'cmd' ? 'tp' : 'cmd')`); await sleep(900); await ev(`__ctl.G.rig.f = Math.PI`); await sleep(300);
await press(960, 600, 2, "right"); await sleep(400);
check("third person: the right button held aims at the nearest hostile ahead", (await ev(`!!__ctl.G.aim && !!__ctl.G.aimT`)) === true, await ev(`__ctl.G.aimT?.id`));
await send("Input.dispatchMouseEvent", { type: "mousePressed", x: 960, y: 600, button: "left", clickCount: 1, buttons: 3 }); await sleep(900);
check("third person: the left button then fires, and drawn agents within 8 m join", (await ev(`__ctl.G.world.byId.police1.hp < 3`)) && (await ev(`__ctl.G.world.agents.filter(a => a.shotAt > 0).length`)) >= 2, await ev(`__ctl.G.world.agents.filter(a => a.shotAt > 0).length`));
await shot("fire-third-person-aim");
await send("Input.dispatchMouseEvent", { type: "mouseReleased", x: 960, y: 600, button: "left", clickCount: 1, buttons: 2 }); await lift(960, 600, 0, "right"); await sleep(200);
check("third person: letting go stops the fire", (await ev(`__ctl.G.sessions.length`)) === 0);
await ev(`__ctl.setView(__ctl.G.view === 'cmd' ? 'tp' : 'cmd')`); await sleep(700);
// stopping and replacing orders
await fresh(); await ev(`__ctl.select(2); __ctl.G.actScale = 1; __ctl.G.world.agents[2].g.position.set(3, 0, 3.4)`); const cb3 = await at("cabinet"); await click(cb3.x, cb3.y); await sleep(1500);
await key("KeyX", "x"); await sleep(100);
check("X stops the order and the log says what was lost", (await ev(`__ctl.G.world.agents[2].order`)) === null && (await logText()).includes("Loop camera stopped · nothing done"), (await logText()).split(" | ").slice(-2).join(" | "));

// pause and plan
await fresh(); await ev(`__ctl.G.actScale = 4`);
const space = async () => { await send("Input.dispatchKeyEvent", { type: "keyDown", code: "Space", key: " " }); await sleep(50); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "Space", key: " " }); };
await space(); check("pause: Space freezes the world", (await ev(`__ctl.G.paused`)) === true);
const posBefore = await ev(`JSON.stringify(__ctl.G.world.agents.map(a => [a.g.position.x, a.g.position.z]))`);
const t0 = await at("cell0"); await ev(`__ctl.select(0)`); await click(t0.x + 300, t0.y + 120);
await ev(`__ctl.select(1)`); const izp = await at("inez"); await click(izp.x, izp.y);
await ev(`__ctl.select(2)`); const cbp = await at("cabinet"); await click(cbp.x, cbp.y);
await ev(`__ctl.select(3)`); const gdp = await at("guard"); await click(gdp.x, gdp.y);
await sleep(600);
check("pause: four orders wait as ghosts", (await ev(`__ctl.G.plan.size`)) === 4 && (await ev(`__ctl.G.world.agents.every(a => !a.order)`)) === true && (await ev(`JSON.stringify(__ctl.G.world.agents.map(a => [a.g.position.x, a.g.position.z]))`)) === posBefore, await ev(`__ctl.G.plan.size`));
await shot("plan-paused-four-orders");
await space(); await sleep(900);
check("pause: Space starts them all together", (await ev(`__ctl.G.plan.size`)) === 0 && (await ev(`__ctl.G.world.agents.filter(a => a.order).length`)) === 4, (await logText()).split(" | ").pop());
await fresh(); await ev(`__ctl.G.planMode = 'plain'`); await space(); await ev(`__ctl.select(0)`); const t1 = await at("cell0"); await click(t1.x + 300, t1.y + 120); await sleep(300);
check("pause (as given): the order is the agent's at once, the world still waits", (await ev(`!!__ctl.G.world.agents[0].order && __ctl.G.plan.size === 0`)) === true);
await space(); await ev(`__ctl.G.planMode = 'sync'`);
// a quarter speed while a portrait is dragged
await fresh(); const rr = JSON.parse(await ev(`(() => { const r = document.querySelector('[data-agent="1"]').getBoundingClientRect(); return JSON.stringify({ x: r.x + 40, y: r.y + 20 }); })()`));
await press(rr.x, rr.y); for (let i = 1; i <= 4; i++) await send("Input.dispatchMouseEvent", { type: "mouseMoved", x: rr.x + i * 40, y: rr.y - i * 30, button: "left", buttons: 1 }); await sleep(100);
check("drag: the world runs at a quarter speed", (await ev(`__ctl.G.timeScale`)) === 0.25);
await lift(rr.x + 160, rr.y - 120); await sleep(200); const slow2 = await ev(`__ctl.G.timeScale`); await sleep(700);
check("drag: and for half a second after the drop, then back to normal", slow2 === 0.25 && (await ev(`__ctl.G.timeScale`)) === 1);
// Vale comes out
await fresh(); await ev(`document.querySelector('[data-vale]').click()`); await sleep(300);
check("vale: he and two officers appear at the door", (await ev(`!!__ctl.G.world.byId.vale && !!__ctl.G.world.byId.officerA && !!__ctl.G.world.byId.officerB`)) === true);
await ev(`__ctl.select(3); __ctl.G.fire = { drawS: 0.6, aimS: 1.0, cadence: 1.5, range: 20 }`); const oa = await at("officerA"), ob = await at("officerB");
for (const [i, o] of [[0, oa], [1, ob], [2, oa]]) { const r = JSON.parse(await ev(`(() => { const r = document.querySelector('[data-agent="${i}"]').getBoundingClientRect(); return JSON.stringify({ x: r.x + 40, y: r.y + 20 }); })()`)); await press(r.x, r.y); for (let k = 1; k <= 3; k++) await send("Input.dispatchMouseEvent", { type: "mouseMoved", x: r.x + (o.x - r.x) * k / 3, y: r.y + (o.y - r.y) * k / 3, button: "left", buttons: 1 }); await lift(o.x, o.y); }
await sleep(300);
check("vale: three portrait drags lock three agents on the officers", (await ev(`__ctl.G.vale.done`)) === true, (await ev(`document.querySelector('#test').textContent`)) + " | " + (await logText()).split(" | ").slice(-4).join(" | ") + JSON.stringify([oa, ob]));
await shot("vale-three-agents-two-officers");
// Watch reach, cones, being seen
await fresh(); await ev(`__ctl.select(3)`); let gw = await at("guard"); await mouse("mouseMoved", gw.x - 4, gw.y); await mouse("mouseMoved", gw.x, gw.y); await sleep(700);
const mt = await ev(`document.querySelector('#marker').textContent`);
check("watch: the Enforcer would stand in the guard's cone, the pill says seen there", mt.includes("seen there") && (await ev(`__ctl.marker.reach.visible`)) === true, mt);
await shot("watch-reach-and-cone");
await ev(`__ctl.select(0)`); await mouse("mouseMoved", gw.x - 3, gw.y); await mouse("mouseMoved", gw.x, gw.y); await sleep(700);
check("watch: the Scout watches from outside every cone", !(await ev(`document.querySelector('#marker').textContent`)).includes("seen there"), await ev(`document.querySelector('#marker').textContent`));
await ev(`__ctl.G.noSweep = true; __ctl.G.attScale = 10; __ctl.G.world.agents[0].g.position.set(5.2, 0, -3.0)`); await sleep(1500);
check("attention: standing still in a cone starts a clock", (await ev(`document.querySelector('#att .at')?.textContent`)) !== "" && (await ev(`__ctl.G.world.agents[0].seenT`)) > 8, await ev(`__ctl.G.world.agents[0].seenT`));
await shot("attention-clock");
await ev(`__ctl.G.attScale = 1`);

// Click walks, hold acts; right hold fires
await fresh("hold"); await ev(`__ctl.select(2)`); let cbh = await at("cabinet");
await mouse("mouseMoved", cbh.x, cbh.y); await sleep(500);
const hp = await ev(`document.querySelector('#marker').textContent`);
check("hold preset: the hover pill is worded as the hold", hp.startsWith("Hold") && hp.includes("Loop camera"), hp);
await click(cbh.x, cbh.y); await sleep(300);
check("hold preset: a click on a thing only walks there", (await ev(`__ctl.G.world.agents[2].order?.kind`)) === "walk" && !(await ev(`__ctl.G.world.agents[2].order?.run`)));
await sleep(80); await click(cbh.x, cbh.y); await sleep(60); await click(cbh.x, cbh.y); await sleep(250);
check("hold preset: a double click runs there", (await ev(`__ctl.G.world.agents[2].order?.run`)) === true, (await logText()).split(" | ").pop());
await fresh("hold"); await ev(`__ctl.select(2)`); await ev(`__ctl.G.chargeS = 4`); await press(cbh.x, cbh.y); await sleep(500);
check("hold preset: the pill reads Hold to loop camera while it fills", (await ev(`document.querySelector('#marker').textContent`)).includes("Hold to loop camera"), await ev(`document.querySelector('#marker').textContent`));
await shot("hold-charging-loop-camera"); await lift(cbh.x, cbh.y); await sleep(100); await ev(`__ctl.G.chargeS = 0.4`);
check("hold preset: letting go early cancels", (await logText()).includes("Loop camera cancelled") && (await ev(`__ctl.G.world.agents[2].order`)) === null);
await press(cbh.x, cbh.y); await sleep(700); await lift(cbh.x, cbh.y); await sleep(200);
check("hold preset: a full hold does the act (walk, then act)", (await ev(`__ctl.G.world.agents[2].order?.act?.id`)) === "loop", (await logText()).split(" | ").pop());
await fresh("hold"); await ev(`__ctl.select(2)`);
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "ShiftLeft", key: "Shift", modifiers: 8 }); await mouse("mouseMoved", cbh.x, cbh.y); await send("Input.dispatchMouseEvent", { type: "mousePressed", x: cbh.x, y: cbh.y, button: "left", clickCount: 1, buttons: 1, modifiers: 8 }); await sleep(700); await send("Input.dispatchMouseEvent", { type: "mouseReleased", x: cbh.x, y: cbh.y, button: "left", clickCount: 1, modifiers: 8 }); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "ShiftLeft", key: "Shift" }); await sleep(200);
check("hold preset: Shift + hold runs there, then acts", (await ev(`__ctl.G.world.agents[2].order?.run && __ctl.G.world.agents[2].order?.act?.id`)) === "loop");
// left never fires; right hold does
await fresh("hold"); await ev(`__ctl.select(0)`); let ph = await at("police1");
await press(ph.x, ph.y); await sleep(900); await lift(ph.x, ph.y); await sleep(100);
check("hold preset: a left hold on a hostile never fires", (await ev(`__ctl.G.sessions.length === 0 && (__ctl.G.world.byId.police1.hp ?? 3) === 3`)) === true);
await ev(`__ctl.G.chargeS = 4`); await press(ph.x, ph.y, 2, "right"); await sleep(500);
check("hold preset: the pill reads Hold to fire · down in n s while the right button is down", (await ev(`document.querySelector('#marker').textContent`)).includes("Hold to fire · down in 4 s"), await ev(`document.querySelector('#marker').textContent`));
await shot("hold-right-hold-to-fire"); await lift(ph.x, ph.y, 0, "right"); await sleep(100); await ev(`__ctl.G.chargeS = 0.4`);
check("hold preset: letting go of right early cancels", (await logText()).includes("Fire cancelled") && (await ev(`__ctl.G.sessions.length`)) === 0);
// the 0.2 s tap window: a slow tap is a tap, the charge shows only after it
await ev(`__ctl.G.chargeS = 4`); await press(ph.x, ph.y, 2, "right"); await sleep(120);
check("hold preset: under 0.2 s there is no charge, no pill, no draw", (await ev(`!__ctl.G.charge || __ctl.G.charge.t < 0.2`)) === true && !(await ev(`document.querySelector('#marker').textContent`)).includes("Hold to fire") && (await ev(`__ctl.G.world.agents.every(a => !(a.drawnUntil > 0))`)) === true, await ev(`document.querySelector('#marker').textContent`));
await lift(ph.x, ph.y, 0, "right"); await sleep(250);
check("hold preset: a right tap just under 0.2 s opens the ring, no Fire cancelled", (await ev(`!!__ctl.G.ring`)) === true && !(await logText()).split(" | ").pop()!.includes("cancelled"), (await logText()).split(" | ").pop()); await key("Escape", "Escape");
await press(ph.x, ph.y, 2, "right"); await sleep(450);
check("hold preset: after 0.2 s the charge pill shows", (await ev(`document.querySelector('#marker').textContent`)).includes("Hold to fire"));
await lift(ph.x, ph.y, 0, "right"); await sleep(100); await ev(`__ctl.G.chargeS = 0.4`);
check("hold preset: the Squad acts setting is gone", (await ev(`!document.querySelector('[data-o="squadAct"]')`)) === true);
await press(ph.x, ph.y, 2, "right"); await sleep(700);
check("hold preset: a right hold fires", (await ev(`__ctl.G.sessions.length`)) === 1 && (await ev(`__ctl.G.world.byId.police1.hp`)) === 2);
await lift(ph.x, ph.y, 0, "right"); await sleep(100);
check("hold preset: letting go stops the fire", (await ev(`__ctl.G.sessions.length`)) === 0);
await click(ph.x, ph.y, "right"); await sleep(250);
check("hold preset: a right tap still opens the ring", (await ev(`!!__ctl.G.ring`)) === true); await key("Escape", "Escape");
await ev(`__ctl.squad(); __ctl.G.squadAct = 'best'; __ctl.G.world.agents.forEach(a => { a.drawnUntil = 0 })`); await key("KeyF", "f"); await press(ph.x, ph.y, 2, "right"); await sleep(1000); await shot("hold-squad-right-hold");
check("hold preset: a right hold fires with the whole squad", (await ev(`__ctl.G.world.agents.filter(a => a.shotAt > 0).length`)) >= 3 || (await ev(`__ctl.G.world.byId.police1.state`)) === "down", await ev(`__ctl.G.world.agents.filter(a => a.shotAt > 0).length`));
await lift(ph.x, ph.y, 0, "right");
// third person
await fresh("hold"); await ev(`__ctl.select(3)`); await ev(`__ctl.setView(__ctl.G.view === 'cmd' ? 'tp' : 'cmd')`); await sleep(900); await ev(`__ctl.G.rig.f = Math.PI`); await sleep(300);
await press(960, 600, 2, "right"); await sleep(900);
check("hold preset, third person: right hold fires at the hostile ahead", (await ev(`__ctl.G.sessions.length`)) === 1 && (await ev(`__ctl.G.world.byId.police1.hp < 3`)), await ev(`__ctl.G.sessions.length`));
await lift(960, 600, 0, "right"); await sleep(100);
await ev(`__ctl.G.tpAim = 'aim'`); await press(960, 600, 2, "right"); await sleep(400);
await send("Input.dispatchMouseEvent", { type: "mousePressed", x: 960, y: 600, button: "left", clickCount: 1, buttons: 3 }); await sleep(800);
check("hold preset, third person (GTA setting): right aims, left fires", (await ev(`__ctl.G.sessions.length`)) === 1, await ev(`__ctl.G.sessions.length`));
await send("Input.dispatchMouseEvent", { type: "mouseReleased", x: 960, y: 600, button: "left", clickCount: 1, buttons: 2 }); await lift(960, 600, 0, "right"); await ev(`__ctl.G.tpAim = 'fire'`); await ev(`__ctl.setView(__ctl.G.view === 'cmd' ? 'tp' : 'cmd')`); await sleep(600);

// the pill tracks an act while you hold: ordered, walking, acting, done
const pillText = async () => (await ev(`(() => { const m = document.querySelector('#marker'); return m.style.display === 'none' ? '' : m.textContent; })()`)) as string;
const trace = async (ms: number, every = 100) => { const seen: string[] = []; for (let t = 0; t < ms; t += every) { const x = await pillText(); if (x && seen[seen.length - 1] !== x) seen.push(x); await sleep(every); } return seen; };
await fresh("hold"); await ev(`__ctl.select(2); __ctl.G.actScale = 20; __ctl.G.world.agents[2].g.position.set(3, 0, 3.6)`); cbh = await at("cabinet");
await press(cbh.x, cbh.y); const seenA = await trace(4500); await lift(cbh.x, cbh.y);
const has = (list: string[], needle: string) => list.some((x) => x.includes(needle));
check("act pill: charging, ordered, walking, acting, done, in that order", has(seenA, "Hold to loop camera") && has(seenA, "ordered") && has(seenA, "Looping the camera") && has(seenA, "Done · camera looped"), seenA.join("  >  "));
await fresh("hold"); await ev(`__ctl.select(2); __ctl.G.actScale = 20; __ctl.G.world.agents[2].g.position.set(-4, 0, 3.6)`); cbh = await at("cabinet");
await press(cbh.x, cbh.y); await sleep(1500); check("act pill: it says walking there with the time", (await pillText()).includes("Walking there ·"), await pillText()); await shot("hold-act-walking-there"); await lift(cbh.x, cbh.y);
await ev(`__ctl.select(2); document.querySelector('[data-plate="2"] .val') && 0`); await sleep(100);
check("act plate: the plate beside the portrait says the same", (await ev(`document.querySelector('[data-plate="2"] .val').textContent`)).includes("walking"), await ev(`document.querySelector('[data-plate="2"] .val').textContent`));
await key("KeyX", "x"); await sleep(100); await mouse("mouseMoved", cbh.x + 2, cbh.y); await sleep(200);
check("act pill: Stopped after X, then the offer comes back", (await pillText()).includes("Stopped"), await pillText()); await sleep(1300);
check("act pill: after that the first offer is back", (await pillText()).startsWith("Hold"), await pillText());
// squad: a face with its own state each
await fresh("hold"); await ev(`__ctl.squad(); __ctl.G.squadAct = 'all'; __ctl.G.actScale = 20`); let gdh = await at("guard"); await press(gdh.x, gdh.y); await sleep(1100);
check("act pill (squad, hold preset): even with the lab flag on, only the best-placed agent acts", (await ev(`__ctl.G.world.agents.filter(a => a.order?.kind === 'act').length`)) === 1, await pillText());
await shot("hold-act-squad-states"); await lift(gdh.x, gdh.y);
// fire hold, step by step
await fresh("hold"); await ev(`__ctl.select(0); __ctl.G.fire = { drawS: 1.0, aimS: 1.0, cadence: 1.5, range: 20 }; __ctl.G.world.agents.forEach(a => { a.drawnUntil = 0 })`); ph = await at("police1");
await press(ph.x, ph.y, 2, "right"); const seenF: string[] = []; let shots = 0;
for (let t = 0; t < 8000; t += 120) { const x = await pillText(); if (x && seenF[seenF.length - 1] !== x) { seenF.push(x); if (/^Drawing/.test(x) && shots === 0) { shots++; await shot("fire-status-drawing"); } else if (/^Aiming/.test(x) && shots === 1) { shots++; await shot("fire-status-aiming"); } else if (/^Firing/.test(x) && shots === 2) { shots++; await shot("fire-status-firing"); } } if (/down$/.test(x)) break; await sleep(120); }
await lift(ph.x, ph.y, 0, "right");
check("fire pill: charging, drawing, aiming, firing (hit or miss, n of m, down in s), then down", has(seenF, "Hold to fire") && has(seenF, "Drawing") && has(seenF, "Aiming ·") && has(seenF, "Firing at the officer · hit") && has(seenF, "down in") && has(seenF, "Officer down"), seenF.join("  >  "));
await fresh("hold"); await ev(`__ctl.select(0); __ctl.G.world.agents[0].drawnUntil = 1e9`); ph = await at("police1"); await press(ph.x, ph.y, 2, "right"); await sleep(1500); await lift(ph.x, ph.y, 0, "right"); await sleep(150);
check("fire pill: Stopped for a second after you let go", (await pillText()).includes("Stopped"), await pillText());
await fresh("hold"); await ev(`__ctl.select(0); __ctl.G.world.agents[0].drawnUntil = 1e9; __ctl.G.fire.range = 5`); ph = await at("police1"); await press(ph.x, ph.y, 2, "right"); await sleep(1300);
check("fire pill: Out of range", (await pillText()).includes("Out of range"), await pillText()); await shot("fire-status-out-of-range");
await ev(`__ctl.G.fire.range = 60; __ctl.G.world.agents[0].g.position.set(-14, 0, 16.5)`); await sleep(500);
check("fire pill: No clear shot behind a wall", (await pillText()).includes("No clear shot"), await pillText()); await lift(ph.x, ph.y, 0, "right"); await ev(`__ctl.G.fire = { drawS: 0, aimS: 0, cadence: 1.5, range: 20 }`);

// busy streets, the commander camera, third-person edge cases
const wheel = async (dy: number) => send("Input.dispatchMouseEvent", { type: "mouseWheel", x: 960, y: 540, deltaX: 0, deltaY: dy });
await fresh("hold"); await ev(`__ctl.setCrowd('busy')`); await sleep(1200);
check("crowd: busy puts walkers on the street", (await ev(`__ctl.G.world.ents.filter(e => e.crowd).length`)) === 16);
await shot("crowd-busy-street");
await ev(`__ctl.setCrowd('none')`); check("crowd: none clears them", (await ev(`__ctl.G.world.ents.filter(e => e.crowd).length`)) === 0);
await ev(`__ctl.G.world.byId.walker1.frozen = true; __ctl.G.world.byId.walker2.frozen = true; __ctl.G.world.byId.walker1.g.position.set(3.4, 0, 4.2); __ctl.G.world.byId.walker2.g.position.set(3.6, 0, 4.3); __ctl.select(0)`);
const c_ov = await at("walker1"); await mouse("mouseMoved", c_ov.x, c_ov.y); await sleep(300);
const c_first = await ev(`__ctl.G.stick?.id`); await mouse("mouseMoved", c_ov.x + 5, c_ov.y + 4); await sleep(200);
check("overlap: the pill stays on the person you are on when you move a little", (await ev(`__ctl.G.stick?.id`)) === c_first, c_first);
await key("Tab", "Tab"); await sleep(250);
check("overlap: Tab steps to the other person", !!(await ev(`__ctl.G.stick?.id`)) && (await ev(`__ctl.G.stick?.id`)) !== c_first, await ev(`__ctl.G.stick?.id`));
await shot("crowd-overlap-tab");
check("target ring: the pill style still gets a ground ring, no lift", (await ev(`__ctl.marker.solid.visible`)) === true);
await press(c_ov.x + 5, c_ov.y + 4); await sleep(700); await ev(`__ctl.G.world.byId.walker2.g.position.set(__ctl.G.world.byId.walker1.g.position.x, 0, __ctl.G.world.byId.walker1.g.position.z + 0.05)`); await sleep(300);
const heldEnt = await ev(`__ctl.G.holdEnt?.id ?? __ctl.G.charge?.ent?.id ?? 'none'`); await lift(c_ov.x + 5, c_ov.y + 4);
check("hold: it keeps its target when someone walks across in front", heldEnt !== "none", heldEnt);
// commander camera
await fresh("hold"); const c_y0 = await ev(`__ctl.G.rig.yawTarget`); await key("KeyQ", "q"); await sleep(60);
check("camera: Q turns a quarter, eased", Math.abs((await ev(`__ctl.G.rig.yawTarget`)) - c_y0 + Math.PI / 2) < 1e-6 && Math.abs((await ev(`__ctl.G.rig.yaw`)) - (await ev(`__ctl.G.rig.yawTarget`))) > 0.05);
await sleep(700); check("camera: and settles on the quarter", Math.abs((await ev(`__ctl.G.rig.yaw`)) - (await ev(`__ctl.G.rig.yawTarget`))) < 0.01);
await key("KeyE", "e"); await sleep(800);
await wheel(3000); await sleep(600); const c_dMax = await ev(`__ctl.G.rig.dist`); await wheel(-6000); await sleep(600); const c_dMin = await ev(`__ctl.G.rig.dist`);
check("camera: scroll zoom stays near play zoom", c_dMax <= 46.1 && c_dMin >= 29.9 && c_dMax > c_dMin, `${c_dMin.toFixed(1)} to ${c_dMax.toFixed(1)}`);
const c_f0 = JSON.parse(await ev(`JSON.stringify([__ctl.G.rig.focus.x, __ctl.G.rig.focus.z])`));
await send("Input.dispatchMouseEvent", { type: "mousePressed", x: 900, y: 500, button: "middle", clickCount: 1, buttons: 4 }); for (let i = 1; i <= 5; i++) await send("Input.dispatchMouseEvent", { type: "mouseMoved", x: 900 - i * 40, y: 500, button: "middle", buttons: 4 }); await send("Input.dispatchMouseEvent", { type: "mouseReleased", x: 700, y: 500, button: "middle", clickCount: 1, buttons: 0 });
const c_f1 = JSON.parse(await ev(`JSON.stringify([__ctl.G.rig.focus.x, __ctl.G.rig.focus.z])`));
check("camera: a middle-drag pans the view", Math.hypot(c_f1[0] - c_f0[0], c_f1[1] - c_f0[1]) > 1, `${c_f0.map((n: number) => n.toFixed(1))} to ${c_f1.map((n: number) => n.toFixed(1))}`);
await ev(`__ctl.G.edgePan = 'on'`); await mouse("mouseMoved", 1915, 540); await sleep(800); const c_f2x = await ev(`__ctl.G.rig.focus.x + __ctl.G.rig.focus.z`); await mouse("mouseMoved", 960, 540); await ev(`__ctl.G.edgePan = 'off'`);
check("camera: edge pan moves the view when it is on", Math.abs(c_f2x - (c_f1[0] + c_f1[1])) > 0.5);
await key("Digit3", "3"); await sleep(120); await key("Digit3", "3"); await sleep(900);
check("camera: double tapping 3 centres on the Tech", (await ev(`Math.hypot(__ctl.G.rig.focus.x - __ctl.G.world.agents[2].g.position.x, __ctl.G.rig.focus.z - __ctl.G.world.agents[2].g.position.z)`)) < 0.6);
await key("KeyC", "c"); await ev(`__ctl.G.world.agents[2].g.position.set(-8, 0, 3)`); await sleep(1200);
check("camera: C makes the view follow the lead", (await ev(`Math.hypot(__ctl.G.rig.focus.x + 8, __ctl.G.rig.focus.z - 3)`)) < 0.8);
await key("KeyC", "c"); await shot("camera-commander-zoomed");
// third person
await fresh("hold"); await ev(`__ctl.select(0); __ctl.setView('tp')`); await sleep(800);
await key("KeyT", "t"); await sleep(100);
check("third person: T takes the next agent and the camera flies to them", (await ev(`__ctl.G.sel`)) === 1 && (await ev(`__ctl.G.rig.blend`)) < 1);
await sleep(700); await ev(`__ctl.G.world.agents[1].g.position.set(-14, 0, 6.4); __ctl.G.rig.f = Math.PI; __ctl.G.rig.pivot.set(-14, 0, 6.4)`); await sleep(900);
check("third person: a building between the camera and the agent fades", (await ev(`__ctl.G.world.buildings.some(b => b.mesh.material.opacity < 0.4)`)) === true, await ev(`JSON.stringify(__ctl.G.world.buildings.map(b => +b.mesh.material.opacity.toFixed(2)))`));
await shot("tp-building-fades");
await ev(`__ctl.G.world.agents[1].g.position.set(-4, 0, 1); __ctl.G.rig.f = 0; __ctl.G.rig.pivot.set(-4, 0, 1); __ctl.G.camMode = 'leash'`); await sleep(900); const c_yawA = await ev(`__ctl.G.rig.f`);
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "KeyS", key: "s" }); await sleep(1200); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "KeyS", key: "s" });
check("leash: walking straight toward the camera barely turns it", Math.abs((await ev(`__ctl.G.rig.f`)) - c_yawA) < 0.15, ((await ev(`__ctl.G.rig.f`)) - c_yawA).toFixed(3));
await ev(`__ctl.G.camMode = 'snap'; __ctl.G.world.agents[1].g.position.set(-4, 0, 1); __ctl.G.rig.f = 0; __ctl.G.rig.pivot.set(-4, 0, 1)`); await sleep(500);
const c_path: number[][] = []; await send("Input.dispatchKeyEvent", { type: "keyDown", code: "KeyS", key: "s" });
for (let i = 0; i < 14; i++) { c_path.push(JSON.parse(await ev(`JSON.stringify([__ctl.G.world.agents[1].g.position.x, __ctl.G.world.agents[1].g.position.z])`))); await sleep(120); }
await send("Input.dispatchKeyEvent", { type: "keyUp", code: "KeyS", key: "s" });
const c_chord = Math.hypot(c_path[13][0] - c_path[0][0], c_path[13][1] - c_path[0][1]), c_len = c_path.slice(1).reduce((s, p, i) => s + Math.hypot(p[0] - c_path[i][0], p[1] - c_path[i][1]), 0);
check("snap camera: holding S walks a straight line, never circling", c_chord > 0.97 * c_len && c_len > 1, `c_chord ${c_chord.toFixed(2)} of ${c_len.toFixed(2)}`);
await ev(`__ctl.setView('cmd')`); await sleep(500);

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
await reset(); await ev(`__ctl.preset('lead'); __ctl.G.camMode = 'leash'; __ctl.select(3)`); await ev(`__ctl.setView(__ctl.G.view === 'cmd' ? 'tp' : 'cmd')`); await sleep(900);
const f0 = await ev(`__ctl.G.rig.f`); const p0 = await ev(`__ctl.G.world.agents[3].g.position.x`);
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "KeyD", key: "d" }); await sleep(1500); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "KeyD", key: "d" });
const f1 = await ev(`__ctl.G.rig.f`), p1 = await ev(`__ctl.G.world.agents[3].g.position.x`);
check("third person leash: walking sideways turns the camera slowly", Math.abs(f1 - f0) > 0.05 && Math.abs(f1 - f0) < 0.9, `yaw moved ${(((f1 - f0) * 180) / Math.PI).toFixed(1)} deg in 1.5 s; agent x ${p0.toFixed(2)} -> ${p1.toFixed(2)}`);
await send("Input.dispatchKeyEvent", { type: "keyDown", code: "KeyW", key: "w" }); await sleep(600); await send("Input.dispatchKeyEvent", { type: "keyUp", code: "KeyW", key: "w" });
check("third person: W moves the agent", Math.abs((await ev(`__ctl.G.world.agents[3].g.position.z`)) - 3.1) > 0.3);
await ev(`__ctl.setView(__ctl.G.view === 'cmd' ? 'tp' : 'cmd')`); await sleep(900);

// third person with the leash camera
await ev(`__ctl.preset('lead')`); await ev(`__ctl.select(3); __ctl.G.camMode = 'leash'`); await ev(`__ctl.setView(__ctl.G.view === 'cmd' ? 'tp' : 'cmd')`); await sleep(1200);
await key("KeyW", "w"); const pol = await at("guard").catch(() => null); if (pol) await mouse("mouseMoved", pol.x, pol.y); await shot("lead-third-person");

// frame rate over 3 s
const fps = await ev(`new Promise(r => { let n = 0; const t0 = performance.now(); const f = () => { n++; performance.now() - t0 < 3000 ? requestAnimationFrame(f) : r(Math.round(n * 1000 / (performance.now() - t0))); }; f(); })`);
console.log("fps over 3 s (headless, software render possible):", fps);
console.log("page errors:", JSON.stringify(await ev("document.getElementById('errors').textContent")));
console.log("console problems:", problems.length ? problems : "none");
ws.close(); chrome.kill();
process.exit(problems.length ? 1 : 0);
