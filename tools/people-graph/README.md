# People graph

The expanded people as a graph in SurrealDB, to ask who knows whom, who can introduce whom and who stands where, in one query. **Derived only**: the JSON (the cast tables, `out/people/*.people.json`) stays the source of truth. A run, a replay, the checker and Unity never read this database; delete it and the command makes it again. No model calls; no embeddings are loaded (they would add nothing to a graph of 130 people).

```
bun run people:graph                      # the promoted cast (held to its run first, as bun run people does)
bun run people:graph --run r015           # an accepted run's cast (the last round that ran Pulse)
bun run people:graph --cast <file>        # a cast file
bun run people:graph query <name> [args]  # a canned query, printed as a table (--json for JSON; --db p_… names the database)
bun run people:graph queries              # the canned queries and their arguments
bun run people:graph list                 # the people graphs on the server
bun run people:graph verify [--run …]     # hold every row and every canned query to the JSON
bun run people:graph status [--run …]     # is this cast's graph loaded
```

`--force` loads again over a loaded graph, or loads people the checker fails; `--dry-run` prints the hash and counts without touching the server. `bun run doctor` has a `people` line: loaded for the promoted cast or not (a warning, never a failure).

## The database

The command expands the cast exactly as `bun run people` does (same code, same checker; it refuses people that fail P-0..P-13), builds the graph in memory (`model.ts`), and hashes it: the hash is of the people, the rules lookups, the places, the Handover roles and who is cast in them, the schema text and `SCHEMA_VERSION`. One database per hash, `p_<first 16 hex digits>`, namespace `suborned` (the corpus's databases are `c_…`). The same people are loaded once: a second run finds `graph:current` (the load record, written last, so a half-loaded database is never reused) and says "Already loaded". Another cast, another expander output or another schema is another database; old ones are left alone (`list` shows them; `REMOVE DATABASE p_…` removes one).

The server and password are tools/rag's (`SURREAL_URL`, `SURREAL_USER`, `SURREAL_PASS` from `.env`; see `.env.example`). The password is never printed.

## Schema (`schema.surql`, all SCHEMAFULL)

Times are minutes of the day clock (`fn::hhmm(m)` writes one as hh:mm). Positions are metres in the plan frame.

| Node | Holds |
|---|---|
| `person` | every field the people file carries: `name`, `tier`, `position`, `temperament` and `need` (links to the lookups), `need_weights` (the row's offer, per mille), `institution`, `start` (bravery, resentment, fear, loyalty), `memory` (empty), `look`, `routine_variant`, `home_plot`. Record id = the person id (`person:⟨p-cafe-12⟩`). The rest of a person is an edge (below); `PERSON_FIELDS` in `model.ts` says where each field lands, and a test fails if the people schema gains one it does not place., `secret` (link to the `secret` lookup: `affair`, held by both lovers, or `record`) |
| `unit` | `name`, `plot`, `area`, `trade`, `employer`, `staff_per_shift`, `hours`, and its `door`, `official`, `bay` places |
| `place` | `kind` (door, bay, stop, seat, stand, home, beyond), `name`, `at`, `area`, `plot`, the `unit` it belongs to (a door, bay or the café's chairs and standing points), the chair `set` it is on, and for homes `residents` / `simulated` |
| `role` | the four Handover roles (`seq` = mission order): what the mission asks (`need`, `approach`, `regular_of`, `tied_to`, `leaves_to`, `waits_for`, `alone`, window) and how they were cast (`cast_ok`, `cast_mode`, `cast_why`) |
| `shift` | a unit's shift (`unit`, `from_min`, `to_min`, `wraps`) |
| `need`, `temperament`, `tie_kind`, `secret` | lookups from `people.rules.json`: what meets a need, what a temperament responds to and refuses and where it starts, a tie kind's direction, limits and `introduces` |

| Edge (RELATE) | From → to | Fields |
|---|---|---|
| `tie` | person → person | `kind` (→ `tie_kind`), `introduces` (the kind's rule), `mutual`, `secret` (the kind's rule: the lover tie), `at` |
| `works_at` | person → unit | `position` (or the civic role), `civic`, `post`, `place`, `shift`, `shift_from`, `shift_to`, `employer` |
| `lives_at` | person → place | (`place:beyond` for a person who lives off the sheet) |
| `stops_at` | person → place | `seq`, `doing` (home, commute, work, meal, pre-shift, evening, errand), `from_min`, `to_min`: one edge per stop of the routine |
| `sits_at` | person → place | `doing`, `from_min`, `to_min`: the stops at a café chair, the seat holds (one person to a chair at a time, P-13) |
| `knows_need_of` | keeper → person | `via`: `workmate`, `unit-regular` (rules `knowledge.needsKnown`) |
| `casts` | role → person | `pinned`: everyone who fits the role in its window, and the one the Handover casts |
| `has_need` | person → need | `weight` (the person's need, per mille of their row's offer) |
| `has_temperament` | person → temperament | |

**One `tie` table with a `kind`, not a table per kind.** The questions that matter cross kinds ("the introducing ties", "any tie within two hops", "ties of a person"), and `kind` links the `tie_kind` lookup, so a new kind in the rules is a lookup row, not a new table and a change to every query. The rules' `introduces` flag is copied onto the edge so a traversal filters it without a join, and the table is indexed on `kind`. The cost is that a per-kind view needs `WHERE kind = tie_kind:regular`.

**Direction.** A two-way kind (family, workmate, rival) is stored once per pair, `in` = the lower person id, `mutual: true`; the JSON carries it on both people. A directed kind is one edge from its source: `regular` is customer → staff, `debtor` is debtor → creditor. Introductions run either way along a tie (a turned contact can introduce the people a tie joins them to; the rules' reason names Inez introducing café contacts, her regulars as well as her workmates), so queries traverse `<->`.

**Lovers and secrets (brief 011).** `lover` is a tie kind like the others (mutual, `secret: true`, `max_share`, `forms_along`), so "the 2-hop neighbourhood" still crosses it; `tie.secret` copies the kind's flag so a traversal can leave out ties the city is never told. `person.secret` links the `secret` lookup the rules list (none when the rules list none, as in a pinned older rulebook). `tie.at` of a lover tie is the stop either of the two makes at the other's work. The `lovers` query lists the pairs with both ends' secrets; the schema version is 2.

**`tie.at`** is derived, not authored: for a regular tie, the stop of the customer's day (pre-shift, then evening, then any other) that stands at the staff person's work place, or the café's customers' side, while they are on shift (the P-7 test). The cast row's own `at` is not in the people file.

**Not in the graph**: background people (they are counts per home plot, on the `home` places), memory (empty at the start), and the embeddings.

## Canned queries (`queries/*.surql`)

Each file is a complete script. Its header says what it asks and names its parameters (`-- param $name type default what`); the `LET $name = …;` lines set them to the defaults. Paste a file into Surrealist as it stands, or change the `LET` lines. The command line rewrites those lines from its arguments (an id, hh:mm or a number; never SurrealQL) and runs the file.

| Query | Asks |
|---|---|
| `who-can-introduce <a> <b>` | the shortest chain of introducing ties (family, workmate, regular) from a to b, hop by hop, with the tie kinds; `hops` is none when there is no chain |
| `lovers` | each lover pair once (the lower id first), both ends' positions and secrets, and the stop they meet on; empty when no cast table makes a pair |
| `regulars-of <place> [at hh:mm]` | the people with a regular tie to staff of the place's unit, whom they are regulars of, their stops at the unit's places (at the time, if given) |
| `who-is-near <place> hh:mm [radius]` | every stop overlapping the time at a place within the radius (default 13 m, the witness reach, GDD §05), nearest first |
| `guards-who-are-cafe-regulars [position] [unit]` | the guards with a regular tie to café staff, and which staff |
| `needs-known-by-keeper <person>` | whose need the keeper knows from the start, with need, help, temperament and how (`via`) |
| `role-fit` | each Handover role with everyone cast in it (pinned first): need, temperament, whether they meet the role's need, approach and `regularOf`, and their ties |
| `neighbourhood <person>` | the 1-hop and 2-hop neighbourhood over every tie kind, either direction |
| `graph-view <person> [hops]` | the same, as plain records for Surrealist's graph view (below) |
| `day-of <person>` | one person's stops in order |
| `counts` | rows per table and ties per kind |

```
bun run people:graph query who-can-introduce p-cafe-12 p-records-office-07
bun run people:graph query regulars-of cafe-entrance at 09:13
bun run people:graph query who-is-near cafe-stand-03 17:35 13
```

**Do the answers agree with the JSON?** `people-graph.test.ts` holds the model to the people file (every tie, stop, work, home, need, knowledge edge and role cast; nothing dangling; unknown kinds, needs, places and one-way two-way ties rejected) and walks the model's edges for the same questions the queries ask. The SurrealQL itself is held to the JSON by `bun run people:graph verify` against a loaded database: every row read back and compared to the model, and each query's answer compared to a TypeScript reference worked out from the people file alone (`reference.ts`: breadth-first introductions, regulars, stops within a radius, keepers, role fit, neighbourhoods). The test runs the same `verify` when `PEOPLE_GRAPH_LIVE=1`; by default no test touches a server.

## Surrealist

1. Connect to the endpoint the command prints: `SURREAL_URL` from `.env` (`.env.example`: `http://skys-mac-studio:18504` over Tailscale; `http://127.0.0.1:18504` on the machine that runs the stack). Authentication: root level, user `suborned` (or `SURREAL_USER`), password `SURREAL_PASS` from `.env`. Namespace `suborned`; database: the `p_…` name `bun run people:graph` printed (also `list`).
2. Open a query tab and paste a file from `queries/`; run it. To try first: `counts`, `role-fit`, `who-can-introduce` (it chains Inez to Councillor Vale through a guard), `regulars-of`.
3. Graph view: paste `graph-view.surql`, set `LET $person` (say `person:⟨p-records-office-01⟩`, the guard) and run it. It returns the person, the people within `$hops` ties and the ties among them as plain records (nodes carry `id`, edges carry `in` and `out`) with the centre's unit, home, need and temperament; switch the result to the graph view. `$hops = 2` is crowded for a hub such as the café keeper. This is written to the shape Surrealist's graph view reads; it has not been opened in the Surrealist app from this machine.
4. Not Surrealist's designer: the schema is `schema.surql`; to look at it, `INFO FOR DB`.

Record ids are bracketed (`person:⟨p-cafe-01⟩`) because the ids have hyphens; Surrealist shows them with backticks.

## Files

`cli.ts` the command · `source.ts` cast → people → model → hash → database name · `model.ts` the graph as plain data (pure) · `surql.ts` SurrealQL text from it · `load.ts` the load and the load record · `queries.ts` + `queries/` the canned queries · `reference.ts` + `verify.ts` the TypeScript answers and the check against the server · `status.ts` the doctor line · `render.ts` the table output · `people-graph.test.ts` the tests.

A change to `schema.surql` or to what `model.ts` writes bumps `SCHEMA_VERSION` in `model.ts`; both are in the hash, so the new shape loads into a database of its own.
