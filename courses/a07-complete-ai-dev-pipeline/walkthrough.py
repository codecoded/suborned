"""Render 4K supporting evidence, not a screen recording of live generation.

Actually runs the existing exporter before rendering. Requires the retained author
inputs, game captures, Pillow, OpenCV and ffmpeg. No API calls or Unity execution.
"""
import contextlib
import hashlib
import importlib.util
import io
import json
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
OUT = ROOT / 'out/scratch/a07/video-hq'
OUT.mkdir(parents=True, exist_ok=True)
W, H = 3840, 2160
BG, INK, MUTED, ACCENT, GOLD = '#102e30', '#f7f4ec', '#acc5bf', '#8cd6c3', '#ecd49b'
FONTS = ROOT / 'briefs/011/ui/kit/fonts'
spec = importlib.util.spec_from_file_location('a07_evidence', HERE / 'evidence.py')
evidence = importlib.util.module_from_spec(spec)
spec.loader.exec_module(evidence)
log = io.StringIO()
with contextlib.redirect_stdout(log):
    evidence.verify()
    evidence.replay(OUT / 'export')
(OUT / 'executed-output.txt').write_text(log.getvalue())
print(log.getvalue(), flush=True)


def font(size, style='body'):
    names = {'body': FONTS / 'Barlow-Regular.ttf', 'bold': FONTS / 'Jost-SemiBold.ttf',
             'mono': Path('/System/Library/Fonts/Menlo.ttc')}
    return ImageFont.truetype(str(names[style]), size)


def text(im, value, xy, size=68, colour=INK, style='body', width=None):
    d, f = ImageDraw.Draw(im), font(size, style)
    x, y = xy
    for line in value.split('\n'):
        assert d.textlength(line, font=f) <= (width or W - x - 110), f'Text too wide: {line}'
        assert y + size * 1.3 < H - 130, f'Text too low: {line}'
        d.text((x, y), line, font=f, fill=colour)
        y += round(size * 1.35)


def base(title, index, label):
    im = Image.new('RGB', (W, H), BG)
    d = ImageDraw.Draw(im)
    d.rectangle((112, 104, 124, 160), fill=ACCENT)
    text(im, 'THE SUBORNED', (156, 87), 55, ACCENT, 'bold')
    text(im, f'A07 / {label.upper()}', (2500, 94), 43, MUTED)
    text(im, title, (112, 225), 126, INK, 'bold')
    d.line((112, H - 125, W - 112, H - 125), fill='#3b5a58', width=2)
    d.text((112, H - 99), 'SUPPORTING EVIDENCE / Historical generation + executed offline export',
           font=font(40), fill=MUTED)
    d.text((W - 295, H - 101), f'{index:02d} / 08', font=font(40), fill=MUTED)
    return im


