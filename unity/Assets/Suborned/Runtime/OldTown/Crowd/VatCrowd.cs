using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Suborned.OldTown.Crowd
{
    /// <summary>The realtime crowd test: agents wandering the <see cref="VatWalkGraph"/> built from the scene's placed
    /// people (hidden while it runs), each wearing one of the <see cref="VatCitizen"/>s in turn, walking or running
    /// (a fixed share by each agent's roll, so the slider tips agents between the two without reshuffling) at the
    /// clip's stride speed with a small jitter, played back at the matching rate so the feet don't skate. Agents
    /// outside the camera's frustum (padded for their shadows) are not drawn; the rest go to the GPU in one instanced
    /// call per citizen and LOD, plus one for their contact shadows. Three LOD bakes, same order and clips: near
    /// (nearCitizens, 8k), mid (citizens, 3k), far (farCitizens, 800). Under the orthographic 2.5D camera every agent is
    /// the same size on screen, so the level is one pick per zoom (near at or under nearSize, far past farSize: at
    /// tactical zoom a 3k figure is ~20 px, all sub-pixel triangles). In first person it's per agent by distance (a
    /// figure is ~1575/d px tall at 62 deg, 1080p: near inside nearDistance, far past farDistance, none past
    /// cullDistance), and only the near and mid levels cast into the shadow cascades.</summary>
    public sealed class VatCrowd : MonoBehaviour
    {
        static readonly string[] NodePrefixes = { "RFM_People", "RFM_CityCommuter" };  // DecoKitCitizens' people
        const float Turn = 540f, Pad = 3f, Offset = .55f;
        const int Near = 0, Mid = 1, Far = 2, Levels = 3;

        [SerializeField] VatCitizen[] citizens, nearCitizens, farCitizens;
        [SerializeField] float nearSize = 5f, farSize = 20f;                               // ortho half-height, m
        [SerializeField] float nearDistance = 6f, farDistance = 25f, cullDistance = 250f;  // perspective, m
        [SerializeField] Shader shader, blobShader;
        [SerializeField] int capacity = 12000;
        [SerializeField] int agents = 3000;
        [SerializeField, Range(0f, 1f)] float runShare = .1f;
        [SerializeField] bool shadows = true, contactShadows = true, separate = true;
        [SerializeField] float reach = 7f, step = .3f;
        [SerializeField] int maxLinks = 5;
        [SerializeField] LayerMask blockers = ~0;
        [SerializeField] int seed = 7;

        struct Agent
        {
            public int from, to, citizen, clip;
            public float t, len, jitter, roll, phase, yaw, scale, pace;  // pace: share of stride speed (separation)
            public Vector3 pos;
            public Vector3 offset;  // a fixed sidestep off the link line, so agents on one link don't walk single file
        }

        Agent[] crowd;
        VatSeparation separation;
        VatCrowdRenderer[][] renderers;  // [level][citizen], null for a level without bakes
        Vector4[][] poses, anims;
        int[] counts, walkClip, runClip;
        float[] radii;
        int[][] tris;
        readonly List<GameObject> hidden = new();
        readonly Plane[] planes = new Plane[6];
        Vector3[] lo, hi;
        System.Random rng;

        public VatWalkGraph Graph { get; private set; }
        public int Capacity => capacity;
        public int Drawn { get; private set; }
        public long DrawnTris { get; private set; }
        public int Agents { get => agents; set => agents = Mathf.Clamp(value, 0, capacity); }
        public float RunShare { get => runShare; set => runShare = Mathf.Clamp01(value); }
        public bool Shadows { get => shadows; set => shadows = value; }
        public bool ContactShadows { get => contactShadows; set => contactShadows = value; }
        public bool Separate { get => separate; set => separate = value; }
        public float TimeScale { get; set; } = 1f;
        public IReadOnlyList<VatCitizen> Citizens => citizens;
        public int[] DrawnPerLevel { get; } = new int[Levels];  // near, mid, far
        public float NearSize { get => nearSize; set => nearSize = value; }
        public float FarSize { get => farSize; set => farSize = value; }

        /// <summary>The live agent nearest `at` (-1 if none), and where it stands.</summary>
        public int Nearest(Vector3 at, int citizen = -1)
        {
            int best = -1;
            float d = float.MaxValue;
            for (int i = 0; crowd != null && i < agents; i++)
                if ((citizen < 0 || crowd[i].citizen == citizen) && (crowd[i].pos - at).sqrMagnitude < d)
                    (best, d) = (i, (crowd[i].pos - at).sqrMagnitude);
            return best;
        }

        public Vector3 Position(int agent) => crowd[agent].pos;
        public float Radius(int agent) => radii[crowd[agent].citizen] * crowd[agent].scale;
        public float Pace(int agent) => crowd[agent].pace;

        void OnEnable()
        {
            if (Application.isPlaying) Prime();
        }

        void OnDisable() => Release();

        void Update() => Tick(Time.deltaTime * TimeScale, Camera.main);

        /// <summary>Builds the graph from the placed people (then hides them), the agents and the GPU buffers.</summary>
        public void Prime()
        {
            Release();
            rng = new System.Random(seed);
            var people = FindObjectsByType<Transform>(FindObjectsInactive.Exclude)
                         .Where(t => IsPerson(t) && (t.parent == null || !IsPerson(t.parent))).ToList();
            var points = people.Select(t => t.position).ToList();
            foreach (var p in people)  // first, so no placed person's collider blocks a link
            {
                p.gameObject.SetActive(false);
                hidden.Add(p.gameObject);
            }
            Physics.SyncTransforms();
            Graph = VatWalkGraph.Build(points, reach, step, maxLinks, blockers);
            var live = Enumerable.Range(0, Graph.nodes.Count).Where(i => Graph.links[i].Length > 0).ToArray();
            if (live.Length == 0)
            {
                Debug.LogWarning($"[VatCrowd] no walkable links among {people.Count} placed people: no crowd");
                return;
            }
            int n = citizens.Length;
            walkClip = citizens.Select(c => c.ClipIndex("walk")).ToArray();
            runClip = citizens.Select(c => Mathf.Max(0, c.ClipIndex("run"))).ToArray();
            var sets = new[] { Usable(nearCitizens, "near"), citizens, Usable(farCitizens, "far") };
            renderers = sets.Select(set => set?.Select(c => new VatCrowdRenderer(c, shader, blobShader, capacity)).ToArray()).ToArray();
            tris = sets.Select(set => set?.Select(Tris).ToArray()).ToArray();
            poses = Enumerable.Range(0, Levels * n).Select(_ => new Vector4[capacity]).ToArray();  // slot = level * n + citizen
            anims = Enumerable.Range(0, Levels * n).Select(_ => new Vector4[capacity]).ToArray();
            counts = new int[Levels * n];
            (lo, hi) = (new Vector3[Levels * n], new Vector3[Levels * n]);
            radii = citizens.Select(c => c.Radius("walk")).ToArray();
            separation = new VatSeparation(capacity, radii.Max() * 1.05f);  // agent scale runs to 1.05
            crowd = new Agent[capacity];
            for (int i = 0; i < capacity; i++)
            {
                ref var a = ref crowd[i];
                a.from = live[rng.Next(live.Length)];
                a.to = Graph.Next(a.from, -1, rng);
                a.len = Mathf.Max(.01f, Vector3.Distance(Graph.nodes[a.from], Graph.nodes[a.to]));
                a.t = R(0f, a.len);
                a.citizen = i % n;
                a.jitter = R(.88f, 1.1f);
                a.roll = (float)rng.NextDouble();
                a.phase = (float)rng.NextDouble();
                a.scale = R(.95f, 1.05f);
                a.pace = 1f;
                float ang = R(0f, 2f * Mathf.PI), r = Mathf.Sqrt(R(0f, 1f)) * Offset;
                a.offset = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r;
                var dir = Graph.nodes[a.to] - Graph.nodes[a.from];
                a.yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            }
            Step(0f);
            Debug.Log($"[VatCrowd] {people.Count} placed people -> {Graph.nodes.Count} nodes, {Graph.Links} links, " +
                      $"{live.Length} walkable; {capacity} agents over {n} citizens");
        }

        static bool IsPerson(Transform t) => NodePrefixes.Any(p => t.name.StartsWith(p));

        static int Tris(VatCitizen c) => Enumerable.Range(0, c.mesh.subMeshCount).Sum(s => (int)c.mesh.GetIndexCount(s) / 3);

        VatCitizen[] Usable(VatCitizen[] set, string level)
        {
            if (set == null || set.Length == 0) return null;
            if (set.Length == citizens.Length && set.Zip(citizens, SameClips).All(x => x)) return set;
            Debug.LogWarning($"[VatCrowd] {level} LODs don't match the citizens' clips: left out");
            return null;
        }

        int OrthoLevel(float size) =>
            size <= nearSize && renderers[Near] != null ? Near : size > farSize && renderers[Far] != null ? Far : Mid;

        static bool SameClips(VatCitizen far, VatCitizen near) =>
            far.clips.Length == near.clips.Length && far.clips.Zip(near.clips, (a, b) => a.name == b.name && a.start == b.start && a.frames == b.frames).All(x => x);

        float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();

        /// <summary>Advances the crowd by dt, then submits it for the frame's cameras, culled against `view`.</summary>
        public void Tick(float dt, Camera view)
        {
            Step(dt);
            Submit(view);
        }

        public void Step(float dt)
        {
            if (crowd == null) return;
            for (int i = 0; i < agents; i++)
            {
                ref var a = ref crowd[i];
                a.clip = a.roll < runShare ? runClip[a.citizen] : walkClip[a.citizen];
                var clip = citizens[a.citizen].clips[a.clip];
                a.t += clip.speed * a.jitter * a.pace * dt;
                while (a.t >= a.len)
                {
                    a.t -= a.len;
                    int next = Graph.Next(a.to, a.from, rng);
                    (a.from, a.to) = (a.to, next);
                    a.len = Mathf.Max(.01f, Vector3.Distance(Graph.nodes[a.from], Graph.nodes[a.to]));
                }
                a.phase += dt * a.jitter * a.pace / clip.Length;  // slowed, the stride shortens with it: no skating
                a.phase -= Mathf.Floor(a.phase);
                Vector3 p0 = Graph.nodes[a.from], p1 = Graph.nodes[a.to], dir = p1 - p0;
                if (dir.x * dir.x + dir.z * dir.z > 1e-4f)
                    a.yaw = Mathf.MoveTowardsAngle(a.yaw, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, Turn * dt);
                a.pos = Vector3.Lerp(p0, p1, a.t / a.len) + a.offset;
            }
            if (!separate || separation == null || dt <= 0f)
            {
                if (!separate) for (int i = 0; i < agents; i++) crowd[i].pace = 1f;
                return;
            }
            for (int i = 0; i < agents; i++) separation.Place(i, crowd[i].pos, radii[crowd[i].citizen] * crowd[i].scale, crowd[i].yaw);
            separation.Relax(agents, dt);
            for (int i = 0; i < agents; i++)
            {
                crowd[i].pos += separation.Offset(i);
                crowd[i].pace = separation.Pace(i);
            }
        }

        /// <summary>One instanced draw per citizen and LOD for this frame (call once per frame, after <see cref="Step"/>).</summary>
        public void Submit(Camera view)
        {
            if (crowd == null) return;
            if (view != null) GeometryUtility.CalculateFrustumPlanes(view, planes);
            System.Array.Clear(counts, 0, counts.Length);
            for (int s = 0; s < lo.Length; s++) (lo[s], hi[s]) = (Vector3.positiveInfinity, Vector3.negativeInfinity);
            bool ortho = view == null || view.orthographic;
            int fixedLevel = view == null ? Mid : ortho ? OrthoLevel(view.orthographicSize) : -1, n = citizens.Length;
            var eye = view != null ? view.transform.position : Vector3.zero;
            float near2 = renderers[Near] != null ? nearDistance * nearDistance : -1f, far2 = renderers[Far] != null ? farDistance * farDistance : float.MaxValue;
            float cull2 = cullDistance * cullDistance;
            var box = new Bounds(Vector3.zero, Vector3.one * (2f + Pad * 2f));
            for (int i = 0; i < agents; i++)
            {
                ref var a = ref crowd[i];
                box.center = a.pos + Vector3.up * .9f;
                if (view != null && !GeometryUtility.TestPlanesAABB(planes, box)) continue;
                int level = fixedLevel;
                if (level < 0)
                {
                    float d2 = (a.pos - eye).sqrMagnitude;
                    if (d2 > cull2) continue;
                    level = d2 < near2 ? Near : d2 < far2 ? Mid : Far;
                }
                var clip = citizens[a.citizen].clips[a.clip];
                int slot = level * n + a.citizen, k = counts[slot]++;
                lo[slot] = Vector3.Min(lo[slot], a.pos);
                hi[slot] = Vector3.Max(hi[slot], a.pos);
                float f = a.phase * clip.frames;
                int fa = Mathf.FloorToInt(f) % clip.frames;
                poses[slot][k] = new Vector4(a.pos.x, a.pos.y, a.pos.z, a.yaw * Mathf.Deg2Rad);
                anims[slot][k] = new Vector4(clip.start + fa, clip.start + (fa + 1) % clip.frames, f - Mathf.Floor(f), a.scale);
            }
            Drawn = 0;
            DrawnTris = 0;
            System.Array.Clear(DrawnPerLevel, 0, Levels);
            for (int s = 0; s < counts.Length; s++)
            {
                int level = s / n, c = s % n;
                if (counts[s] == 0) continue;
                DrawnPerLevel[level] += counts[s];
                Drawn += counts[s];
                DrawnTris += (long)counts[s] * tris[level][c];
                var b = new Bounds();
                b.SetMinMax(lo[s] - new Vector3(1f, 0f, 1f), hi[s] + new Vector3(1f, 2.2f, 1f));  // a figure's reach
                // far figures in first person: past the cascades' useful reach, the contact disc is enough
                renderers[level][c].Draw(poses[s], anims[s], counts[s], b, shadows && (ortho || level != Far), contactShadows);
            }
        }

        public void Release()
        {
            if (renderers != null)
                foreach (var r in renderers.Where(set => set != null).SelectMany(set => set)) r.Dispose();
            renderers = null;
            crowd = null;
            separation?.Dispose();
            separation = null;
            foreach (var go in hidden)
                if (go != null) go.SetActive(true);
            hidden.Clear();
        }
    }
}
