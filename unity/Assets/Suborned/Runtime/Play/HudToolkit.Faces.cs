using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101 step 4, the roster as street faces (Sky's mix, 2026-11-08; Glyph's S mocks, briefs/101/glyph/r2/S-*.png): no tray. Each agent is its live portrait
    /// (60 px) in a cream ring over a soft shadow, the selected one ringed turquoise 5 px out; health is a column of pips at the face's right (turquoise, cream once lost, each in
    /// an ink keyline so it holds on any ground); what the agent is doing is one icon on a disc at the face's lower right (the act, a queued order, a landing's tone, the armed
    /// verb; firing, the disc turns orange), never its seconds or a bar (brief 101); the sidearm mark sits at the lower left while drawn, or on the selected or hovered face (a
    /// click draws or holsters, its hover opens the stance fan); a known face's badge at the upper left, the watcher's eye at the upper right; the cue layer round the face as it
    /// was. A refused order's why, an under-fire cause or a word the icon can't say ("too far", "→ Inez", a landing's word) is a street label beside the face, never a plate.
    /// Every part keeps its click: the face selects (Shift adds) and drags; the act disc switches a walk's pace; its × (on hover) stops the act; the sidearm mark draws and
    /// holsters. It retires the tall column (its art, the row under each face, the steel dividers) and the busy chips beside it. The column keeps its 0.79 scale
    /// (RosterScale), so its own px are screen px over RosterScale; the labels and the × stand in the root at screen px.</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>A face's centre and radius in the column's own px: on screen x 52, y 52 + 86 i, 60 px across (the mock's). Whole column px, as every part's offset,
        /// so the layout rounds every face's parts alike (the faces' audit: one offset on all four).</summary>
        static Vector2 FaceC(int i) => new(Cs(52f - ColX), Cs(52f - ColY + 86f * i));
        static float FaceR => Cs(30f);
        /// <summary>Screen px (at 1080) in the column's own px, whole.</summary>
        static float Cs(float px) => Mathf.Round(px / RosterScale);
        /// <summary>A size in the column's own px, even, so its half is whole too.</summary>
        static float Ce(float px) => 2f * Mathf.Round(px / RosterScale / 2f);
        /// <summary>A face's centre on the panel (screen px at 1080).</summary>
        static Vector2 FaceScreen(int i) => new(ColX + FaceC(i).x * RosterScale, ColY + FaceC(i).y * RosterScale);
        /// <summary>The Squad tab under the faces (the mock: 36 px at 34, 368).</summary>
        const float SquadX = 34f, SquadY = 368f, SquadD = 36f;

        sealed class FaceBits { public VisualElement Known, Row, X; public KeyIcon Eye, Why; public KeyText Words; public bool Over; }
        readonly FaceBits[] faceBits = new FaceBits[4];
        /// <summary>The proof reads it: each face's act disc icon and its words now ("scout:fire:hot fixer::"), "" before the roster stands.</summary>
        public static string FacesShown { get; private set; } = "";

        void BuildFaces(VisualElement frame, Window[] into)
        {
            StreetSheet();
            for (int i = 0; i < 4; i++)
            {
                var w = into[i] = new Window { Index = i, Street = true }; int k = i; var c = FaceC(i); float r = FaceR;
                var sh = Disc(frame, 2f * r, "e-shadow"); Pin(sh, new Vector2(c.x - r, c.y - r + Cs(2.5f))); sh.style.opacity = 0.85f; // the soft shadow the faces stand on
                float sr = Ce(70f) / 2f; w.RingSel = E(frame, null, c.x - sr, c.y - sr, 2f * sr, 2f * sr); Round(w.RingSel, sr); Border(w.RingSel, Cs(3.5f), TurqLight); Show(w.RingSel, false); // the mock: 70 px across, 3.5 px, turquoise
                var clip = w.Clip = E(frame, null, c.x - r, c.y - r, 2f * r, 2f * r); Round(clip, r); clip.style.overflow = Overflow.Hidden;
                w.Portrait = E(clip, null, 0, 0, 2f * r, 2f * r, pick: true); w.Next = E(clip, null, 0, 0, 2f * r, 2f * r); Show(w.Next, false);
                w.Portrait.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.PortraitPress(k, e.shiftKey || e.ctrlKey || e.commandKey); e.StopPropagation(); } });
                w.Portrait.RegisterCallback<PointerEnterEvent>(_ => rosterHover = k); w.Portrait.RegisterCallback<PointerLeaveEvent>(_ => { if (rosterHover == k) rosterHover = -1; });
                var ring = E(frame, "e-inline", c.x - r, c.y - r, 2f * r, 2f * r); Round(ring, r); BorderW(ring, Cs(2.5f)); // the cream ring over the photo's edge
                w.RingBusy = E(frame, null, 0, 0, 0, 0); // busy is the act disc; never drawn
                w.Pips = w.Dots = E(frame, null, c.x + Cs(34f), c.y - Cs(26f), Cs(7f), Cs(60f)); w.Dots.name = "roster-dots"; w.Pips.style.flexDirection = FlexDirection.Column; w.Pips.style.overflow = Overflow.Visible;
                // the sidearm mark (lower left) and the act disc (lower right): 22 and 26 px, the street tab's body and cream ring
                float ad = Ce(22f); w.Arm = E(frame, "e-body e-inline", c.x - Cs(23f) - ad / 2f, c.y + Cs(21f) - ad / 2f, ad, ad, pick: true); Round(w.Arm, ad / 2f); BorderW(w.Arm, Cs(2f));
                float gi = Ce(13f); w.GunIcon = E(w.Arm, null, (ad - gi) / 2f - Cs(2f), (ad - gi) / 2f - Cs(2f), gi, gi); Icon(w.GunIcon, "fire", Cream);
                var wk = w; w.StanceIn = P(w.Arm, (ad - gi) / 2f - Cs(2f), (ad - gi) / 2f - Cs(2f), gi, gi, (p, _) => StanceGlyph(p, new Vector2(gi / 2f, gi / 2f), gi * 0.42f, host.Game != null ? host.Game.Ops[wk.Index].Gun.Stance : "return", Cream));
                w.Arm.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.DrawOrHolsterAgent(k); e.StopPropagation(); } });
                w.Arm.RegisterCallback<PointerEnterEvent>(_ => { faceBits[k].Over = true; if (host.OneInput) return; fanAgent = k; fanArm = true; }); w.Arm.RegisterCallback<PointerLeaveEvent>(_ => { faceBits[k].Over = false; fanArm = false; fanLeft = Time.unscaledTime; });
                var wl = L(w.Arm, "keycap"); wl.name = "warn"; wl.style.position = Position.Absolute; wl.style.left = -Cs(14f); wl.style.top = 0; wl.style.color = Amber; wl.style.fontSize = Cs(12f); Show(wl, false);
                float sd = Ce(26f); w.Status = E(frame, "e-body e-inline", c.x + Cs(23f) - sd / 2f, c.y + Cs(21f) - sd / 2f, sd, sd, pick: true); w.Status.name = "roster-status"; Round(w.Status, sd / 2f); BorderW(w.Status, Cs(2f));
                float si = Ce(14f); w.StatusIcon = E(w.Status, null, (sd - si) / 2f - Cs(2f), (sd - si) / 2f - Cs(2f), si, si); w.StatusIcon.name = "g"; Show(w.Status, false);
                w.Status.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && host.Game != null && host.Game.Ops[k].Walk != null) { host.ToggleGait(k); e.StopPropagation(); } }); // a walk's pace, as the old chip's lead glyph
                w.Status.RegisterCallback<PointerEnterEvent>(_ => faceBits[k].Over = true); w.Status.RegisterCallback<PointerLeaveEvent>(_ => faceBits[k].Over = false);
                // kept for the frame's shared code: the in-car badge, the armed glyph, the hover rim (the left-behind flag's line), the cue and eye layers
                w.Car = E(frame, null, c.x + 13, c.y - 34, 22, 22); E(w.Car, null, 4, 4, 14, 14).name = "g"; P(w.Car, 3, 3, 16, 16, (p, rr) => CuffsPaint(p, rr, Red)).name = "cf"; Show(w.Car, false);
                w.Glyph = E(frame, null, 0, 0, 20, 20); E(w.Glyph, null, 3.4f, 3.4f, 13.2f, 13.2f).name = "g"; Show(w.Glyph, false);
                w.Rim = E(frame, null, c.x - r - 2, c.y - r - 2, 2 * r + 4, 2 * r + 4); Round(w.Rim, r + 2); Border(w.Rim, 2f, Cream); Show(w.Rim, false);
                w.Cue = P(frame, c.x - 60, c.y - 60, 120, 120, (p, _) => CuePaint(wk, p)); w.Cue.style.overflow = Overflow.Visible;
                w.Eye = P(frame, c.x + Cs(14f), c.y - Cs(44f), 34, 24, (p, _) => EyePaint(wk, p));
                // a known face (security knows it): the "seen you" badge at the upper left
                var b = faceBits[i] = new FaceBits(); float kd = Ce(20f);
                b.Known = E(frame, "e-ink e-inline", c.x - Cs(24f) - kd / 2f, c.y - Cs(22f) - kd / 2f, kd, kd); Round(b.Known, kd / 2f); BorderW(b.Known, Cs(1.5f)); Icon(E(b.Known, null, Cs(2.5f), Cs(2.5f), Cs(13f), Cs(13f)), "sawyou", Cream); Show(b.Known, false);
                // beside the face, in the root at screen px: the run's eye, a cause's icon and two words, as street labels; the act's × on hover
                var at = FaceScreen(i); b.Row = StackRow(root, 28f); b.Row.style.position = Position.Absolute; b.Row.style.left = TakeX + TakeD + TakeGap; b.Row.style.top = at.y - 14f; b.Row.style.justifyContent = Justify.FlexStart; // brief 101: after the take-control tab's slot (HudToolkit.TakeControl)
                b.Eye = KIcon(b.Row, "eye_unk", 16f); b.Eye.Root.pickingMode = PickingMode.Position; b.Eye.Root.style.marginRight = 6f;
                b.Eye.Root.RegisterCallback<PointerEnterEvent>(_ => EyeTipShow(faceBits[k].Eye.Root, HudModel.EyeTip(host.RunEye(k), ""))); b.Eye.Root.RegisterCallback<PointerLeaveEvent>(_ => EyeTipShow(null, null));
                b.Why = KIcon(b.Row, "", 16f); b.Why.Root.style.marginRight = 5f; b.Words = KText(b.Row, 14f, "k-semi"); Show(b.Row, false);
                manifest.Add((b.Row, "marker", () => faceBits[k].Words.Words)); // a two-word street mark beside the face
                b.X = E(root, "s-btn_close_normal btn_close", at.x + 45f, at.y + 16f, 16, 16, pick: true); Show(b.X, false); // right of the pips, level with the act disc
                b.X.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.PlateStop(k); e.StopPropagation(); } });
                b.X.RegisterCallback<PointerEnterEvent>(_ => TipFor(k, true)); b.X.RegisterCallback<PointerLeaveEvent>(_ => TipFor(k, false));
            }
        }

        /// <summary>One face this frame (after the shared roster code: the portrait, the rings, the downed mark): the act disc, the sidearm mark, the known badge, the label, the ×.</summary>
        void FacesFrame(int i, Window w, HudModel.Slot s)
        {
            var b = faceBits[i]; var g = host.Game; bool ended = m.Ended, live = g != null && !ended;
            string hw = host.controls.HoldActs && live ? host.PlateWords(i) : "";
            var fs = host.FireNow[i]; bool firing = live && fs.Firing && !s.Down;
            bool left = s.LeftFate.Length > 0; int pk = host.Picked;
            string icon = "", fill = "e-body", tone = live && host.controls.HoldActs ? host.PlateDoneTone(i) : "";
            if (s.InCar) icon = "car";
            else if (left) icon = s.LeftFate == "captured" ? "restrain" : "s_moving";
            else if (s.Down || !live) icon = ""; // the downed mark is on the face: no second disc (Glyph 0.2.104 #7)
            else if (firing) { icon = "fire"; fill = "e-orange"; }
            else if (host.FireRefusalWord(i).Length > 0) icon = "refused";
            else if (hw == "queued") icon = "nextevent";
            else if (s.Phase is "walking" or "running") icon = s.Phase == "running" ? "run" : "s_moving";
            else if (hw is "drawing" or "aiming" or "holding") icon = "fire";
            else if (s.Busy && s.Icon.Length > 0) icon = s.Icon;
            else if (tone.Length > 0) icon = tone == "ok" ? "check" : tone == "soft" ? "asked" : tone == "cancel" ? "close_x" : "refused";
            else if (!PlateOn && pk >= 0 && pk < 5 && i == m.Agent && s.Selected && m.VerbHas[pk]) icon = ArmedIcon(); // the armed verb: a right click gives it
            if (icon == "fire" && !firing) fill = "e-body";
            Show(w.Status, icon.Length > 0);
            if (icon.Length > 0) { Class(w.Status, "e-body", fill == "e-body"); Class(w.Status, "e-orange", fill == "e-orange"); Icon(w.StatusIcon, icon, Cream); }
            // the sidearm mark: drawn (light), or holstered on the selected or hovered face; hidden while the act disc says it fires; a watcher's warning rings it amber
            bool drawn = s.Drawn || fs.Firing, over = rosterHover == i || b.Over || fanAgent == i && fanArm;
            int warn = live ? host.WarnS(i) : -1; bool armOn = s.Armed && !s.Down && live && !firing && (drawn || s.Selected || over || warn >= 0);
            Show(w.Arm, armOn);
            if (armOn)
            {
                bool pulse = warn >= 0 && Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.4f; int look = (drawn ? 1 : 0) | (warn >= 0 ? 2 : 0) | (pulse ? 4 : 0);
                if (armLook[i] != look)
                {
                    armLook[i] = look; Class(w.Arm, "e-page", drawn); Class(w.Arm, "e-body", !drawn); w.GunIcon.style.unityBackgroundImageTintColor = drawn ? BkInk : Cream; w.GunIcon.style.opacity = drawn ? 1f : 0.6f;
                    if (warn >= 0) Border(w.Arm, Cs(pulse ? 2.6f : 2f), Amber); else { BorderW(w.Arm, Cs(2f)); RingColour(w.Arm, drawn ? BkInk : null); }
                }
                var wl = w.Arm.Q<Label>("warn"); Show(wl, warn >= 0); if (warn >= 0) Text(wl, $"{warn}");
            }
            // a known face, the watcher's eye (LivePortrait) and the label beside the face
            bool known = false; if (live) try { known = System.Linq.Enumerable.Contains(g.KnownFaces, Op.Name((OpId)i)); } catch (System.Exception) { known = false; }
            Show(b.Known, known && !s.Down && !s.InCar);
            var (whyIcon, words, alpha) = host.controls.HoldActs && live ? host.WhyChipOf(i) : ("", "", 0f);
            if (words.Length == 0 && live && !s.Down && !s.InCar && hw.Length == 0 && !s.Busy && host.UnderFireNow[i].Count > 0) { var (uc, uw) = host.UnderFireWords(i, host.Peeking); if (uw.Length > 0) { whyIcon = WhyIcon(uc) is { Length: > 0 } wi ? wi : "bang"; words = uw; alpha = 1f; } }
            if (words.Length == 0 && FaceWord(hw)) { words = hw; whyIcon = ""; alpha = 1f; }
            string eye = s.Phase == "running" && live ? host.RunEye(i) : "";
            bool row = (words.Length > 0 || eye.Length > 0) && m.Dock == RosterDock.Left && !TakeHot(i); Show(b.Row, row); // brief 101: a hovered tab or capsule says its own words there
            if (row)
            {
                float rx = TakeRowX(i); if (b.Row.style.left.value.value != rx) b.Row.style.left = rx; b.Row.style.opacity = words.Length > 0 ? Mathf.Clamp01(alpha) : 1f; b.Words.Set(words); Show(b.Words.Root, words.Length > 0); b.Why.Set(this, words.Length > 0 ? whyIcon : "");
                b.Eye.Set(this, eye.Length > 0 ? "eye_" + eye : ""); if (eye.Length > 0) b.Eye.Face.style.unityBackgroundImageTintColor = eye == "open" ? Red : BkInk;
            }
            // the act's × while the face or its disc is hovered (or the × itself): it stops the act, as the old chip's did
            bool canX = live && !s.Down && (s.CanCancel || hw is "firing" or "aiming" or "drawing" or "too far" or "no line") && (rosterHover == i || b.Over || tipFor == i) && m.Dock == RosterDock.Left;
            Show(b.X, canX); if (canX) { bool costly = s.CancelTitle.Length > 0 && host.DoingOf(i) is var d && !(d.Phase is "walking" or "running") && !IsFree(d.CancelCode); b.X.style.unityBackgroundImageTintColor = costly ? TurqLight : Color.white; }
            shown[i] = $"{Op.Name((OpId)i)}:{icon}:{(fill == "e-orange" ? "hot" : "")}:{words}"; if (i == 3) FacesShown = string.Join(" ", shown);
        }
        readonly string[] shown = new string[4]; readonly int[] armLook = { -1, -1, -1, -1 }, pipMax = { -1, -1, -1, -1 };

        /// <summary>A plate word the act disc can't say (the old chip's words): a block, a walk-in's "→ Inez", a landing's word, a refusal; not the acts the icon says.</summary>
        bool FaceWord(string hw) => hw.Length > 0 && hw is not ("queued" or "drawing" or "aiming" or "firing" or "holding") && !(host.Text != null && hw == host.Text.DoneWord("plate.down", "down")) && hw != "down";

        /// <summary>The pips: a column at the face's right, 5 px each in a 1 px ink keyline, 7 px apart (turquoise, page cream once lost); more than eight close up.</summary>
        void FacesPips(Window w, int hp, int max)
        {
            if (pipMax[w.Index] != max)
            {   // laid out once a count: 5 px seen in a 7 px ink keyline, 7 px apart; more than eight close up
                pipMax[w.Index] = max; float pitch = max <= 8 ? 7f : Mathf.Max(4.5f, 58f / max), d = Mathf.Min(7f, pitch);
                while (w.PipDots.Count < max) { var p = E(w.Pips, null, w: 0, h: 0); p.style.flexShrink = 0; w.PipDots.Add(p); }
                for (int k = 0; k < w.PipDots.Count; k++) { var p = w.PipDots[k]; Show(p, k < max); p.style.width = p.style.height = Cs(d); p.style.marginBottom = Cs(pitch) - Cs(d); Round(p, Cs(d) / 2f); Border(p, Cs(1f), BkInk); }
            }
            for (int k = 0; k < max && k < w.PipDots.Count; k++) { var p = w.PipDots[k]; var col = k < hp ? BkTurq : StPage; if (p.style.backgroundColor.value != col) p.style.backgroundColor = col; }
            Show(w.Dots, max > 0);
        }

        /// <summary>The live pips (HudToolkit.Live's LivePips, for the faces): on a hit the lost one flashes alarm-orange for 0.6 s; badly hurt, the rest pulse; the Enforcer's warning
        /// flashes his face and pips.</summary>
        void FacesLivePips(int i, Window w, HudModel.Slot s)
        {
            if (host.Game == null) return; float now = Time.unscaledTime; var face = host.FaceOf(i);
            float ht = now - host.HitAt(i); int lost = host.LostPip(i); bool flash = ht >= 0f && ht < HandoverHost.HitFlashS && Mathf.Repeat(ht, 0.2f) < 0.12f;
            if (i == (int)OpId.Enforcer && host.EnforcerWarnFlash && Mathf.Repeat(now, 0.4f) < 0.24f) { foreach (var d0 in w.PipDots) d0.style.backgroundColor = Alarm; w.Portrait.style.unityBackgroundImageTintColor = Alarm; w.Next.style.unityBackgroundImageTintColor = Alarm; return; }
            float pulse = face == HandoverHost.Face.BadlyHurt ? 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Cos(now * Mathf.PI * 2f)) : 1f;
            for (int k = 0; k < w.PipDots.Count; k++) { var d = w.PipDots[k]; if (flash && k == lost) d.style.backgroundColor = Alarm; d.style.opacity = k < s.Hp ? pulse : 1f; }
        }

        /// <summary>The faces' rules, in the layout audit (screen px at 1080): every act disc, sidearm mark and pip column at one offset from its face on all four; no part of a
        /// face's set touching the next face. Empty = clean.</summary>
        static void FacesAlign(List<string> bad)
        {
            if (live?.column == null || !Visible(live.column) || live.m.Dock != RosterDock.Left) return;
            float k = 1080f / Mathf.Max(1f, live.root.panel.visualTree.layout.height); var offs = new Dictionary<string, Vector2>();
            for (int i = 0; i < 4; i++)
            {
                var w = live.colWins[i]; if (w == null || !w.Street) continue; var fc = w.Clip.worldBound.center * k;
                foreach (var (name, e) in new[] { ("act", w.Status), ("sidearm", w.Arm), ("pips", w.Dots) })
                {
                    if (e == null || !Visible(e)) continue; var r = e.worldBound; var o = r.center * k - fc;
                    if (offs.TryGetValue(name, out var o0) && Vector2.Distance(o, o0) > 0.7f) bad.Add($"face {i + 1}/{name}: offset {o} differs from {o0}"); else offs.TryAdd(name, o);
                    if (i < 3 && live.colWins[i + 1]?.Clip is VisualElement nx && Visible(nx) && r.yMax * k > nx.worldBound.yMin * k + 0.5f && r.xMin < nx.worldBound.xMax && r.xMax > nx.worldBound.xMin) bad.Add($"face {i + 1}/{name} touches face {i + 2}");
                }
            }
        }
    }
}
