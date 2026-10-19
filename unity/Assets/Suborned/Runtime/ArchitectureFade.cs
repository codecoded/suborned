using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned
{
    /// <summary>The play camera's architecture fade (GDD §10, §15: "tagged architecture ghosts to 0.2 + ghost shadow"). The
    /// tactical camera looks down at 38°, so a 38 m tower hides 48 m of street behind it. Architecture that stands between the
    /// camera and a disc round the focus, and covers that disc as the camera sees it, thins to a screen-door ghost; with
    /// `column` everything above a thinned part goes with it, so no solid crown floats over a ghost. What stands beyond the
    /// focus (the fronts that face the camera: doors, shopfronts, signs) stays solid: it hides only ground further back. The
    /// ghost casts a lighter ghost shadow (`ghostShadow`), so the street behind a tower is lit, not a dark pit under a roof that
    /// is no longer drawn, yet the tower still marks the ground.
    ///
    /// Cost: four vectors per camera render, and in Play Mode a pass over the tagged renderers' bounds when the cut moves.
    /// The fade is done per pixel in the shaders the architecture already uses (DK_Cutaway.hlsl in Suborned/FacadeLit and the
    /// DecoKit Cutaway shaders), dithered in the opaque pass: no renderer is added, removed or given a property block, so the
    /// GPU Resident Drawer keeps every instance and no draw call is added by the cut itself. Only what the cut can reach
    /// draws with the variant that may discard (FadeReach: a twin material on those few renderers); the rest of the city
    /// keeps the plain one. What may fade is marked at build time (the builder's FadeTags sets rendering-layer bit `Layer`);
    /// ground, street furniture, people and signals never carry it. The values are set only while this camera renders, so no
    /// other camera (a review eye, the Scene view) is cut. Orthographic cameras only: the test assumes parallel view rays.
    /// One camera at a time holds the reach (the first enabled one); any other fading camera, and every render outside Play
    /// Mode, turns the cutting variant on for the whole of its render instead: the same picture, at a cost that only matters
    /// in play.
    ///
    /// Brief 016 (the view turns, so a tower can stand in front of anything): besides the focus, up to MaxPoints points keep their own
    /// smaller discs clear by the same rule, each at its own level (SetPoints: the host gives the agents, the action target, the cursor's
    /// ground point and the key people every frame). The reach takes what any disc can reach.</summary>
    [DefaultExecutionOrder(1000)] // after whatever moves the camera and sets the focus this frame
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class ArchitectureFade : MonoBehaviour
    {
        /// <summary>Rendering-layer bit on every renderer that may fade (DK_CUT_LAYER in DK_Cutaway.hlsl).</summary>
        public const uint Layer = 1u << 20;

        [Tooltip("On: architecture over the focus disc is a ghost. Turning it on or off eases over fadeTime.")]
        public bool fade = true;
        [Tooltip("The point the player needs to see (world). The camera rig sets it when it moves; the game may set it to the selected person. Its height is the level: nothing at or below it fades.")]
        public Vector3 focus;
        [Tooltip("Disc radius as a share of the orthographic half-height (0.8 at play zoom 14 = 11.2 m).")]
        public float radiusPerZoom = 0.8f;
        [Tooltip("Disc radius limit (m) when zoomed out.")]
        public float maxRadius = 40f;
        [Tooltip("Width (m) of the dithered edge of the disc.")]
        public float feather = 2f;
        [Tooltip("What stays of faded architecture (GDD §15: 0.2). 0 = cut clean away.")]
        [Range(0f, 1f)] public float ghost = 0.2f;
        [Tooltip("Height (m) above the focus level below which nothing fades. 0 = down to the ground: a wall facing away from the camera hides the people at its foot.")]
        public float floor = 0f;
        [Tooltip("How far (m) beyond the focus, along the view, architecture still fades. 0 = only what stands on the camera's side of the focus; the disc radius or more = everything over the disc.")]
        public float behind = 0f;
        [Tooltip("Also fade everything above a faded part (no solid crown over a ghost).")]
        public bool column = true;
        [Tooltip("The ghost casts a ghost shadow (the same fade in the shadow map).")]
        public bool shadows = true;
        [Tooltip("What stays of faded architecture in the sun's shadow map (1 = its full shadow, 0 = none). Above `ghost`, so a faded tower still casts a lighter shadow (light.json `fade.ghostShadow`; Glint, 008 B5: 0.4).")]
        [Range(0f, 1f)] public float ghostShadow = 0.4f;
        [Tooltip("Off (as built since 008 B6): only the fade cells the cut reaches draw with the discarding variant. On: a reached cell takes its whole 64 m batch tile with it, as before B6 (the perf harness's as-was reach).")]
        public bool wholeTiles;
        [Tooltip("Kit pieces (name prefixes) drawn whole inside the discs though tagged: free-standing props, so only building meshes fade (Glyph, brief 016 review §2). Play Mode only.")]
        public string[] wholeProps = { "DK_Cypress", "DK_Parasol" };
        [Tooltip("Seconds to ease in or out when `fade` changes (unscaled time: it runs during a tactical pause).")]
        public float fadeTime = 0.25f;

        static readonly int FocusId = Shader.PropertyToID("_DKCutFocus"), ViewId = Shader.PropertyToID("_DKCutView"),
            ShapeId = Shader.PropertyToID("_DKCutShape"), BackId = Shader.PropertyToID("_DKCutBack"), PointsId = Shader.PropertyToID("_DKCutPoints"), CleanId = Shader.PropertyToID("_DKCutClean");

        [Tooltip("Brief 020 (Glyph): a person's disc (SetPoints with a negative radius) is cut clean, not to the ghost, so no screen door falls over a figure; this is its feather (m).")]
        public float cleanFeather = 0.4f;

        /// <summary>DK_CUT_POINTS in DK_Cutaway.hlsl: the shader's array is always this long.</summary>
        public const int MaxPoints = 10;
        readonly Vector4[] points = new Vector4[MaxPoints]; // xyz world, w disc radius (m)
        int pointCount;
        readonly FadeCut[] cuts = new FadeCut[MaxPoints + 1];

        /// <summary>The points kept clear besides the focus this frame: xyz world (its height is its level), w the disc radius (m); a negative
        /// w is a person's disc of radius -w, cut clean (no ghost dither over the figure). Past MaxPoints are dropped, so give them in order of need.</summary>
        public void SetPoints(System.Collections.Generic.IReadOnlyList<Vector4> list)
        {
            pointCount = 0;
            if (list != null) for (int i = 0; i < list.Count && pointCount < MaxPoints; i++) if (list[i].w != 0f) points[pointCount++] = list[i];
            for (int i = pointCount; i < MaxPoints; i++) points[i] = Vector4.zero;
        }
        /// <summary>The points kept clear now.</summary>
        public int PointCount => pointCount;

        readonly System.Collections.Generic.Dictionary<Renderer, (Material[] plain, uint mask)> forced = new();
        /// <summary>Brief 022: a prop drawn whole (wholeProps) that must fade for now, as the architecture does (the parasol over the table
        /// Inez is serving): on, it takes the cut tag and its cutting materials; off, it is put back. Play Mode only.</summary>
        public void ForceFade(Renderer r, bool on)
        {
            if (!r || reach == null) return;
            if (on)
            {
                if (forced.ContainsKey(r)) return;
                forced[r] = (r.sharedMaterials, r.renderingLayerMask);
                r.renderingLayerMask |= Layer; r.sharedMaterials = reach.CuttingOf(r.sharedMaterials);
            }
            else if (forced.TryGetValue(r, out var was)) { r.sharedMaterials = was.plain; r.renderingLayerMask = was.mask; forced.Remove(r); }
        }

        static ArchitectureFade holder; // the camera whose reach picks the cutting materials

        Camera cam;
        FadeReach reach;
        float level; // 0 = solid, 1 = faded to `ghost`

        /// <summary>Disc radius (m) at the camera's zoom now.</summary>
        public float Radius => cam ? Mathf.Min(maxRadius, radiusPerZoom * cam.orthographicSize) : 0f;
        /// <summary>Renderers drawing with the cutting variant now (0 outside Play Mode: there the whole render takes it).</summary>
        public int Reached => reach?.Reached ?? 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Forget() => holder = null; // Play Mode may start without a domain reload (not named Reset: Unity sends that to the component)

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            level = fade ? 1f : 0f;
            RenderPipelineManager.beginCameraRendering += Begin;
            RenderPipelineManager.endCameraRendering += End;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Begin;
            RenderPipelineManager.endCameraRendering -= End;
            Clear();
            Release();
        }

        void Update()
        {
            float to = fade ? 1f : 0f;
            level = Application.isPlaying && fadeTime > 0f ? Mathf.MoveTowards(level, to, Time.unscaledDeltaTime / fadeTime) : to;
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (!holder && cam.enabled) holder = this;
            if (holder != this) return;
            reach ??= new FadeReach(wholeProps);
            if (cam.enabled && Cuts(out var cut))
            {
                cuts[0] = cut;
                for (int i = 0; i < pointCount; i++) { var q = points[i]; cuts[i + 1] = cut; cuts[i + 1].Focus = q; cuts[i + 1].FloorY = q.y + (cut.FloorY - cut.Focus.y); cuts[i + 1].Radius = Mathf.Abs(q.w); }
                reach.Set(cuts, pointCount + 1, wholeTiles);
            }
            else reach.Clear();
        }

        void Begin(ScriptableRenderContext _, Camera c)
        {
            if (c != cam) return;
            if (!Application.isPlaying) level = fade ? 1f : 0f; // edit-mode renders (Shots) get no Update between a change and the render
            if (!Cuts(out var cut)) { Clear(); return; }
            var fwd = transform.forward;
            float f = Mathf.Max(feather, 0.01f);
            Shader.SetGlobalVector(FocusId, new Vector4(cut.Focus.x, cut.Focus.y, cut.Focus.z, cut.Radius));
            Shader.SetGlobalVector(ViewId, new Vector4(fwd.x, fwd.y, fwd.z, cut.FloorY));
            Shader.SetGlobalVector(ShapeId, new Vector4(f, Mathf.Lerp(1f, ghost, level), column ? 1f : 0f, shadows ? 1f : 0f));
            Shader.SetGlobalVector(BackId, new Vector4(cut.Back, f, Mathf.Lerp(1f, ghostShadow, level), pointCount));
            Shader.SetGlobalVectorArray(PointsId, points);
            Shader.SetGlobalVector(CleanId, new Vector4(1f - level, Mathf.Max(cleanFeather, 0.01f), 0f, 0f));
            if (reach == null) Shader.EnableKeyword(FadeReach.Keyword); // no reach: every tagged shader takes its cutting variant for this render
        }

        void End(ScriptableRenderContext _, Camera c)
        {
            if (c == cam) Clear();
        }

        /// <summary>The cut this camera makes now; false when it makes none (fade off, a perspective or level camera).</summary>
        bool Cuts(out FadeCut cut)
        {
            cut = default;
            var fwd = transform.forward;
            if (level <= 0f || !cam.orthographic || fwd.y > -0.05f) return false;
            var flat = new Vector2(fwd.x, fwd.z);
            cut = new FadeCut { Focus = focus, FloorY = focus.y + floor, Run = flat / -fwd.y, Ahead = flat / Mathf.Max(flat.magnitude, 1e-3f), Back = behind, Radius = Radius };
            return true;
        }

        void Release()
        {
            reach?.Dispose();
            reach = null;
            if (holder == this) holder = null;
        }

        /// <summary>Radius 0 = off: every other camera draws the architecture whole.</summary>
        static void Clear()
        {
            Shader.SetGlobalVector(FocusId, Vector4.zero);
            Shader.DisableKeyword(FadeReach.Keyword);
        }
    }
}
