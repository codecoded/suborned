using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Tactical: the built play camera itself (GDD §10: orthographic, play zoom 14 at 38°, the export's yaw), panned over
    /// the street datum and zoomed between half and three times the play zoom. Until it is first moved it is exactly the camera
    /// the builder made, in a Game view of the shape the builder fitted it to (views.json: 16:9); in any other shape it is
    /// refitted at once, since a wider frame takes in roofs the built stand-off did not clear. Each move refits what Rig.Export fitted at build time: the stand-off that clears what is in the frame
    /// (HeightField.StandOff), the far plane, and TacticalShadows.focusDepth, so the street band keeps its shadow cascade
    /// wherever the focus goes, and ArchitectureFade.focus, so what hides the focus is a ghost (GDD §15; F turns it off and on).
    /// An export view from views.json is adopted with the stand-off, shadow fit and fade focus it was built with.</summary>
    public sealed class TacticalMode : IWalkMode
    {
        /// <summary>WASD pans this many frame half-heights per second (Shift three times that).</summary>
        const float PanRate = 1.5f, Fast = 3f;

        readonly Camera cam;
        readonly TacticalShadows shadows;
        readonly ArchitectureFade fade; // null on a scene built before the fade (rebuild it)
        readonly HeightField heights;
        readonly Quaternion rot;
        readonly Vector3 fwd;
        readonly Vector2 ahead, right; // the view's forward and right on the ground
        readonly float playZoom, farMargin, built; // built: the frame shape the builder fitted the export views to
        Vector3 focus;
        float zoom, aspect;

        public TacticalMode(Camera play, HeightField heights, float builtAspect)
        {
            cam = play; this.heights = heights;
            shadows = play.GetComponent<TacticalShadows>();
            fade = play.GetComponent<ArchitectureFade>();
            rot = play.transform.rotation; fwd = rot * Vector3.forward;
            ahead = new Vector2(fwd.x, fwd.z).normalized; right = new Vector2(ahead.y, -ahead.x);
            zoom = playZoom = play.orthographicSize; aspect = built = builtAspect; // a Game view of another shape refits on the first Tick
            farMargin = play.farClipPlane - shadows.focusDepth; // the builder's: the district's diagonal
            focus = OnStreet(play.transform.position);
        }

        public string Name => "Tactical";
        public string Blurb => $"the game's play camera: orthographic, pitch {ViewAngles.Pitch(rot):F0}°, yaw {ViewAngles.Yaw(rot):F0}°";
        public Camera Camera => cam;
        public Vector3 Ground => focus;
        public float ShadowDistance => 0f;
        public float Yaw => ViewAngles.Yaw(rot);
        public bool Fits(Pose view) => view.Ortho && Quaternion.Angle(view.Rotation, rot) < 0.5f;
        public string Status =>
            $"zoom {zoom:F1}{(Mathf.Approximately(zoom, playZoom) ? " (play zoom)" : $" (play zoom {playZoom:F0})")} · stand-off {shadows.focusDepth:F1} m · shadows to {shadows.Distance:F0} m · " +
            (fade ? fade.fade ? $"fade on, {fade.Radius:F1} m" : "fade off" : "no fade (scene built before it)");

        /// <summary>The play camera's architecture fade; false (and unsettable) on a scene built without it.</summary>
        public bool Fade { get => fade && fade.fade; set { if (fade) fade.fade = value; } }

        public void Enter(IWalkMode from) { focus = Limit(from.Ground); Fit(); }

        public void Go(Place p) { focus = Limit(p.Target); Fit(); }

        public void Take(Pose view)
        {
            zoom = view.OrthoSize; focus = OnStreet(view.Position);
            if (float.IsNaN(view.ShadowFocus) || !Mathf.Approximately(cam.aspect, built)) { focus = Limit(focus); Fit(); return; }
            aspect = built;
            cam.orthographicSize = zoom;
            cam.transform.position = view.Position;
            cam.farClipPlane = view.Far;
            shadows.focusDepth = view.ShadowFocus;
            if (fade) fade.focus = view.Position + fwd * view.ShadowFocus; // the built focus: an element's top on some views, not the street
        }

        public void Tick(in WalkFrame f, float dt)
        {
            var pan = (ahead * f.Move.z + right * f.Move.x) * (zoom * PanRate * (f.Fast ? Fast : 1f) * dt);
            if (f.Dragging)
            {   // the ground follows the pointer: one pixel is 2·zoom / height metres across the frame, and that over sin(pitch) along the ground
                float metres = 2f * zoom / Mathf.Max(cam.pixelHeight, 1);
                pan -= right * (f.Drag.x * metres) + ahead * (f.Drag.y * metres / -fwd.y);
            }
            float to = f.ResetZoom ? playZoom : Mathf.Clamp(zoom * Mathf.Pow(0.9f, f.Scroll) * Mathf.Exp(-f.Dial * dt), playZoom * 0.5f, playZoom * 3f);
            if (pan == Vector2.zero && Mathf.Approximately(to, zoom) && Mathf.Approximately(cam.aspect, aspect)) return;
            zoom = to; focus = Limit(focus + new Vector3(pan.x, 0f, pan.y));
            Fit();
        }

        /// <summary>Rig.Export for the focus and zoom now: stand back far enough to clear the frame column, and fit the shadows to that depth.</summary>
        void Fit()
        {
            aspect = cam.aspect;
            float standOff = heights.StandOff(rot, focus, zoom * aspect, zoom);
            cam.orthographicSize = zoom;
            cam.transform.position = focus - fwd * standOff;
            cam.farClipPlane = standOff + farMargin;
            shadows.focusDepth = standOff;
            if (fade) fade.focus = focus;
        }

        /// <summary>Where the view axis through a camera position meets the street datum.</summary>
        Vector3 OnStreet(Vector3 position) => position + fwd * (position.y / -fwd.y);

        Vector3 Limit(Vector3 p) => new(Mathf.Clamp(p.x, heights.Area.xMin, heights.Area.xMax), 0f, Mathf.Clamp(p.z, heights.Area.yMin, heights.Area.yMax));
    }
}
