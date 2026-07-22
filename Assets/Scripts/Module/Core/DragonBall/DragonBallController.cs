using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.DragonBall
{
    /// <summary>
    /// 龙玉(龙珠)礼包控制器(对标老客户端 DragonBallController,模块 143)。进游戏请求 14311;
    /// 回包据 dragon_gift_data(id/buy_times)增删主界面「龙珠礼包」图标 143(DragonGiftIconType)。
    /// 图标显隐由 DragonBallModel 依据功能开放、alpha、config_start_nuclear、角色/开服状态、限购与首充完整判定，
    /// AddIconAsync 的公共图标配置门作为二次保险。等级变化仅在新等级精确命中表内 open_lv 时复请求14311。
    ///
    /// 本期只做礼包图标:龙珠本体/苍龙镇世/套装(14300-14306/14310/14312)是另一入口(面板)不在本期;
    /// 首充更新只按已缓存14311本地复评；开服日变化重拉14311，均与老端一致。
    /// </summary>
    public sealed class DragonBallController : BaseController
    {
        public static readonly DragonBallController Instance = new DragonBallController();
        private DragonBallController() { }

        public const string ICON_TYPE = DragonBallModel.ICON_TYPE;

        // 复请求 14311 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;
        private int _generation;
#if UNITY_EDITOR
        private static System.Func<byte[], bool> s_outboundIntercept;
#endif

        protected override void Register()
        {
            RegisterProtocal(Proto.DRAGONBALL_GIFT_INFO, On14311);
            // 对标老端 CHANGE_LEVEL→复发 14311:等级变化时复请求(到达礼包 open_lv 后图标出现)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 对标老端 DragonBallController 首充更新→RefreshGiftIcon:图标条件依赖 IsDoneFirstRecharge(),
            // 而 14311 常先于首充数据(15905/15908)到达,此刻判"未首充"→图标被隐藏且不再复判。
            // 订阅首充更新事件,数据到达后按存下的 GiftId 复判图标(无需重发 14311),兜住这个时序。
            EventDispatcher.On(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE, OnFirstRechargeUpdate);
            // 对标老端 DragonBallController.ts:114-116:DAY_CHANGE→SendFmtToGame(14311),跨天复请求礼包数据。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, RequestStartup);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE, OnFirstRechargeUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, RequestStartup);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            DragonBallModel.Instance.Reset();
            _lastLevel = -1;
            _generation++;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 14311)。</summary>
        public void RequestStartup()
        {
            SendEmpty(Proto.DRAGONBALL_GIFT_INFO);
        }

        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            if (s_outboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(protoId, null, null);
                if (s_outboundIntercept(frame)) return;
            }
#endif
            SendFmt(protoId);
        }

        // 14311: id:i, buy_times:h(对标 pt_143.erl write(14311,[Id:32, BuyTimes:16]))。请求无参(read(14311,_)->{ok,[]})。
        private void On14311(NetReader r)
        {
            int giftId = (int)r.ReadU32();
            int buyTimes = r.ReadU16();

            DragonBallModel m = DragonBallModel.Instance;
            m.SetGiftInfo(giftId, buyTimes);

            _ = RefreshGiftIconWhenConfigReady();
        }

        private async Task RefreshGiftIconWhenConfigReady()
        {
            int generation = _generation;
            await DragonBallConfigs.EnsureLoaded();
            await FuncOpenConfig.EnsureLoaded();
            if (generation != _generation) return;
            bool open = RefreshGiftIcon();
            DragonBallModel m = DragonBallModel.Instance;
            GameLog.Info("DragonBall", "14311 龙珠礼包: id={0} buy_times={1} open={2}",
                m.GiftId, m.BuyTimes, open);
        }

        private bool RefreshGiftIcon()
        {
            bool open = DragonBallModel.Instance.GetGiftIconOpenState();
            if (open) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            return open;
        }

        // 对标老端:主角等级变化复请求 14311(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            if (DragonBallConfigs.IsLoaded && DragonBallConfigs.HasOpenLevel(role.Level)) RequestStartup();
            RefreshGiftIcon();
        }

        // 对标老端 首充完成/更新→RefreshGiftIcon:首充态变化后,按已存 GiftId 复判「龙珠礼包」图标显隐
        // (无需重发 14311——礼包数据已在,只是首充门槛此刻才满足/解除)。
        private void OnFirstRechargeUpdate()
        {
            RefreshGiftIcon();
        }
    }
}
