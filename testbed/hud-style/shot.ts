// One picture of the lab: bun testbed/hud-style/shot.ts "p=creamPlate&s=board&z=1&bare=1" out.png [dpr] [x,y,w,h]   (server must be up)
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
const [hash, file, dpr = "1", clip] = process.argv.slice(2);
const PORT = 9336, PAGE = process.env.SHOT_PAGE ?? "";
const chrome = spawn("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", ["--headless=new", `--remote-debugging-port=${PORT}`, "--user-data-dir=/tmp/hudstyle-shot", "--hide-scrollbars", "about:blank"], { stdio: "ignore" });
const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));
let tabs: any[] = [];
for (let i = 0; i < 50 && !tabs.length; i++) { await sleep(200); try { tabs = await (await fetch(`http://127.0.0.1:${PORT}/json`)).json(); } catch {} }
const ws = new WebSocket(tabs.find((t) => t.type === "page").webSocketDebuggerUrl); await new Promise((r) => (ws.onopen = r));
let id = 0; const wait = new Map<number, (v: any) => void>(); ws.onmessage = (m) => { const d = JSON.parse(String(m.data)); if (d.id && wait.has(d.id)) wait.get(d.id)!(d.result); };
const send = (method: string, params: any = {}) => new Promise<any>((res) => { const i = ++id; wait.set(i, res); ws.send(JSON.stringify({ id: i, method, params })); });
await send("Page.enable"); await send("Emulation.setDeviceMetricsOverride", { width: 1920, height: 1080, deviceScaleFactor: Number(dpr), mobile: false });
await send("Page.navigate", { url: `http://localhost:5175/${PAGE}#${hash}` }); await sleep(1800);
const c = clip?.split(",").map(Number);
const r = await send("Page.captureScreenshot", { format: "png", ...(c ? { clip: { x: c[0], y: c[1], width: c[2], height: c[3], scale: 1 } } : {}) });
writeFileSync(file, Buffer.from(r.data, "base64")); chrome.kill(); process.exit(0);
