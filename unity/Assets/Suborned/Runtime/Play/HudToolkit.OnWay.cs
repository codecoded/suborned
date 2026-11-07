using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 100, part 2, the report on its way (Law L4: news travels on foot; Fable's `why.onWay`; the look is Glyph's to settle): over whoever carries a
    /// report still to land, a small plate as the seen count's (the kit's plate_dark, 30 px): the way's icon (radio, walk, run, look, tape, dead screen, a robot's
    /// call, a tell) and the seconds to its landing, so it reads in the street without a sentence. While the cell can still stop it (Fable's `fix`) the plate wears
    /// the HUD's act ring (turquoise-light, 1.5 px); a tape or a dead screen (`nofix`) none. Hovering a plate (or the head under it) shows Fable's plate ("Going to the Office door · 8 s")
    /// over its fix line ("Stop them before it lands") in the kit's plate. The plates take no clicks, so the reporter under them stays clickable. Told and learned:
    /// a camera's tape is marked only for a camera the cell has learned (the host decides: HostWhy.Reports); this only draws.</summary>
    public sealed partial class HudToolkit
    {
        readonly List<(VisualElement root, VisualElement icon, Label secs)> wayMarks = new();
        VisualElement wayTip; Label wayTipPlate, wayTipFix;
        /// <summary>The proof reads it: the marks drawn this frame ("walk:8:fix radio:5:fix camera:12:nofix"), and the tip shown ("plate | fix"), "" when none.</summary>
        public static string OnWayShown { get; private set; } = "";
        public static string OnWayTip { get; private set; } = "";

        void BuildOnWay()
        {
            wayTip = PlateDark(marks, float.NaN, 0.43f); wayTip.style.position = Position.Absolute; wayTip.style.flexDirection = FlexDirection.Column; wayTip.style.alignItems = Align.FlexStart;
            wayTip.style.paddingLeft = 12; wayTip.style.paddingRight = 12; wayTip.style.paddingTop = 6; wayTip.style.paddingBottom = 7;
            wayTipPlate = Words(wayTip, 13.5f); wayTipFix = Words(wayTip, 12.5f, Dim); wayTipFix.style.unityFontStyleAndWeight = FontStyle.Normal; wayTipFix.style.marginTop = 2;
            Show(wayTip, false); // a hover read, as the pill's Next tip: not in the manifest (a capture rests no pointer on a reporter)
        }

        (VisualElement root, VisualElement icon, Label secs) WayMark(int i)
        {
            while (wayMarks.Count <= i)
            {
                var r = PlateDark(marks, 30f, 0.3f); r.style.position = Position.Absolute; r.style.paddingLeft = 9; r.style.paddingRight = 10; Round(r, 15f); // the seen count's plate: a 14 px glyph 4 px clear of the rim
                var ic = E(r, null, w: 14, h: 14); ic.style.flexShrink = 0;
                var t = Words(r, 13.5f); t.style.marginLeft = 5;
                int k = wayMarks.Count; wayMarks.Add((r, ic, t)); manifest.Add((r, "marker", () => k < wayMarks.Count ? wayMarks[k].secs.text : ""));
            }
            return wayMarks[i];
        }

        void OnWayFrame(bool on)
        {
            int used = 0; var shown = new List<string>(); string tip = "", tipFix = ""; Vector2 tipAt = default;
            var g = host.Game;
            if (on && g != null && host.Text != null && host.Text.HasWhy && marks != null)
            {
                var mouse = root.panel != null ? RuntimePanelUtils.ScreenToPanel(root.panel, m.Mouse) : new Vector2(-1, -1); var placed = new List<Rect>(); // reporters at one spot (clerks setting off together): their plates stack, none hidden
                foreach (var w in host.OnWay)
                {
                    Vector3? world = null;
                    if (w.View.Length > 0 && host.Views?.Find(w.View) is Transform t && t && t.gameObject.activeInHierarchy) world = t.position + Vector3.up * (w.View.StartsWith("camera/") ? 1.2f : 2.6f);
                    else if (w.At is Suborned.Sim.Pt p) world = host.World(p) + Vector3.up * 2.4f;
                    if (world == null || !Proj(world.Value, out var q)) continue;
                    var u = WayMark(used++); Show(u.root, true);
                    int s = host.OnWaySeconds(w); Text(u.secs, s >= 0 ? $"{s} s" : ""); Show(u.secs, s >= 0);
                    Icon(u.icon, HandoverHost.OnWayIcon(w.Way), Cream); Border(u.root, w.Fix ? 1.5f : 0f, TurqLight);
                    float wd = u.root.resolvedStyle.width; if (float.IsNaN(wd) || wd < 1f) wd = 52f;
                    float left = q.x - wd / 2f, top = q.y - 30f;
                    for (int n = 0, hit = 1; hit > 0 && n < 8; n++) { hit = 0; foreach (var pr in placed) if (Mathf.Abs(pr.x - left) < Mathf.Max(pr.width, wd) && Mathf.Abs(pr.y - top) < 30f) { top = pr.y - 32f; hit = 1; } }
                    placed.Add(new Rect(left, top, wd, 30f)); u.root.style.left = left; u.root.style.top = top;
                    shown.Add($"{w.Way}:{(s >= 0 ? s.ToString() : "-")}:{(w.Fix ? "fix" : "nofix")}");
                    bool over = mouse.x >= q.x - Mathf.Max(wd / 2f, 22f) && mouse.x <= q.x + Mathf.Max(wd / 2f, 22f) && mouse.y >= top && mouse.y <= q.y + 46f; // the plate, and the head under it
                    if (tip.Length == 0 && over) { var (pl, fx, _) = host.OnWayWords(w); tip = pl; tipFix = fx; tipAt = new Vector2(q.x, top - 4f); }
                }
            }
            for (int i = used; i < wayMarks.Count; i++) Show(wayMarks[i].root, false);
            bool tipOn = tip.Length > 0; if (wayTip != null) Show(wayTip, tipOn);
            if (tipOn) { Text(wayTipPlate, tip); Text(wayTipFix, tipFix); Show(wayTipFix, tipFix.Length > 0); float tw = wayTip.resolvedStyle.width; if (float.IsNaN(tw) || tw < 1f) tw = 220f; float th = wayTip.resolvedStyle.height; if (float.IsNaN(th) || th < 1f) th = 46f; wayTip.style.left = tipAt.x - tw / 2f; wayTip.style.top = tipAt.y - th; }
            OnWayShown = string.Join(" ", shown); OnWayTip = tipOn ? $"{tip} | {tipFix}" : "";
        }
    }
}
