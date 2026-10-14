// The mission's ground: sim/data/handover.site.json, written by tools/sim/site.ts from the plan, the civic block and mission.json.
// Block frame, integer centimetres. Places by id; the walk grid; the occluders; the cameras and the patrol; the schedule; the cast.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class Camera
    {
        public string Id = ""; public Pt At, Facing; public int Reach, Half;
    }
    public sealed class CastRole
    {
        public string Role = "", Id = "", Name = "", Temperament = "", Need = "";
    }

    public sealed class Site
    {
        public readonly JNode Root;
        public readonly Grid Grid;
        public readonly List<Occluder> Occluders = new List<Occluder>();
        readonly Dictionary<string, Pt> at = new Dictionary<string, Pt>(StringComparer.Ordinal);
        readonly Dictionary<string, Pt> stand = new Dictionary<string, Pt>(StringComparer.Ordinal);
        public readonly List<Camera> Cameras = new List<Camera>();
        public readonly List<CastRole> Cast = new List<CastRole>();
        public readonly Dictionary<string, int> Schedule = new Dictionary<string, int>(StringComparer.Ordinal);

        public Site(string json)
        {
            Root = JNode.Parse(json, "site");
            if (Root["schema"].Str() != "suborned.sim.site/1") throw new FormatException("site schema is not suborned.sim.site/1");
            Grid = new Grid(Root["grid"]);
            foreach (var o in Root["occluders"].Items)
            {
                var poly = new List<Pt>(); foreach (var p in o["poly"].Items) poly.Add(Pt.Of(p));
                Occluders.Add(new Occluder(o["id"].Str(), o["z"].Int(), o["h"].Int(), poly.ToArray()));
            }
            foreach (var kv in Root["places"].Entries)
            {
                var p = Pt.Of(kv.Value["at"]); at[kv.Key] = p;
                var s = kv.Value.Opt("stand") is JNode sn ? Pt.Of(sn) : p;
                if (!Grid.Free(s)) throw new FormatException($"place {kv.Key} stands on blocked ground");
                stand[kv.Key] = s;
            }
            foreach (var c in Root["cameras"].Items)
                Cameras.Add(new Camera { Id = c["id"].Str(), At = Pt.Of(c["at"]), Facing = Pt.Of(c["facing"]), Reach = c["reach"].Int(), Half = c["half"].Int() });
            foreach (var kv in Root["schedule"].Entries) if (kv.Value.Kind == JKind.Num) Schedule[kv.Key] = kv.Value.Int();
            foreach (var kv in Root["cast"].Entries)
            {
                if (kv.Value.Kind != JKind.Obj) continue;
                Cast.Add(new CastRole { Role = kv.Key, Id = kv.Value["id"].Str(), Name = kv.Value["name"].Str(), Temperament = kv.Value["temperament"].Str(), Need = kv.Value["need"].Str() });
            }
        }

        /// <summary>A place's own point (reach is measured from it).</summary>
        public Pt At(string id) => at.TryGetValue(id, out var p) ? p : throw new FormatException($"site has no place '{id}'");
        /// <summary>Where a walker stands for the place: the point itself, or the nearest free ground when the point is on a face.</summary>
        public Pt Stand(string id) => stand.TryGetValue(id, out var p) ? p : throw new FormatException($"site has no place '{id}'");
        public bool HasPlace(string id) => at.ContainsKey(id);
        public IEnumerable<string> PlaceIds => at.Keys;
        public string Ref(params string[] path) { JNode x = Root; foreach (var p in path) x = x[p]; return x.Str(); }
        public Pt Vec(params string[] path) { JNode x = Root; foreach (var p in path) x = x[p]; return Pt.Of(x); }
        public int S(string key) => Schedule.TryGetValue(key, out var v) ? v : throw new FormatException($"site schedule has no '{key}'");
        public CastRole Role(string role) { foreach (var c in Cast) if (c.Role == role) return c; throw new FormatException($"site cast has no role '{role}'"); }

        /// <summary>True if no occluder spanning eye height crosses the segment (§05 levels: sightlines in 3D).</summary>
        public bool Sight(Pt a, Pt b, int eye)
        {
            foreach (var o in Occluders) if (o.Blocks(a, b, eye)) return false;
            return true;
        }
        public bool InsideOccluder(Pt p) { foreach (var o in Occluders) if (o.Contains(p)) return true; return false; }
    }
}
