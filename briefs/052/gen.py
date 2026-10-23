"""Cell teaser (brief 052, Forma, 2026-10-23; paid calls moved onto the shared helper by Splice, brief 053).
  python3 -I briefs/052/gen.py img <FRAME> --pass N [--budget USD] [--take N] [--seed S] [--add text] [--note text]
  python3 -I briefs/052/gen.py vid <SHOT> <keyframe.png> --pass N [--budget USD] --model hailuo|seedance|seedance25|h3|h3max [--take N] [--dur 6] [--refs a,b] [--note text]
  python3 -I briefs/052/gen.py music --pass N [--budget USD] --prompt text [--take N] [--seed S]
Images: Seedream 5.0 Pro via Runware. Video: Hailuo 2.3 (minimax:4@1), Seedance (bytedance:seedance@2.0 / 2.5), H3.
Every call goes through tools/paid/paid.py: media land in paid/052-teaser/ (keyframes/, shots/, audio/; briefs/052/keyframes etc. are symlinks to it), each call is one line
of paid/052-teaser/ledger.jsonl, and `bun run paid` shows the spend. A pass needs an explicit budget: the first call of a new pass takes --budget USD (it is then recorded in
paid/052-teaser/passes.json; raising it needs --raise and Sky's OK); the helper prints the estimate and stops at the budget. A repeated take name keeps the older file (the new one gets __r2).
History before the helper (passes 1-5, $48.19) is in paid/052-teaser/ledger.jsonl; briefs/052/PROVENANCE.json is the frozen old record."""
import base64, io, os, sys
from pathlib import Path
from PIL import Image
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[1] / 'tools/paid'))
import paid  # noqa
JOB = '052-teaser'
IMG_MODEL = 'bytedance:seedream@5.0-pro'
VMODELS = {'hailuo': 'minimax:4@1', 'seedance': 'bytedance:seedance@2.0', 'seedance25': 'bytedance:seedance@2.5', 'h3': 'minimax:h3@0', 'h3max': 'minimax:h3@max'}

# ---- cast, with props (no era date on people: style contract people rule) ----
P = {
 'scout': ("the Scout (lean young person, black undercut, earpiece, ivory hooded jacket, black harness straps, black shorts, white sneakers) wearing her backpack: a compact technical pack, "
           "ivory shell with black webbing, chrome buckles and a rolled top, riding high on her back, the harness straps over both shoulders, sized to her slim frame"),
 'fixer': ("the Fixer (woman, black ponytail, ivory and black pinstripe halter dress, long ivory gloves, ivory boots) carrying her suitcase: a sleek hard case, a streamlined rounded shell in matte black and ivory, "
           "chrome corner guards, one slim chrome handle and a ribbed side panel, small enough to carry in one gloved hand"),
 'tech': ("the Tech (muscular man, tied dreadlocks, short beard, black wrap top with ivory sash and cyan piping, wide black trousers, chrome bracers) wearing neon glasses: slim wraparound frames made of thin glowing cyan neon tube across his eyes, "
          "no goggles on his forehead; the cyan piping along his wrap top, sash, trousers and bracers is lit up and glowing like thin cyan light tubes, casting faint cyan light onto the fabric and his skin"),
 'tech_pipe': ("the Tech (muscular man, tied dreadlocks, short beard, black wrap top with ivory sash and cyan piping, wide black trousers, chrome bracers) wearing slim wraparound neon glasses of thin glowing cyan tube, "
          "and holding a slim curved smoking pipe with a chrome bowl at his lips, a thin curl of pale smoke rising, the cyan piping on his clothes glowing softly"),
 'enforcer': ("the Enforcer (big older man, short grey hair, full grey beard, tinted sunglasses, matte black and ivory panelled coat, chrome collar, white boots) with his gun held low in his right hand: "
              "a retro-future sidearm, a rounded streamlined raygun of polished chrome and cream enamel with a finned barrel, a ribbed coil and a black grip, atomic-age styling like a precision tool, not a modern pistol"),
}
LINE = {'scout': 'second', 'fixer': 'second', 'tech': 'third', 'enforcer': 'fourth'}
KEEP = "Keep the face, hair, skin tone, outfit and colours exactly as the person in the reference images (reference image 1 is the full cell, reference image 2 is this person). Sharp matte black, ivory and chrome."
CITY = ("Streamline Moderne and Googie city: cream limestone, seafoam-teal enamel bands, chrome spheres, banners, cypress and palms, pale paving with seafoam inlays, coral sky rail. "
        "An alternate world with atomic power centuries ahead, styled like the 1950s: trams and robots, no screens. Any citizens are bareheaded in sleek tailored space-age couture (cropped jackets, shift dresses with chrome belts) in seafoam, coral, navy and white; no hats, no suits. ")
