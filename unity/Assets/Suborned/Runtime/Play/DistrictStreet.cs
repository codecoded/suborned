using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    // The civic host already owns the people on its roster. This draws additional residents and
    // scheduled street traffic near the camera, using the same figures, animations and vehicle kit.
    public sealed class DistrictStreet : MonoBehaviour
    {
        sealed class View
        {
            public GameObject root;
            public string skin;
            public Animator animator;
            public bool moving;
        }

        readonly Dictionary<int, View> activeViews = new();
        readonly Dictionary<string, Stack<View>> spare = new();
        readonly List<int> stale = new();
        readonly HashSet<int> visible = new();
        HandoverHost host;
        Game current;
        GameObject parkedTrams;
        float rescan;
        const int Tram = 1 << 20, Sedan = 2 << 20, Robot = 3 << 20;
        const int RigBudget = 28;
        public const float FarMetres = 155f;
        readonly HashSet<int> rigged = new();
        readonly List<(int id, float rank)> candidates = new();
        string[] skins;
        DistrictCrowdLod crowd;
        public int InstancedPeople { get; private set; }
        public int VisiblePeople { get; private set; }
        public int VisibleVehicles { get; private set; }
        public int VisibleRobots { get; private set; }
        public bool TramKit => host && host.districtTram && host.districtTramMid && host.districtTramFar;

        void Awake()
        {
            host = GetComponent<HandoverHost>();
            crowd = new DistrictCrowdLod(host.districtCrowd);
            parkedTrams = GameObject.Find("Trams (old Tram at our stops)");
        }

        void OnDestroy()
        {
            if (parkedTrams)
                parkedTrams.SetActive(true);
        }

        static string FigureSkin(FigureLibrary lib, DistrictResident person)
        {
            if (!lib) return null;
            string fallback = person.Job == "guard" ? "guard" : person.Job == "driver" ? "driver" : "passer";
            return lib.Pick(person.Look, person.Id) ?? lib.Pick("family/" + fallback, person.Id);
        }

        void LateUpdate()
        {
            var g = host.Game;
            if (g != current)
            {
                foreach (var item in activeViews.Values) Put(item);
                activeViews.Clear(); visible.Clear(); rigged.Clear();
                current = g;
                rescan = 0;
                host.frame.streetGround = g?.District?.Map.Ground;
                if (g?.District != null)
                    skins = System.Array.ConvertAll(g.District.Map.People, p => FigureSkin(host.figures, p));
                if (parkedTrams) parkedTrams.SetActive(g?.District == null);
            }
            if (g?.District == null || !host.View) return;
            var d = g.District;
            bool scan = Time.unscaledTime >= rescan;
            if (scan)
            {
                rescan = Time.unscaledTime + .18f;
                visible.Clear(); candidates.Clear(); rigged.Clear();
                void Consider(int id, Pt at)
                {
                    Vector3 v = host.View.WorldToViewportPoint(host.frame.World(at));
                    float limit = host.View.orthographic ? host.View.farClipPlane : FarMetres;
                    if (v.z <= 0 || v.z > limit || v.x < -.12f || v.x > 1.12f || v.y < -.14f || v.y > 1.14f) return;
                    visible.Add(id);
                    if (id >= Tram) return;
                    float rel = host.View.orthographic ? .9f / host.View.orthographicSize
                        : .9f / (v.z * Mathf.Tan(host.View.fieldOfView * .5f * Mathf.Deg2Rad));
                    if (rel >= .055f || !crowd.Has(skins[id]))
                        candidates.Add((id, rel - .01f * ((v.x - .5f) * (v.x - .5f) + (v.y - .5f) * (v.y - .5f))));
                }
                for (int i = 0; i < d.Map.People.Length; i++)
                {
                    if (g.HasCivicActor(d.Map.People[i].Id)) continue;
                    if (d.Network.FellAt[i] is Pt fell) Consider(i, fell);
                    else if (d.Roams(i) || d.Network.Away(i)) Consider(i, d.Pose(i, g.Tick).at);
                }
                for (int i = 0; i < d.Traffic.TramCount; i++) Consider(Tram + i, d.Traffic.Tram(i, g.Tick).At);
                for (int i = 0; i < d.Traffic.CarCount; i++)
                {
                    Pt at = d.Traffic.Car(i, g.Tick).At;
                    if (!g.S.Grid.InGrid(at) && d.Map.Roads.Free(at)) Consider(Sedan + i, at);
                }
                for (int i = 0; i < d.Traffic.RobotCount; i++) Consider(Robot + i, d.Traffic.Robot(i, g.Tick).At);
                candidates.Sort((a, b) => b.rank.CompareTo(a.rank));
                for (int i = 0; i < candidates.Count; i++)
                    if (i < RigBudget || !crowd.Has(skins[candidates[i].id])) rigged.Add(candidates[i].id);
                stale.Clear();
                foreach (var pair in activeViews)
                    if (!visible.Contains(pair.Key) || (pair.Key < Tram && !rigged.Contains(pair.Key))) stale.Add(pair.Key);
                foreach (int id in stale) { Put(activeViews[id]); activeViews.Remove(id); }
            }

            VisiblePeople = VisibleVehicles = VisibleRobots = InstancedPeople = 0;
            foreach (int id in visible)
            {
                bool fallen = false;
                Pt at, facing;
                bool moving;
                if (id < Tram)
                {
                    fallen = d.Network.FellAt[id].HasValue;
                    if (fallen) { at = d.Network.FellAt[id].Value; facing = new Pt(0, 100); moving = false; }
                    else (at, facing, moving) = d.Pose(id, g.Tick);
                }
                else
                {
                    var p = id >= Robot ? d.Traffic.Robot(id - Robot, g.Tick)
                        : id >= Sedan ? d.Traffic.Car(id - Sedan, g.Tick) : d.Traffic.Tram(id - Tram, g.Tick);
                    at = p.At; facing = p.Facing; moving = p.Moving;
                }
                Vector3 pos = host.frame.World(at), dir = host.frame.Dir(facing);
                Quaternion rotation = dir.sqrMagnitude > .01f ? Quaternion.LookRotation(dir) : Quaternion.identity;
                if (fallen) rotation *= Quaternion.Euler(0, 0, 85);
                if (id < Tram && !rigged.Contains(id))
                {
                    if (!crowd.Has(skins[id])) continue;
                    crowd.Add(skins[id], Matrix4x4.TRS(pos, rotation, Vector3.one), g.Tick, id, moving);
                    VisiblePeople++; InstancedPeople++;
                    continue;
                }
                if (!activeViews.TryGetValue(id, out var view))
                {
                    view = Rent(id);
                    if (view == null) continue;
                    activeViews.Add(id, view);
                }
                view.root.transform.SetPositionAndRotation(pos, rotation);
                if (view.animator && moving != view.moving)
                {
                    view.moving = moving;
                    view.animator.CrossFade(moving ? "walk" : "idle", .12f);
                }
                if (id < Tram) VisiblePeople++;
                else if (id >= Robot) VisibleRobots++;
                else VisibleVehicles++;
            }
            crowd.Draw();
        }

        // Diagnostic only. Count the selected mesh LOD once, independent of shadows and other render passes.
        public long VisibleStreetTriangles()
        {
            long sum = crowd.SubmittedTriangles;
            var camera = host.View;
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            foreach (var view in activeViews.Values)
            {
                var group = view.root.GetComponentInChildren<LODGroup>();
                var renderers = view.root.GetComponentsInChildren<Renderer>();
                if (group)
                {
                    var t = group.transform;
                    var scale = t.lossyScale;
                    float diameter = group.size * Mathf.Max(scale.x, scale.y, scale.z) * QualitySettings.lodBias;
                    float depth = Vector3.Distance(camera.transform.position, t.TransformPoint(group.localReferencePoint));
                    float halfHeight = camera.orthographic ? camera.orthographicSize : depth * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
                    renderers = System.Array.Empty<Renderer>();
                    foreach (var lod in group.GetLODs())
                        if (diameter >= 2 * halfHeight * lod.screenRelativeTransitionHeight) { renderers = lod.renderers; break; }
                }
                foreach (var r in renderers)
                {
                    if (!r || !r.enabled || !GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
                    var mesh = r is SkinnedMeshRenderer skinned ? skinned.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                    if (!mesh) continue;
                    for (int k = 0; k < mesh.subMeshCount; k++) sum += mesh.GetIndexCount(k) / 3;
                }
            }
            return sum;
        }

        void Put(View view)
        {
            view.root.SetActive(false);
            if (!spare.TryGetValue(view.skin, out var pool))
                spare[view.skin] = pool = new Stack<View>();
            pool.Push(view);
        }

        View Rent(int id)
        {
            GameObject prefab;
            FigureLibrary.Figure figure = null;
            string skin;
            if (id < Tram)
            {
                skin = skins[id];
                if (skin == null) return null;
                figure = host.figures.Find(skin);
                prefab = figure?.model;
            }
            else if (id >= Robot)
            {
                skin = "patrol-robot";
                prefab = host.patrolRobot ? host.patrolRobot : host.robotUnit;
            }
            else if (id >= Sedan)
            {
                int choice = (id - Sedan) % 3;
                skin = "sedan" + choice;
                prefab = choice == 0 ? host.sedanOfficial : choice == 1 ? host.sedanRival : host.sedanGetaway;
            }
            else
            {
                skin = "tram";
                prefab = host.districtTram;
            }

            if (!prefab)
                return null;
            if (spare.TryGetValue(skin, out var pool) && pool.Count > 0)
            {
                var used = pool.Pop();
                used.root.SetActive(true);
                return used;
            }

            var root = new GameObject(skin);
            root.transform.SetParent(transform, false);
            var model = Instantiate(prefab, root.transform, false);
            var v = new View
            {
                root = root,
                skin = skin
            };
            if (figure != null)
            {
                var lod = model.GetComponentInChildren<LODGroup>();
                if (lod && figure.lodHeights.Length > 0)
                {
                    var levels = lod.GetLODs();
                    for (int i = 0; i < levels.Length; i++)
                        levels[i].screenRelativeTransitionHeight = i == levels.Length - 1 ? .006f : Mathf.Max(.13f, figure.lodHeights[Mathf.Min(i, figure.lodHeights.Length - 1)]);
                    lod.SetLODs(levels);
                }

                var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
                anim.runtimeAnimatorController = host.figures.ControllerFor(figure);
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.CullCompletely;
                v.animator = anim;
            }
            else if (skin == "tram")
                DistrictVehicleLod.AddKitLevels(root, model, host.districtTramMid, host.districtTramFar);
            else if (id >= Robot)
                DistrictVehicleLod.AddKitLevels(root, model, host.districtRobotMid, host.districtRobotFar);
            else
                DistrictVehicleLod.AddSedan(root, model);
            return v;
        }
    }
}
