using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 096, the rivals round in play (engine 0.41.0, rivals.turn): placeholder words for Fable in text.json's book.crew block, the cards of the cell's
    /// ways on a member of the rival crew on foot (Watch, Listen, Pay, Mislead; HostCrewTurn) and their feed footers. The block is optional: a text without it
    /// shows these placeholders, never a blank. Listed in briefs/096/latch/new-words-lend.md.</summary>
    public sealed partial class BookWordsFile
    {
        public string CrewLookout = "the lookout", CrewTimetable = "their timetable", CrewTemperamentWord = "money or loyalty";
        public string CrewWatchDoes = "Learn the crew's timetable: where each of them goes, and when.", CrewListenDoes = "Hear whether he's in it for the money or loyal to his crew.";
        public string CrewPayDoes = "{usd} to walk away. A loyal man refuses and tells his crew.", CrewPayDoesMercenary = "He's in it for the money: he takes {usd} and walks off for good.";
        public string CrewPayDoesLoyal = "He's loyal: he refuses, tells his crew about the Fixer, and we keep the money.";
        public string CrewPayUnknown = "takes it, or refuses and tells his crew: Listen first", CrewPayTakes = "takes it and walks off for good", CrewPayRefuses = "refuses: his crew will know the Fixer";
        public string CrewMisleadDoesLookout = "Send him to watch the other door for a while. He comes back knowing the Scout's face.";
        public string CrewMisleadDoesPosted = "Send him to the lookout's post for a while. He comes back knowing the Scout's face.";
        public string CrewBack = "back at his post {t}", CrewFace = "he'll know the Scout's face", CrewSees = "He sees you coming and fires: come from behind", CrewArmed = "Armed: if he sees you coming, he fires";
        public string CrewHeardMercenary = "{who} is in it for the money.", CrewHeardLoyal = "{who} is loyal to his crew.", CrewHeardNothing = "Nothing learned.";
        public string CrewPaid = "{who} takes the money and walks off.", CrewRefused = "{who} won't take it, and tells his crew. The money stays with us.", CrewPayMissed = "{who} was gone before the Fixer paid. The money stays with us.";
        public string CrewMisled = "He takes the word and walks off. He'll be back.", CrewMisleadMissed = "The word missed him: he'd gone, or was already after Popovic's car.";

        void ReadCrew(JNode b)
        {
            static string S(JNode n, string k, string or) => n?.Opt(k) is JNode v && v.Kind == JKind.Str && v.Str().Length > 0 ? v.Str() : or;
            if (b.Opt("crew") is not JNode c) return;
            CrewLookout = S(c, "lookout", CrewLookout); CrewTimetable = S(c, "timetable", CrewTimetable); CrewTemperamentWord = S(c, "temperament", CrewTemperamentWord);
            CrewWatchDoes = S(c, "watchDoes", CrewWatchDoes); CrewListenDoes = S(c, "listenDoes", CrewListenDoes);
            CrewPayDoes = S(c, "payDoes", CrewPayDoes); CrewPayDoesMercenary = S(c, "payDoesMercenary", CrewPayDoesMercenary); CrewPayDoesLoyal = S(c, "payDoesLoyal", CrewPayDoesLoyal);
            CrewPayUnknown = S(c, "payUnknown", CrewPayUnknown); CrewPayTakes = S(c, "payTakes", CrewPayTakes); CrewPayRefuses = S(c, "payRefuses", CrewPayRefuses);
            CrewMisleadDoesLookout = S(c, "misleadDoesLookout", CrewMisleadDoesLookout); CrewMisleadDoesPosted = S(c, "misleadDoesPosted", CrewMisleadDoesPosted);
            CrewBack = S(c, "back", CrewBack); CrewFace = S(c, "face", CrewFace); CrewSees = S(c, "sees", CrewSees); CrewArmed = S(c, "armed", CrewArmed);
            CrewHeardMercenary = S(c, "heardMercenary", CrewHeardMercenary); CrewHeardLoyal = S(c, "heardLoyal", CrewHeardLoyal); CrewHeardNothing = S(c, "heardNothing", CrewHeardNothing);
            CrewPaid = S(c, "paid", CrewPaid); CrewRefused = S(c, "refused", CrewRefused); CrewPayMissed = S(c, "payMissed", CrewPayMissed); CrewMisled = S(c, "misled", CrewMisled); CrewMisleadMissed = S(c, "misleadMissed", CrewMisleadMissed);
        }
    }
}
