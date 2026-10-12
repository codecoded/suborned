using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Minimal JSON reader for the world export: objects → Dictionary, arrays → List, numbers → double.
    /// JsonUtility cannot read nested arrays (points are [x, y]), and this keeps the builder dependency-free.</summary>
    public static class Json
    {
        public static object Parse(string text)
        {
            int i = 0;
            var value = Value(text, ref i);
            Skip(text, ref i);
            if (i != text.Length) throw new FormatException($"JSON: trailing data at {i}");
            return value;
        }

        static void Skip(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Value(string s, ref int i)
        {
            Skip(s, ref i);
            if (i >= s.Length) throw new FormatException("JSON: unexpected end");
            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return Str(s, ref i);
            if (Lit(s, ref i, "true")) return true;
            if (Lit(s, ref i, "false")) return false;
            if (Lit(s, ref i, "null")) return null;
            return Num(s, ref i);
        }

        static bool Lit(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) return false;
            i += word.Length; return true;
        }

        static Dictionary<string, object> Obj(string s, ref int i)
        {
            var d = new Dictionary<string, object>(); i++;
            Skip(s, ref i);
            if (s[i] == '}') { i++; return d; }
            while (true)
            {
                Skip(s, ref i);
                var key = Str(s, ref i);
                Skip(s, ref i);
                if (s[i++] != ':') throw new FormatException($"JSON: ':' expected at {i - 1}");
                d[key] = Value(s, ref i);
                Skip(s, ref i);
                char c = s[i++];
                if (c == '}') return d;
                if (c != ',') throw new FormatException($"JSON: ',' or '}}' expected at {i - 1}");
            }
        }

        static List<object> Arr(string s, ref int i)
        {
            var a = new List<object>(); i++;
            Skip(s, ref i);
            if (s[i] == ']') { i++; return a; }
            while (true)
            {
                a.Add(Value(s, ref i));
                Skip(s, ref i);
                char c = s[i++];
                if (c == ']') return a;
                if (c != ',') throw new FormatException($"JSON: ',' or ']' expected at {i - 1}");
            }
        }

        static string Str(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException($"JSON: string expected at {i}");
            var sb = new StringBuilder(); i++;
            while (s[i] != '"')
            {
                char c = s[i++];
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                    default: sb.Append(e); break;
                }
            }
            i++;
            return sb.ToString();
        }

        static double Num(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (start == i) throw new FormatException($"JSON: value expected at {i}");
            return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Typed access to parsed JSON nodes in the plan frame (points are [x, y] metres).</summary>
    public static class J
    {
        public static Dictionary<string, object> O(object o) => (Dictionary<string, object>)o;
        public static List<object> A(object o) => (List<object>)o;
        public static List<object> A(object o, string key) => (List<object>)O(o)[key];
        public static object Get(object o, string key) => O(o).TryGetValue(key, out var v) ? v : null;
        public static string S(object o, string key) => Get(o, key) as string;
        public static float F(object o, string key) => O(o).TryGetValue(key, out var v) && v is double d ? (float)d
            : throw new FormatException($"World: number `{key}` missing in {S(o, "id") ?? "export"}");
        public static bool B(object o, string key) => Get(o, key) is bool b && b;
        public static Vector2 P(object o) { var a = A(o); return new Vector2((float)(double)a[0], (float)(double)a[1]); }
        public static Vector2 P(object o, string key) => P(O(o)[key]);

        public static Vector2[] Poly(object o)
        {
            var a = A(o); var r = new Vector2[a.Count];
            for (int i = 0; i < a.Count; i++) r[i] = P(a[i]);
            return r;
        }
        public static Vector2[] Poly(object o, string key) => Poly(O(o)[key]);

        public static List<Vector2[]> Polys(object o)
        {
            var r = new List<Vector2[]>();
            foreach (var p in A(o)) r.Add(Poly(p));
            return r;
        }
        public static List<Vector2[]> Polys(object o, string key) => Polys(O(o)[key]);
    }
}
