"""The round 2 board's words (brief 101). notes.md says the same at length."""

SKY = ['“A just covers too much of the screen. And that\'s not even a busy example you\'re showing”',
       '“Why is their stats about 75% etc showing when firing?”',
       '“Don\'t look at how long something takes because I don\'t have a choice with the action so it\'s irrelevant as a player. '
       'Only thing that feels time is important are the key events that happen”']

RULE = ('So in both directions: no hit chance anywhere, and no action durations (no seconds to down, walk times or act lengths). Time shows only '
        'for key events: the clock, the seal, police arriving, the rivals, the handover chime, a report about to land, a loop running out. '
        'An order\'s price is money, Trail, Alert risk and what the street will do.')

BUSY_CAP = ('The final fight, 3:47 into the loud run (seed 3): the seal at 1:54, Police coming with 4 more cards waiting, two bubbles, a policeman '
            'holding our car from the road, Fixer and Tech firing on him, the player holding fire on him too, a report about to land, Popovic off '
            'screen with the Enforcer.')
BUSY_REAL = ('The frame, the clock, the seal, the cards, the bubbles, the car and who holds it are the batch capture of build 0.2.268. Today\'s aim '
             'pill, focus marks and report plate are laid on from other real captures\' HUD layers. The report is borrowed from 4:54 of the same run.')

DIRS = {
    'today': {'name': 'Today', 'line': 'Dark chrome plates on every piece. The seal card alone is a 420 × 104 slab; the aim pill puts "75 %" mid-line.'},
    'S': {'name': '1 · Street labels', 'line': 'No plates at all. Bold type with a solid cream keyline, so it reads on paving, asphalt and palms alike. Everything else is labelled where it happens.'},
    'T': {'name': '2 · Corner tokens', 'line': 'The same street layer, but the clock, the seal, the news and the roster sit on small enamel tokens: a station-clock roundel, an orange seal roundel, one slim tail.'},
}

ROOM = ('Measured the way tools/hud-budget reads a capture: the HUD layer alone, alpha 24 and up, so keylines, shadows and halos count. '
        'The rings at people\'s feet count here too, though the game draws its ground marks in the world, so the build would read a little lower. '
        'The budget allows 15 % at rest. Today\'s numbers are build 0.2.268, before Latch\'s removal of the odds and durations: that trims words, not plates.')

SHOT_POINTS = [
    'The first shot shows what the street will do: Goes loud, Alert to 2, police come, the district seals, Trail +6. The hold ring fills turquoise as you commit.',
    'On the target, his ring in his signal colour: whole when he\'s in range, in three arcs for a long shot. That\'s the only hint, and it has no number. A shield on its front when his cover counts (half-filled: in part).',
    'In the final fight a shot costs nothing new (fight shots are untraced, and the Alert is already 2), so the aim is just his ring and the word Fire.',
    'A firing agent\'s portrait shows rays at the sidearm, never the seconds to down. The hit rate moves to the debrief\'s numbers.',
]

ODDS = ('Today\'s odds plate is already going. Latch\'s removal, in progress, leaves HostOdds.cs with only the debrief\'s signed number and the '
        'Enforcer\'s flash, and HudToolkit.Hold.cs no longer shows a fire breakdown on Alt or on a 0.6 s rest. Retire it, don\'t move it: its parts '
        'become shapes (range is the ring\'s arcs, cover the shield, movement the target\'s own run, aim the agent\'s Aim pips on the book\'s Cell '
        'page), and the streak note goes to the how-to-play. One Alt-only line is left beside it, a locked way\'s long reason. It should also come up '
        'on the 0.6 s rest, so it isn\'t key-only.')

