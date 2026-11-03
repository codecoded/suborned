using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Suborned.Play;
using Suborned.Sim;
using Suborned.World.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using IOPath = System.IO.Path;

namespace Suborned.World.Review
{
    /// <summary>Brief 079 (Latch): the targeted check for play zoom 10, the getaway car's leaving and the café cut. Play mode on the built HarbourDistrict, opened and never saved. A route's orders are given
    /// at their ticks (RunTo, as FightReel078), then, paused, stills and what lies along the ray from the camera to each opposition figure in frame; on to the end, the car by the wall clock behind the
    /// end card; then the café and the Office front at 10 with the LOD picks, triangles and a render timing at 14 and 10.
    ///   -executeMethod Suborned.World.Review.Latch079.Run -script route.json -out dir -ticks 2704,3004 (batchmode, no -quit)</summary>
    [InitializeOnLoad]
    public static class Latch079
    {
        const string Key = "latch079";
        static readonly string[] Names = { "script", "out", "ticks", "mode" };
        static string Arg(string n) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
        static Latch079() { if (SessionState.GetBool(Key, false)) EditorApplication.update += Step; }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/HarbourDistrict.unity", OpenSceneMode.Single);
            var host = Object.FindFirstObjectByType<HandoverHost>(); if (!host) { Debug.LogError("latch079: no host"); EditorApplication.Exit(2); return; }
            if (!host.carFx) host.carFx = CarFxBuild.Ensure();
            host.startPaused = true;
            SessionState.SetBool(Key, true); SessionState.SetInt(Key + ".stage", 0);
            foreach (var n in Names) SessionState.SetString(Key + "." + n, Arg("-" + n) ?? "");
            EditorApplication.update += Step;
            EditorApplication.EnterPlaymode();
        }

