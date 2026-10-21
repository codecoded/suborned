using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 029: the fight range draws the cover pieces of the layout the run uses (pieces-open or pieces-dense, built by RangeScene).</summary>
    public sealed class RangePieces : MonoBehaviour
    {
        HandoverHost host; Transform open, dense; string shown = "";
        void Start() { host = FindAnyObjectByType<HandoverHost>(); open = transform.Find("pieces-open"); dense = transform.Find("pieces-dense"); }
        void Update()
        {
            if (!host || host.rangeLayout == shown) return; shown = host.rangeLayout;
            if (open) open.gameObject.SetActive(shown != "dense"); if (dense) dense.gameObject.SetActive(shown == "dense");
        }
    }
}
