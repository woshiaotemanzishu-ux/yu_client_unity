using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Partner
{
    /// <summary>
    /// 剑魄同修协议控制器(对标老端 commonController/PartnerController.ts;服务端 pt_142/pp_partner)。
    /// 进游戏发 14202 求全量列表;14205 培养(发 "i" companion_id)、14204 激活(发 "i");
    /// 14201 单个推送/请求回包 upsert。14203(跟随)/14206(妖核)/14207(传记)未移植(老端有,后续按需)。
    /// 培养消耗与阶星条件在服务端结算(config_companion_stage);背包/货币扣减经 15017/15018 增量刷新。
    /// </summary>
    public sealed class PartnerController : BaseController
    {
        public static readonly PartnerController Instance = new PartnerController();

        private PartnerController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.PARTNER_INFO, On14201);
            RegisterProtocal(Proto.PARTNER_LIST, On14202);
            RegisterProtocal(Proto.PARTNER_ACTIVE, On14204);
            RegisterProtocal(Proto.PARTNER_TRAIN, On14205);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            PartnerModel.Instance.Clear();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            await PartnerConfigs.EnsureLoaded();
            SendFmt(Proto.PARTNER_LIST);
            GameLog.Info("Partner", "request 14202 companion list(对标 PartnerController REQUSET_SCMD 14202)");
        }

        /// <summary>培养一次(对标老端培养按钮 → SendFmtToGame(14205,"i"));结果经 <see cref="On14205"/>。</summary>
        public void Train(int companionId)
        {
            if (companionId <= 0) return;
            SendFmt(Proto.PARTNER_TRAIN, "i", companionId);
            GameLog.Info("Partner", "train 14205 companion={0}", companionId);
        }

        /// <summary>激活同修(对标 14204)。</summary>
        public void Activate(int companionId)
        {
            if (companionId <= 0) return;
            SendFmt(Proto.PARTNER_ACTIVE, "i", companionId);
            GameLog.Info("Partner", "activate 14204 companion={0}", companionId);
        }

        /// <summary>14202 同修全量:fight_id:i + sum_attr[u16×{attr_id:c,attr_val:i}] + companion_list[u16×单项]。</summary>
        private void On14202(NetReader r)
        {
            int fightId = (int)r.ReadU32();
            r.ReadArray(ReadAttr);   // sum_attr(总属性,本轮读过留待属性面板)
            List<PartnerModel.CompanionVo> list = r.ReadArray(ReadCompanion);
            PartnerModel.Instance.SetList(fightId, list);
            GameLog.Info("Partner", "14202 companions={0} fightId={1} remaining={2}B", list.Count, fightId, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_PARTNER_UPDATE);
        }

        /// <summary>14201 单个更新:sum_attr[] + companion_id:i,stage:h,star:h,is_active:c,blessing:i,train_num:i,attr[],combat:l,fight_id:i。</summary>
        private void On14201(NetReader r)
        {
            r.ReadArray(ReadAttr);   // sum_attr
            var vo = new PartnerModel.CompanionVo
            {
                CompanionId = (int)r.ReadU32(),
                Stage = r.ReadU16(),
                Star = r.ReadU16(),
                IsActive = r.ReadU8() != 0,
                Blessing = r.ReadU32(),
                TrainNum = r.ReadU32(),
            };
            vo.Attrs = r.ReadArray(ReadAttr);
            vo.Combat = r.ReadU64();
            int fightId = (int)r.ReadU32();
            PartnerModel.Instance.Upsert(vo);
            PartnerModel.Instance.FightId = fightId;
            GameLog.Info("Partner", "14201 companion={0} {1}阶{2}星 blessing={3} remaining={4}B",
                vo.CompanionId, vo.Stage, vo.Star, vo.Blessing, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_PARTNER_UPDATE);
        }

        /// <summary>14204 激活结果(对标 On14204:errcode==1 置激活;OutwardChangedView 展示未移植)。</summary>
        private void On14204(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int companionId = (int)r.ReadU32();
            long combat = r.ReadU64();
            if (errcode != 1)
            {
                TipsManager.Toast("激活失败(" + errcode + ")");   // 错误码表(Util.ErrorCodeShow)未移植,显码降级
                return;
            }
            PartnerModel.CompanionVo vo = PartnerModel.Instance.Get(companionId);
            if (vo != null) { vo.IsActive = true; vo.Combat = combat; }
            GameLog.Info("Partner", "14204 activated companion={0} combat={1}(OutwardChangedView 展示未移植)", companionId, combat);
            EventDispatcher.Emit(GlobalEvent.EVT_PARTNER_UPDATE);
        }

        /// <summary>14205 培养结果(对标 On14205):errcode!=1 显码;成功套 stage/star/blessing,升阶/星再拉 14201。</summary>
        private void On14205(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int companionId = (int)r.ReadU32();
            int stage = r.ReadU16();
            int star = r.ReadU16();
            long blessing = r.ReadU32();
            if (errcode != 1)
            {
                TipsManager.Toast("培养失败(" + errcode + ")");   // 错误码表未移植,显码降级(常见:培养材料不足)
                GameLog.Info("Partner", "14205 train fail errcode={0} companion={1}", errcode, companionId);
                return;
            }
            bool upgraded = PartnerModel.Instance.ApplyTrain(companionId, stage, star, blessing);
            GameLog.Info("Partner", "14205 train ok companion={0} → {1}阶{2}星 blessing={3} upgraded={4}",
                companionId, stage, star, blessing, upgraded);
            if (upgraded) SendFmt(Proto.PARTNER_INFO, "i", companionId);   // 对标老端升级后 REQUSET_SCMD 14201 刷全字段
            EventDispatcher.Emit(GlobalEvent.EVT_PARTNER_UPDATE);
        }

        private static (int attrId, long val) ReadAttr(NetReader r)
        {
            return (r.ReadU8(), r.ReadU32());   // {attr_id:c, attr_val:i}
        }

        /// <summary>读 14202 companion_list 单项(字段序照 ClientProtocol.json,逐字段按序读完)。</summary>
        private static PartnerModel.CompanionVo ReadCompanion(NetReader r)
        {
            var vo = new PartnerModel.CompanionVo
            {
                CompanionId = (int)r.ReadU32(),  // companion_id:i
                Stage = r.ReadU16(),             // stage:h
                Star = r.ReadU16(),              // star:h
            };
            List<int> biogs = r.ReadArray(rr => (int)rr.ReadU8());   // biog_list[u16×{lv:c}]
            vo.BiogLvs = biogs;
            vo.IsActive = r.ReadU8() != 0;       // is_active:c
            vo.IsFight = r.ReadU8() != 0;        // is_fight:c
            vo.FigureId = (int)r.ReadU32();      // figure_id:i
            vo.Blessing = r.ReadU32();           // blessing:i
            vo.TrainNum = r.ReadU32();           // train_num:i
            vo.Attrs = r.ReadArray(ReadAttr);    // attr[u16×{attr_id:c,attr_val:i}]
            vo.Combat = r.ReadU64();             // combat:l
            return vo;
        }
    }
}
