using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 085 (GDD v0.36.0 section 10, look C): what a conversation taught and opened comes after the cut, in play, on the person, "New" beside the actions it opens, whether a scene played or
    /// not. One at a time, held as long as it takes to read, in real time; a scene that starts meanwhile holds them back. View only.</summary>
    public sealed partial class HandoverHost
    {
        public sealed class PersonCard { public string Id = "", Who = ""; public readonly List<string> Learned = new(), Opens = new(); public float At, Until, Made; }
        /// <summary>The cards to come, the first up once its time arrives.</summary>
        public readonly List<PersonCard> PersonCards = new();
        /// <summary>A card never goes sooner than a done chip does.</summary>
        public const float ConvoCardMinS = DoneChipS;
        public PersonCard PersonCardNow { get { float now = Time.unscaledTime; PersonCards.RemoveAll(x => now >= x.Until); return PersonCards.Count > 0 && now >= PersonCards[0].At ? PersonCards[0] : null; } }
        /// <summary>A card stands on this thing now, or is waiting for its street lines with this chip's act: the plain chip steps aside, so its facts show once.</summary>
        public bool CardOver(string anchor, float chipBorn) { _ = PersonCardNow; foreach (var c in PersonCards) if (c.Who == anchor && (Time.unscaledTime >= c.At || chipBorn >= c.Made - 1f)) return true; return false; }
        public static float CardReadS(PersonCard c) { float s = 0f; foreach (var l in c.Learned) s += ReadS(l); foreach (var l in c.Opens) s += ReadS(l); return Mathf.Max(ConvoCardMinS, s); }

        void ConvoCardsAfter(Convo c, float at)
        {
            var p = new PersonCard { Id = c.Id, Who = c.Target, Made = Time.unscaledTime }; p.Learned.AddRange(c.Learned); p.Opens.AddRange(c.Opens);
            p.At = PersonCards.Count > 0 ? Mathf.Max(at, PersonCards[^1].Until) : at; p.Until = p.At + CardReadS(p); PersonCards.Add(p);
        }
        void ConvoCardsWait() { float dt = Time.unscaledDeltaTime; foreach (var p in PersonCards) { p.At += dt; p.Until += dt; } }
        /// <summary>Brief 101 round 3 (Glyph's slip): a card whose person is off screen waits, its read time starting when she is in view; the cards after it wait with it.</summary>
        public void PersonCardHold(PersonCard c, float dt)
        {
            int i = PersonCards.IndexOf(c); if (i < 0 || dt <= 0f) return; c.Until += dt;
            for (int k = i + 1; k < PersonCards.Count; k++) { PersonCards[k].At += dt; PersonCards[k].Until += dt; }
        }

        /// <summary>What the scene's people said before the cut isn't left to show after it (the mock's "Yes. Yes. And thank you." over Inez, beside her card).</summary>
        void ConvoSilence(Convo c)
        {
            var said = new HashSet<string> { Suborned.Sim.Op.Name((Suborned.Sim.OpId)c.Agent), c.Target }; foreach (var (sp, _) in c.Lines) said.Add(sp);
            Bubbles.RemoveAll(b => said.Contains(b.Speaker)); Callouts.RemoveAll(o => o.Speech != null && said.Contains(o.Speech.Speaker)); CalloutQueue.RemoveAll(o => o.Speech != null && said.Contains(o.Speech.Speaker));
        }
    }
}
