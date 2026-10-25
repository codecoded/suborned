using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 061 pick A (Sky 2026-10-25), the crossing signal head at run time. LOOK ONLY: the sim, the people and the trams read nothing
    /// from it, and nothing here reaches a replay. Each head reads the mission clock (HandoverHost sets MissionSeconds; never frame time) and its
    /// crossing's cycle (the export's crossings[].cycle {walk, clearing, stop, arrive, offset}, set by the builder), and writes its state with
    /// MaterialPropertyBlocks, so the shared kit materials are never touched:
    ///  · phase = (mission seconds + offset) mod (walk + clearing + stop): WALK, CLEARING, STOP, its last `arrive` seconds ARRIVING;
    ///  · emission (rim, figure, digits, ring, dot rows): Glint's linear(colour) x level for the state and the light phase (SignalLight, from
    ///    signals.json), written as a vector so nothing converts it again; never the .mat default (unity_mats.py writes it gamma-style); the
    ///    colour crossfades over 0.2 s, the glyphs cut; the rim blinks at 2 Hz (off level 0.15) in CLEARING's last 3 s and all of ARRIVING;
    ///  · the faces: `_BaseMap_ST` from Vela's tables (signal-lens.json): the figure by state, the numerals (CLEARING the seconds left, ARRIVING
    ///    3, 2, 1, the tens blank under ten, WALK and STOP blank);
    ///  · the clock ring in the paving (HaloRing): `_Cutoff` = 1 - lit / 24, per head (WALK and STOP 24, CLEARING drains, ARRIVING fills);
    ///  · the beam and the halo: the state's trimmed colour (CrossGlow `_BaseColor`) at their own material alpha;
    ///  · the dot rows (when the builder gives them): `_LitFrom` / `_LitTo` on u (metres along each module): all lit, but in CLEARING they go dark
    ///    from both kerbs toward the middle in step with the seconds left.
    /// The heads keep the export's `faces` yaw; nothing turns them. A head with no cycle (an export before brief 061's) shows the export's state.
    /// Shots hold a state: `-signalstate walk|clearing|stop|arriving` (with `-signalleft n` for the numerals), or Hold from a proof.</summary>
    public sealed class SignalHead : MonoBehaviour
    {
        [Header("The crossing's cycle (export crossings[].cycle; seconds)")]
        public string Crossing = "";
        public bool HasCycle; public int Walk = 6, Clearing = 10, Stop = 24, Arrive = 3, Offset;
        [Tooltip("The export's signal.state: shown when there is no cycle.")]
        public string BuildState = "walk";
        [Header("Parts (the builder wires them by part name)")]
        public Renderer Rim; public Renderer Figure, Tens, Units, Ring, Beam, Halo;
        [Tooltip("A state held on this head alone (a proof's three-states frame); \"\" = the global Hold or the cycle.")]
        public string HoldHere = "";
        [Tooltip("The crossing's dot-row modules this head drives (one head per crossing), each module's start along the crossing (m from line[0]) and its length; CrossLength = kerb to kerb.")]
        public Renderer[] Dots = new Renderer[0]; public float[] DotFrom = new float[0], DotLength = new float[0]; public float CrossLength;

        /// <summary>The mission clock in seconds (HandoverHost, every frame); the cycle never reads frame time.</summary>
        public static double MissionSeconds;
        /// <summary>A state held for shots ("" = the cycle): walk, clearing, stop, arriving; HoldLeft the seconds left it shows.</summary>
        public static string Hold = ""; public static float HoldLeft = 7f;
        /// <summary>Every live head (the proof reads them).</summary>
        public static readonly List<SignalHead> All = new();

        /// <summary>The ring's lowest cutoff: under the texture's faintest ring pixel (5/255), over its empty corners (0).</summary>
        public const float RingCutMin = 0.5f / 255f;
        public const float CrossfadeS = 0.2f, BlinkHz = 2f, BlinkOff = 0.15f, BlinkLastS = 3f; public const int RingSegments = 24;

        // Vela's tables (briefs/061/vela-signal-lens-data.json `faces`): _BaseMap_ST per glyph
        static readonly Dictionary<string, Vector4> FigureST = new()
        {
            ["walk_full"] = new(1, 1, 0f, 0f), ["stand_full"] = new(1, 1, 0.25f, 0f), ["walk_head"] = new(1, 1, 0.5f, 0f), ["stand_head"] = new(1, 1, 0.75f, 0f),
        };
        static readonly Vector4[] DigitST =
        {
            new(1, 1, -0.5f, 0.5f), new(1, 1, -0.25f, 0.5f), new(1, 1, 0f, 0.5f), new(1, 1, 0.25f, 0.5f),
            new(1, 1, -0.5f, 0.25f), new(1, 1, -0.25f, 0.25f), new(1, 1, 0f, 0.25f), new(1, 1, 0.25f, 0.25f),
            new(1, 1, -0.5f, 0f), new(1, 1, -0.25f, 0f),
        };
        static readonly Vector4 BlankST = new(1, 1, 0f, 0f);

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor"), StId = Shader.PropertyToID("_BaseMap_ST"), CutoffId = Shader.PropertyToID("_Cutoff"),
            BaseColorId = Shader.PropertyToID("_BaseColor"), LitFromId = Shader.PropertyToID("_LitFrom"), LitToId = Shader.PropertyToID("_LitTo");
        static MaterialPropertyBlock mpb;

        /// <summary>The state now, the seconds left in it, and the ring's lit segments (read by the proof).</summary>
        public string State { get; private set; } = "";
        public float Left { get; private set; }
        public int RingLit { get; private set; }
        public int Writes { get; private set; }

        Color shown, from, target; float fadeAt = -9f; bool first = true;
        // Redline 064 R4 (0.2.122): the head's parts and its dot rows keep keys of their own, so a clearing crossing's falling `half` (65-69 cm/s,
        // a new centimetre every frame) no longer rewrites all seven parts of every head; the dots' key holds `half` to 5 cm (a dot is lit or dark
        // by its centre, nothing finer shows), and heads without dots never compute it. Writes wait while nothing of the head (or of its rows) is
        // visible; the last-written key stays, so a head that comes into view writes once.
        (string st, int numeral, int lit, bool off, int r, int g, int b) lastHead = ("", -2, -1, false, -1, -1, -1);
        (int half, int r, int g, int b) lastDots = (-2, -1, -1, -1);
        bool wroteHead, wroteDots;
        /// <summary>Redline 064 R2: every head's LateUpdate time this frame, summed (Stopwatch ticks; read by -late when PerfTiming).</summary>
        public static long FrameTicks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Forget() { All.Clear(); MissionSeconds = 0; Hold = ""; HoldLeft = 7f; mpb = null; argsRead = false; }

        static bool argsRead;
        static void ReadArgs()
        {
            if (argsRead) return; argsRead = true;
            if (GameBoot.ArgValue("-signalstate") is string s && s is "walk" or "clearing" or "stop" or "arriving") Hold = s;
            if (GameBoot.ArgValue("-signalleft") is string l && float.TryParse(l, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lv)) HoldLeft = Mathf.Max(0f, lv);
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); first = true; }
        void OnDisable() { All.Remove(this); }

        /// <summary>The state at mission second t for a cycle (pure: the proof and the tests read it).</summary>
        public static (string state, float left) StateAt(double t, int walk, int clearing, int stop, int arrive, int offset)
        {
            int cycle = walk + clearing + stop; if (cycle <= 0) return ("walk", 0f);
            double p = (t + offset) % cycle; if (p < 0) p += cycle;
            if (p < walk) return ("walk", (float)(walk - p));
            if (p < walk + clearing) return ("clearing", (float)(walk + clearing - p));
            float leftStop = (float)(cycle - p);
            return leftStop <= arrive ? ("arriving", leftStop) : ("stop", leftStop);
        }

        void LateUpdate()
        {
            if (!HandoverHost.PerfTiming) { Step(); return; }
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp(); Step(); FrameTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        }

        void Step()
        {
            ReadArgs();
            string st; float left;
            string hold = HoldHere.Length > 0 ? HoldHere : Hold;
            if (hold.Length > 0) { st = hold; left = hold == "arriving" ? Mathf.Min(HoldLeft, Arrive) : hold == "clearing" ? Mathf.Min(HoldLeft, Clearing) : 0f; }
            else if (HasCycle) (st, left) = StateAt(MissionSeconds, Walk, Clearing, Stop, Arrive, Offset);
            else { st = BuildState == "stop" ? "stop" : "walk"; left = 0f; }
            State = st; Left = left;

            // emission: Glint's linear colour x level (phase-aware), crossfaded 0.2 s on a state change
            if (!SignalLight.HeadEmission(st, out var hdr, out var srgb, out _)) { hdr = st == "walk" ? new Color(0.33f, 0.9f, 0.69f) : st == "clearing" ? new Color(1f, 0.71f, 0.14f) : new Color(0.95f, 0.26f, 0.18f); srgb = hdr; }
            float now = Time.unscaledTime;
            if (first) { shown = from = target = hdr; first = false; }
            else if (hdr != target) { from = shown; target = hdr; fadeAt = now; }
            float k = Mathf.Clamp01((now - fadeAt) / CrossfadeS); shown = Color.Lerp(from, target, k);
            bool blinking = st == "arriving" || st == "clearing" && left <= BlinkLastS;
            bool off = blinking && (MissionSeconds * BlinkHz) % 1.0 >= 0.5;
            float rimLevel = off ? BlinkOff : 1f;

            int numeral = st is "clearing" or "arriving" ? Mathf.Max(0, Mathf.CeilToInt(left - 1e-4f)) : -1;
            int lit = st switch
            {
                "clearing" => Clearing > 0 ? Mathf.Clamp(Mathf.CeilToInt(RingSegments * left / Clearing - 1e-4f), 0, RingSegments) : 0,
                "arriving" => Arrive > 0 ? Mathf.Clamp(Mathf.FloorToInt(RingSegments * (Arrive - left) / Arrive), 0, RingSegments) : RingSegments,
                _ => RingSegments,
            };
            RingLit = lit;
            int cr = Mathf.RoundToInt(shown.r * 1000f), cg = Mathf.RoundToInt(shown.g * 1000f), cb = Mathf.RoundToInt(shown.b * 1000f);
            mpb ??= new MaterialPropertyBlock();
            var headKey = (st, numeral, lit, off, cr, cg, cb);
            if ((headKey != lastHead || !wroteHead) && (!wroteHead || HeadSeen())) { lastHead = headKey; wroteHead = true; Writes++; WriteHead(st, numeral, lit, rimLevel, srgb); }
            if (Dots.Length == 0) return; // (a): a head that drives no dots never computes `half`
            float half = st == "clearing" && Clearing > 0 ? CrossLength * 0.5f * Mathf.Clamp01(left / Clearing) : float.PositiveInfinity;
            var dotKey = (float.IsInfinity(half) ? -1 : Mathf.RoundToInt(half * 20f), cr, cg, cb); // (b): to 5 cm
            if ((dotKey == lastDots && wroteDots) || wroteDots && !DotsSeen()) return; // (c): nothing new, or no row on screen
            lastDots = dotKey; wroteDots = true; Writes++;
            for (int i = 0; i < Dots.Length; i++)
            {
                var d = Dots[i]; if (!d) continue;
                float f0 = i < DotFrom.Length ? DotFrom[i] : 0f;
                float a = float.IsInfinity(half) ? -1e4f : CrossLength * 0.5f - half - f0, b = float.IsInfinity(half) ? 1e4f : CrossLength * 0.5f + half - f0;
                mpb.Clear(); mpb.SetFloat(LitFromId, a); mpb.SetFloat(LitToId, b); mpb.SetVector(EmissionId, E(1f)); d.SetPropertyBlock(mpb);
            }
        }

        static bool Seen(Renderer r) => r && r.isVisible;
        bool HeadSeen() => Seen(Rim) || Seen(Figure) || Seen(Tens) || Seen(Units) || Seen(Ring) || Seen(Beam) || Seen(Halo);
        bool DotsSeen() { foreach (var d in Dots) if (Seen(d)) return true; return false; }

        Vector4 E(float lv) => new(shown.r * lv, shown.g * lv, shown.b * lv, 1f); // no closure a frame
        void WriteHead(string st, int numeral, int lit, float rimLevel, Color srgb)
        {
            if (Rim) { mpb.Clear(); mpb.SetVector(EmissionId, E(rimLevel)); Rim.SetPropertyBlock(mpb); }
            string fig = st switch { "walk" => "walk_full", "clearing" => "walk_head", "arriving" => "stand_head", _ => "stand_full" };
            if (Figure) { mpb.Clear(); mpb.SetVector(StId, FigureST[fig]); mpb.SetVector(EmissionId, E(1f)); Figure.SetPropertyBlock(mpb); }
            if (Tens) { mpb.Clear(); mpb.SetVector(StId, numeral >= 10 ? DigitST[(numeral / 10) % 10] : BlankST); mpb.SetVector(EmissionId, E(1f)); Tens.SetPropertyBlock(mpb); }
            if (Units) { mpb.Clear(); mpb.SetVector(StId, numeral >= 0 ? DigitST[numeral % 10] : BlankST); mpb.SetVector(EmissionId, E(1f)); Units.SetPropertyBlock(mpb); }
            if (Ring) { mpb.Clear(); mpb.SetFloat(CutoffId, Mathf.Clamp(1f - lit / (float)RingSegments, RingCutMin, 1.001f)); /* Vela's 1 - lit/24, kept off 0: at 0 the clip keeps alpha 0 too and a full ring drew its whole square (0.2.121); over 1 none is lit */ mpb.SetVector(EmissionId, E(1f)); Ring.SetPropertyBlock(mpb); }
            if (Beam) { mpb.Clear(); mpb.SetColor(BaseColorId, new Color(srgb.r, srgb.g, srgb.b, 1f)); Beam.SetPropertyBlock(mpb); } // CrossGlow keeps its own _Alpha (Vela: beam 0.26, halo 0.40)
            if (Halo) { mpb.Clear(); mpb.SetColor(BaseColorId, new Color(srgb.r, srgb.g, srgb.b, 1f)); Halo.SetPropertyBlock(mpb); }
        }
    }
}
