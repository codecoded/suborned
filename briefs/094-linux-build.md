# Linux test build 0.2.234 — 4 November 2026

Sky requested a Linux build for an external tester. The result is an x86-64
Mono player built with Unity 6000.6.2f1 from `build/linux-20261104`, source
`f96f6d65`. It includes the persistent district-seal countdown; predicting an
unwinnable deadline remains a proposal.

## Files for the tester

Both archives contain the complete game, launcher and instructions:

- `out/build/releases/TheSuborned-0.2.234-linux-x86_64.zip` — 678,524,390 bytes;
  SHA-256 `8d7d542566794c6479466cef44d86661fef1ebdfd62b38d05191e7fcea72637e`.
- `out/build/releases/TheSuborned-0.2.234-linux-x86_64.tar.gz` — 678,360,493 bytes;
  SHA-256 `0c838b4226341ea3f2032eb43f843d0bb8eb69842e23bdf3d9b0c8695c6b90c5`.

Extract the entire folder and run `./Start.sh`. This is for Intel/AMD 64-bit
Linux desktops, with keyboard and mouse. The instructions include executable
permissions, an OpenGL startup option and where to find the player log.
The archive includes per-file checksums and build metadata; Unity's source/debug
backup directory is excluded. No itch upload has been made.

## What passed

- Linux Mono build support installed for the existing Apple silicon Editor.
- The shared builder now has `GameBuild.CliLinux`, using the same scenes, content
  and build counter as macOS, with separate output under `out/build/linux/`.
- Current engine export: 394 mission scenarios and 97 range scenarios pass with
  identical replays. These are the reference/headless checks, not Linux-player
  gameplay tests.
- Unity's Linux build succeeds with zero errors and 18 warnings. The first cold
  build, 0.2.233, spent 3,781 seconds in the player build, predominantly compiling
  shaders. The cached 0.2.234 player build took 14 seconds.
- The executable and UnityPlayer library are ELF x86-64. The complete Mono
  runtime is inside the data folder. Archive permissions preserve the launcher
  and executable bits.
- The capture clip is temporarily imported as VP8 for Linux, with the original
  importer settings restored after the build. Extracting its embedded WebM from
  `resources.resource` and inspecting it with ffprobe confirms VP8, 640 × 360.
  This follows [Unity's video compatibility guidance](https://docs.unity.com/en-us/engine/6000.0/manual/video/sources/file-compatibility).

## Runtime validation still needs the tester

The actual player was attempted in Ubuntu 22.04 x86-64 through Docker Desktop
emulation on Apple silicon, both headless and with Xvfb/Mesa OpenGL. Both exited
134 in Mono before game code, asserting at `x86-codegen.h:410` with
`offset == (gint32)offset`; no scenario result or game screenshot was produced.
Changing the GC setting did not bypass the assertion. The graphical attempt
created an OpenGL 4.5 llvmpipe context before that failure. This is consistent
with reported Mono/emulation issues, but does not establish native compatibility.

Do not call this a successful Linux playtest. A native Intel/AMD Linux desktop
must confirm launch, city rendering, controls, sound, the capture clip and one
complete Handover. No performance claim is made for Linux. The archives and
their README state this limitation.

The first fresh import also reported 21 missing-parent generated prefab variants;
none of their GUIDs is referenced directly in OldTown. Build success does not
prove their absence has no indirect visual effect. The native city check remains
required. Rendering consolidation findings are in brief 095; that migration has
not been applied to this baseline build.

Local logs and proof attempts are in `out/scratch/linux-build/`. The build's seven
tracked range outputs were restored to their original source bytes after Unity
regenerated them; generated copies are retained in scratch. Both source and
regenerated FightRange have 356 serialized documents and 94 named objects.
The existing Mac player was preserved. A7's branch and course files remain locked
at `9ab7c14bc686eb378cffbd556691742b09048145`.
