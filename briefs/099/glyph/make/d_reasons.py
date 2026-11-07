"""The numbers and their reasons, from the page facts (d_data.page). One line under each number, and the explainer's rows three ways:
by what (concept A), by when (B), by who (C). The source of each is named in `src`. Glyph, brief 099."""
from d_words import NEW, f, NAME, DB
from d_data import clock

VALE_CAR = 3500  # 5:50, his own car leaves the pickup (book.job.stops, vale.at-car); the run's end is measured against it
NUM = {1: 'one', 2: 'two', 3: 'three', 4: 'four', 5: 'five'}


def signed(v): return f'+{v}' if v > 0 else str(v)


def usd(v): return f'${v:,}'


def and_list(xs):
    xs = [x for x in xs if x]
    return xs[0] if len(xs) == 1 else ', '.join(xs[:-1]) + ' and ' + xs[-1] if xs else ''


def is_shot(r): return r['kind'] in ('gunfire', 'hit', 'down')


def trail_by_what(M):
    """Grouped rows: a person shot (fired on, hit, down) is one row; an act is one row. (icon, words, +n, ops, when)."""
    rows, shot = [], {}
    for r in M['trail_rows']:
        if is_shot(r):
            g = 'guard' if r['tgt'] == 'guard' else 'rivals' if r['tgt'].startswith('rival') else 'police'
            e = shot.setdefault(g, dict(n=0, ops=[], tgts=set(), t0=r['tick'], t1=r['tick'])); e['n'] += r['delta']; e['tgts'].add(r['tgt']); e['t1'] = r['tick']
            if r['who'] and r['who'] not in e['ops']: e['ops'].append(r['who'])
            continue
        k = r['kind'] if r['kind'] not in ('turn.help', 'turn.pay') else r['kind']
        words = f('trail.' + ('car-seen' if k == 'car-seen' else k), op=NAME.get(r['who'], ''))
        icon = {'turn.help': 'help', 'turn.pay': 'pay', 'loop': 'loop', 'reroute': 'reroute', 'call': 's_calling', 'recorded': 'sawyou', 'car-seen': 'eye_open'}.get(k, 'p_trail')
        rows.append((icon, words, r['delta'], [r['who']] if r['who'] else [], clock(r['tick'])))
    merged = []  # one agent's several small acts read as one row ("Sethre looped the entrance camera and sent the patrol off", +2)
    for r in rows:
        prev = next((m for m in merged if r[3] and m[3] == r[3] and m[0] != 'fire'), None)
        if prev: merged[merged.index(prev)] = (prev[0], prev[1] + ' and ' + r[1].split(' ', 1)[1], prev[2] + r[2], prev[3], prev[4] + ', ' + r[4])
        else: merged.append(r)
    rows = merged
    for g, e in shot.items():
        who = and_list([NAME[o] for o in e['ops']]); n = len(e['tgts'])
        tgt = 'the guard' if g == 'guard' else f"{NUM.get(n, n)} rival{'s' if n > 1 else ''}" if g == 'rivals' else 'the police'
        rows.append(('fire', f"{who} shot {tgt}", e['n'], e['ops'], clock(e['t0']) + ('–' + clock(e['t1']) if e['t1'] - e['t0'] > 30 else '')))
    return sorted(rows, key=lambda r: -r[2])


def trail_by_who(M):
    out = {}
    for r in M['trail_rows']:
        k = r['who'] or ('car' if r['kind'] == 'car-seen' else 'city')
        out[k] = out.get(k, 0) + r['delta']
    return sorted(out.items(), key=lambda kv: -kv[1])


def trail_short(M):
    rows = M['trail_rows']; tot = sum(r['delta'] for r in rows); sh = sum(r['delta'] for r in rows if is_shot(r))
    if tot and sh * 2 >= tot: return f('why.trail.shooting', n=sh, total=tot)
    seen, words = set(), []
    for r in rows:
        k = r['kind']
        if k in seen: continue
        seen.add(k); words.append(NEW.get('short.' + k, k))
    return (and_list(words) or NEW['why.alert.0']).capitalize()


