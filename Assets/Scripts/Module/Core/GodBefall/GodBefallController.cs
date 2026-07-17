using System;
using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 谪仙临凡(GodBefall,自动循环 轮18 便宜活批 PK1)控制器:pt_440.erl 全 16 号(44000-44018,
    /// 44007-44009 空号),纯数据层接入(数据落 <see cref="GodBefallModel"/>,UI 尾包留 GodBefallFlow/
    /// Views 既有壳消费,同 15a/15b Boss、轮16 Marriage 先例)。
    ///
    /// wire 权威 yu_server/src/pt/pt_440.erl + src/god/pp_god.erl(逐号核对,非抄侦察稿估读)。
    /// 触发链镜像老端 GodBefallController.ts:
    ///   ①GAME_START:等级达 OPEN_LV 才发 44000/44010,并对 GodType 3~6 循环发 44017(ts:50-60);
    ///   ②CHANGE_LEVEL(本仓无该专属事件,借 EVT_ROLE_INFO_UPDATE+_lastLevel 探测,同 Marriage/DailyController
    ///     先例):精确等于 OPEN_LV 时补发 44000(ts:117-121,老端用 ==,非 >=);
    ///   ③SceneManager.START(本仓借 EVT_SCENE_SNAPSHOT_READY 每次进场景/切场景后触发,同 AutoFightController
    ///     先例):补发 44010(ts:174-178);
    ///   ④44002 激活成功 / 44005 升星成功后自动补发单只推送 44001(ts:213,250);
    ///   ⑤44011 切变身成功后自动补发 44010(ts:289)。
    /// </summary>
    public sealed class GodBefallController : BaseController
    {
        public static readonly GodBefallController Instance = new GodBefallController();
        private GodBefallController() { }

        /// <summary>GodBefallDefine.OPEN_LV(老端硬编码常量,GodBefallModel.ts:31),与 config_god_kv
        /// key="open_lv" 的值(400)数值一致但老端并不读该配置项(见 GodBefallConfigs.GetKv 文档注释),
        /// 本端镜像老端做法照抄硬编码,不读表。</summary>
        private const int OPEN_LV = 400;

        private int _lastLevel = -1;

        private static void ShowError(int code) => TipsManager.Toast("错误(" + code + ")"); // 错误码表未移植,显码降级

        protected override void Register()
        {
            RegisterProtocal(Proto.GODBEFALL_LIST, On44000);
            RegisterProtocal(Proto.GODBEFALL_ITEM_PUSH, On44001);
            RegisterProtocal(Proto.GODBEFALL_ACTIVATE, On44002);
            RegisterProtocal(Proto.GODBEFALL_LEVEL_UP, On44003);
            RegisterProtocal(Proto.GODBEFALL_GRADE_UP, On44004);
            RegisterProtocal(Proto.GODBEFALL_STAR_UP, On44005);
            RegisterProtocal(Proto.GODBEFALL_SET_BATTLE, On44006);
            RegisterProtocal(Proto.GODBEFALL_SWITCH_CD, On44010);
            RegisterProtocal(Proto.GODBEFALL_SWITCH, On44011);
            RegisterProtocal(Proto.GODBEFALL_EQUIP_WEAR, On44012);
            RegisterProtocal(Proto.GODBEFALL_EQUIP_TAKEOFF, On44013);
            RegisterProtocal(Proto.GODBEFALL_QUICK_SYNTHESIS, On44014);
            RegisterProtocal(Proto.GODBEFALL_POWER_PREVIEW, On44015);
            RegisterProtocal(Proto.GODBEFALL_SMART_SYNTHESIS, On44016);
            RegisterProtocal(Proto.GODBEFALL_TYPE_PANEL, On44017);
            RegisterProtocal(Proto.GODBEFALL_TYPE_STRENGTHEN, On44018);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_SCENE_SNAPSHOT_READY, OnSceneSnapshotReady);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SCENE_SNAPSHOT_READY, OnSceneSnapshotReady);
            _lastLevel = -1;
            GodBefallModel.Instance.Reset();
            base.Dispose();
        }

        // ---------------------------------------------------------------------------------------
        // 触发链(对标老端 GodBefallController.ts InitEvent)
        // ---------------------------------------------------------------------------------------

        private async void OnGameStart()
        {
            await GodBefallConfigs.EnsureLoaded();
            GodBefallModel.Instance.Reset(); // 对标老端 model.Reset()(每次 GAME_START 清态,ts:51)
            int lv = RoleModel.Instance.Level;
            _lastLevel = lv;
            bool open = lv >= OPEN_LV;
            if (open)
            {
                RequestGodList();
                RequestSwitchCd();
                for (int godType = 3; godType <= 6; godType++) RequestTypePanel(godType);
            }
            GameLog.Info("GodBefall", "GAME_START lv={0} openLv={1} 达标补发44000/44010/44017x4={2}", lv, OPEN_LV, open);
        }

        /// <summary>对标老端 role_vo.Bind(CHANGE_LEVEL):精确等于 OPEN_LV 时补发 44000(ts:117-121)。
        /// 本仓无 CHANGE_LEVEL 专属事件,借 EVT_ROLE_INFO_UPDATE+_lastLevel 探测,同 Marriage 先例。</summary>
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo || role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            if (role.Level == OPEN_LV)
            {
                RequestGodList();
                GameLog.Info("GodBefall", "CHANGE_LEVEL 达开启等级临界 lv={0} 补发44000", role.Level);
            }
        }

        /// <summary>对标老端 SceneManager.START(ts:174-178),本仓借场景快照就绪事件(同 AutoFightController
        /// 先例)每次进/切场景重查变身CD。</summary>
        private void OnSceneSnapshotReady() => RequestSwitchCd();

        // ---------------------------------------------------------------------------------------
        // Requests(C2S)
        // ---------------------------------------------------------------------------------------

        public void RequestGodList() => SendFmt(Proto.GODBEFALL_LIST);
        public void RequestItem(long godId) => SendFmt(Proto.GODBEFALL_ITEM_PUSH, "i", godId);
        public void RequestActivate(long godId) => SendFmt(Proto.GODBEFALL_ACTIVATE, "i", godId);
        public void RequestLevelUp(long godId) => SendFmt(Proto.GODBEFALL_LEVEL_UP, "i", godId);
        public void RequestGradeUp(long godId) => SendFmt(Proto.GODBEFALL_GRADE_UP, "i", godId);
        public void RequestStarUp(long godId) => SendFmt(Proto.GODBEFALL_STAR_UP, "i", godId);
        public void RequestSetBattle(int pos, long godId) => SendFmt(Proto.GODBEFALL_SET_BATTLE, "ci", pos, godId);
        public void RequestSwitchCd() => SendFmt(Proto.GODBEFALL_SWITCH_CD);
        public void RequestSwitch() => SendFmt(Proto.GODBEFALL_SWITCH);

        /// <summary>穿戴神装。pp_god.erl:135 destructure [GoodsAutoId, GodId]——第二参是目标神格 GodId,
        /// 不是装备槽位(槽位由服务端按装备自身 subtype 字段自动判定,pp_god.erl:149)。</summary>
        public void RequestEquipWear(long goodsAutoId, long godId) => SendFmt(Proto.GODBEFALL_EQUIP_WEAR, "li", goodsAutoId, godId);

        public void RequestEquipTakeoff(long godId, int pos) => SendFmt(Proto.GODBEFALL_EQUIP_TAKEOFF, "ic", godId, pos);
        public void RequestQuickSynthesis(long ruleId, long goodsAutoId) => SendFmt(Proto.GODBEFALL_QUICK_SYNTHESIS, "il", ruleId, goodsAutoId);
        public void RequestPowerPreview(long godId) => SendFmt(Proto.GODBEFALL_POWER_PREVIEW, "i", godId);

        /// <summary>智能合成(44016,老端自定义 WriteFmt,GodBefallSynthesisView.ts:110)。C2S 变长:
        /// u16 计数 + {RuleId:32,Count:8}×N(pt_440.erl:48-56 read(44016,...) 逐字段核对)。</summary>
        public void RequestSmartSynthesis(IReadOnlyList<(long ruleId, int count)> list)
        {
            list ??= Array.Empty<(long, int)>();
            var fmt = new StringBuilder("h");
            var args = new List<object> { list.Count };
            foreach ((long ruleId, int count) in list)
            {
                fmt.Append("ic");
                args.Add(ruleId);
                args.Add(count);
            }
            SendFmt(Proto.GODBEFALL_SMART_SYNTHESIS, fmt.ToString(), args.ToArray());
        }

        public void RequestTypePanel(int godType) => SendFmt(Proto.GODBEFALL_TYPE_PANEL, "c", godType);

        /// <summary>神格强化提交(44018,老端自定义 WriteFmt)。C2S:GodType:8 + u16计数 +
        /// {GoodsTypeId:32,GoodsNum:16}×N + IsDivide:8(pt_440.erl:60-70 read(44018,...) 逐字段核对)。</summary>
        public void RequestTypeStrengthen(int godType, IReadOnlyList<(long goodsTypeId, int goodsNum)> list, bool isDivide)
        {
            list ??= Array.Empty<(long, int)>();
            var fmt = new StringBuilder("ch");
            var args = new List<object> { godType, list.Count };
            foreach ((long goodsTypeId, int goodsNum) in list)
            {
                fmt.Append("ih");
                args.Add(goodsTypeId);
                args.Add(goodsNum);
            }
            fmt.Append('c');
            args.Add(isDivide ? 1 : 0);
            SendFmt(Proto.GODBEFALL_TYPE_STRENGTHEN, fmt.ToString(), args.ToArray());
        }

        // ---------------------------------------------------------------------------------------
        // Handlers(S2C)
        // ---------------------------------------------------------------------------------------

        /// <summary>44000 神格总列表(裸,无 Code)。二层嵌套:GodList[u16×11字段+EquipList[u16×{Pos,GoodsId}]]。</summary>
        private void On44000(NetReader r)
        {
            List<GodBefallModel.GodEntry> list = r.ReadArray(ReadGodEntry);
            GodBefallModel.Instance.SetGodList(list);
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_UPDATE, 0L);
            GameLog.Info("GodBefall", "44000 神格列表(全量) count={0}", list.Count);
        }

        private static GodBefallModel.GodEntry ReadGodEntry(NetReader r)
        {
            var e = new GodBefallModel.GodEntry
            {
                IsBattle = r.ReadU8(), GodId = r.ReadU32(), Lv = r.ReadU16(), Exp = r.ReadU32(),
                Grade = r.ReadU16(), Star = r.ReadU32(), Power = r.ReadU64(),
                NextLvPower = r.ReadU64(), NextGradePower = r.ReadU64(), NextStarPower = r.ReadU64(),
            };
            e.EquipList.AddRange(r.ReadArray(ReadEquipSlot));
            return e;
        }

        private static GodBefallModel.EquipSlot ReadEquipSlot(NetReader r) =>
            new GodBefallModel.EquipSlot { Pos = r.ReadU8(), GoodsId = r.ReadU64() };

        /// <summary>44001 单只神格推送(裸,无 Code,字段同 44000 元素,44002/44005 成功后自动补发)。</summary>
        private void On44001(NetReader r)
        {
            GodBefallModel.GodEntry e = ReadGodEntry(r);
            GodBefallModel.Instance.UpsertGod(e);
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_UPDATE, e.GodId);
            GameLog.Info("GodBefall", "44001 单只神格推送 godId={0} lv={1} isBattle={2}", e.GodId, e.Lv, e.IsBattle);
        }

        /// <summary>44002 激活。Errcode:32,Power:64,GodId:32。成功后老端无条件置当前变身id(ts:209,quirk,
        /// 不判断是否真为首只上阵神格,存档不修)+自动补发44001(ts:213)。</summary>
        private void On44002(NetReader r)
        {
            int code = r.ReadI32();
            long power = r.ReadU64();
            long godId = r.ReadU32();
            if (code == 1)
            {
                GodBefallModel.Instance.SetCurrentBattleId(godId);
                RequestItem(godId);
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_ACTIVATE, code);
            GameLog.Info("GodBefall", "44002 激活 code={0} godId={1} power={2}", code, godId, power);
        }

        /// <summary>44003 升级。Errcode,GodId,Lv:16,Exp:32,Power:64,NextLvPower:64,NextGradePower:64,NextStarPower:64。</summary>
        private void On44003(NetReader r)
        {
            int code = r.ReadI32();
            long godId = r.ReadU32();
            int lv = r.ReadU16();
            long exp = r.ReadU32();
            long power = r.ReadU64();
            long nextLvPower = r.ReadU64();
            long nextGradePower = r.ReadU64();
            long nextStarPower = r.ReadU64();
            if (code == 1)
            {
                GodBefallModel.Instance.ApplyLevelUp(godId, lv, exp, power, nextLvPower, nextGradePower, nextStarPower);
                EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_UPDATE, godId);
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_LEVEL_UP, code);
            GameLog.Info("GodBefall", "44003 升级 code={0} godId={1} lv={2} exp={3}", code, godId, lv, exp);
        }

        /// <summary>44004 升阶。Errcode,GodId,Grade:16,Power:64,NextLvPower:64,NextGradePower:64,NextStarPower:64。</summary>
        private void On44004(NetReader r)
        {
            int code = r.ReadI32();
            long godId = r.ReadU32();
            int grade = r.ReadU16();
            long power = r.ReadU64();
            long nextLvPower = r.ReadU64();
            long nextGradePower = r.ReadU64();
            long nextStarPower = r.ReadU64();
            if (code == 1)
            {
                GodBefallModel.Instance.ApplyGradeUp(godId, grade, power, nextLvPower, nextGradePower, nextStarPower);
                EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_UPDATE, godId);
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_GRADE_UP, code);
            GameLog.Info("GodBefall", "44004 升阶 code={0} godId={1} grade={2}", code, godId, grade);
        }

        /// <summary>44005 升星。Errcode,GodId,Star:32(⚠32位非16位,同44000/44001),Power:64,Next*Power:64×3。
        /// 成功后自动补发44001(ts:250)。</summary>
        private void On44005(NetReader r)
        {
            int code = r.ReadI32();
            long godId = r.ReadU32();
            long star = r.ReadU32();
            long power = r.ReadU64();
            long nextLvPower = r.ReadU64();
            long nextGradePower = r.ReadU64();
            long nextStarPower = r.ReadU64();
            if (code == 1)
            {
                GodBefallModel.Instance.ApplyStarUp(godId, star, power, nextLvPower, nextGradePower, nextStarPower);
                RequestItem(godId);
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_STAR_UP, code);
            GameLog.Info("GodBefall", "44005 升星 code={0} godId={1} star={2}", code, godId, star);
        }

        /// <summary>44006 出战。Errcode,GodId(无 Pos 回声,槽位由客户端自行记忆)。m7:成功分支补
        /// Toast"出战成功",与 44012/44013/44014 显码口径一致(ts:259)。</summary>
        private void On44006(NetReader r)
        {
            int code = r.ReadI32();
            long godId = r.ReadU32();
            if (code == 1)
            {
                GodBefallModel.Instance.SetBattle(godId);
                TipsManager.Toast("出战成功");
                EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_UPDATE, godId);
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_SET_BATTLE, code);
            GameLog.Info("GodBefall", "44006 出战 code={0} godId={1}", code, godId);
        }

        /// <summary>44010 变身CD(裸,无 Code)。SwitchCd:32,EndTime:32。</summary>
        private void On44010(NetReader r)
        {
            long switchCd = r.ReadU32();
            long endTime = r.ReadU32();
            GodBefallModel.Instance.SetSwitchCd(switchCd, endTime);
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_UPDATE, 0L);
            GameLog.Info("GodBefall", "44010 变身CD switchCd={0} endTime={1}", switchCd, endTime);
        }

        /// <summary>44011 切变身。Errcode,GodId(失败时服务端已写0)。B1修复:老端 on44011 成功后仅补发
        /// 44010+SkillUIModel.ReleaseGodBefallSkillTip(godId)(ts:286-294),**不写** _cur_battle_id——
        /// 全仓真正的"44002·44011 quirk 直写"说法有误,quirk 写点只有 44002 一处(见 On44002 注释);
        /// 本端此前在这里误镜像了一次 SetCurrentBattleId,已删除该调用。ReleaseGodBefallSkillTip 是
        /// 技能UI提示释放,本轮数据层不接 UI,不移植该行为(留档)。</summary>
        private void On44011(NetReader r)
        {
            int code = r.ReadI32();
            long godId = r.ReadU32();
            if (code == 1)
            {
                RequestSwitchCd();
                EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_UPDATE, godId);
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_SWITCH, code);
            GameLog.Info("GodBefall", "44011 切变身 code={0} godId={1}", code, godId);
        }

        /// <summary>44012 穿戴神装。Code:32(仅此一字段)。⚠服务端成功分支(pp_god.erl:196-219)不回本号、
        /// 只回44001推送,本号实际只在失败路径到达——code==1 分支镜像老端保留但按服务端现状不可达。</summary>
        private void On44012(NetReader r)
        {
            int code = r.ReadI32();
            if (code == 1) TipsManager.Toast("穿戴成功"); // 镜像老端 ts:299,按服务端现状此分支不可达
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_EQUIP_WEAR, code);
            GameLog.Info("GodBefall", "44012 穿戴神装 code={0}(成功分支老端镜像/服务端现状不可达,见类注释)", code);
        }

        /// <summary>44013 卸下神装。Code:32。成功=ack(本号)+44001 推送双反馈(与44012不对称)。</summary>
        private void On44013(NetReader r)
        {
            int code = r.ReadI32();
            if (code == 1) TipsManager.Toast("卸下成功");
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_EQUIP_TAKEOFF, code);
            GameLog.Info("GodBefall", "44013 卸下神装 code={0}", code);
        }

        /// <summary>44014 快速合成。Code:32,RuleId:32,GoodsId:64(请求参数回声)。恒记录结果(成功/失败都覆盖)。</summary>
        private void On44014(NetReader r)
        {
            int code = r.ReadI32();
            long ruleId = r.ReadU32();
            long goodsId = r.ReadU64();
            GodBefallModel.Instance.SetQuickSynthesisResult(code, ruleId, goodsId);
            if (code == 1) TipsManager.Toast("合成成功");
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_QUICK_SYNTHESIS, code);
            GameLog.Info("GodBefall", "44014 快速合成 code={0} ruleId={1} goodsId={2}", code, ruleId, goodsId);
        }

        /// <summary>44015 战力预览。GodId:32,Power:64(无 Code,恒交付,无成败概念)。</summary>
        private void On44015(NetReader r)
        {
            long godId = r.ReadU32();
            long power = r.ReadU64();
            GodBefallModel.Instance.SetPowerPreview(godId, power);
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_POWER_PREVIEW, 1); // 无Code,恒视为已交付
            GameLog.Info("GodBefall", "44015 战力预览(无Code) godId={0} power={1}", godId, power);
        }

        /// <summary>44016 智能合成。Code:32,GoodsList[u16×{GoodsType:8,GoodsTypeId:64,GoodsNum:8}]。
        /// 数组恒需读完保游标,仅成功时落地(失败沿用上次数据,对标老端失败分支不消费 goods_list)。</summary>
        private void On44016(NetReader r)
        {
            int code = r.ReadI32();
            List<GodBefallModel.SmartSynthesisReward> rewards = r.ReadArray(ReadSmartReward);
            if (code == 1) GodBefallModel.Instance.SetSmartSynthesisRewards(rewards);
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_SMART_SYNTHESIS, code);
            GameLog.Info("GodBefall", "44016 智能合成 code={0} rewardN={1}", code, rewards.Count);
        }

        private static GodBefallModel.SmartSynthesisReward ReadSmartReward(NetReader r) => new GodBefallModel.SmartSynthesisReward
        {
            GoodsType = r.ReadU8(), GoodsTypeId = r.ReadU64(), GoodsNum = r.ReadU8(),
        };

        /// <summary>44017 神格强化界面(GAME_START 对 GodType 3~6 循环发)。GodType:8,CurrentLv:16,CurrentExp:32
        /// (无 Code,恒交付)。</summary>
        private void On44017(NetReader r)
        {
            int godType = r.ReadU8();
            int currentLv = r.ReadU16();
            long currentExp = r.ReadU32();
            GodBefallModel.Instance.SetStrongGod(godType, currentLv, currentExp);
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_TYPE_PANEL, 1); // 无Code,恒视为已交付
            GameLog.Info("GodBefall", "44017 神格强化界面(无Code) godType={0} lv={1} exp={2}", godType, currentLv, currentExp);
        }

        /// <summary>44018 神格强化提交。Code:32,Args:string,GodType:8,CurrentLv:16,CurrentExp:32,IsDivide:8。
        /// 结果恒记录(成功/失败都覆盖,含 Args 错误详情);仅成功时联动更新 StrongGodDic。</summary>
        private void On44018(NetReader r)
        {
            int code = r.ReadI32();
            string args = r.ReadString();
            int godType = r.ReadU8();
            int currentLv = r.ReadU16();
            long currentExp = r.ReadU32();
            int isDivide = r.ReadU8();
            if (code == 1) GodBefallModel.Instance.SetStrongGod(godType, currentLv, currentExp);
            else ShowError(code);
            GodBefallModel.Instance.SetTypeStrengthenResult(code, args, godType, currentLv, currentExp, isDivide);
            EventDispatcher.Emit(GlobalEvent.EVT_GODBEFALL_RESULT, Proto.GODBEFALL_TYPE_STRENGTHEN, code);
            GameLog.Info("GodBefall", "44018 神格强化提交 code={0} godType={1} lv={2} exp={3} isDivide={4}",
                code, godType, currentLv, currentExp, isDivide);
        }
    }
}
