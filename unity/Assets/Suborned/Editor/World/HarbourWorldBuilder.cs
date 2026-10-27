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
            var skyline = new SkylineKit(placer, look) { SkyBodyHaze = kit.SkyBodyHaze };
            ProcFacade.Today = kit.EveryFaceToday; ProcFacade.ShellWalls = 0; // brief 072-E
            var root = Nodes.Group(null, RootName);
            var stamp = root.gameObject.AddComponent<WorldStamp>(); // what this scene was built from (the walk tool reads it)
            stamp.source = world.Source; stamp.exportSha256 = world.Sha256;
            stamp.exportPath = Path.GetRelativePath(RepoRoot, Path.GetFullPath(path)).Replace('\\', '/');
            KerbKit.KitKerbs(world.Ground.Pieces); // where the export lays kit kerb stones, the builder's kerb steps aside
            var groundLayer = Nodes.Group(root, "ground");
            layers.Ground(groundLayer, world.Ground);
            RoadMarkings.Build(nodes, groundLayer, world); // brief 026 round 2: the dashed centre lines
            Debug.Log($"World: kerbs as stones {KerbKit.Stones} segments, gutter strips {KerbKit.Gutters} segments; {KerbKit.Covered} segments left to the export's kit kerb stones (brief 026 round 2)");
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
            CrossingKit.Signalled.Clear(); if (world.Crossings != null) foreach (var cx in world.Crossings) if (cx.Cycle != null) CrossingKit.Signalled.Add(cx.Id); // brief 061: before the street layer bakes the crossing parts
            city.Street(street, world.Street);
            var roadAlt = RoadFinishTwin.Load(path, world); // brief 055 road test: the asphalt finish beside the enamel, switched at run time
            if (world.Ground.Pieces.Count > 0) { int gp = city.GroundPieces(Nodes.Group(root, "ground-pieces"), world.Ground.Pieces, roadAlt == null ? null : (System.Func<StreetItemDto, string>)(gpc => roadAlt.EnamelOnly.Contains(RoadFinishTwin.Key(gpc)) ? RoadFinishTwin.EnamelLayer : null)); Debug.Log($"World: ground pieces {gp} (export ground.pieces: tiles, drains, manholes)"); }
            if (roadAlt != null) { int ap = city.GroundPieces(Nodes.Group(root, RoadFinishTwin.AsphaltLayer), roadAlt.AsphaltOnly); Debug.Log($"World: road finish twin: {roadAlt.EnamelOnly.Count} enamel-only pieces baked apart, {ap} asphalt-only pieces built off ({roadAlt.Path})"); }
            if (world.Crossings != null) new CrossingKit(nodes, kit, placer).Build(Nodes.Group(root, "crossings"), street, world);
            layers.Levels(Nodes.Group(root, "levels"), Nodes.Group(root, "decks"), world.Levels);
            megas.Megastructures(Nodes.Group(root, "megastructures"), world.Megastructures, placer, standIns);
            if (world.HiddenCivic > 0) Debug.Log($"World: {world.HiddenCivic} civic masses `hidden` in the export, not drawn (data only)");
            facade.Civic(Nodes.Group(root, "civic"), Coplanar.Masses(world.Civic), placer);
            Debug.Log($"World: civic monument plinths {facade.Plinths}");
            if (world.Entrances.Count > 0) ServiceDoors.Build(nodes, Nodes.Group(root, "takedown"), world); // brief 013: the service doors (export takedown.entrances)
            megas.Gates(Nodes.Group(root, "gates"), world.Gates);
            layers.Landmarks(Nodes.Group(root, "landmarks"), world.Landmarks, placer);
            skyline.Signatures(Nodes.Group(root, "signatures"), world.Signatures, nodes);
            var batches = Nodes.Group(null, "batches");
            long verts = Combine.Flush(batches);
            // brief 072-E: the every-face A/B switch (EveryFace, Redline's plan 5): `today` built off, the rest as built
            var everyFace = root.gameObject.AddComponent<EveryFace>();
            everyFace.modules = KitColumns.ModuleRoots.ToArray();
            everyFace.every = Layer072.Every.Append(Layer072.Shell).Select(n => batches.Find(n)).Where(t => t).Select(t => t.gameObject).ToArray();
            everyFace.today = Layer072.Today.Select(n => batches.Find(n)).Where(t => t).Select(t => t.gameObject).ToArray();
            foreach (var t in everyFace.today) t.SetActive(false);
            Debug.Log($"World: every-face switch: {everyFace.modules.Length} module roots, every [{string.Join(", ", everyFace.every.Select(g => $"{g.name} {g.transform.childCount}"))}], " +
                $"today (off; development builds only) [{string.Join(", ", everyFace.today.Select(g => $"{g.name} {g.transform.childCount}"))}]; shadows-only shell on {ProcFacade.ShellWalls} masses");
            if (roadAlt != null) RoadFinishTwin.Install(root, batches);
            int renderers = batches.GetComponentsInChildren<MeshRenderer>().Length;
            Debug.Log($"World: tangents for {Combine.TangentMeshes} normal-mapped batches (brief 026: textured ground)");
            var cameras = Nodes.Group(null, "cameras");
            var lighting = Nodes.Group(null, "lighting");
            Rig.Build(cameras, lighting, root, world, Views.Load(ViewsPath), look);
            skyline.Horizon(Nodes.Group(root, "horizon"), Nodes.Group(null, "sea"), world.Horizon, world.Ground); // after Rig: views frame the district, not the 16 km band
            ReflectionCube.Build(look);
            var flagged = FadeTags.Elements(root.Find("civic"), world.Civic.Where(m => m.Fade).Select(m => m.Id));
            var (fadeKit, fadeBatches, fadeFlagged) = FadeTags.Apply(root, batches, facade.Spec.Atlas, flagged);
            Debug.Log($"World: architecture fade may thin {fadeKit} kit renderers and {fadeBatches} of {renderers} batch renderers (rendering-layer bit {ArchitectureFade.Layer}); " +
                $"of the kit renderers {fadeFlagged} are on {flagged.Count} civic pieces the export flags `fade`");
            var (proxies, proxyTris) = ShadowProxies.Build(root, kit.ShadowProxies, KitColumns.ModuleGroups, kit.ModuleShadowProxies); // after FadeTags: proxies take their LOD1's fade bit
            Debug.Log($"World: shadow proxies {proxies} ({proxyTris} triangles), of them {ShadowProxies.Modules} every-face module proxies from L2 ({ShadowProxies.ModuleTris} triangles), {(kit.ModuleShadowProxies ? "on: no module LOD casts" : "built off")}; " +
                $"kit facades and trees from LOD1 {(kit.ShadowProxies ? "on: kit facades and street trees cast from LOD1, no LOD of theirs casts" : "built off (kit.json shadowProxies): every LOD casts as before; the perf harness switches them")}; " +
                $"every module's L2 is one renderer (F4); {ShadowProxies.MultiLevel} kit facade or tree groups have more than one in their proxy level, cast from the first{(ShadowProxies.MultiLevel > 0 ? ": " + string.Join(", ", ShadowProxies.MultiLevelNames) : "")}");
            var guard = KitLods.PlayZoomGuard(root); // brief 072 (Redline's plan 3.1): throws if a building-layer group switches to L1 nearer than 36 m
            Debug.Log($"World: play-zoom guard: {guard.groups} LOD groups checked; nearest L1 switch on the building layers {guard.nearest:F1} m ({guard.nearestName}) >= {KitLods.GuardM} m; {guard.otherBelow} groups outside the facade layers switch nearer (warned above)");
            ProcKit.Report(world, facade);
            ContactShade.Build(look, world, root); // brief 026: contact shade at building feet, kerb feet and under street pieces (SSAO is blind at ortho play zoom)
            Nodes.Group(Nodes.Group(null, "source"), world.Source ?? Path.GetFileName(path));
            Play.PlayInstall.Install(world); // the live Handover host (brief 011 D): its own root, `play`
            Debug.Log($"World: kit parts spinning {KitParts.Spin(root)} (kit.json `parts`; view only)");
            RobotKit.EnsureStageAtlas(); // item 24: the patrol face atlas at its own 16 x 60 (no power-of-two scaling)
            CrossingKit.EnsureSignalTextures(); /* batch 0.2.118: the signal atlas and ring keep their alpha (the figure, numerals and ring fill are alpha cuts); a guard since the kit's metas carry it (0.2.123) */
            Debug.Log($"World: robots alive: {RobotKit.Seeds(root)} placed robots rigged (face + head), seeded by their export path (brief 026; view only)");

            // Verify before saving: a scene that disagrees with the export is never written.
            Debug.Log(placer.Summary());
            if (Inspect.Verify(root, world) + Inspect.VerifyKit(root, world, placer, standIns) > 0) throw new InvalidDataException("World: layer counts differ from the export");
            Debug.Log($"World: reflection probes baked {Probes.Build(lighting, look)} (light.json probes; <scene>-probe-<name>.exr)");
            NightPhase.Build(lighting, root, look, world); // brief 026 round 2: the night look (light.json night), off; the scene is saved by day
            Directory.CreateDirectory(Path.GetDirectoryName(InUnity(ScenePath)));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException($"World: could not save {ScenePath}");
            Debug.Log($"World: kit facades hide procedural walls on {KitCover.Hidden} wall edges");
            Debug.Log($"World: built {ScenePath} from {path} ({world.Schema}, sha256 {world.Sha256}) in {(DateTime.UtcNow - t0).TotalSeconds:F2}s; " +
                $"{renderers} batch renderers, {verts} vertices; {Coplanar.Faces} coplanar faces inset; fingerprint {Inspect.Fingerprint(root, batches, cameras)}");
        }
    }
}
