# Collapsed clipping output correction

Redline found 2,163 zero-area triangles in the saved eight-mesh tram correction. Aster changed `SwapMesh.Clip` to remove exact consecutive duplicate positions, and `Write.Poly` to reject collapsed/numerically collinear fan triangles before adding output vertices. The area test uses double-precision local 3D coordinates and a scale-relative sine threshold of 1e-12, with no absolute size cutoff. It preserves vertical faces and genuinely thin/tiny geometry; non-finite output is an explicit error.

Run `python3 briefs/075/recovery/tram-degenerate/run.py`. This compiles the actual `SwapMesh.cs` against the project's installed Unity managed assemblies, with all build output under scratch. It creates no native mesh or Unity process. Boundary/duplicate/collinear, thin/tiny/vertical, rotated-area, attribute and repeated-cut checks passed.

The source correction has not regenerated saved meshes or rerun the surface audit. Latch must include it in the next settled tram correction, prove zero degenerate outputs without losing coverage/attributes, and retain collider/nav/input preservation evidence. Redline reviews that final result. Whole tram acceptance and G-3a remain on hold; existing captures describe the prior source state.
