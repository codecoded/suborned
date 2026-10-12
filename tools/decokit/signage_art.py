"""Signage artwork: fictional 1950s space-age ads (Runware Seedream) packed with procedural letter boards into one
2048 atlas that signage.py UV-maps (DK_Billboard_Ads printed, DK_Billboard_Ads_Lit backlit).

python3 Tools/Blender/decokit/signage_art.py --gen [--only a,b]   # missing sources -> Textures/gen/signage_src/
python3 Tools/Blender/decokit/signage_art.py --pack               # -> Textures/gen/billboard_ads_v1.png
Layout (px, y down): 8 ads 1024x341 (3:1) in 2 cols x 4 rows | 4 posters 456x684 (2:3) along the bottom |
letter boards drawn rotated 90 deg CCW in the 224 px strip at the bottom right. Top level stays import-light: Blender
imports the layout from here.
"""

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
GEN = ROOT / "SourceArt/DecoKit/Textures/gen"
SRC = GEN / "signage_src"
ATLAS, S = "billboard_ads_v1.png", 2048
MODEL, WIDE, TALL = "bytedance:seedream@5.0-pro", (2496, 832), (1152, 1728)  # both under the $0.048 pixel tier

ADS = ("mars", "fizz", "aircar", "skyrail", "robot", "planetarium", "cruise", "cinema")
POSTERS = ("saturn", "expo", "rocketrace", "vesper")  # vesper = the kit's existing poster_travel_v1.png
AD = {k: (i % 2 * 1024, i // 2 * 341, 1024, 341) for i, k in enumerate(ADS)}
POSTER = {k: (j * 456, 1364, 456, 684) for j, k in enumerate(POSTERS)}
# letter boards (black stroke capitals on warm white, backlit): rect in the atlas, lines; drawn rotated
BOARD = {"front": ((1824, 1364, 112, 684), ("ROBOT MOON", "+ SAUCER GIRL")),
         "side": ((1936, 1364, 112, 224), ("DOUBLE", "FEATURE"))}

STYLE = ("1950s airbrushed space-age advertising illustration, gouache and airbrush on board, bold flat lettering, "
         "crisp confident painted shapes, optimistic retro-future, sunny palette of ivory, cream, teal, seafoam, coral, "
         "mustard and signal red with chrome highlights. The flat artwork itself, straight-on, filling the whole frame "
         "edge to edge: no billboard structure, no frame, no border, no wall, no mockup, not a photograph. Big bold "
         "imagery, very little text. No real brands, no logos, no trademarks, no watermark, no signature, no small print.")
FRAME = {"ad": "A very wide 3:1 panoramic billboard composition, the main subject large, balanced left to right.",
         "poster": "A tall 2:3 portrait poster composition, the main subject large."}
BRIEF = {
    "mars": ("ad", "SEE MARS", "Holidays by rocket liner: a gleaming silver finned rocket liner with red trim soaring on a "
             "white contrail over a rust-red Martian desert of mesas, a domed glass holiday colony below, a small moon "
             "in a teal sky, smiling passengers at round portholes."),
    "fizz": ("ad", "ATOM FIZZ", "A giant frosted soda bottle with a streamline rocket-like shape and a red cap, fizzing "
             "bubbles, three chrome orbit rings circling it like an atom, a smiling couple toasting with glasses, "
             "starbursts on a seafoam background."),
    "aircar": ("ad", "NEW AIRCAR", "A streamline teardrop flying car with tail fins and a bubble canopy, cream and "
               "coral enamel, swooping above a cream city of towers and palms, a happy family waving from inside, "
               "speed lines behind it."),
    "skyrail": ("ad", "SKYRAIL PASS", "A sleek silver-and-red monorail train sweeping along an elevated beam on white "
                "tapered pylons through a sunny cream city of streamline towers and palms, a big stylised travel pass "
                "ticket with a star punched in it tilted at one side."),
    "robot": ("ad", "ROBOT BUTLER", "A friendly round chrome robot butler with a teal enamel body and glowing eyes "
              "carrying a tray of drinks and a cake into a bright space-age living room, a delighted family on a "
              "boomerang sofa."),
    "planetarium": ("ad", "STAR SHOW", "A white domed planetarium with its dome opening under a dazzling deep blue "
                    "night sky full of stars, a ringed planet, a comet and constellation lines, a crowd in coats "
                    "looking up in wonder."),
    "cruise": ("ad", "HARBOUR CRUISE", "A sleek white streamline cruise liner with teal funnels on a sparkling blue bay "
               "at golden hour, sailboats and gulls, a curving cream harbour city with palms and a white lighthouse."),
    "cinema": ("ad", "DOUBLE FEATURE", "A science-fiction double-feature movie billboard in two dramatic panels: on the "
               "left a giant robot looming over a lunar city, on the right a flying saucer beaming light down on a "
               "startled crowd, pulp adventure mood."),
    "saturn": ("poster", "SEE SATURN", "A silver rocket liner departing a spaceport of white gantries toward a huge "
               "pale ringed planet in a deep teal sky, travel poster."),
    "expo": ("poster", "EXPO", "A world exposition of tomorrow: a soaring white streamline tower with a golden atom "
             "finial above fountains and flags, a monorail curving past, crowds on a sunny terrace."),
    "rocketrace": ("poster", "ROCKET RACE", "Two finned racing rockets, one red and one teal, looping around a "
                   "chequered pylon above a cheering stadium crowd, vapour trails, speed and excitement."),
}


def prompt(key):
    kind, words, brief = BRIEF[key]
    return (f"{FRAME[kind]} {brief} The only lettering is the words \"{words}\" in huge bold streamline capitals; no "
            f"other text anywhere. {STYLE}")


def gen(only):
    sys.path.insert(0, str(ROOT / "Tools/Concept"))
    from concurrent.futures import ThreadPoolExecutor
    from runware import infer

    todo = [k for k in BRIEF if (not only or k in only) and not (SRC / f"{k}.png").exists()]
    SRC.mkdir(parents=True, exist_ok=True)

    def one(k):
        wh = WIDE if BRIEF[k][0] == "ad" else TALL
        try:
            (png, cost, seed), = infer(MODEL, prompt(k), *wh)
        except Exception as e:  # noqa: BLE001 - one failed brief must not sink the batch
            print(f"[art] {k} FAILED: {e}")
            return None
        (SRC / f"{k}.png").write_bytes(png)
        return dict(file=f"signage_src/{k}.png", model=MODEL, seed=seed, cost=cost, size="x".join(map(str, wh)),
                    prompt=prompt(k), postprocess=f"packed into {ATLAS} by signage_art.py --pack", ok=True)
    with ThreadPoolExecutor(4) as ex:
        rows = [r for r in ex.map(one, todo) if r]
    prov = GEN / "PROVENANCE.json"
    data = json.loads(prov.read_text()) if prov.exists() else []
    data = [r for r in data if r["file"] not in {x["file"] for x in rows}] + rows
    prov.write_text(json.dumps(data, indent=1))
    for r in rows:
        print(f"[art] {r['file']:32} seed={r['seed']} ${r['cost']}")
    print(f"[art] batch ${sum(r['cost'] or 0 for r in rows):.4f}")


def fit(im, w, h):
    """Centre-crop to w:h then resize (no stretch)."""
    from PIL import Image
    a, b = im.width / im.height, w / h
    if a > b:
        cw = round(im.height * b)
        im = im.crop(((im.width - cw) // 2, 0, (im.width - cw) // 2 + cw, im.height))
    else:
        ch = round(im.width / b)
        im = im.crop((0, (im.height - ch) // 2, im.width, (im.height - ch) // 2 + ch))
    return im.convert("RGB").resize((w, h), Image.LANCZOS)


def board(w, h, lines, sc=4):
    """Backlit letter board: black stroke capitals centred on warm white, a thin red rule top and bottom."""
    from PIL import Image, ImageDraw
    import glyphs
    W, H = w * sc, h * sc
    im = Image.new("RGB", (W, H), (255, 246, 224))
    dr = ImageDraw.Draw(im)
    for y in (int(H * 0.04), int(H * 0.96)):
        dr.line([(0, y), (W, y)], fill=(196, 42, 31), width=int(H * 0.035))
    lh = H * 0.84 / len(lines)
    for i, text in enumerate(lines):
        placed, (ew, _) = glyphs.layout(text, gap=0.34)
        cap = min(lh * 0.62, W * 0.92 / ew)
        ox, oy = (W - ew * cap) / 2, H * 0.08 + lh * (i + 0.5) + cap / 2
        for strokes, (gx, gy) in placed:
            for st in strokes:
                pts = [(ox + (gx + x) * cap, oy - (gy + y) * cap) for x, y in glyphs.rounded(st, 0.14)]
                dr.line(pts, fill=(28, 26, 30), width=max(2, int(cap * 0.21)), joint="curve")
                for p in (pts[0], pts[-1]):
                    r = cap * 0.105
                    dr.ellipse((p[0] - r, p[1] - r, p[0] + r, p[1] + r), fill=(28, 26, 30))
    return im.resize((w, h), Image.LANCZOS)


def pack():
    from PIL import Image
    atlas = Image.new("RGB", (S, S), (243, 221, 176))
    for k, (x, y, w, h) in {**AD, **POSTER}.items():
        src = GEN / "poster_travel_v1.png" if k == "vesper" else SRC / f"{k}.png"
        if src.exists():
            atlas.paste(fit(Image.open(src), w, h), (x, y))
        else:
            print(f"[art] missing {src.name}: cell left blank")
    for k, ((x, y, w, h), lines) in BOARD.items():  # drawn upright h x w, rotated CCW into the w x h rect
        atlas.paste(board(h, w, lines).rotate(90, expand=True), (x, y))
    atlas.save(GEN / ATLAS)
    prov = GEN / "PROVENANCE.json"
    data = [r for r in json.loads(prov.read_text()) if r["file"] != ATLAS] if prov.exists() else []
    prov.write_text(json.dumps(data + [dict(
        file=ATLAS, model="(packed)", seed=None, cost=0.0, size=f"{S}x{S}", prompt="(see signage_src/* rows)",
        postprocess="signage_art.py --pack: signage_src ads centre-cropped 3:1 -> 1024x341, posters 2:3 -> 456x684 "
                    "(+ poster_travel_v1.png), procedural stroke-font letter boards (glyphs.py) rotated 90 CCW",
        ok=True)], indent=1))
    print(GEN / ATLAS, atlas.size)


if __name__ == "__main__":
    sys.path.insert(0, str(Path(__file__).parent))
    if "--gen" in sys.argv:
        o = sys.argv[sys.argv.index("--only") + 1].split(",") if "--only" in sys.argv else []
        gen(o)
    if "--pack" in sys.argv:
        pack()
