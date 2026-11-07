using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Play;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Suborned.World.Play
{
    /// <summary>The game's scene (Sky, 2026-10-30: "I want to be playing the old town"): OldTown.unity, built by the chain export → HarbourWorldBuilder → OldTownSwap.Build, with the Handover host
    /// installed here on the swap's play camera: the frame moved the 206 m the swap stands the plan up Unity z, the camera the host's (the swap's own OldTownPlayLook and TacticalShadows, plus the
    /// occlusion cut, which the swap's camera lacked), every other camera off. Batch: -executeMethod Suborned.World.Play.OldTownPlay.Install</summary>
    public static class OldTownPlay
    {
        public const string ScenePath = "Assets/Scenes/OldTown.unity";
        const string PlayCam = "Play camera (our play look)";
        const float DZ = Suborned.OldTown.OldTownFrame.DZ; // OldTownSwap.DZ
        const string Export = "out/world/harbour-district.world.json";

        public static void Install()
        {
            try { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); InstallOpen(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void InstallOpen()
        {
            var scene = EditorSceneManager.GetActiveScene(); if (scene.path != ScenePath) throw new InvalidOperationException($"OldTownPlay: {scene.path} is open, not {ScenePath}");
            var cam = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.name == PlayCam) ?? throw new InvalidDataException($"OldTownPlay: no \"{PlayCam}\": rebuild the swap");
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { if (c == cam) continue; c.enabled = false; if (c.CompareTag("MainCamera")) c.tag = "Untagged"; }
            cam.enabled = true; cam.tag = "MainCamera"; cam.GetUniversalAdditionalCameraData().dithering = true; cam.allowHDR = true;
            if (!cam.GetComponent<ArchitectureFade>())
            {
                var light = Json.Parse(File.ReadAllText(Path.Combine(HarbourWorldBuilder.RepoRoot, "unity/Assets/Suborned/Editor/World/light.json")));
                var af = cam.gameObject.AddComponent<ArchitectureFade>(); af.ghostShadow = J.F(J.Get(light, "fade"), "ghostShadow");
            }
            var host = PlayInstall.Install(WorldReader.Load(Path.Combine(HarbourWorldBuilder.RepoRoot, Export)), DZ);
            host.playCamera = PlayCam;
            var look = cam.GetComponent<Suborned.OldTown.OldTownPlayLook>();
            // today's lens (AtmosphereLens' play rig: bloom threshold, the clamp that holds the glitter fix, the play saturation and lift) must win over the scene's grade, as in our scene: the swap's play grade stood at priority 100 over it, so the scene grade's bloom (threshold 0.48, no clamp) washed the view out
            if (look && look.playVolume) { look.playVolume.priority = 0f; EditorUtility.SetDirty(look.playVolume); }
            Debug.Log($"OldTownPlay: host on \"{PlayCam}\" (MSAA {cam.allowMSAA}, crowd layer in the mask {(cam.cullingMask & (1 << Suborned.OldTown.OldTownPlayLook.CrowdLayer)) != 0}, presentation lens {look && look.presentationLens}, fog off {look && look.fogOff}, sun flares off {(look ? look.presentationOnly.Length : 0)}, light swap {look && look.lightSwap})");
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>What the player carries, by folder (the build report's packed assets): build-contents.json beside build.json. The switch's check: no baked people, no traffic, no tram experiment.</summary>
        public static void WriteContents(BuildReport report, string dir)
        {
            var by = new Dictionary<string, ulong>(); ulong total = 0;
            foreach (var pa in report.packedAssets)
                foreach (var c in pa.contents)
                {
                    var parts = (c.sourceAssetPath ?? "").Split('/'); string key = parts.Length >= 5 ? string.Join("/", parts.Take(5)) : string.Join("/", parts);
                    by.TryGetValue(key, out var v); by[key] = v + c.packedSize; total += c.packedSize;
                }
            var rows = by.OrderByDescending(kv => kv.Value).Take(60).Select(kv => $"  \"{kv.Key.Replace("\"", "'")}\": {kv.Value / 1048576}");
            File.WriteAllText(Path.Combine(dir, "build-contents.json"), "{\n \"note\": \"MB packed, by folder\",\n \"totalMB\": " + total / 1048576 + ",\n" + string.Join(",\n", rows) + "\n}\n");
        }
    }
}
