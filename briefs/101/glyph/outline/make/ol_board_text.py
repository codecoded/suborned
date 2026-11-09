"""The hover outline board's words (brief 101, Glyph). Plain words; the screen shares come from make/coverage.json."""

CAP = 'brief 101 · hover outline · glyph'
TITLE = 'A light outline on what you point at'
SUB = ('Glyph, 9 November 2026. Concept only: nothing built, nothing committed, $0. Real frames: the r085 proof at the Office door '
       '(play zoom 10, tonight\'s batch) and Sky\'s own frame from play. Each outline is drawn from the thing\'s real shape, the way Unity would draw it.')

CALL = 'My pick: B, a thin ink line with a cream edge'
WHY = [
    'It means nothing else. Turquoise is the cell and the selection, cream is the edge of a person seen through cover, and red, amber and '
    'violet are the signals.',
    'It reads on any ground: the ink on the cream city, the fine cream edge on dark glass and asphalt. It\'s the street labels\' own keyline, '
    'so it belongs with the HUD.',
    'It\'s a little: 1.5 px of ink and 1 px of cream at 1080p, in 0.1 s. Whatever stands in front of a thing hides its line, so the door\'s '
    'line never runs round the guard.',
]
WHY_AFTER = ('One look for everything a click acts on: people, the cell, cameras, cars, the door. It only says which one; the pill still '
             'says what the click does.')

LOOKS_HEAD = 'Three looks, on Sky\'s case'
LOOKS_NOTE = 'Each pair: the pointer on the guard, then 40 px to the right, on the door. 2x.'
LOOKS = [
    ('A', 'Lit edge', ['cream, 1.5 px, inside the edge, and the thing lifted 7 %', 'fades in 0.1 s', 'hidden behind what covers it']),
    ('B', 'Ink keyline (my pick)', ['ink 1.5 px, then cream 1 px, outside the edge', 'fades in and out in 0.1 s', 'hidden behind what covers it; '
                                     'round a person\'s silhouette']),
    ('C', 'Turquoise glow', ['turquoise 1.5 px and a 5 px glow, outside', 'fades in 0.15 s', 'shows through what covers it, at 35 %']),
]
PROS = {'A': ('+ never touches a neighbour, and it\'s quiet', '- it\'s the silhouette\'s cream edge, and a white outline on dark ground'),
        'B': ('+ means nothing else, and reads on light and dark', '- on dark ground only its thin cream edge shows'),
        'C': ('+ the strongest, and reads anywhere', '- it\'s the cell\'s own colour; heavy; it shows through walls')}
TRIPS_HEAD = 'Where A and C trip'
TRIPS = [
    ('A: cream is the edge the game already draws round a person seen through cover. On the Fixer, whose feet are behind the sphere, the two '
     'merge, and on her light clothes the line fades.'),
    ('C: turquoise is the cell\'s own colour (the selection ring and chevron, the cell\'s rings). The hovered Fixer reads as selected, next to '
     'the Scout, who is.'),
]

C1_HEAD = '1 · The guard in the Office door'
C1 = ('The Enforcer is selected (brief 085\'s case). On the guard\'s body and 20 px round it, a click opens the guard\'s ring ("Hold · Watch '
      'the guard"); 40 px to the right it opens the door\'s ("Hold · Enter the Office"). Today only the pill\'s words tell them apart. Now the '
      'one that would open lights up.')
C1_CAPS = ('the pointer on the guard', '40 px right: the door')
C1_NOTE = ('The door\'s line passes behind the guard, the globe and the stone fin, because what stands in front of a thing hides its line. '
           'Otherwise the door\'s line would run round the guard and outline him too.')
C1_DOOR = ('The door has no mesh of its own: in the game it\'s an invisible 2.4 m box at the guard post. Its outline comes from a stand-in, '
           'a flat arched panel the doorway\'s size that is drawn only for the outline.')
C1_PILL = 'The pill shows in play but isn\'t in this capture.'

C2_HEAD = '2 · One of the cell among the others'
C2 = ('Sky\'s frame at 1080p size. The pointer on the Fixer, then on the Tech, whose boots her head overlaps. You can see whose click it is '
      'before you click. A click on one of the cell selects her, so the cell gets the outline too: one rule for whatever a left click acts on.')
C2_CAPS = ('on the Fixer', 'on the Tech: her head is in front of his boots, so his line stops there', 'today')

C3_HEAD = '3 · Partly hidden'
C3 = ('The Fixer\'s feet are behind the sphere. The game already draws them through it as a flat teal shape with a cream edge (the occlusion '
      'silhouette). The line goes round that too, so she reads as one person. A thing with no silhouette (the door, a car, a camera) hides '
      'its line behind what covers it, as the door does behind the guard.')
C3_CAPS = ('my pick: round her silhouette too', 'the other way: the line stops at the sphere')
C3_PLAY = 'At play size this is case 2\'s first picture.'

C4_HEAD = '4 · Not a person'
C4 = ('The lamp mast\'s camera: the line goes round its head (the pods and the glass dome), not the whole mast, because the post is street '
      'furniture. The official car: on the dark road only the cream edge shows, and that\'s enough.')
