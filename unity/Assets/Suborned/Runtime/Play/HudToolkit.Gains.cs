using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101 round 3, piece 5 (Glyph's done chip, briefs/101/glyph/r3/window.png and the board's "Uneasy › Listen, Pay"; Sky: "yes"): what an act gave, over the
    /// thing that changed, for its 4.2 s, with no plate: the doer's face in the built cream ring with the outcome's kit badge (✓ done, ½ not yet, ✗ refused), one keyed
    /// pictogram and word a fact as street labels (at most three, centred on the face), then "›" and the ways it opened as capsules, each a click (the thing's ring
    /// opens on that way for the selection: HandoverHost.WayClick in HostBook); a way a refusal shut is struck through. Placement, the stack over a window's marker and the pill's lift stay
    /// in HudToolkit.Done. It retires the panel_dark chip, its brass hairline and its plate_dark pills.</summary>
    public sealed partial class HudToolkit
    {
        sealed class ChipEl
        {
            public VisualElement Root, Face, Lines, Unl; public readonly List<(VisualElement row, KeyIcon icon, KeyText word)> Rows = new(); public readonly List<Capsule> Pills = new();
            public int Id = -1; public string Anchor = ""; public readonly List<string> Facts = new(), Verbs = new(); public readonly List<(string icon, string word)> Ways = new();
        }
        readonly List<ChipEl> chipEls = new();
        const float ChipFace = 30f, GainType = 15f, GainRow = 26f;

        ChipEl NewChip()
        {
            var c = new ChipEl();
            c.Root = E(marks, null, 0, 0); c.Root.style.flexDirection = FlexDirection.Row; c.Root.style.alignItems = Align.Center; c.Root.style.overflow = Overflow.Visible;
            c.Face = SFace(c.Root, ChipFace, 2.5f);
            c.Lines = E(c.Root); c.Lines.style.marginLeft = 10; c.Lines.style.flexShrink = 0;
            for (int i = 0; i < 3; i++) { var (row, ic, w) = KeyRow(c.Lines, 16f, GainType); row.style.height = GainRow; c.Rows.Add((row, ic, w)); }
            c.Unl = StackRow(c.Lines, GainRow); c.Unl.style.justifyContent = Justify.FlexStart;
            KText(c.Unl, GainType).Set("›");
            for (int i = 0; i < 3; i++) { int k = i; var cc = c; var p = NewCapsule(c.Unl, 24f, false, () => ChipWay(cc, k)); p.Root.style.marginLeft = i == 0 ? 6f : 6f; c.Pills.Add(p); }
            chipEls.Add(c); return c;
        }

        /// <summary>A new way on a chip clicked: the thing's ring opens on it (no order is given).</summary>
        void ChipWay(ChipEl c, int i) { if (c.Id >= 0 && i < c.Ways.Count) host.WayClick(c.Anchor, c.Ways[i].icon, c.Ways[i].word); }

        /// <summary>A chip's face, badge, facts and ways, once per landing.</summary>
        void ChipFill(ChipEl c, HandoverHost.DoneChip ch)
        {
            c.Id = ch.Id; c.Anchor = ch.AnchorKey; CameoOf(c.Face, ch.Agent); c.Facts.Clear(); c.Verbs.Clear(); c.Ways.Clear();
            foreach (var l in ch.Lines) if (c.Facts.Count < 3) c.Facts.Add(l.Word); foreach (var u in ch.Unlocks) if (c.Verbs.Count < 3) c.Verbs.Add(u.Word); // Glyph budget §3: the manifest spells the chip's rows
            FaceBadge(c.Face, ch.Tone == "ok" ? "badges/mark_ok" : ch.Tone == "soft" ? "badges/mark_half" : "badges/mark_no"); // the tone only picks the kit's badge
            for (int i = 0; i < 3; i++)
            {
                bool has = i < ch.Lines.Count; Show(c.Rows[i].row, has); if (!has) continue;
                c.Rows[i].icon.Set(this, ch.Lines[i].Icon); c.Rows[i].icon.Look("ki-ink", "ki-key"); c.Rows[i].word.Set(ch.Lines[i].Word);
            }
            Show(c.Unl, ch.Unlocks.Count > 0);
            for (int i = 0; i < 3; i++)
            {
                bool has = i < ch.Unlocks.Count; Show(c.Pills[i].Root, has); if (!has) continue; var u = ch.Unlocks[i];
                SetCapsule(c.Pills[i], u.Icon, u.Word, shut: u.Shut); c.Pills[i].Root.pickingMode = u.Shut ? PickingMode.Ignore : PickingMode.Position; c.Ways.Add((u.Icon, u.Word));
            }
        }
    }
}
