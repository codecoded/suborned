import sys, json
sys.path.insert(0, '.')
from pages import *
sets = {'cell-page': cell_page, 'person-opening-guard': guard_page, 'person-inez-turned': inez_page, 'job-page': job_page}
out = {}
for n, f in sets.items():
    P = f(); im = P.img.resize((1920, 1080), Image.LANCZOS).convert('RGB'); im.save(OUT + n + '.png'); out[n] = P; print(n, 'words', P.words)
# the phone board: each page, then its parts big enough to read
def crop(P, box, width=1000):
    x0, y0, x1, y1 = box; s = P.s; im = P.img.crop((x0 * s, y0 * s, x1 * s, y1 * s)).convert('RGB'); k = width / im.width; return im.resize((width, round(im.height * k)), Image.LANCZOS)
def bar(t, sub=None):
    f1, f2 = font('Jost-Bold', 54), font('Jost-Bold', 36); im = Image.new('RGB', (1000, 96 + (52 if sub else 0)), BAND[:3]); d = ImageDraw.Draw(im); d.text((32, 20), t, font=f1, fill=CREAM)
    if sub: d.text((32, 92), sub, font=f2, fill=DIM)
    return im
parts = [bar('The book: a first look', 'Opens over the street. The clock stops.')]
parts += [bar('Your cell', 'One agent a page: what they can do now'), crop(out['cell-page'], (0, 0, 1920, 1080)), crop(out['cell-page'], (40, 100, 800, 800)), crop(out['cell-page'], (810, 100, 1880, 810))]
parts += [bar('A person, at the start', 'Unknowns stay "?"'), crop(out['person-opening-guard'], (0, 0, 1920, 1080)), crop(out['person-opening-guard'], (40, 100, 880, 1000)), crop(out['person-opening-guard'], (890, 100, 1880, 800))]
parts += [bar('Inez, turned', 'After Listen and two Helps'), crop(out['person-inez-turned'], (0, 0, 1920, 1080)), crop(out['person-inez-turned'], (400, 100, 1410, 800)), crop(out['person-inez-turned'], (1430, 100, 1880, 830))]
parts += [bar('The job', 'Mission facts, done and past marked'), crop(out['job-page'], (0, 0, 1920, 1080)), crop(out['job-page'], (40, 100, 650, 720)), crop(out['job-page'], (650, 100, 1400, 720)), crop(out['job-page'], (1400, 100, 1880, 700)), crop(out['job-page'], (40, 740, 1000, 960)), crop(out['job-page'], (960, 740, 1920, 960))]
H = sum(p.height for p in parts); W = Image.new('RGB', (1000, H)); y = 0
for p in parts: W.paste(p, (0, y)); y += p.height
W.save(OUT + 'board.png', optimize=True); print('board', W.size)
