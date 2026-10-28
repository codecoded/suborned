using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Brief 072-E phase 2 and 072-F (Latch, 2026-10-27): the after capture in batchmode, offscreen (no Editor window, no Game view),
    /// on the built HarbourDistrict scene, for when the Studio is in use. The views are Baseline072's (office-front and cell-open, yaw 30 / 120 /
    /// 210 / 300, the play camera posed as HandoverHost.PlaceCamera: ortho 14, pitch 38), rendered by Camera.Render into Capture's HDR target at
    /// 1920 x 1080. Nothing here is UnityStats (that needs the Game view): every count is reproduced from the scene and labelled an estimate.
    ///  1. shots: the eight views and the planets framing → briefs/072/shots/after/.
    ///  2. counts, for the scene as built (every face) and, on the same build, with EveryFace off (the procedural walls the modules replace,
    ///     `today`): Baseline072's census (16:9 frustum + LOD pick: visible renderers, LOD-picked triangles; the baseline's method, which also
    ///     counts shadows-only renderers), the main view without shadows-only renderers, distinct materials, a SetPass estimate (distinct shader
    ///     variants: forward over every visible material, the SSAO depth-normals prepass over the opaque ones, the shadow pass per cascade over
    ///     its casters), shadow casters and their triangles per cascade (URP's cascade split for this camera, reproduced: the camera culled
    ///     again in its own beginCameraRendering with the shadow distance TacticalShadows fits, then each cascade's culling planes and sphere
    ///     from CullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives, tested against each caster's bounds), every-face modules in
    ///     the frustum, and batchmode frame ms (Camera.Render at 1080p, GPU waited on; offscreen, indicative only) → briefs/072/latch/after/.
    ///  3. 072-F: per-material screen area. Every (material, layer) gets a MaterialId072 twin (its id in the colour); the frame is rendered into
    ///     a float target with post, SSAO and fog off, other renderer types off, the architecture fade's cut kept; the pixels are counted per id,
    ///     once with every material and once opaque only. Each material says what maps it binds → briefs/072/texture/area.json.
    ///  4. K-6 at lamplight-south-w/13 (Keel's ShopFill_4 through SideBay_4 at the 45° corner): every module of the building near the corner,
    ///     its L0 vertices against the two walls' lines; plus a close and a play-zoom shot of the corner.
    ///  1b. f1 (Redline phase 2 F1; only with -only f1): a sun-facing street SideBay wall at play zoom, module shadows from the L2 proxies against
    ///     their own LODs, three walls → briefs/072/shots/f1/.
    ///  5. 072-F: texture memory of the scene (texmem): every texture the scene's renderers bind, Profiler.GetRuntimeMemorySizeLong and the GPU
    ///     size from its format, size, mips and layers → briefs/072/latch/&lt;tag&gt;/texmem.json.
    /// `-tag textured` (072-F) writes shots to briefs/072/shots/textured, counts and texmem to briefs/072/latch/textured and the area census to
    /// briefs/072/texture/area-after.json; `-tag before` writes texmem only to briefs/072/latch/textured/texmem-before.json.
    /// `-tag g` (072-G, the rebuild after the palette, holes and grade) writes shots to briefs/072/shots/g, counts, texmem and area-g.json to
    /// briefs/072/latch/g, and f1 to briefs/072/shots/f1g at the three walls recorded in briefs/072/shots/f1/f1.json.
    /// `-tag a3 [-phase before|after]` (brief 064 A3 items 1, 5, 7): the no-look check only (After072.A3.cs) → briefs/072/shots/a3/.
    /// Everything changed is put back; nothing is saved.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.After072.Cli [-only shots,counts,area,k6,texmem] [-tag textured|g|a3] -logFile &lt;log&gt;</summary>
    public static partial class After072
    {
        const int W = Baseline072.W, H = Baseline072.H, WarmFrames = 8, TimedFrames = 30;
        static readonly string[] Ids = { "office-front", "cell-open" };

        public static void Cli() => HarbourWorldBuilder.Batch(Run);

        sealed class Ctx
        {
            public Camera cam; public TacticalShadows shadows; public ArchitectureFade fade; public float standOff;
            public List<Baseline072.View> views = new();
            public string cellFrom;
        }

        static void Run()
        {
            var t0 = DateTime.UtcNow;
            var a = Environment.GetCommandLineArgs(); int oi = Array.IndexOf(a, "-only");
            var only = oi >= 0 && oi + 1 < a.Length ? new HashSet<string>(a[oi + 1].Split(',')) : new HashSet<string> { "shots", "counts", "area", "k6" };
            int ti = Array.IndexOf(a, "-tag"); var tag = ti >= 0 && ti + 1 < a.Length ? a[ti + 1] : null;
            var root = HarbourWorldBuilder.RepoRoot;
            // `-tag g` (072-G): shots, counts, texmem and area-g.json under briefs/072/{shots,latch}/g; f1 at the spots recorded in shots/f1/f1.json → shots/f1g
            bool g = tag == "g";
            string shotsDir = tag == null || tag == "before" ? Path.Combine(root, "briefs", "072", "shots", "after") : tag == "textured" ? Path.Combine(root, "briefs", "072", "shots", "textured") : g ? Path.Combine(root, "briefs", "072", "shots", "g") : Path.Combine(root, "out", "scratch", "latch", "072", tag);
            string countsDir = Path.Combine(root, "briefs", "072", "latch", tag == null ? "after" : g ? "g" : "textured");
            string areaFile = g ? Path.Combine(countsDir, "area-g.json") : Path.Combine(root, "briefs", "072", "texture", tag == "textured" ? "area-after.json" : "area.json");
            // brief 074-C: `-tag 074before` / `-tag 074` (After072.C074.cs): shots and counts of the play views at yaw 30 / 210 plus the wide street view, and the colourway census
            // brief 075 (the A/B/C1-2 rebuild): `-tag 075` = the same views, counts and census → briefs/075/shots/after and briefs/075/latch
            // brief 075-E step 1 (the streamline rebuild): `-tag 075e` = the same views, counts and census → briefs/075/shots/stream/after072 and briefs/075/latch/stream
            // brief 075-F1 (billboards and posters): `-tag 075f1` = the same views, counts and census → briefs/075/shots/billboards/after072 and briefs/075/latch/billboards
            bool c074 = tag == "074before" || tag == "074" || tag == "075" || tag == "075e" || tag == "075f1";
            if (c074) { only = new HashSet<string> { "shots", "counts", "c074" }; shotsDir = Path.Combine(root, "briefs", "074", "shots", tag == "074" ? "after" : "before"); countsDir = Path.Combine(root, "briefs", "074", "latch", tag == "074" ? "after" : "before"); }
            if (tag == "075") { shotsDir = Path.Combine(root, "briefs", "075", "shots", "after"); countsDir = Path.Combine(root, "briefs", "075", "latch"); }
            if (tag == "075e") { shotsDir = Path.Combine(root, "briefs", "075", "shots", "stream", "after072"); countsDir = Path.Combine(root, "briefs", "075", "latch", "stream"); }
            if (tag == "075f1") { shotsDir = Path.Combine(root, "briefs", "075", "shots", "billboards", "after072"); countsDir = Path.Combine(root, "briefs", "075", "latch", "billboards"); }
            if (tag == "a3") only = new HashSet<string> { "a3" }; // brief 064 A3 items 1, 5, 7: the no-look check (After072.A3.cs)
            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            var c = new Ctx();
            var playGo = GameObject.Find("cameras/play") ?? throw new InvalidOperationException("072: no cameras/play in the scene");
            c.cam = playGo.GetComponent<Camera>(); c.shadows = playGo.GetComponent<TacticalShadows>(); c.fade = playGo.GetComponent<ArchitectureFade>();
            var host = UnityEngine.Object.FindAnyObjectByType<global::Suborned.Play.HandoverHost>(FindObjectsInactive.Include);
            c.standOff = host ? host.standOff : Baseline072.DefaultStandOff;
            Vector2 cellOpen = Baseline072.CellOpenFallback; c.cellFrom = "fallback (HostLess.OpeningFocus comment)";
            try { if (host != null) { var w = host.frame.World(global::Suborned.Play.HandoverHost.OpeningFocus); if (!float.IsNaN(w.x) && (w.x != 0 || w.z != 0)) { cellOpen = new Vector2(w.x, w.z); c.cellFrom = "HandoverHost.frame.World(OpeningFocus)"; } } }
            catch (Exception e) { Debug.LogWarning($"072: the host frame could not place OpeningFocus ({e.Message}); using {Baseline072.CellOpenFallback}"); }
            foreach (var (id, f) in new[] { ("office-front", Baseline072.OfficeFront), ("cell-open", cellOpen) })
                foreach (var y in Baseline072.Yaws) c.views.Add(new Baseline072.View { Id = id, Label = id, Focus = f, Yaw = y });

            if (c074) C074Views(c);
            var people = GameObject.Find("people"); if (people) people.SetActive(false);
            // TacticalShadows fits the cascades in its own beginCameraRendering; batchmode may open the scene without its OnEnable having run
            if (c.shadows) { Debug.Log($"072 after: TacticalShadows enabled {c.shadows.enabled}, active and enabled {c.shadows.isActiveAndEnabled}"); c.shadows.enabled = false; c.shadows.enabled = true; }
            if (c.fade) { c.fade.enabled = false; c.fade.enabled = true; }
            foreach (var oc in Camera.allCameras) if (oc != c.cam && oc.targetTexture == null) oc.enabled = false;
            var stamp = UnityEngine.Object.FindAnyObjectByType<WorldStamp>();
            var ef = UnityEngine.Object.FindAnyObjectByType<EveryFace>(FindObjectsInactive.Include);
            string exportLine = stamp ? $"{stamp.exportPath} sha256 {stamp.exportSha256}" : "no WorldStamp";
            Debug.Log($"072 after: scene from {exportLine}; cell-open from {c.cellFrom}; EveryFace {(ef ? $"{ef.modules.Length} modules, today {(ef.HasToday ? "built" : "absent")}" : "absent")}");

            using var session = new RenderSession();
            Baseline072.Pose(c.cam, c.shadows, c.fade, c.views[0].Focus, c.views[0].Yaw, c.standOff);
            session.Grab(c.cam, 64, 64); // the first render after a load comes out flat

            if (only.Contains("texmem")) TexMem(Path.Combine(countsDir, tag == "before" ? "texmem-before.json" : "texmem.json"), exportLine, tag ?? "after");
            if (only.Contains("a3")) A3(c, session, root, a);
            if (only.Contains("shots")) Shots(c, session, shotsDir);
            if (only.Contains("counts"))
            {
                var dir = countsDir; Directory.CreateDirectory(dir);
                var every = Counts(c, "every face (as built)");
                List<Count> today = null;
                if (!c074 && ef && ef.HasToday) { ef.Set(false); try { today = Counts(c, "today (EveryFace off, same build)"); } finally { ef.Set(true); } }
                File.WriteAllText(Path.Combine(dir, "offscreen.json"), CountsJson(c, exportLine, every, today));
            }
            if (only.Contains("c074")) C074Census(Path.Combine(countsDir, "census.json"), exportLine, tag, c);
            if (only.Contains("area")) Area(c, session, areaFile, exportLine);
            if (only.Contains("sd")) SurfaceDetailCheck(c, session, Path.Combine(root, "out", "scratch", "latch", "072", "sd"));
            if (only.Contains("f1")) F1(c, session, Path.Combine(root, "briefs", "072", "shots", g ? "f1g" : "f1"), stamp, g ? Path.Combine(root, "briefs", "072", "shots", "f1", "f1.json") : null);
            if (only.Contains("k6")) K6(c, session, Path.Combine(root, "briefs", "072", "latch", "after", "k6-lamplight-south-w-13.json"), Path.Combine(root, "briefs", "072", "shots", "after"), stamp);
            Debug.Log($"072 after: done in {(DateTime.UtcNow - t0).TotalSeconds:F0} s");
        }

        // ---------------------------------------------------------------- 1. shots

        static void Shots(Ctx c, RenderSession session, string dir)
        {
            Directory.CreateDirectory(dir);
            foreach (var v in c.views)
            {
                Baseline072.Pose(c.cam, c.shadows, c.fade, v.Focus, v.Yaw, c.standOff, ZoomOf(v));
                v.Shot = $"{v.Id}-y{v.Yaw:000}.png";
                File.WriteAllBytes(Path.Combine(dir, v.Shot), session.Grab(c.cam, W, H).EncodeToPNG());
            }
            if (Baseline072.PlanetShot(session, c.cam, c.shadows, Path.Combine(dir, "planets.png"), out var note)) Debug.Log($"072 after: planets ({note})");
            else Debug.LogWarning($"072 after: no planets shot ({note})");
            Debug.Log($"072 after: shots → {dir}");
        }

        // ---------------------------------------------------------------- 1b. F1: proxy against L0 shadows on a sun-facing side-bay wall

        /// <summary>Redline phase 2 F1 (Glint's call): three street-level SideBay columns (one per building) whose outward normal faces the sun most
        /// squarely, seen whole (no other building mass in the view ray of nine points on the wall) from a quarter yaw that looks at the face, nearest
        /// the office front first; each at play zoom 14 with the wall's point 5 m up at the frame's centre,
        /// twice: the modules casting from their L2 proxies (as built) and from their own LODs (L0 at play zoom; LookLab.SetModuleShadows),
        /// plus a 2x crop (the middle 960 x 540) of each. The architecture fade is off for these frames (the wall nearest the focus would ghost);
        /// the shell is in both. → briefs/072/shots/f1/ (f1-&lt;n&gt;-{proxy,l0}[-x2].png, f1.json).</summary>
        static void F1(Ctx c, RenderSession session, string dir, WorldStamp stamp, string spotsFile = null)
        {
            Directory.CreateDirectory(dir);
            var sun = RenderSettings.sun;
            if (!sun) foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)) if (l.type == LightType.Directional) { sun = l; break; }
            if (!sun) { Debug.LogWarning("072 f1: no directional light"); return; }
            var toSun3 = -sun.transform.forward; var toSun = new Vector2(toSun3.x, toSun3.z).normalized;
            var path = stamp ? Path.Combine(HarbourWorldBuilder.RepoRoot, stamp.exportPath) : RenderSession.ExportPath;
            var text = File.ReadAllText(path);
            var rx = new Regex("\"id\":\"([^\"]+)\",\"building\":\"[^\"]*\"[^{}]*?\"kit\":\\{\"id\":\"(SideBay[^\"]*)\"[^{}]*?\"at\":\\[([-\\d.]+),([-\\d.]+)\\]\\},\"base\":([-\\d.]+)[^{}]*?\"normal\":\\[([-\\d.]+),([-\\d.]+)\\](?:[^{}]|\\{[^{}]*\\})*?\"casts\":\"proxy-l2\"");
            var picks = new List<(string id, string kit, Vector2 at, Vector2 n, float dot, float dist)>();
            foreach (Match m in rx.Matches(text))
            {
                float b = P(m.Groups[5].Value); if (b > 1f) continue; // street level: the column's foot
                var at = new Vector2(P(m.Groups[3].Value), P(m.Groups[4].Value)); var n = new Vector2(P(m.Groups[6].Value), P(m.Groups[7].Value)).normalized;
                picks.Add((m.Groups[1].Value, m.Groups[2].Value, at, n, Vector2.Dot(n, toSun), (at - Baseline072.OfficeFront).magnitude));
            }
            if (picks.Count == 0) { Debug.LogWarning("072 f1: no street-level SideBay facade with an L2 proxy in the export"); return; }
            // the building masses (plan ring, base, height) to keep a wall another building hides out of the shot
            var masses = new List<(string id, List<Vector2> ring, float lo, float hi)>();
            foreach (Match m in Regex.Matches(text, "\"id\":\"(bld/[^\"]+)\",\"surface\":\"[^\"]*\",\"ring\":\\[((?:\\[[-\\d.]+,[-\\d.]+\\],?)+)\\],\"base\":([-\\d.]+),\"height\":([-\\d.]+)"))
            {
                var ring = Regex.Matches(m.Groups[2].Value, "\\[([-\\d.]+),([-\\d.]+)\\]").Select(x => new Vector2(P(x.Groups[1].Value), P(x.Groups[2].Value))).ToList();
                float lo = P(m.Groups[3].Value); masses.Add((m.Groups[1].Value, ring, lo, lo + P(m.Groups[4].Value)));
            }
            float tanP = Mathf.Tan(Baseline072.PlayPitch * Mathf.Deg2Rad);
            Vector2 Fwd(float y) => new(Mathf.Sin(y * Mathf.Deg2Rad), Mathf.Cos(y * Mathf.Deg2Rad));
            // how many of nine points on the wall (±2 m along it, 2 / 5 / 8 m up, 0.3 m out) another building hides from a camera at this yaw
            int Hidden(Vector2 at, Vector2 n, float yaw)
            {
                var f = Fwd(yaw); var along = new Vector2(-n.y, n.x); int hid = 0;
                foreach (var u in new[] { -2f, 0f, 2f }) foreach (var h in new[] { 2f, 5f, 8f })
                {
                    var q0 = at + along * u + n * 0.3f;
                    for (float t = 0.5f; t < 80f; t += 0.5f)
                    {
                        var q = q0 - f * t; float z = h + t * tanP; bool hit = false;
                        foreach (var ms in masses) if (z >= ms.lo && z <= ms.hi && Inside(q, ms.ring)) { hit = true; break; }
                        if (hit) { hid++; break; }
                    }
                }
                return hid;
            }
            // squarely sunlit (to 0.02), seen whole from a quarter yaw that looks at the face, one wall per building, then nearest the office front
            var chosen = new List<(string id, string kit, Vector2 at, Vector2 n, float dot, float dist, float yaw, int hidden)>();
            var usedBuildings = new HashSet<string>();
            foreach (var p in picks.Where(x => x.dot > 0.9f).OrderByDescending(x => Mathf.Round(x.dot * 50f)).ThenBy(x => x.dist))
            {
                var bld = p.id[..p.id.LastIndexOf('/')]; if (usedBuildings.Contains(bld)) continue;
                var yaws = Baseline072.Yaws.Where(y => Vector2.Dot(Fwd(y), p.n) < -0.5f).OrderBy(y => Vector2.Dot(Fwd(y), p.n)).ToList();
                foreach (var y in yaws)
                {
                    int hid = Hidden(p.at, p.n, y); if (hid > 0) continue;
                    chosen.Add((p.id, p.kit, p.at, p.n, p.dot, p.dist, y, hid)); usedBuildings.Add(bld); break;
                }
                if (chosen.Count == 3) break;
            }
            if (spotsFile != null)
            {
                // the same three walls as the first F1 run (facade, yaw), so the frames compare; the facade's place comes from this export
                chosen.Clear(); var rec = File.ReadAllText(spotsFile);
                foreach (Match m in Regex.Matches(rec, "\\{ \"facade\": \"([^\"]+)\", \"kit\": \"([^\"]+)\", \"at\": \\[([-\\d.]+), ([-\\d.]+)\\], \"normal\": \\[([-\\d.]+), ([-\\d.]+)\\][^}]*?\"yaw\": (\\d+)"))
                {
                    var id = m.Groups[1].Value; var now = picks.FirstOrDefault(x => x.id == id);
                    var at = now.id != null ? now.at : new Vector2(P(m.Groups[3].Value), P(m.Groups[4].Value));
                    var n = now.id != null ? now.n : new Vector2(P(m.Groups[5].Value), P(m.Groups[6].Value)).normalized;
                    if (now.id == null) Debug.LogWarning($"072 f1: {id} is not a street-level SideBay with an L2 proxy in this export; framing it at the recorded place");
                    chosen.Add((id, now.id != null ? now.kit : m.Groups[2].Value, at, n, Vector2.Dot(n, toSun), (at - Baseline072.OfficeFront).magnitude, P(m.Groups[7].Value), 0));
                }
                Debug.Log($"072 f1: {chosen.Count} walls from {spotsFile}");
            }
            if (chosen.Count == 0) { Debug.LogWarning("072 f1: every sun-facing SideBay wall is hidden from the quarter yaws"); return; }
            bool fadeWas = c.fade && c.fade.fade;
            var sb = new StringBuilder("{\n  \"schema\": \"suborned.072.f1/1\",\n");
            sb.Append($"  \"when\": \"{DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}\",\n  \"export\": \"{Esc(stamp ? $"{stamp.exportPath} sha256 {stamp.exportSha256}" : path)}\",\n");
            sb.Append($"  \"sun\": {{ \"toSun\": [{F(toSun3.x)}, {F(toSun3.y)}, {F(toSun3.z)}], \"euler\": [{F(sun.transform.eulerAngles.x)}, {F(sun.transform.eulerAngles.y)}] }},\n");
            sb.Append($"  \"note\": \"play zoom {Baseline072.PlayZoom}, pitch {Baseline072.PlayPitch}, 1920x1080 offscreen; fade off for these frames; shell inset {ProcFacade.ShellInset} m in both; proxy = module L2 proxies (as built), l0 = modules cast from their own LODs\",\n  \"shots\": [\n");
            try
            {
                if (c.fade) c.fade.fade = false;
                for (int i = 0; i < chosen.Count; i++)
                {
                    var p = chosen[i];
                    // the wall's point 5 m up at the frame's centre: the ground focus lies that far behind it along the view
                    float yaw = p.yaw;
                    var focus = p.at + Fwd(yaw) * (5f / tanP);
                    var names = new List<string>();
                    foreach (var (tag, fromLods) in new[] { ("proxy", false), ("l0", true) })
                    {
                        LookLab.SetModuleShadows(fromLods);
                        Baseline072.Pose(c.cam, c.shadows, c.fade, focus, yaw, c.standOff);
                        session.Grab(c.cam, 64, 64); // settle the cascades on the new pose
                        var tex = session.Grab(c.cam, W, H);
                        var name = $"f1-{i + 1}-{tag}.png"; File.WriteAllBytes(Path.Combine(dir, name), tex.EncodeToPNG()); names.Add(name);
                        var crop = new Texture2D(W / 2, H / 2, TextureFormat.RGBA32, false);
                        crop.SetPixels(tex.GetPixels(W / 4, H / 4, W / 2, H / 2)); crop.Apply();
                        var big = Upscale2(crop); var cname = $"f1-{i + 1}-{tag}-x2.png"; File.WriteAllBytes(Path.Combine(dir, cname), big.EncodeToPNG()); names.Add(cname);
                        UnityEngine.Object.DestroyImmediate(crop); UnityEngine.Object.DestroyImmediate(big);
                    }
                    sb.Append($"    {{ \"facade\": \"{Esc(p.id)}\", \"kit\": \"{p.kit}\", \"at\": [{F(p.at.x)}, {F(p.at.y)}], \"normal\": [{F(p.n.x)}, {F(p.n.y)}], \"sunDot\": {F(p.dot)}, " +
                              $"\"fromOfficeFront\": {F(p.dist, 1)}, \"yaw\": {yaw:0}, \"focus\": [{F(focus.x)}, {F(focus.y)}], \"files\": [{string.Join(", ", names.Select(x => $"\"{x}\""))}] }}{(i + 1 < chosen.Count ? "," : "")}\n");
                    Debug.Log($"072 f1: {p.id} ({p.kit}) sun dot {p.dot:F2}, yaw {yaw:0}, {p.dist:F0} m from the office front");
                }
            }
            finally { LookLab.SetModuleShadows(null); if (c.fade) c.fade.fade = fadeWas; }
            sb.Append("  ]\n}\n");
            File.WriteAllText(Path.Combine(dir, "f1.json"), sb.ToString());
            Debug.Log($"072 f1: {chosen.Count} walls, proxy and L0 → {dir}");
        }

        static bool Inside(Vector2 p, List<Vector2> ring)
        {
            bool c = false;
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
                if ((ring[i].y > p.y) != (ring[j].y > p.y) && p.x < (ring[j].x - ring[i].x) * (p.y - ring[i].y) / (ring[j].y - ring[i].y) + ring[i].x) c = !c;
            return c;
        }

        /// <summary>072-F switch check (only with -only sd): the eight views with the surface detail on and off (LookLab, the global float the perf
        /// harness's surfaceDetail sets), the mean and largest per-pixel difference logged; shots to out/scratch (not evidence that ships).</summary>
        static void SurfaceDetailCheck(Ctx c, RenderSession session, string dir)
        {
            Directory.CreateDirectory(dir);
            try
            {
                foreach (var v in c.views)
                {
                    Baseline072.Pose(c.cam, c.shadows, c.fade, v.Focus, v.Yaw, c.standOff, ZoomOf(v));
                    LookLab.SetSurfaceDetail(true); session.Grab(c.cam, 64, 64); var on = session.Grab(c.cam, W, H).GetPixels32();
                    LookLab.SetSurfaceDetail(false); session.Grab(c.cam, 64, 64); var offTex = session.Grab(c.cam, W, H); var off = offTex.GetPixels32();
                    double sum = 0; int max = 0, changed = 0;
                    for (int i = 0; i < on.Length; i++)
                    {
                        int d = Mathf.Abs(on[i].r - off[i].r) + Mathf.Abs(on[i].g - off[i].g) + Mathf.Abs(on[i].b - off[i].b);
                        sum += d; if (d > max) max = d; if (d > 6) changed++;
                    }
                    File.WriteAllBytes(Path.Combine(dir, $"{v.Id}-y{v.Yaw:000}-off.png"), offTex.EncodeToPNG());
                    Debug.Log($"072 sd: {v.Id} y{v.Yaw:000}: detail on vs off, mean |dRGB| {sum / on.Length:F2}, max {max}, pixels changed (>6) {100.0 * changed / on.Length:F1} %");
                }
            }
            finally { LookLab.SetSurfaceDetail(true); }
        }

        /// <summary>Nearest-neighbour 2x (a crop read at 2x without smoothing).</summary>
        static Texture2D Upscale2(Texture2D src)
        {
            int w = src.width, h = src.height; var a = src.GetPixels32(); var o = new Color32[w * h * 4];
            for (int y = 0; y < h * 2; y++) for (int x = 0; x < w * 2; x++) o[y * w * 2 + x] = a[(y >> 1) * w + (x >> 1)];
            var t = new Texture2D(w * 2, h * 2, TextureFormat.RGBA32, false); t.SetPixels32(o); t.Apply(); return t;
        }

        // ---------------------------------------------------------------- 2. counts

        sealed class Cascade { public int casters; public long tris; public int variants; public float radius; }

        sealed class Count
        {
            public Baseline072.View v;
            public int mainRenderers, shadowsOnlyInFrustum, materials, fwdVariants, dnVariants, shadowVariants, uniqueCasters;
            public long mainTris, opaqueTris, shadowTris;
            public List<Cascade> cascades = new();
            public double median, p90;
            public string cascadeNote;
        }

        static List<Count> Counts(Ctx c, string mode)
        {
            var list = new List<Count>(); var meshTris = new Dictionary<Mesh, long[]>();
            var px = new Texture2D(1, 1, TextureFormat.RGBAHalf, false);
            using var capture = new Capture();
            var rt = capture.Target(W, H);
            var ssao = SsaoOn();
            try
            {
                foreach (var src in c.views)
                {
                    var v = new Baseline072.View { Id = src.Id, Label = src.Label, Focus = src.Focus, Yaw = src.Yaw };
                    Baseline072.Pose(c.cam, c.shadows, c.fade, v.Focus, v.Yaw, c.standOff, ZoomOf(v));
                    var visible = new List<Renderer>(); var lodOut = new HashSet<Renderer>();
                    Baseline072.Census(c.cam, v, meshTris, visible, lodOut);
                    var n = new Count { v = v };
                    var mats = new HashSet<Material>(); var fwd = new HashSet<string>(); var dn = new HashSet<string>();
                    foreach (var r in visible)
                    {
                        if (r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) { n.shadowsOnlyInFrustum++; continue; }
                        n.mainRenderers++;
                        var t = Tris(r, meshTris); var sm = r.sharedMaterials;
                        for (int i = 0; i < t.Length; i++)
                        {
                            n.mainTris += t[i];
                            var m = i < sm.Length ? sm[i] : (sm.Length > 0 ? sm[^1] : null); if (!m || !m.shader) continue;
                            mats.Add(m); fwd.Add(Variant(m, null));
                            if (m.renderQueue < 2500) { n.opaqueTris += t[i]; if (ssao && m.FindPass("DepthNormals") >= 0) dn.Add(Variant(m, new[] { "_ALPHATEST_ON", "_NORMALMAP" })); }
                        }
                    }
                    n.materials = mats.Count; n.fwdVariants = fwd.Count; n.dnVariants = dn.Count;
                    ShadowCensus(c, n, lodOut, meshTris, rt, px);
                    // batchmode frame ms (offscreen, the GPU waited on)
                    c.cam.targetTexture = rt; c.cam.ResetAspect();
                    for (int i = 0; i < WarmFrames; i++) Sync(c.cam, rt, px);
                    var times = new List<double>();
                    for (int i = 0; i < TimedFrames; i++) { var sw = System.Diagnostics.Stopwatch.StartNew(); Sync(c.cam, rt, px); times.Add(sw.Elapsed.TotalMilliseconds); }
                    c.cam.targetTexture = null; c.cam.ResetAspect();
                    times.Sort(); n.median = times[times.Count / 2]; n.p90 = times[(int)(times.Count * 0.9)];
                    list.Add(n);
                    Debug.Log($"072 after [{mode}]: {v.Id} y{v.Yaw:000}: main {n.mainRenderers} renderers {n.mainTris:N0} tris, census {v.Visible}/{v.VisibleTris:N0}, materials {n.materials}, variants fwd {n.fwdVariants} dn {n.dnVariants} shadow {n.shadowVariants}, casters {n.uniqueCasters} ({n.shadowTris:N0} tris), modules {v.ModModules} ({v.ModColumns}), {n.median:F1} ms");
                }
            }
            finally { c.cam.targetTexture = null; c.cam.ResetAspect(); UnityEngine.Object.DestroyImmediate(px); }
            return list;
        }

        static bool SsaoOn()
        {
            var urp = UniversalRenderPipeline.asset; if (!urp) return false;
            foreach (var d in urp.rendererDataList) if (d) foreach (var f in d.rendererFeatures) if (f && f.isActive && f.GetType().Name.Contains("ScreenSpaceAmbientOcclusion")) return true;
            return false;
        }

        static long[] Tris(Renderer r, Dictionary<Mesh, long[]> meshTris)
        {
            var mesh = r is SkinnedMeshRenderer sk ? sk.sharedMesh : r.TryGetComponent<MeshFilter>(out var mf) ? mf.sharedMesh : null;
            if (!mesh) return Array.Empty<long>();
            if (!meshTris.TryGetValue(mesh, out var t))
            {
                t = new long[mesh.subMeshCount];
                for (int i = 0; i < mesh.subMeshCount; i++) t[i] = mesh.GetTopology(i) == MeshTopology.Triangles ? (long)mesh.GetIndexCount(i) / 3 : 0;
                meshTris[mesh] = t;
            }
            return t;
        }

        /// <summary>A shader variant key: the shader and the material's keywords (only `keep` when given: the ones that pass declares).</summary>
        static string Variant(Material m, string[] keep)
        {
            var kw = m.shaderKeywords.Where(k => keep == null || keep.Contains(k)).OrderBy(k => k, StringComparer.Ordinal);
            return m.shader.name + "|" + string.Join(",", kw);
        }

        /// <summary>The cascades URP draws for this camera now, reproduced, and the casters each takes.</summary>
        static void ShadowCensus(Ctx c, Count n, HashSet<Renderer> lodOut, Dictionary<Mesh, long[]> meshTris, RenderTexture rt, Texture2D px)
        {
            var splits = new List<(Plane[] planes, Vector4 sphere)>(); Vector3 lightDir = Vector3.down; int lightMask = ~0; string note = null;
            void Hook(ScriptableRenderContext ctx, Camera cc)
            {
                if (cc != c.cam || splits.Count > 0 || note != null) return;
                try
                {
                    var urp = UniversalRenderPipeline.asset;
                    if (!cc.TryGetCullingParameters(out var p)) { note = "no culling parameters"; return; }
                    p.shadowDistance = Mathf.Min(urp.shadowDistance, cc.farClipPlane);
                    var cull = ctx.Cull(ref p);
                    int li = -1; var sun = RenderSettings.sun;
                    for (int i = 0; i < cull.visibleLights.Length; i++)
                    {
                        var vl = cull.visibleLights[i];
                        if (vl.lightType != LightType.Directional || !vl.light || vl.light.shadows == LightShadows.None) continue;
                        if (li < 0 || vl.light == sun) li = i;
                    }
                    if (li < 0) { note = "no shadowed directional light visible"; return; }
                    var light = cull.visibleLights[li].light; lightDir = light.transform.forward; lightMask = light.cullingMask;
                    int count = urp.shadowCascadeCount;
                    var ratios = count == 4 ? urp.cascade4Split : count == 3 ? new Vector3(urp.cascade3Split.x, urp.cascade3Split.y, 0) : count == 2 ? new Vector3(urp.cascade2Split, 0, 0) : Vector3.zero;
                    int tile = count == 1 ? urp.mainLightShadowmapResolution : urp.mainLightShadowmapResolution / 2;
                    for (int k = 0; k < count; k++)
                    {
                        cull.ComputeDirectionalShadowMatricesAndCullingPrimitives(li, k, count, ratios, tile, light.shadowNearPlane, out _, out _, out var split);
                        var planes = new Plane[split.cullingPlaneCount];
                        for (int q = 0; q < planes.Length; q++) planes[q] = split.GetCullingPlane(q);
                        splits.Add((planes, split.cullingSphere));
                    }
                    note = $"TacticalShadows {(c.shadows && c.shadows.isActiveAndEnabled ? "on" : "off")}, focusDepth {(c.shadows ? c.shadows.focusDepth : 0):F0}; {count} cascades, shadow distance {urp.shadowDistance:F0} m, splits {ratios.x:F3}/{ratios.y:F3}/{ratios.z:F3}, tile {tile}";
                }
                catch (Exception e) { note = "cull failed: " + e.Message; }
            }
            RenderPipelineManager.beginCameraRendering += Hook;
            try { c.cam.targetTexture = rt; c.cam.ResetAspect(); Sync(c.cam, rt, px); }
            finally { RenderPipelineManager.beginCameraRendering -= Hook; c.cam.targetTexture = null; c.cam.ResetAspect(); }
            n.cascadeNote = note ?? "the hook never ran";
            if (splits.Count == 0) return;
            var unique = new HashSet<Renderer>(); var allVariants = 0;
            var casters = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)
                .Where(r => (r is MeshRenderer || r is SkinnedMeshRenderer) && r.enabled && !r.forceRenderingOff && r.shadowCastingMode != ShadowCastingMode.Off
                            && (lightMask & (1 << r.gameObject.layer)) != 0 && !lodOut.Contains(r)).ToList();
            foreach (var (planes, sphere) in splits)
            {
                var cs = new Cascade { radius = sphere.w }; var vars = new HashSet<string>();
                var centre = (Vector3)sphere;
                foreach (var r in casters)
                {
                    var b = r.bounds;
                    if (planes.Length > 0 && !GeometryUtility.TestPlanesAABB(planes, b)) continue;
                    // the sphere extruded toward the light: across the light within radius + the bounds' reach, and not wholly past the sphere
                    var d = b.center - centre; float along = Vector3.Dot(d, lightDir); var across = d - along * lightDir; float reach = b.extents.magnitude;
                    if (across.magnitude > sphere.w + reach || along - reach > sphere.w) continue;
                    cs.casters++; unique.Add(r);
                    var t = Tris(r, meshTris); foreach (var x in t) cs.tris += x;
                    foreach (var m in r.sharedMaterials) if (m && m.shader) vars.Add(Variant(m, new[] { "_ALPHATEST_ON" }));
                }
                cs.variants = vars.Count; allVariants += vars.Count;
                n.cascades.Add(cs); n.shadowTris += cs.tris;
            }
            n.uniqueCasters = unique.Count; n.shadowVariants = allVariants;
        }

        static void Sync(Camera cam, RenderTexture rt, Texture2D px)
        {
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            px.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); // waits for the GPU
            RenderTexture.active = prev;
        }

        static string CountsJson(Ctx c, string export, List<Count> every, List<Count> today)
        {
            var sb = new StringBuilder("{\n");
            void KV(string k, string v) => sb.Append($"  \"{k}\": \"{Esc(v)}\",\n");
            KV("schema", "suborned.072-offscreen/1");
            KV("when", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            KV("machine", $"Studio, Unity batchmode, offscreen: {SystemInfo.deviceModel}, {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType})");
            KV("unity", Application.unityVersion);
            KV("export", export);
            KV("camera", $"cameras/play posed as HandoverHost.PlaceCamera: orthographic {Baseline072.PlayZoom}, pitch {Baseline072.PlayPitch}, standOff {F(c.standOff)} m; cell-open from {c.cellFrom}; Camera.Render into Capture's HDR target {W}x{H} (no Game view)");
            KV("drawer", "GPU Resident Drawer off (RenderSession), as the baseline counts");
            KV("notUnityStats", "No number here is UnityStats (that needs the visible Game view; queued for when the Studio is free). census* = Baseline072's census method (16:9 frustum + LOD pick; counts shadows-only renderers as visible, as the baseline did). main* = the same without shadows-only renderers. setPassEstimate = distinct shader variants: forward (every visible material, its keywords) + SSAO depth-normals prepass (opaque, _ALPHATEST_ON/_NORMALMAP) + shadow (per cascade, its casters, _ALPHATEST_ON); fullscreen passes (sky, SSAO, post, copies) not counted. shadow* = casters per cascade by URP's split for this camera reproduced (CullingResults culling planes and sphere, bounds test; LOD-picked meshes): an estimate. frameMs = batchmode offscreen Camera.Render, GPU waited on, indicative only.");
            sb.Append("  \"modes\": {\n");
            void Mode(string name, List<Count> list, bool last)
            {
                sb.Append($"    \"{name}\": [\n");
                for (int i = 0; i < list.Count; i++)
                {
                    var n = list[i]; var v = n.v;
                    sb.Append($"      {{\"view\": \"{v.Id}\", \"yaw\": {F(v.Yaw)}, \"focus\": [{F(v.Focus.x)}, {F(v.Focus.y)}],\n");
                    sb.Append($"       \"censusVisibleRenderers\": {v.Visible}, \"censusTriangles\": {v.VisibleTris}, \"censusLodCulled\": {v.LodCulled},\n");
                    sb.Append($"       \"mainRenderers\": {n.mainRenderers}, \"mainTriangles\": {n.mainTris}, \"opaqueTriangles\": {n.opaqueTris}, \"shadowsOnlyInFrustum\": {n.shadowsOnlyInFrustum},\n");
                    sb.Append($"       \"materials\": {n.materials}, \"setPassEstimate\": {{\"forward\": {n.fwdVariants}, \"depthNormals\": {n.dnVariants}, \"shadow\": {n.shadowVariants}, \"total\": {n.fwdVariants + n.dnVariants + n.shadowVariants}}},\n");
                    sb.Append($"       \"shadow\": {{\"uniqueCasters\": {n.uniqueCasters}, \"triangles\": {n.shadowTris}, \"note\": \"{Esc(n.cascadeNote)}\", \"cascades\": [{string.Join(", ", n.cascades.Select(k => $"{{\"casters\": {k.casters}, \"triangles\": {k.tris}, \"variants\": {k.variants}, \"sphereRadius\": {F(k.radius)}}}"))}]}},\n");
                    sb.Append($"       \"trianglesAllPassesEstimate\": {n.mainTris + (n.dnVariants > 0 ? n.opaqueTris : 0) + n.shadowTris},\n");
                    sb.Append($"       \"modules\": {{\"modulesInFrustum\": {v.ModModules}, \"columnsInFrustum\": {v.ModColumns}, \"l0Triangles\": {v.ModTris}, \"proxiesOnInScene\": {v.ModProxies}}},\n");
                    sb.Append($"       \"frameMs\": {{\"median\": {F(Math.Round(n.median, 2))}, \"p90\": {F(Math.Round(n.p90, 2))}}},\n");
                    sb.Append($"       \"byLayer\": {{{string.Join(", ", v.ByLayer.OrderByDescending(kv => kv.Value.tris).Take(14).Select(kv => $"\"{Esc(kv.Key)}\": {{\"renderers\": {kv.Value.n}, \"triangles\": {kv.Value.tris}}}"))}}}}}{(i + 1 < list.Count ? "," : "")}\n");
                }
                sb.Append($"    ]{(last ? "" : ",")}\n");
            }
            Mode("every", every, today == null);
            if (today != null) Mode("today", today, true);
            sb.Append("  }\n}\n");
            return sb.ToString();
        }

        // ---------------------------------------------------------------- 3. 072-F: screen area per material

        sealed class Key { public int id; public Material mat; public string layer; public long[] all = new long[8], opaque = new long[8]; public int renderers; }

        static void Area(Ctx c, RenderSession session, string file, string export)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var idShader = Shader.Find("Hidden/Suborned/MaterialId072") ?? throw new InvalidOperationException("072: MaterialId072 shader not found");
            var urp = UniversalRenderPipeline.asset;
            var keys = new Dictionary<(Material, string), Key>(); var byId = new List<Key> { null };
            var twins = new Dictionary<Key, Material>();
            var swapped = new List<(Renderer r, Material[] was)>(); var disabled = new List<Renderer>();
            var features = new List<ScriptableRendererFeature>();
            var camData = c.cam.GetComponent<UniversalAdditionalCameraData>();
            var was = (post: camData ? camData.renderPostProcessing : false, aa: camData ? camData.antialiasing : AntialiasingMode.None, clear: c.cam.clearFlags, bg: c.cam.backgroundColor, fog: RenderSettings.fog);
            var stackOff = new List<Camera>();
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { antiAliasing = 1, name = "072-material-id" };
            var tex = new Texture2D(W, H, TextureFormat.RGBAFloat, false, true);
            int[] unresolved = new int[8], sky = new int[8], unresolvedOpaque = new int[8], skyOpaque = new int[8];
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            try
            {
                foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
                {
                    if (!r.enabled) continue;
                    if (r is not MeshRenderer && r is not SkinnedMeshRenderer) { r.enabled = false; disabled.Add(r); continue; }
                    var sm = r.sharedMaterials; var layer = LayerKey(r.transform); var twin = new Material[sm.Length];
                    for (int i = 0; i < sm.Length; i++)
                    {
                        var m = sm[i]; if (!m) { twin[i] = null; continue; }
                        if (!keys.TryGetValue((m, layer), out var k))
                        {
                            k = new Key { id = byId.Count, mat = m, layer = layer }; keys[(m, layer)] = k; byId.Add(k);
                            var t = new Material(idShader) { name = "id-" + m.name, renderQueue = 2000, hideFlags = HideFlags.HideAndDontSave };
                            int id = k.id; var col = new Color((id & 255) / 255f, ((id >> 8) & 255) / 255f, ((id >> 16) & 255) / 255f, 1f);
                            t.SetColor("_IdColor", linear ? col.gamma : col); // Unity takes the colour as sRGB and uploads it linear: the shader sees `col`
                            if (m.IsKeywordEnabled("_ALPHATEST_ON") && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap"))
                            { t.EnableKeyword("_ALPHATEST_ON"); t.SetTexture("_BaseMap", m.GetTexture("_BaseMap")); t.SetTextureScale("_BaseMap", m.GetTextureScale("_BaseMap")); t.SetTextureOffset("_BaseMap", m.GetTextureOffset("_BaseMap")); t.SetFloat("_Cutoff", m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f); }
                            t.SetFloat("_Cull", m.HasProperty("_Cull") ? m.GetFloat("_Cull") : 2f);
                            t.SetFloat("_CanCut", m.shader.keywordSpace.FindKeyword(FadeReach.Keyword).isValid ? 1f : 0f);
                            twins[k] = t;
                        }
                        k.renderers++;
                        twin[i] = twins[k];
                    }
                    swapped.Add((r, sm)); r.sharedMaterials = twin;
                }
                if (urp) foreach (var d in urp.rendererDataList) if (d) foreach (var f in d.rendererFeatures) if (f && f.isActive) { f.SetActive(false); features.Add(f); }
                if (camData)
                {
                    camData.renderPostProcessing = false; camData.antialiasing = AntialiasingMode.None;
                    foreach (var s in camData.cameraStack) if (s && s.enabled) { s.enabled = false; stackOff.Add(s); }
                }
                c.cam.clearFlags = CameraClearFlags.SolidColor; c.cam.backgroundColor = Color.clear; RenderSettings.fog = false;
                Debug.Log($"072 area: {byId.Count - 1} (material, layer) ids over {swapped.Count} renderers ({keys.Keys.Select(k => k.Item1).Distinct().Count()} materials); {disabled.Count} other renderers off; {features.Count} renderer features off");

                var opaqueSkip = byId.Skip(1).Where(k => k.mat.renderQueue >= 2500).Select(k => twins[k]).ToList();
                for (int vi = 0; vi < c.views.Count; vi++)
                {
                    var v = c.views[vi];
                    Baseline072.Pose(c.cam, c.shadows, c.fade, v.Focus, v.Yaw, c.standOff, ZoomOf(v));
                    for (int pass = 0; pass < 2; pass++)
                    {
                        foreach (var t in opaqueSkip) t.SetFloat("_Skip", pass);
                        var prevT = c.cam.targetTexture; var prevA = RenderTexture.active;
                        try
                        {
                            c.cam.targetTexture = rt; c.cam.ResetAspect(); c.cam.Render();
                            RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(false);
                        }
                        finally { c.cam.targetTexture = prevT; c.cam.ResetAspect(); RenderTexture.active = prevA; }
                        var px = tex.GetPixels();
                        foreach (var p in px)
                        {
                            float fr = p.r * 255f, fg = p.g * 255f, fb = p.b * 255f;
                            int r8 = Mathf.RoundToInt(fr), g8 = Mathf.RoundToInt(fg), b8 = Mathf.RoundToInt(fb);
                            if (Mathf.Abs(fr - r8) > 0.05f || Mathf.Abs(fg - g8) > 0.05f || Mathf.Abs(fb - b8) > 0.05f) { if (pass == 0) unresolved[vi]++; else unresolvedOpaque[vi]++; continue; }
                            int id = r8 | g8 << 8 | b8 << 16;
                            if (id == 0) { if (pass == 0) sky[vi]++; else skyOpaque[vi]++; continue; }
                            if (id >= byId.Count) { if (pass == 0) unresolved[vi]++; else unresolvedOpaque[vi]++; continue; }
                            if (pass == 0) byId[id].all[vi]++; else byId[id].opaque[vi]++;
                        }
                    }
                    Debug.Log($"072 area: {v.Id} y{v.Yaw:000}: sky {sky[vi] * 100.0 / (W * H):F1} %, unresolved {unresolved[vi] * 100.0 / (W * H):F2} %");
                }
            }
            finally
            {
                foreach (var (r, m) in swapped) if (r) r.sharedMaterials = m;
                foreach (var r in disabled) if (r) r.enabled = true;
                foreach (var f in features) if (f) f.SetActive(true);
                foreach (var s in stackOff) if (s) s.enabled = true;
                if (camData) { camData.renderPostProcessing = was.post; camData.antialiasing = was.aa; }
                c.cam.clearFlags = was.clear; c.cam.backgroundColor = was.bg; RenderSettings.fog = was.fog;
                foreach (var t in twins.Values) UnityEngine.Object.DestroyImmediate(t);
                rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex);
            }
            File.WriteAllText(file, AreaJson(c, byId, unresolved, sky, unresolvedOpaque, skyOpaque, export));
            Debug.Log($"072 area: → {file}");
        }

        static string LayerKey(Transform t)
        {
            var chain = new List<string>();
            for (var x = t; x; x = x.parent) chain.Add(x.name);
            chain.Reverse();
            if (chain.Count >= 2) return chain[0] + "/" + Regex.Replace(Regex.Replace(chain[1], @"\s*\(\d+\)$", ""), @"[-_]?\d+(_\d+)*$", "");
            return chain[0];
        }

        /// <summary>The maps a material binds and what that means for 072-F: textured, swatch-atlas (FacadeLit's 16 x 16 point-sampled cells: flat
        /// colour per cell), or flat (no colour map).</summary>
        static (string kind, string maps, string baseMap) Maps(Material m)
        {
            var bound = new List<string>(); Texture baseTex = null;
            foreach (var p in m.GetTexturePropertyNames())
            {
                Texture t; try { t = m.GetTexture(p); } catch { continue; }
                if (!t) continue;
                bound.Add($"{p}={t.name}({t.width}x{t.height}{(t.filterMode == FilterMode.Point ? ",point" : "")})");
                if (p == "_BaseMap" || (p == "_MainTex" && !baseTex)) baseTex = t;
            }
            string kind;
            if (baseTex && (baseTex.name == "facade-atlas" || m.shader.name == "Suborned/FacadeLit")) kind = "swatch-atlas";
            else if (!baseTex || baseTex.width * baseTex.height <= 64) kind = "flat";
            else kind = "textured";
            if (m.shader.name == "Suborned/FacadeLit" && !baseTex) kind = "swatch-atlas?";
            // 072-F: a flat colour that now carries a colourless detail (FacadeLit's surface-detail array, or a detail pair under _DETAIL_MULX2)
            if (kind.StartsWith("swatch-atlas") && m.HasProperty("_SurfaceDetail") && m.GetTexture("_SurfaceDetail")) kind = "swatch-atlas+detail";
            else if (kind == "flat" && m.IsKeywordEnabled("_DETAIL_MULX2") && m.HasProperty("_DetailAlbedoMap") && m.GetTexture("_DetailAlbedoMap")) kind = "flat+detail";
            return (kind, string.Join("; ", bound), baseTex ? baseTex.name : "");
        }

        static string AreaJson(Ctx c, List<Key> byId, int[] unresolved, int[] sky, int[] unresolvedOpaque, int[] skyOpaque, string export)
        {
            double total = (double)W * H; int nv = c.views.Count;
            string VName(int i) => $"{c.views[i].Id}-y{c.views[i].Yaw:000}";
            var byMat = byId.Skip(1).GroupBy(k => k.mat).Select(g => new
            {
                mat = g.Key, keys = g.ToList(),
                all = Enumerable.Range(0, nv).Select(i => g.Sum(k => k.all[i]) / total).ToArray(),
                opaque = Enumerable.Range(0, nv).Select(i => g.Sum(k => k.opaque[i]) / total).ToArray(),
            }).Where(x => x.all.Sum() > 0 || x.opaque.Sum() > 0).OrderByDescending(x => x.all.Average()).ToList();
            var sb = new StringBuilder("{\n");
            void KV(string k, string v) => sb.Append($"  \"{k}\": \"{Esc(v)}\",\n");
            KV("schema", "suborned.072-material-area/1");
            KV("when", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            KV("export", export);
            KV("method", $"Batchmode offscreen material-id pass (Hidden/Suborned/MaterialId072): every (material, layer) of the scene drawn in its own 24-bit colour into a {W}x{H} float target from the play camera at each play view (ortho {Baseline072.PlayZoom}, pitch {Baseline072.PlayPitch}, Baseline072's poses); post, SSAO and fog off, non-mesh renderers off, the snapshot crowd hidden; the architecture fade's dithered cut kept on materials whose shader cuts, alpha-test maps kept. `share` = pixels / frame pixels. `all` draws every material opaque (glass and other transparent surfaces cover what is behind them); `opaqueOnly` drops transparent materials (renderQueue >= 2500), so what stands behind glass counts instead. Not UnityStats; GPU Resident Drawer off.");
            KV("maps", "kind: textured = a colour map bigger than 8x8 bound (_BaseMap or _MainTex); swatch-atlas = Suborned/FacadeLit's facade-atlas (Atlas.cs: 16x16 point-sampled cells, one flat colour per cell: flat colour for 072-F); flat = no colour map (or a map of 8x8 or less). maps lists every bound texture property.");
            sb.Append("  \"views\": [");
            sb.Append(string.Join(", ", Enumerable.Range(0, nv).Select(i => $"{{\"view\": \"{VName(i)}\", \"focus\": [{F(c.views[i].Focus.x)}, {F(c.views[i].Focus.y)}], \"sky\": {F(sky[i] / total, 4)}, \"unresolved\": {F(unresolved[i] / total, 5)}, \"skyOpaqueOnly\": {F(skyOpaque[i] / total, 4)}, \"unresolvedOpaqueOnly\": {F(unresolvedOpaque[i] / total, 5)}}}")));
            sb.Append("],\n");
            // 072-F: mean share by map kind (flat and swatch-atlas = flat colour left; +detail = a flat colour now textured by a colourless detail)
            var kinds = byMat.GroupBy(x => Maps(x.mat).kind).Select(g => (kind: g.Key, n: g.Count(), share: g.Sum(x => x.all.Average()))).OrderByDescending(k => k.share).ToList();
            sb.Append("  \"byKind\": {" + string.Join(", ", kinds.Select(k => $"\"{k.kind}\": {{\"materials\": {k.n}, \"meanShare\": {F(k.share, 5)}}}")) + "},\n");
            // by shader
            var byShader = byMat.GroupBy(x => x.mat.shader ? x.mat.shader.name : "?").Select(g => new
            {
                shader = g.Key, mats = g.Count(),
                all = Enumerable.Range(0, nv).Select(i => g.Sum(x => x.all[i])).ToArray(),
                flat = g.Where(x => Maps(x.mat).kind != "textured").Sum(x => x.all.Average()),
            }).OrderByDescending(x => x.all.Average()).ToList();
            sb.Append("  \"shaders\": [\n");
            for (int s = 0; s < byShader.Count; s++)
            {
                var x = byShader[s];
                sb.Append($"    {{\"shader\": \"{Esc(x.shader)}\", \"materials\": {x.mats}, \"meanShare\": {F(x.all.Average(), 5)}, \"meanShareUntextured\": {F(x.flat, 5)}, \"share\": {{{string.Join(", ", Enumerable.Range(0, nv).Select(i => $"\"{VName(i)}\": {F(x.all[i], 5)}"))}}}}}{(s + 1 < byShader.Count ? "," : "")}\n");
            }
            sb.Append("  ],\n  \"materials\": [\n");
            for (int mi = 0; mi < byMat.Count; mi++)
            {
                var x = byMat[mi]; var m = x.mat; var (kind, maps, baseName) = Maps(m);
                var path = AssetDatabase.GetAssetPath(m);
                var byYaw = Baseline072.Yaws.Select(y => (y, Enumerable.Range(0, nv).Where(i => Mathf.Approximately(c.views[i].Yaw, y)).Average(i => x.all[i]))).ToList();
                var layers = x.keys.Select(k => (k.layer, share: k.all.Sum() / total / nv, k.renderers)).Where(l => l.share > 0).OrderByDescending(l => l.share).ToList();
                sb.Append($"    {{\"material\": \"{Esc(m.name)}\", \"shader\": \"{Esc(m.shader ? m.shader.name : "?")}\", \"asset\": \"{Esc(string.IsNullOrEmpty(path) ? "(built in memory / scene)" : path)}\",\n");
                sb.Append($"     \"maps\": {{\"kind\": \"{kind}\", \"baseMap\": \"{Esc(baseName)}\", \"normalMap\": {(m.HasProperty("_BumpMap") && m.GetTexture("_BumpMap") ? "true" : "false")}, \"bound\": \"{Esc(maps)}\"}}, \"transparent\": {(m.renderQueue >= 2500 ? "true" : "false")}, \"renderQueue\": {m.renderQueue}, \"emission\": {(m.IsKeywordEnabled("_EMISSION") ? "true" : "false")},\n");
                sb.Append($"     \"meanShare\": {F(x.all.Average(), 5)}, \"meanShareOpaqueOnly\": {F(x.opaque.Average(), 5)}, \"byYaw\": {{{string.Join(", ", byYaw.Select(b => $"\"{b.y:0}\": {F(b.Item2, 5)}"))}}},\n");
                sb.Append($"     \"share\": {{{string.Join(", ", Enumerable.Range(0, nv).Select(i => $"\"{VName(i)}\": {F(x.all[i], 5)}"))}}},\n");
                sb.Append($"     \"where\": [{string.Join(", ", layers.Take(6).Select(l => $"{{\"layer\": \"{Esc(l.layer)}\", \"meanShare\": {F(l.share, 5)}, \"renderers\": {l.renderers}}}"))}]}}{(mi + 1 < byMat.Count ? "," : "")}\n");
            }
            sb.Append("  ]\n}\n");
            return sb.ToString();
        }

        // ---------------------------------------------------------------- 5. 072-F: texture memory

        /// <summary>Every texture bound by a material of an enabled renderer in the open scene: Profiler.GetRuntimeMemorySizeLong (the Editor's
        /// loaded size, which can hold a CPU copy for readable textures) and the GPU size from format, size, mip count and layers (what the
        /// player would hold), summed and listed by texture.</summary>
        static void TexMem(string file, string export, string tag)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var tex = new Dictionary<Texture, HashSet<string>>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if (!r.enabled) continue;
                foreach (var m in r.sharedMaterials)
                {
                    if (!m) continue;
                    foreach (var p in m.GetTexturePropertyNames())
                    {
                        Texture t; try { t = m.GetTexture(p); } catch { continue; }
                        if (!t) continue;
                        if (!tex.TryGetValue(t, out var by)) tex[t] = by = new HashSet<string>();
                        by.Add(m.name);
                    }
                }
            }
            static long Gpu(Texture t)
            {
                int layers = t is Texture2DArray ta ? ta.depth : t is Cubemap ? 6 : t is Texture3D t3 ? t3.depth : 1;
                long sum = 0; int w = t.width, h = t.height;
                for (int i = 0; i < Mathf.Max(1, t.mipmapCount); i++)
                {
                    sum += (long)UnityEngine.Experimental.Rendering.GraphicsFormatUtility.ComputeMipmapSize(Mathf.Max(1, w), Mathf.Max(1, h), t.graphicsFormat);
                    w >>= 1; h >>= 1;
                }
                return sum * layers;
            }
            var rows = tex.Select(kv => (t: kv.Key, path: AssetDatabase.GetAssetPath(kv.Key), runtime: UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(kv.Key), gpu: Gpu(kv.Key), by: kv.Value))
                .OrderByDescending(x => x.gpu).ToList();
            const double MiB = 1024.0 * 1024.0;
            var surf = rows.Where(x => x.t.name.StartsWith("surface_", StringComparison.Ordinal)).ToList();
            var sb = new StringBuilder("{\n");
            sb.Append("  \"schema\": \"suborned.072-texmem/1\",\n");
            sb.Append($"  \"tag\": \"{Esc(tag)}\",\n  \"when\": \"{DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}\",\n  \"export\": \"{Esc(export)}\",\n");
            sb.Append("  \"method\": \"Every texture bound by a material of an enabled renderer in the open HarbourDistrict scene (batchmode). runtimeBytes = Profiler.GetRuntimeMemorySizeLong in the Editor (can include a CPU copy of readable textures); gpuBytes = GraphicsFormatUtility.ComputeMipmapSize summed over the texture's mips x layers (what the GPU holds).\",\n");
            sb.Append($"  \"textures\": {rows.Count}, \"runtimeMiB\": {F(rows.Sum(x => x.runtime) / MiB, 2)}, \"gpuMiB\": {F(rows.Sum(x => x.gpu) / MiB, 2)},\n");
            sb.Append($"  \"surfaceSet\": {{\"textures\": {surf.Count}, \"runtimeMiB\": {F(surf.Sum(x => x.runtime) / MiB, 2)}, \"gpuMiB\": {F(surf.Sum(x => x.gpu) / MiB, 2)}}},\n");
            sb.Append("  \"rows\": [\n");
            for (int i = 0; i < rows.Count; i++)
            {
                var x = rows[i];
                sb.Append($"    {{\"texture\": \"{Esc(x.t.name)}\", \"asset\": \"{Esc(string.IsNullOrEmpty(x.path) ? "(scene / memory)" : x.path)}\", \"type\": \"{x.t.GetType().Name}\", \"size\": [{x.t.width}, {x.t.height}], \"format\": \"{x.t.graphicsFormat}\", \"mips\": {x.t.mipmapCount}, \"runtimeBytes\": {x.runtime}, \"gpuBytes\": {x.gpu}, \"materials\": {x.by.Count}}}{(i + 1 < rows.Count ? "," : "")}\n");
            }
            sb.Append("  ]\n}\n");
            File.WriteAllText(file, sb.ToString());
            Debug.Log($"072 texmem ({tag}): {rows.Count} textures, runtime {rows.Sum(x => x.runtime) / MiB:F2} MiB, GPU {rows.Sum(x => x.gpu) / MiB:F2} MiB (surface set {surf.Sum(x => x.gpu) / MiB:F2} MiB) → {file}");
        }

        // ---------------------------------------------------------------- 4. K-6 at lamplight-south-w/13

        static void K6(Ctx c, RenderSession session, string file, string shotsDir, WorldStamp stamp)
        {
            // the body ring (plan x, y = world x, z) from the export the scene was built from
            var path = stamp ? Path.Combine(HarbourWorldBuilder.RepoRoot, stamp.exportPath) : RenderSession.ExportPath;
            var text = File.ReadAllText(path);
            var mm = Regex.Match(text, "\"id\":\\s*\"bld/lamplight-south-w/13/body\"[^{}]*?\"ring\":\\s*\\[((?:\\s*\\[[-\\d.]+,\\s*[-\\d.]+\\],?)+)\\s*\\]");
            if (!mm.Success) { Debug.LogWarning("072 k6: bld/lamplight-south-w/13/body not found in the export"); return; }
            var ring = Regex.Matches(mm.Groups[1].Value, "\\[([-\\d.]+),\\s*([-\\d.]+)\\]").Select(x => new Vector2(P(x.Groups[1].Value), P(x.Groups[2].Value))).ToList();
            // the acute corner: the vertex with the smallest interior angle
            int vi = 0; float best = 999;
            for (int i = 0; i < ring.Count; i++)
            {
                var p = ring[(i + ring.Count - 1) % ring.Count]; var q = ring[i]; var r = ring[(i + 1) % ring.Count];
                float ang = Vector2.Angle(p - q, r - q); if (ang < best) { best = ang; vi = i; }
            }
            var V = ring[vi]; var A = ring[(vi + ring.Count - 1) % ring.Count]; var B = ring[(vi + 1) % ring.Count];
            float area = 0; for (int i = 0; i < ring.Count; i++) { var p = ring[i]; var q = ring[(i + 1) % ring.Count]; area += p.x * q.y - q.x * p.y; }
            Vector2 Out(Vector2 a, Vector2 b) { var d = (b - a).normalized; var n = new Vector2(d.y, -d.x); return area > 0 ? n : -n; } // outward normal for the ring's winding
            var edges = new[] { ("edge into the corner", A, V, Out(A, V)), ("edge out of the corner", V, B, Out(V, B)) };
            var ef = UnityEngine.Object.FindAnyObjectByType<EveryFace>(FindObjectsInactive.Include);
            var rows = new List<string>(); int faults = 0;
            var near = new Vector3(V.x, 0, V.y);
            foreach (var m in ef ? ef.modules : Array.Empty<GameObject>())
            {
                if (!m || !m.activeInHierarchy) continue;
                var lod0 = KitLods.Lod0(m).ToList(); if (lod0.Count == 0) continue;
                var bb = lod0[0].bounds; foreach (var r in lod0) bb.Encapsulate(r.bounds);
                if (bb.SqrDistance(near) > 12f * 12f) continue;
                // which wall the module stands on: the nearer edge line to its pivot (a pier on the vertex: both)
                var piv = new Vector2(m.transform.position.x, m.transform.position.z);
                float d0 = SegDist(piv, A, V), d1 = SegDist(piv, V, B);
                int own = Mathf.Abs(d0 - d1) < 0.3f ? -1 : d0 < d1 ? 0 : 1;
                // the furthest its L0 reaches past the OTHER wall's line, within that wall's length, above the plinth
                float worst = float.NegativeInfinity; string worstAt = "";
                for (int e = 0; e < 2; e++)
                {
                    if (own == e || own == -1) continue;
                    var (_, ea, eb, en) = edges[e]; var dir = (eb - ea); float len = dir.magnitude; dir /= len;
                    foreach (var r in lod0)
                    {
                        var mesh = r.GetComponent<MeshFilter>().sharedMesh; if (!mesh) continue;
                        var verts = mesh.vertices; var l2w = r.transform.localToWorldMatrix;
                        foreach (var lv in verts)
                        {
                            var w = l2w.MultiplyPoint3x4(lv); if (w.y < 0.3f) continue;
                            var p = new Vector2(w.x, w.z); float s = Vector2.Dot(p - ea, dir); if (s < 0.05f || s > len - 0.05f) continue;
                            float past = Vector2.Dot(p - ea, en);
                            if (past > worst) { worst = past; worstAt = $"{F(w.x)},{F(w.y)},{F(w.z)}"; }
                        }
                    }
                }
                bool fault = own != -1 && worst > 0.05f; if (fault) faults++;
                rows.Add($"    {{\"module\": \"{Esc(m.name)}\", \"pivot\": [{F(piv.x)}, {F(piv.y)}], \"wall\": \"{(own == -1 ? "corner (both)" : edges[own].Item1)}\", \"pastOtherWallM\": {(own == -1 || float.IsNegativeInfinity(worst) ? "null" : F(worst))}, \"at\": \"{worstAt}\", \"fault\": {(fault ? "true" : "false")}}}");
            }
            File.WriteAllText(file, "{\n" +
                $"  \"schema\": \"suborned.072-k6-check/1\",\n  \"building\": \"bld/lamplight-south-w/13\",\n  \"export\": \"{Esc(stamp ? stamp.exportPath + " sha256 " + stamp.exportSha256 : "?")}\",\n" +
                $"  \"corner\": [{F(V.x)}, {F(V.y)}], \"interiorAngleDeg\": {F(best)},\n" +
                $"  \"method\": \"Every every-face module of the building whose L0 bounds come within 12 m of the acute corner: the wall it stands on (the edge nearer its pivot; a pier on the vertex stands on both and is not tested), then its L0 vertices above 0.3 m, within the other wall's length, measured past that wall's line along its outward normal. A fault = more than 0.05 m past (K-6's tolerance): the piece showing through the other face.\",\n" +
                $"  \"faults\": {faults},\n  \"modules\": [\n{string.Join(",\n", rows)}\n  ]\n}}\n");
            Debug.Log($"072 k6: lamplight-south-w/13 corner {V} ({best:F1}°): {rows.Count} modules near, {faults} past the other wall by more than 5 cm → {file}");

            // shots: close perspective from outside the corner, and the play camera on it
            Directory.CreateDirectory(shotsDir);
            var bis = (Out(A, V) + Out(V, B)).normalized;
            var saved = (c.cam.orthographic, c.cam.fieldOfView, c.cam.nearClipPlane, c.cam.farClipPlane);
            try
            {
                var target = new Vector3(V.x, 6f, V.y); var eye = target + new Vector3(bis.x, 0, bis.y) * 26f + Vector3.up * 12f; // above the street trees and the neighbour's eaves
                c.cam.orthographic = false; c.cam.fieldOfView = 50f; c.cam.nearClipPlane = 0.3f; c.cam.farClipPlane = 2000f;
                c.cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye, Vector3.up));
                if (c.shadows) c.shadows.focusDepth = 20f; if (c.fade) c.fade.focus = new Vector3(V.x, 0, V.y);
                File.WriteAllBytes(Path.Combine(shotsDir, "k6-lamplight-south-w-13-close.png"), session.Grab(c.cam, W, H).EncodeToPNG());
                float yaw = Mathf.Atan2(-bis.x, -bis.y) * Mathf.Rad2Deg; yaw = (yaw + 360f) % 360f;
                Baseline072.Pose(c.cam, c.shadows, c.fade, V, yaw, c.standOff);
                File.WriteAllBytes(Path.Combine(shotsDir, $"k6-lamplight-south-w-13-play-y{Mathf.RoundToInt(yaw):000}.png"), session.Grab(c.cam, W, H).EncodeToPNG());
            }
            finally { c.cam.orthographic = saved.Item1; c.cam.fieldOfView = saved.Item2; c.cam.nearClipPlane = saved.Item3; c.cam.farClipPlane = saved.Item4; }
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return (a + ab * t - p).magnitude; }
        static float P(string s) => float.Parse(s, CultureInfo.InvariantCulture);
        static string F(double d, int places = 3) => Math.Round(d, places).ToString("0.#####", CultureInfo.InvariantCulture);
        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
