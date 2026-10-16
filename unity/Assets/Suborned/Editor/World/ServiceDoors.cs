using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 013 E (Latch): the service doors of the civic block, drawn where the export's `takedown.entrances` name kind `service`
    /// (the Office's service door the Enforcer stashes a body through, the café's kitchen door). Stand-in geometry until Vela's kit piece:
    /// a chrome frame, an enamel leaf and a step, flush on the nearest wall of the building's main civic mass (`civic/&lt;building&gt;`),
    /// facing the door's `at` (which stands just outside that wall). One element per door under the `takedown` layer, which the export's
    /// counts do not declare, so Inspect.Verify's layer counts are unchanged. Unknown building = reject.</summary>
    public static class ServiceDoors
    {
        const float FrameW = 1.6f, FrameH = 2.6f, FrameD = 0.12f, LeafW = 1.2f, LeafH = 2.3f, LeafD = 0.06f, StepW = 1.8f, StepD = 0.6f, StepH = 0.12f;

        public static int Build(Nodes nodes, Transform layer, WorldDto w)
        {
            int n = 0;
            foreach (var e in w.Entrances.Where(e => e.Kind == "service").OrderBy(e => e.Id, StringComparer.Ordinal))
            {
                var mass = w.Civic.FirstOrDefault(m => m.Id == "civic/" + e.Building)
                    ?? throw new InvalidDataException($"World: service door {e.Id}: no civic mass civic/{e.Building}");
                var (a, b) = NearestEdge(mass.Ring, e.At);
                var d = (b - a).normalized;
                var foot = a + d * Vector2.Dot(e.At - a, d);              // the door on the wall line
                var normal = new Vector2(-d.y, d.x);
                if (Vector2.Dot(e.At - foot, normal) < 0) normal = -normal; // out toward `at`
                float yaw = Mathf.Atan2(d.y, d.x), z = mass.Base;
                var el = Nodes.Element(layer, "service-door/" + e.Id);
                // box size = [along yaw, across, h]: along = the wall, across = out of it
                nodes.Part(el, "frame", PrimKit.Add(new MeshKit(), "box", foot + normal * (FrameD * 0.5f), z, new Vector3(FrameW, FrameD, FrameH), yaw), "trim");
                nodes.Part(el, "leaf", PrimKit.Add(new MeshKit(), "box", foot + normal * (FrameD + LeafD * 0.5f), z + StepH, new Vector3(LeafW, LeafD, LeafH - StepH), yaw), "enamel");
                nodes.Part(el, "step", PrimKit.Add(new MeshKit(), "box", foot + normal * (StepD * 0.5f), Mathf.Max(0f, z - 0.06f), new Vector3(StepW, StepD, StepH), yaw), "plate");
                n++;
            }
            Debug.Log($"World: service doors {n} (export takedown.entrances kind service)");
            return n;
        }

        static (Vector2 a, Vector2 b) NearestEdge(Vector2[] ring, Vector2 p)
        {
            float best = float.MaxValue; (Vector2, Vector2) e = default;
            for (int i = 0; i < ring.Length; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Length];
                if ((b - a).sqrMagnitude < 1e-6f) continue;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (b - a).sqrMagnitude);
                float dist = (a + (b - a) * t - p).sqrMagnitude;
                if (dist < best) { best = dist; e = (a, b); }
            }
            if (best == float.MaxValue) throw new InvalidDataException("World: service door: the mass has no edge");
            return e;
        }
    }
}
