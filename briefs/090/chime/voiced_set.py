"""Brief 090 (Chime): the 1,701 live lines broken down in plain words, and a proposed voiced set: every agreed conversation beat; from the dialogue list, at most two wordings for each distinct meaning (the same event, order, outcome, temperament, subject and speaker); from the overheard and city snippets, the first variant only. Writes briefs/090/chime/voiced-set.json (ids from lines-handover.json)."""
import json, collections
from pathlib import Path
H = Path(__file__).resolve().parent; L = [x for x in json.loads((H / 'lines-handover.json').read_text())['lines'] if x['live']]
T = json.loads((H.parents[2] / 'contracts/missions/handover/text.json').read_text()); EVK = {k: v.get('kind') for k, v in T['events'].items()}
SEC = 0.0857  # seconds of speech per character, measured on round two's takes
dlg = [x for x in L if x['source'] == 'dialogue']; conv = [x for x in L if x['source'] == 'scenes.conversations']; sc = [x for x in L if x['source'] == 'scenes.items']
key = lambda x: (x.get('event'), x.get('verb'), x.get('outcome'), x.get('temperament'), x.get('about'), x['speaker'])
groups = collections.defaultdict(list)
for x in dlg: groups[key(x)].append(x)
cls = lambda e: {'ack': 'acknowledging an order', 'arrive': 'arriving to act', 'done': 'reporting an order done', 'taken': 'acknowledging an order', 'start': 'mission start', 'reply': 'a person\'s reply to being approached', 'read': 'the cell reading a person after Watch or Listen', 'confirm': 'a contact confirming a call'}.get(e, 'world events (alarms, shouts, radio, the fight, the clock)')
by_cls = collections.Counter(cls(x['event']) for x in dlg); grp_cls = collections.Counter(cls(k[0]) for k in groups)
items = collections.Counter((x['id'].split('.')[1]) for x in sc); variants = collections.Counter(tuple(x['id'].split('.')[1:3]) for x in sc)
first_var = [x for x in sc if x['id'].split('.')[2] == '0']
keep = conv + [x for g in groups.values() for x in g[:2]] + first_var
chars = lambda xs: sum(len(x['text']) for x in xs)
doc = {'note': __doc__, 'breakdown': {
    'dialogue_lines': len(dlg), 'dialogue_meanings': len(groups), 'dialogue_extra_wordings': len(dlg) - len(groups),
    'dialogue_lines_by_kind': dict(by_cls.most_common()), 'dialogue_meanings_by_kind': dict(grp_cls.most_common()),
    'overheard_and_city_snippet_lines': len(sc), 'snippets': len(items), 'snippet_variants': len(variants), 'snippet_lines_beyond_first_variant': len(sc) - len(first_var),
    'conversation_lines': len(conv)},
    'proposed': {'lines': len(keep), 'conversation_lines': len(conv), 'dialogue_lines': len(keep) - len(conv) - len(first_var), 'snippet_lines': len(first_var), 'characters': chars(keep), 'speech_minutes': round(chars(keep) * SEC / 60, 1),
                 'generate_hours_three_takes': round(chars(keep) * SEC * 3 * 0.7 / 3600, 2), 'check_hours_approx': round(len(keep) * 3 * 0.8 / 3600, 2), 'by_speaker': dict(collections.Counter(x['speaker'] for x in keep).most_common())},
    'ids': [x['id'] for x in keep]}
(H / 'voiced-set.json').write_text(json.dumps(doc, indent=1, ensure_ascii=False) + '\n'); print(json.dumps({k: doc[k] for k in ('breakdown', 'proposed')}, indent=1))
