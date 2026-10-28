#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Suborned.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap: what of our pipeline's scene goes into the old-built town. Our scene (HarbourWorldBuilder from the pinned
    /// export, plan frame) is opened beside the old one and cut in place: everything in the Handover zone stays exactly as built (flat
    /// ground clipped exactly at the zone's edge; a solid stays whole if its centre is in the zone), and outside the zone only the network
    /// stays (our stations and their banks, stairs, lifts and mezzanines, the Skyport station, the tram platforms, the crossing parts,
    /// the ocean liner, SpaceHQ_Portal for the "ours" switch). The rest of our district is dropped: the old builders are the look outside the
    /// zone. Sky track beams, pylons and tram rails are drawn again in the old pieces (SwapNetwork). The kept roots move into the old scene
    /// under one root at z +206 (plan to old frame). Kit materials the superseded palette edits changed on disk are swapped for HEAD's
    /// (out/scratch/latch/075swap/headmats, regenerated in a HEAD worktree and checked by sha256 against HEAD's kit.json).</summary>
    internal sealed class SwapOurs
    {
        public Transform Root;
        public GameObject SpaceHq;
        /// <summary>Kept solids, old frame: XZ box, height band, id (each a hole the old gives way to).</summary>
        public readonly List<(Rect box, float y0, float y1, string id)> Solids = new();
        readonly SwapShapes net = new(), exclude = new();
        readonly Dictionary<SwapShapes.Shape, float> maxExtent = new(), minTop = new();
        Rect hqRect;
        int keptUnits, droppedUnits, keptComps, droppedComps, clippedComps;
        readonly Dictionary<string, int> keptBy = new();

        static readonly string[] SkipRoots = { "cameras", "lighting", "sea", "source" };

        public static SwapOurs Import(OldTownSwap.Report rep, WorldDto w, string scenePath, string hq)
        {
            var o = new SwapOurs();
            o.Shapes(w);
            var target = SceneManager.GetActiveScene();
            var heads = HeadMats(rep);
            var s = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            var stamp = s.GetRootGameObjects().Select(g => g.GetComponent<WorldStamp>()).FirstOrDefault(x => x);
            rep.Log($"ours: {scenePath} built from {(stamp ? stamp.exportPath : "?")} sha256 {(stamp ? stamp.exportSha256 : "?")}");
            if (stamp && stamp.exportSha256 != w.Sha256) throw new InvalidDataException($"[Swap] {scenePath} was built from export {stamp.exportSha256}, not the pinned {w.Sha256}");
            foreach (var r in s.GetRootGameObjects()) if (Array.IndexOf(SkipRoots, r.name) >= 0) UnityEngine.Object.DestroyImmediate(r);
            foreach (var r in s.GetRootGameObjects()) { var hz = r.transform.Find("horizon"); if (hz) UnityEngine.Object.DestroyImmediate(hz.gameObject); }
            int unpacked = 0;
            foreach (var r in s.GetRootGameObjects())
                foreach (var t in r.GetComponentsInChildren<Transform>(true).Where(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToArray())
                { PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); unpacked++; }
            rep.Log($"ours: {unpacked} kit instances unpacked in the probe scene's copy in memory (so the cut can take parts of them; the probe scene is not saved)");
            o.Filter(s);
            o.FindHq(s);
            var root = new GameObject("Ours (Handover zone + network, our pipeline)");
            SceneManager.MoveGameObjectToScene(root, target);
            root.transform.position = new Vector3(0f, 0f, OldTownSwap.DZ);
            foreach (var r in s.GetRootGameObjects()) { SceneManager.MoveGameObjectToScene(r, target); r.transform.SetParent(root.transform, false); }
            EditorSceneManager.CloseScene(s, true);
            o.Root = root.transform;
            int swapped = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials; bool ch = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] && heads.TryGetValue(AssetDatabase.GetAssetPath(mats[i]), out var h)) { mats[i] = h; ch = true; swapped++; }
                if (ch) r.sharedMaterials = mats;
            }
            rep.Log($"ours kept: {o.keptUnits} objects ({string.Join(", ", o.keptBy.OrderBy(k => k.Key).Select(k => $"{k.Key} {k.Value}"))}), dropped {o.droppedUnits}; batched meshes: {o.keptComps} parts kept, {o.clippedComps} clipped at the zone edge, {o.droppedComps} dropped; {o.Solids.Count} solids the old gives way to; HEAD material slots {swapped} ({heads.Count} materials)");
            rep.Log(o.SpaceHq ? $"ours SpaceHQ_Portal found ({o.SpaceHq.name})" : "ours SpaceHQ_Portal NOT found");
            return o;
        }

        /// <summary>HEAD's versions of the kit materials the superseded edits rewrote: written once into OldTownArt/Swap/HeadMats (git-ignored) with
        /// their own guids, keyed by the Kit material path they replace.</summary>
        static Dictionary<string, Material> HeadMats(OldTownSwap.Report rep)
        {
            var map = new Dictionary<string, Material>();
            string src = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")), "out/scratch/latch/075swap/headmats");
            var okFile = Path.Combine(src, "ok.json");
            if (!File.Exists(okFile)) { rep.Log("HEAD materials: none (no headmats/ok.json): our kit materials as on disk"); return map; }
            var names = Regex.Matches(File.ReadAllText(okFile), "\"Materials/([^\"]+)\\.mat\"").Select(m => m.Groups[1].Value).ToList();
            const string dir = "Assets/Suborned/OldTownArt/Swap/HeadMats";
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", dir)));
            foreach (var n in names)
            {
                var text = File.ReadAllText(Path.Combine(src, "Materials", n + ".mat"));
                var guid = Hash("suborned.oldtown.headmat/" + n);
                var dst = Path.GetFullPath(Path.Combine(Application.dataPath, "..", dir, n + ".mat"));
                File.WriteAllText(dst, text);
                File.WriteAllText(dst + ".meta", $"fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n");
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var n in names)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"{dir}/{n}.mat");
                if (m) map[$"Assets/Suborned/Kit/Materials/{n}.mat"] = m;
            }
            rep.Log($"HEAD materials: {map.Count} of {names.Count} ({string.Join(", ", names)})");
            return map;
        }

        static string Hash(string s) { using var md5 = System.Security.Cryptography.MD5.Create(); return string.Concat(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s)).Select(b => b.ToString("x2"))); }

        // ---------------------------------------------------------------- what counts as network (plan frame)
        void Shapes(WorldDto w)
        {
            SwapShapes.Shape Add(SwapShapes.Shape s, float ext = 60f) { if (s != null) maxExtent[s] = ext; return s; }
            foreach (var st in w.Transit.Stations) Add(net.Add("station", st.Id, Grow(st.Ring, 1.5f)));
            foreach (var g in w.Transit.Stations.GroupBy(st => st.Id.Substring(0, st.Id.LastIndexOf('/'))))  // the station piece over both platforms
                minTop[Add(net.Rect("station-piece", g.Key, Box(g.SelectMany(x => x.Ring)), 6f), 80f)] = 12f;  // the elevated station only, not the street under it
            foreach (var st in w.Transit.Stops) Add(net.Add("stop", st.Id, Grow(st.Ring, .5f)));
            foreach (var c in w.Connectors.Where(c => c.Id.StartsWith("bank/")))
            {
                var pts = new List<Vector2>(c.Ring ?? new Vector2[0]);
                foreach (var d in c.Dress) { float r = Mathf.Max(d.Size.x, d.Size.y) * .5f; pts.Add(d.Centre + new Vector2(r, r)); pts.Add(d.Centre - new Vector2(r, r)); }
                if (pts.Count > 0) Add(net.Rect("connector", c.Id, Box(pts), .5f));
            }
            foreach (var l in w.Levels) foreach (var d in l.Decks.Where(d => d.Id.StartsWith("bank/"))) Add(net.Add("deck", d.Id, Grow(d.Ring, .5f)));
            foreach (var it in w.Street)
            {
                float r = Mathf.Max(it.Size.x, it.Size.y) * .5f + 1f;
                if (it.Id.StartsWith("street/bank/") || it.Id.StartsWith("street/station/")) net.Circle("station-item", it.Id, it.At, r);
                else if (!string.IsNullOrEmpty(it.Crossing)) net.Circle("crossing", it.Id, it.At, Mathf.Max(r, 1.5f));
                if (it.Kind == "tram-shelter" && !SwapMesh.Inside(OldTownSwap.ZonePlan, it.At)) exclude.Circle("tram-shelter (old TramCanopy instead)", it.Id, it.At, 4f);
            }
            foreach (var sig in w.Signatures)
            {
                if (sig.Ground == null || sig.Ground.Length < 4) continue;
                var r = Rect.MinMaxRect(sig.Ground[0], sig.Ground[2], sig.Ground[1], sig.Ground[3]);
                if (sig.Id == "signature/ocean-liner") Add(net.Rect("liner", sig.Id, r, 2f), 400f);
                if (sig.Id == "signature/space-hq") { Add(net.Rect("hq", sig.Id, r, 2f), 400f); hqRect = r; }
            }
            foreach (var p in w.Transit.SkyTrack.Pylons) exclude.Circle("pylon (old pieces instead)", p.Id, p.At, 3f);
        }

        static Vector2[] Grow(Vector2[] ring, float g)
        {
            if (ring == null || ring.Length < 3) return ring;
            var c = Vector2.zero; foreach (var p in ring) c += p; c /= ring.Length;
            return ring.Select(p => p + (p - c).normalized * g).ToArray();
        }
        static Rect Box(IEnumerable<Vector2> pts)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var p in pts) { x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y); }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        static readonly string[] NeverOutside = { "SkyRail_Pylon", "SkyRail_Beam", "Tile_TramRail", "TramCanopy_Play", "TramWire" };

        /// <summary>Kept outside the zone: the network shape the point falls in (and the part is no wider than it allows), unless an exclusion holds it.</summary>
        SwapShapes.Shape Network(Vector2 c, float extent, string name, float top)
        {
            if (name != null && NeverOutside.Any(n => name.Contains(n))) return null;
            if (exclude.Hit(c) != null) return null;
            var s = net.Near(new Rect(c, Vector2.zero)).FirstOrDefault(x => x.Contains(c) && top >= (minTop.TryGetValue(x, out var mt) ? mt : float.NegativeInfinity));
            return s != null && extent <= (maxExtent.TryGetValue(s, out var mx) ? mx : 60f) ? s : null;
        }

        void Keep(string kind) { keptBy[kind] = keptBy.TryGetValue(kind, out var n) ? n + 1 : 1; }
        void Solid(Bounds b, string id)
        {
            if (b.size.y < .4f || id.StartsWith("hq:")) return;  // the HQ portal is a switch, not a hole: the old HQ and its plaza stay for "old"
            Solids.Add((new Rect(b.min.x, b.min.z + OldTownSwap.DZ, b.size.x, b.size.z), b.min.y, b.max.y, id));
        }

        void Filter(Scene s)
        {
            var roots = s.GetRootGameObjects();
            var lods = roots.SelectMany(r => r.GetComponentsInChildren<LODGroup>(true)).ToList();
            var underLod = new HashSet<Renderer>(lods.SelectMany(l => l.GetLODs().SelectMany(x => x.renderers)).Where(r => r));
            foreach (var g in lods)
            {
                var rs = g.GetLODs().SelectMany(x => x.renderers).Where(r => r).ToArray();
                if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                Unit(g.gameObject, b, true);
            }
            foreach (var r in roots.SelectMany(x => x.GetComponentsInChildren<Renderer>(true)).ToArray())
            {
                if (!r || underLod.Contains(r)) continue;
                if (r is not MeshRenderer && r is not SkinnedMeshRenderer) continue;
                var mf = r.GetComponent<MeshFilter>();
                var b = r.bounds;
                if (mf && mf.sharedMesh && Mathf.Max(b.size.x, b.size.z) > 40f) Parts(r, mf);
                else Unit(r.gameObject, b, false);
            }
        }

        void Unit(GameObject go, Bounds b, bool lod)
        {
            var c = new Vector2(b.center.x, b.center.z);
            float ext = Mathf.Max(b.size.x, b.size.z);
            // the zone keeps what reaches into it whole (zoneWhy: "every plot that reaches into it keeps the massing it has"); flat pieces by their centre
            bool inZone = b.size.y < .6f ? SwapMesh.Inside(OldTownSwap.ZonePlan, c) : SwapMesh.Overlaps(SwapMesh.XZ(b), OldTownSwap.ZonePlan, .05f);
            if (inZone) { keptUnits++; Keep("zone"); Solid(b, "zone:" + go.name); return; }
            var n = Network(c, ext, go.name, b.max.y);
            if (n != null) { keptUnits++; Keep(n.Kind); Solid(b, $"{n.Kind}:{n.Id}"); return; }
            droppedUnits++;
            if (lod || go.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(go);
            else { var mr = go.GetComponent<Renderer>(); var mf = go.GetComponent<MeshFilter>(); if (mr) UnityEngine.Object.DestroyImmediate(mr); if (mf) UnityEngine.Object.DestroyImmediate(mf); }
        }

        void Parts(Renderer r, MeshFilter mf)
        {
            var mesh = mf.sharedMesh;
            var rd = new SwapMesh.Read(mesh, r.transform.localToWorldMatrix);
            var ids = rd.Components(out int count);
            var cb = new Bounds[count]; var has = new bool[count];
            for (int s = 0; s < rd.Subs.Count; s++)
                for (int t = 0; t < ids[s].Length; t++)
                {
                    int k = ids[s][t];
                    for (int j = 0; j < 3; j++) { var p = rd.Verts[rd.Subs[s][3 * t + j]].W; if (!has[k]) { cb[k] = new Bounds(p, Vector3.zero); has[k] = true; } else cb[k].Encapsulate(p); }
                }
            // per component: 0 drop, 1 keep, 2 clip to inside the zone
            var act = new int[count];
            var zone = OldTownSwap.ZonePlan;
            for (int k = 0; k < count; k++)
            {
                var b = cb[k]; var box = SwapMesh.XZ(b); var c = new Vector2(b.center.x, b.center.z);
                bool flat = b.size.y < .6f;
                if (box.xMin >= zone.xMin && box.xMax <= zone.xMax && box.yMin >= zone.yMin && box.yMax <= zone.yMax) { act[k] = 1; Solid(b, "zone:" + mesh.name); continue; }
                if (flat && SwapMesh.Overlaps(box, zone)) { act[k] = 2; continue; }
                if (!flat && SwapMesh.Overlaps(box, zone, .05f)) { act[k] = 1; Solid(b, "zone:" + mesh.name); continue; }
                var n = Network(c, Mathf.Max(b.size.x, b.size.z), r.name, b.max.y);
                if (n != null) { act[k] = 1; Keep(n.Kind); Solid(b, $"{n.Kind}:{n.Id}"); }
            }
            var w = new SwapMesh.Write(rd);
            var inside = SwapMesh.Inside(zone);
            for (int s = 0; s < rd.Subs.Count; s++)
                for (int t = 0; t < ids[s].Length; t++)
                {
                    int a = rd.Subs[s][3 * t], b = rd.Subs[s][3 * t + 1], c = rd.Subs[s][3 * t + 2], k = ids[s][t];
                    if (act[k] == 1) w.Keep(s, a, b, c);
                    else if (act[k] == 2) SwapMesh.ClipInto(w, s, rd.Verts[a], rd.Verts[b], rd.Verts[c], inside);
                }
            for (int k = 0; k < count; k++) { if (act[k] == 1) keptComps++; else if (act[k] == 2) clippedComps++; else droppedComps++; }
            if (w.Triangles == 0) { UnityEngine.Object.DestroyImmediate(r); UnityEngine.Object.DestroyImmediate(mf); return; }
            if (act.All(x => x == 1)) return;  // untouched
            var m = w.Mesh(mesh.name + " (swap)");
            mf.sharedMesh = m;
            var col = r.GetComponent<MeshCollider>(); if (col) col.sharedMesh = m;
        }

        /// <summary>SpaceHQ_Portal: the tall renderers standing on signature/space-hq's ground, gathered under one object so the switch can turn it off.</summary>
        void FindHq(Scene s)
        {
            if (hqRect.width <= 0f) return;
            var rs = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Renderer>(true))
                .Where(r => r && SwapMesh.Inside(hqRect, new Vector2(r.bounds.center.x, r.bounds.center.z)) && r.bounds.max.y > 120f).ToList();
            if (rs.Count == 0) return;
            var units = rs.Select(r => (r.GetComponentInParent<LODGroup>() is { } g ? g.transform : r.transform)).Distinct().ToList();
            var hq = new GameObject("Ours SpaceHQ_Portal (signature/space-hq)");
            SceneManager.MoveGameObjectToScene(hq, s);
            foreach (var u in units) u.SetParent(hq.transform, true);
            SpaceHq = hq;
        }
    }
}
#endif
