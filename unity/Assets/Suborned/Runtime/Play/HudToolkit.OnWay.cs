using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 100, part 2, the report on its way (Law L4: news travels on foot; Fable's `why.onWay`), in brief 101's street layer (step 6; Glyph's S mocks,
    /// briefs/101/glyph/r2/S-quiet.png and S-busy.png): a report about to land is a key event, so its time shows. Over whoever carries it, an orange disc with the way's
    /// icon (radio, walk, run, look, tape, dead screen, a robot's call, a tell) and the seconds to its landing beside it in orange street type; at a person's feet, a
    /// ring on the ground (with the world-mesh marks, HudToolkit.Ground) draining over its last 10 s: the page cream on an ink track (the play grade turns an orange ground
    /// mark into the cones' red, and the signal pass outside the grade would draw over legs, so the ring is neutral and the orange stays on the disc and its seconds). While the cell can still stop it (Fable's
    /// `fix`) the disc wears the HUD's act ring (turquoise-light); a tape or a dead screen (`nofix`) an ink one. Hovering the disc (or the head under it) names it in two
    /// street labels ("Going to the Office door · 8 s" over "Stop them before it lands"). The marks take no clicks, so the reporter under them stays clickable. Told and
    /// learned: a camera's tape is marked only for a camera the cell has learned (the host decides: HostWhy.Reports); this only draws. It retires the plate.</summary>
    public sealed partial class HudToolkit
    {
        readonly List<(VisualElement root, VisualElement disc, VisualElement icon, KeyText secs)> wayMarks = new();
        /// <summary>The ring drains over a report's last WayRingS seconds (full before them): what it shows is how near it lands, the same in play and in a capture.</summary>
        const float WayRingS = 10f;
        VisualElement wayTip; KeyText wayTipPlate, wayTipFix;
        /// <summary>The proof reads it: the marks drawn this frame ("walk:8:fix radio:5:fix camera:12:nofix"), and the tip shown ("plate | fix"), "" when none.</summary>
        public static string OnWayShown { get; private set; } = "";
        public static string OnWayTip { get; private set; } = "";
        const float WayDisc = 24f;

        void BuildOnWay()
        {
            wayTip = E(marks); wayTip.style.position = Position.Absolute; wayTip.style.flexDirection = FlexDirection.Column; wayTip.style.alignItems = Align.Center;
            wayTipPlate = KText(wayTip, 14f); wayTipFix = KText(wayTip, 13f, "k-semi"); wayTipFix.Root.style.marginTop = 3;
            Show(wayTip, false); // a hover read, as the clock's Next tip: not in the manifest (a capture rests no pointer on a reporter)
        }

        (VisualElement root, VisualElement disc, VisualElement icon, KeyText secs) WayMark(int i)
        {
            while (wayMarks.Count <= i)
            {
                var r = E(marks); r.style.position = Position.Absolute; r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.height = 28; r.style.overflow = Overflow.Visible;
                var d = E(r, "e-orange", w: WayDisc, h: WayDisc); d.style.flexShrink = 0; Round(d, WayDisc / 2f); Border(d, 1.6f, BkInk); d.style.alignItems = Align.Center; d.style.justifyContent = Justify.Center;
                var ic = E(d, null, w: 14, h: 14); ic.style.flexShrink = 0;
                var t = KText(r, 21f, "k-bold", "k-hot", "k-key-ink", 1.7f); t.Root.style.marginLeft = 5;
                int k = wayMarks.Count; wayMarks.Add((r, d, ic, t)); manifest.Add((r, "marker", () => k < wayMarks.Count ? wayMarks[k].secs.Words : ""));
            }
            return wayMarks[i];
        }

        void OnWayFrame(bool on)
        {
            int used = 0; var shown = new List<string>(); string tip = "", tipFix = ""; Vector2 tipAt = default;
            var g = host.Game;
            if (on && g != null && host.Text != null && host.Text.HasWhy && marks != null)
            {
                var mouse = root.panel != null ? RuntimePanelUtils.ScreenToPanel(root.panel, m.Mouse) : new Vector2(-1, -1); var placed = new List<Rect>(); // reporters at one spot (clerks setting off together): their marks stack, none hidden
                foreach (var w in host.OnWay)
                {
                    Vector3? world = null, feet = null; bool cam = w.View.StartsWith("camera/");
                    if (w.View.Length > 0 && host.Views?.Find(w.View) is Transform t && t && t.gameObject.activeInHierarchy) { world = t.position + Vector3.up * (cam ? 1.2f : 2.6f); if (!cam) feet = t.position; }
                    else if (w.At is Suborned.Sim.Pt p) { feet = host.World(p); world = feet.Value + Vector3.up * 2.4f; }
                    if (world == null || !Proj(world.Value, out var q)) continue;
                    var u = WayMark(used++); Show(u.root, true);
                    int s = host.OnWaySeconds(w); u.secs.Set(s >= 0 ? $"{s} s" : ""); Show(u.secs.Root, s >= 0);
                    Icon(u.icon, HandoverHost.OnWayIcon(w.Way), Cream); RingColour(u.disc, w.Fix ? TurqLight : BkInk);
                    float wd = u.root.resolvedStyle.width; if (float.IsNaN(wd) || wd < 1f) wd = 60f;
                    float left = q.x - WayDisc / 2f, top = q.y - 30f; // the disc over the head, its seconds to the right
                    for (int n = 0, hit = 1; hit > 0 && n < 8; n++) { hit = 0; foreach (var pr in placed) if (Mathf.Abs(pr.x - left) < Mathf.Max(pr.width, wd) && Mathf.Abs(pr.y - top) < 30f) { top = pr.y - 32f; hit = 1; } }
                    placed.Add(new Rect(left, top, wd, 30f)); u.root.style.left = left; u.root.style.top = top;
                    // the ring at the feet, draining over the report's last 10 s
                    if (s >= 0 && feet is Vector3 f) WayRing(f, Mathf.Clamp01(s / WayRingS));
                    shown.Add($"{w.Way}:{(s >= 0 ? s.ToString() : "-")}:{(w.Fix ? "fix" : "nofix")}");
                    bool over = mouse.x >= q.x - Mathf.Max(wd / 2f, 22f) && mouse.x <= q.x + Mathf.Max(wd / 2f, 22f) && mouse.y >= top && mouse.y <= q.y + 46f; // the mark, and the head under it
                    if (tip.Length == 0 && over) { var (pl, fx, _) = host.OnWayWords(w); tip = pl; tipFix = fx; tipAt = new Vector2(q.x, top - 4f); }
                }
            }
            for (int i = used; i < wayMarks.Count; i++) Show(wayMarks[i].root, false);
            bool tipOn = tip.Length > 0; if (wayTip != null) Show(wayTip, tipOn);
            if (tipOn) { wayTipPlate.Set(tip); wayTipFix.Set(tipFix); Show(wayTipFix.Root, tipFix.Length > 0); float tw = wayTip.resolvedStyle.width; if (float.IsNaN(tw) || tw < 1f) tw = 220f; float th = wayTip.resolvedStyle.height; if (float.IsNaN(th) || th < 1f) th = 46f; wayTip.style.left = tipAt.x - tw / 2f; wayTip.style.top = tipAt.y - th; wayTip.BringToFront(); }
            OnWayShown = string.Join(" ", shown); OnWayTip = tipOn ? $"{tip} | {tipFix}" : "";
        }

        /// <summary>The report's ring on the ground at the reporter's feet (the mock's 22 px radius at play zoom, 4.2 px): an ink track, and on it the page cream arc of
        /// what is left, from 12 o'clock clockwise; depth-tested, so legs stand in front of it.</summary>
        void WayRing(Vector3 feet, float left)
        {
            float r = 22f * GroundPxM; feet.y = FloorAt(feet);
            GroundArc(feet, r, new Color(BkInk.r, BkInk.g, BkInk.b, 0.6f), 6.2f, 0f, 360f);
            if (left > 0.01f) GroundArc(feet, r, StPage, 4.2f, 0f, 360f * Mathf.Clamp01(left));
        }
    }
}
