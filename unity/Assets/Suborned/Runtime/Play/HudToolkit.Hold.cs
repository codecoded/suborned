using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;
using Target = Suborned.Play.HandoverHost.Target;

namespace Suborned.Play
{
    /// <summary>Brief 041, the hold preset in the kit HUD (testbed/controls markers.js 'pill', status.js, ui.js, app.js drawGhosts, attention):
    ///  · the verb pill, the only hover marker: the act's icon and colour, "Hold · Watch the guard · …" (brief 101: no seconds, no hit chance), the doer's face wherever an agent
    ///    would be named, a coral "Watched spot" when the stand spot is in a known cone; a hold fills it, its first word swapping "Hold ·" (or
    ///    "Right hold ·") for "Let go ·" when full, the price kept (Fable's wording.md decision 1); live words replace the offer (FireStatus, OrderStatus);
    ///  · the Watch's reach ring round the target; each agent's destination ring in its colour (a run: dashed and doubled); the paused plan's
    ///    ghosts (a dashed path, a dashed ring, a pill with the face); an agent standing in a cone, its attention clock; the portrait drag's band;
    ///    the Squad button under the roster. It reads the host only: replays are untouched.</summary>
    public sealed partial class HudToolkit
    {
        static Color Hex(int rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        string holdKey = "";
        VisualElement stackTab; readonly List<VisualElement> stackDots = new();
        /// <summary>The proof reads it: the stack tab's dots now ("2/3": the active one and the count), "" hidden.</summary>
        public static string StackShown { get; private set; } = "";
        /// <summary>Brief 067 F9 (Glyph): with two or more things under the cursor, a plate_dark 16 px seated on the pill's top edge (overlapping its
        /// chrome 6 px), one dot a candidate (6 px, 9 px pitch), the active one filled cream, the others a 1.4 px cream ring; and a dashed cream ring
        /// (0.5 m, 1.8 px over ink-deep, 60 %) at each other candidate's feet. Z, Shift+Z, Tab and the wheel (rested 0.3 s) step.</summary>
        void StackTabFrame(string state)
        {
            var h = host; int n = h.HoverStackCount; bool on = n >= 2 && state is "offer" or "lock" or "fire" && vpill != null && vpill.resolvedStyle.display != DisplayStyle.None;
            if (stackTab == null) { if (!on) { StackShown = ""; return; } stackTab = PlateDark(marks, 16f, 0.3f); stackTab.style.position = Position.Absolute; stackTab.style.paddingLeft = 6; stackTab.style.paddingRight = 6; stackTab.style.alignItems = Align.Center; manifest.Add((stackTab, "image", () => "stack tab")); }
            Show(stackTab, on); if (!on) { StackShown = ""; return; }
            int act = h.HoverStackIndex, shown = Mathf.Min(n, 6);
            while (stackDots.Count < shown) { var d = E(stackTab, null, w: 6, h: 6); d.style.marginLeft = stackDots.Count == 0 ? 0 : 3; Round(d, 3); d.style.flexShrink = 0; stackDots.Add(d); }
            for (int i = 0; i < stackDots.Count; i++) { bool has = i < shown; Show(stackDots[i], has); if (!has) continue; bool a = i == act; stackDots[i].style.backgroundColor = a ? Cream : Color.clear; Border(stackDots[i], a ? 0f : 1.4f, Cream); }
            float pw = float.IsNaN(vpill.resolvedStyle.width) ? 300f : vpill.resolvedStyle.width, tw = shown * 9f + 12f;
            stackTab.style.left = vpill.style.left.value.value + pw / 2f - tw / 2f; stackTab.style.top = vpill.style.top.value.value - 16f + 6f; stackTab.BringToFront();
            var list = h.HoverStack; for (int i = 0; i < list.Count; i++) if (i != act && h.TargetWorld(list[i]) is Vector3 w) Circle(w, 0.5f, new Color(Cream.r, Cream.g, Cream.b, 0.6f), 1.8f, true);
            StackShown = $"{act + 1}/{n}";
        }

        /// <summary>Brief 067 E3 (Glyph): the locked pill's opacity.</summary>
        public static float LockPillAlpha = 0.80f;
        /// <summary>Glyph 067 E3 / F9: the pill's last state and target (a lock lapsing into the offer), the stack steps seen, the step glide.</summary>
        (string state, HandoverHost.Target t) pillWas = ("", null); int stepsWas; Vector2? pillQWas; Vector2 glideFrom; float glideAt = -9f, lockLapseAt = -9f;
        VisualElement lockAlt; Label lockAltText;
        /// <summary>The proof reads it: the Alt plate beside the pill now ("" hidden).</summary>
        public static string LockAltShown => live?.lockAlt != null && live.lockAlt.resolvedStyle.display != DisplayStyle.None ? live.lockAltText.text : "";
        /// <summary>Brief 067 E3: with Alt, the locked pill's long line (Fable's lock `line`) on a plate_dark under the pill; "" hides it.</summary>
        void LockAltFrame(string line)
        {
            // Sky 2026-11-07 ("but even then it's ugly"): the plate was a fixed one-line height, so a breakdown of several lines spilled over its edges onto
            // the street, and it sat under the pill, on the shooter's bark and the target's marks. Now it grows with its lines and sits beside the pill.
            // The proof reads LockAltShown (brief 101: never a hit chance)
            if (lockAlt == null)
            {
                if (line.Length == 0) return;
                lockAlt = PlateDark(marks, 26f, 0.3f); lockAlt.style.position = Position.Absolute; lockAlt.style.height = StyleKeyword.Auto; lockAlt.style.minHeight = 26; lockAlt.style.alignItems = Align.FlexStart;
                lockAlt.style.paddingLeft = 12; lockAlt.style.paddingRight = 12; lockAlt.style.paddingTop = 7; lockAlt.style.paddingBottom = 7;
                lockAltText = Words(lockAlt, 12.5f); lockAltText.style.whiteSpace = WhiteSpace.Normal; lockAltText.style.maxWidth = 380; manifest.Add((lockAlt, "altline", () => lockAltText.text)); // Glyph budget §4: an Alt line, its own kind
            }
            bool on = line.Length > 0 && vpill != null && vpill.resolvedStyle.display != DisplayStyle.None; Show(lockAlt, on); if (!on) return;
            Text(lockAltText, line); float pl = vpill.style.left.value.value, pt = vpill.style.top.value.value, pw = float.IsNaN(vpill.resolvedStyle.width) ? 300f : vpill.resolvedStyle.width, aw = float.IsNaN(lockAlt.resolvedStyle.width) ? 300f : lockAlt.resolvedStyle.width;
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) || size.x <= 0 ? 1920 : size.x;
            lockAlt.style.left = pl + pw + 10f + aw <= W - 8f ? pl + pw + 10f : Mathf.Max(8f, pl - aw - 10f); lockAlt.style.top = pt; lockAlt.BringToFront();
        }

