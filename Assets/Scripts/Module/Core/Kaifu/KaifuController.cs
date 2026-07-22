using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Tasks;

namespace Shenxiao.Module.Core.Kaifu
{
    /// <summary>
    /// 开服活动控制器(对标老客户端 KaifuActivityController)。进游戏请求 42004(投资开启列表)与 42401(契约之书信息),
    /// 据回包各自增删主界面图标(逐个 add/del):
    ///   42004 → 4205 巅峰投资 / 1112 超值投资(对标老端 On42004→InvestOpen);
    ///   42401 → 424 / 424@1 契约之书(对标老端 On42401→SetBookInfoData→AddBookIcon)。
    /// 这三个图标在 configfunctionicon 里均 controll_by_own_fun=true 且 open_lv 很高(4205=360/1112=132/424=160),
    /// 通用扫描(RefreshDefaultIconsCoreAsync)会跳过、AddIconAsync 又会被 open_lv 拦掉;而老端 addIcon 不校验 open_lv
    /// (FunIsOpenByIconType 恒真),门判全由本模块自管——故一律走 ActivityIconManager.AddOwnerIcon(绕过 open_lv 门)。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)复请求(本端加强,老端无对应 CHANGE_LEVEL 绑定),让达到开启等级后图标及时出现。
    /// 跨天(轮20 实接,EVT_SERVER_DAY_CHANGE)对标老端 KaifuActivityController.ts:56,**仅**复发 42004
    /// (不带 42401——老端该行只有一句 SendFmtToGame(42004));⚠同文件 :84-86 是另一段被注释掉的
    /// AddBookIcon 死链订阅,与本条无关,勿混淆/勿接(见 OnServerDayChange)。
    /// 本期只做图标;投资数据/领取(42000-42003)与契约之书面板(42402-42406)均属面板,未移植,待用户验收。
    /// </summary>
    public sealed class KaifuController : BaseController
    {
        public static readonly KaifuController Instance = new KaifuController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_investInfoOutboundIntercept;
#endif
        private KaifuController() { }

        public const string ICON_INVEST_TOP = KaifuModel.ICON_INVEST_TOP;     // 4205 巅峰投资
        public const string ICON_INVEST_VALUE = KaifuModel.ICON_INVEST_VALUE; // 1112 超值投资
        public const string ICON_BOOK = KaifuModel.ICON_BOOK;                 // 424 契约之书
        public const string ICON_BOOK_1 = KaifuModel.ICON_BOOK_1;             // 424@1 契约之书(8天档)

        // 复请求的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.KAIFU_INVEST_OPEN, On42004);
            RegisterProtocal(Proto.KAIFU_INVEST_INFO, On42001);
            RegisterProtocal(Proto.KAIFU_BOOK_INFO, On42401);
            RegisterProtocal(Proto.KAIFU_INVEST_ERROR, On42000);
            // 本端加强:等级变化时复请求 42004/42401,让达到开启等级后图标及时出现。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 对标老端 DAY_CHANGE→SendFmtToGame(42004)(KaifuActivityController.ts:56)。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            ActivityIconManager.Instance.DeleteIcon(ICON_INVEST_TOP);
            ActivityIconManager.Instance.DeleteIcon(ICON_INVEST_VALUE);
            ActivityIconManager.Instance.DeleteIcon(ICON_BOOK);
            ActivityIconManager.Instance.DeleteIcon(ICON_BOOK_1);
            KaifuModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用)。</summary>
        public void RequestStartup()
        {
            // read(42004,_)->{ok,[]}:裸发(对标老端 GAME_START 后 SendFmtToGame(42004))。
            SendFmt(Proto.KAIFU_INVEST_OPEN);
            // read(42401,_)->{ok,[]}:裸发,拉契约之书信息驱动 424 图标(老端此请求被注释,这里复活以让 424 生效)。
            SendFmt(Proto.KAIFU_BOOK_INFO);
        }

        /// <summary>42000 开服投资(pt_420)家族统一错误出口(轮22 族错误出口批;对标老端
        /// KaifuActivityController.ts:161-164 On42000:无条件 ErrorCodeShow(code,args)。服务端
        /// send_error/2(lib_investment.erl:413-417)是投资相关多处失败分支共享的错误壳,回包恒为错误码)。
        /// 错误码表/args 格式化未移植,显码降级。</summary>
        private void On42000(NetReader r)
        {
            int code = (int)r.ReadU32();
            string args = r.ReadString();
            TipsManager.Toast("操作失败(" + code + ")");
            GameLog.Warn("Kaifu", "42000 投资家族错误壳 code={0} args={1}", code, args);
        }

