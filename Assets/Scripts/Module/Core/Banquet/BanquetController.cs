using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Banquet
{
    /// <summary>
    /// 婚宴(婚礼)控制器(对标老客户端 BanquetController,模块 172)。巨型婚宴系统本期只做主界面两个图标,
    /// 只注册图标相关协议、只做 add/del,婚宴报名/邀请/场景玩法一律不移植。
    /// 进游戏请求 17249(婚礼状态)与 17256(婚礼召集),回包据条件增删图标:
    ///   17249 now_wedding_state==2 → AddIcon(172@2);上次==2 且这次==0 → DeleteIcon(172@2)(对标老端 On17249 的 tri-state)。
    ///   17256 type==1 且 wedding_list 非空 → AddIcon(172@1);否则 DeleteIcon(172@1)(对标老端 SetBanquetCall)。
    /// 老端 172@1 的触发在 SetBanquetCall(由 17256 驱动),原逻辑还按 start_time 延时上图标、按主角是否新人切文案,
    /// 那些属玩法呈现;图标层面等价于「婚礼活动开着就显示」,故本期简化为随激活态即时增删。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)复请求(172@1 配置 open_lv=130,升到 130 级后图标才能过 AddIcon 的门),
    /// 走 _lastLevel 去抖只在等级真变时重发。
    /// </summary>
    public sealed class BanquetController : BaseController
    {
        public static readonly BanquetController Instance = new BanquetController();
        private BanquetController() { }

        public const string ICON_TYPE_WEDDING = BanquetModel.ICON_TYPE_WEDDING;
        public const string ICON_TYPE_GUEST = BanquetModel.ICON_TYPE_GUEST;

        // 复请求的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.BANQUET_WEDDING_STATE, On17249);
            RegisterProtocal(Proto.BANQUET_CALL, On17256);
            // 对标老端 CHANGE_LEVEL 复算图标:等级变化时复请求(172@1 需 130 级)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_WEDDING);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_GUEST);
            BanquetModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 17249/17256)。</summary>
        public void RequestStartup()
        {
            // read(17249,_)->{ok,[]} / read(17256,_)->{ok,[]}:请求均无字段,裸发。
            // 17250(报名视图数据)属玩法,图标不需,不发。
            SendFmt(Proto.BANQUET_WEDDING_STATE);
            SendFmt(Proto.BANQUET_CALL);
        }

        // 17249: now_wedding_state:c, begin_time:i(对标老端 On17249)
        private void On17249(NetReader r)
        {
            int nowState = r.ReadU8();
            int beginTime = (int)r.ReadU32();

            BanquetModel m = BanquetModel.Instance;
            // 对标老端 On17249:state==2 加 172@2;上次==2 且这次==0 才删;其余状态图标不动。
            if (nowState == 2) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE_GUEST);
            else if (m.BanqState == 2 && nowState == 0) ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_GUEST);

            m.NowWeddingState = nowState;
            m.BeginTime = beginTime;
            m.BanqState = nowState; // 记录本次状态供下次 tri-state 判定(对标 banquetModel.banqState = scmd.now_wedding_state)

            GameLog.Info("Banquet", "17249 婚礼状态: now_wedding_state={0} begin_time={1} guestOpen={2}",
                nowState, beginTime, m.GetGuestIconOpen());
        }

        // 17256: type:c, wedding_list[u16 × item_to_bin_24]
        //   item_to_bin_24 = { wedding_type:c, start_time:i,
        //     man_list[u16 × role], woman_list[u16 × role], guest_list[u16 × { role_id:l }] }
        // 图标只看 type==1 且 wedding_list 非空(对标老端 SetBanquetCall);整包仍按字节吃干净。
        private void On17256(NetReader r)
        {
            int type = r.ReadU8();
            int weddingCount = r.ReadU16();
            for (int i = 0; i < weddingCount; i++)
            {
                r.ReadU8();          // wedding_type
                r.ReadU32();         // start_time(老端算倒计时文本用,本期图标只做显隐)
                ReadRoleList(r);     // man_list
                ReadRoleList(r);     // woman_list
                int guestCount = r.ReadU16();
                for (int g = 0; g < guestCount; g++) r.ReadU64(); // guest role_id
            }

            BanquetModel m = BanquetModel.Instance;
            m.WeddingActive = (type == 1 && weddingCount > 0);
            if (m.GetWeddingIconOpen()) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE_WEDDING);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_WEDDING);

            GameLog.Info("Banquet", "17256 婚礼召集: type={0} weddings={1} weddingOpen={2}",
                type, weddingCount, m.GetWeddingIconOpen());
        }

        // item_to_bin_25/26(man_list/woman_list 元素一致):
        //   role_id:l, name:s, lv:h, combat_power:l, sex:c, vip:i, career:c, turn:c, picture:s, picture_ver:i
        // 图标不需要这些字段,仅按字节顺序读掉以对齐包体。
        private static void ReadRoleList(NetReader r)
        {
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                r.ReadU64();    // role_id
                r.ReadString(); // name
                r.ReadU16();    // lv
                r.ReadU64();    // combat_power
                r.ReadU8();     // sex
                r.ReadU32();    // vip
                r.ReadU8();     // career
                r.ReadU8();     // turn
                r.ReadString(); // picture
                r.ReadU32();    // picture_ver
            }
        }

        // 对标老端 CHANGE_LEVEL:主角等级变化复请求(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }
    }
}
