using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 084, the edge icons (Sky: "if someone sees you off screen their icon can be on the edge of screen with arrow"): one list in place of
    /// the edge marks in Lock, Reroute and Rivals and the off-screen dock in Attention. The watcher's face on the screen edge, an arrow out, a badge for
    /// what it's doing: firing at you, calling it in, coming to you, sees you; your own target off screen carries your agents' faces. Known watchers
    /// only (the callouts and the fight's hostiles). A click jumps the camera there, never an order.</summary>
    public sealed partial class HudToolkit
    {
        sealed class EdgeItem { public string Id = "", Kind = "", Face = ""; public Vector3 World; public int Last, N = 1; public Vector2 Raw; public List<int> Shooters; }
        sealed class EdgeChip { public VisualElement Root, Badge, BadgeIcon, Count; public VisualElement Cam; public Label CountText; public Paint Arrow; public readonly List<VisualElement> Faces = new(); public string Id = "", Face = "", Say = ""; public Vector3 World; public Vector2 At; public float Angle, FiredAt = -9f; public int Last = -1; public bool Fire; }
        readonly List<EdgeChip> edgeChips = new(); VisualElement edgeMore; Label edgeMoreText;
        readonly Dictionary<string, EdgeItem> edgeNow = new(); readonly List<EdgeItem> edgeList = new();
        readonly Dictionary<string, (int tick, Vector3 at)> edgeWas = new(); readonly List<Vector2> edgeUsed = new(); readonly HashSet<string> edgeMoving = new();
        /// <summary>The rival car off screen (the Rivals draw, with this switch on, hands it over).</summary>
        Vector3? cueRivalCar;
        /// <summary>The proof reads it: the chips shown now.</summary>
        public static string EdgeShown { get; private set; } = "";
        /// <summary>A chip's centre on screen (origin bottom left), for the proof's click.</summary>
        public static Vector2? EdgeChipScreen { get; private set; }
        static int EdgeRank(string k) => k switch { "fire" => 4, "call" => 3, "come" => 2, "see" => 1, _ => 0 };
        static string EdgeSay(string k) => k switch { "fire" => "Firing at you", "call" => "Calling it in", "come" => "Coming to you", "see" => "Sees you", _ => "You are firing here" };
        string EdgeFace(string id, string view)
        {
            if (id == "car/rivals" || id.StartsWith("rival")) return "badges/signal_rival";
            if (id == "patrol" || view.Contains("robot")) return "badges/npc_patrol_face";
            if (id == "guard") return "badges/signal_guard";
            if (id.StartsWith("camera")) return "badges/signal_camera";
            return view.StartsWith("crowd/") ? "badges/npc_passer" : "badges/signal_police";
        }

        void EdgeAdd(string id, string view, string kind, Vector3 world, int last, float W, float H, List<int> shooters = null)
        {
            if (!Proj(world, out var s)) return; if (CueOnScreen(s, W, H)) return;
            if (edgeNow.TryGetValue(id, out var q)) { if (EdgeRank(kind) > EdgeRank(q.Kind)) q.Kind = kind; q.Last = Mathf.Max(q.Last, last); q.Shooters ??= shooters; return; }
            edgeNow[id] = new EdgeItem { Id = id, Kind = kind, World = world, Last = last, Raw = s, Face = EdgeFace(id, view), Shooters = shooters };
        }

        void CueEdges(bool on, float now, float W, float H)
        {
            edgeNow.Clear(); edgeList.Clear(); edgeUsed.Clear(); EdgeChipScreen = null; var bits = new List<string>();
            if (on)
            {
                var g = host.Game; const int Hold = 25; // ticks a watcher stays listed after its last shot
                for (int k = 0; k < 4; k++) foreach (var u in host.UnderFireNow[k]) if (g.Tick - u.Last <= Hold && host.DrawnAt(u.By) is Vector3 w) EdgeAdd(u.By, host.ViewKey(u.By), "fire", w, u.Last, W, H);
                foreach (var c in host.Callouts) if (host.Views?.Find(c.View) is Transform ct && ct) EdgeAdd(c.Watcher, c.View, c.Stage >= 3 ? "call" : "see", ct.position, g.Tick, W, H);
                foreach (var h in g.Hostiles)
                {
                    if (!h.Present || h.Down || host.DrawnAt(h.Id) is not Vector3 hw) continue;
                    if (!edgeWas.TryGetValue(h.Id, out var was) || g.Tick < was.tick) edgeWas[h.Id] = (g.Tick, hw);
                    else if (g.Tick - was.tick >= Game.Hz) { edgeWas[h.Id] = (g.Tick, hw); if ((was.at - hw).sqrMagnitude > 0.25f) edgeMoving.Add(h.Id); else edgeMoving.Remove(h.Id); }
                    if (edgeMoving.Contains(h.Id)) EdgeAdd(h.Id, host.ViewKey(h.Id), "come", hw, g.Tick, W, H);
                }
                if (cueRivalCar is Vector3 car) EdgeAdd("car/rivals", "car/rivals", "come", car, g.Tick, W, H);
                cueRivalCar = null;
                foreach (var (id, world, shooters) in cueOff) EdgeAdd(id, host.ViewKey(id), "aim", world, g.Tick, W, H, shooters);
                edgeList.AddRange(edgeNow.Values);
                edgeList.Sort((a, b) => a.Kind != b.Kind ? EdgeRank(b.Kind).CompareTo(EdgeRank(a.Kind)) : b.Last != a.Last ? b.Last.CompareTo(a.Last) : string.CompareOrdinal(a.Id, b.Id));
                // one chip for the same face doing the same thing nearby, at most four, "+n" for the rest
                var merged = new List<EdgeItem>();
                foreach (var it in edgeList)
                {
                    var q = merged.Find(x => x.Face == it.Face && x.Kind == it.Kind && (x.Raw - it.Raw).magnitude < 260f);
                    if (q != null) { q.N++; if (it.Shooters != null) { q.Shooters = q.Shooters == null ? new List<int>(it.Shooters) : new List<int>(q.Shooters); foreach (var s in it.Shooters) if (!q.Shooters.Contains(s)) q.Shooters.Add(s); } }
                    else merged.Add(it);
                }
                int shown = Mathf.Min(4, merged.Count);
                for (int i = 0; i < shown; i++) { EdgeDraw(i, merged[i], now, W, H); bits.Add($"{merged[i].Id}:{merged[i].Kind}{(merged[i].N > 1 ? "x" + merged[i].N : "")}"); }
                for (int i = shown; i < edgeChips.Count; i++) Show(edgeChips[i].Root, false);
                bool more = merged.Count > 4;
                if (more || edgeMore != null)
                {
                    if (edgeMore == null) { edgeMore = PlateDark(marks, 22f, 0.3f); edgeMore.style.position = Position.Absolute; edgeMore.style.paddingLeft = 7; edgeMore.style.paddingRight = 7; edgeMore.pickingMode = PickingMode.Ignore; edgeMoreText = Words(edgeMore, 12.5f); }
                    Show(edgeMore, more); if (more) { Text(edgeMoreText, "+" + (merged.Count - 4)); var a = edgeUsed[edgeUsed.Count - 1]; edgeMore.style.left = a.x - 16f; edgeMore.style.top = a.y + 26f; }
                }
                if (shown > 0) { var pc = edgeChips[0].At; var p0 = RuntimePanelUtils.ScreenToPanel(root.panel, Vector2.zero); var p1 = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(100f, 100f)); float sx = (pc.x - p0.x) * 100f / (p1.x - p0.x), sy = (pc.y - p0.y) * 100f / (p1.y - p0.y); EdgeChipScreen = new Vector2(sx, Screen.height - sy); }
            }
            else { foreach (var c in edgeChips) Show(c.Root, false); if (edgeMore != null) Show(edgeMore, false); }
            EdgeShown = string.Join(" ", bits);
        }

        /// <summary>Where a chip sits: from a centre shifted past the roster, out to the edge, clear of the roster, the lab's panels and the other chips.</summary>
        Vector2 EdgePlace(Vector2 raw, float W, float H)
        {
            var c = new Vector2(W / 2f + 60f, H / 2f); var d = raw - c; const float Inset = 64f;
            float k = Mathf.Min((W - c.x - Inset) / Mathf.Max(1e-3f, Mathf.Abs(d.x)), (c.y - Inset) / Mathf.Max(1e-3f, Mathf.Abs(d.y))); var p = c + d * k;
            if (column != null && column.resolvedStyle.display != DisplayStyle.None) { var cb = column.worldBound; if (p.x - 40f < cb.xMax && p.y < cb.yMax + 40f) p.x = cb.xMax + 40f; }
            if (p.y < 152f && p.x > W - 420f) p.y = 152f; // under the clock bar and the speed pills (and room for the shooters' faces above)
            float toPanel = W / Mathf.Max(1f, Screen.width);
            foreach (var r in RangePanel.Rects())
            {
                var pr = new Rect(r.x * toPanel - 56f, r.y * toPanel - 36f, r.width * toPanel + 56f, r.height * toPanel + 72f);
                if (pr.Contains(p)) p.x = pr.x;
            }
            for (int step = 0; step < 6; step++)
            {
                bool hit = EdgeCamHits(new Rect(p.x - 26f, p.y - 26f, 52f, 52f), 6f);
                foreach (var u in edgeUsed) if ((u - p).magnitude < 58f) hit = true;
                if (!hit) break;
                if (p.x <= 200f || p.x >= W - Inset - 1f) p.y += 60f; else p.x -= 60f;
            }
            p.y = Mathf.Min(p.y, H - 40f); edgeUsed.Add(p); return p;
        }

        EdgeChip NewEdgeChip()
        {
            var c = new EdgeChip(); c.Root = E(marks, null, 0, 0, 44, 44, pick: true); c.Root.style.overflow = Overflow.Visible; c.Root.name = "edge-chip";
            c.Arrow = P(c.Root, -30, -30, 104, 104, (p, r) => EdgeArrow(p, c)); c.Arrow.pickingMode = PickingMode.Ignore;
            c.Cam = Cameo(c.Root, 38f, 2f); c.Cam.pickingMode = PickingMode.Ignore;
            c.Badge = E(c.Root, null, 27, 27, 32, 32); c.Badge.pickingMode = PickingMode.Ignore; c.BadgeIcon = E(c.Badge, null, 10, 10, 12, 12); c.BadgeIcon.pickingMode = PickingMode.Ignore;
            c.Count = PlateDark(c.Root, 20f, 0.3f); c.Count.style.position = Position.Absolute; c.Count.style.left = 30; c.Count.style.top = -14; c.Count.style.paddingLeft = 6; c.Count.style.paddingRight = 6; c.Count.pickingMode = PickingMode.Ignore; c.CountText = Words(c.Count, 12.5f);
            c.Root.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.LookAt(c.World); e.StopPropagation(); } });
            c.Root.RegisterCallback<PointerEnterEvent>(_ => EyeTipShow(c.Root, c.Say)); c.Root.RegisterCallback<PointerLeaveEvent>(_ => EyeTipShow(null, null));
            return c;
        }

        void EdgeDraw(int i, EdgeItem it, float now, float W, float H)
        {
            while (edgeChips.Count <= i) edgeChips.Add(NewEdgeChip());
            var c = edgeChips[i]; bool same = c.Id == it.Id; c.Id = it.Id; c.World = it.World; c.Say = EdgeSay(it.Kind); Show(c.Root, true);
            var want = EdgePlace(it.Raw, W, H); c.At = same ? Vector2.Lerp(c.At, want, 1f - Mathf.Exp(-Mathf.Min(Time.unscaledDeltaTime, 0.1f) / 0.1f)) : want;
            var d = it.Raw - c.At; c.Angle = Mathf.Atan2(d.y, d.x); c.Fire = it.Kind == "fire";
            if (c.Fire && it.Last != c.Last) c.FiredAt = now; c.Last = it.Last;
            c.Root.style.left = c.At.x - 22f; c.Root.style.top = c.At.y - 22f; c.Root.BringToFront(); c.Arrow.MarkDirtyRepaint();
            if (c.Face != it.Face) { c.Face = it.Face; Bg(c.Cam.Q("face"), it.Face, Color.white); }
            bool aim = it.Kind == "aim"; Show(c.Badge, !aim);
            if (!aim)
            {
                Bg(c.Badge, c.Fire ? "markers/disc_light_24" : "markers/disc_dark_24", Color.white);
                Icon(c.BadgeIcon, it.Kind switch { "fire" => "fire", "call" => "radio", "come" => "s_moving", _ => "sawyou" }, c.Fire ? Ink : Cream);
            }
            Show(c.Count, it.N > 1); if (it.N > 1) Text(c.CountText, it.N.ToString());
            int faces = it.Shooters?.Count ?? 0; float top = c.At.y < 90f ? 46f : -22f;
            while (c.Faces.Count < Mathf.Min(4, faces)) { var f = Cameo(c.Root, 14f, 1.5f); f.style.position = Position.Absolute; f.pickingMode = PickingMode.Ignore; c.Faces.Add(f); }
            for (int n = 0; n < c.Faces.Count; n++) { bool has = n < faces; Show(c.Faces[n], has); if (!has) continue; CameoOf(c.Faces[n], it.Shooters[n]); c.Faces[n].style.left = 22f - 9f - 8f * (Mathf.Min(4, faces) - 1) + n * 16f; c.Faces[n].style.top = top; }
        }

        /// <summary>The arrow out from the face toward the watcher; a red ring leaves on each shot.</summary>
        void EdgeArrow(Painter2D p, EdgeChip c)
        {
            var o = new Vector2(52f, 52f); float a = c.Angle; Vector2 R(float x, float y) => o + new Vector2(x * Mathf.Cos(a) - y * Mathf.Sin(a), x * Mathf.Sin(a) + y * Mathf.Cos(a));
            p.fillColor = Chrome; p.BeginPath(); p.MoveTo(R(44f, 0f)); p.LineTo(R(20f, -14f)); p.LineTo(R(20f, 14f)); p.ClosePath(); p.Fill();
            p.fillColor = TailBody; p.BeginPath(); p.MoveTo(R(39f, 0f)); p.LineTo(R(20f, -10f)); p.LineTo(R(20f, 10f)); p.ClosePath(); p.Fill();
            float age = Time.unscaledTime - c.FiredAt;
            if (c.Fire && age < 0.5f) { p.strokeColor = Red; p.lineWidth = 3f * (1f - age / 0.5f) + 1f; p.BeginPath(); p.Arc(o, 24f + age * 16f, Deg(0), Deg(360)); p.Stroke(); }
        }
    }
}
