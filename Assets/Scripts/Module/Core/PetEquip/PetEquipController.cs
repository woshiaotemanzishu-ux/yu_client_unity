using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.PetEquip
{
    /// <summary>
    /// 侍魂装备协议控制器，对标老端 PetEquipController.ts 与服务端 pt_160/pp_mount 的 16014-16017。
    /// </summary>
    public sealed class PetEquipController : BaseController
    {
        public const int TYPE_HORSE = 1;
        public const int TYPE_PARTNER = 2;

        public static readonly PetEquipController Instance = new PetEquipController();

        private bool _sessionStarted;
        private bool _featureWasOpen;
        private int _sessionVersion;

#if UNITY_EDITOR
        // CliVerify 模块内出站截获缝：返回 true 时仅记录编码后的真实帧，不向活连接发送。
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private PetEquipController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.PET_EQUIP_INFO, On16014);
            RegisterProtocal(Proto.PET_EQUIP_WEAR, On16015);
            RegisterProtocal(Proto.PET_EQUIP_STRENGTHEN, On16016);
            RegisterProtocal(Proto.PET_EQUIP_POLISH, On16017);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ++_sessionVersion;
            _sessionStarted = false;
            _featureWasOpen = false;
            PetEquipModel.Instance.Clear();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            int version = ++_sessionVersion;
            _sessionStarted = false;
            PetEquipModel.Instance.Clear();
            await PetEquipConfigs.EnsureLoaded();
            await FuncOpenConfig.EnsureLoaded();
            if (!IsInitialized || version != _sessionVersion) return;

            _featureWasOpen = FuncOpenConfig.CheckFuncOpenState("PetEquipBaseView");
            _sessionStarted = true;
            if (_featureWasOpen)
            {
                RequestInfo(TYPE_HORSE);
                RequestInfo(TYPE_PARTNER);
            }
        }

        private void OnRoleInfoUpdate()
        {
            if (!_sessionStarted || !FuncOpenConfig.IsLoaded) return;
            bool featureOpen = FuncOpenConfig.CheckFuncOpenState("PetEquipBaseView");
            if (featureOpen && !_featureWasOpen)
            {
                RequestInfo(TYPE_HORSE);
                RequestInfo(TYPE_PARTNER);
            }
            _featureWasOpen = featureOpen;
        }

        /// <summary>请求坐骑/伙伴装备信息，wire: c(type_id)。</summary>
        public void RequestInfo(int typeId)
        {
            if (!IsSupportedType(typeId)) return;
            SendRequest(Proto.PET_EQUIP_INFO, "c", typeId);
        }

        /// <summary>穿戴或替换装备，wire: ccl(type_id,pos_id,goods_id)。</summary>
        public void RequestWear(int typeId, int posId, long goodsId)
        {
            if (!IsSupportedType(typeId) || posId <= 0 || goodsId <= 0) return;
            SendRequest(Proto.PET_EQUIP_WEAR, "ccl", typeId, posId, goodsId);
        }

        /// <summary>强化装备，wire: c+l+h+动态 l 数组。</summary>
        public void RequestStrengthen(int typeId, long goodsId, IReadOnlyList<long> costGoodsIds)
        {
            if (!IsSupportedType(typeId) || goodsId <= 0 || costGoodsIds == null) return;
            int count = costGoodsIds.Count;
            object[] args = new object[count + 3];
            args[0] = typeId;
            args[1] = goodsId;
            args[2] = count;
            for (int i = 0; i < count; i++) args[i + 3] = costGoodsIds[i];
            SendRequest(Proto.PET_EQUIP_STRENGTHEN, "clh" + new string('l', count), args);
        }

        /// <summary>打磨（装备升星/进阶），wire: cll(type_id,goods_id,cost_goods_id)。</summary>
        public void RequestPolish(int typeId, long goodsId, long costGoodsId)
        {
            if (!IsSupportedType(typeId) || goodsId <= 0 || costGoodsId <= 0) return;
            SendRequest(Proto.PET_EQUIP_POLISH, "cll", typeId, goodsId, costGoodsId);
        }

        private void SendRequest(int protoId, string format, params object[] args)
        {
#if UNITY_EDITOR
            if (s_outboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
                if (s_outboundIntercept(frame)) return;
            }
#endif
            NetManager.SendFmt(protoId, format, args);
        }

        private void On16014(NetReader r)
        {
            int typeId = r.ReadU8();
            int errcode = (int)r.ReadU32();
            long combatPower = r.ReadU32();
            int count = r.ReadU16();
            var items = new List<PetEquipModel.PetEquipItem>(count);
            for (int i = 0; i < count; i++)
            {
                items.Add(new PetEquipModel.PetEquipItem
                {
                    PosId = r.ReadU8(),
                    PosLevel = (int)r.ReadU32(),
                    Stage = (int)r.ReadU32(),
                    Star = r.ReadU16(),
                    PosPoint = r.ReadU32(),
                    GoodsId = r.ReadU64(),
                    GoodsTypeId = (int)r.ReadU32()
                });
            }

            if (errcode != 1)
            {
                ShowError(Proto.PET_EQUIP_INFO, errcode);
                return;
            }

            PetEquipModel.Instance.ApplyInfo(typeId, combatPower, items);
            EventDispatcher.Emit(GlobalEvent.EVT_PET_EQUIP_UPDATE, typeId);
        }

        private void On16015(NetReader r)
        {
            int typeId = r.ReadU8();
            int code = (int)r.ReadU32();
            r.ReadU8();
            r.ReadU64();
            r.ReadU64();
            r.ReadU32();
            r.ReadU32();
            if (code != 1)
            {
                ShowError(Proto.PET_EQUIP_WEAR, code);
                return;
            }
            RequestInfo(typeId);
        }

        private void On16016(NetReader r)
        {
            int typeId = r.ReadU8();
            int code = (int)r.ReadU32();
            long exp = r.ReadU32();
            int level = r.ReadU16();
            long goodsId = r.ReadU64();
            long combatPower = r.ReadU32();
            if (code != 1)
            {
                ShowError(Proto.PET_EQUIP_STRENGTHEN, code);
                return;
            }

            if (!PetEquipModel.Instance.TryApplyStrengthen(typeId, goodsId, exp, level, combatPower,
                    out bool levelChanged)) return;
            if (levelChanged) EventDispatcher.Emit(GlobalEvent.EVT_PET_EQUIP_STRENGTH_SUCCESS);
            EventDispatcher.Emit(GlobalEvent.EVT_PET_EQUIP_UPDATE, typeId);
        }

        private void On16017(NetReader r)
        {
            int typeId = r.ReadU8();
            int code = (int)r.ReadU32();
            int stage = r.ReadU16();
            int star = r.ReadU16();
            long goodsId = r.ReadU64();
            r.ReadU64();
            long combatPower = r.ReadU32();
            long exp = r.ReadU32();
            int level = r.ReadU16();
            if (code != 1)
            {
                ShowError(Proto.PET_EQUIP_POLISH, code);
                return;
            }

            if (!PetEquipModel.Instance.TryApplyPolish(typeId, goodsId, stage, star, exp, level, combatPower)) return;
            int wornPos = typeId == TYPE_HORSE ? BagModel.POS_HORSE : BagModel.POS_PARTNER;
            BagModel.Instance.UpdatePetEquipState(wornPos, goodsId, stage, star, combatPower);
            EventDispatcher.Emit(GlobalEvent.EVT_PET_EQUIP_UPDATE, typeId);
            EventDispatcher.Emit(GlobalEvent.EVT_PET_EQUIP_STAR_SUCCESS);
        }

        private static bool IsSupportedType(int typeId) => typeId == TYPE_HORSE || typeId == TYPE_PARTNER;

        private static void ShowError(int protoId, int code)
        {
            GameLog.Warn("PetEquip", "proto={0} failed code={1}", protoId, code);
            TipsManager.Toast("侍魂装备操作失败(" + code + ")"); // TODO i18n: 接通通用错误码表后改为服务端文案。
        }
    }
}
