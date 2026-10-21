using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 029: the fight range draws the cover pieces of the layout the run uses (pieces-open, pieces-dense or pieces-lane, built by
    /// RangeScene; the lane's root carries its floor past 45 m and its far rings too).</summary>
    public sealed class RangePieces : MonoBehaviour
    {
        HandoverHost host; Transform open, dense, lane; string shown = "";
        void Start() { host = FindAnyObjectByType<HandoverHost>(); open = transform.Find("pieces-open"); dense = transform.Find("pieces-dense"); lane = transform.Find("pieces-lane"); }
        void Update()
        {
            if (!host || host.rangeLayout == shown) return; shown = host.rangeLayout;
            if (open) open.gameObject.SetActive(shown is not ("dense" or "lane")); if (dense) dense.gameObject.SetActive(shown == "dense"); if (lane) lane.gameObject.SetActive(shown == "lane");
        }
    }
}
