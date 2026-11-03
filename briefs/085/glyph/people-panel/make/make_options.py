import json, sys
sys.path.insert(0, '.')
from base import *
from options import *

shots = {}
def out(name, world, L, base, state='rest', keep_plate=False):
    shots[name] = (world, L); save(name, world, L, base, state=state, keep_plate=keep_plate)

fo, fl, fd = frame_open(), frame_learned(), frame_door()
INEZ_O, INEZ_L = (1293, 62), (962, 462)
cursor_at = None

# A
L = Layer(); option_a(L, OPENING); L.ground_ring(1296, 136, 28); out('A-dossier-opening', fo, L, 'open')
L = Layer(); spoken_learned(L); option_a(L, LEARNED); L.ground_ring(963, 532, 28); out('A-dossier-learned', fl, L, 'learned')

# B
chip = rgb(B + 'real-player-overhead.png').crop((806, 232, 1115, 362))
L = Layer(); option_b(L, OPENING, INEZ_O, (1090, 66)); L.ground_ring(1296, 136, 28); out('B-street-opening', fo, L, 'open', keep_plate=True)
L = Layer(); x0, w = b_tag(L, RX, 762, OPENING['guard'], 'guard', arrow=135); b_tag(L, RX, 762 + 56, OPENING['driver'], 'driver', arrow=135); b_tag(L, RX, 762 + 112, OPENING['councillor'], 'councillor', arrow=135)
xc = x0 - RW - 12; yc = 590
L.nine('panel_dark', xc, yc, RW, 183); card_body(L, xc + 20, yc + 15, RW - 40, OPENING['guard'], 'guard')
L.note(xc, yc, RW, 183, 'chip', facts=[f[2] for f in OPENING['guard']['facts']], verbs=['Watch', 'Listen'])
L.line([(xc + RW, yc + 150), (x0, 786)], col=CREAM, lw=1.6)
bx0, bw = b_tag(L, 1090, 66, OPENING['waitress'], 'waitress'); stem(L, 1210, 114, 1293, 62); L.ground_ring(1296, 136, 28)
out('B-street-hover', fo, L, 'open', state='active', keep_plate=True)
L = Layer(); spoken_learned(L)
L.paste(chip.convert('RGBA').resize((chip.width * 2, chip.height * 2), Image.LANCZOS), 806, 232); L.note(806, 232, 309, 130, 'chip', facts=['Bureau loan', 'Fearful', 'Loves guard'], verbs=['Pay', 'Help', 'Pressure'])
b_tag(L, 944, 398, LEARNED['waitress'], 'waitress', icons=['help', 'pressure'], face=False); stem(L, 963, 446, 963, 462)
for i, r in enumerate(('guard', 'driver', 'councillor')): b_tag(L, RX, 762 + i * 56, LEARNED[r], r, arrow=135)
L.ground_ring(963, 532, 28); out('B-street-learned', fl, L, 'learned', keep_plate=True)

# C
L = Layer(); c_rail(L, OPENING, 'waitress'); x, y, H = c_card(L, OPENING, 'waitress'); stem(L, x, y + 60, 1307, 94); L.ground_ring(1296, 136, 28)
out('C-rail-opening', fo, L, 'open')
L = Layer(); spoken_learned(L); c_rail(L, LEARNED, 'waitress'); x, y, H = c_card(L, LEARNED, 'waitress', price=[(None, 'Help'), ('p_cash', '$20k'), ('p_time', '35 s'), ('p_trail', '+1')]); stem(L, x, y + 110, 985, 478); L.ground_ring(963, 532, 28)
out('C-rail-learned', fl, L, 'learned', state='active')
L = Layer(); c_rail(L, LEARNED, 'guard'); x, y, H = c_card(L, LEARNED, 'guard'); stem(L, x, y + 60, 975, 430); L.ground_ring(960, 486, 28)
out('C-rail-click-guard', fd, L, 'learned', state='active')

# for the notes: the rail alone at rest, and the learned card with no hover
L = Layer(); c_rail(L, OPENING); save('zz-C-rail-only', fo, L, 'open'); import os; os.remove(OUT + 'zz-C-rail-only.png')
L = Layer(); spoken_learned(L); c_rail(L, LEARNED, 'waitress'); c_card(L, LEARNED, 'waitress'); save('zz-C-learned-rest', fl, L, 'learned'); os.remove(OUT + 'zz-C-learned-rest.png')
L = Layer(); spoken_learned(L); c_rail(L, LEARNED); save('zz-C-learned-folded', fl, L, 'learned'); os.remove(OUT + 'zz-C-learned-folded.png')
