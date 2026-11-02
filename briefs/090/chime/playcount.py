"""Brief 090 (Chime): about how many lines one playthrough asks for. Replays Sky's two saved sessions (out/scratch/aster/air-0212, played on 2026-11-02) through the sim's `play` (Debug build in sim/cli/bin; it reached the same outcomes at the same clock times), then maps them onto the text: each order the script gives asks for one `ack`; each engine log line asks for one line from every dialogue event whose `log` codes match it (one wording, one speaker); a turn or Listen that lands plays its conversation if one matches, else its overheard snippet. Hostile AI states (not logged) and the crowd are not counted, so this is a floor."""
import json, re, sys, collections
from pathlib import Path
REPO = Path(__file__).resolve().parents[3]; T = json.loads((REPO / 'contracts/missions/handover/text.json').read_text()); EV = T['events']
cv = T['scenes']['conversations']; cv = cv['items'] if isinstance(cv, dict) else cv
D = collections.defaultdict(set)
for d in T['dialogue']: D[d['event']].add(d['speaker'])


def count(script, log):
    orders = json.loads(Path(script).read_text())['orders']; got = collections.Counter(); ex = []
    for o in orders:
        if o['do'] in ('move', 'cancel', 'run'): continue
        got['ack (order taken)'] += 1
    for line in Path(log).read_text().splitlines():
        m = re.match(r'\s+(\d+:\d+\.\d)\s+(\S+)\s', line)
        if not m: continue
        code = m.group(2)
        for e, v in EV.items():
            if any(code == c or (c.endswith('.') and code.startswith(c)) for c in v.get('log') or []) and e in D: got[e] += 1; ex.append((m.group(1), code, e))
        if code.startswith('turn.') or code == 'scout.listen.heard':
            got['conversation beats (estimated)'] += 8
    return got, ex


if __name__ == '__main__':
    res = {}
    for s, log in zip(sys.argv[1::2], sys.argv[2::2]):
        g, ex = count(s, log); res[Path(s).name] = {'by_event': dict(g.most_common()), 'total': sum(g.values())}
    print(json.dumps(res, indent=1))
