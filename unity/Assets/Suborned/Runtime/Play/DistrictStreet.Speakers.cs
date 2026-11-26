using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    public sealed partial class DistrictStreet
    {
        int speaker = -1;
        string civicSpeaker;
        int lastPickFrame = -1, lastPick = -1;
        Vector2 lastPointer;

        internal int PickResident(Vector2 screen)
        {
            if (current == null || current != host.Game || current.District == null) return -1;
            if (lastPickFrame == Time.frameCount && screen == lastPointer) return lastPick;
            lastPickFrame = Time.frameCount;
            lastPointer = screen;
            lastPick = -1;
            float best = Mathf.Pow(15f * Mathf.Max(1f, Screen.height / 1080f), 2);
            foreach (int id in visible)
            {
                if (id >= Tram || current.District.Network.Fallen[id] || !poses.TryGetValue(id, out var pose)) continue;
                var projected = host.View.WorldToScreenPoint(Vector3.LerpUnclamped(pose.from, pose.to, host.StreetFrameAlpha) + Vector3.up);
                if (projected.z <= 0 || projected.z > (host.View.orthographic ? host.View.farClipPlane : FarMetres)) continue;
                float distance = ((Vector2)projected - screen).sqrMagnitude;
                if (distance < best) { best = distance; lastPick = id; }
            }
            return lastPick;
        }

        // Only the speaker needs a full figure for the close shot and its live face.
        internal void PrepareSpeaker(int who)
        {
            var g = host.Game;
            if (current != g || g.DistrictPersonAt(who) is not Pt at) return;
            speaker = who;
            var p = g.District.Map.People[who];
            int civic = g.HasCivicActor(p.Id) ? g.Walkers.FindIndex(w => w.P.Id == p.Id) : -1;
            civicSpeaker = civic >= 0 ? "crowd/" + civic : null;
            if (civicSpeaker != null) return;
            if (!activeViews.TryGetValue(who, out var view))
            {
                view = Rent(who);
                if (view == null) return;
                var facing = host.frame.Dir(g.District.Pose(who, g.Tick).facing);
                view.root.transform.SetPositionAndRotation(host.frame.World(at), facing.sqrMagnitude > .01f ? Quaternion.LookRotation(facing) : Quaternion.identity);
                activeViews.Add(who, view);
            }
            visible.Add(who);
            rigged.Add(who);
            rescan = 0;
        }

        bool IsSpeaker(string id) => speaker >= 0 && current?.District?.Map.People[speaker].Id == id;
        internal string SpeakerKey(string id) => IsSpeaker(id) ? civicSpeaker ?? id : id;
        internal Transform SpeakerView(string id) =>
            IsSpeaker(id) && activeViews.TryGetValue(speaker, out var v) ? v.root.transform : null;

        internal bool SpeakerFace(string id, out Vector3 at, out Vector3 facing)
        {
            at = facing = default;
            if (!IsSpeaker(id) || !activeViews.TryGetValue(speaker, out var view)) return false;
            var body = view.root.transform;
            at = view.head ? view.head.position + Vector3.up * .02f : body.position + Vector3.up * 1.58f;
            facing = body.forward;
            return true;
        }
    }
}
