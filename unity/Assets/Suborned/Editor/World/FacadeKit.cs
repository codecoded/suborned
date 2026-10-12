using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Per-building colours and style, decided once per host (a building: its podium and body share them), seeded by id.</summary>
    public sealed class Host
    {
        public string Id, Area, Style, Wall, Plinth, Roof, A, B;
        public Vector2[] BodyRing;
        public readonly List<FrontageDto> Fronts = new();
    }

    /// <summary>Host colours for every building, and the facades: building masses through ProcFacade (Vela's grammar); civic
    /// limestone, megastructure parts and gate towers through the style kit here (walls by facade bay, storeys from the mass
    /// height, punched windows or bands by style, enamel spandrels, fins, cornice and parapet). Everything is painted from the
    /// swatch atlas, so each layer batches into a few renderers per tile whatever the number of colours.</summary>
    public sealed class FacadeKit
    {
        readonly Nodes nodes; readonly FacadeSpec spec; readonly QuadKit sf; readonly WorldDto world;
        readonly Dictionary<string, Host> hosts = new();
        readonly Dictionary<string, string> bodySurface = new();

        public FacadeSpec Spec => spec;
        public QuadKit Quads => sf;

        public FacadeKit(Nodes nodes, FacadeSpec spec, WorldDto world)
        {
            this.nodes = nodes; this.spec = spec; this.world = world; sf = new QuadKit(spec.Atlas);
            foreach (var f in world.Frontages) Get(f.Host, f.Area).Fronts.Add(f);
            foreach (var m in world.Buildings)
            {
                var id = HostOf(m.Id);
                if (m.Id.EndsWith("/podium")) { continue; }
                bodySurface[id] = m.Surface;
                if (m.Id.EndsWith("/body") || m.Id.EndsWith("/lower")) Get(id, null).BodyRing = Poly.Clean(m.Ring);
            }
        }

        public IEnumerable<string> AllHosts() => hosts.Keys.Where(k => bodySurface.ContainsKey(k)).Select(k => { Resolve(k); return k; }).ToList();

        public static string HostOf(string massId) { int i = massId.LastIndexOf('/'); return i > 0 && massId.Substring(i) is "/podium" or "/body" or "/upper" or "/lower" ? massId.Substring(0, i) : massId; }

        /// <summary>Colours and style of a host; decided on first use from its id and its area (the most common frontage area, else the area named in its id).</summary>
        public Host Get(string id, string areaHint)
        {
            if (hosts.TryGetValue(id, out var h)) { if (h.Area == null && areaHint != null) { Fill(h, areaHint); } return h; }
            h = new Host { Id = id }; hosts[id] = h;
            if (areaHint != null) Fill(h, areaHint);
            return h;
        }

        void Fill(Host h, string area)
        {
            h.Area = spec.AreaFamilies.ContainsKey(area) ? area : "harbour";
            var d = new Dice(h.Id);
            var (names, w) = spec.AreaFamilies[h.Area]; int total = w.Sum(), r = d.Int(total), k = 0;
            while (r >= w[k]) { r -= w[k]; k++; }
            var fam = spec.Families[names[k]];
            h.Wall = fam[0]; h.Plinth = fam[1]; h.Roof = d.Int(3) == 0 ? spec.Roofs[d.Int(spec.Roofs.Length)] : fam[2];
            var en = spec.AreaEnamel[h.Area]; int ia = d.Int(en.Length);
            h.A = en[ia]; h.B = en[(ia + 1 + d.Int(en.Length - 1)) % en.Length];
        }

        public Host Resolve(string id)
        {
            var h = Get(id, null);
            if (h.Area == null)
            {
                string area = spec.AreaEnamel.Keys.FirstOrDefault(a => id.Contains(a)) ?? (id.StartsWith("civic") ? "records" : "harbour");
                Fill(h, area);
            }
            if (h.Style == null) h.Style = StyleOf(h, bodySurface.TryGetValue(h.Id, out var s) ? s : "limestone");
            return h;
        }

        string StyleOf(Host h, string surface)
        {
            var d = new Dice(h.Id, 7);
            if (h.Id.StartsWith("civic") || h.Area == "records") return "grand";
            return surface switch
            {
                "tower" => "curtain",
                "residential" => "punched",
                "commercial" => "band",
                "industrial" or "port" => "shed",
                _ => d.Int(2) == 0 ? "band" : "punched",
            };
        }

        // ---------------------------------------------------------------- buildings

        /// <summary>Every building mass by the procedural grammar (ProcFacade), except tower masses a generated piece stands in for.</summary>
        public void Buildings(Transform layer, List<MassDto> masses, StandIns standIns)
        {
            foreach (var m in masses) { if (standIns.Covers(m)) standIns.Mass(layer, m); else Building(layer, m, false); }
        }

        public void Civic(Transform layer, List<MassDto> masses)
        {
            foreach (var m in masses)
            {
                if (m.Surface == "limestone") Building(layer, m, true);
                else nodes.Solid(layer, m.Id, new MeshKit().Prism(m.Ring, m.Base, m.Base + m.Height), m.Surface);
            }
        }

        sealed class Ctx
        {
            public Host H; public string Style, Wall, Plinth, Roof, A, B, Fin; public string[] Glass;
            public float Base, Top, GroundTop; public bool Podium; public List<(float z0, float h)> Floors = new(); public Dice Dice;
            public Vector2[] Sibling;
        }

        void Building(Transform layer, MassDto m, bool civic)
        {
            var h = Resolve(civic ? m.Id : HostOf(m.Id));
            if (!civic) { ProcFacade.Mass(nodes, spec, Nodes.Element(layer, m.Id), m, Poly.Clean(m.Ring), h); return; }
            Facade(Nodes.Element(layer, m.Id), m, Poly.Clean(m.Ring), h, h.Style, null, null);
        }

        /// <summary>One mass's facade under element `el`. style and wall (a swatch) override the host's when given. peers: the
        /// masses of one structure (megastructure parts); where a peer's collinear, same-facing wall reaches at least as low
        /// and as high, this mass draws nothing there (the peer's facade covers it; two would z-fight).</summary>
        public void Facade(Transform el, MassDto m, Vector2[] ring, Host h, string style, string wall, IReadOnlyList<Peer> peers)
        {
            var c = new Ctx { H = h, Style = style, Base = m.Base, Top = m.Base + m.Height, Podium = m.Id.EndsWith("/podium"), Dice = new Dice(m.Id, 3), Glass = spec.Glass };
            bool tower = m.Surface == "tower";
            c.Wall = wall ?? (tower ? h.A : h.Wall); c.Plinth = h.Plinth; c.Roof = h.Roof; c.A = h.A; c.B = h.B;
            if (tower) { c.Glass = new[] { "glass_t" }; c.Fin = "chrome"; }
            c.Fin ??= c.Style switch { "band" => "white", "grand" => h.A, "shed" => h.Plinth, _ => "white" };
            c.Sibling = c.Podium ? h.BodyRing : null;
            Ground(c); Storeys(c);
            var kitM = new MeshKit(); var kitG = new MeshKit();
            sf.Cap(kitM, c.Roof, ring, c.Top);
            for (int i = 0; i < ring.Length; i++)
            {
                var w = new WallFrame(ring[i], ring[(i + 1) % ring.Length]);
                if (w.Len < 0.2f) continue;
                if (peers == null) { Edge(kitM, kitG, w, c); continue; }
                foreach (var (a, b) in Complement(Hidden(w, m, peers), w.Len))
                    if (b - a >= 0.2f) Edge(kitM, kitG, w.Sub(a, b), c);
            }
            nodes.Part(el, m.Id + "/walls", kitM, spec.Atlas.Matte);
            nodes.Part(el, m.Id + "/glass", kitG, spec.Atlas.Glossy);
        }

        /// <summary>A mass of a multi-part structure: id, cleaned ring, base and top.</summary>
        public readonly struct Peer
        {
            public readonly string Id; public readonly Vector2[] Ring; public readonly float Lo, Hi;
            public Peer(string id, Vector2[] ring, float lo, float hi) { Id = id; Ring = ring; Lo = lo; Hi = hi; }
        }

        /// <summary>Spans of wall w hidden by peers (see Facade); equal masses: the first id draws.</summary>
        static List<(float, float)> Hidden(in WallFrame w, MassDto m, IReadOnlyList<Peer> peers)
        {
            float lo = m.Base, hi = m.Base + m.Height; var skip = new List<(float, float)>();
            foreach (var p in peers)
            {
                if (p.Id == m.Id || p.Lo > lo + 0.01f || p.Hi < hi - 0.01f) continue;
                if (Mathf.Abs(p.Lo - lo) <= 0.01f && Mathf.Abs(p.Hi - hi) <= 0.01f && string.CompareOrdinal(p.Id, m.Id) > 0) continue;
                for (int i = 0; i < p.Ring.Length; i++)
                {
                    Vector2 q0 = p.Ring[i], q1 = p.Ring[(i + 1) % p.Ring.Length];
                    if (Mathf.Abs(w.D(q0)) > 0.05f || Mathf.Abs(w.D(q1)) > 0.05f || Vector2.Dot(q1 - q0, w.U) <= 0) continue;
                    skip.Add((w.S(q0) - 0.05f, w.S(q1) + 0.05f));
                }
            }
            return skip;
        }

        /// <summary>Ground zone top and upper storeys (from the mass's own height: storey 3.6 m, a cap zone for cornice and parapet).</summary>
        void Ground(Ctx c)
        {
            c.GroundTop = c.Base < 1f ? c.Base + spec.Ground : c.Base;
            if (c.H.Fronts.Any(f => f.Z >= c.Base - 0.3f && f.Z < c.Top)) c.GroundTop = Mathf.Max(c.GroundTop, c.Base + spec.GroundUpper);
            c.GroundTop = Mathf.Min(c.GroundTop, c.Top - 1f);
        }

        void Storeys(Ctx c)
        {
            float room = c.Top - c.GroundTop - spec.Cap;
            if (room < 2f) return;
            int n = Mathf.Max(1, Mathf.RoundToInt(room / spec.Storey)); float sh = room / n;
            for (int i = 0; i < n; i++) c.Floors.Add((c.GroundTop + i * sh, sh));
        }

        bool[] Covered(Host h, in WallFrame w, Ctx c)
        {
            int nb = Mathf.Max(1, Mathf.RoundToInt(w.Len / spec.Bay)); var cov = new bool[nb]; float bw = w.Len / nb;
            foreach (var f in h.Fronts)
            {
                if (f.Z < c.Base - 0.3f || f.Z >= c.Top || Mathf.Abs(w.D(f.A)) > 0.4f || Mathf.Abs(w.D(f.B)) > 0.4f) continue;
                float s0 = Mathf.Min(w.S(f.A), w.S(f.B)), s1 = Mathf.Max(w.S(f.A), w.S(f.B));
                for (int k = 0; k < nb; k++) { float mid = (k + 0.5f) * bw; if (mid > s0 - 0.01f && mid < s1 + 0.01f) cov[k] = true; }
            }
            return cov;
        }

        void Edge(MeshKit M, MeshKit G, in WallFrame w, Ctx c)
        {
            sf.Rect(M, c.Wall, w, 0, w.Len, c.Base, c.Top, 0);
            if (c.Base < 1f) sf.Box(M, c.Plinth, w, 0, w.Len, c.Base, c.Base + spec.Plinth, 0, 0.12f);
            if (c.GroundTop > c.Base + 1f && c.Base < 1f) sf.Box(M, c.Plinth, w, 0, w.Len, c.GroundTop - 0.4f, c.GroundTop, 0, 0.3f);
            if (w.Len >= 2.5f || c.Style == "pylon") Bays(M, G, w, c);
            Crown(M, w, c);
        }

        void Bays(MeshKit M, MeshKit G, in WallFrame w, Ctx c)
        {
            int nb = Mathf.Max(1, Mathf.RoundToInt(w.Len / spec.Bay)); float bw = w.Len / nb;
            var cov = Covered(c.H, w, c);
            bool groundZone = c.GroundTop > c.Base + 1f;
            // blank ground-floor bays (no shopfront): small high windows or a service door
            if (groundZone)
                for (int k = 0; k < nb; k++)
                {
                    if (cov[k]) continue;
                    float s0 = k * bw;
                    if (c.Style == "shed") sf.Rect(M, "service", w, s0 + 0.8f, s0 + bw - 0.8f, c.Base + 0.2f, c.Base + 4.2f, 0.06f);
                    else Window(M, G, w, c, s0 + bw * 0.5f - 0.9f, s0 + bw * 0.5f + 0.9f, c.Base + 2.6f, c.Base + 4.0f);
                }
            foreach (var (z0, fh) in c.Floors)
                for (int k = 0; k < nb; k++) Upper(M, G, w, c, k * bw, bw, z0, fh);
            // fins at bay boundaries (curtain: two per bay)
            if (c.Floors.Count > 0 && c.Style is "band" or "curtain" or "grand" or "shed")
            {
                float fz0 = c.GroundTop, fz1 = c.Top - 0.35f;
                float fw = c.Style == "grand" ? 0.9f : c.Style == "curtain" ? 0.22f : 0.3f, fd = c.Style == "grand" ? 0.5f : c.Style == "curtain" ? 0.45f : 0.3f;
                int per = c.Style == "curtain" ? 2 : 1;
                for (int k = 0; k <= nb * per; k++)
                {
                    float s = k * bw / per, a = Mathf.Max(0, s - fw * 0.5f), b = Mathf.Min(w.Len, s + fw * 0.5f);
                    if (b - a > 0.05f) sf.Box(M, c.Fin, w, a, b, fz0, fz1, 0, fd);
                }
            }
        }

        void Window(MeshKit M, MeshKit G, in WallFrame w, Ctx c, float s0, float s1, float z0, float z1)
        {
            sf.Rect(M, "white", w, s0 - 0.1f, s1 + 0.1f, z0 - 0.1f, z1 + 0.1f, 0.04f);
            sf.Rect(G, c.Glass[c.Dice.Int(c.Glass.Length)], w, s0, s1, z0, z1, 0.08f);
        }

        void Upper(MeshKit M, MeshKit G, in WallFrame w, Ctx c, float s0, float bw, float z0, float fh)
        {
            switch (c.Style)
            {
                case "punched":
                {
                    float wz0 = z0 + 0.8f, wz1 = Mathf.Min(z0 + fh - 0.5f, wz0 + 1.9f);
                    float ww = Mathf.Min(1.6f, bw * 0.3f);
                    Window(M, G, w, c, s0 + bw * 0.27f - ww * 0.5f, s0 + bw * 0.27f + ww * 0.5f, wz0, wz1);
                    Window(M, G, w, c, s0 + bw * 0.73f - ww * 0.5f, s0 + bw * 0.73f + ww * 0.5f, wz0, wz1);
                    break;
                }
                case "band":
                    sf.Rect(M, c.A, w, s0, s0 + bw, z0, z0 + 0.95f, 0.06f);
                    sf.Rect(G, c.Glass[c.Dice.Int(c.Glass.Length)], w, s0 + 0.1f, s0 + bw - 0.1f, z0 + 0.95f, z0 + fh - 0.5f, 0.06f);
                    break;
                case "curtain":
                    sf.Rect(G, c.Glass[0], w, s0, s0 + bw, z0 + 0.3f, z0 + fh - 0.1f, 0.06f);
                    break;
                case "grand":
                    Window(M, G, w, c, s0 + 1.1f, s0 + bw - 1.1f, z0 + 0.7f, z0 + fh - 0.45f);
                    sf.Rect(M, c.B, w, s0 + 0.6f, s0 + bw - 0.6f, z0, z0 + 0.4f, 0.1f);
                    break;
                case "pylon": // slender tower: a slot window per storey over an enamel band
                {
                    float ww = Mathf.Min(0.7f, bw * 0.3f), m = s0 + bw * 0.5f;
                    sf.Rect(M, c.A, w, s0, s0 + bw, z0, z0 + 0.3f, 0.05f);
                    Window(M, G, w, c, m - ww * 0.5f, m + ww * 0.5f, z0 + 0.6f, z0 + fh - 0.4f);
                    break;
                }
                case "core": // stair core: a full-storey glazed slot between enamel pilasters
                {
                    float ww = Mathf.Min(1.6f, bw * 0.25f), m = s0 + bw * 0.5f;
                    sf.Rect(G, c.Glass[0], w, m - ww * 0.5f, m + ww * 0.5f, z0 + 0.15f, z0 + fh - 0.15f, 0.06f);
                    sf.Rect(M, c.A, w, m - ww * 0.5f - 0.5f, m - ww * 0.5f, z0, z0 + fh, 0.05f);
                    sf.Rect(M, c.A, w, m + ww * 0.5f, m + ww * 0.5f + 0.5f, z0, z0 + fh, 0.05f);
                    break;
                }
                default: // shed: one clerestory band under the cornice, on the top storey only
                    if (z0 + fh >= c.Top - spec.Cap - 0.01f)
                        sf.Rect(G, c.Glass[0], w, s0 + 0.4f, s0 + bw - 0.4f, z0 + fh - 1.8f, z0 + fh - 0.4f, 0.06f);
                    break;
            }
        }

        /// <summary>Cornice band and parapet on the roof edge; a podium edge that coincides with its body's wall gets neither there.</summary>
        void Crown(MeshKit M, in WallFrame w, Ctx c)
        {
            foreach (var (a, b) in Open(w, c))
            {
                sf.Box(M, c.B, w, a, b, c.Top - 0.4f, c.Top, 0, 0.35f);
                float p = spec.Parapet, t = 0.3f;
                sf.Rect(M, c.Wall, w, a, b, c.Top, c.Top + p, 0.0f);
                sf.Face(M, c.Roof, w.P(a, c.Top + p, 0), w.P(a, c.Top + p, -t), w.P(b, c.Top + p, -t), w.P(b, c.Top + p, 0), Vector3.up);
                sf.Face(M, c.Roof, w.P(a, c.Top, -t), w.P(a, c.Top + p, -t), w.P(b, c.Top + p, -t), w.P(b, c.Top, -t), -w.Out);
            }
        }

        static List<(float, float)> Open(in WallFrame w, Ctx c)
        {
            var skip = new List<(float, float)>();
            if (c.Sibling != null)
                for (int i = 0; i < c.Sibling.Length; i++)
                {
                    var p = c.Sibling[i]; var q = c.Sibling[(i + 1) % c.Sibling.Length];
                    if (Mathf.Abs(w.D(p)) > 0.05f || Mathf.Abs(w.D(q)) > 0.05f) continue;
                    float s0 = w.S(p), s1 = w.S(q); skip.Add((Mathf.Min(s0, s1) - 0.05f, Mathf.Max(s0, s1) + 0.05f));
                }
            return Complement(skip, w.Len);
        }

        /// <summary>The parts of [0, len] outside the skipped spans.</summary>
        static List<(float, float)> Complement(List<(float, float)> skip, float len)
        {
            skip.Sort();
            var res = new List<(float, float)>(); float at = 0;
            foreach (var (a, b) in skip) { if (a > at + 0.05f) res.Add((at, Mathf.Min(a, len))); at = Mathf.Max(at, b); }
            if (at < len - 0.05f) res.Add((at, len));
            return res;
        }
    }
}
