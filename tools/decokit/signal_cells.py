"""The signal atlas's cell table (brief 061, Vela): constants and rects shared by signal_tex.py (draws the PNGs: numpy, Pillow) and crossing_lens.py (Blender, which has no Pillow). No imports."""

CELL, GRID, SS = 64, 4, 8                    # glyph px, cells a side, supersample (SS is signal_tex.py's)
ATLAS = CELL * GRID
FACE_M = 0.50                                # the figure quad, metres (CELL px)
DIGIT_PX_H, DIGIT_RECT_W = 52, 40            # digit height and the width of its UV rect (of the 64 px cell)
RING_PX, RING_HALF, RING_R0, RING_R1, RING_N, RING_GAP = 512, 0.62, 0.47, 0.60, 24, 1.2   # ring texture px, half-size m, radii m, segments, gap each side of a segment (degrees)

CELLS = ["walk_full", "stand_full", "walk_head", "stand_head", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "blank", "dash"]
FIGURES = ("walk_full", "stand_full", "walk_head", "stand_head")


def cell_rect(name):
    """(col, row from the top) of a cell and its UV rect (u0, v0, w, h), v up. Digit cells give the narrower DIGIT_RECT_W rect, centred."""
    i = CELLS.index(name)
    col, row = i % GRID, i // GRID
    x0, w = col * CELL, CELL
    if name.isdigit() or name in ("blank", "dash"):
        x0, w = col * CELL + (CELL - DIGIT_RECT_W) // 2, DIGIT_RECT_W
    return (col, row), (x0 / ATLAS, 1 - (row + 1) * CELL / ATLAS, w / ATLAS, CELL / ATLAS)


