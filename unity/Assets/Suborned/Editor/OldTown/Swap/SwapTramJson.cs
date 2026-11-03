#if UNITY_EDITOR
using System;using System.Collections;using System.Text;using System.Globalization;
namespace Suborned.OldTown.Editor.Swap { internal static class SwapTramJson {
        internal static string Encode(object o) { var sb = new StringBuilder(); W(sb, o, 0); return sb.ToString(); }
        static void W(StringBuilder sb, object o, int depth)
        {
            switch (o)
            {
                case null: sb.Append("null"); break;
                case string s: sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")).Append('"'); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case float f: sb.Append(float.IsFinite(f) ? f.ToString("R", CultureInfo.InvariantCulture) : "null"); break;
                case double d: sb.Append(double.IsFinite(d) ? d.ToString("R", CultureInfo.InvariantCulture) : "null"); break;
                case int i: sb.Append(i); break;
                case long l: sb.Append(l); break;
                case IDictionary dict:
                    sb.Append('{'); bool first = true;
                    foreach (DictionaryEntry e in dict) { if (!first) sb.Append(','); first = false; if (depth < 1) sb.Append("\n "); W(sb, e.Key.ToString(), depth + 1); sb.Append(':'); W(sb, e.Value, depth + 1); }
                    if (depth < 1) sb.Append('\n'); sb.Append('}'); break;
                case IEnumerable en:
                    sb.Append('['); bool f2 = true;
                    foreach (var x in en) { if (!f2) sb.Append(','); f2 = false; if (depth < 2 && x is IDictionary) sb.Append("\n "); W(sb, x, depth + 1); }
                    sb.Append(']'); break;
                default: sb.Append('"').Append(o).Append('"'); break;
            }
        }
}}
#endif
