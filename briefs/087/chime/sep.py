"""Brief 087 (Chime): a local stem split with Demucs (htdemucs, code MIT): writes <out>/<name>.<stem>.wav. Run with the sep venv: out/scratch/chime/sep/bin/python -I briefs/087/chime/sep.py <out dir> <wavs...>"""
import sys
from pathlib import Path
import numpy as np, soundfile as sf, torch
from demucs.pretrained import get_model
from demucs.apply import apply_model
out = Path(sys.argv[1]); out.mkdir(parents=True, exist_ok=True); model = get_model('htdemucs'); dev = 'cpu' if torch.__dict__.get('_no_mps') else ('mps' if torch.backends.mps.is_available() else 'cpu'); model.to(dev); print('device', dev, 'rate', model.samplerate, model.sources)
for p in sys.argv[2:]:
    x, sr = sf.read(p, always_2d=True); assert sr in (44100, 48000); w = torch.tensor(x.T, dtype=torch.float32)
    if sr != model.samplerate:
        import torchaudio.functional as F; w = F.resample(w, sr, model.samplerate)
    mu, sd = w.mean(), w.std(); s = apply_model(model, ((w - mu) / sd)[None].to(dev), split=True, overlap=0.25, device=dev, progress=False)[0].cpu() * sd + mu / 4
    for name, st in zip(model.sources, s): sf.write(str(out / f'{Path(p).stem}.{name}.wav'), st.T.numpy(), model.samplerate, subtype='PCM_16')
    print('done', p, flush=True)
