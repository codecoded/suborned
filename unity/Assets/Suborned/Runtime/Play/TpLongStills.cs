using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 106: STP's readability stills for a `-tplong` row that asks (variant.stills "on"), PNG beside the report:
    /// - `<row>.still.stp-off.png` and `.stp-on.png`: the same frame. The game is paused (the row's own pause after its window), STP off, then on at the
    ///   row's STP scale (or the default) once its history has settled, so only the upscaler differs.
    /// - `<row>.moving.stp-off.png` and `.stp-on.png`: the clock running, each half a second after its switch: not the same frame, but the same view in
    ///   motion, where a temporal upscaler would leave trails behind people, cones and the guard's mark.
    /// The row's switches are put back after. Writes nothing else; the pictures are judged by eye beside each other (briefs/106/latch/results.md).</summary>
    public static class TpLongStills
    {
        const int SettleFrames = 45;

        public static IEnumerator Pair(HandoverHost h, string dir, string row, float scale)
        {
            bool was = BuiltIns.Stp; float wasScale = BuiltIns.StpScale; float s = scale > 0f ? scale : BuiltIns.DefaultStpScale;
            yield return Two(dir, row + ".still", s);
            h.SetPaused(false);
            yield return Two(dir, row + ".moving", s, 0.5f);
            h.SetPaused(true);
            BuiltIns.SetStp(was, wasScale);
            yield return null;
        }

        static IEnumerator Two(string dir, string name, float scale, float wait = 0f)
        {
            BuiltIns.SetStp(false, scale); yield return Settle(wait); yield return Capture(Path.Combine(dir, name + ".stp-off.png"));
            BuiltIns.SetStp(true, scale); yield return Settle(wait); yield return Capture(Path.Combine(dir, name + ".stp-on.png"));
        }

        static IEnumerator Settle(float wait)
        {
            if (wait > 0f) { yield return new WaitForSecondsRealtime(wait); yield break; }
            for (int i = 0; i < SettleFrames; i++) yield return null; // RenderCap applies the scale next frame; STP's history then fills
        }

        static IEnumerator Capture(string file)
        {
            yield return new WaitForEndOfFrame();
            var t = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(file, t.EncodeToPNG()); Debug.Log($"TpLongStills: {Path.GetFileName(file)} {t.width}x{t.height}, {BuiltIns.Describe()}"); }
            catch (Exception e) { Debug.LogError("TpLongStills: " + file + ": " + e.Message); }
            UnityEngine.Object.Destroy(t);
        }
    }
}
