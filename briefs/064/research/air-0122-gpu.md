# Air 0.2.122: sources for the thermal read and the GPU savings (Redline, 2026-10-26)

Used for the section "Air 0.2.122 (2026-10-26)" in briefs/064-code-review-1024.md. The GDD and the brief stay the source; these back the estimates.

## The Air throttles under sustained GPU load
- Notebookcheck, "Apple MacBook Air 15 M4 review" (2025): "Under longer load, the GPU's performance dropped by around 22%." In the combined CPU/GPU stress test the SoC "initially consumed 29 watts" and "this quickly dropped to around 9 watts"; the CPU alone goes from 20.5 W (PL2) to about 15.5 W after 1 min, 12 W after 5 min, and settles at 9 W after 20 min. https://www.notebookcheck.net/Apple-MacBook-Air-15-M4-review.976933.0.html (German original: https://www.notebookcheck.com/Apple-MacBook-Air-15-M4-im-Test-Der-luefterlose-M4-SoC-ist-der-Konkurrenz-um-Jahre-voraus.973111.0.html)
- Takeaway: a ~22 % GPU loss is the published figure (about 1.28x frame time). We saw 1.40-1.56x, so either this load throttles harder (GPU and CPU both loaded, full-panel window), or part of the rise is something else. The trace decides. Equilibrium can take ~20 min, so a 10-min soak may not be fully settled.

## Reading the GPU clock without sudo
- macmon (vladkens): "sudoless performance monitoring for Apple Silicon", M1-M5; reads CPU, GPU and ANE power, average CPU and GPU temperature and the GPU frequency and active ratio through a private macOS API. `brew install macmon`; `macmon pipe -s <n> -i <ms>` prints one JSON object a sample (`gpu_freq_mhz`, `gpu_active_ratio`, `gpu_power`, `temp.gpu_temp_avg`, `temp.cpu_temp_avg`, `sys_power`; check the field names on the first line). It warns that on macOS 27 some power counters may read 0 on unsupported configurations: 0 = unavailable, not 0 W. https://github.com/vladkens/macmon
- `NSProcessInfo.processInfo.thermalState` (0 nominal, 1 fair, 2 serious, 3 critical), read through `osascript -l JavaScript` (already used by PerfHost.Thermal, unity/Assets/Suborned/Runtime/Perf/PerfHost.cs:75). Coarse but needs nothing installed.
- `pmset -g therm` / `pmset -g thermlog` (man pmset): "thermal conditions that affect CPU speed. Not available on all platforms". On this Apple Silicon run it printed nothing, as expected. powermetrics, asitop need sudo: not used.

## SSAO (URP 17.6, the project's package)
- Unity, "SSAO Renderer Feature reference" (6000.3): Downsample "reduces the resolution of the Pass ... by a factor of two", impact "Very high" (a quarter of the pixels); Blur Quality High = bilateral, three passes; Medium = Gaussian, two; Low = Kawase, one; impact "Very high"; Samples High 12 / Medium 8 / Low 4, "an increase ... from 4 to 8 doubles the computational load"; Falloff Distance: "SSAO does not apply to objects farther than this distance from the Camera"; After Opaque "can also increase performance on mobile devices that use tile-based rendering". https://docs.unity3d.com/Manual/urp/ssao-renderer-feature-reference.html
- The package source, read here: `unity/Library/PackageCache/com.unity.render-pipelines.universal@1e1cf3dcbd40/ShaderLibrary/SSAOCommon.hlsl:184` `ShouldSkipAO` returns no occlusion when `halfLinearDepth > falloff`; `GetLinearEyeDepth` (line 147) uses `LinearDepthToEyeDepth` for `_ORTHOGRAPHIC`, which is metres from the camera (ShaderVariablesFunctions.hlsl:544). SSAO.hlsl:370 then scales what is left by `(1 - depth/falloff)^2`. The play camera stands 160 m back (HandoverHost.standOff, HarbourDistrict.unity `standOff: 160`); the renderer's Falloff is 100 (PC_Renderer.asset). So the AO pass returns 0 for every street pixel at play zoom while the blur and composite passes still run full-screen.

## URP asset settings
- Unity, "Universal Render Pipeline Asset reference": Soft Shadows "High impact on platforms that use tile-based rendering"; Soft Shadow Quality Low = 4 PCF taps, Medium = 5x5 tent ("desktop"), High = 7x7 tent ("higher performance impact"); Cascade Count "increasing the number of cascades reduces the performance"; Render Scale and the upscaling filters (FSR 1.0 needs shader model 4.5; STP needs compute and forces TAA); Opaque Texture "works like the GrabPass"; HDR precision 64-bit "requires higher bandwidth". https://docs.unity3d.com/Manual/urp/universalrp-asset.html
- Unity, "Spatial-Temporal Post-processing": compute shaders (SM 5.0), not GLES, turns TAA on. https://docs.unity3d.com/Manual/urp/stp/stp-upscaler.html
- Unity, `Recorder.gpuElapsedNanoseconds`: per-sampler GPU time, valid only where `SystemInfo.supportsGpuRecorder`, three frames late. The way to get a per-pass GPU split on the Air. https://docs.unity3d.com/ScriptReference/Profiling.Recorder-gpuElapsedNanoseconds.html
- Tile-Only Mode (this URP, UniversalRendererDataEditor.cs:43-47): keeps the camera targets in tile memory but "'Post-processing' is incompatible": not usable with the grade, bloom and SMAA.

## Bandwidth
- Apple, "Apple introduces M4 Pro and M4 Max" (2024-10-30): base M4 "higher memory bandwidth of 120GB/s". https://www.apple.com/newsroom/2024/10/apple-introduces-m4-pro-and-m4-max/
- Arithmetic used: at 2880 x 1864 (5.37 Mpx) one 32-bit full-screen target is 21.5 MB, so one full-screen read + write is about 43 MB, 0.36 ms at the ideal 120 GB/s (shared with the CPU and the compositor): 0.3-0.6 ms a pass in practice, more warm.

## The project's own Air measurements used
- briefs/008/perf/ab-1/table.txt (008 B6, Air, 1920 x 1080, world only, contaminated: ratios only): SSAO from depth after opaque costs 1.2-1.9 ms over SSAO off (1.6-1.9 at five of seven stops; heaviest 11.50 vs 9.92, home 6.31 vs 4.45, parade 6.81 vs 5.63); shadows off -24 to -40 %; the fade's discard (since limited to the reach) halved the heaviest stop.
- briefs/064/air-0122/ (this run): tplong cold and warm, perf064, late at 1920 x 1080.

## What forces the depth texture (package source, read here)
- `UniversalRenderPipeline.CheckPostProcessForDepth` (Runtime/UniversalRenderPipeline.cs:1477): post needs the depth texture when DoF or motion blur is active, or when a data-driven lens flare with occlusion is registered (`LensFlareCommonSRP.IsOcclusionRTCompatible() && !Instance.IsEmpty() && asset.supportDataDrivenLensFlare`). AtmosphereLens keeps the sun's LensFlareComponentSRP enabled with `useOcclusion = true` and only sets its intensity to 0 at play (AtmosphereLens.cs:440-442, 529-533), so at play it still forces depth.
