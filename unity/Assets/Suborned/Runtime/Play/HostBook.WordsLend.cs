using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 096, the lend and the rules round (engines 0.40.0-0.42.0): placeholder words for Fable in text.json's book block. book.cars: the two Go's (with G1's
    /// wait for one climbing in, engine 0.43.0) and the end card once Popovic is out; book.police: the countdown plate under the clock after vale.out, with its End button; book.tape: the camera mark on Pay and
    /// Pressure; book.parked: the Drive card's parked notice. Each block optional: a text without one shows these placeholders, never a blank. Listed in
    /// briefs/096/latch/new-words-lend.md.</summary>
    public sealed partial class BookWordsFile
    {
        public string GoLent = "Go · drive him off", GoClear = "Gets the rest clear", EndWinHead = "Popovic is out", EndWinLine = "We drove Popovic off in his own car.", LentName = "Popovic's car, now ours";
        public string GoOnce = "goes once {list} is in", GoWaiting = "Go · waiting for {list}", GoWaitsRoster = "then the car goes"; // engine 0.43.0 (G1): HostGoWaits
        public string CarLost = "We left our car behind, and the police will trace it. Trail +{trail}.", LentSeen = "{by} saw Popovic's car leave: a lead, but not to our car. Trail +{trail}.";
        public string PoliceHead = "Police close in {time}", PoliceWhy = "Popovic is out. The rest get clear in our car.", PoliceLeft = "Still here: {list}", PoliceClear = "Everyone is clear", PoliceEnd = "End now";
        public string TapeSeen = "{camera} would tape it: Alert {alert} in {s} s, +{trail} T", TapeMaybe = "A camera might tape this: Watch {camera} to be sure", TapeLoopEnds = "loop ends {t}";
        public string ParkedSeen = "Parked here, {who} would see it and report it in {s} s", ParkedMaybe = "Parked here, {who} might see it: Watch first to be sure", ParkedBack = "patrol back {t}";

        void ReadLend(JNode b)
        {
            static string S(JNode n, string k, string or) => n?.Opt(k) is JNode v && v.Kind == JKind.Str && v.Str().Length > 0 ? v.Str() : or;
            if (b.Opt("cars") is JNode c) { GoLent = S(c, "goLent", GoLent); GoClear = S(c, "goClear", GoClear); EndWinHead = S(c, "endWinHead", EndWinHead); EndWinLine = S(c, "endWinLine", EndWinLine); LentName = S(c, "lentName", LentName); CarLost = S(c, "carLost", CarLost); LentSeen = S(c, "lentSeen", LentSeen); GoOnce = S(c, "goOnce", GoOnce); GoWaiting = S(c, "goWaiting", GoWaiting); GoWaitsRoster = S(c, "goWaitsRoster", GoWaitsRoster); }
            if (b.Opt("police") is JNode p) { PoliceHead = S(p, "head", PoliceHead); PoliceWhy = S(p, "why", PoliceWhy); PoliceLeft = S(p, "left", PoliceLeft); PoliceClear = S(p, "clear", PoliceClear); PoliceEnd = S(p, "end", PoliceEnd); }
            if (b.Opt("tape") is JNode t) { TapeSeen = S(t, "seen", TapeSeen); TapeMaybe = S(t, "maybe", TapeMaybe); TapeLoopEnds = S(t, "loopEnds", TapeLoopEnds); }
            if (b.Opt("parked") is JNode k) { ParkedSeen = S(k, "seen", ParkedSeen); ParkedMaybe = S(k, "maybe", ParkedMaybe); ParkedBack = S(k, "back", ParkedBack); }
        }
    }
}
