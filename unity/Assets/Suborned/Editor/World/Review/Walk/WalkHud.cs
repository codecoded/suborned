using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>The review tool's overlay (Play Mode only): the readout (mode, position, the plan ids underfoot and under the
    /// crosshair or cursor), the key list, the jump list with its filter, and the one-line note entry. It reports what the
    /// reviewer chose; the rig acts. IMGUI paints it and nothing more: with the Input System as the only input handler a Play
    /// Mode OnGUI gets no key or mouse events, so the list and the fields are driven from the WalkFrame (Tick).</summary>
    public sealed class WalkHud
    {
        static readonly string[] KeyList =
        {
            "<b>WALK REVIEW</b>  a review tool, not the game",
            "",
            "<b>1</b> Walk   <b>2</b> Fly   <b>3</b> Tactical   (<b>Tab</b> next)",
            "<b>W A S D</b>  move / pan      <b>Shift</b>  run / faster",
            "<b>drag</b> (hold a mouse button) or <b>arrows</b>  look;  in Tactical drag pans",
            "<b>Q / E</b>  Walk: level down / up here    Fly: down / up",
            "<b>wheel</b> or <b>[ ]</b>  Fly: speed    Tactical: zoom    <b>Z</b>  play zoom",
            "<b>F</b>  Tactical: architecture fade on / off",
            "<b>G</b>  places: type to filter, <b>Up/Down</b>, <b>Enter</b> jumps, or click one",
            "<b>N</b>  note: type one line, <b>Enter</b> saves it with a screenshot",
            "<b>H</b>  this list      <b>Esc</b>  close a panel",
        };

        const float Pad = 8f, Line = 18f, FieldHeight = 22f;
        const int MaxFilter = 60, MaxNote = 400, WheelLines = 3;

        enum Field { None, Places, Note }

        public bool ShowKeys = true;
        /// <summary>The reviewer picked a place, or entered a note.</summary>
        public Action<Place> Picked;
        public Action<string> Noted;

        readonly IReadOnlyList<Place> all;
        readonly List<Place> shown = new();                      // the places that match the filter
        readonly List<(string head, int place)> lines = new();   // the list as drawn: a group heading, or an index into shown
        readonly List<(Rect at, int place)> rows = new();        // the place rows on screen (overlay units), for clicks
        readonly List<Rect> panels = new();                      // every backed block on screen (overlay units), for Over
        Field open;
        string text = "", toast = "";
        float toastUntil, swallowUntil, wheel, scale = 1f;
        char swallow;
        int selected, first, fit = 1; // the highlighted match, the first line drawn, the lines that fit
        Vector2 pointer;              // overlay units
        GUIStyle label, panel, head, hover, pick, entry;

        public WalkHud(IReadOnlyList<Place> places) { all = places; }

        /// <summary>A text field has the keyboard: the camera keys are off.</summary>
        public bool Typing => open != Field.None;

        public void OpenPlaces() => Open(Field.Places, 'g');
        public void OpenNote() => Open(Field.Note, 'n');
        public void Say(string message) { toast = message; toastUntil = Time.unscaledTime + 4f; }

        /// <summary>The key that opened a field may also arrive as its first typed character: that one is dropped.</summary>
        void Open(Field field, char key)
        {
            open = field; text = ""; selected = first = 0; rows.Clear();
            swallow = key; swallowUntil = Time.unscaledTime + 0.25f;
            if (field == Field.Places) Match();
        }

        /// <summary>Is the pointer (pixels, origin bottom-left) over a panel?</summary>
        public bool Over(Vector2 pixels)
        {
            var p = Overlay(pixels);
            foreach (var r in panels) if (r.Contains(p)) return true;
            return false;
        }

        Vector2 Overlay(Vector2 pixels) => new Vector2(pixels.x, Screen.height - pixels.y) / scale;

        /// <summary>The open field's share of a frame: typed characters, Backspace, Enter, Esc; the places list also takes the
        /// up and down arrows, the wheel over it, and a click on a row.</summary>
        public void Tick(in WalkFrame f)
        {
            pointer = Overlay(f.Pointer);
            if (open == Field.None) return;
            if (f.Escape) { open = Field.None; return; }

            int before = text.Length;
            foreach (char c in f.Typed ?? "")
            {
                if (Time.unscaledTime < swallowUntil && char.ToLowerInvariant(c) == swallow) { swallowUntil = 0f; continue; }
                if (text.Length < (open == Field.Note ? MaxNote : MaxFilter)) text += c;
            }
            if (f.Backspace && text.Length > 0) text = text.Substring(0, text.Length - 1);

            if (open == Field.Note)
            {
                if (!f.Enter) return;
                open = Field.None;
                if (text.Trim().Length > 0) Noted?.Invoke(text.Trim());
                return;
            }

            if (text.Length != before || f.Backspace) { selected = first = 0; Match(); }
            if (f.Row != 0 && shown.Count > 0)
            {
                selected = Mathf.Clamp(selected + f.Row, 0, shown.Count - 1);
                int line = lines.FindIndex(l => l.head == null && l.place == selected);
                if (line - 1 < first) first = Mathf.Max(line - 1, 0); // its heading too
                else if (line >= first + fit) first = line - fit + 1;
            }
            if (f.Wheel != 0f && Over(f.Pointer))
            {
                wheel += f.Wheel * WheelLines; int whole = (int)wheel; wheel -= whole; // a trackpad scrolls by fractions of a line
                first = Mathf.Clamp(first - whole, 0, Mathf.Max(lines.Count - fit, 0));
            }
            if (f.Click)
                foreach (var (at, place) in rows)
                    if (at.Contains(pointer)) { Pick(shown[place]); return; }
            if (f.Enter && shown.Count > 0) Pick(shown[selected]);
        }

        void Pick(Place p) { open = Field.None; Picked?.Invoke(p); }

        /// <summary>The places whose group or name contains the filter, under their group headings.</summary>
        void Match()
        {
            shown.Clear(); lines.Clear();
            string group = null;
            foreach (var p in all)
            {
                if (text.Length > 0 && (p.Group + " " + p.Name).IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (p.Group != group) { group = p.Group; lines.Add((group, -1)); }
                lines.Add((null, shown.Count));
                shown.Add(p);
            }
        }

        public void Draw(IWalkMode mode, string[] readout)
        {
            if (Event.current.type != EventType.Repaint) return;
            Styles();
            scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = Screen.height / scale;
            panels.Clear();

            Block(new Rect(Pad, Pad, Mathf.Min(w - 2f * Pad, 860f), 0f), readout);
            if (ShowKeys) Block(new Rect(Pad, h - Pad - (KeyList.Length * Line + 2f * Pad), 480f, 0f), KeyList);
            else GUI.Label(new Rect(Pad, h - Pad - Line, 400f, Line), "<b>H</b> keys   <b>G</b> places   <b>N</b> note", label);
            if (!mode.Camera.orthographic) GUI.Label(new Rect(w * 0.5f - 6f, h * 0.5f - 10f, 12f, 20f), "+", label);
            if (Time.unscaledTime < toastUntil) Block(new Rect(w * 0.5f - 260f, h - 110f, 520f, 0f), new[] { toast });
            if (open == Field.Places) Places(w, h);
            if (open == Field.Note) Note(w, h);
        }

        void Places(float w, float h)
        {
            var box = new Rect(w - 380f - Pad, Pad, 380f, h - 2f * Pad);
            Panel(box);
            float x = box.x + Pad, y = box.y + Pad, wide = box.width - 2f * Pad;
            GUI.Label(new Rect(x, y, wide, Line), "<b>Places</b>   type to filter · Up/Down · Enter jumps · Esc closes", label); y += Line + 2f;
            Entry(new Rect(x, y, wide, FieldHeight)); y += FieldHeight + 6f;
            fit = Mathf.Max(1, Mathf.FloorToInt((box.yMax - Pad - Line - y) / Line));
            rows.Clear();
            int end = Mathf.Min(lines.Count, first + fit);
            for (int i = first; i < end; i++, y += Line)
            {
                var at = new Rect(x, y, wide, Line);
                if (lines[i].head != null) { GUI.Label(at, lines[i].head, head); continue; }
                int place = lines[i].place;
                rows.Add((at, place));
                if (place == selected) GUI.Box(at, GUIContent.none, pick);
                GUI.Label(new Rect(x + 6f, y, wide - 6f, Line), shown[place].Name, at.Contains(pointer) ? hover : label);
            }
            string count = shown.Count == 0 ? "no place matches" : $"{shown.Count} places";
            GUI.Label(new Rect(x, box.yMax - Pad - Line, wide, Line), end < lines.Count || first > 0 ? count + " · the wheel scrolls" : count, label);
        }

        void Note(float w, float h)
        {
            var box = new Rect(w * 0.5f - 330f, h - 70f, 660f, 58f);
            Panel(box);
            GUI.Label(new Rect(box.x + Pad, box.y + 5f, box.width - 2f * Pad, Line), "<b>Note</b>   Enter saves it here with a screenshot · Esc cancels", label);
            Entry(new Rect(box.x + Pad, box.y + 5f + Line + 2f, box.width - 2f * Pad, FieldHeight));
        }

        /// <summary>The open field's text with a blinking caret; a line longer than the field shows its end.</summary>
        void Entry(Rect at)
        {
            GUI.Box(at, GUIContent.none, pick);
            var inner = new Rect(at.x + 4f, at.y, at.width - 8f, at.height);
            var content = new GUIContent(text + ((int)(Time.unscaledTime * 2f) % 2 == 0 ? "|" : " "));
            entry.alignment = entry.CalcSize(content).x > inner.width ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            GUI.Label(inner, content, entry);
        }

        /// <summary>A backed block of lines, as tall as it needs.</summary>
        void Block(Rect at, string[] content)
        {
            at.height = content.Length * Line + 2f * Pad;
            Panel(at);
            for (int i = 0; i < content.Length; i++) GUI.Label(new Rect(at.x + Pad, at.y + Pad + i * Line, at.width - 2f * Pad, Line), content[i], label);
        }

        void Panel(Rect r)
        {
            panels.Add(r);
            GUI.Box(r, GUIContent.none, panel);
        }

        void Styles()
        {
            if (label != null) return;
            panel = new GUIStyle { normal = { background = Fill(new Color(0.05f, 0.06f, 0.08f, 0.78f)) } };
            pick = new GUIStyle { normal = { background = Fill(new Color(1f, 1f, 1f, 0.16f)) } };
            label = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset(0, 0, 0, 0), normal = { textColor = Color.white } };
            head = new GUIStyle(label) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.82f, 0.45f) } };
            hover = new GUIStyle(label) { normal = { textColor = new Color(0.55f, 0.85f, 1f) } };
            entry = new GUIStyle(label) { richText = false }; // a note may hold a '<'
        }

        static Texture2D Fill(Color c)
        {
            var t = new Texture2D(1, 1); // no hide flags: it goes when the Play session does
            t.SetPixel(0, 0, c); t.Apply();
            return t;
        }
    }
}
