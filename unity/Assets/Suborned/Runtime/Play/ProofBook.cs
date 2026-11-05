using System;
using System.Collections;
using System.Collections.Generic;
using File = System.IO.File;
using Path = System.IO.Path;
using Directory = System.IO.Directory;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Order = Suborned.Sim.Order;

namespace Suborned.Play
{
    /// <summary>Brief 088's rows, in the r083 section (Proof083 calls it): the clock stops and resumes exactly; a saved run replays to the same hash with the book opened and shut during it; every
    /// slot empty takes no room; a test section added by data; the page's locks agree with the ring's on the same target; a way opens the ring and gives no order; no cost with the book shut.
    /// `-bookShots &lt;dir&gt;` is its own runner: the 1920 x 1080 pictures for Sky (briefs/088/latch), on a real route's moment.</summary>
    public static class ProofBook
    {
        static void Keys(params Key[] down) { if (Keyboard.current is Keyboard kb) InputSystem.QueueStateEvent(kb, new KeyboardState(down)); }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(s); }
        static IEnumerator Press(Key k) { Keys(k); yield return Frames(2); Keys(); yield return Frames(2); }
        /// <summary>Route A's orders, as recorded (contracts/missions/handover/routes).</summary>
        internal static List<Order> Route(string name = "route-a") { var path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), "contracts", "missions", "handover", "routes", name + ".json"); return Script.Parse(File.ReadAllText(path)).Orders; }
        static float Tw(string s, float size, string font) => (s ?? "").Length * size * .52f;

        static IEnumerator Fresh(HandoverHost h) { if (h.BookOpen) h.BookClose(); h.controls.conversations = ConversationScenes.Never; h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(false); yield return Frames(3); }

        /// <summary>The cash page: the section one data entry adds; the proof's and the capture's, not the game's.</summary>
        /// <summary>A weapon no agent carries, made from data only: the page must draw it with nothing per weapon in code.</summary>
        public static Weapon TestWeapon() => new("longarm", 3, 25, 2500, 4000);
        public static BookSection TestSection() => new("cash", "cash", "Cash", h => new BookPage { Section = "cash", HeroIcon = "cash", Name = "$" + h.Game.Cash.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), Sub = "in hand" });

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, Action<string, bool, string> Row)
        {
            yield return Fresh(h); var g = h.Game;
            UnityEngine.Debug.Log("ProofBook: the clock: J stops it, J starts it where it stood;"); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- the clock: J stops it, J starts it where it stood; a stopped clock stays stopped; Esc and the back control close it too
            yield return Wait(0.4f); int t0 = g.Tick; yield return Press(Key.J); bool up = h.BookOpen && h.Modal == ModalGate.Modal.Book && h.Paused && HudToolkit.BookShown; int t1 = g.Tick; yield return Wait(0.5f); int t2 = g.Tick;
            yield return Press(Key.J); bool shut = !h.BookOpen && !h.Paused && !HudToolkit.BookShown; yield return Wait(0.4f); int t3 = g.Tick;
            Row("bookClock", up && t2 == t1 && shut && t3 > t2 && t1 - t0 >= 0, $"up {up}, ticks {t0} {t1} {t2} {t3}, closed {shut}");
            h.SetPaused(true); yield return Frames(2); yield return Press(Key.J); bool up2 = h.BookOpen; yield return Press(Key.Escape); bool stays = !h.BookOpen && h.Paused && !h.MenuOpen; int t4 = g.Tick; yield return Wait(0.3f);
            Row("bookStopsStopped", up2 && stays && g.Tick == t4, $"opened {up2}, Esc closed and left the clock stopped {stays}, no menu {!h.MenuOpen}"); h.SetPaused(false); yield return Frames(2);
            UnityEngine.Debug.Log("ProofBook: the modal gate: J does nothing under the briefing "); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- the modal gate: J does nothing under the briefing or the pause menu; street keys do nothing under the book
            h.OpenMenu(); yield return Frames(2); yield return Press(Key.J); bool underMenu = h.BookOpen; h.ResumeFromMenu(); yield return Frames(2);
            h.SetPaused(true); h.ShowBriefing = true; yield return Frames(2); bool startBook = h.BookOpen && h.BookStart && h.BookSectionNow == "job"; yield return Press(Key.J); bool underBrief = !(startBook && !h.BookOpen && !h.ShowBriefing && h.Paused); h.SetPaused(false); yield return Frames(2); // brief 096: the start is the book on Job; J shuts it, the clock still stopped
            h.BookOpenNow(); yield return Frames(3); int n0 = g.Orders.Count, w0 = h.WorldPresses; bool i0 = h.IntelOpen; yield return Press(Key.I); yield return Press(Key.F); yield return Press(Key.Space); bool stillOpen = h.BookOpen && h.Paused && h.IntelOpen == i0 && g.Orders.Count == n0; // brief 096: I (Intel) for the old = (speed, no key now)
            h.BookClose(); yield return Frames(2);
            Row("bookModal", !underMenu && !underBrief && stillOpen && h.WorldPresses == w0, $"under the menu {underMenu}, the start book misbehaved {underBrief}, street keys under the book left it up with no order {stillOpen}");
            UnityEngine.Debug.Log("ProofBook: both views: it opens in third person and the follo"); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- both views: it opens in third person and the follow camera stays
            h.DriveEnter083(); yield return Wait(0.5f); bool third = h.Following; yield return Press(Key.J); bool tOpen = h.BookOpen && h.Paused; var cam3 = h.View.transform.position; yield return Wait(0.3f); float moved = (h.View.transform.position - cam3).magnitude; yield return Press(Key.J); bool tShut = !h.BookOpen;
            Row("bookThird", third && tOpen && tShut && moved < 0.01f, $"third {third}, open {tOpen}, the camera moved {moved:0.000} m, shut {tShut}");
            h.DriveLetGoToMap(); if (h.Following) h.FollowToggle(); yield return Wait(0.3f);
            UnityEngine.Debug.Log("ProofBook: the map view comes back exactly"); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- the map view comes back exactly
            h.TurnSnap(1); yield return Wait(0.6f); h.View.orthographicSize = 13f; h.LookAt(h.frame.World(g.Ops[3].Pos) + new Vector3(4f, 0f, 2f)); yield return Frames(2); var cp = h.View.transform.position; var cr = h.View.transform.rotation; float cz = h.View.orthographicSize;
            h.BookOpenNow("job"); yield return Wait(0.7f); bool went = (h.View.transform.position - cp).magnitude > 1f; h.BookClose(); yield return Frames(2);
            Row("bookCameraReturns", went && (h.View.transform.position - cp).magnitude < 0.001f && Quaternion.Angle(h.View.transform.rotation, cr) < 0.01f && Mathf.Abs(h.View.orthographicSize - cz) < 0.001f, $"moved to the page's subject {went}, back within {(h.View.transform.position - cp).magnitude:0.0000} m"); h.TurnSnap(0);
            UnityEngine.Debug.Log("ProofBook: a saved run replays to the same hash with the book"); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- a saved run replays to the same hash with the book opened and shut during it
            yield return Fresh(h); g = h.Game; h.controls.planTogether = false; var ra = Route(); int stop = 1400; h.SetPaused(true);
            foreach (var o in ra) { if (o.Tick >= stop) break; h.RunTo(o.Tick); h.Issue(new Order { Op = o.Op, Do = o.Do, To = o.To, Point = o.Point, Target = o.Target, Approach = o.Approach, How = o.How, Run = o.Run }); if (o.Tick % 2 == 0) { h.BookOpenNow("cell", o.Tick % 4); yield return Frames(1); h.BookShow("job"); yield return Frames(1); h.BookClose(); yield return Frames(1); } }
            h.RunTo(stop); var path = Path.Combine(dir, "r088-book-session.json"); string saved = h.SaveSessionTo(path); ulong live = Hash.State(g);
            var sc = Script.Parse(File.ReadAllText(path)); var rg = Engine.New(h.PlayInputs, sc.Start); var rl = new List<(int i, Order o)>(); for (int i = 0; i < sc.Orders.Count; i++) rl.Add((i, sc.Orders[i])); rl.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.i.CompareTo(b.i));
            foreach (var (_, o) in rl) { rg.RunTo(o.Tick); rg.Issue(o); } rg.RunTo(stop); ulong back = Hash.State(rg);
            Row("bookReplay", live == back && g.Tick == stop && rg.Tick == stop, $"tick {g.Tick}, {sc.Orders.Count} orders, live {live:x}, replayed {back:x}, saved {saved}");
            UnityEngine.Debug.Log("ProofBook: every slot empty takes no room"); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- every slot empty takes no room
            var full = new BookPage { HeroFace = "x", Name = "A name", Sub = "a sub", State = "State", FactsHead = "head" }; full.Facts.Add(new BookFact { Word = "fact" }); full.Gear.Add(new BookFact { Word = "gear" }); full.Ways.Add(new BookWay { Word = "way" }); full.Can.Add(new BookCan { Word = "can", Line = "a line" }); full.Scales.Add(new BookScale { Word = "s", Value = 1, Max = 2 }); full.Knows.Add(new BookKnow { Word = "k" }); full.Track.Add(new BookStop { Word = "t", Time = "0:01" }); full.Others.Add(new BookOther { Word = "o" });
            var empty = BookLayout.Place(new BookPage(), Tw); float fb = BookLayout.Place(full, Tw).Bottom; bool noItems = empty.Items.Count == 0 && empty.Bottom == BookLayout.Top && fb > 400f;
            Row("bookEmptySlots", noItems, $"an empty page has {empty.Items.Count} items and ends where it starts; the full one ends at {fb:0}");
            UnityEngine.Debug.Log("ProofBook: a test section by data alone, the signs re-space"); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- a test section by data alone, the signs re-space
            yield return Fresh(h); h.BookOpenNow("job"); yield return Frames(2); int signs0 = HudToolkit.BookSigns; h.BookClose(); yield return Frames(2);
            h.BookSections.Insert(3, TestSection()); h.BookOpenNow("cash"); yield return Frames(3); bool test = h.BookPageNow?.Section == "cash" && HudToolkit.BookSigns == signs0 + 1 && HudToolkit.BookLaidOut != null && !HudToolkit.BookLaidOut.Has(BookSlot.Facts); h.BookClose(); h.BookSections.RemoveAt(3); yield return Frames(2);
            Row("bookTestSection", test, $"signs {signs0} then {signs0 + 1}, its page up, its empty slots absent {test}");
            UnityEngine.Debug.Log("ProofBook: an agent's page: the same rows at any place and ti"); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- an agent's page: the same rows at any place and time, and it names no person; nothing on it is a way
            yield return Fresh(h); g = h.Game; h.controls.planTogether = false; h.SetPaused(true); yield return Frames(2);
            string Shape(int k) { h.BookShow("cell", k); var p = h.BookPageNow; return $"{p.Facts.Count}/{p.Gear.Count}/{p.Scales.Count}/{p.Ways.Count} " + string.Join("|", p.Can.ConvertAll(c => c.Id + ":" + c.Word + ":" + c.Line)); }
            var names = new List<string> { h.ValeWord }; foreach (var r in new[] { "waitress", "driver", "guard", "councillor" }) { var pp = g.PersonByRole(r); if (pp?.Name?.Length > 0) names.Add(pp.Name); var tn = h.Text?.TargetName(r, ""); if (tn?.Length > 0) names.Add(tn.Replace("The ", "").Replace("the ", "")); }
            var shapes = new string[4]; int rowsMoved = 0, namedBad = 0; var nsaid = new List<string>(); h.BookOpenNow("cell", 0); yield return Frames(1);
            for (int k = 0; k < 4; k++) { shapes[k] = Shape(k); if (h.BookPageNow.Ways.Count > 0) namedBad++; foreach (var c in h.BookPageNow.Can) if (c.Id != "car") foreach (var nm in names) if ((c.Word + " " + c.Line).Contains(nm)) { namedBad++; nsaid.Add($"{(OpId)k} {c.Word}: {nm}"); } }
            h.BookClose(); yield return Frames(1); var ras = Route(); var seenTick = new List<int>();
            foreach (var o in ras) { if (o.Tick >= 1400) break; h.RunTo(o.Tick); h.Issue(new Order { Op = o.Op, Do = o.Do, To = o.To, Point = o.Point, Target = o.Target, Approach = o.Approach, How = o.How, Run = o.Run }); if (o.Tick > 0 && o.Tick % 3 == 0 || o.Tick == 1195) { seenTick.Add(o.Tick); h.BookOpenNow("cell", 0); yield return Frames(1); for (int k = 0; k < 4; k++) if (Shape(k) != shapes[k]) { rowsMoved++; nsaid.Add($"{(OpId)k} at {o.Tick}: {Shape(k)}"); } h.BookClose(); yield return Frames(1); } }
            h.RunTo(1400); h.BookOpenNow("cell", 0); yield return Frames(1); for (int k = 0; k < 4; k++) if (Shape(k) != shapes[k]) rowsMoved++; var counts = new int[4]; for (int k = 0; k < 4; k++) { h.BookShow("cell", k); counts[k] = h.BookPageNow.Can.FindAll(c => c.Id != "car").Count; } h.BookClose(); yield return Frames(1);
            Row("bookAgentRows", rowsMoved == 0 && namedBad == 0 && seenTick.Count >= 2 && counts[0] == h.BookCanOf(0).Count && counts[1] == HandoverHost.Slots[1].Length, $"rows {string.Join(" ", counts)} at tick 0, {string.Join(" ", seenTick)} and 1400; moved {rowsMoved}, named a person {namedBad}, {string.Join("; ", nsaid)}");
            UnityEngine.Debug.Log("ProofBook: the weapon block's pips are the data scale applied"); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- the weapon block's pips are the data scale applied to the engine's own values, for each agent's weapon and a test weapon by data
            int wbad = 0; var wsaid = new List<string>(); var scales = BookWordsFile.Get().WeaponStats;
            for (int k = 0; k <= 4; k++)
            {
                var w = k < 4 ? g.Ops[k].Gun.W : TestWeapon(); int ag = k < 4 ? k : 3; if (w == null) continue; var bw = h.BookWeaponOf(ag, w);
                var got = new List<string>(); bool ok = bw.Stats.Count == scales.Count && scales.Count >= 4;
                for (int i = 0; i < scales.Count && ok; i++)
                {
                    var (id, _, fullV, pips) = scales[i]; float v = id == "damage" ? w.Damage : id == "rate" ? 600f / w.Every * Game.Hz / 10f : id == "range" ? w.MaxCm / 100f : g.Ops[ag].Gun.Aim; // the engine's fields, read here apart from the page's own read
                    int want = Mathf.Clamp((int)Math.Round(v / fullV * pips, MidpointRounding.AwayFromZero), v > 0 ? 1 : 0, pips); ok &= bw.Stats[i].id == id && bw.Stats[i].pips == want && bw.Stats[i].of == pips; got.Add($"{id} {bw.Stats[i].pips}/{pips} ({v:0.#})");
                }
                if (!ok) wbad++; wsaid.Add($"{(k < 4 ? ((OpId)k).ToString() : "test")} {w.Id}: {string.Join(", ", got)}");
            }
            Row("bookWeaponFigures", wbad == 0, $"{wbad} wrong; {string.Join(" | ", wsaid)}");
            UnityEngine.Debug.Log("ProofBook: the job's conditions: each mark equals its live ch"); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- the job's conditions: each mark equals its live check at points along route A and the pickup route; every check in the data is in the vocabulary; a test condition by data appears
            int nbad = 0, npts = 0; var nmet = new Dictionary<string, int>(); var nsaid2 = new List<string>(); var nw = BookWordsFile.Get().Needs; bool vocab = true;
            foreach (var l in nw.Values) foreach (var n in l) vocab &= h.BookCheck(n.Check) != null && (n.Known.Length == 0 || h.BookCheck(n.Known) != null);
            foreach (var route in new[] { "route-a", "route-b" })
            {
                yield return Fresh(h); g = h.Game; h.SetPaused(true); var ro = Route(route); int oi = 0;
                for (int at = 300; at <= 4200 && !g.Over; at += 300)
                {
                    while (oi < ro.Count && ro[oi].Tick <= at && !g.Over) { var o = ro[oi++]; h.RunTo(o.Tick); h.Issue(new Order { Op = o.Op, Do = o.Do, To = o.To, Point = o.Point, Target = o.Target, Approach = o.Approach, How = o.How, Run = o.Run }); }
                    if (g.Over) break; h.RunTo(at); if (g.Over) break; h.BookOpenNow("job"); yield return Frames(1); npts++;
                    foreach (var n in h.BookPageNow.Needs)
                    {
                        bool withV = g.Vale == ValeAt.WithEnforcer || g.Vale == ValeAt.Car, unk = n.Id == "pickup-camera" && g.Known("camera:pickup") is not { Sight: true } || n.Id == "left-safe" && g.Vale is not (ValeAt.Car or ValeAt.Lent or ValeAt.Out); // brief 096 (Fable): left-safe is "?" until Popovic is in a car
                        bool want = n.Id switch { "guard-away" => !g.GuardAtDoor, "door-camera" => g.CameraDark("entrance") || g.LoopOn && g.LoopCam == "entrance", "inside" => g.Ops[3].Inside || withV, "getaway" => g.Vale == ValeAt.Car,
                            "driver" => g.PersonByRole("driver").Turned, "pickup-camera" => !unk && (g.CameraDark("pickup") || g.LoopOn && g.LoopCam == "pickup"), "has-vale" => withV,
                            "guarded" => g.Vale == ValeAt.Car && (!g.CarHeld() || System.Array.Exists(g.Ops, o => !o.Down && !o.InCar && !o.Inside && o.Gun.Drawn && o.Gun.HolsterAt < 0)), "go" => g.Log.Exists(l => l.Code == "success"), "left-safe" => g.LeftBehind().TrueForAll(x => x.fate == "walked"), _ => !n.Met }; // brief 096: the getaway's conditions, read from the engine
                        want &= !unk; if (n.Met != want || n.Unknown != unk) { nbad++; nsaid2.Add($"{route} {HandoverHost.MissionClock(at)} {n.Group}/{n.Id}: page {n.Met}{(n.Unknown ? "?" : "")}, engine {want}{(unk ? "?" : "")}"); }
                        if (n.Met) nmet[n.Group + "/" + n.Id] = (nmet.TryGetValue(n.Group + "/" + n.Id, out var c) ? c : 0) + 1;
                    }
                    h.BookClose(); yield return Frames(1);
                }
            }
            nw["front-door"].Add(new BookWordsFile.Need { Id = "test-need", Words = "A test condition", Check = "log:start" }); h.BookOpenNow("job"); yield return Frames(2);
            var tneed = h.BookPageNow.Needs.Find(x => x.Id == "test-need"); bool testOk = tneed != null && tneed.Met == g.Log.Exists(x => x.Code == "start") && HudToolkit.BookLaidOut.Has(BookSlot.Needs); h.BookClose(); nw["front-door"].RemoveAll(x => x.Id == "test-need"); yield return Frames(1);
            Row("bookNeeds", nbad == 0 && vocab && testOk && npts >= 10 && nmet.Count >= 5, $"{npts} points on route A and the pickup route, {nbad} wrong; vocabulary {vocab}; test condition {testOk}; ticked at some point: {string.Join(", ", nmet.Keys)} {string.Join("; ", nsaid2)}");
            UnityEngine.Debug.Log("ProofBook: what is still a way (the job's): closes the book, "); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- what is still a way (the job's): closes the book, opens that thing's ring for the selection as it is, gives no order; the ring is the ring a click on that thing opens; the page shows no price of its own
            yield return Fresh(h); g = h.Game; h.SetPaused(true); yield return Frames(2); int bad = 0, seen = 0; var said = new List<string>(); h.Select(OpId.Scout, false);
            h.BookOpenNow("job"); yield return Frames(2); int nWays = h.BookPageNow.Ways.Count; h.BookClose(); yield return Frames(1);
            for (int w = 0; w < nWays; w++)
            {
                h.BookOpenNow("job"); yield return Frames(1); var way = h.BookPageNow.Ways[w]; int ord0 = g.Orders.Count, sel0 = (int)h.Agent; h.BookTakeWay(way); yield return Frames(1); seen++;
                var ring = h.RingWords; var t = h.Ring?.Plan?.T; string same = ""; if (t != null) { var again = h.ContextFor(t, h.Ring.Screen); same = again.Rep.Count == h.Ring.Plan.Rep.Count ? "" : "ring differs"; }
                if (h.BookOpen || g.Orders.Count != ord0 || t?.Key != way.Target?.Key || (int)h.Agent != sel0 || same.Length > 0 || way.SubText.Contains("$")) { bad++; said.Add($"{way.Word}: ring {ring} on {t?.Key}, {same}"); } else said.Add($"{way.Word} ({way.SubText}): {ring}");
                h.CloseRing(); yield return Frames(1);
            }
            Row("bookWayIsTheRing", bad == 0 && seen == 2, $"{seen} ways on the job page, {bad} wrong; {string.Join("; ", said)}");
            UnityEngine.Debug.Log("ProofBook: no cost while shut: the hook's own cost, nothing i"); // where the proof is, if it throws (the coroutine dies silently and the proof waits out its time)
            // ---- no cost while shut: the hook's own cost, nothing in the panel's tree, and the frames either side of an open
            int e0 = HudToolkit.PanelElementCount; h.BookOpenNow("cell"); yield return Frames(3); int eUp = HudToolkit.PanelElementCount; double build = h.BookBuildMs; h.BookClose(); yield return Frames(3); int e1 = HudToolkit.PanelElementCount; var (msEach, bytes) = h.BookShutProbe(10000);
            Row("bookShutCost", e1 == e0 && eUp > e0 && bytes == 0 && msEach < 0.01, $"panel elements {e0} shut, {eUp} up, {e1} shut again; the shut check {msEach * 1000:0.##} us a frame, {bytes} bytes over 10000; a page builds in {build:0.#} ms (at open, never per frame)");
            h.SetPaused(false);
        }
    }

    /// <summary>`-bookShots &lt;dir&gt; -capture-size 1920x1080`: the real captures for Sky's phone: the job page at a real 2:40, the Scout and the Enforcer, a test section. Its own flag; no proof section runs it.</summary>
    public sealed class BookShots : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (GameBoot.ArgValue("-bookShots") is string d) { Application.runInBackground = true; var go = new GameObject("book-shots"); DontDestroyOnLoad(go); go.AddComponent<BookShots>().Dir = d; } }
        string Dir = "";
        IEnumerator Shot(string name) { yield return new WaitForSecondsRealtime(0.9f); yield return new WaitForEndOfFrame(); var t = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(Dir, name + ".png"), t.EncodeToPNG()); Destroy(t); Debug.Log($"book-shots: {name} {Screen.width}x{Screen.height}"); }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir); InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            HandoverHost h; float t0 = Time.realtimeSinceStartup; while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 90f) { Application.Quit(3); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f); /* the size is -capture-size's */
            h.controls.conversations = ConversationScenes.Never; h.controls.planTogether = false; h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
            var g = h.Game; var lines = new List<string>(); int stop = 1600; // 2:40
            void Say(string n) { var p = h.BookPageNow; lines.Add($"{n}: {p?.Name} | {p?.Sub} | clock {HandoverHost.MissionClock(g.Tick)} | facts {string.Join(", ", (p?.Facts ?? new List<BookFact>()).ConvertAll(f => f.Word))} | weapon {p?.Weapon?.Word} {p?.Weapon?.Note} {string.Join(" ", (p?.Weapon?.Stats ?? new List<(string, string, int, int, float)>()).ConvertAll(x => $"{x.Item2} {x.Item3}/{x.Item4}"))} | ways {string.Join(", ", (p?.Ways ?? new List<BookWay>()).ConvertAll(w => (w.Shut ? "~" : "") + w.Word + " [" + w.SubText + "]"))} | can {string.Join(", ", (p?.Can ?? new List<BookCan>()).ConvertAll(c => c.Word + " [" + c.Line + "]"))} | needs {string.Join(", ", (p?.Needs ?? new List<BookNeed>()).ConvertAll(n => (n.Met ? "[x] " : n.Unknown ? "[?] " : "[ ] ") + n.Words))} | track {string.Join(", ", (p?.Track ?? new List<BookStop>()).ConvertAll(s => s.Word + " " + s.Time))} | build {h.BookBuildMs:0.#} ms"); File.WriteAllLines(Path.Combine(Dir, "pages.txt"), lines); }
            bool enforcerShot = false, jobShot = false; foreach (var o in ProofBook.Route()) { if (o.Tick >= stop) break; if (!jobShot && o.Tick >= 50) { h.RunTo(50); jobShot = true; h.BookOpenNow("job"); yield return Shot("job"); Say("job"); h.BookClose(); yield return null; } if (!enforcerShot && o.Tick >= 500) { h.RunTo(500); enforcerShot = true; h.BookOpenNow("cell", 3); yield return Shot("cell-enforcer"); Say("cell-enforcer"); h.BookShow("cell", 0); yield return Shot("cell-scout"); Say("cell-scout"); h.BookClose(); yield return null; } h.RunTo(o.Tick); h.Issue(new Order { Op = o.Op, Do = o.Do, To = o.To, Point = o.Point, Target = o.Target, Approach = o.Approach, How = o.How, Run = o.Run }); }
            h.RunTo(stop); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.3f); h.SetPaused(true);
            h.BookOpenNow("cell", 1);
            for (int k = 1; k < 3; k++) { h.BookShow("cell", k); string n = "cell-" + Op.Name((OpId)k); yield return Shot(n); Say(n); }
            h.BookClose(); yield return null; h.BookSections.Insert(3, new BookSection("longarm", "fire", "Test", hh => hh.BookCellPage(3, ProofBook.TestWeapon()))); h.BookOpenNow("longarm"); yield return Shot("weapon-test"); Say("weapon-test"); h.BookClose(); h.BookSections.RemoveAt(3); yield return null; h.BookSections.Insert(3, ProofBook.TestSection()); h.BookOpenNow("cash"); yield return Shot("test-section"); Say("test-section"); h.BookClose(); h.BookSections.RemoveAt(3);
            var rest = ProofBook.Route(); foreach (var o in rest) { if (o.Tick < stop) continue; if (g.Log.Exists(x => x.Code == "enter.in") || g.Over) break; h.RunTo(o.Tick); h.Issue(new Order { Op = o.Op, Do = o.Do, To = o.To, Point = o.Point, Target = o.Target, Approach = o.Approach, How = o.How, Run = o.Run }); }
            for (int i = 0; i < 1200 && !g.Log.Exists(x => x.Code == "enter.in") && !g.Over; i++) h.RunTo(g.Tick + 1);
            h.RunTo(g.Tick + 20); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.3f); h.SetPaused(true); h.BookOpenNow("job"); yield return Shot("job-late"); Say("job-late"); h.BookToggleEnds(); yield return Shot("job-ends-open"); Say("job-ends-open"); h.BookToggleEnds(); h.BookClose();
            yield return new WaitForSecondsRealtime(0.5f); yield return new WaitForEndOfFrame(); var t = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(Dir, "street-after.png"), t.EncodeToPNG()); Destroy(t);
            Application.Quit(0);
        }
    }
}
