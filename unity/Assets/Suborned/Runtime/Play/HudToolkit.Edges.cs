using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    /// <summary>The edge marks (moved here from HudToolkit.Less by brief 101 step 6): Popovic's, the units arriving, the cell's faces and the key people's pins at the
    /// screen's edge, so a turn or a pan never loses them. Brief 101 step 6 (Glyph's S mocks, briefs/101/glyph/r2/S-busy.png, edge_face): each is a face (or a badge) in
    /// the street's cream ring over a soft shadow, an ink chevron just past it pointing out; no dark disc, chrome or coloured ring (a selected agent's face is ringed
    /// turquoise, the roster's own rule; target mode's pins keep the live accent).</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>The edge face's size (34 px: the mock's 38 less its shadow's room) and each one's count label.</summary>
        const float EdgeFaceD = 34f; readonly Dictionary<VisualElement, KeyText> edgeCounts = new();

        // ---------------------------------------------------------------- edge pointers (Glyph §5): Vale, and units arriving; brief 016 (Glyph
        // review §4): edge cameos for the cell and edge pins for key people, so a turn or a pan never loses them

        /// <summary>Brief 016's edge cameos: a 24 px cameo of the agent with its F key and a cream wedge at it; turquoise rim when selected.</summary>
        sealed class EdgeCam { public VisualElement Root, Key; public Label KeyText; public int Agent = -1; public string Role; public HandoverHost.Pickable Target; }
        readonly List<EdgeCam> edgeCams = new();
        /// <summary>The timeline marks that hang on a known person (the pill's next mark): who to pin at the edge in the 10 s before.</summary>
        static string MarkPerson(string label) { var l = label.ToLowerInvariant(); return l.Contains("chime") ? "guard" : l.Contains("driver") ? "driver" : ""; }

        void Edges(Camera cam, float W, float H)
        {
            int used = 0, camUsed = 0;
            var placed = new List<Rect>();
            // the HUD groups an edge mark slides clear of (panel space), with 6 px air
            var blocks = new List<Rect>();
            foreach (var e in new[] { column, verbRow, pill, keys, scope, historyKey2, goal, respPill, intel, chip2, sheet, crCard, squadBtn, freeBtn, freePlate, clipBlock })
                if (e != null && e.resolvedStyle.display != DisplayStyle.None && e.worldBound.width > 0) { var b = e.worldBound; float air = e == goal ? 12 : e == pill ? 14 : e == crCard || e == historyKey2 ? 22 : 6; blocks.Add(Rect.MinMaxRect(b.xMin - air, b.yMin - air, b.xMax + air, b.yMax + air)); } // Glyph 016 r2 G: 12 px clear of the goal line; techwatch fix 1: 14 px clear of the status pill, the HUD groups' margin; Glyph r5 #6: 22 px clear of the news and the Book (a face's chevron reaches 30 px from its centre)
            // Glyph N7 (0.2.125): the left column's band is no place for an edge mark (the Squad and weapons buttons stand under the roster): edge marks keep x >= the roster's right + 12
            if (column != null && column.resolvedStyle.display != DisplayStyle.None && column.worldBound.width > 0) blocks.Add(Rect.MinMaxRect(-100f, -100f, column.worldBound.xMax + 12f, H + 100f));
            // Glyph techwatch r2 A: the street's own marks win over the edge marks, which slide along the edge clear of them (4 px air): the people's
            // pips, the door, car and held markers and the door's next-step chip, and the other small world marks (OtherMarks)
            void WorldBlock(VisualElement e, float w, float h) { if (e == null || e.style.display.value == DisplayStyle.None || e.parent == null) return; float x = e.style.left.value.value, y = e.style.top.value.value; blocks.Add(new Rect(x - 4, y - 4, w + 8, h + 8)); }
            foreach (var (pr, _, _) in pipEls) WorldBlock(pr, 26, 26);
            WorldBlock(doorMark, 26, 26); WorldBlock(carMark, 26, 26); WorldBlock(heldMark, 26, 26); WorldBlock(doorNext, 52, 26);
            foreach (var om in OtherMarks()) blocks.Add(new Rect(om.x - 4, om.y - 4, om.width + 8, om.height + 8));
            foreach (var br in bubbleRects) blocks.Add(new Rect(br.x - 10, br.y - 10, br.width + 20, br.height + 20)); // Glyph r5 #5: speech is the street's too (0.2.293: a line held on screen covered the right-edge face); round 4 must 5: 10 px off every bubble, the docked line included
            const float M = 26; // the marks' centres run round this inset rectangle
            float pw = W - 2 * M, ph = H - 2 * M, per = 2 * (pw + ph);
            Vector2 Perim(float d) { d = Mathf.Repeat(d, per); if (d < pw) return new Vector2(M + d, M); d -= pw; if (d < ph) return new Vector2(W - M, M + d); d -= ph; if (d < pw) return new Vector2(W - M - d, H - M); d -= pw; return new Vector2(M, H - M - d); }
            float PerimOf(Vector2 a) { if (Mathf.Abs(a.y - M) < 0.5f) return a.x - M; if (Mathf.Abs(a.x - (W - M)) < 0.5f) return pw + (a.y - M); if (Mathf.Abs(a.y - (H - M)) < 0.5f) return pw + ph + (W - M - a.x); return pw + ph + pw + (H - M - a.y); }
            bool Free(Vector2 c, float half) { var r = new Rect(c.x - half, c.y - half, 2 * half, 2 * half); foreach (var b in blocks) if (b.Overlaps(r)) return false; foreach (var q in placed) if (q.Overlaps(r)) return false; return true; }
            // where a world point off the frame meets the edge, slid along it until clear of the HUD and the other marks; null when on screen
            (Vector2 at, Vector2 dir)? Spot(Vector3 w, float half)
            {
                if (!cam) return null; var sp = cam.WorldToScreenPoint(w);
                var vp = cam.WorldToViewportPoint(w); if (vp.z > 0 && vp.x > 0.03f && vp.x < 0.97f && vp.y > 0.03f && vp.y < 0.97f) return null;
                var centre = new Vector2(W / 2, H / 2);
                var pp = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(sp.x, Screen.height - sp.y)); if (sp.z < 0) pp = centre - (pp - centre);
                var dir = pp - centre; if (dir.sqrMagnitude < 1) return null; dir.Normalize();
                float k = Mathf.Min(dir.x > 0 ? (W - M - centre.x) / dir.x : dir.x < 0 ? (M - centre.x) / dir.x : 1e9f, dir.y > 0 ? (H - M - centre.y) / dir.y : dir.y < 0 ? (M - centre.y) / dir.y : 1e9f);
                float d0 = PerimOf(centre + dir * k);
                for (float step = 0; step < per / 2; step += 4)
                    foreach (var d in step == 0 ? new[] { d0 } : new[] { d0 + step, d0 - step })
                    {
                        var a = Perim(d); if (!Free(a, half)) continue;
                        placed.Add(new Rect(a.x - half - 3, a.y - half - 3, 2 * half + 6, 2 * half + 6));
                        var to = pp - a; return (a, to.sqrMagnitude > 1 ? to.normalized : dir);
                    }
                return null;
            }
            void Pointer(Vector3 w, string face, string count)
            {
                if (Spot(w, 19) is not { } spw) return; var (at, dir) = spw;
                // brief 101 step 6 (Glyph's S mock, edge_face): the face (or a badge) 34 px in the street's cream ring over a soft shadow, an ink chevron pointing out
                while (edgeEls.Count <= used)
                {
                    var e = E(marks, null, 0, 0, EdgeFaceD, EdgeFaceD); e.style.overflow = Overflow.Visible; var sh = Disc(e, EdgeFaceD, "e-shadow"); Pin(sh, new Vector2(0f, 2.5f)); sh.style.opacity = 0.85f;
                    var f = E(e, "e-page e-inline", 0, 0, EdgeFaceD, EdgeFaceD); Round(f, EdgeFaceD / 2f); BorderW(f, 2.5f); f.style.overflow = Overflow.Hidden; E(f, null, 0, 0, EdgeFaceD - 5f, EdgeFaceD - 5f).name = "b";
                    var n = KText(e, 14f); n.Root.name = "n"; n.Root.style.position = Position.Absolute; n.Root.style.left = EdgeFaceD - 4f; n.Root.style.top = -9f; edgeCounts[e] = n; edgeEls.Add(e);
                }
                var el = edgeEls[used++]; Show(el, true); el.style.left = at.x - EdgeFaceD / 2f; el.style.top = at.y - EdgeFaceD / 2f;
                Bg(el.Q("b"), face, Color.white); var nl = edgeCounts[el]; Show(nl.Root, count.Length > 0); nl.Set(count);
                Chevron(at, dir, EdgeFaceD / 2f);
            }
            EdgeCam CamEl()
            {
                while (edgeCams.Count <= camUsed)
                {
                    var ec = new EdgeCam(); ec.Root = E(marks, "e-page", 0, 0, 30, 30, pick: true); Round(ec.Root, 15); BorderW(ec.Root, 2.5f); ec.Root.AddToClassList("e-inline"); // brief 101 step 6: the face in the street's cream ring
                    ec.Key = E(ec.Root, "s-plate_light", 16, -13, float.NaN, 18); ec.Key.style.minWidth = 22; ec.KeyText = L(ec.Key, "keycap"); ec.KeyText.style.fontSize = 10f; ec.KeyText.style.flexGrow = 1; ec.KeyText.style.unityTextAlign = TextAnchor.MiddleCenter; ec.KeyText.style.paddingLeft = 4; ec.KeyText.style.paddingRight = 4; /* Glyph 016 r2-3: 10 px type on the order plate's keycap, half a cameo out */ ec.KeyText.style.paddingLeft = 3; ec.KeyText.style.paddingRight = 3;
                    ec.Root.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0) return; e.StopPropagation(); if (ec.Agent >= 0) host.CentreOn((OpId)ec.Agent); else if (ec.Target != null) host.TargetFind(ec.Target); else if (!string.IsNullOrEmpty(ec.Role)) FindPerson(ec.Role); });
                    manifest.Add((ec.Root, "image", () => "edge cameo")); // art and its F key: no running text
                    edgeCams.Add(ec);
                }
                var x = edgeCams[camUsed++]; Show(x.Root, true); return x;
            }
            // Glyph's 0.2.97 review #7: the pointer is the kit's chrome tail joined to the cameo's ring (a loose cream wedge off the rim read as a stray
            // white corner square): its base sits inside the 2 px chrome ring, an ink-deep edge, the tip 8 px out
            // brief 101 step 6 (Glyph's S mock): an ink chevron just past the face's ring, pointing at where it is (a target mode pin keeps its live colour)
            void Chevron(Vector2 at, Vector2 dir, float r, Color? c = null) { var side = new Vector2(-dir.y, dir.x); var a = at + dir * (r + 3f); var tip = at + dir * (r + 13f); var col = c ?? BkInk; paints.Add(p2 => { p2.fillColor = col; p2.BeginPath(); p2.MoveTo(tip); p2.LineTo(a + side * 8f); p2.LineTo(a - side * 8f); p2.ClosePath(); p2.Fill(); }); }
            void Wedge(Vector2 at, Vector2 dir, Color c) => Chevron(at, dir, 15f, c == Cream ? (Color?)null : c);
            if (!m.Ended && !m.BriefingOpen && host.Game != null)
            {
                var g = host.Game; int budget = 4; // Glyph: at most four at the edge at once (Vale's arrow, the cameos, the pins)
                int before = used; Pointer(host.ValeKnown + Vector3.up * 1f, FaceOf("councillor"), ""); if (used > before) budget--; // Popovic's own face (the mock's)
                // the cell: the selected agent always while off screen; another only when busy, hurt, down or seen (an idle one's portrait says ready)
                var order = new List<int>(); for (int k = 0; k < 4; k++) if (host.Selected[k]) order.Add(k); for (int k = 0; k < 4; k++) if (!host.Selected[k]) order.Add(k);
                foreach (int k in order)
                {
                    if (budget <= 0) break; var op = g.Ops[k]; if (op.Inside) continue;
                    var s = m.Slots[k]; bool sel = host.Selected[k], matters = sel || s.Busy || s.Hurt || s.Down || host.RunEye(k) == "open";
                    if (!matters || host.Views?.Find("op/" + Op.Name((OpId)k)) is not Transform t || !t.gameObject.activeSelf) continue;
                    if (Spot(t.position + Vector3.up, 13) is not { } spa) continue; var (at, dir) = spa;
                    var ec = CamEl(); ec.Agent = k; ec.Role = null; ec.Target = null; ec.Root.style.left = at.x - 15; ec.Root.style.top = at.y - 15;
                    Bg(ec.Root, $"portraits/cameo_{Op.Name((OpId)k)}_normal"); RingColour(ec.Root, sel ? TurqLight : null); /* brief 101 step 6: the face in the street's cream ring, turquoise when selected (the faces' own rule) */ Show(ec.Key, KeyLegends); Text(ec.KeyText, host.AgentKeyLabel(k)); if (ec.Root.Q("pin") is VisualElement pn) Show(pn, false);
                    Wedge(at, dir, Cream); budget--;
                }
                // key people: a pin only in the 10 s before the next plan mark that hangs on them (the goal, Vale, keeps his own arrow)
                var tl = host.Text?.Timeline;
                if (tl != null) foreach (var (atS, label, known) in tl)
                    {
                        var p = atS.Split(':'); int tick = (int.Parse(p[0]) * 60 + int.Parse(p[1])) * Game.Hz; if (tick <= g.Tick) continue;
                        var role = known ? MarkPerson(label) : ""; if (budget > 0 && role.Length > 0 && tick - g.Tick <= 10 * Game.Hz && KnownOf(role) && host.Views?.Find(role) is Transform pt && pt.gameObject.activeSelf && Spot(pt.position + Vector3.up, 13) is { } psp)
                        { var (pa, pd) = psp; var ec = CamEl(); ec.Agent = -1; ec.Role = role; ec.Target = null; ec.Root.style.left = pa.x - 15; ec.Root.style.top = pa.y - 15; ec.Root.style.backgroundImage = StyleKeyword.Null; RingColour(ec.Root, null); Show(ec.Key, false); PinBadge(ec, role); Wedge(pa, pd, Cream); budget--; }
                        break; // the next mark only
                    }
                // target mode: the valid targets off screen get edge pins with the verb's glyph (up to four); a click pans to one and focuses it
                if (host.TargetMode)
                {
                    int tb = 4; var icon = m.VerbIcon[Mathf.Clamp(host.Picked, 0, 4)];
                    foreach (var tp in host.TargetMarks)
                    {
                        if (tb <= 0) break; if (Spot(tp.Anchor, 13) is not { } tsp) continue; var (ta, td) = tsp;
                        var ec = CamEl(); ec.Agent = -1; ec.Role = null; ec.Target = tp; ec.Root.style.left = ta.x - 15; ec.Root.style.top = ta.y - 15; ec.Root.style.backgroundImage = StyleKeyword.Null; RingColour(ec.Root, TurqLight); Show(ec.Key, false);
                        var pin = ec.Root.Q("pin"); if (pin == null) { pin = E(ec.Root, null, 3.5f, 3.5f, 18, 18); pin.name = "pin"; } Show(pin, true); Icon(pin, icon, BkDeep); Wedge(ta, td, BkDeep); tb--;
                    }
                }
                var seen = new List<Vector3>();
                foreach (var a in m.Arrivals) { bool near = false; foreach (var s in seen) if ((s - a.World).sqrMagnitude < 400f) near = true; if (near) continue; seen.Add(a.World); int n = 0; foreach (var b in m.Arrivals) if ((b.World - a.World).sqrMagnitude < 400f) n++; Pointer(a.World, "badges/signal_police", n > 1 ? n.ToString() : ""); }
            }
            for (int i = used; i < edgeEls.Count; i++) Show(edgeEls[i], false);
            for (int i = camUsed; i < edgeCams.Count; i++) Show(edgeCams[i].Root, false);
        }
    }
}
