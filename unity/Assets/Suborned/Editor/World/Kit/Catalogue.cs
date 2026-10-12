using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>kit.json: street-item shapes (parts in fractions of the item size), connector surfaces, batch tile size.
    /// Unknown kind = reject.</summary>
    public sealed class Catalogue
    {
        public sealed class Part { public string Shape, Surface; public bool ByH; public Vector2 C; public float Base, Yaw; public Vector3 Size; }

        readonly Dictionary<string, List<Part>> street = new();
        readonly Dictionary<string, string> connectorSurfaces = new();
        public readonly float BatchTile, Slab;

        public Catalogue(string path)
        {
            var o = Json.Parse(File.ReadAllText(path));
            BatchTile = J.F(o, "batchTile");
            var con = J.Get(o, "connectors");
            Slab = J.F(con, "slab");
            foreach (var kv in J.O(J.Get(con, "surfaces"))) connectorSurfaces[kv.Key] = (string)kv.Value;
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
