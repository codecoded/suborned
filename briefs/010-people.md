# 010 · People: the city's cast, generated from its places and routines

**Status (2026-10-15):** A done (dceefb7, e2906ea, with the Dwarf Fortress pass). B done (9e5dff3). D done (9f05ba9). E done on the stand-in cast (f4fbca3, `briefs/010/shots/`). Sky's rulings Q1–Q7 are recorded below. Next: Splice adds P3/P5 checks, the corpus chunks and names given by role; Waymark makes M-2 require the needs; then C, the live run (Sky approves the ceiling). No model call until then.

Goal (Sky, 2026-10-15): "NPC generation, using SurrealDB for RAG to create them based on their placements and routines." Brief 005's life.json gives counts and slots (39 occupations, ≈5,100 staff, ≈30,400 residents, five routine skeletons), not people. Nobody has a name, temperament, need or tie yet. This brief makes them.

## Fixed (Sky, 2026-10-15)
- **The model writes cast tables, code makes the people.** Per area and trade the model proposes the mix: temperaments, needs, tie patterns, routine variants, name pools. Deterministic code seeded by plot and unit id expands them into individuals, as the world generator does. Cheap, checkable, and it replays exactly. No per-person model output.
- **Mission people are city people, pinned by role.** Inez is whichever café waitress the generator makes; the Handover picks her by role and place and pins her by id. The same holds for the driver, the guard and Vale's office. No mission authors a person (§01 systems > scripts).
- **The civic block first:** the café, the Records Office, the square and their stops, the Handover's ground. Sky reviews it, then the whole district runs.
- **The GDD's existing rules hold:**
  - a person = routine + need + temperament + memory (§04);
  - occupation from the trades on their block × temperament × need × ties × routine, fitted to the district's schedules; checked that they reach their places and have one clear need (§09);
  - history, not backstory: text is a light layer, and Fable generates dialogue from person state later;
  - build time only, byte-identical output, exact replay;
  - unknown = reject.
- **Through the pipeline:** a `people` plug-in on `tools/gen` + `tools/levels` + `tools/rag`, copied from the skyline plug-in (`tools/levels/skyline/`). Runs are recorded in `runs/levels/` and replay offline. A live run needs `SUBORNED_LIVE=1`, `--approved-by` and `--max-usd`; Sky approves each.

## Decisions (Sky, 2026-10-15, on Motive's Dwarf Fortress pass, `briefs/010/research/df-for-the-suborned.md`)
- **Q4, the wrong approach for a temperament still works, one step worse:** one more Help, or a higher price; Keel sets the numbers. An approach the temperament refuses outright fails and leaves a remembered refusal.
- **Q3, the preview shows the price always, and the outcome as "?"** until Watch or Listen reveals the temperament.
- **Q2, a met need is replaced between missions,** drawn from the person's own stream (id + mission number). A turned contact tells it free in the debrief; anyone else needs Listen again.
- **Q1: no alias for now;** the cell is known by face only.
- **Q5: investigators question simulated people only;** background crowds add T, Alert and standing.
- **Q6: news travels along ties as facts, never opinion;** standing stays witnessed acts only.
- **Q7: P1–P5 go into GDD §04 and §09** at the next version, together with Q2–Q4.
- Live run C waits until Splice's P3/P5 checks, Waymark's M-2 needs and the cast requirements in Motive's §3 are in.

## A · Motive · the people rules
objective: the rules a generated person must obey, as a contract the checker reads. Write `contracts/people/people.rules.json` with a zod schema `people.rules.schema.ts`. It covers:
- the temperament set (Idealist, Mercenary, Fearful, Loyal to an institution) and its starting bravery and resentment;
- the need set, a closed list, each need tied to the approaches that can meet it (§04 prices);
- the tie kinds (family, workmate, regular, debtor, rival) and their limits;
- the Talker test, fear + resentment > bravery + loyalty, stated as data;
- which trades and roles may carry which temperaments (a guard is Loyal to the Office or Mercenary, never Idealist?);
- the name rule: a pool per district, no era date, a mixed cast (§15).

