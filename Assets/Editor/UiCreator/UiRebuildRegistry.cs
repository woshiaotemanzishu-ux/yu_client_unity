using System;
using System.Collections.Generic;

namespace Shenxiao.Editor.UiCreator
{
    /// <summary>
    /// 重构UI 生成器面板的【注册表】。
    /// 每个 *Creator 用 [InitializeOnLoadMethod] 把自己注册进来(模块 + 名字 + 生成/预览动作),
    /// UiRebuildWindow 只负责按模块分组渲染。这样后续加再多页面,只改各自 Creator、不动面板。
    /// </summary>
    public sealed class UiCreatorEntry
    {
        public string Module;          // 分组名(如 "Login")
        public string Name;            // 条目名(如 "LoginPanel(登录+注册)")
        public string Note;            // 可选一行说明
        public Action Generate;        // ① 生成 prefab
        public Action Preview;         // ② 运行时预览(可空)
        public string PrefabPath;      // 生成产物路径(可空,用于"定位")
        public int Order;              // 同模块内排序
    }

    public static class UiRebuildRegistry
    {
        private static readonly List<UiCreatorEntry> _entries = new List<UiCreatorEntry>();

        public static IReadOnlyList<UiCreatorEntry> Entries => _entries;

        /// <summary>注册一个生成器条目;同 模块+名字 视为同一条,重复注册(域重载后)直接替换。</summary>
        public static void Register(UiCreatorEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Module) || string.IsNullOrEmpty(entry.Name)) return;
            _entries.RemoveAll(x => x.Module == entry.Module && x.Name == entry.Name);
            _entries.Add(entry);
        }
    }
}
