using System.Text;
using Suborned.Sim;
using UnityEngine;
using Camera = UnityEngine.Camera;
using Event = UnityEngine.Event;

namespace Suborned.Play
{
    /// <summary>The debug layer behind F1 (brief 011 D): the engine's build and inputs, its cost per step and per frame, the session file, the
    /// selected agents' busy state, the last engine lines, and every person's tag. Plain IMGUI, reading HandoverHost only.</summary>
    [RequireComponent(typeof(HandoverHost))]
    public sealed class DebugHud : MonoBehaviour
    {
        [System.NonSerialized] HandoverHost host; [System.NonSerialized] GUIStyle text; [System.NonSerialized] Texture2D bg; Rect panel;
        readonly StringBuilder sb = new();

        public static bool Blocks(Vector2 mouse) => false; // the debug panel takes no clicks

        void Awake() => host = GetComponent<HandoverHost>();

        void OnGUI()
        {
            if (host == null || !host.ShowDebug || host.Game == null) return;
            float s = Screen.height / 1080f;
            if (text == null || bg == null)
            {
                bg = new Texture2D(1, 1); bg.SetPixel(0, 0, new Color(0.05f, 0.06f, 0.07f, 0.82f)); bg.Apply();
                text = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(0.9f, 0.92f, 0.88f) }, richText = true, wordWrap = true, alignment = TextAnchor.UpperLeft };
            }
            text.fontSize = Mathf.RoundToInt(14 * s);
            var g = host.Game; sb.Clear();
            sb.AppendLine($"<b>engine</b> {host.Bundle.Source} · dll {host.Bundle.DllSha[..12]} · {SimBundle.Runtime}");
            sb.AppendLine($"tick {g.Tick} ({Report.Clock(g.Tick)}) · step {host.StepMsMean:F3} ms mean, {host.StepMsMax:F2} max · engine {host.FrameMsMean:F3} ms a frame");
            sb.AppendLine($"orders {g.Orders.Count} · checkpoints {g.Checkpoints.Count} · last {(g.Checkpoints.Count > 0 ? Hash.Hex(g.Checkpoints[^1].hash) : "-")}");
            sb.AppendLine($"session {host.SessionPath}");
            sb.AppendLine($"crowd on the street {CountCrowd(g)} of {g.Walkers.Count} · hostiles {g.Hostiles.Count} · fight {g.FightBranch}");
            for (int i = 0; i < 4; i++) { var op = g.Ops[i]; sb.AppendLine($"{i + 1} {Op.Name((OpId)i)} {op.Pos} {(op.Inside ? "inside" : op.Busy != null ? $"{op.Busy} → {op.BusyEnd}" : op.Walk != null ? "walking" : "ready")}"); }
            sb.AppendLine("<b>log</b>");
            for (int i = Mathf.Max(0, g.Log.Count - 10); i < g.Log.Count; i++) sb.AppendLine($"{Report.Clock(g.Log[i].Tick)} {g.Log[i].Code}: {g.Log[i].Text}");
            panel = new Rect(16 * s, 90 * s, 820 * s, 560 * s);
            GUI.DrawTexture(panel, bg);
            GUI.Label(new Rect(panel.x + 10 * s, panel.y + 8 * s, panel.width - 20 * s, panel.height - 16 * s), sb.ToString(), text);
        }

        static int CountCrowd(Game g) { int n = 0; foreach (var w in g.Walkers) if (g.CrowdPos(w).HasValue) n++; return n; }
    }
}
