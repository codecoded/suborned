using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;
using Pt = Suborned.Sim.Pt;

namespace Suborned.Play
{
    /// <summary>The engine 0.28/0.29 port round (brief 041 next-batch items 19-31), proved in the built player, each check one JSON object with "ok":
    ///  · mood (19, 23): route A's orders to the Scout's Watch on Inez (0:29), then the mood the cell knows, her pip's mood disc, the knobs'
    ///    likely / worse marks for the Fixer, and no Watch row for the Scout on Inez (watched) or the driver (nothing to see);
    ///  · cones (19): the Enforcer's Takedown armed, a café customer hovered: their wedge and the near-body ring; Alt: the armed act's people;
    ///    the Fixer's Pressure armed below Alert 2: no cone;
    ///  · down (29, 31): route "loud-left-downed" to the Scout down (4:47.5), the Scout and the Fixer selected: the car's ring is the Fixer's
    ///    alone; a click beside the body walks (no carry); a click on the body rings Carry; Carry taken; Put down on knob 1;
    ///  · patrol (24, 25): the patrol's nearest pass to the entrance camera's pole over 120 s, and close pictures of its face and a service robot's;
    ///  · junction (22): play zoom and street-level pictures at the junction north-east of the civic block and on the Boulevard, and the drawn
    ///    floor's steps round the junction (heights every 0.5 m, a PNG of them).
    /// Pictures go in the proof folder as r29-*. Nothing here changes a rule; every order is the host's own.</summary>
    public static class Proof29
    {
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static Vector2 Sp(HandoverHost h, Vector3 w) { var s = h.View.WorldToScreenPoint(w); return new Vector2(s.x, s.y); }
        static IEnumerator Shoot(HandoverHost h, string dir, string name, List<string> log, string state = "rest")
        {
            bool was = h.Paused; h.SetPaused(true);
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), state)); yield return null; while (HudToolkit.ShotBusy) yield return null;
            h.SetPaused(was);
        }
        static List<Order> Route(string name)
        {
            var path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), "contracts", "missions", "handover", "routes", name + ".json"); // the repo, found as the fight lab finds briefs/027 (a built app looks from its Contents)
            var sc = Suborned.Sim.Script.Parse(File.ReadAllText(path));
            return sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
        }
        /// <summary>A script by its repo path (an engine scenario exported with `suborned-sim script <id>`).</summary>
        static List<Order> ScriptAt(string repoPath)
        {
            var path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), repoPath);
            var sc = Suborned.Sim.Script.Parse(File.ReadAllText(path));
            return sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
        }
        static Order Copy(Order o) => new() { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run };
        /// <summary>A route's orders given as the shot runner gives them, up to (not including) tick `until`; then the clock to `runTo`.</summary>
        static void Play(HandoverHost h, List<Order> orders, int until, int runTo)
        {
            foreach (var o in orders)
            {
                if (o.Tick >= until || h.Game.Over) break;
                if (o.Tick > h.Game.Tick) h.RunTo(o.Tick);
                var c = Copy(o); if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c);
            }
            if (runTo > h.Game.Tick && !h.Game.Over) h.RunTo(runTo);
        }
        static string RingWords(HandoverHost h) => h.Ring == null ? "" : string.Join(" | ", h.Ring.Segs.ConvertAll(x => x.More ? "⋯" : x.Word + "<" + string.Join("+", x.By.ConvertAll(b => Suborned.Sim.Op.Name(b))) + ">"));

        /// <summary>A world-only picture from a free camera (the play view's lens copied, perspective), as TpLongRun's road pictures.</summary>
        static IEnumerator Pose(HandoverHost h, string dir, string name, Vector3 eye, Vector3 look, float fov, List<string> log)
        {
            var view = h.View; if (!view) yield break;
            var go = new GameObject("r29-cam"); var pc = go.AddComponent<Camera>(); pc.CopyFrom(view); pc.enabled = false; go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            pc.orthographic = false; pc.fieldOfView = fov; pc.nearClipPlane = 0.05f; pc.farClipPlane = 400f; go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
            yield return new WaitForEndOfFrame();
            int w = Mathf.Min(Screen.width, 1920), ht = Mathf.Min(Screen.height, 1080); var rt = new RenderTexture(w, ht, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            try
            {
                for (int i = 0; i < 2; i++) { pc.targetTexture = rt; pc.Render(); }
                var prev = RenderTexture.active; RenderTexture.active = rt; var tx = new Texture2D(w, ht, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, w, ht), 0, 0); tx.Apply(); RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), tx.EncodeToJPG(90)); UnityEngine.Object.Destroy(tx); log.Add($"{name}: ok ({w}x{ht})");
            }
            catch (Exception e) { log.Add($"{name}: FAILED {e.Message}"); }
            pc.targetTexture = null; rt.Release(); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(go);
        }

        /// <summary>A camera in front of a robot's display: the face screen's centre (a renderer named *Face under it), the eye `dist` m out
        /// from the robot's middle through it, a little above; null when the robot has no face screen.</summary>
        static (Vector3 eye, Vector3 look)? FaceView(Transform robot, float dist)
        {
            Renderer face = null; foreach (var r in robot.GetComponentsInChildren<Renderer>()) if (r.name.Contains("Face") && r.enabled && r.gameObject.activeInHierarchy) { face = r; break; }
            if (face == null) return null;
            var c = face.bounds.center; var out_ = c - robot.position; out_.y = 0f; if (out_.sqrMagnitude < 1e-6f) out_ = robot.forward; out_.Normalize();
            return (c + out_ * dist + Vector3.up * 0.25f, c);
        }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true; bool keepPlan = h.controls.planTogether; h.controls.planTogether = false; // orders given while paused go now (no "start together" queue)
            HandoverHost.PointerOverride = null; HandoverHost.PointerWorld = null;

            // ---- Inez's round (item 26, Sky: "walking on an invisible ledge sometimes"), first, on a fresh run: her excursion to table 5 round
            // the café's curved front (the baked round: out at 7:41, 17 s out, 15 s serving, 17 s back), seen from one fixed camera beside her
            // path; every frame her figure's height against the walk file under it (frame.Height) and against the drawn mesh under it (a ray
            // onto temporary mesh colliders over the path's box; meshes that are not readable are counted, not hit)
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
                var g = h.Game; int t0 = g.Tick; var izp = g.PersonByRole("waitress"); var start = g.PersonPos(izp);
                h.RunTo(4590); var before = g.PersonPos(izp);
                var iz = h.Views.Find("waitress"); var door = h.frame.World(new Pt(-1030, -2900)); var table = h.frame.World(new Pt(-2017, -4173));
                var mid = (door + table) * 0.5f; var along = table - door; along.y = 0f; var side = Vector3.Cross(Vector3.up, along.normalized);
                // the camera stands on the open side (the square, away from the café's walls): the side whose point is farther from the café's centre
                var cafe = h.frame.World(new Pt(-1500, -2900)); var eyeA = mid + side * 9f; var eyeB = mid - side * 9f; var eye = ((eyeA - cafe).sqrMagnitude > (eyeB - cafe).sqrMagnitude ? eyeA : eyeB) + Vector3.up * 1.8f;
                // temporary colliders on what is drawn over the path's box
                var box = new Bounds(mid, Vector3.zero); box.Encapsulate(door); box.Encapsulate(table); box.Expand(new Vector3(6f, 4f, 6f));
                var added = new List<Component>(); int unreadable = 0;
                foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude))
                {
                    if (mf.transform.root == (iz ? iz.root : null) || mf.sharedMesh == null) continue; var mr = mf.GetComponent<MeshRenderer>(); if (mr == null || !mr.enabled || !mr.bounds.Intersects(box)) continue;
                    if (!mf.sharedMesh.isReadable) { unreadable++; continue; }
                    if (mf.GetComponent<Collider>() == null) added.Add(mf.gameObject.AddComponent<MeshCollider>());
                }
                yield return null;
                float worstWalk = 0f, worstDrawn = 0f; int frames = 0, rays = 0, stills = 0; Vector3 worstAt = default; var notes = new List<string>(); bool moved = false; Vector3? first = null;
                for (int i = 0; i < 520 && !g.Over && iz; i++)
                {
                    h.RunTo(g.Tick + 1); yield return null; var p = iz.position; first ??= p; if ((p - first.Value).sqrMagnitude > 0.25f) moved = true;
                    var bp = h.frame.ToBlock(new Vector2(p.x, p.z)); float walkY = h.frame.Height(bp.x, bp.y); float dw = Mathf.Abs(p.y - walkY); if (dw > worstWalk) { worstWalk = dw; worstAt = p; }
                    float best = float.NaN; foreach (var ht in Physics.RaycastAll(p + Vector3.up * 2.5f, Vector3.down, 6f)) { if (ht.collider.transform.root == iz.root) continue; if (float.IsNaN(best) || ht.point.y > best) best = ht.point.y; }
                    if (!float.IsNaN(best)) { rays++; float dd = Mathf.Abs(p.y - best); if (dd > worstDrawn) { worstDrawn = dd; if (dd > 0.05f) notes.Add($"{F(p.x)},{F(p.z)}: feet {F(p.y)} drawn {F(best)} walk {F(walkY)}"); } }
                    frames++;
                    if (i % 60 == 0 && stills < 9) { var e = g.PersonPos(izp); yield return Pose(h, dir, $"r29-inez-round-{stills}", eye, p + Vector3.up * 0.9f, 50f, log); log.Add($"r29-inez-round-{stills}: tick {g.Tick}, engine {e}, view {F(p.x)},{F(p.y)},{F(p.z)}"); stills++; }
                }
                foreach (var c in added) if (c) UnityEngine.Object.Destroy(c);
                bool iOk = moved && worstWalk <= 0.05f; ok &= iOk;
                parts.Add($"\"inezRound\": {{\"ok\": {B(iOk)}, \"startTick\": {t0}, \"startAt\": \"{start}\", \"at4590\": \"{before}\", \"moved\": {B(moved)}, \"frames\": {frames}, \"stills\": {stills}, \"worstFeetToWalkM\": {F(worstWalk)}, \"worstFeetToDrawnM\": {F(worstDrawn)}, \"rays\": {rays}, \"colliders\": {added.Count}, \"unreadable\": {unreadable}, \"over5cm\": \"{J(string.Join(" | ", notes.GetRange(0, Math.Min(8, notes.Count))))}\"}}");
                log.Add($"r29 inez round: {(iOk ? "ok" : "FAILED")}: start tick {t0} at {start}, at 7:39 {before}; moved {moved} over {frames} frames; feet to the walk file worst {F(worstWalk)} m at {worstAt}; to the drawn mesh worst {F(worstDrawn)} m over {rays} rays ({added.Count} colliders added, {unreadable} meshes not readable); {notes.Count} frames over 5 cm");
            }

            // ---- mood (items 19, 23)
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
                var g = h.Game; Play(h, Route("route-a"), 290, 295); yield return Frames(3);
                string mood = g.MoodOf("waitress"); bool watched = g.PersonByRole("waitress").KRoutine, offered = g.WatchOffered("waitress"), driverOffered = g.PersonPos(g.PersonByRole("driver")).HasValue && g.WatchOffered("driver");
                h.Select(OpId.Fixer, false); var iz = h.Views.Find("waitress"); string knobs = "", ringScout = "", footer = "";
                if (iz)
                {
                    h.LookAt(iz.position); yield return Frames(2); HandoverHost.PointerWorld = iz.position + Vector3.up; yield return Frames(6);
                    knobs = string.Join(",", h.KnobPred); yield return new WaitForSecondsRealtime(0.3f); yield return Shoot(h, dir, "r29-mood-likely", log, "active");
                    HandoverHost.PointerWorld = null; h.Select(OpId.Scout, false); yield return Frames(2);
                    h.LeftClickScripted(Sp(h, iz.position + Vector3.up)); yield return Frames(3); ringScout = RingWords(h); yield return new WaitForSecondsRealtime(0.3f); yield return Shoot(h, dir, "r29-ring-scout-inez-watched", log, "active"); h.CloseRing();
                }
                foreach (var c in h.Feed) if (c.Verb == "watch" && c.Target == "waitress") footer = c.Footer;
                bool noWatch = !ringScout.Contains("Watch"), knobMark = knobs.Contains("likely") || knobs.Contains("hurt");
                bool mOk = watched && mood is "uneasy" or "scared" or "friendly" && !offered && noWatch && knobMark && !footer.Contains("Learned");
                ok &= mOk;
                parts.Add($"\"mood\": {{\"ok\": {B(mOk)}, \"tick\": {g.Tick}, \"mood\": \"{mood}\", \"watched\": {B(watched)}, \"watchOffered\": {B(offered)}, \"driverWatchOffered\": {B(driverOffered)}, \"knobs\": \"{knobs}\", \"ringScoutInez\": \"{J(ringScout)}\", \"footer\": \"{J(footer)}\"}}");
                log.Add($"r29 mood: {(mOk ? "ok" : "FAILED")}: tick {g.Tick} mood '{mood}' watched {watched} offered {offered}; knobs [{knobs}]; Scout's ring on Inez [{ringScout}]; footer '{footer}'");
            }

            // ---- evidence Glyph's 0.2.104 review could not see (#8): the likely plate (forced on the live pill: the run reaches no likely mood
            // here; labelled so), the no-walk cursor over a roof, the "Camera looped" done plate
            {
                var g = h.Game; var iz = h.Views.Find("waitress");
                if (iz)
                {
                    h.Select(OpId.Fixer, false); HandoverHost.PointerWorld = iz.position + Vector3.up; yield return Frames(6);
                    HudToolkit.PredForce = "likely"; yield return Frames(4); yield return Shoot(h, dir, "r29-likely-plate-forced", log, "active");
                    HudToolkit.PredForce = "hurt"; yield return Frames(4); yield return Shoot(h, dir, "r29-hurt-plate-forced", log, "active"); HudToolkit.PredForce = ""; HandoverHost.PointerWorld = null;
                }
                // the no-walk cursor: the pointer over the café's roof (no walkable cell within 3 m)
                h.Select(OpId.Scout, false); Pt solid = new Pt(-1500, -2900); bool foundSolid = false; // a point of ground with no walkable cell within 3 m (inside a building), nearest the café
                for (int rr = 0; rr < 4000 && !foundSolid; rr += 100) for (int a = 0; a < 360 && !foundSolid; a += 20) { var q = new Pt(-1500 + (int)(rr * Mathf.Cos(a * Mathf.Deg2Rad)), -2900 + (int)(rr * Mathf.Sin(a * Mathf.Deg2Rad))); if (g.S.Grid.Free(q)) continue; var nf = g.S.Grid.NearestFree(q); if (!nf.HasValue || Suborned.Sim.G.Dist(nf.Value, q) > 400) { solid = q; foundSolid = true; } }
                var roof = h.frame.World(solid); HandoverHost.PointerWorld = roof; h.LookAt(roof); yield return new WaitForSecondsRealtime(0.4f);
                string cursor = h.CursorKind; yield return Shoot(h, dir, "r29-no-walk-cursor", log, "active"); HandoverHost.PointerWorld = null;
                log.Add($"r29 no-walk cursor: '{cursor}' at {solid} (found {foundSolid}; MoveNow refused '{h.MoveNow?.Refused}', hover {h.Hover?.Kind})");
                parts.Add($"\"noWalkCursor\": \"{cursor}\"");
            }

            // ---- cones (item 19)
            {
                var g = h.Game; var iz = h.Views.Find("waitress"); string near = "", shown = "", alt = "", pressure = "?"; int alert = g.Alert;
                Transform cust = null; float bd = 99f;
                foreach (var (nm, x) in h.Views.All()) { if (!nm.StartsWith("crowd/") || !x || !x.gameObject.activeInHierarchy || !iz) continue; float d = (x.position - iz.position).magnitude; int ci = int.Parse(nm[6..]); var pid = ci < g.Walkers.Count ? g.Walkers[ci].P.Id : ""; if (pid.Length == 0 || g.PersonCone(pid).Kind != "wedge") continue; if (HandoverHost.OverHud(Sp(h, x.position + Vector3.up))) continue; if (d < bd) { bd = d; cust = x; near = pid; } } // a person under a HUD piece (a pip, a chip) is not hoverable
                if (cust)
                {
                    h.Select(OpId.Enforcer, false); var plate = h.PlateOf(OpId.Enforcer); int td = Array.FindIndex(plate, x => x.Id == "takedown"); if (td >= 0) h.Pick(td);
                    h.LookAt(cust.position); yield return Frames(2); HandoverHost.PointerWorld = cust.position + Vector3.up; yield return Frames(8);
                    for (int w = 0; w < 30 && h.Hover?.Id != near; w++) { if (HandoverHost.OverHud(Sp(h, cust.position + Vector3.up))) HandoverHost.PointerWorld = cust.position + Vector3.up * 0.6f; yield return null; }
                    log.Add($"r29 cones hover: {h.Hover?.Kind}/{h.Hover?.Id} (want {near}), picked '{h.PickedId}', act '{h.ConeAct}', over hud {HandoverHost.OverHud(Sp(h, cust.position + Vector3.up))}");
                    shown = HudToolkit.ConesShown; yield return Shoot(h, dir, "r29-cone-takedown", log, "active");
                    h.PeekLatch = true; yield return new WaitForSecondsRealtime(0.4f); yield return Frames(12); alt = HudToolkit.ConesShown; yield return Shoot(h, dir, "r29-cone-alt", log, "active"); h.PeekLatch = false;
                    h.Cancel(); h.Select(OpId.Fixer, false); plate = h.PlateOf(OpId.Fixer); int pr = Array.FindIndex(plate, x => x.Id == "pressure"); if (pr >= 0) h.Pick(pr);
                    HandoverHost.PointerWorld = null; yield return Frames(2); HandoverHost.PointerWorld = cust.position + Vector3.up; yield return Frames(8); pressure = HudToolkit.ConesShown;
                    yield return Shoot(h, dir, "r29-cone-pressure-a" + alert, log, "active"); h.Cancel(); HandoverHost.PointerWorld = null;
                }
                bool cOk = cust && shown.Contains(near + ":wedge+near") && alt.Length > shown.Length && (alert >= 2 || pressure.Length == 0);
                ok &= cOk;
                parts.Add($"\"cones\": {{\"ok\": {B(cOk)}, \"customer\": \"{near}\", \"metres\": {F(bd)}, \"takedown\": \"{shown}\", \"alt\": \"{alt}\", \"alert\": {alert}, \"pressure\": \"{pressure}\"}}");
                log.Add($"r29 cones: {(cOk ? "ok" : "FAILED")}: customer {near} {F(bd)} m from Inez; takedown [{shown}]; Alt [{alt}]; Pressure at Alert {alert} [{pressure}]");
            }

            // ---- "Camera looped" (Glyph's 0.2.104 review #8): the Loop's done plate as it lands
            {
                var g = h.Game;
                // "Camera looped": route A's loop lands (the Tech at the cabinet, 62 s)
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; g = h.Game; Play(h, Route("route-a"), 800, 0); int t0 = g.Tick; bool looped = false;
                for (int i = 0; i < 1200 && !(looped = g.LoopOn) && !g.Over; i++) h.RunTo(g.Tick + 1);
                if (looped) { var cam = h.Views.Find("camera/entrance"); if (cam) h.LookAt(cam.position); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.4f); h.SetPaused(true); yield return Shoot(h, dir, "r29-camera-looped", log, "active"); }
                log.Add($"r29 camera looped: {looped} at tick {g.Tick} (from {t0}); chips {string.Join(",", h.DoneChips.ConvertAll(c => c.Do + ":" + string.Join("/", c.Lines.ConvertAll(l => l.Word))))}");
            }

            // ---- an agent down (items 29, 31)
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
                var g = h.Game; Play(h, Route("loud-left-downed"), 2876, 2876);
                for (int i = 0; i < 60 && !g.Ops[0].Down && !g.Over; i++) h.RunTo(g.Tick + 1);
                bool down = g.Ops[0].Down; string carRing = "", bodyRing = "", beside = "", carried = "", putDown = ""; bool scoutGives = true, carryTaken = false, carryingNow = false, laid = false, knob1 = false;
                if (down && !g.Over)
                {
                    h.Select(OpId.Scout, false); h.Select(OpId.Fixer, true); yield return Frames(2);
                    var car = h.Views.Find("car/getaway"); var sc = h.Views.Find("op/scout");
                    if (car) { h.LookAt(car.position); yield return Frames(2); h.LeftClickScripted(Sp(h, car.position + Vector3.up * 0.8f)); yield return Frames(3); carRing = RingWords(h); scoutGives = h.Ring != null && h.Ring.Segs.Exists(x => x.By.Contains(OpId.Scout)); yield return new WaitForSecondsRealtime(0.3f); yield return Shoot(h, dir, "r29-ring-agent-down", log, "active"); h.CloseRing(); }
                    if (sc)
                    {
                        h.Select(OpId.Fixer, false); h.LookAt(sc.position); yield return Frames(2);
                        int n0 = g.Orders.Count; var side = sc.position + h.View.transform.right * 2.2f; h.LeftClickScripted(Sp(h, side)); yield return Frames(3);
                        for (int i = n0; i < g.Orders.Count; i++) beside += g.Orders[i].order.Do + (g.Orders[i].ok ? "" : ":" + g.Orders[i].code) + " ";
                        h.LeftClickScripted(Sp(h, sc.position + Vector3.up * 0.15f)); yield return Frames(3); bodyRing = RingWords(h);
                        int ci = h.Ring == null ? -1 : h.Ring.Segs.FindIndex(x => !x.More && x.Row?.I?.Order?.Do == "carry");
                        yield return new WaitForSecondsRealtime(0.3f); yield return Shoot(h, dir, "r29-ring-downed-carry", log, "active");
                        if (ci >= 0) { int n1 = g.Orders.Count; h.RingTakeScripted(ci); yield return Frames(3); for (int i = n1; i < g.Orders.Count; i++) if (g.Orders[i].order.Do == "carry" && g.Orders[i].ok) carryTaken = true; carryTaken |= h.PendingOf(1).verb == "carry"; }
                        h.CloseRing();
                        for (int i = 0; i < 300 && !g.Ops[1].Carrying.HasValue && !g.Over && !g.Ops[1].Down; i++) h.RunTo(g.Tick + 1);
                        carryingNow = g.Ops[1].Carrying.HasValue; carried = $"tick {g.Tick}, fixer down {g.Ops[1].Down}";
                        if (carryingNow)
                        {
                            yield return Frames(4); knob1 = h.PlateOf(OpId.Fixer)[0].Id == "drop"; yield return Shoot(h, dir, "r29-carrying", log, "active");
                            h.Pick(0); yield return Frames(2); h.RunTo(g.Tick + 2); laid = !g.Ops[1].Carrying.HasValue; putDown = laid ? "laid down" : "still carrying";
                        }
                    }
                }
                bool dOk = down && carRing.Length > 0 && !scoutGives && !beside.Contains("carry") && beside.Contains("move") && bodyRing.Contains("Carry") && carryTaken && (!carryingNow || knob1 && laid);
                ok &= dOk;
                parts.Add($"\"down\": {{\"ok\": {B(dOk)}, \"scoutDown\": {B(down)}, \"carRing\": \"{J(carRing)}\", \"scoutGives\": {B(scoutGives)}, \"besideOrders\": \"{beside.Trim()}\", \"bodyRing\": \"{J(bodyRing)}\", \"carryTaken\": {B(carryTaken)}, \"carrying\": {B(carryingNow)}, \"carried\": \"{carried}\", \"knob1PutDown\": {B(knob1)}, \"putDown\": \"{putDown}\"}}");
                log.Add($"r29 down: {(dOk ? "ok" : "FAILED")}: Scout down {down}; car ring [{carRing}] (a downed giver: {scoutGives}); beside the body [{beside.Trim()}]; body ring [{bodyRing}]; carry taken {carryTaken}, carrying {carryingNow} ({carried}); knob 1 Put down {knob1}; {putDown}");
            }

            // ---- the patrol (items 24, 25)
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
                var g = h.Game; var cam = g.S.Cameras.Find(c => c.Id == "entrance"); long best = long.MaxValue; int bestTick = -1; Pt bestAt = default;
                for (int i = 0; i < 1200 && !g.Over; i++)
                {
                    h.RunTo(g.Tick + 1); var pp = g.PatrolPose(); if (!pp.HasValue || cam == null) continue;
                    long d = Suborned.Sim.G.D2(pp.Value.pos, cam.At); if (d < best) { best = d; bestTick = g.Tick; bestAt = pp.Value.pos; }
                }
                float bestM = best == long.MaxValue ? -1f : Mathf.Sqrt(best) / 100f;
                var patrol = h.Views.Find("patrol"); string faces = "";
                if (patrol)
                {
                    h.LookAt(patrol.position); yield return Frames(3); yield return Shoot(h, dir, "r29-patrol-pole", log);
                    if (FaceView(patrol, 1.6f) is var (pe, pl)) yield return Pose(h, dir, "r29-patrol-face", pe, pl, 40f, log); else log.Add("r29-patrol-face: no face screen under the patrol");
                    Suborned.Robots.RobotFace near = null; float nd = 1e9f;
                    foreach (var rf in UnityEngine.Object.FindObjectsByType<Suborned.Robots.RobotFace>(FindObjectsInactive.Exclude)) { if (rf.Screens == 0) continue; float d = (rf.transform.position - patrol.position).sqrMagnitude; if (d < nd) { nd = d; near = rf; } }
                    if (near && FaceView(near.transform, 1.8f) is var (se, sl)) { yield return Pose(h, dir, "r29-service-robot-face", se, sl, 40f, log); faces = $"service robot {near.name} {F(Mathf.Sqrt(nd))} m off"; }
                }
                bool pOk = bestM >= 0.6f; ok &= pOk;
                parts.Add($"\"patrol\": {{\"ok\": {B(pOk)}, \"nearestPoleM\": {F(bestM)}, \"atTick\": {bestTick}, \"at\": [{bestAt.X}, {bestAt.Y}], \"pole\": [{cam?.At.X ?? 0}, {cam?.At.Y ?? 0}], \"faces\": \"{J(faces)}\"}}");
                log.Add($"r29 patrol: {(pOk ? "ok" : "FAILED")}: nearest pass to the entrance camera's pole {F(bestM)} m at tick {bestTick} (needs 0.6 m); {faces}");
            }

            // ---- quiet cones (brief 057 §3): a learned cone rests; hovered or with an agent in it, strong
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
                var g = h.Game; string rest = "", strong = "", hovered = ""; bool learned = false;
                h.GiveWatchScripted(OpId.Tech, "camera:entrance"); // as a click gives it: the walk into reach and line, then the Watch
                for (int i = 0; i < 900 && !(learned = g.Known("camera:entrance") is { Sight: true }) && !g.Over; i++) h.RunTo(g.Tick + 1);
                if (learned)
                {
                    h.RunTo(g.Tick + 2); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.4f); h.SetPaused(true); strong = h.Views.FanStrengths;
                    h.Issue(new Order { Tick = g.Tick, Op = "tech", Do = "move", To = "cabinet" }); for (int i = 0; i < 600 && g.Ops[2].Walk != null; i++) h.RunTo(g.Tick + 1);
                    h.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "move", To = "cabinet" }); h.RunTo(g.Tick + 1); h.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "cancel" }); // nobody walks through it
                    yield return new WaitForSecondsRealtime(0.9f); rest = h.Views.FanStrengths;
                    var camT = h.Views.Find("camera/entrance"); if (camT) h.LookAt(camT.position); yield return Frames(3); yield return Shoot(h, dir, "r29-cone-rest", log);
                    if (camT) { HandoverHost.PointerWorld = camT.position + Vector3.up * 1.5f; yield return new WaitForSecondsRealtime(0.5f); hovered = h.Views.FanStrengths; yield return Shoot(h, dir, "r29-cone-strong", log, "active"); HandoverHost.PointerWorld = null; }
                }
                bool qOk = learned && rest.Contains("camera:entrance=0.0") && hovered.Contains("camera:entrance=1.0");
                ok &= qOk;
                parts.Add($"\"quietCones\": {{\"ok\": {B(qOk)}, \"learned\": {B(learned)}, \"techWatching\": \"{strong}\", \"rest\": \"{rest}\", \"hovered\": \"{hovered}\", \"alert\": {g.Alert}}}");
                log.Add($"r29 quiet cones: {(qOk ? "ok" : "FAILED")}: learned {learned}; while the Tech stood in it [{strong}]; at rest [{rest}]; hovered [{hovered}]; Alert {g.Alert}");
            }

            // ---- the reroute walk (brief 057 §2): the route drawn while in play, the patrol on it within 0.3 m, no jump
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
                var g = h.Game; Play(h, Route("route-a"), 786, 790); // the Tech's Reroute at 1:18.5
                for (int i = 0; i < 900 && !(g.PatrolAwayUntil > g.Tick) && !g.Over; i++) h.RunTo(g.Tick + 1);
                int rolelessUnlocks = 0; h.UnattachedGates.Clear(); foreach (var dc in h.DoneChips) if (dc.Role.Length == 0 && dc.Unlocks.Count > 0) rolelessUnlocks++;
                float maxOff = 0f, maxJump = 0f; int samples = 0, shots = 0; bool engine = false, live = false; Vector3? last = null; string shown = ""; List<Vector3> drawn = null; bool edgeShot = false, edgeSeen = false; // the route as first drawn when the reroute began: the robot must keep to it
                var pt = h.Views.Find("patrol"); if (pt) h.LookAt(pt.position);
                for (int i = 0; i < 1400 && g.PatrolAwayUntil > g.Tick && !g.Over; i++) // engine 0.30: ~46 s out, 19 s waiting, 50 s back on route A
                {
                    h.RunTo(g.Tick + 1); yield return null; pt = h.Views.Find("patrol"); if (!pt) break;
                    live |= h.Reroute.Live; engine |= h.Reroute.FromEngine; shown = HudToolkit.RerouteShown;
                    if (drawn == null && h.Reroute.Live && h.Reroute.Route.Count >= 2) drawn = new List<Vector3>(h.Reroute.Route);
                    foreach (var dc in h.DoneChips) if (dc.Role.Length == 0 && dc.Unlocks.Count > 0) rolelessUnlocks++; // Glyph's 0.2.104 review #2: a chip with no person never wears unlock pills
                    var p = pt.position; float off = HandoverHost.OffRoute(p, drawn); if (off >= 0f) { maxOff = Mathf.Max(maxOff, off); samples++; }
                    if (last.HasValue) maxJump = Mathf.Max(maxJump, (p - last.Value).magnitude); last = p;
                    if (i % 300 == 0 && shots < 4) { h.LookAt(p); yield return Frames(2); yield return Shoot(h, dir, $"r29-reroute-walk-{shots}", log); shots++; }
                    if (drawn != null && !edgeShot)
                    {   // Glyph's 0.2.104 review #3: the edge chip, the wait spot put off screen (the view 45 m the far side of the patrol), shot at 2x
                        edgeShot = true; var away = p - h.Reroute.Wait; away.y = 0f; if (away.sqrMagnitude < 1f) away = Vector3.right; h.LookAt(p + away.normalized * 45f);
                        yield return Frames(3); edgeSeen = HudToolkit.RerouteEdgeShown; yield return Shoot(h, dir, "r29-reroute-edge-chip", log); h.LookAt(p); yield return Frames(2);
                    }
                }
                bool rOk = engine && live && samples > 0 && maxOff <= 0.3f && maxJump <= 0.5f && rolelessUnlocks == 0 && edgeSeen; string rok = B(rOk);
                log.Add($"r29 reroute: chips with no person wearing unlocks {rolelessUnlocks}; gates that opened with no chip of their person: {(h.UnattachedGates.Count == 0 ? "none" : string.Join(" | ", h.UnattachedGates))}"); // engine 0.30.0: the reroute walks (PatrolLegs), so the check asserts
                ok &= rOk;
                parts.Add($"\"reroute\": {{\"ok\": {rok}, \"engineRoute\": {B(engine)}, \"live\": {B(live)}, \"samples\": {samples}, \"maxOffM\": {F(maxOff)}, \"maxStepM\": {F(maxJump)}, \"shown\": \"{shown}\", \"shots\": {shots}, \"edgeChip\": {B(edgeSeen)}, \"rolelessUnlocks\": {rolelessUnlocks}, \"unattachedGates\": \"{J(string.Join(" | ", h.UnattachedGates))}\"}}");
                log.Add($"r29 reroute: {(rOk ? "ok" : "FAILED")} (engine route {engine}): drawn {shown}; {samples} samples, furthest off the drawn route {F(maxOff)} m (0.3), largest step between frames {F(maxJump)} m");
            }

            // ---- Go held (brief 057 §4.2; Glyph's 0.2.104 review #8): engine scenario N185 (Vale aboard, a rival holding the exit lane): the car's
            // ring shows Go shut, struck, with the lock and one badge a holder
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; var g = h.Game;
                Play(h, ScriptAt("briefs/057/n185-go-held.script.json"), 3605, 3605); var a = h.GoLock(); string ring = "";
                if (!a.ok && a.code == "car-held")
                {
                    h.Select(OpId.Enforcer, false); var car = h.Views.Find("car/getaway"); if (car) { h.LookAt(car.position); yield return Frames(3); h.LeftClickScripted(Sp(h, car.position + Vector3.up * 0.8f)); yield return Frames(3); ring = RingWords(h); yield return new WaitForSecondsRealtime(0.3f); yield return Shoot(h, dir, "r29-go-held", log, "active"); h.CloseRing(); }
                }
                bool shut = h.Ring == null && ring.Length > 0; log.Add($"r29 go held: tick {g.Tick}, Go {a.code} ({a.ids}); ring [{ring}]");
                parts.Add($"\"goHeld\": {{\"code\": \"{a.code}\", \"holders\": \"{J(a.ids)}\", \"ring\": \"{J(ring)}\"}}");
            }

            // ---- the seal clock and the sealed fail (brief 057 §4)
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
                var g = h.Game; var orders = Route("fail-district-seals"); Play(h, orders, 1700, 1700);
                for (int i = 0; i < 600 && !h.SealOn && !g.Over; i++) h.RunTo(g.Tick + 1);
                bool on = h.SealOn; int left = h.SealLeftS; string words = "", cause = "", endLines = "", car = "";
                if (on)
                {
                    yield return Frames(4); words = HudToolkit.SealWords; HudToolkit.SealHoverForce = true; yield return Frames(2); cause = h.SealCauseIcon();
                    yield return new WaitForSecondsRealtime(0.7f); yield return Shoot(h, dir, "r29-seal-clock", log, "active"); HudToolkit.SealHoverForce = false;
                    // the last 10 s (amber, blinking): run to 8 s before the seal, unpaused a moment so the blink and the underline draw
                    if (g.Lockdown < int.MaxValue / 4) { h.RunTo(g.Lockdown - 8 * Suborned.Sim.Game.Hz); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.35f); h.SetPaused(true); log.Add($"r29 seal last 10 s: marker '{HudToolkit.SealWords}' at tick {g.Tick}"); yield return Shoot(h, dir, "r29-seal-last10", log, "active"); }
                    Play(h, orders, int.MaxValue, 6000); for (int i = 0; i < 3000 && !g.Over; i++) h.RunTo(g.Tick + 1);
                    yield return new WaitForSecondsRealtime(0.6f); car = h.SealedCar; endLines = string.Join(" | ", HudToolkit.EndLinesNow ?? new List<string>());
                    yield return Shoot(h, dir, "r29-seal-end", log, "active");
                }
                bool sOk = on && words.Contains(":") && g.FailCode == "sealed" && !(car == "empty" && endLines.Contains("Vale reached the getaway car"));
                ok &= sOk;
                parts.Add($"\"seal\": {{\"ok\": {B(sOk)}, \"on\": {B(on)}, \"leftS\": {left}, \"marker\": \"{J(words)}\", \"cause\": \"{cause}\", \"fail\": \"{g.FailCode}\", \"car\": \"{car}\", \"end\": \"{J(endLines)}\"}}");
                log.Add($"r29 seal: {(sOk ? "ok" : "FAILED")}: on {on}, {left} s left, marker '{words}', cause {cause}; ended {g.FailCode}; car state '{car}' (Keel's FailCause.Car); end card: {endLines}");
            }

            // ---- fire on a busy agent (the 0.2.113 bot smoke row, Q-OFFER: the ring offered Fire to a busy Scout, refused agent-busy): an act ending
            // within 2 s is waited for and the fire goes when it lands; a longer act is cancelled first and the fire goes now; never agent-busy
            {
                string Busy(Suborned.Sim.Game g0) { var op = g0.Ops[(int)OpId.Scout]; return op.Busy == null ? "" : $"{op.Busy.Do} ends in {(op.BusyEnd - g0.Tick) / (float)Suborned.Sim.Game.Hz:0.0} s"; }
                IEnumerator Case(bool shortAct, string[] res)
                {
                    h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; var g = h.Game;
                    h.Select(OpId.Scout, false); h.GiveScripted(new Order { Op = "scout", Do = "watch", Target = "guard" }); // at the Office door: the entrance camera is in pistol range (0.2.114: the official car was 16 m from the cafe, out of range)
                    var sc = g.Ops[(int)OpId.Scout];
                    for (int i = 0; i < 1500 && !g.Over; i++)
                    {
                        if (sc.Busy != null) { int left = sc.BusyEnd - g.Tick; if (shortAct ? left <= 2 * Suborned.Sim.Game.Hz && left > 3 : left > 4 * Suborned.Sim.Game.Hz) break; }
                        h.RunTo(g.Tick + 1);
                    }
                    // a property target in range with a clear line from where the Scout stands (the entrance camera first)
                    string tgt = ""; var cands = new List<string> { "camera:entrance" }; foreach (var cm in g.S.Cameras) cands.Add("camera:" + cm.Id); cands.Add("car:official"); cands.Add("car:getaway");
                    foreach (var cid in cands) { var tp = g.PosOf(cid); if (tp.HasValue && Suborned.Sim.G.Within(sc.Pos, tp.Value, sc.Gun.W.MaxCm - 100) && g.S.Sight(sc.Pos, tp.Value, g.R.Eye)) { tgt = cid; break; } }
                    string before = Busy(g) + (tgt.Length > 0 ? $", target {tgt}" : ", no property target in range"); int n0 = g.Orders.Count, c0 = h.FireCancelledActs;
                    h.Issue(new Order { Op = "scout", Do = "fire", Target = tgt.Length > 0 ? tgt : "camera:entrance" }, (Vector2?)null, (Vector3?)null); // the player's path (Playable), as a ring wedge gives it
                    int given = g.Orders.Count - n0; bool waited = shortAct && given == 0;
                    for (int i = 0; i < 40 && !g.Over && g.Orders.Count == n0; i++) h.RunTo(g.Tick + 1); // the wait: the fire goes when the act lands
                    h.RunTo(g.Tick + 1);
                    bool busyRefusal = false, fireOk = false, cancel = false;
                    string fireCode = ""; for (int i = n0; i < g.Orders.Count; i++) { var (oo, ok2, code, _) = g.Orders[i]; if (code == "agent-busy") busyRefusal = true; if (oo.Do == "fire") { if (ok2) fireOk = true; else fireCode = code; } if (oo.Do == "cancel" && ok2) cancel = true; }
                    bool cOk = !busyRefusal && fireOk && (shortAct ? waited && !cancel : cancel && h.FireCancelledActs > c0);
                    res[0] = $"{{\"ok\": {B(cOk)}, \"busy\": \"{J(before)}\", \"givenAtOnce\": {given}, \"waited\": {B(waited)}, \"cancelled\": {B(cancel)}, \"fired\": {B(fireOk)}, \"agentBusy\": {B(busyRefusal)}, \"fireRefused\": \"{fireCode}\"}}";
                    log.Add($"r29 fire busy ({(shortAct ? "act ending within 2 s" : "longer act")}): {(cOk ? "ok" : "FAILED")}: {before}; given at once {given}, waited {waited}, cancelled {cancel}, fired {fireOk}{(fireCode.Length > 0 ? " (refused " + fireCode + ")" : "")}, agent-busy {busyRefusal}");
                    ok &= cOk;
                }
                var rs = new string[1]; var rl = new string[1];
                yield return Case(true, rs); yield return Case(false, rl);
                parts.Add($"\"fireBusy\": {{\"shortAct\": {rs[0]}, \"longAct\": {rl[0]}}}");
            }

            // ---- the junction and the Boulevard (item 22)
            {
                var spots = new (string name, Vector2 plan)[] { ("civic-junction", new Vector2(-132f, -267.5f)), ("squarebay", new Vector2(-177.8f, -238.5f)), ("officebay", new Vector2(-161f, -307.8f)), ("boulevard-market", new Vector2(0f, -410f)), ("circus-mouth", new Vector2(0f, -46f)), ("junction-ne", new Vector2(-60f, -198f)), ("boulevard", new Vector2(-9.5f, -104f)) }; // item 22 (Splice's corners, briefs/028/corners): the civic junction, the two lay-bys (Sky's stray-kerb ring at a bay mouth), Boulevard x Market, the circus mouth
                var rows = new List<string>();
                foreach (var (name, plan) in spots)
                {
                    var c = new Vector3(plan.x, 0f, plan.y); var b0 = h.frame.ToBlock(plan); c.y = h.frame.Height(b0.x, b0.y);
                    h.TurnSnap(0); h.LookAt(c); yield return new WaitForSecondsRealtime(0.6f); yield return Shoot(h, dir, $"r29-{name}-play", log);
                    h.TurnSnap(2); h.LookAt(c); yield return new WaitForSecondsRealtime(0.6f); yield return Shoot(h, dir, $"r29-{name}-play-210", log); h.TurnSnap(0);
                    yield return Pose(h, dir, $"r29-{name}-street", c + new Vector3(7f, 1.7f, -7f), c + Vector3.up * 0.6f, 55f, log);
                    yield return Pose(h, dir, $"r29-{name}-street-b", c + new Vector3(-7f, 1.7f, 7f), c + Vector3.up * 0.6f, 55f, log);
                    // the drawn floor round it: heights every 0.5 m over 40 x 40 m; the steepest step between neighbours and a picture of the heights
                    const int N = 81; var hs = new float[N, N]; float lo = 9f, hi = -9f;
                    for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { var bp = h.frame.ToBlock(plan + new Vector2((i - 40) * 0.5f, (j - 40) * 0.5f)); var v = h.frame.Height(bp.x, bp.y); hs[i, j] = v; lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v); }
                    float step = 0f; int over = 0; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i + 1 < N) { float d = Mathf.Abs(hs[i + 1, j] - hs[i, j]); step = Mathf.Max(step, d); if (d > 0.2f) over++; } if (j + 1 < N) { float d = Mathf.Abs(hs[i, j + 1] - hs[i, j]); step = Mathf.Max(step, d); if (d > 0.2f) over++; } }
                    var tx = new Texture2D(N, N, TextureFormat.RGB24, false); for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { float t = hi > lo ? (hs[i, j] - lo) / (hi - lo) : 0f; tx.SetPixel(i, j, new Color(t, t, t)); }
                    tx.Apply(); File.WriteAllBytes(Path.Combine(dir, $"r29-{name}-heights.png"), tx.EncodeToPNG()); UnityEngine.Object.Destroy(tx);
                    rows.Add($"{{\"spot\": \"{name}\", \"plan\": [{F(plan.x)}, {F(plan.y)}], \"low\": {F(lo)}, \"high\": {F(hi)}, \"steepestStep\": {F(step)}, \"stepsOver20cm\": {over}}}");
                    log.Add($"r29 {name}: floor {F(lo)}..{F(hi)} m, steepest step {F(step)} m, {over} steps over 0.2 m (kerbs are 0.15)");
                }
                parts.Add($"\"floor\": [{string.Join(", ", rows)}]");
            }

            h.Begin(); HandoverHost.PointerWorld = null; h.controls.planTogether = keepPlan;
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }
    }
}
