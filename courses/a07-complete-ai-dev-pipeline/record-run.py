"""Record real CLI output as a timed asciicast and live 4K terminal video.

The child actually executes while frames are encoded. This is a terminal-output
recording, not a GUI screen recording or a simulated typing animation. Raw output
and wall-clock timestamps remain available for an independent replay.
"""
import fcntl
import json
import os
import pty
import re
import select
import struct
import subprocess
import threading
import time
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'out/scratch/a07/people-run'
OUT.mkdir(parents=True, exist_ok=True)
W, H, FPS = 3840, 2160, 10
BG, INK, MUTE, GREEN = '#102e30', '#f7f4ec', '#acc5bf', '#8cd6c3'
FONTS = ROOT / 'briefs/011/ui/kit/fonts'
mono = ImageFont.truetype('/System/Library/Fonts/Menlo.ttc', 57)
heading = ImageFont.truetype(str(FONTS / 'Jost-SemiBold.ttf'), 90)
small = ImageFont.truetype(str(FONTS / 'Barlow-Regular.ttf'), 45)
ansi = re.compile(r'\x1b\[[0-?]*[ -/]*[@-~]')
state = {'text': '', 'done': False, 'stage': 'Starting the production proof', 'error': None}
lock = threading.Lock()
video = OUT / 'people-terminal-4K.mp4'
encoder = subprocess.Popen(['ffmpeg', '-y', '-v', 'error', '-f', 'rawvideo', '-pix_fmt', 'rgb24',
                            '-s', f'{W}x{H}', '-r', str(FPS), '-i', '-', '-an', '-c:v', 'libx264',
                            '-preset', 'veryfast', '-crf', '16', '-pix_fmt', 'yuv420p',
                            '-movflags', '+faststart', str(video)], stdin=subprocess.PIPE)
started = time.monotonic()


def render():
    previous, frame = None, None
    frames = 0
    try:
        while True:
            with lock:
                contents, done, title = state['text'], state['done'], state['stage']
            elapsed = time.monotonic() - started
            key = (contents, title, int(elapsed), done)
            if key != previous:
                im = Image.new('RGB', (W, H), BG)
                d = ImageDraw.Draw(im)
                d.text((110, 70), 'THE SUBORNED / ASSIGNMENT 7', font=small, fill=GREEN)
                d.text((110, 160), title, font=heading, fill=INK)
                d.rounded_rectangle((90, 350, W - 90, H - 180), radius=24, fill='#081d20')
                rows = []
                for line in ansi.sub('', contents).replace('\r', '').split('\n'):
                    rows.extend([line[i:i + 100] for i in range(0, len(line), 100)] or [''])
                for i, line in enumerate(rows[-21:]):
                    colour = GREEN if line.startswith(('PASS', '$', '[')) else INK
                    d.text((145, 390 + i * 69), line, font=mono, fill=colour)
                d.text((110, H - 130), 'LIVE CLI OUTPUT CAPTURE / Archived AI responses / No new model calls', font=small, fill=MUTE)
                d.text((W - 530, 77), f'{int(elapsed)//60:02d}:{int(elapsed)%60:02d} elapsed', font=small, fill=MUTE)
                frame, previous = im.tobytes(), key
            encoder.stdin.write(frame)
            frames += 1
            if done:
                break
            due = started + frames / FPS - time.monotonic()
            if due > 0:
                time.sleep(due)
    except Exception as error:
        state['error'] = str(error)
    finally:
        encoder.stdin.close()


thread = threading.Thread(target=render)
thread.start()
master, slave = pty.openpty()
fcntl.ioctl(slave, 0x80087467, struct.pack('HHHH', 30, 100, 0, 0))  # macOS TIOCSWINSZ
command = ['bun', 'courses/a07-complete-ai-dev-pipeline/people-run.ts', 'run']
child = subprocess.Popen(command, cwd=ROOT, stdin=subprocess.DEVNULL, stdout=slave, stderr=slave,
                         env={**os.environ, 'NO_COLOR': '1', 'TERM': 'dumb'})
os.close(slave)
cast = OUT / 'people-run.cast'
with cast.open('w') as f:
    f.write(json.dumps({'version': 2, 'width': 100, 'height': 30, 'timestamp': int(time.time()),
                        'title': 'The Suborned A07: real offline people pipeline execution',
                        'env': {'SHELL': '/bin/zsh', 'TERM': 'dumb'}}) + '\n')
    while True:
        ready, _, _ = select.select([master], [], [], .2)
        if ready:
            try:
                data = os.read(master, 65536)
            except OSError:
                break
            if not data:
                break
            output = data.decode('utf8', errors='replace')
            f.write(json.dumps([round(time.monotonic() - started, 6), 'o', output]) + '\n')
            f.flush()
            print(output, end='', flush=True)
            with lock:
                state['text'] += output
                stages = re.findall(r'\[[1-6]/6\] ([^\r\n]+)', state['text'])
                if stages:
                    state['stage'] = stages[-1].title()
        if child.poll() is not None and not ready:
            break
os.close(master)
exit_code = child.wait()
with lock:
    state['done'] = True
thread.join()
assert encoder.wait() == 0 and not state['error'], state['error']
(OUT / 'terminal-recording.json').write_text(json.dumps({
    'command': command, 'exit_code': exit_code, 'wall_seconds': time.monotonic() - started,
    'video': str(video.relative_to(ROOT)), 'cast': str(cast.relative_to(ROOT)),
    'kind': 'Live terminal-output recording rendered during command execution, not a GUI desktop screen recording',
    'resolution': [W, H], 'fps': FPS,
}, indent=2) + '\n')
raise SystemExit(exit_code)
