using UnityEngine;
using Game = Suborned.Sim.Game;
using TrafficView = Suborned.Sim.TrafficView;

namespace Suborned.Play
{
    /// <summary>Brief 098 step 1b (Sky 2026-11-07: "shot cars can get past, others block"; Keel's jams.html, "What Latch must change"): a wreck pushed aside is drawn
    /// sliding, never jumping. A mission car driving its leg pushes a wreck traffic.shove.cmS a tick (15 cm at 10 Hz), and the views' own ease between the ticks
    /// draws that as a slide already. A Go is different: the engine pushes every wreck on the way out off it on the Go's tick, as far as it must (up to
    /// traffic.shove.maxCm, 6 m), and for our car that tick ends the mission, so no tick follows to ease it. So a wreck whose place moved further than a few ticks'
    /// push since the views last looked, under a push, slides there on the wall clock, as the leaving car drives on the wall clock (HostViews.Leaving,
    /// HostViews.LeavingLent):
    ///  · it waits where it stood until the pushing car's drawn body comes up to it (centres within the two half-lengths and ContactM), so the car is seen to
    ///    shove it; then it slides to its pushed place at the push's speed (traffic.shove.cmS), or faster while the car comes on faster (SlideOfCar of the car's
    ///    drawn speed), eased in and out; the leaving car brakes to PushCapMS while it pushes (PushCap: HostViews.Leaving, HostViews.LeavingLent), then drives on;
    ///  · if the pushing car is not drawn, passes it by, or never comes near it within SlideWaitS of the car's own clock, it slides at the push's speed on its own;
    ///  · paused (a mission still running), it holds.
    /// View only: the engine's place is the pushed one from the Go's tick (its footprint, its cover); nothing here goes back to it.</summary>
    public sealed partial class HostViews
    {
        const float SlideJumpM = 0.5f, ContactM = 1.5f, SlideWaitS = 8f, SlideOfCar = 1.1f, PushCapMS = 5f, PushBrake = 12f;
        int slideFrameWas = -1;
        /// <summary>For the proofs and the clip: the slides drawn this game, the longest (m), and the most a sliding wreck moved in one frame (m).</summary>
        public int WreckSlides { get; private set; }
        public float WreckSlideMaxM { get; private set; }
        public float WreckSlideMaxStepM { get; private set; }
        /// <summary>A wreck sliding now (its view key), for the proofs.</summary>
        public bool WreckSliding(string id) => trafficCars.TryGetValue(id, out var cv) && cv.Sliding;

        /// <summary>The push's speed (m/s): traffic.shove.cmS of the bundle's rules, else 1.5.</summary>
        float ShoveMS => host.Bundle?.Inputs?.Population?.Traffic is { } t ? t.ShoveCmS / 100f : 1.5f;

        /// <summary>Per tick, for each passing car (TrafficViews): true when the slide owns its drawn place this tick (a jump under a push begins one, a slide
        /// under way takes the newest place as its end); false: the tick's ease as for any view.</summary>
        bool SlideTick(TrafficCarView cv, TrafficView v, Game g, bool fresh)
        {
            var to = frame.World(v.At);
            if (cv.Sliding) { if ((to - cv.SlideTo).sqrMagnitude > 1e-6f) { var at = Vector3.Lerp(cv.SlideFrom, cv.SlideTo, Ease(cv.SlideP)); cv.SlideFrom = at; cv.SlideTo = to; cv.SlideP = 0f; } return true; }
            bool pushed = v.Shoved != cv.ShovedSeen; cv.ShovedSeen = v.Shoved; // a push since the views last looked (the Go's lands on a tick they have seen already: the order comes between ticks)
            if (fresh || !pushed || !things.TryGetValue(cv.Key, out var x) || !x.shown) return false;
            var d = to - x.to; d.y = 0f; if (d.magnitude <= SlideJumpM) return false; // a tick's push (15 cm): the ease draws it
            cv.Sliding = true; cv.Touched = false; cv.SlideFrom = x.t.position; cv.SlideTo = to; cv.SlideP = 0f; cv.SlideAt = 0f; cv.GapMin = float.MaxValue; cv.HalfLen = v.Length / 200f;
            cv.SlideBy = v.ShovedBy.StartsWith("car:") ? "car/" + v.ShovedBy.Substring(4) : ""; cv.PusherWas = things.TryGetValue(cv.SlideBy, out var px) ? px.to : cv.SlideFrom;
            WreckSlides++; WreckSlideMaxM = Mathf.Max(WreckSlideMaxM, d.magnitude);
            things[cv.Key] = (x.t, cv.SlideFrom, cv.SlideFrom, true);
            return true;
        }

