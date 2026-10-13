using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Suborned.World.Review
{
    /// <summary>Walk review of the built Harbour District, for sign-off: a review tool, never the game (GDD §10: the game is
    /// never first person). Menu: Suborned/World/Walk Review (Play) opens the generated scene if needed, enters Play Mode and
    /// attaches the tool at runtime: nothing is added to the scene file and the builder's output is not changed, so the scene
    /// fingerprint stays the same. A plain Play, without the menu, runs the scene without it (perf probes measure the game, not
    /// this overlay). Editor only: all of it lives in this Editor assembly, bar a UNITY_EDITOR-only host component.
    /// The parts: WalkRig (session), FootMode / FlyMode / TacticalMode (cameras), WalkSurface (what a walker stands on),
    /// HeightField (roof heights: stand-off and view rays), PlanIndex (ids at a point), WalkPlaces (jump list), WalkHud
    /// (overlay), WalkNotes (out/review/walk-notes.json + out/shots/walk/).</summary>
    [InitializeOnLoad]
    public static class WalkReview
    {
        const string Asked = "Suborned.WalkReview", Background = "Suborned.WalkReview.runInBackground";
        static HeightField heights;

        /// <summary>The running session, for checks driven from outside (null when the tool is not attached).</summary>
        public static WalkRig Rig { get; private set; }

        static WalkReview() => EditorApplication.playModeStateChanged += Changed;

        [MenuItem("Suborned/World/Walk Review (Play)")]
        public static void Start() => HarbourWorldBuilder.Batch(() =>
        {
            if (EditorApplication.isPlaying) { Attach(); return; }
            if (!RenderSession.EnsureScene()) return;
            SessionState.SetBool(Asked, true);
            EditorApplication.EnterPlaymode();
        });

        static void Changed(PlayModeStateChange change)
        {
            bool asked = SessionState.GetBool(Asked, false);
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                // A check that drives the session while the Editor is not in front sets Application.runInBackground; the Editor keeps
                // that in memory after Play Mode (and would write it to ProjectSettings on the next save). The session leaves it as it found it.
                bool was = SessionState.GetBool(Background, Application.runInBackground);
                if (asked && Application.runInBackground != was) Application.runInBackground = was;
                SessionState.EraseBool(Asked); SessionState.EraseBool(Background); heights = null; Rig = null;
                return;
            }
            if (!asked) return;
            // The roofs are read while the meshes are still the builder's own: entering Play Mode combines the static batches.
            if (change == PlayModeStateChange.ExitingEditMode) { SessionState.SetBool(Background, Application.runInBackground); heights = Roofs(); }
            else if (change == PlayModeStateChange.EnteredPlayMode) HarbourWorldBuilder.Batch(Attach);
        }

        static HeightField Roofs()
        {
            var root = GameObject.Find(HarbourWorldBuilder.RootName);
            var ground = GameObject.Find("batches/ground");
            if (!root || !ground) throw new InvalidDataException($"World: walk review: {HarbourWorldBuilder.ScenePath} is not the built district (no {HarbourWorldBuilder.RootName} or batches/ground)");
            var b = Inspect.Bounds(ground.transform); // the plan's ground: the sheet the district stands on
            var sea = GameObject.Find("sea");
            var t0 = System.Diagnostics.Stopwatch.StartNew();
            var field = new HeightField(Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z), root.transform.Find("horizon"), sea ? sea.transform : null);
            Debug.Log($"World: walk review: roof heights over x {b.min.x:F0}..{b.max.x:F0}, z {b.min.z:F0}..{b.max.z:F0} from {field.Meshes} meshes by triangle and {field.Boxes} renderers by bounds, " +
                $"highest {field.Highest:F1} m, in {t0.ElapsedMilliseconds} ms{(Application.isPlaying ? " (read in Play Mode: combined batches count by their bounds, so stand-offs are longer than built)" : "")}");
            return field;
        }

        static void Attach()
        {
            if (Rig != null) return;
            if (SceneManager.GetActiveScene().path != HarbourWorldBuilder.ScenePath)
                throw new InvalidDataException($"World: walk review needs {HarbourWorldBuilder.ScenePath} open (Suborned/World/Walk Review (Play) opens it)");
            var play = Camera.main;
            if (!play || !play.orthographic || !play.GetComponent<TacticalShadows>())
                throw new InvalidDataException("World: walk review: the scene has no play camera (an orthographic main camera with TacticalShadows)");
            heights ??= Roofs();
            var main = Views.Load(HarbourWorldBuilder.ViewsPath).Find(v => v.Main) ?? throw new InvalidDataException($"World: walk review: {HarbourWorldBuilder.ViewsPath} has no main view");

            var world = WorldReader.Load(RenderSession.ExportPath);
            var built = GameObject.Find("source");
            if (built && built.transform.childCount > 0 && built.transform.GetChild(0).name != world.Source)
                Debug.LogWarning($"World: walk review: the scene was built from `{built.transform.GetChild(0).name}` but {RenderSession.ExportPath} is `{world.Source}`: ids in the readout may not match what is drawn; rebuild the scene");
            var index = new WorldIndex(world);
            var plan = new PlanIndex(world, index, Path.Combine(HarbourWorldBuilder.RepoRoot, "contracts", "world", "harbour-district.plan.json"));
            var cameras = GameObject.Find("cameras");
            var places = WalkPlaces.Load(world, index, plan, cameras ? cameras.transform : null);

            foreach (var old in Object.FindObjectsByType<FreeFly>()) Object.Destroy(old.gameObject); // this tool's Fly mode stands in for it: one review camera, one set of keys
            Rig = new WalkRig(play, main.Aspect, new WalkSurface(world), plan, heights, places, new WalkNotes(HarbourWorldBuilder.RepoRoot, world.Source));
            var host = new GameObject("walk-review").AddComponent<ReviewHost>();
            host.Tick = Rig.Tick; host.Gui = Rig.Gui; host.Closed = Rig.Dispose;
            Debug.Log($"World: walk review attached: {places.Count} places, notes → {Rig.Notes.NotesFile}. 1 Walk, 2 Fly, 3 Tactical, F fade, H lists the keys.");
        }
    }
}
