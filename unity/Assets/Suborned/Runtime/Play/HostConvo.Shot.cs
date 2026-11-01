using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 032, 067 H: the conversation scene's shots (split from HostConvo.cs, brief 085): the ladder that frames the speakers, the intercut, what is hidden from the lens.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>Glyph 067 Rev 2 H1 (Sky on 0.2.122: "the camera frames both, agent and subject, faces visible, bubbles on screen, nothing
        /// occluding"): a ladder of shots that always holds both people, the first that works. (1) The profile two-shot for a pair up to 6 m apart:
        /// the lens on the pair's perpendicular, 1.9 m up, 8° down, at fit = clamp(1.44 × sep, 3.6, 8) m, then ±12°, then ±25° round the pair's
        /// middle, then the same five at 0.8 × fit (never under 2.8 m); at an equal angle the side the camera was on (the player's left stays left
        /// across the cut), else the side the subject faces; a lens is clear when the lines to each head and chest keep 0.4 m off every solid,
        /// nobody shows more than 115° of back and nothing solid stands within 1.6 m of it. (2) None clear: the high two-shot, the play pitch (38°)
        /// from 7 m with the same field, centred on the pair, at the yaw the player left (an orthographic lens, so the city's cut and silhouettes
        /// hold, H4). (3) A pair more than 6 m apart: the intercut, each speaker alone 2.6 m from where they face, cut with the bubble. The
        /// over-the-shoulder fallback is gone (it hid the agent's face behind his back).</summary>
        bool ConvoFrameShot(Convo c)
        {
            if (!cam) return false;
            var tb = ConvoView(c.KeyB); var ta = ConvoView(c.KeyA); if (!tb) return false;
            Vector3 b = tb.position, a = ta && ta.gameObject.activeInHierarchy ? ta.position : b;
            var ab = b - a; ab.y = 0f; float sep = ab.magnitude;
            c.Single = false; c.Framed = -1;
            if (sep < 0.2f)
            {   // the two on one point (the engine's own, the views not yet apart): the person alone frames both, from where they face
                c.Shot = "single"; c.Single = true; (c.From, c.Look, c.Side) = SingleShot(b, FigureFacing(c.KeyB) ?? Vector3.forward); c.Dist = ConvoSingleDist; return true;
            }
            var dir = ab / sep;
            if (c.Alone || sep > ConvoTwoShotM && ConvoForceShot != "high")
            {   // (3) the intercut: each speaker alone, from where they face, cut with the bubble (ConvoFrame). A call: three people across a square, none facing another, each framed as they stand
                c.Shot = "intercut"; c.Single = true; c.Dist = ConvoSingleDist;
                void Cut(string who, Vector3 p, Vector3 face) { var (f, l, _) = SingleShot(p, face); c.Cuts[who] = (f, l); }
                Cut(c.KeyA, a, c.Alone ? FigureFacing(c.KeyA) ?? dir : dir); Cut(c.KeyB, b, c.Alone ? FigureFacing(c.KeyB) ?? -dir : -dir);
                foreach (var (sp, _) in c.Lines) if (!c.Cuts.ContainsKey(sp) && ConvoView(sp) is Transform st && st.gameObject.activeInHierarchy) Cut(sp, st.position, FigureFacing(sp) ?? Vector3.forward);
                IntercutOn(c, c.Lines.Count > 0 ? c.Lines[0].speaker : c.KeyB); return true;
            }
            var mid = (a + b) / 2f; var perp = new Vector3(-dir.z, 0f, dir.x);
            if (ConvoForceShot != "high")
            {   // (1) the profile two-shot
                float fit = Mathf.Clamp(ConvoFitPerM * sep, ConvoDist, ConvoFitMaxM);
                var here = cam.transform.position - mid; here.y = 0f; float sd = Vector3.Dot(perp, here);
                if (Mathf.Abs(sd) < 0.05f * Mathf.Max(0.01f, here.magnitude) && FigureFacing(c.KeyB) is Vector3 fb) sd = Vector3.Dot(perp, fb); // the camera on the pair's line: the side the subject's face is toward
                var sides = sd >= 0f ? new[] { perp, -perp } : new[] { -perp, perp };
                foreach (float dist in new[] { fit, Mathf.Max(0.8f * fit, ConvoNearM) })
                    foreach (int deg in ConvoAngles)
                        foreach (var s0 in sides)
                        {
                            var s = Quaternion.Euler(0f, deg, 0f) * s0; var from = mid + s * dist; from.y = mid.y + ConvoHeight;
                            if (!TwoShotClear(a, b, dir, from)) continue;
                            c.Shot = "profile"; c.Side = s; c.Dist = dist; c.From = from; c.Look = mid + Vector3.up * ConvoHeight + Vector3.down * Mathf.Tan(ConvoPitch * Mathf.Deg2Rad) * dist; return true;
                        }
            }
            // (2) the high two-shot: no lens on the ground holds both clear (the café door, the Office front)
            float yaw = cam.transform.eulerAngles.y; var hr = Quaternion.Euler(ConvoHighPitch, yaw, 0f); var fwd = hr * Vector3.forward;
            c.Shot = "high"; c.Look = mid + Vector3.up * 0.9f; c.From = c.Look - fwd * ConvoHighM; c.Dist = ConvoHighM; c.Side = -new Vector3(fwd.x, 0f, fwd.z).normalized;
            // the 35° field at 7 m (2.2 m half height), opened to hold both whole: the pair's spread across the frame and up it (a pair lined up with
            // the view spreads up the frame), each person 1 m wide and 1.75 m tall, inside the 6 % bars
            var d = b - a; float hx = Mathf.Abs(Vector3.Dot(d, hr * Vector3.right)) / 2f + 1.0f, hy = (Mathf.Abs(Vector3.Dot(d, hr * Vector3.up)) / 2f + 1.3f) / 0.88f;
            c.Ortho = Mathf.Max(ConvoHighM * Mathf.Tan(ConvoFov * 0.5f * Mathf.Deg2Rad), hy, hx / Mathf.Max(1f, cam.aspect));
            return true;
        }

        /// <summary>A person's drawn facing (world, flat), else null.</summary>
        Vector3? FigureFacing(string key) => views?.FigureYaw(key is "scout" or "fixer" or "tech" or "enforcer" ? "op/" + key : key) is float fy ? Quaternion.Euler(0f, fy, 0f) * Vector3.forward : (Vector3?)null;

        /// <summary>One person alone, from where they face: 2.6 m out, 1.9 m up, looking at the head; the nearest clear bearing to the facing within
        /// 60° (faces stay visible), else the facing itself.</summary>
        (Vector3 from, Vector3 look, Vector3 side) SingleShot(Vector3 p, Vector3 face)
        {
            face.y = 0f; if (face.sqrMagnitude < 1e-4f) face = Vector3.forward; face.Normalize();
            var look = p + Vector3.up * 1.45f; var side = face; bool found = false;
            var bearings = new[] { 0, 30, -30, 60, -60 };
            for (int pass = 0; pass < 2 && !found; pass++) // a clear line first; else at least a camera outside every mass
                foreach (int deg in bearings)
                {
                    var s = Quaternion.Euler(0f, deg, 0f) * face; var from = look + s * ConvoSingleDist; from.y = p.y + ConvoHeight;
                    if (pass == 0 ? !ConvoClear(look, from) : !ConvoClear(from, from)) continue;
                    side = s; found = true; break;
                }
            var f = look + side * ConvoSingleDist; f.y = p.y + ConvoHeight; return (f, look, side);
        }

        /// <summary>The intercut on the speaker of the line now; a voice with no figure (Milena) keeps the shot up, or opens on KeyB: true when the shot changed.</summary>
        bool IntercutOn(Convo c, string speaker)
        {
            if (!c.Cuts.ContainsKey(speaker)) { if (c.Framed >= 0) return false; speaker = c.KeyB; }
            if (speaker == c.Cut) return false;
            var (from, look) = c.Cuts[speaker]; c.Cut = speaker; c.Framed = speaker == c.KeyA ? 0 : speaker == c.KeyB ? 1 : 2; c.From = from; c.Look = look; c.Side = (from - look).normalized; return true;
        }

        /// <summary>Glyph 067 Rev 2 H1.1: a two-shot lens is clear when nobody shows more than 115° of back to it (each faces the other), nothing solid
        /// stands within 1.6 m of it, and the lines from it to each head (1.5 m) and chest (1.1 m) keep 0.4 m off every solid (ConvoClear).</summary>
        bool TwoShotClear(Vector3 a, Vector3 b, Vector3 dir, Vector3 lens)
        {
            var la = lens - a; la.y = 0f; var lb = lens - b; lb.y = 0f;
            if (Vector3.Angle(dir, la) > ConvoBackDeg || Vector3.Angle(-dir, lb) > ConvoBackDeg) return false;
            if (!LensClear(lens)) return false;
            foreach (var p in new[] { a, b }) if (!ConvoClear(p + Vector3.up * 1.5f, lens) || !ConvoClear(p + Vector3.up * 1.1f, lens)) return false;
            return true;
        }

        /// <summary>No wall or mass (props aside: the scene hides those) within 1.6 m of the lens.</summary>
        bool LensClear(Vector3 lens)
        {
            FollowIndex(); var (cx, cz) = FCell(lens.x, lens.z); float r2 = ConvoLensClearM * ConvoLensClearM;
            for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
            {
                if (!fIndex.TryGetValue((cx + dx, cz + dz), out var l)) continue;
                foreach (var (r, bb, prop) in l) if (!prop && r && r.enabled && bb.SqrDistance(lens) < r2) return false;
            }
            return true;
        }

        /// <summary>The boom's line from the pair's heads to the camera keeps 0.4 m clear of every wall (HostFollow's index of the street's solids).</summary>
        bool ConvoClear(Vector3 from, Vector3 to)
        {
            FollowIndex(); var ray = new Ray(from, (to - from).normalized); float len = Vector3.Distance(from, to);
            var seen = new HashSet<Renderer>();
            for (float t = 0f; t <= len + 4f; t += 4f)
            {
                var q = ray.GetPoint(Mathf.Min(t, len));
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                {
                    var (cx, cz) = FCell(q.x, q.z); if (!fIndex.TryGetValue((cx + dx, cz + dz), out var l)) continue;
                    foreach (var (r, bb, prop) in l)
                    {
                        if (prop || !seen.Add(r) || !r || !r.enabled) continue; var e = bb; e.Expand(0.8f); // 0.4 m each side
                        if (e.Contains(to)) return false; // the camera inside a mass (Latch 2026-10-21: Inez at the café door was shot from inside the café, through its door frame)
                        if (e.Contains(from)) continue; // the pair under an awning: not between them and the camera
                        if (e.IntersectRay(ray, out var hit) && hit <= len) return false;
                    }
                }
            }
            return true;
        }

        readonly List<Renderer> cvHidden = new();
        /// <summary>What would stand in the shot: street props (a planter's palm) on the line from the camera to either person's head or chest
        /// (1.5 m and 1.1 m: a potted palm's fronds hid Inez's body under a clear head line) or within 1.6 m of the camera, and people (passers-by, the cell) standing in those lines or within 1.6 m of the camera, are not drawn while the
        /// scene plays (forceRenderingOff; back at the cut back). A low solid (a planter, a tree bed: under 4 m tall, 6 m wide) on those lines goes too; facades are never hidden (the camera picked a side clear of them).
        /// The orthographic high shot reads its lines along the view; the intercut, the one speaker framed.</summary>
        void ConvoHideOccluders(Convo c)
        {
            cvHidden.Clear(); var from = c.From; bool high = c.Shot == "high"; var fwd = (c.Look - c.From).normalized;
            var heads = new List<Vector3>(); foreach (var k in ConvoFramed(c)) if (ConvoView(k) is Transform t && t.gameObject.activeInHierarchy) { heads.Add(t.position + Vector3.up * 1.5f); heads.Add(t.position + Vector3.up * 1.1f); }
            // the orthographic high shot sees along parallel lines: a head's line runs back from it along the view, not to one lens point
            bool OnLine(Bounds b) { foreach (var h in heads) { var o = high ? h - fwd * 30f : from; var ray = new Ray(o, (h - o).normalized); float len = Vector3.Distance(o, h); var e = b; e.Expand(0.3f); if (e.IntersectRay(ray, out var hit) && hit < len - 0.4f) return true; } return !high && b.SqrDistance(from) < 1.6f * 1.6f; }
            FollowIndex(); var seen = new HashSet<Renderer>();
            var near0 = high ? c.Look : from;
            foreach (var l in fIndex.Values) foreach (var (r, b, prop) in l) if ((prop || b.size.y < 4f && Mathf.Max(b.size.x, b.size.z) < 6f) && r && r.enabled && !r.forceRenderingOff && seen.Add(r) && (b.center - near0).sqrMagnitude < 30f * 30f && OnLine(b)) { r.forceRenderingOff = true; cvHidden.Add(r); }
            var keep = new HashSet<Transform>(); foreach (var k in new[] { c.KeyA, c.KeyB }) if (ConvoView(k) is Transform t) keep.Add(t); foreach (var (k, _) in c.Lines) if (ConvoView(k) is Transform t) keep.Add(t); // no speaker is ever hidden (the intercut's others too)
            foreach (var (n, x) in views.All())
            {
                if (!x || !x.gameObject.activeInHierarchy || keep.Contains(x)) continue;
                var p = x.position; if ((p - near0).sqrMagnitude > 25f * 25f || views.WalkingNow(n)) continue; // brief 085: a passer-by walks past in front, as the street does; only someone standing in the line is not drawn
                var bb = new Bounds(p + Vector3.up * 0.9f, new Vector3(0.7f, 1.8f, 0.7f)); if (!OnLine(bb)) continue;
                foreach (var r in x.GetComponentsInChildren<Renderer>()) if (r.enabled && !r.forceRenderingOff) { r.forceRenderingOff = true; cvHidden.Add(r); }
            }
            if (c.Solo.Length > 0) foreach (var (k, _) in c.Lines) if (k != c.Solo && ConvoView(k) is Transform o) foreach (var r in o.GetComponentsInChildren<Renderer>()) if (r.enabled && !r.forceRenderingOff) { r.forceRenderingOff = true; cvHidden.Add(r); } // Listen's last line: he is alone
        }
    }
}
