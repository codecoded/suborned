using System;
using UnityEngine;

namespace Suborned.Perf
{
    /// <summary>What a perf run measures (brief 008 B6; Redline's review out/review/008-b6-perf.html, item 0 and M1-M8): the play
    /// camera's rig, the stops (and pan paths) it is driven through, the passes (vSync, window, timings, repeats) and the variants
    /// (A/B switches) recorded at each stop. Resolved in the Editor from the built scene (PerfBuild: stand-offs by Rig.StandOff, the
    /// scene's fingerprint and export sha256) and read by the player's PerfHarness from `-perf`. JsonUtility shape.</summary>
    [Serializable]
    public sealed class PerfPlan
    {
        public string fingerprint, exportSha256, source, scene, content;
        /// <summary>Brief 075 Perf A/B: the camera the harness drives, by GameObject name; "" = Camera.main (cameras/play, the real rig).
        /// The old-built town has no play rig: its "DK street study camera" is posed orthographically at a fixed stand-off (SwapShots' pose).</summary>
        public string camera = "";
        /// <summary>exportSha256 of a scene without a WorldStamp (the old-built town).</summary>
        public const string NoStamp = "no WorldStamp";
        /// <summary>Perf honesty (Redline 2026-10-15 M3): `fingerprint` is the world's (root, batches, cameras: the builder's log);
        /// sceneFingerprint covers every scene root the player is built with, sorted by name (`roots`); `stripped` names the roots
        /// the perf build left out (PerfStrip), with what they held; `people` is the person capsules built into the player, which
        /// the harness checks against the scene it runs.</summary>
        public string sceneFingerprint;
        public string[] roots = Array.Empty<string>(), stripped = Array.Empty<string>();
        public int people;
        /// <summary>The play camera as built: pitch and yaw (deg), view aspect, far plane beyond the stand-off.</summary>
        public float pitch, yaw, aspect, farMargin;
        public int width, height;
        public PerfWarmup warmup = new();
        public PerfPass[] passes = Array.Empty<PerfPass>();
        public PerfVariant[] variants = Array.Empty<PerfVariant>();
        public PerfStop[] stops = Array.Empty<PerfStop>();
    }

    /// <summary>Warm-up: the stops cycle unrecorded (base variant, the first pass's frame pacing), dwellSeconds each, and every dwell's
    /// frame p50 goes into the trace. It ends when two full cycles in a row differ by less than `plateau` (a share) in frame p50,
    /// once minSeconds have passed; maxSeconds caps it (the report then says it did not settle).</summary>
    [Serializable]
    public sealed class PerfWarmup { public float minSeconds, maxSeconds, dwellSeconds = 10f, plateau = 0.02f; }

    /// <summary>One way of recording: vSync 0 = uncapped (headroom: p50/p95/p99), 1 = capped at the display (the share of frames
    /// past `missMs` is the missed-vblank rate). Stops rotate by repeat; at each stop the variants rotate too and record back to
    /// back, so a variant's difference is not the machine drifting. repeatFirst records the pass's first segment again at its end
    /// (the A...A check). Empty stops / variants = all of the plan's.</summary>
    [Serializable]
    public sealed class PerfPass
    {
        public string name, why;
        public int vSync, repeats = 1;
        public string window = "FullScreenWindow";
        public float settleSeconds = 2f, recordSeconds = 10f, missMs = 17.5f;
        public string[] stops = Array.Empty<string>(), variants = Array.Empty<string>();
        public bool repeatFirst, shots;
    }

