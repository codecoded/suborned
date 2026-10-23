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
        VisualElement panelCard, tabsRow, bodyClip, bodyContent, scrollBar, footerReset; Label panelTitle, resetLabel;
        readonly List<(VisualElement el, Label l, string id)> tabEls = new();
        bool optionsPage; int optTab, ctlTab; float scroll;
        static readonly string[] OptTabs = { "Display", "Audio", "Camera", "Gameplay" };

        void BuildPanel()
        {
            panelGroup = E(blocker); Fill(panelGroup);
            var p = F.panel;
            panelCard = E(panelGroup, "s-panel_dark_modal", pick: true); panelCard.style.position = Position.Absolute; panelCard.style.width = p.widthRef; panelCard.name = "panel-card";
            panelCard.RegisterCallback<WheelEvent>(e => { scroll += e.delta.y * 24f; e.StopPropagation(); });
            panelTitle = T(panelCard, F.type.panelTitle, "Options"); panelTitle.style.position = Position.Absolute; panelTitle.style.left = p.padRef[3]; panelTitle.style.top = p.titleY - 8;
            var close = E(panelCard, "s-btn_close_normal", w: p.closeRef, h: p.closeRef, pick: true); close.style.position = Position.Absolute; close.style.right = p.closeInset; close.style.top = p.closeInset;
            close.RegisterCallback<PointerEnterEvent>(_ => { close.RemoveFromClassList("s-btn_close_normal"); close.AddToClassList("s-btn_close_hover"); });
            close.RegisterCallback<PointerLeaveEvent>(_ => { close.RemoveFromClassList("s-btn_close_hover"); close.AddToClassList("s-btn_close_normal"); });
            close.RegisterCallback<PointerUpEvent>(e => { if (e.button == 0) { Back(); e.StopPropagation(); } });
            tabsRow = E(panelCard); tabsRow.style.position = Position.Absolute; tabsRow.style.left = p.padRef[3]; tabsRow.style.top = p.tabsY; tabsRow.style.flexDirection = FlexDirection.Row;
            var tabRule = E(panelCard); tabRule.style.position = Position.Absolute; tabRule.style.left = p.padRef[3]; tabRule.style.right = p.padRef[1]; tabRule.style.top = p.tabsY + p.tabH + 12; tabRule.style.height = 1; tabRule.style.backgroundColor = C("panelLine", 0.22f);
            bodyClip = E(panelCard); bodyClip.style.position = Position.Absolute; bodyClip.style.left = p.padRef[3]; bodyClip.style.right = p.padRef[1]; bodyClip.style.top = p.bodyTop; bodyClip.style.overflow = Overflow.Hidden;
            bodyContent = E(bodyClip); bodyContent.style.position = Position.Absolute; bodyContent.style.left = 0; bodyContent.style.right = 0; bodyContent.style.top = 0;
            scrollBar = E(panelCard); scrollBar.style.position = Position.Absolute; scrollBar.style.right = p.padRef[1] - 14; scrollBar.style.width = 4; scrollBar.style.backgroundColor = C("chrome", 0.5f); Radius(scrollBar, 2);
            var foot = E(panelCard); foot.style.position = Position.Absolute; foot.style.left = p.padRef[3]; foot.style.right = p.padRef[1]; foot.style.bottom = p.footer.bottomInset; foot.style.height = p.footer.lineY - p.footer.bottomInset;
            var fl = E(foot); fl.style.position = Position.Absolute; fl.style.left = 0; fl.style.right = 0; fl.style.top = 0; fl.style.height = 1; fl.style.backgroundColor = C("panelLine", 0.22f);
            footerReset = Button(foot, "Reset page", p.footer.resetW, p.footer.resetH, ResetPage, F.type.tab, out _, small: true); footerReset.style.position = Position.Absolute; footerReset.style.left = 0; footerReset.style.bottom = (p.footer.backH - p.footer.resetH) / 2f; footerReset.name = "panel-reset";
            resetLabel = (Label)footerReset.userData;
            var back = Button(foot, "Back", p.footer.backW, p.footer.backH, Back, F.type.button, out _); back.style.position = Position.Absolute; back.style.right = 0; back.style.bottom = 0; back.name = "panel-back";
            BuildClashStrip();
        }

        void PanelLayout()
        {
            var p = F.panel; float h = Mathf.Min(p.heightRef, H - 96f);
            panelCard.style.height = h; panelCard.style.left = (W - p.widthRef) / 2f; panelCard.style.top = (H - h) / 2f;
            float bodyH = h - p.bodyTop - p.bodyBottomInset; bodyClip.style.height = bodyH;
            float contentH = bodyContent.layout.height; if (float.IsNaN(contentH)) contentH = 0;
            float max = Mathf.Max(0f, contentH - bodyH); scroll = Mathf.Clamp(scroll, 0f, max);
            bodyContent.style.top = -scroll;
            Show(scrollBar, max > 1f);
            if (clashStrip != null && clashStrip.resolvedStyle.display != DisplayStyle.None)
            {   // Glyph 0.2.85 #9: between the last row and the footer line (it covered "Walk or run")
                float sh = F.controlsPage.clashStrip.h, top = p.bodyTop + Mathf.Min(contentH - scroll, bodyH) + 2f, maxTop = h - p.footer.lineY - sh - 2f;
                clashStrip.style.bottom = StyleKeyword.Auto; clashStrip.style.top = Mathf.Min(top, maxTop);
            }
            if (max > 1f) { float barH = Mathf.Max(24f, bodyH * bodyH / contentH); scrollBar.style.height = barH; scrollBar.style.top = p.bodyTop + (bodyH - barH) * (scroll / max); }
        }

        void PanelShow(bool options)
        {
            optionsPage = options; scroll = 0;
            SetText(panelTitle, F.type.panelTitle, options ? "Options" : "Controls");
            SetText(resetLabel, F.type.tab, options ? "Reset page" : "Reset all");
            foreach (var t in tabEls) t.el.RemoveFromHierarchy(); tabEls.Clear();
            var names = options ? OptTabs : F.controlsPage.tabs;
            for (int i = 0; i < names.Length; i++)
            {
                int ix = i; var p = F.panel;
                var tb = E(tabsRow, "s-plate_dark", h: p.tabH, pick: true); tb.style.paddingLeft = p.tabPadX; tb.style.paddingRight = p.tabPadX; tb.style.marginRight = p.tabGap; tb.style.justifyContent = Justify.Center; tb.name = "tab-" + names[i].ToLowerInvariant().Replace(' ', '-');
                var l = T(tb, F.type.tab, names[i]); l.style.unityTextAlign = TextAnchor.MiddleCenter;
                tb.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { SetTab(ix); e.StopPropagation(); } });
                tabEls.Add((tb, l, names[i]));
            }
            SetTab(options ? optTab : ctlTab);
            FadeIn(panelCard, 0f, F.timing.screenFadeSeconds);
        }

        void StepTab(int d) { int n = tabEls.Count; if (n == 0) return; SetTab(((optionsPage ? optTab : ctlTab) + d + n) % n); }
        public void SetTab(int i)
        {
            if (optionsPage) optTab = i; else ctlTab = i; scroll = 0; ClashCancel(); listening = null;
            for (int k = 0; k < tabEls.Count; k++)
            {
                bool on = k == i; var (el, l, _) = tabEls[k];
                el.RemoveFromClassList(on ? "s-plate_dark" : "s-plate_light"); el.AddToClassList(on ? "s-plate_light" : "s-plate_dark");
                l.style.color = C(on ? (string.IsNullOrEmpty(F.type.tab.activeColor) ? "ink" : F.type.tab.activeColor) : F.type.tab.color);
            }
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
        void PanelFrame() { PanelLayout(); foreach (var r in rowRefresh) r(); if (listening != null) PaintListening(); }

        // ---------------------------------------------------------------- rows and widgets

        VisualElement GroupHead(string text)
        {
            var g = E(bodyContent, h: F.panel.groupH); g.style.justifyContent = Justify.FlexEnd; g.style.paddingBottom = 8;
            var l = T(g, F.type.group, text); l.style.marginLeft = F.panel.labelX; return g;
        }

        /// <summary>One row: label left, the control right-aligned; the row is a focusable with the ink-deep fill and the live tick on key focus.</summary>
        (VisualElement row, VisualElement ctl, Focusable fo) RowEl(string label, Func<bool> visible = null, float h = -1)
        {
            var p = F.panel; float rh = h > 0 ? h : p.rowH;
            var row = E(bodyContent, h: rh, pick: true); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.flexShrink = 0;
            var line = E(row); line.style.position = Position.Absolute; line.style.left = 0; line.style.right = 0; line.style.bottom = 0; line.style.height = 1; line.style.backgroundColor = C("panelLine", 0.14f);
            var fill = E(row); Fill(fill); fill.style.backgroundColor = C("inkDeep", p.rowFocus.alpha); Radius(fill, p.rowFocus.radiusRef);
            var tick = E(fill); tick.style.position = Position.Absolute; tick.style.left = 0; tick.style.top = 6; tick.style.bottom = 6; tick.style.width = p.rowFocus.tickRef; tick.style.backgroundColor = C("live"); Radius(tick, 1.5f);
            Show(fill, false);
            row.RegisterCallback<PointerEnterEvent>(_ => { if (!keyFocus) Show(fill, true); });
            row.RegisterCallback<PointerLeaveEvent>(_ => { if (Focused?.Ring != fill || !keyFocus) Show(fill, false); });
            var l = T(row, F.type.rowLabel, label); l.style.marginLeft = p.labelX; l.style.flexGrow = 1; l.style.flexShrink = 1;
            var ctl = E(row); ctl.style.flexDirection = FlexDirection.Row; ctl.style.alignItems = Align.Center; ctl.style.justifyContent = Justify.FlexEnd; ctl.style.marginRight = p.controlRight; ctl.style.flexShrink = 0;
            var fo = new Focusable { El = row, Ring = fill, Row = true, Visible = visible }; focusables.Add(fo);
            if (visible != null) rowRefresh.Add(() => Show(row, visible()));
            return (row, ctl, fo);
        }

        void Slider(string label, float min, float max, float step, Func<float> get, Action<float> set, Func<float, string> fmt, Func<bool> visible = null)
        {
            var (row, ctl, fo) = RowEl(label, visible); var s = F.controls.slider;
            var track = E(ctl, w: s.width, h: s.trackH, pick: true); track.style.backgroundColor = C("inkDeep", s.trackAlpha); Border(track, 1, C("chrome", s.rimAlpha), s.trackH / 2f);
            var fill = E(track); fill.style.position = Position.Absolute; fill.style.left = 0; fill.style.top = 0; fill.style.bottom = 0; fill.style.backgroundColor = C("live"); Radius(fill, s.trackH / 2f);
            var knob = E(track, "s-disc_light_24", w: s.knobRef, h: s.knobRef); knob.style.position = Position.Absolute; knob.style.top = (s.trackH - s.knobRef) / 2f - 1;
            var val = T(ctl, F.type.rowValue, ""); val.style.width = s.valueW; val.style.unityTextAlign = TextAnchor.MiddleRight; val.style.marginLeft = 14;
            void Paint() { float v = get(), u = Mathf.InverseLerp(min, max, v); fill.style.width = u * s.width; knob.style.left = u * s.width - s.knobRef / 2f; val.text = fmt(v); }
            void SetAt(float x) { float u = Mathf.Clamp01(x / s.width); float v = Mathf.Round(Mathf.Lerp(min, max, u) / step) * step; set(Mathf.Clamp(v, min, max)); Paint(); }
            bool drag = false;
            track.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0) return; drag = true; track.CapturePointer(e.pointerId); SetAt(e.localPosition.x); e.StopPropagation(); });
            track.RegisterCallback<PointerMoveEvent>(e => { if (drag) SetAt(e.localPosition.x); });
            track.RegisterCallback<PointerUpEvent>(e => { if (!drag) return; drag = false; track.ReleasePointer(e.pointerId); e.StopPropagation(); });
            fo.Adjust = (d, big) => { set(Mathf.Clamp(get() + d * step * (big ? 10 : 1), min, max)); Paint(); };
            rowRefresh.Add(Paint); Paint();
        }

        void Switch(string label, Func<bool> get, Action<bool> set, Func<bool> visible = null)
        {
            var (row, ctl, fo) = RowEl(label, visible); var s = F.controls.@switch;
            var pill = E(ctl, w: s.w, h: s.h, pick: true); Border(pill, 1.5f, C("chrome", s.rimAlpha), s.h / 2f); // the switch, then its word (Glyph 0.2.85)
            var word = T(ctl, F.type.rowValue, ""); word.style.width = s.wordW; word.style.unityTextAlign = TextAnchor.MiddleLeft; word.style.marginLeft = 12;
            var knob = E(pill, "s-disc_light_24", w: s.knobRef, h: s.knobRef); knob.style.position = Position.Absolute; knob.style.top = (s.h - s.knobRef) / 2f - 1.5f;
            void Paint() { bool on = get(); pill.style.backgroundColor = on ? C("live", s.onFillAlpha) : C("inkDeep", s.offFillAlpha); knob.style.left = on ? s.w - s.knobRef - 3.5f : 0.5f; word.text = on ? "On" : "Off"; word.style.color = on ? C("cream") : C("textDim"); }
            void Flip() { set(!get()); Paint(); }
            row.RegisterCallback<PointerUpEvent>(e => { if (e.button == 0) { Flip(); e.StopPropagation(); } });
            fo.Activate = Flip; fo.Adjust = (d, _) => { if (d > 0 != get()) Flip(); };
            rowRefresh.Add(Paint); Paint();
        }

        void Choice(string label, string[] options, Func<int> get, Action<int> set, Func<bool> visible = null, Func<bool> dim = null)
        {
            var (row, ctl, fo) = RowEl(label, visible); var s = F.controls.choice;
            var zone = E(ctl, w: s.w, h: 30); zone.style.flexDirection = FlexDirection.Row; zone.style.alignItems = Align.Center; zone.style.justifyContent = Justify.Center;
            VisualElement Chev(float rot) { var c = E(zone, w: s.chevRef, h: s.chevRef, pick: true); var sp = Sp("icons/singles/chev_down"); if (sp) c.style.backgroundImage = new StyleBackground(sp); c.style.unityBackgroundImageTintColor = C("cream"); c.style.rotate = new Rotate(rot); return c; }
            var left = Chev(90); var val = T(zone, F.type.rowValue, ""); val.style.unityTextAlign = TextAnchor.MiddleCenter; val.style.marginLeft = s.chevGapRef; val.style.marginRight = s.chevGapRef; var right = Chev(-90);
            var rowLabel = row.Q<Label>();
            void Paint() { int i = Mathf.Clamp(get(), 0, options.Length - 1); val.text = options[i]; bool d = dim != null && dim(); val.style.color = d ? C("textDim", 0.45f) : C("cream"); left.style.opacity = right.style.opacity = d ? 0.45f : 1f; if (rowLabel != null) rowLabel.style.opacity = d ? 0.45f : 1f; }
            void Step(int d) { int n = options.Length; set((get() + d + n) % n); Paint(); }
            left.RegisterCallback<PointerUpEvent>(e => { if (e.button == 0) { Step(-1); e.StopPropagation(); } });
            right.RegisterCallback<PointerUpEvent>(e => { if (e.button == 0) { Step(1); e.StopPropagation(); } });
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
                    Choice("Conversations", new[] { "Always", "First time", "Never" }, () => g.conversations, i => { g.conversations = i; Save(); });
                    GroupHead("Fights");
                    Choice("Agents near you", new[] { "Fire with you", "Lock your target" }, () => g.followLock ? 1 : 0, i => { g.followLock = i == 1; Save(); });
                    Choice("Map fire", new[] { "Until down", "While held" }, () => g.commanderHoldFire ? 1 : 0, i => { g.commanderHoldFire = i == 1; Save(); }, () => !Hold());
                    GroupHead("Playground");
                    Switch("Action plate", () => g.actionPlate, v => { g.actionPlate = v; Save(); }, () => !Hold());
                    Choice("Right-click", new[] { "Ring", "Quick action" }, () => g.quickAction ? 1 : 0, i => { g.quickAction = i == 1; Save(); }, () => !Hold());
                    Switch("Third person: left click acts", () => g.clickActs, v => { g.clickActs = v; Save(); }, () => !Hold());
                    Choice("Third person: aim", new[] { "Hold", "Toggle" }, () => g.aimMode, i => { g.aimMode = i; Save(); });
                    Choice("Third person: right button", new[] { "Right hold fires", "Right aims, left fires" }, () => g.tpRightFires ? 0 : 1, i => { g.tpRightFires = i == 0; Save(); }, Hold);
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
            ("Fights", PlayAction.Draw, "Draw or holster"), ("Fights", PlayAction.Cease, "Cease fire"), ("Fights", PlayAction.Stance, "Stance"), ("Fights", PlayAction.Force, "Force fire"),
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
            var rows = ControlRows.Where(r => r.tab == tabName || Debug.isDebugBuild && tabName == "Command" && r.tab == "Debug").ToList();
            var cp = F.controlsPage; int perCol = Mathf.CeilToInt(rows.Count / (float)Mathf.Max(1, cp.columns));
            var cols = E(bodyContent); cols.style.flexDirection = FlexDirection.Row;
            var bodyW = F.panel.widthRef - F.panel.padRef[1] - F.panel.padRef[3]; float colW = (bodyW - cp.columnGapRef * (cp.columns - 1)) / cp.columns;
            var saved = bodyContent;
            for (int c = 0; c < cp.columns; c++)
            {
                var col = E(cols, w: colW); col.style.marginRight = c < cp.columns - 1 ? cp.columnGapRef : 0; col.style.flexDirection = FlexDirection.Column;
                bodyContent = col;
                foreach (var r in rows.Skip(c * perCol).Take(perCol)) ControlRow(r.a, r.name);
            }
            bodyContent = saved;
        }

        void ControlRow(PlayAction a, string name)
        {
            var (row, ctl, fo) = RowEl(name, null, F.controlsPage.rowH); row.name = "ctl-" + a;
            var b = model.BindingOf(a); var k = F.controls.keycap;
            var keys = b.keys ?? Array.Empty<Key>();
            for (int i = keys.Length - 1; i >= 1; i--) AliasCap(ctl, a, i, keys[i]);
            var cap = Cap(ctl, a, 0, PlayControls.Chord(keys.Length > 0 ? keys[0] : Key.None, b.mods), true);
            fo.Activate = () => Listen(a, 0);
            row.RegisterCallback<PointerUpEvent>(e => { if (e.button == 0 && listening == null) { Listen(a, 0); e.StopPropagation(); } });
        }

        VisualElement Cap(VisualElement parent, PlayAction a, int slot, string text, bool primary)
        {
            var k = F.controls.keycap;
            var cap = E(parent, primary ? "s-plate_light" : "s-plate_dark", h: primary ? k.h : k.altH, pick: true);
            if (!primary) cap.style.height = 26; // Glyph 0.2.85 #6: aliases 26 tall, 16 each side, at least 40 wide, the keycap type less 3
            cap.style.minWidth = primary ? k.minW : 40; cap.style.paddingLeft = primary ? k.padX : 16; cap.style.paddingRight = primary ? k.padX : 16; cap.style.justifyContent = Justify.Center; cap.style.marginLeft = 6;
            var l = T(cap, F.type.keycap, text, primary ? null : "textDim"); l.style.unityTextAlign = TextAnchor.MiddleCenter; if (!primary) l.style.fontSize = F.type.keycap.sizeRef - 3;
            if (text == "`") l.style.fontSize = 18;
            var ring = E(cap); ring.style.position = Position.Absolute; ring.style.left = -3; ring.style.top = -3; ring.style.right = -3; ring.style.bottom = -3; Border(ring, 2, C("live"), 8); Show(ring, false); ring.name = "listen-ring";
            var badge = E(cap, "s-mark_half", w: F.controlsPage.clashStrip.keyBadgeRef, h: F.controlsPage.clashStrip.keyBadgeRef); badge.style.position = Position.Absolute; badge.style.right = -10; badge.style.top = -10; Show(badge, false); badge.name = "clash-badge";
            cap.userData = l; keycaps[(a, slot)] = cap;
            if (!primary) cap.RegisterCallback<PointerUpEvent>(e => { if (e.button == 0 && listening == null) { Listen(a, slot); e.StopPropagation(); } });
            return cap;
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
        void PaintListening()
        {
            foreach (var kv in keycaps)
            {
                bool on = listening is var (la, ls) && kv.Key.Item1 == la && kv.Key.Item2 == ls;
                var ring = kv.Value.Q("listen-ring"); Show(ring, on);
                var l = (Label)kv.Value.userData; var b = model.BindingOf(kv.Key.Item1); var ks = b.keys ?? Array.Empty<Key>();
                if (on) { kv.Value.RemoveFromClassList("s-plate_light"); kv.Value.AddToClassList("s-plate_dark"); l.text = "Press a key"; l.style.color = C("cream"); }
                else if (kv.Key.Item2 < ks.Length) { bool primary = kv.Key.Item2 == 0; if (primary) { kv.Value.RemoveFromClassList("s-plate_dark"); kv.Value.AddToClassList("s-plate_light"); } l.text = primary ? PlayControls.Chord(ks[0], b.mods) : PlayControls.KeyName(ks[kv.Key.Item2]); l.style.color = C(primary ? "ink" : "textDim"); }
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
        VisualElement clashStrip; Label clashText; public (PlayAction a, int slot, Key key, PlayAction other)? clash;
        void BuildClashStrip()
        {
            var cs = F.controlsPage.clashStrip;
            clashStrip = E(panelCard, "s-panel_strip", h: cs.h, pick: true); clashStrip.style.position = Position.Absolute; clashStrip.style.left = F.panel.padRef[3]; clashStrip.style.right = F.panel.padRef[1];
            clashStrip.style.bottom = F.panel.footer.lineY + 10; clashStrip.style.flexDirection = FlexDirection.Row; clashStrip.style.alignItems = Align.Center; clashStrip.style.paddingLeft = 14; clashStrip.style.paddingRight = 14; clashStrip.name = "clash-strip";
            var mark = E(clashStrip, "s-mark_half", w: cs.markRef, h: cs.markRef); mark.style.marginRight = 12; mark.style.flexShrink = 0;
            clashText = T(clashStrip, F.type.rowLabel, ""); clashText.style.flexGrow = 1;
            var swap = Button(clashStrip, "Swap", 96, 32, ClashSwap, F.type.tab, out _, small: true); swap.style.marginLeft = 10; swap.name = "clash-swap";
            var cancel = Button(clashStrip, "Cancel", 96, 32, ClashCancel, F.type.tab, out _, small: true); cancel.style.marginLeft = 10; cancel.name = "clash-cancel";
            Show(clashStrip, false);
        }
        void ShowClash(PlayAction a, int slot, Key key, PlayAction other)
        {
            clash = (a, slot, key, other); PaintListening(); // the prompt ends: the keycap shows the offered key, badged, until Swap or Cancel
            if (keycaps.TryGetValue((a, slot), out var pend) && pend.userData is Label pl) { pl.text = slot == 0 ? PlayControls.Chord(key, model.BindingOf(a).mods) : PlayControls.KeyName(key); pl.style.color = C(slot == 0 ? "ink" : "textDim"); }
            clashText.text = $"{PlayControls.Chord(key, model.BindingOf(a).mods)} already: {FriendlyName(other)}";
            Show(clashStrip, true);
            foreach (var kv in keycaps) Show(kv.Value.Q("clash-badge"), (kv.Key.Item1 == a && kv.Key.Item2 == slot) || (kv.Key.Item1 == other && (model.BindingOf(other).keys ?? Array.Empty<Key>()).ElementAtOrDefault(kv.Key.Item2) == key));
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
            clash = null; Show(clashStrip, false); foreach (var kv in keycaps) Show(kv.Value.Q("clash-badge"), false);
            PaintListening();
        }

        // ---------------------------------------------------------------- opened from the pause menu

        /// <summary>The pause menu's Options and Controls: the panel over the frozen game; Back (or Esc, or the corner) returns to the pause menu.</summary>
        public static void OpenFromPause(bool options) { Ensure().Show(options ? Screen.Options : Screen.Controls, pause: true); }
    }
}
