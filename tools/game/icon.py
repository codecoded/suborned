# The game app's icon (tools/game/app-icon.png, 1024 px): the Tech's chosen portrait (briefs/012/cell/tech-final, Forma), face cropped round
# on the HUD's ink with its turquoise ring. Run once: python3 -I tools/game/icon.py <repo>. GameBuild turns it into the app's .icns.
import sys
from PIL import Image, ImageDraw
repo = sys.argv[1]
src = Image.open(f"{repo}/briefs/012/cell/tech-final/TF-portrait-tech-white-chosen.png").convert("RGB")
face = src.crop((190, 0, 830, 640)).resize((760, 760), Image.LANCZOS)
S = 1024; out = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(out)
d.rounded_rectangle((60, 60, S - 60, S - 60), radius=190, fill=(18, 48, 46, 255))      # ink
c0 = (S - 760) // 2
mask = Image.new("L", (760, 760), 0); ImageDraw.Draw(mask).ellipse((0, 0, 759, 759), fill=255)
out.paste(face, (c0, c0), mask)
d.ellipse((c0 - 10, c0 - 10, c0 + 770, c0 + 770), outline=(96, 224, 234, 255), width=22)   # the selection turquoise
out.save(f"{repo}/tools/game/app-icon.png")
print("tools/game/app-icon.png")
