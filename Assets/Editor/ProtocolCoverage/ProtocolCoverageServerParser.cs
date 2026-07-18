using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Shenxiao.Editor.ProtocolCoverage
{
    /// <summary>
    /// 解析 yu_server/src/server/mod_server.erl 的族级路由分发(routing/3 里的 "NNN" -> pp_xxx:handle 分支)。
    /// 只做族级(三位前缀)粗判——号级死号判定留 killlist.json 人工填 evidence(裁决4,不写 ErlangParser)。
    /// </summary>
    public static class ProtocolCoverageServerParser
    {
        public enum RouteStatus { Live, Commented, }

        public sealed class RouteEntry
        {
            public int Prefix;         // 三位族前缀,如 152
            public string Target;      // pp_xxx
            public RouteStatus Status;
            public int Line;
        }

        private static readonly Regex RoutePattern =
            new Regex("^(?<pre>\\s*%*\\s*)\"(?<prefix>\\d{2,4})\"\\s*->\\s*(?<target>pp_\\w+)", RegexOptions.Compiled);

        /// <summary>返回 prefix -> RouteEntry(同前缀多行时取第一条非注释命中,否则取注释行)。</summary>
        public static Dictionary<int, RouteEntry> Parse(string modServerErlPath)
        {
            var result = new Dictionary<int, RouteEntry>();
            if (!File.Exists(modServerErlPath)) return result;

            string[] lines = File.ReadAllLines(modServerErlPath);
            for (int i = 0; i < lines.Length; i++)
            {
                Match m = RoutePattern.Match(lines[i]);
                if (!m.Success) continue;

                int prefix = int.Parse(m.Groups["prefix"].Value);
                bool commented = m.Groups["pre"].Value.Contains("%");
                var entry = new RouteEntry
                {
                    Prefix = prefix,
                    Target = m.Groups["target"].Value,
                    Status = commented ? RouteStatus.Commented : RouteStatus.Live,
                    Line = i + 1,
                };

                if (!result.TryGetValue(prefix, out RouteEntry existing) || (existing.Status == RouteStatus.Commented && !commented))
                {
                    result[prefix] = entry;
                }
            }

            return result;
        }
    }
}
