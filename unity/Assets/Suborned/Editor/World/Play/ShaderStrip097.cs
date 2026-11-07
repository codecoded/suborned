using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Suborned.World.Play
{
    /// <summary>Brief 097 (Redline's render audit item 5) and brief 098's fast loop (Splice, Sky 2026-11-07 "yes"): player builds drop the shader variants of
    /// keyword states the game never draws, so a build compiles and ships fewer programs. On Metal a lit forward fragment variant costs about 5 s of CPU
    /// (DecoKit/Lit Cutaway's 10,368 took 2,638 s on the Studio): every variant kept is build time.
    /// This runs after URP's own stripper (callbackOrder 10 &gt; 0), so it sees what the pipeline asset and the renderer keep. Each rule names its reason;
    /// the 2026-11-07 census (briefs/098/splice/variants.md) found the first group already stripped by URP: they stay as guards, so a settings change
    /// can't bring them back unseen. NOT stripped (tried 2026-11-06, the whole zone drew white): _LIGHT_LAYERS while the asset supports light layers.
    /// Off with -keepVariants (everything URP keeps), -keepCrossFade, -keepCookieOff. Counts per run: strip097.json in the build's folder, with each
    /// DecoKit pass's variants in and kept and the keywords the kept ones hold. A scratch build may add ShaderTrace098 (-scratchVariants traced).</summary>
    public sealed class ShaderStrip097 : IPreprocessShaders, IProcessSceneWithReport, IPreprocessBuildWithReport
    {
        public int callbackOrder => 10;

        /// <summary>One keyword state the game never draws. `on`: every variant holding the keyword goes; otherwise every variant without it, in a stage
        /// whose pass declares it (the keyword is then always on in play).</summary>
        internal sealed class Rule
        {
            public readonly string Keyword, Reason; public readonly bool On;
            public Rule(string keyword, bool on, string reason) { Keyword = keyword; On = on; Reason = reason; }
            public string Id => On ? Keyword : Keyword + " (off)";
        }

        internal const string CrossFade = "LOD_FADE_CROSSFADE", Cookies = "_LIGHT_COOKIES";
        internal static readonly Rule[] Always =
        {
            // guards: URP strips these already (none reached this callback in the 2026-11-07 census)
            new("_ADDITIONAL_LIGHTS_VERTEX", true, "Forward+ (PC_Renderer m_RenderingMode 2): the clustered light loop, never per-vertex lights"),
            new("_MAIN_LIGHT_SHADOWS_SCREEN", true, "no screen-space shadows renderer feature (PC_Renderer)"),
            new("_DBUFFER_MRT1", true, "the URP decals are Screen Space (PC_Renderer Decals technique 2): no DBuffer"),
            new("_DBUFFER_MRT2", true, "the URP decals are Screen Space: no DBuffer"),
            new("_DBUFFER_MRT3", true, "the URP decals are Screen Space: no DBuffer"),
            new("_SCREEN_SPACE_IRRADIANCE", true, "no Surface Cache GI"),
            new("_SCREEN_SPACE_REFLECTION", true, "no screen-space reflections feature (PC_Renderer)"),
            new("_SHADOWS_SOFT_LOW", true, "soft shadows Medium (PC_RPAsset m_SoftShadowQuality 2; TacticalShadows sets Medium on the play camera)"),
            new("META_QUEST_LIGHTUNROLL", true, "no XR (Meta Quest)"),
            new("META_QUEST_ORTHO_PROJ", true, "no XR (Meta Quest)"),
            new("META_QUEST_NO_SPOTLIGHTS_LIGHT_LOOP", true, "no XR (Meta Quest)"),
            // brief 098: states URP keeps that the game never draws
            new(CrossFade, true, "no LODGroup fades: PC_RPAsset m_EnableLODCrossFade 1 keeps the variants, but every LODGroup in the built scenes has fade mode None " +
                "(OldTown.unity 2,204, the lean copy 1,914; PlantingSwap077 makes them with LODFadeMode.None; no player code sets fadeMode). Checked per build: " +
                "a built scene with a fading LODGroup keeps the variants"),
            new(Cookies, false, "the sun always carries the cloud cookie: Look097 puts it on the scene's directional light in every scene with one (the night's moon " +
                "is the same light), so the keyword is on for every lit draw. URP's cookie path is a no-op for a light without one (LightCookieManager.SetupMainLight " +
                "binds white and format None; LightCookie.hlsl SampleMainLightCookie returns 1). Kept when look097.json turns clouds off; -noclouds draws the cookie " +
                "variant with no cookie (Unity's nearest variant: checked in shots)"),
        };
        /// <summary>brief 097 time-of-day test (-todBuild): the lean copy is lit by probe volumes, nothing samples a lightmap, and probe volumes run at L1.</summary>
        static readonly Rule[] Tod = new[] { "LIGHTMAP_ON", "DYNAMICLIGHTMAP_ON", "DIRLIGHTMAP_COMBINED", "LIGHTMAP_SHADOW_MIXING", "SHADOWS_SHADOWMASK", "PROBE_VOLUMES_L2" }
            .Select(k => new Rule(k, true, "-todBuild: lit by probe volumes at L1, no lightmaps")).ToArray();

        static bool Arg(string a) => Array.IndexOf(Environment.GetCommandLineArgs(), a) >= 0;
        static readonly bool Off = Arg("-keepVariants"), KeepCrossFade = Arg("-keepCrossFade"), KeepCookieOff = Arg("-keepCookieOff") || !CloudsOn();
        static readonly Rule[] Rules = Always.Concat(Arg("-todBuild") ? Tod : new Rule[0]).ToArray();

        /// <summary>look097.json's clouds.enabled (true when the file or the field is missing: Look097's own default).</summary>
        static bool CloudsOn()
        {
            var f = Path.Combine(Application.dataPath, "Suborned", "Resources", "Atmosphere", "look097.json");
            if (!File.Exists(f)) return true;
            var m = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(f), "\"clouds\"\\s*:\\s*\\{[^}]*\"enabled\"\\s*:\\s*(true|false)");
            return !m.Success || m.Groups[1].Value == "true";
        }

        // ---------------------------------------------------------------- the build's scenes (the cross-fade check)
        static readonly List<string> fading = new(); static bool crossFadeStripped;

        public void OnPreprocessBuild(BuildReport report) => Reset();

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null) return; // entering Play in the Editor
            foreach (var root in scene.GetRootGameObjects())
                foreach (var g in root.GetComponentsInChildren<LODGroup>(true))
                    if (g.fadeMode != LODFadeMode.None) fading.Add($"{scene.path}: {g.name} ({g.fadeMode})");
            if (fading.Count > 0 && crossFadeStripped && !Off && !KeepCrossFade)
                throw new BuildFailedException($"ShaderStrip097: LOD cross-fade variants were stripped before {fading[0]} was seen fading ({fading.Count} fading LODGroups): set their fade mode to None, or build with -keepCrossFade");
        }

        // ---------------------------------------------------------------- the strip
        internal sealed class Row { public string Shader, Pass, Stage; public int In, Kept; public readonly Dictionary<string, int> Keys = new(); }
        static readonly Dictionary<string, Row> rows = new();
        static readonly Dictionary<string, int> byRule = new();
        static int seen, stripped;

        /// <summary>The shaders whose kept variants the report breaks down by keyword (the kit's: DecoKit/... and the old town's copies).</summary>
        internal static bool Watched(Shader s) => s.name.StartsWith("DecoKit/") || s.name.StartsWith("OldTown/DecoKit/");

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (data.Count == 0) return;
            string key = $"{shader.name}|{snippet.passName}|{snippet.shaderType}";
            if (!rows.TryGetValue(key, out var row)) rows[key] = row = new Row { Shader = shader.name, Pass = snippet.passName, Stage = snippet.shaderType.ToString() };
            row.In += data.Count; seen += data.Count;
            if (!Off) Strip(shader, snippet, data);
            if (ShaderTrace098.On) ShaderTrace098.Keep(shader, snippet, data);
            row.Kept += data.Count;
            if (Watched(shader))
                foreach (var d in data) foreach (var k in d.shaderKeywordSet.GetShaderKeywords()) { row.Keys.TryGetValue(k.name, out var n); row.Keys[k.name] = n + 1; }
        }

        static void Strip(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            var platform = data[0].shaderCompilerPlatform;
            var on = new List<(LocalKeyword k, Rule r)>(); var offs = new List<(LocalKeyword k, Rule r)>();
            foreach (var r in Rules)
            {
                if (r.Keyword == CrossFade && (KeepCrossFade || fading.Count > 0)) continue;
                if (r.Keyword == Cookies && !r.On && KeepCookieOff) continue;
                var lk = shader.keywordSpace.FindKeyword(r.Keyword);
                if (!lk.isValid || !ShaderUtil.PassHasKeyword(shader, snippet.pass, lk, snippet.shaderType, platform)) continue;
                (r.On ? on : offs).Add((lk, r));
            }
            if (on.Count == 0 && offs.Count == 0) return;
            int w = 0;
            for (int i = 0; i < data.Count; i++)
            {
                var ks = data[i].shaderKeywordSet; Rule hit = null;
                foreach (var (k, r) in on) if (ks.IsEnabled(k)) { hit = r; break; }
                if (hit == null) foreach (var (k, r) in offs) if (!ks.IsEnabled(k)) { hit = r; break; }
                if (hit == null) { data[w++] = data[i]; continue; }
                stripped++; byRule.TryGetValue(hit.Id, out var n); byRule[hit.Id] = n + 1;
                if (hit.Keyword == CrossFade) crossFadeStripped = true;
            }
            for (int i = data.Count - 1; i >= w; i--) data.RemoveAt(i);
        }

        // ---------------------------------------------------------------- the report
        /// <summary>The run's counts (GameBuild writes them beside build.json), then reset for the next build in the same domain.</summary>
        public static string Report(string dir)
        {
            var j = new StringBuilder();
            j.Append("{\n \"note\": \"briefs 097/098: variants ShaderStrip097 stripped after URP's own stripper (a variant counts under the first rule it meets); rows: every DecoKit pass and stage, in = what URP kept, kept = what was compiled\",\n");
            j.Append($" \"off\": {B(Off)}, \"keepCrossFade\": {B(KeepCrossFade)}, \"keepCookieOff\": {B(KeepCookieOff)}, \"tracedMode\": {B(ShaderTrace098.On)},\n");
            j.Append($" \"seen\": {seen},\n \"stripped\": {stripped},\n \"kept\": {seen - stripped - ShaderTrace098.Dropped},\n");
            j.Append(" \"byRule\": {").Append(string.Join(",", byRule.OrderByDescending(kv => kv.Value).Select(kv => $"\n  \"{kv.Key}\": {kv.Value}"))).Append("\n },\n");
            j.Append(" \"reasons\": {").Append(string.Join(",", Rules.Select(r => $"\n  \"{r.Id}\": \"{E(r.Reason)}\""))).Append("\n },\n");
            j.Append($" \"fadingLODGroups\": [{string.Join(", ", fading.Take(20).Select(f => $"\"{E(f)}\""))}],\n");
            if (ShaderTrace098.On) j.Append(" \"traced\": ").Append(ShaderTrace098.Json()).Append(",\n");
            j.Append(" \"heaviest\": [").Append(string.Join(",", rows.Values.OrderByDescending(r => r.Kept).Take(25).Select(r => $"\n  {{\"shader\": \"{E(r.Shader)}\", \"pass\": \"{E(r.Pass)}\", \"stage\": \"{r.Stage}\", \"in\": {r.In}, \"kept\": {r.Kept}}}"))).Append("\n ],\n");
            var dk = rows.Values.Where(r => r.Shader.StartsWith("DecoKit/") || r.Shader.StartsWith("OldTown/DecoKit/")).OrderBy(r => r.Shader, StringComparer.Ordinal).ThenByDescending(r => r.Kept).ToList();
            j.Append(" \"decokit\": [").Append(string.Join(",", dk.Select(r => $"\n  {{\"shader\": \"{E(r.Shader)}\", \"pass\": \"{E(r.Pass)}\", \"stage\": \"{r.Stage}\", \"in\": {r.In}, \"kept\": {r.Kept}, \"keywords\": {{{string.Join(", ", r.Keys.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => $"\"{k.Key}\": {k.Value}"))}}}}}"))).Append("\n ]\n}\n");
            try { Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir, "strip097.json"), j.ToString()); } catch (Exception e) { Debug.LogWarning($"ShaderStrip097: report not written ({e.Message})"); }
            var lines = new List<string> { $"ShaderStrip097: {stripped} of {seen} variants stripped{(Off ? " (off: -keepVariants)" : "")}{(ShaderTrace098.On ? $", {ShaderTrace098.Dropped} more not traced" : "")}; {string.Join(", ", byRule.Select(kv => $"{kv.Key} {kv.Value}"))}" };
            lines.AddRange(dk.Where(r => r.In >= 50).Select(r => $"ShaderStrip097: {r.Shader} {r.Pass} {r.Stage.ToLowerInvariant()}: {r.In} -> {r.Kept}"));
            if (fading.Count > 0) lines.Add($"ShaderStrip097: LOD cross-fade kept: {fading.Count} fading LODGroups, first {fading[0]}");
            if (ShaderTrace098.On) { lines.AddRange(ShaderTrace098.Lines()); lines.Add(ShaderTrace098.WritePlayerList(GameBuild.AppPath)); }
            Reset(); return string.Join("\n", lines);
        }

        static void Reset() { seen = stripped = 0; byRule.Clear(); rows.Clear(); fading.Clear(); crossFadeStripped = false; ShaderTrace098.Reset(); }
        static string B(bool b) => b ? "true" : "false";
        internal static string E(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