GRADE = ("Cinematic key art still for a premium video game trailer: anamorphic lens, shallow depth of field with soft oval bokeh, warm low sun backlight at shift-change light, haze and soft bloom, "
         "one consistent teal and peach colour grade, fine film grain, photographic material realism with gouache softness. "
         "No text, no lettering, no logos, no watermark, no interface. No gold surfaces, no glass curtain walls, no grime, no letterbox bars.")
CAST4 = ("The four people in reference image 1 are the player's cell, left to right: " + "; ".join(P[k] for k in ('scout', 'fixer', 'tech', 'enforcer')) + ". Keep their faces, hair, skin tones and outfits exactly as in reference image 1; the props are added to the outfits. ")

FRAMES = {
 # evidence: prop sheet and the pipe alternative
 'PROPS': (['cell_lineup.png', 'scout.png', 'fixer.png', 'tech.png', 'enforcer.png'],
   "Character prop sheet, concept art: the same four people as reference image 1, in the same order left to right and the same standing front poses, full length, on a plain pale warm-grey studio backdrop, even soft light, each now with their prop clearly visible and drawn with care. "
   + CAST4 + "Each person in a relaxed standing pose with the prop held or worn so it reads at a glance. Premium concept art, photographic material realism. No text, no labels, no logos, no watermark."),
 'PIPE': (['cell_lineup.png', 'tech.png'],
   "Character concept, three-quarter portrait of one person from the waist up on a plain pale warm-grey backdrop: " + P['tech_pipe'] + ". " + KEEP + " Premium concept art, photographic material realism. No text, no logos, no watermark."),
 'NEON': (['cell_lineup.png', 'tech.png'],
   "Character concept, three-quarter portrait of one person from the waist up on a plain pale warm-grey backdrop: " + P['tech'] + ". " + KEEP + " Premium concept art, photographic material realism. No text, no logos, no watermark."),
 # shots
 'S0': (['city_hero.jpg', 'records_office.jpg'],
   "Extreme close-up, shallow depth of field: a 1950s-styled closed-circuit camera in a polished chrome hood on a slim chrome post, its round glass lens catching the low sun, filling the left of the frame, "
   "turned slightly toward the viewer. Behind it, far out of focus, the cream-and-seafoam Civic Records Office with its chrome globe and teal banners glows in haze. Low golden backlight, long soft shadows, cream haze, tense hush under the pastel calm. " + CITY + GRADE),
 'S1': (['city_hero.jpg', 'records_office.jpg', 'mission_block.jpg', 'exposition_palace.jpg'],
   "Wide cinematic establishing shot of the Harbour District at shift-change light from a raised terrace, camera looking down a broad civic square toward the Civic Records Office at centre: cream limestone with seafoam enamel bands, tall fluted entrance, chrome globe, teal banners. "
   "A coral sky rail tram crosses on slim arches, fluted cream towers and saucer-topped spires recede into peach and lilac haze, SpaceHQ's chrome atom crown tall on the horizon, lagoon-blue harbour at the edge. Long soft shadows across pale paving, a few tiny distant citizens. "
   "Four tiny dark-and-ivory figures walk in the lower foreground toward the Office. " + CITY + GRADE),
 'S2': (['cell_lineup.png', 'scout.png'],
   "Character-introduction hero shot, medium close-up, camera at chest height: " + P['scout'] + ", seen in three-quarter view from behind-left as she walks and glances back over her shoulder toward the camera, face clear, the pack and harness readable. "
   "Behind her, out of focus, a cream limestone civic square with a chrome sphere and seafoam banners, golden backlight rimming her hair and the pack. " + KEEP + " " + CITY + GRADE),
 'S3': (['cell_lineup.png', 'fixer.png'],
   "Character-introduction hero shot, medium shot, camera at hip height looking slightly up: " + P['fixer'] + ", standing on pale paving in a cream civic square, the suitcase in her right hand at her side, turned three-quarter to camera, calm and watchful, face clear. "
   "Behind her, out of focus, a chrome bollard, a seafoam-striped café parasol and the Records Office in haze, golden backlight. " + KEEP + " " + CITY + GRADE),
 'S4': (['cell_lineup.png', 'tech.png'],
   "Character-introduction hero shot, close medium shot, camera at chest height: " + P['tech'] + ", facing three-quarter to camera, calm and focused, face clear. "
   "The cyan neon of his glasses and piping is the brightest light on him against the warm gold backlight; behind him, out of focus, the shaded edge of a cream stone arcade with seafoam bands. " + KEEP + " " + CITY + GRADE),
 'S5': (['cell_lineup.png', 'enforcer.png'],
   "Character-introduction hero shot, low-angle medium shot, camera near the ground looking up: " + P['enforcer'] + ", standing square to camera in a cream civic square, calm and heavy, face clear behind the tinted sunglasses, the chrome raygun held down at his side catching the sun. "
   "Behind him, out of focus, the Records Office and a pale sky with golden backlight rimming his grey hair and beard. " + KEEP + " " + CITY + GRADE),
 'S6': (['cell_lineup.png', 'enforcer.png', 'records_office.jpg', 'mission_block.jpg'],
   "Tension shot, wide over-the-shoulder: the Enforcer, a big grey-bearded man in a matte black and ivory panelled coat with a chrome collar and white boots, in sharp foreground three-quarter from behind, his chrome-and-cream retro raygun hanging low in his right hand, looking across a civic square toward the Civic Records Office. "
   "At the Office door stands a guard in a pale slate-blue uniform; a long black-and-ivory streamlined car draws up at the kerb. Long shadows, thin haze, tense hush under the pastel calm, low golden light, shallow depth of field with the Office sharp. " + KEEP + " " + CITY + GRADE),
}

