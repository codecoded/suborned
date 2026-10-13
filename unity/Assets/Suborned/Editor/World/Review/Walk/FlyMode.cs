using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Fly: a free perspective camera (FreeFly's controls: move along the view, Q/E down and up, Shift five times faster,
    /// the wheel or [ ] sets the base speed). A review camera, never the game's. Saved perspective views are adopted exactly;
    /// a top view is seen from straight above at the height that frames the same ground.</summary>
    public sealed class FlyMode : IWalkMode
    {
        const float Fov = 60f, Near = 0.2f, Far = 12000f;
        const float MinSpeed = 1f, MaxSpeed = 500f, Fast = 5f;

        readonly Camera cam;
        readonly float facing; // the play camera's yaw
        float yaw, pitch, speed = 20f;

        public FlyMode(Camera cam, float facing) { this.cam = cam; this.facing = facing; }

        public string Name => "Fly";
        public string Blurb => "review camera, not the game's: free flight";
        public Camera Camera => cam;
        public Vector3 Ground { get { var p = cam.transform.position; return new Vector3(p.x, 0f, p.z); } }
        public float ShadowDistance => 900f; // as the flyby's default (flyby.json)
        public string Status => $"eye at {cam.transform.position.y:F1} m · {speed:F0} m/s · fov {cam.fieldOfView:F0}";

        public void Enter(IWalkMode from)
        {
            var t = from.Camera.transform;
            if (!from.Camera.orthographic) { Set(t.position, ViewAngles.Yaw(t.rotation), ViewAngles.Pitch(t.rotation), Fov); return; } // the walker's own eye
            // from the tactical camera: the same direction, 60 m back from its focus
            Set(from.Ground - t.forward * 60f, ViewAngles.Yaw(t.rotation), ViewAngles.Pitch(t.rotation), Fov);
        }

        public void Go(Place p)
        {
            float height = Mathf.Max(p.Top - p.Target.y, 0f), back = Mathf.Max(p.Radius, height) * 1.8f + 25f;
            var rot = Quaternion.Euler(20f, facing, 0f);
            Set(p.Target + Vector3.up * (height * 0.4f) - rot * Vector3.forward * back, facing, 20f, Fov);
        }

        public void Take(Pose view)
        {
            if (!view.Ortho) { Set(view.Position, ViewAngles.Yaw(view.Rotation), ViewAngles.Pitch(view.Rotation), view.Fov); return; }
            var ground = WalkPlaces.Ground(view); // an orthographic view: straight above the ground it frames
            Set(ground + Vector3.up * (view.OrthoSize / Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad)), 0f, 89f, Fov);
        }

        void Set(Vector3 position, float yaw, float pitch, float fov)
        {
            this.yaw = yaw; this.pitch = Mathf.Clamp(pitch, -89f, 89f);
            cam.orthographic = false; cam.fieldOfView = fov; cam.nearClipPlane = Near; cam.farClipPlane = Far;
            cam.transform.SetPositionAndRotation(position, Quaternion.Euler(this.pitch, yaw, 0f));
        }

        public void Tick(in WalkFrame f, float dt)
        {
            yaw += f.Look.x; pitch = Mathf.Clamp(pitch - f.Look.y, -89f, 89f);
            speed = Mathf.Clamp(speed * Mathf.Pow(1.2f, f.Scroll) * Mathf.Exp(f.Dial * dt), MinSpeed, MaxSpeed);
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            var move = rot * new Vector3(f.Move.x, 0f, f.Move.z) + Vector3.up * f.Move.y;
            cam.transform.SetPositionAndRotation(cam.transform.position + move * (speed * (f.Fast ? Fast : 1f) * dt), rot);
        }
    }
}
