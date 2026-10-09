using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>Builds Assets/Scenes/HarbourDistrict.unity from scratch from the world export (suborned.world/1).
    /// Menu: Suborned/Graybox/Build Harbour District. Batchmode:
    ///   Unity -batchmode -nographics -quit -projectPath unity -executeMethod Suborned.Graybox.WorldBuilder.Build
    ///         [-world &lt;path&gt;]   (default: &lt;repo&gt;/out/world/harbour-district.world.json, written by `bun run world`)
    /// One root per export layer, named after its key; meshes and materials are generated in memory and saved in the scene.</summary>
    public static class WorldBuilder
    {
        public const string ScenePath = "Assets/Scenes/HarbourDistrict.unity";
        public const string RootName = "HarbourDistrict";

        public static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        /// <summary>Command-line path argument; relative paths resolve against the repo root.</summary>
        public static string Arg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? Path.GetFullPath(Path.Combine(RepoRoot, args[i + 1])) : fallback;
        }

        public static string WorldPath => Arg("-world", Path.Combine(RepoRoot, "out", "world", "harbour-district.world.json"));

        [MenuItem("Suborned/Graybox/Build Harbour District")]
        public static void Build() => Batch(() => { if (MayReplaceScene()) BuildFrom(WorldPath); });

        /// <summary>In an interactive editor, offers to save modified scenes before one is replaced; false on cancel.
        /// Batchmode always proceeds.</summary>
        public static bool MayReplaceScene()
        {
            if (Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return true;
            Debug.Log("Graybox: cancelled; the open scene was left as it was");
            return false;
        }

        /// <summary>Runs an editor step; in batchmode a failure exits non-zero instead of leaving a half-built scene.</summary>
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

        /// <summary>Scene root whose one child is named after the plan hash the scene was built from.</summary>
        public const string SourceName = "source";

        public static string PlanHash(object world) => J.S(J.Get(world, "source"), "planHash");

        /// <summary>Plan hash stamped into the open scene by BuildFrom.</summary>
        public static string ScenePlanHash()
        {
            var go = GameObject.Find("/" + SourceName);
            if (!go || go.transform.childCount != 1) throw new InvalidDataException($"Graybox: {ScenePath} has no plan stamp; rebuild it");
            return go.transform.GetChild(0).name;
        }

        public static object Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"Graybox: world export not found at {path}; run `bun run world`");
            var world = Json.Parse(File.ReadAllText(path));
            var schema = J.S(world, "schema");
            if (schema != "suborned.world/1") throw new InvalidDataException($"Graybox: expected suborned.world/1, got {schema}");
            if (J.Get(world, "source") == null) throw new InvalidDataException("Graybox: export has no source.planHash; run `bun run world`");
            return world;
        }

        public static void BuildFrom(string path)
        {
            var world = Load(path);
            Poly.Fallbacks = 0;
            Poly.Repairs = 0;
            Debug.Log($"Graybox: world {path} plan {PlanHash(world)}");
            Style.Reset();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            float pavement = J.F(J.O(world)["heights"], "pavement");

            var root = Nodes.Group(null, RootName);
            var cut = new CutAway();
            Transform Layer(string name) => Nodes.Group(root, name);
            LandLayers.Ground(Layer("ground"), world);
            StreetLayers.Roads(Layer("roads"), world);
            StreetLayers.Medians(Layer("medians"), world, pavement);
            StreetLayers.Foot(Layer("foot"), world, pavement);
            LandLayers.Plots(Layer("plots"), world, pavement, cut);
            LandLayers.Areas(Layer("areas"), world);
            StreetLayers.Circus(Layer("circus"), world, pavement);
            LandLayers.Water(Layer("water"), world, pavement);
            TransitLayers.Tram(Layer("tram"), world, new Surfaces(world, pavement));
            TransitLayers.Stops(Layer("stops"), world);
            TransitLayers.SkyRail(Layer("skyRail"), world);
            TransitLayers.Pylons(Layer("pylons"), world);
            TransitLayers.Stations(Layer("stations"), world);
            MarkerLayers.Gates(Layer("gates"), world);
            MarkerLayers.Landmarks(Layer("landmarks"), world, pavement);
            var handover = Layer("handover");
            HandoverLayer.Build(handover, world, cut);
            cut.Build(Layer("cutaway"));

            var focus = Inspect.Bounds(handover.Find("buildings")).center;
            focus.y = pavement;
            Rig.Build(Nodes.Group(null, "cameras"), Nodes.Group(null, "lighting"), Inspect.Bounds(root), focus, Rig.View(world));
            Nodes.Group(Nodes.Group(null, SourceName), PlanHash(world));

            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(RepoRoot, "unity", ScenePath)));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException($"Graybox: could not save {ScenePath}");

            if (Poly.Repairs > 0) Debug.LogWarning($"Graybox: {Poly.Repairs} polygon repairs (the export promises simple polygons)");
            if (Poly.Fallbacks > 0) Debug.LogWarning($"Graybox: {Poly.Fallbacks} ear-clipping fallbacks (degenerate polygons)");
            Debug.Log($"Graybox: built {ScenePath} from {path}\n{Inspect.Counts(root)}");
            Inspect.Verify(root, world);
        }
    }
}
