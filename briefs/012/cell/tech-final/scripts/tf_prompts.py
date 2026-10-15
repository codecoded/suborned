"""Prompts for the Tech final (brief 012, Forma, 2026-10-16): the round 3 cosplay Tech (cos-tech.jpg) in motion, plus the HUD portrait and the lineup.
People prompts carry no date. Every pose prompt is used verbatim by the Codex image tool (lib.codex adds one margin sentence)."""

OUTFIT = ("the same person as in the reference image, exactly his face, short locs, beard, tall muscular build, and exactly the same outfit: "
          "the sleeveless charcoal wrap vest with bone-white diagonal panels and thin pale-teal piping; the smoked visor with two flip-up chrome plates pushed up on his forehead; "
          "chrome gauntlets over black fingerless gloves; the wide split panelled trousers, pleated charcoal wide legs ending at mid-shin, each with a long bone-white diagonal panel on the outside of the leg "
          "that flares out toward the hem and thin pale-teal piping down the leg; black technical high-top trainers with bone-white soles, dark socks.")

HONEST = ("Draw the trousers as a motion test for a game animator, as heavy matte technical fabric under real gravity: the panels swing with the legs, the hems follow the shins, "
          "cloth folds and bunches where the legs bend, two layers of cloth lie against each other and never pass through each other or through a leg. Do not hide the legs or tidy the trousers: "
          "show exactly where every panel, pleat and hem goes. ")

FINISH = ("Polished matte digital character concept art, soft even studio light, crisp clean edges, plain warm light-grey seamless backdrop with a soft ground shadow, "
          "same finish as the reference. No text, no logos, no watermark, no numbers.")

LAYOUT = ("One landscape sheet with two views of the same figure in the same pose, side by side, both full body head to toe on the same ground line: "
          "on the left a pure side view (profile), on the right a three-quarter front view from slightly above. ")


def pose(text, extra=""):
    return f"Animation motion test, one pose. {LAYOUT}{OUTFIT} Pose: {text} {extra}{HONEST}{FINISH}"


POSES = {
    "walk": pose("walking, mid-stride, the moment both feet are on the ground: front leg straight with the heel down, rear leg pushing off on the toe, arms swinging opposite to the legs, relaxed upright posture."),
    "run": pose("running at a sprint, mid-stride in the air: the front knee driven high, the rear leg kicked back with the shin up, the torso leaning forward, elbows bent at a right angle and arms pumping, both feet off the ground."),
    "crouch": pose("crouched low in front of a small waist-high cream enamel junction box with chrome hinges and its door open, working on the wiring inside with both hands and a small chrome tool: a deep crouch, knees bent far and apart, heels near the ground, torso leaning forward, looking into the box.",
                   "The junction box stands in both views, on the pavement. "),
    "fight": pose("a braced shooting stance with a small chrome pistol held in both hands at arm's length: feet wide apart in a staggered stance, the front knee bent, the rear leg straight, weight forward, shoulders turned, head level.",
                  "The pistol is plain and unlit. "),
    "kerb": pose("stepping up onto a kerb: a low pale granite kerb about 18 cm high, a road surface below it and a paved pavement above. The lead foot is planted on the pavement with the knee raised and bent, the trailing foot still on the road with the heel lifting, the torso leaning slightly forward, arms loose.",
                 "The kerb runs across both views. "),
}

STRIP_TAIL = ("The same person in every frame, the same scale, all feet on one shared ground line, evenly spaced, never overlapping, a plain warm light-grey backdrop, no text and no numbers. ")


def strip(kind, view):
    cyc = {"walk": "one walk cycle in four key poses, left to right: (1) contact, the right foot forward with the heel down and the left toe pushing off; (2) passing, the left leg swinging through beside the standing right leg with the left knee bent; (3) contact, the left foot forward with the heel down and the right toe pushing off; (4) passing, the right leg swinging through beside the standing left leg with the right knee bent. Arms swing opposite to the legs, upright relaxed posture.",
           "run": "one run cycle in four key poses, left to right: (1) contact, the right foot landing under the body with the left leg kicked back; (2) the right leg pushing off the ground, the left knee driven high; (3) flight, both feet off the ground, the left knee high and the right leg extended behind; (4) the left foot landing with the right knee driven high. Torso leaning forward, elbows bent at a right angle, arms pumping opposite to the legs."}[kind]
    v = {"side": "pure side view (profile), all four facing the same way (to the right of the sheet)",
         "front": "three-quarter front view from slightly above, all four coming toward the viewer"}[view]
    return (f"Animation key-pose strip for a game animator. A wide landscape sheet with four full-body figures in a row, {v}. {OUTFIT} The four frames show {cyc} {HONEST}{STRIP_TAIL}{FINISH}")


