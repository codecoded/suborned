using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.People
{
    /// <summary>Graybox record panel (brief 010 E; Glyph does the real HUD later): in Play Mode a left click on a person's capsule
    /// opens their record (top right, with a tag over the person), a click on empty ground or Esc closes it. Places with people
    /// inside (not drawn: exteriors only) carry a small `inside: N` tag at their door. Plain IMGUI, scaled to the screen height (laid out at 1080p).</summary>
    public sealed class PeoplePicker : MonoBehaviour
    {
        /// <summary>The snapshot the layer stands at, and where its people came from (set by the layer builder).</summary>
        public string clock, source;
        public PersonRecord selected;

        [System.NonSerialized] InsideCount[] inside;
        [System.NonSerialized] GUIStyle box, title, body, dim, bold; // never carried over a domain reload: built on the first OnGUI
        const float W = 700f, Pad = 18f;

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) selected = null;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            var p = mouse.position.ReadValue();
            if (selected && PanelRect().Contains(new Vector2(p.x, Screen.height - p.y))) return; // a click on the panel itself
            selected = PickAt(p);
        }

        /// <summary>The person under a screen point (pixels, origin bottom left) on the main camera, or null.</summary>
        public PersonRecord PickAt(Vector2 screen)
        {
            var cam = Camera.main;
            if (!cam) return null;
            return Physics.Raycast(cam.ScreenPointToRay(screen), out var hit, 5000f) ? hit.collider.GetComponentInParent<PersonRecord>() : null;
        }

        float Scale => Screen.height / 1080f;
        const float Lh = 27f, Th = 44f;

        Rect PanelRect() => new(Screen.width - (W + 24f) * Scale, 24f * Scale, W * Scale, (selected ? Pad * 2 + Th + Lh * Lines().Count : 0f) * Scale);

        /// <summary>The panel's lines: (text, dim heading?, bold?).</summary>
        List<(string text, bool dim, bool bold)> Lines()
        {
            var l = new List<(string, bool, bool)>
            {
                ($"<color=#a8b3b3>Job</color>  {selected.job}", false, false),
                ($"<color=#a8b3b3>Temperament</color>  {selected.temperament}", false, false),
                ($"<color=#a8b3b3>Need</color>  {selected.need}", false, false),
                ($"<color=#a8b3b3>Now</color>  {selected.where}", false, false),
                ("Ties", true, false),
            };
            foreach (var t in selected.ties) l.Add(("   " + t, false, false));
            l.Add(("Today", true, false));
            for (int i = 0; i < selected.routine.Length; i++) l.Add(((i == selected.now ? "▸ " : "   ") + selected.routine[i], false, i == selected.now));
            return l;
        }

        void Styles()
        {
            if (bold != null) return;
            var bg = new Texture2D(1, 1); bg.SetPixel(0, 0, new Color(0.11f, 0.14f, 0.15f, 1f)); bg.Apply();
            box = new GUIStyle { normal = { background = bg } };
            title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, wordWrap = false, clipping = TextClipping.Clip };
            body = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(0.94f, 0.91f, 0.84f) }, richText = true, wordWrap = false, clipping = TextClipping.Clip };
            dim = new GUIStyle(body) { normal = { textColor = new Color(0.66f, 0.70f, 0.70f) } };
            bold = new GUIStyle(body) { fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        }

        void OnGUI()
        {
            Styles();
            float s = Scale;
            title.fontSize = Mathf.RoundToInt(28 * s); body.fontSize = dim.fontSize = bold.fontSize = Mathf.RoundToInt(18 * s);
            var hint = $"Snapshot {clock} · click a person, Esc closes · {source}";
            var hs = body.CalcSize(new GUIContent(hint));
            GUI.Box(new Rect(16 * s, Screen.height - 46 * s, hs.x + 16 * s, 34 * s), GUIContent.none, box);
            GUI.Label(new Rect(24 * s, Screen.height - 42 * s, hs.x + 8 * s, 30 * s), hint, body);
            if (Camera.main is { } c0)
            {
                inside ??= GetComponentsInChildren<InsideCount>();
                foreach (var ic in inside)
                {
                    var sp = c0.WorldToScreenPoint(ic.transform.position);
                    if (sp.z <= 0f || sp.x < 0 || sp.x > Screen.width || sp.y < 0 || sp.y > Screen.height) continue;
                    var t = $"inside: {ic.count}";
                    var size = dim.CalcSize(new GUIContent(t)) + new Vector2(12 * s, 4 * s);
                    var r0 = new Rect(sp.x - size.x / 2, Screen.height - sp.y - size.y, size.x, size.y);
                    GUI.Box(r0, GUIContent.none, box);
                    GUI.Label(new Rect(r0.x + 6 * s, r0.y + 2 * s, size.x, size.y), t, dim);
                }
            }
            if (!selected) return;
            if (Camera.main is { } cam) // a tag over the selected person, so the panel and the capsule read as one
            {
                var sp = cam.WorldToScreenPoint(selected.transform.position + Vector3.up * 2.1f);
                if (sp.z > 0f)
                {
                    var size = body.CalcSize(new GUIContent("▼ " + selected.personName)) + new Vector2(16 * s, 6 * s);
                    var tag = new Rect(sp.x - size.x / 2, Screen.height - sp.y - size.y, size.x, size.y);
                    GUI.Box(tag, GUIContent.none, box);
                    GUI.Label(new Rect(tag.x + 8 * s, tag.y + 3 * s, size.x, size.y), "▼ " + selected.personName, bold);
                }
            }
            var r = PanelRect();
            GUI.Box(r, GUIContent.none, box);
            float x = r.x + Pad * s, y = r.y + Pad * s, w = r.width - 2 * Pad * s;
            GUI.Label(new Rect(x, y, w, Th * s), selected.personName, title); y += Th * s;
            foreach (var (text, isDim, isBold) in Lines()) { GUI.Label(new Rect(x, y, w, Lh * s), text, isDim ? dim : isBold ? bold : body); y += Lh * s; }
        }

    }
}
