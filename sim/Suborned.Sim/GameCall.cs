// The Call (rulebook summon; Motive, briefs/011/motive-lovers.md §1.3; brief 102, Sky 2026-11-09), moved here from GameOrders.cs (brief 102,
// Keel: that file is over the 500-line mark). What it holds:
//  · the offer (Motive, briefs/102/motive/turned-guard.md §1.1; Aster took Keel's (b)): what a call is decides whether it is offered. No lover,
//    a retired role job (the driver's camera block) or a visit the engine doesn't play is absent, never refused (CallOffered). What has happened
//    decides whether it is shut, with its reason (CallQuery). Both read CallAbsent, the helper CallRefusal refuses by, so they can't drift.
//  · the one called (step 4, Sky's "yes, they refuse"): nobody acts on a call while they hold a memory of the cell's gunfire, violence or drawn
//    weapon. The one called is read first (call-saw-violence), before the one who'd leave his post (summon-saw-violence).
//  · the visit from either end (option 1, Sky's "leaves when called"): a call empties a post, the contact's own if they keep one, else their
//    lover's. Whoever leaves walks to the other's post, stays and walks back (GamePressedCall.cs walks it). Once a shift and the short visit
//    read either end of the pair.
using System;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Whose call drew the visit under way or last made: the role called ("waitress", or "guard" for his own call), "" before any.
        /// Output only (the debrief's route line, RouteFacts.LuredBy), never hashed: the visit is the same from either end.</summary>
        public string LureCalled { get; private set; } = "";
        string visitWarner = ""; // the role of the pair who remembers a Pressure on a pressed visit (the log's words only, never hashed)

        /// <summary>Whether this person keeps a post a visit leaves from. The engine walks a visit only from the door guard's door (GuardPose).</summary>
        bool KeepsPost(Person p) => p == GuardP;

        /// <summary>Option 1: the pair a call on this contact sends on a visit: the one who leaves (the contact if they keep a post, else their lover)
        /// and the one visited, met at her post (brief 022). Null with no lover, or where neither keeps a post (a visit the engine doesn't play).</summary>
        (Person leaves, Person visited)? VisitPair(Person p)
        {
            var lover = p.Lover; if (lover == null) return null;
            if (KeepsPost(p)) return (p, lover);
            if (KeepsPost(lover)) return (lover, p);
            return null;
        }

        /// <summary>Why a Call on this contact has no use under the rules, or null: the driver's retired role job, no lover, or a visit the engine
        /// doesn't play. Each is true from the first tick and nothing in play changes it, so the Call is absent there, never shut.</summary>
        (string code, string why)? CallAbsent(Person p)
        {
            if (p == Driver) return ("nothing-to-call", "the driver has nothing to do on a call: his camera block is retired (brief 059, Motive M059-6.1)");
            if (p.Lover == null) return ("nothing-to-call", $"{p.Role} has nothing to do on a call");
            if (VisitPair(p) == null) return ("summon-no-visit", $"neither {p.Role} nor {p.Lover.Role} keeps a post to leave (only a visit from a post is modelled)");
            return null;
        }

        static readonly string[] CellViolenceActs = { "saw-gunfire", "saw-violence", "saw-weapon-drawn", "saw-takedown", "attacked-aware" };
        /// <summary>A memory of the cell's gunfire, violence or drawn weapon (rulebook summon): one seen (a shot, violence, a drawn weapon, a takedown)
        /// or a takedown seen coming, by a face of the cell. A told entry (act told) is none, and neither is fear (Motive §1.4).</summary>
        static bool HoldsCellViolence(Person p)
        {
            foreach (var m in p.Memory)
                if (Array.IndexOf(CellViolenceActs, m.Act) >= 0 && (m.By == "scout" || m.By == "fixer" || m.By == "tech" || m.By == "enforcer")) return true;
            return false;
        }

        /// <summary>Brief 102 (Motive §1.1): is a Call on this role on the order list? A turned contact whose call has a use under the rules (CallAbsent
        /// null). What has happened (down, busy, an alarm, a memory of violence, came once, too far) keeps it, shut with its reason (CallQuery). Read-only.</summary>
        public bool CallOffered(string role)
        {
            if (!IsPersonRoleName(role)) return false; // a crowd person or an unknown id is no contact
            var p = PersonByRole(role);
            return p.Turned && CallAbsent(p) == null;
        }

        /// <summary>Whether the Fixer could call this contact now (the action card's Call): the call card's own answer, so they can't drift.</summary>
        public bool CanCall(string role) => CallQuery(role).ok;

        /// <summary>Host read (brief 102, option 1): the role a call on this role would send off his post (the contact's own, else their lover's), ""
        /// where the Call has no visit to play. Glyph's "?" for someone who'd leave his own post reads it. Read-only.</summary>
        public string CallLeaver(string role)
        {
            if (!IsPersonRoleName(role)) return "";
            var p = PersonByRole(role);
            return CallAbsent(p) == null && VisitPair(p) is { } v ? v.leaves.Role : "";
        }
        /// <summary>Host read (brief 102, option 1): would a call on this role now be the short visit (either of the pair remembers a Pressure)?
        /// The Call card's door clock reads it, never the one called's memory alone. Read-only.</summary>
        public bool CallWouldBePressed(string role)
        {
            if (!IsPersonRoleName(role)) return false;
            var p = PersonByRole(role);
            return CallAbsent(p) == null && VisitPair(p) is { } v && VisitIsPressed(v.leaves, v.visited);
        }

        /// <summary>Why the visit a call on `called` would send won't happen now (rulebook summon), or code null; walkTicks: the walk of the one who
        /// leaves to the other's post. The one called first (step 4), then the pair: who leaves (the door guard) and the one he goes to.</summary>
        (string? code, string why) SummonRefusal(Person called, out int walkTicks)
        {
            walkTicks = 0;
            var (leaves, visited) = VisitPair(called)!.Value; // CallAbsent let it through: a pair with a post
            if (HoldsCellViolence(called)) return ("call-saw-violence", $"{called.Role} saw what you did and won't act on a call");
            if (GuardDown || leaves.Down) return ("summon-busy", "he's down");
            if (visited.Down) return ("summon-busy", "she's down");
            var at = GuardPose(); if (!at.HasValue) return ("summon-off-street", "he isn't on the street");
            if (GuardHeld || GuardEngaged || !GuardAtDoor) return ("summon-busy", "he's busy");
            if (Alert > R.SummonAlertMax) return ("summon-alert", "security is searching: he won't leave");
            if (HoldsCellViolence(leaves)) return ("summon-saw-violence", "he saw what you did");
            if (leaves.Summoned >= R.SummonsPerShift) return ("summon-came-once", "he came once already");
            var to = ServerOf(visited) is RoundsData.Server vsv ? vsv.At : PersonPos(visited); if (!to.HasValue) return ("summon-off-street", "she isn't on the street"); // brief 022: the summons reads her post, never her pose
            var route = Walk.Route(Walk.NearestFree(at.Value.pos) ?? at.Value.pos, Walk.NearestFree(to.Value) ?? to.Value);
            if (route == null) return ("summon-too-far", "no walk between them");
            long len = new Path(route).Length; walkTicks = (int)((len + peopleWalk - 1) / peopleWalk); // at the city's walking pace (crowd.walk; engine 0.34.0: cell.speed before)
            if (walkTicks > T(R.SummonWalkMaxS)) return ("summon-too-far", $"too far: {walkTicks / Hz} s away");
            return (null, "");
        }

        /// <summary>The call card, before the click (Latch): would the Fixer's call on this role be taken now, and if not, its refusal code,
        /// words and numbers, exactly as Issue would answer it. Read-only: it changes nothing.</summary>
        public Answer CallQuery(string role)
        {
            var saved = refusal; refusal = default; string why;
            try
            {
                var op = OpOf(OpId.Fixer);
                why = Over ? No("mission-over", "the mission is over")
                    : op.Down ? No("agent-down", "the fixer is down")
                    : op.Busy != null ? No("agent-busy", $"fixer is busy until {op.BusyEnd}", tick: op.BusyEnd)
                    : CallRefusal(op, role);
            }
            catch (Exception e) when (!(e is OutOfMemoryException)) { why = No("engine-error", "engine error: " + e.GetType().Name + ": " + e.Message); }
            var a = AnswerOf(why);
            refusal = saved;
            return a;
        }

        /// <summary>Why the Fixer's call on a role is refused now, or "" (the checks of Call, without its effects): the one called, what the call is
        /// (absent), then what has happened to the pair (shut).</summary>
        string CallRefusal(Op op, string role)
        {
            if (op.Id != OpId.Fixer) return No("only-fixer", "only the Fixer calls contacts (§03)");
            if (!IsPersonRoleName(role)) return No("not-a-contact", $"'{role}' is not a contact"); // engine 0.30.0 (Suture's bot pass): refused, not thrown
            var p = PersonByRole(role);
            if (p.Down) return No("person-down", $"{p.Role} is down");
            if (!p.Turned) return No("not-a-contact", $"{p.Role} is not a contact");
            if (CallAbsent(p) is { } absent) return No(absent.code, absent.why);
            var (code, why) = SummonRefusal(p, out int walk);
            return code != null ? No(code, why, tick: code == "summon-too-far" ? walk : -1) : "";
        }

        string Call(Op op, Order o)
        {
            var no = CallRefusal(op, o.Target); if (no.Length > 0) return no;
            return Summon(PersonByRole(o.Target)); // CallRefusal let it through: the visit goes
        }

        /// <summary>The summons (rulebook summon; Motive, motive-lovers.md §1.3; option 1, brief 102): a call on a turned contact empties a post, read
        /// from the city's ties. The one who leaves walks the grid to the other's post at walking pace, stays summon.stay and walks back (a pressed
        /// visit, either of them pressured: GamePressedCall.cs). Counted on the one who leaves, whichever of them was called.</summary>
        string Summon(Person p)
        {
            SummonRefusal(p, out int walk);
            var (leaves, visited) = VisitPair(p)!.Value;
            var sv = ServerOf(visited);
            bool pressed = VisitIsPressed(leaves, visited);
            var v = VisitRoute(visited, leaves, walk); guardOut = v.outWalk; guardBack = v.backWalk; walkT = v.walkT; cafeFace = v.face;
            stayT = T(pressed ? R.SummonPressedStayS : R.SummonStayS); backT = pressed ? RunTicks(guardBack) : walkT; guardRunning = false; GuardCallPressed = pressed;
            visitWarner = !pressed ? "" : visited.Has(R.SummonPressedAct!) ? visited.Role : leaves.Role;
            if (sv != null) RoundCall(visited, Tick + walkT, Tick + walkT + stayT, guardOut.End); // brief 022: they stand together while he stays
            LureTick = Tick; GuardAwayUntil = Tick + walkT + stayT + backT; leaves.Summoned++; LureCalled = p.Role;
            AddTrail(R.CallTrail, "call:" + p.Role); Remember(p, "job", "fixer", false);
            Actions.Add("call:" + p.Role); Chain.Add(p == leaves ? $"{p.Role}-goes-to-{visited.Role}" : $"{p.Role}-calls-{leaves.Role}");
            Note("call." + p.Role, p == leaves ? $"{p.Name} takes the call. He leaves his post and walks over to {visited.Name} ({walkT / Hz} s), to stay {stayT / Hz} s."
                : $"{p.Name} calls {leaves.Name}. He leaves his post and walks over to her ({walkT / Hz} s), to stay {stayT / Hz} s.");
            return "";
        }
    }
}
