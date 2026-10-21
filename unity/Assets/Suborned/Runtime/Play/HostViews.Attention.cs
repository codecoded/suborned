using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 031 (Glyph's cue board briefs/031/cues.html §1-§4): each security watcher shows its displayed attention (HandoverHost.Cue)
    /// on itself, drawn unlit and emissive, each stage with its own motion so colour is never alone:
    ///  · a camera: calm the lens dim; noticed the iris glows and the head nods one small arc toward the agent (the cone does not move), then
    ///    still; wary the iris pulses (1.6 s); reporting the lens flashes (4 Hz);
    ///  · a robot: the old city's robots on their own displays (RobotFace.Stage: Vela's attention glyphs), the patrol robot on its stage face
    ///    (RobotFaceStage); noticed the eyes turn to the agent and widen, wary narrowed and the head tracks, reporting the alarm pictogram;
    ///  · a person (the guard, the officers): noticed the head turns to the agent and holds, wary and reporting it tracks (a procedural head
    ///    yaw after the clip, until Forma's head clip).
    /// A cue shows only once the engine has the agent in the watcher's sight, so nothing here draws a cone the cell hasn't learned. The
    /// callouts' close-ups render the watcher alone (InsetFrame: one small camera a callout, its own layer, a cream ground). View only.</summary>
    public sealed partial class HostViews
    {
        // ---------------------------------------------------------------- cameras: a head with a lens on the mission's poles

        sealed class CamHead { public Transform Head; public Material Lens; public Quaternion Rest; public float Nod; public Vector3 Scale; public int Shown; }
        readonly Dictionary<string, CamHead> camHeads = new();
        static readonly Color LensDim = new(0.09f, 0.12f, 0.13f), Phosphor = new(0.75f, 0.95f, 0.9f), AlarmLens = new(1f, 0.35f, 0.24f);

        /// <summary>The pole's head (housing, hood and lens) at 3.25 m, looking along the camera's facing, tilted down to its ground.</summary>
        void CamHeadFor(Suborned.Sim.Camera c, Vector3 at)
        {
            if (c.World) return; // the world's own masts draw themselves
            var fwd = frame.Dir(c.Facing); if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
            var head = new GameObject("camera/" + c.Id + "/head").transform; head.SetParent(root, false);
            head.position = at + Vector3.up * 3.25f; head.rotation = Quaternion.LookRotation((fwd + Vector3.down * 0.3f).normalized, Vector3.up);
            Part(head, "housing", cube, Lit(host.carColour), new Vector3(0f, 0f, 0.02f), new Vector3(0.24f, 0.22f, 0.5f));
            Part(head, "hood", cube, Lit(new Color(0.42f, 0.45f, 0.48f)), new Vector3(0f, 0.13f, 0.08f), new Vector3(0.3f, 0.035f, 0.62f));
            var lens = Unlit(LensDim);
            var l = Part(head, "lens", sphere, lens, new Vector3(0f, 0f, 0.28f), new Vector3(0.15f, 0.15f, 0.05f));
            foreach (var r in head.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            camHeads["camera/" + c.Id] = new CamHead { Head = head, Lens = lens, Rest = head.rotation, Scale = l.localScale };
        }

        void CamCue(CamHead h, int stage, Vector3? agent, float t, float dt)
        {
            Color c = stage switch
            {
                1 => Phosphor * 1.4f,
                2 => Color.Lerp(Phosphor * 0.45f, Phosphor * 1.6f, 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 1.6f)),
                3 => Mathf.Repeat(t * 4f, 1f) < 0.5f ? AlarmLens * 2.2f : LensDim,
                _ => LensDim,
            };
            c.a = 1f; h.Lens.SetColor("_BaseColor", c); h.Shown = stage;
            var lens = h.Head.Find("lens"); if (lens) lens.localScale = stage == 3 && Mathf.Repeat(t * 4f, 1f) < 0.5f ? h.Scale * 1.35f : h.Scale; // the flash's burst
            // the nod: one small arc toward the agent (at most 10 deg), held while it has him; back to rest when calm
            float want = 0f;
            if (stage >= 1 && agent.HasValue)
            {
                var d = agent.Value - h.Head.position; d.y = 0f; var f = h.Rest * Vector3.forward; f.y = 0f;
                if (d.sqrMagnitude > 0.01f && f.sqrMagnitude > 1e-6f) want = Mathf.Clamp(Vector3.SignedAngle(f, d, Vector3.up), -10f, 10f);
            }
            h.Nod = Mathf.MoveTowards(h.Nod, want, 40f * dt);
            h.Head.rotation = Quaternion.AngleAxis(h.Nod, Vector3.up) * h.Rest;
        }

        // ---------------------------------------------------------------- robots

        readonly Dictionary<string, (Suborned.Robots.RobotFaceStage face, Suborned.Robots.RobotIdle idle)> stageRobots = new();

        /// <summary>A robot model with the stage face (the patrol robot: M_RobotFace): its face driver and a servo head that glances, from the
        /// old robots' numbers (robot-faces.json: range 35, turn 0.7 s, hold 2-5 s, home 0.5).</summary>
        void StageRobot(string key, GameObject inst)
        {
            var face = inst.AddComponent<Suborned.Robots.RobotFaceStage>(); face.Seed = key;
            if (face.Screens == 0) { Object.Destroy(face); return; }
            var idle = inst.AddComponent<Suborned.Robots.RobotIdle>(); idle.Configure(null, 35f, 0.7f, new Vector2(2f, 5f), 0.5f, 0.15f, 2);
            stageRobots[key] = (face, idle);
        }

        /// <summary>The eyes' shift toward a point, in cells (a turn to the robot's right moves its eyes to the viewer's left).</summary>
        static int GazeCells(Transform robot, Vector3 at, int cells)
        {
            var d = robot.InverseTransformPoint(at); float a = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            return -Mathf.RoundToInt(Mathf.Clamp(a / 35f, -1f, 1f) * cells);
        }

        // ---------------------------------------------------------------- people: the head

        sealed class HeadTurn { public float Yaw; public Vector3? Hold; public int Stage; }
        readonly Dictionary<string, HeadTurn> heads = new();

        /// <summary>A restart: the views are new; nothing carries over.</summary>
        public void ResetAttention() { heads.Clear(); }

        Vector3? AgentAt(string op)
        {
            if (op.Length == 0) return null; int k = (int)Op.Parse(op);
            var a = things[OpKeys[k]]; return a.shown ? a.t.position + Vector3.up * 1.2f : (Vector3?)null;
        }

        /// <summary>Per frame, from Frame: every watcher's cue on itself.</summary>
        void AttentionViews()
        {
            var g = host.Game; if (g == null || g.Range) return;
            float t = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            foreach (var kv in camHeads)
            {
                var (st, op) = host.Cue("camera:" + kv.Key.Substring(7));
                if (st == 0 && kv.Value.Nod == 0f && kv.Value.Shown == 0) continue;
                CamCue(kv.Value, st, st > 0 ? AgentAt(op) : null, t, dt);
            }
            foreach (var kv in stageRobots)
            {
                var (face, idle) = kv.Value; if (!face) continue;
                string w = kv.Key == "patrol" ? "patrol" : kv.Key.StartsWith("unit/") ? kv.Key.Substring(5) : kv.Key;
                var (st, op) = host.Cue(w); var at = st > 0 ? AgentAt(op) : null;
                face.Stage(st, at.HasValue ? GazeCells(face.transform, at.Value, 1) : 0);
                idle.Attend(st >= 2 && op.Length > 0 ? things[OpKeys[(int)Op.Parse(op)]].t : null, Time.time);
            }
            foreach (var kv in robotHeads)
            {
                var (idle, _) = kv.Value; if (!idle || !idle.Face) continue;
                string w = kv.Key == "patrol" ? "patrol" : kv.Key.StartsWith("unit/") ? kv.Key.Substring(5) : kv.Key;
                var (st, op) = host.Cue(w); var at = st > 0 ? AgentAt(op) : null;
                idle.Face.Stage(st, at.HasValue ? GazeCells(idle.transform, at.Value, 2) : 0);
                idle.Attend(st >= 2 && op.Length > 0 ? things[OpKeys[(int)Op.Parse(op)]].t : null, Time.time);
            }
        }

        /// <summary>From the host's LateUpdate, after the clips have posed the figures: the guard's and the officers' heads on the agent they
        /// have (noticed: turned and held; wary and reporting: tracking), eased at 240 deg/s, within 70 deg of the body's facing.</summary>
        public void LateHeads()
        {
            var g = host.Game; if (g == null || g.Range) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            foreach (var key in PersonWatchers)
            {
                if (!figs.TryGetValue(key, out var f) || !things.TryGetValue(key, out var th) || !th.shown) continue;
                string w = key == "guard" ? "guard" : WatcherOf(key, g); if (w == null) continue;
                var (st, op) = host.Cue(w);
                if (!heads.TryGetValue(key, out var h)) heads[key] = h = new HeadTurn();
                var at = st > 0 ? AgentAt(op) : null;
                if (st == 1 && h.Stage != 1) h.Hold = at; // noticed: the turn, then held
                if (st != 1) h.Hold = null;
                h.Stage = st;
                var aim = st == 1 ? h.Hold ?? at : at;
                float want = 0f;
                var bone = Bone(f, HumanBodyBones.Head, "Head"); if (!bone) continue;
                if (aim.HasValue)
                {
                    var d = aim.Value - bone.position; d.y = 0f; var fw = f.model.forward; fw.y = 0f;
                    if (d.sqrMagnitude > 0.01f && fw.sqrMagnitude > 1e-6f) want = Mathf.Clamp(Vector3.SignedAngle(fw, d, Vector3.up), -70f, 70f);
                }
                h.Yaw = Mathf.MoveTowardsAngle(h.Yaw, want, 240f * dt);
                if (Mathf.Abs(h.Yaw) > 0.01f) bone.rotation = Quaternion.AngleAxis(h.Yaw, Vector3.up) * bone.rotation;
            }
        }

        static readonly string[] PersonWatchers = { "guard", "security/0", "security/1" };
        static string WatcherOf(string key, Game g) { int i = key == "security/0" ? 0 : key == "security/1" ? 1 : -1; return i >= 0 && i < g.SecGuns.Length ? g.SecGuns[i].Id : null; }

        /// <summary>The head yaw the attention has given a person's figure now (deg; 0 for none): the inset faces it.</summary>
        float HeadYaw(string key) => heads.TryGetValue(key, out var h) ? h.Yaw : 0f;

        // ---------------------------------------------------------------- the callouts' close-ups (board §9: a real camera inset)

        sealed class Inset { public UnityEngine.Camera Cam; public RenderTexture Rt; public string View = ""; public readonly List<(Renderer r, int layer)> Moved = new(); }
        readonly Inset[] insets = new Inset[4];
        /// <summary>The layers the insets draw on (unnamed in the project, so nothing else lives there); the play camera keeps drawing them.</summary>
        public const int InsetLayer0 = 24;
        static readonly Color InsetGround = new(0.969f, 0.957f, 0.925f);

        /// <summary>The close-up of `view` into slot k's texture (128 px, the watcher alone on cream), or none (null view): its camera on only
        /// while a callout is up.</summary>
        public RenderTexture InsetFrame(int k, string view)
        {
            var s = insets[k] ??= NewInset(k);
            if (view != s.View) { Restore(s); s.View = view ?? ""; if (s.View.Length > 0 && things.TryGetValue(s.View, out var th)) { Move(s, th.t, InsetLayer0 + k); if (camHeads.TryGetValue(s.View, out var hh)) Move(s, hh.Head, InsetLayer0 + k); } }
            bool on = s.View.Length > 0 && things.TryGetValue(s.View, out var x) && x.shown && x.t.gameObject.activeInHierarchy;
            if (s.Cam.enabled != on) s.Cam.enabled = on;
            if (!on) return null;
            // frame the watcher's face: the camera head's lens, a robot's display, a person's head
            Vector3 at, fwd; float size;
            if (camHeads.TryGetValue(s.View, out var ch)) { at = ch.Head.position; fwd = ch.Head.forward; size = 0.34f; }
            else if (figs.TryGetValue(s.View, out var f) && Bone(f, HumanBodyBones.Head, "Head") is Transform hb) { at = hb.position + Vector3.up * 0.02f; fwd = Quaternion.AngleAxis(HeadYaw(s.View), Vector3.up) * f.model.forward; size = 0.3f; }
            else
            {
                var t = things[s.View].t; Renderer screen = null;
                foreach (var r in t.GetComponentsInChildren<Renderer>()) if (r.name.EndsWith("_Face")) { screen = r; break; }
                var headT = screen ? screen.transform : t;
                at = screen ? screen.bounds.center : t.position + Vector3.up * 1.1f;
                fwd = headT.forward; if (Mathf.Abs(fwd.y) > 0.9f) fwd = t.forward;
                size = 0.32f;
            }
            fwd.y = Mathf.Clamp(fwd.y, -0.3f, 0.3f); if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward; fwd.Normalize();
            var cpos = at + fwd * 2.2f + Vector3.up * 0.25f;
            s.Cam.transform.SetPositionAndRotation(cpos, Quaternion.LookRotation(at - cpos, Vector3.up)); s.Cam.orthographicSize = size;
            return s.Rt;
        }

        Inset NewInset(int k)
        {
            var go = new GameObject("inset/" + k); go.transform.SetParent(root, false);
            var c = go.AddComponent<UnityEngine.Camera>(); c.orthographic = true; c.nearClipPlane = 0.05f; c.farClipPlane = 8f; c.cullingMask = 1 << (InsetLayer0 + k);
            c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = InsetGround; c.allowHDR = false; c.allowMSAA = false; c.depth = -10;
            var rt = new RenderTexture(128, 128, 16, RenderTextureFormat.ARGB32) { name = "inset" + k, antiAliasing = 1 }; rt.Create(); c.targetTexture = rt;
            var d = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            d.renderPostProcessing = false; d.renderShadows = false; d.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None; d.requiresColorOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off; d.requiresDepthOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
            c.enabled = false;
            if (host.View) host.View.cullingMask |= 1 << (InsetLayer0 + k); // the watcher still draws in the street
            return new Inset { Cam = c, Rt = rt };
        }

        static void Move(Inset s, Transform t, int layer) { foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { s.Moved.Add((r, r.gameObject.layer)); r.gameObject.layer = layer; } }
        static void Restore(Inset s) { foreach (var (r, l) in s.Moved) if (r) r.gameObject.layer = l; s.Moved.Clear(); }
    }
}
