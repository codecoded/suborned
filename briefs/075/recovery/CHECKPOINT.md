# Brief 075 recovery checkpoint

Recovered the stalled crew's work under managed child tasks. Redline's independent integration review is in `integration-review/report.html`; Latch's captured-input handoff is in `latch-integration/README.md`.

Accepted provisionally: the two civic plots use the existing flag paving, the foliage review tooling stays opt-in/default-off, and captures establish station placement inside SpaceHQ. This is a code and documentation checkpoint, not a player build or release.

Still on hold:

- Foliage adoption: three review placements add cost without measured frame time; dark leaves, bulky blooms, motion alpha and LOD behavior need further work. Topiary, palms and embedded planting remain unresolved.
- Tram acceptance: overlaps, gaps and unreadable audit coverage remain. Captures establish placement, not motion correctness.
- Geometry: G-3a remains 59 against the unchanged cap of 58. The exact world/walk pair is verified, but geometry acceptance is blocked.
- Sight correction: Keel's proposed patch is unapplied. Its wider tag/robot/cafe effects and junction conflicts require resolution before protected-sightline approval. Production simulation and recordings were preserved.

The review helper still has limited fingerprint coverage and non-transactional writes to generated review assets. Its earlier successful manifests remain evidence of that captured state; they are not a general exact-preservation guarantee. Generated review meshes/scenes, images, builds and logs stay local and are excluded from this checkpoint.

After Redline's review, Aster fixed the comparator's exit status and literal placement-ID check, and added `--output-dir` so verification can leave captured evidence intact. The original manifests pass all 14 checks. Negative probes for duplicate IDs, a wrong ID and changed navigation each return exit 1. Probe outputs are under `out/scratch/aster/recovery-gate/`; the original captured manifests were not rewritten.

No production foliage enablement, site rebake, recording replacement, Air run or new player build occurred. Full recovered tram/HQ source changes remain outside this provisional civic checkpoint pending their own review.
