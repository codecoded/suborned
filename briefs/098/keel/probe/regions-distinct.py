# Keel's scratch reader (brief 098, engine 0.50.0): the regions probe's held spells of 5 s or more, each counted once across plans (the city on a
# seed is the same under every plan, so one spell shows once per plan). Usage: python3 -I distinct.py a.json b.json
import json, re, sys
def rows(path):
    d=json.load(open(path)); out={}
    for s in d['settings']:
        for r in s['regions']:
            keys={}
            for l in r['longest']:
                m=re.match(r'([0-9.]+) s (\S+) at \((-?\d+),(-?\d+)\) to (\S+(?: \S+)?) \((\S+) #(\d+)\)', l)
                if not m: continue
                k=(m.group(2),m.group(3),m.group(4),m.group(5),m.group(7)); keys[k]=max(keys.get(k,0), float(m.group(1)))
            out[(s['setting'],r['region'])]=(r, keys)
    return out
A=rows(sys.argv[1]); B=rows(sys.argv[2])
for k in sorted(A):
    if k[1]=='cafe-alley': continue
    (ra,ka),(rb,kb)=A[k],B[k]
    d5a=len(ka); d10a=sum(1 for v in ka.values() if v>=10); d5b=len(kb); d10b=sum(1 for v in kb.values() if v>=10)
    print(f"{k[0]:10} {k[1]:16} distinct 5s+ {d5a:3}->{d5b:3}  10s+ {d10a:3}->{d10b:3}   (counted per plan 5s+ {ra['held5sOrMore']:3}->{rb['held5sOrMore']:3}; max {ra['maxS10']/10:5.1f}->{rb['maxS10']/10:5.1f} s)")
