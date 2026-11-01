using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 087: the sound pack, measured in the running player: clips as imported, the listener's yaw, the sum at its worst (every voice cap on one sample), each loop over its seam after Vorbis, and the same hash with sound on and off, across a route and a fight.</summary>
    public static partial class Proof085Scenes
    {
        static float Db(float v) => 20f * Mathf.Log10(Mathf.Max(v, 1e-6f));
        static void WavF(string path, float[] d, int n, int rate) { using var w = new BinaryWriter(File.Create(path)); w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)2); w.Write(rate); w.Write(rate * 4); w.Write((short)4); w.Write((short)16); w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n * 2); for (int i = 0; i < n; i++) w.Write((short)Mathf.Clamp(d[i] * 32767f, -32768f, 32767f)); }

        static IEnumerator SeamOf(HandoverHost h, string id, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            int rate = AudioSettings.outputSampleRate; var buf = new float[rate * 2 * 5]; AudioMaster.Bypass = true; float len = h.PackSoloLoop(id, 2f); yield return new WaitForSecondsRealtime(0.2f); AudioMaster.TapAt = 0; AudioMaster.Tap = buf; yield return new WaitForSecondsRealtime(4f); AudioMaster.Tap = null; AudioMaster.Bypass = false; int n = AudioMaster.TapAt; WavF(Path.Combine(dir, "r087-seam-" + id + ".wav"), buf, n, rate);
            // the worst sample-to-sample step against the steps around it: a click stands far above them
            var steps = new List<float>(); float worst = 0f; int at = 0; for (int i = 2; i < n; i += 2) { float s = Mathf.Abs(buf[i] - buf[i - 2]); steps.Add(s); if (s > worst) { worst = s; at = i; } } steps.Sort(); float p999 = steps.Count > 0 ? steps[(int)(steps.Count * 0.999f)] : 0f;
            row("r087seam" + id, n > rate * 2 && worst < Mathf.Max(0.02f, p999 * 4f), $"{id} {len:0.0} s over its loop point: {n / 2 / (float)rate:0.0} s taken; worst step {worst:0.0000} at {at / 2 / (float)rate:0.00} s, 99.9th {p999:0.0000}, ratio {worst / Mathf.Max(1e-6f, p999):0.0}");
        }

        static IEnumerator SoundChecks(HandoverHost h, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            yield return Fresh(h, ConversationScenes.Always); var g = h.Game; h.SetPaused(true); yield return Frames(3);
            var clips = h.PackClips(); bool all = clips.Count > 0 && !clips.Exists(c => c.Contains("NONE")); row("r087clips", all, $"{h.PackRead}: {string.Join("; ", clips)}");
            bool yawOk = true; string yaws = ""; for (int t = 0; t < 4; t++) { h.TurnSnap(t); yield return new WaitForSecondsRealtime(0.6f); var cy = h.View.transform.eulerAngles.y; float ly = h.ListenerYaw; yaws += $"{cy:0}/{ly:0} "; yawOk &= Mathf.Abs(Mathf.DeltaAngle(cy, ly)) < 1f; } h.TurnSnap(0); row("r087listener", yawOk, $"camera/listener yaw: {yaws}");
            AudioMaster.Reset(); int n = h.PackStress(); yield return new WaitForSecondsRealtime(1.2f); float inDb = Db(AudioMaster.PeakIn), outDb = Db(AudioMaster.PeakOut), most = AudioMaster.MostReductionDb; int hard = AudioMaster.HardStops;
            row("r087cap", n > 0 && AudioMaster.PeakOut <= 1.0001f, $"{n} voices on one sample, master {Settings.Current.audio.master}, effects {Settings.Current.audio.effects}: in {inDb:0.0} dBFS, out {outDb:0.0} dBFS, most taken {most:0.0} dB, hard stops {hard}, loudest 400 ms RMS {AudioMaster.RmsMaxDb:0.0} dBFS");
            yield return SeamOf(h, "street_bed", dir, log, row); yield return SeamOf(h, "room_tone", dir, log, row);
            // the same hash with sound on and off, on a route with its scenes and on a fight
            var res = new List<string>(); var sets = new List<HashSet<string>>();
            foreach (var (route, until) in new[] { ("route-a", 1800), ("loud-fight", 2600) })
                foreach (bool off in new[] { false, true })
                {
                    HandoverHost.SoundOff = off; yield return Fresh(h, ConversationScenes.Always, sameSeed: true); g = h.Game; h.CuesPlayed.Clear(); var r = new RouteRun { Orders = Route(route) };
                    while (!g.Over && g.Tick < until) { Step(h, r, until); if (h.ConvoPending || h.ConvoOn) { for (int i = 0; i < 3 && h.ConvoPending; i++) yield return null; h.ConvoEndNow(); yield return null; } else if (g.Tick % 50 == 0) yield return null; }
                    var cp = g.Checkpoints.Count > 0 ? g.Checkpoints[^1] : (0, 0UL); var names = new HashSet<string>(); foreach (var c in h.CuesPlayed) names.Add(c.Split('@')[0]); sets.Add(names); int logCues = h.CuesPlayed.FindAll(c => !(c.StartsWith("card_new") || c.StartsWith("cut_") || c.StartsWith("camera_click") || c.StartsWith("handover_chime"))).Count; /* those read the view's frames; the rest, the log */ res.Add($"{route} {(off ? "off" : "on")}: tick {g.Tick}, {cp.Item1}:{cp.Item2:x16}, log {g.Log.Count}, log cues {logCues}");
                }
            HandoverHost.SoundOff = false; string H(string s) { int i = s.IndexOf(": "); return s[i..]; }
            row("r087hash", H(res[0]) == H(res[1]) && H(res[2]) == H(res[3]), string.Join(" | ", res));
            var seen = new List<string>(sets[0]); seen.AddRange(sets[2]); row("r087cuesFired", seen.Contains("fire_carbine") || seen.Contains("fire_pistol"), $"route-a: {string.Join(",", sets[0])} || loud-fight: {string.Join(",", sets[2])}");
        }
    }
}
