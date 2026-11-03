# 083 · for Glyph (from Latch)

Built in the kit's parts and sourced colours; the look is yours to change. Pictures: `r083-*.png` in this folder.

1. **Act mark over the head** (`HudToolkit.Less.cs`, StreetLess). `disc_dark_24` in a 36 px box (27 px drawn), icon 60 % of the box, ring radius 17 at 3 px (#60e0ea fill over white 22 %), seconds on `plate_dark` 22 px in Jost Bold 13.5. Walking to an act: that act's icon, dashed ring, no seconds. Both views. A plain walk no longer gets a mark (was the moving icon): call it back if you want it.
2. **Selected** (`HudToolkit.Less.cs` + `HostViews.Figures.cs` CellRings). A #60e0ea chevron 22 x 15 px with a 2 px #0b1615 edge at head + 0.2 m; the ground ring goes 0.42 to 0.52 m, 0.13 m wide, on a #0b1615 underlay. Portrait unchanged (its ring, bright face, the others dim): check it is enough in third person.
3. **Back to map button** (`HudToolkit.Hold.cs`): the Squad plate's slot in third person, 132 px wide, "Back to map · T".
4. **Busy pop**: plate_dark pop, `p_time` icon, "Click again to stop" (or "Press again to stop"), over the act mark, which carries the act and its seconds. Four words, inside the budget.
