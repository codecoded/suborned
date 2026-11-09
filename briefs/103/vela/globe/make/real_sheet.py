"""Brief 103 (Vela, 2026-11-10): the real pieces before and after, on the board's page (board.py's Board, grade, crop and scale: 54 px a metre, the play size at 1080p).

python3 briefs/103/vela/globe/make/real_sheet.py      (after real.py; reads out/scratch/vela/globe/real, writes briefs/103/vela/globe/real-piece.png)"""
import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(REPO / "briefs/103/vela/globe/make"))
import board as B  # noqa: E402

B.R = REPO / "out/scratch/vela/globe/real"                 # the board's helpers read these renders and masks (real.py's)
OUT = REPO / "briefs/103/vela/globe/real-piece.png"
DOORS = {210: "42.5 %", 300: "59.4 %"}                      # measured from the masks (real.py), before and after alike


def main():
    b = B.Board(6000)
    b.caps("Brief 103 · the Office's ball, built · Vela · 10 Nov 2026")
    b.text("The real piece: Sky's pick A, satin nickel, finials too", size=29, face="Bold", lead=1.15, gap=8)
    b.text("RecordsEntrance as the generator now builds it, beside the piece as it was. Same rig, light and grade as the board. "
           "Shown at the play size at 1080p. The door, landing, steps, piers, marquee, blades and banners are unchanged.", size=15.5, col=B.SOFT)
    b.rule()
    for yaw, cap in ((210, "Yaw 210, the play heading"), (300, "Yaw 300, a quarter turn: the front almost face-on")):
        b.head(f"Yaw {yaw}", cap)
        b.pics([(B.scaled(B.crop(B.render(f"entrance-{t}-y{yaw:03d}.png"), yaw)), f"{lab} · the door shows {DOORS[yaw]}")
                for t, lab in (("before", "Before: the chrome relief globe"), ("after", "After: the ball on its three columns, satin nickel"))])
        b.rule()
    b.head("The official front", "Its two marquee finials take the same satin nickel",
           "RecordsEntrance_Official shares the marquee and its corner finials, so the two fronts match. It has no globe and no pier balls. "
           "Shown on a plain wall: the tower isn't placed for it here.")
    ims = [B.scaled(B.render(f"official-{t}-y210.png")) for t in ("before", "after")]
    b.pics([(ims[0], "Before: chrome"), (ims[1], "After: satin nickel")])
    b.rule()
    for t in ("The after renders match the board's A with the finials in satin nickel to 3/255 at most; the before renders match the board's today to 1/255.",
              "Renders: out/scratch/vela/globe/real/ (make/real.py on the board's rig, make/real_sheet.py)."):
        b.text(t, size=13.5, col=B.SOFT, lead=1.34, gap=5)
    im = b.done()
    im.save(OUT)
    print(f"[103] sheet {OUT} {im.size}")


if __name__ == "__main__":
    main()
