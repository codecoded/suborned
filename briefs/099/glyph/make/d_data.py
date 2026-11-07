"""A played run -> the debrief page's facts. Every number keeps its source (an engine field, a ledger row or a log code) in `src`,
so notes.md can list them. Words come from text.json where Fable has written them (T), else from d_words.NEW (stand-ins for Fable).
Glyph, brief 099. Mock tool only; nothing here ships."""
import json, re
from d_words import NEW, T, DB, BOOK, NAME, ROLE, OPS, who_words

R = '/Users/sky/Projects/suborned/'
RUNS = R + 'briefs/099/glyph/runs/'
SHOT = re.compile(r'^(\S+) fires on (\S+) at (\d+) m \((\d+) ‰\): (hit|miss)')


def clock(tick):
    s = tick // 10; return f'{s // 60}:{s % 60:02d}'


def load(name):
    r = json.load(open(RUNS + name + '.json')); r['name'] = name; return r


def first(run, *codes, pred=None):
    for l in run['log']:
        if l['code'] in codes and (pred is None or pred(l)): return l
    return None


def shots(run):
    """Per agent: shots fired, hits; and who hit each target last (for downs). Log codes shot.hit / shot.miss, by shooter."""
    per = {o: dict(shots=0, hits=0, downs=[], taken=0) for o in OPS}; last = {}; out = []; lastt = None
    for l in run['log']:
        if l['code'] in ('shot.hit', 'shot.miss'):
            m = SHOT.match(l['text'])
            if not m: continue
            by, at, hit = m.group(1), m.group(2), m.group(5) == 'hit'; out.append((l['tick'], by, at, hit))
            if by in per: per[by]['shots'] += 1; per[by]['hits'] += hit
            if hit and by in per: last[at] = by
            if hit and at in per: per[at]['taken'] += 1
            if hit: lastt = at
        downed = None
        if l['code'] == 'fight.down': downed = l['text'].rstrip('.').split(' drops ')[-1]
        elif l['code'] == 'rivals.down': downed = l['text'].split(' ')[0]
        elif l['code'] == 'guard.down': downed = 'guard'
        elif l['code'] == 'security.officer-down': downed = l['text'].split(' ')[0]
        elif l['code'] == 'security.down': downed = lastt
        if downed and downed in last:
            o = last[downed]
            if downed not in per[o]['downs'] and not any(downed in per[p]['downs'] for p in OPS): per[o]['downs'].append(downed)
    return per, out


WATCH = [(re.compile(r'^The (guard)(?: [A-Z]\w+ [A-Z]\w+)? sees the (scout|fixer|tech|enforcer) running'), 'running'),
         (re.compile(r'^(?:the )?(camera \w+|patrol|guard) notices the (scout|fixer|tech|enforcer) (standing still|running)'), None),
         (re.compile(r"^The (security) sees the (scout|fixer|tech|enforcer)'s weapon drawn"), 'gun drawn')]


def seen(run):
    """Who noticed each agent (log run.seen, attention.noticed, drawn.seen), and the reports that landed about them (attention.landed)."""
    per = {o: [] for o in OPS}
    for l in run['log']:
        for rx, how in WATCH:
            m = rx.match(l['text'])
            if m and l['code'] in ('run.seen', 'attention.noticed', 'drawn.seen'):
                w, o = m.group(1), m.group(2); h = how or m.group(3)
                if not any(x[1] == w for x in per[o]): per[o].append((l['tick'], w, h))
    reports = [l for l in run['log'] if l['code'] == 'attention.landed']
    return per, reports


def trail_rows(run, sh):
    """The ledger's Trail rows, each with the agent behind it: a shooting row by the shot at that tick (gunfire, hit), or the last hit
    before it (down); an act by who ordered it (order.* log lines)."""
    by_order = {}
    for l in run['log']:
        if l['code'].startswith('order.') and l['code'] not in ('order.move', 'order.rejected'):
            by_order.setdefault(l['code'][6:], l['text'].split(' ')[0])
    rows = []
    for e in run['ledger']:
        if e['what'] != 'trail': continue
        c = e['cause']; kind, _, tgt = c.partition(':'); who = None
        if kind in ('gunfire', 'hit'):
            cand = [s for s in sh if s[2] == tgt and s[0] <= e['tick'] and (kind == 'gunfire' or s[3])]
            who = cand[-1][1] if kind == 'hit' and cand else (cand[0][1] if cand else None)
            if kind == 'gunfire': who = next((s[1] for s in sh if s[2] == tgt), who)
        elif kind == 'down':
            cand = [s for s in sh if s[2] == tgt and s[3] and s[0] <= e['tick']]; who = cand[-1][1] if cand else None
        elif kind in ('loop', 'reroute'): who = by_order.get(kind)
        elif kind.startswith('turn.'): who = 'fixer'
        elif kind == 'call': who = 'fixer'
        elif kind == 'recorded': who = 'fixer'
        rows.append(dict(tick=e['tick'], delta=e['delta'], cause=c, kind=kind, tgt=tgt, who=who))
    return rows


