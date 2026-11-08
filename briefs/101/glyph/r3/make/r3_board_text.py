"""The round 3 board's words (brief 101): kept apart from the drawing so they read as one text. Glyph."""

TITLE = 'The rest of the old kit'
CAP = 'brief 101 · the hud sweep · round 3'
SUB = ("Glyph · 9 November 2026 · mock only. Real frames of 0.2.286 and 0.2.287 with the old pieces lifted off and drawn again in the kit "
       "Latch built for steps 4 to 6. Nothing new to learn: the same labels, round tabs, rings and faces.")

RULES = [
    ('Words in the street are street labels.', 'Pops, plan ghosts, stash names, the hint line and the act ring\'s prices lose their plates, chrome and screws.'),
    ('What you click is a round tab, or a capsule if it has a word.', 'Door and car markers, the pips over people, the hint disc, edge chips and a chip\'s new ways.'),
    ('Time that runs is the tab\'s own cream ring.', 'It drains for a window and fills for someone\'s attention. In the last 10 s it turns orange, like the seal.'),
    ('Every face wears the built cream ring.', 'Chips, the callout, the shooters on a shot, plan ghosts and the slip.'),
    ('The bottom right holds only the news and the Book.', 'Nothing docks beside the lozenge.'),
]

INVENTORY = ("I went through 219 captures of 0.2.286 and 0.2.287 (the batch's shots and proofs, and Latch's round 4 frames): 281 manifests and their parts. "
             "These are the pieces still in the old kit (chrome plates, screws, brass hairlines, chrome rings):")
FAMILIES = [  # family, how many of the 219 captures show it, what it becomes
    ('Step markers (door, car, body, stand)', 83, 'round tabs'),
    ('Pips over people (guard, driver, Inez, Popovic)', 99, 'the badge in the cream ring'),
    ('Hint disc, hint line', 43, 'round tab, street label'),
    ('Edge chips (reroute, rival car, lock)', 31, 'round tab with the ink chevron'),
    ('Done chips', 21, 'face, labels, capsules'),
    ('Window markers', 19, 'timed tab and its seconds'),
    ('Attention callout (medallion)', 17, 'cream-ringed face, a stem'),
    ('Person cards after a conversation (Inez\'s)', 13, 'a slip on her, never docked'),
    ('Pops (refusals, Report stopped, Camera on)', 11, 'icon and words, no plate'),
    ('Attention clocks', 7, 'timed tab, the stage word'),
    ('Stack tab (••) over a crowded spot', 9, 'small capsule'),
    ('Plan ghosts, stash plates', 2, 'street labels'),
    ('Act ring price plates and chrome rim', 'each ring', 'street labels, cream rim'),
    ("Shooters' faces on the aim row", 'each aim', 'cream ring'),
]
ALREADY = ("Already in the street style: the state words ('done' beside a face, 'taking aim' over a target), the hover label, bubbles, the car's marks, "
           "the focus ring and the edge faces. Out of scope: the conversation scene's own frame.")

NEXT_HEAD = 'The next event gets its own limit'
NEXT_WHY = ("Today the budget reads the next event as a window marker, whose cap is two words (a lock and m:ss). So 'Popovic exits 5:30' "
            "fails as three words, though it's a different piece: a timeline mark under the stats row, read at a glance. Its form is right as built: "
            "the next-event icon, the name in SemiBold 15, the time in Bold 17, and a '?' in place of the name while the cell can't know it. It needs its own kind and cap.")
NEXT_LIMIT = [
    'Name: at most 3 words and 16 characters, spaces included. The time is not part of the name.',
    'Exactly one m:ss, at the end. One line, no full stop. One on screen.',
    'An unknown event shows its time alone (name: 0 words).',
]
NEXT_REASON = ("Why 16: the stats row above sets the clock stack's width: 251 px at the start, 231 px at $5,000. At 16 characters a name "
               "is about 115 px (139 px with many capitals), so the row is 184 to 207 px and sits inside the stack. The stack never widens or "
               "shifts as the next event changes. Only below $1,000 does the stats row shrink under it (193 px at $0). "
               "Why 3 words: you take three in at a glance. It sits between the lock pill's 2-word reason and the goal line's 6 words. "
               "All seven of the timeline's short names fit already (the longest is 14 characters).")
