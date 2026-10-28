# The recorded source edits to the staged old code and art (briefs/075/oldbuild/edits.md lists them with counts).
import os, re, sys, json
STAGE = sys.argv[1]
log = []
def edit(path_glob_ext, root, rules):
    for d, _, fs in os.walk(os.path.join(STAGE, root)):
        for f in fs:
            if not f.endswith(path_glob_ext): continue
            p = os.path.join(d, f); s = open(p, encoding='utf-8').read(); o = s
            for tag, pat, rep in rules:
                s, k = re.subn(pat, rep, s)
                if k: log.append((tag, os.path.relpath(p, STAGE), k))
            if s != o: open(p, 'w', encoding='utf-8').write(s)
code_rules = [
    ('E1 art path', r'Assets/TheSuborned/Art/', 'Assets/Suborned/OldTownArt/'),
    ('E2 data path', r'Assets/TheSuborned/Data/', 'Assets/Suborned/OldTownArt/Data/'),
    ('E3 shader path', r'Assets/TheSuborned/Shaders/', 'Assets/Suborned/OldTownArt/Shaders/'),
    ('E4 scenes dir', r'"Assets/TheSuborned/Scenes"', '"Assets/Scenes"'),
    ('E5 namespace', r'\bTheSuborned\b', 'Suborned.OldTown'),
    ('E6 shader names', r'"DecoKit/', '"OldTown/DecoKit/'),
    ('E7 scene name', r'const string SceneName = "DecoKitStreet";', 'const string SceneName = "OldTownProof";'),
]
edit('.cs', 'code', code_rules)
art_rules = [
    ('A1 shader names', r'^Shader "DecoKit/', 'Shader "OldTown/DecoKit/'),
    ('A2 include path', r'Assets/TheSuborned/Art/DecoKit/Shaders/', 'Assets/Suborned/OldTownArt/DecoKit/Shaders/'),
]
for ext in ('.shader', '.hlsl'):
    edit(ext, 'art', [(t, re.compile(p, re.M), r) for t, p, r in art_rules])
json.dump(log, open(sys.argv[2], 'w'), indent=0)
from collections import defaultdict
agg = defaultdict(lambda: [0, 0])
for t, p, k in log: agg[t][0] += 1; agg[t][1] += k
for t in sorted(agg): print(t, 'files', agg[t][0], 'sites', agg[t][1])
