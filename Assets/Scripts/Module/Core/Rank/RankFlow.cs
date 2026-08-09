using System;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Rank
{
    /// <summary>
    /// 排行榜大窗编排(自动循环 轮12 #12;纯数据层轮)。
    ///
    /// **UI 裁决(spec §UI 裁决)**:排行榜 prefab 全套不存在(RankEntView/RankView/RankTabButton/RankItem/
    /// RankMenuView 均未 convert-module),烤图需编辑器(被打包会话占用)且快照管线需用户参与验收——
    /// 本轮不做 UI、不建 prefab、不写 Creator、不写任何布局代码。
    ///
    /// RankFlow 只做"降级壳":Toggle→尝试按约定 key 加载内容 prefab→加载失败(本轮预期状态)记 GameLog +
    /// toast"排行榜界面待烤"。若未来 convert-module 把 prefab 烤出来(key 与 <see cref="CONTENT_MODULE"/>/
    /// <see cref="CONTENT_PREFAB"/> 一致),本类不用改就能直接切换到真实打开路径。
    /// 入口注册见 <see cref="RankBootstrap"/>(MainUIRouter "activity_rank",修复 HUD 竞榜卡孤儿路由
    /// MainUIRankView.cs:387)。
    /// </summary>
    public static class RankFlow
    {
        private const string CONTENT_MODULE = "rank";
        private const string CONTENT_PREFAB = "RankEntView";

        private static GameObject _root;
        private static bool _loading;

        public static void Toggle()
        {
            if (_root != null) { Close(); return; }
            _ = OpenAsync();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_root == null) return;
            ResManager.ReleaseInstance(_root);
            _root = null;
        }

        private static async Task OpenAsync()
        {
            if (_root != null) return;
            if (_loading) return;
            _loading = true;

            string key = GameResPath.GetUIPrefab(CONTENT_MODULE, CONTENT_PREFAB);
            GameObject go = null;
            try
            {
                // RequestRankFirstPage 依赖 config_ranking.rank_max 决定是否继续拉取后续页。
                // 全仓没有其他 RankConfigs.EnsureLoaded 调用点，因此必须在内容实例化前完成配置加载；
                // 否则未来 Prefab 落地后会静默退化为 ONE_MAX(20) 条，而战力榜配置上限是 100。
                await RankConfigs.EnsureLoaded();
                go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            }
            catch (Exception e)
            {
                GameLog.Warn("Rank", "排行榜大窗 prefab 加载异常 key={0} error={1}(convert-module 未跑,本轮预期状态)",
                    key, e.Message);
            }
            finally
            {
                _loading = false;
            }

            if (go == null)
            {
                GameLog.Warn("Rank", "排行榜大窗 prefab 未烤(key={0}),降级 toast(convert-module 待用户验收后接入)", key);
                TipsManager.Toast("排行榜界面待烤");
                return;
            }

            // prefab 一旦存在(未来尾包),走正常打开路径:默认榜(对标老端 GAME_START selectTab=0/战力榜)。
            _root = go;
            _root.name = CONTENT_PREFAB;
            RankController.Instance.RequestRankFirstPage(RankModel.TYPE_FIGHT);
            GameLog.Info("Rank", "排行榜大窗打开(prefab 已就绪——本轮预期它不存在,若看到此行说明尾包已完成 convert-module)");
        }

        internal static void Reset()
        {
            Close();
            _loading = false;
        }
    }
}
