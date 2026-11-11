using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 107 (Sky 2026-11-11: the weather and the light are rolled from the seed and can be changed at the briefing; engine 0.54.0): on the book's
    /// Job page (the start, brief 096), at the right of the kicker line while the clock hasn't run: Day · Night, then Clear · Overcast · Rain · Mist. A word gives
    /// the game's `light` or `weather` order (HandoverHost.ChooseLight, ChooseWeather), taken before the clock only, so the engine owns both and a saved run
    /// replays them. The chosen word is the book's deep teal with a 2 px line under it, the others its soft ink (the book's own colours, no new ones).
    /// Night is left out where the scene has none. Words are placeholders for Fable; the place and the type are Glyph's to check.</summary>
    public sealed partial class HudToolkit
    {
        static readonly (string what, string kind, string word)[] LookWords =
            { ("light", "day", "Day"), ("light", "night", "Night"), ("weather", "clear", "Clear"), ("weather", "overcast", "Overcast"), ("weather", "rain", "Rain"), ("weather", "mist", "Mist") };
        const float LookSize = 17f, LookGap = 16f, LookGroupGap = 34f, LookRight = 730f;

        void BkLook(VisualElement body, BookLayout.Item it)
        {
            if (!host.BookStart || host.Game == null || host.Game.Tick > 0) return;
            var shown = new List<(string what, string kind, string word)>();
            foreach (var w in LookWords) if (!(w.kind == "night" && !host.CanNight)) shown.Add(w);
            float total = 0f; for (int i = 0; i < shown.Count; i++) total += BkTw(shown[i].word, LookSize, "Jost-SemiBold") + (i == 0 ? 0f : shown[i].what != shown[i - 1].what ? LookGroupGap : LookGap);
            float x = it.At.x + LookRight - total, cy = it.Cy;
            var made = new List<(Label l, VisualElement line, string what, string kind)>();
            for (int i = 0; i < shown.Count; i++)
            {
                var (what, kind, word) = shown[i];
                if (i > 0) x += shown[i].what != shown[i - 1].what ? LookGroupGap : LookGap;
                float tw = BkTw(word, LookSize, "Jost-SemiBold");
                var hit = E(body, null, x - 4f, cy - 16f, tw + 8f, 34f, true); hit.name = "book-look";
                var l = BkPut(body, x, cy, word, LookSize, "Jost-SemiBold", BkSoftHex); l.pickingMode = PickingMode.Ignore;
                var line = E(body, null, x, cy + 12f, tw, 2f); line.style.backgroundColor = BkDeep;
                made.Add((l, line, what, kind));
                hit.RegisterCallback<ClickEvent>(ev => { ev.StopPropagation(); if (what == "light") host.ChooseLight(kind == "night"); else host.ChooseWeather(kind); Paint(); });
                x += tw;
            }
            void Paint()
            {
                var g = host.Game; if (g == null) return;
                foreach (var (l, line, what, kind) in made)
                {
                    bool on = what == "light" ? g.Light == kind : g.Weather == kind;
                    l.style.color = on ? BkDeep : BkSoft; line.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
            Paint();
        }
    }
}
