import sys,re
def rows(p):
    out={}
    for l in open(p):
        t=l.split()
        if len(t)<6 or t[0] not in ('today','lab','lab-snap-off','lab-cover','lab-charge-off','lab-field-on','lab-kite-off'): continue
        if t[1] in ('6.4','6.4.7','6.4.7b') or t[1].startswith('6.4'): continue
        res=t[2]; i=3; fail=''
        if res=='failure': fail=t[3]; i=4
        end=t[i]; trail=t[i+1]; hp=t[i+9]
        v=l.split('s',1)
        m=re.search(r' (pass \([^)]*\)|pass|FAIL)', l)
        verdict=m.group(1) if m else '?'
        down=t[i+7]; cap=t[i+8]
        out[(t[0],t[1])]=f"{res[:4]} {fail} {end} T{trail} hp{hp}% dn{down} cap{cap} {verdict}"
    return out
a=rows(sys.argv[1]); b=rows(sys.argv[2])
n=0
for k in a:
    if a[k]!=b.get(k):
        n+=1; print(f"{k[0]:8} {k[1]:46} {a[k]:60} -> {b.get(k)}")
print(n,'rows differ of',len(a))
