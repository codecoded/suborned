using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Perf
{
    /// <summary>Applies a variant's A/B switches (Redline item 0) to the running player and puts everything back for "as built":
    /// SSAO (URP's feature settings are internal, so by reflection: AfterOpaque and Source), the play camera's shadow cascades
    /// (TacticalShadows) and shadows, the architecture fade, the shadow proxies, every face and module shadows (072-E) and the surface detail (072-F). Each Apply sets every switch, so the order of
    /// variants never matters.</summary>
    public sealed class PerfSwitches
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly UniversalAdditionalCameraData data;
        readonly TacticalShadows shadows;
        readonly ArchitectureFade fade;
        readonly PerfProxies proxies;
        readonly EveryFace everyFace;
        readonly ScriptableRendererFeature ssao;
        readonly object ssaoSettings;
        readonly FieldInfo afterOpaque, source;
        readonly object builtSource, depthSource, normalsSource;
        readonly bool builtAfterOpaque, builtActive, builtFade, builtWhole;
        readonly int builtCascades;
        // brief 075 Perf A/B: the old town's switches
        readonly OldTargets old;

        /// <summary>What the four 075ab switches found: crowd renderers, pipelines, MSAA, DOF and flare.</summary>
        public string AsBuilt075 => old.Describe();

        /// <summary>What the player's SSAO is as built ("on, after opaque True, source Depth"), or why it cannot be switched.</summary>
        public string SsaoAsBuilt { get; }

        public PerfSwitches(Camera cam, TacticalShadows shadows, ArchitectureFade fade, PerfProxies proxies, EveryFace everyFace = null)
        {
            this.shadows = shadows; this.fade = fade; this.proxies = proxies; this.everyFace = everyFace;
            data = cam.GetUniversalAdditionalCameraData();
            old = new OldTargets(cam);
            // brief 075 Perf A/B: the old-built town's camera has no TacticalShadows or ArchitectureFade; the switches that need them refuse there
            if (shadows) builtCascades = shadows.cascades;
            if (fade) { builtFade = fade.fade; builtWhole = fade.wholeTiles; }
            try
            {
                var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
                foreach (var f in urp.rendererDataList[0].rendererFeatures) if (f is ScreenSpaceAmbientOcclusion) ssao = f;
                if (!ssao) { SsaoAsBuilt = "no SSAO feature"; return; }
                ssaoSettings = typeof(ScreenSpaceAmbientOcclusion).GetField("m_Settings", Any).GetValue(ssao);
                var t = ssaoSettings.GetType();
                afterOpaque = t.GetField("AfterOpaque", Any); source = t.GetField("Source", Any);
                builtActive = ssao.isActive; builtAfterOpaque = (bool)afterOpaque.GetValue(ssaoSettings); builtSource = source.GetValue(ssaoSettings);
                depthSource = Enum.ToObject(source.FieldType, 0); // DepthSource.Depth
                normalsSource = Enum.ToObject(source.FieldType, 1); // DepthSource.DepthNormals
                SsaoAsBuilt = $"{(ssao.isActive ? "on" : "off")}, after opaque {builtAfterOpaque}, source {builtSource}; " + (shadows ? $"the orthographic play camera {(shadows.orthoSsao ? "keeps it" : "turns it off (064 A3 item 1; ssao depth|normals turns it back on)")}" : "no TacticalShadows on this camera: SSAO runs on the orthographic view as on any other");
            }
            catch (Exception e) { SsaoAsBuilt = "cannot switch: " + e.Message; ssaoSettings = null; }
        }

        public void Apply(PerfVariant v)
        {
            // brief 075 Perf A/B first: the pipeline decides which asset the MSAA and texture switches act on
            old.Apply(v);
            if ((!shadows || !fade) && (!string.IsNullOrEmpty(v.ssao) && v.ssao != "on" || v.cascades > 0 || !string.IsNullOrEmpty(v.fade) || v.fadeOff))
                throw new InvalidOperationException($"Perf: variant `{v.name}` switches SSAO, cascades or the fade, but this camera has no TacticalShadows / ArchitectureFade (the old-built town)");
            string mode = string.IsNullOrEmpty(v.ssao) ? "on" : v.ssao;
            if (mode != "on" && mode != "depth" && mode != "normals" && mode != "off") throw new InvalidOperationException($"Perf: variant `{v.name}` ssao `{mode}`: on, depth, normals or off");
            if (mode != "on" && ssaoSettings == null) throw new InvalidOperationException($"Perf: variant `{v.name}` switches SSAO, but {SsaoAsBuilt}");
            if (ssaoSettings != null)
            {
                ssao.SetActive(mode switch { "off" => false, "on" => builtActive, _ => true });
                afterOpaque.SetValue(ssaoSettings, mode switch { "depth" => true, "normals" => false, _ => builtAfterOpaque });
                source.SetValue(ssaoSettings, mode switch { "depth" => depthSource, "normals" => normalsSource, _ => builtSource });
            }
            // brief 064 A3 item 1: as built, TacticalShadows turns SSAO off while the orthographic play camera renders; an explicit depth or
            // normals variant (Redline's every-ssao) brings it back there, so the variant measures the item's saving
            if (shadows)
            {
                shadows.orthoSsao = mode is "depth" or "normals";
                shadows.cascades = v.cascades > 0 ? v.cascades : builtCascades;
            }
            data.renderShadows = !v.shadowsOff;
            string f = string.IsNullOrEmpty(v.fade) ? "on" : v.fade;
            if (f != "on" && f != "tile" && f != "reach" && f != "off") throw new InvalidOperationException($"Perf: variant `{v.name}` fade `{f}`: on, tile, reach or off");
            if (fade)
            {
                fade.fade = builtFade && !v.fadeOff && f != "off";
                fade.wholeTiles = f switch { "tile" => true, "reach" => false, _ => builtWhole };
            }
            if (v.UsesProxies && proxies is not { Count: > 0 }) throw new InvalidOperationException($"Perf: variant `{v.name}` switches shadow proxies, but this scene has none");
            if (proxies != null) proxies.Set(v.proxies == "on" || (v.proxies != "off" && proxies.BuiltOn));
            // brief 072-E: the every-face A/B
            string ef = string.IsNullOrEmpty(v.everyFace) ? "on" : v.everyFace, ms = string.IsNullOrEmpty(v.moduleShadows) ? "proxy" : v.moduleShadows;
            if (ef != "on" && ef != "off") throw new InvalidOperationException($"Perf: variant `{v.name}` everyFace `{ef}`: on or off");
            if (ms != "proxy" && ms != "lod0") throw new InvalidOperationException($"Perf: variant `{v.name}` moduleShadows `{ms}`: proxy or lod0");
            if (ef == "off" && (everyFace == null || !everyFace.HasToday)) throw new InvalidOperationException($"Perf: variant `{v.name}` switches every face off, but this player has no `today` walls (EveryFace; development builds only)");
            if (v.moduleShadows == "lod0" && proxies is not { ModuleCount: > 0 }) throw new InvalidOperationException($"Perf: variant `{v.name}` switches module shadows, but this scene has no module proxies");
            if (everyFace != null) everyFace.Set(ef == "on");
            if (proxies != null) proxies.SetModules(string.IsNullOrEmpty(v.moduleShadows) ? proxies.ModulesBuiltOn : ms == "proxy");
            // brief 072-F: the texture A/B (a global float; as built = on)
            string sd = string.IsNullOrEmpty(v.surfaceDetail) ? "on" : v.surfaceDetail;
            if (sd != "on" && sd != "off") throw new InvalidOperationException($"Perf: variant `{v.name}` surfaceDetail `{sd}`: on or off");
            LookLab.SetSurfaceDetail(sd == "on");
        }

        public void Restore() => Apply(new PerfVariant { name = "as built" });
    }

    /// <summary>Brief 075 Perf A/B (Redline, perf-075ab.json): the switches for the old-built town, each echoed by the report (PerfVariant is
    /// serialized whole) and each refused on a scene without its target, so a cut is never measured as the build against itself.
    /// On the swap's play camera ("Play camera (our play look)", OldTownPlayLook: crowd culled, MSAA off, no soft focus or flare as built) each
    /// switch acts on what that camera draws (Latch, 2026-10-29): oldCrowd = the crowd layer in its culling mask (and the crowd renderers),
    /// msaa = its allowMSAA (on above 1) with the asset's sample count, oldLens = OldTownPlayLook.presentationLens (the old depth of field and the
    /// sun flare in the play view). "off" is the play camera as built; "on" (or msaa 2-8) puts the old cost back, so a cut's saving is on - off.</summary>
    sealed class OldTargets
    {
        /// <summary>OldTownPlayLook.CrowdLayer (the swap puts the old town's baked crowd and traffic on it; Suborned.OldTown is not referenced here).</summary>
        const int CrowdLayer = 29;
        readonly Camera cam; readonly Behaviour playLook; readonly FieldInfo lensField;
        readonly int builtMask; readonly bool builtAllowMsaa, builtLens;
        public static readonly string[] CrowdRoots = { "District crowd and traffic", "Southside crowd and traffic" };
        readonly List<(Renderer r, bool on)> crowd = new();
        readonly List<(DepthOfField dof, bool active)> dofs = new();
        readonly LensFlareComponentSRP flare; readonly bool flareOn;
        readonly RenderPipelineAsset builtPipeline, ours;
        readonly Dictionary<UniversalRenderPipelineAsset, (int msaa, bool depth, bool opaque)> assets = new();
        readonly int crowdRoots;

        public OldTargets(Camera cam)
        {
            this.cam = cam;
            playLook = cam ? cam.GetComponent("OldTownPlayLook") as Behaviour : null;
            if (playLook)
            {
                lensField = playLook.GetType().GetField("presentationLens") ?? throw new InvalidOperationException("Perf: OldTownPlayLook has no presentationLens (a player older than the 075 play-camera switches)");
                builtMask = cam.cullingMask; builtAllowMsaa = cam.allowMSAA; builtLens = (bool)lensField.GetValue(playLook);
            }
            foreach (var name in CrowdRoots)
            {
                var root = GameObject.Find(name); if (!root) continue;
                crowdRoots++;
                foreach (var r in root.GetComponentsInChildren<Renderer>(true)) crowd.Add((r, r.enabled));
            }
            foreach (var vol in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include))
                if (vol.sharedProfile && vol.sharedProfile.TryGet<DepthOfField>(out var d) && d.active && d.mode.value != DepthOfFieldMode.Off) dofs.Add((d, d.active));
            var sun = RenderSettings.sun;
            if (!sun) foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)) if (l.type == LightType.Directional && l.enabled) { sun = l; break; }
            if (sun && sun.TryGetComponent(out flare)) flareOn = flare.enabled;
            builtPipeline = QualitySettings.renderPipeline ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
            ours = GraphicsSettings.defaultRenderPipeline;
            Remember(builtPipeline as UniversalRenderPipelineAsset); Remember(ours as UniversalRenderPipelineAsset);
        }

        void Remember(UniversalRenderPipelineAsset a) { if (a && !assets.ContainsKey(a)) assets[a] = (a.msaaSampleCount, a.supportsCameraDepthTexture, a.supportsCameraOpaqueTexture); }

        bool HasOldPipeline => builtPipeline && ours && builtPipeline != ours;

        public string Describe() =>
            $"camera {(cam ? cam.name : "none")}{(playLook ? $" (OldTownPlayLook: crowd layer {CrowdLayer} {((builtMask & (1 << CrowdLayer)) != 0 ? "drawn" : "culled")}, allowMSAA {builtAllowMsaa}, presentation lens {(builtLens ? "on" : "off")} as built; the switches act on this camera)" : "")}; " +
            $"oldCrowd: {crowdRoots} roots, {crowd.Count} renderers ({crowd.FindAll(c => c.on).Count} on as built); pipeline: built {(builtPipeline ? builtPipeline.name : "none")}, ours {(ours ? ours.name : "none")}{(HasOldPipeline ? "" : " (no old pipeline in this player)")}; " +
            $"msaa as built {string.Join(", ", ListAssets())}; oldLens: {dofs.Count} DOF overrides, sun flare {(flare ? (flareOn ? "on" : "off") : "none")}";

        IEnumerable<string> ListAssets() { foreach (var kv in assets) yield return $"{kv.Key.name} {kv.Value.msaa}x, depth {kv.Value.depth}, opaque {kv.Value.opaque}"; }

        public void Apply(PerfVariant v)
        {
            // pipeline: the quality level's pipeline as built, or ours
            string p = string.IsNullOrEmpty(v.pipeline) ? "" : v.pipeline;
            if (p != "" && p != "old" && p != "ours") throw new InvalidOperationException($"Perf: variant `{v.name}` pipeline `{p}`: old or ours");
            if (p == "old" && !HasOldPipeline) throw new InvalidOperationException($"Perf: variant `{v.name}` asks for the old pipeline, but this player carries none (quality {(builtPipeline ? builtPipeline.name : "none")} = default {(ours ? ours.name : "none")})");
            if (p == "ours" && !ours) throw new InvalidOperationException($"Perf: variant `{v.name}` asks for our pipeline, but GraphicsSettings has no default pipeline");
            var want = p == "ours" ? ours : builtPipeline;
            if (QualitySettings.renderPipeline != want) QualitySettings.renderPipeline = want;
            var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline; Remember(urp);
            var built = assets[urp];
            // put every asset back first, then switch the active one
            foreach (var kv in assets) { kv.Key.msaaSampleCount = kv.Value.msaa; kv.Key.supportsCameraDepthTexture = kv.Value.depth; kv.Key.supportsCameraOpaqueTexture = kv.Value.opaque; }
            // msaa
            if (v.msaa != 0 && v.msaa != 1 && v.msaa != 2 && v.msaa != 4 && v.msaa != 8) throw new InvalidOperationException($"Perf: variant `{v.name}` msaa {v.msaa}: 1, 2, 4 or 8");
            urp.msaaSampleCount = v.msaa > 0 ? v.msaa : built.msaa;
            if (playLook) cam.allowMSAA = v.msaa > 0 ? v.msaa > 1 : builtAllowMsaa;  // the play camera keeps MSAA off as built: msaa 2-8 turns it back on there
            // old crowd
            string c = string.IsNullOrEmpty(v.oldCrowd) ? "" : v.oldCrowd;
            if (c != "" && c != "on" && c != "off") throw new InvalidOperationException($"Perf: variant `{v.name}` oldCrowd `{c}`: on or off");
            if (c != "" && crowdRoots == 0) throw new InvalidOperationException($"Perf: variant `{v.name}` switches the old crowd, but this scene has no {string.Join(" / ", CrowdRoots)}");
            foreach (var (r, on) in crowd) if (r) r.enabled = c == "off" ? false : on;
            if (playLook) cam.cullingMask = c == "on" ? builtMask | (1 << CrowdLayer) : c == "off" ? builtMask & ~(1 << CrowdLayer) : builtMask;  // the play camera culls the crowd layer as built
            // old lens
            string l = string.IsNullOrEmpty(v.oldLens) ? "" : v.oldLens;
            if (l != "" && l != "on" && l != "off") throw new InvalidOperationException($"Perf: variant `{v.name}` oldLens `{l}`: on or off");
            if (l != "" && dofs.Count == 0 && !flare) throw new InvalidOperationException($"Perf: variant `{v.name}` switches the old lens, but this scene has no depth-of-field override and no sun flare");
            foreach (var (d, a) in dofs) d.active = l == "off" ? false : a;
            if (flare) flare.enabled = l == "off" ? false : flareOn;
            if (playLook) lensField.SetValue(playLook, l == "on" || (l == "" && builtLens));  // the play camera draws no soft focus or flare as built: "on" brings the old lens into the play view
            if (l == "off") { urp.supportsCameraDepthTexture = false; urp.supportsCameraOpaqueTexture = false; }
        }
    }
}
