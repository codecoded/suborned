"""Brief 104 (Vela): the dot-matrix face each kit robot shows when calm, as a texture the render puts on its screen.
python3 -I briefs/104/vela/robots/make/faces.py <out_dir>

Same data and drawing as brief 031 (tools/decokit/compose031face.py, robot_face_stage.py): the old robots keep their own LED colour and their rig's
neutral glyphs (robot-faces.json, stage 0 "calm"); the patrol robot shows its 16 x 10 calm face in seafoam phosphor. Render-only: in the game
the faces are drawn by the engine's shader from the same tables."""

import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(REPO / "tools/decokit"))
import compose031face as C  # noqa: E402
import robot_face_stage as RF  # noqa: E402

OUT = Path(sys.argv[1])
OUT.mkdir(parents=True, exist_ok=True)
for rb in ("RobotButler", "RobotPorter", "RobotWaiter", "RobotNanny", "RobotShopkeeper"):
    own = C.OWN[C.DATA["robots"][rb]["face"]]
    C.dots_any(C.old_grid(rb, 0), own).save(OUT / f"{rb}.png")
RF.dots(RF.lit(0), 24, RF.HEX["phosphor"]).save(OUT / "PatrolRobot.png")
print(f"[faces] 6 calm faces -> {OUT}")
