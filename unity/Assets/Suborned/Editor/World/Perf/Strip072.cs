using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Suborned.World.Perf
{
    /// <summary>Brief 072-E: the every-face A/B's `today` walls (EveryFace.today: batches/buildings-today, skins-today) are for development players
    /// only (the perf harness). A release build drops them from the scene copy it processes; the scene file is not touched. Play Mode keeps them.</summary>
    public sealed class Strip072 : IProcessSceneWithReport
    {
        public int callbackOrder => 1;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || (report.summary.options & BuildOptions.Development) != 0) return;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var ef in go.GetComponentsInChildren<EveryFace>(true))
                {
                    int n = 0;
                    foreach (var t in ef.today) if (t) { Object.DestroyImmediate(t); n++; }
                    ef.today = System.Array.Empty<GameObject>();
                    if (n > 0) Debug.Log($"Build: stripped the every-face A/B's {n} `today` batch roots from {scene.path} (release build)");
                }
        }
    }
}
