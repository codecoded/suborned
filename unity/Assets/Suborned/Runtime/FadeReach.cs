using System.Collections.Generic;
using UnityEngine;

namespace Suborned
{
    /// <summary>The cut the architecture fade makes this frame, as the shaders test it (DK_Cutaway.hlsl).</summary>
    public struct FadeCut
    {
        /// <summary>The focus (world): the disc lies round it, at its height.</summary>
        public Vector3 Focus;
        /// <summary>Height (world y) at or below which nothing is cut.</summary>
        public float FloorY;
        /// <summary>Ground run (x, z) of a view ray per metre it drops.</summary>
        public Vector2 Run;
        /// <summary>The view on the ground (x, z), unit.</summary>
        public Vector2 Ahead;
        /// <summary>How far beyond the focus, along Ahead, the cut reaches (m).</summary>
        public float Back;
        /// <summary>Disc radius (m).</summary>
        public float Radius;

        public bool Same(in FadeCut o) => Focus == o.Focus && FloorY == o.FloorY && Run == o.Run && Ahead == o.Ahead && Back == o.Back && Radius == o.Radius;
    }

    /// <summary>Keeps the cost of the architecture fade to what it can reach. A shader that may discard costs the GPU its
    /// hidden-surface removal on every pixel it covers, so the tagged architecture draws with its plain variant, and only the
    /// renderers the cut can reach (their bounds against the cut's slanted column, with a margin) take a twin of their
    /// material with the cutting variant on (keyword _DK_CUT). The shaders still decide per pixel what thins: a renderer
    /// changing variant is not seen. Renderer count and material count per renderer never change, so the GPU Resident Drawer
    /// keeps every instance; a pan moves a handful of renderers from one material to its twin. Play Mode only: the twins are
    /// runtime objects and must never be saved into the generated scene.</summary>
    public sealed class FadeReach
    {
        public const string Keyword = "_DK_CUT";
        /// <summary>Reach beyond the disc (m): a renderer takes its cutting variant before the cut gets to it, so a focus that
        /// moves after this frame's reach was set (or a lag of a frame) shows no uncut edge.</summary>
        const float Margin = 4f;

        readonly MeshRenderer[] renderers;
        readonly Bounds[] bounds;
        readonly Material[][] plain, cutting;
        readonly bool[] reached;
        readonly Dictionary<Material, Material> twins = new();
        FadeCut last;
        bool any;

        /// <summary>Tagged renderers in the loaded scenes.</summary>
        public int Count => renderers.Length;
        /// <summary>How many of them draw with the cutting variant now.</summary>
        public int Reached { get; private set; }

        public FadeReach()
        {
            var tagged = new List<MeshRenderer>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
                if ((r.renderingLayerMask & ArchitectureFade.Layer) != 0) tagged.Add(r);
            renderers = tagged.ToArray();
            bounds = new Bounds[renderers.Length]; plain = new Material[renderers.Length][]; cutting = new Material[renderers.Length][]; reached = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) { bounds[i] = renderers[i].bounds; plain[i] = renderers[i].sharedMaterials; }
        }

        /// <summary>Gives the cutting variant to what the cut can reach and takes it back from what it no longer can.</summary>
        public void Set(in FadeCut cut)
        {
            if (any && cut.Same(last)) return;
            last = cut; any = true;
            float r = cut.Radius + Margin;
            for (int i = 0; i < renderers.Length; i++) Mark(i, Reaches(bounds[i], cut, r));
        }

        /// <summary>Nothing fades: every renderer back to its plain material.</summary>
        public void Clear()
        {
            if (!any) return;
            any = false;
            for (int i = 0; i < renderers.Length; i++) Mark(i, false);
        }

        /// <summary>Puts the plain materials back and destroys the twins.</summary>
        public void Dispose()
        {
            Clear();
            foreach (var twin in twins.Values) if (twin) Object.Destroy(twin);
            twins.Clear();
        }

        /// <summary>Whether a fragment inside b can be cut: some part above the floor, on the camera's side of the back limit,
        /// whose view ray (or, in column mode, the run of rays below it) lands within r of the focus.</summary>
        static bool Reaches(in Bounds b, in FadeCut c, float r)
        {
            Vector3 min = b.min, max = b.max;
            if (max.y <= c.FloorY) return false;
            // the least of dot(p - focus, ahead) over the box: at or past the back limit, all of it stands beyond
            float least = Mathf.Min(c.Ahead.x * (min.x - c.Focus.x), c.Ahead.x * (max.x - c.Focus.x)) + Mathf.Min(c.Ahead.y * (min.z - c.Focus.z), c.Ahead.y * (max.z - c.Focus.z));
            if (least >= c.Back) return false;
            // where the box's rays land on the focus level: its footprint swept along the view from the floor to its top
            float lo = c.FloorY - c.Focus.y, hi = max.y - c.Focus.y;
            float dx = Gap(min.x + Mathf.Min(c.Run.x * lo, c.Run.x * hi), max.x + Mathf.Max(c.Run.x * lo, c.Run.x * hi), c.Focus.x);
            float dz = Gap(min.z + Mathf.Min(c.Run.y * lo, c.Run.y * hi), max.z + Mathf.Max(c.Run.y * lo, c.Run.y * hi), c.Focus.z);
            return dx * dx + dz * dz < r * r;
        }

        static float Gap(float lo, float hi, float v) => v < lo ? lo - v : v > hi ? v - hi : 0f;

        void Mark(int i, bool on)
        {
            if (reached[i] == on) return;
            reached[i] = on; Reached += on ? 1 : -1;
            if (!renderers[i]) return;
            renderers[i].sharedMaterials = on ? cutting[i] ??= Twins(plain[i]) : plain[i];
        }

        Material[] Twins(Material[] of)
        {
            var to = new Material[of.Length];
            for (int i = 0; i < of.Length; i++) to[i] = Twin(of[i]);
            return to;
        }

        Material Twin(Material m)
        {
            if (!m || !m.shader.keywordSpace.FindKeyword(Keyword).isValid) return m; // glass and the like: no cutting variant
            if (!twins.TryGetValue(m, out var twin))
            {
                twins[m] = twin = new Material(m) { name = m.name, hideFlags = HideFlags.HideAndDontSave };
                twin.EnableKeyword(Keyword);
            }
            return twin;
        }
    }
}
