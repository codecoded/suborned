import os, re, sys, collections
STAGE, UNITY, OLDPROJ = sys.argv[1:4]
DEF = re.compile(rb'^guid: ([0-9a-f]{32})', re.M); REF = re.compile(rb'guid: ([0-9a-f]{32})')
def defs(root):
    s = {}
    for d, _, fs in os.walk(root):
        for f in fs:
            if f.endswith('.meta'):
                p=os.path.join(d,f); m = DEF.search(open(p,'rb').read())
                if m: s[m.group(1).decode()] = p
    return s
have = defs(STAGE); have.update(defs(os.path.join(UNITY,'Assets'))); have.update(defs(os.path.join(UNITY,'Library/PackageCache')))
oldp = defs(os.path.join(OLDPROJ,'Assets')); oldp.update(defs(os.path.join(OLDPROJ,'Library/PackageCache')) if os.path.isdir(os.path.join(OLDPROJ,'Library/PackageCache')) else {})
miss = collections.defaultdict(set)
for d, _, fs in os.walk(STAGE):
    for f in fs:
        p=os.path.join(d,f)
        with open(p,'rb') as h: head=h.read(5)
        if not (f.endswith('.meta') or head==b'%YAML'): continue
        b=open(p,'rb').read()
        own = DEF.search(b).group(1) if f.endswith('.meta') and DEF.search(b) else None
        for m in REF.finditer(b):
            g=m.group(1).decode()
            if g.startswith('0000000000000000') or g in have: continue
            miss[g].add(os.path.relpath(p,STAGE))
print(len(miss),'dangling guids')
for g,ps in sorted(miss.items(), key=lambda x:-len(x[1])):
    print(g, len(ps), 'old-project:', os.path.relpath(oldp[g], OLDPROJ) if g in oldp else '?', '| e.g.', sorted(ps)[0])
