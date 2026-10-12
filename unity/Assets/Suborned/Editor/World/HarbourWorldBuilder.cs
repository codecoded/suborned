using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Builds Assets/Scenes/HarbourDistrict.unity (generated, git-ignored) from a world export, from scratch,
    /// one marker root per layer (ground, transit, buildings, roofs, frontages, connectors, street, levels, decks,
    /// megastructures, civic, gates, landmarks) and their geometry combined under `batches` per layer, material and tile.
    /// Menu: Suborned/World/Build Harbour District (Sample). Batchmode:
    ///   Unity -batchmode -nographics -quit -projectPath unity -executeMethod Suborned.World.HarbourWorldBuilder.Build
    ///         [-world &lt;path&gt;]   (relative to the repo root; default the hand-made sample under Samples/)</summary>
    public static class HarbourWorldBuilder
    {
        /// <summary>The generated scene; `-scene Assets/Scenes/<name>.unity` builds and shoots another file (a probe, so it never races the default scene).</summary>
        public static string ScenePath { get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-scene"); return i >= 0 && i + 1 < a.Length ? a[i + 1] : "Assets/Scenes/HarbourDistrict.unity"; } }
        public const string RootName = "HarbourDistrict";
        const string Here = "Assets/Suborned/Editor/World";

        public static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        static string InUnity(string assetPath) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        public static string SamplePath => InUnity(Here + "/Samples/sample.world.json");
        public static string StylePath => Path.Combine(RepoRoot, "contracts", "style", "suborned.style.json");
        public static string LookPath => InUnity(Here + "/look.json");
        public static string ViewsPath => Arg("-views", InUnity(Here + "/views.json"));
        public static string FacadePath => InUnity(Here + "/facade.json");
        public static string KitPath => InUnity(Here + "/kit.json");
        public static string KitCataloguePath => Path.Combine(RepoRoot, "contracts", "world", "kit.json");

        public static string Arg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? Path.GetFullPath(Path.Combine(RepoRoot, args[i + 1])) : fallback;
        }

        [MenuItem("Suborned/World/Build Harbour District (Sample)")]
        public static void BuildMenu() => Batch(() => { if (MayReplaceScene()) BuildFrom(SamplePath); });

        /// <summary>Batchmode entry: -world &lt;path&gt; or the sample.</summary>
        public static void Build() => Batch(() => BuildFrom(Arg("-world", SamplePath)));

        public static bool MayReplaceScene() =>
            Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        /// <summary>Runs a step; in batchmode any exception exits 1 instead of leaving a half-built scene.</summary>
        public static void Batch(Action step)
        {
            try { step(); }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static void BuildFrom(string path)
        {
            var t0 = DateTime.UtcNow;
            var world = WorldReader.Load(path);
            var look = new Look(StylePath, LookPath);
            var kit = new Catalogue(KitPath);
            var nodes = new Nodes(look);
            var layers = new Layers(nodes);
            var placer = new KitPlacer(KitCataloguePath);
            KitImport.Generated(placer);
            KitImport.Check(placer);
            var city = new CityLayers(nodes, kit, placer);
            Poly.Fallbacks = 0; Poly.Repairs = 0; Coplanar.Reset(); Combine.Reset(kit.BatchTile);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var facade = new FacadeKit(nodes, new FacadeSpec(FacadePath, look, world), world); // after NewScene: it would destroy the atlas materials
            ProcKit.Init(world, facade);
            var standIns = new StandIns(placer, world);
            var shops = new ShopKit(nodes, facade);
            var roofs = new RoofKit(nodes, facade);
            var megas = new MegaKit(nodes, facade, world);
            var root = Nodes.Group(null, RootName);
            layers.Ground(Nodes.Group(root, "ground"), world.Ground);
            layers.Transit(Nodes.Group(root, "transit"), world.Transit);
            facade.Buildings(Nodes.Group(root, "buildings"), world.Buildings, standIns);
            roofs.Roofs(Nodes.Group(root, "roofs"), world.Roofs, placer, standIns);
            var frontages = Nodes.Group(root, "frontages");
            shops.Frontages(frontages, world.Frontages);
            placer.Signs(frontages, world.Frontages);
            city.Connectors(Nodes.Group(root, "connectors"), world.Connectors);
            city.Street(Nodes.Group(root, "street"), world.Street);
            layers.Levels(Nodes.Group(root, "levels"), Nodes.Group(root, "decks"), world.Levels);
            megas.Megastructures(Nodes.Group(root, "megastructures"), world.Megastructures, placer, standIns);
            facade.Civic(Nodes.Group(root, "civic"), Coplanar.Masses(world.Civic));
            megas.Gates(Nodes.Group(root, "gates"), world.Gates);
            layers.Landmarks(Nodes.Group(root, "landmarks"), world.Landmarks, placer);
            var batches = Nodes.Group(null, "batches");
            long verts = Combine.Flush(batches);
            int renderers = batches.GetComponentsInChildren<MeshRenderer>().Length;
            var cameras = Nodes.Group(null, "cameras");
            Rig.Build(cameras, Nodes.Group(null, "lighting"), root, world, Views.Load(ViewsPath), look);
            ReflectionCube.Build();
            ProcKit.Report(world, facade);
            Nodes.Group(Nodes.Group(null, "source"), world.Source ?? Path.GetFileName(path));

            // Verify before saving: a scene that disagrees with the export is never written.
            Debug.Log(placer.Summary());
            if (Inspect.Verify(root, world) + Inspect.VerifyKit(root, world, placer, standIns) > 0) throw new InvalidDataException("World: layer counts differ from the export");
            Directory.CreateDirectory(Path.GetDirectoryName(InUnity(ScenePath)));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException($"World: could not save {ScenePath}");
            Debug.Log($"World: built {ScenePath} from {path} ({world.Schema}) in {(DateTime.UtcNow - t0).TotalSeconds:F2}s; " +
                $"{renderers} batch renderers, {verts} vertices; {Coplanar.Faces} coplanar faces inset; fingerprint {Inspect.Fingerprint(root, batches, cameras)}");
        }
    }
}