FRAMES['S1b'] = (['cell_lineup.png', 'city_hero.jpg', 'records_office.jpg', 'mission_block.jpg'],
   "Wide cinematic establishing shot of the Harbour District at shift-change light from a raised terrace, camera looking down a broad civic square toward the Civic Records Office at centre: cream limestone with seafoam enamel bands, tall fluted entrance, chrome globe, teal banners. "
   "A coral sky rail tram crosses on slim arches, fluted cream towers and saucer-topped spires recede into peach and lilac haze, SpaceHQ's chrome atom crown tall on the horizon, lagoon-blue harbour at the edge. Long soft shadows across pale paving. "
   "In the lower foreground, seen from behind, the four people of reference image 1 walk toward the Office side by side, each about a sixth of the frame tall: a big grey-haired man in a black and ivory panelled coat at the left of centre, a lean young person in an ivory hooded jacket with a backpack, a woman in a black and ivory pinstripe dress carrying a hard case, a man with tied dreadlocks in a black wrap top with glowing cyan piping. They are the only people dressed in black and ivory; every other citizen is tiny and far away, bareheaded in couture. " + CITY + GRADE)


# ---- pass 2: Sky's props sheet (refs/sky-props-reference.png; crops sky_*.png), costumes stay on the turnarounds ----
P2 = {
 'scout': ("the Scout (lean young person, black undercut, earpiece, ivory hooded jacket, black harness straps, black shorts, white sneakers) with her backpack: a small matte black technical backpack with black harness straps and chrome buckles, slung on one shoulder (her left), boxy and compact, clearly black against her ivory jacket"),
 'fixer': ("the Fixer (woman, black ponytail, SHORT knee-length ivory and black pinstripe halter dress, long ivory gloves, ivory boots; keep the short turnaround dress, not a long dress) carrying her attache case: a flat, slim rectangular case of brushed silver metal, briefcase-sized, held low in her right hand by a slim handle at her thigh, at the same distance from the camera as her body"),
 'tech': ("the Tech (muscular man, tied dreadlocks, short beard, black wrap top with ivory sash and cyan piping, wide black trousers, chrome bracers) with his visor: an angular chrome visor with a dark lens pushed up on top of his head above the forehead, resting up in his dreadlocks, not over his eyes; the cyan piping along his wrap top, sash, trousers and bracers is lit and glows cyan"),
 'tech_worn': ("the Tech (muscular man, tied dreadlocks, short beard, black wrap top with ivory sash and cyan piping, wide black trousers, chrome bracers) wearing his angular chrome visor pulled down over his eyes, its lens a lit neon-cyan band; the cyan piping along his wrap top, sash, trousers and bracers is lit and glows cyan, lighting his skin"),
 'enforcer': ("the Enforcer (big older man, short grey hair, full grey beard, tinted sunglasses, matte black and ivory panelled coat, chrome collar, white boots) with his rifle: a long brushed-chrome rifle with round disc fittings and a long slim barrel, as long as his leg, carried low and vertical in his right gloved hand along his right leg with the muzzle pointing at the ground"),
}
KEEP2 = ("Keep the face, hair, skin tone and outfit exactly as the person in reference image 1 (the full cell, the authority for costumes) and reference image 2 (this person). Reference image 3 shows only their prop: take the prop from it, not the clothes. Sharp matte black, ivory and chrome.")
def _hero(scene, who, extra=''):
    return scene.replace('@', P2[who]) + ' ' + KEEP2 + ' ' + CITY + GRADE + ' ' + extra
