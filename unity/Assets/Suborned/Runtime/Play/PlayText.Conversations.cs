using System;
using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 085 (Fable's handover-to-latch.md; text.json scenes.conversations, conversations.schema.ts): the whole conversations an act plays when it lands, what the capture clip's plate says
    /// and the Door price field's words. `status` records what Sky has read and gates nothing, so it is never read here. LookupConversation is a port of lookupConversation; no match is null, and the
    /// act plays its scenes.items lines. Nothing here reaches the engine.</summary>
    public sealed partial class PlayText
    {
        /// <summary>A line. Its view-only fields (Fable's note) apply only in the cutaway: street lines and fallback bubbles draw the plain Line, and nothing the engine records reads them.</summary>
        public sealed class ConvLine { public string Speaker = "", Line = "", Lean = "", HoldOn = "", RevealAfter = ""; public float HoldBefore, HoldAfter, RevealS; }
        public sealed class ConvStreet { public string On = "", Beat = ""; public bool Pressed; public readonly List<ConvLine> Beats = new(); }
        public sealed class Conv
        {
            public string Id = "", Title = "", Verb = "", About = "", Temperament = "", Outcome, TurnedBy; public bool? Turned, Helped;
            public readonly List<ConvLine> Beats = new(); public readonly List<ConvStreet> Street = new(); public readonly List<string> Learned = new(), Opens = new();
        }
        public sealed class ConvQuery { public string Verb = "", About = "", Temperament = "", Outcome, TurnedBy; public bool Turned, Helped; }

        public readonly List<Conv> Conversations = new();
        public readonly Dictionary<string, string> ConvNames = new();
        /// <summary>scenes.clip: the capture clip's one word plate.</summary>
        public string ClipService = "Service door", ClipPublic = "Public door", ClipBoth = "Doors held";
        /// <summary>less.price.fields.door.text: "Door {clock}".</summary>
        public string DoorField = "Door {clock}";
        /// <summary>scenes.conversations.ui: the word beside the actions a conversation opens, and what shows for a voice with no figure.</summary>
        public string ConvNew = "New"; public readonly Dictionary<string, string> ConvVoices = new();
        /// <summary>Brief 101 step 6 (Fable's less.street.loud, "Goes loud"): the word the first shot's row leads with; "" when the text has none (never the host's own).</summary>
        public string StreetLoud = "";

        void LoadConversations(JNode n)
        {
            var sc = n.Opt("scenes");
            if (sc?.Opt("clip") is JNode cl) { ClipService = S(cl, "service") ?? ClipService; ClipPublic = S(cl, "public") ?? ClipPublic; ClipBoth = S(cl, "both") ?? ClipBoth; }
            if (n.Opt("less")?.Opt("price")?.Opt("fields")?.Opt("door") is JNode df) DoorField = S(df, "text") ?? DoorField;
            StreetLoud = n.Opt("less")?.Opt("street") is JNode stl ? S(stl, "loud") ?? "" : "";
            if (sc?.Opt("conversations") is not JNode cv) return;
            if (cv.Opt("names") is JNode nm) foreach (var kv in nm.Entries) ConvNames[kv.Key] = kv.Value.Str();
            if (cv.Opt("ui") is JNode ui) { ConvNew = S(ui, "new") ?? ConvNew; if (ui.Opt("voice") is JNode vo) foreach (var kv in vo.Entries) ConvVoices[kv.Key] = kv.Value.Str(); }
            foreach (var it in cv["items"].Items)
            {
                var w = it["when"];
                var c = new Conv { Id = S(it, "id") ?? "", Title = S(it, "title") ?? "", Verb = S(w, "verb") ?? "", About = S(w, "about") ?? "", Temperament = S(w, "temperament") ?? "", Outcome = S(w, "outcome"), TurnedBy = S(w, "turnedBy"), Turned = Flag(w, "turned"), Helped = Flag(w, "helped") };
                foreach (var b in it["beats"].Items) c.Beats.Add(new ConvLine { Speaker = S(b, "speaker") ?? "", Line = S(b, "line") ?? "", Lean = S(b, "lean") ?? "", HoldOn = S(b, "holdOn") ?? "", RevealAfter = S(b, "revealAfter") ?? "", HoldBefore = Secs(b, "holdBefore"), HoldAfter = Secs(b, "holdAfter"), RevealS = Secs(b, "revealS") });
                if (it.Opt("street") is JNode st)
                    foreach (var r in st.Items)
                    {
                        var u = new ConvStreet { On = S(r, "on") ?? "", Beat = S(r, "beat") ?? "", Pressed = Flag(r, "pressed") == true };
                        foreach (var b in r["beats"].Items) u.Beats.Add(new ConvLine { Speaker = S(b, "speaker") ?? "", Line = S(b, "line") ?? "" });
                        c.Street.Add(u);
                    }
                if (it.Opt("learned") is JNode le) c.Learned.AddRange(le.Strs());
                if (it.Opt("opens") is JNode op) c.Opens.AddRange(op.Strs());
                Conversations.Add(c);
            }
        }
        static float Secs(JNode n, string k) => n.Opt(k) is JNode v ? float.Parse(v.NumberText, System.Globalization.CultureInfo.InvariantCulture) : 0f;
        static bool? Flag(JNode n, string k) => n.Opt(k) is JNode v && v.Kind == JKind.Bool ? v.Bool() : (bool?)null;

        /// <summary>The conversation for an act that has landed; every field its `when` names must equal the query's. Null: the act plays its scenes.items scene.</summary>
        public Conv LookupConversation(ConvQuery q)
        {
            foreach (var c in Conversations)
                if (c.Verb == q.Verb && c.About == q.About && c.Temperament == q.Temperament && (c.Outcome == null || c.Outcome == q.Outcome) && (c.Turned == null || c.Turned == q.Turned)
                    && (c.Helped == null || c.Helped == q.Helped) && (c.TurnedBy == null || c.TurnedBy == q.TurnedBy)) return c;
            return null;
        }

        /// <summary>The name a conversation shows for a speaker (Inez, Okafor, Zhou, Milena); everywhere else the game still says Guard and Driver.</summary>
        public string ConvName(string speaker) => ConvNames.TryGetValue(speaker, out var n) ? n : Speaker(speaker);
    }
}
