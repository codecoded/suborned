using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 101 (Sky's pick B; Vela's notes 4-6, briefs/101/vela/lamp-mast.md): a world mast is drawn by its own kit piece (MastCamera_Lamp: the terrace lamp
    /// carried up to a crown of three lens pods, the lenses 7.68 m up, the crown's top 8.2 m), not by the host:
    ///  · the stand-in cylinder at its foot is hidden; its collider stays the click target, run up to the crown and slimmed to the column, with a box round the pods;
    ///  · the lens cue (CamCue's stages, as on the hovering camera's lens): the lens of the head that has the agent wears the cue's unlit material, and the burst on
    ///    reporting scales it about its own centre; the other two keep their lens as built. The pods are one mesh with the crown, so no head nods;
    ///  · its marks reach the crown: the "?" and the dead disc over its top, the callout's figure to its top, the ring's focus on its lenses, the pick anchor on
    ///    the pods, and the windows' and chips' anchor lifted by its head's height over a pole camera's, so they keep a pole camera's place below its head (AnchorWorld).
    /// A mast whose piece has no lens a head (the old MastCamera, a kit without the lamp mast) keeps the stand-in, as before. View only: it reads the engine.</summary>
    public sealed partial class HostViews
    {
        sealed class Mast
        {
            public Suborned.Sim.Camera Cam; public HandoverHost.WorldMast W; public CamHead Cue; public Material Dead;
            public Transform[] Pivot; public Material[] Built; public Vector3[] Look;
            /// <summary>Heights above the foot (m): the crown's top, the lenses' centre, the pods' centre.</summary>
            public float Top, Lens, Pods; public int Active = -1;
        }
        readonly Dictionary<string, Mast> masts = new();
        /// <summary>Each lens's material as built, taken once: the piece is the scene's, so it outlives these views (a restart builds new ones while a lens may still wear a cue).</summary>
        static readonly Dictionary<Renderer, Material> lensBuilt = new();
        /// <summary>Vela's pods look 15 deg nose-down (the lens look (0.966, -0.259, 0) in her table).</summary>
        const float MastTilt = 15f;

        HandoverHost.WorldMast MastOf(Suborned.Sim.Camera c)
        {
            foreach (var w in host.worldMasts ?? System.Array.Empty<HandoverHost.WorldMast>())
                if (w != null && w.id == c.Id && w.piece && w.lens != null && w.lens.Length == c.Facings.Length && System.Array.TrueForAll(w.lens, r => r)) return w;
            return null;
        }

        /// <summary>From the constructor, on a world camera's stand-in (1.6 m up, scale 0.25 x 1.6 x 0.25): with the lamp mast's piece, the stand-in hidden, its
        /// capsule run from the foot to the crown's top and a box round the pods; without it, nothing changes.</summary>
        void MastPole(Suborned.Sim.Camera c, Transform pole)
        {
            var w = MastOf(c); if (w == null) return;
            float foot = pole.position.y - 1.6f, top = foot, lens = 0f; var pods = w.lens[0].bounds;
            foreach (var r in w.piece.GetComponentsInChildren<Renderer>(true)) top = Mathf.Max(top, r.bounds.max.y);
            foreach (var r in w.lens) { pods.Encapsulate(r.bounds); lens += r.bounds.center.y; }
            var m = new Mast { Cam = c, W = w, Top = top - foot, Lens = lens / w.lens.Length - foot, Pods = pods.center.y - foot };
            if (pole.GetComponent<MeshRenderer>() is MeshRenderer mr) mr.enabled = false; // Vela's note 6: the piece is the camera, the stand-in only its click
            var col = pole.GetComponent<CapsuleCollider>(); col.height = m.Top / 1.6f; col.center = new Vector3(0f, (m.Top / 2f - 1.6f) / 1.6f, 0f); col.radius = 0.35f / 0.25f; // 0.7 m round the column, sleeve and globe: not the pole's fat 1.25 m, which up 8.2 m beside the cell's start would take the street's walk clicks
            var crown = new GameObject("crown").transform; crown.SetParent(pole, false); crown.localScale = new Vector3(1f / 0.25f, 1f / 1.6f, 1f / 0.25f);
            crown.SetPositionAndRotation(pods.center, Quaternion.identity);
            crown.gameObject.AddComponent<BoxCollider>().size = pods.size + Vector3.one * 0.5f; // the pods and the crown between them, 25 cm round: the obvious thing to click
            masts["camera/" + c.Id] = m;
        }

        /// <summary>From CamHeadFor: each lens under a pivot at its own centre (the burst), its look (the facing, nose-down as built), and the cue's head: a marker the
        /// cue's nod and the callout's inset read, standing on the lens of the head that has the agent.</summary>
        void MastHead(Suborned.Sim.Camera c)
        {
            if (!masts.TryGetValue("camera/" + c.Id, out var m)) return; // no lamp piece: the world's mast draws itself with no cue, as before brief 101
            int n = m.W.lens.Length; m.Pivot = new Transform[n]; m.Built = new Material[n]; m.Look = new Vector3[n];
            for (int k = 0; k < n; k++)
            {
                var r = m.W.lens[k]; if (!lensBuilt.TryGetValue(r, out var built)) lensBuilt[r] = built = r.sharedMaterial; m.Built[k] = built;
                string pn = $"camera/{c.Id}/lens{k}"; var piv = r.transform.parent;
                if (!piv || piv.name != pn) { piv = new GameObject(pn).transform; piv.SetParent(r.transform.parent, false); piv.SetPositionAndRotation(r.bounds.center, Quaternion.identity); r.transform.SetParent(piv, true); }
                piv.localScale = Vector3.one; m.Pivot[k] = piv; // an earlier view's pivot is this one's: a restart left it where it was
                var f = frame.Dir(c.Facings[k]); f.y = 0f; f = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
                m.Look[k] = Quaternion.AngleAxis(MastTilt, Vector3.Cross(Vector3.up, f)) * f;
            }
            var head = new GameObject("camera/" + c.Id + "/head").transform; head.SetParent(root, false);
            head.SetPositionAndRotation(m.Pivot[0].position, Quaternion.LookRotation(m.Look[0], Vector3.up));
            m.Cue = new CamHead { Head = head, Lens = Unlit(LensDim), Rest = head.rotation, Scale = Vector3.one }; m.Dead = Unlit(LensDim);
            camHeads["camera/" + c.Id] = m.Cue; // CamCue keeps its colour and stage (AttentionViews); MastViews puts it on the lens that saw
        }

        /// <summary>Per frame, after CamCue (AttentionViews): the cue on the lens of the head that has the agent, the others as built; dark, every lens dim.</summary>
        void MastViews(float t)
        {
            var g = host.Game; if (g == null || masts.Count == 0) return;
            foreach (var m in masts.Values)
            {
                if (m.Cue == null) continue;
                bool dead = g.CameraDark(m.Cam.Id); var (_, op) = host.Cue("camera:" + m.Cam.Id);
                int k = dead || m.Cue.Shown <= 0 ? -1 : HeadOn(m, op);
                for (int i = 0; i < m.Pivot.Length; i++)
                {
                    var want = dead ? m.Dead : i == k ? m.Cue.Lens : m.Built[i]; var r = m.W.lens[i];
                    if (r && r.sharedMaterial != want) r.sharedMaterial = want;
                    bool burst = i == k && m.Cue.Shown == 3 && Mathf.Repeat(t * 4f, 1f) < 0.5f; m.Pivot[i].localScale = burst ? Vector3.one * 1.35f : Vector3.one;
                }
                m.Active = k; int at = k >= 0 ? k : 0;
                var rest = Quaternion.LookRotation(m.Look[at], Vector3.up); m.Cue.Rest = rest; m.Cue.Nod = 0f;
                m.Cue.Head.SetPositionAndRotation(m.Pivot[at].position, rest);
            }
        }

        /// <summary>The head that has the agent: of the heads whose cone holds him, the one that faces him most squarely; while the shown stage lingers after he
        /// stepped out (HostAttention's 2 s), the head that last had him.</summary>
        int HeadOn(Mast m, string op)
        {
            var g = host.Game; if (op.Length == 0) return m.Active; Pt p;
            try { p = g.Ops[(int)Op.Parse(op)].Pos; } catch (System.FormatException) { return m.Active; }
            var c = m.Cam; int best = -1; float bestA = float.MaxValue;
            for (int k = 0; k < c.Facings.Length; k++)
            {
                if (!G.InCone(c.At, c.Facings[k], c.Reach, c.Half, 0, p)) continue;
                float a = Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(c.Facings[k].Y, c.Facings[k].X) * Mathf.Rad2Deg, Mathf.Atan2(p.Y - c.At.Y, p.X - c.At.X) * Mathf.Rad2Deg));
                if (a < bestA) { bestA = a; best = k; }
            }
            return best >= 0 ? best : m.Active;
        }

        /// <summary>The proof reads these: whether a view is a world mast with its own piece; the head whose lens wears the cue now (-1 none) and how many lenses do.</summary>
        public bool IsMast(string view) => masts.ContainsKey(view);
        public (int head, int cued) MastCue(string view)
        {
            if (!masts.TryGetValue(view, out var m) || m.Cue == null) return (-1, 0);
            int n = 0; foreach (var r in m.W.lens) if (r && r.sharedMaterial == m.Cue.Lens) n++;
            return (m.Active, n);
        }

        // ---------------------------------------------------------------- the marks' heights, by camera (the HUD reads these)

        /// <summary>A camera's head above its foot (m): a mast's lenses, else the mission pole's (CamHeadUp).</summary>
        public float HeadUp(string view) => masts.TryGetValue(view, out var m) ? m.Lens : CamHeadUp;
        /// <summary>Where marks stack over a camera (m above its foot): a mast's crown top, else the mission pole's head (CamHeadUp, the dead disc's "pole top").</summary>
        public float TopUp(string view) => masts.TryGetValue(view, out var m) ? m.Top : CamHeadUp;
        /// <summary>A camera's drawn height, for the callout's figure box: a mast's top, else CameraTall.</summary>
        public float TallOf(string view) => masts.TryGetValue(view, out var m) ? m.Top : CameraTall;
        /// <summary>The pick anchor's lift over a camera's view (its stand-in, 1.6 m up): a mast's pods, else none.</summary>
        public float PickUp(string view) => masts.TryGetValue(view, out var m) ? m.Pods - 1.6f : 0f;
        /// <summary>The lift HandoverHost.AnchorWorld gives a view: a mast's head over a pole camera's, so its window tab, chips and cards stand where a pole camera's do below
        /// its head (on the column under the crown), clear of the "?" and the dead disc over its top; none for any other view.</summary>
        public float AnchorLift(string view) => masts.TryGetValue(view, out var m) ? m.Lens - CamHeadUp : 0f;

        readonly Dictionary<string, float> headTops = new();
        /// <summary>The top of a camera, where its "?" stands (world): a mast's crown on its column; a hovering head's own top, bobbing with it (its renderers'
        /// height over the head, taken once); the kit pole's mount; else the stand-in's head.</summary>
        public Vector3 CamTop(Suborned.Sim.Camera c)
        {
            var foot = frame.World(c.At);
            if (masts.TryGetValue("camera/" + c.Id, out var m)) return foot + Vector3.up * m.Top;
            if (!c.World && host.hoverCamera && camHeads.TryGetValue("camera/" + c.Id, out var h) && h.Head)
            {
                if (!headTops.TryGetValue(c.Id, out var up)) { up = 0f; foreach (var r in h.Head.GetComponentsInChildren<Renderer>()) up = Mathf.Max(up, r.bounds.max.y - h.Head.position.y); headTops[c.Id] = up; }
                var p = h.Head.position; return new Vector3(p.x, p.y + up, p.z);
            }
            return foot + Vector3.up * (c.World ? 3.6f : host.cctvPole ? CameraTall : 3.4f);
        }
    }
}