def stats(M):
    """The five numbers, each with its one line. src = where the number comes from."""
    run = M['run']; out = []
    if M['kind'] == 'win':
        left = max(0, VALE_CAR - M['tick']); why = f('why.time.win', left=clock(left))
    else: why = NEW['why.time.fail']
    out.append(dict(id='time', icon='p_time', label=NEW['stat.time'], value=M['end'], why=why, src='play tick; log success / fail.* (FailCause.clock)'))
    paid = M['paid']; kind = 'pay' if any('pay' in p['code'] for p in paid) else 'help' if paid else 'none'
    out.append(dict(id='cash', icon='p_cash', label=NEW['stat.cash'], value=usd(M['usd']) if M['usd'] else '$0', why=NEW['why.cash.' + kind],
                    src='play cash (purse $100,000 − cash); the act from log turn.pay.turned / turn.help.turned'))
    out.append(dict(id='trail', icon='p_trail', label=NEW['stat.trail'], value=signed(run['trail']), why=trail_short(M), src='play trail; ledger rows what=trail with their cause'))
    a = M['alert']; top = a['top']
    if top == 0: why = NEW['why.alert.0']
    else:
        c = a['rows'][0]['cause']; why = f('why.alert.loiter', op=NAME.get(c.split(':')[-1], 'one of us')) if c.startswith('loiter') else NEW['why.alert.gunfire']
    out.append(dict(id='alert', icon='p_alert', label=NEW['stat.alert'], value=str(top), why=why, src='ledger rows what=alert (highest now); attention.landed / report.landed'))
    st = M['standing']; band = NEW['band.' + band_of(st['next'])]
    why = DB['standing']['causes'].get(st['cause'], '').split('. ')[0].rstrip('.')  # the run's own main cause; word of mouth is in its explainer
    out.append(dict(id='standing', icon=mood(st['next']), label=NEW['stat.standing'], value=f"{band} {signed(st['next'])}", why=why,
                    src='debrief nextMission.standing (from, to, band, moves; word of mouth between missions); ledger what=standing'))
    return out


def band_of(v): return 'hero' if v >= 3 else 'menace' if v <= -3 else 'neutral'
def mood(v): return 'mood_friendly' if v >= 3 else 'mood_scared' if v <= -3 else 'mood_uneasy'


def cell_line(M, c):
    """The one moment that mattered for each agent, from their own log lines."""
    o = c['op']; L = M['run']['log']; has = lambda code, w=None: any(l['code'] == code and (w is None or w in l['text']) for l in L)
    if o == 'scout':
        if has('scout.watch.watched', 'waitress') and has('scout.listen.heard', 'waitress'): return f('cell.watched', who='Inez')
        if has('scout.listen.heard', 'driver'): return f('cell.listened', who='the driver')
    if o == 'fixer':
        if has('turn.help.turned', 'waitress'): return f('cell.turned', who='Inez', usd=usd(M['usd']))
        if has('turn.pay.turned', 'driver'): return f('cell.turned', who='the driver', usd=usd(M['usd']))
    if o == 'tech':
        if has('loop.on') and has('patrol.rerouted'): return NEW['cell.loop2']
        if has('loop.on'): return NEW['cell.loop']
        return NEW['cell.idle']
    if o == 'enforcer':
        if 'guard' in c['downs']: return NEW['cell.shot.guard']
        if has('enter.in'): return NEW['cell.carried']
        if any(l['code'] == 'order.rejected' and l['text'].startswith('enforcer') for l in L): return NEW['cell.waited']
    if c['downs']: return NEW['cell.rivals']
    return NEW['cell.idle']


def city_lines(M):
    """The city's answer, in the debrief's own words where they exist (DB.investigation, causes, between, assets)."""
    C = M['city']; inv = C['inv']; out = []
    cause = DB['causes'].get(inv['cause'].split(':')[0], '')
    out.append(('found', DB['investigation'][inv['state']].rstrip('.'), cause, 'debrief investigation.state / cause'))
    for c in C['cases']:
        out.append(('papers', NEW['city.case'], f('city.case.line', n=c['missions']), 'debrief nextMission.cases'))
    for w in C['watch']:
        out.append(('eye_open', f('city.watch', g=w['guards'], c=w['cameras']), f('city.watch.line', n=w['missions']), 'debrief nextMission.areaWatch'))
    if C['hunted']: out.append(('car', NEW['city.hunt.car'], NEW['city.hunt.line'], 'debrief investigators.hunted; carSeenBy'))
    for a in C['assets']:
        out.append(('turned', NEW['city.kept'], DB['assets'][a].rstrip('.'), 'debrief assets'))
    face = sum(1 for x in C['words'] if 'fixer-face' in x['detail'])
    if face: out.append(('talker', f('city.word.face', n=face), NEW['city.word.line'], "debrief nextMission.lines line=word (detail …fixer-face)"))
    elif C['words']: out.append(('talker', f('city.word', n=len(C['words'])), NEW['p.where'], 'debrief nextMission.lines line=word'))
    return out
