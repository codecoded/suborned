#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Suborned.World;
using G = Suborned.OldTown.Editor.Swap.SwapTramJoin;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Pinned lossless sidecar reader. No generation or exception-driven run splitting.
    /// Source producer hashes are checked before use; the old helper hash is a recorded consumer preimage,
    /// not a producer dependency (this phase intentionally changes that helper).</summary>
    internal static class SwapTramSource
    {
        internal const string Sidecar = "briefs/075/recovery/tram-topology/topology.json";
        internal const string SidecarSha = "0cc1c1934c5f5281e809e7a497692fe182ab167817da330d80ad14384fbce950";
        internal static Dictionary<string, object> Obj(object o) => (Dictionary<string, object>)o;
        internal static List<object> Arr(object o) => (List<object>)o;
        internal static double Num(object o) => (double)o;
        internal static G.P Point(object o) { var a = Arr(o); return new G.P(Num(a[0]), Num(a[1])); }
        internal static string Hash(string path) { using var sha = SHA256.Create(); return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2"))); }
        static bool Equal(object a, object b)
        {
            if (a is Dictionary<string, object> d && b is Dictionary<string, object> e) return d.Count == e.Count && d.All(k => e.ContainsKey(k.Key) && Equal(k.Value, e[k.Key]));
            if (a is List<object> x && b is List<object> y) return x.Count == y.Count && x.Zip(y, Equal).All(v => v);
            return Equals(a, b);
        }
        internal static G.Footprint[] Load(string repo, string world)
        {
            string Abs(string p) => Path.IsPathRooted(p) ? p : Path.Combine(repo, p);
            if (Hash(Abs(Sidecar)) != SidecarSha) throw new InvalidDataException("tram topology sidecar pin mismatch");
            var root = Obj(Json.Parse(File.ReadAllText(Abs(Sidecar))));
            if ((string)root["schema"] != "suborned.tram-topology/1") throw new InvalidDataException("tram topology schema");
            foreach (var pair in Obj(root["inputs"])) { var pin = Obj(pair.Value); if (Hash(Abs((string)pin["path"])) != (string)pin["sha256"]) throw new InvalidDataException("tram input pin: " + pair.Key); }
            if (Hash(Abs(world)) != (string)Obj(Obj(root["inputs"])["world"])["sha256"]) throw new InvalidDataException("tram active world pin");
            foreach (var pair in Obj(root["sourceHashes"]))
                if (pair.Key != "unity/Assets/Suborned/Editor/OldTown/Swap/SwapTramJoin.cs" && Hash(Abs(pair.Key)) != (string)pair.Value)
                    throw new InvalidDataException("tram producer pin: " + pair.Key);
            var rawWorld = Obj(Json.Parse(File.ReadAllText(Abs(world))));
            var parents = Arr(Obj(rawWorld["ground"])["pieces"]).Select(Obj).Where(p => (string)p["kind"] == "tram-rail").ToDictionary(p => (string)p["id"]);
            var items = Arr(root["items"]).Select(Obj).ToArray();
            if (items.Length != parents.Count) throw new InvalidDataException("tram omitted/duplicate parents");
            foreach (var item in items)
            {
                if (!item.TryGetValue("exported", out var exported) || exported == null || !Equal(exported, parents[(string)Obj(exported)["id"]]))
                    throw new InvalidDataException("tram parent not exactly pinned");
            }
            var segments = new Dictionary<string, G.Segment>();
            var tracks = Arr(root["tracks"]).Select(Obj).ToDictionary(p => (string)p["id"]);
            string Key(string track, object segment) => track + ":" + Num(segment);
            foreach (var track in tracks)
            {
                if (!(bool)track.Value["continuous"] || (bool)track.Value["closed"] || Arr(track.Value["omitted"]).Count != 0) throw new InvalidDataException("unsupported source topology");
                foreach (var value in Arr(track.Value["segments"]))
                {
                    var s = Obj(value); var geom = Obj(s["geometry"]); double length = Num(s["length"]);
                    double k = (string)geom["kind"] == "line" ? 0 : Num(geom["sweep"]) / length;
                    var key = Key(track.Key, s["segment"]);
                    segments.Add(key, new G.Segment(key, Point(s["start"]), Point(s["startTangent"]), length, k));
                    var sg = segments[key];
                    if ((sg.At(length) - Point(s["end"])).Length > 1e-9 || (sg.Tangent(length) - Point(s["endTangent"])).Length > 1e-9)
                        throw new InvalidDataException("tram source endpoint/tangent mismatch");
                }
            }
            var tiles = new Dictionary<string, G.Interval>();
            foreach (var value in Arr(root["tiles"]))
            {
                var t = Obj(value); var id = (string)t["id"]; var parent = parents[(string)t["parent"]];
                int n = Math.Max(1, (int)Math.Floor(Num(Arr(parent["size"])[0]) / 2 + .5));
                int i = (int)Num(t["subdivision"]); double len = Num(Arr(parent["size"])[0]), yaw = Num(parent["yaw"]);
                var roundedT = new G.P(Math.Cos(yaw), Math.Sin(yaw)); var roundedC = Point(parent["at"]) + roundedT * (-len/2 + len/n*(i+.5));
                var c = Point(t["center"]); var u = Point(t["tangent"]); double step = Num(t["step"]);
                double tangentBound = 2 * Math.Sin(.00005 / 2), centreBound = Math.Sqrt(2)*.0005 + .00025 + step*n/2*tangentBound;
                if (i < 0 || i >= n || Num(t["subdivisions"]) != n || (roundedC-c).Length > centreBound+1e-10 ||
                    (roundedT-u).Length > tangentBound+1e-10 || Math.Abs(len/n-step) > .0005/n+1e-10 ||
                    Math.Abs(Num(t["z"])-Num(parent["z"])) > .0005+1e-10)
                    throw new InvalidDataException("tram quantisation bound: " + id);
                var interval = Arr(t["interval"]); var segment = segments[Key((string)t["track"], t["segment"])];
                tiles.Add(id, new G.Interval(new G.Tile(id,c,u,step),segment,Num(interval[0]),Num(interval[1])));
            }
            if (tiles.Count != parents.Values.Sum(p => Math.Max(1,(int)Math.Floor(Num(Arr(p["size"])[0])/2+.5)))) throw new InvalidDataException("tram subdivision coverage");
            var graph = Arr(root["graph"]).Select(Obj).ToArray();
            var termini = graph.Where(e => Arr(e["joins"]).Count == 0).ToArray();
            if (termini.Length != 2 || graph.Any(e => Arr(e["joins"]).Count > 1)) throw new InvalidDataException("unsupported tram graph branch/cycle");
            var chain = new List<G.Interval>(); var visited = new HashSet<string>(); var reversed = new Dictionary<string,G.Segment>();
            var entry = termini[0];
            while (true)
            {
                string track = (string)entry["track"]; bool reverse = (string)entry["end"] == "end";
                if (!visited.Add(track)) throw new InvalidDataException("tram graph cycle");
                var ids = Arr(tracks[track]["orderedTiles"]).Select(x => (string)x).ToArray();
                foreach (var id in reverse ? ids.Reverse() : ids.AsEnumerable())
                {
                    var x = tiles[id];
                    if (!reverse) { chain.Add(x); continue; }
                    if (!reversed.TryGetValue(x.Segment.Key, out var sg))
                    {
                        var s = x.Segment; sg = new G.Segment(s.Key + "/reverse",s.At(s.Length),s.Tangent(s.Length)*-1,s.Length,-s.Curvature); reversed.Add(s.Key,sg);
                    }
                    chain.Add(new G.Interval(new G.Tile(x.Tile.Id,x.Tile.Centre,x.Tile.Tangent*-1,x.Tile.Step),sg,sg.Length-x.To,sg.Length-x.From));
                }
                var exit = graph.Single(e => (string)e["track"] == track && (string)e["end"] == (reverse ? "start" : "end"));
                var links = Arr(exit["joins"]); if (links.Count == 0) break;
                var link = Obj(links[0]); entry = graph.Single(e => (string)e["track"] == (string)link["track"] && (string)e["end"] == (string)link["end"]);
                if ((Point(exit["at"])-Point(entry["at"])).Length > 1e-9) throw new InvalidDataException("tram endpoint graph mismatch");
            }
            if (visited.Count != tracks.Count || chain.Count != tiles.Count || chain.Select(x => x.Tile.Id).Distinct().Count() != tiles.Count) throw new InvalidDataException("incomplete tram graph traversal");
            return G.BuildSource(chain);
        }
    }
}
#endif
