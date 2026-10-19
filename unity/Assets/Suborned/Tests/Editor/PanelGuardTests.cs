using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Suborned.World.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Suborned.Play.Tests
{
    /// <summary>Sky 2026-10-19: "engine fault in panel: FormatException: no person in the role 'patrol'". The action panel and the hover price
    /// open on every target kind (each cast person, the patrol, each camera, the rivals, the response unit, both cars, the door, the cabinet, the
    /// agents, passers-by, bodies, hostiles, units, stashes) for each agent alone and all four, at 0:30, every 90 s and the mission's moments (the
    /// patrol away, a body, a unit out, Vale out, the final fight), over four routes, with no fault (PanelGuard). Needs the generated scene
    /// (Assets/Scenes/HarbourDistrict.unity with the host); enters and leaves play mode. Run: the Test Runner (EditMode) or run_tests
    /// filter "PanelGuard".</summary>
    public sealed class PanelGuardTests
    {
        const string Scene = "Assets/Scenes/HarbourDistrict.unity";

        [UnityTest]
        public IEnumerator ThePanelNeverFaultsOnAnyTargetOrSelection()
        {
            bool entered = false;
            if (!EditorApplication.isPlaying)
            {
                if (Object.FindAnyObjectByType<HandoverHost>() == null)
                {
                    if (!File.Exists(Path.Combine(Application.dataPath, "..", Scene))) Assert.Ignore($"no {Scene}: build the world first (bun run world, then the Unity build)");
                    EditorSceneManager.OpenScene(Scene);
                }
                yield return new EnterPlayMode(); entered = true;
            }
            yield return null; yield return null; // the host's Start
            LogAssert.ignoreFailingMessages = true; // a caught fault logs an error; the assertions below say which
            var r = PanelGuard.Sweep(); var line = PanelGuard.Report(r);
            var faults = r.Misses.Select(m => $"{m.Route} {m.At} ({m.Moment}) {m.Target} [{m.Selection}] {m.Where}: {m.Fault}").ToList();
            var missing = PanelGuard.Missing(r);
            if (entered) yield return new ExitPlayMode();
            Assert.That(faults, Is.Empty, line + "\n" + string.Join("\n", faults.Take(30)));
            Assert.That(missing, Is.Empty, "target kinds never swept: " + string.Join(", ", missing));
        }
    }
}
