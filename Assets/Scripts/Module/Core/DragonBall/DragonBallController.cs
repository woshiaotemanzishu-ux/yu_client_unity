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
    /// 龙玉(龙珠)控制器(对标老客户端 DragonBallController,模块 143)。进游戏依次请求 14310、14303、14311;
    /// 回包据 dragon_gift_data(id/buy_times)增删主界面「龙珠礼包」图标 143(DragonGiftIconType)。
    /// 图标显隐由 DragonBallModel 依据功能开放、alpha、config_start_nuclear、角色/开服状态、限购与首充完整判定，
    /// AddIconAsync 的公共图标配置门作为二次保险。等级变化仅在新等级精确命中表内 open_lv 时复请求14311。
    ///
    /// 14310 保存雕像快照，14303 保存套装概览，14300 保存龙珠本体列表；
    /// 激活/升级/穿戴/苍龙镇世等操作链(14301-14302/14304-14306/14312)仍不在本期;
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
            RegisterProtocal(Proto.DRAGONBALL_STATUE_OVERVIEW, On14310);
            RegisterProtocal(Proto.DRAGONBALL_SUIT_INFO, On14303);
            RegisterProtocal(Proto.DRAGONBALL_LIST, On14300);
            RegisterProtocal(Proto.DRAGONBALL_GIFT_INFO, On14311);
            // 对标老端 CHANGE_LEVEL→复发 14311:等级变化时复请求(到达礼包 open_lv 后图标出现)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 对标老端 DragonBallController 首充更新→RefreshGiftIcon:图标条件依赖 IsDoneFirstRecharge(),
            // 而 14311 常先于首充数据(15905/15908)到达,此刻判"未首充"→图标被隐藏且不再复判。
            // 订阅首充更新事件,数据到达后按存下的 GiftId 复判图标(无需重发 14311),兜住这个时序。
            EventDispatcher.On(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE, OnFirstRechargeUpdate);
            // 对标老端 DragonBallController.ts:114-116:DAY_CHANGE→SendFmtToGame(14311),跨天复请求礼包数据。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_FIRST_RECHARGE_UPDATE, OnFirstRechargeUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnDayChange);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            DragonBallModel.Instance.Reset();
            _lastLevel = -1;
            _generation++;
            base.Dispose();
        }

        /// <summary>进游戏请求：对标老端严格依次空发 14310、14303、14311。</summary>
        public void RequestStartup()
        {
            RequestStatueOverview();
            RequestSuitInfo();
            RequestGiftInfo();
        }

        /// <summary>14310 严格空包，获取龙珠雕像总览快照。</summary>
        public void RequestStatueOverview() => SendEmpty(Proto.DRAGONBALL_STATUE_OVERVIEW);
        /// <summary>14303 严格空包，获取龙珠套装概览。</summary>
        public void RequestSuitInfo() => SendEmpty(Proto.DRAGONBALL_SUIT_INFO);
        /// <summary>14300 严格空包；雕像 inactive→active 边沿补取本体列表，服务端也会主动刷新本包。</summary>
        public void RequestDragonList() => SendEmpty(Proto.DRAGONBALL_LIST);
        /// <summary>14311 严格空包，获取龙珠礼包购买快照。</summary>
        public void RequestGiftInfo() => SendEmpty(Proto.DRAGONBALL_GIFT_INFO);

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

        // 14310: status:c,power:l。仅 inactive→active 边沿补拉 14300。
        private void On14310(NetReader r)
        {
            DragonBallModel model = DragonBallModel.Instance;
            byte oldStatus = model.StatueStatus;
            byte status = r.ReadU8();
            model.SetStatueOverview(status, unchecked((ulong)r.ReadU64()));
            if (status == 1 && oldStatus != 1) RequestDragonList();
        }

        // 14300: count:h,{dragon_id:i,dragon_lv:h,power:l,next_power:l}；按 id upsert。
        private void On14300(NetReader r)
        {
            int count = r.ReadU16();
            var entries = new System.Collections.Generic.List<DragonBallModel.BallEntry>(count);
            for (int i = 0; i < count; i++)
                entries.Add(new DragonBallModel.BallEntry(r.ReadU32(), r.ReadU16(), unchecked((ulong)r.ReadU64()), unchecked((ulong)r.ReadU64())));
            DragonBallModel.Instance.SetBallData(entries);
        }

        // 14303: wear_type:c,count:h,{type:c,lv:c,power:l,next_power:l}；按 type upsert。
        private void On14303(NetReader r)
        {
            byte wearType = r.ReadU8();
            int count = r.ReadU16();
            var entries = new System.Collections.Generic.List<DragonBallModel.SuitEntry>(count);
            for (int i = 0; i < count; i++)
                entries.Add(new DragonBallModel.SuitEntry(r.ReadU8(), r.ReadU8(), unchecked((ulong)r.ReadU64()), unchecked((ulong)r.ReadU64())));
            DragonBallModel.Instance.SetSuitData(wearType, entries);
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
            if (DragonBallConfigs.IsLoaded && DragonBallConfigs.HasOpenLevel(role.Level)) RequestGiftInfo();
            RefreshGiftIcon();
        }

        // 对标老端 首充完成/更新→RefreshGiftIcon:首充态变化后,按已存 GiftId 复判「龙珠礼包」图标显隐
        // (无需重发 14311——礼包数据已在,只是首充门槛此刻才满足/解除)。
        private void OnFirstRechargeUpdate()
        {
            RefreshGiftIcon();
        }

        // 跨天只刷新礼包购买次数，不能重复请求雕像总览。
        private void OnDayChange() => RequestGiftInfo();
    }
}
