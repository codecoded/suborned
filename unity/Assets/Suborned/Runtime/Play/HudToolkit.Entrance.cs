using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>What stays of brief 070's briefing page (Briefing.cs, retired by brief 096: the start is the book's Job page, HostBook.Start): the HUD's entrance
    /// after Begin, and the page type's one-line text, which the book draws with. `BriefOnStreet` keeps the 0.2.117 card hidden (it is built but never shown).</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>The 0.2.117 briefing card stays built and hidden (on: hidden). Only a comparison would turn it off.</summary>
        public static bool BriefOnStreet = true;

        /// <summary>One line of page type at (x, y) in the page grid (Jost; a colour from the kit or the contract).</summary>
        static Label PgText(VisualElement parent, float x, float y, string font, float size, string hex, string text, float track = 0f, bool upper = false)
        {
            var l = new Label { pickingMode = PickingMode.Ignore }; parent.Add(l); l.style.position = Position.Absolute; if (!float.IsNaN(x)) l.style.left = x; l.style.top = y;
            FrontEnd.Typeset(l, new FrontEnd.TypeS { font = font, sizeRef = size, trackingEm = track, uppercase = upper }, text ?? "", hex); l.style.whiteSpace = WhiteSpace.NoWrap;
            return l;
        }

        /// <summary>Brief 062 §5, the HUD's entrance after Begin: hidden under the briefing and the glide; at 3.3 s the roster slides in from the
        /// left over 0.25 s; at 3.6 s the clock bar fades in (0.3 s) as the clock starts. Per frame, after the roster and the pill are laid out.</summary>
        void Entrance()
        {
            var h = host; float t = h.StartT;
            float roster = h.ShowBriefing ? 0f : h.Starting ? Mathf.Clamp01((t - HandoverHost.BriefExitS - HandoverHost.BriefGlideS) / HandoverHost.BriefRosterS) : 1f;
            float clockA = h.ShowBriefing || h.Starting ? 0f : Mathf.Clamp01((Time.unscaledTime - h.SequencePlayedAt) / 0.3f); // a script's start shows it at once
            if (h.ShowBriefing && h.Game != null && h.Game.Tick > 0) { roster = 1f; clockA = 1f; } // the briefing opened again mid-run (B): the play HUD stays as it was
            float re = 1f - (1f - roster) * (1f - roster);
            foreach (var e in new[] { column, row }) { if (e == null) continue; e.style.opacity = re; e.style.translate = new Translate(-140f * (1f - re), 0); e.style.visibility = roster <= 0f ? Visibility.Hidden : StyleKeyword.Null; }
            if (pill != null) { pill.style.opacity = clockA; pill.style.visibility = clockA <= 0f ? Visibility.Hidden : StyleKeyword.Null; }
            // the settle (3.3 s): the four ground rings pulse once, out from each agent's feet
            if (h.Starting && t >= HandoverHost.BriefExitS + HandoverHost.BriefGlideS && h.Game != null && h.Views != null)
            {
                float u = Mathf.Clamp01((t - HandoverHost.BriefExitS - HandoverHost.BriefGlideS) / HandoverHost.BriefSettleS);
                for (int k = 0; k < 4; k++) if (h.Views.Find("op/" + Op.Name((OpId)k)) is Transform ot && ot.gameObject.activeInHierarchy) GroundRing(ot.position, 0.45f + 0.5f * u, new Color(LookLive.r, LookLive.g, LookLive.b, 1f - u), 2.6f, false, 0.6f * (1f - u));
            }
        }
    }
}
