"""The page's words. T/DB/BOOK: what Fable has already written (contracts/missions/handover/text.json). NEW: stand-ins this mock needed,
every one listed in ../notes.md for Fable to write. Plain everyday words (Sky: "Plain-English end cards"). Glyph, brief 099."""
import json
R = '/Users/sky/Projects/suborned/'
T = json.load(open(R + 'contracts/missions/handover/text.json'))
DB, BOOK = T['debrief'], T['book']
NAME = BOOK['cell']['name']                     # scout -> Larthi (book.cell.name)
OPS = ('scout', 'fixer', 'tech', 'enforcer')
ROLE = {o: T['names']['speakers'][o] for o in OPS}
PEOPLE = {'waitress': 'Inez', 'driver': 'the driver', 'guard': 'the guard', 'councillor': 'Popovic'}


def who_words(id_):
    if id_ in NAME: return NAME[id_]
    if id_ in PEOPLE: return PEOPLE[id_]
    if id_.startswith('rival-'): return 'a rival'
    if id_.startswith(('security', 'officer', 'police')): return 'the police'
    return T['names']['targets'].get(id_, id_)


NEW = {
    # the head
    'caps.win': 'THE HANDOVER · DONE AT {end}', 'caps.fail': 'THE HANDOVER · LOST AT {end}',
    # how each start-page condition came true (book.job.needs.ways rows, answered)
    'how.called': 'Inez called him over', 'how.shot': 'shot down', 'how.looped': 'looped', 'how.in': 'in', 'how.paid': 'paid',
    'how.helped': 'helped', 'how.taped': 'it taped the pay', 'the driver': 'the driver', 'how.never': 'never',
    # the strip's beats
    'beat.watched': 'Watched', 'beat.listened': 'Listened', 'beat.turned': '{who} turned', 'beat.loop': 'Loop', 'beat.reroute': 'Patrol off',
    'beat.call': 'Guard drawn off', 'beat.in': 'In', 'beat.out': 'Popovic out', 'beat.rivals': 'Rivals in', 'beat.lookout': 'Lookout posted',
    'beat.alert': 'Alert', 'beat.taped': 'Pay taped', 'beat.vale-out': 'Popovic out', 'beat.fight': 'Fight', 'beat.shots': 'Shots',
    'beat.body': 'Body found', 'beat.car-seen': 'Car seen', 'beat.away': 'Away', 'beat.lost': 'Taken',
    # the numbers: label, and the one line under each
    'stat.time': 'Time', 'stat.cash': 'Spent', 'stat.trail': 'Trail', 'stat.alert': 'Alert', 'stat.standing': 'Standing',
    'why.time.win': '{left} to spare', 'why.time.fail': 'Their lookout got him',
    'why.cash.help': "Clearing Inez's debt", 'why.cash.pay': 'Paying the driver', 'why.cash.none': 'Nothing paid',
    'why.alert.0': 'Nobody raised the alarm', 'why.alert.loiter': 'A camera saw {op} hanging about', 'why.alert.gunfire': 'Gunfire at the door',
    'why.trail.shooting': 'Shooting: {n} of the {total}',
    # Trail rows (the explainer): what left a trace
    'trail.turn.help': '{op} helped Inez', 'trail.turn.pay': '{op} paid the driver', 'trail.loop': '{op} looped the entrance camera',
    'trail.reroute': '{op} sent the patrol off', 'trail.call': 'Inez called the guard over', 'trail.recorded': 'The pickup camera taped the pay',
    'trail.shot': '{op} shot {tgt}', 'trail.car-seen': 'A police officer saw the car leave',
    'short.turn.help': 'Inez turned', 'short.turn.pay': 'the pay', 'short.loop': 'the loop', 'short.reroute': 'the patrol', 'short.call': 'her call',
    'short.recorded': 'the tape', 'short.car-seen': 'the car seen leaving',
    'trail.law': 'Shooting at someone: +2 when you fire, +2 when they are hit, +2 when they fall.',
    'trail.means': 'Trail is what investigators can trace back to you. It stays with the campaign.',
    'trail.by.who': 'Who left it', 'trail.by.what': 'What left it',
    # the cell's one moment each
    'cell.watched': 'Watched and listened to {who}', 'cell.listened': 'Listened to {who}', 'cell.turned': 'Turned {who} · {usd}',
    'cell.loop': 'Looped the entrance camera', 'cell.loop2': 'Looped the camera, sent the patrol off', 'cell.carried': 'Carried Popovic out',
    'cell.waited': 'Waited at the service lane', 'cell.shot.guard': 'Shot the guard, carried Popovic out',
    'cell.rivals': 'Drove the rivals off', 'cell.idle': 'Stood by',
    # the city's answer
    'city.word': '{n} people are talking about it', 'city.word.face': "{n} people heard what Hasti looks like",
    'city.hunt.car': 'They are hunting our car', 'city.kept': 'Kept',
    'band.hero': 'Hero', 'band.neutral': 'Neutral', 'band.menace': 'Menace',
    # people (concept C)
    'people.head': 'Who it touched', 'tag.turned': 'Turned', 'tag.saw': 'Saw it',
    'tag.down': 'Down', 'tag.drawn-off': 'Drawn off', 'tag.knows-face': "Knows Hasti's face", 'tag.taken': 'Taken', 'tag.in-car': 'In our car',
    'tag.grieves': 'Grieves', 'tag.followed': 'Followed it',
    # people (concept C): the row's names and lines, the cards' numbers
    'p.vale.win': 'taken alive · {t}', 'p.vale.fail': 'by the rivals · {t}', 'p.lookout': 'Their lookout', 'p.lookout.line': 'watched his car from {t}',
    'p.driver': 'The driver', 'p.guard': 'The guard', 'p.guard.killed': 'Killed', 'p.guard.case': 'a case for {n} missions', 'p.guard.saw': 'saw {op} run · {t}',
    'p.guard.by': 'by Inez', 'p.grief': 'for the guard: more afraid now', 'p.police': 'The police', 'p.n.down': '{n} down', 'p.police.car': 'one saw the car leave',
    'p.rivals': 'The rivals', 'p.rivals.off': 'the rest drove off', 'p.rivals.ran': 'the rest ran', 'p.patrol': 'The patrol', 'p.patrol.line': '{ops} hanging about',
    'p.people': '{n} people', 'p.word': 'word of mouth', 'p.talking': 'Talking', 'p.where': 'where and when',
    'card.hits': '{hits} hits of {shots}', 'card.down': '{n} down', 'card.hit': 'hit {n}×', 'card.seen': 'seen by {who}', 'card.share': '{n} T',
    'w.guard': 'the guard', 'w.camera': 'a camera', 'w.patrol': 'the patrol', 'w.police': 'the police',
    # the city's rows (concept A, C)
    'city.case': 'A case on the guard', 'city.case.line': 'a killing was found · {n} missions', 'city.watch': '+{g} guard, +{c} camera at the door',
    'city.watch.line': 'for {n} missions', 'city.hunt.line': 'a police officer saw it leave', 'city.word.line': 'word of mouth, by tonight',
    'way.front': 'Front door', 'way.pickup': 'Pickup', 'way.yours': 'your way in',
    # the fail
    'next.head': 'Next time',
    'next.rivals-reach-vale.lookout': 'Send their lookout away before the car goes round.',
    'next.ways.lookout': [('mislead', 'False tip', 'scout'), ('pay', 'Pay him off', 'fixer')],
    'missed': 'Nobody dealt with him for {span}',
    # ways on
    'way.again': 'Try again', 'way.new': 'New start', 'way.same': 'Same start', 'way.replay': 'Watch the replay', 'way.start': 'Back to the start',
}


