#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.World;
using Suborned.World.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Glint, brief 077 (lighting side): the four fountain-island globe lamps' night light and the testbed leaves' gloss, checked in
    /// Latch's probe (Assets/Scenes/probe-planting077.unity, git-ignored), opened and NEVER saved. Everything here is in memory: lights, material
    /// copies, mask textures. Nothing in the kit, the probe's assets or any scene is written.
    /// Night = light.json night as HarbourDistrict's NightPhase built it (probe-leafshader077/night.json, Latch's read-only copy of LightPhase.night:
    /// moon, trilight, fog, night sky, night reflection cube, the night grade) with lens.json's night floor on top (LightPhase.NightFloor: the
    /// ambient and the moon), on the scene light and on the play look's own light. The lens's lift (AtmosphereLens, a play-mode volume) is not
    /// applied: an offset of 0.002 in linear, below what these frames can show.
    /// Globe lamp (light.json night.lamps kind "globe"): an unshadowed point light 2.31 m above each GlobeLamp_TB base; the opal's emission
    /// at its night level. Stand-ins of the street kind beside any old-town street-lamp head found near the square (the swap has no night of its
    /// own yet), so the square reads as it will with every lamp kind lit.
    /// Gloss: the botanical mask rebuilt in memory with other k (A = 1 - k x ORM.g, leaf rows k leaf, petal rows k petal), assigned to copies of
    /// DK_Leaf_TB and DK_Petal_TB.
    /// usage: -executeMethod Suborned.OldTown.Editor.Swap.Glint077.Run -glintStage sweep|final -glintOut out/scratch/glint/077/run
    ///        [-globeI 9] [-globeRange 5.8] [-opalNight 3.5] [-kLeaf 0.49] [-kPetal 0.64] [-kLeafB 0.8] [-kPetalB 0.8]</summary>
    public static class Glint077
    {
        const string Probe = "Assets/Scenes/probe-planting077.unity";
        const string NightJson = "Assets/Scenes/probe-leafshader077/night.json";
        const string Mats = "Assets/Scenes/probe-planting077/Kit/Materials";
        const string Orm = "the-suborned-store/testbed-art/cafe-plants/botanical-orm-v2.png";
        const string OrmSha = "c3574a250b1ea075f1b80b9c8fe51b805ab3e0dcb819ce5644d97c681b8d3130";
        const string StudyName = "DK street study camera";
        static string Repo => HarbourWorldBuilder.RepoRoot;
        static string A(string k, string d) => DecoKitStage.Arg(k, d);
        static float F(string k, float d) => DecoKitStage.Arg(k, d);
        static readonly Vector2 Cafe = new(-170.5f, -227.5f);

        public static void Run() => HarbourWorldBuilder.Batch(() =>
        {
            var t0 = DateTime.UtcNow;
            string stage = A("glintStage", "sweep"), outDir = Path.Combine(Repo, A("glintOut", "out/scratch/glint/077/" + stage));
            Directory.CreateDirectory(outDir);
            var log = new StringBuilder(); var rows = new List<string>();
            EditorSceneManager.OpenScene(Probe, OpenSceneMode.Single);
            DecoKitHarbour.Ocean();
            var play = SwapPlayLook.Find() ?? throw new InvalidDataException("glint077: no play camera in the probe");
            var look = play.GetComponent<OldTownPlayLook>();
            var study = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.name == StudyName);
            if (study) { study.enabled = true; study.farClipPlane = 2500f; study.nearClipPlane = .05f; study.useOcclusionCulling = false; }
            log.AppendLine($"probe {Probe}; play {play.name}; study {(study ? study.name : "none")}");

            // ---- the globe lamps
            var holder = GameObject.Find("planting077 (testbed swap, review probe)") ?? throw new InvalidDataException("glint077: no planting077 holder");
            var globes = holder.GetComponentsInChildren<Transform>(true).Where(t => t.parent == holder.transform && t.name.EndsWith("-> GlobeLamp_TB", StringComparison.Ordinal)).OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
            if (globes.Length != 4) throw new InvalidDataException($"glint077: {globes.Length} GlobeLamp_TB groups, expected 4");
            var lampRoot = new GameObject("glint077 night lamps (in memory)").transform;
            Color globeCol = Hex(A("globeColour", "#fff1d8"));
            var globeLights = new List<Light>();
            foreach (var g in globes)
            {
                var o = new GameObject("globe/" + g.name); o.transform.SetParent(lampRoot, false); o.transform.position = g.TransformPoint(0f, 2.31f, 0f);
                var l = o.AddComponent<Light>(); l.type = LightType.Point; l.color = globeCol; l.range = F("globeRange", 5.8f); l.intensity = F("globeI", 9f);
                l.shadows = LightShadows.None; l.cookie = null; l.renderMode = LightRenderMode.Auto; l.enabled = false; globeLights.Add(l);
                log.AppendLine($"globe light {g.name}: at {V(o.transform.position)} (base {V(g.position)}), lossyScale {V(g.lossyScale)}");
            }
            // street-kind stand-ins (light.json night.lamps street: spot 19, 6.5 m, 85 deg, #ffe6bf, down from the head less 0.4 m) beside old-town lamp heads near the square
            var cafeW = OldTownSwap.Old3(Cafe, 0f); var standIns = new List<Light>(); var seen = new HashSet<Vector2Int>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OrderBy(r => r.name, StringComparer.Ordinal))
            {
                string n = r.name; if (!(n.StartsWith("DK_Streetlamp", StringComparison.Ordinal) || n.StartsWith("DK_PlazaSphereLamp", StringComparison.Ordinal) || n.Contains("StreetLampGlobe") || n.StartsWith("DK_Lamppost", StringComparison.Ordinal))) continue;
                var b = r.bounds; var p = b.center; if (new Vector2(p.x - cafeW.x, p.z - cafeW.z).magnitude > 45f) continue;
                var key = new Vector2Int(Mathf.RoundToInt(p.x * 2), Mathf.RoundToInt(p.z * 2)); if (!seen.Add(key)) continue;
                var o = new GameObject("street/" + n); o.transform.SetParent(lampRoot, false); o.transform.position = new Vector3(p.x, b.max.y - .4f, p.z); o.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                var l = o.AddComponent<Light>(); l.type = LightType.Spot; l.color = Hex("#ffe6bf"); l.intensity = 19f; l.range = 6.5f; l.spotAngle = 85f; l.innerSpotAngle = 85f * .35f; l.shadows = LightShadows.None; l.enabled = false; standIns.Add(l);
            }
            log.AppendLine($"street stand-ins near the square: {standIns.Count} ({string.Join(", ", standIns.Select(l => l.name).Take(20))})");

            // ---- the opal (a copy on the four globes' renderers)
            var opal = AssetDatabase.LoadAssetAtPath<Material>(Mats + "/DK_Opal_TB.mat") ?? throw new FileNotFoundException("DK_Opal_TB");
            var opalRs = globes.SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).Where(r => r.sharedMaterials.Contains(opal)).ToArray();
            var opalCopy = new Material(opal) { name = "DK_Opal_TB (glint077 copy)" }; opalCopy.EnableKeyword("_EMISSION"); opalCopy.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; // the probe's .mat lost _EMISSION (m_ValidKeywords []); the kit copy in out/decokit-077 has it
            foreach (var r in opalRs) r.sharedMaterials = r.sharedMaterials.Select(m => m == opal ? opalCopy : m).ToArray();
            Color opalDay = opal.GetColor("_EmissionColor"); // linear #fff1d2 x 0.06
            Color opalBase = Hex("#fff1d2").linear;
            void Opal(float level) => opalCopy.SetColor("_EmissionColor", level <= 0.0601f && level >= 0.0599f ? opalDay : new Color(opalBase.r * level, opalBase.g * level, opalBase.b * level, level));
            log.AppendLine($"opal renderers {opalRs.Length}; day emission {opalDay}; probe .mat _EMISSION {opal.IsKeywordEnabled("_EMISSION")}, kit-copy keyword on the copy {opalCopy.IsKeywordEnabled("_EMISSION")}");

            // ---- the gloss variants
            var leaf = AssetDatabase.LoadAssetAtPath<Material>(Mats + "/DK_Leaf_TB.mat"); var petal = AssetDatabase.LoadAssetAtPath<Material>(Mats + "/DK_Petal_TB.mat");
            var plantRs = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(r => r.sharedMaterials.Any(m => m == leaf || m == petal)).ToArray();
            var asBuilt = plantRs.ToDictionary(r => r, r => r.sharedMaterials);
            log.AppendLine($"leaf/petal renderers {plantRs.Length}; leaf mask {AssetDatabase.GetAssetPath(leaf.GetTexture("_MetallicGlossMap"))}, _Smoothness {leaf.GetFloat("_Smoothness")}");
            var ormPath = Path.Combine(Path.GetDirectoryName(Repo), Orm); var ormBytes = File.ReadAllBytes(ormPath);
            if (Sha(ormBytes) != OrmSha) throw new InvalidDataException("glint077: ORM sha256 mismatch");
            var gloss = new Dictionary<string, (float kl, float kp)>
            {
                ["A"] = (F("kLeaf", .49f), F("kPetal", .64f)),
                ["B"] = (F("kLeafB", .80f), F("kPetalB", .80f)),
                ["C"] = (F("kLeafC", 1.0f), F("kPetalC", .90f)),
            };
            var glossMats = new Dictionary<string, Dictionary<Material, Material>>();
            foreach (var (id, k) in gloss)
            {
                if (id == "A" && Mathf.Approximately(k.kl, .49f) && Mathf.Approximately(k.kp, .64f)) { glossMats[id] = null; continue; } // as built
                var tex = Mask(ormBytes, k.kl, k.kp);
                var lc = new Material(leaf) { name = $"DK_Leaf_TB (k {k.kl})" }; lc.SetTexture("_MetallicGlossMap", tex);
                var pc = new Material(petal) { name = $"DK_Petal_TB (k {k.kp})" }; pc.SetTexture("_MetallicGlossMap", tex);
                glossMats[id] = new Dictionary<Material, Material> { [leaf] = lc, [petal] = pc };
            }
            void Gloss(string id) { var map = glossMats[id]; foreach (var r in plantRs) r.sharedMaterials = asBuilt[r].Select(m => map != null && m && map.TryGetValue(m, out var o) ? o : m).ToArray(); }

            // ---- night
            var night = (Dictionary<string, object>)Json.Parse(File.ReadAllText(Path.Combine(Repo, "unity", NightJson)));
            var nightSwitch = NightSwitch(night, look, log);
            void Lamps(bool on) { foreach (var l in globeLights.Concat(standIns)) l.enabled = on; }

            // ---- views
            var views = new Dictionary<string, (Vector2 f, float yaw, float size)>
            {
                ["cafe-square-y030"] = (Cafe, 30f, 14f), ["cafe-square-y210"] = (Cafe, 210f, 14f),
                ["close-troughs-y030"] = (new(-179.5f, -220.5f), 30f, 4.5f), ["close-island-y030"] = (new(-164.3f, -232.2f), 30f, 5f),
                ["close-island-y210"] = (new(-155.7f, -240.8f), 210f, 5f), ["fountain-y030"] = (new(-160f, -236.5f), 30f, 8f), ["fountain-y210"] = (new(-160f, -236.5f), 210f, 8f),
            };
            void Shot(Camera cam, string view, string name)
            {
                var v = views[view]; SwapShots.Play(cam, v.f, v.yaw, v.size);
                Texture2D px = null; DecoKitStage.WithShadows(200f, () => px = DecoKitStage.Render(cam, 1920, 1080, 2));
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), px.EncodeToPNG()); Object.DestroyImmediate(px); rows.Add(name);
            }
            void Warm(Camera cam) { SwapShots.Play(cam, Cafe, 30f, 14f); for (int i = 0; i < 3; i++) DecoKitStage.WithShadows(200f, () => Object.DestroyImmediate(DecoKitStage.Render(cam, 1920, 1080, 2))); }

            using (new RenderSession())
            {
                Object.DestroyImmediate(DecoKitStage.Render(play, 640, 360));
                if (stage == "sweep")
                {
                    // day gloss: A / B / C, play look and presentation look
                    nightSwitch(false); Lamps(false); Opal(.06f); Warm(play);
                    foreach (var id in new[] { "A", "B", "C" })
                    {
                        Gloss(id);
                        foreach (var v in new[] { "cafe-square-y030", "cafe-square-y210", "close-troughs-y030", "close-island-y030" }) Shot(play, v, $"day-play-{id}-{v}");
                        if (study) foreach (var v in new[] { "cafe-square-y030", "close-troughs-y030", "close-island-y030" }) Shot(study, v, $"day-presentation-{id}-{v}");
                    }
                    // night: the lamp intensity and opal sweep (gloss A), the fountain at yaw 30 and the square at yaw 30
                    Gloss("A"); nightSwitch(true); Warm(play);
                    Lamps(false); Opal(.06f); Shot(play, "fountain-y030", "night-off-fountain-y030");
                    foreach (var (i, o) in new[] { (6f, 2.2f), (9f, 2.2f), (9f, 3.5f), (12f, 3.5f), (16f, 3.5f) })
                    {
                        foreach (var l in globeLights) l.intensity = i; Lamps(true); Opal(o);
                        Shot(play, "fountain-y030", $"night-i{i:0}-o{o:0.0}-fountain-y030");
                        if (i == 9f && o == 3.5f) Shot(play, "cafe-square-y030", $"night-i{i:0}-o{o:0.0}-cafe-square-y030");
                    }
                    nightSwitch(false);
                }
                else
                {
                    // final: the chosen lamp values, gloss before (A) and after (B), night at yaw 30 and 210; day after for the record
                    float gi = F("globeI", 9f), go = F("opalNight", 3.5f); foreach (var l in globeLights) l.intensity = gi;
                    nightSwitch(true); Lamps(true); Opal(go); Warm(play);
                    Gloss("B");
                    foreach (var o in new[] { 0.06f, 1.5f, 2.5f, 3.5f }) { Opal(o); Shot(play, "fountain-y030", $"night-opal{o:0.0#}-B-fountain-y030"); }
                    foreach (var i in new[] { 5f, 7f }) { foreach (var l in globeLights) l.intensity = i; Opal(go); Shot(play, "fountain-y030", $"night-i{i:0}-B-fountain-y030"); }
                    foreach (var l in globeLights) l.intensity = gi; Opal(go);
                    foreach (var id in new[] { "A", "B" })
                    {
                        Gloss(id);
                        foreach (var v in new[] { "cafe-square-y030", "cafe-square-y210", "fountain-y030", "fountain-y210" }) Shot(play, v, $"night-{id}-{v}");
                    }
                    Lamps(false); Opal(.06f); Gloss("B");
                    foreach (var v in new[] { "cafe-square-y030", "cafe-square-y210" }) Shot(play, v, $"night-lampsoff-B-{v}");
                    nightSwitch(false); Warm(play);
                    foreach (var id in new[] { "A", "B" })
                    {
                        Gloss(id);
                        foreach (var v in new[] { "cafe-square-y210", "close-island-y210" }) Shot(play, v, $"day-play-{id}-{v}");
                        if (study) Shot(study, "cafe-square-y210", $"day-presentation-{id}-cafe-square-y210");
                    }
                }
            }
            // ---- cost: what each globe light reaches (renderers whose bounds meet its range)
            var all = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var l in globeLights)
            {
                var s = new Bounds(l.transform.position, Vector3.one * l.range * 2f);
                int n = all.Count(r => r.enabled && r.bounds.Intersects(s) && (r.bounds.ClosestPoint(l.transform.position) - l.transform.position).magnitude <= l.range);
                log.AppendLine($"cost {l.name}: renderers in range {n}");
            }
            log.AppendLine($"captures {rows.Count}: {string.Join(", ", rows)}");
            log.AppendLine($"seconds {(DateTime.UtcNow - t0).TotalSeconds:0.0}");
            File.WriteAllText(Path.Combine(outDir, "log.txt"), log.ToString());
            Debug.Log("[glint077] " + log);
        });

        /// <summary>(night?) on the scene light and the play look's own light: light.json night (night.json) plus lens.json's night floor.</summary>
        static Action<bool> NightSwitch(Dictionary<string, object> n, OldTownPlayLook look, StringBuilder log)
        {
            var sun = RenderSettings.sun ? RenderSettings.sun : Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(l => l.type == LightType.Directional);
            var probes = Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var day = (rot: sun.transform.rotation, col: sun.color, i: sun.intensity, sh: sun.shadowStrength, amb: RenderSettings.ambientMode, sky: RenderSettings.ambientSkyColor, eq: RenderSettings.ambientEquatorColor, gr: RenderSettings.ambientGroundColor,
                       probe: RenderSettings.ambientProbe, fog: RenderSettings.fogColor, fs: RenderSettings.fogStartDistance, fe: RenderSettings.fogEndDistance, fd: RenderSettings.fogDensity,
                       box: RenderSettings.skybox, refl: RenderSettings.defaultReflectionMode, cube: RenderSettings.customReflectionTexture, ri: RenderSettings.reflectionIntensity, probesI: probes.Select(p => p.intensity).ToArray());
            var lookDay = (rot: look.sunRotation, col: look.sunColour, i: look.sunIntensity, sh: look.sunShadowStrength, amb: look.ambientMode, sky: look.ambientSky, eq: look.ambientEquator, gr: look.ambientGround, ai: look.ambientIntensity, probe: look.ambientProbe, grade: look.playVolume ? look.playVolume.sharedProfile : null);
            var rotL = (System.Collections.IList)n["sunRotation"]; var moon = new Quaternion(Convert.ToSingle(rotL[0]), Convert.ToSingle(rotL[1]), Convert.ToSingle(rotL[2]), Convert.ToSingle(rotL[3]));
            var nightSky = AssetDatabase.LoadAssetAtPath<Material>((string)n["skybox"]); var nightCube = AssetDatabase.LoadAssetAtPath<Cubemap>((string)n["reflection"]);
            var nightGrade = AssetDatabase.LoadAssetAtPath<VolumeProfile>((string)n["grade"]);
            float ri = Convert.ToSingle(n["reflectionIntensity"]), pi = Convert.ToSingle(n["probeIntensity"]);
            // lens.json night floor (LightPhase.NightFloor)
            var lens = JsonUtility.FromJson<AtmosphereLens.Lens>((Resources.Load<TextAsset>(AtmosphereLens.ResourcePath) ?? throw new FileNotFoundException("lens.json")).text).night;
            Color nSky = AtmosphereLens.Scaled(lens.ambient.sky.hex, lens.ambient.sky.value), nEq = AtmosphereLens.Scaled(lens.ambient.equator.hex, lens.ambient.equator.value), nGr = AtmosphereLens.Scaled(lens.ambient.ground.hex, lens.ambient.ground.value);
            Color moonCol = string.IsNullOrEmpty(lens.moonColour) ? Col(n, "sunColour") : AtmosphereLens.Colour(lens.moonColour); float moonI = lens.moonIntensity > 0f ? lens.moonIntensity : Convert.ToSingle(n["sunIntensity"]);
            log.AppendLine($"night: moon {moonI} {moonCol}, shadow {lens.shadowStrength}, ambient {nSky} / {nEq} / {nGr}, grade {(nightGrade ? nightGrade.name : "none")}, sky {(nightSky ? nightSky.name : "none")}, cube {(nightCube ? nightCube.name : "none")}, reflection {ri}, probes {probes.Length} at {pi}");
            log.AppendLine($"day look: sun {lookDay.i} {lookDay.col}, grade {(lookDay.grade ? lookDay.grade.name : "none")}");
            return toNight =>
            {
                if (toNight)
                {
                    sun.transform.rotation = moon; sun.color = moonCol; sun.intensity = moonI; sun.shadowStrength = lens.shadowStrength > 0f ? lens.shadowStrength : day.sh;
                    RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = nSky; RenderSettings.ambientEquatorColor = nEq; RenderSettings.ambientGroundColor = nGr;
                    RenderSettings.fogColor = Col(n, "fogColour"); RenderSettings.fogStartDistance = Convert.ToSingle(n["fogStart"]); RenderSettings.fogEndDistance = Convert.ToSingle(n["fogEnd"]); RenderSettings.fogDensity = Convert.ToSingle(n["fogDensity"]);
                    if (nightSky) RenderSettings.skybox = nightSky;
                    if (nightCube) { RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom; RenderSettings.customReflectionTexture = nightCube; }
                    RenderSettings.reflectionIntensity = ri; foreach (var p in probes) p.intensity = pi >= 0f ? pi : p.intensity;
                    DynamicGI.UpdateEnvironment();
                    look.sunRotation = moon; look.sunColour = moonCol; look.sunIntensity = moonI; look.sunShadowStrength = sun.shadowStrength; look.ambientMode = AmbientMode.Trilight;
                    look.ambientSky = nSky; look.ambientEquator = nEq; look.ambientGround = nGr; look.ambientIntensity = 1f;
                    look.ambientProbe = OldTownPlayLook.Flatten(RenderSettings.ambientProbe);
                    if (look.playVolume && nightGrade) look.playVolume.sharedProfile = nightGrade;
                }
                else
                {
                    sun.transform.rotation = day.rot; sun.color = day.col; sun.intensity = day.i; sun.shadowStrength = day.sh;
                    RenderSettings.ambientMode = day.amb; RenderSettings.ambientSkyColor = day.sky; RenderSettings.ambientEquatorColor = day.eq; RenderSettings.ambientGroundColor = day.gr;
                    RenderSettings.fogColor = day.fog; RenderSettings.fogStartDistance = day.fs; RenderSettings.fogEndDistance = day.fe; RenderSettings.fogDensity = day.fd;
                    RenderSettings.skybox = day.box; RenderSettings.defaultReflectionMode = day.refl; RenderSettings.customReflectionTexture = day.cube; RenderSettings.reflectionIntensity = day.ri;
                    for (int i = 0; i < probes.Length; i++) probes[i].intensity = day.probesI[i];
                    RenderSettings.ambientProbe = day.probe; DynamicGI.UpdateEnvironment(); RenderSettings.ambientProbe = day.probe;
                    look.sunRotation = lookDay.rot; look.sunColour = lookDay.col; look.sunIntensity = lookDay.i; look.sunShadowStrength = lookDay.sh; look.ambientMode = lookDay.amb; look.ambientSky = lookDay.sky; look.ambientEquator = lookDay.eq;
                    look.ambientGround = lookDay.gr; look.ambientIntensity = lookDay.ai; look.ambientProbe = lookDay.probe; if (look.playVolume) look.playVolume.sharedProfile = lookDay.grade;
                }
            };
        }

        /// <summary>PlantingSwap077.Mask's rule (A = round(255 - k x ORM.g), k by atlas row: v &gt;= 0.5 leaf, else petal) into a linear texture with mips, in memory.</summary>
        static Texture2D Mask(byte[] orm, float kLeaf, float kPetal)
        {
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!src.LoadImage(orm)) throw new InvalidDataException("ORM");
            int w = src.width, h = src.height; var px = src.GetPixels32(); var o = new Color32[px.Length];
            for (int y = 0; y < h; y++) { float kk = (y + .5f) / h >= .5f ? kLeaf : kPetal; for (int x = 0; x < w; x++) { int i = y * w + x; o[i] = new Color32(0, 0, 0, (byte)Math.Round(255.0 - kk * px[i].g, MidpointRounding.AwayFromZero)); } }
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 8, name = $"botanical mask k {kLeaf}/{kPetal} (in memory)" };
            dst.SetPixels32(o); dst.Apply(true); Object.DestroyImmediate(src); return dst;
        }

        static Color Col(Dictionary<string, object> n, string k) { var l = (System.Collections.IList)n[k]; return new Color(Convert.ToSingle(l[0]), Convert.ToSingle(l[1]), Convert.ToSingle(l[2]), l.Count > 3 ? Convert.ToSingle(l[3]) : 1f); }
        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : throw new InvalidDataException(h);
        static string V(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:0.###}, {1:0.###}, {2:0.###})", v.x, v.y, v.z);
        static string Sha(byte[] b) { using var s = System.Security.Cryptography.SHA256.Create(); return string.Concat(s.ComputeHash(b).Select(x => x.ToString("x2"))); }
    }
}
#endif
