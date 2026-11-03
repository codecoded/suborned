"""The targeted install (brief 069, Aster: install only what the game uses, decided by data). python3 -I tools/figures/install_clips.py <built People> <installed People>
Only the game's clips (clips.json: every clip without `game: false`; the build's clips.rows.json carries it) go in: each .fbx and .meta copied when its
bytes differ. The installed manifest keeps its figures, props and the rest, and takes this build's clip rows (assemble.py: already the game's clips) and
the clip count and bytes in its totals; it is written as assemble.py writes it. Figures, props and anything else under People are not touched and
nothing is deleted. Prints every file it changes."""
import json, os, sys, shutil, hashlib
out, dst = sys.argv[1], sys.argv[2]
sha = lambda p: hashlib.sha256(open(p, 'rb').read()).hexdigest() if os.path.exists(p) else ''
rows = [r for r in json.load(open(f'{out}/Clips/clips.rows.json')) if r.get('game', True)]
changed = []
for r in rows:
    for f in (r['file'], r['file'] + '.meta'):
        if sha(f'{out}/{f}') != sha(f'{dst}/{f}'): shutil.copyfile(f'{out}/{f}', f'{dst}/{f}'); changed.append(f)
mp = f'{dst}/manifest.json'; m = json.load(open(mp)); nm = json.load(open(f'{out}/manifest.json'))
before = json.dumps(m, indent=1)
m['clips'] = nm['clips']
m['totals'] = dict(m['totals'], clips=len(nm['clips']), bytes=sum(f['bytes'] for r in m['figures'] for f in r['files']) + sum(os.path.getsize(f"{dst}/{c['file']}") for c in nm['clips']))
if json.dumps(m, indent=1) != before: json.dump(m, open(mp, 'w'), indent=1); changed.append('manifest.json')
print(f"installed the game's {len(rows)} clips to {dst}: {len(changed)} files changed")
for f in changed: print('  changed', f)