FRAMES['V2S2'] = (['cell_lineup.png', 'scout.png', 'sky_pack.png'], _hero(
   "Character-introduction hero shot, medium close-up, camera at chest height: @, seen in three-quarter view from behind-left as she walks and glances back over her shoulder toward the camera, face clear, the black pack readable against her ivory jacket. "
   "Behind her, out of focus, a cream limestone civic square with a chrome sphere and seafoam banners, golden backlight rimming her hair. The background square is nearly empty with only a few tiny distant figures, no robots.", 'scout'))
FRAMES['V2S3'] = (['cell_lineup.png', 'fixer.png', 'sky_case.png'], _hero(
   "Character-introduction hero shot, medium shot, camera at waist height, a little further back so her whole body down to the knees shows: @, standing on pale paving in a cream civic square, turned three-quarter to camera, calm and watchful, face clear, the case hanging at her thigh. "
   "Behind her, out of focus, a chrome bollard, a seafoam-striped cafe parasol and the Records Office in haze, golden backlight. The background square is nearly empty with only a few tiny distant figures, no robots.", 'fixer'))
FRAMES['V2S4'] = (['cell_lineup.png', 'tech.png', 'sky_visor.png'], _hero(
   "Character-introduction hero shot, close medium shot, camera at chest height: @, facing three-quarter to camera, calm and focused, face clear and his eyes visible. "
   "Cool shade falls on him so the cyan glow of the piping is vivid against the warm gold backlight; behind him, out of focus, the shaded edge of a cream stone arcade with seafoam bands. The background is nearly empty bright haze with at most two tiny distant figures.", 'tech'))
