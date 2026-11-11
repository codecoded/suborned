using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 107 (Chime, briefs/107/chime/wiring.md; Sky: "go with your recommendations, though rain needs to be light background noise"): the weather's
    /// and the night's sounds, from what the view draws (PlayView107: Game.Weather / Game.Light, or a shot's night). All on the ambience bus, like street_bed:
    /// its Options slider, its duck in a scene and the bed's halving on pause. Levels are Chime's, final: every source sits at the bus gain.
    /// - night: `night_bed` in place of `street_bed` on bedSource (PackLoops picks the clip: BedCue);
    /// - rain: `rain_bed` on a second 2D source over whichever bed plays; `rain_awning` and `drips` on 4 + 4 sources of their own (3d-near) at the café
    ///   awning and parasol sites nearest the camera's focus (no eave sites in the world yet: drips stand at the same sites, as the handoff allows). A source
    ///   that changes site fades out and back in over about a second; each loop starts at Fnv1a32("cue|site") % its length, so neighbours never play in step;
    /// - night: the far tram, a 2D one-shot every 40-90 s, the gap from Fnv1a32("night_tram|n") (its row's 40 s cooldown the floor).
    /// Wet steps are not wired: no footsteps are played anywhere yet (the handoff: only if footsteps are). Mist and overcast add no sound. No UnityEngine.Random.</summary>
    public sealed partial class HandoverHost
    {
        const int RainSites = 4; const float RainFadeS = 1f;
        sealed class SiteLoop { public AudioSource S; public string Site = ""; public string Want = ""; public Vector3 At, WantAt; public float Gain; }
        AudioSource rainSource; readonly List<SiteLoop> awningLoops = new(), dripLoops = new();
        List<(string id, Vector3 at)> rainSites; float tramNext = -1f; int tramN;

        /// <summary>The bed's cue now: the night bed at night, else the street bed.</summary>
        string BedCue() => PlayView107.Night && pack.TryGetValue("night_bed", out var nb) && nb.Clip ? "night_bed" : "street_bed";

        void PackWeather(Game g, float dt, bool on)
        {
            bool rain = on && PlayView107.Weather == "rain", night = on && PlayView107.Night;
            float amb = PackGain("ambience") * (Paused && !ConvoOn ? 0.5f : 1f);
            // the rain bed, over the bed
            if (pack.TryGetValue("rain_bed", out var rb) && rb.Clip)
            {
                if (!rainSource) rainSource = PackSource("rain bed", listener);
                if (rainSource)
                {
                    if (rain && !rainSource.isPlaying) { rainSource.clip = rb.Clip; rainSource.loop = true; rainSource.time = (Streams.Fnv1a32("rain_bed|" + g.Seed) % 1000u) / 1000f * rb.Clip.length; rainSource.Play(); }
                    else if (!rain && rainSource.isPlaying) rainSource.Stop();
                    rainSource.volume = amb;
                }
            }
            // awnings and drips at the nearest sites
            if (rain && rainSites == null) rainSites = FindRainSites();
            Loops(awningLoops, "rain_awning", rain, amb, dt); Loops(dripLoops, "drips", rain, amb, dt);
            // the far tram at night
            if (!night) { tramNext = -1f; return; }
            float now = Time.unscaledTime;
            if (tramNext < 0f) tramNext = now + 40f + Streams.Fnv1a32($"night_tram|{tramN}") % 51u;
            else if (now >= tramNext && !Paused && !ConvoOn) { tramN++; if (PackCool("night_tram")) PackPlay("night_tram", null, g.Tick, "night_tram" + tramN); tramNext = now + 40f + Streams.Fnv1a32($"night_tram|{tramN}") % 51u; }
        }

        void Loops(List<SiteLoop> loops, string cue, bool rain, float amb, float dt)
        {
            if (!pack.TryGetValue(cue, out var c) || !c.Clip) return;
            if (!rain) { foreach (var l in loops) if (l.S && l.S.isPlaying) { l.S.Stop(); l.Gain = 0f; l.Site = ""; } return; }
            if (rainSites == null || rainSites.Count == 0) return;
            while (loops.Count < RainSites)
            {
                var s = PackSource(cue + " " + loops.Count, null); if (!s) return;
                var sp = packSpatial.TryGetValue(c.Spatial, out var ss) ? ss : (8f, 45f, 1f); s.spatialBlend = sp.Item3; s.rolloffMode = AudioRolloffMode.Logarithmic; s.minDistance = sp.Item1; s.maxDistance = sp.Item2; s.loop = true; s.clip = c.Clip;
                loops.Add(new SiteLoop { S = s });
            }
            // the nearest sites to the focus
            var near = new List<(float d, string id, Vector3 at)>(); foreach (var (id, at) in rainSites) { var d = at - focus; d.y = 0f; near.Add((d.sqrMagnitude, id, at)); }
            near.Sort((a, b) => a.d != b.d ? a.d.CompareTo(b.d) : string.CompareOrdinal(a.id, b.id));
            var want = new List<(string id, Vector3 at)>(); for (int i = 0; i < near.Count && i < RainSites; i++) want.Add((near[i].id, near[i].at));
            // keep a loop that already stands at a wanted site; the others go to the remaining sites, fading out first
            var taken = new HashSet<string>(); foreach (var l in loops) if (want.Exists(w => w.id == l.Site)) { taken.Add(l.Site); l.Want = l.Site; }
            int k = 0; foreach (var l in loops) { if (taken.Contains(l.Site)) continue; while (k < want.Count && taken.Contains(want[k].id)) k++; if (k < want.Count) { l.Want = want[k].id; l.WantAt = want[k].at; taken.Add(want[k].id); k++; } else l.Want = ""; }
            foreach (var l in loops)
            {
                bool there = l.Site.Length > 0 && l.Want == l.Site;
                l.Gain = Mathf.MoveTowards(l.Gain, there ? 1f : 0f, dt / RainFadeS);
                if (!there && l.Gain <= 0.001f && l.Want.Length > 0)
                {   // silent: move to the new site, start at the site's own place in the loop
                    l.Site = l.Want; l.At = l.WantAt; l.S.transform.position = l.At; l.S.Stop();
                    l.S.time = (Streams.Fnv1a32($"{cue}|{l.Site}") % 100000u) / 100000f * c.Clip.length; l.S.Play();
                }
                if (l.S.isPlaying && Paused) l.S.Pause(); else if (!l.S.isPlaying && !Paused && l.Site.Length > 0) l.S.UnPause();
                l.S.volume = amb * l.Gain;
            }
        }

        /// <summary>The café awnings and parasols the world has drawn (kit pieces by name), one site per 3 m cell, at awning height.</summary>
        static List<(string id, Vector3 at)> FindRainSites()
        {
            var o = new List<(string, Vector3)>(); var seen = new HashSet<Vector2Int>();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                string n = r.name; if (!(n.Contains("Parasol") || n.Contains("CafeBay") || n.Contains("CafeSet"))) continue;
                var b = r.bounds; var key = new Vector2Int(Mathf.RoundToInt(b.center.x / 3f), Mathf.RoundToInt(b.center.z / 3f)); if (!seen.Add(key)) continue;
                o.Add(($"{key.x},{key.y}", new Vector3(b.center.x, Mathf.Min(b.max.y, b.min.y + 3f), b.center.z)));
            }
            Debug.Log($"HostAudio: {o.Count} rain sites (café awnings and parasols)");
            return o;
        }
    }
}
