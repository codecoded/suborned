using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 080 (engine 0.37.0; Sky: "let the game play organically"): each new game rolls a seed; seed + orders replay exactly. A new game (the front
    /// end's New game, a host's first start) rolls; Replay and Restart mission keep the street they just played. A proof, a batch row or a probe (GameBoot.Automation
    /// in a player, batchmode in the Editor) pins seed 0, the neutral start its fixed ticks were written for, unless it passes `-seed n` or sets SeedPin. Host only:
    /// the engine reads nothing but Start.Seed, and a saved run carries its own.</summary>
    public sealed partial class HandoverHost
    {
        static bool seedPinRead; static int? seedPin;
        /// <summary>The seed every Begin plays while set (null: Begin rolls). Read once from the command line.</summary>
        public static int? SeedPin
        {
            get
            {
                if (seedPinRead) return seedPin;
                seedPinRead = true;
                if (GameBoot.ArgValue("-seed") is string v && int.TryParse(v, out int n) && n >= 0) seedPin = n;
                else if (Application.isBatchMode || GameBoot.Automation && !Application.isEditor) seedPin = 0;
                return seedPin;
            }
            set { seedPinRead = true; seedPin = value; }
        }

        int seedAgain = -1;
        /// <summary>The next Begin plays this seed once (Replay, Restart mission), over a pin or a roll; brief 098: and the dial's counts it was played with (HostDial).</summary>
        public void BeginSameSeed() { seedAgain = Game != null ? Game.Seed : -1; DialKeep(); Begin(); }

        /// <summary>1..2^31-1, never 0 (0 is the neutral start, for tests). The roll is the host's: nothing in the run reads it but Start.Seed.</summary>
        public static int RollSeed() { int s = System.Guid.NewGuid().GetHashCode() & int.MaxValue; return s == 0 ? 1 : s; }

        int SeedForBegin()
        {
            if (seedAgain >= 0) { int s = seedAgain; seedAgain = -1; return s; }
            return SeedPin ?? RollSeed();
        }
    }
}
