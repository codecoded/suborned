"""Boards for brief 028 "Palm LODs" (Vela, 2026-10-22). python3 tools/decokit/palm_lod_board.py <sweep_dir> <impostor_dir> <play_dir> <street_dir> <out_dir>

  board-1-levels.png   L0 | L1 | L2 (| impostor) at the distance each switches in, third person, the panel's pixel scale, haze as the game
  board-2-<view>.png   the boulevard: every palm L0 | with levels | the difference x4 | where each level sits, with palm triangle counts
  board-3-play.png     play zoom: the avenue as the game draws it (every palm L0) and each level at play zoom, with the rule
  board-4-pop.png      the switch: before | after | the difference x6 at the two switch distances, two azimuths, with the numbers
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).parent))
import palm_lod_metrics as M  # noqa: E402

SW, IMP, PLAY, STREET, OUT = (Path(a) for a in sys.argv[1:6]); OUT.mkdir(parents=True, exist_ok=True)
OLD = Path(sys.argv[6]) if len(sys.argv) > 6 else None   # the 20 Oct collapse levels (palm_lod_old.py)
F = lambda s, b=False: ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf" if b else "/System/Library/Fonts/Supplemental/Arial.ttf", s)  # noqa: E731
BG, FG, DIM, CORAL, TURQ, SUN = (24, 46, 46), (247, 244, 236), (176, 205, 199), (255, 106, 85), (96, 224, 234), (240, 160, 32)
TRIS = {"CoconutPalm": (6310, 1330, 514), "CoconutPalm_Street": (8230, 1330, 514)}
NAMES = {"CoconutPalm": "CoconutPalm", "CoconutPalm_Street": "CoconutPalm_Street"}


def img(arr):
    return Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8))


def bgcol(d):
    z = np.zeros((1, 1, 3)); return tuple(int(v) for v in (img(M.comp(z, np.zeros((1, 1)), d))).getpixel((0, 0)))


def palm_crop(d, piece, lv, az, scale, folder=SW, w=170, pad=8, vfrac=(0.0, 1.0), cxfrac=None):
    """The palm at distance d, hazed, cropped to the L0 palm's bounds (same box for every level; vfrac = the part of its height), enlarged by integer `scale` (nearest)."""
    rgb0, a0 = M.load(SW / f"{piece}_L0_d{d}_az{az}.png")
    ys, xs = np.where(a0 > 0.1); top, bot = ys.min() - pad, ys.max() + pad
    y0, y1 = int(top + (bot - top) * vfrac[0]), int(top + (bot - top) * vfrac[1])
    cx = (xs.min() + xs.max()) // 2 if cxfrac is None else int(xs.min() + (xs.max() - xs.min()) * cxfrac)
    rgb, a = M.load(folder / f"{piece}_L{lv}_d{d}_az{az}.png")
    c = M.comp(rgb, a, d)
    x0 = cx - w // 2
    crop = c[y0:y1, x0:x0 + w]
    return img(crop).resize((crop.shape[1] * scale, crop.shape[0] * scale), Image.NEAREST), int(ys.max() - ys.min() + 1)


