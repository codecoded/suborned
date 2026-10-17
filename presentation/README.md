# The Suborned: presentation deck

A self-contained slide deck for the course review: from the 26 September paintings to the shipped Handover.

## Open it
Double-click `index.html`. It works from any folder, offline, in Safari or Chrome: no server, no network, and the fonts are in `assets/fonts/`.

## Keys
| key | does |
|---|---|
| ↓ → Page Down, Space | next slide |
| ↑ ← Page Up, Shift+Space | previous slide |
| Home / End | first / last slide |
| M | section menu |
| N | speaker notes for the current slide |
| F | full screen |
| click an image | open it full screen (Esc or click to close) |

Slide 23 has a before/after slider: drag it. A slide number in the address (`index.html#15`) opens that slide.

## Pack and send
From the repo root:
```sh
bun run deck:pack   # → out/presentation/TheSuborned-presentation.zip
bun run deck:send   # pack, then Taildrop it to skys-macbook-air-1
DECK_DEVICE=other-mac bun run deck:send   # another Tailscale device
```
The zip lands in the Air's Downloads through the Tailscale app. Unzip it and double-click `presentation/index.html`.

## Sources
Every image is copied from the repo (`briefs/`, `out/`) or the concept store, resized. The plan sheet comes from `bun run plan`, the world numbers from `bun run world`, the people figures and the graph from `out/people/harbour-district.people.json`, `bun run people:graph` and its `lovers` and `who-can-introduce` queries, the routes and checks from `runs/missions/handover/c016/run.json`, and the HUD numbers from `briefs/014/glyph-less.md` and `bun run budget`. Fonts: Jost and Barlow, SIL Open Font License (licences in `assets/fonts/`).
