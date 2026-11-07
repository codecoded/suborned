using UnityEngine;

namespace Suborned.Play
{
    /// <summary>A kit part turning about the vertical axis through its piece (Splice: AtomFountain_Testbed's three orbits, kit.json `parts`).
    /// View only: nothing in the sim reads it. Unscaled time, so a pause freezes nothing the player reads.
    /// The axis is the parent's (the piece's element or prefab root, which stands on the axis), read every frame in world space: `axisPoint`
    /// was baked in the builder's frame, and once the zone moved into the old town (its root 206 m along z) the orbits turned round a point
    /// 206 m off and swung out across the city, leaving the fountain bare (Sky 2026-11-07: "what's happened to our fountain?"). axisPoint is
    /// kept for a part with no parent.</summary>
    public sealed class Spin : MonoBehaviour
    {
        public Vector3 axisPoint; public float degreesPerSecond = 12f;
        void Update()
        {
            var p = transform.parent;
            transform.RotateAround(p ? p.position : axisPoint, p ? p.up : Vector3.up, degreesPerSecond * Time.deltaTime);
        }
    }
}
