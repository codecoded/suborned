"""Runner: metrics for every variant and pose; with --render also the image tiles. Blender: -b --python tf_run.py -- <outdir> [--render] [--variants AD,FX]"""
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from tf_proxy import *
import tf_proxy as tp
if __name__ == "__main__":
    P = dict(P_DEFAULT)
    pj = os.path.join(OUT, "params.json")
    if os.path.exists(pj): P.update(json.load(open(pj)))
    res = run_all(P)
    print(table(res))
    clean = {vn: {pn: {k: v for k, v in d.items() if not k.startswith("_")} for pn, d in poses.items()} for vn, poses in res.items()}
    json.dump(dict(params=P, results=clean, variants={k: v["label"] for k, v in VARIANTS.items()}), open(os.path.join(OUT, "metrics.json"), "w"), indent=1)
    npz = {}
    for vn in res:
        for pn in res[vn]:
            for sd in SIDES: npz[f"{vn}|{pn}|{sd}"] = res[vn][pn]["_V"][sd]
    for sd in SIDES: npz[f"faces|{sd}"] = res["AD"]["stand"]["_legs"][sd]["faces"]
    np.savez_compressed(os.path.join(OUT, "verts.npz"), **npz)
    if RENDER:
        vs = [a.split("=")[1] for a in argv if a.startswith("--variants=")]
        vs = vs[0].split(",") if vs else list(VARIANTS)
        os.makedirs(os.path.join(OUT, "tiles"), exist_ok=True)
        for vn in vs:
            for pn, spec in POSES.items():
                for kind in ("front", "side", "game"):
                    render_pose(res[vn][pn], kind, os.path.join(OUT, "tiles", f"{vn}-{pn}-{kind}.png"), json.loads(json.dumps(spec)))
        print("rendered", vs)
