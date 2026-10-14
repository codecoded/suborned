// The mission's ground: sim/data/handover.site.json, written by tools/sim/site.ts from the plan, the civic block and mission.json.
// Block frame, integer centimetres. Places by id; the walk grid; the occluders; the cameras and the patrol; the schedule; the cast.
using System;
using System.Collections.Generic;
using System.Linq;

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

        // the mission's references, resolved and checked at load (Redline E1): place ids, facings, the schedule, the purse
        public readonly string GuardPost, GuardCafe, ValeExit, ValeCar, ValeService, CarPickup, CarDriver, CarService, CarBlock, RivalsPark, RivalsEntry, RivalsFront,
            SecEntry, SecVia, SecTo, GetawayCar, FightEntry, FightGuard, PatrolA, PatrolB, PatrolReroute;
        public readonly Pt GuardFacing, CafeFacing, PatrolFacing, PatrolRerouteFacing;
        public readonly string[] FightAmbush, FightPolice;
        public readonly int RivalsArrive, Chime, DriverLast, ValeExits, ValeAtCar, Over, GuardWalkS, GuardStayS, BlockMoveS, BlockHoldS;
        public readonly long Purse;

        public Site(string json)
        {
            Root = JNode.Parse(json, "site");
            var r = Root;
            r.Only("schema", "mission", "frame", "from", "grid", "occluders", "places", "cameras", "patrol", "guard", "vale", "car", "rivals", "security", "getaway", "fight", "schedule", "purse", "purseSrc", "cast");
            if (r["schema"].Str() != "suborned.sim.site/1") throw new FormatException("site schema is not suborned.sim.site/1");
            if (r["mission"].Str() != "handover") throw new FormatException("only the Handover plays (brief 011)");
            r["grid"].Only("x0", "y0", "cell", "nx", "ny", "body", "rows", "src");
            Grid = new Grid(r["grid"]);
            foreach (var o in r["occluders"].Items)
            {
                o.Only("id", "z", "h", "poly", "src");
                var poly = new List<Pt>(); foreach (var p in o["poly"].Items) poly.Add(Pt.Of(p));
                if (poly.Count < 3) throw new FormatException($"{o.Path}: an occluder needs 3 points");
                Occluders.Add(new Occluder(o["id"].Str(), o["z"].Int(), o["h"].Int(), poly.ToArray()));
            }
            foreach (var kv in r["places"].Entries)
            {
                kv.Value.Only("at", "src", "reason", "stand");
                var p = Pt.Of(kv.Value["at"]); at[kv.Key] = p;
                var s = kv.Value.Opt("stand") is JNode sn ? Pt.Of(sn) : p;
                if (!Grid.Free(s)) throw new FormatException($"place {kv.Key} stands on blocked ground");
                stand[kv.Key] = s;
            }
            foreach (var c in r["cameras"].Items)
            {
                c.Only("id", "at", "facing", "reach", "half", "src", "reason");
                var cam = new Camera { Id = c["id"].Str(), At = Pt.Of(c["at"]), Facing = Pt.Of(c["facing"]), Reach = c["reach"].Int(), Half = c["half"].Int() };
                if (cam.Facing == new Pt(0, 0) || cam.Reach <= 0 || cam.Reach > 100_000 || cam.Half < 0) throw new FormatException($"{c.Path}: a camera needs a facing and a reach");
                Cameras.Add(cam);
            }
            if (!Cameras.Exists(c => c.Id == "entrance") || !Cameras.Exists(c => c.Id == "pickup")) throw new FormatException("site cameras: the Handover needs 'entrance' and 'pickup'");
            string P(JNode n) { var id = n.Str(); if (!at.ContainsKey(id)) throw new FormatException($"{n.Path}: place '{id}' is not in the site"); return id; }
            Pt F(JNode n) { var v = Pt.Of(n); if (v == new Pt(0, 0)) throw new FormatException($"{n.Path}: a facing can't be zero"); return v; }
            var pa = r["patrol"]; pa.Only("a", "b", "facing", "reroute", "rerouteFacing", "src");
            PatrolA = P(pa["a"]); PatrolB = P(pa["b"]); PatrolReroute = P(pa["reroute"]); PatrolFacing = F(pa["facing"]); PatrolRerouteFacing = F(pa["rerouteFacing"]);
            var gu = r["guard"]; gu.Only("post", "facing", "cafe", "cafeFacing", "src");
            GuardPost = P(gu["post"]); GuardCafe = P(gu["cafe"]); GuardFacing = F(gu["facing"]); CafeFacing = F(gu["cafeFacing"]);
            var va = r["vale"]; va.Only("exit", "car", "service", "src"); ValeExit = P(va["exit"]); ValeCar = P(va["car"]); ValeService = P(va["service"]);
            var ca = r["car"]; ca.Only("pickup", "driver", "service", "block", "src"); CarPickup = P(ca["pickup"]); CarDriver = P(ca["driver"]); CarService = P(ca["service"]); CarBlock = P(ca["block"]);
            var ri = r["rivals"]; ri.Only("park", "entry", "front", "src"); RivalsPark = P(ri["park"]); RivalsEntry = P(ri["entry"]); RivalsFront = P(ri["front"]);
            var se = r["security"]; se.Only("entry", "via", "to", "src"); SecEntry = P(se["entry"]); SecVia = P(se["via"]); SecTo = P(se["to"]);
            var ge = r["getaway"]; ge.Only("car", "src"); GetawayCar = P(ge["car"]);
            var fi = r["fight"]; fi.Only("ambush", "police", "entry", "guard", "src");
            FightAmbush = fi["ambush"].Items.Select(P).ToArray(); FightPolice = fi["police"].Items.Select(P).ToArray(); FightEntry = P(fi["entry"]); FightGuard = P(fi["guard"]);
            var sc = r["schedule"]; sc.Only("rivalsArrive", "chime", "driverLast", "valeExits", "valeAtCar", "over", "guardWalkS", "guardStayS", "blockMoveS", "blockHoldS", "src");
            int Sec(string k, int lo) { int v = sc[k].Int(); if (v < lo || v > 3600) throw new FormatException($"{sc.Path}.{k} = {v} is outside {lo}..3600"); Schedule[k] = v; return v; }
            RivalsArrive = Sec("rivalsArrive", 0); Chime = Sec("chime", 0); DriverLast = Sec("driverLast", 0); ValeExits = Sec("valeExits", 0); ValeAtCar = Sec("valeAtCar", 0); Over = Sec("over", 1);
            GuardWalkS = Sec("guardWalkS", 1); GuardStayS = Sec("guardStayS", 1); BlockMoveS = Sec("blockMoveS", 1); BlockHoldS = Sec("blockHoldS", 1);
            if (!(DriverLast < ValeExits && ValeExits < ValeAtCar && ValeAtCar < Over)) throw new FormatException("site schedule: driverLast < valeExits < valeAtCar < over");
            Purse = r["purse"].Long(); if (Purse < 0 || Purse > 1_000_000_000) throw new FormatException("site purse is out of range");
            var cast = r["cast"]; cast.Only("waitress", "driver", "guard", "councillor", "from", "sha256", "startsAt", "fixture", "src");
            foreach (var role in new[] { "waitress", "driver", "guard", "councillor" })
            {
                var v = cast[role]; v.Only("id", "name", "temperament", "need");
                Cast.Add(new CastRole { Role = role, Id = v["id"].Str(), Name = v["name"].Str(), Temperament = v["temperament"].Str(), Need = v["need"].Str() });
            }
        }

        /// <summary>A place's own point (reach is measured from it).</summary>
        public Pt At(string id) => at.TryGetValue(id, out var p) ? p : throw new FormatException($"site has no place '{id}'");
        /// <summary>Where a walker stands for the place: the point itself, or the nearest free ground when the point is on a face.</summary>
        public Pt Stand(string id) => stand.TryGetValue(id, out var p) ? p : throw new FormatException($"site has no place '{id}'");
        public bool HasPlace(string id) => at.ContainsKey(id);
        public IEnumerable<string> PlaceIds => at.Keys;
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
