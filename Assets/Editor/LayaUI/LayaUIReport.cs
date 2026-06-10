using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// 转换报告:缺图、近似处理、运行时才赋值的节点、没见过的属性,全部落盘,绝不静默。
    /// 输出 Reports/LayaUI/{module}_report.md(仓库根目录,不进 Assets)。
    /// </summary>
    public class LayaUIReport
    {
        private readonly string _module;
        private string _scene = "";
        private readonly List<string> _lines = new List<string>();
        private readonly Dictionary<string, int> _unknownProps = new Dictionary<string, int>();
        public int MissingCount { get; private set; }

        public LayaUIReport(string module)
        {
            _module = module;
        }

        public void BeginScene(string sceneKey)
        {
            _scene = sceneKey;
            _lines.Add("");
            _lines.Add("## " + sceneKey);
        }

        public void MissingSkin(string skin, string why)
        {
            MissingCount++;
            _lines.Add("- ❌ 缺图 `" + skin + "`:" + why);
        }

        public void Approx(string what)
        {
            _lines.Add("- ⚠ 近似:" + what);
        }

        public void RuntimeAssigned(string node, string what)
        {
            _lines.Add("- ℹ 运行时赋值:`" + node + "` " + what);
        }

        public void Note(string msg)
        {
            _lines.Add("- " + msg);
        }

        public void UnknownProp(string type, string prop)
        {
            string key = type + "." + prop;
            int n;
            _unknownProps.TryGetValue(key, out n);
            _unknownProps[key] = n + 1;
        }

        public string Save()
        {
            Directory.CreateDirectory(LayaUISettings.REPORT_ROOT);
            string path = Path.Combine(LayaUISettings.REPORT_ROOT, _module + "_report.md");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# LayaUI 转换报告 — " + _module);
            sb.AppendLine();
            sb.AppendLine("生成时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            foreach (string l in _lines) sb.AppendLine(l);
            if (_unknownProps.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## 未映射属性(转换器忽略,需评估)");
                foreach (KeyValuePair<string, int> kv in _unknownProps)
                {
                    sb.AppendLine("- `" + kv.Key + "` ×" + kv.Value);
                }
            }
            File.WriteAllText(path, sb.ToString());
            Debug.Log("[LayaUI] 报告已写出: " + path);
            return path;
        }
    }
}
