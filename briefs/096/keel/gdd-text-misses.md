# Brief 096 · GDD text for Aster: Motive's misses (engine 0.43.0)

Keel, 2026-11-05. Source: Motive's misses page (briefs/096/motive/misses.html, misses.json), Sky 2026-11-05 "all recommended" (Q1 a, Q2 a,
Q3 a); Motive's rivals page (rivals.json pay.needs, Sky 2026-11-04). Built on this worktree's branch as engine 0.43.0. Keel doesn't edit the
GDD: this is the text for the next version, section by section, in the agent GDD's register. Every value [S]. It amends gdd-text-lend.md,
whose F2 line is corrected there too.

## Decision-log line

2026-11-05 · brief 096 · Sky: Motive's misses page, all recommended. Engine 0.43.0 (Keel): a holstered agent who fired is no foe to the
police or robots for it while a gang shooting is unplaced (V3); the police close in 75 s after Popovic is out (was 60); a Go waits for an
agent climbing into the car (G1); Pay on one of the rival crew needs a Watch first (the rivals page); a quiet final fight may be 10 s (Q1);
the veteran's price is Popovic out, the other three caught (Q2); loud on route B stays the hard way (Q3).

## §05 pressure (the gang shooting)

- **Who fired [S].** While a gang shooting is pending and the cell not placed, a police body or robot does not count an agent a foe for
  having fired: nobody of security saw who fired. A shot security sees places the cell (as today), and from then on "fired" counts again,
  as do a drawn weapon seen, seen violence and a known face. Rulebook security.unclaimed.fired: placed.

## §06 fights (the getaway)

- **After vale.out [S].** 75 s later (or at the seal, or at 8:10, whichever is first) the police close in. (Replaces "60 s".) On the
  Handover vale.out comes about 6:49, so the countdown starts at 1:15 and the 8:10 close cuts it only for a late lend.
- **Go waits for a Board [S].** A Go given on a car while an agent is climbing into it (a Board under way, 1 s, or 4 s carrying someone)
  lands the moment that Board lands. Boxed in, held or gated still refuse at once; an empty car, or one with only the downed aboard, waits
  for the agent climbing in. The Go's card counts those climbing in as aboard and names who it leaves behind (caught). If the agent is shot
  down or called off mid-climb, the Go lands then with whoever is in, or is refused if the car is now held or empty.

## §07 rivals (the block, thinning)

- **A broken crew [S]** (replaces the 0.42.0 text's last line). A man who breaks in the final fight, or whose crew's car is gone, runs off
  holstered and never comes back. A crew that breaks before the final fight with its car still able to drive boards it and drives off
  holstered, out of every fight while away, and comes back to block again security.rivals.retreat after the break; at the final fight's
  start the floor may call it back sooner. Destroying the car is what ends them.
- **Pay on one of the crew [S].** Needs a Watch first (any agent, on any member or on their car: the crew's timetable), as Pay on a person
  needs his routine; before it Pay is refused "Watch first". Listen still shows his temperament ("?" before it).

## §09 generation (the Handover's bands)

- Quiet final fight: median 10-40 s, 95 % under 90 s (Sky Q1: preparation earns a short fight; was 15-40).
- Route B on the lend: Popovic out at least 85 % (no ceiling); all four out on 60-80 % of wins, at most 0.6 captured a win; thinned by a
  posted man: out within 3 points of B, all four out at least B + 10 points. Route G: at least 75 %.
- Loud on the lend (prepared-loud-lend): a report, about 30-40 % (Sky Q3: the lend is route B's quiet tool; loud's route is the front door).
- The veteran's shot: Popovic out 40-70 %, the other three caught being its price (Sky Q2 a); captures a win reported.

## §10 presentation (for Latch, Glyph, Fable, Chime)

- The police countdown after vale.out reads 1:15 (from Game.SplitPoliceAt, no number in the host).
- G1 on the Go card: while someone climbs in, the card's left-behind list leaves him off, and the Go's click says the car waits for him
  (log go.waits). If the Go then fails to land, go.missed with the refusal.
- Pay on one of the crew: "Watch first" (needs-routine) until the crew is watched.
- Popovic's car's Go card: "+1 T if seen" names who would see it leave (Game.LentCarWatcher).
