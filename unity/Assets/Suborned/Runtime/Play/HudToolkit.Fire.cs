using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 024 "Firing" (Sky 2026-10-21: "it's not clear I was firing at the agent (or that it was 'on')"; mockup tabs 4 and 5,
    /// briefs/024/action-controls/): firing is visible whatever the control model, drawn as light on the HUD (outside the post grade), every mark
    /// read from the engine's own fire state (HandoverHost.FireNow, Keel's FireStateOf), so it starts and stops in the frame the engine does.
    ///  · the tracer: a dashed line marching from the muzzle to the target while the agent fires; thick for the driven agent, thin for an agent
    ///    returning fire; a muzzle flash and a hit spark per shot (the engine's shot log); hostile fire a solid streak (shape, not colour);
    ///  · the firing mark over the agent (the gun with rays), the return mark (the gun with a back arrow), the hollow follow mark;
    ///  · one line: "Firing at the guard · down in 3 s · held · X stop", then "Stopped" for 1 s;
    ///  · while aiming: the reticle with its band ticks (wider when the odds drop), the 0.5 s hold's ring, the 8 m follow ring on the ground;
    ///  · the third person key strip and the prompt's key caps (E, and the mouse when a left click acts);
    ///  · cars: smoke when disabled, flame while burning, the blast ring when one explodes (shapes only, no text).</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>Brief 024 rule 6: no pointer event keeps keyboard focus (WASD keeps working after any click).</summary>
        public static void BlurFocus() { var f = live?.root?.panel?.focusController?.focusedElement; if (f is Focusable fe) fe.Blur(); }

        /// <summary>For the proof: agent k's roster portrait centre in screen pixels (origin bottom left), else null.</summary>
        public static Vector2? PortraitScreen(int k)
        {
            if (live?.root?.panel == null || k < 0 || k > 3) return null;
            foreach (var set in new[] { live.colWins, live.rowWins })
            {
                var w = set?[k]; if (w?.Portrait == null || w.Portrait.resolvedStyle.display == DisplayStyle.None) continue;
                var b = w.Portrait.worldBound; if (b.width <= 0 || float.IsNaN(b.width)) continue;
                float k2 = live.doc != null ? live.doc.panelSettings.scale : 1f; var c = b.center * k2; return new Vector2(c.x, Screen.height - c.y);
            }
            return null;
        }

        Paint fireLayer; readonly List<Action<Painter2D>> firePaints = new();
        VisualElement fireLine, fireIcon, fireHeldIcon, fireCapX, fireCapLmb; Label fireHead, fireDown, fireHeld, fireStop;
        VisualElement tpStrip, chipCaps, chipMouse; bool fireBuilt;
        static readonly Color FlameA = new(1f, 0.62f, 0.18f), FlameB = new(1f, 0.86f, 0.42f), Smoke = new(0.42f, 0.45f, 0.45f);

        void BuildFire()
        {
            fireBuilt = true;
            fireLayer = P(marks, 0, 0, 0, 0, (p, r) => { foreach (var f in firePaints) f(p); });
            fireLayer.style.left = 0; fireLayer.style.top = 0; fireLayer.style.right = 0; fireLayer.style.bottom = 0; fireLayer.style.width = StyleKeyword.Auto; fireLayer.style.height = StyleKeyword.Auto;
            // the line: [gun] Firing at the guard · down in 3 s · [mouse] held · [X] stop
            fireLine = E(root, "s-plate_dark", 0, 0, float.NaN, 38); fireLine.style.flexDirection = FlexDirection.Row; fireLine.style.alignItems = Align.Center; fireLine.style.paddingLeft = 18; fireLine.style.paddingRight = 18; // clear of the plate's rounded caps (the layout audit's 4 px)
            fireLine.pickingMode = PickingMode.Ignore;
            fireCapLmb = P(fireLine, 0, 0, 14, 20, (p, r) => MouseGlyph(p, r.center, Cream, true)); fireCapLmb.style.position = Position.Relative; fireCapLmb.style.left = StyleKeyword.Auto; fireCapLmb.style.top = StyleKeyword.Auto; fireCapLmb.style.marginRight = 6;
            fireIcon = E(fireLine, null, w: 16, h: 16); fireIcon.style.marginRight = 6; // 11 px inside a 38 px plate: the audit's rim plus 4 px Icon(fireIcon, "fire", TurqLight);
            fireHead = L(fireLine, "verb-name"); fireHead.style.fontSize = 14f; fireHead.style.unityFontStyleAndWeight = FontStyle.Bold;
            fireDown = L(fireLine, "verb-name"); fireDown.style.fontSize = 13f; fireDown.style.marginLeft = 14;
            fireHeldIcon = P(fireLine, 0, 0, 14, 20, (p, r) => MouseGlyph(p, r.center, Cream, true)); fireHeldIcon.style.position = Position.Relative; fireHeldIcon.style.left = StyleKeyword.Auto; fireHeldIcon.style.top = StyleKeyword.Auto; fireHeldIcon.style.marginLeft = 14;
            fireHeld = L(fireLine, "verb-name"); fireHeld.style.fontSize = 13f; fireHeld.style.marginLeft = 4; fireHeld.text = "held";
            fireCapX = E(fireLine); fireCapX.style.flexDirection = FlexDirection.Row; fireCapX.style.alignItems = Align.Center; fireCapX.style.marginLeft = 10; KeyCap(fireCapX, host.controls.Label(PlayAction.Cease));
            fireStop = L(fireCapX, "verb-name"); fireStop.style.fontSize = 13f; fireStop.style.marginLeft = 4; fireStop.text = "stop";
            manifest.Add((fireLine, "image", () => "fire line: who, how long, the stop key"));
            Show(fireLine, false);
            // the third-person key strip (one row, one word per cap)
            tpStrip = E(root, "s-plate_dark", 0, 0, float.NaN, 34); tpStrip.style.flexDirection = FlexDirection.Row; tpStrip.style.alignItems = Align.Center; tpStrip.style.paddingLeft = 18; tpStrip.style.paddingRight = 10; tpStrip.pickingMode = PickingMode.Ignore;
            void Cap(string cap, string word) { KeyCapW(tpStrip, cap); var l = L(tpStrip, "verb-name"); l.text = word; l.style.fontSize = 12.5f; l.style.marginLeft = 5; l.style.marginRight = 10; }
            Cap("WASD", "move"); Cap(host.controls.Label(PlayAction.AddToGroup), "run"); Cap(host.controls.Label(PlayAction.Act), "act"); Cap("hold " + host.controls.Label(PlayAction.Act), "options");
            Cap("RMB", "aim"); Cap("LMB", "fire"); Cap(host.controls.Label(PlayAction.Draw), "draw");
            manifest.Add((tpStrip, "image", () => "third person key strip"));
            Show(tpStrip, false);
            // the prompt's caps at the head of the price chip: E, and the mouse when a left click on the prompt acts
            chipCaps = E(chip2); chipCaps.style.flexDirection = FlexDirection.Row; chipCaps.style.alignItems = Align.Center; chipCaps.style.marginRight = 8; chip2.Insert(0, chipCaps);
            KeyCap(chipCaps, host.controls.Label(PlayAction.Act)); chipCaps[0].style.marginLeft = 0;
            chipMouse = P(chipCaps, 0, 0, 14, 20, (p, r) => MouseGlyph(p, r.center, Cream, true)); chipMouse.style.position = Position.Relative; chipMouse.style.left = StyleKeyword.Auto; chipMouse.style.top = StyleKeyword.Auto; chipMouse.style.marginLeft = 5;
            Show(chipCaps, false);
        }

        /// <summary>A keycap that fits its label (the strip's "WASD", "hold E").</summary>
        void KeyCapW(VisualElement parent, string label)
        {
            var kp = E(parent, "s-plate_light", w: float.NaN, h: 19); kp.style.position = Position.Relative; kp.style.left = StyleKeyword.Auto; kp.style.top = StyleKeyword.Auto; kp.style.minWidth = 22; kp.style.flexShrink = 0; kp.style.paddingLeft = 4; kp.style.paddingRight = 4;
            var l = L(kp, "keycap"); l.text = label; l.style.flexGrow = 1; l.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        /// <summary>A mouse: a rounded body, the split, the left button filled (the glyph for a left click, held or not).</summary>
        static void MouseGlyph(Painter2D p, Vector2 c, Color col, bool left)
        {
            float w = 10f, h = 15f; var r = new Rect(c.x - w / 2, c.y - h / 2, w, h);
            p.strokeColor = col; p.lineWidth = 1.5f; p.BeginPath(); p.Arc(new Vector2(c.x, r.yMin + w / 2), w / 2, Deg(180), Deg(360)); p.LineTo(new Vector2(r.xMax, r.yMax - 3)); p.Arc(new Vector2(c.x, r.yMax - 3), w / 2, Deg(0), Deg(180)); p.ClosePath(); p.Stroke();
            p.BeginPath(); p.MoveTo(new Vector2(c.x, r.yMin)); p.LineTo(new Vector2(c.x, r.yMin + 6)); p.MoveTo(new Vector2(r.xMin, r.yMin + 6)); p.LineTo(new Vector2(r.xMax, r.yMin + 6)); p.Stroke();
            if (left) { p.fillColor = col; p.BeginPath(); p.Arc(new Vector2(c.x, r.yMin + w / 2), w / 2 - 1.2f, Deg(180), Deg(270)); p.LineTo(new Vector2(c.x - 0.6f, r.yMin + 5.2f)); p.LineTo(new Vector2(r.xMin + 1.2f, r.yMin + 5.2f)); p.ClosePath(); p.Fill(); }
        }

        /// <summary>A small pistol (barrel right), for the marks over the agents.</summary>
        static void Gun(Painter2D p, Vector2 c, float s, Color col)
        {
            p.fillColor = col; p.BeginPath();
            p.MoveTo(c + new Vector2(-6, -3) * s); p.LineTo(c + new Vector2(6, -3) * s); p.LineTo(c + new Vector2(6, 0) * s); p.LineTo(c + new Vector2(-1, 0) * s);
            p.LineTo(c + new Vector2(-2, 5) * s); p.LineTo(c + new Vector2(-5.5f, 5) * s); p.LineTo(c + new Vector2(-4.5f, 0) * s); p.LineTo(c + new Vector2(-6, 0) * s); p.ClosePath(); p.Fill();
        }

        void FireHud(float W, float H)
        {
            if (marks == null || host?.Game == null) return;
            if (!fireBuilt) BuildFire();
            firePaints.Clear(); var g = host.Game; float now = Time.unscaledTime; bool live = !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None;
            var cam = host.View;
            if (live && cam) { Tracers(now); ShotFx(now); FireMarks(now); Reticle(now); CarFx(now); }
            fireLayer.MarkDirtyRepaint(); if (fireLayer.parent.IndexOf(fireLayer) != fireLayer.parent.childCount - 1) fireLayer.BringToFront();
            FireLineFrame(W, H, live);
            // the third person key strip: bottom centre, while driven
            bool strip = live && host.Driving && !host.Exploring; Show(tpStrip, strip);
            if (strip) { float sw = float.IsNaN(tpStrip.resolvedStyle.width) ? 560 : tpStrip.resolvedStyle.width; tpStrip.style.left = (W - sw) / 2; tpStrip.style.top = H - 14 - 34; }
            // the prompt's caps: driven, the chip on the prompted target (not while aiming: the reticle has its own line)
            bool caps = live && host.Driving && host.Hover != null && host.DriveFocus != null && host.Hover.Kind == host.DriveFocus.Kind && host.Hover.Key == host.DriveFocus.Key && !host.Aiming;
            Show(chipCaps, caps); if (caps) Show(chipMouse, host.controls.clickActs);
            if (host.Aiming) Show(chip2, false);
            PortraitFire(live);
        }

        /// <summary>The portrait's sidearm mark while its agent fires: a pulsing solid ring and rays (firing), a dashed ring and the back arrow
        /// (returning fire), read from the engine's fire state this frame.</summary>
        void PortraitFire(bool live)
        {
            foreach (var set in new[] { colWins, rowWins })
                for (int i = 0; i < 4 && set != null; i++)
                {
                    var w = set[i]; if (w?.Arm == null) continue;
                    if (w.Arm.Q<Paint>("firemark") is not Paint fm)
                    {
                        int k = i; fm = P(w.Arm, -8, -8, 36, 36, (p, r) => FireMarkPaint(p, r, k)); fm.name = "firemark";
                    }
                    var fs = host.FireNow[i]; bool on = live && fs.Firing; Show(fm, on); if (on) fm.MarkDirtyRepaint();
                }
        }

        void FireMarkPaint(Painter2D p, Rect r, int k)
        {
            var fs = host.FireNow[k]; var c = r.center; float now = Time.unscaledTime;
            if (fs.Mode == "return")
            {   // returning fire: a dashed ring and the back arrow
                p.strokeColor = Cream; p.lineWidth = 2f; p.lineCap = LineCap.Butt;
                for (int i = 0; i < 8; i++) { p.BeginPath(); p.Arc(c, 14f, Deg(i * 45f), Deg(i * 45f + 26f)); p.Stroke(); }
                p.lineCap = LineCap.Round; p.BeginPath(); p.MoveTo(c + new Vector2(17, 10)); p.LineTo(c + new Vector2(10, 10)); p.MoveTo(c + new Vector2(13, 7)); p.LineTo(c + new Vector2(10, 10)); p.LineTo(c + new Vector2(13, 13)); p.Stroke();
                return;
            }
            float pulse = 0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 2f * 1.5f);
            p.strokeColor = new Color(TurqLight.r, TurqLight.g, TurqLight.b, 0.55f + 0.45f * pulse); p.lineWidth = 2f + 1.4f * pulse; p.BeginPath(); p.Arc(c, 13.5f + 1.5f * pulse, Deg(0), Deg(360)); p.Stroke();
            p.strokeColor = Cream; p.lineWidth = 1.8f; p.lineCap = LineCap.Round; p.BeginPath(); // rays off the muzzle side
            p.MoveTo(c + new Vector2(14, -6)); p.LineTo(c + new Vector2(18, -9)); p.MoveTo(c + new Vector2(15, -1)); p.LineTo(c + new Vector2(19.5f, -1)); p.MoveTo(c + new Vector2(14, 4)); p.LineTo(c + new Vector2(18, 7)); p.Stroke();
        }

        Vector3 Muzzle(int k)
        {
            var key = "op/" + Op.Name((OpId)k); var t = host.Views?.Find(key); var at = t ? t.position : host.frame.World(host.Game.Ops[k].Pos);
            float yaw = host.Views != null && host.Views.FigureYaw(key) is float y ? y : 0f; var fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            return at + Vector3.up * 1.32f + fwd * 0.42f;
        }

        /// <summary>The cell's tracers: from each firing agent's muzzle to its target, dashes marching toward the target; cut the frame the engine stops.</summary>
        void Tracers(float now)
        {
            var g = host.Game; int driven = host.DriveAgent;
            for (int k = 0; k < 4; k++)
            {
                var fs = host.FireNow[k]; if (!fs.Firing || string.IsNullOrEmpty(fs.Target)) continue;
                if (host.DrawnAt(fs.Target) is not Vector3 tw) continue;
                if (!Proj(Muzzle(k), out var a) || !Proj(tw + Vector3.up * 1.2f, out var b)) continue;
                bool thick = k == driven, thin = fs.Mode == "return";
                float core = thick ? 3.2f : thin ? 1.3f : 2.1f, edge = thick ? 7.5f : thin ? 3.2f : 5f, dash = thick ? 14f : 10f, gap = thick ? 9f : 8f;
                float phase = Mathf.Repeat(now * 260f, dash + gap);
                firePaints.Add(pp =>
                {
                    var d = b - a; float len = d.magnitude; if (len < 4f) return; var u = d / len; pp.lineCap = LineCap.Butt;
                    for (int pass = 0; pass < 3; pass++)
                    {
                        pp.strokeColor = pass == 0 ? new Color(Ink.r, Ink.g, Ink.b, 0.55f) : pass == 1 ? new Color(TurqLight.r, TurqLight.g, TurqLight.b, 0.8f) : Cream;
                        pp.lineWidth = pass == 0 ? edge + 2f : pass == 1 ? edge : core;
                        pp.BeginPath();
                        for (float s = phase - (dash + gap); s < len; s += dash + gap) { float s0 = Mathf.Max(0f, s), s1 = Mathf.Min(len, s + dash); if (s1 <= s0) continue; pp.MoveTo(a + u * s0); pp.LineTo(a + u * s1); }
                        pp.Stroke();
                    }
                });
            }
        }

        /// <summary>Per shot (the engine's shot log): the muzzle flash, a hit spark or a miss puff; in the follow view also the hostiles' solid streaks
        /// (the overhead view draws those in ShotMarks).</summary>
        void ShotFx(float now)
        {
            foreach (var s in host.ShotFx)
            {
                float age = now - s.born; if (age > 0.25f) continue;
                bool cell = s.side == "cell";
                if (!Proj(s.from, out var a) || !Proj(s.to, out var b)) continue;
                var d = (b - a); var u = d.sqrMagnitude > 1f ? d.normalized : Vector2.right; var n = new Vector2(-u.y, u.x);
                if (age < 0.09f) firePaints.Add(pp =>
                {   // the muzzle flash: a four-point star on an ink underlay, the length of the barrel's direction
                    float r = cell ? 9f : 7f;
                    void Star(Color c, float rr) { pp.fillColor = c; pp.BeginPath(); pp.MoveTo(a + u * rr * 1.8f); pp.LineTo(a + n * rr * 0.4f); pp.LineTo(a + n * rr); pp.LineTo(a - u * rr * 0.4f); pp.LineTo(a - n * rr); pp.LineTo(a - n * rr * 0.4f); pp.ClosePath(); pp.Fill(); }
                    Star(new Color(Ink.r, Ink.g, Ink.b, 0.6f), r + 2f); Star(new Color(1f, 0.97f, 0.86f), r);
                });
                if (!cell && host.Following && age < 0.14f) firePaints.Add(pp => { pp.lineCap = LineCap.Round; pp.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.6f); pp.lineWidth = 4.5f; pp.BeginPath(); pp.MoveTo(a); pp.LineTo(b); pp.Stroke(); pp.strokeColor = s.side == "rival" ? Violet : Amber; pp.lineWidth = 2.4f; pp.BeginPath(); pp.MoveTo(a); pp.LineTo(b); pp.Stroke(); });
                if (age < 0.2f) firePaints.Add(pp =>
                {
                    float t = age / 0.2f;
                    if (s.hit)
                    {   // the hit spark: six rays bursting out and fading
                        pp.lineCap = LineCap.Round;
                        for (int i = 0; i < 6; i++)
                        {
                            float ang = (i * 60f + 15f) * Mathf.Deg2Rad; var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)); float r0 = 3f + 6f * t, r1 = 9f + 10f * t;
                            pp.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.5f * (1 - t)); pp.lineWidth = 4f; pp.BeginPath(); pp.MoveTo(b + dir * r0); pp.LineTo(b + dir * r1); pp.Stroke();
                            pp.strokeColor = new Color(1f, 0.95f, 0.75f, 1 - t); pp.lineWidth = 2.2f; pp.BeginPath(); pp.MoveTo(b + dir * r0); pp.LineTo(b + dir * r1); pp.Stroke();
                        }
                    }
                    else { pp.strokeColor = new Color(Cream.r, Cream.g, Cream.b, 0.8f * (1 - t)); pp.lineWidth = 1.6f; pp.BeginPath(); pp.Arc(b, 3f + 7f * t, Deg(0), Deg(360)); pp.Stroke(); } // a miss: a small puff where it lands
                });
            }
        }

        /// <summary>Over each firing agent: the gun with rays (firing: the driven agent's on a turquoise disc), the gun with a back arrow (returning
        /// fire), hollow when a follower or a lock has no reach or line. A follower of the driven agent wears the mark as soon as it follows.</summary>
        void FireMarks(float now)
        {
            int driven = host.DriveAgent;
            for (int k = 0; k < 4; k++)
            {
                var fs = host.FireNow[k];
                bool follower = host.Followers.Contains((OpId)k);
                bool on = fs.Firing || fs.Hollow || follower && fs.Mode is "follow" or "follow-held";
                if (!on || host.Game.Ops[k].Down || host.Game.Ops[k].Inside) continue;
                var t = host.Views?.Find("op/" + Op.Name((OpId)k)); if (!t || !t.gameObject.activeInHierarchy) continue;
                if (!Proj(t.position + Vector3.up * 2.25f, out var c)) continue;
                bool ret = fs.Mode == "return", hollow = fs.Hollow || !fs.Firing, mine = k == driven, locked = fs.Mode == "lock";
                firePaints.Add(pp =>
                {
                    float r = 12f;
                    pp.fillColor = hollow ? new Color(Plate.r, Plate.g, Plate.b, 0.55f) : mine ? TurqLight : Plate; pp.BeginPath(); pp.Arc(c, r, Deg(0), Deg(360)); pp.Fill();
                    pp.strokeColor = hollow ? TurqLight : Cream; pp.lineWidth = 1.6f;
                    if (hollow) { for (int i = 0; i < 10; i++) { pp.BeginPath(); pp.Arc(c, r, Deg(i * 36f), Deg(i * 36f + 20f)); pp.Stroke(); } }
                    else { pp.BeginPath(); pp.Arc(c, r, Deg(0), Deg(360)); pp.Stroke(); }
                    var gc = mine && !hollow ? Ink : Cream; Gun(pp, c + new Vector2(-1.5f, 0.5f), 1f, gc);
                    if (ret)
                    {   // the back arrow under the barrel: fire comes back at its shooter
                        pp.strokeColor = gc; pp.lineWidth = 1.5f; pp.lineCap = LineCap.Round; pp.BeginPath(); pp.MoveTo(c + new Vector2(6, 6)); pp.LineTo(c + new Vector2(0, 6)); pp.MoveTo(c + new Vector2(2.5f, 3.5f)); pp.LineTo(c + new Vector2(0, 6)); pp.LineTo(c + new Vector2(2.5f, 8.5f)); pp.Stroke();
                    }
                    else if (!hollow && Mathf.Repeat(now * 3f, 1f) < 0.75f)
                    {   // the rays at the muzzle (they flicker while it fires)
                        pp.strokeColor = gc; pp.lineWidth = 1.5f; pp.lineCap = LineCap.Round; pp.BeginPath();
                        pp.MoveTo(c + new Vector2(6.5f, -5)); pp.LineTo(c + new Vector2(9.5f, -7.5f)); pp.MoveTo(c + new Vector2(7.5f, -1.5f)); pp.LineTo(c + new Vector2(10.5f, -1.5f)); pp.MoveTo(c + new Vector2(6.5f, 2)); pp.LineTo(c + new Vector2(9.5f, 4.5f)); pp.Stroke();
                    }
                    if (locked) { pp.fillColor = Cream; pp.BeginPath(); pp.Arc(c + new Vector2(r - 1, -r + 1), 3.2f, Deg(0), Deg(360)); pp.Fill(); } // a lock: a small pip at the rim (a lasting order)
                });
            }
        }

        /// <summary>While aiming: the reticle at the cursor (band ticks set by the hit chance), the hold's fill for a press that needs it, and the
        /// follow ring (8 m) on the ground round the driven agent with each follower ringed.</summary>
        void Reticle(float now)
        {
            if (!host.Aiming || host.DriveAgent < 0) return;
            var c = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(host.AimScreen.x, Screen.height - host.AimScreen.y));
            var fp = host.AimPreview; bool has = host.AimId.Length > 0 && fp != null && fp.Line;
            float acc = has ? Mathf.Clamp01(fp.Acc / 1000f) : 0f; float spread = has ? Mathf.Lerp(16f, 5f, acc) : 12f; float hold = host.AimHoldProgress;
            bool hostile = host.AimAt != null && host.AimId.Length > 0 && (host.AimAt.Kind is "hostile" || host.Game.FightOn && host.AimAt.Kind is "unit" or "security" or "patrol" or "rivals");
            firePaints.Add(pp =>
            {
                float r = 13f; var col = has ? Cream : new Color(Cream.r, Cream.g, Cream.b, 0.6f);
                pp.lineCap = LineCap.Round;
                for (int pass = 0; pass < 2; pass++)
                {
                    pp.strokeColor = pass == 0 ? new Color(Ink.r, Ink.g, Ink.b, 0.6f) : col; pp.lineWidth = pass == 0 ? 4f : 2f;
                    if (hostile || !has) { pp.BeginPath(); pp.Arc(c, r, Deg(0), Deg(360)); pp.Stroke(); }
                    else for (int i = 0; i < 12; i++) { pp.BeginPath(); pp.Arc(c, r, Deg(i * 30f), Deg(i * 30f + 16f)); pp.Stroke(); } // dashed: not a combatant (the loud shape cue): the press takes the hold
                    pp.BeginPath();
                    foreach (var dir in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right }) { pp.MoveTo(c + dir * (r + spread * 0.25f)); pp.LineTo(c + dir * (r + 5f + spread * 0.25f)); } // band ticks: wider apart when the odds drop
                    pp.Stroke();
                }
                pp.fillColor = col; pp.BeginPath(); pp.Arc(c, 1.8f, Deg(0), Deg(360)); pp.Fill();
                if (hold > 0f) RingArc(pp, c, r + 9f, hold, TurqLight, 3.4f); // the 0.5 s hold
            });
            // the follow ring: 8 m round the driven agent on the ground, dashed; followers ringed solid
            var me = host.Views?.Find("op/" + Op.Name((OpId)host.DriveAgent)); if (!me) return;
            float rad = host.Game.FollowRingCm / 100f; var segs = new List<(Vector2 a, Vector2 b)>(); var camP = host.View.transform.position; var camF = host.View.transform.forward;
            for (int i = 0; i < 96; i += 2) // dashed: every other 1/96 of the ring; only the arcs in front of the camera
            {
                float a0 = i * Mathf.PI * 2f / 96f, a1 = (i + 1) * Mathf.PI * 2f / 96f;
                var w0 = me.position + new Vector3(Mathf.Cos(a0) * rad, 0.05f, Mathf.Sin(a0) * rad); var w1 = me.position + new Vector3(Mathf.Cos(a1) * rad, 0.05f, Mathf.Sin(a1) * rad);
                if (Vector3.Dot(w0 - camP, camF) < 0.5f || Vector3.Dot(w1 - camP, camF) < 0.5f) continue;
                if (Proj(w0, out var q0) && Proj(w1, out var q1)) segs.Add((q0, q1));
            }
            if (segs.Count > 0) firePaints.Add(pp =>
            {
                pp.lineCap = LineCap.Butt;
                for (int pass = 0; pass < 2; pass++)
                {
                    pp.strokeColor = pass == 0 ? new Color(Ink.r, Ink.g, Ink.b, 0.35f) : new Color(TurqLight.r, TurqLight.g, TurqLight.b, 0.8f); pp.lineWidth = pass == 0 ? 3.6f : 1.8f;
                    foreach (var (sa, sb) in segs) { pp.BeginPath(); pp.MoveTo(sa); pp.LineTo(sb); pp.Stroke(); }
                }
            });
            foreach (var f in host.Followers)
            {
                var ft = host.Views?.Find("op/" + Op.Name(f)); if (!ft) continue; var ring = new List<Vector2>(); bool fo = true;
                for (int i = 0; i <= 24 && fo; i++) { float a = i * Mathf.PI * 2f / 24f; if (Proj(ft.position + new Vector3(Mathf.Cos(a) * 0.6f, 0.05f, Mathf.Sin(a) * 0.6f), out var q)) ring.Add(q); else fo = false; }
                if (fo) firePaints.Add(pp => { pp.strokeColor = TurqLight; pp.lineWidth = 2.2f; pp.BeginPath(); pp.MoveTo(ring[0]); for (int i = 1; i < ring.Count; i++) pp.LineTo(ring[i]); pp.Stroke(); });
            }
        }

        /// <summary>The cars as targets (engine 0.16, Game.CarOf): smoke rising while disabled, flame while burning, the blast ring expanding to the
        /// rulebook's blast radius when one explodes, then a smudge of smoke. Shapes only.</summary>
        void CarFx(float now)
        {
            var g = host.Game;
            foreach (var id in host.CarIdsNow())
            {
                Game.CarTarget c = null; try { c = g.CarOf(id); } catch { }
                if (c == null || c.DisabledAt < 0 && c.BurningAt < 0 && !c.Blown) continue;
                var vt = host.Views?.Find("car/" + id.Substring(4)); Vector3 at = vt && vt.gameObject.activeInHierarchy ? vt.position : host.frame.World(g.CarAt(id) ?? c.At);
                bool burning = c.BurningAt >= 0 && !c.Blown;
                if (c.Blown && host.CarBlownAt.TryGetValue(id, out var bt))
                {
                    float age = now - bt, rad = g.R.CarBlastCm / 100f;
                    if (age < 0.9f)
                    {   // the blast ring: from the car out to the blast radius in 0.6 s, then fading
                        float t = Mathf.Clamp01(age / 0.6f), rr = Mathf.Lerp(0.8f, rad, 1f - (1f - t) * (1f - t)), al = age < 0.6f ? 1f : 1f - (age - 0.6f) / 0.3f;
                        var ring = new List<Vector2>(); bool ok = true;
                        for (int i = 0; i <= 48 && ok; i++) { float a = i * Mathf.PI * 2f / 48f; if (Proj(at + new Vector3(Mathf.Cos(a) * rr, 0.1f, Mathf.Sin(a) * rr), out var q)) ring.Add(q); else ok = false; }
                        if (ok) firePaints.Add(pp => { for (int pass = 0; pass < 2; pass++) { pp.strokeColor = pass == 0 ? new Color(Ink.r, Ink.g, Ink.b, 0.5f * al) : new Color(FlameB.r, FlameB.g, FlameB.b, al); pp.lineWidth = pass == 0 ? 7f : 4f; pp.BeginPath(); pp.MoveTo(ring[0]); for (int i = 1; i < ring.Count; i++) pp.LineTo(ring[i]); pp.Stroke(); } });
                        if (age < 0.35f && Proj(at + Vector3.up * 1f, out var fc)) { float ft = age / 0.35f; firePaints.Add(pp => { pp.fillColor = new Color(1f, 0.95f, 0.8f, 1f - ft); pp.BeginPath(); pp.Arc(fc, 18f + 40f * ft, Deg(0), Deg(360)); pp.Fill(); }); } // the flash
                    }
                    Puffs(at, now, 3, 0.7f); // the wreck smokes
                    continue;
                }
                if (burning) { Flames(at, now); Puffs(at, now, 4, 1f); }
                else Puffs(at, now, 3, 0.75f); // disabled: smoke from the bonnet
            }
        }

        /// <summary>Smoke: n grey puffs rising from over the car and growing as they go, looping.</summary>
        void Puffs(Vector3 at, float now, int n, float dark)
        {
            for (int i = 0; i < n; i++)
            {
                float life = 2.2f, t = Mathf.Repeat(now / life + i / (float)n, 1f);
                var w = at + new Vector3(Mathf.Sin(i * 2.1f + now * 0.7f) * 0.35f, 1.25f + t * 2.6f, Mathf.Cos(i * 1.7f) * 0.3f);
                if (!Proj(w, out var q)) continue; float px = host.View && host.View.orthographic ? 14f / Mathf.Max(1f, host.View.orthographicSize) : 1f;
                float r = (9f + 20f * t) * px, al = (t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f) * 0.85f;
                var col = new Color(Smoke.r * dark, Smoke.g * dark, Smoke.b * dark, al);
                firePaints.Add(pp => { pp.fillColor = col; pp.BeginPath(); pp.Arc(q, r, Deg(0), Deg(360)); pp.Fill(); });
            }
        }

        /// <summary>Flame: three flickering tongues over the car (amber, a pale core).</summary>
        void Flames(Vector3 at, float now)
        {
            for (int i = 0; i < 3; i++)
            {
                var w = at + new Vector3((i - 1) * 0.7f, 0.95f, 0f); if (!Proj(w, out var q)) continue;
                float px = host.View && host.View.orthographic ? 14f / Mathf.Max(1f, host.View.orthographicSize) : 1f;
                float f = 0.75f + 0.25f * Mathf.Sin(now * 13f + i * 2.3f), h = (46f + 14f * (i % 2)) * f * px, wd = 17f * px;
                firePaints.Add(pp =>
                {
                    void Tongue(Color c, float hh, float ww) { pp.fillColor = c; pp.BeginPath(); pp.MoveTo(q + new Vector2(-ww, 0)); pp.BezierCurveTo(q + new Vector2(-ww, -hh * 0.5f), q + new Vector2(-ww * 0.2f, -hh * 0.7f), q + new Vector2(0, -hh)); pp.BezierCurveTo(q + new Vector2(ww * 0.2f, -hh * 0.7f), q + new Vector2(ww, -hh * 0.5f), q + new Vector2(ww, 0)); pp.ClosePath(); pp.Fill(); }
                    Tongue(new Color(Ink.r, Ink.g, Ink.b, 0.45f), h + 3f, wd + 2f); Tongue(FlameA, h, wd); Tongue(FlameB, h * 0.55f, wd * 0.5f);
                });
            }
        }

        /// <summary>The one fire line, at the target (the reticle while aiming): who and how long, "held" while the trigger is down, the stop key;
        /// "Stopped" for 1 s after the fire ends. While aiming and not firing: the reticle's chip (the left button, Fire, down in n s).</summary>
        void FireLineFrame(float W, float H, bool live)
        {
            int k = host.FireLineAgent; bool show = false;
            if (live && k >= 0)
            {
                var (head, down, stopped, drawn) = host.FireLine(k);
                if (host.controls.HoldActs) { if (host.FireStatus() != null) head = ""; else if (head.Length > 0 && !stopped) { head = host.FireWords(k); down = ""; } } // brief 041: the pill's words; the pill itself when a hold or a lock is on
                bool aimChip = head.Length == 0 && host.Aiming && host.AimId.Length > 0 && !(host.controls.HoldActs && host.FireHold != null);
                if (head.Length > 0 || aimChip)
                {
                    show = true;
                    bool trig = host.Driving && k == host.DriveAgent && host.TriggerDown;
                    if (aimChip)
                    {
                        Text(fireHead, host.AimHolding ? "Hold to fire" : "Fire"); Text(fireDown, host.AimDownS >= 0 ? $"{(host.AimPreview?.DownIs == "disabled" ? "disabled" : "down")} in {host.AimDownS} s" : host.AimPreview?.Line == false ? "no line" : "");
                        Show(fireCapLmb, true); Show(fireIcon, true); Show(fireHeldIcon, false); Show(fireHeld, false); Show(fireCapX, false);
                    }
                    else
                    {
                        Text(fireHead, head); Text(fireDown, stopped ? (drawn ? "gun drawn" : "") : down);
                        Show(fireCapLmb, false); Show(fireIcon, !stopped); Show(fireHeldIcon, trig && !stopped); Show(fireHeld, trig && !stopped); Show(fireCapX, !stopped && !trig);
                    }
                    Show(fireDown, fireDown.text.Length > 0);
                    // where: above the target (the reticle while aiming), else above the agent
                    Vector2 at;
                    var fs = host.FireNow[k]; string tgt = fs.Firing ? fs.Target : host.FireStoppedOn[k];
                    if (host.Aiming) at = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(host.AimScreen.x, Screen.height - host.AimScreen.y)) + new Vector2(28, -54);
                    else if (tgt.Length > 0 && host.DrawnAt(tgt) is Vector3 tw && Proj(tw + Vector3.up * 2.2f, out var tq)) at = tq + new Vector2(-40, -50);
                    else if (host.Views?.Find("op/" + Op.Name((OpId)k)) is Transform ot && Proj(ot.position + Vector3.up * 2.9f, out var oq)) at = oq + new Vector2(-40, -40);
                    else at = new Vector2(W / 2 - 120, 120);
                    float lw = float.IsNaN(fireLine.resolvedStyle.width) ? 320 : fireLine.resolvedStyle.width;
                    fireLine.style.left = Mathf.Clamp(at.x, 240, W - lw - 14); fireLine.style.top = Mathf.Clamp(at.y, 112, H - 230); // clear of the roster, the top pills, the feed and the key strip
                    fireLine.style.opacity = stopped ? Mathf.Clamp01((HandoverHost.StoppedS - (Time.unscaledTime - host.FireStoppedAt[k])) / 0.3f) : 1f;
                }
            }
            Show(fireLine, show); if (show) fireLine.BringToFront();
        }
    }
}
