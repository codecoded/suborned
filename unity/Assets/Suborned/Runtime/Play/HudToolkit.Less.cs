using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    /// <summary>Brief 014, less reading (Sky: "It's the reading, it's too much"; Glyph's briefs/014/glyph-less.md, Fable's briefs/014/fable-less.md).
    /// The play HUD at rest carries no sentence but people speaking: the roster at 0.8 with a busy chip per busy agent, the order plate, the
    /// status pill (clock, pause, next mark, $, Trail, Alert) and two keys under it, the goal line at the bottom centre (the objective or the hint,
    /// one imperative, its nouns in their marker colours, fading), story beats at the lower right (two at most, four seconds), speech bubbles at
    /// the speaker, refusal pops of an icon and a few words, the plot scope (HudToolkit.Scope), and the street's own marks: the eye on the
    /// target, the "?" over a watcher not yet watched, the clock pip over a guard whose routine is known, the key people's pips, the wake ring,
    /// the lock on a held door, the "!" and its dotted line to the phone, the activity ring over a busy agent, edge pointers to Vale and to
    /// anything urgent. An order is one line per way (HudModel.PriceLine). Alt shows the long words again as fragments and tooltips; the
    /// history (L) and the debrief keep every sentence. The view only reads the host: replays are untouched.</summary>
    public sealed partial class HudToolkit
    {
        static readonly Color Amber = new(0.941f, 0.627f, 0.125f), Violet = new(0.541f, 0.31f, 0.816f), VioletText = new(0.722f, 0.580f, 0.925f), Red = new(0.878f, 0.227f, 0.184f), Plate = new(0.059f, 0.153f, 0.157f, 0.92f);
        const float RosterScale = 0.8f;

        /// <summary>A painted element (UI Toolkit Painter2D): rings, wedges, arrows, dotted lines.</summary>
        sealed class Paint : VisualElement
        {
            public Action<Painter2D, Rect> Fn;
            public Paint() { pickingMode = PickingMode.Ignore; generateVisualContent += c => Fn?.Invoke(c.painter2D, contentRect); }
        }

        static Paint P(VisualElement parent, float x, float y, float w, float h, Action<Painter2D, Rect> fn)
        { var p = new Paint { Fn = fn }; p.style.position = Position.Absolute; p.style.left = x; p.style.top = y; p.style.width = w; p.style.height = h; parent?.Add(p); return p; }

        static Angle Deg(float d) => new(d, AngleUnit.Degree);
        static void RingArc(Painter2D p, Vector2 c, float r, float frac, Color col, float w)
        {
            if (frac <= 0.001f) return; p.strokeColor = col; p.lineWidth = w; p.lineCap = LineCap.Round; p.BeginPath();
            p.Arc(c, r, Deg(-90), Deg(-90 + 360f * Mathf.Clamp01(frac))); p.Stroke();
        }
        static void Dashed(Painter2D p, Vector2 c, float r, Color col, float w, int n = 12)
        {
            p.strokeColor = col; p.lineWidth = w; p.lineCap = LineCap.Butt;
            for (int i = 0; i < n; i++) { p.BeginPath(); float a0 = i * 360f / n; p.Arc(c, r, Deg(a0), Deg(a0 + 180f / n)); p.Stroke(); }
        }

        void Art(VisualElement e, string path, Color? tint = null)
        {
            if (string.IsNullOrEmpty(path)) { Show(e, false); return; }
            Show(e, true);
            if (path.StartsWith("icons/")) Bg(e, path, tint ?? Cream); else Bg(e, path, Color.white);
        }

        // ---------------------------------------------------------------- pieces

        VisualElement nextUnknown, nextTip; Label nextTipText;
        readonly List<VisualElement> peopleDiscs = new(); VisualElement historyKey2;
        VisualElement keys, keyIntel, keyHistory, keyDot, pauseIcon, goal, goalCameo, goalGlyph, goalWords, beatsBox, sheet, sheetHead, sheetCameo, sheetBadge, sheetRows, sheetTip, chip2, chipLead, chipFields, respIcons, loudIcons, loudLost;
        Label endCash, endTrail, endStand; Paint endStandArrow; int endStandDir;
        Label keyIntelN, sheetName, sheetNote, sheetTipText, respOff, respBot, respCome, chipRefused, endTally, briefGoal;
        Paint world; // the street's painted marks (rings, wedges, dotted lines, arrows), behind the HUD's plates
        readonly List<(VisualElement root, VisualElement icon, Label text, VisualElement bar, VisualElement fill, VisualElement x)> busy = new();
        readonly List<(VisualElement root, VisualElement badge, Label text)> beatEls = new();
        readonly List<(VisualElement root, Label text, VisualElement tail)> bubbleEls = new();
        readonly List<(VisualElement root, VisualElement icon, Label text)> popEls = new();
        sealed class Row { public VisualElement Root, Glyph, Fields, Alt, Key, Cameo, TagIcon; public Label Name, KeyText; public int Index; public string FieldsKey = "", AltKey = ""; }
        readonly List<Row> rows = new();
        readonly List<VisualElement> worldEls = new(); int worldUsed; // small marks over people (eyes, pips, "?", turned marks, activity icons)
        readonly List<(VisualElement root, VisualElement badge, Label name)> pipEls = new();
        readonly List<VisualElement> edgeEls = new();
        readonly List<VisualElement> briefPortraits = new(); VisualElement briefPeople, briefFacts, briefAltBox;
        string goalKey = "", chipKey = "";
        float cashChanged = -100f, trailChanged = -100f; string lastCash = "", lastTrail = "";
        int hoverRow = -1; bool lessBuilt;
        /// <summary>Elements for the manifest of a capture: (element, kind).</summary>
        readonly List<(VisualElement e, string kind, Func<string> text)> manifest = new();

        void BuildLess()
        {
            lessBuilt = true;
            world = P(marks, 0, 0, 0, 0, DrawWorld); world.style.left = 0; world.style.top = 0; world.style.right = 0; world.style.bottom = 0; world.style.width = StyleKeyword.Auto; world.style.height = StyleKeyword.Auto;
            world.SendToBack();
            // the roster at 0.8 (Glyph §1): the kit column scaled from its top left
            column.style.transformOrigin = new TransformOrigin(0, 0, 0); column.style.scale = new Scale(new Vector2(RosterScale, RosterScale));
            manifest.Add((column, "image", () => "roster portraits"));
            // the pill: a pause glyph instead of the PAUSED word
            pauseIcon = E(pill, null, w: 15, h: 15); pauseIcon.style.marginLeft = 11; pauseIcon.style.flexShrink = 0; Icon(pauseIcon, "pause", Cream);
            // the next mark (Sky: "?3:10" had no space and no meaning): the next-event glyph, then "?" as a glyph while the cell can't know what it is, then its time; hover names it
            nextUnknown = E(null, null, w: 15, h: 15); Icon(nextUnknown, "p_unknown", Cream); nextUnknown.style.marginLeft = 9; nextUnknown.style.flexShrink = 0; pill.Insert(pill.IndexOf(nextLabel), nextUnknown);
            nextTip = E(root, "s-plate_dark"); nextTip.style.position = Position.Absolute; nextTip.style.height = 24; nextTip.style.paddingLeft = 9; nextTip.style.paddingRight = 9; nextTip.style.justifyContent = Justify.Center; nextTipText = L(nextTip, "verb-name"); Show(nextTip, false);
            // the people plate under the pill (Sky: "2/4 … not sure what that refers to"): the mission's four key people as small discs, their
            // role glyphs (the pips' and the scope's), filled once the cell knows them, a quiet socket until then; a known one finds them, the
            // plate's rim opens Intel
            keys = E(root, "s-plate_dark", pick: true); keys.style.position = Position.Absolute; keys.style.right = 14; keys.style.top = 14 + 44 + 8; keys.style.height = ChipH; keys.style.flexDirection = FlexDirection.Row; keys.style.alignItems = Align.Center; keys.style.paddingLeft = ChipPad; keys.style.paddingRight = ChipPad;
            keys.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.IntelOpen = !host.IntelOpen; e.StopPropagation(); } });
            keyIntel = keys; keyIntelN = L(null, "verb-name");
            string[] people = { "waitress", "guard", "driver", "councillor" };
            for (int i = 0; i < 4; i++)
            {
                int k = i; var d = E(keys, null, w: Glyph, h: Glyph, pick: true); d.style.marginLeft = i == 0 ? 0 : 5; Bg(d, CastBadge(people[i])); peopleDiscs.Add(d);
                d.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { if (KnownPerson(people[k])) FindPerson(people[k]); else host.IntelOpen = !host.IntelOpen; e.StopPropagation(); } });
            }
            manifest.Add((keys, "image", () => "known people"));
            // the history (notes) key: the bottom-right corner, where its drawer opens (Sky); the story beats stack above it
            var hk = E(root, "s-plate_dark", pick: true); hk.style.position = Position.Absolute; hk.style.right = 14; hk.style.bottom = 14; hk.style.width = ChipH; hk.style.height = ChipH; hk.style.alignItems = Align.Center; hk.style.justifyContent = Justify.Center;
            // the glyph 11 px in (the rim's 6.5 and 4 clear); unread notes widen the plate by 10 so the dot sits beside the glyph, inside the
            // same clearance (the audit's rule), never on the rim's corner
            keyHistory = E(hk, null, 11, 11, Glyph, Glyph, pick: true); Icon(E(keyHistory, null, 0, 0, Glyph, Glyph), "papers", Cream);
            keyDot = E(hk, "s-dot_unread", 28, (ChipH - 7) / 2f, 7, 7);
            hk.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.FeedHistoryOpen = !host.FeedHistoryOpen; e.StopPropagation(); } });
            historyKey2 = hk;
            // busy chips beside the portraits
            for (int i = 0; i < 4; i++)
            {
                int k = i;
                // Sky round 2: fixed slots inside real padding (12 each side, as the pill's content sits inside its caps): icon 16, gap 8, bar 40,
                // gap 8, value 40 right-aligned (room for "88 s" and "1.4 m/s"); the ✕ sits outside the chip, never over its content
                var r = E(root, "s-plate_dark", 0, 0, ChipW, ChipH, pick: true); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.paddingLeft = ChipPad; r.style.paddingRight = ChipPad; r.style.overflow = Overflow.Visible;
                // Sky 2026-10-16: icon | bar | value | eye on equal 8 px gaps, the bar taking the slack, every glyph's whole box 4 px clear of the rim
                var ic = E(r, null, w: Glyph, h: Glyph); ic.style.flexShrink = 0;
                var bar = E(r, null, h: 8); bar.style.marginLeft = 8; bar.style.flexGrow = 1; bar.style.flexShrink = 1; bar.style.minWidth = 24; bar.style.backgroundColor = new Color(0.039f, 0.102f, 0.11f); Round(bar, 4); Border(bar, 1, new Color(0.51f, 0.627f, 0.612f));
                var fill = E(bar, null, 0, 0, 0, 6); fill.style.backgroundColor = TurqLight; Round(fill, 3);
                var tx = L(r, "verb-name"); tx.style.marginLeft = 8; tx.style.width = ValueW; tx.style.flexShrink = 0; tx.style.unityTextAlign = TextAnchor.MiddleRight; tx.style.whiteSpace = WhiteSpace.NoWrap;
                var x = E(r, "s-btn_close_normal btn_close", ChipW + 4, (ChipH - 16) / 2, 16, 16, pick: true);
                x.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.CancelAgent(k, null); e.StopPropagation(); } });
                x.RegisterCallback<PointerEnterEvent>(_ => TipFor(k, true)); x.RegisterCallback<PointerLeaveEvent>(_ => TipFor(k, false));
                r.RegisterCallback<PointerEnterEvent>(_ => busyHover = k); r.RegisterCallback<PointerLeaveEvent>(_ => { if (busyHover == k) busyHover = -1; });
                // Sky: the pace is the chip's lead glyph (footprints walk, runner run); a click on it switches a moving agent's pace (G does the same)
                ic.pickingMode = PickingMode.Position; ic.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && host.Game.Ops[k].Walk != null) { host.ToggleGait(k); e.StopPropagation(); } });
                var ey = E(r, null, w: Glyph, h: Glyph, pick: true); ey.style.marginLeft = 8; ey.style.flexShrink = 0; Icon(E(ey, null, 0, 0, Glyph, Glyph), "eye_shut", Cream); /* bare, its whole 32 px box ("?" included) at 14 px (Glyph's minimum) */
                int ek = i; ey.RegisterCallback<PointerEnterEvent>(_ => { var e2 = host.RunEye(ek); EyeTipShow(ey, HudModel.EyeTip(e2, "")); }); ey.RegisterCallback<PointerLeaveEvent>(_ => EyeTipShow(null, null)); busyEyes.Add(ey);
                busy.Add((r, ic, tx, bar, fill, x));
            }
            // the goal line: bottom centre
            goal = E(root, "s-plate_dark", pick: true); goal.style.position = Position.Absolute; goal.style.bottom = 14; goal.style.height = GoalH; goal.style.flexDirection = FlexDirection.Row; goal.style.alignItems = Align.Center; goal.style.paddingLeft = ChipPad; goal.style.paddingRight = ChipPad;
            // the chips' rule (the audit): the hint's cameo 18 px and its glyph 14 px, both 4 px clear of the plate's rim, so the line is 40 tall
            goalCameo = E(goal, null, w: 18, h: 18); goalCameo.style.marginRight = 6; Round(goalCameo, 9); Border(goalCameo, 1.4f, TurqLight);
            goalGlyph = E(goal, null, w: Glyph, h: Glyph); goalGlyph.style.marginRight = 8;
            goalWords = E(goal); goalWords.style.flexDirection = FlexDirection.Row; goalWords.style.alignItems = Align.Center;
            goal.RegisterCallback<PointerEnterEvent>(_ => goalHover = true); goal.RegisterCallback<PointerLeaveEvent>(_ => goalHover = false);
            manifest.Add((goal, "line", () => goalText));
            hintQ = E(root, "s-disc_dark_24", 0, 0, 26, 26, pick: true); hintQ.style.top = StyleKeyword.Auto; hintQ.style.bottom = 14 + (GoalH - 26) / 2f; Icon(E(hintQ, null, 5, 5, 16, 16), "p_unknown", TurqLight); Show(hintQ, false);
            hintQ.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.HintReopenAt = Time.unscaledTime; e.StopPropagation(); } });
            manifest.Add((hintQ, "image", () => "hint reopen"));
            manifest.Add((goalCameo, "image", () => "hint portrait"));
            // story beats: lower right, newest at the bottom
            beatsBox = E(root); beatsBox.style.position = Position.Absolute; beatsBox.style.right = 14; beatsBox.style.bottom = 14 + 36 + 8; // above the history key, the standard gap beatsBox.style.flexDirection = FlexDirection.Column; beatsBox.style.alignItems = Align.FlexEnd;
            for (int i = 0; i < 2; i++)
            {
                var b = E(beatsBox, "s-plate_dark"); b.style.height = ChipH; b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center; b.style.paddingLeft = ChipPad; b.style.paddingRight = ChipPad; b.style.marginTop = 8; // the chips' rule
                var bd = E(b, null, w: Glyph, h: Glyph); bd.style.marginRight = 8; /* under 70% of the chip's height */ var t = L(b, "verb-name"); t.style.fontSize = 13;
                beatEls.Add((b, bd, t)); int bi = i; manifest.Add((b, "beat", () => beatEls[bi].text.text));
            }
            // the order sheet (Glyph §2): one row per way
            sheet = E(root, "s-panel_dark", 0, 0, 374, float.NaN, pick: true); sheet.style.paddingTop = 12; sheet.style.paddingBottom = 18; sheet.style.paddingLeft = 22; sheet.style.paddingRight = 22; // clear of panel_dark's corner screws
            sheet.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            sheetHead = E(sheet); sheetHead.style.flexDirection = FlexDirection.Row; sheetHead.style.alignItems = Align.Center; sheetHead.style.height = 28; sheetHead.style.paddingLeft = 4;
            sheetCameo = E(sheetHead, null, w: 24, h: 24);
            var arrow = P(sheetHead, 0, 0, 14, 14, (p, r) => { p.fillColor = TurqLight; p.BeginPath(); p.MoveTo(new Vector2(2, 3)); p.LineTo(new Vector2(12, 7)); p.LineTo(new Vector2(2, 11)); p.ClosePath(); p.Fill(); });
            arrow.style.position = Position.Relative; arrow.style.left = StyleKeyword.Auto; arrow.style.top = StyleKeyword.Auto; arrow.style.marginLeft = 6; arrow.style.marginRight = 6;
            sheetBadge = E(sheetHead, null, w: 20, h: 20); sheetName = L(sheetHead, "panel-title"); sheetName.style.marginLeft = 6; sheetName.style.fontSize = 15; E(sheetHead).style.flexGrow = 1;
            var sx = E(sheetHead, "s-btn_close_normal btn_close", w: 16, h: 16, pick: true); sx.style.marginRight = 8; sx.style.marginTop = 6; /* Glyph round 2: clear of the corner screw */ sx.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ClosePanel(); e.StopPropagation(); } });
            manifest.Add((sheetCameo, "image", () => "agent cameo")); manifest.Add((sheetBadge, "image", () => "target badge"));
            sheetNote = L(sheet, "verb-name"); sheetNote.style.marginLeft = 8; sheetNote.style.marginTop = 4;
            sheetRows = E(sheet); sheetRows.style.marginTop = 4;
            sheetTip = E(root, "s-panel_dark", 0, 0, 300, float.NaN); sheetTip.style.paddingLeft = 14; sheetTip.style.paddingRight = 14; sheetTip.style.paddingTop = 10; sheetTip.style.paddingBottom = 10;
            sheetTipText = L(sheetTip, "ticker"); sheetTipText.style.whiteSpace = WhiteSpace.Normal;
            // the price chip at the cursor: the same line, led by the verb's glyph
            chip2 = E(root, "s-chip_price", 0, 0, float.NaN, 40); chip2.style.flexDirection = FlexDirection.Row; chip2.style.alignItems = Align.Center; chip2.style.paddingLeft = 18; chip2.style.paddingRight = 18; chip2.style.paddingBottom = 9;
            chipLead = E(chip2, null, w: 18, h: 18); chipFields = E(chip2); chipFields.style.flexDirection = FlexDirection.Row; chipFields.style.alignItems = Align.Center;
            chipRefused = L(chip2, "chip-value"); chipRefused.style.marginLeft = 8;
            // the response pill as icons and numbers
            respIcons = E(respPill); respIcons.style.flexDirection = FlexDirection.Row; respIcons.style.alignItems = Align.Center;
            // Sky: the shields and the "IIII" didn't read. The pill shows only while units are on their way: Glyph's arrive glyph, how many, how soon
            if (respPill.childCount > 0) Show(respPill[0], false);
            var ar = E(respIcons, null, w: Glyph, h: Glyph); Icon(ar, "arrive", Cream); respCome = L(respIcons, "verb-name"); respCome.style.marginLeft = 6; respCome.style.fontSize = 13;
            var un = E(respIcons, null, w: 14, h: 14); Icon(un, "units", Cream); un.style.marginLeft = 6; un.SendToBack(); respIcons.Remove(un); respIcons.Insert(1, un); // Glyph 016 r2: what the number counts (units coming)
            var tm = E(respIcons, null, w: 14, h: 14); Icon(tm, "p_time", Cream); tm.style.marginLeft = 12; respOff = L(respIcons, "verb-name"); respOff.style.marginLeft = 5; respOff.style.fontSize = 13; respBot = respOff;
            // the going-loud card's icon row and the allies it loses (crossed)
            loudIcons = E(loud); loudIcons.style.flexDirection = FlexDirection.Row; loudIcons.style.flexWrap = Wrap.Wrap; loudIcons.style.alignItems = Align.Center; loudIcons.style.marginTop = 8;
            loudLost = E(loud); loudLost.style.flexDirection = FlexDirection.Row; loudLost.style.marginTop = 8;
            int holdAt = loud.IndexOf(loudRing.parent); loud.Insert(holdAt, loudIcons); loud.Insert(holdAt + 1, loudLost); // above the hold ring
            loud.style.width = 330;
            // the end card's tally and the briefing's goal and portraits
            var tallyRow = E(end); tallyRow.style.flexDirection = FlexDirection.Row; tallyRow.style.alignItems = Align.Center; tallyRow.style.marginTop = 8; end.Insert(1, tallyRow);
            Label TallyPart(string icon) { var ic = E(tallyRow, null, w: 15, h: 15); Icon(ic, icon, Cream); var l = L(tallyRow, "briefing-fact"); l.style.marginLeft = 5; l.style.marginRight = 18; return l; }
            endTally = TallyPart("p_time"); endCash = TallyPart("p_cash"); endTrail = TallyPart("p_trail");
            // Standing: ▲ or ▼ by the move, the band and the signed number ("▲ Hero +3"); the sentence is the debrief's
            endStandArrow = P(tallyRow, 0, 0, 10, 10, (p, r) => { if (endStandDir == 0) return; p.fillColor = Cream; p.BeginPath(); if (endStandDir > 0) { p.MoveTo(new Vector2(5, 1)); p.LineTo(new Vector2(10, 9)); p.LineTo(new Vector2(0, 9)); } else { p.MoveTo(new Vector2(0, 1)); p.LineTo(new Vector2(10, 1)); p.LineTo(new Vector2(5, 9)); } p.ClosePath(); p.Fill(); });
            endStandArrow.style.position = Position.Relative; endStandArrow.style.left = StyleKeyword.Auto; endStandArrow.style.top = StyleKeyword.Auto; endStandArrow.style.marginRight = 5;
            endStand = L(tallyRow, "briefing-fact");
            briefGoal = L(brief, "objective"); brief.Insert(2, briefGoal); briefGoal.style.marginTop = 14; briefGoal.style.fontSize = 18;
            briefPeople = E(brief); briefPeople.style.flexDirection = FlexDirection.Row; briefPeople.style.marginTop = 14; brief.Insert(3, briefPeople);
            foreach (var role in new[] { "scout", "fixer", "tech", "enforcer" })
            {
                var c = E(briefPeople); c.style.alignItems = Align.Center; c.style.marginRight = 22;
                Bg(E(c, null, w: 56, h: 56), $"portraits/cameo_{role}_normal"); var nl = L(c, "verb-name"); nl.text = host.Text?.Speaker(role) ?? role; nl.style.marginTop = 4;
                briefPortraits.Add(c);
            }
            // Intel: a person's row finds them in the street (Sky: find a person from Intel)
            string[] intelRoles = { "waitress", "driver", "guard", "councillor" };
            for (int i = 0; i < 4; i++) { int k = i; intelRows[i].Row.pickingMode = PickingMode.Position; intelRows[i].Row.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { FindPerson(intelRoles[k]); e.StopPropagation(); } }); }
            manifest.Add((endTitle, "beat", () => endTitle.text)); // the end card's one line (Fable §3 end)
            manifest.Add((briefGoal, "line", () => briefGoal.text)); manifest.Add((briefPeople, "image", () => "cell portraits"));
            BuildScope();
            BuildMenu();
        }

        bool goalHover; int busyHover = -1; string goalText = "";

        // ---------------------------------------------------------------- per frame

        void LessFrame()
        {
            if (!lessBuilt) BuildLess();
            if (host.Game != runSeen) { runSeen = host.Game; hintShows.Clear(); objWas = ""; markAt = -100f; lastTickSeen = -1; pulseAt.Clear(); } // a new run (Begin, Replay): the HUD's per-run memory starts again
            var g = host.Game; bool alt = host.Peeking, ended = m.Ended; var cam = host.View;
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) || size.x <= 0 ? 1920 : size.x, H = float.IsNaN(size.y) || size.y <= 0 ? 1080 : size.y;
            // what is cut at rest (Glyph §6 "cut at rest"): strips, ticker, the feed stack, the objective bar, the NEXT STEP plate, toasts, the old
            // chip, the old card panel, the arrival chips with words
            for (int i = 0; i < 4; i++) Show(strips[i].Root, false);
            foreach (var e in new[] { ticker, feed, objective, hint, hintChip, toast, chip, panel, intelStrip, respLine, wayRow }) Show(e, false);
            Show(tooltip, false);
            foreach (var u in edgeMarks) Show(u.Root, false);
            foreach (var u in unknownMarks) Show(u, false);
            worldUsed = 0;
            // roster: the column at 0.8; the plate beside the selected portrait; busy chips beside busy portraits
            if (m.Dock == RosterDock.Left)
            {
                int a = m.Agent; float colW = 109 * RosterScale;
                float WinMid(int i) => ColY + ColumnBezels[i].y * RosterScale; // the bezel's measured centre
                float plateX = ColX + colW + 4, plateY = WinMid(a) - PlateH / 2; // brief 014: chips and the plate share one line, 4 off the column
                verbPlate.style.left = plateX; verbPlate.style.top = plateY;
                PlateFold(plateX, WinMid(a)); bool folded = PlateFolded;
                for (int k = 0; k < 4; k++)
                {
                    bool has = m.VerbHas[k]; knobs[k].style.opacity = has ? 1f : 0.28f; Show(knobIcons[k], has); Show(verbPlate.Q("badge" + k), has); // an empty knob: a quiet socket
                    Class(knobs[k], "selected", has && host.Picked == k); // the turquoise ring means "armed: a right click gives this"; nothing else
                }
                if (host.Picked < 0) Show(armedPlate, false); // the name plate only while a verb is armed (Glyph §1)
                else if (m.TakedownArmed) Text(armedName, host.Text != null && host.Text.Actions.TryGetValue("takedown", out var tda) && tda.Ways.TryGetValue(m.Way, out var wy) ? wy.label : m.Way == "kill" ? "Kill" : "Knock out"); // the way, not the verb
                if (armedPlate.style.display != DisplayStyle.None)
                {
                    float ax = folded ? plateX : plateX + KnobX0 + 24 + m.VerbLit * 47.6f - 40, ay = folded ? WinMid(a) + StubH / 2 + 4 : plateY + PlateH + 2;
                    float aw = float.IsNaN(armedPlate.resolvedStyle.width) ? 120 : armedPlate.resolvedStyle.width, ah = 32;
                    for (int j = 0; j < 4; j++) // Glyph 016 r3: 6 px clear of a timer pill under it (it rises; the plate's lower rim may go under it)
                    {
                        var br = busy[j].root; if (br.resolvedStyle.display == DisplayStyle.None) continue;
                        float bx2 = br.style.left.value.value, by2 = br.style.top.value.value, bw2 = br.resolvedStyle.width, bh2 = br.resolvedStyle.height; if (float.IsNaN(bw2) || float.IsNaN(bh2)) continue;
                        if (ax < bx2 + bw2 && ax + aw > bx2 && ay < by2 + bh2 && ay + ah + 6 > by2) ay = by2 - 6 - ah;
                    }
                    armedPlate.style.left = ax; armedPlate.style.top = ay;
                }
                for (int i = 0; i < 4; i++)
                {
                    var s = m.Slots[i]; var (r, ic, tx, bar, fill, x) = busy[i];
                    bool on = (s.Busy || s.Down || s.Pace.Length > 0) && !ended; Show(r, on); if (!on) continue;
                    float bx = i == a ? plateX + (folded ? StubW + 4 : PlateW + 16 + 6) : ColX + colW + 4; /* Glyph fight r1 #6: 6 px clear of the fold tab (it stands 16 px out of the plate) */ r.style.left = bx; r.style.top = WinMid(i) - ChipH / 2; // anchored to its portrait's row (or after the plate)
                    bool moving = s.Phase is "walking" or "running", running = s.Phase == "running";
                    Icon(ic, s.Down ? "s_downed" : moving ? (running ? "run" : "s_moving") : s.Icon, running ? TurqLight : Cream);
                    r.style.unityBackgroundImageTintColor = running ? new Color(0.78f, 0.97f, 0.96f) : Color.white; fill.style.backgroundColor = running ? Cream : TurqLight; // run: a lighter plate and a cream bar (kit tones, never a signal)
                    string eye = running ? host.RunEye(i) : ""; var eb = busyEyes[i]; Show(eb, eye.Length > 0); // a run is noticed: who could see it, as on the price line
                    if (eye.Length > 0) Icon(eb[0], "eye_" + eye, eye == "open" ? new Color(0.878f, 0.227f, 0.184f) : Cream);
                    bool carry = s.Pace.Length > 0 && s.Progress < 0;
                    bar.style.visibility = s.Progress >= 0 && !s.Down ? Visibility.Visible : Visibility.Hidden; fill.style.width = Length.Percent(100f * Mathf.Clamp01(s.Progress / 1000f)); // the slot keeps its place
                    Text(tx, s.Down ? "" : carry ? System.Text.RegularExpressions.Regex.Match(s.Pace, @"[\d.]+ m/s").Value : s.Left);
                    if (Stress && tx.text.Length > 0) Text(tx, carry ? "1.4 m/s" : "88 s");
                    bool iconOnly = (s.Progress < 0 || s.Down) && tx.text.Length == 0; // no end ("holding the loop"), or down: the icon alone (Glyph §1)
                    float cw = iconOnly ? IconOnlyW : ChipW; r.style.width = cw; Show(bar, !iconOnly); Show(tx, !iconOnly); // one width: the bar takes what the eye does not
                    float isz = iconOnly ? 13 : Glyph; ic.style.width = isz; ic.style.height = isz; r.style.paddingLeft = iconOnly ? (IconOnlyW - isz) / 2 : ChipPad; r.style.paddingRight = iconOnly ? (IconOnlyW - isz) / 2 : ChipPad; /* Glyph round 3: the loop glyph centred at ~75% in the small chip */
                    bool canX = s.CanCancel && (busyHover == i || tipFor == i); x.style.left = cw + 4;
                    Show(x, canX);
                    if (canX) { bool costly = !(s.CancelTitle.Length == 0) && host.DoingOf(i) is var d && !(d.Phase is "walking" or "running") && !IsFree(d.CancelCode); x.style.unityBackgroundImageTintColor = costly ? TurqLight : Color.white; }
                }
            }
            else foreach (var b in busy) Show(b.root, false);
            // cancel tooltip: 1 to 3 words (less.cancel); the long line on Alt
            if (tipFor >= 0 && host.Text != null)
            {
                var d = host.DoingOf(tipFor); string shortCost = host.Text.CancelShort.TryGetValue(d.CancelCode ?? "", out var cs) ? cs : "";
                bool show = alt || shortCost.Length > 0; Show(sheetTip, show && !m.PanelOpen);
                if (show && !m.PanelOpen) { Text(sheetTipText, alt ? m.Slots[tipFor].CancelBody : shortCost); var p = RuntimePanelUtils.ScreenToPanel(root.panel, m.Mouse); sheetTip.style.left = p.x + 14; sheetTip.style.top = p.y + 12; sheetTip.style.width = alt ? 300 : StyleKeyword.Auto; }
            }
            // the pill: clock, pause glyph (or speed), next mark as icon and time ("?" when unknown; its word on Alt), $ and Trail dimming
            Show(pauseIcon, host.Paused); Text(state, host.Speed > 1 ? $"x{host.Speed}" : ""); Show(state, host.Speed > 1);
            {
                bool unknown = m.NextLabel == "?"; string shortLabel = "";
                if (host.Text != null && host.Text.TimelineShort.TryGetValue(m.NextTime, out var sl)) shortLabel = sl;
                Show(nextUnknown, unknown); Show(nextLabel, alt && !unknown); Text(nextLabel, shortLabel.Length > 0 ? shortLabel : m.NextLabel); nextLabel.style.marginLeft = 9; nextLabel.style.marginRight = 0; nextTime.style.marginLeft = 9; // told and learned: unknown stays "?" on Alt too
                bool tipOn = pillHover && !m.Ended; Show(nextTip, tipOn);
                if (tipOn) { Text(nextTipText, unknown ? "Next · ?" : $"Next · {(shortLabel.Length > 0 ? shortLabel : m.NextLabel)}"); nextTip.style.right = 14; nextTip.style.top = 14 + 44 + 8 + 36 + 8; }
                if (m.Cash != lastCash) { lastCash = m.Cash; cashChanged = Time.unscaledTime; }
                if (m.Trail != lastTrail) { lastTrail = m.Trail; trailChanged = Time.unscaledTime; }
                bool lit = alt || pillHover; cash.style.opacity = lit || Time.unscaledTime - cashChanged < 4f ? 1f : 0.55f; trail.style.opacity = lit || Time.unscaledTime - trailChanged < 4f ? 1f : 0.55f;
            }
            Show(keys, !ended && !m.BriefingOpen); Show(historyKey2, !ended && !m.BriefingOpen); Show(keyDot, m.Unread > 0); historyKey2.style.width = m.Unread > 0 ? ChipH + 10 : ChipH;
            { string[] people = { "waitress", "guard", "driver", "councillor" }; for (int i = 0; i < 4; i++) { bool kn = KnownPerson(people[i]); peopleDiscs[i].style.opacity = kn ? 1f : 0.28f; peopleDiscs[i].style.unityBackgroundImageTintColor = kn ? Color.white : new Color(0.55f, 0.6f, 0.6f); } }
            intel.style.top = 14 + 44 + 8 + 36 + 8;
            // Intel rows: the sentence column is Alt only
            if (m.IntelOpen) for (int i = 0; i < 4; i++) { var u = intelRows[i]; Show(u.Next, alt); Show(u.Secret, alt && m.Intel[i].Secret.Length > 0); }
            // the response pill: icons and counts; its rows on R or a click (a drawer)
            bool coming = host.RespComing > 0 && !ended; Show(respPill, coming);
            if (coming) { Text(respCome, host.RespComing.ToString()); Text(respOff, host.RespSoonestS >= 0 ? $"{host.RespSoonestS} s" : "?"); respPill.style.width = StyleKeyword.Auto; respPill.style.height = ChipH; respPill.style.paddingLeft = ChipPad; respPill.style.paddingRight = ChipPad; /* Glyph round 2: the arrow clear of the plate's rounded cap */ respPill.RemoveFromClassList("s-panel_strip"); respPill.AddToClassList("s-plate_dark"); }
            Show(respList, host.RespAny && m.ResponseOpen); /* a drawer (the pill or R), never on Alt: its four rows are a table, and Alt is fragments (the budget's block rule) */
            if (coming) respPill.style.top = m.IntelOpen ? intel.resolvedStyle.top + (float.IsNaN(intel.resolvedStyle.height) ? 0 : intel.resolvedStyle.height) + 8 : 14 + 44 + 8 + 36 + 8;
            // the history drawer over the beats' place
            history.style.bottom = 14 + 36 + 8; // opens above its key
            GoalLine(W);
            Beats();
            Bubbles(cam);
            Pops(cam);
            Sheet(alt, W, H);
            Chip(alt, W);
            StreetLess(alt, cam);
            Pips(cam);
            foreach (var (br, _, _) in bubbleEls) if (br.resolvedStyle.display != DisplayStyle.None) br.BringToFront(); // Glyph 016 r2 B: speech above the pins
            ClearUnderChip(alt);
            Edges(cam, W, H);
            LoudLess(alt);
            BriefLess(alt);
            EndLess();
            ScopeFrame();
            IntelFrame();
            for (int i = worldUsed; i < worldEls.Count; i++) Show(worldEls[i], false);
            world.MarkDirtyRepaint();
        }

        bool InView(Vector2 q) { var sz = root.panel.visualTree.layout.size; return q.x > 0 && q.y > 0 && q.x < sz.x && q.y < sz.y; }

        /// <summary>The chips' rule (Sky 2026-10-16): plate_dark's chrome ends 6.5 px in (measured on the art at 1x); every glyph's whole box sits
        /// 4 px clear of it, so a chip is the glyph + 21 px tall; both ends pad alike. Busy chip: pad 16 | icon 14 | 8 | bar (the slack) | 8 |
        /// value 44 (88 s, 1.4 m/s) | 8 | eye 14 | pad 16.</summary>
        public const float RimIn = 6.5f, Clear = 4f, Glyph = 14f, ChipPad = 16f, ValueW = 44f;
        const float GoalH = 40, ChipH = 36, ChipW = 16 + 14 + 8 + 48 + 8 + 44 + 8 + 14 + 16, IconOnlyW = 36, EyeW = 0;
        readonly List<VisualElement> busyEyes = new(); // 12 + icon 16 + 8 + bar 40 + 8 + value 40 + 12 + 4 to spare

        static bool IsFree(string code) => code is "" or "stop" or "holster" or "cease" or "nothing" or "none" or "down" or "quiet";

        // ---------------------------------------------------------------- the goal line (objective and hint share one place)

        readonly Dictionary<string, (float first, int shows, float lastEnd)> hintShows = new();
        /// <summary>The hint line now: its fade (0 = folded), the verb's glyph and the noun's marker colour, which its ring in the street shares.</summary>
        Game runSeen; float hintAlpha; bool hintQOn; string hintVerbGlyph = ""; Color hintNounColour = TurqLight; VisualElement hintQ;
        static bool show0(string l) => l != null;
        string objWas = ""; float objChanged = -100f, markAt = -100f; int lastTickSeen = -1;

        void GoalLine(float W)
        {
            var t = host.Text; var g = host.Game; float now = Time.unscaledTime;
            if (t == null || m.Ended || m.BriefingOpen) { Show(goal, false); goalText = ""; return; }
            string obj = host.ObjectiveKey switch { "carry" => t.ObjCarry, "bring" => t.ObjBring, _ => t.ObjTake }; // Fable's steps: take, bring, carry
            if (obj != objWas) { objWas = obj; objChanged = now; }
            // the marks where a window opens: the chime (5:30) and Vale out (5:50)
            foreach (var mk in new[] { 330 * Game.Hz, 350 * Game.Hz }) if (lastTickSeen >= 0 && lastTickSeen < mk && g.Tick >= mk) markAt = now;
            lastTickSeen = g.Tick;
            float showS = t.ObjShowS;
            bool objOn = now - host.ClockStarted < showS || now - objChanged < showS && host.ClockStarted > 0 || now - markAt < showS || pillHover;
            string line = null, cameo = null, glyph = null; hintAlpha = 0f; hintQOn = false;
            if (objOn) line = obj;
            else if (host.HintNow != null && t.Hints2.Find(x => x.Id == host.HintNow.Id) is PlayText.Hint h && h.Short.Length > 0)
            {
                if (!hintShows.TryGetValue(h.Id, out var hs)) { hs = (now, 1, -1f); hintShows[h.Id] = hs; }
                float age = now - hs.first;
                bool on = age < t.HintShowS;
                if (!on && hs.lastEnd < 0) { hs.lastEnd = hs.first + t.HintShowS; hintShows[h.Id] = hs; }
                if (!on && hs.shows < t.HintMaxShows && now - hs.lastEnd > t.HintReturnS) { hs = (now, hs.shows + 1, -1f); hintShows[h.Id] = hs; on = true; }
                // reopened (the "?" disc, or H while the line is folded): the line and its ring for another showSeconds
                if (!on && host.HintReopenAt > hs.lastEnd && host.HintReopenAt > hs.first) { hs = (now, hs.shows, -1f); hintShows[h.Id] = hs; host.HintReopenAt = -1f; age = 0f; on = true; }
                if (on || goalHover)
                {
                    float left = t.HintShowS - (now - hs.first); hintAlpha = goalHover ? 1f : Mathf.Clamp01(left / 0.6f); // the line and its ring fade together
                    hintVerbGlyph = HudModel.AtlasIcon(h.Verb, h.Verb, ""); int nb = h.Short.IndexOf('['), ne = h.Short.IndexOf(']');
                    hintNounColour = nb >= 0 && ne > nb ? Noun(h.Short[(nb + 1)..ne].Replace("{who}", host.HintNow.Who)).c : TurqLight;
                    string who = host.HintNow.Who; line = h.Short.Replace("{who}", who.Length == 0 ? "them" : host.Text.TargetNames.ContainsKey(who) ? Cap1(host.Text.TargetName(who, who)) : who); // a common noun ("rival") stays lower case
                    string ag = h.Agent == "any" ? (host.HintNow.Portrait >= 0 ? Op.Name((OpId)host.HintNow.Portrait) : "") : h.Agent;
                    cameo = ag.Length > 0 ? $"portraits/cameo_{ag}_normal" : null; glyph = HudModel.AtlasIcon(h.Verb, h.Verb, "");
                    if (h.Verb == "turn-view" && line.IndexOf('<') < 0 && line.LastIndexOf(':') is int ci and > 0) line = line[..ci] + ": <TurnLeft> <TurnRight>"; // Glyph 016 §3: the keys as keycaps (text.json's "Q / E" until it carries the tokens)
                }
                else hintQOn = true; // folded: a "?" disc at the goal line's place reopens it
            }
            host.HintLineShowing = hintAlpha > 0f;
            Show(hintQ, hintQOn && !show0(line)); if (hintQOn) hintQ.style.left = (W - 26) / 2;
            if (line != null) line = Keys(line);
            bool show = line != null; Show(goal, show); goalText = show ? line.Replace("[", "").Replace("]", "").Replace("<", "").Replace(">", "") : "";
            goal.style.opacity = hintAlpha > 0f ? hintAlpha : 1f;
            if (!show) return;
            Show(goalCameo, cameo != null); if (cameo != null) { Bg(goalCameo, cameo); goalCameo.style.opacity = 0.75f + 0.25f * Mathf.Sin(now * 5f); }
            Show(goalGlyph, glyph != null); if (glyph != null) { float gs = glyph == "turn_view" ? 18f : Glyph; goalGlyph.style.width = gs; goalGlyph.style.height = gs; Icon(goalGlyph, glyph, TurqLight); } // Glyph 016 §3: the turn glyph at 18 px
            if (line != goalKey)
            {
                goalKey = line; goalWords.Clear();
                int i = 0;
                while (i < line.Length)
                {
                    int a = line.IndexOfAny(new[] { '[', '<' }, i);
                    if (a < 0) { Word(goalWords, line[i..], null); break; }
                    if (a > i) Word(goalWords, line[i..a], null);
                    bool key = line[a] == '<'; int b = line.IndexOf(key ? '>' : ']', a); if (b < 0) b = line.Length;
                    if (key) KeyCap(goalWords, line[(a + 1)..b]); else Word(goalWords, line[(a + 1)..b], Noun(line[(a + 1)..b]));
                    i = b + 1;
                }
            }
            goal.style.left = (W - (float.IsNaN(goal.resolvedStyle.width) ? 330 : goal.resolvedStyle.width)) / 2;
        }

        /// <summary>A line's key tokens (`&lt;TurnLeft&gt;`, a PlayAction's name) to the bound key's label (`&lt;Q&gt;`), so a remapped key shows.</summary>
        string Keys(string line)
        {
            int i = 0; var sb = new System.Text.StringBuilder();
            while (i < line.Length)
            {
                int a = line.IndexOf('<', i); if (a < 0) { sb.Append(line, i, line.Length - i); break; }
                int b = line.IndexOf('>', a); if (b < 0) { sb.Append(line, i, line.Length - i); break; }
                sb.Append(line, i, a - i); var name = line[(a + 1)..b];
                sb.Append('<').Append(Enum.TryParse<PlayAction>(name, out var act) ? host.controls.Label(act) : name).Append('>'); i = b + 1;
            }
            return sb.ToString();
        }

        /// <summary>A key in a line: the order plate's light keycap, 22 px wide (Glyph 016 §3).</summary>
        void KeyCap(VisualElement parent, string label)
        {
            var kp = E(parent, "s-plate_light", w: 22, h: 19); kp.style.position = Position.Relative; kp.style.left = StyleKeyword.Auto; kp.style.top = StyleKeyword.Auto; kp.style.marginLeft = 4; kp.style.flexShrink = 0;
            var l = L(kp, "keycap"); l.text = label; l.style.flexGrow = 1; l.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        static string Cap1(string s) => s.Length > 0 && s.StartsWith("the ") ? s : s.Length > 0 ? char.ToUpperInvariant(s[0]) + s[1..] : s;

        /// <summary>A noun's marker colour and shape (Glyph §6): people the cell goes after and places in turquoise-light, a guard or the patrol
        /// amber, rivals violet, cameras red; a triangle for a person, a ring for a place, so colour is never alone.</summary>
        static (Color c, bool person) Noun(string n)
        {
            var l = n.ToLowerInvariant();
            if (l.Contains("rival")) return (Violet, true);
            if (l.Contains("guard") || l.Contains("patrol") || l.Contains("officer")) return (Amber, true);
            if (l.Contains("camera")) return (Red, false);
            bool place = l.Contains("car") && !l.Contains("care") || l.Contains("cabinet") || l.Contains("terrace") || l.Contains("door") || l == "mark" || l == "view"; // "view": the turn hint (H13), a thing, not a person
            return (TurqLight, !place);
        }

        void Word(VisualElement parent, string text, (Color c, bool person)? noun)
        {
            if (noun.HasValue)
            {
                var (c, person) = noun.Value;
                if (!person && text.Trim().ToLowerInvariant() == "car") // Glyph 016 r2 C: the car's glyph, as on the knob and the ground marker (a ring read as an O)
                { var cg = E(parent, null, w: 14, h: 14); cg.style.position = Position.Relative; cg.style.left = StyleKeyword.Auto; cg.style.top = StyleKeyword.Auto; cg.style.marginLeft = 4; cg.style.marginRight = 4; Icon(cg, "car", c); }
                else {
                var shape = P(null, 0, 0, 10, 10, (p, r) =>
                {
                    if (person) { p.fillColor = c; p.BeginPath(); p.MoveTo(new Vector2(0, 1)); p.LineTo(new Vector2(10, 1)); p.LineTo(new Vector2(5, 9)); p.ClosePath(); p.Fill(); }
                    else { p.strokeColor = c; p.lineWidth = 1.8f; p.BeginPath(); p.Arc(new Vector2(5, 5), 3.8f, Deg(0), Deg(360)); p.Stroke(); }
                });
                shape.style.position = Position.Relative; shape.style.left = StyleKeyword.Auto; shape.style.top = StyleKeyword.Auto; shape.style.marginLeft = 4; shape.style.marginRight = 4; parent.Add(shape);
                }
            }
            // Glyph round 2: a word's own leading space becomes a 4 px margin and a trailing one goes, so the gaps round a shape are even
            var trimmed = text.Trim(); if (trimmed.Length == 0) return;
            var l = L(parent, "objective"); l.text = trimmed.Replace(' ', ' '); l.style.fontSize = 15; if (noun.HasValue) l.style.color = noun.Value.c == Violet ? VioletText : noun.Value.c; // Glyph fight r1 #3: violet words lighter (#b894ec), the pictogram keeps #8a4fd0
            if (text.StartsWith(" ")) l.style.marginLeft = 4; // (a trailing space is dropped: the next noun's shape carries its own 4 px)
        }

        // ---------------------------------------------------------------- beats, bubbles, pops

        void Beats()
        {
            host.DecideBeats(host.View);
            float now = Time.unscaledTime, life = host.Text?.FeedS ?? 4f; int max = Mathf.Min(host.Text?.FeedMax ?? 2, beatEls.Count);
            var live = new List<HandoverHost.StoryBeat>();
            for (int i = host.Beats.Count - 1; i >= 0 && live.Count < max; i--) { var b = host.Beats[i]; if (b.Decided && b.Show && now - b.Born < life + 0.25f) live.Insert(0, b); }
            bool on = !m.HistoryOpen && !m.Ended;
            for (int i = 0; i < beatEls.Count; i++)
            {
                var (r, bd, tx) = beatEls[i]; bool has = on && i < live.Count; Show(r, has); if (!has) continue;
                var b = live[i]; Art(bd, b.Badge, b.Tinted ? Cream : (Color?)null); Text(tx, b.Text);
                float age = now - b.Born; r.style.opacity = Mathf.Clamp01((life + 0.25f - age) / 0.4f) * Mathf.Clamp01((age - 0.25f) / 0.2f);
            }
        }

        readonly List<Rect> bubbleRects = new();
        static readonly HashSet<string> Cell = new() { "scout", "fixer", "tech", "enforcer" };

        void Bubbles(Camera cam)
        {
            float now = Time.unscaledTime, life = host.Text?.BubbleS ?? 3f; int max = host.Text?.BubbleMax ?? 2;
            var live = new List<(HandoverHost.Bubble b, Vector2 at)>();
            for (int i = host.Bubbles.Count - 1; i >= 0 && live.Count < max; i--)
            {
                var b = host.Bubbles[i]; if (now - b.Born > life) break;
                if (host.BubbleWorld(b) is not Vector3 w || !Proj(w, out var p)) continue;
                bool dup = false; foreach (var x in live) if (x.b.Speaker == b.Speaker) dup = true; if (dup) continue; // one bubble a speaker: the newest
                live.Add((b, p));
            }
            while (bubbleEls.Count < max)
            {
                var r = E(marks, "s-plate_light"); r.style.position = Position.Absolute; /* placed by left/top: in the flow, a second bubble sat a bubble's height off where it was put (the 5 px "stack") */ r.style.paddingLeft = 10; r.style.paddingRight = 10; r.style.paddingTop = 4; r.style.paddingBottom = 5; r.style.maxWidth = 250;
                r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center;
                var dot = E(r, null, w: 7, h: 7); Round(dot, 3.5f); dot.style.marginRight = 6; dot.name = "who"; // who speaks: a dot in their colour (a border drew a box outside the pill art)
                var t = L(r, "card-body"); t.style.whiteSpace = WhiteSpace.Normal; t.style.color = Ink; t.style.fontSize = 13;
                var tail = E(r, null, 0, 0, 9, 9); tail.style.rotate = new Rotate(45); tail.style.backgroundColor = new Color(0.98f, 0.95f, 0.89f); // a child of the bubble: it can't come apart
                tail.style.left = Length.Percent(50); tail.style.marginLeft = -4.5f; tail.style.top = Length.Percent(100); tail.style.marginTop = -6;
                int bi = bubbleEls.Count; bubbleEls.Add((r, t, tail)); manifest.Add((r, "speech", () => bubbleEls[bi].text.text));
            }
            bubbleRects.Clear();
            for (int i = 0; i < bubbleEls.Count; i++)
            {
                var (r, t, tail) = bubbleEls[i]; bool has = i < live.Count && !m.Ended; Show(r, has); Show(tail, has); if (!has) continue;
                var (b, p) = live[i]; Text(t, b.Text);
                t.style.whiteSpace = WhiteSpace.NoWrap; r.style.maxWidth = StyleKeyword.None; // one line (Glyph §6: a cream plate, one line, a tail to the head)
                Color rim = Cell.Contains(b.Speaker) ? Turq : b.Speaker is "guard" or "patrol" or "security" ? Amber : b.Speaker == "rivals" ? Violet : Color.clear;
                var who = r.Q("who"); Show(who, rim.a > 0); who.style.backgroundColor = rim;
                float w = r.resolvedStyle.width, h = r.resolvedStyle.height; if (float.IsNaN(w)) w = 160; if (float.IsNaN(h)) h = 26;
                float y = p.y - h - 34, x = p.x - w / 2;
                const float BubbleGap = 14; // 2026-10-18: at least 10 px between stacked bubbles as drawn (the plate art's rim eats ~4), so each reads, and is counted, as its own; the newest nearest the speaker
                for (int guard = 0; guard < 4; guard++) { bool hit = false; foreach (var pr in bubbleRects) if (pr.Overlaps(new Rect(x - BubbleGap / 2, y - BubbleGap / 2, w + BubbleGap, h + BubbleGap))) { y = pr.y - h - BubbleGap; hit = true; } if (!hit) break; }
                bubbleRects.Add(new Rect(x, y, w, h));
                r.style.left = x; r.style.top = y;
                float age = Time.unscaledTime - b.Born; r.style.opacity = tail.style.opacity = Mathf.Clamp01((life - age) / 0.3f);
            }
        }

        void Pops(Camera cam)
        {
            float now = Time.unscaledTime; bool alt = host.Peeking; var list = new List<HandoverHost.PopUp>();
            for (int i = m.Pops.Count - 1; i >= 0 && list.Count < 3; i--) if (now <= m.Pops[i].Until) list.Add(m.Pops[i]);
            while (popEls.Count < 3)
            {
                var r = E(marks, "s-plate_dark"); r.style.position = Position.Absolute; r.style.height = ChipH; r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.paddingLeft = ChipPad; r.style.paddingRight = ChipPad; // the chips' rule
                var ic = E(r, null, w: Glyph, h: Glyph); ic.style.marginRight = 8; var t = L(r, "verb-name"); t.style.fontSize = 13; t.style.whiteSpace = WhiteSpace.Normal; t.style.maxWidth = 320;
                popEls.Add((r, ic, t));
            }
            float stack = 0;
            for (int i = 0; i < popEls.Count; i++)
            {
                var (r, ic, t) = popEls[i]; bool has = i < list.Count; Show(r, has); if (!has) continue;
                var pp = list[i]; Vector2 at;
                if (pp.World.HasValue && Proj(pp.World.Value + Vector3.up * 2.9f, out var wp)) at = wp;
                else if (pp.Screen.HasValue) { at = RuntimePanelUtils.ScreenToPanel(root.panel, pp.Screen.Value) + new Vector2(18, -36 - stack); stack += 32; }
                else { Show(r, false); continue; }
                Art(ic, pp.Icon.Length > 0 ? pp.Icon : "badges/mark_no", pp.Icon.StartsWith("icons/") ? Cream : (Color?)null);
                Text(t, alt && pp.Long.Length > 0 ? pp.Long : pp.Text); r.style.height = alt ? StyleKeyword.Auto : ChipH;
                float w = r.resolvedStyle.width; if (float.IsNaN(w)) w = 120;
                r.style.left = pp.Screen.HasValue && !pp.World.HasValue ? at.x : at.x - w / 2; r.style.top = at.y;
                r.style.opacity = Mathf.Clamp01((pp.Until - now) / 0.3f);
                r.style.unityBackgroundImageTintColor = pp.Kind == HandoverHost.Feedback.Refused ? new Color(1f, 0.86f, 0.84f) : Color.white;
                if (r.parent.IndexOf(r) < r.parent.childCount - popEls.Count) r.BringToFront(); // above every world mark (the pool grows after the pops were made)
            }
        }

        // ---------------------------------------------------------------- the order sheet and the chip

        HandoverHost.ActionPanel sheetFor; bool sheetStress; float sheetW = 374; readonly Dictionary<string, float> sheetCols = new(); const float ColGap = 10;
        Label probe;

        /// <summary>A text's width in a type style (the probe label sits in the panel, invisible), in panel px.</summary>
        float Measure(string text, string style, float size)
        {
            if (probe == null) { probe = L(root, style); probe.style.position = Position.Absolute; probe.style.visibility = Visibility.Hidden; probe.style.left = -2000; }
            probe.ClearClassList(); probe.AddToClassList("t-" + style); probe.style.fontSize = size; probe.style.unityFontStyleAndWeight = FontStyle.Bold;
            return probe.MeasureTextSize(text ?? "", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
        }

        VisualElement eyeTip; Label eyeTipText;
        /// <summary>The eye's one-line hover tip, above the glyph.</summary>
        void EyeTipShow(VisualElement over, string text)
        {
            if (eyeTip == null) { eyeTip = E(root, "s-plate_dark"); eyeTip.style.position = Position.Absolute; eyeTip.style.height = 26; eyeTip.style.paddingLeft = 12; eyeTip.style.paddingRight = 12; eyeTip.style.justifyContent = Justify.Center; eyeTipText = L(eyeTip, "verb-name"); }
            Show(eyeTip, over != null); if (over == null) return;
            Text(eyeTipText, text); var b = over.worldBound; var rb = root.worldBound; eyeTip.style.left = b.center.x - rb.x - 40; eyeTip.style.top = b.y - rb.y - 32; eyeTip.BringToFront();
        }

        /// <summary>A price field's width in a row: its glyphs and its text.</summary>
        float FieldWidth(HudModel.PField f)
        {
            const float ic = 14; float w = f.Found ? ic + 3 : 0;
            switch (f.Kind)
            {
                case "eye": return w + 18;
                case "mark": return w + ic + 2;
                case "learns": return w + f.Icons.Count * (ic + 6);
            }
            return w + (f.Icon.Length > 0 ? ic + 3 : 0) + Mathf.Ceil(Measure(f.Text, "card-price", 13f)) + 1;
        }

        void Sheet(bool alt, float W, float H)
        {
            var pn = host.Panel; Show(sheet, pn != null && !m.Ended);
            if (pn == null || m.Ended) { sheetFor = null; if (tipFor < 0) Show(sheetTip, false); return; }
            if (sheetFor != pn || sheetStress != Stress)
            {
                sheetFor = pn; sheetStress = Stress; int first = Array.IndexOf(host.Selected, true);
                Bg(sheetCameo, $"portraits/cameo_{(first >= 0 ? Op.Name((OpId)first) : "scout")}_normal");
                Show(sheetBadge, m.PanelBadge.Length > 0); if (m.PanelBadge.Length > 0) Bg(sheetBadge, m.PanelBadge);
                Text(sheetName, OneName(pn.T));
                bool none = pn.Rows.Count == 0; Show(sheetNote, none);
                Text(sheetNote, pn.T.Kind == "crowd" ? (host.Text != null && host.Text.Pops.TryGetValue("passer-by", out var pb) ? pb : "Passer-by") : (host.Text != null && host.Text.Pops.TryGetValue("nothing-here", out var nh) ? nh : "Nothing here"));
                while (rows.Count < pn.Rows.Count) NewRow();
                bool many = false; for (int i = 1; i < pn.Rows.Count; i++) if (pn.Rows[i].By != pn.Rows[0].By) many = true;
                // Sky 2026-10-16 (Inez's card wrapped "Pa / y", "100,00 / 0"): fixed column slots per field across the card, each the widest value
                // any row needs, the name slot the widest name; nothing shrinks and nothing wraps; the card is as wide as its widest row
                var lines = new List<List<HudModel.PField>>();
                foreach (var r0 in pn.Rows)
                {   // Glyph fight r1 #4: a refused row never shows the "works" tick beside its lock
                    var pl = HudModel.PriceLine(host, r0.I);
                    if (!r0.Available) pl = pl.ConvertAll(f => f.Kind == "mark" ? new HudModel.PField { Kind = "mark", Mark = "no" } : f);
                    lines.Add(pl);
                }
                sheetCols.Clear(); foreach (var fl in lines) foreach (var f in fl) { float wv = FieldWidth(f); if (!sheetCols.TryGetValue(f.Kind, out var cw0) || wv > cw0) sheetCols[f.Kind] = wv; }
                float nameW = 0; foreach (var r0 in pn.Rows) nameW = Mathf.Max(nameW, Measure(HudModel.ShortName(host, r0.I), "card-title", 13.5f));
                bool anyTag = pn.Rows.Exists(r0 => HudModel.Tag(r0.I).Length > 0);
                float colsW = 0; foreach (var kind in HudModel.FieldOrder) if (sheetCols.TryGetValue(kind, out var cw1)) colsW += cw1 + ColGap;
                sheetW = 22 + 22 + 10 + 14 + (many ? 22 : 0) + 16 + 6 + Mathf.Ceil(nameW) + (anyTag ? 22 : 0) + 12 + colsW + 8 + 18;
                sheetW = Mathf.Max(sheetW, 240); sheet.style.width = sheetW;
                for (int i = 0; i < rows.Count; i++)
                {
                    var u = rows[i]; Show(u.Root, i < pn.Rows.Count); if (i >= pn.Rows.Count) continue;
                    var r = pn.Rows[i]; var it = r.I;
                    Show(u.Cameo, many); if (many) Bg(u.Cameo, $"portraits/cameo_{Op.Name(r.By)}_normal");
                    Icon(u.Glyph, r.Available ? (it.Lead.Length > 0 ? it.Lead : HudModel.AtlasIcon(it.Order.Do, it.Order.Approach, "")) : "lock", Ink);
                    Text(u.Name, HudModel.ShortName(host, it)); u.Name.style.width = Mathf.Ceil(nameW) + 1; u.Name.style.flexShrink = 0; var tag = HudModel.Tag(it);
                    Show(u.TagIcon, anyTag); u.TagIcon.style.visibility = tag.Length > 0 ? Visibility.Visible : Visibility.Hidden; // the cabinet's loops: which camera, as a glyph (Glyph: no word)
                    if (tag.Length > 0) Art(u.TagIcon, tag == "entrance" ? "icons/singles/enter" : tag == "pickup" ? "icons/singles/car" : "badges/signal_camera", Ink);
                    u.FieldsKey = ""; Fields(u.Fields, lines[i], Ink, ref u.FieldsKey, false, cols: sheetCols);
                    bool key = r.Key >= 0 && host.Selected[(int)r.By]; u.Key.style.visibility = key ? Visibility.Visible : Visibility.Hidden; Text(u.KeyText, key ? host.VerbKey(r.Key) : "0"); // the slot is kept, so every row's columns align
                    u.Root.style.opacity = r.Available ? 1f : 0.45f;
                }
            }
            // Alt: the fragments under each way; Alt + hover: the old sentence beside the sheet
            for (int i = 0; i < pn.Rows.Count && i < rows.Count; i++)
            {
                var u = rows[i]; Show(u.Alt, alt);
                if (alt) Fields(u.Alt, HudModel.AltLine(host, pn.Rows[i].I), new Color(0.27f, 0.32f, 0.31f), ref u.AltKey, false, small: true);
                Lift(u.Root, hoverRow == i);
            }
            var at = RuntimePanelUtils.ScreenToPanel(root.panel, m.PanelAt);
            float hgt = sheet.resolvedStyle.height; if (float.IsNaN(hgt) || hgt <= 0) hgt = 160;
            float left = Mathf.Clamp(at.x + 30, 14, Mathf.Max(14, W - sheetW - 14)), top = Mathf.Clamp(at.y - 4, 14, Mathf.Max(14, H - hgt - 14)); // Glyph 016 r2 H: 16 px right, 10 px down, off the target's feet
            {   // Glyph fight r1 #4: the panel opens to the side away from a step marker it would cover (the car's, the door's)
                var sr = new Rect(left, top, sheetW, hgt);
                foreach (var mk in new[] { carMark, heldMark, doorMark })
                {
                    if (mk == null || mk.resolvedStyle.display == DisplayStyle.None || mk.worldBound.width <= 0) continue;
                    var mr = root.WorldToLocal(mk.worldBound); mr.xMin -= 8; mr.xMax += 8; mr.yMin -= 8; mr.yMax += 8;
                    if (!sr.Overlaps(mr)) continue;
                    float l2 = mr.center.x <= at.x + 30 ? Mathf.Max(left, mr.xMax) : mr.xMin - sheetW; // marker left of the click: past it on the right; else to its left
                    if (l2 + sheetW > W - 14) l2 = mr.xMin - sheetW;
                    left = Mathf.Clamp(l2, 14, Mathf.Max(14, W - sheetW - 14)); sr.x = left;
                }
            }
            sheet.style.left = left; sheet.style.top = top;
            bool tip = alt && hoverRow >= 0 && hoverRow < pn.Rows.Count; if (tipFor < 0) Show(sheetTip, tip);
            if (tip)
            {
                var r = pn.Rows[hoverRow]; string line = r.Available ? r.I.Does : r.Unlock.Length > 0 ? r.Unlock : r.Reason;
                if (r.I.Rows.Count > 0) foreach (var (_, text) in r.I.Rows) line += "\n" + text;
                Text(sheetTipText, line); sheetTip.style.width = 300;
                sheetTip.style.left = left + sheetW + 8 > W - 314 ? left - 308 : left + sheetW + 8; sheetTip.style.top = top + 36 + hoverRow * 40;
            }
        }

        static string OneName(HandoverHost.Target t)
        {
            var s = (t.Label ?? "").Trim(); int sp = s.LastIndexOf(' '); if (sp >= 0) s = s[(sp + 1)..]; // one word: the badge says the rest
            if (t.Kind == "rivals") s = "Rivals";
            return s.Length > 0 ? char.ToUpperInvariant(s[0]) + s[1..] : s;
        }

        void NewRow()
        {
            var u = new Row { Index = rows.Count };
            u.Root = E(sheetRows, "s-plate_light", pick: true); u.Root.style.minHeight = 32; u.Root.style.marginTop = 6; u.Root.style.paddingLeft = 10; u.Root.style.paddingRight = 14; u.Root.style.paddingTop = 4; u.Root.style.paddingBottom = 4; // Glyph round 2
            var line = E(u.Root); line.style.flexDirection = FlexDirection.Row; line.style.alignItems = Align.Center; line.style.height = 24;
            u.Cameo = E(line, null, w: 18, h: 18); u.Cameo.style.marginRight = 4;
            u.Glyph = E(line, null, w: 16, h: 16); u.Glyph.style.flexShrink = 0;
            u.Name = L(line, "card-title"); u.Name.style.marginLeft = 6; u.Name.style.color = Ink; u.Name.style.fontSize = 13.5f;
            u.TagIcon = E(line, null, w: 16, h: 16); u.TagIcon.style.marginLeft = 6;
            E(line).style.flexGrow = 1;
            u.Fields = E(line); u.Fields.style.flexDirection = FlexDirection.Row; u.Fields.style.alignItems = Align.Center;
            u.Key = E(line, "s-plate_light", w: float.NaN, h: 18); u.Key.style.marginLeft = 8; u.Key.style.minWidth = 18; u.KeyText = L(u.Key, "keycap"); u.KeyText.style.paddingLeft = 5; u.KeyText.style.paddingRight = 5; u.KeyText.style.unityTextAlign = TextAnchor.MiddleCenter;
            u.Alt = E(u.Root); u.Alt.style.flexDirection = FlexDirection.Row; u.Alt.style.marginTop = 3; u.Alt.style.marginLeft = 22; u.Alt.style.borderTopWidth = 1; u.Alt.style.borderTopColor = new Color(0.7f, 0.74f, 0.72f); u.Alt.style.paddingTop = 3;
            int k = u.Index;
            u.Root.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.PanelDo(k); e.StopPropagation(); } });
            u.Root.RegisterCallback<PointerEnterEvent>(_ => { hoverRow = k; host.SetPanelHover(k); });
            u.Root.RegisterCallback<PointerLeaveEvent>(_ => { if (hoverRow == k) { hoverRow = -1; host.SetPanelHover(-1); } });
            rows.Add(u);
        }

        /// <summary>A price line drawn: each field an icon (or an eye badge, a mark, learn icons) and its number, with a gap between fields.</summary>
        void Fields(VisualElement into, List<HudModel.PField> fs, Color ink, ref string key, bool chip, bool small = false, Dictionary<string, float> cols = null)
        {
            var sb = new System.Text.StringBuilder(); foreach (var f in fs) sb.Append(f.Kind).Append(f.Icon).Append(f.Text).Append(f.Eye).Append(f.Mark).Append(f.Found).Append(string.Join(",", f.Icons)).Append('|');
            var k = sb.ToString(); if (k == key) return; key = k; into.Clear();
            float ic = chip ? 16 : small ? 12 : 14;
            var order = new List<HudModel.PField>(fs);
            if (cols != null) // every column of the card, in Fable's order; an empty slot where this row has no such field
            {
                order.Clear(); foreach (var kind in HudModel.FieldOrder) if (cols.ContainsKey(kind)) order.Add(fs.Find(x => x.Kind == kind) ?? new HudModel.PField { Kind = kind, Icon = "", Text = "\u0000" });
            }
            foreach (var f in order)
            {
                var part = E(into); part.style.flexDirection = FlexDirection.Row; part.style.alignItems = Align.Center; part.style.marginLeft = cols != null ? ColGap : small ? 16 : 11; part.style.flexShrink = 0;
                if (cols != null) { part.style.width = cols[f.Kind]; if (f.Text == "\u0000") continue; }
                if (f.Found) { var fd = E(part, null, w: ic, h: ic); Icon(fd, "found", ink); fd.style.marginRight = 3; }
                if (f.Kind == "eye") // Glyph, "Eyes, revised": the bare glyph, 18 px (no chrome disc: a rimmed disc read as a help button); its line on hover
                {
                    float es = chip ? 18 : small ? 14 : 18; var gl = E(part, null, w: es, h: es, pick: true); Icon(gl, f.Icon, f.Eye == "open" ? Red : chip ? Cream : Ink);
                    var tip = f.Tip; gl.RegisterCallback<PointerEnterEvent>(_ => EyeTipShow(gl, tip)); gl.RegisterCallback<PointerLeaveEvent>(_ => EyeTipShow(null, null)); continue;
                }
                if (f.Kind == "mark") { Bg(E(part, null, w: ic + 2, h: ic + 2), $"badges/mark_{f.Mark}"); continue; }
                if (f.Kind == "learns") { foreach (var li in f.Icons) { var d = E(part, "s-disc_dark_24", w: ic + 4, h: ic + 4); d.style.marginRight = 2; Icon(E(d, null, 2, 2, ic, ic), li, Cream); } continue; }
                if (f.Icon.Length > 0) { var e = E(part, null, w: ic, h: ic); Icon(e, f.Icon, ink); e.style.marginRight = 3; }
                var l = L(part, chip ? "chip-value" : "card-price"); l.text = f.Text; l.style.color = ink; if (!chip) l.style.fontSize = small ? 11.5f : 13f; l.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
        }

        void Chip(bool alt, float W)
        {
            var it = host.HoverIntent; var hov = host.Hover; bool ended = m.Ended;
            string lead = null; List<HudModel.PField> fs = null; string refused = "";
            if (!ended && host.Panel == null && host.Loud == null)
            {
                if (host.AimNow is HandoverHost.AimParts a && host.AimLine != null)
                {
                    lead = a.Badge; fs = new List<HudModel.PField>();
                    if (a.Refused.Length > 0) refused = a.Refused;
                    else
                    {
                        fs.Add(new HudModel.PField { Kind = "time", Icon = "p_time", Text = a.Down });
                        if (alt) fs.Add(new HudModel.PField { Kind = "hits", Icon = "", Text = "hits " + a.Ratio }); // Glyph fight r1 #2: "3/10" read as shots, odds or a magazine; the odds in words, with Alt
                        if (a.Trail.Length > 0) fs.Add(new HudModel.PField { Kind = "trail", Icon = "p_trail", Text = a.Trail });
                        if (a.Civilian) fs.Add(new HudModel.PField { Kind = "mark", Mark = "no" });
                    }
                }
                else if (hov == null && alt && host.MoveNow is HandoverHost.MoveParts mv) // the ring answers at rest; Alt adds the numbers beside it
                {
                    fs = new List<HudModel.PField>();
                    if (mv.Refused.Length > 0) { lead = "icons/singles/s_moving"; refused = mv.Refused; }
                    else if (mv.CarryS >= 0) { lead = "icons/singles/s_carrying"; fs.Add(new HudModel.PField { Kind = "time", Icon = "p_time", Text = $"{mv.CarryS} s" }); if (mv.Eye.Length > 0) fs.Add(new HudModel.PField { Kind = "eye", Icon = "eye_" + mv.Eye, Eye = mv.Eye }); }
                    else
                    {
                        lead = "icons/singles/s_moving"; fs.Add(new HudModel.PField { Kind = "walk", Icon = "", Text = $"{mv.WalkS} s" });
                        if (mv.RunS >= 0) { fs.Add(new HudModel.PField { Kind = "run", Icon = "run", Text = $"{mv.RunS} s" }); if (mv.Eye.Length > 0) fs.Add(new HudModel.PField { Kind = "eye", Icon = "eye_" + mv.Eye, Eye = mv.Eye }); if (mv.Alert.Length > 0) fs.Add(new HudModel.PField { Kind = "alert", Icon = "p_alert", Text = mv.Alert }); }
                    }
                }
                else if (it != null && hov != null && it.Actionable)
                {
                    lead = "icons/singles/" + (it.Lead.Length > 0 ? it.Lead : HudModel.AtlasIcon(it.Order.Do, it.Order.Approach, ""));
                    fs = HudModel.PriceLine(host, it);
                }
            }
            bool on = fs != null; Show(chip2, on); if (!on) return;
            Art(chipLead, lead, Cream); Text(chipRefused, refused); Show(chipRefused, refused.Length > 0);
            Fields(chipFields, fs, Cream, ref chipKey, true);
            var p = RuntimePanelUtils.ScreenToPanel(root.panel, m.Mouse); float cw = chip2.resolvedStyle.width; if (float.IsNaN(cw)) cw = 220;
            if (hov == null && host.MoveNow != null && ringOn) p = ringAt + new Vector2(ringRx + 6, 18); // beside the ring, tail to it
            chip2.style.left = p.x + 16 + cw > W - 14 ? p.x - cw - 16 : p.x + 16; chip2.style.top = p.y - 46;
        }

        // ---------------------------------------------------------------- the street

        VisualElement WorldEl(string cls, float w, float h)
        {
            while (worldEls.Count <= worldUsed) { var e = E(marks, null, 0, 0, w, h); var g = E(e, null, 0, 0, w, h); g.name = "g"; worldEls.Add(e); }
            var el = worldEls[worldUsed++]; Show(el, true); el.ClearClassList(); if (cls != null) foreach (var c in cls.Split(' ')) el.AddToClassList(c);
            el.style.width = w; el.style.height = h; el.style.opacity = 1; el.style.unityBackgroundImageTintColor = Color.white; el.style.backgroundImage = StyleKeyword.Null;
            if (el.Q<Label>("num") is Label nl0) Show(nl0, false);
            var gl = el.Q("g"); gl.style.left = w * 0.2f; gl.style.top = h * 0.2f; gl.style.width = w * 0.6f; gl.style.height = h * 0.6f; Show(gl, true); gl.style.backgroundImage = StyleKeyword.Null;
            Border(el, 0, Color.clear); Round(el, 0); el.style.backgroundColor = Color.clear; // a pooled element: no fill left from an eye marker's disc
            return el;
        }

        /// <summary>Painted every frame by `world` (positions in panel space): ground rings, activity rings, the clock pip's wedge, wake rings,
        /// the dashed eye rims, reports' dotted lines, and the box being dragged.</summary>
        readonly List<Action<Painter2D>> paints = new();

        void DrawWorld(Painter2D p, Rect r) { foreach (var f in paints) f(p); }

        void StreetLess(bool alt, Camera cam)
        {
            paints.Clear(); eyesAt.Clear(); var g = host.Game; if (g == null || !cam) return;
            float px = 14f / cam.orthographicSize; float now = Time.unscaledTime;
            // the hover ring and the hint's ring at the feet; pulses where an order lands
            void Ground(Vector3 w, float d, Color c, float width, bool dashed = false)
            {
                if (!Proj(w, out var q)) return; float rx = d * px / 2, ry = rx * 0.62f;
                paints.Add(pp =>
                {
                    pp.strokeColor = c; pp.lineWidth = width; int n = dashed ? 16 : 1;
                    for (int i = 0; i < n; i++)
                    {
                        pp.BeginPath(); float a0 = dashed ? i * 360f / n : 0, a1 = dashed ? a0 + 180f / n : 360;
                        for (int s = 0; s <= 24; s++) { float a = Mathf.Deg2Rad * Mathf.Lerp(a0, a1, s / 24f); var v = q + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); if (s == 0) pp.MoveTo(v); else pp.LineTo(v); }
                        pp.Stroke();
                    }
                });
            }
            bool carMarked = host.Hover?.Kind == "car" && host.Hover.Key == "getaway" && (carMark?.resolvedStyle.display == DisplayStyle.Flex || heldMark?.resolvedStyle.display == DisplayStyle.Flex);
            if (m.HoverRing.HasValue && !carMarked) Ground(m.HoverRing.Value, 64, Cream, 2.2f); // Glyph fight r1 B: the car's marker is its one mark
            // the Next Step's ring: only while its line shows, fading with it, in its noun's colour, with the verb's glyph at the centre ("do this here")
            if (host.HintNow?.Ring is Vector3 hr && !m.Ended && hintAlpha > 0f)
            {
                var rc = hintNounColour; rc.a = hintAlpha; Ground(hr, 90 * (1f + 0.12f * Mathf.Sin(now * 4f)), rc, 2.6f, true);
                if (Proj(hr, out var hq)) { var he = WorldEl("s-disc_dark_24", 24, 24); he.style.left = hq.x - 12; he.style.top = hq.y - 12; he.style.opacity = hintAlpha; Icon(he.Q("g"), hintVerbGlyph, rc); }
            }
            foreach (var (w, kind, born) in m.Pulses) { float a = (now - born) / 0.7f; if (a < 0 || a > 1) continue; Ground(w, 50 + 90 * a, kind == HandoverHost.Feedback.Refused ? new Color(1f, 0.6f, 0.55f, 1 - a) : new Color(0.85f, 1f, 0.9f, 1 - a), 2f); }
            if (m.Box is Rect box) { var bp = RuntimePanelUtils.ScreenToPanel(root.panel, box.min); var bq = RuntimePanelUtils.ScreenToPanel(root.panel, box.max); paints.Add(pp => { pp.strokeColor = Cream; pp.lineWidth = 1.5f; pp.BeginPath(); pp.MoveTo(bp); pp.LineTo(new Vector2(bq.x, bp.y)); pp.LineTo(bq); pp.LineTo(new Vector2(bp.x, bq.y)); pp.ClosePath(); pp.Stroke(); }); }
            // activity rings over busy agents: the act's glyph in a disc, a ring that fills (Glyph's 05: the ring fills round the Enforcer)
            for (int i = 0; i < 4; i++)
            {
                var rg = m.Rings[i]; if (!rg.Show || !Proj(rg.World + Vector3.up * 2.35f, out var q)) continue;
                var e = WorldEl("s-disc_dark_24", 24, 24); e.style.left = q.x - 12; e.style.top = q.y - 12;
                Icon(e.Q("g"), HudModel.AtlasIcon(host.Game.Ops[i].Busy?.Do ?? (host.DoingOf(i).Phase is "walking" or "running" ? "move" : host.DoingOf(i).Verb), host.Game.Ops[i].Busy?.Approach ?? "", host.DoingOf(i).Phase), Cream);
                float frac = rg.Progress < 0 ? 1f : rg.Progress / 1000f; bool open = rg.Progress < 0;
                paints.Add(pp => { if (open) Dashed(pp, q, 15, new Color(1, 1, 1, 0.5f), 2.4f, 10); else { RingArc(pp, q, 15, 1f, new Color(1, 1, 1, 0.22f), 2.6f); RingArc(pp, q, 15, frac, TurqLight, 2.6f); } });
                if (rg.ActAt.HasValue && Proj(rg.ActAt.Value + Vector3.up * 0.3f, out var aq)) { var ae = WorldEl("s-disc_light_24", 20, 20); ae.style.left = aq.x - 10; ae.style.top = aq.y - 10; var (qd, qa) = host.PendingOf(i); Icon(ae.Q("g"), HudModel.AtlasIcon(qd, qa, ""), Ink); }
            }
            StepMarks(px, now);
            ShotMarks();
            PickMarks(now);
            MovePreview(alt, px, now);
            // destinations (Sky 2026-10-16, replacing the route line): a ring at each moving agent's destination with its number, as the scope's
            // cell discs (cream, ink numeral, the selected one ringed turquoise); never on another pin; gone on arrival. Alt: the engine's own
            // walk path, dotted, thin, for each moving agent.
            var pinRects = new List<Rect>();
            for (int k = 0; k < 4; k++)
            {
                var op = g.Ops[k]; if (op.Walk == null || op.Inside || op.Down || m.Ended) continue;
                var dw = host.frame.World(op.Walk.End); if (!Proj(dw, out var dq)) continue;
                bool sel = host.Selected[k]; var col = sel ? TurqLight : Cream;
                Ground(dw, 34, col, 2f);
                var rect = new Rect(dq.x - 9, dq.y - 24, 18, 18);
                for (int guard = 0; guard < 4; guard++) { bool hit = false; foreach (var pr in pinRects) if (pr.Overlaps(rect)) { rect.x = pr.xMax + 2; hit = true; } if (!hit) break; }
                pinRects.Add(rect);
                var pe = WorldEl(null, 18, 18); pe.style.left = rect.x; pe.style.top = rect.y; Round(pe, 9); pe.style.backgroundColor = Cream; Border(pe, sel ? 1.6f : 0.8f, sel ? Turq : Ink);
                var gl = pe.Q("g"); Show(gl, false);
                var lab = pe.Q<Label>("num"); if (lab == null) { lab = L(pe, "numplate", 0, 0, 18, 18); lab.name = "num"; lab.style.unityTextAlign = TextAnchor.MiddleCenter; lab.style.color = Ink; lab.style.fontSize = 10; }
                Show(lab, true); Text(lab, (k + 1).ToString());
                if (alt && op.Walk.Points.Length > 1)
                {
                    var pts = new List<Vector2>(); Proj(host.frame.World(op.Pos), out var p0); pts.Add(p0); long done = op.WalkMm, run = 0; var wp = op.Walk.Points;
                    for (int j = 1; j < wp.Length; j++) { run += G.Isqrt(G.D2(wp[j - 1], wp[j]) * 100); if (run > done && Proj(host.frame.World(wp[j]), out var pj)) pts.Add(pj); }
                    var pc = col; paints.Add(pp => { pp.fillColor = new Color(pc.r, pc.g, pc.b, 0.9f); for (int j = 1; j < pts.Count; j++) { float len = Vector2.Distance(pts[j - 1], pts[j]); int n = Mathf.Max(1, (int)(len / 7)); for (int q = 0; q < n; q++) { pp.BeginPath(); pp.Arc(Vector2.Lerp(pts[j - 1], pts[j], q / (float)n), 1.4f, Deg(0), Deg(360)); pp.Fill(); } } });
                }
            }
            // a fight: health pips over fighters; a downed agent's mark
            foreach (var f in m.Fighters)
            {
                if (!Proj(f.World + Vector3.up * 2.1f, out var q)) continue; int hp = f.Hp, mx = f.Max; bool cell = f.Cell;
                paints.Add(pp => { for (int k = 0; k < mx; k++) { var c = new Vector2(q.x + (k - (mx - 1) / 2f) * 8, q.y); pp.fillColor = k < hp ? (cell ? Cream : new Color(1f, 0.8f, 0.75f)) : new Color(1, 1, 1, 0.25f); pp.BeginPath(); pp.Arc(c, 2.6f, Deg(0), Deg(360)); pp.Fill(); } });
            }
            foreach (var dn in m.Down) { if (!Proj(dn.World + Vector3.up * 1.0f, out var q)) continue; var e = WorldEl("s-disc_dark_24", 22, 22); e.style.left = q.x - 11; e.style.top = q.y - 11; Icon(e.Q("g"), "s_downed", Cream); }
            // the eye on the target while a takedown is armed or hovered (Glyph §3): shut = from behind, open = he sees you, ? unknown
            var it = host.HoverIntent; HandoverHost.Target tg = host.Hover;
            if (host.Panel != null && host.PanelHover >= 0 && host.PanelHover < host.Panel.Rows.Count) { it = host.Panel.Rows[host.PanelHover].I; tg = host.Panel.T; }
            else if (host.Panel != null) { var tr = host.Panel.Rows.Find(x => x.I?.Td != null); if (tr != null) { it = tr.I; tg = host.Panel.T; } }
            if (it?.Td is TakedownQuote tq && tg != null && host.TargetWorld(tg) is Vector3 tw && Proj(tw + Vector3.up * 2.5f, out var eq))
            {
                string state = tq.Aware == "no" ? "shut" : tq.Aware == "yes" ? "open" : "unk";
                EyeMark(eq + new Vector2(26, -2), state, tg.Key == "guard" ? Amber : Cream);
            }
            // the clock pip over a guard whose routine is known: armed, hovered, the sheet on him, or Alt
            var gp = g.GuardP;
            bool guardFocus = alt || (host.Hover?.Key == "guard") || (host.Panel?.T?.Key == "guard") || m.TakedownArmed;
            if (gp.KRoutine && !gp.Down && guardFocus && host.Views?.Find("guard") is Transform gt && gt.gameObject.activeSelf && Proj(gt.position + Vector3.up * 2.5f, out var cq))
            {
                int mark = -1; try { mark = g.NextMarkOf(gp, g.Tick); } catch { }
                if (mark >= 0)
                {
                    float period = g.R.CheckInPeriodS * Game.Hz, left = Mathf.Max(0, mark - g.Tick), frac = Mathf.Clamp01(left / period); bool soon = left < 10 * Game.Hz;
                    var c = cq + new Vector2(-26, -2);
                    paints.Add(pp =>
                    {
                        pp.fillColor = new Color(0.06f, 0.15f, 0.16f, 0.95f); pp.BeginPath(); pp.Arc(c, 9, Deg(0), Deg(360)); pp.Fill();
                        pp.fillColor = soon && Mathf.Repeat(Time.unscaledTime, 0.6f) < 0.3f ? Amber : Cream; pp.BeginPath(); pp.MoveTo(c); pp.Arc(c, 6.5f, Deg(-90), Deg(-90 + 360 * frac)); pp.ClosePath(); pp.Fill();
                        pp.strokeColor = Amber; pp.lineWidth = 2f; pp.BeginPath(); pp.Arc(c, 9, Deg(0), Deg(360)); pp.Stroke();
                    });
                }
            }
            // "?" over watchers not yet watched: those noticing an agent now; on Alt all of them
            foreach (var u in m.Unknowns) if (Proj(u, out var uq)) EyeMark(uq, "unk", Cream);
            if (alt) foreach (var (w, col) in UnwatchedWatchers()) if (Proj(w, out var wq)) EyeMark(wq, "unk", col);
            // turned contacts: the ✓ over them
            foreach (var role in new[] { "waitress", "driver" })
                if (g.PersonByRole(role).Turned && host.Views?.Find(role) is Transform rt && rt.gameObject.activeSelf && Proj(rt.position + Vector3.up * 2.45f, out var tq2))
                { var e = WorldEl(null, 16, 16); e.style.left = tq2.x + 12; e.style.top = tq2.y - 8; Bg(e.Q("g"), "badges/mark_ok"); e.Q("g").style.left = 0; e.Q("g").style.top = 0; e.Q("g").style.width = 16; e.Q("g").style.height = 16; }
            // reports in transit: the "!" disc, its ring, the dotted line to the phone and the phone's own small disc (Glyph §3)
            foreach (var rp in m.Reports)
            {
                if (!Proj(rp.World, out var q)) continue;
                var e = WorldEl("s-disc_dark_24", 22, 22); e.style.left = q.x - 11; e.style.top = q.y - 11; Icon(e.Q("g"), "bang", Cream);
                float frac = rp.Frac; bool timed = rp.LandsS >= 0; paints.Add(pp => { if (timed) { RingArc(pp, q, 14, 1f, new Color(1, 1, 1, 0.22f), 2.4f); RingArc(pp, q, 14, frac, Cream, 2.4f); } });
                if (rp.To.HasValue && Proj(rp.To.Value + Vector3.up * 1.2f, out var to))
                {
                    var ce = WorldEl("s-disc_dark_24", 18, 18); ce.style.left = to.x - 9; ce.style.top = to.y - 9; Icon(ce.Q("g"), "call", Cream);
                    var from = q + new Vector2(0, 14); float phase = Mathf.Repeat(now * 1.5f, 1f);
                    paints.Add(pp => { float len = Vector2.Distance(from, to); int n = Mathf.Max(2, (int)(len / 9)); pp.fillColor = Cream; for (int k = 0; k < n; k++) { float t = (k + phase) / n; var v = Vector2.Lerp(from, to, t); pp.BeginPath(); pp.Arc(v, 1.8f, Deg(0), Deg(360)); pp.Fill(); } });
                }
            }
            // the brief 013 marks keep their glyphs; their words go: the held door's time, the stash's name (hover and Alt keep it), the old report disc
            foreach (var d in doorMarks) if (d.Second != null) Show(d.Second, alt);
            foreach (var r in reportMarks) { Show(r.Root, false); foreach (var dd in r.Dots) Show(dd, false); }
            for (int i = 0; i < stashMarks.Count && i < m.Stashes.Count; i++) Show(stashMarks[i].Text, alt || m.Stashes[i].Hover);
        }


        // ---------------------------------------------------------------- the move preview (Glyph, "Move preview", variant A: the ring)

        Vector2 lastMouse; float mouseRestAt; Vector2 ringAt; float ringRx; bool ringOn;

        /// <summary>One ground ellipse at the cursor (64 x 39 px at play zoom): the rim fills clockwise with the walk time (full = 60 s) over a faint
        /// track and an ink under-stroke; a dark inner disc carries the symbol: footprints (safe, turquoise), the open eye (seen, the watcher's
        /// own colour), the eye with "?" (unknown, cream, dashed track), the lamp (raises Alert, ink rim inside a cream one). After the cursor
        /// rests 0.25 s the stretches of the route inside a known watcher's sight glow in its colour (no route line: Sky). An unwatched watcher
        /// on the route wears a faint "?". Unreachable ground: nothing. Alt: the numbers chip beside the ring.</summary>
        void MovePreview(bool alt, float px, float now)
        {
            ringOn = false; var mv = host.MoveNow;
            if (m.Mouse != lastMouse) { lastMouse = m.Mouse; mouseRestAt = now; }
            if (mv == null || mv.Refused.Length > 0 || mv.WalkS < 0 || host.Hover != null || host.Panel != null || m.Ended || !Proj(mv.At, out var q)) return;
            ringOn = true; ringAt = q; float rx = 32f * px, ry = rx * 0.616f; ringRx = rx;
            Color Kind(string k) => k == "camera" ? Red : k == "rivals" ? Violet : k == "guard" ? Amber : TurqLight;
            Color ink = new(0.071f, 0.188f, 0.18f), cream = new(0.969f, 0.957f, 0.925f);
            string state = mv.State; Color rim = state == "seen" ? Kind(mv.SeenKind) : state == "unk" ? cream : state == "alert" ? ink : TurqLight;
            float frac = Mathf.Clamp01((mv.CarryS >= 0 ? mv.CarryS : mv.WalkS) / 60f);
            Vector2 E(float deg, float sx, float sy) { float a = Mathf.Deg2Rad * deg; return q + new Vector2(Mathf.Sin(a) * sx, -Mathf.Cos(a) * sy); } // 0 = 12 o'clock, clockwise
            void Ellipse(Painter2D p, float sx, float sy, float from, float to, Color c, float w, bool butt = false)
            { p.strokeColor = c; p.lineWidth = w; p.lineCap = butt ? LineCap.Butt : LineCap.Round; p.BeginPath(); int n = Mathf.Max(2, (int)((to - from) / 6)); for (int i = 0; i <= n; i++) { var v = E(Mathf.Lerp(from, to, i / (float)n), sx, sy); if (i == 0) p.MoveTo(v); else p.LineTo(v); } p.Stroke(); }
            // the glow first (under the ring), after a 0.25 s rest: the seen stretch only, in the watcher's colour, a solid core on an ink
            // under-stroke (it must hold over the watcher's own translucent cone) and a soft halo that breathes once a second
            if (now - mouseRestAt >= 0.25f)
                foreach (var (a, b, kind) in mv.Glow)
                    if (Proj(a, out var pa) && Proj(b, out var pb))
                    {
                        var gc = Kind(kind); float al = 0.45f + 0.2f * Mathf.Sin(now * Mathf.PI * 2f);
                        paints.Add(p =>
                        {
                            void Seg(Color c, float w) { p.strokeColor = c; p.lineWidth = w; p.lineCap = LineCap.Round; p.BeginPath(); p.MoveTo(pa); p.LineTo(pb); p.Stroke(); }
                            Seg(new Color(gc.r, gc.g, gc.b, al), 13f * px); Seg(new Color(ink.r, ink.g, ink.b, 0.9f), 7f * px); Seg(gc, 4f * px);
                        });
                    }
            paints.Add(p =>
            {
                p.fillColor = new Color(ink.r, ink.g, ink.b, 0.95f); p.BeginPath(); for (int i = 0; i <= 40; i++) { var v = E(i * 9f, rx - 3.5f, ry - 3.5f); /* the panel blends in linear: 0.95 reads dark on pale stone; it meets the under-stroke */ if (i == 0) p.MoveTo(v); else p.LineTo(v); } p.ClosePath(); p.Fill(); // the dark inner disc
                Ellipse(p, rx, ry, 0, 360, ink, 8f);                                                                                   // the under-stroke: it holds on pale stone
                if (state == "alert") Ellipse(p, rx + 5.5f, ry + 5.5f * 0.616f, 0, 360, cream, 2.4f);                                  // Alert: a second, cream rim outside
                Color.RGBToHSV(rim, out var th, out var ts, out _);
                var track = state == "seen" ? Color.HSVToRGB(th, ts, 0.42f) : Color.Lerp(ink, rim, 0.32f);                              // opaque (an alpha track washes out in the linear blend); seen: the watcher's colour darkened in place, never blended with ink (Glyph: amber #6b4609, not olive)
                if (state == "unk") for (int i = 0; i < 16; i++) Ellipse(p, rx, ry, i * 22.5f, i * 22.5f + 11f, Color.Lerp(ink, rim, 0.55f), 4.6f, butt: true); // unknown: a dashed track
                else Ellipse(p, rx, ry, 0, 360, state == "alert" ? rim : track, 4.6f);                                                  // the faint track
                if (frac > 0 && state != "alert") Ellipse(p, rx, ry, 0, 360 * frac, rim, 4.6f);                                        // the clock: the walk time, a full ring = 60 s
                else if (frac > 0) Ellipse(p, rx, ry, 0, 360 * frac, cream, 2f);
            });
            string glyph = state == "seen" ? "eye_open" : state == "unk" ? "eye_unk" : state == "alert" ? "p_alert" : mv.CarryS >= 0 ? "s_carrying" : "s_moving";
            float gs = Mathf.Min(25f, 25f * px); var se = WorldEl(null, gs, gs); se.style.left = q.x - gs / 2; se.style.top = q.y - gs / 2;
            var sg = se.Q("g"); sg.style.left = 0; sg.style.top = 0; sg.style.width = gs; sg.style.height = gs; Icon(sg, glyph, state == "seen" ? rim : state == "alert" ? cream : state == "unk" ? cream : TurqLight);
            foreach (var w in mv.Unwatched) if (Proj(w, out var wq)) { int was = worldUsed; EyeMark(wq, "unk", Cream); if (worldUsed > was) worldEls[worldUsed - 1].style.opacity = 0.6f; }
        }

        readonly List<Vector2> eyesAt = new();
        void EyeMark(Vector2 at, string state, Color rim)
        {
            foreach (var e0 in eyesAt) if ((e0 - at).sqrMagnitude < 40f * 40f) return; // Glyph 016 r2 A: one eye a person (the takedown's, else the "?"): two icons at most
            eyesAt.Add(at);
            // Glyph, "Eyes, revised": a flat ink-deep disc (no chrome, which read as a button), the glyph 18 px; the thin rim says whose eye
            var e = WorldEl(null, 24, 24); e.style.left = at.x - 12; e.style.top = at.y - 12; Round(e, 12); e.style.backgroundColor = new Color(0.059f, 0.153f, 0.157f, 0.94f);
            var g2 = e.Q("g"); g2.style.left = 3; g2.style.top = 3; g2.style.width = 18; g2.style.height = 18;
            Icon(g2, state == "open" ? "eye_open" : state == "shut" ? "eye_shut" : "eye_unk", state == "open" ? Red : Cream);
            if (rim != Cream) Border(e, 1.2f, rim);
        }

        /// <summary>The watchers the cell has not learned (cameras, the guard, the patrol, the rivals) where they stand, for Alt's "?".</summary>
        IEnumerable<(Vector3 w, Color c)> UnwatchedWatchers()
        {
            var g = host.Game;
            foreach (var c in g.S.Cameras) if (g.Known("camera:" + c.Id) is not { Sight: true }) yield return (host.frame.World(c.At) + Vector3.up * 3.6f, Red);
            if (g.Known("guard") is not { Sight: true } && g.GuardPose() is var gp && gp.HasValue && !g.GuardP.KRoutine) yield return (host.frame.World(gp.Value.pos) + Vector3.up * 2.9f, Amber);
            if (g.Known("patrol") is not { Sight: true } && g.PatrolPose() is var pp && pp.HasValue) yield return (host.frame.World(pp.Value.pos) + Vector3.up * 2.9f, Amber);
            if (g.RivalsPresent && g.Known("rivals") is not { Sight: true } && g.RivalsPose() is var rp && rp.HasValue) yield return (host.frame.World(rp.Value.pos) + Vector3.up * 2.9f, Violet);
        }

        // ---------------------------------------------------------------- key-person pips (Sky, 2026-10-16: "no idea who the waitress is")

        static readonly string[] Cast = { "waitress", "guard", "driver", "councillor" };
        public static string CastBadge(string role) => role switch { "waitress" => "badges/npc_waitress", "guard" => "badges/signal_guard", "driver" => "badges/npc_driver", _ => "badges/npc_councillor" };
        readonly Dictionary<string, float> pulseAt = new();
        string pipHover;

        /// <summary>A known person found from Intel or the scope: the camera pans to them and their pip pulses for 2 s.</summary>
        public void FindPerson(string role)
        {
            if (host.Views?.Find(role) is Transform t && t.gameObject.activeSelf) host.LookAt(t.position);
            else if (role == "councillor") host.LookAt(host.ValeKnown);
            pulseAt[role] = Time.unscaledTime;
        }

        /// <summary>The people the cell knows of (told in the briefing, or learned) carry a pip with their role glyph over the head, the same glyph
        /// as their blip on the scope; hovering it names them. The pips never sit on each other: a later one moves up.</summary>
        void Pips(Camera cam)
        {
            while (pipEls.Count < Cast.Length)
            {
                var r = E(marks, null, 0, 0, 26, 26, pick: true); var bd = E(r, null, 2, 2, 22, 22); var nm = L(marks, "verb-name", 0, 0); nm.AddToClassList("s-plate_dark"); nm.style.paddingLeft = 7; nm.style.paddingRight = 7; nm.style.height = 20;
                int k = pipEls.Count; r.RegisterCallback<PointerEnterEvent>(_ => pipHover = Cast[k]); r.RegisterCallback<PointerLeaveEvent>(_ => { if (pipHover == Cast[k]) pipHover = null; });
                r.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { FindPerson(Cast[k]); e.StopPropagation(); } else if (e.button == 1) { host.RightOnPerson(Cast[k]); e.StopPropagation(); } }); // Sky, route B: a right click on the pin is a right click on the person
                pipEls.Add((r, bd, nm)); manifest.Add((r, "image", () => "pip " + Cast[k]));
            }
            var placed = new List<Rect>(); bool alt = host.Peeking;
            for (int i = 0; i < Cast.Length; i++)
            {
                var (r, bd, nm) = pipEls[i]; var role = Cast[i];
                bool on = !m.Ended && !m.BriefingOpen && KnownOf(role) && host.Views?.Find(role) is Transform t && t.gameObject.activeSelf && Proj(t.position + Vector3.up * 2.3f, out var q) && InView(q);
                if (!on) { Show(r, false); Show(nm, false); continue; }
                Proj(host.Views.Find(role).position + Vector3.up * 2.3f, out var at);
                var rect = new Rect(at.x - 13, at.y - 26, 26, 26);
                for (int guard = 0; guard < 6; guard++) { bool hit = false; foreach (var pr in placed) if (pr.Overlaps(rect)) { rect.y = pr.y - 28; hit = true; } if (!hit) break; }
                // Glyph round 2: a pip never covers another mark (the guard's shield over the door's lock): it moves aside, 2 px clear
                for (int guard = 0; guard < 4; guard++) { bool hit = false; foreach (var mr in OtherMarks()) if (mr.Overlaps(rect)) { rect.x = mr.x - rect.width - 2 >= at.x - 40 ? mr.x - rect.width - 2 : mr.xMax + 2; hit = true; break; } if (!hit) break; }
                // under the cursor's price chip: the chip is about this person, so the pip steps out from under it (above the chip)
                if (chip2.resolvedStyle.display != DisplayStyle.None && chip2.layout.width > 0) { var cr = new Rect(chip2.style.left.value.value, chip2.style.top.value.value, chip2.layout.width, chip2.layout.height); if (cr.Overlaps(rect)) rect.y = cr.y - rect.height - 2; }
                placed.Add(rect);
                if (Proj(host.Views.Find(role).position + Vector3.up * 1.95f, out var head)) { var top = new Vector2(rect.center.x, rect.yMax - 2); var hd = head; paints.Add(pp => { pp.strokeColor = new Color(0.97f, 0.96f, 0.92f, 0.9f); pp.lineWidth = 1.6f; pp.BeginPath(); pp.MoveTo(top); pp.LineTo(hd); pp.Stroke(); }); } // a stem to the head: which of a cluster it is
                Show(r, true); r.style.left = rect.x; r.style.top = rect.y; Bg(bd, CastBadge(role));
                float pulse = pulseAt.TryGetValue(role, out var pa) ? Time.unscaledTime - pa : 99f;
                float s = pulse < 2f ? 1f + 0.35f * Mathf.Abs(Mathf.Sin(pulse * Mathf.PI * 2f)) : 1f; r.style.scale = new Scale(new Vector2(s, s));
                bool named = pipHover == role || alt || (host.Hover?.Kind == "person" && host.Hover.Key == role); Show(nm, named); // the hovered person is named by their pip's tag (no second chip)
                if (named)
                {
                    Text(nm, Cap1(host.Text?.TargetName(role, role) ?? role).Replace("the ", ""));
                    float tw = nm.resolvedStyle.width; if (float.IsNaN(tw) || tw < 1) tw = 60;
                    bool Free(Rect t) { foreach (var mr in OtherMarks()) if (mr.Overlaps(t)) return false; foreach (var pr in placed) if (pr != rect && pr.Overlaps(t)) return false; return true; }
                    var right = new Rect(rect.x + 28, rect.y + 3, tw, 20); var left = new Rect(rect.x - 2 - tw, rect.y + 3, tw, 20); var above = new Rect(rect.center.x - tw / 2, rect.y - 22, tw, 20);
                    var tag = Free(above) ? above : Free(right) ? right : left; // Glyph 016 r2 A: above the pips first; an eye pip covered the guard's tag ("ard")
                    nm.style.left = tag.x; nm.style.top = tag.y; placed.Add(tag);
                }
            }
        }

        /// <summary>While Alt is held, the world marks inside the numbers chip's rectangle (plus 8 px) are hidden: a mark half under the chip
        /// looks accidental (Glyph, the rival's "?" under the carry chip).</summary>
        void ClearUnderChip(bool alt)
        {
            if (!alt || chip2.resolvedStyle.display == DisplayStyle.None || chip2.layout.width <= 0) return;
            var cr = new Rect(chip2.style.left.value.value - 8, chip2.style.top.value.value - 8, chip2.layout.width + 16, chip2.layout.height + 16);
            for (int i = 0; i < worldUsed && i < worldEls.Count; i++)
            {
                var e = worldEls[i]; var r = new Rect(e.style.left.value.value, e.style.top.value.value, e.style.width.value.value, e.style.height.value.value);
                if (r.Overlaps(cr)) Show(e, false);
            }
            foreach (var (r, _, nm) in pipEls)
            {
                if (r.resolvedStyle.display != DisplayStyle.None && new Rect(r.style.left.value.value, r.style.top.value.value, 26, 26).Overlaps(cr)) { Show(r, false); Show(nm, false); }
            }
        }

        /// <summary>The rects of the other marks in the street this frame (door locks, bodies, stash plates, the small world marks).</summary>
        IEnumerable<Rect> OtherMarks()
        {
            foreach (var l in new[] { doorMarks, bodyMarks, stashMarks, reportMarks })
                foreach (var k in l) if (k.Root.resolvedStyle.display != DisplayStyle.None) { var b = k.Root.layout; if (b.width > 0) yield return new Rect(k.Root.style.left.value.value, k.Root.style.top.value.value, b.width, b.height); }
            for (int i = 0; i < worldUsed && i < worldEls.Count; i++) { var e = worldEls[i]; yield return new Rect(e.style.left.value.value, e.style.top.value.value, e.style.width.value.value, e.style.height.value.value); }
        }

        /// <summary>Told or learned (GDD §04): the briefing names Inez, the door guard, Vale's driver and Vale, so the cell knows of all four.</summary>
        bool KnownOf(string role) => host.Game != null && host.Text != null;
        /// <summary>The cell has learned something of this person (Intel's "known": routine, need, temperament, turned, or a suspicion).</summary>
        bool KnownPerson(string role) { int i = Array.IndexOf(new[] { "waitress", "driver", "guard", "councillor" }, role); return i >= 0 && m.Intel[i].Known; }

        // ---------------------------------------------------------------- the plate folds (Sky 2026-10-18: "I am finding the control panel not much";
        // a small arrow to collapse and show it). Remembered per profile; open for a first-time player. Folded, keys 1-4 still pick, and the stub
        // shows the knob that fired (the armed verb while armed, else 1.2 s).

        const float StubW = 38, StubH = 62; const string PlateFoldPref = "Suborned.Hud.PlateFolded"; // the stub's glyphs 4 px clear of the dark plate's rim (the audit)
        bool? plateFolded; VisualElement plateTab, plateStub, stubIcon; int stubPicked = -1; float stubAt = -10f;
        bool PlateFolded { get => plateFolded ??= PlayerPrefs.GetInt(PlateFoldPref, 0) == 1; set { plateFolded = value; PlayerPrefs.SetInt(PlateFoldPref, value ? 1 : 0); PlayerPrefs.Save(); } }
        /// <summary>Fold or open the order plate (the tab or the stub; also for scripted shots).</summary>
        public void FoldPlate(bool fold) => PlateFolded = fold;

        void PlateFold(float x, float midY)
        {
            if (plateTab == null)
            {
                plateTab = E(verbPlate, "s-disc_dark_24", PlateW - 6, PlateH / 2 - 11, 22, 22, pick: true); var ch = E(plateTab, null, 5, 5, 12, 12); ch.name = "c"; Icon(ch, "chev_down", Cream); ch.style.rotate = new Rotate(new Angle(90, AngleUnit.Degree)); // points left: fold
                plateTab.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0) return; e.StopPropagation(); PlateFolded = true; });
                plateStub = E(root, "s-plate_dark", 0, 0, StubW, StubH, pick: true); var so = E(plateStub, null, 13, 11, 12, 12); so.name = "c"; Icon(so, "chev_down", Cream); so.style.rotate = new Rotate(new Angle(-90, AngleUnit.Degree)); // points right: open
                stubIcon = E(plateStub, null, 11, 30, 16, 16);
                plateStub.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0) return; e.StopPropagation(); PlateFolded = false; });
                manifest.Add((plateTab, "image", () => "plate fold tab")); manifest.Add((verbPlate, "image", () => "order plate")); manifest.Add((plateStub, "image", () => "plate stub")); // knobs and their key digits: art, no running text
            }
            bool folded = PlateFolded; Show(verbPlate, !folded); Show(plateStub, folded);
            if (armedPlate.resolvedStyle.display != DisplayStyle.None && armedPlate.parent != null && armedPlate.parent.IndexOf(armedPlate) != armedPlate.parent.childCount - 1) armedPlate.BringToFront(); // Glyph 016 r2 F: the armed verb's chip above the timer pills
            if (host.Picked != stubPicked) { stubPicked = host.Picked; if (host.Picked >= 0) stubAt = Time.unscaledTime; }
            if (!folded) return;
            plateStub.style.left = x; plateStub.style.top = midY - StubH / 2;
            int k = host.Picked >= 0 ? host.Picked : m.VerbLit; bool show = host.Picked >= 0 || Time.unscaledTime - stubAt < 1.2f;
            Show(stubIcon, show && k >= 0 && k < 4 && m.VerbHas[k]); if (show && k >= 0 && k < 4) Icon(stubIcon, m.VerbIcon[k], host.Picked >= 0 ? TurqLight : Cream);
        }

        // ---------------------------------------------------------------- picking (brief 016 queue: Sky, "hard to select areas, like a camera")

        VisualElement nameChip; Label nameText; readonly List<VisualElement> targetEls = new();
        /// <summary>The hovered thing's soft rim (a cream circle round its pick anchor) and a one-word name chip over it; in target mode, a
        /// marker over every valid target in view (the verb's glyph in a disc over a dashed ring, the hint ring's grammar; the Tab focus ringed
        /// solid), each a click that gives the picked verb there.</summary>
        /// <summary>Shots in flight (Glyph fight r2): each a 2 px line on a 1 px dark underlay with a small muzzle flash; the cell's cream core in
        /// a turquoise glow, rivals violet, guards and police amber, so whose fire it is reads before the line ends. Fades over its short life.</summary>
        void ShotMarks()
        {
            var v = host.Views; if (v == null || m.Ended) return; float now = Time.unscaledTime;
            foreach (var sh in v.ShotLines)
            {
                if (!Proj(sh.From, out var a) || !Proj(sh.To, out var b)) continue;
                float al = Mathf.Clamp01(1f - (now - sh.Born) / HostViews.ShotLife * 0.6f); string side = sh.Side;
                Color core = side == "cell" ? Cream : side == "rival" ? Violet : Amber;
                paints.Add(pp =>
                {
                    pp.lineCap = LineCap.Round;
                    void Line(Color c, float w) { pp.strokeColor = c; pp.lineWidth = w; pp.BeginPath(); pp.MoveTo(a); pp.LineTo(b); pp.Stroke(); }
                    if (side == "cell") Line(new Color(TurqLight.r, TurqLight.g, TurqLight.b, 0.45f * al), 7f); // the glow
                    Line(new Color(Ink.r, Ink.g, Ink.b, 0.7f * al), 4f); Line(new Color(core.r, core.g, core.b, al), 2f);
                    // the muzzle flash: a small four-point star at the muzzle, on the same underlay
                    var d = (b - a).normalized; var n = new Vector2(-d.y, d.x);
                    void Star(Color c, float r)
                    {
                        pp.fillColor = c; pp.BeginPath(); pp.MoveTo(a + d * r * 1.6f); pp.LineTo(a + n * r * 0.45f); pp.LineTo(a + n * r); pp.LineTo(a - d * r * 0.45f);
                        pp.LineTo(a - n * r); pp.LineTo(a - n * r * 0.45f); pp.ClosePath(); pp.Fill();
                    }
                    Star(new Color(Ink.r, Ink.g, Ink.b, 0.6f * al), 6.5f); Star(new Color(1f, 0.97f, 0.86f, al), 5f);
                });
            }
        }

        void PickMarks(float now)
        {
            if (nameChip == null) { nameChip = E(marks, "s-plate_dark", 0, 0, float.NaN, float.NaN); nameChip.style.paddingLeft = 8; nameChip.style.paddingRight = 8; nameChip.style.paddingTop = 4; nameChip.style.paddingBottom = 4; nameChip.style.minHeight = 22; nameChip.style.justifyContent = Justify.Center; nameText = L(nameChip, "verb-name"); manifest.Add((nameChip, "image", () => "name chip")); } // Glyph 016 r3: 8 px sideways, 4 px above and below
            var g = host.Game; bool live = g != null && !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None;
            var cam = host.View;
            bool named = false;
            if (live && doorHover && host.DoorState == "unk" && doorMark != null && doorMark.resolvedStyle.display != DisplayStyle.None)
            {   // the door marker's unknown state says why on hover (Glyph's ruling)
                Text(nameText, "Unknown. Watch first."); float w = float.IsNaN(nameChip.resolvedStyle.width) ? 150 : nameChip.resolvedStyle.width;
                nameChip.style.left = doorMark.style.left.value.value + 13 - w / 2; nameChip.style.top = doorMark.style.top.value.value + 32; named = true; // under the marker: the price chip and the wake disc sit above it
            }
            else if (live && host.HoverAnchor is Vector3 ha && host.HoverName.Length > 0 && Proj(ha, out var hq))
            {
                // Glyph 016 r2 E: one ellipse on the ground (the hover ring when the thing is actionable, else this one, the same size)
                if (m.HoverRing == null && host.Hover != null && !(host.Hover.Kind == "car" && host.Hover.Key == "getaway" && (host.CarRingOn || carMark?.resolvedStyle.display == DisplayStyle.Flex || heldMark?.resolvedStyle.display == DisplayStyle.Flex)) && host.TargetWorld(host.Hover) is Vector3 gw && Proj(gw, out var gq)) // the fight ring is the car's one marker
                { float rx = 64 * (cam ? 14f / cam.orthographicSize : 1f) / 2, ry = rx * 0.62f; var c = gq; paints.Add(pp => { pp.strokeColor = Cream; pp.lineWidth = 2.2f; pp.BeginPath(); for (int s2 = 0; s2 <= 32; s2++) { float a = Mathf.PI * 2 * s2 / 32; var v = c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); if (s2 == 0) pp.MoveTo(v); else pp.LineTo(v); } pp.Stroke(); }); }
                // a known person is named by their pip's tag; anything else by the chip
                bool pip = host.Hover?.Kind == "person" && Array.IndexOf(Cast, host.Hover.Key) >= 0 && KnownOf(host.Hover.Key);
                // Glyph fight r1 #1 and A: a holder's lock already says rival (the chip's violet diamond names it), and the car's own marker names
                // the car: no second label over either
                bool marked = host.Hover?.Kind == "hostile" && holderIds.Contains(host.Hover.Key)
                    || host.Hover?.Kind == "car" && host.Hover.Key == "getaway" && (carMark?.resolvedStyle.display == DisplayStyle.Flex || heldMark?.resolvedStyle.display == DisplayStyle.Flex);
                if (!pip && !marked)
                {
                    Text(nameText, host.HoverName); float w = float.IsNaN(nameChip.resolvedStyle.width) ? 60 : nameChip.resolvedStyle.width;
                    float x = hq.x - w / 2, y = hq.y - 17 - 8 - 22, h = float.IsNaN(nameChip.resolvedStyle.height) ? 30 : nameChip.resolvedStyle.height;
                    if (chip2 != null && chip2.resolvedStyle.display != DisplayStyle.None && chip2.worldBound.width > 0)
                    {   // the price chip beside the cursor: the name chip keeps 8 px clear of it (they read as one sentence when they touch)
                        var cr = marks.WorldToLocal(chip2.worldBound);
                        if (x < cr.xMax + 8 && x + w > cr.xMin - 8 && y < cr.yMax + 8 && y + h > cr.yMin - 8) x = cr.xMin - 8 - w;
                    }
                    nameChip.style.left = x; nameChip.style.top = y; named = true;
                }
                int sn = host.HoverStackCount, si = host.HoverStackIndex; // things overlap here: a dot each, the one a right click hits filled (Tab or the wheel steps)
                if (sn > 1) { var top = new Vector2(hq.x, hq.y - 17 - 8 - 22 - 7); paints.Add(pp => { for (int i = 0; i < sn; i++) { var c2 = top + new Vector2((i - (sn - 1) / 2f) * 9f, 0); pp.BeginPath(); pp.Arc(c2, 3f, Deg(0), Deg(360)); if (i == si) { pp.fillColor = Cream; pp.Fill(); } else { pp.strokeColor = Cream; pp.lineWidth = 1.4f; pp.Stroke(); } } }); }
            }
            Show(nameChip, named); if (named && nameChip.parent.IndexOf(nameChip) != nameChip.parent.childCount - 1) nameChip.BringToFront();
            int used = 0;
            if (live && host.TargetMode && cam)
            {
                var icon = m.VerbIcon[Mathf.Clamp(host.Picked, 0, 3)]; var focus = host.Focused;
                foreach (var p in host.TargetMarks)
                {
                    var vp = cam.WorldToViewportPoint(p.Anchor); if (vp.z <= 0 || vp.x < 0.02f || vp.x > 0.98f || vp.y < 0.02f || vp.y > 0.98f) continue; // off screen: an edge pin (Edges)
                    if (!Proj(p.Anchor, out var q)) continue;
                    while (targetEls.Count <= used)
                    {
                        var e = E(marks, "s-disc_dark_24", 0, 0, 24, 24, pick: true); E(e, null, 4, 4, 16, 16).name = "g"; int ix = targetEls.Count;
                        e.RegisterCallback<PointerDownEvent>(ev => { if (ev.button != 0) return; ev.StopPropagation(); if (e.userData is HandoverHost.Pickable pk) host.TargetClick(pk); });
                        manifest.Add((e, "image", () => "target marker")); targetEls.Add(e);
                    }
                    var el = targetEls[used++]; el.userData = p; Show(el, true); var at = q + new Vector2(0, -30); el.style.left = at.x - 12; el.style.top = at.y - 12; Icon(el.Q("g"), icon, TurqLight);
                    bool f = focus == p; var rc = q;
                    paints.Add(pp => { if (f) { pp.strokeColor = TurqLight; pp.lineWidth = 2.2f; pp.BeginPath(); pp.Arc(at, 15f, Deg(0), Deg(360)); pp.Stroke(); } Dashed(pp, rc, 15f, new Color(TurqLight.r, TurqLight.g, TurqLight.b, 0.9f), 1.8f, 10); });
                }
            }
            for (int i = used; i < targetEls.Count; i++) Show(targetEls[i], false);
        }

        // ---------------------------------------------------------------- the end steps (Sky 2026-10-18: "it's really unclear on how to take Vale")

        VisualElement doorMark, carMark, heldMark, doorNext; Label heldNum; Paint knobRing; bool doorHover;
        readonly Dictionary<string, (Vector3 at, float lost)> holderSeen = new(); readonly HashSet<string> holderIds = new();
        /// <summary>The door marker while Enter is open to the Enforcer (a dashed ring with the Enter glyph, the hint ring's grammar, quiet: no
        /// pulse), with his progress round it while he is inside; the car marker once he is out with Vale. A click gives the order (HostValeSteps).
        /// His first knob carries a dashed ring while either is open, unless it is already armed.</summary>
        void StepMarks(float px, float now)
        {
            VisualElement Mk(string which)
            {
                var e = E(marks, "s-disc_dark_24", 0, 0, 26, 26, pick: true); E(e, null, 5, 5, 16, 16).name = "g";
                e.RegisterCallback<PointerDownEvent>(ev => { if (ev.button != 0) return; ev.StopPropagation(); host.StepMarkClick(which); });
                if (which == "door") { e.RegisterCallback<PointerEnterEvent>(_ => doorHover = true); e.RegisterCallback<PointerLeaveEvent>(_ => doorHover = false); }
                manifest.Add((e, "image", () => which + " marker")); return e;
            }
            doorMark ??= Mk("door"); carMark ??= Mk("car");
            if (heldMark == null)
            {   // the held car: a lock and the number of hostiles holding it, no words (Glyph fight r1 A)
                heldMark = E(marks, "s-disc_dark_24", 0, 0, 26, 26, pick: true); Icon(E(heldMark, null, 5, 5, 16, 16), "lock", Cream);
                var nb = E(heldMark, null, 17, -6, 17, 17); nb.style.backgroundColor = Cream; Round(nb, 8.5f); Border(nb, 1.2f, Ink);
                heldNum = L(nb, "verb-name"); heldNum.style.position = Position.Absolute; heldNum.style.left = 0; heldNum.style.top = 0; heldNum.style.width = 14.6f; heldNum.style.height = 14.6f; heldNum.style.unityTextAlign = TextAnchor.MiddleCenter; heldNum.style.color = Ink; heldNum.style.fontSize = 11; heldNum.style.paddingLeft = 0; heldNum.style.paddingRight = 0; heldNum.style.paddingTop = 0; heldNum.style.paddingBottom = 0; heldNum.style.marginLeft = 0; heldNum.style.marginTop = 0;
                heldMark.RegisterCallback<PointerDownEvent>(ev => { if (ev.button != 0) return; ev.StopPropagation(); host.StepMarkClick("car"); });
                manifest.Add((heldMark, "image", () => "car held marker")); Show(heldMark, false);
            }
            if (knobRing == null && verbPlate != null) knobRing = P(verbPlate, 0, 0, PlateW, PlateH, DrawKnobRing);
            var g = host.Game; bool live = g != null && !m.Ended && !m.BriefingOpen;
            bool door = live && (host.DoorMarked || host.EnterProgress >= 0), car = live && host.ExtractAvailable;
            // Glyph's ruling: the door from what the cell learned: go (turquoise, the Enter glyph), unknown (cream, "?" eye), seen (the watcher's eye, no ring)
            string ds = host.EnterProgress >= 0 ? "go" : host.DoorState; Color dc = ds == "unk" ? Cream : TurqLight;
            string dIcon = ds == "unk" ? "eye_unk" : ds == "seen" ? "eye_open" : "enter"; Color dIconC = ds == "seen" ? (host.DoorEye == "camera" ? Red : Amber) : dc;
            void Place(VisualElement e, bool on, Vector3 w, string icon, bool ringIt, int progress, Color col, Color iconCol)
            {
                if (!on || !Proj(w, out var q)) { Show(e, false); return; }
                Show(e, true); e.style.left = q.x - 13; e.style.top = q.y - 13; Icon(e.Q("g"), icon, iconCol);
                if (ringIt) { float rx = 46 * px / 2, ry = rx * 0.62f; paints.Add(pp => { pp.strokeColor = col; pp.lineWidth = 2f; for (int i = 0; i < 14; i++) { pp.BeginPath(); float a0 = i * 360f / 14, a1 = a0 + 180f / 14; for (int s2 = 0; s2 <= 8; s2++) { float a = Mathf.Deg2Rad * Mathf.Lerp(a0, a1, s2 / 8f); var v = q + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); if (s2 == 0) pp.MoveTo(v); else pp.LineTo(v); } pp.Stroke(); } }); }
                if (progress >= 0) { var c = q; float f = progress / 1000f; paints.Add(pp => { RingArc(pp, c, 16, 1f, new Color(1, 1, 1, 0.22f), 2.6f); RingArc(pp, c, 16, f, TurqLight, 2.6f); }); }
            }
            bool hintHere(string id) => host.HintNow?.Id == id && hintAlpha > 0f; // the hint's own ring stands there: no second ring
            if (g != null)
            {
                Place(doorMark, door, host.frame.World(g.S.At(g.S.GuardPost)), dIcon, host.EnterProgress < 0 && ds != "seen" && !hintHere("H14"), host.EnterProgress, dc, dIconC); // Glyph 016 r2: the ring round the marker only (it touched the Z)
                // "unk": who learns it next (Sky 2026-10-18, the Tech watches machines): the Intel chip's grammar beside the marker, the agent's
                // cameo and the Watch glyph, its rim turquoise when that agent is the one selected; a click selects, a second gives the Watch
                if (doorNext == null)
                {
                    doorNext = E(marks, null, 0, 0, 52, 26, pick: true); Round(doorNext, 13); doorNext.style.backgroundColor = new Color(0.039f, 0.149f, 0.141f);
                    var dc0 = E(doorNext, null, 7, 4, 18, 18); dc0.name = "cameo"; Round(dc0, 9); Border(dc0, 1f, Cream); var dg = E(doorNext, null, 29.5f, 4.2f, 15, 15); dg.name = "g";
                    doorNext.RegisterCallback<PointerDownEvent>(ev => { if (ev.button != 0) return; ev.StopPropagation(); host.DoorNextClick(); });
                    manifest.Add((doorNext, "image", () => "door next step")); Show(doorNext, false);
                }
                {
                    var nx = door && ds == "unk" && doorMark.resolvedStyle.display != DisplayStyle.None ? host.DoorNext() : null;
                    Show(doorNext, nx.HasValue);
                    if (nx is { } dn)
                    {
                        bool sel = host.Selected[(int)dn.by] && host.Agent == dn.by;
                        doorNext.style.left = doorMark.style.left.value.value + 26 + 6; doorNext.style.top = doorMark.style.top.value.value;
                        Bg(doorNext.Q("cameo"), $"portraits/cameo_{Op.Name(dn.by)}_normal", Color.white); Border(doorNext, 1.3f, sel ? Turq : Cream); Icon(doorNext.Q("g"), "watch", sel ? TurqLight : Cream);
                    }
                }
                // brief 018 grammar (Glyph fight r1 A-C): one car marker whose shape is its state. In the final fight the hold reach is a ring on
                // the ground, solid and quiet while the car is free, dashed with a slow drift while it is held; the marker turns to a lock with the
                // holder count; each holder carries a lock and a tether to the car (ink underlay), and both fade over a beat when it stops holding.
                var carW = host.frame.World(g.S.At(g.S.GetawayCar)); bool ring = host.CarRingOn, held = ring && host.CarHeldNow;
                var holders = held ? host.CarHolders() : new List<(string id, Vector3 at)>();
                Place(carMark, (car || ring) && !held, carW, "car", !ring && !hintHere("H15"), -1, TurqLight, TurqLight);
                if (held && Proj(carW, out var cq0)) { Show(heldMark, true); Text(heldNum, holders.Count.ToString()); heldMark.style.left = cq0.x - 13; heldMark.style.top = cq0.y - 13; }
                else Show(heldMark, false);
                float holdM = host.CarHoldM;
                if (ring && holdM > 0 && holdM <= 12f) // the 25 m reach of today fills the screen (noise, not a ring); drawn once brief 018 takes it to 8 m
                {
                    var pts = new Vector2[49]; bool all = true;
                    for (int k = 0; k <= 48; k++) { float a = Mathf.PI * 2 * k / 48; if (!Proj(carW + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * holdM, out pts[k])) { all = false; break; } }
                    if (all)
                    {
                        bool dashed = held; float drift = now * 0.6f % 1f;
                        paints.Add(pp =>
                        {
                            void Stroke(Color c, float wdt)
                            {
                                pp.strokeColor = c; pp.lineWidth = wdt;
                                if (!dashed) { pp.BeginPath(); pp.MoveTo(pts[0]); for (int k = 1; k <= 48; k++) pp.LineTo(pts[k]); pp.Stroke(); return; }
                                for (int k = 0; k < 48; k += 2) { pp.BeginPath(); var a0 = Vector2.Lerp(pts[k], pts[k + 1], drift); pp.MoveTo(a0); pp.LineTo(pts[k + 1]); pp.LineTo(Vector2.Lerp(pts[k + 1], pts[k + 2 > 48 ? 48 : k + 2], drift)); pp.Stroke(); }
                            }
                            Stroke(new Color(Ink.r, Ink.g, Ink.b, 0.5f), dashed ? 4.2f : 3.2f); Stroke(new Color(TurqLight.r, TurqLight.g, TurqLight.b, dashed ? 1f : 0.6f), dashed ? 2.2f : 1.4f);
                        });
                    }
                }
                foreach (var (id, at) in holders) holderSeen[id] = (at, -1f);
                holderIds.Clear(); foreach (var (id, _) in holders) holderIds.Add(id);
                var gone = new List<string>();
                foreach (var kv in holderSeen)
                {
                    var (hw0, lost) = kv.Value; if (!holderIds.Contains(kv.Key) && lost < 0) { lost = now; holderSeen[kv.Key] = (hw0, lost); }
                    float alpha = lost < 0 ? 1f : 1f - (now - lost) / 0.4f; if (alpha <= 0f || !ring) { gone.Add(kv.Key); continue; }
                    if (!Proj(hw0 + Vector3.up * 2.4f, out var hq) || !Proj(hw0, out var fq)) continue;
                    var e = WorldEl("s-disc_dark_24", 22, 22); e.style.left = hq.x - 11; e.style.top = hq.y - 11; e.style.opacity = alpha; Icon(e.Q("g"), "lock", Cream);
                    // the tether ends on the ring (a holder outside it, on the exit lane) or at the car's foot (inside it)
                    var d = hw0 - carW; d.y = 0; var endW = d.magnitude > holdM && holdM > 0 ? carW + d.normalized * holdM : carW;
                    if (!Proj(endW, out var eq)) continue;
                    var a = fq; var b = eq; float al = alpha;
                    paints.Add(pp => { int n = Mathf.Max(2, (int)(Vector2.Distance(a, b) / 6)); for (int pass = 0; pass < 2; pass++) { pp.fillColor = pass == 0 ? new Color(Ink.r, Ink.g, Ink.b, 0.5f * al) : new Color(Cream.r, Cream.g, Cream.b, 0.95f * al); float r0 = pass == 0 ? 2.2f : 1.2f; for (int i = 0; i <= n; i++) { pp.BeginPath(); pp.Arc(Vector2.Lerp(a, b, i / (float)n), r0, Deg(0), Deg(360)); pp.Fill(); } } });
                }
                foreach (var k in gone) holderSeen.Remove(k);
            }
            else { Show(doorMark, false); Show(carMark, false); }
            knobOn = live && m.Agent == (int)OpId.Enforcer && (host.EnterAvailable || host.ExtractAvailable) && host.Picked != 0; knobRing?.MarkDirtyRepaint();
        }
        bool knobOn;
        void DrawKnobRing(Painter2D p, Rect r)
        {
            if (!knobOn || knobs[0] == null || verbPlate == null) return;
            var kb = knobs[0].worldBound; var pb = verbPlate.worldBound; if (kb.width <= 0) return;
            var c = kb.center - pb.position; Dashed(p, c, kb.width / 2 - 3f, TurqLight, 1.6f, 12); // Glyph 016 r2 D: tight on the bezel, 2 px clear of the key number
        }

        // ---------------------------------------------------------------- edge pointers (Glyph §5): Vale, and units arriving; brief 016 (Glyph
        // review §4): edge cameos for the cell and edge pins for key people, so a turn or a pan never loses them

        /// <summary>Brief 016's edge cameos: a 24 px cameo of the agent with its F key and a cream wedge at it; turquoise rim when selected.</summary>
        sealed class EdgeCam { public VisualElement Root, Key; public Label KeyText; public int Agent = -1; public string Role; public HandoverHost.Pickable Target; }
        readonly List<EdgeCam> edgeCams = new();
        /// <summary>The timeline marks that hang on a known person (the pill's next mark): who to pin at the edge in the 10 s before.</summary>
        static string MarkPerson(string label) { var l = label.ToLowerInvariant(); return l.Contains("chime") ? "guard" : l.Contains("driver") ? "driver" : ""; }

        void Edges(Camera cam, float W, float H)
        {
            int used = 0, camUsed = 0;
            var placed = new List<Rect>();
            // the HUD groups an edge mark slides clear of (panel space), with 6 px air
            var blocks = new List<Rect>();
            foreach (var e in new[] { column, verbPlate, pill, keys, scope, historyKey2, goal, respPill, intel, chip2, sheet, beatsBox, plateStub })
                if (e != null && e.resolvedStyle.display != DisplayStyle.None && e.worldBound.width > 0) { var b = e.worldBound; float air = e == goal ? 12 : 6; blocks.Add(Rect.MinMaxRect(b.xMin - air, b.yMin - air, b.xMax + air, b.yMax + air)); } // Glyph 016 r2 G: 12 px clear of the goal line
            const float M = 26; // the marks' centres run round this inset rectangle
            float pw = W - 2 * M, ph = H - 2 * M, per = 2 * (pw + ph);
            Vector2 Perim(float d) { d = Mathf.Repeat(d, per); if (d < pw) return new Vector2(M + d, M); d -= pw; if (d < ph) return new Vector2(W - M, M + d); d -= ph; if (d < pw) return new Vector2(W - M - d, H - M); d -= pw; return new Vector2(M, H - M - d); }
            float PerimOf(Vector2 a) { if (Mathf.Abs(a.y - M) < 0.5f) return a.x - M; if (Mathf.Abs(a.x - (W - M)) < 0.5f) return pw + (a.y - M); if (Mathf.Abs(a.y - (H - M)) < 0.5f) return pw + ph + (W - M - a.x); return pw + ph + pw + (H - M - a.y); }
            bool Free(Vector2 c, float half) { var r = new Rect(c.x - half, c.y - half, 2 * half, 2 * half); foreach (var b in blocks) if (b.Overlaps(r)) return false; foreach (var q in placed) if (q.Overlaps(r)) return false; return true; }
            // where a world point off the frame meets the edge, slid along it until clear of the HUD and the other marks; null when on screen
            (Vector2 at, Vector2 dir)? Spot(Vector3 w, float half)
            {
                if (!cam) return null; var sp = cam.WorldToScreenPoint(w);
                var vp = cam.WorldToViewportPoint(w); if (vp.z > 0 && vp.x > 0.03f && vp.x < 0.97f && vp.y > 0.03f && vp.y < 0.97f) return null;
                var centre = new Vector2(W / 2, H / 2);
                var pp = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(sp.x, Screen.height - sp.y)); if (sp.z < 0) pp = centre - (pp - centre);
                var dir = pp - centre; if (dir.sqrMagnitude < 1) return null; dir.Normalize();
                float k = Mathf.Min(dir.x > 0 ? (W - M - centre.x) / dir.x : dir.x < 0 ? (M - centre.x) / dir.x : 1e9f, dir.y > 0 ? (H - M - centre.y) / dir.y : dir.y < 0 ? (M - centre.y) / dir.y : 1e9f);
                float d0 = PerimOf(centre + dir * k);
                for (float step = 0; step < per / 2; step += 4)
                    foreach (var d in step == 0 ? new[] { d0 } : new[] { d0 + step, d0 - step })
                    {
                        var a = Perim(d); if (!Free(a, half)) continue;
                        placed.Add(new Rect(a.x - half - 3, a.y - half - 3, 2 * half + 6, 2 * half + 6));
                        var to = pp - a; return (a, to.sqrMagnitude > 1 ? to.normalized : dir);
                    }
                return null;
            }
            void Pointer(Vector3 w, string badge, Color c, string count)
            {
                if (Spot(w, 17) is not { } spw) return; var (at, dir) = spw;
                // Glyph round 2: the inner disc sat ~3 px off its bezel: a CSS border moved the content box. The disc art (disc_dark_30, 76 px with a
                // shadow: the disc's centre is 0.4 px below the sprite's) carries the badge on its own centre; the ring is painted on the same centre
                while (edgeEls.Count <= used) { var e = E(marks, "s-disc_dark_30", 0, 0, 30, 30); E(e, null, 5, 5.2f, 20, 20).name = "b"; var n = L(e, "keycap"); n.name = "n"; n.AddToClassList("s-plate_light"); n.style.position = Position.Absolute; n.style.left = 22; n.style.top = -8; n.style.paddingLeft = 3; n.style.paddingRight = 3; edgeEls.Add(e); }
                var el = edgeEls[used++]; Show(el, true); el.style.left = at.x - 15; el.style.top = at.y - 15.2f; var ringC = at;
                paints.Add(p2 => { p2.strokeColor = c; p2.lineWidth = 2f; p2.BeginPath(); p2.Arc(ringC, 16.5f, Deg(0), Deg(360)); p2.Stroke(); });
                Bg(el.Q("b"), badge); var nl = el.Q<Label>("n"); Show(nl, count.Length > 0); Text(nl, count);
                var tip = at + dir * 24; var side = new Vector2(-dir.y, dir.x) * 5;
                paints.Add(p2 => { p2.fillColor = c; p2.BeginPath(); p2.MoveTo(tip); p2.LineTo(at + dir * 18 + side); p2.LineTo(at + dir * 18 - side); p2.ClosePath(); p2.Fill(); });
            }
            EdgeCam CamEl()
            {
                while (edgeCams.Count <= camUsed)
                {
                    var ec = new EdgeCam(); ec.Root = E(marks, null, 0, 0, 24, 24, pick: true); Round(ec.Root, 12); ec.Root.style.backgroundColor = Ink;
                    ec.Key = E(ec.Root, "s-plate_light", 16, -13, float.NaN, 18); ec.Key.style.minWidth = 22; ec.KeyText = L(ec.Key, "keycap"); ec.KeyText.style.fontSize = 10f; ec.KeyText.style.flexGrow = 1; ec.KeyText.style.unityTextAlign = TextAnchor.MiddleCenter; ec.KeyText.style.paddingLeft = 4; ec.KeyText.style.paddingRight = 4; // Glyph 016 r2-3: 10 px type on the order plate's keycap, half a cameo out ec.KeyText.style.paddingLeft = 3; ec.KeyText.style.paddingRight = 3;
                    ec.Root.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0) return; e.StopPropagation(); if (ec.Agent >= 0) host.CentreOn((OpId)ec.Agent); else if (ec.Target != null) host.TargetFind(ec.Target); else if (!string.IsNullOrEmpty(ec.Role)) FindPerson(ec.Role); });
                    manifest.Add((ec.Root, "image", () => "edge cameo")); // art and its F key: no running text
                    edgeCams.Add(ec);
                }
                var x = edgeCams[camUsed++]; Show(x.Root, true); return x;
            }
            void Wedge(Vector2 at, Vector2 dir, Color c) { var tip = at + dir * 19; var side = new Vector2(-dir.y, dir.x) * 4.5f; paints.Add(p2 => { p2.fillColor = c; p2.BeginPath(); p2.MoveTo(tip); p2.LineTo(at + dir * 14 + side); p2.LineTo(at + dir * 14 - side); p2.ClosePath(); p2.Fill(); }); }
            if (!m.Ended && !m.BriefingOpen && host.Game != null)
            {
                var g = host.Game; int budget = 4; // Glyph: at most four at the edge at once (Vale's arrow, the cameos, the pins)
                int before = used; Pointer(host.ValeKnown + Vector3.up * 1f, "badges/npc_councillor", TurqLight, ""); if (used > before) budget--;
                // the cell: the selected agent always while off screen; another only when busy, hurt, down or seen (an idle one's portrait says ready)
                var order = new List<int>(); for (int k = 0; k < 4; k++) if (host.Selected[k]) order.Add(k); for (int k = 0; k < 4; k++) if (!host.Selected[k]) order.Add(k);
                foreach (int k in order)
                {
                    if (budget <= 0) break; var op = g.Ops[k]; if (op.Inside) continue;
                    var s = m.Slots[k]; bool sel = host.Selected[k], matters = sel || s.Busy || s.Hurt || s.Down || host.RunEye(k) == "open";
                    if (!matters || host.Views?.Find("op/" + Op.Name((OpId)k)) is not Transform t || !t.gameObject.activeSelf) continue;
                    if (Spot(t.position + Vector3.up, 13) is not { } spa) continue; var (at, dir) = spa;
                    var ec = CamEl(); ec.Agent = k; ec.Role = null; ec.Target = null; ec.Root.style.left = at.x - 12; ec.Root.style.top = at.y - 12;
                    Bg(ec.Root, $"portraits/cameo_{Op.Name((OpId)k)}_normal"); Border(ec.Root, 1.6f, sel ? Turq : Cream); Show(ec.Key, true); Text(ec.KeyText, host.AgentKeyLabel(k)); if (ec.Root.Q("pin") is VisualElement pn) Show(pn, false);
                    Wedge(at, dir, Cream); budget--;
                }
                // key people: a pin only in the 10 s before the next plan mark that hangs on them (the goal, Vale, keeps his own arrow)
                var tl = host.Text?.Timeline;
                if (tl != null) foreach (var (atS, label, known) in tl)
                    {
                        var p = atS.Split(':'); int tick = (int.Parse(p[0]) * 60 + int.Parse(p[1])) * Game.Hz; if (tick <= g.Tick) continue;
                        var role = known ? MarkPerson(label) : ""; if (budget > 0 && role.Length > 0 && tick - g.Tick <= 10 * Game.Hz && KnownOf(role) && host.Views?.Find(role) is Transform pt && pt.gameObject.activeSelf && Spot(pt.position + Vector3.up, 13) is { } psp)
                        { var (pa, pd) = psp; var ec = CamEl(); ec.Agent = -1; ec.Role = role; ec.Target = null; ec.Root.style.left = pa.x - 12; ec.Root.style.top = pa.y - 12; ec.Root.style.backgroundImage = StyleKeyword.Null; Border(ec.Root, 1.6f, Cream); Show(ec.Key, false); PinBadge(ec, role); Wedge(pa, pd, Cream); budget--; }
                        break; // the next mark only
                    }
                // target mode: the valid targets off screen get edge pins with the verb's glyph (up to four); a click pans to one and focuses it
                if (host.TargetMode)
                {
                    int tb = 4; var icon = m.VerbIcon[Mathf.Clamp(host.Picked, 0, 3)];
                    foreach (var tp in host.TargetMarks)
                    {
                        if (tb <= 0) break; if (Spot(tp.Anchor, 13) is not { } tsp) continue; var (ta, td) = tsp;
                        var ec = CamEl(); ec.Agent = -1; ec.Role = null; ec.Target = tp; ec.Root.style.left = ta.x - 12; ec.Root.style.top = ta.y - 12; ec.Root.style.backgroundImage = StyleKeyword.Null; Border(ec.Root, 1.6f, TurqLight); Show(ec.Key, false);
                        var pin = ec.Root.Q("pin"); if (pin == null) { pin = E(ec.Root, null, 3, 3, 18, 18); pin.name = "pin"; } Show(pin, true); Icon(pin, icon, TurqLight); Wedge(ta, td, TurqLight); tb--;
                    }
                }
                var seen = new List<Vector3>();
                foreach (var a in m.Arrivals) { bool near = false; foreach (var s in seen) if ((s - a.World).sqrMagnitude < 400f) near = true; if (near) continue; seen.Add(a.World); int n = 0; foreach (var b in m.Arrivals) if ((b.World - a.World).sqrMagnitude < 400f) n++; Pointer(a.World, "badges/signal_police", Amber, n > 1 ? n.ToString() : ""); }
            }
            for (int i = used; i < edgeEls.Count; i++) Show(edgeEls[i], false);
            for (int i = camUsed; i < edgeCams.Count; i++) Show(edgeCams[i].Root, false);
        }

        /// <summary>A key person's pin: their role badge (the same as their pip and scope blip) inside the edge disc.</summary>
        void PinBadge(EdgeCam ec, string role)
        {
            var b = ec.Root.Q("pin"); if (b == null) { b = E(ec.Root, null, 3, 3, 18, 18); b.name = "pin"; }
            Show(b, true); Bg(b, CastBadge(role), Color.white);
        }

        // ---------------------------------------------------------------- the going-loud card, the briefing, the end card without sentences

        string loudKey = "";
        void LoudLess(bool alt)
        {
            if (!m.LoudOpen || host.Loud == null) { loudKey = ""; return; }
            var c = host.Loud.Card;
            Show(loudSub, alt); foreach (var l in loudRows) Show(l.parent, alt && l.text.Length > 0); Show(loudHold, alt); Show(loudTitle, alt);
            var key = $"{c.AlertTo}|{c.PoliceInS}|{c.RivalsInS}|{c.Seal}|{c.GuardAllyLost}|{c.WarningLost}|{c.DownedCaptured}|{c.TrailPerHit}|{host.Loud.Takedown}";
            if (key == loudKey) return; loudKey = key; loudIcons.Clear(); loudLost.Clear();
            void Part(string art, string text, Color? tint = null) { var p = E(loudIcons); p.style.flexDirection = FlexDirection.Row; p.style.alignItems = Align.Center; p.style.marginRight = 14; p.style.marginBottom = 4; var e = E(p, null, w: 18, h: 18); Art(e, art, tint); var l = L(p, "chip-value"); l.text = text; l.style.marginLeft = 5; }
            Part("icons/singles/p_alert", c.AlertTo.ToString());
            if (c.PoliceInS >= 0) Part("badges/signal_police", $"{c.PoliceInS} s");
            if (c.RivalsInS >= 0) Part("badges/signal_rival", $"{c.RivalsInS} s");
            if (c.Seal >= 0) Part("icons/singles/lock", HandoverHost.MissionClock(c.Seal));
            if (!host.Loud.Takedown) Part("icons/singles/p_trail", $"+{c.TrailPerHit}/hit");
            void Lost(string badge) { var e = E(loudLost, null, w: 26, h: 26); e.style.marginRight = 10; Bg(e, badge); P(e, 0, 0, 26, 26, (p, r) => { p.strokeColor = Red; p.lineWidth = 2.4f; p.BeginPath(); p.MoveTo(new Vector2(3, 23)); p.LineTo(new Vector2(23, 3)); p.Stroke(); }); }
            if (c.GuardAllyLost) Lost("badges/signal_guard");
            if (c.WarningLost) Lost("badges/npc_waitress");
            if (c.DownedCaptured) { var e = E(loudLost, "s-disc_dark_24", w: 26, h: 26); Icon(E(e, null, 5, 5, 16, 16), "restrain", Cream); P(e, 0, 0, 26, 26, (p, r) => { p.strokeColor = Red; p.lineWidth = 2.4f; p.BeginPath(); p.MoveTo(new Vector2(3, 23)); p.LineTo(new Vector2(23, 3)); p.Stroke(); }); }
        }

        void BriefLess(bool alt)
        {
            if (!m.BriefingOpen || briefObjective == null) return;
            // at rest: the title, the goal line, three numbers, the four portraits and Begin; the sentences come back on Alt
            foreach (var e in new VisualElement[] { briefObjective.parent, briefTarget.parent, briefWin.parent.parent, briefWays[0].title.parent.parent }) Show(e, alt);
            if (briefFootLine != null) Show(briefFootLine, alt);
            Show(briefArms.parent, alt); Show(briefSub, alt); if (brief.childCount > 0) Show(brief[0], alt); // at rest: the title alone (the tab and the place and time are Alt)
            Text(briefGoal, (host.Text?.BriefGoal ?? "").Replace("[", "").Replace("]", "")); Show(briefGoal, !alt); Show(briefPeople, !alt);
            Text(briefClose, host.Text?.Close ?? "8:30"); Text(briefPurse, m.Cash); Text(briefAlert, $"{m.Alert}/3");
            brief.style.width = alt ? 700 : 520; var bsz = root.panel.visualTree.layout.size; brief.style.left = ((float.IsNaN(bsz.x) ? 1920 : bsz.x) - (alt ? 700 : 520)) / 2;
        }

        void EndLess()
        {
            if (!m.Ended) return;
            var g = host.Game; var t = host.Text; string line = t == null ? (m.EndLines.Count > 0 ? m.EndLines[0] : "") : g.Result == Outcome.Success ? t.EndWin : g.Result == Outcome.Withdrawn ? t.EndWithdraw : t.EndFail.TryGetValue(g.FailCode, out var f) ? f : (m.EndLines.Count > 0 ? m.EndLines[0] : "");
            Text(endTitle, line); Show(endLine, endDetailOpen);
            Text(endTally, HandoverHost.MissionClock(g.Tick)); Text(endCash, $"${g.S.Purse - g.Cash:N0}"); Text(endTrail, $"+{g.Trail}");
            var stl = host.StandingLines(); bool hasSt = stl.Count > 0; Show(endStand, hasSt); Show(endStandArrow, hasSt && stl[0].dir != 0);
            if (hasSt) { var st = stl[0]; endStandDir = st.dir; Text(endStand, $"{st.band} {(st.to > 0 ? "+" : st.to < 0 ? "\u2212" : "")}{Mathf.Abs(st.to)}"); endStandArrow.MarkDirtyRepaint(); }
            float ew = endDetailOpen ? 560 : EndCardW; end.style.width = ew; var esz = root.panel.visualTree.layout.size; end.style.left = ((float.IsNaN(esz.x) ? 1920 : esz.x) - ew) / 2;
        }
    }
}
