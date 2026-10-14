// A small strict JSON reader (RFC 8259): objects keep their key order, duplicate keys are an error, numbers keep their text.
// The engine reads its rules and site with it so it needs no package (Unity loads the DLL as it is) and so a number is never
// silently turned into a float: Long() refuses anything that is not an integer.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Suborned.Sim
{
    public enum JKind { Obj, Arr, Str, Num, Bool, Null }

    public sealed class JNode
    {
        public readonly JKind Kind;
        readonly List<KeyValuePair<string, JNode>>? obj;
        readonly Dictionary<string, JNode>? index;
        readonly List<JNode>? arr;
        readonly string? text; // string value, or a number's literal
        readonly bool flag;
        public readonly string Path;

        JNode(JKind kind, string path, List<KeyValuePair<string, JNode>>? o = null, List<JNode>? a = null, string? t = null, bool b = false)
        {
            Kind = kind; Path = path; obj = o; arr = a; text = t; flag = b;
            if (o != null) { index = new Dictionary<string, JNode>(StringComparer.Ordinal); foreach (var kv in o) index[kv.Key] = kv.Value; }
        }

        public static JNode Parse(string s, string name = "$") { var p = new Parser(Canon(s), name); var n = p.Value(name); p.End(); return n; }
        /// <summary>The text as the engine reads and hashes it: a leading BOM dropped, CRLF and CR made LF (Redline E4, E11).</summary>
        public static string Canon(string s) { if (s.Length > 0 && s[0] == '\uFEFF') s = s.Substring(1); return s.IndexOf('\r') < 0 ? s : s.Replace("\r\n", "\n").Replace('\r', '\n'); }

        public bool Has(string key) => Kind == JKind.Obj && index!.ContainsKey(key);
        public JNode this[string key]
        {
            get
            {
                if (Kind != JKind.Obj) throw new FormatException($"{Path} is not an object");
                if (!index!.TryGetValue(key, out var v)) throw new FormatException($"{Path} has no key '{key}'");
                return v;
            }
        }
        public JNode? Opt(string key) => Kind == JKind.Obj && index!.TryGetValue(key, out var v) ? v : null;
        public IReadOnlyList<KeyValuePair<string, JNode>> Entries => Kind == JKind.Obj ? obj! : throw new FormatException($"{Path} is not an object");
        public IEnumerable<string> Keys { get { foreach (var kv in Entries) yield return kv.Key; } }
        public IReadOnlyList<JNode> Items => Kind == JKind.Arr ? arr! : throw new FormatException($"{Path} is not an array");
        public string Str() => Kind == JKind.Str ? text! : throw new FormatException($"{Path} is not a string");
        public bool Bool() => Kind == JKind.Bool ? flag : throw new FormatException($"{Path} is not a boolean");
        public bool IsNull => Kind == JKind.Null;
        public long Long()
        {
            if (Kind != JKind.Num) throw new FormatException($"{Path} is not a number");
            if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v)) throw new FormatException($"{Path} is {text}, not an integer (integer state only)");
            return v;
        }
        public int Int() { var v = Long(); if (v < int.MinValue || v > int.MaxValue) throw new FormatException($"{Path} {v} is out of range"); return (int)v; }
        public string NumberText => Kind == JKind.Num ? text! : throw new FormatException($"{Path} is not a number");
        public List<string> Strs() { var o = new List<string>(); foreach (var x in Items) o.Add(x.Str()); return o; }
        public int[] Ints() { var o = new int[Items.Count]; for (int i = 0; i < o.Length; i++) o[i] = Items[i].Int(); return o; }

        /// <summary>Reject any key not in the allowed set (unknown = reject).</summary>
        public void Only(params string[] allowed)
        {
            foreach (var kv in Entries)
                if (Array.IndexOf(allowed, kv.Key) < 0) throw new FormatException($"{Path} has unknown key '{kv.Key}' (unknown = reject)");
        }

        sealed class Parser
        {
            readonly string s; int i;
            public Parser(string text, string name) { s = text; i = 0; }
            void Ws() { while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++; }
            Exception Err(string m) => new FormatException($"JSON at {i}: {m}");
            public void End() { Ws(); if (i != s.Length) throw Err("trailing text"); }
            public JNode Value(string path)
            {
                Ws();
                if (i >= s.Length) throw Err("unexpected end");
                char c = s[i];
                if (c == '{')
                {
                    i++; var list = new List<KeyValuePair<string, JNode>>(); var seen = new HashSet<string>(StringComparer.Ordinal);
                    Ws(); if (i < s.Length && s[i] == '}') { i++; return new JNode(JKind.Obj, path, o: list); }
                    while (true)
                    {
                        Ws(); var k = Str(); if (!seen.Add(k)) throw Err($"duplicate key '{k}'");
                        Ws(); if (i >= s.Length || s[i] != ':') throw Err("':' expected"); i++;
                        list.Add(new KeyValuePair<string, JNode>(k, Value(path + "." + k)));
                        Ws(); if (i < s.Length && s[i] == ',') { i++; continue; }
                        if (i < s.Length && s[i] == '}') { i++; return new JNode(JKind.Obj, path, o: list); }
                        throw Err("',' or '}' expected");
                    }
                }
                if (c == '[')
                {
                    i++; var list = new List<JNode>();
                    Ws(); if (i < s.Length && s[i] == ']') { i++; return new JNode(JKind.Arr, path, a: list); }
                    while (true)
                    {
                        list.Add(Value(path + "[" + list.Count + "]"));
                        Ws(); if (i < s.Length && s[i] == ',') { i++; continue; }
                        if (i < s.Length && s[i] == ']') { i++; return new JNode(JKind.Arr, path, a: list); }
                        throw Err("',' or ']' expected");
                    }
                }
                if (c == '"') return new JNode(JKind.Str, path, t: Str());
                if (c == 't' && s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return new JNode(JKind.Bool, path, b: true); }
                if (c == 'f' && s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return new JNode(JKind.Bool, path, b: false); }
                if (c == 'n' && s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return new JNode(JKind.Null, path); }
                if (c == '-' || (c >= '0' && c <= '9'))
                {
                    int st = i; if (s[i] == '-') i++;
                    if (i < s.Length && s[i] == '0' && i + 1 < s.Length && s[i + 1] >= '0' && s[i + 1] <= '9') throw Err("a number with a leading zero");
                    if (i >= s.Length || s[i] < '0' || s[i] > '9') throw Err("a digit expected");
                    while (i < s.Length && ((s[i] >= '0' && s[i] <= '9') || s[i] == '.' || s[i] == 'e' || s[i] == 'E' || s[i] == '+' || s[i] == '-')) i++;
                    return new JNode(JKind.Num, path, t: s.Substring(st, i - st));
                }
                throw Err($"unexpected '{c}'");
            }
            string Str()
            {
                if (s[i] != '"') throw Err("string expected"); i++;
                var b = new StringBuilder();
                while (true)
                {
                    if (i >= s.Length) throw Err("unterminated string");
                    char c = s[i++];
                    if (c == '"') return b.ToString();
                    if (c < 0x20) throw Err("a raw control character in a string");
                    if (c != '\\') { b.Append(c); continue; }
                    if (i >= s.Length) throw Err("unterminated escape");
                    char e = s[i++];
                    switch (e)
                    {
                        case '"': b.Append('"'); break; case '\\': b.Append('\\'); break; case '/': b.Append('/'); break;
                        case 'b': b.Append('\b'); break; case 'f': b.Append('\f'); break; case 'n': b.Append('\n'); break;
                        case 'r': b.Append('\r'); break; case 't': b.Append('\t'); break;
                        case 'u':
                            if (i + 4 > s.Length || !int.TryParse(s.Substring(i, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var u)) throw Err("a bad \\u escape");
                            b.Append((char)u); i += 4; break;
                        default: throw Err($"bad escape \\{e}");
                    }
                }
            }
        }
    }

    /// <summary>A tiny JSON writer for the CLI's output and the order log: ordinal, invariant, no floats.</summary>
    public sealed class JWriter
    {
        readonly StringBuilder b = new StringBuilder();
        readonly Stack<bool> first = new Stack<bool>();
        void Sep() { if (first.Count > 0) { if (!first.Peek()) b.Append(','); first.Pop(); first.Push(false); } }
        public JWriter Obj() { Sep(); b.Append('{'); first.Push(true); return this; }
        public JWriter EndObj() { first.Pop(); b.Append('}'); return this; }
        public JWriter Arr() { Sep(); b.Append('['); first.Push(true); return this; }
        public JWriter EndArr() { first.Pop(); b.Append(']'); return this; }
        public JWriter Key(string k) { Sep(); Quote(k); b.Append(':'); first.Push(true); keyPending = true; return this; }
        bool keyPending;
        void Value(string raw) { if (keyPending) { keyPending = false; first.Pop(); b.Append(raw); return; } Sep(); b.Append(raw); }
        public JWriter Str(string v) { var t = new JWriter(); t.Quote(v); Value(t.b.ToString()); return this; }
        public JWriter Num(long v) { Value(v.ToString(CultureInfo.InvariantCulture)); return this; }
        public JWriter Bool(bool v) { Value(v ? "true" : "false"); return this; }
        public JWriter Null() { Value("null"); return this; }
        public JWriter Raw(string json) { Value(json); return this; }
        public JWriter Prop(string k, string v) => Key(k).Str(v);
        public JWriter Prop(string k, long v) => Key(k).Num(v);
        public JWriter Prop(string k, bool v) => Key(k).Bool(v);
        void Quote(string v)
        {
            b.Append('"');
            foreach (var c in v)
            {
                if (c == '"') b.Append("\\\""); else if (c == '\\') b.Append("\\\\"); else if (c == '\n') b.Append("\\n");
                else if (c < 0x20) b.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture)); else b.Append(c);
            }
            b.Append('"');
        }
        public JWriter Open(string k) { Key(k); keyPending = false; first.Pop(); b.Append('{'); first.Push(true); return this; }
        public JWriter OpenArr(string k) { Key(k); keyPending = false; first.Pop(); b.Append('['); first.Push(true); return this; }
        public override string ToString() => b.ToString();
    }
}
