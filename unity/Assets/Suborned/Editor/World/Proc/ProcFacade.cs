using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Building massing as grammar: bay rhythm (A-B-A...), storey zoning (base, middle, top), corner treatment
    /// (radiused, chamfered, tower), cornice and string-course profiles, Googie crown lines. One mass in, one MeshKit out.</summary>
    public static class ProcFacade
    {
        const float Cap = 0.9f, Parapet = 0.6f;

        public static void Mass(Nodes nodes, FacadeSpec spec, Transform el, MassDto m, Vector2[] ring, Host h)
        {
            var hostId = FacadeKit.HostOf(m.Id);
            if (!ProcGrammar.Hosts.TryGetValue(hostId, out var g)) { Debug.LogWarning($"World: proc grammar missing for {hostId}"); return; }
            var kit = new MeshKit(); var B = new Pb(kit, spec.Atlas);
            var bounds = new Bounds(new Vector3(ring[0].x, 0, ring[0].y), Vector3.zero); foreach (var p in ring) bounds.Encapsulate(new Vector3(p.x, 0, p.y));
            var c = new Pc
            {
                B = B, G = g, H = h, Dice = new Dice(m.Id, 17), D = ProcKit.Detail(new Vector2(bounds.center.x, bounds.center.z)),
                Wall = h.Wall, Plinth = h.Plinth, Roof = h.Roof, A = h.A, Bc = h.B,
                Base = m.Base, Top = m.Base + m.Height, Podium = m.Id.EndsWith("/podium"), Tower = m.Surface == "tower",
                MassId = m.Id, Skin = ProcSkin.Of(m.Id),
            };
            MeshKit skinKit = null;
            if (c.Skin != null)
            {   // the export's skin decides family, accent, glass and bays (brief 007 round 2): one accent role per building
                skinKit = new MeshKit(); c.S = new Pb(skinKit, spec.Atlas);
                c.A = c.Bc = ProcSkin.Swatch(c.Skin.Accent);
            }
            bool body = c.Base >= 1f;
            c.GroundTop = c.Base < 1f ? c.Base + spec.Ground : c.Base;
            if (h.Fronts.Any(f => f.Z >= c.Base - 0.3f && f.Z < c.Top)) c.GroundTop = Mathf.Max(c.GroundTop, c.Base + spec.GroundUpper);
            c.GroundTop = Mathf.Min(c.GroundTop, c.Top - 1f);
            var floors = new List<(float z0, float h)>();
            float room = c.Top - c.GroundTop - Cap;
            if (room >= 2f) { int n = Mathf.Max(1, Mathf.RoundToInt(room / spec.Storey)); float sh = room / n; for (int i = 0; i < n; i++) floors.Add((c.GroundTop + i * sh, sh)); }

            var rr = body && (g.Corner == "radius" || g.Corner == "chamfer") && c.Top - c.Base >= 7f ? Corners.Round(ring, g) : Corners.Plain(ring);
            Vector2[] sib = null;
            if (c.Podium && h.BodyRing != null)
            {
                var bm = Corners.Round(h.BodyRing, g); sib = (g.Corner == "radius" || g.Corner == "chamfer") ? bm.Pts : h.BodyRing;
            }
            B.Cap(c.Roof, Cls.Matte, rr.Pts, c.Top);
            for (int i = 0; i < rr.Pts.Length; i++)
            {
                var a = rr.Pts[i]; var b = rr.Pts[(i + 1) % rr.Pts.Length];
                var w = new WallFrame(a, b);
                if (w.Len < 0.2f) continue;
                float eL = Ext(rr, i, 0), eR = Ext(rr, (i + 1) % rr.Pts.Length, 1);
                var spans = KitCover.Spans(hostId, w, c.Base);
                if (spans.Count == 0) { Edge(c, spec, w, rr.Facet[i], eL, eR, floors, sib); continue; }
                // kit facade pieces stand on part of this wall (brief 007 E): leave them that span up to their top
                foreach (var (s0, s1, top) in KitCover.Split(spans, w.Len))
                {
                    if (top >= c.Top + Parapet - 0.05f) continue;
                    var sub = w.Sub(s0, s1);
                    Edge(c, spec, sub, rr.Facet[i], s0 <= 0.01f ? eL : 0f, s1 >= w.Len - 0.01f ? eR : 0f, floors, sib, top);
                }
            }
            int vg = kit.VertexCount;
            bool kitFaced = KitCover.HasCovers(hostId); // its street faces are kit pieces with their own crowns and corners
            if ((!c.Podium || h.BodyRing == null) && !kitFaced) Googie(c, spec, rr.Pts, g);
            if (body && g.Corner == "tower" && c.Top - c.Base >= 7f && !kitFaced) CornerTower(c, spec, ring, g);
            ProcKit.Tally("googie+tower", c.D, vg, kit.VertexCount);
            ProcKit.VertsByTier[c.D] += kit.VertexCount; ProcKit.MassesByTier[c.D]++;
            nodes.Part(el, m.Id + "/walls", kit, spec.Atlas.Matte);
            if (skinKit != null) nodes.Part(ProcSkin.Element(c.Skin), m.Id + "/skin", skinKit, spec.Atlas.Matte);
        }

        /// <summary>Mitre extension of a profile at the start (side 0) or end (side 1) of edge i: depth * tan(turn / 2) at convex corners.</summary>
        static float Ext(Corners.Result r, int vertex, int side)
        {
            float t = r.Turn[vertex]; return t > 0.02f ? 0.6f * Mathf.Tan(Mathf.Min(t, 1.5f) * 0.5f) : 0f;
        }

        // ------------------------------------------------------------------ one wall

        /// <summary>One wall. hidden = top of a kit facade piece standing on it (NegativeInfinity: none): storeys wholly under it, the
        /// ground zone under it and (when it reaches the parapet) the crown are left to the piece.</summary>
        static void Edge(Pc c, FacadeSpec spec, in WallFrame w, bool facet, float extL, float extR, List<(float z0, float h)> floors, Vector2[] sibling, float hidden = float.NegativeInfinity)
        {
            var B = c.B; var g = c.G;
            bool bands = facet || w.Len < 2.5f;
            // wall below the floors: ground zone (podium) or nothing (body starts at its base)
            int v0 = c.B.K.VertexCount;
            if (c.Base < 1f && hidden < c.GroundTop) Ground(c, spec, w, bands);
            ProcKit.Tally("ground", c.D, v0, c.B.K.VertexCount);
            if (floors.Count > 0)
            {
                int nb = bands ? 1 : Mathf.Max(1, Mathf.RoundToInt(w.Len / g.Bay)); float bw = w.Len / nb;
                var face = c.Skin != null && !bands ? ProcSkin.Face(c.MassId, w) : null;
                for (int fi = 0; fi < floors.Count; fi++)
                {
                    var (z0, fh) = floors[fi];
                    if (z0 + fh <= hidden + 0.01f) continue;
                    if (c.Skin != null && ProcSkin.BandAt(z0) && z0 > c.Base + 0.05f && z0 + 0.8f < c.Top)
                    { var keep = c.B; c.B = c.S; ProcSkin.Bands(c, c.Skin, w, z0, extL, extR); c.B = keep; }
                    if (face != null)
                    {   // the skin's bays on this face (a storey without a window row is plain stone)
                        int vs = c.S.K.VertexCount;
                        if (ProcSkin.HasRow(face, z0)) { var keep = c.B; c.B = c.S; ProcSkin.Storey(c, c.Skin, face, w, z0, fh); c.B = keep; }
                        else ProcWindows.Wall(c, new Cell(w, 0, w.Len, z0, z0 + fh, 0, 1), c.Wall, Cls.Stone, 0, w.Len, z0, z0 + fh);
                        ProcKit.Tally("skin", c.D, vs, c.S.K.VertexCount);
                        CourseAt(c, g, w, fi, floors.Count, z0, extL, extR);
                        continue;
                    }
                    string zone = c.Podium || c.Base < 1f ? "pod" : c.Tower ? "mid" : floors.Count >= 3 && fi == 0 ? "base" : floors.Count >= 3 && fi == floors.Count - 1 ? "top" : floors.Count == 2 && fi == 1 ? "top" : "mid";
                    if (bands) { FacetBand(c, w, z0, fh); }
                    else
                        for (int k = 0; k < nb; k++)
                        {
                            var cell = new Cell(w, k * bw, (k + 1) * bw, z0, z0 + fh, k, nb);
                            char L = Letter(g, k, nb);
                            string fam = zone switch { "base" => g.BaseFam, "top" => g.TopFam, "pod" => g.PodFam, _ => g.Fam };
                            if (L == 'C') fam = g.Fam2;
                            int vv = B.K.VertexCount; ProcWindows.Draw(c, fam, cell, L); ProcKit.Tally("fam:" + (L == 'B' ? "pier" : fam), c.D, vv, B.K.VertexCount);
                        }
                    CourseAt(c, g, w, fi, floors.Count, z0, extL, extR);
                }
            }
            if (c.Skin != null && !bands && ProcSkin.Face(c.MassId, w, false) is { } rowFace)
                foreach (var z in rowFace.Rows)
                {   // window rows that are no storey floor here (sheds: the porthole row under the eave)
                    if (z < c.Base - 0.05f || z >= c.Top - 1f || z < hidden + 0.01f || floors.Exists(fl => Mathf.Abs(fl.z0 - z) < 0.15f)) continue;
                    var keep = c.B; c.B = c.S; ProcSkin.Overlay(c, c.Skin, rowFace, w, z, c.Top - spec.Cap); c.B = keep;
                }
            int vk = B.K.VertexCount; if (hidden < c.Top + Parapet - 0.05f) Crown(c, w, extL, extR, sibling); ProcKit.Tally("crown", c.D, vk, B.K.VertexCount);
        }

        static string Zone(Pc c, int fi, int n) =>
            c.Podium || c.Base < 1f ? "pod" : c.Tower ? "mid" : n >= 3 && fi == 0 ? "base" : n >= 3 && fi == n - 1 ? "top" : n == 2 && fi == 1 ? "top" : "mid";

        /// <summary>String courses: strong at the zone changes, slim at the others (skinned faces: the strong ones only, the speed bands
        /// mark the rest).</summary>
        static void CourseAt(Pc c, Grammar g, in WallFrame w, int fi, int n, float z0, float extL, float extR)
        {
            var B = c.B; int vc = B.K.VertexCount; string zone = Zone(c, fi, n);
            if (c.D >= 1 && fi > 0 && !(c.Podium && fi == 0))
            {
                bool strong = zone == "top" && n >= 3 || zone == "mid" && fi == 1 && n >= 4;
                if (strong) Course(c, w, 0, w.Len, z0 - 0.21f, 0.5f, extL, extR);
                else if (c.Skin == null && c.D >= 2 && ZoneFam(g, zone) != "ribbon") Course(c, w, 0, w.Len, z0 - 0.11f, 0.26f, 0, 0);
            }
            ProcKit.Tally("courses", c.D, vc, B.K.VertexCount);
        }

        static string ZoneFam(Grammar g, string zone) => zone switch { "base" => g.BaseFam, "top" => g.TopFam, "pod" => g.PodFam, _ => g.Fam };

        static char Letter(Grammar g, int k, int nb)
        {
            var p = g.Rhythm; int i = g.Mirror ? Mathf.Min(k, nb - 1 - k) : k;
            return p[i % p.Length];
        }

        /// <summary>Continuous glazed band for a short edge or a corner facet: spandrel, glass, slim nosing; the way a radiused corner wraps.</summary>
        static void FacetBand(Pc c, in WallFrame w, float z0, float fh)
        {
            var B = c.B; float s1 = w.Len;
            B.R(c.Wall, Cls.Stone, w, 0, s1, z0, z0 + 0.9f, 0f, 1f, 0.9f);
            B.R(c.A, Cls.Enamel, w, 0, s1, z0 + 0.2f, z0 + 0.88f, 0.04f);
            B.R(c.Glass(), Cls.Glass, w, 0, s1, z0 + 0.9f, z0 + fh - 0.5f, c.D >= 1 ? -0.1f : 0.02f, 0.8f, 0.95f);
            B.R(c.Wall, Cls.Stone, w, 0, s1, z0 + fh - 0.5f, z0 + fh, 0f, 0.82f, 0.92f);
            if (c.D >= 1) B.Box(c.Bc, Cls.Enamel, w, 0, s1, z0 - 0.08f, z0 + 0.12f, 0f, 0.2f);
        }

        // ------------------------------------------------------------------ ground zone (podium)

        static void Ground(Pc c, FacadeSpec spec, in WallFrame w, bool bands)
        {
            var B = c.B; var g = c.G; float z0 = c.Base, z1 = c.GroundTop;
            // stone base wall (covered bays stay behind their shopfronts), contact shadow at the foot
            B.R(c.Wall, Cls.Stone, w, 0, w.Len, z0, z0 + 1.2f, 0f, 0.78f, 1f);
            B.R(c.Wall, Cls.Stone, w, 0, w.Len, z0 + 1.2f, z1, 0f, 1f, 0.88f);
            var sk = c.Skin;
            if (sk != null)
            {   // grammar §2a.1: the ground storey in the family's plinth faience, plinth and ground-top band (every tier: they carry the share)
                if (c.D == 0) { B.R(c.Plinth, Cls.Enamel, w, 0, w.Len, z0, z0 + sk.FaiencePlinth, 0.03f); B.R(c.Plinth, Cls.Enamel, w, 0, w.Len, z1 - sk.FaienceBand, z1, 0.03f); }
                else
                {
                    B.Box(c.Plinth, Cls.Enamel, w, 0, w.Len, z0, z0 + sk.FaiencePlinth, 0f, 0.14f, c.D >= 2 ? 0.04f : 0f);
                    B.Box(c.Plinth, Cls.Enamel, w, 0, w.Len, z1 - sk.FaienceBand, z1, 0f, 0.16f, c.D >= 2 ? 0.03f : 0f);
                    B.Box(ProcSkin.Swatch(sk.Trim), Cls.Brass, w, 0, w.Len, z1 - sk.FaienceBand - 0.06f, z1 - sk.FaienceBand, 0f, 0.12f);
                }
            }
            else if (c.D >= 1) B.Box(c.Plinth, Cls.Stone, w, 0, w.Len, z0, z0 + spec.Plinth, 0f, 0.14f, c.D >= 2 ? 0.05f : 0f);
            if (!bands && w.Len >= 2.5f)
            {
                int nb = Mathf.Max(1, Mathf.RoundToInt(w.Len / g.Bay)); float bw = w.Len / nb; var cov = Covered(c.H, w, c, nb);
                for (int k = 0; k < nb; k++)
                {
                    if (cov[k]) continue;
                    var cell = new Cell(w, k * bw, (k + 1) * bw, z0, z1, k, nb);
                    float m = (cell.S0 + cell.S1) * 0.5f;
                    if (c.G.Use == "industrial")
                    {
                        B.R("service", Cls.Matte, w, cell.S0 + 0.8f, cell.S1 - 0.8f, z0 + 0.2f, z0 + 4.2f, 0.06f);
                        continue;
                    }
                    ProcWindows.Opening(c, cell, m - 0.9f, m + 0.9f, z0 + 2.6f, z0 + 4.0f);
                    if (c.D >= 1) B.Box("white", Cls.Stone, w, m - 1.05f, m + 1.05f, z0 + 4.0f, z0 + 4.14f, 0f, 0.1f);
                }
            }
            if (sk == null && c.D >= 1 && c.GroundTop > c.Base + 1f) Course(c, w, 0, w.Len, z1 - 0.46f, 0.55f, 0, 0, true);
        }

        static bool[] Covered(Host h, in WallFrame w, Pc c, int nb)
        {
            var cov = new bool[nb]; float bw = w.Len / nb;
            foreach (var f in h.Fronts)
            {
                if (f.Z < c.Base - 0.3f || f.Z >= c.Top || Mathf.Abs(w.D(f.A)) > 0.4f || Mathf.Abs(w.D(f.B)) > 0.4f) continue;
                float s0 = Mathf.Min(w.S(f.A), w.S(f.B)), s1 = Mathf.Max(w.S(f.A), w.S(f.B));
                for (int k = 0; k < nb; k++) { float mid = (k + 0.5f) * bw; if (mid > s0 - 0.01f && mid < s1 + 0.01f) cov[k] = true; }
            }
            return cov;
        }

        // ------------------------------------------------------------------ profiles

        /// <summary>A string course (stone profile plus its enamel / chrome lines) with its underside at z; size scales the profile (0.5 = 0.5 m tall).</summary>
        static void Course(Pc c, in WallFrame w, float s0, float s1, float z, float size, float extL, float extR, bool shop = false)
        {
            var B = c.B; float k = size / 0.42f; string kind = c.G.Cornice;
            if (c.D < 2) { B.Box("white", Cls.Stone, w, s0 - extL, s1 + extR, z, z + size, 0f, 0.24f * k, 0f, null, false, 0.9f); return; }
            var pr = Profile(kind, k);
            var pts = pr.Select(p => new Vector2(p.x, z + p.y)).ToList();
            B.Extrude("white", Cls.Stone, w, s0 - extL, s1 + extR, pts, false, false, 0.9f);
            switch (kind)
            {
                case "streamline":
                    for (int i = 0; i < 3; i++) B.Box("chrome", Cls.Chrome, w, s0 - extL, s1 + extR, z + (0.07f + i * 0.075f) * k, z + (0.095f + i * 0.075f) * k, 0.2f * k, 0.225f * k);
                    break;
                case "stepped": B.Box(c.A, Cls.Enamel, w, s0, s1, z + 0.14f * k, z + 0.2f * k, 0.18f * k, 0.2f * k); break;
                case "coved": B.Box(c.Bc, Cls.Enamel, w, s0, s1, z + 0.3f * k, z + 0.34f * k, 0.33f * k, 0.345f * k); break;
                default: B.Box(c.Bc, Cls.Enamel, w, s0, s1, z + 0.17f * k, z + 0.23f * k, 0.22f * k, 0.235f * k); break;
            }
        }

        static List<Vector2> Profile(string kind, float k)
        {
            Vector2[] p = kind switch
            {
                "streamline" => new[] { V(0, 0), V(.2f, 0), V(.2f, .31f), V(.14f, .34f), V(0, .34f) },
                "stepped" => new[] { V(0, 0), V(.08f, 0), V(.08f, .1f), V(.16f, .1f), V(.16f, .22f), V(.24f, .22f), V(.24f, .34f), V(0, .34f) },
                "coved" => new[] { V(0, 0), V(.06f, 0), V(.08f, .06f), V(.14f, .13f), V(.22f, .19f), V(.3f, .22f), V(.34f, .3f), V(.34f, .38f), V(0, .38f) },
                _ => new[] { V(0, 0), V(.1f, 0), V(.1f, .06f), V(.18f, .09f), V(.2f, .2f), V(.26f, .26f), V(.26f, .38f), V(.1f, .42f), V(0, .42f) },
            };
            return p.Select(q => q * k).ToList();
        }
        static Vector2 V(float a, float b) => new(a, b);

        // ------------------------------------------------------------------ crown

        static void Crown(Pc c, in WallFrame w, float extL, float extR, Vector2[] sibling)
        {
            var B = c.B;
            foreach (var (a, b) in OpenSpans(w, sibling))
            {
                float eL = a <= 0.06f ? extL : 0f, eR = b >= w.Len - 0.06f ? extR : 0f;
                float top = c.Top;
                if (c.D >= 1) Course(c, w, a, b, top - 0.74f, 0.74f, eL, eR);
                else B.Box(c.Bc, Cls.Enamel, w, a, b, top - 0.4f, top, 0f, 0.35f);
                // parapet: stone face, coping, roof side
                B.R(c.Wall, Cls.Stone, w, a, b, top, top + Parapet, 0f, 0.9f, 1f);
                const float t = 0.3f;
                B.Quad(c.Roof, Cls.Matte, w.P(a, top + Parapet, 0), w.P(a, top + Parapet, -t), w.P(b, top + Parapet, -t), w.P(b, top + Parapet, 0), Vector3.up);
                B.Quad(c.Roof, Cls.Matte, w.P(a, top, -t), w.P(a, top + Parapet, -t), w.P(b, top + Parapet, -t), w.P(b, top, -t), -w.Out, 0.8f, 0.9f, 0.9f, 0.8f);
                if (c.Skin != null && c.D >= 1) B.Box(ProcSkin.Swatch(c.Skin.Trim), Cls.Brass, w, a - eL, b + eR, top + Parapet - 0.02f, top + Parapet + 0.1f, -0.06f, 0.1f); // §2a.5 gold coping bead
                else if (c.D >= 2) B.Box("white", Cls.Stone, w, a - eL, b + eR, top + Parapet - 0.02f, top + Parapet + 0.1f, -0.06f, 0.1f, 0.02f);
            }
        }

        static List<(float, float)> OpenSpans(in WallFrame w, Vector2[] sib)
        {
            var skip = new List<(float, float)>();
            if (sib != null)
                for (int i = 0; i < sib.Length; i++)
                {
                    var p = sib[i]; var q = sib[(i + 1) % sib.Length];
                    if (Mathf.Abs(w.D(p)) > 0.05f || Mathf.Abs(w.D(q)) > 0.05f) continue;
                    float s0 = w.S(p), s1 = w.S(q); skip.Add((Mathf.Min(s0, s1) - 0.05f, Mathf.Max(s0, s1) + 0.05f));
                }
            skip.Sort();
            var res = new List<(float, float)>(); float at = 0;
            foreach (var (a, b) in skip) { if (a > at + 0.05f) res.Add((at, Mathf.Min(a, w.Len))); at = Mathf.Max(at, b); }
            if (at < w.Len - 0.05f) res.Add((at, w.Len));
            return res;
        }

        // ------------------------------------------------------------------ Googie roof lines

        static void Googie(Pc c, FacadeSpec spec, Vector2[] ring, Grammar g)
        {
            if (g.Crown == "cornice" || c.Top - c.Base < 6f) return;
            var B = c.B; if (c.D == 0) return;
            // street-facing edge: outward normal equal to a frontage normal; the longest, else the longest edge
            int best = -1; float bl = 0; bool street = false;
            for (int i = 0; i < ring.Length; i++)
            {
                var w = new WallFrame(ring[i], ring[(i + 1) % ring.Length]);
                bool st = c.H.Fronts.Any(f => Vector2.Dot(f.Normal, w.N) > 0.95f);
                if (w.Len < 5f) continue;
                if (st && !street || (st == street && w.Len > bl)) { best = i; bl = w.Len; street = st; }
            }
            if (best < 0) return;
            var wf = new WallFrame(ring[best], ring[(best + 1) % ring.Length]);
            var d = new Dice(c.H.Id, 31); float L, H; var shape = new List<Vector2>();
            float top = c.Top + Parapet;
            switch (g.Crown)
            {
                case "boomerang": L = Mathf.Min(wf.Len * 0.7f, 18f); H = 3.8f; foreach (var p in new[] { V(0, 0), V(.05f, .4f), V(.2f, 1f), V(.34f, .52f), V(.62f, .22f), V(1, .1f), V(1, 0) }) shape.Add(p); break;
                case "butterfly": L = Mathf.Min(wf.Len * 0.8f, 22f); H = 2.8f; foreach (var p in new[] { V(0, 0), V(0, 1), V(.5f, .3f), V(1, 1), V(1, 0) }) shape.Add(p); break;
                case "folded":
                {
                    L = Mathf.Min(wf.Len * 0.9f, 24f); H = 1.9f; int n = Mathf.Max(3, Mathf.RoundToInt(L / 2.4f));
                    shape.Add(V(0, 0)); shape.Add(V(0, .45f));
                    for (int i = 0; i < n; i++) { shape.Add(V((i + .5f) / n, 1f)); shape.Add(V((i + 1f) / n, .45f)); }
                    shape.Add(V(1, 0)); break;
                }
                case "spire": L = 5.2f; H = 6.2f; foreach (var p in new[] { V(.1f, 0), V(.38f, .5f), V(.5f, 1f), V(.62f, .5f), V(.9f, 0) }) shape.Add(p); break;
                default: L = Mathf.Min(wf.Len * 0.5f, 10f); H = 3.2f; foreach (var p in new[] { V(0, 0), V(0, .3f), V(.2f, .3f), V(.2f, .62f), V(.4f, .62f), V(.4f, 1), V(.6f, 1), V(.6f, .62f), V(.8f, .62f), V(.8f, .3f), V(1, .3f), V(1, 0) }) shape.Add(p); break;
            }
            float room = wf.Len - L; if (room < 0) { L = wf.Len; room = 0; }
            float s0 = g.Crown == "spire" ? (wf.Len - L) * 0.5f : room * (d.Int(3) == 0 ? 0.5f : d.Int(2) == 0 ? 0.08f : 0.92f);
            var pts = shape.Select(p => new Vector2(s0 + p.x * L, top + p.y * H)).ToList();
            System.Func<Vector2, Vector3> map = q => wf.P(q.x, q.y, 0.15f);
            B.Plate(c.A, Cls.Enamel, pts, map, wf.Out, 0.3f);
            var back = pts.Select(p => new Vector2(p.x, top + (p.y - top) * 0.78f)).ToList();
            System.Func<Vector2, Vector3> map2 = q => wf.P(q.x, q.y, -1.0f);
            B.Plate(c.Bc, Cls.Enamel, back, map2, wf.Out, 0.26f);
            B.Tube("chrome", Cls.Chrome, pts, map, wf.Out, 0.17f, 0.04f, 0.1f);
        }

        // ------------------------------------------------------------------ corner tower

        static void CornerTower(Pc c, FacadeSpec spec, Vector2[] ring, Grammar g)
        {
            var B = c.B; if (c.D == 0) return;
            var cand = new List<int>();
            for (int i = 0; i < ring.Length; i++)
            {
                var p = ring[(i + ring.Length - 1) % ring.Length]; var q = ring[i]; var n = ring[(i + 1) % ring.Length];
                var u1 = (q - p).normalized; var u2 = (n - q).normalized;
                if (u1.x * u2.y - u1.y * u2.x > 0.5f && (q - p).magnitude > 8f && (n - q).magnitude > 8f) cand.Add(i);
            }
            if (cand.Count == 0) return;
            int vi = cand[g.CornerIx % cand.Count];
            var prev = ring[(vi + ring.Length - 1) % ring.Length]; var cur = ring[vi]; var next = ring[(vi + 1) % ring.Length];
            var w1 = new WallFrame(prev, cur); var w2 = new WallFrame(cur, next);
            float z0 = c.Base, z1 = c.Top + 5.2f, pr = 0.55f, span = 3.4f;
            B.Box(c.Wall, Cls.Stone, w1, w1.Len - span, w1.Len + pr, z0, z1, 0f, pr, 0.12f, "white");
            B.Box(c.Wall, Cls.Stone, w2, -pr, span, z0, z1, 0f, pr, 0.12f, "white");
            foreach (var (w, s0, s1) in new[] { (w1, w1.Len - span + 0.9f, w1.Len - 0.4f), (w2, 0.4f, span - 0.9f) })
            {
                B.R("glass_t", Cls.Glass, w, s0, s1, z0 + 5f, z1 - 1.2f, pr + 0.02f, 0.8f, 1f);
                int nf = Mathf.FloorToInt((z1 - 1.2f - z0 - 5f) / 3.6f);
                for (int i = 0; i <= nf; i++) B.Box("chrome", Cls.Chrome, w, s0 - 0.05f, s1 + 0.05f, z0 + 5f + i * 3.6f - 0.05f, z0 + 5f + i * 3.6f + 0.05f, pr, pr + 0.08f);
                B.Box(c.A, Cls.Enamel, w, s0 - 0.12f, s0, z0 + 5f, z1 - 1.2f, pr, pr + 0.1f);
                B.Box(c.A, Cls.Enamel, w, s1, s1 + 0.12f, z0 + 5f, z1 - 1.2f, pr, pr + 0.1f);
            }
            // cap: a chrome mast at the outer corner
            B.Box("chrome", Cls.Chrome, w1, w1.Len - 0.04f, w1.Len + 0.04f, z1, z1 + 4.5f, -0.04f, 0.04f);
        }
    }
}
