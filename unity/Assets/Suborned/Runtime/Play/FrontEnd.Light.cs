using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 051 direction A, "light line" (Glyph's briefs/051/lightline/SPEC.md, numbers in frontend.json `lightline`; Sky 2026-10-23 "A seems ok"):
    /// text on the dark, and only three marks move: the marker (a 9 ref square turned 45 degrees, turquoise-light), the line (2 ref turquoise-light
    /// under the focused item) and the tick (a 3 ref bar at a focused row's left edge). Words get marker + line; rows get tick + line. Nothing else
    /// changes on focus (no weight, size, position or plate), so nothing reflows. Hover = focus. The pieces here are static so the HUD's pause card
    /// draws with the same parts (FrontEnd.Shared's data and fonts).</summary>
    public sealed partial class FrontEnd
    {
        [Serializable] public sealed class LAlpha { public float idleWord = 0.74f, idleRow = 0.9f, focus = 1f, disabled = 0.4f, chevIdle = 0.5f, chevDisabled = 0.25f, tabHover = 0.92f; }
        [Serializable]
        public sealed class LTypes
        {
            public TypeS word = new() { sizeRef = 22, trackingEm = 0.22f, uppercase = true }, wordSmall = new() { sizeRef = 18, trackingEm = 0.2f, uppercase = true },
                action = new() { sizeRef = 15.5f, trackingEm = 0.24f, uppercase = true }, title = new() { sizeRef = 32, trackingEm = 0.3f, uppercase = true },
                pauseTitle = new() { sizeRef = 22, trackingEm = 0.3f, uppercase = true }, tab = new() { sizeRef = 15.5f, trackingEm = 0.2f, uppercase = true },
                group = new() { sizeRef = 12.5f, trackingEm = 0.2f, uppercase = true, color = "textDim" }, rowLabel = new() { sizeRef = 17, trackingEm = 0.02f },
                rowValue = new() { font = "Jost-Regular-Tab", sizeRef = 17 }, keycap = new() { font = "Jost-Regular-Tab", sizeRef = 14.5f, trackingEm = 0.02f },
                keycapAlt = new() { font = "Jost-Regular-Tab", sizeRef = 12, trackingEm = 0.02f, color = "textDim" }, clash = new() { sizeRef = 16.5f },
                pauseBuild = new() { font = "Jost-Regular-Tab", sizeRef = 11.5f, color = "textDim", alpha = 0.85f };
        }
        [Serializable] public sealed class LShadow { public string color = "#0b1615"; public float alpha = 0.55f, blurRef = 6, yRef = 1; }
        [Serializable] public sealed class LMarker { public float sizeRef = 9, gutterRef = 30; }
        [Serializable] public sealed class LLine { public float wordHRef = 2, rowHRef = 2, idleHairAlpha = 0.16f, tickWRef = 3, tickInsetRef = 7, ruleAlpha = 0.22f; }
        [Serializable] public sealed class LMenu { public float leftRef = 72, bottomRef = 100, itemW = 300, itemH = 48, gapRef = 12, textInsetRef = 52, lineWRef = 232, lineYRef = 37; }
        [Serializable]
        public sealed class LSheet
        {
            public float glassTint = 0.72f, glassBlurRef = 18, wRef = 960, hRef = 780, flatAlpha = 0.9f, borderAlpha = 0.34f, borderRef = 1, radiusRef = 2, padX = 48,
                titleTop = 42, tabsTop = 116, tabH = 36, tabPadX = 14, tabGap = 22, bodyTop = 172, footerRuleFromBottom = 92, footerItemH = 40, footerInset = 30, footerGutter = 20, footerLineY = 29,
                rowH = 44, groupH = 40, rowLabelX = 14, rowControlRight = 14, colGap = 44, controlsRowH = 34, pauseVeil = 0.4f;
        }
        [Serializable] public sealed class LChoice { public float chevRef = 14, chevHit = 28, zoneW = 286, valueW = 190, nudgeRef = 3; }
        [Serializable] public sealed class LSlider { public float railW = 220, railH = 2, railAlpha = 0.38f, knobRef = 11, knobBorderRef = 1.5f, knobFocusRef = 15, knobDragRef = 17, valueW = 54, valueGap = 16; }
        [Serializable] public sealed class LSwitch { public float boxRef = 20, boxBorderRef = 1.5f, boxBorderAlpha = 0.6f, diamondRef = 9, wordW = 38, gapRef = 14; }
        [Serializable]
        public sealed class LKeycap
        {
            public float hRef = 28, minW = 48, padX = 12, altH = 22, altPadX = 8, borderRef = 1.5f, borderAlpha = 0.85f, altBorderAlpha = 0.35f, fillAlpha = 0.45f, radiusRef = 3, gapRef = 8,
                listenFillAlpha = 0.6f, listenMinW = 110, listenSizeRef = 13.5f;
        }
        [Serializable] public sealed class LClash { public float badgeRef = 18, stripH = 56, stripBadgeRef = 26, gapAbove = 8, ruleAlpha = 0.5f, fillAlpha = 0.35f; }
        [Serializable]
        public sealed class LPause
        {
            public float wRef = 360, topFrac = 0.12f, padX = 32, padTop = 30, padBottom = 24, fillAlpha = 0.9f, itemH = 44, titleH = 52, gutterRef = 30, insetRef = 34, lineWRef = 266, lineYRef = 33,
                endGapRef = 14, buildH = 34;
        }
        [Serializable] public sealed class LBrief { public float continueW = 232, continueH = 48, continueInset = 40, continueGutter = 24, continueLineW = 192, continueLineY = 37, continueRightRef = 64, continueBottomRef = 64; }
        [Serializable]
        public sealed class LTiming
        {
            public float markerSlide = 0.18f, lineDraw = 0.22f, lineDelay = 0.04f, lineRetract = 0.1f, tickSlide = 0.16f, tabSlide = 0.18f, labelFade = 0.12f, pressIn = 0.08f, pressOut = 0.12f,
                valueNudge = 0.1f, switchFlip = 0.12f, markerAppearDelay = 0.3f, markerAppear = 0.12f, listenBlink = 1f;
        }
        [Serializable]
        public sealed class LightS
        {
            public LAlpha alpha = new(); public LTypes type = new(); public LShadow shadow = new(); public LMarker marker = new(); public LLine line = new(); public LMenu menu = new();
            public LSheet sheet = new(); public LChoice choice = new(); public LSlider slider = new(); public LSwitch @switch = new(); public LKeycap keycap = new(); public LClash clash = new();
            public LPause pause = new(); public LBrief brief = new(); public LTiming timing = new();
        }

        /// <summary>The light line's numbers (frontend.json `lightline`).</summary>
        public static LightS LS => Shared.lightline ?? (Shared.lightline = new LightS());
        public static Color SC(string name, float a = 1f)
        {
            var c = Shared.colors;
            return Hex(name switch { "ink" => c.ink, "inkDeep" => c.inkDeep, "cream" => c.cream, "textDim" => c.textDim, "chrome" => c.chrome, "panelLine" => c.panelLine, "live" => c.live, "brass" => c.brass, _ => name }, a);
        }

        // ---------------------------------------------------------------- transitions (unscaled: the panel's own clock)

        public static void Trans(VisualElement e, params (string prop, float s, EasingMode ease, float delay)[] t)
        {
            var props = new List<StylePropertyName>(); var durs = new List<TimeValue>(); var eases = new List<EasingFunction>(); var dels = new List<TimeValue>();
            foreach (var (p, s, ease, d) in t) { props.Add(new StylePropertyName(p)); durs.Add(new TimeValue(s, TimeUnit.Second)); eases.Add(new EasingFunction(ease)); dels.Add(new TimeValue(d, TimeUnit.Second)); }
            e.style.transitionProperty = new StyleList<StylePropertyName>(props); e.style.transitionDuration = new StyleList<TimeValue>(durs);
            e.style.transitionTimingFunction = new StyleList<EasingFunction>(eases); e.style.transitionDelay = new StyleList<TimeValue>(dels);
        }

        /// <summary>The marker: a square turned 45 degrees, turquoise-light, centred on its left/top (translate -50 %).</summary>
        public static VisualElement Marker(VisualElement parent, float size)
        {
            var m = E(parent); m.name = "marker"; m.style.position = Position.Absolute; m.style.width = size; m.style.height = size; m.style.backgroundColor = SC("live");
            m.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent)); m.style.rotate = new Rotate(45); m.style.opacity = 0;
            return m;
        }

        public static void ShadowOn(VisualElement l)
        {
            var sh = LS.shadow; l.style.textShadow = new TextShadow { offset = new Vector2(0, sh.yRef), blurRadius = sh.blurRef, color = Hex(sh.color, sh.alpha) };
        }

        // ---------------------------------------------------------------- words (menu, pause, footer, Continue, clash actions)

        public sealed class LightWord
        {
            public VisualElement Root, Line, Marker, LabelBox; public Label Label; public float LineW, Top, H; public bool Focused, Pressed, Disabled, LineFromLabel; public LightList List; public float Inset, Gutter;
        }
        /// <summary>A vertical list of words with one marker that slides between them.</summary>
        public sealed class LightList { public VisualElement Root, Marker; public readonly List<LightWord> Items = new(); public float W, ItemH, Gap, Inset, Gutter, Height; public bool Intro; }

        /// <summary>One word item: the label at `inset`, the line under it at `lineY` (drawn from the label's left edge), its own marker when it is not in a list.
        /// `lineW` &lt;= 0: the label's width + 14 (footer and clash actions).</summary>
        public static LightWord Word(VisualElement parent, string text, TypeS type, float w, float h, float inset, float gutter, float lineW, float lineY, bool ownMarker, bool shadow)
        {
            var lw = new LightWord { Inset = inset, Gutter = gutter, H = h, LineW = lineW, LineFromLabel = lineW <= 0 };
            var r = E(parent, pick: true); r.style.height = h; if (!float.IsNaN(w)) r.style.width = w; r.focusable = false; lw.Root = r;
            var box = E(r); box.style.position = Position.Absolute; box.style.left = inset; box.style.top = 0; box.style.bottom = 0; box.style.justifyContent = Justify.Center; box.style.alignItems = Align.FlexStart; lw.LabelBox = box;
            var l = new Label { pickingMode = PickingMode.Ignore }; box.Add(l); Typeset(l, type, text); l.style.opacity = LS.alpha.idleWord; lw.Label = l;
            if (shadow) ShadowOn(box);
            Trans(l, ("opacity", LS.timing.labelFade, EasingMode.EaseOut, 0f));
            var line = E(r); line.style.position = Position.Absolute; line.style.left = inset; line.style.top = lineY; line.style.height = LS.line.wordHRef; line.style.width = 0; line.style.backgroundColor = SC("live"); lw.Line = line;
            if (ownMarker)
            {
                lw.Marker = Marker(r, LS.marker.sizeRef); lw.Marker.style.left = inset - gutter; lw.Marker.style.top = h / 2f;
                Trans(lw.Marker, ("opacity", LS.timing.labelFade, EasingMode.EaseOut, 0f), ("scale", LS.timing.pressIn, EasingMode.EaseOut, 0f));
            }
            if (lw.LineFromLabel)
                l.RegisterCallback<GeometryChangedEvent>(_ => { float tw = l.layout.width; if (float.IsNaN(tw) || tw <= 0) return; lw.LineW = tw + 14f; if (float.IsNaN(w)) r.style.width = inset + tw + 6f; PaintWord(lw); });
            return lw;
        }

        /// <summary>The word's state: label 74 % idle, 100 % focused or pressed, 40 % disabled; the line draws in (0.22 s after 0.04 s, ease-out-cubic) or
        /// retracts (0.10 s, ease-in-cubic); pressed: the line cream, the marker at 70 %.</summary>
        public static void PaintWord(LightWord w)
        {
            var a = LS.alpha; var t = LS.timing; bool on = w.Focused && !w.Disabled && !(w.List?.Intro ?? false);
            w.Label.style.opacity = w.Disabled ? a.disabled : on || w.Pressed ? a.focus : a.idleWord;
            float target = on ? Mathf.Max(0f, w.LineW) : 0f;
            if (on) Trans(w.Line, ("width", t.lineDraw, EasingMode.EaseOutCubic, t.lineDelay), ("background-color", t.pressIn, EasingMode.EaseOut, 0f));
            else Trans(w.Line, ("width", t.lineRetract, EasingMode.EaseInCubic, 0f), ("background-color", t.pressIn, EasingMode.EaseOut, 0f));
            w.Line.style.width = target; w.Line.style.backgroundColor = on && w.Pressed ? SC("cream") : SC("live");
            if (w.Marker != null) { w.Marker.style.opacity = on ? 1f : 0f; float k = on && w.Pressed ? 0.7f : 1f; w.Marker.style.scale = new Scale(new Vector3(k, k, 1)); }
            if (w.List != null) PaintListMarker(w.List);
        }

        public static LightList List(VisualElement parent, float w, float itemH, float gap, float inset, float gutter)
        {
            var li = new LightList { W = w, ItemH = itemH, Gap = gap, Inset = inset, Gutter = gutter };
            li.Root = E(parent); li.Root.style.position = Position.Absolute; li.Root.style.width = w;
            li.Marker = Marker(li.Root, LS.marker.sizeRef); li.Marker.style.left = inset - gutter; li.Marker.style.top = itemH / 2f;
            MarkerSlides(li, true);
            return li;
        }
        static void MarkerSlides(LightList li, bool slide)
        {
            var t = LS.timing;
            Trans(li.Marker, ("top", slide ? t.markerSlide : 0f, EasingMode.EaseOutCubic, 0f), ("opacity", li.Intro ? t.markerAppear : t.labelFade, EasingMode.EaseOut, 0f), ("scale", t.pressIn, EasingMode.EaseOut, 0f));
        }
        public static LightWord ListWord(LightList li, string text, TypeS type, float lineW, float lineY, float gapBefore, bool shadow)
        {
            float top = li.Items.Count == 0 ? gapBefore : li.Height + li.Gap + gapBefore;
            var w = Word(li.Root, text, type, li.W, li.ItemH, li.Inset, li.Gutter, lineW, lineY, false, shadow);
            w.Root.style.position = Position.Absolute; w.Root.style.left = 0; w.Root.style.top = top; w.Top = top; w.List = li; li.Items.Add(w);
            li.Height = top + li.ItemH; li.Root.style.height = li.Height; li.Marker.BringToFront();
            return w;
        }
        /// <summary>The list's one marker: at the focused item's centre (it slides 0.18 s), hidden when none is focused.</summary>
        public static void PaintListMarker(LightList li)
        {
            LightWord f = null; foreach (var it in li.Items) if (it.Focused && !it.Disabled && it.Root.resolvedStyle.display != DisplayStyle.None && it.Root.style.display != DisplayStyle.None) { f = it; break; }
            bool on = f != null && !li.Intro;
            if (f != null) li.Marker.style.top = f.Top + li.ItemH / 2f;
            li.Marker.style.opacity = on ? 1f : 0f; float k = on && f.Pressed ? 0.7f : 1f; li.Marker.style.scale = new Scale(new Vector3(k, k, 1));
        }
        /// <summary>Re-lay a list after items were shown or hidden (the menu's Continue): tops close up; the marker jumps (no slide).</summary>
        public static void Relayout(LightList li)
        {
            float y = 0; bool first = true;
            foreach (var it in li.Items) { if (it.Root.style.display == DisplayStyle.None) continue; if (!first) y += li.Gap; it.Top = y; it.Root.style.top = y; y += li.ItemH; first = false; }
            li.Height = y; li.Root.style.height = y;
        }
        /// <summary>The marker set at the default item without a slide, hidden; `Reveal` fades it in (0.12 s) and lets the line draw once.</summary>
        public static void IntroStart(LightList li) { li.Intro = true; MarkerSlides(li, false); foreach (var it in li.Items) PaintWord(it); }
        public static void IntroReveal(LightList li) { li.Intro = false; foreach (var it in li.Items) PaintWord(it); li.Marker.schedule.Execute(() => MarkerSlides(li, true)).StartingIn(Mathf.RoundToInt(LS.timing.markerAppear * 1000f) + 20); }

        /// <summary>Pointer: entering focuses (hover = focus); a press shows the pressed state; the action runs on release over the item.</summary>
        public static void Hook(LightWord w, Action onEnter, Action onRelease)
        {
            var r = w.Root;
            r.RegisterCallback<PointerEnterEvent>(_ => { if (!w.Disabled) onEnter?.Invoke(); });
            r.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0) return; e.StopPropagation(); if (w.Disabled) return; w.Pressed = true; onEnter?.Invoke(); PaintWord(w); });
            r.RegisterCallback<PointerUpEvent>(e => { if (e.button != 0) return; e.StopPropagation(); if (!w.Pressed) return; w.Pressed = false; PaintWord(w); onRelease?.Invoke(); });
            r.RegisterCallback<PointerLeaveEvent>(_ => { if (!w.Pressed) return; w.Pressed = false; PaintWord(w); });
        }

        // ---------------------------------------------------------------- the smoked-glass sheet's bake

        /// <summary>The picture baked once at about 1/12 size and box-blurred (glassBlurRef ref px at 1080), for the sheet's background: UI Toolkit has no
        /// backdrop blur. Read through a RenderTexture (the picture need not be readable).</summary>
        public static Texture2D BakeGlass(Texture2D src, float blurRef)
        {
            if (!src) return null;
            try
            {
                int h = 120, w = Mathf.Max(1, Mathf.RoundToInt(h * src.width / (float)src.height));
                Texture cur = src; RenderTexture prevRt = null; int cw = src.width, ch = src.height;
                while (cw / 2 >= w * 2) { cw /= 2; ch /= 2; var rt = RenderTexture.GetTemporary(cw, ch, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); rt.filterMode = FilterMode.Bilinear; Graphics.Blit(cur, rt); if (prevRt) RenderTexture.ReleaseTemporary(prevRt); prevRt = rt; cur = rt; }
                var fin = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); fin.filterMode = FilterMode.Bilinear; Graphics.Blit(cur, fin); if (prevRt) RenderTexture.ReleaseTemporary(prevRt);
                var was = RenderTexture.active; RenderTexture.active = fin;
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "front-end glass" };
                t.ReadPixels(new UnityEngine.Rect(0, 0, w, h), 0, 0); RenderTexture.active = was; RenderTexture.ReleaseTemporary(fin);
                var px = t.GetPixels32(); int r = Mathf.Max(1, Mathf.RoundToInt(blurRef / 1080f * h));
                for (int pass = 0; pass < 3; pass++) { BoxBlur(px, w, h, r, true); BoxBlur(px, w, h, r, false); }
                t.SetPixels32(px); t.Apply(false, true); return t;
            }
            catch (Exception e) { Debug.LogWarning("FrontEnd: glass bake failed, flat fill used: " + e.Message); return null; }
        }
        static void BoxBlur(Color32[] px, int w, int h, int r, bool horiz)
        {
            int n = horiz ? w : h, lines = horiz ? h : w; var buf = new Color32[n]; int span = 2 * r + 1;
            for (int ln = 0; ln < lines; ln++)
            {
                int Ix(int i) => horiz ? ln * w + Mathf.Clamp(i, 0, n - 1) : Mathf.Clamp(i, 0, n - 1) * w + ln;
                int sr = 0, sg = 0, sb = 0;
                for (int i = -r; i <= r; i++) { var c = px[Ix(i)]; sr += c.r; sg += c.g; sb += c.b; }
                for (int i = 0; i < n; i++)
                {
                    buf[i] = new Color32((byte)(sr / span), (byte)(sg / span), (byte)(sb / span), 255);
                    var a = px[Ix(i - r)]; var b = px[Ix(i + r + 1)]; sr += b.r - a.r; sg += b.g - a.g; sb += b.b - a.b;
                }
                for (int i = 0; i < n; i++) px[Ix(i)] = buf[i];
            }
        }
    }
}
