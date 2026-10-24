// Publishes the controls playground (brief 041) as a public web page: `bun run controls:publish`.
// Copies the page (no shots, no dev scripts) to out/controls-site and deploys it to the Cloudflare Pages
// project suborned-controls, served at https://suborned-controls.pages.dev/.
import { cpSync, mkdirSync, readdirSync, rmSync } from "node:fs";
import { join } from "node:path";
import { spawnSync } from "node:child_process";

const PROJECT = "suborned-controls";
const src = import.meta.dir;
const out = join(src, "..", "..", "out", "controls-site");
// dev-only: the shooters (shoot.ts, shoot-occ.ts, shoot-done.ts, shoot-mobile.ts, any shoot*.ts), the server, this script and the shots
const skip = (e: string) => e === "shots" || e === "serve.ts" || e === "publish.ts" || /^shoot.*\.ts$/.test(e);

rmSync(out, { recursive: true, force: true });
mkdirSync(out, { recursive: true });
for (const e of readdirSync(src)) if (!skip(e)) cpSync(join(src, e), join(out, e), { recursive: true });

const r = spawnSync("wrangler", ["pages", "deploy", out, "--project-name", PROJECT, "--branch", "main", "--commit-dirty=true"], { stdio: "inherit" });
if (r.status !== 0) process.exit(r.status ?? 1);
console.log(`controls: published → https://${PROJECT}.pages.dev/`);
