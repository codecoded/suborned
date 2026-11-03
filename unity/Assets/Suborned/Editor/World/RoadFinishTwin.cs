using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 055 road test (Sky: "go"), opt-in with SUBORNED_ROAD_TWIN: the export's asphalt twin, `bun tools/world/export.ts --road-finish asphalt --out
    /// out/world/harbour-district.road-asphalt.world.json` (Vela's finish set), read beside the export the scene is built from. It is
    /// taken only when it is the same world but for the road finish: every section the same in count, the same roads, circuses and flats
    /// by id, and the ground pieces differing only in what each finish lays (the enamel's lozenges and seams, the asphalt's lines). The
    /// enamel-only pieces bake into the batch layer `road-finish-enamel`, the asphalt-only ones into `road-finish-asphalt` (built off);
    /// RoadFinish (runtime) turns them and swaps the carriageway's material. No twin, or one from another world: the scene is as before.</summary>
    public sealed class RoadFinishTwin
    {
        public const string EnamelLayer = "road-finish-enamel", AsphaltLayer = "road-finish-asphalt";
        public string Path;
        public readonly HashSet<string> EnamelOnly = new();
        public readonly List<StreetItemDto> AsphaltOnly = new();

        public static string Key(StreetItemDto p) => p.Id + "|" + (p.Kit?.Id ?? "");

        public static string TwinPath(string exportPath) => exportPath.EndsWith(".world.json") ? exportPath[..^".world.json".Length] + ".road-asphalt.world.json" : exportPath + ".road-asphalt";

        public static RoadFinishTwin Load(string exportPath, WorldDto w)
        {
            // opt-in (Aster 2026-10-24: the shipped build stays enamel until Sky picks): only when SUBORNED_ROAD_TWIN names the twin ("1" = the
            // default path beside the export); a twin left in out/world/ by a test round is never picked up by another build
            var want = System.Environment.GetEnvironmentVariable("SUBORNED_ROAD_TWIN");
            if (string.IsNullOrEmpty(want)) { Debug.Log("World: road finish twin: off (SUBORNED_ROAD_TWIN unset); the enamel only"); return null; }
            var path = want == "1" ? TwinPath(exportPath) : want;
            if (!File.Exists(path)) { Debug.LogWarning($"World: road finish twin: {path} missing; the enamel only"); return null; }
            WorldDto a;
            try { a = WorldReader.Load(path); }
            catch (System.Exception e) { Debug.LogWarning($"World: road finish twin {path} did not read ({e.Message}); the enamel only"); return null; }
            string why = null;
            if (a.Source != w.Source) why = $"source {a.Source} is not {w.Source}";
            else if (a.Buildings.Count != w.Buildings.Count || a.Street.Count != w.Street.Count || a.Roofs.Count != w.Roofs.Count || a.Frontages.Count != w.Frontages.Count || a.Civic.Count != w.Civic.Count) why = "its sections differ in count";
            else if (!a.Ground.Roads.Select(r => r.Id).SequenceEqual(w.Ground.Roads.Select(r => r.Id)) || !a.Ground.Flats.Select(f => f.Id).SequenceEqual(w.Ground.Flats.Select(f => f.Id))) why = "its roads or flats differ";
            if (why != null) { Debug.LogWarning($"World: road finish twin {path} is another world ({why}); the enamel only"); return null; }
            var t = new RoadFinishTwin { Path = path };
            var mine = new HashSet<string>(w.Ground.Pieces.Select(Key)); var theirs = new HashSet<string>(a.Ground.Pieces.Select(Key));
            foreach (var p in w.Ground.Pieces) if (!theirs.Contains(Key(p))) t.EnamelOnly.Add(Key(p));
            foreach (var p in a.Ground.Pieces) if (!mine.Contains(Key(p))) t.AsphaltOnly.Add(p);
            var kinds = t.AsphaltOnly.Select(p => p.Kit?.Id ?? p.Kind).Distinct().OrderBy(x => x, System.StringComparer.Ordinal);
            Debug.Log($"World: road finish twin {path}: {t.EnamelOnly.Count} enamel-only, {t.AsphaltOnly.Count} asphalt-only pieces ({string.Join(", ", kinds)})");
            return t;
        }

        /// <summary>After Combine.Flush: the RoadFinish switch on the world root, the asphalt's batches off.</summary>
        public static void Install(Transform root, Transform batches)
        {
            Material M(string id) => AssetDatabase.LoadAssetAtPath<Material>($"{Look.KitMaterials}/{id}.mat") ?? throw new InvalidDataException($"World: road finish twin: kit material {id} missing (run `bun run kit`)");
            var rf = root.gameObject.AddComponent<Suborned.RoadFinish>();
            rf.enamel = M("DK_Ground_RoadEnamel"); rf.asphalt = M("DK_Ground_RoadAsphalt");
            rf.enamelOnly = batches.Find(EnamelLayer)?.gameObject;
            var asph = batches.Find(AsphaltLayer) ?? throw new InvalidDataException("World: road finish twin: no asphalt batches were baked");
            rf.asphaltOnly = asph.gameObject; asph.gameObject.SetActive(false);
            Debug.Log($"World: road finish switch installed: enamel-only batches {(rf.enamelOnly ? rf.enamelOnly.transform.childCount : 0)}, asphalt-only batches {asph.childCount} (off)");
        }
    }
}
