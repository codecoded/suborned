using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>The `-tplong` census (moved out of TpLongRun.cs by topic, brief 106: that file is over 500 lines).</summary>
    public sealed partial class TpLongRun
    {
        // ---------------------------------------------------------------- census: renderers this camera draws (LOD-aware), and those beyond 150 m

        void Census(Res res)
        {
            var cam = h.View; if (!cam) throw new InvalidOperationException("no camera");
            var planes = GeometryUtility.CalculateFrustumPlanes(cam); var cp = cam.transform.position;
            float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), bias = QualitySettings.lodBias;
            // as briefs/048/probe/Probe.cs (the spec, Splice 2026-10-23): a renderer in a LOD group counts only when its own level (the first it is
            // listed in) is the group's active one; a culled group counts nothing; the frustum alone decides distance (no straight-line far test)
            var active = new Dictionary<Renderer, bool>();
            foreach (var g in FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var lods = g.GetLODs(); if (lods.Length == 0) continue;
                int a = -1;
                if (g.enabled)
                {
                    var t = g.transform; var refp = t.TransformPoint(g.localReferencePoint); var ls = t.lossyScale;
                    float size = g.size * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                    float rel = cam.orthographic ? size * 0.5f / cam.orthographicSize : size * 0.5f / (Mathf.Max(0.001f, Vector3.Distance(refp, cp)) * tanHalf);
                    rel *= bias;
                    for (int i = 0; i < lods.Length; i++) if (rel >= lods[i].screenRelativeTransitionHeight) { a = i; break; }
                }
                for (int i = 0; i < lods.Length; i++) foreach (var r in lods[i].renderers) if (r && !active.ContainsKey(r)) active[r] = i == a;
            }
            int drawn = 0, far = 0;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled || r.forceRenderingOff || !(r is MeshRenderer || r is SkinnedMeshRenderer) || (cam.cullingMask & (1 << r.gameObject.layer)) == 0) continue;
                if (active.TryGetValue(r, out var on) && !on) continue;
                var b = r.bounds; if (!GeometryUtility.TestPlanesAABB(planes, b)) continue;
                drawn++; bool beyond = b.SqrDistance(cp) >= 150f * 150f; if (beyond) far++;
                Tally(byBranch, Branch(r.transform), beyond); Tally(byPiece, Piece(r.transform), beyond);
            }
            res.Drawn = drawn; res.Beyond150 = far;
            WriteCensus(res.Name, drawn, far);
        }

        // what the census counted, by scene branch (root / child / grandchild) and by piece (the nearest LODGroup owner, else the object's name
        // without its index): `census-<row>.json` beside the report, for Redline's and Splice's reading of the counts (not judged)
        readonly Dictionary<string, (int all, int far)> byBranch = new(), byPiece = new();
        static void Tally(Dictionary<string, (int all, int far)> d, string k, bool far) { d.TryGetValue(k, out var v); d[k] = (v.all + 1, v.far + (far ? 1 : 0)); }
        static string Branch(Transform t)
        {
            var parts = new List<string>(); for (var p = t; p; p = p.parent) parts.Add(p.name); parts.Reverse();
            return string.Join("/", parts.Take(Math.Min(3, Math.Max(1, parts.Count - 1))));
        }
        static string Piece(Transform t)
        {
            for (var p = t; p; p = p.parent) if (p.GetComponent<LODGroup>()) return Stem(p.name);
            return Stem(t.name);
        }
        static string Stem(string n) => System.Text.RegularExpressions.Regex.Replace(n.Replace("(Clone)", ""), @"[\s_#-]*\(?\d+\)?$|[|@].*$", "").Trim();
        void WriteCensus(string row, int drawn, int far)
        {
            string Top(Dictionary<string, (int all, int far)> d) => string.Join(",\n   ", d.OrderByDescending(kv => kv.Value.far).ThenByDescending(kv => kv.Value.all).Take(30)
                .Select(kv => "{\"key\": \"" + Esc(kv.Key) + "\", \"drawn\": " + kv.Value.all + ", \"beyond150\": " + kv.Value.far + "}"));
            try
            {
                File.WriteAllText(Path.Combine(dir, "census-" + row + ".json"), "{\n \"schema\": \"suborned.tplong.census/1\", \"row\": \"" + Esc(row) + "\", \"drawn\": " + drawn + ", \"beyond150\": " + far +
                    ",\n \"byBranch\": [\n   " + Top(byBranch) + "\n ],\n \"byPiece\": [\n   " + Top(byPiece) + "\n ]\n}\n");
            }
            catch (Exception e) { Debug.LogError("TpLongRun: census: " + e.Message); }
            byBranch.Clear(); byPiece.Clear();
        }
    }
}
