import json, sys, os
out = sys.argv[1]
def S(name, orders, until=600, start=None):
    json.dump({'schema':'suborned.sim.script/1','mission':'range','start':start or {},'orders':orders,'until':until}, open(os.path.join(out, name+'.json'),'w'), indent=1)
ops = ['scout','fixer','tech','enforcer']
def spawn(kind, slot, n): return {'tick':0,'do':'spawn','target':kind,'to':slot,'count':n}
def draw(*who): return [{'tick':0,'op':o,'do':'draw'} for o in who]
S('enf-v3-near', [spawn('rivals','near',3)] + draw('enforcer'))
S('cell-v3-near', [spawn('rivals','near',3)] + draw(*ops))
S('cell-v5-near', [spawn('rivals','near',5)] + draw(*ops))
S('cell-v3-cover', [spawn('rivals','cover',3)] + draw(*ops))
S('cell-v4-police-eff', [spawn('police','eff',4)] + draw(*ops))
S('cell-v4-police-cover', [spawn('police','cover',4)] + draw(*ops))
S('cell-v5-rivals-eff', [spawn('rivals','eff',5)] + draw(*ops))
print('ok')
