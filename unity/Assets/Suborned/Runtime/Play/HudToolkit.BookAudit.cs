using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 099: the book's page measured as it is drawn, for the debrief's captures (DebriefShots): a manifest of every word and picture on it in screen pixels (the
    /// format tools/hud-budget reads; state `page`, the reading surface outside play), and the page's own layout audit: no two words over each other, every word on the
    /// screen and clear of the fold's veil unless the fold keeps it. The HUD capture writes no manifest while the book is up (brief 096); this is the book's. Reads only.</summary>
    public sealed partial class HudToolkit
    {
        struct PagePiece { public string Kind, Text; public Rect At; public VisualElement E; }

        /// <summary>The page's words and pictures in screen pixels. A word: its label's line box (`ink`: the ink alone, cap height and a little, for the overlap audit). A picture: a face,
        /// an icon, a disc or a pip (a small element with an image or a solid fill and no words in it), taken whole, its icon with it.</summary>
        static List<PagePiece> BookPieces(bool ink)
        {
            var l = new List<PagePiece>(); var bk = live?.bk; if (bk == null || live.root?.panel == null) return l;
            float k = Screen.height / Mathf.Max(1f, live.root.panel.visualTree.layout.height);
            static bool Words(VisualElement e) { if (e is Label x && !string.IsNullOrEmpty(x.text)) return true; foreach (var c in e.Children()) if (Words(c)) return true; return false; }
            void Walk(VisualElement e)
            {
                if (e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.visibility == Visibility.Hidden || e.resolvedStyle.opacity < 0.05f) return;
                var b = e.worldBound; var at = new Rect(b.x * k, b.y * k, b.width * k, b.height * k);
                if (e is Label lb && !string.IsNullOrEmpty(lb.text))
                {
                    var need = lb.MeasureTextSize(lb.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                    float h = ink ? Mathf.Min(at.height, lb.resolvedStyle.fontSize * 0.74f * k) : at.height, y = at.y + (at.height - h) / 2f, w = Mathf.Min(at.width, need.x * k);
                    var al = lb.resolvedStyle.unityTextAlign; float x = al is TextAnchor.MiddleCenter or TextAnchor.UpperCenter or TextAnchor.LowerCenter ? at.x + (at.width - w) / 2f : al is TextAnchor.MiddleRight or TextAnchor.UpperRight or TextAnchor.LowerRight ? at.xMax - w : at.x; // where the ink is
                    l.Add(new PagePiece { Kind = "hud", Text = lb.text, At = new Rect(x, y, w, h), E = e });
                    return;
                }
                bool art = e.resolvedStyle.backgroundImage.sprite != null || e.resolvedStyle.backgroundImage.texture != null, fill = e.resolvedStyle.backgroundColor.a >= 0.5f;
                bool picture = Mathf.Min(at.width, at.height) >= 6f * k && Mathf.Max(at.width, at.height) <= 2.5f * Mathf.Min(at.width, at.height); // a face, an icon, a disc or a pip; a rule, a joining line or a plate behind words is not one
                if ((art || fill) && picture && at.width <= 360f * k && !Words(e)) { l.Add(new PagePiece { Kind = "image", Text = e.name.Length > 0 ? e.name : "picture", At = at, E = e }); return; }
                foreach (var c in e.Children()) Walk(c);
            }
            Walk(bk); return l;
        }

        /// <summary>The page's manifest (tools/hud-budget's format), every word and picture on it.</summary>
        public static string BookManifest(string state = "page")
        {
            var sb = new StringBuilder("{\n \"state\": \"" + state + "\",\n \"elements\": [\n"); var ps = BookPieces(false);
            string N(float v) => Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture);
            for (int i = 0; i < ps.Count; i++) { var p = ps[i]; sb.Append($"  {{\"kind\": \"{p.Kind}\", \"x\": {N(p.At.x)}, \"y\": {N(p.At.y)}, \"w\": {N(p.At.width)}, \"h\": {N(p.At.height)}, \"text\": \"{p.Text.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"}}{(i < ps.Count - 1 ? "," : "")}\n"); }
            return sb.Append(" ]\n}\n").ToString();
        }

        /// <summary>The page's layout audit: two words whose ink overlaps (by more than a pixel), a word off the screen, a word cut at its box. Empty = clean. A word the open fold
        /// covers on purpose (under the veil) is not counted against the fold's own.</summary>
        public static List<string> BookAudit()
        {
            var bad = new List<string>(); var ps = BookPieces(true); if (ps.Count == 0) { bad.Add("no page"); return bad; }
            VisualElement fold = null; live.bk.Query<VisualElement>("debrief-fold").ForEach(e => fold = e);
            bool InFold(VisualElement e) { for (var x = e; x != null; x = x.parent) if (x == fold) return true; return false; }
            var words = ps.FindAll(p => p.Kind == "hud");
            foreach (var w in words) if (w.At.xMin < -0.5f || w.At.yMin < -0.5f || w.At.xMax > Screen.width + 0.5f || w.At.yMax > Screen.height + 0.5f) bad.Add($"'{w.Text}' off the screen at {w.At}");
            for (int a = 0; a < words.Count; a++)
                for (int b = a + 1; b < words.Count; b++)
                {
                    var A = words[a]; var B = words[b]; if (fold != null && InFold(A.E) != InFold(B.E)) continue; // the fold lies over the stepped-back page by design
                    var o = Rect.MinMaxRect(Mathf.Max(A.At.xMin, B.At.xMin), Mathf.Max(A.At.yMin, B.At.yMin), Mathf.Min(A.At.xMax, B.At.xMax), Mathf.Min(A.At.yMax, B.At.yMax));
                    if (o.width > 1f && o.height > 1f) bad.Add($"'{A.Text}' and '{B.Text}' overlap ({o.width:0}x{o.height:0} px)");
                }
            return bad;
        }
    }
}
