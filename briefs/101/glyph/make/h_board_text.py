"""The board's words (brief 101): the inventory, the three directions, the pick and the build order. notes.md says the same at length."""

QUOTE = '“really hate this card and design. Lot of our HUD UI has become lazy looking. Do a sweep to see how we can massively improve it” (Sky, on the seal card)'

ROOT = ("The common fault is one frame for everything: brief 044's chrome-rimmed dark plate with small cream type. With one frame and one size, "
        "a sentence, a number and a warning all weigh the same, so nothing leads. The dark plates also fight the bright street and the book's "
        "cream page. The cure isn't small again (brief 093 made the seal big because it was missed). It's big where it matters, and designed.")

TODAY = [  # (crop key, title, verdict)
    ('seal', 'The seal card', 'Three lines of small text on the same dark plate. The countdown is no bigger than the sentence, the 2 px bar says nothing, and it repeats the job.'),
    ('police', 'Police coming', "The why is a grey 13 px run-on with an arrow, and 'here in ?' sits where the seconds should be."),
    ('top', 'The top bar', 'Six equal tokens on a chrome pill. Alert, the number that matters in a crisis, is three 9 px dots by the pause.'),
    ('aim', 'Aiming', "'Let go · Fire · 75 % · Alert ≥2': the chance, the one number to decide on, sits mid-line at body size."),
    ('feed', 'A feed card', 'The same plate and size for every kind of news, so police and a boxed-in car look alike.'),
    ('report', "The reporter's countdown", 'The most urgent thing on screen (stop him in 4 s) is a 26 px plate with no face. Its meaning is on hover.'),
]

INVENTORY = [  # (piece, verdict) for everything else in play
    ('Known people', 'Four 14 px pictograms on a second small plate. They don\'t read as people, and nothing says they can be clicked.'),
    ('Roster', 'The painted faces are the best art in the HUD, boxed in a chrome tray with screws. 8 px health dots, no names; the busy chip is one more dark plate.'),
    ('Plot scope', 'The period scope is the right idea. Its three buttons are identical chrome coins: the new eye differs only by a 12 px icon.'),
    ('Why meter', "It answers Sky's question, but reads as a log: grey two-line rows on a dark drawer over the street."),
    ('Act ring', "The wedges and their icons work. Each wedge's plate is a row of symbols ('draws 1.0 s, +2, ≥2, !') that needs a legend."),
    ('Speech bubbles', 'In the right place but too small: 13 px type on a thin dark pill, with a letter in a circle where a face should be.'),
    ('Pops', "Readable, but the same plate again. Two at once ('Camera on', 'Patrol back') look like debug labels."),
    ('Markers and badges', "'46' with a mystery glyph floats in the street: 46 what? The new eye-and-count badge is clear. Door, pip and lock markers are small dark stickers."),
    ('Attention callout', "Its close-up face is the right idea, but it's drawn as one more chrome-rimmed disc."),
    ('Book key', "Clear and clickable, but a dark plate beside a book that's a cream page with round tabs."),
    ('Dial (dev)', "Dev only, so plain is fine, but it's the one dark slab on the light start page."),
    ('End card', "Today's one-line beat is being retired by debrief A. The HUD should lead into that page, not look like another game."),
]

DIRS = {
    'A': {'name': "The page's edge", 'file': 'A-page', 'mood': None,
          'idea': ("The book's cream page peeks into play at the right-hand corners, cut by its big curve with the turquoise halo. The clock and numbers "
                   "live in the top corner, and the one crucial card in the bottom one, its badge breaking the curve. When something matters, the corner "
                   "grows: the seal turns its halo orange and puts the countdown huge under the clock. It uses ink type, round tabs, painted faces, and orange for now. "
                   "Pressing Book grows the corner into the whole page."),
          'good': 'Same surface as the book, the start page and debrief A, so play and pages read as one designed game. Ink on cream reads best of the three, and the big numbers have room.',
          'risk': 'It takes the most screen of the three, though still under the 15 % limit. Cream corners over the road could feel like a dialog at the edges.'},
    'B': {'name': 'Enamel signs', 'file': 'B-sign', 'mood': 'B-enamel.png',
          'idea': ("The HUD is the city's own signage: flat vitreous enamel in the book's deep teal with a cream inline (the start page's Front door "
                   "and Pickup plates, the book's tabs), cut as Googie roundels and lozenges. The clock is a station roundel; the seal hangs under it "
                   "as an orange roundel; numbers sit on cream price tickets."),
          'good': "The cheapest: today's layout and teal stay, and the plates become flat sprites. Strong shapes, and the seal roundel is hard to miss.",
          'risk': "It's still dark plates on a bright street, so it can read as today's HUD recoloured. The shapes change more than the hierarchy does."},
    'C': {'name': 'In the street', 'file': 'C-street', 'mood': 'C-street.png',
          'idea': ("Almost nothing in the corners. The city is labelled where things happen, like a planner's drawing: ink labels with a cream halo "
                   "on the street, faces on stems, lines from a cause to its place, rings on the ground. The seal is the district closing in, an orange "
                   "edge round the whole view. The hit chance is a gauge on the target's ring. News from off screen docks at the edge it comes from."),
          'good': 'Takes the least screen. The most Syndicate: the rules show in the street (GDD §10), and the why is a line to its place.',
          'risk': 'Labels over busy ground (palms, street furniture) are harder to read, it has the most world-anchored pieces to build, and the card queue gets less obvious.'},
}
MOMENT_NAMES = {'seal': 'The seal warning', 'police': 'Police coming, with its why', 'top': 'The top bar mid-mission', 'aim': 'A hover and aim on a target',
                'feed': 'A feed card', 'report': "The reporter's countdown"}

