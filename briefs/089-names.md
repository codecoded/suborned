# 089 · Names that belong to this game alone

Owner: Aster. Opened 2026-11-02. Sky: "I also can't stand out characters names. See the same Name like Okafar and vale in tottally unrelated other AI built apps"; "All names need to be relevant for this game and this game alone"; "We should also give our cells names".

## Objective
Every name in the game comes from a naming custom only this world has, and the cell has names of its own.

## Done-check
- Sky has picked a naming custom from sample casts. No rename before that.
- The custom is a rule the people generator can follow for every future person, with no model free-picking names.
- A ban list of names language models favour, with its sources, and a check that fails on any banned name in what the player sees.
- The player-visible names change in data (`contracts/missions/handover/text.json`, `contracts/people/`); internal code names (`ValeAt`, `councillor`, `waitress`) may stay; a saved run still replays. Scenes are reread by Fable, because names are said aloud in them.
- A new GDD version, then `bun run crew`, after Sky's pick.

## Scope
People first: the named cast, the cell and the generator's pools. Places and institutions are listed for Sky to judge. `assignment-04` and its evidence under `courses/` are never changed.

## Owners
Fable (custom, names, scenes) · Splice (the check) · Pulse (the generator's pools) · Latch (build) · Sky (the pick).
