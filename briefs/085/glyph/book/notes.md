# The book: proof-of-concept mock (brief 085)

Glyph, 2026-11-02. Mock only: no edits under `unity/Assets`, no GDD or `text.json` edits, no paid calls. Drawn from real data and the kit by `make/` (`python3 make.py`). Open `board.png` on the phone first.

| picture | state |
|---|---|
| `cell-page.png` | 0:05, Scout's page |
| `person-opening-guard.png` | 0:05, the guard, mostly unknown |
| `person-inez-turned.png` | 2:10, Inez after Watch, Listen and two Helps |
| `job-page.png` | 2:40, same run, the first station past |
| `board.png` | all four with readable crops, for the phone |

The run behind the 2:10 and 2:40 pages is the route script `contracts/missions/handover/routes/route-a-board.json`. Times are its order ticks plus the act's seconds, worked out by hand. The sim CLI's built binary wouldn't start here, so I read the rules rather than ran them; Keel can check with a dump. A real frame at 2:00 in that route family shows the same $60,000 and Trail 2.

## What each line reads
Every line is something the game holds and a rule reads (GDD §09), or text the mission already has. New words are draft for Fable.

**Your cell**
| line | reads |
|---|---|
| Line under each face | `text.json` `briefing.page.cell` |
| Watch, Listen, Tip + their sentences | `actions.ring-*` labels; `actions.watch`, `listen`, `mislead` `.does`, already written |
| 20 s, 30 s, 30 s | rulebook `watch-s`, `listen-s`, `mislead-s` |
| Sees furthest: 30 m | rulebook `watch-reach-scout-m` (draft wording) |
| On who, now; lock on Inez and the guard | `Game.ScoutQuery` per person, as `IntelNext` calls it; `needs-routine` |
| No rivals yet | `locks.no-rivals` |
| Free, Sidearm | `Op.Busy`, `Gun` (Handover: all four armed) |
| Everyone can | `actions.*.by` is all four |

**A person**
| line | reads |
|---|---|
| Name, job | as the Intel cards; Okafor once heard (mismatch in the panel notes) |
| The line under the name | `targets.<role>.about` until Watch, then `.watched` |
| Needs, Nature, Secret, "?" | `Person.KNeed`, `KTemperament`, `KSecret`, `Secret`, `Lover`, `NeedRecordOf`, as `IntelFrame` |
| Told: close to Inez | `briefing.hints.front-door` (text only, no rule) |
| cleared | `Person.NeedMet` |
| What we could try | same checks as `IntelNext`; ring labels |
| How we turned her: Help, twice, $40,000 | `Memory` (`help` entries) and the price lines `visits-share-*` |
| What she'll do: Call, Door 2:54 | `Person.Turned`, `actions.call.does` shortened, §07 window; the real figure comes from the price preview |
| Trust: four meters | `Person.Sc`, step words from `people.rules.scales`; loyalty 2 is two Helps × `loyalty-per-help`; fear 1 is Fearful's start |
| Questioned, she keeps quiet | `Rules.IsTalker(Sc)`: 1 + 0 > 0 + 2 is false |
| What she knows about us | `Game.FactsOf(person)`: `fixer-face`, `place-time` |
| Between us | `Memory` for what she remembers; the Watch and Listen come from the order log, as neither leaves a memory |

**The job**
| line | reads |
|---|---|
| Objective, Comes out at 4:50 | `briefing.objective`, `briefing.page.target` |
| What ends it | `briefing.fails`, cut short (draft) |
| Two ways in, whens, sentences | `briefing.page.heads`, `.ways`, `briefing.hints` |
| Turned, Called away, Camera 1:05, Inside | `Turned`; the guard's lure state; loop's run time; `Op.Inside` |
| So far | the history's own entries, shortened |
| The clock | `briefing.page.clock`, `.stations`, `briefing.timeline` |

The ways reuse the briefing page's words, so nothing new is spoiled. "The ways we've seen. There may be others." is new, so it doesn't read as the only routes.

## Left out on purpose
- **Backstory:** none. A bio with no rule reading it is rejected by §09.
- **A helped Inez warns of the ambush 10 s early:** a rule reads it, but saying so spoils the final fight. It can go on the page once it has happened.
- **Filters:** four people don't need them. The small inset shows the later shape: grouped by what people give (§02), counts made up and labelled so.

## How it joins up
- **Rail (option C):** a click on a face opens its card and moves the view. A click on that card's name opens that person's page. The rail stays the at-a-glance view.
- **Guide:** the last step points at the book's key once. Nothing else changes there.
- **The clock:** it stops as the pause menu stops it (`HostMenu.cs:19`, `pausedBeforeMenu`). The engine sees nothing, so a replay is untouched. Back to the street restores the earlier state.
- **Opening it:** the corner key that opens the notes today opens the book on the last page read.

## What it replaces
| today | with the book | lost? |
|---|---|---|
| I, Intel panel | I opens The people; the rail does the glance | Nothing, once C is in. Its next-step chip moves to C's card |
| L, history drawer | L opens The journal | **Yes, one thing:** the drawer reads while the clock runs; a book stops it. Keep the drawer until Sky tries it |
| B, briefing mid-run | The job covers the facts | Stills and plan stay on the briefing; keep B and link to it |
| Alt | unchanged | no |

## Reading
Words per page, counted by the mock: cell 152, guard 96, Inez 129, job 197. The briefing page's cap is 220. Body lines are 20 px or more, except labels and plates. I didn't run `page.ts` on these: its pins, stills and caption rules are written for the briefing, and the book needs its own. These pages read in seconds; the job page is the longest and the first to trim.

## Size against the panel and guide
| piece | size | what |
|---|---|---|
| Panel C | M | as in the people panel notes |
| Guide | M | as in the people panel notes |
| Book, whole | L | four tabs, one page per agent and person, journal, key changes, its budget rules, proof rows |
| **PoC: job + cell** | S to M | two read-only pages on `Briefing.cs`'s paper look; text already exists |
| PoC: + person page | adds S to M, after C | shares C's card data; adds Memory, Sc and FactsOf reads |

## Smallest worth building
**The job and cell pages, opened from one key, stopping the clock.** They answer "what do my agents do?" and "what am I trying to do?", which is Sky's reason for doing it now. They need no new data, and they prove the paper, the stop and the way back.

Add the person page once C is in, since it's C's card made larger. Leave out filters, the inset and the journal tab for now: L's drawer already does that job, and keeping it loses nothing.

Keep the guide: it gets people doing things, and the book is for looking things up. If only one can go first, build C and the guide for the new-player test, then the book.

## Assumptions and open questions
- **Motive and Sky to rule:** today no screen shows `Sc` or the Talker result. Showing them to the player is new information. The mock shows them only on turned people, on the idea that a contact you've turned is one you can read. If that gives too much, show the "Questioned" line only.
- **Keel to confirm:** Inez's numbers (from the rule tables), and that the guard's Ask is open at 0:05 (`ask` requires nothing; not run).
- **Rival car time:** 2:35 moves ±15 s by seed, so the journal's time would vary.
- **Stills:** cropped from the real briefing capture and enlarged, so they're soft. The shots tool could take sharper ones. The guard's still shows his model; his small face is still the brief 063 render.
- **Door 2:54 and Camera 1:05** are worked out from §07 and the loop's 2:00, not read from a run.
- **Words** are all draft for Fable, including Needs, Nature, Loyal, Afraid, Bitter, Brave and the section heads.
- **GDD:** building this needs a new §10 version, and `briefs/parked.md` has the idea parked until after mission two, so Sky's "by adding it now or at least PoC" should be recorded there.