PICK = ('A, with three street marks from C', [
    "It makes the HUD the same object as the book, the start page and debrief A, which Sky picked. That's the brief's goal.",
    'It fixes the hierarchy, not just the style: one big number per moment (3:07, 0:42, 75 %, 4 s), with the words small beside it.',
    'Ink on cream is the most readable of the three at play zoom, and the corners never move, so the middle of the street stays clear.',
    "From C, take the marks that belong on a person, not in a corner: the reporter's draining ring at his feet, the chance as a gauge on the target's ring, and the why's dotted line to its place."])

BUILD = [  # (step, what, size)
    ('0', 'Measure the HUD alone on the Air at play zoom, HUD on and HUD off. Its cost has never been split from the frame.', 'S'),
    ('1', "Kit: the corner shape with its halo baked in, round tabs, lamps, ink and cream text styles, and the book's colours in kit.uss.", 'S'),
    ('2', 'The top corner and the seal first (Sky\'s pain point): clock, numbers and key people; it grows for the seal, the job drawn as Popovic to the car, with Go on it once he\'s aboard. This retires the seal card and the status pill.', 'M'),
    ('3', 'The bottom corner: the crucial card (badge on the curve, title, why, big number, 1 of n) and the Book tab.', 'M'),
    ('4', "The reporter: his face on a stem, the draining ring at his feet (from C), the seconds, and the hover slip.", 'S'),
    ('5', "Aim and price: the chance big on a slip, then down-in and the going-loud row; the gauge on the target's ring (from C).", 'M'),
    ('6', 'Roster: names under the faces, the busy arc, readable health. Live portraits stay as they are.', 'M'),
    ('7', "Plot scope: the map in the book's inks, with round tabs for turn, turn and the eye.", 'S'),
    ('8', 'Everything else in the same voice: bubbles with the speaker\'s face, pops, window markers, the ring\'s price plates, the why meter as a page fold, edge cameos.', 'M'),
]
STAYS = ("Every position on the GDD's HUD map, every mechanic and every click; the signal colours and the cones; the painted faces, Jost and the kit's "
         "icons; the ring's wedges and the turquoise act ring; Fable's words (the long lines move to hover and the Journal); the HUD budget and the 2x crop check.")
PERF = ("No direction adds a full-screen pass or a blur at run time: every shape is a sprite or a vector mesh rebuilt only when it changes, with its halo and "
        "shadow baked in. A cream corner covers 3 to 6 % of the screen in one blended layer, a tiny fraction of a millisecond on the M4's GPU (an estimate). "
        "The cost to watch is the main thread, where text is rebuilt every frame. The rule: set a label only when its value changes (the clock once a second). "
        "C's labels move with the world every frame, a dozen at most: cheap, but not free. Step 0 measures it before anything is built.")
MISSING = ["The going-loud card, with its held commit", "The police countdown after Popovic is out (Police close in, End now)",
           "The getaway's Go card with seat pips, and a blocked Go", "The rivals' tally under the clock", "The why meter pinned, with a row hovered",
           "Any HUD state at night (only the café's night rest exists)", "The Air's fullscreen 2880 × 1864 (every capture is 1920 × 1080)",
           "The ground view with a card and the seal", "The roster under fire, hurt and downed", "A refusal pop at the cursor ('Too far · 4 m')"]
