# Glyph, brief 014 A: the moments sheet, before and after, with the checker's numbers.
from g14 import *
B=json.load(open('before.json')); A=json.load(open('after.json'))
names={'01':'Tech: loop targets (0:13)','02':'Scout watches the guard (0:50)','03':'Takedown armed, price chip (2:01)','04':'Right click the guard (2:01)','05':'Takedown in progress (2:04)','06':'Body, Enforcer inside (2:14)','07':'Carry to the bin (2:12)','08':'Body hidden, response sent (2:34)','09':'End card (3:39)','10':'Held door (2:29)','11':'Enter refused (2:29)','12':'NEW · the same card with Alt held'}
notes={
'01':'The NEXT STEP box and the objective bar go; one six-word line at the bottom says what to do, the noun in its marker colour with a shape. The four-card loop panel is one narrow sheet: a row per target, icons and numbers. Strips become a glyph, a bar and seconds.',
'02':'The overheard scene is speech at the speaker (the guard\'s bubble), not a card. "?" on the people the Scout has not watched. The Watch is a bar on the Scout\'s chip. The feed is empty: no acks of what you just watched.',
'03':'The chip is the price line with the eye shut (nobody sees it) and a clock pip over the guard (his learned check-in). The guard\'s learned cone is on the ground. The ways are two round keys and one name.',
'04':'Three cards of about 20 facts become three rows: Knock out, Kill, Fire. Hovering a way draws its preview in the street: the lock the body would put on the door, the ring on the guard. The sentences are Alt (12).',
'05':'"Takedown begins. The guard didn\'t see it coming." is gone: the ring fills round the Enforcer and his chip counts 4 s. His own words are a bubble over him.',
'06':'The wake ring on the body empties to the wake; hover or Alt gives the clock time. The feed is empty. "Entrance camera is live" is the camera\'s red cone coming back.',
'07':'The carry strip, the carry chip and two stash plates become: a carry disc on the Enforcer, a chip with time and the eye ("?" until watched), the bin and the service door as round plates with pips, and the route dotted through the guard\'s cone.',
'08':'Three feed cards and the response pill (words) become two story beats and a pill of unit icons and numbers. An edge arrow says where the units come from. The Scout is off screen, so the beat speaks for her.',
'09':'One line and two buttons. The result sentence, the tally and the security notes are the debrief (as Fable has it).',
'10':'The lock appears on the door when it happens, with its ring and tape; the body\'s dashed ring says it is found; two unit badges stand there. The three cards become two beats.',
'11':'The refusal is an icon and one word at the cursor ("Held"), and the lock shakes. No sentence.',
'12':'Hold Alt: the sentences return as icon-and-number fragments under each way, name tags appear, every learned mark shows. Nothing else changes, and it goes when Alt is let go.'}
W=1000; H=int(1142*W/1726); PAD=40; COL=2*W+3*PAD
rows=[k for k in ['01','02','03','04','05','06','07','08','09','10','11','12']]
RH=H+280
sheet=Image.new('RGBA',(COL,190+RH*len(rows)+60),C['ink'])
text(sheet,PAD,56,'Brief 014 A · The same moments, before and after','d',24,'Bold','cream','lm')
for i,l in enumerate(wrap('Left: Latch\'s 013 play shots, old HUD in place. Right: the redesign on the same world, with the old HUD and tags patched out (my patching smears the street in places; Latch\'s clean captures replace it). The numbers are tools/hud-budget run on each image. Before: bounding rectangles of the old HUD (coarse, so a little high). After: the HUD layer\'s own alpha (exact).','b',12.5,'Medium',COL-2*PAD)): text(sheet,PAD,100+i*32,l,'b',12.5,'Medium','text-dim','lm')
y=190
for k in rows:
    bf=[f for f in B if os.path.basename(f).startswith(k)]; af=[f for f in A if os.path.basename(f).startswith(k)]
    d=ImageDraw.Draw(sheet)
    text(sheet,PAD,y+24,f'{k.lstrip("0") if k!="12" else "+"}  {names[k]}','d',16,'Bold','turquoise-light','lm')
    for side,(path,fl,rep) in enumerate((('briefs/013/play/'+(bf[0].split('/')[-1] if bf else ''),bf,B[bf[0]] if bf else None),('out/glyph014/after/%s.png'%k,af,A[af[0]] if af else None))):
        x=PAD+side*(W+PAD)
        if side==0 and not bf: 
            d.rectangle((x,y+92,x+W,y+92+H),outline=(130,160,156,255),width=2); text(sheet,x+W/2,y+92+H/2,'(new moment: no old equivalent)','b',14,'Medium','text-dim','mm'); continue
        im=Image.open(R+'/'+path).convert('RGB').resize((W,H),Image.LANCZOS); sheet.paste(im,(x,y+92))
        label='BEFORE' if side==0 else 'AFTER'
        ok=rep['pass']; col='pale-seafoam' if ok else '#ff8a7a'
        tag=f"{label}   HUD {rep['coveragePct']:.0f}% of the screen · running text {len(rep['sentences'])} line{'s' if len(rep['sentences'])!=1 else ''}, {len(rep['blocks'])} block{'s' if len(rep['blocks'])!=1 else ''} · {'WITHIN BUDGET' if ok else 'OVER BUDGET'}"
        text(sheet,x,y+82,tag,'d',12.5,'Bold',col,'lb')
    text(sheet,PAD,y+60+H+14,'','b',12)
    yy=y+92+H+22
    for i,l in enumerate(wrap(notes[k],'b',13,'Medium',COL-2*PAD)): text(sheet,PAD,yy+i*34,l,'b',13,'Medium','cream')
    y+=RH
sheet=sheet.crop((0,0,COL,y)); sheet.convert('RGB').save(R+'/briefs/014/glyph/less-moments.jpg'); print(sheet.size)