PICK = ('My pick: 2 · Corner tokens', [
    'The corners are what you glance at twenty times a minute. A fixed shape in a fixed place is found without reading: the orange roundel is the seal before you read 1:54. Sky missed the seal when it was small (brief 093); a token can\'t be missed and still costs little.',
    'It is still half of today: 3.0 % at rest against 4.6 %, and 5.1 % at the busiest moment against 10.0 %. Everything on people is the street layer, the same in both directions.',
    'It has a voice. Flat vitreous enamel in the book\'s deep teal, cream and orange, like the city\'s own signs. It belongs with the book, the start page and debrief A, with no chrome or screws.',
    'Street labels (1) is the fallback if Sky wants it leaner still. Its bare type reads on any ground now, but the corners lose their shape.'])

BUILD = [  # (step, what, size)
    ('0', 'Measure the HUD alone on the Air at play zoom, with the HUD on and off. Its cost has never been split (brief 086).', 'S'),
    ('1', 'Kit: the enamel roundel and lozenge sprites, the keyline (UI Toolkit\'s own text outline plus a soft shadow), keyed copies of about 20 street icons. The book\'s colours go in kit.uss.', 'S'),
    ('2', 'The clock token and the seal roundel first, Sky\'s pain point. They replace the status pill and the 420 × 104 seal card; the key people hang under the tail. HudToolkit.Seal.cs, the pill in HudToolkit.cs, the key people in HudToolkit.Less.cs.', 'M'),
    ('3', 'The news lozenge and the Book roundel. HudToolkit.Crucial.cs, HudToolkit.BookKey.cs.', 'S'),
    ('4', 'The roster: enamel-ringed live faces, no tray, health pips, the act\'s icon with no seconds, the cue layer kept. HudToolkit.Roster.cs, HudToolkit.Live.cs.', 'M'),
    ('5', 'The scope as a round enamel instrument with three round tabs. HudToolkit.Scope.cs.', 'S'),
    ('6', 'The street layer: keyline labels, the shot (ring, arcs, shield, hold, Fire, the Goes-loud row), the report\'s draining ring, the car\'s lock and seat pips, holder marks, edge faces, bubbles with the speaker\'s face. The rings join the world-mesh batch in HudToolkit.Ground.cs. Also Hold.cs, Fire.cs, TargetRings.cs, OnWay.cs, HeldGo.cs, CuesEdge.cs.', 'M'),
]
BUILD_NOTE = ('Sizes are my estimate for Latch to check: S is small, M is a few S. HudToolkit.cs (1,300 lines) and HudToolkit.Less.cs (1,847) are over the '
              '500-line rule, so each piece moves into its own file as it changes (brief 084). Removing the odds and durations is Latch\'s work already under way.')

STAYS = ('What stays: every position on the GDD\'s HUD map, every mechanic and every click; the signal colours and their rules (cones red, guard amber, '
         'rivals violet, unlit); the live portraits and their cues; Jost and the kit\'s icons; the act ring\'s wedges; the budget check and the 2x crop check. '
         'The GDD\'s §15 HUD kit (“Chrome plate”) changes once Sky has picked and seen a real capture, in a new version.')

PERF = ('On the Air: no full-screen pass and no blur at run time. Tokens are sprites, the keyline is the text outline UI Toolkit already draws, and the '
        'rings are world meshes in the existing batch. Labels are set only when their value changes (the clock once a second). That\'s an estimate; step 0 measures it.')

WORDS = ('Words for Fable: one new label, “Goes loud”. “Fire”, “Chime” and “1 of n ›” are already the game\'s. The less.odds block (chip, sum, limits, '
         'streak) leaves play.')

NEED = [
    'One real capture of the busiest moment, so no composite is needed: the loud run on seed 3 at about 3:48, the Scout selected, a right hold on the policeman at the planter run (security-1).',
    'A report walking during a fight. On seed 3 none does at 3:47; the next starts at 4:54, after the fight. A seed with one, or 4:54 to 5:03 of this run.',
    'The same moment at the Air\'s fullscreen 2880 × 1864, and one at night.',
    'The police countdown after Popovic is out, and the Go card with the seat pips filling.',
]
