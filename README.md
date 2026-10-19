# The Suborned

Single-player real-time tactics and social infiltration in a bright retro-future city. PC · Unity. Design: `GDD/`.

The game is built by an agent crew defined in the GDD (§12 Crew, §14 Roles): Aster leads 16 agents, Sky is the only human. Content is generated at build time by the same crew, from the GDD retrieved out of a local index; nothing runs AI at play time.

## Assignment 4 submission (content pipeline)
Branch `assignment-04`. Everything for marking is in **[`courses/a04-content-pipeline/`](courses/a04-content-pipeline/README.md)**: start at "For markers". The quick check is `bun install && bun run verify` (offline, 1–2.5 minutes; allow a 5-minute timeout).

## Assignment 5 submission (goal-oriented coding agent)

Branch `assignment-05`. Human and agent reviewers: start at **[courses/a05-goal-agent/README.md](courses/a05-goal-agent/README.md)**. Run `bun install --frozen-lockfile && bun run goals:verify` from the repository root (Bun 1.4+, .NET 10; allow 10 minutes). It tests the submitted simulation and replays the real Codex generation and unchanged rerun. No model, database, Docker or Unity is needed. New live generation is a separate, opt-in workflow with documented limits.

## Play the game

```sh
bun run game          # builds out/build/play/The Suborned.app if it is missing or out of date, then opens it full screen
bun run game -- --windowed
```

The Handover opens on its briefing. Esc opens the pause menu (Resume, Restart mission, Quit). `bun run game:build` forces a build; `bun run game:check` proves the built app (all scenarios against headless, then the briefing, play and menu shots with frame times, into `out/build/play/proof/`). A fresh build is proved to draw before it is trusted: Unity sometimes ships a player that draws only a black screen (a second build in one Editor session), so `game:build` and `game:check` run the proof after building and, if its frame is blank, build once more on their own. Building needs Unity 6000.6.2f1. With Unity closed the build runs in batch mode. With the project open in Unity it builds through the Editor (menu Suborned/Build Game), and refuses while Unity is in Play.

On another Mac (the MacBook Air):

- `bun run game:pack` zips the app to `out/build/play/TheSuborned-<sha>.zip`, and `bun run game:send [device]` sends it by Taildrop (default `skys-macbook-air-1`; `--dry-run` prints the command). No SSH is needed.
- On the Air, the zip lands in Downloads through the Tailscale app. Unzip it, then open the app by right-click → Open the first time, or run `xattr -dr com.apple.quarantine "The Suborned.app"` first. The app is signed ad hoc, not notarised.
- Or pull the branch on the Air and run `bun run game` there. That needs Unity 6000.6.2f1 on the Air.

Signing and notarisation, so the game opens on other Macs without "not verified":

- Run `bun run game:signing-check` to see what exists. Two things are needed, made once on this Mac:
  1. A "Developer ID Application" certificate: Xcode → Settings → Accounts → (team) → Manage Certificates → + → Developer ID Application. This needs the Account Holder role. You can also create it at developer.apple.com → Certificates and install it.
  2. A notary profile: make an app-specific password at appleid.apple.com, then run `xcrun notarytool store-credentials suborned-notary --apple-id <email> --team-id <TEAM>`. It asks for the password itself.
- With both in place, `bun run game:pack` does the rest:
  - signs under the hardened runtime with `tools/game/entitlements.plist` (allow-jit and disable-library-validation, the two the Mono player needs);
  - notarises (`notarytool submit --wait`), staples, re-zips, and checks with `codesign --verify` and `spctl`.
- Without them it packs the ad hoc build and prints what is missing.
- `SUBORNED_SIGN_ID` picks a different identity.

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
