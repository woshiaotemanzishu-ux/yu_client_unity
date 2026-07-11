using Shenxiao.Module.Core.FirstRecharge;

namespace Shenxiao.Module.Core.DragonBall
{
    /// <summary>
    /// 龙玉(龙珠)礼包数据(对标老客户端 DragonBallModel 中与主界面图标 143 相关的部分)。
    /// 本期只做「龙珠礼包」活动图标(DragonGiftIconType=143):由 14311(dragon_gift_data)驱动显隐。
    /// 龙珠本体/苍龙镇世/套装(14300-14306/14310/14312)是另一入口(module 143 面板),不是活动图标,不在本期。
    ///
    /// 老端 RefreshGiftIcon 显隐门槛(faithful):
    ///   1. GetOpenState()          —— 功能开放且非审核服;
    ///   2. dragon_gift_data.id > 0 —— 服务端有下发可购礼包;
    ///   3. config_start_nuclear[id] 存在 且 role_lv>=open_lv 且 open_day 已到 且 times_limit-buy_times>0;
    ///   4. IsDoneFirstRecharge()   —— 已完成首充。
    /// Unity 侧:1/3 中的 open_lv/open_day/审核服门由图标配置(configfunctionicon 143:open_lv=0/open_day=1/
    /// need_hide_in_alpha=true)经 ActivityIconManager.AddIconAsync→FunIsOpenByIconType 统一把控;
    /// config_start_nuclear(每活动 times_limit/自有 open_lv)未移植到 Unity,故「买满即隐」暂不精确复刻
    /// (BuyTimes 已存,待该配置移植后补 times_limit 判定)。2/4 在此模型判。
    /// </summary>
    public sealed class DragonBallModel
    {
        public static readonly DragonBallModel Instance = new DragonBallModel();
        private DragonBallModel() { }

        /// <summary>龙珠礼包活动图标类型(对标老端 DragonBallModel.DragonGiftIconType=143)。</summary>
        public const string ICON_TYPE = "143";

        // 14311 龙珠礼包数据(对标老端 dragon_gift_data / SetDragonBallGiftData)
        public int GiftId;    // 礼包活动id(config_start_nuclear 主键;0 表示无可购礼包)
        public int BuyTimes;  // 已购买次数(老端 times_limit-buy_times>0 才显示;times_limit 配置待移植)

        public void SetGiftInfo(int giftId, int buyTimes)
        {
            GiftId = giftId;
            BuyTimes = buyTimes;
        }

        /// <summary>
        /// 龙珠礼包图标开启状态(对标老端 RefreshGiftIcon 里客户端可判的部分)。
        /// id>0(有可购礼包)且 已完成首充(is_frist);等级/开服天/审核服门交由图标配置在 AddIconAsync 里判。
        /// </summary>
        public bool GetGiftIconOpenState()
        {
            return GiftId > 0 && FirstRechargeModel.Instance.IsDoneFirstRecharge();
        }

        public void Reset()
        {
            GiftId = 0;
            BuyTimes = 0;
        }
    }
}
