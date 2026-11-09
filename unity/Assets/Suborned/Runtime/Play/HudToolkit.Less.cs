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

        VisualElement nextUnknown, nextTip;
        readonly List<VisualElement> peopleDiscs = new(); VisualElement historyKey2;
        VisualElement keys, pauseIcon, goal, goalCameo, goalGlyph, goalWords, sheet, sheetHead, sheetCameo, sheetBadge, sheetRows, sheetTip, chip2, chipLead, chipFields, loudIcons, loudLost;
        Label endCash, endTrail, endStand; Paint endStandArrow; int endStandDir;
        Label sheetName, sheetNote, sheetTipText, chipRefused, endTally, briefGoal;
        Paint world; // the street's painted marks (rings, wedges, dotted lines, arrows), behind the HUD's plates
        readonly List<(VisualElement root, VisualElement icon, Label text, VisualElement bar, VisualElement fill, VisualElement x)> busy = new();
        readonly List<(VisualElement root, Label text, VisualElement tail)> bubbleEls = new();
        sealed class Row { public VisualElement Root, Glyph, Fields, Alt, Key, Cameo, TagIcon; public Label Name, KeyText; public int Index; public string FieldsKey = "", AltKey = ""; }
        readonly List<Row> rows = new();
        readonly List<VisualElement> worldEls = new(); int worldUsed; // small marks over people (eyes, pips, "?", turned marks, activity icons)
        readonly List<(VisualElement root, VisualElement badge, VisualElement name)> pipEls = new();
        readonly List<VisualElement> edgeEls = new();
        readonly List<VisualElement> briefPortraits = new(); VisualElement briefPeople, briefFacts, briefAltBox;
        string goalKey = "", chipKey = "";
        int hoverRow = -1; bool lessBuilt;
        /// <summary>Elements for the manifest of a capture: (element, kind).</summary>
        readonly List<(VisualElement e, string kind, Func<string> text)> manifest = new();
        /// <summary>Brief 070 (glyph-page-budget §2; tools/hud-budget/page.ts): a manifest element's page part, its pin number (or `car`) and its rows as wrapped.</summary>
        readonly Dictionary<VisualElement, string> mfPart = new(), mfN = new();
        readonly Dictionary<VisualElement, string[]> mfRows = new();

        void BuildLess()
        {
            lessBuilt = true;
            world = P(marks, 0, 0, 0, 0, DrawWorld); world.style.left = 0; world.style.top = 0; world.style.right = 0; world.style.bottom = 0; world.style.width = StyleKeyword.Auto; world.style.height = StyleKeyword.Auto;
            world.SendToBack();
            // the roster at 0.8 (Glyph §1): the kit column scaled from its top left
            column.style.transformOrigin = new TransformOrigin(0, 0, 0); column.style.scale = new Scale(new Vector2(RosterScale, RosterScale));
            manifest.Add((column, "image", () => "roster portraits"));
            // brief 101: the pause button, the next mark and the key people are the clock's (HudToolkit.Clock)
            BuildBookKey(); historyKey2 = bookKey; // brief 096: the Book key takes the notes key's place (HudToolkit.BookKey); the history is the book's Journal
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
                x.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.PlateStop(k); e.StopPropagation(); } });
                x.RegisterCallback<PointerEnterEvent>(_ => TipFor(k, true)); x.RegisterCallback<PointerLeaveEvent>(_ => TipFor(k, false));
                r.RegisterCallback<PointerEnterEvent>(_ => busyHover = k); r.RegisterCallback<PointerLeaveEvent>(_ => { if (busyHover == k) busyHover = -1; });
                // Sky: the pace is the chip's lead glyph (footprints walk, runner run); a click on it switches a moving agent's pace (G does the same)
                ic.pickingMode = PickingMode.Position; ic.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && host.Game.Ops[k].Walk != null) { host.ToggleGait(k); e.StopPropagation(); } });
                var ey = E(r, null, w: Glyph, h: Glyph, pick: true); ey.style.marginLeft = 8; ey.style.flexShrink = 0; Icon(E(ey, null, 0, 0, Glyph, Glyph), "eye_shut", Cream); /* bare, its whole 32 px box ("?" included) at 14 px (Glyph's minimum) */
                int ek = i; ey.RegisterCallback<PointerEnterEvent>(_ => { var e2 = host.RunEye(ek); EyeTipShow(ey, HudModel.EyeTip(e2, "")); }); ey.RegisterCallback<PointerLeaveEvent>(_ => EyeTipShow(null, null)); busyEyes.Add(ey);
                busy.Add((r, ic, tx, bar, fill, x));
            }
            BuildHintLine(); // brief 101 round 3: the hint line as a street label, the reopen disc a round tab (HudToolkit.HintLine)
            BuildCrucial(); // brief 096: the slim feed, one crucial card above the Book key (HudToolkit.Crucial); the story beats live in the Journal
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
            // brief 101: the units on their way are the clock's street row (HudToolkit.Clock); the old response pill stays hidden
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
            BuildRing(); BuildKeys096(); // brief 030; brief 096: the pause button and the turn buttons (HudToolkit.Keys096)
        }

        bool goalHover; int busyHover = -1; string goalText = "";

        // ---------------------------------------------------------------- per frame

        void LessFrame()
        {
            if (!lessBuilt) BuildLess();
            if (host.Game != runSeen) { runSeen = host.Game; hintShows.Clear(); pulseAt.Clear(); } // a new run (Begin, Replay): the HUD's per-run memory starts again
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
                float WinMid(int i) => ColY + BezelAt(i).y * RosterScale; // the bezel's measured centre
                float plateX = ColX + colW + 4, plateY = WinMid(a) - PlateH / 2; // brief 014: chips and the plate share one line, 4 off the column
                verbPlate.style.left = plateX; verbPlate.style.top = plateY;
                PlateFold(plateX, WinMid(a)); bool folded = PlateFolded && PlateOn; // brief 030 (no plate): LiveFrame hides the plate and its stub
                for (int k = 0; k < 4; k++)
                {
                    bool has = m.VerbHas[k], refused = m.VerbRefused[k]; knobs[k].style.opacity = has ? 1f : refused ? 0.55f : 0.28f; Show(knobIcons[k], has || refused); Show(verbPlate.Q("badge" + k), has || refused); // an empty knob: a quiet socket; a refused one (Fire while carrying Vale) keeps its glyph, greyed
                    Class(knobs[k], "selected", has && host.Picked == k); // the turquoise ring means "armed: a right click gives this"; nothing else
                }
                KnobPredFrame(); // engine 0.28: the dashed likely / worse marks on the knobs
                if (host.Picked < 0) Show(armedPlate, false); // the name plate only while a verb is armed (Glyph §1)
                else if (m.TakedownArmed) Text(armedName, host.Text != null && host.Text.Actions.TryGetValue("takedown", out var tda) && tda.Ways.TryGetValue(m.Way, out var wy) ? wy.label : m.Way == "kill" ? "Kill" : "Knock out"); // the way, not the verb
                if (armedPlate.style.display != DisplayStyle.None)
                {
                    float ax = folded ? plateX : plateX + KnobX0 + 24 + m.VerbLit * 47.6f - 40, ay = folded ? WinMid(a) + StubH / 2 + 4 : plateY + PlateH + 6; // techwatch fix 2: 6 px clear of the plate's bottom rim
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
                    string hw = host.controls.HoldActs ? host.PlateWords(i) : ""; // item 13 (d): a down agent's plate says "down" (it was the grey glyph alone) // brief 041: the plate says the pill's words (aiming, queued, → Inez; brief 101: never an act's seconds)
                    var (whyIcon, whyWords, whyAlpha) = host.controls.HoldActs && !ended ? host.WhyChipOf(i) : ("", "", 0f); bool why = whyWords.Length > 0; // brief 067 C4 (Glyph): a refused squad order says why on the portrait's chip
                    if (!why && !ended && !s.Down && !s.InCar && hw.Length == 0 && !s.Busy && host.UnderFireNow[i].Count > 0) { var (uc, uw) = host.UnderFireWords(i, alt); if (uw.Length > 0) { whyIcon = WhyIcon(uc) is { Length: > 0 } wi0 ? wi0 : "bang"; whyWords = uw; whyAlpha = 1f; why = true; } } // engine 0.33.0 (Fable's less.why): beside the under-fire cue, its main cause in words (Alt: every cause), in the chip slot when the agent has no other chip
                    bool on = !colWins[i].Street && ((s.Busy || s.Pace.Length > 0 || hw.Length > 0) && !s.Down && !ended && !s.InCar || why); Show(r, on); if (!on) continue; /* brief 101 step 4: the street faces carry the act, its why and its × (HudToolkit.Faces) */ /* Glyph's 0.2.104 review #7: a downed agent's portrait carries the downed mark; no second disc beside it */ // brief 018 (Glyph r1): aboard, the in-car badge says it; no chip
                    float bx = i == a ? SelectedChipX(plateX, folded) : ColX + colW + 4; /* Glyph fight r1 #6: 6 px clear of the fold tab (it stands 16 px out of the plate) */ r.style.left = bx; r.style.top = WinMid(i) - ChipH / 2; // anchored to its portrait's row (or after the plate)
                    r.style.opacity = why ? whyAlpha : 1f; tx.style.fontSize = why ? 12.5f : StyleKeyword.Null; tx.style.color = StyleKeyword.Null;
                    if (why)
                    {   // the why chip (Glyph 067 C4): the busy chip's frame, the cause's own icon (14 px cream), 6 px, two words (Jost 12.5 cream); no bar, no seconds, no ×
                        Icon(ic, whyIcon, Cream); ic.style.width = 14; ic.style.height = 14; Text(tx, whyWords); Show(tx, true); Show(bar, false); Show(x, false); Show(busyEyes[i], false);
                        tx.style.width = StyleKeyword.Auto; tx.style.marginLeft = 6; tx.style.paddingRight = 0; tx.style.unityTextAlign = TextAnchor.MiddleLeft;
                        r.style.width = StyleKeyword.Auto; r.style.minWidth = StyleKeyword.Null; r.style.paddingLeft = 16; r.style.paddingRight = 16; r.style.unityBackgroundImageTintColor = Color.white;
                        if (r.Q("vale-cameo") is VisualElement vc0) Show(vc0, false);
                        continue;
                    }
                    bool moving = s.Phase is "walking" or "running", running = s.Phase == "running";
                    Icon(ic, s.Down ? "s_downed" : host.FireRefusalWord(i).Length > 0 ? "refused" : hw == "queued" ? "nextevent" : moving ? (running ? "run" : "s_moving") : hw is "drawing" or "aiming" or "firing" or "holding" ? "fire" : hw == "under fire" ? "bang" : host.controls.HoldActs && host.PlateDoneTone(i) is { Length: > 0 } dtn ? (dtn == "ok" ? "check" : dtn == "soft" ? "asked" : dtn == "cancel" ? "close_x" : "refused") : s.Icon, running ? TurqLight : Cream); // brief 043: the plate's word after a landing or a cancel
                    r.style.unityBackgroundImageTintColor = running ? new Color(0.78f, 0.97f, 0.96f) : Color.white; fill.style.backgroundColor = running ? Cream : TurqLight; // run: a lighter plate and a cream bar (kit tones, never a signal)
                    string eye = running ? host.RunEye(i) : ""; var eb = busyEyes[i]; Show(eb, eye.Length > 0); // a run is noticed: who could see it, as on the price line
                    if (eye.Length > 0) Icon(eb[0], "eye_" + eye, eye == "open" ? new Color(0.878f, 0.227f, 0.184f) : Cream);
                    bool carry = s.Pace.Length > 0 && s.Progress < 0;
                    bar.style.visibility = s.Progress >= 0 && !s.Down ? StyleKeyword.Null : Visibility.Hidden; fill.style.width = Length.Percent(100f * Mathf.Clamp01(s.Progress / 1000f)); // the slot keeps its place
                    // brief 049 row 9 (Glyph's 0.2.94 review #5c): a word alone ("aiming", "refused", "not yet") is the icon alone, on the small plate, never a
                    // bar-wide plate with a word at its far end; brief 101 (Sky 2026-11-08): an act or a walk is its icon and the bar, no seconds, no number slot
                    bool hwDigit = false; for (int ci = 0; ci < hw.Length && !hwDigit; ci++) hwDigit = char.IsDigit(hw[ci]); bool hwWord = hw.Length > 0 && !hwDigit; /* Redline 064 #6: no LINQ a row a frame */ string hwShown = hwWord ? "" : hw;
                    Text(tx, s.Down && hw.Length == 0 || hwWord ? "" : hw.Length > 0 ? hw : carry ? PaceWords(s.Pace) : "");
                    tx.style.width = hwShown.Length > 0 ? StyleKeyword.Auto : ValueW;
                    // Glyph's N12 (0.2.122): a snug number ("1 s") sat 2 px from the bar's end cap: left-set 6 px after the bar, a little room on its right
                    tx.style.unityTextAlign = hwShown.Length > 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight; tx.style.marginLeft = hwShown.Length > 0 ? 6 : 8; tx.style.paddingRight = hwShown.Length > 0 ? 4 : 0;
                    if (Stress && tx.text.Length > 0) Text(tx, carry ? "1.4 m/s" : "88 s");
                    bool iconOnly = (s.Progress < 0 || s.Down || hwWord) && tx.text.Length == 0 && hwShown.Length == 0; // no end ("holding the loop"), down, or a word: the icon alone (Glyph §1)
                    bool noValue = !iconOnly && tx.text.Length == 0 && hwShown.Length == 0; // brief 101: the bar alone, as wide as it was, the value's slot gone
                    float cw = iconOnly ? IconOnlyW : noValue ? ChipW - ValueW - 8 : ChipW; if (hwShown.Length > 0) { r.style.width = StyleKeyword.Auto; r.style.minWidth = ChipW; float rw = r.resolvedStyle.width; if (!float.IsNaN(rw) && rw > cw) cw = rw; } else { r.style.minWidth = StyleKeyword.Null; r.style.width = cw; } Show(bar, !iconOnly); Show(tx, !iconOnly && !noValue); // one width: the bar takes what the eye does not
                    if (carry && hw.Length == 0 && !s.Down) { Show(bar, false); r.style.width = StyleKeyword.Auto; r.style.minWidth = StyleKeyword.Null; tx.style.width = StyleKeyword.Auto; tx.style.marginLeft = 8; } // Glyph's 0.2.104 review #7: carrying, the plate is its icon and pace (no empty bar slot between them)
                    else tx.style.marginLeft = StyleKeyword.Null;
                    { // Aster's call (brief 062 "Portraits and ids"): the Enforcer's plate names who he carries: Vale's model cameo, 14 px (18 and 15 failed the rim audit), before the pace
                        var g0 = host.Game; bool vale = i == (int)OpId.Enforcer && g0 != null && !s.Down && (g0.Vale == ValeAt.WithEnforcer || g0.Ops[i].CarryBody.Length > 0 && host.CastOf("councillor") is Person vp && g0.Ops[i].CarryBody == vp.Id);
                        var vc = r.Q("vale-cameo");
                        if (vale && vc == null) { vc = E(r, null, w: 14, h: 14); vc.name = "vale-cameo"; /* 14 px: the largest that sits 4 px clear of the 36 px plate's rim after layout rounding (the audit wants 10.2 px; 18 and 15 px failed) */ vc.style.flexShrink = 0; vc.style.marginLeft = 6; r.Remove(vc); r.Insert(Mathf.Min(r.childCount, r.IndexOf(ic) + 1), vc); Bg(vc, "portraits/cameo_councillor_model", Color.white); }
                        if (vc != null) Show(vc, vale);
                        if (vale) { r.style.width = StyleKeyword.Auto; r.style.minWidth = StyleKeyword.Null; } /* the plate grows by the cameo */
                    }
                    float isz = iconOnly ? 13 : Glyph; ic.style.width = isz; ic.style.height = isz; r.style.paddingLeft = iconOnly ? (IconOnlyW - isz) / 2 : ChipPad; r.style.paddingRight = iconOnly ? (IconOnlyW - isz) / 2 : ChipPad; /* Glyph round 3: the loop glyph centred at ~75% in the small chip */
                    bool canX = (s.CanCancel || hw is "firing" or "aiming" or "drawing" or "too far" or "no line") && (busyHover == i || tipFor == i); /* brief 041: × also stops a lasting fire order */ x.style.left = cw + 4;
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
            ClockFrame(); // brief 101: the clock as street labels (HudToolkit.Clock): the pause button, the time, the lamps, Trail, money, the next key event, the faces
            BookKeyFrame(!ended && !m.BriefingOpen);
            intel.style.top = ClockBottom() + 8;
            // Intel rows: the sentence column is Alt only
            if (m.IntelOpen) for (int i = 0; i < 4; i++) { var u = intelRows[i]; Show(u.Next, alt); Show(u.Secret, alt && m.Intel[i].Secret.Length > 0); }
            // the response's drawer (a click on the clock's police row, or R; never on Alt: its four rows are a table, and Alt is fragments, the budget's block rule)
            Show(respPill, false); bool drawer = host.RespAny && m.ResponseOpen; Show(respList, drawer); // brief 101: the units coming are the clock's street row (HudToolkit.Clock)
            if (drawer) respList.style.top = m.IntelOpen ? intel.resolvedStyle.top + (float.IsNaN(intel.resolvedStyle.height) ? 0 : intel.resolvedStyle.height) + 8 : ClockBottom() + 8;
            // the history drawer over the beats' place
            history.style.bottom = 14 + 36 + 8; // opens above its key
            GoalLine(W);
            CrucialFrame(!m.Ended && !m.BriefingOpen);
            Bubbles(cam);
            Pops(cam);
            Sheet(alt || host.PricePeek, W, H); // brief 096: a price rested on 0.6 s shows its breakdown, as Alt does
            Chip(alt || host.PricePeek, W);
            RingFrame(); CursorFrame(); // brief 030: the ring, the cursor's mark
            StreetLess(alt, cam);
            AttentionHud(W, H); // brief 031: the Watch ring, the person watchers' marks, the callouts (HudToolkit.Attention)
            BubblesOffCallouts(W, H); // Glyph G5 (0.2.135): speech steps aside from the attention callouts (before the tails and leaders are drawn)
            FightLabLayer(alt, cam); // brief 027: Glyph's finished fight HUD (HudToolkit.FightLab), with the lab preset
            Pips(cam); BubbleTails();
            foreach (var (br, _, _) in bubbleEls) if (br.resolvedStyle.display != DisplayStyle.None) br.BringToFront(); // Glyph 016 r2 B: speech above the pins
            ClearUnderChip(alt);
            Edges(cam, W, H);
            GoCommit(W, H);
            LoudLess(alt);
            BriefLess(alt);
            EndLess();
            ScopeFrame();
            IntelFrame();
            LiveFrame(W); // brief 030 (no plate) and brief 033 (HudToolkit.Live)
            for (int i = worldUsed; i < worldEls.Count; i++) Show(worldEls[i], false);
            world.MarkDirtyRepaint();
        }

        bool InView(Vector2 q) { var sz = root.panel.visualTree.layout.size; return q.x > 0 && q.y > 0 && q.x < sz.x && q.y < sz.y; }
        /// <summary>Glyph N7 (0.2.125, after 0.2.127): on screen and clear of the roster's band (the column, the Squad and weapons buttons under it): a
        /// speaker or a pip there would draw behind the roster.</summary>
        bool InPlay(Vector2 q) => InView(q) && !(column != null && column.resolvedStyle.display != DisplayStyle.None && column.worldBound.width > 0 && q.x < column.worldBound.xMax + 12f && q.y < (squadBtn != null && squadBtn.resolvedStyle.display != DisplayStyle.None ? squadBtn.worldBound.yMax : column.worldBound.yMax) + 40f);

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


        // ---------------------------------------------------------------- beats, bubbles, pops

        static readonly HashSet<string> Cell = new() { "scout", "fixer", "tech", "enforcer" };
        static readonly string[] CellOrder = { "scout", "fixer", "tech", "enforcer" };

        // ---------------------------------------------------------------- the order sheet and the chip

        HandoverHost.ActionPanel sheetFor; bool sheetStress; float sheetW = 374; readonly Dictionary<string, float> sheetCols = new(); const float ColGap = 10;
        Label probe;

        /// <summary>A text's width in a type style (the probe label sits in the panel, invisible), in panel px.</summary>
        float Measure(string text, string style, float size)
        {
            if (probe == null) { probe = L(root, style); probe.style.position = Position.Absolute; probe.style.visibility = Visibility.Hidden; probe.style.left = -2000; }
            probe.ClearClassList(); probe.AddToClassList("t-" + style); probe.style.fontSize = size; probe.style.unityFontStyleAndWeight = FontStyle.Bold; probe.style.unityFontDefinition = StyleKeyword.Null; TypeOf(probe, style); // measured in the face the label draws (brief 046: the type token; drawn as cut)
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
                case "left": return w + f.Icons.Count * (20 + 3 + 16 + 8);
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
                Text(sheetNote, pn.T.Kind == "crowd" && pn.T.Security ? host.OneWord(pn.T) : pn.T.Kind == "crowd" ? (host.Text != null && host.Text.Pops.TryGetValue("passer-by", out var pb) ? pb : "Passer-by") : (host.Text != null && host.Text.Pops.TryGetValue("nothing-here", out var nh) ? nh : "Nothing here")); // Pulse 067 W2: a shift guard's sheet says "Guard"
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
                    Show(u.TagIcon, anyTag); u.TagIcon.style.visibility = tag.Length > 0 ? StyleKeyword.Null : Visibility.Hidden; // the cabinet's loops: which camera, as a glyph (Glyph: no word)
                    if (tag.Length > 0) Art(u.TagIcon, tag == "entrance" ? "icons/singles/enter" : tag == "pickup" ? "icons/singles/car" : "badges/signal_camera", Ink);
                    u.FieldsKey = ""; Fields(u.Fields, lines[i], Ink, ref u.FieldsKey, false, cols: sheetCols);
                    bool key = KeyLegends && r.Key >= 0 && host.Selected[(int)r.By]; Show(u.Key, KeyLegends); u.Key.style.visibility = key ? StyleKeyword.Null : Visibility.Hidden; Text(u.KeyText, key ? host.VerbKey(r.Key) : "0"); // the slot is kept, so every row's columns align
                    bool goBlocked = !r.Available && it.Order?.Do == "go"; u.Root.style.opacity = r.Available || goBlocked ? 1f : 0.45f; u.Glyph.style.opacity = u.Name.style.opacity = goBlocked ? 0.45f : 1f; // Glyph r3: a blocked Go dims only its lock and name; the cameos and red cuffs stay full
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
            sheet.style.left = left; sheet.style.top = top; sheetTL = new Vector2(left, top); sheetTLAt = Time.unscaledTime;
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
            var k = "#" + sb; if (k == key) return; key = k; into.Clear(); // "#": an empty list still differs from a reset key, so a pooled row never keeps the last card's fields
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
                    var tip = f.Tip; gl.RegisterCallback<PointerEnterEvent>(_ => EyeTipShow(gl, tip)); gl.RegisterCallback<PointerLeaveEvent>(_ => EyeTipShow(null, null));
                    if (f.Text.Length > 0) { var en = L(part, chip ? "chip-value" : "card-price"); en.text = f.Text; en.style.color = ink; en.style.marginLeft = 2; if (!chip) { en.style.fontSize = small ? 11.5f : 13f; en.style.unityFontStyleAndWeight = FontStyle.Bold; } } // the move card: how many would keep the runner's face (Fable: the number after the eye)
                    continue;
                }
                if (f.Kind == "mark") { Bg(E(part, null, w: ic + 2, h: ic + 2), $"badges/mark_{f.Mark}"); continue; }
                if (f.Kind == "left") // brief 018 (Glyph r1): a 20 px cameo, then its fate glyph at 14 px (lock captured, footprints walks out)
                {
                    foreach (var li in f.Icons)
                    {
                        var who = li.Split(':')[0]; bool cap = li.EndsWith(":captured");
                        var cm = E(part, null, w: 20, h: 20); Round(cm, 10); Bg(cm, $"portraits/cameo_{who}_normal", Color.white); Border(cm, 1.3f, cap ? Red : ink); cm.style.flexShrink = 0;
                        var fg = cap ? Cuffs(part, Red) : E(part, null, w: 14, h: 14); if (!cap) Icon(fg, "s_moving", ink); fg.style.marginLeft = 3; fg.style.marginRight = 8; fg.style.flexShrink = 0; // Glyph r3: the cuffs at full red
                    }
                    continue;
                }
                if (f.Kind == "learns") { foreach (var li in f.Icons) { var d = E(part, "s-disc_dark_24", w: ic + 4, h: ic + 4); d.style.marginRight = 2; Icon(E(d, null, 2, 2, ic, ic), li, Cream); } continue; }
                if (f.Icon.Length > 0) { var e = E(part, null, w: ic, h: ic); Icon(e, f.Icon, ink); e.style.marginRight = 3; }
                var l = L(part, chip ? "chip-value" : "card-price"); l.text = f.Text; l.style.color = ink; if (!chip) { l.style.fontSize = small ? 11.5f : 13f; l.style.unityFontStyleAndWeight = FontStyle.Bold; } // the chip's Jost is the type token's, drawn as cut (brief 046)
            }
        }

        void Chip(bool alt, float W)
        {
            var it = host.HoverIntent; var hov = host.Hover; bool ended = m.Ended;
            string lead = null; List<HudModel.PField> fs = null; string refused = "";
            if (!ended && host.Panel == null && host.Loud == null && host.Ring == null)
            {
                if (host.AimNow is HandoverHost.AimParts a && host.AimLine != null)
                {
                    lead = a.Badge; fs = new List<HudModel.PField>();
                    if (a.Refused.Length > 0) refused = a.Refused;
                    else
                    {
                        if (a.Lab && a.WalkS >= 0) fs.Add(new HudModel.PField { Kind = "walk", Icon = "s_moving", Text = $"{a.WalkS} s" }); // Keel's walk-in: the walk first, the pips from the stop point
                        if (a.Lab) fs.Add(new HudModel.PField { Kind = "time", Icon = "", Text = a.Down }); // brief 027: the lab chip's pips (no seconds); brief 101: the crosshair shows no seconds to down and no chance
                        if (a.Lab && a.Charge > 0) fs.Add(new HudModel.PField { Kind = "charge", Icon = "", Text = $"▮{a.Charge}" });
                        if (a.Trail.Length > 0 && (!a.Lab || alt || !host.Fights.Hud.trailAltOnly)) fs.Add(new HudModel.PField { Kind = "trail", Icon = "p_trail", Text = a.Trail });
                        if (a.Civilian) fs.Add(new HudModel.PField { Kind = "mark", Mark = "no" });
                    }
                }
                else if (hov == null && alt && host.MoveNow is HandoverHost.MoveParts mv) // the ring answers at rest; Alt adds who would see it beside it
                {   // brief 101 (Sky 2026-11-08): no walk, run or carry seconds; a walk with nothing else to say draws no chip (its ring says where)
                    fs = new List<HudModel.PField>();
                    if (mv.Refused.Length > 0) { lead = "icons/singles/s_moving"; refused = mv.Refused; }
                    else if (mv.CarryS >= 0) { lead = "icons/singles/s_carrying"; if (mv.Eye.Length > 0) fs.Add(new HudModel.PField { Kind = "eye", Icon = "eye_" + mv.Eye, Eye = mv.Eye }); }
                    else
                    {
                        lead = "icons/singles/s_moving";
                        if (mv.RunS >= 0) { fs.Add(new HudModel.PField { Kind = "run", Icon = "run", Text = "" }); if (mv.Eye.Length > 0) fs.Add(new HudModel.PField { Kind = "eye", Icon = "eye_" + mv.Eye, Eye = mv.Eye, Text = mv.Remembers > 0 ? mv.Remembers.ToString() : "", Tip = mv.Remembers > 0 ? HudModel.RememberTip(mv.Remembers) : "" }); /* engine 0.31.0: the passers-by who keep the face, their number after the eye */ if (mv.Alert.Length > 0) fs.Add(new HudModel.PField { Kind = "alert", Icon = "p_alert", Text = mv.Alert }); }
                    }
                    if (fs.Count == 0 && refused.Length == 0) fs = null;
                }
                else if (it != null && hov != null && it.Actionable && host.Driving && it.Opens && host.HoverAct.glyph.Length == 0)
                {   // brief 024 rule 2: third person, the likely way is harmful (never lit): the prompt shows only that E opens the options
                    lead = null; fs = new List<HudModel.PField> { new HudModel.PField { Kind = "more", Icon = "", Text = "⋯" } };
                }
                else if (it != null && hov != null && it.Actionable)
                {
                    lead = "icons/singles/" + (it.Lead.Length > 0 ? it.Lead : HudModel.AtlasIcon(it.Order.Do, it.Order.Approach, ""));
                    fs = HudModel.PriceLine(host, it);
                    if (it.More) fs.Add(new HudModel.PField { Kind = "more", Icon = "", Text = "⋯" }); // brief 030: other ways are one hold away
                }
            }
            if (fs != null && host.controls.Modern && host.AimNow == null && (hov != null || host.SelTarget != null) && host.MoveNow == null) fs = null; // brief 041: the verb pill carries the price
            bool on = fs != null; Show(chip2, on); if (!on) return;
            Art(chipLead, lead, Cream); Text(chipRefused, refused); Show(chipRefused, refused.Length > 0);
            Fields(chipFields, fs, Cream, ref chipKey, true);
            var p = RuntimePanelUtils.ScreenToPanel(root.panel, m.Mouse); float cw = chip2.resolvedStyle.width; if (float.IsNaN(cw)) cw = 220;
            if (hov == null && host.MoveNow != null && ringOn) p = ringAt + new Vector2(ringRx + 6, 18); // beside the ring, tail to it
            chip2.style.left = p.x + 16 + cw > W - 14 ? p.x - cw - 16 : p.x + 16; chip2.style.top = p.y - 46;
            if (host.AimNow is { Lab: true } && host.LabFireTarget.Length > 0 && host.DrawnAt(host.LabFireTarget) is Vector3 tw && Proj(tw, out var tq))
            { chip2.style.left = tq.x + 22 + cw > W - 14 ? tq.x - cw - 22 : tq.x + 22; chip2.style.top = tq.y + 6; } // Glyph frame B: down-right of the target, off the heads
        }

        // ---------------------------------------------------------------- the street

        /// <summary>Dead camera discs drawn since start (the proof reads it).</summary>
        public static int DeadDiscsShown { get; private set; }

        // brief 083: the act mark over a head and the selected chevron
        const float ActMarkBox = 36f; // the sprite's 12.5 % margin leaves a 27 px disc; the ring just outside it
        readonly List<Action<Painter2D>> actPaints = new(); void ActPaint(Action<Painter2D> f) => actPaints.Add(f);
        readonly Rect[] actPlaced = new Rect[4];
        /// <summary>The proof reads them: the act marks drawn this frame ("scout:watch: "), and the selected chevrons.</summary>
        public static string ActMarksShown { get; private set; } = ""; public static int SelectedCarets { get; private set; }
        /// <summary>The act marks' own layer: the follow view hides every other mark over the street (FollowMarks) and keeps this one. The proof reads how many discs a frame draws, in that layer's visibility.</summary>
        VisualElement actMarkRoot; readonly List<VisualElement> actDiscs = new(); public static int ActMarkDiscsSeen { get; private set; }
        void ActMarkRootEnsure() { if (actMarkRoot != null) return; actMarkRoot = E(marks, null, 0, 0, 0, 0); actMarkRoot.style.overflow = Overflow.Visible; actMarkRoot.pickingMode = PickingMode.Ignore; }
        VisualElement ActDiscEl083(int n)
        {
            ActMarkRootEnsure();
            while (actDiscs.Count <= n) { var d = E(actMarkRoot, "s-disc_dark_24", 0, 0, ActMarkBox, ActMarkBox); d.pickingMode = PickingMode.Ignore; var g = E(d, null, ActMarkBox * 0.2f, ActMarkBox * 0.2f, ActMarkBox * 0.6f, ActMarkBox * 0.6f); g.name = "g"; g.pickingMode = PickingMode.Ignore; actDiscs.Add(d); manifest.Add((d, "image", () => "act mark")); }
            Show(actDiscs[n], true); return actDiscs[n];
        }

        VisualElement WorldEl(string cls, float w, float h)
        {
            while (worldEls.Count <= worldUsed) { var e = E(marks, null, 0, 0, w, h); var g = E(e, null, 0, 0, w, h); g.name = "g"; worldEls.Add(e); manifest.Add((e, "image", () => "street mark")); } // Glyph G2 (0.2.135): every street mark in the manifest
            var el = worldEls[worldUsed++]; Show(el, true); el.ClearClassList(); if (cls != null) foreach (var c in cls.Split(' ')) el.AddToClassList(c);
            el.style.width = w; el.style.height = h; el.style.opacity = 1; el.style.unityBackgroundImageTintColor = Color.white; el.style.backgroundImage = StyleKeyword.Null; el.style.scale = StyleKeyword.Null;
            if (el.Q<Label>("num") is Label nl0) Show(nl0, false);
            var gl = el.Q("g"); gl.style.left = w * 0.2f; gl.style.top = h * 0.2f; gl.style.width = w * 0.6f; gl.style.height = h * 0.6f; Show(gl, true); gl.style.backgroundImage = StyleKeyword.Null;
            Border(el, 0, Color.clear); Round(el, 0); el.style.backgroundColor = Color.clear; // a pooled element: no fill left from an eye marker's disc
            return el;
        }

        /// <summary>Painted every frame by `world` (positions in panel space): ground rings, activity rings, the clock pip's wedge, wake rings,
        /// the dashed eye rims, reports' dotted lines, and the box being dragged.</summary>
        readonly List<Action<Painter2D>> paints = new();

        // ---------------------------------------------------------------- brief 018 §6.5 (Glyph r1): the Go commit card. One row, about 160 x 44: the hold ring
        // with the car glyph inside, then each one left (a 20 px cameo and its 14 px fate glyph), and "+1 T" when police would see the car leave;
        // at the Go card's top-left (the sheet it was given from), else beside the click.
        VisualElement goCard, goRing, goPairs; readonly List<VisualElement> goDots = new(); Vector2 sheetTL; float sheetTLAt = -10f; string goKey = "";
        void GoCommit(float W, float H)
        {
            var gate = host.Loud; bool on = gate?.Left != null && !m.Ended;
            if (goCard == null)
            {
                goCard = E(root, "s-plate_dark", 0, 0, float.NaN, 52, pick: true); goCard.style.flexDirection = FlexDirection.Row; goCard.style.alignItems = Align.Center; goCard.style.paddingLeft = 14; goCard.style.paddingRight = 18; /* Glyph r2: the plate art has no corner screws; content 14 px in from each end */
                goCard.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.GateHoldByCard(); e.StopPropagation(); } });
                goRing = E(goCard, null, w: 40, h: 40); goRing.style.flexShrink = 0;
                for (int k = 0; k < 24; k++) { float a = Mathf.PI / 2 - k * Mathf.PI * 2 / 24; var dt = E(goRing, null, 20 + 16 * Mathf.Cos(a) - 2, 20 - 16 * Mathf.Sin(a) - 2, 4, 4); Round(dt, 2); goDots.Add(dt); }
                Icon(E(goRing, null, 12, 12, 16, 16), "car", Cream);
                goPairs = E(goCard); goPairs.style.flexDirection = FlexDirection.Row; goPairs.style.alignItems = Align.Center; goPairs.style.marginLeft = 8;
                manifest.Add((goCard, "image", () => "go commit card")); Show(goCard, false);
            }
            Show(goCard, on); if (!on) { goKey = ""; return; }
            Show(loud, false);
            var key = string.Join(",", gate.Left.ConvertAll(x => x.op + x.fate)) + gate.SeenLeaving;
            if (key != goKey)
            {
                goKey = key; goPairs.Clear(); int n = 0;
                foreach (var (who, fate, _) in gate.Left)
                {
                    if (n++ >= 3) break; bool cap = fate == "captured";
                    var cm = E(goPairs, null, w: 20, h: 20); Round(cm, 10); Bg(cm, $"portraits/cameo_{who}_normal", Color.white); Border(cm, 1.3f, cap ? Red : Cream); cm.style.flexShrink = 0;
                    var fg = cap ? Cuffs(goPairs, Red) : E(goPairs, null, w: 14, h: 14); if (!cap) Icon(fg, "s_moving", Cream); fg.style.marginLeft = 3; fg.style.marginRight = 8; fg.style.flexShrink = 0;
                }
                if (gate.SeenLeaving) { var tr = E(goPairs, null, w: 14, h: 14); Icon(tr, "p_trail", Cream); var tl = L(goPairs, "chip-value"); tl.text = "+1"; tl.style.marginLeft = 4; tl.style.flexShrink = 0; }
            }
            int lit = Mathf.RoundToInt(host.GateProgress * 24); for (int k = 0; k < goDots.Count; k++) goDots[k].style.backgroundColor = k < lit ? TurqLight : new Color(1, 1, 1, 0.15f);
            Vector2 at = Time.unscaledTime - sheetTLAt < 2f ? sheetTL : RuntimePanelUtils.ScreenToPanel(root.panel, m.LoudAt) + new Vector2(24, -60);
            float cw = float.IsNaN(goCard.resolvedStyle.width) ? 180 : goCard.resolvedStyle.width;
            goCard.style.left = Mathf.Clamp(at.x, 14, W - cw - 14); goCard.style.top = Mathf.Clamp(at.y, 14, H - 66);
        }

        void DrawWorld(Painter2D p, Rect r)
        {
            foreach (var f in actPaints) f(p); // brief 083: the act and selection marks, in both views
            if (host.Following) return; // the follow view's rings (its agent's, the hover target's) are on the ground now (brief 046, HudToolkit.Ground); the rest are sized for the orthographic camera
            foreach (var f in paints) f(p);
        }

        void StreetLess(bool alt, Camera cam)
        {
            paints.Clear(); actPaints.Clear(); eyesAt.Clear(); var g = host.Game; if (g == null || !cam) return;
            float px = 14f / cam.orthographicSize; float now = Time.unscaledTime;
            // the hover ring and the hint's ring at the feet; pulses where an order lands
            void Ground(Vector3 w, float d, Color c, float width, bool dashed = false) { if (!host.Following) GroundRing(w, d * px / 2f * GroundPxM, c, width, dashed); } // the follow view keeps its own two rings only // brief 046: on the ground, depth-tested (HudToolkit.Ground)
            bool carMarked = host.Hover?.Kind == "car" && host.Hover.Key == "getaway" && (carMark?.resolvedStyle.display == DisplayStyle.Flex || heldMark?.resolvedStyle.display == DisplayStyle.Flex);
            if (m.HoverRing.HasValue && !carMarked && (!host.controls.Modern || host.Hover?.Kind is "person" or "patrol" or "security" or "unit" or "rivals" or "hostile" or "crowd" && !(host.HoverIntent is { Actionable: true } or { Locked: true }) && !host.TargetRings.Exists(r => r.Hover))) Ground(m.HoverRing.Value, 64, Cream, 2.2f); // brief 041: the pill alone; brief 085: a person with no pill to show (the Tech on the driver), the ring, so nobody under the pointer is without a cue (Fable A6, A7) // Glyph fight r1 B: the car's marker is its one mark
            // the Next Step's ring: only while its line shows, fading with it, in its noun's colour, with the verb's glyph at the centre ("do this here")
            if (host.HintNow?.Ring is Vector3 hr && !m.Ended && hintAlpha > 0f)
            {
                var rc = hintNounColour; rc.a = hintAlpha; Ground(hr, 90 * (1f + 0.12f * Mathf.Sin(now * 4f)), rc, 2.6f, true);
                if (host.HintNow.Cause != "door" && Proj(hr, out var hq)) { var he = WorldEl("e-body e-inline", 26, 26); Round(he, 13f); BorderW(he, 2f); RingColour(he, null); /* brief 101 round 3: a round tab; H13's door cause: the door marker stands there, the ring alone */ he.style.left = hq.x - 13; he.style.top = hq.y - 13; he.style.opacity = hintAlpha; var hg = he.Q("g"); hg.style.left = hg.style.top = 6.24f; hg.style.width = hg.style.height = 13.5f; Icon(hg, hintVerbGlyph, rc); }
            }
            foreach (var (w, kind, born) in m.Pulses) { float a = (now - born) / 0.7f; if (a < 0 || a > 1) continue; { bool refused = kind == HandoverHost.Feedback.Refused; var pc = refused ? Cream : TurqLight; pc.a = 1 - a; Ground(w, 50 + 90 * a, pc, 2f, refused); } } // sourced colours (the old coral and mint were the lab colours): refused = a dashed cream pulse, taken = turquoise-light (Glyph 0.2.79: the faint pink circle)
            if (m.Box is Rect box) { var bp = RuntimePanelUtils.ScreenToPanel(root.panel, box.min); var bq = RuntimePanelUtils.ScreenToPanel(root.panel, box.max); paints.Add(pp => { pp.strokeColor = Cream; pp.lineWidth = 1.5f; pp.BeginPath(); pp.MoveTo(bp); pp.LineTo(new Vector2(bq.x, bp.y)); pp.LineTo(bq); pp.LineTo(new Vector2(bp.x, bq.y)); pp.ClosePath(); pp.Stroke(); }); }
            // brief 083 (Sky on 0.2.159: "very unclear when a player is performing an action - need an icon above their head"; Glyph owns the look): over an agent at an act, in both views, the act's
            // icon on a kit disc the cameo's size (art bible: kind reads by its icon, as large as the face) and a ring that fills (brief 101: no seconds plate). Walking to an act: that act's icon, the ring dashed.
            // A plain walk has no mark: its destination ring and the portrait say it (four walking agents read as furniture otherwise).
            ActMarksShown = ""; Rect[] placed = actPlaced; int nPlaced = 0, nDisc = 0; bool tpv = host.Following;
            for (int i = 0; i < 4; i++)
            {
                var rg = m.Rings[i]; if (!rg.Show) continue;
                var dg = host.DoingOf(i); bool walkingTo = dg.Phase is "walking" or "running" && dg.QueuedGlyph.Length > 0, plainWalk = dg.Phase is "walking" or "running" && !walkingTo;
                if (plainWalk && !rg.Done || !Proj(rg.World + Vector3.up * (tpv ? 2.35f : 2.6f), out var q)) continue; // review 12: 2.6 m (at 2.35 a running pip sat on a neighbour's elbow in a formation)
                { var pr = PillRect(); if (pr.width > 0 && q.x + 21 > pr.xMin && q.x - 21 < pr.xMax && q.y + 21 > pr.yMin && q.y - 21 < pr.yMax + 10) q = new Vector2(q.x, pr.yMin - 25f); }
                foreach (var role in Cast) // Glyph's 0.2.104 review #7: over a cast person's head the stack is theirs (pip, mood): an agent's act disc beside one goes to the agent's feet
                    if (!tpv && KnownOf(role) && host.Views?.Find(role) is Transform ct && ct.gameObject.activeSelf && Proj(ct.position + Vector3.up * 2.3f, out var ch) && (ch - q).sqrMagnitude < 44f * 44f && Proj(rg.World + Vector3.up * 0.2f, out var feet)) { q = feet + new Vector2(0f, 20f); break; } // review 12: above the pill (beside it, it sat on the figure's body line over an elbow) // brief 043: the agent's ring never sits over the pill's words (the playground shot: the tick over "Hold")
                for (int pi = 0; pi < nPlaced; pi++) { var o = placed[pi]; if (Mathf.Abs(q.y - o.center.y) < 40f && q.x + 21f > o.xMin && q.x - 21f < o.xMax) q.x = o.xMax + 24f; } // two agents side by side: the later steps right, never over the first's disc
                var e = ActDiscEl083(nDisc++); e.style.left = q.x - ActMarkBox / 2f; e.style.top = q.y - ActMarkBox / 2f;
                string tone = host.DoneTickAge(i) < HandoverHost.DoneTickS + 0.3f ? host.DoneTone(i) : "";
                float rgR = ActMarkBox / 2f - 1f;
                if (rg.Done || tone is "soft" or "bad") { Icon(e.Q("g"), tone == "soft" ? "asked" : tone == "bad" ? "refused" : "check", TurqLight); var dq = q; ActPaint(pp => RingArc(pp, dq, rgR, 1f, TurqLight, 3f)); placed[nPlaced++] = new Rect(q.x - 21f, q.y - 21f, 42f, 42f); continue; } // techwatch fix 3: the act ran out: the ring stays a beat, full, with a tick
                var opB = host.Game.Ops[i]; string iconDo = opB.Busy?.Do ?? (walkingTo ? host.PendingOf(i).verb : dg.Verb), iconAp = opB.Busy?.Approach ?? (walkingTo ? host.PendingOf(i).approach : "");
                Icon(e.Q("g"), HudModel.AtlasIcon(iconDo, iconAp, walkingTo ? "" : dg.Phase), Cream);
                float frac = rg.Progress < 0 ? 1f : rg.Progress / 1000f; bool open = rg.Progress < 0 || walkingTo; var aq = q;
                ActPaint(pp => { if (open) Dashed(pp, aq, rgR, new Color(1, 1, 1, 0.6f), 3f, 10); else { RingArc(pp, aq, rgR, 1f, new Color(1, 1, 1, 0.22f), 3f); RingArc(pp, aq, rgR, frac, TurqLight, 3f); } });
                placed[nPlaced++] = Rect.MinMaxRect(q.x - 21f, q.y - 21f, q.x + 21f, q.y + 21f);
                ActMarksShown += $"{Op.Name((OpId)i)}:{iconDo}: ";
                if (rg.ActAt.HasValue && !tpv && Proj(rg.ActAt.Value + Vector3.up * 0.3f, out var atq)) { var ae = WorldEl("s-disc_light_24", 20, 20); ae.style.left = atq.x - 10; ae.style.top = atq.y - 10; var (qd, qa) = host.PendingOf(i); Icon(ae.Q("g"), HudModel.AtlasIcon(qd, qa, ""), Ink); }
            }
            for (int i = nDisc; i < actDiscs.Count; i++) Show(actDiscs[i], false);
            ActMarkDiscsSeen = 0; if (actMarkRoot != null && actMarkRoot.resolvedStyle.visibility == Visibility.Visible) for (int i = 0; i < nDisc; i++) if (actDiscs[i].resolvedStyle.display != DisplayStyle.None) ActMarkDiscsSeen++;
            SelectionMarks(); // brief 083 note 5: the selected chevrons; brief 101: the eye over the driven agent (HudToolkit.TakeControl)
            // Glyph techwatch r2 C: a camera being watched (the Scout's or the Tech's Watch under way) carries a small apex mark where its cone will
            // start: a red diamond with a cream rim, unlit like the cone
            for (int k = 0; k < 4; k++)
            {
                var bz = g.Ops[k].Busy; if (bz == null || bz.Do != "watch" || Game.WatcherKind(bz.Target) != "camera") continue;
                Vector3? apex = null; var fan = g.ConeFan(bz.Target); if (fan != null) apex = host.frame.World(fan.At);
                if (apex == null && host.Views?.Find("camera/" + bz.Target.Substring(bz.Target.IndexOf(':') + 1)) is Transform ct) apex = ct.position;
                if (apex is not Vector3 aw || !Proj(aw + Vector3.up * 0.1f, out var aq)) continue;
                var cc = host.coneColour; paints.Add(pp => { pp.fillColor = cc; pp.strokeColor = Cream; pp.lineWidth = 1.5f; pp.BeginPath(); pp.MoveTo(aq + new Vector2(0, -6)); pp.LineTo(aq + new Vector2(6, 0)); pp.LineTo(aq + new Vector2(0, 6)); pp.LineTo(aq + new Vector2(-6, 0)); pp.ClosePath(); pp.Fill(); pp.Stroke(); });
            }
            StepMarks(px, now);
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
                var pe = WorldEl(null, 18, 18); pe.style.left = rect.x; pe.style.top = rect.y; Round(pe, 9); pe.style.backgroundColor = Cream; Border(pe, sel ? 1.6f : 0.8f, sel ? TurqLight : Ink);
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
            if (!g.Lab) foreach (var f in m.Fighters) // brief 027: with the lab the segment bars over the heads replace these pips (Glyph frame B)
            {
                if (!Proj(f.World + Vector3.up * 2.1f, out var q)) continue; int hp = f.Hp, mx = f.Max; bool cell = f.Cell;
                if (host.OneInput && f.Id != null && reticles.TryGetValue(f.Id, out var rf) && rf.Gone < 0f && host.DrawnAt(f.Id) is Vector3 rw && Proj(rw + Vector3.up * (f.Id.StartsWith("car:") ? 1.9f : ReticleUp), out var rq) && InView(rq)) q = new Vector2(rq.x, rq.y + PipsUnderDisc); // Glyph G3 (0.2.135): the focus's stack, top to bottom: lock disc, pips, wait plate, 4 px apart
                var sig = f.Id != null && f.Id.StartsWith("rival") ? RingViolet : RingAmber; paints.Add(pp => { for (int k = 0; k < mx; k++) { var c = new Vector2(q.x + (k - (mx - 1) / 2f) * (cell ? 8 : 10), q.y); if (!cell) { pp.fillColor = BkInk; pp.BeginPath(); pp.Arc(c, 4.9f, Deg(0), Deg(360)); pp.Fill(); } pp.fillColor = k < hp ? (cell ? Cream : sig) : cell ? new Color(1, 1, 1, 0.25f) : StPage; pp.BeginPath(); pp.Arc(c, cell ? 2.6f : 3.5f, Deg(0), Deg(360)); pp.Fill(); } /* brief 101 step 6 (Glyph's S mock): a hostile's pips 7 px in his signal colour, the book's page cream once lost, on a 1.4 px ink keyline, 10 px apart */ });
            }
            foreach (var dn in m.Down) { if (!Proj(dn.World + Vector3.up * 1.0f, out var q)) continue; var e = WorldEl("s-disc_dark_24", 22, 22); e.style.left = q.x - 11; e.style.top = q.y - 11; Icon(e.Q("g"), "s_downed", Cream); }
            // Glyph 058 §2 (board 02): a dead camera keeps a disc to the end, 24 px, centred 20 px above its projected head (the pole top), clear of
            // the window marker: cut (Disable) = disc_dark with the disable icon in cream; shot out = disc_light with fire in ink (harm's own pair)
            if (!g.Range && host.Views != null)
                foreach (var cm in g.S.Cameras)
                {
                    string dead = ""; try { dead = g.CameraDeadAs(cm.Id); } catch { } if (dead.Length == 0) continue;
                    var ct = host.Views.Find("camera/" + cm.Id); if (ct == null || !ct.gameObject.activeInHierarchy || !Proj(ct.position + Vector3.up * (host.Views.CamHeadUp - 1.6f), out var hq)) continue;
                    bool cut = dead == "cut"; var de = WorldEl(cut ? "s-disc_dark_24" : null, 24, 24); de.style.left = hq.x - 12; de.style.top = hq.y - 20 - 12;
                    if (!cut) Bg(de, "markers/disc_light_24", Color.white);
                    var dg = de.Q("g"); dg.style.left = 4.5f; dg.style.top = 4.5f; dg.style.width = 15; dg.style.height = 15; Icon(dg, cut ? "disable" : "fire", cut ? Cream : Ink);
                    DeadDiscsShown++;
                }
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
            // turned contacts: the ✓ rides on their pip's corner (Pips; Glyph 022: a loose tick beside the cup belonged to nobody)
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
            for (int i = 0; i < stashMarks.Count && i < m.Stashes.Count; i++) Show(stashMarks[i].Name.Root, alt || m.Stashes[i].Hover); // brief 101 round 3: the street label's name (its pictogram and pips stay)
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
            ringOn = true; ringAt = q; float rx = 32f * px; ringRx = rx;
            Color Kind(string k) => k == "camera" ? Red : k == "rivals" ? Violet : k == "guard" ? Amber : TurqLight;
            Color ink = new(0.071f, 0.188f, 0.18f), cream = new(0.969f, 0.957f, 0.925f);
            string state = mv.State; Color rim = state == "seen" ? Kind(mv.SeenKind) : state == "unk" ? cream : state == "alert" ? ink : TurqLight;
            float frac = Mathf.Clamp01((mv.CarryS >= 0 ? mv.CarryS : mv.WalkS) / 60f);
            // the glow first (under the ring), after a 0.25 s rest: the seen stretch only, in the watcher's colour, a solid core on an ink
            // under-stroke (it must hold over the watcher's own translucent cone) and a soft halo that breathes once a second
            if (now - mouseRestAt >= 0.25f)
                foreach (var (a, b, kind) in mv.Glow)
                {   // on the ground too (brief 046), under the ring
                    var gk = Kind(kind); float al = 0.45f + 0.2f * Mathf.Sin(now * Mathf.PI * 2f); var seg = new List<Vector3> { a, b };
                    GroundPath(seg, new Color(gk.r, gk.g, gk.b, al), 13f * px, false); GroundPath(seg, new Color(ink.r, ink.g, ink.b, 0.9f), 7f * px, false); GroundPath(seg, gk, 4f * px, false);
                }
            // brief 046 and Glyph's leftover: the ellipse lies on the ground (depth-tested: legs in front hide it), the same rings in metres
            {
                var gc = mv.At; float gm = GroundPxM, grx = rx * gm; Color.RGBToHSV(rim, out var th0, out var ts0, out _);
                Color track0 = state == "seen" ? Color.HSVToRGB(th0, ts0, 0.42f) : Color.Lerp(ink, rim, 0.32f); // opaque; seen: the watcher's colour darkened in place
                GroundDisc(gc, grx - 3.5f * gm, new Color(ink.r, ink.g, ink.b, 0.95f));
                GroundRing(gc, grx, ink, 8f, false);
                if (state == "alert") GroundRing(gc, grx + 5.5f * gm, cream, 2.4f, false);
                if (state == "unk") for (int i = 0; i < 16; i++) GroundArc(gc, grx, Color.Lerp(ink, rim, 0.55f), 4.6f, i * 22.5f, i * 22.5f + 11f);
                else GroundArc(gc, grx, state == "alert" ? rim : track0, 4.6f, 0f, 360f);
                if (frac > 0 && state != "alert") GroundArc(gc, grx, rim, 4.6f, 0f, 360f * frac);
                else if (frac > 0) GroundArc(gc, grx, cream, 2f, 0f, 360f * frac);
            }
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
            bool fight = host.OneInput && g.FightOn; // Glyph N4 (0.2.137): on Alt too, no "?" over the guard, the patrol or the rivals while the fight is on
            if (fight) yield break; // the cameras above keep theirs
            if (g.Known("guard") is not { Sight: true } && g.GuardPose() is var gp && gp.HasValue && !g.GuardP.KRoutine) yield return (host.frame.World(gp.Value.pos) + Vector3.up * 2.9f, Amber);
            if (g.Known("patrol") is not { Sight: true } && g.PatrolPose() is var pp && pp.HasValue) yield return (host.frame.World(pp.Value.pos) + Vector3.up * 2.9f, Amber);
            if (g.RivalsPresent && !g.RivalsWatched && g.RivalCarPose() is var rp && rp.HasValue) yield return (host.frame.World(rp.Value.pos) + Vector3.up * 2.2f, Violet); // brief 059 §2.2: the Watch is on the car (RivalsWatched)
        }

        // ---------------------------------------------------------------- key-person pips (Sky, 2026-10-16: "no idea who the waitress is")

        static readonly string[] Cast = { "waitress", "guard", "driver", "councillor" };
        /// <summary>Brief 062 "Portraits and ids" (Glyph 2026-10-25): a cast person's face at card size (Intel, the feed, the ticker): the painted
        /// cameo with its baked ring, portraits/cameo_&lt;role&gt;_normal, for the waitress, the driver and the councillor; the guard keeps his pictogram
        /// (he has no face file). CastBadge stays on the 11 to 14 px reads, where a face does not hold.</summary>
        public static string FaceOf(string role) => role is "waitress" or "driver" or "councillor" ? $"portraits/cameo_{role}_normal" : CastBadge(role);
        public static bool HasFace(string role) => role is "waitress" or "driver" or "councillor";

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

        // Sky 2026-10-26 ("Simplify the visuals, it's way too crowded. Just having the agent facing who they're shooting should be enough to not
        // need those lines everywhere"): the shots-in-flight lines (ShotMarks, Glyph fight r2) are gone; the muzzle flash and the hit spark stay
        // (HudToolkit.Fire ShotFx, both views) and the shooter faces its target (HandoverHost.AimTargets).

        VisualElement nameChip; Label nameText; readonly List<VisualElement> targetEls = new();
        /// <summary>The hovered thing's soft rim (a cream circle round its pick anchor) and a one-word name chip over it; in target mode, a
        /// marker over every valid target in view (the verb's glyph in a disc over a dashed ring, the hint ring's grammar; the Tab focus ringed
        /// solid), each a click that gives the picked verb there.</summary>
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
            else if (live && !host.controls.Modern && host.HoverAnchor is Vector3 ha && host.HoverName.Length > 0 && Proj(ha, out var hq)) // brief 041: no name tag
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
                if (!pip && !marked && !(host.AimNow is { Lab: true })) // brief 027 (Glyph frame B): the chip's side coin names a fighter; the bar over the head stays clear
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
                var icon = m.VerbIcon[Mathf.Clamp(host.Picked, 0, 4)]; var focus = host.Focused;
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

        bool knobOn;
        void DrawKnobRing(Painter2D p, Rect r)
        {
            if (!knobOn || knobs[0] == null || verbPlate == null) return;
            var kb = knobs[0].worldBound; var pb = verbPlate.worldBound; if (kb.width <= 0) return;
            var c = kb.center - pb.position; Dashed(p, c, kb.width / 2 - 3f, TurqLight, 1.6f, 12); // Glyph 016 r2 D: tight on the bezel, 2 px clear of the key number
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
            var left = host.Loud.Left;
            var key = $"{c.AlertTo}|{c.PoliceInS}|{c.RivalsInS}|{c.Seal}|{c.GuardAllyLost}|{c.WarningLost}|{c.DownedCaptured}|{c.TrailPerHit}|{host.Loud.Takedown}|{(left == null ? "-" : string.Join(",", left.ConvertAll(x => x.op + x.fate)))}|{host.Loud.SeenLeaving}|{c.Fails}|{host.Loud.What}";
            if (key == loudKey) return; loudKey = key; loudIcons.Clear(); loudLost.Clear();
            void Part(string art, string text, Color? tint = null) { var p = E(loudIcons); p.style.flexDirection = FlexDirection.Row; p.style.alignItems = Align.Center; p.style.marginRight = 14; p.style.marginBottom = 4; var e = E(p, null, w: 18, h: 18); Art(e, art, tint); var l = L(p, "chip-value"); l.text = text; l.style.marginLeft = 5; }
            if (left != null)
            {   // brief 018 §6.5: a Go that leaves someone: the car, then each one left (their cameo and a fate glyph: a lock captured, walking out), and +1 T if seen
                Part("icons/singles/car", "");
                foreach (var (who, fate, _) in left)
                {
                    var p = E(loudIcons); p.style.flexDirection = FlexDirection.Row; p.style.alignItems = Align.Center; p.style.marginRight = 12; p.style.marginBottom = 4;
                    var cm = E(p, null, w: 22, h: 22); Round(cm, 11); Bg(cm, $"portraits/cameo_{who}_normal", Color.white); Border(cm, 1.4f, fate == "captured" ? Red : Cream);
                    var fg = E(p, null, w: 14, h: 14); fg.style.marginLeft = 4; Icon(fg, fate == "captured" ? "restrain" : "s_moving", Cream);
                }
                if (host.Loud.SeenLeaving) Part("icons/singles/p_trail", "+1");
                return;
            }
            if (c.Fails.Length > 0) // engine 0.15.0 (tuning.md §9.3): a shot that fails the mission always takes the hold; the card says what fails, at rest (Fable's fail line)
            {
                var fl = host.Text != null && host.Text.LoudFails.TryGetValue(c.Fails, out var lf) ? lf : c.Fails; // Fable's goingLoud.fails (2026-10-21): the whole line is text, never the host's
                Part("icons/singles/refused", fl, Red);
            }
            if (!c.Loud && c.Fails.Length == 0 && host.Loud.What == "panic") { Part("icons/singles/run", ""); return; } // brief 038 §5: the panic hold in a fight: the run glyph and the hold ring
            Part("icons/singles/p_alert", c.AlertTo.ToString());
            if (c.PoliceInS >= 0) Part("badges/signal_police", $"{c.PoliceInS} s");
            if (c.RivalsInS >= 0) Part("badges/signal_rival", $"{c.RivalsInS} s");
            if (c.Seal >= 0) Part("icons/singles/lock", HandoverHost.MissionClock(c.Seal));
            if (!host.Loud.Takedown) Part("icons/singles/p_trail", host.Loud.Orders.Exists(o => o.Do == "fire" && HandoverHost.PropertyId(o.Target)) && !host.Loud.Orders.Exists(o => o.Do == "fire" && !HandoverHost.PropertyId(o.Target)) ? $"+{c.TrailPerShooting}" : $"+{c.TrailPerHit}/hit"); // brief 058: on a camera or car, the shooting once
            void Lost(string badge) { var e = E(loudLost, null, w: 26, h: 26); e.style.marginRight = 10; Bg(e, badge); P(e, 0, 0, 26, 26, (p, r) => { p.strokeColor = Red; p.lineWidth = 2.4f; p.BeginPath(); p.MoveTo(new Vector2(3, 23)); p.LineTo(new Vector2(23, 3)); p.Stroke(); }); }
            if (c.GuardAllyLost) Lost("badges/signal_guard");
            if (c.WarningLost) Lost("badges/npc_waitress");
            if (c.DownedCaptured) { var e = E(loudLost, "s-disc_dark_24", w: 26, h: 26); Icon(E(e, null, 5, 5, 16, 16), "restrain", Cream); P(e, 0, 0, 26, 26, (p, r) => { p.strokeColor = Red; p.lineWidth = 2.4f; p.BeginPath(); p.MoveTo(new Vector2(3, 23)); p.LineTo(new Vector2(23, 3)); p.Stroke(); }); }
        }

        void BriefLess(bool alt)
        {
            if (!m.BriefingOpen || briefObjective == null || BriefOnStreet) return;
            // at rest: the title, the goal line, three numbers, the four portraits and Begin; the sentences come back on Alt
            foreach (var e in new VisualElement[] { briefObjective.parent, briefTarget.parent, briefWin.parent.parent, briefWays[0].title.parent.parent }) Show(e, alt);
            if (briefFootLine != null) Show(briefFootLine, alt);
            Show(briefArms.parent, alt); Show(briefSub, alt); if (brief.childCount > 0) Show(brief[0], alt); // at rest: the title alone (the tab and the place and time are Alt)
            Text(briefGoal, (host.Text?.BriefGoal ?? "").Replace("[", "").Replace("]", "")); Show(briefGoal, !alt); Show(briefPeople, !alt);
            Text(briefClose, host.Text?.Close ?? ""); Text(briefPurse, m.Cash); Text(briefAlert, $"{m.Alert}/3");
            if (briefHeat != null && host.Game is { } hg) // brief 031: the Heat band's word (Fable's), read once at the start; the eye's turns count the band
            {
                string band = ""; try { band = hg.HeatBand; } catch (Exception) { }
                Text(briefHeat, host.Text != null && host.Text.Heat.TryGetValue(band, out var hw) ? hw : band.Length > 0 ? char.ToUpperInvariant(band[0]) + band[1..] : "");
                int turns = band switch { "noticed" => 1, "wanted" => 2, "infamous" => 3, _ => 0 };
                if (turns != briefHeatTurns) { briefHeatTurns = turns; briefHeat.parent.Q("turns")?.MarkDirtyRepaint(); }
            }
            brief.style.width = alt ? 700 : 520; var bsz = root.panel.visualTree.layout.size; brief.style.left = ((float.IsNaN(bsz.x) ? 1920 : bsz.x) - (alt ? 700 : 520)) / 2;
        }

        void EndLess()
        {
            if (!m.Ended) return;
            var g = host.Game; var t = host.Text; string line = t == null ? (m.EndLines.Count > 0 ? m.EndLines[0] : "") : g.Result == Outcome.Success ? (host.ValeOut ? BookWordsFile.Get().EndWinLine : t.EndWin) : g.Result == Outcome.Withdrawn ? t.EndWithdraw : t.EndFail.TryGetValue(g.FailCode, out var f) ? f : (m.EndLines.Count > 0 ? m.EndLines[0] : "");
            Text(endTitle, line); Show(endLine, endDetailOpen);
            Text(endTally, HandoverHost.MissionClock(g.Tick)); Text(endCash, $"${g.S.Purse - g.Cash:N0}"); Text(endTrail, $"+{g.Trail}");
            var stl = host.StandingLines(); bool hasSt = stl.Count > 0; Show(endStand, hasSt); Show(endStandArrow, hasSt && stl[0].dir != 0);
            if (hasSt) { var st = stl[0]; endStandDir = st.dir; Text(endStand, $"{st.band} {(st.to > 0 ? "+" : st.to < 0 ? "\u2212" : "")}{Mathf.Abs(st.to)}"); endStandArrow.MarkDirtyRepaint(); }
            float ew = Mathf.Max(endDetailOpen ? 560 : EndCardW, EndButtonsW); end.style.width = ew; var esz = root.panel.visualTree.layout.size; end.style.left = ((float.IsNaN(esz.x) ? 1920 : esz.x) - ew) / 2;
        }
    
        // Redline 064 #6: the carry pace's "n m/s" taken once per pace string, not a Regex a row a frame
        string paceIn = "", paceOut = "";
        string PaceWords(string pace) { if (pace != paceIn) { paceIn = pace; paceOut = System.Text.RegularExpressions.Regex.Match(pace ?? "", @"[\d.]+ m/s").Value; } return paceOut; }
}
}
