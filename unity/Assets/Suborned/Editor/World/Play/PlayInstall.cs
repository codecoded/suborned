using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.Play
{
    /// <summary>Puts the live Handover host into the generated scene (brief 011 D): the root `handover` with HandoverHost and its DebugHud, baked from
    /// the sources so a player needs no repo files but the engine bundle in StreamingAssets: the block-to-plan frame (plan.json civic.blockToPlan),
    /// the ground heights on a 1 m grid over the site window (the scene's export, its plot slabs: the people layer's rule), the capsule colours
    /// (civic look palette, unlit hex; the people layer's job blocks for the cast) and the signals' draw colours (style contract signal_colours;
    /// unlit and emissive, not reserved from the world: Sky, 2026-10-16), and
    /// two material templates (Assets/Suborned/Materials/Play) whose shaders a build then carries. The builder calls it on every build; the
    /// menu installs it on the open scene and sets the people layer to the mission start (mission.json clock.startsAt).
    /// Menu: Suborned/Play/Install Handover Host · Suborned/Play/Play the Handover</summary>
    public static class PlayInstall
    {
        public const string RootName = "handover"; // a scene root; never "play" (cameras/play is the play camera)
        const string MatDir = "Assets/Suborned/Materials/Play";

        [MenuItem("Suborned/Play/Install Handover Host")]
        static void InstallMenu() => HarbourWorldBuilder.Batch(() =>
        {
            if (!Review.RenderSession.EnsureScene()) return;
            InstallOpen(true);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        });

        [MenuItem("Suborned/Play/Play the Handover")]
        static void PlayMenu() => HarbourWorldBuilder.Batch(() =>
        {
            if (!Review.RenderSession.EnsureScene()) return;
            if (!Root()) { InstallOpen(true); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); }
            EditorApplication.EnterPlaymode();
        });

        /// <summary>On the open generated scene: the host from the scene's own export, and (people) the people layer at the mission start.</summary>
        public static HandoverHost InstallOpen(bool people)
        {
            var stamp = Object.FindAnyObjectByType<WorldStamp>() ?? throw new InvalidDataException("Play: no WorldStamp; build the world first");
            var host = Install(WorldReader.Load(People.PeopleLayer.ExportOf(stamp)));
            if (people && File.Exists(People.PeopleLayer.PeoplePath))
            {
                var mission = Json.Parse(File.ReadAllText(Path.Combine(HarbourWorldBuilder.RepoRoot, "contracts/missions/handover/mission.json")));
                People.PeopleLayer.Snapshot(People.PeopleLayer.Minutes(J.S(J.Get(mission, "clock"), "startsAt")));
            }
            return host;
        }

        /// <summary>Replaces the `play` root of the open scene. Throws on anything unknown.</summary>
        public static HandoverHost Install(WorldDto world)
        {
            if (Root() is { } old) Object.DestroyImmediate(old);
            var go = new GameObject(RootName);
            var host = go.AddComponent<HandoverHost>(); go.AddComponent<PlayHud>(); go.AddComponent<DebugHud>();
            var repo = HarbourWorldBuilder.RepoRoot;
            var plan = Json.Parse(File.ReadAllText(Path.Combine(repo, "contracts/world/harbour-district.plan.json")));
            var b2p = J.Get(J.Get(plan, "civic"), "blockToPlan");
            var f = host.frame;
            f.origin = J.P(b2p, "origin"); f.axisX = J.P(b2p, "x"); f.axisY = J.P(b2p, "y");
            f.x0 = -52f; f.y0 = -66f; f.cell = 1f; f.nx = 164; f.ny = 82; // the site window (tools/sim/site.ts WINDOW), block metres
            f.heights = Heights(world, f);

            var look = Json.Parse(File.ReadAllText(Path.Combine(repo, "contracts/world/civic-look.json")));
            var pal = J.A(J.Get(look, "palette"), "materials").ToDictionary(m => J.S(m, "material"), m => J.S(m, "unlit"));
            Color P(string material) => pal.TryGetValue(material, out var hex) && ColorUtility.TryParseHtmlString(hex, out var c) ? c : throw new InvalidDataException($"Play: {material} is not in the civic look palette (unknown = reject)");
            var spec = Json.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Suborned/Editor/World/People/people.json")));
            Color Block(string b) => P(J.S(J.Get(spec, "blocks"), b) ?? throw new InvalidDataException($"Play: people.json has no block {b}"));
            var style = Json.Parse(File.ReadAllText(Path.Combine(repo, "contracts/style/suborned.style.json")));
            Color Sig(string k) => ColorUtility.TryParseHtmlString(J.S(J.Get(style, "signal_colours"), k), out var c) ? c : throw new InvalidDataException($"Play: style contract signal_colours.{k}");

            host.cellColour = P("DK_Enamel_Charcoal"); host.cellHeadColour = P("DK_Enamel_Ivory"); host.headColour = P(J.S(spec, "headMaterial"));
            host.waitressColour = Block("job/food"); host.civicColour = Block("job/civic"); host.securityColour = P("DK_Steel");
            host.rivalBodyColour = P("DK_GlassDark"); host.carColour = P("DK_Steel"); host.getawayColour = P("DK_Enamel_Ivory");
            host.coneColour = Sig("cones"); host.guardColour = Sig("guard"); host.rivalColour = Sig("rivals"); host.selectColour = P("DK_Bloom_Cream");
            // crowd colours: each engine job's look block, as the people layer colours that position (out/people), else resident
            var jobBlocks = new SortedDictionary<string, string>(System.StringComparer.Ordinal) { ["resident"] = "job/resident" };
            if (File.Exists(People.PeopleLayer.PeoplePath))
                foreach (var p in J.A(Json.Parse(File.ReadAllText(People.PeopleLayer.PeoplePath)), "persons"))
                {
                    var w = J.Get(p, "work"); var job = w == null ? "resident" : J.S(w, "position") ?? J.S(w, "role");
                    if (job != null && !jobBlocks.ContainsKey(job)) jobBlocks[job] = J.S(J.Get(p, "look"), "block");
                }
            host.jobs = jobBlocks.Keys.ToArray(); host.jobColours = jobBlocks.Values.Select(Block).ToArray();
            host.figures = People.FigureBuild.Library(); // brief 020: the stand-in figures, when installed (else capsules)
            GameObject Kit(string path) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/Kit/Models/" + path); // brief 020: kit models (bun run kit), else the boxes
            host.sedanOfficial = Kit("Vehicles/DK_Sedan_Official.fbx"); host.sedanGetaway = Kit("Vehicles/DK_Sedan_Getaway.fbx"); host.robotPorter = Kit("Robots/DK_RobotPorter_Play.fbx"); host.patrolRobot = Kit("Robots/DK_RobotButler_Play.fbx"); // the patrol robot (a sim agent, no world element): the lighter butler, as the café's foreground robot (Splice, brief 020)
            host.litTemplate = Template("PlayLit", "Universal Render Pipeline/Lit"); host.unlitTemplate = Template("PlayUnlit", "Universal Render Pipeline/Unlit"); host.fanTemplate = FanTemplate();
            // GDD v0.24: the public door's view from each yaw (DoorSight), for the door marker's turn glyph and H13's door cause
            try
            {
                var site = SimBundle.Load().Inputs.SiteData; host.doorSeen = DoorSight.Measure(host.frame.World(site.At(site.GuardPost)));
                var ms = Json.Parse(File.ReadAllText(Path.Combine(HarbourWorldBuilder.RepoRoot, "contracts/missions/handover/mission.json"))); // the Office entrance's `yaw` (V-1, v0.24), else the yaw that sees the door best (brief 023: the west door, clear at the opening yaw 30, carries no yaw)
                int best = 0; for (int k = 1; k < host.doorSeen.Length; k++) if (host.doorSeen[k] > host.doorSeen[best]) best = k;
                host.doorPlayedYaw = host.doorSeen.Length == 4 ? 30 + 90 * best : 210; foreach (var d in J.A(ms, "destinations")) if (J.S(d, "name") == "Office entrance" && J.Get(d, "yaw") is double y) host.doorPlayedYaw = (int)y;
                Debug.Log($"Play: the public door seen at yaw 30/120/210/300: {string.Join(" / ", host.doorSeen.Select(x => x.ToString("0.00")))} (clear at {HandoverHost.DoorClearLimit}); played at {host.doorPlayedYaw}");
            }
            catch (System.Exception e) { host.doorSeen = new float[0]; Debug.LogWarning($"Play: the door's view was not measured ({e.Message}); the host falls back to the GDD's yaw 210"); }
            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log($"Play: installed the Handover host (`{RootName}`): frame origin {f.origin}, heights {f.nx}x{f.ny} at {f.cell} m (max {f.heights.Max():F2} m)");
            return host;
        }

        /// <summary>The host's root in the active scene: a root object by name, never a child (GameObject.Find matches any depth).</summary>
        public static GameObject Root() => EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == RootName);

        static Material Template(string name, string shader)
        {
            var path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m) return m;
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Suborned/Materials", "Play");
            m = new Material(Shader.Find(shader) ?? throw new InvalidDataException($"Play: shader {shader} missing")) { name = name, enableInstancing = true };
            if (shader.EndsWith("/Lit")) m.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>The sight fans' fill: URP Unlit set transparent (alpha blend, no depth write) and two-sided, as an asset so its variant ships.</summary>
        static Material FanTemplate()
        {
            var path = $"{MatDir}/PlayFan.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Suborned/Materials", "Play");
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? throw new InvalidDataException("Play: shader Universal Render Pipeline/Unlit missing")) { name = "PlayFan" };
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_Cull", 0f); m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.3f));
            EditorUtility.SetDirty(m); AssetDatabase.SaveAssetIfDirty(m);
            return m;
        }

        /// <summary>Pavement height per 1 m cell: the highest plot slab under the cell centre with its top below 1 m (PeopleLayer's Ground.Height);
        /// a drive and the bay it reaches at their own level (the slab is cut round them).</summary>
        static float[] Heights(WorldDto w, SiteFrame f)
        {
            var slabs = new List<(Vector2[] ring, Rect box, float top)>();
            foreach (var p in w.Ground.Plots)
                if (p.Ring is { Length: >= 3 } && p.Base + p.Height < 1f)
                {
                    float x0 = p.Ring.Min(v => v.x), y0 = p.Ring.Min(v => v.y);
                    slabs.Add((p.Ring, new Rect(x0, y0, p.Ring.Max(v => v.x) - x0, p.Ring.Max(v => v.y) - y0), p.Base + p.Height));
                }
            var cuts = new List<(Vector2[] ring, float level)>();
            foreach (var d in w.Ground.Drives) { cuts.Add((d.Ring, d.Level)); if (w.Ground.Flats.Find(x => x.Id == d.To) is { } bay) cuts.Add((bay.Ring, bay.Level)); }
            var h = new float[f.nx * f.ny];
            for (int i = 0; i < f.nx; i++)
                for (int j = 0; j < f.ny; j++)
                {
                    var plan = f.ToPlan(f.x0 + (i + 0.5f) * f.cell, f.y0 + (j + 0.5f) * f.cell); float top = 0f;
                    foreach (var (ring, box, t) in slabs) if (t > top && box.Contains(plan) && Poly.Contains(ring, plan)) top = t;
                    foreach (var (ring, level) in cuts) if (Poly.Contains(ring, plan)) { top = level; break; } // brief 023: a drive and its bay are cut out of the slab (Layers.Ground)
                    h[i * f.ny + j] = top;
                }
            return h;
        }
    }
}
