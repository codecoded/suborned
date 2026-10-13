using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>The walk review tool for one Play session: three camera modes over the built district (Walk, Fly, Tactical), jumps
    /// to places, a readout of the plan ids underfoot and in view, and notes with screenshots. It starts in Tactical with the
    /// play camera exactly as built. Only the current mode's camera renders. Everything it makes is made at runtime; the scene
    /// file is never touched.</summary>
    public sealed class WalkRig : IDisposable
    {
        const float IdsEvery = 0.1f; // seconds between plan-id lookups
        const float Reach = 3000f;   // how far a view ray looks for a roof or the street
        const float MaxStep = 0.1f;  // seconds of movement taken from one frame (a hitch does not throw the camera)

        readonly IWalkMode[] modes;
        readonly IWalkMode fly;
        readonly TacticalMode tactical;
        readonly Camera play;
        readonly WalkEye eye;
        readonly PlanIndex plan;
        readonly HeightField heights;
        readonly List<Place> places;
        readonly WalkNotes notes;
        readonly WalkHud hud;
        readonly WalkInput input = new();
        readonly bool sceneFog = RenderSettings.fog;
        int current;
        float nextIds;
        PlanIds feet, look;
        Vector3? lookAt;

        /// <summary>Where each frame's input comes from: the keyboard and mouse, unless a check scripts it.</summary>
        public Func<float, WalkFrame> Read;
        /// <summary>A standing warning shown under the readout (the scene and the export file differ); null = none.</summary>
        public string Warning;

        public IWalkMode Mode => modes[current];
        public IReadOnlyList<Place> Places => places;
        public WalkNotes Notes => notes;

        public WalkRig(Camera play, float builtAspect, WalkSurface surface, PlanIndex plan, HeightField heights, List<Place> places, WalkNotes notes)
        {
            this.play = play; this.plan = plan; this.heights = heights; this.places = places; this.notes = notes;
            eye = new WalkEye(play);
            tactical = new TacticalMode(play, heights, builtAspect);
            fly = new FlyMode(eye.Camera, tactical.Yaw);
            var foot = new FootMode(eye.Camera, surface, heights.Area, tactical.Yaw, p => heights.At(p.x, p.y) < FootMode.EyeHeight);
            modes = new[] { foot, fly, tactical }; // keys 1, 2, 3
            current = Array.IndexOf(modes, tactical);
            hud = new WalkHud(places) { Picked = Go, Noted = Note };
            Read = dt => input.Read(dt, hud.Typing, hud.Over);
            Fog();
        }

        public void Tick()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxStep);
            var f = Read(dt);
            hud.Tick(f); // an open field takes its keys first: it may jump or save a note
            if (f.Keys) hud.ShowKeys = !hud.ShowKeys;
            if (f.Places) hud.OpenPlaces();
            if (f.Note) hud.OpenNote();
            if (f.Fade && Mode == tactical) { tactical.Fade = !tactical.Fade; hud.Say(tactical.Fade ? "architecture fade on" : "architecture fade off"); }
            if (f.Mode > 0) Switch(f.Mode - 1);
            else if (f.NextMode) Switch((current + 1) % modes.Length);
            Mode.Tick(f, dt);
            eye.ShadowDistance = Mode.ShadowDistance;
            if (Time.unscaledTime < nextIds) return;
            nextIds = Time.unscaledTime + IdsEvery;
            Identify(f.Pointer);
        }

        public void Gui() => hud.Draw(Mode, Readout());

        void Switch(int to)
        {
            if (to == current) return;
            var from = Mode;
            current = to;
            Mode.Enter(from);
            play.enabled = Mode.Camera == play; eye.Camera.enabled = Mode.Camera == eye.Camera;
            Fog();
            nextIds = 0f;
        }

        /// <summary>Fog as Shots renders the approved views: off for the play camera (views.json: every export view is rendered
        /// without it), the scene's own haze for the review eye (as the parade, skyport and skyline views and the flyby).</summary>
        void Fog() => RenderSettings.fog = sceneFog && Mode != tactical;

        /// <summary>Jumps to a place. A saved view chooses its mode (an export view is Tactical, any other is flown); a plan place keeps the current one.</summary>
        public void Go(Place p)
        {
            if (p.View is { } view) { Switch(Array.IndexOf(modes, tactical.Fits(view) ? tactical : fly)); Mode.Take(view); }
            else Mode.Go(p);
            nextIds = 0f;
            hud.Say($"{Mode.Name}: {p.Name}");
        }

        /// <summary>Saves a note where the reviewer is now, with a screenshot of the current camera (no overlay).</summary>
        public void Note(string text)
        {
            Identify(null);
            var t = Mode.Camera.transform;
            var note = new WalkNote
            {
                Mode = Mode.Name, Text = text, Position = t.position, Ground = Mode.Ground,
                Yaw = ViewAngles.Yaw(t.rotation), Pitch = ViewAngles.Pitch(t.rotation),
                Lens = Mode.Camera.orthographic ? $"orthographic size {Mode.Camera.orthographicSize:0.##}" : $"fov {Mode.Camera.fieldOfView:0.##}",
                Feet = feet, LookAt = lookAt, Look = look,
            };
            string shot = notes.Add(note, Shot());
            hud.Say($"note {notes.Count} saved · {shot}");
            Debug.Log($"World: walk review: note {notes.Count} → {notes.NotesFile} ({shot})");
        }

        /// <summary>What the mode stands on, and what the crosshair (or, for the tactical camera, the cursor) is on.</summary>
        void Identify(Vector2? pointer)
        {
            feet = plan.At(Mode.Ground);
            var cam = Mode.Camera;
            bool cursor = cam.orthographic && pointer is { } at && cam.pixelRect.Contains(at);
            var ray = cursor ? cam.ScreenPointToRay(pointer.Value) : cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (heights.Hit(ray, Reach, out var hit))
            {
                lookAt = hit;
                look = plan.At(hit + ray.direction * HeightField.Cell * 0.5f); // half a cell on: inside the wall it met
            }
            else { lookAt = null; look = default; }
        }

        /// <summary>The lines the overlay shows: mode, position, ids underfoot, ids in view, notes.</summary>
        public string[] Readout()
        {
            var t = Mode.Camera.transform;
            var lines = new List<string>
            {
                $"<b>{Mode.Name.ToUpperInvariant()}</b>  {Mode.Blurb}",
                $"x {t.position.x:F1}  z {t.position.z:F1}  height {t.position.y:F1}   yaw {ViewAngles.Yaw(t.rotation):F0}°  pitch {ViewAngles.Pitch(t.rotation):F0}°   {Mode.Status}",
                $"<b>at</b> x {Mode.Ground.x:F1}  z {Mode.Ground.z:F1}:  {feet}",
                lookAt is { } p ? $"<b>{(Mode.Camera.orthographic ? "cursor" : "look")}</b> x {p.x:F1}  z {p.z:F1}  h {p.y:F1}:  {look}" : "<b>look</b>  sky",
                $"notes {notes.Count} in out/review/walk-notes.json",
            };
            if (Warning != null) lines.Add($"<b>!</b>  {Warning}");
            return lines.ToArray();
        }

        /// <summary>The current camera rendered at the Game view's size, as Shots renders a view (its shadow fit applied for the render).</summary>
        byte[] Shot()
        {
            var cam = Mode.Camera;
            int w = Mathf.Max(cam.pixelWidth, 320), h = Mathf.Max(cam.pixelHeight, 180);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            var urp = UniversalRenderPipeline.asset;
            try
            {
                cam.targetTexture = rt;
                using (cam.TryGetComponent<TacticalShadows>(out var fit) ? fit.Use() : TacticalShadows.Save(urp))
                {
                    if (!fit) urp.shadowDistance = Mode.ShadowDistance;
                    cam.Render();
                }
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                return tex.EncodeToPNG();
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(tex);
                rt.Release();
                UnityEngine.Object.Destroy(rt);
            }
        }

        public void Dispose() { input.Dispose(); eye.Dispose(); RenderSettings.fog = sceneFog; }
    }
}
