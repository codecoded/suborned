using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    public sealed partial class HudToolkit
    {
        VisualElement noticeRow;
        Label noticeText;
        Game noticeGame;
        int noticeDeadline;
        float noticeUntil;
        internal static string VisibleSealNotice => live?.noticeRow?.resolvedStyle.display == DisplayStyle.Flex ? live.noticeText.text : "";

        void SealNotice(HandoverHost h, bool counting)
        {
            if (noticeRow == null)
            {
                noticeRow = E(root);
                noticeRow.pickingMode = PickingMode.Ignore;
                noticeRow.style.position = Position.Absolute;
                noticeRow.style.top = Length.Percent(17);
                noticeRow.style.left = noticeRow.style.right = 0;
                noticeRow.style.alignItems = Align.Center;
                noticeText = KLabel(noticeRow, "k-bold", "k-page", 24f);
                noticeText.pickingMode = PickingMode.Ignore;
                noticeText.style.backgroundColor = new Color(0.16f, 0.09f, 0.06f, 0.95f);
                noticeText.style.paddingLeft = noticeText.style.paddingRight = 18;
                noticeText.style.paddingTop = noticeText.style.paddingBottom = 8;
                Round(noticeText, 6f);
            }
            if (noticeGame != h.Game) { noticeGame = h.Game; noticeDeadline = -1; noticeUntil = 0; }
            if (counting && noticeDeadline != h.Game.Lockdown)
            {
                noticeDeadline = h.Game.Lockdown;
                noticeUntil = Time.unscaledTime + 3f;
            }
            bool closed = h.Game?.FailCode == "sealed" && h.EndHeld && !h.FailCamOn;
            bool visible = !m.BriefingOpen && !m.Ended && (closed || counting && (Time.unscaledTime < noticeUntil || h.SealLeftS <= 10));
            Show(noticeRow, visible);
            if (visible) noticeText.text = closed ? "District sealed · Escape route closed" : h.Text.SealHeading(h.SealLeftS);
        }
    }
}
