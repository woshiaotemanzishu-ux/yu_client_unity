using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.FirstRecharge;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.AddVipService
{
    /// <summary>
    /// 关注有礼控制器(对标老客户端 AddVipServiceModel.CheckIconOpen,由首充 FirstRechargeController.On15908 驱动)。
    /// 据 GetEntranceOpenState(!IsAlpha && 首充 is_buy==1 && 渠道白名单命中)增删主界面图标 114。
    ///
    /// 驱动协议 = 首充 15908(is_buy)。★不在此重注册 15908★:Unity NetManager 每协议仅一处理器
    /// (RegisterProtocal 覆盖式),15908 已由 FirstRechargeController 独占并解析进 FirstRechargeModel.IsBuy,
    /// 若此处再注册会把首充图标(158/159)处理逻辑覆盖掉。故改订阅其解析后广播的 EVT_FIRST_RECHARGE_UPDATE,
    /// 读 FirstRechargeModel.IsBuy 即可(与老端"On15908→CheckIconOpen"等价)。15908 的请求已由
    /// FirstRechargeController.RequestStartupState() 在进游戏时发过,本控制器无需再发。
    ///
    /// 等级变化(EVT_ROLE_INFO_UPDATE,去抖)复评图标以对齐模板;114 本身无等级门,此处仅作复检钩子。
    /// 本期只做图标;关注有礼面板/微信收藏/领奖等未移植,待用户验收。
    /// </summary>
    public sealed class AddVipServiceController : BaseController
    {
        public static readonly AddVipServiceController Instance = new AddVipServiceController();
        private AddVipServiceController() { }

        public const string ICON_TYPE = AddVipServiceModel.ICON_TYPE;

        // 复评图标的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时复评。
        private int _lastLevel = -1;

        protected override void Register()
        {
            // 驱动源:首充 15908 解析(is_buy)后广播的更新事件(对标老端 On15908→CheckIconOpen)。
            EventDispatcher.On(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE, OnFirstRechargeUpdate);
            // 模板对齐:等级变化复评(114 无等级门,仅复检钩子)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE, OnFirstRechargeUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            AddVipServiceModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>
        /// 进游戏钩子(可由 GameStartController.RequestStartupPackets 调用)。驱动协议 15908 已由
        /// FirstRechargeController.RequestStartupState() 请求,此处无需发包,仅按当前状态复评一次图标
        /// (此刻多半 is_buy 未到、复评为空;真正驱动在 15908 回包后的 EVT_FIRST_RECHARGE_UPDATE)。
        /// </summary>
        public void RequestStartup()
        {
            RefreshIcon("startup");
        }

        // 首充 15908(is_buy)解析后广播:对标老端 On15908,is_buy==1 → is_show=true → CheckIconOpen。
        private void OnFirstRechargeUpdate()
        {
            RefreshIcon("15908");
        }

        private void RefreshIcon(string from)
        {
            AddVipServiceModel m = AddVipServiceModel.Instance;
            // 对标老端:首充已购(is_buy==1)则置显示位,一旦置真不回落(Reset 才清)。
            if (FirstRechargeModel.Instance.IsBuy) m.IsShowAddVipServiceIcon = true;

            bool open = m.GetEntranceOpenState();
            // 走 AddIconAsync 过一遍图标配置门(与老端 addIcon 一致);渠道/审核/is_buy 门在 GetEntranceOpenState。
            if (open) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("AddVipService", "{0} 关注有礼: isBuy={1} showFlag={2} open={3}",
                from, FirstRechargeModel.Instance.IsBuy, m.IsShowAddVipServiceIcon, open);
        }

        // 模板对齐:主角等级变化(去抖)复评图标(114 无等级门,复检无害)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RefreshIcon("levelChange");
        }
    }
}
