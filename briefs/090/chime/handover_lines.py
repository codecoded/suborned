"""Brief 090 (Chime): every line a person or robot can say in the Handover as it plays now, read from contracts/missions/handover/text.json (never edited), into briefs/090/chime/lines-handover.json for wiring to read later. A line is counted as live unless its entry, its event or its verb is parked or pending in the text, or its conversation is not yet agreed."""
import json, re, collections
from pathlib import Path
REPO = Path(__file__).resolve().parents[3]; T = json.loads((REPO / 'contracts/missions/handover/text.json').read_text())
PARKED_VERBS = {k for k, v in T['actions'].items() if 'parked' in v}; EV = T['events']; out = []


def add(id, speaker, text, source, live, why, **kw):
    kind = 'radio' if kw.get('outcome') == 'radio' or speaker == 'watcher' else 'shout' if text.rstrip().endswith('!') and len(text.split()) <= 8 else 'line'
    out.append({'id': id, 'speaker': speaker, 'text': text, 'kind': kind, 'source': source, 'live': live, 'why_not': None if live else why, **{k: v for k, v in kw.items() if v is not None}})


for i, d in enumerate(T['dialogue']):
    e = EV.get(d['event'], {}); why = 'entry parked' if 'parked' in d else 'entry pending' if 'pending' in d else f"verb {d.get('verb')} parked" if d.get('verb') in PARKED_VERBS else 'event not in the event table' if d['event'] not in EV else 'event parked' if 'parked' in e else None
    for j, l in enumerate(d['lines']):
        add(f'dlg.{i:03d}.{j}', d['speaker'], l, 'dialogue', why is None, why, event=d['event'], verb=d.get('verb'), outcome=d.get('outcome'), temperament=d.get('temperament'), about=d.get('about'), trigger=e.get('trigger'), log=e.get('log'))
cv = T['scenes']['conversations']; cv = cv['items'] if isinstance(cv, dict) else cv
for c in cv:
    for j, b in enumerate(c.get('beats', [])): add(f"conv.{c['id']}.{j}", b['speaker'], b['line'], 'scenes.conversations', c.get('status') == 'agreed', f"conversation {c.get('status')}", conversation=c['id'], when=c.get('when'))
for i, it in enumerate(T['scenes']['items']):
    vs = it.get('variants') or [{'beats': it.get('beats', [])}]
    for v, var in enumerate(vs):
        for j, b in enumerate(var.get('beats', [])):
            if 'line' not in b: continue
            add(f'scene.{i:03d}.{v}.{j}', b['speaker'], b['line'], 'scenes.items', 'parked' not in it and it.get('verb') not in PARKED_VERBS, 'scene parked', kind_scene=it.get('kind'), verb=it.get('verb'), about=it.get('about'))
live = [x for x in out if x['live']]; by = collections.Counter(x['speaker'] for x in live); kinds = collections.Counter(x['kind'] for x in live); chars = sum(len(x['text']) for x in live)
uniq = len({(x['speaker'], x['text']) for x in live})
doc = {'schema': 'suborned.voice-lines/1', 'note': __doc__, 'counts': {'all_lines': len(out), 'live_lines': len(live), 'live_unique_speaker_text': uniq, 'live_by_speaker': dict(by.most_common()), 'live_by_kind': dict(kinds), 'live_characters': chars, 'not_live_by_reason': dict(collections.Counter(x['why_not'] for x in out if not x['live']))}, 'lines': out}
(Path(__file__).resolve().parent / 'lines-handover.json').write_text(json.dumps(doc, indent=1, ensure_ascii=False) + '\n'); print(json.dumps(doc['counts'], indent=0))
