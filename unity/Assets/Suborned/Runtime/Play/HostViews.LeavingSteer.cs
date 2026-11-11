using System.Collections.Generic;
using UnityEngine;
using Game = Suborned.Sim.Game;

namespace Suborned.Play
{
    /// <summary>Brief 109 round 2 (Sky on 0.2.325: "in that example it drove through the rivals car"): the getaway's drive-off after the Go is the view's
    /// (HostViews.Leaving: the engine ends on the Go's tick and stops, so the cars it was moving stand where they were). It now drives as a driver would,
    /// "walkable as seen" for cars: every frame it looks ahead along its way (a stopping distance and a swerve's length) for the other cars drawn
    /// (each a capsule from its own model's bounds and pose, its own included); a car in the way and it eases sideways onto the nearest offset that
    /// clears it for the whole look ahead, swerve included, moving sideways only where the drive grid has room for the car (out of its bay, not into a kerb); nothing clears and it brakes to
    /// a stop behind it and waits. It turns as it swerves (the heading follows the sideways motion) and moves sideways only as it rolls, harder from rest than at speed. It goes back
    /// onto its way once that is clear. No fade, no cut. View only: the engine's car and the hash are untouched.</summary>
    public sealed partial class HostViews
    {
        /// <summary>A car the view drives after the engine stopped (brief 109: our getaway; Popovic's car at the end, HostViews.LeavingOfficial): its way,
        /// how far along it is, its speed, and its sideways steering round the other cars.</summary>
        sealed class ViewDrive
        {
            public string key = ""; public readonly List<Vector3> way = new(); public float s, v, len, side, sideGoal, sideRate; public string last = ""; public Vector3? heading;
            public void Reset() { s = v = len = side = sideGoal = sideRate = 0f; last = ""; heading = null; way.Clear(); }
            public void Measure() { len = 0f; for (int i = 1; i < way.Count; i++) len += Vector3.Distance(way[i - 1], way[i]); }
            public bool Done => way.Count >= 2 && s >= len;
        }
        const float SteerClear = 0.35f, SteerMax = 4.5f, SteerStep = 0.5f, LeaveBrake = 6f; // m between bodies; m off the way at most; m a try; m/s²
        /// <summary>How far sideways a car can go per metre it rolls: a pull-out from rest turns hard (0.9), at speed it changes lane gently (0.3 by 10 m/s).</summary>
        static float SteerSlope(float v) => Mathf.Lerp(0.9f, 0.3f, Mathf.Clamp01(v / 10f));
        /// <summary>For checks and captures: what the getaway's drive-off did last ("" clear, "steer 3.0 round car/rivals", "wait for car/rivals").</summary>
        public string LeaveSteerLast => getaway.last;
        readonly Dictionary<Transform, (float half, float rad, float cz)> bodyDims = new();
        readonly List<(Vector2 a, Vector2 b, float r, string key)> leaveObs = new();

        static void Say(ViewDrive c, string w) { if (w != c.last) Debug.Log($"HostViews: {c.key}'s drive-off: {w} at {c.s:0.0} m of {c.len:0} m, {c.v:0.0} m/s"); c.last = w; }

