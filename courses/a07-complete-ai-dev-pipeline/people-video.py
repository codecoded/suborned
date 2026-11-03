"""Edit the real recorded people-pipeline session and its player captures.

Graphics are explanatory inserts. CLI clips retain real command output/timing;
engine load/save waits are cut. No simulated terminal or generated game imagery.
"""
import hashlib
import json
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
OUT = ROOT / 'out/scratch/a07/people-run'
EDIT = OUT / 'edit'
EDIT.mkdir(exist_ok=True)
W, H = 3840, 2160
BG, INK, MUTE, GREEN, GOLD = '#102e30', '#f7f4ec', '#acc5bf', '#8cd6c3', '#ecd49b'
FONT = ROOT / 'briefs/011/ui/kit/fonts'
evidence = json.loads((HERE / 'evidence/people.json').read_text())
assert evidence['integration']['player_report']['status'] == 'ok'
raw_cast = [json.loads(line) for line in (OUT / 'people-run.cast').read_text().splitlines()]
start_game = next(row[0] for row in raw_cast[1:] if '[5/6]' in row[2])
end_game = next(row[0] for row in raw_cast[1:] if '[6/6]' in row[2])
recording = json.loads((OUT / 'terminal-recording.json').read_text())
assert recording['exit_code'] == 0
clips, chapters = [], []


def font(size, bold=False):
    return ImageFont.truetype(str(FONT / ('Jost-SemiBold.ttf' if bold else 'Barlow-Regular.ttf')), size)


def text(im, words, x, y, size=80, colour=INK, bold=False):
    d, f = ImageDraw.Draw(im), font(size, bold)
    for line in words.split('\n'):
        assert d.textlength(line, font=f) < W - x - 110, line
        d.text((x, y), line, font=f, fill=colour)
        y += int(size * 1.4)


def card(title, sub):
    im = Image.new('RGB', (W, H), BG)
    text(im, 'THE SUBORNED / SKY RUMSEY / ASSIGNMENT 7', 130, 85, 48, GREEN)
    text(im, title, 130, 260, 130, INK, True)
    text(im, sub, 130, 450, 67, MUTE)
    ImageDraw.Draw(im).line((130, 2010, 3710, 2010), fill='#3b5a58', width=2)
    text(im, 'Historical AI generation / Actual offline pipeline execution and Unity player capture', 130, 2050, 42, MUTE)
    return im


