using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 096 scratch check (`-viewTrace dir`, a person's player only; nothing else starts it): the café on the map and in third person
    /// (the same agent, the same moment), the way in and the way back logged per frame (trace.csv: the frame's half-height at the agent's ground
    /// point, from the camera's own projection, so the blended glide counts too), stills along the way back, then Options' free zoom at 3 and 60.
    /// The way back must never show more than the map's half-height (no zoom-out) and must end on it (no snap).</summary>
    public sealed class ViewTrace096 : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || GameBoot.ArgValue("-viewTrace") is not string dir) return;
            Application.runInBackground = true; var go = new GameObject("view-trace-096"); DontDestroyOnLoad(go); go.AddComponent<ViewTrace096>().dir = dir;
        }

        string dir; HandoverHost h; Camera cam; Transform agent; readonly StringBuilder csv = new("frame,phase,t,ortho,fov,halfH,dist,agentVx,agentVy,standOff,shadowDist\n"); int shot; string phase = "";

        void Row()
        {
            var g = agent ? new Vector3(agent.position.x, 0f, agent.position.z) : h.Focus;
            var a = cam.WorldToViewportPoint(g); var b = cam.WorldToViewportPoint(g + cam.transform.up);
            float halfH = Mathf.Abs(b.y - a.y) > 1e-6f ? 0.5f / Mathf.Abs(b.y - a.y) : float.NaN;
            var so = h.GetComponent<PlayStandOff>(); var urp = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
            csv.Append($"{Time.frameCount},{phase},{Time.unscaledTime:0.000},{(cam.orthographic ? 1 : 0)},{cam.fieldOfView:0.0},{halfH:0.00},{Vector3.Distance(cam.transform.position, g):0.0},{a.x:0.000},{a.y:0.000},{(so ? so.Now : 0f):0.0},{(urp ? urp.shadowDistance : 0f):0.0}\n");
        }
        IEnumerator Frames(int n) { Debug.Log($"ViewTrace096: {phase}"); for (int i = 0; i < n; i++) { yield return null; Row(); } }
        IEnumerator Shot(string name) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"{++shot:00}-{name}.png")); yield return null; }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir); Debug.Log("ViewTrace096: start " + dir);
            float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null || !h.View) { if (Time.realtimeSinceStartup - t0 > 60f) { Debug.LogError("ViewTrace096: no host"); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f);
            cam = h.View; Debug.Log("ViewTrace096: host up");
            h.Begin(); h.IntelOpen = false; yield return null; h.StartClock(); h.SetPaused(true);
            // the agent nearest the waitress (the café's front)
            var w = h.Views.Find("waitress"); int best = 0; float bd = float.MaxValue;
            for (int k = 0; k < 4; k++) if (h.Views.Find("op/" + Suborned.Sim.Op.Name((Suborned.Sim.OpId)k)) is Transform t) { float d = w ? (t.position - w.position).sqrMagnitude : k; if (d < bd) { bd = d; best = k; } }
            agent = h.Views.Find("op/" + Suborned.Sim.Op.Name((Suborned.Sim.OpId)best));
            h.Select((Suborned.Sim.OpId)best, false);
            h.TurnSnap(0); cam.orthographicSize = Mathf.Clamp(h.Cam.overhead.zoom, h.zoomMin, h.zoomMax); h.LookAt(agent.position);
            phase = "map"; yield return Frames(60); yield return Shot("map-cafe");
            phase = "enter"; h.DriveEnter083(); Row();
            for (int i = 0; i < 40; i++) { yield return null; Row(); if (i == 10) yield return Shot("enter-mid"); }
            phase = "third"; yield return Frames(60); yield return Shot("third-cafe");
            phase = "exit"; h.BackToMap(); Row();
            float te = Time.unscaledTime; int n = 0;
            while ((h.Following || h.FollowExiting) && Time.unscaledTime - te < 3f) { yield return null; Row(); n++; if (n == 8 || n == 18) yield return Shot($"exit-{n}"); }
            phase = "after"; yield return Frames(45); yield return Shot("map-after");
            // the same way in and back again with no captures (a capture stalls a frame and the 0.5 s glides run on the clock): the clean curves
            phase = "enter2"; h.DriveEnter083(); Row(); yield return Frames(45);
            phase = "third2"; yield return Frames(30);
            phase = "exit2"; h.BackToMap(); Row(); te = Time.unscaledTime;
            while ((h.Following || h.FollowExiting) && Time.unscaledTime - te < 3f) { yield return null; Row(); }
            phase = "after2"; yield return Frames(20);
            // Options, Camera, Free zoom (debug): in memory only (no Save), the wheel's range through CamApply's clamp
            Settings.Current.camera.freeZoom = true;
            phase = "zoom3"; cam.orthographicSize = 3f; h.CamApply(); yield return Frames(45); yield return Shot("free-zoom-3");
            phase = "zoom60"; cam.orthographicSize = 60f; h.CamApply(); yield return Frames(45); yield return Shot("free-zoom-60");
            Settings.Current.camera.freeZoom = false; phase = "zoomOff"; h.CamApply(); yield return Frames(5);
            File.WriteAllText(Path.Combine(dir, "trace.csv"), csv.ToString());
            Debug.Log($"ViewTrace096: written to {dir}");
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit(0);
        }
    }
}
