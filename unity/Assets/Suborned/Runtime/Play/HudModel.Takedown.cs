using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 013 E (Glyph's takedown spec): what the HUD shows of takedowns, as plain data the views draw: the armed takedown's way,
    /// the takedown card's "Before you act" rows, and the street's marks (bodies, stashes, held doors, a report in transit, the response's
    /// units, their arrival chips and the response pill), all from the host's per-frame lists.</summary>
    public sealed partial class HudModel
    {
        /// <summary>Takedown armed on the plate: the knob, and the way (knockout or kill).</summary>
        public bool TakedownArmed; public int TakedownKnob = -1; public string Way = "knockout";
        public readonly List<(string icon, string text)> PanelBefore = new();
        public IReadOnlyList<HandoverHost.BodyMark> Bodies = System.Array.Empty<HandoverHost.BodyMark>();
        public IReadOnlyList<HandoverHost.StashMark> Stashes = System.Array.Empty<HandoverHost.StashMark>();
        public IReadOnlyList<HandoverHost.DoorMark> Doors = System.Array.Empty<HandoverHost.DoorMark>();
        public IReadOnlyList<HandoverHost.ReportMark> Reports = System.Array.Empty<HandoverHost.ReportMark>();
        public IReadOnlyList<HandoverHost.UnitBadge> Units = System.Array.Empty<HandoverHost.UnitBadge>();
        public IReadOnlyList<HandoverHost.EdgeChip> Arrivals = System.Array.Empty<HandoverHost.EdgeChip>();
        public IReadOnlyList<Vector3> MastHeads = System.Array.Empty<Vector3>();
        public string Response = ""; public bool ResponseOpen; public IReadOnlyList<(string icon, string label, string text)> ResponseRows = System.Array.Empty<(string, string, string)>();

        void Street(HandoverHost h)
        {
            var plate = h.PlateOf(h.Agent);
            TakedownKnob = System.Array.FindIndex(plate, x => x.Id == "takedown" && !x.Dim);
            TakedownArmed = TakedownKnob >= 0 && h.Picked == TakedownKnob && h.Ending.Length == 0; Way = h.Way;
            Bodies = h.BodyMarks; Stashes = h.StashMarks; Doors = h.DoorMarks; Reports = h.ReportMarks; Units = h.UnitBadges; Arrivals = h.Arrivals; MastHeads = h.MastHeads;
            Response = h.ResponseLine; ResponseOpen = h.ResponseOpen; ResponseRows = h.ResponseRows;
        }
    }
}
