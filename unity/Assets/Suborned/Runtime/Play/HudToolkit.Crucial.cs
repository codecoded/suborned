using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The slim feed's one crucial card as an enamel lozenge, bottom right by the Book (brief 101; Sky 2026-11-08: "tokens bottom right"; Glyph's T mocks,
    /// briefs/101/glyph/r2/T-*.png): the book's deep teal with a cream inline, its badge or face on an orange roundel over its left end, the title, the why on one
    /// line (the cause, where, and the clock it came at), and "1 of n ›" on a small tag under its right end. A key time it is waiting on (police arriving: "here in
    /// 42 s") stands apart on a cream ticket as m:ss, orange (a key event keeps its time; brief 101). The rivals' card rings its roundel in their violet; the
    /// Alert card carries its lamps; the lookout's card keeps its fix chips (an agent's chip is a button) on a row above it.
    /// Brief 096's behaviour stands: a click on the card reads it and shows the next; a click on its line pans there and keeps it; nothing pauses or times out.
    /// The host decides (HostCrucial); this only draws.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement crCard, crLoz, crLozInline, crDiscInline, crIcon, crFace, crBadge, crLamps, crFixes, crTicket, crCountTag; Label crTitle, crLine, crCount, crTicketWords;
        int crShownId = -1; KeyText crTipK; bool crHover;
        /// <summary>Brief 101 round 3 (Fable's lozenge round): the proof reads the hover's line ("" hidden) and where the lozenge is (pixels, origin bottom left).</summary>
        /// <summary>The proof's picture holds the hover while the window changes size round it (CaptureSize), as the seal's SealHoverForce; the check reads the real pointer first.</summary>
        public static bool CrucialHoverForce;
        public static string CrucialTip => live?.crTipK != null && live.crTipK.Root.style.display != DisplayStyle.None ? live.crTipK.Words : "";
        public static Vector2? CrucialScreen { get { var b = live?.crLoz; if (b == null || b.resolvedStyle.display == DisplayStyle.None || b.worldBound.width <= 0) return null; var c = b.worldBound.center; var sz = b.panel.visualTree.layout.size; return new Vector2(c.x / sz.x * Screen.width, Screen.height - c.y / sz.y * Screen.height); } }
        /// <summary>The proof reads it: the card shown ("kind|title|line|1 of n"), "" when none.</summary>
        public static string CrucialShown { get; private set; } = "";
        /// <summary>The lozenge's place: its right end 90 px in from the screen's (the Book roundel's 56 px and 14 px of air), its foot 27 px up.</summary>
        const float CrRight = 90f, CrBottom = 13f, CrH = 50f, CrDisc = 58f, CrLineMax = 300f;

        void BuildCrucial()
        {
            crCard = E(root, null, pick: false); crCard.name = "crucial"; crCard.style.position = Position.Absolute; crCard.style.right = CrRight; crCard.style.bottom = CrBottom;
            crCard.style.flexDirection = FlexDirection.Column; crCard.style.alignItems = Align.FlexEnd; crCard.style.paddingLeft = 26; // the roundel's overhang is the card's too (the edge marks' keep-out, the manifest's rect)
            crFixes = E(crCard); crFixes.style.flexDirection = FlexDirection.Row; crFixes.style.marginBottom = 8; crFixes.style.alignSelf = Align.FlexStart; crFixes.style.marginLeft = 40;
            (crLoz, crLozInline) = Lozenge(crCard, CrH, "e-deep", pick: true); crLoz.style.paddingLeft = 40; crLoz.style.paddingRight = 24;
            crLoz.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.CrucialDismiss(); e.StopPropagation(); } });
            crLoz.RegisterCallback<PointerEnterEvent>(_ => { RingColour(crLozInline, TurqLight); crHover = true; }); crLoz.RegisterCallback<PointerLeaveEvent>(_ => { RingColour(crLozInline, null); crHover = false; });
            // brief 101 round 3 (Fable's lozenge round): hovering an Alert or Police card shows its cause's full line, filled, as a street label over the news; leaving hides it
            crTipK = KText(root, 14.5f, "k-semi"); crTipK.Root.style.position = Position.Absolute; KeyWrap(crTipK, 460f); crTipK.Face.style.unityTextAlign = TextAnchor.UpperRight; foreach (var tc in crTipK.Copies) tc.style.unityTextAlign = TextAnchor.UpperRight; Show(crTipK.Root, false); manifest.Add((crTipK.Root, "hover", () => crTipK.Words)); // round 4: a hover line, as shown
            // the roundel over the lozenge's left end
            VisualElement disc; (disc, crDiscInline) = Roundel(crLoz, CrDisc, "e-orange"); Pin(disc, new Vector2(-26f, -4f));
            crIcon = E(disc, null, w: CrDisc * 0.56f, h: CrDisc * 0.56f); crIcon.style.flexShrink = 0;
            crBadge = E(disc, null, w: CrDisc * 0.68f, h: CrDisc * 0.68f); crBadge.style.flexShrink = 0;
            crFace = E(disc, null, w: CrDisc * 0.72f, h: CrDisc * 0.72f); crFace.style.flexShrink = 0; Round(crFace, CrDisc * 0.36f); crFace.style.overflow = Overflow.Hidden;
            foreach (var x in new[] { crIcon, crBadge, crFace }) { x.style.position = Position.Absolute; }
            manifest.Add((disc, "image", () => "card badge"));
            // the title (and the Alert card's lamps), the why on one line
            var text = E(crLoz); text.style.flexShrink = 1; text.style.minWidth = 0;
            var head = E(text); head.style.flexDirection = FlexDirection.Row; head.style.alignItems = Align.Center;
            crTitle = KLabel(head, "k-bold", "k-page", 17f);
            crLamps = E(head); crLamps.style.flexDirection = FlexDirection.Row; crLamps.style.marginLeft = 8;
            for (int i = 0; i < 3; i++) { var l = E(crLamps, null, w: 11, h: 11); l.style.flexShrink = 0; l.style.marginRight = 3; Round(l, 5.5f); BorderW(l, 1.4f); l.AddToClassList("e-inline"); }
            var sub = E(text, null, pick: true); sub.style.flexDirection = FlexDirection.Row; sub.style.alignItems = Align.Center; sub.style.marginTop = 1;
            crLine = KLabel(sub, "k-medium", "k-line", 13f); crLine.style.maxWidth = CrLineMax; crLine.style.overflow = Overflow.Hidden; crLine.style.textOverflow = TextOverflow.Ellipsis; crLine.style.flexShrink = 1;
            sub.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.CrucialPan(); e.StopPropagation(); } }); // the place: pan, the card stays
            // a key time on a cream ticket (police arriving)
            crTicket = E(crLoz); crTicket.AddToClassList("e-page"); crTicket.style.height = CrH - 14f; crTicket.style.minWidth = 72; crTicket.style.marginLeft = 16;
            crTicket.style.justifyContent = Justify.Center; crTicket.style.alignItems = Align.Center; crTicket.style.paddingLeft = 8; crTicket.style.paddingRight = 8; Round(crTicket, 9f); crTicket.style.flexShrink = 0;
            crTicketWords = KLabel(crTicket, "k-bold", "k-hot", 22f);
            // "1 of n ›" under the right end
            crCountTag = E(crCard, null, pick: true); crCountTag.AddToClassList("e-body"); crCountTag.style.height = 18; crCountTag.style.marginTop = -4; Round(crCountTag, 9f);
            crCountTag.style.paddingLeft = 8; crCountTag.style.paddingRight = 8; crCountTag.style.justifyContent = Justify.Center;
            crCount = KLabel(crCountTag, "k-semi", "k-page", 11.5f);
            crCountTag.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.CrucialDismiss(); e.StopPropagation(); } });
            manifest.Add((crCard, "card", () => crTitle.text + " · " + crLine.text + (crTicket.resolvedStyle.display != DisplayStyle.None ? " · " + crTicketWords.text : "")));
            Show(crCard, false);
            BuildWhy(); BuildOnWay(); // brief 100: the meter's list (HudToolkit.Why), the report on its way (HudToolkit.OnWay)
        }

        void CrucialFrame(bool on)
        {
            WhyFrame(on); OnWayFrame(on); // brief 100
            var c = on ? host.CrucialNow : null; Show(crCard, c != null);
            if (c == null) { CrucialShown = ""; crShownId = -1; Show(crTipK.Root, false); return; }
            int n = host.CrucialQueue.Count; var words = BookWordsFile.Get();
            int keyS = host.CrucialKeyS(c); bool ticket = keyS >= 0;
            Text(crTitle, c.Title); Text(crLine, host.CrucialLineApart(c)); Text(crCount, words.FeedOf.Replace("{i}", "1").Replace("{n}", n.ToString()) + " ›");
            Show(crTicket, ticket); if (ticket) Text(crTicketWords, $"{keyS / 60}:{keyS % 60:00}");
            crLoz.style.paddingRight = ticket ? 12f : 24f; crCountTag.style.marginRight = ticket ? 78f : 0f; // Glyph's T mock: the tag under the words, clear of the ticket
            Show(crLamps, c.Kind == "alert"); if (c.Kind == "alert") for (int i = 0; i < 3; i++) crLamps[i].style.backgroundColor = i < c.Value ? BkOrange : BkBody;
            if (crShownId != c.Id)
            {
                crShownId = c.Id;
                bool face = c.Face.StartsWith("portraits/"), badge = !face && c.Face.Length > 0; Show(crFace, face); Show(crBadge, badge); Show(crIcon, !face && !badge);
                if (face) Bg(crFace, c.Face, Color.white); else if (badge) Bg(crBadge, c.Face, Color.white); else Icon(crIcon, c.Icon.Length > 0 ? c.Icon : "p_alert", Cream);
                RingColour(crDiscInline, c.Kind == "for-car" ? RivalViolet : null); // the rivals' own ring on their card (r123 §3's car disc)
                crFixes.Clear(); Show(crFixes, c.Fixes.Count > 0);
                for (int fi = 0; fi < c.Fixes.Count; fi++)
                {
                    var (icon, word) = c.Fixes[fi]; int fix = fi; bool acts = fi < c.FixBy.Count && c.FixBy[fi].Length > 0; // brief 096: a chip with an agent is a button (the lookout's ring for him)
                    var chip = E(crFixes); chip.AddToClassList("e-body"); chip.AddToClassList("e-inline"); chip.style.flexDirection = FlexDirection.Row; chip.style.alignItems = Align.Center; chip.style.height = 24;
                    chip.style.paddingLeft = 9; chip.style.paddingRight = 10; chip.style.marginRight = 6; Round(chip, 12f); BorderW(chip, 1.4f);
                    var ic = E(chip, null, w: 13, h: 13); ic.style.marginRight = 5; Icon(ic, icon, Cream); var wl = KLabel(chip, "k-bold", "k-page", 12.5f); wl.text = word;
                    if (acts) { chip.pickingMode = PickingMode.Position; RingColour(chip, TurqLight); chip.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.CrucialFix(fix); e.StopPropagation(); } }); }
                }
            }
            CrucialShown = $"{c.Kind}|{c.Title}|{crLine.text}{(ticket ? " · " + crTicketWords.text : "")}|1 of {n}";
            string tip = (crHover || CrucialHoverForce) && c.Kind is "alert" or "police" && c.Why != null ? c.Why.Line : ""; Show(crTipK.Root, tip.Length > 0);
            if (tip.Length > 0) { crTipK.Set(tip); crTipK.Root.BringToFront(); var lb = crLoz.worldBound; var cb = crCard.worldBound; var rb = root.worldBound; float tw = crTipK.Root.resolvedStyle.width, th = crTipK.Root.resolvedStyle.height; if (float.IsNaN(tw) || tw < 1f) tw = 400f; if (float.IsNaN(th) || th < 1f) th = 22f; crTipK.Root.style.left = lb.xMax - rb.x - tw; crTipK.Root.style.top = cb.yMin - rb.y - th - 10f; }
        }
    }
}
