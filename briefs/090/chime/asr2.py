"""Brief 090 (Chime): Whisper small.en (local) on a list of clips. Args: in.json out.json; in.json = [{"wav":..., "line":..., "prompt": optional vocabulary}]. The prompt only biases spelling toward names it lists; a clip that sounds like another word still comes out as that word."""
import json, re, sys
from pathlib import Path
import whisper
norm = lambda s: re.sub(r"[^a-z' ]", ' ', s.lower().replace("'s", '')).split()


def wer(ref, hyp):
    r, h = norm(ref), norm(hyp); d = list(range(len(h) + 1))
    for i in range(1, len(r) + 1):
        p, d[0] = d[0], i
        for j in range(1, len(h) + 1): p, d[j] = d[j], min(d[j] + 1, d[j - 1] + 1, p + (r[i - 1] != h[j - 1]))
    return round(d[len(h)] / max(1, len(r)), 3)


m = whisper.load_model('small.en'); out = []
for j in json.loads(Path(sys.argv[1]).read_text()):
    t = m.transcribe(j['wav'], language='en', fp16=False, temperature=0.0, condition_on_previous_text=False, initial_prompt=j.get('prompt'))['text'].strip(); out.append({**j, 'heard': t, 'word_error': wer(j['line'], t)})
Path(sys.argv[2]).write_text(json.dumps(out, indent=1, ensure_ascii=False))
