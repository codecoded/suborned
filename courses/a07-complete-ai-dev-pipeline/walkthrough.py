"""Make an explicitly labelled evidence walkthrough, not a new live AI-generation recording.

Runs the real deterministic exporter, retains its stdout, then presents those results
with historical input sheets, tracked Unity sprites, importer code and real game frames.
Author machine only: needs retained AI sheets, screenshots, Pillow, NumPy, OpenCV and ffmpeg.
"""
import contextlib
import hashlib
import importlib.util
import io
import json
import subprocess
import textwrap
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
OUT = ROOT / 'out/scratch/a07/video'
OUT.mkdir(parents=True, exist_ok=True)
spec = importlib.util.spec_from_file_location('a07_evidence', HERE / 'evidence.py')
evidence = importlib.util.module_from_spec(spec)
spec.loader.exec_module(evidence)
log = io.StringIO()
with contextlib.redirect_stdout(log):
    evidence.verify()
    evidence.replay(OUT / 'export')
(OUT / 'executed-output.txt').write_text(log.getvalue())
print(log.getvalue(), flush=True)

FONT = '/System/Library/Fonts/Supplemental/Arial.ttf'
MONO = '/System/Library/Fonts/Menlo.ttc'
def font(size, mono=False):
    return ImageFont.truetype(MONO if mono else FONT, size)

def page(title, subtitle):
    im = Image.new('RGB', (1600, 900), '#f4f0e7')
    d = ImageDraw.Draw(im)
    d.text((55, 25), 'THE SUBORNED / ASSIGNMENT 7', fill='#146b66', font=font(22))
    d.text((55, 75), title, fill='#173c3d', font=font(43))
    d.text((55, 137), subtitle, fill='#536567', font=font(23))
    d.line((55, 815, 1545, 815), fill='#c9d2cd', width=2)
    d.text((55, 836), 'OFFLINE REPLAY | Original model generation is historical. No fresh API calls in this video.', fill='#a34430', font=font(22))
    return im

def lines(im, text, xy, width=90, size=25, colour='#173c3d', mono=False):
    d = ImageDraw.Draw(im); x, y = xy
    for paragraph in text.split('\n'):
        for line in textwrap.wrap(paragraph, width=width, replace_whitespace=False) or ['']:
            d.text((x, y), line, fill=colour, font=font(size, mono)); y += int(size * 1.5)

def picture(im, path, box):
    image = Image.open(path).convert('RGBA')
    image.thumbnail((box[2] - box[0], box[3] - box[1]), Image.Resampling.LANCZOS)
    im.paste(image, (box[0] + (box[2]-box[0]-image.width)//2, box[1]), image)

stages = []
im = page('1 / Recorded AI generation', '14 real Seedream / Runware receipts: $1.15560, including superseded takes.')
picture(im, ROOT / 'briefs/033/portraits/scout_states_t3.png', (50, 200, 650, 790))
lines(im, 'Original prompt: a 3 x 3 expression sheet of the same person, reference-conditioned.\n\nNine states: calm, focused, noticed, under fire, firing, hurt, badly hurt, downed, success.\n\nHuman-approved source takes: Scout / Fixer / Enforcer t3; Tech t4.\n\nThe ledger retains prompts, raw output hashes and provider-reported costs.', (710, 220), width=48, size=26)
stages.append((im, 9))

im = page('2 / Executed exporter output', 'This command ran while preparing this walkthrough; stdout is retained in executed-output.txt.')
lines(im, '$ python3 courses/a07-complete-ai-dev-pipeline/evidence.py replay\n\n' + log.getvalue(), (60, 230), width=99, size=23, mono=True)
lines(im, 'The exporter uses ring detection, masking, resizing and fixed filenames.\n32 unmodified states reproduce the production assets exactly.\nFour approved downed replacements are explicitly excluded from the grid replay.', (60, 630), width=100, size=25)
stages.append((im, 9))

im = page('3 / Production assets', '36 final sprites are tracked in Unity. Four downed replacements remain a documented manual override.')
states = evidence.STATES
for row, agent in enumerate(evidence.AGENTS):
    ImageDraw.Draw(im).text((60, 250 + row * 125), agent.upper(), fill='#173c3d', font=font(23))
    for col, state in enumerate(states):
        x, y = 240 + col * 142, 215 + row * 133
        picture(im, evidence.UNITY / f'{agent}_{state}@2x.png', (x, y, x + 100, y + 100))
        if row == 0:
            ImageDraw.Draw(im).text((x, 190), state.replace('_', ' '), fill='#536567', font=font(16))
lines(im, '36 / 36 finals = installed Unity sprites = recorded local build inputs.', (60, 765), size=25)
stages.append((im, 8))

im = page('4 / Automatic engine integration', 'Existing production code; this walkthrough does not run the Unity Editor or rebuild the game.')
code = (ROOT / 'unity/Assets/Suborned/Editor/World/Play/GameBuild.cs').read_text()
hook = next(l.strip() for l in code.splitlines() if 'KitImport.LivePortraits()' in l)
lines(im, 'GameBuild.cs calls:\n' + hook + '\n\nKitImport.cs copies changed @2x PNGs and configures Sprite importers automatically.\n\nHostPortraits.cs selects expressions from simulation activity, attention, firing and health.\n\nHudToolkit.Live.cs loads:\nKit/portraits/live/<agent>_<state>@2x\n\nThe 36 installed assets match build 0.2.232 input hashes.', (60, 220), width=90, size=27)
stages.append((im, 9))

target = OUT / 'TheSuborned-A07-pipeline-walkthrough.mp4'
proc = subprocess.Popen(['ffmpeg', '-y', '-loglevel', 'error', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-s', '1600x900', '-r', '10', '-i', '-', '-an', '-c:v', 'libx264', '-preset', 'fast', '-crf', '21', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(target)], stdin=subprocess.PIPE)
for im, seconds in stages:
    frame = im.tobytes()
    for _ in range(seconds * 10): proc.stdin.write(frame)
# Existing frame-by-frame capture from the playable build, with the clock running.
game = ROOT / 'out/scratch/itch-screenshots/fire/raw/firefight-live'
game_report = json.loads((game.parent / 'shots.json').read_text())
assert game_report['status'] == 'ok' and '0.2.232' in game_report['build']
for _ in range(2):
    for i in range(0, 60, 2):
        im = page('5 / In the playable game', 'Recorded live gameplay from build 0.2.232: roster portraits react during combat.')
        picture(im, game / f'f{i:04d}.png', (200, 185, 1400, 810))
        proc.stdin.write(im.tobytes())
proc.stdin.close()
assert proc.wait() == 0
receipt = {'video': target.name, 'sha256': hashlib.sha256(target.read_bytes()).hexdigest(), 'seconds': sum(t for _, t in stages) + 6, 'type': 'annotated evidence walkthrough of real offline exporter execution and historical generation; not a screen recording of fresh AI generation', 'model_calls_during_video_preparation': 0, 'game_build': '0.2.232', 'public_video_url': None}
(OUT / 'video.json').write_text(json.dumps(receipt, indent=2) + '\n')
print(target)
