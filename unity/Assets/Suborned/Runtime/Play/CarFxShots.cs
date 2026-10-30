using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Brief 078: the car effects proved and pictured, in the fight range or the Handover (Mode). The cell shoots a car by the host's
    /// own orders until it is disabled, then burning (pictures at play zoom 14 and close), then the blast drawn on a fixed list of mission times
    /// (CarFxPool.TimeOverride, the sim stepped to each: a contact sheet at play zoom and close), then a second identical run drawn every 1/30 s
    /// for the mp4 frames, which also checks that the second run's particles at blast + 1 s are the first run's (the replay), that a pause holds
    /// them still, and reads the counts and the fill (fx.json). Frames: Camera.Render into ARGBHalf, a Blit to sRGB, ReadPixels; the HUD layer
    /// drawn into its own texture and laid over (HudToolkit.HudInto). Presentation only: nothing is written to the engine but the orders.</summary>
    public sealed class CarFxShots : MonoBehaviour
    {
        public string Dir = "", Mode = "range";
        public static bool Finished;
        public int W = 1920, H = 1080;
        readonly List<string> log = new(), parts = new();
        bool ok = true;
        RenderTexture hdr, ldr, hudRt; Texture2D tex, hudTex;

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "'").Replace("\n", " ");
        void Check(string name, bool held, string detail) { ok &= held; parts.Add($"\"{name}\": {{\"ok\": {(held ? "true" : "false")}, \"detail\": \"{Esc(detail)}\"}}"); log.Add($"{name}: {(held ? "ok" : "FAILED")} · {detail}"); Debug.Log($"carfx: {name}: {(held ? "ok" : "FAILED")} · {detail}"); }

        static readonly float[] Sheet = { -0.3f, 0f, 0.033f, 0.067f, 0.1f, 0.15f, 0.2f, 0.3f, 0.45f, 0.6f, 0.8f, 1.0f, 1.3f, 1.7f, 2.2f, 3.0f, 4.5f, 7f, 12f, 20f };

        IEnumerator Start()
        {
            Finished = false; Directory.CreateDirectory(Dir);
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 120f) { Write("no host"); Finished = true; yield break; } yield return null; }
            for (int i = 0; i < 30; i++) yield return null;
            hdr = new RenderTexture(W, H, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { name = "fx-hdr" };
            ldr = new RenderTexture(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "fx-ldr" };
            hudRt = new RenderTexture(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "fx-hud" };
            tex = new Texture2D(W, H, TextureFormat.RGB24, false); hudTex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            if (h.View) h.View.targetTexture = hdr; // the HUD projects onto the picture's size
            bool hud = HudToolkit.HudInto(hudRt);
            log.Add($"mode {Mode}, screen {Screen.width}x{Screen.height}, picture {W}x{H}, hud layer {hud}, prefab {(h.carFx ? h.carFx.name : "none (made in code)")}");
            IEnumerator Guarded(IEnumerator step)
            {
                while (true)
                {
                    object cur; try { if (!step.MoveNext()) yield break; cur = step.Current; } catch (Exception e) { ok = false; log.Add("step threw: " + e); Debug.LogException(e); yield break; }
                    yield return cur;
                }
            }
            yield return Guarded(Run(h));
            CarFxPool.TimeOverride = -1f; CarFxPool.MaxStepsPerFrame = 16;
            if (h.View) h.View.targetTexture = null; HudToolkit.HudInto(null);
            Write(ok ? "ok" : "failed"); Finished = true;
        }

        // ------------------------------------------------------------------ the scenario

        string carId = "", runTag = "";
        int blastTick = -1;
        List<Vector3> snapA; string descA = "";

        IEnumerator Run(HandoverHost h)
        {
            runTag = Mode == "handover" ? "handover" : "range";
            // run 1: disabled, burning, the blast's sheet
            yield return Setup(h); if (carId.Length == 0) yield break;
            var g = h.Game;
            yield return FireUntil(h, () => g.CarDisabled(carId), 2400, "disabled");
            Cease(h); h.RunTo(g.Tick + 25); yield return Frames(4);
            yield return Pair(h, $"{runTag}-disabled");
            Stats(h, "disabled");
            yield return FireUntil(h, () => g.CarBurning(carId) || (g.CarOf(carId)?.Blown ?? false), 2400, "burning");
            Cease(h); var c = g.CarOf(carId); if (c == null || c.BurningAt < 0) { Check("burning", false, "the car never burned"); yield break; }
            blastTick = c.BlastAt; int burnTicks = c.BlastAt - c.BurningAt;
            h.RunTo(Mathf.Min(c.BurningAt + Mathf.Max(5, burnTicks * 6 / 10), c.BlastAt - 4)); yield return Frames(4);
            yield return Pair(h, $"{runTag}-burning");
            Stats(h, "burning");
            Check("states", c.DisabledAt >= 0 && c.BurningAt >= c.DisabledAt, $"{carId}: disabled at tick {c.DisabledAt}, burning at {c.BurningAt}, blast at {c.BlastAt} (burn {burnTicks / (float)Game.Hz:0.0} s, blast {g.R.CarBlastCm / 100f} m)");
            // the sheet: each mission time drawn exactly (every 1/60 s step simulated)
            CarFxPool.MaxStepsPerFrame = 100000;
            var playTiles = new List<Texture2D>(); var closeTiles = new List<Texture2D>(); var peak = new List<string>();
            foreach (var off in Sheet)
            {
                int tick = blastTick + Mathf.FloorToInt(off * Game.Hz + 1e-3f); if (tick > g.Tick) h.RunTo(tick);
                CarFxPool.TimeOverride = blastTick / (float)Game.Hz + off; yield return Frames(3);
                Aim(h, 14f); yield return Frames(2); playTiles.Add(Tile(Grab(h, true), 480, 270));
                if (Mathf.Abs(off - 1.0f) < 1e-3f) { snapA = h.CarFxPool.Snapshot(); descA = h.CarFxPool.Describe(); }
                if (Mathf.Abs(off - 0.1f) < 1e-3f || Mathf.Abs(off - 0.3f) < 1e-3f || Mathf.Abs(off - 1f) < 1e-3f || Mathf.Abs(off - 4.5f) < 1e-3f || Mathf.Abs(off - 20f) < 1e-3f) { Save(Path.Combine(Dir, $"{runTag}-blast+{F(off)}s-play.png")); }
                Aim(h, 6.5f); yield return Frames(2); closeTiles.Add(Tile(Grab(h, true), 480, 270));
                if (Mathf.Abs(off - 0.1f) < 1e-3f || Mathf.Abs(off - 0.3f) < 1e-3f || Mathf.Abs(off - 1f) < 1e-3f || Mathf.Abs(off - 4.5f) < 1e-3f || Mathf.Abs(off - 20f) < 1e-3f) { Save(Path.Combine(Dir, $"{runTag}-blast+{F(off)}s-close.png")); }
                peak.Add(Stats(h, $"blast+{F(off)}s"));
            }
            SheetPng(playTiles, Path.Combine(Dir, $"{runTag}-blast-sheet-play.png")); SheetPng(closeTiles, Path.Combine(Dir, $"{runTag}-blast-sheet-close.png"));
            parts.Add($"\"sheetTimes\": [{string.Join(", ", Sheet.Select(F))}]");
            CarFxPool.TimeOverride = -1f;

            // run 2: the same orders from the start, drawn every 1/30 s for the reel (two grid steps a frame)
            yield return Setup(h); if (carId.Length == 0) yield break;
            g = h.Game;
            yield return FireUntil(h, () => g.CarDisabled(carId), 2400, "disabled (run 2)");
            Cease(h); h.RunTo(g.Tick + 25);
            yield return FireUntil(h, () => g.CarBurning(carId) || (g.CarOf(carId)?.Blown ?? false), 2400, "burning (run 2)");
            Cease(h); c = g.CarOf(carId);
            Check("replayTicks", c != null && c.BlastAt == blastTick, $"run 2 blows at tick {c?.BlastAt} (run 1: {blastTick}): the same orders, the same engine");
            if (c == null) yield break;
            string frames = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "out", "scratch", "latch", "078-frames", runTag)); // never under briefs (big)
            if (Directory.Exists(frames)) Directory.Delete(frames, true); Directory.CreateDirectory(frames + "/play"); Directory.CreateDirectory(frames + "/close");
            float blastS = c.BlastAt / (float)Game.Hz; int k0r = -Mathf.CeilToInt((c.BlastAt - c.BurningAt) / (float)Game.Hz * 30f) + 15, k1r = 9 * 30; // frames on a 1/30 s grid from the blast
            float from = blastS + k0r / 30f, to = blastS + k1r / 30f; int n = 0; bool replayDone = false;
            for (int kf = k0r; kf <= k1r; kf++, n++)
            {
                float t = blastS + kf / 30f;
                int tick = Mathf.FloorToInt(t * Game.Hz + 1e-3f); if (tick > g.Tick) h.RunTo(tick);
                CarFxPool.TimeOverride = t; yield return Frames(1);
                Aim(h, 14f); Grab(h, true); File.WriteAllBytes($"{frames}/play/{n:0000}.jpg", tex.EncodeToJPG(93));
                Aim(h, 6.5f); Grab(h, true); File.WriteAllBytes($"{frames}/close/{n:0000}.jpg", tex.EncodeToJPG(93));
                if (!replayDone && kf == 30)
                {
                    replayDone = true; var snapB = h.CarFxPool.Snapshot(); float dev = Dev(snapA, snapB);
                    Check("replay", snapA != null && snapA.Count == snapB.Count && dev < 1e-4f, $"particles at blast + 1 s: run 1 {snapA?.Count} (stepped by the sheet's times), run 2 {snapB.Count} (stepped every 1/30 s); largest gap {dev:0.000000} m; run 1 {descA}; run 2 {h.CarFxPool.Describe()}");
                }
            }
            // the real pause: the override off, the host paused; 40 frames later nothing has moved
            CarFxPool.TimeOverride = -1f; CarFxPool.MaxStepsPerFrame = 16; h.SetPaused(true); yield return Frames(2); h.CarFxPool.ResetPeak();
            float clock0 = h.CarFxClock; var p0 = h.CarFxPool.Snapshot(); yield return Frames(40); var p1 = h.CarFxPool.Snapshot();
            Check("pause", p0.Count > 0 && Dev(p0, p1) == 0f && h.CarFxClock == clock0, $"paused 40 frames: clock {F(clock0)} → {F(h.CarFxClock)}, {p0.Count} particles, largest move {Dev(p0, p1):0.000000} m");
            // on at play speed 1, then 4: the effects' clock follows the ticks
            int k0 = g.Tick; h.SetSpeed(1); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.4f);
            float r0 = Time.realtimeSinceStartup, c0 = h.CarFxClock; yield return new WaitForSecondsRealtime(1.5f); float rate1 = (h.CarFxClock - c0) / (Time.realtimeSinceStartup - r0);
            h.SetSpeed(4); yield return new WaitForSecondsRealtime(0.3f); r0 = Time.realtimeSinceStartup; c0 = h.CarFxClock; yield return new WaitForSecondsRealtime(1.5f); float rate4 = (h.CarFxClock - c0) / (Time.realtimeSinceStartup - r0);
            h.SetPaused(true); h.SetSpeed(1); var p2 = h.CarFxPool.Snapshot();
            Check("speed", rate1 > 0.7f && rate1 < 1.3f && rate4 > 3f && rate4 < 5f && Dev(p1, p2) > 0f, $"mission seconds per real second: {F(rate1)} at speed 1, {F(rate4)} at speed 4 (ticks {k0} → {g.Tick}); the smoke moved once the clock ran; live-stepping peak {F((float)h.CarFxPool.PeakMs)} ms, {h.CarFxPool.PeakSteps} steps in a frame");
            parts.Add($"\"reelFrames\": {n}, \"reelFrom\": {F(from)}, \"reelTo\": {F(to)}, \"framesDir\": \"{Esc(frames)}\"");
            log.Add($"reel: {n} frames each zoom in {frames}");
            CarFxPool.TimeOverride = -1f;
        }

        static float Dev(List<Vector3> a, List<Vector3> b)
        {
            if (a == null || b == null || a.Count != b.Count) return float.PositiveInfinity;
            float m = 0f; for (int i = 0; i < a.Count; i++) m = Mathf.Max(m, (a[i] - b[i]).magnitude); return m;
        }

        IEnumerator Setup(HandoverHost h)
        {
            CarFxPool.TimeOverride = -1f; CarFxPool.MaxStepsPerFrame = 100000; carId = "";
            if (Mode == "range")
            {
                h.rangeLayout = "open"; h.Begin(); h.SetPaused(true); yield return Frames(2); h.StartClock(); h.SetPaused(true); if (h.View) h.View.orthographicSize = 14f;
                var g = h.Game; bool sp = h.Issue(new Order { Do = "spawn", Target = "car", To = "car:1", Count = 1, With = "" }); h.RunTo(g.Tick + 1);
                var car = g.RangeCars().FirstOrDefault(); if (car == null) { Check("spawn", false, $"no car (spawn {sp}: {h.Status})"); yield break; }
                carId = car.Id;
                foreach (var a in new[] { "scout", "fixer", "enforcer" }) h.Issue(new Order { Op = a, Do = "draw" });
                h.RunTo(g.Tick + 10);
            }
            else
            {
                h.Begin(); h.SetPaused(true); yield return Frames(2); h.StartClock(); h.SetPaused(true); yield return null;
                var g = h.Game; const int E = (int)OpId.Enforcer, S = (int)OpId.Scout, X = (int)OpId.Fixer; const string T = "car:official";
                h.Issue(new Order { Op = "cell", Do = "free", On = false });
                var cp = g.CarAt(T); if (!cp.HasValue) { Check("spawn", false, "no official car on the street"); yield break; }
                int stand = Mathf.Min(1300, g.R.CarBlastCm + 450);
                ProofFire.ApproachInSight(h, E, cp.Value, stand); Arrive(h, 1500);
                ProofFire.ApproachInSight(h, S, cp.Value, stand + 150); Arrive(h, 1500);
                ProofFire.ApproachInSight(h, X, cp.Value, stand + 250); Arrive(h, 1500);
                foreach (var k in new[] { E, S, X }) h.Issue(new Order { Op = Suborned.Sim.Op.Name((OpId)k), Do = "draw" }); h.RunTo(g.Tick + 10);
                carId = T;
                log.Add($"handover: tick {g.Tick}, enforcer {g.S.Sight(g.Ops[E].Pos, cp.Value, g.R.Eye)}, scout {g.S.Sight(g.Ops[S].Pos, cp.Value, g.R.Eye)}, fixer {g.S.Sight(g.Ops[X].Pos, cp.Value, g.R.Eye)} in sight of the car");
            }
            h.Select(OpId.Enforcer, false);
        }

        static void Arrive(HandoverHost h, int cap) { for (int i = 0; i < cap && !h.Game.Over; i++) { bool any = false; for (int k = 0; k < 4; k++) any |= h.Game.Ops[k].Walk != null; if (!any) break; h.RunTo(h.Game.Tick + 1); } }

        IEnumerator FireUntil(HandoverHost h, Func<bool> done, int cap, string what)
        {
            var g = h.Game; int t0 = g.Tick;
            foreach (var a in new[] { "scout", "fixer", "enforcer" }) h.Issue(new Order { Op = a, Do = "fire", Target = carId });
            for (int i = 0; i < cap && !done() && !g.Over; i++) { h.RunTo(g.Tick + 1); if (i % 50 == 0) yield return null; }
            log.Add($"{what}: {(done() ? "reached" : "NOT reached")} at tick {g.Tick} ({(g.Tick - t0) / (float)Game.Hz:0.0} s of fire){(g.Over ? ", the game is over: " + g.Result : "")}");
            if (!done()) Check(what.Replace(" ", ""), false, $"not reached in {cap} ticks");
        }

        static void Cease(HandoverHost h)
        {
            foreach (var a in new[] { "scout", "fixer", "enforcer" })
                if (h.Game.OneInputOn) h.Issue(new Order { Op = a, Do = "holster" }); else h.Issue(new Order { Op = a, Do = "cease" });
            h.RunTo(h.Game.Tick + 1);
        }

        float aimSize = 14f; Vector3? aimAt;
        /// <summary>The framing for the next grabs: the half-height `ortho` (m) about the car (the host's own camera code keeps its zoom, so the
        /// grab sets this on the camera for its render and puts it back).</summary>
        void Aim(HandoverHost h, float ortho)
        {
            var at = h.Views.Find("car/" + carId.Substring(4)); aimSize = ortho; aimAt = at ? at.position + Vector3.up * 0.8f : null;
            if (at) h.LookAt(at.position);
        }

        IEnumerator Pair(HandoverHost h, string name)
        {
            Aim(h, 14f); yield return Frames(3); Grab(h, true); Save(Path.Combine(Dir, name + "-play.png"));
            Aim(h, 6.5f); yield return Frames(3); Grab(h, true); Save(Path.Combine(Dir, name + "-close.png"));
            Aim(h, 14f);
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        // ------------------------------------------------------------------ frames

        /// <summary>The camera into ARGBHalf (the grade sees HDR, as the player's screen path), sRGB by a Blit, read back; the HUD layer over it.</summary>
        Texture2D Grab(HandoverHost h, bool withHud)
        {
            var cam = h.View; var prev = RenderTexture.active;
            var pos0 = cam.transform.position; float size0 = cam.orthographicSize;
            if (aimAt is Vector3 f)
            {
                if (cam.orthographic) { cam.orthographicSize = aimSize; cam.transform.position = f - cam.transform.forward * Vector3.Dot(pos0 - f, -cam.transform.forward); }
                else cam.transform.position = f - cam.transform.forward * (aimSize / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            }
            cam.targetTexture = hdr; cam.Render(); Graphics.Blit(hdr, ldr);
            cam.transform.position = pos0; cam.orthographicSize = size0;
            RenderTexture.active = ldr; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(false);
            if (withHud && aimSize >= 13f) // the HUD layer is drawn for the host's own framing (play zoom): close grabs are the world only
            {
                RenderTexture.active = hudRt; hudTex.ReadPixels(new Rect(0, 0, W, H), 0, 0); hudTex.Apply(false);
                var a = tex.GetPixels32(); var b = hudTex.GetPixels32(); long any = 0;
                for (int i = 0; i < a.Length; i++)
                {
                    int al = b[i].a; if (al == 0) continue; any++;
                    a[i] = new Color32((byte)((b[i].r * al + a[i].r * (255 - al)) / 255), (byte)((b[i].g * al + a[i].g * (255 - al)) / 255), (byte)((b[i].b * al + a[i].b * (255 - al)) / 255), 255);
                }
                if (any > 0) { tex.SetPixels32(a); tex.Apply(false); }
                hudPixels = any;
            }
            RenderTexture.active = prev;
            return tex;
        }
        long hudPixels;

        void Save(string path) { File.WriteAllBytes(path, tex.EncodeToPNG()); log.Add($"{Path.GetFileName(path)} (hud px {hudPixels})"); }

        static Texture2D Tile(Texture2D src, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var t = new Texture2D(src.width, src.height, TextureFormat.RGB24, false); t.SetPixels32(src.GetPixels32()); t.Apply(false);
            Graphics.Blit(t, rt); var prev = RenderTexture.active; RenderTexture.active = rt;
            var o = new Texture2D(w, h, TextureFormat.RGB24, false); o.ReadPixels(new Rect(0, 0, w, h), 0, 0); o.Apply(false);
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt); Destroy(t); return o;
        }

        static void SheetPng(List<Texture2D> tiles, string path)
        {
            if (tiles.Count == 0) return; int cols = 5, rows = (tiles.Count + cols - 1) / cols, tw = tiles[0].width, th = tiles[0].height, gap = 4;
            var s = new Texture2D(cols * tw + (cols + 1) * gap, rows * th + (rows + 1) * gap, TextureFormat.RGB24, false);
            var bg = new Color32[s.width * s.height]; for (int i = 0; i < bg.Length; i++) bg[i] = new Color32(23, 48, 47, 255); s.SetPixels32(bg);
            for (int i = 0; i < tiles.Count; i++) { int cx = i % cols, cy = rows - 1 - i / cols; s.SetPixels32(gap + cx * (tw + gap), gap + cy * (th + gap), tw, th, tiles[i].GetPixels32()); }
            s.Apply(false); File.WriteAllBytes(path, s.EncodeToPNG()); Destroy(s); foreach (var t in tiles) Destroy(t);
        }

        // ------------------------------------------------------------------ counts and fill

        readonly List<(Vector3 at, float size, bool additive)> sizes = new();
        string Stats(HandoverHost h, string at)
        {
            var p = h.CarFxPool; if (p == null) return "";
            p.Sizes(sizes); var cam = h.View; float pxPerM = H / (2f * 14f); double fill = 0, add = 0; // at play zoom (ortho 14): a billboard's side in pixels is size × px a metre
            foreach (var s in sizes) { double px = s.size * pxPerM; fill += px * px; if (s.additive) add += px * px; }
            string row = $"{{\"at\": \"{at}\", \"rigs\": {p.LiveRigs}, \"particles\": {p.LiveParticles}, \"lights\": {p.LiveLights}, \"cpuMsPeak\": {F((float)p.PeakMs)}, \"stepsPeak\": {p.PeakSteps}, \"seeks\": {p.SeeksLastFrame}, \"fillPx14\": {fill:0}, \"fillScreens14\": {F((float)(fill / (W * (double)H)))}, \"additivePx14\": {add:0}}}";
            rows.Add(row); p.ResetPeak(); return row;
        }
        readonly List<string> rows = new();

        void Write(string status)
        {
            var body = $"{{\n \"schema\": \"suborned.carfx-shots/1\",\n \"status\": \"{Esc(status)}\",\n \"mode\": \"{Mode}\",\n \"build\": \"{Esc(BuildStamp.Line)}\",\n \"maxLive\": {CarFxPool.MaxLive},\n \"stepS\": {F(CarFxPool.Step)},\n \"log\": [{string.Join(", ", log.Select(l => "\"" + Esc(l) + "\""))}],\n \"stats\": [\n  {string.Join(",\n  ", rows)}\n ]{(parts.Count > 0 ? ",\n " + string.Join(",\n ", parts) : "")}\n}}\n";
            File.WriteAllText(Path.Combine(Dir, $"fx-{Mode}.json"), body);
        }
    }
}
