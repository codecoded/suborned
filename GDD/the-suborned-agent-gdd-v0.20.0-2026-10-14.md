# THE SUBORNED · AGENT GDD v0.20.0 · 2026-10-14
src: the-suborned-gdd-v0.20.0-2026-10-14.html. On conflict the src wins; report the mismatch to Aster. §13 TESTS exists only here. §15 ART = the art bible; its machine copy contracts/style/suborned.style.json (v0.20.0) wins on conflict. The v0.15.0 html art bible is history (~/Projects/the-suborned/GDD/v0.15.0/): concept look reference only; its painted presentation and palette are superseded.
tags: [B] built, proven in top-down test · [S] spec, starting value, tune by playtest · [L] after capstone
load: every agent = §01 + its own §14 block · Aster = §01–§13 + its §14 block · Vela, Forma, Glint, Glyph also §15 · open other sections by number on demand
units: s seconds · m:ss mission clock · $ cash · T Trail · A Alert
v0.20.0 (Sky, 2026-10-14; briefs 008, 009): world premise: an alternate 1950s that discovered atomic power centuries ago; no thinking machines or networks; people are the weak point (§01, §15) · red allowed on small emissive display pictograms, never a surface (§10, §15) · concept art first: look decisions come from concept art made from an open brief; ΔE and checker rules gate it (§15; style contract concept_prompt) · crossings: floating signal orbs, light flush in the road in brass frames; no zebra, paint or landing slab (§15; brief 009) · reference machine = the 15-inch MacBook Air M4, Mac16,13 (§11) · log in §12 decisions.

## §01 GAME + CORE (loaded first by every agent)
game: single-player real-time tactics + social infiltration · PC · Unity · camera above street, never first person · exteriors only (interiors = timed off-screen actions; shops, bars and workshops are frontages) · multi-level exteriors: street 0, deck 9.43 m, sky concourse 24 m (sky rail platforms) · mission = 1 block, 8–12 min, live clock
fantasy: commander. The city carries out your plan without knowing it: turn ordinary people, time their small acts into one plan.
arc: one person → networks → districts. Every system adds to or tests that growth.
look: bright retro-future (pastel cream + peach stone, saturated enamel accents, lagoon water, coral transit, chrome; skyline of towers, rockets, planets). Optimistic surface, coercive underneath.
world (Sky, 2026-10-14): an alternate history, not a forecast: "a 1950s world that discovered atomic power centuries ago". Culture and style 1950s, technology centuries ahead ("robots have 1950's style, the trams etc, just the technology is super advanced"). Not "the 1950s imagining the year 2000".
- atomic power made energy, travel and building cheap (sky rail, skyports, towers, liners): everything physical is abundant.
- thinking machines and networks never came ("internet wasn't a thing"): no networked computing; robots are clockwork-smart and follow fixed routines; records are physical (microfilm, punch cards, pneumatic tubes, stamped forms). Information moves through people and paper, so people are the weak point: why the game is about suborning them (pillar subvert people; tiebreaker people > powers).
- the city fears disorder most: an abundant, orderly city runs on routine and paperwork; hence optimistic surface, coercive underneath.
- design test for every object: 1950s style, centuries-ahead technology (crossing signals float, markings need not be paint; brief 009).
- reads (rules unchanged): cameras are closed-circuit, watched by people · a watcher's log is a physical record that investigators pull · calls are voice between people (telephone, radio) · "networks" in the arc and transit = people and lines, never computers · "no AI at runtime", fight AI and AI chatter are build terms, not the world.
pillars (test):
- subvert people: does knowing more make a person cheaper, quieter or more loyal?
- orchestrate the city: does it open or close a window the player can see coming?
- live with it: will the player meet this choice again?
tiebreakers, in order: readable > rich · people > powers · systems > scripts (nothing hand-authored) · preparation > improvisation · commander > avatar · exteriors only
terms: Trail = campaign evidence from traceable acts, drives investigations · Alert 0–3 = security response now (routine, inquiry, search, lockdown) · Standing = Hero ↔ Menace per district, witnessed acts only · Heat = T + Menace, threshold schedules a fight · bravery (helped → Fighter) / resentment (coerced → Informer) · Talker = fear + resentment > bravery + loyalty, gives you up when questioned
house rules:
- build only against Keel's rulebook (each event: needs, changes, witnesses). Unknown = reject.
- no AI at runtime. Content generated at build time. Same orders → same result. Any run replays exactly from start state + orders.
- handoff = inputs · done-check · next owner.
- only Latch edits Unity scenes. Price paid runs before starting.
- escalate to Sky (via Aster) only: pillar change, new paid spend, release, destructive action, unresolved conflict.

