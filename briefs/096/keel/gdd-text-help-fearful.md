# Brief 096 · GDD text for Aster: Help for a Fearful person (engine 0.44.0)

Keel, 2026-11-05. Source: Sky 2026-11-05 on paying Inez, option 1 ("I think 1 is fine for now"); Motive's rule
(briefs/096/motive/help-fearful.md, help-fearful.json). Built on branch `t3/fearful-help-refusal-before-listen` as engine 0.44.0. Keel
doesn't edit the GDD: this is the text for the next version, section by section, in the agent GDD's register. Every value [S].

## Decision-log line

2026-11-05 · brief 096 · Sky on paying Inez: option 1, "I think 1 is fine for now". Engine 0.44.0 (Keel; Motive help-fearful.md): Help
before the Scout's Listen is a small offer everyone refuses ($0, 35 s, +1 T, a remembered refusal); after the Listen a Fearful person takes
the full offer once ($40,000). Was: a Fearful person in a Hero district needed Help twice, $20,000 a visit. Open for Sky: Lukac's two
visits (Mercenary), the Loyal's two Helps, route A's new wait before the rival car.

## §04 people

- **approaches table, the Help row:** `| Help (clear debt) | need told (the briefing's word) for a small offer, refused; need heard (Listen 30 s) for the full offer | 40,000 (a person it takes twice: 20,000 a visit; the small offer $0) | 35 s | +1 | gratitude; bravery can grow |`
- **new line after the table [S]:** small offer: Help on someone whose need is only told (Intel's "Debt") and not heard is a small offer:
  the Fixer offers without knowing what the need is, and everyone refuses it. $0, 35 s, +1 T and nothing else. It leaves a refusal like
  any other: they hold the Fixer's face and the place and time, every later offer costs +$10,000 and +1 T, Ask cold is closed. Help is not
  marked refused: after the Listen the full offer stands. The card shows the refusal before the click ("refuses a small offer: Listen
  first"), the same for every temperament, so it gives none away. With no need told, Help stays locked ("Listen first").
- **temperament table, the Fearful row:** `| Fearful | Pressure | hesitates: refuses a small offer; turns on the full offer after the Listen | folds free |`
- **wrong approach [S]:** "(one more Help, or a higher price; Keel sets the numbers)" → "(one more Help for a Mercenary; for a Fearful
  person the small offer before the Listen, refused; or a higher price; Keel sets the numbers)".
- **purse line:** unchanged ("help + driver = 80,000 fits"). Add: "a small offer refused first: 50,000 + 40,000 = 90,000 still fits."

## §07 handover

- **route A:** "→ Fixer helps Inez ($40,000; a fearful Inez takes two visits at $20,000)" → "→ Fixer helps Inez after the Listen
  ($40,000, once: before it Help is only a small offer, and she refuses it)". And: "→ Fixer calls Inez" → "→ Fixer calls Inez once the
  rival car is seen coming (2:35 ±15), so Popovic comes out to a posted crew".
- **reference results** (no number in the GDD changes): the route-A references now end 4:12.7 at 3 T, Alert 0 (route-a) and 4 T, Alert 1
  (route-a-disable), mission record c054.

## §14 holds

- **Motive's hold:** "Help $40,000, or $20,000 a visit where it takes two · 35 s · +1." → "Help $40,000, or $20,000 a visit where it
  takes two (a Mercenary) · 35 s · +1; before the Listen a small offer, $0 · 35 s · +1, refused."
- **Keel's hold:** no change.

## people.rules.json (Motive; with the next people run)

temperaments.fearful.hero reads "hesitates; needs Help ×2". The new words are "hesitates: refuses a small offer; turns on the full offer
after the Listen". Not changed in this round: the people files pin the sha256 of people.rules.json and a reader refuses a file whose hashes
are not the current inputs, so the words change with the next promoted people run (no rule reads the string; the rulebook carries the
rule).
