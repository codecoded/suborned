"""Brief 107 (Chime): the weather and night takes. One Runware call per take (Seed Audio 1.0, text to audio, as the 087 pack), through tools/paid/paid.py, job 107-weather-sound.
Run from the repo root: python3 -I briefs/107/chime/make/gen.py <pass> <budget> <prompts.json> [ids...]
Takes land in paid/107-weather-sound/takes/<id>_t<pass><n>.wav; paid.py never overwrites. The pass stops at its budget."""
import json, sys, time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
REPO = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(REPO / 'tools/paid'))
import paid

JOB = '107-weather-sound'
MODEL = 'bytedance:seed-audio@1.0'   # the 087 pack's generator; it picks its own length (15-21 s), about $0.04-0.06 a take


def main(a):
    name, budget, table = a[0], float(a[1]), json.loads(Path(a[2]).read_text())
    want = set(a[3:])
    p = paid.Pass(JOB, name, budget=budget, note='Weather and night sounds (brief 107 item 3; Sky allowed up to $2)')
    jobs = [(c, t) for c in table if not want or c['id'] in want for t in range(1, c.get('takes', 3) + 1)]

    def one(j):
        c, t = j
        task = {'model': MODEL, 'positivePrompt': c['prompt'], 'outputFormat': 'WAV', 'numberResults': 1}  # one clip per call: paid.py's audio wait stops at its first result
        for attempt in range(4):
            try:
                return p.audio(task, name=f"{c['id']}_t{name}{t}.wav", subdir='takes', est=c.get('est', 0.07), prompt=c['prompt'],
                               params={'cue': c['id']}, note=f"take {t} for cue {c['id']}")
            except SystemExit as e:
                if 'serviceOverloaded' not in str(e) or attempt == 3: print(e); return None
                time.sleep(15 * (attempt + 1))
    with ThreadPoolExecutor(2) as ex:
        list(ex.map(one, jobs))
    print(f'pass {name}: ${p.spent():.4f} of ${p.budget:.2f}')


if __name__ == '__main__':
    main(sys.argv[1:])