def ffmpeg(args, path):
    if path.exists() and path.stat().st_size > 1024:
        return  # Resume unchanged completed segments after an encoder failure.
    subprocess.run(['ffmpeg', '-y', '-v', 'error', *args, '-an', '-r', '20',
                    '-c:v', 'libx264', '-preset', 'veryfast', '-crf', '16',
                    '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(path)], check=True)


def still(im, seconds, name):
    p = EDIT / f'{len(clips):02d}-{name}.png'; im.save(p)
    clip = p.with_suffix('.mp4')
    ffmpeg(['-loop', '1', '-i', str(p), '-t', str(seconds)], clip)
    chapters.append({'start': round(sum(t for _, t in clips), 2), 'name': name})
    clips.append((clip, seconds))


im = card('A city made of people', 'The production path behind the Handover\'s social infiltration')
text(im, 'GDD + rules', 160, 790, 108, GREEN, True)
text(im, 'RAG + cast generation', 160, 1030, 108, INK, True)
text(im, 'Checks + critique + promotion', 160, 1270, 108, INK, True)
text(im, 'People + routines  /  Engine export  /  Unity play', 160, 1510, 100, GOLD, True)
text(im, 'Original AI responses are archived; the downstream pipeline runs now.', 160, 1810, 67, MUTE)
still(im, 8, 'Production path')

im = card('What the AI actually produced', 'Archived r016 / Pulse round 2 / original retrieved context and structured response')
text(im, 'RETRIEVED PEOPLE RULES', 160, 730, 62, GREEN, True)
text(im, 'Routine + need + temperament + memory + secret\nOne need per person; memory starts empty.\nStarting relationships are represented as ties.', 160, 860, 82)
text(im, 'ACCEPTED CAFE CAST ROW', 160, 1270, 62, GREEN, True)
row = json.loads((ROOT / 'runs/levels/harbour-district/r016/round-2/pulse.json').read_text())['cast']['rows'][0]
assert row['positions'][0]['weight'] == 60 and row['positions'][1]['weight'] == 40
text(im, '60% servers / 40% cooks\nTemperament, need and relationship mixes; routine choices.', 160, 1400, 88)
text(im, 'Original prompts, passages, outputs and costs are retained in the repository.', 160, 1810, 65, MUTE)
still(im, 10, 'Original generation evidence')

terminal = OUT / 'people-terminal-4K.mp4'
p = EDIT / '02-actual-cli.mp4'
# Keep the actual recorded verification, expansion, export and staging at real speed.
ffmpeg(['-i', str(terminal), '-t', str(start_game + 1.5)], p)
chapters.append({'start': round(sum(t for _, t in clips), 2), 'name': 'Recorded pipeline execution'})
clips.append((p, start_game + 1.5))


def gameplay(name, caption):
    src = OUT / 'gameplay' / name
    frames = sorted(src.glob('f*.jpg'))
    assert frames
    label = EDIT / f'{name}-overlay.png'
    overlay = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(overlay)
    lines = [caption, 'A07 replay variant / 1080p source at 20 fps']
    f = font(49)
    width = max(d.textlength(s, font=f) for s in lines)
    x = round((W - width) / 2)
    d.rounded_rectangle((x - 25, 140, x + width + 25, 300), radius=16, fill=BG)
    for i, s in enumerate(lines): d.text((x, 150 + i * 65), s, font=f, fill=INK)
    overlay.save(label)
    p = EDIT / f'{len(clips):02d}-{name}.mp4'
    ffmpeg(['-framerate', '20', '-i', str(src / 'f%04d.jpg'), '-loop', '1', '-i', str(label),
            '-filter_complex', f'[0:v]scale={W}:{H}:flags=lanczos[game];[game][1:v]overlay=0:0:shortest=1',
            '-t', str(len(frames) / 20)], p)
    chapters.append({'start': round(sum(t for _, t in clips), 2), 'name': caption})
    clips.append((p, len(frames) / 20))


gameplay('cafe-routines', 'Generated people following the cafe routines')
im = Image.open(OUT / 'gameplay/social-intel.png').convert('RGB').resize((W, H), Image.Resampling.LANCZOS)
ImageDraw.Draw(im).rectangle((650, 150, 3190, 370), fill=BG)
text(im, 'Listen reveals Inez\'s need and her connection to Arthur.', 720, 170, 68)
text(im, 'Actual captured Intel frame / A07 replay variant / 1080p source', 720, 280, 49, MUTE)
still(im, 8, 'The social route in the game')
gameplay('guard-leaves-post', 'Inez calls Arthur away: a relationship changes the route')
gameplay('street-level-people', 'Street-level view / The built world and kit provide the setting')

p = EDIT / '07-actual-result.mp4'
end_length = recording['wall_seconds'] - end_game
ffmpeg(['-ss', str(max(0, end_game - .2)), '-i', str(terminal), '-t', str(end_length + .2),
        '-vf', 'tpad=stop_mode=clone:stop_duration=4'], p)
chapters.append({'start': round(sum(t for _, t in clips), 2), 'name': 'Recorded result and costs'})
clips.append((p, end_length + .2 + 4))

im = card('Recorded cost, including retries', 'r016 / Three actual historical model calls / Claude subscription usage valuation')
text(im, '$0.6905', 160, 760, 220, GREEN, True)
text(im, 'Highest call: Keel critic / $0.2800659', 160, 1110, 90)
text(im, 'Mid-project change: checks before critique', 160, 1390, 90, GOLD, True)
text(im, 'Old r008: 2 critic calls on failed candidates / $0.4135186\nNew r016 failed round: 0 critic calls / 0 critic tokens / $0', 160, 1550, 76)
text(im, 'Different candidates and objectives. No equal-work percentage is claimed.', 160, 1830, 63, MUTE)
still(im, 12, 'Cost and optimisation')

im = card('What this run proves', 'Original cast lineage / real regeneration / automatic staging / functioning Unity player')
text(im, '130 people / 222 relationships / 87 places', 160, 770, 112, GREEN, True)
text(im, 'The four mission profiles match the shipped game.\nThe remapped background population differs.', 160, 1030, 90)
text(im, 'Architecture change: pin the complete dependency graph.\nA promoted cast alone does not pin a whole release.', 160, 1390, 87, GOLD)
text(im, 'Original AI generation is replayed. Existing player binary; no new Unity compilation.\nLoad/save waits were removed; raw timed terminal output is retained.', 160, 1770, 61, MUTE)
still(im, 11, 'Scope and architectural reflection')

im = card('Submission evidence', 'github.com/codecoded/suborned / branch assignment-07')
text(im, 'courses/a07-complete-ai-dev-pipeline/', 160, 800, 100, GREEN, True)
text(im, 'Run receipts, exact hashes, replay commands and a one-page audit.', 160, 1040, 83)
text(im, 'Playable: codecoded.itch.io/the-suborned', 160, 1360, 93, INK, True)
text(im, 'Remaining release gate: a compatible stranger must reach play within 120 seconds.\nThat timing has not been certified by this pipeline recording.', 160, 1690, 69, GOLD)
still(im, 8, 'Links and remaining playable gate')

manifest = EDIT / 'clips.txt'
manifest.write_text(''.join(f"file '{p.name}'\n" for p, _ in clips))
target = OUT / 'TheSuborned-A07-People-Pipeline-4K.mp4'
subprocess.run(['ffmpeg', '-y', '-v', 'error', '-f', 'concat', '-safe', '0', '-i', str(manifest),
                '-c', 'copy', '-movflags', '+faststart', str(target)], check=True)
receipt = {
    'video': str(target.relative_to(ROOT)), 'sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
    'resolution': [W, H], 'fps': 20, 'chapters': chapters,
    'terminal_source': recording,
    'edit': 'Explanatory graphics; actual CLI at original speed; engine loading/saving wait removed; actual player frames from this run; final result held 4 seconds for readability',
    'model_calls_this_run': 0, 'fresh_unity_compilation': False,
    'gameplay_source': '1920x1080 at 20 fps, upscaled; local current-plan people variant in retained 0.2.232 binary',
    'youtube_url': None,
}
(OUT / 'video.json').write_text(json.dumps(receipt, indent=2) + '\n')
print(target, flush=True)
