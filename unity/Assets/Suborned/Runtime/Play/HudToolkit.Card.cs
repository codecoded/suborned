using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 085, look C (GDD v0.36.0 section 10; Glyph's mock, its right side): the card a conversation leaves, in play, on the person, in the done chip's spot: her face and name, what was learned,
    /// a brass hairline, then "New" beside the actions it opened. Off screen it docks on the feed's top edge, the face saying who. It takes no clicks.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement personCard, personCardTail; string personCardKey = "";
        const float CardFace = 46f, CardType = 19f, CardNameType = 15f;
        /// <summary>The proof reads them: the card is up, it's docked, what it says.</summary>
        public static bool PersonCardUp => live?.personCard != null && live.personCard.resolvedStyle.display != DisplayStyle.None;
        public static bool PersonCardDocked { get; private set; }
        public static string PersonCardWords => live == null || !PersonCardUp ? "" : live.personCardWords;
        string personCardWords = "";

        void PersonCardHud(float W, float H)
        {
            var c = host.PersonCardNow; float now = Time.unscaledTime; bool on = c != null && !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None && !host.FailCamOn && !host.ConvoOn;
            if (personCard == null)
            {
                if (!on) return;
                personCard = E(root, "s-panel_dark"); personCard.style.position = Position.Absolute; personCard.style.paddingLeft = 18; personCard.style.paddingRight = 22; personCard.style.paddingTop = 13; personCard.style.paddingBottom = 14; personCard.style.unitySliceScale = 0.5f; personCard.pickingMode = PickingMode.Ignore; personCard.style.overflow = Overflow.Visible;
                manifest.Add((personCard, "card", () => host.PersonCardNow is HandoverHost.PersonCard pc ? string.Join(" ", pc.Learned) + " " + string.Join(" ", pc.Opens) : ""));
            }
            Show(personCard, on); if (!on) return;
            PersonCardFill(c);
            float cw = personCard.resolvedStyle.width, ch = personCard.resolvedStyle.height; if (float.IsNaN(cw) || cw < 1f) cw = 420f; if (float.IsNaN(ch) || ch < 1f) ch = 130f;
            // on the person, as a done chip stands: above the head, above her own speech, clear of the pill; off screen, on the feed's top edge
            bool seen = host.AnchorWorld(c.Who) is Vector3 at && Proj(at + Vector3.up * 2.3f, out var q) && InView(q) && q.y >= ch * 0.3f; q = default; if (host.AnchorWorld(c.Who) is Vector3 at2) Proj(at2 + Vector3.up * 2.3f, out q);
            PersonCardDocked = !seen; Show(personCardTail, seen);
            if (seen)
            {
                Rect pl = PillRect(); float x = q.x - cw / 2f, bottom = ClearOfSpeech(SpeechRects(), x, cw, q.y - 16f, ch);
                if (pl.width > 0 && x < pl.xMax && x + cw > pl.xMin && bottom > pl.yMin - 8f && bottom - ch < pl.yMax) bottom = pl.yMin - 8f;
                float cx = Mathf.Clamp(x, 14f, Mathf.Max(14f, W - cw - 14f)); personCard.style.left = cx; personCard.style.right = StyleKeyword.Auto; personCard.style.top = Mathf.Max(8f, bottom - ch);
                personCardTail.style.left = Mathf.Clamp(q.x - cx - 6f, 20f, cw - 32f);
            }
            else
            {
                float top = feed != null && feed.resolvedStyle.display != DisplayStyle.None && feed.worldBound.height > 1f ? feed.worldBound.yMin : H - 130f;
                personCard.style.left = StyleKeyword.Auto; personCard.style.right = 14; personCard.style.top = Mathf.Max(120f, top - ch - 10f);
            }
            personCard.style.opacity = Mathf.Clamp01(Mathf.Min((now - c.At) / 0.15f, (c.Until - now) / 0.3f)); personCard.BringToFront();
        }

        /// <summary>Fable's cards as written; the header is the person's name, as the conversation names her.</summary>
        void PersonCardFill(HandoverHost.PersonCard c)
        {
            string key = c.Id + "|" + c.Who + "|" + string.Join(" ", c.Learned) + "|" + string.Join(" ", c.Opens); if (key == personCardKey) return; personCardKey = key; personCard.Clear();
            personCardWords = (c.Learned.Count > 0 ? string.Join(" / ", c.Learned) + " " : "") + (c.Opens.Count > 0 ? (host.Text?.ConvNew ?? "New") + " " + string.Join(" / ", c.Opens) : "");
            var top = E(personCard); top.style.flexDirection = FlexDirection.Row; top.style.alignItems = Align.Center;
            var face = E(top, null, w: CardFace, h: CardFace); face.style.flexShrink = 0; Round(face, CardFace / 2f); Border(face, 2f, Chrome); Bg(face, FaceOf(c.Who), Color.white); face.style.marginRight = 12;
            var col = E(top); var nm = Words(col, CardNameType, Dim); nm.text = host.Text?.ConvName(c.Who) ?? c.Who; nm.style.marginBottom = 1;
            foreach (var l in c.Learned) { var t = Words(col, CardType); t.text = l; t.style.marginTop = 2; }
            if (c.Opens.Count > 0)
            {
                if (c.Learned.Count > 0) { var hr = E(personCard, h: 1); hr.style.backgroundColor = LookHair; hr.style.marginTop = 8; hr.style.marginBottom = 7; }
                else top.style.marginBottom = 8;
                var row = E(personCard); row.style.flexDirection = FlexDirection.Row;
                var nw = Words(row, CardType, LookLive); nw.text = host.Text?.ConvNew ?? "New"; nw.style.width = CardFace + 12f; nw.style.flexShrink = 0; nw.style.unityTextAlign = TextAnchor.UpperLeft;
                var rows = E(row);
                foreach (var l in c.Opens)
                {
                    var r = E(rows); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginBottom = 2;
                    var ch = E(r, null, w: 12, h: 12); ch.style.flexShrink = 0; ch.style.marginRight = 7; Icon(ch, "chev_down", LookLive); ch.style.rotate = new Rotate(new Angle(-90, AngleUnit.Degree));
                    var t = Words(r, CardType); t.text = l;
                }
            }
            personCardTail = E(personCard, null, 0, 0, 12, 12); personCardTail.style.position = Position.Absolute; personCardTail.style.rotate = new Rotate(45); personCardTail.style.backgroundColor = new Color(0.047f, 0.204f, 0.204f); personCardTail.style.top = Length.Percent(100); personCardTail.style.marginTop = -7;
        }
    }
}