def spent(run):
    """Cash: the engine reports what is left (field cash); the purse is $100,000 (sim/data). No cash rows in the ledger today (ask Keel)."""
    paid = [l for l in run['log'] if re.match(r'turn\.(pay|help)\.turned', l['code'])]
    return 100000 - run['cash'], paid


def standing(run):
    db = run['debrief']; st = (db.get('nextMission') or {}).get('standing') or [{}]; s = st[0]
    moves = [(e['tick'], e['delta'], e['cause']) for e in run['ledger'] if e['what'] == 'standing']
    nxt = [(m['delta'], m['cause'], m['who']) for m in s.get('moves', [])]
    return dict(start=s.get('from', 2), end=run['standing'], next=s.get('to', run['standing']), band=s.get('band', 'neutral'), moves=moves,
                between=[m for m in nxt if m[1].startswith('word') or m[1].startswith('drift')], cause=s.get('cause', ''))


def way_of(run):
    if first(run, 'turn.pay.turned', 'turn.help.turned', pred=lambda l: 'driver' in l['text']): return 'pickup'
    return 'front-door'


def conditions(run, kind):
    """The start page's own rows for the way taken (book.job.needs.ways), answered from the log: when each came true, and how."""
    out = []; L = lambda *c, **k: first(run, *c, **k)
    if way_of(run) == 'front-door':
        g = L('call.waitress', 'guard.down', 'guard.knocked-out', 'takedown.done')
        how = {'call.waitress': NEW['how.called'], 'guard.down': NEW['how.shot']}.get(g['code'], '') if g else ''
        out.append(('guard-away', bool(g), g and clock(g['tick']), how))
        lp = L('loop.on'); out.append(('door-camera', bool(lp), lp and clock(lp['tick']), NEW['how.looped'] if lp else ''))
        e = L('enter.in'); out.append(('inside', bool(e), e and clock(e['tick']), NEW['how.in']))
    else:
        d = L('turn.pay.turned', 'turn.help.turned', pred=lambda l: 'driver' in l['text'])
        out.append(('driver', bool(d), d and clock(d['tick']), NEW['how.paid'] if d and 'pay' in d['code'] else NEW['how.helped']))
        rec = L('camera.recorded', pred=lambda l: 'pickup' in l['text'])
        out.append(('pickup-camera', False, rec and clock(rec['tick']), NEW['how.taped'] if rec else ''))
        tk = L('take.done', 'board.in', pred=lambda l: 'Popovic' in l['text'])
        out.append(('has-vale', bool(tk), tk and clock(tk['tick']), ''))
    b = L('board.in', pred=lambda l: 'Popovic' in l['text'])
    out.append(('getaway', bool(b), b and clock(b['tick']), ''))
    words = {i['id']: i['words'] for i in BOOK['job']['needs']['ways'][way_of(run)]}
    ex = lambda w: w.replace('{t:valeExits}', '5:30').replace('{t:driverLast}', '5:20')
    return [(cid, ok, t, how, ex(words[cid])) for cid, ok, t, how in out]


def beats(run, kind):
    """The run on one strip: the mission's own stops (book.job.stops log codes), the cell's acts, the city's answers. (tick, lane, icon, word, op, hot)."""
    B = []; seen_codes = set()
    def add(code, lane, icon, word, op=None, hot=0, pred=None, once=True):
        for l in run['log']:
            if l['code'] == code and (pred is None or pred(l)):
                if once and code in seen_codes: return
                seen_codes.add(code); B.append((l['tick'], lane, icon, word, op, hot))
                if once: return
    add('scout.watch.watched', 'you', 'watch', NEW['beat.watched'], 'scout')
    add('scout.listen.heard', 'you', 'listen', NEW['beat.listened'], 'scout')
    add('turn.help.turned', 'you', 'turned', NEW['beat.turned'].format(who=who_words('waitress')), 'fixer')
    add('turn.pay.turned', 'you', 'turned', NEW['beat.turned'].format(who='Driver'), 'fixer')
    add('loop.on', 'you', 'loop', NEW['beat.loop'], 'tech')
    add('patrol.rerouted', 'you', 'reroute', NEW['beat.reroute'], 'tech')
    add('call.waitress', 'city', 's_calling', NEW['beat.call'])
    add('enter.in', 'you', 'enter', NEW['beat.in'], 'enforcer')
    add('enter.out', 'mission', 's_carrying', NEW['beat.out'])
    add('rivals.coming', 'mission', 'car', NEW['beat.rivals'])
    add('rivals.lookout-posted', 'city', 'sawyou', NEW['beat.lookout'], hot=2 if kind == 'fail' else 0)
    al = first(run, 'attention.landed', 'report.landed')
    if al and al['code'] == 'attention.landed': B.append((al['tick'], 'city', 'p_alert', NEW['beat.alert'] + ' ' + al['text'].rstrip('.').split('Alert ')[-1], None, 0))
    add('camera.record-lands', 'city', 'sawyou', NEW['beat.taped'])
    add('vale.exits', 'mission', 's_moving', NEW['beat.vale-out'])
    for l in run['log']:  # each fight once, at its start, by fight.judged's span (the final fight is the Fight; an earlier one is Shots)
        m = re.match(r'The fight \((\d+):(\d+\.\d)-(\d+):(\d+\.\d)(, the final fight)?\)', l['text']) if l['code'] == 'fight.judged' else None
        if m:
            t0 = (int(m.group(1)) * 60 + float(m.group(2))) * 10; fin = bool(m.group(5))
            op = next((s_[1] for s_ in shots(run)[1] if s_[0] >= t0 - 25 and s_[1] in OPS), None)
            B.append((int(t0), 'you', 'fire', NEW['beat.fight'] if fin else NEW['beat.shots'], None if fin else op, 0))
    add('report.landed', 'city', 'radio', NEW['beat.body'])
    add('car.seen', 'city', 'eye_open', NEW['beat.car-seen'])
    end = run['tick']
    if kind == 'win': B.append((end, 'mission', 'car', NEW['beat.away'], None, 1))
    else: B.append((end, 'mission', 'close_x', NEW['beat.lost'], None, 2))
    return sorted(B, key=lambda b: b[0])


