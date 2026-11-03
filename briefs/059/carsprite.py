# Glyph (2026-10-25), brief 059: a stand-in sprite for the rival car, cut from the official car at the bay (briefs/044/build/done-call-window.png) and turned to the rivals' violet
# (style contract signal_colours.rivals). It is a board placeholder only: Vela's sedan in rival trim replaces it (briefs/059 §Glyph).
import numpy as np, cv2, colorsys
from PIL import Image, ImageDraw
src = Image.open("/Users/sky/Projects/suborned/briefs/044/build/done-call-window.png").convert("RGB")
box = (1480, 590, 1680, 780); crop = src.crop(box); k = 4
poly = [(125,240),(160,200),(240,160),(330,148),(385,168),(500,318),(645,488),(655,545),(635,595),(560,622),(440,634),(410,606),(375,600),(330,540),(240,440),(190,385),(160,345),(135,300),(125,270)]
m = Image.new("L", (crop.width * k, crop.height * k), 0); ImageDraw.Draw(m).polygon(poly, fill=255)
m = np.array(m.resize(crop.size, Image.LANCZOS)).astype(np.float32); m = cv2.GaussianBlur(m, (0, 0), 0.7)
a = np.array(crop).astype(np.float32) / 255; h = np.zeros(a.shape[:2]); 
hsv = cv2.cvtColor((a * 255).astype(np.uint8), cv2.COLOR_RGB2HSV).astype(np.float32)
# teal body (hue ~ 85 in opencv's 0..180) turns to the rivals' violet (hue of #8a4fd0 = 266 deg = 133); the cream roof stays, as a pale roof stripe
body = (hsv[..., 1] > 90) & (hsv[..., 0] > 60) & (hsv[..., 0] < 105)
hsv[..., 0][body] = 133; hsv[..., 1][body] = np.minimum(255, hsv[..., 1][body] * 0.95)
out = cv2.cvtColor(hsv.astype(np.uint8), cv2.COLOR_HSV2RGB); rgba = np.dstack([out, m.astype(np.uint8)])
im = Image.fromarray(rgba, "RGBA"); bb = im.getbbox(); im = im.crop(bb); im.save("/Users/sky/Projects/suborned/briefs/059/rival_car_standin.png"); print(im.size)
# the same cut, left as it is (Vale's official car), for the yard
a2 = np.dstack([np.array(crop), m.astype(np.uint8)]); Image.fromarray(a2, "RGBA").crop(bb).save("/Users/sky/Projects/suborned/briefs/059/official_car_cut.png")
