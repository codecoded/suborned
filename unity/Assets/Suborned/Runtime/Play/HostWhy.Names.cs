using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 100's fills (Fable's `why.rule`, briefs/100/fable/words.md "The fills"): who reported or witnessed it ({who}: the cast by name, a crowd
    /// person by job, a watcher or unit as `by`; `by.another` when they share a job with {about}), who or what it is about ({about}), the agent ({agent}:
    /// "the Scout"), the place ({where}: the nearest of `places` within 13 m, else "nearby") and the report point ({to}). Every name comes from text.json
    /// (names.targets, why.by, less.done.walk.who); an id the host can't name gives "" and the line that needs it is not shown.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>A reporter, witness or subject in the engine's grammar (FailCause.cs's ids: camera:&lt;id&gt;, guard, patrol, security-&lt;n&gt;, robot-&lt;n&gt;,
        /// police-&lt;n&gt;, round:&lt;stash&gt;, desk, heard, a role, a person's id, an operative, a rival) in the player's words; "" when it names nothing known.</summary>
        string WhyWho(string id)
        {
            if (string.IsNullOrEmpty(id) || Text == null || Game == null) return "";
            if (id.StartsWith("camera:", StringComparison.Ordinal)) { var cid = id[7..]; return Text.TargetNames.TryGetValue("camera-" + cid, out var cn) ? cn : WhyBy("camera"); }
            if (id.StartsWith("round:", StringComparison.Ordinal)) return WhyBy("round");
            if (id == "desk") return WhyBy("desk");
            if (id is "heard" or "witness" or "people") return WhyBy("witness");
            if (IsOpName(id)) return TheAgent(id);
            if (id is "waitress" or "driver" or "guard" or "councillor" or "patrol") return Name(id);
            if (id.StartsWith("robot", StringComparison.Ordinal)) return WhyBy("robot");
            if (id.StartsWith("security", StringComparison.Ordinal) || id.StartsWith("police", StringComparison.Ordinal) || id.StartsWith("officer", StringComparison.Ordinal)) return WhyBy("police");
            if (Game.PersonById(id) is Person p) return p.Role.Length > 0 ? Name(p.Role) : PersonName(p);
            if (HostViews.IsCrewId(id) || id is "rivals" or "car:rivals") return Name(id);
            return "";
        }
        string WhyBy(string k) => Text != null && Text.WhyBy.TryGetValue(k, out var s) ? s : "";
        /// <summary>{agent}: "the" and the agent's job word (names.speakers: "the Scout"), as Fable's fills say it.</summary>
        string TheAgent(string op) { if (!IsOpName(op)) return ""; var j = AgentJob((int)Op.Parse(op)); return j.Length == 0 ? "" : "the " + j; }

        /// <summary>The plate word of a person's job (less.done.walk.who: the cast's own, crowd-&lt;job&gt;), for `by.another`; "" for a watcher or a unit.</summary>
        string WhyJob(string id)
        {
            if (Text == null || Game == null || string.IsNullOrEmpty(id)) return "";
            var p = id is "waitress" or "driver" or "guard" or "councillor" ? Game.PersonByRole(id) : Game.PersonById(id); if (p == null) return "";
            string key = p.Role.Length > 0 ? p.Role : !p.Civilian ? (p.Job == "guard" ? "crowd-guard" : "crowd-officer") : "crowd-" + p.Job;
            return Text.DoneWord("walk.who." + key, "");
        }
        /// <summary>{who} for a report about `about`: `by.another` ("another guard") when both share a job, so a line never reads "a guard spotted the guard's body".</summary>
        string WhoAbout(string who, string about)
        {
            var w = WhyWho(who); string jw = WhyJob(who), ja = WhyJob(about), another = WhyBy("another");
            return jw.Length > 0 && jw == ja && another.Length > 0 && who != about ? PlayText.FillWhy(another, new Dictionary<string, string> { ["job"] = jw }, false) : w;
        }

        List<(Pt at, string words)> whyPlaces; Game whyPlacesOf;
        /// <summary>{where}: the nearest of Fable's places within 13 m (why.places, by the site's place ids: the mission's d: destinations and the site's own), else `nearby`.</summary>
        string WhyWhere(Pt? at)
        {
            if (Text == null || Game == null) return "";
            string near = Text.WhyPlaces.TryGetValue("nearby", out var n) ? n : "";
            if (!at.HasValue) return near;
            if (whyPlaces == null || !ReferenceEquals(whyPlacesOf, Game))
            {
                whyPlaces = new(); whyPlacesOf = Game;
                foreach (var kv in Text.WhyPlaces) if (kv.Key != "nearby" && Game.S.HasPlace(kv.Key)) whyPlaces.Add((Game.S.At(kv.Key), kv.Value));
            }
            string best = near; long bd = 1300L * 1300L;
            foreach (var (p, words) in whyPlaces) { long dx = p.X - at.Value.X, dy = p.Y - at.Value.Y, d = dx * dx + dy * dy; if (d <= bd) { bd = d; best = words; } }
            return best;
        }
        /// <summary>The places that resolve on this site (the host check reads it: Fable's place ids must be the engine's).</summary>
        public int WhyPlacesKnown { get { WhyWhere(default(Pt)); return whyPlaces?.Count ?? 0; } }

        /// <summary>{to}: the report point a walker heads for (onWay.to: a staffed door by id, any call box, else a phone).</summary>
        string WhyTo(string refuge)
        {
            if (Text == null) return "";
            if (!string.IsNullOrEmpty(refuge) && Text.OnWayTo.TryGetValue(refuge, out var t)) return t;
            if (!string.IsNullOrEmpty(refuge) && !refuge.StartsWith("door:", StringComparison.Ordinal) && Text.OnWayTo.TryGetValue("call-box", out var cb)) return cb;
            return Text.OnWayTo.TryGetValue("phone", out var ph) ? ph : "";
        }

        /// <summary>Where a person, watcher or thing stands now (engine frame), by the engine's id; null when off the street.</summary>
        Pt? WhyPos(string id)
        {
            var g = Game; if (g == null || string.IsNullOrEmpty(id)) return null;
            if (id.StartsWith("camera:", StringComparison.Ordinal)) { var c = g.S.Cameras.Find(x => x.Id == id[7..]); return c?.At; }
            if (IsOpName(id)) { var op = g.Ops[(int)Op.Parse(id)]; return op.Inside ? null : op.Pos; }
            if (id == "guard") return g.GuardPose()?.pos ?? (g.PersonByRole("guard") is Person gp ? g.PosOfPerson(gp) : null);
            if (id == "patrol") return g.PatrolPose()?.pos;
            var p = id is "waitress" or "driver" or "councillor" ? g.PersonByRole(id) : g.PersonById(id);
            if (p != null) return g.BodyOf(p) is Body b && p.Down ? g.BodyPos(b) : g.PosOfPerson(p);
            return g.PosOf(id);
        }
        Vector3? WhyWorld(Pt? at) => at.HasValue ? frame.World(at.Value) : null;
        /// <summary>An engine point (cm, block frame) in the world (the HUD's marks).</summary>
        public Vector3 World(Pt p) => frame.World(p);
    }
}
