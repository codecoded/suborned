using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;
using Face = Suborned.Play.HandoverHost.Face;

namespace Suborned.Play
{
    /// <summary>Three of Sky's 2026-10-21 decisions on the play HUD.
    ///  · Brief 030, option A (briefs/030/plate.html): no action plate. Its jobs move: the armed verb rides the cursor (a 24 px glyph disc) and
    ///    sits on the selected portrait's lower right, and the ring opens on its wedge; Tab or Shift shows a 4-glyph strip beside the selected
    ///    portrait for 2.5 s (the armed slot lit, the ⇧ caps, dimmed while a driven agent moves); target mode (Y) is a crosshair cursor and a
    ///    pip pill by the clock (one pip per queued shot); the freeze queue docks beside the portraits; the stance is a fan on the drawn
    ///    sidearm mark's hover (hold, return, at will; a stance other than return shows inside the mark). Keys unchanged. The pause menu's
    ///    Action plate setting brings the plate back (PlayControls.actionPlate).
    ///  · Brief 033 (Forma's stills, Glyph's cues briefs/033/cues.html): the portraits swap faces with a 0.15 s cross-fade by the host's state
    ///    (HostPortraits), and one vector layer per portrait carries what the face can't at 64 px: an orbit arc (focused), a ripple and the eye
    ///    badge (noticed, ticks by stage), spikes jittering (under fire), rays at the sidearm (firing), a crack, a 2 px shake and the lost pip
    ///    flashing (hurt), a dashed beating ring and pulsing pips (badly hurt), a slash and a dark ring (downed), one sweep and three ticks
    ///    (success). Calm draws nothing.
    ///  · Brief 032: the conversation scene's HUD: the clock pill, one bubble at its speaker, a row of dots, the Skip keycap, the letterbox
    ///    bars and the dip; everything else hides.
    /// View only.</summary>
    public sealed partial class HudToolkit
    {
        bool PlateOn => host.controls.actionPlate;
        static readonly Color Pale = new(1f, 0.914f, 0.659f), Alarm = new(1f, 0.353f, 0.235f), DownRing = new(0.231f, 0.29f, 0.278f), DiscTeal = new(0.086f, 0.4f, 0.369f);
        const float CueS = 39f / 32f; // the cue board's 64 px portrait (ring r 31-32) to the roster's photo (r 39)
        const float FadeS = 0.15f;

        // ---------------------------------------------------------------- brief 033: the faces

        /// <summary>The live face for agent i (false: the stills aren't imported; the caller draws the kit's looks).</summary>
        bool LivePortrait(int i, Window w, HudModel.Slot s, bool any)
        {
            var face = host.FaceOf(i); string file = $"portraits/live/{s.Role}_{HandoverHost.FaceFiles[(int)face]}";
            var sp = Sp(file); if (sp == null) { Show(w.Next, false); Show(w.Cue, false); Show(w.Eye, false); return false; }
            float now = Time.unscaledTime;
            w.Clip.style.backgroundColor = DiscTeal; // one disc colour behind the cut-outs (Forma: the grounds differ a little per agent)
            if (w.Face.Length == 0) { Bg(w.Portrait, file); w.Face = file; }
            else if (w.Face != file) { w.Next.style.backgroundImage = new StyleBackground(sp); w.Next.style.opacity = 0f; Show(w.Next, true); w.FadeAt = now; w.Face = file; }
            if (w.Next.style.display != DisplayStyle.None)
            {
                float t = (now - w.FadeAt) / FadeS; w.Next.style.opacity = Mathf.Clamp01(t);
                if (t >= 1f) { Bg(w.Portrait, w.Face); Show(w.Next, false); }
            }
            var tint = any && !s.Selected ? new Color(0.62f, 0.62f, 0.62f) : Color.white; // the others dim while one is selected (the kit's dim look)
            w.Portrait.style.unityBackgroundImageTintColor = tint; w.Next.style.unityBackgroundImageTintColor = tint;
            // hurt: the face shakes 2 px once on the hit
            float ht = now - host.HitAt(i); float dx = ht >= 0f && ht < 0.3f && face is Face.Hurt or Face.BadlyHurt or Face.UnderFire ? 2f * Mathf.Sin(ht * 60f) * (1f - ht / 0.3f) : 0f;
            var tr = new Translate(dx, 0); w.Portrait.style.translate = tr; w.Next.style.translate = tr;
            // the cue layer: redrawn while its state moves
            if (w.CueFace != face) { w.CueFace = face; w.CueSince = host.FaceSince(i); }
            Show(w.Cue, !m.Ended || face == Face.Downed); w.Cue.MarkDirtyRepaint();
            bool eye = host.EyeStage(i) >= 1 && !s.InCar && !s.Down && !m.Ended; Show(w.Eye, eye); if (eye) w.Eye.MarkDirtyRepaint();
            // brief 030 (no plate): the armed verb's glyph on the selected portrait
            int pk = host.Picked; bool armed = !PlateOn && pk >= 0 && pk < 5 && i == m.Agent && s.Selected && m.VerbHas[pk] && !m.Ended;
            Show(w.Glyph, armed);
            if (armed) { Icon(w.Glyph.Q("g"), ArmedIcon(), Cream); w.Glyph.style.opacity = host.PlateKeysDim ? 0.4f : 1f; }
            // the stance inside the drawn mark when it is not the default (return)
            bool showStance = s.Armed && s.Drawn && host.Game.Ops[i].Gun.Stance != "return";
            if (w.GunIcon != null) Show(w.GunIcon, !showStance); Show(w.StanceIn, showStance); if (showStance) w.StanceIn.MarkDirtyRepaint();
            return true;
        }

