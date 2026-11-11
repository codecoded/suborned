#if UNITY_EDITOR
using System.Linq;
using Suborned.Play;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 107 (Glint's fix for night in the old town): the swap dropped our scene's `lighting` root, so OldTown.unity had no LightPhase and a
    /// night briefing or shot quietly played as day. Before that root goes, HarbourDistrict's night (NightPhase's build from light.json: the night State, the
    /// night grade `grade-night`, the night lamps tuned for the zone) comes across into a root of its own, moved by the same DZ as Ours. Night lamps outside
    /// the Handover zone are dropped (HarbourDistrict's other streets aren't the old town's); the old town's own lamp heads get PlayView107's lamps. The phase
    /// keeps the old town's own day (LightPhase.sceneDay) and drives no sun: PlayView107 owns the play camera's light, and the phase does the rest
    /// (RoadLight, SignalLight, the glows, the night sky, reflection and grade). Day frames are unchanged: the phase is saved in its day state, lamps off.</summary>
    internal static class SwapNight107
    {
        public const string RootName = "Night (our LightPhase, brief 107)";

        /// <summary>From our scene `ours` (opened additive) into `target`, before the swap destroys `lighting`. Null when our scene has no phase.</summary>
        public static Transform Carry(OldTownSwap.Report rep, Scene ours, Scene target)
        {
            var lighting = ours.GetRootGameObjects().FirstOrDefault(g => g.name == "lighting");
            var phaseT = lighting ? lighting.transform.Find("phase") : null;
            if (!phaseT || !phaseT.TryGetComponent<LightPhase>(out var lp)) { rep.Log("night: our scene has no lighting/phase (LightPhase): the old town has NO night (PlayView107 refuses it)"); return null; }
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, ours);
            foreach (var n in new[] { "phase", "night-lamps", "grade-night" }) { var t = lighting.transform.Find(n); if (t) t.SetParent(root.transform, true); }
            SceneManager.MoveGameObjectToScene(root, target);
            root.transform.position = new Vector3(0f, 0f, OldTownSwap.DZ);
            var zone = OldTownSwap.ZoneOld; int kept = 0, dropped = 0;
            var lamps = new System.Collections.Generic.List<Light>(); var level = new System.Collections.Generic.List<float>();
            for (int i = 0; i < lp.lamps.Length; i++)
            {
                var l = lp.lamps[i]; if (!l) continue;
                if (SwapMesh.Inside(zone, new Vector2(l.transform.position.x, l.transform.position.z))) { lamps.Add(l); level.Add(i < lp.lampIntensity.Length ? lp.lampIntensity[i] : l.intensity); kept++; }
                else { Object.DestroyImmediate(l.gameObject); dropped++; }
            }
            lp.lamps = lamps.ToArray(); lp.lampIntensity = level.ToArray();
            lp.sun = null; lp.probes = new ReflectionProbe[0]; lp.probeIntensities = new float[0]; lp.day.grade = null; lp.sceneDay = true;
            if (lp.night.grade) lp.night.grade.enabled = false;
            rep.Log($"night: our LightPhase carried into the old town ({RootName}, DZ {OldTownSwap.DZ}): {kept} night lamps in the zone kept, {dropped} outside dropped; night grade {(lp.night.grade ? lp.night.grade.name : "none")}; {lp.glows.Length} glows; sun none (PlayView107 drives the play light)");
            return root.transform;
        }
    }
}
#endif
