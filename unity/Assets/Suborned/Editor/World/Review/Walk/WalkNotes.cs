using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>One sign-off comment: where the reviewer was, what they were on and looking at, and what they wrote.</summary>
    public struct WalkNote
    {
        public string Mode, Lens, Text;
        /// <summary>Camera position and the point the mode stands on (Unity frame: x east, y up, z north).</summary>
        public Vector3 Position, Ground;
        /// <summary>Compass yaw (90 = east) and pitch in degrees below horizontal, as views.json.</summary>
        public float Yaw, Pitch;
        public PlanIds Feet;
        /// <summary>What the crosshair or cursor was on; null = sky.</summary>
        public Vector3? LookAt;
        public PlanIds Look;
    }

    /// <summary>Appends review notes to &lt;repo&gt;/out/review/walk-notes.json (one JSON array, newest last) and writes each note's
    /// screenshot to out/shots/walk/. The file is how sign-off comments reach the crew, so an existing file that is not a JSON
    /// array is set aside, never overwritten.</summary>
    public sealed class WalkNotes
    {
        readonly string repo, export, scene, file;
        public string NotesFile => Path.Combine(repo, "out", "review", "walk-notes.json");
        string Shots => Path.Combine(repo, "out", "shots", "walk");
        /// <summary>Notes in the file after the last Add.</summary>
        public int Count { get; private set; }

        /// <summary>export: the export's source line. scene: the SHA-256 of the export the scene was built from (its WorldStamp; null
        /// = an unstamped scene), written into every note as `exportSha256`, so a comment is traced to the build it was made in.
        /// file: the SHA-256 of the export file the ids were read from, written as `idsFromExportSha256` only where it differs.</summary>
        public WalkNotes(string repo, string export, string scene, string file)
        {
            this.repo = repo; this.export = export; this.scene = scene; this.file = file;
            Count = Existing(out _);
        }

        /// <summary>Writes the screenshot and appends the note; returns the screenshot's repo-relative path.</summary>
        public string Add(WalkNote n, byte[] png)
        {
            var now = DateTimeOffset.Now;
            int have = Existing(out string inner);
            Directory.CreateDirectory(Shots);
            string shot = $"walk-{now:yyyyMMdd-HHmmss}-{have + 1:D3}.png";
            File.WriteAllBytes(Path.Combine(Shots, shot), png);
            string rel = "out/shots/walk/" + shot;

            var sb = new StringBuilder("  {\n");
            sb.Append($"    \"time\": {S(now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture))},\n");
            sb.Append($"    \"export\": {S(export)},\n");
            sb.Append($"    \"exportSha256\": {S(scene)},\n");
            if (file != scene) sb.Append($"    \"idsFromExportSha256\": {S(file)},\n");
            sb.Append($"    \"mode\": {S(n.Mode)},\n");
            sb.Append($"    \"position\": {V(n.Position)},\n");
            sb.Append($"    \"plan\": {{\"x\": {F(n.Position.x)}, \"y\": {F(n.Position.z)}, \"height\": {F(n.Position.y)}}},\n");
            sb.Append($"    \"yaw\": {F(n.Yaw)}, \"pitch\": {F(n.Pitch)}, \"lens\": {S(n.Lens)},\n");
            sb.Append($"    \"ground\": {V(n.Ground)},\n");
            sb.Append($"    \"ids\": {Ids(n.Feet)},\n");
            sb.Append("    \"look\": ").Append(n.LookAt is { } at ? "{\"at\": " + V(at) + ", \"ids\": " + Ids(n.Look) + "}" : "null").Append(",\n");
            sb.Append($"    \"text\": {S(n.Text)},\n");
            sb.Append($"    \"screenshot\": {S(rel)}\n  }}");

            Directory.CreateDirectory(Path.GetDirectoryName(NotesFile));
            File.WriteAllText(NotesFile, "[\n" + (inner.Length > 0 ? inner + ",\n" : "") + sb + "\n]\n");
            Count = have + 1;
            return rel;
        }

        /// <summary>Notes already in the file and the array's contents; a file that is not a JSON array is moved aside first.</summary>
        int Existing(out string inner)
        {
            inner = "";
            if (!File.Exists(NotesFile)) return 0;
            string text = File.ReadAllText(NotesFile).Trim();
            try
            {
                if (Json.Parse(text) is List<object> notes)
                {
                    inner = text.Substring(1, text.Length - 2).Trim('\n', '\r', ' ', '\t');
                    return notes.Count;
                }
            }
            catch (Exception) { } // any way of not being a JSON array: set it aside
            string aside = Path.ChangeExtension(NotesFile, $".unreadable-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Move(NotesFile, aside);
            Debug.LogWarning($"World: walk review: {NotesFile} was not a JSON array; kept as {aside}, starting a new file");
            return 0;
        }

        static string Ids(PlanIds i) => $"{{\"area\": {S(i.Area)}, \"plot\": {S(i.Plot)}, \"road\": {S(i.Road)}, \"on\": {S(i.On)}}}";

        static string V(Vector3 v) => $"{{\"x\": {F(v.x)}, \"y\": {F(v.y)}, \"z\": {F(v.z)}}}";

        static string F(float f) => f.ToString("0.##", CultureInfo.InvariantCulture);

        static string S(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder("\"");
            foreach (char c in s)
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c); break;
                }
            return sb.Append('"').ToString();
        }
    }
}
