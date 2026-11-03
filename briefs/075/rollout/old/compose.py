"""Compose the per-class renders into contact_sheet.png (4 columns, labelled). python3 -I compose.py <dir>"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
d = Path(sys.argv[1])
order = ["S2", "S3", "S3W", "S4", "S6", "S9", "S9W", "D10", "P3", "P4", "P7", "P7-new", "B3", "B4", "B8", "B8-new", "A3", "H", "S6-tower"]
tw, th = 640, 320
cols = 4; rows = (len(order) + cols - 1) // cols
sheet = Image.new("RGB", (cols * tw, rows * (th + 26)), (250, 248, 240))
dr = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 18)
except Exception:
    font = ImageFont.load_default()
for i, n in enumerate(order):
    im = Image.open(d / f"{n}.png").convert("RGB").resize((tw, th), Image.LANCZOS)
    x, y = (i % cols) * tw, (i // cols) * (th + 26)
    sheet.paste(im, (x, y + 26))
    dr.text((x + 8, y + 4), n + "  (A: corner + 3 bays + end_M · B: end + 3 bays + corner_M)", fill=(20, 40, 40), font=font)
sheet.save(d / "contact_sheet.png")
print(d / "contact_sheet.png", sheet.size)
