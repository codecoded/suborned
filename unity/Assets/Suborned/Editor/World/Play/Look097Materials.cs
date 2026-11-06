using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Suborned.World.Play
{
    /// <summary>Brief 097: the kit materials' wind and vertex-AO rows (Editor/World/look097.json) on the generated kit materials (Assets/Suborned/Kit/Materials,
    /// git-ignored; `bun run kit` leaves a .mat Unity owns alone unless its catalogue hash moves, and the next build sets the rows again). Idempotent: a
    /// material already as the rows say is not touched (no reimport). Called by GameBuild.Build before the player build.</summary>
    public static class Look097Materials
    {
        const string Dir = "Assets/Suborned/Kit/Materials";
        const string Config = "unity/Assets/Suborned/Editor/World/look097.json";

        public static string Apply()
        {
            var path = Path.Combine(HarbourWorldBuilder.RepoRoot, Config);
            if (!File.Exists(path)) return "Look097Materials: no look097.json";
            var cfg = Json.Parse(File.ReadAllText(path));
            int wind = 0, ao = 0, changed = 0;
            var winds = J.Get(cfg, "wind") as Dictionary<string, object> ?? new Dictionary<string, object>();
            foreach (var kv in winds)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/{kv.Key}.mat"); if (!m) continue;
                float reach = System.Convert.ToSingle(kv.Value, System.Globalization.CultureInfo.InvariantCulture);
                bool on = m.IsKeywordEnabled("_DK_WIND") && Mathf.Approximately(m.GetFloat("_Parallax"), reach);
                if (!on) { m.EnableKeyword("_DK_WIND"); m.SetFloat("_Parallax", reach); EditorUtility.SetDirty(m); changed++; }
                wind++;
            }
            int rooms = 0;
            if (J.Get(cfg, "interior") is Dictionary<string, object> ins)
                foreach (var kv in ins)
                {
                    if (kv.Value is not double sm) continue;
                    var m = AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/{kv.Key}.mat"); if (!m) continue;
                    if (!m.IsKeywordEnabled("_DK_INTERIOR") || !Mathf.Approximately(m.GetFloat("_Smoothness"), (float)sm))
                    { m.EnableKeyword("_DK_INTERIOR"); m.SetFloat("_Smoothness", (float)sm); m.SetFloat("_EnvironmentReflections", 1f); m.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); EditorUtility.SetDirty(m); changed++; }
                    rooms++;
                }
            var aoCfg = J.Get(cfg, "vertexAO") as Dictionary<string, object>;
            var prefixes = (J.Get(aoCfg, "prefixes") as List<object> ?? new List<object>()).Select(o => (string)o).ToArray();
            var exclude = (J.Get(aoCfg, "exclude") as List<object> ?? new List<object>()).Select(o => (string)o).ToArray();
            if (prefixes.Length > 0)
                foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Dir }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid); var n = Path.GetFileNameWithoutExtension(p);
                    if (!prefixes.Any(x => x == "*" || n.StartsWith(x)) || exclude.Any(n.StartsWith)) continue;
                    var m = AssetDatabase.LoadAssetAtPath<Material>(p); if (!m || !m.shader || !m.shader.name.StartsWith("DecoKit/") || !m.shader.name.Contains("Cutaway") || m.shader.name.Contains("Leaf")) continue;
                    if (!m.IsKeywordEnabled("_DK_VERTEX_AO")) { m.EnableKeyword("_DK_VERTEX_AO"); EditorUtility.SetDirty(m); changed++; }
                    ao++;
                }
            int moved = 0;
            if (J.Get(cfg, "shaders") is Dictionary<string, object> rows)
                foreach (var kv in rows)
                {
                    if (kv.Value is not Dictionary<string, object> row) continue;
                    var m = AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/{kv.Key}.mat"); var sh = Shader.Find(J.S(row, "shader"));
                    if (!m || !sh) { Debug.LogWarning($"Look097Materials: {kv.Key} or {J.S(row, "shader")} missing"); continue; }
                    bool dirty = m.shader != sh; if (dirty) { var tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null; m.shader = sh; if (tex && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex); }
                    if (J.Get(row, "floats") is Dictionary<string, object> fl) foreach (var f in fl) { float v = System.Convert.ToSingle(f.Value, System.Globalization.CultureInfo.InvariantCulture); if (!m.HasProperty(f.Key) || Mathf.Approximately(m.GetFloat(f.Key), v)) continue; m.SetFloat(f.Key, v); dirty = true; }
                    if (J.Get(row, "colours") is Dictionary<string, object> co) foreach (var c in co) { var a4 = J.A(c.Value); var col = new Color((float)(double)a4[0], (float)(double)a4[1], (float)(double)a4[2], (float)(double)a4[3]); if (!m.HasProperty(c.Key) || m.GetColor(c.Key) == col) continue; m.SetColor(c.Key, col); dirty = true; }
                    m.renderQueue = -1;
                    if (dirty) { EditorUtility.SetDirty(m); changed++; } moved++;
                }
            changed += Fx097Assets.Ensure();
            if (changed > 0) AssetDatabase.SaveAssets();
            return $"Look097Materials: wind on {wind} materials, vertex AO on {ao}, {moved} on 097 shaders, rooms behind {rooms}; {changed} changed";
        }
    }
}