        /// <summary>The armed verb's glyph (the takedown's way, not the verb: Glyph §1).</summary>
        string ArmedIcon() => m.TakedownArmed ? (m.Way == "kill" ? "way_kill" : "way_knock") : m.VerbIcon[Mathf.Clamp(host.Picked, 0, 4)];

        /// <summary>The pips (HudToolkit.Pips) under the face: on a hit the lost one flashes alarm-orange for 0.6 s; badly hurt, the rest pulse.</summary>
        void LivePips(int i, Window w, HudModel.Slot s)
        {
            if (host.Game == null) return; float now = Time.unscaledTime; var face = host.FaceOf(i);
            float ht = now - host.HitAt(i); int lost = host.LostPip(i); bool flash = ht >= 0f && ht < HandoverHost.HitFlashS && Mathf.Repeat(ht, 0.2f) < 0.12f;
            float pulse = face == Face.BadlyHurt ? 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Cos(now * Mathf.PI * 2f)) : 1f;
            for (int k = 0; k < w.PipDots.Count; k++)
            {
                var d = w.PipDots[k]; bool full = k < s.Hp;
                d.style.unityBackgroundImageTintColor = flash && k == lost ? Alarm : Color.white;
                if (flash && k == lost) Bg(d, "markers/pip_full");
                d.style.opacity = full ? pulse : 1f;
            }
        }

        static Vector2 At(float bx, float by) => new(60f + (bx - 32f) * CueS, 60f + (by - 32f) * CueS); // a board point (64 px space) in the cue layer

