using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Suborned.Play
{
    /// <summary>The HUD in Glyph's kit on UI Toolkit (step 2 of the plan Aster approved 2026-10-15): the status pill, the objective line, the roster
    /// column with its portraits, the slot strips, the verb plate, the ticker, the scene feed's bar and cards. It reads the same HudModel as the
    /// IMGUI view and calls the same host methods on a click; the pieces not yet built here (the action panel, the briefing, the chips, the
    /// marks in the world, the hint, the going-loud and end cards) stay IMGUI until steps 3 and 4. Everything is laid out in the kit's px at
    /// its 1920 x 1080 reference (HudPanel scales with the screen, matching height), from briefs/011/ui/kit/layout.json; sprites and type
    /// styles come from kit.uss, generated from the kit by Suborned/UI/Import Glyph's Kit. Without the imported kit it stays off and the
    /// IMGUI view draws everything. F11 switches views (PlayControls.view). Only the cards, the strips, the plates and the bar take clicks;
    /// the rest passes them to the world.</summary>
    [RequireComponent(typeof(HandoverHost))]
    public sealed partial class HudToolkit : MonoBehaviour
    {
        static HudToolkit live;
        /// <summary>The kit view is drawing (the IMGUI view then skips the pieces it has).</summary>
        public static bool Active => live != null && live.on;
        /// <summary>A screen point (origin bottom left) on a piece that takes clicks.</summary>
        public static bool Blocks(Vector2 screen)
        {
            if (CameraLab.Blocks(screen)) return true; // brief 025: the camera lab's panel takes its clicks
            if (!Active || live.root?.panel == null) return false;
            var p = RuntimePanelUtils.ScreenToPanel(live.root.panel, new Vector2(screen.x, Screen.height - screen.y));
            return live.root.panel.Pick(p) != null;
        }
        /// <summary>The place of the feed card under the mouse (the IMGUI view rings it in the street).</summary>
        public static Vector3? HoverPlace => Active && live.hoverCard != null ? live.host.PlaceNow(live.hoverCard) : null;

        HandoverHost host; PlayHud imgui; HudModel m; UIDocument doc; VisualElement root; bool on;
        readonly Dictionary<string, Sprite> sprites = new();

        void Start()
        {
            host = GetComponent<HandoverHost>(); imgui = GetComponent<PlayHud>();
            var ps = Resources.Load<PanelSettings>("Kit/HudPanel");
            if (ps == null) { Debug.Log("Play: the kit is not imported (Suborned/UI/Import Glyph's Kit); the IMGUI HUD draws"); enabled = false; return; }
            var go = new GameObject("hud-kit"); go.SetActive(false); go.transform.SetParent(transform, false);
            doc = go.AddComponent<UIDocument>(); doc.panelSettings = Instantiate(ps); go.SetActive(true); // a copy: the budget shots point it at a render texture (never the asset)
            doc.panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize; CrispScale(); // brief 014: an integer scale, so the @2x art and the type land on whole pixels // the panel exists once enabled with its settings
            root = doc.rootVisualElement; root.pickingMode = PickingMode.Ignore; root.style.flexGrow = 1;
            var uss = Resources.Load<StyleSheet>("Kit/kit"); if (uss != null) root.styleSheets.Add(uss);
            m = imgui != null ? imgui.Model : new HudModel();
            Build(); live = this;
        }

        void OnDestroy() { if (live == this) live = null; }

        /// <summary>The panel's scale: a whole number of screen pixels per kit pixel (1 at 1080 to 1619 px tall, 2 on a Retina MacBook Air's
        /// 2x backing, 3 above), never a fraction, so the @2x sprites draw at 0.5 or 1.0 of their texels and nothing is resampled soft.</summary>
        void CrispScale() { float k = Mathf.Max(1f, Mathf.Round(Screen.height / 1080f)); if (doc != null && doc.panelSettings.scale != k) doc.panelSettings.scale = k; }

        // ---------------------------------------------------------------- helpers

        Sprite Sp(string file) // "portraits/portrait_scout_normal" → the @2x sprite under Resources/Kit
        {
            if (sprites.TryGetValue(file, out var s)) return s;
            s = Resources.Load<Sprite>($"Kit/{file}@2x"); sprites[file] = s; return s;
        }

        static VisualElement E(VisualElement parent, string cls = null, float x = float.NaN, float y = float.NaN, float w = float.NaN, float h = float.NaN, bool pick = false)
        {
            var e = new VisualElement { pickingMode = pick ? PickingMode.Position : PickingMode.Ignore, focusable = false };
            if (cls != null) foreach (var c in cls.Split(' ')) e.AddToClassList(c);
            if (!float.IsNaN(x)) { e.style.position = Position.Absolute; e.style.left = x; }
            if (!float.IsNaN(y)) { e.style.position = Position.Absolute; e.style.top = y; }
            if (!float.IsNaN(w)) e.style.width = w; if (!float.IsNaN(h)) e.style.height = h;
            parent?.Add(e); return e;
        }

        static Label L(VisualElement parent, string style, float x = float.NaN, float y = float.NaN, float w = float.NaN, float h = float.NaN, bool ellipsis = false)
        {
            var l = new Label { pickingMode = PickingMode.Ignore, focusable = false }; l.AddToClassList("t-" + style);
            l.style.whiteSpace = WhiteSpace.NoWrap; l.style.flexShrink = 0; // Sky 2026-10-16: nothing in the HUD wraps or squeezes unless a reading surface says so
            if (!float.IsNaN(x)) { l.style.position = Position.Absolute; l.style.left = x; }
            if (!float.IsNaN(y)) { l.style.position = Position.Absolute; l.style.top = y; }
            if (!float.IsNaN(w)) l.style.width = w; if (!float.IsNaN(h)) l.style.height = h;
            if (ellipsis) { l.style.overflow = Overflow.Hidden; l.style.whiteSpace = WhiteSpace.NoWrap; l.style.textOverflow = TextOverflow.Ellipsis; }
            l.style.unityTextAlign = TextAnchor.MiddleLeft;
            parent?.Add(l); return l;
        }

        /// <summary>The build line (BuildStamp): one quiet line, the card's corner or under the pause menu's buttons; a reading surface, never in play.</summary>
        static Label BuildLine(VisualElement parent, bool corner)
        {
            var l = L(parent, "briefing-small"); l.text = BuildStamp.Line; l.style.fontSize = 10; l.style.color = new Color(0.69f, 0.8f, 0.78f, 0.55f);
            if (corner) { l.style.position = Position.Absolute; l.style.right = 40; l.style.bottom = 7; } // clear of the card's corner screw
            else l.style.marginTop = 18;
            return l;
        }

        static void Text(Label l, string s) { s ??= ""; if (l.text != s) l.text = s; }
        static void Show(VisualElement e, bool v) { var d = v ? DisplayStyle.Flex : DisplayStyle.None; if (e.style.display != d) e.style.display = d; }
        void Bg(VisualElement e, string file, Color? tint = null) { var s = Sp(file); if (s != null && e.style.backgroundImage.value.sprite != s) e.style.backgroundImage = new StyleBackground(s); if (tint.HasValue) e.style.unityBackgroundImageTintColor = tint.Value; }
        void Icon(VisualElement e, string atlas, Color tint) { if (string.IsNullOrEmpty(atlas)) { Show(e, false); return; } Show(e, true); Bg(e, "icons/singles/" + atlas, tint); }
        static void Class(VisualElement e, string c, bool v) { if (e.ClassListContains(c) != v) e.EnableInClassList(c, v); }

        static readonly Color Turq = new(0.11f, 0.725f, 0.776f);
        /// <summary>Hover, in style (Sky: no white outlines): the panel's tint lifts a little; at rest it sits slightly lower.</summary>
        static void Lift(VisualElement e, bool hover) { var c = hover ? Color.white : new Color(0.93f, 0.93f, 0.93f); if (e.style.unityBackgroundImageTintColor.value != c) e.style.unityBackgroundImageTintColor = c; }
        static readonly Color Cream = new(0.969f, 0.957f, 0.925f), Ink = new(0.09f, 0.188f, 0.184f), TurqLight = new(0.376f, 0.878f, 0.918f), Grey = new(0.478f, 0.549f, 0.541f), DeepTeal = new(0.122f, 0.424f, 0.404f);

        // ---------------------------------------------------------------- the pieces (rects from layout.json, px at 1920 x 1080)

        VisualElement pill, objective, column, ticker, feed, verbPlate, tooltip;
        Label clock, nextLabel, nextTime, cash, trail, state, objText, objTail, tickerText, barCount, barKeys, tipTitle, tipBody, tipKey;
        readonly VisualElement[] lamps = new VisualElement[3];
        VisualElement tickerChip, barChevron, barPill;
        // roster windows (roster_column.windows) and the strips
        static readonly Rect[] Win = { new(22, 34, 66, 65), new(22, 127, 66, 66), new(22, 222, 66, 66), new(22, 317, 65, 65) };
        const float ColX = 14, ColY = 14; // brief 014: one margin (14) from the screen edge for every corner group
        sealed class Strip { public VisualElement Root, Tags, Icon, Bar, Fill, Cancel, QueuedIcon, Stance; public Label Role, State, Action, Time, Queued, Ready, Hint; public bool Compact; }
        /// <summary>One agent's window in a roster frame (the column, or the row for the corner and bottom docks).</summary>
        const float EndCardW = 400f, EndCardPad = 30f; // the end card at rest (Debrief opens it to 560)
        sealed class Window { public VisualElement Portrait, RingSel, RingBusy, Pips, Arm, Car, Rim; public Label Num; public readonly List<VisualElement> PipDots = new(); }
        int rosterHover = -1; readonly VisualElement[] leftTips = new VisualElement[4];
        /// <summary>Brief 018 §6.6 (Glyph r1): the left-behind flag's line on hover (the whole portrait) or Alt: "Left behind", then Fable's fate line.</summary>
        void LeftTip(int i, Window w, HudModel.Slot s)
        {
            if (leftTips[i] == null)
            {
                var t = leftTips[i] = E(root, "s-plate_dark", 0, 0, float.NaN, float.NaN); t.style.paddingLeft = 10; t.style.paddingRight = 10; t.style.paddingTop = 5; t.style.paddingBottom = 6;
                var a = L(t, "verb-name"); a.name = "a"; a.style.fontSize = 12; var b = L(t, "card-body"); b.name = "b"; b.style.fontSize = 12; b.style.color = Cream; Show(t, false);
                manifest.Add((t, "line", () => leftTips[i].Q<Label>("a").text + " " + leftTips[i].Q<Label>("b").text));
            }
            var tip = leftTips[i]; Show(tip, s != null); if (s == null) return;
            var tx = host.Text; string name = s.Role.Length > 0 ? "The " + char.ToUpperInvariant(s.Role[0]) + s.Role[1..] : "";
            Text(tip.Q<Label>("a"), tx?.GoFlag ?? "Left behind");
            Text(tip.Q<Label>("b"), (tx != null && tx.GoFate.TryGetValue(s.LeftFate, out var f) ? f : "{who}").Replace("{who}", name));
            var pb = w.Portrait.worldBound; tip.style.left = pb.xMax + 22; tip.style.top = pb.center.y - 20;
        }
        readonly Window[] colWins = new Window[4], rowWins = new Window[4];
        static readonly Rect[] RowWin = { new(37, 15, 65, 64), new(123, 15, 65, 64), new(210, 15, 65, 64), new(296, 15, 65, 64) };
        VisualElement row;
        readonly Strip[] strips = new Strip[4];
        readonly VisualElement[] knobs = new VisualElement[4], knobIcons = new VisualElement[4]; readonly Label[] knobNums = new Label[4]; VisualElement armedPlate; Label armedName;
        sealed class Card { public VisualElement Root, Header, KindIcon, VerbIcon, New, Dot, Prog, Fill, Lines, Footer, Check, Ack; public Label Title, Where, Clock, Remaining, Result, AckKey, Typing; public readonly List<(VisualElement row, VisualElement chip, Label name, Label text)> Rows = new(); public HandoverHost.FeedCard For; }
        readonly Card[] cards = new Card[3]; Label more; HandoverHost.FeedCard hoverCard;
        sealed class IntelUi { public VisualElement Row, Need, NeedGlyph, NeedMark, Temp, TempGlyph, Turned, NextIcon; public Label Name, Job, Next, Secret; }
        // the action panel (action_panel): Glyph's round 6 frame 05 and anatomy a2
        sealed class ActCard
        {
            public VisualElement Root, Cameo, Icon, Suggested, KeyPlate, Lock, Price, Outcome, OutcomeMark, LevRow, Walk, Locked;
            public Label Title, Key, Body, Usd, Time, Trail, Alert, OutcomeText, WalkText, LockReason, Unlock; public int Index;
            public VisualElement Extra; public readonly List<(VisualElement row, VisualElement icon, Label text)> ExtraRows = new(); // brief 013: a way's rows
        }
        VisualElement panel, panelCameo, panelBadge, panelDiscs, panelNeed, panelNeedMark, panelTemp, panelSecret; ScrollView panelCards; Label panelTitle, panelSub, panelAbout, panelNote;
        readonly List<ActCard> actCards = new(); int panelHover = -1;
        VisualElement intel, intelStrip, intelOpen, intelChevron;
        // step 3: the hint, the chips at the cursor, the going-loud card, the briefing, the end card, the history
        VisualElement chipSeenIcon; Label chipSeen; readonly List<VisualElement> unknownMarks = new();
        VisualElement hint, toast, chip, chipPrice, chipIcon, chipMark, loud, loudRing, brief, end, endDetail, history, historyRows; Label hintBody, hintKey, toastText, chipText, chipUsd, chipTime, chipTrail, chipAlert;
        Label loudTitle, loudSub, loudHold, briefTitle, briefSub, briefObjective, briefTarget, briefWin, briefLose, briefClose, briefPurse, briefAlert, briefArms, briefBeginKey, endTitle, endLine, endReplayKey;
        readonly List<Label> loudRows = new(), endLines = new(); readonly List<VisualElement> loudDots = new(); readonly (Label title, Label body)[] briefWays = new (Label, Label)[2];
        readonly List<VisualElement> filterChips = new(); bool endDetailOpen; Label intelCount; readonly IntelUi[] intelRows = new IntelUi[4];
        // collapsible panels (Glyph #8, collapse-states.jpg): the hint folds to a "?" chip after 6 s, the objective after 20 s; a click pins either
        // open, remembered per panel (PlayerPrefs, as Intel and the strips are)
        VisualElement hintChip; Label hintChipKey; string hintSeen; float hintAt, objUntil; bool hintHover, pillHover, objHover, briefWas = true;
        const float HintOpenS = 6f, ObjectiveS = 20f;
        const string HintPinPref = "Suborned.Hud.HintPinned", ObjPinPref = "Suborned.Hud.ObjectivePinned";
        bool? hintPinned, objPinned;
        bool HintPinned { get => hintPinned ??= PlayerPrefs.GetInt(HintPinPref, 0) == 1; set { hintPinned = value; PlayerPrefs.SetInt(HintPinPref, value ? 1 : 0); PlayerPrefs.Save(); } }
        bool ObjPinned { get => objPinned ??= PlayerPrefs.GetInt(ObjPinPref, 0) == 1; set { objPinned = value; PlayerPrefs.SetInt(ObjPinPref, value ? 1 : 0); PlayerPrefs.Save(); } }
        readonly HashSet<int> openRows = new(); // history rows opened to their lines (by card id)

        void Build()
        {
            // the status pill (status_pill): top right 14, 12; height 44; padding from the content rect
            pill = E(root, "s-pill_status", pick: true); pill.style.position = Position.Absolute; pill.style.right = 14; pill.style.top = 14; pill.style.height = 44;
            pill.style.flexDirection = FlexDirection.Row; pill.style.alignItems = Align.Center; pill.style.paddingLeft = 24; pill.style.paddingRight = 24; // clear of the pill's end caps, the same each side
            clock = L(pill, "clock"); Sep(pill);
            Icon(E(pill, null, w: 17, h: 17), "nextevent", TurqLight); nextLabel = L(pill, "pill-label"); nextTime = L(pill, "pill-value"); Sep(pill);
            Icon(E(pill, null, w: 17, h: 17), "p_cash", Cream); cash = L(pill, "pill-value"); Sep(pill);
            Icon(E(pill, null, w: 17, h: 17), "p_trail", Cream); trail = L(pill, "pill-value"); Sep(pill);
            for (int i = 0; i < 3; i++) // alert lamps: 12.4 px, pitch 17; off #0a1c1e with a #78968f ring; on a cream core
            {
                var l = E(pill, null, w: 12.4f, h: 12.4f); l.style.marginRight = 4.6f; Round(l, 6.2f);
                l.style.borderTopWidth = l.style.borderBottomWidth = l.style.borderLeftWidth = l.style.borderRightWidth = 1.4f;
                l.style.borderTopColor = l.style.borderBottomColor = l.style.borderLeftColor = l.style.borderRightColor = new Color(0.47f, 0.588f, 0.561f);
                lamps[i] = l;
            }
            state = L(pill, "pill-value"); state.style.marginLeft = 8;
            foreach (var c in pill.Children()) c.style.flexShrink = 0;
            nextLabel.style.marginLeft = 6; nextTime.style.marginRight = 2; cash.style.marginLeft = 5; trail.style.marginLeft = 5;
            pill.RegisterCallback<PointerEnterEvent>(_ => pillHover = true); pill.RegisterCallback<PointerLeaveEvent>(_ => pillHover = false); // hover the pill: the objective
            // the objective line (objective_line): under the pill, right 14, top 62, height 34
            objective = E(root, "s-panel_strip", pick: true); objective.style.position = Position.Absolute; objective.style.right = 14; objective.style.top = 62; objective.style.height = 34;
            objective.style.flexDirection = FlexDirection.Row; objective.style.alignItems = Align.Center; objective.style.paddingLeft = 40; objective.style.paddingRight = 26;
            var mark = E(objective, null, 23, 12, 11, 11); Round(mark, 5.5f); Border(mark, 1.4f, TurqLight); var dot = E(mark, null, 3.3f, 3.3f, 4.4f, 4.4f); Round(dot, 2.2f); dot.style.backgroundColor = TurqLight;
            objText = L(objective, "objective"); objTail = L(objective, "objective-tail");
            objective.RegisterCallback<PointerEnterEvent>(_ => objHover = true); objective.RegisterCallback<PointerLeaveEvent>(_ => objHover = false);
            objective.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { ObjPinned = !ObjPinned; if (!ObjPinned) objUntil = 0; e.StopPropagation(); } }); // a click pins it open
            // Intel (intel_panel): right 14, top 108, 318 wide; a strip when collapsed, the dark panel with a row a person when open
            intel = E(root); intel.style.position = Position.Absolute; intel.style.right = 14; intel.style.top = 108; intel.style.width = 318;
            intelStrip = E(intel, "s-panel_strip", pick: true); intelStrip.style.height = 34; intelStrip.style.flexDirection = FlexDirection.Row; intelStrip.style.alignItems = Align.Center; intelStrip.style.paddingLeft = 24; intelStrip.style.paddingRight = 18;
            intelStrip.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.IntelOpen = !host.IntelOpen; e.StopPropagation(); } });
            var it = L(intelStrip, "section-label"); it.text = "INTEL"; intelCount = L(intelStrip, "panel-sub"); intelCount.style.marginLeft = 10; intelCount.style.flexGrow = 1;
            intelChevron = E(intelStrip, null, w: 14, h: 14);
            intelOpen = E(intel, "s-panel_dark", pick: true); intelOpen.style.paddingTop = 4; intelOpen.style.paddingBottom = 10; intelOpen.style.marginTop = 4;
            for (int i = 0; i < 4; i++)
            {
                var r = intelRows[i] = new IntelUi();
                r.Row = E(intelOpen); r.Row.style.height = 62; r.Row.style.marginLeft = 14; r.Row.style.marginRight = 14;
                r.Name = L(r.Row, "intel-name", 26, 0, 120, 18); r.Job = L(r.Row, "intel-job", 130, 1, 150, 16, true);
                r.Need = E(r.Row, "s-disc_dark_24", 26, 19, 24, 24); r.NeedGlyph = E(r.Need, null, 5, 5, 14, 14); r.NeedMark = E(r.Need, null, 14, -4, 12, 12);
                r.Temp = E(r.Row, "s-disc_dark_24", 56, 19, 24, 24); r.TempGlyph = E(r.Temp, null, 5, 5, 14, 14);
                r.Turned = E(r.Row, null, 88, 22, 17, 17);
                r.NextIcon = E(r.Row, null, 118, 24, 15, 15); r.Next = L(r.Row, "intel-next", 136, 22, 170, 16, true);
                r.Secret = L(r.Row, "panel-sub", 118, 42, 180, 16, true);
            }
            // the roster column (roster_column): portraits under the frame, then the frame, the scrim and pips, the number plate, the rings
            column = E(root, null, ColX, ColY, 109, 416); BuildRoster(column, "s-roster_column", 109, 416, Win, colWins);
            row = E(root, null, 14, 0, 398, 92); BuildRoster(row, "s-roster_row", 398, 92, RowWin, rowWins); // roster_row (corner, bottom)
            for (int i = 0; i < 4; i++) strips[i] = new Strip();
            // the slot strips (slot_strip): x = column right + 4, centred on their window; 228 x 92
            for (int i = 0; i < 4; i++)
            {
                var st = strips[i]; int k = i;
                st.Root = E(root, "s-panel_slot", ColX + 113, 0, 228, 92, pick: true);
                st.Root.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.PortraitClick(k, e.shiftKey); e.StopPropagation(); } });
                var tags = st.Tags = E(st.Root, null, 30, 7, 170, 16); tags.style.flexDirection = FlexDirection.Row;
                st.Role = L(tags, "role-tag"); st.State = L(tags, "slot-state-ready"); st.State.style.marginLeft = 8;
                st.Stance = E(st.Root, null, 190, 7, 20, 20);
                st.Icon = E(st.Root, null, 21, 27, 18, 18); st.Action = L(st.Root, "slot-action", 55, 28, 140, 20, true);
                st.Bar = E(st.Root, null, 30, 48, 106, 8); st.Bar.style.backgroundColor = new Color(0.039f, 0.102f, 0.11f); Border(st.Bar, 1, new Color(0.51f, 0.627f, 0.612f)); Round(st.Bar, 4);
                st.Fill = E(st.Bar, null, 0, 0, 0, 6); st.Fill.style.backgroundColor = TurqLight; Round(st.Fill, 3);
                st.Time = L(st.Root, "slot-time", 142, 46, 56, 14);
                st.Cancel = E(st.Root, "s-btn_close_normal btn_close", 186, 46, 21, 21, pick: true);
                st.Cancel.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.CancelAgent(k, null); e.StopPropagation(); } });
                st.Cancel.RegisterCallback<PointerEnterEvent>(_ => TipFor(k, true)); st.Cancel.RegisterCallback<PointerLeaveEvent>(_ => TipFor(k, false));
                st.QueuedIcon = E(st.Root, null, 31, 69, 11, 11); Icon(st.QueuedIcon, "nextevent", TurqLight);
                st.Queued = L(st.Root, "slot-queued", 47, 68, 150, 16, true);
                st.Ready = L(st.Root, "slot-ready", 30, 29, 160, 20); st.Hint = L(st.Root, "slot-hint", 30, 50, 170, 16, true);
            }
            // the verb plate (verb_plate): right of the selected strip +10; four knobs, glyph, numeral; the armed knob's name under it
            verbPlate = E(root, "s-plate_verbs", ColX + 113 + 228 + 10, 0, PlateW, PlateH, pick: true); // brief 014 round 2: room for the end bolts and the key numbers
            for (int i = 0; i < 4; i++)
            {
                int k = i; float cx = KnobX0 + 24 + i * 47.6f;
                var kb = knobs[i] = E(verbPlate, "s-btn_verb_normal btn_verb", cx - 24, KnobY, 48, 48, pick: true);
                kb.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.Pick(k); e.StopPropagation(); } });
                knobIcons[i] = E(kb, null, 14, 14, 20, 20);
                var badge = E(verbPlate, null, cx - 8, 56, 16, 9); badge.name = "badge" + i; // under its knob, on the field: top 56, cap height under 8, clear of the rim (69.5)
                knobNums[i] = L(badge, "numplate", 0, 0, 16, 9); knobNums[i].style.color = Cream; knobNums[i].style.fontSize = 9.5f; knobNums[i].style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            armedPlate = E(root, "s-plate_dark", 0, 0, float.NaN, 32); armedPlate.style.justifyContent = Justify.Center; armedName = L(armedPlate, "verb-name"); armedName.style.paddingLeft = 12; armedName.style.paddingRight = 12; armedName.style.paddingTop = 0; armedName.style.paddingBottom = 0; armedName.style.fontSize = 10.5f; armedName.style.unityTextAlign = TextAnchor.MiddleCenter; /* Glyph round 3: 30 px chip, 11 px type: about 5 px above and below */ // Glyph round 2: 8 px clear of the rim
            // the ticker (ticker): below the column +8, 341 x 34; the cell's cameo and its last word, fading
            ticker = E(root, "s-panel_strip", ColX, ColY + 416 + 8, 341, 34);
            tickerChip = E(ticker, null, 14, 3, 28, 28); tickerText = L(ticker, "ticker", 48, 8, float.NaN, 18, true); tickerText.style.right = 16;
            // the cancel tooltip (tooltip_cancel): a card at the cursor
            tooltip = E(root, "s-panel_card", 0, 0, 292, float.NaN); tooltip.style.paddingLeft = 15; tooltip.style.paddingRight = 15; tooltip.style.paddingTop = 30; tooltip.style.paddingBottom = 10;
            tipTitle = L(tooltip, "tooltip-title"); tipBody = L(tooltip, "tooltip-body"); tipBody.style.whiteSpace = WhiteSpace.Normal;
            var keyRow = E(tooltip); keyRow.style.flexDirection = FlexDirection.Row; keyRow.style.marginTop = 4;
            var kp = E(keyRow, "s-plate_light", w: float.NaN, h: 19); tipKey = L(kp, "keycap"); tipKey.style.paddingLeft = 6; tipKey.style.paddingRight = 6;
            var kt = L(keyRow, "tooltip-key"); kt.text = "cancels the selected agent's act"; kt.style.marginLeft = 6;
            Show(tooltip, false);
            BuildPanel(); BuildStep3(); BuildTakedown();
            // the feed (feed_bar, scene_card): bottom right 14; cards above the bar, newest on top
            feed = E(root); feed.style.position = Position.Absolute; feed.style.right = 14; feed.style.bottom = 14; feed.style.width = 384; feed.style.flexDirection = FlexDirection.ColumnReverse;
            var bar = E(feed, "s-panel_strip", pick: true); bar.style.height = 32; bar.style.flexShrink = 0;
            bar.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.FeedHistoryOpen = !host.FeedHistoryOpen; e.StopPropagation(); } });
            var title = L(bar, "feed-bar", 24, 9, 60, 14); title.text = "SCENES";
            barPill = E(bar, "s-pill_white", 82, 7, float.NaN, 18); barCount = L(barPill, "feed-filter"); barCount.style.paddingLeft = 8; barCount.style.paddingRight = 8; barCount.style.unityTextAlign = TextAnchor.MiddleCenter;
            barKeys = L(bar, "feed-bar-sub", 210, 9, 134, 14); barKeys.style.unityTextAlign = TextAnchor.MiddleRight;
            barChevron = E(bar, null, 358, 9, 14, 14);
            more = L(feed, "feed-filter"); more.AddToClassList("s-pill_white"); more.style.alignSelf = Align.FlexEnd; more.style.marginBottom = 6; more.style.marginTop = 2; more.style.paddingLeft = 8; more.style.paddingRight = 8; more.style.height = 20; more.style.unityTextAlign = TextAnchor.MiddleCenter;
            for (int i = 0; i < 3; i++) cards[i] = NewCard(feed);
        }

        void BuildPanel()
        {
            panel = E(root, "s-panel_dark", 0, 0, 384, float.NaN, pick: true); panel.style.paddingBottom = 8;
            panel.RegisterCallback<PointerDownEvent>(e => e.StopPropagation()); // a click on the panel's own frame does nothing (the world never gets it)
            var head = E(panel); head.style.minHeight = 54; head.style.paddingLeft = 52; head.style.paddingRight = 40; head.style.paddingTop = 9; head.style.paddingBottom = 6;
            panelCameo = E(panel, null, 14, 13, 28, 28);
            var titleRow = E(head); titleRow.style.flexDirection = FlexDirection.Row; titleRow.style.alignItems = Align.Center;
            panelTitle = L(titleRow, "panel-title"); panelBadge = E(titleRow, null, w: 18, h: 18); panelBadge.style.marginLeft = 6;
            panelSub = L(head, "panel-sub"); panelSub.style.marginTop = 2;
            panelAbout = L(head, "panel-sub"); panelAbout.style.whiteSpace = WhiteSpace.Normal; panelAbout.style.marginTop = 2; panelAbout.style.color = new Color(0.82f, 0.9f, 0.886f);
            panelDiscs = E(head); panelDiscs.style.flexDirection = FlexDirection.Row; panelDiscs.style.marginTop = 6;
            panelNeed = E(panelDiscs, "s-disc_dark_24", w: 24, h: 24); E(panelNeed, null, 5, 5, 14, 14).name = "glyph"; panelNeedMark = E(panelNeed, null, 14, -4, 12, 12);
            panelTemp = E(panelDiscs, "s-disc_dark_24", w: 24, h: 24); panelTemp.style.marginLeft = 6; E(panelTemp, null, 5, 5, 14, 14).name = "glyph";
            panelSecret = E(panelDiscs, "s-disc_dark_24", w: 24, h: 24); panelSecret.style.marginLeft = 6; Icon(E(panelSecret, null, 5, 5, 14, 14), "secret", Cream);
            var close = E(panel, "s-btn_close_normal btn_close", 355, 13, 17, 17, pick: true);
            close.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ClosePanel(); e.StopPropagation(); } });
            panelNote = L(panel, "panel-sub"); panelNote.style.whiteSpace = WhiteSpace.Normal; panelNote.style.marginLeft = 14; panelNote.style.marginRight = 14; panelNote.style.marginBottom = 6;
            panelCards = new ScrollView(ScrollViewMode.Vertical) { pickingMode = PickingMode.Ignore, focusable = false }; panelCards.verticalScrollerVisibility = ScrollerVisibility.Hidden; panelCards.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            panelCards.style.maxHeight = 1080 - 28 - 90; panelCards.style.marginLeft = 8; panelCards.style.marginRight = 8; panel.Add(panelCards);
            BuildBefore();
            Show(panel, false);
        }

        ActCard NewActCard()
        {
            var c = new ActCard(); int idx = actCards.Count; c.Index = idx;
            c.Root = E(panelCards, "s-panel_card", pick: true); c.Root.style.width = 368; c.Root.style.marginBottom = 6; c.Root.style.paddingBottom = 8;
            c.Root.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { c.Root.style.unityBackgroundImageTintColor = new Color(0.86f, 0.86f, 0.86f); host.PanelDo(c.Index); e.StopPropagation(); } });
            c.Root.RegisterCallback<PointerUpEvent>(_ => Lift(c.Root, true));
            c.Root.RegisterCallback<PointerEnterEvent>(_ => { panelHover = c.Index; host.SetPanelHover(c.Index); });
            c.Root.RegisterCallback<PointerLeaveEvent>(_ => { if (panelHover == c.Index) { panelHover = -1; host.SetPanelHover(-1); } Lift(c.Root, false); });
            // the header band (the card sprite's own 28 px band): agent cameo, verb icon, title, SUGGESTED, the keycap; a padlock when locked
            var hb = E(c.Root); hb.style.height = 28; hb.style.flexDirection = FlexDirection.Row; hb.style.alignItems = Align.Center; hb.style.paddingLeft = 8; hb.style.paddingRight = 10;
            c.Cameo = E(hb, null, w: 20, h: 20); c.Cameo.style.marginRight = 5; c.Cameo.style.flexShrink = 0;
            c.Icon = E(hb, null, w: 16, h: 16); c.Icon.style.marginRight = 6; c.Icon.style.flexShrink = 0;
            c.Title = L(hb, "card-title"); c.Title.style.flexShrink = 1;
            c.Suggested = E(hb, "s-pill_new"); c.Suggested.style.height = 15; c.Suggested.style.marginLeft = 8; var sg = L(c.Suggested, "card-tag"); sg.text = "SUGGESTED"; sg.style.paddingLeft = 6; sg.style.paddingRight = 6; sg.style.unityTextAlign = TextAnchor.MiddleCenter;
            E(hb).style.flexGrow = 1;
            c.Lock = E(hb, null, w: 13, h: 13); Icon(c.Lock, "lock", Cream); c.Lock.style.marginRight = 6;
            c.KeyPlate = E(hb, "s-plate_light"); c.KeyPlate.style.height = 19; c.KeyPlate.style.minWidth = 22; c.Key = L(c.KeyPlate, "keycap"); c.Key.style.flexGrow = 1; c.Key.style.unityTextAlign = TextAnchor.MiddleCenter; c.Key.style.paddingLeft = 5; c.Key.style.paddingRight = 5;
            // the body: what it does; the price chips; the outcome (and the leverage); the walk on the right; locked: why and how to unlock
            var body = E(c.Root); body.style.paddingLeft = 14; body.style.paddingRight = 14; body.style.paddingTop = 8;
            c.Body = L(body, "card-body"); c.Body.style.whiteSpace = WhiteSpace.Normal;
            c.Price = E(body); c.Price.style.flexDirection = FlexDirection.Row; c.Price.style.alignItems = Align.Center; c.Price.style.marginTop = 6;
            Label PricePart(string icon) { var ic = E(c.Price, null, w: 14, h: 14); Icon(ic, icon, Ink); ic.style.marginRight = 4; var l = L(c.Price, "card-price"); l.style.marginRight = 12; return l; }
            c.Usd = PricePart("p_cash"); c.Time = PricePart("p_time"); c.Trail = PricePart("p_trail"); c.Alert = PricePart("p_alert");
            var orow = E(body); orow.style.flexDirection = FlexDirection.Row; orow.style.alignItems = Align.Center; orow.style.marginTop = 5;
            c.OutcomeMark = E(orow, null, w: 14, h: 14); c.OutcomeMark.style.marginRight = 6; c.OutcomeText = L(orow, "card-outcome"); c.OutcomeText.style.flexGrow = 1; c.OutcomeText.style.flexShrink = 1;
            c.Walk = E(orow); c.Walk.style.flexDirection = FlexDirection.Row; c.Walk.style.alignItems = Align.Center; Icon(E(c.Walk, null, w: 12, h: 12), "s_moving", new Color(0.275f, 0.322f, 0.306f)); c.WalkText = L(c.Walk, "card-walk"); c.WalkText.style.marginLeft = 4;
            c.Outcome = orow;
            c.LevRow = E(body); c.LevRow.style.flexDirection = FlexDirection.Row; c.LevRow.style.alignItems = Align.Center; c.LevRow.style.marginTop = 3;
            Icon(E(c.LevRow, null, w: 14, h: 14), "secret", Ink); var lv = L(c.LevRow, "card-outcome"); lv.text = "Leans on their secret"; lv.style.marginLeft = 6;
            c.Extra = E(body); c.Extra.style.marginTop = 4;
            for (int i = 0; i < 5; i++)
            {
                var r = E(c.Extra); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.FlexStart; r.style.marginTop = 3;
                var ic = E(r, null, w: 13, h: 13); ic.style.marginRight = 6; ic.style.marginTop = 1; ic.style.flexShrink = 0;
                var tx = L(r, "card-body"); tx.style.whiteSpace = WhiteSpace.Normal; tx.style.flexShrink = 1; c.ExtraRows.Add((r, ic, tx));
            }
            c.Locked = E(body); c.Locked.style.marginTop = 6;
            c.LockReason = L(c.Locked, "card-body-locked"); c.LockReason.style.whiteSpace = WhiteSpace.Normal;
            c.Unlock = L(c.Locked, "card-body-locked"); c.Unlock.style.whiteSpace = WhiteSpace.Normal; c.Unlock.style.marginTop = 2;
            actCards.Add(c); return c;
        }

        void PanelFrame()
        {
            Show(panel, m.PanelOpen); if (!m.PanelOpen) return;
            Bg(panelCameo, $"portraits/cameo_{m.PanelRole}_normal");
            Text(panelTitle, $"{char.ToUpperInvariant(m.PanelRole[0])}{m.PanelRole[1..]} → {m.PanelTarget}");
            Show(panelBadge, m.PanelBadge.Length > 0); if (m.PanelBadge.Length > 0) Bg(panelBadge, m.PanelBadge);
            Text(panelSub, m.PanelPerson ? m.PanelSub : ""); Show(panelSub, m.PanelPerson);
            Text(panelAbout, m.PanelAbout); Show(panelAbout, m.PanelAbout.Length > 0);
            Show(panelDiscs, m.PanelPerson);
            if (m.PanelPerson) { Icon(panelNeed.Q("glyph"), m.PanelNeed, Cream); Bg(panelNeedMark, $"badges/mark_{m.PanelNeedMark}"); Icon(panelTemp.Q("glyph"), m.PanelTemp, Cream); Show(panelSecret, m.PanelSecret); }
            Text(panelNote, m.PanelNote); Show(panelNote, m.PanelNote.Length > 0);
            BeforeFrame();
            var cards = m.PanelCards;
            while (actCards.Count < cards.Count) NewActCard();
            for (int i = 0; i < actCards.Count; i++)
            {
                var u = actCards[i]; Show(u.Root, i < cards.Count); if (i >= cards.Count) continue;
                var c = cards[i];
                Bg(u.Cameo, $"portraits/cameo_{c.Role}_normal"); Icon(u.Icon, c.Icon, Cream);
                Text(u.Title, c.Name); Show(u.Suggested, c.Suggested && c.Available);
                Show(u.KeyPlate, c.Verb.Length > 0 && c.Mine); Text(u.Key, c.Verb);
                Show(u.Lock, !c.Available);
                Text(u.Body, c.Does); Show(u.Body, c.Does.Length > 0);
                bool priced = c.Available || c.Usd.Length > 0;
                Show(u.Price, priced && !(c.Usd == "$0" && c.Time.Length == 0)); Text(u.Usd, c.Usd); Text(u.Time, c.Time); Text(u.Trail, c.Trail); Text(u.Alert, c.Alert);
                Show(u.Outcome, c.Available && (c.Outcome.Length > 0 || c.Walk.Length > 0)); Text(u.OutcomeText, c.Outcome);
                Show(u.OutcomeMark, c.Outcome.Length > 0); if (c.Outcome.Length > 0) Bg(u.OutcomeMark, $"badges/mark_{c.OutcomeMark}");
                Show(u.Walk, c.Walk.Length > 0); Text(u.WalkText, c.Walk);
                Show(u.LevRow, c.Leverage && c.Available);
                Show(u.Extra, c.Extra.Count > 0);
                for (int k = 0; k < u.ExtraRows.Count; k++) { var (er, ei, et) = u.ExtraRows[k]; Show(er, k < c.Extra.Count); if (k < c.Extra.Count) { Icon(ei, c.Extra[k].icon, Ink); Text(et, c.Extra[k].text); } }
                Show(u.Locked, !c.Available); Text(u.LockReason, c.Reason.Length > 0 ? "Locked: " + c.Reason : ""); Text(u.Unlock, c.Unlock.Length > 0 ? "To unlock: " + c.Unlock : ""); Show(u.Unlock, c.Unlock.Length > 0);
                // locked: dimmed (the kit: 38% darker card); hover: the hot outline (white 3.2 + turquoise inner)
                u.Root.style.opacity = c.Available ? 1f : 0.62f;
                Lift(u.Root, panelHover == i); // hover: the card's tint lifts (no outline)
            }
            // at the click, right of the target, clamped to the screen with 14 px margin
            var at = RuntimePanelUtils.ScreenToPanel(root.panel, m.PanelAt);
            float w = 384, hgt = panel.resolvedStyle.height; if (float.IsNaN(hgt) || hgt <= 0) hgt = 300;
            var size = root.panel.visualTree.layout.size;
            panel.style.left = Mathf.Clamp(at.x + 12, 14, Mathf.Max(14, size.x - w - 14)); panel.style.top = Mathf.Clamp(at.y - 12, 14, Mathf.Max(14, size.y - hgt - 14));
        }

        /// <summary>A roster frame: the portraits under the frame sprite (its windows are transparent), then the scrim and pips, the number plate,
        /// the selected and busy rings (roster_column and roster_row share the layers).</summary>
        void BuildRoster(VisualElement frame, string sprite, float w0, float h0, Rect[] wins, Window[] into)
        {
            for (int i = 0; i < 4; i++)
            {
                var w = into[i] = new Window(); int k = i; var r = wins[i];
                w.Portrait = E(frame, null, r.x, r.y, r.width, r.height, pick: true);
                w.Portrait.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.PortraitClick(k, e.shiftKey); e.StopPropagation(); } });
            }
            E(frame, sprite, 0, 0, w0, h0);
            for (int i = 0; i < 4; i++)
            {
                var w = into[i]; var r = wins[i];
                // brief 014, Sky round 2: the photo fills the bezel's inner circle, centred on it. The circle is measured from the art (the cream band's
                // outer edge, a least-squares fit over 72 rays in roster_column@2x; briefs/014/play/polish/round2/bezels.json): it is not the window
                // rect's centre (that was 1 px right and 1.2 px low, so the band showed thick at the top). Everything over the photo (the scrim, the
                // health pips) is clipped to the same circle; the key badge sits above all of it at one offset from the centre on every portrait.
                // Selection: Glyph's inner ring (portrait_ring_inner, inside the photo circle: the art has no room outside the bezel), the turquoise
                // key badge and the others dimmed; busy is the timer chip.
                Vector2 c0 = sprite == "s-roster_column" && i < ColumnBezels.Length ? ColumnBezels[i] : new Vector2(r.x + r.width / 2, r.y + r.height / 2);
                const float rad = BezelR; var clip = E(frame, null, c0.x - rad, c0.y - rad, 2 * rad, 2 * rad); Round(clip, rad); clip.style.overflow = Overflow.Hidden;
                w.Portrait.RemoveFromHierarchy(); clip.Add(w.Portrait); w.Portrait.style.left = 0; w.Portrait.style.top = 0; w.Portrait.style.width = 2 * rad; w.Portrait.style.height = 2 * rad;
                E(clip, "s-portrait_scrim", 0, 2 * rad - 16, 2 * rad, 16);
                w.RingSel = E(clip, "s-portrait_ring_inner", 0, 0, 2 * rad, 2 * rad); // Glyph round 2: the selected agent's ring, inside the photo circle (layers: photo, scrim, ring, pips)
                w.Pips = E(clip, null, 0, 2 * rad - 14, 2 * rad, 8); w.Pips.style.flexDirection = FlexDirection.Row; w.Pips.style.justifyContent = Justify.Center; // raised 2 px to clear the ring's band
                w.RingBusy = E(frame, null, 0, 0, 0, 0); // busy is the timer chip; never drawn
                w.Arm = E(frame, "s-disc_dark_24", c0.x + 14, c0.y + 13, 20, 20); Round(w.Arm, 10); Border(w.Arm, 1.6f, Color.clear); // Glyph fight r1 #5: 3 px in, 2 px up, clear of the selection ring; the top right stays free for 018's in-car badge
                Icon(E(w.Arm, null, 2.4f, 2.4f, 12, 12), "fire", Cream); // Sky 2026-10-18: a sidearm pip on every armed agent (all four carry one in the Handover)
                w.Car = E(frame, null, c0.x + 13, c0.y - 34, 22, 22); Round(w.Car, 11); Border(w.Car, 1.6f, Color.clear); E(w.Car, null, 4, 4, 14, 14).name = "g"; P(w.Car, 3, 3, 16, 16, (p, r) => CuffsPaint(p, r, Red)).name = "cf"; Show(w.Car, false);
                w.Rim = E(frame, null, c0.x - BezelR - 2, c0.y - BezelR - 2, 2 * BezelR + 4, 2 * BezelR + 4); Round(w.Rim, BezelR + 2); Border(w.Rim, 2f, Cream); Show(w.Rim, false); // the hover rim (brief 018 §6.6: the left-behind flag's line on hover)
                int kk = i; w.Portrait.RegisterCallback<PointerEnterEvent>(_ => rosterHover = kk); w.Portrait.RegisterCallback<PointerLeaveEvent>(_ => { if (rosterHover == kk) rosterHover = -1; }); // brief 018 §6.6: the in-car badge, or (Go card up) the left-behind flag, in the top right kept for it
                var plate = E(frame, "s-plate_light", c0.x - 34, c0.y - 34, 25, 21); plate.name = "keyplate"; w.Num = L(plate, "numplate"); w.Num.style.flexGrow = 1; w.Num.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
        }

        /// <summary>The roster column's bezel centres (1x kit px, from the art) and the photo's radius: the cream band's outer edge (38.2 to 38.6) plus
        /// half a pixel into the chrome, so no cream shows at any edge.</summary>
        /// <summary>The order plate (plate_verbs, 9-sliced): its end bolts span x 16 to 26 at 1x, so the first knob's bezel (inset 4 in its 48 px
        /// element) starts at 31; the plate is as wide again on the right, and tall enough for a row of key numbers under the knobs.</summary>
        public const float PlateW = 248, PlateH = 78, KnobX0 = 28.5f, KnobY = 9.5f; // Glyph round 2: the rebuilt plate is drawn 1:1 at 78 px tall (field y 8.5 to 69.5); both bolt gaps ~8 px
        static readonly Vector2[] ColumnBezels = { new(54.03f, 65.27f), new(54.09f, 159.25f), new(54.00f, 254.65f), new(53.99f, 348.85f) };
        const float BezelR = 39f;

        void BuildStep3()
        {
            // the hint (hint_plate): 600 wide at (520, 12); the NEXT STEP tab, the close (H), the body; a toast below it
            hint = E(root, "s-panel_dark", 520, 12, 600, float.NaN, pick: true); hint.style.paddingTop = 46; hint.style.paddingLeft = 32; hint.style.paddingRight = 70; hint.style.paddingBottom = 12;
            var tab = E(hint, "s-panel_tab_small", 28, 12, 94, 22); var tl = L(tab, "hint-tab"); tl.text = "NEXT STEP"; tl.style.flexGrow = 1; tl.style.unityTextAlign = TextAnchor.MiddleCenter;
            hintBody = L(hint, "hint-body"); hintBody.style.whiteSpace = WhiteSpace.Normal;
            var hc = E(hint, "s-btn_close_normal btn_close", 562, 15, 17, 17, pick: true); hc.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.DismissHint(); e.StopPropagation(); } });
            var hk = E(hint, "s-plate_light", 556, 38, 29, 19); hintKey = L(hk, "keycap"); hintKey.style.flexGrow = 1; hintKey.style.unityTextAlign = TextAnchor.MiddleCenter;
            hint.RegisterCallback<PointerEnterEvent>(_ => hintHover = true); hint.RegisterCallback<PointerLeaveEvent>(_ => hintHover = false);
            // the hint folded: a "?" disc at the top centre with its key; hover opens the plate, a click pins it open (or folds it again)
            hintChip = E(root, "s-disc_dark_30", 520 + 300 - 17, 14, 34, 34, pick: true); /* centred where the plate stands */ Icon(E(hintChip, null, 8, 8, 18, 18), "p_unknown", Cream);
            var hck = E(hintChip, "s-plate_light", 22, 22, 17, 15); hintChipKey = L(hck, "keycap"); hintChipKey.style.flexGrow = 1; hintChipKey.style.unityTextAlign = TextAnchor.MiddleCenter; hintChipKey.style.fontSize = 9;
            hintChip.RegisterCallback<PointerEnterEvent>(_ => hintHover = true); hintChip.RegisterCallback<PointerLeaveEvent>(_ => hintHover = false);
            hintChip.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { HintPinned = !HintPinned; e.StopPropagation(); } });
            toast = E(root, "s-panel_strip", 520, 0, float.NaN, 34); toast.style.paddingLeft = 24; toast.style.paddingRight = 24; toastText = L(toast, "ticker"); toastText.style.flexGrow = 1;
            // the chips at the cursor (price_chip, fight_chip): never take clicks
            chip = E(root, "s-chip_price", 0, 0, float.NaN, 42); chip.style.flexDirection = FlexDirection.Row; chip.style.alignItems = Align.Center; chip.style.paddingLeft = 17; chip.style.paddingRight = 16; chip.style.paddingBottom = 11;
            chipIcon = E(chip, null, w: 20, h: 20); chipIcon.style.marginRight = 12;
            chipPrice = E(chip); chipPrice.style.flexDirection = FlexDirection.Row; chipPrice.style.alignItems = Align.Center;
            Label Part(string icon) { var ic = E(chipPrice, null, w: 17, h: 17); Icon(ic, icon, Cream); ic.style.marginRight = 5; var l = L(chipPrice, "chip-value"); l.style.marginRight = 16; return l; }
            chipUsd = Part("p_cash"); chipTime = Part("p_time"); chipTrail = Part("p_trail"); chipAlert = Part("p_alert");
            chipSeenIcon = E(chipPrice, null, w: 17, h: 17); Icon(chipSeenIcon, "watch", Cream); chipSeenIcon.style.marginRight = 5; chipSeen = L(chipPrice, "chip-value"); chipSeen.style.marginRight = 16; // seen by: ? or who
            chipMark = E(chip, null, w: 17, h: 17);
            chipText = L(chip, "fight-chip");
            // the going-loud card (going_loud_card): 430 wide, near the cursor; the hold ring fills while the input is held
            loud = E(root, "s-panel_dark_modal", 0, 0, 430, float.NaN, pick: true); loud.style.paddingLeft = 28; loud.style.paddingRight = 28; loud.style.paddingTop = 18; loud.style.paddingBottom = 18;
            loud.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.GateHoldByCard(); e.StopPropagation(); } });
            var lh = E(loud); lh.style.flexDirection = FlexDirection.Row; lh.style.alignItems = Align.Center;
            var ld = E(lh, "s-disc_dark_30", w: 34, h: 34); Icon(E(ld, null, 8, 8, 18, 18), "p_alert", Cream);
            var lt = E(lh); lt.style.marginLeft = 10; loudTitle = L(lt, "loud-title"); loudTitle.text = "Going loud"; loudSub = L(lt, "loud-row"); loudSub.style.color = new Color(0.69f, 0.8f, 0.78f);
            for (int i = 0; i < 6; i++) { var r = E(loud); r.style.flexDirection = FlexDirection.Row; r.style.marginTop = 6; Icon(E(r, null, w: 14, h: 14), "dot", TurqLight); var l = L(r, "loud-row"); l.style.marginLeft = 8; l.style.whiteSpace = WhiteSpace.Normal; l.style.flexShrink = 1; loudRows.Add(l); }
            var hold = E(loud); hold.style.flexDirection = FlexDirection.Row; hold.style.alignItems = Align.Center; hold.style.marginTop = 12;
            loudRing = E(hold, "s-ring_track", w: 44, h: 44); loudRing.style.flexShrink = 0;
            for (int k = 0; k < 24; k++) { float a = Mathf.PI / 2 - k * Mathf.PI * 2 / 24; var dt = E(loudRing, null, 22 + 17 * Mathf.Cos(a) - 2, 22 - 17 * Mathf.Sin(a) - 2, 4, 4); Round(dt, 2); loudDots.Add(dt); }
            loudHold = L(hold, "loud-hold"); loudHold.style.marginLeft = 12; loudHold.style.whiteSpace = WhiteSpace.Normal; loudHold.style.flexShrink = 1;
            // the briefing (briefing_card): 700 x 548, top centre +40, y 150
            brief = E(root, "s-panel_dark_modal", 0, 150, 700, float.NaN, pick: true); brief.style.paddingLeft = 30; brief.style.paddingRight = 30; brief.style.paddingTop = 24; brief.style.paddingBottom = 24;
            var btab = E(brief, "s-panel_tab_small"); btab.style.width = 170; btab.style.height = 28; var btl = L(btab, "hint-tab"); btl.text = "MISSION BRIEFING"; btl.style.flexGrow = 1; btl.style.unityTextAlign = TextAnchor.MiddleCenter;
            var bt = E(brief); bt.style.flexDirection = FlexDirection.Row; bt.style.alignItems = Align.FlexEnd; bt.style.marginTop = 8;
            briefTitle = L(bt, "briefing-title"); briefSub = L(bt, "briefing-sub"); briefSub.style.marginLeft = 14; briefSub.style.marginBottom = 4;
            Label Section(string icon, string label) { var r = E(brief); r.style.marginTop = 12; var h = E(r); h.style.flexDirection = FlexDirection.Row; h.style.alignItems = Align.Center; Icon(E(h, null, w: 14, h: 14), icon, TurqLight); var sl = L(h, "section-label"); sl.text = label; sl.style.marginLeft = 8; var b = L(r, "briefing-body"); b.style.whiteSpace = WhiteSpace.Normal; b.style.marginLeft = 22; return b; }
            briefObjective = Section("take", "OBJECTIVE"); briefTarget = Section("p_unknown", "THE TARGET");
            var wl = E(brief); wl.style.flexDirection = FlexDirection.Row; wl.style.marginTop = 12;
            Label Box(string mark, string label, float left) { var bx = E(wl); bx.style.flexGrow = 1; bx.style.flexBasis = 0; bx.style.minHeight = 94; bx.style.marginLeft = left; bx.style.backgroundColor = new Color(0.094f, 0.235f, 0.227f); Round(bx, 8); bx.style.paddingLeft = 12; bx.style.paddingRight = 12; bx.style.paddingTop = 10; var h = E(bx); h.style.flexDirection = FlexDirection.Row; Bg(E(h, null, w: 15, h: 15), "badges/" + mark); var sl = L(h, "section-label"); sl.text = label; sl.style.marginLeft = 8; var b = L(bx, "briefing-small"); b.style.whiteSpace = WhiteSpace.Normal; b.style.marginTop = 4; return b; }
            briefWin = Box("mark_ok", "YOU WIN", 0); briefLose = Box("mark_no", "YOU LOSE", 10);
            var facts = E(brief); facts.style.flexDirection = FlexDirection.Row; facts.style.marginTop = 12;
            Label Fact(string icon) { var f = E(facts); f.style.flexDirection = FlexDirection.Row; f.style.alignItems = Align.Center; f.style.marginRight = 22; Icon(E(f, null, w: 14, h: 14), icon, Cream); var l = L(f, "briefing-fact"); l.style.marginLeft = 6; return l; }
            briefClose = Fact("p_time"); briefPurse = Fact("p_cash"); briefAlert = Fact("p_alert"); briefArms = Fact("fire");
            var ways = E(brief); ways.style.flexDirection = FlexDirection.Row; ways.style.marginTop = 12;
            for (int i = 0; i < 2; i++) { var c = E(ways, "s-panel_card"); c.style.flexGrow = 1; c.style.flexBasis = 0; c.style.minHeight = 106; if (i == 1) c.style.marginLeft = 10; var t = L(c, "card-title"); t.style.marginLeft = 12; t.style.marginTop = 5; var b = L(c, "card-body"); b.style.whiteSpace = WhiteSpace.Normal; b.style.marginLeft = 12; b.style.marginRight = 12; b.style.marginTop = 10; briefWays[i] = (t, b); }
            var foot = E(brief); foot.style.flexDirection = FlexDirection.Row; foot.style.alignItems = Align.Center; foot.style.marginTop = 16;
            var fcol = E(foot); fcol.style.flexGrow = 1; fcol.style.flexShrink = 1;
            var fl = L(fcol, "briefing-small"); briefFootLine = fl; fl.text = "Time stops until you begin. Space pauses at any time."; fl.style.color = new Color(0.69f, 0.8f, 0.78f);
            { // GDD v0.24: the Office front is played at yaw 210, two quarter turns from the opening view; the briefing names the turn keys
                var kr = E(fcol); kr.style.flexDirection = FlexDirection.Row; kr.style.alignItems = Align.Center; kr.style.marginTop = 6;
                KeyCap(kr, host.controls.Label(PlayAction.TurnLeft)); KeyCap(kr, host.controls.Label(PlayAction.TurnRight)); kr[0].style.marginLeft = 0;
                var kl = L(kr, "briefing-small"); kl.text = "turn the view."; kl.style.color = new Color(0.69f, 0.8f, 0.78f); kl.style.marginLeft = 8;
            }
            var begin = E(foot, "s-btn_text_normal btn_text", w: 190, h: 40, pick: true); begin.style.flexDirection = FlexDirection.Row; begin.style.alignItems = Align.Center; begin.style.justifyContent = Justify.Center;
            begin.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.StartClock(); e.StopPropagation(); } });
            var bl = L(begin, "button-label"); bl.text = "Begin"; briefBeginKey = L(null, "keycap"); /* Sky: no key hint on a button (keys are taught by the hints at first use); Space still begins */
            BuildLine(brief, corner: true); // Sky 2026-10-19: which build this is, quiet, in the card's corner (BuildStamp)
            // the end card (debrief_card): 560 wide, top centre +40, y 160; Debrief opens the numbers, Replay plays again
            end = E(root, "s-panel_dark_modal", 0, 160, 560, float.NaN, pick: true); end.style.paddingLeft = EndCardPad; end.style.paddingRight = EndCardPad; end.style.paddingTop = 24; end.style.paddingBottom = 38; // Glyph round 2: bottom matches top
            endTitle = L(end, "briefing-title"); endTitle.style.whiteSpace = WhiteSpace.Normal; /* a reading surface: the line wraps to two lines (EndCardAudit) */ endLine = L(end, "briefing-body"); endLine.style.whiteSpace = WhiteSpace.Normal; endLine.style.marginTop = 6;
            endDetail = E(end); endDetail.style.marginTop = 10;
            for (int i = 0; i < 14; i++) { var row = E(endDetail); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; var cm = E(row, null, w: 18, h: 18); cm.name = "cm"; Round(cm, 9); cm.style.flexShrink = 0; cm.style.marginRight = 4; var fg = E(row, null, w: 16, h: 16); fg.name = "fg"; fg.style.flexShrink = 0; fg.style.marginRight = 7; P(fg, 0, 0, 16, 16, (p, r) => CuffsPaint(p, r, Red)).name = "cf"; var l = L(row, "briefing-small"); l.style.flexShrink = 1; /* brief 018: room for the getaway rows; a row may lead with a cameo and a fate glyph (Glyph r2) */ l.style.whiteSpace = WhiteSpace.Normal; l.style.marginTop = 3; endLines.Add(l); }
            var eb = E(end); eb.style.flexDirection = FlexDirection.Row; eb.style.marginTop = 18; eb.style.justifyContent = Justify.Center; // Glyph round 2: the pair centred
            VisualElement Btn(string text, System.Action act) { var b = E(eb, "s-btn_text_normal btn_text", w: 150, h: 40, pick: true); b.style.marginLeft = 10; b.style.marginRight = 10; b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center; b.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { act(); e.StopPropagation(); } }); var l = L(b, "button-label"); l.text = text; return b; }
            Btn("Debrief", () => endDetailOpen = !endDetailOpen);
            showAgain = Btn("Show me", () => host.FailCamStart()); // the fail camera again (Sky on the Air: "Show me again" ran past its pill) // the fail camera again (HostFailCam)
            var rp = Btn("Replay", () => host.Begin()); endButtons = eb; endReplayKey = L(null, "keycap"); /* Sky: label only, no keycap; Return still replays */
            // the history (history_panel): above the feed bar; filters, Read all, the rows (the wheel scrolls them)
            history = E(root, "s-panel_dark", pick: true); history.style.position = Position.Absolute; history.style.right = 14; history.style.bottom = 14 + 32 + 6; history.style.width = 384;
            history.style.paddingLeft = 28; history.style.paddingRight = 28; history.style.paddingTop = 14; history.style.paddingBottom = 12; // clear of the frame's corner screws
            var hh = E(history); hh.name = "head"; hh.style.flexDirection = FlexDirection.Row; hh.style.alignItems = Align.Center; var ht = L(hh, "section-label"); ht.text = "SCENES · HISTORY"; ht.style.flexGrow = 1;
            history.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && !host.FeedHistoryOpen) { host.FeedHistoryOpen = true; e.StopPropagation(); } }); // Sky 2026-10-18: collapsed, a click on the panel expands it (L too)
            var ra = E(hh, "s-pill_white", pick: true); ra.style.height = 20; var ral = L(ra, "feed-filter"); ral.text = "Read all"; ral.style.paddingLeft = 8; ral.style.paddingRight = 8; ral.style.unityTextAlign = TextAnchor.MiddleCenter;
            ra.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ReadAll(); e.StopPropagation(); } });
            var fr = E(history); fr.name = "filters"; fr.style.flexDirection = FlexDirection.Row; fr.style.flexWrap = Wrap.Wrap; fr.style.marginTop = 8;
            foreach (var (key, label) in new[] { ("all", "All"), ("learned", "Learned"), ("turned", "Turned"), ("city", "City"), ("warnings", "Warnings") })
            {
                var f = E(fr, "s-plate_dark", pick: true); f.style.height = 19; f.style.height = 24; f.style.unitySliceScale = 0.5f; f.style.marginRight = 6; f.style.marginBottom = 4; f.name = key; var fl2 = L(f, "feed-filter"); fl2.text = label; fl2.style.paddingLeft = 12; fl2.style.paddingRight = 12; fl2.style.flexGrow = 1; fl2.style.unityTextAlign = TextAnchor.MiddleCenter; /* Sky: the same padding rule as the other chips */
                f.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.FeedFilter = key; e.StopPropagation(); } });
                filterChips.Add(f);
            }
            // Sky 2026-10-18 (the list clipped "Reroute patrol 4:02" mid-line and had no scroll): newest at the top, a scrollable list (wheel,
            // trackpad, a thin bar in the panel's style) under a fixed header and filters, a fade on the bottom edge so no row ends cut, and
            // "↑ n new" when entries arrive while the player reads further down
            var wrap = E(history); wrap.name = "list"; wrap.style.position = Position.Relative; wrap.style.flexShrink = 1; wrap.style.marginTop = 6; wrap.style.overflow = Overflow.Hidden;
            var sv = new ScrollView(ScrollViewMode.Vertical) { focusable = false }; sv.verticalScrollerVisibility = ScrollerVisibility.Auto; sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden; sv.mouseWheelScrollSize = 36f;
            sv.style.maxHeight = 420; sv.style.overflow = Overflow.Hidden; sv.style.flexShrink = 1; wrap.Add(sv); history.style.overflow = Overflow.Hidden; /* the list scrolls inside its frame, never past it */
            ScrollBasics(sv); ThinScroller(sv.verticalScroller);
            historyRows = sv.contentContainer; historySv = sv;
            historyFade = E(wrap, null, pick: false); historyFade.style.position = Position.Absolute; historyFade.style.left = 0; historyFade.style.right = 10; historyFade.style.bottom = 0; historyFade.style.height = 28; historyFade.style.backgroundImage = FadeTexture();
            historyNew = E(hh, "s-pill_white", pick: true); historyNew.style.height = 20; historyNew.style.marginRight = 6; historyNew.style.flexShrink = 0; historyNew.PlaceBehind(ra); // in the header beside Read all: over the list it covered the top row's title
            var hnl = L(historyNew, "feed-filter"); hnl.name = "n"; hnl.style.paddingLeft = 10; hnl.style.paddingRight = 10; hnl.style.unityTextAlign = TextAnchor.MiddleCenter;
            historyNew.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { historySv.scrollOffset = Vector2.zero; historyPinned = true; historyNewCount = 0; e.StopPropagation(); } });
            Show(historyNew, false);
            { var cv = E(hh, null, w: 18, h: 18, pick: true); cv.style.marginLeft = 10; Icon(cv, "chev_down", Cream); cv.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.FeedHistoryOpen = false; e.StopPropagation(); } }); } // collapse
            foreach (var e2 in new[] { hint, hintChip, toast, chip, loud, brief, end, history }) Show(e2, false);
        }

        VisualElement endButtons; /** the end card's button row: every pill the widest label's width plus 28 px each side (EndButtonsFit) */
        /// <summary>Test hook for the layout audit: a deliberately long label on the middle button, so the end card's button check can be seen to fail.</summary>
        public static bool EndButtonBad; float EndButtonsW;
        void EndButtonsFit()
        {
            if (endButtons == null) return; float w = 0;
            foreach (var b in endButtons.Children()) if (b.resolvedStyle.display != DisplayStyle.None && b.Q<Label>() is Label l)
            { if (b == showAgain) l.text = EndButtonBad ? "Show me the whole thing again" : "Show me"; w = Mathf.Max(w, l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x); }
            float pill = EndButtonBad ? 150 : Mathf.Max(130, Mathf.Ceil(w) + 76) /* the pill art's pointed ends take about 24 px each */; int shown = 0; foreach (var b in endButtons.Children()) { b.style.width = pill; b.style.marginLeft = b.style.marginRight = 8; b.style.flexShrink = 0; if (b.resolvedStyle.display != DisplayStyle.None) shown++; }
            EndButtonsW = shown * (pill + 16) + 2 * EndCardPad; // the card is at least this wide (EndLess): a squeezed pill ran its label past its rim
        }
        VisualElement showAgain, failBox, failMarks, failChip, failLoc, failLocIcon; Label failCap, failTip; bool failHid;
        /// <summary>The fail camera (HostFailCam): every HUD piece hidden but one caption at the bottom centre and its tip under it (Fable's
        /// less.failcam); the end card waits. True while it plays.</summary>
        bool FailCamHud()
        {
            if (failBox == null)
            {
                failBox = E(root, "s-plate_dark", 0, 0, float.NaN, float.NaN); failBox.style.top = StyleKeyword.Auto; failBox.style.bottom = 40; failBox.style.paddingLeft = 22; failBox.style.paddingRight = 22; failBox.style.paddingTop = 10; failBox.style.paddingBottom = 12; failBox.style.alignItems = Align.Center;
                failCap = L(failBox, "verb-name"); failCap.style.fontSize = 17; failTip = L(failBox, "card-body"); failTip.style.color = Cream; failTip.style.opacity = 0.8f; failTip.style.marginTop = 3; failTip.style.fontSize = 13;
                manifest.Add((failCap, "line", () => failCap.text)); manifest.Add((failTip, "line", () => failTip.text)); Show(failBox, false);
                failCap.style.fontSize = 17; failCap.style.whiteSpace = WhiteSpace.NoWrap; failTip.style.fontSize = 17; failTip.style.color = Cream; failTip.style.opacity = 1f; failTip.style.marginTop = 0; failTip.style.whiteSpace = WhiteSpace.NoWrap;
                // Glyph r1: the marks drawn over the world: the caught operative's pulsing cyan ring, a dotted line to the culprit, its locator
                failMarks = new VisualElement { pickingMode = PickingMode.Ignore }; failMarks.style.position = Position.Absolute; failMarks.style.left = 0; failMarks.style.top = 0; failMarks.style.right = 0; failMarks.style.bottom = 0;
                failMarks.generateVisualContent += FailMarksPaint; root.Add(failMarks); failMarks.SendToBack(); Show(failMarks, false);
                failLoc = E(root, "s-disc_dark_24", 0, 0, 28, 28); Border(failLoc, 2f, new Color(0.878f, 0.227f, 0.184f)); failLocIcon = E(failLoc, null, 5, 5, 18, 18); Icon(failLocIcon, "eye_open", new Color(0.878f, 0.227f, 0.184f)); manifest.Add((failLoc, "image", () => "culprit locator")); Show(failLoc, false);
                failChip = E(root, "s-plate_dark", 14, 14, float.NaN, 30); failChip.style.flexDirection = FlexDirection.Row; failChip.style.alignItems = Align.Center; failChip.style.paddingLeft = 12; failChip.style.paddingRight = 12;
                var fcl = L(failChip, "verb-name"); fcl.name = "t"; fcl.style.fontSize = 12; manifest.Add((failChip, "image", () => "replay chip")); Show(failChip, false);
            }
            bool f = host.FailCamOn;
            if (showAgain != null) Show(showAgain, !f && host.FailCamAvailable && m.Ended); EndButtonsFit();
            if (!f && !failHid) return false;
            failHid = f;
            foreach (var c in root.Children()) if (c != failBox && c != failMarks && c != failLoc && c != failChip) c.style.visibility = f ? Visibility.Hidden : StyleKeyword.Null;
            Show(failBox, f); Show(failMarks, f); Show(failChip, f); if (!f) { Show(failLoc, false); return false; }
            // one line: the caption while it replays; at the deciding moment the tip replaces it, the same size (Glyph r1)
            bool atMoment = host.FailAtMoment && host.FailTip.Length > 0;
            Text(failCap, atMoment ? host.FailTip : host.FailCaption); Show(failTip, false);
            Text(failChip.Q<Label>("t"), $"replay {host.FailPace:0.0#}×");
            Icon(failLocIcon, host.FailShot ? "fire" : "eye_open", new Color(0.878f, 0.227f, 0.184f)); // a shooter is marked by the shot, not an eye
            if (host.FailCulpritAt is Vector3 cw && Proj(cw, out var cq)) { Show(failLoc, true); failLoc.style.left = cq.x - 14; failLoc.style.top = cq.y - 14; } else Show(failLoc, false);
            failMarks.MarkDirtyRepaint();
            var sz = root.panel.visualTree.layout.size; float bw = failBox.resolvedStyle.width; if (float.IsNaN(bw)) bw = 360; failBox.style.left = ((float.IsNaN(sz.x) ? 1920 : sz.x) - bw) / 2;
            return true;
        }
        /// <summary>The fail camera's marks: a 4 px cyan ring round the caught operative's feet (about 1.3 x a figure's width, pulsing) and a
        /// dotted line from it up to the culprit's locator.</summary>
        void FailMarksPaint(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D; var cam = host.View; if (!cam || !host.FailCamOn) return;
            if (host.FailOpAt is not Vector3 ow || !Proj(ow, out var oq)) return;
            float px = 14f / cam.orthographicSize, k = 1f + 0.1f * Mathf.Sin(Time.unscaledTime * 5f), rx = 0.75f * 38.6f * px * k, ry = rx * 0.62f;
            var cyan = new Color(0.376f, 0.878f, 0.918f);
            for (int pass = 0; pass < 2; pass++)
            {
                p.strokeColor = pass == 0 ? new Color(0.09f, 0.188f, 0.184f, 0.6f) : cyan; p.lineWidth = pass == 0 ? 6.5f : 4f; p.BeginPath();
                for (int i = 0; i <= 40; i++) { float a = i * Mathf.PI * 2f / 40f; var v = oq + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); if (i == 0) p.MoveTo(v); else p.LineTo(v); }
                p.Stroke();
            }
            if (host.FailShot && Proj(host.FailShooterAt, out var sq) && Proj(ow + Vector3.up * 1.3f, out var hq))
            {
                // Glyph 022 (N125): the shot itself, a tracer from the shooter's chest to the operative's, flashing once every 0.7 s
                float ph = (Time.unscaledTime % 0.7f) / 0.7f, a = ph < 0.4f ? 1f : 0.8f; // a steady line that flashes: any still frame shows it
                var dv = (hq - sq).normalized; var a0 = sq + dv * 6f; var b0 = hq - dv * 4f;
                p.lineCap = LineCap.Round;
                p.strokeColor = new Color(0.09f, 0.188f, 0.184f, 0.6f * a); p.lineWidth = 5f; p.BeginPath(); p.MoveTo(a0); p.LineTo(b0); p.Stroke();
                p.strokeColor = new Color(1f, 0.86f, 0.55f, a); p.lineWidth = 2.6f; p.BeginPath(); p.MoveTo(a0); p.LineTo(b0); p.Stroke();
                p.fillColor = new Color(1f, 0.93f, 0.75f, a); p.BeginPath(); p.Arc(a0, 4f * a + 1f, new Angle(0), new Angle(360, AngleUnit.Degree)); p.Fill(); // the muzzle
            }
            else if (host.FailCulpritAt is Vector3 cw && Proj(cw, out var cq))
            {
                // the watcher's line to whom it saw: a thin static dotted line (Glyph 022: a heavy red line read as a shot)
                var dv = (cq - oq).normalized; float te = 1f / Mathf.Sqrt(dv.x * dv.x / (rx * rx) + dv.y * dv.y / (ry * ry)); // the ring's edge along the line (Glyph r2)
                var a0 = oq + dv * (te + 3f); var b = cq + (oq - cq).normalized * 15f; float len = Vector2.Distance(a0, b); if (len < 4f) return; var dir = (b - a0) / len;
                for (int pass = 0; pass < 2; pass++) { p.fillColor = pass == 0 ? new Color(0.09f, 0.188f, 0.184f, 0.55f) : new Color(0.878f, 0.227f, 0.184f, 0.9f); float r0 = pass == 0 ? 1.8f : 1.05f; for (float d = 0; d <= len; d += 6f) { var c = a0 + dir * d; p.BeginPath(); p.MoveTo(c + new Vector2(r0, 0)); p.Arc(c, r0, new Angle(0), new Angle(360, AngleUnit.Degree)); p.Fill(); } }
            }
        }
        int historyKey = -1; Label briefFootLine; ScrollView historySv; bool historyPinned = true, historyWasOpen, historySnapped = true;
        const float HistoryMaxH = 420f; float historyFitH = HistoryMaxH, historyLastOff, historyStillAt;
        VisualElement historyFade, historyNew; int historyTopId = -1, historyNewCount; string historyFilter = ""; float historyKeepH = -2f, historyKeepOff;
        /// <summary>A public view of the history's scroll for shots and checks: the offset (px from the newest at the top) and its limit.</summary>
        public (float offset, float max, int fresh) HistoryScroll => historySv == null ? (0f, 0f, 0) : (historySv.scrollOffset.y, Mathf.Max(0f, historyRows.layout.height - historySv.contentViewport.layout.height), historyNewCount);
        public void HistoryScrollTo(float y) { if (historySv != null) { historySv.scrollOffset = new Vector2(0f, y); historyPinned = y <= 4f; } }

        /// <summary>The HUD theme (HudTheme.tss) carries only the kit's sheet, not Unity's default one, so a ScrollView has none of its layout
        /// rules: the content shrank to the viewport (nothing to scroll) and the bar sat under the list. These are the defaults it needs.</summary>
        static void ScrollBasics(ScrollView sv)
        {
            if (sv.Q(className: "unity-scroll-view__content-and-vertical-scroll-container") is VisualElement row) { row.style.flexDirection = FlexDirection.Row; row.style.flexGrow = 1; row.style.flexShrink = 1; }
            sv.contentViewport.style.flexGrow = 1; sv.contentViewport.style.flexShrink = 1; sv.contentViewport.style.overflow = Overflow.Hidden;
            sv.contentContainer.style.flexShrink = 0; sv.contentContainer.style.flexGrow = 0;
            var sc = sv.verticalScroller; sc.style.flexShrink = 0; sc.style.flexDirection = FlexDirection.Column;
            var sl = sc.slider; sl.style.flexGrow = 1; sl.style.flexDirection = FlexDirection.Column;
            if (sl.Q(className: "unity-base-slider__input") is VisualElement inp) { inp.style.flexGrow = 1; inp.style.flexDirection = FlexDirection.Column; }
            if (sl.Q(className: "unity-base-slider__drag-container") is VisualElement dc) { dc.style.flexGrow = 1; dc.style.position = Position.Relative; }
            else if (sl.Q("unity-drag-container") is VisualElement dc2) { dc2.style.flexGrow = 1; dc2.style.position = Position.Relative; }
            if (sl.Q("unity-tracker") is VisualElement tr) { tr.style.position = Position.Absolute; tr.style.top = 0; tr.style.bottom = 0; }
            if (sl.Q("unity-dragger") is VisualElement dg) dg.style.position = Position.Absolute;
            if (sl.Q("unity-dragger-border") is VisualElement db) db.style.position = Position.Absolute;
        }

        /// <summary>The panel's thin scrollbar: no arrow buttons, a 4 px cream thumb on a faint track, 6 px wide.</summary>
        static void ThinScroller(Scroller sc)
        {
            sc.style.width = 6; sc.style.minWidth = 6; sc.style.marginLeft = 4; sc.style.borderLeftWidth = 0; sc.style.backgroundColor = Color.clear;
            Show(sc.lowButton, false); Show(sc.highButton, false);
            var sl = sc.slider; sl.style.marginTop = sl.style.marginBottom = 0; sl.style.width = 6; sl.style.minWidth = 6;
            if (sl.Q("unity-tracker") is VisualElement tr) { tr.style.backgroundColor = new Color(0.824f, 0.902f, 0.886f, 0.12f); tr.style.borderTopWidth = tr.style.borderBottomWidth = tr.style.borderLeftWidth = tr.style.borderRightWidth = 0; tr.style.left = 1; tr.style.right = 1; tr.style.borderTopLeftRadius = tr.style.borderTopRightRadius = tr.style.borderBottomLeftRadius = tr.style.borderBottomRightRadius = 2; }
            var dg = sl.Q("unity-dragger"); if (dg == null) return; dg.style.backgroundColor = new Color(Cream.r, Cream.g, Cream.b, 0.7f); dg.style.width = 4; dg.style.left = 1; dg.style.borderTopWidth = dg.style.borderBottomWidth = dg.style.borderLeftWidth = dg.style.borderRightWidth = 0;
            dg.style.borderTopLeftRadius = dg.style.borderTopRightRadius = dg.style.borderBottomLeftRadius = dg.style.borderBottomRightRadius = 2;
            if (sl.Q("unity-dragger-border") is VisualElement db) Show(db, false);
        }

        static Texture2D fadeTex;
        /// <summary>The bottom-edge fade: the dark panel's own colour from clear to solid, top to bottom.</summary>
        static Texture2D FadeTexture()
        {
            if (fadeTex) return fadeTex;
            fadeTex = new Texture2D(1, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 32; y++) { float a = 1f - y / 31f; fadeTex.SetPixel(0, y, new Color(11 / 255f, 57 / 255f, 55 / 255f, a * a * (3f - 2f * a))); } // row 0 is the bottom
            fadeTex.Apply(); return fadeTex;
        }
        /// <summary>Test hook for the layout audit's feed check: rebuilds the rows as they were before the fix (rows that shrink to a fixed list
        /// height), so the audit can be seen to fail on overlapping rows.</summary>
        public static bool FeedBadLayout;
        void Step3Frame()
        {
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) || size.x <= 0 ? 1920 : size.x;
            // the hint: the plate for its first 6 s, then the "?" chip (hover opens it, a click pins it); its ring only while the plate is open
            if (m.Hint != hintSeen) { hintSeen = m.Hint; hintAt = Time.unscaledTime; }
            bool hintOpen = m.Hint != null && (Time.unscaledTime - hintAt < HintOpenS || hintHover || HintPinned);
            Show(hint, hintOpen); if (hintOpen) { Text(hintBody, m.Hint); Text(hintKey, m.HintKey); }
            Show(hintChip, m.Hint != null && !hintOpen); Text(hintChipKey, m.HintKey);
            Show(toast, m.Toast != null); if (m.Toast != null) { Text(toastText, m.Toast); toast.style.top = hintOpen ? 12 + hint.resolvedStyle.height + 8 : m.Hint != null ? 56 : 12; }
            // the chip at the cursor
            bool chipOn = m.Chip != HudModel.ChipKind.None; Show(chip, chipOn);
            if (chipOn)
            {
                bool price = m.Chip == HudModel.ChipKind.Price;
                Show(chipIcon, price); Show(chipPrice, price); Show(chipMark, price); Show(chipText, !price);
                if (price) { Icon(chipIcon, m.ChipIcon, new Color(1f, 0.988f, 0.925f)); Text(chipUsd, m.ChipUsd); Text(chipTime, m.ChipTime); Text(chipTrail, m.ChipTrail); Text(chipAlert, m.ChipAlert); Bg(chipMark, $"badges/mark_{m.ChipMark}"); Show(chipSeenIcon, m.ChipSeen.Length > 0); Show(chipSeen, m.ChipSeen.Length > 0); Text(chipSeen, m.ChipSeen); }
                else { Text(chipText, m.ChipText); chipText.style.color = m.Chip == HudModel.ChipKind.AimWarn ? new Color(1f, 0.75f, 0.68f) : Cream; }
                var p = RuntimePanelUtils.ScreenToPanel(root.panel, m.Mouse);
                float cw = chip.resolvedStyle.width; if (float.IsNaN(cw)) cw = 300;
                chip.style.left = p.x + 14 + cw > W - 14 ? p.x - cw - 14 : p.x + 14; chip.style.top = p.y - 48;
            }
            // "?" over each watcher noticing an agent whose sight the cell doesn't know (Game.Detection, Known false)
            var view = host.View;
            while (unknownMarks.Count < m.Unknowns.Count) { var u = E(root, "s-disc_dark_30", 0, 0, 30, 30); Icon(E(u, null, 6, 6, 18, 18), "p_unknown", Cream); unknownMarks.Add(u); }
            for (int i = 0; i < unknownMarks.Count; i++)
            {
                bool on = i < m.Unknowns.Count && view; Vector3 sp = default;
                if (on) { sp = view.WorldToScreenPoint(m.Unknowns[i]); on = sp.z > 0; }
                Show(unknownMarks[i], on); if (!on) continue;
                var pp = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(sp.x, Screen.height - sp.y));
                unknownMarks[i].style.left = pp.x - 15; unknownMarks[i].style.top = pp.y - 15;
            }
            // the going-loud card
            Show(loud, m.LoudOpen);
            if (m.LoudOpen)
            {
                var lines = m.LoudLines; Text(loudSub, "The first shot cannot be undone.");
                int rows = Mathf.Max(0, lines.Count - 2);
                for (int i = 0; i < loudRows.Count; i++) { Show(loudRows[i].parent, i < rows); if (i < rows) Text(loudRows[i], lines[i + 1]); }
                Text(loudHold, lines.Count > 0 ? lines[lines.Count - 1] : "");
                int on = Mathf.RoundToInt(m.LoudProgress * 24); for (int k = 0; k < loudDots.Count; k++) loudDots[k].style.backgroundColor = k < on ? TurqLight : new Color(1, 1, 1, 0.15f);
                var p = RuntimePanelUtils.ScreenToPanel(root.panel, m.LoudAt); float lh2 = loud.resolvedStyle.height; if (float.IsNaN(lh2)) lh2 = 268;
                loud.style.left = Mathf.Clamp(p.x + 24, 14, W - 430 - 14); loud.style.top = Mathf.Max(14, p.y - lh2 - 16);
            }
            // the briefing
            Show(brief, m.BriefingOpen && m.BriefTitle.Length > 0);
            if (m.BriefingOpen && m.BriefTitle.Length > 0)
            {
                brief.style.left = (W - 700) / 2 + 40;
                Text(briefTitle, m.BriefTitle); Text(briefSub, m.BriefSub); Text(briefObjective, m.BriefObjective); Text(briefTarget, m.BriefSituation);
                Text(briefWin, m.BriefWin); Text(briefLose, string.Join(" · ", m.BriefFails));
                Text(briefClose, m.BriefClock.Contains("8:30") ? "Closes 8:30" : m.BriefClock); Text(briefPurse, m.Cash.Length > 0 ? "Purse " + m.Cash : m.BriefPurse); Text(briefAlert, $"Alert {m.Alert} of 3"); Text(briefArms, "Cell armed");
                for (int i = 0; i < 2; i++) { bool has = i < m.BriefWays.Count; Show(briefWays[i].title.parent, has); if (has) { Text(briefWays[i].title, $"Approach {(char)('A' + i)}: {m.BriefWays[i].title}"); Text(briefWays[i].body, m.BriefWays[i].body); } }
                Text(briefBeginKey, m.BeginKey);
            }
            // the end card
            Show(end, m.Ended);
            if (m.Ended)
            {
                end.style.left = (W - 560) / 2 + 40;
                Text(endTitle, m.EndLines.Count > 0 ? m.EndLines[0] : ""); Text(endLine, m.EndLines.Count > 1 ? m.EndLines[1] : ""); Text(endReplayKey, m.RestartKey);
                Show(endDetail, endDetailOpen);
                for (int i = 0; i < endLines.Count; i++)
                {
                    int li = i + 2; bool has = endDetailOpen && li < m.EndLines.Count; var row = endLines[i].parent; Show(row, has); if (!has) continue;
                    var ln = m.EndLines[li]; string lead = null;
                    if (ln.StartsWith("\u0001")) { int e2 = ln.IndexOf('\u0001', 1); lead = ln.Substring(1, e2 - 1); ln = ln[(e2 + 1)..]; }
                    Show(row.Q("cm"), lead != null); Show(row.Q("fg"), lead != null); Text(endLines[i], ln);
                    if (lead != null) { var (who, fate) = (lead.Split(':')[0], lead.Split(':')[1]); bool cap = fate == "captured"; Bg(row.Q("cm"), $"portraits/cameo_{who}_normal", Color.white); Border(row.Q("cm"), 1.2f, cap ? Red : Cream); Show(row.Q("cf"), cap); if (cap) row.Q("fg").style.backgroundImage = StyleKeyword.Null; else Icon(row.Q("fg"), "s_moving", Cream); }
                }
            }
            else endDetailOpen = false;
            // the history (Sky 2026-10-18: "always opened showing latest message, but then can expand up for history"): collapsed it shows the
            // newest entry only (its title and its newest spoken line), anchored at its bottom edge; expanded (L, a click on it, or the papers
            // key) it grows upward: the header and filters fixed, every entry with the newest at the top, held at the newest unless scrolled down
            bool expanded = m.HistoryOpen, collapsedOn = !expanded && !m.Ended && !m.BriefingOpen && m.History.Count > 0;
            Show(history, expanded || collapsedOn);
            if (expanded && !historyWasOpen) historyPinned = true; historyWasOpen = expanded;
            if (expanded || collapsedOn)
            {
                Show(history.Q("head"), expanded); Show(history.Q("filters"), expanded); history.Q("list").style.marginTop = expanded ? 6 : 0;
                history.style.paddingTop = expanded ? 14 : 14; history.style.paddingBottom = expanded ? 12 : 14; // collapsed: the text clear of the frame's corner screws
                foreach (var f in filterChips) { bool on = f.name == m.Filter; Class(f, "s-plate_light", on); Class(f, "s-plate_dark", !on); f.Q<Label>().style.color = on ? Ink : Cream; } // ink on the light plate, cream on the dark
                int key = m.History.Count * 31 + m.Filter.GetHashCode() + m.Unread * 7 + openRows.Count * 131 + (expanded ? 7919 : 0) + (FeedBadLayout ? 104729 : 0) + (m.History.Count > 0 ? m.History[0].card.Id * 13 + m.History[0].card.Lines.Count : 0);
                if (key != historyKey)
                {
                    float oldH = historyRows.layout.height, oldOff = historySv.scrollOffset.y;
                    historyKey = key; historyRows.Clear();
                    // Sky: "Runner seen" three times for one agent: repeats of the same title and result within 15 s merge into one row, "×n"
                    var merged = new List<(HandoverHost.FeedCard card, bool unread, int n)>();
                    foreach (var (_, card, unread) in m.History)
                    {
                        if (card == null) continue;
                        if (merged.Count > 0 && merged[^1].card.Title == card.Title && merged[^1].card.Footer == card.Footer && card.Lines.Count == 0 && merged[^1].card.Lines.Count == 0 && Mathf.Abs(merged[^1].card.Started - card.Started) <= 15 * Suborned.Sim.Game.Hz)
                        { var mm = merged[^1]; merged[^1] = (mm.card, mm.unread || unread, mm.n + 1); continue; }
                        merged.Add((card, unread, 1));
                    }
                    // newest at the top, in every filter (the list arrives newest first; Sky 2026-10-18)
                    feedTitle = null; feedSpeech = null;
                    if (!expanded && merged.Count > 1) merged.RemoveRange(1, merged.Count - 1); // collapsed: the newest only
                    int topId = merged.Count > 0 ? merged[0].card.Id : -1;
                    if (!expanded || m.Filter != historyFilter || historyTopId < 0) { historyPinned = true; historyNewCount = 0; historyKeepH = -2f; } // a new filter (or opening it) starts at the newest
                    else if (!historyPinned)
                    {
                        if (topId != historyTopId) { int at = merged.FindIndex(x => x.card.Id == historyTopId); historyNewCount = at < 0 ? merged.Count : at; historyKeepH = oldH; historyKeepOff = oldOff; } // new rows above: keep the reader's place
                        else { historyKeepH = -1f; historyKeepOff = oldOff; } // the same rows (a row opened or closed): the same offset
                    }
                    if (historyPinned || !expanded) historyTopId = topId;
                    historyFilter = m.Filter;
                    historySv.style.maxHeight = expanded ? historyFitH : StyleKeyword.None; // whole rows (the frame below sets it from the rows)
                    historySv.style.height = FeedBadLayout ? 120 : StyleKeyword.Auto; // the audit's bad case: a fixed list height the rows shrink into
                    foreach (var (card, unread, count) in merged)
                    {
                        var r = E(historyRows, null, pick: true); r.name = "feed-row"; r.style.flexDirection = FlexDirection.Row; r.style.minHeight = expanded ? 40 : 0; r.style.marginBottom = expanded ? 6 : 0; r.style.alignItems = Align.FlexStart;
                        r.style.flexShrink = FeedBadLayout ? 1 : 0; // Sky's overlap: a row shrank below its wrapped lines, which drew into the next row; it is as tall as its lines
                        var bar = E(r, null, w: 3, h: 34); bar.style.backgroundColor = Accent(card.Kind); bar.style.marginRight = 8; bar.style.flexShrink = 0;
                        // Glyph 022: a row that is one person speaking (its title is the speaker) is one line, "Passer-by: Stay inside.", under a
                        // speech-bubble glyph; no empty title line above it, and no stopwatch
                        int sl0 = expanded ? 0 : card.Lines.Count - 1;
                        bool speech = count == 1 && card.Lines.Count > 0 && card.Verb.Length == 0 && string.Equals(card.Title.Trim(), card.Lines[sl0].Name.Trim(), System.StringComparison.OrdinalIgnoreCase);
                        var disc = E(r, "s-disc_light_24", w: 21, h: 21); disc.style.flexShrink = 0; disc.style.marginTop = 2;
                        if (speech && expanded) { r.style.minHeight = 26; bar.style.height = 22; } // one line: the row as tall as its line
                        if (speech) SpeechGlyph(E(disc, null, 4f, 4.5f, 13, 12), Ink);
                        else Icon(E(disc, null, 4.5f, 4.5f, 12, 12), card.Verb.Length > 0 ? HudModel.AtlasIcon(card.Verb, card.Verb, "") : KindGlyph.TryGetValue(card.Kind, out var kg) ? kg : "dot", Ink); // the verb's own glyph (Reroute is not Listen's ear), else the kind's
                        var col = E(r); col.style.flexGrow = 1; col.style.flexShrink = 1; col.style.marginLeft = 8;
                        var top = E(col); top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.Center;
                        var t = L(top, "history-row-title"); t.text = count > 1 ? $"{card.Title} ×{count}" : card.Title; t.style.flexShrink = 0;
                        if (speech)
                        {   // the speaker's line takes the title's place (Glyph: "Guard" over "Guard: He's armed…" said it twice; an empty title left a blank line)
                            var ln0 = card.Lines[sl0]; t.text = $"{ln0.Name}: {ln0.Text}"; Class(t, "t-history-row-title", false); Class(t, "t-history-row-result", true); BodyText(t);
                            t.style.flexShrink = 1; t.style.flexGrow = 1; t.style.whiteSpace = expanded ? WhiteSpace.Normal : WhiteSpace.NoWrap; if (!expanded) t.style.textOverflow = TextOverflow.Ellipsis; t.style.overflow = Overflow.Hidden; t.style.marginRight = 6;
                        }
                        var wh = L(top, "feed-where", ellipsis: true); wh.text = speech ? "" : card.Where; wh.style.marginLeft = 6; wh.style.flexGrow = speech ? 0 : 1; wh.style.flexShrink = 1;
                        if (unread) { var dot = E(top, "s-dot_unread", w: 10, h: 10); dot.style.marginRight = 6; dot.style.flexShrink = 0; } // unread: a dot, no button
                        var ck = L(top, "feed-clock"); ck.text = HandoverHost.MissionClock(card.Started); ck.style.flexShrink = 0;
                        if (!expanded) // collapsed: the title and the newest spoken line (one line, the budget's speech), "+n" for the rest; the full entry on expand
                        {
                            wh.text = ""; bar.style.height = 22;
                            if (speech) { feedSpeech = t; feedTitle = null; } // one line: the speaker and their words
                            else
                            {
                                if (card.Lines.Count > 0) { var ln = card.Lines[card.Lines.Count - 1]; var sl = L(col, "history-row-result", ellipsis: true); sl.name = "speech"; sl.text = $"{ln.Name}: {ln.Text}"; sl.style.whiteSpace = WhiteSpace.NoWrap; BodyText(sl); sl.style.marginTop = 2; sl.style.flexShrink = 0; feedSpeech = sl; } else feedSpeech = null;
                                feedTitle = t;
                            }
                            if (card.Lines.Count > 1) { var more = L(top, "feed-clock"); more.text = $"+{card.Lines.Count - 1}"; more.style.marginRight = 8; more.style.flexShrink = 0; more.PlaceBehind(ck); }
                            continue;
                        }
                        bool open = openRows.Contains(card.Id) || card.Footer.Length == 0;
                        if (card.Footer.Length > 0) { var res = L(col, "history-row-result"); res.text = card.Footer; BodyText(res); res.style.whiteSpace = WhiteSpace.Normal; res.style.maxHeight = open ? new StyleLength(StyleKeyword.None) : new StyleLength(40); res.style.overflow = Overflow.Hidden; res.style.flexShrink = FeedBadLayout ? 1 : 0; }
                        if (open)
                            for (int li = speech ? 1 : 0; li < card.Lines.Count; li++)
                            { var ln = card.Lines[li]; var l = L(col, "history-row-result"); l.text = $"{ln.Name}: {ln.Text}"; BodyText(l); l.style.whiteSpace = WhiteSpace.Normal; l.style.marginTop = 2; l.style.flexShrink = FeedBadLayout ? 1 : 0; }
                        // a click pans to its place and opens (or closes) the row to the card's lines
                        var cc = card; r.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0) return; if (!openRows.Remove(cc.Id)) openRows.Add(cc.Id); if (host.PlaceNow(cc) is Vector3 at) host.LookAt(at); e.StopPropagation(); });
                    }
                    // the budget's manifest: collapsed, the title is a beat (the newest event, a few words) and its line is speech
                    manifest.RemoveAll(x => x.e == feedTitleReg || x.e == feedSpeechReg); feedTitleReg = feedSpeechReg = null;
                    if (!expanded && feedTitle != null) { var ft = feedTitle; feedTitleReg = ft; manifest.Add((ft, "beat", () => ft.text)); }
                    if (!expanded && feedSpeech != null) { var fs = feedSpeech; feedSpeechReg = fs; manifest.Add((fs, "speech", () => fs.text)); }
                }
                // expanded: at the newest (the top) unless the player has scrolled down; their place is kept when rows arrive above it
                if (expanded && historySv != null)
                {
                    float h = historyRows.layout.height, max = Mathf.Max(0f, h - historySv.contentViewport.layout.height);
                    if (historyKeepH > -2f)
                    {
                        if (!float.IsNaN(h) && (historyKeepH < 0f || Mathf.Abs(h - historyKeepH) > 0.5f)) { historySv.scrollOffset = new Vector2(0f, historyKeepOff + (historyKeepH < 0f ? 0f : h - historyKeepH)); historyKeepH = -2f; }
                    }
                    else
                    {
                        bool atTop = historySv.scrollOffset.y <= 4f;
                        if (atTop && !historyPinned) { historyPinned = true; historyNewCount = 0; historyTopId = -1; historyKey = -1; } // back at the top: the newest, and rebuild to take the top id
                        else if (!atTop && historyPinned) historyPinned = false;
                    }
                    if (historyPinned && historySv.scrollOffset.y > 0.5f && historyKeepH <= -2f && historySv.scrollOffset.y <= 4f) historySv.scrollOffset = Vector2.zero;
                    Show(historyNew, !historyPinned && historyNewCount > 0); if (historyNewCount > 0) historyNew.Q<Label>("n").text = $"↑ {historyNewCount} new";
                    // Sky 2026-10-18 ("Reroute patrol 4:02" cut in half): the list is as tall as the whole rows that fit in 420 px from the top, so
                    // at the newest no row ends cut; scrolled, it comes to rest on a row's top edge, and a row the bottom edge still crosses fades
                    float fit = 0f; bool laid = true;
                    foreach (var c in historyRows.Children()) { float rh = c.layout.height + c.resolvedStyle.marginBottom; if (float.IsNaN(rh)) { laid = false; break; } if (fit > 0f && fit + rh > HistoryMaxH) break; fit += rh; }
                    if (laid && fit > 0f) { fit = Mathf.Min(Mathf.Ceil(fit), HistoryMaxH); if (Mathf.Abs(fit - historyFitH) > 0.5f) { historyFitH = fit; historySv.style.maxHeight = fit; } }
                    float off = historySv.scrollOffset.y, now = Time.unscaledTime;
                    if (Mathf.Abs(off - historyLastOff) > 0.1f) { historyLastOff = off; historyStillAt = now; historySnapped = false; }
                    else if (!historySnapped && now - historyStillAt > 0.15f && !float.IsNaN(max) && off > 4f && off < max - 1f)
                    {
                        historySnapped = true; float best = off, bd = float.MaxValue;
                        foreach (var c in historyRows.Children()) { float y0 = c.layout.yMin; if (Mathf.Abs(y0 - off) < bd) { bd = Mathf.Abs(y0 - off); best = y0; } }
                        best = Mathf.Min(best, max); if (Mathf.Abs(best - off) > 0.5f) { historySv.scrollOffset = new Vector2(0f, best); historyLastOff = best; }
                    }
                    float vb = historySv.scrollOffset.y + historySv.contentViewport.layout.height; bool cut = false;
                    foreach (var c in historyRows.Children()) { var l = c.layout; if (l.yMin < vb - 1f && l.yMax > vb + 1f) { cut = true; break; } }
                    Show(historyFade, cut); // a row crossing the bottom edge fades out, never ends in a sliced line
                }
                else { Show(historyFade, false); Show(historyNew, false); }
            }
        }
        Label feedTitle, feedSpeech; VisualElement feedTitleReg, feedSpeechReg;

        /// <summary>Glyph 022: the history's body text at 13.5 px in the light sea-cream (#c8e0da, about 8:1 on the panel) instead of 12.5 px muted
        /// teal (about 4:1).</summary>
        static void BodyText(Label l) { l.style.fontSize = 13.5f; l.style.color = new Color(0.784f, 0.878f, 0.855f); l.style.opacity = 1f; }

        /// <summary>A speech bubble drawn in the glyph's box (the atlas has none without a ? or !): a rounded bubble, its tail at the lower left.</summary>
        static void SpeechGlyph(VisualElement e, Color c)
        {
            e.generateVisualContent += ctx =>
            {
                var p = ctx.painter2D; var r = e.contentRect; float w = r.width, h = r.height, bh = h * 0.74f, rad = bh * 0.45f;
                p.fillColor = c; p.BeginPath();
                p.MoveTo(new Vector2(rad, 0)); p.LineTo(new Vector2(w - rad, 0)); p.ArcTo(new Vector2(w, 0), new Vector2(w, rad), rad); p.LineTo(new Vector2(w, bh - rad)); p.ArcTo(new Vector2(w, bh), new Vector2(w - rad, bh), rad);
                p.LineTo(new Vector2(w * 0.42f, bh)); p.LineTo(new Vector2(w * 0.16f, h)); p.LineTo(new Vector2(w * 0.24f, bh)); p.LineTo(new Vector2(rad, bh)); p.ArcTo(new Vector2(0, bh), new Vector2(0, bh - rad), rad); p.LineTo(new Vector2(0, rad)); p.ArcTo(new Vector2(0, 0), new Vector2(rad, 0), rad);
                p.ClosePath(); p.Fill();
            };
            e.MarkDirtyRepaint();
        }

        static Color Accent(string kind) => kind switch
        {
            "learned" => new Color(0.396f, 0.698f, 0.678f), "approach" => new Color(0.416f, 0.573f, 0.557f), "call" => new Color(0.463f, 0.651f, 0.667f),
            "city" => new Color(0.651f, 0.714f, 0.667f), "warning" => new Color(0.431f, 0.478f, 0.486f), _ => new Color(0.329f, 0.376f, 0.38f),
        };

        void Sep(VisualElement parent) { var s = E(parent, null, w: 1, h: 18); s.style.backgroundColor = new Color(0.824f, 0.902f, 0.886f, 0.35f); s.style.marginLeft = 14; s.style.marginRight = 14; }
        static void Round(VisualElement e, float r) { e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r; }
        static void Border(VisualElement e, float w, Color c) { e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = w; e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = c; }

        Card NewCard(VisualElement parent)
        {
            var c = new Card();
            c.Root = E(parent, "s-panel_card", pick: true); c.Root.style.width = 384; c.Root.style.marginBottom = 6; c.Root.style.paddingBottom = 6;
            c.Root.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && c.For != null) { host.Acknowledge(c.For, true); e.StopPropagation(); } });
            c.Root.RegisterCallback<PointerEnterEvent>(_ => hoverCard = c.For); c.Root.RegisterCallback<PointerLeaveEvent>(_ => { if (hoverCard == c.For) hoverCard = null; });
            c.Header = E(c.Root); c.Header.style.height = 25; c.Header.style.marginLeft = 3.5f; c.Header.style.marginRight = 3.5f; c.Header.style.marginTop = 3.5f; Round(c.Header, 7);
            c.KindIcon = E(c.Header, "s-disc_light_24", 6.5f, 2, 21, 21); var ki = E(c.KindIcon, null, 4.5f, 4.5f, 12, 12); ki.name = "glyph";
            c.VerbIcon = E(c.Header, null, 30.5f, 6.5f, 15, 15);
            c.Title = L(c.Header, "feed-title", 46.5f, 5.5f, 150, 16); c.Where = L(c.Header, "feed-where", 0, 5.5f, 100, 16, true);
            c.New = E(c.Header, "s-pill_new", 256.5f, 5.5f, 34, 15); var nl = L(c.New, "card-tag"); nl.text = "NEW"; nl.style.flexGrow = 1; nl.style.unityTextAlign = TextAnchor.MiddleCenter; nl.style.color = Ink;
            c.Clock = L(c.Header, "feed-clock", 296.5f, 5.5f, 72, 16); c.Clock.style.unityTextAlign = TextAnchor.MiddleRight;
            c.Dot = E(c.Root, "s-dot_unread", 372, 2, 12, 12);
            c.Prog = E(c.Root); c.Prog.style.height = 12; c.Prog.style.marginLeft = 14; c.Prog.style.marginTop = 6; c.Prog.style.flexDirection = FlexDirection.Row; c.Prog.style.alignItems = Align.Center;
            var track = E(c.Prog); track.style.width = 284; track.style.height = 8; track.style.backgroundColor = new Color(0.82f, 0.85f, 0.83f); Round(track, 4);
            c.Fill = E(track, null, 0, 0, 0, 8); c.Fill.style.backgroundColor = DeepTeal; Round(c.Fill, 4);
            c.Remaining = L(c.Prog, "feed-live"); c.Remaining.style.marginLeft = 8;
            c.Lines = E(c.Root); c.Lines.style.marginTop = 6; c.Lines.style.marginLeft = 14; c.Lines.style.marginRight = 14;
            for (int i = 0; i < 6; i++)
            {
                var row = E(c.Lines); row.style.flexDirection = FlexDirection.Row; row.style.marginBottom = 5; row.style.minHeight = 22;
                var chip = E(row, null, w: 22, h: 22); chip.style.flexShrink = 0; chip.style.marginRight = 8;
                var col = E(row); col.style.flexDirection = FlexDirection.Row; col.style.flexWrap = Wrap.Wrap; col.style.width = 300;
                var name = L(col, "feed-name"); name.style.marginRight = 6;
                var text = L(col, "feed-line"); text.style.whiteSpace = WhiteSpace.Normal; text.style.maxWidth = 286;
                c.Rows.Add((row, chip, name, text));
            }
            c.Typing = L(c.Lines, "feed-live"); c.Typing.text = "• • •";
            c.Footer = E(c.Root); c.Footer.style.height = 30; c.Footer.style.marginLeft = 8; c.Footer.style.marginRight = 8; c.Footer.style.marginTop = 4; Round(c.Footer, 7);
            c.Footer.style.flexDirection = FlexDirection.Row; c.Footer.style.alignItems = Align.Center; c.Footer.style.paddingLeft = 8;
            c.Check = E(c.Footer, null, w: 16, h: 16); Round(c.Check, 8); c.Check.style.backgroundColor = DeepTeal; c.Check.style.flexShrink = 0; Icon(E(c.Check, null, 3, 3, 10, 10), "check", Cream);
            c.Result = L(c.Footer, "feed-result", ellipsis: true); c.Result.style.marginLeft = 8; c.Result.style.flexGrow = 1; c.Result.style.flexShrink = 1;
            c.Ack = E(c.Footer, "s-btn_ack_normal btn_ack", pick: true); c.Ack.style.width = 61; c.Ack.style.height = 24; c.Ack.style.marginRight = 4; c.Ack.style.flexShrink = 0;
            c.Ack.style.flexDirection = FlexDirection.Row; c.Ack.style.alignItems = Align.Center; c.Ack.style.justifyContent = Justify.Center;
            c.Ack.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && c.For != null) { host.Acknowledge(c.For, false); e.StopPropagation(); } });
            Icon(E(c.Ack, null, w: 12, h: 12), "check", DeepTeal);
            var kp = E(c.Ack, "s-plate_light"); kp.style.height = 16; kp.style.marginLeft = 5; c.AckKey = L(kp, "keycap"); c.AckKey.style.paddingLeft = 5; c.AckKey.style.paddingRight = 5;
            return c;
        }

        // ---------------------------------------------------------------- per frame

        int tipFor = -1;
        void TipFor(int k, bool over) { if (over) tipFor = k; else if (tipFor == k) tipFor = -1; }

        void LateUpdate()
        {
            if (root == null) return;
            bool want = host.Game != null && host.Fault == null && host.controls.view == HudView.Kit;
            if (want != on) { on = want; Show(root, on); }
            if (!on) return;
            if (FailCamHud()) return; // the fail camera: its caption alone
            m.Frame(host); CrispScale();
            // a folded hint rings nothing (Glyph #8: the ring only while the plate is open)
            if (m.Hint != null && !(Time.unscaledTime - hintAt < HintOpenS || hintHover || HintPinned) && m.Hint == hintSeen) { m.HintRing = null; for (int i = 0; i < 4; i++) m.Slots[i].HintRing = false; }
            // the pill
            Text(clock, Stress ? "8:30" : m.Clock); Text(nextLabel, m.NextLabel + " · "); Text(nextTime, Stress ? "8:30" : m.NextTime); Text(cash, m.Cash); Text(trail, Stress ? "12" : m.Trail.Replace("T ", ""));
            for (int i = 0; i < 3; i++) lamps[i].style.backgroundColor = i < m.Alert ? new Color(1f, 0.988f, 0.94f) : new Color(0.039f, 0.11f, 0.118f);
            Text(state, m.State.Contains("PAUSE") ? "PAUSED" : m.State);
            Text(objText, m.Objective); Text(objTail, m.ObjectiveTail.Replace(' ', '\u00A0')); // UI Toolkit trims a label's leading spaces: "car· closes"
            // the objective never changes in a mission: up for 20 s after the briefing closes (B brings it back), or while the pill is hovered, or pinned
            if (briefWas && !m.BriefingOpen) objUntil = Time.unscaledTime + ObjectiveS; briefWas = m.BriefingOpen;
            bool objOn = m.BriefingOpen || ObjPinned || pillHover || objHover || Time.unscaledTime < objUntil;
            Show(objective, objOn); intel.style.top = objOn ? 108 : 62;
            // Intel: the strip, and when open a row for each person the cell knows something of (a secret and a lover tie only once known)
            Text(intelCount, $"{m.IntelKnown} people known"); Icon(intelChevron, m.IntelOpen ? "chev_up" : "chev_down", Cream);
            Show(intelOpen, m.IntelOpen);
            if (m.IntelOpen)
                for (int i = 0; i < 4; i++)
                {
                    var r = m.Intel[i]; var u = intelRows[i]; Show(u.Row, r.Known); if (!r.Known) continue;
                    Text(u.Name, r.Name); Text(u.Job, r.Job);
                    Icon(u.NeedGlyph, r.NeedIcon, Cream); Bg(u.NeedMark, $"badges/mark_{r.NeedMark}");
                    Icon(u.TempGlyph, r.TempIcon, Cream);
                    Show(u.Turned, r.Turned); if (r.Turned) Bg(u.Turned, "badges/mark_ok");
                    Icon(u.NextIcon, r.NextIcon, TurqLight); Text(u.Next, r.Next);
                    Show(u.Secret, r.Secret.Length > 0); Text(u.Secret, r.Secret);
                }
            // the roster (the dock: the left column, or the row in the corner or at the bottom) and the strips
            Dock();
            var wins = m.Dock == RosterDock.Left ? colWins : rowWins;
            bool any = false; for (int i = 0; i < 4; i++) any |= m.Slots[i].Selected;
            for (int i = 0; i < 4; i++)
            {
                var s = m.Slots[i]; var st = strips[i]; var w = wins[i];
                string look = s.Down ? "down" : s.Hurt ? "hurt" : !any || s.Selected ? "normal" : "dim";
                Bg(w.Portrait, $"portraits/portrait_{s.Role}_{look}");
                Show(w.RingSel, s.Selected); Show(w.RingBusy, s.Busy && !s.Selected);
                bool inCar = s.InCar, left = s.LeftFate.Length > 0; Show(w.Car, inCar || left);
                if (inCar) { w.Car.style.backgroundColor = TurqLight; Icon(w.Car.Q("g"), "car", Ink); Border(w.Car, 1.4f, Ink); } // Glyph r1: a solid cyan disc, a dark glyph (unlike the stance ring)
                bool capF = !inCar && left && s.LeftFate == "captured"; Show(w.Car.Q("cf"), capF); Show(w.Car.Q("g"), !capF);
                if (left && !inCar) { w.Car.style.backgroundColor = Ink; if (!capF) Icon(w.Car.Q("g"), "s_moving", Cream); Border(w.Car, 1.6f, s.LeftFate == "captured" ? Red : Cream); } // captured: a red lock on a red rim; walks out: footprints on a cream rim
                bool tipOn = left && (rosterHover == i || host.Peeking); Show(w.Rim, tipOn); LeftTip(i, w, tipOn ? s : null);
                Show(w.Arm, s.Armed && !s.Down); if (s.Armed && w.Arm.childCount > 0) w.Arm[0].style.unityBackgroundImageTintColor = s.Drawn ? TurqLight : Cream; Border(w.Arm, 1.6f, s.Drawn ? TurqLight : Color.clear); // drawn: turquoise, and a turquoise rim (the shape cue)
                Text(w.Num, s.Key); w.Num.parent.style.unityBackgroundImageTintColor = s.Selected ? TurqLight : Color.white; // selected: the key badge turns turquoise (no ring: see BuildRoster)
                Pips(w, s.Hp, s.HpMax);
                if (st.Root.style.display == DisplayStyle.None) continue;
                // compact (Glyph #8): a ready or downed agent is one 34 px line, "SCOUT · READY"; ` shows every strip full
                if (st.Compact != s.Compact)
                {
                    st.Compact = s.Compact; Class(st.Root, "s-panel_slot", !s.Compact); Class(st.Root, "s-panel_strip", s.Compact);
                    st.Root.style.height = s.Compact ? 34 : 92; st.Tags.style.top = s.Compact ? 9 : 7;
                }
                Class(st.Root, "selected", s.Selected);
                Border(st.Root, s.Selected ? 1.8f : 0f, s.Selected ? Turq : Color.clear); // selected: the turquoise inner line only
                Text(st.Role, s.Name.ToUpperInvariant()); Text(st.State, "·  " + s.StateTag);
                foreach (var cls in new[] { "t-slot-state-ready", "t-slot-state-busy", "t-slot-state-hurt", "t-slot-state-down" }) Class(st.State, cls, cls == "t-slot-state-" + (s.StateTag is "RUNNING" or "CARRYING" ? "busy" : s.StateTag.ToLowerInvariant()));
                Show(st.Stance, m.Fight && s.Armed); if (m.Fight && s.Armed) Bg(st.Stance, $"badges/stance_{(s.Stance == "H" ? "hold" : s.Stance == "A" ? "free" : "return")}");
                bool busy = s.Busy && !s.Down && !s.Compact;
                Show(st.Icon, busy); if (busy) Icon(st.Icon, s.Icon, Cream);
                Show(st.Action, busy); Text(st.Action, s.Label);
                Show(st.Bar, busy && s.Progress >= 0); st.Fill.style.width = 104 * Mathf.Clamp01(s.Progress / 1000f);
                Show(st.Time, busy && s.Left.Length > 0); Text(st.Time, s.Left);
                Show(st.Cancel, s.CanCancel && !s.Compact);
                Show(st.Queued, s.Queued.Length > 0 && !s.Compact); Show(st.QueuedIcon, s.Queued.Length > 0 && !s.Compact); Text(st.Queued, s.Queued);
                Show(st.Ready, !busy && !s.Compact); Text(st.Ready, s.Down ? "Down" : s.Hurt ? "Hurt, still fighting" : "Ready");
                Show(st.Hint, (!busy || s.Pace.Length > 0 && s.Progress < 0) && !s.Compact && s.Queued.Length == 0 && (s.HintLine.Length > 0 || s.Pace.Length > 0)); Text(st.Hint, s.Pace.Length > 0 ? s.Pace : s.HintLine); // carrying: the pace on line 2 (Glyph §4)
            }
            for (int i = 0; i < 4; i++)
            {
                Class(knobs[i], "selected", i == m.VerbLit && m.VerbHas[i]); Class(knobs[i], "disabled", !m.VerbHas[i]);
                Icon(knobIcons[i], m.VerbIcon[i], m.VerbHas[i] ? Cream : Grey); Text(knobNums[i], m.VerbKey[i]);
            }
            if (m.VerbLit >= 0 && m.VerbLit < 4) Text(armedName, m.VerbName[m.VerbLit]);
            // the cancel tooltip at the cursor
            Show(tooltip, tipFor >= 0);
            if (tipFor >= 0)
            {
                var s = m.Slots[tipFor]; Text(tipTitle, s.CancelTitle); Text(tipBody, s.CancelBody); Text(tipKey, m.CancelKey);
                var p = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(m.Mouse.x, m.Mouse.y)); tooltip.style.left = p.x + 14; tooltip.style.top = p.y + 14;
            }
            // the ticker: the cell's last word, about 4 s, then it fades
            {
                var t = m.Ticker; float a = t == null ? 0f : Mathf.Clamp01((5f - m.TickerAge) / 1f);
                Show(ticker, a > 0.01f); ticker.style.opacity = a;
                if (t != null && a > 0.01f) { Text(tickerText, t.Text); Bg(tickerChip, $"portraits/cameo_{t.Speaker}_normal"); }
            }
            Feed();
            PanelFrame(); Step3Frame(); TakedownFrame();
            LessFrame(); MenuFrame(); // brief 014: the less-reading HUD over the pieces above (it hides what is cut at rest)
            FollowMarks();
        }

        /// <summary>The test follow view (HostFollow): the world marks are sized for the orthographic camera (rings, pins, markers, the move ring,
        /// edge cameos), so they hide while it is on; speech and pops (screen-sized, anchored to a head or a click) stay. Visibility, not display:
        /// the HUD's own Show calls keep their state, and the overhead view gets every mark back as it was.</summary>
        void FollowMarks()
        {
            bool f = host.Following;
            // the chip by the plot scope while the follow view is on (Glyph r2): what is on, and its key (T leaves it)
            if (followChip == null)
            {
                followChip = E(root, "s-plate_dark", 0, 0, float.NaN, 30); followChip.style.top = StyleKeyword.Auto; followChip.style.left = 14 + 212 + 8; followChip.style.bottom = 14 + (132 - 30) / 2f;
                followChip.style.flexDirection = FlexDirection.Row; followChip.style.alignItems = Align.Center; followChip.style.paddingLeft = 12; followChip.style.paddingRight = 10;
                var fl = L(followChip, "verb-name"); followLabel = fl; fl.text = "Follow view"; fl.style.fontSize = 12; KeyCap(followChip, host.controls.Label(PlayAction.FollowView));
                manifest.Add((followChip, "image", () => "follow view chip")); Show(followChip, false);
            }
            Show(followChip, f && !m.Ended);
            if (f) Text(followLabel, host.Exploring ? "Exploring" : host.Driving ? "Let go" : "Follow view"); // brief 025 (Fable: "Let go: T")
            // a plain ground ring under the followed agent, drawn in the view's own perspective (the one world mark the follow view keeps)
            followRing.Clear();
            if (f && host.FollowAgent >= 0 && host.Views?.Find("op/" + Suborned.Sim.Op.Name((Suborned.Sim.OpId)host.FollowAgent)) is Transform ft && ft.gameObject.activeSelf)
                for (int i = 0; i <= 40; i++) { float a = i * Mathf.PI * 2f / 40f; if (!Proj(ft.position + new Vector3(Mathf.Cos(a) * 0.55f, 0.03f, Mathf.Sin(a) * 0.55f), out var q)) { followRing.Clear(); break; } followRing.Add(q); }
            if (!f && !followHid) return; followHid = f;
            foreach (var c in marks.Children())
            {
                bool keep = c == world; foreach (var b in bubbleEls) if (b.root == c) keep = true; foreach (var pe in popEls) if (pe.root == c) keep = true;
                c.style.visibility = f && !keep ? Visibility.Hidden : StyleKeyword.Null;
            }
        }
        bool followHid; VisualElement followChip; Label followLabel; readonly List<Vector2> followRing = new();

        /// <summary>Where the roster, the strips, the verb plate and the ticker sit for the dock (Glyph's dock rules, layout.json `docks`).
        ///  · left column: the column at (12, 84); a strip beside each shown agent's window; the verb plate right of the selected strip; the ticker under the column.
        ///  · corner: the row at the bottom left; the busy, hurt, down or selected strips stacked above it, bottom up; the verb plate above them; the ticker right of the row.
        ///  · bottom: the row centred at the bottom; no strips; the verb plate centred above the row; the ticker above the verb plate.</summary>
        void Dock()
        {
            var d = m.Dock; var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) || size.x <= 0 ? 1920 : size.x, H = float.IsNaN(size.y) || size.y <= 0 ? 1080 : size.y;
            Show(column, d == RosterDock.Left); Show(row, d != RosterDock.Left);
            int a = m.Agent; bool plateOn; float plateX, plateY;
            if (d == RosterDock.Left)
            {
                for (int i = 0; i < 4; i++) { var st = strips[i]; Show(st.Root, m.Slots[i].Show); st.Root.style.left = ColX + 113; st.Root.style.top = ColY + Win[i].y + Win[i].height / 2 - (m.Slots[i].Compact ? 17 : 46); }
                plateOn = m.Slots[a].Show; plateX = ColX + 113 + 228 + 10; plateY = ColY + Win[a].y + Win[a].height / 2 - 27;
                ticker.style.left = ColX; ticker.style.top = ColY + 416 + 8; ticker.style.width = 341;
            }
            else if (d == RosterDock.Corner)
            {
                float rowTop = H - 14 - 92; row.style.left = 14; row.style.top = rowTop;
                float y = rowTop - 6;
                for (int i = 3; i >= 0; i--)
                {
                    var s = m.Slots[i]; bool on = s.Show; Show(strips[i].Root, on); if (!on) continue;
                    y -= s.Compact ? 34 : 92; strips[i].Root.style.left = 14; strips[i].Root.style.top = y; y -= 6;
                }
                plateOn = true; plateX = 18; plateY = y - 54;
                ticker.style.left = 14 + 398 + 12; ticker.style.top = H - 14 - 34; ticker.style.width = 520;
            }
            else
            {
                float rowTop = H - 14 - 92; row.style.left = (W - 398) / 2; row.style.top = rowTop;
                for (int i = 0; i < 4; i++) Show(strips[i].Root, false);
                plateOn = true; plateX = (W - 229) / 2; plateY = rowTop - 10 - 54;
                ticker.style.left = (W - 341) / 2; ticker.style.top = plateY - 22 - 8 - 34; ticker.style.width = 341; // above the verb's name tag
            }
            Show(verbPlate, plateOn); verbPlate.style.left = plateX; verbPlate.style.top = plateY;
            bool armed = plateOn && m.VerbLit >= 0 && m.VerbLit < 4 && m.VerbHas[m.VerbLit]; Show(armedPlate, armed);
            // the verb's name under its knob; in the corner and bottom docks the strips or the row sit right under the plate, so it goes above
            if (armed) { armedPlate.style.left = plateX + 42.3f + m.VerbLit * 47.6f - 40; armedPlate.style.top = d == RosterDock.Left ? plateY + 54 + 4 : plateY - 4 - 18; }
        }

        void Pips(Window st, int hp, int max)
        {
            // Glyph round 2: the row must fit the photo circle's chord at its height (centre 30 px below the bezel centre: chord ~50 px); keep
            // 4 px clear of the chrome, so 6 pips shrink (6 px dots and 2.9 px gaps for four, smaller for six)
            float pitch = Mathf.Min(8.9f, 42f / Mathf.Max(1, max)), dot = Mathf.Min(6f, pitch * 0.67f), gap = (pitch - dot) / 2f;
            while (st.PipDots.Count < max) { var d = E(st.Pips, null, w: 6, h: 6); st.PipDots.Add(d); }
            foreach (var d in st.PipDots) { d.style.width = dot; d.style.height = dot; d.style.marginLeft = gap; d.style.marginRight = gap; d.style.alignSelf = Align.Center; }
            for (int i = 0; i < st.PipDots.Count; i++) { Show(st.PipDots[i], i < max); if (i < max) Bg(st.PipDots[i], i < hp ? "markers/pip_full" : "markers/pip_empty"); }
        }

        static readonly Dictionary<string, string> KindGlyph = new() { ["learned"] = "listen", ["approach"] = "help", ["call"] = "call", ["city"] = "p_time", ["warning"] = "p_alert", ["fight"] = "fire" };

        void Feed()
        {
            var feed = m.Feed; int live = 0; foreach (var c in feed) if (c.Live) live++;
            Text(barCount, m.Unread > 0 ? $"{m.Unread} new" : live > 0 ? $"{live} live" : "0 new");
            Text(barKeys, $"{m.AckKey} ack · {m.HistoryKey} history"); Icon(barChevron, m.HistoryOpen ? "chev_down" : "chev_up", Cream);
            Show(more, feed.Count > 3 && !m.HistoryOpen); Text(more, $"+{feed.Count - 3} more");
            // cards: newest on top (the column reverses: the first child after the bar sits nearest it), three at most; folded but the newest; in a fight all fold
            for (int i = 0; i < 3; i++)
            {
                var c = cards[i]; int idx = feed.Count - 3 + i; // cards[0] nearest the bar = the oldest of the three
                var card = !m.HistoryOpen && idx >= 0 && idx < feed.Count ? feed[idx] : null;
                c.For = card; Show(c.Root, card != null); if (card == null) continue;
                bool newest = idx == feed.Count - 1, open = m.Fight ? hoverCard == card : newest;
                foreach (var k in KindGlyph.Keys) Class(c.Header, "kind-" + k, card.Kind == k);
                Icon(c.KindIcon.Q("glyph"), KindGlyph.TryGetValue(card.Kind, out var kg) ? kg : "dot", Ink);
                Icon(c.VerbIcon, HudModel.AtlasIcon(card.Verb, card.Verb, ""), Cream);
                Text(c.Title, card.Title); Text(c.Where, card.Where); c.Where.style.left = 46.5f + Mathf.Min(150, card.Title.Length * 8.2f) + 8;
                Text(c.Clock, HandoverHost.MissionClock(card.Started));
                bool unread = !card.Live && !card.Read; Show(c.New, unread && newest); Show(c.Dot, unread);
                Show(c.Prog, open && card.Live && card.Progress >= 0); c.Fill.style.width = 284 * Mathf.Clamp01(card.Progress / 1000f);
                Text(c.Remaining, card.TicksLeft >= 0 ? $"{(card.TicksLeft + 9) / 10}s left" : "");
                Show(c.Lines, open);
                if (open)
                {
                    int shown = Mathf.Min(card.Revealed, card.Lines.Count);
                    for (int r = 0; r < c.Rows.Count; r++)
                    {
                        var (row, chip, name, text) = c.Rows[r]; Show(row, r < shown); if (r >= shown) continue;
                        var l = card.Lines[r]; bool cell = l.Speaker is "scout" or "fixer" or "tech" or "enforcer";
                        Text(name, l.Name); Class(name, "t-feed-name-cell", cell); Class(name, "t-feed-name", !cell);
                        Text(text, l.Text); Class(text, "t-feed-line-npc", !cell); Class(text, "t-feed-line", cell);
                        Bg(chip, cell ? $"portraits/cameo_{l.Speaker}_normal" : SpeakerBadge(l.Speaker));
                    }
                    Show(c.Typing, card.Revealed < card.Lines.Count);
                }
                bool footer = !card.Live && card.Footer.Length > 0 && (!m.Fight || open || newest); // a live card or one with nothing to report has no footer
                Show(c.Footer, footer);
                if (footer)
                {
                    c.Footer.style.backgroundColor = card.Live ? new Color(0.839f, 0.871f, 0.855f) : new Color(0.729f, 0.886f, 0.839f);
                    Border(c.Footer, card.Live ? 1f : 1.2f, card.Live ? new Color(0.588f, 0.667f, 0.651f) : new Color(0.384f, 0.62f, 0.573f));
                    Show(c.Check, !card.Live); Text(c.Result, card.Live ? "The result lands when the act finishes." : card.Footer);
                    Show(c.Ack, unread); Text(c.AckKey, m.AckKey);
                }
                Lift(c.Root, hoverCard == card);
            }
        }

        static string SpeakerBadge(string speaker) => speaker switch
        {
            "waitress" => "badges/npc_waitress", "driver" => "badges/npc_driver", "councillor" => "badges/npc_councillor", "watcher" => "badges/npc_watcher",
            "guard" => "badges/signal_guard", "rivals" => "badges/signal_rival", "patrol" or "security" => "badges/signal_police", _ => "badges/npc_passer",
        };
    }
}
