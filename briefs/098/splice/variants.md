# 098 fast loop: shader variants (Splice, 2026-11-07)

Sky asked if the iteration build could be faster ("yes" to a fast-loop pass). A change to the shared DecoKit include (`DK_Wind097.hlsl` and its siblings) recompiles every variant of the three kit shaders. On Metal one lit forward fragment variant costs about 6 s of CPU, so the count is the build time.

## What I found
- **`ShaderStrip097` stripped nothing.** It reported 0 of 21,988 variants stripped in every build since brief 097: URP had already removed every keyword on its list. Those rules stay as guards.
- **Two states URP keeps that the game never draws.** In a census of what reaches the stripper (`strip097-before.json`), Lit Cutaway's 9,792 forward fragment variants come from 17 material combinations times eight keyword groups:
  - main or cascaded shadows ×2
  - soft shadows on or off ×2
  - cookie ×2
  - lightmap ×2
  - LOD cross-fade ×2
  - cut ×2
  - fog off, linear or exp ×3
  - instancing off, `INSTANCING_ON` or `DOTS_INSTANCING_ON` ×3

## Stripped in every build now (`ShaderStrip097.cs`), with the reasons

| Keyword state | Reason |
|---|---|
| `LOD_FADE_CROSSFADE` (on) | No LODGroup fades. PC_RPAsset `m_EnableLODCrossFade 1` keeps the variants, but all 2,204 LODGroups in OldTown.unity (1,914 in the lean copy) have fade mode None, PlantingSwap077 makes them with `LODFadeMode.None`, and no player code sets `fadeMode`. This is checked per build: a built scene with a fading LODGroup keeps the variants, or fails the build loudly if it was seen too late. `-keepCrossFade` turns the strip off. |
| `_LIGHT_COOKIES` (off) | The sun always carries the cloud cookie. Look097 puts it on the scene's directional light in every scene that has one, and at night the moon is the same light, so the keyword is on for every lit draw. URP's cookie path is a no-op for a light without a cookie: `LightCookieManager.SetupMainLight` binds white with format None, and `SampleMainLightCookie` returns 1. If `look097.json` turns clouds off, the strip is kept off; `-keepCookieOff` also turns it off. With `-noclouds`, Unity draws the cookie variant with no cookie (its nearest variant); shots show this matches. |
| Guards (already stripped by URP) | `_ADDITIONAL_LIGHTS_VERTEX` (Forward+, PC_Renderer mode 2), `_MAIN_LIGHT_SHADOWS_SCREEN`, `_DBUFFER_MRT1/2/3` (the decals are Screen Space), `_SCREEN_SPACE_IRRADIANCE`, `_SCREEN_SPACE_REFLECTION`, `_SHADOWS_SOFT_LOW` (soft shadows Medium), the `META_QUEST_*` keywords. |

**Kept on purpose:**
- **Fog off, linear and exp.** All three are drawn. The play camera has no fog, the third-person views use linear fog and the flybys use exp fog (`haze.json`).
- **`INSTANCING_ON`.** The world builder's surface materials enable instancing (`Look.cs:261`), and runtime property blocks (hover, signal heads, cars) take renderers into instanced draws. A non-instanced fallback would draw them wrong.
- **Soft shadows off.** The tp-long `softShadows` row makes the lights hard to measure their cost.
- **The rest.** Lightmapped and unlit, GRD and plain, main and cascaded shadows, cut and uncut: all are drawn.

## Counts (the census in `strip097-*.json`; lean scratch build, Metal)

| Shader | Pass | Stage | Before | Every build | Traced scratch |
|---|---|---|---:|---:|---:|
| DecoKit/Lit Cutaway | ForwardLit | fragment | 9,792 | 2,448 | 84 |
| DecoKit/Complex Lit Cutaway | ForwardLit | fragment | 1,920 | 480 | 18 |
| OldTown/DecoKit/Lit Cutaway | ForwardLit | fragment | 1,344 | 336 | 21 |
| DecoKit/Lit Cutaway Leaf | ForwardLit | fragment | 768 | 192 | 4 |
| OldTown/DecoKit/Complex Lit Cutaway | ForwardLit | fragment | 768 | 192 | 3 |
| DecoKit/Lit Cutaway | ForwardLit | vertex | 432 | 216 | 26 |
| DecoKit/Complex Lit Cutaway | ForwardLit | vertex | 144 | 72 | 13 |
| DecoKit/Lit Cutaway | DepthNormals | fragment | 108 | 54 | 0 |
| DecoKit/Lit Cutaway | ShadowCaster / DepthOnly | fragment | 60 / 60 | 30 / 30 | 11 / 0 |
| All kit passes | | | 16,196 | 4,473 | 237 |
| Every shader in the build | | | 21,988 | 7,859 | 3,623 |

URP Lit's forward fragment also drops, from 1,728 to 432. The player shrinks from 1,150 MB to 1,054 MB.

