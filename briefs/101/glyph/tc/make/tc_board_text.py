"""The take-control board's words (brief 101, Glyph). Plain words; figures from make/coverage.json are filled in by tc_board.py."""

CAP = 'brief 101 · the ground view · glyph'
TITLE = 'Take control gets a button'
SUB = ('Glyph, 9 November 2026. Mock only: nothing built, nothing committed, $0. Real frames: 0.2.288 on the map (captured at 2x) and 0.2.289 '
       'in third person. Every colour is from street.uss. The only new art is two pictograms.')

CALL = 'My call: a button'
WHY = [
    'The ground view already plays in every build, and only T reaches it. Sky\'s rule is no key-only features, so it needs a click. The other '
    'way, gating T behind the lab switch, takes away a view Sky uses.',
    'It\'s cheap: one tab, the way back in the same slot, one Esc branch, two pictograms and the eye. About a day for Latch, with its proofs.',
    'It costs almost no screen: {rest_d} % at rest, {hover_d} % hovered, {ctrl_d} % in third person. All four frames pass the HUD budget.',
]
WHY_AFTER = ('Ship the eye with it: without it you can\'t tell from the street whether anyone sees you. A button puts the ground view in the '
             'game, so it\'s Sky\'s call and a new GDD version: §10 still marks the view [lab].')

ICONS_HEAD = 'Two new pictograms, in the kit\'s style'
ICONS = [('take_control', 'Take control', 'Two chevrons drop into the ring the cell stand in: down to the street, as her.'),
         ('map', 'Back to map', 'A folded paper map with a pin.')]
ICONS_NOTE = ('Five other drawings were tried and dropped at 14 px: a chevron over a bust read as a medal, an arrow over a bust as a cross, a '
              'bust in its ring as "selected", and an arrow or triangle into the ring looked like the kit\'s Put down.')

REST_HEAD = '1 · At rest, on the map'
REST = ('One round tab beside the selected agent\'s face, right of her pips. It\'s the built tab: 30 px, the book\'s body, a 2 px cream ring. '
        'The face\'s corners are all taken (known badge, the watcher\'s eye, sidearm, act disc), so it sits beside her. It shows next to the agent '
        'T would take, and hides while she\'s down, inside or in a car.')

HOVER_HEAD = '2 · Hover'
HOVER = ('Its ring turns turquoise, like every built tab, and its word appears as a street label: Fable\'s "Take control", the key after it in '
         'soft ink. Point at another face and that face shows its own tab. One click takes control of that agent.')

CTRL_HEAD = '3 · In control'
CTRL = [
    'The same slot becomes the way back. It has a word, so it\'s a capsule (rule 2): the map and Fable\'s "Back to map".',
    'The Squad tab stays Squad in both views. It no longer doubles as the way back.',
    'Over her head, one keyed eye: could anyone see an act or a run here? Shut means no, open in the cones\' red means yes, "?" means a watcher '
    'the cell hasn\'t learned might. GDD §10 specifies it; it isn\'t built.',
    'The selection chevron goes while you drive her: the camera is behind her and her face is ringed. Her act stays on her face\'s disc.',
    'Nothing else changes: the clock, the scope with its turn tabs, the Book and the news stay as built.',
]
EYES = ['nobody sees', 'seen', 'might be seen']

BACK_HEAD = '4 · The way back'
BACK = ('Click the capsule and the camera glides back to the map (0.5 s, as built), centred on her at play zoom and the yaw you left. Her order '
        'keeps running, and her face\'s tab reads "Take control" again. T does the same. So does Esc, once nothing else is open (ring, hold, '
        'panel), but that overturns brief 041\'s "Esc stays the pause menu", so it needs Sky\'s yes.')

COST_HEAD = 'What it takes (Latch)'
COST = [
    ('The tab beside the face: when it shows, its click (select that agent, then take control, as T does), its hover word', 'HudToolkit.Faces.cs, HostDrive.cs', 'S'),
    ('The capsule in the same slot in third person; the Squad tab back to plain Squad', 'HudToolkit.Faces.cs, HudToolkit.Hold.cs', 'S'),
    ('Esc in third person goes back to the map once nothing else is open', 'HandoverHost.cs, one branch', 'XS'),
    ('The two pictograms into Kit/icons/singles (from briefs/101/glyph/tc/icons/)', 'Kit', 'XS'),
    ('The eye over the driven agent: the ring\'s "seen" read for where she stands, twice a second like the run\'s eye; no chevron while driven', 'HostLess.cs, HUD marks', 'S'),
    ('Proofs: r083\'s back-to-map row reads the capsule, plus a new row for the tab. Delta only: r083, r085, drive, holdActs, the shots', 'Proof083.cs', 'S'),
]

LACKS_HEAD = 'What the lab view still lacks for the real game'
LACKS = [
    'Who sees you, beyond the eye: pips over known watchers within 30 m, an edge marker for one off screen, the learned cone as a curtain '
    '(§10). Test T14 also needs a "goal beam" that nothing specifies yet.',
    'Balance: §10 says the map is the only view the game is balanced in. Driving keeps the rulebook\'s paces and 1x speed, so prices hold, but '
    'nobody has played a plan walked from the street (T14 has never run).',
    'Frame rate: the bar is 60 fps on the Air, "also at street level". The last street-level reading is the Studio\'s 223-227 fps (brief 098). '
    'There\'s no recent Air reading at street level.',
    'The ring at her feet: up close the cell ring is about 220 px across at the bottom of the screen. It\'s world art, so the HUD budget doesn\'t '
    'count it. It could thin while you drive her (optional, my look).',
]

COV_HEAD = 'Coverage'
COV_NOTE = ('% of the 1920 x 1080 screen, read as tools/hud-budget reads a capture (the HUD layer, alpha >= 24). "Mocked" is today\'s layer with '
            'the lifted pieces out and the new ones in. All four pass the project\'s reader (make/budget.txt).')
