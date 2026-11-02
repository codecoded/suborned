"""Brief 090 (Chime): Qwen3-TTS run on this Mac (official qwen-tts 0.1.1, PyTorch on MPS, bf16, weights from Hugging Face in out/scratch/chime/models). Free, and the seed is ours.
Run: out/scratch/chime/qwen/bin/python -I briefs/090/chime/qwen_local.py jobs.json
jobs.json: [{"out": path, "mode": "design"|"clone", "text": ..., "instruct": ... | "ref": wav, "ref_text": ..., "seed": int}]. Writes each wav and a timing line."""
import json, sys, time
from pathlib import Path
import torch, soundfile as sf
from qwen_tts import Qwen3TTSModel
M = Path(__file__).resolve().parents[3] / 'out/scratch/chime/models'; DEV = 'mps' if torch.backends.mps.is_available() else 'cpu'; _m = {}; _p = {}


def model(kind):
    if kind not in _m: _m[kind] = Qwen3TTSModel.from_pretrained(str(M / f'Qwen3-TTS-12Hz-1.7B-{kind}'), device_map=DEV, dtype=torch.bfloat16, attn_implementation='sdpa')
    return _m[kind]


for j in json.loads(Path(sys.argv[1]).read_text()):
    if Path(j['out']).exists() and not j.get('force'): continue
    torch.manual_seed(j.get('seed', 0)); t0 = time.time()
    if j['mode'] == 'design': w, sr = model('VoiceDesign').generate_voice_design(text=j['text'], language='English', instruct=j['instruct'])
    else:
        k = j['ref']
        if k not in _p: _p[k] = model('Base').create_voice_clone_prompt(ref_audio=k, ref_text=j['ref_text'])
        w, sr = model('Base').generate_voice_clone(text=j['text'], language='English', voice_clone_prompt=_p[k])
    Path(j['out']).parent.mkdir(parents=True, exist_ok=True); sf.write(j['out'], w[0], sr); dt = time.time() - t0
    print(json.dumps({'out': Path(j['out']).name, 'seconds_audio': round(len(w[0]) / sr, 2), 'seconds_wall': round(dt, 2), 'rtf': round(dt / max(.01, len(w[0]) / sr), 2), 'sr': sr, 'device': DEV}), flush=True)
