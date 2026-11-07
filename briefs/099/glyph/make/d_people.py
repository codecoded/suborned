"""Concept C · The people. The run told through who was in it: the cell's four cards (what each did, their numbers, health, who saw
them), then the people the run touched and what each will do now; the numbers in one slim band. A number opens on the people: Trail
shows each agent's share on their own card. Glyph, brief 099. Mock only."""
import sys, os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))  # python3 -I drops the script folder
from d_parts import *
from d_data import page, first
from d_reasons import trail_by_who, cell_line, stats, signed, city_lines
from d_words import DB
from slots import bust

FRAME = {'quiet-win.route-a': 'win', 'fail-lookout': 'fail', 'loud-win.seed3': 'loud'}
WATCHER = {'guard': NEW['w.guard'], 'camera pickup': NEW['w.camera'], 'patrol': NEW['w.patrol'], 'security': NEW['w.police'], 'camera entrance': NEW['w.camera']}


def people_of(M):
    """Who the run touched, by rule from the run: Popovic; the culprit of a fail; each contact kept; the guard; grief; the police;
    the rivals; the word going round. (face key or None, name, tag, hot, line, src)"""
    run, db, C = M['run'], M['run']['debrief'], M['city']; L = run['log']; out = []
    if M['kind'] == 'win':
        o = first(run, 'enter.out', 'board.in'); out.append(('councillor', 'Popovic', NEW['tag.in-car'], 0, f('p.vale.win', t=clock(o['tick'])), 'debrief route vale-reaches-getaway-car; log enter.out'))
    else: out.append(('councillor', 'Popovic', NEW['tag.taken'], 2, f('p.vale.fail', t=M['end']), 'cause fail rivals-reach-vale, culprits role=victim'))
    if M['kind'] == 'fail' and M['cause']['why'] == 'lookout':
        p = first(run, 'rivals.lookout-posted'); out.append(('lookout', NEW['p.lookout'], NEW['tag.followed'], 2, f('p.lookout.line', t=clock(p['tick'])), 'cause culprits role=reached; log rivals.lookout-posted'))
    for c in C['contacts']:
        who = c['who']; a = next((DB['assets'][x] for x in C['assets'] if x.startswith(who)), '')
        line = a.split(', ', 1)[-1].replace('though ', '').replace('who ', '').rstrip('.') if a else ''
        out.append((who, 'Inez' if who == 'waitress' else NEW['p.driver'], NEW['tag.turned'], 0, line, 'debrief nextMission.contacts; assets'))
    downs = {d for c in M['cell'] for d in c['downs']}
    if 'guard' in downs: out.append(('guard', NEW['p.guard'], NEW['p.guard.killed'] if C['cases'] else NEW['tag.down'], 2, f('p.guard.case', n=C['cases'][0]['missions']) if C['cases'] else '', 'debrief cases; log guard.down'))
    elif first(run, 'call.waitress'):
        s = next((x for c in M['cell'] for x in c['seen'] if x[1] == 'guard' and x[0] > 900), None)
        out.append(('guard', NEW['p.guard'], NEW['tag.drawn-off'], 0, f('p.guard.saw', op=[c['name'] for c in M['cell'] if s in c['seen']][0], t=clock(s[0])) if s else NEW['p.guard.by'], 'log call.waitress; run.seen'))
    for g in [x for x in C['lines'] if x['line'] == 'grief']:
        out.append(('waitress', 'Inez', NEW['tag.grieves'], 0, NEW['p.grief'], 'debrief nextMission.lines line=grief'))
    pol = [d for d in downs if d.startswith('security')]
    if pol: out.append((None, NEW['p.police'], f('p.n.down', n=len(pol)), 2, NEW['p.police.car'] if C['carSeenBy'] else '', 'shots by the cell; debrief carSeenBy'))
    riv = [d for d in downs if d.startswith('rival')]
    if riv and not pol: out.append((None, NEW['p.rivals'], f('p.n.down', n=len(riv)), 0, NEW['p.rivals.off'] if first(run, 'rivals.drives-off') else NEW['p.rivals.ran'], 'log rivals.down, rivals.drives-off'))
    if M['kind'] == 'fail':
        pat = [x for c in M['cell'] for x in c['seen'] if x[1] == 'patrol']
        if pat: out.append((None, NEW['p.patrol'], NEW['tag.saw'], 0, f('p.patrol.line', ops=', '.join(sorted({c['name'] for c in M['cell'] if any(x[1] == 'patrol' for x in c['seen'])}))), 'log attention.noticed (patrol)'))
    face_ = sum(1 for x in C['words'] if 'fixer-face' in x['detail'])
    if face_: out.append((None, f('p.people', n=face_), NEW['tag.knows-face'], 2, NEW['p.word'], 'debrief nextMission.lines line=word, detail fixer-face'))
    elif C['words']: out.append((None, f('p.people', n=len(C['words'])), NEW['p.talking'], 0, NEW['p.where'], 'debrief nextMission.lines line=word'))
    return out