        static string LowerFirst(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];

        /// <summary>The proof reads it: the pill's words now ("" hidden) are PillWords; and what it is about.</summary>
        public static string PillState { get; private set; } = "";

        /// <summary>Per frame (the hold preset): what the pill says and where. A charge first, then the live words (fire, then the orders on
        /// the thing under the cursor or the focus), then the offer of the hovered thing's main act; a lock; a passing car's state (brief 098).</summary>
        /// <summary>The top (panel y) of what stands over a camera: its window marker (as DoneMarks places it, before any lift for speech) or its dead
        /// disc (as StreetLess places it); +inf when neither.</summary>
        float CameraMarkTop(string camKey)
        {
            var h = host; float top = float.PositiveInfinity;
            foreach (var w in h.WindowMarks) if (w.Key == camKey && h.AnchorWorld(camKey) is Vector3 at && Proj(at + Vector3.up * 0.75f, out var mq)) top = Mathf.Min(top, mq.y - 18f - WinD);
            string id = camKey.Substring(7), dead = ""; try { dead = h.Game?.CameraDeadAs(id) ?? ""; } catch (System.Exception) { }
            if (dead.Length > 0 && h.Views?.Find(camKey) is Transform ct && ct.gameObject.activeInHierarchy && Proj(ct.position + Vector3.up * (h.Views.CamHeadUp - 1.6f), out var hq)) top = Mathf.Min(top, hq.y - 20f - 12f);
            return top;
        }

