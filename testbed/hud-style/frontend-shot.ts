// Front-end boards (brief 051, direction A): bun testbed/hud-style/frontend-shot.ts "<hash>" out.png [dpr] [x,y,w,h] [rects.json]
// hash: s=menu&w=1920&h=1080&static=1 (w,h = device px at dpr 1). Needs `bun run hud-style` up. A fresh headless Chrome per call.
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
const [hash, file, dpr = "1", clip, rectsFile] = process.argv.slice(2);
const q = new URLSearchParams(hash); const W = +(q.get("w") || 1920), H = +(q.get("h") || 1080);
const PORT = 9340 + Math.floor(Math.random() * 400);
const chrome = spawn("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", ["--headless=new", `--remote-debugging-port=${PORT}`, `--user-data-dir=/tmp/fe-shot-${PORT}`, "--hide-scrollbars", "--force-color-profile=srgb", "about:blank"], { stdio: "ignore" });
const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));
let tabs: any[] = [];
for (let i = 0; i < 50 && !tabs.length; i++) { await sleep(200); try { tabs = await (await fetch(`http://127.0.0.1:${PORT}/json`)).json(); } catch {} }
const ws = new WebSocket(tabs.find((t) => t.type === "page").webSocketDebuggerUrl); await new Promise((r) => (ws.onopen = r));
let id = 0; const wait = new Map<number, (v: any) => void>(); ws.onmessage = (m) => { const d = JSON.parse(String(m.data)); if (d.id && wait.has(d.id)) wait.get(d.id)!(d.result); };
const send = (method: string, params: any = {}) => new Promise<any>((res) => { const i = ++id; wait.set(i, res); ws.send(JSON.stringify({ id: i, method, params })); });
await send("Page.enable"); await send("Emulation.setDeviceMetricsOverride", { width: W, height: H, deviceScaleFactor: Number(dpr), mobile: false });
await send("Page.navigate", { url: `http://localhost:5175/frontend.html#${hash}` });
for (let i = 0; i < 60; i++) { await sleep(250); const r = await send("Runtime.evaluate", { expression: "window.__ready===true" }); if (r?.result?.value) break; }
await sleep(500);
if (rectsFile) { const r = await send("Runtime.evaluate", { expression: "JSON.stringify(window.__rects())", returnByValue: true }); writeFileSync(rectsFile, r.result.value); }
const c = clip ? clip.split(",").map(Number) : undefined;
const r = await send("Page.captureScreenshot", { format: "png", ...(c ? { clip: { x: c[0], y: c[1], width: c[2], height: c[3], scale: 1 } } : {}) });
writeFileSync(file, Buffer.from(r.data, "base64")); chrome.kill(); process.exit(0);
