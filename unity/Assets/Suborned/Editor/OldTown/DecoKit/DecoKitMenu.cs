#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Entry points. Pipeline: Blender export_kit.py -> Import -> Studio capture (parity) / Showroom.
    public static class DecoKitMenu
    {
        const string Menu = "The Suborned/Deco Kit/";

        [MenuItem(Menu + "1 Import Materials + Prefabs")]
        public static void Import() => Run("import", ImportKit);

        [MenuItem(Menu + "2 Capture Studio Icons")]
        public static void Studio() => Run("studio", DecoKitStudio.CaptureAll);

        [MenuItem(Menu + "3 Build Showroom")]
        internal static void Showroom() => Run("showroom", DecoKitShowroom.Build);

        [MenuItem(Menu + "4 Build Plaza Demo (walkable)")]
        public static void Plaza() => Run("plaza", DecoKitPlazaDemo.Build);

        [MenuItem(Menu + "5 Build Street Demo (walkable)")]
        public static void Street() => Run("street", DecoKitStreetDemo.Build);

        /// Batch: fresh paving from export_kit.py, then the street proof in one Unity launch.
        public static void ImportStreet() => Run("import+street", () => { ImportKit(); DecoKitStreetDemo.Build(); });

        // batch: re-capture the saved street (-dkShots a,b) after a shader or grade edit
        public static void Reshoot() => Run("reshoot", DecoKitStreetDemo.Reshoot);

        // batch: camera fly-through of the saved street to Scratch/Captures/DecoKit/<dkOut>.mp4 (-dkStoryboard 5 for stills)
        public static void Flyby() => Run("flyby", DecoKitFlyby.Render);

        // batch: close-ups of each service robot glancing, blinking, changing expression (-dkStoryboard 0.5 for stills)
        public static void RobotReel() => Run("robot reel", DecoKitRobotReel.Render);

        // batch: Southside replaced on the saved street, repainted, saved, 2.5D shots (-dkShots, "none"); Iso: shots only
        public static void Southside() => Run("southside", DecoKitSouthside.Rebuild);
        public static void Iso() => Run("iso", DecoKitIso.Reshoot);

        // batch: the image-to-3D citizens (-dkLineup <assets dir>) in a row on Orbit Square, shot at play, close, detail zoom
        public static void Lineup() => Run("lineup", DecoKitCitizenLineup.Shoot);

        // batch: the realtime crowd test (VAT citizens, Art/Citizens3D/crowd) saved as DecoKitCrowdTest, then shot + timed
        public static void CrowdTest() => Run("crowd test", DecoKitCrowdTest.Build);
        public static void CrowdShoot() => Run("crowd shoot", DecoKitCrowdTest.Shoot);
        public static void CrowdPlayer() => Run("crowd player", DecoKitCrowdTest.Player);

        public static void Paint() => Run("paint", DecoKitStreetDemo.PaintStudy);
        public static void Repaint() => Run("repaint", DecoKitStreetDemo.Repaint);

        // batch: draw cost of the saved street at every shot (no captures)
        public static void Cost() => Run("cost", DecoKitStreetDemo.Cost);

        // batch: draw cost, bake occlusion over the walkable district, draw cost again
        public static void Occlusion() => Run("occlusion", () =>
        {
            DecoKitStreetDemo.Cost();
            DecoKitOcclusion.Bake();
            DecoKitStreetDemo.Cost();
        });

        [MenuItem(Menu + "Import + Studio + Showroom")]
        public static void All() => Run("all", () =>
        {
            ImportKit();
            DecoKitStudio.CaptureAll();
            DecoKitShowroom.Build();
        });

        static void ImportKit()
        {
            var lib = DecoKitMaterials.BuildAll();
            var built = DecoKitModels.BuildAll(lib);
            Debug.Log($"[DecoKit] imported {lib.Count} materials, {built.Count} prefabs");
        }

        static void Run(string step, Action body)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                AssetDatabase.Refresh();  // pick up fresh FBX/PNG from export_kit.py
                body();
                Debug.Log($"[DecoKit] {step} OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DecoKit] {step} FAILED: {e}");
            }
        }
    }
}
#endif