FRAMES['V2S4b'] = (['cell_lineup.png', 'tech.png', 'sky_visor.png'], _hero(
   "Character-introduction hero shot, close medium shot, camera at chest height: @, facing three-quarter to camera, calm and focused. "
   "Cool shade falls on him so the cyan glow of the visor and piping is vivid against the warm gold backlight; behind him, out of focus, the shaded edge of a cream stone arcade with seafoam bands. The background is nearly empty bright haze with at most two tiny distant figures.", 'tech_worn'))
FRAMES['V2S5'] = (['cell_lineup.png', 'enforcer.png', 'sky_rifle.png'], _hero(
   "Character-introduction hero shot, low-angle medium shot, camera near the ground looking up: @, standing square to camera in a cream civic square, calm and heavy, face clear behind the tinted sunglasses, the rifle catching the sun. "
   "Behind him, out of focus, the Records Office and a pale sky with golden backlight rimming his grey hair and beard. The square behind him is nearly empty: at most two tiny distant figures, bareheaded, no hats, no robots.", 'enforcer'))
FRAMES['V2S6'] = (['cell_lineup.png', 'enforcer.png', 'sky_rifle.png', 'records_office.jpg'], _hero(
   "Tension shot, wide over-the-shoulder: @, in sharp foreground three-quarter from behind, looking across a civic square toward the Civic Records Office. "
   "At the Office door stands a guard in a pale slate-blue uniform; a long black-and-ivory streamlined car draws up at the kerb. Long shadows, thin haze, tense hush under the pastel calm, low golden light, shallow depth of field with the Office sharp. Few citizens, all tiny and bareheaded, no hats.", 'enforcer'))
FRAMES['V2S1'] = (['cell_lineup.png', 'sky-props-reference.png', 'city_hero.jpg', 'records_office.jpg'],
   "Wide cinematic establishing shot of the Harbour District at shift-change light from a raised terrace, camera looking down a broad civic square toward the Civic Records Office at centre: cream limestone with seafoam enamel bands, tall fluted entrance, chrome globe, teal banners. "
   "A coral sky rail tram crosses on slim arches, fluted cream towers and saucer-topped spires recede into peach and lilac haze, SpaceHQ's chrome atom crown tall on the horizon, lagoon-blue harbour at the edge. Long soft shadows across pale paving. "
   "In the lower foreground, seen from behind, the four people of reference image 1 walk toward the Office side by side, each about a sixth of the frame tall, with their props from reference image 2 (take only the props, keep the costumes of reference image 1): "
   "the big grey-haired Enforcer in a black and ivory panelled coat carrying a long brushed-chrome rifle low in his right hand, muzzle down; the lean Scout in an ivory hooded jacket with a small matte BLACK backpack slung on one shoulder; "
   "the Fixer in a SHORT black and ivory pinstripe halter dress with a flat brushed-silver attache case at her thigh; the Tech with tied dreadlocks, a chrome visor pushed up on his head and cyan-lit piping. "
   "They are the only people dressed in black and ivory; every other citizen is tiny and far away, bareheaded in couture. " + CITY + GRADE)
FRAMES['V2S1c'] = (['cell_lineup.png', 'props2.png', 'city_hero.jpg', 'records_office.jpg'],
   FRAMES['V2S1'][1].replace('reference image 2 (take only the props, keep the costumes of reference image 1)', 'reference image 2 (the same four with their props; take the props, costumes as in reference image 1)').replace('walk toward the Office side by side', 'walk toward the Office side by side, EXACTLY FOUR people in the group and only one woman').replace('the four people of reference image 1', 'the four people (one big man, one lean young person, one woman, one muscular man) of reference image 1'))
FRAMES['PROPS2'] = (['cell_lineup.png', 'scout.png', 'fixer.png', 'tech.png', 'enforcer.png', 'sky-props-reference.png'],
   "Character prop sheet, concept art: the same four people as reference image 1, in the same order left to right, full length, standing in relaxed three-quarter poses like the pose sheet in reference image 6, on a plain pale warm-grey studio backdrop, even soft light. Costumes exactly as the turnaround in reference image 1 (the Fixer in her short halter dress). "
   "Their props as in reference image 6: " + "; ".join(P2[k] for k in ('scout', 'fixer', 'tech', 'enforcer')) + ". Each prop clearly visible and drawn with care, true to its size. Premium concept art, photographic material realism. No text, no labels, no logos, no watermark.")

