using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Somewhere to jump to. A plan place is a ground point with its extent (each mode frames it its own way); a saved
    /// view carries the camera it was built with and is adopted as it is.</summary>
    public sealed class Place
    {
        public string Group, Name;
        /// <summary>The place on the ground (Unity frame), its plan radius and its top.</summary>
        public Vector3 Target;
        public float Radius, Top;
        /// <summary>A saved camera (a views.json view as built, a flyby.json stop as resolved); ShadowFocus set = an export view's built shadow fit.</summary>
        public Pose? View;
    }

    /// <summary>The jump list: the plan's areas, the export's landmarks, signature structures, megastructures and civic block
    /// (the café, the Records Office and what stands round them), the built views.json cameras and the flyby.json stops.
    /// Nothing is hand-placed: every entry comes from the plan, the export or the scene.</summary>
    public static class WalkPlaces
    {
        public static List<Place> Load(WorldDto w, WorldIndex index, PlanIndex plan, Transform cameras)
        {
            var list = new List<Place>();
            foreach (var a in plan.Areas)
                list.Add(new Place { Group = "Plan areas", Name = $"{a.Name}  ({a.Id})", Target = new Vector3(a.Label.x, 0f, a.Label.y) });
            foreach (var c in w.Ground.Circuses)
                list.Add(new Place { Group = "Landmarks", Name = c.Id, Target = new Vector3(c.Centre.x, 0f, c.Centre.y), Radius = c.Radius });
            foreach (var l in w.Landmarks) Element(list, "Landmarks", l.Id, index, plan);
            foreach (var s in w.Signatures) Element(list, "Signature structures", s.Id, index, plan);
            foreach (var m in w.Megastructures) Element(list, "Megastructures", m.Id, index, plan);
            foreach (var c in w.Civic) Element(list, "Civic block", c.Id, index, plan);

            if (cameras)
                foreach (Transform t in cameras)
                {
                    if (!t.TryGetComponent<Camera>(out var c)) continue;
                    var pose = new Pose
                    {
                        Position = t.position, Rotation = t.rotation, Ortho = c.orthographic, OrthoSize = c.orthographicSize, Fov = c.fieldOfView,
                        Near = c.nearClipPlane, Far = c.farClipPlane, ShadowFocus = t.TryGetComponent<TacticalShadows>(out var ts) ? ts.focusDepth : float.NaN,
                    };
                    list.Add(new Place { Group = "Views (views.json)", Name = t.name, Target = Ground(pose), View = pose });
                }

            try
            {
                var path = new FlybyPath(FlybyPath.DefaultPath, index);
                foreach (var k in path.Keys)
                {
                    if (!k.Contact) continue;
                    var pose = path.Evaluate(k.Start);
                    pose.ShadowFocus = float.NaN; // the flyby stands its play stop 800 m back for the dolly; here the stop is refitted like any play view
                    list.Add(new Place { Group = "Flyby stops (flyby.json)", Name = k.Name, Target = Ground(pose), View = pose });
                }
            }
            catch (System.IO.InvalidDataException e) { Debug.LogWarning($"World: walk review: flyby stops left out of the jump list ({e.Message})"); }
            return list;
        }

        static void Element(List<Place> list, string group, string id, WorldIndex index, PlanIndex plan)
        {
            if (!index.ById.TryGetValue(id, out var item) || item.Box.size == Vector3.zero) return;
            var b = item.Box;
            list.Add(new Place
            {
                Group = group, Name = plan.Name(id) is { } name ? $"{name}  ({id})" : id,
                Target = new Vector3(b.center.x, Mathf.Max(b.min.y, 0f), b.center.z), Radius = Mathf.Max(b.extents.x, b.extents.z), Top = b.max.y,
            });
        }

        /// <summary>Where a camera's axis meets the street datum; a camera looking level or up stands for itself.</summary>
        public static Vector3 Ground(Pose pose)
        {
            var fwd = pose.Rotation * Vector3.forward;
            return fwd.y < -0.05f ? pose.Position + fwd * (pose.Position.y / -fwd.y) : new Vector3(pose.Position.x, 0f, pose.Position.z);
        }
    }
}
