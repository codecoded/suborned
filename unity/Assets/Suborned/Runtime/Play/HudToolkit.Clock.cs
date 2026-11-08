using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101 (Sky 2026-11-08: "street labels top right but tokens bottom right"; Glyph's S mocks, briefs/101/glyph/r2/S-*.png): the clock as street
    /// labels, top right and right-aligned, with no plate: the pause button and the time (42 px); the seal's lock and count under it while it runs
    /// (HudToolkit.Seal); the Alert lamps, Trail and money; the next key event (its icon, its short word and its time); the units on their way (how many, how soon);
    /// the key people's faces. It retires the status pill (the kit's pill_status), the people plate and the response pill under it. Every part keeps its click (never key-only): the pause button pauses and resumes,
    /// the lamps show and pin the why list (HudToolkit.Why), a known face finds them and an unknown one opens Intel; the next event, hovered, names itself in full.
    /// Only those parts take the pointer: the stack's empty corners pass clicks to the street. `pill` is the stack's root, so the entrance, the keep-outs and
    /// the capture's parts read it as they read the pill. No action time anywhere (brief 101): the clock and the next key event are key events.</summary>
    public sealed partial class HudToolkit
    {
        KeyText clockK, speedK, trailK, cashK, nextWordK, nextTimeK, nextTipK, respK, respTimeK; KeyIcon alertIc, trailIc, cashIc, nextIc, nextUnkIc;
        VisualElement clockRow, statsRow, nextRow, clockLamps, respRow; bool nextHover;
        static readonly string[] KeyPeople = { "waitress", "guard", "driver", "councillor" };
        /// <summary>The stack's right edge from the screen's (Glyph's S mocks: the type's right edge at 1898 of 1920).</summary>
        const float ClockRight = 20f, ClockTop = 14f;

        void BuildClock()
        {
            StreetSheet();
            pill = E(root); pill.name = "clock"; pill.style.position = Position.Absolute; pill.style.right = ClockRight; pill.style.top = ClockTop;
            pill.style.flexDirection = FlexDirection.Column; pill.style.alignItems = Align.FlexEnd;
            // the time, with the pause button before it (and the speed, x2 or x4, while it runs fast)
            clockRow = StackRow(pill, 48f);
            speedK = KText(clockRow, 15f, "k-semi"); speedK.Root.style.marginRight = 8; state = speedK.Face;
            pauseBtn = Tab(clockRow, 34f, "pause", out pauseIcon); pauseBtn.name = "pause-button"; pauseBtn.style.marginRight = 10;
            pauseBtn.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.PauseClick(); e.StopPropagation(); } });
            var cb = new VisualElement { pickingMode = PickingMode.Ignore }; cb.style.flexShrink = 0; clockRow.Add(cb); clockK = KText(cb, 42f); clock = clockK.Face;
            // brief 080's close (Vale missed): a draining underline under the time, orange on an ink edge
            closeBar = new VisualElement { pickingMode = PickingMode.Ignore }; closeBar.AddToClassList("e-orange"); closeBar.style.position = Position.Absolute; closeBar.style.left = 0;
            closeBar.style.bottom = 4; closeBar.style.height = 4; Round(closeBar, 2f); BorderW(closeBar, 1f); closeBar.style.borderTopColor = closeBar.style.borderBottomColor = closeBar.style.borderLeftColor = closeBar.style.borderRightColor = BkInk;
            closeBar.style.display = DisplayStyle.None; cb.Add(closeBar);
            // the Alert lamps (the why list's hover and click: HudToolkit.Why), Trail, money
            statsRow = StackRow(pill, 28f); statsRow.style.marginTop = 4;
            var lb = clockLamps = E(statsRow); lb.name = "alert-lamps"; lb.style.flexDirection = FlexDirection.Row; lb.style.alignItems = Align.Center; lb.style.height = 28; lb.style.paddingLeft = 4; lb.style.paddingRight = 4;
            alertIc = KIcon(lb, "p_alert", 20f);
            for (int i = 0; i < 3; i++)
            {   // Glyph's lamps: 13 px, lit orange or unlit cream, each in a 1.6 px ink ring so they hold on any ground
                var l = lamps[i] = E(lb, null, w: 16.2f, h: 16.2f); l.style.flexShrink = 0; l.style.marginLeft = i == 0 ? 5f : 0.8f; Round(l, 8.1f); BorderW(l, 1.6f);
                l.style.borderTopColor = l.style.borderBottomColor = l.style.borderLeftColor = l.style.borderRightColor = BkInk;
            }
            manifest.Add((lb, "image", () => "alert lamps"));
            trailIc = KIcon(statsRow, "p_trail", 20f); trailIc.Root.style.marginLeft = 12; trailK = KText(statsRow, 18f); trailK.Root.style.marginLeft = 4; trail = trailK.Face;
            cashIc = KIcon(statsRow, "p_cash", 20f); cashIc.Root.style.marginLeft = 16; cashK = KText(statsRow, 17f, "k-semi"); cashK.Root.style.marginLeft = 5; cash = cashK.Face;
            manifest.Add((trailIc.Root, "image", () => "trail icon")); manifest.Add((cashIc.Root, "image", () => "cash icon"));
            manifest.Add((statsRow, "bar", () => "")); // the clock bar's figures (glyph-budget §5): one row of figures and icons, no word, read off the picture
            // the next key event: its icon, its short word ("?" while the cell can't know it) and its time; hovered, its full name under the faces
            nextRow = StackRow(pill, 28f); nextRow.pickingMode = PickingMode.Position; nextRow.name = "next-event";
            nextIc = KIcon(nextRow, "nextevent", 18f, "ki-deep"); nextUnkIc = KIcon(nextRow, "p_unknown", 16f); nextUnkIc.Root.style.marginLeft = 6; nextUnknown = nextUnkIc.Root;
            nextWordK = KText(nextRow, 15f, "k-semi"); nextWordK.Root.style.marginLeft = 6; nextLabel = nextWordK.Face;
            nextTimeK = KText(nextRow, 17f); nextTimeK.Root.style.marginLeft = 6; nextTime = nextTimeK.Face;
            nextRow.RegisterCallback<PointerEnterEvent>(_ => nextHover = true); nextRow.RegisterCallback<PointerLeaveEvent>(_ => nextHover = false);
            manifest.Add((nextIc.Root, "image", () => "next icon")); manifest.Add((nextUnknown, "image", () => "next unknown"));
            manifest.Add((nextRow, "marker", () => nextUnknown.resolvedStyle.display != DisplayStyle.None || nextWordK.Words.Length == 0 ? nextTimeK.Words : nextWordK.Words + " " + nextTimeK.Words)); // the next key event: a timeline mark (its short word and its time), never a line
            // the units on their way (police arriving is a key event: its time shows): the arrive mark, how many, how soon, orange; a click opens the response's drawer
            respRow = StackRow(pill, 28f); respRow.pickingMode = PickingMode.Position; respRow.name = "police-coming";
            KIcon(respRow, "arrive", 18f); KIcon(respRow, "units", 16f).Root.style.marginLeft = 6; respK = KText(respRow, 17f); respK.Root.style.marginLeft = 4;
            respTimeK = KText(respRow, 17f, "k-bold", "k-hot", "k-key-ink", KeyHotW); respTimeK.Root.style.marginLeft = 10;
            respRow.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ResponseOpen = !host.ResponseOpen; e.StopPropagation(); } });
            manifest.Add((respRow, "marker", () => respTimeK.Words.Length > 0 ? respTimeK.Words : null)); Show(respRow, false);
            // the key people: their faces, grey until the cell knows them; a known face finds them in the street, an unknown one opens Intel
            keys = StackRow(pill, 30f); keys.style.marginTop = 4; keys.name = "known-people";
            for (int i = 0; i < 4; i++)
            {
                int k = i; var w = E(keys, null, w: 26, h: 26, pick: true); w.style.flexShrink = 0; w.style.marginLeft = i == 0 ? 0 : 4;
                var sh = Disc(w, 26f, "e-shadow"); Pin(sh, new Vector2(0f, 2f)); sh.style.opacity = 0.6f;
                var d = E(w, null, 0, 0, 26, 26); Round(d, 13f); BorderW(d, 2f); d.AddToClassList("e-inline"); d.AddToClassList("e-page"); Bg(d, FaceOf(KeyPeople[i])); peopleDiscs.Add(d);
                w.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { if (KnownPerson(KeyPeople[k])) FindPerson(KeyPeople[k]); else host.IntelOpen = !host.IntelOpen; e.StopPropagation(); } });
            }
            manifest.Add((keys, "image", () => "known people"));
            nextTipK = KText(pill, 14f, "k-semi"); nextTipK.Root.style.marginTop = 6; nextTip = nextTipK.Root; Show(nextTip, false);
            manifest.Add((clockRow, "marker", () => clockK.Words)); manifest.Add((pauseBtn, "image", () => "pause button")); // the row, keylines and the button with it: a clock reading
        }

        /// <summary>A row of the stack, right-aligned, `h` tall, its pieces centred on its middle.</summary>
        static VisualElement StackRow(VisualElement parent, float h)
        {
            var r = new VisualElement { pickingMode = PickingMode.Ignore, focusable = false }; r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center;
            r.style.justifyContent = Justify.FlexEnd; r.style.height = h; r.style.flexShrink = 0; r.style.overflow = Overflow.Visible; parent.Add(r); return r;
        }

        /// <summary>A round button on the street (Glyph's tab: the kit's deep body, a 2 px cream ring, a cream pictogram at half its size): the pause button.
        /// Hovered, its ring turns turquoise-light.</summary>
        VisualElement Tab(VisualElement parent, float d, string icon, out VisualElement glyph)
        {
            var t = E(parent, null, w: d, h: d, pick: true); t.style.flexShrink = 0; t.style.alignItems = Align.Center; t.style.justifyContent = Justify.Center; t.style.overflow = Overflow.Visible;
            var sh = Disc(t, d, "e-shadow"); Pin(sh, new Vector2(0f, 2f)); sh.style.opacity = 0.6f;
            var body = Disc(t, d, "e-body"); Pin(body, Vector2.zero); body.AddToClassList("e-inline"); BorderW(body, 2f);
            glyph = E(t, null, w: d * 0.52f, h: d * 0.52f); glyph.style.flexShrink = 0; Icon(glyph, icon, Cream);
            t.RegisterCallback<PointerEnterEvent>(_ => RingColour(body, TurqLight)); t.RegisterCallback<PointerLeaveEvent>(_ => RingColour(body, null));
            return t;
        }

        static void RingColour(VisualElement e, Color? c)
        {
            if (c is Color k) e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = k;
            else e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = StyleKeyword.Null;
        }

        /// <summary>The stack's bottom in panel px (the why list, Intel and the response drawer hang under it).</summary>
        float ClockBottom()
        {
            float b = pill != null ? pill.layout.yMax : float.NaN;
            if (nextTip != null && nextTip.resolvedStyle.display != DisplayStyle.None && !float.IsNaN(b)) b -= nextTip.layout.height + 6f; // the hover tip never pushes the drawers
            return float.IsNaN(b) || b <= 0f ? ClockTop + 48f + 4f + 28f + 28f + 34f : b;
        }

        /// <summary>Per frame, in the less-reading pass: the clock's words, the pause button, the lamps, Trail, money, the next key event, the units coming and the faces.</summary>
        void ClockFrame()
        {
            bool ended = m.Ended;
            clockK.Set(Stress ? "7:30" : m.Clock); CloseUnderline();
            Keys096Frame(ended); pauseIcon.style.opacity = host.Frozen && !PlateOn ? 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f)) : 1f; // brief 030: the pause glyph pulses while frozen
            bool fast = host.Speed > 1; speedK.Set(fast ? $"x{host.Speed}" : ""); Show(speedK.Root, fast);
            int a = m.Alert; alertIc.Look(a > 0 ? "ki-hot" : "ki-ink", "ki-key");
            for (int i = 0; i < 3; i++) { var c = i < a ? BkOrange : StPage; if (lamps[i].style.backgroundColor.value != c) lamps[i].style.backgroundColor = c; }
            trailK.Set(Stress ? "12" : m.Trail.Replace("T ", "")); cashK.Set(m.Cash);
            bool unknown = m.NextLabel == "?", has = m.NextTime.Length > 0; Show(nextRow, has);
            string word = host.Text != null && host.Text.TimelineShort.TryGetValue(m.NextTime, out var sl) && sl.Length > 0 ? sl : m.NextLabel;
            Show(nextUnknown, unknown); Show(nextWordK.Root, !unknown && word.Length > 0); nextWordK.Set(word); nextTimeK.Set(Stress ? "7:30" : m.NextTime);
            bool tip = has && nextHover && !ended; Show(nextTip, tip); if (tip) nextTipK.Set(unknown ? "Next · ?" : $"Next · {m.NextLabel}");
            bool coming = host.RespComing > 0 && !ended; Show(respRow, coming);
            if (coming) { int sec = host.RespSoonestS; respK.Set(host.RespComing.ToString()); respTimeK.Set(sec >= 0 ? $"{sec / 60}:{sec % 60:00}" : "?"); }
            Show(keys, !ended && !m.BriefingOpen);
            for (int i = 0; i < 4; i++) { bool kn = KnownPerson(KeyPeople[i]); peopleDiscs[i].style.opacity = kn ? 1f : 0.6f; peopleDiscs[i].style.unityBackgroundImageTintColor = kn ? Color.white : new Color(0.55f, 0.6f, 0.6f); }
        }
    }
}