# ---- v4 (30 s): flyby plates and the 'turning ordinary people' middle ----
NPC = "Any citizens are bareheaded, in sleek tailored space-age couture (cropped jackets, shift dresses with chrome belts) in seafoam, coral, navy and white; no hats, no suits, no period dress. "
AER = ("Cinematic aerial establishing shot of the Harbour District at shift-change light, as an anamorphic key art still: lagoon-blue harbour with a white atomic liner and sails, terraced cream-and-seafoam enamel-banded blocks, fluted towers, saucer-topped spires, "
       "a coral sky rail with a tram on slim arches, SpaceHQ's chrome atom crown on a needle mast, peach and lilac sky, low golden sun, long soft shadows, warm haze layers, tiny citizens and small trams and cars on the streets. ")
FRAMES['A1'] = (['city_hero.jpg', 'blocks_saucer_spires.jpg', 'blocks_terraces.jpg', 'power_spire.jpg'],
   AER + "High and wide, looking over the harbour toward the city, SpaceHQ's atom crown tall at centre right, the coral sky rail curving through the lower third. " + CITY + GRADE)
FRAMES['A2'] = (['city_hero.jpg', 'records_office.jpg', 'mission_block.jpg', 'blocks_terraces.jpg'],
   AER + "Lower now, a descending view from above the sky rail looking down a broad civic square toward the Civic Records Office with its tall fluted entrance, chrome globe and teal banners, small figures crossing the pale plaza, a tram on the arches at left, the harbour and SpaceHQ's atom crown far behind. " + CITY + GRADE)
FRAMES['M1'] = (['inez_cutaway.jpg', 'records_office.jpg'],
   "Close medium shot, shallow depth of field: a young cafe waitress in a coral shift dress with a chrome belt, dark wavy hair, a white apron and a tray with a cup, standing at the corner cafe beside the Civic Records Office, glancing sideways out of frame with a worried, hopeful look, her eyes lit by the low golden sun. "
   "Behind her out of focus: seafoam striped parasols, cafe tables, the Records Office door with a guard in a pale slate-blue uniform. " + CITY + NPC + GRADE)
FRAMES['M2'] = (['cell_lineup.png', 'fixer.png', 'sky_case.png'],
   "Medium close shot across a cafe table: the Fixer (woman, black ponytail, SHORT ivory and black pinstripe halter dress, long ivory gloves; keep the short turnaround dress) sits at a seafoam cafe table with her flat brushed-silver attache case by her chair, and with one gloved hand slides a cream envelope across the white table toward a man in a pale slate-blue chauffeur jacket, bareheaded, who reaches for it. "
   "The cafe terrace beside the Civic Records Office behind them, out of focus, golden backlight, a black-and-ivory streamlined car at the kerb. " + KEEP + " " + CITY + NPC + GRADE)
FRAMES['M3'] = (['cell_lineup.png', 'tech.png', 'sky_visor.png'],
   "Close medium shot, shallow depth of field: the Tech (muscular man, tied dreadlocks, short beard, black wrap top with ivory sash and glowing cyan piping, chrome bracers, his angular chrome visor pushed up on his head), seen in profile, one black-gloved hand opening the small chrome junction box on the slim chrome post of a 1950s-styled closed-circuit camera in a chrome hood, thin cyan light spilling from inside the box onto his glove and face. "
   "Behind him out of focus, a cream limestone plaza with seafoam bands and a few tiny bareheaded citizens, golden backlight. " + KEEP + " " + CITY + NPC + GRADE)
