#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.OldTown.Sky;
using Suborned.World;
using UnityEngine;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap: our network in the old look (keep/drop rows "Sky track beam and pylons", "Tram", "Tram rails"):
    /// - beams: every beam of our sky track (transit.skyTrack.beams, our route, levels and clearances) swept by the old DecoKitSkyRail beam
    ///   builder (its private Beam, called by reflection on an open SkyPath through our line at its top);
    /// - pylons: at our pylon points (the plan's 30 m spacing, the old spacing), the old pieces: SkyRail_Pylon_Tall foot, the stretched
    ///   SkyRail_Pylon_Shaft, the old single SkyRail_Pylon_Head, and our SkyRail_Pylon_HeadTwin where the line is double (the old had none);
    /// - tram: the old Tile_TramRail (2 m) along every rail piece of ours (our 12 m straights as six old tiles, our 2 m curve chords as one),
    ///   except where our line runs on the old boulevard track (the old track stands); the old Tram at our stops and parked tram; the old
    ///   TramCanopy at our stop shelters (our TramCanopy_Play only in the zone).
    /// Everything it places goes under one root the cut leaves alone, and adds the holes the old gives way to.</summary>
    internal sealed class SwapNetwork
    {
        readonly OldTownSwap.Report rep; readonly WorldDto w; readonly DecoKitPlacer placer; readonly OurKit kit;
        public readonly Transform Root;
        /// <summary>Old frame holes: beam corridors, pylon plinths, tram corridors.</summary>
        public readonly SwapShapes Holes = new();
        readonly List<(Vector2 at, Vector2 dir)> rails = new();
        const float BeamH = 1.6f;

        public SwapNetwork(OldTownSwap.Report rep, WorldDto w, DecoKitPlacer placer, OurKit kit)
        {
            this.rep = rep; this.w = w; this.placer = placer; this.kit = kit;
            Root = new GameObject("Network (our routes, old pieces)").transform;
        }

        GameObject Put(string piece, Vector3 at, float yaw, Transform parent)
        {
            var go = placer.Put(piece, at, yaw);
            if (go == null) throw new System.InvalidOperationException($"[Swap] old piece {piece} missing (NP-1)");
            go.transform.SetParent(parent, true);
            return go;
        }

        public void Beams()
        {
            var beam = typeof(DecoKitSkyRail).GetMethod("Beam", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new System.MissingMethodException("DecoKitSkyRail.Beam");
            var root = new GameObject("Sky track (old beam on our route)").transform; root.SetParent(Root, false);
            float m = 0f;
            foreach (var b in w.Transit.SkyTrack.Beams)
            {
                var go = new GameObject(b.Id); go.transform.SetParent(root, false);
                var path = go.AddComponent<SkyPath>();
                path.Set(b.Line.Select(v => OldTownSwap.Old3(v, b.Top)).ToArray(), false);
                beam.Invoke(null, new object[] { path, go.transform, "Swap_" + b.Id.Replace('/', '_').Replace('-', '_') });
                m += path.Length;
                Holes.Ribbon("beam", b.Id, b.Line.Select(OldTownSwap.Old).ToList(), 2.2f, b.Top - BeamH - 2.6f, b.Top + .6f);
            }
            rep.Log($"sky track: {w.Transit.SkyTrack.Beams.Count} beams, {m:0} m, swept by the old DecoKitSkyRail.Beam (ivory body, teal stripe) on our route at top {string.Join("/", w.Transit.SkyTrack.Beams.Select(b => b.Top).Distinct())} m");
        }

        public void Pylons()
        {
            var root = new GameObject("Sky track pylons (old pieces at our points)").transform; root.SetParent(Root, false);
            int tall = 0, bare = 0, twin = 0, single = 0;
            foreach (var p in w.Transit.SkyTrack.Pylons)
            {
                float yaw = -p.Yaw * Mathf.Rad2Deg;
                float ground = p.Foot != null ? p.FootZ : p.ShaftZ;
                if (Mathf.Abs(ground - .18f) < .02f) ground = L.Walk;  // our pavement top (0.18) is the old walk (0.15)
                var g = new GameObject(p.Id).transform; g.SetParent(root, false);
                float shaft0 = ground;
                if (p.Foot != null) { Put("SkyRail_Pylon_Tall", OldTownSwap.Old3(p.At, ground), yaw, g); shaft0 = ground + 12f; tall++; } else bare++;
                float headBottom = p.HeadZ - 2.2f;
                var sh = Put("SkyRail_Pylon_Shaft", OldTownSwap.Old3(p.At, shaft0), yaw, g);
                sh.transform.localScale = new Vector3(1f, Mathf.Max(.1f, headBottom - shaft0), 1f);
                if (p.Head != null && p.Head.Id == "SkyRail_Pylon_HeadTwin")
                {
                    var h = kit.Put("SkyRail_Pylon_HeadTwin", OldTownSwap.Old3(p.At, p.HeadZ), Quaternion.Euler(0f, yaw, 0f), g);
                    if (h == null) throw new System.InvalidOperationException("[Swap] our SkyRail_Pylon_HeadTwin missing (NP-1)");
                    twin++;
                }
                else { Put("SkyRail_Pylon_Head", OldTownSwap.Old3(p.At, p.HeadZ), yaw, g); single++; }
                Holes.Circle("pylon", p.Id, OldTownSwap.Old(p.At), 3f, -2f, p.HeadZ + 1f);
            }
            rep.Log($"pylons: {w.Transit.SkyTrack.Pylons.Count} at our points: {tall} on the old SkyRail_Pylon_Tall, {bare} bare shafts (export: the plan's point stands in a walk zone), heads {twin} our HeadTwin, {single} old single");
        }

        /// <summary>On the old boulevard track (x -2.5 between the kerbs, z -40 to the tram's end short of the circus): the old track stands.</summary>
        static bool OnOldTrack(Vector3 o) => Mathf.Abs(o.x - L.TrackX) < 4.6f && o.z > L.Z.x - 2f && o.z < P.CircusSouth - 4f;

        public void Rails()
        {
            var root = new GameObject("Tram rails (old Tile_TramRail on our route)").transform; root.SetParent(Root, false);
            int pieces = 0, tiles = 0, onOld = 0;
            foreach (var g in w.Ground.Pieces.Where(x => x.Kind == "tram-rail"))
            {
                var d = new Vector2(Mathf.Cos(g.Yaw), Mathf.Sin(g.Yaw));
                if (OnOldTrack(OldTownSwap.Old3(g.At, 0f))) { onOld++; continue; }
                float len = g.Size.x; int n = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
                float yaw = -g.Yaw * Mathf.Rad2Deg, step = len / n;
                for (int i = 0; i < n; i++)
                {
                    var c = g.At + d * (-len * .5f + step * (i + .5f));
                    Put("Tile_TramRail", OldTownSwap.Old3(c, .003f), yaw, root); tiles++;
                }
                pieces++;
                rails.Add((g.At, d));
                var a = OldTownSwap.Old(g.At - d * (len * .5f + .5f)); var b = OldTownSwap.Old(g.At + d * (len * .5f + .5f)); var nn = new Vector2(-d.y, d.x) * 2.1f;
                Holes.Add("tram", g.Id, new[] { a + nn, b + nn, b - nn, a - nn }, -.5f, 4.5f);
            }
            rep.Log($"tram rails: {pieces} of our rail pieces laid as {tiles} old Tile_TramRail tiles; {onOld} pieces on the old boulevard track left to the old rails (old primary there)");
        }

        public void Trams()
        {
            var root = new GameObject("Trams (old Tram at our stops)").transform; root.SetParent(Root, false);
            var placed = new List<Vector2>();
            void Tram(Vector2 at, Vector2 d, string why)
            {
                if (placed.Any(p => Vector2.Distance(p, at) < 15f)) return;
                Put("Tram", OldTownSwap.Old3(at, 0f), Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, root);
                placed.Add(at);
            }
            foreach (var g in w.Transit.Stops.GroupBy(s => s.Id.Substring(0, s.Id.LastIndexOf('/'))))
            {
                var c = Vector2.zero; int k = 0; foreach (var s in g) foreach (var p in s.Ring) { c += p; k++; } c /= Mathf.Max(1, k);
                if (SwapMesh.Inside(OldTownSwap.ZonePlan, c)) continue;
                var near = rails.OrderBy(r => Vector2.Distance(r.at, c)).FirstOrDefault();
                if (rails.Count == 0 || Vector2.Distance(near.at, c) > 12f) { rep.Log($"tram: no rail of ours by stop {g.Key} (old track there)"); continue; }
                Tram(near.at, near.dir, g.Key);
            }
            foreach (var t in w.Trams) if (t.Kit != null) { float y = float.IsNaN(t.Kit.Yaw) ? 0f : t.Kit.Yaw; Tram(t.Kit.At, new Vector2(-Mathf.Sin(y), Mathf.Cos(y)), t.Id); }  // our Tram piece is the old design: unity yaw = -yaw, as KitPlacer
            int canopies = 0;
            foreach (var it in w.Street.Where(x => x.Kind == "tram-shelter" && !SwapMesh.Inside(OldTownSwap.ZonePlan, x.At)))
            { Put("TramCanopy", OldTownSwap.Old3(it.At, it.Z), -it.Yaw * Mathf.Rad2Deg, root); canopies++; }
            rep.Log($"trams: {placed.Count} old Tram at our stops and parked tram; {canopies} old TramCanopy at our stop shelters outside the zone");
        }
    }
}
#endif