def card(P, M, c, x, y, w, h, share=None, dim=False):
    """One of the cell: face over the card's edge, name, role and health, the numbers that aren't zero, who saw them, their moment."""
    g = (0xc8, 0xc4, 0xb8, 255); ink, soft = (g, g) if dim else (INK, SOFT)
    P.rrect(x, y, w, h, 20, fill=FOLD, line=ORANGE if share else None, lw=2.5); face(P, c['op'], x + 16, y - 34, 78, ring=g if dim else DEEP, rw=3, gray=dim)
    if share is not None:
        P.rrect(x + w - 92, y + 12, 80, 34, 17, fill=ORANGE); P.text(x + w - 52, y + 29, f('card.share', n=signed(share)), 19, col=CREAM, face='Jost-Bold', anchor='mm')
    P.put(x + 16, y + 66, c['name'], 25, col=ink, face='Jost-Bold'); P.put(x + 16, y + 92, c['role'], 17, col=soft)
    px0 = x + 18 + P.tw(c['role'], 17) + 8; step = min(13, (x + w - 14 - px0) / c['max']); pd = min(10, step - 2)  # health pips fit the card
    for k in range(c['max']): P.disc(px0 + k * step, y + 86 + (10 - pd) / 2, pd, fill=(g if dim else DEEP) if k < c['hp'] else None, ring=None if k < c['hp'] else soft, rw=1.3)
    yy = y + 126; rows = []
    if c['shots']: rows.append(('fire', f('card.hits', hits=c['hits'], shots=c['shots'])))
    if c['downs']: rows.append(('s_downed', f('card.down', n=len(c['downs']))))
    if c['taken']: rows.append(('s_hurt', f('card.hit', n=c['taken'])))
    if c['seen']: rows.append(('sawyou', f('card.seen', who=WATCHER.get(c['seen'][0][1], c['seen'][0][1])) + (f" +{len(c['seen']) - 1}" if len(c['seen']) > 1 else '')))
    for ic, t in rows[:3]:
        P.icon(ic, x + 16, yy - 10, 20, col=soft if dim else DEEP); P.put(x + 44, yy, t, fit(P, t, w - 56, (17.5, 16, 15), 'Jost-Medium'), col=ink); yy += 27
    t = cell_line(M, c); sz = 17 if len(wrap_lines(P, t, w - 30, 17, 'Jost-SemiBold')) <= 2 else 15.5
    put_wrap(P, x + 16, y + h - 46 - (sz < 17) * 8, t, w - 30, sz, col=ink, face_='Jost-SemiBold', maxl=3)


def person(P, x, y, w, pp, dim=False):
    """One person the run touched, as a column: face, the tag under it (what they are to us now), name, one line."""
    fk, name, tag, hot, line, _ = pp; d = 64; g = (0xc8, 0xc4, 0xb8, 255)
    if fk: face(P, fk, x, y, d, ring=g if dim else ORANGE if hot == 2 else DEEP, rw=2.5, gray=dim)
    else: bust(P, x, y, d, PALE)
    ts = fit(P, tag, w - 30, (15.5, 14.5, 13.5), 'Jost-SemiBold'); tw_ = P.tw(tag, ts, 'Jost-SemiBold') + 20
    P.rrect(x, y + d - 6, tw_, 25, 12.5, fill=g if dim else ORANGE if hot == 2 else DEEP); P.put(x + 10, y + d + 6.5, tag, ts, col=CREAM, face='Jost-SemiBold')
    P.put(x, y + d + 40, name, fit(P, name, w - 14, (20, 18, 16.5)), col=g if dim else INK, face='Jost-Bold')
    if line: put_wrap(P, x, y + d + 64, line, w - 16, 15.5, col=g if dim else SOFT, maxl=2)


