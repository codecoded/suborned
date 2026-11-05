using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Path = System.IO.Path;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 096 probe (`-hitchTrace dir`, a probe's player only; nothing else starts it): the first switch to each perspective view, timed
    /// frame by frame with no captures (a capture stalls a frame). It waits for the host and the shader warm-up (ShaderPrewarm, as the loading bar
    /// does), then: the map at the four yaws; third person (T, the drive: the agent nearest Inez taken), walked forward, back to the map, and in and
    /// out again (the warm second time); a conversation cutaway from the map (route A's opening up to the first scene, conversations Always).
    /// `-hitchOrder convo` plays the cutaway before third person, so each view's first use can be timed on its own; `-hitchStop map` stops after the opening map view, before any turn (a recording of the play view as the street opens). Engine steps the probe runs to
    /// reach the scene are phase "setup" and not judged. Writes frames.csv (frame, phase, ms, gc) and hitch.json (max ms per phase, the warm-up's
    /// seconds); the frames while the warm-up runs are phase "warming"; with `-gscTrace file` the recording is saved at the end (ShaderPrewarm).</summary>
    public sealed class HitchTrace096 : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || GameBoot.ArgValue("-hitchTrace") is not string dir) return;
            Application.runInBackground = true; var go = new GameObject("hitch-trace-096"); DontDestroyOnLoad(go); go.AddComponent<HitchTrace096>().dir = dir;
        }

        /// <summary>`-hitchShaderLog` (a development player): Unity logs each shader program as it creates it, so the log shows what each phase compiled.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ShaderLog() { if (!Application.isEditor && GameBoot.Arg("-hitchShaderLog")) UnityEngine.Rendering.GraphicsSettings.logWhenShaderIsCompiled = true; }

        string dir, phase = "boot"; HandoverHost h; double last; int gc0;
        readonly StringBuilder csv = new("frame,phase,ms,gc\n");
        readonly List<(string phase, int frames, double max, int gc)> phases = new();
        readonly Dictionary<string, (int n, double max, int gc)> acc = new();

        void Row()
        {
            double now = Time.realtimeSinceStartupAsDouble, ms = (now - last) * 1000.0; last = now; int gc = GC.CollectionCount(0) - gc0;
            csv.Append($"{Time.frameCount},{phase},{ms:0.0},{gc}\n");
            acc.TryGetValue(phase, out var a); acc[phase] = (a.n + 1, Math.Max(a.max, ms), gc);
        }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) { yield return null; Row(); } }
        void Phase(string p) { Close(); phase = p; Debug.Log($"HitchTrace096: {p}"); }
        void Close() { if (acc.TryGetValue(phase, out var a)) phases.Add((phase, a.n, a.max, a.gc)); acc.Remove(phase); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir); Debug.Log("HitchTrace096: start " + dir);
            float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null || !h.View) { if (Time.realtimeSinceStartup - t0 > 90f) { Debug.LogError("HitchTrace096: no host"); yield break; } yield return null; }
            float tw = Time.realtimeSinceStartup; last = Time.realtimeSinceStartupAsDouble; phase = "warming"; while (!ShaderPrewarm.Done && Time.realtimeSinceStartup - tw < 90f) { yield return null; Row(); } Close(); // the frames while it warms: the menu's and the Job page's
            Debug.Log($"HitchTrace096: host up {tw - t0:0.00} s after start; warm-up done {Time.realtimeSinceStartup - tw:0.00} s later ({ShaderPrewarm.Seconds:0.00} s from its begin)");
            yield return new WaitForSecondsRealtime(3f);
            gc0 = GC.CollectionCount(0); last = Time.realtimeSinceStartupAsDouble;
            h.Begin(); h.IntelOpen = false; yield return null; h.StartClock(); h.SetPaused(true); yield return null; Row();

            Phase("map"); h.TurnSnap(0); yield return Frames(60);
            bool convoFirst = GameBoot.ArgValue("-hitchOrder") == "convo", mapOnly = GameBoot.ArgValue("-hitchStop") == "map"; // map: the opening play view only (the base the shipped warm-up leaves out)
            if (!mapOnly) { for (int k = 1; k < 4; k++) { Phase($"map-yaw{k}"); h.TurnSnap(k); yield return Frames(30); } Phase("map-back"); h.TurnSnap(0); yield return Frames(30); }
            if (!mapOnly) { if (convoFirst) yield return Convo(); yield return Third(); if (!convoFirst) yield return Convo(); }
            Close();

            var j = new StringBuilder("{\n  \"schema\": \"suborned.hitch096/1\",\n");
            j.Append($"  \"order\": \"{(convoFirst ? "convo" : "third")}\", \"prewarmS\": {ShaderPrewarm.Seconds:0.00},\n  \"phases\": [\n");
            j.Append(string.Join(",\n", phases.Select(p => $"    {{\"phase\": \"{p.phase}\", \"frames\": {p.frames}, \"maxMs\": {p.max:0.0}, \"gc\": {p.gc} }}")));
            j.Append("\n  ]\n}\n");
            File.WriteAllText(Path.Combine(dir, "hitch.json"), j.ToString()); File.WriteAllText(Path.Combine(dir, "frames.csv"), csv.ToString());
            foreach (var p in phases) Debug.Log($"HitchTrace096: {p.phase,-12} {p.frames,4} frames, max {p.max,7:0.0} ms, gc {p.gc}");
            ShaderPrewarm.TraceSave();
            Debug.Log($"HitchTrace096: written to {dir}");
            yield return new WaitForSecondsRealtime(0.5f);
            GameBoot.Leave(0);
        }

        /// <summary>Third person (T, the drive), the agent nearest Inez; walked forward; back; in and out again.</summary>
        IEnumerator Third()
        {
            h.Select(Nearest("waitress"), false); yield return null; Row();
            Phase("third"); h.DriveEnter083(); yield return Frames(90);
            Phase("walk"); h.SetPaused(false); h.DriveInputOverride = new Vector2(0f, 1f); yield return Frames(90); h.DriveInputOverride = null; h.SetPaused(true);
            Phase("third-back"); h.BackToMap(); yield return Frames(60);
            Phase("third2"); h.DriveEnter083(); yield return Frames(60);
            Phase("third2-back"); h.BackToMap(); yield return Frames(60);
            // the pickup's side: the agent nearest the driver, in, walked, back (more of the street for the recording)
            h.Select(Nearest("driver"), false); yield return null; Row();
            Phase("third-pickup"); h.DriveEnter083(); yield return Frames(60);
            Phase("walk-pickup"); h.SetPaused(false); h.DriveInputOverride = new Vector2(0.3f, 1f); yield return Frames(90); h.DriveInputOverride = null; h.SetPaused(true);
            Phase("pickup-back"); h.BackToMap(); yield return Frames(60);
        }

        /// <summary>The cell's agent standing nearest a person (by view key).</summary>
        OpId Nearest(string person)
        {
            var w = h.Views.Find(person); int best = 0; float bd = float.MaxValue;
            for (int k = 0; k < 4; k++) if (h.Views.Find("op/" + Op.Name((OpId)k)) is Transform t) { float d = w ? (t.position - w.position).sqrMagnitude : k; if (d < bd) { bd = d; best = k; } }
            return (OpId)best;
        }

        /// <summary>A route of the Handover's, found from the app up to the repo (a scratch build sits deeper than out/build/play).</summary>
        static string RouteFile(string name)
        {
            for (var d = new DirectoryInfo(Application.dataPath); d != null; d = d.Parent) { var f = Path.Combine(d.FullName, "contracts", "missions", "handover", "routes", name + ".json"); if (File.Exists(f)) return f; }
            throw new FileNotFoundException($"no contracts/missions/handover/routes/{name}.json above {Application.dataPath}");
        }

        /// <summary>Route A's orders from the start until the first conversation lands (engine steps: "setup"); then the cutaway, timed; then skipped.</summary>
        IEnumerator Convo()
        {
            var keep = h.controls.conversations; h.controls.conversations = ConversationScenes.Always;
            List<Order> route = null;
            try { route = Script.Parse(File.ReadAllText(RouteFile("route-a"))).Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList(); }
            catch (Exception e) { Debug.LogError("HitchTrace096: route-a: " + e.Message); }
            var g = h.Game; Phase("setup"); bool hit = false;
            if (route != null)
                foreach (var o in route)
                {
                    while (g.Tick < o.Tick && !g.Over && !hit) { h.RunTo(g.Tick + 1); hit = h.ConvoPending || h.ConvoOn; if (g.Tick % 20 == 0) { yield return null; Row(); } }
                    if (hit || g.Over) break;
                    var c = new Order { Tick = g.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run };
                    if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c);
                }
            for (int i = 0; i < 3000 && !hit && !g.Over; i++) { h.RunTo(g.Tick + 1); hit = h.ConvoPending || h.ConvoOn; if (i % 20 == 0) { yield return null; Row(); } }
            if (!hit) { Debug.LogError($"HitchTrace096: no conversation ({h.ConvoLast})"); h.controls.conversations = keep; yield break; }
            yield return null; Row();
            Phase("convo"); yield return Frames(120);
            Debug.Log($"HitchTrace096: convo {h.ConvoLast}");
            Phase("convo-back"); h.ConvoSkip(); yield return Frames(60);
            h.ConvoEndNow(); h.controls.conversations = keep;
        }
    }
}
