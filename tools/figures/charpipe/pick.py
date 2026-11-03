"""Role rules -> one spec per person (brief 069, proof). Pure python, no bpy, no random module: every pick is sha256(person id / key), so the same id gives
the same person on any machine, in any order, on any run.
  python3 -I tools/figures/charpipe/pick.py <out specs.json> [id=role ...]     (no ids: the proof cast from roles.json proofCast)
"""
import hashlib, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
WARD = json.load(open(os.path.join(HERE, "wardrobe.json")))


def u(pid, key):
    """A uniform number in [0, 1) from the person's id and what is being picked."""
    return int.from_bytes(hashlib.sha256(f"{pid}/{key}".encode()).digest()[:8], "big") / 2.0 ** 64


def weighted(pid, key, options, sex):
    """options: {name: weight | {m: w, f: w}} in file order; returns the picked name."""
    items = []
    for k, w in options.items():
        if isinstance(w, dict): w = w.get(sex, 0.0)
        if w > 0: items.append((k, float(w)))
    total = sum(w for _, w in items); x = u(pid, key) * total
    for k, w in items:
        if x < w: return k
        x -= w
    return items[-1][0]


def rng(pid, key, lo_hi, sex="m"):
    if isinstance(lo_hi, dict): lo_hi = lo_hi[sex]
    lo, hi = lo_hi
    return round(lo + (hi - lo) * u(pid, key), 4)


def colour_from(pid, key, src, sets, picked, avoid=()):
    """src: a set name, a list of colour names, or {same:X | colour: weight}."""
    if isinstance(src, dict):
        ch = weighted(pid, key + "/src", src, "m")
        return picked[ch[5:]] if ch.startswith("same:") else ch
    pool = list(sets[src]) if isinstance(src, str) else list(src)
    keep = [c for c in pool if c not in avoid] or pool
    return keep[int(u(pid, key) * len(keep)) % len(keep)]


def person(pid, role_name, roles, sets):
    r = roles[role_name]; b = r["body"]
    sex = weighted(pid, "sex", b["sex"], "m")
    band = weighted(pid, "ageband", {k: v[2] for k, v in b["age"].items()}, sex)
    macros = {"gender": 1.0 if sex == "m" else 0.0, "age": rng(pid, "age", b["age"][band][:2]), "muscle": rng(pid, "muscle", b["muscle"], sex),
              "weight": rng(pid, "weight", b["weight"], sex), "height": rng(pid, "height", b["height"], sex), "proportions": rng(pid, "proportions", b["proportions"], sex)}
    slots = {s: weighted(pid, "slot/" + s, opts, sex) for s, opts in r["slots"].items()}
    if slots.get("top") == "dress_shift": slots["legs"] = "none"          # the dress covers the legs slot
    c = r["colours"]; picked = {}
    picked["S"] = colour_from(pid, "col/S", c["S"], sets, picked)
    hair_set = "hair_old" if band == "old" else "hair_young"
    picked["HR"] = colour_from(pid, "col/HR", hair_set if c["HR"] == "hair" else c["HR"], sets, picked)
    picked["M"] = colour_from(pid, "col/M", c["M"], sets, picked)
    picked["B"] = colour_from(pid, "col/B", c["B"], sets, picked, avoid=(picked["M"],))
    picked["T"] = colour_from(pid, "col/T", c["T"], sets, picked, avoid=(picked["M"], picked["B"]))
    # brass is a thin line only (GDD §15): it may sit on a hem band (TL), never on painted trim (T), which is a body-face strip 3-8 cm wide
    picked["TL"] = colour_from(pid, "col/TL", c["TL"], sets, picked, avoid=(picked["M"], picked["B"])) if "TL" in c else picked["T"]
    if picked["T"] == "brass_line": picked["T"] = "chrome"
    picked["BT"] = colour_from(pid, "col/BT", c["BT"], sets, picked)
    picked["H"] = colour_from(pid, "col/H", c["H"], sets, picked, avoid=(picked["HR"],))
    picked["L"] = colour_from(pid, "col/L", c["L"], sets, picked)
    picked["LT"] = colour_from(pid, "col/LT", c["LT"], sets, picked)
    picked["G"] = colour_from(pid, "col/G", c["G"], sets, picked)
    picked["E"] = colour_from(pid, "col/E", c["E"], sets, picked)
    for k in ("D", "AP", "BG"):
        if k in c: picked[k] = colour_from(pid, "col/" + k, c[k], sets, picked, avoid=(picked["M"],) if k == "BG" else ())
    parts = [v for s, v in slots.items() if s != "gloves" and v not in ("none", "buzz")]
    gone = {x for v in parts for x in WARD["parts"].get(v, {}).get("replaces", [])}     # a hood replaces a puff or a bob under it
    parts = [v for v in parts if v not in gone]
    # every figure carries a dark step (Glyph's read): a part with an ink band (D), or else ink boots
    if not set(parts) & {"top_tunic", "coat_car", "guard_yoke", "duty_belt", "over_tabard"} and picked["B"] != "ink_black": picked["BT"] = "ink_black"
    base = ["body", "boots", "eyes"] + (["gloves"] if slots.get("gloves") == "gloves" else [])
    return {"id": pid, "role": role_name, "sex": sex, "ageBand": band, "macros": macros, "slots": slots, "base": base, "parts": parts,
            "buzz": slots["hair"] == "buzz", "colours": dict(sorted(picked.items()))}


def cast(roles_doc):
    out = []
    for role, n in roles_doc["proofCast"].items():
        out += [(f"cp-{role}-{i:02d}", role) for i in range(1, n + 1)]
    return out


def main():
    a = sys.argv[1:]
    roles_doc = json.load(open(os.path.join(HERE, "roles.json")))
    pal = json.load(open(os.path.join(HERE, "palettes.json")))
    people = [tuple(x.split("=", 1)) for x in a[1:]] or cast(roles_doc)
    specs = [person(pid, role, roles_doc["roles"], pal["sets"]) for pid, role in people]
    for s in specs:
        for k, v in s["colours"].items():
            if v not in pal["colours"]: raise SystemExit(f"{s['id']}: colour {k}={v} not in palettes.json")
    with open(a[0], "w") as f: json.dump({"schema": "suborned.charpipe.specs/1", "people": specs}, f, indent=1, sort_keys=True)
    print("PICKED", len(specs))


if __name__ == "__main__":
    main()
