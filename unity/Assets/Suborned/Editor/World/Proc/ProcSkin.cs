using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>The export's procedural facade skins (`skins`, tools/world/gen/skins.ts; grammar §2-3) drawn by ProcFacade: per wall, the
    /// face's bay letters (R ribbon window, P porthole bay, B balcony bay, - plain stone), its window rows and porthole columns; per building,
    /// the accent its speed bands carry (one role), clear glass set in stone with a frame and chrome mullions, porthole size and ring.
    /// A wall finds its face by mass, then by edge geometry (collinear, same facing): the ring ProcFacade walks is the cleaned, corner-rounded
    /// one, so its edge indices are not the export's. Skin geometry goes under `skins/&lt;skin id&gt;` (one element per skin); a skin whose walls
    /// are all kit-covered or whose masses a stand-in covers is marked covered at its building's place. Static, reset per build.</summary>
    public static class ProcSkin
    {
        static readonly Dictionary<string, SkinDto> byMass = new(System.StringComparer.Ordinal);
        static readonly Dictionary<string, List<SkinFaceDto>> faces = new(System.StringComparer.Ordinal);
        static readonly Dictionary<string, SkinDto> byBuilding = new(System.StringComparer.Ordinal);
        static readonly Dictionary<SkinDto, Transform> elements = new();
        public static int Matched, Unmatched, Cells, Panels, Spandrels, BalconyFronts, Fins;

        public static void Init(WorldDto w)
        {
            byMass.Clear(); faces.Clear(); elements.Clear(); byBuilding.Clear(); Matched = Unmatched = Cells = Panels = Spandrels = BalconyFronts = Fins = 0;
            foreach (var s in w.Skins) byBuilding[s.Building] = s;
            foreach (var s in w.Skins)
                foreach (var f in s.Faces)
                {
                    byMass[f.Mass] = s;
                    if (!faces.TryGetValue(f.Mass, out var l)) faces[f.Mass] = l = new List<SkinFaceDto>();
                    l.Add(f);
                }
        }

        /// <summary>Swatches the skins need (accent roles, frame and ring surfaces), registered before the atlas is baked.</summary>
        public static IEnumerable<string> Surfaces(WorldDto w)
        {
            var r = new SortedSet<string>(System.StringComparer.Ordinal);
            foreach (var s in w.Skins)
            {
                r.Add(s.Accent); r.Add(s.FrameSurface); r.Add(s.RingSurface); r.Add(s.Accent2); r.Add(s.Trim); r.Add(s.PanelSurface); r.Add(s.Spandrel?.Surface);
                foreach (var f in s.Fins) { r.Add(f.Surface); r.Add(f.BeadSurface); }
            }
            foreach (var a in w.Fabric.Awnings) foreach (var st in a.Stripes) r.Add(st);
            foreach (var b in w.Fabric.Banners) { r.Add(b.Surface); r.Add(b.BorderSurface); }
            foreach (var f in w.Fabric.Flags) { r.Add(f.Surface); if (f.Mast != "chrome") r.Add(f.Mast); } // chrome is the facade atlas swatch
            r.Remove(null);
            return r;
        }

        public static string Swatch(string surface) => MegaKit.Swatch(surface);

        public static void Elements(Transform layer, List<SkinDto> skins)
        {
            foreach (var s in skins) elements[s] = Nodes.Element(layer, s.Id);
        }

        public static SkinDto Of(string massId) => byMass.TryGetValue(massId, out var s) ? s : null;
        /// <summary>The skin of a plot building (frontage hosts), or null.</summary>
        public static SkinDto OfBuilding(string buildingId) => buildingId != null && byBuilding.TryGetValue(buildingId, out var s) ? s : null;
        public static Transform Element(SkinDto s) => elements[s];

        /// <summary>After the buildings: a skin with no geometry (every wall under kit facades, or its masses under a stand-in) is covered
        /// by design; its marker sits on its building's masses.</summary>
        public static void Finish(Transform buildings)
        {
            foreach (var kv in elements)
            {
                if (Combine.HasGeometry(kv.Value)) continue;
                Bounds? b = null;
                foreach (var f in kv.Key.Faces)
                    foreach (var q in new[] { f.A, f.B })
                    {
                        var p = new Vector3(q.x, 0, q.y);
                        if (b is { } h) { h.Encapsulate(p); b = h; } else b = new Bounds(p, Vector3.zero);
                    }
                Combine.Cover(kv.Value, b ?? new Bounds());
            }
        }

        /// <summary>The face of `mass` the wall w lies on (collinear within 0.3 m, facing the same way), or null.</summary>
        public static SkinFaceDto Face(string mass, in WallFrame w, bool count = true)
        {
            if (!faces.TryGetValue(mass, out var l)) return null;
            SkinFaceDto best = null; float bestD = 0.3f;
            foreach (var f in l)
            {
                var d = f.B - f.A; float len = d.magnitude; if (len < 0.1f) continue;
                var u = d / len;
                if (Vector2.Dot(u, w.U) < 0.995f) continue;
                var n = new Vector2(u.y, -u.x);
                float off = Mathf.Abs(Vector2.Dot(w.A - f.A, n));
                float s0 = Vector2.Dot(w.A - f.A, u), s1 = s0 + w.Len;
                if (off > bestD || s1 < -0.3f || s0 > len + 0.3f) continue;
                best = f; bestD = off;
            }
            if (count) { if (best != null) Matched++; else Unmatched++; }
            return best;
        }

        /// <summary>Floor z0 is one of the face's window rows.</summary>
        public static bool HasRow(SkinFaceDto f, float z0)
        {
            foreach (var r in f.Rows) if (Mathf.Abs(r - z0) < 0.15f) return true;
            return false;
        }

        /// <summary>Speed-band floor line (grammar §2): every 2nd storey from the deck line (9.43) up.</summary>
        public static bool BandAt(float z0)
        {
            float k = (z0 - 9.43f) / 3.6f; int i = Mathf.RoundToInt(k);
            return i >= 0 && Mathf.Abs(k - i) * 3.6f < 0.15f && i % 2 == 0;
        }

        /// <summary>One storey of wall w on face f: the face's bays mapped onto w (w may be a part of the face: rounded corners shorten it,
        /// kit pieces split it), each bay clipped to the wall; a clipped bay too narrow for its window is plain stone. Portholes on plain bays.</summary>
        public static void Storey(Pc c, SkinDto s, SkinFaceDto f, in WallFrame w, float z0, float fh)
        {
            int n = f.Bays.Length;
            var fd = f.B - f.A; float flen = fd.magnitude; var fu = fd / flen;
            float off = Vector2.Dot(w.A - f.A, fu);
            if (n == 0) { ProcWindows.Wall(c, new Cell(w, 0, w.Len, z0, z0 + fh, 0, 1), c.Wall, Cls.Stone, 0, w.Len, z0, z0 + fh); return; }
            float bw = flen / n;
            float at = 0f;
            for (int k = 0; k < n; k++)
            {
                float a = Mathf.Max(0f, k * bw - off), b = Mathf.Min(w.Len, (k + 1) * bw - off);
                if (b - a < 0.02f) continue;
                if (a > at + 0.01f) Plain(c, new Cell(w, at, a, z0, z0 + fh, k, n));
                var cell = new Cell(w, a, b, z0, z0 + fh, k, n);
                char L = f.Bays[k];
                if (L != '-' && b - a < bw * 0.7f) L = '-';
                Draw(c, s, cell, L);
                at = b; Cells++;
            }
            if (at < w.Len - 0.01f) Plain(c, new Cell(w, at, w.Len, z0, z0 + fh, n - 1, n));
            // porthole columns (stone end walls, stair cores): one per storey, on plain stone only
            float zc = z0 + (s.Sill + Mathf.Min(s.Head, fh - 0.3f)) * 0.5f, rad = s.PortholeD * 0.5f;
            foreach (var p in f.Portholes)
            {
                float sp = p - off; if (sp < rad + 0.2f || sp > w.Len - rad - 0.2f) continue;
                int k = Mathf.Clamp(Mathf.FloorToInt((p) / bw), 0, n - 1);
                if (f.Bays[k] != '-') continue;
                Porthole(c, s, new Cell(w, sp - rad, sp + rad, z0, z0 + fh, k, n), sp, zc, rad, false);
            }
        }

        /// <summary>A window row that is no storey floor of the mass (sheds: a porthole row under the eave): its letters drawn proud of the
        /// wall the storeys already drew (no wall of their own, so nothing z-fights): portholes, and ribbon glass in its frame. The row's
        /// openings stay under `zMax` (the cornice).</summary>
        public static void Overlay(Pc c, SkinDto s, SkinFaceDto f, in WallFrame w, float z, float zMax)
        {
            int n = f.Bays.Length; if (n == 0) return;
            var fd = f.B - f.A; float flen = fd.magnitude; var fu = fd / flen;
            float off = Vector2.Dot(w.A - f.A, fu), bw = flen / n, rad = s.PortholeD * 0.5f;
            float zc = Mathf.Min(z + (s.Sill + s.Head) * 0.5f, zMax - rad - s.PortholeRing - 0.1f);
            if (zc - rad < z - 0.5f) return;
            for (int k = 0; k < n; k++)
            {
                float a = Mathf.Max(0f, k * bw - off), b = Mathf.Min(w.Len, (k + 1) * bw - off);
                if (b - a < bw * 0.7f) continue;
                var cell = new Cell(w, a, b, z, zMax, k, n);
                switch (f.Bays[k])
                {
                    case 'P':
                        for (int i = 0; i < Mathf.Max(1, s.PerBay); i++) Porthole(c, s, cell, a + (b - a) * (i + 0.5f) / Mathf.Max(1, s.PerBay), zc, rad, true);
                        Cells++; break;
                    case 'R':
                    {
                        float gw = Mathf.Min(s.GlassShare * (b - a), s.MaxPane), m = (a + b) * 0.5f, g0 = z + s.Sill, g1 = Mathf.Min(z + s.Head, zMax - 0.2f);
                        if (g1 - g0 < 0.6f) break;
                        c.B.R(c.Glass(), Cls.Glass, w, m - gw * 0.5f, m + gw * 0.5f, g0, g1, 0.03f, 0.85f, 0.95f);
                        if (c.D >= 1) Frame(c, s, cell, m - gw * 0.5f, m + gw * 0.5f, g0, g1);
                        Cells++; break;
                    }
                }
            }
        }

        static void Plain(Pc c, in Cell k) => ProcWindows.Wall(c, k, c.Wall, Cls.Stone, k.S0, k.S1, k.Z0, k.Z1);

        static void Draw(Pc c, SkinDto s, in Cell k, char letter)
        {
            switch (letter)
            {
                case 'R': Ribbon(c, s, k); break;
                case 'P': PortholeBay(c, s, k); break;
                case 'B': Balcony(c, s, k); break;
                default: Plain(c, k); break;
            }
        }

        /// <summary>Ribbon window: share x bay of clear glass (never wider than maxPane), sill to head, set back `reveal`, a frame of the
        /// pier's frame surface round it, chrome mullions every `mullion` m. Far tier: wall and one glass quad.</summary>
        static void Ribbon(Pc c, SkinDto s, in Cell k)
        {
            float gw = Mathf.Min(s.GlassShare * k.Bw, s.MaxPane, k.Bw - 0.6f); if (gw < 0.5f) { Plain(c, k); return; }
            float m = (k.S0 + k.S1) * 0.5f, h0 = m - gw * 0.5f, h1 = m + gw * 0.5f;
            float g0 = k.Z0 + s.Sill, g1 = Mathf.Min(k.Z0 + s.Head, k.Z1 - 0.3f);
            if (c.D == 0)
            {
                Plain(c, k);
                c.B.R(c.Glass(), Cls.Glass, k.W, h0, h1, g0, g1, 0.03f, 0.85f, 0.95f);
                Spandrel(c, s, k, h0, h1, g0);
                return;
            }
            Spandrel(c, s, k, h0, h1, g0);
            float r = c.D >= 2 ? s.Reveal : s.Reveal * 0.5f;
            ProcWindows.Holes(c, k, new() { (h0, h1) }, g0, g1);
            ProcWindows.Opening(c, k, h0, h1, g0, g1, r, c.Glass());
            Frame(c, s, k, h0, h1, g0, g1);
            int nm = Mathf.Max(1, Mathf.RoundToInt((h1 - h0) / Mathf.Max(0.5f, s.Mullion)));
            for (int i = 1; i < nm; i++)
            {
                float x = Mathf.Lerp(h0, h1, i / (float)nm);
                c.B.R("chrome", Cls.Chrome, k.W, x - 0.035f, x + 0.035f, g0, g1, -r + 0.05f);
            }
        }

        /// <summary>Grammar §2a.3: an accent panel under the ribbon, its width, `h` high ending at the sill (under the frame's sill bar);
        /// a 1.9 m one runs down to the head of the storey below, never into the ground storey.</summary>
        static void Spandrel(Pc c, SkinDto s, in Cell k, float h0, float h1, float g0)
        {
            if (s.Spandrel == null) return;
            float fw = c.D == 0 ? 0f : Mathf.Min(s.Frame, 0.2f);
            float z1 = g0 - fw, z0 = Mathf.Max(g0 - s.Spandrel.H, c.GroundTop + 0.05f);
            if (z1 - z0 < 0.2f) return;
            if (c.D == 0) c.B.R(Swatch(s.Spandrel.Surface), Cls.Enamel, k.W, h0, h1, z0, z1, 0.03f);
            else c.B.Box(Swatch(s.Spandrel.Surface), Cls.Enamel, k.W, h0, h1, z0, z1, 0f, 0.05f);
            Spandrels++;
        }

        /// <summary>A slim surround (pier frame surface) proud of the wall round an opening; width min(frame, 0.2) so a 0.45 m frame
        /// band reads at street distance without eating the stone.</summary>
        static void Frame(Pc c, SkinDto s, in Cell k, float h0, float h1, float g0, float g1)
        {
            float fw = Mathf.Min(s.Frame, 0.2f), pr = 0.07f; var sw = Swatch(s.FrameSurface);
            c.B.Box(sw, Cls.Brass, k.W, h0 - fw, h1 + fw, g1, g1 + fw, 0f, pr);
            c.B.Box(sw, Cls.Brass, k.W, h0 - fw, h1 + fw, g0 - fw, g0, 0f, pr + 0.03f);
            c.B.Box(sw, Cls.Brass, k.W, h0 - fw, h0, g0, g1, 0f, pr);
            c.B.Box(sw, Cls.Brass, k.W, h1, h1 + fw, g0, g1, 0f, pr);
        }

        static void PortholeBay(Pc c, SkinDto s, in Cell k)
        {
            int n = Mathf.Max(1, s.PerBay); float rad = s.PortholeD * 0.5f;
            float zc = k.Z0 + (s.Sill + Mathf.Min(s.Head, k.Fh - 0.3f)) * 0.5f;
            Plain(c, k);
            for (int i = 0; i < n; i++)
            {
                float cs = k.S0 + k.Bw * (i + 0.5f) / n;
                Porthole(c, s, k, cs, zc, rad, true);
            }
        }

        /// <summary>A porthole on the plain wall the caller drew: glass disc proud of it, ring of the ring surface, a chrome cross at the hero tier.</summary>
        static void Porthole(Pc c, SkinDto s, in Cell k, float cs, float zc, float rad, bool _)
        {
            var w = k.W; int N = c.D >= 2 ? 16 : c.D == 1 ? 12 : 8; float ring = c.D == 0 ? 0f : s.PortholeRing;
            // grammar §2a.3a: the accent panel behind it, flush on the wall (1.5 cm proud so it never z-fights), inside the storey
            float pw = s.PanelW * 0.5f, ph = s.PanelH * 0.5f;
            float p0 = Mathf.Max(zc - ph, k.Z0 + 0.05f), p1 = Mathf.Min(zc + ph, k.Z1 - 0.05f);
            if (pw > rad && p1 - p0 > 2f * rad) { c.B.R(Swatch(s.PanelSurface), Cls.Enamel, w, cs - pw, cs + pw, p0, p1, 0.015f); Panels++; }
            var disc = new List<Vector3>(); var outer = new List<Vector3>();
            for (int i = 0; i < N; i++)
            {
                float a = 2 * Mathf.PI * i / N, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                disc.Add(w.P(cs + ca * rad, zc + sa * rad, 0.04f));
                outer.Add(w.P(cs + ca * (rad + ring), zc + sa * (rad + ring), 0.07f));
            }
            if (ring > 0)
                for (int i = 0; i < N; i++) c.B.Quad(Swatch(s.RingSurface), Cls.Brass, outer[i], outer[(i + 1) % N], disc[(i + 1) % N], disc[i], w.Out);
            c.B.FanShared(c.Glass(), Cls.Glass, disc, w.Out, 0.85f);
            if (c.D >= 2)
            {
                c.B.Box("chrome", Cls.Chrome, w, cs - 0.025f, cs + 0.025f, zc - rad, zc + rad, 0.04f, 0.08f);
                c.B.Box("chrome", Cls.Chrome, w, cs - rad, cs + rad, zc - 0.025f, zc + 0.025f, 0.04f, 0.08f);
            }
        }

        /// <summary>Balcony bay: a glazed door (sill 0.15) in a frame and a balcony slab with rounded ends (limestone) under a chrome rail.</summary>
        static void Balcony(Pc c, SkinDto s, in Cell k)
        {
            float m = (k.S0 + k.S1) * 0.5f, dw = Mathf.Min(1.8f, k.Bw * 0.4f), g0 = k.Z0 + 0.15f, g1 = Mathf.Min(k.Z0 + s.Head, k.Z1 - 0.3f);
            if (c.D == 0)
            {
                Plain(c, k);
                c.B.R(c.Glass(), Cls.Glass, k.W, m - dw * 0.5f, m + dw * 0.5f, g0, g1, 0.03f, 0.85f, 0.95f);
                float hs = Mathf.Min(k.Bw * 0.42f, 2.3f);
                c.B.Box("white", Cls.Stone, k.W, m - hs, m + hs, k.Z0 - 0.04f, k.Z0 + 0.16f, 0f, 1.2f);
                if (s.Spandrel != null) { c.B.R(Swatch(s.Spandrel.Surface), Cls.Enamel, k.W, m - hs, m + hs, k.Z0 + 0.16f, k.Z0 + s.Spandrel.BalconyH, 1.2f); BalconyFronts++; }
                return;
            }
            ProcWindows.Holes(c, k, new() { (m - dw * 0.5f, m + dw * 0.5f) }, g0, g1);
            ProcWindows.Opening(c, k, m - dw * 0.5f, m + dw * 0.5f, g0, g1, c.Recess, c.Glass());
            Frame(c, s, k, m - dw * 0.5f, m + dw * 0.5f, g0, g1);
            // slab: a box with half-round ends (radius = depth / 2 ... depth), plan rounded like the concept balconies
            float sl = Mathf.Min(k.Bw * 0.42f, 2.3f), dp = 1.25f, z0 = k.Z0 - 0.04f, z1 = k.Z0 + 0.16f;
            var w = k.W; const int Seg = 6;
            var outline = new List<Vector2>(); // (s along wall, d out)
            outline.Add(new Vector2(m - sl, 0));
            for (int i = 0; i <= Seg; i++) { float t = Mathf.PI * 0.5f * i / Seg; outline.Add(new Vector2(m - sl + dp * (1 - Mathf.Cos(t)) * 0.6f, dp * Mathf.Sin(t))); }
            for (int i = 0; i <= Seg; i++) { float t = Mathf.PI * 0.5f * (Seg - i) / Seg; outline.Add(new Vector2(m + sl - dp * (1 - Mathf.Cos(t)) * 0.6f, dp * Mathf.Sin(t))); }
            outline.Add(new Vector2(m + sl, 0));
            var top = new List<Vector3>(); var bot = new List<Vector3>();
            foreach (var q in outline) { top.Add(w.P(q.x, z1, q.y)); bot.Add(w.P(q.x, z0, q.y)); }
            c.B.Fan("white", Cls.Stone, top, Vector3.up);
            var botR = new List<Vector3>(bot); botR.Reverse();
            c.B.Fan("white", Cls.Stone, botR, Vector3.down, 0.6f);
            for (int i = 0; i + 1 < outline.Count; i++)
            {
                var o = Pb.OutOf(w, outline[i + 1].x - outline[i].x, outline[i + 1].y - outline[i].y);
                c.B.Quad("white", Cls.Stone, bot[i], top[i], top[i + 1], bot[i + 1], o, 0.85f, 1f, 1f, 0.85f);
            }
            // rail: chrome top rail along the outline over translucent panels, or (grammar §2a.3) a solid accent front `balcony.h` high
            bool solid = s.Spandrel != null; float rail = solid ? k.Z0 + s.Spandrel.BalconyH : k.Z0 + 1.0f;
            string panel = solid ? Swatch(s.Spandrel.Surface) : "glass_t"; int pc = solid ? Cls.Enamel : Cls.Glass;
            for (int i = 0; i + 1 < outline.Count; i++)
            {
                var p0 = outline[i]; var p1 = outline[i + 1];
                var o = Pb.OutOf(w, p1.x - p0.x, p1.y - p0.y);
                c.B.Quad(panel, pc, w.P(p0.x, z1, p0.y), w.P(p0.x, rail, p0.y), w.P(p1.x, rail, p1.y), w.P(p1.x, z1, p1.y), o, 0.9f, 1f, 1f, 0.9f);
                if (solid) c.B.Quad(panel, pc, w.P(p0.x, z1, p0.y - 0.03f), w.P(p0.x, rail, p0.y - 0.03f), w.P(p1.x, rail, p1.y - 0.03f), w.P(p1.x, z1, p1.y - 0.03f), -o, 0.8f, 0.9f, 0.9f, 0.8f);
                c.B.Quad("chrome", Cls.Chrome, w.P(p0.x, rail, p0.y), w.P(p0.x, rail + 0.07f, p0.y), w.P(p1.x, rail + 0.07f, p1.y), w.P(p1.x, rail, p1.y), o);
            }
            if (solid) BalconyFronts++;
        }

        /// <summary>Grammar §2a.4: every skin's fins under its element (`<skin id>/fins`): a slab `w` along the wall, `d` proud, z0 to z1, in
        /// the fin surface, its outer face's two vertical edges beaded in gold; the fin's top capped in gold.</summary>
        public static void FinsAll(Nodes nodes, Atlas atlas)
        {
            foreach (var kv in elements)
            {
                var s = kv.Key; if (s.Fins.Count == 0) continue;
                var kit = new MeshKit(); var B = new Pb(kit, atlas);
                foreach (var f in s.Fins)
                {
                    var n = f.Normal.normalized; var u = new Vector2(-n.y, n.x);
                    var w = new WallFrame(f.At - u * (f.W * 0.5f), f.At + u * (f.W * 0.5f));
                    if (Vector2.Dot(w.N, n) < 0) w = new WallFrame(f.At + u * (f.W * 0.5f), f.At - u * (f.W * 0.5f));
                    B.Box(Swatch(f.Surface), Cls.Enamel, w, 0f, w.Len, f.Z0, f.Z1, 0f, f.D, 0.04f);
                    B.Box(Swatch(f.BeadSurface), Cls.Brass, w, 0f, f.BeadW, f.Z0, f.Z1, f.D - 0.02f, f.D + 0.03f);
                    B.Box(Swatch(f.BeadSurface), Cls.Brass, w, w.Len - f.BeadW, w.Len, f.Z0, f.Z1, f.D - 0.02f, f.D + 0.03f);
                    B.Box(Swatch(f.BeadSurface), Cls.Brass, w, -0.02f, w.Len + 0.02f, f.Z1, f.Z1 + 0.12f, 0f, f.D + 0.04f);
                    Fins++;
                }
                nodes.Part(kv.Value, s.Id + "/fins", kit, atlas.Matte);
            }
        }

        /// <summary>Speed bands (grammar §2) on a floor line: an accent strip and a chrome line 0.25 m above it, both proud, in the
        /// spandrel under the storey's sills; the whole wall length (facets too, so the pair wraps round rounded corners).</summary>
        public static void Bands(Pc c, SkinDto s, in WallFrame w, float z0, float extL, float extR)
        {
            float a = -extL, b = w.Len + extR;
            if (c.D == 0)
            {
                c.B.R(Swatch(s.Accent), Cls.Enamel, w, a, b, z0 + 0.1f, z0 + 0.45f, 0.04f);
                return;
            }
            c.B.Box(Swatch(s.Accent), Cls.Enamel, w, a, b, z0 + 0.1f, z0 + 0.45f, 0f, 0.08f);
            c.B.Box("chrome", Cls.Chrome, w, a, b, z0 + 0.62f, z0 + 0.7f, 0f, 0.06f);
        }
    }
}
