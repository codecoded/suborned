# The Suborned

Single-player real-time tactics and social infiltration in a bright retro-future city. PC · Unity. Design: `GDD/`.

The game is built by an agent crew defined in the GDD (§12 Crew, §14 Roles): Aster leads 16 agents, Sky is the only human. Content is generated at build time by the same crew, from the GDD retrieved out of a local index; nothing runs AI at play time.

## Assignment 4 submission (content pipeline)
Branch `assignment-04`. Everything for marking is in **[`courses/a04-content-pipeline/`](courses/a04-content-pipeline/README.md)**: start at "For markers". The quick check is `bun install && bun run verify` (offline, 1–2.5 minutes; allow a 5-minute timeout).

## Run it

Needs [Bun](https://bun.sh) 1.4+ and Docker. Generation and the crew need [Claude Code](https://code.claude.com) (signed in). The visible world needs Unity 6000.6.2f1; the Unity MCP needs the `unity` CLI on `PATH` (or set `UNITY_CLI`).

```sh
bun install
bun run check                 # typecheck + tests + plan checker; no Docker, no model calls
bun run up                    # local SurrealDB + embedding service (first start downloads the model)
bun run ingest                # index the GDD and the district plan
bun run retrieve "tram stop spacing and station reach"
claude --agent lead-producer  # the crew, led by the lead producer (Aster in the GDD)
```

| Command | What it does |
|---|---|
| `bun run plan` | Check `contracts/world/harbour-district.plan.json` against its rules; draw the CAD sheet to `out/plan/` (exit 1 on any failed rule) |
| `bun run block` | Write `contracts/world/civic-block.json` (café and Records Office at their Oct 9 scale) from the old repo's sources, with sha256 |
| `bun run world` | Generate the district from the plan → `out/world/harbour-district.world.json` (buildings, frontages, levels, megastructures, street furniture, kit refs); exits 1 unless the plan passes the checker |
| `bun run life` | Generate `contracts/world/harbour-district.life.json` (city-life rules: transit, traffic, deliveries, footfall) from `tools/life/`, the plan and the r008 timetable |
| `bun run kit` | Write `contracts/world/kit.json` from the DecoKit manifest; copy only `adopted` pieces into `unity/Assets/Suborned/Kit/` (git-ignored), checked by hash |
| `bun run decokit` | Run the Blender DecoKit generator (`tools/decokit`) → `out/decokit/` (needs Blender) |
| `bun run crew` | Regenerate `.claude/agents/` from the agent GDD |
| `bun run ingest` / `retrieve` | Build / query the retrieval index (HNSW + BM25, reciprocal rank fusion) |
| `bun run levels transit` | The A04 transit pipeline (retrieval → crew → checker → Keel); `levels:replay`, `levels:sheets`, `levels:ranks` |
| `bun run verify` / `doctor` | Offline replay of the recorded A04 runs / preflight for Bun, Docker, ports, Claude Code |
| `bun run down` | Stop the local stack |

Unity: open `unity/` with Unity 6000.6.2f1. The district scene `Assets/Scenes/HarbourDistrict.unity` is generated from the world export (git-ignored).
