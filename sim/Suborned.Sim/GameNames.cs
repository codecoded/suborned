// Brief 089 (Sky: "All names need to be relevant for this game and this game alone"): the engine's sentences name the cast from the data,
// never from a literal, so a rename is data only. The name is the site cast's (sim/data/handover.site.json cast.<role>.name, written by
// sim:site from the people file); how a sentence calls the person is the cast's `called` when the site gives one, else the custom the
// mission's words use: the waitress by her given name ("Inez"), everyone else by the family name ("Popovic"). Words only: no rule
// reads them and the checkpoint hash never does (Hash.State hashes a log line's tick and code, a refusal's code, never the words).
using System;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>How the engine's sentences call the cast member in a role (waitress, driver, guard, councillor): the cast's `called`,
        /// else the given name for the waitress and the family name for the rest; the role itself ("the councillor") if the cast has no name.</summary>
        public string Called(string role)
        {
            CastRole? c = null; foreach (var x in S.Cast) if (x.Role == role) { c = x; break; }
            if (c == null || c.Name.Trim().Length == 0) return "the " + role;
            if (c.Called.Length > 0) return c.Called;
            var parts = c.Name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return role == "waitress" ? parts[0] : parts[parts.Length - 1];
        }
        /// <summary>The councillor as the sentences call him (Popovic; Vale before brief 089).</summary>
        public string ValeName => Called("councillor");
        /// <summary>The waitress as the sentences call her (Inez).</summary>
        public string InezName => Called("waitress");
        /// <summary>A name or a role at the start of a sentence: "the councillor" becomes "The councillor"; a name is unchanged.</summary>
        public static string Cap(string s) => s.Length > 0 && char.IsLower(s[0]) ? char.ToUpperInvariant(s[0]) + s.Substring(1) : s;
        /// <summary>The possessive: "Popovic's".</summary>
        public static string Poss(string s) => s + "'s";
    }
}
