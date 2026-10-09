using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>The plan checker's V-1 per-point occluders at the play view (out/unity/v1.json, written by
    /// tools/unity/v1.ts), and the study's comparison against them. Blocker names are the scene's: "plot &lt;id&gt;" or the
    /// Handover building / structure id.</summary>
    public static class V1
    {
        public static string Path => WorldBuilder.Arg("-v1", System.IO.Path.Combine(WorldBuilder.RepoRoot, "out", "unity", "v1.json"));

        /// <summary>Point name → sorted blockers, or null when the file is missing. Throws when it was made from another
        /// plan or another view than the study's.</summary>
        public static Dictionary<string, string[]> Load(string planHash, float yaw, float pitch)
        {
            if (!File.Exists(Path)) return null;
            var v = Json.Parse(File.ReadAllText(Path));
            if (J.S(v, "planHash") != planHash)
                throw new InvalidDataException($"Graybox: {Path} is from plan {J.S(v, "planHash")}, export is {planHash}; rerun tools/unity/v1.ts");
            if (!Mathf.Approximately(J.F(v, "yaw"), yaw) || !Mathf.Approximately(J.F(v, "pitch"), pitch))
                throw new InvalidDataException($"Graybox: {Path} is for view yaw {J.F(v, "yaw")} pitch {J.F(v, "pitch")}, study plays yaw {yaw} pitch {pitch}");
            return J.A(v, "points").ToDictionary(p => J.S(p, "name"),
                p => J.A(p, "blockers").Cast<string>().OrderBy(s => s, System.StringComparer.Ordinal).ToArray());
        }

        /// <summary>Per-point list (study vs V-1) for the log; each disagreement is also logged as an error. Returns the
        /// number of disagreements (0 when V-1 is missing: there is nothing to compare, the list is for Suture).</summary>
        public static int Compare(int yaw, IEnumerable<(string name, string[] blockers)> study, Dictionary<string, string[]> v1, out string report)
        {
            static string L(string[] b) => b.Length == 0 ? "clear" : string.Join(", ", b);
            var sb = new StringBuilder(v1 == null
                ? $"Graybox: study per point at plan yaw {yaw} (no V-1 file at {Path}; compare by hand)\n"
                : $"Graybox: study vs V-1 per point at plan yaw {yaw}\n");
            int bad = 0;
            var seen = new HashSet<string>();
            foreach (var (name, blockers) in study)
            {
                seen.Add(name);
                if (v1 == null) { sb.Append($"  {name}: {L(blockers)}\n"); continue; }
                bool has = v1.TryGetValue(name, out var want);
                bool same = has && want.SequenceEqual(blockers);
                sb.Append($"  {(same ? "ok  " : "DIFF")} {name}: study {L(blockers)} | V-1 {(has ? L(want) : "no such point")}\n");
                if (same) continue;
                bad++;
                Debug.LogError($"Graybox: V-1 disagreement at yaw {yaw}: {name}: study {L(blockers)}, V-1 {(has ? L(want) : "no such point")}");
            }
            if (v1 != null)
                foreach (var name in v1.Keys.Where(k => !seen.Contains(k)))
                {
                    bad++;
                    sb.Append($"  DIFF {name}: not in the study | V-1 {L(v1[name])}\n");
                    Debug.LogError($"Graybox: V-1 disagreement at yaw {yaw}: {name} is in V-1 but not a study point");
                }
            report = sb.ToString();
            return bad;
        }
    }
}
