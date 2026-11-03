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

## Sky's calls
- The city: normal names, as GTA and Desperados III do (Sky: "why can't they be a bit more normal> What does GTA do?"; option D in `briefs/089/fable/naming-customs.md`), drawn from real frequency lists by origin, sex and age, never by a model.
- The cell: "I think the cell has a bit more esoteric names as they're a different timeline". Picked (Sky, 2026-11-02, "1. ok 2. Woman"): Etruscan names, one each: Scout **Larthi** (she), Fixer **Hasti**, Tech **Sethre**, Enforcer **Arruns**. In their timeline the Etruscans never died out.
- Inez and Milena keep their names (Sky: "keep"). Vale, Okafor and Zhou take Fable's sample draws, which Sky didn't object to: Councillor **Uros Popovic**, the guard **Arthur Reilly**, the driver **Aleksander Lukac**. Everyone else is drawn by the rule.
- Families share a surname (Sky, 2026-11-02: "sure - family name makes it more a real city"), so a player can trace who's tied to whom.

## Scope
People first: the named cast, the cell and the generator's pools. Places and institutions are listed for Sky to judge. `assignment-04` and its evidence under `courses/` are never changed.

## Owners
Fable (custom, names, scenes) · Splice (the check) · Pulse (the generator's pools) · Latch (build) · Sky (the pick).
