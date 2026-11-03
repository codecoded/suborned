using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        [Header("Brief 056 F: the capsule cut")]
        [Tooltip("Brief 056 F (Sky 2026-10-25): in the camera's passes a soft capsule per point (SetPoints) replaces the ground discs: a person's foot to head, a ground point's foot to `pointHeight`, cut only where the wall is nearer the camera than the point by `depthMargin`; no focus disc. The shadow map is not cut (Glint 2026-10-25: a tower casts its full shadow); ghostShadow is the column dither's. Off: the column dither (A), as built to 0.2.117.")]
        public bool capsules = true;
        [Tooltip("A person's capsule radius (m) on the screen (the lab's 2 m at the person's depth).")]
        public float capsuleRadius = 2f;
        [Tooltip("A ground point's capsule radius (m): the cursor, the target, a door marker (the lab's 1.5 m).")]
        public float pointRadius = 1.5f;
        [Tooltip("A person's capsule runs from the feet to this height (m).")]
        public float personHeight = 1.9f;
        [Tooltip("A ground point's capsule runs from the ground to this height (m).")]
        public float pointHeight = 0.4f;
        [Tooltip("The soft edge starts at this share of the radius and is gone at the radius (smoothstep; the lab's 0.55).")]
        [Range(0f, 1f)] public float edgeInner = 0.70f; // Glint 056 F2 (2026-10-26): 0.55 -> 0.70 trial (band 0.6 m, crisper on a sunlit roof); Proof118 shoots 0.55, 0.70, 0.85
        [Tooltip("A fragment is cut only when it is nearer the camera than the point by this much (m, linear eye depth: the lab's 0.45).")]
        public float depthMargin = 0.45f;
        [Tooltip("The cut's rim leans towards this colour (cream #f7f4ec; Glint tunes it).")]
        public Color rimColor = new Color(0.969f, 0.957f, 0.925f);
        [Tooltip("How far the rim leans at most (the lab's 0.55 x 0.6 = 0.33). 0 = no rim.")]
        [Range(0f, 1f)] public float rim = 0.33f;
        [Tooltip("The rim's cream scales with the wall's lit luminance x this (Glint 2.5): faint on enamel and at night, nothing on sunlit stone. signals.json capsule.rimLit wins when present.")]
        public float rimLit = 2.5f;

        [Tooltip("Brief 056 F: the people who matter (the cell, hostiles, key people: SilhouettePass.SilhouetteLayer, set by the host) are drawn again, flat with a rim, where something nearer covers them (SilhouettePass), this camera only.")]
        public bool silhouettes = true;
        [Tooltip("The silhouette's fill by day (Glint's pre-grade #12544C that the day grade lands near the kit's ink #17302f); signals.json silhouette.fill wins per phase when present.")]
        public Color silhouetteFill = new Color(0.071f, 0.329f, 0.298f);
        [Tooltip("The silhouette's rim by day (Glint's pre-grade #FFFFF2, the nearest-cream a plain colour reaches through the grade); signals.json silhouette.edge wins per phase.")]
        public Color silhouetteEdge = new Color(1f, 1f, 0.949f);
        [Tooltip("The rim's width (m): the figure grown along its normals (Glint 0.045: 1.7 px at 1080p play zoom).")]
        public float silhouetteEdgeM = 0.045f;
        [Tooltip("Cover must stand this much nearer the camera than the person (m), so a figure never silhouettes over itself or a kerb its feet dip into.")]
        public float silhouetteMarginM = 0.35f;
        SilhouettePass silPass; Material silMat;

        static readonly int CapsId = Shader.PropertyToID("_DKCutCaps"), CapsSegId = Shader.PropertyToID("_DKCapsSeg"), CapsInfoId = Shader.PropertyToID("_DKCapsInfo"), RimId = Shader.PropertyToID("_DKCutRim"), RimLitId = Shader.PropertyToID("_DKCutRimLit");
        readonly Vector4[] capsSeg = new Vector4[MaxPoints], capsInfo = new Vector4[MaxPoints];

        static readonly int FocusId = Shader.PropertyToID("_DKCutFocus"), ViewId = Shader.PropertyToID("_DKCutView"),
            ShapeId = Shader.PropertyToID("_DKCutShape"), VeilId = Shader.PropertyToID("_DKCutVeil"), BackId = Shader.PropertyToID("_DKCutBack"), PointsId = Shader.PropertyToID("_DKCutPoints"), CleanId = Shader.PropertyToID("_DKCutClean");

        [Tooltip("Brief 020 (Glyph): a person's disc (SetPoints with a negative radius) is cut clean, not to the ghost, so no screen door falls over a figure; this is its feather (m), outward from the disc (Glyph 0.2.99).")]
        public float personFeather = 1.3f; // Glyph's 0.2.99 review 1b: ~100 px at 2x at play zoom (38.6 px/m), feathered outward from the disc (DK_Cutaway.hlsl)
        [Tooltip("Glyph's 0.2.94/0.2.97 reviews #1: what stays of a ghost leans by `streetVeil` at full fade towards the street it hides, as that street is lit (DK_Cutaway.hlsl: the paving's albedo under the sun at the hidden ground point, through the shadow map there, plus the ambient), so the dots sink into the paving instead of screening it. 0 = the wall's own colour (as built to 0.2.94).")]
        [Range(0f, 1f)] public float streetVeil = 0.9f; // renamed from `veil` (0.45 / #f2ead6 to 0.2.97) so scenes built before 0.2.98 take the new values
        [Tooltip("The street's albedo (sRGB) the veil takes: the civic paving's mean (pave_civic_v1.png, #e4daca), lit in the shader as flat ground at the hidden point.")]
        public Color streetAlbedo = new Color(0.894f, 0.855f, 0.792f); // #e4daca (pave_civic_v1.png mean)

        /// <summary>DK_CUT_POINTS in DK_Cutaway.hlsl: the shader's array is always this long.</summary>
        public const int MaxPoints = 12; // Glint 056 (2026-10-26): 8 fixed slots (4 agents, target or hover, door, cursor, guard) + the driver and Vale; DK_CUT_POINTS must match
        readonly Vector4[] points = new Vector4[MaxPoints]; // xyz world, w disc radius (m)
        int pointCount;
        readonly FadeCut[] cuts = new FadeCut[2 * MaxPoints + 1];

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
        /// <summary>Glint 056 (2026-10-26) test: whether a kept point lies within m (ground plane) of w.</summary>
        public bool HasPointNear(Vector3 w, float m) { for (int i = 0; i < pointCount; i++) { var d = new Vector3(points[i].x - w.x, 0f, points[i].z - w.z); if (d.sqrMagnitude <= m * m) return true; } return false; }
        /// <summary>How many points the shader takes (MaxPoints = DK_CUT_POINTS).</summary>
        public int Cap => MaxPoints;

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
                // Glint (brief 056 F §2): with capsules the camera passes cut only the capsules and the shadow map is not cut, so the reach is each
                // capsule's own conservative disc (about 5 m), not the 11.2 m focus disc and the point discs; a count of 0 reaches nothing
                int nc = 0;
                if (capsules) { for (int i = 0; i < pointCount; i++) cuts[nc++] = CapsuleCut(cut, points[i]); }
                else { cuts[nc++] = cut; for (int i = 0; i < pointCount; i++) { var q = points[i]; var d = cut; d.Focus = q; d.FloorY = q.y + (cut.FloorY - cut.Focus.y); d.Radius = Mathf.Abs(q.w); cuts[nc++] = d; } }
                reach.Set(cuts, nc, wholeTiles);
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
            Shader.SetGlobalVector(ShapeId, new Vector4(f, Mathf.Lerp(1f, ghost, level), column ? 1f : 0f, shadows && !capsules ? 1f : 0f)); // Glint (brief 056 F §2): with capsules a tower casts its full shadow (the shadow pass never discards)
            Shader.SetGlobalVector(BackId, new Vector4(cut.Back, f, Mathf.Lerp(1f, ghostShadow, level), pointCount));
            Shader.SetGlobalVectorArray(PointsId, points);
            var vc = streetAlbedo.linear; Shader.SetGlobalVector(VeilId, new Vector4(vc.r, vc.g, vc.b, streetVeil * level));
            Shader.SetGlobalVector(CleanId, new Vector4(1f - level, Mathf.Max(personFeather, 0.01f), level, 0f));
            Capsules();
            Silhouettes(c);
            if (reach == null) Shader.EnableKeyword(FadeReach.Keyword); // no reach: every tagged shader takes its cutting variant for this render
        }

        /// <summary>Brief 056 F: the capsules for this render, in the camera's view space (x, y on the screen in m, -z the linear eye depth):
        /// a person (SetPoints w &lt; 0) from the feet to personHeight at capsuleRadius, a ground point from the ground to pointHeight at
        /// pointRadius; the depth is the capsule's middle.</summary>
        void Capsules()
        {
            int n = capsules ? pointCount : 0;
            if (n > 0)
            {
                var v = cam.worldToCameraMatrix;
                for (int i = 0; i < n; i++)
                {
                    var q = points[i]; bool person = q.w < 0f; var foot = new Vector3(q.x, q.y, q.z); var head = foot + Vector3.up * (person ? personHeight : pointHeight);
                    Vector3 f = v.MultiplyPoint3x4(foot), h = v.MultiplyPoint3x4(head);
                    capsSeg[i] = new Vector4(f.x, f.y, h.x, h.y); capsInfo[i] = new Vector4(-(f.z + h.z) * 0.5f, person ? capsuleRadius : pointRadius, 0f, 0f);
                }
                Shader.SetGlobalVectorArray(CapsSegId, capsSeg); Shader.SetGlobalVectorArray(CapsInfoId, capsInfo);
            }
            Shader.SetGlobalVector(CapsId, new Vector4(capsules ? 1f : 0f, n, edgeInner, depthMargin));
            var (rimNow, rimCol, litNow) = SignalLight.CapsuleRim(rim, rimColor, rimLit); // Glint's signals.json `capsule` when present
            var rc = rimCol.linear; Shader.SetGlobalVector(RimId, new Vector4(rc.r, rc.g, rc.b, capsules ? rimNow * level : 0f));
            Shader.SetGlobalFloat(RimLitId, litNow);
        }

        /// <summary>Brief 056 F, for the reach: the cut a capsule can make, as a ground disc FadeGrid.Reaches understands (conservative): round
        /// the capsule's middle carried down its view ray to the point's level, its radius grown by half the capsule and stretched for the
        /// pitch, nothing below the point's level less a metre, and a little beyond the point.</summary>
        FadeCut CapsuleCut(in FadeCut cut, Vector4 q)
        {
            bool person = q.w < 0f; float h = person ? personHeight : pointHeight, r = person ? capsuleRadius : pointRadius;
            var c = cut; var mid = new Vector3(q.x, q.y, q.z);
            c.Focus = new Vector3(mid.x + cut.Run.x * h * 0.5f, mid.y, mid.z + cut.Run.y * h * 0.5f);
            float stretch = Mathf.Min(5f, Mathf.Sqrt(1f + cut.Run.sqrMagnitude)); // 1 / sin(pitch): |Run| is cot(pitch)
            c.Radius = (r + h * 0.5f) * stretch + 0.5f; c.FloorY = q.y - 1f; c.Back = h * cut.Run.magnitude * 0.5f + r + 0.5f;
            return c;
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

        void Silhouettes(Camera c)
        {
            if (!silhouettes || !Application.isPlaying) return;
            if (silPass == null) { silMat = SilhouettePass.MakeMaterial(); if (silMat == null) { silhouettes = false; Debug.LogWarning("ArchitectureFade: no silhouette shader (Resources/Shaders/Silhouette): silhouettes off"); return; } silPass = new SilhouettePass(silMat); }
            var (fill, edge, edgeM, margin) = SignalLight.SilhouetteNow(silhouetteFill, silhouetteEdge, silhouetteEdgeM, silhouetteMarginM); // Glint (brief 056 F §3): per phase, the grade's not the light's
            silPass.Look(fill, edge, edgeM, margin);
            c.GetUniversalAdditionalCameraData()?.scriptableRenderer?.EnqueuePass(silPass);
        }

        void Release()
        {
            if (silMat) { if (Application.isPlaying) Destroy(silMat); else DestroyImmediate(silMat); } silMat = null; silPass = null;
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
