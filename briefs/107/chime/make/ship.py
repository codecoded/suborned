"""Brief 107 (Chime): Sky's picks (2026-11-11: "go with your recommendations, though rain needs to be light background noise") into the game.
Writes unity/Assets/Suborned/Resources/Audio/107/<id>.wav + .meta (import settings as the 087 pack's) and contracts/audio/weather.json (suborned.audio-weather/1,
schema contracts/audio/weather.schema.ts): which clip plays when, where, at what level. Called by build.py; deterministic (same takes, same bytes)."""
import hashlib, json, sys
from pathlib import Path
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[3]
sys.path.insert(0, str(REPO / 'briefs/087/chime'))
import audiolib as A
OUT = REPO / 'unity/Assets/Suborned/Resources/Audio/107'
CONTRACT = REPO / 'contracts/audio/weather.json'


def meta(name, load, rate, stereo, three):
    guid = hashlib.md5(('suborned/107/' + name).encode()).hexdigest()
    load_, comp, q, pre = {'decompress': (0, 1, 0.7, 1), 'memory': (1, 1, 0.6, 1), 'stream': (2, 1, 0.45, 0)}[load]
    return f"""fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: {load_}
    sampleRateSetting: 0
    sampleRateOverride: {rate}
    compressionFormat: {comp}
    quality: {q}
    conversionMode: 0
    preloadAudioData: {pre}
  platformSettingOverrides: {{}}
  forceToMono: {0 if stereo else 1}
  normalize: 0
  loadInBackground: {1 if load == 'stream' else 0}
  ambisonic: 0
  3D: {1 if three else 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def write(name, y, sr, c):
    p = OUT / f'{name}.wav'; A.write_wav(p, y, sr); stereo = y.ndim > 1 and y.shape[1] == 2
    (OUT / f'{name}.wav.meta').write_text(meta(name, c['load'], sr, stereo, c['spatial'].startswith('3d')))
    m = {'seconds': round(len(y) / sr, 3), 'sample_rate': sr, 'channels': 2 if stereo else 1, 'bytes': p.stat().st_size, 'lufs_integrated': round(A.lufs_i(y, sr), 1),
         'lufs_momentary_max': round(A.lufs_m(y, sr), 1), 'true_peak_dbtp': round(A.true_peak(y), 1)}
    if c['kind'] == 'loop': m['seam'] = A.seam(y, sr)
    return {'file': f'Audio/107/{name}', 'path': str(p.relative_to(REPO)), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest(), 'measured': m}


def ship(cues, doc_cues, picks):
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT.parent / '107.meta').write_text(f"fileFormatVersion: 2\nguid: {hashlib.md5(b'suborned/107/folder').hexdigest()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    rows = []; docs = {d['id']: d for d in doc_cues}
    for c in cues:
        d = docs[c['id']]; t = next(r for r in d['takes'] if r['id'] == d['pick']); src = t.get('source', [])
        base = {'label': c['label'], 'role': c['role'], 'when': c['when'], 'place': c['place'], 'plays': c['plays'], 'mix_group': 'ambience', 'voices_max': c['voices'], 'cooldown_s': c['cooldown'],
                'loop': c['kind'] == 'loop', 'spatial': c['spatial'], 'level': {'lufs': c['level'], 'meter': 'integrated' if c['meter'] == 'i' else 'momentary max'}, 'import': c['load'], 'optional': c['optional'],
                'source': {'by': 'Seed Audio 1.0 via Runware (bytedance:seed-audio@1.0)', 'takes': [s['take'] for s in src], 'ledger_ids': [s['ledger_id'] for s in src], 'cost_usd': round(sum(s['cost_usd'] for s in src), 6)}}
        if c['id'] == 'wet_steps':
            ys, sr = picks['wet_steps']
            for i, y in enumerate(ys): rows.append({'id': f'wet_step_{i + 1}', **base, 'label': f"{c['label']}, step {i + 1} of {len(ys)}", **write(f'wet_step_{i + 1}', y, sr, c)})
        else:
            rows.append({'id': c['id'], **base, **write(c['id'], *picks[c['id']], c)})
    doc = {'schema': 'suborned.audio-weather/1', 'brief': '107',
           'note': 'Chime, brief 107, 2026-11-11. The weather and night sounds Sky picked ("go with your recommendations, though rain needs to be light background noise"). Made by briefs/107/chime/make/build.py from paid/107-weather-sound/takes; never edit by hand. '
                   'Rows read like briefs/087/cues.json rows (file, mix_group, voices_max, cooldown_s, loop, spatial: the 087 pack\'s buses and spatial table), and add when (the start state\'s weather and light, Game.Weather / Game.Light), role and place. Levels are per clip at gain 1. How to play them: briefs/107/chime/wiring.md.',
           'cues': rows}
    CONTRACT.write_text(json.dumps(doc, indent=1, ensure_ascii=False) + '\n')
    for r in rows: print(f"shipped {r['id']:12} {r['measured']['seconds']:6.2f}s I {r['measured']['lufs_integrated']:6.1f} M {r['measured']['lufs_momentary_max']:6.1f} tp {r['measured']['true_peak_dbtp']:6.1f} {r['path']}")
    return {'unity': str(OUT.relative_to(REPO)), 'contract': str(CONTRACT.relative_to(REPO)), 'clips': [r['id'] for r in rows]}
