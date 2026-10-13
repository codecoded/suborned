using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Walk: a perspective eye 1.7 m over whatever WalkSurface puts underfoot (street, deck, concourse, a stair's run),
    /// at the city-life walking pace. A review camera, never the game's (GDD §10: never first person). Nothing blocks it.</summary>
    public sealed class FootMode : IWalkMode
    {
        public const float EyeHeight = 1.7f;
        /// <summary>contracts/world/harbour-district.life.json walkSpeed [S]; Shift runs at four times that.</summary>
        const float Pace = 1.4f, Run = 5.6f;
        const float Fov = 60f, Near = 0.2f, Far = 12000f; // the horizon band reaches 8 km
        /// <summary>How fast the eye follows a change of surface (m/s): a kerb is instant, a deck edge a short drop.</summary>
        const float Settle = 30f;
        /// <summary>How far back from a place's edge a jump stands the walker, and the step and reach of its search for open ground (m).</summary>
        const float Back = 8f, Ring = 6f, Search = 42f;

        readonly Camera cam;
        readonly WalkSurface surface;
        readonly Rect area;
        readonly float facing; // the play camera's yaw: jumps arrive looking the way the game looks
        readonly System.Func<Vector2, bool> open; // is the sky open over a plan point (no roof, deck or mass above a walker)?
        Vector2 at;
        float feet, eye, yaw, pitch, speed = Pace;
        WalkSurface.Spot spot;
        string said = "";

        public FootMode(Camera cam, WalkSurface surface, Rect area, float facing, System.Func<Vector2, bool> open)
        {
            this.cam = cam; this.surface = surface; this.area = area; this.facing = facing; this.open = open;
        }

        public string Name => "Walk";
        public string Blurb => "review camera, not the game's: eye 1.7 m over the surface underfoot, nothing blocks";
        public Camera Camera => cam;
        public Vector3 Ground => new(at.x, feet, at.y);
        public float ShadowDistance => 400f; // as the `eye` view (views.json)
        public string Status => $"on {spot.Level}{(spot.Id != null ? " " + spot.Id : "")} at {feet:F2} m · {speed:F1} m/s{said}";

        public void Enter(IWalkMode from)
        {
            var t = from.Camera.transform;
            bool flying = !from.Camera.orthographic;
            yaw = ViewAngles.Yaw(t.rotation); pitch = flying ? Mathf.Clamp(ViewAngles.Pitch(t.rotation), -85f, 85f) : 0f;
            // from the air: land on the highest surface under the eye; from the tactical focus: the street
            Land(new Vector2(from.Ground.x, from.Ground.z), flying ? t.position.y - EyeHeight : 0f);
        }

        /// <summary>A raised platform that can be stood on (the Skyport pad, the liner berth) is arrived on, and so is an area's
        /// label point. Anything else is faced from Back metres off its edge: along the play yaw, or from the first other side
        /// (45° steps, then further out) that is open ground. A label under a roof moves to the nearest open ground the same way.</summary>
        public void Go(Place p)
        {
            var target = new Vector2(p.Target.x, p.Target.z);
            yaw = facing; pitch = 0f;
            bool platform = p.Top > WalkSurface.Step && Mathf.Abs(surface.Stand(target, p.Top).Height - p.Top) <= WalkSurface.Step;
            if (platform || (p.Radius <= 0f && Fair(target))) { Land(target, p.Top); return; }
            float nearest = p.Radius > 0f ? p.Radius + Back : Ring;
            var from = Side(target, 0, nearest);
            for (float off = nearest; off <= nearest + Search; off += Ring)
                for (int i = 0; i < 8; i++)
                {
                    if (!Fair(Side(target, i, off))) continue;
                    from = Side(target, i, off);
                    if (p.Radius > 0f) { yaw = facing + i * 45f; pitch = -Mathf.Clamp(Mathf.Atan2((p.Top - EyeHeight) * 0.5f, off) * Mathf.Rad2Deg, 0f, 35f); } // facing it, looking half-way up
                    Land(from, 0f);
                    return;
                }
            Land(from, 0f); // nowhere open: where it would have been
        }

        /// <summary>Somewhere a walker can arrive: not in the water and not under a roof.</summary>
        bool Fair(Vector2 p) => surface.Stand(p, 0f).Level != "water" && open(p);

        /// <summary>The point `off` metres from the target on side i: side 0 looks at it along the play yaw, each next one 45° round.</summary>
        Vector2 Side(Vector2 target, int i, float off)
        {
            float a = (facing + i * 45f) * Mathf.Deg2Rad;
            return target - new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * off;
        }

        public void Take(Pose view)
        {
            yaw = ViewAngles.Yaw(view.Rotation); pitch = 0f;
            Land(new Vector2(view.Position.x, view.Position.z), view.Position.y - EyeHeight);
        }

        void Land(Vector2 where, float under)
        {
            at = Clamp(where);
            spot = surface.Stand(at, under); feet = spot.Height; eye = feet + EyeHeight; said = "";
            cam.orthographic = false; cam.fieldOfView = Fov; cam.nearClipPlane = Near; cam.farClipPlane = Far;
            Apply();
        }

        public void Tick(in WalkFrame f, float dt)
        {
            yaw += f.Look.x; pitch = Mathf.Clamp(pitch - f.Look.y, -85f, 85f);
            var fwd = new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));
            var move = Vector2.ClampMagnitude(fwd * f.Move.z + new Vector2(fwd.y, -fwd.x) * f.Move.x, 1f);
            speed = f.Fast ? Run : Pace;
            if (move != Vector2.zero) { at = Clamp(at + move * (speed * dt)); said = ""; }
            if (f.LevelUp || f.LevelDown)
            {
                if (surface.Shift(at, feet, f.LevelUp ? 1 : -1, out var to)) { feet = to.Height; said = ""; }
                else said = f.LevelUp ? " · no level above here" : " · no level below here";
            }
            spot = surface.Stand(at, feet); feet = spot.Height;
            eye = Mathf.MoveTowards(eye, feet + EyeHeight, Settle * dt);
            Apply();
        }

        void Apply() => cam.transform.SetPositionAndRotation(new Vector3(at.x, eye, at.y), Quaternion.Euler(pitch, yaw, 0f));

        Vector2 Clamp(Vector2 p) => new(Mathf.Clamp(p.x, area.xMin, area.xMax), Mathf.Clamp(p.y, area.yMin, area.yMax));
    }
}
