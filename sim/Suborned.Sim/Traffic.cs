// Brief 098 step 1 (law L8; Sky 2026-11-07: "our roads are empty, no reason can't have passing traffic"): the traffic's rules, as
// contracts/rules/population.json gives them (kinds.cars and traffic; checked at load, unknown = reject), and the state of one passing car.
// Read only when a start sets cars; every value is proposed (Pulse and Motive tune them). Integers only: speeds in cm/s (= mm a tick at 10 Hz),
// accelerations in cm/s², gaps and widths in cm, times in seconds or tenths.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class TrafficRules
    {
        /// <summary>The cars kind: the slot ids' prefix (a car is car:&lt;prefix&gt;&lt;slot&gt;-&lt;trip&gt;) and the most a start may ask.</summary>
        public readonly string IdPrefix; public readonly int Max;
        public readonly string LanesFile;
        /// <summary>The kinds of vehicle (the kit's at-budget set), their share of trips (per cent), footprints (cm) and whether they give cover.</summary>
        public readonly string[] Models; public readonly int[] Share, LenCm, WCm; public readonly bool[] Cover;
        public readonly int AccelCmS2, BrakeCmS2, HardBrakeCmS2, GapCm, HeadwayDs, LookCm, ClearCm, PeopleGapCm;
        public readonly int OffMinS, OffMaxS, WarmS, StandS, SettleS, PassWaitS, LatCmS, PassCmS, BackCm, AheadS, EntryS, FleePm, LockAlert;
        /// <summary>Brief 098 step 1b (Sky 2026-11-07: "shot cars can get past, others block"): how fast a mission car pushes a wreck in its way
        /// aside (cm/s), and how far at most it pushes one wreck in all (cm).</summary>
        public readonly int ShoveCmS, ShoveMaxCm;
        public readonly bool LockStop;
        public readonly string[] Duck, Flee;
        public readonly string DriverRole, DriverJob, DriverId, DriverName; public readonly string[] DriverAllow;
        public int MaxW { get { int w = 0; foreach (var x in WCm) w = Math.Max(w, x); return w; } }

        public TrafficRules(JNode kind, JNode t, JNode temperamentsBy)
        {
            kind.Only("id", "max", "src", "proposed", "reason"); kind["src"].Str();
            IdPrefix = kind["id"].Str(); Max = kind["max"].Int();
            if (IdPrefix.Length == 0 || IdPrefix.Contains(':')) throw new FormatException($"{kind["id"].Path}: a car's id prefix, no colon (a car is car:<prefix><slot>-<trip>)");
            if (Max < 0 || Max > 1000) throw new FormatException($"{kind["max"].Path}: 0..1000");
            t.Only("lanes", "mix", "size", "cover", "src", "proposed", "reason", "drive", "trips", "stand", "pass", "yield", "shove", "react", "lockdown", "driver"); t["src"].Str();
            LanesFile = t["lanes"].Str();
            var mix = t["mix"]; var size = t["size"]; var models = new List<string>(); var share = new List<int>(); var len = new List<int>(); var w = new List<int>();
            foreach (var kv in mix.Entries)
            {
                int s = kv.Value.Int(); if (s < 0) throw new FormatException($"{kv.Value.Path}: a share is 0 or more");
                var sz = size.Opt(kv.Key) ?? throw new FormatException($"{size.Path}.{kv.Key} is missing: every kind in the mix has a footprint");
                var a = sz.Ints(); if (a.Length != 2 || a[0] < 50 || a[0] > 2000 || a[1] < 30 || a[1] > 400 || a[1] > a[0]) throw new FormatException($"{sz.Path}: [length, width] cm, 50..2000 by 30..400, no wider than long");
                models.Add(kv.Key); share.Add(s); len.Add(a[0]); w.Add(a[1]);
            }
            foreach (var kv in size.Entries) if (mix.Opt(kv.Key) == null) throw new FormatException($"{kv.Value.Path}: a size for a kind not in the mix (unknown = reject)");
            int sum = 0; foreach (var s in share) sum += s; if (models.Count == 0 || sum <= 0) throw new FormatException($"{mix.Path}: one kind with a share at least");
            Models = models.ToArray(); Share = share.ToArray(); LenCm = len.ToArray(); WCm = w.ToArray(); Cover = new bool[Models.Length];
            foreach (var k in t["cover"].Strs()) { int i = Array.IndexOf(Models, k); if (i < 0) throw new FormatException($"{t["cover"].Path}: '{k}' is not a kind of the mix (unknown = reject)"); Cover[i] = true; }
            var d = t["drive"]; d.Only("accelCmS2", "brakeCmS2", "hardBrakeCmS2", "gapCm", "headwayDs", "lookCm", "clearCm", "peopleGapCm", "src", "proposed", "reason");
            AccelCmS2 = d["accelCmS2"].Int(); BrakeCmS2 = d["brakeCmS2"].Int(); HardBrakeCmS2 = d["hardBrakeCmS2"].Int(); GapCm = d["gapCm"].Int(); HeadwayDs = d["headwayDs"].Int();
            LookCm = d["lookCm"].Int(); ClearCm = d["clearCm"].Int(); PeopleGapCm = d["peopleGapCm"].Int();
            if (AccelCmS2 < 10 || BrakeCmS2 < 10 || HardBrakeCmS2 < BrakeCmS2 || GapCm < 50 || HeadwayDs < 0 || LookCm < 500 || LookCm > 10000 || ClearCm < 0 || ClearCm > 200 || PeopleGapCm < 50)
                throw new FormatException("traffic.drive: accel and brake 10 cm/s² or more, hardBrake no less than brake, gap 50 cm or more, look 5..100 m, clear 0..200 cm, peopleGap 50 cm or more");
            var tr = t["trips"]; tr.Only("offS", "warmS", "src", "proposed", "reason"); var off = tr["offS"].Ints();
            if (off.Length != 2 || off[0] < 0 || off[1] < off[0]) throw new FormatException($"{tr["offS"].Path}: a band [min, max] of seconds"); OffMinS = off[0]; OffMaxS = off[1];
            WarmS = tr["warmS"].Int(); if (WarmS < 0 || WarmS > 600) throw new FormatException($"{tr["warmS"].Path}: 0..600 s");
            var st = t["stand"]; st.Only("standS", "settleS", "src", "proposed", "reason"); StandS = st["standS"].Int(); SettleS = st["settleS"].Int();
            if (StandS < 0 || SettleS < StandS) throw new FormatException("traffic.stand: standS 0 or more, settleS no less");
            var ps = t["pass"]; ps.Only("waitS", "latCmS", "passCmS", "backCm", "src", "proposed", "reason"); PassWaitS = ps["waitS"].Int(); LatCmS = ps["latCmS"].Int(); PassCmS = ps["passCmS"].Int(); BackCm = ps["backCm"].Int();
            if (PassWaitS < 0 || LatCmS < 1 || PassCmS < 10 || BackCm < 0 || BackCm > 2000) throw new FormatException("traffic.pass: waitS 0 or more, latCmS and passCmS positive, backCm 0..2000");
            var y = t["yield"]; y.Only("aheadS", "entryS", "src", "proposed", "reason"); AheadS = y["aheadS"].Int(); EntryS = y["entryS"].Int();
            if (AheadS < 0 || AheadS > 30 || EntryS < 0 || EntryS > 60) throw new FormatException("traffic.yield: aheadS 0..30 s, entryS 0..60 s");
            var sh = t["shove"]; sh.Only("cmS", "maxCm", "src", "proposed", "reason"); ShoveCmS = sh["cmS"].Int(); ShoveMaxCm = sh["maxCm"].Int();
            if (ShoveCmS < 10 || ShoveCmS > 1000 || ShoveMaxCm < 50 || ShoveMaxCm > 2000) throw new FormatException("traffic.shove: cmS 10..1000 cm/s, maxCm 50..2000 cm");
            var re = t["react"]; re.Only("duck", "flee", "fleePm", "src", "proposed", "reason"); Duck = re["duck"].Strs().ToArray(); Flee = re["flee"].Strs().ToArray(); FleePm = re["fleePm"].Int();
            if (FleePm < 1000 || FleePm > 4000) throw new FormatException("traffic.react.fleePm: 1000..4000 per mille");
            var lk = t["lockdown"]; lk.Only("alert", "then", "src", "proposed", "reason"); LockAlert = lk["alert"].Int(); var then = lk["then"].Str();
            if (LockAlert < 1 || LockAlert > 3 || (then != "leave" && then != "stop")) throw new FormatException("traffic.lockdown: alert 1..3, then leave or stop"); LockStop = then == "stop";
            var dr = t["driver"]; dr.Only("role", "job", "id", "name", "src", "proposed", "reason");
            DriverRole = dr["role"].Str(); DriverJob = dr["job"].Str(); DriverId = dr["id"].Str(); DriverName = dr["name"].Str();
            if (DriverId.Length == 0 || DriverId.StartsWith("p-", StringComparison.Ordinal)) throw new FormatException($"{dr["id"].Path}: a driver's id prefix is not the people file's (p-)");
            var role = temperamentsBy["roles"].Opt(DriverRole) ?? throw new FormatException($"{dr["role"].Path}: '{DriverRole}' is not a people.rules temperamentsBy role (unknown = reject)");
            DriverAllow = role["allow"].Strs().ToArray();
            foreach (var x in DriverAllow) if ((Array.IndexOf(Duck, x) >= 0) == (Array.IndexOf(Flee, x) >= 0)) throw new FormatException($"traffic.react: '{x}' is a driver's nature: it either ducks or flees, never both nor neither");
        }
        public bool Ducks(string temperament) => Array.IndexOf(Duck, temperament) >= 0;
    }

    /// <summary>What a passing car is doing. Drive: on its way; Brake: alarmed, stopping hard; Duck: stopped, its driver down at the wheel until it
    /// is quiet; Flee: driving off fast; Held: pulled up for good at a lockdown (traffic.lockdown stop); Abandoned: its driver got out.</summary>
    public enum TrafficMode { Drive, Brake, Duck, Flee, Held, Abandoned }

    /// <summary>One passing car on one trip (car:&lt;prefix&gt;&lt;slot&gt;-&lt;trip&gt;): its kind and footprint, its route and where along it its centre is (mm), its
    /// speed (mm a tick), how far right of the route's line it rides (cm), what it is doing, the junction turn it holds, and its driver.</summary>
    public sealed class TrafficCar
    {
        public string Id = "", Model = ""; public int Slot, Trip, Kind, HalfLen, HalfW; public bool Cover;
        public TrafficRoute Route = null!; public long Mm; public int V, Lat, LatGoal;
        public TrafficMode Mode; public int ModeTick, StoppedAt = -1, Quiet = -1, BlockedAt = -1, AskedAt = -1, BornAt;
        /// <summary>The route turn it holds (an index of Route.Turns) or -1; the turn it was refused for something that will not move on, since when.</summary>
        public int Turn = -1, Refused = -1, RefusedAt = -1;
        /// <summary>Backing off: the mm still to go back, and the blockage (BlockedAt) it backed off for (once a blockage).</summary>
        public long Back; public int BackedAt = -2;
        /// <summary>The last tick it looked for a way round and found none: it looks again a second later, not every tick.</summary>
        public int PlannedAt = -100;
        public bool Flees; public Pt Alarm;
        /// <summary>The mission car this car waits for now (its first thing ahead is that car or its next seconds), else "": a mission car never waits
        /// long for a driver who waits for it.</summary>
        public string WaitsFor = "";
        /// <summary>Set once a tick at the end of the traffic's step: standing (stopped traffic.stand.standS, or disabled, burning, abandoned, its driver
        /// ducked or pulled up: ground nobody walks through, a box on a way out, cover) and settled (stopped settleS or for good: the crowd's legs go round it).</summary>
        public bool Standing, Settled;
        public Person Driver = null!; public Walker? Out;
        public bool Gone;
        /// <summary>Brief 098 step 1b: a wreck a mission car has pushed aside stands where it was pushed (its pose off its route from then on, never driven
        /// again): the mission car that pushed it last, when, and how far it has been pushed in all (cm).</summary>
        public bool Moved; public Pt MovedAt, MovedH; public string ShovedBy = ""; public int ShovedAt = -1, ShovedCm, ShoveSide;
        /// <summary>Brief 098 step 1b: the mission car this car stands in the way of while waiting for it (they would wait for each other): it gets
        /// out of the way, sideways within its half or backing off; "" when it is in nobody's way.</summary>
        public string Clears = "";
        public long Front => Mm + HalfLen * 10L; public long Rear => Mm - HalfLen * 10L;
    }
    /// <summary>One slot of the count: a car at a time, its next trip's earliest tick, and how many trips it has run.</summary>
    public sealed class TrafficSlot { public int Index, Next, Trips; public TrafficCar? Car; }
}
