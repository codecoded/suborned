// Headless check for brief 043 (the done moment): drives Chrome with real mouse events through the Handover's chain on the fake street and
// saves 1920x1080 PNGs into shots/done-*.png. Same set-up as shoot.ts; run with the server up: `bun run controls`, then `bun testbed/controls/shoot-done.ts`.
// Chain: Watch Inez, Listen (Help and Pay open), Help twice (not yet, then a contact), Call (the guard's window), Loop (the camera's window, closing),
// Take Vale (Board opens), a new order dropping a Listen, and "As built" for comparison.
import { spawn } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const url = process.argv[2] ?? "http://localhost:5173/";
const out = join(import.meta.dir, "shots"); mkdirSync(out, { recursive: true });
const chrome = spawn("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", ["--headless=new", "--remote-debugging-port=9334", "--user-data-dir=/tmp/controls-shoot-done", "--window-size=1920,1080", "--hide-scrollbars", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist", "about:blank"], { stdio: "ignore" });
const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));
let tabs: any[] = [];
for (let i = 0; i < 50 && !tabs.length; i++) { await sleep(200); try { tabs = await (await fetch("http://127.0.0.1:9334/json")).json(); } catch {} }
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
const press = async (x: number, y: number) => { await mouse("mouseMoved", x, y); await send("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button: "left", clickCount: 1, buttons: 1 }); };
const lift = async (x: number, y: number) => send("Input.dispatchMouseEvent", { type: "mouseReleased", x, y, button: "left", clickCount: 1, buttons: 0 });
const holdAct = async (x: number, y: number) => { await press(x, y); await sleep(650); await lift(x, y); }; // the hold: fills 0.4 s, commits on release
const click = async (x: number, y: number) => { await mouse("mouseMoved", x, y); await mouse("mousePressed", x, y, "left", 1); await sleep(60); await mouse("mouseReleased", x, y, "left", 1); };
const at = async (name: string) => JSON.parse(await ev(`JSON.stringify(__ctl.screenOf('${name}'))`)) as { x: number; y: number };
const shot = async (name: string) => { await sleep(120); const r = await send("Page.captureScreenshot", { format: "png" }); writeFileSync(join(out, "done-" + name + ".png"), Buffer.from(r.data, "base64")); console.log("shot", name); };
const check = (name: string, ok: boolean, extra = "") => { console.log(ok ? "PASS" : "FAIL", name, extra); if (!ok) problems.push("check failed: " + name); };
const until = async (expr: string, ms = 8000) => { for (let t = 0; t < ms; t += 80) { if (await ev(expr)) return true; await sleep(80); } return false; };
const text = async (sel: string) => (await ev(`[...document.querySelectorAll('${sel}')].map(e => e.textContent).join(' | ')`)) as string;
const pill = async () => (await ev(`(() => { const m = document.querySelector('#marker'); return m.style.display === 'none' ? '' : m.textContent; })()`)) as string;
const ways = async (id: string, a: number) => JSON.parse(await ev(`JSON.stringify(__ctl.ways('${id}', ${a}))`)) as string[];
const fresh = async (mode = "gated") => { await ev(`document.querySelector('[data-reset]').click()`); await sleep(300); await ev(`__ctl.preset('hold'); __ctl.G.done = '${mode}'; __ctl.G.actScale = 20; __ctl.G.fights = 'current'; __ctl.G.camMode = 'leash'`); };

await send("Runtime.enable"); await send("Page.enable");
await send("Emulation.setDeviceMetricsOverride", { width: 1920, height: 1080, deviceScaleFactor: 1, mobile: false });
await send("Page.navigate", { url }); await sleep(2500);
console.log("errors element:", JSON.stringify(await ev("document.getElementById('errors').textContent")));
check("the Done moment option is in the settings", (await ev(`document.querySelector('[data-o="done"][data-v="gated"]') !== null`)) === true);

// 1. Watch Inez: she is the gate to Listen
await fresh("gated"); await ev(`__ctl.select(0); __ctl.G.world.agents[0].g.position.set(-7, 0, 3.2)`); await sleep(300);
check("gated: before any Watch the Scout has only Watch on Inez", JSON.stringify(await ways("inez", 0)) === JSON.stringify(["watch"]), JSON.stringify(await ways("inez", 0)));
check("gated: the Fixer has only Watch on Inez yet (Pay, Help, Pressure, Call are shut)", JSON.stringify(await ways("inez", 1)) === JSON.stringify(["watch"]), JSON.stringify(await ways("inez", 1)));
let inez = await at("inez"); await mouse("mouseMoved", inez.x, inez.y); await sleep(500);
check("offer pill: Hold · Watch Inez", (await pill()).includes("Hold") && (await pill()).includes("Watch Inez"), await pill());
await holdAct(inez.x, inez.y);
check("watch lands: a chip on Inez with her rounds", await until(`__ctl.G.chips?.length === 1`) && (await text("#gains .gn")).includes("Rounds"), await text("#gains .gn"));
await sleep(200); await shot("1-watch-inez");
check("watch lands: the chip names what opened (Listen)", (await text("#gains .gn")).includes("Listen"), await text("#gains .gn"));
check("watch lands: the pill does not also say Done", !(await pill()).includes("Done"), await pill());
check("watch lands: the plate says done, in one word", (await ev(`document.querySelector('[data-plate="0"] .val').textContent`)) === "done" && (await ev(`document.querySelector('[data-plate="0"]').style.display`)) === "flex");
check("watch lands: a tick over the agent", (await ev(`document.querySelectorAll('#pops .tick').length`)) === 1);
check("watch lands: a history row with a notes key", (await ev(`__ctl.G.hist.length`)) === 1 && (await ev(`getComputedStyle(document.getElementById('histkey')).display`)) !== "none");
check("watch lands: Listen is now the Scout's main action on Inez, lit", JSON.stringify(await ways("inez", 0)) === JSON.stringify(["listen", "watch"]), JSON.stringify(await ways("inez", 0)));
await sleep(4300);
check("the chip goes away by itself", (await ev(`document.querySelectorAll('#gains .gn').length`)) === 0);
await mouse("mouseMoved", inez.x + 3, inez.y); await sleep(500);
check("hover again: the offer is Listen and carries the lit pip", (await pill()).includes("Listen Inez") && (await ev(`document.querySelector('#marker em.new') !== null`)) === true, await pill());
await shot("2-listen-offered-with-pip");

// 2. Listen: the Intel facts, and Help and Pay open
await holdAct(inez.x, inez.y);
check("listen lands: the facts, in the Intel fragments", await until(`__ctl.G.chips?.length === 1`) && (await text("#gains .gn")).includes("Bureau loan") && (await text("#gains .gn")).includes("Idealist") && (await text("#gains .gn")).includes("Loves the guard"), await text("#gains .gn"));
await sleep(250); await shot("3-listen-inez");
check("listen lands: the chip names Help as newly open", (await text("#gains .gn")).includes("Help"), await text("#gains .gn"));
check("listen lands: the Fixer's ways on Inez are now Help first (it fits), then Pay, Pressure", JSON.stringify((await ways("inez", 1)).slice(0, 3)) === JSON.stringify(["help", "pay", "press"]), JSON.stringify(await ways("inez", 1)));
await sleep(4300);

// 3. Help Inez twice: not yet, then a contact, then the call opens
await ev(`__ctl.select(1); __ctl.G.world.agents[1].g.position.set(-8, 0, 3.4)`); await sleep(300); inez = await at("inez");
await mouse("mouseMoved", inez.x, inez.y); await sleep(500);
check("fixer offer on Inez is the lit Help", (await pill()).includes("Help Inez") && (await ev(`document.querySelector('#marker em.new') !== null`)) === true, await pill());
await holdAct(inez.x, inez.y);
check("help lands, first time: 'Not yet', soft, and the act can be tried again", await until(`__ctl.G.chips?.length === 1`, 12000) && (await text("#gains .gn")).includes("Not yet") && (await ev(`document.querySelector('#gains .gn').classList.contains('soft')`)) === true && (await ev(`document.querySelector('[data-plate="1"] .val').textContent`)) === "not yet", await text("#gains .gn"));
await shot("4-help-not-yet");
check("help not yet: Help is still on offer", (await ways("inez", 1)).includes("help"), JSON.stringify(await ways("inez", 1)));
await sleep(4400); await mouse("mouseMoved", inez.x + 2, inez.y); await sleep(300); await holdAct(inez.x, inez.y);
check("help lands, second time: a contact, and Call opens", await until(`__ctl.G.chips?.length === 1`, 12000) && (await text("#gains .gn")).includes("Contact") && (await text("#gains .gn")).includes("Call"), await text("#gains .gn"));
await shot("5-help-contact");
check("help lands: the plate says turned", (await ev(`document.querySelector('[data-plate="1"] .val').textContent`)) === "turned");
await sleep(4400);

// 4. Call Inez: the guard's window opens, with a number
await mouse("mouseMoved", inez.x, inez.y); await sleep(500);
check("call is the lit offer on a contact", (await pill()).includes("Call Inez"), await pill());
await holdAct(inez.x, inez.y);
check("call lands: 'Guard away' on the guard, and a window marker counting down", await until(`__ctl.G.chips?.length === 1`) && (await text("#gains .gn")).includes("Guard away") && (await ev(`document.querySelectorAll('#wins .win').length`)) === 1, await text("#gains .gn") + " / " + await text("#wins .win"));
await sleep(300); await shot("6-call-guard-window");
check("call lands: the guard's cone is off while the window is open", (await ev(`__ctl.G.world.byId.guard.dark`)) === true);
await ev(`__ctl.G.world.byId.guard.window.until = __ctl.G.clock + 0.4`); // the minute, jumped
check("the call's window closes by itself and says so", await until(`__ctl.G.world.byId.guard.dark === false`, 3000) && (await text("#pops .pop")).includes("Guard back"), await text("#pops .pop"));

// 5. Loop through the cabinet: the camera goes dark while the Tech stays; leaving ends it
await fresh("gated"); await ev(`__ctl.select(2); __ctl.G.world.agents[2].g.position.set(3, 0, 3.6)`); await sleep(300);
let cab = await at("cabinet"); await mouse("mouseMoved", cab.x, cab.y); await sleep(500);
await holdAct(cab.x, cab.y);
check("loop lands: 'Camera dark' on the camera, the Tech's face on its window", await until(`__ctl.G.chips?.length === 1`, 12000) && (await text("#gains .gn")).includes("Camera dark") && (await ev(`document.querySelectorAll('#wins .win .faces').length`)) === 1, await text("#gains .gn"));
await sleep(300); await shot("7-loop-camera-dark");
check("loop lands: the camera's cone is off", (await ev(`__ctl.G.world.byId.camera.dark`)) === true);
await ev(`__ctl.select(2)`); const away = await at("cabinet"); await click(away.x - 220, away.y + 120);
check("the Tech walking off ends the loop: 'Camera on'", await until(`__ctl.G.world.byId.camera.dark === false`, 3000) && (await text("#pops .pop")).includes("Camera on"), await text("#pops .pop"));
await shot("8-loop-ends-camera-on");

// 6. A new order drops a Listen in progress: the warning first, then the pop
await fresh("gain"); await ev(`__ctl.G.actScale = 4; __ctl.select(0); __ctl.G.world.agents[0].g.position.set(-7, 0, 3.2)`); await sleep(300); inez = await at("inez");
await holdAct(inez.x, inez.y); await until(`__ctl.G.world.agents[0].order?.phase === 'act'`, 4000); await sleep(1500);
const gd = await at("guard"); await mouse("mouseMoved", gd.x, gd.y); await sleep(600);
check("before you commit: the pill on another target says what it would cancel", (await text("#marker .cx")).includes("Cancels Watch"), await text("#marker .cx"));
await shot("9-cancel-warning-on-target");
const c0 = await at("cell0"), gx = c0.x - 130, gy = c0.y + 80; await mouse("mouseMoved", gx, gy); await mouse("mouseMoved", gx + 2, gy); await sleep(600);
check("before you commit: open ground says it too, with the time lost", (await text("#cxg")).includes("Cancels Watch") && /\d+ s lost/.test(await text("#cxg")), await text("#cxg"));
await shot("10-cancel-warning-on-ground");
await click(gx, gy); await sleep(200);
check("a new order drops the Watch: a pop on the agent", (await text("#pops .pop")).includes("Watch cancelled"), await text("#pops .pop"));
check("a new order drops the Watch: the plate says cancelled", (await ev(`document.querySelector('[data-plate="0"] .val').textContent`)) === "cancelled" || (await ev(`__ctl.G.world.agents[0].doneWord`)) === "cancelled");
await shot("11-cancel-pop");
await ev(`__ctl.G.histOpen = true`); await sleep(300);
check("the history keeps the cancel as a row", (await text("#hist")).includes("Watch cancelled"), await text("#hist"));
await shot("12-history");

// 7. Take Vale: the car can be boarded
await fresh("gated"); await ev(`document.querySelector('[data-vale]').click(); __ctl.select(3); __ctl.G.world.agents[3].g.position.set(11, 0, 4)`); await sleep(400);
check("gated: Board is not offered on the car before Vale is taken", !(await ways("car", 3)).includes("board"), JSON.stringify(await ways("car", 3)));
const vl = await at("vale"); await mouse("mouseMoved", vl.x, vl.y); await sleep(500); await holdAct(vl.x, vl.y);
check("take lands: 'Vale' and Board opens on the car", await until(`__ctl.G.chips?.length >= 1`, 14000) && (await text("#gains .gn")).includes("Vale") && (await text("#gains .gn")).includes("Board") && (await ways("car", 3)).includes("board"), await text("#gains .gn"));
await shot("13-take-vale");

// 8. As built: the old behaviour is untouched
await fresh("built"); await ev(`__ctl.select(2); __ctl.G.world.agents[2].g.position.set(3, 0, 3.6)`); await sleep(300); cab = await at("cabinet"); await mouse("mouseMoved", cab.x, cab.y); await sleep(400); await holdAct(cab.x, cab.y);
await until(`__ctl.G.world.agents[2].order === null && __ctl.G.trail === 1`, 12000); await sleep(150);
check("as built: no chip, the old pill says Done · camera looped", (await ev(`(__ctl.G.chips ?? []).length`)) === 0 && (await pill()).includes("Done · camera looped"), await pill());

console.log("page errors:", JSON.stringify(await ev("document.getElementById('errors').textContent")));
console.log("console problems:", problems.length ? problems : "none");
ws.close(); chrome.kill();
process.exit(problems.length ? 1 : 0);
