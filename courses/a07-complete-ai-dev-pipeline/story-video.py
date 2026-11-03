"""Gameplay-led A07 edit. Real captures, editorial graphics and the game's music.
No inference, fabricated gameplay, generated speech or new engine build.
The prior full execution video remains available as detailed supporting proof.
"""
import hashlib
import json
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
BASE = ROOT / 'out/scratch/a07/people-run'
OUT = ROOT / 'out/scratch/a07/story-video-v2'
OUT.mkdir(parents=True, exist_ok=True)
GAME = BASE / 'gameplay'
FIRE = ROOT / 'out/scratch/a07/action-opening/raw'
FONT = ROOT / 'briefs/011/ui/kit/fonts'
W, H = 3840, 2160
CREAM, TEAL, MUTE, GOLD = '#f7f1e4', '#123b3c', '#abc3bb', '#ecd49b'
scenes = []


def font(n, bold=False):
    return ImageFont.truetype(str(FONT / ('Jost-SemiBold.ttf' if bold else 'Barlow-Regular.ttf')), n)


def words(im, txt, xy, size=100, colour=CREAM, bold=True):
    d, f = ImageDraw.Draw(im), font(size, bold)
    x, y = xy
    for line in txt.split('\n'):
        assert d.textlength(line, font=f) < W - x - 85, line
        d.text((x, y), line, font=f, fill=colour)
        y += int(size * 1.3)


