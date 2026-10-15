"""Shared helpers for briefs/012 (cell characters r2). Ledger = append-only jsonl, file-locked, one line per image.

Spend guard: Qwen calls refuse to start when ledger total + the call's worst-case cost would pass STOP ($2.90).
Hard ceiling $3.00 (Sky via Aster, 2026-10-15); the probe ($0.014) is counted in PROBE.
"""
import base64, fcntl, hashlib, io, json, os, subprocess, sys, tempfile, threading, time
from pathlib import Path
from PIL import Image

sys.path.insert(0, "/Users/sky/Projects/the-suborned/workbench/Tools/Concept")
import runware  # noqa: E402

ROOT = Path("/Users/sky/Projects/suborned")
SCR = ROOT / "out/concepts/cell-r2"
RAW = SCR / "raw"
DEL = ROOT / "briefs/012/cell"
LEDGER = SCR / "ledger.jsonl"
LOCK = SCR / ".ledger.lock"
PROBE = 0.014
STOP = 2.90
QWEN = "alibaba:qwen-image@2.1-turbo"
REFS = SCR / "refs"
PICK = {k: REFS / f for k, f in dict(scout="scout_S1-2.png", fixer="fixer_P4-6.png", tech="tech_F2-1.png", enforcer="enforcer_G3.png").items()}
_mem = threading.Lock()


def sha(b): return hashlib.sha256(b).hexdigest()


def seed_for(ident): return int(hashlib.sha256(ident.encode()).hexdigest()[:8], 16) % 2147483647


def read_ledger():
    if not LEDGER.exists(): return []
    return [json.loads(l) for l in LEDGER.read_text().splitlines() if l.strip()]


def spent():
    return PROBE + sum(float(r.get("cost") or 0) for r in read_ledger() if r.get("model") == "qwen")


def log(row):
    with _mem, open(LOCK, "w") as lk:
        fcntl.flock(lk, fcntl.LOCK_EX)
        with open(LEDGER, "a") as f: f.write(json.dumps(row) + "\n")


def done(ident):
    return any(r["id"] == ident for r in read_ledger()) and (RAW / f"{ident}.png").exists()


def datauri(p):
    im = Image.open(p).convert("RGB")
    b = io.BytesIO(); im.save(b, "PNG")
    return "data:image/png;base64," + base64.b64encode(b.getvalue()).decode()


def deliver(ident, png, folder, keep_png=False):
    """raw PNG -> scratch; delivered JPG q92 (or PNG) -> briefs/012/cell/<folder>/<ident>.*"""
    (RAW / f"{ident}.png").write_bytes(png)
    d = DEL / folder; d.mkdir(parents=True, exist_ok=True)
    im = Image.open(io.BytesIO(png)).convert("RGB")
    p = d / (ident + (".png" if keep_png else ".jpg"))
    if keep_png: p.write_bytes(png)
    else: im.save(p, "JPEG", quality=92, optimize=True)
    return p, im.size


def qwen(ident, folder, prompt, w, h, refs=(), setname="", hook="", meta=None, keep_png=False, est=0.025):
    if done(ident): return
    if spent() + est > STOP: raise SystemExit(f"spend guard: ${spent():.3f} + {est} > {STOP}")
    extra = {"settings": {"promptExtend": False}, "seed": seed_for(ident)}
    if refs: extra["inputs"] = {"referenceImages": [datauri(r) for r in refs]}
    t = time.time()
    res = runware.infer(QWEN, prompt, w, h, 1, **extra)
    png, cost, seed = res[0]
    p, size = deliver(ident, png, folder, keep_png)
    row = dict(id=ident, set=setname or folder, model="qwen", model_id=QWEN, prompt=prompt, refs=[str(r) for r in refs], seed=seed,
               cost=cost, file=str(p.relative_to(DEL)), size=list(size), sha256=sha(p.read_bytes()), sha256_png=sha(png), hook=hook,
               secs=round(time.time() - t, 1), at=time.strftime("%Y-%m-%dT%H:%M:%S"), **(meta or {}))
    log(row)
    print(f"[qwen] {ident} ${cost} total ${spent():.3f}", flush=True)
    return row


ASK = ("Use your image generation tool to create exactly ONE image in {aspect} aspect from the prompt below, used verbatim (do not "
       "rewrite or add to it). {refline}Save it as out.png in the current directory, then stop. Prompt: {prompt}")
FRAME = " Leave clear margin around every figure: nothing cropped at the head or feet."


def aspect(w, h): return "landscape 3:2" if w > h * 1.1 else "portrait 2:3" if h > w * 1.1 else "square 1:1"


def codex(ident, folder, prompt, w, h, refs=(), refnotes="", setname="", hook="", meta=None, keep_png=False, tries=2, timeout=1500, frame=True):
    if done(ident): return
    refline = (f"The attached image(s) are reference images: {refnotes} Use them as image references for the generation. " if refs else "")
    ask = ASK.format(aspect=aspect(w, h), prompt=prompt + (FRAME if frame else ""), refline=refline)
    t = time.time(); last = ""
    for k in range(tries):
        with tempfile.TemporaryDirectory() as d:
            cmd = ["codex", "exec", "--skip-git-repo-check", "-s", "workspace-write"]
            for r in refs: cmd += ["-i", str(r)]
            try:
                r = subprocess.run(cmd, input=ask, cwd=d, capture_output=True, text=True, timeout=timeout)
            except subprocess.TimeoutExpired:
                last = "timeout"; continue
            out = Path(d) / "out.png"
            if out.exists():
                png = out.read_bytes(); break
            last = (r.stdout[-300:] + r.stderr[-300:])
    else:
        print(f"[codex] FAIL {ident}: {last}", flush=True); return
    p, size = deliver(ident, png, folder, keep_png)
    row = dict(id=ident, set=setname or folder, model="codex", model_id="codex image tool (ChatGPT plan, free)", prompt=prompt, refs=[str(r) for r in refs],
               refnotes=refnotes, seed=None, cost=0, file=str(p.relative_to(DEL)), size=list(size), sha256=sha(p.read_bytes()), sha256_png=sha(png), hook=hook,
               secs=round(time.time() - t, 1), at=time.strftime("%Y-%m-%dT%H:%M:%S"), **(meta or {}))
    log(row)
    print(f"[codex] {ident} {int(time.time()-t)}s", flush=True)
    return row