SPLICE = [
    'tools/hud-budget/budget.ts: a new kind `next` (Kind, TextKind, TEXT_KINDS), NEXT_MAX = { lines: 1, nameWords: 3, nameChars: 16, times: 1, onScreen: 1 }.',
    'Text `<name> <m:ss>` or `<m:ss>`. Name = the text minus the trailing clock. Count words with glyphWords and characters on the name as written.',
    'Faults: over 3 words · over 16 characters · not exactly one clock at the end · a full stop · more than one line · more than one on screen.',
    'Until the host says `next`, recognise() maps a `marker` that has letters before a trailing clock to `next`. Bare clocks stay markers.',
    'MARKER_MAX stays 2 words for the seal and the window markers. Tests: the 7 shorts pass; "Popovic leaves the Office 5:30" (4 words) and "Councillor Popovic exits 5:30" (24 characters) fail; two rows fail.',
]

CARD_HEAD = "Inez's card: on her, never beside the news"
CARD_WHY = ("The card is what a conversation taught you. It belongs to Inez, not to the corner. On screen it becomes a slip of the book's "
            "cream page on a fine stem to her, the same family as the bubbles. Her face breaks its corner. Each label word "
            "(Debt, Secret) becomes its icon, and Fable's line follows as written. The ways it opened are capsules you can click (Help, Pressure), with 'New' in "
            "the book's teal. Off screen it no longer docks bottom right. It waits for her, and her face in the key people top right "
            "carries the Book's new dot until it's read. Nothing is lost: her Intel page shows her need (the debt) and her secret once learned, "
            "and the ways it opened lead her ring with a lit pip for 90 s, as every newly opened act does.")
CARD_NUMS = ("r123's moment: {cb} % of the screen today, {ca} % without the docked card (the proof's 2-card fault goes too). "
             "On her: {po} % as a dark plate, {pn} % as the slip (type 15 px, as the bubbles).")

LINE_HEAD = 'The news line: 40 characters, confirmed. Keep the lozenge as it is'
LINE_WHY = ("Measured in the line's own type (Jost Medium 13) in its 300 px box. ' · 2:46' takes 40 px. Real why lines run 5.7 px a character, so "
            "40 characters is 217 to 236 px, leaving 24 to 43 px spare. 45 is the edge. Growing the lozenge to fit today's 67 characters "
            "takes 430 px, about 130 px more. The lozenge would then run to x 1310 and cover the faces docked on the bottom edge (one sits at x 1492 in the quiet frame). One short line reads faster "
            "anyway, and a click on it already pans to the place.")
LINE_FABLE = ("For Fable: the long ones say the place twice. A camera is named by where it is ('the square's mast camera' is 24 characters) and "
              "then the place follows ('beside the café'). Example in the mock (illustrative): 'a camera saw the Fixer stay too long · 2:46', 36 characters.")

LATCH_HEAD = 'The order for Latch'
LATCH = [  # (what, files, size)
    ("Inez's card: the slip on her. Off screen it waits, its read time starting when she's in view, and her key face carries the dot. Clears r123's 2-card fault.", 'HudToolkit.Card.cs', 'M'),
    ("The next row's manifest kind `next` (one word, line 57), once Splice's NEXT_MAX lands.", 'HudToolkit.Clock.cs', 'S'),
    ('Timed tabs: window markers (drain) and attention clocks (fill), orange for the last 10 s.', 'HudToolkit.Done.cs, Hold.cs', 'S'),
    ("Pops as street labels; a refusal wears the kit's mark_no badge; 'Report stopped' is the report's disc struck through.", 'HudToolkit.Less.cs (pops)', 'S'),
    ("Done chips: the face with its tick, one keyed icon and word a fact, the new ways as capsules (each a click).", 'HudToolkit.Done.cs', 'M'),
    ("The act ring: price rows as street labels, the rim cream; the struck badge for a shut way.", 'HudToolkit.Ring.cs', 'S'),
    ("The callout: a cream-ringed face (a camera: its red eye on the body), a stem, the stage ticks on the agent's badge.", 'HudToolkit.Attention.cs', 'S'),
    ("The sweep: step markers, pips, the hint disc and edge chips as round tabs; plan ghosts, stash names and the hint line as labels; the shooters' faces in cream. World labels keep out of the clock stack (a stash name flips under its point).", 'Less.cs, Hold.cs, StepMarks.cs, CuesEdge.cs', 'M'),
]
LATCH_NOTE = ("Each piece is shot and checked at 2x on its own. Run the delta only: the proof sections that show it (r059, r067, r083, r101, r123) "
              "and the shots, not a full batch. Every colour is already in street.uss: nothing new.")

OPEN = [
    "Fable: 'Take control: T' names a key, and Sky's rule is no key-only features. The pop should name the click, or go.",
    "Fable: the card's label words (Debt, Secret, Help, Pressure) become icons. The rest of each row stays as written. OK?",
    'Sky: the slip is the one piece of reading left in play. It shows for its read time, as today.',
]
