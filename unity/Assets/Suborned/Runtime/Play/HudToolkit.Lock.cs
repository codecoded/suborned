using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 067 D (Glyph, Sky on 0.2.122: "when the rivals move I kept losing the target on them"): the lock reticle. A lasting fire
    /// order already follows its target in the engine (Gun.Order); what was lost was the eye: the 049 ring is a ground decal, hidden by cover
    /// and legs among a fight's rings. So a screen-space mark rides over the target's head, never depth-tested: the kit's `disc_dark_24` drawn
    /// 26 px with `overlays/crosshair` 16 px cream, and under it the shooters' cameos (12 px in the chrome ring, overlapped 4 px, up to 4).
    /// States by shape and motion only: set (1.8 → 1.0 in 0.18 s and one ring leaving), locked (still), held (the disc 70 %, the crosshair
    /// 55 %, a dashed cream ring turning 0.25 turn/s: drawing, walking in, out of range, no line), released (fades, 1.0 → 0.8, 0.25 s). A
    /// target off screen: an edge chip (plate_dark 22 × 52, the lock disc and a pointer turned toward it). A re-target: the old reticle releases
    /// and the new one sets (Sky 2026-10-26, "way too crowded": the 0.25 s hand-off line between them is gone; no line in a fight). The same
    /// target given again: one 0.25 s pulse. View only.
    /// Brief 101 step 6 (Glyph's S mocks): the disc goes; the reticle is a crosshair laid on the street (cream on an ink keyline, the mock's), and why it waits is a
    /// street label under it, never a plate.</summary>
    public sealed partial class HudToolkit
    {
        sealed class LockMark
        {
            public string Id = ""; public VisualElement Root, Disc, Cross, Faces, Edge, WhyPlate; public EdgeTabEl EdgeTab; public Label Why; public KeyText WhyK; public Paint Ring; public readonly List<int> Shooters = new();
            /// <summary>Glyph 067 D3: when each shooter joined the lock (a later one's cameo slides into the row in 0.15 s; the set's own don't).</summary>
            public readonly Dictionary<int, float> Joined = new();
            public float Born = -9f, Gone = -1f, Given = -9f, PulseAt = -9f; public bool Locked, Seen; public Vector2 At; public bool OnScreen;
        }
        readonly Dictionary<string, LockMark> reticles = new(); readonly List<string> reticleGone = new();
        /// <summary>The proof reads it: the reticles drawn now, "id:state:shooters" each.</summary>
        public static string ReticlesShown { get; private set; } = "";
        public const float ReticleD = 26f, ReticleUp = 2.55f;
        /// <summary>Glyph G3 (0.2.135): the focus's pips sit 4 px under the lock disc's held ring (the disc's foot is 6 px above its anchor, the dashed
        /// ring 3.75 px past it; the pips' ink rim 3.6 px), and the wait plate 4 px under them.</summary>
        const float RingPast = 3.75f, PipsUnderDisc = -6f + RingPast + 4f + 3.6f, PipsH = 7.2f;

        LockMark NewReticle(string id)
        {
            var r = new LockMark { Id = id };
            r.Root = E(marks, null, 0, 0, ReticleD, ReticleD + 18f); r.Root.pickingMode = PickingMode.Ignore; r.Root.style.overflow = Overflow.Visible;
            r.Ring = P(r.Root, -12, -12, ReticleD + 24, ReticleD + 24, (p, rc) => ReticleRing(r, p, rc));
            r.Disc = E(r.Root, null, 0, 0, ReticleD, ReticleD); r.Disc.pickingMode = PickingMode.Ignore; r.Disc.style.overflow = Overflow.Visible;
            r.Cross = P(r.Disc, 0, 0, ReticleD, ReticleD, (p, rc) => KeyedCross(p, rc.center, 8.5f)); // brief 101 step 6: the crosshair on the street, no disc
            r.Faces = E(r.Root, null, 0, ReticleD + 1f, float.NaN, 16); r.Faces.style.flexDirection = FlexDirection.Row; r.Faces.pickingMode = PickingMode.Ignore;
            r.EdgeTab = NewEdgeTab("lock"); r.Edge = r.EdgeTab.Root; // brief 101 round 3 (Glyph's rule 2): a round tab with the ink chevron
            manifest.Add((r.Root, "image", () => "lock reticle")); manifest.Add((r.Edge, "image", () => "lock edge chip"));
            // engine 0.35 (the sticky focus): why it waits, one or two words on a plate_dark under the disc (GDD §10: "aiming, walking in, no line")
            r.WhyPlate = E(r.Root); r.WhyPlate.style.position = Position.Absolute; r.WhyPlate.style.top = ReticleD + 4f; r.WhyPlate.pickingMode = PickingMode.Ignore; // brief 101 step 6: a street label, no plate
            r.WhyK = KText(r.WhyPlate, 13f, "k-semi"); r.Why = r.WhyK.Face; Show(r.WhyPlate, false);
            manifest.Add((r.WhyPlate, "marker", () => r.Why.text)); // a world marker on the focus disc (at most two words), shown at rest in a fight: never the lock pill's kind
            return r;
        }

        /// <summary>The set beat's ring leaving, the held state's turning dashed ring, the same-target pulse (painted round the disc).</summary>
        void ReticleRing(LockMark r, Painter2D p, Rect rc)
        {
            float now = Time.unscaledTime, rad = ReticleD / 2f; var c = new Vector2(rc.width / 2f, ReticleD / 2f + 12f);
            float set = (now - r.Born) / 0.18f;
            if (set >= 0f && set < 1.6f) { float u = Mathf.Clamp01(set / 1.6f); p.strokeColor = new Color(Cream.r, Cream.g, Cream.b, 0.9f * (1f - u)); p.lineWidth = 3f; p.BeginPath(); p.Arc(c, rad + 2f + 9f * u, Deg(0), Deg(360)); p.Stroke(); } // one 3 px ring leaving it
            if (!r.Locked && r.Gone < 0f)
            {   // held: a dashed cream ring (3 on / 3 off, 1.5 px) turning 0.25 turn/s
                float circ = 2f * Mathf.PI * (rad + 3f); int n = Mathf.Max(6, Mathf.RoundToInt(circ / 6f)); float rot = now * 90f;
                p.strokeColor = Cream; p.lineWidth = 1.5f;
                for (int i = 0; i < n; i++) { float a0 = rot + i * 360f / n; p.BeginPath(); p.Arc(c, rad + 3f, Deg(a0), Deg(a0 + 180f / n)); p.Stroke(); }
            }
        }

        /// <summary>Per frame (the hold preset's marks): the reticles over the lasting fire orders' targets.</summary>
        void LockReticles(bool on)
        {
            var h = host; float now = Time.unscaledTime; var bits = new List<string>();
            var live = on && !m.Ended && h.Game != null ? h.FireLockTargets() : new List<(string id, List<int> shooters, bool locked, float given)>();
            foreach (var r in reticles.Values) r.Seen = false;
            foreach (var (id, shooters, locked, given) in live)
            {
                if (!reticles.TryGetValue(id, out var r)) { r = NewReticle(id); reticles[id] = r; r.Born = now; r.Given = given; }
                else if (r.Gone >= 0f) { r.Gone = -1f; r.Born = now; r.Given = given; } // set again
                else if (given > r.Given + 0.05f) { r.Given = given; r.PulseAt = now; } // the same target given again: one pulse
                r.Seen = true; r.Locked = locked; r.Shooters.Clear(); r.Shooters.AddRange(shooters);
                bool setNow = now - r.Born < 0.01f; foreach (var k in shooters) if (!r.Joined.ContainsKey(k) || setNow) r.Joined[k] = setNow ? -9f : now; // the set beat brings its own faces; a later shooter slides in
                if (r.Joined.Count > shooters.Count) foreach (var k in new List<int>(r.Joined.Keys)) if (!shooters.Contains(k)) r.Joined.Remove(k);
            }
            foreach (var r in reticles.Values) if (!r.Seen && r.Gone < 0f) r.Gone = now;
            reticleGone.Clear();
            foreach (var r in reticles.Values)
            {
                float age = r.Gone >= 0f ? now - r.Gone : -1f; if (age > 0.25f) { reticleGone.Add(r.Id); continue; }
                float up = r.Id.StartsWith("car:") ? 1.9f : ReticleUp + (FightCues.Target ? 0.4f : 0f); /* brief 084: clear of the target's corners */
                var w = h.DrawnAt(r.Id); Vector2 q = default; bool proj = w.HasValue && Proj(w.Value + Vector3.up * up, out q) && InView(q);
                if (proj) { r.At = q; r.OnScreen = true; }
                else if (w.HasValue && h.View) r.OnScreen = false;
                // the disc over the head
                Show(r.Root, r.OnScreen); Show(r.Edge, !r.OnScreen && w.HasValue && r.Gone < 0f && !FightCues.Edge);
                if (r.OnScreen)
                {
                    float set = Mathf.Clamp01((now - r.Born) / 0.18f), ease = 1f - (1f - set) * (1f - set), scale = Mathf.Lerp(1.8f, 1f, ease);
                    float pl = now - r.PulseAt; if (pl >= 0f && pl < 0.25f) scale *= 1f + 0.15f * Mathf.Sin(pl / 0.25f * Mathf.PI);
                    float fade = age >= 0f ? 1f - age / 0.25f : 1f; if (age >= 0f) scale *= Mathf.Lerp(1f, 0.8f, age / 0.25f);
                    r.Root.style.left = r.At.x - ReticleD / 2f; r.Root.style.top = r.At.y - ReticleD - 6f; // 6 px above the head bar's point
                    r.Disc.style.scale = new Scale(new Vector3(scale, scale, 1f)); r.Root.style.opacity = fade;
                    r.Disc.style.opacity = r.Locked || age >= 0f ? 1f : 0.7f; r.Cross.style.opacity = r.Locked || age >= 0f ? 1f : 0.55f;
                    // engine 0.35 (one input, Glyph b-final: "no cameos under it"): the focus disc alone, and why it waits under it
                    bool one = h.OneInput; Show(r.Faces, !one);
                    string why = one && !r.Locked && r.Gone < 0f ? h.FocusWhy(r.Id, r.Shooters) : ""; Show(r.WhyPlate, why.Length > 0);
                    if (why.Length > 0) { bool pips = false; foreach (var f in m.Fighters) if (f.Id == r.Id) pips = true; r.WhyPlate.style.top = ReticleD + RingPast + 4f + (pips ? PipsH + 4f : 0f); r.WhyK.Set(why); float ww = float.IsNaN(r.WhyPlate.resolvedStyle.width) ? 80f : r.WhyPlate.resolvedStyle.width; r.WhyPlate.style.left = ReticleD / 2f - ww / 2f; }
                    // the shooters' faces under the disc
                    while (r.Faces.childCount < r.Shooters.Count && r.Faces.childCount < 4) { var f = SFace(r.Faces, 16f, 1.6f); f.style.marginLeft = r.Faces.childCount == 1 ? 0 : -3; } // brief 101 round 3: the shooters' faces in the cream ring
                    for (int i = 0; i < r.Faces.childCount; i++)
                    {
                        bool has = i < r.Shooters.Count; Show(r.Faces[i], has); if (!has) continue; CameoOf(r.Faces[i], r.Shooters[i]);
                        float js = r.Joined.TryGetValue(r.Shooters[i], out var ja) && ja > 0f ? Mathf.Clamp01((now - ja) / 0.15f) : 1f, je = 1f - (1f - js) * (1f - js); // Glyph 067 D3: one more shooter's cameo slides into the row in 0.15 s
                        r.Faces[i].style.translate = new Translate(Mathf.Lerp(10f, 0f, je), 0f); r.Faces[i].style.opacity = je;
                    }
                    float fw = Mathf.Min(4, r.Shooters.Count) * 13f + 3f; r.Faces.style.left = ReticleD / 2f - fw / 2f;
                    r.Ring.MarkDirtyRepaint(); r.Root.BringToFront();
                }
                else if (r.Edge.resolvedStyle.display != DisplayStyle.None && w.HasValue && h.View is UnityEngine.Camera cam)
                {   // the edge chip: at the screen edge on the line from the centre toward the target, its pointer turned to it
                    var sp = cam.WorldToScreenPoint(w.Value); var size = root.panel.visualTree.layout.size; var c = new Vector2(size.x / 2f, size.y / 2f);
                    var d = new Vector2(sp.x - Screen.width / 2f, Screen.height / 2f - sp.y); if (sp.z < 0f) d = -d; if (d.sqrMagnitude < 1f) d = Vector2.up;
                    float k = Mathf.Min((size.x / 2f - 40f) / Mathf.Max(1e-3f, Mathf.Abs(d.x)), (size.y / 2f - 24f) / Mathf.Max(1e-3f, Mathf.Abs(d.y)));
                    var at = c + d * k; EdgeTabAt(r.EdgeTab, at, Mathf.Atan2(d.y, d.x));
                }
                bits.Add($"{r.Id}:{(r.Gone >= 0f ? "released" : now - r.Born < 0.18f ? "set" : r.Locked ? "locked" : "held")}:{string.Join(",", r.Shooters)}{(r.OnScreen ? "" : ":edge")}{(r.WhyPlate != null && r.WhyPlate.resolvedStyle.display != DisplayStyle.None && r.Why.text.Length > 0 ? ":why=" + r.Why.text : "")}");
            }
            foreach (var id in reticleGone) { var r = reticles[id]; r.Root.RemoveFromHierarchy(); r.Edge.RemoveFromHierarchy(); manifest.RemoveAll(x => x.e == r.Root || x.e == r.Edge || x.e == r.WhyPlate); reticles.Remove(id); } // Glyph N2: a released reticle leaves the manifest too (it listed a reticle the frame did not draw)
            ReticlesShown = string.Join(" ", bits);
        }

        /// <summary>Brief 101 step 6 (Glyph's S mock, r2_base.crosshair): a crosshair ring and four ticks, the book's page cream on a 2.2 px ink keyline.</summary>
        static void KeyedCross(Painter2D p, Vector2 c, float r)
        {
            p.lineCap = LineCap.Round;
            for (int pass = 0; pass < 2; pass++)
            {
                p.strokeColor = pass == 0 ? BkInk : StPage; p.lineWidth = pass == 0 ? 4.6f : 2.4f;
                p.BeginPath(); p.Arc(c, r, Deg(0), Deg(360)); p.Stroke();
                p.BeginPath(); foreach (var d in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right }) { p.MoveTo(c + d * (r - 3.5f)); p.LineTo(c + d * (r + 5.5f)); } p.Stroke();
            }
        }

        /// <summary>The top (panel y) of the reticle over `id` now, +inf when none: the verb pill's tail stands 6 px above it.</summary>
        float ReticleTop(string id) => id != null && reticles.TryGetValue(id, out var r) && r.OnScreen && r.Gone < 0f ? r.At.y - ReticleD - 6f : float.PositiveInfinity;
    }
}
