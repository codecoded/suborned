# Brief 096 · Motive: Help for a Fearful person (Sky's option 1), 2026-11-05

For Keel (engine 0.44.0), Waymark (plans), Latch and Glyph (the card), Fable (words). Values: `help-fearful.json`.

## Where it starts

- **Built today (engine 0.43.0):** a Fearful person in a Hero (or neutral) district "hesitates; needs Help ×2": Help is a worse approach
  for her (§04 wrong approach, Sky Q4), so it takes one more order (`worse-help-orders` 1) and the money is split over the two visits
  (`worse-help-usd` −20,000): $20,000 a visit, $40,000 in all, 35 s and +1 T each. Help is locked ("Listen first", `needs-need`) until
  the Scout's Listen; the briefing's "Debt" (Intel's told word, `Person.Suspected`) unlocks nothing.
- **Sky (2026-11-05):** "the first attempt fails, it's not enough to turn her. The second attempt is about offering substantially more" ·
  "Inez wouldn't take the first payment. And makes no sense to make a second payment if she did, as player is just standing there
  immediately doing the same action again". Of Aster's options: **"I think 1 is fine for now"**: knowledge unlocks the full offer.

## The rule

**H1 · The small offer (everyone whose need is told, not heard).** Help on a person whose need the cell has only been told (the
briefing's word: "Debt" for Inez, "Cash" for Lukac) and has not heard (no Listen; nobody told it whole) is offered, as a small offer:
the Fixer offers money without knowing what the need is or how big. **Everyone refuses it.**
- Price: **$0, 35 s, +1 T**, and nothing else: it is priced by its own line alone (no refused-before surcharge, no visits, no mood step).
  No money changes hands, so the card asks for no cash.
- It is private as every Help is: no witnesses, no Standing, no incident. A Loyal person's report reads the fit as today (in a Hero
  or neutral district Help suits a Loyal person, so no report; in a Menace district the report comes as it does for any approach).
- **What the refusal leaves**, exactly as any refusal (Ask cold, Pay to an Idealist):
  1. a memory entry `refusal` by the Fixer: she holds **the Fixer's face** and **the place and time** (talker facts). A Fearful person
     starts a Talker (fear 1 > 0), so questioned before she is helped, she gives up both; once helped (loyalty 1) she no longer talks.
  2. **a remembered refusal: every later offer +$10,000 and +1 T** (refused-before on Help and Pay; +1 T on Pressure). So the full Help
     after a refused small offer is $50,000 and +2 T.
  3. **Ask cold is closed** to her (Ask needs no remembered refusal).
  4. the debrief line `small-offer` (who, approach).
- It does **not** mark Help as refused (the card's ✗): the cell learns nothing new from it, and the full offer stays open after the Listen.
- With nothing told (no need word in Intel), there is nothing to offer for: Help stays locked "Listen first", as today.

Why everyone and not only the Fearful: before the Listen the card can't know her temperament (§04 preview: "?" until the Listen). If only
a Fearful person were offered the small Help, or only she refused it, the lock or the mark would tell the player her temperament before the
Listen. One rule reads simply: *Help needs the Listen; before it, it's a small offer, and nobody takes one.*

**H2 · The full offer (Fearful, after the Listen).** Once the need is heard, Help on a Fearful person is the full offer: **$40,000,
35 s, +1 T, one visit, and she turns**, in every band. A Fearful person no longer needs Help twice (`one-more-help` and both
visits-share lines leave the Fearful out). With a remembered refusal: $50,000, +2 T.

**H3 · The card.**
- Before the Listen (need told): **$0 · 35 s · +1 T · ✗ "refuses a small offer: Listen first"**. The engine's `Quote.Outcome` is
  `small-offer` and the price line `small-offer` holds; it is definite, never "?", since every temperament refuses it. An informed player
  sees the refusal before the click.
- After the Listen (Fearful): **$40,000 · 35 s · +1 T · ✓ turns** (`Quote.Outcome` `turned`, `Orders` 1). Others: as today (Mercenary
  "not yet" at $20,000 and the second visit; Loyal two Helps). With a remembered refusal the refused-before line holds (+$10,000, +1 T).

**H4 · The other Fearful people.** The Handover's turnable people are its four cast roles (Inez, Arthur, Lukac, Popovic); Turn takes no
one else (`IsPersonRoleName`). **Inez is the only Fearful one**: Arthur, Lukac and Popovic are Mercenary (sim/data/handover.site.json
cast). sim/data/handover.people.json does hold 19 Fearful people, but the other 18 are the crowd (two café staff, clerks, residents,
kiosk attendants), and no order turns them on this mission. So the brief's "two fearful" is one in play. The rule is general: any Fearful
person, any mission, any band: the small offer refused before the Listen, one full Help after it. Keel's scenario makes the driver Fearful
by the start state (his "Cash" is told by the briefing too) to show it is the temperament's rule, not Inez's.

**H5 · Menace.** "Fearful, Menace: folds free" is unchanged: Ask suits, $0, 20 s, +1 T, and she turns. Help on a Fearful person in a
Menace district follows H1 and H2 (Help is worse for her in every band, as today), but nobody pays $40,000 for what Ask gives free.

**H6 · Everyone else.** After the Listen nothing changes for anyone but the Fearful: an Idealist takes one Help (suits), a Mercenary two
visits at $20,000 (Lukac on route B), a Loyal person two Helps (trust over time), the Menace refusals and reports as today, Pay and
Pressure as today. Before the Listen the one change for everyone: where the need is told (on the Handover, Inez and Lukac) Help is a
small offer that is refused, where it was a lock. No bot or reference plan Helps before the Listen (it was refused), so no plan changes
for H1; H2 shortens route A by one Help.

**H7 · The purse ($100,000).** Help on Inez $40,000 + Help on Lukac $40,000 (two visits) = $80,000: fits (§04 "learning buys the second
route" holds). After a refused small offer on Inez: $50,000 + $40,000 = $90,000: fits. Pay on Lukac $70,000 + Help on Inez $40,000 =
$110,000: still doesn't, as today. A small offer on both: $50,000 + $50,000 = $100,000: fits, exactly.

**H8 · Bands.** Expected: route A's informed plans (quiet-a, prepared-loud) turn Inez one visit sooner (35 s) and leave 1 T less; the
door is opened from the same Loop (planted after the one Help), so A's win rate should hold, and nothing on routes B or G moves (Lukac is
Mercenary, Help on him unchanged). Keel measures 200 seeds a plan before and after; a band that moves comes back to me.

The Q4 note: under Sky's call a Fearful person's worse step is paid only by the uninformed (the refused small offer). An informed player
pays her what an Idealist costs. That is Sky's call ("$40,000 and turns her first time") and stays.

## Questions for Sky (through Aster); nothing below is built

**Q1 · Lukac's two visits.** Help on a Mercenary is still two visits at $20,000 (route B's driver), and Sky's reason ("just standing
there immediately doing the same action again") applies to him too. Options: (a) leave it for now; (b) the same as Inez: one visit after
the Listen, at one step worse in money ($50,000: Inez $40,000 + Lukac $50,000 = $90,000 still fits); (c) his second visit unlocked by
another act (a Watch of his garage bill, a job). **Recommend (b)** in a later round: one rule for every worse Help (the full offer once,
a dearer one), and Q4's "one step worse" holds as money. Built now: (a).

**Q2 · The Loyal's two Helps.** "Turns after repeated Help" is the same repeat. Options: (a) leave it: trust over time is the Loyal's
whole road; (b) the second Help only in a later mission (trust over time, between missions); (c) one Help at a higher price. **Recommend
(a) for now** (no Loyal person is turnable on the Handover) and (b) when the campaign carries contacts between missions.
