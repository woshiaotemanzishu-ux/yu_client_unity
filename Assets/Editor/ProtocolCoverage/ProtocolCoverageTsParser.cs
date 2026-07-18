using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Shenxiao.Editor.ProtocolCoverage
{
    /// <summary>
    /// 老端 TS 源码的最小括号感知解析工具:不是完整 TS parser,只做到「够用」——
    /// 找 RegisterProtocal(NUM, ARG) 调用的第二实参、按标识符/字符串名回查同文件里的函数体、
    /// 括号配对提取函数体文本。字符串/模板字符串/注释内的括号会被跳过,不参与深度计数。
    /// </summary>
    public static class ProtocolCoverageTsParser
    {
        public sealed class RegisterCall
        {
            public int Cmd;
            public int Line;          // 1-based,RegisterProtocal 调用所在行
            public string ArgRaw;     // 第二实参原始文本(未解析)
            public bool IsCommentedOut; // 调用整行本身被注释掉
        }

        /// <summary>在源码里找全部 `RegisterProtocal(<数字>, <ARG>)` 调用(含 this.RegisterProtocal)。
        /// 每行开头若是 // 或 * 或 /* 视为整行注释,标 IsCommentedOut。</summary>
        public static List<RegisterCall> FindRegisterCalls(string src)
        {
            var result = new List<RegisterCall>();
            var callRegex = new Regex(@"RegisterProtocal\s*\(", RegexOptions.Compiled);
            int[] lineStarts = BuildLineStarts(src);

            foreach (Match m in callRegex.Matches(src))
            {
                int openParenIdx = m.Index + m.Length - 1; // 指向调用的 '('
                if (!TryReadCallArgs(src, openParenIdx, out string firstArg, out string secondArg, out int _))
                {
                    continue;
                }

                firstArg = firstArg.Trim();
                if (!Regex.IsMatch(firstArg, @"^\d+$"))
                {
                    continue; // 老端协议号恒为数字字面量(scan_old.py 同口径),符号常量不在此列
                }

                int line = LineOf(lineStarts, m.Index);
                string lineText = GetLine(src, lineStarts, line);
                string trimmedLine = lineText.TrimStart();
                bool commented = trimmedLine.StartsWith("//") || trimmedLine.StartsWith("*") || trimmedLine.StartsWith("/*");

                result.Add(new RegisterCall
                {
                    Cmd = int.Parse(firstArg),
                    Line = line,
                    ArgRaw = secondArg.Trim(),
                    IsCommentedOut = commented,
                });
            }

            return result;
        }

        /// <summary>解析第二实参的「种类」:字符串字面量("onXXXX")、标识符(可能带 this./.bind(this))、
        /// 或内联函数字面量(=&gt; / function)。返回用于查体的名字(字符串/标识符情形)或 null(内联情形)。</summary>
        public static bool TryResolveHandlerName(string argRaw, out string name)
        {
            name = null;
            if (string.IsNullOrEmpty(argRaw)) return false;

            Match strM = Regex.Match(argRaw, "^[\"']([A-Za-z_][A-Za-z0-9_]*)[\"']$");
            if (strM.Success) { name = strM.Groups[1].Value; return true; }

            Match idM = Regex.Match(argRaw, @"^(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)(?:\.bind\(\s*this\s*\))?$");
            if (idM.Success) { name = idM.Groups[1].Value; return true; }

            return false; // 内联函数字面量等,调用方走 TryExtractInlineBody
        }

        /// <summary>第二实参本身就是内联函数字面量(=&gt; { ... } 或 function(){...})时直接抠出函数体。
        /// 找不到花括号(极少见的无花括号表达式体箭头函数)时返回整个实参文本作为「体」。</summary>
        public static string ExtractInlineBody(string argRaw)
        {
            int brace = argRaw.IndexOf('{');
            if (brace < 0) return argRaw; // 表达式体箭头函数,整段当作体处理(仍可用于判空/判ErrorCodeShow-only)
            if (!TryExtractBracedBody(argRaw, brace, out string body)) return argRaw;
            return body;
        }

        /// <summary>按名字在整份源码里找函数体(类方法简写 / let-const-var 赋值箭头 / 类字段箭头),
        /// 取第一个匹配。找不到返回 null(调用方应「失败即视为存活」,不误杀)。</summary>
        public static string FindDefinitionBody(string src, string name)
        {
            string esc = Regex.Escape(name);
            var patterns = new[]
            {
                // 类方法简写 / function 声明: (public|private|...)? NAME(args) {
                @"(?:(?:public|private|protected|static|async)\s+)*\b" + esc + @"\s*\(([^()]*)\)\s*\{",
                // let/const/var NAME = (args) => {
                @"(?:let|const|var)\s+" + esc + @"\s*=\s*(?:async\s*)?\(([^()]*)\)\s*=>\s*\{",
                // 类字段箭头: NAME = (args) => {  (前面不能是 . 或字母数字,避免匹配到别的成员访问)
                @"(?<![\w.])" + esc + @"\s*=\s*(?:async\s*)?\(([^()]*)\)\s*=>\s*\{",
            };

            foreach (string pat in patterns)
            {
                Match m = Regex.Match(src, pat);
                if (!m.Success) continue;
                int braceIdx = m.Index + m.Length - 1;
                if (TryExtractBracedBody(src, braceIdx, out string body)) return body;
            }

            return null;
        }

        /// <summary>去掉 // 行注释与 /* */ 块注释(保留其余原文,含换行,便于后续判空/文案检查)。</summary>
        public static string StripComments(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                int ni = SkipStringOrComment(s, i, out bool consumed, out bool wasComment);
                if (consumed)
                {
                    if (!wasComment) sb.Append(s, i, ni - i); // 字符串原样保留,只吃掉注释
                    i = ni;
                    continue;
                }
                sb.Append(s[i]);
                i++;
            }
            return sb.ToString();
        }

        /// <summary>去注释后是否为「空函数体」(如 GuildController.ts:729 的 `let on40029 = () =&gt; {};`)。</summary>
        public static bool IsBodyEmpty(string body)
        {
            if (body == null) return false; // null=未解析到定义,交给调用方按“存活”兜底,不当空
            string stripped = StripComments(body);
            return stripped.Trim().Length == 0;
        }

        /// <summary>裁决7 收紧规则:去掉样板 `let scmd = (this.)?user_msg_adapter.GetSCMD(...)` 赋值行
        /// 与全部 Util.ErrorCodeShow(...) 调用后,剩余骨架(if/else/括号/花括号/分号/空白)必须收敛为空——
        /// 即函数体除 Util.ErrorCodeShow 外无其它副作用。</summary>
        public static bool IsErrorExitOnly(string body)
        {
            if (string.IsNullOrEmpty(body)) return false;
            string s = StripComments(body);
            // 必须确实出现过至少一次 ErrorCodeShow,否则不是错误出口(纯空函数体已由 IsBodyEmpty 另外判断)
            bool hadErrorShow = Regex.IsMatch(s, @"Util\s*\.\s*ErrorCodeShow\s*\(");
            if (!hadErrorShow) return false;

            s = Regex.Replace(s, @"let\s+scmd\s*=\s*(?:this\.)?(?:local_)?[Uu]ser_?[Mm]sg_?[Aa]dapter[^;\n]*(;|\n)", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"Util\.ErrorCodeShow\s*\([^)]*\)\s*;?", "");
            s = Regex.Replace(s, @"\b(if|else)\b", "");
            // 剥离条件括号(非嵌套 best-effort,连续剥几轮吃掉普通条件)
            for (int pass = 0; pass < 4; pass++)
            {
                string prev = s;
                s = Regex.Replace(s, @"\(([^()]*)\)", "");
                if (s == prev) break;
            }
            s = Regex.Replace(s, @"[{}();\s]", "");
            return s.Length == 0;
        }

        // ---- 括号/引号感知的底层扫描 ----

        /// <summary>从 RegisterProtocal( 的开括号位置起,读出两个顶层实参(逗号在花括号/圆括号/方括号
        /// /字符串内部不算分隔符)。callEndIdx = 调用自身闭括号下标。</summary>
        public static bool TryReadCallArgs(string s, int openParenIdx, out string firstArg, out string secondArg, out int callEndIdx)
        {
            firstArg = null; secondArg = null; callEndIdx = -1;
            var args = new List<string>();
            int i = openParenIdx + 1;
            int depth = 1;
            int argStart = i;
            int guard = 0;
            while (i < s.Length && depth > 0 && guard < 200000)
            {
                guard++;
                int ni = SkipStringOrComment(s, i, out bool consumed, out bool _);
                if (consumed) { i = ni; continue; }

                char c = s[i];
                if (c == '(' || c == '{' || c == '[') { depth++; i++; continue; }
                if (c == ')' || c == '}' || c == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        args.Add(s.Substring(argStart, i - argStart));
                        callEndIdx = i;
                        break;
                    }
                    i++; continue;
                }
                if (c == ',' && depth == 1)
                {
                    args.Add(s.Substring(argStart, i - argStart));
                    argStart = i + 1;
                    i++;
                    continue;
                }
                i++;
            }

            if (callEndIdx < 0 || args.Count < 2) return false;
            firstArg = args[0];
            var rest = new List<string>();
            for (int k = 1; k < args.Count; k++) rest.Add(args[k]);
            secondArg = string.Join(",", rest);
            return true;
        }

        /// <summary>openBraceIdx 指向一个 '{',返回其内部文本(不含首尾花括号本身)。</summary>
        public static bool TryExtractBracedBody(string s, int openBraceIdx, out string body)
        {
            body = null;
            if (openBraceIdx < 0 || openBraceIdx >= s.Length || s[openBraceIdx] != '{') return false;
            int depth = 1;
            int i = openBraceIdx + 1;
            int start = i;
            int guard = 0;
            while (i < s.Length && depth > 0 && guard < 500000)
            {
                guard++;
                int ni = SkipStringOrComment(s, i, out bool consumed, out bool _);
                if (consumed) { i = ni; continue; }
                char c = s[i];
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        body = s.Substring(start, i - start);
                        return true;
                    }
                }
                i++;
            }
            return false;
        }

        /// <summary>若 i 处是 //、/* */ 或 引号/模板字符串起点,返回跳过后的下标并置 consumed=true;
        /// 否则原样返回 i、consumed=false。wasComment 区分“跳过的是注释”还是“跳过的是字符串字面量”。</summary>
        private static int SkipStringOrComment(string s, int i, out bool consumed, out bool wasComment)
        {
            consumed = false; wasComment = false;
            char c = s[i];
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                int j = s.IndexOf('\n', i);
                consumed = true; wasComment = true;
                return j < 0 ? s.Length : j + 1;
            }
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                int j = s.IndexOf("*/", i + 2, StringComparison.Ordinal);
                consumed = true; wasComment = true;
                return j < 0 ? s.Length : j + 2;
            }
            if (c == '\'' || c == '"' || c == '`')
            {
                char quote = c;
                int j = i + 1;
                while (j < s.Length)
                {
                    if (s[j] == '\\') { j += 2; continue; }
                    if (s[j] == quote) { j++; break; }
                    j++;
                }
                consumed = true; wasComment = false;
                return j;
            }
            return i;
        }

        private static int[] BuildLineStarts(string s)
        {
            var list = new List<int> { 0 };
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\n') list.Add(i + 1);
            }
            return list.ToArray();
        }

        private static int LineOf(int[] lineStarts, int idx)
        {
            int lo = 0, hi = lineStarts.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (lineStarts[mid] <= idx) lo = mid; else hi = mid - 1;
            }
            return lo + 1; // 1-based
        }

        private static string GetLine(string s, int[] lineStarts, int line1Based)
        {
            int idx = line1Based - 1;
            if (idx < 0 || idx >= lineStarts.Length) return string.Empty;
            int start = lineStarts[idx];
            int end = idx + 1 < lineStarts.Length ? lineStarts[idx + 1] : s.Length;
            return s.Substring(start, Math.Max(0, end - start));
        }
    }
}
