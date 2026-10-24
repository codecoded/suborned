using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned
{
    /// <summary>Brief 055 road test (Sky: "go"; Vela's asphalt option beside today's enamel): one scene carries both road finishes, so the
    /// same views can be shot both ways from one build. The builder (HarbourWorldBuilder, given the export's asphalt twin) bakes the pieces
    /// only the enamel set lays (lozenges, seams) into the batch layer `road-finish-enamel` and the pieces only the asphalt set lays (dashed
    /// centre and lane lines, edge lines) into `road-finish-asphalt`, built off; the carriageway's material is the only other difference
    /// (DK_Ground_RoadEnamel or DK_Ground_RoadAsphalt, each with its own repeat). Set(asphalt) swaps the material on every renderer that
    /// draws the other and turns the two batch layers. Enamel is the default and what the scene is saved with. `-road asphalt` on the
    /// command line opens on the asphalt; Shift+F11 toggles (a test switch, not a player option). View only: nothing the engine reads.</summary>
    public sealed class RoadFinish : MonoBehaviour
    {
        public Material enamel, asphalt;
        public GameObject enamelOnly, asphaltOnly;

        public static RoadFinish Instance { get; private set; }
        /// <summary>The finish drawn now ("enamel" or "asphalt"); "" when the scene was built without the asphalt twin.</summary>
        public static string Current => Instance == null || !Instance.Ready ? "" : Instance.isAsphalt ? "asphalt" : "enamel";
        public bool Ready => enamel && asphalt && asphaltOnly;

        bool isAsphalt; readonly List<(Renderer r, int slot)> swapped = new();

        void Awake()
        {
            Instance = this;
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-road");
            if (i >= 0 && i + 1 < a.Length && a[i + 1] == "asphalt") Set(true);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f11Key.wasPressedThisFrame && kb.shiftKey.isPressed) Set(!isAsphalt);
        }

        /// <summary>Draw the asphalt finish (true) or the enamel (false). False when the scene has no asphalt twin.</summary>
        public bool Set(bool toAsphalt)
        {
            if (!Ready) { Debug.LogWarning("RoadFinish: the scene was built without the asphalt twin (out/world/harbour-district.road-asphalt.world.json)"); return false; }
            if (toAsphalt == isAsphalt) return true;
            if (toAsphalt)
            {
                swapped.Clear();
                foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var ms = r.sharedMaterials; bool any = false;
                    for (int k = 0; k < ms.Length; k++) if (ms[k] == enamel) { ms[k] = asphalt; swapped.Add((r, k)); any = true; }
                    if (any) r.sharedMaterials = ms;
                }
            }
            else
            {
                foreach (var (r, k) in swapped) { if (!r) continue; var ms = r.sharedMaterials; if (k < ms.Length) { ms[k] = enamel; r.sharedMaterials = ms; } }
                swapped.Clear();
            }
            if (enamelOnly) enamelOnly.SetActive(!toAsphalt);
            asphaltOnly.SetActive(toAsphalt);
            isAsphalt = toAsphalt;
            Debug.Log($"RoadFinish: {(toAsphalt ? "asphalt" : "enamel")} ({swapped.Count} material slots on asphalt)");
            return true;
        }
    }
}
