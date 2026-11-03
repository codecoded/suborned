using System;
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
    /// window and centred; the paper fills the window. Begin: the page slides up off the top (0.3 s) onto the camera at the target's door, then the glide.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement bf, bfPaper, bfPage, bfPlan; Label bfPurseText, bfHeatText;
        FrontEnd.LightWord bfBegin;
        BriefScreen bfFor;
        /// <summary>The plan's pin discs (plan px), for PlanPaint's clearance (Glyph N7).</summary>
        readonly List<Rect> bfPinBox = new();
        /// <summary>Glyph N7 (0.2.137): a green shape is drawn only 4 px clear of every pin disc, and only when its drawn box is at least this
        /// many px on its short side (the 7 x 11 px bean over pin 3 read as a stub; the beds kept are 9 px and up). Area: tools/game/briefstills.ts.</summary>
        const float PlanKeepPx = 4f, PlanGreenMinPx = 8f;
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
            manifest.RemoveAll(x => bfOwn.Contains(x.Item1)); foreach (var e in bfOwn) { mfPart.Remove(e); mfN.Remove(e); mfRows.Remove(e); } bfOwn.Clear();
            int m0 = manifest.Count; try { BriefingFillInner(b, pg); } finally { for (int i = m0; i < manifest.Count; i++) bfOwn.Add(manifest[i].Item1); } // every row the fill appends is the page's
        }

        readonly HashSet<VisualElement> bfOwn = new();

        /// <summary>A page piece in the capture's manifest (glyph-page-budget §2): its kind, words, part, and a pin's number or the rows as set.</summary>
        void Pg(VisualElement e, string kind, Func<string> text, string part, string n = null, string[] rows = null)
        {
            manifest.Add((e, kind, text)); mfPart[e] = part; if (n != null) mfN[e] = n; if (rows != null) mfRows[e] = rows;
        }
        /// <summary>A section head: small tracked capitals in deep teal (the mock's TARGET, TWO WAYS IN, WHERE, THE CLOCK), from briefing.page.heads. Its
        /// manifest words are the source string (Typeset spaces the label's own text).</summary>
        Label Head(VisualElement pg, float x, float y, string text) { var l = PgText(pg, x, y, "Jost-Bold", 12.5f, PgTealHex, text, 0.26f, true); Pg(l, "hud", () => text, "heading"); return l; }

        string HeadWord(string key, string fallback) => host.Text != null && host.Text.PageHeads.TryGetValue(key, out var v) && v.Length > 0 ? v : fallback;

        void BriefingFillInner(BriefScreen b, VisualElement pg)
        {
            // the seafoam band and its brass hairline (full width: on the paper, outside the scaled page)
            var band = E(bfPaper, null, 0, 0); band.style.left = 0; band.style.right = 0; band.style.height = 6; band.style.backgroundColor = PgSeafoam;
            var bl = E(bfPaper, null, 0, 6); bl.style.left = 0; bl.style.right = 0; bl.style.height = 1; bl.style.backgroundColor = LookHair;
            Pg(bfPaper, "paper", () => "", "paper"); // glyph-page-budget 2.1: the paper is opaque and covers the frame
            // ---- the header: the title, the hook, the objective (brief.json); place · time on the right
            var title = PgText(pg, PgM, 48, "Jost-Medium", 15, PgInkHex, b.Title, 0.30f, true); Pg(title, "hud", () => b.Title, "title");
            var hook = PgText(pg, PgM, 70, "Jost-Bold", 46, PgInkHex, b.Hook); Pg(hook, "hud", () => b.Hook, "hook");
            var obj = PgText(pg, PgM, 130, "Jost-Medium", 22, PgInkHex, b.Objective); Pg(obj, "hud", () => b.Objective, "objective");
            var where = PgText(pg, float.NaN, 48, "Jost-Medium", 15, PgInkHex, b.Place + (b.Place.Length > 0 && b.Time.Length > 0 ? "  ·  " : "") + b.Time); where.style.right = PgM; Pg(where, "hud", () => where.text, "place");
            Hair(pg, PgM, 180, PgW - 2 * PgM);
            // ---- the target: the head (less.briefing.target), the photograph as a print, the name, two facts
            Head(pg, PgM, 202, host.Text?.BriefWord("target", "Target") ?? "Target");
            float phW = 392f, phH = 392f * 592f / 720f; var photo = Print(pg, 60, 228, phW, phH);
            if (Sp(b.TargetPhoto) != null) Bg(photo, b.TargetPhoto, Color.white); else { photo.style.backgroundColor = DiscBody; var bd = E(photo, null, phW / 2 - 48, phH / 2 - 48, 96, 96); Bg(bd, b.TargetBadge, Color.white); }
            Pg(photo.parent, "image", () => "target photograph", "target-photo");
            float ty = 228 + phH + 18;
            var nm = PgText(pg, PgM, ty, "Jost-Bold", 30, PgInkHex, b.TargetName); Pg(nm, "hud", () => b.TargetName, "target-name");
            var f1 = PgText(pg, PgM, ty + 40, "Jost-Medium", 18, PgInkHex, b.TargetFact); Pg(f1, "hud", () => b.TargetFact, "target-fact");
            var f2 = PgText(pg, PgM, ty + 66, "Jost-Medium", 18, PgInkHex, b.TargetFact2); Pg(f2, "hud", () => b.TargetFact2, "target-fact");
            // ---- the plan of the block, drawn from data, with the pins; the legend under it; its head (WHERE) over it
            Head(pg, PgM, 682, HeadWord("where", "Where"));
            bfPlan = P(pg, PgM, 706, 400, 140, PlanPaint); Pg(bfPlan, "image", () => "plan", "plan");
            Hair(pg, PgM, 706, 400); Hair(pg, PgM, 846, 400);
            var pinsAll = new List<BriefScreen.Pin>(); foreach (var w in b.Ways) pinsAll.AddRange(w.Pins); if (b.EndPin != null) pinsAll.Add(b.EndPin);
            const float PlanPinD = 24f, Keep = 4f;
            var pinBox = new List<Rect>(); foreach (var p in pinsAll) { var q = PlanXY(b, p.At); pinBox.Add(new Rect(q.x - PlanPinD / 2f, q.y - PlanPinD / 2f, PlanPinD, PlanPinD)); }
            bfPinBox.Clear(); bfPinBox.AddRange(pinBox); bfPlan?.MarkDirtyRepaint();
            foreach (var (name, poly) in b.PlanBuildings) if (poly.Length >= 3) PlanLabel(b, name, poly, pinBox, Keep);
            foreach (var p in pinsAll) { var at = PlanXY(b, p.At); var d = PinDisc(bfPlan, at.x, at.y, PlanPinD, p.N); Pg(d, "pin", () => "", "pin", p.N.Length > 0 ? p.N : "car"); }
            int numbered = 0; foreach (var p in pinsAll) if (p.N.Length > 0) numbered++;
            for (int i = 0; i < pinsAll.Count; i++)
            {
                var p = pinsAll[i]; bool end = p.N.Length == 0; // the legend: the numbered pins two to a row, the ending on its own row under them
                float lx = PgM + (end ? 0 : (i % 2) * 200), ly = 862 + (end ? (numbered + 1) / 2 : i / 2) * 26;
                var d = PinDisc(pg, lx + 11, ly + 9, 22, p.N); Pg(d, "pin", () => "", "pin", end ? "car" : p.N);
                var lt = PgText(pg, lx + 30, ly, "Jost-Bold", 14, PgInkHex, p.Caption); Pg(lt, "hud", () => p.Caption, "legend");
            }
            // ---- the ways in: one row each (name, the `when` plate, two sentences; two stills joined by an arrow); the shared ending on the right
            const float X = 500f, SW = 360f, SH = 226f, EX = 1590f, EW = 274f, EH = 470f;
            Head(pg, X, 202, HeadWord("ways." + b.Ways.Count, b.Ways.Count == 1 ? "One way in" : b.Ways.Count + " ways in"));
            for (int i = 0; i < b.Ways.Count && i < 2; i++)
            {
                var w = b.Ways[i]; float y = 232 + 300 * i;
                var wn = PgText(pg, X, y - 4, "Jost-Bold", 26, PgInkHex, w.Name); Pg(wn, "hud", () => w.Name, "way-name");
                if (w.When.Length > 0)
                {
                    var plate = PlateDark(pg, UnlH, UnlScale); plate.style.position = Position.Absolute; plate.style.left = X; plate.style.top = y + 40; plate.style.paddingLeft = 11; plate.style.paddingRight = 12; // the unlock pill's plate (its rim clears the icon by the audit's 4 px)
                    Icon(E(plate, null, w: 13, h: 13), "p_time", Cream); var pt = Words(plate, 13.5f); pt.style.marginLeft = 6; pt.text = w.When; Pg(plate, "hud", () => w.When, "way-when");
                }
                var col = E(pg, null, X, y + 86, 262); col.style.flexDirection = FlexDirection.Column; // the two sentences flow down, a gap between
                foreach (var s in w.Lines) { var l = PgPara(col, float.NaN, 0, 262, "Jost-Medium", 17, PgInkHex, s); l.style.position = Position.Relative; l.style.top = 0; l.style.marginBottom = 12; Pg(l, "hud", () => s, "way-text"); }
                for (int k = 0; k < w.Pins.Count && k < 2; k++)
                {
                    var p = w.Pins[k]; float sx = 790 + k * (SW + 44);
                    StillPrint(pg, p, sx, y + 8, SW, SH);
                    var cap = PgText(pg, sx, y + SH + 22, "Jost-Bold", 15, PgInkHex, p.Caption); Pg(cap, "hud", () => p.Caption, "caption");
                    Arrow(pg, k == 0 ? sx + SW + 12 : EX - 34, y + 8 + SH / 2 - 10);
                }
            }
            if (b.EndPin != null)
            {
                StillPrint(pg, b.EndPin, EX, 240, EW, EH);
                var cap = PgText(pg, EX, 240 + EH + 20, "Jost-Bold", 15, PgInkHex, b.EndPin.Caption); Pg(cap, "hud", () => b.EndPin.Caption, "caption");
                var el = PgPara(pg, EX, 240 + EH + 44, EW, "Jost-Medium", 15, PgInkHex, b.Ending); Pg(el, "hud", () => b.Ending, "ending");
            }
            // ---- the clock: a line of stations, each its time and label (briefing.page.clock); a station that names the target is filled
            int n = b.Clock.Count; float tx0 = X + 6, tx1 = 1858f, step = n > 0 ? (tx1 - tx0) / n : 0f, lyc = 852f;
            if (n > 0)
            {
                Head(pg, X, 814, HeadWord("clock", "The clock"));
                var track = E(pg, null, tx0, lyc - 1, step * (n - 1) + 6, 2); track.style.backgroundColor = Ink;
                for (int i = 0; i < n; i++)
                {
                    var c = b.Clock[i]; float x = tx0 + i * step;
                    var dot = E(pg, null, x - 6, lyc - 6, 12, 12); Round(dot, 6); dot.style.backgroundColor = c.Lit ? Ink : PgPaper; Border(dot, 2f, Ink);
                    var t = PgText(pg, x - 6, lyc + 14, "Jost-Medium-Tab", 20, PgInkHex, c.At); Pg(t, "hud", () => c.At, "clock-time");
                    var l = PgPara(pg, x - 6, lyc + 44, step - 22, "Jost-Medium", 13.5f, PgInkHex, c.Label); Pg(l, "hud", () => c.Label, "clock-label");
                }
            }
            // ---- the band: the cell (a face and one line each), the purse, Heat (its head and one word), Begin; its pieces sit 8 px above the band's middle,
            // so each ends 32 px (3 %) above the frame's foot (glyph-page-budget 2.3)
            const float U = 8f;
            var bandB = E(pg, null, -2000, 966, PgW + 4000, 114); bandB.style.backgroundColor = LookTail; var bb = E(pg, null, -2000, 966, PgW + 4000, 1); bb.style.backgroundColor = LookHair;
            notHud.Add(bandB); Pg(bandB, "band", () => "", "band");
            for (int i = 0; i < 4; i++)
            {
                var role = Op.Name((OpId)i); float x = PgM + i * 290;
                var cm = Cameo(pg, 54f, 2f); cm.style.position = Position.Absolute; cm.style.left = x; cm.style.top = 1023 - 30 - U; Bg(cm.Q("face"), $"portraits/cameo_{role}_normal", Color.white);
                Pg(cm, "image", () => "face " + role, "face"); // glyph-page-budget 2.2: one picture, one image
                // the head, as the book's title: the name (book.cell.name) in cream, then "· job" (names.speakers) in the dim; both data
                string who = host.AgentName(i), job = host.AgentJob(i), tag = who != job ? "· " + job : "";
                var hd = E(pg, null, x + 72, 992 - U); hd.style.flexDirection = FlexDirection.Row; hd.style.alignItems = Align.FlexEnd; hd.pickingMode = PickingMode.Ignore;
                var wl = PgText(hd, float.NaN, 0, "Jost-Bold", 12.5f, PgCreamHex, who, 0.16f, true); wl.style.position = Position.Relative; Pg(wl, "hud", () => who, "cell-head");
                if (tag.Length > 0) { var jl = PgText(hd, float.NaN, 0, "Jost-Bold", 12.5f, PgDimHex, tag, 0.16f, true); jl.style.position = Position.Relative; jl.style.marginLeft = 8; Pg(jl, "hud", () => tag, "cell-head"); }
                var line = b.CellLines.TryGetValue(role, out var cl) ? cl : "";
                var ll = PgPara(pg, x + 72, 1012 - U, 212, "Jost-Medium", 13.5f, PgDimHex, line); Pg(ll, "hud", () => line, "cell");
            }
            var purse = PlateDark(pg, 36, 0.43f); purse.style.position = Position.Absolute; purse.style.left = 1250; purse.style.top = 990 - U; purse.style.paddingLeft = 14; purse.style.paddingRight = 16; // the order pill's slice scale: a thinner rim
            Icon(E(purse, null, w: 16, h: 16), "p_cash", Cream); bfPurseText = Words(purse, 15.5f); bfPurseText.style.marginLeft = 8; Pg(purse, "hud", () => bfPurseText.text, "purse");
            var heat = E(pg, null, 1252, 1036 - U - 2); heat.style.flexDirection = FlexDirection.Row; heat.style.alignItems = Align.Center; // Glyph N6: the head word, then the eye and the word
            var hhw = HeadWord("heat", "Heat"); var hh = PgText(heat, float.NaN, 0, "Jost-Bold", 12.5f, PgDimHex, hhw, 0.16f, true); hh.style.position = Position.Relative; hh.style.top = 0; Pg(hh, "hud", () => hhw, "heat-head");
            var hw = E(heat); hw.style.flexDirection = FlexDirection.Row; hw.style.alignItems = Align.Center; hw.style.marginLeft = 12;
            Icon(E(hw, null, w: 15, h: 15), "eye_open", Dim); bfHeatText = Words(hw, 14f); bfHeatText.style.marginLeft = 7; Pg(hw, "hud", () => bfHeatText.text, "heat");
            var right = E(pg); right.style.position = Position.Absolute; right.style.right = PgM; right.style.top = 1000 - U; right.style.flexDirection = FlexDirection.Row; right.style.alignItems = Align.Center; right.pickingMode = PickingMode.Ignore;
            var beginType = new FrontEnd.TypeS { font = "Jost-Medium", sizeRef = 26, trackingEm = 0.34f, uppercase = true };
            bfBegin = FrontEnd.Word(right, host.Text?.BriefWord("begin", "Begin") ?? "Begin", beginType, float.NaN, 52, 30f, 22f, 0f, 44f, true, false); bfBegin.Root.style.flexShrink = 0;
            FrontEnd.Hook(bfBegin, null, () => host.BriefingBegin()); bfBegin.Focused = true; FrontEnd.PaintWord(bfBegin);
            Pg(bfBegin.Root, "hud", () => "Begin", "begin");
        }

        /// <summary>Glyph N2 (0.2.130): a building's name on the plan. Inside its outline, centred, on two lines split at the middle word ("Records / Office"),
        /// where that fits the outline's box and keeps 4 px from every pin; else on one line over the outline (or under it), clear of the pins. Placed once
        /// its size is known (the first layout), the plan's own box the limit.</summary>
        void PlanLabel(BriefScreen b, string name, Vector2[] poly, List<Rect> pins, float keep)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var q in poly) { var p = PlanXY(b, q); x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y); }
            var outline = Rect.MinMaxRect(x0, y0, x1, y1); var plan = new Rect(0f, 0f, 400f, 140f);
            var words = name.Split(' '); string[] two = null;
            if (words.Length >= 2) { int best = 1; float bd = float.MaxValue; for (int k = 1; k < words.Length; k++) { float d = Mathf.Abs(string.Join(" ", words, 0, k).Length - string.Join(" ", words, k, words.Length - k).Length); if (d < bd) { bd = d; best = k; } } two = new[] { string.Join(" ", words, 0, best), string.Join(" ", words, best, words.Length - best) }; }
            var l = PgText(bfPlan, 0f, 0f, "Jost-Bold", 11.5f, PgTealHex, two != null ? two[0] + "\n" + two[1] : name); l.style.unityTextAlign = TextAnchor.UpperCenter; l.style.visibility = Visibility.Hidden;
            Pg(l, "hud", () => name, "plan-label", null, two);
            bool Clear(Rect r) { foreach (var p in pins) if (r.xMin < p.xMax + keep && r.xMax > p.xMin - keep && r.yMin < p.yMax + keep && r.yMax > p.yMin - keep) return false; return r.xMin >= plan.xMin + 2f && r.xMax <= plan.xMax - 2f && r.yMin >= plan.yMin + 2f && r.yMax <= plan.yMax - 2f; }
            int stage = two != null ? 0 : 1;
            void Place(GeometryChangedEvent _)
            {
                float w = l.layout.width, h = l.layout.height; if (float.IsNaN(w) || w <= 0f) return;
                if (stage == 0)
                {   // inside: the box less 4 px each side; from the centre outward, 1 px steps, the first spot clear of the pins
                    var inner = Rect.MinMaxRect(outline.xMin + keep, outline.yMin + keep, outline.xMax - keep, outline.yMax - keep);
                    if (w <= inner.width && h <= inner.height)
                    {
                        float cx = outline.center.x - w / 2f, cy = outline.center.y - h / 2f;
                        for (int s = 0; s <= (int)(inner.height - h); s++)
                            foreach (var dy in s == 0 ? new[] { 0f } : new[] { (float)s, -s })
                            {
                                var r = new Rect(cx, cy + dy, w, h); if (r.yMin < inner.yMin || r.yMax > inner.yMax || !Clear(r)) continue;
                                l.style.left = r.x; l.style.top = r.y; l.style.visibility = StyleKeyword.Null; l.UnregisterCallback<GeometryChangedEvent>(Place); return;
                            }
                    }
                    stage = 1; l.text = name; mfRows.Remove(l); return; // one line outside: the next layout places it
                }
                float lx = Mathf.Clamp(outline.center.x - w / 2f, plan.xMin + 2f, plan.xMax - 2f - w);
                foreach (var ly in new[] { outline.yMin - 3f - h, outline.yMax + 3f })
                {
                    var r = new Rect(lx, ly, w, h); if (!Clear(r)) continue;
                    l.style.left = r.x; l.style.top = r.y; l.style.visibility = StyleKeyword.Null; l.UnregisterCallback<GeometryChangedEvent>(Place); return;
                }
                l.style.left = lx; l.style.top = Mathf.Max(plan.yMin + 2f, outline.yMin - 3f - h); l.style.visibility = StyleKeyword.Null; l.UnregisterCallback<GeometryChangedEvent>(Place); // nowhere clear: over the outline, as before (the budget's 2.4 says so)
            }
            l.RegisterCallback<GeometryChangedEvent>(Place);
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
            // the lift (0 to 0.3 s after Begin; Glyph N9): the page and its paper slide up off the top, never half there over the street (ease in)
            float u = h.Starting ? Mathf.Clamp01(t / HandoverHost.BriefExitS) : 0f, e = u * u;
            bfPage.style.left = (W - PgW * k) / 2f; bfPage.style.top = (H - PgH * k) / 2f - H * e; bfPage.style.scale = new Scale(new Vector3(k, k, 1f));
            bf.style.opacity = 1f; bfPaper.style.translate = new Translate(0, -H * e);
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
        /// <summary>A paragraph that wraps at `w` (the page is a reading surface: GDD §10, outside play). No widows (Glyph N7, glyph-page-budget 2.8):
        /// the last two words are held together by a no-break space, so a wrap pulls them down as one.</summary>
        static Label PgPara(VisualElement parent, float x, float y, float w, string font, float size, string hex, string text)
        {
            var l = PgText(parent, x, y, font, size, hex, NoWidow(text)); l.style.whiteSpace = WhiteSpace.Normal; l.style.width = w; l.style.unityTextAlign = TextAnchor.UpperLeft; return l;
        }
        static string NoWidow(string t)
        {
            t ??= ""; var s = t.TrimEnd(); int i = s.LastIndexOf(' '); if (i <= 0 || s.IndexOf(' ') == i) return t; // one or two words: nothing to hold
            return s[..i] + "\u00A0" + s[(i + 1)..];
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
            Pg(pic.parent, "image", () => "still " + p.Id, "still");
            var d = PinDisc(parent, x + 20, y + 20, 30, p.N); Pg(d, "pin", () => "", "pin", p.N.Length > 0 ? p.N : "car");
        }

        /// <summary>A pin: the kit's dark disc with a chrome ring, the number in cream (or the car icon for the ending).</summary>
        VisualElement PinDisc(VisualElement parent, float cx, float cy, float d, string n)
        {
            var w = E(parent, null, cx - d / 2f, cy - d / 2f, d, d); Round(w, d / 2f); w.style.backgroundColor = DiscBody; Border(w, 2f, Chrome);
            if (n.Length > 0) { var l = new Label(n) { pickingMode = PickingMode.Ignore }; w.Add(l); FrontEnd.Typeset(l, new FrontEnd.TypeS { font = "Jost-Bold", sizeRef = d * 0.5f }, n, PgCreamHex); l.style.position = Position.Absolute; l.style.left = 0; l.style.right = 0; l.style.top = 0; l.style.bottom = 0; l.style.unityTextAlign = TextAnchor.MiddleCenter; }
            else Icon(E(w, null, d * 0.22f - 2f, d * 0.22f - 2f, d * 0.56f, d * 0.56f), "car", Cream);
            return w;
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

        /// <summary>Glyph N7: whether a green shape is drawn: its box at least PlanGreenMinPx on the short side, and every edge PlanKeepPx clear of
        /// every pin disc (the bed under pin 4 ran into its chrome ring).</summary>
        bool GreenDrawn(BriefScreen b, Vector2[] pg)
        {
            if (pg.Length < 3) return false; var q = new Vector2[pg.Length]; float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < pg.Length; i++) { q[i] = PlanXY(b, pg[i]); x0 = Mathf.Min(x0, q[i].x); y0 = Mathf.Min(y0, q[i].y); x1 = Mathf.Max(x1, q[i].x); y1 = Mathf.Max(y1, q[i].y); }
            if (Mathf.Min(x1 - x0, y1 - y0) < PlanGreenMinPx) return false;
            foreach (var pin in bfPinBox)
            {
                var c = pin.center; float r = pin.width / 2f + PlanKeepPx; bool inside = false;
                for (int i = 0, j = q.Length - 1; i < q.Length; j = i++)
                {
                    var a = q[j]; var d = q[i] - a; float t = d.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(c - a, d) / d.sqrMagnitude) : 0f;
                    if (Vector2.Distance(c, a + d * t) < r) return false;
                    if ((q[i].y > c.y) != (a.y > c.y) && c.x < (a.x - q[i].x) * (c.y - q[i].y) / (a.y - q[i].y) + q[i].x) inside = !inside;
                }
                if (inside) return false;
            }
            return true;
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
            foreach (var g in b.PlanGreen) if (GreenDrawn(b, g)) Poly(g, PgGreen);
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