        static void Step()
        {
            int stage = SessionState.GetInt(Key + ".stage", 0);
            if (EditorApplication.isPlaying)
            {
                if (stage == 0 && Object.FindFirstObjectByType<HandoverHost>() is { } h && h.Game != null)
                {
                    var s = new GameObject("latch079").AddComponent<Latch079Shots>();
                    s.A = Names.ToDictionary(n => n, n => SessionState.GetString(Key + "." + n, ""));
                    SessionState.SetInt(Key + ".stage", 1);
                }
                else if (stage == 1 && Latch079Shots.Finished) { SessionState.SetInt(Key + ".stage", 2); EditorApplication.ExitPlaymode(); }
                return;
            }
            if (stage != 2) return;
            EditorApplication.update -= Step; SessionState.EraseBool(Key); SessionState.SetInt(Key + ".stage", 3);
            if (Application.isBatchMode) EditorApplication.Exit(Latch079Shots.Failed ? 1 : 0);
        }
    }

    [DefaultExecutionOrder(32000)]
    public sealed class Latch079Shots : MonoBehaviour
    {
        const int W = 1920, H = 1080;
        public static bool Finished, Failed;
        public Dictionary<string, string> A;
        HandoverHost h; UnityEngine.Camera cam; RenderTexture hdr, ldr, hudRt; Texture2D tex, hudTex; List<Order> orders; int oi; string outDir; readonly StringBuilder rep = new();
        static readonly (string id, Vector3 f, int turn)[] Views = { ("cafe-y210", new Vector3(-170.5f, 0, -227.5f), 2), ("cafe-y030", new Vector3(-170.5f, 0, -227.5f), 0), ("office-y210", new Vector3(-160f, 0, -281f), 2), ("office-y030", new Vector3(-160f, 0, -281f), 0) };

        void Start() { Finished = Failed = false; StartCoroutine(Guarded(Go())); }

        IEnumerator Guarded(IEnumerator step)
        {
            while (true)
            {
                object cur; try { if (!step.MoveNext()) break; cur = step.Current; } catch (Exception e) { Log("THREW " + e); Failed = true; break; }
                yield return cur;
            }
            HandoverHost.BeforeStep = null; if (h && h.View) h.View.targetTexture = null; HudToolkit.HudInto(null);
            File.WriteAllText(IOPath.Combine(outDir ?? ".", "report.txt"), rep.ToString());
            Finished = true;
        }

        void Log(string s) { rep.AppendLine(s); Debug.Log("latch079: " + s); }
        static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        IEnumerator Go()
        {
            outDir = IOPath.GetFullPath(A["out"]); Directory.CreateDirectory(outDir);
            h = FindAnyObjectByType<HandoverHost>(); cam = h.View;
            for (int i = 0; i < 30; i++) yield return null;
            h.controls.conversations = ConversationScenes.Never;
            hdr = new RenderTexture(W, H, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { name = "l-hdr" };
            ldr = new RenderTexture(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "l-ldr" };
            hudRt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "l-hud" };
            tex = new Texture2D(W, H, TextureFormat.RGB24, false); hudTex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            cam.targetTexture = hdr; HudToolkit.HudInto(hudRt);
            h.Begin(); h.IntelOpen = false; h.SetSpeed(1);
            if (A["mode"] == "crossing") { yield return Crossing(); yield break; }
            { var noCut = new Dictionary<string, int>(); foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)) { if ((r.renderingLayerMask & ArchitectureFade.Layer) == 0) continue; foreach (var m in r.sharedMaterials) if (m && !m.shader.keywordSpace.FindKeyword(FadeReach.Keyword).isValid) { var k = $"{m.name} ({m.shader.name}, q{m.renderQueue})"; noCut.TryGetValue(k, out var c); noCut[k] = c + 1; } } Log("materials on tagged renderers with no cutting variant: " + (noCut.Count == 0 ? "none" : string.Join("; ", noCut.OrderByDescending(kv => kv.Value).Take(14).Select(kv => $"{kv.Key} x{kv.Value}")))); }
            Log($"opening: size {F(cam.orthographicSize)} (preset zoom {F(h.Cam.overhead.zoom)}), range {F(h.zoomMin)} to {F(h.zoomMax)}, build {BuildStamp.Line}");

            // the wheel's reach (the host's own method) and the double click's frame
            var wheel = typeof(HandoverHost).GetMethod("OverheadWheel", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < 80; i++) wheel.Invoke(h, new object[] { 1f }); float inMost = cam.orthographicSize;
            for (int i = 0; i < 80; i++) wheel.Invoke(h, new object[] { -1f }); float outMost = cam.orthographicSize;
            h.PortraitClick(0, false); h.PortraitClick(0, false);
            Log($"wheel in to {F(inMost)}, out to {F(outMost)}; double click on a portrait frames at {F(cam.orthographicSize)}");
            yield return null; for (int i = 0; i < 20; i++) yield return null;
            yield return Still("00-opening");

            var sc = Suborned.Sim.Script.Parse(File.ReadAllText(A["script"]));
            orders = sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
            h.StartClock(); h.SetPaused(true); h.TurnSnap(2); // the Records Office front's quarter, as the fight video
            HandoverHost.BeforeStep = host => { while (oi < orders.Count && orders[oi].Tick <= host.Game.Tick && !host.Game.Over) Give(orders[oi++]); };
            var ticks = A["ticks"].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            foreach (var t in ticks)
            {
                RunTo(t); h.SetPaused(true); h.LookAt(Fighters());
                for (int i = 0; i < 20; i++) yield return null;
                Log($"--- tick {h.Game.Tick}: fade points {h.FadePointCount}, uncut silhouettes [{h.UncutSilhouettePeople()}], over {h.Game.Over}");
                Rays();
                yield return Still($"10-fight-tick{h.Game.Tick}");
                if (t == ticks[ticks.Length - 1])
                {   // the same moment at 14: the video's framing
                    float was = cam.orthographicSize; cam.orthographicSize = 14f; for (int i = 0; i < 6; i++) yield return null; yield return Still($"11-fight-tick{h.Game.Tick}-z14"); Rays(); cam.orthographicSize = was; yield return null;
                }
            }

            // on to the end: the win, the car behind the end card
            var carT = h.Views.Find("car/getaway"); h.SetPaused(false);
            while (!h.Game.Over && h.Game.Tick < 5100) { if (h.Paused) h.SetPaused(false); h.LookAt(Fighters()); yield return null; }
            for (int i = 0; i < 3; i++) yield return null;
            Log($"--- over at tick {h.Game.Tick}: {h.Game.Result}, CarLeftTick {h.Game.CarLeftTick}, engine car {h.Game.GetawayCarPos().X},{h.Game.GetawayCarPos().Y} (never moves: the engine stops at Go), hash {Suborned.Sim.Hash.Hex(h.Game.Checkpoints[h.Game.Checkpoints.Count - 1].hash)}; ending [{h.Ending}]");
            float t0 = Time.realtimeSinceStartup; var seen = new List<string>(); var marks = new[] { 0.4f, 2f, 4f, 7f, 14f }; int mi = 0; var last = carT.position; float speedMax = 0f, lastT = t0;
            while (mi < marks.Length)
            {
                yield return null; float now = Time.realtimeSinceStartup - t0;
                bool on = carT && carT.gameObject.activeSelf; if (on) { float dt = Time.realtimeSinceStartup - lastT; if (dt > 0f) speedMax = Mathf.Max(speedMax, (carT.position - last).magnitude / dt); last = carT.position; lastT = Time.realtimeSinceStartup; }
                if (now >= marks[mi])
                {
                    seen.Add($"{F(marks[mi])} s: {(on ? $"drawn at {F(carT.position.x)},{F(carT.position.z)}" : "gone")}, left {F(h.Views.GetawayLeftM)} m");
                    yield return Still($"20-end-{marks[mi]:00.0}s");
                    mi++;
                }
            }
            Log("car behind the end card: " + string.Join(" | ", seen) + $"; top {F(speedMax)} m/s (wall clock), ops in car {h.Game.Ops.Count(o => o.InCar)}");
            Log($"fade points at the end {h.FadePointCount}");

            // replay: Begin again, the car stands at its bay, and comes round to it again at the same hash
            ulong hash0 = h.Game.Checkpoints[h.Game.Checkpoints.Count - 1].hash; int over0 = h.Game.Tick;
            h.Begin(); h.IntelOpen = false; h.SetSpeed(1); oi = 0; yield return null; yield return null;
            carT = h.Views.Find("car/getaway");
            Log($"replay: after Begin the car drawn {(carT.gameObject.activeSelf ? "at " + F(carT.position.x) + "," + F(carT.position.z) : "gone")}, left {F(h.Views.GetawayLeftM)} m, size {F(cam.orthographicSize)}");
            h.StartClock(); h.SetPaused(true); RunTo(over0 - 2); h.SetPaused(false);
            while (!h.Game.Over && h.Game.Tick < 5100) { if (h.Paused) h.SetPaused(false); yield return null; }
            for (int i = 0; i < 6; i++) yield return null; float t1 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t1 < 2f) yield return null;
            Log($"replay: over at tick {h.Game.Tick}, hash same {h.Game.Checkpoints[h.Game.Checkpoints.Count - 1].hash == hash0}, left {F(h.Views.GetawayLeftM)} m after 2 s");
            yield return Still("21-replay-end-2s");

            // the street at 10, cost at 14 and 10 (a fresh run: the opening's people and no fight)
            HandoverHost.BeforeStep = null; h.Begin(); h.IntelOpen = false; yield return null; h.StartClock(); h.SetPaused(true); h.RunTo(300);
            foreach (var v in Views)
            {
                h.TurnSnap(v.turn); h.LookAt(v.f); for (int i = 0; i < 20; i++) yield return null;
                yield return Still("30-" + v.id);
                Cost(v.id);
            }
        }

        /// <summary>Brief 079 note 2: the two crossings by the café and the Office at zoom 10, at both quarter turns, in stop, walk and clearing (SignalHead.Hold, as the shots do), and the pieces' materials as built.</summary>
        IEnumerator Crossing()
        {
            h.StartClock(); h.SetPaused(true); h.RunTo(300); cam.orthographicSize = 10f;
            Log($"size {F(cam.orthographicSize)}, heads {SignalHead.All.Count}");
            var seen = new HashSet<string>();
            foreach (var (id, crossing) in new[] { ("cafe", "crossing/starline-street/0"), ("office", "crossing/orbit-walk/0") })
            {
                var head = SignalHead.All.First(x => x.Crossing == crossing && x.Dots.Length > 0);
                var b = head.Dots[0].bounds; foreach (var d in head.Dots) if (d) b.Encapsulate(d.bounds);
                Log($"{id}: {head.Crossing} at {F(b.center.x)},{F(b.center.z)} size {F(b.size.x)} x {F(b.size.z)}, {head.Dots.Length} modules, cross length {F(head.CrossLength)}");
                foreach (var r in new[] { head.Rim, head.Ring, head.Beam, head.Halo, head.Dots[0] }) if (r) foreach (var m in r.sharedMaterials) if (m && seen.Add(m.name)) Log($"  {r.name}: {m.name} ({m.shader.name}, q{m.renderQueue}{(m.HasProperty("_Mode") ? ", mode " + m.GetFloat("_Mode") : "")})");
                foreach (var d in head.Dots) foreach (var m in d.sharedMaterials) if (m && seen.Add(m.name)) Log($"  {d.name}: {m.name} ({m.shader.name}, q{m.renderQueue})");
                foreach (var turn in id == "cafe" ? new[] { 1, 3 } : new[] { 0, 2 }) // the café itself or a roof hides the café crossing from 30 and 210
                {
                    h.TurnSnap(turn); h.LookAt(new Vector3(b.center.x, 0f, b.center.z));
                    foreach (var st in new[] { "stop", "walk", "clearing" })
                    {
                        SignalHead.Hold = st; SignalHead.HoldLeft = 5f; for (int i = 0; i < 24; i++) yield return null;
                        Log($"  {id} turn {turn} {st}: head state {head.State}, ring lit {head.RingLit}");
                        yield return Still($"{id}-y{30 + 90 * turn:000}-{st}");
                    }
                }
            }
            SignalHead.Hold = "";
        }

        void RunTo(int t)
        {
            while (oi < orders.Count && orders[oi].Tick < t && !h.Game.Over) { var o = orders[oi++]; if (o.Tick > h.Game.Tick) h.RunTo(o.Tick); if (h.Game.Over) break; Give(o); }
            if (t > h.Game.Tick && !h.Game.Over) h.RunTo(t);
        }

        void Give(Order o)
        {
            var c = new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run, Stance = o.Stance, Force = o.Force, On = o.On, Release = o.Release, Face = o.Face, Count = o.Count, Shots = o.Shots, Then = o.Then };
            if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c);
        }

        Vector3 Fighters()
        {
            int t = h.Game.Tick; string[] who = t < 2286 ? new[] { "scout", "fixer", "tech" } : t < 2614 ? new[] { "scout", "fixer" } : t < 2966 ? new[] { "enforcer" } : new[] { "scout", "fixer", "tech", "enforcer" };
            var sum = Vector3.zero; int n = 0; foreach (var w in who) if (h.Views.Find("op/" + w) is Transform x && x) { sum += x.position; n++; }
            return n > 0 ? sum / n : cam.transform.position;
        }

        /// <summary>Everything drawn along the ray from the camera to each person in the cut list in frame, nearer the camera than he is: what could show in his cut.</summary>
        void Rays()
        {
            var rs = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            foreach (var (key, at) in h.Views.SilhouettePeople().Concat(new[] { "hostile/" }.SelectMany(_ => Enumerable.Empty<(string, Vector3)>())))
            {
                var vp = cam.WorldToViewportPoint(at); if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) continue;
                var ray = new Ray(at + Vector3.up * 1f - cam.transform.forward * 400f, cam.transform.forward); var sb = new StringBuilder($"  {key} at {F(at.x)},{F(at.z)} screen {F(vp.x * W)},{F(vp.y * H)}:");
                foreach (var r in rs)
                {
                    if (!r.enabled || !r.bounds.IntersectRay(ray, out var d) || d > 400f) continue;
                    if (r is SkinnedMeshRenderer || r is ParticleSystemRenderer || r is LineRenderer) continue;
                    foreach (var m in r.sharedMaterials) if (m && m.renderQueue >= 2500 || (r.renderingLayerMask & ArchitectureFade.Layer) != 0) { sb.Append($"\n     {PathOf(r.transform)} [{(r.renderingLayerMask & ArchitectureFade.Layer) != 0}] {string.Join(",", r.sharedMaterials.Where(x => x).Select(x => $"{x.name}({x.shader.name},q{x.renderQueue},cut {x.IsKeywordEnabled(FadeReach.Keyword)})"))}"); break; }
                }
                Log(sb.ToString());
            }
        }
        static string PathOf(Transform t) { var n = t.name; int k = 0; for (var q = t.parent; q && k < 4; q = q.parent, k++) n = q.name + "/" + n; return n; }

        IEnumerator Still(string name)
        {
            yield return null; yield return null;
            cam.targetTexture = hdr; cam.Render(); Graphics.Blit(hdr, ldr);
            var prev = RenderTexture.active; RenderTexture.active = ldr; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            var buf = tex.GetRawTextureData<byte>().ToArray();
            RenderTexture.active = hudRt; hudTex.ReadPixels(new Rect(0, 0, W, H), 0, 0); var b = hudTex.GetRawTextureData<Color32>();
            for (int i = 0; i < b.Length; i++) { int al = b[i].a; if (al == 0) continue; int j = i * 3, ia = 255 - al; buf[j] = (byte)((b[i].r * al + buf[j] * ia) / 255); buf[j + 1] = (byte)((b[i].g * al + buf[j + 1] * ia) / 255); buf[j + 2] = (byte)((b[i].b * al + buf[j + 2] * ia) / 255); }
            RenderTexture.active = prev;
            var t = new Texture2D(W, H, TextureFormat.RGB24, false); t.LoadRawTextureData(buf); t.Apply(false);
            File.WriteAllBytes(IOPath.Combine(outDir, name + ".png"), t.EncodeToPNG()); Destroy(t);
        }

        /// <summary>LOD picks, triangles in the frustum and a timed render (the GPU waited for by a read back) at 14 and 10, on this machine: a comparison, not the Air's number.</summary>
        void Cost(string id)
        {
            float was = cam.orthographicSize; var groups = FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude);
            var pick = new Dictionary<float, Dictionary<LODGroup, int>>();
            foreach (var z in new[] { 14f, 10f })
            {
                cam.orthographicSize = z; var planes = GeometryUtility.CalculateFrustumPlanes(cam); var inLod = new HashSet<Renderer>(); var byLevel = new SortedDictionary<int, (int n, long tris)>(); var pk = new Dictionary<LODGroup, int>(); long lodTris = 0, plain = 0; int plainN = 0;
                foreach (var g in groups)
                {
                    var lods = g.GetLODs(); float rel = g.size * MaxScale(g.transform) / (2f * z) * QualitySettings.lodBias; int lv = lods.Length; for (int i = 0; i < lods.Length; i++) if (rel >= lods[i].screenRelativeTransitionHeight) { lv = i; break; }
                    foreach (var l in lods) foreach (var r in l.renderers) if (r) inLod.Add(r);
                    var wb = new Bounds(g.transform.TransformPoint(g.localReferencePoint), Vector3.one * g.size * MaxScale(g.transform)); if (!GeometryUtility.TestPlanesAABB(planes, wb)) continue;
                    pk[g] = lv; long tr = 0; if (lv < lods.Length) foreach (var r in lods[lv].renderers) tr += Tris(r); lodTris += tr; byLevel.TryGetValue(lv, out var c); byLevel[lv] = (c.n + 1, c.tris + tr);
                }
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude)) { if (!r.enabled || inLod.Contains(r) || !GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue; plain += Tris(r); plainN++; }
                pick[z] = pk;
                var times = new List<double>(); var one = new Rect(0, 0, 1, 1); var px = new Texture2D(1, 1, TextureFormat.RGB24, false);
                for (int i = 0; i < 24; i++) { var sw = System.Diagnostics.Stopwatch.StartNew(); cam.Render(); Graphics.Blit(hdr, ldr); var prev = RenderTexture.active; RenderTexture.active = ldr; px.ReadPixels(one, 0, 0); RenderTexture.active = prev; sw.Stop(); if (i >= 6) times.Add(sw.Elapsed.TotalMilliseconds); }
                Destroy(px); times.Sort();
                Log($"{id} zoom {z}: LODGroups in frustum {pk.Count}, by level {string.Join(" ", byLevel.Select(kv => $"L{kv.Key}:{kv.Value.n}/{kv.Value.tris}t"))}; lod tris {lodTris}, plain renderers {plainN}/{plain}t, total {lodTris + plain}; render+readback median {times[times.Count / 2]:0.0} ms (p10 {times[times.Count / 10]:0.0})");
            }
            cam.orthographicSize = was;
            var moved = pick[10f].Where(kv => pick[14f].TryGetValue(kv.Key, out var v14) && v14 != kv.Value).ToList();
            var names = moved.GroupBy(kv => Root(kv.Key.name)).OrderByDescending(x => x.Count()).Take(12).Select(x => $"{x.Key} x{x.Count()} ({string.Join(",", x.Select(k => $"L{pick[14f][k.Key]}>L{k.Value}").Distinct())})");
            Log($"{id}: in both frustums {pick[10f].Count(kv => pick[14f].ContainsKey(kv.Key))}, level changed by 14 to 10: {moved.Count}: {string.Join("; ", names)}");
            var plants = pick[10f].Where(kv => Mathf.Abs(kv.Key.size - 4f) < 0.01f).ToList();
            Log($"{id}: lodSize 4 groups (the planting) in frustum at 10: {plants.Count}, levels {string.Join(" ", plants.GroupBy(kv => kv.Value).OrderBy(x => x.Key).Select(x => $"L{x.Key}:{x.Count()}"))}, half-heights {(plants.Count > 0 ? string.Join(" / ", plants[0].Key.GetLODs().Select(l => F(plants[0].Key.size / (2f * l.screenRelativeTransitionHeight)))) : "-")}");
        }
        static string Root(string n) { int i = n.IndexOf(' '); n = i > 0 ? n[..i] : n; i = n.IndexOf('('); return i > 0 ? n[..i] : n; }
        static float MaxScale(Transform t) { var s = t.lossyScale; return Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)); }
        static long Tris(Renderer r) { var f = r.GetComponent<MeshFilter>(); var m = f ? f.sharedMesh : null; if (!m) return 0; long c = 0; for (int s = 0; s < m.subMeshCount; s++) c += m.GetIndexCount(s) / 3; return c; }
    }
}
