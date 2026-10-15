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
    public sealed class HudToolkit : MonoBehaviour
    {
        static HudToolkit live;
        /// <summary>The kit view is drawing (the IMGUI view then skips the pieces it has).</summary>
        public static bool Active => live != null && live.on;
        /// <summary>A screen point (origin bottom left) on a piece that takes clicks.</summary>
        public static bool Blocks(Vector2 screen)
        {
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
            doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; go.SetActive(true); // the panel exists once enabled with its settings
            root = doc.rootVisualElement; root.pickingMode = PickingMode.Ignore; root.style.flexGrow = 1;
            var uss = Resources.Load<StyleSheet>("Kit/kit"); if (uss != null) root.styleSheets.Add(uss);
            m = imgui != null ? imgui.Model : new HudModel();
            Build(); live = this;
        }

        void OnDestroy() { if (live == this) live = null; }

        // ---------------------------------------------------------------- helpers

        Sprite Sp(string file) // "portraits/portrait_scout_normal" → the @2x sprite under Resources/Kit
        {
            if (sprites.TryGetValue(file, out var s)) return s;
            s = Resources.Load<Sprite>($"Kit/{file}@2x"); sprites[file] = s; return s;
        }

        static VisualElement E(VisualElement parent, string cls = null, float x = float.NaN, float y = float.NaN, float w = float.NaN, float h = float.NaN, bool pick = false)
        {
            var e = new VisualElement { pickingMode = pick ? PickingMode.Position : PickingMode.Ignore };
            if (cls != null) foreach (var c in cls.Split(' ')) e.AddToClassList(c);
            if (!float.IsNaN(x)) { e.style.position = Position.Absolute; e.style.left = x; }
            if (!float.IsNaN(y)) { e.style.position = Position.Absolute; e.style.top = y; }
            if (!float.IsNaN(w)) e.style.width = w; if (!float.IsNaN(h)) e.style.height = h;
            parent?.Add(e); return e;
        }

        static Label L(VisualElement parent, string style, float x = float.NaN, float y = float.NaN, float w = float.NaN, float h = float.NaN, bool ellipsis = false)
        {
            var l = new Label { pickingMode = PickingMode.Ignore }; l.AddToClassList("t-" + style);
            if (!float.IsNaN(x)) { l.style.position = Position.Absolute; l.style.left = x; }
            if (!float.IsNaN(y)) { l.style.position = Position.Absolute; l.style.top = y; }
            if (!float.IsNaN(w)) l.style.width = w; if (!float.IsNaN(h)) l.style.height = h;
            if (ellipsis) { l.style.overflow = Overflow.Hidden; l.style.whiteSpace = WhiteSpace.NoWrap; l.style.textOverflow = TextOverflow.Ellipsis; }
            l.style.unityTextAlign = TextAnchor.MiddleLeft;
            parent?.Add(l); return l;
        }

        static void Text(Label l, string s) { s ??= ""; if (l.text != s) l.text = s; }
        static void Show(VisualElement e, bool v) { var d = v ? DisplayStyle.Flex : DisplayStyle.None; if (e.style.display != d) e.style.display = d; }
        void Bg(VisualElement e, string file, Color? tint = null) { var s = Sp(file); if (s != null && e.style.backgroundImage.value.sprite != s) e.style.backgroundImage = new StyleBackground(s); if (tint.HasValue) e.style.unityBackgroundImageTintColor = tint.Value; }
        void Icon(VisualElement e, string atlas, Color tint) { if (string.IsNullOrEmpty(atlas)) { Show(e, false); return; } Show(e, true); Bg(e, "icons/singles/" + atlas, tint); }
        static void Class(VisualElement e, string c, bool v) { if (e.ClassListContains(c) != v) e.EnableInClassList(c, v); }

        static readonly Color Cream = new(0.969f, 0.957f, 0.925f), Ink = new(0.09f, 0.188f, 0.184f), TurqLight = new(0.376f, 0.878f, 0.918f), Grey = new(0.478f, 0.549f, 0.541f), DeepTeal = new(0.122f, 0.424f, 0.404f);

        // ---------------------------------------------------------------- the pieces (rects from layout.json, px at 1920 x 1080)

        VisualElement pill, objective, column, ticker, feed, verbPlate, tooltip;
        Label clock, nextLabel, nextTime, cash, trail, state, objText, objTail, tickerText, barCount, barKeys, tipTitle, tipBody, tipKey;
        readonly VisualElement[] lamps = new VisualElement[3];
        VisualElement tickerChip, barChevron, barPill;
        // roster windows (roster_column.windows) and the strips
        static readonly Rect[] Win = { new(22, 34, 66, 65), new(22, 127, 66, 66), new(22, 222, 66, 66), new(22, 317, 65, 65) };
        const float ColX = 12, ColY = 84;
        sealed class Strip { public VisualElement Root, Portrait, RingSel, RingBusy, Pips, Icon, Bar, Fill, Cancel, QueuedIcon, Stance; public Label Num, Role, State, Action, Time, Queued, Ready, Hint; public readonly List<VisualElement> PipDots = new(); }
        readonly Strip[] strips = new Strip[4];
        readonly VisualElement[] knobs = new VisualElement[4], knobIcons = new VisualElement[4]; readonly Label[] knobNums = new Label[4]; VisualElement armedPlate; Label armedName;
        sealed class Card { public VisualElement Root, Header, KindIcon, VerbIcon, New, Dot, Prog, Fill, Lines, Footer, Check, Ack; public Label Title, Where, Clock, Remaining, Result, AckKey, Typing; public readonly List<(VisualElement row, VisualElement chip, Label name, Label text)> Rows = new(); public HandoverHost.FeedCard For; }
        readonly Card[] cards = new Card[3]; Label more; HandoverHost.FeedCard hoverCard;
        sealed class IntelUi { public VisualElement Row, Need, NeedGlyph, NeedMark, Temp, TempGlyph, Turned, NextIcon; public Label Name, Job, Next, Secret; }
        VisualElement intel, intelStrip, intelOpen, intelChevron; Label intelCount; readonly IntelUi[] intelRows = new IntelUi[4];

        void Build()
        {
            // the status pill (status_pill): top right 14, 12; height 44; padding from the content rect
            pill = E(root, "s-pill_status", pick: true); pill.style.position = Position.Absolute; pill.style.right = 14; pill.style.top = 12; pill.style.height = 44;
            pill.style.flexDirection = FlexDirection.Row; pill.style.alignItems = Align.Center; pill.style.paddingLeft = 19; pill.style.paddingRight = 18;
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
            // the objective line (objective_line): under the pill, right 14, top 62, height 34
            objective = E(root, "s-panel_strip", pick: true); objective.style.position = Position.Absolute; objective.style.right = 14; objective.style.top = 62; objective.style.height = 34;
            objective.style.flexDirection = FlexDirection.Row; objective.style.alignItems = Align.Center; objective.style.paddingLeft = 40; objective.style.paddingRight = 26;
            var mark = E(objective, null, 23, 12, 11, 11); Round(mark, 5.5f); Border(mark, 1.4f, TurqLight); var dot = E(mark, null, 3.3f, 3.3f, 4.4f, 4.4f); Round(dot, 2.2f); dot.style.backgroundColor = TurqLight;
            objText = L(objective, "objective"); objTail = L(objective, "objective-tail");
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
            column = E(root, null, ColX, ColY, 109, 416);
            for (int i = 0; i < 4; i++)
            {
                var st = strips[i] = new Strip(); int k = i; var w = Win[i];
                st.Portrait = E(column, null, w.x, w.y, 66, 66, pick: true);
                st.Portrait.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.PortraitClick(k, e.shiftKey); e.StopPropagation(); } });
            }
            E(column, "s-roster_column", 0, 0, 109, 416).pickingMode = PickingMode.Ignore;
            for (int i = 0; i < 4; i++)
            {
                var st = strips[i]; var w = Win[i];
                E(column, "s-portrait_scrim", w.x, w.y + 52, 66, 14);
                st.Pips = E(column, null, w.x, w.y + 56, 66, 8); st.Pips.style.flexDirection = FlexDirection.Row; st.Pips.style.justifyContent = Justify.Center;
                var plate = E(column, "s-plate_light", w.x + 2, w.y + 1, 25, 21); st.Num = L(plate, "numplate"); st.Num.style.flexGrow = 1; st.Num.style.unityTextAlign = TextAnchor.MiddleCenter;
                st.RingSel = E(column, "s-portrait_ring_selected", w.x - 11, w.y - 11, 87, 87); st.RingBusy = E(column, "s-portrait_ring_busy", w.x - 11, w.y - 11, 87, 87);
            }
            // the slot strips (slot_strip): x = column right + 4, centred on their window; 228 x 92
            for (int i = 0; i < 4; i++)
            {
                var st = strips[i]; int k = i; float top = ColY + Win[i].y + Win[i].height / 2 - 46;
                st.Root = E(root, "s-panel_slot", ColX + 113, top, 228, 92, pick: true);
                st.Root.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.PortraitClick(k, e.shiftKey); e.StopPropagation(); } });
                var tags = E(st.Root, null, 30, 7, 170, 16); tags.style.flexDirection = FlexDirection.Row;
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
            verbPlate = E(root, "s-plate_verbs", ColX + 113 + 228 + 10, 0, 229, 54, pick: true);
            for (int i = 0; i < 4; i++)
            {
                int k = i; float cx = 42.3f + i * 47.6f;
                var kb = knobs[i] = E(verbPlate, "s-btn_verb_normal btn_verb", cx - 24, 3, 48, 48, pick: true);
                kb.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.Pick(k); e.StopPropagation(); } });
                knobIcons[i] = E(kb, null, 14, 14, 20, 20);
                knobNums[i] = L(kb, "numplate", 27, 28, 12, 12); knobNums[i].style.color = Cream; knobNums[i].style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            armedPlate = E(root, "s-plate_dark", 0, 0, float.NaN, 18); armedName = L(armedPlate, "verb-name"); armedName.style.paddingLeft = 8; armedName.style.paddingRight = 8; armedName.style.unityTextAlign = TextAnchor.MiddleCenter;
            // the ticker (ticker): below the column +8, 341 x 34; the cell's cameo and its last word, fading
            ticker = E(root, "s-panel_strip", ColX, ColY + 416 + 8, 341, 34);
            tickerChip = E(ticker, null, 14, 3, 28, 28); tickerText = L(ticker, "ticker", 48, 8, 280, 18, true);
            // the cancel tooltip (tooltip_cancel): a card at the cursor
            tooltip = E(root, "s-panel_card", 0, 0, 292, float.NaN); tooltip.style.paddingLeft = 15; tooltip.style.paddingRight = 15; tooltip.style.paddingTop = 30; tooltip.style.paddingBottom = 10;
            tipTitle = L(tooltip, "tooltip-title"); tipBody = L(tooltip, "tooltip-body"); tipBody.style.whiteSpace = WhiteSpace.Normal;
            var keyRow = E(tooltip); keyRow.style.flexDirection = FlexDirection.Row; keyRow.style.marginTop = 4;
            var kp = E(keyRow, "s-plate_light", w: float.NaN, h: 19); tipKey = L(kp, "keycap"); tipKey.style.paddingLeft = 6; tipKey.style.paddingRight = 6;
            var kt = L(keyRow, "tooltip-key"); kt.text = "cancels the selected agent's act"; kt.style.marginLeft = 6;
            Show(tooltip, false);
            // the feed (feed_bar, scene_card): bottom right 14; cards above the bar, newest on top
            feed = E(root); feed.style.position = Position.Absolute; feed.style.right = 14; feed.style.bottom = 14; feed.style.width = 384; feed.style.flexDirection = FlexDirection.ColumnReverse;
            var bar = E(feed, "s-panel_strip", pick: true); bar.style.height = 32; bar.style.flexShrink = 0;
            bar.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.FeedHistoryOpen = !host.FeedHistoryOpen; e.StopPropagation(); } });
            var title = L(bar, "feed-bar", 24, 9, 60, 14); title.text = "SCENES";
            barPill = E(bar, "s-pill_white", 82, 7, float.NaN, 18); barCount = L(barPill, "feed-filter"); barCount.style.paddingLeft = 8; barCount.style.paddingRight = 8; barCount.style.unityTextAlign = TextAnchor.MiddleCenter;
            barKeys = L(bar, "feed-bar-sub", 210, 9, 134, 14); barKeys.style.unityTextAlign = TextAnchor.MiddleRight;
            barChevron = E(bar, null, 358, 9, 14, 14);
            more = L(feed, "feed-filter"); more.AddToClassList("s-pill_white"); more.style.alignSelf = Align.FlexEnd; more.style.marginBottom = 4; more.style.paddingLeft = 8; more.style.paddingRight = 8; more.style.height = 20; more.style.unityTextAlign = TextAnchor.MiddleCenter;
            for (int i = 0; i < 3; i++) cards[i] = NewCard(feed);
        }

        void Sep(VisualElement parent) { var s = E(parent, null, w: 1, h: 18); s.style.backgroundColor = new Color(0.824f, 0.902f, 0.886f, 0.35f); s.style.marginLeft = 12; s.style.marginRight = 12; }
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
            m.Frame(host);
            // the pill
            Text(clock, m.Clock); Text(nextLabel, m.NextLabel + " · "); Text(nextTime, m.NextTime); Text(cash, m.Cash); Text(trail, m.Trail.Replace("T ", ""));
            for (int i = 0; i < 3; i++) lamps[i].style.backgroundColor = i < m.Alert ? new Color(1f, 0.988f, 0.94f) : new Color(0.039f, 0.11f, 0.118f);
            Text(state, m.State.Contains("PAUSE") ? "PAUSED" : m.State);
            Text(objText, m.Objective); Text(objTail, m.ObjectiveTail);
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
            // the roster and the strips
            bool any = false; for (int i = 0; i < 4; i++) any |= m.Slots[i].Selected;
            for (int i = 0; i < 4; i++)
            {
                var s = m.Slots[i]; var st = strips[i];
                string look = s.Down ? "down" : s.Hurt ? "hurt" : !any || s.Selected ? "normal" : "dim";
                Bg(st.Portrait, $"portraits/portrait_{s.Role}_{look}");
                Show(st.RingSel, s.Selected); Show(st.RingBusy, s.Busy && !s.Selected);
                Text(st.Num, s.Key);
                Pips(st, s.Hp, s.HpMax);
                Show(st.Root, s.Show);
                if (!s.Show) continue;
                Class(st.Root, "selected", s.Selected);
                Border(st.Root, s.Selected ? 2.4f : 0f, s.Selected ? Color.white : Color.clear);
                Text(st.Role, s.Name.ToUpperInvariant()); Text(st.State, "·  " + s.StateTag);
                foreach (var cls in new[] { "t-slot-state-ready", "t-slot-state-busy", "t-slot-state-hurt", "t-slot-state-down" }) Class(st.State, cls, cls == "t-slot-state-" + (s.StateTag == "RUNNING" ? "busy" : s.StateTag.ToLowerInvariant()));
                Show(st.Stance, m.Fight && s.Armed); if (m.Fight && s.Armed) Bg(st.Stance, $"badges/stance_{(s.Stance == "H" ? "hold" : s.Stance == "A" ? "free" : "return")}");
                bool busy = s.Busy && !s.Down;
                Show(st.Icon, busy); if (busy) Icon(st.Icon, s.Icon, Cream);
                Show(st.Action, busy); Text(st.Action, s.Label);
                Show(st.Bar, busy && s.Progress >= 0); st.Fill.style.width = 104 * Mathf.Clamp01(s.Progress / 1000f);
                Show(st.Time, busy && s.Left.Length > 0); Text(st.Time, s.Left);
                Show(st.Cancel, s.CanCancel);
                Show(st.Queued, s.Queued.Length > 0); Show(st.QueuedIcon, s.Queued.Length > 0); Text(st.Queued, s.Queued);
                Show(st.Ready, !busy); Text(st.Ready, s.Down ? "Down" : s.Hurt ? "Hurt, still fighting" : "Ready");
                Show(st.Hint, !busy && s.Queued.Length == 0); Text(st.Hint, s.HintLine);
            }
            // the verb plate beside the selected agent's strip
            {
                int a = m.Agent; bool show = m.Slots[a].Show;
                Show(verbPlate, show); Show(armedPlate, show && m.VerbLit >= 0 && m.VerbLit < 4 && m.VerbHas[m.VerbLit]);
                float top = ColY + Win[a].y + Win[a].height / 2 - 27; verbPlate.style.top = top;
                for (int i = 0; i < 4; i++)
                {
                    Class(knobs[i], "selected", i == m.VerbLit && m.VerbHas[i]); Class(knobs[i], "disabled", !m.VerbHas[i]);
                    Icon(knobIcons[i], m.VerbIcon[i], m.VerbHas[i] ? Cream : Grey); Text(knobNums[i], m.VerbKey[i]);
                }
                if (m.VerbLit >= 0 && m.VerbLit < 4) { armedPlate.style.left = ColX + 113 + 228 + 10 + 42.3f + m.VerbLit * 47.6f - 40; armedPlate.style.top = top + 54 + 4; Text(armedName, m.VerbName[m.VerbLit]); }
            }
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
        }

        void Pips(Strip st, int hp, int max)
        {
            while (st.PipDots.Count < max) { var d = E(st.Pips, null, w: 6, h: 6); d.style.marginLeft = 1.45f; d.style.marginRight = 1.45f; st.PipDots.Add(d); }
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
                bool footer = (card.Live || card.Footer.Length > 0) && (!m.Fight || open || newest); // a card with nothing to report has no footer
                Show(c.Footer, footer);
                if (footer)
                {
                    c.Footer.style.backgroundColor = card.Live ? new Color(0.839f, 0.871f, 0.855f) : new Color(0.729f, 0.886f, 0.839f);
                    Border(c.Footer, card.Live ? 1f : 1.2f, card.Live ? new Color(0.588f, 0.667f, 0.651f) : new Color(0.384f, 0.62f, 0.573f));
                    Show(c.Check, !card.Live); Text(c.Result, card.Live ? "The result lands when the act finishes." : card.Footer);
                    Show(c.Ack, unread); Text(c.AckKey, m.AckKey);
                }
                Border(c.Root, hoverCard == card ? 2.4f : 0f, hoverCard == card ? Color.white : Color.clear);
            }
        }

        static string SpeakerBadge(string speaker) => speaker switch
        {
            "waitress" => "badges/npc_waitress", "driver" => "badges/npc_driver", "councillor" => "badges/npc_councillor", "watcher" => "badges/npc_watcher",
            "guard" => "badges/signal_guard", "rivals" => "badges/signal_rival", "patrol" or "security" => "badges/signal_police", _ => "badges/npc_passer",
        };
    }
}