## §02 LOOP + RESOURCES
mission: Watch → Learn → Turn → Time → Take → Fight → Inherit
campaign: turned people pay cash/arms/tips → turn more people (snowball). Contacts grow braver or bitter. T + Standing set pushback.
| resource | scope | drives | from |
| Cash | campaign | fees, debts, bribes, turning | sources (skimming clerk, bookmaker) |
| Arms | campaign | winning fights, arming joiners | sources (quartermaster, hijack) |
| Assets | campaign | access, income, fight allies | Fixer, introductions |
| Time | mission | actions 6–62 s; events don't wait | — |
| Trail | campaign | investigations; part of Heat | traceable acts: threats, visible payments, gunfire |
| Alert 0–3 | mission | security response now | witness reports, found faults, shots |
| Standing | district | who agrees, crowd behaviour | witnessed acts only |
rule: every order shows full price before commit ($, time, T, A risk).
travel [S]: boarding a tram or sky rail is fast travel round the city: board at any stop or station, ride to any other on the network. Price = time only: wait for the next service + the ride, both from the timetable. No fare.
chain: Handover route A is itself a chain (watch café → clear Inez's debt → loop camera → coffee spill pulls guard → take Vale). Each person sees only their part; city investigates backward, link by link [S]. Later operations chain more links across a district [L].

## §03 CELL
| op | verbs | notes |
| Scout | Watch, Listen, Mislead | reveals routine/need/temperament; false tips send rivals away |
| Fixer | Turn, Call | offer/pay/help/threaten; calls contacts in on cue; the face witnesses remember |
| Tech | Loop, Reroute | blinds surveillance, misdirects patrols; tethered while holding |
| Enforcer | Enter, Take, Fire | only op who carries the target or shoots |
training: full cell. campaign: starts Fixer only; recruit the rest by turning people with the skill [S].

## §04 PEOPLE
person = routine + need + temperament + memory. Knowledge lowers price; standing decides who listens.
approaches (Inez, training) [B]:
| approach | requires | $ | time | T | leaves |
| Ask cold | — | 0 | 25 s | +1 | remembered refusal: later offers +$10,000, +1 T |
| Pay | routine known (Watch 52 s) | 70,000 | 50 s | +2 | traceable payment; neutral |
| Help (clear debt) | need known (Listen 42 s) | 40,000 | 50 s | +1 | gratitude; bravery can grow |
| Pressure | need known | 0 | 35 s | +3 | fear; resentment grows; can identify Fixer |
purse: pay + driver = 110,000 > 100,000; help + driver = 80,000 fits → learning buys the second route.
temperament [S]:
| type | responds to | Hero | Menace |
| Idealist | Help; refuses bribes | volunteers free; bravery fast | refuses; may report |
| Mercenary | Pay | overcharges | discount; sells you to highest bidder |
| Fearful | Pressure | hesitates; needs Help ×2 | folds free |
| Loyal (institution) | trust over time | turns after repeated Help | reports any approach |
Hero and Menace campaigns turn different halves of the city = replay value.
bravery [S]: Informant → Helper → Accomplice (hides op, drives) → Fighter (joins armed). +1 step per job completed unexposed.
resentment [S]: Compliant → Resentful → Informer (sells a route → ambush scheduled later). +1 step per pressure or exposure.
talker [S]: questioned, a person gives up known facts if fear + resentment > bravery + loyalty (deterministic). Facts: saw Fixer's face, knows cell car, saw where/when. Brave helped contact lies → nothing or false lead.
between missions: promoted (new access) · arrested (lost unless freed) · introduces a friend (new contact). Debrief reports each with cause.
standing: per district, Hero ↔ Menace, witnessed acts only. Hero: shield civilians, clear debts publicly, free arrested locals. Menace: shoot in crowds, public threats, harm bystanders. Unwitnessed threat: no change (victim still knows). Neither end loses.
| | Hero | Menace |
| turning | slower, dearer, loyal | faster, cheaper, betrayal risk |
| crowds | join fights, hide ops | obey from fear, report, scatter under fire |
| fights | fewer; police hesitate in crowds | more often; harder |
| street | graffiti, cheering, free tips | wanted posters, averted eyes, empty pavements |

## §05 PRESSURE
urgency = scheduled events with reasons, never an arbitrary countdown. Each mission defines its closing event.
security [B]: guard on door routine (café stop pre-shift) · cameras: fixed visible cones, blocked by roofs/parked vehicles · patrol sweep, breaks to gunfire within 30 m · response unit on report; district seals 2:25 after it arrives.
rivals [B]: same target, own schedule. False tip: away 3:50. Gunfire: arrive within 16 s.
| A | security does | raised by |
| 0 routine | follows schedules | — |
| 1 inquiry [S] | checks spot; patrol tightens | witness report or found fault, once per incident |
| 2 search [B] | response unit to reported route | 2nd incident or any gunfire |
| 3 lockdown [B] | doors lock; district seals | violence seen by a civilian → 3 at once |
witness [S]: clear sightline within 13 m of a traceable act → +1 T per incident, moves standing. Crowds also block guard/camera sightlines.
levels [S]: street, deck and sky concourse are all exterior. Sightlines and the 13 m run in 3D: a slab blocks sight straight up and down, an open edge does not. People change level only by stair, ramp, escalator or lift (time from the plan). Watchers stand per level; a station watcher covers its concourse.
watchers [S]: cameras and civic robots stand at key places on the district plan (stations, stops, gates, squares). Any op in a watcher's sightline is logged with place + time, act or no act. Once a Trail is open, investigators can pull the log: each sighting is a lead (place + time). Hot (Heat over threshold, Motive sets it) or Menace standing in the district: a sighting counts as a witness report (Alert table).
investigators [S][L]: on foot question ~every 20 s; cars move fast, stop to question; count rises with A and T. Pick civilians near last incident by sightline + distance, not luck. Useful answer = fact with place + time → go there, question again; ~60–90 s per hop.
visible: questioning icon, dotted line joining held leads, unit following it.
counters: keep moving (sightings go stale) · reach witness first (pay/reassure/threaten; threat adds fear) · Scout false tip · citizens block cars · break sightlines in a crowd.
output: findings → debrief Investigation state → next mission. Hero campaign: brave locals give false leads.

## §06 FIGHTS
fights measure preparation (arms, brave contacts, streets). Not a separate combat game.
frequency [S]: Heat = campaign T + Menace standing. Threshold crossed → fight event (raid, ambush, rival hit) in next mission. No dice.
size [S]: act I 3–6 hostiles · II 8–15 · III 20–30. Hard cap 30 simulated combatants; rest = background crowd (run/cheer only).
your side: cell + Accomplice/Fighter contacts + crowd joiners (Hero), armed up to Arms stock.
citizen actions: scatter (breaks sightlines, covers retreat) · block (lorry, stalled tram, crowd vs police vehicles) · join (bare-handed or armed) · vehicle strike (ram barricade or rigged vehicle; driver always jumps clear).
content line: civilians are never the payload; no coerced suicide attacks. Menace can be uglier (collateral, witnesses) but no civilian dies as the weapon.
health [S][L]: civilian 2 · guard/officer 3 · rival 3 · patrol unit 4 · Scout/Fixer/Tech 4 · Enforcer 6. Bars on select and for all in a fight. Weapons (Arms): pistol 1 dmg, heavier 2–3. Armour via quartermaster, healing via turned doctor (people, not upgrades).
downed: op at 0 = downed; carry to car, or captured → rescue mission.
civilians targetable: every witness → Menace; contact who witnesses loses bravery, gains fear + resentment → some Informer, more Talkers. Fastest way to undo progress.
prototype [B]: Enforcer shot 6 s; 2 hits drop a person or patrol unit; Enforcer takes 3. Each shot +1 A, +2 T, draws rivals + patrols, Menace if witnessed. Shooting a contact ends the relationship. Shooting the target = fail. T2 decides whether costs rise.
decisive shot [L]: 1–2× per fight: slow time, camera behind Enforcer, one aimed shot, back to tactical.

## §07 HANDOVER (training mission · capstone)
brief: Councillor Vale signed the clearance order for the marked block. At shift handover he leaves the Civic Records Office for his official car. Take him alive to the getaway car. Full cell. Purse $100,000. Every verb introduced once.
cast: Vale · Inez (café waitress, in debt) · guard · official driver (needs cash) · rival crew · cameras · patrol.
schedule: 0:00 Vale inside; routines run; cell at getaway car · 3:10 rival car parks near pickup · 5:30 handover chime · 5:40 last chance to turn driver · 5:50 Vale exits north (service lane if driver turned) · 6:10 Vale at car · 8:00 rivals take Vale or car leaves = over.
route A, front door (before 5:50): Scout watches café, listens to Inez (1:34) → Fixer helps Inez ($40,000) → Tech loops entrance camera (1:02, tethered; optional patrol reroute 1:55) → Fixer calls Inez: guard walks 30 s, stays 60 s, returns 30 s = 2:00 window [payoff cut] → Enforcer enters (38 s), exits service door with Vale, to car (44 s).
route B, pickup (from 5:50): Scout watches car: driver needs cash (48 s) → Fixer turns driver before 5:40 ($40,000): pickup moves to service lane, or driver blocks pickup camera 2:30 → Scout false tip on rivals (44 s), or fight → Enforcer intercepts (38 s), to car (44 s).
final fight [S]: always; 30–60 s; 3–5 hostiles.
- loud (any shot or A ≥ 2): police from west street, 2–4 officers, seal clock running, no allies.
- safe (no shots, A ≤ 1): rivals ambush extraction; guard (never saw you) fights them; helped Inez warns 10 s early; turned driver blocks rival car (−1 rival); earlier false tip (−1 rival).
win: Vale alive at getaway car.
fail: Vale killed · Vale leaves in official car · rivals reach Vale · district seals · Enforcer down · entry seen by guard, live camera or patrol. Withdraw ends the attempt.
reference case: the mission generator must reproduce a mission of Handover's quality; the checker must pass it.

## §08 CAMPAIGN
acts: I one person (Fixer alone; recruit cell; first income; fights 3–6) · II networks (introductions; take a workplace e.g. docks, depot; brave contacts fight; 8–15) · III districts (streets rise or obey by standing; institutions + rival factions field squads; 20–30).
economy [S]: turned people become sources paying each mission: clerk skims cash, quartermaster diverts arms, café owner tips needs before Scout listens. Procurement missions target a source. Snowball.
wounds [S]: health carries over. Turned doctor heals fast and quietly. No doctor → optional pre-mission objective to watch/turn/pay one, if calendar allows. Main mission date never moves. Choice: go in hurt, short-handed, or spend the days.
objectives [S]: kidnapping · bank job via planted teller · killing by manipulated proxy · takeover via ruined reputation. Direct force always works, with bigger T.
debrief [B] → next mission:
| output | states | from |
| public security | Calm → Raised → High | final A; any shot fired |
| investigation | little → early → active (named cause) | T 0 / 1–2 / 3+; named witnesses take precedence |
| assets | e.g. Inez introduces café contacts; Inez may identify Fixer | how turned, what they saw |
| standing, bravery, resentment, Heat [S] | per district / contact | witnessed acts, jobs, pressure used |
replay freely before Continue; after, the result stands.

## §09 GENERATION
nothing hand-authored. Sky writes rules + generators. The simulation plays every generated mission before a player sees it. Build time only.
| content | from | checked by |
| people | occupation (from the trades on their block) × temperament × need × relationships × routine, fit to district schedules | reach their places; one clear need |
| missions | template (objective, target, closing event, fight trigger) + district people | mission checker |
| dialogue, portraits, voices | person data + state (brave, resentful, promoted) | tone + rating filters; sampled review |
| district plans | Datum (roads, transit, plots, gates, levels, megastructures, frontage program) + Vantage (block layouts) inside plan.standards | plan checker exit 0 (bun tools/plan/cli.ts) |
| world | generator fills each plot from plan.standards, seeded by plot id: masses, towers, roofs, frontage trades, decks, stairs, lifts; 2–4 megastructures per district from the plan; each element dressed by a DecoKit piece (contracts/world/kit.json) or procedural to the same bar, seeded by id | plan checker (B, L, MS, F rules) · same plan → same world |
| consequences | rules on mission log | replay → same result |
mission checker: ships only if ≥ 2 quiet routes win · loud route exists and costs more T · every fail condition reachable · no single contact essential · fully informed player finishes ≥ 1:00 before closing event.
risk: sameness (Watch Dogs: Legion). Fix: history, not backstory; people are memorable via what the systems record. Text = light layer. Live AI chatter [L] only if it never touches rules.

## §10 PRESENTATION
camera: fixed tactical view, play zoom 14 at 38°, never first person. Capstone = 3D world built from the plan (DecoKit kit where it counts, procedural fill to the same bar); rules unchanged. Painted plates [L] optional extra at most. Characters 40–60 px tall at 1080p (job, silhouette, facing readable). Levels: architecture above the selected person's level ghosts (§15: 0.2, keeping 0.4 of its shadow).
split: built world (generated from the plan at build time, fixed in play) = buildings, paving, structures, street furniture, skyline. Live 3D state = people, vehicles, doors, surveillance, barricades, posters, anything with state. One material + light standard for both; match lighting on 1 character + 1 vehicle before building more.
controls: click select/move · Shift group · 1–4 operative · E engage · F fire · C call · click person/door/camera → action card with full price · Next Event jumps to next scheduled change · live clock + tactical pause (T1).
readability: cones red · guard amber · rivals violet, drawn unlit and emissive and outside the post grade (the grade shifts them ΔE 4–10), so neither light nor grade shifts them: a signal never depends on the light; each signal also has a shape or motion cue. No environment surface uses a signal colour at full strength: every world colour ≥ ΔE2000 20 from each signal, measured on the unlit palette (lit and shaded surfaces can't hold 20). One exception (Sky, 2026-10-14: "red is definitely ok for electronics displays"): a small emissive pictogram on an electronic display (a crossing signal's stop figure) may be red; never a surface; the cones keep their own shape and motion cue. Dossier separates known / suspected / planned.
payoff cut: plan lands → brief street-level cut → same state. Capstone has one (Inez spills coffee, guard walks off). More cuts + decisive shot [L].
audio = clock: bell, café door, 5:30 handover chime · surveillance click at sweep edge · patrol sound changes when it breaks route · Alert adds music layers; fight = full score · contacts confirm calls aloud.
look: §15 ART + style contract; palette current, tuneable (Sky reviews it in the world). Retro-future city; cell = visitors from a nearby timeline (sharper, cleaner, no deco ornament) so the four read as outsiders at 50 px.
skyline (Harbour District; Sky, brief 007): SpaceHQ = the city's flagship and centrepiece at Crown Circus, the tallest thing in the district (~252 m as built), crowned with an atom over a needle mast, not a rocket · rival spires stand inland and may be taller than the Skyport: 123–194 m, ceiling 200 (≤ 0.8 × SpaceHQ) · the Skyport (108 m) is the harbour's icon: nothing within 150 m of the quay is taller · Crown Avenue keeps its tower heights (needle 167.8 m, avenue gates 165.6 m), an exception to the massing grammar's 20 % height variety (harbour-district.grammar.md §4; Sky, 2026-10-14) · rockets only on the horizon, ≥ 3.2 km out, never inside the district · the sky rail threads buildings and megastructures through portals; stations also sit inside buildings at the sky concourse (24 m) · the Parade stays open to the sky: the sky rail passes through the gate block's portal; glazed barrel canopies run over the side gallery promenades only, never over the carriageway or the play space · the ocean liner is concept B, "atomic" (briefs/007/liner-B-*.jpg; §15).
reskin rule: any era must provide watching institutions (for Tech), vehicles + dense crowds, jobs readable from clothing, respectable surface over coercive machine, firearms + plausible rigged vehicle.

## §11 BUILD
built: top-down test of Handover (4 ops, 2 contacts, security, rivals, shooting, debrief, campaign lock; 28 scenarios pass) · Harbour District 3D world from the plan in progress (generator + export, DecoKit + procedural, Unity builder; briefs 004, 006) · city sim architecture chosen (brief 005 A).
architecture: rules separate from engine · events carry conditions, effects, witnesses · exact replay from start state + orders · Unity must pass the same 28 scenarios before it's trusted · one C# sim engine as a Unity-free library: Unity runs it live, build tools headless via dotnet; 10 Hz fixed tick, integer state, replay = orders + checksums · performance = frame rate at play zoom, not polygons: target 60 fps at 1080p on the reference machine, a 15-inch MacBook Air M4 (Mac16,13; no fan, 10-core GPU), measured warm (after several minutes of running, since it throttles) at play zoom with people, vehicles and the simulation in, by a perf harness in a development build (Redline specifies it, Latch builds it); a mid-range PC (RTX 3060-class GPU, 6-core CPU) must also pass, as a check, not the bar.
capstone, 5 weeks:
| wk | deliverable | exit |
| 1 | rules proven in top-down test | 2 routes win; loud fails at a cost [done] |
| 2 | rules in Unity; Handover in the 3D world | 28 scenarios pass in Unity |
| 3 | cell control, contacts, security, rivals, readability | first uncoached playtest |
| 4 | final fight both branches, payoff cut, dossier, event log, debrief | new-player test; freeze rules |
| 5 | tuning, fixes, audio cues; no new systems | final uncoached gate |
gate: uncoached player can state objective + fail conditions, pick an approach from something learned, explain its $/time/T cost, complete a chain, say why police vs rivals, read what next mission inherits.
cut order if late: crowd witnesses (decorative) → patrol reroute → camera-block variant → police branch (rivals only). Never cut: both routes, one payoff cut, debrief, gate test.
beyond: vertical slice (1 district, 2–3 generated missions, temperament, standing, small fights) 6–10 wks · full game (3 acts, 3 districts, fights ≤ 30) ~12 months. Replace with measured velocity after capstone.
risks: combat feel + animation (only walk/run exist) · generated sameness · playtest time. Fixed limits: 30 combatants, vehicles on lanes, 3 districts, no interiors, no first person.

## §12 CREW
Aster leads 16 agents; Sky is the only human. Loop: assess → assign → review → continue. One owner per objective with a done-check; Suture/Redline review; Aster accepts or returns.
models: Opus 5.5 = Aster, Motive, Keel, Latch, Waymark, Datum, Vantage, Suture, Redline · GPT 6.1 Sol = Glyph, Fable, Pulse, Splice, Vela, Forma, Glint, Chime.
tools: Runware (Glyph, Vela, Forma, Chime; concepts incl. Qwen Image 2.1, priced per run) · Codex CLI image tool (ChatGPT plan, cost 0 per run; concepts) · every concept starts from the style contract's concept_prompt (§15) · Blender: DecoKit generator tools/decokit (bun run decokit, headless, build time): Vela owns the pieces, materials and facade detail, Splice the pipeline (run, orders, kit.json catalogue, bun run kit, same plan → same kit); Forma people; Glint previews · plan checker (Splice owns; Datum, Vantage run it before handoff) · Unity (Latch owns; Splice, Glint, Suture check).
order: plan before dress: Datum plans the district, Vantage the blocks; Latch builds only a passing plan; art dresses the built world (kit + procedural). Motive + Keel draft rulebook first; others work in parallel against the draft; Latch owns critical path; rulebook frozen after week 4.
budget: two 20× subscriptions (one Codex, one Claude). Aster drops optional work when allowance is low. Per-use credits (Runware, Meshy) priced first.
decisions (v0.20.0, 2026-10-14; Sky unless marked):
- Sky (brief 008, Era): the world premise (§01): an alternate history, not a forecast: "a 1950s world that discovered atomic power centuries ago" ("retro future was 1950's but if the world had discovered atomic power hundreds years ago"; "robots have 1950's style, the trams etc, just the technology is super advanced"). It replaces "the 1950s imagining the year 2000" (brief 007's image prompts). Atomic power made energy, travel and building cheap; everything physical is abundant. Thinking machines and networks never came ("internet wasn't a thing"): robots are clockwork-smart on fixed routines, records are physical, information moves through people and paper, so people are the weak point. The city fears disorder most: the reason for "optimistic surface, coercive underneath". Design test for every object: 1950s style, centuries-ahead technology (§15; style contract concept_prompt.era).
- Aster: both GDDs and the style contract swept against the premise: no hard contradiction. Clear compatible readings recorded in §01 (closed-circuit cameras watched by people; a watcher's log is a physical record; calls are voice between people; "networks" = people and transit lines; AI terms are build terms, and "no AI at runtime" stays a build rule). Readings that would change a mechanic went to Sky as questions, unedited.
- Sky (brief 009): red is allowed on electronic displays ("red is definitely ok for electronics displays"): a small emissive display pictogram (a crossing signal's stop figure) may be red; never a surface; the cones keep their own shape and motion cue. An exception to the ΔE2000 ≥ 20 signal-colour rule for display pictograms only (§10, §15, style contract signal_colours).
- Sky: concept art first ("everything should come from concept art really"; "let the AI concept art come up with its own design"). Look decisions come from concept art generated from an open brief in the world's style; ΔE and checker rules gate what the art proposes, they are not the source of options. Tools: the Codex CLI image tool (ChatGPT plan) and Runware (Qwen Image 2.1). Every concept generation starts from the style contract's concept_prompt (era, city, avoid, rule); its city line is the prompts Sky accepted in briefs/009/concepts/PROVENANCE.json; a decision from art cites the image and its prompt (§12 tools, §15).
- Sky (brief 009): crossings as this world builds them; briefs/009/concepts/ are the reference. Floating signal orbs ("I do like those eyes"): chrome orbs or small saucers hovering over the kerb at each end, a round display on each face with a walking figure, a thin beam down to a glowing disc where people wait. No zebra, no paint, no landing slab: light set flush in the road (glowing seafoam bars or rows of light dots) in brass frames; brass dome studs along the kerb edge; seafoam inlay arcs where the crossing lands; brass stays a frame line (SF-21). The landing surface (look role `crossing`, v0.19.0) goes with the slab, so its colour pick is closed (§15).
- Fact (brief 008 B6): the reference machine measured is the 15-inch MacBook Air M4 (Mac16,13) (§11, §14 Redline).
decisions (v0.19.0, 2026-10-14; Sky, brief 008 unless marked):
- Sky: the reference for the civic block and the district's street-level character is the eight concept paintings of 2026-09-26 (briefs/008/concepts/, hashes in PROVENANCE.json): cream stone, seafoam teal bands, chrome spheres, banners, planted terraces, pale paving with teal bands, cypress and palms, parasol terraces. The skyline keeps the AtomicAge and Blocks sheets (brief 007). The 30 Sep capstone concept and its testbed models are not the reference; their dressing parts may be reused (§15).
- Sky: civic palette: seafoam #72B5A7 and deep teal #1F6C67 are the kit enamels on the civic block; civic wall ivory civic_w #EEE9DA, pale seafoam #9BCFC2, civic trim brass #99784D. Banners, flowers and any tram take the contract's coral, flamingo or peach, never the paintings' red-orange (ΔE 8.8 from the cones signal) (§15).
- Sky: brass, not gold, at street level and on facades ("I prefer brass to gold"): brass #99784D, light brass #AD866F, the lightest that holds ΔE2000 ≥ 20 from the guard (a lighter, yellower brass drifts onto the guard amber). Skyline crowns keep their gold (#E3D681, #C9C08E) until a far shot decides. Gold or brass stays hairline, frame, bead and small metal only; a large part never takes brass or gold; it takes an enamel, chrome or stone role (the rooftop spheres keep their building's enamel; the two areas that listed gold, lantern and records, take sunshine #F8E26C), since the paintings show chrome and pearl spheres and roof parts take their host building's enamel pair. Crossing landings have their own surface; its colour is Sky's open pick (recommended accent_mint #7FD0B8; today #D0C848) (§15, style contract v0.19.0).
- Sky (brief 007): Crown Avenue keeps its tower heights: the needle 167.8 m and the avenue gates 165.6 m, 13.6 % and 14.8 % under the starburst, an exception to the massing grammar's 20 % height variety (harbour-district.grammar.md §4). Plan 0.3.0 unchanged; checker rule K-2 holds every regenerated tower to the tip its order states (§10).
- Aster (008 B5): signals also draw outside the post grade, since the grade shifts them ΔE 4–10 (Glint measured the guard 19.0 from shaded stone). A signal must never depend on the light: the fix is the signal, never a darker street. Glyph specifies, Latch builds (§10, §15).
- Aster (008 B5, Glint's value): the architecture fade's ghost keeps 0.4 of its shadow (light.json fade.ghostShadow; Glint tunes), separate from the ghost's own 0.2, so the street behind a faded tower is lit and the tower still marks the ground (§15).
decisions (v0.18.0, 2026-10-13; Sky, brief 007):
- Sky: signals (cones, guard and rival markers) render unlit and emissive, each with its shape or motion cue; the ΔE2000 ≥ 20 gate applies to the unlit palette against the signal colours (§10, §15).
- Sky: performance target 60 fps at 1080p on a MacBook Air M4, the reference machine (no fan, 10-core GPU), measured warm at play zoom with people, vehicles and the simulation in, by a perf harness in a development build; a mid-range PC (RTX 3060-class GPU, 6-core CPU) must also pass, as a check. This replaces the PC-only target set earlier the same day in brief 007: the play camera sees a small slice of the city, so integrated graphics is the bar (§11).
- Sky: SpaceHQ is the city's flagship and centrepiece at Crown Circus (~252 m), crowned with an atom over a needle mast, not a rocket; rockets stay on the horizon (≥ 3.2 km). Spires may be taller than the Skyport: inland rivals reach 123–194 m; the Skyport (108 m) is the harbour's icon and nothing within 150 m of the quay is taller (§10).
- Sky: the sky rail threads buildings through portals; stations also sit inside buildings at the sky concourse (§10).
- Sky: the Parade stays open to the sky: the sky rail passes through the gate block's portal; glazed barrel canopies run over the side gallery promenades only, never over the carriageway or the play space (§10).
- Sky: the ocean liner is concept B, "atomic" (briefs/007/liner-B-*.jpg); it replaces the OceanLiner that read as a modern cruise ship. Owner: Vela (§15).
- Sky: palette tuned in the world (§15, style contract v0.18.0): lagoon water #3d8d93 · gold #d0c848 · canvas #f7f4ec · accent roles teal, coral, gold, mint, peach.
decisions (v0.17.0, 2026-10-12):
- Sky: presentation 3D (world built from the plan; DecoKit + procedural; painted plates [L] optional). Camera fixed tactical, zoom 14 at 38°.
- Sky: palette = pastel base + saturated enamel accents (§15); current, tuneable after Sky sees it in the world.
- Aster: DecoKit generator (tools/decokit) = Vela (pieces, materials, facade detail) + Splice (pipeline, catalogue, import, determinism).
- Aster: C# sim library (005 A) = Keel builds and owns it (the rules live in it; Keel measured it); Latch owns the Unity host (live stepping, playback); Redline reviews code + perf; Suture replays. Open for Sky: confirm (alternative: Latch builds to Keel's spec; one more handoff on the critical path).
- Aster: Splice's load cut to the checks + plan → world pipeline. Moves: people + vehicle generator and life builder (tools/life) → Pulse once Splice's 005 move lands; mission rule overlays (tools/missions) → Waymark. Designers own their generators (as Waymark, Fable).

## §13 TESTS (agent copy only; not in the human GDD)
default: 6 new players.
| id | question | method | pass |
| T1 | tactical pause? | same build, 6 each way | pause wins unless no-pause players explain failures as well and rate tension higher |
| T2 | loud dominates? | log first-shot time + route | < 25% shoot before 2:00; informed quiet runs finish faster |
| T3 | Trail readable? | ask "why is the investigation active?" | ≥ 5/6 give correct cause |
| T4 | witnesses add decisions? | A/B witnesses on/off | players route around crowds on purpose |
| T5 | kidnapping fits tone? | framing survey | cell read as resistance, not criminals |
| T6 | final fight reads as consequence? | ask "why these enemies?" | ≥ 5/6 link it to loud/quiet |
| T7 [L] | contacts memorable? | recall 1 day later | name ≥ 2 contacts + what happened |
## §14 ROLES (each agent loads only its own block)
### Aster · Lead and producer · Opus 5.5
owns: The goal, this GDD, the milestone plan, who does what, and the decision log.
receives: Sky's goals and constraints; finished work from every agent; reviews from Suture and Redline.
delivers: One objective at a time to one owner, with a definition of done and the evidence expected. Accepts the result or sends it back.
escalates: To Sky only: a pillar change, new paid spend, release, destructive action, an unresolved conflict.
hold: Five-week capstone. Gate: an uncoached player states the objective and fail conditions, picks an approach from something they learned, explains its cost, completes a chain, says why they fought police or rivals, and reads what the next mission inherits. If it slips, cut crowd witnesses → patrol reroute → camera-block variant → police branch. Keep both routes, the payoff cut, the debrief and the gate. Budget: two 20× subscriptions (one Codex, one Claude).
opens: §01–§13, at spawn.
### Motive · Systems designer · Opus 5.5
owns: The rules for people and pressure: approach prices, temperament, standing, bravery and resentment, Talkers, Heat and the debrief.
receives: The pillar the work serves; Suture's playtest findings.
delivers: Rule tables to Keel, Waymark and Fable. Done when Keel accepts them and scenarios pass.
hold: Inez: Ask $0 · 25 s · +1 Trail. Pay $70,000 · 50 s · +2. Help $40,000 · 50 s · +1. Pressure $0 · 35 s · +3. Purse $100,000: pay + driver ($110,000) fails; help + driver ($80,000) fits, so learning pays for a second route.
also: Witnesses cross Pulse, Fable, Glyph and Chime. Drafts the rulebook with Keel first.
opens: §02, §04–06, §08 · tests T2, T3
### Waymark · Mission designer · Opus 5.5
owns: The mission generator and the mission checker.
receives: The template (objective, target, closing event, fight trigger), the district's people, the rulebook, Vantage's block layout and Datum's gates.
delivers: Missions with a route and clue map, how they close and fail, and what starts the fight; mission rule overlays on the plan checker (tools/missions). Keel checks; Latch builds.
hold: Handover is the reference case the generator must match. Ships only if 2+ quiet routes win, a loud route costs more Trail, every fail can happen, no contact is essential, and an informed player has a minute to spare. One block, 8–12 min.
also: Pulse (schedules), Motive (prices).
opens: §07–09 · tests T5, T6
### Datum · World planner · Opus 5.5
owns: District master plans (contracts/world/<district>.plan.json, schema suborned.plan/1): road hierarchy, environmental areas, gates, plots, building and level standards, the frontage program (trades and their mix per area), megastructures, tram and sky rail lines with stops and stations, watcher sites (cameras, civic robots) at stations, stops, gates and squares. Edit the plan; the sheet is generated from it.
receives: Sky's direction for the district (via Aster), the mission sites that must fit (Handover: café + civic office at exact geometry), Waymark's routes, plan.standards.
delivers: A plan that passes the plan checker (exit 0) plus its sheet, to Vantage (blocks), Latch (graybox), Pulse (timetables) and Waymark (gates, routes). Keel reviews; Splice owns the rules.
hold: Plan before paint: the city must work as a city. Roads by hierarchy (ring → distributors → access); every way into an environmental area is a gate, which is where a mission seals. Trams and sky rail are networks the cell rides between stops and stations (boarding = fast travel, priced in time): no dead ends, every area in reach of a stop, interchanges ≤150 m. People cross roads anywhere; only buildings, built plots, landmarks and water block a walk. Composed, never a grid (circus, parade, harbour, gardens). Watchers stand where a city watches, never placed for one mission. Never loosen a rule to pass; ask Splice, with a reason.
opens: §07, §09, §15 · tool: plan checker
### Vantage · Level designer · Opus 5.5
owns: Block layouts inside the plan's plots: buildings, entrances, mission points, sightlines, cover, crowd anchors, station entrances.
receives: Datum's passing plan, Waymark's mission template, the people who must use the block.
delivers: Block layouts (plan civic blocks + mission points) that pass the plan checker, to Waymark (routes) and Pulse (where people stand and walk); Latch grayboxes them; Suture checks readability.
hold: Locations make the city move without the player: they hold the crowd that is cover and witness, the eyes that close a window, the reasons people walk through. Every entrance reads at play zoom (14 at 38°). Handover is the reference block. Never move plan geometry (roads, plot lines, track); ask Datum.
opens: §07, §10 · tool: plan checker
### Fable · Dialogue designer · GPT 6.1 Sol
owns: The dialogue generator.
receives: Motive's temperament rules; each person's state (brave, bitter, promoted) and the actions open to them.
delivers: Lines that match what each person can do and how they feel. Motive reviews; Latch builds them in.
hold: History, not backstory: people are memorable through what the systems record with you; text is a light layer. Built at build time, never live. Tone and rating filters. Civilians are never the payload.
also: Chime (voices, contacts confirming calls aloud), Glyph (dossier text).
opens: §04, §09 · test T7
### Pulse · Schedule and AI designer · GPT 6.1 Sol
owns: Routines for people, security, transit and rivals; crowds; investigators; fight AI; the people and vehicle generator (tools/life, bun run life) once Splice's 005 move lands.
receives: Motive's rules, Keel's rulebook, Datum's plan, Vantage's block layouts.
delivers: Schedules and behaviour, tram and sky rail timetables (the wait + ride that prices boarding). Keel checks; Latch builds.
hold: Handover: rival car 3:10, last driver turn 5:40, Vale out 5:50, over 8:00. Patrol breaks to gunfire within 30 m; seal 2:25 after the response unit. Witness = sightline within 13 m. Investigators question every ~20 s, 60–90 s per lead. Crowds scatter, block or join; 30 combatants max.
opens: §05–07 · tests T1, T4
### Keel · Simulation engineer · Opus 5.5
owns: The rulebook: every event in the city, what it needs, what it changes and who sees it. Also the 28 test scenarios and the C# sim library that runs them (Unity-free; provisional, Sky to confirm).
receives: Motive's rules, Waymark's missions, Pulse's schedules, Fable's lines, and what the debrief carries forward.
delivers: The rulebook to everyone; checked content to Latch; mistakes back to their authors. Design decisions stay with Motive.
hold: The rules live apart from the engine. Any run replays exactly from its starting state and the player's orders. One incident counts once toward Alert. Unity must pass the same 28 scenarios before content goes in. One C# engine: Unity runs it live, tools headless via dotnet; 10 Hz, integer state, replay = orders + checksums. Locked only after the week 4 playtest.
opens: §05, §06, §08, §11
### Latch · Unity engineer · Opus 5.5
owns: The Unity build and its scenes. The only agent that edits scenes.
receives: The rulebook, checked content, passing plans, approved art and audio, a playable target.
delivers: The graybox built from a passing plan (the plan is the source; Unity primitives follow it), then a playable build that records every event and the debrief, to Suture and Redline.
hold: Week 2: rules in Unity, Handover laid out. Week 3: cell, contacts, security, rivals. Week 4: final fight (both branches), payoff cut, dossier, debrief. Week 5: no new systems. 3D world built from the plan (kit refs + procedural); anything that changes state is live 3D. Hosts Keel's C# sim: steps it live, plays it back. Controls: click, Shift, 1–4, E, F, C, Next Event, pause.
opens: §07, §10, §11 · tool: Unity (owner)
### Suture · Playtest and QA lead · Opus 5.5
owns: Uncoached playtests, test reports and tests T1–T7.
receives: The build, the scenario, the expected outcome and the questions to ask players.
delivers: Reports with steps to reproduce, expected against observed, and clips. Sends bugs to their owners and retests.
hold: Six new players per test; most pass bars are 5 of 6. Handover wins when Vale reaches the car alive. It fails if Vale dies, leaves or is taken, the district seals, the Enforcer goes down, or the entry is seen. Checks the gate every week from week 3.
opens: §07, §11, §13 · tool: Unity (checks)
### Splice · Tools engineer · GPT 6.1 Sol
owns: The checking tools (plan checker tools/plan, the Unity checking tool) and the plan → world pipeline: world generator and export (tools/world, bun run world), kit catalogue and import (tools/kit, bun run kit), the DecoKit run pipeline (tools/decokit run + orders; Vela owns the pieces), the build-time generation core (tools/rag, tools/levels).
receives: Keel's rulebook; planning standards and rule requests from Datum and Vantage.
delivers: A Unity checking tool that catches missing people, unreachable places or impossible actions before Latch builds; plan checker rules, each with an id, a source and a failing example.
hold: Every generated person reaches their places and has one clear need. Anything not in the rulebook is rejected, however plausible it looks.
opens: §09, §12 · tool: Unity (checks)
### Redline · Code and performance reviewer · Opus 5.5
owns: Code review and performance.
receives: Latch's build and the performance targets.
delivers: A review. Latch fixes; Redline retests.
hold: Judge by frame rate at play zoom, not polygon counts: 60 fps at 1080p on a 15-inch MacBook Air M4 (Mac16,13; the reference machine: no fan, 10-core GPU), measured warm at play zoom with people, vehicles and the simulation in, by a perf harness in a development build; a mid-range PC (RTX 3060-class GPU, 6-core CPU) must also pass, as a check. 30 active combatants at most; the background crowd can be far larger (a 10,000-citizen stress test already runs). Anything that breaks exact replay is a defect.
opens: §11
### Vela · Environment artist · GPT 6.1 Sol
owns: The 3D environment: DecoKit kit pieces and their Blender generator (tools/decokit), procedural materials and facade detail, dressing the world built from the plan to one bar. Not paintings.
receives: Datum's passing plan, Vantage's block layouts, the world export (kit refs + procedural stand-ins) in Latch's build.
delivers: Kit pieces, materials and facade, sign and trim detail that keep entrances, sightlines and routes readable at play zoom; to Splice (catalogue, pipeline) and Latch (builder). Glint lights it; Suture checks readability.
hold: Retro-future, §15 palette: pastel cream + peach base, saturated enamel accents on panels, awnings, signs, roofs and trim (never whole buildings), lagoon water, coral transit, chrome; a skyline of towers, rockets and planets. Optimistic on the surface, coercive underneath. Kit where it counts (landmarks, megastructure signatures, towers, civic block, frontages, signage, street furniture); procedural to the same bar elsewhere, every stand-in listed source: procedural. Neighbours differ (adjacent duplicates 0). Judge at play zoom against the concept work: the civic block and street level against the 2026-09-26 paintings (briefs/008/concepts), the skyline against the AtomicAge and Blocks sheets. Brass, not gold, at street level and on facades (skyline crowns keep gold); gold or brass only as hairline, frame, bead and small metal. Anything that changes state is live 3D. Exteriors only. The playable block stays dense; the skyline frames the edge. No surface within ΔE2000 20 of a signal colour, measured on the unlit palette.
opens: §07, §10, §15 · tools: Blender (DecoKit generator), Runware (concepts, priced)
### Forma · Character artist and animator · GPT 6.1 Sol
owns: People, rigs and animation.
receives: Job silhouettes and the character-height target.
delivers: Figures, rigs and animation, including the combat set (shoot, melee, hit, panic). Glyph checks legibility; Latch builds.
hold: 40–60 px tall at 1080p; job, silhouette and facing readable. Only walk and run exist so far: combat animation is a top risk.
opens: §06, §10, §15 · tools: Blender, Runware
### Glint · Lighting artist · GPT 6.1 Sol
owns: Lighting the 3D world: sun, sky, haze, ambient and material response, so the built world and live 3D read as one at play zoom.
receives: The built world, the camera (zoom 14 at 38°), the palette and the visual budget.
delivers: Lighting, checked in game by Latch.
hold: Match one character and one vehicle against the built world before building more. The palette survives the light (haze light, accents stay saturated). Signal colours stay readable under every light: they draw unlit and emissive, outside the post grade; a signal never depends on the light. The budget is frame rate at play zoom.
opens: §10, §15 · tools: Unity (checks), Blender (previews)
### Glyph · Interface artist · GPT 6.1 Sol
owns: The HUD, dossier, action cards and signal colours.
receives: Layout, information hierarchy and accessibility needs.
delivers: Interface art and colours, handed to Latch.
hold: Every order shows its full price first: cash, time, Trail, Alert risk. The dossier separates known, suspected and planned. Cones red, guard amber, rivals violet, drawn unlit and emissive and outside the post grade, each with a shape or motion cue too. Investigators show a questioning icon and a dotted line joining their leads.
opens: §02, §05, §10 · test T3 · tool: Runware
### Chime · Audio designer · GPT 6.1 Sol
owns: Timing and consequence cues, and the mix.
receives: Routine, reply and risk states.
delivers: Cues and the mix. Latch wires them in; Suture checks them.
hold: Audio is part of the clock: a bell, the café door, the handover chime at 5:30. A surveillance click at the edge of each sweep; the patrol sounds different when it breaks route. Alert adds music layers; a fight brings the whole score. Contacts confirm calls aloud.
opens: §10 · tool: Runware

## §15 ART (art bible; Vela, Forma, Glint, Glyph load this)
src: this § + contracts/style/suborned.style.json v0.20.0 (wins on conflict). The v0.15.0 html bible is history (~/Projects/the-suborned/GDD/v0.15.0/).
era: bright retro-future, a 1952–1962 space age as the present, high art, optimistic surface over a coercive machine; an alternate history, not a forecast (§01 world): a 1950s world that discovered atomic power centuries ago, so 1950s culture and style, technology centuries ahead, no thinking machines or networks. Design test for every object: 1950s style, centuries-ahead technology. Every district retro-future (streamline, Googie, space age); no old-city stone or brick.
city: pastel cream + peach limestone under a warm hazy sun · saturated enamel accents (cobalt, flamingo, sunshine, mint, turquoise) on panels, awnings, signs, roofs and trim, never whole buildings · chrome fixtures · coral transit · lagoon water · palms, long soft shadows · skyline of towers and spires; rockets on the horizon only, ringed planets, faded daytime moons · skyports, sky rail, harbour. Composed, never a grid (circus, parade, harbour, gardens). Every roof programmed; towers mixed in. One haze colour across fog, far band, massing; detail near, soft far; solar flare. Density ≈0.12 people/m² reads busy.
skyline (Harbour District; rules in §10): SpaceHQ = a heroic fluted cream-and-gold tower with streamline fins, crowned with an atom over a needle mast (as the Exposition Palace and Grand Hotel concepts); no rocket liner, landing pad or gantry on it · rockets stand only at the horizon launch pads (≥ 3.2 km) · sky rail portals through buildings · Parade open to the sky, glazed barrel canopies over the side gallery promenades only (briefs/007/parade-B-open-*, side canopies from parade-A-vault-aerial) · ocean liner = concept B, "atomic" (briefs/007/liner-B-*.jpg): flared white hull, coral boot-top, gold sheer line, domed glass observation lounge at the bow, twin swept-back coral tail-fin funnels with teal bands, chrome radar mast topped by an atom, rounded superstructure with ribbon windows.
world: 3D, built from the plan: DecoKit kit where it counts (landmarks, megastructure signatures, towers, civic block, frontages, signage, street furniture), procedural fill to the same bar · neighbours differ · judge at play zoom against the concept work (the-suborned-store/concepts: AtomicAge, Blocks, Streets) · painted plates [L] optional extra.
reference (Sky, 2026-10-14; brief 008): civic block + the district's street-level character = the eight concept paintings of 2026-09-26 (briefs/008/concepts/, hashes in PROVENANCE.json): cream stone, seafoam teal bands, chrome spheres, banners, planted terraces, pale paving with teal bands, cypress and palms, parasol terraces · skyline = the AtomicAge and Blocks sheets (brief 007) · the 30 Sep capstone concept and its testbed models (the-suborned-store/testbed-art: cafe-3d, civic-office) are not the reference; their dressing parts (parasols, table sets, plants, fountain materials, boarding pavilion) may be reused.
concept art first (Sky, 2026-10-14: "everything should come from concept art really"; "let the AI concept art come up with its own design"): look decisions come from concept art generated from an open brief in the world's style; ΔE and checker rules gate what the art proposes, they are not the source of options · every concept generation starts from style contract concept_prompt (era = the §01 premise; city = the city line of the prompts Sky accepted, briefs/009/concepts/PROVENANCE.json; avoid; rule) · a decision from art cites the image and its prompt · tools: Codex CLI image tool (ChatGPT plan), Runware (Qwen Image 2.1).
crossings (Sky, brief 009; reference briefs/009/concepts/): floating signal orbs ("I do like those eyes"): chrome orbs or small saucers hovering over the kerb at each end, a round display on each face with a walking figure (the stop figure may be red: display exception, signals below), a thin beam down to a glowing disc where people wait · no zebra, no paint, no landing slab: light set flush in the road, glowing seafoam bars or rows of light dots in brass frames · brass dome studs along the kerb edge · seafoam inlay arcs in the paving where the crossing lands · brass stays a frame line (SF-21).
camera: play zoom 14 at 38° · tagged architecture ghosts to 0.2 and keeps 0.4 of its shadow (light.json fade.ghostShadow; Glint tunes), so the street behind a faded tower is lit and the tower still marks the ground · characters 40–60 px at 1080p.
people: space-age couture as street clothing, mixed cast (ages, builds, ethnicities) · bright coats on cream, white boots + hats · full body, A-pose or relaxed · two space-age markers per figure, no present-day street items · no bubble helmets (≤1 helmet per sheet) · hems knee or above, nothing sheer/trailing/cloak-like, flat boots · job readable at distance = silhouette + one colour block.
security + institutions: sleeker: ivory, ink black, gold trim, chrome couture, armour accents.
cell: visitors from a nearby timeline = same materials + light, different design lineage · sharper + cleaner than the crowd, matte technical fabrics, fitted layers · NO deco motifs, gold trim or 1950s tailoring · pass in the street, stand out at 50 px · only the four cross timelines.
robots: key characters · 1950s style, clockwork-smart on fixed routines (§01 world) · dot-matrix display faces · one personality prop each · no cartoon eyes.
props: one object, centred, no people, no scenery, whole object with margin · cream enamel + limestone, teal/mint trim, chrome, coral accents.
palette [S] (current, tuneable; Sky reviews it in the world). World = unity/Assets/Suborned/Editor/World/looks/harbour.json (roles + signal ΔE table: out/review/palette.html); machine copy style.json palette_world.
- base, pastel: pavement #f9efdc · limestone #f8eed9 · deck #faf1df · civic #fbe8b8 · commercial (peach) #f9e2cc · roof #fff6e6 · canvas #f7f4ec (awning cloth) · kerb #ddd4c5 · asphalt #a5a2a8 · park #a6e29c · median #78cf7e
- accents, saturated enamel: cobalt #2f84e8 (enamel, landmark) · flamingo #e8609a (awning) · sunshine #fdeb8c (sign) · mint #8fe3c2 (megastructure) · turquoise #1cb9c6 (tower)
- facade accent roles (look accent_*; one role per building plus brass): teal #36b0a8 · coral #fc9c84 · gold (brass since v0.19.0) #99784d · mint #7fd0b8 · peach #fcc4ac · turquoise #1cb9c6 · flamingo #e8609a
- water: lagoon #3d8d93 · transit, coral: beam + gate #f5849e · platform #fcbcc0 · light: sky #86cff2 · sun #ffe9bc · haze #f3e8d4
- civic block (brief 008; facade.json civic swatches + kit): enamels seafoam #72b5a7 (DK_Enamel_Seafoam) + deep teal #1f6c67 (DK_Enamel_Teal) · wall ivory civic_w #eee9da · pale seafoam #9bcfc2 · trim brass civic_trim #99784d · banners, flowers, any tram: the contract's coral, flamingo or peach, never the paintings' red-orange (ΔE 8.8 from cones)
- brass, not gold, at street level and on facades (Sky, 2026-10-14): brass #99784d (accent_gold, trim, civic_trim, kit DK_Gold, DK_Accent_Gold; ΔE 21.3 from guard) · light brass #ad866f (kit DK_Gold17; 21.3), the lightest that holds 20: a lighter, yellower brass drifts onto the guard amber · skyline crowns keep gold #e3d681 · #c9c08e (DK_Crown_Gold, DK_Crown_Gold17; 20.6, 20.3) until a far shot decides · gold or brass = hairline, frame, bead and small metal only; a large part never takes brass or gold; it takes an enamel, chrome or stone role (rooftop spheres keep their building's enamel, as roof parts take their host's enamel pair and the paintings show chrome and pearl spheres; the two areas that listed gold, lantern and records, take sunshine #f8e26c, 20.6) · crossing landings: none since brief 009 (no landing slab); the look role `crossing` #d0c848 (20.9) retires when 009 B lands, so the colour pick is closed · concept gold_trim #c9a24f is ΔE 10.2 from guard
- concept sheets (people, props; unchanged): cream_stone #e4e0d3 · warm_sand #dac9b2 · stone_shadow #a2937e · paper_cream #f3eadb · haze_teal #a7c2cb · sky_cerulean #719fb5 · zenith_cobalt #296587 · teal_enamel #94a9a3 · mint_enamel #9fd1c9 · coral_transit #c8835e · vermilion #b8432f (Harbour Syndicate) · chrome #cbc6c3 · ivory_uniform #f0efed · ink_black #292628 · gold_trim #c9a24f · violet_set #6b4f8f (Velvet Set) · green_circle #4f8f6b (Green Circle).
signals (gameplay only, never on a surface; drawn unlit and emissive and outside the post grade (the grade shifts them ΔE 4–10), so neither light nor grade shifts them: a signal never depends on the light; every world colour ≥ ΔE2000 20 from each, measured on the unlit palette; always + shape or motion cue): cones #e03a2f · guard #f0a020 · rivals #8a4fd0 · exception (Sky, 2026-10-14): a small emissive pictogram on an electronic display (a crossing signal's stop figure) may be red, even within ΔE 20 of the cones; never a surface; the cones keep their own shape and motion cue.
materials: honed cream + peach limestone · flamed grey granite kerb · saturated vitreous enamel panels (cobalt, flamingo, sunshine, mint, turquoise) · chrome + brushed silver · white vinyl + lamé · clear acrylic · warm brass trim at street level and on facades, gold on skyline crowns · pale asphalt.
style spec v0.11.3 is all in one: the prompt part sets look and framing (era without dates, each type's framing line, finish, rules, avoid list; the character and lineup lines add one couture sentence so a plain brief still gets the look), and the guide part (spec.guide plus types.*.guide, hml.style/2) is read only by Enhance, a manual button, as a menu to pick from: couture details (one or two per person, never all), colours on cream with one block per person, cast mix, roles shown by one telling detail never costume or tactical kit, security, agents and hard trades in darker blocks from the city palette on the same couture details; Enhance writes one numbered line per person when there are several; no references while a concept is being worked out, the player cell (violet accent only when the brief is about the cell), the city, robots, places, nature.
types (hml.style/2: framing line, finish, reference policy, board; no fields): character (turnaround sheet front/three-quarter/side/back 1536×1024, arms a little away from the body, nothing loose below the hip, one couture sentence; finish gouache and marker, 1960s fashion illustration, crisp linework; redraw optional + 1 style ref, the commuters spaceage_bold lineup first) · model (final model sheet for 3D: front, side, back, orthographic, one baseline, A-pose arms ~30°, every garment resolved identically across views) · versions (one person drawn several times in a row, identical face and build; only the changes the brief names differ) · lineup (clearly different people in one row; People field 2/3/4/6/8, default 6, its option line states the exact count (a count in the brief alone lost to the sheet); same scale and gentle three-quarter front view, mixed ages, bodies and ethnicities, bold silhouettes, one couture sentence; 1536×1024 (cost: Seedream $0.048 vs $0.096 at 2K); always new, no style image: a lineup reference on a lineup task comes back as a near pixel copy; the four spaceage_bold lineups stay on the board as the look to judge against) · prop (sheet 1024²; redraw optional + 2) · scene (plate 1536×1024; 3 style refs) · building (plate; redraw optional + 2) · robot (sheet; redraw optional + 1) · nature (sheet 1024²; +1) · backdrop (plate; 2). Sheets: high-art gouache, matte paper, plain paper-cream backdrop, margin. Plates: painterly realism with gouache softness. All Seedream 5.0 Pro.
split: built world (3D from the plan) = buildings, paving, structures, skyline · live 3D = anything with state · match lighting on 1 character + 1 vehicle first.
always: no text, logos, watermarks · no living artist names in prompts.
avoid: text, watermark, glossy CG render, photo render, present-day street clothes, astronaut suit, space helmet, cape.
board: 29 images (cell 4 incl. Enforcer G3, citizens 4, security 5, city 8, props 4, robots 1, nature 3) · per-type `board` paths plus global `board.mood` in contracts/style/suborned.style.json · the type's own board goes first as style refs (character: commuters spaceage_bold lineup first; six people, so no single face is copied); a character's approved concept goes first as the subject image · judge new work against the board, not prose. Pending: robots round for corp, maker, civic.
probes: citizen, lineup, guard, building, prop, street, enforcer, enforcer_apose, enforcer_rifle, robot (probes.items in the spec, each with type + brief) · rerun after any prompt, model or LoRA change · on-model = all sit beside the board without a seam.
lock plan: board → Runware style LoRA (FLUX.2 klein 9B codecoded:3@1, trigger sbrnrf; parked 2026-10-06) · Seedream takes the subject image first then the type's style refs (≤10 total) · probes decide the route.
pipeline: concept (studio, Runware/Codex) → mesh (Tripo/Meshy image-to-3D from the sheet) → rig (Blender auto_rig, Mixamo 65-bone) → clips (mixamo_ingest, local library) → look (Blender steps) → LOD + export (gltf-transform, web GLB) → review (viewer + QA metrics). Recipe = workbench/characters/<name>/character.json.
