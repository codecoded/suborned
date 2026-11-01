"""Brief 087 (Chime): takes → finished clips + .meta + briefs/087/cues.json. Deterministic: same takes, same bytes. Run from the repo root with the scratch venv:
   out/scratch/chime/venv/bin/python -I briefs/087/chime/build.py"""
import hashlib, json, subprocess, sys, tempfile
from pathlib import Path
import numpy as np
HERE = Path(__file__).resolve().parent; REPO = HERE.parents[2]
sys.path.insert(0, str(HERE))
import audiolib as A
from pack import CUES, BUSES, SPATIAL, T
OUT = REPO / 'unity/Assets/Suborned/Resources/Audio/087'
CEIL = -1.0


def load(path):
    p = REPO / path
    if p.suffix == '.ogg':
        with tempfile.TemporaryDirectory() as d:
            w = Path(d) / 'x.wav'; subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(p), '-c:a', 'pcm_s16le', str(w)], check=True); return A.read_wav(w)
    return A.read_wav(p)


def src_of(c, s):
    return s if c['kind'] == 'kenney' else T + s


def cut(x, sr, a, b): return x[int(a * sr):int(b * sr)]


def make(c):
    srcs = c['src'] if isinstance(c['src'], list) else [c['src']]; parts = []; sr = None
    for s in srcs:
        x, sr = load(src_of(c, s)); parts.append(x * 10 ** ((c['level'] - A.lufs_i(x, sr)) / 20) if c['even'] else x)
    x = parts[0]
    if len(parts) > 1:
        for y in parts[1:]: x = A.join(cut(x, sr, 0, 99)[:-int(0.1 * sr)], y[int(0.1 * sr):], sr, c['xf'])
        x = x[int(0.1 * sr):-int(0.1 * sr)]
    if c['cut']: x = cut(x, sr, *c['cut'])
    if isinstance(c['edit'], list):
        segs = [cut(x, sr, a, b) for a, b in c['edit']]; y = segs[0]
        for g in segs[1:]: y = A.join(y, g, sr, 0.02)
        x = y
    x = x if c['stereo'] else x.mean(axis=1, keepdims=True)
    for src, a, b, at, db, hp_hz in c['layers'] or []:  # a layer: a cut of another take, high-passed, laid in at `at`, so one clip carries the blast and the debris
        y, ysr = load(src_of(c, src)); y = A.hp(cut(y, ysr, a, b).mean(axis=1, keepdims=True), hp_hz, ysr) * 10 ** (db / 20); i = int(at * sr); n = i + len(y)
        if n > len(x): x = np.vstack([x, np.zeros((n - len(x), 1))])
        x = x.copy(); x[i:n] += A.fade(y, sr, 0.03, 0.2)
    if c['pitch']:  # a tape-speed shift: pitch and length together
        r = 2 ** (c['pitch'] / 12); x = A.resample(x, int(round(sr * r)), sr)
    x = A.resample(x, sr, c['rate']); sr = c['rate']
    if c['hp']: x = A.hp(x, c['hp'], sr)
    if c['lp']: x = A.lp(x, c['lp'], sr)
    if c['edit'] == 'double':
        gap = int(0.13 * sr); y = np.zeros((len(x) + gap, x.shape[1])); y[:len(x)] += x; y[gap:] += x; x = y
    if c['loop']:
        if c['note'] == 'cut to whole steps':
            per, conf = A.period(x, sr, 0.25, 1.0); k = round((len(x) / sr - c['xf']) / per - 0.5); n = int((k * per + c['xf']) * sr); x = x[:n]; c['_period'] = (round(per, 3), round(conf, 2), k)
        x = A.loop(x, sr, c['xf'])
    else:
        x = A.fade(x, sr, c['fin'] or 0.002, c['fade'] or 0.005)
    if c['group'] == 'fight' and not c['id'].startswith('down') or c['id'] in ('camera_click', 'notice_1', 'notice_2', 'notice_3', 'cut_in', 'patrol_break', 'car_warn_relay', 'car_warn_escape', 'car_warn_bell'):
        # on the frame: a cue that goes with a flash or a mark starts at its attack, 3 ms of lead kept and faded
        m = x.reshape(len(x), -1).mean(axis=1); h = max(1, int(0.002 * sr)); e = np.sqrt(np.convolve(m ** 2, np.ones(h) / h, 'same')); on = int(np.argmax(e > e.max() * 0.1)); x = A.fade(x[max(0, on - int(0.003 * sr)):], sr, 0.002, 0)
    # master: to the level, then to the ceiling if the level would pass it (the shortfall is recorded)
    meter = A.lufs_i if c['loop'] else A.lufs_m
    if c['drive']:  # a soft limit: guns are all peak, so without it the ceiling stops them short of their level
        d = 10 ** (c['drive'] / 20); x = x / (np.abs(x).max() + 1e-9); x = np.tanh(x * d) / np.tanh(d)
    g = c['level'] - meter(x, sr); y = x * 10 ** (g / 20); tp = A.true_peak(y)
    if tp > CEIL: y = y * 10 ** ((CEIL - tp) / 20)
    return y, sr


