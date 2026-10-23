// Paid helper tests (brief 053): runs the Python unit tests (ledger, rollup maths, budget refusal; fake client, no network) and
// smoke-runs the viewer's script against a stub DOM built from a fixture ledger. No paid call is made.
import { expect, test } from "bun:test";
import { mkdtempSync, readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

const here = import.meta.dir;
const run = (args: string[], env: Record<string, string> = {}) =>
  Bun.spawnSync(["python3", "-I", ...args], { env: { ...process.env, ...env }, stdout: "pipe", stderr: "pipe" });

test("python unit tests: ledger, rollup, budget", () => {
  const r = run([join(here, "test_paid.py")]);
  expect(r.stderr.toString() + r.stdout.toString()).toContain("OK");
  expect(r.exitCode).toBe(0);
});

test("viewer renders totals, jobs and badges from a ledger", () => {
  const dir = mkdtempSync(join(tmpdir(), "paid-view-"));
  const paid = join(dir, "paid"), spend = join(dir, "spend.json");
  mkdirSync(join(paid, "900-t"), { recursive: true });
  const rows = [
    { id: 1, task: "image", pass: "1", model: "m1", at: "2026-10-20T10:00:00+01:00", status: "ok", cost_usd: 0.25, used_in: ["film.mp4 (hero)"], file: "a.png", prompt: "p <b>" },
    { id: 2, task: "video", pass: "1", model: "m2", at: "2026-10-21T10:00:00+01:00", status: "blocked", cost_usd: 0, used_in: [], file: null, prompt: "q" },
    { id: 3, task: "image", pass: "1", model: "m1", at: "2026-10-21T11:00:00+01:00", status: "ok", cost_usd: 0.30, used_in: [], file: "c.mp4" },
  ];
  writeFileSync(join(paid, "900-t/ledger.jsonl"), rows.map((r) => JSON.stringify(r)).join("\n") + "\n");
  writeFileSync(join(paid, "900-t/a.png"), "not really a png");   // exists, so it's viewable; the thumbnail just fails
  writeFileSync(join(paid, "900-t/c.mp4"), "not really a video");
  writeFileSync(join(paid, "900-t/passes.json"), JSON.stringify({ "1": { budget_usd: 2.0 } }));
  const py = `import sys; sys.path.insert(0, ${JSON.stringify(here)}); import paid; paid.build_view()`;
  const b = run(["-c", py], { SUBORNED_PAID: paid, SUBORNED_PAID_SPEND: spend });
  expect(b.exitCode).toBe(0);
  const html = readFileSync(join(paid, "index.html"), "utf8");
  const script = html.split("<script>").pop()!.split("</script>")[0];
  const blob = html.split('id="data">')[1].split("</script>")[0];
  const els: Record<string, any> = {};
  const on: Record<string, (e: any) => void> = {};
  const cls = () => { const set = new Set(["hide"]); return { add: (c: string) => set.add(c), remove: (c: string) => set.delete(c), contains: (c: string) => set.has(c) }; };
  const el = (id: string) => (els[id] ??= { id, innerHTML: "", textContent: id === "data" ? blob : "", value: id === "fSort" ? "time" : "", disabled: false, classList: cls(),
    addEventListener(t: string, f: any) { on[id + ":" + t] = f; } });
  const document = { getElementById: el, body: { style: {} }, addEventListener(t: string, f: any) { on["doc:" + t] = f; } };
  new Function("document", "Image", script)(document, class { src = ""; });
  const hero: string = el("hero").innerHTML;
  expect(hero).toContain("$0.55");                       // spent all time
  expect(hero).toContain("0.55");                        // 0.25 + 0.30
  expect(el("tables").innerHTML).toContain("900-t");
  expect(el("tables").innerHTML).toContain("$2.00");     // the pass budget
  const groups: string = el("groups").innerHTML;
  expect(groups).toContain("used in film.mp4 (hero)");
  expect(groups).toContain("blocked");
  expect(groups).toContain("p &lt;b&gt;");               // prompts are escaped
  expect(el("count").textContent).toContain("3 of 3 rows");

  // lightbox: clicking a thumbnail opens it; arrows step through the viewable assets in view; Esc closes
  expect(groups).toContain('data-i="0"');
  expect(groups).toContain('data-i="1"');
  expect(groups).not.toContain('data-i="2"');            // the blocked call has no file
  const key = (k: string) => on["doc:keydown"]({ key: k, preventDefault() {} });
  on["groups:click"]({ target: { closest: () => ({ dataset: { i: "0" } }) } });
  expect(el("lb").classList.contains("hide")).toBe(false);
  expect(el("lbStage").innerHTML).toContain('<img src="900-t/a.png"');
  expect(el("lbCap").innerHTML).toContain("1 of 2 in view");
  expect(el("lbPrev").disabled).toBe(true);
  key("ArrowRight");
  expect(el("lbStage").innerHTML).toContain("<video");
  expect(el("lbCap").innerHTML).toContain("2 of 2 in view");
  expect(el("lbNext").disabled).toBe(true);
  key("ArrowRight");                                        // stays on the last
  expect(el("lbCap").innerHTML).toContain("2 of 2 in view");
  key("ArrowLeft");
  expect(el("lbCap").innerHTML).toContain("p &lt;b&gt;");
  key("Escape");
  expect(el("lb").classList.contains("hide")).toBe(true);
  expect(el("lbStage").innerHTML).toBe("");
});
