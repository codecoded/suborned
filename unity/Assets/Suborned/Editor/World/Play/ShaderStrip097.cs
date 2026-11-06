using System.Collections.Generic;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World.Play
{
    /// <summary>Brief 097 (Redline's render audit item 5, Sky 2026-11-05 "yes": brief 095's variant strip): player builds drop the shader variants of
    /// keyword families the game never turns on, so a build compiles and ships fewer programs (the cold cutaway compile was 7,488 variants on Linux).
    /// The game's facts: Forward+ (the clustered light loop, never per-vertex lights), one shadowed light (the sun: night lamps are unshadowed, GDD §15),
    /// no screen-space shadows, no URP decals (DBuffer), no screen-space reflections, no Surface Cache GI (screen-space irradiance), no XR (Meta Quest).
    /// A variant that holds any of these keywords is stripped. NOT stripped (tried 2026-11-06, the whole zone drew white): _LIGHT_LAYERS, which URP turns
    /// on for every draw while the asset supports light layers, and _ADDITIONAL_LIGHT_SHADOWS, kept to be safe.
    /// The play's soft shadows are Medium (TacticalShadows) and the asset's quality is Medium: LOW is stripped, MEDIUM and HIGH kept.
    /// Off with -keepVariants on the build's command line. Counts per run: out/build/play/strip097.json (or the scratch build's folder).</summary>
    public sealed class ShaderStrip097 : IPreprocessShaders
    {
        public int callbackOrder => 10;
        static readonly string[] Strip =
        {
            "_ADDITIONAL_LIGHTS_VERTEX", "_MAIN_LIGHT_SHADOWS_SCREEN", "_DBUFFER_MRT1", "_DBUFFER_MRT2", "_DBUFFER_MRT3",
            "_SCREEN_SPACE_IRRADIANCE", "_SCREEN_SPACE_REFLECTION", "_SHADOWS_SOFT_LOW", "META_QUEST_LIGHTUNROLL", "META_QUEST_ORTHO_PROJ",
            "META_QUEST_NO_SPOTLIGHTS_LIGHT_LOOP",
        };
        static readonly ShaderKeyword[] Keys = System.Array.ConvertAll(Strip, k => new ShaderKeyword(k));
        static readonly bool Off = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-keepVariants") >= 0;
        static int seen, stripped; static readonly Dictionary<string, int> byKey = new();

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (Off) return;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                seen++;
                var ks = data[i].shaderKeywordSet;
                for (int k = 0; k < Keys.Length; k++)
                {
                    if (!ks.IsEnabled(Keys[k])) continue;
                    data.RemoveAt(i); stripped++; byKey.TryGetValue(Strip[k], out var n); byKey[Strip[k]] = n + 1; break;
                }
            }
        }

        /// <summary>The run's counts (GameBuild writes them beside build.json), then reset for the next build in the same domain.</summary>
        public static string Report(string dir)
        {
            var rows = new List<string>(); foreach (var kv in byKey) rows.Add($"  \"{kv.Key}\": {kv.Value}");
            var json = $"{{\n \"note\": \"brief 097: variants stripped by ShaderStrip097 (a variant counts under the first stripped keyword it holds)\",\n \"off\": {(Off ? "true" : "false")},\n \"seen\": {seen},\n \"stripped\": {stripped},\n \"byKeyword\": {{\n{string.Join(",\n", rows)}\n }}\n}}\n";
            try { Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir, "strip097.json"), json); } catch { }
            var line = $"ShaderStrip097: {stripped} of {seen} variants stripped{(Off ? " (off: -keepVariants)" : "")}";
            seen = stripped = 0; byKey.Clear(); return line;
        }
    }
}
