using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 107 (Glint's fix, briefs/107/glint/weather.html; Sky: look only): the ONE driver of the play light and the weather's look. It draws
    /// Game.Light and Game.Weather (engine 0.54.0: rolled from the seed, changed at the briefing), or an override (a shot's night, `-night`; a `-tplong` row's weather). Brief 107 follow-up: the `-weather` dev flag is gone: the briefing gives the engine's `weather` order.
    /// - The play camera's light: in the old town the message "PlayLight107" to its OldTownPlayLook (assembly Suborned.OldTown, which this one doesn't
    ///   reference): [night, sun x, ambient x, desaturate, shadow x, night exposure (stops)]; it writes its own sun, ambient, probe, exposure, lamps and grade from it
    ///   (OldTownPlayLook.Weather107.cs). Sent only when something changes.
    /// - The LightPhase (the swap's carry in the old town, SwapNight107; HarbourDistrict's own): Set(night), which RoadLight, SignalLight, the glows, the
    ///   night sky, reflection and grade read.
    /// - The weather (Weather107): the wet ground, the rain (Rain107), the mist (MistPass107); the lit rooms (look097.json) at night.
    /// No hidden fallback: CanNight says whether the scene has a night (a LightPhase); HostNight refuses night when it has not.</summary>
    public sealed class PlayView107 : MonoBehaviour
    {
        public static PlayView107 Instance { get; private set; }
        /// <summary>The night and weather drawn now.</summary>
        public static bool Night => Instance && Instance.night;
        public static string Weather => Instance ? Instance.weather : "clear";
        static bool? nightOverride; static string weatherOverride;
        static readonly string[] LampNames = { "Streetlamp", "SphereLamp", "GlobeLamp", "BackDoorLamp", "CafeLamp" };

        HandoverHost host; Component look; LightPhase phase; bool bound;
        bool night, nightApplied; string weather = "clear", weatherApplied = "";
        Weather107.Data data;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance) return;
            var go = new GameObject("play view (brief 107)"); DontDestroyOnLoad(go); Instance = go.AddComponent<PlayView107>();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => { if (Instance) Instance.bound = false; };
        }

        /// <summary>A shot's or `-night`'s view: night or day whatever the game says, until Release.</summary>
        public static void ForceNight(bool on) { nightOverride = on; if (Instance) Instance.Tick(true); }
        public static void ForceWeather(string w) { weatherOverride = w; if (Instance) Instance.Tick(true); }
        public static void Release() { nightOverride = null; weatherOverride = null; if (Instance) Instance.Tick(true); }

        /// <summary>Whether this scene can draw a night: a LightPhase (the swap's carry, or HarbourDistrict's own).</summary>
        public static bool CanNight => (LightPhase.Instance ? LightPhase.Instance : FindAnyObjectByType<LightPhase>()) != null;

        void Bind()
        {
            host = FindAnyObjectByType<HandoverHost>();
            phase = LightPhase.Instance ? LightPhase.Instance : FindAnyObjectByType<LightPhase>();
            look = null; foreach (var c in FindObjectsByType<Camera>(FindObjectsInactive.Include)) if (c.GetComponent("OldTownPlayLook") is Component k && k) { look = k; break; }
            data = Weather107.Load(); nightApplied = false; weatherApplied = "\n"; bound = true;
            Debug.Log($"PlayView107: bound; play look {(look ? look.name : "none")}, LightPhase {(phase ? phase.name : "none")}; night {(CanNight ? "possible" : "NOT possible in this scene")}");
        }

        void Update() => Tick(false);

        void Tick(bool force)
        {
            if (!bound) Bind();
            if (!host) host = FindAnyObjectByType<HandoverHost>();
            var g = host ? host.Game : null;
            night = (nightOverride ?? (g != null && g.Night)) && CanNight;
            weather = weatherOverride ?? (g != null ? g.Weather : "clear");
            if (force || night != nightApplied || weather != weatherApplied) Apply();
            Weather107.Frame(host);
        }

        void Apply()
        {
            nightApplied = night; weatherApplied = weather;
            if (phase) { phase.Focus = () => host ? host.FocusPoint : (Vector3?)null; if (phase.Night != night) phase.Set(night); }
            Weather107.Apply(weather, night, data);
            Shader.SetGlobalVector("_DKRoomsNight", night ? new Vector4(Look097.RoomsNight.x, Look097.RoomsNight.y, 0, 0) : Vector4.zero); // the lit rooms behind the glass (look097.json)
            var w = data.Of(weather);
            if (look) look.SendMessage("PlayLight107", new[] { night ? 1f : 0f, w.sun, w.ambient, w.desaturate, w.shadow, data.nightExposure }, SendMessageOptions.DontRequireReceiver);
            Debug.Log($"PlayView107: {(night ? "night" : "day")}, weather {weather} (sun x{w.sun}, ambient x{w.ambient}, shadow x{w.shadow}, wet {w.wet}, rain {w.rain}, mist {w.mist})");
        }

        /// <summary>Every lamp head in the scene (the lamp pieces by name, as the night's lamps find them), for the rain's reflections.</summary>
        public static List<Vector3> LampHeads()
        {
            var o = new List<Vector3>(); var seen = new HashSet<Vector3Int>();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                string n = r.name; int k = Array.FindIndex(LampNames, x => n.Contains(x)); if (k < 0 || LampNames[k] == "CafeLamp") continue;
                var bb = r.bounds; var head = new Vector3(bb.center.x, bb.max.y - 0.35f, bb.center.z);
                if (seen.Add(Vector3Int.RoundToInt(head))) o.Add(head);
            }
            return o;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
