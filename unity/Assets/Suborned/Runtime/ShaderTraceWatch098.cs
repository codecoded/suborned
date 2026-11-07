using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned
{
    /// <summary>Brief 098 fast loop (Splice): the loud half of a traced scratch player (-scratchVariants traced, Editor ShaderTrace098). Only such a player
    /// carries StreamingAssets/traced098.json (per kit shader pass and stage: the keywords the build compiles and the variants the trace kept); nothing else
    /// starts this. It records what the game draws (a GraphicsStateCollection trace; the traced player is a development build) and every few seconds logs,
    /// once each, a kit variant the game asked for that the traced build left out. Unity then draws with its nearest variant, so the look there may be
    /// wrong: "ShaderTrace098: not traced: DecoKit/Lit Cutaway pass 0 fragment: FOG_LINEAR ...". A keyword no build compiles (the directional lightmaps,
    /// GraphicsSettings m_LightmapKeepDirCombined 0) is set aside, as every build draws without it. Off while -gscTrace records.</summary>
    public sealed class ShaderTraceWatch098 : MonoBehaviour
    {
        [Serializable] sealed class Kept { public Row[] rows; }
        [Serializable] sealed class Row { public string shader, stage; public int sub, pass; public string[] space, kept; }
#pragma warning disable CS0649 // set by JsonUtility
        [Serializable] sealed class Gsc { public Entry[] m_VariantInfoMap; }
        [Serializable] sealed class Entry { public Info second; }
        [Serializable] sealed class Info { public string shaderName, keywordNames; public int subShaderIndex, passIndex; }
#pragma warning restore CS0649

        const float Every = 5f;
        readonly Dictionary<string, List<(string stage, HashSet<string> space, HashSet<string> kept)>> rows = new();
        readonly HashSet<string> told = new();
        GraphicsStateCollection trace; string tmp; float next; int missing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || Array.IndexOf(Environment.GetCommandLineArgs(), "-gscTrace") >= 0) return;
            var f = Path.Combine(Application.streamingAssetsPath, "traced098.json");
            if (!File.Exists(f)) return;
            var go = new GameObject("ShaderTraceWatch098") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go); go.AddComponent<ShaderTraceWatch098>().Load(f);
        }

        void Load(string f)
        {
            foreach (var r in JsonUtility.FromJson<Kept>(File.ReadAllText(f))?.rows ?? new Row[0])
            {
                var key = $"{r.shader}|{r.sub}|{r.pass}";
                if (!rows.TryGetValue(key, out var l)) rows[key] = l = new();
                l.Add((r.stage, new HashSet<string>(r.space ?? new string[0]), new HashSet<string>(r.kept ?? new string[0])));
            }
            tmp = Path.Combine(Application.temporaryCachePath, "traced098-watch.graphicsstate");
            trace = new GraphicsStateCollection(); bool on = trace.BeginTrace();
            Debug.Log($"ShaderTrace098: a traced scratch player ({rows.Count} kit passes); {(on ? "watching for variants the trace left out" : "the watch could not start a trace (a development player is needed)")}");
            next = Time.realtimeSinceStartup + Every;
        }

        void Update() { if (trace != null && Time.realtimeSinceStartup >= next) { Check(); next = Time.realtimeSinceStartup + Every; } }
        void OnApplicationQuit() => Check();

        void Check()
        {
            if (trace == null || !trace.isTracing) return;
            trace.EndTrace();
            try
            {
                if (trace.SaveToFile(tmp))
                    foreach (var e in JsonUtility.FromJson<Gsc>(File.ReadAllText(tmp))?.m_VariantInfoMap ?? new Entry[0])
                    {
                        var v = e?.second; if (v == null || !rows.TryGetValue($"{v.shaderName}|{v.subShaderIndex}|{v.passIndex}", out var stages)) continue;
                        var ks = (v.keywordNames ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var (stage, space, kept) in stages)
                        {
                            var p = string.Join(" ", ks.Where(space.Contains).OrderBy(k => k, StringComparer.Ordinal));
                            if (kept.Contains(p) || !told.Add($"{v.shaderName}|{v.passIndex}|{stage}|{p}")) continue;
                            missing++;
                            Debug.LogWarning($"ShaderTrace098: not traced: {v.shaderName} pass {v.passIndex} {stage.ToLowerInvariant()}: {(p == "" ? "(no keywords)" : p)} (drawn with the nearest variant: the look there may be wrong; {missing} so far)");
                        }
                    }
            }
            catch (Exception x) { Debug.LogWarning($"ShaderTrace098: the watch could not read its trace ({x.Message})"); }
            trace = new GraphicsStateCollection(); trace.BeginTrace();
        }
    }
}