        public void RequestInvestInfo(byte type)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.KAIFU_INVEST_INFO, "c", new object[] { type });
            if (s_investInfoOutboundIntercept != null && s_investInfoOutboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.KAIFU_INVEST_INFO, "c", type);
        }

        private void On42001(NetReader reader)
        {
            byte type = reader.ReadU8();
            ushort curLv = reader.ReadU16();
            uint buyTime = reader.ReadU32();
            uint getTime = reader.ReadU32();
            ushort loginDays = reader.ReadU16();
            int count = reader.ReadU16();
            var rewards = new List<KaifuModel.InvestRewardEntry>(count);
            for (int i = 0; i < count; i++)
            {
                rewards.Add(new KaifuModel.InvestRewardEntry(reader.ReadU8(), reader.ReadU16()));
            }

            KaifuModel.Instance.ReplaceInvestInfo(type, curLv, buyTime, getTime, loginDays, rewards);
        }

        // 42004: open_list[u16 count × { type:c, show_id:h, state:c, refresh_time:i }](对标 pt_420 item_to_bin_1)
        private void On42004(NetReader r)
        {
            int count = r.ReadU16();
            var items = new List<KaifuModel.InvestItem>(count);
            for (int i = 0; i < count; i++)
            {
                items.Add(new KaifuModel.InvestItem
                {
                    Type = r.ReadU8(),
                    ShowId = r.ReadU16(),
                    State = r.ReadU8(),
                    RefreshTime = r.ReadU32(),
                });
            }

            KaifuModel.Instance.SetInvestOpen(items);
            _ = RefreshInvestIconsAsync();

            GameLog.Info("Kaifu", "42004 投资开启: count={0} top(4205)={1} value(1112)={2}",
                count, KaifuModel.Instance.ShowTopIcon, KaifuModel.Instance.ShowValueIcon);
        }

        // 42401: list[u16 count × { chapter:c, status:c, stage_list[u16 × { stage:c, progress:i, status:c, extra:s }] }]
        // (对标 pt_424 item_to_bin_0/1)。图标只关心:是否有活动数据、是否已全部领取(任何 status!=2 即未领完)。
        private void On42401(NetReader r)
        {
            int count = r.ReadU16();
            bool hasData = count > 0;
            bool anyUnclaimed = false;
            for (int i = 0; i < count; i++)
            {
                r.ReadU8();                 // chapter
                int chapterStatus = r.ReadU8();
                if (chapterStatus != 2) anyUnclaimed = true;

                int stageLen = r.ReadU16();
                for (int j = 0; j < stageLen; j++)
                {
                    r.ReadU8();             // stage
                    r.ReadU32();            // progress(面板进度,图标不需)
                    int stageStatus = r.ReadU8();
                    r.ReadString();         // extra(面板用,读掉保证游标对齐)
                    if (stageStatus != 2) anyUnclaimed = true;
                }
            }

            // 对标老端 finishAll() 的图标相关近似:全部 status==2 视为已领完(面板未移植,不逐章比对配置阶段数)。
            KaifuModel.Instance.SetBookInfo(hasData, hasData && !anyUnclaimed);
            _ = RefreshBookIconsAsync();

            GameLog.Info("Kaifu", "42401 契约之书: chapters={0} open(424)={1}",
                count, KaifuModel.Instance.GetBookIconOpen());
        }

        // 4205/1112 走 AddOwnerIcon(绕过 open_lv 门,门判已在 KaifuModel.SetInvestOpen 里算好),对标老端 addIcon 不校验 open_lv。
        private async Task RefreshInvestIconsAsync()
        {
            await MainUIConfigs.EnsureLoaded();
            KaifuModel m = KaifuModel.Instance;

            if (m.ShowTopIcon) ActivityIconManager.Instance.AddOwnerIcon(ICON_INVEST_TOP);
            else ActivityIconManager.Instance.DeleteIcon(ICON_INVEST_TOP);

            if (m.ShowValueIcon) ActivityIconManager.Instance.AddOwnerIcon(ICON_INVEST_VALUE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_INVEST_VALUE);
        }

        private async Task RefreshBookIconsAsync()
        {
            await MainUIConfigs.EnsureLoaded();
            bool open = KaifuModel.Instance.GetBookIconOpen();
            RefreshBookIcon(ICON_BOOK, open);
            RefreshBookIcon(ICON_BOOK_1, open);
        }

        // 对标老端 AddBookIcon:!finishAll() 时按图标配置的 open_day/max_open_day/open_task_id 门判(不看 open_lv),AddOwnerIcon 强加。
        private static void RefreshBookIcon(string iconType, bool bookOpen)
        {
            if (!bookOpen) { ActivityIconManager.Instance.DeleteIcon(iconType); return; }

            MainUIConfigs.FunctionIconCfg cfg = MainUIConfigs.GetFunctionIconCfg(iconType);
            if (cfg == null) { ActivityIconManager.Instance.DeleteIcon(iconType); return; }

            int openDay = ServerTimeModel.GetOpenServerDay();
            if (openDay <= 0) openDay = 1;
            bool pass = openDay >= cfg.OpenDay
                && (cfg.MaxOpenDay == 0 || openDay <= cfg.MaxOpenDay)
                && TaskModel.Instance.NewestFinishTaskId >= cfg.OpenTaskId;

            if (pass) ActivityIconManager.Instance.AddOwnerIcon(iconType);
            else ActivityIconManager.Instance.DeleteIcon(iconType);
        }

        // 本端加强:主角等级变化复请求(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        /// <summary>跨天(对标老端 KaifuActivityController.ts:56 DAY_CHANGE):**仅**复发 42004(投资开启列表),
        /// 不带 42401——老端该 DAY_CHANGE 绑定原文只有一行 SendFmtToGame(42004)。⚠勿与同文件 :84-86
        /// 被注释掉的 AddBookIcon DAY_CHANGE 死链订阅混淆(那段死链不接)。</summary>
        private void OnServerDayChange()
        {
            SendFmt(Proto.KAIFU_INVEST_OPEN); // 42004
            GameLog.Info("Kaifu", "DAY_CHANGE 跨天复发42004");
        }
    }
}
