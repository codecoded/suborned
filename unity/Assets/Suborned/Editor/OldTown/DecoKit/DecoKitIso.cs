#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The 2.5D look test: the game's camera (GDD: fixed isometric-style orthographic, no free orbit; the Harbor
    /// slice's 38 deg pitch at 45 deg yaw, Q/E turning it in 90 deg steps; ortho size 8 close / 14 play / 40+ tactical,
    /// the play zoom brought in from 22 so the crowds and the kit read)
    /// over Southside and, for comparison, the boulevard. Orthographic from 150 m back along the view, so nothing in
    /// the district clips; fog thinned for the shots (it measures from the lens, which sits far off in the air).
    ///   -dkShots "square,square_r"   by name, "" = all        -dkIsoW 1920 -dkIsoH 1080
    /// The play shots (focus on the ground) run the game camera's cutaway (DKCutaway); the art shots, framed up in the
    /// air, do not.   -dkCut column | band | off   -dkGhost .2 (what stays in the disc)   -dkCutR .8 (x ortho size)
    ///   -dkFeather 2 (m)   -dkCutShadow 1 (the ghost casts a ghost shadow; 0 = full shadow)
    ///   -dkPitch 38   -dkZoom 1 (x the play shots' ortho size; the art shots keep theirs)
    internal static class DecoKitIso
    {
        const float Pitch = 38f, Back = 150f, Fog = .35f;

        // name, ground focus, ortho size (half the frame height, m), yaw
        static readonly (string name, Vector3 focus, float size, float yaw)[] Shots =
        {
            ("square", new(0f, 0f, -125f), 14f, 45f),        // gameplay zoom over Orbit Square
            ("square_r", new(0f, 0f, -125f), 14f, 225f),     // the same, turned 180 (Q/E)
            ("square_q", new(0f, 0f, -125f), 14f, 135f),
            ("close", new(-12f, 0f, -148f), 8f, 45f),        // close zoom: the square's south-west corner, Rocket Green
            ("rocket", new(-45f, 0f, -195f), 14f, 315f),     // Rocket Lane
            ("satellite", new(8f, 0f, -225f), 14f, 45f),     // Satellite Street, Starline Green, Quay Road
            ("lane", new(-95f, 0f, -225f), 14f, 45f),        // Rocket Lane's west: RL2, the mews, RB1, SS5
            ("launch", new(-50f, 0f, -225f), 11f, 135f),     // Launch Green from the south-east
            ("hero", new(-50f, 20f, -76f), 36f, 45f),       // Constellation (N1): the deck, the towers rising out of frame
            ("hero_tall", new(-50f, 95f, -76f), 95f, 45f),   // the whole of its three towers
            ("hero_deck", new(-58f, 20f, -86f), 18f, 45f),   // the deck close up
            ("seam", new(0f, 0f, -45f), 17f, 45f),           // Sunset Way: the boulevard's 26-40 m walls meet Southside
            ("boulevard", new(0f, 0f, 60f), 17f, 45f),       // the boulevard itself in 2.5D
            ("roofs_s", new(8f, 30f, 70f), 60f, 45f),        // the boulevard's southern roofs: Market Street, the Disc, the Starburst
            ("roofs_n", new(-4f, 30f, 280f), 60f, 45f),      // the northern roofs: the Lantern, the Terrace, the parade
            ("district", new(-20f, 40f, 150f), 165f, 45f),   // the whole boulevard district
            ("tactical", new(-30f, 0f, -150f), 48f, 45f),    // tactical zoom
            ("quarter", new(-45f, 0f, -170f), 95f, 45f),     // the whole quarter
        };

        /// Reopen the saved street and capture (no rebuild).
        internal static void Reshoot()
        {
            DecoKitStreetDemo.OpenSaved().enabled = false;
            DecoKitHarbour.Ocean();
            ShootAll(DecoKitStage.Arg("dkOut", "iso"), DecoKitStage.Arg("dkShots", ""));
        }

        internal static void ShootAll(string prefix, string only)
        {
            var pick = only.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
            Shoot(prefix, Shots.Where(s => pick.Length == 0 || pick.Contains(s.name)));
        }

        /// Captures each shot to CaptureDir/<prefix>_<name>.png: the play shots (focus on the ground) with the cutaway.
        internal static void Shoot(string prefix, IEnumerable<(string name, Vector3 focus, float size, float yaw)> shots)
        {
            int w = (int)DecoKitStage.Arg("dkIsoW", 1920f), h = (int)DecoKitStage.Arg("dkIsoH", 1080f);
            float pitch = DecoKitStage.Arg("dkPitch", Pitch), zoom = DecoKitStage.Arg("dkZoom", 1f);
            var cam = DecoKitStage.Camera("DK iso camera", 30f);
            cam.orthographic = true;
            cam.nearClipPlane = -Back * 2f;  // orthographic: the towers' upper floors stand nearer the lens than Back
            cam.farClipPlane = Back + 700f;
            float fog = RenderSettings.fogDensity;
            RenderSettings.fogDensity = fog * Fog;
            try
            {
                Object.DestroyImmediate(DecoKitStage.Render(cam, 480, 270));  // warm-up (NaN bloom on a fresh scene)
                foreach (var s in shots)
                {
                    bool play = s.focus.y <= 0f;
                    float size = play ? s.size * zoom : s.size;
                    var rot = Quaternion.Euler(pitch, s.yaw, 0f);
                    cam.transform.SetPositionAndRotation(s.focus - rot * Vector3.forward * Back, rot);
                    cam.orthographicSize = size;
                    Cut(s.focus, cam.transform.forward, size);
                    // shadows must reach from the lens (Back) through the frame's depth
                    DecoKitStage.WithShadows(Back + size * 4f, () =>
                        DecoKitStage.Capture(cam, w, h, Path.Combine(CaptureDir, $"{prefix}_{s.name}.png")));
                    Debug.Log($"[DecoKit] iso {s.name}: size {size}, pitch {pitch}, yaw {s.yaw}, cutaway {(play ? DecoKitStage.Arg("dkCut", "column") : "off")}");
                }
            }
            finally
            {
                DKCutaway.Clear();
                RenderSettings.fogDensity = fog;
                Object.DestroyImmediate(cam.gameObject);
            }
        }

        /// The game camera's cutaway round a play shot's focus: a disc of .8 x the ortho size (at most 40 m), 2 m feather,
        /// thinned to DKCutaway's ghost from 4.5 m up.
        static void Cut(Vector3 focus, Vector3 forward, float size)
        {
            string mode = DecoKitStage.Arg("dkCut", "column");
            if (focus.y > 0f || mode == "off") { DKCutaway.Clear(); return; }
            DKCutaway.Set(focus, forward, Mathf.Min(40f, DecoKitStage.Arg("dkCutR", .8f) * size), DecoKitStage.Arg("dkFeather", 2f),
                          DecoKitStage.Arg("dkGhost", DKCutaway.Ghost), 4.5f, mode == "column",
                          DecoKitStage.Arg("dkCutShadow", 1f) > .5f);
        }
    }
}
#endif
