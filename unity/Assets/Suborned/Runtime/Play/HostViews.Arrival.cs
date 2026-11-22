using UnityEngine;

namespace Suborned.Play
{
    public sealed partial class HostViews
    {
        // The mission clock starts after the last operative reaches their authored start.
        // The parked car and these four transforms return to the simulation on the next frame.
        const float ParkAt = 1.7f;

        internal void ArrivalPose(string key, float time, ref Vector3 position, ref bool visible)
        {
            if (key != GetawayKey && !key.StartsWith("op/", System.StringComparison.Ordinal)) return;
            var car = things[GetawayKey].to;
            var forward = GetawayFacing() ?? Vector3.forward;
            if (key == GetawayKey)
            {
                float braking = 1f - Mathf.Clamp01(time / ParkAt);
                position = car - forward * (13f * braking * braking);
                return;
            }

            int seat = System.Array.IndexOf(OpKeys, key);
            if (seat < 0) return;
            float exitAt = ParkAt + 0.14f * seat;
            visible &= time >= exitAt;
            if (!visible) return;
            var right = Vector3.Cross(Vector3.up, forward);
            var side = (seat % 2 == 0 ? 1f : -1f) * right;
            var end = (seat < 2 ? 1f : -1f) * forward;
            var door = car + side * 1.05f + end * 0.85f;
            var corner = car + side * 1.55f + end * 2.65f;
            var next = Vector3.Dot(position - car, side) < 0f ? car - side * 1.55f + end * 2.65f : corner;
            float first = Vector3.Distance(door, corner), second = Vector3.Distance(corner, next);
            float dist = Mathf.Clamp01((time - exitAt) / (HandoverHost.BriefPlayAt - 0.3f - exitAt)) * (first + second + Vector3.Distance(next, position));
            position = dist < first ? Vector3.MoveTowards(door, corner, dist)
                : dist < first + second ? Vector3.MoveTowards(corner, next, dist - first)
                : Vector3.MoveTowards(next, position, dist - first - second);
        }
    }
}
