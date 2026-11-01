using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 087 (Chime, briefs/087/wiring.md; data: briefs/087/cues.json, shipped as StreamingAssets/sim/sound.json until Chime moves it): the sound pack. It reads and never writes: everything hangs off the
    /// log (Drain) and view frames; no order, no step, no random stream. Any variation comes from a hash of the cue, tick and shooter. A voice pool of 16; every one-shot loaded up front. CuesPlayed records what the log
    /// asked for, whatever the caps or the clips did. SoundOff (the proof) silences all of it and changes nothing else.</summary>
    public sealed partial class HandoverHost
    {
        sealed class PackCue { public string Id = "", File = "", Group = "", Spatial = ""; public int Voices = 1; public float Cooldown; public bool Loop; public AudioClip Clip; }
        sealed class Voice { public AudioSource S; public string Cue = "", Group = ""; public float Gain = 1f, Born = -9f; public Vector3? PanFrom; }
        /// <summary>For the proof: no sound at all (same hash either way).</summary>
        public static bool SoundOff;
        readonly Dictionary<string, PackCue> pack = new(); readonly Dictionary<string, (float min, float max, float blend)> packSpatial = new(); readonly Dictionary<string, float> packDuck = new(), duckNow = new();
        readonly List<Voice> voices = new(); AudioSource bedSource, roomSource, patrolA, patrolB; bool packReady, packLoaded;
        Game packGame; int packLastTick, clickNext = -1, clickReadAt = -1, noticeStage, noticeTick = -1; bool chimeDone, patrolAlarmed; string cardSeen = ""; float dangerDuckUntil, patrolMix; readonly Dictionary<int, float> noticeLast = new();
        readonly List<(string cue, Vector3 at, int tick, string by)> shotsDue = new();
        /// <summary>How the pack came up, for the proof.</summary>
        public string PackRead { get; private set; } = "";
        public float ListenerYaw => listener ? listener.eulerAngles.y : 0f;

        void PackInit()
        {
            if (packReady) return; packReady = true;
            try
            {
                var path = System.IO.Path.Combine(SimBundle.Dir, "sound.json"); if (!System.IO.File.Exists(path)) { PackRead = "no sound.json"; return; }
                var j = JNode.Parse(System.IO.File.ReadAllText(path), "sound.json");
                foreach (var b in j["buses"].Keys) { busDb[b] = F(j["buses"][b]["db"]); packDuck[b] = j["buses"][b].Opt("duck_convo_db") is JNode d ? F(d) : 0f; duckNow[b] = 0f; }
                foreach (var s in j["spatial"].Keys) { var n = j["spatial"][s]; packSpatial[s] = (n.Opt("minDistanceM") is JNode a ? F(a) : 10f, n.Opt("maxDistanceM") is JNode m ? F(m) : 100f, F(n["blend"])); }
                foreach (var c in j["cues"].Items) pack[c["id"].Str()] = new PackCue { Id = c["id"].Str(), File = c["file"].Str(), Group = c["mix_group"].Str(), Spatial = c["spatial"].Str(), Voices = c["voices_max"].Int(), Cooldown = F(c["cooldown_s"]), Loop = c["loop"].Bool() };
            }
            catch (Exception e) { Debug.LogWarning("sound.json: " + e.Message); pack.Clear(); PackRead = "bad: " + e.Message; return; }
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) { PackRead = $"{pack.Count} cues, no clips (batch)"; return; }
            int got = 0; foreach (var c in pack.Values) if ((c.Clip = Resources.Load<AudioClip>(c.File))) got++; packLoaded = true;
            for (int i = 0; i < 16; i++) { var s = PackSource("voice" + i, listener); if (s) voices.Add(new Voice { S = s }); }
            bedSource = PackSource("bed", listener); roomSource = PackSource("room", listener); if (listener && !listener.GetComponent<AudioMaster>()) listener.gameObject.AddComponent<AudioMaster>();
            PackRead = $"{pack.Count} cues, {got} clips, {voices.Count} voices";
        }
        static AudioSource PackSource(string name, Transform parent) { var go = new GameObject(name); if (parent) go.transform.SetParent(parent, false); var s = go.AddComponent<AudioSource>(); if (s) { s.playOnAwake = false; s.dopplerLevel = 0f; s.spatialBlend = 0f; } return s; }

        /// <summary>A bus's gain now, Options and the duck in: master is AudioListener.volume's own.</summary>
        float PackGain(string bus)
        {
            float db = busDb.TryGetValue(bus, out var d) ? d : 0f; if (duckNow.TryGetValue(bus, out var dk)) db += dk;
            if (bus == "fight" && Time.unscaledTime < dangerDuckUntil) db -= 4f; // danger over the fight: the siren and a notice stand clear
            var a = Settings.Current.audio; return Mathf.Pow(10f, db / 20f) * (bus == "ambience" ? a.ambience : a.effects) / 100f;
        }

        /// <summary>One voice: the oldest of the cue's own past its cap, else a free one, else the oldest of all.</summary>
        Voice PackPlay(string id, Vector3? at, int tick, string by = "", float delayMax = 0f, float pitchSpread = 0f)
        {
            if (SoundOff || !packLoaded || !pack.TryGetValue(id, out var c) || !c.Clip || voices.Count == 0) return null;
            Voice mine = null, oldestOwn = null, oldest = null; int own = 0;
            foreach (var v in voices) { bool busy = v.S.isPlaying; if (busy && v.Cue == id) { own++; if (oldestOwn == null || v.Born < oldestOwn.Born) oldestOwn = v; } if (!busy && mine == null) mine = v; if (oldest == null || v.Born < oldest.Born) oldest = v; }
            var use = own >= c.Voices ? oldestOwn : mine ?? oldest; if (use == null) return null; use.S.Stop();
            uint h = Streams.Fnv1a32($"{id}|{tick}|{by}"); var sp = packSpatial.TryGetValue(c.Spatial, out var ss) ? ss : (10f, 100f, 0f);
            use.Cue = id; use.Group = c.Group; use.Born = Time.unscaledTime; use.Gain = 1f; use.PanFrom = null;
            var s = use.S; s.clip = c.Clip; s.loop = false; s.spatialBlend = sp.Item3; s.rolloffMode = AudioRolloffMode.Logarithmic; s.minDistance = sp.Item1; s.maxDistance = sp.Item2; s.panStereo = 0f;
            s.transform.position = at ?? (listener ? listener.position : Vector3.zero); s.pitch = 1f + pitchSpread * (((h >> 8) % 1000u) / 500f - 1f); s.volume = PackGain(c.Group);
            if (delayMax > 0f) s.PlayScheduled(AudioSettings.dspTime + delayMax * ((h % 1000u) / 1000f)); else s.Play();
            return use;
        }

        bool PackCool(string id) { if (!pack.TryGetValue(id, out var c) || c.Cooldown <= 0f) return true; float now = Time.unscaledTime; if (cueLast.TryGetValue(id, out var t0) && now - t0 < c.Cooldown) return false; cueLast[id] = now; return true; }
        void PackCue1(string id, LogLine l, Vector3? at = null) { CuesPlayed.Add($"{id}@{MissionClock(l.Tick)}"); if (PackCool(id)) PackPlay(id, at, l.Tick); }

        /// <summary>A log line in. The fight group only in the range (Chime: Sky judges the voices there, beside the flash and the jolt).</summary>
        void PackLog(LogLine l)
        {
            PackInit(); bool range = Game.Range;
            switch (l.Code)
            {
                case "shot.hit": case "shot.miss": case "shot.covering": PackShot(l); return;
                case "car.hit": PackCue1("hit_metal", l); return;
                case "fight.down": case "guard.down": case "security.officer-down": case "rivals.down": case "civilian.down": case "enforcer.down": PackDown(l, "down_body"); return;
                case "patrol.down": patrolAlarmed = false; PackDown(l, "down_robot", "patrol"); return;
                case "response.down": PackDown(l, l.Text.StartsWith("robot-") ? "down_robot" : "down_body", l.Text.Split(' ')[0]); return;
            }
            if (range) return;
            switch (l.Code)
            {
                case "guard.at-cafe": PackCue1("cafe_bell", l); break;
                case "guard.leaves-cafe": PackCue1("cafe_door", l); break;
                case "attention.noticed": case "attention.wary": case "attention.reporting": { int st = l.Code.EndsWith("noticed") ? 1 : l.Code.EndsWith("wary") ? 2 : 3; if (noticeTick != l.Tick || st > noticeStage) { noticeStage = st; noticeTick = l.Tick; } CuesPlayed.Add($"notice_{st}@{MissionClock(l.Tick)}"); break; } // several on one tick: only the highest, in the frame
                case "patrol.alarm": patrolAlarmed = true; PackCue1("patrol_break", l, views?.Find("patrol")?.position); break;
                case "patrol.breaks": PackCue1("patrol_break", l, views?.Find("patrol")?.position); break;
                case "patrol.sweep": case "patrol.returns": case "patrol.resumes": case "patrol.stands-down": patrolAlarmed = false; break;
                case "alarm.dispatches":
                    CuesPlayed.Add($"police_siren@{MissionClock(l.Tick)}");
                    if (PackCool("police_siren") && PackPlay("police_siren", null, l.Tick) is Voice sv) { Pt? from = null; foreach (var u in Game.Units) if (u.St == "wait" || u.St == "walk") { from = u.From; break; } if (from.HasValue) sv.PanFrom = frame.World(from.Value); }
                    break;
            }
        }

        void PackDown(LogLine l, string id, string who = "")
        {
            Vector3? at = null; if (who.Length > 0) at = DrawnAt(who); else { int sp = l.Text.IndexOf(" is down", StringComparison.Ordinal); if (sp > 0) at = DrawnAt(l.Text[..sp]); }
            PackCue1(id, l, at);
        }

        void PackShot(LogLine l)
        {
            int f = l.Text.IndexOf(" fires on ", StringComparison.Ordinal), n = 10; if (f <= 0) { f = l.Text.IndexOf(" lays covering fire on ", StringComparison.Ordinal); n = 23; } if (f <= 0) return;
            int e = l.Text.IndexOf(" at ", f + n, StringComparison.Ordinal); string by = l.Text[..f], t = (e > 0 ? l.Text[(f + n)..e] : l.Text[(f + n)..]).TrimEnd('.');
            var from = DrawnAt(by); var to = DrawnAt(t); string fire = FireVoice(by);
            CuesPlayed.Add($"{fire}@{MissionClock(l.Tick)}"); if (from.HasValue) shotsDue.Add((fire, from.Value, l.Tick, by));
            if (l.Code == "shot.hit") { string hit = t == "patrol" || t.StartsWith("robot-") || t.StartsWith("camera:") || t.StartsWith("car") ? "hit_metal" : "hit_body"; CuesPlayed.Add($"{hit}@{MissionClock(l.Tick)}"); if (to.HasValue) shotsDue.Add((hit, to.Value, l.Tick, by)); }
            else if (l.Code == "shot.miss") { CuesPlayed.Add($"miss@{MissionClock(l.Tick)}"); if (to.HasValue) shotsDue.Add(("miss", to.Value, l.Tick, by)); }
        }
        /// <summary>The shooter's voice, by the gun he holds where that can be read (the range lets loadouts change).</summary>
        string FireVoice(string by)
        {
            if (by is "scout" or "fixer" or "tech" or "enforcer" && Game.Ops[(int)Op.Parse(by)].Gun.W == Game.R.Carbine) return "fire_carbine";
            if (by.StartsWith("rival")) return "fire_rival"; if (by == "patrol" || by.StartsWith("robot-")) return "fire_robot";
            return by is "scout" or "fixer" or "tech" or "enforcer" ? "fire_pistol" : "fire_police";
        }

        /// <summary>Per frame, on the view: loops, ducks, the frame-read cues. Shots land in beats: past 6 on one tick the farthest go, and each is staggered 0-40 ms and pitched ±3 % by hash.</summary>
        int packFrame = -1;
        void PackFrame()
        {
            if (packFrame == Time.frameCount) return; packFrame = Time.frameCount; PackInit();
            var g = Game; float dt = Time.unscaledDeltaTime;
            if (packGame != g) { packGame = g; packLastTick = g.Tick; clickNext = -1; clickReadAt = -1; chimeDone = false; patrolAlarmed = false; cardSeen = ""; noticeTick = -1; shotsDue.Clear(); foreach (var v in voices) v.S.Stop(); }
            bool convo = ConvoOn, range = g.Range;
            // the ducks: over the 0.12 s dip into a scene, 0.4 s back
            foreach (var k in packDuck) { float want = convo ? k.Value : 0f; duckNow[k.Key] = Mathf.MoveTowards(duckNow[k.Key], want, Mathf.Abs(k.Value) * dt / (convo ? ConvoDipS : 0.4f)); }
            foreach (var v in voices) if (v.S.isPlaying || v.Born > Time.unscaledTime - 0.05f) { v.S.volume = PackGain(v.Group) * v.Gain; if (v.PanFrom is Vector3 pf && cam) { var r = cam.transform.right; r.y = 0f; r.Normalize(); v.S.panStereo = Mathf.Clamp(Vector3.Dot(r, pf - focus) / 40f, -0.8f, 0.8f); dangerDuckUntil = Time.unscaledTime + 0.1f; } }
            // shots, nearest six of a tick
            if (shotsDue.Count > 0)
            {
                shotsDue.Sort((a, b) => a.tick != b.tick ? a.tick.CompareTo(b.tick) : (a.at - focus).sqrMagnitude.CompareTo((b.at - focus).sqrMagnitude)); int run = 0, tk = -1, fires = 0;
                foreach (var s in shotsDue) { if (s.tick != tk) { tk = s.tick; run = 0; fires = 0; } bool fire = s.cue.StartsWith("fire_"); if (fire && ++fires > 6) continue; run++; PackPlay(s.cue, s.at, s.tick, s.by, 0.04f, 0.03f); }
                shotsDue.Clear();
            }
            if (range) { packLastTick = g.Tick; return; }
            if (noticeTick >= 0) { float now = Time.unscaledTime; if (!noticeLast.TryGetValue(noticeStage, out var t0) || now - t0 >= 0.5f) { noticeLast[noticeStage] = now; PackPlay("notice_" + noticeStage, null, noticeTick); dangerDuckUntil = now + 0.5f; } noticeTick = -1; noticeStage = 0; }
            // the handover chime: no log line; once a run, when the clock crosses it
            if (!chimeDone && g.S.Chime > 0 && g.Tick >= Game.T(g.S.Chime)) { chimeDone = true; if (packLastTick < Game.T(g.S.Chime)) { CuesPlayed.Add($"handover_chime@{MissionClock(g.Tick)}"); PackPlay("handover_chime", null, g.Tick); } }
            // the patrol's turn: read once a second; silent until its routine is watched. Cameras are fixed, so the click is on the patrol's turns
            if (clickReadAt < 0 || g.Tick >= clickReadAt) { clickReadAt = g.Tick + Game.Hz; clickNext = -1; var rt = g.Routine("patrol", 60 * Game.Hz); if (rt != null) foreach (var e in rt) if (e.What.StartsWith("turn-at") && e.Tick > g.Tick) { clickNext = e.Tick; break; } }
            if (clickNext >= 0 && g.Tick >= clickNext) { clickNext = -1; CuesPlayed.Add($"camera_click@{MissionClock(g.Tick)}"); PackPlay("camera_click", views?.Find("patrol")?.position, g.Tick); }
            // the card's chime: a card that opens something, first up
            if (PersonCardNow is PersonCard pc && pc.Opens.Count > 0 && !ConvoOn) { string key = pc.Id + "|" + pc.Made; if (key != cardSeen) { cardSeen = key; CuesPlayed.Add($"card_new@{MissionClock(g.Tick)}"); PackPlay("card_new", null, g.Tick); } }
            PackLoops(g, dt); packLastTick = g.Tick;
        }

        void PackLoops(Game g, float dt)
        {
            if (!packLoaded) return; bool on = !SoundOff && !g.Over && Ending.Length == 0;
            if (bedSource && pack.TryGetValue("street_bed", out var bed) && bed.Clip)
            {
                if (on && !bedSource.isPlaying) { bedSource.clip = bed.Clip; bedSource.loop = true; bedSource.Play(); } else if (!on && bedSource.isPlaying) bedSource.Stop();
                bedSource.volume = PackGain("ambience") * (Paused && !ConvoOn ? 0.5f : 1f);
            }
            if (roomSource && pack.TryGetValue("room_tone", out var room) && room.Clip)
            {
                bool want = on && ConvoOn; float vol = Mathf.MoveTowards(roomSource.volume, want ? PackGain("convo") : 0f, dt / (want ? ConvoDipS : 0.4f));
                if (want && !roomSource.isPlaying) { roomSource.clip = room.Clip; roomSource.loop = true; roomSource.volume = 0f; roomSource.Play(); } roomSource.volume = vol; if (!want && vol <= 0.001f && roomSource.isPlaying) roomSource.Stop();
            }
            var pt = views?.Find("patrol"); bool up = on && pt && pt.gameObject.activeInHierarchy && !g.PatrolDown;
            if (up && !patrolA && pack.TryGetValue("patrol_loop", out var pl) && pl.Clip && pack.TryGetValue("patrol_chase", out var pc) && pc.Clip)
            {
                var sp = packSpatial["3d"]; AudioSource Mk(AudioClip c, string id) { var s = pt.gameObject.AddComponent<AudioSource>(); s.clip = c; s.loop = true; s.playOnAwake = false; s.dopplerLevel = 0f; s.spatialBlend = sp.blend; s.rolloffMode = AudioRolloffMode.Logarithmic; s.minDistance = sp.min; s.maxDistance = sp.max; s.volume = 0f; s.time = (Streams.Fnv1a32(id + "|" + g.Seed) % 1000u) / 1000f * c.length; return s; }
                patrolA = Mk(pl.Clip, "patrol_loop"); patrolB = Mk(pc.Clip, "patrol_chase");
            }
            if (patrolA)
            {
                bool chase = g.PatrolChasing || patrolAlarmed; patrolMix = Mathf.MoveTowards(patrolMix, chase ? 1f : 0f, dt / 0.3f); float gain = PackGain("danger");
                patrolA.volume = gain * (1f - patrolMix); patrolB.volume = gain * patrolMix;
                foreach (var s in new[] { patrolA, patrolB }) { bool run = up && !Paused; if (run && !s.isPlaying) s.Play(); else if (!run && s.isPlaying) s.Pause(); }
            }
        }

        /// <summary>The dips' clicks, from the scene.</summary>
        void PackCut(bool into) { if (Game == null) return; PackInit(); CuesPlayed.Add($"{(into ? "cut_in" : "cut_out")}@{MissionClock(Game.Tick)}"); PackPlay(into ? "cut_in" : "cut_out", null, Game.Tick); }

        /// <summary>For the proof: each clip as imported; every voice cap on one sample at nearest distance; one looping clip alone over its seam.</summary>
        public List<string> PackClips() { var l = new List<string>(); foreach (var c in pack.Values) l.Add(c.Clip ? $"{c.Id} {c.Clip.loadType} {c.Clip.channels}ch {c.Clip.frequency} {c.Clip.length:0.00}s" : $"{c.Id} NONE"); return l; }
        public int PackStress() { int n = 0; foreach (var c in pack.Values) { if (c.Loop || c.Id.StartsWith("foot")) continue; for (int i = 0; i < c.Voices && n < voices.Count; i++) { var v = voices[n++]; v.S.Stop(); v.S.clip = c.Clip; v.S.spatialBlend = 0f; v.S.pitch = 1f; v.S.volume = 1f; v.Cue = c.Id; v.Group = c.Group; v.Gain = 1f; v.Born = Time.unscaledTime; v.PanFrom = null; v.S.time = c.Clip.length > 0f ? 0f : 0f; v.S.PlayScheduled(AudioSettings.dspTime + 0.1); } } return n; }
        public float PackSoloLoop(string id, float before)
        {
            foreach (var v in voices) v.S.Stop(); if (bedSource) bedSource.Stop(); if (roomSource) roomSource.Stop(); if (patrolA) { patrolA.volume = 0f; patrolB.volume = 0f; } if (loopSource) loopSource.Stop();
            if (!pack.TryGetValue(id, out var c) || !c.Clip) return 0f; var s = id == "room_tone" ? roomSource : bedSource; s.clip = c.Clip; s.loop = true; s.volume = 1f; s.time = Mathf.Max(0f, c.Clip.length - before); s.Play(); return c.Clip.length;
        }
    }

    /// <summary>Brief 087: the master compressor (Chime's numbers: -6 dB, 4:1, 5 ms attack, 100 ms release), on the listener, after everything, with a hard stop at 0 dBFS; and the only meter that reads the mix the player hears. Audio thread; no allocation.</summary>
    public sealed class AudioMaster : MonoBehaviour
    {
        const float ThresholdDb = -6f, Ratio = 4f, AttackS = 0.005f, ReleaseS = 0.1f; float env, att, rel; int rate;
        /// <summary>The meter since the last Reset: peak in and out (linear), the most gain taken (dB), the loudest 400 ms RMS out (dBFS), and samples the hard stop touched.</summary>
        public static volatile float PeakIn, PeakOut, MostReductionDb, RmsMaxDb; public static volatile int HardStops; public static volatile bool Bypass;
        static double sum; static int count, window; public static void Reset() { PeakIn = PeakOut = MostReductionDb = 0f; RmsMaxDb = -120f; HardStops = 0; sum = 0; count = 0; }
        /// <summary>A copy of what leaves the master, for a seam check: a buffer to fill, how far filled.</summary>
        public static volatile float[] Tap; public static volatile int TapAt;
        void Awake() { rate = AudioSettings.outputSampleRate; att = Mathf.Exp(-1f / (AttackS * rate)); rel = Mathf.Exp(-1f / (ReleaseS * rate)); window = (int)(0.4f * rate); Reset(); }
        void OnAudioFilterRead(float[] d, int ch)
        {
            if (ch <= 0) return; float pIn = PeakIn, pOut = PeakOut, most = MostReductionDb; int hard = HardStops;
            for (int i = 0; i < d.Length; i += ch)
            {
                float m = 0f; for (int c = 0; c < ch; c++) { float a = Mathf.Abs(d[i + c]); if (a > m) m = a; } if (m > pIn) pIn = m;
                env = m > env ? att * env + (1f - att) * m : rel * env + (1f - rel) * m;
                float g = 1f; if (!Bypass && env > 0.501f) { float db = 20f * Mathf.Log10(env), over = db - ThresholdDb, cut = over - over / Ratio; g = Mathf.Pow(10f, -cut / 20f); if (cut > most) most = cut; }
                float e = 0f;
                for (int c = 0; c < ch; c++) { float v = d[i + c] * g; if (!Bypass) { if (v > 1f) { v = 1f; hard++; } else if (v < -1f) { v = -1f; hard++; } } d[i + c] = v; float a = Mathf.Abs(v); if (a > pOut) pOut = a; e += v * v; }
                sum += e / ch; if (++count >= window) { float r = 10f * Mathf.Log10((float)(sum / count) + 1e-12f); if (r > RmsMaxDb) RmsMaxDb = r; sum = 0; count = 0; }
                var t = Tap; if (t != null) { int at = TapAt; if (at + ch <= t.Length) { for (int c = 0; c < ch; c++) t[at + c] = d[i + c]; TapAt = at + ch; } }
            }
            PeakIn = pIn; PeakOut = pOut; MostReductionDb = most; HardStops = hard;
        }
    }
}
