"""The round 4 board's words (brief 101, Glyph). Plain, short, the player's words; the figures come from make/coverage.json."""
CAP = 'brief 101 · round 4 · glyph · mock'
TITLE = 'The docked line, the last chrome, the wrap'
SUB = ("Real 0.2.290 frames with the new pieces drawn in the built street kit and its five rules. Mock only: nothing in the game changed, "
       "nothing was committed, $0. Coverage is tools/hud-budget's own reading of each frame.")

WRAP_HEAD = 'Long lines: one row, or two rows at 460 px?'
WRAP_WHY = ("A conversation's lines are full sentences of up to 25 words (Sky, 5 Nov). Today a bubble never wraps, so the Fixer's 21-word line "
            "is one row 712 px wide, more than a third of the screen. B wraps a bubble at 460 px into at most two rows. Where it can, it breaks "
            "after a comma or a full stop.")
WRAP_A = 'A · one row (today)'
WRAP_B = 'B · two rows at 460'
WRAP_A_CLAMPED = "A · one row (today's, clamped)"
CALL_NOTE = ("Today this line runs off the screen. Both options are drawn clamped (Latch's fix 5) and 10 px clear of "
             "the Scout's face at the edge, which the clamp alone still covers.")
WRAP_POINTS = [
    'Screen share: the same, within 0.12 points.',
    "Reading: A's row runs to about 100 characters. B's rows are about 55, a length you take in at a glance.",
    "Placement: at the screen's edge, A has to slide {a_off} px off the Fixer and B {b_off} px.",
    "Under B, most conversation lines take two rows ({two} of the {all} in text.json). Barks stay on one row ({bark2} of {barks:,} take two).",
    "Two lines (123 and 128 characters) need more than 460 px on two rows. Their bubbles widen to 485 and 490 px; a bubble never takes a third row.",
]
WRAP_PICK = 'My pick: B. It costs no screen and reads faster. It is a design change, so it waits for Sky.'

DOCK_HEAD = 'A line said off screen: the street bubble at the docked face'
DOCK_WHY = ("Today the line beside a docked face is the old bevelled plate with a diamond tail. It is drawn outside the pool of two bubbles, "
            "so a frame can show three lines: 4:15 here.")
DOCK_RULES = [
    ("The look", "The street bubble, one row like the rest (or two, if Sky picks B), with no face of its own: the docked face is the speaker. "
                 "A short stem joins it to the face. It sits level with the face, on the side toward the screen's centre."),
    ("Two at once", "It counts toward the two lines on screen, as a bubble at its speaker does."),
    ("Which gives way", "When a third line would show, the oldest gives way, wherever it is drawn. A docked line that gives way takes its face with it. "
                        "The Journal keeps every line."),
]
DOCK_ORDER = ("Which line is oldest at 4:15 is my reading of the run (the call's line, then the rivals, then the guard). The build knows each line's age.")
INSET_NOTE = ("Inez's line at the left edge, 2:39: today two rows in the plate; as the street bubble it fits one row. It keeps 10 px off the "
              "scope's eye button by sliding up 7 px.")

CHROME_HEAD = 'The verb plate, the armed plate and the strip become one row of tabs'
CHROME_WHY = ("The Action plate (a pause-menu setting) and the strip you get on Tab or Shift show the same thing: the selected agent's verbs. "
              "In the street kit they are one piece: a round tab for each verb, after the take-control tab, level with the face.")
CHROME_POINTS = [
    "Every tab takes a click. That arms the verb, as a knob does today.",
    "No Shift 1 to Shift 4 under the tabs (Sky's no-legend rule). Hovered, a tab shows its verb and then its key in the soft ink, as the take-control tab does.",
    "Armed: the book's deep teal with the turquoise ring. Its name under it, as a street label, replaces the 'Knock out' plate.",
    "The fold tab goes. The row is about a quarter of the plate's size, and the setting turns it off.",
    "With the plate off, the row shows for 2.5 s after Tab or Shift, as the strip does, and takes clicks while it shows.",
    "At the cursor, the armed verb is its keyed pictogram, with no disc, because a disc means a click.",
    "The outcome marks of the Fixer's turn ways sit at a tab's lower right on the cream badge disc, as on a face.",
]
STATES = ['at rest', 'hovered: its name, then its key', 'armed: deep teal, its name under it', 'shut: Fire while carrying Popovic',
          "the Fixer's turn ways over a person, with their outcome badges (illustrative)"]

COV_HEAD = 'Each frame, today and in round 4'
COV_NOTE = ("% of the 1920 × 1080 screen, as tools/hud-budget reads each frame. The Alt frame fails today and in the mock for the same "
            "reason: the hold pill shows while Alt is held. That isn't one of this round's pieces.")
