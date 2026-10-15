using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    /// <summary>What the host draws from the engine each frame (brief 011 D, graybox): the cell's four operatives, the four cast roles, the
    /// patrol, the rivals, the response unit, the official and getaway cars, and the markers a click can hit (the public door, the Tech's
    /// cabinet, the two cameras). People are capsules as the people layer draws them (1.75 m, ivory head); signals are unlit lines on the ground
    /// (§10: cones red, guard amber, rivals violet): the cameras' cones, the guard's and the rivals' sight. Positions are the engine's, eased
    /// between its 10 Hz ticks. Nothing here feeds back into the engine.</summary>
    public sealed class HostViews
    {
        public const float Tall = 1.75f, Radius = 0.28f, HeadD = 0.3f;
        readonly Transform root; readonly SiteFrame frame; readonly HandoverHost host;
        readonly Material head, ring, select, red, amber, violet, grey;
        readonly Dictionary<string, (Transform t, Vector3 from, Vector3 to, bool shown)> things = new();
        readonly Dictionary<string, LineRenderer> lines = new();
        static Mesh capsule, sphere, cylinder, cube;

        public HostViews(HandoverHost host, Transform root, SiteFrame frame)
        {
            this.host = host; this.root = root; this.frame = frame;
            capsule ??= Prim(PrimitiveType.Capsule); sphere ??= Prim(PrimitiveType.Sphere); cylinder ??= Prim(PrimitiveType.Cylinder); cube ??= Prim(PrimitiveType.Cube);
            head = Lit(host.headColour); ring = Unlit(Color.white); select = Unlit(host.selectColour);
            red = Unlit(host.coneColour); amber = Unlit(host.guardColour); violet = Unlit(host.rivalColour); grey = Unlit(new Color(0.55f, 0.55f, 0.55f));
            var g = host.Game; var s = g.S;
            for (int i = 0; i < 4; i++) Person("op/" + Op.Name((OpId)i), host.cellColour, true);
            Person("waitress", host.waitressColour); Person("driver", host.civicColour); Person("guard", host.civicColour); Person("councillor", host.civicColour);
            Person("patrol", host.securityColour);
            for (int i = 0; i < 3; i++) Person("rivals/" + i, host.rivalBodyColour);
            for (int i = 0; i < 2; i++) Person("security/" + i, host.securityColour);
            Box("car/official", new Vector3(1.8f, 1.4f, 4.6f), host.carColour, true);
            Box("car/getaway", new Vector3(1.8f, 1.4f, 4.6f), host.getawayColour, true);
            Marker("door", frame.World(s.At(s.GuardPost)) + Vector3.up * 1.2f, new Vector3(2.4f, 2.4f, 2.4f));
            Marker("cabinet", frame.World(s.At("cabinet")) + Vector3.up * 0.7f, new Vector3(1.4f, 1.4f, 1.4f));
            foreach (var c in s.Cameras)
            {
                var at = frame.World(c.At);
                var pole = Part(root, "camera/" + c.Id, cylinder, Lit(host.carColour), at + Vector3.up * 1.6f, new Vector3(0.25f, 1.6f, 0.25f));
                var col = pole.gameObject.AddComponent<CapsuleCollider>(); col.radius = 2.5f; col.height = 2f; // a fat target, pole scale makes it ~1.2 m
                things["camera/" + c.Id] = (pole, pole.position, pole.position, true);
                Line("cone/" + c.Id, red);
            }
            Line("cone/guard", amber); Line("cone/rivals", violet);
            for (int i = 0; i < g.Walkers.Count; i++) Person("crowd/" + i, host.JobColour(g.Walkers[i].P.Job), false, false);
            for (int i = 0; i < MaxHostiles; i++) Person("hostile/" + i, host.securityColour);
            Ring("sel/0"); Ring("sel/1"); Ring("sel/2"); Ring("sel/3");
        }

        public Transform Find(string key) => things.TryGetValue(key, out var x) ? x.t : null;
        const int MaxHostiles = 8;
        readonly Dictionary<Pt, int> stacked = new();

        /// <summary>Sets where each thing is after a tick (the frame eases from the previous one).</summary>
        public void Tick()
        {
            var g = host.Game; var s = g.S;
            for (int i = 0; i < 4; i++) { var op = g.Ops[i]; Set("op/" + Op.Name((OpId)i), op.Inside ? null : op.Pos); }
            Set("waitress", g.PersonPos(g.Inez), g.Inez.Down); Set("driver", g.DriverPos(), g.Driver.Down);
            Set("guard", g.GuardPose()?.pos, g.GuardDown); Set("councillor", g.ValePos(), g.ValeP.Down);
            var pp = g.PatrolPose(); Set("patrol", pp?.pos, g.PatrolDown);
            var rp = g.RivalsPose();
            for (int i = 0; i < 3; i++) Set("rivals/" + i, rp.HasValue ? rp.Value.pos + new Pt(i == 1 ? 90 : 0, i == 2 ? 90 : 0) : (Pt?)null);
            var sp = g.SecurityPose();
            for (int i = 0; i < 2; i++) Set("security/" + i, sp.HasValue ? sp.Value.pos + new Pt(0, i * 90) : (Pt?)null);
            Set("car/official", g.Vale == ValeAt.Car && g.Result != Outcome.Success ? (Pt?)null : g.CarPos());
            Set("car/getaway", s.At(s.GetawayCar));
            // the crowd: people the engine puts on one place point are fanned out on a 0.5 m sunflower for the eye only (the engine is not told)
            stacked.Clear();
            for (int i = 0; i < g.Walkers.Count; i++)
            {
                var at = g.CrowdPos(g.Walkers[i]);
                if (!at.HasValue) { Set("crowd/" + i, null); continue; }
                int k = stacked.TryGetValue(at.Value, out var n) ? n + 1 : 0; stacked[at.Value] = k;
                float r = 50f * Mathf.Sqrt(k), th = k * 2.399963f;
                Set("crowd/" + i, at.Value + new Pt(Mathf.RoundToInt(r * Mathf.Cos(th)), Mathf.RoundToInt(r * Mathf.Sin(th))));
            }
            // the final fight's hostiles (officers in the loud branch, the rival ambush in the safe one)
            for (int i = 0; i < MaxHostiles; i++)
            {
                var h = i < g.Hostiles.Count ? g.Hostiles[i] : null;
                Set("hostile/" + i, h != null && h.Present && !h.Down ? h.Pos(g.Tick) : (Pt?)null);
                if (h != null) Tint("hostile/" + i, h.Id.StartsWith("rival") ? host.rivalBodyColour : host.securityColour);
            }
        }

        void Tint(string key, Color c)
        {
            var r = things[key].t.Find("body")?.GetComponent<MeshRenderer>(); if (!r) return;
            if (r.sharedMaterial.GetColor("_BaseColor") != c) r.sharedMaterial.SetColor("_BaseColor", c);
        }

        /// <summary>Per frame: eases positions, draws the cones and the selection rings.</summary>
        public void Frame(float alpha)
        {
            foreach (var kv in things)
            {
                var (t, from, to, shown) = kv.Value;
                if (t.gameObject.activeSelf != shown) t.gameObject.SetActive(shown);
                if (shown && !kv.Key.StartsWith("camera/") && !kv.Key.StartsWith("marker/")) t.position = Vector3.Lerp(from, to, alpha);
            }
            var g = host.Game; var s = g.S;
            foreach (var c in s.Cameras)
            {
                bool live = c.Id == "entrance" ? !g.LoopOn : g.PickupCameraLive();
                Cone("cone/" + c.Id, c.At, c.Facing, c.Reach, c.Half, live ? red : grey);
            }
            var gp = g.GuardPose();
            if (gp.HasValue) { var (pos, f, phase) = gp.Value; bool door = phase == "door"; Cone("cone/guard", pos, f, door ? g.R.DoorReachCm : g.R.FacingReachCm, door ? g.R.DoorHalfCm : g.R.FacingHalfCm, amber); }
            else Hide("cone/guard");
            var rp = g.RivalsPose();
            if (rp.HasValue && g.RivalsDrawn) Cone("cone/rivals", rp.Value.pos, rp.Value.facing, g.R.RivReachCm, g.R.RivHalfCm, violet);
            else Hide("cone/rivals");
            for (int i = 0; i < 4; i++)
            {
                var r = things["sel/" + i].t; var op = things["op/" + Op.Name((OpId)i)];
                bool on = host.Selected[i] && op.shown;
                if (r.gameObject.activeSelf != on) r.gameObject.SetActive(on);
                if (on) r.position = op.t.position + Vector3.up * 0.03f;
            }
        }

        void Set(string key, Pt? at, bool down = false)
        {
            var x = things[key];
            if (!at.HasValue) { things[key] = (x.t, x.to, x.to, false); return; }
            var w = frame.World(at.Value);
            bool wasShown = x.shown;
            things[key] = (x.t, wasShown ? x.to : w, w, true);
            var body = x.t.Find("body");
            if (body) x.t.localRotation = down ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity; // down: laid flat
        }

        void Person(string key, Color body, bool cell = false, bool collide = true)
        {
            var go = new GameObject(key); go.transform.SetParent(root, false);
            float bodyH = Tall - HeadD - 0.03f;
            Part(go.transform, "body", capsule, Lit(body), new Vector3(0, bodyH / 2, 0), new Vector3(Radius * 2, bodyH / 2, Radius * 2));
            Part(go.transform, "head", sphere, cell ? Lit(host.cellHeadColour) : head, new Vector3(0, Tall - HeadD / 2, 0), Vector3.one * HeadD);
            if (collide) { var col = go.AddComponent<CapsuleCollider>(); col.height = Tall; col.radius = Radius + 0.12f; col.center = new Vector3(0, Tall / 2, 0); } // the crowd is not a target (orders name roles)
            go.SetActive(false);
            things[key] = (go.transform, Vector3.zero, Vector3.zero, false);
        }

        void Box(string key, Vector3 size, Color c, bool collide)
        {
            var go = new GameObject(key); go.transform.SetParent(root, false);
            Part(go.transform, "body", cube, Lit(c), new Vector3(0, size.y / 2, 0), size);
            if (collide) { var col = go.AddComponent<BoxCollider>(); col.size = size; col.center = new Vector3(0, size.y / 2, 0); }
            go.SetActive(false);
            things[key] = (go.transform, Vector3.zero, Vector3.zero, false);
        }

        void Marker(string key, Vector3 at, Vector3 size)
        {
            var go = new GameObject("marker/" + key); go.transform.SetParent(root, false); go.transform.position = at;
            var col = go.AddComponent<BoxCollider>(); col.size = size;
            things["marker/" + key] = (go.transform, at, at, true);
        }

        void Ring(string key)
        {
            var t = Part(root, key, cylinder, select, Vector3.zero, new Vector3(1.1f, 0.01f, 1.1f));
            t.gameObject.SetActive(false);
            things[key] = (t, Vector3.zero, Vector3.zero, false);
        }

        void Line(string key, Material m)
        {
            var go = new GameObject(key); go.transform.SetParent(root, false);
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = m; lr.useWorldSpace = true; lr.loop = true; lr.positionCount = 3;
            lr.widthMultiplier = 0.14f; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false; lr.alignment = LineAlignment.TransformZ;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // lie flat on the ground
            lines[key] = lr;
        }

        /// <summary>A sight cone as the engine tests it (Geo.InCone): ahead up to reach, half `half` wide at its end.</summary>
        void Cone(string key, Pt at, Pt facing, int reach, int half, Material m)
        {
            var lr = lines[key]; if (!lr.enabled) lr.enabled = true;
            if (lr.sharedMaterial != m) lr.sharedMaterial = m;
            var o = frame.World(at) + Vector3.up * 0.06f; var f = frame.Dir(facing); var side = new Vector3(-f.z, 0f, f.x);
            lr.SetPosition(0, o); lr.SetPosition(1, o + f * (reach / 100f) + side * (half / 100f)); lr.SetPosition(2, o + f * (reach / 100f) - side * (half / 100f));
        }
        void Hide(string key) { if (lines[key].enabled) lines[key].enabled = false; }

        static Transform Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            return go.transform;
        }

        static Mesh Prim(PrimitiveType t) { var g = GameObject.CreatePrimitive(t); var m = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g); return m; }

        // from the templates the scene holds (PlayInstall), so a player build carries their shaders
        Material Lit(Color c) { var m = new Material(host.litTemplate) { enableInstancing = true }; m.SetColor("_BaseColor", c); return m; }
        Material Unlit(Color c) { var m = new Material(host.unlitTemplate); m.SetColor("_BaseColor", c); return m; }
    }
}
