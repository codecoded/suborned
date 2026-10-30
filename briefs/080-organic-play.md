# 080 · Organic play

Owner: Aster. Opened 2026-10-30. Sky: "let the game play organically - player should be merited for their playing style, but same time feel it's vibrant. As long as the core mechanics are fair, like in any RPG / strategy i.e. guards don't hit 100% with high damage etc - then it should just all flow. Re turning the tests to check this shouldn't be so rigid". Before that: "this whole everything must replay exact feels boring. How would syndicate feel if it did that?"

## Direction
- Each new game rolls a seed, saved with the run. Saved runs replay exactly; new games differ.
- Fair core mechanics: real hit chance and damage that a player can read and play around; nobody hits 100% with high damage; every fail has a cause a player can see.
- Playing style is what earns the result: preparation, quiet play and good positioning shift the odds. They don't guarantee a script.
- Vibrant: people's timings, crowds, reactions and rivals vary within fair bounds.

## Sky, 2026-10-30: only the start is seeded
"only things that should matter is starting positions, which can be seeded, and then every thing plays out from there". So the seed sets the starting state, and no dice are rolled during play: everything after the start is the systems playing out. Keel measures how far runs diverge from different starts.

## Sky, 2026-10-30: where this goes (not this brief's work)
"a real living world needs organic play - we add 1000 characters, they don't run on scripts, they run on genetic algos kinda thing, based on their own seed. Some characters will always be the same, others will have fuzziness". For now: per-character seed from game seed + id, class stats with a per-character offset, a fuzziness value per character (0 = always the same), so the Handover's slice doesn't close that door.

## Sky, 2026-10-30: hit rolls
"hit rolls" (Option A): each shot rolls to hit from the game's seed; class stats (guards better aim than civilians, same HP and damage, slow; police above guards; rivals most dangerous); routines, crowds and rivals flow from the seeded start. Bounded so one roll can't lose the mission. This refines "only the start is seeded": combat rolls too.

## Sky, 2026-10-30: design approved
On briefs/080/design.html: "1. ok 2. ok 3. ok". The seed sets timings as well as positions; streak caps lean the player's way, and the game says so; the aim ladder stands as the start (civilian 35, patrol robot 45, guard 50, Tech 50, Fixer 55, police 60, Scout 60, rival 65, Enforcer 75; hit % = aim + range + cover + movement, clamped 5-95; civilians 3 HP; no crits; hostile pistols 1 damage; streaks cap at 3).

## Checks
Stay rigid: no crash or stall, unknown = reject, a saved run replays exactly, every order shows its price, every fail names its cause.
Become bands across many seeds: win rates by playing style, fight length, down rate, cost of loud against quiet. Route scripts become smoke tests that play an adaptive plan, not fixed ticks.

## Order
1. Motive + Keel: one-page design for Sky (briefs/080/design.html). No code.
2. After Sky's yes: Keel the seed and first slice (in a worktree), Waymark and Suture the checks; a GDD version (Aster).