        static void Stroke2(Painter2D p, Action path, Color c, float w, float under = 2.5f)
        {
            p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            p.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.85f); p.lineWidth = w + under; p.BeginPath(); path(); p.Stroke();
            p.strokeColor = c; p.lineWidth = w; p.BeginPath(); path(); p.Stroke();
        }

        /// <summary>One portrait's cue (Glyph's board §2), in the cue layer's 120 px box centred on the photo.</summary>
        void CuePaint(Window w, Painter2D p)
        {
            if (host?.Game == null) return; var c = new Vector2(60, 60); float now = Time.unscaledTime, t = now - w.CueSince, s = CueS;
            switch (w.CueFace)
            {
                case Face.Focused:
                    {   // a short arc orbits the ring, 2 s a turn
                        float a0 = -90f + Mathf.Repeat(t / 2f, 1f) * 360f;
                        Stroke2(p, () => p.Arc(c, 36f * s, Deg(a0), Deg(a0 + 51f)), Cream, 3f * s);
                        break;
                    }
                case Face.Noticed:
                    {   // a ripple at each step up (the eye badge says the stage)
                        float rt = now - Mathf.Max(host.EyeUpAt(w.Index), w.CueSince); if (rt < 0f || rt > 1f) break;
                        float r = (33f + 8f * rt) * s; var col = new Color(Pale.r, Pale.g, Pale.b, 1f - rt);
                        p.strokeColor = col; p.lineWidth = 3f * s * (1f - 0.5f * rt); p.BeginPath(); p.Arc(c, r, Deg(0), Deg(360)); p.Stroke();
                        break;
                    }
                case Face.UnderFire:
                    {   // the ring is ringed by 8 short spikes that jitter at 4 Hz
                        int step = Mathf.FloorToInt(now * 4f); float jit = ((step * 7919) % 5 - 2) * 2.2f;
                        for (int i = 0; i < 8; i++)
                        {
                            float a = (i * 45f + 22.5f + jit) * Mathf.Deg2Rad; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); float r0 = 36f * s + ((step + i) % 2) * 0.8f;
                            Stroke2(p, () => { p.MoveTo(c + d * r0); p.LineTo(c + d * 41f * s); }, Cream, 3f * s);
                        }
                        break;
                    }
                case Face.Firing:
                    {   // three rays at the sidearm corner, flickering at 2 Hz
                        if (Mathf.Repeat(now * 2f, 1f) > 0.7f) break;
                        Stroke2(p, () => { p.MoveTo(At(2, 54)); p.LineTo(At(-6, 58)); p.MoveTo(At(4, 60)); p.LineTo(At(-2, 68)); p.MoveTo(At(12, 64)); p.LineTo(At(10, 73)); }, Pale, 2.6f * s);
                        break;
                    }
                case Face.Hurt:
                    {   // on the hit: a crack across the rim for 0.6 s (the alarm orange only here, where the shape says it already)
                        float ht = now - host.HitAt(w.Index); if (ht < 0f || ht > HandoverHost.HitFlashS) break;
                        Stroke2(p, () => { p.MoveTo(At(52, 8)); p.LineTo(At(49, 13)); p.LineTo(At(54, 15)); p.LineTo(At(50, 21)); p.LineTo(At(55, 23)); }, Alarm, 2.5f * s);
                        break;
                    }
                case Face.BadlyHurt:
                    {   // a dashed ring beating at 1 Hz (thickness swell)
                        float beat = 0.5f + 0.5f * Mathf.Cos(now * Mathf.PI * 2f), wd = (2.2f + 1.4f * beat) * s;
                        float r = 31f * s, circ = 2f * Mathf.PI * r, dash = 7f * s, gap = 5f * s; int n = Mathf.Max(6, Mathf.RoundToInt(circ / (dash + gap)));
                        for (int pass = 0; pass < 2; pass++)
                        {
                            p.strokeColor = pass == 0 ? new Color(Ink.r, Ink.g, Ink.b, 0.7f) : TurqLight; p.lineWidth = pass == 0 ? wd + 2.5f : wd; p.lineCap = LineCap.Butt;
                            for (int i = 0; i < n; i++) { float a0 = i * 360f / n; p.BeginPath(); p.Arc(c, r, Deg(a0), Deg(a0 + 360f / n * dash / (dash + gap))); p.Stroke(); }
                        }
                        break;
                    }
                case Face.Downed:
                    {   // the ring solid dark, a slash across the face
                        p.strokeColor = DownRing; p.lineWidth = 3f * s; p.BeginPath(); p.Arc(c, 31f * s, Deg(0), Deg(360)); p.Stroke();
                        p.lineCap = LineCap.Round; p.strokeColor = Ink; p.lineWidth = 8f * s; p.BeginPath(); p.MoveTo(At(8, 56)); p.LineTo(At(56, 8)); p.Stroke();
                        p.strokeColor = Cream; p.lineWidth = 3.5f * s; p.BeginPath(); p.MoveTo(At(8, 56)); p.LineTo(At(56, 8)); p.Stroke();
                        break;
                    }
                case Face.Success:
                    {   // one bright arc sweeps the ring once (0.6 s), three short ticks at the top for the beat
                        if (t < 0.6f) { float a0 = -90f + t / 0.6f * 360f; Stroke2(p, () => p.Arc(c, 36f * s, Deg(a0), Deg(a0 + 65f)), Color.white, 3.2f * s, 2.8f); }
                        Stroke2(p, () => { foreach (var deg in new[] { -70f, -50f, -30f }) { var d = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)); p.MoveTo(c + d * 37f * s); p.LineTo(c + d * (deg == -50f ? 43f : 41f) * s); } }, Cream, 2.6f * s); // three ticks at the upper right, clear of the portrait above
                        break;
                    }
            }
        }

        /// <summary>The attention eye badge (the brief 031 callout's eye): a dark pill with the eye, 1 to 3 ticks over it by the stage.</summary>
        void EyePaint(Window w, Painter2D p)
        {
            if (host?.Game == null) return; int n = Mathf.Clamp(host.EyeStage(w.Index), 1, 3); float s = CueS * 0.95f;
            var o = new Vector2(1, 6); float bw = 26f * s, bh = 16f * s, r = bh / 2f;
            p.fillColor = Ink; p.BeginPath(); p.MoveTo(o + new Vector2(r, 0)); p.LineTo(o + new Vector2(bw - r, 0)); p.Arc(o + new Vector2(bw - r, r), r, Deg(-90), Deg(90)); p.LineTo(o + new Vector2(r, bh)); p.Arc(o + new Vector2(r, r), r, Deg(90), Deg(270)); p.ClosePath(); p.Fill();
            p.strokeColor = Cream; p.lineWidth = 1.2f; p.Stroke();
            var e = o + new Vector2(bw / 2f, bh / 2f); float ew = 8f * s, eh = 4.2f * s;
            p.lineWidth = 1.5f; p.BeginPath(); p.MoveTo(e - new Vector2(ew, 0)); p.QuadraticCurveTo(e + new Vector2(0, -eh * 1.5f), e + new Vector2(ew, 0)); p.QuadraticCurveTo(e + new Vector2(0, eh * 1.5f), e - new Vector2(ew, 0)); p.Stroke();
            p.fillColor = Cream; p.BeginPath(); p.Arc(e, 2.3f * s, Deg(0), Deg(360)); p.Fill();
            p.fillColor = Pale; for (int i = 0; i < n; i++) { float x = e.x - (n - 1) * 2.6f * s + i * 5.2f * s; p.BeginPath(); p.MoveTo(new Vector2(x - 1.5f, 0)); p.LineTo(new Vector2(x + 1.5f, 0)); p.LineTo(new Vector2(x + 1.5f, 5f)); p.LineTo(new Vector2(x - 1.5f, 5f)); p.ClosePath(); p.Fill(); }
        }

        /// <summary>A stance glyph: pause bars (hold), a turn-back arrow (return), a crosshair (at will).</summary>
        static void StanceGlyph(Painter2D p, Vector2 c, float r, string stance, Color col)
        {
            p.strokeColor = col; p.fillColor = col; p.lineWidth = Mathf.Max(1.4f, r * 0.32f); p.lineCap = LineCap.Round;
            switch (stance)
            {
                case "hold":
                    foreach (var x in new[] { -0.45f, 0.45f }) { p.BeginPath(); p.MoveTo(c + new Vector2(x * r - r * 0.18f, -r * 0.75f)); p.LineTo(c + new Vector2(x * r + r * 0.18f, -r * 0.75f)); p.LineTo(c + new Vector2(x * r + r * 0.18f, r * 0.75f)); p.LineTo(c + new Vector2(x * r - r * 0.18f, r * 0.75f)); p.ClosePath(); p.Fill(); }
                    break;
                case "free":
                    p.BeginPath(); p.Arc(c, r * 0.55f, Deg(0), Deg(360)); p.Stroke();
                    p.BeginPath(); p.MoveTo(c + new Vector2(0, -r)); p.LineTo(c + new Vector2(0, -r * 0.35f)); p.MoveTo(c + new Vector2(0, r * 0.35f)); p.LineTo(c + new Vector2(0, r)); p.MoveTo(c + new Vector2(-r, 0)); p.LineTo(c + new Vector2(-r * 0.35f, 0)); p.MoveTo(c + new Vector2(r * 0.35f, 0)); p.LineTo(c + new Vector2(r, 0)); p.Stroke();
                    break;
                default: // return: a turn-back arrow
                    p.BeginPath(); p.Arc(c, r * 0.7f, Deg(-200), Deg(60)); p.Stroke();
                    { var tip = c + new Vector2(Mathf.Cos(-200f * Mathf.Deg2Rad), Mathf.Sin(-200f * Mathf.Deg2Rad)) * r * 0.7f; p.BeginPath(); p.MoveTo(tip + new Vector2(-r * 0.05f, -r * 0.55f)); p.LineTo(tip); p.LineTo(tip + new Vector2(r * 0.55f, -r * 0.1f)); p.Stroke(); }
                    break;
            }
        }

        // ---------------------------------------------------------------- brief 030: the strip, the cursor badge, the stance fan, the target pill

        VisualElement strip, fan, cursorArmed, targetPill; Paint cursorCross, targetPips; readonly List<(VisualElement slot, VisualElement icon, VisualElement cap, Label key)> stripSlots = new();
        readonly List<VisualElement> fanDiscs = new(); int fanAgent = -1; bool fanArm, fanOver; float fanLeft = -10f; int targetShots;
        const float StripSlot = 40f, StripGap = 6f, StripPad = 11f, StripW = StripPad * 2 + StripSlot * 4 + StripGap * 3, StripH = StripPad * 2 + StripSlot;
        static readonly string[] StanceOrder = { "hold", "return", "free" };

        void LiveBuild()
        {
            if (strip != null) return;
            strip = E(root, "s-plate_dark", 0, 0, StripW, StripH); strip.pickingMode = PickingMode.Ignore;
            for (int i = 0; i < 4; i++)
            {
                var sl = E(strip, null, StripPad + i * (StripSlot + StripGap), StripPad, StripSlot, StripSlot); Round(sl, 7);
                var ic = E(sl, null, 10, 10, 20, 20);
                var cap = E(sl, "s-plate_light", -2, -3, float.NaN, 14); cap.style.minWidth = 18; var k = L(cap, "keycap"); k.style.fontSize = 9; k.style.paddingLeft = 3; k.style.paddingRight = 3; k.style.unityTextAlign = TextAnchor.MiddleCenter;
                stripSlots.Add((sl, ic, cap, k));
            }
            manifest.Add((strip, "image", () => "verb strip")); Show(strip, false);
            // the stance fan: three glyph discs right of the drawn sidearm mark
            fan = E(root, "s-plate_dark", 0, 0, 12 + 3 * 30 + 2 * 6 + 12, 46, pick: true); fan.style.flexDirection = FlexDirection.Row; fan.style.alignItems = Align.Center; fan.style.paddingLeft = 12; fan.style.paddingRight = 12;
            fan.RegisterCallback<PointerEnterEvent>(_ => fanOver = true); fan.RegisterCallback<PointerLeaveEvent>(_ => { fanOver = false; fanLeft = Time.unscaledTime; });
            for (int i = 0; i < 3; i++)
            {
                string st = StanceOrder[i]; var d = E(fan, null, w: 30, h: 30, pick: true); Round(d, 15); if (i > 0) d.style.marginLeft = 6;
                P(d, 0, 0, 30, 30, (p, r) => StanceGlyph(p, new Vector2(15, 15), 8.5f, st, Cream));
                d.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && fanAgent >= 0) { host.SetStanceAgent(fanAgent, st); e.StopPropagation(); } });
                fanDiscs.Add(d);
            }
            manifest.Add((fan, "image", () => "stance fan")); Show(fan, false);
            // the armed verb at the cursor (24 px), and target mode's crosshair
            cursorArmed = E(root, null, 0, 0, 24, 24); Round(cursorArmed, 12); Border(cursorArmed, 2f, TurqLight); cursorArmed.style.backgroundColor = DiscTeal; E(cursorArmed, null, 4.5f, 4.5f, 15, 15).name = "g";
            manifest.Add((cursorArmed, "image", () => "armed verb at the cursor")); Show(cursorArmed, false);
            cursorCross = P(root, 0, 0, 40, 40, (p, r) => { var c = new Vector2(20, 20); for (int pass = 0; pass < 2; pass++) { p.strokeColor = pass == 0 ? Ink : Color.white; p.lineWidth = pass == 0 ? 3.2f : 1.5f; p.BeginPath(); p.Arc(c, 13f, Deg(0), Deg(360)); p.Stroke(); p.lineWidth = pass == 0 ? 3f : 1.5f; p.BeginPath(); p.MoveTo(c + new Vector2(0, -19)); p.LineTo(c + new Vector2(0, -8)); p.MoveTo(c + new Vector2(0, 8)); p.LineTo(c + new Vector2(0, 19)); p.MoveTo(c + new Vector2(-19, 0)); p.LineTo(c + new Vector2(-8, 0)); p.MoveTo(c + new Vector2(8, 0)); p.LineTo(c + new Vector2(19, 0)); p.Stroke(); } });
            manifest.Add((cursorCross, "image", () => "target crosshair")); Show(cursorCross, false);
            // target mode's pill by the clock: the crosshair glyph and a pip per queued shot
            targetPill = E(root, "s-plate_dark", 0, 0, float.NaN, ChipH); targetPill.style.flexDirection = FlexDirection.Row; targetPill.style.alignItems = Align.Center; targetPill.style.paddingLeft = ChipPad; targetPill.style.paddingRight = ChipPad;
            var tc = P(targetPill, 0, 0, Glyph, Glyph, (p, r) => Crosshair(p, new Vector2(Glyph / 2, Glyph / 2), 5.6f, Cream)); tc.style.position = Position.Relative; tc.style.left = StyleKeyword.Auto; tc.style.top = StyleKeyword.Auto; tc.style.flexShrink = 0;
            targetPips = P(targetPill, 0, 0, 10, Glyph, (p, r) => { for (int j = 0; j < Mathf.Max(1, targetShots); j++) { var c = new Vector2(4f + j * 10f, r.height / 2); p.BeginPath(); p.Arc(c, 3.6f, Deg(0), Deg(360)); if (j < targetShots) { p.fillColor = Cream; p.Fill(); } else { p.strokeColor = Cream; p.lineWidth = 1.2f; p.Stroke(); } } });
            targetPips.style.position = Position.Relative; targetPips.style.left = StyleKeyword.Auto; targetPips.style.top = StyleKeyword.Auto; targetPips.style.marginLeft = 10; targetPips.style.flexShrink = 0;
            manifest.Add((targetPill, "image", () => "target pill")); Show(targetPill, false);
        }

        /// <summary>The strip shows now (Tab or Shift within 2.5 s), beside the selected portrait.</summary>
        bool StripOn => !PlateOn && strip != null && host.Game != null && m.Dock == RosterDock.Left && !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None && !host.ConvoOn && Time.unscaledTime - host.VerbStripAt < HandoverHost.VerbStripS && m.Slots[m.Agent].Show;

        /// <summary>After the less-reading frame: the strip, the cursor's badge or crosshair, the fan, the target pill; the plate hidden.</summary>
        void LiveFrame(float W)
        {
            LiveBuild();
            if (!PlateOn) { Show(verbPlate, false); Show(armedPlate, false); Show(wayRow, false); if (plateStub != null) Show(plateStub, false); }
            float now = Time.unscaledTime;
            // the strip: four glyphs, the armed one lit, the ⇧ caps (dim while a driven agent moves); fades in its last 0.3 s
            bool so = StripOn; Show(strip, so);
            if (so)
            {
                int a = m.Agent; float colW = 109 * RosterScale, mid = ColY + ColumnBezels[a].y * RosterScale;
                strip.style.left = ColX + colW + 4; strip.style.top = mid - StripH / 2; strip.BringToFront();
                strip.style.opacity = Mathf.Clamp01((HandoverHost.VerbStripS - (now - host.VerbStripAt)) / 0.3f);
                for (int k = 0; k < 4; k++)
                {
                    var (sl, ic, cap, key) = stripSlots[k]; bool has = m.VerbHas[k], refused = m.VerbRefused[k], lit = has && host.Picked == k;
                    Show(ic, has || refused); if (has || refused) Icon(ic, m.VerbIcon[k], has ? Cream : Grey);
                    sl.style.backgroundColor = lit ? DiscTeal : new Color(0.03f, 0.11f, 0.11f, 0.55f); Border(sl, lit ? 2.2f : 1f, lit ? TurqLight : new Color(0.51f, 0.627f, 0.612f, 0.7f));
                    sl.style.opacity = has ? 1f : refused ? 0.55f : 0.3f;
                    Text(key, m.VerbKey[k]); cap.style.opacity = host.PlateKeysDim ? 0.35f : 1f;
                }
            }
            // the cursor: the armed verb rides it (Fire's crosshair mark wins where a press fires); target mode: a crosshair
            var mp = RuntimePanelUtils.ScreenToPanel(root.panel, m.Mouse);
            bool live = host.Game != null && !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None && host.Ring == null && !host.ConvoOn && host.Panel == null;
            bool cross = !PlateOn && live && host.Frozen && host.TargetFreeze;
            bool badge = !PlateOn && live && !cross && host.Picked >= 0 && host.Picked < 5 && m.VerbHas[host.Picked] && host.CursorKind != "aim";
            Show(cursorArmed, badge); Show(cursorCross, cross);
            if (badge) { cursorArmed.style.left = mp.x + 14; cursorArmed.style.top = mp.y + 16; Icon(cursorArmed.Q("g"), ArmedIcon(), Cream); cursorArmed.style.opacity = host.PlateKeysDim ? 0.45f : 1f; cursorArmed.BringToFront(); Show(cursorMark, false); }
            if (cross) { cursorCross.style.left = mp.x - 20; cursorCross.style.top = mp.y - 20; cursorCross.BringToFront(); Show(cursorMark, false); }
            // target mode's pill by the clock (the top centre pill steps aside), pulsing while the mode is on
            bool tp = !PlateOn && host.Game != null && host.Game.Lab && host.Frozen && host.TargetFreeze && !m.Ended; Show(targetPill, tp);
            if (tp)
            {
                Show(flPill, false); int n = 0; for (int k = 0; k < 4; k++) n += Mathf.Max(0, host.VolleyShots[k]); n = Mathf.Min(n, 8);
                if (n != targetShots) { targetShots = n; targetPips.style.width = Mathf.Max(1, n) * 10f - 2f; } targetPips.MarkDirtyRepaint();
                var pb = pill.worldBound; float tw = targetPill.resolvedStyle.width; if (float.IsNaN(tw) || tw < 1) tw = 80;
                targetPill.style.left = pb.xMin - 8 - tw; targetPill.style.top = 14 + (44 - ChipH) / 2f; targetPill.style.opacity = 0.75f + 0.25f * (0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 2f));
            }
            // the stance fan: hovering a drawn sidearm mark (or the fan); a click picks
            bool fanOn = !PlateOn && fanAgent >= 0 && host.Game != null && !m.Ended && host.Modal == ModalGate.Modal.None && m.Slots[fanAgent].Armed && m.Slots[fanAgent].Drawn && !m.Slots[fanAgent].Down && (fanArm || fanOver || now - fanLeft < 0.3f);
            Show(fan, fanOn);
            if (fanOn)
            {
                var wins = m.Dock == RosterDock.Left ? colWins : rowWins; var ab = wins[fanAgent].Arm.worldBound; var at = root.WorldToLocal(ab);
                fan.style.left = at.xMax + 6; fan.style.top = at.center.y - 23; fan.BringToFront();
                string cur = host.Game.Ops[fanAgent].Gun.Stance;
                for (int i = 0; i < 3; i++) { bool on = StanceOrder[i] == cur; fanDiscs[i].style.backgroundColor = on ? DiscTeal : Color.clear; Border(fanDiscs[i], on ? 2f : 1f, on ? TurqLight : new Color(Cream.r, Cream.g, Cream.b, 0.45f)); }
            }
        }

        /// <summary>The busy chip of the selected agent stands after the strip while the strip shows (brief 030), else beside its portrait.</summary>
        float SelectedChipX(float plateX, bool folded) => PlateOn ? plateX + (folded ? StubW + 4 : PlateW + 16 + 6) : plateX + (StripOn ? StripW + 6 : 0f);

        // ---------------------------------------------------------------- brief 032: the conversation scene

        VisualElement convoBubble, convoTail, convoSkip, convoBarTop, convoBarBottom, convoVeil; Label convoText; Paint convoDots; bool convoHid;
        /// <summary>Elements that are not HUD for the budget (the letterbox bars, the dip): the HUD layer of a shot is drawn without them.</summary>
        readonly List<VisualElement> notHud = new();

        bool ConvoHud()
        {
            var c = host.ConvoNow; bool on = c != null;
            if (!on && !convoHid) return false;
            if (convoBubble == null)
            {
                convoBarTop = E(root); convoBarBottom = E(root); convoVeil = E(root);
                foreach (var b in new[] { convoBarTop, convoBarBottom, convoVeil }) { b.style.position = Position.Absolute; b.style.left = 0; b.style.right = 0; b.style.backgroundColor = Color.black; b.pickingMode = PickingMode.Ignore; notHud.Add(b); }
                convoBarTop.style.top = 0; convoBarTop.style.height = Length.Percent(6); convoBarBottom.style.bottom = 0; convoBarBottom.style.height = Length.Percent(6); convoVeil.style.top = 0; convoVeil.style.bottom = 0;
                convoBubble = E(root, "s-plate_light"); convoBubble.style.position = Position.Absolute; convoBubble.style.paddingLeft = 14; convoBubble.style.paddingRight = 14; convoBubble.style.paddingTop = 6; convoBubble.style.paddingBottom = 7;
                convoText = L(convoBubble, "card-body"); convoText.style.color = Ink; convoText.style.fontSize = 20;
                convoTail = E(convoBubble, null, 0, 0, 12, 12); convoTail.style.rotate = new Rotate(45); convoTail.style.backgroundColor = new Color(0.98f, 0.95f, 0.89f); convoTail.style.top = Length.Percent(100); convoTail.style.marginTop = -8; convoTail.style.left = Length.Percent(50); convoTail.style.marginLeft = -6;
                manifest.Add((convoBubble, "speech", () => convoText.text));
                convoDots = P(root, 0, 0, 60, 14, (p, r) => { var cc = host.ConvoNow; if (cc == null) return; int n = cc.Lines.Count; for (int i = 0; i < n; i++) { var q = new Vector2(r.width / 2 + (i - (n - 1) / 2f) * 14f, 7); p.BeginPath(); p.Arc(q, 4f, Deg(0), Deg(360)); if (i <= cc.Beat) { p.fillColor = Cream; p.Fill(); } p.strokeColor = Cream; p.lineWidth = 1.4f; p.Stroke(); } });
                manifest.Add((convoDots, "image", () => "scene beats"));
                convoSkip = E(root, "s-plate_light", pick: true); convoSkip.style.position = Position.Absolute; convoSkip.style.height = 28; convoSkip.style.flexDirection = FlexDirection.Row; convoSkip.style.alignItems = Align.Center; convoSkip.style.paddingLeft = 12; convoSkip.style.paddingRight = 10;
                var sk = L(convoSkip, "keycap"); sk.text = "Skip"; sk.style.fontSize = 14; var chev = E(convoSkip, null, w: 12, h: 12); chev.style.marginLeft = 6; Icon(chev, "chev_down", Ink); chev.style.rotate = new Rotate(new Angle(-90, AngleUnit.Degree));
                convoSkip.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ConvoSkip(); e.StopPropagation(); } });
                manifest.Add((convoSkip, "image", () => "skip keycap"));
            }
            var mine = new HashSet<VisualElement> { convoBarTop, convoBarBottom, convoVeil, convoBubble, convoDots, convoSkip, pill };
            if (on != convoHid)
            {
                convoHid = on;
                foreach (var ch in root.Children()) if (!mine.Contains(ch)) { ch.style.visibility = on ? Visibility.Hidden : StyleKeyword.Null; ch.style.opacity = on ? 0f : StyleKeyword.Null; } // opacity too: a descendant set visible (the pips, a chip's bar) shows through a hidden parent
                foreach (var e in mine) if (e != pill) Show(e, on);
                if (!on) { Show(pauseIcon, host.Paused); pauseIcon.style.opacity = 1f; return false; }
            }
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) ? 1920 : size.x, H = float.IsNaN(size.y) ? 1080 : size.y;
            // the clock with its paused mark, pulsing (the freeze is explained); the bars and the dip
            Text(clock, m.Clock); Show(pauseIcon, true); pauseIcon.style.opacity = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f));
            convoVeil.style.opacity = host.ConvoDip; Show(convoVeil, host.ConvoDip > 0.001f); convoVeil.BringToFront();
            bool shot = c.OutAt < 0f || host.ConvoDip > 0f && Time.unscaledTime - c.OutAt < HandoverHost.ConvoDipS * 0.5f;
            Show(convoBarTop, shot); Show(convoBarBottom, shot);
            // one bubble at its speaker (the tail says who; no names), whole, held up to bubbleSeconds; off the street, low on the near side
            bool speak = shot && c.Beat < c.Lines.Count && Time.unscaledTime >= c.BeatAt; Show(convoBubble, speak);
            if (speak)
            {
                var (who, line) = c.Lines[c.Beat]; Text(convoText, line);
                float bw = convoBubble.resolvedStyle.width, bh = convoBubble.resolvedStyle.height; if (float.IsNaN(bw) || bw < 1) bw = 280; if (float.IsNaN(bh) || bh < 1) bh = 36;
                Vector2 at = default; bool tail = host.ConvoHead(who) is Vector3 hw && Proj(hw, out at) && InView(at);
                if (!tail) at = new Vector2(W * 0.3f, H * 0.62f);
                float x = Mathf.Clamp(at.x - bw / 2, 24, W - bw - 24), y = Mathf.Clamp(at.y - bh - 10, H * 0.06f + 56, H * 0.94f - bh - 60);
                convoBubble.style.left = x; convoBubble.style.top = y; Show(convoTail, tail); if (tail) { convoTail.style.left = Mathf.Clamp(at.x - x, 16, bw - 16); convoTail.style.marginLeft = -6; }
                convoBubble.style.opacity = Mathf.Clamp01((Time.unscaledTime - c.BeatAt) / 0.12f);
            }
            // the row of dots under the pair (the played ones filled)
            Show(convoDots, shot && c.Lines.Count > 1);
            if (shot && host.ConvoFeet(c) is Vector3 fw && Proj(fw, out var fq)) { convoDots.style.left = Mathf.Clamp(fq.x - 30, 24, W - 84); convoDots.style.top = Mathf.Min(fq.y + 18, H * 0.94f - 30); convoDots.style.width = 60; convoDots.MarkDirtyRepaint(); }
            // Skip, bottom right over the bar's edge
            Show(convoSkip, shot); convoSkip.style.right = 40; convoSkip.style.bottom = H * 0.06f + 14;
            pill.BringToFront(); convoBubble.BringToFront(); convoSkip.BringToFront(); convoVeil.BringToFront(); // the clock over the top bar
            return true;
        }
    }
}
