"""Brief 090 (Chime): a machine listener as a check, not a judge: Whisper (local, small.en) transcribes each clip; the word error against the line says whether the words came out at all, and how it spells the names shows what it heard. Run with the sep venv: out/scratch/chime/sep/bin/python -I briefs/090/chime/asr.py <out.json> <service dirs...>"""
import json, re, sys
from pathlib import Path
import whisper
sys.path.insert(0, str(Path(__file__).resolve().parent)); import lines as LN
TEXT = {}
for i, (w, l) in enumerate(LN.SCENE, 1): TEXT[f'scene{i:02d}_{w}'] = l
for i, (w, l, _) in enumerate(LN.SHOUTS, 1): TEXT[f'shout{i:02d}_{w}'] = l
for i, (w, l, _) in enumerate(LN.NAMES, 1): TEXT[f'name{i:02d}_{w}'] = l
norm = lambda s: re.sub(r"[^a-z' ]", ' ', s.lower()).split()


def wer(ref, hyp):
    r, h = norm(ref), norm(hyp); d = list(range(len(h) + 1))
    for i in range(1, len(r) + 1):
        p, d[0] = d[0], i
        for j in range(1, len(h) + 1): p, d[j] = d[j], min(d[j] + 1, d[j - 1] + 1, p + (r[i - 1] != h[j - 1]))
    return d[len(h)] / max(1, len(r))


m = whisper.load_model('small.en'); out = {}
for d in sys.argv[2:]:
    for f in sorted(Path(d).glob('*.wav')):
        k = f.stem; ref = TEXT.get(k)
        if not ref: continue
        t = m.transcribe(str(f), language='en', fp16=False, temperature=0.0, condition_on_previous_text=False)['text'].strip(); out[f'{Path(d).name}/{k}'] = {'heard': t, 'line': ref, 'word_error': round(wer(ref, t), 2)}
        print(Path(d).name, k, round(wer(ref, t), 2), '|', t, flush=True)
Path(sys.argv[1]).write_text(json.dumps(out, indent=1, ensure_ascii=False))
