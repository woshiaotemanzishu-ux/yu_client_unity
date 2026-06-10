using System.Globalization;
using System.Text;

namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// Builds an Erlang text term suitable for sending to yu_server.
    /// Format strings (mirrors LayaAir client UserMsgAdapter):
    ///   i = int32, l = int64, f = float, d = double
    ///   s = string, b = bool, a = atom
    /// </summary>
    public static class UserMsgAdapter
    {
        /// <summary>
        /// Build a wire payload: [protoId, body...]
        /// </summary>
        public static string Encode(int protoId, string format, params object[] args)
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(protoId);

            if (!string.IsNullOrEmpty(format))
            {
                int argIdx = 0;
                for (int i = 0; i < format.Length; i++)
                {
                    sb.Append(',');
                    AppendArg(sb, format[i], args[argIdx++]);
                }
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static void AppendArg(StringBuilder sb, char fmt, object value)
        {
            switch (fmt)
            {
                case 'i': sb.Append(System.Convert.ToInt32(value, CultureInfo.InvariantCulture)); break;
                case 'l': sb.Append(System.Convert.ToInt64(value, CultureInfo.InvariantCulture)); break;
                case 'f': sb.Append(System.Convert.ToSingle(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)); break;
                case 'd': sb.Append(System.Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)); break;
                case 's':
                    sb.Append('"');
                    foreach (var c in value?.ToString() ?? string.Empty)
                    {
                        if (c == '"' || c == '\\') sb.Append('\\');
                        sb.Append(c);
                    }
                    sb.Append('"');
                    break;
                case 'b': sb.Append(((bool)value) ? "true" : "false"); break;
                case 'a': sb.Append(value?.ToString()); break;
                default:
                    throw new System.ArgumentException("Unknown format char: " + fmt);
            }
        }
    }
}
