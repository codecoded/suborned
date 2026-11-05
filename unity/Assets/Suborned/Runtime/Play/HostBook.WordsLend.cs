using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 096, the lend and the rules round (engines 0.40.0-0.42.0): placeholder words for Fable in text.json's book block. book.cars: the two Go's and the
    /// end card once Popovic is out; book.police: the countdown plate under the clock after vale.out, with its End button; book.tape: the camera mark on Pay and
    /// Pressure; book.parked: the Drive card's parked notice. Each block optional: a text without one shows these placeholders, never a blank. Listed in
    /// briefs/096/latch/new-words-lend.md.</summary>
    public sealed partial class BookWordsFile
    {
        public string GoLent = "Go · Popovic out", GoClear = "Gets the rest clear", EndWinHead = "Popovic is out", EndWinLine = "Popovic is out in his own car.", LentName = "Popovic's car, ours now";
        public string CarLost = "We left our car behind. The police will trace it. Trail +{trail}.", LentSeen = "{by} saw Popovic's car leave: a lead, not our car. Trail +{trail}.";
        public string PoliceHead = "Police close in {time}", PoliceWhy = "Popovic is out. Get the rest clear.", PoliceLeft = "Still out: {list}", PoliceClear = "Everyone is clear", PoliceEnd = "End now";
        public string TapeSeen = "{camera} would tape it: Alert {alert} in {s} s, +{trail} T", TapeMaybe = "A camera might see this: watch {camera} first", TapeLoopEnds = "loop ends {t}";
        public string ParkedSeen = "Parked here, {who} would see it: reported after {s} s", ParkedMaybe = "Parked here, {who} might see it: watch first", ParkedBack = "patrol back {t}";

        void ReadLend(JNode b)
        {
            static string S(JNode n, string k, string or) => n?.Opt(k) is JNode v && v.Kind == JKind.Str && v.Str().Length > 0 ? v.Str() : or;
            if (b.Opt("cars") is JNode c) { GoLent = S(c, "goLent", GoLent); GoClear = S(c, "goClear", GoClear); EndWinHead = S(c, "endWinHead", EndWinHead); EndWinLine = S(c, "endWinLine", EndWinLine); LentName = S(c, "lentName", LentName); CarLost = S(c, "carLost", CarLost); LentSeen = S(c, "lentSeen", LentSeen); }
            if (b.Opt("police") is JNode p) { PoliceHead = S(p, "head", PoliceHead); PoliceWhy = S(p, "why", PoliceWhy); PoliceLeft = S(p, "left", PoliceLeft); PoliceClear = S(p, "clear", PoliceClear); PoliceEnd = S(p, "end", PoliceEnd); }
            if (b.Opt("tape") is JNode t) { TapeSeen = S(t, "seen", TapeSeen); TapeMaybe = S(t, "maybe", TapeMaybe); TapeLoopEnds = S(t, "loopEnds", TapeLoopEnds); }
            if (b.Opt("parked") is JNode k) { ParkedSeen = S(k, "seen", ParkedSeen); ParkedMaybe = S(k, "maybe", ParkedMaybe); ParkedBack = S(k, "back", ParkedBack); }
        }
    }
}
