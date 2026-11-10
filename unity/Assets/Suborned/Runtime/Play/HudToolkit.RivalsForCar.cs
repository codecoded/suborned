using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Glyph r123 §3 (rivals walking to a held car, engine 0.34.0 M067-K3 a: Game.RivalsForCar): over each rival walking for the car,
    /// the kit's `car` icon (cream) on `disc_dark_24` in a 2 px violet ring (#8a4fd0, their foot ring's, style contract signal_colours.rivals);
    /// the car's lock marker takes the same ring while any of them walks. No line: the walking figure is the direction. Off as soon as a rival
    /// stops going for it (back to cover, recalled, down). The overhead view only. View only: nothing reaches the engine.</summary>
    public sealed partial class HudToolkit
    {
        readonly List<VisualElement> forCarEls = new(); VisualElement heldRing;
        /// <summary>The proof reads it: the rivals wearing the car disc this frame.</summary>
        public static string ForCarShown { get; private set; } = "";

        VisualElement ForCarEl(int i)
        {
            while (forCarEls.Count <= i)
            {
                var ring = E(marks, null, 0, 0, 30, 30); Round(ring, 15f); Border(ring, 2f, RivalViolet); ring.pickingMode = PickingMode.Ignore;
                var d = E(ring, "e-body e-inline", 0, 0, 26, 26); Round(d, 13f); BorderW(d, 2f); d.pickingMode = PickingMode.Ignore; Icon(Middle(d, 13.5f, 13.5f), "car", Cream); /* centring.md: inside its 2 px ring */ /* brief 101 round 3: a round tab */
                manifest.Add((ring, "image", () => "rival going for the car")); forCarEls.Add(ring);
            }
            return forCarEls[i];
        }

        void RivalsForCarMarks(bool on)
        {
            int used = 0; var g = host.Game; var ids = on && g != null && !g.Over ? g.RivalsForCar : null; string shown = "";
            if (ids != null)
                foreach (var id in ids)
                {
                    var tr = host.Views?.Find(host.ViewKey(id)); if (!tr || !tr.gameObject.activeInHierarchy || !Proj(tr.position + Vector3.up * 2.95f, out var q)) continue;
                    var el = ForCarEl(used++); Show(el, true); el.style.left = q.x - 15f; el.style.top = q.y - 15f - 14f; // above the overhead pips
                    shown += (shown.Length > 0 ? "," : "") + id;
                }
            for (int i = used; i < forCarEls.Count; i++) Show(forCarEls[i], false);
            ForCarShown = shown;
            if (heldMark != null)
            {
                if (heldRing == null && used > 0) { heldRing = E(heldMark, null, -2f, -2f, 30, 30); Round(heldRing, 15f); Border(heldRing, 2f, RivalViolet); heldRing.pickingMode = PickingMode.Ignore; }
                if (heldRing != null) Show(heldRing, used > 0);
            }
        }
    }
}