        /// <summary>Per frame (TrafficFrame, after the leaving cars moved and before the views take their places): each wreck sliding, a step along its slide.</summary>
        void SlideFrame()
        {
            var g = host.Game; if (g == null || Time.frameCount == slideFrameWas) return; // once a frame (an order given draws a frame of its own too)
            slideFrameWas = Time.frameCount;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f), carHalf = g.R.CarLenCm / 200f;
            bool hold = host.Paused && !g.Over;
            foreach (var cv in trafficCars.Values)
            {
                if (!cv.Sliding || !things.TryGetValue(cv.Key, out var x)) continue;
                var span = cv.SlideTo - cv.SlideFrom; span.y = 0f; float len = span.magnitude;
                var was = Vector3.Lerp(cv.SlideFrom, cv.SlideTo, Ease(cv.SlideP));
                if (len < 1e-3f) { cv.Sliding = false; things[cv.Key] = (x.t, cv.SlideTo, cv.SlideTo, true); continue; }
                float speed = 0f;
                if (!hold)
                {
                    if (cv.SlideBy.Length > 0 && things.TryGetValue(cv.SlideBy, out var px) && px.shown && px.t.gameObject.activeInHierarchy)
                    {
                        var pp = px.to; var mv = pp - cv.PusherWas; mv.y = 0f; float pv = dt > 1e-4f ? mv.magnitude / dt : 0f; cv.PusherWas = pp;
                        var gap = pp - was; gap.y = 0f; float gm = gap.magnitude; cv.SlideAt += dt; // the wait on the cars' own clock (a slow frame moves them 0.1 s at most)
                        if (!cv.Touched && (gm <= cv.HalfLen + carHalf + ContactM || gm > cv.GapMin + 1f && cv.GapMin < cv.HalfLen + carHalf + 8f)) cv.Touched = true; // its body has come up to it, or it came near and is going past and away
                        cv.GapMin = Mathf.Min(cv.GapMin, gm);
                        speed = cv.Touched ? Mathf.Max(ShoveMS, pv * SlideOfCar) : cv.SlideAt > SlideWaitS ? ShoveMS : 0f;
                    }
                    else speed = ShoveMS; // no pushing car drawn: it slides on its own
                }
                cv.SlideP = Mathf.Min(1f, cv.SlideP + speed * dt / len);
                var at = Vector3.Lerp(cv.SlideFrom, cv.SlideTo, Ease(cv.SlideP));
                var step = at - was; step.y = 0f; WreckSlideMaxStepM = Mathf.Max(WreckSlideMaxStepM, step.magnitude);
                things[cv.Key] = (x.t, at, at, true);
                if (cv.SlideP >= 1f) cv.Sliding = false;
            }
        }

        /// <summary>A leaving car's speed (m/s) this frame, `v` as it would drive: braked toward PushCapMS while it is shoving a wreck aside (a wreck it has come up
        /// to and not yet cleared), else as it is.</summary>
        float PushCap(string pusherKey, float v, float dt)
        {
            foreach (var cv in trafficCars.Values) if (cv.Sliding && cv.Touched && cv.SlideBy == pusherKey) return v > PushCapMS ? Mathf.Max(PushCapMS, v - PushBrake * dt) : v;
            return v;
        }

        /// <summary>Eased in and out (smoothstep): a shove starts and settles, never a constant glide.</summary>
        static float Ease(float p) { p = Mathf.Clamp01(p); return p * p * (3f - 2f * p); }
    }
}
