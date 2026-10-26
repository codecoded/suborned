using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 070, the briefing page (GDD v0.27.0 §10 briefing; Glyph's mock briefs/070/glyph/briefing-page-mock.png; Sky 2026-10-26: "a proper
    /// page with just screengrabs / closeups of what matters"). Replaces brief 062 B's overlay on the street. A full-screen page in the game's paper
    /// look, modal until Begin: cream stone paper (style contract cream_stone), a seafoam band with a brass hairline at the top; the title, the hook
    /// and the objective; place and time; the target's file card (photograph, name, two facts); the ways in, one row each (name, the `when` plate,
    /// two sentences, two stills with numbered pins joined by arrows); the shared ending (the getaway car's still); a small plan of the block drawn
    /// from data with the same pins and a legend; the clock as a line of stations (time and label); and the deep-teal band: the cell's four faces,
    /// each with one line, the purse, Heat as one word, and Begin. No Day / Night (Sky: night is not a toggle). Every word comes from the mission's
    /// data (BriefScreen); the stills are the build's captures (tools/game/briefstills.ts). Laid out on a 1920 x 1080 grid, scaled as one to the
    /// window and centred; the paper fills the window. Begin: the page lifts away (0.3 s) onto the camera at the target's door, then the glide.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement bf, bfPaper, bfPage, bfPlan; Label bfPurseText, bfHeatText;
        FrontEnd.LightWord bfBegin;
        BriefScreen bfFor;
        const float PgW = 1920f, PgH = 1080f, PgM = 56f;
        /// <summary>The briefing replaces the old card (the 0.2.117 card stays built, hidden; off brings it back, for comparison only).</summary>
        public static bool BriefOnStreet = true;

        /// <summary>The proof reads it: which parts the briefing drew and where its data came from.</summary>
        public static string BriefingState { get; private set; } = "";

        // the page's colours, every one sourced: ink, ink-deep, cream, dim, chrome, brass (kit); the style contract's cream_stone (paper),
        // seafoam #72b5a7 and deep teal #1f6c67 (civic swatches), pavement #f9efdc and asphalt #a5a2a8 (world roles), pale seafoam #9bcfc2; the
        // band is the kit's panel body #0c3434 (the chrome tail's body)
        static readonly Color PgPaper = Hex(0xe4e0d3), PgSeafoam = Hex(0x72b5a7), PgDeepTeal = Hex(0x1f6c67), PgPave = Hex(0xf9efdc), PgAsphalt = Hex(0xa5a2a8), PgGreen = Hex(0x9bcfc2);
        const string PgInkHex = "#17302f", PgTealHex = "#1f6c67", PgDimHex = "#b0cdc7", PgCreamHex = "#f7f4ec";

        void BriefingBuild()
        {
            bf = E(root); bf.style.position = Position.Absolute; bf.style.left = 0; bf.style.top = 0; bf.style.right = 0; bf.style.bottom = 0; bf.pickingMode = PickingMode.Ignore; bf.style.overflow = Overflow.Hidden;
            bfPaper = new VisualElement { pickingMode = PickingMode.Position }; bfPaper.style.position = Position.Absolute; bfPaper.style.left = 0; bfPaper.style.top = 0; bfPaper.style.right = 0; bfPaper.style.bottom = 0;
            bfPaper.style.backgroundColor = PgPaper; bf.Add(bfPaper); notHud.Add(bfPaper); /* the paper is the page's ground, not HUD over the world (as the old card's veil) */
            bfPage = E(bf, null, 0, 0, PgW, PgH); bfPage.pickingMode = PickingMode.Ignore; bfPage.style.transformOrigin = new TransformOrigin(0, 0, 0);
            Show(bf, false);
        }

        /// <summary>The page's contents, built once per briefing data (a new run reads it again).</summary>
        void BriefingFill(BriefScreen b)
        {
            bfPage.Clear(); bfPaper.Clear(); var pg = bfPage; // a new run reads the page again: its old pieces and their manifest rows go
            int m0 = manifest.Count; manifest.RemoveAll(x => bfOwn.Contains(x.Item1)); bfOwn.Clear();
            m0 = manifest.Count; try { BriefingFillInner(b, pg); } finally { for (int i = m0; i < manifest.Count; i++) bfOwn.Add(manifest[i].Item1); } // every row the fill appends is the page's
        }

        readonly HashSet<VisualElement> bfOwn = new();

        void BriefingFillInner(BriefScreen b, VisualElement pg)
        {
            // the seafoam band and its brass hairline (full width: on the paper, outside the scaled page)
            var band = E(bfPaper, null, 0, 0); band.style.left = 0; band.style.right = 0; band.style.height = 6; band.style.backgroundColor = PgSeafoam;
            var bl = E(bfPaper, null, 0, 6); bl.style.left = 0; bl.style.right = 0; bl.style.height = 1; bl.style.backgroundColor = LookHair;
            // ---- the header: the title, the hook, the objective (brief.json); place · time on the right
            var title = PgText(pg, PgM, 46, "Jost-Medium", 15, PgInkHex, b.Title, 0.30f, true); manifest.Add((title, "hud", () => b.Title));
            var hook = PgText(pg, PgM, 70, "Jost-Bold", 46, PgInkHex, b.Hook); manifest.Add((hook, "hud", () => b.Hook));
            var obj = PgText(pg, PgM, 130, "Jost-Medium", 22, PgInkHex, b.Objective); manifest.Add((obj, "hud", () => b.Objective));
            var where = PgText(pg, float.NaN, 48, "Jost-Medium", 15, PgInkHex, b.Place + (b.Place.Length > 0 && b.Time.Length > 0 ? "  ·  " : "") + b.Time); where.style.right = PgM; manifest.Add((where, "hud", () => where.text));
            Hair(pg, PgM, 180, PgW - 2 * PgM);
            // ---- the target: the head (less.briefing.target), the photograph as a print, the name, two facts
            var th = PgText(pg, PgM, 202, "Jost-Bold", 12.5f, PgTealHex, host.Text?.BriefWord("target", "Target") ?? "Target", 0.26f, true);
            float phW = 392f, phH = 392f * 592f / 720f; var photo = Print(pg, 60, 228, phW, phH);
            if (Sp(b.TargetPhoto) != null) Bg(photo, b.TargetPhoto, Color.white); else { photo.style.backgroundColor = DiscBody; var bd = E(photo, null, phW / 2 - 48, phH / 2 - 48, 96, 96); Bg(bd, b.TargetBadge, Color.white); }
            manifest.Add((photo.parent, "image", () => "target photograph"));
            float ty = 228 + phH + 18;
            var nm = PgText(pg, PgM, ty, "Jost-Bold", 30, PgInkHex, b.TargetName); manifest.Add((nm, "hud", () => b.TargetName));
            var f1 = PgText(pg, PgM, ty + 40, "Jost-Medium", 18, PgInkHex, b.TargetFact); manifest.Add((f1, "hud", () => b.TargetFact));
            var f2 = PgText(pg, PgM, ty + 66, "Jost-Medium", 18, PgInkHex, b.TargetFact2); manifest.Add((f2, "hud", () => b.TargetFact2));
            // ---- the plan of the block, drawn from data, with the pins; the legend under it
            bfPlan = P(pg, PgM, 706, 400, 140, PlanPaint); manifest.Add((bfPlan, "image", () => "plan"));
            Hair(pg, PgM, 706, 400); Hair(pg, PgM, 846, 400);
            var pinXY = new List<Vector2>(); foreach (var w in b.Ways) foreach (var p in w.Pins) pinXY.Add(PlanXY(b, p.At)); if (b.EndPin != null) pinXY.Add(PlanXY(b, b.EndPin.At));
            foreach (var (name, poly) in b.PlanBuildings)
            {
                if (poly.Length < 3) continue; float cx = 0f, top = float.MaxValue, bot = float.MinValue; foreach (var q in poly) { var pq = PlanXY(b, q); cx += pq.x / poly.Length; top = Mathf.Min(top, pq.y); bot = Mathf.Max(bot, pq.y); }
                bool Clear(float y) { foreach (var q in pinXY) if (Mathf.Abs(q.x - cx) < 52f && Mathf.Abs(q.y - y) < 16f) return false; return true; }
                bool above = top >= 16f && (Clear(top - 9f) || !Clear(bot + 9f)); // the name over the building's outline, or under it: where no pin sits and the box has room
                var l = PgText(bfPlan, cx, above ? top - 3f : bot + 3f, "Jost-Bold", 11.5f, PgTealHex, name); l.style.translate = new Translate(Length.Percent(-50), Length.Percent(above ? -100 : 0));
            }
            var pinsAll = new List<BriefScreen.Pin>(); foreach (var w in b.Ways) pinsAll.AddRange(w.Pins); if (b.EndPin != null) pinsAll.Add(b.EndPin);
            foreach (var p in pinsAll) { var at = PlanXY(b, p.At); PinDisc(bfPlan, at.x, at.y, 24, p.N); }
            int numbered = 0; foreach (var p in pinsAll) if (p.N.Length > 0) numbered++;
            for (int i = 0; i < pinsAll.Count; i++)
            {
                var p = pinsAll[i]; bool end = p.N.Length == 0; // the legend: the numbered pins two to a row, the ending on its own row under them
                float lx = PgM + (end ? 0 : (i % 2) * 200), ly = 862 + (end ? (numbered + 1) / 2 : i / 2) * 26;
                PinDisc(pg, lx + 11, ly + 9, 22, p.N); var lt = PgText(pg, lx + 30, ly, "Jost-Bold", 14, PgInkHex, p.Caption); manifest.Add((lt, "hud", () => p.Caption));
            }
            // ---- the ways in: one row each (name, the `when` plate, two sentences; two stills joined by an arrow); the shared ending on the right
            const float X = 500f, SW = 360f, SH = 226f, EX = 1590f, EW = 274f, EH = 470f;
            for (int i = 0; i < b.Ways.Count && i < 2; i++)
            {
                var w = b.Ways[i]; float y = 232 + 300 * i;
                var wn = PgText(pg, X, y - 4, "Jost-Bold", 26, PgInkHex, w.Name); manifest.Add((wn, "hud", () => w.Name));
                if (w.When.Length > 0)
                {
                    var plate = PlateDark(pg, UnlH, UnlScale); plate.style.position = Position.Absolute; plate.style.left = X; plate.style.top = y + 40; plate.style.paddingLeft = 11; plate.style.paddingRight = 12; // the unlock pill's plate (its rim clears the icon by the audit's 4 px)
                    Icon(E(plate, null, w: 13, h: 13), "p_time", Cream); var pt = Words(plate, 13.5f); pt.style.marginLeft = 6; pt.text = w.When; manifest.Add((plate, "hud", () => w.When));
                }
                var col = E(pg, null, X, y + 86, 262); col.style.flexDirection = FlexDirection.Column; // the two sentences flow down, a gap between
                foreach (var s in w.Lines) { var l = PgPara(col, float.NaN, 0, 262, "Jost-Medium", 17, PgInkHex, s); l.style.position = Position.Relative; l.style.top = 0; l.style.marginBottom = 12; manifest.Add((l, "hud", () => s)); }
                for (int k = 0; k < w.Pins.Count && k < 2; k++)
                {
                    var p = w.Pins[k]; float sx = 790 + k * (SW + 44);
                    StillPrint(pg, p, sx, y + 8, SW, SH);
                    var cap = PgText(pg, sx, y + SH + 22, "Jost-Bold", 15, PgInkHex, p.Caption); manifest.Add((cap, "hud", () => p.Caption));
                    Arrow(pg, k == 0 ? sx + SW + 12 : EX - 34, y + 8 + SH / 2 - 10);
                }
            }
            if (b.EndPin != null)
            {
                StillPrint(pg, b.EndPin, EX, 240, EW, EH);
                var cap = PgText(pg, EX, 240 + EH + 20, "Jost-Bold", 15, PgInkHex, b.EndPin.Caption); manifest.Add((cap, "hud", () => b.EndPin.Caption));
                var el = PgPara(pg, EX, 240 + EH + 44, EW, "Jost-Medium", 15, PgInkHex, b.Ending); manifest.Add((el, "hud", () => b.Ending));
            }
            // ---- the clock: a line of stations, each its time and label (briefing.page.clock); a station that names the target is filled
            int n = b.Clock.Count; float tx0 = X + 6, tx1 = 1858f, step = n > 0 ? (tx1 - tx0) / n : 0f, lyc = 852f;
            if (n > 0)
            {
                var track = E(pg, null, tx0, lyc - 1, step * (n - 1) + 6, 2); track.style.backgroundColor = Ink;
                for (int i = 0; i < n; i++)
                {
                    var c = b.Clock[i]; float x = tx0 + i * step;
                    var dot = E(pg, null, x - 6, lyc - 6, 12, 12); Round(dot, 6); dot.style.backgroundColor = c.Lit ? Ink : PgPaper; Border(dot, 2f, Ink);
                    var t = PgText(pg, x - 6, lyc + 14, "Jost-Medium-Tab", 20, PgInkHex, c.At); manifest.Add((t, "hud", () => c.At));
                    var l = PgPara(pg, x - 6, lyc + 44, step - 22, "Jost-Medium", 13.5f, PgInkHex, c.Label); manifest.Add((l, "hud", () => c.Label));
                }
            }
            // ---- the band: the cell (a face and one line each), the purse, Heat as one word, Begin
            var bandB = E(pg, null, -2000, 966, PgW + 4000, 114); bandB.style.backgroundColor = LookTail; var bb = E(pg, null, -2000, 966, PgW + 4000, 1); bb.style.backgroundColor = LookHair;
            notHud.Add(bandB);
            for (int i = 0; i < 4; i++)
            {
                var role = Op.Name((OpId)i); float x = PgM + i * 290;
                var cm = Cameo(pg, 54f, 2f); cm.style.position = Position.Absolute; cm.style.left = x; cm.style.top = 1023 - 30; Bg(cm.Q("face"), $"portraits/cameo_{role}_normal", Color.white);
                var who = host.Text != null && host.Text.Speakers.TryGetValue(role, out var sp) ? sp : role;
                PgText(pg, x + 72, 992, "Jost-Bold", 12.5f, PgCreamHex, who, 0.16f, true);
                var line = b.CellLines.TryGetValue(role, out var cl) ? cl : "";
                var ll = PgPara(pg, x + 72, 1012, 212, "Jost-Medium", 13.5f, PgDimHex, line); manifest.Add((ll, "hud", () => line));
            }
            manifest.Add((pg, "image", () => "cell faces"));
            var purse = PlateDark(pg, 36, 0.43f); purse.style.position = Position.Absolute; purse.style.left = 1250; purse.style.top = 990; purse.style.paddingLeft = 14; purse.style.paddingRight = 16; // the order pill's slice scale: a thinner rim
            Icon(E(purse, null, w: 16, h: 16), "p_cash", Cream); bfPurseText = Words(purse, 15.5f); bfPurseText.style.marginLeft = 8; manifest.Add((purse, "hud", () => bfPurseText.text));
            var heat = E(pg, null, 1252, 1038); heat.style.flexDirection = FlexDirection.Row; heat.style.alignItems = Align.Center;
            Icon(E(heat, null, w: 15, h: 15), "eye_open", Dim); bfHeatText = Words(heat, 14f); bfHeatText.style.marginLeft = 7; manifest.Add((heat, "hud", () => bfHeatText.text));
            var right = E(pg); right.style.position = Position.Absolute; right.style.right = PgM; right.style.top = 1000; right.style.flexDirection = FlexDirection.Row; right.style.alignItems = Align.Center; right.pickingMode = PickingMode.Ignore;
            var beginType = new FrontEnd.TypeS { font = "Jost-Medium", sizeRef = 26, trackingEm = 0.34f, uppercase = true };
            bfBegin = FrontEnd.Word(right, host.Text?.BriefWord("begin", "Begin") ?? "Begin", beginType, float.NaN, 52, 30f, 22f, 0f, 44f, true, false); bfBegin.Root.style.flexShrink = 0;
            FrontEnd.Hook(bfBegin, null, () => host.BriefingBegin()); bfBegin.Focused = true; FrontEnd.PaintWord(bfBegin);
            manifest.Add((bfBegin.Root, "hud", () => "Begin"));
        }

        /// <summary>Per frame: the page while the briefing is up, its lift in the first 0.3 s after Begin.</summary>
        void BriefingFrame()
        {
            if (bf == null) BriefingBuild();
            var h = host; var b = h.Brief; float t = h.StartT;
            bool up = b != null && !m.Ended && (h.ShowBriefing || h.Starting && t < HandoverHost.BriefExitS);
            Show(bf, up);
            if (!up) { BriefingState = ""; return; }
            if (bfFor != b) { bfFor = b; BriefingFill(b); }
            BriefingState = (h.Starting ? "exit" : "up") + "|page|" + string.Join(";", b.Sources);
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) || size.x <= 0 ? PgW : size.x, H = float.IsNaN(size.y) || size.y <= 0 ? PgH : size.y;
            float k = Mathf.Min(W / PgW, H / PgH);
            // the lift (0 to 0.3 s after Begin): the page and its paper rise and fade onto the camera at the target's door
            float e = h.Starting ? Mathf.Clamp01(t / HandoverHost.BriefExitS) : 0f; e = 1f - (1f - e) * (1f - e);
            bfPage.style.left = (W - PgW * k) / 2f; bfPage.style.top = (H - PgH * k) / 2f - 60f * e; bfPage.style.scale = new Scale(new Vector3(k, k, 1f));
            bf.style.opacity = 1f - e; bfPaper.style.translate = new Translate(0, -60f * e);
            if (bfPurseText != null) bfPurseText.text = m.Cash;
            if (bfHeatText != null && h.Game is { } g)
            {
                string band = ""; try { band = g.HeatBand; } catch (System.Exception) { }
                bfHeatText.text = h.Text != null && h.Text.Heat.TryGetValue(band, out var hw) ? hw : band.Length > 0 ? char.ToUpperInvariant(band[0]) + band[1..] : "";
            }
        }

        // ---------------------------------------------------------------- the page's pieces

        /// <summary>One line of page type at (x, y) in the page grid (Jost; a colour from the kit or the contract).</summary>
        static Label PgText(VisualElement parent, float x, float y, string font, float size, string hex, string text, float track = 0f, bool upper = false)
        {
            var l = new Label { pickingMode = PickingMode.Ignore }; parent.Add(l); l.style.position = Position.Absolute; if (!float.IsNaN(x)) l.style.left = x; l.style.top = y;
            FrontEnd.Typeset(l, new FrontEnd.TypeS { font = font, sizeRef = size, trackingEm = track, uppercase = upper }, text ?? "", hex); l.style.whiteSpace = WhiteSpace.NoWrap;
            return l;
        }
        /// <summary>A paragraph that wraps at `w` (the page is a reading surface: GDD §10, outside play).</summary>
        static Label PgPara(VisualElement parent, float x, float y, float w, string font, float size, string hex, string text)
        {
            var l = PgText(parent, x, y, font, size, hex, text); l.style.whiteSpace = WhiteSpace.Normal; l.style.width = w; l.style.unityTextAlign = TextAnchor.UpperLeft; return l;
        }
        static void Hair(VisualElement parent, float x, float y, float w) { var e = E(parent, null, x, y, w, 1); e.style.backgroundColor = LookHair; }

        /// <summary>A print on the page: a cream border (6 px), a 1 px chrome rim, a soft ink shadow under it; returns the picture's element.</summary>
        VisualElement Print(VisualElement parent, float x, float y, float w, float h)
        {
            const float B = 6f;
            var sh = E(parent, null, x - B + 1, y - B + 5, w + 2 * B, h + 2 * B); Round(sh, 4); sh.style.backgroundColor = new Color(Ink.r, Ink.g, Ink.b, 0.16f);
            var fr = E(parent, null, x - B, y - B, w + 2 * B, h + 2 * B); Round(fr, 3); fr.style.backgroundColor = Cream; Border(fr, 1f, Chrome);
            var pic = E(fr, null, B - 1, B - 1, w, h); pic.style.unityBackgroundScaleMode = ScaleMode.ScaleAndCrop; pic.style.overflow = Overflow.Hidden;
            return pic;
        }

        /// <summary>A pin's still as a print with its numbered pin (or the car for the ending); a cream field with the pin alone until the build has captured it.</summary>
        void StillPrint(VisualElement parent, BriefScreen.Pin p, float x, float y, float w, float h)
        {
            var pic = Print(parent, x, y, w, h);
            if (p.Still != null) pic.style.backgroundImage = new StyleBackground(Background.FromTexture2D(p.Still)); else pic.style.backgroundColor = PgPave;
            PinDisc(parent, x + 20, y + 20, 30, p.N);
            manifest.Add((pic.parent, "image", () => "still " + p.Id));
        }

        /// <summary>A pin: the kit's dark disc with a chrome ring, the number in cream (or the car icon for the ending).</summary>
        void PinDisc(VisualElement parent, float cx, float cy, float d, string n)
        {
            var w = E(parent, null, cx - d / 2f, cy - d / 2f, d, d); Round(w, d / 2f); w.style.backgroundColor = DiscBody; Border(w, 2f, Chrome);
            if (n.Length > 0) { var l = new Label(n) { pickingMode = PickingMode.Ignore }; w.Add(l); FrontEnd.Typeset(l, new FrontEnd.TypeS { font = "Jost-Bold", sizeRef = d * 0.5f }, n, PgCreamHex); l.style.position = Position.Absolute; l.style.left = 0; l.style.right = 0; l.style.top = 0; l.style.bottom = 0; l.style.unityTextAlign = TextAnchor.MiddleCenter; }
            else Icon(E(w, null, d * 0.22f - 2f, d * 0.22f - 2f, d * 0.56f, d * 0.56f), "car", Cream);
        }

        /// <summary>The arrow between two stills (the kit's pointer single, deep teal).</summary>
        void Arrow(VisualElement parent, float x, float y)
        {
            P(parent, x, y, 22, 20, (p, r) => { p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round; p.strokeColor = PgDeepTeal; p.lineWidth = 2.4f; p.BeginPath(); p.MoveTo(new Vector2(2, 10)); p.LineTo(new Vector2(19, 10)); p.MoveTo(new Vector2(13, 4)); p.LineTo(new Vector2(19, 10)); p.LineTo(new Vector2(13, 16)); p.Stroke(); });
        }

        /// <summary>A block point (metres, x south, y east) in the plan's box: x across, y down, as Glyph's mock lays the block (the café left, the Office right).</summary>
        static Vector2 PlanXY(BriefScreen b, Vector2 q)
        {
            var r = b.PlanBounds; const float BW = 400f, BH = 140f; if (r.width <= 0f || r.height <= 0f) return new Vector2(BW / 2f, BH / 2f);
            float s = Mathf.Min(BW / r.width, BH / r.height), ox = (BW - r.width * s) / 2f, oy = (BH - r.height * s) / 2f;
            return new Vector2(ox + (q.x - r.xMin) * s, oy + (q.y - r.yMin) * s);
        }

        /// <summary>The plan: pavement, the bays and drives in asphalt, green and water, the buildings in ivory with a deep-teal edge.</summary>
        void PlanPaint(Painter2D p, Rect r)
        {
            var b = bfFor; if (b == null) return;
            p.fillColor = PgPave; p.BeginPath(); p.MoveTo(new Vector2(0, 0)); p.LineTo(new Vector2(r.width, 0)); p.LineTo(new Vector2(r.width, r.height)); p.LineTo(new Vector2(0, r.height)); p.ClosePath(); p.Fill();
            void Poly(Vector2[] pg, Color fill, Color? edge = null)
            {
                if (pg.Length < 3) return; p.BeginPath(); p.MoveTo(PlanXY(b, pg[0])); for (int i = 1; i < pg.Length; i++) p.LineTo(PlanXY(b, pg[i])); p.ClosePath();
                p.fillColor = fill; p.Fill(); if (edge is Color e) { p.strokeColor = e; p.lineWidth = 1.6f; p.lineJoin = LineJoin.Round; p.Stroke(); }
            }
            foreach (var g in b.PlanGround) Poly(g, PgAsphalt);
            foreach (var g in b.PlanGreen) Poly(g, PgGreen);
            foreach (var g in b.PlanWater) Poly(g, PgSeafoam);
            foreach (var (_, g) in b.PlanBuildings) Poly(g, Ivory, PgDeepTeal);
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