    /// <summary>A/B switches, each "" (or 0) = as built. ssao: "on" = as built, "depth" = after opaques from the depth texture (no
    /// prepass; as built since B6), "normals" = before opaques from the depth-normals prepass (as built before B6), "off". Since 064 A3
    /// item 1 the orthographic play camera renders without SSAO as built ("on" keeps that); "depth" and "normals" turn it back on there too.
    /// cascades: the play camera's shadow cascades (TacticalShadows; 2 as built since B6, 4 before). proxies: "on" = kit facades and
    /// street trees cast from their LOD1 shadow proxy (ShadowProxy) and no LOD casts its own (as built since B6), "off" = every LOD
    /// casts as before. fade: "tile" = the cut's discarding variant takes whole 64 m batch tiles (as built before B6), "reach" =
    /// only the fade cells it reaches (as built since B6), "off" = no fade (as fadeOff). A variant that names every switch measures the same switches in any build: `as-was` sets the switches of the build before B6,
    /// on the geometry of the player it runs in (since B6: 16 m fade cells, uncompressed vertices), so it is "as-was (emulated on B6 geometry)", not the pre-B6 player (Redline 2026-10-15 M4).</summary>
    [Serializable]
    public sealed class PerfVariant
    {
        public string name, why, ssao = "", proxies = "", fade = "";
        /// <summary>Brief 072-E (Redline's plan 5): everyFace "on" = the every-face modules (as built), "off" = the procedural walls and skin as
        /// they were before them (EveryFace: development builds only); moduleShadows "proxy" = modules cast from their L2 proxy (as built),
        /// "lod0" = the modules cast from their own LODs (L0 at play zoom), no module proxies.</summary>
        public string everyFace = "", moduleShadows = "";
        /// <summary>Brief 072-F (Redline phase 2, 4.3): the Air's texture A/B. "on" = the surface detail as built, "off" = FacadeLit's surface-detail
        /// array and the DecoKit detail slots (kit rows and builder Look rows) skipped at runtime by the global _SubornedSurfaceDetailOff
        /// (LookLab): same materials, same variants; measures the fetches and their ALU, not a different build. "" = as built (on).</summary>
        public string surfaceDetail = "";
        /// <summary>Brief 075 Perf A/B (Redline, perf-075ab.json), each "" (or 0) = as built, each refused on a scene without its target:
        /// oldCrowd "on" | "off" = renderers under the old town's baked glTF crowd roots (District / Southside crowd and traffic) on or off;
        /// msaa 1 | 2 | 4 | 8 = the active pipeline asset's msaaSampleCount; oldLens "on" | "off" = the old town's Gaussian DOF (scene volumes)
        /// and the sun's LensFlareComponentSRP, and the asset's depth and opaque textures always (on) or by request (off); pipeline
        /// "old" | "ours" = QualitySettings.renderPipeline: the player's quality-level pipeline as built (the old PC_RPAsset in the old-town
        /// player) or the project's own (GraphicsSettings.defaultRenderPipeline). All four are serialized, so the report's variants echo them.</summary>
        public string oldCrowd = "", oldLens = "", pipeline = "";
        public int msaa;
        public int cascades;
        public bool shadowsOff, fadeOff;

        public bool UsesProxies => proxies == "on" || proxies == "off";

        public string Describe() => $"ssao {(string.IsNullOrEmpty(ssao) ? "as built" : ssao)}, cascades {(cascades > 0 ? cascades.ToString() : "as built")}, shadows {(shadowsOff ? "off" : "on")}, fade {(fadeOff ? "off" : string.IsNullOrEmpty(fade) ? "as built" : fade)}, proxies {(string.IsNullOrEmpty(proxies) ? "as built" : proxies)}, every face {(string.IsNullOrEmpty(everyFace) ? "as built" : everyFace)}, module shadows {(string.IsNullOrEmpty(moduleShadows) ? "as built" : moduleShadows)}, surface detail {(string.IsNullOrEmpty(surfaceDetail) ? "as built" : surfaceDetail)}, old crowd {(string.IsNullOrEmpty(oldCrowd) ? "as built" : oldCrowd)}, msaa {(msaa > 0 ? msaa.ToString() : "as built")}, old lens {(string.IsNullOrEmpty(oldLens) ? "as built" : oldLens)}, pipeline {(string.IsNullOrEmpty(pipeline) ? "as built" : pipeline)}";
    }

    [Serializable]
    public sealed class PerfStop
    {
        public string name, why;
        /// <summary>World point the play camera frames (its ArchitectureFade focus; y = the level: 0 street, 9.43 deck, 24 concourse).</summary>
        public Vector3 focus;
        /// <summary>Orthographic size, and the distance back along the view that clears the frame column (Rig.StandOff).</summary>
        public float zoom, standOff;
        /// <summary>Brief 072-E: the stop's own camera yaw (deg); negative = the plan's (the built play camera's). PerfBuild resolves the stand-off with it.</summary>
        public float yaw = -1f;
        /// <summary>A pan: the focus moves along these samples at `speed` m/s while the stop records (one way; the record lasts
        /// the path). Empty = a parked camera at `focus`.</summary>
        public PerfPathPoint[] path = Array.Empty<PerfPathPoint>();
        public float speed;

        public bool IsPath => path is { Length: > 1 };
    }

    [Serializable]
    public struct PerfPathPoint { public Vector3 focus; public float standOff, along; }
}
