namespace Suborned.Play
{
    /// <summary>Brief 109 round 4 (Sky on the tilt-shift: "cool but yeah, need to play with it. Think it might be too much for this game"): the miniature look
    /// in the pause menu's Display settings, off by default: a switch and a strength slider (Juice109Tilt reads them live; `-tiltshift on` still forces it
    /// on for a capture). The slider greys out while the switch is off.</summary>
    public sealed partial class FrontEnd
    {
        void MiniatureRows(Settings.DisplayS d)
        {
            Switch("Miniature look", () => d.miniature, v => { d.miniature = v; Save(); });
            Slider("Miniature strength", 0, 100, 5, () => d.miniatureStrength, v => { d.miniatureStrength = UnityEngine.Mathf.RoundToInt(v); Save(); }, Pct, visible: () => d.miniature);
        }
    }
}