def encode(args, p):
    if p.exists() and p.stat().st_size > 1500:
        return
    subprocess.run(['ffmpeg', '-y', '-v', 'error', *args, '-an', '-r', '20',
                    '-c:v', 'libx264', '-preset', 'medium', '-crf', '20',
                    '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(p)], check=True)


def overlay(title, sub, source, number, action=False):
    im = Image.new('RGBA', (W, H))
    d = ImageDraw.Draw(im)
    top = H - 330 if action else 150
    d.rounded_rectangle((330, top, W - 330, top + (190 if action else 315)), radius=28, fill=TEAL)
    words(im, title, (390, top + 15), 70 if action else 100)
    words(im, sub, (395, top + (112 if action else 185)), 44 if action else 54, GOLD, False)
    d.rectangle((0, H - 110, W, H), fill=TEAL)
    words(im, f'CASE FILE 07 / {source}', (90, H - 91), 43, MUTE, False)
    words(im, f'{number:02d}', (W - 190, H - 96), 50, GOLD)
    p = OUT / f'overlay-{number:02d}.png'; im.save(p)
    return p


def add(p, seconds, name, kind, source):
    actual = float(json.loads(subprocess.check_output(['ffprobe', '-v', 'error',
        '-show_entries', 'format=duration', '-of', 'json', str(p)]))['format']['duration'])
    assert abs(actual - seconds) < .051, (p, actual, seconds)
    scenes.append({'file':p, 'start':round(sum(s['seconds'] for s in scenes), 3),
                   'seconds':actual, 'name':name, 'kind':kind, 'source':str(source)})
    print(f'{len(scenes):02d} / {name}', flush=True)


def game(folder, title, sub, seconds, start=0, release=False):
    n = len(scenes)
    ext = 'png' if release else 'jpg'
    label = 'RELEASE BUILD 0.2.232' if release else 'A07 LOCAL REPLAY / CURRENT-PLAN PEOPLE'
    ov = overlay(title, sub, label + ' / 1080p source, 20 fps', n, action=release)
    p = OUT / f'{n:02d}-game.mp4'
    # Slide in editorial caption; source gameplay remains at its original speed.
    motion = "overlay=0:0:shortest=1:enable='gte(t,2)'" if release else "overlay=x='if(lt(t,0.25),-W*(1-t/0.25),0)':y=0:shortest=1"
    encode(['-framerate','20','-start_number',str(round(start*20)),'-i',str(folder / f'f%04d.{ext}'),
            '-loop','1','-i',str(ov),'-filter_complex',
            f'[0:v]scale={W}:{H}:flags=lanczos[bg];[bg][1:v]{motion}',
            '-t',str(seconds)],p)
    add(p,seconds,title,'real gameplay',folder)


def plate(title, lines, seconds, background=None, eyebrow='THE SUBORNED / THE MAKING OF A CITY', footer='EDITORIAL GRAPHIC / See the linked run records for exact evidence'):
    n = len(scenes)
    if background:
        im = Image.open(background).convert('RGB').resize((W,H), Image.Resampling.LANCZOS).filter(ImageFilter.GaussianBlur(10))
        shade = Image.new('RGB',(W,H),TEAL);im = Image.blend(im,shade,.84)
    else: im=Image.new('RGB',(W,H),TEAL)
    d=ImageDraw.Draw(im)
    d.rectangle((125,110,139,1810),fill=GOLD)
    words(im,eyebrow,(205,110),49,GOLD,False)
    words(im,title,(205,400),150)
    y=810
    for line in lines:
        words(im,line,(205,y),84,CREAM,False);y+=180
    words(im,footer,(205,1970),44,MUTE,False)
    png=OUT/f'{n:02d}-plate.png';im.save(png)
    p=png.with_suffix('.mp4')
    # A restrained camera push gives the card motion without moving the text off screen.
    encode(['-loop','1','-framerate','20','-i',str(png),'-vf',
            f"zoompan=z='1+0.00006*on':x='iw/2-iw/zoom/2':y='ih/2-ih/zoom/2':d=1:s={W}x{H}:fps=20",
            '-t',str(seconds)],p)
    add(p,seconds,title,'editorial graphic',background or 'verified run data')


def photo(title,sub,seconds,source,source_label):
    n=len(scenes)
    ov=overlay(title,sub,source_label,n)
    p=OUT/f'{n:02d}-photo.mp4'
    encode(['-loop','1','-framerate','20','-i',str(source),'-loop','1','-i',str(ov),
            '-filter_complex',f"[0:v]scale={W}:{H},zoompan=z='1+0.00015*on':x='iw/2-iw/zoom/2':y='ih/2-ih/zoom/2':d=1:s={W}x{H}:fps=20[bg];[bg][1:v]overlay=0:0:shortest=1",
            '-t',str(seconds)],p)
    add(p,seconds,title,'actual player still / editorial camera push',source)


def terminal(start,seconds,title):
    n=len(scenes)
    p=OUT/f'{n:02d}-terminal.mp4'
    encode(['-ss',str(start),'-i',str(BASE/'people-terminal-4K.mp4'),'-t',str(seconds)],p)
    add(p,seconds,title,'actual recorded CLI / original speed',BASE/'people-run.cast')


# Let each fight read at its original speed; captions wait two seconds.
game(FIRE/'officers-wide','Officers close in.','There is a loud way through the city.',6,release=True)
game(FIRE/'rivals-follow','Rivals want the same prize.','Your crew has company.',6,release=True)
game(GAME/'guard-leaves-post','Or you could move one person.','A guard. A doorway. One useful relationship.',4)
game(GAME/'cafe-routines','Meet Inez. She needs help.','Watch. Listen. Learn what matters to her.',6)
photo('Arthur is her lover.','Her need and her relationship become your opening.',6,GAME/'social-intel.png','ACTUAL INTEL FRAME / A07 replay variant / 1080p source')
game(GAME/'guard-leaves-post','She calls. He leaves.','A relationship changes the route.',4,start=2)
plate('THE SUBORNED',['Make the city do the work.'],3,eyebrow='SKY RUMSEY / ASSIGNMENT 7',footer='Real game captures / No staged relationship overrides')

# Whole production workflow before following the worked example.
plate('How do you build that city?',[
    'DESIGN + BRIEFS  /  AGENT CODE + CONTENT',
    'CHECKS + HUMAN REVIEW  /  UNITY',
    'PLAYER TESTS  /  SIGNED BUILD + RELEASE',
    'World. Art. People. Dialogue. Simulation. Interface.'
],7,GAME/'social-intel.png',footer='Whole workflow overview / The following execution is one worked example')
plate('First, give the cast some rules.',[
    'AI proposes the cast from retrieved design context.',
    'Code expands the people and their routines.',
    'A failed relationship/routine check goes back for revision.'
],6,footer='Historical r016 / First proposal rejected; corrected cast checked and reviewed')
terminal(5.3,8.2,'130 people. 222 relationship links. Checked.')
terminal(13.3,3.5,'Approved data, staged automatically into Unity')
game(GAME/'street-level-people','Now they have somewhere to be.','People and routines running in the real Unity player.',5)
plate('About 69 cents of cast generation.',[
    '$0.6905 recorded usage / Three historical AI calls',
    'Largest call: the final review / about $0.28',
    'Subscription usage value; not an additional API payment.',
    'This is one measured run, not the cost of the whole game.'
],8,footer='r016 costs include the rejected proposal / Exact receipts are in the evidence pack')
plate('Stop paying to review known faults.',[
    'BEFORE: even failed checks went to the AI critic.',
    'AFTER: fix the failure first. Then ask for a review.',
    'Old failed candidates: 2 reviews / $0.4135',
    'People run, failed first round: 0 review calls / $0'
],7,footer='Shared workflow change / Different candidates, not a like-for-like saving percentage')
plate('The next thing to fix?',['A release should remember every input.',
    'Pin the design, plan, rules, assets and tool versions.',
    'Then reject stale outputs before they can ship.'
],6,GAME/'social-intel.png',footer='The replay exposed changed background people after the city remap; four mission profiles match')
plate('Make the city do the work.',[
    'PLAY  codecoded.itch.io/the-suborned',
    'SOURCE + AUDIT  github.com/codecoded/suborned',
    'Branch: assignment-07 / Detailed execution video linked below'
],7,eyebrow='THE SUBORNED / SKY RUMSEY / ASSIGNMENT 7',footer='Archived AI output / Existing Unity binary / Local replay variant / Two-minute first-play check still open')

manifest=OUT/'clips.txt';manifest.write_text(''.join(f"file '{s['file'].name}'\n" for s in scenes))
silent=OUT/'story-silent.mp4'
subprocess.run(['ffmpeg','-y','-v','error','-f','concat','-safe','0','-i',str(manifest),'-c','copy',str(silent)],check=True)
# Use the original approved sketch, not the experimental stem-separated layers.
music=ROOT/'briefs/087/music/lounge_spy.mp3'
duration=float(json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','json',str(silent)]))['format']['duration'])
assert abs(duration - sum(s['seconds'] for s in scenes)) < .1
target=OUT/'TheSuborned-A07-Make-the-City-Work-v2-4K.mp4'
subprocess.run(['ffmpeg','-y','-v','error','-i',str(silent),'-i',str(music),'-i',str(music),
    '-filter_complex',f'[1:a][2:a]acrossfade=d=2:c1=tri:c2=tri,atrim=0:{duration},loudnorm=I=-20:TP=-2:LRA=8,afade=t=in:d=0.4,afade=t=out:st={duration-2}:d=2[music]',
    '-map','0:v','-map','[music]','-c:v','copy','-c:a','aac','-b:a','192k','-t',str(duration),'-movflags','+faststart',str(target)],check=True)
upload=target.with_name(target.stem + '-upload.mp4')
subprocess.run(['ffmpeg','-y','-v','error','-i',str(target),'-c:v','libx264',
    '-preset','medium','-crf','23','-c:a','copy','-movflags','+faststart',str(upload)],check=True)
assert upload.stat().st_size < 50 * 1024 * 1024, 'Browser upload bridge limit'
receipt={
    'video':str(target.relative_to(ROOT)),'seconds':duration,'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),
    'bytes':target.stat().st_size,'resolution':[W,H],'fps':20,'model_calls':0,
    'edit':'Gameplay-led story, editorial captions/motion graphics, actual CLI excerpts at original speed, brief source gameplay excerpts and stills',
    'music':{'file':str(music.relative_to(ROOT)),'sha256':hashlib.sha256(music.read_bytes()).hexdigest(),
             'source':'Game lounge-spy sketch, ACE-Step; existing provenance in briefs/087/licences/generated.md. No experimental Demucs layers used.',
             'mix':'Two copies joined with a 2-second crossfade; -20 LUFS target, -2 dBTP limit; fades at start/end. Editorial soundtrack, not recorded gameplay audio.'},
    'limits':['AI generation is archived; no new model calls','Current-plan replay variant in the retained 0.2.232 binary; four mission profiles match production; full background differs',
              'Opening officers/rivals firefights are separate release-build captures, labelled on screen after two seconds','Gameplay is 1080p/20fps source upscaled to 4K','No new Unity compilation','Public two-minute first-play test remains open'],
    'detailed_execution_url':'https://youtu.be/725LKDmVNQ8',
    'upload':{'file':str(upload.relative_to(ROOT)), 'bytes':upload.stat().st_size,
              'sha256':hashlib.sha256(upload.read_bytes()).hexdigest(),
              'encoding':'H.264 CRF 23, medium; 3840x2160 at 20 fps; original mixed AAC audio copied'},
    'scenes':[{**s,'file':str(s['file'].relative_to(ROOT)),
               'source':s['source'].removeprefix(str(ROOT) + '/')} for s in scenes],
    'youtube_url':None,
}
(OUT/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
print(target,flush=True)
