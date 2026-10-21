// A tiny static server for the controls playground: `bun run controls`.
import { join, normalize } from "node:path";

const root = import.meta.dir;
const port = Number(process.env.PORT ?? 5173);
const server = Bun.serve({
  port,
  async fetch(req) {
    let path = decodeURIComponent(new URL(req.url).pathname);
    if (path.endsWith("/")) path += "index.html";
    const file = normalize(join(root, path));
    if (!file.startsWith(root) || file.endsWith("serve.ts")) return new Response("Not found", { status: 404 });
    const f = Bun.file(file);
    return (await f.exists()) ? new Response(f, { headers: { "Cache-Control": "no-store" } }) : new Response("Not found", { status: 404 });
  },
});
console.log(`Controls playground: http://localhost:${server.port}/`);
