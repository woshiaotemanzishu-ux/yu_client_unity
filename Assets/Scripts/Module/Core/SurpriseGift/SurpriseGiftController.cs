using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.SurpriseGift
{
    /// <summary>
    /// 惊喜礼包控制器（对标老客户端 SurpriseGiftController，模块 490 / yu_server pt_490）。进游戏请求 49000;
    /// 服务端 pp_surprise_gift.handle 仅在「开服天数≥SURP_KV_OPEN_DAY 且 等级≥SURP_KV_OPEN_LEV」时才回 49000
    /// (否则 skip 不回包),故据回包 code 增删主界面图标 490(GetEntranceOpenState:code==成功 显示);
    /// AddIconAsync 再走图标配置 open_lv/open_day 二次门判(对标老端 ActivityIconManager.addIcon 的门槛)。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)复请求 49000(对标老端 CHANGE_LEVEL→LoadConfig→请求),让升到开启等级后
    /// 图标及时出现。抽奖/购买/翻牌/刷新(49001-49004)本期只解析落 <see cref="SurpriseGiftModel"/> 并发
    /// EVT_SURPRISE_GIFT_UPDATE,红点/翻牌/面板 UI 待用户验收。
    /// </summary>
    public sealed class SurpriseGiftController : BaseController
    {
        public static readonly SurpriseGiftController Instance = new SurpriseGiftController();
        private SurpriseGiftController() { }

        public const string ICON_TYPE = SurpriseGiftModel.ICON_TYPE;

        // 服务端 ?SUCCESS(=1);49000 回包 code==SUCCESS 表示活动开启(对标老端 On49000: scmd.code != 1 → 报错)。
        private const int SUCCESS = 1;

        // 复请求 49000 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.SURPRISE_GIFT_INFO, On49000);
            RegisterProtocal(Proto.SURPRISE_GIFT_DRAW, On49001);
            RegisterProtocal(Proto.SURPRISE_GIFT_TURN, On49002);
            RegisterProtocal(Proto.SURPRISE_GIFT_BUY, On49003);
            RegisterProtocal(Proto.SURPRISE_GIFT_REFRESH, On49004);
            // 对标老端 CHANGE_LEVEL→LoadConfig→请求 49000:等级变化时复请求(升到开启等级后图标出现)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 对标老端 SurpriseGiftController.ts:53:DAY_CHANGE→game_start(=LoadConfig,SurpriseGiftModel.ts:63-72
            // 内 getOpenStatus() 通过才 Fire 49000)。与 OnRoleInfoUpdate 同一简化口径:服务端已按开服天/等级
            // 把控(未开则不回包),故无条件复请求,不复刻客户端侧 getOpenStatus() 预判。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, RequestStartup);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, RequestStartup);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            SurpriseGiftModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求 49000(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START→请求)。</summary>
        public void RequestStartup() => SendFmt(Proto.SURPRISE_GIFT_INFO);

        // 面板/视图交互入口(对标老端 SCMD_REQUEST 转发)。read(49000/49002,_)->{ok,[]} 无参裸发;
        // read(49001/49003,GiftId:32) 带一个 i。
        public void RequestInfo() => SendFmt(Proto.SURPRISE_GIFT_INFO);
        public void Draw(int giftId) => SendFmt(Proto.SURPRISE_GIFT_DRAW, "i", giftId);
        public void Turn() => SendFmt(Proto.SURPRISE_GIFT_TURN);
        public void Buy(int giftId) => SendFmt(Proto.SURPRISE_GIFT_BUY, "i", giftId);

        // 49000: Code:i, FreeDrawTimes:i, AddDrawTimes:i, UseFreeTimes:i, DrawTimes:i, TurnId:i,
        //        DrawList[u16×{DrawId:i}], GiftList[u16×{GiftId:c, BuyTime:i, BackDay:h}], DayTaskList[u16×{TaskId:c, State:c}]
        private void On49000(NetReader r)
        {
            int code = (int)r.ReadU32();
            int free = (int)r.ReadU32();
            int add = (int)r.ReadU32();
            int useFree = (int)r.ReadU32();
            int drawTimes = (int)r.ReadU32();
            int turnId = (int)r.ReadU32();

            int n = r.ReadU16();
            var drawList = new List<int>(n);
            for (int i = 0; i < n; i++) drawList.Add((int)r.ReadU32());

            n = r.ReadU16();
            var giftList = new List<SurpriseGiftModel.GiftItem>(n);
            for (int i = 0; i < n; i++) giftList.Add(new SurpriseGiftModel.GiftItem(r.ReadU8(), (int)r.ReadU32(), r.ReadU16()));

            var dayTasks = ReadTasks(r);

            bool open = code == SUCCESS;
            SurpriseGiftModel m = SurpriseGiftModel.Instance;
            m.SetInfo(open, free, add, useFree, drawTimes, turnId, drawList, giftList, dayTasks);

            // 对标老端:getOpenStatus() 通过则 addIcon(490)。服务端已按开服天数/等级把控(未开则不回包),
            // AddIconAsync 再据图标配置 open_lv/open_day 二次门判。
            if (m.GetEntranceOpenState()) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("SurpriseGift", "49000 信息: code={0} free={1} 抽过={2} 礼包={3} 日任务={4} open={5}",
                code, free, drawList.Count, giftList.Count, dayTasks.Count, open);
            EventDispatcher.Emit(GlobalEvent.EVT_SURPRISE_GIFT_UPDATE);
        }

        // 49001: Code:i, GiftId:i(购买结果)。老端只刷红点/弹提示,不动图标增删。
        private void On49001(NetReader r)
        {
            int code = (int)r.ReadU32();
            int giftId = (int)r.ReadU32();
            GameLog.Info("SurpriseGift", "49001 抽奖: code={0} giftId={1}", code, giftId);
            EventDispatcher.Emit(GlobalEvent.EVT_SURPRISE_GIFT_UPDATE);
        }

        // 49002: Code:i, TurnId:h, GiftId:h, UseFreeTimes:i(翻牌结果)。老端只刷红点,不动图标增删。
        private void On49002(NetReader r)
        {
            int code = (int)r.ReadU32();
            int turnId = r.ReadU16();
            int giftId = r.ReadU16();
            int useFree = (int)r.ReadU32();
            GameLog.Info("SurpriseGift", "49002 翻牌: code={0} turnId={1} giftId={2} useFree={3}", code, turnId, giftId, useFree);
            EventDispatcher.Emit(GlobalEvent.EVT_SURPRISE_GIFT_UPDATE);
        }

        // 49003: Code:i, GiftId:i(领取返利结果)。老端只刷红点,不动图标增删。
        private void On49003(NetReader r)
        {
            int code = (int)r.ReadU32();
            int giftId = (int)r.ReadU32();
            GameLog.Info("SurpriseGift", "49003 购买: code={0} giftId={1}", code, giftId);
            EventDispatcher.Emit(GlobalEvent.EVT_SURPRISE_GIFT_UPDATE);
        }

        // 49004: FreeDrawTimes:i, AddDrawTimes:i, UseFreeTimes:i, DayTaskList[u16×{TaskId:c, State:c}](刷新推送)。
        private void On49004(NetReader r)
        {
            int free = (int)r.ReadU32();
            int add = (int)r.ReadU32();
            int useFree = (int)r.ReadU32();
            var dayTasks = ReadTasks(r);
            SurpriseGiftModel.Instance.SetRefresh(free, add, useFree, dayTasks);
            EventDispatcher.Emit(GlobalEvent.EVT_SURPRISE_GIFT_UPDATE);
        }

        // 对标老端:主角等级变化复请求 49000(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        private static List<SurpriseGiftModel.TaskItem> ReadTasks(NetReader r)
        {
            int n = r.ReadU16();
            var list = new List<SurpriseGiftModel.TaskItem>(n);
            for (int i = 0; i < n; i++) list.Add(new SurpriseGiftModel.TaskItem(r.ReadU8(), r.ReadU8()));
            return list;
        }
    }
}
