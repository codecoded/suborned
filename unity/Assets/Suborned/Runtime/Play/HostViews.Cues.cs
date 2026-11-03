using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 084, the body's jolts (FightCues): knocked along the shot and flashed white when hit, a small kick back on his own shot. A
    /// procedural push on the figure's model and a material swap: it never touches the view's position, which is what everything else reads
    /// (DrawnAt, the rings, the target marks), and never the engine. 54 px a metre at play zoom. Figures only: a robot or a car takes none.</summary>
    public sealed partial class HostViews
    {
        sealed class Jolt { public float hitAt = -9f, kickAt = -9f; public Vector3 hitDir, kickDir; public Renderer[] rends; public Material[][] mats; public bool white; public Transform model; }
        readonly Dictionary<string, Jolt> jolts = new(); readonly List<string> joltsDone = new();
        static Material whiteMat; const float PxPerM = 54f, KickS = 0.14f;

        public bool CueCameraDead(string watcher) => CameraDead(watcher);
        Jolt JoltOf(string key) { if (!jolts.TryGetValue(key, out var j)) jolts[key] = j = new Jolt(); return j; }
        public void CueHit(string key, Vector3 dir) { if (figs.ContainsKey(key)) { var j = JoltOf(key); j.hitAt = Time.unscaledTime; j.hitDir = dir; } }
        public void CueKick(string key, Vector3 dir) { if (figs.ContainsKey(key)) { var j = JoltOf(key); j.kickAt = Time.unscaledTime; j.kickDir = dir; } }

        /// <summary>Per frame, after the figures took their poses (each pose sets the model's position, so the push never adds up).</summary>
        void Jolts()
        {
            if (jolts.Count == 0) return;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp(); float now = Time.unscaledTime; var v = FightCues.V; joltsDone.Clear();
            foreach (var kv in jolts)
            {
                var j = kv.Value; float hit = now - j.hitAt, kick = now - j.kickAt;
                bool fig = figs.TryGetValue(kv.Key, out var f) && f.model; bool lifted = this.lifted.TryGetValue(kv.Key, out var l) && l > 0.5f;
                bool on = FightCues.Jolt && fig && !lifted && !host.Paused;
                var off = Vector3.zero;
                if (on && hit < v.knockTime && v.knockTime > 0f)
                {
                    float inS = v.knockTime * 0.12f, u = hit < inS ? hit / inS : 1f - (hit - inS) / (v.knockTime - inS);
                    off += j.hitDir * (v.knockPx / PxPerM * (hit < inS ? u : u * u));
                }
                if (on && kick < KickS) off -= j.kickDir * (v.kickPx / PxPerM * (1f - kick / KickS));
                if (off != Vector3.zero) f.model.position += off;
                bool white = FightCues.Jolt && fig && hit < v.whiteTime;
                if (white != j.white) White(j, f, white);
                if (!white && hit >= v.knockTime && kick >= KickS) joltsDone.Add(kv.Key);
            }
            foreach (var k in joltsDone) jolts.Remove(k);
            FightCues.CostTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        }

        /// <summary>The body white, or back: every renderer of the figure (each LOD) on one shared white material. A Lit one, so he keeps his shadow;
        /// its emission lifts him to white in the shade. Nothing per frame but the two swaps.</summary>
        void White(Jolt j, Fig f, bool on)
        {
            j.white = on; if (f == null || !f.model) return;
            if (j.model != f.model) { j.model = f.model; j.rends = f.model.GetComponentsInChildren<Renderer>(true); j.mats = new Material[j.rends.Length][]; for (int i = 0; i < j.rends.Length; i++) j.mats[i] = j.rends[i].sharedMaterials; }
            if (!whiteMat)
            {
                whiteMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "cue-white" };
                whiteMat.SetColor("_BaseColor", Color.white); whiteMat.SetFloat("_Smoothness", 0f); whiteMat.EnableKeyword("_EMISSION"); whiteMat.SetColor("_EmissionColor", Color.white * 0.75f);
            }
            for (int i = 0; i < j.rends.Length; i++)
            {
                var r = j.rends[i]; if (!r) continue;
                if (!on) { r.sharedMaterials = j.mats[i]; continue; }
                var w = new Material[j.mats[i].Length]; for (int s = 0; s < w.Length; s++) w[s] = whiteMat; r.sharedMaterials = w;
            }
        }
    }
}
