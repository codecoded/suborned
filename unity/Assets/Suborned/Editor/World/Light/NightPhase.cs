using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Play;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>Brief 026 round 2 (Glint, light.json `night`; Sky chose a night Handover): the second light state, built with the day code
    /// from the night block (sun = the moon, trilight ambient, fog, sky gradient, a night street-canyon reflection cube, the night grade as
    /// &lt;scene&gt;-grade-night.asset) and the night lamps from `night.lamps` beside the real fixtures, into a LightPhase under `lighting`.
    /// The scene is saved in the day state, so day frames do not change. Look only: nothing here is read by the sim.
    /// Lamp fixtures (the builder's names for light.json's kinds): table = the café table tops (civic/cafe-table-N), interior = the café
    /// interior piece, awning = the café bay's front, palm = the café palms (civic/cafe-palm-N), street = every DK_Streetlamp and
    /// DK_PlazaSphereLamp head. Vela's lamp pieces replace nothing yet: each light stands where its piece will.</summary>
    public static class NightPhase
    {
        public static int Lamps;

        public static void Build(Transform lighting, Transform root, Look look, WorldDto world)
        {
            var n = look.Light("night"); Lamps = 0;
            if (n == null) return;
            var go = new GameObject("phase"); go.transform.SetParent(lighting, false);
            var lp = go.AddComponent<LightPhase>();
            // day: the state the builder just made
            lp.sun = RenderSettings.sun;
            lp.day = Capture(lighting.Find("grade")?.GetComponent<Volume>());
            var probes = lighting.Find("probes") ? lighting.Find("probes").GetComponentsInChildren<ReflectionProbe>(true) : new ReflectionProbe[0];
            lp.probes = probes; lp.probeIntensities = probes.Select(p => p.intensity).ToArray();
            // night
            var s = new LightPhase.State();
            var sun = J.Get(n, "sun"); s.sunColour = look.Colour(sun); s.sunIntensity = J.F(sun, "intensity"); s.sunRotation = Quaternion.Euler(J.F(sun, "pitch"), J.F(sun, "yaw"), 0f);
            var moonDir = -(s.sunRotation * Vector3.forward);
            var amb = J.Get(n, "ambient"); s.ambientSky = look.Colour(J.Get(amb, "sky")); s.ambientEquator = look.Colour(J.Get(amb, "equator")); s.ambientGround = look.Colour(J.Get(amb, "ground"));
            var fog = J.Get(n, "fog"); s.fogColour = look.Colour(fog); s.fogStart = J.F(fog, "start"); s.fogEnd = J.F(fog, "end"); s.fogDensity = J.F(fog, "density");
            var sky = J.Get(n, "skyGradient");
            var skyMat = Atmosphere.Sky(look, sky, moonDir); skyMat.name = "sky-night";
            var skyPath = Path.ChangeExtension(HarbourWorldBuilder.ScenePath, null) + "-sky-night.mat"; AssetDatabase.DeleteAsset(skyPath); AssetDatabase.CreateAsset(skyMat, skyPath); s.skybox = skyMat;
            var rf = J.Get(n, "reflection"); var st = rf != null ? J.Get(rf, "street") : null;
            s.reflection = ReflectionCube.Make(look, sky, rf, moonDir, Path.ChangeExtension(HarbourWorldBuilder.ScenePath, null) + "-reflection-night.asset");
            // Glint: 0.25, not the block's 0.1 (at 0.1 the cube is invisible and chrome goes black); probes the same
            s.reflectionIntensity = st != null && J.Get(st, "intensityOverride") != null ? J.F(st, "intensityOverride") : J.F(rf, "intensity");
            s.probeIntensity = s.reflectionIntensity;
            s.grade = Atmosphere.Grade(lighting, Atmosphere.Profile(look, J.Get(n, "post"), "grade-night"), "grade-night"); s.grade.enabled = false;
            lp.night = s;
            // lamps
            var lamps = J.Get(n, "lamps"); var rules = J.Get(lamps, "rules");
            lp.liveCap = (int)J.F(rules, "liveCap"); lp.liveRadius = J.F(rules, "liveRadiusM");
            var cycle = J.A(J.Get(lamps, "palette"), "cycle").Select(o => look.Palette((string)o)).ToArray();
            var group = Nodes.Group(lighting, "night-lamps");
            var made = new List<(Light l, float i)>();
            var kinds = J.A(lamps, "kinds").ToDictionary(k => J.S(k, "kind"), k => k);
            var all = root.GetComponentsInChildren<Transform>(true);
            Color Col(object k, int i) { var c = J.S(k, "colour"); return c == "cycle" ? cycle[i % cycle.Length] : look.Palette(c); }
            Light Make(object k, string name, Vector3 at, Vector3? aim, int i)
            {
                var o = new GameObject(name); o.transform.SetParent(group, false); o.transform.position = at;
                var l = o.AddComponent<Light>();
                l.type = (J.S(k, "light") ?? "point") == "spot" ? LightType.Spot : LightType.Point;
                l.color = Col(k, i); l.intensity = J.F(k, "intensity"); l.range = J.F(k, "range");
                l.shadows = LightShadows.None; l.cookie = null; l.renderMode = LightRenderMode.Auto; l.enabled = false; // off by day; LightPhase lights the nearest at night
                if (l.type == LightType.Spot)
                {
                    l.spotAngle = J.F(k, "spotAngle"); l.innerSpotAngle = J.Get(k, "inner") != null ? l.spotAngle * J.F(k, "inner") : l.spotAngle * 0.35f;
                    if (aim.HasValue) o.transform.rotation = Quaternion.LookRotation((aim.Value - at).normalized, Mathf.Abs((aim.Value - at).normalized.y) > 0.99f ? Vector3.forward : Vector3.up);
                }
                made.Add((l, l.intensity)); return l;
            }
            // table: a point 0.16 m over each café table top (the marker stands on the top)
            if (kinds.TryGetValue("table", out var kt))
            {
                int i = 0; foreach (var t in all.Where(x => System.Text.RegularExpressions.Regex.IsMatch(x.name, @"cafe-table-\d+$")).OrderBy(x => x.name, System.StringComparer.Ordinal))
                {
                    // Splice's table lamps (CafeLamp_Testbed_<Hue>, the table's own `<table>/lamp` in the export): the light in its opal, its hue
                    // night.lamp<Hue>; a table with no lamp piece keeps the light over its top in the cycle colour
                    var lampM = world.Civic.FirstOrDefault(m => m.Id == t.name + "/lamp" && m.Kit != null && m.Kit.Id.StartsWith("CafeLamp_"));
                    var l = Make(kt, $"table/{t.name}", lampM != null ? new Vector3(lampM.Kit.At.x, lampM.Base + J.F(kt, "height"), lampM.Kit.At.y) : t.position + Vector3.up * J.F(kt, "height"), null, i++);
                    if (lampM != null && lampM.Kit.Id.LastIndexOf('_') is int u && u > 0 && look.HasPalette("night.lamp" + lampM.Kit.Id[(u + 1)..])) l.color = look.Palette("night.lamp" + lampM.Kit.Id[(u + 1)..]);
                }
            }
            var bay = all.FirstOrDefault(x => x.name.StartsWith("DK_CafeBay")); var inside = all.FirstOrDefault(x => x.name.StartsWith("DK_CafeInterior"));
            Vector3 front = Vector3.back; Bounds bb = default;
            if (bay)
            {
                bb = Bounds(bay); front = bay.forward; front.y = 0; front.Normalize(); // the bay piece faces out of its front (as every kit facade piece does): the awning and the street side
            }
            // interior: `count` points across the interior at `height` over its floor
            if (kinds.TryGetValue("interior", out var ki) && inside)
            {
                var ib = Bounds(inside); int c = (int)J.F(ki, "count"); var along = Vector3.Cross(Vector3.up, front).normalized; float w = Mathf.Abs(Vector3.Dot(ib.size, along)) * 0.7f;
                for (int i = 0; i < c; i++) { var p = ib.center + along * (c == 1 ? 0 : -w / 2 + w * i / (c - 1)); p.y = inside.position.y + J.F(ki, "height"); Make(ki, $"interior/{i}", p, null, i); }
            }
            // awning: one spot per table bay along the café front, under the awning (3 m), aimed at the paving 1.15 m out
            if (kinds.TryGetValue("awning", out var ka) && bay)
            {
                var along = Vector3.Cross(Vector3.up, front).normalized; float w = Mathf.Abs(Vector3.Dot(bb.size, along)) * 0.9f; float depth = Mathf.Abs(Vector3.Dot(bb.size, front)) / 2f; int c = J.Get(ka, "count") != null ? (int)J.F(ka, "count") : 6; // Glint round 3: 4
                for (int i = 0; i < c; i++)
                {
                    var p = bb.center + front * depth + along * (-w / 2 + w * i / (c - 1)); p.y = bay.position.y + 3f;
                    var aim = p + front * 1.15f; aim.y = bay.position.y;
                    // Glint round 3: an awning pool that lands on a table (its lamp's pool) clips; that awning's aim moves 0.6 m along the front, away from it
                    foreach (var tb in all.Where(x => System.Text.RegularExpressions.Regex.IsMatch(x.name, @"cafe-table-\d+$")))
                    { var dd = tb.position - aim; dd.y = 0; if (dd.magnitude < 1.5f) { aim -= along * Mathf.Sign(Vector3.Dot(dd, along) == 0 ? 1f : Vector3.Dot(dd, along)) * 0.6f; break; } }
                    Make(ka, $"awning/{i}", p, aim, i);
                }
            }
            // palm: an uplight 0.425 m out from each café palm, toward the street, aimed 3 m up the trunk
            if (kinds.TryGetValue("palm", out var kp))
            {
                int i = 0; foreach (var t in all.Where(x => System.Text.RegularExpressions.Regex.IsMatch(x.name, @"cafe-palm-\d+$")).OrderBy(x => x.name, System.StringComparer.Ordinal))
                { var p = t.position + front * 0.425f + Vector3.up * 0.1f; Make(kp, $"palm/{t.name}", p, t.position + Vector3.up * 3f, i++); }
            }
            // street: a spot down from every street and plaza lamp head (each piece's top less 0.4 m), one per head
            if (kinds.TryGetValue("street", out var ks))
            {
                var seen = new HashSet<Vector2Int>(); int i = 0;
                foreach (var t in all.Where(x => (x.name.StartsWith("DK_Streetlamp") || x.name.StartsWith("DK_PlazaSphereLamp")) && PrefabUtility.IsOutermostPrefabInstanceRoot(x.gameObject)).OrderBy(x => x.position.x).ThenBy(x => x.position.z))
                {
                    var key = new Vector2Int(Mathf.RoundToInt(t.position.x * 2), Mathf.RoundToInt(t.position.z * 2)); if (!seen.Add(key)) continue;
                    var b = Bounds(t); var head = new Vector3(t.position.x, b.max.y - 0.4f, t.position.z);
                    Make(ks, $"street/{i}", head, head + Vector3.down * 4f, i); i++;
                }
                // the civic square's lamps are batched kit (an empty marker per lamp): the head from the export's height
                var byName = new Dictionary<string, Transform>(System.StringComparer.Ordinal); foreach (var t in all) byName.TryAdd(t.name, t);
                foreach (var m in world.Civic.Where(m => m.Kit != null && (m.Kit.Id.StartsWith("PlazaSphereLamp") || m.Kit.Id.StartsWith("Streetlamp"))).OrderBy(m => m.Id, System.StringComparer.Ordinal))
                {
                    if (!byName.TryGetValue(m.Id, out var t)) continue;
                    var key = new Vector2Int(Mathf.RoundToInt(t.position.x * 2), Mathf.RoundToInt(t.position.z * 2)); if (!seen.Add(key)) continue;
                    var head = new Vector3(t.position.x, m.Base + m.Height - 0.4f, t.position.z);
                    Make(ks, $"street/{m.Id}", head, head + Vector3.down * 4f, i); i++;
                }
            }
            // office (Glint round 3): five lights in the Records Office public entrance's own frame (the Handover's front, Inez and the guard;
            // pivot on the face at the door axis, forward out of the face; landing 0.75 m over the street)
            if (kinds.TryGetValue("office", out var ko) && all.FirstOrDefault(x => x.name.StartsWith("DK_RecordsEntrance") && !x.name.Contains("Official") && PrefabUtility.IsOutermostPrefabInstanceRoot(x.gameObject)) is Transform door)
            {
                Vector3 o0 = door.position, fw = door.forward, rt = door.right, up = Vector3.up; float landing = 0.75f;
                foreach (var part in J.A(ko, "parts"))
                {
                    string pn = J.S(part, "name"); int cnt = J.Get(part, "count") != null ? (int)J.F(part, "count") : 1;
                    for (int j = 0; j < cnt; j++)
                    {
                        Vector3 at, aim;
                        switch (pn)
                        {
                            case "door": at = o0 + fw * 0.6f + up * (landing + 2.9f); aim = at + Vector3.down; break;
                            case "landing": at = o0 + fw * 1f + up * (landing + 3.2f); aim = o0 + fw * 0.75f + up * landing; break;
                            case "pierWash": { float side = j == 0 ? -3.5f : 3.5f; at = o0 + rt * side + fw * 0.4f + up * 0.1f; aim = o0 + rt * side + up * 3.6f; break; } // the piers at about ±3.5 m (the piece is 8 m across)
                            case "forecourt": at = o0 + fw * 5.2f + up * 4f; aim = at + Vector3.down * 4f; break; // where the door's keep-clear meets the lane (5.2 m out); light.json's plan (64, -40.5) is the face itself
                            default: throw new InvalidDataException($"World: light.json night.lamps office part `{pn}` has no placement");
                        }
                        Make(part, $"office/{pn}{(cnt > 1 ? "-" + j : "")}", at, aim, j);
                    }
                }
            }
            lp.lamps = made.Select(m => m.l).ToArray(); lp.lampIntensity = made.Select(m => m.i).ToArray();
            Lamps = made.Count;
            Debug.Log($"World: night phase built (light.json night): moon {s.sunIntensity}, reflection {s.reflectionIntensity}, grade-night; lamps {made.Count} (" +
                      string.Join(", ", made.GroupBy(m => m.l.name.Split('/')[0]).Select(g => $"{g.Key} {g.Count()}")) + $"), at most {lp.liveCap} lit within {lp.liveRadius} m; the scene is saved by day");
        }

        static LightPhase.State Capture(Volume grade)
        {
            var sun = RenderSettings.sun;
            return new LightPhase.State
            {
                sunColour = sun ? sun.color : Color.white, sunIntensity = sun ? sun.intensity : 1f, sunRotation = sun ? sun.transform.rotation : Quaternion.identity,
                ambientSky = RenderSettings.ambientSkyColor, ambientEquator = RenderSettings.ambientEquatorColor, ambientGround = RenderSettings.ambientGroundColor,
                fogColour = RenderSettings.fogColor, fogStart = RenderSettings.fogStartDistance, fogEnd = RenderSettings.fogEndDistance, fogDensity = RenderSettings.fogDensity,
                skybox = RenderSettings.skybox, reflection = RenderSettings.customReflectionTexture as Cubemap, reflectionIntensity = RenderSettings.reflectionIntensity, grade = grade,
            };
        }

        static Bounds Bounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }
    }
}
