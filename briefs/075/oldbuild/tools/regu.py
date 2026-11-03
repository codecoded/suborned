# Re-GUID the clashing GUIDs inside the staged old copy only (never our Kit): every .meta and every Unity YAML file in
# the stage gets old->new for each clash. new = md5("suborned.oldtown/" + old). Writes the map beside clashes.json.
import os, re, sys, json, hashlib
STAGE, CLASHES, MAPOUT = sys.argv[1:4]
clash = [r['guid'] for r in json.load(open(CLASHES))['clashes']]
mp = {g: hashlib.md5(('suborned.oldtown/' + g).encode()).hexdigest() for g in clash}
pat = re.compile('|'.join(mp).encode())
files = changed = subs = 0
for d, _, fs in os.walk(STAGE):
    for f in fs:
        p = os.path.join(d, f); files += 1
        with open(p, 'rb') as h: head = h.read(5)
        if not (f.endswith('.meta') or head == b'%YAML'): continue
        b = open(p, 'rb').read()
        n, k = pat.subn(lambda m: mp[m.group(0).decode()].encode(), b)
        if k:
            open(p, 'wb').write(n); changed += 1; subs += k
json.dump(mp, open(MAPOUT, 'w'), indent=0)
print(f'{files} files, {changed} rewritten, {subs} guid replacements, {len(mp)} guids remapped')
