using UnityEngine;

namespace Suborned
{
    /// <summary>Brief 072-E (Redline's plan 5, `PerfVariant.everyFace`): the every-face A/B in one scene. On (as built): the every-face modules
    /// (KitColumns: baked columns and single modules, with their L2 shadow proxies), the procedural wall left over them (batches/*-every) and the
    /// shadows-only shell behind them (batches/buildings-shell). Off: those hidden and the procedural walls and skin as they were before the modules
    /// (batches/*-today, built off) shown: the scene as it was, on the same build. `today` exists only in development builds (a release build
    /// strips it, Strip072). Built by the world builder on the HarbourDistrict root.</summary>
    public sealed class EveryFace : MonoBehaviour
    {
        public GameObject[] modules = System.Array.Empty<GameObject>();
        public GameObject[] every = System.Array.Empty<GameObject>();
        public GameObject[] today = System.Array.Empty<GameObject>();
        public bool on = true;

        public bool HasToday { get { foreach (var t in today) if (t) return true; return false; } }

        public void Set(bool value)
        {
            if (value == on) return;
            on = value;
            foreach (var m in modules) if (m) m.SetActive(value);
            foreach (var e in every) if (e) e.SetActive(value);
            foreach (var t in today) if (t) t.SetActive(!value);
        }
    }
}
