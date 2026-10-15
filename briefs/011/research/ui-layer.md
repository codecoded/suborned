# UI layer for the play HUD: UI Toolkit or uGUI (Latch, 2026-10-15)

Question: the play HUD is IMGUI (`PlayHud.cs`); Sky wants Glyph's real kit (9-slices, portrait frames, button states, icon atlas, font, layout at 1080p/1440p/4K). Which runtime UI layer takes it?

## What the Unity 6.2 manual says ("Comparison of UI systems in Unity")
Source: https://docs.unity3d.com/6000.2/Documentation/Manual/UI-system-compare.html

- Runtime recommendation in general: uGUI, UI Toolkit the alternative.
- By use case: "Multi-resolution menus and HUD in intensive UI projects": UI Toolkit. "UI that requires customized shaders and materials": uGUI.
- Feature rows:

| Feature | UI Toolkit | uGUI |
|---|---|---|
| World-space (3D) rendering | Yes | Yes |
| Custom materials and shaders | No | Yes |
| Integration with Animation Clips and Timeline | No | Yes |
| UI transition animations | Yes | No |
| Dynamic texture atlas | Yes | No |
| Textureless elements | Yes | No |
| UI anti-aliasing | Yes | No |
| Rich text, scalable text, font fallbacks | Yes | Yes (uGUI needs TextMesh Pro) |

- An earlier search summary of the same page: UI Toolkit "is easier to optimize for rendering performance", drawing element hierarchies with one shader, one material and a texture atlas.

## Used for
The choice in Latch's report to Aster (2026-10-15): UI Toolkit for the screen HUD, positioned overlay elements for the world-anchored marks, and in-world geometry left in HostViews.
