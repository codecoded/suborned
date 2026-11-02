"""The phone board for round two of the book's look: the concepts as generated, with a thumbnail test. Glyph, brief 085."""
import shutil, sys
sys.path.insert(0, '/Users/sky/Projects/suborned/briefs/085/glyph/people-panel/make')
from kit import *
R = '/Users/sky/Projects/suborned/'; P = R + 'paid/085-book/codex-look3/'; O = R + 'briefs/085/glyph/book/look2/'
BAND = (12, 52, 52)
names = ['turntable', 'turntable-unknown', 'turntable-job', 'turntable-cell', 'light', 'light-unknown', 'poster', 'atom', 'console']
for n in names: shutil.copy(P + n + '.png', O + n + '.png')
W = 1000
def bar(t, subs=(), tag=None):
    f1, f2 = font('Jost-Bold', 52), font('Jost-Bold', 35); im = Image.new('RGB', (W, 88 + 50 * len(subs) + 14), BAND); d = ImageDraw.Draw(im); d.text((30, 18), t, font=f1, fill=CREAM)
    for i, s in enumerate(subs): d.text((30, 86 + i * 50), s, font=f2, fill=DIM)
    if tag:
        tw = f2.getlength(tag); d.rounded_rectangle((W - tw - 66, 22, W - 24, 82), 14, outline=TURQ, width=4); d.text((W - tw - 45, 30), tag, font=f2, fill=TURQ)
    return im
def pic(n, w=W): im = Image.open(O + n + '.png').convert('RGB'); return im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)
def row(ns):
    w = (W - 8 * (len(ns) - 1)) // len(ns); h = round(w * 941 / 1672); im = Image.new('RGB', (W, h), BAND)
    for i, n in enumerate(ns): im.paste(pic(n, w), (i * (w + 8), 0))
    return im
parts = [bar('The book, round two', ['Inez, just turned, over the frozen street.', 'Lettering and numbers in the art are made up.'])]
parts += [bar('Thumbnail test', ['Does it say retro future this small?']), row(['turntable', 'light', 'poster']), row(['atom', 'console'])]
parts += [bar('The turntable', ["A world's fair stage. Your person turns", 'into view, in their real game figure.'], tag='Pick this'), pic('turntable')]
parts += [bar('Someone unknown', ['Question marks, still handsome.']), pic('turntable-unknown'), bar('The job', ['The stage rim is the clock.']), pic('turntable-job'), bar('Your cell', ['All four on stage.']), pic('turntable-cell')]
parts += [bar('The light page', ['Clearest, easiest, street stays in view.', 'Less of an object once built.'], tag='Second'), pic('light'), pic('light-unknown')]
parts += [bar('The tomorrow poster', ['Loudest, but a different game from the street.']), pic('poster')]
parts += [bar('Rejected: the atom', ['Right idea, hard to read. Maybe for the', 'network view later.']), pic('atom')]
parts += [bar('Rejected: the console', ['Clear, but says old car, not tomorrow.']), pic('console')]
H = sum(p.height for p in parts); B = Image.new('RGB', (W, H), BAND); y = 0
for p in parts: B.paste(p, (0, y)); y += p.height
B.save(O + 'board.png', optimize=True); print(B.size)
