# 013 · Takedowns, bodies and quiet places

**Status (2026-10-16):** the cones have landed in the sim and the play build. Drafts are in for A (Motive: `briefs/013/motive-takedowns.md`), the witnesses (Pulse: `briefs/013/pulse-witnesses.md`) and C (Vantage: `briefs/013/vantage-quiet-places.md`). Sky has answered Motive's calls (below). Motive and Pulse are now reconciling with each other and folding those answers in. B (Keel, engine) starts on the reconciled rules.

Goal (Sky, 2026-10-16, after playing the Handover and finding the cell idle; Motive's study `briefs/012/idle-cell-study.md` measured the Enforcer idle for 47–81% of a quiet run):
- "the enforcer could take out the driver, in theory can take out anyone, but it's all about being seen or not. The player has to decide the risk, and if the mission can succeed based on their strategy. Killing a civilian in front of a camera as a lone gunman is highly likely to end in failure."
- "civilians can see bodies. They don't disappear; the enforcer has to take them into a quiet place. That gives them a real game time. Hitman style, I guess."
- Later missions (a design note, not this brief): the Enforcer can call in their own suborned to join early and hold positions while the rest of the cell sets the scene for the fight.

## Decisions (Sky, 2026-10-16)
- **Anyone can be targeted.** No named target list (today `GameCombat.IsCivilianTarget` names waitress, driver and councillor). One set of rules for every simulated person; the cost comes only from who sees it: witnesses, cameras, Alert, Trail, Standing, the response.
- **Bodies persist and can be seen.** A downed or knocked-out person stays where they fell until moved. Anyone whose sight or routine passes them finds them, and that is an incident.
- **The Enforcer carries them to a quiet place.** Carrying is slow and visible (the existing carry: walk only). A quiet place hides a body. Exteriors only: a quiet place is outdoors (a service lane, a large bin, a cargo crate, a loading bay), or a timed off-screen action at a service door.
- **The cones are the Scout's skill** (approved the same day): the Scout's Watch is how the player learns whether a takedown will be seen. The price shows "seen by: ?" until then.

- **Both kill and knockout are possible** (Sky: "knock out is also possible"). The player chooses, and each shows its price. A knocked-out person wakes after a while and talks (faces, Trail) unless they are hidden, restrained or turned. A killed person never talks, but a found body is far worse: Alert, Standing (Menace) and the investigation.

### Sky's answers to Motive's calls (2026-10-16)
1. **A found killing moves Standing toward Menace (−1)**, though nobody saw the kill: yes ("easy to change", so keep it one tunable value).
2. **No rule that every body is found by the next mission.** A hidden body becomes a **missing person**, not a found body: the district answers with more guards and more cameras in that area next time. The pressure is similar, but its cause is different.
3. **The Enforcer's face is a fact a Talker can give up** once a witness has seen it: agreed.
4. **The guard checks in every 90 s**, at times the Scout can learn: ok.
5. **A service door and a Dumpster in the service lane**: ok (a block adjustment with a reason, and Vela adopts the Dumpster).
6. **The driver is takeable, not ruled out.** If the Tech takes out the pickup camera, the guard and the driver can both be taken. But then nobody is there to get Vale away:
   - A person who expects someone (Vale, his driver) and finds them missing grows suspicious.
   - Like anyone who sees a body, he can raise the alarm.
   - **An alarm sends robots and police to the scene, and to where the alarm-raiser's connections are meant to be.**
   - Sky: "the key is rapid response on alerts, making early missions very hard to fight out, perhaps???" (tentative). Fighting out should be a last resort, not a route.
   - Kills are shown, not cut away (Sky, 2026-10-16: "just body lays on the ground - can show it"). The act plays on screen, and the body lies where it fell until it is moved.
   - Sky: "it's easy to swarm the play area with robots if we need to temper the game play". The response's size and speed are the difficulty dial. Keep them as data per district and mission, not code.

### Sky's answers to the merged calls (2026-10-16, "otherwise ok to others")
All of these are approved as Motive proposed in `briefs/013/motive-takedowns.md` §8. That file, aligned with Pulse's, is the reference for Keel.
1. For silent acts, Alert rises when the report lands. Gunfire raises it at sight. Witness Trail and Standing move at sight.
2. A knockout is never above Alert 1.
3. The response dial is security levels 0–4, as data. The Handover is level 2, so the loud run is re-recorded.
4. Mercenary witnesses keep quiet until Alert 2.
5. Turned witnesses add no witness Trail.
6. Only a container or a service door hides a body. Pulse's "screened" level comes later.
7. Call boxes are placed by a world rule.
8. The Tech has no action to cut a call-box line.
9. Bin rounds run only within the mission.
10. Kills are shown: the body lies on the ground (above).
- The missing-person numbers: +2 Trail and no Menace; the victim's ties fear +1; +1 guard and +1 camera on the block for 3 missions; every 2 open cases is +1 district security level; a found killing's case closes after 3 missions.

### A found body locks down the scene (Sky, 2026-10-16)
Waymark's c009 showed the best guard route left the knocked-out guard where he fell: a found knockout is only Alert 1, so carrying never paid. Sky chose a lockdown at the scene:
- A found body, knocked out or killed, brings the response unit to it.
- While the unit is there, the nearest doors are held, and Enter is refused.
- A knockout stays at Alert 1, but leaving the guard at his door shuts that door. Carry him away to keep it open.

### Rev 4 calls (Sky, 2026-10-16)
- **A Take at Vale's own car by his turned driver happens inside the car:** off-screen, with nothing for anyone to witness. Any other Take has a 5 s struggle that onlookers can see. An escort on foot looks wrong only to kin, or to anyone within 3 m who sees the grip.
- **Preparation earns the right to leave a body.** With the camera looped and the patrol rerouted, leaving the knocked-out guard where he fell is fine. Carrying pays when a finder could arrive while the cell still needs that door. No new doorstep watcher.

## Proposed (Aster), for Sky to confirm
- **The quiet takedown belongs to the Enforcer:** close range, a few seconds, silent if unseen. The other three can carry, but slower.

## Parts
- **A · Rules (Motive, with Keel for feasibility):** the takedown (range, time, who can do it, knockout or lethal); body discovery (who notices, at what range, through crowds, by routine); wake-up and talk; carry speeds; what makes a place quiet; prices and Trail, Alert and Standing for each case. The prices show before commit. People > powers: no new powers, only people, sight and time. Output: `briefs/013/motive-takedowns.md` with rulebook entries marked proposed.
- **B · Engine (Keel):** remove the named target list. Add a takedown order; bodies as persistent world state; discovery through the existing witness and incident rules; carry and stash; wake. Scenarios for each case; replays stay identical.
- **C · Quiet places (Vantage, with Splice):** where they are in the civic block, derived from the world (kit pieces and block layout), never hand-placed for one mission. Added to the export if the world doesn't carry them.
- **D · The Handover (Waymark):** new informed routes (take out the guard; take out the driver). Every mission-check condition still holds; loud and careless violence stay worse; re-measure idle time with `tools/missions/idle/`.
- **E · Play (Latch, Glyph):** the takedown and carry orders, body markers, quiet-place markers, the "seen by" price.

Done-check: on the Handover, a takedown of the guard or the driver that nobody sees is a viable route with a real time cost. The same act in front of a camera or a crowd ends in failure or a loud run. The Enforcer's idle time on the informed route falls below 40%. The mission check SHIPS, replays are identical, and the GDD edit is approved by Sky.
