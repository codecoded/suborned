using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Play
{
    /// <summary>Brief 046 (Sky: "the selection ring drew over the agent's legs"): marks that lie on the ground (destination rings, the Watch's
    /// reach, the paused plan's rings and paths, the hint's ring, an order's pulse) are drawn in the world, not painted over it: flat ring and
    /// path meshes a few centimetres above the ground, in the transparent queue after the opaque figures and architecture, depth-tested, so
    /// legs, props and walls in front hide them. One mesh per colour each frame (Graphics.RenderMesh: no renderer, nothing for the GPU Resident
    /// Drawer to rebuild), with the sight fans' own material (URP Unlit, transparent, already in the build). Widths are given in HUD pixels at
    /// the play camera's zoom, as the painted marks were.</summary>
    public sealed partial class HudToolkit
    {
        sealed class GroundBatch { public Material Mat; public Mesh Mesh; public readonly List<Vector3> V = new(); public readonly List<int> T = new(); }
        readonly Dictionary<int, GroundBatch> groundBatches = new();
        const float GroundLift = 0.035f;

        /// <summary>Metres per HUD pixel on the ground at the play camera's zoom (orthographic); a perspective view gets the play zoom's.</summary>
        float GroundPxM
        {
            get
            {
                var cam = host.View; float H = root != null && root.layout.height > 0 ? root.layout.height : 1080f;
                return cam && cam.orthographic ? 2f * cam.orthographicSize / H : 2f * 14f / 1080f;
            }
        }

        GroundBatch GroundOf(Color c, bool under)
        {
            var c32 = (Color32)c; c32.a = (byte)(Mathf.RoundToInt(c32.a / 16f) * 16 > 255 ? 255 : Mathf.RoundToInt(c32.a / 16f) * 16); // alphas in 16 steps: few materials
            int key = (c32.r << 24) ^ (c32.g << 16) ^ (c32.b << 8) ^ c32.a ^ (under ? 0x55 << 1 : 0);
            if (groundBatches.TryGetValue(key, out var b)) return b;
            var tpl = host.fanTemplate ? host.fanTemplate : host.unlitTemplate;
            var m = new Material(tpl) { name = "ground-mark" }; m.SetColor("_BaseColor", c32); m.renderQueue = (int)RenderQueue.Transparent + (under ? 10 : 11);
            b = new GroundBatch { Mat = m, Mesh = new Mesh { name = "ground-marks" } }; b.Mesh.MarkDynamic(); groundBatches[key] = b; return b;
        }

        /// <summary>A ring on the ground round `c` (radius in metres), `widthPx` wide; dashed in 32 dashes; an ink underlay `underAlpha` (0: none)
        /// 2 px wider, under it.</summary>
        void GroundRing(Vector3 c, float r, Color col, float widthPx, bool dashed, float underAlpha = 0f)
        {
            if (r <= 0f || col.a <= 0.01f) return; float m = GroundPxM;
            if (underAlpha > 0f) RingQuads(GroundOf(new Color(Ink.r, Ink.g, Ink.b, underAlpha), true), c, r, (widthPx + 2f) * m, dashed);
            RingQuads(GroundOf(col, false), c, r, widthPx * m, dashed);
        }

        static void RingQuads(GroundBatch b, Vector3 c, float r, float w, bool dashed)
        {
            int n = Mathf.Clamp(Mathf.CeilToInt(r * 24f), 32, 96); if (dashed) n = 64; float y = c.y + GroundLift, ri = Mathf.Max(0f, r - w / 2f), ro = r + w / 2f;
            for (int i = 0; i < n; i++)
            {
                if (dashed && (i & 1) == 1) continue;
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                Quad(b, new Vector3(c.x + Mathf.Cos(a0) * ri, y, c.z + Mathf.Sin(a0) * ri), new Vector3(c.x + Mathf.Cos(a0) * ro, y, c.z + Mathf.Sin(a0) * ro),
                        new Vector3(c.x + Mathf.Cos(a1) * ro, y, c.z + Mathf.Sin(a1) * ro), new Vector3(c.x + Mathf.Cos(a1) * ri, y, c.z + Mathf.Sin(a1) * ri));
            }
        }

        /// <summary>A path on the ground through world points, `widthPx` wide; dashed in 12 px dashes (55 % on) as the painted one was.</summary>
        void GroundPath(List<Vector3> pts, Color col, float widthPx, bool dashed, float underAlpha = 0f)
        {
            if (pts == null || pts.Count < 2 || col.a <= 0.01f) return;
            if (underAlpha > 0f) PathQuads(pts, GroundOf(new Color(InkDeep.r, InkDeep.g, InkDeep.b, underAlpha), true), (widthPx + 2f) * GroundPxM, dashed, 1f * GroundPxM); // Glyph 0.2.65 review 7: an ink-deep line 1 px round each dash, so the route reads on cream paving
            PathQuads(pts, GroundOf(col, false), widthPx * GroundPxM, dashed, 0f);
        }

        void PathQuads(List<Vector3> pts, GroundBatch b, float width, bool dashed, float grow)
        {
            float m = GroundPxM, hw = width / 2f, dash = 12f * m;
            for (int j = 1; j < pts.Count; j++)
            {
                var a = pts[j - 1]; var e = pts[j]; var d = e - a; d.y = 0f; float len = d.magnitude; if (len < 1e-4f) continue;
                var f = d / len; var side = new Vector3(-f.z, 0f, f.x) * hw;
                int k = dashed ? Mathf.Max(1, Mathf.FloorToInt(len / dash)) : 1;
                for (int s = 0; s < k; s++)
                {
                    float t0 = dashed ? s / (float)k : 0f, t1 = dashed ? (s + 0.55f) / k : 1f;
                    var p0 = Vector3.Lerp(a, e, t0) + Vector3.up * GroundLift - f * grow; var p1 = Vector3.Lerp(a, e, t1) + Vector3.up * GroundLift + f * grow;
                    Quad(b, p0 - side, p0 + side, p1 + side, p1 - side);
                }
            }
        }

        /// <summary>The screen's directions laid on the ground (up: away from the camera; right), so a mark drawn "clockwise from 12 o'clock"
        /// on the ground reads that way at any yaw.</summary>
        (Vector3 up, Vector3 right) GroundAxes()
        {
            var cam = host.View; if (!cam) return (Vector3.forward, Vector3.right);
            var u = cam.transform.forward; u.y = 0f; if (u.sqrMagnitude < 1e-6f) u = cam.transform.up; u.y = 0f; u.Normalize();
            var r = cam.transform.right; r.y = 0f; r.Normalize(); return (u, r);
        }

        /// <summary>An arc on the ground round `c` (radius in metres) from `from` to `to` degrees, 0 at the screen's 12 o'clock, clockwise as
        /// seen on screen, `widthPx` wide: the move preview's clock and track (brief 046: on the ground, behind legs).</summary>
        void GroundArc(Vector3 c, float r, Color col, float widthPx, float from, float to, float underAlpha = 0f)
        {
            if (r <= 0f || col.a <= 0.01f || to <= from) return; var (u, rt) = GroundAxes(); float m = GroundPxM;
            if (underAlpha > 0f) ArcQuads(GroundOf(new Color(Ink.r, Ink.g, Ink.b, underAlpha), true), c, r, (widthPx + 2f) * m, from, to, u, rt);
            ArcQuads(GroundOf(col, false), c, r, widthPx * m, from, to, u, rt);
        }
        static void ArcQuads(GroundBatch b, Vector3 c, float r, float w, float from, float to, Vector3 u, Vector3 rt)
        {
            int n = Mathf.Clamp(Mathf.CeilToInt((to - from) / 6f), 2, 64); float y = c.y + GroundLift, ri = Mathf.Max(0f, r - w / 2f), ro = r + w / 2f;
            Vector3 P(float deg, float rad) { float a = deg * Mathf.Deg2Rad; var v = rt * Mathf.Sin(a) + u * Mathf.Cos(a); return new Vector3(c.x + v.x * rad, y, c.z + v.z * rad); }
            for (int i = 0; i < n; i++)
            {
                float a0 = Mathf.Lerp(from, to, i / (float)n), a1 = Mathf.Lerp(from, to, (i + 1) / (float)n);
                Quad(b, P(a0, ri), P(a0, ro), P(a1, ro), P(a1, ri));
            }
        }
        /// <summary>A filled disc on the ground (radius in metres): the move preview's dark middle.</summary>
        void GroundDisc(Vector3 c, float r, Color col) { if (r > 0f && col.a > 0.01f) RingQuads(GroundOf(col, true), c, r / 2f, r, false); }

        static void Quad(GroundBatch b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            int i = b.V.Count; b.V.Add(p0); b.V.Add(p1); b.V.Add(p2); b.V.Add(p3);
            b.T.Add(i); b.T.Add(i + 1); b.T.Add(i + 2); b.T.Add(i); b.T.Add(i + 2); b.T.Add(i + 3);
        }

        /// <summary>Start of the HUD's frame: no ground marks yet.</summary>
        void GroundBegin() { foreach (var b in groundBatches.Values) { b.V.Clear(); b.T.Clear(); } }

        /// <summary>End of the HUD's frame: this frame's marks to the play camera.</summary>
        void GroundFlush()
        {
            var cam = host.View; if (!cam) return;
            foreach (var b in groundBatches.Values)
            {
                if (b.V.Count == 0) continue;
                b.Mesh.Clear(); b.Mesh.SetVertices(b.V); b.Mesh.SetTriangles(b.T, 0); b.Mesh.RecalculateBounds();
                Graphics.RenderMesh(new RenderParams(b.Mat) { camera = cam, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false, layer = 0 }, b.Mesh, 0, Matrix4x4.identity);
            }
        }
    }
}
