# old Editor | new Editor | player: the home frame at half size, and the sun paving crop (500,560)-(800,700) at 2x, labelled.
import sys
from PIL import Image, ImageDraw
d = sys.argv[1]
names = [("editor-old-home.png", "old Editor (ARGB32, MSAA 4: clipped)"), ("editor-new-home.png", "new Editor (ARGBHalf + sRGB blit)"), ("player-home.png", "player (backbuffer)")]
ims = [Image.open(f"{d}/{n}").convert("RGB") for n, _ in names]
W, H = 960, 540; cw, ch = 600, 280
sheet = Image.new("RGB", (W * 3, 36 + H + 36 + ch), (24, 28, 30))
dr = ImageDraw.Draw(sheet)
for i, (im, (_, label)) in enumerate(zip(ims, names)):
    sheet.paste(im.resize((W, H), Image.LANCZOS), (i * W, 36))
    sheet.paste(im.crop((500, 560, 800, 700)).resize((cw, ch), Image.NEAREST), (i * W + (W - cw) // 2, 36 + H + 36))
    dr.text((i * W + 12, 10), label, fill=(240, 236, 226))
    dr.text((i * W + 12, 36 + H + 10), "sun paving (500,560)-(800,700), 2x", fill=(200, 200, 200))
sheet.save(f"{d}/old-new-player.jpg", quality=90)
