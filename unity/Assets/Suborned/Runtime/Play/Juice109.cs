using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 109 (Sky 2026-11-11: "let's add juice"): punch from the view only, each tied to something real in the engine, so it tells the
    /// player something. Round 1 builds moments 1-3: an order (the ring pops, a dust puff where the agent sets off, the destination pulses, the
    /// money and Trail tick), a takedown landing (a short Cinemachine Impulse shake and a vignette pulse), the getaway (tyre smoke from a VFX Graph,
    /// exhaust, headlight flares, the camera following the car out after the Go).
    ///
    /// View only: nothing in the engine or the host's decisions reads any of it, and the game clock is never touched (no slow motion, no hit-stop).
    /// One launch switch: `-juice on|off`. Without it the juice is on for a player's own launch and off for an automation launch (a proof, shots, the
    /// late-fight check: any flag playing.ts calls automation), so the batch's proofs and pictures stay as they were until Sky adopts it.
    ///
    /// This facade holds no package type. Juice109Cam (Cinemachine) and Juice109Fx (particles, VFX Graph) are made only by a running player
    /// (RuntimeInitializeOnLoadMethod), and are reached only through the NoInlining methods below, so the host logic's plain dotnet (no
    /// Cinemachine assembly) never loads them.</summary>
    public static class Juice109
    {
        /// <summary>The switch, read once per launch.</summary>
        public static readonly bool On = Decide(Environment.GetCommandLineArgs());
        /// <summary>The live rigs (null until a player installs them, and always in the host logic).</summary>
        public static MonoBehaviour Cam { get; private set; }
        public static MonoBehaviour Fx { get; private set; }
        public static bool Live => Fx != null;

        /// <summary>`-juice on|off` wins; else on unless the launch has an automation flag (playing.ts: anything but the window and macOS ones).</summary>
        public static bool Decide(string[] args)
        {
            int i = Array.IndexOf(args, "-juice");
            if (i >= 0) return !(i + 1 < args.Length && args[i + 1].Equals("off", StringComparison.OrdinalIgnoreCase));
            for (int k = 1; k < args.Length; k++)
            {
                var a = args[k]; if (a.Length < 2 || a[0] != '-') continue;
                if (a == "-windowed" || a == "-popupwindow" || a.StartsWith("-screen-", StringComparison.Ordinal) || a.StartsWith("-psn_", StringComparison.Ordinal) || a.StartsWith("-NS", StringComparison.Ordinal) || a.StartsWith("-Apple", StringComparison.Ordinal)) continue;
                return false;
            }
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Debug.Log($"Juice109: {(On ? "on" : "off")} (-juice on|off; default on for a player's launch, off for automation)");
            if (!On || Fx) return;
            Install();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Install()
        {
            var go = new GameObject("Juice109 (brief 109: view-only juice)"); UnityEngine.Object.DontDestroyOnLoad(go);
            Fx = go.AddComponent<Juice109Fx>(); Cam = go.AddComponent<Juice109Cam>();
        }

        // ---------------------------------------------------------------- what the host tells it (each a no-op unless installed)

        /// <summary>An order the engine took: who gives it, where they stand, where it goes (null: in place), and the order's verb.</summary>
        public static void Order(Vector3? from, Vector3? to, string verb) { if (Live) OrderImpl(from, to, verb); }
        [MethodImpl(MethodImplOptions.NoInlining)] static void OrderImpl(Vector3? from, Vector3? to, string verb) => ((Juice109Fx)Fx).Order(from, to, verb);

        /// <summary>A takedown landed (out or dead) at a world point: the shake and the vignette pulse.</summary>
        public static void Takedown(Vector3 at, int tick) { if (Live) TakedownImpl(at, tick); }
        [MethodImpl(MethodImplOptions.NoInlining)] static void TakedownImpl(Vector3 at, int tick) { if (Cam) ((Juice109Cam)Cam).Takedown(at, tick); }
    }
}
