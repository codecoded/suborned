using UnityEngine;

namespace Suborned.Play
{
    /// <summary>A kit part turning about the vertical axis through its piece (Splice: AtomFountain_Testbed's three orbits, kit.json `parts`).
    /// View only: nothing in the sim reads it. It turns in its parent's own space: the part's parent is the piece (its element or prefab root,
    /// standing on the axis), so the turn is about the piece's own centre and up wherever the piece is placed, moved or turned. A world point
    /// baked by the builder sent the orbits round a spot 206 m off once the zone moved into the old town (Sky 2026-11-07: "what's happened to
    /// our fountain?"; "shouldn't they be calculated around the center of the object itself, not the world?"). A part with no parent turns
    /// about its own pivot.</summary>
    public sealed class Spin : MonoBehaviour
    {
        public float degreesPerSecond = 12f;
        void Update()
        {
            float deg = degreesPerSecond * Time.deltaTime;
            if (!transform.parent) { transform.Rotate(Vector3.up, deg, Space.World); return; }
            var q = Quaternion.AngleAxis(deg, Vector3.up); // the piece's up, in the piece's own space
            transform.localPosition = q * transform.localPosition; transform.localRotation = q * transform.localRotation;
        }
    }
}