        /// <summary>A car body as a capsule in its own frame: half the straight between the two ends, the radius (half its width), the centre's offset along +Z.</summary>
        (float half, float rad, float cz) Dims(Transform m)
        {
            if (bodyDims.TryGetValue(m, out var d)) return d;
            var b = new Bounds(Vector3.zero, Vector3.zero); bool any = false;
            foreach (var mf in m.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.sharedMesh) continue; var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var p = m.InverseTransformPoint(mf.transform.TransformPoint(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            if (!any || b.size.z < 1f) b = new Bounds(Vector3.zero, new Vector3(1.8f, 1.3f, 4.7f)); // a stand-in: the kit sedan's size
            var s = m.lossyScale.x; d = (Mathf.Max(0f, b.extents.z - b.extents.x) * s, b.extents.x * s, b.center.z * s); bodyDims[m] = d; return d;
        }

        static Vector2 XZ(Vector3 v) => new(v.x, v.z);
        static Vector3 RightOf(Vector3 d) => new(d.z, 0f, -d.x);

        /// <summary>Per frame of the leaving: the other cars now; the side to drive at; the speed it may have (braked when nothing clears). Returns the speed.</summary>
        float LeaveSteer(Game g, ViewDrive c, float v, float dt)
        {
            var me = carModels.TryGetValue(c.key, out var mm) && mm ? Dims(mm) : (1.45f, 0.9f, 0f);
            leaveObs.Clear();
            foreach (var kv in carModels)
            {
                if (kv.Key == c.key || !kv.Key.StartsWith("car/") || !kv.Value || !kv.Value.gameObject.activeInHierarchy || !things.TryGetValue(kv.Key, out var th) || !th.shown) continue;
                var d = Dims(kv.Value); var f = kv.Value.forward; f.y = 0f; if (f.sqrMagnitude < 1e-6f) f = Vector3.forward; f.Normalize();
                var oc = kv.Value.position + f * d.cz; leaveObs.Add((XZ(oc - f * d.half), XZ(oc + f * d.half), d.rad, kv.Key));
            }
            // how close each is now: a car already that close (alongside the stand) blocks only if the way would bring us closer still, as a driver pulls out past it
            var near = new float[leaveObs.Count];
            { var (p0, d0) = WayAt(c.way, c.s); var c0 = p0 + RightOf(d0) * c.side + d0 * me.Item3; Vector2 a0 = XZ(c0 - d0 * me.Item1), b0 = XZ(c0 + d0 * me.Item1); for (int i = 0; i < near.Length; i++) near[i] = SegDist(a0, b0, leaveObs[i].a, leaveObs[i].b); }
            float slope = SteerSlope(v), look = Mathf.Max(25f, 4f + v * 1.4f + v * v / (2f * LeaveBrake)); // as far down the road as a driver reads it: past a car in the way, to see a swerve that clears it
            string by = "";
            // the way at `goal`, the swerve from where it is now included (sideways SteerSlope m per m rolled): clear? else how far ahead it is blocked
            // room to the side here: the drive grid's own clearance from solids at the car's centre (the way itself is the grid's route, always room)
            bool Room(Vector3 p, Vector3 r, float side) => Mathf.Abs(side) < 0.01f || g.S.Drive.ClearanceAt(frame.Engine(p + r * side)) >= me.Item2 * 100f;
            // the plan for `goal`, as a driver drives it: along the way, moving sideways (slope m per m) only where there is room for the car, else holding
            // its line; clear for the whole look ahead? else how far ahead the first car in the way is
            bool Clear(float goal, out float blockedAt)
            {
                float side = c.side;
                for (float ds = 0f; ds <= look; ds += 0.75f)
                {
                    var (p, d) = WayAt(c.way, c.s + ds); var r = RightOf(d);
                    if (ds > 0f) { float want = Mathf.MoveTowards(side, goal, slope * 0.75f); if (Room(p, r, want)) side = want; }
                    var cc = p + r * side + d * me.Item3; Vector2 a = XZ(cc - d * me.Item1), b = XZ(cc + d * me.Item1);
                    for (int i = 0; i < leaveObs.Count; i++) { var o = leaveObs[i]; float dd = SegDist(a, b, o.a, o.b); if (dd < me.Item2 + o.r + SteerClear && dd < near[i] - 0.05f) { blockedAt = ds; by = o.key; return false; } }
                }
                blockedAt = float.MaxValue; return true;
            }
            if (!Clear(c.sideGoal, out var blocked))
            {
                string first = by; bool found = false;
                var tries = new List<float>(); for (float k = -SteerMax; k <= SteerMax + 1e-3f; k += SteerStep) tries.Add(k);
                tries.Sort((x, y) => Mathf.Abs(x - c.side).CompareTo(Mathf.Abs(y - c.side)));
                foreach (var k in tries) if (Mathf.Abs(k - c.sideGoal) > 1e-3f && Clear(k, out _)) { c.sideGoal = k; found = true; break; }
                if (found) Say(c, $"steer {c.sideGoal:0.0} round {first}");
                else
                {
                    v = Mathf.Min(v, Mathf.Sqrt(2f * LeaveBrake * Mathf.Max(0f, blocked - 1f))); // a driver stops short of it and waits
                    if (c.last != $"wait for {first}") { var why = new System.Text.StringBuilder(); foreach (var k in tries) { Clear(k, out var bk); why.Append($" {k:0.0}:{(bk == float.MaxValue ? "clear" : $"{by}@{bk:0.0}")}"); } Debug.Log($"HostViews: the drive-off's tries ({c.key}) at {c.s:0.0} m (side {c.side:0.0}, look {look:0.0} m):{why}"); }
                    Say(c, $"wait for {first}");
                }
            }
            else if (Mathf.Abs(c.sideGoal) > 1e-3f && Clear(0f, out _)) { c.sideGoal = 0f; Say(c, "back on its way"); }
            float was = c.side; { var (p, d) = WayAt(c.way, c.s + v * dt); float want = Mathf.MoveTowards(c.side, c.sideGoal, slope * v * dt); if (Room(p, RightOf(d), want)) c.side = want; } // sideways only where there is room
            c.sideRate = v * dt > 1e-5f ? (c.side - was) / (v * dt) : 0f;
            return v;
        }

        /// <summary>Where the car is drawn at `s` along its way, at its side, facing along the way turned by its sideways motion.</summary>
        static (Vector3 at, Vector3 dir) SteeredAt(ViewDrive c)
        {
            var (p, d) = WayAt(c.way, c.s); var r = RightOf(d);
            var h = d + r * c.sideRate; h.y = 0f;
            return (p + r * c.side, h.sqrMagnitude > 1e-8f ? h.normalized : d);
        }

        /// <summary>One frame of a view drive: speed up to `top` (braked to push a wreck aside, PushCap), steer round the cars drawn or wait, move on; the car drawn there.</summary>
        void DriveFrame(Game g, ViewDrive c, float top, float dt)
        {
            c.v = PushCap(c.key, Mathf.Min(top, c.v + LeaveAccel * dt), dt);
            c.v = LeaveSteer(g, c, c.v, dt); c.s += c.v * dt;
            var (at, dir) = SteeredAt(c); c.heading = dir;
            if (things.TryGetValue(c.key, out var th)) things[c.key] = (th.t, at, at, true);
        }

        /// <summary>The least distance between two segments in the plane.</summary>
        static float SegDist(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
        {
            Vector2 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2; float a = Vector2.Dot(d1, d1), e = Vector2.Dot(d2, d2), f = Vector2.Dot(d2, r), s, t;
            if (a <= 1e-8f && e <= 1e-8f) return r.magnitude;
            if (a <= 1e-8f) { s = 0f; t = Mathf.Clamp01(f / e); }
            else
            {
                float c = Vector2.Dot(d1, r);
                if (e <= 1e-8f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                else { float b = Vector2.Dot(d1, d2), den = a * e - b * b; s = den > 1e-8f ? Mathf.Clamp01((b * f - c * e) / den) : 0f; t = (b * s + f) / e; if (t < 0f) { t = 0f; s = Mathf.Clamp01(-c / a); } else if (t > 1f) { t = 1f; s = Mathf.Clamp01((b - c) / a); } }
            }
            return (p1 + d1 * s - (p2 + d2 * t)).magnitude;
        }
    }
}
