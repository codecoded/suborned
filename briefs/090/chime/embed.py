"""Brief 090 (Chime): speaker embeddings from Qwen3-TTS Base's own speaker encoder (the x-vector its cloning uses), and cosine similarities. Same model family as the voices, so it is a friendly judge: read the numbers as relative (a character against itself vs against the others), not as proof. Args: in.json out.json; in.json = {"name": [wavs]}."""
import json, sys
from pathlib import Path
import numpy as np, torch, librosa
from qwen_tts import Qwen3TTSModel
M = Path(__file__).resolve().parents[3] / 'out/scratch/chime/models/Qwen3-TTS-12Hz-1.7B-Base'
m = Qwen3TTSModel.from_pretrained(str(M), device_map='mps', dtype=torch.bfloat16, attn_implementation='sdpa')
out = {}
for k, ws in json.loads(Path(sys.argv[1]).read_text()).items():
    out[k] = {}
    for w in ws:
        a, _ = librosa.load(w, sr=24000, mono=True); e = m.model.extract_speaker_embedding(audio=a, sr=24000).float().cpu().numpy().ravel(); out[k][w] = (e / np.linalg.norm(e)).tolist()
Path(sys.argv[2]).write_text(json.dumps(out))
