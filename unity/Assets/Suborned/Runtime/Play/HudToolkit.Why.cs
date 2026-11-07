using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 100, part 3, the meter remembers (Fable's `why.meter`; the layout is Glyph's to settle): the pill's three Alert lamps take a hit box (they are 12 px:
    /// the box runs the pill's height and 6 px either side); a hover shows the list and a click pins it (a click again, or on its head, folds it). The list hangs
    /// under the pill as the response drawer does (the kit's panel_dark, 318 wide): its head ("Why it's Alert 2", or "Nobody has raised the alarm"), then the rises
    /// and calls newest first, each `when` (dim) over `what` (cream). A click on a row pans to where it happened; the hovered row's sentence (its line, `again`
    /// and what the Alert set off) shows under the rows. Clickable, never key-only. The host words it all (HostWhy); this only draws.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement whyHit, whyList, whyRows; Label whyHead, whyLine; bool whyHover, whyListHover, whyPinned; int whyHoverRow = -1;
        readonly List<(VisualElement row, Label when, Label what)> whyRowUi = new(); readonly List<HandoverHost.WhyRow> whyShown = new();
        const int WhyMax = 6;
        /// <summary>The proof reads it: the list as drawn ("head|when / what|…"), "" while folded.</summary>
        public static string WhyMeterShown { get; private set; } = "";
        /// <summary>The proof reads it: the lamps' hit box on screen (pixels, origin bottom left), null when hidden.</summary>
        public static Vector2? WhyMeterScreen { get { var b = live?.whyHit; if (b == null || b.worldBound.width <= 0) return null; var c = b.worldBound.center; var sz = b.panel.visualTree.layout.size; return new Vector2(c.x / sz.x * Screen.width, Screen.height - c.y / sz.y * Screen.height); } }
        /// <summary>The proof (and a click) folds or pins the list.</summary>
        public static void WhyMeterPin(bool on) { if (live != null) live.whyPinned = on; }

        void BuildWhy()
        {
            whyHit = E(pill, null, pick: true); whyHit.name = "why-lamps"; whyHit.style.position = Position.Absolute; whyHit.style.top = 0; whyHit.style.height = 44;
            whyHit.RegisterCallback<PointerEnterEvent>(_ => whyHover = true); whyHit.RegisterCallback<PointerLeaveEvent>(_ => whyHover = false);
            whyHit.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { whyPinned = !whyPinned; e.StopPropagation(); } });
            whyList = E(root, "s-panel_dark", pick: true); whyList.name = "why-list"; whyList.style.position = Position.Absolute; whyList.style.right = 14; whyList.style.top = 14 + 44 + 8; whyList.style.width = 318;
            whyList.style.paddingTop = 12; whyList.style.paddingBottom = 12; whyList.style.paddingLeft = 22; whyList.style.paddingRight = 18;
            whyList.RegisterCallback<PointerEnterEvent>(_ => whyListHover = true); whyList.RegisterCallback<PointerLeaveEvent>(_ => { whyListHover = false; whyHoverRow = -1; });
            whyHead = Words(whyList, 15f); whyHead.style.marginBottom = 6;
            whyHead.pickingMode = PickingMode.Position; whyHead.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { whyPinned = false; whyListHover = false; e.StopPropagation(); } });
            whyRows = E(whyList);
            for (int i = 0; i < WhyMax; i++)
            {
                int k = i; var r = E(whyRows, null, pick: true); r.style.paddingTop = 3; r.style.paddingBottom = 3;
                var when = Words(r, 12.5f, Dim); when.style.unityFontStyleAndWeight = FontStyle.Normal;
                var what = Words(r, 13.5f); what.style.whiteSpace = WhiteSpace.Normal;
                r.RegisterCallback<PointerEnterEvent>(_ => whyHoverRow = k); r.RegisterCallback<PointerLeaveEvent>(_ => { if (whyHoverRow == k) whyHoverRow = -1; });
                r.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && k < whyShown.Count && whyShown[k].Place is Vector3 at) { host.LookAt(at); e.StopPropagation(); } });
                whyRowUi.Add((r, when, what));
            }
            whyLine = Words(whyList, 12.5f, Dim); whyLine.style.whiteSpace = WhiteSpace.Normal; whyLine.style.marginTop = 6; whyLine.style.unityFontStyleAndWeight = FontStyle.Normal;
            Show(whyList, false);
        }

        void WhyFrame(bool on)
        {
            if (whyHit == null) return;
            bool can = on && host.Text != null && host.Text.HasWhy; Show(whyHit, can);
            if (can) { var a = lamps[0].layout; var b = lamps[2].layout; if (!float.IsNaN(a.x) && b.width > 0) { whyHit.style.left = a.x - 6; whyHit.style.width = b.xMax - a.x + 12; } }
            bool open = can && (whyPinned || whyHover || whyListHover); Show(whyList, open);
            if (!open) { WhyMeterShown = ""; if (!can) whyPinned = false; return; }
            whyList.BringToFront();
            Text(whyHead, host.WhyMeterHead());
            whyShown.Clear(); whyShown.AddRange(host.WhyMeterRows(WhyMax));
            var shown = new List<string> { whyHead.text };
            for (int i = 0; i < whyRowUi.Count; i++)
            {
                var (r, when, what) = whyRowUi[i]; bool has = i < whyShown.Count; Show(r, has); if (!has) continue;
                var (w, x) = host.WhyMeterLines(whyShown[i]); Text(when, w); Text(what, x); Show(what, x.Length > 0);
                r.style.backgroundColor = i == whyHoverRow ? new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.55f) : StyleKeyword.Null;
                shown.Add(x.Length > 0 ? $"{w} / {x}" : w);
            }
            string line = whyHoverRow >= 0 && whyHoverRow < whyShown.Count ? host.WhyFull(whyShown[whyHoverRow]) : "";
            Text(whyLine, line); Show(whyLine, line.Length > 0);
            WhyMeterShown = string.Join("|", shown);
        }
    }
}