def board1():
    # (piece, distance, scale, caption, levels, vertical part shown)
    rows = [("CoconutPalm_Street", 60, 3, "L1 takes over at 60 m: the crown, 3x (the fronds are the same; only the trunk differs)", [0, 1, 2], (0.0, 0.5)),
            ("CoconutPalm_Street", 140, 3, "L2 takes over at 140 m, 3x", [0, 1, 2, 9], (0.0, 1.0)),
            ("CoconutPalm", 60, 3, "L1 takes over at 60 m: the crown, 3x", [0, 1, 2], (0.0, 0.55)),
            ("CoconutPalm", 140, 4, "L2 takes over at 140 m, 4x", [0, 1, 2], (0.0, 1.0))]
    cw = 600
    chs = [int(r[2] * (319 if r[0].endswith("Street") else 237) * (r[5][1] - r[5][0]) * (60 / r[1]) + 40) for r in rows]
    W = 40 + 4 * (cw + 20); H = 110 + sum(chs) + len(rows) * 70
    b = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(b)
    d.text((20, 14), "Palm levels at the distance each takes over, third person (55 degrees, the Air's 1864 px panel, haze linear 90 to 350 m)", font=F(22, True), fill=FG)
    d.text((20, 50), "Every panel is the real pixels of that distance, enlarged by whole numbers (nearest), cropped to the same box. Blender, kit materials, one sun; the game adds its grade.", font=F(16), fill=DIM)
    d.text((20, 72), "L0 is the palm as built. The impostor is a trial of three crossed cards baked from L0: not proposed.", font=F(16), fill=DIM)
    y = 110
    for r, (piece, dist, scale, cap, lvs, vf) in enumerate(rows):
        ch = chs[r]
        d.text((20, y), f"{piece}   {dist} m   {cap}", font=F(19, True), fill=FG)
        for c, lv in enumerate(lvs):
            folder = IMP if lv == 9 else SW
            im, h = palm_crop(dist, piece, lv, 0, scale, folder=folder, w=cw // scale, vfrac=vf)
            x = 20 + c * (cw + 20)
            panel = Image.new("RGB", (cw, ch), bgcol(dist)); panel.paste(im, ((cw - im.width) // 2, 20))
            b.paste(panel, (x, y + 30))
            tri = "impostor trial 6 tris" if lv == 9 else f"L{lv}  {TRIS[piece][lv]:,} tris"
            d.text((x, y + 30 + ch + 4), tri, font=F(16, True), fill=FG)
        if dist == 60:  # the one thing L1 changes: the trunk's leaf-scar rings, 4x, lower trunk
            for c, lv in enumerate((0, 1)):
                im, h = palm_crop(dist, piece, lv, 0, 4, w=60, vfrac=(0.62, 0.86), cxfrac=0.5)
                x = 20 + 3 * (cw + 20) + c * 150
                panel = Image.new("RGB", (140, ch), bgcol(dist)); panel.paste(im, (max(0, (140 - im.width) // 2), 20)); b.paste(panel, (x, y + 30))
                d.text((x, y + 30 + ch + 4), f"trunk 4x  L{lv}", font=F(14, True), fill=FG)
        y += ch + 70
    b.save(OUT / "board-1-levels.png"); print("board-1", b.size)


def street_stats(view):
    a = np.asarray(Image.open(STREET / f"{view}_L0.png").convert("RGB")).astype(float)
    b = np.asarray(Image.open(STREET / f"{view}_LOD.png").convert("RGB")).astype(float)
    dm = np.abs(a - b).max(axis=2)
    return a, b, dm


def plan_map(lj, size, px=1.9):
    """The palms of one view from above: the camera at the foot, ahead up the page; coral L0, turquoise L1, white L2; rings at the two switch distances."""
    im = Image.new("RGB", size, (236, 217, 180)); dr = ImageDraw.Draw(im)
    cx, cy = size[0] // 2, size[1] - 20
    hd = np.radians(lj["heading"]); fx, fy = np.sin(hd), np.cos(hd)
    for R, col in ((60, CORAL), (140, TURQ), (350, (150, 130, 100))):
        dr.arc((cx - R * px, cy - R * px, cx + R * px, cy + R * px), 180, 360, fill=col, width=2)
    half = np.radians(42.8)
    for s in (-1, 1):
        dr.line((cx, cy, cx + s * np.sin(half) * 360 * px, cy - np.cos(half) * 360 * px), fill=(170, 150, 120), width=1)
    for r in lj["palms"]:
        vx, vy = r["x"] - lj["camera"][0], r["y"] - lj["camera"][1]
        fwd = vx * fx + vy * fy; side = vx * fy - vy * fx   # right = (fy, -fx)
        x, y = cx + side * px, cy - fwd * px
        col = {0: CORAL, 1: TURQ, 2: (255, 255, 255)}[r["level"]]
        dr.ellipse((x - 4, y - 4, x + 4, y + 4), fill=col, outline=(70, 60, 50))
    for R, txt in ((60, "60 m  L0 | L1"), (140, "140 m  L1 | L2"), (350, "350 m far clip")):
        dr.text((cx + 8, cy - R * px - 14), txt, font=F(14, True), fill=(70, 60, 50))
    return im


def board2(view, title):
    cnt = json.load(open(STREET / "counts.json")); lj = json.load(open(STREET / f"{view}_levels.json")); lv = lj["palms"]
    a, b, dm = street_stats(view)
    half = (1280, 720)
    A = Image.open(STREET / f"{view}_L0.png").convert("RGB"); B = Image.open(STREET / f"{view}_LOD.png").convert("RGB")
    diff = Image.fromarray(np.clip(np.abs(a - b) * 4, 0, 255).astype(np.uint8))
    mp = plan_map(lj, half)
    box = (777, 270, 1177, 520)
    L0c, LDc, Dc = A.crop(box), B.crop(box), diff.crop(box)
    key = f"{view}_"
    n0, n1 = cnt[key + "L0"], cnt[key + "LOD"]
    ndiff = float((dm > 12).mean()); mad = float(dm.mean())
    W = 2480; H = 120 + 2 * (half[1] + 50) + 560
    bd = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(bd)
    d.text((20, 14), f"{title}", font=F(22, True), fill=FG)
    d.text((20, 48), f"Third person, 55 degrees, 1920 x 1080 here, haze linear 90 to 350 m. Real placements from the world export; buildings are cream masses, roads and medians flat (the game's own are richer).", font=F(15), fill=DIM)
    d.text((20, 70), f"Palms in view {n0['palms']}.  Levels chosen by screen height as the LODGroup does: L0 {n1['levels']['0']}, L1 {n1['levels']['1']}, L2 {n1['levels']['2']}.  Palm triangles in view: {n0['tris']:,} all L0, {n1['tris']:,} with levels ({100 * n1['tris'] / n0['tris']:.0f} %).", font=F(16, True), fill=FG)
    pos = [(20, 100), (40 + half[0], 100), (20, 100 + half[1] + 50), (40 + half[0], 100 + half[1] + 50)]
    for (x, y), im, cap in zip(pos, (A, B, diff, mp), ("every palm at L0 (what the game draws today)", "with levels", f"difference x4 ({100 * ndiff:.3f} % of the frame's pixels differ by more than 12/255; mean {mad:.3f} of 255)", "plan: where each level sits (palms seen from above, the camera at the foot; rings at 60 m and 140 m)")):
        bd.paste(im.resize(half, Image.LANCZOS), (x, y + 24)); d.text((x, y), cap, font=F(17, True), fill=FG)
    y = 100 + 2 * (half[1] + 50) + 10
    d.text((20, y), "The far end of the avenue, native pixels enlarged 2x (palms from about 90 to 300 m): L0 | with levels | difference x4", font=F(17, True), fill=FG)
    for i, im in enumerate((L0c, LDc, Dc)):
        bd.paste(im.resize((800, 500), Image.NEAREST), (20 + i * 820, y + 28))
    bd.save(OUT / f"board-2-{view}.png"); print("board-2", view, bd.size, f"diff>12: {100 * ndiff:.2f}% mean {mad:.2f}")
    return dict(view=view, palms=n0["palms"], levels=n1["levels"], tris_L0=n0["tris"], tris_levels=n1["tris"], pix_diff_over_12=round(ndiff, 4), mean_abs_diff=round(mad, 3))


def board3():
    cnt = json.load(open(STREET / "counts.json")); n = cnt["play-market_LOD"]; sw = cnt["screen_relative_switch"]; size = cnt["size_m"]
    A = Image.open(STREET / "play-market_LOD.png").convert("RGB")
    cw = 580
    W = 40 + 1280 + 40 + 3 * (cw + 20); H = 130 + 760 + 420 + 420
    b = Image.new("RGB", (max(W, 2680), H), BG); d = ImageDraw.Draw(b)
    d.text((20, 14), "Play zoom: the levels never take over", font=F(22, True), fill=FG)
    d.text((20, 48), "Orthographic, half-height 14 m (the zoom runs 12.5 to 18.5), pitch 38, yaw 30, the Air's 1864 px: 66 px a metre. Unity's LODGroup reads an orthographic camera as size / (2 x half-height).", font=F(15), fill=DIM)
    d.text((20, 70), f"{n['palms']} palms in this frame, all L0 ({n['tris']:,} triangles, unchanged by the levels). Real placements, Market Street; the near buildings are hidden as the game's architecture fade does.", font=F(16, True), fill=FG)
    b.paste(A.resize((1280, 720), Image.LANCZOS), (20, 110))
    # the rule, as a table
    y = 110
    x = 1340
    d.text((x, y), "Screen-relative height of one palm at play zoom, against the switch", font=F(17, True), fill=FG); y += 30
    rows = [("", "zoom 12.5", "zoom 14", "zoom 18.5", "L1 below", "L2 below")]
    for k in ("CoconutPalm_Street", "CoconutPalm"):
        S_ = size[k]
        rows.append((k, *(f"{S_ / (2 * h):.3f}" for h in (12.5, 14, 18.5)), f"{sw[k][0]:.3f}", f"{sw[k][1]:.3f}"))
    for r, row in enumerate(rows):
        for c, cell in enumerate(row):
            d.text((x + (0 if c == 0 else 230 + (c - 1) * 105), y + r * 26), cell, font=F(15, r == 0), fill=FG if r == 0 or c == 0 else DIM)
    y += 3 * 26 + 10
    for line in ("A palm is never below 0.215 at the widest zoom, so L1 (0.17 and 0.127) is out of reach:", "the orthographic half-height would have to reach 31 m (the camera lab's 200 m maximum is not play).", "", "Why they stay out: at 66 px a metre the trunk's leaf-scar rings are 9 px apart and every frond is drawn", "at 200 to 250 px; L1 would drop the rings, L2 would show flat card edges (panels below, trunk and crown)."):
        d.text((x, y), line, font=F(15), fill=DIM); y += 22
    # each level at play zoom
    y0 = 110 + 720 + 30
    d.text((20, y0), "CoconutPalm_Street at play zoom 14, real pixels: L0 | L1 | L2 (shown only to prove the point; the game would draw L0)", font=F(17, True), fill=FG)
    for i, lv in enumerate((0, 1, 2)):
        im = Image.open(PLAY / f"CoconutPalm_Street_L{lv}_play14.png").convert("RGBA")
        bg = Image.new("RGBA", im.size, (236, 217, 180, 255)); bg.alpha_composite(im)
        c = bg.convert("RGB").crop((100, 140, 680, 620))
        x = 20 + i * (cw + 20)
        b.paste(c.resize((int(c.width * 0.62), int(c.height * 0.62)), Image.LANCZOS), (x, y0 + 30))
        tr = bg.convert("RGB").crop((250, 540, 450, 840)); b.paste(tr, (x + 370, y0 + 30))
        d.text((x, y0 + 30 + 320), f"L{lv}  {TRIS['CoconutPalm_Street'][lv]:,} tris   (crown 0.62x, trunk 1:1)", font=F(16, True), fill=FG)
    y1 = y0 + 30 + 350
    d.text((20, y1), "Close, third person at 25 m, real pixels (the levels never show this near; for the record): L0 | L1 | L2 crown", font=F(17, True), fill=FG)
    for i, lv in enumerate((0, 1, 2)):
        im, h = palm_crop(25, "CoconutPalm_Street", lv, 0, 1, w=cw, vfrac=(0.0, 0.40), pad=4)
        b.paste(im, (20 + i * (cw + 20), y1 + 30)); d.text((20 + i * (cw + 20), y1 + 30 + im.height + 4), f"L{lv}  {TRIS['CoconutPalm_Street'][lv]:,} tris", font=F(16, True), fill=FG)
    b.save(OUT / "board-3-play.png"); print("board-3", b.size)


def board4():
    mt = json.load(open(SW / "metrics.json"))
    rows = [("CoconutPalm_Street", 60, 1, 0, 2), ("CoconutPalm_Street", 140, 2, 0, 3), ("CoconutPalm", 60, 1, 0, 3), ("CoconutPalm", 140, 2, 0, 4)]
    pw = 380
    W = 40 + 3 * (pw + 20) + 760; chs = [int((350 if "Street" in r[0] else 260) * (60 / r[1]) * r[4] * 0.62 + 90) for r in rows]
    H = 110 + sum(chs) + len(rows) * 40
    b = Image.new("RGB", (W, H), BG); d = ImageDraw.Draw(b)
    d.text((20, 14), "The switch: before | after | difference x6, at the distance each takes over", font=F(22, True), fill=FG)
    d.text((20, 48), "Same pixels, same haze; azimuth 60 degrees (of 0, 60 and 120 measured). A pop is the difference between these two pictures: it is the whole of what the player could see change.", font=F(15), fill=DIM)
    y = 110
    for (piece, dist, lv, az, sc), ch in zip(rows, chs):
        before, after = lv - 1, lv
        vf = (0.0, 0.62) if dist == 60 else (0.0, 1.0)
        ims = []
        for l in (before, after):
            im, h = palm_crop(dist, piece, l, 60, sc, w=pw // sc, vfrac=vf); ims.append(im)
        # the difference, in the same crop
        arrs = []
        for l in (before, after):
            rgb0, a0 = M.load(SW / f"{piece}_L0_d{dist}_az60.png")
            ys, xs = np.where(a0 > 0.1); top, bot = ys.min() - 8, ys.max() + 8
            y0, y1 = int(top + (bot - top) * vf[0]), int(top + (bot - top) * vf[1]); cx = (xs.min() + xs.max()) // 2; x0 = cx - (pw // sc) // 2
            rgb, a = M.load(SW / f"{piece}_L{l}_d{dist}_az60.png"); arrs.append(M.comp(rgb, a, dist)[y0:y1, x0:x0 + pw // sc])
        diff = img(np.abs(arrs[0] - arrs[1]) * 6).resize((arrs[0].shape[1] * sc, arrs[0].shape[0] * sc), Image.NEAREST)
        rr = (10.65 if "Street" in piece else 7.94) / (2 * np.tan(np.radians(27.5)) * dist)
        d.text((20, y), f"{piece}   L{before} to L{after} at {dist} m   (LODGroup screen height {rr:.3f}, enlarged {sc}x)", font=F(18, True), fill=FG)
        for i, im in enumerate((ims[0], ims[1], diff)):
            x = 20 + i * (pw + 20)
            panel = Image.new("RGB", (pw, ch - 20), bgcol(dist) if i < 2 else (0, 0, 0)); panel.paste(im, ((pw - im.width) // 2, 8))
            lab = (f"L{before}  {TRIS[piece][before]:,} tris", f"L{after}  {TRIS[piece][after]:,} tris", "difference x6")[i]
            ImageDraw.Draw(panel).text((8, panel.height - 24), lab, font=F(15, True), fill=(60, 50, 40) if i < 2 else FG); b.paste(panel, (x, y + 30))
        sel = [r for r in mt if r["piece"] == piece and r["level"] == after and r["d"] == dist]
        if after == 1:
            sel1 = sel
        mean = lambda k: float(np.mean([r[k] for r in sel])); mx = lambda k: float(np.max([r[k] for r in sel]))  # noqa: E731
        x = 20 + 3 * (pw + 20)
        for j, line in enumerate((f"at {dist} m the palm is {int(np.mean([r['h'] for r in sel]))} px tall, haze {100 * sel[0]['fog']:.0f} %", f"silhouette changes by {100 * mean('sil'):.1f} % of its area (the floor is 3 to 4 %: leaflet edges)", f"pixels that differ by > 12/255 after blur 2 px: {100 * mean('pixb'):.1f} % (worst azimuth {100 * mx('pixb'):.1f} %)", f"mean colour of the palm moves {mean('tone'):.1f} / 255; its coverage is {100 * mean('cov'):.0f} % of L0's", "no change of size, lean, crown spread or colour: the difference is fine leaflet shading")):
            d.text((x, y + 40 + j * 28), line, font=F(16, j == 0), fill=FG if j == 0 else DIM)
        y += ch + 40
    b.save(OUT / "board-4-pop.png"); print("board-4", b.size)


def board5():
    """Before and after: the generator's 6 % collapse L2 against the authored L2, at 95, 140 and 200 m."""
    om = json.load(open(OLD / "metrics.json")); nm = json.load(open(SW / "metrics.json"))
    pw = 460
    rows = [(95, 3), (140, 4), (200, 5)]
    W = 40 + 3 * (pw + 20) + 520; H = 120 + sum(int(300 * 0.62 * 60 / dd * sc) // 1 + 90 for dd, sc in [(95, 2), (140, 3), (200, 3)]) + 40
    b = Image.new("RGB", (W, 1900), BG); d = ImageDraw.Draw(b)
    d.text((20, 14), "L2, before and after: the 20 Oct collapse (6 %) against the authored cards", font=F(22, True), fill=FG)
    d.text((20, 48), "CoconutPalm_Street, third person, real pixels enlarged by whole numbers, azimuth 0. Left to right: L0 (8,230) | 20 Oct L2 (485) | authored L2 (514).", font=F(15), fill=DIM)
    y = 90
    for dist, sc in [(95, 3), (140, 4), (200, 5)]:
        im0, h = palm_crop(dist, "CoconutPalm_Street", 0, 0, sc, w=pw // sc, vfrac=(0.0, 0.5))
        imo, _ = palm_crop(dist, "CoconutPalm_Street", 2, 0, sc, folder=OLD, w=pw // sc, vfrac=(0.0, 0.5))
        imn, _ = palm_crop(dist, "CoconutPalm_Street", 2, 0, sc, w=pw // sc, vfrac=(0.0, 0.5))
        ch = im0.height + 16
        d.text((20, y), f"{dist} m   (the palm is {h} px tall, haze {100 * M.fog(dist):.0f} %, enlarged {sc}x)", font=F(18, True), fill=FG)
        for i, (im, lab) in enumerate(((im0, "L0  8,230 tris"), (imo, "20 Oct L2  485 tris"), (imn, "authored L2  514 tris"))):
            x = 20 + i * (pw + 20)
            panel = Image.new("RGB", (pw, ch), bgcol(dist)); panel.paste(im, ((pw - im.width) // 2, 8)); ImageDraw.Draw(panel).text((8, ch - 22), lab, font=F(15, True), fill=(60, 50, 40)); b.paste(panel, (x, y + 28))
        o = [r for r in om if r["piece"] == "CoconutPalm_Street" and r["level"] == 2 and r["d"] == dist]; n = [r for r in nm if r["piece"] == "CoconutPalm_Street" and r["level"] == 2 and r["d"] == dist]
        x = 20 + 3 * (pw + 20)
        d.text((x, y + 34), "against L0 (mean of 3 azimuths)", font=F(15, True), fill=FG)
        for j, (lab, key, f) in enumerate((("silhouette error", "sil", 100), ("pixels changed after blur", "pixb", 100), ("foliage coverage", "cov", 100))):
            d.text((x, y + 62 + j * 26), f"{lab}:  20 Oct {f * np.mean([r[key] for r in o]):.1f} %   authored {f * np.mean([r[key] for r in n]):.1f} %", font=F(15), fill=DIM)
        y += ch + 70
    b = b.crop((0, 0, W, y)); b.save(OUT / "board-5-l2-before-after.png"); print("board-5", b.size)


if __name__ == "__main__":
    board1()
    board3()
    board4()
    if OLD:
        board5()
    stats = [board2("market", "Market Street from its west end: every palm at L0 | with distance levels"), board2("market-w", "Market Street from its east end: every palm at L0 | with distance levels")]
    (OUT / "street-stats.json").write_text(json.dumps(stats, indent=1))
