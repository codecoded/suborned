"""Brief 104 (Chime): builds briefs/104/chime/robot-sfx.html from make/robot-sfx.src.html, measures.json and the drafts.
Every <audio data-clip="name"> in the source gets its clip embedded (FLAC for the drafts, exact as measured; MP3 for the long preview and the
pack's reference clips), so the page is one self-contained file. <!--measures--> becomes the measured table.
Run from the repo root after robot_synth.py, robot_measure.py and robot_paid.py (+ preview):   out/scratch/chime/venv/bin/python -I briefs/104/chime/make/report.py"""
import base64, json, re, subprocess, tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent; CH = HERE.parent; REPO = HERE.parents[3]
PACK = REPO / 'unity/Assets/Suborned/Resources/Audio/087'
REFS = {'fire_robot', 'fire_pistol', 'fire_police', 'police_siren', 'patrol_loop', 'patrol_break', 'notice_1', 'notice_3', 'down_robot', 'hit_metal', 'fire_rival'}


def encode(name):
    paid = CH / 'paid' / f'{name}.wav'
    src = paid if paid.exists() else (PACK if name in REFS else CH / 'drafts') / f'{name}.wav'
    flac = (name.startswith('robot_') or paid.exists()) and 'preview' not in name
    extra = ['-t', '6'] if name in ('patrol_loop', 'roll_ball_t2') else []
    with tempfile.TemporaryDirectory() as d:
        out = Path(d) / ('x.flac' if flac else 'x.mp3')
        cmd = ['ffmpeg', '-v', 'error', '-y', '-i', str(src), *extra] + (['-c:a', 'flac'] if flac else ['-c:a', 'libmp3lame', '-b:a', '128k'])
        subprocess.run(cmd + ['-map_metadata', '-1', str(out)], check=True)
        return f"data:audio/{'flac' if flac else 'mpeg'};base64," + base64.b64encode(out.read_bytes()).decode()


def table(m):
    rows = []
    for k, v in m['drafts'].items():
        c = v.get('clear_of_bed_and_roll') or {}; g = v.get('clear_of_gunfire') or {}
        lvl = f"{v['lufs_integrated']} I" if 'lufs_integrated' in v else f"{v['lufs_momentary_max']} M"
        seam = f"{v['seam']['step_ratio']} / {v['seam']['level_jump_db']} dB" if 'seam' in v else '–'
        rows.append(f"<tr><td><code>{k}</code></td><td>{v['seconds']}</td><td>{v['true_peak_dbtp']}</td><td>{lvl}</td><td>{v['small_speaker_lufs_m']}</td>"
                    f"<td>{v['centroid_hz']}</td><td>{c.get('margin_top3_db', '–')}</td><td>{c.get('loudness_margin_lu', '–')}</td><td>{g.get('margin_top3_db', '–')}</td><td>{seam}</td></tr>")
    head = ('<tr><th>Draft</th><th>s</th><th>True peak dBTP</th><th>LUFS</th><th>Small speaker LUFS-M</th><th>Centroid Hz</th>'
            '<th>Over bed + roll, dB</th><th>Over bed + roll, LU</th><th>Over gunfire, dB</th><th>Loop seam (step ratio / jump)</th></tr>')
    return f'<div class="scroll"><table class="num">{head}{"".join(rows)}</table></div>'


def paid_table(m):
    """The paid candidates, mastered, beside the drafts: level, peak, clean ends, floor, shape distance from the 087 sound each sits with, clear of the bed and of gunfire."""
    rows = []
    for k, v in {**m['clips'], **m['drafts']}.items():
        lvl = f"{v['lufs_integrated']} I" if 'lufs_integrated' in v else f"{v['lufs_momentary_max']} M"
        ends = f"{v['seam']['step_ratio']} / {v['seam']['level_jump_db']} dB (seam)" if 'seam' in v else f"{v['head_5ms_db']} / {v['tail_20ms_db']}"
        vs = ', '.join(f"{r} {q['shape_distance_db']}" for r, q in v['vs_pack'].items()); c = v.get('clear_of_bed_and_roll') or {}; g = v.get('clear_of_gunfire') or {}
        who = 'synth draft' if k.startswith('robot_') else (v.get('model') or '').split(':')[0] + f" #{v.get('ledger_id')}"
        rows.append(f"<tr><td><code>{k}</code></td><td>{who}</td><td>{v['seconds']}</td><td>{lvl}</td><td>{v['true_peak_dbtp']}</td><td>{v['clipped_samples']}</td><td>{ends}</td><td>{v.get('floor_p10_db', '–')}</td>"
                    f"<td>{v['centroid_hz']}</td><td>{vs}</td><td>{c.get('margin_top3_db', '–')}</td><td>{g.get('margin_top3_db', '–')}</td></tr>")
    head = ('<tr><th>Clip</th><th>From</th><th>s</th><th>LUFS</th><th>True peak dBTP</th><th>Clipped</th><th>Head 5 ms / tail 20 ms, dB under peak</th><th>Floor, dB</th>'
            '<th>Centroid Hz</th><th>Shape distance from the 087 sound, dB</th><th>Over bed + roll, dB</th><th>Over gunfire, dB</th></tr>')
    return f'<div class="scroll"><table class="num">{head}{"".join(rows)}</table></div>'


def main():
    m = json.loads((CH / 'measures.json').read_text()); html = (HERE / 'robot-sfx.src.html').read_text()
    html = html.replace('<!--measures-->', table(m)).replace('<!--paid-measures-->', paid_table(json.loads((CH / 'paid-measures.json').read_text())))
    html = re.sub(r'<audio data-clip="([a-z0-9_]+)"([^>]*)>', lambda r: f'<audio controls preload="metadata"{r.group(2)} src="{encode(r.group(1))}">', html)
    out = CH / 'robot-sfx.html'; out.write_text(html); print(out, f'{out.stat().st_size // 1024} KB')


if __name__ == '__main__':
    main()
