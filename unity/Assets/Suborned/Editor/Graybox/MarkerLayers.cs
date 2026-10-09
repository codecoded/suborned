using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>Point features the export gives as a position: district gates (open: a post on each kerb line, across the
    /// road from `at`, so the carriageway stays clear; the export has no seal state, so none is drawn sealed) and landmarks (drums of the given radius; height from Style.LandmarkHeight).</summary>
    public static class MarkerLayers
    {
        public static void Gates(Transform root, object world)
        {
            var widths = new Dictionary<string, float>();
            foreach (var r in J.A(world, "roads")) widths[J.S(r, "id")] = J.F(r, "width");
            foreach (var g in J.A(world, "gates"))
            {
                string road = J.S(g, "road");
                if (!widths.TryGetValue(road, out float width)) throw new InvalidDataException($"Graybox: gate {J.S(g, "id")} on unknown road {road}");
                Vector2 at = J.P(g, "at"), dir = J.P(g, "dir").normalized, across = new Vector2(-dir.y, dir.x) * (width * 0.5f + Style.GatePost);
                var kit = new MeshKit()
                    .Prism(Poly.Rect(at + across, dir, Style.GatePost, Style.GatePost), 0, Style.GateHeight)
                    .Prism(Poly.Rect(at - across, dir, Style.GatePost, Style.GatePost), 0, Style.GateHeight);
                Nodes.Part(root, J.S(g, "id"), kit, "gate");
            }
        }

        public static void Landmarks(Transform root, object world, float pavement)
        {
            foreach (var l in J.A(world, "landmarks"))
            {
                float r = J.F(l, "radius");
                var ring = Poly.Circle(J.P(l, "centre"), r, 48);
                Nodes.Part(root, J.S(l, "id"), new MeshKit().Prism(ring, 0, pavement + Style.LandmarkHeight(r)), "landmark");
            }
        }
    }
}
