#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Walkable pavement proof: the Streets study "limestone_flags" rebuilt from paving v2, captured from the study's
    /// camera (eye level, a step off the kerb, looking up the boulevard) so the two compare frame for frame.
    internal static class DecoKitStreetDemo
    {
        const string SceneName = "OldTownProof";
        const float Far = 1500f;

        // Tunables (batch: -dkExposure -dkSun -dkFill -dkSkyGain -dkFov -dkCam x,y,z -dkLook x,y,z -dkOut)
        internal const string Colour = "plan+livery+soft";  // DecoKitColourways scheme
        static float Exposure => DecoKitStage.Arg("dkExposure", -.8f);
        static float SunIntensity => DecoKitStage.Arg("dkSun", 3f);
        static float Fill => DecoKitStage.Arg("dkFill", 1f);
        static float SkyGain => DecoKitStage.Arg("dkSkyGain", 1.5f);
        static Vector3 CamPos => Vec("dkCam", new Vector3(7.85f, 1f, -14f));
        static Vector3 CamLook => Vec("dkLook", new Vector3(7.3f, .35f, 30f));
        // late-morning sun behind-left at 45 deg: front-lights the landmarks down the vista, the east walk and kerb
        // face stay lit past the 37 m west frontage's shadow
        static Vector3 SunTravel => Vec("dkSunDir", new Vector3(.5f, -.7f, .5f));
        static readonly Vector3 WalkPos = new(12.5f, L.Walk, -12f), WalkLook = new(9f, 1.2f, 30f);  // Play spawn on the promenade
        // warmer bounce than the plaza: the study's shadows on limestone read R/B 1.18 (sky from sky_from_image.py)
        static readonly DecoKitAtmosphere.Ambient Ambient = new(new Color(.46f, .49f, .54f), new Color(.53f, .48f, .41f),
                                                                new Color(.6f, .5f, .37f));

        internal static void Build()
        {
            DecoKitStage.NewScene(SceneName);
            // fog .0005 and saturation 25 (were .0011, 10): the colour study found the cream haze whitened the distance
            DecoKitSky.Apply("Street", -SunTravel, Ambient, SkyGain, Fill, DecoKitStage.Arg("dkFogDensity", .0005f));
            var sun = DecoKitStage.Directional("Sun", SunTravel, new Color(1f, .93f, .8f), SunIntensity, true);
            var grade = DecoKitStage.Grade("DK_StreetGrade", TonemappingMode.ACES, Exposure, 10f, DecoKitStage.Arg("dkSaturation", 25f));
            DecoKitLens.SoftFocus(grade, DecoKitStage.Arg("dkSoftStart", 300f), DecoKitStage.Arg("dkSoftEnd", 1800f),
                                  DecoKitStage.Arg("dkSoftRadius", 1.2f));  // airbrush: the distance goes soft
            DecoKitLens.SunFlare(sun, DecoKitStage.Arg("dkFlare", 1f));
            var street = new GameObject("Deco Street").transform;
            var placer = new DecoKitPlacer(street);
            L.Build(placer);
            DecoKitCircus.Build(placer);
            DecoKitHarbour.Build(placer);
            DecoKitGardens.Build(placer);
            DecoKitSkyTraffic.Build(placer);  // sky rail, aircar lanes, rockets
            DecoKitStreetLife.Build(placer);  // last on the pavements: it keeps only the spots that clear what stands
            DecoKitDistrictLife.Build(street);  // people and traffic, on the spots street life left clear
            DecoKitSouthside.Build();         // the low-rise quarter south of the boulevard, under its own root
            DecoKitCityMass.Build(DecoKitSkyRail.Route());  // mid-ground city out to the far-city band
            DecoKitColourways.Apply(DecoKitStage.Arg("dkColour", Colour));  // before the bake: the probes see the paint
            Ground();
            DecoKitRoadMarkings.Build(street);  // off the kerbs, before the merge folds them into tiles
            DecoKitTileMerge.Merge(street);
            DecoKitCutawayTags.Apply();  // last of the geometry: the 2.5D cutaway's architecture
            DecoKitWalker.Check(WalkPos);
            DecoKitWalker.Check(DecoKitSouthside.WalkTo);  // and the quarter is walkable ground
            for (float z = L.Z.x + 20f; z < L.Z.y; z += 40f)
                DecoKitAtmosphere.Probe($"Street probe {z:0}", new Vector3(0f, 5f, z), new Vector3(2f * L.FrontX, 22f, 44f));
            DecoKitAtmosphere.Probe("Circus probe", P.Circus + Vector3.up * 6f, new Vector3(80f, 24f, 80f));
            DecoKitAtmosphere.Probe("Gardens probe", new Vector3(-78f, 4f, 105f), new Vector3(70f, 16f, 300f));
            foreach (var (name, c, size) in DecoKitSouthside.Probes) DecoKitAtmosphere.Probe(name, c, size);
            for (float z = P.HarbourZ.x + 40f; z < P.HarbourZ.y; z += 80f)
                DecoKitAtmosphere.Probe($"Harbour probe {z:0}", new Vector3(85f, 3f, z), new Vector3(80f, 20f, 84f));
            DecoKitStage.BakeEnvironment(SceneName);
            DecoKitShowpiece.Apply("SpaceHQ", SkyGain);  // a custom probe: the bake leaves it alone
            DecoKitOcclusion.Bake();
            placer.ReportTop("Pave_Flags_0", L.Walk);

            var cam = DecoKitStage.Camera("DK street study camera", 60f);
            cam.nearClipPlane = .05f;
            cam.farClipPlane = 2500f;  // background towers to ~1 km, the fog wall beyond
            ShootAll(cam, DecoKitStage.Arg("dkOut", "unity_street"), "");
            cam.enabled = false;
            DecoKitIso.ShootAll(DecoKitStage.Arg("dkOut", "unity_street") + "_iso", "");
            DecoKitWalker.Make(WalkPos, WalkLook);
            DecoKitStage.Save();
            var missing = placer.Missing.Count == 0 ? "none" : string.Join(", ", placer.Missing.OrderBy(n => n));
            Debug.Log($"[DecoKit] street built; missing prefabs: {missing}");
        }

        /// Reopen the saved street and capture again (shader / grade iteration without the rebuild and bake):
        /// -dkShots "harbour,aerial_e" picks shots by suffix ("main" = the study framing), empty = all.
        internal static void Reshoot()
        {
            var cam = OpenSaved();
            DecoKitHarbour.Ocean();  // the sea's material values live in code
            cam.enabled = true;
            Object.DestroyImmediate(DecoKitStage.Render(cam, 1536, 1024));  // warm-up: a freshly opened scene's first frame blows out (NaN bloom)
            ShootAll(cam, DecoKitStage.Arg("dkOut", "unity_street"), DecoKitStage.Arg("dkShots", ""));
            cam.enabled = false;
        }

        /// Reopen the saved street once per colourway (DecoKitColourways) and capture the same shots:
        /// -dkSchemes "none,plan+livery" -dkShots "main,circus". Probes keep the saved bake. URP evicts probe-atlas
        /// entries by frame count, which a batch job never advances: each scheme's reopened probes pile up, and past
        /// ~2 schemes later ones can skip probes ("atlas being full").
        internal static void PaintStudy()
        {
            foreach (var scheme in DecoKitStage.Arg("dkSchemes", "plan+livery,plan+livery+muted,plan+livery+soft").Split(','))
            {
                var cam = OpenSaved();
                DecoKitHarbour.Ocean();
                DecoKitColourways.Apply(scheme);
                cam.enabled = true;
                Object.DestroyImmediate(DecoKitStage.Render(cam, 1536, 1024));  // warm-up (NaN bloom)
                var file = new string(scheme.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
                ShootAll(cam, $"paint_{file}", DecoKitStage.Arg("dkShots", "main,circus,parade,harbour,aerial_e"));
            }
        }

        /// Re-apply the colour plan (-dkColour, default the build's) to the saved street and save it, no rebuild: the
        /// probes keep their bake. Captures -dkShots (default none) as -dkOut.
        internal static void Repaint()
        {
            var cam = OpenSaved();
            DecoKitColourways.Apply(DecoKitStage.Arg("dkColour", Colour));
            DecoKitStage.Save();
            var shots = DecoKitStage.Arg("dkShots", "");
            if (shots == "") return;
            DecoKitHarbour.Ocean();
            cam.enabled = true;
            Object.DestroyImmediate(DecoKitStage.Render(cam, 1536, 1024));  // warm-up (NaN bloom)
            ShootAll(cam, DecoKitStage.Arg("dkOut", "unity_street"), shots);
            cam.enabled = false;
        }

        /// Reopen the saved street and log the draw cost at every shot, no captures (culling iteration).
        internal static void Cost()
        {
            var cam = OpenSaved();
            foreach (var s in Shots())
            {
                Aim(cam, s.pos, s.look, s.fov);
                var seen = DecoKitDrawCost.Log(cam, s.name);
                Debug.Log($"[DecoKit] cost {s.name}: {DecoKitDrawCost.Visible(cam, seen, DecoKitStage.Arg("dkCostTop", "") == s.name)} submeshes after the camera's culling");
            }
        }

        internal static Camera OpenSaved()
        {
            EditorSceneManager.OpenScene($"{ScenesDir}/{SceneName}.unity");
            return Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .First(c => c.name == "DK street study camera");
        }

        // suffix, eye, look-at, fov, whole-skyline shadow distance
        static (string name, Vector3 pos, Vector3 look, float fov, bool wide)[] Shots() => new[]
        {
            ("main", CamPos, CamLook, DecoKitStage.Arg("dkFov", 60f), false),  // the study's framing
            ("corner", new Vector3(6.6f, 1.65f, 50f), new Vector3(11f, .4f, 62f), 55f, false),
            ("walk", WalkPos + Vector3.up * 1.5f, WalkLook, 55f, false),  // player eye (1.65 m)
            ("promenade", new Vector3(-12.5f, 1.7f, 96f), new Vector3(-11f, 1.1f, 150f), 55f, false),     // west walk: tram stop, civic hall
            ("promenade_e", new Vector3(12f, 1.7f, 252f), new Vector3(11.5f, 1.1f, 320f), 55f, false),    // east walk north of the circus
            ("side", new Vector3(12f, 1.4f, L.SideZ.x + .9f), new Vector3(270f, 14f, 68f), 55f, false),  // Market St to the dock
            ("skyline", new Vector3(-3.5f, 1.7f, -30f), new Vector3(0f, 70f, 450f), 60f, false),  // tram lane, on axis
            ("plaza", new Vector3(-7f, 1.7f, 378f), new Vector3(15f, 43f, 450f), 75f, false),      // foot of the HQ stair
            ("sky", new Vector3(3f, 1.7f, 100f), new Vector3(3f, 1.7f, 100f) + DecoKitSky.Dir(-4f, 30f) * 100f, 72f, false),
            ("sunward", new Vector3(14f, 1.7f, 360f), new Vector3(14f, 1.7f, 360f) + DecoKitSky.Dir(-150f, 22f) * 100f, 70f, false),
            ("air", new Vector3(4f, 42f, -60f), new Vector3(0f, 34f, 200f), 60f, false),  // aircar lane height
            ("circus", new Vector3(5f, 1.7f, 158f), new Vector3(-2f, 9f, 206f), 62f, false),  // kerb, into the circus
            ("canopy", P.Circus + P.Dir(45f) * 29f + Vector3.up * 1.7f, P.Circus + P.Dir(50.6f) * 34f + Vector3.up * 4.5f, 55f, false),  // a crescent joint
            ("parade", new Vector3(16f, 1.7f, 203f), new Vector3(170f, 22f, 206f), 60f, false),  // to the skyport
            ("harbour", new Vector3(92f, P.PromenadeY + 1.65f, 16f), new Vector3(100f, 6f, 230f), 60f, false),  // clear of the rail pylons (x 88)
            ("alley", new Vector3(20f, 1.7f, 137f), new Vector3(46f, 4f, 137f), 60f, false),   // the east lane
            ("gardens", new Vector3(-44f, 1.7f, 203f), new Vector3(-104f, 5f, 206f), 62f, false),  // the west gate
            ("canal", new Vector3(-72f, 1.7f, 186f), new Vector3(-78f, 2f, 20f), 55f, false),
            ("aerial", new Vector3(-190f, 150f, -110f), new Vector3(10f, 50f, 380f), 50f, true),  // layout check
            ("aerial_e", new Vector3(300f, 140f, -160f), new Vector3(0f, 10f, 190f), 50f, true),  // from the sea
            ("plan", new Vector3(0f, 420f, 150f), new Vector3(0f, 0f, 151f), 70f, true),          // top down
            ("hq", new Vector3(-70f, 330f, 370f), new Vector3(0f, 273f, 450f), 45f, true),         // the HQ deck (272.7 m) from above
            ("aerial_full", new Vector3(480f, 380f, 560f), new Vector3(-20f, 0f, 150f), 50f, true),  // the whole city, from over the sea (NE: the gantry off to one side)
            ("plan_full", new Vector3(0f, 1000f, 180f), new Vector3(0f, 0f, 181f), 60f, true),      // the whole layout, top down
        };

        static void ShootAll(Camera cam, string shot, string only)
        {
            var pick = only.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
            var list = Shots().Where(s => pick.Length == 0 || pick.Contains(s.name)).ToArray();
            foreach (var wide in new[] { false, true })
                DecoKitStage.WithShadows(wide ? 1200f : 120f, () =>
                {
                    foreach (var s in list.Where(s => s.wide == wide))
                        Shoot(cam, s.pos, s.look, s.fov, s.name == "main" ? shot : $"{shot}_{s.name}");
                });
        }

        static void Shoot(Camera cam, Vector3 pos, Vector3 look, float fov, string file)
        {
            Aim(cam, pos, look, fov);
            DecoKitStage.Capture(cam, 1536, 1024, Path.Combine(CaptureDir, file + ".png"));
            DecoKitDrawCost.Log(cam, file);
        }

        static void Aim(Camera cam, Vector3 pos, Vector3 look, float fov)
        {
            cam.transform.position = pos;
            cam.transform.LookAt(look, Vector3.up);
            cam.fieldOfView = fov;
        }

        /// Road deck between the kerbs' gutters and plain stone beyond the frontage, out past the fog west and to the
        /// harbour steps east; Crown Circus lays its own ground inside its square.
        static void Ground()
        {
            var asphalt = DecoKitGroundMesh.Road();
            var stone = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/DK_Limestone_Tex.mat");
            float k = L.KerbX, kb = L.BoulevardKerbX, s0 = L.SideZ.x, s1 = L.SideZ.y, e = P.StepsX, v = L.Walk - .006f;  // verge: just under the flags (E10: kb south of the circus)
            var sq = DecoKitCircus.Square;
            // Southside (its own deck) cuts the boulevard's ground from its south edge to the walks' end
            float n = L.Z.x, s = DecoKitSouthside.Bounds.yMin, w = DecoKitSouthside.Bounds.xMin;
            DecoKitStage.Slab("Street road deck", Rect.MinMaxRect(-kb, n, kb + 2f, Far), -.002f, asphalt);  // + corner cells
            DecoKitStage.Slab("Street road deck S", Rect.MinMaxRect(-kb, -Far, kb + 2f, s), -.002f, asphalt);
            DecoKitStage.Slab("Side street deck", Rect.MinMaxRect(kb, s0, L.SideEnd, s1), -.002f, asphalt);
            DecoKitStage.Slab("Street east verge S", Rect.MinMaxRect(kb, n, e, s0 - 2f), v, stone);
            DecoKitStage.Slab("Street east verge SS", Rect.MinMaxRect(kb + 2f, -Far, e, s), v, stone);
            DecoKitStage.Slab("Street east verge SC", Rect.MinMaxRect(kb + 2f, s0 - 2f, e, s0), v, stone);
            DecoKitStage.Slab("Street east verge NC", Rect.MinMaxRect(kb + 2f, s1, e, s1 + 2f), v, stone);
            DecoKitStage.Slab("Street east verge N", Rect.MinMaxRect(kb, s1 + 2f, e, sq.yMin), v, stone);
            DecoKitStage.Slab("Street east verge C", Rect.MinMaxRect(sq.xMax, sq.yMin, e, sq.yMax), v, stone);
            DecoKitStage.Slab("Street east verge NN", Rect.MinMaxRect(k, sq.yMax, e, Far), v, stone);
            DecoKitStage.Slab("Street west verge S", Rect.MinMaxRect(w, n, -kb, sq.yMin), v, stone);
            DecoKitStage.Slab("Street west verge W", Rect.MinMaxRect(-Far, -Far, w, sq.yMin), v, stone);
            DecoKitStage.Slab("Street west verge SS", Rect.MinMaxRect(w, -Far, -kb, s), v, stone);
            DecoKitStage.Slab("Street west verge C", Rect.MinMaxRect(-Far, sq.yMin, sq.xMin, sq.yMax), v, stone);
            DecoKitStage.Slab("Street west verge N", Rect.MinMaxRect(-Far, sq.yMax, -k, Far), v, stone);
        }


        static Vector3 Vec(string key, Vector3 fallback)
        {
            var s = DecoKitStage.Arg(key, "");
            var f = s.Split(',').Select(t => float.TryParse(t, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : float.NaN).ToArray();
            return f.Length == 3 && f.All(v => !float.IsNaN(v)) ? new Vector3(f[0], f[1], f[2]) : fallback;
        }
    }
}
#endif
