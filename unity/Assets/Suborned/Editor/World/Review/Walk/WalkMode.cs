using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>One way of moving the review camera through the built district. The rig owns the modes, hands each frame's input
    /// to the current one and enables only its camera.</summary>
    public interface IWalkMode
    {
        string Name { get; }
        /// <summary>One line for the readout: what this camera is.</summary>
        string Blurb { get; }
        Camera Camera { get; }
        /// <summary>Where the mode is on the plan: the walker's feet, the point under the flying eye, the tactical focus.</summary>
        Vector3 Ground { get; }
        /// <summary>Mode state for the readout (level, speed, zoom, shadow fit).</summary>
        string Status { get; }
        /// <summary>Main-light shadow distance (m) while this mode's camera renders; 0 = the camera fits its own (TacticalShadows).</summary>
        float ShadowDistance { get; }

        /// <summary>Takes over from another mode at the same place.</summary>
        void Enter(IWalkMode from);
        void Tick(in WalkFrame f, float dt);
        /// <summary>Jumps to a plan place, framed this mode's way.</summary>
        void Go(Place place);
        /// <summary>Adopts a saved view as it was built.</summary>
        void Take(Pose view);
    }

    /// <summary>Camera angles as views.json writes them.</summary>
    public static class ViewAngles
    {
        /// <summary>Compass degrees, 0 = north, 90 = looking east.</summary>
        public static float Yaw(Quaternion q) => q.eulerAngles.y;

        /// <summary>Degrees below horizontal (negative = looking up).</summary>
        public static float Pitch(Quaternion q) { float x = q.eulerAngles.x; return x > 180f ? x - 360f : x; }
    }
}