## A clean compile of the kit (the DK include changed; `briefs/098/splice/scripts/cold.sh`)
`4.0` becomes `4.00` in `DK_Wind097.hlsl`, a code literal that sits outside every keyword guard. That changes every kit variant's source without changing what it computes. The script then builds a lean scratch player and puts the byte back (trapped).

| | Kit forward fragments compiled | Their compile | Whole build |
|---|---:|---:|---:|
| Before | 12,480 | about 70 min. Today's measured cost is 5.9 s CPU per variant at 17.9 cores; Nov 6's real cold build was 2,638 s for 10,368 lighter variants, 3,338 s in all | about 71 min |
| Every build | 3,120 | 801 + 161 + 96 s, measured | **1,072 s (18 min)** |
| Traced scratch (`4.000`, never compiled before) | 106 | 24 + 7 + 3 s, measured | **49 s** (78 s with the editor's start) |

## The look, before and after
I compared 14 shots (`scripts/shots.json`, taken with main097's `run.sh` rig): play view at four yaws, close zoom, hero views, the third-person follow, the loud fight, and night at the square and the Office. Comparing shrunk 16× (`scripts/areadiff.py`), so edges and motion average out, a wrong variant would show as a changed area.
- Two before runs differ by 0.23–0.88 per cell (mean): the run noise from wind, clouds, birds and petals.
- Warm runs before and after differ by 0.23–0.93: the same noise. The first after-run, on a cold Metal cache, was an outlier; a warm second run matches.
- With `-noclouds`, the warm before and after runs differ by 0.21–1.15, within each pair's own noise (the follow view is noisy in every pair). The side-by-side shows only scattered petals and water.
- Comparing the traced development player with the after build gives 0.37–0.94, against its own noise of 0.22–2.37.
- The draw check (`-drawCheck`, GameBoot.DrawCheck) is `ok` on the before, after and traced players.

## Scratch builds only: `-scratchVariants traced` (`ShaderTrace098.cs`, `ShaderTraceWatch098.cs`)
- **Build:** `EXTRA="-scratchVariants traced" bun run guard zsh briefs/098/splice/scripts/build.sh <name>`, or GameBuild.CliScratch with `-scratchVariants traced [-variantTrace a.graphicsstate,b...] [-strictVariants]`. A game build refuses the flag.
- **What it keeps:** the kit shaders keep only the variants a GraphicsStateCollection trace saw. The trace defaults to `briefs/098/splice/traced/*.graphicsstate`. A build variant stays when a traced variant of its shader and pass holds exactly its keywords, among those its stage declares and this build compiles. A keyword no build variant holds is set aside, because every build draws without it. Every other shader keeps what the always-on strip keeps.
- **Record or refresh the trace:** a `-scratchDev` player of the same tree (`EXTRA=-scratchDev … build.sh dev`), then `zsh briefs/098/splice/scripts/trace.sh out/scratch/splice/dev briefs/098/splice/traced/play.graphicsstate`. This runs HitchTrace096 in both orders: the map at four yaws, third person and the drive, a conversation cutaway. Today's trace has 262 variants and 436 states.
- **Loud, not silent:**
  - The build logs every traced variant that matches nothing, which means the trace is older than the build's keywords.
  - The player is a development build carrying `StreamingAssets/traced098.json`. Only in such a player, `ShaderTraceWatch098` logs once each kit variant the game asks for that the trace left out, for example `ShaderTrace098: not traced: DecoKit/Lit Cutaway pass 0 fragment: FOG_LINEAR …`. Over the 14 shots it named 77: third-person linear fog, `_RECEIVE_SHADOWS_OFF`, cascades on Complex Lit and others.
  - `-strictVariants` also draws those with the error shader (strict variant matching). Today that floods: about 150,000 lines a run and every lightmapped surface magenta, because of the gap below.

## For others (not changed here)
1. **The zone's lightmaps are directional, but the player draws them flat** (Glint, Latch). In the fresh trace, 40 of 84 Lit Cutaway draw states ask for `DIRLIGHTMAP_COMBINED`. GraphicsSettings strips directional-lightmap variants (`m_LightmapKeepDirCombined: 0`), so the player silently uses the plain lightmap variant, while the Editor draws directional. There are two ways out:
   - Bake non-directional. Nothing changes in the player, the bake is smaller, and the Editor matches the player.
   - Keep the directional variants. The player then matches the Editor, at +50% for lightmapped variants.

   Either one also makes `-strictVariants` usable.
2. **The world builder's surface materials enable GPU instancing** (`Editor/World/Look.cs:261`, Latch). They are a third of DecoKit/Lit Cutaway's variants: 3,264 of 9,792 before, 816 of 2,448 now. With GRD and the SRP Batcher on, instancing only applies to renderers with a property block. Turning it off would cut that third, after a check that those draws still batch.
3. **`ShaderStrip097`'s old comment** said its list kept the cutaway compile down; the census shows URP was doing it.
