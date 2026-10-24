using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Engine 0.28/0.29: the people's cones on the ground (Glyph's hud29.spec.json; the shapes are HostNotice's, from the engine's reads).
    /// In the ground batches (HudToolkit.Ground): the signal layer, unlit, depth-tested, so legs in front hide them. Widths are metres on the ground.
    ///  · civilian wedge: rim only, cream 0.075 m over ink-deep 0.17 m (alpha 0.6), no fill; a cut (a wall short of the reach) and the edge of dead
    ///    ground: cream 0.05 m over ink-deep 0.12 m (alpha 0.55); dead ground (seen over, not into) hatched at 45 degrees, cream 0.045 m lines on a
    ///    0.34 m pitch at alpha 0.7, never a tint;
    ///  · security ring: red 0.07 m over ink-deep 0.14 m (alpha 0.5), no fill, cut like a fan, no hatch;
    ///  · near-body ring: a dashed circle of NearCm at the civilian's feet, 36 dashes 56 % on, cream 0.07 m over ink-deep 0.14 m (alpha 0.6).
    /// Colours: kit.uss --cream, --ink-deep; the style contract's cone red. Nothing new.</summary>
    public sealed partial class HudToolkit
    {
        const float WedgeRim = 0.075f, WedgeEdge = 0.17f, WedgeEdgeA = 0.6f, CutRim = 0.05f, CutEdge = 0.12f, CutEdgeA = 0.55f;
        const float HatchW = 0.045f, HatchPitch = 0.34f, HatchA = 0.7f, SecRim = 0.07f, SecEdge = 0.14f, SecEdgeA = 0.5f;
        const float NearRim = 0.07f, NearEdge = 0.14f, NearEdgeA = 0.6f, NearOn = 0.56f; const int NearDashes = 36;

        /// <summary>The proof reads it: the cones drawn this frame ("id:kind[+near]" joined).</summary>
        public static string ConesShown => live?.host?.PersonCones is { Count: > 0 } pc ? ConesWords(pc) : ""; // Redline 064 #6: built when a proof reads it, not every frame
        static string ConesWords(List<HandoverHost.ConeMark> pc) { var said = new List<string>(); foreach (var c in pc) said.Add(c.Id + ":" + c.Kind + (c.Near ? "+near" : "")); return string.Join(" ", said); }

        void LineM(GroundBatch b, Vector3 a, Vector3 e, float w)
        {
            var d = e - a; d.y = 0f; float len = d.magnitude; if (len < 1e-4f) return;
            var f = d / len; var side = new Vector3(-f.z, 0f, f.x) * (w / 2f); var lift = Vector3.up * GroundLift; var ext = f * (w / 2f); // square ends that meet at corners
            Quad(b, a - ext - side + lift, a - ext + side + lift, e + ext + side + lift, e + ext - side + lift);
        }

        void EdgedLine(Vector3 a, Vector3 e, Color top, float wTop, float wUnder, float underA, float alpha)
        {
            LineM(GroundOf(new Color(InkDeep.r, InkDeep.g, InkDeep.b, underA * alpha), true), a, e, wUnder);
            LineM(GroundOf(new Color(top.r, top.g, top.b, top.a * alpha), false), a, e, wTop);
        }

        /// <summary>45 degree lines (x − z = c, every HatchPitch) across a convex quad on the ground.</summary>
        void HatchQuad(GroundBatch b, Vector3[] q)
        {
            float lo = float.MaxValue, hi = float.MinValue; foreach (var p in q) { float c = (p.x - p.z) / Mathf.Sqrt(2f); lo = Mathf.Min(lo, c); hi = Mathf.Max(hi, c); }
            for (float c = Mathf.Ceil(lo / HatchPitch) * HatchPitch; c < hi; c += HatchPitch)
            {
                Vector3? p0 = null, p1 = null;
                for (int i = 0; i < 4; i++)
                {
                    var a = q[i]; var e = q[(i + 1) % 4]; float ca = (a.x - a.z) / Mathf.Sqrt(2f) - c, ce = (e.x - e.z) / Mathf.Sqrt(2f) - c;
                    if ((ca < 0f) == (ce < 0f)) continue; var x = Vector3.Lerp(a, e, ca / (ca - ce));
                    if (p0 == null) p0 = x; else { p1 = x; break; }
                }
                if (p0.HasValue && p1.HasValue) LineM(b, p0.Value, p1.Value, HatchW);
            }
        }

        /// <summary>Per frame (between GroundBegin and GroundFlush): the people's cones HostNotice chose.</summary>
        void ConesDraw()
        {
            var list = host.PersonCones; if (list.Count == 0) return;
            foreach (var c in list)
            {
                float al = c.Alpha; bool ring = c.Kind == "ring";
                for (int j = 0; j + 1 < c.Outline.Count; j++)
                {
                    var a = c.Outline[j]; var e = c.Outline[j + 1];
                    if (ring) EdgedLine(a, e, Red, SecRim, SecEdge, SecEdgeA, al);
                    else if (j < c.Cut.Count && c.Cut[j]) EdgedLine(a, e, Cream, CutRim, CutEdge, CutEdgeA, al);
                    else EdgedLine(a, e, Cream, WedgeRim, WedgeEdge, WedgeEdgeA, al);
                }
                if (!ring)
                {
                    var hb = GroundOf(new Color(Cream.r, Cream.g, Cream.b, HatchA * al), false);
                    foreach (var q in c.Dead) HatchQuad(hb, q);
                    foreach (var (a, e) in c.DeadEdges) EdgedLine(a, e, Cream, CutRim, CutEdge, CutEdgeA, al);
                }
                if (c.Near && c.NearM > 0f)
                {   // the near-body ring: a place, not a direction (dashed)
                    var under = GroundOf(new Color(InkDeep.r, InkDeep.g, InkDeep.b, NearEdgeA * al), true); var top = GroundOf(new Color(Cream.r, Cream.g, Cream.b, al), false);
                    for (int i = 0; i < NearDashes; i++)
                    {
                        float a0 = i * Mathf.PI * 2f / NearDashes, a1 = (i + NearOn) * Mathf.PI * 2f / NearDashes;
                        int seg = 3; for (int s = 0; s < seg; s++)
                        {
                            float t0 = Mathf.Lerp(a0, a1, s / (float)seg), t1 = Mathf.Lerp(a0, a1, (s + 1) / (float)seg);
                            var p0 = c.At + new Vector3(Mathf.Cos(t0), 0f, Mathf.Sin(t0)) * c.NearM; var p1 = c.At + new Vector3(Mathf.Cos(t1), 0f, Mathf.Sin(t1)) * c.NearM;
                            LineM(under, p0, p1, NearEdge); LineM(top, p0, p1, NearRim);
                        }
                    }
                }
            }
        }
    }
}
