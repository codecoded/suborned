using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101 round 3, piece 1 (Glyph's slip, briefs/101/glyph/r3/conv.png; Sky: "yes"): the card a conversation leaves is a slip of the book's cream page on
    /// a fine stem to the person's head, the bubbles' family: her face in the built cream ring breaks its top-left corner, her name small above the rows, each learned
    /// row led by its label word's kit pictogram (Fable's "Debt:" and "Secret:" become the icons; the rest of the line as written), the book's hairline, then "New" in
    /// the book's deep teal beside the ways it opened as capsules you can click (her ring opens on that way for the selection; no order is given: HandoverHost.WayClick, HostBook). Type 15
    /// px, as the bubbles. Off screen it never docks: it waits, its read time starting when she is in view, and her face among the key people (top right) carries the
    /// Book's new dot meanwhile (HudToolkit.Clock). It retires the panel_dark card and its dock beside the news (r123's "2 card panels").</summary>
    public sealed partial class HudToolkit
    {
        VisualElement personCard, slipFace, slipCol; string personCardKey = ""; float slipSeenAt = -1f;
        const float SlipFace = 44f, SlipType = 15f, SlipRow = 23f, SlipR = 14f, SlipStem = 26f, SlipText = 42f;
        /// <summary>The proof reads them: the slip is up; a card is due but its person is off screen, so it waits; what it says.</summary>
        public static bool PersonCardUp => live?.personCard != null && live.personCard.resolvedStyle.display != DisplayStyle.None;
        public static bool PersonCardWaiting { get; private set; }
        public static string PersonCardWords => live == null || !PersonCardUp ? "" : live.personCardWords;
        string personCardWords = "";
        /// <summary>Who a waiting card is about ("" none): that key face carries the Book's new dot.</summary>
        string slipWaitsFor = "";

        void PersonCardHud(float W, float H)
        {
            var c = host.PersonCardNow; float now = Time.unscaledTime; bool on = c != null && !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None && !host.FailCamOn && !host.ConvoOn;
            if (!on) { if (personCard != null) Show(personCard, false); PersonCardWaiting = false; slipWaitsFor = ""; slipSeenAt = -1f; return; }
            if (personCard == null)
            {
                personCard = E(root, null, 0, 0); personCard.name = "person-card"; personCard.style.overflow = Overflow.Visible; // in the root, as the old card: the follow view hides the street marks
                var sh = E(personCard, "e-shadow"); sh.style.position = Position.Absolute; sh.style.left = sh.style.right = 0; sh.style.top = 3; sh.style.bottom = -3; Round(sh, SlipR); sh.style.opacity = 0.55f;
                var page = E(personCard, "e-page"); page.style.position = Position.Absolute; page.style.left = page.style.right = page.style.top = page.style.bottom = 0; Round(page, SlipR);
                slipCol = E(personCard); slipCol.style.paddingLeft = SlipText; slipCol.style.paddingRight = 26; slipCol.style.paddingTop = 3; slipCol.style.paddingBottom = 21;
                slipFace = SFace(personCard, SlipFace, 2.5f); Pin(slipFace, new Vector2(-14f, -16f));
                manifest.Add((personCard, "slip", () => host.PersonCardNow is HandoverHost.PersonCard pc ? string.Join(" ", pc.Learned) + " " + string.Join(" ", pc.Opens) : ""));
            }
            // on her, over her head, clear of her speech and the pill; off screen (or behind the roster) it waits: her read time held, her key face dotted
            Vector2 q = default, qf = default; bool seen = host.AnchorWorld(c.Who) is Vector3 at && Proj(at + Vector3.up * 2.3f, out q) && Proj(at, out qf) && InPlay(q);
            if (!seen) { Show(personCard, false); PersonCardWaiting = true; slipWaitsFor = c.Who; slipSeenAt = -1f; host.PersonCardHold(c, Time.unscaledDeltaTime); return; }
            PersonCardWaiting = false; slipWaitsFor = ""; if (slipSeenAt < 0f) slipSeenAt = now;
            Show(personCard, true); PersonCardFill(c);
            float cw = personCard.resolvedStyle.width, ch = personCard.resolvedStyle.height; if (float.IsNaN(cw) || cw < 1f) cw = 360f; if (float.IsNaN(ch) || ch < 1f) ch = 137f;
            float foot = q.y - 30f, x = q.x - cw / 2f, bottom = ClearOfSpeech(SpeechRects(), x - 14f, cw + 14f, foot - SlipStem, ch + 16f); // the stem's foot just over her pip
            Rect pl = PillRect(); if (pl.width > 0 && x < pl.xMax && x + cw > pl.xMin && bottom > pl.yMin - 8f && bottom - ch < pl.yMax) bottom = pl.yMin - 8f;
            bool under = bottom - ch < 24f && qf.y + 18f + SlipStem + ch < H - 120f; // no room over her head (she stands at the top of the view): the slip hangs under her feet, its stem up to them
            float cx = Mathf.Clamp(x, 28f, Mathf.Max(28f, W - cw - 14f)), top = under ? qf.y + 18f + SlipStem : Mathf.Max(24f, bottom - ch); // the face breaks the corner 14 px out and 16 px up: room for it
            personCard.style.left = cx; personCard.style.top = top;
            float a = Mathf.Clamp01(Mathf.Min((now - slipSeenAt) / 0.15f, (c.Until - now) / 0.3f)); personCard.style.opacity = a;
            float sx = Mathf.Clamp(q.x, cx + 22f, cx + cw - 22f);
            if (under) Stem(new Vector2(sx, top + 1f), new Vector2(qf.x, qf.y + 6f), a); else Stem(new Vector2(sx, top + ch - 1f), new Vector2(q.x, foot), a);
            personCard.BringToFront();
        }

        /// <summary>A card's line split at its label word ("Debt: Bureau loan, and she's behind." is "Debt" and the rest); a line with no label, "" and the line. A way
        /// with no label ("Call Inez") leads with its verb when the kit has the verb's pictogram.</summary>
        (string label, string rest) CardSplit(string line, bool way)
        {
            int i = line.IndexOf(": ", System.StringComparison.Ordinal);
            if (i > 0 && i <= 16) return (line[..i], line[(i + 2)..]);
            if (way) { int s = line.IndexOf(' '); string w = s > 0 ? line[..s] : line; if (Sp("icons/singles/" + w.ToLowerInvariant()) != null) return (w, s > 0 ? line[(s + 1)..] : ""); }
            return ("", line);
        }

        /// <summary>A label word's kit pictogram ("Debt" is icons/singles/debt; "Listen first" leads with listen), "" when the kit has none.</summary>
        string CardIcon(string label)
        {
            if (label.Length == 0) return ""; int s = label.IndexOf(' '); string w = (s > 0 ? label[..s] : label).ToLowerInvariant();
            return Sp("icons/singles/" + w) != null ? w : "";
        }

        /// <summary>Fable's cards as written; the name is the person's, as the conversation names her.</summary>
        void PersonCardFill(HandoverHost.PersonCard c)
        {
            string key = c.Id + "|" + c.Who + "|" + string.Join(" ", c.Learned) + "|" + string.Join(" ", c.Opens); if (key == personCardKey) return; personCardKey = key; slipCol.Clear();
            string nw = host.Text?.ConvNew ?? "New";
            personCardWords = (c.Learned.Count > 0 ? string.Join(" / ", c.Learned) + " " : "") + (c.Opens.Count > 0 ? nw + " " + string.Join(" / ", c.Opens) : "");
            FaceArt(slipFace, -1, c.Who);
            var nm = KLabel(slipCol, "k-semi", "k-soft", 13.5f); nm.text = host.Text?.ConvName(c.Who) ?? c.Who; nm.style.height = 17;
            VisualElement Row() { var r = E(slipCol, null, h: SlipRow); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.overflow = Overflow.Visible; return r; }
            foreach (var l in c.Learned)
            {
                var (label, rest) = CardSplit(l, false); string icon = CardIcon(label); var r = Row();
                var ic = E(r, null, w: 16, h: 16); ic.style.flexShrink = 0; if (icon.Length > 0) Icon(ic, icon, BkInk); else ic.style.visibility = Visibility.Hidden;
                var t = KLabel(r, "k-medium", "k-ink", SlipType); t.style.marginLeft = 6; t.text = icon.Length > 0 ? rest : l;
            }
            if (c.Opens.Count == 0) return;
            if (c.Learned.Count > 0) { var hr = E(slipCol, null, h: 1.2f); hr.style.backgroundColor = BkRule; hr.style.marginTop = 4.5f; }
            bool first = true; string who = c.Who;
            foreach (var l in c.Opens)
            {
                var (label, rest) = CardSplit(l, true); string icon = CardIcon(label); var r = Row();
                if (first) { var n = KLabel(r, "k-bold", "k-deep", 12.5f); n.text = nw; n.style.position = Position.Absolute; n.style.left = 10f - SlipText; n.style.top = 0; n.style.height = SlipRow; first = false; }
                if (label.Length > 0)
                {
                    string ic = icon, wd = label; var cap = NewCapsule(r, 22f, false, () => host.WayClick(who, ic, wd)); SetCapsule(cap, icon, label);
                    var t = KLabel(r, "k-medium", "k-ink", SlipType); t.style.marginLeft = 8; t.text = rest; Show(t, rest.Length > 0);
                }
                else { var t = KLabel(r, "k-medium", "k-ink", SlipType); t.text = l; }
            }
        }
    }
}
