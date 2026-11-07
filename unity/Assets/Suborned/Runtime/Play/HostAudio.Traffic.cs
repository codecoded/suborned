using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 098 step 1 (law L8): the passing cars heard as well as seen. A quiet engine loop on the nearest moving passing cars, at most TrafficVoices of
    /// them: the rival car's loop (cues.json `rival-car-engine`: its clip, bus, 3D distances and speed bounds) at TrafficEngineGain of its volume, from the
    /// car's drawn place, its volume and pitch rising with the engine's speed for the car; faded in and out as a voice moves to a nearer car; paused with the
    /// clock; silent with the sound off. A start offset from the car's id, never a random one (a shot replays). For Chime to tune or replace with a pack cue.
    /// View only: nothing reaches the engine or its hash.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The most passing cars heard at once, and their loop's volume against the rival car's.</summary>
        public static int TrafficVoices = 4; public static float TrafficEngineGain = 0.35f;
        sealed class TrafficVoice { public AudioSource Src; public string Id = ""; public float Gain; }
        readonly List<TrafficVoice> trafficVoices = new(); readonly List<(float d, TrafficView v)> trafficNear = new();
        /// <summary>For the proofs: the passing-car voices playing now.</summary>
        public int TrafficVoicesPlaying { get { int n = 0; foreach (var v in trafficVoices) if (v.Src && v.Src.isPlaying && v.Gain > 0.01f) n++; return n; } }

        /// <summary>Per frame, after the listener moved (RivalAudioFrame): the voices on the nearest moving passing cars.</summary>
        void TrafficAudioFrame()
        {
            var tn = views?.TrafficNow; float dt = Mathf.Max(1e-4f, Time.unscaledDeltaTime);
            bool on = engineLoop != null && !SoundOff && tn != null && tn.Count > 0 && !Game.Over;
            trafficNear.Clear();
            if (on)
            {
                float reach = engineLoop.MaxD * 0.6f, r2 = reach * reach;
                foreach (var v in tn) { if (v.Speed < 50 || !v.DriverIn || v.State != "moving" || !views.TrafficDrawnAt(v.Id, out var at)) continue; float d = (at - focus).sqrMagnitude; if (d <= r2) trafficNear.Add((d, v)); }
                trafficNear.Sort((a, b) => a.d.CompareTo(b.d)); if (trafficNear.Count > TrafficVoices) trafficNear.RemoveRange(TrafficVoices, trafficNear.Count - TrafficVoices);
            }
            if (on && trafficVoices.Count < TrafficVoices && trafficNear.Count > trafficVoices.Count) TrafficVoiceMake();
            // a voice keeps its car while it is among the nearest; a free voice takes the nearest car nobody has
            foreach (var tv in trafficVoices) if (tv.Id.Length > 0 && !TrafficNear(tv.Id) && tv.Gain <= 0.001f) tv.Id = "";
            foreach (var (_, v) in trafficNear) if (!TrafficVoiced(v.Id) && TrafficVoiceOf("") is TrafficVoice free) { free.Id = v.Id; free.Gain = 0f; }
            foreach (var tv in trafficVoices)
            {
                if (!tv.Src) continue;
                var v = tv.Id.Length > 0 ? views?.TrafficOf(tv.Id) : null; bool near = on && v != null && TrafficNear(tv.Id);
                float want = near && !Paused ? 1f : 0f; tv.Gain = Mathf.MoveTowards(tv.Gain, want, dt / (want > tv.Gain ? engineLoop?.InS ?? 0.25f : engineLoop?.OutS ?? 0.6f));
                if (v != null && views.TrafficDrawnAt(v.Id, out var at)) tv.Src.transform.position = at + Vector3.up * 0.6f;
                if (tv.Gain <= 0.001f) { if (tv.Src.isPlaying) tv.Src.Stop(); if (!near) tv.Id = ""; continue; }
                if (Paused) { if (tv.Src.isPlaying) tv.Src.Pause(); continue; }
                if (!tv.Src.isPlaying) { tv.Src.time = (Streams.Fnv1a32("traffic-engine|" + tv.Id) % 1000u) / 1000f * tv.Src.clip.length; tv.Src.Play(); }
                float k = Mathf.InverseLerp(engineLoop.Lo, engineLoop.Hi, (v?.Speed ?? 0) / 100f);
                tv.Src.volume = engineLoop.Volume * TrafficEngineGain * Bus(engineLoop.Bus) * Mathf.Lerp(engineLoop.VolLo, engineLoop.VolHi, k) * tv.Gain;
                tv.Src.pitch = Mathf.Lerp(engineLoop.PitchLo, engineLoop.PitchHi, k) * (v?.Model == "scooter" ? 1.25f : v?.Model == "van" ? 0.85f : 1f); // the scooter's hum higher, the van's lower
            }
        }

        bool TrafficNear(string id) { foreach (var x in trafficNear) if (x.v.Id == id) return true; return false; }
        bool TrafficVoiced(string id) => TrafficVoiceOf(id) != null;
        TrafficVoice TrafficVoiceOf(string id) { foreach (var x in trafficVoices) if (x.Id == id) return x; return null; }

        void TrafficVoiceMake()
        {
            var clip = Resources.Load<AudioClip>(engineLoop.Clip); if (!clip) return;
            var go = new GameObject("traffic-engine/" + trafficVoices.Count); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>(); s.clip = clip; s.loop = true; s.playOnAwake = false; s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.minDistance = engineLoop.MinD; s.maxDistance = engineLoop.MaxD; s.dopplerLevel = engineLoop.Doppler * 0.5f; s.volume = 0f;
            trafficVoices.Add(new TrafficVoice { Src = s });
        }
    }
}
