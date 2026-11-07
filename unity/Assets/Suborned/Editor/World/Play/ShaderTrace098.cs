using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Suborned.World.Play
{
    /// <summary>Brief 098 fast loop (Splice, Sky 2026-11-07): `-scratchVariants traced`, for scratch builds only (GameBuild.CliScratch; the game build
    /// refuses it). The kit's shaders (DecoKit/... and the old town's OldTown/DecoKit/... copies) keep only the variants a GraphicsStateCollection trace
    /// saw the game draw: `-variantTrace a.graphicsstate[,b...]`, default every briefs/098/splice/traced/*.graphicsstate (recorded as brief 096's were:
    /// a -scratchDev player of the same tree run with HitchTrace096's -hitchTrace and -gscTrace). A build variant stays when a traced variant of its
    /// shader and pass holds exactly its keywords, among those its stage declares and this build compiles (a keyword no candidate variant holds, as the
    /// directional lightmaps GraphicsSettings strips, is set aside: every build draws without it). Every other shader keeps what ShaderStrip097 keeps.
    /// Loud, not silent: the build logs each traced variant that matches nothing (the trace is older than the build's keywords: record it again); the
    /// player is a development build carrying StreamingAssets/traced098.json, and ShaderTraceWatch098 logs, once each, every kit variant the game asks
    /// for that the trace left out ("ShaderTrace098: not traced: ..."). `-strictVariants` also draws those with the error shader (strict variant
    /// matching): today that floods, as every lightmapped draw asks for the directional lightmap no build has. Rules and counts: briefs/098/splice/variants.md.</summary>
    internal static class ShaderTrace098
    {
        static string Value(string a) { var x = Environment.GetCommandLineArgs(); int i = Array.IndexOf(x, a); return i >= 0 && i + 1 < x.Length ? x[i + 1] : null; }
        internal static readonly string Mode = Value("-scratchVariants");
        internal static bool On => Mode == "traced";
        static string Dir => Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "098", "splice", "traced");

        /// <summary>GameBuild, before the build: a mode it doesn't know, or the traced mode outside a scratch build, fails it; so does a missing or empty trace.</summary>
        internal static void Check(bool scratch)
        {
            if (Mode == null) return;
            if (Mode != "traced") throw new InvalidOperationException($"Game: -scratchVariants {Mode}: the one mode is `traced`");
            if (!scratch) throw new InvalidOperationException("Game: -scratchVariants traced is for scratch builds only (GameBuild.CliScratch): the player lacks every variant the trace never saw");
            Load();
        }

        /// <summary>With -strictVariants the traced player is built with strict variant matching (a missing variant draws with the error shader, logged by name); the project's setting comes back after.</summary>
        internal static IDisposable Strict()
        {
            if (!On || Array.IndexOf(Environment.GetCommandLineArgs(), "-strictVariants") < 0) return null;
            bool was = PlayerSettings.strictShaderVariantMatching; PlayerSettings.strictShaderVariantMatching = true;
            return new Restore(() => PlayerSettings.strictShaderVariantMatching = was);
        }
        sealed class Restore : IDisposable { readonly Action a; public Restore(Action a) => this.a = a; public void Dispose() => a(); }

        // ---------------------------------------------------------------- the trace (a GraphicsStateCollection file is JSON; the 64-bit state ids are not read)
#pragma warning disable CS0649 // set by JsonUtility
        [Serializable] sealed class Gsc { public Entry[] m_VariantInfoMap; }
        [Serializable] sealed class Entry { public Info second; }
        [Serializable] sealed class Info { public string shaderName; public string keywordNames; public int subShaderIndex; public int passIndex; }
#pragma warning restore CS0649

        static Dictionary<string, List<string[]>> sets; static List<string> files = new(); static int traced;
        static string Key(string shader, uint sub, uint pass) => $"{shader}|{sub}|{pass}";

        static void Load()
        {
            if (sets != null) return;
            var arg = Value("-variantTrace"); var root = HarbourWorldBuilder.RepoRoot;
            files = arg != null ? arg.Split(',').Select(f => Path.GetFullPath(Path.IsPathRooted(f) ? f : Path.Combine(root, f))).ToList()
                : Directory.Exists(Dir) ? Directory.GetFiles(Dir, "*.graphicsstate").OrderBy(f => f, StringComparer.Ordinal).ToList() : new List<string>();
            if (files.Count == 0) throw new InvalidOperationException($"Game: -scratchVariants traced: no trace (-variantTrace file[,file], or {Dir}/*.graphicsstate)");
            var s = new Dictionary<string, List<string[]>>(); traced = 0;
            foreach (var f in files)
            {
                if (!File.Exists(f)) throw new FileNotFoundException($"Game: -scratchVariants traced: {f} is missing");
                var g = JsonUtility.FromJson<Gsc>(File.ReadAllText(f));
                foreach (var e in g?.m_VariantInfoMap ?? new Entry[0])
                {
                    var v = e?.second; if (v == null || !Kit(v.shaderName)) continue;
                    var k = Key(v.shaderName, (uint)v.subShaderIndex, (uint)v.passIndex);
                    if (!s.TryGetValue(k, out var l)) s[k] = l = new List<string[]>();
                    l.Add((v.keywordNames ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)); traced++;
                }
            }
            if (traced == 0) throw new InvalidOperationException($"Game: -scratchVariants traced: no kit variant in {string.Join(", ", files)}");
            sets = s;
        }

        static bool Kit(string name) => name != null && (name.StartsWith("DecoKit/") || name.StartsWith("OldTown/DecoKit/"));

        // ---------------------------------------------------------------- the filter (ShaderStrip097.OnProcessShader, after its own rules)
        internal sealed class Row { public string Shader, Pass, Stage, Example; public uint Sub, Index; public int Traced, Kept, Dropped, Unmatched; public readonly SortedSet<string> Space = new(StringComparer.Ordinal), KeptSets = new(StringComparer.Ordinal); }
        static readonly Dictionary<string, Row> rows = new();
        internal static int Dropped { get; private set; }

        internal static void Keep(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (data.Count == 0 || !Kit(shader.name)) return;
            Load();
            sets.TryGetValue(Key(shader.name, snippet.pass.SubshaderIndex, snippet.pass.PassIndex), out var seen);
            var declared = new HashSet<string>(ShaderUtil.GetPassKeywords(shader, snippet.pass, snippet.shaderType).Select(k => k.name));
            var names = new string[data.Count]; var held = new HashSet<string>();
            for (int i = 0; i < data.Count; i++)
            {
                var ks = data[i].shaderKeywordSet.GetShaderKeywords().Select(k => k.name).OrderBy(k => k, StringComparer.Ordinal).ToArray();
                held.UnionWith(ks); names[i] = string.Join(" ", ks);
            }
            var want = new HashSet<string>();
            foreach (var t in seen ?? new List<string[]>()) want.Add(string.Join(" ", t.Where(k => declared.Contains(k) && held.Contains(k)).OrderBy(k => k, StringComparer.Ordinal)));
            var key = $"{shader.name}|{snippet.passName}|{snippet.shaderType}";
            if (!rows.TryGetValue(key, out var row)) rows[key] = row = new Row { Shader = shader.name, Pass = snippet.passName, Stage = snippet.shaderType.ToString(), Sub = snippet.pass.SubshaderIndex, Index = snippet.pass.PassIndex };
            row.Traced += want.Count; row.Space.UnionWith(held.Where(declared.Contains));
            var have = new HashSet<string>(names);
            foreach (var p in want) if (!have.Contains(p)) { row.Unmatched++; row.Example ??= p == "" ? "(no keywords)" : p; }
            int w = 0;
            for (int i = 0; i < data.Count; i++) { if (want.Contains(names[i])) { row.KeptSets.Add(names[i]); data[w++] = data[i]; } else { row.Dropped++; Dropped++; } }
            for (int i = data.Count - 1; i >= w; i--) data.RemoveAt(i);
            row.Kept += w;
        }

        // ---------------------------------------------------------------- the report (inside strip097.json, and the build log)
        internal static string Json()
        {
            var r = rows.Values.OrderBy(x => x.Shader, StringComparer.Ordinal).ThenByDescending(x => x.Dropped + x.Kept);
            return $"{{\"files\": [{string.Join(", ", files.Select(f => $"\"{ShaderStrip097.E(f)}\""))}], \"tracedKitVariants\": {traced}, \"dropped\": {Dropped}, \"rows\": [" +
                string.Join(",", r.Select(x => $"\n   {{\"shader\": \"{ShaderStrip097.E(x.Shader)}\", \"pass\": \"{ShaderStrip097.E(x.Pass)}\", \"stage\": \"{x.Stage}\", \"tracedSets\": {x.Traced}, \"kept\": {x.Kept}, \"dropped\": {x.Dropped}, \"unmatched\": {x.Unmatched}{(x.Example != null ? $", \"unmatchedExample\": \"{ShaderStrip097.E(x.Example)}\"" : "")}}}")) + "]}";
        }

        internal static IEnumerable<string> Lines()
        {
            int kept = rows.Values.Sum(x => x.Kept), un = rows.Values.Sum(x => x.Unmatched);
            yield return $"ShaderTrace098: traced mode, {traced} kit variants in {files.Count} trace(s): the kit kept {kept} variants, {Dropped} never traced dropped; a development player that logs what the trace left out{(Array.IndexOf(Environment.GetCommandLineArgs(), "-strictVariants") >= 0 ? ", strict variant matching on" : "")}";
            foreach (var x in rows.Values.Where(x => x.Unmatched > 0).OrderByDescending(x => x.Unmatched))
                yield return $"ShaderTrace098: {x.Shader} {x.Pass} {x.Stage.ToLowerInvariant()}: {x.Unmatched} traced variants match nothing in this build (the trace is older than its keywords: record it again), e.g. {x.Example}";
            if (un == 0) yield return "ShaderTrace098: every traced variant matched one in this build";
        }

        /// <summary>The kept list the traced player's ShaderTraceWatch098 reads (StreamingAssets/traced098.json in the app; GameBuild signs the app after).</summary>
        internal static string WritePlayerList(string app)
        {
            var dir = Path.Combine(app, "Contents", "Resources", "Data", "StreamingAssets");
            if (!Directory.Exists(dir)) return $"ShaderTrace098: no {dir}: the player has no kept list (its watch stays off)";
            static string A(IEnumerable<string> xs) => "[" + string.Join(", ", xs.Select(x => $"\"{ShaderStrip097.E(x)}\"")) + "]";
            var json = "{\"rows\": [" + string.Join(",", rows.Values.Select(r => $"\n {{\"shader\": \"{ShaderStrip097.E(r.Shader)}\", \"sub\": {r.Sub}, \"pass\": {r.Index}, \"stage\": \"{r.Stage}\", \"space\": {A(r.Space)}, \"kept\": {A(r.KeptSets)}}}")) + "\n]}\n";
            File.WriteAllText(Path.Combine(dir, "traced098.json"), json);
            return $"ShaderTrace098: the player's watch list: {rows.Count} kit passes and stages, {rows.Values.Sum(r => r.KeptSets.Count)} kept variants";
        }

        internal static void Reset() { rows.Clear(); Dropped = 0; sets = null; }
    }
}
