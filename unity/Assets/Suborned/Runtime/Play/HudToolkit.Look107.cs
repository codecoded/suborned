using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 107 (Sky 2026-11-11: the weather and the light are rolled from the seed and can be changed at the briefing; engine 0.54.0), redrawn by Glyph
    /// after Sky on 0.2.316 ("that briefing page looks cluttered where the weather condition selector is"): one calm control on the start's Job page while the clock
    /// hasn't run, a small pill just left of the clock chip, on the clock's line and above the title. It shows the look now: the sun or the moon, the weather's
    /// pictogram (none for clear) and the weather's one word, then a small chevron. A click opens a small card under it with two rows, Day · Night and
    /// Clear · Overcast · Rain · Mist, each a pill with its pictogram; the chosen one is filled in the book's deep teal with cream ink. A pick gives the game's
    /// `light` or `weather` order (HandoverHost.ChooseLight, ChooseWeather; taken before the clock only, so the engine owns both and a saved run replays them)
    /// and shuts the card; a click anywhere else shuts it too (no key). Night is left out where the scene has none (host.CanNight). The pictograms are painted
    /// solid (the kit has no weather singles), sitting in their pills by flex centring, the shared rule's Middle() way (HudToolkit.Centre). Book colours only.
    /// Words are placeholders for Fable.</summary>
    public sealed partial class HudToolkit
    {
        static readonly (string what, string kind, string word)[] LookWords =
            { ("light", "day", "Day"), ("light", "night", "Night"), ("weather", "clear", "Clear"), ("weather", "overcast", "Overcast"), ("weather", "rain", "Rain"), ("weather", "mist", "Mist") };
        const float LkH = 36f, LkIcon = 22f, LkWordSize = 19f, LkGapToClock = 12f, LkTop = 16f;
        bool lookMenuOpen; static bool lookMenuPin;

        /// <summary>The shots: draw the start's look card open (a briefing still of the choices).</summary>
        public static void LookMenuPin(bool on) { lookMenuPin = on; if (live != null) { live.lookMenuOpen = on; live.bkFor = null; } }

        /// <summary>After the clock chip (BkChrome), so the pill and its card draw over the page.</summary>
        void BkLook(VisualElement body, BookPage pg)
        {
            if (!host.BookStart || pg.Kicker.Length == 0 || host.Game == null || host.Game.Tick > 0) { lookMenuOpen = false; return; }
            if (lookMenuPin) lookMenuOpen = true;
            var clock = bkClock?.parent;
            float Right() => 64f + (clock != null && clock.layout.width > 0f ? clock.layout.width : 12f + 18f + 6f + BkTw("0:00", 22f, "Jost-Medium-Tab") + 14f) + LkGapToClock;

            // the pill: the look now, one word, the chevron
            var chip = LkPill(body, true); chip.name = "book-look"; chip.style.position = Position.Absolute; chip.style.top = LkTop; chip.style.right = Right();
            Border(chip, 2f, BkDeep); chip.style.backgroundColor = new Color(BkPaper.r, BkPaper.g, BkPaper.b, 1f); chip.style.paddingRight = 10f;
            var lightIcon = LkPaint(chip, "day", BkInk); var weatherIcon = LkPaint(chip, "overcast", BkInk); weatherIcon.style.marginLeft = 2f;
            var word = LkWord(chip, "", BkInkHex); word.style.marginLeft = 7f;
            var chev = E(chip, null, w: 14f, h: 14f); chev.style.marginLeft = 7f; chev.style.flexShrink = 0;

            // the card: two rows of pills under the pill, its right edge on the pill's
            var veil = E(body, null, 0f, 0f, BookLayout.Grid, BookLayout.GridH, true); veil.name = "book-look-veil"; // a click off the card shuts it (and does nothing else)
            var card = E(body, null, pick: true); card.name = "book-look-card"; card.style.position = Position.Absolute; card.style.top = LkTop + LkH + 8f; card.style.right = Right();
            Round(card, 18f); Border(card, 2f, BkDeep); card.style.backgroundColor = new Color(BkPaper.r, BkPaper.g, BkPaper.b, 1f);
            card.style.paddingLeft = card.style.paddingRight = card.style.paddingTop = card.style.paddingBottom = 8f;
            var opts = new List<(VisualElement pill, Paint icon, Label word, string what, string kind)>();
            for (int row = 0; row < 2; row++)
            {
                var r = E(card); r.style.flexDirection = FlexDirection.Row; if (row == 1) r.style.marginTop = 6f;
                foreach (var (what, kind, w) in LookWords)
                {
                    if ((what == "light") != (row == 0) || kind == "night" && !host.CanNight) continue;
                    var pill = LkPill(r, true); pill.name = "book-look-" + kind; if (r.childCount > 1) pill.style.marginLeft = 6f;
                    var ic = LkPaint(pill, kind, BkInk); var l = LkWord(pill, w, BkInkHex); l.style.marginLeft = 7f;
                    opts.Add((pill, ic, l, what, kind));
                    pill.RegisterCallback<PointerEnterEvent>(_ => { if (!On(what, kind)) pill.style.backgroundColor = new Color(BkPale.r, BkPale.g, BkPale.b, .35f); });
                    pill.RegisterCallback<PointerLeaveEvent>(_ => Repaint());
                    pill.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); if (what == "light") host.ChooseLight(kind == "night"); else host.ChooseWeather(kind); lookMenuOpen = lookMenuPin; Repaint(); });
                }
            }

            chip.RegisterCallback<PointerEnterEvent>(_ => chip.style.backgroundColor = BkPaleOn(true));
            chip.RegisterCallback<PointerLeaveEvent>(_ => chip.style.backgroundColor = BkPaleOn(false));
            chip.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); lookMenuOpen = !lookMenuOpen; Repaint(); });
            veil.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); lookMenuOpen = false; Repaint(); });
            clock?.RegisterCallback<GeometryChangedEvent>(_ => { chip.style.right = Right(); card.style.right = Right(); });

            bool On(string what, string kind) { var g = host.Game; return g != null && (what == "light" ? g.Light == kind : g.Weather == kind); }
            void Repaint()
            {
                var g = host.Game; if (g == null) return;
                bool night = g.Light == "night"; string weather = string.IsNullOrEmpty(g.Weather) ? "clear" : g.Weather;
                LkTint(lightIcon, night ? "night" : "day", BkInk); Show(weatherIcon, weather != "clear"); if (weather != "clear") LkTint(weatherIcon, weather, BkInk);
                string ww = "Clear"; foreach (var (wt, k, w) in LookWords) if (wt == "weather" && k == weather) ww = w; Text(word, ww);
                Icon(chev, lookMenuOpen ? "chev_up" : "chev_down", BkSoft);
                Show(veil, lookMenuOpen); Show(card, lookMenuOpen);
                foreach (var (pill, ic, l, what, kind) in opts)
                {
                    bool on = On(what, kind); pill.style.backgroundColor = on ? BkDeep : new Color(0f, 0f, 0f, 0f);
                    LkTint(ic, kind, on ? Cream : BkInk); l.style.color = on ? Cream : BkInk;
                }
            }
            Repaint();
        }

        static Color BkPaleOn(bool hover) => hover ? Color.Lerp(new Color(BkPaper.r, BkPaper.g, BkPaper.b, 1f), BkPale, .3f) : new Color(BkPaper.r, BkPaper.g, BkPaper.b, 1f);

        /// <summary>A rounded pill that lays its pictogram and word out in a row, centred on its middle line by flex (inside its border).</summary>
        static VisualElement LkPill(VisualElement parent, bool pick)
        {
            var e = E(parent, null, h: LkH, pick: pick); Round(e, LkH / 2f); e.style.flexDirection = FlexDirection.Row; e.style.alignItems = Align.Center; e.style.flexShrink = 0;
            e.style.paddingLeft = 11f; e.style.paddingRight = 14f; return e;
        }

        static Label LkWord(VisualElement parent, string text, string hex)
        {
            var l = new Label { pickingMode = PickingMode.Ignore }; parent.Add(l);
            FrontEnd.Typeset(l, new FrontEnd.TypeS { font = "Jost-SemiBold", sizeRef = LkWordSize }, text ?? "", hex); l.style.whiteSpace = WhiteSpace.NoWrap; l.style.flexShrink = 0; return l;
        }

        static Paint LkPaint(VisualElement parent, string kind, Color c)
        {
            var p = P(parent, 0f, 0f, LkIcon, LkIcon, null); p.style.position = Position.Relative; p.style.left = StyleKeyword.Auto; p.style.top = StyleKeyword.Auto; p.style.flexShrink = 0;
            LkTint(p, kind, c); return p;
        }

        static void LkTint(Paint p, string kind, Color c) { p.Fn = (pt, r) => Pictogram(pt, r, kind, c); p.MarkDirtyRepaint(); }

        /// <summary>The look's solid pictograms in a square: the sun (day, clear), the moon (night), a cloud (overcast), a cloud with rain, mist's three bands.</summary>
        static void Pictogram(Painter2D p, Rect r, string kind, Color c)
        {
            float s = Mathf.Min(r.width, r.height); Vector2 o = r.position + new Vector2((r.width - s) / 2f, (r.height - s) / 2f); Vector2 At(float x, float y) => o + new Vector2(x * s, y * s);
            p.fillColor = c; p.strokeColor = c; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            void Disc(float x, float y, float rad) { p.BeginPath(); p.Arc(At(x, y), rad * s, Deg(0), Deg(360)); p.ClosePath(); p.Fill(); }
            void Bar(float x0, float y0, float x1, float y1, float w) { p.lineWidth = w * s; p.BeginPath(); p.MoveTo(At(x0, y0)); p.LineTo(At(x1, y1)); p.Stroke(); }
            void Cloud(float dy, float k) // a cloud's three humps on a round-ended base
            {
                float Y(float y) => .5f + (y - .5f) * k + dy; float X(float x) => .5f + (x - .5f) * k;
                Disc(X(.34f), Y(.56f), .17f * k); Disc(X(.55f), Y(.44f), .23f * k); Disc(X(.74f), Y(.58f), .15f * k);
                Bar(X(.26f), Y(.64f), X(.76f), Y(.64f), .18f * k);
            }
            switch (kind)
            {
                case "day": case "clear":
                    Disc(.5f, .5f, .2f);
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; Vector2 d = new(Mathf.Cos(a), Mathf.Sin(a)); Bar(.5f + d.x * .33f, .5f + d.y * .33f, .5f + d.x * .44f, .5f + d.y * .44f, .085f); }
                    break;
                case "night": // a crescent: the outer circle's long arc, back along the inner circle's arc between their crossings
                {
                    Vector2 a = At(.5f, .5f), b = At(.66f, .36f); float ra = .38f * s, rb = .31f * s, d = Vector2.Distance(a, b);
                    float x = (d * d + ra * ra - rb * rb) / (2f * d), h = Mathf.Sqrt(Mathf.Max(0f, ra * ra - x * x)); Vector2 u = (b - a) / d, m = a + u * x, n = new(-u.y, u.x);
                    Vector2 p1 = m + n * h, p2 = m - n * h; float A(Vector2 c0, Vector2 q) => Mathf.Atan2(q.y - c0.y, q.x - c0.x) * Mathf.Rad2Deg;
                    p.BeginPath(); p.MoveTo(p1); p.Arc(a, ra, Deg(A(a, p1)), Deg(A(a, p2)), ArcDirection.Clockwise); p.Arc(b, rb, Deg(A(b, p2)), Deg(A(b, p1)), ArcDirection.CounterClockwise); p.ClosePath(); p.Fill();
                    break;
                }
                case "overcast": Cloud(.04f, 1.18f); break;
                case "rain":
                    Cloud(-.12f, 1f);
                    for (int i = 0; i < 3; i++) { float x = .31f + i * .2f; Bar(x, .73f, x - .07f, .92f, .1f); }
                    break;
                case "mist": Bar(.18f, .28f, .68f, .28f, .12f); Bar(.32f, .50f, .84f, .50f, .12f); Bar(.14f, .72f, .62f, .72f, .12f); break;
            }
        }
    }
}
