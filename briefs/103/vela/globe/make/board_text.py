"""Brief 103 (Vela): the words on the globe board (board.py). Plain words, one idea a line."""

HEAD = dict(
    caps="Brief 103 · The Office's globe · Vela · 9 Nov 2026",
    title="What goes in the Records Office's window",
    sub="Sky: “what is this on the districts office? looks terrible”. These are renders of the entrance at the play camera (zoom 10, 38°), "
        "shown at the size it plays at 1080p, in light matched to Sky's frame. They aren't the game, and nothing in the game has changed.",
)

PICK = dict(
    key="A",
    title="My pick: A, the painting's ball, smaller, in satin nickel",
    why=[
        "It's the painting's idea, a ball in the arch on three slim columns, at the size our 6 m face can hold: 1.5 m across, inside the arch, "
        "2.2 m above the marquee. Nothing hangs over the door or the guard any more.",
        "Satin nickel, not mirror chrome. In our light chrome mirrors a pale sky and reads as glass. Satin metal is lit by the sun and has a "
        "shaded side, so it reads as a solid silver ball, with less shine.",
        "The door reads the same in every option: the globe never covered it. What A changes is that the biggest, brightest thing in view no "
        "longer sits right above the guard. If you'd rather the door stood alone, B is the safe second.",
    ],
)

REF = dict(
    sky="In play today: Sky's frame at yaw 210.",
    paint="The 26 Sep painting: a mirror ball high in a very tall window, held on three columns, well clear of the canopy.",
)

YAWCAP = {210: "yaw 210, the play heading", 300: "yaw 300, a quarter turn: the front almost face-on"}

ROWS = [
    ("Today", "The relief globe",
     "2.4 m across, with chrome seas and steel continents. In our light it reads as blotchy pale-blue glass, and it hangs over the marquee "
     "right above the guard."),
    ("A", "The painting's ball",
     "1.5 m across and smooth, on the painting's three slim columns, inside the window's arch. Satin nickel, so it reads as solid metal, not glass."),
    ("B", "No globe",
     "The window, the blades, the marquee and the lanterns as they are."),
    ("C", "An enamel armillary",
     "Matte enamel, no shine, in A's place: sunshine-yellow hoops (§15 gives the records area sunshine), a coral band and a turquoise "
     "core, on a slim teal stem."),
]

MATS = dict(
    title="Why not the painting's mirror chrome?",
    line="The game's chrome mirrors one bright probe of sky and paving, so a chrome ball has no shaded side, and it reads as glass. Anything with a "
         "colour of its own reads solid: satin metal (the sun lights it and its reflections are soft) or enamel. Shown close, at yaw 210.",
    caps=[("chrome", "Mirror chrome, as painted: glass"),
          ("nickel", "Satin nickel: solid silver (my pick)"),
          ("gold", "Satin gold: solid and warmer; allowed on a landmark"),
          ("enamel", "Turquoise enamel: solid, but toy-like")],
)

FINIALS = dict(
    title="The four chrome finials read as glass too",
    line="The balls on the two piers and the two small ones on the marquee's corners are chrome, and in our light they read as the same "
         "pale-blue glass. Here is A with them as they are, and A with them in the ball's satin nickel.",
    caps=[("a-nickel", "A, finials as they are (chrome)"), ("a-nickel-finials", "A, finials in satin nickel")],
)

DOOR = dict(
    title="The door reads the same in every option",
    line="The globe never covers the doorway. In every option the same share of a stand-in filling the doorway shows: 42% at yaw 210 and 59% "
         "at yaw 300. The marquee and the guard hide the rest. What changes is what sits above the guard. Glyph's hover line (ink with a cream "
         "edge) is drawn on the door here.",
    caps={"Today": "Today: the biggest, brightest thing in view hangs right over the guard.",
          "A": "A: the ball sits in the arch, well above the marquee, and leaves the door and the guard alone."},
)

YAW30 = dict(
    title="At yaw 30 and 120 the front faces away",
    line="From both headings the Office's wing hides the entrance; only one pier's finial peeks round the corner. No option changes what "
         "they show.",
)

DECIDE = [
    ("Which ornament goes over the Office's public door?",
     "It's the Handover's door, and today's globe reads as blotchy pale-blue glass hanging over the guard. A is the painting's ball, smaller "
     "and in satin; B takes the globe away; C is a matte enamel armillary. I recommend A, with B as the safe second."),
    ("If A: satin nickel or satin gold?",
     "Nickel is the painting's silver. Gold is warmer and reads more strongly, and §15 allows gold on a landmark. I recommend nickel."),
    ("Should the four chrome finials take the same satin finish?",
     "They read as pale-blue glass for the same reason. It's the same piece and the same kind of change. I recommend yes, whichever option you pick."),
]

FOOT = [
    "How these were made: Blender 5.2 (Cycles). The entrance is built by tools/decokit/civic_records.py with only its globe swapped "
    "(make/globe_variants.py), with the game's Records Office model behind it. The light is the play light in light.json. Chrome mirrors a "
    "probe-like sky and paving rather than its neighbours, as the old town's zone probe does. The play grade's saturation and contrast are "
    "applied. Nothing was exported; the kit, the world and Unity weren't touched; $0.",
    "Files: briefs/103/vela/globe/make/ (render.py, globe_variants.py, grade.py, board.py, board_text.py). Full-size renders are in "
    "out/scratch/vela/globe/renders/.",
]