C4_CAPS = ('the mast at 1080p size', 'its head, close', 'the car at play zoom', 'close, 2x')

GETS_HEAD = 'What gets it, and what it replaces'
GETS = [
    'Whatever a left click acts on: a person with a ring, a camera, a car, the door, the cabinet, a stash, a body, a downed agent, and the '
    'cell (a click selects her). Never a passer-by, unless the selection has a way on them.',
    'Tab, Z or the wheel step through things under the pointer, and the outline moves with each step.',
    'While a ring is open, its thing keeps the outline, so you can see whose ring it is.',
    'The colour carries no meaning: one look for every thing and every agent.',
]
REPL = [  # piece, today, with the outline
    ('the cream ring at a person\'s feet when there\'s no pill (brief 085)', 'goes: the outline is that cue now'),
    ('the dashed rings at the feet of the other things under the pointer (brief 067)', 'go: the outline marks the one a click hits'),
    ('the dots on the pill (brief 067)', 'stay: they say how many things are there'),
    ('the pill: the way and its price', 'stays'),
    ('classic controls: the cream ground ring and ellipse / the name chip', 'go / stays'),
]

LATCH_HEAD = 'For Latch, when Sky says yes'
SPEC = [
    ('line', 'ink #17302f, 1.5 px at 1080p, 92 %; then cream #f7f4ec, 1 px, 85 %; both outside the edge'),
    ('size', 'in 1080p px times screen height / 1080 (2.6 + 1.7 px on the Air at 2880 x 1864); the same at every zoom'),
    ('fade', 'in 0.1 s, out 0.1 s; a new thing fades in as the old fades out; it holds 0.15 s when the pointer leaves to open ground '
             '(the pill\'s stick)'),
    ('cover', 'the line sits at the thing\'s depth, hidden where something nearer stands, except a person drawn as a silhouette: their line '
              'goes round the silhouette too'),
    ('order', 'after the world, under the HUD; never on the book, the start page or a conversation scene; map view only for now'),
    ('shape', 'the thing\'s own renderers (a figure, the sedan model, a camera\'s head). The door, the cabinet and stashes use a stand-in '
              'drawn only into the mask: for the door, an arched panel the size of the Office\'s public doorway'),
]
ROUTE = ('The standard URP route, built the way the silhouettes are (SilhouettePass, brief 056 F, with no renderer-asset edit). The host '
         'puts the hovered thing\'s renderers on a rendering-layer bit (1 << 23). A RenderGraph pass after the opaques draws them into a small '
         'mask with their own depth. Then a full-screen pass, cut to the thing\'s screen box plus 8 px, draws the line from the mask\'s distance and '
         'compares depths. The HUD draws after it.')
COST = ('Cost: one extra draw of one thing, plus a pass over its box only (a figure is about 70 x 170 px on the Air, the door about 190 x 760). '
        'That\'s well under 0.1 ms of the Air\'s GPU at 2880 x 1864, and nothing at all while nothing is hovered. Redline to confirm it on the Air.')
PROOF = ('Proof, delta only: r085\'s two hovers (on the guard, and 40 px beside him) gain captures "r085-outline-guard" and "r085-outline-door", '
         'with a row checking that the outline is on the thing the pill names. The look needs no new capture: r085\'s frames are at play zoom 10.')
SIZE = 'About a day, with its proof.'

SKY_HEAD = 'For Sky'
W_CAPS = 'true size: above 1.5 px (my pick), below 2 px'
ASKS = [
    ('Which look?', 'B, the ink line (my pick), reads everywhere and means nothing else. C, the turquoise glow, is the strongest but reads as '
                    'the cell\'s own colour. A, the cream lit edge, merges with the edge of a person seen through cover.'),
    ('Is it a little?', '1.5 px of ink and 1 px of cream at 1080p, at true size below, with 2 px under it. 2 px reads too, but heavier; '
                        'it\'s the step up if 1.5 gets lost in play.'),
    ('Into the GDD?', 'Your yes adds one line to §10: "what a left click would act on wears a thin ink outline". Aster writes it as a new '
                      'version.'),
]

HOW_HEAD = 'How it was made'
HOW = [
    'Each thing\'s shape was cut from its frame (GrabCut on a 4x crop, with hint shapes). The door\'s shape is the doorway\'s. The line is '
    'drawn from each shape\'s distance at 4x, then scaled down, which is the same sum the edge pass does.',
    'Frames: r085-guard-enforcer and r085-guard-hover from Latch\'s batch (copied before the batch moved on), and Sky\'s frame (2880 x 1864, '
    'HUD scale 2).',
    'The line\'s share of the screen while hovered: {lo} % (the guard) to {hi} % (the door, the car). It\'s world art like the rings, so '
    'tools/hud-budget doesn\'t count it.',
    'Scripts: make/ol_masks.py (the shapes), ol_draw.py (the three looks), ol_frames.py (the pictures), ol_board.py and ol_board_text.py '
    '(this board). All under 500 lines.',
]
