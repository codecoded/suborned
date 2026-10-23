using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Play
{
    /// <summary>`-tplong &lt;resolved.json&gt; &lt;outDir&gt;` (brief 048, Splice's `tp-long` batch row; Redline's draw-distance numbers): per row the follow
    /// view behind an agent at a heading (after the cell walks `walk` metres from the start) or the play view at the opening, the clock running and
    /// the HUD on, at the row's size; after `settleSeconds`, `seconds` of frames timed (unscaled gap), FrameTiming's GPU, main and render thread
    /// means, the Render ProfilerRecorders' Triangles, SetPass Calls and Shadow Casters means, then a census of what the camera draws (LOD- and
    /// static-batch-aware, as briefs/048/probe/Probe.cs). Writes `tplong.json` (suborned.tplong.report/1) after every row; exits 0 every row ran,
    /// 1 a row did not, 2 the file would not read.</summary>
    public sealed class TpLongRun : MonoBehaviour
    {
        public const string Schema = "suborned.tplong.report/1";

        [Serializable] public class Row { public string name = "", view = "follow", kind = "third", agent = "scout"; public float[] walk = { 0, 0 }; public float faceHeading = -1, yaw = 30, zoom = 14, settleSeconds = 1.5f, seconds = 6; public int w = 2880, h = 1864; }
        [Serializable] public class FileData { public string schema = "", outDir = ""; public Row[] rows; }

        sealed class Res
        {
            public string Name = "", Status = "ok", Error = ""; public int W, H; public float Heading = float.NaN;
            public int Count; public double Mean, P99, Max, Gpu = -1, Main = -1, Render = -1, Tris, SetPass, Casters, GrdMs = -1; public int Drawn, Beyond150;
        }

        string path, dir; readonly List<Res> results = new(); HandoverHost h;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-tplong");
            if (i < 0 || i + 2 >= a.Length) return;
            var go = new GameObject("tplong-run"); DontDestroyOnLoad(go);
            var r = go.AddComponent<TpLongRun>(); r.path = a[i + 1]; r.dir = a[i + 2];
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            FileData file;
            try { file = JsonUtility.FromJson<FileData>(File.ReadAllText(path)); if (file?.rows == null || file.rows.Length == 0) throw new FormatException("no rows"); }
            catch (Exception e) { Write("unreadable: " + e.Message); Application.Quit(2); yield break; }
            float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { Write("no host after 60 s"); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f); // the HUD built, the world streamed in
            h.controls.conversations = ConversationScenes.Never;
            bool allOk = true;
            foreach (var row in file.rows)
            {
                var res = new Res { Name = row.name }; results.Add(res);
                yield return CaptureSize.Apply(row.w, row.h);
                string err = Guard(() => { if (h.Following) h.FollowToggle(); h.Begin(); h.IntelOpen = false; h.StartClock(); });
                if (err != null) { Fail(res, err); allOk = false; Write("running"); continue; }
                yield return null; yield return null;
                if (row.view == "follow")
                {
                    int k = Math.Max(0, Array.IndexOf(new[] { "scout", "fixer", "tech", "enforcer" }, row.agent));
                    err = Guard(() => Walk(new Vector2(row.walk != null && row.walk.Length > 0 ? row.walk[0] : 0f, row.walk != null && row.walk.Length > 1 ? row.walk[1] : 0f)));
                    if (err != null) { Fail(res, err); allOk = false; Write("running"); continue; }
                    yield return FollowAt(k, row.faceHeading);
                }
                h.SetPaused(false);
                yield return new WaitForSecondsRealtime(row.settleSeconds);
                if (row.view == "follow") { var cam = h.View; if (cam) res.Heading = Mathf.Repeat(cam.transform.eulerAngles.y, 360f); }
                yield return Time_(res, row.seconds);
                res.W = Screen.width; res.H = Screen.height;
                err = Guard(() => Census(res));
                if (err != null) { Fail(res, err); allOk = false; }
                h.SetPaused(true);
                // brief 048 (Glint's checklist, for Glyph and Sky): a picture of each row as timed; at the first follow row also the sky teal
                // (Shift+F9) and the atmosphere off (Shift+F10) for the A/B, and the long tier (far > 900 m: the flyby's look) 60 m above it
                yield return Picture(row.name);
                if (row.view == "follow" && !extras)
                {
                    extras = true;
                    bool teal = AtmosphereSettings.SkyTeal, on = AtmosphereSettings.Enabled;
                    AtmosphereSettings.SkyTeal = true; yield return null; yield return null; yield return Picture(row.name + "-teal"); AtmosphereSettings.SkyTeal = teal;
                    AtmosphereSettings.Enabled = false; yield return null; yield return null; yield return Picture(row.name + "-atmosphere-off"); AtmosphereSettings.Enabled = on;
                    yield return LongPicture(row.name + "-long");
                    yield return LongTiming(row.name); // Glint 2026-10-23: the long view's cost with the far blur on and off
                }
                if (h.Following) h.FollowToggle();
                Write("running");
            }
            yield return PalmLods(); // brief 048 (Aster 2026-10-22, Sky approved palm LODs): Market Street east and west, and the commander view
            yield return SignalsHaze(); // Glint 2026-10-23: the signals in a long third-person view under the haze
            yield return GlassFrames(); // Vela 2026-10-23: tower windows (DK_GlassSky) and the facade's class 3 shopfront glass, close, in third person
            Write(allOk ? "ok" : "failed"); yield return new WaitForSecondsRealtime(0.5f); Application.Quit(allOk ? 0 : 1);
        }

        // ---------------------------------------------------------------- palm LODs (Splice adopted L1 1,330 / L2 514 tris, per-id switch heights)

        /// <summary>The palms' LOD switch in the third-person view on Market Street (world z -410, the export's line x -340 to 300) facing east
        /// and west from x -20 at eye height 3 m (the follow view's: fov 55, far 350 m), and in the commander view at the opening: per view the
        /// palms in frame by active level and the nearest and farthest distance at each level; a picture each (palms-east.jpg, palms-west.jpg).
        /// Writes palms.json. The level is the one Unity picks (LOD group size x lossy scale, the screen height at that distance, lodBias).</summary>
        IEnumerator PalmLods()
        {
            var rows = new List<string>(); var groups = new List<LODGroup>();
            foreach (var g in FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (g.name.StartsWith("DK_CoconutPalm")) groups.Add(g);
            string Levels(Camera cam, string view)
            {
                var planes = GeometryUtility.CalculateFrustumPlanes(cam); var cp = cam.transform.position; float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), bias = QualitySettings.lodBias;
                var n = new int[4]; var lo = new float[4]; var hi = new float[4]; for (int i = 0; i < 4; i++) { lo[i] = float.MaxValue; hi[i] = 0f; }
                foreach (var g in groups)
                {
                    var t = g.transform; var refp = t.TransformPoint(g.localReferencePoint); float d = Vector3.Distance(refp, cp);
                    if (!cam.orthographic && d > cam.farClipPlane) continue;
                    var lods = g.GetLODs(); Bounds b = default; bool any = false; foreach (var l in lods) foreach (var r in l.renderers) if (r) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                    if (!any || !GeometryUtility.TestPlanesAABB(planes, b)) continue;
                    var ls = t.lossyScale; float size = g.size * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                    float rel = (cam.orthographic ? size * 0.5f / cam.orthographicSize : size * 0.5f / (Mathf.Max(0.001f, d) * tanHalf)) * bias;
                    int a = 3; for (int i = 0; i < lods.Length; i++) if (rel >= lods[i].screenRelativeTransitionHeight) { a = i; break; }
                    a = Mathf.Min(a, 3); n[a]++; lo[a] = Mathf.Min(lo[a], d); hi[a] = Mathf.Max(hi[a], d);
                }
                string L(int i) => n[i] == 0 ? "null" : $"{{\"count\": {n[i]}, \"nearM\": {F(lo[i])}, \"farM\": {F(hi[i])}}}";
                return $"{{\"view\": \"{view}\", \"L0\": {L(0)}, \"L1\": {L(1)}, \"L2\": {L(2)}, \"culled\": {L(3)}}}";
            }
            var cam0 = h.View;
            if (cam0) { if (h.Following) h.FollowToggle(); h.Begin(); yield return null; yield return null; rows.Add(Levels(h.View, "commander-opening")); }
            if (cam0)
            {
                var go = new GameObject("tplong-palm-cam"); var pc = go.AddComponent<Camera>(); pc.CopyFrom(h.View); pc.enabled = false; pc.orthographic = false; pc.fieldOfView = h.followFov; pc.nearClipPlane = 0.2f; pc.farClipPlane = Mathf.Min(h.followFar, DistanceHaze.FarClip > 0f ? DistanceHaze.FarClip : h.followFar);
                go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
                // in a carriageway (the road is 18 m with the palm median on its line), stepped on until nothing stands in front of the lens
                // (0.2.78's frames stood behind a signal eye and a median trunk)
                var blockers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Where(r => r.enabled && r.bounds.max.y > 0.6f && r.bounds.size.x < 20f && r.bounds.size.z < 20f).Select(r => r.bounds).ToArray();
                foreach (var (name, dir, side) in new[] { ("palms-east", Vector3.right, -5.5f), ("palms-west", Vector3.left, 5.5f) })
                {
                    var at = new Vector3(-20f, 3f, -410f + side);
                    for (int k = 0; k < 15 && Blocked(at, dir, blockers); k++) at += dir * 4f;
                    go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(dir + Vector3.down * 0.06f));
                    rows.Add(Levels(pc, name));
                    yield return new WaitForEndOfFrame();
                    int w = Screen.width, ht = Screen.height; var rt = new RenderTexture(w, ht, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    try
                    {
                        for (int i = 0; i < 2; i++) { pc.targetTexture = rt; pc.Render(); }
                        var prev = RenderTexture.active; RenderTexture.active = rt; var tx = new Texture2D(w, ht, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, w, ht), 0, 0); tx.Apply(); RenderTexture.active = prev;
                        File.WriteAllBytes(Path.Combine(dir_, name + ".jpg"), tx.EncodeToJPG(88)); Destroy(tx);
                    }
                    catch (Exception e) { Debug.LogError("TpLongRun: palm picture: " + e.Message); }
                    pc.targetTexture = null; rt.Release(); Destroy(rt);
                }
                Destroy(go);
            }
            try { File.WriteAllText(Path.Combine(dir_, "palms.json"), "{\n \"schema\": \"suborned.tplong.palms/1\", \"palmGroups\": " + groups.Count + ", \"lodBias\": " + F(QualitySettings.lodBias) + ", \"views\": [\n  " + string.Join(",\n  ", rows) + "\n ]\n}\n"); } catch (Exception e) { Debug.LogError("TpLongRun: palms.json: " + e.Message); }
        }
        string dir_ => dir;

        // ---------------------------------------------------------------- glass (Vela 2026-10-23)

        /// <summary>Two stills from the follow view's lens (fov, far 350, post) facing Market Street's north side: `tp-shopfront-glass` (eye 2 m at the
        /// north kerb, the frontage across the pavement, level) and `tp-tower-windows` (eye 3 m from the south lane, 32 degrees up the same frontage),
        /// each stepped east until nothing stands within 10 m of the lens.</summary>
        IEnumerator GlassFrames()
        {
            var view = h.View; if (!view) yield break;
            if (h.Following) h.FollowToggle(); h.SetPaused(true); yield return null;
            var go = new GameObject("tplong-glass-cam"); var pc = go.AddComponent<Camera>(); pc.CopyFrom(view); pc.enabled = false; pc.orthographic = false; pc.fieldOfView = h.followFov; pc.nearClipPlane = 0.2f; pc.farClipPlane = Mathf.Min(h.followFar, DistanceHaze.FarClip > 0f ? DistanceHaze.FarClip : h.followFar);
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            var blockers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.enabled && r.bounds.max.y > 0.6f && r.bounds.size.x < 20f && r.bounds.size.z < 20f).Select(r => r.bounds).ToArray();
            // Market Street runs along x at z -410 (18 m road): the north side's ground-floor shops (the hotel's, as palms-east shows them) stand about
            // 9 m past the north kerb; 0.2.80's first try looked south from x -30 and went down a side street
            var north = Vector3.forward;
            foreach (var (name, at, pitch) in new[] { ("tp-shopfront-glass", new Vector3(5f, 2f, -404f), 0f), ("tp-tower-windows", new Vector3(5f, 3f, -414f), 32f) })
            {
                var p = at; var look = Quaternion.AngleAxis(-pitch, Vector3.Cross(Vector3.up, north)) * north; // tilted up by `pitch`
                for (int k = 0; k < 15 && Blocked(p, north, blockers); k++) p += Vector3.right * 4f;
                go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(look));
                yield return new WaitForEndOfFrame();
                int w = Screen.width, ht = Screen.height; var rt = new RenderTexture(w, ht, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                try
                {
                    for (int i = 0; i < 2; i++) { pc.targetTexture = rt; pc.Render(); }
                    var prev = RenderTexture.active; RenderTexture.active = rt; var tx = new Texture2D(w, ht, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, w, ht), 0, 0); tx.Apply(); RenderTexture.active = prev;
                    File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), tx.EncodeToJPG(88)); Destroy(tx);
                }
                catch (Exception e) { Debug.LogError("TpLongRun: glass picture: " + e.Message); }
                pc.targetTexture = null; rt.Release(); Destroy(rt);
                Debug.Log($"TpLongRun: {name} at {p}");
            }
            Destroy(go);
        }

        // ---------------------------------------------------------------- the signals under the haze (Glint 2026-10-23)

        /// <summary>The signals under the haze (Glint 2026-10-23, round 2): the Scout learns the guard's sight and the Tech the entrance camera's, and their
        /// fans are drawn (Alt held and ShowFans; `fansDrawn` counts the fan objects really on). A follow-tier camera (perspective, far 800 m, 30 m up,
        /// pitched 25 degrees down, heading 210) stands so the guard's fan is 150 m, then 250 m away (the follow haze's 23 % and 62 %). Each distance is
        /// shot with the haze on and off, twice over: the fans as built (the cones are on the default layer, so the base camera draws them, fogged), and
        /// the fans moved for the shot onto the signal layer that the stacked overlay camera draws (DistanceHaze: an overlay camera is the play tier, no
        /// fog). A mask frame (the fans alone on black) gives the pixels the ΔE is read over (tools: briefs/048/build/signals-de.json).
        /// Writes signals-&lt;d&gt;-&lt;asbuilt|overlay&gt;-&lt;haze|nohaze&gt;.png, signals-&lt;d&gt;-mask.png and signals.json (suborned.tplong.signals/2).</summary>
        IEnumerator SignalsHaze()
        {
            int controlMats = 0; string note = ""; bool guardCone = false, camCone = false; var rows = new List<string>(); int drawn = 0; string watchers = "";
            Vector3 gw = default; bool ready = false;
            string err = Guard(() =>
            {
                if (h.Following) h.FollowToggle();
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true);
                var g = h.Game;
                h.GiveScripted(new Suborned.Sim.Order { Op = "scout", Do = "watch", Target = "guard" });
                h.GiveScripted(new Suborned.Sim.Order { Op = "tech", Do = "watch", Target = "entrance" });
                int lim = g.Tick + Suborned.Sim.Game.T(120);
                while (g.Tick < lim && !g.Over && !((g.Known("guard")?.Sight ?? false) && (g.Known("entrance")?.Sight ?? false))) h.RunTo(g.Tick + 5);
                guardCone = g.Known("guard")?.Sight ?? false; camCone = g.Known("entrance")?.Sight ?? false;
                var fans = g.KnownFans(); watchers = string.Join(",", fans.Select(f => f.Watcher));
                var gf = fans.FirstOrDefault(f => f.Watcher == "guard"); if (gf == null) { note = "no guard fan"; return; }
                gw = h.frame.World(gf.At); ready = true;
            });
            if (err != null) note = err;
            if (ready)
            {
                h.PeekLatch = true; h.SetPaused(true); yield return null; yield return null;
                h.Views.ShowFans(h.Game.KnownFans()); yield return null;
                drawn = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Count(t => t.name.StartsWith("fan/") && t.gameObject.activeInHierarchy);
                var fanRenderers = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(t => t.name.StartsWith("fan/")).SelectMany(t => t.GetComponentsInChildren<Renderer>(true)).ToList();
                var layers = fanRenderers.Select(r => r.gameObject.layer).ToList();
                const int SignalLayer = HandoverHost.SignalLayer;
                var bgo = new GameObject("tplong-signals-base"); var bc = bgo.AddComponent<Camera>(); bc.CopyFrom(h.View); bc.enabled = false;
                bc.orthographic = false; bc.fieldOfView = h.followFov; bc.nearClipPlane = 0.3f; bc.farClipPlane = 800f; bc.cullingMask = h.View.cullingMask & ~(1 << SignalLayer);
                var bud = bgo.AddComponent<UniversalAdditionalCameraData>(); bud.renderType = CameraRenderType.Base; bud.renderPostProcessing = true;
                var ogo = new GameObject("tplong-signals-overlay"); ogo.transform.SetParent(bgo.transform, false); var oc = ogo.AddComponent<Camera>(); oc.CopyFrom(bc); oc.enabled = false; oc.cullingMask = 1 << SignalLayer; oc.clearFlags = CameraClearFlags.Nothing;
                var oud = ogo.AddComponent<UniversalAdditionalCameraData>(); oud.renderType = CameraRenderType.Overlay; oud.renderPostProcessing = false; bud.cameraStack.Add(oc);
                var mgo = new GameObject("tplong-signals-mask"); var mc = mgo.AddComponent<Camera>(); mc.CopyFrom(bc); mc.enabled = false; mc.cullingMask = 1 << SignalLayer; mc.clearFlags = CameraClearFlags.SolidColor; mc.backgroundColor = Color.black;
                var mud = mgo.AddComponent<UniversalAdditionalCameraData>(); mud.renderPostProcessing = false;
                string baseTier = DistanceHaze.TierOf(bc).ToString(), overlayTier = DistanceHaze.TierOf(oc).ToString();
                Vector3 dv;
                bool atm = AtmosphereSettings.Enabled;
                int w = Screen.width, ht = Screen.height;
                Color32[] Grab(Camera c, int gw2, int gh2, string name)
                {
                    var rt = new RenderTexture(gw2, gh2, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); Color32[] px = null;
                    try
                    {
                        for (int i = 0; i < 2; i++) { c.targetTexture = rt; c.Render(); } // the second frame: a camera made fresh gets its post on its second (AtmosphereLens)
                        var prev = RenderTexture.active; RenderTexture.active = rt; var tx = new Texture2D(gw2, gh2, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, gw2, gh2), 0, 0); tx.Apply(); RenderTexture.active = prev;
                        if (name != null) File.WriteAllBytes(Path.Combine(dir, name + ".png"), tx.EncodeToPNG()); else px = tx.GetPixels32(); Destroy(tx);
                    }
                    catch (Exception e) { Debug.LogError("TpLongRun: signals picture " + name + ": " + e.Message); }
                    c.targetTexture = null; rt.Release(); Destroy(rt); return px;
                }
                void Shoot(Camera c, string name) => Grab(c, w, ht, name);
                void Layers(bool onSignal) { for (int i = 0; i < fanRenderers.Count; i++) if (fanRenderers[i]) fanRenderers[i].gameObject.layer = onSignal ? SignalLayer : layers[i]; }
                /// the share of the fan's mask pixels the fan really changes in the frame (1: in plain view; 0: behind a roof), at 720p with the haze off
                float Visible()
                {
                    AtmosphereSettings.Enabled = false; Layers(true); var mask = Grab(mc, 1280, 720, null); var bare = Grab(bc, 1280, 720, null); Layers(false); var with = Grab(bc, 1280, 720, null);
                    if (mask == null || bare == null || with == null) return 0f; int n = 0, seen = 0;
                    for (int i = 0; i < mask.Length; i++) { if (mask[i].r + mask[i].g + mask[i].b < 72) continue; n++; if (Mathf.Abs(bare[i].r - with[i].r) + Mathf.Abs(bare[i].g - with[i].g) + Mathf.Abs(bare[i].b - with[i].b) > 18) seen++; }
                    return n < 40 ? 0f : seen / (float)n;
                }
                foreach (int d in new[] { 150, 250 })
                {
                    float bestVis = -1f, bestHd = 210f, bestUp = 30f, dh = 0f; var tried = new List<string>();
                    // a clear line to the fan: 30 m up first (Glint), heading 210 then round the compass in 15 degree steps; the first with half the fan
                    // in plain view; when no heading clears the roofs, 60 m, then 90 m (the pitch keeps 25 degrees)
                    foreach (float up in new[] { 30f, 60f, 90f })
                    {
                        dh = Mathf.Sqrt(Mathf.Max(1f, d * d - up * up));
                        for (int k = 0; k < 24; k++)
                        {
                            float hd = 210f + (k % 2 == 0 ? 1 : -1) * 15f * ((k + 1) / 2);
                            var dir = new Vector3(Mathf.Sin(hd * Mathf.Deg2Rad), 0f, Mathf.Cos(hd * Mathf.Deg2Rad));
                            bgo.transform.SetPositionAndRotation(gw - dir * dh + Vector3.up * up, Quaternion.LookRotation(dir) * Quaternion.Euler(25f, 0f, 0f));
                            mgo.transform.SetPositionAndRotation(bgo.transform.position, bgo.transform.rotation);
                            float vis = Visible(); if (vis > bestVis) { bestVis = vis; bestHd = hd; bestUp = up; } if (k % 4 == 0) tried.Add($"{up:0}m/{hd:0}:{vis:0.00}");
                            if (vis >= 0.5f) break;
                            yield return null;
                        }
                        if (bestVis >= 0.5f) break;
                    }
                    { var dir = new Vector3(Mathf.Sin(bestHd * Mathf.Deg2Rad), 0f, Mathf.Cos(bestHd * Mathf.Deg2Rad)); dv = dir; dh = Mathf.Sqrt(Mathf.Max(1f, d * d - bestUp * bestUp));
                      bgo.transform.SetPositionAndRotation(gw - dir * dh + Vector3.up * bestUp, Quaternion.LookRotation(dir) * Quaternion.Euler(25f, 0f, 0f));
                      mgo.transform.SetPositionAndRotation(bgo.transform.position, bgo.transform.rotation); }
                    var vp = bc.WorldToViewportPoint(gw); bool inFrame = vp.z > 0 && vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f;
                    Layers(false);
                    // the haze alone on and off (DistanceHaze; the lens's bloom and grade stay on in both, so only the fog differs: Glint's ΔE <= 1)
                    var hz0 = DistanceHaze.Instance;
                    foreach (var haze in new[] { true, false })
                    {
                        AtmosphereSettings.Enabled = true; if (hz0) hz0.enabled = haze; yield return null; yield return null;
                        Shoot(bc, $"signals-{d}-asbuilt-{(haze ? "haze" : "nohaze")}");
                    }
                    if (hz0) hz0.enabled = true;
                    // Glint's control (test only, restored at once): the same frame, haze off, with the fan's depth offset (-2, -2) and with ZTest Always;
                    // seen only with ZTest Always: a building hides it (correct); seen with the offset: depth precision
                    var fanMats = fanRenderers.Where(r => r).SelectMany(r => r.sharedMaterials).Where(mm => mm && mm.HasProperty("_ZTest")).Distinct().ToList();
                    AtmosphereSettings.Enabled = false;
                    foreach (var mm in fanMats) { mm.SetFloat("_OffsetFactor", -2f); mm.SetFloat("_OffsetUnits", -2f); }
                    yield return null; Shoot(bc, $"signals-{d}-control-offset");
                    foreach (var mm in fanMats) { mm.SetFloat("_OffsetFactor", 0f); mm.SetFloat("_OffsetUnits", 0f); mm.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always); }
                    yield return null; Shoot(bc, $"signals-{d}-control-ztest-always");
                    foreach (var mm in fanMats) mm.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                    controlMats = fanMats.Count;
                    Layers(true); AtmosphereSettings.Enabled = false; yield return null; Shoot(bc, $"signals-{d}-bare-nohaze"); // the frame with no fan (the base camera does not draw the signal layer)
                    foreach (var r in fanRenderers) if (r) r.gameObject.layer = SignalLayer; // the mask: the fans alone on black
                    AtmosphereSettings.Enabled = false; yield return null; Shoot(mc, $"signals-{d}-mask");
                    for (int i = 0; i < fanRenderers.Count; i++) if (fanRenderers[i]) fanRenderers[i].gameObject.layer = layers[i];
                    rows.Add($"{{\"distanceM\": {d}, \"headingDeg\": {F(bestHd)}, \"fanVisibleShare\": {F(bestVis)}, \"tried\": \"{string.Join(" ", tried)}\", \"cameraHeightM\": {F(bestUp)}, \"pitchDeg\": 25, \"guardInFrame\": {(inFrame ? "true" : "false")}, \"slantM\": {F(Vector3.Distance(bgo.transform.position, gw))}}}");
                }
                AtmosphereSettings.Enabled = atm; h.PeekLatch = false;
                Destroy(bgo); Destroy(mgo);
                note = $"base camera tier {baseTier}; fans on layer(s) {string.Join("/", layers.Distinct())}; fan shader(s) {string.Join("/", fanRenderers.Where(r => r).SelectMany(r => r.sharedMaterials).Where(mm => mm).Select(mm => mm.shader.name).Distinct())}; control materials {controlMats}";
            }
            try { File.WriteAllText(Path.Combine(dir, "signals.json"), "{\n \"schema\": \"suborned.tplong.signals/2\", \"guardSightKnown\": " + (guardCone ? "true" : "false") + ", \"cameraSightKnown\": " + (camCone ? "true" : "false") + ", \"fansDrawn\": " + drawn + ", \"fanWatchers\": \"" + Esc(watchers) + "\",\n \"shots\": [" + string.Join(", ", rows) + "],\n \"note\": \"" + Esc(note) + "\"\n}\n"); } catch (Exception e) { Debug.LogError("TpLongRun: signals.json: " + e.Message); }
        }

        /// <summary>Something stands within 10 m in front of a camera at `p` looking along `dir` (five rays: centre, 1.6 m either side, 1.2 m up and down).</summary>
        static bool Blocked(Vector3 p, Vector3 dir, Bounds[] bs)
        {
            var side = Vector3.Cross(Vector3.up, dir).normalized;
            foreach (var o in new[] { Vector3.zero, side * 1.6f, -side * 1.6f, Vector3.up * 1.2f, Vector3.down * 1.2f })
            {
                var ray = new Ray(p + o, dir);
                foreach (var b in bs) if (b.Contains(ray.origin) || (b.IntersectRay(ray, out float d) && d < 10f)) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- the drive (as briefs/048/probe/Probe.cs)

        Vector3 OpPos(int k) { var t = h.Views.Find("op/" + Suborned.Sim.Op.Name((Suborned.Sim.OpId)k)); return t ? t.position : Vector3.zero; }

        /// <summary>The cell walks (paused, the engine stepped) to its start + (dx, dz) world metres, each a little apart.</summary>
        void Walk(Vector2 d)
        {
            if (d == Vector2.zero) return;
            h.SetPaused(true);
            for (int k = 0; k < 4; k++)
            {
                var p = OpPos(k) + new Vector3(d.x, 0f, d.y) + new Vector3(k * 1.2f, 0f, 0f);
                h.Issue(new Suborned.Sim.Order { Op = Suborned.Sim.Op.Name((Suborned.Sim.OpId)k), Do = "move", Point = h.frame.Engine(p) });
            }
            for (int g = 0; g < 2000 && !h.Game.Over && (h.Game.Ops[0].Walk != null || h.Game.Ops[1].Walk != null || h.Game.Ops[2].Walk != null || h.Game.Ops[3].Walk != null); g++) h.RunTo(h.Game.Tick + 1);
        }

        IEnumerator FollowAt(int k, float heading)
        {
            if (h.Following) h.FollowToggle();
            h.Select((Suborned.Sim.OpId)k, false); yield return null;
            h.FollowToggle(); yield return null;
            var p = OpPos(k); var dirv = new Vector3(Mathf.Sin(heading * Mathf.Deg2Rad), 0f, Mathf.Cos(heading * Mathf.Deg2Rad));
            h.FollowFace(p + dirv * 50f);
            yield return new WaitForSecondsRealtime(0.8f);
            h.FollowFace(p + dirv * 50f);
        }

        bool extras;
        IEnumerator Picture(string name)
        {
            yield return new WaitForEndOfFrame();
            var t = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), t.EncodeToJPG(88)); } catch (Exception e) { Debug.LogError("TpLongRun: picture " + name + ": " + e.Message); }
            Destroy(t);
        }
        /// <summary>The long tier (DistanceHaze.TierOf: perspective, far over 900 m): the follow camera raised 60 m, looking out 15 degrees down,
        /// far 2500 m, for one frame (the flyby's and free fly's look); then the follow view as it was.</summary>
        IEnumerator LongPicture(string name)
        {
            var cam = h.View; if (!cam || cam.orthographic) yield break;
            yield return new WaitForEndOfFrame();
            var fwd = cam.transform.forward; fwd.y = 0f; if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward; fwd.Normalize();
            var go = new GameObject("tplong-long-cam"); var lc = go.AddComponent<Camera>(); lc.CopyFrom(cam); lc.enabled = false;
            var ud = go.AddComponent<UniversalAdditionalCameraData>(); ud.renderPostProcessing = true;
            lc.farClipPlane = 2500f; go.transform.SetPositionAndRotation(cam.transform.position + Vector3.up * 60f, Quaternion.LookRotation(Quaternion.AngleAxis(15f, Vector3.Cross(Vector3.up, fwd)) * fwd));
            int w = Screen.width, ht = Screen.height; var rt = new RenderTexture(w, ht, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            try
            {
                for (int i = 0; i < 2; i++) { lc.targetTexture = rt; lc.Render(); } // the second frame: a camera made fresh gets its post on its second (AtmosphereLens)
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var t = new Texture2D(w, ht, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, w, ht), 0, 0); t.Apply(); RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), t.EncodeToJPG(88)); Destroy(t);
            }
            catch (Exception e) { Debug.LogError("TpLongRun: long picture: " + e.Message); }
            lc.targetTexture = null; rt.Release(); Destroy(rt); Destroy(go);
        }

        /// <summary>The long tier timed (Glint 2026-10-23: is the Gaussian far blur worth its cost?): a second camera as LongPicture's (far 2500 m, 60 m
        /// up, 15 degrees down, post on) drawn every frame under the play camera for 4 s with AtmosphereLens.LongDof on, then off; `long.json` gives both
        /// and the difference, and `&lt;row&gt;-long-nodof.jpg` the picture without the blur. The difference is the blur's (both runs draw the same).</summary>
        IEnumerator LongTiming(string row)
        {
            var cam = h.View; if (!cam || cam.orthographic) yield break;
            var fwd = cam.transform.forward; fwd.y = 0f; if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward; fwd.Normalize();
            var go = new GameObject("tplong-long-timed"); var lc = go.AddComponent<Camera>(); lc.CopyFrom(cam);
            var ud = go.AddComponent<UniversalAdditionalCameraData>(); ud.renderPostProcessing = true;
            lc.farClipPlane = 2500f; lc.depth = cam.depth - 1; // under the play camera (the host's Uncover stands down any camera above it)
            go.transform.SetPositionAndRotation(cam.transform.position + Vector3.up * 60f, Quaternion.LookRotation(Quaternion.AngleAxis(15f, Vector3.Cross(Vector3.up, fwd)) * fwd));
            bool was = AtmosphereLens.LongDof; var on = new Res { Name = "long-dof" }; var off = new Res { Name = "long-nodof" };
            AtmosphereLens.LongDof = true; yield return new WaitForSecondsRealtime(1f); yield return Time_(on, 4f);
            AtmosphereLens.LongDof = false; yield return new WaitForSecondsRealtime(1f); yield return Time_(off, 4f);
            Destroy(go);
            yield return LongPicture(row + "-long-nodof");
            AtmosphereLens.LongDof = was;
            string R(Res r) => $"{{\"frames\": {r.Count}, \"meanMs\": {F(r.Mean)}, \"p99Ms\": {F(r.P99)}, \"gpuMs\": {F(r.Gpu)}, \"renderMs\": {F(r.Render)}}}";
            try { File.WriteAllText(Path.Combine(dir, "long.json"), "{\n \"schema\": \"suborned.tplong.long/1\", \"row\": \"" + Esc(row) + "\", \"screen\": [" + Screen.width + ", " + Screen.height + "],\n \"dofOn\": " + R(on) + ",\n \"dofOff\": " + R(off) + ",\n \"deltaMeanMs\": " + F(on.Mean - off.Mean) + ", \"deltaGpuMs\": " + F(on.Gpu >= 0 && off.Gpu >= 0 ? on.Gpu - off.Gpu : -1) + "\n}\n"); }
            catch (Exception e) { Debug.LogError("TpLongRun: long.json: " + e.Message); }
        }

        // ---------------------------------------------------------------- timing and counters

        static ProfilerRecorder Counter(string name) { var r = ProfilerRecorder.StartNew(ProfilerCategory.Render, name, 1); return r; }

        IEnumerator Time_(Res res, float seconds)
        {
            var tris = Counter("Triangles Count"); var sp = Counter("SetPass Calls Count"); var sc = Counter("Shadow Casters Count");
            // brief 045 F6: the GPU Resident Drawer's draw-list rebuild, when the player exposes the marker (development builds); -1 when not
            ProfilerRecorder grd = default; var hs = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(hs);
            foreach (var hh in hs) { var d = ProfilerRecorderHandle.GetDescription(hh); if (d.Name.Contains("RebuildDrawLists")) { grd = new ProfilerRecorder(hh, 1, ProfilerRecorderOptions.Default); grd.Start(); break; } }
            yield return null; yield return null;
            var ms = new List<double>(); double tSum = 0, spSum = 0, scSum = 0, grdSum = 0; int grdN = 0;
            double gpu = 0, main = 0, rt = 0; int gn = 0; var ft = new FrameTiming[1];
            double last = Time.realtimeSinceStartupAsDouble, t0 = last;
            while (Time.realtimeSinceStartupAsDouble - t0 < seconds)
            {
                yield return null; double now = Time.realtimeSinceStartupAsDouble; ms.Add((now - last) * 1000.0); last = now;
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, ft) == 1) { if (ft[0].gpuFrameTime > 0) { gpu += ft[0].gpuFrameTime; gn++; } main += ft[0].cpuMainThreadFrameTime; rt += ft[0].cpuRenderThreadFrameTime; }
                if (tris.Valid) tSum += tris.LastValue; if (sp.Valid) spSum += sp.LastValue; if (sc.Valid) scSum += sc.LastValue;
                if (grd.Valid) { grdSum += grd.LastValue / 1e6; grdN++; }
            }
            int n = Math.Max(1, ms.Count); var sorted = ms.OrderBy(x => x).ToList();
            res.Count = ms.Count; res.Mean = ms.Count > 0 ? ms.Average() : 0; res.P99 = ms.Count > 0 ? sorted[Math.Max(0, (int)Math.Ceiling(0.99 * sorted.Count) - 1)] : 0; res.Max = ms.Count > 0 ? sorted[^1] : 0;
            res.Gpu = gn > 0 ? gpu / gn : -1; res.Main = main / n; res.Render = rt / n;
            res.Tris = tSum / n; res.SetPass = spSum / n; res.Casters = scSum / n; res.GrdMs = grdN > 0 ? grdSum / grdN : -1;
            tris.Dispose(); sp.Dispose(); sc.Dispose(); if (grd.Valid) grd.Dispose();
        }

        // ---------------------------------------------------------------- census: renderers this camera draws (LOD-aware), and those beyond 150 m

        void Census(Res res)
        {
            var cam = h.View; if (!cam) throw new InvalidOperationException("no camera");
            var planes = GeometryUtility.CalculateFrustumPlanes(cam); var cp = cam.transform.position;
            float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), bias = QualitySettings.lodBias;
            // as briefs/048/probe/Probe.cs (the spec, Splice 2026-10-23): a renderer in a LOD group counts only when its own level (the first it is
            // listed in) is the group's active one; a culled group counts nothing; the frustum alone decides distance (no straight-line far test)
            var active = new Dictionary<Renderer, bool>();
            foreach (var g in FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var lods = g.GetLODs(); if (lods.Length == 0) continue;
                int a = -1;
                if (g.enabled)
                {
                    var t = g.transform; var refp = t.TransformPoint(g.localReferencePoint); var ls = t.lossyScale;
                    float size = g.size * Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                    float rel = cam.orthographic ? size * 0.5f / cam.orthographicSize : size * 0.5f / (Mathf.Max(0.001f, Vector3.Distance(refp, cp)) * tanHalf);
                    rel *= bias;
                    for (int i = 0; i < lods.Length; i++) if (rel >= lods[i].screenRelativeTransitionHeight) { a = i; break; }
                }
                for (int i = 0; i < lods.Length; i++) foreach (var r in lods[i].renderers) if (r && !active.ContainsKey(r)) active[r] = i == a;
            }
            int drawn = 0, far = 0;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled || r.forceRenderingOff || !(r is MeshRenderer || r is SkinnedMeshRenderer) || (cam.cullingMask & (1 << r.gameObject.layer)) == 0) continue;
                if (active.TryGetValue(r, out var on) && !on) continue;
                var b = r.bounds; if (!GeometryUtility.TestPlanesAABB(planes, b)) continue;
                drawn++; bool beyond = b.SqrDistance(cp) >= 150f * 150f; if (beyond) far++;
                Tally(byBranch, Branch(r.transform), beyond); Tally(byPiece, Piece(r.transform), beyond);
            }
            res.Drawn = drawn; res.Beyond150 = far;
            WriteCensus(res.Name, drawn, far);
        }

        // what the census counted, by scene branch (root / child / grandchild) and by piece (the nearest LODGroup owner, else the object's name
        // without its index): `census-<row>.json` beside the report, for Redline's and Splice's reading of the counts (not judged)
        readonly Dictionary<string, (int all, int far)> byBranch = new(), byPiece = new();
        static void Tally(Dictionary<string, (int all, int far)> d, string k, bool far) { d.TryGetValue(k, out var v); d[k] = (v.all + 1, v.far + (far ? 1 : 0)); }
        static string Branch(Transform t)
        {
            var parts = new List<string>(); for (var p = t; p; p = p.parent) parts.Add(p.name); parts.Reverse();
            return string.Join("/", parts.Take(Math.Min(3, Math.Max(1, parts.Count - 1))));
        }
        static string Piece(Transform t)
        {
            for (var p = t; p; p = p.parent) if (p.GetComponent<LODGroup>()) return Stem(p.name);
            return Stem(t.name);
        }
        static string Stem(string n) => System.Text.RegularExpressions.Regex.Replace(n.Replace("(Clone)", ""), @"[\s_#-]*\(?\d+\)?$|[|@].*$", "").Trim();
        void WriteCensus(string row, int drawn, int far)
        {
            string Top(Dictionary<string, (int all, int far)> d) => string.Join(",\n   ", d.OrderByDescending(kv => kv.Value.far).ThenByDescending(kv => kv.Value.all).Take(30)
                .Select(kv => "{\"key\": \"" + Esc(kv.Key) + "\", \"drawn\": " + kv.Value.all + ", \"beyond150\": " + kv.Value.far + "}"));
            try
            {
                File.WriteAllText(Path.Combine(dir, "census-" + row + ".json"), "{\n \"schema\": \"suborned.tplong.census/1\", \"row\": \"" + Esc(row) + "\", \"drawn\": " + drawn + ", \"beyond150\": " + far +
                    ",\n \"byBranch\": [\n   " + Top(byBranch) + "\n ],\n \"byPiece\": [\n   " + Top(byPiece) + "\n ]\n}\n");
            }
            catch (Exception e) { Debug.LogError("TpLongRun: census: " + e.Message); }
            byBranch.Clear(); byPiece.Clear();
        }

        static string Guard(Action a) { try { a(); return null; } catch (Exception e) { return e.GetType().Name + ": " + e.Message; } }
        void Fail(Res r, string why) { r.Status = "failed"; r.Error = why; Debug.LogError($"TpLongRun: {r.Name}: {why}"); }
        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "'").Replace("\n", " ");
        static string F(double v) => double.IsNaN(v) || double.IsInfinity(v) ? "-1" : v.ToString("0.###", CultureInfo.InvariantCulture);

        void Write(string status)
        {
            var sb = new StringBuilder();
            sb.Append("{\n \"schema\": \"").Append(Schema).Append("\",\n \"status\": \"").Append(Esc(status)).Append("\",\n \"build\": \"").Append(Esc(BuildStamp.Line)).Append("\",\n \"rows\": [");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                sb.Append(i == 0 ? "\n" : ",\n").Append("  {\"name\": \"").Append(Esc(r.Name)).Append("\", \"status\": \"").Append(r.Status).Append('"');
                if (r.Error.Length > 0) sb.Append(", \"error\": \"").Append(Esc(r.Error)).Append('"');
                sb.Append(", \"screen\": [").Append(r.W).Append(", ").Append(r.H).Append(']');
                if (!float.IsNaN(r.Heading)) sb.Append(", \"faceHeading\": ").Append(F(r.Heading));
                sb.Append(", \"frames\": {\"count\": ").Append(r.Count).Append(", \"meanMs\": ").Append(F(r.Mean)).Append(", \"p99Ms\": ").Append(F(r.P99)).Append(", \"maxMs\": ").Append(F(r.Max)).Append('}')
                  .Append(", \"gpuMs\": ").Append(F(r.Gpu)).Append(", \"mainMs\": ").Append(F(r.Main)).Append(", \"renderMs\": ").Append(F(r.Render))
                  .Append(", \"counters\": {\"triangles\": ").Append(F(r.Tris)).Append(", \"setPass\": ").Append(F(r.SetPass)).Append(", \"shadowCasters\": ").Append(F(r.Casters)).Append('}')
                  .Append(", \"drawn\": {\"renderers\": ").Append(r.Drawn).Append(", \"beyond150\": ").Append(r.Beyond150).Append('}')
                  .Append(", \"grdRebuildMs\": ").Append(F(r.GrdMs)).Append('}');
            }
            sb.Append("\n ]\n}\n");
            try { File.WriteAllText(Path.Combine(dir, "tplong.json"), sb.ToString()); } catch (Exception e) { Debug.LogError("TpLongRun: tplong.json not written: " + e.Message); }
        }
    }
}
