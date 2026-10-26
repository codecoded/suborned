# Brief 069 research: licence of the base bodies and the candidates (Forma, 2026-10-26)

Kept in scratch because the brief's Hold keeps Forma's writes there; Aster may copy it to `briefs/069/research/` with the brief's docs.

## What was installed, where
Nothing went into Sky's Blender config. Everything sits under `out/scratch/forma/char-pipeline/`:
- `dl/mpfb/mpfb-2.0.18.zip`: MPFB 2.0.18 from extensions.blender.org, sha256 `c075b3e9b94b8e5556a5f03f4653602d02ea306de08126e8f55ed6dee28a2dad` (matches the listing's URL hash). Unpacked to `blender-user/extensions/user_default/mpfb/`.
- `dl/sysassets/makehuman_system_assets_cc0.zip` (267 MB): sha256 `b542127a8e25547c7c29c19f2d1d2adb9a664c80396ecd694095dbc8028a0107`. Unpacked to `blender-user/extensions/.user/user_default/mpfb/data/`.
- Blender runs with `BLENDER_USER_RESOURCES` and `BLENDER_USER_EXTENSIONS` pointed at `blender-user/`, plus `--factory-startup` (see `blender.sh`).

## Licence finding: usable, CC0 output
- **MPFB code** is GPL-3.0-or-later (`blender_manifest.toml`; extensions.blender.org listing). It runs only in our build tool and never ships. GPL covers the code, not what it outputs.
- **MPFB's assets** (base mesh, proxies, targets, modifiers, textures, clothes, rigs, poses, expressions and the JSON mesh data) are CC0 1.0. Source: the MPFB repo's `LICENSE.md` ("These assets have been released under CC0 1.0 Universal") and `LICENSE.ASSETS.md` (the CC0 1.0 text). This covers the `mixamo_unity` rig and its weights, which the pipeline uses.
- **Output:** MPFB's `LICENSE.md` says output carries no program logic and the team claims no rights over it (exports, renders, saved files). MakeHuman's 2020 licence FAQ says the same: "Exports only consist of things which were CC0 to start with."
- **System assets pack** (the proxies male1591, female1605 and proxy741 used here, plus skins, hair and clothes): the pack page lists every asset as CC0. Checked on disk: all 147 `.proxy`, `.mhclo` and `.mhmat` files carry the header "This asset was explicitly released as CC0 in september 2020", and `packs/makehuman_system_assets.json` lists `"license": "CC0"` for each entry. The base mesh `base.obj` carries the same header.
- **Not used:** none of the pack's clothes, hair or skins. Every garment is our own recipe (`tools/figures/charpipe/wardrobe.json`), and every colour comes from `palettes.json`. The fedoras in the pack would break the GDD's people rule in any case.
- **Clips:** `unity/Assets/Suborned/People/Clips` are the Mixamo library files the game already uses, unchanged. Nothing new is added.
- **Paid spend: $0.** No Codex stills, Runware calls or 3D generation were needed. The garments are procedural recipes on the base body.

Sources:
- https://extensions.blender.org/add-ons/mpfb/ (version 2.0.18, 2026-10-10, GPL-3.0-or-later, Blender 4.2+)
- https://github.com/makehumancommunity/mpfb2 (`LICENSE.md`, `LICENSE.ASSETS.md`, `LICENSE.CODE.md`)
- https://raw.githubusercontent.com/makehumancommunity/mpfb2/master/LICENSE.md
- https://static.makehumancommunity.org/about/license.html ("All core assets are shared under Creative Commons, CC0")
- https://static.makehumancommunity.org/oldsite/faq/what_changed_regarding_the_license_in_2020.html
- https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html (every asset CC0; the zip URL used)
- https://static.makehumancommunity.org/assets/assetpacks/faq.html (the system pack is meant for MPFB2)

## Candidates compared
| | MPFB 2 (MakeHuman), picked | Quaternius Universal Base Characters | today: image-to-3D per person (Tripo/Meshy + own rig) |
|---|---|---|---|
| licence | assets and output CC0; code GPL (build-time only) | CC0 | per-vendor terms, one-off meshes |
| bodies | one parametric body: sex, age, muscle, weight, height, proportions (and more targets) | 6 fixed bodies (superhero, regular, teen x M/F); no build or height sliders listed | each person its own mesh |
| skeleton | built-in `mixamo_unity` rig with fingers, refitted to every slider setting; Mixamo names, so Humanoid maps unaided | one UE5-style 65-joint skeleton shared with the Universal Animation Library (a second-hand claim, not checked) | own 24-bone rig per figure, no fingers (the Enforcer: Mixamo) |
| low-poly topology | CC0 proxies 1,591 / 1,605 verts with separate fingers, and 741 verts for a LOD, fitted to the body | about 13k tris on average; LODs our own | 3k / 1.5k by decimation |
| scripting | headless `HumanService` API: same macros give the same mesh | a .blend to edit by hand; the source pack costs $19.99 | a paid generation per person |
| cost | $0 | $0 standard, $19.99 source (paid: would need an OK) | paid per generation |

Why MPFB: it is the only candidate whose body varies by parameter (build and height) and comes with a finger rig refitted to that body. Both run headless and give the same result every time, which is what a seeded generator needs. Its low-poly proxies land at today's crowd triangle budget without decimation. Quaternius would give a cleaner stylised look out of the box but fixed bodies, so the diversity would have to come from more hand-made bases. Quaternius sources: https://quaternius.itch.io/universal-base-characters and https://quaternius.itch.io/universal-animation-library.
