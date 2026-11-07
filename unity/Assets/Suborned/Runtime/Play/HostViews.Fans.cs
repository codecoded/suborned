using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Play
{
    /// <summary>Sight fans (the Scout's skill; Sky 2026-10-16, Glyph's review §3.4): no cone by default; the host asks each frame for the fans it
    /// wants and how strongly (ShowFans; brief 067 F: 0 or 1, in over 0.25 s, out over 0.6 s). Each is the engine's own sight polygon (Game.ConeFan),
    /// read into the world once a pose: its rings (a mast's heads too), their stops, the shadows it does not see into, and the fill's triangles with
    /// those shadows cut out. The drawing is by signal colour (HostViews.FanUnion: Sky 2026-11-07, "every rival having a cone - dominates the game"):
    /// a colour's fans draw as one shape. Grey when the watcher sees nothing now; a looped camera hollow and dashed. Nothing here feeds the engine.</summary>
    public sealed partial class HostViews
    {
        sealed class FanDraw
        {
            public string Watcher = "", Kind = "", Group = ""; public Fan For; public bool Live, Looped, Shown;
            /// <summary>Brief 067 F3: how strong it draws now (0..1), eased toward Want (0 or 1).</summary>
            public float Strength, Want;
            public Vector3 Hub; public float Reach = 1f;
            /// <summary>The polygons in the world (Points, then a mast's other heads), each ring's stops, and the shadows (convex), once a pose.</summary>
            public readonly List<List<Vector3>> Rings = new(); public readonly List<string[]> Stops = new(); public readonly List<Vector3[]> Shades = new();
            /// <summary>The fill: a fan of triangles from the lens to each ring, the shadows cut out (world, FillY up).</summary>
            public readonly List<Vector3> FillV = new(); public readonly List<int> FillI = new();
            /// <summary>Its outline (rims and far-edge cues), its shadow cuts' ink and a grey fan's hatch, once a pose (HostViews.FanUnion.RimsOf).</summary>
            public readonly List<Vector3> RimV = new(), InkV = new(), HatchV = new(); public readonly List<int> RimI = new(), InkI = new(), HatchI = new();
            /// <summary>A count that moves with every rebuild (the union's cache key).</summary>
            public int Version;
        }
        readonly Dictionary<string, FanDraw> fanBy = new(); readonly HashSet<string> fanSeen = new();
        readonly List<Vector3> verts = new(); readonly List<int> tris = new(); readonly List<Color> cols = new();
        const float InS = 0.25f, OutS = 0.6f, FillY = 0.05f, RimY = 0.07f, RimW = 0.085f; // Glyph techwatch r2 C: the rim 2 px or more at play zoom where it runs across the view (0.085 m x 38.6 px/m x sin 38), the fill a little up, so a cone under a ghosted tower's dither still reads
        static readonly Color Grey = new(0.55f, 0.55f, 0.55f);
        /// <summary>The perf probe reads them: fan geometry builds (a pose read into the world) and union builds since the last read.</summary>
        public static int FanGeometries, FanUnions;

        /// <summary>The proof reads it: the watchers whose drawn fan is looped (hollow, dashed all round).</summary>
        public string LoopedFans { get { var l = new List<string>(); foreach (var kv in fanBy) if (kv.Value.Shown && kv.Value.Looped) l.Add(kv.Key); return string.Join(" ", l); } }
        /// <summary>The proof reads it: each drawn fan's watcher and strength now ("camera:entrance=0.00"); brief 067 F: a cone gone from the street (0, hidden) still lists at 0.</summary>
        public string FanStrengths { get { var l = new List<string>(); foreach (var kv in fanBy) l.Add($"{kv.Key}={(kv.Value.Shown ? kv.Value.Strength : 0f):0.00}"); return string.Join(" ", l); } }

        /// <summary>The fans to draw this frame and which of them matter (strong); the rest fade out and hide. A fan's world geometry is rebuilt when the
        /// engine hands back a different Fan (its cache keeps one per pose) or its Live or looped state flips; then the colours' unions are drawn.</summary>
        public void ShowFans(IReadOnlyList<Fan> list, IReadOnlyList<bool> strong = null)
        {
            var seen = fanSeen; seen.Clear(); int n = list?.Count ?? 0; float dt = Time.unscaledDeltaTime; // Redline 064 #2: one set, reused
            for (int i = 0; i < n; i++)
            {
                var f = list[i]; string key = f.Watcher.Length > 0 ? f.Watcher : "fan/" + i;
                if (CameraDead(f.Watcher) || FightCues.Blind && !f.Live) continue; // brief 084: the hollow cone's instead (HudToolkit.CuesBlind) // Glyph 058 §2: a camera cut or shot out has no cone (its dead disc stays)
                if (!seen.Add(key)) continue;
                if (!fanBy.TryGetValue(key, out var d)) fanBy[key] = d = new FanDraw { Watcher = key };
                d.Want = strong == null ? 1f : i < strong.Count && strong[i] ? 1f : 0f;
                // brief 067 F3 (Glyph): no rest strength: a cone that stops mattering leaves the street (out over 0.6 s), one that starts comes in over 0.25 s
                if (!d.Shown) { if (d.Want <= 0f) continue; d.Shown = true; d.Strength = 0f; d.For = null; }
                d.Strength = Mathf.MoveTowards(d.Strength, d.Want, dt / (d.Want > d.Strength ? InS : OutS));
                if (d.Strength <= 0f && d.Want <= 0f) { d.Shown = false; d.For = null; continue; }
                if (!ReferenceEquals(d.For, f) || d.Live != f.Live || d.Looped != LoopedFan(f)) { long t0 = fanTiming ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; Build(d, f); if (fanTiming) { long took = System.Diagnostics.Stopwatch.GetTimestamp() - t0; poseTicks += took; poseMaxTicks = System.Math.Max(poseMaxTicks, took); } }
            }
            foreach (var kv in fanBy) if (!seen.Contains(kv.Key) && kv.Value.Shown) { kv.Value.Shown = false; kv.Value.For = null; }
            UnionFrame();
        }

        /// <summary>The fan's pose in the world: rings, stops, shadows, the fill's triangles (shadows cut out), its outline and its colour's group.</summary>
        void Build(FanDraw d, Fan f)
        {
            FanGeometries++; d.Version++;
            d.For = f; d.Live = f.Live; d.Kind = f.Kind; d.Looped = LoopedFan(f);
            d.Group = d.Looped ? "loop:" + d.Watcher : !d.Live ? "grey" : f.Kind == "guard" ? "amber" : f.Kind == "rivals" ? "violet" : "red"; // §10: cones red, guard amber, rivals violet
            d.Rings.Clear(); d.Stops.Clear();
            var r0 = new List<Vector3>(f.Points.Length); foreach (var p in f.Points) r0.Add(frame.World(p)); d.Rings.Add(r0); d.Stops.Add(f.Stop);
            for (int h = 0; h < f.More.Length; h++) // a mast (brief 013): one polygon a head; heads 2 and 3 are the fan's More, from the same pole
            {
                var r = new List<Vector3>(f.More[h].Length); foreach (var q in f.More[h]) r.Add(frame.World(q));
                d.Rings.Add(r); d.Stops.Add(h < f.MoreStop.Length ? f.MoreStop[h] : System.Array.Empty<string>());
            }
            d.Hub = frame.World(f.At); d.Reach = 1f; foreach (var p in r0) d.Reach = Mathf.Max(d.Reach, Flat(p - d.Hub).magnitude);
            ShadowsOf(f); d.Shades.Clear(); d.Shades.AddRange(shadows);
            // the fill: a fan of triangles from the watcher (the engine's polygon is star-shaped round it: its rays start there), the shadows cut out
            verts.Clear(); tris.Clear();
            foreach (var ring in d.Rings)
            {
                int m = ring.Count; if (m < 3) continue; int b0 = verts.Count;
                verts.Add(d.Hub + Vector3.up * FillY);
                for (int i = 0; i < m; i++) verts.Add(ring[i] + Vector3.up * FillY);
                for (int i = 0; i < m; i++) { tris.Add(b0); tris.Add(b0 + 1 + i); tris.Add(b0 + 1 + (i + 1) % m); }
            }
            CutFill(); // engine 0.28 (hud29 securityFan.shadows): Fan.Shadows cut out of the fill
            d.FillV.Clear(); d.FillV.AddRange(verts); d.FillI.Clear(); d.FillI.AddRange(tris);
            RimsOf(d);
        }

        Color KindColour(string kind) => kind switch { "guard" => host.guardColour, "rivals" => host.rivalColour, _ => host.coneColour }; // §10: cones red, guard amber, rivals violet

        /// <summary>Glyph 058 §2 (board 02): the camera the running loop plays on: its cone draws recorded, not live: still red, hollow, dashed all round.</summary>
        bool LoopedFan(Fan f) { var g = host.Game; return g != null && !f.Live && g.LoopOn && f.Watcher == "camera:" + g.LoopCam; }
        bool CameraDead(string watcher) { var g = host.Game; if (g == null || !watcher.StartsWith("camera:", System.StringComparison.Ordinal)) return false; try { return g.CameraDeadAs(watcher[7..]).Length > 0; } catch { return false; } }

        readonly Dictionary<(Color, int, int, CompareFunction, StencilOp), Material> unionMats = new();
        /// <summary>A union's material: the signal shader (unlit, no fog, both faces, transparent), its alpha from the mesh's vertex colours (SignalUnlit
        /// _VertexAlpha). With a stencil bit it tests (and the fill writes) that colour's bit: the fill NotEqual then Replace (a pixel fills once), the
        /// lines NotEqual and Keep (only outside every fill of the colour), the hatch Equal (only inside). `queue` is added to the transparent queue so
        /// every fill draws before the ink, the lines and the hatch.</summary>
        Material UnionMat(Color c, int bit, int queue, CompareFunction comp, StencilOp pass)
        {
            if (unionMats.TryGetValue((c, bit, queue, comp, pass), out var m)) return m;
            m = host.SignalMat(new Color(c.r, c.g, c.b, 0.999f), true); m.name = queue == 0 ? "signal-union-fill" : "signal-union-line";
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f); // Glint 2026-10-23: no fog on a signal
            if (m.HasProperty("_OffsetFactor")) { m.SetFloat("_OffsetFactor", -2f); m.SetFloat("_OffsetUnits", -2f); } // Aster 2026-10-23 (Glint's 150 m control): the flat fan wins its depth ties with kerbs and paving
            if (m.HasProperty("_VertexAlpha")) m.SetFloat("_VertexAlpha", 1f);
            if (bit > 0 && m.HasProperty("_StencilComp")) { m.SetFloat("_StencilRef", bit); m.SetFloat("_StencilMask", bit); m.SetFloat("_StencilComp", (float)comp); m.SetFloat("_StencilPass", (float)pass); }
            m.renderQueue += queue;
            return unionMats[(c, bit, queue, comp, pass)] = m;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        // ---------------------------------------------------------------- the marks a rim is made of (each appends to verts and tris)

        void Quad(Vector3 a, Vector3 b, float w)
        {
            var side = Vector3.Cross(Vector3.up, Flat(b - a)).normalized * (w * 0.5f); int k = verts.Count;
            verts.Add(a - side); verts.Add(a + side); verts.Add(b + side); verts.Add(b - side);
            tris.Add(k); tris.Add(k + 1); tris.Add(k + 2); tris.Add(k); tris.Add(k + 2); tris.Add(k + 3);
        }

        void Chevron(Vector3 p, Vector3 o, float s, float w)
        {
            var side = Vector3.Cross(Vector3.up, o) * (s * 0.6f); var tip = p + o * (s * 0.5f); var back = p - o * (s * 0.3f);
            Quad(back + side, tip, w); Quad(back - side, tip, w);
        }

        void Diamond(Vector3 p, Vector3 o, float s)
        {
            var side = Vector3.Cross(Vector3.up, o) * (s * 0.35f); int k = verts.Count;
            verts.Add(p + o * (s * 0.5f)); verts.Add(p + side); verts.Add(p - o * (s * 0.5f)); verts.Add(p - side);
            tris.Add(k); tris.Add(k + 1); tris.Add(k + 2); tris.Add(k); tris.Add(k + 2); tris.Add(k + 3);
        }

        readonly List<float> hatchCuts = new();
        /// <summary>A grey fan's diagonal hatching (0.9 m apart, hairline): each line of x − z = c is cut by the polygon's edges and drawn between pairs
        /// of crossings (even-odd), so it stays inside the notched shape. The lines are the world's, so two grey fans' hatches lie on the same lines.</summary>
        void Hatch(List<Vector3> ring)
        {
            int n = ring.Count; float lo = float.MaxValue, hi = float.MinValue, y = 0f;
            for (int i = 0; i < n; i++) { float c = ring[i].x - ring[i].z; lo = Mathf.Min(lo, c); hi = Mathf.Max(hi, c); y += ring[i].y / n; }
            y += RimY;
            for (float c = Mathf.Ceil(lo / 0.9f) * 0.9f; c < hi; c += 0.9f)
            {
                hatchCuts.Clear();
                for (int i = 0; i < n; i++)
                {
                    var a = ring[i]; var b = ring[(i + 1) % n]; float ca = a.x - a.z - c, cb = b.x - b.z - c;
                    if ((ca < 0f) == (cb < 0f)) continue;
                    hatchCuts.Add(Mathf.Lerp(a.x + a.z, b.x + b.z, ca / (ca - cb))); // position along the line (x + z)
                }
                hatchCuts.Sort();
                for (int k = 0; k + 1 < hatchCuts.Count; k += 2)
                {
                    float s0 = hatchCuts[k], s1 = hatchCuts[k + 1];
                    Quad(new Vector3((s0 + c) * 0.5f, y, (s0 - c) * 0.5f), new Vector3((s1 + c) * 0.5f, y, (s1 - c) * 0.5f), RimW * 0.5f);
                }
            }
        }
    }
}
