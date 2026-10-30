using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 084: the cue switches and values, one body in two places: under the range's panel, and in the P panel (the Handover, where
    /// they start off). IMGUI, as the range's panel and the P panel are: a lab's tool, none of it reaches the player.</summary>
    public static class FightCuesPanel
    {
        /// <summary>The rows' rects on screen (GUI space, pixels) as last drawn: the proof clicks them.</summary>
        public static readonly Dictionary<string, Rect> Controls = new();
        public const float BoxH = 400f;
        static GUIStyle small, way; static float lastEdit = -1f; static string copied = ""; static float copiedAt = -9f;
        static readonly string[] Ways = { "As built", "Pick" }, ShotWays = { "As built", "Hot flash", "Hot + jolt" }, Floors = { "Grey", "Paving" };

        static void Mark(string name, Vector2 origin, float k)
        {
            if (Event.current.type != EventType.Repaint) return;
            var l = GUILayoutUtility.GetLastRect(); Controls[name] = new Rect((origin.x + l.x) * k, (origin.y + l.y) * k, l.width * k, l.height * k);
        }

        static bool Row(string word, string name, bool on, Vector2 o, float k)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(word, small, GUILayout.Width(100));
            int was = on ? 1 : 0, now = GUILayout.Toolbar(was, Ways, way, GUILayout.Height(20)); Mark(name, o, k); GUILayout.EndHorizontal();
            return now == 1;
        }

        static float Slide(string word, string name, float v, float lo, float hi, string fmt, Vector2 o, float k)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(word, small, GUILayout.Width(100));
            float n = GUILayout.HorizontalSlider(v, lo, hi, GUILayout.Height(16)); Mark(name, o, k);
            GUILayout.Label(v.ToString(fmt), small, GUILayout.Width(48)); GUILayout.EndHorizontal();
            if (!Mathf.Approximately(n, v)) lastEdit = Time.unscaledTime;
            return n;
        }

        /// <summary>The body. `o`: its top left in GUI units (the rects for the proof); `k`: GUI units to pixels; `range`: the floor row.</summary>
        public static void Body(bool range, Vector2 o, float k)
        {
            small ??= new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.96f, 0.93f, 0.85f) } };
            if (way == null)
            {   // which way a switch is thrown, at a glance: the kit's turquoise under ink (a lab's tool: Glyph has not been asked to dress it)
                var on = new Texture2D(1, 1); on.SetPixel(0, 0, new Color(0.376f, 0.878f, 0.918f)); on.Apply();
                way = new GUIStyle(GUI.skin.button) { fontSize = 12, onNormal = { background = on, textColor = new Color(0.043f, 0.086f, 0.082f) }, onHover = { background = on, textColor = new Color(0.043f, 0.086f, 0.082f) }, onActive = { background = on, textColor = new Color(0.043f, 0.086f, 0.082f) } };
            }
            var v = FightCues.V;
            GUILayout.BeginHorizontal(); GUILayout.Label("CUES", small, GUILayout.Width(100));
            int all = FightCues.AllPick ? 1 : FightCues.Any ? -1 : 0, na = GUILayout.Toolbar(all, new[] { "As built", "Glyph's pick" }, way, GUILayout.Height(22)); Mark("all", o, k); GUILayout.EndHorizontal();
            if (na != all && na >= 0) FightCues.SetAll(na == 1);
            FightCues.Mine = Row("Who's mine", "mine", FightCues.Mine, o, k);
            FightCues.Target = Row("Target", "target", FightCues.Target, o, k);
            FightCues.Incoming = Row("Incoming", "incoming", FightCues.Incoming, o, k);
            GUILayout.BeginHorizontal(); GUILayout.Label("Shots", small, GUILayout.Width(100));
            FightCues.Shots = (FightCues.ShotLook)GUILayout.Toolbar((int)FightCues.Shots, ShotWays, way, GUILayout.Height(20)); Mark("shots", o, k); GUILayout.EndHorizontal();
            FightCues.Edge = Row("Edge icons", "edge", FightCues.Edge, o, k);
            FightCues.Blind = Row(range ? "Blind (Handover)" : "Blind watchers", "blind", FightCues.Blind, o, k);
            if (range)
            {
                GUILayout.BeginHorizontal(); GUILayout.Label("Floor", small, GUILayout.Width(100));
                int fw = v.paving ? 1 : 0, nf = GUILayout.Toolbar(fw, Floors, way, GUILayout.Height(20)); Mark("floor", o, k); GUILayout.EndHorizontal();
                if (nf != fw) { v.paving = nf == 1; FightCues.FloorVersion++; lastEdit = Time.unscaledTime; }
            }
            GUILayout.Space(4);
            v.flashSize = Slide("Flash size", "flashSize", v.flashSize, 0.2f, 2.5f, "0.00", o, k);
            v.flashTime = Slide("Flash time", "flashTime", v.flashTime, 0.03f, 0.5f, "0.00 s", o, k);
            v.sparkTime = Slide("Spark time", "sparkTime", v.sparkTime, 0.05f, 0.8f, "0.00 s", o, k);
            v.knockPx = Slide("Knock", "knockPx", v.knockPx, 0f, 50f, "0 px", o, k);
            v.knockTime = Slide("Knock time", "knockTime", v.knockTime, 0.1f, 1.2f, "0.00 s", o, k);
            v.whiteTime = Slide("White flash", "whiteTime", v.whiteTime, 0f, 0.5f, "0.00 s", o, k);
            v.kickPx = Slide("Kick", "kickPx", v.kickPx, 0f, 25f, "0 px", o, k);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Glyph's numbers", GUILayout.Height(20))) { bool pave = v.paving; FightCues.Reset(); FightCues.V.paving = pave; FightCues.FloorVersion++; }
            Mark("reset", o, k);
            if (GUILayout.Button(Time.unscaledTime - copiedAt < 2f ? copied : "Copy values", GUILayout.Height(20))) { GUIUtility.systemCopyBuffer = FightCues.V.Line(); copied = "Copied"; copiedAt = Time.unscaledTime; }
            GUILayout.EndHorizontal();
            if (lastEdit > 0f && Time.unscaledTime - lastEdit > 0.4f && Event.current.type == EventType.Repaint) { lastEdit = -1f; FightCues.Save(); }
        }
    }
}
