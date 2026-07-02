using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Dungeon
{
    /// <summary>
    /// 通用副本进出结算控制器(对标老端 commonController/BaseDungeonController.ts;服务端 pt_610)。
    /// 御魂本(config_dungeon.type=12,dun_id 12001~)最小闭环:61001 进入 → 61003 结算推送 → 61002 退出
    /// (61002 已在 <see cref="Shenxiao.Module.Core.AutoBrush.AutoBrushController"/> 注册,复用其常量
    /// Proto.DUNGEON_EXIT,本控制器不重复注册/不再发起独立处理,Exit() 仅 SendFmt 转发)。
    ///
    /// ⚠61003 vs 61013 抉择(侦察实证,见工单交付报告):老端结算入口是 <b>61003</b>「通用结算界面」
    /// (BaseDungeonController.ts:767 起注册,御魂本 dun_type=Rune 走第 911/976 行分支,统一走
    /// SetDungeonResultInfo + OPEN_DENGEON_RESULT_VIEW/TryAddResultDrop 收尾);61013 在老端源码整棵
    /// h5/src 树里 <b>零处注册处理器</b>,proto610.d.ts 里的类型声明写明 desc="结算界面加好友,邀请加入
    /// 公会,积分展示"——是结算面板上的社交附加协议(好友/公会邀请),配合 61011/61012 服务"助战类"副本,
    /// 并非御魂本会收到的结算本体。本控制器按侦察结论只接 61003;61013 常量保留(Proto.DUNGEON_SETTLE)
    /// 但不注册,如后续实测服务端确有下发再补。
    /// </summary>
    public sealed class DungeonController : BaseController
    {
        public static readonly DungeonController Instance = new DungeonController();

        private DungeonController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.DUNGEON_ENTER, On61001);
            RegisterProtocal(Proto.DUNGEON_SETTLE_UI, On61003);
            RegisterProtocal(Proto.DUNGEON_STATE, On61020);
            // 61002(DUNGEON_EXIT)已由 AutoBrushController 注册,红线不可重复注册;Exit() 只发不接。
        }

        public override void Dispose()
        {
            DungeonModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>进入副本(对标老端 675 行 61001 请求;发 "i" dun_id)。</summary>
        public void Enter(int dunId)
        {
            if (dunId <= 0) return;
            SendFmt(Proto.DUNGEON_ENTER, "i", dunId);
            GameLog.Info("Dungeon", "enter 61001 dun_id={0}", dunId);
        }

        /// <summary>退出副本(复用 AutoBrushController 已注册的 61002 回包处理,本控制器只发不接,无参)。</summary>
        public void Exit()
        {
            SendFmt(Proto.DUNGEON_EXIT);
            GameLog.Info("Dungeon", "exit 61002(回包由 AutoBrushController.On61002 处理)");
        }

        /// <summary>请求副本状态/次数(对标 pt_610 read(61020):请求体仅 dun_type:c 一个字段)。</summary>
        public void RequestState(int dunType)
        {
            SendFmt(Proto.DUNGEON_STATE, "c", dunType);
            GameLog.Info("Dungeon", "request 61020 dun_type={0}", dunType);
        }

        /// <summary>61001 进入回包:dun_id:i, scene_id:i, error_code:i, error_code_args:s。
        /// error_code==1 成功(对标老端 BaseDungeonController.ts:675~681,与 61002 的"1=成功"同一套约定)。</summary>
        private void On61001(NetReader r)
        {
            int dunId = (int)r.ReadU32();
            int sceneId = (int)r.ReadU32();
            int errorCode = (int)r.ReadU32();
            string errorCodeArgs = r.ReadString();

            if (errorCode != 1)
            {
                TipsManager.Toast("进入副本失败(" + errorCode + (string.IsNullOrEmpty(errorCodeArgs) ? "" : "," + errorCodeArgs) + ")");
                GameLog.Warn("Dungeon", "61001 enter fail dun_id={0} errorCode={1} args={2}", dunId, errorCode, errorCodeArgs);
                return;
            }

            DungeonModel.Instance.Apply61001(dunId);
            TipsManager.Toast("进入副本:" + DungeonConfigs.GetName(dunId));
            GameLog.Info("Dungeon", "61001 enter ok dun_id={0} scene_id={1}", dunId, sceneId);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_UPDATE);
        }

        /// <summary>61003 结算推送(对标老端"通用结算界面";字段序照 ClientProtocol.json "61003" 逐个读完):
        /// result:c, result_subtype:c, dun_id:i, grade:c, scene_id:i,
        /// reward_list[u16×{style:c,typeId:i,count:l,goods_id:l}],
        /// other_reward[u16×{reward_type:c, other_reward_list[u16×{style1:c,type_id1:i,count1:l,goods_id1:l}]}],
        /// ex_data[u16×{key:h,val:i}], count:c。result==1 成功(与 61001/61002 同一套"1=成功"约定)。</summary>
        private void On61003(NetReader r)
        {
            int result = r.ReadU8();
            int resultSubtype = r.ReadU8();
            int dunId = (int)r.ReadU32();
            int grade = r.ReadU8();
            int sceneId = (int)r.ReadU32();

            var rewards = new List<(int typeId, long num)>();
            List<(int style, int typeId, long count, long goodsId)> rewardList = r.ReadArray(ReadRewardItem);
            foreach ((int style, int typeId, long count, long goodsId) item in rewardList)
                rewards.Add((item.typeId, item.count));

            List<(int rewardType, List<(int style1, int typeId1, long count1, long goodsId1)> list)> otherReward =
                r.ReadArray(ReadOtherReward);
            foreach ((int rewardType, List<(int style1, int typeId1, long count1, long goodsId1)> list) group in otherReward)
            {
                if (group.list == null) continue;
                foreach ((int style1, int typeId1, long count1, long goodsId1) item in group.list)
                    rewards.Add((item.typeId1, item.count1));
            }

            r.ReadArray(ReadExData);   // ex_data(附加键值,本轮只按序读完,不用于展示)
            int count = r.ReadU8();

            DungeonModel.Instance.ApplySettle(result, rewards);

            // toast 前 3 件(经 GoodsModel.GetMappingTypeId 映射成真实 goods_id,style 沿用 reward_list 的 style 作为 GetMappingTypeId 的 type)
            var summary = new System.Text.StringBuilder();
            int shown = 0;
            for (int i = 0; i < rewardList.Count && shown < 3; i++)
            {
                (int style, int typeId, long count, long goodsId) item = rewardList[i];
                (int goodsId2, int _) = GoodsModel.GetMappingTypeId(item.style, item.typeId);
                if (shown > 0) summary.Append("、");
                summary.Append(GoodsModel.GetGoodsBasicByTypeId(goodsId2)?.Name ?? ("物品" + goodsId2));
                summary.Append("x").Append(item.count);
                shown++;
            }

            TipsManager.Toast(result == 1 ? "副本通关" : "副本失败(result=" + result + ")");
            if (shown > 0) TipsManager.Toast("获得:" + summary);

            GameLog.Info("Dungeon", "61003 settle dun_id={0} result={1} subtype={2} grade={3} scene={4} rewards={5} other={6} count={7}",
                dunId, result, resultSubtype, grade, sceneId, rewardList.Count, otherReward.Count, count);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_UPDATE);
        }

        /// <summary>61020 副本状态回包:dun_type:c + dun_list[u16×{dun_id:i, daily_count:h, weekly_count:h,
        /// permanent_count:h, reset_count:h, vip_count:h, add_count:h, is_sweep:c, rec_data[u16×{key:h,val:i}]}]。</summary>
        private void On61020(NetReader r)
        {
            int dunType = r.ReadU8();
            List<DungeonModel.DunState> list = r.ReadArray(ReadDunState);
            DungeonModel.Instance.Apply61020(dunType, list);
            GameLog.Info("Dungeon", "61020 state dun_type={0} count={1}", dunType, list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_DUNGEON_UPDATE);
        }

        private static (int style, int typeId, long count, long goodsId) ReadRewardItem(NetReader r)
        {
            return (r.ReadU8(), (int)r.ReadU32(), (long)r.ReadU64(), (long)r.ReadU64());
        }

        private static (int rewardType, List<(int style1, int typeId1, long count1, long goodsId1)> list) ReadOtherReward(NetReader r)
        {
            int rewardType = r.ReadU8();
            List<(int style1, int typeId1, long count1, long goodsId1)> list = r.ReadArray(ReadOtherRewardItem);
            return (rewardType, list);
        }

        private static (int style1, int typeId1, long count1, long goodsId1) ReadOtherRewardItem(NetReader r)
        {
            return (r.ReadU8(), (int)r.ReadU32(), (long)r.ReadU64(), (long)r.ReadU64());
        }

        private static (int key, int val) ReadExData(NetReader r)
        {
            return (r.ReadU16(), r.ReadI32());
        }

        private static DungeonModel.DunState ReadDunState(NetReader r)
        {
            var s = new DungeonModel.DunState
            {
                DunId = (int)r.ReadU32(),
                DailyCount = r.ReadU16(),
                WeeklyCount = r.ReadU16(),
                PermanentCount = r.ReadU16(),
                ResetCount = r.ReadU16(),
                VipCount = r.ReadU16(),
                AddCount = r.ReadU16(),
            };
            s.IsSweep = r.ReadU8() != 0;
            r.ReadArray(ReadExData);   // rec_data(附加键值,本轮只按序读完)
            return s;
        }
    }
}
