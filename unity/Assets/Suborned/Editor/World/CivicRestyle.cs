using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 075 civic restyle (Vela's briefs/075/civic/restyle.json, copied verbatim beside the builder as civic-restyle.json): the
    /// testbed's Records Office, café and square dressed in the old town's materials, visual only. Three kinds of change, nothing else:
    /// - a material slot on a civic kit piece the builder already places (`remap`: merged into the mass's own `materials`, so the placed and
    ///   the baked pieces take it by the export's slot path, KitSlots; a slot the piece lacks fails the build);
    /// - a ground material on two plots and the bed-kerb masses (`ground`, `surfaces`: a surface added to the look in memory, look.json untouched);
    /// - scene-only overlays with no collider (`overlays`: kit prefabs under the café's element, so they take its fade; never counted as
    ///   export pieces, LOD0 only so the play-zoom guard never sees a small piece switching below L0).
    /// Footprints, heights, doors, mission points, bays, cover, colliders and walkable surfaces are not touched: no ring, base, height or
    /// position is read for anything but the overlays. `-civicRestyle off` builds the scene as before (the before | after pairs);
    /// `-civicRestyle &lt;path&gt;` (repo-relative) reads another restyle file.</summary>
    public sealed class CivicRestyle
    {
        public const string Schema = "vela.civic-restyle/1";
        /// <summary>The café's element: the overlays hang under it (its fade, Vela's "parented under civic/cafe").</summary>
        public const string OverlayHost = "civic/cafe";
        const string MaterialsRoot = "Kit/Materials/";

        public readonly string Path, Sha256;
        readonly List<(HashSet<string> pieces, string from, string to)> remap = new();
        readonly List<(string plot, string to, float tile)> ground = new();
        readonly List<(HashSet<string> ids, string to)> surfaces = new();
        readonly List<(string id, string piece, Vector2 at, float z, float yaw, float scale)> overlays = new();
        public int Slots, Plots, Kerbs, Overlays;

        /// <summary>The restyle this build applies (the -civicRestyle switch), or null for "off".</summary>
        public static CivicRestyle Load()
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-civicRestyle");
            string v = i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
            if (v == "off") { Debug.Log("World: civic restyle off (-civicRestyle off)"); return null; }
            var path = v == null ? System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Assets/Suborned/Editor/World/civic-restyle.json"))
                                 : System.IO.Path.GetFullPath(System.IO.Path.Combine(HarbourWorldBuilder.RepoRoot, v));
            return new CivicRestyle(path);
        }

        CivicRestyle(string path)
        {
            Path = path;
            var bytes = File.ReadAllBytes(path);
            using (var sha = SHA256.Create()) Sha256 = string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
            var o = Json.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            if (J.S(o, "schema") != Schema) throw new InvalidDataException($"World: civic restyle {path}: schema `{J.S(o, "schema")}`, want {Schema}");
            foreach (var r in J.A(o, "remap"))
                remap.Add((new HashSet<string>(J.A(r, "pieces").Cast<string>(), StringComparer.Ordinal), J.S(r, "from"), Mat(J.S(r, "to"), path)));
            foreach (var g in J.A(o, "ground"))
                ground.Add((J.S(g, "plot"), Mat(J.S(g, "to"), path), J.F(g, "tile")));
            foreach (var s in J.A(o, "surfaces"))
                surfaces.Add((new HashSet<string>(J.A(s, "ids").Cast<string>(), StringComparer.Ordinal), Mat(J.S(s, "to"), path)));
            foreach (var ov in J.A(o, "overlays"))
            {
                if (J.Get(ov, "collider") is not false) throw new InvalidDataException($"World: civic restyle overlay {J.S(ov, "id")}: only collider-free overlays are visual only");
                overlays.Add((J.S(ov, "id"), J.S(ov, "piece"), J.P(J.Get(ov, "at")), J.F(ov, "z"), J.F(ov, "yaw"), J.Get(ov, "scale") is double sc ? (float)sc : 1f));
            }
        }

        /// <summary>A `to` path (Kit/Materials/&lt;name&gt;.mat under unity/Assets/Suborned) as the kit material's name; it must exist.</summary>
        static string Mat(string to, string path)
        {
            if (to == null || !to.StartsWith(MaterialsRoot, StringComparison.Ordinal) || !to.EndsWith(".mat", StringComparison.Ordinal))
                throw new InvalidDataException($"World: civic restyle {path}: material `{to}` is not a kit material (Kit/Materials/*.mat)");
            var name = System.IO.Path.GetFileNameWithoutExtension(to);
            if (!AssetDatabase.LoadAssetAtPath<Material>($"{Look.KitMaterials}/{name}.mat")) throw new FileNotFoundException($"World: civic restyle: kit material {name} missing in {Look.KitMaterials} (run `bun run kit`)");
            return name;
        }

        static string SurfaceKey(string material, float tile, bool box) => $"civic-restyle/{material}@{tile:0.###}{(box ? "/box" : "")}";

        /// <summary>Before the ground and civic layers are built: the slots into the civic masses' `materials`, the two plots and the bed kerbs onto
        /// surfaces of their new material. Only material names and surface keys change; every rule must find what it names.</summary>
        public string Prepare(WorldDto w, Look look)
        {
            var hit = new int[remap.Count];
            foreach (var m in w.Civic)
            {
                if (m.Kit == null) continue;
                for (int i = 0; i < remap.Count; i++)
                {
                    var (pieces, from, to) = remap[i];
                    if (!pieces.Contains(m.Kit.Id)) continue;
                    m.Materials ??= new Dictionary<string, string>(StringComparer.Ordinal);
                    if (m.Materials.TryGetValue(from, out var had) && had != to) throw new InvalidDataException($"World: civic restyle: {m.Id} already draws `{from}` in {had} (export), the restyle asks {to}");
                    m.Materials[from] = to; hit[i]++; Slots++;
                }
            }
            for (int i = 0; i < remap.Count; i++) if (hit[i] == 0) throw new InvalidDataException($"World: civic restyle: no civic kit piece of [{string.Join(", ", remap[i].pieces)}] in the export");
            foreach (var (plot, to, tile) in ground)
            {
                var p = w.Ground.Plots.FirstOrDefault(x => x.Id == plot) ?? throw new InvalidDataException($"World: civic restyle: plot {plot} not in the export");
                var key = SurfaceKey(to, tile, false); look.AddSurface(key, to, tile, false);
                p.Surface = key; Plots++;
            }
            foreach (var (ids, to) in surfaces)
            {
                var key = SurfaceKey(to, 1f, true); look.AddSurface(key, to, 1f, true); // the kit meshes' world metres, walls along their face
                foreach (var id in ids)
                {
                    var m = w.Civic.FirstOrDefault(x => x.Id == id) ?? throw new InvalidDataException($"World: civic restyle: civic mass {id} not in the export");
                    if (m.Kit != null) throw new InvalidDataException($"World: civic restyle: {id} is a kit piece, not a builder surface");
                    m.Surface = key; Kerbs++;
                }
            }
            return $"World: civic restyle {System.IO.Path.GetFileName(Path)} sha256 {Sha256}: {Slots} kit slots on {hit.Sum()} placements ({string.Join("; ", remap.Select((r, i) => $"{r.from}->{r.to} on {hit[i]}"))}), {Plots} plots, {Kerbs} bed kerbs";
        }

        /// <summary>The overlays under the café's element (after the civic layer, before the fade tags): kit prefab instances, no collider,
        /// LOD0 only, not counted by the placer (they are not export pieces, so Inspect's kit counts stay the export's).</summary>
        public string PlaceOverlays(Transform civic, KitPlacer placer)
        {
            if (overlays.Count == 0) return "World: civic restyle overlays: none";
            Transform host = null; foreach (Transform t in civic) if (t.name == OverlayHost) host = t;
            if (!host) throw new InvalidDataException($"World: civic restyle: overlay host {OverlayHost} was not built");
            var lines = new List<string>();
            foreach (var (id, piece, at, z, yaw, scale) in overlays)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(placer.Prefab(piece), host);
                go.name = id;
                go.transform.SetPositionAndRotation(new Vector3(at.x, z, at.y), Quaternion.Euler(0f, -yaw * Mathf.Rad2Deg, 0f));
                go.transform.localScale = Vector3.one * scale;
                int stripped = 0;
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) { UnityEngine.Object.DestroyImmediate(c); stripped++; }
                // LOD0 only: the lower levels off, the group's cull height kept (a 0.8 m piece would switch to L1 a few metres out, below the play zoom)
                int off = 0;
                if (go.GetComponentInChildren<LODGroup>(true) is { } lg)
                {
                    var lods = lg.GetLODs();
                    var keep = lods[0].renderers.Where(r => r).ToArray();
                    foreach (var r in lods.Skip(1).SelectMany(l => l.renderers).Where(r => r && !keep.Contains(r))) { r.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(r); off++; }
                    lg.SetLODs(new[] { new LOD(lods[lods.Length - 1].screenRelativeTransitionHeight, keep) });
                    PrefabUtility.RecordPrefabInstancePropertyModifications(lg);
                }
                var b = KitLods.Lod0(go).Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                if (go.GetComponentsInChildren<Collider>(true).Length > 0) throw new InvalidDataException($"World: civic restyle overlay {id} still has a collider");
                Overlays++;
                lines.Add($"{id} {piece} at ({at.x:0.##}, {at.y:0.##}) yaw {yaw:0.####}: {b.min.y - z:0.00}-{b.max.y - z:0.00} m above its base, {stripped} colliders stripped, {off} lower-LOD renderers off");
            }
            return $"World: civic restyle overlays under {OverlayHost} ({Overlays}): {string.Join("; ", lines)}";
        }
    }
}
