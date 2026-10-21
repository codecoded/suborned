using UnityEngine;

namespace Suborned.Play
{
    /// <summary>A kit part turning about the vertical axis through its piece (Splice: AtomFountain_Testbed's three orbits, kit.json `parts`).
    /// View only: nothing in the sim reads it. Unscaled time, so a pause freezes nothing the player reads.</summary>
    public sealed class Spin : MonoBehaviour
    {
        public Vector3 axisPoint; public float degreesPerSecond = 12f;
        void Update() => transform.RotateAround(axisPoint, Vector3.up, degreesPerSecond * Time.deltaTime);
    }
}
