import json, sys
P=json.load(open(sys.argv[1])); by={p['id']:p for p in P['persons']}
pl={p['id']:p for p in P['places']}
hm=lambda t: f"{int(t)//60:02d}:{int(t)%60:02d}"
def day(p, lo=0, hi=1440):
    return ' · '.join(f"{hm(s['from'])}-{hm(s['to'])} {s['doing']}@{s['place']}" for s in sorted(p['routine']['stops'],key=lambda s:s['from']) if s['to']>lo and s['from']<hi)
# the cast per M-2 (names pinned)
names={'Inez':None,'Vale':None}
for p in P['persons']:
    if p['name'].startswith('Inez '): names['Inez']=p['id']
    if p['name'].endswith(' Vale'): names['Vale']=p['id']
print('pinned', names)
role=lambda w,k: [p for p in P['persons'] if p['work'] and (p['work'].get('role') or p['work'].get('position'))==k and p['work']['employer']==w]
print('\n-- guards of the Office (shift, post, place, variant)')
for g in sorted(role('records-office','guard'), key=lambda p:p['id']):
    w=g['work']; print(f"{g['id']} {g['name']:<20} shift {hm(w['shift']['from'])}-{hm(w['shift']['to'])} post {w['post']:<8} at {w['place']:<17} {g['temperament']:<9} ties {[(t['kind'],t['with']) for t in g['ties'] if t['kind']=='regular']}")
print('\n-- who stands at the public door (office-entrance) in the visible posts, by minute around the chime')
for t in range(17*60+20, 17*60+36):
    at=[p['id'] for p in P['persons'] if p['work'] and p['work']['post']=='door' and p['work']['unit']=='records-office' and any(s['from']<=t<s['to'] and s['place']=='office-entrance' and s['doing'] in('work','meal') for s in p['routine']['stops'])]
    print(hm(t), at)
print('\n-- the door guard, the incoming watch, around 17:30')
for g in sorted(role('records-office','guard'), key=lambda p:p['id']):
    w=g['work']
    if w['post']=='door': print(g['id'], 'shift',hm(w['shift']['from']),hm(w['shift']['to']), '|', day(g,16*60+30,18*60+30))
print('\n-- Vale, his driver')
v=by[names['Vale']]; print(v['name'], v['work']['place'], v['temperament'], v['need'], '|', day(v,8*60,10*60), ' ... ', day(v,16*60+30,18*60))
for d in role('records-office','official-driver'): print(d['id'], d['name'], d['temperament'], d['need'], 'ties', [(t['kind'],t['with']) for t in d['ties']], '|', day(d,8*60,10*60), ' ... ', day(d,16*60+30,18*60))
print('\n-- Inez'); i=by[names['Inez']]; print(i['name'], i['work']['shift'], i['need'], '|', day(i,11*60,19*60))
print('\n-- the day guard\'s regular ties'); 
for g in role('records-office','guard'):
    if g['work']['post']=='door' and g['work']['shift']['from']==570:
        for t in g['ties']:
            if t['kind']=='regular': q=by[t['with']]; print(g['id'],'regular of',q['id'],q['name'],q['work']['position'],'shift',hm(q['work']['shift']['from']),hm(q['work']['shift']['to']),'post',q['work']['post'])
