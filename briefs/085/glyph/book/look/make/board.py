import sys, shutil
sys.path.insert(0, '.')
from proof import *
PAID = R + 'paid/085-book/codex/'
for f in ['A-case-file', 'A-case-file-job', 'A-case-file-cell', 'B-wall', 'C-magazine', 'D-machine', 'D-machine-job', 'D-machine-cell']:
    shutil.copy(PAID + f + '.png', OUT + 'concept-' + f + '.png')
W_ = 1000
def bar(t, subs=(), tag=None):
    f1, f2 = font('Jost-Bold', 52), font('Jost-Bold', 35); im = Image.new('RGB', (W_, 88 + 50 * len(subs) + 14), BAND[:3]); d = ImageDraw.Draw(im); d.text((30, 18), t, font=f1, fill=CREAM)
    for i, s_ in enumerate(subs): d.text((30, 86 + i * 50), s_, font=f2, fill=DIM)
    if tag:
        tw = f2.getlength(tag); d.rounded_rectangle((W_ - tw - 66, 22, W_ - 24, 82), 14, outline=TURQ, width=4); d.text((W_ - tw - 45, 30), tag, font=f2, fill=TURQ)
    return im
def pic(p, w=W_):
    im = Image.open(p).convert('RGB'); return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)
def pair(a, b):
    im = Image.new('RGB', (W_, 281), BAND[:3]); im.paste(pic(a, 496), (0, 0)); im.paste(pic(b, 496), (504, 0)); return im
p = proof = inez(); big = proof.img.convert('RGB')
def crop(box, w=W_):
    x0, y0, x1, y1 = box; c = big.crop((x0 * S, y0 * S, x1 * S, y1 * S)); return c.resize((w, round(c.height * w / c.width)), Image.LANCZOS)
parts = [bar('The book: four objects', ["Same moment: Inez, just turned.", "Lettering in the art is made up. Judge the feel."])]
parts += [bar('A · The case file', ['A dossier on the table. Clear and roomy.'], tag='Pick this'), pic(OUT + 'concept-A-case-file.png'), pair(OUT + 'concept-A-case-file-job.png', OUT + 'concept-A-case-file-cell.png')]
parts += [bar('B · The wall', ['Shows the network. Busiest, and it floats.']), pic(OUT + 'concept-B-wall.png')]
parts += [bar('C · The picture magazine', ['The prettiest. Needs a painting per person.']), pic(OUT + 'concept-C-magazine.png')]
parts += [bar('D · The machine', ['Most of this world. One small card to read.'], tag='Second'), pic(OUT + 'concept-D-machine.png'), pair(OUT + 'concept-D-machine-job.png', OUT + 'concept-D-machine-cell.png')]
parts += [bar('A, with true type and real facts', ['Inez at 2:10. This one is real words.']), crop((0, 0, 1920, 1080)), crop((100, 20, 930, 1020)), crop((930, 20, 1740, 500)), crop((930, 450, 1740, 1020)), crop((1700, 0, 1920, 1040), 420)]
H = sum(x.height for x in parts); B = Image.new('RGB', (W_, H), BAND[:3]); y = 0
for x in parts:
    B.paste(x, (0, y)); y += x.height
B.save(OUT + 'board.png', optimize=True); print(B.size)
