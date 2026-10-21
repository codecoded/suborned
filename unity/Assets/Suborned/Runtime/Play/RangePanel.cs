using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 029, the fight range's own controls (a graybox tool for Sky; Glyph reviews the look once): the spawn panel (side, how many,
    /// where) issuing the engine's `spawn` order (so a run replays), the layout (open, dense), the preset (today, lab: from a restart), the fight
    /// mode (realtime, freeze, target: live), ½× time, R to restart, and "numbers": every line of fire now (Keel's RangeLines: in reach or aimed,
    /// or all from the selected agent), each labelled in one line with the hit chance (pips with the lab, per cent today), the range band and the
    /// cover the shot counts. Each cover piece wears a one-word label. Read only, apart from the orders and restarts it gives.</summary>
    public sealed class RangePanel : MonoBehaviour
    {
        HandoverHost h;
        static readonly string[] Sides = { "police", "rivals", "response" }, SideWords = { "Police", "Rivals", "Response" };
        static readonly string[] Places = { "near", "eff", "beyond", "cover" }, PlaceWords = { "Near 8 m", "Effective 15 m", "Beyond 18 m", "Cover 20 m+" };
        int side, place = 3, count = 3; bool numbers = true; string last = ""; float lastAt = -9f;
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

        /// <summary>The lines to show: from the selected agent, every line; else the ones in reach or aimed (Keel's advice).</summary>
        List<FireLine> Shown()
        {
            var all = h.Game.RangeLines(); var sel = -1; for (int k = 0; k < 4; k++) if (h.Selected[k]) { sel = k; break; }
            string me = sel >= 0 ? Op.Name((OpId)sel) : "";
            return all.FindAll(l => l.Shooter == me || l.InReach || l.Aimed);
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
            // labels: each cover piece by kind, and each line of fire's numbers
            if (cam)
            {
                foreach (var o in g.S.Occluders)
                {
                    if (!o.Id.StartsWith("range/")) continue; var kind = o.Id.StartsWith("range/low-wall") ? "low wall" : o.Id.StartsWith("range/high-wall") ? "high wall" : o.Id.StartsWith("range/pillar") ? "pillar" : o.Id.StartsWith("range/car-box") ? "box" : "";
                    if (kind.Length == 0) continue; long sx = 0, sy = 0; foreach (var p in o.Poly) { sx += p.X; sy += p.Y; }
                    var at = h.frame.World(new Pt((int)(sx / o.Poly.Length), (int)(sy / o.Poly.Length))) + Vector3.up * (o.H / 100f + 0.2f);
                    Tag(cam, at, kind, k, small);
                }
                if (numbers && !h.Following)
                    foreach (var l in Shown())
                    {
                        var mid = (h.frame.World(l.From) + h.frame.World(l.To)) * 0.5f + Vector3.up * 1.4f;
                        string chance = g.Lab ? new string('●', Mathf.Clamp(l.Pips, 0, 4)) + new string('○', 4 - Mathf.Clamp(l.Pips, 0, 4)) : $"{l.Acc / 10}%";
                        string cover = l.Cover == "full" ? " · full cover" : l.Cover == "partial" ? " · half cover" : "";
                        Tag(cam, mid, $"{chance} · {l.DistanceCm / 100} m {l.Band}{cover}", k, tag);
                    }
            }
            // the panel, top right under the clock
            var r = new Rect(W - 470, 120, 456, 330); GUI.Box(r, GUIContent.none, box);
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 10, r.width - 24, r.height - 20));
            GUILayout.Label("FIGHT RANGE", label);
            side = GUILayout.Toolbar(side, SideWords);
            GUILayout.BeginHorizontal(); GUILayout.Label("How many", small, GUILayout.Width(80)); count = GUILayout.Toolbar(count - 1, new[] { "1", "2", "3", "4", "5", "6" }) + 1; GUILayout.EndHorizontal();
            place = GUILayout.Toolbar(place, PlaceWords);
            if (GUILayout.Button($"Spawn {count} {SideWords[side].ToLowerInvariant()} · {PlaceWords[place]}"))
            {
                var o = new Order { Do = "spawn", Target = Sides[side], To = Places[place], Count = count };
                bool ok = h.Issue(o); last = ok ? $"spawned {count} {Sides[side]} at {Places[place]}" : $"refused: {h.Status}"; lastAt = Time.unscaledTime;
            }
            GUILayout.Space(6);
            GUILayout.BeginHorizontal(); GUILayout.Label("Layout", small, GUILayout.Width(80));
            int lay = h.rangeLayout == "dense" ? 1 : 0, nl = GUILayout.Toolbar(lay, new[] { "Open", "Dense" }); GUILayout.EndHorizontal();
            if (nl != lay) { h.rangeLayout = nl == 1 ? "dense" : "open"; h.Begin(); }
            GUILayout.BeginHorizontal(); GUILayout.Label("Preset", small, GUILayout.Width(80));
            int pr = h.Fights.preset == "lab" ? 1 : 0, np = GUILayout.Toolbar(pr, new[] { "Today", "Lab" }); GUILayout.EndHorizontal();
            if (np != pr) { h.Fights.Load(np == 1 ? "lab" : "today"); h.Begin(); }
            GUILayout.BeginHorizontal(); GUILayout.Label("Fight mode", small, GUILayout.Width(80));
            int fm = System.Array.IndexOf(FightLab.Modes, h.Fights.fightMode); int nf = GUILayout.Toolbar(Mathf.Max(0, fm), new[] { "Realtime", "Freeze", "Target" }); GUILayout.EndHorizontal();
            if (nf != fm) h.Fights.fightMode = FightLab.Modes[nf];
            GUILayout.BeginHorizontal(); h.rangeHalf = GUILayout.Toggle(h.rangeHalf, " ½× time"); numbers = GUILayout.Toggle(numbers, " Numbers"); if (GUILayout.Button("Restart (R)")) h.Begin(); GUILayout.EndHorizontal();
            var hs = g.RangeHostiles(); int up = hs.FindAll(x => !x.Down && !x.Gone).Count;
            GUILayout.Label($"hostiles up {up} of {hs.Count}{(Time.unscaledTime - lastAt < 4f ? " · " + last : "")}", small);
            GUILayout.EndArea();
            GUI.matrix = Matrix4x4.identity;
        }

        static void Tag(UnityEngine.Camera cam, Vector3 world, string text, float k, GUIStyle st)
        {
            var s = cam.WorldToScreenPoint(world); if (s.z <= 0f) return;
            var c = new GUIContent(text); var size = st.CalcSize(c); var x = s.x / k - size.x / 2f; var y = (Screen.height - s.y) / k - size.y / 2f;
            GUI.Label(new Rect(x, y, size.x, size.y), c, st);
        }
    }
}
