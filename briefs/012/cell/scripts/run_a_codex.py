"""Set A on Codex: one worker per accent (argv[1]): team lineup first, then the four portraits (lineup is the second reference)."""
import sys
from lib import *
from a_prompts import *
acc = sys.argv[1]
ROLES4 = ("scout", "fixer", "tech", "enforcer")
AN = ACCENT[acc][0]
lid = f"A-codex-lineup-{acc}"
codex(lid, "A", lineup(acc), 1536, 1024, refs=[PICK[k] for k in ROLES4], refnotes=REFNOTE, setname="A", hook=f"the four picks, one finish, {AN}; Codex", keep_png=True)
lineup_png = RAW / f"{lid}.png"
for role in ROLES4:
    codex(f"A-codex-portrait-{role}-{acc}", "A", portrait(role, acc), 1024, 1536, refs=[PICK[role], lineup_png],
          refnotes="image 1 = the locked pick for this person (identity); image 2 = the team lineup in the accent to use (outfit colours and finish).",
          setname="A", hook=f"{role} head-and-shoulders portrait for the HUD, {AN}; Codex", keep_png=True, frame=False)