FRAMES['M4'] = (['city_hero.jpg', 'patrol_robot_game.png'],
   "Medium shot through the open door of a small security booth: a bored security watcher, a heavy-lidded young man in an ink-black and ivory uniform jacket, bareheaded, slouching in a chair with his chin on his fist, in front of a wall of nine round-cornered chrome-framed 1950s tube monitors showing soft grey-green closed-circuit pictures of the cream civic plaza, a pneumatic message tube and a steaming cup beside him, warm lamp light, no keyboard, no computer. "
   "Cinematic shallow depth of field, a hint of golden light from the plaza outside. " + CITY + NPC + GRADE)
FRAMES['M5'] = (['cell_lineup.png', 'scout.png', 'sky_pack.png', 'records_office.jpg'],
   "Medium shot from slightly below: the Scout (lean young person, black undercut, earpiece, ivory hooded jacket, black harness straps, black shorts) with her small matte black technical backpack on one shoulder, on a high cream balcony with a chrome railing, leaning forward with both forearms on the rail, watching intently down toward the Civic Records Office and its square far below, one hand touching her earpiece, face in three-quarter profile and clear. "
   "Warm low golden backlight on her hair, haze, the sky rail and towers behind, out of focus. " + KEEP + " " + CITY + GRADE)

# ---- v5: tech with only visor + thin piping lit; envelope with a driver who has hair and a slate-blue jacket ----
FRAMES['V5S4'] = (['cell_lineup.png', 'tech.png', 'sky_visor.png'],
   "Character-introduction hero shot, close medium shot, camera at chest height: the Tech (muscular man, tied dreadlocks, short beard, black wrap top with an ivory sash, wide black trousers, chrome bracers, black gloves) facing three-quarter to camera, calm and focused, face and eyes clear, his angular chrome visor with a dark lens pushed up on top of his head in his dreadlocks. "
   "The piping is only a very thin cyan line, about one centimetre wide, running along the seams and edges of his black wrap top and along his trousers: those hairline lines glow softly cyan like thin light tubes. The ivory sash, the chest, the sleeves, the arms and the bracers do not glow at all: the sash stays plain ivory, the bracers plain chrome. "
   "Cool shade on him against warm gold backlight; behind him, out of focus, the shaded edge of a cream stone arcade with seafoam bands, a few tiny bareheaded citizens. " + KEEP2 + " " + CITY + GRADE)
FRAMES['V5M2'] = (['cell_lineup.png', 'fixer.png', 'sky_case.png', 'records_office.jpg'],
   FRAMES['M2'][1].replace("a man in a pale slate-blue chauffeur jacket, bareheaded, who reaches for it", "a man with a full head of short dark-brown hair, in a slate-blue (grey-blue) chauffeur jacket with a black collar and white gloves, who reaches for it").replace("bareheaded", "with hair"))
FRAMES['V5S4'] = (FRAMES['V5S4'][0], FRAMES['V5S4'][1])

def music(take, prompt, seed=None, note=''):
    task = {'model': 'minimax:music@2.6', 'positivePrompt': prompt, 'settings': {'instrumental': True}, 'numberResults': 1}
    if seed: task['seed'] = int(seed)
    PASS.audio(task, name=f'score_t{take}.mp3', est=0.2, subdir='audio', prompt=prompt, note=note)

def durl(path, size=None, q=92):
    im = Image.open(path).convert('RGB')
    if size: im = im.resize(size, Image.LANCZOS)
    b = io.BytesIO(); im.save(b, 'JPEG', quality=q)
    return 'data:image/jpeg;base64,' + base64.b64encode(b.getvalue()).decode()

def img(frame, take, seed=None, add='', note='', size='2560x1440'):
    refs, scene = FRAMES[frame]
    p = (scene + ' ' + add).replace('  ', ' ')
    w, h = map(int, size.split('x'))
    uris = [durl(HERE / 'refs' / r) for r in refs]
    PASS.image(IMG_MODEL, p, w, h, name=f'{frame}_t{take}.png', est=0.12, subdir='keyframes', inputs={'referenceImages': uris}, seed=seed,
               refs_log=[f'refs/{r}' for r in refs], note=note)

