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
    ///  · per shot (the engine's shot log): a muzzle flash at the shooter's gun and a hit spark (or a miss puff) on the target. No line runs from a
    ///    shooter to its target, lasting or per shot (Sky 2026-10-26: "Simplify the visuals, it's way too crowded. Just having the agent facing who
    ///    they're shooting should be enough to not need those lines everywhere"): the shooter faces its target (HandoverHost.AimTargets);
    ///  · the firing mark over the agent (the gun with rays), the return mark (the gun with a back arrow), the hollow follow mark;
    ///  · one line: the gun disc, the downed mark and the seconds ("45 s"; brief 049 row 4), then "Stopped" for 1 s;
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

        Paint fireLayer; readonly List<Action<Painter2D>> firePaints = new(); readonly List<Rect> fireDiscs = new();
        VisualElement fireLine, fireIcon, fireDownIcon, fireHeldIcon, fireCapX, fireCapLmb; const float FireDisc = 32f; Label fireHead, fireDown, fireHeld, fireStop;
        bool fireBuilt;
        static readonly Color FlameA = new(1f, 0.62f, 0.18f), FlameB = new(1f, 0.86f, 0.42f), Smoke = new(0.42f, 0.45f, 0.45f);

        void BuildFire()
        {
            fireBuilt = true;
            fireLayer = P(marks, 0, 0, 0, 0, (p, r) => { foreach (var f in firePaints) f(p); });
            fireLayer.style.left = 0; fireLayer.style.top = 0; fireLayer.style.right = 0; fireLayer.style.bottom = 0; fireLayer.style.width = StyleKeyword.Auto; fireLayer.style.height = StyleKeyword.Auto;
            // the line (brief 049 row 4; Glyph's 0.2.94 review #3: no sentence): [gun disc] [downed] 45 s; "Stopped" for 1 s after
            fireLine = E(root, "s-plate_dark", 0, 0, float.NaN, 38); fireLine.style.flexDirection = FlexDirection.Row; fireLine.style.alignItems = Align.Center; fireLine.style.paddingLeft = 6; fireLine.style.paddingRight = 16; // the disc sits in the plate's rounded cap; the words clear of the other cap (the layout audit's 4 px)
            fireLine.pickingMode = PickingMode.Ignore;
            fireCapLmb = P(fireLine, 0, 0, 14, 20, (p, r) => MouseGlyph(p, r.center, Cream, true)); fireCapLmb.style.position = Position.Relative; fireCapLmb.style.left = StyleKeyword.Auto; fireCapLmb.style.top = StyleKeyword.Auto; fireCapLmb.style.marginRight = 6; Show(fireCapLmb, false);
            // the kit's light disc (harm and fire: disc_light_24, ink icon), 32 px box = 24 px drawn, inside the 38 px plate
            fireIcon = E(fireLine, null, w: FireDisc, h: FireDisc); fireIcon.style.flexShrink = 0; Bg(fireIcon, "markers/disc_light_24", Color.white);
            Icon(E(fireIcon, null, (FireDisc - 14f) / 2f, (FireDisc - 14f) / 2f, 14f, 14f), "fire", Col(Tok.type.card, Ink));
            fireDownIcon = E(fireLine, null, w: 14, h: 14); fireDownIcon.style.flexShrink = 0; fireDownIcon.style.marginLeft = 8; Icon(fireDownIcon, "s_downed", Cream);
            fireHead = L(fireLine, "verb-name"); fireHead.style.fontSize = 14f; fireHead.style.marginLeft = 5;
            fireDown = L(fireLine, "verb-name"); fireDown.style.fontSize = 13f; fireDown.style.marginLeft = 10; Show(fireDown, false);
            fireHeldIcon = P(fireLine, 0, 0, 14, 20, (p, r) => MouseGlyph(p, r.center, Cream, true)); fireHeldIcon.style.position = Position.Relative; fireHeldIcon.style.left = StyleKeyword.Auto; fireHeldIcon.style.top = StyleKeyword.Auto; fireHeldIcon.style.marginLeft = 14; Show(fireHeldIcon, false);
            fireHeld = L(fireLine, "verb-name"); fireHeld.style.fontSize = 13f; fireHeld.style.marginLeft = 4; fireHeld.text = "held"; Show(fireHeld, false);
            fireCapX = E(fireLine, "s-plate_light", pick: true); fireCapX.style.flexDirection = FlexDirection.Row; fireCapX.style.alignItems = Align.Center; fireCapX.style.marginLeft = 10; fireCapX.style.height = 24; fireCapX.style.paddingLeft = 8; fireCapX.style.paddingRight = 10; var sx = E(fireCapX, null, w: 12, h: 12); Icon(sx, "close_x", Ink); fireCapX.name = "fire-stop"; fireCapX.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.StopClick(); e.StopPropagation(); } }); // brief 096: the "X stop" keycap is a Stop button (Glyph's key audit)
            fireStop = L(fireCapX, "verb-name"); fireStop.style.fontSize = 13f; fireStop.style.marginLeft = 5; fireStop.style.color = Ink; fireStop.text = BookWordsFile.Get().StopWord; Show(fireCapX, false);
            manifest.Add((fireLine, "image", () => "fire line: who, how long, the stop key"));
            Show(fireLine, false);
            // Sky's rule (a key legend means the controls are wrong; Glyph's 0.2.65 review 8, Aster 2026-10-22 "make sure no key legend survives
            // anywhere"): no third-person key strip, no keycaps on the prompt, no "held" / "X stop" caps on the fire line. Keys are taught by the
            // hints at first use; the verbs are the pill, the act prompt and the plate.
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
            firePaints.Clear(); fireDiscs.Clear(); var g = host.Game; float now = Time.unscaledTime; bool live = !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None;
            var cam = host.View;
            if (live && cam) { if (FightCues.Hot) CueShotFx(now); else ShotFx(now); if (!host.OneInput) FireMarks(now); Reticle(now); CarFx(now); } // engine 0.35 (Glyph b-final, cut): no firing discs over the shooters: they face him and fire // Sky 2026-10-26: no tracers (the facing says who shoots whom)
            CuesFrame(live && cam, now, W, H); /* brief 084 */
            fireLayer.MarkDirtyRepaint(); // its place in the marks (above every mark but the speech and the pops): StepAside
            RivalsForCarMarks(live && cam && !host.Following); // Glyph r123 §3: the car disc over each rival walking for the held car
            FireLineFrame(W, H, live);
            StepAside(H); // Glyph r123 F2: bubbles win; the fire pill, the clocks and the holders' plates step aside
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
                        int k = i; var wk = w; fm = P(w.Arm, -8, -8, 36, 36, (p, r) => FireMarkPaint(p, r, k, wk.Under)); fm.name = "firemark"; if (w.Under) { fm.style.left = -8; fm.style.top = -8; fm.style.width = w.Arm.style.width.value.value + 16; fm.style.height = w.Arm.style.height.value.value + 16; }
                    }
                    var fs = host.FireNow[i]; bool on = live && fs.Firing; Show(fm, on); if (on) fm.MarkDirtyRepaint();
                }
        }

        void FireMarkPaint(Painter2D p, Rect r, int k, bool under = false)
        {
            var fs = host.FireNow[k]; var c = r.center; float now = Time.unscaledTime;
            if (under)
            {   // brief 046: the weapon slot under the portrait sits 3 px from the status slot: a ring just outside the disc, no rays or arrow (return = dashed)
                float rr = (r.width - 16f) / 2f * 0.75f + 2.5f, pl = 0.5f + 0.5f * Mathf.Sin(now * Mathf.PI * 2f * 1.5f);
                p.lineCap = LineCap.Butt;
                if (fs.Mode == "return") { p.strokeColor = Cream; p.lineWidth = 1.6f; for (int i = 0; i < 8; i++) { p.BeginPath(); p.Arc(c, rr, Deg(i * 45f), Deg(i * 45f + 26f)); p.Stroke(); } return; }
                p.strokeColor = new Color(TurqLight.r, TurqLight.g, TurqLight.b, 0.55f + 0.45f * pl); p.lineWidth = 1.4f + 0.8f * pl; p.BeginPath(); p.Arc(c, rr, Deg(0), Deg(360)); p.Stroke();
                return;
            }
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

        /// <summary>Per shot (the engine's shot log): the muzzle flash at the shooter's gun, a hit spark on the target or a miss puff where the shot
        /// lands; the same in both views. Sky 2026-10-26: no streak or tracer from the shooter to the target (the cell's lasting dashed tracer, the
        /// overhead view's per-shot lines and the follow view's hostile streaks are gone); the flash, the shooter's facing and the spark say it.
        /// ShotFlashes: the frames a muzzle flash was drawn in so far (the proof reads it).</summary>
        public static int ShotFlashes { get; private set; }
        /// <summary>The frames a hostile's (anyone not the cell's) muzzle flash was drawn in so far (Glyph r123 G1: the proof reads it).</summary>
        public static int ShotFlashesHostile { get; private set; }
        const float MissShowS = 0.45f; // brief 080: a miss stays longer than a hit's spark, so a roll reads at play zoom
        void ShotFx(float now)
        {
            foreach (var s in host.ShotFx)
            {
                float age = now - s.born; if (age > MissShowS) continue;
                bool cell = s.side == "cell";
                // Glyph r123 G1 (the follow view's 0 flashes): the flash needs only the shooter on screen; its way is the shot's own, taken 0.5 m
                // along it when the target is off the view (behind the third-person camera); the spark needs the target
                if (!Proj(s.from, out var a)) continue; bool toOn = Proj(s.to, out var b);
                var wd = s.to - s.from; Vector2 d = toOn ? b - a : wd.sqrMagnitude > 1e-4f && Proj(s.from + wd.normalized * 0.5f, out var ah) ? ah - a : Vector2.right;
                var u = d.sqrMagnitude > 1f ? d.normalized : Vector2.right; var n = new Vector2(-u.y, u.x);
                if (age < 0.09f) { ShotFlashes++; if (!cell) ShotFlashesHostile++; firePaints.Add(pp =>
                {   // the muzzle flash: a four-point star on an ink underlay, its long point the way the barrel faces (toward the target)
                    float r = cell ? 9f : 7f;
                    void Star(Color c, float rr) { pp.fillColor = c; pp.BeginPath(); pp.MoveTo(a + u * rr * 1.8f); pp.LineTo(a + n * rr * 0.4f); pp.LineTo(a + n * rr); pp.LineTo(a - u * rr * 0.4f); pp.LineTo(a - n * rr); pp.LineTo(a - n * rr * 0.4f); pp.ClosePath(); pp.Fill(); }
                    Star(new Color(Ink.r, Ink.g, Ink.b, 0.6f), r + 2f); Star(new Color(1f, 0.97f, 0.86f), r);
                }); }
                if (age < MissShowS && toOn && !s.hit && !s.covered) { var wideFrom = a; var wideTo = b; float wideT = age / MissShowS; firePaints.Add(pp =>
                {   // brief 080 (hit rolls: a miss must read): the tracer that goes wide, on its way past the target to the puff, fading from the gun
                    pp.lineCap = LineCap.Round;
                    var tail = Vector2.Lerp(wideFrom, wideTo, wideT * 0.8f);
                    pp.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.5f * (1 - wideT)); pp.lineWidth = 3.6f; pp.BeginPath(); pp.MoveTo(tail); pp.LineTo(wideTo); pp.Stroke();
                    pp.strokeColor = new Color(1f, 0.97f, 0.86f, 0.95f * (1 - wideT)); pp.lineWidth = 1.6f; pp.BeginPath(); pp.MoveTo(tail); pp.LineTo(wideTo); pp.Stroke();
                }); }
                if (age < (s.hit || s.covered ? 0.2f : MissShowS) && toOn) firePaints.Add(pp =>
                {
                    float t = age / (s.hit || s.covered ? 0.2f : MissShowS);
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
                    else if (s.covered)
                    {   // engine 0.35 (ShotEvent.Covered; GDD §10 "a round cover takes chips the prop"): five stone chips flying off the prop, no ring
                        pp.lineCap = LineCap.Round;
                        for (int i = 0; i < 5; i++)
                        {
                            float ang = (i * 72f + 40f + (s.born * 997f % 30f)) * Mathf.Deg2Rad; var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) - 0.6f); var q = b + dir * (4f + 14f * t) + new Vector2(0, 10f * t * t);
                            pp.fillColor = new Color(Ink.r, Ink.g, Ink.b, 0.55f * (1 - t)); pp.BeginPath(); pp.Arc(q, 2.4f, Deg(0), Deg(360)); pp.Fill();
                            pp.fillColor = new Color(Cream.r, Cream.g, Cream.b, 1 - t); pp.BeginPath(); pp.Arc(q, 1.5f, Deg(0), Deg(360)); pp.Fill();
                        }
                    }
                    else
                    {   // a miss (Glyph b-final, cut: the miss ring): a dust puff on the paving past him, three soft filled blobs spreading and fading
                        for (int i = 0; i < 3; i++) { var q = b + new Vector2((i - 1) * (3f + 5f * t), -2f * t - (i == 1 ? 2f : 0f)); pp.fillColor = new Color(Smoke.r, Smoke.g, Smoke.b, 0.55f * (1 - t)); pp.BeginPath(); pp.Arc(q, 3.5f + 8f * t, Deg(0), Deg(360)); pp.Fill(); }
                    }
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
                fireDiscs.Add(new Rect(c.x - 13f, c.y - 13f, 26f, 26f)); // StepAside: plates keep off the discs
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
            // brief 049 (Glyph): no 8 m follow ring (it read as the fight lab's range circle); the followers wear their own 0.6 m ring
            foreach (var f in host.Followers)
            {
                var ft = host.Views?.Find("op/" + Op.Name(f)); if (!ft) continue; var ring = new List<Vector2>(); bool fo = true;
                for (int i = 0; i <= 24 && fo; i++) { float a = i * Mathf.PI * 2f / 24f; if (Proj(ft.position + new Vector3(Mathf.Cos(a) * 0.6f, 0.05f, Mathf.Sin(a) * 0.6f), out var q)) ring.Add(q); else fo = false; }
                if (fo) firePaints.Add(pp => { pp.strokeColor = TurqLight; pp.lineWidth = 2.2f; pp.BeginPath(); pp.MoveTo(ring[0]); for (int i = 1; i < ring.Count; i++) pp.LineTo(ring[i]); pp.Stroke(); });
            }
        }

        /// <summary>The cars as targets (engine 0.16, Game.CarOf): smoke rising while disabled, flame while burning, the blast ring expanding to the
        /// rulebook's blast radius when one explodes, then a smudge of smoke. Shapes only. Brief 078: a car the world's rig draws (HostCarFx) keeps
        /// only the blast ring here; the smoke, flame and flash are the rig's.</summary>
        void CarFx(float now)
        {
            var g = host.Game;
            foreach (var id in host.CarIdsNow())
            {
                Game.CarTarget c = null; try { c = g.CarOf(id); } catch { }
                if (c == null || c.DisabledAt < 0 && c.BurningAt < 0 && !c.Blown) continue;
                var vt = host.Views?.Find("car/" + id.Substring(4)); Vector3 at = vt && vt.gameObject.activeInHierarchy ? vt.position : host.frame.World(g.CarAt(id) ?? c.At);
                bool burning = c.BurningAt >= 0 && !c.Blown, inWorld = host.CarFxDrawn(id); // brief 078: the world's rig draws smoke, flame and flash
                if (c.Blown && (inWorld || host.CarBlownAt.ContainsKey(id)))
                {
                    // the blast ring stays (readable > rich: it is the rulebook's blast reach, who the blast hits), on the mission clock, so a
                    // pause holds it and a replay draws it at the same time as the world's fireball
                    float age = host.CarFxClock >= 0f ? host.CarFxClock - c.BlastAt / (float)Game.Hz : now - host.CarBlownAt[id], rad = g.R.CarBlastCm / 100f;
                    if (age >= 0f && age < 0.9f)
                    {   // the blast ring: from the car out to the blast radius in 0.6 s, then fading
                        float t = Mathf.Clamp01(age / 0.6f), rr = Mathf.Lerp(0.8f, rad, 1f - (1f - t) * (1f - t)), al = age < 0.6f ? 1f : 1f - (age - 0.6f) / 0.3f;
                        var ring = new List<Vector2>(); bool ok = true;
                        for (int i = 0; i <= 48 && ok; i++) { float a = i * Mathf.PI * 2f / 48f; if (Proj(at + new Vector3(Mathf.Cos(a) * rr, 0.1f, Mathf.Sin(a) * rr), out var q)) ring.Add(q); else ok = false; }
                        if (ok) firePaints.Add(pp => { for (int pass = 0; pass < 2; pass++) { pp.strokeColor = pass == 0 ? new Color(Ink.r, Ink.g, Ink.b, 0.5f * al) : new Color(FlameB.r, FlameB.g, FlameB.b, al); pp.lineWidth = pass == 0 ? 7f : 4f; pp.BeginPath(); pp.MoveTo(ring[0]); for (int i = 1; i < ring.Count; i++) pp.LineTo(ring[i]); pp.Stroke(); } });
                        if (!inWorld && age >= 0f && age < 0.35f && Proj(at + Vector3.up * 1f, out var fc)) { float ft = age / 0.35f; firePaints.Add(pp => { pp.fillColor = new Color(1f, 0.95f, 0.8f, 1f - ft); pp.BeginPath(); pp.Arc(fc, 18f + 40f * ft, Deg(0), Deg(360)); pp.Fill(); }); } // the flash
                    }
                    if (!inWorld) Puffs(at, now, 3, 0.7f); // the wreck smokes
                    continue;
                }
                if (inWorld) continue; // brief 078: the rig's smoke and flames; the flat shapes only stand in for a car with no rig (the pool's cap)
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

        /// <summary>The one fire line, at the target (the reticle while aiming): the gun disc, the downed mark and the seconds to down ("45 s");
        /// "Stopped" for 1 s after the fire ends. While aiming and not firing: the reticle's chip (the same disc and seconds).</summary>
        void FireLineFrame(float W, float H, bool live)
        {
            int k = host.FireLineAgent; bool show = false;
            if (live && k >= 0)
            {
                var (head, down, stopped, drawn) = host.FireLine(k);
                if (host.controls.HoldActs && host.FireStatus() != null) head = ""; // brief 041: the pill says it when a hold or a lock is on
                bool aimChip = head.Length == 0 && host.Aiming && host.AimId.Length > 0 && !(host.controls.HoldActs && host.FireHold != null);
                if (host.OneInput && !host.Aiming) head = ""; // Glyph G1 (0.2.135): under one input the fire line is off the street (it sat on the focus's lock disc and pips); the time to down rides on the shooter's portrait chip (HostHoldStatus.PlateWords); the cursor's own chip while aiming stays
                if (head.Length > 0 || aimChip)
                {
                    show = true;
                    // brief 049 row 4 (Glyph's 0.2.94 review #3): the gun disc and the number, never a sentence: [gun] [downed] 45 s; "Stopped"
                    int secs = aimChip ? host.AimDownS : stopped ? -1 : host.FireLineDownS(k);
                    string word = stopped ? "Stopped" : secs >= 0 ? $"{secs} s" : aimChip && host.AimPreview?.Line == false ? "no line" : "";
                    if (aimChip && secs >= 0 && HandoverHost.OddsOf(host.AimPreview, host.Game) is int aimPct && aimPct >= 0) word = $"{host.OddsBareLine(aimPct)} · {word}"; // brief 080: the odds on the reticle's chip
                    Text(fireHead, word); Show(fireHead, word.Length > 0);
                    Show(fireIcon, !stopped); Show(fireDownIcon, secs >= 0);
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
            Show(fireLine, show); Show(fireCapX, show); if (show) fireLine.BringToFront();
        }
    }
}