def band(P, M, x, y, w, sel=None, dim=False):
    """The numbers in one line: icon, number, word; its reason under it (hover or the line)."""
    S = stats(M); cw = w / len(S); boxes = {}
    for i, s in enumerate(S):
        cx = x + i * cw; on = s['id'] == sel; hot = s['value'].startswith('Menace') or s['id'] == 'alert' and s['value'] != '0'
        g = (0xc8, 0xc4, 0xb8, 255); fade = dim and not on
        if on: P.rrect(cx - 10, y - 30, cw - 8, 96, 16, fill=PAGE, line=ORANGE, lw=2.5)
        P.icon(s['icon'], cx, y - 12, 24, col=g if fade else ORANGE if hot else DEEP)
        vs = fit(P, s['value'], cw - 40, (30, 26, 23)); P.put(cx + 32, y, s['value'], vs, col=g if fade else ORANGE if hot else INK, face='Jost-Bold')
        P.put(cx + 32, y + 26, s['label'], 15.5, col=g if fade else SOFT, face='Jost-SemiBold')
        if not dim: put_wrap(P, cx, y + 52, s['why'], cw - 18, 15, col=SOFT, maxl=2)
        boxes[s['id']] = (cx - 10, y - 30, cx + cw - 18, y + 66)
    return boxes


def people_page(M, detail=None):
    P = Page(2); light_page(P, FRAME[M['name']], M['subject']); tabs(P)
    y = head(P, M, sizes=(60, 54, 48), line_size=24)
    shares = dict(trail_by_who(M)) if detail == 'trail' else {}
    cy = max(y + 52, 222); w, h, gap = 178, 262, 12
    for i, c in enumerate(M['cell']):
        card(P, M, c, 1004 + i * (w + gap), cy, w, h, share=shares.get(c['op']) if detail else None, dim=bool(detail) and c['op'] not in shares)
    py = cy + h + 44; caps(P, 880, py, NEW['people.head'], 16, col=SOFT if detail else DEEP)
    PP = [pp for pp in people_of(M) if pp[0] != M['subject']][:5]; pw = (X1 - 880) / 5
    for i, pp in enumerate(PP): person(P, 880 + i * pw, py + 22, pw, pp, dim=bool(detail))
    by = py + 222; boxes = band(P, M, 880, by, X1 - 880, sel=detail, dim=bool(detail))
    if not detail:
        st = M['standing']; X = standing_bar(P, 1010, by + 118, 300, st)
        P.put(880, by + 125, NEW['stat.standing'], 17, col=SOFT, face='Jost-SemiBold')
        inv = city_lines(M)[0]; P.icon(inv[0], 1350, by + 110, 24, col=DEEP); P.put(1384, by + 122, inv[1], 19, col=INK, face='Jost-SemiBold')
        put_wrap(P, 1384, by + 148, inv[2], X1 - 1384, 16, col=SOFT, maxl=1)
    if M['kind'] == 'fail' and not detail:
        P.put(880, by + 186, NEW['next.head'] + ':', 19, col=ORANGE, face='Jost-Bold'); P.put(880 + P.tw(NEW['next.head'] + ':', 19) + 10, by + 186, M['next'], 19, col=INK, face='Jost-SemiBold')
    if detail == 'trail':
        b = boxes['trail']; car = shares.get('car', 0)
        txt = f"Trail {signed(M['run']['trail'])}: {NEW['trail.by.who'].lower()}. " + (f"{NEW['short.car-seen'].capitalize()}: +{car}. " if car else '') + NEW['trail.law']
        panel(P, 880, by + 72, X1 - 880, 96, tail=((b[0] + b[2]) / 2, True)); put_wrap(P, 906, by + 104, txt, X1 - 940, 19, col=INK, face_='Jost-SemiBold', maxl=2)
    ways_on(P, M['seed'])
    return P


if __name__ == '__main__':
    for n, det, out in (('quiet-win.route-a', None, 'C-people-win'), ('fail-lookout', None, 'C-people-fail'), ('loud-win.seed3', 'trail', 'C-people-detail')):
        if len(sys.argv) > 1 and out not in sys.argv[1:]: continue
        P = people_page(page(n), det); save(P, out); print(out, P.words, 'words')
