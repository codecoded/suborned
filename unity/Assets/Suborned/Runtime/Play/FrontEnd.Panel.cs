using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 051's Options (Display, Audio, Camera, Gameplay) and Controls (Command, Camera, Fights, Third person) on one panel: the kit's
    /// dark modal frame, plate tabs, rows of a slider, a switch or a choice (the value always a word), Reset page and Back. Every change goes to
    /// settings.json at once (Settings.Save(notify)) and the host applies it (its table, the camera, the display). The twelve old pause-menu toggles
    /// live on Gameplay and Camera, hidden by the click scheme as the pause menu hid them, each keeping its saved value.</summary>
    public sealed partial class FrontEnd
    {
        VisualElement panelCard, tabsRow, tabLine, bodyClip, bodyContent, scrollBar, footerReset, glassEl, glassImg, glassVeil, glassTint, rowTick; Label panelTitle, resetLabel;
        LightWord resetWord;
        readonly List<(VisualElement el, Label l, string id)> tabEls = new();
        bool optionsPage; int optTab, ctlTab; float scroll;
        static readonly string[] OptTabs = { "Display", "Audio", "Camera", "Gameplay" };
        LSheet SH => F.lightline.sheet;

        /// <summary>Light line: the sheet (no sprite, no corner close: Esc, Back and B close it), 960 x 780 ref, centred, a 1 ref chrome border at 34 %,
        /// radius 2; smoked glass = the picture baked small and blurred, lined up with the full-screen picture, under ink-deep at the veil's 52 % and
        /// ink at 72 % (from the pause menu: flat ink 90 %). Title, a tab strip with one sliding line, rows with one sliding tick, the footer's words.</summary>
        void BuildPanel()
        {
            panelGroup = E(blocker); Fill(panelGroup);
            var sh = SH; var ll = F.lightline;
            panelCard = E(panelGroup, pick: true); panelCard.style.position = Position.Absolute; panelCard.style.width = sh.wRef; panelCard.name = "panel-card"; panelCard.style.overflow = Overflow.Hidden;
            Border(panelCard, sh.borderRef, C("chrome", sh.borderAlpha), sh.radiusRef);
            panelCard.RegisterCallback<WheelEvent>(e => { scroll += e.delta.y * 24f; e.StopPropagation(); });
            glassEl = E(panelCard); Fill(glassEl); glassEl.style.overflow = Overflow.Hidden;
            glassImg = E(glassEl); glassImg.style.position = Position.Absolute; if (glass) { glassImg.style.backgroundImage = new StyleBackground(glass); glassImg.style.backgroundSize = new BackgroundSize(new Length(100, LengthUnit.Percent), new Length(100, LengthUnit.Percent)); }
            glassVeil = E(glassEl); Fill(glassVeil); glassTint = E(glassEl); Fill(glassTint);
            panelTitle = T(panelCard, ll.type.title, "Options"); panelTitle.style.position = Position.Absolute; panelTitle.style.left = sh.padX - 2; panelTitle.style.top = sh.titleTop - 0.22f * ll.type.title.sizeRef;
            tabsRow = E(panelCard); tabsRow.style.position = Position.Absolute; tabsRow.style.left = sh.padX; tabsRow.style.right = sh.padX; tabsRow.style.top = sh.tabsTop; tabsRow.style.height = sh.tabH + 1; tabsRow.style.flexDirection = FlexDirection.Row;
            tabsRow.style.borderBottomWidth = 1; tabsRow.style.borderBottomColor = C("panelLine", ll.line.ruleAlpha);
            tabLine = E(tabsRow); tabLine.style.position = Position.Absolute; tabLine.style.bottom = -1; tabLine.style.height = ll.line.wordHRef; tabLine.style.backgroundColor = C("live"); tabLine.style.width = 0;
            Trans(tabLine, ("left", ll.timing.tabSlide, EasingMode.EaseOutCubic, 0f), ("width", ll.timing.tabSlide, EasingMode.EaseOutCubic, 0f));
            bodyClip = E(panelCard); bodyClip.style.position = Position.Absolute; bodyClip.style.left = sh.padX; bodyClip.style.right = sh.padX; bodyClip.style.top = sh.bodyTop; bodyClip.style.overflow = Overflow.Hidden;
            bodyContent = E(bodyClip); bodyContent.style.position = Position.Absolute; bodyContent.style.left = 0; bodyContent.style.right = 0; bodyContent.style.top = 0;
            rowTick = E(bodyClip); rowTick.style.position = Position.Absolute; rowTick.style.width = ll.line.tickWRef; rowTick.style.backgroundColor = C("live"); rowTick.name = "row-tick"; rowTick.style.opacity = 0;
            Trans(rowTick, ("top", ll.timing.tickSlide, EasingMode.EaseOutCubic, 0f), ("left", ll.timing.tickSlide, EasingMode.EaseOutCubic, 0f), ("opacity", ll.timing.labelFade, EasingMode.EaseOut, 0f));
            scrollBar = E(panelCard); scrollBar.style.position = Position.Absolute; scrollBar.style.right = sh.padX - 14; scrollBar.style.width = 4; scrollBar.style.backgroundColor = C("chrome", 0.5f); Radius(scrollBar, 2);
            // the footer: a rule 92 above the bottom, the words centred in the strip below it (Reset page left, Back right)
            var rule = E(panelCard); rule.style.position = Position.Absolute; rule.style.left = sh.padX; rule.style.right = sh.padX; rule.style.bottom = sh.footerRuleFromBottom - 1; rule.style.height = 1; rule.style.backgroundColor = C("panelLine", ll.line.ruleAlpha);
            float itemBottom = sh.footerRuleFromBottom - (sh.footerRuleFromBottom - sh.footerItemH) / 2f + 2f - sh.footerItemH;
            resetWord = WordButton(panelCard, "Reset page", ll.type.action, float.NaN, sh.footerItemH, sh.footerInset, sh.footerGutter, -1f, sh.footerLineY, ResetPage, out _);
            footerReset = resetWord.Root; footerReset.style.position = Position.Absolute; footerReset.style.left = sh.padX - sh.footerInset + 4; footerReset.style.bottom = itemBottom; footerReset.name = "panel-reset"; resetLabel = resetWord.Label;
            var back = WordButton(panelCard, "Back", ll.type.action, float.NaN, sh.footerItemH, sh.footerInset, sh.footerGutter, -1f, sh.footerLineY, Back, out _).Root;
            back.style.position = Position.Absolute; back.style.right = sh.padX - 8; back.style.bottom = itemBottom; back.name = "panel-back";
            BuildClashStrip();
        }

        void PanelLayout()
        {
            var sh = SH; float h = Mathf.Min(sh.hRef, H - 96f);
            float left = (W - sh.wRef) / 2f, top = (H - h) / 2f;
            panelCard.style.height = h; panelCard.style.left = left; panelCard.style.top = top;
            // the glass lines up with the full-screen picture (background position = -sheet.left, -sheet.top); flat ink 90 % from the pause menu or with no bake
            bool glassy = glass && !fromPause;
            Show(glassImg, glassy);
            if (glassy) { glassImg.style.left = pictureEl.style.left.value.value - left - 1f; glassImg.style.top = pictureEl.style.top.value.value - top - 1f; glassImg.style.width = pictureEl.style.width.value.value; glassImg.style.height = pictureEl.style.height.value.value; }
            glassVeil.style.backgroundColor = glassy ? C("inkDeep", SrgbAlpha(F.scrims.panel.Length > 0 ? F.scrims.panel[0].alphaAtEdge : 0.52f)) : Color.clear;
            glassTint.style.backgroundColor = C("ink", SrgbAlpha(glassy ? sh.glassTint : sh.flatAlpha));
            float bodyH = h - sh.bodyTop - sh.footerRuleFromBottom - 8f; bodyClip.style.height = bodyH;
            float contentH = bodyContent.layout.height; if (float.IsNaN(contentH)) contentH = 0;
            bool clashOn = clashStrip != null && clashStrip.resolvedStyle.display != DisplayStyle.None;
            if (clashOn) bodyH = Mathf.Min(bodyH, h - sh.footerRuleFromBottom - F.lightline.clash.stripH - F.lightline.clash.gapAbove - 2f - sh.bodyTop);
            float max = Mathf.Max(0f, contentH - bodyH); scroll = Mathf.Clamp(scroll, 0f, max);
            bodyContent.style.top = -scroll;
            Show(scrollBar, max > 1f);
            if (clashOn)
            {   // between the table and the footer rule (table ends 614, strip 622-678 at 1080)
                float cs = F.lightline.clash.stripH, t = sh.bodyTop + Mathf.Min(contentH - scroll, bodyH) + F.lightline.clash.gapAbove, maxTop = h - sh.footerRuleFromBottom - cs - 2f;
                clashStrip.style.bottom = StyleKeyword.Auto; clashStrip.style.top = Mathf.Min(t, maxTop);
            }
            if (max > 1f) { float barH = Mathf.Max(24f, bodyH * bodyH / contentH); scrollBar.style.height = barH; scrollBar.style.top = sh.bodyTop + (bodyH - barH) * (scroll / max); }
            // the tab line under the active tab (it slides), the tick at the focused row's left edge (it slides), the focused row kept in view
            int at = optionsPage ? optTab : ctlTab;
            if (at >= 0 && at < tabEls.Count) { var tl = tabEls[at].el.layout; if (!float.IsNaN(tl.width) && tl.width > 0) { tabLine.style.left = tl.x; tabLine.style.width = tl.width; } }
            var f = Focused; bool tick = f != null && f.Row && keyFocus && IsShown(f.El) && !(f.Disabled?.Invoke() ?? false);
            if (tick)
            {
                var rb = f.El.worldBound; var cb = bodyClip.worldBound; float k = cb.height > 0 && bodyClip.layout.height > 0 ? bodyClip.layout.height / cb.height : 1f;
                float ry = (rb.y - cb.y) * k, rx = (rb.x - cb.x) * k, rh = rb.height * k, ins = F.lightline.line.tickInsetRef;
                if (!float.IsNaN(ry) && rh > 0)
                {
                    if (ry < 0 && max > 0) scroll = Mathf.Max(0, scroll + ry); else if (ry + rh > bodyH && max > 0) scroll = Mathf.Min(max, scroll + ry + rh - bodyH);
                    rowTick.style.top = ry + ins; rowTick.style.left = rx; rowTick.style.height = rh - 2 * ins;
                }
            }
            rowTick.style.opacity = tick ? 1f : 0f;
        }

        void PanelShow(bool options)
        {
            optionsPage = options; scroll = 0; var ll = F.lightline; var sh = SH;
            SetText(panelTitle, ll.type.title, options ? "Options" : "Controls");
            SetText(resetLabel, ll.type.action, options ? "Reset page" : "Reset all");
            foreach (var t in tabEls) t.el.RemoveFromHierarchy(); tabEls.Clear();
            var names = options ? OptTabs : F.controlsPage.tabs;
            for (int i = 0; i < names.Length; i++)
            {
                int ix = i;
                // a tab is its word: 74 % idle, 100 % active (the sliding line under it), pointer over: 92 % and a 1 ref cream 40 % underline
                var tb = E(tabsRow, h: sh.tabH, pick: true); tb.style.paddingLeft = sh.tabPadX; tb.style.paddingRight = sh.tabPadX; tb.style.marginRight = sh.tabGap; tb.style.justifyContent = Justify.Center; tb.style.alignItems = Align.Center; tb.name = "tab-" + names[i].ToLowerInvariant().Replace(' ', '-');
                var l = T(tb, ll.type.tab, names[i]); l.style.unityTextAlign = TextAnchor.MiddleCenter; Trans(tb, ("opacity", ll.timing.labelFade, EasingMode.EaseOut, 0f));
                var hov = E(tb); hov.name = "tab-hover"; hov.style.position = Position.Absolute; hov.style.left = 0; hov.style.right = 0; hov.style.bottom = -1; hov.style.height = 1; hov.style.backgroundColor = C("cream", 0.4f); Show(hov, false);
                tb.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { SetTab(ix); e.StopPropagation(); } });
                tb.RegisterCallback<PointerEnterEvent>(_ => { tabHover = ix; PaintTabs(); });
                tb.RegisterCallback<PointerLeaveEvent>(_ => { if (tabHover == ix) tabHover = -1; PaintTabs(); });
                tabEls.Add((tb, l, names[i]));
            }
            tabHover = -1; SetTab(options ? optTab : ctlTab);
            FadeIn(panelCard, 0f, F.timing.screenFadeSeconds);
        }
        int tabHover = -1;
        void PaintTabs()
        {
            var a = F.lightline.alpha; int at = optionsPage ? optTab : ctlTab;
            for (int k = 0; k < tabEls.Count; k++) { bool on = k == at, hv = k == tabHover && !on; tabEls[k].el.style.opacity = on ? a.focus : hv ? a.tabHover : a.idleWord; Show(tabEls[k].el.Q("tab-hover"), hv); }
        }

        void StepTab(int d) { int n = tabEls.Count; if (n == 0) return; SetTab(((optionsPage ? optTab : ctlTab) + d + n) % n); }
        public void SetTab(int i)
        {
            if (optionsPage) optTab = i; else ctlTab = i; scroll = 0; ClashCancel(); listening = null;
            PaintTabs();
            focusables.RemoveAll(f => f.Row);
            bodyContent.Clear(); rowRefresh.Clear();
            if (optionsPage) BuildOptions(OptTabs[i].ToLowerInvariant()); else BuildControls(i);
            FocusFirst();
            bodyContent.style.opacity = 0; FadeIn(bodyContent, 0f, F.timing.pageSwitchSeconds);
        }
        public string TabName => tabEls.Count > 0 ? tabEls[optionsPage ? optTab : ctlTab].id : "";

        void ResetPage()
        {
            if (optionsPage) Settings.Current.ResetPage(OptTabs[optTab].ToLowerInvariant());
            else Settings.Current.ResetPage("controls");
            HostTable()?.ApplyBindings(Settings.Current);
            SetTab(optionsPage ? optTab : ctlTab);
        }

        readonly List<Action> rowRefresh = new();
        void PanelFrame() { PanelLayout(); foreach (var r in rowRefresh) r(); if (listening != null || clash != null) PaintListening(); }

        // ---------------------------------------------------------------- rows and widgets (light line)

        VisualElement GroupHead(string text)
        {
            // the group head: the dim word, then a hairline to the row's end
            var ll = F.lightline; var g = E(bodyContent, h: SH.groupH); g.style.flexDirection = FlexDirection.Row; g.style.alignItems = Align.Center; g.style.flexShrink = 0;
            var l = T(g, ll.type.group, text); l.style.marginLeft = SH.rowLabelX;
            var hr = E(g, h: 1); hr.style.flexGrow = 1; hr.style.marginLeft = 14; hr.style.marginTop = 1; hr.style.backgroundColor = C("panelLine", ll.line.idleHairAlpha);
            return g;
        }

        /// <summary>One row: label left (cream 90 %), the control right-aligned 14 in; a 1 ref hairline at 16 % under it. Focused: the label 100 %, the
        /// sliding tick at its left edge and the hairline turned a 2 ref turquoise-light line across the row (it draws in from the left). Disabled: label and
        /// value at 40 %, never focused.</summary>
        (VisualElement row, VisualElement ctl, Focusable fo) RowEl(string label, Func<bool> visible = null, float h = -1)
        {
            var sh = SH; var ll = F.lightline; float rh = h > 0 ? h : sh.rowH;
            var row = E(bodyContent, h: rh, pick: true); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.flexShrink = 0; row.name = "row-" + label.ToLowerInvariant().Replace(' ', '-');
            var hair = E(row); hair.style.position = Position.Absolute; hair.style.left = 0; hair.style.right = 0; hair.style.bottom = 0; hair.style.height = 1; hair.style.backgroundColor = C("panelLine", ll.line.idleHairAlpha);
            var ul = E(row); ul.style.position = Position.Absolute; ul.style.left = 0; ul.style.bottom = -1; ul.style.height = ll.line.rowHRef; ul.style.width = new Length(0, LengthUnit.Percent); ul.style.backgroundColor = C("live");
            var l = T(row, ll.type.rowLabel, label); l.style.marginLeft = sh.rowLabelX; l.style.flexGrow = 1; l.style.flexShrink = 1; l.style.opacity = ll.alpha.idleRow;
            Trans(l, ("opacity", ll.timing.labelFade, EasingMode.EaseOut, 0f));
            var ctl = E(row); ctl.style.flexDirection = FlexDirection.Row; ctl.style.alignItems = Align.Center; ctl.style.justifyContent = Justify.FlexEnd; ctl.style.marginRight = sh.rowControlRight; ctl.style.flexShrink = 0;
            var fo = new Focusable { El = row, Row = true, Visible = visible };
            fo.Paint = on =>
            {
                bool dis = fo.Disabled?.Invoke() ?? false; on &= !dis;
                l.style.opacity = dis ? ll.alpha.disabled : on ? ll.alpha.focus : ll.alpha.idleRow; ctl.style.opacity = dis ? ll.alpha.disabled : 1f;
                hair.style.opacity = on ? 0f : 1f;
                if (on) Trans(ul, ("width", ll.timing.lineDraw, EasingMode.EaseOutCubic, ll.timing.lineDelay)); else Trans(ul, ("width", ll.timing.lineRetract, EasingMode.EaseInCubic, 0f));
                ul.style.width = new Length(on ? 100 : 0, LengthUnit.Percent);
            };
            focusables.Add(fo);
            row.RegisterCallback<PointerEnterEvent>(_ => { if (listening == null && clash == null) SetFocus(fo); });
            if (visible != null) rowRefresh.Add(() => Show(row, visible()));
            return (row, ctl, fo);
        }
        /// <summary>The lab's options (the Playground group, the Debug control rows): a development player, or a release player started with `-debug`.</summary>
        public static bool DebugOptions => debugOptions ??= Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(), "-debug") >= 0; static bool? debugOptions;

        /// <summary>Re-run a row's paint each frame (its disabled state follows another setting: Frame cap under V-sync).</summary>
        void RepaintRow(Focusable fo) { int i = focusables.IndexOf(fo); fo.Paint?.Invoke(i == focusIx && keyFocus); }

        void Slider(string label, float min, float max, float step, Func<float> get, Action<float> set, Func<float, string> fmt, Func<bool> visible = null)
        {
            var (row, ctl, fo) = RowEl(label, visible); var s = F.lightline.slider; var tm = F.lightline.timing;
            // a 220 ref rail (2 ref, chrome 38 %), turquoise-light fill, the knob = the marker's square (11, 15 focused, 17 dragging; cream, 1.5 ref ink-deep rim)
            var hit = E(ctl, w: s.railW, h: 28, pick: true); hit.style.marginRight = s.valueGap;
            var rail = E(hit, w: s.railW, h: s.railH); rail.style.position = Position.Absolute; rail.style.left = 0; rail.style.top = (28 - s.railH) / 2f; rail.style.backgroundColor = C("chrome", s.railAlpha);
            var fill = E(rail); fill.style.position = Position.Absolute; fill.style.left = 0; fill.style.top = 0; fill.style.bottom = 0; fill.style.backgroundColor = C("live");
            var knob = E(rail); knob.name = "knob"; knob.style.position = Position.Absolute; knob.style.top = s.railH / 2f; knob.style.backgroundColor = C("cream"); Border(knob, s.knobBorderRef, C("inkDeep"));
            knob.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent)); knob.style.rotate = new Rotate(45);
            Trans(knob, ("width", tm.labelFade, EasingMode.EaseOutCubic, 0f), ("height", tm.labelFade, EasingMode.EaseOutCubic, 0f));
            var val = T(ctl, F.lightline.type.rowValue, ""); val.style.width = s.valueW; val.style.unityTextAlign = TextAnchor.MiddleRight;
            bool drag = false;
            void Paint() { float v = get(), u = Mathf.InverseLerp(min, max, v); fill.style.width = u * s.railW; knob.style.left = u * s.railW; val.text = fmt(v); bool on = Focused == fo && keyFocus; float k = drag ? s.knobDragRef : on ? s.knobFocusRef : s.knobRef; knob.style.width = k; knob.style.height = k; }
            void SetAt(float x) { float u = Mathf.Clamp01(x / s.railW); float v = Mathf.Round(Mathf.Lerp(min, max, u) / step) * step; set(Mathf.Clamp(v, min, max)); Paint(); }
            hit.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0) return; drag = true; SetFocus(fo); hit.CapturePointer(e.pointerId); SetAt(e.localPosition.x); e.StopPropagation(); });
            hit.RegisterCallback<PointerMoveEvent>(e => { if (drag) SetAt(e.localPosition.x); });
            hit.RegisterCallback<PointerUpEvent>(e => { if (!drag) return; drag = false; hit.ReleasePointer(e.pointerId); Paint(); e.StopPropagation(); });
            fo.Adjust = (d, big) => { set(Mathf.Clamp(get() + d * step * (big ? 10 : 1), min, max)); Paint(); };
            rowRefresh.Add(Paint); Paint();
        }

        void Switch(string label, Func<bool> get, Action<bool> set, Func<bool> visible = null)
        {
            var (row, ctl, fo) = RowEl(label, visible); var s = F.lightline.@switch; var tm = F.lightline.timing;
            // a 20 ref box (1.5 ref chrome 60 % edge, radius 2), the marker's square inside when on, nothing when off; then the word (Off in text-dim)
            var box = E(ctl, w: s.boxRef, h: s.boxRef); Border(box, s.boxBorderRef, C("chrome", s.boxBorderAlpha), 2); box.style.marginRight = s.gapRef;
            var dia = E(box); dia.style.position = Position.Absolute; dia.style.left = new Length(50, LengthUnit.Percent); dia.style.top = new Length(50, LengthUnit.Percent); dia.style.width = s.diamondRef; dia.style.height = s.diamondRef; dia.style.backgroundColor = C("live");
            dia.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent)); dia.style.rotate = new Rotate(45);
            Trans(dia, ("scale", tm.switchFlip, EasingMode.EaseOutCubic, 0f));
            var word = T(ctl, F.lightline.type.rowValue, ""); word.style.width = s.wordW; word.style.unityTextAlign = TextAnchor.MiddleLeft;
            bool pressed = false;
            void Paint() { bool on = get(); float k = on ? (pressed ? 0.6f : 1f) : 0f; dia.style.scale = new Scale(new Vector3(k, k, 1)); Border(box, pressed ? 2f : s.boxBorderRef, pressed ? C("live") : C("chrome", s.boxBorderAlpha)); word.text = on ? "On" : "Off"; word.style.color = on ? C("cream") : C("textDim"); }
            void Flip() { set(!get()); Paint(); }
            row.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { pressed = true; Paint(); e.StopPropagation(); } });
            row.RegisterCallback<PointerLeaveEvent>(_ => { if (pressed) { pressed = false; Paint(); } });
            row.RegisterCallback<PointerUpEvent>(e => { if (e.button == 0) { pressed = false; Flip(); e.StopPropagation(); } });
            fo.Activate = Flip; fo.Adjust = (d, _) => { if (d > 0 != get()) Flip(); };
            rowRefresh.Add(Paint); Paint();
        }

        void Choice(string label, string[] options, Func<int> get, Action<int> set, Func<bool> visible = null, Func<bool> dim = null)
        {
            var (row, ctl, fo) = RowEl(label, visible); var s = F.lightline.choice; var a = F.lightline.alpha; var tm = F.lightline.timing;
            if (dim != null) fo.Disabled = dim; // light line: a dimmed choice (Frame cap under V-sync) is disabled: 40 %, never focused, keys skip it
            // chev_down (14 ref, turned) in a 28 ref hit box at each end of a 286 ref zone, the value centred between (190 ref at least, tab digits)
            VisualElement left = null, right = null; Label val = null; bool lastDim = false;
            var zone = E(ctl, w: s.zoneW, h: s.chevHit); zone.style.flexDirection = FlexDirection.Row; zone.style.alignItems = Align.Center; zone.style.justifyContent = Justify.SpaceBetween;
            VisualElement Chev(float rot, int side)
            {
                var hb = E(zone, w: s.chevHit, h: s.chevHit, pick: true); hb.style.flexShrink = 0; Trans(hb, ("opacity", tm.labelFade, EasingMode.EaseOut, 0f), ("translate", tm.valueNudge, EasingMode.EaseOut, 0f));
                var c = E(hb, w: s.chevRef, h: s.chevRef); c.name = "glyph"; c.style.position = Position.Absolute; c.style.left = (s.chevHit - s.chevRef) / 2f; c.style.top = (s.chevHit - s.chevRef) / 2f;
                var sp = Sp("icons/singles/chev_down"); if (sp) c.style.backgroundImage = new StyleBackground(sp); c.style.unityBackgroundImageTintColor = C("cream"); c.style.rotate = new Rotate(rot);
                hb.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0 || (dim?.Invoke() ?? false)) return; c.style.unityBackgroundImageTintColor = C("live"); hb.style.translate = new Translate(side * s.nudgeRef, 0); e.StopPropagation(); });
                void Release() { c.style.unityBackgroundImageTintColor = C("cream"); hb.style.translate = new Translate(0, 0); }
                hb.RegisterCallback<PointerLeaveEvent>(_ => Release());
                hb.RegisterCallback<PointerUpEvent>(e => { if (e.button != 0) return; Release(); if (dim?.Invoke() ?? false) return; Step(side); e.StopPropagation(); });
                return hb;
            }
            left = Chev(90, -1); val = T(zone, F.lightline.type.rowValue, ""); val.style.unityTextAlign = TextAnchor.MiddleCenter; val.style.flexGrow = 1; val.style.minWidth = s.valueW; right = Chev(-90, 1);
            void Paint()
            {
                int i = Mathf.Clamp(get(), 0, options.Length - 1); val.text = options[i]; bool d = dim != null && dim(); bool on = Focused == fo && keyFocus && !d;
                left.style.opacity = right.style.opacity = d ? Mathf.Pow(a.chevDisabled, 2.2f) / a.disabled : on ? a.focus : a.chevIdle; // the control already sits at 40 % when disabled. Glyph 0.2.97 #6: UI Toolkit blends opacity in linear light here (as the scrims, SrgbAlpha), so 0.25 x 0.4 read as ~50 %; 0.25^2.2 overall reads as 25 % over the dark sheet
                if (d != lastDim) { lastDim = d; RepaintRow(fo); }
            }
            void Step(int d) { int n = options.Length; set((get() + d + n) % n); Paint(); }
            fo.Activate = () => Step(1); fo.Adjust = (d, _) => Step(d);
            rowRefresh.Add(Paint); Paint();
        }

        // ---------------------------------------------------------------- Options pages

        static Settings S => Settings.Current;
        static void Save() => Settings.Current.Save(true);
        static string Pct(float v) => $"{Mathf.RoundToInt(v)}%";
        HandoverHost Host => FindAnyObjectByType<HandoverHost>();
        PlayControls HostTable() => Host?.controls;

        Resolution[] resolutions;
        void BuildOptions(string page)
        {
            switch (page)
            {
                case "display":
                {
                    var d = S.display;
                    Choice("Window mode", new[] { "Windowed", "Borderless", "Fullscreen" }, () => d.windowMode switch { "windowed" => 0, "fullscreen" => 2, _ => 1 }, i => { d.windowMode = i switch { 0 => "windowed", 2 => "fullscreen", _ => "borderless" }; Save(); });
                    resolutions ??= UnityEngine.Screen.resolutions.GroupBy(r => (r.width, r.height)).Select(g => g.First()).OrderByDescending(r => r.width * r.height).ToArray();
                    var resNames = new[] { "Native" }.Concat(resolutions.Select(r => $"{r.width} × {r.height}")).ToArray();
                    Choice("Resolution", resNames, () => { if (d.width <= 0) return 0; int i = Array.FindIndex(resolutions, r => r.width == d.width && r.height == d.height); return i < 0 ? 0 : i + 1; },
                        i => { if (i == 0) { d.width = d.height = 0; } else { d.width = resolutions[i - 1].width; d.height = resolutions[i - 1].height; } Save(); });
                    Slider("Render scale", 50, 100, 5, () => d.renderScale, v => { d.renderScale = Mathf.RoundToInt(v); Save(); }, Pct);
                    Choice("Quality", new[] { "Low", "Balanced", "High" }, () => d.quality switch { "low" => 0, "high" => 2, _ => 1 }, i => { d.quality = i switch { 0 => "low", 2 => "high", _ => "balanced" }; Save(); });
                    Switch("V-sync", () => d.vsync, v => { d.vsync = v; Save(); });
                    var caps = new[] { 30, 60, 120, 0 };
                    Choice("Frame cap", new[] { "30", "60", "120", "Off" }, () => Math.Max(0, Array.IndexOf(caps, d.frameCap)), i => { d.frameCap = caps[i]; Save(); }, dim: () => d.vsync);
                    Slider("HUD scale", 80, 150, 5, () => d.hudScale, v => { d.hudScale = Mathf.RoundToInt(v); Save(); }, Pct);
                    break;
                }
                case "audio":
                {
                    var a = S.audio;
                    Slider("Master", 0, 100, 5, () => a.master, v => { a.master = Mathf.RoundToInt(v); Save(); }, v => $"{Mathf.RoundToInt(v)}");
                    Slider("Music", 0, 100, 5, () => a.music, v => { a.music = Mathf.RoundToInt(v); Save(); }, v => $"{Mathf.RoundToInt(v)}");
                    Slider("Effects", 0, 100, 5, () => a.effects, v => { a.effects = Mathf.RoundToInt(v); Save(); }, v => $"{Mathf.RoundToInt(v)}");
                    Slider("Voices", 0, 100, 5, () => a.voices, v => { a.voices = Mathf.RoundToInt(v); Save(); }, v => $"{Mathf.RoundToInt(v)}");
                    break;
                }
                case "camera":
                {
                    var c = S.camera;
                    GroupHead("Commander view");
                    Slider("Pan speed", 6, 36, 1, () => c.panSpeed, v => { c.panSpeed = v; Save(); }, v => $"{Mathf.RoundToInt(v)}");
                    Switch("Edge pan", () => c.edgePan, v => { c.edgePan = v; Save(); });
                    Slider("Orbit sensitivity", 0.25f, 2f, 0.05f, () => c.orbitSensitivity, v => { c.orbitSensitivity = v; Save(); }, v => v.ToString("0.00", CultureInfo.InvariantCulture));
                    Switch("Invert orbit", () => c.invertOrbit, v => { c.invertOrbit = v; Save(); });
                    Switch("Trackpad drag orbit", () => c.trackpadOrbit, v => { c.trackpadOrbit = v; Save(); });
                    GroupHead("Third person");
                    Choice("Camera distance", new[] { "Close", "Default", "Far" }, () => c.thirdPersonDistance switch { "close" => 0, "far" => 2, _ => 1 }, i => { c.thirdPersonDistance = i switch { 0 => "close", 2 => "far", _ => "default" }; Save(); });
                    Switch("Camera settle", () => c.settle, v => { c.settle = v; Save(); });
                    break;
                }
                case "gameplay":
                {
                    var g = S.gameplay; bool Hold() => g.clickScheme == (int)ControlPreset.HoldActs;
                    GroupHead("Orders");
                    // the scheme's order in the choice: the default first
                    var schemes = new[] { ControlPreset.HoldActs, ControlPreset.DoubleClick, ControlPreset.Built };
                    Choice("Click scheme", new[] { "Click walks, hold acts", "Double click", "As built" }, () => Math.Max(0, Array.IndexOf(schemes, (ControlPreset)g.clickScheme)), i => { g.clickScheme = (int)schemes[i]; Save(); });
                    Choice("Orders while paused", new[] { "Start together", "Start as given" }, () => g.planTogether ? 0 : 1, i => { g.planTogether = i == 0; Save(); }, Hold);
                    Switch("Hints", () => g.hints, v => { g.hints = v; Save(); });
                    var speeds = new[] { 1, 2, 4, 8 };
                    Choice("Default game speed", new[] { "×1", "×2", "×4", "×8" }, () => Math.Max(0, Array.IndexOf(speeds, g.defaultSpeed)), i => { g.defaultSpeed = speeds[i]; Save(); });
                    Choice("Conversations", new[] { "Always", "First time for each scene", "Never" }, () => g.conversations, i => { g.conversations = i; Save(); });
                    GroupHead("Fights");
                    Choice("Agents near you", new[] { "Fire with you", "Lock your target" }, () => g.followLock ? 1 : 0, i => { g.followLock = i == 1; Save(); });
                    Choice("Map fire", new[] { "Until down", "While held" }, () => g.commanderHoldFire ? 1 : 0, i => { g.commanderHoldFire = i == 1; Save(); }, () => !Hold());
                    if (DebugOptions) // Glyph's 0.2.97 review #6: the lab's switches (lab terms) stay out of the shipped Options; a development player or `-debug` shows them
                    {
                        GroupHead("Playground");
                        Switch("Action plate", () => g.actionPlate, v => { g.actionPlate = v; Save(); }, () => !Hold());
                        Choice("Right-click", new[] { "Ring", "Quick action" }, () => g.quickAction ? 1 : 0, i => { g.quickAction = i == 1; Save(); }, () => !Hold());
                        Switch("Third person: left click acts", () => g.clickActs, v => { g.clickActs = v; Save(); }, () => !Hold());
                        Choice("Third person: aim", new[] { "Hold", "Toggle" }, () => g.aimMode, i => { g.aimMode = i; Save(); });
                        Choice("Third person: right button", new[] { "Right hold fires", "Right aims, left fires" }, () => g.tpRightFires ? 0 : 1, i => { g.tpRightFires = i == 0; Save(); }, Hold);
                    }
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- Controls: the one binding table

        /// <summary>The page's rows by tab: (action, friendly name). Debug keys show only in a development build.</summary>
        static readonly (string tab, PlayAction a, string name)[] ControlRows =
        {
            ("Command", PlayAction.Agent1, "Select agent 1"), ("Command", PlayAction.Agent2, "Select agent 2"), ("Command", PlayAction.Agent3, "Select agent 3"), ("Command", PlayAction.Agent4, "Select agent 4"),
            ("Command", PlayAction.SelectAll, "Select all"), ("Command", PlayAction.CycleAgent, "Next agent"), ("Command", PlayAction.AddToGroup, "Add to group"),
            ("Command", PlayAction.Verb1, "Pick action 1"), ("Command", PlayAction.Verb2, "Pick action 2"), ("Command", PlayAction.Verb3, "Pick action 3"), ("Command", PlayAction.Verb4, "Pick action 4"),
            ("Command", PlayAction.Cancel, "Cancel"), ("Command", PlayAction.Gait, "Walk or run"),
            ("Command", PlayAction.Pause, "Pause"), ("Command", PlayAction.Faster, "Faster"), ("Command", PlayAction.Slower, "Slower"), ("Command", PlayAction.NextEvent, "Next event"),
            ("Command", PlayAction.Briefing, "Briefing"), ("Command", PlayAction.DialogueLog, "History"), ("Command", PlayAction.Intel, "Intel"), ("Command", PlayAction.Response, "Response list"),
            ("Command", PlayAction.Hints, "Hints"), ("Command", PlayAction.Acknowledge, "Acknowledge"), ("Command", PlayAction.Peek, "Detail (hold)"), ("Command", PlayAction.AllStrips, "All strips"), ("Command", PlayAction.Restart, "Play again"),
            ("Camera", PlayAction.PanUp, "Pan up"), ("Camera", PlayAction.PanDown, "Pan down"), ("Camera", PlayAction.PanLeft, "Pan left"), ("Camera", PlayAction.PanRight, "Pan right"),
            ("Camera", PlayAction.TurnLeft, "Turn left"), ("Camera", PlayAction.TurnRight, "Turn right"), ("Camera", PlayAction.TurnDrag, "Free turn (middle drag)"), ("Camera", PlayAction.FollowView, "Follow view"),
            ("Fights", PlayAction.Draw, "Draw or holster"), ("Fights", PlayAction.Force, "Force fire"), // engine 0.35 (one input, GDD v0.30.1 §10): Cease (X) and Stance (V) are retired: no rows
            ("Fights", PlayAction.TargetMode, "Target mode"), ("Fights", PlayAction.StepTarget, "Step target"),
            ("Third person", PlayAction.Act, "Act"),
            ("Debug", PlayAction.Debug, "Debug"), ("Debug", PlayAction.HudView, "HUD view"), ("Debug", PlayAction.CameraLab, "Camera lab"),
        };
        public static string FriendlyName(PlayAction a) { foreach (var r in ControlRows) if (r.a == a) return r.name; return a.ToString(); }

        /// <summary>The page's own copy of the table (the defaults with the player's changes), the one the game reads after a save.</summary>
        PlayControls model = new();
        readonly Dictionary<(PlayAction, int), VisualElement> keycaps = new();

        void BuildControls(int tab)
        {
            model.ApplyBindings(Settings.Current); keycaps.Clear();
            var tabName = F.controlsPage.tabs[Mathf.Clamp(tab, 0, F.controlsPage.tabs.Length - 1)];
            var rows = ControlRows.Where(r => r.tab == tabName || DebugOptions && tabName == "Command" && r.tab == "Debug").ToList();
            var cp = F.controlsPage; int perCol = Mathf.CeilToInt(rows.Count / (float)Mathf.Max(1, cp.columns));
            var cols = E(bodyContent); cols.style.flexDirection = FlexDirection.Row;
            var bodyW = SH.wRef - 2 * SH.padX; float gap = SH.colGap; float colW = (bodyW - gap * (cp.columns - 1)) / cp.columns; // light line: two columns, 44 apart
            var saved = bodyContent;
            for (int c = 0; c < cp.columns; c++)
            {
                var col = E(cols, w: colW); col.style.marginRight = c < cp.columns - 1 ? gap : 0; col.style.flexDirection = FlexDirection.Column;
                bodyContent = col;
                foreach (var r in rows.Skip(c * perCol).Take(perCol)) ControlRow(r.a, r.name);
            }
            bodyContent = saved;
        }

        void ControlRow(PlayAction a, string name)
        {
            var (row, ctl, fo) = RowEl(name, null, SH.controlsRowH); row.name = "ctl-" + a;
            var b = model.BindingOf(a); var k = F.controls.keycap;
            var keys = b.keys ?? Array.Empty<Key>();
            for (int i = keys.Length - 1; i >= 1; i--) AliasCap(ctl, a, i, keys[i]);
            var cap = Cap(ctl, a, 0, PlayControls.Chord(keys.Length > 0 ? keys[0] : Key.None, b.mods), true);
            fo.Activate = () => Listen(a, 0);
            row.RegisterCallback<PointerUpEvent>(e => { if (e.button == 0 && listening == null) { Listen(a, 0); e.StopPropagation(); } });
        }

        /// <summary>Light line keycaps: primary 28 ref tall, at least 48 wide, a 1.5 ref chrome edge at 85 %, ink-deep 45 % fill, radius 3, cream text; alias 22
        /// ref, chrome edge at 35 %, no fill, text-dim 12. No plate sprites.</summary>
        VisualElement Cap(VisualElement parent, PlayAction a, int slot, string text, bool primary)
        {
            var k = F.lightline.keycap; var ll = F.lightline;
            var cap = E(parent, h: primary ? k.hRef : k.altH, pick: true); cap.name = primary ? "cap" : "cap-alt";
            cap.style.minWidth = primary ? k.minW : 0; cap.style.paddingLeft = cap.style.paddingRight = primary ? k.padX : k.altPadX; cap.style.justifyContent = Justify.Center; cap.style.alignItems = Align.Center; cap.style.marginLeft = k.gapRef;
            var l = T(cap, primary ? ll.type.keycap : ll.type.keycapAlt, text); l.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (text == "`") l.style.fontSize = 18;
            var badge = E(cap, "s-mark_half", w: ll.clash.badgeRef, h: ll.clash.badgeRef); badge.style.position = Position.Absolute; badge.style.right = -8; badge.style.top = -10; Show(badge, false); badge.name = "clash-badge";
            cap.userData = l; keycaps[(a, slot)] = cap; CapIdle(cap, primary);
            if (!primary) cap.RegisterCallback<PointerUpEvent>(e => { if (e.button == 0 && listening == null) { Listen(a, slot); e.StopPropagation(); } });
            return cap;
        }
        void CapIdle(VisualElement cap, bool primary)
        {
            var k = F.lightline.keycap; Border(cap, k.borderRef, C("chrome", primary ? k.borderAlpha : k.altBorderAlpha), k.radiusRef);
            cap.style.backgroundColor = primary ? C("inkDeep", k.fillAlpha) : Color.clear; cap.style.minWidth = primary ? k.minW : 0;
            if (cap.userData is Label l) { var t = primary ? F.lightline.type.keycap : F.lightline.type.keycapAlt; l.style.fontSize = l.text == "`" ? 18 : t.sizeRef; l.style.color = C(primary ? "cream" : "textDim"); }
        }
        void AliasCap(VisualElement parent, PlayAction a, int slot, Key key) => Cap(parent, a, slot, PlayControls.KeyName(key), false);

        // ---------------------------------------------------------------- rebinding: press a key; a clash is shown and resolved, never silent

        public (PlayAction a, int slot)? listening;
        float listenAt; Key? pendingMod;
        public void Listen(PlayAction a, int slot)
        {
            ClashCancel(); listening = (a, slot); listenAt = Time.unscaledTime; pendingMod = null; PaintListening();
            var fo = focusables.FirstOrDefault(f => f.El?.name == "ctl-" + a); if (fo != null) { focusIx = focusables.IndexOf(fo); keyFocus = true; PaintFocus(); } // the rebind row carries the focus fill and tick
        }
        /// <summary>Listening: the primary shows "Press a key" in a 2 ref turquoise-light edge blinking to 45 % and back every 1.0 s (ink-deep 60 % fill); Esc
        /// cancels. A clash: both caps a 2 ref cream edge and the half badge.</summary>
        void PaintListening()
        {
            var k = F.lightline.keycap; float blink = 0.725f + 0.275f * Mathf.Cos(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.1f, F.lightline.timing.listenBlink));
            foreach (var kv in keycaps)
            {
                bool on = listening is var (la, ls) && kv.Key.Item1 == la && kv.Key.Item2 == ls; bool primary = kv.Key.Item2 == 0;
                var l = (Label)kv.Value.userData; var b = model.BindingOf(kv.Key.Item1); var ks = b.keys ?? Array.Empty<Key>();
                bool clashed = kv.Value.Q("clash-badge")?.resolvedStyle.display == DisplayStyle.Flex || kv.Value.Q("clash-badge")?.style.display == DisplayStyle.Flex;
                if (on)
                {
                    l.text = "Press a key"; l.style.color = C("cream", 0.78f); l.style.fontSize = k.listenSizeRef;
                    Border(kv.Value, 2f, C("live", blink), k.radiusRef); kv.Value.style.backgroundColor = C("inkDeep", k.listenFillAlpha); kv.Value.style.minWidth = k.listenMinW;
                }
                else
                {
                    if (clash == null && kv.Key.Item2 < ks.Length) l.text = primary ? PlayControls.Chord(ks[0], b.mods) : PlayControls.KeyName(ks[kv.Key.Item2]);
                    CapIdle(kv.Value, primary);
                    if (clash != null && clashed) Border(kv.Value, 2f, C("cream"), k.radiusRef);
                }
            }
        }

        static bool IsMod(Key k) => k is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftCommand or Key.RightCommand;

        void ListenFrame(Keyboard kb)
        {
            if (Time.unscaledTime - listenAt < 0.05f) return;
            if (kb.escapeKey.wasPressedThisFrame) { listening = null; PaintListening(); return; }
            var (a, slot) = listening.Value;
            if (kb.backspaceKey.wasPressedThisFrame && slot > 0) { RemoveAlias(a, slot); listening = null; RebuildTab(); return; }
            foreach (var kc in kb.allKeys)
            {
                if (kc == null || !kc.wasPressedThisFrame) continue;
                var key = kc.keyCode; if (key == Key.Escape || key == Key.None) continue;
                if (IsMod(key)) { pendingMod = key; continue; }
                Offer(a, slot, key); return;
            }
            if (pendingMod is Key pm && !kb[pm].isPressed) Offer(a, slot, pm); // a modifier pressed and let go alone: it is the key (Add to group, Force fire, Detail)
        }

        /// <summary>The key pressed for (action, slot): taken at once, or the clash strip when another action in the same context has it with the same mods.</summary>
        public void Offer(PlayAction a, int slot, Key key)
        {
            listening = null; var b = model.BindingOf(a);
            var other = model.ClashOf(a, key, b.mods ?? "", b.context ?? "");
            if (other is PlayAction o) { ShowClash(a, slot, key, o); return; }
            Bind(a, slot, key); RebuildTab();
        }

        void Bind(PlayAction a, int slot, Key key)
        {
            var b = model.BindingOf(a); var ks = (b.keys ?? Array.Empty<Key>()).ToList();
            if (slot < ks.Count) ks[slot] = key; else ks.Add(key);
            Settings.Current.SetBinding(a, ks.ToArray(), b.mods ?? ""); model.ApplyBindings(Settings.Current);
        }
        void RemoveAlias(PlayAction a, int slot)
        {
            var b = model.BindingOf(a); var ks = (b.keys ?? Array.Empty<Key>()).ToList(); if (slot <= 0 || slot >= ks.Count) return;
            ks.RemoveAt(slot); Settings.Current.SetBinding(a, ks.ToArray(), b.mods ?? ""); model.ApplyBindings(Settings.Current); Save();
        }
        void RebuildTab() { Save(); float sc = scroll; SetTab(ctlTab); scroll = sc; }

        // the clash strip: "E already turns the view right" style, Swap / Cancel
        VisualElement clashStrip; Label clashText; public (PlayAction a, int slot, Key key, PlayAction other)? clash; Focusable clashCancelFo;
        /// <summary>Light line: 56 ref tall, a 1 ref chrome 50 % rule on top, ink-deep 35 % fill, the 26 ref half badge at left, "N already: Next event"
        /// (Jost Regular 16.5), and two word actions, Swap and Cancel (Cancel focused by default).</summary>
        void BuildClashStrip()
        {
            var cs = F.lightline.clash; var ll = F.lightline; var sh = SH;
            clashStrip = E(panelCard, h: cs.stripH, pick: true); clashStrip.style.position = Position.Absolute; clashStrip.style.left = sh.padX; clashStrip.style.right = sh.padX; clashStrip.name = "clash-strip";
            clashStrip.style.borderTopWidth = 1; clashStrip.style.borderTopColor = C("chrome", cs.ruleAlpha); clashStrip.style.backgroundColor = C("inkDeep", cs.fillAlpha);
            var mark = E(clashStrip, "s-mark_half", w: cs.stripBadgeRef, h: cs.stripBadgeRef); mark.style.position = Position.Absolute; mark.style.left = 14; mark.style.top = (cs.stripH - cs.stripBadgeRef) / 2f;
            clashText = T(clashStrip, ll.type.clash, ""); clashText.style.position = Position.Absolute; clashText.style.left = 56; clashText.style.top = 0; clashText.style.bottom = 0; clashText.style.unityTextAlign = TextAnchor.MiddleLeft;
            var swap = WordButton(clashStrip, "Swap", ll.type.action, 120, 40, sh.footerInset, sh.footerGutter, -1f, sh.footerLineY, ClashSwap, out _).Root; swap.style.position = Position.Absolute; swap.style.right = 150; swap.style.top = 8; swap.name = "clash-swap";
            var cancel = WordButton(clashStrip, "Cancel", ll.type.action, 130, 40, sh.footerInset, sh.footerGutter, -1f, sh.footerLineY, ClashCancel, out clashCancelFo).Root; cancel.style.position = Position.Absolute; cancel.style.right = 12; cancel.style.top = 8; cancel.name = "clash-cancel";
            Show(clashStrip, false);
        }
        void ShowClash(PlayAction a, int slot, Key key, PlayAction other)
        {
            clash = (a, slot, key, other); PaintListening(); // the prompt ends: the keycap shows the offered key, badged, until Swap or Cancel
            if (keycaps.TryGetValue((a, slot), out var pend) && pend.userData is Label pl) pl.text = slot == 0 ? PlayControls.Chord(key, model.BindingOf(a).mods) : PlayControls.KeyName(key);
            clashText.text = $"{PlayControls.Chord(key, model.BindingOf(a).mods)} already: {FriendlyName(other)}";
            Show(clashStrip, true);
            foreach (var kv in keycaps) Show(kv.Value.Q("clash-badge"), (kv.Key.Item1 == a && kv.Key.Item2 == slot) || (kv.Key.Item1 == other && (model.BindingOf(other).keys ?? Array.Empty<Key>()).ElementAtOrDefault(kv.Key.Item2) == key));
            PaintListening(); SetFocus(clashCancelFo); // Cancel focused by default: it keeps the old binding
        }
        /// <summary>Swap: the action gets the key, the other action gets the key this slot had.</summary>
        public void ClashSwap()
        {
            if (clash is not var (a, slot, key, other)) return;
            var mine = model.BindingOf(a).keys ?? Array.Empty<Key>(); Key old = slot < mine.Length ? mine[slot] : Key.None;
            var theirs = (model.BindingOf(other).keys ?? Array.Empty<Key>()).ToList(); int oi = theirs.IndexOf(key);
            if (oi >= 0) { if (old != Key.None) theirs[oi] = old; else theirs.RemoveAt(oi); }
            Settings.Current.SetBinding(other, theirs.ToArray(), model.BindingOf(other).mods ?? ""); model.ApplyBindings(Settings.Current);
            Bind(a, slot, key);
            clash = null; Show(clashStrip, false); RebuildTab();
        }
        public void ClashCancel()
        {
            if (clashStrip == null) return;
            var was = clash; clash = null; Show(clashStrip, false); foreach (var kv in keycaps) Show(kv.Value.Q("clash-badge"), false);
            PaintListening();
            if (was.HasValue) { var fo = focusables.FirstOrDefault(f => f.El?.name == "ctl-" + was.Value.a); if (fo != null) SetFocus(fo); } // the focus back on the row
        }

        // ---------------------------------------------------------------- opened from the pause menu

        /// <summary>The pause menu's Options and Controls: the panel over the frozen game; Back (or Esc, or the corner) returns to the pause menu.</summary>
        public static void OpenFromPause(bool options) { Ensure().Show(options ? Screen.Options : Screen.Controls, pause: true); }
    }
}
