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

        /// <summary>Queue a capture to `basePath` (absolute, no extension); `state` is rest, active or alt. Poll ShotBusy.</summary>
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
            var sb = new StringBuilder("{\n"); bool first = true;
            foreach (var (n, e) in list)
            {
                if (e == null || !Visible(e)) continue; var b = e.worldBound; if (b.width < 1 || b.height < 1) continue;
                sb.Append(first ? "" : ",\n").Append($" \"{n}\": [{R(b.x * k)}, {R(b.y * k)}, {R(b.width * k)}, {R(b.height * k)}]"); first = false;
            }
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
                int i = 0; foreach (var c in e.Children()) Walk(c, $"{path}/{(c.name.Length > 0 ? c.name : c.GetType().Name)}[{i++}]");
            }
            Walk(live.root, "hud");
            return bad;
        }

        static string R(float v) => Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
    }
}
