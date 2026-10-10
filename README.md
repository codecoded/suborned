# The Suborned

Single-player real-time tactics and social infiltration in a bright retro-future city. PC · Unity. Design: `GDD/`.

The game is built by an agent crew defined in the GDD (§12 Crew, §14 Roles): Aster leads 16 agents, Sky is the only human. Content is generated at build time by the same crew, from the GDD retrieved out of a local index; nothing runs AI at play time.

## Assignment 4 submission (content pipeline)
Branch `assignment-04`. Everything for marking is in **[`courses/a04-content-pipeline/`](courses/a04-content-pipeline/README.md)**: start at "For markers". The quick check is `bun install && bun run verify` (offline, under 2 minutes).

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
| `bun run crew` | Regenerate `.claude/agents/` from the agent GDD |
| `bun run ingest` / `retrieve` | Build / query the retrieval index (HNSW + BM25, reciprocal rank fusion) |
| `bun run down` | Stop the local stack |

Unity: open `unity/` with Unity 6000.6.2f1.
