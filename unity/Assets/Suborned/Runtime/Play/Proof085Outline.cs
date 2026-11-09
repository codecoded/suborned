using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OpId = Suborned.Sim.OpId;

namespace Suborned.Play
{
    /// <summary>Brief 101, the hover outline (Glyph's look B; HostOutline, Suborned.HoverOutlinePass), in r083 beside r085's door guard. The proof holds the
    /// pointer itself on a world point (HandoverHost.PointerWorld: a capture resizes the window for its frame, and a point held in screen pixels would land on
    /// something else there), so it needs no real pointer and no window focus. At play zoom 10:
    ///  · the guard at his post, the Enforcer selected (brief 085's case), the pointer on him: the line round the guard (r085-outline-guard);
    ///  · 40 px beside him, in the door's box: round the door, behind the guard (r085-outline-door);
    ///  · the Scout selected, the pointer on the Fixer: one of the cell (r085-outline-cell);
    ///  · the pointer on the lamp mast's pods: its head, not its post (r085-outline-mast).
    /// Each picture has a 2x crop round the line beside it (-2x.png). Rows: r085outline (the guard's two hovers: the line on the thing the pill names, faded
    /// in, drawn this frame), r085outlineCell, and r085outlineMast (the line's box is the head's, well under the mast's height).</summary>
    public static class Proof085Outline
    {
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static IEnumerator Shoot(string dir, string name, List<string> log)
        {
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), "active")); yield return null; while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: 2x {Crop2x(Path.Combine(dir, name + ".png"))}");
        }

        /// <summary>The pointer held off the street (the screen's top edge) for a beat, then on the world point `at`, until the pill's stick and the line's fade are done.</summary>
        static IEnumerator Point(Vector3 at)
        {
            HandoverHost.PointerWorld = null; HandoverHost.PointerOverride = new Vector2(Screen.width * 0.5f, Screen.height - 4f); yield return new WaitForSecondsRealtime(0.3f);
            HandoverHost.PointerOverride = null; HandoverHost.PointerWorld = at; yield return new WaitForSecondsRealtime(0.45f); yield return Frames(2);
        }

        /// <summary>The line now against what it should be on: on it, faded in, drawn within the last frames from at least one renderer.</summary>
        static (bool ok, string words) Now(HandoverHost h, string want)
        {
            string on = h.OutlineOn; float a = h.OutlineAlpha; int ago = Time.frameCount - HoverOutlinePass.DrawnFrame; var b = HoverOutlinePass.DrawnBox;
            bool ok = on == want && a >= 0.99f && ago <= 2 && h.OutlineParts > 0;
            return (ok, $"outline {(on.Length > 0 ? on : "none")} (fade {a:0.00}, {h.OutlineParts} parts, drawn {ago} frames ago, box {b.width}x{b.height} px at {b.x},{b.y}); pill on '{HudToolkit.PillOn}' '{HudToolkit.PillWords}'");
        }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            // ---- the guard in the Office door, the Enforcer selected: on him, then 40 px beside him (the door)
            yield return Proof085.Fresh(h); bool post = Proof085.GuardAtPost(h);
            h.Select(OpId.Enforcer, false); var gt = h.Views.Find("guard"); h.LookAt(gt.position); yield return Frames(3);
            float kp = Screen.height / 1080f; var chest = gt.position + Vector3.up; var gs = h.View.WorldToScreenPoint(chest);
            var beside = h.View.ScreenToWorldPoint(new Vector3(gs.x + 40f * kp, gs.y, gs.z)); // 40 px (at 1080p) to his right, at his depth: it stays 40 px at any window size
            yield return Point(chest); var (guard, onGuard) = Now(h, "person/guard"); bool pillGuard = HudToolkit.PillOn == "person/guard";
            yield return Shoot(dir, "r085-outline-guard", log);
            yield return Point(beside); var (door, onDoor) = Now(h, "door/door"); bool pillDoor = HudToolkit.PillOn == "door/door";
            yield return Shoot(dir, "r085-outline-door", log);
            row("r085outline", post && guard && pillGuard && door && pillDoor, $"guard at his post {post}; on him: {onGuard}; 40 px beside: {onDoor}");

            // ---- one of the cell: the Scout selected, the pointer on the Fixer
            h.Select(OpId.Scout, false); var fx = h.Views.Find("op/fixer"); bool cellOk = false; string cell = "no Fixer drawn";
            if (fx && fx.gameObject.activeInHierarchy)
            {
                h.LookAt(fx.position); yield return Frames(3);
                yield return Point(fx.position + Vector3.up); (cellOk, cell) = Now(h, "op/fixer");
                yield return Shoot(dir, "r085-outline-cell", log);
            }
            row("r085outlineCell", cellOk, $"the Scout selected, the pointer on the Fixer: {cell}");

            // ---- the lamp mast: the pointer on its pods; the line round its head, not its post
            const string Mast = "camera/mast-z0"; bool mastOk = false; string mast = "no lamp mast";
            if (h.Views.IsMast(Mast) && h.Views.Find(Mast) is Transform mt)
            {
                var pods = mt.position + Vector3.up * h.Views.PickUp(Mast); var d = h.View.transform.forward;
                h.LookAt(pods + d * (pods.y / Mathf.Max(0.1f, -d.y))); yield return Frames(3); // the ground point the view's ray through the pods meets
                yield return Point(pods); (mastOk, mast) = Now(h, "camera/mast-z0");
                float foot = mt.position.y - 1.6f, tall = h.View.WorldToScreenPoint(new Vector3(mt.position.x, foot + h.Views.TallOf(Mast), mt.position.z)).y - h.View.WorldToScreenPoint(new Vector3(mt.position.x, foot, mt.position.z)).y;
                float box = HoverOutlinePass.DrawnBox.height * (h.View.pixelHeight / (float)Mathf.Max(1, HoverOutlinePass.DrawnTarget.y));
                mastOk &= box < tall * 0.5f; mast += $"; the line's box {box:0} px tall, the mast {tall:0} px";
                yield return Shoot(dir, "r085-outline-mast", log);
            }
            row("r085outlineMast", mastOk, $"the pointer on the pods: {mast}");
            yield return Officers(h, dir, log, row);
            HandoverHost.PointerOverride = null; HandoverHost.PointerWorld = null; yield return Frames(2);
        }

        /// <summary>Brief 101, each officer his own target: a route played headless until the response unit's two officers stand on the street outside a fight (the views
        /// security/0 and /1; in a fight they are hostiles, each by his id already), an armed agent on the street selected, the pointer on each in turn. Row r085outlineOfficer:
        /// the hover, the line (its box round him), the pill and Fire's order are that officer's, and the ring's anchor is his figure, not the first's (r085-outline-officer).</summary>
        static readonly string[] OfficerRoutes = { "careless-guard-left-door-held", "loud", "careless-body-found" }; // a body held as a scene; a loud entry before the fight takes them
        static IEnumerator Officers(HandoverHost h, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            var g = h.Game; string via = "";
            bool Up(int i) => i < g.SecGuns.Length && g.SecOfficerPose(i).HasValue && !g.Hostiles.Exists(x => x.Id == g.SecGuns[i].Id);
            bool Both() => Up(0) && Up(1) && Suborned.Sim.G.D2(g.SecOfficerPose(0).Value.pos, g.SecOfficerPose(1).Value.pos) > 60L * 60L; // two figures, not one on the other
            foreach (var name in OfficerRoutes) // whichever seed this game rolled: the first route that puts the two on the street outside a fight
            {
                yield return Proof085.Fresh(h); g = h.Game; var orders = ProofBook.Route(name); int oi = 0;
                while (!g.Over && g.Tick < 3300 && !Both()) { while (oi < orders.Count && orders[oi].Tick <= g.Tick) { var o = orders[oi++]; h.Issue(new Suborned.Sim.Order { Op = o.Op, Do = o.Do, To = o.To, Point = o.Point, Target = o.Target, Approach = o.Approach, How = o.How, Run = o.Run }); } h.RunTo(g.Tick + 2); }
                if (Both()) { via = $"{name}, seed {g.Seed}"; break; }
            }
            if (via.Length == 0) { row("r085outlineOfficer", false, $"no route of {string.Join(", ", OfficerRoutes)} put the two officers on the street outside a fight"); yield break; }
            int armed = 0; for (int k = 3; k >= 0; k--) if (!g.Ops[k].Down && !g.Ops[k].Inside && !g.Ops[k].InCar && g.Ops[k].Gun.W != null) armed = k; // the first armed agent on the street: a click on an officer is his Fire
            h.Select((OpId)armed, false); h.LookAt(h.Views.Find("security/0").position); yield return new WaitForSecondsRealtime(1f); // the figures ease to where the engine put them
            var said = new List<string>(); bool ok = true; int yaw0 = h.YawIndex;
            for (int i = 0; i < 2; i++)
            {
                var v = h.Views.Find("security/" + i); var other = h.Views.Find("security/" + (1 - i)); string id = g.SecGuns[i].Id;
                if (!v || !v.gameObject.activeInHierarchy) { ok = false; said.Add($"{id} not drawn"); continue; }
                h.LookAt(v.position); yield return Frames(3); Vector3? on = null;
                for (int turn = 0; turn < 4 && on == null; turn++) // a point on him the pointer reaches: where one stands in front of the other the nearer is under the pointer (the pick's own law), so a side or his head, else the view a quarter round
                {
                    if (turn > 0) { h.TurnSnap((h.YawIndex + 1) % 4); h.LookAt(v.position); yield return Frames(4); }
                    foreach (float up in Ups) foreach (float side in Sides)
                    {
                        if (on != null) break;
                        var q = v.position + Vector3.up * up + h.View.transform.right * side; HandoverHost.PointerOverride = null; HandoverHost.PointerWorld = q; yield return Frames(3);
                        if (h.Hover?.Kind == "security" && h.Hover.Key == id) on = q;
                    }
                }
                var at = on ?? v.position + Vector3.up; yield return Point(at);
                var t = h.Hover; var (lineOk, line) = Now(h, "security/" + id); var anchor = t != null ? h.TargetWorld(t) : null; var sp = h.View.WorldToScreenPoint(at);
                bool onHim = anchor is Vector3 a && Vector3.Distance(a, v.position) < 0.3f && (!other || Vector3.Distance(a, other.position) > Vector3.Distance(a, v.position) + 0.3f);
                var px = new Vector2Int((int)(sp.x * HoverOutlinePass.DrawnTarget.x / (float)h.View.pixelWidth), (int)(sp.y * HoverOutlinePass.DrawnTarget.y / (float)h.View.pixelHeight)); bool boxHim = HoverOutlinePass.DrawnBox.Contains(px);
                string fire = t == null ? "" : h.ContextFor(t, new Vector2(sp.x, sp.y)).Pn.Rows.Find(r => r.I?.Order?.Do == "fire")?.I.Order.Target ?? "";
                ok &= t?.Kind == "security" && t.Key == id && lineOk && boxHim && onHim && (HudToolkit.PillOn.Length == 0 || HudToolkit.PillOn == "security/" + id) && fire == id; // a pill, if any, is his
                said.Add($"{id} ({via}, {HandoverHost.MissionClock(g.Tick)}, {(OpId)armed} selected, yaw {30 + 90 * h.YawIndex}, {(on != null ? "a point on him the pointer reaches" : "no point on him the pointer reaches")}): hover {t?.Kind}/{t?.Key}; {line}; the line's box round him {boxHim}; the ring's anchor on him {onHim}; Fire's target '{fire}'");
                if (i == 1) yield return Shoot(dir, "r085-outline-officer", log);
            }
            h.TurnSnap(yaw0); row("r085outlineOfficer", ok, string.Join(" | ", said));
        }
        static readonly float[] Ups = { 1.0f, 1.5f, 0.6f }, Sides = { 0f, -0.15f, 0.15f };

        /// <summary>A 2x crop (nearest pixel) of the line's box and 40 px round it, beside the picture; what it wrote.</summary>
        static string Crop2x(string png)
        {
            if (!File.Exists(png)) return "no picture";
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!src.LoadImage(File.ReadAllBytes(png))) return "unreadable";
                if (!HoverOutlinePass.DrawnBoxFor(src.width, src.height, out var b)) return $"no line drawn at the picture's size {src.width}x{src.height}"; const int Pad = 40; // the box of the capture's own frame
                int x0 = Mathf.Clamp(b.x - Pad, 0, src.width), x1 = Mathf.Clamp(b.xMax + Pad, 0, src.width);
                int y0 = Mathf.Clamp(b.y - Pad, 0, src.height), y1 = Mathf.Clamp(b.yMax + Pad, 0, src.height); // rows from the bottom, as the box
                int w = x1 - x0, h = y1 - y0; if (w < 2 || h < 2) return "empty box";
                var px = src.GetPixels32(); var o = new Color32[w * 2 * h * 2];
                for (int y = 0; y < h * 2; y++) for (int x = 0; x < w * 2; x++) o[y * w * 2 + x] = px[(y0 + y / 2) * src.width + x0 + x / 2];
                var dst = new Texture2D(w * 2, h * 2, TextureFormat.RGBA32, false); dst.SetPixels32(o); dst.Apply();
                var path = png[..^4] + "-2x.png"; File.WriteAllBytes(path, dst.EncodeToPNG()); Object.Destroy(dst);
                return $"{Path.GetFileName(path)} {w * 2}x{h * 2} (from {x0},{y0})";
            }
            finally { Object.Destroy(src); }
        }
    }
}
