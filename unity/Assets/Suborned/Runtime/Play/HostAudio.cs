using System;
using System.Collections.Generic;
using System.Globalization;
using Path = System.IO.Path; using File = System.IO.File;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 059 (Chime, 2026-10-26; contracts/audio/cues.json `suborned.audio-cues/1`): the game's first sounds, keyed by the engine's log.
    ///  · the cue `rivals-coming`: on the log code rivals.coming (2:35, and the drawn and late entries that use the code), the clip once as a 2D
    ///    one-shot at its volume times its bus gain, panned toward the side of the screen the car enters from, one voice, its cooldown;
    ///  · the loop `rival-car-engine`: on the rival car's view, 3D (logarithmic, min and max distance, doppler), playing while the car's drawn pose
    ///    moves faster than 0.05 m/s, volume and pitch rising with its speed between the cue file's bounds, faded in and out, paused with the clock.
    /// The listener sits on the camera's ground focus (the camera is far above the street). The numbers are read from cues.json: the build's
    /// StreamingAssets/sim/cues.json when the sim step ships it, else the repo's contracts/audio/cues.json (found as the fight lab finds the
    /// repo), else the file's values as of 2026-10-26 (logged as defaults). View only: nothing reaches the engine or its hash.</summary>
    public sealed partial class HandoverHost
    {
        sealed class CueSpec { public string Id = "", Log = "", Clip = "", Bus = ""; public float Volume = 1f, Pitch = 1f, CooldownS; }
        sealed class LoopSpec { public string Id = "", Clip = "", Bus = ""; public float Volume = 1f, MinD = 10f, MaxD = 120f, Doppler = 0.5f, Lo = 0.5f, Hi = 5f, VolLo = 0.5f, VolHi = 1f, PitchLo = 0.82f, PitchHi = 1f, InS = 0.25f, OutS = 0.6f; }
        readonly List<CueSpec> cues = new(); LoopSpec engineLoop; readonly Dictionary<string, float> busDb = new();
        readonly Dictionary<string, float> cueLast = new(); AudioSource cueSource, loopSource; Transform listener; bool audioReady; Vector3 loopLast; float loopGain;
        /// <summary>Where the cue data came from ("streaming", "repo", "defaults"), and the cues played this run (the proof reads them).</summary>
        public string AudioSourceOf { get; private set; } = "";
        public readonly List<string> CuesPlayed = new();
        public bool EngineLoopPlaying => loopSource && loopSource.isPlaying && loopGain > 0.01f;

        void AudioInit()
        {
            if (audioReady) return; audioReady = true;
            string text = null;
            try { var sp = Path.Combine(Application.streamingAssetsPath, "sim", "cues.json"); if (File.Exists(sp)) { text = File.ReadAllText(sp); AudioSourceOf = "streaming"; } } catch (Exception) { }
            if (text == null) try { var rp = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), "contracts", "audio", "cues.json"); if (File.Exists(rp)) { text = File.ReadAllText(rp); AudioSourceOf = "repo"; } } catch (Exception) { }
            if (text != null) try { ParseCues(text); } catch (Exception e) { Debug.LogWarning("cues.json: " + e.Message); cues.Clear(); engineLoop = null; text = null; }
            if (text == null)
            {   // cues.json as of 2026-10-26 (Chime): used only when no copy of the file is found
                AudioSourceOf = "defaults"; busDb["master"] = 0f; busDb["cues"] = 0f; busDb["vehicles"] = -3f;
                cues.Add(new CueSpec { Id = "rivals-coming", Log = "rivals.coming", Clip = "Audio/rivals_coming", Bus = "cues", Volume = 0.85f, CooldownS = 5f });
                engineLoop = new LoopSpec { Id = "rival-car-engine", Clip = "Audio/rival_car_engine", Bus = "vehicles", Volume = 0.55f };
            }
            Debug.Log($"audio cues: {AudioSourceOf}, {cues.Count} cue(s), loop {(engineLoop != null ? engineLoop.Id : "none")}");
            // the listener on the camera's ground focus (Chime: the camera is too high for a 3D loop to be heard at play zoom)
            foreach (var l in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude)) l.enabled = false;
            listener = new GameObject("listener").transform; listener.SetParent(transform, false); listener.gameObject.AddComponent<AudioListener>();
            cueSource = new GameObject("cues").AddComponent<AudioSource>(); cueSource.transform.SetParent(listener, false); cueSource.playOnAwake = false; cueSource.spatialBlend = 0f;
        }

        static float F(JNode n) => float.Parse(n.NumberText, CultureInfo.InvariantCulture);

        void ParseCues(string text)
        {
            var j = JNode.Parse(text, "cues.json"); if (j.Opt("schema")?.Str() != "suborned.audio-cues/1") throw new FormatException("schema is not suborned.audio-cues/1");
            foreach (var k in j["buses"].Keys) busDb[k] = F(j["buses"][k]["db"]);
            foreach (var c in j["cues"].Items)
                cues.Add(new CueSpec { Id = c["id"].Str(), Log = c["on"]["log"].Str(), Clip = c["clip"].Str(), Bus = c["bus"].Str(), Volume = F(c["volume"]), Pitch = c.Opt("pitch") is JNode p ? F(p) : 1f, CooldownS = c.Opt("voices")?.Opt("cooldownS") is JNode cd ? F(cd) : 0f });
            foreach (var l in j["loops"].Items)
            {
                if (l["id"].Str() != "rival-car-engine") continue;
                var sp = l["spatial"]; var gn = l["gain"]; var fd = l["fade"];
                engineLoop = new LoopSpec
                {
                    Id = l["id"].Str(), Clip = l["clip"].Str(), Bus = l["bus"].Str(), Volume = F(l["volume"]), MinD = F(sp["minDistanceM"]), MaxD = F(sp["maxDistanceM"]), Doppler = F(sp["dopplerLevel"]),
                    Lo = F(gn["movingSpeedMps"].Items[0]), Hi = F(gn["movingSpeedMps"].Items[1]), VolLo = F(gn["volumeScale"].Items[0]), VolHi = F(gn["volumeScale"].Items[1]),
                    PitchLo = F(gn["pitch"].Items[0]), PitchHi = F(gn["pitch"].Items[1]), InS = F(fd["inS"]), OutS = F(fd["outS"]),
                };
            }
        }

        /// <summary>A bus's gain (linear), its parent's multiplied in.</summary>
        float Bus(string bus) { float db = busDb.TryGetValue(bus, out var d) ? d : 0f; if (duckNow.TryGetValue(bus, out var dk)) db += dk; if (bus != "master" && busDb.TryGetValue("master", out var m)) db += m; return Mathf.Pow(10f, db / 20f) * Settings.Current.audio.effects / 100f; }

        /// <summary>A log line in (Drain): its cue, once, if the cue file has one for its code.</summary>
        void AudioLog(LogLine l)
        {
            if (Game == null) return; AudioInit(); PackLog(l); if (Game.Range) return;
            foreach (var c in cues)
            {
                if (c.Log != l.Code) continue;
                CuesPlayed.Add($"{c.Id}@{MissionClock(l.Tick)}"); if (SoundOff) continue; /* brief 087: what the log asked for, whatever the cooldown did */ float now = Time.unscaledTime; if (cueLast.TryGetValue(c.Id, out var t0) && now - t0 < c.CooldownS) continue; cueLast[c.Id] = now;
                var clip = Resources.Load<AudioClip>(c.Clip); if (!clip) { Debug.LogWarning($"audio cue {c.Id}: no clip {c.Clip}"); continue; }
                cueSource.pitch = c.Pitch; cueSource.panStereo = c.Id == "rivals-coming" ? EntryPan() : 0f;
                cueSource.PlayOneShot(clip, c.Volume * Bus(c.Bus));
                if (c.Id == "rivals-coming") loopGain = Mathf.Max(loopGain, 0.001f); // the loop starts on the same tick
            }
        }

        /// <summary>Chime's optional pan: toward the side of the screen the car enters from, clamp(dot(cameraRight, entry - focus) / 40 m, -0.6, 0.6).</summary>
        float EntryPan()
        {
            var g = Game; if (!cam || g == null) return 0f; Vector3 entry;
            try { entry = g.RivalCarPose() is { } cp ? frame.World(cp.pos) : frame.World(g.S.CarStand(g.S.RivalCarEntry)); } catch (Exception) { return 0f; }
            var r = cam.transform.right; r.y = 0f; r.Normalize(); return Mathf.Clamp(Vector3.Dot(r, entry - focus) / 40f, -0.6f, 0.6f);
        }

        /// <summary>Per frame: the listener on the ground focus; the engine loop on the rival car, by its drawn speed.</summary>
        void RivalAudioFrame()
        {
            if (Game == null || views == null) return; AudioInit();
            if (listener) { listener.position = focus; if (cam) listener.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f); } // brief 087: 3D left and right turn with the camera (they came out reversed at yaw 210)
            PackFrame(); if (Game.Range) return;
            if (SoundOff) { if (loopSource && loopSource.isPlaying) loopSource.Stop(); return; } if (Time.frameCount % 60 == 0) foreach (var l in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude)) if (l.transform != listener && l.enabled) l.enabled = false; // one listener: the ground focus's (a camera rebuilt by Begin brings its own)
            var car = views.Find("car/rivals"); bool shown = car && car.gameObject.activeInHierarchy;
            if (engineLoop == null) return;
            if (!loopSource && shown)
            {
                var clip = Resources.Load<AudioClip>(engineLoop.Clip); if (!clip) { engineLoop = null; Debug.LogWarning("audio loop: no clip " + (clip ? "" : "Audio/rival_car_engine")); return; }
                loopSource = car.gameObject.AddComponent<AudioSource>(); loopSource.clip = clip; loopSource.loop = true; loopSource.playOnAwake = false; loopSource.spatialBlend = 1f;
                loopSource.rolloffMode = AudioRolloffMode.Logarithmic; loopSource.minDistance = engineLoop.MinD; loopSource.maxDistance = engineLoop.MaxD; loopSource.dopplerLevel = engineLoop.Doppler; loopSource.volume = 0f;
                loopLast = car.position;
            }
            if (!loopSource) return;
            float dt = Mathf.Max(1e-4f, Time.unscaledDeltaTime); var mv = shown ? car.position - loopLast : Vector3.zero; mv.y = 0f; loopLast = shown ? car.position : loopLast;
            float simDt = Paused || Game.Over ? 0f : dt * Speed; float mps = simDt > 0f ? mv.magnitude / simDt : 0f; // the car's speed in the street's seconds (a 2x clock is not a faster car)
            bool moving = shown && !Paused && mps > 0.05f;
            float want = moving ? 1f : 0f; loopGain = Mathf.MoveTowards(loopGain, want, dt / (want > loopGain ? engineLoop.InS : engineLoop.OutS));
            if (loopGain > 0.001f && !loopSource.isPlaying && !Paused) { loopSource.time = (Streams.Fnv1a32("rival-car-engine|" + Game.CarStTick) % 1000u) / 1000f * loopSource.clip.length; loopSource.Play(); } // a start offset from the leg, never a random one (shots replay)
            if (Paused && loopSource.isPlaying) loopSource.Pause(); else if (!Paused && loopGain > 0.001f && !loopSource.isPlaying) loopSource.UnPause();
            float k = Mathf.InverseLerp(engineLoop.Lo, engineLoop.Hi, mps);
            loopSource.volume = engineLoop.Volume * Bus(engineLoop.Bus) * Mathf.Lerp(engineLoop.VolLo, engineLoop.VolHi, k) * loopGain;
            loopSource.pitch = Mathf.Lerp(engineLoop.PitchLo, engineLoop.PitchHi, k);
            if (loopGain <= 0.001f && loopSource.isPlaying) loopSource.Stop();
        }
    }
}
