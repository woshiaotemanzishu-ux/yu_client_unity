using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// Parsed Erlang term value. Mirrors the loosely typed runtime form used by yu_server text protocol.
    /// </summary>
    public sealed class ErlangTerm
    {
        public enum Kind { Atom, Int, Float, String, List, Tuple }

        public Kind Type { get; }
        public object Raw { get; }

        public ErlangTerm(Kind type, object raw) { Type = type; Raw = raw; }

        public bool IsCollection => Type == Kind.List || Type == Kind.Tuple;
        public IReadOnlyList<ErlangTerm> Items => Raw as IReadOnlyList<ErlangTerm>;

        // ---- accessors ----
        public T Get<T>(int index)
        {
            var items = Items;
            if (items == null || index < 0 || index >= items.Count) return default;
            return items[index].As<T>();
        }

        public T As<T>()
        {
            object v = Raw;
            Type target = typeof(T);

            if (target == typeof(string))
            {
                if (v is string s) return (T)(object)s;
                return (T)(object)v?.ToString();
            }
            if (target == typeof(int))   return (T)(object)Convert.ToInt32(v, CultureInfo.InvariantCulture);
            if (target == typeof(long))  return (T)(object)Convert.ToInt64(v, CultureInfo.InvariantCulture);
            if (target == typeof(float)) return (T)(object)Convert.ToSingle(v, CultureInfo.InvariantCulture);
            if (target == typeof(double))return (T)(object)Convert.ToDouble(v, CultureInfo.InvariantCulture);
            if (target == typeof(bool))
            {
                if (v is string sb) return (T)(object)(sb == "true");
                return (T)(object)Convert.ToBoolean(v, CultureInfo.InvariantCulture);
            }
            return (T)v;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case Kind.Atom: case Kind.Int: case Kind.Float: return Raw?.ToString();
                case Kind.String: return "\"" + Raw + "\"";
                case Kind.List:
                case Kind.Tuple:
                {
                    var sb = new StringBuilder();
                    sb.Append(Type == Kind.List ? '[' : '{');
                    var items = Items;
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append(items[i]);
                    }
                    sb.Append(Type == Kind.List ? ']' : '}');
                    return sb.ToString();
                }
            }
            return "<term>";
        }
    }

    /// <summary>
    /// Parses Erlang text-format terms produced by yu_server.
    /// Supports atoms, integers, floats, strings, lists, tuples.
    /// Mirrors LayaAir client ErlangParser.ts.
    /// </summary>
    public static class ErlangParser
    {
        public static ErlangTerm Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string s = text.Trim();
            // Wrap bare values with brackets to share the array/tuple branch.
            if (s.Length == 0 || (s[0] != '[' && s[0] != '{')) s = "[" + s + "]";
            int pos = 0;
            return ParseValue(s, ref pos);
        }

        private static ErlangTerm ParseValue(string s, ref int pos)
        {
            SkipWhitespace(s, ref pos);
            if (pos >= s.Length) return null;
            char c = s[pos];
            if (c == '[') return ParseList(s, ref pos, '[', ']', ErlangTerm.Kind.List);
            if (c == '{') return ParseList(s, ref pos, '{', '}', ErlangTerm.Kind.Tuple);
            if (c == '"') return ParseString(s, ref pos);
            return ParseAtomOrNumber(s, ref pos);
        }

        private static ErlangTerm ParseList(string s, ref int pos, char open, char close, ErlangTerm.Kind kind)
        {
            pos++; // skip open
            var items = new List<ErlangTerm>();
            SkipWhitespace(s, ref pos);
            while (pos < s.Length && s[pos] != close)
            {
                var item = ParseValue(s, ref pos);
                if (item != null) items.Add(item);
                SkipWhitespace(s, ref pos);
                if (pos < s.Length && s[pos] == ',') { pos++; SkipWhitespace(s, ref pos); }
            }
            if (pos < s.Length && s[pos] == close) pos++;
            return new ErlangTerm(kind, items);
        }

        private static ErlangTerm ParseString(string s, ref int pos)
        {
            pos++; // skip opening quote
            var sb = new StringBuilder();
            while (pos < s.Length && s[pos] != '"')
            {
                if (s[pos] == '\\' && pos + 1 < s.Length) { sb.Append(s[pos + 1]); pos += 2; }
                else sb.Append(s[pos++]);
            }
            if (pos < s.Length) pos++; // skip closing quote
            return new ErlangTerm(ErlangTerm.Kind.String, sb.ToString());
        }

        private static ErlangTerm ParseAtomOrNumber(string s, ref int pos)
        {
            int start = pos;
            while (pos < s.Length && IsIdentChar(s[pos])) pos++;
            string token = s.Substring(start, pos - start);
            if (token.Length == 0) return null;

            // number?
            if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out long iv))
                return new ErlangTerm(ErlangTerm.Kind.Int, iv);
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double dv))
                return new ErlangTerm(ErlangTerm.Kind.Float, dv);
            return new ErlangTerm(ErlangTerm.Kind.Atom, token);
        }

        private static bool IsIdentChar(char c)
        {
            if (c >= '0' && c <= '9') return true;
            if (c >= 'a' && c <= 'z') return true;
            if (c >= 'A' && c <= 'Z') return true;
            if (c == '_' || c == '.' || c == '-' || c == '+') return true;
            // Allow Chinese identifier chars used in some atoms.
            if (c >= 0x4E00 && c <= 0x9FA5) return true;
            return false;
        }

        private static void SkipWhitespace(string s, ref int pos)
        {
            while (pos < s.Length)
            {
                char c = s[pos];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r') pos++; else break;
            }
        }
    }
}
