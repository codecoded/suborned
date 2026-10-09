using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>Graybox look: one flat colour per material key, and the sizes the export leaves to the builder (never anything
    /// that decides what the player can see: plot block heights come from the plan).
    /// Muted on purpose: no environment surface uses a signal colour (red, amber, violet) at full strength (GDD §10).</summary>
    public static class Style
    {
        public const float BlockInset = 3;          // pavement margin left round each block (m); the plan's height applies to the inset block

        // Builder-chosen sizes for things the export gives only as a point or line.
        public const float TrackWidth = 1.6f;       // one tram track strip
        public const float AreaOutline = 1.0f;      // district area boundary ribbon
        public const float BeamWidth = 1.4f, BeamDepth = 1.6f;
        public const float PylonSize = 1.6f, CrossheadDepth = 1.0f;
        public const float StopPlatformTop = 0.3f;  // above the carriageway
        public const float StationDeck = 1.0f;      // platform slab depth
        public const float StationDeckRecess = 0.3f; // deck top below station height (= beam top), so they never share a plane
        public const float GatePost = 0.5f, GateHeight = 2.4f;  // gate: a post at each kerb (open; no seal state exists yet)
        public const float WaterLevel = -1.2f;      // water surface; quay walls run from here to the pavement
        public const float CanopyRoof = 0.25f, CanopyPost = 0.25f;
        public const float StripeWidth = 0.5f, StripeMargin = 0.25f;   // zebra bars and their gaps; margin at each kerb
        public const float PegSize = 0.5f, PegHeight = 1.8f;           // mission point: a person-sized post
        public const float DoorPad = 1.2f, DoorPadLift = 0.03f;        // flat pad under a door or entrance point
        public const float CrewPegSize = 0.35f, CrewPegHeight = 1.2f;  // crew start: smaller than a mission peg
        public const float TowerPost = 0.3f;                           // entrance tower anchor: a thin post to the crown
        public const float CutWall = 0.3f, CutFloorLift = 0.02f;       // cut-away walls and floor (see CutAway)

        /// <summary>Landmark drum height from its radius (the export gives centre + radius only).</summary>
        public static float LandmarkHeight(float radius) => Mathf.Clamp(radius * 1.5f, 8f, 60f);

        static readonly Dictionary<string, Color32> Colours = new()
        {
            ["ground"] = new Color32(0x8a, 0x8f, 0x88, 255),
            ["beyond"] = new Color32(0xb8, 0xb2, 0xa2, 255),
            ["carriageway"] = new Color32(0x4a, 0x4d, 0x52, 255),
            ["kerb"] = new Color32(0x9a, 0x9a, 0x96, 255),
            ["reservation"] = new Color32(0x7d, 0x8a, 0x76, 255),
            ["median"] = new Color32(0x86, 0x9e, 0x74, 255),
            ["foot"] = new Color32(0xe6, 0xd9, 0xbc, 255),
            ["pavement"] = new Color32(0xd3, 0xcb, 0xb8, 255),
            ["use/residential"] = new Color32(0xe8, 0xdc, 0xc4, 255),
            ["use/mixed"] = new Color32(0xd9, 0xc6, 0xa2, 255),
            ["use/commercial"] = new Color32(0xbf, 0xd2, 0xd4, 255),
            ["use/civic"] = new Color32(0xf2, 0xee, 0xe4, 255),
            ["use/port"] = new Color32(0xa6, 0xb1, 0xb6, 255),
            ["use/industrial"] = new Color32(0xa3, 0x9a, 0x8a, 255),
            ["use/park"] = new Color32(0x7f, 0xae, 0x6a, 255),
            ["use/square"] = new Color32(0xe3, 0xd6, 0xb6, 255),
            ["use/open"] = new Color32(0xb4, 0xc6, 0x96, 255),
            ["use/other"] = new Color32(0xcc, 0xcc, 0xcc, 255),
            ["area"] = new Color32(0x2f, 0x7f, 0x86, 255),
            ["circus/island"] = new Color32(0xc9, 0xc2, 0xb0, 255),
            ["circus/garden"] = new Color32(0x6f, 0x9f, 0x5f, 255),
            ["water"] = new Color32(0x3f, 0x7f, 0x95, 255),
            ["quay"] = new Color32(0xbd, 0xb5, 0xa3, 255),
            ["track"] = new Color32(0x2a, 0x2c, 0x30, 255),
            ["beam"] = new Color32(0xc8, 0xcc, 0xd0, 255),
            ["pylon"] = new Color32(0x9c, 0xa2, 0xa8, 255),
            ["stop"] = new Color32(0x3f, 0x9a, 0x9a, 255),
            ["station"] = new Color32(0x2a, 0x84, 0x88, 255),
            ["gate"] = new Color32(0x4f, 0x5f, 0x86, 255),
            ["landmark"] = new Color32(0xef, 0xe7, 0xd2, 255),
            ["building/cafe"] = new Color32(0xe9, 0xb0, 0x8a, 255),
            ["building"] = new Color32(0xf4, 0xf0, 0xe6, 255),
            ["structure"] = new Color32(0x4f, 0x9c, 0x9c, 255),
            ["object/table"] = new Color32(0xb4, 0xa0, 0x7c, 255),
            ["object/planter"] = new Color32(0x5f, 0x8f, 0x52, 255),
            ["object/fountain"] = new Color32(0x86, 0xbc, 0xcc, 255),
            ["object"] = new Color32(0x99, 0x99, 0x99, 255),
            ["bay/officialBay"] = new Color32(0xcf, 0xc6, 0x9c, 255),
            ["bay/getawayBay"] = new Color32(0x9c, 0xbc, 0xa4, 255),
            ["crossing"] = new Color32(0x6b, 0x6e, 0x74, 255),
            ["crossing/stripe"] = new Color32(0xfa, 0xfa, 0xf6, 255),
            ["cut/floor"] = new Color32(0xb9, 0xb3, 0xa6, 255),
            // Debug markers, not environment: deliberately loud, and clear of the §10 signal hues (red, amber, violet).
            ["marker/point"] = new Color32(0xc8, 0xf0, 0x00, 255),
            ["marker/door"] = new Color32(0xff, 0x2e, 0xc8, 255),
            ["marker/crew"] = new Color32(0x1e, 0x9b, 0xff, 255),
            ["marker/tower"] = new Color32(0x20, 0x20, 0x24, 255),
        };

        static readonly Dictionary<string, Material> Cache = new();

        public static void Reset() => Cache.Clear();

        /// <summary>Material for a key (falls back to its family before the slash, then neutral grey). Cached per build;
        /// the materials live inside the scene, like the meshes.</summary>
        public static Material Mat(string key)
        {
            if (!Colours.ContainsKey(key))
            {
                int slash = key.IndexOf('/');
                key = slash > 0 && Colours.ContainsKey(key[..slash]) ? key[..slash] : "use/other";
            }
            if (Cache.TryGetValue(key, out var m)) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(shader) { name = key };
            m.SetColor("_BaseColor", Colours[key]);
            m.SetFloat("_Smoothness", 0f);
            m.SetFloat("_Metallic", 0f);
            // Matte: no highlights or reflections (a straight-down view otherwise washes the tops out to white).
            m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            m.SetFloat("_ScreenSpaceReflections", 0f);
            m.enableInstancing = true;
            Cache[key] = m;
            return m;
        }
    }
}
