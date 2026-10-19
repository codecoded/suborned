# Brief 024 research notes (Glyph, 2026-10-19)

Web searches only (not fetched pages); community and guide sources, so they shape the choice and the GDD/briefs stay the source. Nothing here is an official developer spec.

## Ghost Recon Wildlands: squad orders
- Sync Shot: mark enemies (drone, binoculars or sights), a number shows over each marked head, a spinning circle clears when a squad mate has lined up, then the squad fires together, on your command or when you fire.
  - https://twinfinite.net/?p=265009
  - https://twinfinite.net/guides/ghost-recon-wildlands-how-to-use-sync-shot/
  - https://inverse.com/article/28914-ghost-recon-wildlands-stealth-guide-tips-and-tricks
- Players report the AI squad fights back when fired on and sometimes scatters or gets spotted when sneaking (anecdotal): https://steamcommunity.com/app/460930/discussions/0/135511027314398734
- Used for: a group command set the player can give without leaving the ground; "mark, then the squad acts" as the stretch; no official alert-icon description was found, so the awareness cue is modelled on Hitman instead.

## Hitman: awareness at eye level
- A white marker over a person means they can start to notice you; a yellow arrow fills as suspicion builds; a full fill or red means they are aware. Colour progression white, yellow, red in the Absolution guide.
  - https://steamcommunity.com/app/236870/discussions/0/392184522708056906
  - https://steamcommunity.com/app/1659040/discussions/0/3782498683141235167/?ctp=4
  - https://tvtropes.org/pmwiki/pmwiki.php/Main/VisibilityMeter
- Used for: a pip over each watcher that covers you, an eye on you, an edge marker when the watcher is off screen. Fill rates not found; the engine has no notice ramp today (see the brief, §3).

## Unity: orbit camera
- Cinemachine 3 on Unity 6 offers Orbital Follow / Third Person Follow, an input axis controller and a Deoccluder; the Deoccluder reportedly misses terrain unless layers are set.
  - https://uhiyama-lab.com/en/notes/unity/unity-cinemachine-guide/
  - https://discussions.unity.com/t/cinemachine-3-1-5-is-now-available/1691770
- Decision: not taken for the prototype. The city has no colliders; HostFollow already has its own boom against the engine's indexed solids.
