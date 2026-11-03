"""Brief 087 (Chime): the generated takes. One Runware call per take (Mirelo SFX 1.5, text to audio), through tools/paid/paid.py, job 087-sound.
Run: python3 -I briefs/087/chime/gen.py <pass> <budget> <prompts.json> [ids...]   Take files land in paid/087-sound/takes/<id>_t<n>.wav and are never overwritten."""
import json, sys, time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
REPO = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(REPO / 'tools/paid'))
import paid

MODEL = 'mirelo:1@1'
EST_PER_S = 0.01      # the first calls reported $0.01 per second per clip (Runware's page lists less); every call's real cost is in the ledger


def main(a):
    name, budget, table = a[0], float(a[1]), json.loads(Path(a[2]).read_text())
    want = set(a[3:])
    p = paid.Pass('087-sound', name, budget=budget, note='First sound pack takes (brief 087; Sky allowed up to $10 in all)')
    jobs = []
    for c in table:
        if want and c['id'] not in want: continue
        for t in range(1, c.get('takes', 2) + 1):
            jobs.append((c, t))
    def one(j):
        c, t = j
        seed = 87000 + abs(hash_id(c['id'])) % 10000 + t * 100
        task = {'model': c['model'], 'positivePrompt': c['prompt'], 'outputFormat': 'WAV', 'numberResults': 1} if c.get('model') else {'model': MODEL, 'positivePrompt': c['prompt'], 'duration': c['s'], 'steps': 25, 'seed': seed, 'outputFormat': 'WAV', 'numberResults': 1}  # one clip per call: paid.py's audio wait stops at its first result, so a second would be billed and lost
        for attempt in range(4):
            try:
                return p.audio(task, name=f"{c['id']}_t{c.get('base', 0) + t}.wav", subdir='takes', est=c.get('est', EST_PER_S * c['s']), prompt=c['prompt'],
                               params={'duration': c['s'], 'steps': 25, 'cue': c['id']}, note=f"take {t} for cue {c['id']}")
            except SystemExit as e:
                if 'serviceOverloaded' not in str(e) or attempt == 3: print(e); return None
                time.sleep(15 * (attempt + 1))
    with ThreadPoolExecutor(2) as ex:
        list(ex.map(one, jobs))
    print(f'pass {name}: ${p.spent():.4f} of ${p.budget:.2f}')


def hash_id(s):
    h = 2166136261
    for ch in s.encode():
        h = ((h ^ ch) * 16777619) & 0xffffffff
    return h


if __name__ == '__main__':
    main(sys.argv[1:])
