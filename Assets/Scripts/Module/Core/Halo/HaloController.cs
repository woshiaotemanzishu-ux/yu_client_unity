using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Halo
{
    /// <summary>
    /// 光环(Halo)协议控制器(自动循环 轮18 PK2;对标老端 commonController/HaloController.ts,服务端 pt_514,
    /// 3 号全活:51400/51401/51402)。进游戏(EVT_GAME_START)发 51400 求活动信息;DAY_CHANGE 老端同样发 51400
    /// (ts:57-68,按 ConfigFuncOpenCondition 的 open_lv/open_day 门槛判定),但 Unity 尚无服务器日切事件源,
    /// TODO 待 ServerTime 模块接入后补挂(同 DungeonController.cs:96-98 先例,不臆造替代触发源)。
    /// ⚠51401/51402 的 Errcode 均在包尾(与常见"开头 Errcode"习惯相反,pt_514.erl:46-70 已核,勿套通用模板)。
    /// ⚠51402(RequestHaloSetting)发送点老端散在 4 外系统入口,HaloController.ts 内部调用反是注释掉的死代码
    /// (ts:38-41)。本轮只接数据层收发,UI 闭环留尾包,4 处入口存档:
    ///   arena/ArenaEnterView.ts:195,199(HaloPrivilegeType.ArenaSweep=3 一键扫荡);
    ///   dungeonEquip/DungeonEquipEnterView.ts:222,226(DungeonSweep=5,type=DUN_TYPE.Equip 合并挑战);
    ///   dungeonDragon/DungeonDragonEnterView.ts:190,194(DungeonSweep=5,type=DUN_TYPE.Dragon 合并挑战);
    ///   godBeast/GodBeastComView.ts:215,219(GodBeastComposite=6 一键合成)。
    /// </summary>
    public sealed class HaloController : BaseController
    {
        public static readonly HaloController Instance = new HaloController();
        private HaloController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.HALO_INFO, On51400);
            RegisterProtocal(Proto.HALO_REWARD_RECEIVE, On51401);
            RegisterProtocal(Proto.HALO_SETTING_UPDATE, On51402);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            HaloModel.Instance.Reset();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            await HaloConfigs.EnsureLoaded();
            RequestInfo();
        }

        /// <summary>请求光环信息(对标老端 GAME_START/DAY_CHANGE→Fire(REQUEST_SCMD,51400));发空包。</summary>
        public void RequestInfo()
        {
            SendFmt(Proto.HALO_INFO);
            GameLog.Info("Halo", "request 51400 halo info");
        }

        /// <summary>领取特权奖励(对标 HaloItem.ts:55)。发 "i"(Id)。</summary>
        public void RequestRewardReceive(int id)
        {
            if (id <= 0) return;
            SendFmt(Proto.HALO_REWARD_RECEIVE, "i", id);
            GameLog.Info("Halo", "request 51401 reward receive id={0}", id);
        }

        /// <summary>光环/自动扫荡特权设置(4 外系统入口共用,见类注释)。发 "hhc"(haloId, type, state)。</summary>
        public void RequestHaloSetting(int haloId, int type, int state)
        {
            SendFmt(Proto.HALO_SETTING_UPDATE, "hhc", haloId, type, state);
            GameLog.Info("Halo", "request 51402 setting haloId={0} type={1} state={2}", haloId, type, state);
        }

        /// <summary>51400:EndTime:32, Rewards[u16×{Id:32,State:8}], SettingList[u16×{HaloId:16,Type:16,State:8}]。</summary>
        private void On51400(NetReader r)
        {
            uint endTime = r.ReadU32();
            List<(int Id, int State)> rewards = r.ReadArray(rr => ((int)rr.ReadU32(), (int)rr.ReadU8()));
            List<(int HaloId, int Type, int State)> settingList = r.ReadArray(rr => ((int)rr.ReadU16(), (int)rr.ReadU16(), (int)rr.ReadU8()));
            HaloModel.Instance.ApplyInfo(endTime, rewards, settingList);
            GameLog.Info("Halo", "51400 info endTime={0} rewards={1} settings={2}", endTime, rewards.Count, settingList.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_HALO_UPDATE, Proto.HALO_INFO);
        }

        /// <summary>51401:Id:32, State:8, **Errcode:32 在末尾**(pt_514.erl:46-56 已核,勿套开头模板)。
        /// m1修复:老端 On51401(HaloController.ts:90-93)不判 errcode,直接 model.ShowHaloReward(id,state)
        /// 无条件套值——本端此前加了 errcode==1 门是错误镜像,已删除;失败仍弹 Toast 是本端单向加强,
        /// 老端无显码(同轮16 22305 先例留痕)。</summary>
        private void On51401(NetReader r)
        {
            int id = (int)r.ReadU32();
            int state = r.ReadU8();
            int errcode = (int)r.ReadU32();
            HaloModel.Instance.ApplyReward(id, state);
            if (errcode != 1) TipsManager.Toast("领取失败(" + errcode + ")"); // 本端单向加强,老端无显码(同轮16 22305 先例留痕)
            GameLog.Info("Halo", "51401 reward receive id={0} state={1} errcode={2}", id, state, errcode);
            EventDispatcher.Emit(GlobalEvent.EVT_HALO_UPDATE, Proto.HALO_REWARD_RECEIVE);
        }

        /// <summary>51402:Id:16, Type:16, State:8, **Errcode:32 在末尾**(pt_514.erl:58-70 已核)。同号双向,
        /// 本端主动发出后落地的服务端回执也走这里(老端 On51402 无论谁触发都统一套 SetSettingData)。
        /// m1修复:老端 On51402(HaloController.ts:96-99)同样不判 errcode,直接 SetSettingData——本端
        /// 此前加了 errcode==1 门是错误镜像,已删除;失败额外弹 Toast 是本端单向加强(同上 51401)。</summary>
        private void On51402(NetReader r)
        {
            int haloId = r.ReadU16();
            int type = r.ReadU16();
            int state = r.ReadU8();
            int errcode = (int)r.ReadU32();
            HaloModel.Instance.SetSetting(haloId, type, state);
            if (errcode != 1) TipsManager.Toast("设置失败(" + errcode + ")"); // 本端单向加强,老端无显码(同轮16 22305 先例留痕)
            GameLog.Info("Halo", "51402 setting haloId={0} type={1} state={2} errcode={3}", haloId, type, state, errcode);
            EventDispatcher.Emit(GlobalEvent.EVT_HALO_UPDATE, Proto.HALO_SETTING_UPDATE);
        }
    }
}
