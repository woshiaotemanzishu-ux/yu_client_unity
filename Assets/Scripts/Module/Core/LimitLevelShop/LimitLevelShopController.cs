using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.LimitLevelShop
{
    /// <summary>
    /// 限时等级抢购控制器(对标老客户端 LimitLevelShopController,模块 612)。进游戏请求 61200 拿
    /// 抢购礼包列表;回包据列表是否非空(GetEntranceOpenState)增删主界面图标 61201。
    /// 老端 On61200→RefreshState 对每个在开礼包 addIcon(其 act_condition.pic,变体 61201..61225)、
    /// 对已消失的 deleteIcon;本期图标化先只做主图标 61201(暂不解析 act_condition,见 RefreshIcon 上方 TODO)。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)复请求 61200——本系统按等级开抢购,升级可能开出新档,本端补的
    /// Unity 侧探测(老端无对应 CHANGE_LEVEL 绑定,纯本端加强,保留)。
    /// 跨天(轮20 实接,EVT_SERVER_DAY_CHANGE)同样复请求 61200(对标老端 LimitLevelShopController.ts:46
    /// DAY_CHANGE→ResetData;该闭包命名具误导性,实际只 Fire(SCMD_REQUEST,61200)发包,并不重置本地模型——
    /// 模型重置 _model.ReSetModel() 是 GAME_START 专属,DAY_CHANGE 不带,见 OnServerDayChange)。
    /// 61203 按老端在 61200 后逐礼包自动只读请求；61201 购买链仍未注册、未发送。
    /// </summary>
    public sealed class LimitLevelShopController : BaseController
    {
        public static readonly LimitLevelShopController Instance = new LimitLevelShopController();
        private LimitLevelShopController() { }

#if UNITY_EDITOR
        private static System.Func<byte[], bool> s_giftConfigOutboundIntercept = null;
#endif

        public const string ICON_TYPE = LimitLevelShopModel.ICON_TYPE;

        // 复请求 61200 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;
        // 当前在栏上显示的抢购变体图标集(每个在开礼包 act_condition.pic 解析所得,如 61206龙语/61207圣衣);
        // 礼包消失时据此删对应图标(对标老端 RefreshState 的 icon_dic_ + delete_list)。
        private readonly HashSet<string> _shownIcons = new HashSet<string>();

        protected override void Register()
        {
            RegisterProtocal(Proto.LIMITLEVELSHOP_LIST, On61200);
            RegisterProtocal(Proto.LIMITLEVELSHOP_GIFT_CONFIG, On61203);
            // 本端加强:等级变化时复请求(按等级开抢购),老端无对应绑定。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 对标老端 DAY_CHANGE→SCMD_REQUEST 61200(LimitLevelShopController.ts:46)。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            foreach (string icon in _shownIcons) ActivityIconManager.Instance.DeleteIcon(icon);
            _shownIcons.Clear();
            LimitLevelShopModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START→SCMD_REQUEST 61200)。</summary>
        public void RequestStartup()
        {
            // read(61200,_)->{ok,[]}:请求无字段,裸发。
            SendFmt(Proto.LIMITLEVELSHOP_LIST);
        }

        public void RequestGiftConfig(ushort type, ushort subtype, ushort grade)
        {
#if UNITY_EDITOR
            if (s_giftConfigOutboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(Proto.LIMITLEVELSHOP_GIFT_CONFIG, "hhh", new object[] { type, subtype, grade });
                if (s_giftConfigOutboundIntercept(frame)) return;
            }
#endif
            SendFmt(Proto.LIMITLEVELSHOP_GIFT_CONFIG, "hhh", type, subtype, grade);
        }

        private void On61203(NetReader r)
        {
            ushort type = r.ReadU16(); ushort subtype = r.ReadU16();
            List<LimitLevelShopModel.GiftConfigEntry> entries = r.ReadArray(rr => new LimitLevelShopModel.GiftConfigEntry(rr.ReadU16(), rr.ReadString(), rr.ReadString(), rr.ReadString(), rr.ReadString(), rr.ReadString(), rr.ReadString(), rr.ReadU16(), rr.ReadU16()));
            LimitLevelShopModel.Instance.ApplyGiftConfig(type, subtype, entries);
        }

        // 61200: gift_list[u16 count × {
        //   type:h, subtype:h, end_time:i,
        //   grade_state[u16 count × {grade:h, state:c}],
        //   old_grade_state[u16 count × {grade:h, state:c}],
        //   act_condition:string(u16 len + utf8), open_times:h }]
        // 服务端只发"在开"的礼包(pt_612 item_to_bin_0)。图标只需 type/subtype/end_time,其余读掉。
        private void On61200(NetReader r)
        {
            int count = r.ReadU16();
            var gifts = new List<LimitLevelShopModel.GiftEntry>(count);
            var newIcons = new HashSet<string>();
            var addList = new List<(string icon, long endTime)>();
            for (int i = 0; i < count; i++)
            {
                ushort type = r.ReadU16();
                ushort subtype = r.ReadU16();
                long endTime = r.ReadU32();

                int gradeStateCount = r.ReadU16();
                var gradeStates = new List<LimitLevelShopModel.GradeState>(gradeStateCount);
                for (int g = 0; g < gradeStateCount; g++)
                    gradeStates.Add(new LimitLevelShopModel.GradeState(r.ReadU16(), r.ReadU8()));

                int oldGradeStateCount = r.ReadU16();
                var oldGradeStates = new List<LimitLevelShopModel.GradeState>(oldGradeStateCount);
                for (int g = 0; g < oldGradeStateCount; g++)
                    oldGradeStates.Add(new LimitLevelShopModel.GradeState(r.ReadU16(), r.ReadU8()));

                string actCondition = r.ReadString(); // act_condition(erlang 串,内含 pic=变体图标类型)
                ushort openTimes = r.ReadU16();

                // 变体图标:每个在开礼包取自己的 pic(61201..61225,如 61206龙语/61207圣衣);解析不到回退泛用 61201。
                string icon = ResolveGiftIcon(actCondition) ?? ICON_TYPE;
                gifts.Add(new LimitLevelShopModel.GiftEntry(type, subtype, endTime, gradeStates,
                    oldGradeStates, actCondition, openTimes, icon));
                if (newIcons.Add(icon)) addList.Add((icon, endTime)); // 同一变体只加一次(取首个 end_time)
            }

            LimitLevelShopModel.Instance.SetGiftList(gifts);
            RefreshIcons(newIcons, addList);

            // 对标老端 RefreshState：61200 落地后只读查询每个在开礼包的 61203 展示配置。
            // type=66 使用当前档位；其余礼包以 grade=0 请求整组配置。61201 购买协议仍不注册、不发送。
            for (int i = 0; i < gifts.Count; i++)
            {
                LimitLevelShopModel.GiftEntry gift = gifts[i];
                ushort grade = gift.Type == 66 && gift.GradeStates.Count > 0 ? gift.GradeStates[0].Grade : (ushort)0;
                RequestGiftConfig(gift.Type, gift.Subtype, grade);
            }
        }

        // 集合式增删(对标老端 RefreshState:对每个在开礼包 addIcon(pic)、对已消失礼包 deleteIcon(旧 pic))。
        private void RefreshIcons(HashSet<string> newIcons, List<(string icon, long endTime)> addList)
        {
            // 删除本轮不再出现的变体图标。
            foreach (string old in new List<string>(_shownIcons))
            {
                if (!newIcons.Contains(old))
                {
                    ActivityIconManager.Instance.DeleteIcon(old);
                    _shownIcons.Remove(old);
                }
            }
            // 加/更新当前在开礼包的变体图标(带倒计时=各自 end_time)。
            for (int i = 0; i < addList.Count; i++)
            {
                _shownIcons.Add(addList[i].icon);
                _ = ActivityIconManager.Instance.AddIconAsync(addList[i].icon, addList[i].endTime);
            }
            GameLog.Info("LimitLevelShop", "61200 限时等级抢购: gifts={0} 变体图标={1}",
                LimitLevelShopModel.Instance.Gifts.Count, newIcons.Count);
        }

        // 解析礼包 act_condition(erlang 串)取 pic 变体图标号(对标老端 GetGiftCond→cond.pic);无 pic/解析失败返回 null。
        private static string ResolveGiftIcon(string actCondition)
        {
            if (string.IsNullOrEmpty(actCondition) || actCondition == "[]") return null;
            ErlangTerm cond;
            try { cond = ErlangParser.Parse(actCondition); }
            catch { return null; }

            IReadOnlyList<ErlangTerm> tuples = cond?.Items;
            if (tuples == null) return null;
            for (int i = 0; i < tuples.Count; i++)
            {
                IReadOnlyList<ErlangTerm> pair = tuples[i]?.Items;
                if (pair == null || pair.Count < 2) continue;
                if (pair[0].As<string>() != "pic") continue;
                try
                {
                    int pic = pair[1].As<int>();
                    return pic > 0 ? pic.ToString() : null;
                }
                catch { return null; }
            }
            return null;
        }

        // 本端加强:主角等级变化复请求 61200(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        /// <summary>跨天(对标老端 LimitLevelShopController.ts:46 DAY_CHANGE→ResetData):只复请求 61200,
        /// 不清本地模型状态(ResetData 这个闭包名字具误导性,内部只 Fire(SCMD_REQUEST,61200);
        /// 真正的模型重置 _model.ReSetModel() 只在 GAME_START 调,DAY_CHANGE 不带)。</summary>
        private void OnServerDayChange()
        {
            RequestStartup();
            GameLog.Info("LimitLevelShop", "DAY_CHANGE 跨天复请求61200");
        }
    }
}