        void HoldPillFrame()
        {
            var h = host; var cam = h.View; bool modal = m.Ended || h.Modal != ModalGate.Modal.None || h.Ring != null || h.Panel != null || h.Loud != null || cam == null;
            Target t = null; string anchorId = "", text = "", icon = ""; pillPredNow = ""; pillAfterNow = false; pillFire = null; /* brief 101 step 6: a shot's offer lays out its "Goes loud" row */ bool harm = false, seen = false; float fill = -1f; var faces = new List<int>(); string state = "";
            if (!modal)
            {
                var ch = h.Charge;
                if (ch != null && ch.Shown)
                {
                    // Fable's wording row 3-6 (decision 1: the price stays while you hold): one line, its first word swaps: "Hold · …" filling, "Let go · …" full
                    t = ch.T; fill = ch.Progress; state = ch.Ready ? "ready" : "charge"; string head = ch.Ready ? "Let go · " : ch.Fire ? "Right hold · " : "Hold · ";
                    if (ch.Fire) { var fo = t != null ? h.FireOfferFor(t) : null; text = head + (fo != null ? PillLine(h, fo) : $"Fire at {(t?.Label ?? "")}"); harm = true; icon = "fire"; faces.AddRange(h.FireShooters(t)); pillFire = fo; } // item 8: the price on the charge too (Trail a hit, Alert)
                    else { var r = ch.Plan.Row(ch.Way); var pi = ch.PillI ?? r.I; var o = pi.Order; text = head + PillLine(h, pi); pillPredNow = pi.PredMark; harm = Harmful(o); icon = pi.Lead.Length > 0 ? pi.Lead : HudModel.AtlasIcon(o.Do, o.Approach, ""); faces.AddRange(ch.Doers ?? h.DoersOf(ch.Plan, ch.Way)); } // brief 067 C2: a group Board's own pill (the slowest time)
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
                        text = (h.HoverFire ? (h.FightOnNow ? "" : "Right hold · ") : "Hold · ") + PillLine(h, hi); if (h.HoverFire) pillFire = hi; if (!h.HoverFire) pillPredNow = hi.PredMark; faces.AddRange(h.HoverFire ? h.FireShooters(on) : h.HoverDoers); seen = h.HoverSeen; // in a fight a right click fires: no "Right hold" (the quiet fight pill)
                        pillAfterNow = hi.Order.Do == "drive-car" && hi.Outcome == $"after {host.ValeWord}'s car"; // Glyph 2026-10-26 §3: the yard before Vale's car parks: the caution plate
                    }
                    else if (on != null && h.HoverIntent is { Locked: true } lk && on.Kind != "op")
                    {   // brief 067 E3 (Glyph; Suture's 0.2.122 findings §2a): the locked pill: the lock disc, the way with its target in cream, a dim dot and
                        // the short lock in --text-dim, the doer's face; no "Hold" head, no sweep; 80 %. Alt: the long line under it (LockAltFrame)
                        state = "lock"; t = on; icon = "lock"; text = lk.Label + (lk.LockShort.Length > 0 ? "\u0001" + lk.LockShort : ""); if (lk.Doer >= 0) faces.Add(lk.Doer);
                    }
                    else if (on != null && h.TrafficPill(on) is { Length: > 0 } tp)
                    {   // brief 098 step 1: a passing car with no way offered on it: what it is and what it is doing, in plain words (HostTraffic.TrafficPill); the car icon, nothing to hold
                        state = "info"; t = on; icon = "car"; text = tp;
                    }
                }
            }
            Vector3? at = null;
            if (text.Length > 0) { if (anchorId.Length > 0) at = h.DrawnAt(anchorId); else if (t != null) at = h.TargetWorld(t); }
            Vector2 q = default; bool show = at.HasValue && Proj(at.Value + Vector3.up * (t == null || t.Kind is "person" or "crowd" or "hostile" or "bystander" or "patrol" or "rivals" or "security" or "unit" ? 2.25f : 1.5f), out q);
            // Glyph's N9 (0.2.122): on a camera with a window marker (the loop's clock, the desk's) or a dead disc, the pill's tail stands 6 px above
            // that element, so the order reads pill, marker or disc, camera head (it covered "1:59", the desk's "14" and the disc)
            if (show) { string camKey = t?.Kind == "camera" ? "camera/" + t.Key : anchorId.StartsWith("camera/") ? anchorId : anchorId.StartsWith("camera:") ? "camera/" + anchorId[7..] : null; if (camKey != null) q.y = Mathf.Min(q.y, CameraMarkTop(camKey) - 6f); }
            if (show) q.y = Mathf.Min(q.y, ReticleTop(anchorId.Length > 0 ? anchorId : h.FireIdFor(t)) - 6f); // brief 067 D (Glyph): the pill's tail 6 px above a lock reticle
            Show(vpill, show); Show(vpillTail, show); PillState = show ? state : ""; if (!show) { Show(loudRow, false); CancelTagFrame("", null, false); PipShown = false; SweepShownNow = -1f; StackTabFrame(""); pillWas = ("", null); pillQWas = null; return; }
            PillLook(text, icon, harm, faces, seen, fill, state);
            // Glyph 067 E3: a lock that lapses under the cursor turns the locked pill into the offer with a 0.15 s cross-fade (the new pill comes
            // up from the locked one's dimness); F9: a step through the stack (the wheel, Z, Tab) glides the pill's tail to the next thing in 0.12 s
            float nowP = Time.unscaledTime;
            if (pillWas.state == "lock" && state == "offer" && t != null && pillWas.t != null && t.Kind == pillWas.t.Kind && t.Key == pillWas.t.Key) lockLapseAt = nowP;
            if (h.StackSteps != stepsWas && pillQWas.HasValue) { glideFrom = pillQWas.Value; glideAt = nowP; }
            stepsWas = h.StackSteps; pillWas = (state, t);
            float lapse = Mathf.Clamp01((nowP - lockLapseAt) / 0.15f), lapseA = state == "offer" && lapse < 1f ? Mathf.Lerp(LockPillAlpha * 0.45f, 1f, 1f - (1f - lapse) * (1f - lapse)) : 1f;
            vpill.style.opacity = state is "charge" or "ready" ? h.HoldDim : state == "lock" ? LockPillAlpha : lapseA; vpillTail.style.opacity = vpill.style.opacity; // the hold dims as the cursor pulls away (it cancels at 80 px); brief 067 E3: the locked pill at 80 % (nothing to hold)
            // brief 101 (Sky 2026-11-08, "I agree": no hit chance in play): the fire chip's Alt breakdown and the rested price's sum are gone; Alt keeps the lock's long line
            LockAltFrame(state == "lock" && h.Peeking ? (h.HoverIntent?.LockLong ?? "") : "");
            vpill.style.scale = state == "ready" ? new Scale(new Vector3(ReadyScale, ReadyScale, 1f)) : new Scale(Vector3.one); // full: the pill pops while it says "Let go"
            float glide = Mathf.Clamp01((nowP - glideAt) / 0.12f); if (glide < 1f) q = Vector2.Lerp(glideFrom, q, 1f - (1f - glide) * (1f - glide));
            pillQWas = q;
            PillPlace(q, out _);
            StackTabFrame(state); // brief 067 F9 (Glyph): two or more under the cursor: the stack tab on the pill, a dashed ring at the others' feet
            // brief 043: the cancel tag over the pill, and the lit pip of an act that just opened
            bool fireish = state == "fire" || h.Charge is { Fire: true } || state == "offer" && h.HoverFire;
            CancelTagFrame(state, faces, fireish);
            Order po = state == "offer" ? h.HoverIntent?.Order : state is "charge" or "ready" && h.Charge is { Fire: false, Plan: not null } pc ? pc.Plan.Row(pc.Way)?.I?.Order : null;
            PipShown = po != null && t != null && h.Fresh(t, po);
            Show(vpillPip, PipShown); // on the frame's top-right chrome corner
        }

