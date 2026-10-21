// The rules, read as data and compiled at load: contracts/rules/rulebook.json (Keel) and contracts/people/people.rules.json (Motive).
// Everything the engine reads is checked when the rules load and turned into typed fields: every object's keys (unknown = reject,
// GDD §01), every condition and effect kind with its values, where each kind may stand (no cash-covers outside an event's needs,
// nothing that prices inside a price line, no fit outside a Turn), every v:/p: ref resolved to an integer within its unit's bounds,
// every id (temperament, approach, scale, need, tie kind, memory act, traceable act, event, debrief line) known. After load nothing
// here reads JSON by path or throws on rule content, so Game.Step() can't fail on a bad rulebook (Redline E1).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public enum Band { Hero, Neutral, Menace }
    public enum Fit { Suits, Worse, Refused }
    /// <summary>A shooter's gait this tick (tuning.md §8.3): from its own step. Hostiles have one moving pace: a walk.</summary>
    public enum Gait { Stand, Walk, Run, RunAway }

    public sealed class Cond
    {
        public readonly string K; public readonly JNode N;
        public Cond(JNode n) { N = n; K = n["k"].Str(); Rules.CheckCond(this); }
        public string S(string key) => N[key].Str();
        public bool B(string key) => N[key].Bool();
        public List<string> L(string key) => N[key].Strs();
    }
    public sealed class Eff
    {
        public readonly string Do; public readonly JNode N;
        public Eff(JNode n) { N = n; Do = n["do"].Str(); Rules.CheckEff(this); }
        public string S(string key) => N[key].Str();
        public List<string> L(string key) => N[key].Strs();
    }
    public sealed class Line
    {
        public readonly string Id; public readonly List<Cond> When = new List<Cond>(); public readonly string? Usd, S, Trail, Orders;
        public long UsdV, SV, TrailV, OrdersV; // resolved at load
        public Line(JNode n)
        {
            n.Only("line", "when", "usd", "s", "trail", "orders");
            Id = n["line"].Str();
            if (n.Opt("when") is JNode w) foreach (var c in w.Items) When.Add(new Cond(c));
            Usd = n.Opt("usd")?.Str(); S = n.Opt("s")?.Str(); Trail = n.Opt("trail")?.Str(); Orders = n.Opt("orders")?.Str();
        }
    }
    public sealed class ROutcome
    {
        public readonly string Id; public readonly List<Cond> When = new List<Cond>(); public readonly List<Eff> Changes = new List<Eff>();
        public ROutcome(JNode n)
        {
            n.Only("id", "when", "changes"); Id = n["id"].Str();
            foreach (var c in n["when"].Items) When.Add(new Cond(c));
            foreach (var e in n["changes"].Items) Changes.Add(new Eff(e));
        }
    }
    public sealed class Also
    {
        public readonly List<Cond> When = new List<Cond>(); public readonly List<Eff> Changes = new List<Eff>();
        public Also(JNode n) { n.Only("when", "changes"); foreach (var c in n["when"].Items) When.Add(new Cond(c)); foreach (var e in n["changes"].Items) Changes.Add(new Eff(e)); }
    }
    public sealed class Witness
    {
        public readonly List<Cond> When = new List<Cond>(); public readonly string Act;
        public Witness(JNode n) { n.Only("when", "act"); if (n.Opt("when") is JNode w) foreach (var c in w.Items) When.Add(new Cond(c)); Act = n["act"].Str(); }
    }
    public sealed class Event
    {
        public readonly string Id, By, Phase;
        /// <summary>rulebook privacy (Sky 2026-10-16): a turning conversation; onlookers learn nothing from it (no witnesses, nobody on the card).</summary>
        public readonly bool Private;
        public readonly List<Cond> Needs = new List<Cond>(); public readonly List<Line> Price = new List<Line>();
        public readonly List<ROutcome> Outcomes = new List<ROutcome>(); public readonly List<Also> Also = new List<Also>(); public readonly List<Witness> Witnesses = new List<Witness>();
        public Event(JNode n)
        {
            n.Only("id", "by", "phase", "private", "tag", "src", "proposed", "reason", "needs", "price", "outcomes", "also", "witnesses", "how", "order", "band");
            Id = n["id"].Str(); By = n["by"].Str(); Phase = n["phase"].Str();
            if (n.Opt("private") is JNode pv) Private = pv.Bool();
            else if (By == "fixer" || By == "scout" || By == "cell") throw new FormatException($"{Id}: an event by the Fixer, the Scout or the cell says whether it is private (rulebook privacy)");
            foreach (var c in n["needs"].Items) Needs.Add(new Cond(c));
            if (n.Opt("price") is JNode p) foreach (var l in p.Items) Price.Add(new Line(l));
            foreach (var o in n["outcomes"].Items) Outcomes.Add(new ROutcome(o));
            if (n.Opt("also") is JNode a) foreach (var x in a.Items) Also.Add(new Also(x));
            if (n.Opt("witnesses") is JNode w) foreach (var x in w.Items) Witnesses.Add(new Witness(x));
            if (Outcomes.Count == 0 || Outcomes[Outcomes.Count - 1].When.Count != 0) throw new FormatException($"{Id}: the last outcome must have no conditions");
        }
        public bool IsTurn => Id.StartsWith("turn.", StringComparison.Ordinal);
        public string Approach => IsTurn ? Id.Substring(5) : "";
    }

    /// <summary>One of fight.public's outcomes (Sky 2026-10-16): its conditions (fight kinds only), and the Standing it moves with its cause.</summary>
    public sealed class FightOutcome { public string Id = "", Cause = ""; public int Delta; public readonly List<Cond> When = new List<Cond>(); }

    public sealed class Temperament
    {
        public string Id = ""; public HashSet<string> RespondsTo = new HashSet<string>(StringComparer.Ordinal), Refuses = new HashSet<string>(StringComparer.Ordinal);
        public bool ReportsApproach; public int[] Start = new int[4];
    }
    public sealed class Approach { public string Id = "", Requires = ""; public int Usd, S, Trail; }
    public sealed class Need { public string Id = "", Requires = ""; public HashSet<string> MetBy = new HashSet<string>(StringComparer.Ordinal); }

    public sealed class Rules
    {
        public static readonly string[] ScaleNames = { "bravery", "resentment", "fear", "loyalty" };
        public const int Bravery = 0, Resentment = 1, Fear = 2, Loyalty = 3;
        static readonly string[] Bands = { "hero", "neutral", "menace" }, Fits = { "suits", "worse", "refused" }, Knows = { "routine", "need", "temperament", "refused", "secret" };

        static readonly Dictionary<string, string[]> CondKeys = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["tier"] = new[] { "is" }, ["turned"] = new[] { "is", "of" }, ["known"] = new[] { "what", "is" }, ["leverage"] = new[] { "approach" }, ["lover-within"] = new[] { "m" }, ["requires"] = new[] { "approach" },
            ["need-met-by"] = new[] { "approach" }, ["need-is"] = new[] { "in" }, ["need-met"] = new[] { "is" }, ["reach"] = new[] { "m" },
            ["cash-covers"] = new string[0], ["memory"] = new[] { "act", "has", "this", "of" }, ["fit"] = new[] { "is" }, ["temperament"] = new[] { "in" },
            ["standing"] = new[] { "band" }, ["orders-reached"] = new[] { "is" }, ["reports"] = new string[0], ["reports-approach"] = new string[0],
            ["outcome"] = new[] { "is" }, ["talker"] = new[] { "is" }, ["lies"] = new[] { "is" }, ["holds-fact"] = new[] { "is", "how" },
            ["scale"] = new[] { "scale", "op", "than" }, ["trail"] = new[] { "atLeast" }, ["keeper-position"] = new string[0], ["introduces-along"] = new string[0],
            ["tie"] = new[] { "kinds" }, ["tie-between-holds"] = new[] { "is" }, ["act"] = new[] { "in" }, ["within"] = new[] { "m", "sightline" },
            ["cell"] = new[] { "is" }, ["called"] = new[] { "is" }, ["exposed"] = new[] { "is" }, ["count"] = new[] { "event", "below" },
            ["done-to"] = new[] { "in" }, ["alive"] = new[] { "is" }, ["standing-moved"] = new[] { "is" }, // brief 014: Standing's word and drift
            // Sky 2026-10-16 (briefs/014/motive-standing.md, a public fight): fight.public reads one fight
            ["fight"] = new[] { "quiet" }, ["public"] = new[] { "m" }, ["fired-on"] = new[] { "in" }, ["rivals-public"] = new[] { "in" }, ["fired-first"] = new[] { "is" }, ["rivals-broken"] = new[] { "is" },
        };
        static readonly Dictionary<string, string[]> EffKeys = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["charge"] = new[] { "parts" }, ["turn"] = new string[0], ["move"] = new[] { "on" }, ["need-met"] = new string[0], ["learn"] = new[] { "what", "of" },
            ["forget"] = new[] { "what" }, ["remember"] = new[] { "act" }, ["report"] = new string[0], ["expose"] = new string[0],
            ["trail"] = new[] { "add", "per" }, ["standing"] = new[] { "step", "per" }, ["alert"] = new[] { "atLeast", "per" }, ["lead"] = new[] { "from" },
            ["spend-hop"] = new string[0], ["tell"] = new[] { "along", "hops" }, ["lapse-tie"] = new string[0], ["replace-need"] = new[] { "stream" },
            ["schedule"] = new[] { "fight" }, ["debrief"] = new[] { "line" },
            ["word-standing"] = new[] { "toward", "step", "cap" }, ["drift-standing"] = new[] { "step", "to" }, // brief 014
            ["fight-standing"] = new[] { "toward", "step", "cause" }, // Sky 2026-10-16: fight.public
        };
        /// <summary>What the cell did to a person this mission that their word carries (standing.word; rulebook.schema.ts DONE_TO).</summary>
        public static readonly string[] DoneTo = { "helped", "pressured", "knocked-out" };
        /// <summary>The Standing line's causes besides seen-&lt;traceable act&gt; (rulebook standing.causes; rulebook.schema.ts STANDING_CAUSES).</summary>
        public static readonly string[] StandingFixedCauses = { "found-killing", "word-helped", "word-pressured", "word-knocked-out", "balanced", "drift-down", "drift-up", "steady" };
        public static readonly string[] StandingLineFields = { "district", "from", "to", "band", "cause", "who" };
        /// <summary>Whom the cell fired on in a fight, and how the public sees a mission's rivals (fight.public; rulebook.schema.ts FOES, RIVALS_PUBLIC).</summary>
        public static readonly string[] Foes = { "security", "rivals" }, RivalsPublics = { "menace", "neutral" };
        static readonly string[] FightKinds = { "fired-on", "rivals-public", "fired-first", "rivals-broken" };
        /// <summary>The kinds the engine itself evaluates inside a mission (the interpreted events: scout.*, turn.*; the reports table).</summary>
        static readonly HashSet<string> MissionKinds = new HashSet<string>(StringComparer.Ordinal)
        { "tier", "turned", "known", "leverage", "lover-within", "requires", "need-met-by", "need-is", "need-met", "reach", "cash-covers", "memory", "fit", "temperament", "standing", "orders-reached", "reports", "reports-approach", "outcome", "talker", "lies", "holds-fact", "scale", "trail" };
        /// <summary>The effects the engine applies inside a mission's interpreted events.</summary>
        static readonly HashSet<string> MissionEffects = new HashSet<string>(StringComparer.Ordinal) { "charge", "turn", "move", "need-met", "learn", "forget", "remember", "report", "debrief" };

        internal static void CheckCond(Cond c)
        {
            if (!CondKeys.TryGetValue(c.K, out var keys)) throw new FormatException($"{c.N.Path}: condition kind '{c.K}' is unknown (unknown = reject)");
            var all = new List<string>(keys) { "k" }; c.N.Only(all.ToArray());
        }
        internal static void CheckEff(Eff e)
        {
            if (!EffKeys.TryGetValue(e.Do, out var keys)) throw new FormatException($"{e.N.Path}: effect kind '{e.Do}' is unknown (unknown = reject)");
            var all = new List<string>(keys) { "do" }; e.N.Only(all.ToArray());
        }

        public readonly JNode Book, People;
        readonly Dictionary<string, long> values = new Dictionary<string, long>(StringComparer.Ordinal);
        readonly Dictionary<string, long> refs = new Dictionary<string, long>(StringComparer.Ordinal);
        readonly Dictionary<string, Event> events = new Dictionary<string, Event>(StringComparer.Ordinal);
        public readonly List<Event> Events = new List<Event>();
        readonly Dictionary<string, Temperament> temps = new Dictionary<string, Temperament>(StringComparer.Ordinal);
        public readonly List<string> TemperamentIds = new List<string>();
        readonly Dictionary<string, Approach> approaches = new Dictionary<string, Approach>(StringComparer.Ordinal);
        public readonly Dictionary<string, Need> Needs = new Dictionary<string, Need>(StringComparer.Ordinal);
        public readonly List<string> NeedIds = new List<string>();
        public readonly HashSet<string> TieKinds = new HashSet<string>(StringComparer.Ordinal);
        public readonly int[] ScaleMin = new int[4], ScaleMax = new int[4];
        public readonly List<string>[] ScaleSteps = new List<string>[4];
        /// <summary>A scale value as its step word (people.rules scales.*.steps: every number reads as a word).</summary>
        public string Step(int scale, int v) => v >= 0 && v < ScaleSteps[scale].Count ? ScaleSteps[scale][v] : v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public readonly int[] TalkerLeft, TalkerRight; public readonly int LieBraveryAtLeast; public readonly bool LieTalker, LieHelped; public readonly string LieGivesHero, LieGivesOtherwise;
        public readonly List<string> Facts;
        public readonly List<string> KnownBy;
        readonly Dictionary<string, Fit> fitRows = new Dictionary<string, Fit>(StringComparer.Ordinal), fitApproach = new Dictionary<string, Fit>(StringComparer.Ordinal);
        public readonly List<List<Cond>> Reports = new List<List<Cond>>();
        public readonly Dictionary<string, (int scale, int by)[]> Ladders = new Dictionary<string, (int, int)[]>(StringComparer.Ordinal);
        public readonly Dictionary<string, int> Traceable = new Dictionary<string, int>(StringComparer.Ordinal); // +1 hero, −1 menace, 0 none
        public readonly HashSet<string> MemoryActs = new HashSet<string>(StringComparer.Ordinal);
        public readonly Dictionary<string, string> FaceFact = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly HashSet<string> DebriefLines = new HashSet<string>(StringComparer.Ordinal);
        public readonly string HelpedAct, RefusedAct;
        public readonly int MemoryCap;
        public readonly int[] AlertByIncident; public readonly int GunfireAlert, ViolenceSeen;
        public readonly int StandingStart, StandingStep, HeroAt, MenaceAt;
        // brief 014 (Sky 2026-10-16, briefs/014/motive-standing.md): the word between missions and the drift back to the start
        public readonly int WordStandingStep, WordStandingCap, StandingDrift;
        /// <summary>fight.public (Sky 2026-10-16, briefs/014/motive-standing.md, a public fight): a fight outside the final fight ends this
        /// long after its last shot; it is public when a person of the city who is not security stands within this reach of a shot; its
        /// outcomes in order, the first that holds moves Standing once when it ends; the rivals' public face when the mission names none.</summary>
        public readonly int FightQuietTicks, FightPublicCm;
        public readonly List<FightOutcome> FightOutcomes = new List<FightOutcome>();
        public readonly string RivalsPublicDefault = "";
        public readonly HashSet<string> WordTies = new HashSet<string>(StringComparer.Ordinal), StandingCauses = new HashSet<string>(StringComparer.Ordinal);
        public readonly int WitnessRangeCm, WitnessTrail, ApproachReachCm, ListenReachCm, TrailOpen;
        // cell
        /// <summary>cell.loop.targets: the cameras the cabinet loops, by site camera id, or "world" for the city's own (brief 013 D).</summary>
        public readonly List<string> LoopTargets = new List<string>();
        /// <summary>cell.drive (brief 025, EXPERIMENTAL): the driven agent's rules; null when the rulebook has none (drive = unknown verb).</summary>
        public readonly DriveRules? Drive;
        public sealed class DriveRules { public int SlackCm, BurstTicks, BusyPushCm, GapCm; public string OnBusy = "cancel", OnRelease = "stand"; }
        /// <summary>fight.lab (brief 027, EXPERIMENTAL): the fight playground's rules; null when the rulebook has none (a start that turns it on is
        /// refused). Read only by a run whose start turns it on (Start.FightLabOn); off, every rule is today's.</summary>
        public readonly LabRules? Lab;
        public readonly int CellSpeed, RunSpeed, RunAlert, ActReachCm, AtReachCm, LoopS, LoopTrail, TetherCm, RerouteS, RerouteTrail, RerouteAwayS, MisleadS, MisleadTrail, MisleadAwayS, CallTrail, EnterS, EnterAlarmS, TakeS, TakeTrailUnturned, TakeStruggleS, CarryS;
        // weapons, health and what a shot costs (Sky 2026-10-15, briefs/011/motive-fights.md)
        public readonly Weapon Pistol, Carbine;
        // cancel (Sky 2026-10-15 playtest): the lines that may change; the rest are checked to say what the engine does
        public readonly string CancelTurn, CancelEnter, CancelTake;
        // the summons and the secret (Motive, briefs/011/motive-lovers.md)
        public readonly int SummonStayS, SummonWalkMaxS, SummonAlertMax, SummonsPerShift, TogetherCm;
        public readonly HashSet<string> SecretIds = new HashSet<string>(StringComparer.Ordinal);
        // the fight AI (Pulse, briefs/011/pulse-fight-ai.md)
        public readonly int CoverSnapCm, CoverQFull, CoverQPartial, ModCoverPartial, CoverRetreatCm, CoverStepCloserCm, CoverStepGapS, CoverLeaveS, EngageLapseS, ReacquireS, StopReachPerMille, OverflowCm,
            SeekRivalCm, SeekPoliceCm, SeekResponseCm, SeekAllyCm, SeekGuardCm, RivalGoneCm, GuardReturnS, PatrolResumeS;
        // the crowd in a fight (Pulse §4)
        public readonly int CrowdScatterCm, CrowdStartleTicks, CrowdStartleFearfulTicks, CrowdRunSpeed, CrowdFleeMaxCm, CrowdQuietS, CrowdOccludeCm, CrowdPaceSpreadPct;
        /// <summary>car.drive (brief 023, engine 0.8.2): the rule's words; the engine routes every moving car on the site's drive grid (Site.Drive).</summary>
        public readonly string CarDrive = "";
        /// <summary>car.pace (brief 023, Motive 2026-10-19): cm/s (= mm a tick) on a carriageway cell and on a drive or bay cell.</summary>
        public readonly int CarRoadSpeed, CarYardSpeed;
        /// <summary>Brief 030: a fire order out of reach walks into the effective ring (weapons.close); a weapon drawn by fire or panic holsters this long after its fight (weapons.autoHolster, s).</summary>
        public readonly bool FireClose; public readonly int AutoHolsterS;
        /// <summary>Sky 2026-10-20 (tuning.md §7.9): today's kite response (fight.kite): on, and its numbers (ticks, cm), as the lab's are.</summary>
        public readonly bool KiteOn; public readonly int KiteCoverHold, KiteReact, KiteReplan, KiteChaseCm, KiteLapse, KiteHide, KiteContactCm, KiteRearm, KiteHideMax;
        /// <summary>Tuning.md §8.3 (Sky 2026-10-21, engine 0.15.0): fire on the move. A run toward or across, a run away (within runAwayCone of
        /// straight away from the target), a running target past point blank (one step, never with cover); the away cone as a per-mille cosine.
        /// PointBlankCover: today's cover counts inside point blank (§8.4: the switch Keel measures; true is today's rule).</summary>
        /// <summary>ai.cover.stepFires: true, a step to cover fires at the walk's chance; false is tuning.md §8.3's fallback (a rival's cover
        /// step and a cover-snap walk fire nothing), measured, not adopted.</summary>
        public readonly bool CoverStepFires;
        public readonly int ModRunning, ModRunningAway, ModTargetRunning, RunAwayConeDeg, RunAwayCosPm; public readonly bool PointBlankCover;
        public readonly int PointBlankCm, AccPoint, AccEff, AccLong, AccStart, ModMoving, ModCover, ModVale, ModUntrained, DrawTicks, HolsterTicks, AimNewTicks, ReactS, LineLapseS, SidearmArms;
        public readonly int HealthCar, CarDisabledAt, CarBurnS, CarBlastCm, CarBlastDamage; // tuning.md §10.7 (Sky 2026-10-21, cars "as in Syndicate"): car.target, ×healthScale in the lab (not burn, not the ring)
        public readonly string WeaponsSettle = ""; // tuning.md §10.2 settle: the rule text (no number: it waits weapons.aimNew)
        /// <summary>Tuning.md §11 (engine 0.17.0): the follow ring round the driven agent (weapons.follow, follow-m, cm) and the trigger's rule text.</summary>
        public readonly int FollowCm; public readonly string WeaponsTrigger = "";
        public readonly int HealthCamera; // tuning.md §9.2 gap 2 (Sky 2026-10-21): a camera's health (one sidearm hit; ×healthScale in the lab)
        public readonly int HealthEnforcer, HealthOp, HealthGuard, HealthOfficer, HealthRival, HealthPatrol, HealthCivilian, ResponseOfficers, HitTrail, DownTrail, GunfireTrail, DrawWindowS, DrawSeenSecurity, DrawSeenPeople, LoudHoldTicks;
        // security
        public readonly int RespWitnessS, RespSeenS, RespUnseenS, RespCivilianS, RespRivalsUnseenS, SecuritySpeed, RivalsSpeed, RivalsDrawnS, RivalsRetreatS, PatrolBreakCm, PatrolPeriodS, PatrolChase;
        // perception
        public readonly int NearCm, Eye, DoorReachCm, DoorHalfCm, FacingReachCm, FacingHalfCm, SecReachCm, SecHalfCm, RivReachCm, RivHalfCm, PatrolDepthCm, PatrolHalfBase, PatrolHalfPerMille;
        // the cell's knowledge of watchers (Sky 2026-10-16: cones are the Scout's skill): Watch's time and reach on a watcher, the watched
        // seconds a routine needs, and per kind (camera, guard, patrol, security, rivals) how its sight is learned and what its routine is
        public readonly int SightWatchS, SightRoutineS;
        /// <summary>Brief 031 (Sky 2026-10-20): the Watch's reach by operative (sight.watch.reach.*; Scout 30, Fixer 12, Tech 24, Enforcer 3 m), indexed by OpId.</summary>
        public readonly int[] WatchReachCms = new int[4];
        public int WatchReachCm(OpId op) => WatchReachCms[(int)op];
        /// <summary>The Scout's Watch reach (kept for hosts that read one reach; brief 031 gives each operative its own: WatchReachCm).</summary>
        public int SightReachCm => WatchReachCms[(int)OpId.Scout];
        /// <summary>Brief 031 §2 (rulebook attention): the Heat bands (ascending from heatFrom 0) with their marks in seconds; whether the guard and
        /// the officers sense within AttnNearCm all round (people) or by their cone; the Alert a loiterer reported raises; the report's scan and call.</summary>
        public readonly List<(string id, int heatFrom, int noticeS, int waryS, int reportS)> AttnBands = new List<(string, int, int, int, int)>();
        public readonly bool AttnGuardNear, AttnOfficerNear; public readonly int AttnNearCm, AttnAlert, AttnScanS, AttnCallS;
        /// <summary>The band a Heat falls in: the last whose heatFrom it reaches.</summary>
        public (string id, int heatFrom, int noticeS, int waryS, int reportS) AttnBand(int heat) { var b = AttnBands[0]; foreach (var x in AttnBands) if (heat >= x.heatFrom) b = x; return b; }
        public readonly Dictionary<string, (string learn, string routine)> SightKinds = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        /// <summary>sight.kinds.*.by: the operatives who may Watch a kind (Sky 2026-10-18), empty for a kind known on arrival.</summary>
        public readonly Dictionary<string, string[]> SightBy = new Dictionary<string, string[]>(StringComparer.Ordinal);
        public static readonly string[] WatcherKinds = { "camera", "guard", "patrol", "security", "rivals" };
        // brief 013 (Sky 2026-10-16): the takedown, bodies, the people who report them, the response dial, the campaign's cases
        public readonly int KoS, KillS, StruggleS, TakedownReachCm, KillTrail, KoAlertMax, FoundKillStanding, WakeS, StaggerS, RestrainS, FreeS, CallOutS,
            CarryUntrainedSpeed, DropS, StashContainerS, StashDoorS, StashDoorHolds, ServicePeriodS, ServiceDwellS;
        public readonly int WitnessReactS, CallS, PostCallS, RobotReportS, CameraScanS, ReportCap, ScreamCm, ScreamFear, TerrifiedFear, MercenaryAlert, BodyCheckTicks, BodyCheckS, TellCm;
        public readonly int AlertSeen, AlertCarry, AlertDown, AlertDead, AlertWoken, AlertNoise, AlertMark, AlertMissing;
        public readonly HashSet<string> StaffJobs = new HashSet<string>(StringComparer.Ordinal), KinTies = new HashSet<string>(StringComparer.Ordinal), CloseTies = new HashSet<string>(StringComparer.Ordinal); // kin: the witness row (lover, family); close: whose absence or loss a person feels (expectations, the alarm's places, grief and worry)
        public readonly List<string> TellTies = new List<string>();
        public readonly DialLevel[] Dial = new DialLevel[5];
        public readonly int RobotSpeed, RobotWireS, RobotReachCm, ReportReachCm, DockReachCm, LaunchGapTicks, CheckS, SearchS, SearchStashCm, SearchStashS, StandDownInquiryS, StandDownSearchS, AlarmSpread, CombatantCap, CheckInPeriodS, SuspicionS, MeetCm;
        public readonly Dictionary<string, int> SuspicionPm = new Dictionary<string, int>(StringComparer.Ordinal);
        public readonly int MissingTrail, MissingTiesFear, AreaWatchStep, AreaWatchMissions, CasesPerLevel, CaseLevelCap, CaseCloseMissions, CaseTrailEq; // CaseTrailEq: the checker's price of an open case (rev 4 R4.4), never Heat
        /// <summary>Each memory act's facts as rulebook memory.acts lists them (empty for a told entry, whose facts it carries).</summary>
        public readonly Dictionary<string, List<string>> ActFacts = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        // the final fight
        public readonly int SafeHostiles, SafeMinus, SafeWarnS, SafeAmbushCm, LoudOfficers, LoudAlert, LoudCivilian, HoldCm; public readonly bool FightTraced;
        // brief 018 (Sky 2026-10-18, the getaway): the exit lane's length and its blockers, boarding, and what a capture costs the campaign
        public readonly int BlockCm, BlockNearCm, BlockFarCm, Blockers, BoardS, CapturedTrail, CustodyFear, CustodyLoyalty, AbandonedResentment, AbandonedLoyalty, CustodyMissions;
        /// <summary>The founding (training) cell's scales (campaign.custody.cellStart), in ScaleNames order: bravery, resentment, fear, loyalty.</summary>
        public readonly int[] CellStart = new int[4];
        // brief 022 (Pulse): a server's round: the linger after an order on her, the summoned lover's stop short of her post, the beat they part
        public readonly int RoundsLingerS, PartnersCloseCm, PartnersClearCm, PartnersPartS;
        public readonly HashSet<string> RoundHolds = new HashSet<string>(StringComparer.Ordinal), RoundEnds = new HashSet<string>(StringComparer.Ordinal);
        // the investigation and between missions
        public readonly int InvestigatorsBase, InvestigatorsPerAlert, InvestigatorHops, WordHops, WordQuietLoyalty, IntroductionsPerContact, NeedWeightScale;
        public readonly Dictionary<string, string> StreamKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly Dictionary<string, bool> TieIntroduces = new Dictionary<string, bool>(StringComparer.Ordinal);
        public readonly List<string> ScaleStepNames = new List<string>();

        public Rules(string rulebookJson, string peopleJson)
        {
            Book = JNode.Parse(rulebookJson, "rulebook"); People = JNode.Parse(peopleJson, "people");
            var b = Book; var pp = People;
            b.Only("schema", "author", "gdd", "rule", "reads", "tags", "units", "values", "standing", "alert", "fit", "reports", "ladders", "memory", "exposure", "traceable", "privacy", "cell", "security", "weapons", "ai", "crowd", "summon", "cancel", "perception", "sight", "attention", "takedown", "witness", "response", "campaign", "rounds", "car", "fight", "investigation", "preview", "streams", "phases", "debrief", "events");
            if (b["schema"].Str() != "suborned.rulebook/1") throw new FormatException("rulebook schema is not suborned.rulebook/1");
            pp.Only("schema", "author", "gdd", "rule", "person", "tiers", "scales", "temperaments", "institutions", "approaches", "needs", "secrets", "ties", "knowledge", "talker", "positions", "temperamentsBy", "names");
            if (pp["schema"].Str() != "suborned.people.rules/1") throw new FormatException("people rules schema is not suborned.people.rules/1");

            // ---- people rules: what the engine reads, strictly
            for (int i = 0; i < 4; i++)
            {
                var sc = pp["scales"][ScaleNames[i]]; sc.Only("min", "max", "integer", "steps", "to", "src", "proposed", "reason");
                ScaleMin[i] = sc["min"].Int(); ScaleMax[i] = sc["max"].Int();
                if (ScaleMin[i] != 0 || ScaleMax[i] < 1 || ScaleMax[i] > 10) throw new FormatException($"people.scales.{ScaleNames[i]} is {ScaleMin[i]}..{ScaleMax[i]}");
            }
            foreach (var key in pp["scales"].Keys) if (Array.IndexOf(ScaleNames, key) < 0) throw new FormatException($"people.scales.{key} is unknown");
            foreach (var kv in pp["approaches"].Entries)
            {
                if (kv.Key == "src") continue;
                var a = kv.Value; a.Only("requires", "usd", "timeS", "trail", "leaves");
                var ap = new Approach { Id = kv.Key, Requires = a["requires"].Str(), Usd = Bounded(a["usd"], 0, 1_000_000_000), S = Bounded(a["timeS"], 1, 3600), Trail = Bounded(a["trail"], 0, 100) };
                if (ap.Requires != "nothing" && ap.Requires != "routine" && ap.Requires != "need") throw new FormatException($"{a.Path}.requires '{ap.Requires}'");
                approaches[kv.Key] = ap;
            }
            foreach (var kv in pp["temperaments"].Entries)
            {
                var v = kv.Value; v.Only("name", "respondsTo", "refuses", "reportsApproach", "institution", "hero", "menace", "start", "src", "proposed", "reason");
                var t = new Temperament { Id = kv.Key, ReportsApproach = v["reportsApproach"].Bool() };
                foreach (var a in v["respondsTo"].Strs()) t.RespondsTo.Add(Known(approaches, a, "approach", v.Path));
                foreach (var a in v["refuses"].Strs()) t.Refuses.Add(Known(approaches, a, "approach", v.Path));
                var st = v["start"]; st.Only(ScaleNames);
                for (int i = 0; i < 4; i++) { t.Start[i] = st[ScaleNames[i]].Int(); if (t.Start[i] < ScaleMin[i] || t.Start[i] > ScaleMax[i]) throw new FormatException($"{st.Path}.{ScaleNames[i]} is off its scale"); }
                temps[kv.Key] = t; TemperamentIds.Add(kv.Key);
            }
            pp["needs"].Only("rule", "list", "requiresKinds");
            foreach (var kv in pp["needs"]["list"].Entries)
            {
                var v = kv.Value; v.Only("metBy", "help", "requires", "src", "proposed", "reason");
                var n = new Need { Id = kv.Key, Requires = v["requires"].Str() };
                foreach (var a in v["metBy"].Strs()) n.MetBy.Add(Known(approaches, a, "approach", v.Path));
                if (Array.IndexOf(new[] { "nothing", "home", "work", "family-tie", "rival-tie" }, n.Requires) < 0) throw new FormatException($"{v.Path}.requires '{n.Requires}'");
                Needs[kv.Key] = n; NeedIds.Add(kv.Key);
            }
            foreach (var kv in pp["ties"]["kinds"].Entries) { TieKinds.Add(kv.Key); TieIntroduces[kv.Key] = kv.Value["introduces"].Bool(); }
            for (int i = 0; i < 4; i++) { var steps = pp["scales"][ScaleNames[i]].Opt("steps"); ScaleSteps[i] = steps == null ? new List<string>() : steps.Strs(); }
            var tk = pp["talker"]; tk.Only("when", "test", "deterministic", "gives", "facts", "src", "lie");
            tk["test"].Only("left", "op", "right");
            if (tk["test"]["op"].Str() != ">") throw new FormatException("people.talker.test.op is not >");
            TalkerLeft = Scales(tk["test"]["left"].Strs()); TalkerRight = Scales(tk["test"]["right"].Strs());
            Facts = tk["facts"].Strs();
            foreach (var f in new[] { "cell-face", "cell-car", "place-time", "contact" }) if (!Facts.Contains(f)) throw new FormatException($"people.talker.facts lacks '{f}': a captive gives it (brief 018 §3)");
            var lie = tk["lie"]; lie.Only("when", "gives", "src", "proposed", "reason"); lie["when"].Only("talker", "helped", "braveryAtLeast"); lie["gives"].Only("hero", "otherwise");
            LieTalker = lie["when"]["talker"].Bool(); LieHelped = lie["when"]["helped"].Bool(); LieBraveryAtLeast = Bounded(lie["when"]["braveryAtLeast"], 0, ScaleMax[Bravery]);
            LieGivesHero = lie["gives"]["hero"].Str(); LieGivesOtherwise = lie["gives"]["otherwise"].Str();

            // ---- the rulebook's values, bounded by unit (Redline E3, E9)
            foreach (var kv in b["values"].Entries)
            {
                var v = kv.Value; v.Only("v", "unit", "tag", "src", "proposed", "reason");
                var unit = v["unit"].Str(); long x = v["v"].Long();
                var (lo, hi) = unit switch
                {
                    "s" => (1L, 3600L), "usd" => (-1_000_000_000L, 1_000_000_000L), "m" => (0L, 1000L), "cm" => (0L, 100_000L), "cm-per-s" => (1L, 10_000L),
                    "trail" => (0L, 100L), "alert" => (0L, 3L), "standing" => (-1000L, 1000L), "orders" => (0L, 10L), "scale-steps" => (-10L, 10L),
                    "entries" => (1L, 100L), "hops" => (0L, 10L), "count" => (0L, 100L), "per-mille" => (0L, 1_000_000L), "ticks" => (1L, 36_000L), "pct" => (0L, 100L), "deg" => (0L, 360L),
                    _ => throw new FormatException($"{v.Path}.unit '{unit}' is unknown"),
                };
                if (x < lo || x > hi) throw new FormatException($"{v.Path} = {x} is outside {lo}..{hi} for unit {unit}");
                values[kv.Key] = x;
                var tag = v["tag"].Str(); if (tag != "B" && tag != "S" && tag != "L") throw new FormatException($"{v.Path}.tag '{tag}'");
            }
            ResolveAllRefs(b);

            // ---- sections, strictly, into fields
            var s = b["standing"]; s.Only("start", "step", "hero", "menace", "word", "drift", "causes", "rule", "tag", "src", "proposed", "reason");
            StandingStart = RI(s["start"]); StandingStep = RI(s["step"]); HeroAt = RI(s["hero"]); MenaceAt = RI(s["menace"]);
            if (!(MenaceAt < StandingStart && StandingStart < HeroAt)) throw new FormatException("standing.start lies between the Menace and Hero bands");
            s["word"].Only("step", "cap", "ties"); WordStandingStep = RI(s["word"]["step"]); WordStandingCap = RI(s["word"]["cap"]); StandingDrift = RI(s["drift"]);
            if (WordStandingStep < 0 || WordStandingCap < 0 || StandingDrift < 0) throw new FormatException("standing.word and standing.drift are not negative");
            foreach (var t in s["word"]["ties"].Strs()) { if (!TieKinds.Contains(t)) throw new FormatException($"standing.word.ties: tie kind '{t}' is unknown"); WordTies.Add(t); }
            foreach (var kv in s["causes"].Entries) { kv.Value.Str(); StandingCauses.Add(kv.Key); }
            var al = b["alert"]; al.Only("levels", "byIncident", "gunfire", "violenceSeen", "rule", "tag", "src");
            var by = al["byIncident"].Items; AlertByIncident = new int[by.Count]; for (int i = 0; i < by.Count; i++) AlertByIncident[i] = RI(by[i]);
            if (AlertByIncident.Length == 0) throw new FormatException("alert.byIncident is empty");
            GunfireAlert = RI(al["gunfire"]); ViolenceSeen = RI(al["violenceSeen"]);
            var fit = b["fit"]; fit.Only("from", "approaches", "standing", "rule", "tag", "src", "proposed", "reason");
            fit["from"].Only("suits", "refused", "otherwise");
            foreach (var kv in fit["approaches"].Entries) { kv.Value.Only("fit", "tag", "src", "proposed", "reason"); Known(approaches, kv.Key, "approach", kv.Value.Path); fitApproach[kv.Key] = ParseFit(kv.Value["fit"]); }
            foreach (var r in fit["standing"].Items)
            {
                r.Only("band", "temperament", "approaches", "fit", "tag", "src", "proposed", "reason");
                var band = OneOf(r["band"], Bands); var t = Known(temps, r["temperament"].Str(), "temperament", r.Path); var f = ParseFit(r["fit"]);
                foreach (var a in r["approaches"].Strs()) { var key = band + "/" + t + "/" + Known(approaches, a, "approach", r.Path); if (fitRows.ContainsKey(key)) throw new FormatException($"fit.standing: {key} twice"); fitRows[key] = f; }
            }
            var mem = b["memory"]; mem.Only("entry", "cap", "whenFull", "faceFacts", "acts", "reads", "rule", "tag", "src");
            MemoryCap = RI(mem["cap"]);
            foreach (var kv in mem["acts"].Entries)
            {
                kv.Value.Only("facts"); MemoryActs.Add(kv.Key);
                var f = kv.Value["facts"]; var af = new List<string>(); ActFacts[kv.Key] = af;
                if (f.Kind == JKind.Str) { if (f.Str() != "as-told") throw new FormatException($"{f.Path}"); } else foreach (var x in f.Strs()) { if (!Facts.Contains(x)) throw new FormatException($"{f.Path}: fact '{x}' is not in people.rules talker.facts"); af.Add(x); }
            }
            foreach (var kv in mem["faceFacts"].Entries) { var fact = kv.Value.Str(); if (!Facts.Contains(fact)) throw new FormatException($"memory.faceFacts.{kv.Key}: fact '{fact}' is unknown"); FaceFact[kv.Key] = fact; }
            mem["reads"].Only("helped", "refused");
            HelpedAct = KnownAct(mem["reads"]["helped"]); RefusedAct = KnownAct(mem["reads"]["refused"]);
            b["exposure"].Only("rule", "tag", "src", "proposed", "reason");
            b["privacy"].Only("rule", "tag", "src", "proposed", "reason");
            foreach (var kv in b["traceable"].Entries)
            {
                kv.Value.Only("standing", "tag", "src", "proposed", "reason");
                Traceable[kv.Key] = OneOf(kv.Value["standing"], new[] { "hero", "menace", "none" }) switch { "hero" => 1, "menace" => -1, _ => 0 };
            }
            var lad = b["ladders"]; lad.Only("rule", "moves", "tag", "src");
            foreach (var kv in lad["moves"].Entries)
            {
                kv.Value.Only("add", "tag", "src", "proposed", "reason");
                var list = new List<(int, int)>();
                foreach (var a in kv.Value["add"].Items) { a.Only("scale", "by"); list.Add((ScaleIndex(a["scale"].Str()), RI(a["by"]))); }
                Ladders[kv.Key] = list.ToArray();
            }
            foreach (var kv in b["debrief"]["lines"].Entries) { kv.Value.Only("fields", "src"); DebriefLines.Add(kv.Key); }
            // brief 014: the Standing line's causes are exactly what the engine emits (unknown = reject, both ways)
            var wantCauses = new SortedSet<string>(StandingFixedCauses, StringComparer.Ordinal);
            foreach (var kv in Traceable) if (kv.Value != 0) wantCauses.Add("seen-" + kv.Key);
            foreach (var e in b["events"].Items) foreach (var o in e["outcomes"].Items) foreach (var ch in o["changes"].Items) // fight.public's outcomes name theirs
                if (ch["do"].Str() == "fight-standing") wantCauses.Add(ch["cause"].Str());
            if (!wantCauses.SetEquals(StandingCauses)) throw new FormatException($"standing.causes are {string.Join(",", new SortedSet<string>(StandingCauses, StringComparer.Ordinal))}; the engine's are {string.Join(",", wantCauses)}");
            if (b["debrief"]["lines"].Opt("standing") is not JNode sl || string.Join(",", sl["fields"].Strs()) != string.Join(",", StandingLineFields)) throw new FormatException("debrief.lines.standing carries " + string.Join(", ", StandingLineFields));
            b["debrief"].Only("lines", "rule", "tag", "src");
            // brief 018 §3: the getaway's lines, with exactly the fields the engine fills (unknown = reject, both ways)
            foreach (var (ln, fs) in new[] { ("left-behind", "who,fate,why"), ("captive", "who,held-at,holds,rescue-until"), ("car-seen", "by,trail") })
                if (b["debrief"]["lines"].Opt(ln) is not JNode dl || string.Join(",", dl["fields"].Strs()) != fs) throw new FormatException($"debrief.lines.{ln} carries {fs.Replace(",", ", ")} (brief 018)");
            WitnessRangeCm = VI("witness-range-m") * 100; WitnessTrail = VI("witness-trail"); ApproachReachCm = VI("approach-reach-m") * 100; ListenReachCm = VI("listen-reach-m") * 100; TrailOpen = VI("trail-open");

            var c = b["cell"]; c.Only("speed", "runSpeed", "runAlert", "reach", "atReach", "loop", "reroute", "mislead", "call", "enter", "take", "carry", "board", "drive", "rule", "tag", "src");
            if (c.Opt("drive") is JNode dv) // brief 025 (EXPERIMENTAL, engine 0.9.0): the driven agent; absent, a drive order is an unknown verb
            {
                dv.Only("experimental", "slack", "burst", "busyPush", "gap", "onBusy", "onRelease", "rule", "tag", "src");
                if (dv["experimental"].Str().Length == 0) throw new FormatException("cell.drive.experimental: say what the playground is (brief 025)");
                var onBusy = dv["onBusy"].Str(); var onRelease = dv["onRelease"].Str();
                if (onBusy != "cancel" && onBusy != "hold") throw new FormatException($"cell.drive.onBusy '{onBusy}' is cancel or hold (unknown = reject)");
                if (onRelease != "stand" && onRelease != "resume") throw new FormatException($"cell.drive.onRelease '{onRelease}' is stand or resume (unknown = reject)");
                Drive = new DriveRules { SlackCm = RI(dv["slack"]), BurstTicks = RI(dv["burst"]), BusyPushCm = RI(dv["busyPush"]), GapCm = RI(dv["gap"]), OnBusy = onBusy, OnRelease = onRelease };
            }
            CellSpeed = RI(c["speed"]); RunSpeed = RI(c["runSpeed"]); RunAlert = RI(c["runAlert"]); ActReachCm = RI(c["reach"]) * 100; AtReachCm = RI(c["atReach"]);
            c["loop"].Only("s", "trail", "tether", "targets"); LoopS = RI(c["loop"]["s"]); LoopTrail = RI(c["loop"]["trail"]); TetherCm = RI(c["loop"]["tether"]) * 100;
            LoopTargets.AddRange(c["loop"]["targets"].Strs());
            c["reroute"].Only("s", "trail", "away"); RerouteS = RI(c["reroute"]["s"]); RerouteTrail = RI(c["reroute"]["trail"]); RerouteAwayS = RI(c["reroute"]["away"]);
            c["mislead"].Only("s", "trail", "away"); MisleadS = RI(c["mislead"]["s"]); MisleadTrail = RI(c["mislead"]["trail"]); MisleadAwayS = RI(c["mislead"]["away"]);
            c["call"].Only("trail"); CallTrail = RI(c["call"]["trail"]);
            c["enter"].Only("s", "alarm"); EnterS = RI(c["enter"]["s"]); EnterAlarmS = RI(c["enter"]["alarm"]);
            c["carry"].Only("s"); CarryS = RI(c["carry"]["s"]);
            c["board"].Only("s"); BoardS = RI(c["board"]["s"]); // brief 018 §2: Board, and stepping out
            if (BoardS < 1) throw new FormatException("cell.board.s is at least 1 s");
            c["take"].Only("s", "trailUnturned", "struggle"); TakeS = RI(c["take"]["s"]); TakeTrailUnturned = RI(c["take"]["trailUnturned"]); TakeStruggleS = RI(c["take"]["struggle"]);
            var se = b["security"]; se.Only("response", "speed", "rivals", "patrol", "rule", "tag", "src"); // checkIn and seal retired by brief 013 (response.checkIn, response.levels)
            var re = se["response"]; re.Only("witness", "seen", "unseen", "civilian", "rivalsUnseen");
            RespWitnessS = RI(re["witness"]); RespSeenS = RI(re["seen"]); RespUnseenS = RI(re["unseen"]); RespCivilianS = RI(re["civilian"]); RespRivalsUnseenS = RI(re["rivalsUnseen"]);
            SecuritySpeed = RI(se["speed"]);
            se["rivals"].Only("speed", "drawn", "retreat"); RivalsSpeed = RI(se["rivals"]["speed"]); RivalsDrawnS = RI(se["rivals"]["drawn"]); RivalsRetreatS = RI(se["rivals"]["retreat"]);
            se["patrol"].Only("breakTo", "period", "chase"); PatrolBreakCm = RI(se["patrol"]["breakTo"]) * 100; PatrolPeriodS = RI(se["patrol"]["period"]); PatrolChase = RI(se["patrol"]["chase"]);
            var wp = b["weapons"]; wp.Only("pistol", "carbine", "pointBlank", "pointBlankCover", "runAwayCone", "acc", "mod", "draw", "holster", "aimNew", "settle", "trigger", "follow", "react", "lineLapse", "sidearmArms", "close", "autoHolster", "health", "costs", "rule", "tag", "src", "proposed", "reason");
            WeaponsSettle = wp.Opt("settle")?.Str() ?? throw new FormatException("weapons.settle is missing: engine 0.16.0 settles a shooter whose run ends (tuning.md §10.2)"); if (WeaponsSettle.Length == 0) throw new FormatException("weapons.settle is empty");
            WeaponsTrigger = wp.Opt("trigger")?.Str() ?? throw new FormatException("weapons.trigger is missing: engine 0.17.0 reads the driven agent's trigger (tuning.md §11)"); if (WeaponsTrigger.Length == 0) throw new FormatException("weapons.trigger is empty");
            FollowCm = RI(wp.Opt("follow") ?? throw new FormatException("weapons.follow: engine 0.17.0 reads the follow ring (tuning.md §11.1, follow-m)")) * 100; if (FollowCm < 100) throw new FormatException("weapons.follow is 1 m or more");
            FireClose = (wp.Opt("close") ?? throw new FormatException("weapons.close: engine 0.12.0 reads it (brief 030)")).Bool();
            AutoHolsterS = RI(wp.Opt("autoHolster") ?? throw new FormatException("weapons.autoHolster: engine 0.12.0 reads it (brief 030)")); if (AutoHolsterS < 0) throw new FormatException("weapons.autoHolster is 0 s or more");
            PointBlankCm = RI(wp["pointBlank"]) * 100;
            Pistol = LoadWeapon(wp["pistol"], "pistol", PointBlankCm); Carbine = LoadWeapon(wp["carbine"], "carbine", PointBlankCm);
            wp["acc"].Only("point", "eff", "long", "start"); AccPoint = RI(wp["acc"]["point"]); AccEff = RI(wp["acc"]["eff"]); AccLong = RI(wp["acc"]["long"]); AccStart = RI(wp["acc"]["start"]);
            PointBlankCover = (wp.Opt("pointBlankCover") ?? throw new FormatException("weapons.pointBlankCover: engine 0.15.0 reads it (tuning.md §8.4)")).Bool();
            RunAwayConeDeg = RI(wp.Opt("runAwayCone") ?? throw new FormatException("weapons.runAwayCone: engine 0.15.0 reads it (tuning.md §8.3)"));
            if (RunAwayConeDeg < 1 || RunAwayConeDeg > 89) throw new FormatException("weapons.runAwayCone is 1..89 degrees");
            RunAwayCosPm = (int)Math.Round(Math.Cos(RunAwayConeDeg * Math.PI / 180) * 1000); // load time only: the rule's integer from here on (60° → 500)
            wp["mod"].Only("moving", "running", "runningAway", "targetRunning", "cover", "vale", "untrained");
            ModRunning = RI(wp["mod"].Opt("running") ?? throw new FormatException("weapons.mod.running: engine 0.15.0 reads it (tuning.md §8.3)")); ModRunningAway = RI(wp["mod"].Opt("runningAway") ?? throw new FormatException("weapons.mod.runningAway: engine 0.15.0 reads it")); ModTargetRunning = RI(wp["mod"].Opt("targetRunning") ?? throw new FormatException("weapons.mod.targetRunning: engine 0.15.0 reads it"));
            ModMoving = RI(wp["mod"]["moving"]); ModCover = RI(wp["mod"]["cover"]); ModVale = RI(wp["mod"]["vale"]); ModUntrained = RI(wp["mod"]["untrained"]);
            DrawTicks = RI(wp["draw"]); HolsterTicks = RI(wp["holster"]); AimNewTicks = RI(wp["aimNew"]); ReactS = RI(wp["react"]); LineLapseS = RI(wp["lineLapse"]); SidearmArms = RI(wp["sidearmArms"]);
            if (SidearmArms < 1) throw new FormatException("weapons.sidearmArms must be at least 1");
            var hp = wp["health"]; hp.Only("enforcer", "op", "guard", "officer", "rival", "patrol", "civilian", "camera", "responseOfficers");
            HealthCamera = RI(hp.Opt("camera") ?? throw new FormatException("weapons.health.camera: engine 0.15.0 reads it (tuning.md §9.2)")); if (HealthCamera < 1) throw new FormatException("weapons.health.camera is at least 1");
            HealthEnforcer = RI(hp["enforcer"]); HealthOp = RI(hp["op"]); HealthGuard = RI(hp["guard"]); HealthOfficer = RI(hp["officer"]); HealthRival = RI(hp["rival"]); HealthPatrol = RI(hp["patrol"]); HealthCivilian = RI(hp["civilian"]); ResponseOfficers = RI(hp["responseOfficers"]);
            foreach (var h in new[] { HealthEnforcer, HealthOp, HealthGuard, HealthOfficer, HealthRival, HealthPatrol, HealthCivilian }) if (h < 1) throw new FormatException("weapons.health: every health is at least 1");
            if (ResponseOfficers < 1) throw new FormatException("weapons.health.responseOfficers must be at least 1");
            var co = wp["costs"]; co.Only("hit", "down", "gunfire", "drawWindow", "drawSeenSecurity", "drawSeenPeople", "loudHold");
            HitTrail = RI(co["hit"]); DownTrail = RI(co["down"]); GunfireTrail = RI(co["gunfire"]); DrawWindowS = RI(co["drawWindow"]); DrawSeenSecurity = RI(co["drawSeenSecurity"]); DrawSeenPeople = RI(co["drawSeenPeople"]); LoudHoldTicks = RI(co["loudHold"]);
            var ai = b["ai"]; ai.Only("cover", "engageLapse", "reacquire", "stopReach", "overflow", "seek", "rivalGone", "guardReturn", "patrolResume", "rule", "tag", "src", "proposed", "reason");
            var aic = ai["cover"]; aic.Only("snap", "qFull", "qPartial", "modPartial", "retreat", "stepCloser", "stepGap", "leave", "stepFires"); CoverStepFires = (aic.Opt("stepFires") ?? throw new FormatException("ai.cover.stepFires: engine 0.15.0 reads it (tuning.md §8.3 fallback)")).Bool();
            CoverSnapCm = RI(aic["snap"]); CoverQFull = RI(aic["qFull"]); CoverQPartial = RI(aic["qPartial"]); ModCoverPartial = RI(aic["modPartial"]); CoverRetreatCm = RI(aic["retreat"]); CoverStepCloserCm = RI(aic["stepCloser"]); CoverStepGapS = RI(aic["stepGap"]); CoverLeaveS = RI(aic["leave"]);
            if (!(CoverQPartial <= CoverQFull)) throw new FormatException("ai.cover: qPartial ≤ qFull");
            EngageLapseS = RI(ai["engageLapse"]); ReacquireS = RI(ai["reacquire"]); StopReachPerMille = RI(ai["stopReach"]); OverflowCm = RI(ai["overflow"]);
            if (StopReachPerMille < 1 || StopReachPerMille > 1000) throw new FormatException("ai.stopReach is per mille of the effective reach, 1..1000");
            var sk = ai["seek"]; sk.Only("rival", "police", "response", "ally", "guard");
            SeekRivalCm = RI(sk["rival"]); SeekPoliceCm = RI(sk["police"]); SeekResponseCm = RI(sk["response"]); SeekAllyCm = RI(sk["ally"]); SeekGuardCm = RI(sk["guard"]);
            RivalGoneCm = RI(ai["rivalGone"]); GuardReturnS = RI(ai["guardReturn"]); PatrolResumeS = RI(ai["patrolResume"]);
            var su = b["summon"]; su.Only("stay", "walkMax", "alertMax", "perShift", "together", "rule", "tag", "src", "proposed", "reason");
            SummonStayS = RI(su["stay"]); SummonWalkMaxS = RI(su["walkMax"]); SummonAlertMax = RI(su["alertMax"]); SummonsPerShift = RI(su["perShift"]); TogetherCm = RI(su["together"]) * 100;
            if (pp.Opt("secrets") is JNode sec)
            {
                sec.Only("rule", "perPerson", "list", "src", "proposed", "reason");
                foreach (var kv in sec["list"].Entries) { kv.Value.Only("requires", "learnedBy", "src", "proposed", "reason"); OneOf(kv.Value["requires"], new[] { "nothing", "lover-tie" }); SecretIds.Add(kv.Key); }
            }
            var cr = b["crowd"]; cr.Only("scatter", "startle", "startleFearful", "run", "fleeMax", "quiet", "occlude", "pace", "rule", "tag", "src", "proposed", "reason");
            CrowdScatterCm = RI(cr["scatter"]) * 100; CrowdStartleTicks = RI(cr["startle"]); CrowdStartleFearfulTicks = RI(cr["startleFearful"]); CrowdRunSpeed = RI(cr["run"]);
            CrowdFleeMaxCm = RI(cr["fleeMax"]); CrowdQuietS = RI(cr["quiet"]); CrowdOccludeCm = RI(cr["occlude"]);
            CrowdPaceSpreadPct = RI(cr["pace"]); if (CrowdPaceSpreadPct < 0 || CrowdPaceSpreadPct > 50) throw new FormatException("crowd.pace: a spread of 0..50 %"); // brief 023 (engine 0.8.2)
            var ca = b["cancel"]; ca.Only("move", "draw", "fire", "watch", "listen", "turn", "loop", "loopHeld", "reroute", "mislead", "enter", "take", "carry", "carrying", "takedown", "body", "rule", "tag", "src", "proposed", "reason");
            OneOf(ca["takedown"], new[] { "released" }); OneOf(ca["body"], new[] { "lost" }); // brief 013
            foreach (var (k, v) in new[] { ("move", "stop"), ("draw", "holster"), ("fire", "cease"), ("watch", "lost"), ("listen", "lost"), ("loop", "lost"), ("loopHeld", "ends"), ("reroute", "lost"), ("mislead", "lost"), ("carry", "lost"), ("carrying", "put-down") })
                OneOf(ca[k], new[] { v });
            CancelTurn = OneOf(ca["turn"], new[] { "aborted", "lost" }); CancelEnter = OneOf(ca["enter"], new[] { "staff-report", "quiet" }); CancelTake = OneOf(ca["take"], new[] { "vale-reports", "quiet" });
            if (!MemoryActs.Contains("aborted") && CancelTurn == "aborted") throw new FormatException("cancel.turn aborted needs memory.acts.aborted");
            var pe = b["perception"]; pe.Only("near", "eye", "door", "facing", "security", "rivals", "patrol", "rule", "tag", "src");
            NearCm = RI(pe["near"]) * 100; Eye = RI(pe["eye"]);
            pe["door"].Only("reach", "half"); DoorReachCm = RI(pe["door"]["reach"]) * 100; DoorHalfCm = RI(pe["door"]["half"]) * 100;
            pe["facing"].Only("reach", "half"); FacingReachCm = RI(pe["facing"]["reach"]) * 100; FacingHalfCm = RI(pe["facing"]["half"]) * 100;
            pe["security"].Only("reach", "half"); SecReachCm = RI(pe["security"]["reach"]) * 100; SecHalfCm = RI(pe["security"]["half"]) * 100;
            pe["rivals"].Only("reach", "half"); RivReachCm = RI(pe["rivals"]["reach"]) * 100; RivHalfCm = RI(pe["rivals"]["half"]) * 100;
            pe["patrol"].Only("depth", "halfBase", "halfPerMille"); PatrolDepthCm = RI(pe["patrol"]["depth"]) * 100; PatrolHalfBase = RI(pe["patrol"]["halfBase"]); PatrolHalfPerMille = RI(pe["patrol"]["halfPerMille"]);
            var si = b["sight"]; si.Only("watch", "kinds", "unknown", "rule", "tag", "src", "proposed", "reason");
            si["watch"].Only("s", "reach", "routine"); SightWatchS = RI(si["watch"]["s"]); SightRoutineS = RI(si["watch"]["routine"]);
            { // brief 031: one reach a operative (a single reach, as runs pinned before it carry, is every operative's)
                var wr = si["watch"]["reach"];
                if (wr.Kind == JKind.Str) { int one = RI(wr) * 100; for (int q = 0; q < 4; q++) WatchReachCms[q] = one; }
                else { wr.Only("scout", "fixer", "tech", "enforcer"); for (int q = 0; q < 4; q++) WatchReachCms[q] = RI(wr[Sim.Op.Name((OpId)q)]) * 100; }
                foreach (var v in WatchReachCms) if (v <= 0) throw new FormatException("sight.watch.reach: every operative's reach is above 0");
            }
            si["kinds"].Only(WatcherKinds);
            foreach (var k in WatcherKinds)
            {
                var kn = si["kinds"][k]; kn.Only("learn", "routine", "by");
                SightKinds[k] = (OneOf(kn["learn"], new[] { "watch", "arrival" }), OneOf(kn["routine"], new[] { "fixed", "person", "watched", "none" }));
                // Sky 2026-10-18: who may Watch a kind (the Tech the machines, the Scout everything watched); unknown = reject
                var kby = kn.Opt("by");
                if ((SightKinds[k].learn == "watch") != (kby != null)) throw new FormatException($"sight.kinds.{k}.by: a kind learned by a Watch names who watches it, and only such a kind");
                var who = new List<string>();
                if (kby != null) foreach (var w in kby.Items) { var n = OneOf(w, new[] { "scout", "fixer", "tech", "enforcer" }); if (who.Contains(n)) throw new FormatException($"sight.kinds.{k}.by: '{n}' twice"); who.Add(n); }
                if (kby != null && !who.Contains("scout")) throw new FormatException($"sight.kinds.{k}.by: the Scout watches every watched kind (cones are the Scout's skill)");
                SightBy[k] = who.ToArray();
            }
            if (si["unknown"].Str() != "?") throw new FormatException("sight.unknown is not '?'");
            { // brief 031 §2: the attention clock (Sky 2026-10-20 D1, D3, L1-L3)
                var at = b.Opt("attention") ?? throw new FormatException("rulebook has no attention section: engine 0.11.0 runs the attention clock (brief 031)");
                at.Only("bands", "sense", "near", "alert", "report", "rule", "tag", "src", "proposed", "reason");
                foreach (var bn in at["bands"].Items)
                {
                    bn.Only("id", "heatFrom", "notice", "wary", "report");
                    var row = (bn["id"].Str(), RI(bn["heatFrom"]), RI(bn["notice"]), RI(bn["wary"]), RI(bn["report"]));
                    if (!(row.Item3 >= 1 && row.Item3 < row.Item4 && row.Item4 < row.Item5)) throw new FormatException($"attention.bands.{row.Item1}: 1 <= notice < wary < report");
                    if (AttnBands.Count == 0 ? row.Item2 != 0 : row.Item2 <= AttnBands[AttnBands.Count - 1].heatFrom) throw new FormatException($"attention.bands.{row.Item1}: heatFrom ascends from 0");
                    foreach (var x in AttnBands) if (x.id == row.Item1) throw new FormatException($"attention.bands: '{row.Item1}' twice");
                    AttnBands.Add(row);
                }
                if (AttnBands.Count == 0) throw new FormatException("attention.bands: at least one band");
                var asn = at["sense"]; asn.Only("camera", "patrol", "robot", "guard", "officer");
                foreach (var m in new[] { "camera", "patrol", "robot" }) OneOf(asn[m], new[] { "cone" }); // L2: a machine sees by its cone only
                AttnGuardNear = OneOf(asn["guard"], new[] { "cone", "near" }) == "near"; AttnOfficerNear = OneOf(asn["officer"], new[] { "cone", "near" }) == "near";
                AttnNearCm = RI(at["near"]) * 100; AttnAlert = RI(at["alert"]);
                at["report"].Only("scan", "call"); AttnScanS = RI(at["report"]["scan"]); AttnCallS = RI(at["report"]["call"]);
                if (AttnScanS < 1 || AttnCallS < 0 || AttnAlert < 1 || AttnAlert > 3) throw new FormatException("attention: scan >= 1 s, call >= 0 s, alert 1..3");
            }
            if (SightKinds["guard"].routine == "person" && SightKinds["guard"].learn != "watch") throw new FormatException("sight.kinds.guard: a person routine is learned by a Watch");
            // ---- brief 013: the takedown, the witnesses, the response dial, the campaign
            var td = b["takedown"]; td.Only("ko", "kill", "struggle", "reach", "trail", "koAlertMax", "foundKillStanding", "wake", "stagger", "restrain", "free", "callOut", "carry", "stash", "service", "rule", "tag", "src", "proposed", "reason");
            KoS = RI(td["ko"]); KillS = RI(td["kill"]); StruggleS = RI(td["struggle"]); TakedownReachCm = RI(td["reach"]); td["trail"].Only("kill"); KillTrail = RI(td["trail"]["kill"]);
            KoAlertMax = RI(td["koAlertMax"]); FoundKillStanding = RI(td["foundKillStanding"]); WakeS = RI(td["wake"]); StaggerS = RI(td["stagger"]); RestrainS = RI(td["restrain"]); FreeS = RI(td["free"]); CallOutS = RI(td["callOut"]);
            td["carry"].Only("untrained", "drop"); CarryUntrainedSpeed = RI(td["carry"]["untrained"]); DropS = RI(td["carry"]["drop"]);
            td["stash"].Only("container", "door", "doorHolds"); StashContainerS = RI(td["stash"]["container"]); StashDoorS = RI(td["stash"]["door"]); StashDoorHolds = RI(td["stash"]["doorHolds"]);
            if (StashDoorHolds < 1) throw new FormatException("takedown.stash.doorHolds is at least 1");
            td["service"].Only("period", "dwell"); ServicePeriodS = RI(td["service"]["period"]); ServiceDwellS = RI(td["service"]["dwell"]);
            if (KoAlertMax < 0 || KoAlertMax > 3) throw new FormatException("takedown.koAlertMax is an Alert, 0..3");
            var wi = b["witness"]; wi.Only("react", "call", "postCall", "robot", "cameraScan", "cap", "scream", "screamFear", "terrifiedFear", "mercenaryAlert", "check", "tell", "alert", "staff", "kin", "close", "tells", "rule", "tag", "src", "proposed", "reason");
            WitnessReactS = RI(wi["react"]); CallS = RI(wi["call"]); PostCallS = RI(wi["postCall"]); RobotReportS = RI(wi["robot"]); CameraScanS = RI(wi["cameraScan"]); ReportCap = RI(wi["cap"]);
            ScreamCm = RI(wi["scream"]) * 100; ScreamFear = RI(wi["screamFear"]); TerrifiedFear = RI(wi["terrifiedFear"]); MercenaryAlert = RI(wi["mercenaryAlert"]); TellCm = RI(wi["tell"]) * 100;
            wi["check"].Only("ticks", "s"); BodyCheckTicks = RI(wi["check"]["ticks"]); BodyCheckS = RI(wi["check"]["s"]);
            if (ReportCap < 1) throw new FormatException("witness.cap is at least 1");
            var wa = wi["alert"]; wa.Only("seen", "carry", "down", "dead", "woken", "noise", "mark", "missing");
            AlertSeen = RI(wa["seen"]); AlertCarry = RI(wa["carry"]); AlertDown = RI(wa["down"]); AlertDead = RI(wa["dead"]); AlertWoken = RI(wa["woken"]); AlertNoise = RI(wa["noise"]); AlertMark = RI(wa["mark"]); AlertMissing = RI(wa["missing"]);
            foreach (var j in wi["staff"].Strs()) StaffJobs.Add(j);
            foreach (var k in wi["kin"].Strs()) { if (!TieKinds.Contains(k)) throw new FormatException($"witness.kin: tie kind '{k}' is unknown"); KinTies.Add(k); }
            foreach (var k in wi["close"].Strs()) { if (!TieKinds.Contains(k)) throw new FormatException($"witness.close: tie kind '{k}' is unknown"); CloseTies.Add(k); }
            foreach (var k in KinTies) if (!CloseTies.Contains(k)) throw new FormatException($"witness.kin: '{k}' is not among witness.close");
            foreach (var k in wi["tells"].Strs()) { if (!TieKinds.Contains(k)) throw new FormatException($"witness.tells: tie kind '{k}' is unknown"); TellTies.Add(k); }
            var rs = b["response"]; rs.Only("levels", "robotSpeed", "robotWire", "robotReach", "world", "launchGap", "check", "search", "standDown", "spread", "cap", "checkIn", "suspicion", "rule", "tag", "src", "proposed", "reason");
            var lvs = rs["levels"].Items; if (lvs.Count != 5) throw new FormatException("response.levels: five security levels, 0..4");
            for (int i = 0; i < 5; i++)
            {
                var l = lvs[i]; l.Only("name", "dispatch", "a1Robots", "a2Units", "a2Robots", "reinforce", "unitCap", "seal", "a3Units", "doorsLock");
                Dial[i] = new DialLevel { Level = i, Name = l["name"].Str(), DispatchS = RI(l["dispatch"]), A1Robots = RI(l["a1Robots"]), A2Units = RI(l["a2Units"]), A2Robots = RI(l["a2Robots"]),
                    ReinforceS = l.Opt("reinforce") is JNode rf ? RI(rf) : 0, UnitCap = RI(l["unitCap"]), SealS = RI(l["seal"]), A3Units = RI(l["a3Units"]), DoorsLock = l["doorsLock"].Bool() };
                if (Dial[i].A3Units < 0) throw new FormatException($"response.levels[{i}].a3Units"); if (Dial[i].A2Units < 1) throw new FormatException($"response.levels[{i}].a2Units: an Alert 2 alarm sends at least the response unit");
            }
            RobotSpeed = RI(rs["robotSpeed"]); RobotWireS = RI(rs["robotWire"]); RobotReachCm = RI(rs["robotReach"]) * 100;
            rs["world"].Only("report", "dock"); /* the world's rules (R-REPORT, R-DOCK): the engine reads the places they made */ ReportReachCm = RI(rs["world"]["report"]) * 100; DockReachCm = RI(rs["world"]["dock"]) * 100;
            LaunchGapTicks = RI(rs["launchGap"]); CheckS = RI(rs["check"]);
            rs["search"].Only("s", "m", "stash"); SearchS = RI(rs["search"]["s"]); SearchStashCm = RI(rs["search"]["m"]) * 100; SearchStashS = RI(rs["search"]["stash"]);
            rs["standDown"].Only("inquiry", "search"); StandDownInquiryS = RI(rs["standDown"]["inquiry"]); StandDownSearchS = RI(rs["standDown"]["search"]);
            AlarmSpread = RI(rs["spread"]); CombatantCap = RI(rs["cap"]); CheckInPeriodS = RI(rs["checkIn"]);
            var su2 = rs["suspicion"]; su2.Only("s", "meet", "pm"); SuspicionS = RI(su2["s"]); MeetCm = RI(su2["meet"]) * 100;
            su2["pm"].Only("fearful", "loyal", "idealist", "mercenary"); foreach (var kv in su2["pm"].Entries) SuspicionPm[kv.Key] = RI(kv.Value);
            var cpn = b["campaign"]; cpn.Only("missing", "areaWatch", "cases", "custody", "rule", "tag", "src", "proposed", "reason");
            var cus = cpn["custody"]; cus.Only("trail", "fear", "loyalty", "abandonedResentment", "abandonedLoyalty", "missions", "cellStart", "rule", "src"); // brief 018 §3
            CapturedTrail = RI(cus["trail"]); CustodyFear = RI(cus["fear"]); CustodyLoyalty = RI(cus["loyalty"]); AbandonedResentment = RI(cus["abandonedResentment"]); AbandonedLoyalty = RI(cus["abandonedLoyalty"]); CustodyMissions = RI(cus["missions"]);
            if (CapturedTrail < 0 || CustodyFear < 0 || CustodyLoyalty < 0 || AbandonedResentment < 0 || AbandonedLoyalty < 0 || CustodyMissions < 1) throw new FormatException("campaign.custody: the costs are not negative and the rescue window is at least one mission");
            cus["cellStart"].Only(ScaleNames);
            for (int i = 0; i < 4; i++) { CellStart[i] = RI(cus["cellStart"][ScaleNames[i]]); if (CellStart[i] < ScaleMin[i] || CellStart[i] > ScaleMax[i]) throw new FormatException($"campaign.custody.cellStart.{ScaleNames[i]} is off its scale"); }
            cpn["missing"].Only("trail", "tiesFear"); MissingTrail = RI(cpn["missing"]["trail"]); MissingTiesFear = RI(cpn["missing"]["tiesFear"]);
            cpn["areaWatch"].Only("step", "missions"); AreaWatchStep = RI(cpn["areaWatch"]["step"]); AreaWatchMissions = RI(cpn["areaWatch"]["missions"]);
            cpn["cases"].Only("perLevel", "cap", "close", "trailEq"); CaseTrailEq = RI(cpn["cases"]["trailEq"]); CasesPerLevel = RI(cpn["cases"]["perLevel"]); CaseLevelCap = RI(cpn["cases"]["cap"]); CaseCloseMissions = RI(cpn["cases"]["close"]);
            if (CasesPerLevel < 1) throw new FormatException("campaign.cases.perLevel is at least 1");
            foreach (var m2 in new[] { "saw-takedown", "attacked", "attacked-aware", "saw-carry", "found-body", "heard-alarm", "reported", "missed-expected" })
                if (!MemoryActs.Contains(m2)) throw new FormatException($"memory.acts.{m2}: brief 013's act is missing");
            if (!Traceable.ContainsKey("takedown")) throw new FormatException("traceable.takedown is missing");
            // brief 023 (engine 0.8.2): where a car may be. The rule has no numbers; the engine routes every moving car on the site's drive grid
            // only when the rulebook states it (unknown = reject: a rulebook without car.drive is one the engine was not built for)
            var cd = b.Opt("car") ?? throw new FormatException("rulebook has no car section: engine 0.8.2 routes cars by car.drive (brief 023)");
            cd.Only("drive", "pace", "paceRule", "target", "tag", "src", "proposed", "reason"); CarDrive = cd["drive"].Str(); if (CarDrive.Length == 0) throw new FormatException("car.drive is empty");
            // car.pace (Motive 2026-10-19, brief 023): a moving car drives each cell at its surface's pace; required from engine 0.8.2
            var cp = cd.Opt("pace") ?? throw new FormatException("rulebook car.pace is missing: engine 0.8.2 drives each cell at its surface's pace (brief 023)");
            cp.Only("road", "yard"); CarRoadSpeed = RI(cp["road"]); CarYardSpeed = RI(cp["yard"]);
            if ((cd.Opt("paceRule")?.Str() ?? "").Length == 0) throw new FormatException("rulebook car.paceRule is missing (brief 023)");
            { // tuning.md §10.7 (engine 0.16.0): cars are targets; required (unknown = reject: a rulebook without it is one this engine was not built for)
                var ct = cd.Opt("target") ?? throw new FormatException("rulebook car.target is missing: engine 0.16.0 makes cars targets (tuning.md §10.7)");
                ct.Only("health", "disabledAt", "burn", "blast", "rule", "src"); ct["rule"].Str(); ct["src"].Str();
                var bl = ct["blast"]; bl.Only("m", "damage");
                HealthCar = RI(ct["health"]); CarDisabledAt = RI(ct["disabledAt"]); CarBurnS = RI(ct["burn"]); CarBlastCm = RI(bl["m"]) * 100; CarBlastDamage = RI(bl["damage"]);
                if (HealthCar < 2 || CarDisabledAt < 1 || CarDisabledAt >= HealthCar) throw new FormatException("car.target: health at least 2, disabledAt between 1 and health − 1 (disabled before it burns)");
                if (CarBurnS < 1 || CarBlastCm < 100 || CarBlastDamage < 1) throw new FormatException("car.target: burn at least 1 s, blast.m at least 1 m, blast.damage at least 1");
            }
            var rd = b["rounds"]; rd.Only("linger", "close", "clear", "part", "holds", "ends", "rule", "tag", "src", "proposed", "reason"); // brief 022
            RoundsLingerS = RI(rd["linger"]); PartnersCloseCm = RI(rd["close"]); PartnersClearCm = RI(rd["clear"]); PartnersPartS = RI(rd["part"]);
            foreach (var v in rd["holds"].Strs()) { if (v != "listen" && !approaches.ContainsKey(v)) throw new FormatException($"rounds.holds: '{v}' is neither listen nor an approach (unknown = reject)"); RoundHolds.Add(v); }
            foreach (var v in rd["ends"].Strs()) { if (Array.IndexOf(new[] { "alert-2", "gunfire", "body-found", "final-fight" }, v) < 0) throw new FormatException($"rounds.ends: '{v}' is unknown"); RoundEnds.Add(v); }
            if (PartnersCloseCm < 1 || PartnersCloseCm >= TogetherCm) throw new FormatException("rounds.close is inside summon.together (a Watch that ends with them standing together still teaches the affair)");
            if (PartnersClearCm < 1) throw new FormatException("rounds.clear is a distance of 1 cm or more (the summoned partner's room round his stop)");
            var fg = b["fight"]; fg.Only("safe", "loud", "hold", "block", "blockNear", "blockFar", "blockers", "traced", "rivalsPublic", "rivalsPublicReason", "tracedReason", "over", "overReason", "lab", "kite", "rule", "tag", "src");
            { // Sky 2026-10-20 (tuning.md §7.9): the kite response, today's (the lab reads its own fight.lab.kite)
                var kt = fg.Opt("kite") ?? throw new FormatException("rulebook fight.kite is missing: engine 0.13.0 runs the kite response in today (tuning.md §7.9)");
                kt.Only("on", "react", "replan", "chase", "lapse", "hide", "contact", "rearm", "hideMax", "coverHold", "rule", "src"); kt["rule"].Str(); kt["src"].Str();
                KiteCoverHold = RI(kt["coverHold"]); if (KiteCoverHold < 1) throw new FormatException("fight.kite.coverHold is at least 1 tick"); // tuning.md §7.10
                KiteOn = kt["on"].Bool(); KiteReact = RI(kt["react"]); KiteReplan = RI(kt["replan"]); KiteChaseCm = RI(kt["chase"]); KiteLapse = RI(kt["lapse"]); KiteHide = RI(kt["hide"]);
                KiteContactCm = RI(kt["contact"]); KiteRearm = RI(kt["rearm"]); KiteHideMax = RI(kt["hideMax"]);
                if (KiteReact < 1 || KiteReplan < 1 || KiteLapse < 1 || KiteHide < 1 || KiteContactCm < 0 || KiteRearm < 1 || KiteHideMax < 1) throw new FormatException("fight.kite: react, replan, lapse, hide, rearm, hideMax at least 1 tick; contact at least 0 cm");
            }
            if (fg.Opt("lab") is JNode flab) Lab = LoadLab(flab); // brief 027 (EXPERIMENTAL, engine 0.10.0): the fight playground; absent, a start that turns it on is refused
            // fight.over (Motive 2026-10-19, brief 023): the engine ends fire orders on final-fight hostiles at the fight's end and refuses new ones (fight-over); the words are required
            if (fg.Opt("over") is not JNode fover || fover.Str().Length == 0) throw new FormatException("rulebook fight.over is missing: engine 0.8.2 ends the fire orders on the final fight's hostiles when it is over (brief 023)");
            BlockCm = RI(fg["block"]) * 100; Blockers = RI(fg["blockers"]); // brief 018 §1
            if (BlockCm < 100 || Blockers < 0) throw new FormatException("fight.block is at least 1 m and fight.blockers is not negative");
            BlockNearCm = RI(fg["blockNear"]) * 100; BlockFarCm = RI(fg["blockFar"]) * 100; // brief 018 §10 (Pulse): the blocker's post ring
            RivalsPublicDefault = OneOf(fg["rivalsPublic"], RivalsPublics); fg["rivalsPublicReason"].Str();
            fg["safe"].Only("hostiles", "minus", "warn", "ambush"); SafeHostiles = RI(fg["safe"]["hostiles"]); SafeMinus = RI(fg["safe"]["minus"]); SafeWarnS = RI(fg["safe"]["warn"]); SafeAmbushCm = RI(fg["safe"]["ambush"]) * 100;
            fg["loud"].Only("officers", "alert", "civilian"); LoudOfficers = RI(fg["loud"]["officers"]); LoudAlert = RI(fg["loud"]["alert"]); LoudCivilian = RI(fg["loud"]["civilian"]);
            HoldCm = RI(fg["hold"]) * 100; FightTraced = fg["traced"].Bool();
            if (!(BlockNearCm > HoldCm && BlockFarCm >= BlockNearCm && BlockFarCm < Pistol.EffCm))
                throw new FormatException("fight.blockNear lies beyond fight.hold, fight.blockFar at or beyond blockNear and inside the pistol's effective reach (brief 018 §10)");
            var inv = b["investigation"]; inv.Only("base", "perAlert", "hops", "rule", "tag", "src", "proposed", "reason");
            InvestigatorsBase = RI(inv["base"]); InvestigatorsPerAlert = RI(inv["perAlert"]); InvestigatorHops = RI(inv["hops"]);
            WordHops = VI("word-hops"); WordQuietLoyalty = VI("word-quiet-loyalty"); IntroductionsPerContact = VI("introductions-per-contact"); NeedWeightScale = VI("need-weight-scale");
            foreach (var kv in b["streams"]["keys"].Entries) StreamKeys[kv.Key] = kv.Value.Str();

            // ---- events: parsed, every id checked, every kind where it may stand
            foreach (var e in b["events"].Items) { var ev = new Event(e); if (events.ContainsKey(ev.Id)) throw new FormatException($"event {ev.Id} repeats"); events[ev.Id] = ev; Events.Add(ev); }
            foreach (var r in b["reports"].Items)
            {
                r.Only("id", "when", "tag", "src", "proposed", "reason");
                var list = new List<Cond>(); foreach (var x in r["when"].Items) list.Add(new Cond(x));
                foreach (var x in list) { ValidateCond(x, null, "reports"); if (x.K == "cash-covers" || x.K == "orders-reached" || x.K == "reports" || x.K == "outcome") throw new FormatException($"{x.N.Path}: '{x.K}' may not stand in the reports table"); if (!MissionKinds.Contains(x.K)) throw new FormatException($"{x.N.Path}: '{x.K}' is not evaluated in a mission"); }
                Reports.Add(list);
            }
            foreach (var ev in Events) ValidateEvent(ev);
            { // Sky 2026-10-16 (briefs/014/motive-standing.md, a public fight): the engine reads one fight's ends, its reach and its outcomes from fight.public
                var fp = Ev("fight.public"); Cond? quiet = null, pub = null;
                foreach (var fc in fp.Needs) { if (fc.K == "fight") quiet = fc; else if (fc.K == "public") pub = fc; else throw new FormatException($"fight.public: a fight's needs are fight and public, not '{fc.K}'"); }
                if (quiet == null || pub == null || fp.Needs.Count != 2) throw new FormatException("fight.public needs fight (quiet) and public (m)");
                FightQuietTicks = checked((int)Num(quiet.S("quiet")) * Game.Hz); FightPublicCm = checked((int)Num(pub.S("m")) * 100);
                foreach (var o in fp.Outcomes)
                {
                    var fo = new FightOutcome { Id = o.Id };
                    foreach (var oc in o.When) { if (Array.IndexOf(FightKinds, oc.K) < 0) throw new FormatException($"fight.public.{o.Id}: '{oc.K}' is not a fight's condition"); fo.When.Add(oc); }
                    foreach (var e in o.Changes)
                    {
                        if (e.Do != "fight-standing" || fo.Cause.Length > 0) throw new FormatException($"fight.public.{o.Id}: an outcome changes at most one fight-standing");
                        fo.Delta = (e.S("toward") == "hero" ? 1 : -1) * checked((int)Num(e.S("step"))); fo.Cause = e.S("cause");
                    }
                    FightOutcomes.Add(fo);
                }
            }
            var pv = b["preview"]; pv.Only("rule", "unknown", "knownBy", "knownByRule", "tag", "src");
            KnownBy = pv["knownBy"].Strs(); foreach (var k in KnownBy) Ev(k);
            var ph = b["phases"]; ph.Only("mission-start", "between-missions", "iterate", "tag", "src");
            foreach (var k in ph["mission-start"].Strs()) Ev(k); foreach (var k in ph["between-missions"].Strs()) Ev(k);
            { // brief 014: the engine runs the word after word.passes and the drift last, so the drift sees every move
                var bm = ph["between-missions"].Strs(); int wpi = bm.IndexOf("word.passes"), sw = bm.IndexOf("standing.word"), sd = bm.IndexOf("standing.drift");
                if (wpi < 0 || sw != wpi + 1 || sd != bm.Count - 1) throw new FormatException("phases.between-missions: standing.word follows word.passes, standing.drift is last");
            }
            var stm = b["streams"]; stm.Only("hash", "generator", "draw", "keys", "weightScale", "weights", "rule", "tag", "src");
        }

        static int Bounded(JNode n, long lo, long hi) { long v = n.Long(); if (v < lo || v > hi) throw new FormatException($"{n.Path} = {v} is outside {lo}..{hi}"); return (int)v; }
        static string Known<T>(Dictionary<string, T> d, string id, string what, string where) => d.ContainsKey(id) ? id : throw new FormatException($"{where}: {what} '{id}' is unknown (unknown = reject)");
        string KnownAct(JNode n) { var a = n.Str(); if (!MemoryActs.Contains(a)) throw new FormatException($"{n.Path}: memory act '{a}' is unknown"); return a; }
        static string OneOf(JNode n, string[] allowed) { var v = n.Str(); if (Array.IndexOf(allowed, v) < 0) throw new FormatException($"{n.Path} = '{v}' is not one of {string.Join(", ", allowed)}"); return v; }
        static Fit ParseFit(JNode n) => OneOf(n, Fits) switch { "suits" => Fit.Suits, "worse" => Fit.Worse, _ => Fit.Refused };
        static int[] Scales(List<string> names) { var o = new int[names.Count]; for (int i = 0; i < o.Length; i++) o[i] = ScaleIndex(names[i]); return o; }
        Weapon LoadWeapon(JNode n, string id, int pointBlankCm)
        {
            n.Only("damage", "every", "eff", "max");
            var w = new Weapon(id, RI(n["damage"]), RI(n["every"]), RI(n["eff"]) * 100, RI(n["max"]) * 100);
            if (w.Damage < 1) throw new FormatException($"{n.Path}.damage must be at least 1");
            if (!(pointBlankCm <= w.EffCm && w.EffCm <= w.MaxCm)) throw new FormatException($"{n.Path}: point blank ≤ eff ≤ max");
            return w;
        }
        public Weapon WeaponOf(string id) => id == "pistol" ? Pistol : id == "carbine" ? Carbine : throw new FormatException($"weapon '{id}' is unknown (pistol, carbine)");

        LabRules LoadLab(JNode n)
        {
            n.Only("experimental", "on", "healthScale", "weapons", "pips", "chance", "runPips", "pip0", "draw", "aimNew", "accStart", "stances", "then", "close", "charge", "field", "target", "policeWarning", "hostilesFireMoving", "kite", "policeContest", "coverSnap", "loadout", "rule", "tag", "src", "proposed", "reason");
            if (n["experimental"].Str().Length == 0) throw new FormatException("fight.lab.experimental: say what the playground is (brief 027)");
            var l = new LabRules { On = n["on"].Bool(), HealthScale = RI(n["healthScale"]), Pips = n["pips"].Bool(), DrawTicks = RI(n["draw"]), AimNewTicks = RI(n["aimNew"]), AccStart = RI(n["accStart"]),
                Stances = OneOf(n["stances"], new[] { "two", "three" }), Then = OneOf(n["then"], new[] { "free", "stop" }), Close = n["close"].Bool(), FireMoving = n["hostilesFireMoving"].Bool() };
            if (l.HealthScale < 1 || l.HealthScale > 100) throw new FormatException("fight.lab.healthScale is 1..100");
            var ws = n["weapons"]; ws.Only(LabRules.WeaponIds);
            foreach (var id in LabRules.WeaponIds)
            {
                var wn = ws[id]; wn.Only("damage", "every", "eff", "max", "arms", "target", "charge", "near", "nearDamage", "noFireMoving");
                var w = new Weapon(id, RI(wn["damage"]), RI(wn["every"]), RI(wn["eff"]) * 100, RI(wn["max"]) * 100)
                {
                    Arms = RI(wn["arms"]), TargetCharge = RI(wn["target"]), Charge = wn.Opt("charge") is JNode wc ? RI(wc) : 0,
                    NearCm = wn.Opt("near") is JNode wnr ? RI(wnr) * 100 : 0, NearDamage = wn.Opt("nearDamage") is JNode wnd ? RI(wnd) : 0,
                    NoFireMoving = wn.Opt("noFireMoving") is JNode nf && nf.Bool(),
                };
                if (w.Damage < 1 || w.Every < 1) throw new FormatException($"fight.lab.weapons.{id}: damage and every are at least 1");
                if (!(PointBlankCm <= w.EffCm && w.EffCm <= w.MaxCm)) throw new FormatException($"fight.lab.weapons.{id}: point blank ≤ eff ≤ max");
                if ((w.NearCm > 0) != (w.NearDamage > 0) || w.NearCm > w.EffCm) throw new FormatException($"fight.lab.weapons.{id}: a near band names its damage and lies inside eff");
                l.Weapons[id] = w;
            }
            var ch = n["chance"].Items; if (ch.Count != 4) throw new FormatException("fight.lab.chance is four steps (one pip to four)");
            for (int k = 0; k < 4; k++) { l.Chance[k + 1] = RI(ch[k]); if (l.Chance[k + 1] < 0 || l.Chance[k + 1] > 1000 || (k > 0 && l.Chance[k + 1] < l.Chance[k])) throw new FormatException("fight.lab.chance rises 0..1000"); }
            l.RunPips = RI(n.Opt("runPips") ?? throw new FormatException("fight.lab.runPips: engine 0.15.0 reads it (tuning.md §8.3)")); l.Chance[0] = RI(n.Opt("pip0") ?? throw new FormatException("fight.lab.pip0: engine 0.15.0 reads it (tuning.md §8.3)"));
            if (l.RunPips < 1 || l.RunPips > 3 || l.Chance[0] < 0 || l.Chance[0] > l.Chance[1]) throw new FormatException("fight.lab: runPips 1..3, pip0 0..the one-pip step");
            var cg = n["charge"]; cg.Only("on", "max", "regen", "rest"); l.Charge = cg["on"].Bool(); l.ChargeMax = RI(cg["max"]); l.ChargeRegen = RI(cg["regen"]); l.ChargeRestTicks = RI(cg["rest"]); // rest: tuning.md §6.3
            var fd = n["field"]; fd.Only("on", "cost", "absorb", "ticks"); l.Field = fd["on"].Bool(); l.FieldCost = RI(fd["cost"]); l.FieldAbsorb = RI(fd["absorb"]); l.FieldTicks = RI(fd["ticks"]);
            var tg = n["target"]; tg.Only("maxShots", "volleysPerFight"); l.MaxShots = RI(tg["maxShots"]); l.VolleysPerFight = RI(tg["volleysPerFight"]);
            var pw = n["policeWarning"]; pw.Only("on", "ticks", "alert", "perWatcher", "trail"); l.Warn = pw["on"].Bool(); l.WarnTicks = RI(pw["ticks"]); l.WarnAlert = RI(pw["alert"]); l.WarnPerWatcher = RI(pw["perWatcher"]); l.WarnTrail = RI(pw["trail"]); // perWatcher, trail: tuning.md §6.4
            if (l.AccStart < 0 || l.AccStart >= 1000 || l.ChargeRestTicks < 1 || l.WarnPerWatcher < 0) throw new FormatException("fight.lab: accStart is 0..999, charge.rest at least 1 tick, policeWarning.perWatcher at least 0");
            var kt = n["kite"]; kt.Only("on", "react", "replan", "chase", "lapse", "hide", "contact", "rearm", "hideMax", "coverHold");
            l.KiteCoverHold = RI(kt.Opt("coverHold") ?? throw new FormatException("fight.lab.kite.coverHold: engine 0.14.0 reads it (tuning.md §7.10)")); if (l.KiteCoverHold < 1) throw new FormatException("fight.lab.kite.coverHold is at least 1 tick"); l.Kite = kt["on"].Bool(); l.KiteReact = RI(kt["react"]); l.KiteReplan = RI(kt["replan"]); l.KiteChaseCm = RI(kt["chase"]); l.KiteLapse = RI(kt["lapse"]); l.KiteHide = RI(kt["hide"]); // F2, Pulse kite-response.md
            if (l.KiteReact < 1 || l.KiteReplan < 1 || l.KiteLapse < 1 || l.KiteHide < 1) throw new FormatException("fight.lab.kite: react, replan, lapse and hide are at least 1 tick");
            l.KiteContactCm = RI(kt["contact"]); l.KiteRearm = RI(kt["rearm"]); l.KiteHideMax = RI(kt["hideMax"]); l.PoliceContest = RI(n["policeContest"]); // brief 027 retest (Pulse retest-fixes.md §1, §2, §5)
            if (l.KiteContactCm < 0 || l.KiteRearm < 1 || l.KiteHideMax < 1 || l.PoliceContest < 0) throw new FormatException("fight.lab: kite.contact at least 0 cm, kite.rearm and kite.hideMax at least 1 tick, policeContest at least 0");
            var cs = n["coverSnap"]; cs.Only("on", "snap", "marks"); l.CoverSnap = cs["on"].Bool(); l.CoverSnapCm = RI(cs["snap"]); l.CoverMarksCm = RI(cs["marks"]) * 100; // brief 027 B5
            if (l.CoverSnapCm < 1 || l.CoverMarksCm < l.CoverSnapCm) throw new FormatException("fight.lab.coverSnap: snap at least 1 cm, marks at least snap");
            var lo = n["loadout"]; lo.Only("scout", "fixer", "tech", "enforcer");
            for (int k = 0; k < 4; k++) { var a = lo[Op.Name((OpId)k)].Str(); if (a != "none" && !l.Weapons.ContainsKey(a)) throw new FormatException($"fight.lab.loadout: weapon '{a}' is unknown (unknown = reject)"); l.Loadout[k] = a; }
            if (l.ChargeMax < 1 || l.MaxShots < 1 || l.FieldTicks < 1 || l.WarnTicks < 1 || l.DrawTicks < 1 || l.AimNewTicks < 1) throw new FormatException("fight.lab: charge.max, target.maxShots, field.ticks, policeWarning.ticks, draw and aimNew are at least 1");
            return l;
        }

        /// <summary>Brief 027 (EXPERIMENTAL, fight.lab pips): a shot's pips, 0 beyond the weapon's max ring, else the band (4 within point blank, 3
        /// within eff, 2 within max), −1 untrained and −1 cover beyond point blank, −1 moving, +1 high, clamped 1–4. rulebook.ref.ts labPips().
        /// Tuning.md §8.3 (engine 0.15.0): the shooter's gait: a walk −1 (floor 1), a run toward or across −runPips (floor 0), a run away 0
        /// pips whatever the band; 0 pips inside the max ring is the pip0 rung (only a runner reaches it). Beyond max: −1 (no shot).</summary>
        public int LabPips(Weapon w, long distanceCm, Gait gait, bool cover, bool untrained, bool high, int runPips)
        {
            if (distanceCm > w.MaxCm) return -1;
            if (gait == Gait.RunAway) return 0;
            int p = distanceCm <= PointBlankCm ? 4 : distanceCm <= w.EffCm ? 3 : 2;
            if (distanceCm > PointBlankCm) { if (untrained) p--; if (cover) p--; }
            if (gait == Gait.Walk) p--;
            if (gait == Gait.Run) p -= runPips;
            if (high) p++;
            return Math.Max(gait == Gait.Run ? 0 : 1, Math.Min(4, p));
        }
        /// <summary>Before engine 0.15.0's gaits: moving is a walk; 0 beyond max.</summary>
        public int LabPips(Weapon w, long distanceCm, bool moving, bool cover, bool untrained, bool high) => Math.Max(0, LabPips(w, distanceCm, moving ? Gait.Walk : Gait.Stand, cover, untrained, high, 2));

        /// <summary>A shot's accuracy in per mille (rulebook weapons): the band's (point blank, effective, long; 0 beyond max), times each
        /// modifier that applies, in this order, each rounded down: moving, cover, vale, untrained. rulebook.ref.ts accuracy() is the same.</summary>
        public int Accuracy(Weapon w, long distanceCm, bool moving, bool cover, bool vale, bool untrained) => Accuracy(w, distanceCm, moving ? Gait.Walk : Gait.Stand, cover ? ModCover : 1000, vale, untrained, false);
        /// <summary>As above, with the cover modifier given in per mille (full, partial or 1000 none: Pulse's cover points).</summary>
        public int Accuracy(Weapon w, long distanceCm, bool moving, int coverMod, bool vale, bool untrained) => Accuracy(w, distanceCm, moving ? Gait.Walk : Gait.Stand, coverMod, vale, untrained, false);
        /// <summary>Tuning.md §8.3 (engine 0.15.0): the shooter's gait first (walk mod.moving, a run toward or across mod.running, a run away
        /// mod.runningAway), then cover, or for a running target past point blank mod.targetRunning in its place (one step, never stacked with
        /// cover: a mover has none), then vale, then untrained; each rounded down.</summary>
        public int Accuracy(Weapon w, long distanceCm, Gait gait, int coverMod, bool vale, bool untrained, bool targetRunning)
        {
            if (distanceCm > w.MaxCm) return 0;
            long a = distanceCm <= PointBlankCm ? AccPoint : distanceCm <= w.EffCm ? AccEff : AccLong;
            a = a * GaitMod(gait) / 1000;
            a = a * (targetRunning && distanceCm > PointBlankCm ? ModTargetRunning : coverMod) / 1000;
            if (vale) a = a * ModVale / 1000;
            if (untrained) a = a * ModUntrained / 1000;
            return (int)a;
        }
        public int GaitMod(Gait g) => g == Gait.Walk ? ModMoving : g == Gait.Run ? ModRunning : g == Gait.RunAway ? ModRunningAway : 1000;
        /// <summary>Tuning.md §8.3: a step s runs away from a target t (the vector to it) when it points within runAwayCone of straight away:
        /// dot(s, t) &lt; 0 and dot² · 10⁶ ≥ cos² · |s|² · |t|² (cos per mille), integers only. The cone's edge is away.</summary>
        public bool RunsAway(Pt s, Pt t)
        {
            long dot = (long)s.X * t.X + (long)s.Y * t.Y; if (dot >= 0) return false;
            long s2 = (long)s.X * s.X + (long)s.Y * s.Y, t2 = (long)t.X * t.X + (long)t.Y * t.Y;
            return (decimal)dot * dot * 1000000m >= (decimal)RunAwayCosPm * RunAwayCosPm * s2 * t2;
        }

        int RI(JNode n) { if (n.Kind != JKind.Str) throw new FormatException($"{n.Path} is not a ref"); return (int)Num(n.Str()); }

        void ValidateEvent(Event ev)
        {
            bool interpreted = ev.Id.StartsWith("turn.", StringComparison.Ordinal) || ev.Id.StartsWith("scout.", StringComparison.Ordinal) || ev.Id == "cell.watch"; // brief 031: the Watch is the cell's
            bool choosesOnly = ev.Id == "investigator.question"; // the engine reads its needs and picks its outcome; its effects are the investigation's own
            if (ev.IsTurn) Known(approaches, ev.Approach, "approach", ev.Id);
            var outs = new HashSet<string>(StringComparer.Ordinal); foreach (var o in ev.Outcomes) if (!outs.Add(o.Id)) throw new FormatException($"{ev.Id}: outcome {o.Id} repeats");
            void Check(IEnumerable<Cond> cs, string part)
            {
                foreach (var c in cs)
                {
                    ValidateCond(c, ev, part, outs);
                    if (c.K == "cash-covers" && part != "needs") throw new FormatException($"{c.N.Path}: cash-covers may stand only in an event's needs");
                    if (part == "price" && (c.K == "orders-reached" || c.K == "reports" || c.K == "outcome")) throw new FormatException($"{c.N.Path}: '{c.K}' may not price a line");
                    if (c.K == "outcome" && (part == "needs" || part == "outcomes")) throw new FormatException($"{c.N.Path}: outcome stands only in also and witnesses");
                    if ((c.K == "fit" || c.K == "orders-reached") && !ev.IsTurn) throw new FormatException($"{c.N.Path}: '{c.K}' reads a Turn's approach");
                    if ((interpreted || (choosesOnly && part != "also")) && !MissionKinds.Contains(c.K)) throw new FormatException($"{c.N.Path}: '{c.K}' is not evaluated in a mission event");
                }
            }
            Check(ev.Needs, "needs");
            foreach (var l in ev.Price)
            {
                Check(l.When, "price");
                l.UsdV = l.Usd != null ? Num(l.Usd) : 0; l.SV = l.S != null ? Num(l.S) : 0; l.TrailV = l.Trail != null ? Num(l.Trail) : 0; l.OrdersV = l.Orders != null ? Num(l.Orders) : 0;
            }
            if (ev.Price.Count > 0 && ev.Price[0].Id != "base") throw new FormatException($"{ev.Id}: the first price line is the base");
            foreach (var o in ev.Outcomes) { Check(o.When, "outcomes"); foreach (var e in o.Changes) ValidateEff(e, ev, interpreted); }
            foreach (var a in ev.Also) { Check(a.When, "also"); foreach (var e in a.Changes) ValidateEff(e, ev, interpreted); }
            if (ev.Private && ev.Witnesses.Count > 0) throw new FormatException($"{ev.Id}: a private event has no witnesses (rulebook privacy)");
            foreach (var w in ev.Witnesses) { Check(w.When, "witnesses"); if (!Traceable.ContainsKey(w.Act)) throw new FormatException($"{ev.Id}: witness act '{w.Act}' is not traceable"); }
        }

        void ValidateCond(Cond c, Event? ev, string part, HashSet<string>? outs = null)
        {
            var n = c.N; string w = n.Path;
            void Bool(string k) { if (n.Has(k)) n[k].Bool(); }
            switch (c.K)
            {
                case "tier": OneOf(n["is"], new[] { "simulated", "background" }); break;
                case "turned": Bool("is"); if (n.Has("of")) OneOf(n["of"], new[] { "friend" }); break;
                case "known": OneOf(n["what"], Knows); Bool("is"); break;
                case "leverage": Known(approaches, c.S("approach"), "approach", w); break;
                case "lover-within": Num(c.S("m")); break;
                case "requires": case "need-met-by": Known(approaches, c.S("approach"), "approach", w); break;
                case "need-is": foreach (var x in c.L("in")) Known(Needs, x, "need", w); break;
                case "need-met": case "orders-reached": case "talker": case "lies": case "cell": case "called": case "exposed": case "tie-between-holds": case "fired-first": case "rivals-broken": Bool("is"); break;
                case "reach": case "within": case "public": Num(c.S("m")); if (c.K == "within") OneOf(n["sightline"], new[] { "3d" }); break;
                case "fight": Num(c.S("quiet")); break;
                case "fired-on": if (c.L("in").Count == 0) throw new FormatException($"{w}: fired-on names a foe"); foreach (var x in c.L("in")) OneOfStr(x, Foes, w); break;
                case "rivals-public": if (c.L("in").Count == 0) throw new FormatException($"{w}: rivals-public names one"); foreach (var x in c.L("in")) OneOfStr(x, RivalsPublics, w); break;
                case "memory": if (!MemoryActs.Contains(c.S("act"))) throw new FormatException($"{w}: memory act '{c.S("act")}' is unknown"); Bool("has"); if (n.Has("this")) OneOf(n["this"], new[] { "mission" }); if (n.Has("of")) OneOf(n["of"], new[] { "friend" }); break;
                case "fit": foreach (var x in c.L("is")) if (Array.IndexOf(Fits, x) < 0) throw new FormatException($"{w}: fit '{x}' is unknown"); break;
                case "temperament": foreach (var x in c.L("in")) Known(temps, x, "temperament", w); break;
                case "standing": foreach (var x in c.L("band")) if (Array.IndexOf(Bands, x) < 0) throw new FormatException($"{w}: band '{x}' is unknown"); break;
                case "outcome": foreach (var x in c.L("is")) if (outs == null || !outs.Contains(x)) throw new FormatException($"{w}: outcome '{x}' is not one of {ev?.Id}'s"); break;
                case "holds-fact": Bool("is"); if (n.Has("how")) OneOf(n["how"], new[] { "any", "witnessed" }); break;
                case "scale":
                    ScaleIndex(c.S("scale")); OneOf(n["op"], new[] { "<", "<=", "==", ">=", ">" });
                    var than = n["than"]; if (than.Kind == JKind.Str) Num(than.Str()); else { than.Only("scale"); ScaleIndex(than["scale"].Str()); }
                    break;
                case "trail": Num(c.S("atLeast")); break;
                case "tie": foreach (var x in c.L("kinds")) if (!TieKinds.Contains(x)) throw new FormatException($"{w}: tie kind '{x}' is unknown"); break;
                case "act": foreach (var x in c.L("in")) if (!Traceable.ContainsKey(x)) throw new FormatException($"{w}: act '{x}' is not traceable"); break;
                case "count": Ev(c.S("event")); Num(c.S("below")); break;
                case "done-to": foreach (var x in c.L("in")) if (Array.IndexOf(DoneTo, x) < 0) throw new FormatException($"{w}: done-to '{x}' is unknown"); break;
                case "alive": case "standing-moved": Bool("is"); break;
            }
        }

        void ValidateEff(Eff e, Event ev, bool interpreted)
        {
            var n = e.N; string w = n.Path;
            if (interpreted && !MissionEffects.Contains(e.Do)) throw new FormatException($"{w}: effect '{e.Do}' is not applied in a mission event");
            switch (e.Do)
            {
                case "charge": foreach (var x in e.L("parts")) if (x != "usd" && x != "s" && x != "trail") throw new FormatException($"{w}: charge part '{x}'"); break;
                case "move": if (!Ladders.ContainsKey(e.S("on"))) throw new FormatException($"{w}: move '{e.S("on")}' is unknown"); break;
                case "learn": foreach (var x in e.L("what")) OneOfStr(x, Knows, w); OneOf(n["of"], new[] { "subject", "needs-known", "friend" }); if (interpreted && e.S("of") != "subject") throw new FormatException($"{w}: a mission event learns of its subject"); if (e.L("what").Contains("refused") && !ev.IsTurn) throw new FormatException($"{w}: only a Turn learns a refusal"); break;
                case "forget": foreach (var x in e.L("what")) OneOfStr(x, Knows, w); break;
                case "remember": if (!MemoryActs.Contains(e.S("act"))) throw new FormatException($"{w}: memory act '{e.S("act")}' is unknown"); break;
                case "trail": Num(e.S("add")); OneOf(n["per"], new[] { "incident" }); break;
                case "standing": Num(e.S("step")); OneOf(n["per"], new[] { "incident" }); break;
                case "alert": Num(e.S("atLeast")); OneOf(n["per"], new[] { "incident" }); break;
                case "lead": OneOf(n["from"], new[] { "facts", "lie" }); break;
                case "tell": foreach (var x in e.L("along")) if (!TieKinds.Contains(x)) throw new FormatException($"{w}: tie kind '{x}' is unknown"); Num(e.S("hops")); break;
                case "replace-need": if (Book["streams"]["keys"].Opt(e.S("stream")) == null) throw new FormatException($"{w}: stream '{e.S("stream")}' is unknown"); break;
                case "schedule": OneOf(n["fight"], new[] { "ambush" }); break;
                case "debrief": if (!DebriefLines.Contains(e.S("line"))) throw new FormatException($"{w}: debrief line '{e.S("line")}' is unknown"); break;
                case "word-standing": OneOf(n["toward"], new[] { "hero", "menace" }); Num(e.S("step")); Num(e.S("cap")); break;
                case "drift-standing": Num(e.S("step")); Num(e.S("to")); break;
                case "fight-standing": OneOf(n["toward"], new[] { "hero", "menace" }); Num(e.S("step")); if (!StandingCauses.Contains(e.S("cause"))) throw new FormatException($"{w}: cause '{e.S("cause")}' is not a standing cause"); if (ev.Id != "fight.public") throw new FormatException($"{w}: fight-standing is fight.public's"); break;
            }
        }
        static void OneOfStr(string v, string[] allowed, string where) { if (Array.IndexOf(allowed, v) < 0) throw new FormatException($"{where}: '{v}' is not one of {string.Join(", ", allowed)}"); }

        void ResolveAllRefs(JNode n)
        {
            switch (n.Kind)
            {
                case JKind.Str: var s = n.Str(); if (s.Length > 2 && (s.StartsWith("v:", StringComparison.Ordinal) || s.StartsWith("p:", StringComparison.Ordinal)) && IsRef(s)) Num(s); break;
                case JKind.Arr: foreach (var x in n.Items) ResolveAllRefs(x); break;
                case JKind.Obj: foreach (var kv in n.Entries) ResolveAllRefs(kv.Value); break;
            }
        }
        static bool IsRef(string s)
        {
            for (int i = 2; i < s.Length; i++) { char c = s[i]; if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '.')) return false; }
            return true;
        }

        /// <summary>A value of the rulebook by id ("loop-s").</summary>
        public long V(string id) => values.TryGetValue(id, out var v) ? v : throw new FormatException($"rulebook has no value '{id}'");
        public int VI(string id) => (int)V(id);
        /// <summary>A v: or p: ref to its integer; every ref in the rulebook is resolved at load, so in play this is a lookup.</summary>
        public long Num(string reff)
        {
            if (refs.TryGetValue(reff, out var hit)) return hit;
            long v;
            if (reff.StartsWith("v:", StringComparison.Ordinal)) v = V(reff.Substring(2));
            else if (reff.StartsWith("p:", StringComparison.Ordinal))
            {
                JNode x = People;
                foreach (var part in reff.Substring(2).Split('.')) { var y = x.Opt(part); if (y == null) throw new FormatException($"{reff} does not resolve in people.rules.json"); x = y; }
                v = x.Long();
            }
            else throw new FormatException($"'{reff}' is not a ref");
            if (v < -1_000_000_000L || v > 1_000_000_000L) throw new FormatException($"{reff} = {v} is out of range");
            refs[reff] = v; return v;
        }

        public Event Ev(string id) => events.TryGetValue(id, out var e) ? e : throw new FormatException($"rulebook has no event '{id}'");
        public bool HasEvent(string id) => events.ContainsKey(id);
        public Temperament Temp(string id) => temps.TryGetValue(id, out var t) ? t : throw new FormatException($"temperament '{id}' is unknown (unknown = reject)");
        public bool HasTemperament(string id) => temps.ContainsKey(id);
        public Approach App(string id) => approaches.TryGetValue(id, out var a) ? a : throw new FormatException($"approach '{id}' is unknown (unknown = reject)");
        public bool HasApproach(string id) => approaches.ContainsKey(id);
        public string ApproachRequires(string a) => App(a).Requires;
        public bool NeedMetBy(string need, string approach) => Needs.TryGetValue(need, out var n) && n.MetBy.Contains(approach);
        public void CheckNeed(string need) { if (!Needs.ContainsKey(need)) throw new FormatException($"need '{need}' is unknown (unknown = reject)"); }

        public Band BandOf(int standing) => standing >= HeroAt ? Band.Hero : standing <= MenaceAt ? Band.Menace : Band.Neutral;
        public static string BandId(Band b) => b == Band.Hero ? "hero" : b == Band.Menace ? "menace" : "neutral";

        /// <summary>Sky Q4: a standing row wins, then an approach row, then respondsTo (suits) / refuses (refused) / otherwise worse.</summary>
        public Fit FitOf(string approach, string temperament, Band band)
        {
            if (fitRows.TryGetValue(BandId(band) + "/" + temperament + "/" + approach, out var f)) return f;
            if (fitApproach.TryGetValue(approach, out var a)) return a;
            var t = Temp(temperament);
            if (t.Refuses.Contains(approach)) return Fit.Refused;
            if (t.RespondsTo.Contains(approach)) return Fit.Suits;
            return Fit.Worse;
        }

        /// <summary>§04 Talker test, read from people.rules: sum(left) > sum(right).</summary>
        public bool IsTalker(int[] sc)
        {
            int l = 0, r = 0;
            foreach (var i in TalkerLeft) l += sc[i];
            foreach (var i in TalkerRight) r += sc[i];
            return l > r;
        }
        public static int ScaleIndex(string s) { int i = Array.IndexOf(ScaleNames, s); if (i < 0) throw new FormatException($"scale '{s}' is unknown"); return i; }
        public int Clamp(int scale, int v) => Math.Max(ScaleMin[scale], Math.Min(ScaleMax[scale], v));
    }

    /// <summary>One security level of the response dial (rulebook response.levels; Motive, briefs/013/motive-takedowns.md §5.3).</summary>
    public sealed class DialLevel
    {
        public int Level; public string Name = ""; public int DispatchS, A1Robots, A2Units, A2Robots, ReinforceS, UnitCap, SealS, A3Units; public bool DoorsLock;
    }

    /// <summary>A weapon (rulebook weapons.pistol, weapons.carbine): damage a hit, ticks between shots, effective and max reach in cm.</summary>
    public sealed class Weapon
    {
        public readonly string Id; public readonly int Damage, Every, EffCm, MaxCm;
        /// <summary>Brief 027 (EXPERIMENTAL, fight.lab weapons; 0 or false for today's two): Arms in the loadout, charge drawn a shot, the
        /// charge a targeted shot costs (target mode), a near band and its damage (the riot gun), no fire while walking (the long rifle).</summary>
        public int Arms, Charge, TargetCharge, NearCm, NearDamage; public bool NoFireMoving;
        public Weapon(string id, int damage, int every, int effCm, int maxCm) { Id = id; Damage = damage; Every = every; EffCm = effCm; MaxCm = maxCm; }
        /// <summary>A hit's damage at a distance: the near band's within it (the riot gun), else Damage.</summary>
        public int DamageAt(long distanceCm) => NearCm > 0 && distanceCm <= NearCm ? NearDamage : Damage;
    }

    /// <summary>fight.lab (brief 027, EXPERIMENTAL): the playground's numbers and its switches' defaults (a run's start may set the switches).</summary>
    public sealed class LabRules
    {
        public static readonly string[] WeaponIds = { "sidearm", "carbine", "machine", "riot", "rifle" };
        public bool On, Pips, Close, Charge, Field, Warn, FireMoving, CoverSnap, Kite; public string Stances = "two", Then = "free";
        public int KiteReact, KiteReplan, KiteChaseCm, KiteLapse, KiteHide; // brief 027 F2 (Pulse): the hostiles' answer to fire from beyond their reach
        public int KiteContactCm, KiteRearm, KiteHideMax, PoliceContest, KiteCoverHold; // brief 027 retest (Pulse retest-fixes.md §1, §2, §5)
        public int CoverSnapCm, CoverMarksCm; // brief 027 B5: a move's end snaps to a free cover point within CoverSnapCm; the host's marks within CoverMarksCm
        public int HealthScale, DrawTicks, AimNewTicks, ChargeMax, ChargeRegen, FieldCost, FieldAbsorb, FieldTicks, MaxShots, VolleysPerFight, WarnTicks, WarnAlert;
        public int AccStart, ChargeRestTicks, WarnPerWatcher, WarnTrail; // tuning.md §6.1–6.4: a new target's count, the charge's rest, warnings per watcher, a heeded warning's Trail
        public readonly int[] Chance = new int[5]; // [pips] per mille; [0] = pip0, the running shooter's rung (tuning.md §8.3; only a run reaches it)
        public int RunPips;                       // tuning.md §8.3: pips off a run toward or across (floor 0); a run away is 0 pips
        public readonly Dictionary<string, Weapon> Weapons = new Dictionary<string, Weapon>(StringComparer.Ordinal);
        public readonly string[] Loadout = new string[4]; // by OpId: a weapon id or "none"
    }
}
