// Writes the direction A tokens (frontend.tokens.js) to briefs/051/lightline/frontend.a.json for Latch: `bun testbed/hud-style/frontend-export.ts`
import { A } from "./frontend.tokens.js";
import { writeFileSync } from "node:fs";
writeFileSync(new URL("../../briefs/051/lightline/frontend.a.json", import.meta.url), JSON.stringify(A, null, 2) + "\n");
console.log("wrote briefs/051/lightline/frontend.a.json");