The ruling on how people are drawn: anything not in this file is rejected.
done-check: the schema parses the file; `bun run typecheck`; one page of reasoning beside it in `briefs/010/` (why each rule, which GDD § it serves).
next owner: Splice (B reads it), Keel (critic).

## B · Splice · plug-in, expander and checker (no model call)
objective: everything the run needs except the model's answer.
1. **Corpus:** ingest `harbour-district.life.json`, `civic-block.json`, `contracts/missions/*/mission.json` and `people.rules.json` (chunker bump, `bun run ingest`). Check with `bun run retrieve` that a cast-table query finds the café's trade, hours and staff.
2. **Schemas:** `contracts/people/cast.schema.ts` holds the cast tables the model proposes: per area × occupation, weighted temperaments and needs, routine variants fitted to the life.json bands, tie patterns, name pools. `contracts/people/people.schema.ts` holds the expanded person: id, home, work unit and shift, routine (timed stops), temperament, need, ties, look key (job colour block), tier (simulated / background).
3. **Expander** (`tools/people/`, `bun run people`): cast tables + plan + civic block + life.json → `out/people/harbour-district.people.json`. Seeded by plot and unit id, byte-identical twice.
4. **Checker** (rule ids P-1…):
   - everyone reaches each routine stop on the walk graph in time;
   - exactly one clear need;
   - only temperaments, needs and ties `people.rules.json` allows;
   - staff counts match life.json per unit;
   - no one stands in a CD-1 keep-clear zone off-shift;
   - the Handover can cast every role it names (a waitress on shift at the café at 17:00, a driver at the bay, a guard at the Office door).
5. **Plug-in** `tools/levels/people/` from the skyline template, with Pulse (builder) and Keel (critic) objectives in `tools/levels/objectives/people-{pulse,critic}.md`. `--preview` and a stand-in must run end to end with no model call.

done-check: `bun test tools/people tools/levels`, `bun run typecheck`, `bun run levels people --preview` exits 0, `bun run verify` OK.
next owner: Pulse (C).

## C · Pulse · the first cast (live, Sky approves)
objective: Pulse's objective for the run. The cast tables for the civic block, fitted to its trades, hours, stops and the r008 timetable: who works the café and the Office and when, who lives above, who passes through and why. Keel critiques them against `people.rules.json`, the GDD and the checker.
- The run: `bun run levels people --scope civic-block --approved-by "Sky, <date>" --max-usd <Sky's ceiling>`. Before asking Sky, estimate the cost with `--preview`.
done-check: the run is ACCEPTED, replays offline (`levels:replay`), and the checker has 0 faults. The expanded people come with a review page `out/review/010-cast.html`, also copied to `briefs/010/`, showing:
- the cast by occupation and temperament;
- five sample people with their day;
- who the Handover would cast.

next owner: Waymark (D), Latch (E).

## D · Waymark · the Handover casts by role
objective: `contracts/missions/handover/mission.json` names roles (waitress at the café on the 17:00 shift, the councillor's driver, the Office door guard), not people. Its overlay pins the generated person per role by id and fails if a role can't be cast. The schedule is unchanged.
done-check: the Handover overlay exits 0 against the run's people; recorded runs still replay.

## E · Latch · see them (graybox, no simulation yet)
objective: Sky sees the cast before Keel's simulation exists. A snapshot at a chosen clock time (17:00 for the Handover):
- every simulated person in the civic block stands at the routine stop they're at then;
- each is a capsule with a job colour block (brief 005's graybox);
- clicking one shows their record.

The scene is still built from exports, and people come from `out/people/`.
done-check: play-zoom shots at 08:00, 13:00 and 17:00 in `briefs/010/shots/`; console 0 errors.

## Later
- The whole district, once Sky signs off the civic block.
- Keel's C# simulation steps routines (brief 005).
- Fable's dialogue from person state.
- Forma's figures replace the capsules.
- Promotions, arrests and introductions between missions (§04).
