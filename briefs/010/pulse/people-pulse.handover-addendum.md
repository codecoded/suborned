# Addendum for tools/levels/objectives/people-pulse.md (draft by Pulse, 2026-10-15; not applied: Splice is mid-edit on the schema and the plug-in hash)

Add after "What the Handover needs of the cast", once the vocabulary below is in contracts/people/cast.schema.ts:

- The Handover is played at 17:24:30-17:32:30; the Office's shift ends at 17:30 (the chime). Cast its ground so that, at that minute:
  - **one** guard stands the public door (`posts: [{post: 'door', count: 1}]`), one the service door (`post: 'official'`), the rest are on rounds inside (`post: 'inside'`, `side: 'official'`, so the public door holds only whoever stands it). Give the guard position an exact `count` (4) rather than a weight.
  - the guards keep a watch outside office hours (`watch: {count: 2}`): the 17:30 watch calls at the café before the shift (`preShift: 'cafe'`) and takes the door as the day's door guard leaves for the café (`evening: 'square'`).
  - the councillor and his driver are one each (`count: 1`) and use the official car: routine `commute: 'car'`, `leaveBeforeMin: [0, 0]`; the councillor's inside post is `side: 'official'`, the driver's `post: 'bay'`. They arrive and leave together at the bay.
  - the driver holds a workmate tie to the councillor (`{kind: 'workmate', share: 1, positions: ['official-driver'], with: 'councillor'}`).
  - the guards hold two regular ties to servers (`{kind: 'regular', share: 1, positions: ['guard'], with: 'server', count: 2}`): the first is aimed at the stop before the shift (the morning's server), the second at the stop after it (the afternoon's, who is on at 17:30).
- Keep `leaveBeforeMin` small ([0, 6] at most): the minutes are spent walking, so a 40 minute slack is a 40 minute walk over 200 m.
