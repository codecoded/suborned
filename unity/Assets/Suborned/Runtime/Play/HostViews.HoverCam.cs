using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 096 (Sky: "I really hate our street camera"; concept A3, "hovering"; "it's same position as current camera, just floating"; "doesn't need
    /// a cradle - this is retro future world"): every mission camera is Vela's HoverCamera, the porthole eye floating on a ring of light where the pole's
    /// camera was, with no pole and nothing holding it up (briefs/096/vela/camera.md). The lens glass stands on the pole lens's point (4.78 m up, 0.89 m out
    /// along the facing), so cones and sight lines do not move. View only: it reads the engine and never changes it.
    ///  · live: the head bobs ±3 cm at 0.5 Hz, the ring breathes, the tally lamp is lit; it yaws to the camera's facing, read every frame.
    ///  · looped (the Tech's loop on this camera): the tally blinks at 1 Hz (Vela's placeholder until Glyph and Motive decide).
    ///  · dead (Game.CameraDark: shot or cut): lamp and ring dark (Vela's dead colours), the lens dim, and the head sinks and settles on the ground at its
    ///    spot, resting on its ring. Coming back (a restart) reverses it.
    ///  · its shadow (Sky: "you need to give the camera a shadow"): every part casts the sun's shadow, live or down, and a soft contact shadow lies on the
    ///    ground straight under it (the game's own Suborned/Contact Shade blob, as under the street pieces), following the bob, a little fainter and wider
    ///    the higher the head is, so its height and place read even where the sun shadow falls off-screen.
    /// The attention cue (CamCue) still tints the lens: while a cue shows the lens wears the cue's unlit material, and the burst scales it about its own
    /// centre. Lab switch for the review shots: `-hoverScale s` scales the head uniformly about the lens point.</summary>
    public sealed partial class HostViews
    {
        sealed class Hover
        {
            public Suborned.Sim.Camera Cam; public CamHead Cue; public Transform Lens, Blob; public Material BlobMat;
            public Renderer LensR, Tally, Ring; public Material LensBuilt; public Color RingEmit;
            public float Phase, Down; public int State = -1;
        }
        readonly List<Hover> hovers = new();
        static readonly MaterialPropertyBlock hoverMpb = new();
        static Mesh blobQuad;

        /// <summary>Vela's measures (camera.md, Unity axes, metres at scale 1): the lens glass front from the head's origin; the origin's height when resting on
        /// its ring; the head's half width and half length (the contact shadow's ellipse).</summary>
        static readonly Vector3 LensFront = new(0f, 0f, 0.23f);
        const float RestUp = 0.328f, HalfW = 0.27f, HalfL = 0.52f, BobM = 0.03f, BobHz = 0.5f, SinkS = 1.6f;
        /// <summary>The contact shadow's strength on the ground (resting) and at the hover height (5 m), its spread there, and its lift off the ground.</summary>
        const float BlobNear = 1f, BlobFar = 0.85f, BlobSpread = 0.35f, BlobLift = 0.012f;
        static readonly Color TallyDead = new(0x6B / 255f, 0x4A / 255f, 0x1E / 255f), RingDead = new(0x5E / 255f, 0x6B / 255f, 0x6A / 255f);
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"), EmissionId = Shader.PropertyToID("_EmissionColor"), StrengthId = Shader.PropertyToID("_Strength");

        /// <summary>The head's uniform scale about the lens point (Sky 2026-11-05: 1.4 × Vela's size, the one line to change), else the player's `-hoverScale s` (0.5..3), for review shots.</summary>
        public const float DefaultHoverScale = 1.4f; public static readonly float HoverScale = Mathf.Clamp(Arg("-hoverScale", DefaultHoverScale), 0.5f, 3f);
        static float Arg(string name, float or)
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length && float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : or;
        }

        /// <summary>The camera's head (from CamHeadFor): `head` is the turn centre (the nod turns it about its origin, as Vela's pivot), the kit model under it,
        /// the lens part under a pivot at its own centre (the cue's burst); the contact shadow on the ground under it.</summary>
        void HoverHead(Suborned.Sim.Camera c, Vector3 at, Transform head)
        {
            float s = HoverScale; head.localScale = Vector3.one * s;
            var h = new Hover { Cam = c, Phase = Seed(c.Id) };
            var m = UnityEngine.Object.Instantiate(host.hoverCamera, head, false).transform; m.name = "model";
            h.Cue = new CamHead { Head = head, Lens = Unlit(LensDim), Scale = Vector3.one };
            LivePose(h, at, 0f, out var p0, out var r0); head.SetPositionAndRotation(p0, r0); h.Cue.Rest = r0;
            foreach (var r in m.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; // the sun's shadow, every part, the ring too
                if (r.name.EndsWith("_Lens")) h.LensR = r; else if (r.name.EndsWith("_Tally")) h.Tally = r; else if (r.name.EndsWith("_Ring")) h.Ring = r;
            }
            if (h.LensR)
            {
                h.LensBuilt = h.LensR.sharedMaterial;
                var piv = new GameObject("lens").transform; piv.SetParent(head, false); piv.position = h.LensR.bounds.center; // CamCue finds "lens" and scales it for the flash
                h.LensR.transform.SetParent(piv, true); h.Lens = piv;
            }
            if (h.Ring) h.RingEmit = h.Ring.sharedMaterial.GetColor(EmissionId);
            if (host.contactTemplate)
            {
                if (!blobQuad)
                {
                    blobQuad = new Mesh { name = "hover-blob" };
                    blobQuad.SetVertices(new[] { new Vector3(-1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, -1f) });
                    blobQuad.SetUVs(0, new[] { new Vector2(-1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(1f, -1f) });
                    blobQuad.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0); blobQuad.RecalculateBounds();
                }
                h.BlobMat = new Material(host.contactTemplate) { name = "hover-blob/" + c.Id }; h.BlobMat.SetFloat("_Mode", 1f); h.BlobMat.SetFloat("_Power", 1.1f);
                h.Blob = Part(root, "camera/" + c.Id + "/shadow", blobQuad, h.BlobMat, Vector3.zero, Vector3.one);
                var br = h.Blob.GetComponent<MeshRenderer>(); br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; br.receiveShadows = false;
            }
            camHeads["camera/" + c.Id] = h.Cue; hovers.Add(h);
        }

        /// <summary>Per frame, after the cues (AttentionViews): each head's pose, lamps and contact shadow from the engine.</summary>
        void HoverViews(float t, float dt)
        {
            var g = host.Game; if (g == null || hovers.Count == 0) return;
            foreach (var h in hovers)
            {
                if (!h.Cue.Head) continue;
                bool dead = g.CameraDark(h.Cam.Id), looped = !dead && g.LoopOn && g.LoopCam == h.Cam.Id;
                h.Down = Mathf.MoveTowards(h.Down, dead ? 1f : 0f, dt / SinkS);
                var at = frame.World(h.Cam.At); float bob = Mathf.Sin((t * BobHz + h.Phase) * Mathf.PI * 2f);
                LivePose(h, at, BobM * bob * (1f - h.Down), out var lp, out var lr);
                h.Cue.Rest = lr; var live = Quaternion.AngleAxis(h.Cue.Nod, Vector3.up) * lr;
                // down: on its ring on the ground under where it hovered, level, facing as it faced
                var f = FlatDir(lr * Vector3.forward); float ground = Ground(lp);
                var restAt = new Vector3(lp.x, ground + RestUp * HoverScale, lp.z); var restRot = Quaternion.LookRotation(f, Vector3.up);
                float e = Mathf.SmoothStep(0f, 1f, h.Down); // it sinks and settles
                h.Cue.Head.SetPositionAndRotation(Vector3.Lerp(lp, restAt, e), Quaternion.Slerp(live, restRot, e));
                if (h.LensR) { var want = h.Cue.Shown > 0 || dead ? h.Cue.Lens : h.LensBuilt; if (h.LensR.sharedMaterial != want) h.LensR.sharedMaterial = want; }
                int state = dead ? 2 : looped ? 1 : 0;
                bool lampOn = state == 0 || state == 1 && Mathf.Repeat(t, 1f) < 0.5f;
                if (h.Tally && (state != h.State || state == 1)) Lamp(h.Tally, lampOn ? (Color?)null : TallyDead);
                if (h.Ring) { if (dead) { if (h.State != 2) Lamp(h.Ring, RingDead); } else Glow(h.Ring, h.RingEmit * (0.88f + 0.12f * bob)); }
                h.State = state;
                if (h.Blob)
                {
                    var p = h.Cue.Head.position; float k = Mathf.Clamp01((p.y - ground - RestUp * HoverScale) / 5f), spread = (1f + BlobSpread * k) * HoverScale;
                    h.Blob.SetPositionAndRotation(new Vector3(p.x, Ground(p) + BlobLift, p.z), Quaternion.LookRotation(FlatDir(h.Cue.Head.forward), Vector3.up));
                    h.Blob.localScale = new Vector3(HalfW * 1.5f * spread, 1f, HalfL * 1.2f * spread);
                    h.BlobMat.SetFloat(StrengthId, Mathf.Lerp(BlobNear, BlobFar, k));
                }
            }
        }

        /// <summary>The live pose: the lens glass on the pole lens's point (4.78 m up, 0.89 m out: brief 096 keeps it exactly; Vela's origin, at + up × 4.78 +
        /// facing × 0.66, put the tilted glass 7 cm lower), 16.7 deg nose-down, yawed to the camera's facing as the engine has it now, plus the bob.</summary>
        void LivePose(Hover h, Vector3 at, float bob, out Vector3 pos, out Quaternion rot)
        {
            var f = FlatDir(frame.Dir(h.Cam.Facing));
            rot = Quaternion.LookRotation(Quaternion.AngleAxis(LensDown, Vector3.Cross(Vector3.up, f)) * f, Vector3.up);
            var lens = at + Vector3.up * LensUp + f * LensOut;
            pos = lens - rot * (LensFront * HoverScale) + Vector3.up * bob;
        }

        float Ground(Vector3 w) { var b = frame.ToBlock(new Vector2(w.x, w.z)); return frame.Height(b.x, b.y); }
        static Vector3 FlatDir(Vector3 f) { f.y = 0f; return f.sqrMagnitude < 1e-6f ? Vector3.forward : f.normalized; }
        static float Seed(string id) { int k = 17; foreach (var ch in id) k = k * 31 + ch; return Mathf.Repeat(k * 0.618034f, 1f); }

        /// <summary>A lamp dark (its dead base colour, no emission) or as built (null).</summary>
        static void Lamp(Renderer r, Color? dark)
        {
            if (dark is not Color d) { r.SetPropertyBlock(null); return; }
            hoverMpb.Clear(); hoverMpb.SetColor(BaseColorId, d); hoverMpb.SetColor(EmissionId, Color.black); r.SetPropertyBlock(hoverMpb);
        }

        static void Glow(Renderer r, Color emit) { hoverMpb.Clear(); hoverMpb.SetColor(BaseColorId, r.sharedMaterial.GetColor(BaseColorId)); hoverMpb.SetColor(EmissionId, emit); r.SetPropertyBlock(hoverMpb); }
    }
}
