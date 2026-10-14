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
    /// prepass; as built since B6), "normals" = before opaques from the depth-normals prepass (as built before B6), "off".
    /// cascades: the play camera's shadow cascades (TacticalShadows; 2 as built since B6, 4 before). proxies: "on" = kit facades and
    /// street trees cast from their LOD1 shadow proxy (ShadowProxy) and no LOD casts its own (as built since B6), "off" = every LOD
    /// casts as before. A variant that names every switch measures the same scene in any build: `as-was` is the build before B6.</summary>
    [Serializable]
    public sealed class PerfVariant
    {
        public string name, why, ssao = "", proxies = "";
        public int cascades;
        public bool shadowsOff, fadeOff;

        public bool UsesProxies => proxies == "on" || proxies == "off";

        public string Describe() => $"ssao {(string.IsNullOrEmpty(ssao) ? "as built" : ssao)}, cascades {(cascades > 0 ? cascades.ToString() : "as built")}, shadows {(shadowsOff ? "off" : "on")}, fade {(fadeOff ? "off" : "on")}, proxies {(string.IsNullOrEmpty(proxies) ? "as built" : proxies)}";
    }

    [Serializable]
    public sealed class PerfStop
    {
        public string name, why;
        /// <summary>World point the play camera frames (its ArchitectureFade focus; y = the level: 0 street, 9.43 deck, 24 concourse).</summary>
        public Vector3 focus;
        /// <summary>Orthographic size, and the distance back along the view that clears the frame column (Rig.StandOff).</summary>
        public float zoom, standOff;
        /// <summary>A pan: the focus moves along these samples at `speed` m/s while the stop records (one way; the record lasts
        /// the path). Empty = a parked camera at `focus`.</summary>
        public PerfPathPoint[] path = Array.Empty<PerfPathPoint>();
        public float speed;

        public bool IsPath => path is { Length: > 1 };
    }

    [Serializable]
    public struct PerfPathPoint { public Vector3 focus; public float standOff, along; }
}
