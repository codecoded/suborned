using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 029: the fight range draws the cover pieces of the layout the run uses (pieces-open, pieces-dense or pieces-lane, built by
    /// RangeScene; the lane's root carries its floor past 45 m and its far rings too).</summary>
    public sealed class RangePieces : MonoBehaviour
    {
        HandoverHost host; Transform open, dense, lane; string shown = "";
        void Start() { host = FindAnyObjectByType<HandoverHost>(); open = transform.Find("pieces-open"); dense = transform.Find("pieces-dense"); lane = transform.Find("pieces-lane"); }
        int floorDone = -1; Renderer[] floors; Color? greyBack;
        /// <summary>Brief 084: the floor's colour, grey or the street's paving, so flashes and white bodies are judged on what they meet in play. A material colour at run time (the grid's texture tinted); no scene edit. The tint is FightCues' floor values.</summary>
        void Floor()
        {
            if (floorDone == FightCues.FloorVersion) return; floorDone = FightCues.FloorVersion;
            if (floors == null) { var a = transform.Find("floor"); var b = lane ? lane.Find("floor-lane") : null; floors = new[] { a ? a.GetComponent<Renderer>() : null, b ? b.GetComponent<Renderer>() : null }; }
            var v = FightCues.V; var c = v.paving && FightCues.LabRun ? new Color(v.floorR, v.floorG, v.floorB) : Color.white;
            foreach (var r in floors) if (r) r.material.SetColor("_BaseColor", c);
            if (host && host.View) { greyBack ??= host.View.backgroundColor; host.View.backgroundColor = v.paving && FightCues.LabRun ? new Color32(241, 228, 199, 255) : greyBack.Value; } /* the void past the floor */
        }
        void Update()
        {
            Floor();
            if (!host || host.rangeLayout == shown) return; shown = host.rangeLayout;
            if (open) open.gameObject.SetActive(shown is not ("dense" or "lane")); if (dense) dense.gameObject.SetActive(shown == "dense"); if (lane) lane.gameObject.SetActive(shown == "lane");
        }
    }
}
