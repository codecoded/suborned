using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>Brief 026, task 3: contact shade where things meet the ground, baked as geometry because URP SSAO does nothing at the
    /// orthographic play zoom (Glint's finding; confirmed on the old city: its SSAO 0.4 → 0.9 moved no play-zoom pixel,
    /// briefs/026/old-decokit/compare-template.txt). Three kinds, all on the ground and drawn with Suborned/Contact Shade (multiply):
    ///   building feet: a strip outward from every building (from just outside its plinth, light.json contact.footOffset) and ground-standing civic mass footprint;
    ///   kerb feet: a strip on the carriageway along each kerbed edge (the gutter's shade under the kerb face);
    ///   standing street pieces (planters, tubs, bins, robots, vending, benches...): an elliptic blob under the piece's footprint.
    /// Widths and strengths are light.json `contact` (Glint tunes; defaults here). Meshes are cut in 64 m tiles, one per kind per tile, under
    /// a scene root `contact` (not the build root: Inspect counts only the export's layers). No shadow, no lighting, fog fades it out.</summary>
    public static class ContactShade
    {
        const float Tile = 64f, Lift = 0.004f;

        sealed class Buf { public readonly List<Vector3> v = new(); public readonly List<Vector2> uv = new(); public readonly List<int> t = new(); }
        static readonly Dictionary<(string kind, int x, int z), Buf> bufs = new();
        public static int Strips, Blobs, Feet;

        static Buf At(string kind, Vector3 p)
        {
            var key = (kind, Mathf.FloorToInt(p.x / Tile), Mathf.FloorToInt(p.z / Tile));
            if (!bufs.TryGetValue(key, out var b)) bufs[key] = b = new Buf();
            return b;
        }

        static void Quad(string kind, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            var buf = At(kind, (a + c) * 0.5f); int i = buf.v.Count;
            buf.v.Add(a); buf.v.Add(b); buf.v.Add(c); buf.v.Add(d);
            buf.uv.Add(ua); buf.uv.Add(ub); buf.uv.Add(uc); buf.uv.Add(ud);
            buf.t.Add(i); buf.t.Add(i + 1); buf.t.Add(i + 2); buf.t.Add(i); buf.t.Add(i + 2); buf.t.Add(i + 3);
        }

        static Vector3 W(Vector2 p, float y) => new(p.x, y, p.y);

        /// <summary>A strip along `edge` (the contact line, v = 0) to `far` (v = 1), point for point.</summary>
        static void Strip(string kind, Vector2[] edge, Vector2[] far, float y, bool closed)
        {
            int n = edge.Length; if (n < 2 || far == null || far.Length != n) return;
            for (int i = 0; i < (closed ? n : n - 1); i++)
            {
                int j = (i + 1) % n;
                if ((edge[j] - edge[i]).sqrMagnitude < 1e-4f) continue;
                Quad(kind, W(edge[i], y), W(edge[j], y), W(far[j], y), W(far[i], y), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
                Strips++;
            }
        }

        /// <summary>A ring's outline pushed `d` outward (each corner on its miter, spikes clamped as Poly's).</summary>
        static Vector2[] Outset(Vector2[] ring, float d)
        {
            int n = ring.Length; var r = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = ring[(i + n - 1) % n], cur = ring[i], next = ring[(i + 1) % n];
                Vector2 d1 = (cur - prev).normalized, d2 = (next - cur).normalized;
                Vector2 n1 = new(d1.y, -d1.x), n2 = new(d2.y, -d2.x); // right normals: outward on a CCW ring
                var m = n1 + n2; if (m.sqrMagnitude < 1e-6f) m = n1; m.Normalize();
                float cos = Mathf.Max(Vector2.Dot(m, n1), 0.34f);
                r[i] = cur + m * (d / cos);
            }
            return r;
        }

        static float F(object e, string k, float fallback) => e != null && J.Get(e, k) is double d ? (float)d : fallback;

        public static void Build(Look look, WorldDto world, Transform root)
        {
            bufs.Clear(); Strips = Blobs = Feet = 0;
            var cfg = look.Light("contact");
            if (cfg != null && J.Get(cfg, "enabled") is false) { Debug.Log("World: contact shade off (light.json contact.enabled false)"); return; }
            float footW = F(cfg, "footWidth", 0.8f), kerbW = F(cfg, "kerbWidth", 0.3f);
            // the wall-foot strip starts outside the facade plinth (FacadeKit: the plinth box stands 0.12 m proud of the wall), else the plinth covers it (Glint, r1)
            float footFrom = F(cfg, "footOffset", 0.14f);

            // building and ground-standing civic feet
            foreach (var m in world.Buildings.Concat(world.Civic.Where(c => c.Base < 1f && c.Kit == null)))
            {
                var ring = Poly.Clean(m.Ring); if (ring == null || ring.Length < 3) continue;
                if (Poly.SignedArea(ring) < 0f) ring = ring.Reverse().ToArray();
                Strip("foot", Outset(ring, footFrom), Outset(ring, footFrom + footW), m.Base + Lift, true); Feet++;
            }
            // kerb feet: on the carriageway, from its edge inward
            foreach (var r in world.Ground.Roads)
            {
                if (r.KerbHeight <= 0.02f) continue;
                var line = Poly.CleanLine(r.Line); if (line.Length < 2) continue;
                var (l, rr) = Poly.Ribbon(line, r.Width, false);
                var (li, ri) = Poly.Ribbon(line, Mathf.Max(0.1f, r.Width - 2f * kerbW), false);
                Strip("kerb", l, li, r.Level + Lift, false);
                Strip("kerb", rr, ri, r.Level + Lift, false);
            }
            // standing street pieces: a blob under each kit instance that stands on the ground
            var street = root.Find("street");
            if (street)
                foreach (var t in street.GetComponentsInChildren<Transform>(true))
                {
                    if (!PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) continue;
                    var rs = KitLods.Lod0(t.gameObject).ToArray(); if (rs.Length == 0) continue;
                    var b = rs[0].bounds; foreach (var x in rs) b.Encapsulate(x.bounds);
                    if (b.size.y < 0.3f || b.min.y > t.position.y + 0.4f) continue; // flat inlays, hanging baskets
                    float ex = Mathf.Min(b.extents.x, 1.6f) * 1.3f + 0.15f, ez = Mathf.Min(b.extents.z, 1.6f) * 1.3f + 0.15f;
                    float y = b.min.y + Lift; var c = b.center;
                    Quad("blob", new Vector3(c.x - ex, y, c.z - ez), new Vector3(c.x + ex, y, c.z - ez), new Vector3(c.x + ex, y, c.z + ez), new Vector3(c.x - ex, y, c.z + ez),
                        new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1));
                    Blobs++;
                }

            var shader = Shader.Find("Suborned/Contact Shade") ?? throw new InvalidDataException("World: shader Suborned/Contact Shade not found (Assets/Suborned/Shaders)");
            Material Mat(string kind, float strength, float power, int mode)
            {
                var m = new Material(shader) { name = "contact/" + kind };
                if (cfg != null && J.Get(cfg, "tint") is string hex && ColorUtility.TryParseHtmlString(hex, out var tint)) m.SetColor("_Tint", tint);
                m.SetFloat("_Strength", strength); m.SetFloat("_Power", power); m.SetFloat("_Mode", mode);
                return m;
            }
            var mats = new Dictionary<string, Material>
            {
                ["foot"] = Mat("foot", F(cfg, "footStrength", 0.45f), F(cfg, "footPower", 2.2f), 0),
                ["kerb"] = Mat("kerb", F(cfg, "kerbStrength", 0.4f), F(cfg, "kerbPower", 1.6f), 0),
                ["blob"] = Mat("blob", F(cfg, "blobStrength", 0.5f), F(cfg, "blobPower", 1.5f), 1),
            };
            var holder = Nodes.Group(null, "contact");
            foreach (var kv in bufs.OrderBy(k => k.Key.kind).ThenBy(k => k.Key.x).ThenBy(k => k.Key.z))
            {
                var buf = kv.Value; if (buf.t.Count == 0) continue;
                var mesh = new Mesh { name = $"contact {kv.Key.kind} {kv.Key.x} {kv.Key.z}", indexFormat = buf.v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(buf.v); mesh.SetUVs(0, buf.uv); mesh.SetTriangles(buf.t, 0); mesh.RecalculateBounds();
                var go = new GameObject(mesh.name) { isStatic = true }; go.transform.SetParent(holder, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mats[kv.Key.kind];
                mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            Debug.Log($"World: contact shade: {Feet} building feet, {Strips} strip quads, {Blobs} street-piece blobs, {holder.childCount} meshes (64 m tiles); light.json contact {(cfg == null ? "absent: defaults" : "applied")}");
        }
    }
}
