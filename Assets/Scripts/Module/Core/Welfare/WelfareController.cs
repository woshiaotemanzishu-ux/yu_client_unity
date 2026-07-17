using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Welfare
{
    /// <summary>
    /// 福利余量(Welfare)控制器(对标老客户端 commonController/WelfareController.ts,自动循环 轮18 PK4)。
    /// 承接签到(41703-05)/静默下载(41707-08)/在线福利(41715-16)/心悦礼包(41719)四段。GAME_START 时
    /// 老端一次性发 41700(RushGift 另管)/41703/41707/41715(ts:440-454);等级精确命中
    /// config_welfare_cfg["3"]=online_reward_open_lv 时补发 41715(ts:467-470);DAY_CHANGE 延时5ms重发 41703
    /// (ts:457-465)——本仓暂无跨天/日期切换事件源(全量核对 GlobalEvent 无果,同轮16 MarriageController/
    /// DungeonController/ChatController 先例),该重拉留 TODO,尾包补跨天事件源后在此接。
    /// 死号 41702/41706/41710-41714/41717/41718 不注册(见 Proto.cs §1 死号总清单);19301-19304 归
    /// <see cref="Shenxiao.Module.Core.AdReward.AdRewardController"/> 独立模块;41722 归既有
    /// <see cref="Shenxiao.Module.Core.GrowthBenefits.GrowthBenefitsController"/>(本轮追加);
    /// 41723/41724 归 <see cref="CombatWelfareController"/>(老端独立 GrowthForceModel)。
    /// </summary>
    public sealed class WelfareController : BaseController
    {
        public static readonly WelfareController Instance = new WelfareController();
        private WelfareController() { }

        // 等级去抖(EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时判门槛;同 GrowthBenefitsController 先例)。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.WELFARE_CHECKIN_INFO, On41703);
            RegisterProtocal(Proto.WELFARE_CHECKIN_CLAIM, On41704);
            RegisterProtocal(Proto.WELFARE_CHECKIN_RETROACTIVE, On41705);
            RegisterProtocal(Proto.WELFARE_DOWNLOAD_INFO, On41707);
            RegisterProtocal(Proto.WELFARE_DOWNLOAD_CLAIM, On41708);
            RegisterProtocal(Proto.WELFARE_ONLINE_INFO, On41715);
            RegisterProtocal(Proto.WELFARE_ONLINE_CLAIM, On41716);
            RegisterProtocal(Proto.WELFARE_XINYUE_GIFT, On41719);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            WelfareModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>对标老端 GlobalEventSystem.Bind(GAME_START,...)(WelfareController.ts:440-454):
        /// Reset() + 发 41703/41707/41715 裸包。心悦欢迎礼(41719 opr=3)老端门槛函数
        /// GetWelfareWelcomeOpenState 函数体已整段注释恒 false(CommonManager.ts:101-114),
        /// 已是死条件,本端镜像不自动发,仅留 <see cref="RequestXinyueGift"/> 供未来 UI 调用。</summary>
        private async void OnGameStart()
        {
            WelfareModel.Instance.Reset();
            await WelfareConfigs.EnsureLoaded();
            SendFmt(Proto.WELFARE_CHECKIN_INFO);
            SendFmt(Proto.WELFARE_DOWNLOAD_INFO);
            SendFmt(Proto.WELFARE_ONLINE_INFO);
            GameLog.Info("Welfare", "GAME_START 请求 41703/41707/41715(对标老端 WelfareController.ts:440-454)");
        }

        /// <summary>对标老端 RoleManager 主角 CHANGE_LEVEL 绑定(ts:467-470):等级精确命中
        /// config_welfare_cfg["3"]=online_reward_open_lv 时补发 41715 刷新在线福利面板。</summary>
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            int onlineOpenLv = WelfareConfigs.GetKvInt(3, 75); // 兜底线上值(config_welfare_cfg["3"].val=75)
            if (role.Level == onlineOpenLv) SendFmt(Proto.WELFARE_ONLINE_INFO);
        }

        // ---- 发送封装 ----

        public void RequestCheckinInfo() => SendFmt(Proto.WELFARE_CHECKIN_INFO);

        /// <summary>签到领取(对标老端 Handler41704 触发点,发 "cc" day,retroactive)。</summary>
        public void ClaimCheckin(int day, int retroactive) => SendFmt(Proto.WELFARE_CHECKIN_CLAIM, "cc", day, retroactive);

        /// <summary>补签(发 "c" day)。</summary>
        public void RetroactiveCheckin(int day) => SendFmt(Proto.WELFARE_CHECKIN_RETROACTIVE, "c", day);

        public void RequestDownloadInfo() => SendFmt(Proto.WELFARE_DOWNLOAD_INFO);
        public void ClaimDownload() => SendFmt(Proto.WELFARE_DOWNLOAD_CLAIM);
        public void RequestOnlineInfo() => SendFmt(Proto.WELFARE_ONLINE_INFO);

        /// <summary>领取在线福利(发 "i" id)。</summary>
        public void ClaimOnline(int id) => SendFmt(Proto.WELFARE_ONLINE_CLAIM, "i", id);

        /// <summary>心悦礼包请求(opr=3 查询新手欢迎态/4 领取新手礼包)。见 <see cref="OnGameStart"/> 注释:
        /// 老端 GAME_START 自动发 opr=3 的门槛条件现已死,故本端不自动触发,仅留公开方法。</summary>
        public void RequestXinyueGift(int opr) => SendFmt(Proto.WELFARE_XINYUE_GIFT, "c", opr);

        // ---- 协议处理 ----

        /// <summary>41703 签到基础信息(裸;9字段双平行数组)。pt_417.erl:16-17,105-141。</summary>
        private void On41703(NetReader r)
        {
            int totalDays = r.ReadU8();
            int totalType = r.ReadU16();
            List<WelfareModel.TotalStateEntry> totalState = r.ReadArray(rr =>
                new WelfareModel.TotalStateEntry(rr.ReadU32(), rr.ReadU8()));
            List<WelfareModel.AccStateEntry> accState = r.ReadArray(rr =>
                new WelfareModel.AccStateEntry(rr.ReadU8(), rr.ReadU8()));
            int checkType = r.ReadU16();
            int retroTimes = r.ReadU8();
            int daysFresh = r.ReadU8();
            int remainTimes = r.ReadU8();
            int checkDay = r.ReadU8();
            WelfareModel.Instance.SetCheckinInfo(totalDays, totalType, totalState, accState,
                checkType, retroTimes, daysFresh, remainTimes, checkDay);
            EventDispatcher.Emit(GlobalEvent.EVT_WELFARE_UPDATE, Proto.WELFARE_CHECKIN_INFO);
            GameLog.Info("Welfare", "41703 签到信息 totalDays={0} checkDay={1} totalStateN={2} accStateN={3}",
                totalDays, checkDay, totalState.Count, accState.Count);
        }

        /// <summary>41704 签到领取(老端自定义 ReadFmt 裸读,非 GetSCMD;Rewads/ExtraRewads 三字段均 32 位,
        /// 位宽已按 pt_417.erl:143-167 原文核实)。成功后对标老端 Fire(41703) 刷新。</summary>
        private void On41704(NetReader r)
        {
            int code = (int)r.ReadU32();
            List<(int style, int typeId, long count)> reward = ReadRewadsTriple(r);
            List<(int style, int typeId, long count)> extraReward = ReadRewadsTriple(r);
            if (code == 1)
            {
                string summary = FormatRewadsSummary(reward);
                string extraSummary = FormatRewadsSummary(extraReward);
                string text = extraSummary.Length == 0 ? summary
                    : summary.Length == 0 ? extraSummary : summary + "、" + extraSummary;
                if (text.Length > 0) TipsManager.Toast("获得 " + text);
                SendFmt(Proto.WELFARE_CHECKIN_INFO); // 对标老端成功后再发一次 41703 刷新
            }
            else
            {
                TipsManager.Toast("签到领取失败(" + code + ")");
            }
            EventDispatcher.Emit(GlobalEvent.EVT_WELFARE_RESULT, Proto.WELFARE_CHECKIN_CLAIM, code);
            GameLog.Info("Welfare", "41704 签到领取 code={0} rewardN={1} extraN={2}", code, reward.Count, extraReward.Count);
        }

        /// <summary>41705 补签(Rewads 结构同 41704 首个数组,三字段均 32 位)。</summary>
        private void On41705(NetReader r)
        {
            int code = (int)r.ReadU32();
            List<(int style, int typeId, long count)> reward = ReadRewadsTriple(r);
            if (code == 1)
            {
                string summary = FormatRewadsSummary(reward);
                if (summary.Length > 0) TipsManager.Toast("获得 " + summary);
                SendFmt(Proto.WELFARE_CHECKIN_INFO); // 对标老端成功后再发一次 41703 刷新
            }
            else
            {
                TipsManager.Toast("补签失败(" + code + ")");
            }
            EventDispatcher.Emit(GlobalEvent.EVT_WELFARE_RESULT, Proto.WELFARE_CHECKIN_RETROACTIVE, code);
            GameLog.Info("Welfare", "41705 补签 code={0} rewardN={1}", code, reward.Count);
        }

        /// <summary>41707 静默下载奖励信息(标准 write_object_list:Type:8,TypeId:32,Num:32)。</summary>
        private void On41707(NetReader r)
        {
            int code = (int)r.ReadU32();
            List<(int type, int typeId, int num)> rewads = ReadObjectList(r);
            WelfareModel.Instance.SetDownloadState(code);
            EventDispatcher.Emit(GlobalEvent.EVT_WELFARE_UPDATE, Proto.WELFARE_DOWNLOAD_INFO);
            GameLog.Info("Welfare", "41707 静默下载信息 code={0} rewadsN={1}", code, rewads.Count);
        }

        /// <summary>41708 领取静默下载奖励(裸 Code)。真实奖励明细来自 config_key_value[1].value(Erlang term,
        /// 对标老端 Handler41708),Unity 侧 config_key_value 未同步(P0 config 搬运清单缺项),暂只提示成功文案,
        /// TODO 待补该配置后还原 CongratulationObtainView 明细。</summary>
        private void On41708(NetReader r)
        {
            int code = (int)r.ReadU32();
            if (code == 1)
            {
                WelfareModel.Instance.SetDownloadState(2); // 对标老端 UpdateResourceGiftRewardState({code:2})
                TipsManager.Toast("领取成功");
            }
            else
            {
                TipsManager.Toast("领取失败(" + code + ")");
            }
            EventDispatcher.Emit(GlobalEvent.EVT_WELFARE_RESULT, Proto.WELFARE_DOWNLOAD_CLAIM, code);
            GameLog.Info("Welfare", "41708 领取静默下载奖励 code={0}", code);
        }

        /// <summary>41715 在线福利信息(裸)。</summary>
        private void On41715(NetReader r)
        {
            int time = r.ReadU16();
            long loginTime = r.ReadU32();
            List<WelfareModel.OnlineEntry> list = r.ReadArray(rr => new WelfareModel.OnlineEntry((int)rr.ReadU32(), rr.ReadU8()));
            WelfareModel.Instance.SetOnlineInfo(time, loginTime, list);
            EventDispatcher.Emit(GlobalEvent.EVT_WELFARE_UPDATE, Proto.WELFARE_ONLINE_INFO);
            GameLog.Info("Welfare", "41715 在线福利信息 time={0} loginTime={1} listN={2}", time, loginTime, list.Count);
        }

        /// <summary>41716 领取在线福利(二层嵌套 SendList{RewardId,Rewards(ObjectList),OtherRewards(ObjectList)})。
        /// OtherRewards 是月卡额外档(对标老端仅 KaifuActivityModel.CheckHaveMonthCard() 才展示),Unity 暂无月卡查询
        /// 通道接入本模块,先只读完保游标、toast 汇总走主档 Rewards,TODO 留接月卡态后按老端逻辑补展示 OtherRewards。</summary>
        private void On41716(NetReader r)
        {
            int code = (int)r.ReadU32();
            var sendList = r.ReadArray(rr =>
            {
                int rewardId = (int)rr.ReadU32();
                List<(int type, int typeId, int num)> rewards = ReadObjectList(rr);
                List<(int type, int typeId, int num)> otherRewards = ReadObjectList(rr);
                return (rewardId, rewards, otherRewards);
            });
            if (code == 1)
            {
                SendFmt(Proto.WELFARE_ONLINE_INFO); // 对标老端成功后再发一次 41715 刷新
                var all = new List<(int type, int typeId, int num)>();
                foreach (var entry in sendList) all.AddRange(entry.rewards);
                string summary = FormatRewardSummary(all);
                if (summary.Length > 0) TipsManager.Toast("获得 " + summary);
            }
            else
            {
                TipsManager.Toast("领取失败(" + code + ")");
            }
            EventDispatcher.Emit(GlobalEvent.EVT_WELFARE_RESULT, Proto.WELFARE_ONLINE_CLAIM, code);
            GameLog.Info("Welfare", "41716 领取在线福利 code={0} sendListN={1}", code, sendList.Count);
        }

        /// <summary>41719 心悦礼包(标准 write_object_list)。老端此二失败码不弹错误(ts:295):1005/1028。
        /// opr==4(领取新手礼包)成功后老端还会绑一次性 CONGRATULATION_VIEW_CLOSE 回调触发 TaskModel.DoTask()
        /// 开始新手任务链(ts:271-282),Unity 暂无该奖励弹窗关闭事件通道与新手任务自动触发点,数据层先不接,
        /// TODO 留待该 UI 落地后补。</summary>
        private void On41719(NetReader r)
        {
            int code = (int)r.ReadU32();
            int opr = r.ReadU8();
            int giftSt = r.ReadU8();
            List<(int type, int typeId, int num)> reward = ReadObjectList(r);
            if (code == 1)
            {
                WelfareModel.Instance.SetXinyueState(opr, giftSt);
                if (reward.Count > 0)
                {
                    string summary = FormatRewardSummary(reward);
                    TipsManager.Toast("获得 " + summary);
                }
            }
            else if (code != 1005 && code != 1028)
            {
                TipsManager.Toast("心悦礼包失败(" + code + ")");
            }
            EventDispatcher.Emit(GlobalEvent.EVT_WELFARE_RESULT, Proto.WELFARE_XINYUE_GIFT, code);
            GameLog.Info("Welfare", "41719 心悦礼包 code={0} opr={1} giftSt={2} rewardN={3}", code, opr, giftSt, reward.Count);
        }

        // ---- 小工具 ----

        /// <summary>Rewads/ExtraRewads 专用三元组(Style:32,TypeId:32,Count:32,pt_417.erl:143-167;
        /// 与标准 write_object_list 位宽不同,勿混用)。</summary>
        private static List<(int style, int typeId, long count)> ReadRewadsTriple(NetReader r) =>
            r.ReadArray(rr => ((int)rr.ReadU32(), (int)rr.ReadU32(), (long)rr.ReadU32()));

        /// <summary>标准 write_object_list 三元组(Type:8,TypeId:32,Num:32,与 RushGiftController/MailController
        /// 等既有 object_list 读法一致)。</summary>
        private static List<(int type, int typeId, int num)> ReadObjectList(NetReader r) =>
            r.ReadArray(rr => ((int)rr.ReadU8(), (int)rr.ReadU32(), (int)rr.ReadU32()));

        /// <summary>奖励摘要文案(降级 toast 用,对标老端 CongratulationObtainView 的物品名列表;同 MailController/
        /// DailyController 先例——本项目 CongratulationObtainView 尚无业务子类消费方)。</summary>
        private static string FormatRewardSummary(List<(int type, int typeId, int num)> rewards)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rewards.Count; i++)
            {
                if (i > 0) sb.Append('、');
                (int goodsId, int _) = GoodsModel.GetMappingTypeId(rewards[i].type, rewards[i].typeId);
                string name = GoodsModel.GetGoodsName(goodsId);
                if (string.IsNullOrEmpty(name)) name = "物品" + goodsId;
                sb.Append(name).Append('x').Append(rewards[i].num);
            }
            return sb.ToString();
        }

        private static string FormatRewadsSummary(List<(int style, int typeId, long count)> rewards)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rewards.Count; i++)
            {
                if (i > 0) sb.Append('、');
                (int goodsId, int _) = GoodsModel.GetMappingTypeId(rewards[i].style, rewards[i].typeId);
                string name = GoodsModel.GetGoodsName(goodsId);
                if (string.IsNullOrEmpty(name)) name = "物品" + goodsId;
                sb.Append(name).Append('x').Append(rewards[i].count);
            }
            return sb.ToString();
        }
    }
}
