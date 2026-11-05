using System.Globalization;
using UnityEngine;

namespace Suborned.Play
{
    public sealed partial class FrontEnd
    {
        /// <summary>Options, Camera (moved out of FrontEnd.Panel.cs, brief 096, which was over 500 lines). Debug (Sky 2026-11-05: "for debug, can I have
        /// control on the game plan camera so I can zoom in / out a lot more"): Free zoom, off by default and saved like the rest; with it on the map
        /// view's wheel and trackpad zoom from HandoverHost.FreeZoomMin to FreeZoomMax (ortho half-height, m) instead of play's 8 to 18.5. Pitch and yaw keep their rules.</summary>
        void CameraTab()
        {
            var c = S.camera;
            GroupHead("Commander view");
            Slider("Pan speed", 6, 36, 1, () => c.panSpeed, v => { c.panSpeed = v; Save(); }, v => $"{Mathf.RoundToInt(v)}");
            Switch("Edge pan", () => c.edgePan, v => { c.edgePan = v; Save(); });
            Slider("Orbit sensitivity", 0.25f, 2f, 0.05f, () => c.orbitSensitivity, v => { c.orbitSensitivity = v; Save(); }, v => v.ToString("0.00", CultureInfo.InvariantCulture));
            Switch("Invert orbit", () => c.invertOrbit, v => { c.invertOrbit = v; Save(); });
            Switch("Trackpad drag orbit", () => c.trackpadOrbit, v => { c.trackpadOrbit = v; Save(); });
            GroupHead("Third person");
            Choice("Camera distance", new[] { "Close", "Default", "Far" }, () => c.thirdPersonDistance switch { "close" => 0, "far" => 2, _ => 1 }, i => { c.thirdPersonDistance = i switch { 0 => "close", 2 => "far", _ => "default" }; Save(); });
            Switch("Camera settle", () => c.settle, v => { c.settle = v; Save(); });
            GroupHead("Debug");
            Switch("Free zoom (debug)", () => c.freeZoom, v => { c.freeZoom = v; Save(); });
        }
    }
}
