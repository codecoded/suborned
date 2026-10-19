using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 014's budget shots (Glyph §7, tools/hud-budget): one call writes three files for a moment of play, the format of
    /// briefs/014/glyph/after: `NN.png` the composited frame, `NN.hud.png` the HUD layer alone (the same UI Toolkit panel drawn into a transparent
    /// render texture on the next frame, the world paused under it; alpha marks every HUD pixel), and `NN.manifest.json` the state and the
    /// elements a screenshot cannot judge: bubbles (speech), story beats (beat), the goal line (line), portraits, pips and the scope (image), in
    /// screen pixels from each element's worldBound. Captures only; nothing here touches the run.</summary>
    public sealed partial class HudToolkit
    {
        public static bool ShotBusy { get; private set; }
        public static string LastShot { get; private set; } = "";

        /// <summary>Queue a capture to `basePath` (absolute, no extension); `state` is rest, active or alt (rest becomes briefing while the briefing card is up, log while the history panel is open). Poll ShotBusy.</summary>
        public static string Shoot(string basePath, string state)
        {
            if (live == null || !live.on) return "the kit view is not drawing";
            if (ShotBusy) return "busy";
            ShotBusy = true; live.StartCoroutine(live.ShootCo(basePath, state)); return "queued";
        }

        IEnumerator ShootCo(string basePath, string state)
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(basePath)!); } catch { }
            for (int i = 0; i < 4; i++) yield return null; // the layout settles (a mark placed from its own size reads it a frame late)
            yield return new WaitForEndOfFrame();
            var full = ScreenCapture.CaptureScreenshotAsTexture();
            int w = full.width, h = full.height;
            File.WriteAllBytes(basePath + ".png", full.EncodeToPNG());
            if (state == "rest" && host != null && host.ShowBriefing) state = "briefing"; // Splice's budget state for the briefing card (modal)
            else if (state == "rest" && host != null && host.FeedHistoryOpen) state = "log"; // Splice's budget state for the open history panel (a reading surface, 18 %)
            File.WriteAllText(basePath + ".manifest.json", Manifest(state, h));
            File.WriteAllText(basePath + ".parts.json", Parts(h)); // brief 014 round 2: every visible HUD part's rect, for the 2x inspection crops
            Destroy(full);
            var ps = doc.panelSettings; bool clear = ps.clearColor; var col = ps.colorClearValue;
            var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            ps.targetTexture = rt; ps.clearColor = true; ps.colorClearValue = new Color(0, 0, 0, 0);
            yield return null; yield return null; yield return new WaitForEndOfFrame();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var hud = new Texture2D(w, h, TextureFormat.RGBA32, false); hud.ReadPixels(new Rect(0, 0, w, h), 0, 0); hud.Apply();
            RenderTexture.active = prev;
            ps.targetTexture = null; ps.clearColor = clear; ps.colorClearValue = col;
            File.WriteAllBytes(basePath + ".hud.png", hud.EncodeToPNG());
            Destroy(hud); rt.Release(); Destroy(rt);
            LastShot = basePath; ShotBusy = false;
        }

        static bool Visible(VisualElement e)
        {
            for (var x = e; x != null; x = x.parent) { if (x.resolvedStyle.display == DisplayStyle.None || x.resolvedStyle.visibility == Visibility.Hidden || x.resolvedStyle.opacity < 0.05f) return false; }
            return true;
        }

        string Manifest(string state, int screenH)
        {
            float k = screenH / Mathf.Max(1f, root.panel.visualTree.layout.height);
            var sb = new StringBuilder(); sb.Append("{\n \"state\": \"").Append(state).Append("\",\n \"elements\": [");
            bool first = true;
            foreach (var (e, kind, text) in manifest)
            {
                if (!Visible(e)) continue; var b = e.worldBound; if (b.width < 1 || b.height < 1) continue;
                string t = text?.Invoke() ?? ""; if (kind is "speech" or "beat" or "line" && t.Length == 0) continue;
                sb.Append(first ? "\n" : ",\n"); first = false;
                sb.Append("  {\"kind\": \"").Append(kind).Append("\", \"x\": ").Append(R(b.x * k)).Append(", \"y\": ").Append(R(b.y * k)).Append(", \"w\": ").Append(R(b.width * k)).Append(", \"h\": ").Append(R(b.height * k))
                  .Append(", \"text\": \"").Append(t.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ")).Append("\"}");
            }
            // the pill's Alert lamps and pause glyph: icons by construction (the checker reads the lamp rings and the pause bars as strokes)
            var group = new[] { lamps[0], lamps[1], lamps[2], pauseIcon };
            Rect u = default; bool any = false; foreach (var e in group) if (e != null && Visible(e)) { var b = e.worldBound; u = any ? Rect.MinMaxRect(Mathf.Min(u.xMin, b.xMin), Mathf.Min(u.yMin, b.yMin), Mathf.Max(u.xMax, b.xMax), Mathf.Max(u.yMax, b.yMax)) : b; any = true; }
            if (any) { var pb = pill.worldBound; u = Rect.MinMaxRect(u.xMin, pb.yMin, pb.xMax, pb.yMax); } // to the pill's right end: its rim and screws are art too
            if (any) sb.Append(first ? "\n" : ",\n").Append("  {\"kind\": \"image\", \"x\": ").Append(R((u.x - 2) * k)).Append(", \"y\": ").Append(R((u.y - 2) * k)).Append(", \"w\": ").Append(R((u.width + 4) * k)).Append(", \"h\": ").Append(R((u.height + 4) * k)).Append(", \"text\": \"alert lamps, pause, the pill's end\"}");
            sb.Append("\n ]\n}\n"); return sb.ToString();
        }

        /// <summary>Every visible HUD component by name with its screen rect (pixels): the inspection crops are cut from these.</summary>
        string Parts(int screenH)
        {
            float k = screenH / Mathf.Max(1f, root.panel.visualTree.layout.height);
            var list = new System.Collections.Generic.List<(string, VisualElement)> { ("roster", column), ("plate", verbPlate), ("pill", pill), ("keys", keys), ("response", respPill), ("next-tip", nextTip), ("scope", scope), ("sheet", sheet), ("chip", chip2), ("goal", goal), ("end", end), ("briefing", brief), ("intel", intel), ("history", history), ("loud", loud), ("armed", armedPlate) };
            for (int i = 0; i < busy.Count; i++) list.Add(($"timer-{i + 1}", busy[i].root));
            for (int i = 0; i < beatEls.Count; i++) list.Add(($"beat-{i + 1}", beatEls[i].root));
            for (int i = 0; i < bubbleEls.Count; i++) list.Add(($"bubble-{i + 1}", bubbleEls[i].root));
            for (int i = 0; i < popEls.Count; i++) list.Add(($"pop-{i + 1}", popEls[i].root));
            for (int i = 0; i < pipEls.Count; i++) list.Add(($"pip-{Cast[i]}", pipEls[i].root));
            for (int i = 0; i < edgeEls.Count; i++) list.Add(($"edge-{i + 1}", edgeEls[i]));
            for (int i = 0; i < edgeCams.Count; i++) list.Add(($"edge-cameo-{i + 1}", edgeCams[i].Root));
            list.Add(("history-key", historyKey2));
            for (int i = 0; i < stashMarks.Count; i++) list.Add(($"stash-{i + 1}", stashMarks[i].Plate));
            var sb = new StringBuilder("{\n"); bool first = true;
            foreach (var (n, e) in list)
            {
                if (e == null || !Visible(e)) continue; var b = e.worldBound; if (b.width < 1 || b.height < 1) continue;
                sb.Append(first ? "" : ",\n").Append($" \"{n}\": [{R(b.x * k)}, {R(b.y * k)}, {R(b.width * k)}, {R(b.height * k)}]"); first = false;
            }
            if (ringOn) // the move ring is painted, not an element: its box with room for the second rim and the start of the glow
            { float pad = 24f; sb.Append(first ? "" : ",\n").Append($" \"ring\": [{R((ringAt.x - ringRx - pad) * k)}, {R((ringAt.y - ringRx * 0.616f - pad) * k)}, {R((2 * ringRx + 2 * pad) * k)}, {R((2 * ringRx * 0.616f + 2 * pad) * k)}]"); }
            return sb.Append("\n}\n").ToString();
        }

        /// <summary>Worst-case values for the layout audit (Sky's wrapped "Pa / y"): every price line shows $100k, 88 s, +12, ≥2; chips 88 s; the
        /// pill 8:30 and Trail 12. Display only: the run is never touched.</summary>
        public static bool Stress;

        /// <summary>The layout audit: every visible label whose text is wider than its box, or that runs onto a second line while it is meant to
        /// be one line (white-space nowrap), and every price row whose content overflows its row. Empty = clean.</summary>
        public static List<string> AuditText()
        {
            var bad = new List<string>(); if (live == null || live.root == null) { bad.Add("no HUD"); return bad; }
            void Walk(VisualElement e, string path)
            {
                if (e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.visibility == Visibility.Hidden) return;
                if (e is Label l && !string.IsNullOrEmpty(l.text) && l.text != "\u0000")
                {
                    var need = l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                    var box = l.contentRect; bool oneLine = l.resolvedStyle.whiteSpace == WhiteSpace.NoWrap;
                    if (oneLine && box.width > 0 && need.x > box.width + 0.75f) bad.Add($"{path}/'{l.text}': text {need.x:0.0} px in a {box.width:0.0} px box");
                    if (oneLine && box.height > need.y * 1.6f && need.y > 0) bad.Add($"{path}/'{l.text}': {box.height:0.0} px tall for one {need.y:0.0} px line");
                }
                // a fixed-size element whose children spill past it (a row squeezed): children's right edge beyond its own
                if (e.childCount > 0 && e.parent != null && e.ClassListContains("s-plate_light"))
                {
                    float right = 0; foreach (var c in e.Children()) if (c.resolvedStyle.display != DisplayStyle.None && c.resolvedStyle.position == Position.Relative) right = Mathf.Max(right, c.layout.xMax);
                    if (right > e.layout.width + 0.75f) bad.Add($"{path}: content {right:0.0} px in a {e.layout.width:0.0} px row");
                }
                // Sky 2026-10-16 (the eye's "?" poked out of its chip): every glyph's whole box inside a dark plate sits 4 px clear of the plate's
                // chrome (6.5 px in, measured on plate_dark's art); a satellite drawn wholly outside the plate (a cancel ✕) is not its content
                if (e.ClassListContains("s-plate_dark") && e.worldBound.width > 0)
                {
                    var plate = e.worldBound; float inset = RimIn + Clear - 0.3f;
                    var inner = Rect.MinMaxRect(plate.xMin + inset, plate.yMin + inset, plate.xMax - inset, plate.yMax - inset);
                    void Glyphs(VisualElement x, string px)
                    {
                        foreach (var c in x.Children())
                        {
                            if (c.resolvedStyle.display == DisplayStyle.None || c.resolvedStyle.visibility == Visibility.Hidden) continue;
                            var b = c.worldBound; if (!b.Overlaps(plate)) continue;
                            bool art = c.resolvedStyle.backgroundImage.sprite != null || c.resolvedStyle.backgroundImage.texture != null;
                            if (art && c.childCount == 0 && b.width > 0 && (b.xMin < inner.xMin || b.yMin < inner.yMin || b.xMax > inner.xMax || b.yMax > inner.yMax))
                                bad.Add($"{px}/{(c.name.Length > 0 ? c.name : "glyph")}: box {b.width:0.#}x{b.height:0.#} at ({b.x - plate.x:0.#},{b.y - plate.y:0.#}) not 4 px clear of the rim of a {plate.width:0.#}x{plate.height:0.#} plate");
                            Glyphs(c, px + "/" + (c.name.Length > 0 ? c.name : c.GetType().Name));
                        }
                    }
                    Glyphs(e, path);
                }
                int i = 0; foreach (var c in e.Children()) Walk(c, $"{path}/{(c.name.Length > 0 ? c.name : c.GetType().Name)}[{i++}]");
            }
            Walk(live.root, "hud");
            FeedAudit(bad);
            EndCardAudit(bad);
            EndButtonAudit(bad);
            return bad;
        }

        /// <summary>The end card is a reading surface: its line may wrap to two lines, never three, and no word may be wider than the card. Every
        /// line text.json can put there (less.end win, withdraw and each fail) is measured at the card's narrow width (400 less its 30 px
        /// padding each side), whether or not the card is open, so a new or longer line fails here first (the 379 px "The rival crew got to
        /// Vale first" in a 340 px box, 2026-10-18).</summary>
        /// <summary>Sky 2026-10-18 (rows drawn over each other): every feed row holds its own lines (no label past the row's bottom) and sits
        /// above the next row. FeedBadLayout rebuilds the rows as they were, so the check can be seen to fail.</summary>
        static void FeedAudit(List<string> bad)
        {
            var rows = live.historyRows; if (rows == null || !Visible(live.history)) return;
            var list = new List<VisualElement>(); foreach (var c in rows.Children()) if (c.name == "feed-row" && c.resolvedStyle.display != DisplayStyle.None) list.Add(c);
            for (int i = 0; i < list.Count; i++)
            {
                var rb = list[i].worldBound;
                list[i].Query<Label>().ForEach(l => { if (l.resolvedStyle.display != DisplayStyle.None && !string.IsNullOrEmpty(l.text) && l.worldBound.yMax > rb.yMax + 0.5f) bad.Add($"feed row {i}: '{(l.text.Length > 30 ? l.text[..30] + "…" : l.text)}' ends {l.worldBound.yMax - rb.yMax:0.#} px below its row"); });
                if (i + 1 < list.Count && rb.yMax > list[i + 1].worldBound.yMin + 0.5f) bad.Add($"feed rows {i} and {i + 1} overlap by {rb.yMax - list[i + 1].worldBound.yMin:0.#} px");
            }
        }

        /// <summary>Sky on the Air ("Show me again" ran past its pill): every end-card button's label inside its pill with 12 px each side, whether
        /// or not the card is open (the buttons are measured as laid out, the middle one as it would read on a failure).</summary>
        static void EndButtonAudit(List<string> bad)
        {
            var row = live.endButtons; if (row == null) return; live.EndButtonsFit();
            foreach (var b in row.Children())
            {
                if (b.Q<Label>() is not Label l || string.IsNullOrEmpty(l.text)) continue;
                float need = l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x + 48f, pill = b.resolvedStyle.width; // 24 px each side: the pointed ends
                if (float.IsNaN(pill) || pill <= 0) pill = b.style.width.value.value;
                if (pill > 0 && need > pill + 0.5f) bad.Add($"end card button '{l.text}': label {need - 48f:0} px (+48) in a {pill:0} px pill");
            }
        }

        static void EndCardAudit(List<string> bad)
        {
            var t = live.host != null ? live.host.Text : null; var l = live.endTitle; if (t == null || l == null) return;
            if (l.resolvedStyle.whiteSpace != WhiteSpace.Normal) bad.Add("end card: the line does not wrap");
            const float W = EndCardW - 2 * EndCardPad;
            float one = l.MeasureTextSize("Vale", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).y;
            var lines = new List<(string key, string text)> { ("win", t.EndWin), ("withdraw", t.EndWithdraw) };
            foreach (var kv in t.EndFail) lines.Add(("fail." + kv.Key, kv.Value));
            foreach (var (code, fl) in t.Fails) lines.Add(("briefing.fails." + code, fl.Split(new[] { ". " }, System.StringSplitOptions.None)[0].TrimEnd('.'))); // the fallback head (HudModel.EndCard) when less.end has no line for the code
            lines.Add(("fallback", "The attempt failed"));
            foreach (var (key, text) in lines)
            {
                if (string.IsNullOrEmpty(text)) continue;
                var sz = l.MeasureTextSize(text, W, VisualElement.MeasureMode.AtMost, 0, VisualElement.MeasureMode.Undefined);
                if (one > 0 && sz.y > 2 * one + 1f) bad.Add($"end card/{key} '{text}': {Mathf.RoundToInt(sz.y / one)} lines at {W:0} px (two at most)");
                foreach (var word in text.Split(' ')) { float ww = l.MeasureTextSize(word, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x; if (ww > W + 0.75f) bad.Add($"end card/{key} '{word}': {ww:0} px wider than the {W:0} px card"); }
            }
        }

        static string R(float v) => Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
    }
}
