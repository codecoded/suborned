using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 029, the fight range's own controls (a graybox tool for Sky; Glyph reviews the look once), up to engine 0.18: the spawn
    /// panel issuing the engine's `spawn` order (so a run replays): police, rivals, the response, bystanders (how many, where), or a car on a
    /// car slot with someone inside or nobody (Game.RangeKinds, RangeCarWith, RangeSlotList); the layout (open, dense, lane: Game.RangeLayouts);
    /// the preset (today, lab: from a restart); the fight mode (realtime, freeze, target: live); ½× time; R to restart; the two fire settings Sky
    /// compares here (the follow setting: agents near the leader fire with it or lock its target; map fire while held or until down); and
    /// "numbers": every line of fire now (Keel's RangeLines), each labelled with the engine's own plain line (FireLine.Text: the chance, the
    /// distance, cover, hits in n shots, aim kept, walking or running, steadying, trigger, following, locked on, answering your fire), so the
    /// range teaches the rules. Each cover piece wears a one-word label. Read only, apart from the orders, settings and restarts it gives.</summary>
    public sealed class RangePanel : MonoBehaviour
    {
        HandoverHost h;
        static readonly string[] Kinds = { "police", "rivals", "response", "bystanders", "car" }, KindWords = { "Police", "Rivals", "Response", "Bystanders", "Car" };
        static readonly string[] Places = { "near", "eff", "beyond", "cover" }, PlaceWords = { "Near 8 m", "Effective 15 m", "Beyond 18 m", "Cover 20 m+" };
        static readonly string[] Withs = { "", "police", "rivals", "bystander" }, WithWords = { "Empty", "Police", "Rival", "Bystander" };
        static readonly string[] Layouts = { "open", "dense", "lane" }, LayoutWords = { "Open", "Dense", "Lane 90 m" };
        int kind, place = 3, count = 3, carSlot, with = 1; bool numbers = true; string last = ""; float lastAt = -9f;
        readonly List<LineRenderer> pool = new(); Material mat;
        GUIStyle box, label, small, tag;

        void Awake() => h = GetComponent<HandoverHost>();

        void Update()
        {
            int used = 0;
            if (h != null && h.rangeMode && h.Game != null && numbers && !h.Following)
            {
                if (!mat) { var sh = Shader.Find("Universal Render Pipeline/Unlit"); mat = new Material(sh) { name = "range-lines" }; }
                foreach (var l in Shown())
                {
                    var lr = Line(used++); var a = h.frame.World(l.From) + Vector3.up * 1.2f; var b = h.frame.World(l.To) + Vector3.up * 1.2f;
                    lr.SetPosition(0, a); lr.SetPosition(1, b);
                    bool agent = IsOp(l.Shooter); var c = agent ? HandoverHost.CellTurquoise : l.Shooter.StartsWith("rival") ? HandoverHost.RivalViolet : HandoverHost.PoliceAmber;
                    if (agent && l.Bystander) c = new Color(0.92f, 0.92f, 0.88f); // a line onto a bystander: pale, never the cell's colour
                    lr.startColor = lr.endColor = new Color(c.r, c.g, c.b, l.InReach ? 0.95f : 0.4f); lr.widthMultiplier = l.InReach ? 0.06f : 0.03f; lr.enabled = true;
                }
            }
            for (int i = used; i < pool.Count; i++) pool[i].enabled = false;
        }

        LineRenderer Line(int i)
        {
            while (pool.Count <= i)
            {
                var go = new GameObject("range-line"); go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>(); lr.positionCount = 2; lr.useWorldSpace = true; lr.sharedMaterial = mat; lr.numCapVertices = 2;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false; pool.Add(lr);
            }
            return pool[i];
        }

        static bool IsOp(string id) => id is "scout" or "fixer" or "tech" or "enforcer";

        /// <summary>The lines to show: every line from the selected agent; the hostiles' lines in reach or aimed; another agent's line only onto
        /// what it aims at (a bystander or a car never gets a line from every agent at once).</summary>
        public List<FireLine> Shown()
        {
            if (shownFrame == Time.frameCount && shown != null) return shown; // once a frame (OnGUI runs per event)
            shownFrame = Time.frameCount; return shown = ShownNow();
        }
        List<FireLine> shown; int shownFrame = -1;
        List<FireLine> ShownNow()
        {
            var all = h.Game.RangeLines(); var sel = -1; for (int k = 0; k < 4; k++) if (h.Selected[k]) { sel = k; break; }
            string me = sel >= 0 ? Op.Name((OpId)sel) : "";
            return all.FindAll(l => l.Shooter == me || l.Aimed || !IsOp(l.Shooter) && l.InReach || IsOp(l.Shooter) && l.InReach && !l.Bystander && !l.Car && l.Mode.Length > 0);
        }

        void Styles()
        {
            if (box != null) return;
            var bg = new Texture2D(1, 1); bg.SetPixel(0, 0, new Color(0.06f, 0.16f, 0.16f, 0.92f)); bg.Apply();
            box = new GUIStyle(GUI.skin.box) { normal = { background = bg }, padding = new RectOffset(10, 10, 8, 8) };
            label = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.96f, 0.93f, 0.85f) } };
            small = new GUIStyle(label) { fontSize = 12 };
            tag = new GUIStyle(GUI.skin.box) { fontSize = 13, normal = { background = bg, textColor = new Color(0.96f, 0.93f, 0.85f) }, padding = new RectOffset(5, 5, 2, 2) };
        }

        void OnGUI()
        {
            if (h == null || !h.rangeMode || h.Game == null) return;
            Styles(); var g = h.Game; var cam = UnityEngine.Camera.main;
            float k = Screen.height / 1080f; GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1f)); float W = Screen.width / k;
            // labels: each cover piece by kind, and each line of fire in the engine's plain words
            if (cam)
            {
                foreach (var o in g.S.Occluders)
                {
                    if (!o.Id.StartsWith("range/")) continue; var kindW = o.Id.StartsWith("range/low-wall") ? "low wall" : o.Id.StartsWith("range/high-wall") ? "high wall" : o.Id.StartsWith("range/pillar") ? "pillar" : o.Id.StartsWith("range/car-box") ? "box" : "";
                    if (kindW.Length == 0) continue; long sx = 0, sy = 0; foreach (var p in o.Poly) { sx += p.X; sy += p.Y; }
                    var at = h.frame.World(new Pt((int)(sx / o.Poly.Length), (int)(sy / o.Poly.Length))) + Vector3.up * (o.H / 100f + 0.2f);
                    Tag(cam, at, kindW, k, small);
                }
                if (numbers && !h.Following)
                    foreach (var l in Shown())
                    {
                        // the label sits a third of the way from the shooter, so two lines onto one target don't stack their words
                        var at = Vector3.Lerp(h.frame.World(l.From), h.frame.World(l.To), 0.38f) + Vector3.up * 1.4f;
                        Tag(cam, at, l.Text.Length > 0 ? l.Text : $"{l.Acc / 10}% · {l.DistanceCm / 100} m", k, tag);
                    }
            }
            // the panel, top right under the clock
            bool car = Kinds[kind] == "car";
            var r = new Rect(W - 470, 120, 456, 420); GUI.Box(r, GUIContent.none, box);
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 10, r.width - 24, r.height - 20));
            GUILayout.Label("FIGHT RANGE", label);
            kind = GUILayout.Toolbar(kind, KindWords);
            if (car)
            {
                var slots = g.RangeSlotList().FindAll(s => s.Placement == "car");
                var sw = new List<string> { "Any free" }; foreach (var s in slots) sw.Add($"{s.K}: {s.DistanceCm / 100} m{(s.Held ? " ·held" : "")}");
                GUILayout.BeginHorizontal(); GUILayout.Label("Slot", small, GUILayout.Width(80)); carSlot = Mathf.Min(GUILayout.Toolbar(carSlot, sw.ToArray()), sw.Count - 1); GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(); GUILayout.Label("Inside", small, GUILayout.Width(80)); with = GUILayout.Toolbar(with, WithWords); GUILayout.EndHorizontal();
                if (GUILayout.Button(slots.Count == 0 ? "This layout has no car slot" : $"Spawn a car · {(carSlot == 0 ? "first free slot" : "slot " + carSlot)} · {(with == 0 ? "empty" : WithWords[with].ToLowerInvariant() + " inside")}") && slots.Count > 0)
                {
                    var o = new Order { Do = "spawn", Target = "car", To = carSlot == 0 ? "car" : $"car:{carSlot}", Count = 1, With = Withs[with] };
                    bool ok = h.Issue(o); last = ok ? "car spawned" : $"refused: {h.Status}"; lastAt = Time.unscaledTime;
                }
            }
            else
            {
                GUILayout.BeginHorizontal(); GUILayout.Label("How many", small, GUILayout.Width(80)); count = GUILayout.Toolbar(count - 1, new[] { "1", "2", "3", "4", "5", "6" }) + 1; GUILayout.EndHorizontal();
                place = GUILayout.Toolbar(place, PlaceWords);
                if (GUILayout.Button($"Spawn {count} {KindWords[kind].ToLowerInvariant()} · {PlaceWords[place]}"))
                {
                    var o = new Order { Do = "spawn", Target = Kinds[kind], To = Places[place], Count = count };
                    bool ok = h.Issue(o); last = ok ? $"spawned {count} {Kinds[kind]} at {Places[place]}" : $"refused: {h.Status}"; lastAt = Time.unscaledTime;
                }
            }
            GUILayout.Space(6);
            GUILayout.BeginHorizontal(); GUILayout.Label("Layout", small, GUILayout.Width(80));
            int lay = Mathf.Max(0, System.Array.IndexOf(Layouts, h.rangeLayout)), nl = GUILayout.Toolbar(lay, LayoutWords); GUILayout.EndHorizontal();
            if (nl != lay) { h.rangeLayout = Layouts[nl]; h.Begin(); }
            GUILayout.BeginHorizontal(); GUILayout.Label("Preset", small, GUILayout.Width(80));
            int pr = h.Fights.preset == "lab" ? 1 : 0, np = GUILayout.Toolbar(pr, new[] { "Today", "Lab" }); GUILayout.EndHorizontal();
            if (np != pr) { h.Fights.Load(np == 1 ? "lab" : "today"); h.Begin(); }
            GUILayout.BeginHorizontal(); GUILayout.Label("Fight mode", small, GUILayout.Width(80));
            int fm = System.Array.IndexOf(FightLab.Modes, h.Fights.fightMode); int nf = GUILayout.Toolbar(Mathf.Max(0, fm), new[] { "Realtime", "Freeze", "Target" }); GUILayout.EndHorizontal();
            if (nf != fm) h.Fights.fightMode = FightLab.Modes[nf];
            // the two fire settings (the pause menu's own), side by side here so Sky can compare them on the range
            GUILayout.BeginHorizontal(); GUILayout.Label("Agents near", small, GUILayout.Width(80));
            int fl = h.controls.followLock ? 1 : 0, nfl = GUILayout.Toolbar(fl, new[] { "Fire with you", "Lock your target" }); GUILayout.EndHorizontal();
            if (nfl != fl) h.SetFollowLock(nfl == 1);
            GUILayout.BeginHorizontal(); GUILayout.Label("Map fire", small, GUILayout.Width(80));
            int hf = h.controls.commanderHoldFire ? 1 : 0, nhf = GUILayout.Toolbar(hf, new[] { "Until down", "While held" }); GUILayout.EndHorizontal();
            if (nhf != hf) { h.controls.commanderHoldFire = nhf == 1; h.controls.SaveSettings(); }
            GUILayout.BeginHorizontal(); h.rangeHalf = GUILayout.Toggle(h.rangeHalf, " ½× time"); numbers = GUILayout.Toggle(numbers, " Numbers"); if (GUILayout.Button("Restart (R)")) h.Begin(); GUILayout.EndHorizontal();
            GUILayout.Label(Tally(g) + (Time.unscaledTime - lastAt < 4f ? " · " + last : ""), small);
            GUILayout.EndArea();
            GUI.matrix = Matrix4x4.identity;
        }

        /// <summary>One line: hostiles up, bystanders on the floor, fled and witnesses, cars standing and burning.</summary>
        static string Tally(Game g)
        {
            var hs = g.RangeHostiles(); int up = hs.FindAll(x => !x.Down && !x.Gone).Count;
            var s = $"hostiles up {up} of {hs.Count}";
            var bs = g.RangeBystanders();
            if (bs.Count > 0) s += $" · bystanders {bs.FindAll(b => b.State is "fleeing" or "safe").Count} fled, {bs.FindAll(b => b.Saw.Length > 0).Count} saw you, {bs.FindAll(b => b.Down).Count} down";
            var cs = g.RangeCars();
            if (cs.Count > 0) s += $" · cars {cs.FindAll(c => c.State == "ok").Count} ok, {cs.FindAll(c => c.State is "disabled" or "burning").Count} hit, {cs.FindAll(c => c.State == "wrecked").Count} blown";
            return s;
        }

        static void Tag(UnityEngine.Camera cam, Vector3 world, string text, float k, GUIStyle st)
        {
            var s = cam.WorldToScreenPoint(world); if (s.z <= 0f) return;
            var c = new GUIContent(text); var size = st.CalcSize(c); var x = s.x / k - size.x / 2f; var y = (Screen.height - s.y) / k - size.y / 2f;
            GUI.Label(new Rect(x, y, size.x, size.y), c, st);
        }
    }
}
