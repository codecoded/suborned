using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Suborned.World.Perf
{
    /// <summary>Perf honesty (Redline 2026-10-15 M3): the people layer (People Shots and the Snapshot menus save it into the
    /// generated scene, with its per-frame IMGUI picker) is not part of what the perf player measures unless the build asks for it.
    /// While PerfBuild builds without people, this strips the scene roots named in `Roots` from the scene copy the build
    /// processes; the scene file is not touched. PerfPlanner fingerprints every other root and records what was stripped, and the
    /// player checks the people it finds against the plan (PerfHarness). Any other build, and entering Play Mode, keeps the scene as it is.</summary>
    public sealed class PerfStrip : IProcessSceneWithReport
    {
        /// <summary>Roots a perf build without people leaves out.</summary>
        public static readonly string[] Roots = { People.PeopleLayer.RootName, Play.PlayInstall.RootName }; // the live host draws the engine's people (brief 011 D)

        /// <summary>Set by PerfBuild for its own build only.</summary>
        internal static bool Active;

        public static bool Strips(string root) => Active && Array.IndexOf(Roots, root) >= 0;

        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (!Active || report == null) return; // report null: Play Mode in the Editor
            foreach (var go in scene.GetRootGameObjects())
                if (Array.IndexOf(Roots, go.name) >= 0)
                {
                    Debug.Log($"Perf: stripped root `{go.name}` ({go.GetComponentsInChildren<Suborned.People.PersonRecord>(true).Length} people) from {scene.path} for the perf player");
                    UnityEngine.Object.DestroyImmediate(go);
                }
        }
    }
}
