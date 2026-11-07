// The start state (brief 011 B onward): what a scenario, a route script or a saved run fixes before tick 0, beyond the site, and how it
// is written into the start hash. Moved out of Game.cs (brief 098: files stay small); brief 098 step 0 adds the dial's counts (StartCounts.cs).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>The start state's choices beyond the site: what a scenario fixes before tick 0 (hashed into the start state).</summary>
    public sealed class Start
    {
        public int? RivalsAtS;                                            // §07 3:10 unless a scenario moves it
        /// <summary>Brief 080 (engine 0.37.0, Sky 2026-10-30): the run's seed, 0..2^31-1. The host rolls one at each new game (never 0) and saves it with
        /// the run: seed + orders replay exactly. It sets the start (rulebook seed) and every shot's draw (weapons.sheet). 0, and a script with none:
        /// the neutral start, every start draw at the middle of its band, the mission's own timetable; shots still roll, on seed 0's streams.
        /// Hashed into the start when set.</summary>
        public int Seed;
        /// <summary>Test only (brief 080, engine 0.37.0): true plays the shots before brief 080 (the counted accumulator, today's multipliers), as
        /// Pre039 plays the fight before brief 039, for scenarios whose subject is another rule and that need a shot to land on a known tick.
        /// Never in a route or a session; hashed when set.</summary>
        public bool Pre080;
        /// <summary>Test only (brief 080 pacing, engine 0.37.0): the clock before the shift came forward a minute: Vale out 5:50, close 8:30, the city's routines a minute later (the rivals never
        /// moved). For the scenarios of other rules, written to the old clock's ticks, until Waymark retimes them. Never in a route or a session; hashed when set.</summary>
        public bool OldClock;
        public readonly List<string> Turned = new List<string>();         // roles turned before the mission (a scenario fixture)
        public readonly Dictionary<string, List<string>> Known = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        public readonly Dictionary<string, string> Temperament = new Dictionary<string, string>(StringComparer.Ordinal);
        public long? Cash;
        public bool FinalFight = true;  // §07: the final fight is always fought; the top-down test's 28 had none
        public bool Crowd = true;       // §05 witnesses among the city's people; Suture's T4 arm switches it off
        public int? Arms;               // the mission's Arms (site arms) unless a scenario fixes it: one sidearm per weapons.sidearmArms
        public readonly List<string> Untie = new List<string>(); // roles whose lover tie (and affair) a test cuts: "with no tie, the call doesn't pull him"
        public int? Security;           // brief 013: the security level 0..4 a scenario fixes (else the district's base + the mission's bonus)
        public int? Standing;           // brief 014: the district's Standing at the start, as a campaign hands it on (else rulebook standing.start)
        /// <summary>Brief 031 §2.1: the cell's Heat (campaign Trail + Menace) at the start, fixed for the mission; it picks the attention band.
        /// Null: the mission's start.heat (site heat), else 0. Once a campaign layer exists it writes this from the debrief.</summary>
        public int? Heat;
        /// <summary>Tuning.md §11.5 (engine 0.17.0): the follow setting at the start: true, followers lock the driven agent's target at a press;
        /// false, they fire with its trigger. Null: off. Logged as a follow order when it changes in play. Hashed when set.</summary>
        public bool? FollowLock;
        /// <summary>Tuning.md §14 (engine 0.19.0): weapons free at the start. Null: the rulebook's weapons.free.start (on). Logged as a free order
        /// when it changes in play. Hashed into the start state when set.</summary>
        public bool? WeaponsFree;
        /// <summary>Brief 039 (engine 0.35.0): whether the cell's agents are pinned by hostile fire for this run, over the rulebook's
        /// weapons.pin.cell (Motive's open question; the range and Sky's test can try both). Null: the rulebook's. Hashed into the start when set.</summary>
        public bool? PinCell;
        /// <summary>Test only (brief 039, engine 0.35.0): false plays the controls before brief 039 (cease, stance, panic and free taken; weapons
        /// free from the start or the rulebook), as a rulebook pinned before 0.35.0 does, for the scenarios whose scripted player needs them
        /// (one shot then cease, weapons free off). Never in a route or a session; null, the rulebook's weapons.oneInput; hashed when set.</summary>
        public bool? OneInput;
        /// <summary>Test only (brief 039, engine 0.35.0): true plays the fight before brief 039 (weapons.oneInput, focus, takeCover, pin and moves
        /// all off; the values stay the rulebook's), as a rulebook pinned before 0.35.0 plays it, for the scenarios of that fight's mechanics (the
        /// fire order's lapse and out-of-range end, the stances, cease, panic, weapons free off). Never in a route or a session; hashed when set.</summary>
        public bool Pre039;
        /// <summary>Test only (brief 039, engine 0.35.0): heat-main grilles for the Tech's Steam, by id and point, until the site carries which grates
        /// are heat mains (Motive, Vantage). Never in a route or a session; empty, every start hashes as before.</summary>
        public readonly List<(string id, Pt at)> Grilles = new List<(string, Pt)>();
        /// <summary>Brief 022: the server's round. Null: the mission's data decides (on when the site carries a rounds file). false: off, the
        /// migration gate only (rounds off plays exactly as before the round existed); no recorded route sets it.</summary>
        public bool? Rounds;
        /// <summary>Test only (Keel 2026-10-19, N85; Aster's call: option 1 of briefs/023/keel-n85-question.md): a held scene placed at an entrance
        /// from `from` to `until` (mission seconds), read as a unit holding that door (Game.DoorHeld, HeldUntil). For a rule play can't reach
        /// on a block, as `untie` cuts a tie for a test. Never in a route or a session; empty, every start hashes as before.</summary>
        public readonly List<(string door, int from, int until)> Holds = new List<(string, int, int)>();
        /// <summary>Test only (brief 059, Keel): Vale's driver neither drives him away (Sky 3a) nor round: Vale waits at his car at the official
        /// bay to the close, as before engine 0.32.0. For the scenarios of other systems (takedowns, the crowd, the fail camera) that need the
        /// pickup after 6:13, as `holds` places a held scene. Never in a route or a session; off, every start hashes as before.</summary>
        public bool ValeWaits, OneCar; // OneCar: engine 0.42.0, test only: car.lend.split read as 0 (the one-car getaway's twin); hashed when set
        /// <summary>Test only (engine 0.33.0, Keel; Motive's S1-FLOOR-NO-SPAWN, S1-FLOOR-ZERO, S1-CREW-WIPED): members of the rival crew killed before
        /// the mission and never found (no body), so the floor and crew-destroyed rules can be read with three or five dead, as `holds` places a
        /// held scene. Never in a route or a session; empty, every start hashes as before.</summary>
        public readonly List<string> CrewGone = new List<string>();
        /// <summary>Brief 025 playground (EXPERIMENTAL): the driven agent's switches for this run, over the rulebook's cell.drive: pace (per mille
        /// of every pace, 250..4000), onBusy (cancel, hold), onRelease (stand, resume), gap (cm, 0 off). Null: the rulebook's. Hashed when set.</summary>
        public int? DrivePace, DriveGap; public string? DriveOnBusy, DriveOnRelease;
        public string? RivalsPublic;    // fight.public: how the public sees the rivals, as a scenario fixes it (else site rivals.public, else fight.rivalsPublic)
        /// <summary>Brief 027 playground (EXPERIMENTAL, rulebook fight.lab): the fight lab for this run. Off (the default, and today's preset): every
        /// rule is today's v0.24 and nothing below is read or hashed. On: the rulebook's fight.lab with these switches over its defaults (null: the
        /// rulebook's): healthScale, pips, stances (two, three), then (free, stop), close, charge, field, policeWarning, hostilesFireMoving, coverSnap (B5), kite (F2), and the
        /// loadout (per operative: sidearm, carbine, machine, riot, rifle or none). Hashed into the start state only when on.</summary>
        public bool FightLabOn; public int? LabHealthScale; public bool? LabPips, LabClose, LabCharge, LabField, LabWarn, LabFireMoving, LabCoverSnap, LabKite; public string? LabStances, LabThen;
        public readonly string?[] LabLoadout = new string?[4];
        /// <summary>For a behaviour-only comparison across brief 013 (suborned-sim hashes --legacy-start): leave the security level out of the
        /// canonical start, as the start states before it had none. Never set in play.</summary>
        public static bool LegacyCanonical;
        /// <summary>Brief 098 step 0 (Sky 2026-11-07: "add 3 more guards, take some away... the game then just does that"): how many of each kind
        /// this game has, by kind (Population.Kinds: guards, people), the whole zone's count of that kind, the cast included. A kind not set plays
        /// the people file's own (today's world exactly); none set, the start hashes as before. Set, the engine builds that world at the start from
        /// the seed and the dial's rules (contracts/rules/population.json; GamePopulation.cs), and the counts and those rules are hashed into the
        /// start: seed + counts + orders replay exactly. Below a kind's floor (its tenets: the cast) the floor plays; Game.Census says so.</summary>
        public readonly SortedDictionary<string, int> Counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        static void ParseCounts(Start s, JNode n)
        {
            foreach (var kv in n.Entries)
            {
                if (Array.IndexOf(Population.Kinds, kv.Key) < 0) throw new FormatException($"{kv.Value.Path}: '{kv.Key}' is not a kind the dial builds ({string.Join(", ", Population.Kinds)}; unknown = reject)");
                long v = kv.Value.Long(); if (v < 0 || v > Population.CountCap) throw new FormatException($"{kv.Value.Path}: a count is 0..{Population.CountCap}");
                s.Counts[kv.Key] = (int)v;
            }
        }
        public static Start Parse(JNode? n)
        {
            var s = new Start(); if (n == null) return s;
            n.Only("seed", "pre080", "oldClock", "rivalsAt", "turned", "known", "temperament", "cash", "finalFight", "crowd", "arms", "untie", "security", "standing", "rivalsPublic", "rounds", "holds", "drive", "fightLab", "heat", "followLock", "weaponsFree", "pinCell", "grilles", "oneInput", "pre039", "valeWaits", "crewGone", "oneCar", "counts");
            if (n.Opt("seed") is JNode sd) { long sv = sd.Long(); if (sv < 0 || sv > int.MaxValue) throw new FormatException($"{sd.Path}: seed 0..2147483647"); s.Seed = (int)sv; } // brief 080
            if (n.Opt("pre080") is JNode p80) s.Pre080 = p80.Bool(); // brief 080, test only
            if (n.Opt("oldClock") is JNode oc) s.OldClock = oc.Bool(); // brief 080, test only
            if (n.Opt("valeWaits") is JNode vw) s.ValeWaits = vw.Bool(); if (n.Opt("oneCar") is JNode o1) s.OneCar = o1.Bool(); // brief 059, engine 0.42.0: test only
            if (n.Opt("crewGone") is JNode cg) foreach (var x in cg.Items) { var id = x.Str(); if (!id.StartsWith("rival-", StringComparison.Ordinal) || s.CrewGone.Contains(id)) throw new FormatException($"{x.Path}: crewGone names rival-1..rival-5, once each"); s.CrewGone.Add(id); } // engine 0.33.0: test only
            if (n.Opt("fightLab") is JNode fl) // brief 027 (EXPERIMENTAL): the fight lab's switches; the rulebook checks the weapon ids when the game starts
            {
                fl.Only("on", "healthScale", "pips", "stances", "then", "close", "charge", "field", "policeWarning", "hostilesFireMoving", "coverSnap", "kite", "loadout");
                s.FightLabOn = fl["on"].Bool();
                if (fl.Opt("healthScale") is JNode hsn) { s.LabHealthScale = hsn.Int(); if (s.LabHealthScale < 1 || s.LabHealthScale > 100) throw new FormatException($"{hsn.Path}: healthScale 1..100"); }
                if (fl.Opt("pips") is JNode x1) s.LabPips = x1.Bool();
                if (fl.Opt("close") is JNode x2) s.LabClose = x2.Bool();
                if (fl.Opt("charge") is JNode x3) s.LabCharge = x3.Bool();
                if (fl.Opt("field") is JNode x4) s.LabField = x4.Bool();
                if (fl.Opt("policeWarning") is JNode x5) s.LabWarn = x5.Bool();
                if (fl.Opt("hostilesFireMoving") is JNode x6) s.LabFireMoving = x6.Bool();
                if (fl.Opt("coverSnap") is JNode x9) s.LabCoverSnap = x9.Bool(); // brief 027 B5
                if (fl.Opt("kite") is JNode x10) s.LabKite = x10.Bool(); // brief 027 F2 (Pulse kite-response.md)
                if (fl.Opt("stances") is JNode x7) { s.LabStances = x7.Str(); if (s.LabStances != "two" && s.LabStances != "three") throw new FormatException($"{x7.Path}: stances two or three"); }
                if (fl.Opt("then") is JNode x8) { s.LabThen = x8.Str(); if (s.LabThen != "free" && s.LabThen != "stop") throw new FormatException($"{x8.Path}: then free or stop"); }
                if (fl.Opt("loadout") is JNode lo) { lo.Only("scout", "fixer", "tech", "enforcer"); for (int q = 0; q < 4; q++) if (lo.Opt(Sim.Op.Name((OpId)q)) is JNode a) s.LabLoadout[q] = a.Str(); }
            }
            if (n.Opt("drive") is JNode dr)
            {
                dr.Only("pace", "onBusy", "onRelease", "gap");
                if (dr.Opt("pace") is JNode dp) { s.DrivePace = dp.Int(); if (s.DrivePace < 250 || s.DrivePace > 4000) throw new FormatException($"{dp.Path}: pace 250..4000 per mille"); }
                if (dr.Opt("gap") is JNode dg) { s.DriveGap = dg.Int(); if (s.DriveGap < 0 || s.DriveGap > 500) throw new FormatException($"{dg.Path}: gap 0..500 cm"); }
                if (dr.Opt("onBusy") is JNode db) { s.DriveOnBusy = db.Str(); if (s.DriveOnBusy != "cancel" && s.DriveOnBusy != "hold") throw new FormatException($"{db.Path}: cancel or hold"); }
                if (dr.Opt("onRelease") is JNode dl) { s.DriveOnRelease = dl.Str(); if (s.DriveOnRelease != "stand" && s.DriveOnRelease != "resume") throw new FormatException($"{dl.Path}: stand or resume"); }
            }
            if (n.Opt("holds") is JNode hs) foreach (var h in hs.Items)
            {
                h.Only("door", "from", "until"); int f = h["from"].Int(), u = h["until"].Int();
                if (f < 0 || u <= f) throw new FormatException($"{h.Path}: a held door runs from ≥ 0 to a later until (seconds)");
                s.Holds.Add((h["door"].Str(), f, u));
            }
            if (n.Opt("rounds") is JNode ro) s.Rounds = ro.Bool();
            if (n.Opt("rivalsPublic") is JNode rp) { s.RivalsPublic = rp.Str(); if (Array.IndexOf(Rules.RivalsPublics, s.RivalsPublic) < 0) throw new FormatException($"{rp.Path}: rivalsPublic is one of {string.Join(", ", Rules.RivalsPublics)}"); }
            if (n.Opt("followLock") is JNode fk) s.FollowLock = fk.Bool(); // tuning.md §11.5
            if (n.Opt("weaponsFree") is JNode wf) s.WeaponsFree = wf.Bool(); // tuning.md §14
            if (n.Opt("pinCell") is JNode pc) s.PinCell = pc.Bool(); // brief 039
            if (n.Opt("oneInput") is JNode oin) s.OneInput = oin.Bool(); // brief 039, test only
            if (n.Opt("pre039") is JNode p39) s.Pre039 = p39.Bool(); // brief 039, test only
            if (n.Opt("grilles") is JNode gr) foreach (var x in gr.Items) { x.Only("id", "at"); var id = x["id"].Str(); if (id.Length == 0 || s.Grilles.Exists(q => q.id == id)) throw new FormatException($"{x.Path}: a grille has a unique id"); s.Grilles.Add((id, Pt.Of(x["at"]))); } // brief 039, test only
            if (n.Opt("heat") is JNode ht) { s.Heat = ht.Int(); if (s.Heat < 0 || s.Heat > 1000) throw new FormatException($"{ht.Path}: heat 0..1000"); } // brief 031
            if (n.Opt("standing") is JNode st) { s.Standing = st.Int(); if (s.Standing < -1000 || s.Standing > 1000) throw new FormatException($"{st.Path}: standing -1000..1000"); }
            if (n.Opt("untie") is JNode un) s.Untie.AddRange(un.Strs());
            if (n.Opt("security") is JNode sl) { s.Security = sl.Int(); if (s.Security < 0 || s.Security > 4) throw new FormatException($"{sl.Path}: security level 0..4"); }
            if (n.Opt("arms") is JNode ar) { s.Arms = ar.Int(); if (s.Arms < 0 || s.Arms > 100) throw new FormatException($"{ar.Path}: arms 0..100"); }
            if (n.Opt("finalFight") is JNode ff) s.FinalFight = ff.Bool();
            if (n.Opt("crowd") is JNode cr) s.Crowd = cr.Bool();
            if (n.Opt("rivalsAt") is JNode r) s.RivalsAtS = r.Int();
            if (n.Opt("turned") is JNode t) s.Turned.AddRange(t.Strs());
            if (n.Opt("known") is JNode k) foreach (var kv in k.Entries) s.Known[kv.Key] = kv.Value.Strs();
            if (n.Opt("temperament") is JNode tm) foreach (var kv in tm.Entries) s.Temperament[kv.Key] = kv.Value.Str();
            if (n.Opt("cash") is JNode c) s.Cash = c.Long();
            if (n.Opt("counts") is JNode cn) ParseCounts(s, cn); // brief 098 step 0: the dial
            return s;
        }
        public string Canonical()
        {
            var w = new JWriter().Obj();
            if (Seed != 0) w.Prop("seed", Seed); // brief 080: only a seeded start adds bytes
            if (Pre080) w.Prop("pre080", true); // brief 080, test only: likewise
            if (OldClock) w.Prop("oldClock", true); // brief 080, test only: likewise
            if (RivalsAtS.HasValue) w.Prop("rivalsAt", RivalsAtS.Value);
            if (Turned.Count > 0) { w.OpenArr("turned"); foreach (var x in Turned) w.Str(x); w.EndArr(); }
            if (Known.Count > 0) { w.Open("known"); var ks = new List<string>(Known.Keys); ks.Sort(StringComparer.Ordinal); foreach (var k in ks) { w.OpenArr(k); foreach (var x in Known[k]) w.Str(x); w.EndArr(); } w.EndObj(); }
            if (Temperament.Count > 0) { w.Open("temperament"); var ks = new List<string>(Temperament.Keys); ks.Sort(StringComparer.Ordinal); foreach (var k in ks) w.Prop(k, Temperament[k]); w.EndObj(); }
            if (Cash.HasValue) w.Prop("cash", Cash.Value);
            if (!FinalFight) w.Prop("finalFight", false);
            if (!Crowd) w.Prop("crowd", false);
            if (Arms.HasValue) w.Prop("arms", Arms.Value);
            if (Untie.Count > 0) { w.OpenArr("untie"); foreach (var x in Untie) w.Str(x); w.EndArr(); }
            if (Security.HasValue && !LegacyCanonical) w.Prop("security", Security.Value);
            if (Standing.HasValue) w.Prop("standing", Standing.Value); // only a start that fixes it adds bytes: every earlier start hashes as before
            if (RivalsPublic != null) w.Prop("rivalsPublic", RivalsPublic); // likewise
            if (Heat.HasValue) w.Prop("heat", Heat.Value); // brief 031: likewise
            if (FollowLock.HasValue) w.Prop("followLock", FollowLock.Value); // tuning.md §11.5: likewise
            if (WeaponsFree.HasValue) w.Prop("weaponsFree", WeaponsFree.Value); // tuning.md §14: likewise
            if (PinCell.HasValue) w.Prop("pinCell", PinCell.Value); // brief 039: likewise
            if (OneInput.HasValue) w.Prop("oneInput", OneInput.Value); // brief 039, test only: likewise
            if (Pre039) w.Prop("pre039", true); // brief 039, test only: likewise
            if (Grilles.Count > 0) { w.OpenArr("grilles"); foreach (var (id, at) in Grilles) w.Obj().Prop("id", id).Key("at").Raw($"[{at.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{at.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}]").EndObj(); w.EndArr(); } // brief 039, test only: likewise
            if (Rounds.HasValue) w.Prop("rounds", Rounds.Value); // brief 022: likewise
            if (DrivePace.HasValue || DriveGap.HasValue || DriveOnBusy != null || DriveOnRelease != null) // brief 025: only a playground start adds bytes
            {
                w.Open("drive");
                if (DrivePace.HasValue) w.Prop("pace", DrivePace.Value);
                if (DriveOnBusy != null) w.Prop("onBusy", DriveOnBusy);
                if (DriveOnRelease != null) w.Prop("onRelease", DriveOnRelease);
                if (DriveGap.HasValue) w.Prop("gap", DriveGap.Value);
                w.EndObj();
            }
            if (FightLabOn) // brief 027: only a run with the fight lab on adds bytes (today's preset, lab off, hashes as before)
            {
                w.Open("fightLab").Prop("on", true);
                if (LabHealthScale.HasValue) w.Prop("healthScale", LabHealthScale.Value);
                if (LabPips.HasValue) w.Prop("pips", LabPips.Value);
                if (LabStances != null) w.Prop("stances", LabStances);
                if (LabThen != null) w.Prop("then", LabThen);
                if (LabClose.HasValue) w.Prop("close", LabClose.Value);
                if (LabCharge.HasValue) w.Prop("charge", LabCharge.Value);
                if (LabField.HasValue) w.Prop("field", LabField.Value);
                if (LabWarn.HasValue) w.Prop("policeWarning", LabWarn.Value);
                if (LabFireMoving.HasValue) w.Prop("hostilesFireMoving", LabFireMoving.Value);
                if (LabCoverSnap.HasValue) w.Prop("coverSnap", LabCoverSnap.Value);
                if (LabKite.HasValue) w.Prop("kite", LabKite.Value);
                bool any = false; foreach (var a in LabLoadout) any |= a != null;
                if (any) { w.Open("loadout"); for (int k = 0; k < 4; k++) if (LabLoadout[k] != null) w.Prop(Sim.Op.Name((OpId)k), LabLoadout[k]!); w.EndObj(); }
                w.EndObj();
            }
            if (Holds.Count > 0) { w.OpenArr("holds"); foreach (var (d, f, u) in Holds) w.Obj().Prop("door", d).Prop("from", f).Prop("until", u).EndObj(); w.EndArr(); } // test only: likewise
            if (ValeWaits) w.Prop("valeWaits", true); if (OneCar) w.Prop("oneCar", true); // brief 059, engine 0.42.0, test only: likewise
            if (CrewGone.Count > 0) { w.OpenArr("crewGone"); foreach (var x in CrewGone) w.Str(x); w.EndArr(); } // engine 0.33.0, test only: likewise
            if (Counts.Count > 0) { w.Open("counts"); foreach (var kv in Counts) w.Prop(kv.Key, kv.Value); w.EndObj(); } // brief 098 step 0: only a start that sets the dial adds bytes
            return w.EndObj().ToString();
        }
    }
}
