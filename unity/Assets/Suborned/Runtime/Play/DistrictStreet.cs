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
            public float height;
        }

        readonly Dictionary<int, View> activeViews = new();
        readonly Dictionary<string, Stack<View>> spare = new();
        readonly List<int> stale = new();
        readonly HashSet<int> visible = new();
        HandoverHost host;
        Game current;
        GameObject parkedTrams;
        float rescan;
        const int Tram = 1000, Sedan = 1100, Robot = 1200;
        public int VisiblePeople { get; private set; }
        public int VisibleVehicles { get; private set; }
        public int VisibleRobots { get; private set; }
        public bool TramKit => host && host.districtTram && host.districtTramMid && host.districtTramFar;

        void Awake()
        {
            host = GetComponent<HandoverHost>();
            parkedTrams = GameObject.Find("Trams (old Tram at our stops)");
        }

        void OnDestroy()
        {
            if (parkedTrams)
                parkedTrams.SetActive(true);
        }

        void LateUpdate()
        {
            var g = host.Game;
            if (g != current)
            {
                foreach (var item in activeViews.Values)
                    Put(item);
                activeViews.Clear();
                current = g;
                rescan = 0;
                if (parkedTrams)
                    parkedTrams.SetActive(g?.District == null);
            }

            if (g?.District == null || !host.View)
                return;
            bool scan = Time.unscaledTime >= rescan;
            if (scan)
            {
                rescan = Time.unscaledTime + .2f;
                visible.Clear();
                VisiblePeople = VisibleVehicles = VisibleRobots = 0;
            }

            var d = g.District;
            for (int i = 0; i < d.Map.People.Length; i++)
            {
                var p = d.Map.People[i];
                if (host.Game.HasCivicActor(p.Id) || !d.Roams(i))
                    continue;
                if (!scan && !activeViews.ContainsKey(i))
                    continue;
                var pose = d.Pose(i, g.Tick);
                Draw(i, pose.at, pose.facing, pose.moving, scan);
            }

            for (int i = 0; i < d.Map.Trams.Length; i++)
            {
                var pose = d.Traffic.Tram(i, g.Tick);
                Draw(Tram + i, pose.At, pose.Facing, pose.Moving, scan);
            }

            for (int i = 0; i < d.Map.Streets.Length; i++)
            {
                if (!scan && !activeViews.ContainsKey(Sedan + i) && !activeViews.ContainsKey(Robot + i))
                    continue;
                var pose = d.Traffic.Car(i, g.Tick);
                if (!g.S.Grid.InGrid(pose.At) && d.Map.Roads.Free(pose.At))
                    Draw(Sedan + i, pose.At, pose.Facing, pose.Moving, scan);
                if (i % 2 == 0)
                {
                    var patrol = DistrictMap.Shuttle(d.Map.People[(i * 7 + 3) % d.Map.People.Length].Walk, g.Tick, i * 1091, 130, 170);
                    Draw(Robot + i, patrol.at, patrol.facing, patrol.moving, scan);
                }
            }

            if (scan)
            {
                stale.Clear();
                foreach (var p in activeViews)
                    if (!visible.Contains(p.Key))
                        stale.Add(p.Key);
                foreach (int k in stale)
                {
                    Put(activeViews[k]);
                    activeViews.Remove(k);
                }
            }
        }

        void Draw(int id, Pt at, Pt heading, bool moving, bool scan)
        {
            Vector3 pos = host.frame.World(at);
            if (scan)
            {
                var v = host.View.WorldToViewportPoint(pos);
                if (v.z < 0 || v.z > 160 || v.x < -.2f || v.x > 1.2f || v.y < -.2f || v.y > 1.2f)
                    return;
                visible.Add(id);
                if (id < Tram)
                    VisiblePeople++;
                else if (id >= Robot)
                    VisibleRobots++;
                else
                    VisibleVehicles++;
            }

            if (!activeViews.TryGetValue(id, out var view))
            {
                if (!scan)
                    return;
                view = Rent(id);
                if (view == null)
                    return;
                activeViews.Add(id, view);
            }

            view.root.transform.position = pos + Vector3.up * view.height;
            Vector3 dir = host.frame.Dir(heading);
            if (dir.sqrMagnitude > .01f)
                view.root.transform.rotation = Quaternion.LookRotation(dir);
            if (view.animator && moving != view.moving)
            {
                view.moving = moving;
                view.animator.CrossFade(moving ? "walk" : "idle", .12f);
            }
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
            var d = host.Game.District;
            GameObject prefab;
            FigureLibrary.Figure figure = null;
            string skin;
            if (id < Tram)
            {
                var p = d.Map.People[id];
                var lib = host.figures;
                if (!lib)
                    return null;
                string family = p.Job == "server" || p.Job == "cook" ? "cafe" : p.Job == "clerk" ? "office" : p.Job == "guard" ? "guard" : "passer";
                skin = lib.Pick("job/" + p.Job, p.Id) ?? lib.Pick("family/" + family, p.Id);
                figure = lib.Find(skin);
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
                        levels[i].screenRelativeTransitionHeight = figure.lodHeights[Mathf.Min(i, figure.lodHeights.Length - 1)];
                    lod.SetLODs(levels);
                }

                var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
                anim.runtimeAnimatorController = host.figures.ControllerFor(figure);
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.CullCompletely;
                v.animator = anim;
            }
            else if (skin == "tram" && host.districtTramMid && host.districtTramFar)
            {
                var mid = Instantiate(host.districtTramMid, root.transform, false);
                var far = Instantiate(host.districtTramFar, root.transform, false);
                var group = root.AddComponent<LODGroup>();
                group.SetLODs(new[] { new LOD(.18f, model.GetComponentsInChildren<Renderer>()), new LOD(.06f, mid.GetComponentsInChildren<Renderer>()), new LOD(.008f, far.GetComponentsInChildren<Renderer>()) });
                group.RecalculateBounds();
            }

            return v;
        }
    }
}
