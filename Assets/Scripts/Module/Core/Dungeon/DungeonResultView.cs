using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.DungeonCommon;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Dungeon
{
    /// <summary>
    /// 副本结算弹层(对标老端 dungeon/DungeonVictoryView.ts + DungeonFailureView.ts,由
    /// BaseDungeonController OPEN_DENGEON_RESULT_VIEW 打开)。61003 结算推送(result/grade/reward_list)
    /// 到达时由 <see cref="DungeonController.On61003"/> 调 <see cref="Show"/>:胜利开 DungeonVictoryView、
    /// 失败开 DungeonFailureView(转换产物 DungeonCommonModule.prefab,静态皮肤/布局全在 prefab)。
    ///
    /// 本轮范围(真数据真 UI,未接的演出精确降级):
    ///   · 胜利:胜利字样组(_gp_victory 皮肤)+ 评级星(grade→_img_star0..2)+ 奖励格(_tpl_CommonRewardItem
    ///     内嵌 EquipmentItem,真 goods 图标/数量)+ 点击关闭(对标老端 click_bg_toClose)。
    ///   · 失败:失败皮肤 + 关闭按钮(老端的"战力提升建议"列表 _tpl_DungeonFailureStrengItem 未接线,隐藏)。
    ///   · 未接演出:SYTweenLite 铜牌/宝箱抖动、ShowExpAni 经验条增长、_html_left_time 自动退出倒计时、
    ///     result_type 分型布局(老端按 DungeonResultType 挪 UI)—— 记录为后续,不臆造。
    /// 加载失败(prefab 缺/绑定缺)回退 Toast 并记精确 blocker,不落静默。
    /// </summary>
    public sealed class DungeonResultView
    {
        public static readonly DungeonResultView Instance = new DungeonResultView();
        private DungeonResultView() { }

        private GameObject _moduleRoot;
        private DungeonVictoryViewBind _victory;
        private DungeonFailureViewBind _failure;
        private Task<bool> _loadTask;
        private readonly List<GameObject> _rewardCells = new List<GameObject>();
        private int _openEpoch;
        private bool _clicksBound;

        /// <summary>
        /// 打开结算(result==1 胜利,其余失败;grade=评级星 0-3;rewards=已映射真 goods_id 的奖励)。
        /// fire-and-forget 安全:内部兜异常 + 加载失败回退 Toast。
        /// </summary>
        public void Show(bool victory, int grade, List<(int goodsId, long count)> rewards)
        {
            _ = ShowAsync(++_openEpoch, victory, grade, rewards ?? new List<(int, long)>());
        }

        public void Close()
        {
            ++_openEpoch;
            ClearRewardCells();
            if (_victory != null) _victory.Hide();
            if (_failure != null) _failure.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
            GameLog.Info("Dungeon", "DungeonResultView closed");
        }

        private async Task ShowAsync(int epoch, bool victory, int grade, List<(int goodsId, long count)> rewards)
        {
            try
            {
                if (!await EnsureLoaded())
                {
                    // 回退:界面开不出来也绝不吞结果(沿用修复前的 Toast 行为)。
                    TipsManager.Toast(victory ? "副本通关" : "副本失败");
                    return;
                }
                if (epoch != _openEpoch) return;

                _moduleRoot.SetActive(true);
                if (victory) OpenVictory(grade, rewards);
                else OpenFailure();
                GameLog.Info("Dungeon", "DungeonResultView opened: victory={0} grade={1} rewards={2}",
                    victory, grade, rewards.Count);
            }
            catch (System.Exception ex)
            {
                GameLog.Error("Dungeon", "DungeonResultView open failed: {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }

        private void OpenVictory(int grade, List<(int goodsId, long count)> rewards)
        {
            if (_failure != null) _failure.Hide();
            _victory.Show();
            _victory.transform.SetAsLastSibling();

            // 未接线的演出组隐藏(精确降级,见类头):经验增长/层数/击杀数/铜币评级/宝箱/倒计时/分享。
            SetActive(_victory._gp_get_exp, false);
            SetActive(_victory._gp_advanceExp, false);
            SetActive(_victory._gp_layer, false);
            SetActive(_victory._gp_kill, false);
            SetActive(_victory._gp_copper_level_effect, false);
            SetActive(_victory._gp_treasure, false);
            SetActive(_victory._gp_cost_count, false);
            SetActive(_victory.wx_share_con, false);
            SetActive(_victory._gp_btns, false); // 再次挑战/退出按钮链未接(61001 重复进本/61002 退出后续接),点击任意处关闭
            if (_victory._html_left_time != null) _victory._html_left_time.text = "";
            if (_victory._lb_tips != null) _victory._lb_tips.text = "点击任意区域关闭";

            // 评级星(grade 0-3,对标老端 _gp_stars):有星才显示星组。
            SetActive(_victory._gp_stars, grade > 0);
            SetStar(_victory._img_star0, grade >= 1);
            SetStar(_victory._img_star1, grade >= 2);
            SetStar(_victory._img_star2, grade >= 3);

            // 奖励格(真 goods 图标;无奖励显示 _lb_noreward)。
            bool hasReward = rewards.Count > 0;
            SetActive(_victory._lb_noreward, !hasReward);
            BuildRewardCells(rewards);

            BindClicks();
        }

        private void OpenFailure()
        {
            if (_victory != null) _victory.Hide();
            _failure.Show();
            _failure.transform.SetAsLastSibling();

            // 老端失败界面的"战力提升建议"(_tpl_DungeonFailureStrengItem 数据链 ConfigStrengthWay)未接线 → 隐藏列表区。
            SetActive(_failure._gp_streng, false);
            SetActive(_failure.goods_list, false);
            SetActive(_failure._img_activity, false);

            BindClicks();
        }

        private void BindClicks()
        {
            if (_clicksBound) return;
            _clicksBound = true;
            // 胜利:点击任意处关闭(对标老端 click_bg_toClose;_gp_click 为整屏点击区,兜底 _img_bg)。
            if (_victory != null)
            {
                if (_victory._gp_click != null) UIUtil.AddClick(_victory._gp_click, Close);
                if (_victory._img_bg != null) UIUtil.AddClick(_victory._img_bg, Close);
            }
            if (_failure != null)
            {
                if (_failure._btn_close != null) UIUtil.AddClick(_failure._btn_close, Close);
                if (_failure.goBtn != null) UIUtil.AddClick(_failure.goBtn, Close);
                if (_failure._img_bg != null) UIUtil.AddClick(_failure._img_bg, Close);
            }
        }

        private void BuildRewardCells(List<(int goodsId, long count)> rewards)
        {
            ClearRewardCells();
            if (_victory._tpl_CommonRewardItem == null || _victory._sc_reward == null || _victory._sc_reward.content == null)
            {
                if (rewards.Count > 0)
                {
                    GameLog.Warn("Dungeon", "DungeonVictoryView 奖励容器/模板缺失(重转 dungeonCommon LayaUI?),奖励 {0} 项无法展示", rewards.Count);
                }
                return;
            }

            RectTransform content = _victory._sc_reward.content;
            for (int i = 0; i < rewards.Count; i++)
            {
                GameObject cellGo = Object.Instantiate(_victory._tpl_CommonRewardItem, content);
                cellGo.SetActive(true);
                EquipmentItem cell = cellGo.GetComponentInChildren<EquipmentItem>(true);
                if (cell == null)
                {
                    GameLog.Warn("Dungeon", "CommonRewardItem 模板缺 EquipmentItem 组件(跑 common 绑定回填),奖励格降级隐藏");
                    Object.Destroy(cellGo);
                    return;
                }
                cell.gameObject.SetActive(true);
                cell.Show();
                RectTransform rt = (RectTransform)cellGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(i * 84f, 0f);
                cell.SetData(rewards[i].goodsId, rewards[i].count);
                _rewardCells.Add(cellGo);
            }
        }

        private async Task<bool> EnsureLoaded()
        {
            if (_moduleRoot != null && _victory != null && _failure != null) return true;

            // 引用失效(外因销毁,Unity fake-null)或上次加载失败 → 丢缓存重载(同 TaskFinishView 约定)。
            if (_loadTask != null && _loadTask.IsCompleted)
            {
                _loadTask = null;
                _moduleRoot = null;
                _victory = null;
                _failure = null;
                _clicksBound = false;
                ClearRewardCells();
            }

            if (_loadTask == null) _loadTask = LoadPrefab();
            return await _loadTask;
        }

        private async Task<bool> LoadPrefab()
        {
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("Dungeon", "DungeonResultView cannot load: Window layer missing");
                return false;
            }

            string key = GameResPath.GetUIPrefab("dungeonCommon", "DungeonCommonModule");
            _moduleRoot = await ResManager.InstantiateAsync(key, parent);
            if (_moduleRoot == null)
            {
                GameLog.Error("Dungeon", "DungeonCommonModule prefab load failed: {0}", key);
                return false;
            }

            _moduleRoot.name = "DungeonCommonModule";
            BaseView[] views = _moduleRoot.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views) v.gameObject.SetActive(false);

            _victory = _moduleRoot.GetComponentInChildren<DungeonVictoryViewBind>(true);
            _failure = _moduleRoot.GetComponentInChildren<DungeonFailureViewBind>(true);
            if (_victory == null || _failure == null)
            {
                GameLog.Error("Dungeon",
                    "DungeonCommonModule missing binds(victory={0} failure={1})。Run dungeonCommon LayaUI convert + bind backfill.",
                    _victory != null, _failure != null);
                return false;
            }

            if (_victory._tpl_CommonRewardItem != null) _victory._tpl_CommonRewardItem.SetActive(false);
            _moduleRoot.SetActive(false);
            return true;
        }

        private void ClearRewardCells()
        {
            for (int i = 0; i < _rewardCells.Count; i++)
            {
                if (_rewardCells[i] != null) Object.Destroy(_rewardCells[i]);
            }
            _rewardCells.Clear();
        }

        private static void SetActive(Component c, bool active)
        {
            if (c != null) c.gameObject.SetActive(active);
        }

        private static void SetStar(UnityEngine.UI.Image star, bool on)
        {
            if (star != null) star.gameObject.SetActive(on);
        }
    }
}
