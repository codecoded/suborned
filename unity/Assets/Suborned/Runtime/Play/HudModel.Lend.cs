using System;
using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 096, the lend (engine 0.42.0, car.lend L9): the debrief's car lines in plain words. Our car seen leaving (car-seen, as brief 018); our car left
    /// standing at the end (the engine's CarSeenBy "left": book.cars.carLost, +1 T, hunted); Popovic's car seen leaving (lent-seen: book.cars.lentSeen, +1 T and a lead).</summary>
    public sealed partial class HudModel
    {
        void CarLines(HandoverHost h, Debrief d, PlayText t, Func<Dictionary<string, string>, string, string> W)
        {
            string trail = h.Game.R.WitnessTrail.ToString(); var bw = BookWordsFile.Get();
            if (d.Next.CarSeenBy == "left") EndLines.Add(bw.CarLost.Replace("{trail}", trail));
            else if (d.Next.CarSeenBy.Length > 0 && t.DebriefBetween.TryGetValue("car-seen", out var cs))
                EndLines.Add(cs.Replace("{by}", W(t.GetawayBy, PlayText.RoleOfId(d.Next.CarSeenBy))).Replace("{trail}", trail));
            if (h.Game.Log.Exists(x => x.Code == "car.shot-out")) EndLines.Add(bw.ShotOut); // brief 105 (engine 0.53.0): our car shot out, the fight went on
            foreach (var l in d.Next.Lines) if (l.Line == "lent-seen") { var ln = (h.LentCarId == "car:official" ? bw.LentSeen : bw.TakenSeen.Replace("{car}", h.TakenCarWord())).Replace("{by}", W(t.GetawayBy, PlayText.RoleOfId(l.Who))).Replace("{trail}", trail); EndLines.Add(ln.Length > 0 ? char.ToUpperInvariant(ln[0]) + ln.Substring(1) : ln); }
        }
    }
}