def page(name):
    """Everything a concept might place, from one run."""
    run = load(name); db = run['debrief']; kind = 'win' if run['outcome'] == 'success' else 'fail'
    per, sh = shots(run); sn, reports = seen(run); tr = trail_rows(run, sh); st = standing(run); usd, paid = spent(run)
    M = dict(name=name, kind=kind, seed=run['seed'], end=clock(run['tick']), tick=run['tick'], run=run, way=way_of(run))
    if kind == 'win':
        M['head'] = DB['headline']['win']; M['line'] = DB['result']['win']; M['subject'] = 'councillor'
    else:
        c = run['cause']; cw = DB['cause'][c['fail']][c['why']]; M['head'] = cw['head']; M['line'] = cw['line']; M['cause'] = c
        M['subject'] = 'lookout' if c['why'] == 'lookout' else 'councillor'; M['next'] = NEW['next.' + c['fail'] + '.' + c['why']]
    M['story'] = [DB['route'][r] for r in db['route'] if r in DB['route']]
    M['conds'] = conditions(run, kind); M['beats'] = beats(run, kind); M['trail_rows'] = tr; M['standing'] = st
    alerts = [e for e in run['ledger'] if e['what'] == 'alert']
    M['alert'] = dict(top=max([0] + [e['now'] for e in alerts]), rows=alerts, reports=reports)
    M['usd'] = usd; M['paid'] = paid
    M['cell'] = [dict(op=o, name=NAME[o], role=ROLE[o], hp=h, max=mx, seen=sn[o], **per[o]) for o, h, mx in ((c['op'], c['health'], c['max']) for c in db['cell'])]
    left = {x['who']: x['fate'] for x in db.get('leftBehind', [])}
    for c in M['cell']: c['fate'] = left.get(c['op'], 'car' if kind == 'win' else '')
    nm = db.get('nextMission', {}); inv = db.get('investigators', {})
    M['city'] = dict(security=db['publicSecurity'], inv=db['investigation'], investigators=inv.get('count', 0), sightings=inv.get('sightings', 0),
                     leads=[f['lead'] for f in inv.get('findings', [])], hunted=inv.get('hunted', []), cases=nm.get('cases', []), watch=nm.get('areaWatch', []),
                     words=[x for x in nm.get('lines', []) if x['line'] == 'word'], lines=nm.get('lines', []), carSeenBy=nm.get('carSeenBy', ''),
                     contacts=nm.get('contacts', []), assets=db.get('assets', []), fight=db.get('finalFight'))
    return M


if __name__ == '__main__':
    import sys
    for n in sys.argv[1:] or ['quiet-win.route-a', 'fail-lookout', 'loud-win.seed3']:
        M = page(n); print('==', n, M['kind'], M['end'], M['head'], '|', M['line'])
        print(' conds', M['conds']); print(' usd', M['usd'], [p['text'][:60] for p in M['paid']])
        print(' trail', [(clock(r['tick']), r['delta'], r['cause'], r['who']) for r in M['trail_rows']])
        print(' alert', M['alert']['top'], [(clock(e['tick']), e['cause']) for e in M['alert']['rows']])
        print(' standing', M['standing'])
        for c in M['cell']: print(' cell', c['name'], c['hp'], c['max'], c['shots'], c['hits'], c['downs'], c['taken'], [(clock(t), w, h) for t, w, h in c['seen']], c['fate'])
        print(' beats', [(clock(b[0]), b[1], b[3], b[4]) for b in M['beats']])
        print(' city', {k: v for k, v in M['city'].items() if k not in ('lines', 'words', 'fight')}, len(M['city']['words']))
