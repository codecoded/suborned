// Pulse scratch probe only (out/scratch/pulse): reads for the rival-stay measure, and env switches for the proposal. Never in the repo's sim/.
using System;
namespace Suborned.Sim
{
    public static class PulseSw
    {
        static int I(string n, int d) { var v = Environment.GetEnvironmentVariable(n); return v == null ? d : int.Parse(v, System.Globalization.CultureInfo.InvariantCulture); }
        public static readonly int BreakSpeed = I("PULSE_BREAK_SPEED", 0);   // cm/s a broken rival moves; 0 = rivals.speed
        public static readonly int Retreat = I("PULSE_RETREAT", -1);         // security.rivals.retreat override, s
        public static readonly bool WholeFinal = I("PULSE_WHOLE_FINAL", 0) == 1; // the final fight's break counts the crew of five
        public static readonly bool WholePre = I("PULSE_WHOLE_PRE", 0) == 1;     // the street's break counts the crew of five
        public static readonly int LastS = I("PULSE_LAST_S", 0); public static readonly bool NoPress = I("PULSE_NO_PRESS", 0) == 1; public static readonly bool PastFree = I("PULSE_PAST_FREE", 0) == 1; public static readonly bool Alone = I("PULSE_ALONE", 0) == 1; public static readonly bool PreOff = I("PULSE_PRE_OFF", 0) == 1; public static readonly string Nerve = Environment.GetEnvironmentVariable("PULSE_NERVE") ?? "half";
    }
    public sealed partial class Game
    {
        public bool PSees(Pt a, Pt b) => Sees(a, b);
    }
}
