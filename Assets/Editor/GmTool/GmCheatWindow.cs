using System.Collections.Generic;
using System.Linq;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Gm;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.GmTool
{
    /// <summary>
    /// GM 秘籍窗口(对标老客户端按 Z 呼出的 CheatInputView):
    /// Play 模式连上游戏服后「拉取清单」→ 服务端下发全部秘籍(分类/命令/参数/默认值,零硬编码)
    /// → 左分类右命令,填参数点发送(命令_参数_参数);顶部直发框可手敲任意命令。
    /// 鉴权:服务端 gm_password 非空时先发 setgmpassword_密码(顶部直发框敲一次即可)。
    /// </summary>
    public class GmCheatWindow : EditorWindow
    {
        private string _rawCommand = "";
        private string _search = "";
        private int _categoryIndex;
        private Vector2 _catScroll;
        private Vector2 _cmdScroll;
        // 参数值:按 "命令名#参数序号" 记(窗口会话内),没填过用服务端默认值
        private readonly Dictionary<string, string> _argValues = new Dictionary<string, string>();

        [MenuItem("神霄/GM 秘籍", priority = 3)]
        public static void Open()
        {
            var w = GetWindow<GmCheatWindow>("GM 秘籍");
            w.minSize = new Vector2(640f, 420f);
        }

        private void OnEnable()
        {
            EventDispatcher.On(GlobalEvent.EVT_GM_CHEAT_LIST, OnListArrived);
        }

        private void OnDisable()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GM_CHEAT_LIST, OnListArrived);
        }

        private void OnListArrived() => Repaint();

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play 模式并连上游戏服(登录到角色列表/游戏内)后使用。\n" +
                    "清单由服务端下发(pp_gm.erl),服务端加命令这里自动出现。", MessageType.Info);
                return;
            }
            bool connected = NetManager.IsConnected;
            if (!connected)
                EditorGUILayout.HelpBox("游戏服未连接——先在游戏里走到选角/游戏内。", MessageType.Warning);

            using (new EditorGUI.DisabledScope(!connected))
            {
                // 顶部:直发框 + 拉取
                using (new EditorGUILayout.HorizontalScope())
                {
                    _rawCommand = EditorGUILayout.TextField(_rawCommand);
                    if (GUILayout.Button("直接发送", GUILayout.Width(72f)) && !string.IsNullOrWhiteSpace(_rawCommand))
                        GmCheatController.Instance.SendCommand(_rawCommand);
                    if (GUILayout.Button(GmCheatController.Instance.Categories.Count > 0 ? "刷新清单" : "拉取清单",
                            GUILayout.Width(72f)))
                        GmCheatController.Instance.RequestList();
                }
                EditorGUILayout.LabelField(
                    "格式:命令_参数_参数(如 lv_100 / goods_36010001_10);鉴权先发 setgmpassword_密码",
                    EditorStyles.miniLabel);

                IReadOnlyList<GmCheatController.GmCategory> cats = GmCheatController.Instance.Categories;
                if (cats.Count == 0)
                {
                    EditorGUILayout.HelpBox("还没有清单——点「拉取清单」(需要已通过 10000 登录游戏服)。", MessageType.Info);
                    return;
                }

                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawCategories(cats);
                    DrawCommands(cats);
                }
            }
        }

        private void DrawCategories(IReadOnlyList<GmCheatController.GmCategory> cats)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(120f)))
            {
                _catScroll = EditorGUILayout.BeginScrollView(_catScroll);
                for (int i = 0; i < cats.Count; i++)
                {
                    bool on = GUILayout.Toggle(_categoryIndex == i,
                        $"{cats[i].Name}({cats[i].Commands.Count})", "Button", GUILayout.Height(24f));
                    if (on && _categoryIndex != i) { _categoryIndex = i; GUI.FocusControl(null); }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawCommands(IReadOnlyList<GmCheatController.GmCategory> cats)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                // 有搜索词时跨分类搜,否则显示当前分类
                List<GmCheatController.GmCommand> shown;
                if (!string.IsNullOrEmpty(_search))
                {
                    string q = _search.ToLowerInvariant();
                    shown = cats.SelectMany(c => c.Commands)
                        .Where(c => c.Command.ToLowerInvariant().Contains(q) || c.DisplayName.Contains(_search))
                        .ToList();
                }
                else
                {
                    shown = cats[Mathf.Clamp(_categoryIndex, 0, cats.Count - 1)].Commands;
                }

                _cmdScroll = EditorGUILayout.BeginScrollView(_cmdScroll);
                foreach (GmCheatController.GmCommand cmd in shown)
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(
                            new GUIContent($"{cmd.DisplayName}", cmd.Command), GUILayout.Width(150f));
                        EditorGUILayout.LabelField(cmd.Command, EditorStyles.miniLabel, GUILayout.Width(120f));
                        var parts = new List<string> { cmd.Command };
                        for (int a = 0; a < cmd.Args.Length; a++)
                        {
                            string key = cmd.Command + "#" + a;
                            if (!_argValues.TryGetValue(key, out string val))
                                val = a < cmd.Defaults.Length ? cmd.Defaults[a] : "";
                            string newVal = EditorGUILayout.TextField(val, GUILayout.MinWidth(50f));
                            _argValues[key] = newVal;
                            // 参数描述做悬停提示
                            Rect last = GUILayoutUtility.GetLastRect();
                            if (a < cmd.Args.Length)
                                GUI.Label(last, new GUIContent("", cmd.Args[a]));
                            parts.Add(newVal);
                        }
                        if (GUILayout.Button("发送", GUILayout.Width(48f)))
                        {
                            // 去掉尾部空参数(无参命令直接发命令名)
                            while (parts.Count > 1 && string.IsNullOrWhiteSpace(parts[parts.Count - 1]))
                                parts.RemoveAt(parts.Count - 1);
                            GmCheatController.Instance.SendCommand(string.Join("_", parts));
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }
}
