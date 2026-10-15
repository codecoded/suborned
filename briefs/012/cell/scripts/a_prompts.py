"""Set A prompts: the four locked picks (Sky 2026-10-06) redrawn in one finish with three accents."""
from lib import PICK

FINISH = ("One shared finish for everyone: polished matte digital character concept art, soft even studio light, crisp clean edges, natural skin "
          "and matte technical fabric with a subtle painted texture, plain warm light-grey seamless backdrop with a soft ground shadow. "
          "No text, no logos, no watermark.")

ACCENT = {
    "teal": ("deep teal-black accent",
             "every violet panel, stripe and lining becomes deep teal-black (a very dark blue-green, almost black); ink black and a cool off-white stay as the base; no other accent colour"),
    "sienna": ("deep sienna-brown accent",
               "every violet panel, stripe and lining becomes deep sienna brown (#74483a: a dark, muted clay brown like terracotta in shade, clearly darker and browner than any red or orange, not bright); ink black and a cool off-white stay as the base; no other accent colour"),
    "white": ("white-on-charcoal",
              "the palette is charcoal and ink black with bone-white panels; every violet panel, stripe and lining becomes bone white; there is no colour accent at all, only charcoal, bone white, chrome and matte metal"),
}
FIXES = ("Changes from the references: all gold trim, buckles and rings become brushed chrome or matte gunmetal, never gold or brass; "
         "{acc}; the Tech's glowing cyan strips and glowing visor become a dark smoked visor that is unlit (calm, no neon glow, no glowing strips; at most one hairline of soft light); "
         "the Enforcer's amber glasses become smoked neutral grey lenses (amber is a reserved signal colour, so nothing in the design is amber, orange or yellow); the Scout's and Enforcer's gold trim is chrome. The clothes stay sharp, fitted, matte and technical, with no deco motifs and no period tailoring.")
KEEP = ("Keep exactly each person's face, hair, skin tone, body build, height, pose, props and silhouette from their reference image: "
        "the Scout (young woman, black undercut hair, earpiece, backpack and harness, shorts, trainers), "
        "the Fixer (woman with a long black ponytail, long slim column dress, boots, silver briefcase, turning her head over her shoulder), "
        "the Tech (tall man with short locs, visor, sleeveless vest with a shoulder plate, wrist bands, cargo shorts, sandals), "
        "the Enforcer (big older man with silver hair and beard, glasses, long layered coat, boots, carrying a long rifle).")

REFNOTE = ("image 1 = the Scout, image 2 = the Fixer, image 3 = the Tech, image 4 = the Enforcer.")


def lineup(acc):
    name, rule = ACCENT[acc]
    return (f"Character concept art, a 4-person team lineup, full body head to toe, the four standing together in one row, left to right Scout, Fixer, Tech, Enforcer, "
            f"same scale with their natural height differences, gentle three-quarter views. Redraw the four people from the reference images as one coherent team ({name}). {KEEP} "
            f"{FIXES.format(acc=rule)} {FINISH}")


ROLE = {
    "scout": ("the Scout, a young woman with a black undercut", "image 1"),
    "fixer": ("the Fixer, a woman with a long black ponytail", "image 2"),
    "tech": ("the Tech, a tall man with short locs", "image 3"),
    "enforcer": ("the Enforcer, a big older man with silver hair and beard", "image 4"),
}
POSE = {
    "scout": "alert, attentive, a half smile, eyes scanning to one side, earpiece visible, backpack strap and harness at the shoulder",
    "fixer": "poised and composed, face turned toward the viewer with a calm direct gaze, long ponytail, a hint of a knowing smile",
    "tech": "relaxed and amused, chin slightly raised, the smoked visor pushed up on his forehead so his eyes are visible, wrist bands at the forearm",
    "enforcer": "steady and weathered, a calm level gaze, smoked neutral-grey glasses pushed up into his silver hair so his eyes are visible, close-trimmed silver beard",
}


def portrait(role, acc):
    name, rule = ACCENT[acc]
    who, im = ROLE[role]
    return (f"Character portrait, head and shoulders, one person facing the viewer in a gentle three-quarter view, for a game interface portrait. {who}, redrawn from the reference: "
            f"keep exactly the face, hair, skin tone, age and build of the person in the first reference image. Expression: {POSE[role]}. "
            f"The second reference image is the team lineup in the accent to use: copy its outfit colours and materials for this person ({name}). "
            f"All gold trim is brushed chrome or matte gunmetal, no gold; {rule}. The clothes stay sharp, fitted, matte and technical, no deco motifs, no period tailoring. {FINISH}")


# Qwen Turbo takes the reference but follows edits less strictly than Codex, so the edits are named per person.
QEDIT = {
    "scout": "all gold or brass (trim, buckles, shoe details, ankle straps, rings) becomes brushed chrome or matte gunmetal; every violet or teal panel becomes {a}",
    "fixer": "every violet pleat, panel and lining becomes {a}; all gold or brass becomes brushed chrome; her silver briefcase stays",
    "tech": "remove every glowing cyan strip and all glow: the wrist bands, vest seams, trousers seams and sandal soles become matte charcoal bands with no light; the glowing cyan visor becomes a dark smoked visor that is unlit; any trim is {a}",
    "enforcer": "every violet part including the stripe on the rifle becomes {a}; all gold or brass becomes brushed chrome; the amber glasses become smoked neutral grey lenses; nothing is amber, orange or yellow",
}


def qfull(role, acc):
    name, rule = ACCENT[acc]
    a = rule.split("becomes ")[1].split(" (")[0].split(";")[0] if "becomes" in rule else "bone white"
    return (f"Character concept art, full body head to toe, one person standing. Redraw the person in the reference image exactly: same face, hair, skin tone, body build, pose, silhouette and props. "
            f"Edits: {QEDIT[role].format(a=a)}. Accent scheme: {name}. Clothes stay sharp, fitted and matte. {FINISH}")


def qportrait(role, acc):
    name, rule = ACCENT[acc]
    a = rule.split("becomes ")[1].split(" (")[0].split(";")[0] if "becomes" in rule else "bone white"
    return (f"Character portrait, head and shoulders, one person in a gentle three-quarter view, for a game interface. Redraw the person in the reference image exactly: same face, hair, skin tone and age. "
            f"Expression: {POSE[role]}. Edits: {QEDIT[role].format(a=a)}. Accent scheme: {name}. {FINISH}")


def qlineup(acc):
    name, rule = ACCENT[acc]
    a = rule.split("becomes ")[1].split(" (")[0].split(";")[0] if "becomes" in rule else "bone white"
    return (f"Character concept art, a four-person team lineup, full body head to toe, standing together in one row, left to right as in the reference images (Scout, Fixer, Tech, Enforcer). Redraw exactly the four people from the reference images: same faces, hair, builds, poses, props and silhouettes. "
            f"Edits for all four: all gold or brass becomes brushed chrome or matte gunmetal, including shoes; every violet part becomes {a}; the Tech has no glowing cyan strips and no glow anywhere, his visor is dark smoked and unlit; the Enforcer's amber glasses become smoked neutral grey; nothing amber, orange or yellow. "
            f"Accent scheme: {name}. {FINISH}")
