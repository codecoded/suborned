using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Graybox
{
    /// <summary>Cameras and light.
    /// overview: orthographic, straight down, north up (screen right = east), framing the whole export, for comparison
    /// with the CAD sheet. Disabled; rendered on demand.
    /// play: "zoom 14 at 38°" read as the game's tactical camera precedent (old CrowdTestCamera/DecoKitIso): orthographic,
    /// orthographicSize 14 (28 m of frame height), standing 150 m back along its ray. Yaw and pitch are the plan's `view`
    /// (yaw 90 = looking east, pitch 38: Aster's decision 2026-10-10, brief 001), read from the export's top-level `view`.
    /// Centred on the Handover buildings (café and civic office bounds).
    /// Cut-away (study option, off by default): tall buildings sit on TallLayer and their cut twins on CutLayer
    /// (see CutAway); CutAway(cam, on) picks which set a camera draws.</summary>
    public static class Rig
    {
        public const float PlaySize = 14f, PlayBack = 150f;
        public const int TallLayer = 8, CutLayer = 9;
        public const float CutHeight = 3f;
        static readonly Color Clear = new Color32(0xcf, 0xd6, 0xd8, 255);

        /// <summary>Play view (yaw compass degrees, pitch above horizontal) from the export's `view`; throws if it is missing.</summary>
        public static (float yaw, float pitch) View(object world)
        {
            var v = J.Get(world, "view") ?? throw new System.InvalidOperationException("Graybox: export has no `view`");
            return (J.F(v, "yaw"), J.F(v, "pitch"));
        }

        public static void Build(Transform cameras, Transform lighting, Bounds district, Vector3 handover, (float yaw, float pitch) view)
        {
            var overview = Make(cameras, "overview");
            overview.orthographicSize = Mathf.Max(district.extents.x, district.extents.z) + 15f;
            overview.transform.SetPositionAndRotation(new Vector3(district.center.x, 100f, district.center.z), Quaternion.Euler(89.9f, 0f, 0f));
            overview.farClipPlane = 110f;
            overview.enabled = false;

            var play = Make(cameras, "play");
            play.orthographicSize = PlaySize;
            Aim(play, handover, view.yaw, view.pitch);
            play.farClipPlane = PlayBack + 300f;
            play.gameObject.tag = "MainCamera";

            var sun = new GameObject("sun").AddComponent<Light>();
            sun.transform.SetParent(lighting, false);
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.56f, 0.58f);
            RenderSettings.sun = sun;
        }

        /// <summary>Places a play camera at the given pitch and yaw so `focus` is at the centre of its frame.</summary>
        public static void Aim(Camera cam, Vector3 focus, float yaw, float pitch)
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * PlayBack, rot);
        }

        /// <summary>Draw tall buildings whole (off) or cut down to CutHeight with no roof (on).</summary>
        public static void CutAway(Camera cam, bool on) => cam.cullingMask = ~(1 << (on ? TallLayer : CutLayer));

        static Camera Make(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Clear;
            cam.nearClipPlane = 0.3f;
            CutAway(cam, false);
            return cam;
        }
    }
}
