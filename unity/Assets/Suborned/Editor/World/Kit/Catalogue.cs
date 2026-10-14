using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>kit.json: street-item shapes (parts in fractions of the item size), connector surfaces, crossing paint, batch tile size.
    /// Unknown kind = reject.</summary>
    public sealed class Catalogue
    {
        public sealed class Part { public string Shape, Surface; public bool ByH; public Vector2 C; public float Base, Yaw; public Vector3 Size; }

        readonly Dictionary<string, List<Part>> street = new();
        readonly Dictionary<string, string> connectorSurfaces = new();
        public readonly float BatchTile, FadeCell, Slab;
        /// <summary>Whether the shadow proxies (ShadowProxies) are on as built (kit.json `shadowProxies`).</summary>
        public readonly bool ShadowProxies;
        /// <summary>How a crossing is drawn (kit.json `crossings`): Paint = how far paint stands off the surface it is on, Bar and Gap =
        /// a zebra's bar and the space between two, Line = an edge line's width, Lip = how far a refuge stands proud of its median, Reach =
        /// the most a kerb batch may grow to take a crossing in, Surface = the paint of a crossing whose road the export does not name.</summary>
        public sealed class CrossingSpec { public float Paint, Bar, Gap, Line, Lip, Reach; public string Surface; }
        public readonly CrossingSpec Crossings;

        public Catalogue(string path)
        {
            var o = Json.Parse(File.ReadAllText(path));
            BatchTile = J.F(o, "batchTile");
            FadeCell = J.F(o, "fadeCell");
            ShadowProxies = J.B(o, "shadowProxies");
            var con = J.Get(o, "connectors");
            Slab = J.F(con, "slab");
            foreach (var kv in J.O(J.Get(con, "surfaces"))) connectorSurfaces[kv.Key] = (string)kv.Value;
            var x = J.Get(o, "crossings") ?? throw new InvalidDataException("World: kit.json has no `crossings`");
            Crossings = new CrossingSpec { Paint = J.F(x, "paint"), Bar = J.F(x, "bar"), Gap = J.F(x, "gap"), Line = J.F(x, "line"), Lip = J.F(x, "lip"), Reach = J.F(x, "reach"), Surface = J.S(x, "surface") };
            foreach (var kv in J.O(J.Get(o, "street")))
            {
                var parts = new List<Part>();
                foreach (var p in J.A(kv.Value))
                {
                    var s = J.A(p, "size");
                    parts.Add(new Part
                    {
                        Shape = J.S(p, "shape"), Surface = J.S(p, "surface"), ByH = J.S(p, "by") == "h",
                        C = J.Get(p, "c") != null ? J.P(p, "c") : Vector2.zero,
                        Base = J.Get(p, "base") is double b ? (float)b : 0f, Yaw = J.Get(p, "yaw") is double y ? (float)y : 0f,
                        Size = new Vector3((float)(double)s[0], (float)(double)s[1], (float)(double)s[2]),
                    });
                }
                street[kv.Key] = parts;
            }
        }

        public List<Part> Street(string kind, string variant) =>
            (variant != null && street.TryGetValue(kind + "/" + variant, out var v)) ? v
            : street.TryGetValue(kind, out var k) ? k
            : throw new InvalidDataException($"World: street kind `{kind}`{(variant != null ? "/" + variant : "")} not in kit.json");

        public string ConnectorSurface(string kind) =>
            connectorSurfaces.TryGetValue(kind, out var s) ? s : throw new InvalidDataException($"World: connector kind `{kind}` not in kit.json");
    }
}
