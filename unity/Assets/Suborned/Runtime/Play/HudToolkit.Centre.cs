using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101, centring (Sky 2026-11-10, build 0.2.310: "another non centered icon"; Glyph, briefs/101/glyph/centring.md). The one rule for
    /// a number, letter or icon inside a disc, ring or plate. Views only: nothing here reaches the engine.
    ///
    /// 1. The border. UI Toolkit lays an absolute child out from inside its parent's border (Yoga's padding box), so a child put at (box - size) / 2 in
    ///    a bordered disc sits `border` px right and low. The destination disc's "1" was 1.6 px right and 1.6 px low when selected (measured 1.2 and 1.7
    ///    in Sky's frame, 1.5 and 2.0 for a "4" in the batch's fight-safe-body). A centred child stands in Middle(): a holder stretched to the padding box
    ///    (left, right, top and bottom 0) that centres its child by flex, so it stays centred at any border width, including a ring that thickens
    ///    on selection or a pooled disc that gets a rim. A label fills the padding box the same way (CentreNum). The scope's numbered blips already did this.
    /// 2. The glyph. A label centres its advance, not its ink. Jost's tabular "1" has its ink 0.055 em left of its advance centre (0.048 in Medium;
    ///    fontTools on the kit's TTFs), so CentreNum nudges a single character by its ink offset. Only glyphs off by 0.02 em or more are listed: 7 is +0.013,
    ///    the other figures under 0.02. Vertically, Unity centres the line box (hhea 1070 / -375, middle 0.3475 em over the baseline), and Jost's figures
    ///    and capitals ink 0 to 0.70-0.71 em (middle 0.35-0.355), so no vertical nudge is needed: under 0.1 px at these sizes.</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>A w x h child centred in `parent` whatever its border: the child sits in a holder stretched over the parent's padding box.</summary>
        static VisualElement Middle(VisualElement parent, float w, float h, string name = null)
        {
            var hold = new VisualElement { pickingMode = PickingMode.Ignore, focusable = false }; FrontEnd.Fill(hold); // stretched over the padding box (the inside of the border)
            hold.style.alignItems = Align.Center; hold.style.justifyContent = Justify.Center; parent.Add(hold);
            var e = E(hold, null, w: w, h: h); e.style.flexShrink = 0; if (name != null) e.name = name; return e;
        }

        /// <summary>A number or letter centred in its disc or plate: the label fills the padding box, middle-centred. Set its text with SetNum().</summary>
        static Label CentreNum(VisualElement parent, string style)
        {
            var l = L(parent, style); FrontEnd.Fill(l); l.style.unityTextAlign = TextAnchor.MiddleCenter; return l;
        }

        /// <summary>A centred label's text and its optical nudge: a single character moves by its ink offset (InkOffset), anything else sits on its advance.</summary>
        static void SetNum(Label l, string s)
        {
            Text(l, s);
            float dx = 0f;
            if (s != null && s.Length == 1)
            {
                var f = l.style.unityFontDefinition.value.font ?? l.resolvedStyle.unityFont; float size = l.resolvedStyle.fontSize;
                if (l.style.fontSize.keyword == StyleKeyword.Undefined && l.style.fontSize.value.value > 0f) size = l.style.fontSize.value.value; // an inline size wins before the first layout
                dx = -InkOffset(f != null ? f.name : "", s[0]) * size;
            }
            var cur = l.style.translate; if (cur.keyword != StyleKeyword.Undefined || cur.value.x.value != dx) l.style.translate = new Translate(dx, 0f);
        }

        /// <summary>Where a glyph's ink centre sits from its advance centre, in em (+ right): (left bearing - right bearing) / 2, from the kit's TTFs.</summary>
        static float InkOffset(string font, char c)
        {
            var table = font.StartsWith("Jost-Bold") ? InkBold : font.StartsWith("Jost-Medium") ? InkMedium : InkRegular; // the HUD's numplate and keycap take fontSpec.body, Jost-Regular-Tab
            return table.TryGetValue(char.ToUpperInvariant(c), out var o) ? o / 1000f : 0f;
        }
        static readonly Dictionary<char, float> InkRegular = new() { ['1'] = -55f, ['D'] = 20f, ['J'] = -95.5f, ['K'] = 37.5f, ['L'] = 35f, ['P'] = 20f, ['R'] = 35f };
        static readonly Dictionary<char, float> InkMedium = new() { ['1'] = -48f, ['D'] = 20.5f, ['J'] = -104.5f, ['K'] = 38f, ['L'] = 35.5f, ['P'] = 20.5f, ['R'] = 35.5f };
        static readonly Dictionary<char, float> InkBold = new() { ['D'] = 20f, ['J'] = -119f, ['K'] = 37.5f, ['L'] = 35f, ['P'] = 20f, ['Q'] = 20f, ['R'] = 35f };

        /// <summary>The book's mark in a disc ("?" on a hollow need, fact or tick): the label spans the disc's inside, middle-centred, on its centre
        /// line (BkPut placed it from the disc's outer edge, so it sat the border's width right and low).</summary>
        Label BkMid(VisualElement disc, float d, float border, string text, float size, string font, string hex)
        {
            var l = BkPut(disc, 0f, d / 2f - border, text, size, font, hex); l.style.width = d - 2f * border; l.style.unityTextAlign = TextAnchor.MiddleCenter; return l;
        }
    }
}