def picture(im, path, box):
    with Image.open(path) as src:
        src = src.convert('RGBA')
        scale = min((box[2] - box[0]) / src.width, (box[3] - box[1]) / src.height)
        src = src.resize((round(src.width * scale), round(src.height * scale)), Image.Resampling.LANCZOS)
        xy = (box[0] + (box[2] - box[0] - src.width) // 2,
              box[1] + (box[3] - box[1] - src.height) // 2)
        im.paste(src, xy, src)


stages = []


def stage(im, seconds, name):
    path = OUT / f'{len(stages):03d}-{name}.png'
    im.save(path)
    stages.append((path, seconds))


im = base('From generated art to a live HUD', 1, 'Evidence overview')
for i, agent in enumerate(evidence.AGENTS):
    picture(im, ROOT / f'briefs/033/portraits/final/master/{agent}_calm.png',
            (170 + i * 920, 530, 870 + i * 920, 1230))
    text(im, agent.upper(), (350 + i * 920, 1290), 66, ACCENT, 'bold')
text(im, 'Four agents. Nine expressions each. Present in local build 0.2.232.', (170, 1510), 80)
text(im, 'An annotated evidence walkthrough.\nNo live model generation or Unity build is recorded here.', (170, 1690), 68, MUTED)
stage(im, 9, 'overview')

im = base('01 / Historical AI generation', 2, 'Source + receipts')
picture(im, ROOT / 'briefs/033/portraits/scout_states_t3.png', (120, 440, 1640, 1960))
text(im, 'SEEDREAM / RUNWARE', (1810, 490), 58, ACCENT, 'bold')
text(im, '14 calls', (1810, 630), 156, INK, 'bold')
text(im, '$1.15560 actual', (1810, 845), 117, GOLD, 'bold')
text(im, 'Includes rejected and superseded takes.', (1810, 1040), 66, MUTED)
text(im, 'Retained evidence', (1810, 1260), 76, INK, 'bold')
text(im, 'Original prompts and model IDs\nProvider-reported costs\nRaw output hashes and selected takes', (1810, 1390), 70, MUTED)
text(im, 'Original generation is historical.', (1810, 1810), 60, GOLD)
stage(im, 11, 'generation')

im = base('02 / Export replay executed', 3, 'Author-machine execution')
ImageDraw.Draw(im).rounded_rectangle((112, 480, 3728, 1450), radius=30, fill='#091e20')
text(im, '$ python3 …/evidence.py replay', (190, 565), 76, ACCENT, 'mono')
for i, agent in enumerate(evidence.AGENTS):
    text(im, f'{agent:<9} ok 9', (190, 760 + i * 125), 76, INK, 'mono')
text(im, '32 exact matches', (1900, 770), 115, ACCENT, 'bold')
text(im, 'Byte for byte against Unity sprites.', (1900, 950), 67, MUTED)
text(im, '4 downed overrides excluded.', (1900, 1100), 67, GOLD)
text(im, 'Ring detection / alpha mask / resize / fixed filenames', (140, 1570), 79)
text(im, 'The command ran before this video was rendered; full stdout is retained.\nThis page reports the results. No API calls were made.', (140, 1740), 63, MUTED)
stage(im, 13, 'export')

im = base('03 / The production sprite set', 4, 'Tracked Unity assets')
text(im, '36 finals match local build inputs. Runtime sprites are 128 px; enlarged here.', (112, 405), 65, MUTED)
for col, state in enumerate(evidence.STATES):
    text(im, state.replace('_', '\n'), (720 + col * 330, 560), 43, MUTED)
for row, agent in enumerate(evidence.AGENTS):
    y = 750 + row * 290
    text(im, agent.upper(), (140, y + 70), 66, ACCENT, 'bold')
    for col, state in enumerate(evidence.STATES):
        x = 720 + col * 330
        picture(im, evidence.UNITY / f'{agent}_{state}@2x.png', (x, y, x + 236, y + 236))
text(im, 'Manual exception: four downed replacements were selected and finished by hand.', (140, 1900), 63, GOLD)
stage(im, 11, 'sprites')

im = base('04 / The engine integration', 5, 'Production code excerpts')
build = (ROOT / 'unity/Assets/Suborned/Editor/World/Play/GameBuild.cs').read_text()
hook = next(line.split('//')[0].strip() for line in build.splitlines() if 'KitImport.LivePortraits()' in line)
text(im, 'BUILD HOOK / GameBuild.cs', (150, 470), 55, ACCENT, 'bold')
text(im, hook, (150, 595), 66, INK, 'mono')
text(im, 'KitImport.cs copies PNGs and sets Sprite importers.', (150, 780), 79)
ImageDraw.Draw(im).line((150, 995, 3690, 995), fill='#3b5a58', width=3)
text(im, 'RUNTIME / HudToolkit.Live.cs', (150, 1110), 55, ACCENT, 'bold')
code = (ROOT / 'unity/Assets/Suborned/Runtime/Play/HudToolkit.Live.cs').read_text()
assert 'host.FaceOf(i)' in code and 'HandoverHost.FaceFiles[(int)face]' in code
text(im, 'host.FaceOf(i)', (150, 1250), 87, INK, 'mono')
text(im, 'Simulation state selects the named portrait.\nThe HUD cross-fades between expression sprites.', (150, 1435), 83)
text(im, 'Existing code hooks; Unity is not running in this walkthrough.', (150, 1830), 65, GOLD)
stage(im, 13, 'integration')

im = base('05 / Repair only the rejected state', 6, 'Recorded cost change')
text(im, 'BEFORE / Full nine-state sheet', (160, 500), 67, MUTED)
text(im, '$0.09630 per call', (160, 610), 116, INK, 'bold')
ImageDraw.Draw(im).rectangle((160, 810, 3480, 915), fill='#647f7b')
text(im, 'AFTER / Three downed options only', (160, 1050), 67, MUTED)
text(im, '$0.04815 per call', (160, 1160), 116, ACCENT, 'bold')
ImageDraw.Draw(im).rectangle((160, 1360, 1820, 1465), fill=ACCENT)
text(im, '50% lower cost per repair call', (160, 1610), 100, GOLD, 'bold')
text(im, 'A narrower output scope, not equal work or a per-portrait saving.\nPortrait-job costs only; excludes other jobs and subscriptions.', (160, 1790), 65, MUTED)
stage(im, 12, 'costs')

# Play every source gameplay frame at its native 20 fps, once full frame and once
# as a labelled HUD detail replay. No generated/interpolated gameplay frames.
game = ROOT / 'out/scratch/itch-screenshots/fire/raw/firefight-live'
report = json.loads((game.parent / 'shots.json').read_text())
assert report['status'] == 'ok' and '0.2.232' in report['build']
for detail in (False, True):
    for i in range(60):
        with Image.open(game / f'f{i:04d}.png') as src:
            if not detail:
                im = src.convert('RGB').resize((W, H), Image.Resampling.LANCZOS)
                d = ImageDraw.Draw(im)
                d.rectangle((0, H - 155, W, H), fill=BG)
                d.text((100, H - 125), 'RECORDED GAMEPLAY / 0.2.232 / 1080p source, 20 fps', font=font(53, 'bold'), fill=INK)
            else:
                im = base('06 / The portraits in the live HUD', 7, 'Gameplay detail replay')
                crop = src.crop((0, 0, 300, 530)).convert('RGBA')
                crop = crop.resize((760, 1343), Image.Resampling.LANCZOS)
                im.paste(crop, (240, 490))
                text(im, 'Actual roster during combat', (1280, 670), 109, INK, 'bold')
                text(im, 'The same three-second capture,\nreplayed as a close-up.', (1280, 945), 84, MUTED)
                text(im, 'No staged expression overrides\nwere used for this capture.', (1280, 1345), 84, MUTED)
        stage(im, .05, f'game-{int(detail)}-{i:03d}')

im = base('Evidence you can inspect', 8, 'Repository + limits')
text(im, 'github.com/codecoded/suborned', (150, 520), 108, ACCENT, 'bold')
text(im, 'branch: assignment-07', (150, 690), 83, MUTED)
text(im, 'courses/a07-complete-ai-dev-pipeline/', (150, 880), 76, INK, 'mono')
text(im, 'Receipts. Hashes. Cost arithmetic. Export replay. Integration code.', (150, 1080), 77)
text(im, 'Still required for the final submission', (150, 1420), 94, GOLD, 'bold')
text(im, 'A recorded primary pipeline execution into the engine,\nthe final audit, and a verified two-minute first-play path.', (150, 1590), 80, MUTED)
stage(im, 10, 'evidence')

manifest = OUT / 'frames.ffconcat'
manifest.write_text('ffconcat version 1.0\n' + ''.join(
    f"file '{path.name}'\noption framerate 20\nduration {seconds:.3f}\n"
    for path, seconds in stages) + f"file '{stages[-1][0].name}'\noption framerate 20\n")
target = OUT / 'TheSuborned-A07-supporting-evidence-4K.mp4'
seconds = round(sum(t for _, t in stages), 2)
subprocess.run(['ffmpeg', '-y', '-loglevel', 'warning', '-f', 'concat', '-safe', '0',
                '-i', str(manifest), '-t', str(seconds), '-r', '20', '-an',
                '-c:v', 'libx264', '-preset', 'veryfast', '-crf', '16',
                '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(target)], check=True)
receipt = {
    'video': target.name, 'sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
    'seconds': seconds, 'resolution': [W, H], 'fps': 20, 'crf': 16,
    'gameplay_source': '1920x1080 at 20 fps; full-frame upscale and labelled detail replay',
    'type': 'Annotated supporting evidence; historical generation and actual offline export results, not a primary execution recording',
    'model_calls_during_video_preparation': 0, 'game_build': '0.2.232', 'public_video_url': None,
    'chapters': [{'start': round(sum(t for _, t in stages[:i]), 2), 'image': p.name}
                 for i, (p, _) in enumerate(stages) if i < 6 or i == len(stages) - 1],
}
(OUT / 'video.json').write_text(json.dumps(receipt, indent=2) + '\n')
print(target, flush=True)
