"""Brief 075 golden vista (Vela): compose <dir>/contact_sheet.png from review075_vista.py's renders and lods.json. python3 -I tools/decokit/review075_vista_sheet.py briefs/075/golden/vista"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

D = Path(sys.argv[1])
L = json.loads((D / "lods.json").read_text())
try:
    F = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial.ttf", 15); FB = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 20)
except OSError:
    F = FB = ImageFont.load_default()
W, INK = 330, (23, 48, 47)


def lab(k):
    r = L.get(k)
    return f"{r['tris']} tris · {r['size'][0]:.2f} x {r['size'][2]:.2f} m" if r else ""


rows = [("Arch: old | pier | ParabolicArch@crown-avenue L0 / L1 / L2", [("arch_old", "old DecoKit", "arch/old"), ("arch_pier", "pier ParabolicArch", "arch/pier"), ("arch_L0", "crown-avenue L0", "arch/L0"), ("arch_L1", "L1", "arch/L1"), ("arch_L2", "L2", "arch/L2")]),
        ("Tower: old | gate (kit) | DiscTower@crown-vista L0 / L1 / L2 (crown gold) | L0 livery gold", [("tower_old", "old DecoKit", "tower/old")] + ([("tower_oldhq", "old SpaceHQ (reel's gold)", "tower/oldhq")] if (D / "tower_oldhq.png").exists() else []) + [("tower_kit", "avenue-gate", "tower/kit"), ("tower_L0", "crown-vista L0", "tower/L0"), ("tower_L1", "L1", "tower/L1"), ("tower_L2", "L2", "tower/L2"), ("tower_L0-livery", "L0 livery #F2C14E", "tower/L0-livery")]),
        ("Wire pieces (DK_Iron wire, chrome, teal)", [("piece_Contact", "TramWire_Contact 1 m", "wire/Contact"), ("piece_Span", "TramWire_Span 1 m", "wire/Span"), ("piece_Hanger", "TramWire_Hanger", "wire/Hanger"), ("piece_Rosette", "TramWire_Rosette", "wire/Rosette"), ("piece_Pole", "TramWire_Pole", "wire/Pole")])]
small = [("wires_play", "Wires, about the play camera: 20 m street, rosettes, a pole pair, spans 30 m"), ("wires_street", "Wires at eye height"), ("wires_close_rosette", "Rosette and span, close"), ("wires_close_pole", "Pole collar and span, close")]
V2 = not (D / "wires_play.png").exists()  # vista 2 (--nowires): no wire rows; the arch at (0, 53), the tower at scale 1
if V2:
    rows, small = rows[:2], []
GOLD = next(q for q in (D.parent, D.parent.parent) if (q / "old.png").exists())
sheet = Image.new("RGB", (max(len(r[1]) for r in rows) * W + 20, 10 + len(rows) * (30 + W + 22) + 30 + 550 + (330 if small else 10)), (251, 248, 236)); d = ImageDraw.Draw(sheet); y = 10
for title, items in rows:
    d.text((10, y), title, fill=INK, font=FB); y += 30
    for i, (f, t, k) in enumerate(items):
        im = Image.open(D / f"{f}.png").convert("RGB"); im = im.resize((W, int(W * im.height / im.width))).crop((0, 0, W, W))
        sheet.paste(im, (10 + i * W, y)); d.text((14 + i * W, y + W + 2), f"{t}: {lab(k)}", fill=INK, font=F)
    y += W + 22
d.text((10, y), ("Vista 2 from the golden camera (ours): arch (0, 53) top 105.3, gate pair (±44, 190), crown-vista scale 1 (0, 350) tip 250, livery gold  |  old.png" if V2 else "Vista from the golden camera (ours): arch (0, 160), gate pair (±44, 190), crown-vista x 1.1 (0, 350), livery gold  |  old.png"), fill=INK, font=FB); y += 30
sheet.paste(Image.open(D / "vista.png").convert("RGB").resize((960, 540)), (10, y))
sheet.paste(Image.open(GOLD / "old.png").convert("RGB").resize((960, 540)), (980, y)); y += 550
for i, (f, t) in enumerate(small):
    im = Image.open(D / f"{f}.png").convert("RGB").resize((490, 368)).crop((0, 0, 490, 300)); sheet.paste(im, (10 + i * 500, y)); d.text((14 + i * 500, y + 302), t, fill=INK, font=F)
sheet.save(D / "contact_sheet.png")
print(D / "contact_sheet.png", sheet.size)