def f(key, **kw):
    s = NEW[key]
    for k, v in kw.items(): s = s.replace('{' + k + '}', str(v))
    return s


GROUPS = [('caps.', 'The head'), ('how.', 'How a start-page row came true'), ('beat.', 'The run strip'), ('stat.', 'The numbers'), ('why.', 'One line under each number'),
          ('trail.', 'The Trail explainer'), ('short.', 'Short causes'), ('cell.', 'The cell, one moment each'), ('card.', 'The cell cards (C)'), ('w.', 'Who saw them'),
          ('p.', 'Who the run touched (C)'), ('tag.', 'Their tags (C)'), ('people.', 'People (C)'), ('city.', 'What the city does now'), ('band.', 'Standing bands'),
          ('way.', 'Ways on'), ('next.', 'A fail: next time'), ('missed', 'A fail: next time'), ('the driver', 'How')]


def markdown():
    """Every new word this mock used, by key, for Fable (written to ../words.md)."""
    done = set(); out = ['# New words for Fable (brief 099)', '', 'Stand-ins Glyph used in the debrief mocks, by key (make/d_words.py NEW). Existing text.json words are not listed.', '']
    for pre, title in GROUPS:
        ks = [k for k in NEW if k.startswith(pre) and k not in done]
        if not ks: continue
        out.append(f'**{title}**')
        for k in ks:
            v = NEW[k]; done.add(k); out.append(f"- `{k}`: {' · '.join(w for _, w, _ in v) if isinstance(v, list) else v}")
        out.append('')
    assert not [k for k in NEW if k not in done]
    return '\n'.join(out)


if __name__ == '__main__':
    open(R + 'briefs/099/glyph/words.md', 'w').write(markdown()); print(len(NEW), 'words')