# ---- HUD portrait + lineup (A white set; Tech swapped for the round 3 cosplay Tech) ----
import sys as _s
_s.path.insert(0, "/Users/sky/Projects/suborned/briefs/012/cell/scripts")
from a_prompts import FINISH as A_FINISH

NEW_TECH = ("the Tech, a tall muscular man with short locs and a short beard, in his new outfit: the sleeveless charcoal wrap vest with bone-white diagonal panels and thin pale-teal piping, "
            "the smoked visor with two flip-up chrome plates pushed up on his forehead, chrome gauntlets over black fingerless gloves, and the wide split panelled charcoal trousers ending mid-shin "
            "with a long bone-white diagonal panel on the outside of each leg and thin pale-teal piping, black technical high-top trainers with bone-white soles")


def lineup():
    return ("Character concept art, a 4-person team lineup, full body head to toe, the four standing together in one row, left to right Scout, Fixer, Tech, Enforcer, same scale with their natural height differences, gentle three-quarter views. "
            "Image 1 is the current team lineup: keep the Scout, the Fixer and the Enforcer exactly as they are in image 1: same faces, hair, clothes, props, poses, positions, scale and finish. "
            f"Replace only the third person with the Tech from image 2 in his new outfit ({NEW_TECH}). He keeps exactly his face, locs, beard, skin tone and tall build from image 2, "
            "and stands relaxed in a gentle three-quarter view with his weight on one leg, in the same place in the row. The team stays white-on-charcoal: charcoal and ink black with bone-white panels, chrome and matte metal; "
            "the only colour anywhere is the Tech's thin pale-teal piping. The clothes stay sharp, fitted, matte and technical, no deco motifs, no period tailoring. " + A_FINISH)


def portrait():
    return ("Character portrait, head and shoulders, one person facing the viewer in a gentle three-quarter view, for a game interface portrait. the Tech, a tall man with short locs, redrawn from image 1: "
            "keep exactly the face, hair, beard, skin tone, age and build of the person in image 1, and his outfit from image 1: the sleeveless charcoal wrap vest with a bone-white diagonal panel across the chest and thin pale-teal piping, a chrome gauntlet at the forearm. "
            "Expression: relaxed and amused, chin slightly raised, the smoked visor with its two flip-up chrome plates pushed up on his forehead so his eyes are visible. "
            "Image 2 is the existing HUD portrait of the Tech and image 3 the existing portrait of the Fixer: copy their exact framing, scale of the head in the frame, camera distance, soft light, grey backdrop and painted finish, so the new portrait sits in the same row as the other three. "
            "The palette is charcoal and ink black with bone-white panels, chrome and matte metal; the only colour is the thin pale-teal piping. The clothes stay sharp, fitted, matte and technical, no deco motifs, no period tailoring. " + A_FINISH)


def change():
    return ("Costume revision sheet, one plain warm light-grey landscape sheet, no text, no numbers. The same person as in the reference image, exactly his face, locs, beard, build and the same outfit, shown full body head to toe in three views side by side: "
            "FRONT, BACK, and a three-quarter front view, all standing relaxed with his feet only a hand's width apart (not in an A-pose). ONE change only to the trousers: the inner side of each trouser leg now hangs straight down from the knee to the hem, "
            "so the two inner hem edges are two clean vertical lines with a clear gap between the legs, and the hem width on the inner side is about a quarter narrower than in the reference; the outer side of each leg keeps its wide flare and its long bone-white diagonal panel and teal piping exactly as in the reference, "
            "and the hem stays at mid-shin at the same height. Everything else is identical to the reference: the sleeveless charcoal wrap vest with bone diagonal panels and pale-teal piping, the visor with flip plates pushed up on the forehead, chrome gauntlets, black high-top trainers. "
            + FINISH)
