using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 108 (Glyph's pages; Sky uses the dial, so it stays): the dial's dev panel folded to a small chip at the bottom left of the street, "Dial DEV" and a chevron;
    /// a click opens today's panel, its − folds it back. A person playing keeps it open or shut across runs (PlayerPrefs); a proof, a batch row or a probe starts folded
    /// and opens it with DialUnfold.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement dlChip;
        const string DialOpenPref = "suborned.dial.open";
        static bool? dialOpen;
        static bool DialPrefsOn => !(Application.isBatchMode || GameBoot.Automation && !Application.isEditor);
        static bool DialOpen => dialOpen ??= DialPrefsOn && PlayerPrefs.GetInt(DialOpenPref, 0) == 1;

        /// <summary>The panel opened (true) or folded to its chip.</summary>
        public static void DialUnfold(bool open)
        {
            dialOpen = open; if (DialPrefsOn) { PlayerPrefs.SetInt(DialOpenPref, open ? 1 : 0); PlayerPrefs.Save(); }
            if (live != null) live.bkFor = null;
        }

        void DialChipBuild()
        {
            dlChip?.RemoveFromHierarchy();
            float w = 18f + BkTw("Dial", 17f, "Jost-SemiBold") + 10f + 46f + 8f + 12f + 14f;
            dlChip = E(bkPage, null, DlX, BookLayout.GridH - 24f - 40f, w, 40f, true); dlChip.name = "dial-chip";
            Round(dlChip, 20f); dlChip.style.backgroundColor = new Color(BkBody.r, BkBody.g, BkBody.b, 0.9f); Border(dlChip, 1.5f, DlRim);
            BkPut(dlChip, 16f, 19f, "Dial", 17f, "Jost-SemiBold", BkCreamHex);
            float tx = 16f + BkTw("Dial", 17f, "Jost-SemiBold") + 10f; var tag = E(dlChip, null, tx, 9f, 46f, 20f); Round(tag, 4f); Border(tag, 1f, Hex(0xb0cdc7));
            float dw = BkTw("DEV", 11f, "Jost-Bold") + 2f * 11f * .2f; PgText(tag, (44f - dw) / 2f + 11f * .1f, 1f, "Jost-Bold", 11f, BkDimHex, "DEV", 0.2f, true); // centred by its ink (tracking adds a space after each letter)
            Icon(E(dlChip, null, tx + 46f + 8f, 13f, 12f, 12f), "chev_up", Hex(0xb0cdc7));
            dlChip.RegisterCallback<PointerEnterEvent>(_ => Border(dlChip, 1.5f, TurqLight)); dlChip.RegisterCallback<PointerLeaveEvent>(_ => Border(dlChip, 1.5f, DlRim));
            dlChip.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); DialUnfold(true); });
        }
    }
}
