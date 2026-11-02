using System; using System.Collections;
using Suborned.Play; using Suborned.Sim; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.InputSystem.LowLevel;

/// <summary>Brief 088, film for Sky's phone: the book in play, real time, real keys and mouse through the Input System's devices: open it, move between sections and agents, take a way (the ring, no order), back to the street.</summary>
public sealed partial class Rec085
{
    static void BKey(params Key[] k) { if (Keyboard.current is Keyboard kb) InputSystem.QueueStateEvent(kb, new KeyboardState(k)); }
    IEnumerator BPress(Key k) { BKey(k); yield return null; yield return null; BKey(); yield return null; yield return null; }
    IEnumerator BMove(Vector2 to, float s = 0.55f) { var from = dotAt; dotOn = true; float t0 = Time.unscaledTime; while (Time.unscaledTime - t0 < s) { float u = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - t0) / s); dotAt = Vector2.Lerp(from, to, u); Mouse2(dotAt); yield return null; } dotAt = to; Mouse2(to); yield return null; }
    IEnumerator BClick(string name, int i, Func<bool> did, Action fallback)
    {
        if (HudToolkit.BookElementScreen(name, i) is not Vector2 p) { Note($"no {name} {i}"); fallback?.Invoke(); yield break; }
        yield return BMove(p); dotDown = true; Mouse2(p, true); yield return new WaitForSecondsRealtime(0.09f); dotDown = false; Mouse2(p); yield return null; yield return null; yield return null;
        if (did != null && !did()) { Note($"{name} {i}: the click didn't land; called directly"); fallback?.Invoke(); } else Note($"{name} {i}: clicked");
    }
    IEnumerator Book()
    {
        dotAt = new Vector2(Screen.width * .7f, Screen.height * .5f);
        yield return Fresh("route-a"); yield return Jump(() => G.Tick >= 1000); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.5f);
        RecOn(); yield return new WaitForSecondsRealtime(2.2f); int t0 = G.Tick; Note($"J at tick {t0}"); yield return BPress(Key.J); Note($"book {h.BookOpen}, paused {h.Paused}, section {h.BookSectionNow}");
        yield return new WaitForSecondsRealtime(2.4f);
        yield return BClick("book-sign-job", 0, () => h.BookSectionNow == "job", () => h.BookShow("job")); yield return new WaitForSecondsRealtime(3.2f);
        yield return BClick("book-sign-cell", 0, () => h.BookSectionNow == "cell", () => h.BookShow("cell")); yield return new WaitForSecondsRealtime(1.2f);
        int target = (int)OpId.Enforcer; int idx = 0; foreach (var o in h.BookPageNow.Others) { if (o.Agent == target) break; idx++; }
        yield return BClick("book-other", idx, () => h.BookAgent == target, () => h.BookShow("cell", target)); yield return new WaitForSecondsRealtime(2.4f);
        idx = 0; foreach (var o in h.BookPageNow.Others) { if (o.Agent == (int)OpId.Scout) break; idx++; }
        yield return BClick("book-other", idx, () => h.BookAgent == 0, () => h.BookShow("cell", 0)); yield return new WaitForSecondsRealtime(1.6f);
        int orders = G.Orders.Count; var way = h.BookPageNow.Ways.Find(w => !w.Shut); int wi = h.BookPageNow.Ways.IndexOf(way);
        yield return BClick("book-way", wi, () => !h.BookOpen, () => h.BookTakeWay(way)); Note($"way {way?.Word}: ring {h.RingWords}, orders added {G.Orders.Count - orders}, paused {h.Paused}");
        yield return new WaitForSecondsRealtime(2.6f); yield return BPress(Key.Escape); Note($"ring closed {h.Ring == null}, tick {G.Tick}"); yield return new WaitForSecondsRealtime(2.4f); yield return BPress(Key.J); yield return new WaitForSecondsRealtime(1.4f); yield return BClick("book-back", 0, () => !h.BookOpen, () => h.BookClose()); yield return new WaitForSecondsRealtime(1.6f); RecOff();
        Note($"end: tick {G.Tick}, book {h.BookOpen}, paused {h.Paused}");
    }
}
