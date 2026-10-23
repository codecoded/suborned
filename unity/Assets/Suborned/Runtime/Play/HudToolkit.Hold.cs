using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;
using Target = Suborned.Play.HandoverHost.Target;

namespace Suborned.Play
{
    /// <summary>Brief 041, the hold preset in the kit HUD (testbed/controls markers.js 'pill', status.js, ui.js, app.js drawGhosts, attention):
    ///  · the verb pill, the only hover marker: the act's icon and colour, "Hold · Watch the guard · 26 s · …", the doer's face wherever an agent
    ///    would be named, a coral "Watched spot" when the stand spot is in a known cone; a hold fills it, its first word swapping "Hold ·" (or
    ///    "Right hold ·") for "Let go ·" when full, the price kept (Fable's wording.md decision 1); live words replace the offer (FireStatus, OrderStatus);
    ///  · the Watch's reach ring round the target; each agent's destination ring in its colour (a run: dashed and doubled); the paused plan's
    ///    ghosts (a dashed path, a dashed ring, a pill with the face); an agent standing in a cone, its attention clock; the portrait drag's band;
    ///    the Squad button under the roster. It reads the host only: replays are untouched.</summary>
    public sealed partial class HudToolkit
    {
        static Color Hex(int rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        string holdKey = "";

        static string LowerFirst(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];

        /// <summary>The proof reads it: the pill's words now ("" hidden) are PillWords; and what it is about.</summary>
        public static string PillState { get; private set; } = "";

        /// <summary>Per frame (the hold preset): what the pill says and where. A charge first, then the live words (fire, then the orders on
        /// the thing under the cursor or the focus), then the offer of the hovered thing's main act.</summary>
        void HoldPillFrame()
        {
            var h = host; var cam = h.View; bool modal = m.Ended || h.Modal != ModalGate.Modal.None || h.Ring != null || h.Panel != null || h.Loud != null || cam == null;
            Target t = null; string anchorId = "", text = "", icon = ""; bool harm = false, seen = false; float fill = -1f; var faces = new List<int>(); string state = "";
            if (!modal)
            {
                var ch = h.Charge;
                if (ch != null && ch.Shown)
                {
                    // Fable's wording row 3-6 (decision 1: the price stays while you hold): one line, its first word swaps: "Hold · …" filling, "Let go · …" full
                    t = ch.T; fill = ch.Progress; state = ch.Ready ? "ready" : "charge"; string head = ch.Ready ? "Let go · " : ch.Fire ? "Right hold · " : "Hold · ";
                    if (ch.Fire) { var fo = t != null ? h.FireOfferFor(t) : null; text = head + (fo != null ? PillLine(h, fo) : $"Fire at {(t?.Label ?? "")}" + (ch.Extra.Length > 0 ? " · " + ch.Extra : "")); harm = true; icon = "fire"; faces.AddRange(h.FireShooters(t)); } // item 8: the price on the charge too (Trail a hit, Alert)
                    else { var r = ch.Plan.Row(ch.Way); var o = r.I.Order; text = head + PillLine(h, r.I); harm = Harmful(o); icon = r.I.Lead.Length > 0 ? r.I.Lead : HudModel.AtlasIcon(o.Do, o.Approach, ""); faces.AddRange(ch.Doers ?? h.DoersOf(ch.Plan, ch.Way)); }
                }
                else
                {
                    var live = h.FireStatus(h.Hover); // weapons-free picks only on the hovered target (the HUD budget: one line on screen); orders you gave always
                    var on = h.Hover ?? (h.Driving ? h.DriveFocus : null);
                    if (live == null && on != null) live = h.OrderStatus(on);
                    if (live != null && live.Text.Length + live.Items.Count > 0)
                    {
                        state = live.Fire ? "fire" : "order";
                        if (live.Fire) { anchorId = live.Id; harm = true; icon = "fire"; } else { t = live.T; harm = Harmful(live.O); icon = live.O != null ? HudModel.AtlasIcon(live.O.Do, live.O.Approach, "") : ""; }
                        if (live.Items.Count > 0) { var parts = new List<string>(); foreach (var (k, w) in live.Items) parts.Add($"{Op.Name((OpId)k)[0].ToString().ToUpperInvariant()}{Op.Name((OpId)k)[1..]}: {w}"); text = string.Join(" · ", parts); faces.AddRange(live.Agents); }
                        else { text = live.Text; faces.AddRange(live.Agents); }
                    }
                    else if (on != null && h.HoverIntent is { Actionable: true } hi && on.Kind != "op")
                    {
                        state = "offer"; t = on; harm = Harmful(hi.Order); icon = hi.Order.Do == "fire" ? "fire" : hi.Lead.Length > 0 ? hi.Lead : HudModel.AtlasIcon(hi.Order.Do, hi.Order.Approach, "");
                        text = (h.HoverFire ? (h.FightOnNow ? "" : "Right hold · ") : "Hold · ") + PillLine(h, hi); faces.AddRange(h.HoverFire ? h.FireShooters(on) : h.HoverDoers); seen = h.HoverSeen; // in a fight a right click fires: no "Right hold" (the quiet fight pill)
                    }
                }
            }
            Vector3? at = null;
            if (text.Length > 0) { if (anchorId.Length > 0) at = h.DrawnAt(anchorId); else if (t != null) at = h.TargetWorld(t); }
            Vector2 q = default; bool show = at.HasValue && Proj(at.Value + Vector3.up * (t == null || t.Kind is "person" or "crowd" or "hostile" or "bystander" or "patrol" or "rivals" or "security" or "unit" ? 2.25f : 1.5f), out q);
            Show(vpill, show); Show(vpillTail, show); PillState = show ? state : ""; if (!show) { CancelTagFrame("", null, false); PipShown = false; SweepShownNow = -1f; return; }
            PillLook(text, icon, harm, faces, seen, fill, state);
            vpill.style.opacity = state is "charge" or "ready" ? h.HoldDim : 1f; vpillTail.style.opacity = vpill.style.opacity; // the hold dims as the cursor pulls away (it cancels at 80 px)
            vpill.style.scale = state == "ready" ? new Scale(new Vector3(ReadyScale, ReadyScale, 1f)) : new Scale(Vector3.one); // full: the pill pops while it says "Let go"
            PillPlace(q, out _);
            // brief 043: the cancel tag over the pill, and the lit pip of an act that just opened
            bool fireish = state == "fire" || h.Charge is { Fire: true } || state == "offer" && h.HoverFire;
            CancelTagFrame(state, faces, fireish);
            Order po = state == "offer" ? h.HoverIntent?.Order : state is "charge" or "ready" && h.Charge is { Fire: false, Plan: not null } pc ? pc.Plan.Row(pc.Way)?.I?.Order : null;
            PipShown = po != null && t != null && h.Fresh(t, po);
            Show(vpillPip, PipShown); // on the frame's top-right chrome corner
        }

        // ---------------------------------------------------------------- the street marks

        const float GhostH = 38f, ClockH = 28f;
        VisualElement squadBtn; readonly List<(VisualElement root, VisualElement face, Label text)> ghostEls = new(); readonly List<(VisualElement root, Label text)> clockEls = new();
        VisualElement dragGhost; readonly List<VisualElement> dragFaces = new();

        /// <summary>A ring on the ground (brief 046: drawn in the world, depth-tested, so legs and props in front hide it; HudToolkit.Ground).</summary>
        void Circle(Vector3 c, float r, Color col, float w, bool dashed, float under = 0.55f) => GroundRing(c, r, col, w, dashed, under);

        /// <summary>A dashed path on the ground (brief 046: in the world, as Circle).</summary>
        void DashedPath(List<Vector3> world, Color col) => GroundPath(world, col, 2.4f, true, 0.85f);

        /// <summary>Per frame after the less HUD (overhead paints; the follow view draws none of them).</summary>
        void HoldMarks()
        {
            var h = host; var g = h.Game; bool on = h.controls.HoldActs && g != null && !m.Ended && !m.BriefingOpen;
            // the Squad button under the roster column
            if (squadBtn == null)
            {
                squadBtn = E(root, "s-plate_dark", ColX, ColY + ColH * RosterScale + 8, 86, 30, pick: true); squadBtn.style.alignItems = Align.Center; squadBtn.style.justifyContent = Justify.Center;
                var sl = L(squadBtn, "verb-name"); sl.text = "Squad"; sl.style.fontSize = 12; sl.style.unityTextAlign = TextAnchor.MiddleCenter;
                squadBtn.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.SelectAll(); e.StopPropagation(); } });
                manifest.Add((squadBtn, "image", () => "squad button"));
            }
            Show(squadBtn, on && m.Dock == RosterDock.Left && !h.Following);
            int gu = 0, cu = 0;
            if (on && !h.Following && h.View)
            {
                // destination rings in cream over an ink underlay (a walk solid; a run dashed and doubled), for a plain walk; no role colours (brief 044 §6)
                for (int k = 0; k < 4; k++)
                {
                    var op = g.Ops[k]; if (op.Walk == null || op.Inside || op.Down || h.PendingOf(k).verb.Length > 0) continue;
                    var end = h.frame.World(op.Walk.End);
                    if (op.Running) { Circle(end, 0.6f, LookGround, 2.4f, true); Circle(end, 0.3f, LookGround, 2f, false); } else Circle(end, 0.5f, LookGround, 2.4f, false);
                }
                // the Watch's reach round its target (the cones the cell knows show with it: HostSight)
                if (h.WatchReach is (Target wt, int wk) && h.Hover != null && h.TargetWorld(wt) is Vector3 wc) Circle(wc, g.R.WatchReachCm((OpId)wk) / 100f, LookLive, 2.6f, false, 0.6f); // turquoise-light: the live accent, as the pill's hold
                // the plan's ghosts
                foreach (var it in h.Plan)
                {
                    DashedPath(it.Path, new Color(LookGround.r, LookGround.g, LookGround.b, 0.85f)); // the doer reads by the face on the ghost plate, not a role colour
                    var end = it.Path.Count > 0 ? it.Path[it.Path.Count - 1] : h.frame.World(g.Ops[it.K].Pos); Circle(end, 0.55f, LookGround, 2.4f, true);
                    if (!Proj(end + Vector3.up * 1.2f, out var gq)) continue;
                    while (ghostEls.Count <= gu)
                    {
                        // the ghost plate: plate_dark (38 px at slice 0.43, the verb pill's height), the doer's face in the chrome ring, the words (glyphs 4 px clear of the rim)
                        var r = PlateDark(marks, GhostH, 0.43f); r.style.position = Position.Absolute; r.style.paddingLeft = 10; r.style.paddingRight = 12; // opaque (Glyph 0.2.65 review 7: the world showed through at 0.92)
                        var f = Cameo(r, 16f, 1.5f); var l = Words(r, 13f); l.style.marginLeft = 6;
                        ghostEls.Add((r, f, l)); // one or two words: no manifest line (the budget's line is the objective)
                    }
                    var (gr, gf, gl) = ghostEls[gu++]; Show(gr, true); Text(gl, it.Label); CameoOf(gf, it.K);
                    float gw = float.IsNaN(gr.resolvedStyle.width) ? 160 : gr.resolvedStyle.width; gr.style.left = gq.x - gw / 2; gr.style.top = gq.y - GhostH - 6;
                }
                // the attention clock over an agent standing in a watcher's sight (the engine's Attention)
                var (notice, wary, report) = g.AttentionMarks;
                for (int k = 0; k < 4; k++)
                {
                    var op = g.Ops[k]; if (op.Inside || op.Down || op.InCar || op.Walk != null) continue; string nm = Op.Name((OpId)k); int ticks = 0;
                    foreach (var w in g.WatcherIds()) { var (st, who) = g.Attention(w); if (who == nm) ticks = Mathf.Max(ticks, g.AttentionTicks(w, nm)); }
                    if (ticks < Game.Hz / 2 || h.Views?.Find("op/" + nm) is not Transform tr || !tr.gameObject.activeSelf || !Proj(tr.position + Vector3.up * 2.7f, out var aq)) continue;
                    float s = ticks / (float)Game.Hz; string word = s >= report ? "reported" : s >= wary ? "wary" : s >= notice ? "noticed" : "seen";
                    while (clockEls.Count <= cu)
                    {
                        // the attention clock (brief 044 §6): plate_dark, cream words, a draining underline in cream, amber (the HUD's "running out") once wary
                        var r = PlateDark(marks, ClockH, 0.43f); r.style.position = Position.Absolute; r.style.width = 74; r.style.justifyContent = Justify.Center;
                        var l = Words(r, 12.5f); l.style.unityTextAlign = TextAnchor.MiddleCenter; l.style.marginBottom = 1;
                        var u = E(r, null, 11, ClockH - 8.5f, 0, 2.5f); u.name = "under"; Round(u, 1.25f);
                        clockEls.Add((r, l)); // one or two words: no manifest line (the budget's line is the objective)
                    }
                    var (cr, cl) = clockEls[cu++]; Show(cr, true); Text(cl, word);
                    bool soon = word is "wary" or "reported"; float frac = report > 0 ? Mathf.Clamp01(s / report) : 0f;
                    var under = cr.Q("under"); under.style.backgroundColor = soon ? LookSoon : Cream; under.style.width = (74f - 22f) * frac;
                    cr.style.left = aq.x - 37; cr.style.top = aq.y - ClockH / 2f; var cq = aq + new Vector2(-48, 0); var arc = soon ? LookSoon : Cream;
                    paints.Add(pp => { RingArc(pp, cq, 8f, 1f, new Color(Ink.r, Ink.g, Ink.b, 0.6f), 4f); RingArc(pp, cq, 8f, frac, arc, 2.6f); });
                }
                // the portrait drag: a band from the portrait to the cursor, the faces it carries
                if (h.PortraitDragging && PortraitScreen(h.PortraitDragFrom) is Vector2 ps)
                {
                    var a = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(ps.x, Screen.height - ps.y)); var b = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(h.PortraitDragAt.x, Screen.height - h.PortraitDragAt.y));
                    paints.Add(pp => { pp.lineCap = LineCap.Round; pp.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.6f); pp.lineWidth = 4.5f; pp.BeginPath(); pp.MoveTo(a); pp.LineTo(b); pp.Stroke(); pp.strokeColor = Cream; pp.lineWidth = 2.2f; pp.BeginPath(); pp.MoveTo(a); pp.LineTo(b); pp.Stroke(); });
                    if (dragGhost == null) { dragGhost = E(marks, null, 0, 0, float.NaN, 32); dragGhost.style.flexDirection = FlexDirection.Row; for (int i = 0; i < 4; i++) { var f = Cameo(dragGhost, FaceSize); f.style.marginLeft = i == 0 ? 0 : -7; dragFaces.Add(f); } }
                    var carry = h.PortraitCarry; Show(dragGhost, true);
                    for (int i = 0; i < 4; i++) { bool fo = i < carry.Count; Show(dragFaces[i], fo); if (fo) CameoOf(dragFaces[i], carry[i]); }
                    dragGhost.style.left = b.x + 14; dragGhost.style.top = b.y + 10; dragGhost.BringToFront();
                }
                else if (dragGhost != null) Show(dragGhost, false);
            }
            else if (dragGhost != null) Show(dragGhost, false);
            FreeToggleFrame(on && m.Dock == RosterDock.Left && !h.Following);
            UnderFireMarks(); KnownFaceMarks(on);
            for (int i = gu; i < ghostEls.Count; i++) Show(ghostEls[i].root, false);
            for (int i = cu; i < clockEls.Count; i++) Show(clockEls[i].root, false);
        }

        // ---------------------------------------------------------------- brief 041 / engine 0.19: weapons free, under fire, known faces

        VisualElement freeBtn, freeIcon, freePlate; Label freeWord;
        /// <summary>G's toggle under the roster, beside Squad: the gun lit while weapons free is on; a click toggles it. Its word shows a moment
        /// after a change ("Weapons free" / "Return fire only", Fable's wording decision 4).</summary>
        void FreeToggleFrame(bool show)
        {
            var h = host;
            if (freeBtn == null)
            {
                freeBtn = E(root, "s-plate_dark", ColX + 92, ColY + ColH * RosterScale + 5, 38, 36, pick: true); // the glyph 4 px clear of plate_dark's rim (10.2 px in)
                freeIcon = E(freeBtn, null, 12.5f, 11.5f, 13, 13); // placed: 4 px clear of the rim (10.2 px in)

                freeBtn.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ToggleFree(); e.StopPropagation(); } });
                manifest.Add((freeBtn, "image", () => "weapons free toggle"));
                // the word on its own plate_dark (review 9: bare cream text read 1.3:1 on the pale ground)
                freePlate = PlateDark(root, 30f, 0.3f); freePlate.style.position = Position.Absolute; freePlate.style.left = ColX + 136; freePlate.style.top = ColY + ColH * RosterScale + 8; freePlate.style.paddingLeft = 13; freePlate.style.paddingRight = 13;
                freeWord = Words(freePlate, 13f); freeWord.style.marginBottom = 1;
                manifest.Add((freePlate, "line", () => freeWord.text));
            }
            bool fon = h.Game != null && h.Game.WeaponsFree; Show(freeBtn, show && h.Game != null);
            if (show) { Icon(freeIcon, "fire", fon ? LookLive : LookHead); freeBtn.style.opacity = 1f; } // opaque in both states (review 9): on, the gun lit in the live accent; off, dim. (A tick badge broke the audit's 4 px rim rule in a 38 px plate.)
            bool said = show && Time.unscaledTime - h.FreeSaidAt < 1.6f; Show(freePlate, said); if (said) Text(freeWord, fon ? "Weapons free" : "Return fire only");
        }

        /// <summary>Per agent, the screen angle (panel space, y down) from its portrait toward who is shooting it; null when nobody is (the
        /// portrait's ticks come in from that side).</summary>
        static readonly float?[] ufAngle = new float?[4];
        readonly int[] ufLast = { -1, -1, -1, -1 }; readonly float[] ufPopAt = { -9f, -9f, -9f, -9f };

        /// <summary>Engine 0.19 UnderFireOf (Sky's answer 1, no auto-draw: "the player character avatar should clearly show they're being shot at"):
        /// the under-fire arc on the ground at the agent's feet, centred on the shooter (brief 046 round 2, style A).</summary>
        void UnderFireMarks()
        {
            var h = host; var g = h.Game; for (int k = 0; k < 4; k++) ufAngle[k] = null; if (g == null || m.Ended || h.View == null) return;
            float now = Time.unscaledTime;
            for (int k = 0; k < 4; k++)
            {
                var list = h.UnderFireNow[k]; if (list.Count == 0) continue;
                var by = list[0]; foreach (var x in list) if (x.Aiming) { by = x; break; }
                if (by.Last != ufLast[k]) { ufLast[k] = by.Last; ufPopAt[k] = now; }
                var tr = h.Views?.Find("op/" + Op.Name((OpId)k)); if (!tr || !tr.gameObject.activeSelf) continue; var from = h.DrawnAt(by.By); if (!from.HasValue) continue;
                var a = tr.position; var d = from.Value - a; d.y = 0f; if (d.sqrMagnitude < 0.01f) continue; var f = d.normalized; var side = new Vector3(-f.z, 0f, f.x);
                if (Proj(a, out var pa) && Proj(a + f * 3f, out var pb)) ufAngle[k] = Mathf.Atan2(pb.y - pa.y, pb.x - pa.x);
                // brief 046 round 2 / 049 (Sky: "horrible ... a lot more subtle"; Glyph's pick A): one arc just outside the cell's ring,
                // centred on the shooter, on the ground behind the legs, breathing 60 to 100 % (why.arrow: alpha 0.85, weight 1; Sky judges 1 vs 1.6).
                // Units: the selected ring's outer radius (0.42 + 0.055 m) = 100; arc r 118, 70 deg, 10 thick, ink-deep edge 17 at 50 %.
                var (gu, gr) = GroundAxes(); float ang = Mathf.Atan2(Vector3.Dot(f, gr), Vector3.Dot(f, gu)) * Mathf.Rad2Deg; float unit = 0.475f / 100f, wgt = Mathf.Max(0.5f, Tok.why?.arrow?.weight ?? 1f);
                // Glyph 0.2.79 #1 (the arc read as a faint hairline): as strong as the shooter's red ring: the threat red, its 0.12 m line and
                // ink-deep edge (0.17 m at 60 %), solid; a shot lands: the line swells 30 % for 0.35 s. Same place and span (r 118, 70 deg).
                float pop = Mathf.Max(0f, 1f - (now - ufPopAt[k]) / 0.35f), lw = 0.12f * wgt * (1f + 0.3f * pop);
                ArcQuads(GroundOf(new Color(InkDeep.r, InkDeep.g, InkDeep.b, EdgeAlpha), true), a, 118f * unit, lw + 0.05f, ang - 35f, ang + 35f, gu, gr);
                ArcQuads(GroundOf(RingThreat, false), a, 118f * unit, lw, ang - 35f, ang + 35f, gu, gr);
            }
        }

        /// <summary>The portrait's under-fire cue: the rim's pulse ring (CuePaint).</summary>
        static void UnderFireTicks(Painter2D p, int k, Vector2 c, float s)
        {   // brief 046 (why.ring cream, why.pulseMs): the pulse ring on the rim; the cause sits in the status slot, the direction on the ground's arc
            float now = Time.unscaledTime, beat = 0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 2f / 0.9f);
            p.strokeColor = new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.5f); p.lineWidth = (3.2f + 1.2f * beat) * s; p.BeginPath(); p.Arc(c, 33f * s, 0f, 360f); p.Stroke();
            p.strokeColor = new Color(Cream.r, Cream.g, Cream.b, 0.55f + 0.45f * beat); p.lineWidth = (1.8f + 1.2f * beat) * s; p.BeginPath(); p.Arc(c, 33f * s, 0f, 360f); p.Stroke();
        }

        readonly VisualElement[] knownEls = new VisualElement[4];
        /// <summary>Engine 0.19 KnownFaces: an agent whose face security knows carries a small "seen you" badge on its portrait (police fire on a
        /// known face from Alert 2, robots at wanted Heat). An icon, no words.</summary>
        void KnownFaceMarks(bool on)
        {
            var g = host.Game; IReadOnlyCollection<string> known = null; if (g != null && on && !m.Ended) try { known = g.KnownFaces; } catch (System.Exception) { known = null; }
            for (int k = 0; k < 4; k++)
            {
                bool show = known != null && System.Linq.Enumerable.Contains(known, Op.Name((OpId)k)) && PortraitScreen(k) is Vector2 && !(colWins[k]?.Under == true && m.Dock == RosterDock.Left); // the tall column: the status slot's sawyou
                if (knownEls[k] == null) { if (!show) continue; knownEls[k] = E(root, null, 0, 0, 20, 20); Round(knownEls[k], 10); knownEls[k].style.backgroundColor = Ink; Border(knownEls[k], 1.5f, Cream); var ic = E(knownEls[k], null, 2, 2, 16, 16); ic.name = "g"; manifest.Add((knownEls[k], "image", () => "face known badge")); }
                Show(knownEls[k], show); if (!show) continue;
                var ps = PortraitScreen(k).Value; var pp = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(ps.x, Screen.height - ps.y));
                knownEls[k].style.left = pp.x - 40; knownEls[k].style.top = pp.y - 40; Icon(knownEls[k].Q("g"), "sawyou", Cream);
            }
        }
    }
}