def vid(shot, key, model, take, prompt, dur=6, refs=None, note='', audio=False, res='1080p', mode='ref'):
    est = {'h3': 0.13 * dur, 'h3max': 0.5 * dur, 'hailuo': 0.49 if dur == 6 else 0.56, 'seedance': 0.4 * dur, 'seedance25': 0.614 * dur}[model]
    task = {'model': VMODELS[model], 'positivePrompt': prompt, 'duration': dur}
    keyp = Path(key)
    if model in ('h3', 'h3max'):
        fi = [{'image': durl(keyp, (1920, 1080), 95), 'frame': 'first'}]
        if mode == 'fl': fi.append({'image': durl(Path(os.environ['LASTFRAME']), (1920, 1080), 95), 'frame': 'last'})
        task['inputs'] = {'frameImages': fi}; task['resolution'] = '1440p'; task['settings'] = {'promptExpansion': 'disabled'}
    elif model == 'hailuo':
        task['inputs'] = {'frameImages': [{'image': durl(keyp, (1920, 1080), 95), 'frame': 'first'}]}
        task['width'], task['height'] = (1920, 1080) if dur == 6 else (1366, 768)
        task['providerSettings'] = {'minimax': {'promptOptimizer': False}}
    elif mode == 'fl':
        task['inputs'] = {'frameImages': [{'image': durl(keyp, (1920, 1080), 95), 'frame': 'first'}, {'image': durl(Path(os.environ['LASTFRAME']), (1920, 1080), 95), 'frame': 'last'}]}
        task['resolution'] = res; task['settings'] = {'audio': audio}
    elif mode == 'frame':
        task['inputs'] = {'frameImages': [{'image': durl(keyp, (1920, 1080), 95), 'frame': 'first'}]}
        task['resolution'] = res; task['settings'] = {'audio': audio}
    else:
        rl = [str(keyp)] + [str(HERE / 'refs' / r) for r in (refs or [])]
        task['inputs'] = {'referenceImages': [durl(r, None, 93) for r in rl]}
        task['resolution'] = res; task['settings'] = {'audio': audio}
    name = f'{shot}_{model}{"F" if mode=="frame" and model!="hailuo" else ("L" if mode=="fl" else "")}_t{take}.mp4'
    PASS.video(task, name=name, est=est, subdir='shots', prompt=prompt, note=note, duration_s=dur, refs_log=[str(r) for r in (refs or [])],
               params={'shot': shot, 'mode': mode if model != 'hailuo' else 'frame', 'last_frame': os.environ.get('LASTFRAME') if mode == 'fl' else None,
                       'resolution': res if model != 'hailuo' else '1080p-request', 'keyframe': str(keyp.relative_to(HERE)) if keyp.is_relative_to(HERE) else str(keyp), 'audio': audio})

if __name__ == '__main__':
    a = sys.argv[1:]; g = lambda k, d=None: a[a.index(k) + 1] if k in a else d
    if not a or a[0] not in ('img', 'vid', 'music'):
        raise SystemExit(__doc__)
    PASS = paid.Pass(JOB, g('--pass'), budget=float(g('--budget')) if g('--budget') else None, allow_raise='--raise' in a) if g('--pass') else None
    if PASS is None:
        raise SystemExit('paid: --pass N is required (and --budget USD the first time a pass is used); see the docstring')
    if a[0] == 'img': img(a[1], g('--take', '1'), g('--seed'), g('--add', ''), g('--note', ''))
    elif a[0] == 'music': music(g('--take', '1'), g('--prompt'), g('--seed'), g('--note', ''))
    elif a[0] == 'vid': vid(a[1], a[2], g('--model', 'hailuo'), g('--take', '1'), g('--prompt'), int(g('--dur', 6)), g('--refs', '').split(',') if g('--refs') else None, g('--note', ''), '--audio' in a, mode=g('--mode', 'ref'))
