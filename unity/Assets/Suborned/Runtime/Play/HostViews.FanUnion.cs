using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Play
{
    /// <summary>Sky 2026-11-07 (playing, five rival cones over the Office corner: "that's a bit much - every rival having a cone - dominates the game"):
    /// the fans of one signal colour draw as one shape, their union: rivals violet, the guard amber, cameras and police red, a fan that sees nothing
    /// grey (hatched); a looped camera stays its own hollow dashed cone. All of it on the GPU, in the signal pass's own cleared depth-stencil
    /// (SignalsFeature), each colour on its own stencil bit (SignalUnlit's stencil state):
    ///  · one light fill: the colour's fills in one mesh, NotEqual then Replace, so a pixel any of its fans sees fills once, flat at UnionFillA (overlaps
    ///    never darken); the strongest fan's triangles first, so an overlap takes the strongest one's strength;
    ///  · one outline round the outside: each fan's lines (rims, far-edge cues, shadow cuts and their ink) drawn after every fill, NotEqual and Keep,
    ///    centred on its edges at twice their width, so the stencil clips the inner half and every stretch inside any fan of the colour: what stays
    ///    is the shape's outside edge at its width, and no inner edge or dotted rim inside it. The cues sit just outside the edge.
    /// A fan's lines are built once a pose (RimsOf); a colour's meshes are its fans' lists put together when one changes, joins or leaves (no
    /// geometry between fans on the CPU); a strength step rewrites colours and the fill's order only. Nothing here reads or changes the run.</summary>
    public sealed partial class HostViews
    {
        sealed class FanGroup
        {
            public string Key = ""; public int Bit; public bool Loop, Hatched; public GameObject Go;
            public Mesh Fill, Rim, Ink, Hatch; public MeshRenderer FillR, RimR, InkR, HatchR;
            public readonly List<FanDraw> Members = new();
            /// <summary>The members and their versions the meshes were built from.</summary>
            public readonly List<(FanDraw d, int v)> Built = new();
            /// <summary>Per vertex: the member it belongs to; each member's run of fill indices; the fill's member order last drawn.</summary>
            public readonly List<int> FillOwner = new(), RimOwner = new(), InkOwner = new(), HatchOwner = new(), AllI = new(), TriFrom = new(), TriN = new(), Order = new();
            public readonly List<float> Applied = new();
        }
        readonly Dictionary<string, FanGroup> fanGroups = new(); readonly List<FanGroup> fanGroupList = new();
        readonly List<int> order = new();
        /// <summary>The union's flat fill (Sky: one light fill, overlaps never darken); a looped camera's dashed rim.</summary>
        const float UnionFillA = 0.16f, LoopRimW = 0.05f, LoopRimA = 0.5f, LoopDashOn = 0.13f, LoopDashOff = 0.10f;

        /// <summary>The proof reads it: each colour drawn now and how many fans it merges ("violet:5 red:2").</summary>
        public string FanUnionsShown { get { var l = new List<string>(); foreach (var g in fanGroupList) if (g.Go.activeSelf && g.Members.Count > 0) l.Add($"{g.Key}:{g.Members.Count}"); return string.Join(" ", l); } }

        FanGroup GroupOf(string key)
        {
            if (fanGroups.TryGetValue(key, out var gr)) return gr;
            gr = new FanGroup { Key = key, Loop = key.StartsWith("loop:", System.StringComparison.Ordinal), Hatched = key == "grey", Go = new GameObject("fan/" + key) };
            gr.Bit = key switch { "violet" => 2, "amber" => 4, "red" => 8, "grey" => 16, _ => 0 }; // a looped camera has no fill: nothing to merge, no stencil
            gr.Go.transform.SetParent(root, false);
            (gr.Fill, gr.FillR) = MeshPart(gr.Go.transform, "fill"); (gr.Ink, gr.InkR) = MeshPart(gr.Go.transform, "shadow-edge");
            (gr.Rim, gr.RimR) = MeshPart(gr.Go.transform, "rim"); (gr.Hatch, gr.HatchR) = MeshPart(gr.Go.transform, "hatch");
            foreach (var r in new Renderer[] { gr.FillR, gr.InkR, gr.RimR, gr.HatchR }) r.gameObject.layer = HandoverHost.SignalLayer; // the signal pass, whose stencil is the cones' alone (the ink and grey too: they clip by it)
            fanGroups[key] = gr; fanGroupList.Add(gr); return gr;
        }

        static (Mesh, MeshRenderer) MeshPart(Transform parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name }; mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return (mesh, r);
        }

        static readonly System.Comparison<FanDraw> ByWatcher = (a, b) => string.CompareOrdinal(a.Watcher, b.Watcher);
        /// <summary>`-fanTiming` (a probe, off in play): every 300 frames the player log gets the unions' builds and their cost, and the host's sight
        /// cost a frame (HandoverHost.PerfSightTicks, which the flag turns on).</summary>
        static readonly bool fanTiming = GameBoot.Arg("-fanTiming");
        static long unionTicks, unionMaxTicks, sightTicks, sightMaxTicks, poseTicks, poseMaxTicks; static int unionBuilds, timedFrames, posesAt;

        /// <summary>Per frame after the fans' strengths: each shown fan into its colour, each colour's meshes put together if its fans changed, its colours set.</summary>
        void UnionFrame()
        {
            foreach (var gr in fanGroupList) gr.Members.Clear();
            foreach (var d in fanBy.Values) if (d.Shown && d.For != null) GroupOf(d.Group).Members.Add(d);
            foreach (var gr in fanGroupList)
            {
                if (gr.Members.Count == 0) { if (gr.Go.activeSelf) gr.Go.SetActive(false); gr.Built.Clear(); continue; }
                if (gr.Members.Count > 1) gr.Members.Sort(ByWatcher);
                if (!gr.Go.activeSelf) gr.Go.SetActive(true);
                if (!BuiltFrom(gr)) { long t0 = fanTiming ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; BuildUnion(gr); if (fanTiming) { long dt = System.Diagnostics.Stopwatch.GetTimestamp() - t0; unionTicks += dt; unionMaxTicks = System.Math.Max(unionMaxTicks, dt); unionBuilds++; } }
                Restrength(gr);
            }
            if (fanTiming) FanTimingFrame();
        }

        void FanTimingFrame()
        {
            HandoverHost.PerfTiming = true; long st = HandoverHost.PerfSightTicks; sightTicks += st; sightMaxTicks = System.Math.Max(sightMaxTicks, st);
            if (++timedFrames < 300) return;
            double ms = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            Debug.Log($"fanTiming: {timedFrames} frames, {unionBuilds} union builds, {unionTicks * ms:0.0} ms in all (max {unionMaxTicks * ms:0.00} ms, {unionTicks * ms / timedFrames:0.000} ms a frame); {FanGeometries - posesAt} fan poses read, {poseTicks * ms:0.0} ms (max {poseMaxTicks * ms:0.00} ms, {poseTicks * ms / timedFrames:0.000} ms a frame); sight {sightTicks * ms / timedFrames:0.000} ms a frame (max {sightMaxTicks * ms:0.00}); shown {FanUnionsShown}");
            unionTicks = unionMaxTicks = sightTicks = sightMaxTicks = poseTicks = poseMaxTicks = 0; unionBuilds = timedFrames = 0; posesAt = FanGeometries;
        }

        static bool BuiltFrom(FanGroup gr)
        {
            if (gr.Built.Count != gr.Members.Count) return false;
            for (int i = 0; i < gr.Members.Count; i++) if (!ReferenceEquals(gr.Built[i].d, gr.Members[i]) || gr.Built[i].v != gr.Members[i].Version) return false;
            return true;
        }

        void BuildUnion(FanGroup gr)
        {
            FanUnions++;
            var ms = gr.Members; gr.Built.Clear(); foreach (var d in ms) gr.Built.Add((d, d.Version));
            // the fill: every member's triangles in one mesh (none for a looped camera: hollow); the order comes with the strengths (Restrength)
            verts.Clear(); gr.FillOwner.Clear(); gr.AllI.Clear(); gr.TriFrom.Clear(); gr.TriN.Clear();
            for (int i = 0; i < ms.Count; i++)
            {
                var d = ms[i]; int b0 = verts.Count; gr.TriFrom.Add(gr.AllI.Count);
                if (!gr.Loop) { verts.AddRange(d.FillV); for (int k = 0; k < d.FillV.Count; k++) gr.FillOwner.Add(i); foreach (var t in d.FillI) gr.AllI.Add(b0 + t); }
                gr.TriN.Add(gr.AllI.Count - gr.TriFrom[i]);
            }
            gr.Fill.Clear(); gr.Fill.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16; gr.Fill.SetVertices(verts);
            Concat(gr, 0, gr.Rim, gr.RimOwner); Concat(gr, 1, gr.Ink, gr.InkOwner); Concat(gr, 2, gr.Hatch, gr.HatchOwner);
            var c = gr.Hatched ? Grey : KindColour(ms[0].Kind);
            void Mat(Renderer r, Material m) { if (r.sharedMaterial != m) r.sharedMaterial = m; } // brief 045 F6: no material set when it is the same
            Mat(gr.FillR, UnionMat(c, gr.Bit, 0, CompareFunction.NotEqual, StencilOp.Replace)); Mat(gr.InkR, UnionMat(InkDeepC, gr.Bit, 1, CompareFunction.NotEqual, StencilOp.Keep));
            Mat(gr.RimR, UnionMat(c, gr.Bit, 2, CompareFunction.NotEqual, StencilOp.Keep)); Mat(gr.HatchR, UnionMat(c, gr.Bit, 3, CompareFunction.Equal, StencilOp.Keep));
            if (gr.FillR.enabled == gr.Loop) gr.FillR.enabled = !gr.Loop;
            gr.Applied.Clear(); gr.Order.Clear(); // the next Restrength writes every colour and the fill's order
        }

        /// <summary>One of the members' line lists (0 rims, 1 ink, 2 hatch) into the colour's mesh, each vertex's member noted.</summary>
        void Concat(FanGroup gr, int which, Mesh m, List<int> owner)
        {
            verts.Clear(); tris.Clear(); owner.Clear(); var ms = gr.Members;
            for (int i = 0; i < ms.Count; i++)
            {
                var d = ms[i]; var (vv, ii) = which == 0 ? (d.RimV, d.RimI) : which == 1 ? (d.InkV, d.InkI) : (d.HatchV, d.HatchI);
                int b0 = verts.Count; verts.AddRange(vv); for (int k = 0; k < vv.Count; k++) owner.Add(i); foreach (var t in ii) tris.Add(b0 + t);
            }
            m.Clear(); m.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16; m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateBounds();
        }

        /// <summary>A strength step: the fill's order (the strongest member first) and alpha (UnionFillA x strength); the lines' alpha (strength; a looped
        /// camera's at half, the ink at ShadowInkA). Skipped while no member's strength moved.</summary>
        void Restrength(FanGroup gr)
        {
            var ms = gr.Members; int n = ms.Count; bool moved = gr.Applied.Count != n;
            for (int i = 0; i < n && !moved; i++) { float s0 = ms[i].Strength; moved = Mathf.Abs(gr.Applied[i] - s0) > 0.02f || s0 == ms[i].Want && gr.Applied[i] != s0; }
            if (!moved) return;
            var s = gr.Applied; s.Clear(); foreach (var d in ms) s.Add(Mathf.Clamp01(d.Strength));
            order.Clear(); for (int i = 0; i < n; i++) { int at = order.Count; while (at > 0 && s[order[at - 1]] < s[i]) at--; order.Insert(at, i); } // a handful of members
            bool same = gr.Order.Count == n; for (int i = 0; i < n && same; i++) same = gr.Order[i] == order[i];
            if (!same && !gr.Loop)
            {
                tris.Clear(); foreach (var i in order) for (int k = 0; k < gr.TriN[i]; k++) tris.Add(gr.AllI[gr.TriFrom[i] + k]);
                gr.Fill.SetTriangles(tris, 0); gr.Order.Clear(); gr.Order.AddRange(order);
            }
            Alphas(gr.Fill, gr.FillOwner, s, UnionFillA); Alphas(gr.Rim, gr.RimOwner, s, gr.Loop ? LoopRimA : 1f);
            Alphas(gr.Ink, gr.InkOwner, s, ShadowInkA); Alphas(gr.Hatch, gr.HatchOwner, s, 1f);
        }

        void Alphas(Mesh m, List<int> owner, List<float> s, float a)
        {
            cols.Clear(); foreach (var o in owner) cols.Add(new Color(1f, 1f, 1f, a * s[o]));
            if (cols.Count == m.vertexCount) m.SetColors(cols);
        }

        // ---------------------------------------------------------------- one fan's lines, once a pose

        struct Piece { public float T0, T1; }
        readonly List<Piece> pieces = new(); readonly List<float> cutT = new();
        float ex0, ex1, ez0, ez1; // the edge being cut, its bounds

        /// <summary>A fan's lines: each ring's edges where they are not in its own shadows (a crisp line along walls and the near circle, the far edge in
        /// its kind's cue), each shadow cut's edges where they run inside the fan and outside its other shadows (engine 0.28, hud29 securityFan.shadows: a
        /// thin line of the colour over ink-deep), and a grey fan's hatch. Lines are twice their width, centred: the union's stencil clips the inner half.</summary>
        void RimsOf(FanDraw d)
        {
            verts.Clear(); tris.Clear(); underV.Clear(); underT.Clear();
            for (int r = 0; r < d.Rings.Count; r++)
            {
                var ring = d.Rings[r]; int n = ring.Count; if (n < 3) continue; var stop = d.Stops[r]; float dash = 0f;
                for (int k = 0; k < n; k++)
                {
                    Vector3 a = ring[k], b = ring[(k + 1) % n]; float len = Flat(b - a).magnitude; if (len < 1e-4f) continue;
                    CutStart(a, b); foreach (var sh in d.Shades) Cuts(a, b, sh); cutT.Sort(); pieces.Clear();
                    for (int c = 0; c + 1 < cutT.Count; c++) { float t0 = cutT[c], t1 = cutT[c + 1]; if (t1 - t0 > 1e-4f && !InShades(d.Shades, Vector3.Lerp(a, b, (t0 + t1) * 0.5f))) AddPiece(t0, t1); }
                    EdgeMarks(d, a, b, len, stop.Length == n && stop[k] == "reach" && stop[(k + 1) % n] == "reach", ref dash);
                }
            }
            foreach (var sh in d.Shades) ShadeEdges(d, sh);
            d.RimV.Clear(); d.RimV.AddRange(verts); d.RimI.Clear(); d.RimI.AddRange(tris); d.InkV.Clear(); d.InkV.AddRange(underV); d.InkI.Clear(); d.InkI.AddRange(underT);
            verts.Clear(); tris.Clear(); if (!d.Live && !d.Looped) foreach (var ring in d.Rings) if (ring.Count >= 3) Hatch(ring);
            d.HatchV.Clear(); d.HatchV.AddRange(verts); d.HatchI.Clear(); d.HatchI.AddRange(tris);
        }

        void ShadeEdges(FanDraw d, Vector3[] sh)
        {
            for (int k = 0; k < sh.Length; k++)
            {
                Vector3 a = sh[k], b = sh[(k + 1) % sh.Length]; if (Flat(b - a).sqrMagnitude < 1e-8f) continue;
                CutStart(a, b); foreach (var ring in d.Rings) Cuts(a, b, ring); foreach (var o in d.Shades) if (o != sh) Cuts(a, b, o);
                cutT.Sort(); pieces.Clear();
                for (int c = 0; c + 1 < cutT.Count; c++)
                {
                    float t0 = cutT[c], t1 = cutT[c + 1]; if (t1 - t0 < 1e-4f) continue;
                    var mid = Vector3.Lerp(a, b, (t0 + t1) * 0.5f); bool inside = false, other = false;
                    foreach (var ring in d.Rings) if (ring.Count >= 3 && InRing(ring, mid)) { inside = true; break; }
                    foreach (var o in d.Shades) if (o != sh && InConvex(o, mid)) { other = true; break; }
                    if (inside && !other) AddPiece(t0, t1);
                }
                var up = Vector3.up * FillY; float x2 = d.Looped ? 1f : 2f; // twice the width where a fill clips the inner half (a looped camera has none)
                foreach (var p in pieces) { var p0 = Vector3.Lerp(a, b, p.T0) + up; var p1 = Vector3.Lerp(a, b, p.T1) + up; Quad(p0, p1, ShadowRimW * x2); UnderQuad(p0, p1, ShadowInkW * x2); }
            }
        }

        /// <summary>A ring edge's marks along its pieces: a crisp line (walls, the near circle); a far edge in its kind's cue (the guard's chevrons on a
        /// crisp rim, the rivals' diamond ticks on a thin one, the red dashes 0.4 m on 0.3 off), its phase carried along the arc, the marks just outside
        /// the edge; a looped camera's rim dashed all round at its own width (Glyph 058 §2: 0.13 m on, 0.10 off; no fill clips it).</summary>
        void EdgeMarks(FanDraw d, Vector3 a, Vector3 b, float len, bool far, ref float dash)
        {
            Vector3 A = a + Vector3.up * FillY, B = b + Vector3.up * FillY; // the fill's height, so the stencil clips a line exactly at the fill's edge
            if (d.Looped)
            {
                float period = LoopDashOn + LoopDashOff;
                for (float t = -dash; t < len; t += period) Span(A, B, len, Mathf.Max(0f, t), Mathf.Min(len, t + LoopDashOn), LoopRimW);
                dash = (dash + len) % period; return;
            }
            if (!far || d.Kind is "guard" or "rivals") { float w = far && d.Kind == "rivals" ? RimW : RimW * 2f; foreach (var p in pieces) Span(A, B, len, p.T0 * len, p.T1 * len, w); }
            if (!far) return;
            var outward = Flat((a + b) * 0.5f - d.Hub).normalized;
            switch (d.Kind)
            {
                case "guard": // chevrons marching the way he faces (Glyph 020: the hairline left a flat amber wash)
                    for (; dash < len; dash += 0.7f) if (OnPiece(dash / len)) Chevron(Vector3.Lerp(A, B, dash / len) + outward * (0.32f * 0.3f + RimW), outward, 0.32f, RimW);
                    dash -= len; break;
                case "rivals": // diamond ticks
                    for (; dash < len; dash += 0.8f) if (OnPiece(dash / len)) Diamond(Vector3.Lerp(A, B, dash / len) + outward * 0.15f, outward, 0.3f);
                    dash -= len; break;
                default: // camera, patrol, officers
                    for (float t = -dash; t < len; t += 0.7f) Span(A, B, len, Mathf.Max(0f, t), Mathf.Min(len, t + 0.4f), RimW * 2f);
                    dash = (dash + len) % 0.7f; break;
            }
        }

        /// <summary>The stretch [s0, s1] metres along the edge, drawn where its pieces are.</summary>
        void Span(Vector3 A, Vector3 B, float len, float s0, float s1, float w)
        {
            if (s1 <= s0) return;
            foreach (var p in pieces) { float u0 = Mathf.Max(s0, p.T0 * len), u1 = Mathf.Min(s1, p.T1 * len); if (u1 > u0) Quad(Vector3.Lerp(A, B, u0 / len), Vector3.Lerp(A, B, u1 / len), w); }
        }

        bool OnPiece(float t) { foreach (var p in pieces) if (t >= p.T0 && t <= p.T1) return true; return false; }
        void AddPiece(float t0, float t1)
        {
            if (pieces.Count > 0 && Mathf.Abs(pieces[^1].T1 - t0) < 1e-5f) { var p = pieces[^1]; p.T1 = t1; pieces[^1] = p; }
            else pieces.Add(new Piece { T0 = t0, T1 = t1 });
        }

        void CutStart(Vector3 a, Vector3 b)
        {
            cutT.Clear(); cutT.Add(0f); cutT.Add(1f);
            ex0 = Mathf.Min(a.x, b.x) - 0.01f; ex1 = Mathf.Max(a.x, b.x) + 0.01f; ez0 = Mathf.Min(a.z, b.z) - 0.01f; ez1 = Mathf.Max(a.z, b.z) + 0.01f;
        }
        void Cuts(Vector3 a, Vector3 b, List<Vector3> poly) { int n = poly.Count; for (int k = 0; k < n; k++) CutAt(a, b, poly[k], poly[(k + 1) % n]); }
        void Cuts(Vector3 a, Vector3 b, Vector3[] poly) { int n = poly.Length; for (int k = 0; k < n; k++) CutAt(a, b, poly[k], poly[(k + 1) % n]); }
        /// <summary>Where segment c-e crosses the edge a-b (its parameter along a-b), on the ground plane; parallel segments are left to the pieces' tests.</summary>
        void CutAt(Vector3 a, Vector3 b, Vector3 c, Vector3 e)
        {
            if (Mathf.Max(c.x, e.x) < ex0 || Mathf.Min(c.x, e.x) > ex1 || Mathf.Max(c.z, e.z) < ez0 || Mathf.Min(c.z, e.z) > ez1) return;
            float rx = b.x - a.x, rz = b.z - a.z, sx = e.x - c.x, sz = e.z - c.z, den = rx * sz - rz * sx; if (Mathf.Abs(den) < 1e-9f) return;
            float qx = c.x - a.x, qz = c.z - a.z, t = (qx * sz - qz * sx) / den, u = (qx * rz - qz * rx) / den;
            if (t > 0f && t < 1f && u >= 0f && u <= 1f) cutT.Add(t);
        }
        static bool InShades(List<Vector3[]> s, Vector3 p) { foreach (var sh in s) if (InConvex(sh, p)) return true; return false; }
    }
}
