"""Set D: the same 6 prompts on Qwen Turbo and Codex, side by side. Prompts reused verbatim from B and C."""
import b_data, c_data
from b_data import ROLES

def _bp(role, i): return b_data.prompt(role, ROLES[role][i])
PORTRAIT_HEAD = ("Character portrait, head and shoulders, one person facing the viewer in a gentle three-quarter view, plain warm light-grey studio backdrop, soft even studio light. ")
_p = ROLES["fixer"][0]
D = [
 dict(id="D1-scout-surveyor", w=1024, h=1536, role="scout", hook=ROLES["scout"][0]["hook"], prompt=_bp("scout", 0)),
 dict(id="D2-fixer-switchboard", w=1024, h=1536, role="fixer", hook=ROLES["fixer"][8]["hook"], prompt=_bp("fixer", 8)),
 dict(id="D3-tech-lift-engineer", w=1024, h=1536, role="tech", hook=ROLES["tech"][2]["hook"], prompt=_bp("tech", 2)),
 dict(id="D4-enforcer-ring-champion", w=1024, h=1536, role="enforcer", hook=ROLES["enforcer"][1]["hook"], prompt=_bp("enforcer", 1)),
 dict(id="D5-lineup-seafoam-and-bone", w=1536, h=1024, role="team", hook=[t for t in c_data.TEAMS if t["id"] == "seafoam-and-bone"][0]["hook"],
      prompt=c_data.prompt([t for t in c_data.TEAMS if t["id"] == "seafoam-and-bone"][0])),
 dict(id="D6-fixer-clerk-portrait", w=1024, h=1280, role="fixer", hook=_p["hook"],
      prompt="Close-cropped character portrait for a game interface: head and shoulders only, the frame cut at the chest, one person facing the viewer in a gentle three-quarter view, plain warm light-grey studio backdrop, soft even studio light. "
             f"{_p['who']}, the Fixer of a small crew. Personality: {_p['hook']} Face: {_p['face']}, reading glasses pushed up into her hair. Clothes: a fitted column-cut jacket with a sharp stand collar over a high-collar blouse layer. "
             f"Colour: dark matte base with {_p['accent']} as the only accent. " + b_data.LINEAGE.replace("Bare open face", "Bare open face") + b_data.FIN),
]
