using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>facade.json: geometry constants, swatches (baked into the Atlas), colour families, per-area enamel sets, trade
    /// colours, roof item colours and sign icon sketches. No colour lives in the facade code.</summary>
    public sealed class FacadeSpec
    {
        public readonly float Bay, Ground, GroundUpper, Storey, Cap, Parapet, Plinth, ShopHeight;
        public readonly Dictionary<string, string[]> Families = new();
        public readonly Dictionary<string, (string[] Names, int[] Weights)> AreaFamilies = new();
        public readonly Dictionary<string, string[]> AreaEnamel = new();
        public readonly Dictionary<string, string[]> Trades = new();
        public readonly Dictionary<string, string> RoofItems = new();
        public readonly Dictionary<string, string> Icons = new();
        public readonly string[] Glass, Roofs, Greens, TradeDefault;
        public readonly Atlas Atlas = new();

        public FacadeSpec(string path, Look look, WorldDto world)
        {
            var o = Json.Parse(File.ReadAllText(path));
            var g = J.Get(o, "geometry");
            Bay = J.F(g, "bay"); Ground = J.F(g, "ground"); GroundUpper = J.F(g, "groundUpper"); Storey = J.F(g, "storey");
            Cap = J.F(g, "cap"); Parapet = J.F(g, "parapet"); Plinth = J.F(g, "plinth"); ShopHeight = J.F(g, "shopHeight");
            foreach (var kv in J.O(J.Get(o, "swatches")))
            {
                if (!ColorUtility.TryParseHtmlString((string)kv.Value, out var c)) throw new InvalidDataException($"World: facade swatch {kv.Key} hex {kv.Value}");
                Atlas.Add(kv.Key, c);
            }
            // Sign boards: the export's palette keys (§15), with facade.json overrides where a §15 hex is too close to a signal.
            var over = J.Get(o, "paletteOverride") is Dictionary<string, object> po ? po : new Dictionary<string, object>();
            foreach (var f in world.Frontages)
            {
                var key = "pal:" + f.Palette;
                if (Atlas.Has(key)) continue;
                if (over.TryGetValue(f.Palette ?? "", out var hex) && ColorUtility.TryParseHtmlString((string)hex, out var oc)) Atlas.Add(key, oc);
                else Atlas.Add(key, look.Palette(f.Palette));
            }
            // Megastructure and gate walls keep their look.json surface colour.
            foreach (var surface in MegaKit.Surfaces(world)) Atlas.Add(MegaKit.Swatch(surface), look.SurfaceColour(surface));
            // Skin accents (speed bands), window frames and porthole rings keep their look.json surface colour too.
            foreach (var surface in ProcSkin.Surfaces(world)) Atlas.Add(ProcSkin.Swatch(surface), look.SurfaceColour(surface));
            var mat = J.Get(o, "materials");
            Atlas.Bake(Shader.Find("Suborned/FacadeLit") ?? throw new InvalidDataException("World: Suborned/FacadeLit shader not found"), J.F(mat, "matte"), J.F(mat, "glossy"));
            foreach (var kv in J.O(J.Get(o, "families"))) Families[kv.Key] = Strs(kv.Value);
            foreach (var kv in J.O(J.Get(o, "areas")))
            {
                var fam = J.O(J.Get(kv.Value, "families")); var names = new List<string>(); var w = new List<int>();
                foreach (var f in fam) { names.Add(f.Key); w.Add((int)(double)f.Value); }
                AreaFamilies[kv.Key] = (names.ToArray(), w.ToArray());
                AreaEnamel[kv.Key] = Strs(J.Get(kv.Value, "enamel"));
            }
            foreach (var kv in J.O(J.Get(o, "trades"))) Trades[kv.Key] = Strs(kv.Value);
            foreach (var kv in J.O(J.Get(o, "roofItems"))) RoofItems[kv.Key] = (string)kv.Value;
            foreach (var kv in J.O(J.Get(o, "icons"))) Icons[kv.Key] = (string)kv.Value;
            Glass = Strs(J.Get(o, "glass")); Roofs = Strs(J.Get(o, "roofs")); Greens = Strs(J.Get(o, "greens")); TradeDefault = Strs(J.Get(o, "tradeDefault"));
        }

        static string[] Strs(object a) { var l = J.A(a); var r = new string[l.Count]; for (int i = 0; i < r.Length; i++) r[i] = (string)l[i]; return r; }
    }

    /// <summary>Small deterministic generator (FNV-1a seed, xorshift): the same id gives the same dice on every machine.</summary>
    public struct Dice
    {
        uint s;
        public Dice(string id, uint salt = 0)
        {
            uint h = 2166136261u ^ salt;
            foreach (var ch in id) { h ^= ch; h *= 16777619u; }
            s = h == 0 ? 1u : h; Next(); Next();
        }
        public uint Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
        public int Int(int n) => (int)(Next() % (uint)n);
        public float F() => (Next() & 0xFFFFFF) / (float)0x1000000;
        public T Pick<T>(IReadOnlyList<T> l) => l[Int(l.Count)];
    }
}
