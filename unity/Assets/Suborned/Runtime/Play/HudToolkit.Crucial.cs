using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 096, the slim feed's card (Glyph's 3a, 3b, 3d; Sky's calls 4, 7): one crucial card, bottom right above the Book key: the kit's panel_dark, a badge or
    /// face on a disc clear of the rim, a title of four words or fewer (with the Alert lamps on the Alert card), where and when in one dim line, the two fix chips on the
    /// rivals-for-the-car card, and "1 of n ›" clear of the screws. A click on the card reads it and shows the next; a click on its line pans there and keeps it.
    /// Nothing pauses and nothing times out. The host decides (HostCrucial); this only draws.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement crCard, crDisc, crIcon, crFace, crLamps, crFixes, crText; Label crTitle, crLine, crCount; int crShownId = -1;
        /// <summary>The proof reads it: the card shown ("kind|title|line|i of n"), "" when none.</summary>
        public static string CrucialShown { get; private set; } = "";

        void BuildCrucial()
        {
            crCard = E(root, "s-panel_dark", pick: true); crCard.style.position = Position.Absolute; crCard.style.right = 14; crCard.style.bottom = 14 + 40 + 8; crCard.style.width = 384; crCard.style.minHeight = 76;
            crCard.style.flexDirection = FlexDirection.Row; crCard.style.alignItems = Align.Center; crCard.style.paddingLeft = 16; crCard.style.paddingRight = 20; crCard.style.paddingTop = 8; crCard.style.paddingBottom = 8; crCard.name = "crucial";
            crDisc = E(crCard, "s-disc_dark_24", w: 44, h: 44); crDisc.style.flexShrink = 0;
            crIcon = E(crDisc, null, 10, 10, 24, 24);
            crFace = E(crDisc, null, 3, 3, 38, 38); Round(crFace, 19f); crFace.style.overflow = Overflow.Hidden;
            crText = E(crCard); crText.style.flexGrow = 1; crText.style.flexShrink = 1; crText.style.marginLeft = 12; crText.style.minWidth = 0;
            var head = E(crText); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
            crTitle = Words(head, 17f);
            crLamps = E(head); crLamps.style.flexDirection = FlexDirection.Row; crLamps.style.marginLeft = 8;
            for (int i = 0; i < 3; i++) { var l = E(crLamps, null, w: 9, h: 9); Round(l, 4.5f); l.style.marginRight = 3; Border(l, 1.3f, Hex(0x78968f)); }
            var sub = E(crText, null, pick: true); sub.style.flexDirection = FlexDirection.Row; sub.style.alignItems = Align.Center; sub.style.marginTop = 3;
            crLine = Words(sub, 13.5f, Dim); crLine.style.unityFontStyleAndWeight = FontStyle.Normal;
            sub.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.CrucialPan(); e.StopPropagation(); } }); // the place: pan, the card stays
            crFixes = E(crText); crFixes.style.flexDirection = FlexDirection.Row; crFixes.style.marginTop = 6;
            var count = E(crCard); count.style.flexDirection = FlexDirection.Row; count.style.alignItems = Align.Center; count.style.flexShrink = 0; count.style.marginLeft = 8;
            crCount = Words(count, 13f, Dim); var chev = Words(count, 20f, TurqLight); chev.text = "›"; chev.style.marginLeft = 5;
            crCard.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.CrucialDismiss(); e.StopPropagation(); } });
            manifest.Add((crCard, "card", () => crTitle.text + " · " + crLine.text));
            Show(crCard, false);
            crLine.style.whiteSpace = WhiteSpace.Normal; crLine.style.flexShrink = 1; sub.style.flexShrink = 1; // brief 100: the cause can run to a second line
            BuildWhy(); BuildOnWay(); // brief 100: the meter's list (HudToolkit.Why), the report on its way (HudToolkit.OnWay)
        }

        void CrucialFrame(bool on)
        {
            WhyFrame(on); OnWayFrame(on); // brief 100
            var c = on ? host.CrucialNow : null; Show(crCard, c != null);
            if (c == null) { CrucialShown = ""; crShownId = -1; return; }
            int n = host.CrucialQueue.Count; var words = BookWordsFile.Get();
            Text(crTitle, c.Title); Text(crLine, "→ " + host.CrucialLine(c)); Text(crCount, words.FeedOf.Replace("{i}", "1").Replace("{n}", n.ToString()));
            Show(crLamps, c.Kind == "alert"); if (c.Kind == "alert") for (int i = 0; i < 3; i++) crLamps[i].style.backgroundColor = i < c.Value ? Cream : Hex(0x0a1c1e);
            if (crShownId != c.Id)
            {
                crShownId = c.Id;
                bool face = c.Face.Length > 0; Show(crFace, face); Show(crIcon, !face);
                if (face) { if (c.Face.StartsWith("portraits/")) { Bg(crFace, c.Face, Color.white); crFace.style.backgroundColor = Hex(0x9bcfc2); } else { Bg(crFace, c.Face, Color.white); crFace.style.backgroundColor = StyleKeyword.Null; } }
                else Icon(crIcon, c.Icon.Length > 0 ? c.Icon : "p_alert", Cream);
                Border(crDisc, c.Kind == "for-car" ? 2f : 0f, RivalViolet); Round(crDisc, 22f); // the rivals' own ring on their card (r123 §3's car disc)
                crFixes.Clear(); Show(crFixes, c.Fixes.Count > 0);
                for (int fi = 0; fi < c.Fixes.Count; fi++)
                {
                    var (icon, word) = c.Fixes[fi]; int fix = fi; bool acts = fi < c.FixBy.Count && c.FixBy[fi].Length > 0; // brief 096: a chip with an agent is a button (the lookout's ring for him)
                    var chip = E(crFixes); chip.style.flexDirection = FlexDirection.Row; chip.style.alignItems = Align.Center; chip.style.height = 20; chip.style.paddingLeft = 8; chip.style.paddingRight = 9; chip.style.marginRight = 6;
                    chip.style.backgroundColor = InkDeep; Round(chip, 10f); Border(chip, 1f, PanelLine);
                    var ic = E(chip, null, w: 12, h: 12); ic.style.marginRight = 5; Icon(ic, icon, Cream); Words(chip, 12.5f).text = word;
                    if (acts) { chip.pickingMode = PickingMode.Position; Border(chip, 1f, TurqLight); chip.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.CrucialFix(fix); e.StopPropagation(); } }); }
                }
            }
            CrucialShown = $"{c.Kind}|{c.Title}|{crLine.text}|1 of {n}";
        }
    }
}
