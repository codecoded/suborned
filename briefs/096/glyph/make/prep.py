"""Glyph, brief 096: clean base frames for the mocks. Real captures from out/batch/shots (0.2.234-era batch), HUD painted out by its own mask where a mock needs the bare street."""
import sys, numpy as np, cv2
from PIL import Image
R = '/Users/sky/Projects/suborned/'; S = R + 'out/batch/shots/'; O = R + 'briefs/096/glyph/make/bg/'

def clean(name, out, dx=0):
    im = cv2.imread(S + name + '.png'); m = (np.array(Image.open(S + name + '.hud.png').convert('RGBA'))[:, :, 3] > 16).astype(np.uint8) * 255
    m = cv2.dilate(m, np.ones((9, 9), np.uint8)); im = cv2.inpaint(im, m, 7, cv2.INPAINT_TELEA)
    if dx: im = np.roll(im, dx, axis=1)
    cv2.imwrite(O + out, im)

def copy(name, out): cv2.imwrite(O + out, cv2.imread(S + name + '.png'))

clean('start-overhead-day', 'start-clean.png')
clean('cafe-front-day', 'cafe-clean.png')
copy('start-overhead-day', 'start-hud.png')
copy('fight-safe-body', 'fight-hud.png')
copy('plate-pay-armed', 'pay-hud.png')
clean('fight-loud-checkpoint', 'bay-clean.png')
