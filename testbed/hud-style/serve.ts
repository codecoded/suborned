// HUD style lab: `bun run hud-style` (http://localhost:5175/). Serves this folder, the game's real kit at /kit/ (read from
// unity/Assets/Suborned/UI/Resources/Kit, so the lab never drifts from the sprites and fonts the game imports),
// and one write endpoint: POST /api/export saves the picked tokens (and recoloured sprites) to testbed/hud-style/export/.
import { join, normalize } from "node:path";
import { mkdirSync, writeFileSync } from "node:fs";

const root = import.meta.dir;
const kit = join(root, "../../unity/Assets/Suborned/UI/Resources/Kit");
const port = Number(process.env.PORT ?? 5175);
const send = (f: string) => { const b = Bun.file(f); return b.exists().then((ok) => ok ? new Response(b, { headers: { "Cache-Control": "no-store" } }) : new Response("Not found", { status: 404 })); };
Bun.serve({
  port,
  async fetch(req) {
    const u = new URL(req.url);
    let path = decodeURIComponent(u.pathname);
    if (req.method === "POST" && path === "/api/export") {
      const body = await req.json() as { name: string; tokens: unknown; sprites?: Record<string, string> };
      const name = String(body.name).replace(/[^a-z0-9_-]/gi, "_");
      const dir = join(root, "export"); mkdirSync(join(dir, "sprites"), { recursive: true });
      const json = JSON.stringify(body.tokens, null, 2) + "\n";
      writeFileSync(join(dir, `${name}.tokens.json`), json);
      writeFileSync(join(dir, "hud.tokens.json"), json); // the one Latch loads: the last picked
      for (const [n, b64] of Object.entries(body.sprites ?? {})) writeFileSync(join(dir, "sprites", `${name}-${n.replace(/[^a-z0-9_@.-]/gi, "_")}`), Buffer.from(b64, "base64"));
      return Response.json({ ok: true, wrote: [`export/${name}.tokens.json`, "export/hud.tokens.json", ...Object.keys(body.sprites ?? {}).map((n) => `export/sprites/${name}-${n}`)] });
    }
    if (path.startsWith("/kit/")) { const f = normalize(join(kit, path.slice(5))); return f.startsWith(kit) ? send(f) : new Response("Not found", { status: 404 }); }
    if (path.endsWith("/")) path += "index.html";
    const f = normalize(join(root, path));
    if (!f.startsWith(root) || f.endsWith("serve.ts") || f.includes("/prep/")) return new Response("Not found", { status: 404 });
    return send(f);
  },
});
console.log(`HUD style lab: http://localhost:${port}/`);
