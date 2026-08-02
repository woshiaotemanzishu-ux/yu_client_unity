using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Shenxiao.Editor.ProtocolCoverage
{
    /// <summary>
    /// 协议覆盖率核验器的核心扫描逻辑。四路数据源:
    ///  1) Unity 已注册集合 = 运行时真值——ControllerHub.InitAll() + LoginController.Instance.Init() 后
    ///     反射 NetManager 的私有静态 _handlers 字典取 Keys(裁决1:比正则强一个量级,注释掉的注册/
    ///     不在 Hub 的控制器天然进不了这个字典)。
    ///  2) 老端活 handler = 扫 {ClientRoot}/h5/src/**/*.ts 的 RegisterProtocal(数字,ARG)。ARG 可能是
    ///     内联函数、标识符引用、或字符串("on11003",经 common/BaseController.ts:25-29 `this[name]` 解析)——
    ///     三种都要回查函数体;体裁定为空(如 GuildController.ts:729 `let on40029 = () =&gt; {};`)则判 DEAD,
    ///     不算活缺口分子分母,否则会把死号误判成 Unity 未接的活缺口(误杀式虚报)。
    ///  3) 协议全集 = ClientProtocol.json 的 key 集合。
    ///  4) 服务端族级路由 = 解析 mod_server.erl 的 "NNN" -&gt; pp_xxx; 分发(族级粗判,见
    ///     ProtocolCoverageServerParser 顶部注释:号级死号判定留 killlist.json 人工填 evidence)。
    ///
    /// 另外静态扫描 Unity C# 源码里的 RegisterProtocal 调用点(用于 D 段双注册检查)和直接
    /// `Send*(Proto.X,...)` 调用点(用于 G 段 send_only 发送证据)。运行时字典是覆盖语义,
    /// 同号注册两次在 Dictionary 里看不出来,必须回源码才能抓到。
    /// </summary>
    public static class ProtocolCoverageScanner
    {
        public sealed class OldHandlerInfo
        {
            public int Cmd;
            public string File;
            public int Line;
            public bool Alive;
            public string DeadReason;
        }

        public sealed class DuplicateSite
        {
            public string File;
            public int Line;
            public string Class;
        }

        public sealed class FamilyStat
        {
            public int Prefix;
            public int UnityRegistered;
            public int LiveGap;
            public readonly List<int> LiveGapCmds = new List<int>();
            public int DeadGap;
            public string ServerRouteTarget;
            public string ServerRouteStatus = "NoRoute";
        }

        public sealed class ScanResult
        {
            public DateTime GeneratedAt;

            /// <summary>运行时真值:ControllerHub.InitAll()+LoginController.Instance.Init() 后
            /// NetManager._handlers 的 Keys。</summary>
            public readonly HashSet<int> UnityRegistered = new HashSet<int>();
            public readonly Dictionary<int, string> UnityHandlerSource = new Dictionary<int, string>();

            /// <summary>Proto.cs 中 public const int 协议号全集。只用于硬负约束防复发，
            /// 不参与覆盖率分子；运行时注册集合仍是覆盖率唯一真值。</summary>
            public readonly HashSet<int> UnityProtocolConstants = new HashSet<int>();

            /// <summary>老端每个协议号的「最新已知状态」——同号多处注册时,任一处真活则整体判活
            /// (与 scan_old.py 的 setdefault 语义一致,外加本轮新增的空体转 DEAD 升级判断)。</summary>
            public readonly Dictionary<int, OldHandlerInfo> OldAll = new Dictionary<int, OldHandlerInfo>();
            public readonly Dictionary<int, List<(string file, int line)>> OldCommentedOnly = new Dictionary<int, List<(string, int)>>();

            public readonly HashSet<int> ClientProtocolDefined = new HashSet<int>();

            public readonly Dictionary<int, ProtocolCoverageServerParser.RouteEntry> ServerRoutes =
                new Dictionary<int, ProtocolCoverageServerParser.RouteEntry>();

            /// <summary>Unity C# 源码里全部 RegisterProtocal 静态调用点(含单次注册的,过滤重复见 DuplicateRegistrations)。</summary>
            public readonly Dictionary<int, List<DuplicateSite>> UnityStaticSites = new Dictionary<int, List<DuplicateSite>>();

            /// <summary>生产 C# 源码里去注释后的直接 `Send*(Proto.X,...)` 调用点。
            /// 只作为 killlist send_only 的静态发送证据，不参与覆盖率分子。</summary>
            public readonly Dictionary<int, List<DuplicateSite>> UnityStaticSendSites =
                new Dictionary<int, List<DuplicateSite>>();

            /// <summary>生产 C# 源码里去注释后的直接五位数字 `Send*(12345,...)` 调用点。
            /// 这类调用不能作为 send_only 正向证据，只用于 F/G 抓绕过 Proto 常量的违规发送。</summary>
            public readonly Dictionary<int, List<DuplicateSite>> UnityStaticLiteralSendSites =
                new Dictionary<int, List<DuplicateSite>>();

            /// <summary>老端活协议里,函数体裁定为「仅 Util.ErrorCodeShow」的号(裁决7 收紧规则)。</summary>
            public readonly HashSet<int> ErrorExitCandidates = new HashSet<int>();

            public static int Family(int cmd) => cmd / 100;

            public HashSet<int> OldActiveKeys()
            {
                var set = new HashSet<int>();
                foreach (KeyValuePair<int, OldHandlerInfo> kv in OldAll)
                {
                    if (kv.Value.Alive) set.Add(kv.Key);
                }
                return set;
            }

            /// <summary>活口径分母:ClientProtocol.json 定义 ∩ 老端真活。</summary>
            public HashSet<int> LiveDefinedSet()
            {
                var set = new HashSet<int>(ClientProtocolDefined);
                set.IntersectWith(OldActiveKeys());
                return set;
            }

            /// <summary>活缺口 = 活协议全集 - Unity 已注册。</summary>
            public HashSet<int> LiveGap()
            {
                var set = LiveDefinedSet();
                set.ExceptWith(UnityRegistered);
                return set;
            }

            /// <summary>死号(粗口径)= 定义了但老端无活 handler 且 Unity 也没注册(含老端空体/仅注释的号)。</summary>
            public HashSet<int> DeadGap()
            {
                var set = new HashSet<int>(ClientProtocolDefined);
                set.ExceptWith(OldActiveKeys());
                set.ExceptWith(UnityRegistered);
                return set;
            }

            /// <summary>Unity 已注册但不在 ClientProtocol.json 里的号(手写场景/战斗协议,口径陷阱,见裁决2)。</summary>
            public HashSet<int> HandwrittenExtra()
            {
                var set = new HashSet<int>(UnityRegistered);
                set.ExceptWith(ClientProtocolDefined);
                return set;
            }

            public Dictionary<int, List<DuplicateSite>> DuplicateRegistrations()
            {
                var dup = new Dictionary<int, List<DuplicateSite>>();
                foreach (KeyValuePair<int, List<DuplicateSite>> kv in UnityStaticSites)
                {
                    var distinct = new HashSet<string>();
                    foreach (DuplicateSite s in kv.Value) distinct.Add(s.File + ":" + s.Line);
                    if (distinct.Count > 1) dup[kv.Key] = kv.Value;
                }
                return dup;
            }

            public List<FamilyStat> BuildFamilyTable()
            {
                var byPrefix = new Dictionary<int, FamilyStat>();
                FamilyStat Get(int p)
                {
                    if (!byPrefix.TryGetValue(p, out FamilyStat fs))
                    {
                        fs = new FamilyStat { Prefix = p };
                        byPrefix[p] = fs;
                    }
                    return fs;
                }

                foreach (int cmd in UnityRegistered) Get(Family(cmd)).UnityRegistered++;
                foreach (int cmd in LiveGap())
                {
                    FamilyStat family = Get(Family(cmd));
                    family.LiveGap++;
                    family.LiveGapCmds.Add(cmd);
                }
                foreach (int cmd in DeadGap()) Get(Family(cmd)).DeadGap++;

                foreach (KeyValuePair<int, FamilyStat> kv in byPrefix)
                {
                    kv.Value.LiveGapCmds.Sort();
                    if (ServerRoutes.TryGetValue(kv.Key, out ProtocolCoverageServerParser.RouteEntry route))
                    {
                        kv.Value.ServerRouteTarget = route.Target;
                        kv.Value.ServerRouteStatus = route.Status.ToString();
                    }
                }

                return byPrefix.Values.OrderBy(f => f.Prefix).ToList();
            }
        }

        public static ScanResult Scan()
        {
            var r = new ScanResult { GeneratedAt = DateTime.Now };
            ReflectUnityRegistered(r);
            Dictionary<string, int> protoConsts = ParseProtoConsts();
            r.UnityProtocolConstants.UnionWith(protoConsts.Values);
            ScanUnityStaticSites(r, protoConsts);
            ScanOldClient(r);
            ScanClientProtocolJson(r);
            ScanServerRoutes(r);
            return r;
        }

        // ---- 1) 运行时真值 ----

        private static void ReflectUnityRegistered(ScanResult r)
        {
            Shenxiao.Module.Core.Game.ControllerHub.InitAll();
            Shenxiao.Module.Core.Login.LoginController.Instance.Init();

            FieldInfo field = typeof(Shenxiao.Framework.Net.NetManager).GetField(
                "_handlers", BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
            {
                throw new InvalidOperationException(
                    "反射 NetManager._handlers 失败:字段不存在(签名变了?需要同步改这里)。");
            }

            if (!(field.GetValue(null) is IDictionary dict))
            {
                throw new InvalidOperationException("NetManager._handlers 反射取值为 null 或非 IDictionary。");
            }

            foreach (DictionaryEntry kv in dict)
            {
                int cmd = (int)kv.Key;
                r.UnityRegistered.Add(cmd);
                if (kv.Value is Delegate handler)
                {
                    string decl = handler.Method.DeclaringType != null ? handler.Method.DeclaringType.FullName : "?";
                    r.UnityHandlerSource[cmd] = decl + "." + handler.Method.Name;
                }
            }
        }

        // ---- D/G 段:Unity C# 源码静态扫描(注册/发送证据,不作为已注册集合的权威来源)----

        private static Dictionary<string, int> ParseProtoConsts()
        {
            var consts = new Dictionary<string, int>();
            string path = ProtocolCoverageSettings.ProtoConstsPath;
            if (!File.Exists(path)) return consts;
            string src = File.ReadAllText(path);
            foreach (Match m in Regex.Matches(src, @"public const int\s+(\w+)\s*=\s*(\d+)\s*;"))
            {
                consts[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
            }
            return consts;
        }

        private static void ScanUnityStaticSites(ScanResult r, Dictionary<string, int> protoConsts)
        {
            string root = ProtocolCoverageSettings.UnityScriptsRoot;
            if (!Directory.Exists(root)) return;

            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string rel = path.Replace('\\', '/');
                if (rel.EndsWith("Framework/Net/BaseController.cs") || rel.EndsWith("Framework/Net/NetManager.cs"))
                {
                    continue;
                }

                string src = File.ReadAllText(path);
                string cls = ExtractClassName(src);
                string[] lines = StripLineComments(StripCSharpStringLiterals(src));
                string clean = string.Join("\n", lines);

                foreach (Match m in Regex.Matches(
                    clean,
                    @"\bSend\w*\s*\(\s*Proto\s*\.\s*(\w+)\b"))
                {
                    if (!protoConsts.TryGetValue(m.Groups[1].Value, out int cmd)) continue;
                    AddStaticSite(r.UnityStaticSendSites, cmd, rel, FindLine(clean, m.Index), cls);
                }

                foreach (Match m in Regex.Matches(clean, @"\bSend\w*\s*\(\s*(\d{5})\b"))
                {
                    int cmd = int.Parse(m.Groups[1].Value);
                    AddStaticSite(r.UnityStaticLiteralSendSites, cmd, rel, FindLine(clean, m.Index), cls);
                }

                if (!clean.Contains("RegisterProtocal")) continue;
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (Match m in Regex.Matches(lines[i], @"RegisterProtocal\s*\(\s*([^,]+?)\s*,"))
                    {
                        string arg = m.Groups[1].Value.Trim();
                        int? num = ResolveArgNumber(arg, protoConsts);
                        if (num == null) continue;

                        if (!r.UnityStaticSites.TryGetValue(num.Value, out List<DuplicateSite> list))
                        {
                            list = new List<DuplicateSite>();
                            r.UnityStaticSites[num.Value] = list;
                        }
                        list.Add(new DuplicateSite { File = rel, Line = i + 1, Class = cls });
                    }
                }
            }
        }

        private static int FindLine(string source, int index)
        {
            int line = 1;
            for (int i = 0; i < index; i++)
            {
                if (source[i] == '\n') line++;
            }
            return line;
        }

        private static void AddStaticSite(
            Dictionary<int, List<DuplicateSite>> target,
            int cmd,
            string file,
            int line,
            string cls)
        {
            if (!target.TryGetValue(cmd, out List<DuplicateSite> sites))
            {
                sites = new List<DuplicateSite>();
                target[cmd] = sites;
            }
            sites.Add(new DuplicateSite { File = file, Line = line, Class = cls });
        }

        /// <summary>把普通/逐字字符串与字符字面量替换为空格，保留总长度和换行位置。
        /// 发送扫描不能把 GameLog/XML 文案里的 `SendFmt(12345)` 当成生产调用。</summary>
        private static string StripCSharpStringLiterals(string source)
        {
            char[] chars = source.ToCharArray();
            bool inString = false;
            bool inChar = false;
            bool verbatim = false;
            bool escaped = false;

            for (int i = 0; i < chars.Length; i++)
            {
                char c = source[i];
                if (!inString && !inChar)
                {
                    if (c == '"')
                    {
                        inString = true;
                        verbatim = (i > 0 && source[i - 1] == '@')
                            || (i > 1 && source[i - 2] == '@' && source[i - 1] == '$');
                        escaped = false;
                        chars[i] = ' ';
                        continue;
                    }
                    else if (c == '\'')
                    {
                        inChar = true;
                        escaped = false;
                        chars[i] = ' ';
                        continue;
                    }
                    continue;
                }

                if (c != '\n' && c != '\r') chars[i] = ' ';
                if (inString)
                {
                    if (verbatim)
                    {
                        if (c == '"')
                        {
                            if (i + 1 < source.Length && source[i + 1] == '"')
                            {
                                chars[++i] = ' ';
                            }
                            else
                            {
                                inString = false;
                                verbatim = false;
                            }
                        }
                    }
                    else if (escaped)
                    {
                        escaped = false;
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                }
                else if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '\'')
                {
                    inChar = false;
                }
            }

            return new string(chars);
        }

        private static int? ResolveArgNumber(string arg, Dictionary<string, int> protoConsts)
        {
            if (Regex.IsMatch(arg, @"^\d+$")) return int.Parse(arg);
            string key = arg.Contains(".") ? arg.Substring(arg.LastIndexOf('.') + 1) : arg;
            return protoConsts.TryGetValue(key, out int v) ? v : (int?)null;
        }

        private static string ExtractClassName(string src)
        {
            Match m = Regex.Match(src, @"class\s+(\w+Controller)\b");
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>逐行去掉 // 与 /* */ 注释(与 round20 scan.py 的 strip_comments_lines 同语义),
        /// 保留行号不变,便于 D 段报告能指到具体行。</summary>
        private static string[] StripLineComments(string src)
        {
            string[] rawLines = src.Replace("\r\n", "\n").Split('\n');
            var outLines = new string[rawLines.Length];
            bool inBlock = false;
            for (int i = 0; i < rawLines.Length; i++)
            {
                string s = rawLines[i];
                if (inBlock)
                {
                    int endIdx = s.IndexOf("*/", StringComparison.Ordinal);
                    if (endIdx >= 0) { s = s.Substring(endIdx + 2); inBlock = false; }
                    else { s = string.Empty; }
                }

                int blockIdx = s.IndexOf("/*", StringComparison.Ordinal);
                if (blockIdx >= 0)
                {
                    string pre = s.Substring(0, blockIdx);
                    string rest = s.Substring(blockIdx + 2);
                    int endIdx = rest.IndexOf("*/", StringComparison.Ordinal);
                    if (endIdx >= 0) { s = pre + rest.Substring(endIdx + 2); }
                    else { s = pre; inBlock = true; }
                }

                int lineIdx = s.IndexOf("//", StringComparison.Ordinal);
                if (lineIdx >= 0) s = s.Substring(0, lineIdx);
                outLines[i] = s;
            }
            return outLines;
        }

        // ---- 2) 老端活 handler ----

        private static void ScanOldClient(ScanResult r)
        {
            string root = ProtocolCoverageSettings.OldClientSrcRoot;
            if (!Directory.Exists(root)) return;

            foreach (string path in Directory.GetFiles(root, "*.ts", SearchOption.AllDirectories))
            {
                string src;
                try { src = File.ReadAllText(path); }
                catch { continue; }
                if (!src.Contains("RegisterProtocal")) continue;

                string rel = "h5/src/" + path.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');

                List<ProtocolCoverageTsParser.RegisterCall> calls;
                try { calls = ProtocolCoverageTsParser.FindRegisterCalls(src); }
                catch { continue; }

                foreach (ProtocolCoverageTsParser.RegisterCall call in calls)
                {
                    if (call.IsCommentedOut)
                    {
                        if (!r.OldCommentedOnly.TryGetValue(call.Cmd, out List<(string, int)> list))
                        {
                            list = new List<(string, int)>();
                            r.OldCommentedOnly[call.Cmd] = list;
                        }
                        list.Add((rel, call.Line));
                        continue;
                    }

                    string body = ResolveHandlerBody(src, call.ArgRaw);
                    bool alive = !ProtocolCoverageTsParser.IsBodyEmpty(body);
                    string deadReason = alive ? null : "empty_body";

                    if (!r.OldAll.TryGetValue(call.Cmd, out OldHandlerInfo existing) || (!existing.Alive && alive))
                    {
                        r.OldAll[call.Cmd] = new OldHandlerInfo
                        {
                            Cmd = call.Cmd,
                            File = rel,
                            Line = call.Line,
                            Alive = alive,
                            DeadReason = deadReason,
                        };
                    }

                    if (alive && ProtocolCoverageTsParser.IsErrorExitOnly(body))
                    {
                        r.ErrorExitCandidates.Add(call.Cmd);
                    }
                }
            }
        }

        private static string ResolveHandlerBody(string src, string argRaw)
        {
            if (ProtocolCoverageTsParser.TryResolveHandlerName(argRaw, out string name))
            {
                return ProtocolCoverageTsParser.FindDefinitionBody(src, name); // null=未解析到定义,按存活兜底(IsBodyEmpty(null)=false)
            }
            return ProtocolCoverageTsParser.ExtractInlineBody(argRaw);
        }

        // ---- 3) 协议全集 ----

        private static void ScanClientProtocolJson(ScanResult r)
        {
            string path = ProtocolCoverageSettings.ClientProtocolJsonPath;
            if (!File.Exists(path)) return;
            JObject obj = JObject.Parse(File.ReadAllText(path));
            foreach (JProperty prop in obj.Properties())
            {
                if (int.TryParse(prop.Name, out int cmd)) r.ClientProtocolDefined.Add(cmd);
            }
        }

        // ---- 4) 服务端族级路由 ----

        private static void ScanServerRoutes(ScanResult r)
        {
            Dictionary<int, ProtocolCoverageServerParser.RouteEntry> routes =
                ProtocolCoverageServerParser.Parse(ProtocolCoverageSettings.ModServerErlPath);
            foreach (KeyValuePair<int, ProtocolCoverageServerParser.RouteEntry> kv in routes)
            {
                r.ServerRoutes[kv.Key] = kv.Value;
            }
        }
    }
}
