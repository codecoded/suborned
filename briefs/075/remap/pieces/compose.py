"""Brief 075 remap (Vela): contact_sheet.png from the per-piece renders (review075_remap.py) and the lawn tile (lawn.png). python3 -I compose.py <dir>"""
import json, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
D = Path(sys.argv[1]); keys = json.loads((D / "keys.json").read_text())
ids = list(keys); tw, th, cols = 420, 315 + 44, 6
rows = (len(ids) + 1 + cols - 1) // cols
sheet = Image.new("RGB", (cols * tw, rows * th + 40), (24, 40, 40)); dr = ImageDraw.Draw(sheet)
try: f = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 15); fb = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 20)
except Exception: f = fb = ImageFont.load_default()
dr.text((10, 10), "Brief 075 remap: old pieces adopted at true size (each tile: old DecoKit left, ours right; red post 1.8 m) + the lawn", fill=(247, 244, 236), font=fb)
tiles = [(i, D / f"{i}.png") for i in ids] + [("lawn", D / "lawn.png")]
for k, (i, p) in enumerate(tiles):
    x, y = (k % cols) * tw, 40 + (k // cols) * th
    sheet.paste(Image.open(p).convert("RGB").resize((tw, 315)), (x, y))
    if i == "lawn":
        t1, t2 = "lawn: old lawn_v1 | before | now (stripes out)", "DK_Ground_Lawn, 8 m sheet, sm .12"
    else:
        a = keys[i]; s = a["size"]
        t1 = f"{i}  {s[0]:.2f} x {s[1]:.2f} x {s[2]:.2f} m"
        t2 = f"{a['use'][0]}  tris {a['tris_old']}/{a['tris_ours']}  LODs {'/'.join(map(str, a['lods'])) or '-'}"
    dr.text((x + 6, y + 318), t1, fill=(247, 244, 236), font=f); dr.text((x + 6, y + 338), t2, fill=(176, 205, 199), font=f)
sheet.save(D / "contact_sheet.png"); print("contact_sheet.png", sheet.size)
