using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;
using Target = Suborned.Play.HandoverHost.Target;

namespace Suborned.Play
{
    /// <summary>Brief 041, the hold preset in the kit HUD (testbed/controls markers.js 'pill', status.js, ui.js, app.js drawGhosts, attention):
    ///  · the verb pill, the only hover marker: the act's icon and colour, "Hold · Watch the guard · 26 s · …", the doer's face wherever an agent
    ///    would be named, a coral "seen there" when the stand spot is in a known cone; a charge fills it ("Hold to watch the guard"); a right hold
    ///    says "Hold to fire · bystander · down in 5 s"; live words replace the offer (HandoverHost.FireStatus, OrderStatus);
    ///  · the Watch's reach ring round the target; each agent's destination ring in its colour (a run: dashed and doubled); the paused plan's
    ///    ghosts (a dashed path, a dashed ring, a pill with the face); an agent standing in a cone, its attention clock; the portrait drag's band;
    ///    the Squad button under the roster. It reads the host only: replays are untouched.</summary>
    public sealed partial class HudToolkit
    {
        static Color Hex(int rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
        /// <summary>The cell's colours in the playground (data.js ROLES): Scout, Fixer, Tech, Enforcer.</summary>
        public static readonly Color[] RoleColours = { Hex(0x8fd3c8), Hex(0xf2b48a), Hex(0x9db7ff), Hex(0xe6dfcc) };
        static readonly Color Coral = Hex(0xff7a5c), LookBlue = Hex(0x4aa8ff), HarmPink = Hex(0xff4d7d);

        VisualElement vpillFill, vpillFaces; Label vpillSeen; readonly List<VisualElement> faceEls = new();
        string holdKey = "";

        void BuildHoldPill()
        {
            vpill.style.overflow = Overflow.Hidden;
            vpillFill = E(vpill, null, 0, 0, 0, 30); vpillFill.style.backgroundColor = new Color(1f, 1f, 1f, 0.55f); vpillFill.SendToBack();
            vpillFaces = E(vpill); vpillFaces.style.flexDirection = FlexDirection.Row; vpillFaces.style.marginLeft = 8; vpillFaces.style.flexShrink = 0;
            for (int i = 0; i < 4; i++) { var f = E(vpillFaces, null, w: 24, h: 24); Round(f, 12); f.style.flexShrink = 0; f.style.marginLeft = i == 0 ? 0 : -7; faceEls.Add(f); }
            vpillSeen = L(vpill, "chip-value"); vpillSeen.text = "seen there"; vpillSeen.style.backgroundColor = Coral; vpillSeen.style.color = PillInk; vpillSeen.style.fontSize = 13; vpillSeen.style.marginLeft = 8;
            vpillSeen.style.paddingLeft = 8; vpillSeen.style.paddingRight = 8; Round(vpillSeen, 10); Border(vpillSeen, 2f, Color.white);
        }

        void Faces(IReadOnlyList<int> agents)
        {
            int n = agents?.Count ?? 0; Show(vpillFaces, n > 0);
            for (int i = 0; i < 4; i++)
            {
                bool on = i < n; Show(faceEls[i], on); if (!on) continue; int k = agents[i];
                Bg(faceEls[i], $"portraits/cameo_{Op.Name((OpId)k)}_normal", Color.white); Border(faceEls[i], 2.5f, RoleColours[k]);
            }
        }

        static string LowerFirst(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];

        /// <summary>The proof reads it: the pill's words now ("" hidden) are PillWords; and what it is about.</summary>
        public static string PillState { get; private set; } = "";

        /// <summary>Per frame (the hold preset): what the pill says and where. A charge first, then the live words (fire, then the orders on
        /// the thing under the cursor or the focus), then the offer of the hovered thing's main act.</summary>
        void HoldPillFrame()
        {
            if (vpillFill == null) BuildHoldPill();
            var h = host; var cam = h.View; bool modal = m.Ended || h.Modal != ModalGate.Modal.None || h.Ring != null || h.Panel != null || h.Loud != null || cam == null;
            Target t = null; string anchorId = "", text = "", icon = ""; Color c = Cream; bool harm = false, seen = false; float fill = -1f; var faces = new List<int>(); string state = "";
            if (!modal)
            {
                var ch = h.Charge;
                if (ch != null && ch.Shown)
                {
                    t = ch.T; fill = ch.Progress; state = "charge";
                    if (ch.Fire) { text = "Hold to fire" + (ch.Bystander ? " · bystander" : "") + (ch.Extra.Length > 0 ? " · " + ch.Extra : ""); (c, harm) = (HarmPink, true); icon = "fire"; for (int k = 0; k < 4; k++) if (h.Selected[k] || h.Driving && k == h.DriveAgent) faces.Add(k); if (h.Driving) { faces.Clear(); faces.Add(h.DriveAgent); } }
                    else { var r = ch.Plan.Row(ch.Way); var o = r.I.Order; text = "Hold to " + LowerFirst(r.I.Label); (c, harm) = KindOf(o); icon = r.I.Lead.Length > 0 ? r.I.Lead : HudModel.AtlasIcon(o.Do, o.Approach, ""); faces.AddRange(h.DoersOf(ch.Plan, ch.Way)); }
                }
                else
                {
                    var live = h.FireStatus();
                    var on = h.Hover ?? (h.Driving ? h.DriveFocus : null);
                    if (live == null && on != null) live = h.OrderStatus(on);
                    if (live != null && live.Text.Length + live.Items.Count > 0)
                    {
                        state = live.Fire ? "fire" : "order";
                        if (live.Fire) { anchorId = live.Id; (c, harm) = (HarmPink, true); icon = "fire"; } else { t = live.T; (c, harm) = KindOf(live.O); icon = live.O != null ? HudModel.AtlasIcon(live.O.Do, live.O.Approach, "") : ""; }
                        if (live.Items.Count > 0) { var parts = new List<string>(); foreach (var (k, w) in live.Items) parts.Add($"{Op.Name((OpId)k)[0].ToString().ToUpperInvariant()}{Op.Name((OpId)k)[1..]}: {w}"); text = string.Join(" · ", parts); faces.AddRange(live.Agents); }
                        else { text = live.Text; faces.AddRange(live.Agents); }
                    }
                    else if (on != null && h.HoverIntent is { Actionable: true } hi && on.Kind != "op")
                    {
                        state = "offer"; t = on; (c, harm) = KindOf(hi.Order); icon = hi.Order.Do == "fire" ? "fire" : hi.Lead.Length > 0 ? hi.Lead : HudModel.AtlasIcon(hi.Order.Do, hi.Order.Approach, "");
                        text = (h.HoverFire ? "Right hold · " : "Hold · ") + PillLine(h, hi); faces.AddRange(h.HoverDoers); seen = h.HoverSeen;
                    }
                }
            }
            Vector3? at = null;
            if (text.Length > 0) { if (anchorId.Length > 0) at = h.DrawnAt(anchorId); else if (t != null) at = h.TargetWorld(t); }
            Vector2 q = default; bool show = at.HasValue && Proj(at.Value + Vector3.up * (t == null || t.Kind is "person" or "crowd" or "hostile" or "bystander" or "patrol" or "rivals" or "security" or "unit" ? 2.25f : 1.5f), out q);
            Show(vpill, show); Show(vpillTail, show); PillState = show ? state : ""; if (!show) return;
            string key = text + icon + c + seen + string.Join(",", faces) + state;
            if (key != holdKey) { holdKey = key; Text(vpillText, text); Art(vpillIcon, icon.Length > 0 ? "icons/singles/" + icon : "", PillInk); Faces(faces); Show(vpillSeen, seen); }
            Show(vpillFill, fill >= 0f); if (fill >= 0f) { float pw0 = float.IsNaN(vpill.resolvedStyle.width) ? 300f : vpill.resolvedStyle.width; vpillFill.style.width = pw0 * fill; vpillFill.style.height = 34; }
            vpill.style.backgroundColor = c; Round(vpill, harm ? 6f : 17f);
            float pw = float.IsNaN(vpill.resolvedStyle.width) ? 300f : vpill.resolvedStyle.width, W = root.layout.width > 0 ? root.layout.width : 1920f;
            float x = Mathf.Clamp(q.x - pw / 2f, 14f, Mathf.Max(14f, W - pw - 14f));
            vpill.style.left = x; vpill.style.top = q.y - 10f - 34f - 9f; vpillTail.style.left = q.x - 7f; vpillTail.style.top = q.y - 10f - 9f - 1f;
            vpill.BringToFront(); vpillTail.BringToFront();
        }

        // ---------------------------------------------------------------- the street marks

        VisualElement squadBtn; readonly List<(VisualElement root, VisualElement face, Label text)> ghostEls = new(); readonly List<(VisualElement root, Label text)> clockEls = new();
        VisualElement dragGhost; readonly List<VisualElement> dragFaces = new();

        void Circle(Vector3 c, float r, Color col, float w, bool dashed, float under = 0.55f)
        {
            var pts = new List<Vector2>(41);
            for (int i = 0; i <= 40; i++) { float a = i * Mathf.PI * 2f / 40f; if (!Proj(c + new Vector3(Mathf.Cos(a) * r, 0.04f, Mathf.Sin(a) * r), out var q)) return; pts.Add(q); }
            paints.Add(pp =>
            {
                pp.lineJoin = LineJoin.Round; pp.lineCap = LineCap.Butt;
                for (int pass = 0; pass < 2; pass++)
                {
                    pp.strokeColor = pass == 0 ? new Color(Ink.r, Ink.g, Ink.b, under) : col; pp.lineWidth = pass == 0 ? w + 2f : w;
                    if (!dashed) { pp.BeginPath(); pp.MoveTo(pts[0]); for (int i = 1; i < pts.Count; i++) pp.LineTo(pts[i]); pp.Stroke(); continue; }
                    for (int i = 0; i + 1 < pts.Count; i += 2) { pp.BeginPath(); pp.MoveTo(pts[i]); pp.LineTo(pts[i + 1]); pp.Stroke(); }
                }
            });
        }

        void DashedPath(List<Vector3> world, Color col)
        {
            var pts = new List<Vector2>(); foreach (var w in world) if (Proj(w, out var q)) pts.Add(q); if (pts.Count < 2) return;
            paints.Add(pp =>
            {
                pp.lineCap = LineCap.Round; pp.strokeColor = col; pp.lineWidth = 2.4f;
                for (int j = 1; j < pts.Count; j++) { float len = Vector2.Distance(pts[j - 1], pts[j]); int n = Mathf.Max(1, (int)(len / 12f)); for (int s = 0; s < n; s++) { var a = Vector2.Lerp(pts[j - 1], pts[j], s / (float)n); var b = Vector2.Lerp(pts[j - 1], pts[j], (s + 0.55f) / n); pp.BeginPath(); pp.MoveTo(a); pp.LineTo(b); pp.Stroke(); } }
            });
        }

        /// <summary>Per frame after the less HUD (overhead paints; the follow view draws none of them).</summary>
        void HoldMarks()
        {
            var h = host; var g = h.Game; bool on = h.controls.HoldActs && g != null && !m.Ended && !m.BriefingOpen;
            // the Squad button under the roster column
            if (squadBtn == null)
            {
                squadBtn = E(root, "s-plate_dark", ColX, ColY + 416 * RosterScale + 8, 86, 30, pick: true); squadBtn.style.alignItems = Align.Center; squadBtn.style.justifyContent = Justify.Center;
                var sl = L(squadBtn, "verb-name"); sl.text = "Squad"; sl.style.fontSize = 12; sl.style.unityTextAlign = TextAnchor.MiddleCenter;
                squadBtn.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.SelectAll(); e.StopPropagation(); } });
                manifest.Add((squadBtn, "image", () => "squad button"));
            }
            Show(squadBtn, on && m.Dock == RosterDock.Left && !h.Following);
            int gu = 0, cu = 0;
            if (on && !h.Following && h.View)
            {
                // destination rings in each agent's colour (a walk solid; a run dashed and doubled), for a plain walk
                for (int k = 0; k < 4; k++)
                {
                    var op = g.Ops[k]; if (op.Walk == null || op.Inside || op.Down || h.PendingOf(k).verb.Length > 0) continue;
                    var end = h.frame.World(op.Walk.End);
                    if (op.Running) { Circle(end, 0.6f, RoleColours[k], 2.4f, true); Circle(end, 0.3f, RoleColours[k], 2f, false); } else Circle(end, 0.5f, RoleColours[k], 2.4f, false);
                }
                // the Watch's reach round its target (the cones the cell knows show with it: HostSight)
                if (h.WatchReach is (Target wt, int wk) && h.Hover != null && h.TargetWorld(wt) is Vector3 wc) Circle(wc, g.R.WatchReachCm((OpId)wk) / 100f, LookBlue, 2.6f, false, 0.6f);
                // the plan's ghosts
                foreach (var it in h.Plan)
                {
                    var col = RoleColours[it.K]; DashedPath(it.Path, new Color(col.r, col.g, col.b, 0.85f));
                    var end = it.Path.Count > 0 ? it.Path[it.Path.Count - 1] : h.frame.World(g.Ops[it.K].Pos); Circle(end, 0.55f, col, 2.4f, true);
                    if (!Proj(end + Vector3.up * 1.2f, out var gq)) continue;
                    while (ghostEls.Count <= gu)
                    {
                        var r = E(marks, null, 0, 0, float.NaN, 30); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.paddingLeft = 4; r.style.paddingRight = 12; Round(r, 15); Border(r, 2f, Ink);
                        r.style.backgroundColor = new Color(Cream.r, Cream.g, Cream.b, 0.88f); var f = E(r, null, w: 22, h: 22); Round(f, 11); f.style.flexShrink = 0; var l = L(r, "chip-value"); l.style.color = PillInk; l.style.fontSize = 13; l.style.marginLeft = 6; l.style.unityFontStyleAndWeight = FontStyle.Bold;
                        ghostEls.Add((r, f, l)); // one or two words: no manifest line (the budget's line is the objective)
                    }
                    var (gr, gf, gl) = ghostEls[gu++]; Show(gr, true); Text(gl, it.Label); Bg(gf, $"portraits/cameo_{Op.Name((OpId)it.K)}_normal", Color.white); Border(gf, 2f, col);
                    float gw = float.IsNaN(gr.resolvedStyle.width) ? 160 : gr.resolvedStyle.width; gr.style.left = gq.x - gw / 2; gr.style.top = gq.y - 40;
                }
                // the attention clock over an agent standing in a watcher's sight (the engine's Attention)
                var (notice, wary, report) = g.AttentionMarks;
                for (int k = 0; k < 4; k++)
                {
                    var op = g.Ops[k]; if (op.Inside || op.Down || op.InCar || op.Walk != null) continue; string nm = Op.Name((OpId)k); int ticks = 0;
                    foreach (var w in g.WatcherIds()) { var (st, who) = g.Attention(w); if (who == nm) ticks = Mathf.Max(ticks, g.AttentionTicks(w, nm)); }
                    if (ticks < Game.Hz / 2 || h.Views?.Find("op/" + nm) is not Transform tr || !tr.gameObject.activeSelf || !Proj(tr.position + Vector3.up * 2.7f, out var aq)) continue;
                    float s = ticks / (float)Game.Hz; string word = s >= report ? "report" : s >= wary ? "wary" : s >= notice ? "noticed" : "seen";
                    while (clockEls.Count <= cu)
                    {
                        var r = E(marks, null, 0, 0, 72, 24); r.style.alignItems = Align.Center; r.style.justifyContent = Justify.Center; Round(r, 12); Border(r, 2f, Ink);
                        var l = L(r, "chip-value"); l.style.fontSize = 12; l.style.unityFontStyleAndWeight = FontStyle.Bold; l.style.unityTextAlign = TextAnchor.MiddleCenter; clockEls.Add((r, l)); // one or two words: no manifest line (the budget's line is the objective)
                    }
                    var (cr, cl) = clockEls[cu++]; Show(cr, true); Text(cl, word);
                    cr.style.backgroundColor = word == "report" ? Red : word == "wary" ? Coral : word == "noticed" ? Amber : new Color(Cream.r, Cream.g, Cream.b, 0.92f); cl.style.color = word == "report" ? Color.white : PillInk;
                    cr.style.left = aq.x - 36; cr.style.top = aq.y - 12; var cq = aq + new Vector2(-46, 0); float frac = report > 0 ? Mathf.Clamp01(s / report) : 0f;
                    paints.Add(pp => { RingArc(pp, cq, 8f, 1f, new Color(Ink.r, Ink.g, Ink.b, 0.6f), 4f); RingArc(pp, cq, 8f, frac, Coral, 2.6f); });
                }
                // the portrait drag: a band from the portrait to the cursor, the faces it carries
                if (h.PortraitDragging && PortraitScreen(h.PortraitDragFrom) is Vector2 ps)
                {
                    var a = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(ps.x, Screen.height - ps.y)); var b = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(h.PortraitDragAt.x, Screen.height - h.PortraitDragAt.y));
                    paints.Add(pp => { pp.lineCap = LineCap.Round; pp.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.6f); pp.lineWidth = 4.5f; pp.BeginPath(); pp.MoveTo(a); pp.LineTo(b); pp.Stroke(); pp.strokeColor = Cream; pp.lineWidth = 2.2f; pp.BeginPath(); pp.MoveTo(a); pp.LineTo(b); pp.Stroke(); });
                    if (dragGhost == null) { dragGhost = E(marks, null, 0, 0, float.NaN, 28); dragGhost.style.flexDirection = FlexDirection.Row; for (int i = 0; i < 4; i++) { var f = E(dragGhost, null, w: 26, h: 26); Round(f, 13); f.style.marginLeft = i == 0 ? 0 : -8; dragFaces.Add(f); } }
                    var carry = h.PortraitCarry; Show(dragGhost, true);
                    for (int i = 0; i < 4; i++) { bool fo = i < carry.Count; Show(dragFaces[i], fo); if (fo) { Bg(dragFaces[i], $"portraits/cameo_{Op.Name((OpId)carry[i])}_normal", Color.white); Border(dragFaces[i], 2.5f, RoleColours[carry[i]]); } }
                    dragGhost.style.left = b.x + 14; dragGhost.style.top = b.y + 10; dragGhost.BringToFront();
                }
                else if (dragGhost != null) Show(dragGhost, false);
            }
            else if (dragGhost != null) Show(dragGhost, false);
            for (int i = gu; i < ghostEls.Count; i++) Show(ghostEls[i].root, false);
            for (int i = cu; i < clockEls.Count; i++) Show(clockEls[i].root, false);
        }
    }
}
