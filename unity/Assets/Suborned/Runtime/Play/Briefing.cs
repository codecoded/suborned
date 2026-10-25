using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 062 B, "on the street" (Glyph's "Spec for Latch", board 03 frame 1; Sky 2026-10-25 picked B). Replaces the briefing card
    /// (panel_dark_modal) and the front end's lore screen: UI Toolkit layers over the real, paused street, read from the mission
    /// (HandoverHost.Brief, BriefScreen): a vignette and a corner scrim; the title, the hook and the objective top left; the file card
    /// (panel_card: TARGET, the photograph, the name, one fact) with a leader to the Office door and a dashed ground ring; the Office tag over
    /// the canopy; the two ways (panel_dark: name, then contact face → step → step) with dashed leaders to the door and the car; the clock plate
    /// (plate_dark: 0:00, the chime, the end; the preparation sweep); the bottom strip: the cell's faces with their job words, the purse,
    /// Day / Night and Begin (the light line's word, always focused; Space begins). Kit frames and kit colours only.
    /// Begin runs HandoverHost's start sequence; this view plays its first 0.3 s (title up, cards right, strip down), then the roster slides in
    /// at 3.3 s and the clock bar fades in at 3.6 s (Entrance).</summary>
    public sealed partial class HudToolkit
    {
        VisualElement bfTagTail;
        VisualElement bf, bfScrim, bfTitleGroup, bfCol, bfCard, bfPhoto, bfPhotoBadge, bfTag, bfClock, bfStrip, bfLeaders, bfPurse; Label bfTitle, bfHook, bfObjective, bfCardHead, bfName, bfFact, bfTagText, bfPurseText;
        readonly List<(VisualElement root, Label name, List<VisualElement> pics, List<VisualElement> arrows)> bfWays = new();
        readonly List<(VisualElement root, VisualElement icon, Label time)> bfMarks = new();
        readonly List<(VisualElement cameo, Label job)> bfFaces = new();
        readonly FrontEnd.LightWord[] bfPhase = new FrontEnd.LightWord[2]; FrontEnd.LightWord bfBegin;
        BriefScreen bfFor;
        const float BfColW = 408f, BfMargin = 64f;
        /// <summary>Brief 062 B on (the street briefing); off: the 0.2.117 card, for comparison only.</summary>
        public static bool BriefOnStreet = true;

        /// <summary>The proof reads it: which parts the briefing drew and where its data came from.</summary>
        public static string BriefingState { get; private set; } = "";

        void BriefingBuild()
        {
            bf = E(root); bf.style.position = Position.Absolute; bf.style.left = 0; bf.style.top = 0; bf.style.right = 0; bf.style.bottom = 0; bf.pickingMode = PickingMode.Ignore; bf.style.overflow = Overflow.Hidden;
            bfScrim = new VisualElement { pickingMode = PickingMode.Ignore }; bfScrim.style.position = Position.Absolute; bfScrim.style.left = 0; bfScrim.style.top = 0; bfScrim.style.right = 0; bfScrim.style.bottom = 0;
            bfScrim.generateVisualContent += ScrimMesh; bf.Add(bfScrim); notHud.Add(bfScrim); /* the vignette dims the world, like the conversation veil and the dip: not HUD (brief 032) */
            bfLeaders = P(bf, 0, 0, 0, 0, LeaderPaint); bfLeaders.style.right = 0; bfLeaders.style.bottom = 0; bfLeaders.style.width = StyleKeyword.Auto; bfLeaders.style.height = StyleKeyword.Auto;
            // the title group (64, 58): THE HANDOVER (Jost Medium 15, tracking 0.30), a 2 px turquoise-light line 132 px, the hook (Jost Bold 34), the objective (Jost Regular 19)
            bfTitleGroup = E(bf, null, BfMargin, 58); bfTitleGroup.pickingMode = PickingMode.Ignore;
            bfTitle = new Label { pickingMode = PickingMode.Ignore }; bfTitleGroup.Add(bfTitle);
            var tl = E(bfTitleGroup, null, w: 132, h: 2); tl.style.position = Position.Relative; tl.style.marginTop = 6; tl.style.backgroundColor = LookLive;
            bfHook = new Label { pickingMode = PickingMode.Ignore }; bfHook.style.marginTop = 12; bfTitleGroup.Add(bfHook);
            bfObjective = new Label { pickingMode = PickingMode.Ignore }; bfObjective.style.marginTop = 6; bfTitleGroup.Add(bfObjective);
            manifest.Add((bfTitle, "hud", () => bfTitle.text)); manifest.Add((bfHook, "hud", () => bfHook.text)); /* the title and hook are the reading surface's copy, as the 0.2.117 card's were */ manifest.Add((bfObjective, "hud", () => bfObjective.text)); /* reading copy before play (READING state); the in-play goal line is the one objective line */
            // the right column: the clock plate, the file card, the two ways (scaled as one when the window is short)
            bfCol = E(bf, null, 0, 0, BfColW, 1000); bfCol.pickingMode = PickingMode.Ignore; bfCol.style.transformOrigin = new TransformOrigin(Length.Percent(100), 0, 0);
            bfClock = PlateDark(bfCol, 82, 0.5f); bfClock.style.position = Position.Absolute; bfClock.style.left = BfColW - 560; bfClock.style.top = 40; bfClock.style.width = 560;
            P(bfClock, 0, 0, 560, 82, ClockPaint);
            manifest.Add((bfClock, "image", () => "briefing clock"));
            bfCard = E(bfCol, "s-panel_card", 0, 132, BfColW, 478); bfCard.pickingMode = PickingMode.Ignore;
            bfCardHead = Words(bfCard, 11.5f); bfCardHead.style.position = Position.Absolute; bfCardHead.style.left = 24; bfCardHead.style.top = 7; bfCardHead.style.letterSpacing = 11.5f * 0.12f;
            var rim = E(bfCard, null, 21, 37, 366, 302); Round(rim, 13); rim.style.backgroundColor = InkDeep;
            bfPhoto = E(rim, null, 3, 3, 360, 296); Round(bfPhoto, 10); bfPhoto.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop;
            bfPhotoBadge = E(rim, null, 183 - 48, 151 - 48, 96, 96); bfPhotoBadge.pickingMode = PickingMode.Ignore; Show(bfPhotoBadge, false);
            var hair = E(bfCard, null, 24, 352, BfColW - 48, 1); hair.style.backgroundColor = LookHair;
            bfName = Words(bfCard, 28f, Ink); bfName.style.position = Position.Absolute; bfName.style.left = 24; bfName.style.top = 362;
            var fr = E(bfCard, null, 24, 410); fr.style.flexDirection = FlexDirection.Row; fr.style.alignItems = Align.Center;
            Icon(E(fr, null, w: 15, h: 15), "take", Ink); bfFact = Words(fr, 15f, Ink); bfFact.style.marginLeft = 7;
            manifest.Add((bfCard, "image", () => "target file card")); manifest.Add((bfName, "hud", () => bfName.text));
            for (int i = 0; i < 2; i++)
            {
                var w = E(bfCol, "s-panel_dark", 0, 640 + 128 * i, BfColW, 112); w.style.unitySliceScale = 0.5f; w.pickingMode = PickingMode.Ignore;
                var n = Words(w, 15.5f); n.style.position = Position.Absolute; n.style.left = 20; n.style.top = 13;
                var row = E(w, null, 20, 48); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.right = 20; row.style.width = StyleKeyword.Auto; row.style.justifyContent = Justify.Center; // Glyph's N7: the chain centred on the card
                var pics = new List<VisualElement>(); var arrows = new List<VisualElement>();
                for (int k = 0; k < 3; k++)
                {
                    if (k > 0) { var ar = P(row, 0, 0, 20, 14, (p, r) => Stroke2(p, () => { p.MoveTo(new Vector2(1, 7)); p.LineTo(new Vector2(17, 7)); p.MoveTo(new Vector2(12, 2.5f)); p.LineTo(new Vector2(17, 7)); p.LineTo(new Vector2(12, 11.5f)); }, Cream, 1.8f, 1.6f)); ar.style.position = Position.Relative; ar.style.marginLeft = 16; ar.style.marginRight = 16; arrows.Add(ar); }
                    var holder = E(row, null, w: 44, h: 44); holder.style.position = Position.Relative; holder.style.flexShrink = 0; pics.Add(holder);
                }
                bfWays.Add((w, n, pics, arrows)); int wi = i; manifest.Add((w, "image", () => wi < bfWays.Count ? "way " + bfWays[wi].name.text : "way"));
            }
            // the Office tag over the canopy: plate_dark, the papers icon, the name; a chrome tail onto the door
            bfTag = PlateDark(bf, UnlH, UnlScale); bfTag.style.position = Position.Absolute; bfTag.style.paddingLeft = 9; bfTag.style.paddingRight = 11;
            Icon(E(bfTag, null, w: 13, h: 13), "papers", Cream); bfTagText = Words(bfTag, 13.5f); bfTagText.style.marginLeft = 6;
            var tail = P(bfTag, 0, 0, 18, 13, (p, r) =>
            {
                p.fillColor = Chrome; p.BeginPath(); p.MoveTo(new Vector2(0, 2)); p.LineTo(new Vector2(18, 2)); p.LineTo(new Vector2(9, 13)); p.ClosePath(); p.Fill();
                p.fillColor = LookTail; p.BeginPath(); p.MoveTo(new Vector2(3, 0)); p.LineTo(new Vector2(15, 0)); p.LineTo(new Vector2(9, 7.5f)); p.ClosePath(); p.Fill();
            });
            tail.style.top = StyleKeyword.Auto; tail.style.bottom = -11; tail.style.left = Length.Percent(50); tail.style.translate = new Translate(-9, 0); bfTagTail = tail;
            manifest.Add((bfTag, "hud", () => bfTagText.text));
            // the bottom strip: the faces (cameos in the chrome ring, 62 px) and their job words, the purse, Day / Night, Begin
            bfStrip = E(bf); bfStrip.style.position = Position.Absolute; bfStrip.style.left = 0; bfStrip.style.right = 0; bfStrip.style.bottom = 0; bfStrip.style.height = 150; bfStrip.pickingMode = PickingMode.Ignore;
            for (int i = 0; i < 4; i++)
            {
                var c = Cameo(bfStrip, 56f, 2f); c.style.position = Position.Absolute; c.style.left = BfMargin + i * 88; c.style.top = 150 - 64 - 62 - 22;
                var j = new Label { pickingMode = PickingMode.Ignore }; j.style.position = Position.Absolute; j.style.left = BfMargin + i * 88 - 13; j.style.width = 88; j.style.top = 150 - 64 - 14; j.style.unityTextAlign = TextAnchor.MiddleCenter; bfStrip.Add(j);
                bfFaces.Add((c, j));
            }
            manifest.Add((bfFaces[0].cameo, "image", () => "cell faces"));
            bfPurse = PlateDark(bfStrip, 34, 0.43f); bfPurse.style.position = Position.Absolute; bfPurse.style.left = BfMargin + 4 * 88 + 30; bfPurse.style.top = 150 - 64 - 62 - 22 + 14; bfPurse.style.paddingLeft = 12; bfPurse.style.paddingRight = 14;
            Icon(E(bfPurse, null, w: 14, h: 14), "p_cash", Cream); bfPurseText = Words(bfPurse, 15.5f); bfPurseText.style.marginLeft = 7;
            manifest.Add((bfPurse, "hud", () => bfPurseText.text));
            var right = E(bfStrip); right.style.position = Position.Absolute; right.style.right = BfMargin; right.style.bottom = 64 - 18; right.style.flexDirection = FlexDirection.Row; right.style.alignItems = Align.Center; right.pickingMode = PickingMode.Ignore;
            var LL = FrontEnd.LS;
            for (int i = 0; i < 2; i++)
            {
                bool night = i == 1; var w = FrontEnd.Word(right, host.Text?.BriefWord(night ? "night" : "day", night ? "Night" : "Day") ?? (night ? "Night" : "Day"), LL.type.tab, float.NaN, 34, 0f, 0f, 0f, 29f, false, false);
                w.Root.style.marginLeft = i == 0 ? 0 : 22; FrontEnd.Hook(w, null, () => host.SetNight(night)); bfPhase[i] = w;
            }
            manifest.Add((bfPhase[0].Root, "hud", () => "Day Night"));
            var beginType = new FrontEnd.TypeS { font = "Jost-Medium", sizeRef = 26, trackingEm = 0.34f, uppercase = true };
            bfBegin = FrontEnd.Word(right, host.Text?.BriefWord("begin", "Begin") ?? "Begin", beginType, float.NaN, 52, 30f, 22f, 0f, 44f, true, false); bfBegin.Root.style.marginLeft = 64; bfBegin.Root.style.flexShrink = 0;
            FrontEnd.Hook(bfBegin, null, () => host.BriefingBegin()); bfBegin.Focused = true; FrontEnd.PaintWord(bfBegin);
            manifest.Add((bfBegin.Root, "hud", () => "Begin"));
            Show(bf, false);
        }

        /// <summary>Per frame (from the briefing's place in LateUpdate): the screen while the briefing is up, its exit in the first 0.3 s after Begin.</summary>
        void BriefingFrame()
        {
            if (bf == null) BriefingBuild();
            var h = host; var b = h.Brief; float t = h.StartT;
            bool up = b != null && !m.Ended && (h.ShowBriefing || h.Starting && t < HandoverHost.BriefExitS);
            Show(bf, up); BriefingState = up ? (h.Starting ? "exit" : "up") + "|" + string.Join(";", b.Sources) : "";
            if (!up) return;
            if (bfFor != b) { bfFor = b; BriefingFill(b); }
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) || size.x <= 0 ? 1920 : size.x, H = float.IsNaN(size.y) || size.y <= 0 ? 1080 : size.y;
            float k = Mathf.Min(1f, (H - 170f) / 880f); // the column (clock top 40 to the second way's foot 880) clear of the strip on a short window
            bfCol.style.left = W - BfMargin - BfColW; bfCol.style.scale = new Scale(new Vector3(k, k, 1f));
            // the exit (0 to 0.3 s after Begin): the title lifts away, the cards slide right, the strip drops; the scene stays lit
            float e = h.Starting ? Mathf.Clamp01(t / HandoverHost.BriefExitS) : 0f; e = 1f - (1f - e) * (1f - e);
            bfTitleGroup.style.translate = new Translate(0, -48f * e); bfTitleGroup.style.opacity = 1f - e;
            bfCol.style.translate = new Translate(140f * e, 0); bfCol.style.opacity = 1f - e;
            bfStrip.style.translate = new Translate(0, 90f * e); bfStrip.style.opacity = 1f - e;
            bfScrim.style.opacity = 1f - e; bfLeaders.style.opacity = 1f - e; bfTag.style.opacity = 1f - e;
            bfPurseText.text = m.Cash;
            for (int i = 0; i < 2; i++) if (bfPhase[i] is { } pw) { bool on = (i == 1) == h.NightLook; if (pw.Focused != on) { pw.Focused = on; FrontEnd.PaintWord(pw); } }
            // the Office tag over the canopy, its tail onto the door
            // Glyph's N2: anchored on the canopy (OfficeAt = brief.target.at, the official entrance), clamped inside the frame (24 px from the top, 16 px
            // from each side); clamped, the tail no longer points at the canopy, so it is dropped
            if (b.OfficeAt is Vector3 oa && Proj(oa + Vector3.up * 4.6f, out var tq))
            {
                Show(bfTag, true); float tw = float.IsNaN(bfTag.resolvedStyle.width) ? 150f : bfTag.resolvedStyle.width;
                float left = tq.x - tw / 2f, top = tq.y - UnlH - 12f, cl = Mathf.Clamp(left, 16f, Mathf.Max(16f, W - 16f - tw)), ct = Mathf.Clamp(top, 24f, Mathf.Max(24f, H - 170f - UnlH));
                bfTag.style.left = cl; bfTag.style.top = ct; Show(bfTagTail, Mathf.Abs(cl - left) < 0.5f && Mathf.Abs(ct - top) < 0.5f);
            }
            else Show(bfTag, false);
            // the dashed ground rings at the door and the car (the world markers' ground code: cream over ink)
            if (b.TargetAt is Vector3 ta) GroundRing(ta, 0.9f, Cream, 2.4f, true, 0.85f);
            foreach (var w in b.Ways) if (w.At is Vector3 wa && (!b.TargetAt.HasValue || (wa - b.TargetAt.Value).sqrMagnitude > 1f)) GroundRing(wa, 1.6f, Cream, 2.4f, true, 0.85f);
            bfLeaders.MarkDirtyRepaint(); bfScrim.MarkDirtyRepaint();
        }

        void BriefingFill(BriefScreen b)
        {
            FrontEnd.Typeset(bfTitle, new FrontEnd.TypeS { font = "Jost-Medium", sizeRef = 15, trackingEm = 0.30f, uppercase = true }, b.Title);
            FrontEnd.Typeset(bfHook, new FrontEnd.TypeS { font = "Jost-Bold", sizeRef = 34 }, b.Hook);
            FrontEnd.Typeset(bfObjective, new FrontEnd.TypeS { font = "Jost-Regular", sizeRef = 19 }, b.Objective);
            // Glyph's N8: Jost Bold (the kit file as cut) on the card head, the name, the fact and the way names; the overlays' own face is lighter
            FrontEnd.Typeset(bfCardHead, new FrontEnd.TypeS { font = "Jost-Bold", sizeRef = 11.5f, trackingEm = 0.12f, uppercase = true }, host.Text?.BriefWord("target", "Target") ?? "Target");
            FrontEnd.Typeset(bfName, new FrontEnd.TypeS { font = "Jost-Bold", sizeRef = 28f }, b.TargetName, "ink");
            FrontEnd.Typeset(bfFact, new FrontEnd.TypeS { font = "Jost-Bold", sizeRef = 15f }, b.TargetFact, "ink"); bfFact.style.marginLeft = 7;
            bool photo = Sp(b.TargetPhoto) != null; Show(bfPhotoBadge, !photo);
            if (photo) { Bg(bfPhoto, b.TargetPhoto, Color.white); bfPhoto.style.backgroundColor = StyleKeyword.Null; } else { bfPhoto.style.backgroundImage = StyleKeyword.Null; bfPhoto.style.backgroundColor = DiscBody; Bg(bfPhotoBadge, b.TargetBadge, Color.white); } // until the portrait lands: the npc badge on the ink field
            Text(bfTagText, b.OfficeName);
            for (int i = 0; i < bfWays.Count; i++)
            {
                var (root2, name, pics, arrows) = bfWays[i]; bool has = i < b.Ways.Count; Show(root2, has); if (!has) continue; var w = b.Ways[i];
                FrontEnd.Typeset(name, new FrontEnd.TypeS { font = "Jost-Bold", sizeRef = 15.5f }, w.Name);
                for (int k = 0; k < pics.Count; k++)
                {
                    var holder = pics[k]; holder.Clear(); bool on = k < w.Steps.Count; Show(holder, on); if (k > 0) Show(arrows[k - 1], on); if (!on) continue;
                    var s = w.Steps[k];
                    if (s.StartsWith("face:")) { var c = Cameo(holder, 38f, 2f); var role = s[5..]; var face = Sp("portraits/face_" + role) != null ? "portraits/face_" + role : HudModel.PersonBadge(role); Bg(c.Q("face"), face, Color.white); }
                    else if (s.StartsWith("badges/") || s.StartsWith("badge:")) { var d = E(holder, "s-disc_dark_24", -7, -7, 58, 58); var bd = E(d, null, 12, 12, 34, 34); Bg(bd, s.StartsWith("badge:") ? "badges/" + s[6..] : s, Color.white); } // Glyph's N7: the badge on the kit disc with its chrome ring, one family with the icon discs
                    else { var icon = s.StartsWith("icon:") ? s[5..] : s; var d = E(holder, "s-disc_dark_24", -7, -7, 58, 58); var g = E(d, null, 16, 16, 26, 26); Icon(g, icon, Cream); }
                }
            }
            for (int i = 0; i < 4; i++)
            {
                var role = Op.Name((OpId)i); Bg(bfFaces[i].cameo.Q("face"), $"portraits/cameo_{role}_normal", Color.white);
                FrontEnd.Typeset(bfFaces[i].job, new FrontEnd.TypeS { font = "Jost-SemiBold", sizeRef = 11.5f, trackingEm = 0.12f, uppercase = true, color = "textDim" }, host.Text?.BriefWord("job." + role, role) ?? role);
                bfFaces[i].job.style.justifyContent = Justify.Center; // Glyph's N5: a tracked word is a row of letters (FrontEnd.SetText), so the row is centred on the 62 px portrait, not the text
            }
            // the clock marks: icon and time over the track, at their place along it (the first left, the last right, the rest centred)
            foreach (var mk in bfMarks) mk.root.RemoveFromHierarchy(); bfMarks.Clear();
            for (int i = 0; i < b.Clock.Count; i++)
            {
                var c = b.Clock[i]; var r = E(bfClock); r.style.position = Position.Absolute; r.style.top = 14; r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.pickingMode = PickingMode.Ignore;
                var ic = E(r, null, w: 14, h: 14); ic.style.flexShrink = 0; Icon(ic, c.Icon, c.Lit ? LookLive : Cream); var tm = Words(r, 15.5f, c.Lit ? LookLive : Cream); tm.style.marginLeft = 6; tm.text = c.At;
                bfMarks.Add((r, ic, tm));
            }
            bfClock.schedule.Execute(PlaceMarks).StartingIn(0);
        }

        const float ClockX0 = 40f, ClockX1 = 520f, ClockTrackY = 58f;
        float ClockX(float s) => ClockX0 + (ClockX1 - ClockX0) * Mathf.Clamp01(s / Mathf.Max(1f, bfFor?.EndS ?? 510f));
        void PlaceMarks()
        {
            if (bfFor == null) return;
            for (int i = 0; i < bfMarks.Count && i < bfFor.Clock.Count; i++)
            {
                var r = bfMarks[i].root; float w = float.IsNaN(r.resolvedStyle.width) ? 60f : r.resolvedStyle.width, x = ClockX(bfFor.Clock[i].S);
                r.style.left = i == 0 ? x - 7f : i == bfMarks.Count - 1 ? x - w + 7f : x - w / 2f;
            }
        }

        void ClockPaint(Painter2D p, Rect r)
        {
            var b = bfFor; if (b == null) return; PlaceMarks();
            float lit = -1f; foreach (var c in b.Clock) if (c.Lit) lit = c.S;
            p.lineCap = LineCap.Round; p.strokeColor = new Color(Cream.r, Cream.g, Cream.b, 0.35f); p.lineWidth = 2f; p.BeginPath(); p.MoveTo(new Vector2(ClockX0, ClockTrackY)); p.LineTo(new Vector2(ClockX1, ClockTrackY)); p.Stroke();
            if (lit > 0f) { p.strokeColor = LookLive; p.lineWidth = 3f; p.BeginPath(); p.MoveTo(new Vector2(ClockX0, ClockTrackY)); p.LineTo(new Vector2(ClockX(lit), ClockTrackY)); p.Stroke(); } // the preparation sweep to the handover
            foreach (var c in b.Clock)
            {
                var at = new Vector2(ClockX(c.S), ClockTrackY);
                p.fillColor = c.Lit ? LookLive : InkDeep; p.BeginPath(); p.Arc(at, c.Lit ? 6f : 4.5f, Deg(0), Deg(360)); p.Fill();
                p.strokeColor = c.Lit ? Cream : Cream; p.lineWidth = 1.6f; p.BeginPath(); p.Arc(at, c.Lit ? 6f : 4.5f, Deg(0), Deg(360)); p.Stroke();
            }
        }

        /// <summary>The leaders: cream 2.4 px over ink-deep from the file card to the Office door (solid) and from each way card to its place (dashed).</summary>
        void LeaderPaint(Painter2D p, Rect r)
        {
            var b = bfFor; if (b == null || bf == null) return;
            Vector2 Local(Vector2 panel) => bfLeaders.WorldToLocal(panel);
            if (b.TargetAt is Vector3 ta && Proj(ta + Vector3.up * 0.1f, out var tq))
            {
                var cr = bfCard.worldBound; var from = Local(new Vector2(cr.xMin, cr.yMin + cr.height * 0.42f)); var to = Local(tq);
                Stroke2(p, () => { p.MoveTo(from); p.LineTo(to); }, Cream, 2.4f, 2.2f);
            }
            for (int i = 0; i < b.Ways.Count && i < bfWays.Count; i++)
            {
                if (b.Ways[i].At is not Vector3 wa || !Proj(wa + Vector3.up * 0.1f, out var wq)) continue;
                var wr = bfWays[i].root.worldBound; Vector2 a = Local(new Vector2(wr.xMin, wr.center.y)), z = Local(wq);
                var d = z - a; float len = d.magnitude; if (len < 4f) continue; var u = d / len;
                p.lineCap = LineCap.Butt; p.strokeColor = new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.85f); p.lineWidth = 4.6f;
                for (float s = 0f; s < len; s += 12f) { p.BeginPath(); p.MoveTo(a + u * s); p.LineTo(a + u * Mathf.Min(len, s + 7f)); p.Stroke(); }
                p.strokeColor = Cream; p.lineWidth = 2.4f;
                for (float s = 0f; s < len; s += 12f) { p.BeginPath(); p.MoveTo(a + u * s); p.LineTo(a + u * Mathf.Min(len, s + 7f)); p.Stroke(); }
            }
        }

        /// <summary>The scrims as one vertex-coloured grid in ink-deep: the vignette (0.42 from half the way to the corners), the corner scrim
        /// (0.82 at the top left falling linearly to 0 over 1000 px) and the strip's (0 at 300 px from the foot to 0.88 at it), composed as layers.</summary>
        void ScrimMesh(MeshGenerationContext mgc)
        {
            var r = bfScrim.contentRect; float W = r.width, H = r.height; if (W < 2f || H < 2f) return;
            const int nx = 32, ny = 18; var md = mgc.Allocate((nx + 1) * (ny + 1), nx * ny * 6);
            var c = new Vector2(W / 2f, H / 2f); float half = c.magnitude;
            for (int j = 0; j <= ny; j++)
                for (int i = 0; i <= nx; i++)
                {
                    float x = W * i / nx, y = H * j / ny;
                    float vig = 0.42f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, (new Vector2(x, y) - c).magnitude / half));
                    float corner = 0.82f * Mathf.Clamp01(1f - new Vector2(x, y).magnitude / 1000f); // Glyph's N3: linear over 1000 px (was ^1.5 over 700): the objective line reaches 4.5:1
                    float strip = 0.88f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(H - 300f, H, y)); // Glyph's N6: 0.88 over 300 px (was 0.82 over 220) under Begin and the tabs
                    float a = 1f - (1f - vig) * (1f - corner) * (1f - strip);
                    md.SetNextVertex(new Vertex { position = new Vector3(x, y, Vertex.nearZ), tint = new Color(InkDeep.r, InkDeep.g, InkDeep.b, a) });
                }
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                {
                    ushort v0 = (ushort)(j * (nx + 1) + i), v1 = (ushort)(v0 + 1), v2 = (ushort)(v0 + nx + 1), v3 = (ushort)(v2 + 1);
                    md.SetNextIndex(v0); md.SetNextIndex(v1); md.SetNextIndex(v2); md.SetNextIndex(v1); md.SetNextIndex(v3); md.SetNextIndex(v2);
                }
        }

        /// <summary>Brief 062 §5, the HUD's entrance after Begin: hidden under the briefing and the glide; at 3.3 s the roster slides in from the
        /// left over 0.25 s; at 3.6 s the clock bar fades in (0.3 s) as the clock starts. Per frame, after the roster and the pill are laid out.</summary>
        void Entrance()
        {
            var h = host; float t = h.StartT;
            float roster = h.ShowBriefing ? 0f : h.Starting ? Mathf.Clamp01((t - HandoverHost.BriefExitS - HandoverHost.BriefGlideS) / HandoverHost.BriefRosterS) : 1f;
            float clockA = h.ShowBriefing || h.Starting ? 0f : Mathf.Clamp01((Time.unscaledTime - h.SequencePlayedAt) / 0.3f); // a script's start shows it at once
            if (h.ShowBriefing && h.Game != null && h.Game.Tick > 0) { roster = 1f; clockA = 1f; } // the briefing opened again mid-run (B): the play HUD stays as it was
            float re = 1f - (1f - roster) * (1f - roster);
            foreach (var e in new[] { column, row }) { if (e == null) continue; e.style.opacity = re; e.style.translate = new Translate(-140f * (1f - re), 0); e.style.visibility = roster <= 0f ? Visibility.Hidden : StyleKeyword.Null; }
            if (pill != null) { pill.style.opacity = clockA; pill.style.visibility = clockA <= 0f ? Visibility.Hidden : StyleKeyword.Null; }
            // the settle (3.3 s): the four ground rings pulse once, out from each agent's feet
            if (h.Starting && t >= HandoverHost.BriefExitS + HandoverHost.BriefGlideS && h.Game != null && h.Views != null)
            {
                float u = Mathf.Clamp01((t - HandoverHost.BriefExitS - HandoverHost.BriefGlideS) / HandoverHost.BriefSettleS);
                for (int k = 0; k < 4; k++) if (h.Views.Find("op/" + Op.Name((OpId)k)) is Transform ot && ot.gameObject.activeInHierarchy) GroundRing(ot.position, 0.45f + 0.5f * u, new Color(LookLive.r, LookLive.g, LookLive.b, 1f - u), 2.6f, false, 0.6f * (1f - u));
            }
        }
    }
}
