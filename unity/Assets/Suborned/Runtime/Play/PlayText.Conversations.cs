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
        public sealed class ConvLine { public string Speaker = "", Line = ""; }
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

        void LoadConversations(JNode n)
        {
            var sc = n.Opt("scenes");
            if (sc?.Opt("clip") is JNode cl) { ClipService = S(cl, "service") ?? ClipService; ClipPublic = S(cl, "public") ?? ClipPublic; ClipBoth = S(cl, "both") ?? ClipBoth; }
            if (n.Opt("less")?.Opt("price")?.Opt("fields")?.Opt("door") is JNode df) DoorField = S(df, "text") ?? DoorField;
            if (sc?.Opt("conversations") is not JNode cv) return;
            if (cv.Opt("names") is JNode nm) foreach (var kv in nm.Entries) ConvNames[kv.Key] = kv.Value.Str();
            foreach (var it in cv["items"].Items)
            {
                var w = it["when"];
                var c = new Conv { Id = S(it, "id") ?? "", Title = S(it, "title") ?? "", Verb = S(w, "verb") ?? "", About = S(w, "about") ?? "", Temperament = S(w, "temperament") ?? "", Outcome = S(w, "outcome"), TurnedBy = S(w, "turnedBy"), Turned = Flag(w, "turned"), Helped = Flag(w, "helped") };
                foreach (var b in it["beats"].Items) c.Beats.Add(new ConvLine { Speaker = S(b, "speaker") ?? "", Line = S(b, "line") ?? "" });
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
