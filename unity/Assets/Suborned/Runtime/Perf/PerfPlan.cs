using System;
using UnityEngine;

namespace Suborned.Perf
{
    /// <summary>What a perf run measures (brief 008 B6): the play camera's rig and the stops it is driven through, resolved in the
    /// Editor from the built scene (PerfBuild: stand-offs by Rig.StandOff, the fingerprint and export sha256 of the scene the player
    /// was built from) and read by the player's PerfHarness from the `-perf` argument. JsonUtility shape.</summary>
    [Serializable]
    public sealed class PerfPlan
    {
        public string fingerprint, exportSha256, source, scene, content;
        /// <summary>The play camera as built: pitch and yaw (deg), view aspect, far plane beyond the stand-off.</summary>
        public float pitch, yaw, aspect, farMargin;
        public int width, height;
        /// <summary>Warm-up loops the stops unrecorded for warmupSeconds, dwellSeconds at each; then each stop settles and records.</summary>
        public float warmupSeconds, dwellSeconds, settleSeconds, recordSeconds;
        public PerfStop[] stops;
    }

    [Serializable]
    public sealed class PerfStop
    {
        public string name, why;
        /// <summary>World point the play camera frames (its ArchitectureFade focus; y = the level: 0 street, 9.43 deck).</summary>
        public Vector3 focus;
        /// <summary>Orthographic size, and the distance back along the view that clears the frame column (Rig.StandOff).</summary>
        public float zoom, standOff;
    }
}
