using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Glint, brief 096 ambient (Sky: "something missing, and I think it's shading / ambient lighting"; the Handover zone only, no full rebake). A review
    /// prototype of a build-time contact shade, run once for a launch by GradeReview (`contact: 1`): under the OldTown cut's Handover root, a strip of the game's
    /// Suborned/Contact Shade (multiply, strip mode) along the foot of every building (its ground-level outline: the convex hull of the mesh's lowest vertices when
    /// the mesh is readable, else its oriented bounds) and a blob (blob mode) under every prop, batched into two meshes (two draw calls). The real version is the
    /// same geometry written by the builder at build time; nothing here runs in the game without the review flag.</summary>
    public static class ContactShade096
    {
        public const string Root = "Ours (Handover zone + network, our pipeline)";

        public static bool Build(float width, float strip, float blob, float spread, Color tint)
        {
            var root = GameObject.Find(Root); if (!root) return false;
            var shader = Shader.Find("Suborned/Contact Shade");
            var host = Object.FindAnyObjectByType<HandoverHost>(); if (!shader && host && host.contactTemplate) shader = host.contactTemplate.shader;
            if (!shader) { Debug.LogError("ContactShade096: no Suborned/Contact Shade"); return true; }
            var sv = new List<Vector3>(); var su = new List<Vector2>(); var st = new List<int>();
            var bv = new List<Vector3>(); var bu = new List<Vector2>(); var bt = new List<int>();
            int nb = 0, np = 0, hull = 0;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled || r.gameObject.layer == SignalsFeature.Layer || !r.sharedMaterial || r.sharedMaterial.renderQueue >= 2500 || r.sharedMaterial.shader == shader) continue;
                var b = r.bounds; float foot = b.size.x * b.size.z; if (b.size.y < 0.35f || b.min.y > 1.5f) continue; // floating parts and decks are not grounded
                var mf = r.GetComponent<MeshFilter>(); var tr = r.transform; float y = b.min.y + 0.015f;
                if (b.size.y > 4.5f && foot > 30f)
                {
                    var pts = new List<Vector2>();
                    if (mf && mf.sharedMesh && mf.sharedMesh.isReadable)
                    {
                        var vs = mf.sharedMesh.vertices; foreach (var v in vs) { var w = tr.TransformPoint(v); if (w.y < b.min.y + 0.6f) pts.Add(new Vector2(w.x, w.z)); }
                        if (pts.Count >= 3) hull++;
                    }
                    if (pts.Count < 3) { pts.Clear(); var lb = r.localBounds; foreach (var c in new[] { new Vector3(lb.min.x, 0, lb.min.z), new Vector3(lb.max.x, 0, lb.min.z), new Vector3(lb.max.x, 0, lb.max.z), new Vector3(lb.min.x, 0, lb.max.z) }) { var w = tr.TransformPoint(c); pts.Add(new Vector2(w.x, w.z)); } }
                    var h = Hull(pts); if (h.Count < 3) continue;
                    for (int i = 0; i < h.Count; i++)
                    {
                        Vector2 a = h[i], c = h[(i + 1) % h.Count], e = c - a; if (e.sqrMagnitude < 0.04f) continue;
                        var n = new Vector2(e.y, -e.x).normalized * width; // the hull is counter-clockwise: outward is to the right of the edge
                        int k = sv.Count;
                        sv.Add(new Vector3(a.x, y, a.y)); sv.Add(new Vector3(c.x, y, c.y)); sv.Add(new Vector3(c.x + n.x, y, c.y + n.y)); sv.Add(new Vector3(a.x + n.x, y, a.y + n.y));
                        su.Add(new Vector2(0, 0)); su.Add(new Vector2(1, 0)); su.Add(new Vector2(1, 1)); su.Add(new Vector2(0, 1));
                        st.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
                    }
                    nb++;
                }
                else if (b.size.y <= 7f && Mathf.Max(b.size.x, b.size.z) < 4f)
                {
                    var lb = r.localBounds; var ctr = tr.TransformPoint(new Vector3(lb.center.x, lb.min.y, lb.center.z));
                    var ax = tr.TransformVector(new Vector3(lb.extents.x, 0, 0)) * spread; var az = tr.TransformVector(new Vector3(0, 0, lb.extents.z)) * spread;
                    ax.y = 0; az.y = 0; if (ax.magnitude < 0.12f) ax = ax.normalized * 0.12f; if (az.magnitude < 0.12f) az = az.normalized * 0.12f;
                    int k = bv.Count; var p0 = new Vector3(ctr.x, y, ctr.z);
                    bv.Add(p0 - ax - az); bv.Add(p0 - ax + az); bv.Add(p0 + ax + az); bv.Add(p0 + ax - az);
                    bu.Add(new Vector2(-1, -1)); bu.Add(new Vector2(-1, 1)); bu.Add(new Vector2(1, 1)); bu.Add(new Vector2(1, -1));
                    bt.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                    np++;
                }
            }
            Part(root.transform, "contact shade 096 (strips)", sv, su, st, shader, 0f, strip, 1.6f, tint);
            Part(root.transform, "contact shade 096 (blobs)", bv, bu, bt, shader, 1f, blob, 1.3f, tint);
            Debug.Log($"ContactShade096: {nb} building feet ({hull} from mesh outlines, {nb - hull} from bounds), {st.Count / 6} strip quads; {np} prop blobs; 2 draw calls");
            return true;
        }

        /// <summary>The existing contact shade (World/Light/ContactShade.cs, the scene root `contact`, cut into OldTown by the swap): its strength x k, its tint left.</summary>
        public static bool Boost(float k)
        {
            var root = GameObject.Find("contact"); if (!root) return false;
            var seen = new HashSet<Material>(); int n = 0;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) foreach (var m in r.sharedMaterials) if (m && m.HasProperty("_Strength") && seen.Add(m)) { m.SetFloat("_Strength", Mathf.Clamp01(m.GetFloat("_Strength") * k)); n++; }
            Debug.Log($"ContactShade096: the scene's contact shade x{k} on {n} materials");
            return true;
        }

        static void Part(Transform parent, string name, List<Vector3> v, List<Vector2> u, List<int> t, Shader shader, float mode, float strength, float power, Color tint)
        {
            if (t.Count == 0) return;
            var m = new Mesh { name = name, indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            m.SetVertices(v); m.SetUVs(0, u); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.one;
            if (parent.lossyScale != Vector3.one) go.transform.SetParent(null, true);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); var mat = new Material(shader) { name = name };
            mat.SetFloat("_Mode", mode); mat.SetFloat("_Strength", strength); mat.SetFloat("_Power", power); mat.SetColor("_Tint", tint);
            mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        }

        static List<Vector2> Hull(List<Vector2> p)
        {
            p.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var h = new List<Vector2>();
            for (int pass = 0; pass < 2; pass++)
            {
                int start = h.Count;
                for (int i = 0; i < p.Count; i++)
                {
                    var q = pass == 0 ? p[i] : p[p.Count - 1 - i];
                    while (h.Count >= start + 2 && Cross(h[h.Count - 2], h[h.Count - 1], q) <= 0) h.RemoveAt(h.Count - 1);
                    h.Add(q);
                }
                h.RemoveAt(h.Count - 1);
            }
            return h;
        }
        static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }
}
