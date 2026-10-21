using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    /// <summary>Brief 031 in the kit HUD (Glyph's cue board briefs/031/cues.html; Sky 2026-10-21: option A):
    ///  · the Watch ring (§6): the agent's reach on the ground while a Watch is picked or about to be given (the knob, the hover, a panel or
    ///    ring row): cell turquoise, 3 px over a dark under-stroke so it reads on cream stone and the quay, the agent's cameo on its east rim;
    ///    no digits in play;
    ///  · a person watcher's mark (§8): wary the narrowed eye, reporting the radio, one of the two icons over a person;
    ///  · the close-up callout (§9, option A): a 72 px medallion 70 px above the watcher with a stem to its head, the watcher's own face
    ///    rendered close (HostViews.InsetFrame), its ring repeating the stage, the agent's cameo and 1-3 ticks on its badge, "+1" when more wait;
    ///    placed by a fixed list of spots clear of the watcher, the agents, the line between them, the other callouts and the HUD; docked to
    ///    the screen edge with a chevron when the watcher is off screen or nothing fits; a click centres on the watcher, never an order.
    /// It reads the host only (HandoverHost.Callouts, Cue), so replays are untouched.</summary>
    public sealed partial class HudToolkit
    {
        // ---------------------------------------------------------------- the Watch ring

        /// <summary>Who a Watch is about to be given by, if one is (the picked knob, the hovered target's quick act, a panel or ring row).</summary>
        int WatchRingAgent()
        {
            var g = host.Game; if (g == null || m.Ended || m.BriefingOpen) return -1;
            if (host.Picked >= 0) { var plate = host.PlateOf(host.Agent); if (host.Picked < plate.Length && plate[host.Picked].Id == "watch" && host.Selected[(int)host.Agent]) return (int)host.Agent; }
            if (host.Panel != null && host.PanelHover >= 0 && host.PanelHover < host.Panel.Rows.Count && host.Panel.Rows[host.PanelHover].I?.Order?.Do == "watch") return (int)host.Panel.Rows[host.PanelHover].By;
            if (host.Ring is { } rs && rs.Hover >= 0 && rs.Hover < rs.Segs.Count && rs.Segs[rs.Hover].Row?.I?.Order?.Do == "watch") return (int)rs.Segs[rs.Hover].Row.By;
            if (host.HoverIntent?.Order is { Do: "watch" } ho && host.HoverIntent.Actionable) return (int)Op.Parse(ho.Op);
            return -1;
        }

        void WatchRing()
        {
            int k = WatchRingAgent(); if (k < 0) return;
            var g = host.Game; var op = g.Ops[k]; if (op.Inside || op.Down || op.InCar) return;
            var tr = host.Views?.Find("op/" + Op.Name((OpId)k)); if (!tr || !tr.gameObject.activeSelf) return;
            float r = g.R.WatchReachCm((OpId)k) / 100f; var c = tr.position; c.y += 0.05f;
            var pts = new List<Vector2>(73); Vector2 east = default; float ex = float.MinValue;
            for (int i = 0; i <= 72; i++)
            {
                float a = i * Mathf.PI * 2f / 72f; if (!Proj(c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), out var q)) return;
                pts.Add(q); if (q.x > ex) { ex = q.x; east = q; }
            }
            paints.Add(pp =>
            {
                pp.lineJoin = LineJoin.Round;
                for (int pass = 0; pass < 2; pass++)
                {
                    pp.strokeColor = pass == 0 ? new Color(Ink.r, Ink.g, Ink.b, 0.6f) : Turq; pp.lineWidth = pass == 0 ? 5f : 3f;
                    pp.BeginPath(); pp.MoveTo(pts[0]); for (int i = 1; i < pts.Count; i++) pp.LineTo(pts[i]); pp.ClosePath(); pp.Stroke();
                }
            });
            var e = WorldEl(null, 20, 20); e.style.left = east.x - 10; e.style.top = east.y - 10; Round(e, 10); Border(e, 1.4f, Turq);
            Bg(e, $"portraits/cameo_{Op.Name((OpId)k)}_normal", Color.white); Show(e.Q("g"), false);
        }

        // ---------------------------------------------------------------- a person watcher's mark

        void PersonCueMarks()
        {
            if (m.Ended || m.BriefingOpen) return;
            foreach (var (w, view, stage, _) in host.Cues())
            {
                if (stage < 2 || !(view == "guard" || view.StartsWith("security/") || (view.StartsWith("unit/") && !view.Contains("robot")))) continue;
                if (host.Views?.Find(view) is not Transform t || !t.gameObject.activeSelf || !Proj(t.position + Vector3.up * 2.35f, out var q) || !InView(q)) continue;
                var e = WorldEl("s-disc_dark_24", 22, 22); e.style.left = q.x + 22 - 11; e.style.top = q.y - 11; Icon(e.Q("g"), stage >= 3 ? "radio" : "sawyou", stage >= 3 && Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f ? Red : Cream);
            }
        }

        // ---------------------------------------------------------------- the callouts

        sealed class Medal { public VisualElement Root, Disc, Img, Badge, Plus; public Label PlusText; public Paint Ring; public HandoverHost.Callout For; public Vector2 At; public bool Placed, Docked; public int Spot = -1; public Vector2 Toward; public int Stage; }
        readonly Medal[] medals = new Medal[4];
        const float MedalD = 72f, DockD = 56f, MedalLift = 70f, ScreenPad = 12f;

        Medal NewMedal(int k)
        {
            var md = new Medal();
            md.Root = E(marks, null, 0, 0, MedalD, MedalD, pick: true); md.Root.name = "callout" + k;
            md.Root.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && md.For != null) { host.CalloutClick(md.For); e.StopPropagation(); } });
            md.Disc = E(md.Root, null, 0, 0, MedalD, MedalD); Round(md.Disc, MedalD / 2); md.Disc.style.overflow = Overflow.Hidden; md.Disc.style.backgroundColor = Cream;
            md.Img = E(md.Disc, null, 0, 0, MedalD, MedalD);
            md.Ring = P(md.Root, -8, -8, MedalD + 16, MedalD + 16, (p, r) => MedalRing(p, md));
            md.Badge = E(md.Root, null, MedalD - 24, MedalD - 22, 24, 24); Round(md.Badge, 12); Border(md.Badge, 1.4f, Cream);
            md.Plus = E(md.Root, "s-plate_dark", MedalD - 14, -8, 26, 16); md.PlusText = L(md.Plus, "chip-value", 0, 0, 26, 16); md.PlusText.style.fontSize = 10; md.PlusText.style.unityTextAlign = TextAnchor.MiddleCenter; md.PlusText.style.color = Cream;
            manifest.Add((md.Root, "image", () => "attention callout"));
            Show(md.Root, false); return md;
        }

        /// <summary>The medallion's ring (board §9: thin ring noticed, double ring that breathes wary, dashed ring flashing reporting), the
        /// badge's 1-3 ticks, and the dock's chevron toward the watcher.</summary>
        void MedalRing(Painter2D p, Medal md)
        {
            float d = md.Docked ? DockD : MedalD, R = d / 2f; var c = new Vector2(8 + MedalD / 2f, 8 + MedalD / 2f); float t = Time.unscaledTime;
            p.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.7f); p.lineWidth = 4f; p.BeginPath(); p.Arc(c, R, Deg(0), Deg(360)); p.Stroke();
            switch (md.Stage)
            {
                case 1: p.strokeColor = Cream; p.lineWidth = 2f; p.BeginPath(); p.Arc(c, R, Deg(0), Deg(360)); p.Stroke(); break;
                case 2:
                    p.strokeColor = Cream; p.lineWidth = 2f; p.BeginPath(); p.Arc(c, R, Deg(0), Deg(360)); p.Stroke();
                    p.strokeColor = new Color(Cream.r, Cream.g, Cream.b, 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 1.6f))); p.BeginPath(); p.Arc(c, R + 4f, Deg(0), Deg(360)); p.Stroke(); break;
                case 3: if (Mathf.Repeat(t * 4f, 1f) < 0.6f) Dashed(p, c, R + 1f, Red, 3f, 16); break;
            }
            // the ticks over the badge: 1 noticed, 2 wary, 3 reporting
            var b = new Vector2(8 + MedalD - 12, 8 + MedalD - 10);
            for (int i = 0; i < md.Stage; i++) { p.fillColor = md.Stage == 3 ? Red : Cream; p.BeginPath(); p.Arc(b + new Vector2(-14 + i * 6, -16), 2.2f, Deg(0), Deg(360)); p.Fill(); }
            if (md.Docked && md.Toward.sqrMagnitude > 0.01f)
            {   // the chevron on the edge side, pointing at the watcher
                var dir = md.Toward.normalized; var tip = c + dir * (R + 12f); var side = new Vector2(-dir.y, dir.x) * 6f; var bk = c + dir * (R + 5f);
                p.strokeColor = Cream; p.lineWidth = 2.6f; p.lineCap = LineCap.Round; p.BeginPath(); p.MoveTo(bk + side); p.LineTo(tip); p.LineTo(bk - side); p.Stroke();
            }
        }

        /// <summary>The keep-outs for a callout: HUD pieces on show (their panel rects), inflated by the screen margin.</summary>
        List<Rect> HudRects()
        {
            var l = new List<Rect>();
            foreach (var e in new[] { column, verbPlate, pill, intel, keys, respPill, nextTip, scope, sheet, chip2, goal, history, armedPlate, feed, ticker, hint })
            {
                if (e == null || e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.opacity < 0.05f) continue;
                var r = e.worldBound; if (r.width <= 1 || r.height <= 1 || float.IsNaN(r.width)) continue;
                l.Add(new Rect(r.x - ScreenPad, r.y - ScreenPad, r.width + 2 * ScreenPad, r.height + 2 * ScreenPad));
            }
            return l;
        }

        /// <summary>Board §9 A's spots in their fixed order: up, up-away, up-toward, level-away, level-toward, then the same a row higher (the
        /// first three). `away` is the side the agent is not on.</summary>
        public static Vector2 CalloutSpot(Vector2 head, int i, float away)
        {
            float up = MedalLift, side = 62f, level = 84f;
            return i switch
            {
                0 => head + new Vector2(0, -up), 1 => head + new Vector2(away * side, -up), 2 => head + new Vector2(-away * side, -up),
                3 => head + new Vector2(away * level, 0), 4 => head + new Vector2(-away * level, 0),
                5 => head + new Vector2(0, -2 * up), 6 => head + new Vector2(away * side, -2 * up), _ => head + new Vector2(-away * side, -2 * up),
            };
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float t = ab.sqrMagnitude < 1e-4f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return Vector2.Distance(p, a + ab * t); }

        /// <summary>Board §9 A's rules for a centre: inside the screen (12 px), clear of the HUD, the watcher's figure, every agent (22 px),
        /// the line from the watched agent to the watcher (a 36 px capsule) and every callout already placed. Deterministic from its inputs.</summary>
        public static bool CalloutFits(Vector2 c, Vector2 size, List<Rect> hud, Rect watcher, List<Rect> agents, Vector2 lineA, Vector2 lineB, List<Vector2> claimed)
        {
            float R = MedalD / 2f;
            if (c.x - R < ScreenPad || c.y - R < ScreenPad || c.x + R > size.x - ScreenPad || c.y + R > size.y - ScreenPad) return false;
            bool Hits(Rect r) { float dx = Mathf.Max(r.xMin - c.x, 0f, c.x - r.xMax), dy = Mathf.Max(r.yMin - c.y, 0f, c.y - r.yMax); return dx * dx + dy * dy < R * R; }
            foreach (var r in hud) if (Hits(r)) return false;
            if (Hits(watcher)) return false;
            foreach (var r in agents) if (Hits(new Rect(r.x - 22, r.y - 22, r.width + 44, r.height + 44))) return false;
            if (SegDist(c, lineA, lineB) < R + 18f) return false;
            foreach (var o in claimed) if (Vector2.Distance(o, c) < MedalD + 6f) return false;
            return true;
        }

        /// <summary>A figure's box on screen from its feet to its head (null when off screen).</summary>
        Rect? FigureBox(Vector3 feet, float tall, float halfW = 14f)
        {
            if (!Proj(feet, out var a) || !Proj(feet + Vector3.up * tall, out var b)) return null;
            return new Rect(Mathf.Min(a.x, b.x) - halfW, Mathf.Min(a.y, b.y), Mathf.Abs(a.x - b.x) + 2 * halfW, Mathf.Abs(a.y - b.y));
        }

        float WatcherTall(string view) => view.StartsWith("camera/") ? 3.4f : view == "patrol" || view.Contains("robot") ? 1.45f : 1.85f;

        void Callouts(float W, float H)
        {
            var list = new List<HandoverHost.Callout>(host.Callouts);
            bool live = !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None && host.Game != null && !host.Following;
            list.Sort((a, b) => a.Stage != b.Stage ? b.Stage.CompareTo(a.Stage) : b.Seq.CompareTo(a.Seq)); // the highest stage claims first, then the newest
            var size = new Vector2(W, H); var hud = live ? HudRects() : null; var claimed = new List<Vector2>();
            var agents = new List<Rect>(); var agentAt = new Vector2?[4];
            if (live) for (int k = 0; k < 4; k++) if (host.Views?.Find("op/" + Op.Name((OpId)k)) is Transform at && at.gameObject.activeSelf && FigureBox(at.position, 1.85f) is Rect ar) { agents.Add(ar); agentAt[k] = ar.center; }
            float now = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f); int floating = 0;
            for (int i = 0; i < medals.Length; i++)
            {
                var c = live && i < list.Count ? list[i] : null;
                medals[i] ??= NewMedal(i); var md = medals[i];
                var rt = host.Views?.InsetFrame(i, c?.View);
                if (c == null || rt == null) { Show(md.Root, false); md.For = null; md.Placed = false; continue; }
                if (md.For != c) { md.For = c; md.Placed = false; md.Spot = -1; }
                md.Stage = c.Stage;
                var wt = host.Views.Find(c.View); var feet = wt.position; if (c.View.StartsWith("camera/")) feet.y -= 1.6f; // the pole's view sits at its middle
                float tall = WatcherTall(c.View);
                bool seen = Proj(feet + Vector3.up * (tall + 0.15f), out var head) && InView(head) && host.View && host.View.WorldToViewportPoint(feet).z > 0;
                var wbox = seen ? FigureBox(feet, tall, c.View.StartsWith("camera/") ? 10f : 14f) ?? new Rect(head, Vector2.zero) : new Rect();
                int ak = c.Op.Length > 0 ? (int)Op.Parse(c.Op) : -1; Vector2 agentMid = ak >= 0 && agentAt[ak].HasValue ? agentAt[ak].Value : head;
                float away = agentMid.x <= head.x ? 1f : -1f;
                Vector2 want = default; bool dock = !seen || floating >= 3;
                if (!dock)
                {
                    var others = new List<Rect>(agents);
                    bool Fits(int s, out Vector2 at) { at = CalloutSpot(head, s, away); return CalloutFits(at, size, hud, wbox, others, agentMid, wbox.center, claimed); }
                    if (md.Spot >= 0 && Fits(md.Spot, out var keep)) want = keep; // only re-chosen when its spot is blocked
                    else { md.Spot = -1; for (int s = 0; s < 8; s++) if (Fits(s, out var at)) { md.Spot = s; want = at; break; } }
                    if (md.Spot < 0) dock = true;
                }
                md.Docked = dock;
                if (dock)
                {   // the edge nearest the watcher: from the screen's centre toward it, inset 12 px, clear of the roster column
                    var sc = new Vector2(W / 2f, H / 2f); Vector2 dir;
                    var vp = host.View ? host.View.WorldToViewportPoint(feet + Vector3.up * tall) : new Vector3(0.5f, 0.5f, 1f);
                    dir = new Vector2(vp.x - 0.5f, 0.5f - vp.y); if (vp.z < 0) dir = -dir; if (dir.sqrMagnitude < 1e-4f) dir = Vector2.up;
                    float R = DockD / 2f + ScreenPad, kx = Mathf.Abs(dir.x) < 1e-4f ? float.MaxValue : (W / 2f - R) / Mathf.Abs(dir.x), ky = Mathf.Abs(dir.y) < 1e-4f ? float.MaxValue : (H / 2f - R) / Mathf.Abs(dir.y);
                    want = sc + dir * Mathf.Min(kx, ky);
                    if (column != null && column.resolvedStyle.display != DisplayStyle.None) { var cb = column.worldBound; if (want.x - DockD / 2f < cb.xMax + ScreenPad && want.y > cb.yMin - DockD / 2f && want.y < cb.yMax + DockD / 2f) want.x = cb.xMax + ScreenPad + DockD / 2f; }
                    md.Toward = (seen ? head : sc + dir * 4000f) - want;
                }
                else floating++;
                md.At = md.Placed ? Vector2.Lerp(md.At, want, 1f - Mathf.Exp(-dt / 0.1f)) : want; md.Placed = true; // eased over 0.1 s
                claimed.Add(md.At);
                // the medallion
                Show(md.Root, true); md.Root.BringToFront();
                float d = dock ? DockD : MedalD; md.Root.style.scale = new Scale(new Vector3(d / MedalD, d / MedalD, 1f));
                md.Root.style.left = md.At.x - MedalD / 2f; md.Root.style.top = md.At.y - MedalD / 2f;
                md.Root.style.opacity = Mathf.Clamp01((now - c.Born) / 0.15f);
                if (md.Img.style.backgroundImage.value.renderTexture != rt) md.Img.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
                if (ak >= 0) Bg(md.Badge, $"portraits/cameo_{c.Op}_normal", Color.white);
                Border(md.Badge, 1.4f, c.Stage == 3 ? Red : Cream);
                int plus = host.QueuedBehind(c); Show(md.Plus, plus > 0); if (plus > 0) Text(md.PlusText, $"+{plus}");
                md.Ring.MarkDirtyRepaint();
                // the stem: a 14 px notch on the rim, aimed at the head, never longer than 48 px
                if (!dock)
                {
                    var at0 = md.At; var hv = head - at0; float dist = hv.magnitude;
                    if (dist > MedalD / 2f + 4f)
                    {
                        var dn = hv / dist; float len = Mathf.Min(48f, Mathf.Max(14f, dist - MedalD / 2f - 6f));
                        var b0 = at0 + dn * (MedalD / 2f - 1f); var tip = at0 + dn * (MedalD / 2f + len); var sd = new Vector2(-dn.y, dn.x) * 6f; var col = c.Stage == 3 ? Red : Cream;
                        paints.Add(pp => { pp.fillColor = new Color(Ink.r, Ink.g, Ink.b, 0.6f); pp.BeginPath(); pp.MoveTo(b0 + sd * 1.3f); pp.LineTo(tip + (tip - b0).normalized * 1.5f); pp.LineTo(b0 - sd * 1.3f); pp.ClosePath(); pp.Fill(); pp.fillColor = col; pp.BeginPath(); pp.MoveTo(b0 + sd); pp.LineTo(tip); pp.LineTo(b0 - sd); pp.ClosePath(); pp.Fill(); });
                    }
                }
            }
        }

        /// <summary>Per frame, after the street marks: the Watch ring, the person marks, the callouts.</summary>
        void AttentionHud(float W, float H)
        {
            if (host.Game == null) return;
            WatchRing();
            PersonCueMarks();
            Callouts(W, H);
        }
    }
}
