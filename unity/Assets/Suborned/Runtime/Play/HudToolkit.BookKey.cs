using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The Book as an enamel roundel, bottom right (brief 101; Sky 2026-11-08: "tokens bottom right"; Glyph's T mocks): the book's deep teal with a cream
    /// inline, the papers pictogram (the book's own Journal icon) in cream, and the turquoise "new" dot on its rim while the Journal has rows not yet shown. A click
    /// opens the book (on the Journal when something is new); J stays (brief 096: Sky, "we need an icon for the book - I hate keyboard shortcuts"). Hovered, the
    /// inline lights turquoise. It retires the plate_dark key and its word.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement bookKey, bookKeyDot, bookKeyInline;
        const float BookD = 56f;

        void BuildBookKey()
        {
            (bookKey, bookKeyInline) = Roundel(root, BookD, "e-deep", 1.6f, 4f, pick: true); bookKey.name = "book-key";
            bookKey.style.position = Position.Absolute; bookKey.style.right = 20; bookKey.style.bottom = 24;
            var g = E(bookKey, null, w: 28, h: 28); g.style.flexShrink = 0; Icon(g, "papers", Cream);
            bookKeyDot = E(bookKey, null, 42f - 0.5f, -2f, 15, 15); Round(bookKeyDot, 7.5f); BorderW(bookKeyDot, 2f); bookKeyDot.AddToClassList("e-turq"); bookKeyDot.AddToClassList("e-inline");
            bookKey.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.BookOpenNow(host.JournalUnread > 0 ? "journal" : null); e.StopPropagation(); } });
            bookKey.RegisterCallback<PointerEnterEvent>(_ => RingColour(bookKeyInline, TurqLight)); bookKey.RegisterCallback<PointerLeaveEvent>(_ => RingColour(bookKeyInline, null));
            manifest.Add((bookKey, "image", () => "book key"));
        }

        void BookKeyFrame(bool on) { Show(bookKey, on); if (on) Show(bookKeyDot, host.JournalUnread > 0); }
    }
}
