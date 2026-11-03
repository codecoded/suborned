#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap, second build (Latch; Aster 2026-10-29: "the play view is washed out under the old stage light"; Sky's play-only
    /// performance cuts): the old-built scene gets its own play camera, "Play camera (our play look)", orthographic 14 at pitch 38 as HandoverHost,
    /// carrying our TacticalShadows (the fit read from our pipeline's scene: shadow distance and cascades fitted to the play view, SSAO off at
    /// ortho) and OldTownPlayLook (fog and the old renderer's Harbour fog off, the old grade / bloom / soft focus volumes at weight 0 and our play
    /// grade at 1, the sun flare off, the baked crowd and cars culled, MSAA off), all only while it renders. The old materials stay. The
    /// presentation cameras (the study camera, the flyby) keep the old full look. The play grade is our pipeline's grade profile, copied beside
    /// the scene (Assets/Suborned/OldTownArt/Swap/&lt;scene&gt;-play-grade.asset).</summary>
    internal static class SwapPlayLook
    {
        internal const string Name = "Play camera (our play look)";
        const string Dir = "Assets/Suborned/OldTownArt/Swap";

        internal static Camera Make(OldTownSwap.Report rep, SwapOurs ours, Camera study, Light sun, string scene)
        {
            var go = new GameObject(Name);
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(study);
            cam.enabled = false; cam.orthographic = true; cam.orthographicSize = 14f; cam.nearClipPlane = .3f; cam.farClipPlane = 1660f; cam.useOcclusionCulling = false;
            go.transform.rotation = Quaternion.Euler(38f, 30f, 0f);
            var add = go.GetComponent<UniversalAdditionalCameraData>() ?? go.AddComponent<UniversalAdditionalCameraData>();
            add.renderPostProcessing = true; add.antialiasing = AntialiasingMode.None; add.renderShadows = true;
            cam.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.EveryFrame);

            var ts = go.AddComponent<TacticalShadows>();
            if (!string.IsNullOrEmpty(ours.PlayShadowsJson)) JsonUtility.FromJsonOverwrite(ours.PlayShadowsJson, ts);
            ts.focusDepth = 160f;  // SwapShots.Play's stand-off (HandoverHost's 160 m); a rig sets it as it moves

            Volume play = null;
            if (ours.PlayGrade)
            {
                Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", Dir)));
                var path = $"{Dir}/{scene}-play-grade.asset";
                AssetDatabase.DeleteAsset(path);
                var src = AssetDatabase.GetAssetPath(ours.PlayGrade);
                VolumeProfile profile = !string.IsNullOrEmpty(src) && AssetDatabase.CopyAsset(src, path) ? AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) : null;
                if (profile == null) { profile = Object.Instantiate(ours.PlayGrade); AssetDatabase.CreateAsset(profile, path); }
                var vg = new GameObject("Play grade (ours, weight 0 except while the play camera renders)");
                play = vg.AddComponent<Volume>(); play.isGlobal = true; play.priority = 100f; play.weight = 0f; play.sharedProfile = profile;
            }
            var look = go.AddComponent<OldTownPlayLook>();
            look.playVolume = play;
            look.presentationVolumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(v => v != play).ToArray();
            look.presentationOnly = Object.FindObjectsByType<LensFlareComponentSRP>(FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Behaviour>().ToArray();
            if (ours.PlaySun is { } ps && sun)
            {
                look.lightSwap = true; look.sun = sun; look.sunRotation = ps.rot; look.sunColour = ps.colour; look.sunIntensity = ps.intensity; look.sunShadowStrength = ps.shadow;
                var a = ours.PlayAmbient; look.ambientMode = a.mode; look.ambientSky = a.sky; look.ambientEquator = a.equator; look.ambientGround = a.ground; look.ambientIntensity = a.intensity;
                look.ambientProbe = a.probe != null && a.probe.Any(v => Mathf.Abs(v) > 1e-5f) ? a.probe : new float[0];
            }

            int crowd = 0;
            foreach (var root in SwapPerf.CrowdRoots)
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name == root).ToArray())
                    foreach (var c in t.GetComponentsInChildren<Transform>(true)) { c.gameObject.layer = OldTownPlayLook.CrowdLayer; crowd++; }
            rep.Log($"play camera: \"{Name}\" (ortho 14, pitch 38, MSAA off), TacticalShadows {(string.IsNullOrEmpty(ours.PlayShadowsJson) ? "at its defaults (none read from our scene)" : $"from our scene (near {ts.nearBand}, far {ts.farBand}, {ts.cascades} cascades, ortho SSAO {(ts.orthoSsao ? "on" : "off")})")}; " +
                    $"OldTownPlayLook: fog off, \"{string.Join("\", \"", look.featuresOff)}\" off, {look.presentationVolumes.Length} presentation volumes at 0 ({string.Join(", ", look.presentationVolumes.Select(v => v.name))}), " +
                    $"play grade {(play ? AssetDatabase.GetAssetPath(play.sharedProfile) : "MISSING")}, {look.presentationOnly.Length} sun flares off; our play light {(look.lightSwap ? $"on (sun {look.sunRotation.eulerAngles} x{look.sunIntensity}, ambient {look.ambientMode}{(look.ambientProbe.Length == 27 ? " + probe" : "")})" : "off (no sun read)")}; {crowd} crowd and traffic objects on layer {OldTownPlayLook.CrowdLayer} (culled by the play camera)");
            return cam;
        }

        internal static Camera Find() => Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.name == Name);
    }
}
#endif
