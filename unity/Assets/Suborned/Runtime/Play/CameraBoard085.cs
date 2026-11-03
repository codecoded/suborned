using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using OpId = Suborned.Sim.OpId;
using Order = Suborned.Sim.Order;

namespace Suborned.Play
{
    /// <summary>`-cameraBoard &lt;dir&gt;` (brief 085, Sky: "is this the right camera angle ... Feels a little too high up"): stills for a board and nothing else. The same three spots at the same paused moment
    /// (route A at 1:20), in the real player's look with the HUD, through the camera lab's values (CamPreset): today, orthographic 10 at 32°, orthographic 8 at 38°, perspective FOV 30 at a distance that
    /// keeps a figure about 90 px at 38°, perspective FOV 30 at 45°, perspective FOV 40 at 38°. Writes board.json (the figure's pixels and the triangles each) and quits. Nothing in play changes.</summary>
    public sealed class CameraBoard085 : MonoBehaviour
    {
        string dir;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            var d = GameBoot.ArgValue("-cameraBoard"); if (d == null) return;
            var go = new GameObject("camera board"); DontDestroyOnLoad(go); go.AddComponent<CameraBoard085>().dir = d;
        }

        static CamPreset Preset(string name, bool persp, float zoom, float pitch, float fov) { var p = new CamPreset { name = name }; p.overhead.perspective = persp; p.overhead.zoom = zoom; p.overhead.zoomMin = 4f; p.overhead.zoomMax = 40f; p.overhead.pitch = pitch; p.overhead.fov = fov; return p; }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            HandoverHost h = null; while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) yield return null;
            yield return new WaitForSecondsRealtime(3f);
            var cs = CaptureSize.Parse(GameBoot.ArgValue("-capture-size")) ?? CaptureSize.Default; yield return CaptureSize.Apply(cs.x, cs.y);
            h.controls.conversations = ConversationScenes.Never; h.controls.planTogether = false; h.Begin(); h.IntelOpen = false; h.ShowBriefing = false; h.StartClock(); h.SetPaused(true);
            var g = h.Game; var path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), "contracts", "missions", "handover", "routes", "route-a.json");
            foreach (var o in Suborned.Sim.Script.Parse(File.ReadAllText(path)).Orders) { if (o.Tick >= 800) break; if (o.Tick > g.Tick) h.RunTo(o.Tick); if (o.Do is "watch" or "listen" or "turn" && o.Op != "cell") h.GiveScripted(new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach }); else h.Issue(new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run }); }
            h.RunTo(800); h.Select(OpId.Scout, false); yield return null;
            var spots = new (string id, string label, Vector3 at, int turn)[]
            {
                ("door", "Records Office door, yaw 210", h.Views.Find("guard").position, 2),
                ("cafe", "Café terrace, yaw 30", h.Views.Find("waitress").position, 0),
                ("corner", "Street corner by the cabinet, flat paving, yaw 210", h.Views.Find("op/tech").position + new Vector3(2f, 0f, -2f), 2),
            };
            var views = new[]
            {
                ("today", "Today: orthographic 10, 38°", Preset("today", false, 10f, 38f, 30f)),
                ("ortho10-32", "Orthographic 10, 32°", Preset("ortho10-32", false, 10f, 32f, 30f)),
                ("ortho8-38", "Orthographic 8, 38°", Preset("ortho8-38", false, 8f, 38f, 30f)),
                ("persp30-38", "Perspective FOV 30, 38°", Preset("persp30-38", true, 8.9f, 38f, 30f)),
                ("persp30-45", "Perspective FOV 30, 45°", Preset("persp30-45", true, 8.9f, 45f, 30f)),
                ("persp40-38", "Perspective FOV 40, 38°", Preset("persp40-38", true, 8.9f, 38f, 40f)),
            };
            var rows = new List<string>(); var tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1); var scout = h.Views.Find("guard");
            foreach (var (sid, slabel, at, turn) in spots)
                foreach (var (vid, vlabel, preset) in views)
                {
                    h.TurnSnap(turn); h.CamUse(preset); h.TurnSnap(turn); h.LookAt(at); for (int i = 0; i < 8; i++) yield return null; yield return new WaitForSecondsRealtime(0.25f);
                    var cam = h.View; var feet = cam.WorldToScreenPoint(at); var head = cam.WorldToScreenPoint(at + Vector3.up * 1.8f); float px = Mathf.Abs(head.y - feet.y);
                    long t = tris.Valid ? tris.LastValue : -1; var name = $"{sid}-{vid}"; while (HudToolkit.ShotBusy) yield return null; var said = HudToolkit.Shoot(Path.Combine(dir, name), "rest"); yield return null; while (HudToolkit.ShotBusy) yield return null;
                    rows.Add($"{{\"spot\": \"{sid}\", \"spotLabel\": \"{slabel}\", \"view\": \"{vid}\", \"viewLabel\": \"{vlabel}\", \"file\": \"{name}.png\", \"figurePx\": {px.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}, \"triangles\": {t}, \"camY\": {cam.transform.position.y.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}, \"tick\": {g.Tick}}}");
                }
            tris.Dispose(); h.CamUse(new CamPreset());
            File.WriteAllText(Path.Combine(dir, "board.json"), "[\n" + string.Join(",\n", rows) + "\n]\n");
            Application.Quit(0);
        }
    }
}
