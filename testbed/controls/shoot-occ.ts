// Brief 056: headless pictures of each occlusion option at each camera turn. Run with the server up:
//   PORT=5191 bun testbed/controls/serve.ts   and   bun testbed/controls/shoot-occ.ts [url] [outdir]
import { spawn } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const url = process.argv[2] ?? "http://localhost:5191/";
const out = process.argv[3] ?? join(import.meta.dir, "shots", "occ"); mkdirSync(out, { recursive: true });
const DP = 9391;
const chrome = spawn("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", ["--headless=new", `--remote-debugging-port=${DP}`, "--user-data-dir=/tmp/controls-shoot-occ", "--window-size=1920,1080", "--hide-scrollbars", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist", "about:blank"], { stdio: "ignore" });
const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));
let tabs: any[] = [];
for (let i = 0; i < 50 && !tabs.length; i++) { await sleep(200); try { tabs = await (await fetch(`http://127.0.0.1:${DP}/json`)).json(); } catch {} }
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
const shot = async (name: string, wait = 1200) => { await sleep(wait); const r = await send("Page.captureScreenshot", { format: "png" }); writeFileSync(join(out, name + ".png"), Buffer.from(r.data, "base64")); console.log("shot", name); };

await send("Runtime.enable"); await send("Page.enable");
await send("Emulation.setDeviceMetricsOverride", { width: 1920, height: 1080, deviceScaleFactor: Number(process.env.DPR ?? 1), mobile: false });
await send("Page.navigate", { url }); await sleep(2500);
console.log("errors element:", JSON.stringify(await ev("document.getElementById('errors').textContent")));

const modes = (process.env.MODES ?? "column,sil,circle,whole,floors,mix").split(","), yaws = (process.env.YAWS ?? "30,120,210,300").split(",").map(Number);
// the squad stands in the street; the cursor rests on the Scout
await ev(`__ctl.squad()`);
// where the squad stands for each camera turn: the east end of the street is the only place a turn to 120 has a block in the way
const stand = (yaw: number) => (yaw === 120 ? { x: 14.5, z: 3 } : yaw === 300 ? { x: -3, z: 3 } : { x: 2, z: 3 });
const place = (yaw: number) => { const s = stand(yaw); return ev(`(() => { const G = __ctl.G; const pos = [[-1, 0], [1.2, -0.8], [3.4, 0.2], [5.6, -0.6]]; G.world.agents.forEach((a, i) => { a.g.position.set(${s.x} + pos[i][0], 0, ${s.z} + pos[i][1]); a.order = null; }); G.rig.focus.set(${s.x} + 2, 0, ${s.z}); })()`); };
for (const yaw of yaws) {
  await place(yaw); await ev(`__ctl.yaw(${yaw})`);
  for (const m of modes) {
    await ev(`__ctl.setOcc('${m}')`);
    await shot(`${m}-yaw${yaw}`, 1500);
  }
}
console.log("problems:", problems.length ? problems.join("\n") : "none");
chrome.kill(); ws.close(); process.exit(0);
