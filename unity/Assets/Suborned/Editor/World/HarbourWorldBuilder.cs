using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Builds Assets/Scenes/HarbourDistrict.unity (generated, git-ignored) from a world export, from scratch,
    /// one marker root per layer (ground, transit, buildings, skins, fabric, facades, roofs, frontages, connectors, street, levels, decks,
    /// megastructures, civic, gates, landmarks, signatures, horizon, and crossings when the export has them; the sea sectors, land and banks under `sea`) and their geometry combined under `batches` per layer, material and tile.
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
        public static string LightPath => InUnity(Here + "/light.json");
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

        /// <summary>Builds and saves the scene. A build that fails leaves an empty scene, not its half-built one open and unsaved:
        /// the next build would stop on Unity's "Scene(s) Have Been Modified" dialog, which nothing driving the Editor from outside
        /// can answer (the main thread waits in it).</summary>
        public static void BuildFrom(string path)
        {
            try { Construct(path); }
            catch
            {
                if (!Application.isBatchMode) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                throw;
            }
        }

        static void Construct(string path)
        {
            var t0 = DateTime.UtcNow;
            var world = WorldReader.Load(path);
            // an empty scene first: KitLods rewrites the placed prefabs, which must not be live in the open scene (thousands of instances)
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var look = new Look(StylePath, LookPath);
            var kit = new Catalogue(KitPath);
            var nodes = new Nodes(look);
            var layers = new Layers(nodes);
            var placer = new KitPlacer(KitCataloguePath);
            KitImport.Generated(placer);
            KitImport.Check(placer);
            KitLods.Build(placer);
            KitCover.Init(world, placer);
            var city = new CityLayers(nodes, kit, placer);
            Poly.Fallbacks = 0; Poly.Repairs = 0; Coplanar.Reset(); Combine.Reset(kit.BatchTile, kit.FadeCell, FadeTags.Layer);

            var facade = new FacadeKit(nodes, new FacadeSpec(FacadePath, look, world), world); // after NewScene: it would destroy the atlas materials
            ProcKit.Init(world, facade);
            var standIns = new StandIns(placer, world);
            var shops = new ShopKit(nodes, facade);
            var roofs = new RoofKit(nodes, facade);
            var megas = new MegaKit(nodes, facade, world);
            var skyline = new SkylineKit(placer, look);
            var root = Nodes.Group(null, RootName);
            var stamp = root.gameObject.AddComponent<WorldStamp>(); // what this scene was built from (the walk tool reads it)
            stamp.source = world.Source; stamp.exportSha256 = world.Sha256;
            stamp.exportPath = Path.GetRelativePath(RepoRoot, Path.GetFullPath(path)).Replace('\\', '/');
            layers.Ground(Nodes.Group(root, "ground"), world.Ground);
            layers.Transit(Nodes.Group(root, "transit"), world.Transit);
            var buildings = Nodes.Group(root, "buildings");
            ProcSkin.Elements(Nodes.Group(root, "skins"), world.Skins);
            facade.Buildings(buildings, world.Buildings, standIns);
            ProcSkin.FinsAll(nodes, facade.Spec.Atlas);
            ProcSkin.Finish(buildings);
            new FabricKit(nodes, facade.Spec.Atlas).Build(Nodes.Group(root, "fabric"), world.Fabric);
            Debug.Log($"World: skins {world.Skins.Count}: {ProcSkin.Matched} walls matched to a face, {ProcSkin.Unmatched} by the grammar alone (facets, unskinned walls), {ProcSkin.Cells} bay cells; colour life: {ProcSkin.Panels} porthole panels, {ProcSkin.Spandrels} spandrels, {ProcSkin.BalconyFronts} balcony fronts, {ProcSkin.Fins} fins");
            skyline.Facades(Nodes.Group(root, "facades"), world.Facades);
            roofs.Roofs(Nodes.Group(root, "roofs"), world.Roofs, placer, standIns);
            var frontages = Nodes.Group(root, "frontages");
            shops.Frontages(frontages, world.Frontages);
            placer.Signs(frontages, world.Frontages);
            city.Connectors(Nodes.Group(root, "connectors"), world.Connectors);
            var street = Nodes.Group(root, "street");
            city.Street(street, world.Street);
            if (world.Crossings != null) new CrossingKit(nodes, kit).Build(Nodes.Group(root, "crossings"), street, world);
            layers.Levels(Nodes.Group(root, "levels"), Nodes.Group(root, "decks"), world.Levels);
            megas.Megastructures(Nodes.Group(root, "megastructures"), world.Megastructures, placer, standIns);
            facade.Civic(Nodes.Group(root, "civic"), Coplanar.Masses(world.Civic), placer);
            Debug.Log($"World: civic monument plinths {facade.Plinths}");
            megas.Gates(Nodes.Group(root, "gates"), world.Gates);
            layers.Landmarks(Nodes.Group(root, "landmarks"), world.Landmarks, placer);
            skyline.Signatures(Nodes.Group(root, "signatures"), world.Signatures, nodes);
            var batches = Nodes.Group(null, "batches");
            long verts = Combine.Flush(batches);
            int renderers = batches.GetComponentsInChildren<MeshRenderer>().Length;
            var cameras = Nodes.Group(null, "cameras");
            var lighting = Nodes.Group(null, "lighting");
            Rig.Build(cameras, lighting, root, world, Views.Load(ViewsPath), look);
            skyline.Horizon(Nodes.Group(root, "horizon"), Nodes.Group(null, "sea"), world.Horizon, world.Ground); // after Rig: views frame the district, not the 16 km band
            ReflectionCube.Build(look);
            var flagged = FadeTags.Elements(root.Find("civic"), world.Civic.Where(m => m.Fade).Select(m => m.Id));
            var (fadeKit, fadeBatches, fadeFlagged) = FadeTags.Apply(root, batches, facade.Spec.Atlas, flagged);
            Debug.Log($"World: architecture fade may thin {fadeKit} kit renderers and {fadeBatches} of {renderers} batch renderers (rendering-layer bit {ArchitectureFade.Layer}); " +
                $"of the kit renderers {fadeFlagged} are on {flagged.Count} civic pieces the export flags `fade`");
            var (proxies, proxyTris) = ShadowProxies.Build(root, kit.ShadowProxies); // after FadeTags: proxies take their LOD1's fade bit
            Debug.Log($"World: shadow proxies {proxies} ({proxyTris} LOD1 triangles), {(kit.ShadowProxies ? "on: kit facades and street trees cast from LOD1, no LOD of theirs casts" : "built off (kit.json shadowProxies): every LOD casts as before; the perf harness switches them")}");
            ProcKit.Report(world, facade);
            Nodes.Group(Nodes.Group(null, "source"), world.Source ?? Path.GetFileName(path));
            Play.PlayInstall.Install(world); // the live Handover host (brief 011 D): its own root, `play`

            // Verify before saving: a scene that disagrees with the export is never written.
            Debug.Log(placer.Summary());
            if (Inspect.Verify(root, world) + Inspect.VerifyKit(root, world, placer, standIns) > 0) throw new InvalidDataException("World: layer counts differ from the export");
            Debug.Log($"World: reflection probes baked {Probes.Build(lighting, look)} (light.json probes; <scene>-probe-<name>.exr)");
            Directory.CreateDirectory(Path.GetDirectoryName(InUnity(ScenePath)));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException($"World: could not save {ScenePath}");
            Debug.Log($"World: kit facades hide procedural walls on {KitCover.Hidden} wall edges");
            Debug.Log($"World: built {ScenePath} from {path} ({world.Schema}, sha256 {world.Sha256}) in {(DateTime.UtcNow - t0).TotalSeconds:F2}s; " +
                $"{renderers} batch renderers, {verts} vertices; {Coplanar.Faces} coplanar faces inset; fingerprint {Inspect.Fingerprint(root, batches, cameras)}");
        }
    }
}
