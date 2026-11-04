// The rulebook's getaway rules of brief 096 (Motive's rules page, briefs/096/motive/rules.html; Sky 2026-11-04 "all. recommended"; engine
// 0.40.0, Keel): bribes on camera (`camera`), box-in (`car.box`), the kerbside stop (`car.kerb`) and the parked car noticed (`attention.parked`).
// The attention section's reader moved here from Rules.cs with it (that file is over the 500-line mark). Every key is required: a rulebook
// without them is pinned before 0.40.0 (unknown = reject).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Rules
    {
        /// <summary>camera.approaches: the approaches a live camera records (pay, pressure).</summary>
        public string[] CamApproaches { get; private set; } = Array.Empty<string>();
        /// <summary>camera: the record's Trail (on top of the approach's), the Alert it raises to at least, its desk's scan period and call (s), and whether the lead holds the Fixer's face.</summary>
        public int CamTrail { get; private set; } public int CamAlert { get; private set; } public int CamScanS { get; private set; } public int CamCallS { get; private set; } public bool CamFace { get; private set; }
        /// <summary>car.box.side: the way out's keep-off each side of the car (cm); the way out's half-width is car.footprint.w / 2 plus it.</summary>
        public int BoxSideCm { get; private set; }
        public int BoxHalfCm => CarWCm / 2 + BoxSideCm;
        /// <summary>car.kerb: the kerbside stop's clearance all round (cm) and its footprint's edge off the kerb line (cm).</summary>
        public int KerbClearCm { get; private set; } public int KerbEdgeCm { get; private set; }
        /// <summary>attention.parked: the private ground (drive, official, service) and the height over a car's centre it is seen at (cm).</summary>
        public readonly HashSet<string> ParkedGround = new HashSet<string>(StringComparer.Ordinal);
        public int CarSightCm { get; private set; }

        void ReadAttention(JNode b)
        {
            var at = b.Opt("attention") ?? throw new FormatException("rulebook has no attention section: engine 0.11.0 runs the attention clock (brief 031)");
            at.Only("bands", "sense", "near", "alert", "report", "run", "parked", "rule", "tag", "src", "proposed", "reason");
            { // engine 0.34.0 (brief 067, Motive M067-B2): the run counted on the clock; required (unknown = reject: a rulebook without it is pinned before 0.34.0)
                var ar = at.Opt("run") ?? throw new FormatException("attention.run is missing: engine 0.34.0 counts a run on the watchers' clocks (cell.runAlert is retired; brief 067, Motive M067-B2)");
                ar.Only("at", "counts", "rule", "tag", "src", "proposed", "reason");
                OneOf(ar["at"], new[] { "notice" }); OneOf(ar["counts"], new[] { "still" });
                if (ar["rule"].Str().Length == 0) throw new FormatException("attention.run.rule is empty");
                AttnRun = true;
            }
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
            { // brief 096 (engine 0.40.0): the parked car noticed
                var pk = at.Opt("parked") ?? throw new FormatException("attention.parked is missing: engine 0.40.0 keeps a clock on the cell's car on private ground (brief 096)");
                pk.Only("ground", "sight", "rule", "tag", "src", "proposed", "reason");
                foreach (var gname in pk["ground"].Strs()) { In(gname, pk["ground"].Path, "drive", "official", "service"); if (!ParkedGround.Add(gname)) throw new FormatException($"attention.parked.ground: '{gname}' twice"); }
                if (ParkedGround.Count == 0) throw new FormatException("attention.parked.ground: at least one kind of private ground");
                CarSightCm = RI(pk["sight"]); if (CarSightCm < 50 || CarSightCm > 300) throw new FormatException("attention.parked.sight: 50..300 cm over the car's centre");
                if (pk["rule"].Str().Length == 0) throw new FormatException("attention.parked.rule is empty");
            }
        }

        static void In(string v, string path, params string[] allowed) { if (Array.IndexOf(allowed, v) < 0) throw new FormatException($"{path}: '{v}' is not one of {string.Join(", ", allowed)}"); }

        void ReadCars096(JNode b)
        {
            var cam = b.Opt("camera") ?? throw new FormatException("rulebook has no camera section: engine 0.40.0 records a Pay or a Pressure a live camera watches (brief 096)");
            cam.Only("approaches", "window", "trail", "alert", "report", "standing", "lead", "rule", "tag", "src", "proposed", "reason");
            var ap = new List<string>();
            foreach (var a in cam["approaches"].Strs()) { if (!approaches.ContainsKey(a)) throw new FormatException($"camera.approaches: '{a}' is no approach (people.rules approaches)"); if (ap.Contains(a)) throw new FormatException($"camera.approaches: '{a}' twice"); ap.Add(a); }
            CamApproaches = ap.ToArray();
            if (cam["window"].Str() != "any-second") throw new FormatException("camera.window: 'any-second' is the only window the engine runs");
            CamTrail = RI(cam["trail"]); CamAlert = RI(cam["alert"]);
            cam["report"].Only("scan", "call"); CamScanS = RI(cam["report"]["scan"]); CamCallS = RI(cam["report"]["call"]);
            if (cam["standing"].Int() != 0) throw new FormatException("camera.standing: the engine moves no Standing for a record (0)");
            var lead = new List<string>(cam["lead"].Strs());
            foreach (var l in lead) In(l, cam["lead"].Path, "place-time", "subject", "face:fixer");
            if (!lead.Contains("place-time") || !lead.Contains("subject")) throw new FormatException("camera.lead: a record always leads to its place-time and its subject");
            CamFace = lead.Contains("face:fixer");
            if (CamTrail < 0 || CamAlert < 1 || CamAlert > 3 || CamScanS < 1 || CamCallS < 0) throw new FormatException("camera: trail >= 0, alert 1..3, scan >= 1 s, call >= 0 s");
            if (cam["rule"].Str().Length == 0) throw new FormatException("camera.rule is empty");
            var cd = b["car"];
            var box = cd.Opt("box") ?? throw new FormatException("car.box is missing: engine 0.40.0 boxes a car in (brief 096)");
            box.Only("side", "refuse", "rule", "tag", "src", "proposed", "reason");
            BoxSideCm = RI(box["side"]); if (BoxSideCm < 0) throw new FormatException("car.box.side is not negative");
            box["refuse"].Only("go", "drive", "into");
            if (box["refuse"]["go"].Str() != "car-boxed" || box["refuse"]["drive"].Str() != "drive-boxed" || box["refuse"]["into"].Str() != "stand-boxed") throw new FormatException("car.box.refuse: go car-boxed, drive drive-boxed, into stand-boxed (the engine's codes)");
            var kb = cd.Opt("kerb") ?? throw new FormatException("car.kerb is missing: engine 0.40.0 stops the turned driver at the kerb short of a taken drive (brief 096)");
            kb.Only("clear", "edge", "rule", "tag", "src", "proposed", "reason");
            KerbClearCm = RI(kb["clear"]); KerbEdgeCm = RI(kb["edge"]);
            if (KerbClearCm < 0 || KerbEdgeCm < 0) throw new FormatException("car.kerb: clear and edge are not negative");
        }
    }
}
