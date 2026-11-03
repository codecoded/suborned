using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World.Review
{
    /// <summary>Brief 072-F, owed by Latch (Redline phase 3 §2 #3; 2026-10-28): for FacadeLit and the two DecoKit cutaway shaders,
    ///  1. the Inspector's SRP Batcher line, read the way the Inspector reads it (ShaderUtil.GetSRPBatcherCompatibilityCode and its issue reason,
    ///     internal, by reflection), per subshader, after one offscreen play-camera render of the built scene (before it, every shader says
    ///     "not initialized");
    ///  2. the texture reads per shader: the forward pass's fragment program compiled for Metal (macOS) with each distinct local keyword set
    ///     the built scene's and the project's materials use on that shader, plus the play view's pipeline keywords (cascaded main-light shadows, soft shadows high,
    ///     Forward+, linear fog), and its sample calls counted in the Metal source (`.sample(`, `.sample_compare(`, `.read(`, `.gather(`).
    ///     Static counts: a sample inside a branch counts once whether or not the branch runs; the shadow taps of URP's soft shadows are in it.
    /// → briefs/072/latch/textured/shaders.json.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.Shaders072.Cli -logFile &lt;log&gt;</summary>
    public static class Shaders072
    {
        static readonly string[] Names = { "Suborned/FacadeLit", "DecoKit/Lit Cutaway", "DecoKit/Complex Lit Cutaway" };
        static readonly string[] PlayKeywords = { "_MAIN_LIGHT_SHADOWS_CASCADE", "_SHADOWS_SOFT", "_SHADOWS_SOFT_HIGH", "_FORWARD_PLUS", "FOG_LINEAR" };

        public static void Cli() => HarbourWorldBuilder.Batch(Run);

        static void Run()
        {
            var su = typeof(ShaderUtil); const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var code = su.GetMethod("GetSRPBatcherCompatibilityCode", S); var reason = su.GetMethod("GetSRPBatcherCompatibilityIssueReason", S);
            // the built scene's materials (FacadeLit's live in the scene, not as assets) and the project's; one play-camera render first, since the
            // SRP Batcher's compatibility is only known once the pipeline has loaded the shader (the Inspector says "not initialized" before)
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var mats = AssetDatabase.FindAssets("t:Material").Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                .Concat(UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include).SelectMany(r => r.sharedMaterials))
                .Where(m => m && m.shader).Distinct().ToList();
            var playGo = GameObject.Find("cameras/play");
            if (playGo && playGo.TryGetComponent<Camera>(out var cam))
            {
                using var session = new RenderSession();
                Baseline072.Pose(cam, playGo.GetComponent<TacticalShadows>(), playGo.GetComponent<ArchitectureFade>(), Baseline072.OfficeFront, 30f, Baseline072.DefaultStandOff);
                session.Grab(cam, 640, 360); session.Grab(cam, 640, 360);
            }
            var sb = new StringBuilder("{\n  \"schema\": \"suborned.072.shaders/1\",\n");
            sb.Append($"  \"when\": \"{DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}\",\n  \"unity\": \"{Application.unityVersion}\",\n");
            sb.Append($"  \"method\": \"SRP Batcher: ShaderUtil.GetSRPBatcherCompatibilityCode / GetSRPBatcherCompatibilityIssueReason (the Inspector's source), per subshader, after one play-camera render of the built scene (office-front y030). Texture reads: ShaderData pass ForwardLit (or the first pass tagged UniversalForward) fragment program, CompileVariant for Metal / StandaloneOSX (external-tool source), with each distinct local keyword set of the project's materials on the shader plus {string.Join(" ", PlayKeywords)}; counted calls .sample( .sample_compare( .read( .gather( in the Metal source; static counts (a branch's samples count once)\",\n  \"shaders\": [\n");
            var lines = new List<string>(); var blocks = new List<string>();
            foreach (var name in Names)
            {
                var sh = Shader.Find(name); if (!sh) { blocks.Add($"    {{ \"shader\": \"{name}\", \"missing\": true }}"); continue; }
                var path = AssetDatabase.GetAssetPath(sh); var data = ShaderUtil.GetShaderData(sh);
                var batcher = new List<string>();
                for (int s = 0; s < data.SubshaderCount; s++)
                {
                    string line;
                    if (code == null) line = "ShaderUtil.GetSRPBatcherCompatibilityCode not found in this Unity";
                    else
                    {
                        int c = (int)code.Invoke(null, new object[] { sh, s });
                        line = c == 0 ? "SRP Batcher: compatible" : $"SRP Batcher: not compatible ({(reason != null ? reason.Invoke(null, new object[] { sh, s, c }) : "code " + c)})";
                    }
                    batcher.Add($"subshader {s}: {line}"); lines.Add($"{name} subshader {s}: {line}");
                }
                var users = mats.Where(m => m.shader == sh).ToList();
                var sets = users.GroupBy(m => string.Join(" ", m.shaderKeywords.Where(k => sh.keywordSpace.FindKeyword(k).isValid && !PlayKeywords.Contains(k)).OrderBy(k => k)))
                    .Select(g => (keys: g.Key, mats: g.Select(m => m.name).OrderBy(n => n).ToList())).OrderByDescending(x => x.mats.Count).ToList();
                if (sets.Count == 0) sets.Add(("", new List<string>()));
                var (sub, pass, passName) = ForwardPass(data);
                var rows = new List<string>();
                foreach (var (keys, ms) in sets)
                {
                    var kw = (keys.Length > 0 ? keys.Split(' ') : Array.Empty<string>()).Concat(PlayKeywords).ToArray();
                    string row;
                    try
                    {
                        var info = pass.CompileVariant(ShaderType.Fragment, kw, ShaderCompilerPlatform.Metal, BuildTarget.StandaloneOSX, true);
                        if (!info.Success) row = $"\"error\": \"{Esc(string.Join(" | ", info.Messages.Select(m => m.message)))}\"";
                        else
                        {
                            var src = Encoding.UTF8.GetString(info.ShaderData);
                            int sample = Count(src, @"\.sample\("), cmp = Count(src, @"\.sample_compare\("), read = Count(src, @"\.read\("), gather = Count(src, @"\.gather(_compare)?\(");
                            var tex = Regex.Matches(src, @"texture2d(?:_array)?<[^>]+>\s+(\w+)\s*\[\[\s*texture\(\d+\)\s*\]\]").Select(m => m.Groups[1].Value).Distinct().ToList();
                            var arr = Regex.Matches(src, @"(?:texture2d_array|depth2d_array|depth2d|texturecube)<[^>]+>\s+(\w+)\s*\[\[\s*texture\(\d+\)\s*\]\]").Select(m => m.Groups[1].Value).Distinct();
                            var bound = tex.Concat(arr).Distinct().ToList();
                            row = $"\"samples\": {sample}, \"shadowCompares\": {cmp}, \"reads\": {read}, \"gathers\": {gather}, \"total\": {sample + cmp + read + gather}, \"textureBindings\": {bound.Count}, \"textures\": [{string.Join(", ", bound.Select(t => $"\"{t}\""))}]";
                            lines.Add($"{name} [{(keys.Length > 0 ? keys : "no local keywords")}] ({ms.Count} materials): {sample} samples + {cmp} shadow compares + {read + gather} reads/gathers = {sample + cmp + read + gather}, {bound.Count} textures bound");
                        }
                    }
                    catch (Exception e) { row = $"\"error\": \"{Esc(e.GetType().Name + ": " + e.Message)}\""; }
                    rows.Add($"        {{ \"localKeywords\": \"{keys}\", \"materials\": {ms.Count}, \"examples\": [{string.Join(", ", ms.Take(6).Select(m => $"\"{Esc(m)}\""))}], {row} }}");
                }
                blocks.Add($"    {{ \"shader\": \"{name}\", \"path\": \"{path}\", \"srpBatcher\": [{string.Join(", ", batcher.Select(b => $"\"{Esc(b)}\""))}], \"pass\": \"{Esc(passName)} (subshader {sub})\",\n      \"variants\": [\n{string.Join(",\n", rows)}\n      ] }}");
            }
            sb.Append(string.Join(",\n", blocks)).Append("\n  ]\n}\n");
            var file = Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "072", "latch", "textured", "shaders.json");
            File.WriteAllText(file, sb.ToString());
            Debug.Log("072 shaders:\n  " + string.Join("\n  ", lines) + $"\n→ {file}");
        }

        static (int sub, ShaderData.Pass pass, string name) ForwardPass(ShaderData data)
        {
            for (int s = 0; s < data.SubshaderCount; s++)
            {
                var ss = data.GetSubshader(s);
                for (int p = 0; p < ss.PassCount; p++)
                {
                    var ps = ss.GetPass(p);
                    if (ps.Name == "ForwardLit" || ps.FindTagValue(new ShaderTagId("LightMode")).name == "UniversalForward") return (s, ps, ps.Name);
                }
            }
            var first = data.GetSubshader(0).GetPass(0); return (0, first, first.Name);
        }

        static int Count(string s, string rx) => Regex.Matches(s, rx).Count;
        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
    }
}
