# The seal deadline must be visible before it costs the mission

Sky's 0.2.232 Air play, 4 November 2026: "this feels unfair - I had no
countdown, no indicator that this would happen." The screenshot records seed
102258265, a seal at 5:22, Trail 56 and Menace -5, with Popovic not aboard.

The existing source has a countdown, but represents it as a small 38 px lock
and m:ss badge beside the known-people strip. Its cause is hover-only, and its
last ten seconds fade in and out. The end-card screenshot cannot establish
whether it was absent, covered or simply missed during this run. Either way,
the deadline was not communicated sufficiently to the player.

The fix places a persistent 420 × 104 kit plate at the top centre, beneath the
status bar, in both views. It names the deadline ("District seals in 2:25"),
explains that security is closing the exits and says what to do. Before boarding:
get Popovic aboard and leave. Aboard: press Go. Aboard but blocked: clear the exit
then press Go. The timer stays fully opaque, turns amber for the final 30 seconds
and retains the draining bar. Its wording is mission data in `less.sealed.live`.
The engine's deadline, consequences, simulation and saved orders are unchanged.

Checks passed:

- All player runtime scripts compile against the retained 0.2.232 player's Unity
  and package assemblies: zero errors, one existing unused-function warning.
- Eleven C# checks cover countdown formatting, the three instructions, legacy
  text fallback and preserving the existing failure explanation.
- Mission text validates against its schema; 19 focused mission/style tests pass.
- The existing player proof now requires a named deadline, an escape instruction,
  visible on-screen bounds and visibility in the follow view.

Still required before shipping: rebuild through Unity and run that player proof,
inspect actual map/third-person captures at Air HUD scale, then an Air playtest.
The Unity import cache was removed in the storage cleanup, so the normal runtime
compile helper lacks Library package references. This compile used the retained
player's matching assemblies without recreating Library or launching Unity.
No fresh player, visual proof or upload is claimed. The Air still has 0.2.232.
Local verification files: `out/scratch/seal-warning/`. A7 remains locked.
