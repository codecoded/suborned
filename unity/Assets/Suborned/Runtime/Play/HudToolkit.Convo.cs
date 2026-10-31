using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 032, 085: the conversation scene's HUD: its bars and dip, the one bubble with its speaker's name, a dot a line, Skip, the clock, and at the end the cards. Split from HudToolkit.Live.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement convoBubble, convoTail, convoSkip, convoBarTop, convoBarBottom, convoVeil, convoCards; Label convoText, convoName; Paint convoDots; bool convoHid; string convoCardKey = "";
        /// <summary>Elements that are not HUD for the budget (the letterbox bars, the dip): the HUD layer of a shot is drawn without them.</summary>
        readonly List<VisualElement> notHud = new();

        /// <summary>The proof reads it: the scene's bubble stands under its speaker's head (flipped) this frame.</summary>
        public static bool ConvoBubbleFlipped { get; private set; }
        /// <summary>The proof reads it: what the bubble and the name plate say this frame, and the cards up.</summary>
        public static string ConvoSaid => live?.convoBubble != null && live.convoBubble.resolvedStyle.display != DisplayStyle.None ? $"{live.convoName.text}: {live.convoText.text}" : "";
        public static bool ConvoCardsUp => live?.convoCards != null && live.convoCards.resolvedStyle.display != DisplayStyle.None;

        bool ConvoHud()
        {
            var c = host.ConvoNow; bool on = c != null;
            if (!on && !convoHid) return false;
            if (convoBubble == null)
            {
                convoBarTop = E(root); convoBarBottom = E(root); convoVeil = E(root);
                foreach (var b in new[] { convoBarTop, convoBarBottom, convoVeil }) { b.style.position = Position.Absolute; b.style.left = 0; b.style.right = 0; b.style.backgroundColor = Color.black; b.pickingMode = PickingMode.Ignore; notHud.Add(b); }
                convoBarTop.style.top = 0; convoBarTop.style.height = Length.Percent(6); convoBarBottom.style.bottom = 0; convoBarBottom.style.height = Length.Percent(6); convoVeil.style.top = 0; convoVeil.style.bottom = 0;
                convoBubble = E(root, "s-plate_light"); convoBubble.style.position = Position.Absolute; convoBubble.style.paddingLeft = 14; convoBubble.style.paddingRight = 14; convoBubble.style.paddingTop = 6; convoBubble.style.paddingBottom = 7; convoBubble.style.maxWidth = 560;
                convoName = L(convoBubble, "verb-name"); convoName.style.color = Ink; convoName.style.fontSize = 13.5f; convoName.style.marginBottom = 1;
                convoText = L(convoBubble, "card-body"); convoText.style.color = Ink; convoText.style.fontSize = 20; convoText.style.whiteSpace = WhiteSpace.Normal;
                convoTail = E(convoBubble, null, 0, 0, 12, 12); convoTail.style.rotate = new Rotate(45); convoTail.style.backgroundColor = new Color(0.98f, 0.95f, 0.89f); convoTail.style.top = Length.Percent(100); convoTail.style.marginTop = -8; convoTail.style.left = Length.Percent(50); convoTail.style.marginLeft = -6;
                manifest.Add((convoBubble, "speech", () => $"{convoName.text} {convoText.text}"));
                convoDots = P(root, 0, 0, 60, 14, (p, r) => { var cc = host.ConvoNow; if (cc == null) return; int n = cc.Lines.Count; for (int i = 0; i < n; i++) { var q = new Vector2(r.width / 2 + (i - (n - 1) / 2f) * 14f, 7); p.BeginPath(); p.Arc(q, 4f, Deg(0), Deg(360)); if (i <= cc.Beat) { p.fillColor = Cream; p.Fill(); } p.strokeColor = Cream; p.lineWidth = 1.4f; p.Stroke(); } });
                manifest.Add((convoDots, "image", () => "scene beats"));
                convoSkip = E(root, "s-plate_light", pick: true); convoSkip.style.position = Position.Absolute; convoSkip.style.height = 28; convoSkip.style.flexDirection = FlexDirection.Row; convoSkip.style.alignItems = Align.Center; convoSkip.style.paddingLeft = 12; convoSkip.style.paddingRight = 10;
                var sk = L(convoSkip, "keycap"); sk.text = "Skip"; sk.style.fontSize = 14; var chev = E(convoSkip, null, w: 12, h: 12); chev.style.marginLeft = 6; Icon(chev, "chev_down", Ink); chev.style.rotate = new Rotate(new Angle(-90, AngleUnit.Degree));
                convoSkip.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ConvoSkip(); e.StopPropagation(); } });
                manifest.Add((convoSkip, "image", () => "skip keycap"));
                // the cards: the kit's panel_dark, one icon and line a card, a brass hairline between what the player now knows and what they can now do
                convoCards = E(root, "s-panel_dark"); convoCards.style.position = Position.Absolute; convoCards.style.paddingLeft = 22; convoCards.style.paddingRight = 24; convoCards.style.paddingTop = 14; convoCards.style.paddingBottom = 14; convoCards.style.unitySliceScale = 0.5f;
                manifest.Add((convoCards, "speech", ConvoCardWords));
            }
            var mine = new HashSet<VisualElement> { convoBarTop, convoBarBottom, convoVeil, convoBubble, convoDots, convoSkip, convoCards, pill };
            if (on != convoHid)
            {
                convoHid = on;
                foreach (var ch in root.Children()) if (!mine.Contains(ch)) { ch.style.visibility = on ? Visibility.Hidden : StyleKeyword.Null; ch.style.opacity = on ? 0f : StyleKeyword.Null; } // opacity too: a descendant set visible (the pips, a chip's bar) shows through a hidden parent
                foreach (var e in mine) if (e != pill) Show(e, on);
                if (!on) { Show(pauseIcon, host.Paused); pauseIcon.style.opacity = 1f; return false; }
            }
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) ? 1920 : size.x, H = float.IsNaN(size.y) ? 1080 : size.y;
            // the clock with its paused mark, pulsing (the freeze is explained); the bars and the dip
            Text(clock, m.Clock); Show(pauseIcon, true); pauseIcon.style.opacity = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f));
            convoVeil.style.opacity = host.ConvoDip; Show(convoVeil, host.ConvoDip > 0.001f); convoVeil.BringToFront();
            bool shot = c.OutAt < 0f || host.ConvoDip > 0f && Time.unscaledTime - c.OutAt < HandoverHost.ConvoDipS * 0.5f;
            Show(convoBarTop, shot); Show(convoBarBottom, shot);
            // one bubble at its speaker: the tail says who, the plate's first line names them; a voice with no figure in the frame has no tail; whole, held to read
            bool speak = shot && !c.InCards && c.Beat < c.Lines.Count && Time.unscaledTime >= c.BeatAt; Show(convoBubble, speak);
            if (speak)
            {
                var (who, line) = c.Lines[c.Beat]; Text(convoText, line); Text(convoName, c.Id.Length > 0 ? host.Text?.ConvName(who) ?? who : host.Text?.Speaker(who) ?? who);
                float bw = convoBubble.resolvedStyle.width, bh = convoBubble.resolvedStyle.height; if (float.IsNaN(bw) || bw < 1) bw = 280; if (float.IsNaN(bh) || bh < 1) bh = 36;
                Vector2 at = default; bool tail = host.ConvoHead(who) is Vector3 hw && Proj(hw, out at) && InView(at);
                if (!tail) at = new Vector2(W * 0.3f, H * 0.62f);
                // Glyph 067 H1.2: clamped to the frame less the bars and a 4 % margin; a bubble that would leave the frame above flips under the head, its tail up
                float mx = Mathf.Max(24f, W * 0.04f), top = Mathf.Max(H * 0.06f + 56, H * 0.10f), bottom = H * 0.94f - Mathf.Max(60f, H * 0.04f);
                bool flip = tail && at.y - bh - 10 < top;
                float x = Mathf.Clamp(at.x - bw / 2, mx, W - bw - mx), y = Mathf.Clamp(flip ? at.y + 16 : at.y - bh - 10, top, bottom - bh);
                convoBubble.style.left = x; convoBubble.style.top = y; Show(convoTail, tail); if (tail) { convoTail.style.left = Mathf.Clamp(at.x - x, 16, bw - 16); convoTail.style.marginLeft = -6; convoTail.style.top = flip ? Length.Percent(0) : Length.Percent(100); convoTail.style.marginTop = flip ? -4 : -8; }
                ConvoBubbleFlipped = flip;
                convoBubble.style.opacity = Mathf.Clamp01((Time.unscaledTime - c.BeatAt) / 0.12f);
            }
            // the cards, on the bottom bar's edge
            bool cards = shot && c.InCards; Show(convoCards, cards);
            if (cards)
            {
                ConvoCardsFill(c);
                float cw = convoCards.resolvedStyle.width, ch = convoCards.resolvedStyle.height; if (float.IsNaN(cw) || cw < 1) cw = 420; if (float.IsNaN(ch) || ch < 1) ch = 120;
                convoCards.style.left = (W - cw) / 2f; convoCards.style.top = Mathf.Max(H * 0.1f, H * 0.94f - ch - 32f); convoCards.style.opacity = Mathf.Clamp01((Time.unscaledTime - c.BeatAt) / 0.15f); convoCards.BringToFront();
            }
            // the row of dots under the pair (the played ones filled)
            Show(convoDots, shot && !c.InCards && c.Lines.Count > 1);
            if (shot && host.ConvoFeet(c) is Vector3 fw && Proj(fw, out var fq)) { float dw = c.Lines.Count * 14f + 14f; convoDots.style.left = Mathf.Clamp(fq.x - dw / 2f, 24, W - dw - 24); convoDots.style.top = Mathf.Min(fq.y + 18, H * 0.94f - 30); convoDots.style.width = dw; convoDots.MarkDirtyRepaint(); }
            // Skip, bottom right over the bar's edge
            Show(convoSkip, shot); convoSkip.style.right = 40; convoSkip.style.bottom = H * 0.06f + 14;
            pill.BringToFront(); convoBubble.BringToFront(); convoSkip.BringToFront(); convoVeil.BringToFront(); // the clock over the top bar
            return true;
        }

        string ConvoCardWords() { var c = host.ConvoNow; return c == null ? "" : string.Join(" ", c.Learned) + " " + string.Join(" ", c.Opens); }

        /// <summary>Fable's cards as written. Learned leads with the Listen ear, Opens with the kit's pointer; no bar, no kind colour.</summary>
        void ConvoCardsFill(HandoverHost.Convo c)
        {
            string key = c.Id + "|" + ConvoCardWords(); if (key == convoCardKey) return; convoCardKey = key; convoCards.Clear();
            void Row(string icon, string line) { var r = E(convoCards); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 3; r.style.marginBottom = 3; var ic = E(r, null, w: 20, h: 20); ic.style.marginRight = 10; ic.style.flexShrink = 0; Icon(ic, icon, Cream); var t = L(r, "verb-name"); t.style.color = Cream; t.style.fontSize = 18; t.text = line; }
            foreach (var l in c.Learned) Row("listen", l);
            if (c.Learned.Count > 0 && c.Opens.Count > 0) { var hr = E(convoCards, h: 1); hr.style.backgroundColor = new Color(0.6f, 0.47f, 0.30f); hr.style.marginTop = 6; hr.style.marginBottom = 6; }
            foreach (var l in c.Opens) Row("pointer", l);
        }
    }
}
