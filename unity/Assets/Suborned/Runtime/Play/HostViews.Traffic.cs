using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 098 step 1 (law L8; Sky 2026-11-07: "our roads are empty, no reason can't have passing traffic"): the passing cars drawn from the
    /// engine's one read, Game.TrafficNow() (Keel, GameTrafficHost.cs), a view per car on the roads, keyed car/&lt;slot&gt;-&lt;trip&gt; as the engine's target
    /// id is car:&lt;slot&gt;-&lt;trip&gt;, so the fire order, the car effects (HostCarFx), the pick and the hover treat it as they treat the mission cars.
    ///  · The model by the engine's kind. A sedan is one of the mission cars' own bodies (Sedan_Official, Sedan_Getaway) repainted in Pulse's palette
    ///    (presentation.json `paints`: the kit's DK_CarPaint_* materials, kit.json carPaints' rule: the body and roof slots swapped, everything else as
    ///    built), the body and the paint picked by the car's id, so neighbours differ and a replay draws the same street; never a paint that would pass for
    ///    Vale's car or ours (TrafficKit: tropical-turquoise, dusk-pink and tangerine are kept off), and
    ///    never the rivals' plum Sedan_Rival (it would read as the rival car before 2:35). A van is the old town's box truck (BoxTruck_Play is still a
    ///    candidate, not in the kit). A scooter is Vela's hover scooter, 0.35 m up with a slow bob. No kit copied: the box, as the mission cars.
    ///  · Eased between the 10 Hz ticks as every view (Set), the heading too (the engine's own, never (0, 0)). The click box is the car's footprint
    ///    and turns with it. The kit cars are one mesh each (the wheels are part of the body), so nothing spins.
    ///  · A car that leaves the roads gives its view back for the next trip of the same model: once the street is warm a trip instantiates nothing.
    ///  · A driver who gets out is a walker of the crowd from then on (GameTrafficLaw.DriverOut): GrowCrowd gives each new walker a crowd view, dressed
    ///    as anyone, and the spread, the seats and the bodies know it.
    /// View only: nothing goes back to the engine.</summary>
    public sealed partial class HostViews
    {
        sealed class TrafficCarView
        {
            public string Id = "", Key = "", Kit = ""; public Transform T, Model; public int Paint = -1; public float Lift, Phase; public bool Box;
            public Renderer[] Rs = System.Array.Empty<Renderer>(); public Material[][] Built = System.Array.Empty<Material[]>();
            public Vector3 HeadFrom = Vector3.forward, HeadTo = Vector3.forward;
            /// <summary>Brief 098 step 1b, a wreck's push drawn as a slide (HostViews.TrafficShove): from, to, how far along (0..1), the pushing car's view and its last drawn place.</summary>
            public bool Sliding, Touched; public Vector3 SlideFrom, SlideTo, PusherWas; public float SlideP, SlideAt, HalfLen, GapMin; public string SlideBy = ""; public int ShovedSeen;
        }
        readonly Dictionary<string, TrafficCarView> trafficCars = new(System.StringComparer.Ordinal);
        readonly Dictionary<string, Stack<TrafficCarView>> trafficSpare = new(System.StringComparer.Ordinal);
        readonly List<TrafficView> trafficNow = new(); readonly HashSet<string> trafficSeen = new(System.StringComparer.Ordinal); readonly List<string> trafficGone = new();
        bool trafficFault;
        /// <summary>The passing cars as the engine gave them at the last tick, in id order (empty without traffic): the host's one copy (CarIdsNow, the hover, the sound).</summary>
        public IReadOnlyList<TrafficView> TrafficNow => trafficNow;
        /// <summary>For the proofs: views built (a pooled view taken again is not counted), views drawn now, and crowd views added for drivers who got out.</summary>
        public int TrafficViewsMade { get; private set; }
        public int TrafficViewsOn => trafficCars.Count;
        public int CrowdGrown { get; private set; }
        /// <summary>A passing car as the engine gave it at the last tick, by its id (car:…), else null.</summary>
        public TrafficView TrafficOf(string id) { foreach (var v in trafficNow) if (v.Id == id) return v; return null; }
        /// <summary>Where a passing car's view is drawn now (its eased place), false when it has none.</summary>
        public bool TrafficDrawnAt(string id, out Vector3 at) { if (trafficCars.TryGetValue(id, out var cv) && cv.T && cv.T.gameObject.activeInHierarchy) { at = cv.T.position; return true; } at = default; return false; }
        /// <summary>The kit model a passing car's view was made from (the proofs): "sedan-o", "sedan-g", "van", "scooter", "" when none.</summary>
        public string TrafficKitOf(string id) => trafficCars.TryGetValue(id, out var cv) ? cv.Kit + (cv.Box ? "/box" : "") : "";
        /// <summary>The paint a passing sedan wears now, by its key in host.trafficPaintKeys, else "".</summary>
        public string TrafficPaintOf(string id) => trafficCars.TryGetValue(id, out var cv) && cv.Paint >= 0 && host.trafficPaintKeys != null && cv.Paint < host.trafficPaintKeys.Length ? host.trafficPaintKeys[cv.Paint] : "";

        /// <summary>Per tick (HostViews.Tick): every passing car's view where the engine has it; the views of cars that left go back to their model's spares.</summary>
        void TrafficViews(Game g)
        {
            trafficNow.Clear();
            if (g.TrafficOn && !trafficFault)
                try { trafficNow.AddRange(g.TrafficNow()); }
                catch (System.Exception e) { trafficFault = true; Debug.LogError($"traffic views: Game.TrafficNow threw ({e.Message}); the passing cars are not drawn this game"); }
            trafficSeen.Clear();
            foreach (var v in trafficNow)
            {
                trafficSeen.Add(v.Id);
                bool fresh = !trafficCars.TryGetValue(v.Id, out var cv); if (fresh) cv = TrafficTake(v);
                var dir = frame.Dir(v.Heading); if (dir.sqrMagnitude < 1e-6f) dir = cv.HeadTo;
                cv.HeadFrom = fresh ? dir : cv.HeadTo; cv.HeadTo = dir;
                if (!SlideTick(cv, v, g, fresh)) Set(cv.Key, v.At); // brief 098 step 1b: a wreck pushed a long way in one tick (a Go's push) slides there instead
            }
            if (trafficCars.Count == trafficSeen.Count) return;
            trafficGone.Clear(); foreach (var id in trafficCars.Keys) if (!trafficSeen.Contains(id)) trafficGone.Add(id);
            foreach (var id in trafficGone) { TrafficRelease(trafficCars[id]); trafficCars.Remove(id); }
        }

        /// <summary>Per frame (HostViews.Frame): the heading eased between the ticks, as the position is; the scooter's hover bob.</summary>
        void TrafficFrame(float alpha)
        {
            if (trafficCars.Count == 0) return;
            SlideFrame(); // brief 098 step 1b: the wrecks being pushed aside, drawn sliding (HostViews.TrafficShove)
            float now = Time.time;
            foreach (var cv in trafficCars.Values)
            {
                if (!cv.Model) continue;
                var d = Vector3.Slerp(cv.HeadFrom, cv.HeadTo, alpha); d.y = 0f; if (d.sqrMagnitude < 1e-6f) d = cv.HeadTo;
                cv.Model.rotation = Quaternion.LookRotation(d.normalized, Vector3.up) * carNose;
                if (cv.Lift > 0f) cv.Model.localPosition = new Vector3(0f, cv.Lift + 0.03f * Mathf.Sin(now * 2.1f + cv.Phase), 0f);
            }
        }

        /// <summary>The model kind a passing car is drawn as: its engine kind, a sedan one of the two bodies by its id.</summary>
        static string TrafficKit(TrafficView v) => v.Model switch { "van" => "van", "scooter" => "scooter", _ => (TrafficHash(v.Id) & 1u) == 0 ? "sedan-o" : "sedan-g" };
        static uint TrafficHash(string s) { uint h = 2166136261; foreach (var c in s) { h ^= c; h *= 16777619; } h ^= h >> 15; h *= 0x2C1B3C6Du; h ^= h >> 12; return h; }

        TrafficCarView TrafficTake(TrafficView v)
        {
            var kit = TrafficKit(v); var key = "car/" + v.Id.Substring(4);
            TrafficCarView cv;
            if (trafficSpare.TryGetValue(kit, out var spare) && spare.Count > 0) { cv = spare.Pop(); cv.T.name = key; }
            else cv = TrafficMake(kit, key, v);
            cv.Id = v.Id; cv.Key = key; cv.Phase = (TrafficHash(v.Id) % 6283u) / 1000f;
            things[key] = (cv.T, Vector3.zero, Vector3.zero, false);
            if (cv.Kit.StartsWith("sedan")) TrafficPaint(cv, PaintFor(cv.Kit, v.Id));
            trafficCars[v.Id] = cv;
            return cv;
        }

        TrafficCarView TrafficMake(string kit, string key, TrafficView v)
        {
            var go = new GameObject(key); go.transform.SetParent(root, false);
            var cv = new TrafficCarView { Kit = kit, T = go.transform };
            var model = kit switch { "sedan-o" => host.sedanOfficial, "sedan-g" => host.sedanGetaway, "van" => host.trafficVan, "scooter" => host.trafficScooter, _ => null };
            float l = v.Length / 100f, w = v.Width / 100f, h = kit == "van" ? 3.1f : kit == "scooter" ? 1.2f : 1.4f;
            if (model)
            {
                var inst = Object.Instantiate(model, go.transform, false); inst.name = "model"; cv.Model = inst.transform;
                cv.Rs = inst.GetComponentsInChildren<Renderer>(true); cv.Built = new Material[cv.Rs.Length][];
                for (int i = 0; i < cv.Rs.Length; i++) { cv.Rs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; cv.Built[i] = cv.Rs[i].sharedMaterials; }
                var hit = inst.AddComponent<BoxCollider>(); hit.size = new Vector3(w, h, l); hit.center = new Vector3(0f, h / 2f, 0f); // the footprint, turning with the car
                if (kit == "scooter") cv.Lift = 0.35f; // presentation.json scooter/hover: "hovering 0.35 m"
            }
            else
            {
                var body = Part(go.transform, "model", cube, Lit(host.carColour), new Vector3(0f, h / 2f, 0f), new Vector3(w, h, l)); cv.Model = body; cv.Box = true; body.gameObject.AddComponent<BoxCollider>(); // no kit copied: the box, as the mission cars
                cv.Rs = body.GetComponentsInChildren<Renderer>(true); cv.Built = new Material[cv.Rs.Length][]; for (int i = 0; i < cv.Rs.Length; i++) cv.Built[i] = cv.Rs[i].sharedMaterials;
            }
            go.SetActive(false); TrafficViewsMade++;
            return cv;
        }

        /// <summary>The paint a passing sedan wears: picked by its id among the palette's paints that may go on its body (host.trafficPaintAvoid, TrafficKit:
        /// never one that would pass for Vale's car or ours); -1 (as built) with no palette.</summary>
        int PaintFor(string kit, string id)
        {
            var pal = host.trafficPaints; var av = host.trafficPaintAvoid; int n = pal == null ? 0 : pal.Length / 2, bit = kit == "sedan-g" ? 2 : 1, ok = 0;
            for (int i = 0; i < n; i++) if (av == null || i >= av.Length || (av[i] & bit) == 0) ok++;
            if (ok == 0) return -1;
            int pick = (int)(TrafficHash(id + "|paint") % (uint)ok);
            for (int i = 0; i < n; i++) if ((av == null || i >= av.Length || (av[i] & bit) == 0) && pick-- == 0) return i;
            return -1;
        }

        /// <summary>A sedan's body and roof slots (host.trafficSlots: kit.json carPaints.slots) in paint k of the palette (host.trafficPaints: body, roof pairs); -1 as built.</summary>
        void TrafficPaint(TrafficCarView cv, int k)
        {
            if (cv.Paint == k) return; cv.Paint = k;
            var pal = host.trafficPaints; var slots = host.trafficSlots; bool on = k >= 0 && pal != null && 2 * k + 1 < pal.Length && slots != null && slots.Length >= 4;
            string body = on ? slots[cv.Kit == "sedan-g" ? 2 : 0] : "", roof = on ? slots[cv.Kit == "sedan-g" ? 3 : 1] : "";
            for (int i = 0; i < cv.Rs.Length; i++)
            {
                if (!cv.Rs[i]) continue; var built = cv.Built[i]; var mats = new Material[built.Length];
                for (int j = 0; j < built.Length; j++) { var m = built[j]; mats[j] = on && m && m.name == body && pal[2 * k] ? pal[2 * k] : on && m && m.name == roof && pal[2 * k + 1] ? pal[2 * k + 1] : m; }
                cv.Rs[i].sharedMaterials = mats;
            }
        }

        void TrafficRelease(TrafficCarView cv)
        {
            if (carTint.ContainsKey(cv.Key)) Scorch(cv.Key, 1f); // a char the car effects left (CarTint) goes with the car
            things.Remove(cv.Key); carTint.Remove(cv.Key); lifted.Remove(cv.Key); cv.Sliding = cv.Touched = false; cv.SlideBy = ""; cv.ShovedSeen = 0;
            if (cv.T) cv.T.gameObject.SetActive(false);
            if (!trafficSpare.TryGetValue(cv.Kit, out var spare)) trafficSpare[cv.Kit] = spare = new Stack<TrafficCarView>();
            spare.Push(cv);
        }

        // ---------------------------------------------------------------- drivers who get out join the crowd

        /// <summary>Per tick, before the crowd's views (HostViews.Tick): a walker the engine added since the start (a passing car's driver who got out)
        /// gets a crowd view, dressed by their job as anyone (a visitor: a passer-by's figure); the spread grows in place (HostCrowdSpread.SpreadInit).</summary>
        void GrowCrowd(Game g)
        {
            int had = crowdKeys.Length, n = g.Walkers.Count; if (n <= had) return;
            System.Array.Resize(ref crowdKeys, n);
            for (int i = had; i < n; i++)
            {
                var w = g.Walkers[i]; crowdKeys[i] = "crowd/" + i;
                if (!things.ContainsKey(crowdKeys[i])) { Person(crowdKeys[i], Family(w.P.Job), false, true, Shape(w.P.Job)); Dress(crowdKeys[i], PersonFigure(w.P)); }
                if (walkerIndex.Count > 0) walkerIndex[w.P] = i;
            }
            walkerByKey = null; CrowdGrown += n - had;
        }
    }
}
