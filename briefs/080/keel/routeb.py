# Route B on the new clock, a scratch variant for the measure only (Waymark owns the route): the orders after the Mislead come forward a minute, as Vale does.
import json
W='/Users/sky/Projects/suborned/out/scratch/keel/wt080/contracts/missions/handover/routes/'
P='/Users/sky/Projects/suborned/out/scratch/keel/p080/routea/'
for name in ['route-b','route-b-board']:
    d=json.load(open(W+name+'.json')); d.pop('expect',None)
    mis=[o['tick'] for o in d['orders'] if o['do']=='mislead']
    t0=mis[0] if mis else 1900
    if name=='route-b':
        for o in d['orders']: print(o['tick']/10,o.get('op'),o.get('do'),o.get('target') or o.get('to') or '')
    for o in d['orders']:
        if o['tick']>t0: o['tick']-=600
    d['until']=4500
    json.dump(d,open(P+name+'-minus60.json','w'))
