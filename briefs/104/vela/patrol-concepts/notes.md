# Brief 104 · Patrol robot concepts (Vela, 2026-11-10)

Sky: "create some concept work for a suborned patrol robot". Today's alarm robots are luggage porters with a lamp, so this is a real patrol robot, designed from an open brief.

**Board:** `briefs/104/vela/patrol-concepts/board.png` (1000 × 3743 px, for a phone; git-ignored like other renders under `briefs/`, so it stays on disk). Top to bottom: my top three · the three in the civic square at play size · every design at play size · the three sheets with each design's id.

## What I made: one pass, 3 images, $0
| Pass | Tool | Images | Cost | Ledger |
|---|---|---|---|---|
| `lineup-1` | Codex CLI image tool, through `tools/paid/paid.py codex` | `paid/104-patrol-robot/codex/A-civic.png` (1536 × 1024), `B-stern.png` (1920 × 819), `C-signal.png` (1774 × 887) | $0 | rows 1–3; row 4 is a note (no call) that they're used on this board |

Each sheet is six designs and a grey 1.75 m figure for scale, so 18 designs. All three came out right on the first take, so I stopped there. No paid calls.

## The prompts
Every prompt starts with the style contract's `concept_prompt` (era, city, avoid), word for word. Then comes the same open brief, which gives the robot's jobs and facts, not a design. Then the sheet layout, one direction line, and the finish. The full text of all three is in `prompts.json`, written by `make/prompts.py`.

- **The brief:** what the robot does (it walks a fixed round through the civic square and past the Records Office, notices people who linger, and reports them by radio). How it thinks (clockwork-smart, no networks). How it sees (only the way its face points; its back is blind). Its dot-matrix face in four stages, no cartoon eyes. A red alarm signal with its own shape. It is harder to stop than a person, and civic on the surface with authority underneath. One personality prop, plain enamel with no lettering. The colours, with "more colour, less shine". It must read from 38° above at 80 px, unlike a person, the guard, or the waiter, butler and porter robots. It must fit a street dock about 1 m wide. It must be original, with no domed astromech, pepper-pot, tin-man, bubble head or police cyborg.
- **A · Friendly civic:** "Approachable, rounded and courteous, like a well-loved public institution: the robot a child would wave at, still plainly on duty."
- **B · Quietly stern:** "Courteous but unmistakably official: upright, composed, still and watchful, the authority under the city's polish … calm rather than menacing, never military."
- **C · Signal and silhouette:** "Each robot's outline and top must be unmistakable from high above, and each carries its alarm signal as a defining shape of the design … Show every robot with its alarm signal lit."
- **Finish:** high-art gouache on matte paper, ink linework, matte enamel, satin chrome without hot highlights, plain paper backdrop, no text.

## The 18 designs (heights as drawn, against the sheet's 1.75 m figure)
| | 1 | 2 | 3 | 4 | 5 | 6 |
|---|---|---|---|---|---|---|
| **A** | beetle 1.2 m | stilt bird 2.0 | saucer 1.1 | unicycle 1.8 | rolling ball 1.1 | arch 2.0 |
| **B** | car front 1.5 | pillar 2.1 | warden 1.9 | ticket walker 1.5 | cabinet 2.2 | manta 1.8 |
| **C** | handle 1.1 | crest 2.0 | crescent 1.6 | deco tower 1.9 | zigzag 1.2 | hoop 1.8 |

## What survives at play zoom
I cut every design out (`make/cut.py`) and drew it at play size beside Inez as build 0.2.303 draws her (`make/strip.py`). I also pasted the top three into the real civic square shot at 1:1, beside the cell.
- **A lit alarm signal is the strongest read.** C2's crest, C3's horns, C4's twin horns and C5's zigzag show from across the square. The unlit fins on sheets A and B read only as small dark shapes.
- **Low, wide bodies can't be mistaken for anyone:** B1, B6, C5, A1.
- **Tall, slim uprights read as street furniture**, a lamp or a post box: B3 the warden, A4 the unicycle, B2 the pillar. B3 is the most civic idea on paper (a brimmed canopy and a round stop paddle), but at play size it reads like a lamp.
- **Tall designs:** 10 of the 18 are drawn 1.8 m or taller. The dock and the plan's robot allow about 1.8 m, so a pick gets sized in its turnaround.

## My top three
1. **C2 · crest.** Its alarm is its outline: a red crest that sweeps back, so you can see which way it faces from above, even unlit. Broad shoulders give it authority without armour, and its face sits high, where the play camera sees it. Two legs. The crest folds or shortens for the dock.
2. **B1 · car front.** Low and long, so it can't be mistaken for a person or the upright service robots. Grille face at the front, fin at the back, as plain as a 1950s car; the dark red fin is its alarm. It needs narrowing and shortening for the dock.
3. **C5 · zigzag.** The zigzag crown is the clearest alarm shape on any sheet, and it reports on a telephone handset: by voice and wire, the way this city works. It needs narrowing for the dock. Six legs fit the 22-bone crowd rig only at two bones a leg.

**Props worth borrowing for any pick:** B3's round stop paddle, B4's report-slip printer, and the stamps (C2, C4, C6) and telephone handsets (C1, C3, C5) on sheet C. All of them are paper and voice, no networks.

## Open, for Aster
- **Sight height.** The rules give the patrol robot's eye at 0.95 m and the response robots' at 1.28 m (GDD §05 sight). As drawn, C2's face is centred at about 1.35 m, B1's at about 0.8 m and C5's at about 0.65 m (measured on the sheets). Whether the rule follows the model is Keel's call, once Sky picks.
- **The response robots.** The patrol and the response could share the pick, with the alarm signal lit on the response, as the robot board proposed. That's Sky's choice; it isn't part of this board.

## Next, if Sky picks one
1. **A turnaround sheet of the pick:** front, side, back and top. It shows the four dot-matrix faces (calm, noticed, wary, reporting), the alarm signal dark and lit, and a check from the 38° play camera. Codex, $0.
2. **The 3D piece in the DecoKit.** It gets the dot-matrix face the engine drives (`M_RobotFace` stages, as the patrol robot has today). It is sized for the dock (0.98 m wide inside, about 1.8 m) and to the crowd budget (about 12k triangles, 22 bones, LODs). Then Latch installs it.

Nothing in `unity/`, the kit or the GDD was touched.

## Remake ($0, from the repo root)
```
python3 -I briefs/104/vela/patrol-concepts/make/prompts.py                     # prompts.json; add A, B or C to print one prompt
python3 -I tools/paid/paid.py codex 104-patrol-robot lineup-1 --name A-civic.png --prompt "$(python3 -I briefs/104/vela/patrol-concepts/make/prompts.py A)"
S=out/scratch/vela/patrol-concepts
for k in A-civic:A B-stern:B C-signal:C; do python3 -I briefs/104/vela/patrol-concepts/make/cut.py paid/104-patrol-robot/codex/${k%%:*}.png $S/cuts/${k##*:} ${k##*:}; done
python3 -I briefs/104/vela/patrol-concepts/make/board.py $S/cuts out/scratch/vela/robots/play briefs/104/vela/patrol-concepts/board.png
```
The board also reads today's robots rendered with the play camera (`out/scratch/vela/robots/play`, from `briefs/104/vela/robots/make/render.py`) and the game shots of build 0.2.303 (`out/batch/shots/cafe-front-day.png` for Inez, `start-overhead-day.png` for the square).
