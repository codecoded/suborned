using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;

namespace Suborned.Play
{
    /// <summary>Brief 086 (Redline 1.3 item 3): per-pass GPU time for a `-tplong` row that asks (variant.gpuSplit "on"), through Recorder.gpuElapsedNanoseconds, as far as this player gives it. Writes gpu-&lt;row&gt;.json: whether
    /// the platform has GPU recorders, how many samplers the player names, every render-pass sampler that answered and its mean ms a frame. A sampler with no GPU blocks reads -1, so "not available" never reads as zero.</summary>
    public static class TpLongGpu
    {
        static readonly Regex Passes = new("shadow|opaque|transparent|bloom|smaa|final|blit|uber|post|depth|ssao|skybox|ui|overlay|render ?graph|draw", RegexOptions.IgnoreCase);

        public static IEnumerator Sample(string row, float seconds, string dir)
        {
            var names = new List<string>(); Sampler.GetNames(names);
            var pick = names.Where(n => Passes.IsMatch(n)).Distinct().Take(200).ToList();
            var recs = new List<(string name, Recorder r)>(); foreach (var n in pick) { var r = Recorder.Get(n); if (r != null && r.isValid) { r.enabled = true; recs.Add((n, r)); } }
            var gpu = new double[recs.Count]; var cpu = new double[recs.Count]; var blocks = new long[recs.Count]; int frames = 0; float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < seconds)
            {
                yield return null; frames++;
                for (int i = 0; i < recs.Count; i++) { gpu[i] += recs[i].r.gpuElapsedNanoseconds; blocks[i] += recs[i].r.gpuSampleBlockCount; cpu[i] += recs[i].r.elapsedNanoseconds; }
            }
            foreach (var (_, r) in recs) r.enabled = false;
            int n1 = System.Math.Max(1, frames);
            var rows = Enumerable.Range(0, recs.Count).Where(i => blocks[i] > 0 || cpu[i] > 0).OrderByDescending(i => gpu[i]).Select(i => $"  {{\"name\": \"{recs[i].name.Replace("\"", "'")}\", \"gpuMs\": {(blocks[i] > 0 ? (gpu[i] / n1 / 1e6).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) : "-1")}, \"cpuMs\": {(cpu[i] / n1 / 1e6).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)}, \"gpuBlocks\": {blocks[i] / n1}}}");
            File.WriteAllText(Path.Combine(dir, $"gpu-{row}.json"), $"{{\n \"schema\": \"suborned.tplong.gpu/1\", \"row\": \"{row}\", \"supportsGpuRecorder\": {(SystemInfo.supportsGpuRecorder ? "true" : "false")}, \"debugBuild\": {(Debug.isDebugBuild ? "true" : "false")}, \"frames\": {frames}, \"samplersNamed\": {names.Count}, \"samplersTried\": {recs.Count},\n \"touched\": \"{TpLongVariants.Touched.Replace("\"", "'")}\",\n \"passes\": [\n{string.Join(",\n", rows)}\n ]\n}}\n");
        }
    }
}