def meta(c, path, secs):
    guid = hashlib.md5(('suborned/087/' + path.name).encode()).hexdigest()
    load_, comp, q, pre = {'decompress': (0, 1, 0.7, 1), 'memory': (1, 1, 0.6, 1), 'stream': (2, 1, 0.45, 0)}[c['load']]
    three = 1 if c['spatial'].startswith('3d') else 0
    return f"""fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: {load_}
    sampleRateSetting: 0
    sampleRateOverride: {c['rate']}
    compressionFormat: {comp}
    quality: {q}
    conversionMode: 0
    preloadAudioData: {pre}
  platformSettingOverrides: {{}}
  forceToMono: {0 if c['stereo'] else 1}
  normalize: 0
  loadInBackground: {1 if c['load'] == 'stream' else 0}
  ambisonic: 0
  3D: {three}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def main():
    OUT.mkdir(parents=True, exist_ok=True); (OUT.parent / '087.meta').write_text(f"fileFormatVersion: 2\nguid: {hashlib.md5(b'suborned/087/folder').hexdigest()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    rows = []; ledger = {}
    for l in (REPO / 'paid/087-sound/ledger.jsonl').read_text().splitlines():
        r = json.loads(l)
        if r.get('file'): ledger[Path(r['file']).name] = r
    bed = None; built = {}
    for c in CUES:
        y, sr = make(c); p = OUT / f"{c['id']}.wav"; A.write_wav(p, y, sr); (OUT / f"{c['id']}.wav.meta").write_text(meta(c, p, len(y) / sr)); built[c['id']] = (y, sr)
    bed = built['street_bed']
    for c in CUES:
        y, sr = built[c['id']]; p = OUT / f"{c['id']}.wav"; b, cen = A.bands(y, sr)
        srcs = c['src'] if isinstance(c['src'], list) else [c['src']]; led = [ledger.get(s) for s in srcs]
        cost = 0.0 if c['kind'] == 'kenney' else round(sum((r or {}).get('cost_usd') or 0 for r in led), 5)
        m = {'seconds': round(len(y) / sr, 3), 'sample_rate': sr, 'channels': y.shape[1] if y.ndim > 1 else 1, 'bytes': p.stat().st_size, 'sample_peak_dbfs': round(A.sample_peak(y), 1), 'true_peak_dbtp': round(A.true_peak(y), 1), 'lufs_momentary_max': round(A.lufs_m(y, sr), 1),
             'clipped_samples': int((np.abs(y) >= 0.9995).sum()), 'bands_db_lt200_lt1k_lt4k_hi': b, 'centroid_hz': cen}
        if c['loop']: m['lufs_integrated'] = round(A.lufs_i(y, sr), 1); m['seam'] = A.seam(y, sr)
        if not c['loop']:
            q = y.reshape(len(y), -1).mean(axis=1); h = max(1, int(0.002 * sr)); e = np.sqrt(np.convolve(q ** 2, np.ones(h) / h, 'same')); m['onset_s'] = round(float(np.argmax(e > e.max() * 0.1) / sr), 3); m['peak_at_s'] = round(float(e.argmax() / sr), 3)
        sp = A.lufs_m(A.hp(y, 200, sr, 4), sr); m['small_speaker_lufs_m'] = round(sp, 1)  # a phone or laptop speaker is taken as nothing below 200 Hz
        if c.get('_period'): m['step_period_s'], m['step_confidence'], m['steps_in_loop'] = c['_period']
        if c['group'] in ('danger', 'clock') and c['id'] != 'street_bed':
            k = A.clear_of(y, sr, *bed); k['loudness_margin_lu'] = round(A.lufs_m(y, sr) - A.lufs_i(*bed), 1); m['clear_of_street_bed'] = k
        led = led + [ledger.get(l[0]) for l in c['layers'] or []]; srcs = srcs + [l[0] for l in c['layers'] or []]; cost = 0.0 if c['kind'] == 'kenney' else round(sum((r or {}).get('cost_usd') or 0 for r in led), 5)
        src_rec = {'kind': 'generated' if c['kind'] != 'kenney' else 'library', 'by': 'Seed Audio 1.0 via Runware (bytedance:seed-audio@1.0)' if c['kind'] != 'kenney' else 'Kenney',
                   'files': [str(Path(s).name) for s in srcs], 'prompt': (led[0] or {}).get('prompt') if led[0] else None, 'seed': (led[0] or {}).get('seed') if led[0] else None, 'ledger_ids': [(r or {}).get('id') for r in led] if c['kind'] != 'kenney' else []}
        rows.append({'id': c['id'], 'label': c['label'], 'trigger': c['trigger'], 'on_log': c['onlog'], 'file': f"Audio/087/{c['id']}", 'path': str(p.relative_to(REPO)), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest(), 'source': src_rec, 'licence': 'CC0-1.0, Kenney; briefs/087/licences/kenney-*.txt' if c['kind'] == 'kenney' else 'Runware terms: Partner Model outputs may be used commercially; briefs/087/licences/generated.md',
                     'cost_usd': cost, 'measured': m, 'mix_group': c['group'], 'voices_max': c['voices'], 'cooldown_s': c['cooldown'], 'loop': c['loop'], 'spatial': c['spatial'], 'priority': c['priority'], 'optional': c['optional'], 'import': c['load']})
        print(f"{c['id']:16} {m['seconds']:6.2f}s {m['bytes'] // 1024:5d}KB tp {m['true_peak_dbtp']:6.1f} M {m['lufs_momentary_max']:6.1f}" + (f" I {m['lufs_integrated']:6.1f} seam {m['seam']}" if c['loop'] else '') + (f" clear {m['clear_of_street_bed']['margin_top3_db']:5.1f} dB @{m['clear_of_street_bed']['top_bands_hz']} LU {m['clear_of_street_bed']['loudness_margin_lu']}" if 'clear_of_street_bed' in m else '') + (f" period {c['_period']}" if c.get('_period') else '') + f" cen {m['centroid_hz']} small {m['small_speaker_lufs_m']} on {m.get('onset_s')} pk {m.get('peak_at_s')}")
    doc = {'schema': 'suborned.audio-pack/1', 'brief': '087', 'note': 'Made by briefs/087/chime/build.py from paid/087-sound/takes and the Kenney packs. Measured numbers are not a judgement of how anything sounds: Chime cannot hear. Levels are per clip at gain 1; the buses and Options sit above them.', 'buses': BUSES, 'spatial': SPATIAL, 'cues': rows}
    (REPO / 'briefs/087/cues.json').write_text(json.dumps(doc, indent=1, ensure_ascii=False) + '\n')
    tot = sum(r['measured']['bytes'] for r in rows); print(f'{len(rows)} clips, {tot / 1e6:.2f} MB of WAV')


main()
