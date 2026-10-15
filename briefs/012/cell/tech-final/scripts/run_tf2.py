"""Lineup and HUD portrait candidates on Codex. Usage: run_tf2.py lineup-1 portrait-1 ..."""
import sys, concurrent.futures as cf
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/012/cell/scripts")
from lib import *
from tf_prompts import *
COS = DEL / "round3/cos-tech.jpg"
A = DEL / "A"
def job(j):
    kind, n = j.split("-")
    if kind == "lineup":
        return codex(f"TF-lineup-white-v{n}", "tech-final", lineup(), 1536, 1024, refs=[A / "A-codex-lineup-white.png", COS],
                     refnotes="image 1 = the current A white team lineup; image 2 = the Tech's round 3 cosplay build sheet (face, hair, build and the whole new outfit).", setname="TF", hook="team lineup, Tech swapped", keep_png=True, frame=False)
    if kind == "portrait":
        return codex(f"TF-portrait-tech-white-v{n}", "tech-final", portrait(), 1024, 1536, refs=[COS, A / "A-codex-portrait-tech-white.png", A / "A-codex-portrait-fixer-white.png"],
                     refnotes="image 1 = the Tech's round 3 cosplay build sheet (face and outfit); image 2 = the existing HUD portrait of the Tech; image 3 = the existing HUD portrait of the Fixer (framing and finish).", setname="TF", hook="Tech HUD portrait, new outfit", keep_png=True, frame=False)
if __name__ == "__main__":
    with cf.ThreadPoolExecutor(4) as ex: list(ex.map(job, sys.argv[1:]))
