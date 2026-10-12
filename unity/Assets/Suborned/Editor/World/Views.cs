using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>One named shot view from views.json (see its note). Missing optional numbers read as NaN.</summary>
    public sealed class ViewSpec
    {
        public string Name, Projection, Along, Clear, Street, Focus;
        public int Width, Height;
        public float Yaw, Pitch, Fov, Margin, Near, Far, EyeHeight, At, Side, ShadowDistance, Zoom, Radius;
        public bool Fog, Main;
        /// <summary>Plan point [x, y, height] as Unity (x, height, y). perspective: Eye stands there looking at Look; export: Look is the focus.</summary>
        public Vector3? Eye, Look;
        public List<string> Frame = new();
        public float Aspect => (float)Width / Height;
    }

    public static class Views
    {
        public static List<ViewSpec> Load(string path)
        {
            var list = new List<ViewSpec>();
            foreach (var v in J.A(Json.Parse(File.ReadAllText(path)), "views"))
            {
                var size = J.A(v, "size");
                var s = new ViewSpec
                {
                    Name = J.S(v, "name") ?? throw new InvalidDataException("World: view without name"),
                    Projection = J.S(v, "projection"), Along = J.S(v, "along"), Street = J.S(v, "street"), Focus = J.S(v, "focus"), Clear = J.S(v, "clear") ?? "sky",
                    Width = (int)(double)size[0], Height = (int)(double)size[1],
                    Yaw = Opt(v, "yaw"), Pitch = Opt(v, "pitch"), Fov = Opt(v, "fov"), Margin = Opt(v, "margin", 1f),
                    Near = Opt(v, "near", 0.3f), Far = Opt(v, "far"), EyeHeight = Opt(v, "eyeHeight"), At = Opt(v, "at", 0f), Side = Opt(v, "side", 0f),
                    ShadowDistance = Opt(v, "shadowDistance"), Zoom = Opt(v, "zoom"), Radius = Opt(v, "radius", 40f), Fog = J.B(v, "fog"), Main = J.B(v, "main"),
                };
                s.Eye = Point(v, "eye"); s.Look = Point(v, "look");
                if (s.Eye != null && s.Look == null) throw new InvalidDataException($"World: view {s.Name} has an eye and no look");
                if (J.Get(v, "frame") is List<object> frame) foreach (var f in frame) s.Frame.Add((string)f);
                if (s.Projection is not ("top" or "export" or "perspective"))
                    throw new InvalidDataException($"World: view {s.Name} has unknown projection `{s.Projection}`");
                list.Add(s);
            }
            return list;
        }

        static Vector3? Point(object o, string key) =>
            J.Get(o, key) is List<object> p ? new Vector3((float)(double)p[0], (float)(double)p[2], (float)(double)p[1]) : null;

        static float Opt(object o, string key, float fallback = float.NaN) => J.Get(o, key) is double d ? (float)d : fallback;
    }
}
