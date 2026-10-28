#if UNITY_EDITOR
using System.Linq;
using UnityEngine;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>The swap's two switches, kept in the scene as one named object ("Swap switches heights=rule spacehq=old"): which heights it was
    /// built at, which SpaceHQ is on. Both SpaceHQs stand; the other one is inactive. (A plain object: the editor assembly adds no components.)</summary>
    internal sealed class SwapSwitches
    {
        public const string OldHq = "DK_SpaceHQ", OursHq = "Ours SpaceHQ_Portal (signature/space-hq)";
        public string heights, spaceHq; public GameObject oldSpaceHq, oursSpaceHq, marker;

        static GameObject Find(string name) => Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == name)?.gameObject;

        public static SwapSwitches Make(string heights, string hq)
        {
            var s = new SwapSwitches { heights = heights, oldSpaceHq = Find(OldHq), oursSpaceHq = Find(OursHq), marker = new GameObject("Swap switches") };
            s.SetSpaceHq(hq);
            return s;
        }

        public static SwapSwitches Read()
        {
            var m = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name.StartsWith("Swap switches"))?.gameObject;
            if (m == null) return null;
            string Get(string k) { var i = m.name.IndexOf(k + "=", System.StringComparison.Ordinal); if (i < 0) return null; var v = m.name[(i + k.Length + 1)..]; int sp = v.IndexOf(' '); return sp < 0 ? v : v[..sp]; }
            return new SwapSwitches { heights = Get("heights"), spaceHq = Get("spacehq"), oldSpaceHq = Find(OldHq), oursSpaceHq = Find(OursHq), marker = m };
        }

        public void SetSpaceHq(string which)
        {
            spaceHq = which;
            if (oldSpaceHq) oldSpaceHq.SetActive(which == "old");
            if (oursSpaceHq) oursSpaceHq.SetActive(which == "ours");
            if (marker) marker.name = $"Swap switches heights={heights} spacehq={which}";
        }
    }
}
#endif
