using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Graybox
{
    /// <summary>Transit: tram track strips (each segment just above the surface it crosses), tram stop platforms, sky rail
    /// beams, pylons and station platforms.</summary>
    public static class TransitLayers
    {
        const float TrackLevel = 0.03f;

        static string PathName(object t) => J.S(t, "line") + "-" + J.S(t, "path");

        public static void Tram(Transform root, object world, Surfaces ground)
        {
            foreach (var t in J.A(world, "tram"))
            {
                var g = Nodes.Group(root, PathName(t));
                int i = 0;
                foreach (var raw in J.Polys(t, "tracks"))
                {
                    var track = Poly.CleanLine(raw);
                    var level = new float[track.Length - 1];
                    for (int k = 0; k < level.Length; k++) level[k] = ground.Top(track[k], track[k + 1]) + TrackLevel;
                    Nodes.Part(g, "track-" + i++, new MeshKit().Strip(track, Style.TrackWidth, level), "track");
                }
            }
        }

        public static void Stops(Transform root, object world)
        {
            foreach (var s in J.A(world, "stops"))
            {
                var kit = new MeshKit();
                foreach (var p in J.Polys(s, "platforms")) kit.Prism(p, 0, Style.StopPlatformTop);
                Nodes.Part(root, J.S(s, "id"), kit, "stop");
            }
        }

        /// <summary>Beams with their top at the path height (24 m), one per track polyline.</summary>
        public static void SkyRail(Transform root, object world)
        {
            foreach (var t in J.A(world, "skyRail"))
            {
                var g = Nodes.Group(root, PathName(t));
                float top = J.F(t, "height");
                int i = 0;
                foreach (var beam in J.Polys(t, "tracks"))
                    Nodes.Part(g, "beam-" + i++, new MeshKit().Ribbon(beam, Style.BeamWidth, top - Style.BeamDepth, top), "beam");
            }
        }

        /// <summary>A column to the beam soffit and a crosshead across the path (wide enough for both beams when double).
        /// The crosshead turns with the nearest segment of the pylon's own path.</summary>
        public static void Pylons(Transform root, object world)
        {
            var paths = new Dictionary<string, object>();
            foreach (var t in J.A(world, "skyRail")) paths[PathName(t)] = t;
            foreach (var p in J.A(world, "pylons"))
            {
                var at = J.P(p, "at");
                float soffit = J.F(p, "height") - Style.BeamDepth;
                var path = paths[PathName(p)];
                var tangent = Poly.NearestTangent(J.Poly(path, "centreline"), at, out _);
                float across = (J.B(path, "double") ? J.F(path, "gauge") : 0) + Style.BeamWidth + 1f;
                var kit = new MeshKit()
                    .Prism(Poly.Rect(at, tangent, Style.PylonSize, Style.PylonSize), 0, soffit - Style.CrossheadDepth)
                    .Prism(Poly.Rect(at, tangent, Style.PylonSize, across), soffit - Style.CrossheadDepth, soffit);
                Nodes.Part(root, J.S(p, "id"), kit, "pylon");
            }
        }

        /// <summary>Platform decks recessed just under the station height, so the beam tops (at the same height) stand
        /// proud of the deck instead of sharing its plane.</summary>
        public static void Stations(Transform root, object world)
        {
            foreach (var s in J.A(world, "stations"))
            {
                float top = J.F(s, "height") - Style.StationDeckRecess;
                var kit = new MeshKit();
                foreach (var p in J.Polys(s, "platforms")) kit.Prism(p, top - Style.StationDeck, top);
                Nodes.Part(root, J.S(s, "id"), kit, "station");
            }
        }
    }
}
