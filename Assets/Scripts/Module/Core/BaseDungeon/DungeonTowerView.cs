using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.DungeonTower;

namespace Shenxiao.Module.Core.BaseDungeon
{
    /// <summary>
    /// 现有 DungeonTowerView Prefab 的业务接管层。当前只消费 61117/61118 权威字段；
    /// 关卡曲线列表依赖缺失的 config_limit_tower_round/config_dungeon_grade，禁止以硬编码假数据补齐。
    /// </summary>
    public sealed class DungeonTowerView : DungeonTowerViewBind
    {
        private long _nextSecond;

        protected override void OnInit()
        {
            if (_box_click != null) UIUtil.AddClick(_box_click, OnBigRewardClick);
            if (_btn_c != null) UIUtil.AddClick(_btn_c, OnChallengeClick);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_CommonRewardItem != null) _tpl_CommonRewardItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            BaseDungeonModel.Instance.TowerInfoChanged += Refresh;
            Refresh();
        }

        protected override void OnHide()
        {
            BaseDungeonModel.Instance.TowerInfoChanged -= Refresh;
        }

        protected override void OnDispose()
        {
            BaseDungeonModel.Instance.TowerInfoChanged -= Refresh;
        }

        private void Update()
        {
            if (!IsShown || TimeUtil.NowSec() < _nextSecond) return;
            _nextSecond = TimeUtil.NowSec() + 1;
            RefreshCountdown();
        }

        private void Refresh()
        {
            BaseDungeonModel model = BaseDungeonModel.Instance;
            int passed = model.PassedDungeonIds.Count;

            if (_lb_4 != null) _lb_4.text = "第" + passed + "关";
            // 总关数属于 config_limit_tower_round；该配置未入 Unity 前不显示伪造分母/进度。
            if (_lb_2 != null) _lb_2.text = passed.ToString();
            if (_progress_value != null) _progress_value.gameObject.SetActive(false);

            bool canClaim = model.RewardMode == 1;
            bool claimed = model.RewardMode == 2;
            if (_box_click != null) _box_click.gameObject.SetActive(canClaim || claimed);
            if (_img_receive != null) _img_receive.gameObject.SetActive(canClaim);
            if (_img_got != null) _img_got.gameObject.SetActive(claimed);

            // 尚无权威关卡目录，清空转换快照中的旧选择，避免烤制账号数据冒充当前玩家状态。
            if (_lb_idx != null) _lb_idx.text = string.Empty;
            if (_html_desc != null) _html_desc.text = string.Empty;
            if (_html_desc2 != null) _html_desc2.text = string.Empty;
            if (_btn_c != null) _btn_c.gameObject.SetActive(false);
            if (img_redAc != null) img_redAc.gameObject.SetActive(false);

            int round = model.Round <= 0 ? 1 : model.Round;
            if (_img_bg != null)
                _ = ResManager.SetImageAsync(_img_bg,
                    GameResPath.GetIconOtherPath("dungeontower", "act_box_bg_" + round), nativeSize: false);
            if (_img_title != null)
                _ = ResManager.SetImageAsync(_img_title,
                    GameResPath.GetIcon("dungeontower", "act_title_" + round), nativeSize: true);
            if (_img_b_bg != null)
                _ = ResManager.SetImageAsync(_img_b_bg,
                    GameResPath.GetIconOtherPath("dungeontower", "tower_bg"), nativeSize: false);

            RefreshCountdown();
        }

        private void RefreshCountdown()
        {
            if (_lb_time2 == null) return;
            long left = BaseDungeonModel.Instance.OverTime - TimeUtil.NowSec();
            if (left < 0) left = 0;
            long hours = left / 3600;
            long minutes = (left % 3600) / 60;
            long seconds = left % 60;
            _lb_time2.text = hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        private static void OnBigRewardClick()
        {
            BaseDungeonModel model = BaseDungeonModel.Instance;
            if (model.RewardMode == 1) BaseDungeonController.Instance.RequestTowerBigReward();
            else GameLog.Info("BaseDungeon", "限时塔大奖当前不可领取 reward_mode={0}", model.RewardMode);
        }

        private static void OnChallengeClick()
        {
            GameLog.Info("BaseDungeon", "限时塔挑战需关卡配置 + Dungeon 61001 进入链，登记跨岛 blocker");
        }
    }
}
