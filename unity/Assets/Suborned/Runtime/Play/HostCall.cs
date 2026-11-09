using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 102 (engine 0.51.0, Sky's calls on the guard; Keel, briefs/102/keel/report-turned-guard-call.md; Motive, briefs/102/motive/turned-guard.md §1;
    /// Glyph, briefs/101/glyph/r4/notes.md "For Aster" 2): the Fixer's way on a turned contact.
    ///  · The Call where the engine offers one (Game.CallOffered: Inez, and the turned guard, who leaves his own door under option 1), shut with the engine's own
    ///    answer (CallQuery) when play or the clock has stopped it.
    ///  · Where a call has no use (the turned driver, a contact with no tie) it is absent, not greyed, and the Fixer's click answers with one "Already turned" lock:
    ///    a shut Turn wedge with the turned pictogram and Fable's short lock. The four turn ways stay absent by the same law; the violent ways follow it, dashed
    ///    and last, as E2 has them.
    ///  · A refused call: the contact's answer, and her tie's only where he hears it (TieHears). Step 4's call-saw-violence rings nothing: no card, no line, the
    ///    pop the lock's own short (IssueCore makes the card only for the summons' codes).</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>A turned cast person but Popovic: the Fixer's one row on them is the Call, or the Already turned lock (TurnedRow).</summary>
        bool TurnedContact(Target t) => t != null && t.Kind == "person" && t.Key != "councillor" && CastOf(t.Key) is { Turned: true };

        /// <summary>Is a Call on this role on the order list (engine 0.51.0)? Read-only; an engine fault stops the host, as any read does.</summary>
        bool CallOffered(string role) { bool on = false; Guard("call offer", () => on = Game.CallOffered(role)); return on; }

        /// <summary>The Fixer on a turned contact for a picked verb's chip and click (IntentFor): the Call, else the lock, which gives no order.</summary>
        Intent TurnedIntent(Target t) => CallOffered(t.Key) ? Call(t) : AlreadyTurned(t);

        /// <summary>The Fixer's one row on a turned contact (BuildPanel): the Call with its lock, or the Already turned lock, never available.</summary>
        PanelRow TurnedRow(Target t)
        {
            if (CallOffered(t.Key)) { var c = Call(t); Words(c, t); var cl = CallLock(t.Key); return new PanelRow { By = OpId.Fixer, Slot = -1, I = c, Verb = c.Name, Lock = cl, Reason = cl.ok ? "" : cl.why }; }
            var i = AlreadyTurned(t); return new PanelRow { By = OpId.Fixer, Slot = -1, I = i, Verb = i.Name, Lock = TurnedLock, Reason = TurnedLock.why };
        }

        /// <summary>The engine's own code for a turn on someone already turned (GameOrders "turned"), so the lock reads Fable's words for it.</summary>
        static readonly Answer TurnedLock = new() { ok = false, code = "already-turned", why = "already turned", m = -1, cm = -1, tick = -1, usd = -1, ids = "" };

        /// <summary>Glyph's lock: the Fixer's Turn as a whole on someone already turned: no approach, the turned pictogram, the ring's Turn word (Fable's ring-turn)
        /// with the target, Fable's already-turned short and line. A lock, never an order: it isn't actionable and its row is never available.</summary>
        Intent AlreadyTurned(Target t)
        {
            var o = new Order { Op = "fixer", Do = "turn", Target = t.Key, Approach = "" };
            string turn = Text != null && Text.Actions.TryGetValue("ring-turn", out var rt) && rt.Label.Length > 0 ? rt.Label : "Turn";
            var i = new Intent { Slot = -1, Order = o, Lead = "turned", Label = $"{turn} {t.Label}", Name = $"{turn} {Name(t.Key)}", Locked = true, LockCode = TurnedLock.code, Doer = (int)OpId.Fixer };
            i.LockShort = ShortOf(o, TurnedLock) ?? TurnedLock.why; i.LockLong = LockLine(o, TurnedLock) ?? i.LockShort; i.Hint = i.LockShort; i.Does = i.LockLong;
            return i;
        }

        Intent Call(Target t)
        {
            var i = new Intent { Slot = -1, Label = $"Call {t.Label}", Order = new Order { Op = "fixer", Do = "call", Target = t.Key } };
            i.Time = "now"; i.Trail = $"T +{Game.R.CallTrail}"; i.Outcome = Game.CanCall(t.Key) ? "ready" : "not now";
            return i;
        }

        /// <summary>Why the Fixer can't call a turned contact now: the engine's own answer before the click (Game.CallQuery, the same Answer Issue would give,
        /// read-only), so the card is locked with the right reason (Fable's lock line by code); a race between hover and click still gets the refusal card.</summary>
        Answer CallLock(string role)
        {
            Answer a = new Answer { ok = true, code = "", why = "" };
            Guard("call query", () => a = Game.CallQuery(role));
            a.why ??= ""; a.code ??= "";
            return a;
        }

        /// <summary>The refused call's rows (text.json summon-refused) for the one called: on the leaver's own call (the guard's, option 1) a row `about` him is
        /// that call's (his words to the Fixer, and hers to what she hears); Inez's call reads the rows with no `about`, as before, so its lines don't move.</summary>
        string SummonAbout(string role) { string leaves = ""; Guard("call leaver", () => leaves = Game.CallLeaver(role)); return leaves == role ? role : null; }

        /// <summary>Fable's keys (briefs/102/fable/notes.md, "The guard's end of the call"): the café's lines and scene on the guard's own call are his (`about` guard:
        /// "Look who's left his door."); on Inez's call, null, so hers play as before.</summary>
        string CafeAbout() => Game != null && Game.LureCalled is { Length: > 0 } lc ? SummonAbout(lc) : null;

        /// <summary>Who warns the other on a pressed visit (the engine's GameCall visitWarner, words only): the one visited if she remembers the Pressure, else the one who
        /// left if he does; "" for a plain visit. Fable: the warning is picked by who was pressured, never by who called.</summary>
        string PressedWarner()
        {
            var g = Game; string leaver = ""; Guard("call leaver", () => leaver = g.CallLeaver(g.LureCalled)); var lp = CastOf(leaver); var vp = lp?.Lover; var act = g.R.SummonPressedAct;
            if (act == null || lp == null) return "";
            return vp != null && vp.Has(act) ? vp.Role : lp.Has(act) ? lp.Role : "";
        }

        /// <summary>The cell knows this contact's tie (Motive turned-guard.md §1.3): a Listen on either of the pair learned the affair, or a briefing hint names them both (the
        /// Handover's: "Inez and the door guard are close"). Until then a summons' lock names only the one called (Fable's tie-unknown).</summary>
        bool TieKnown(Person a)
        {
            var b = a?.Lover; if (b == null || a.Knows("secret") || b.Knows("secret")) return true;
            if (Text == null) return false;
            string Word(Person p) { var n = Text.TargetName(p.Role, p.Name) ?? p.Name; return n.StartsWith("the ", System.StringComparison.Ordinal) ? n[4..] : n; } // "Inez", "guard"
            foreach (var h in Text.Hints) if (h.Contains(Word(a)) && h.Contains(Word(b))) return true;
            return false;
        }
        /// <summary>The lock variant a refusal is worded by: tie-unknown for a summons' refusal on a Call before the cell knows the tie; else the text's own (LockVariant).</summary>
        string LockVariantFor(Order o, Answer a) => o?.Do == "call" && (a.code ?? "").StartsWith("summon-", System.StringComparison.Ordinal) && CastOf(o.Target) is Person p && !TieKnown(p) ? "tie-unknown" : LockVariant(a, TargetKindOf(o?.Target ?? ""));

        /// <summary>Fable's line for a call refused summon-busy because the one who'd leave his post is the contact's tie and he's down (brief 102; engine 0.52.0's
        /// Game.KnowsDown): summon-knows-down where she knows it (she found him, saw him carried or was told), else summon-unanswered (she tries him and nobody
        /// answers); "" in any other case, and when she is down herself (then nobody answers at all: FeedSummonRefused).</summary>
        string TieDownEvent(string role, Answer a)
        {
            if (a.code != "summon-busy" || CastOf(role) is not Person p || p.Lover is not Person tie || IsDown(p)) return "";
            string leaver = ""; Guard("call leaver", () => leaver = Game.CallLeaver(role)); if (leaver != tie.Role || !IsDown(tie)) return "";
            bool knows = false; Guard("knows down", () => knows = Game.KnowsDown(role, leaver));
            return knows ? "summon-knows-down" : "summon-unanswered";
        }
        /// <summary>A cast person down: knocked out, tied or dead (the door guard by the engine's GuardDown).</summary>
        bool IsDown(Person p) => p.Down || p == Game.GuardP && Game.GuardDown;

        /// <summary>Brief 102 (Fable's findings; text.json events summon-refused: "Inez answers the Fixer; the guard, if he is within earshot"): whether the
        /// contact's tie hears a refused call, and so answers. Never when he is down (knocked out, tied or dead: the engine's "he's down"). A call that sends for
        /// the tie (Inez's: he'd leave his door for her) reaches him where the engine refused it for another reason (busy on his feet, an alarm, what he saw, his
        /// one visit), as the call scene has him answer her from his door. Where it can't reach him (off the street, too far), or the call isn't for the tie (the
        /// Fixer called the guard himself: under option 1 he is the one who'd leave, and Inez isn't on that call; no visit at all), the tie hears only standing
        /// within earshot of the contact: both out on the street, within the people's reach (rulebook witness-range-m, Fable's "sight or earshot", brief 012).</summary>
        bool TieHears(string code, Person contact, Person tie)
        {
            if (IsDown(tie)) return false;
            string leaves = ""; Guard("call leaver", () => leaves = Game.CallLeaver(contact.Role));
            if (leaves == tie.Role && code is not ("summon-off-street" or "summon-too-far")) return true;
            var at = Game.PersonPos(contact); var by = Game.PersonPos(tie);
            return at.HasValue && by.HasValue && Suborned.Sim.G.Within(at.Value, by.Value, Game.R.WitnessRangeCm);
        }
    }
}
