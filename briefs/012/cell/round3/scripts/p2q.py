"""Round 3, part 2 wildcard (Qwen Turbo, portrait as reference): a sixth, bolder take per person."""
from lib import *
import p2
W = {
 "scout": ("zip-line", "Zip-line courier", "a cropped hooded jacket, fitted shorts, bare legs, chunky trainers, a harness with chrome pulley loops", "a hood, a harness and a rope coil on one hip: the loop", "a chrome pulley handle clipped at the shoulder with a coil of fine line", "a single earpiece on a fern-green cord"),
 "fixer": ("sculpted-sleeve", "Sculpted-sleeve", "a long slim column dress with one huge sculpted sleeve and a bare other arm, flat boots", "one giant sleeve against a narrow column: the lopsided column", "her silver case with a coiled handset cable", "one long chrome pin at the collar"),
 "tech": ("clockwork-sleeve", "Clockwork forearm", "a sleeveless wrap-front top, wide trousers, flat boots, one bare arm and one arm in a chrome clockwork forearm brace", "an uneven pair of arms, one bare, one chrome: the odd arm", "his visor pushed further: a smoked visor with a rotating chrome lens turret, lit soft pale aqua only when working, and the chrome clockwork forearm brace", "the clockwork brace ticking"),
 "enforcer": ("long-duster", "Long duster gunman", "a floor-skimming heavy duster with a high collar, heavy boots, gloves", "a wide long duster with the rifle slung low at the hip: the low rifle", "the long chrome rifle slung low at the hip, its shroud inlaid with ink blue", "smoked glasses pushed up into his hair"),
}
for role, t in W.items():
    qwen(f"R3-2-{role}-6-{t[0]}-qwen", "2_icons", p2.prompt(role, t), 1024, 1536, refs=[PORT[role]], setname="R3-2",
         hook=t[1] + " (Qwen wildcard)", meta=dict(option=2, role=role, lineage=t[1], silhouette=t[3], signature=t[4], accent=p2.WHO[role][1], accent_name=p2.WHO[role][2], detail=t[5]), est=0.0145)