        // ---------------------------------------------------------------- the street marks

        const float GhostH = 24f;
        /// <summary>The proof reads it: where the Squad / Back to map button is on screen (pixels, origin bottom left), null when hidden.</summary>
        public static Vector2? SquadBtnScreen { get { var b = live?.squadBtn; if (b == null || b.resolvedStyle.display == DisplayStyle.None) return null; var c = b.worldBound.center; var sz = b.panel.visualTree.layout.size; return new Vector2(c.x / sz.x * Screen.width, Screen.height - c.y / sz.y * Screen.height); } }
        KeyText squadK; VisualElement squadBtn; readonly List<(VisualElement root, VisualElement face, KeyText text)> ghostEls = new(); 
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
                squadBtn = Tab(root, SquadD, "units", out _); squadBtn.style.position = Position.Absolute; squadBtn.style.left = SquadX; squadBtn.style.top = SquadY; // brief 101 step 4: the street tab under the faces (the mock's)
                squadK = KText(root, 14f, "k-semi"); squadK.Root.style.position = Position.Absolute; squadK.Root.style.left = SquadX + SquadD + 8f; squadK.Root.style.top = SquadY + SquadD / 2f - 11f; squadK.Root.pickingMode = PickingMode.Position; manifest.Add((squadK.Root, "image", () => "squad button word")); // the button's own word (it was inside the old plate): art, as the button
                foreach (var sb in new[] { squadBtn, squadK.Root }) sb.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { if (host.Following) host.BackToMap(); else host.SelectAll(); e.StopPropagation(); } }); // brief 083: in third person this slot is the way back (the tab and its word)
                manifest.Add((squadBtn, "image", () => "squad button"));
            }
            Show(squadBtn, on && m.Dock == RosterDock.Left && !h.FollowExiting);
            { bool back = h.Following && squadBtn.style.display != DisplayStyle.None; Show(squadK.Root, back); squadK.Set(back ? $"{h.BackToMapWord} · {h.controls.Label(PlayAction.FollowView)}" : ""); } // brief 083: third person names the way back beside the tab
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
                        // brief 101 round 3 (Glyph's rule 1): the ghost is a street label: the doer's face in the cream ring, then the word (Jost Bold 15.5, ink on the cream keyline)
                        var r = StackRow(marks, GhostH); r.style.position = Position.Absolute; r.style.justifyContent = Justify.FlexStart;
                        var f = SFace(r, 16f, 2f); var l = KText(r, 15.5f); l.Root.style.marginLeft = 6;
                        ghostEls.Add((r, f, l)); // one or two words: no manifest line (the budget's line is the objective)
                    }
                    var (gr, gf, gl) = ghostEls[gu++]; Show(gr, true); gl.Set(it.Label); CameoOf(gf, it.K);
                    float gw = float.IsNaN(gr.resolvedStyle.width) ? 160 : gr.resolvedStyle.width; gr.style.left = gq.x - gw / 2; gr.style.top = gq.y - GhostH - 8;
                }
                AttentionClocks(g, ref cu); // the attention clock over an agent standing in a watcher's sight (brief 101 round 3: a timed tab, HudToolkit.Timed)
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
            if (!h.OneInput) UnderFireMarks(); // engine 0.35 (Glyph b-final, cut): no under-fire arcs; the portrait's under-fire state says it
            KnownFaceMarks(on); LockReticles(on); // brief 067 D: the lock reticles over the fire orders' targets
            CoverSnapMarks(on); // engine 0.35: the snapped move ring's shield, and the struck shield over a flanked focus's cover
            for (int i = gu; i < ghostEls.Count; i++) Show(ghostEls[i].root, false);
            for (int i = cu; i < clockEls.Count; i++) Show(clockEls[i].Root, false);
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
                freeBtn = E(root, "s-plate_dark", SquadX + SquadD + 8f, SquadY, 38, 36, pick: true); // the glyph 4 px clear of plate_dark's rim (10.2 px in); beside the Squad tab
                freeIcon = E(freeBtn, null, 12.5f, 11.5f, 13, 13); // placed: 4 px clear of the rim (10.2 px in)

                freeBtn.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ToggleFree(); e.StopPropagation(); } });
                manifest.Add((freeBtn, "image", () => "weapons free toggle"));
                // the word on its own plate_dark (review 9: bare cream text read 1.3:1 on the pale ground)
                freePlate = PlateDark(root, 30f, 0.3f); freePlate.style.position = Position.Absolute; freePlate.style.left = SquadX + SquadD + 52f; freePlate.style.top = SquadY + 3f; freePlate.style.paddingLeft = 13; freePlate.style.paddingRight = 13;
                freeWord = Words(freePlate, 13f); freeWord.style.marginBottom = 1;
                manifest.Add((freePlate, "line", () => freeWord.text));
            }
            bool fon = h.Game != null && h.Game.WeaponsFree; show &= !h.OneInput; Show(freeBtn, show && h.Game != null); // engine 0.35: weapons free is always on: no toggle
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
                bool show = known != null && System.Linq.Enumerable.Contains(known, Op.Name((OpId)k)) && PortraitScreen(k) is Vector2 && !(colWins[k]?.Street == true && m.Dock == RosterDock.Left); // the faces carry their own badge (FacesFrame)
                if (knownEls[k] == null) { if (!show) continue; knownEls[k] = E(root, null, 0, 0, 20, 20); Round(knownEls[k], 10); knownEls[k].style.backgroundColor = Ink; Border(knownEls[k], 1.5f, Cream); var ic = E(knownEls[k], null, 2, 2, 16, 16); ic.name = "g"; manifest.Add((knownEls[k], "image", () => "face known badge")); }
                Show(knownEls[k], show); if (!show) continue;
                var ps = PortraitScreen(k).Value; var pp = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(ps.x, Screen.height - ps.y));
                knownEls[k].style.left = pp.x - 40; knownEls[k].style.top = pp.y - 40; Icon(knownEls[k].Q("g"), "sawyou", Cream);
            }
        }
    }
}
