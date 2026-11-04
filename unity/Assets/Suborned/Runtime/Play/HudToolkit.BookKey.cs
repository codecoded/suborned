using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 096 (Sky 2026-11-04: "we need an icon for the book - I hate keyboard shortcuts"; Glyph's 2-hud-book.png and 2c, Sky's calls 3 and 4): the Book key,
    /// bottom right in the notes key's place: the kit's plate_dark, an open-book glyph and the word, and the turquoise "new" dot while the Journal has rows not yet
    /// shown. A click opens the book (on the Journal when something is new); J stays. The glyph is Glyph's stand-in (briefs/096/glyph/make/bookglyph.svg) drawn as
    /// vectors until it is drawn into the kit's icons/singles.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement bookKey, bookKeyDot; Label bookKeyWord;

        void BuildBookKey()
        {
            bookKey = E(root, "s-plate_dark", pick: true); bookKey.style.position = Position.Absolute; bookKey.style.right = 14; bookKey.style.bottom = 14; bookKey.style.height = 40;
            bookKey.style.flexDirection = FlexDirection.Row; bookKey.style.alignItems = Align.Center; bookKey.style.paddingLeft = 12; bookKey.style.paddingRight = 14; bookKey.name = "book-key";
            var g = P(bookKey, 0, 0, 22, 22, BookGlyphPaint); g.style.position = Position.Relative; g.style.left = StyleKeyword.Auto; g.style.top = StyleKeyword.Auto; g.style.flexShrink = 0;
            bookKeyWord = Words(bookKey, 15f); bookKeyWord.style.marginLeft = 8; Text(bookKeyWord, BookWordsFile.Get().KeyWord);
            bookKeyDot = E(bookKey, null, w: 9, h: 9); Round(bookKeyDot, 4.5f); bookKeyDot.style.backgroundColor = TurqLight; bookKeyDot.style.marginLeft = 8; bookKeyDot.style.flexShrink = 0;
            bookKey.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.BookOpenNow(host.JournalUnread > 0 ? "journal" : null); e.StopPropagation(); } });
            bookKey.RegisterCallback<PointerEnterEvent>(_ => bookKey.style.unityBackgroundImageTintColor = new Color(0.75f, 1f, 1f)); bookKey.RegisterCallback<PointerLeaveEvent>(_ => bookKey.style.unityBackgroundImageTintColor = Color.white);
            manifest.Add((bookKey, "image", () => "book key"));
        }

        void BookKeyFrame(bool on) { Show(bookKey, on); if (on) Show(bookKeyDot, host.JournalUnread > 0); }

        /// <summary>The open book (Glyph's stand-in, a 64 grid scaled to the box): two pages and their two top strips, cream.</summary>
        static void BookGlyphPaint(Painter2D p, Rect r)
        {
            float k = Mathf.Min(r.width, r.height) / 64f; Vector2 V(float x, float y) => new(r.x + x * k, r.y + y * k);
            p.fillColor = Cream;
            for (int side = 0; side < 2; side++)
            {
                float X(float x) => side == 0 ? x : 64f - x;
                p.BeginPath(); p.MoveTo(V(X(4f), 15.5f)); p.BezierCurveTo(V(X(13.5f), 11.9f), V(X(23f), 12.7f), V(X(30f), 17.9f)); p.LineTo(V(X(30f), 54f));
                p.BezierCurveTo(V(X(23f), 49.4f), V(X(13.5f), 48.8f), V(X(4f), 51.8f)); p.ClosePath(); p.Fill();
                p.BeginPath(); p.MoveTo(V(X(8f), 11f)); p.BezierCurveTo(V(X(16f), 8.6f), V(X(23.5f), 9.4f), V(X(30f), 13.2f)); p.LineTo(V(X(30f), 15.8f));
                p.BezierCurveTo(V(X(23.6f), 12.4f), V(X(16f), 11.8f), V(X(8f), 14f)); p.ClosePath(); p.Fill();
            }
        }
    }
}
