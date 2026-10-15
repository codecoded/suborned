"""Motion test + strips on Codex (free, ChatGPT plan). Usage: run_tf.py <job> ... ; jobs: pose-<name> strip-<walk|run>-<side|front>"""
import sys, concurrent.futures as cf
sys.path.insert(0, "/Users/sky/Projects/suborned/briefs/012/cell/scripts")
from lib import *
from tf_prompts import *
REF = DEL / "round3/cos-tech.jpg"
NOTE = ("the Tech's round 3 cosplay build sheet (front, back and detail views): the face, hair, build and the whole outfit to copy exactly.")
def job(j):
    kind, *rest = j.split("-")
    if kind == "pose":
        n = rest[0]; return codex(f"TF-pose-{n}", "tech-final", POSES[n], 1536, 1024, refs=[REF], refnotes="image 1 = " + NOTE, setname="TF", hook=f"motion test: {n}", tries=3, frame=True)
    if kind == "change":
        return codex("TF-change-inner-hem", "tech-final", change(), 1536, 1024, refs=[REF], refnotes="image 1 = " + NOTE, setname="TF", hook="the one proposed change to the trousers", tries=3, frame=True, keep_png=True)
    if kind == "strip":
        k, v = rest; return codex(f"TF-strip-{k}-{v}", "tech-final", strip(k, v), 1536, 1024, refs=[REF], refnotes="image 1 = " + NOTE, setname="TF", hook=f"{k} cycle strip, {v}", tries=3, frame=True)
if __name__ == "__main__":
    with cf.ThreadPoolExecutor(4) as ex: list(ex.map(job, sys.argv[1:]))
