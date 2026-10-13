using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Everything a building's facade is decided by: bay rhythm, storey zoning (base / middle / top families), window
    /// families, corner treatment, crown line, cornice profile and colourway. Seeded by the host id and its area; neighbours along a
    /// street edge are made to contrast (ProcGrammar.Assign).</summary>
    public sealed class Grammar
    {
        public string Rhythm, Fam, Fam2, BaseFam, TopFam, PodFam, Corner, Crown, Cornice, WallFam, A, B, Use;
        public bool Mirror; public float Bay, Radius; public int CornerIx;
        public string Colourway => $"{WallFam}/{A}/{B}";
        /// <summary>The coarse signature the metrics count: grammar (rhythm + bay), window family, colourway, corner.</summary>
        public string Coarse => $"{Rhythm}{(Mirror ? "m" : "")}@{Bay:0.0}|{Fam}|{Colourway}|{Corner}";
        public string Full => $"{Coarse}|{Fam2}|{BaseFam}|{TopFam}|{PodFam}|{Crown}|{Cornice}";
    }

    /// <summary>Per-frontage sign: board shape, colourway, icon, mount.</summary>
    public sealed class SignSpec
    {
        public string Shape, Board, Tube, Ink, Icon, Mount, Trade;
        public string Kit;
        public string Key => $"{Shape}|{Board}|{Tube}|{Icon}|{Kit}";
    }

    public static class ProcGrammar
    {
        public static readonly Dictionary<string, Grammar> Hosts = new();
        public static readonly Dictionary<string, SignSpec> Signs = new();
        /// <summary>Host sequences along every street edge (street + side), in walking order; frontage sequences likewise.</summary>
        public static readonly List<List<string>> HostEdges = new();
        public static readonly List<List<FrontageDto>> ShopEdges = new();
        public static readonly Dictionary<string, string> UseOf = new();

        static readonly string[] Rhythms = { "A", "AB", "AAB", "ABA", "AABB", "ABAC", "AAAB", "ABBA", "AC", "AAC" };
        static readonly string[] Corners = { "square", "radius", "chamfer", "tower" };
        static readonly string[] Crowns = { "cornice", "boomerang", "butterfly", "folded", "spire", "stepped" };
        static readonly string[] Cornices = { "classic", "streamline", "stepped", "coved" };
        static readonly float[] Bays = { 4.0f, 4.8f, 6f, 7.5f };

        static readonly Dictionary<string, (string, int)[]> FamByUse = new()
        {
            ["residential"] = new[] { ("punched", 2), ("balcony", 4), ("eyebrow", 3), ("louvre", 2), ("glassblock", 2), ("fins", 1), ("porthole", 1) },
            ["commercial"] = new[] { ("ribbon", 4), ("brise", 3), ("fins", 2), ("porthole", 3), ("sawtooth", 1), ("eyebrow", 1) },
            ["mixed"] = new[] { ("ribbon", 2), ("eyebrow", 2), ("punched", 2), ("sawtooth", 2), ("balcony", 2), ("porthole", 2), ("louvre", 1), ("brise", 1) },
            ["industrial"] = new[] { ("sawtooth", 3), ("louvre", 3), ("glassblock", 2), ("ribbon", 2), ("porthole", 2) },
            ["tower"] = new[] { ("fins", 4), ("ribbon", 3), ("porthole", 1), ("brise", 1) }, // no curtain wall (grammar §3)
            ["other"] = new[] { ("punched", 3), ("fins", 2), ("eyebrow", 2), ("ribbon", 1), ("balcony", 1) },
        };

        static readonly Dictionary<string, Dictionary<string, float>> AreaBias = new()
        {
            ["harbour"] = new() { ["porthole"] = 2, ["sawtooth"] = 1.6f, ["louvre"] = 1.6f },
            ["docks"] = new() { ["porthole"] = 2, ["sawtooth"] = 2f, ["louvre"] = 1.5f },
            ["gardens"] = new() { ["balcony"] = 2, ["brise"] = 2, ["glassblock"] = 1.5f },
            ["crescent"] = new() { ["balcony"] = 2, ["brise"] = 1.6f, ["glassblock"] = 1.5f },
            ["lantern"] = new() { ["eyebrow"] = 2, ["balcony"] = 1.6f, ["punched"] = 1.3f },
            ["lamplight"] = new() { ["eyebrow"] = 2, ["balcony"] = 1.4f, ["louvre"] = 1.3f },
            ["records"] = new() { ["fins"] = 2.5f, ["punched"] = 1.6f, ["glassblock"] = 1.3f },
        };

        public static string UseFor(string surface) => surface switch
        {
            "residential" => "residential", "commercial" => "commercial", "mixed" => "mixed", "tower" => "tower",
            "industrial" or "port" => "industrial", _ => "other",
        };

        static string WeightedFam(Dice d, string use, string area, string avoid1, string avoid2)
        {
            var list = FamByUse[use]; AreaBias.TryGetValue(area ?? "", out var bias);
            float tot = 0; var w = new float[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                w[i] = list[i].Item2 * (bias != null && bias.TryGetValue(list[i].Item1, out var m) ? m : 1f);
                if (list[i].Item1 == avoid1 || list[i].Item1 == avoid2) w[i] *= 0.02f;
                tot += w[i];
            }
            float r = d.F() * tot;
            for (int i = 0; i < list.Length; i++) { r -= w[i]; if (r <= 0) return list[i].Item1; }
            return list[^1].Item1;
        }

        static T Pick<T>(Dice d, IReadOnlyList<T> l, IReadOnlyList<int> w)
        {
            int tot = 0; foreach (var x in w) tot += x; int r = d.Int(tot);
            for (int i = 0; i < l.Count; i++) { r -= w[i]; if (r < 0) return l[i]; }
            return l[^1];
        }

        // ------------------------------------------------------------ buildings

        /// <summary>Host table (tallest mass) and the street-edge sequences.</summary>
        static Dictionary<string, MassDto> Index(WorldDto world)
        {
            Hosts.Clear(); Signs.Clear(); HostEdges.Clear(); ShopEdges.Clear(); UseOf.Clear();
            // host use = its tallest mass's surface (the body; a lone mass is its own)
            var tall = new Dictionary<string, MassDto>();
            foreach (var m in world.Buildings)
            {
                var id = FacadeKit.HostOf(m.Id);
                if (!tall.TryGetValue(id, out var t) || m.Base + m.Height > t.Base + t.Height) tall[id] = m;
            }
            // edges: frontages grouped by street and side, in walking order
            var groups = new Dictionary<string, List<FrontageDto>>();
            foreach (var f in world.Frontages.Where(f => f.Level == "street"))
            {
                var key = $"{f.Street}|{Mathf.RoundToInt(f.Normal.x)},{Mathf.RoundToInt(f.Normal.y)}|{(Mathf.Abs(f.Normal.x) > Mathf.Abs(f.Normal.y) ? Mathf.Round(f.A.x) : Mathf.Round(f.A.y))}";
                if (!groups.TryGetValue(key, out var l)) groups[key] = l = new();
                l.Add(f);
            }
            foreach (var kv in groups.OrderBy(k => k.Key, System.StringComparer.Ordinal))
            {
                var l = kv.Value;
                l.Sort((p, q) => Along(p).CompareTo(Along(q)));
                ShopEdges.Add(l);
                var hs = new List<string>();
                foreach (var f in l) if (hs.Count == 0 || hs[^1] != f.Host) hs.Add(f.Host);
                hs.RemoveAll(x => !tall.ContainsKey(x));
                HostEdges.Add(hs);
            }
            return tall;
        }

        public static void Assign(WorldDto world, FacadeKit fk)
        {
            var tall = Index(world);
            var nb = new Dictionary<string, HashSet<string>>();
            foreach (var e in HostEdges)
                for (int i = 0; i + 1 < e.Count; i++)
                {
                    if (!nb.TryGetValue(e[i], out var a)) nb[e[i]] = a = new(); a.Add(e[i + 1]);
                    if (!nb.TryGetValue(e[i + 1], out var b)) nb[e[i + 1]] = b = new(); b.Add(e[i]);
                }
            foreach (var id in tall.Keys.OrderBy(k => k, System.StringComparer.Ordinal))
            {
                var h = fk.Resolve(id);
                string use = UseFor(tall[id].Surface); UseOf[id] = use;
                Grammar best = null; int bestScore = int.MinValue;
                for (int tryN = 0; tryN < 48; tryN++)
                {
                    var g = Make(h, use, (uint)tryN, fk.Spec);
                    int score = 0;
                    if (nb.TryGetValue(id, out var ns))
                        foreach (var o in ns)
                        {
                            if (!Hosts.TryGetValue(o, out var og)) continue;
                            if (og.Coarse == g.Coarse) score -= 100;
                            if (og.Fam == g.Fam) score -= 6;
                            if (og.WallFam == g.WallFam) score -= 4;
                            if (og.A == g.A) score -= 2;
                            if (og.Corner == g.Corner && og.Crown == g.Crown) score -= 3;
                            if (og.Rhythm == g.Rhythm) score -= 1;
                        }
                    if (score > bestScore) { bestScore = score; best = g; }
                    if (score == 0) break;
                }
                Hosts[id] = best;
                var fam = fk.Spec.Families[best.WallFam];
                h.Wall = fam[0]; h.Plinth = fam[1]; h.A = best.A; h.B = best.B;
            }
            AssignSigns(world, fk);
        }

        static float Along(FrontageDto f)
        {
            var t = new Vector2(f.Normal.y, -f.Normal.x); var m = (f.A + f.B) * 0.5f; return Vector2.Dot(m, t);
        }

        static Grammar Make(Host h, string use, uint salt, FacadeSpec spec)
        {
            var d = new Dice(h.Id, 101 + salt * 7919);
            var g = new Grammar { Use = use };
            g.Rhythm = Rhythms[d.Int(Rhythms.Length)]; g.Mirror = d.Int(3) == 0; g.Bay = Bays[d.Int(Bays.Length)];
            if (use == "tower") { g.Rhythm = d.Int(2) == 0 ? "A" : "AAB"; }
            g.Fam = WeightedFam(d, use, h.Area, null, null);
            g.Fam2 = g.Rhythm.Contains('C') ? WeightedFam(d, use, h.Area, g.Fam, null) : g.Fam;
            g.BaseFam = new[] { "punched", "ribbon", "porthole", "glassblock", "fins" }[d.Int(5)];
            if (g.BaseFam == g.Fam) g.BaseFam = "punched" == g.Fam ? "ribbon" : "punched";
            g.TopFam = new[] { g.Fam, g.Fam, "ribbon", "porthole", "fins", "eyebrow" }[d.Int(6)];
            g.PodFam = new[] { "ribbon", "punched", "glassblock", "porthole", "brise", "eyebrow" }[d.Int(6)];
            g.Corner = Pick(d, Corners, new[] { 2, 4, 3, 2 });
            g.Radius = g.Corner == "radius" ? 5.3f * (0.7f + 0.3f * d.F()) : 2.4f + d.F() * 1.6f;
            if (g.Corner == "radius") d.Int(3); // a spent draw, kept so every later pick (and every facade) stays as the probe shot it
            g.CornerIx = d.Int(4);
            if (g.Corner == "radius" && d.Int(2) == 0) g.Radius = 5.3f;
            g.Crown = Pick(d, Crowns, new[] { 3, 2, 2, 2, 1, 1 });
            g.Cornice = Cornices[d.Int(Cornices.Length)];
            // colourway: wall family by area weights, enamel pair
            var (names, w) = spec.AreaFamilies[h.Area ?? "harbour"];
            g.WallFam = Pick(d, names, w);
            var en = spec.AreaEnamel[h.Area ?? "harbour"]; int ia = d.Int(en.Length);
            g.A = en[ia]; g.B = en[(ia + 1 + d.Int(en.Length - 1)) % en.Length];
            return g;
        }

        // ------------------------------------------------------------ signs

        static readonly Dictionary<string, string[]> TradeShapes = new()
        {
            ["diner"] = new[] { "starburst", "boomerang", "arrow" }, ["automat"] = new[] { "bar", "disc", "pennant" },
            ["soda-fountain"] = new[] { "disc", "starburst", "pennant" }, ["newsstand"] = new[] { "blade", "bar" },
            ["cocktail-lounge"] = new[] { "arrow", "boomerang", "disc" }, ["tiki-bar"] = new[] { "pennant", "pylon", "boomerang" },
            ["dockside-tavern"] = new[] { "blade", "disc" }, ["jazz-club"] = new[] { "pylon", "starburst" },
            ["beauty-salon"] = new[] { "disc", "blade" }, ["florist"] = new[] { "disc", "pennant" }, ["record-shop"] = new[] { "disc", "blade", "arrow" },
            ["travel-bureau"] = new[] { "arrow", "pylon" }, ["couture-boutique"] = new[] { "boomerang", "blade" },
            ["market-stall"] = new[] { "pennant", "bar" }, ["grocer"] = new[] { "bar", "pennant" }, ["robot-repair"] = new[] { "pylon", "disc" },
            ["radio-tv-repair"] = new[] { "pylon", "arrow" }, ["launderette"] = new[] { "disc", "bar" }, ["hotel-lobby"] = new[] { "pylon", "bar" },
            ["pawnbroker"] = new[] { "blade", "bar" }, ["barber"] = new[] { "blade", "pylon" }, ["lobby"] = new[] { "plaque", "disc" },
            ["ships-chandler"] = new[] { "blade", "pennant" }, ["cinema"] = new[] { "pylon", "starburst" }, ["tailor"] = new[] { "blade", "arrow" },
        };
        static readonly string[] AllShapes = { "arrow", "starburst", "boomerang", "disc", "pylon", "blade", "pennant", "bar" };

        static void AssignSigns(WorldDto world, FacadeKit fk)
        {
            var spec = fk.Spec;
            foreach (var edge in ShopEdges)
            {
                SignSpec prev = null;
                foreach (var f in edge)
                {
                    var h = fk.Resolve(f.Host);
                    SignSpec best = null; int bestScore = int.MinValue;
                    for (int tryN = 0; tryN < 40; tryN++)
                    {
                        var s = MakeSign(f, h, (uint)tryN, spec);
                        int score = 0;
                        if (prev != null)
                        {
                            if (prev.Key == s.Key) score -= 100;
                            if (prev.Shape == s.Shape) score -= 8;
                            if (prev.Board == s.Board) score -= 4;
                            if (prev.Icon == s.Icon) score -= 1;
                        }
                        if (score > bestScore) { bestScore = score; best = s; }
                        if (score == 0) break;
                    }
                    Signs[f.Id] = best; prev = best;
                }
            }
            foreach (var f in world.Frontages) if (!Signs.ContainsKey(f.Id)) Signs[f.Id] = MakeSign(f, fk.Resolve(f.Host), 0, spec);
        }

        static SignSpec MakeSign(FrontageDto f, Host h, uint salt, FacadeSpec spec)
        {
            var d = new Dice(f.Id, 211 + salt * 104729);
            string trade = f.Trade ?? "";
            string[] tc = spec.Trades.TryGetValue(trade, out var t) ? t : spec.TradeDefault;
            var shapes = TradeShapes.TryGetValue(trade, out var sh) ? sh : AllShapes;
            // trade family look: mostly its own shapes, sometimes one from the whole set
            string shape = d.Int(7) == 0 ? AllShapes[d.Int(AllShapes.Length)] : shapes[d.Int(shapes.Length)];
            if (f.Lobby || trade == "lobby") shape = shapes[d.Int(shapes.Length)];
            if (f.SignKit != null && (shape == "blade" || shape == "pylon")) shape = "bar"; // a modelled blade or wall sign is placed on these
            string[] en = spec.AreaEnamel[h.Area ?? "harbour"];
            var s = new SignSpec { Shape = shape, Icon = f.Icon ?? "page", Trade = trade, Kit = f.SignKit?.Id };
            switch (d.Int(5))
            {
                case 0: s.Board = tc[0]; s.Tube = "white"; s.Ink = "ink"; break;
                case 1: s.Board = "paper"; s.Tube = tc[0]; s.Ink = tc[0]; break;
                case 2: s.Board = "ink"; s.Tube = tc[0]; s.Ink = tc[1]; break;
                case 3: s.Board = en[d.Int(en.Length)]; s.Tube = "white"; s.Ink = "ink"; break;
                default: s.Board = tc[1] == "chrome" || tc[1] == "chrome_dark" ? "paper" : tc[1]; s.Tube = tc[0]; s.Ink = "ink"; break;
            }
            if (f.Lobby || trade == "lobby") { s.Board = h.A; s.Tube = "white"; s.Ink = "ink"; }
            if (s.Board == s.Tube) s.Tube = "white";
            if (s.Board == "ink") s.Ink = "paper";
            s.Mount = shape switch { "blade" => "blade", "starburst" or "disc" or "pylon" => "stand", _ => "fascia" };
            return s;
        }

        // ------------------------------------------------------------ metrics

        public static string Metrics(WorldDto world, FacadeKit fk)
        {
            var sb = new System.Text.StringBuilder();
            var ids = Hosts.Keys.ToList();
            var groups = ids.GroupBy(id => Hosts[id].Coarse).ToList();
            int adj = 0, adjPairs = 0;
            foreach (var e in HostEdges)
                for (int i = 0; i + 1 < e.Count; i++) { adjPairs++; if (Hosts[e[i]].Coarse == Hosts[e[i + 1]].Coarse) adj++; }
            var sg = world.Frontages.GroupBy(f => Signs[f.Id].Key).ToList();
            int sAdj = 0, sPairs = 0;
            foreach (var e in ShopEdges)
                for (int i = 0; i + 1 < e.Count; i++) { sPairs++; if (Signs[e[i].Id].Key == Signs[e[i + 1].Id].Key) sAdj++; }
            sb.Append($"{{\"buildings\":{ids.Count},\"facadeSignatures\":{groups.Count},\"largestFacadeGroup\":{groups.Max(g => g.Count())}");
            sb.Append($",\"fullGrammarSignatures\":{ids.Select(i => Hosts[i].Full).Distinct().Count()},\"windowFamiliesUsed\":{ids.Select(i => Hosts[i].Fam).Distinct().Count()}");
            sb.Append($",\"streetEdges\":{HostEdges.Count},\"edgeNeighbourPairs\":{adjPairs},\"adjacentDuplicateFacades\":{adj}");
            sb.Append($",\"frontages\":{world.Frontages.Count},\"distinctSigns\":{sg.Count},\"largestSignGroup\":{sg.Max(g => g.Count())}");
            sb.Append($",\"signNeighbourPairs\":{sPairs},\"adjacentDuplicateSigns\":{sAdj}");
            sb.Append(",\"corners\":" + Hist(ids.Select(i => Hosts[i].Corner)) + ",\"windowFamilies\":" + Hist(ids.Select(i => Hosts[i].Fam)));
            sb.Append(",\"crowns\":" + Hist(ids.Select(i => Hosts[i].Crown)) + ",\"signShapes\":" + Hist(world.Frontages.Select(f => Signs[f.Id].Shape)));
            sb.Append(",\"rhythms\":" + Hist(ids.Select(i => Hosts[i].Rhythm)));
            sb.Append(",\"topFacadeGroups\":[" + string.Join(",", groups.OrderByDescending(g => g.Count()).Take(5).Select(g => g.Count())) + "]}");
            return sb.ToString();
        }

        static string Hist(IEnumerable<string> xs) =>
            "{" + string.Join(",", xs.GroupBy(x => x).OrderByDescending(g => g.Count()).Select(g => $"\"{g.Key}\":{g.Count()}")) + "}";
    }
}
