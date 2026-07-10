using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 神屠九炼(神炼)协议控制器(自动循环 轮4 队列#4;对标老端 EquipController.ts on15255;服务端 pt_152)。
    /// UI 挂 EquipView tab4(EquipRefinementView)。与页签1"神兵淬炼"(EquipSmeltController,15250/51)是两套完全
    /// 独立系统,命名相似但业务/事件不通用,勿混(详见规格侦察 r4_oldequip §一)。
    ///
    /// 独立成小控制器而非塞进 EquipWashController:一模块一控制器,对标既有 EquipStoneController 先例
    /// (体量小也各自独立成类,便于后续扩展铸灵/护灵/觉醒等同段其它号时各自归位,不越滚越大)。
    ///
    /// 无专属查询协议:refinement_lv 展示读 15000/15001 GoodsDetailVo.RefinementLv(见 GoodsDynamicModel),
    /// 对标老端 GetWearEquipDynamics 直接读 dynamic,不额外发协议查询。
    /// </summary>
    public sealed class EquipRefinementController : BaseController
    {
        public static readonly EquipRefinementController Instance = new EquipRefinementController();

        private EquipRefinementController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.EQUIP_REFINEMENT_DO, On15255);
        }

        /// <summary>15255 执行(发 "l" goods_id 装备实例id,不是 equip_type;对标 EquipRefinementView.ts:143 _btn_refi)。</summary>
        public void Refine(long goodsId)
        {
            SendFmt(Proto.EQUIP_REFINEMENT_DO, "l", goodsId);
            GameLog.Info("Equip", "refine 15255 goods_id={0}", goodsId);
        }

        /// <summary>15255 回包:code:i, goods_id:l, refine_lv:i。code==1 → toast「神炼成功」+
        /// GoodsDynamicModel.Patch 就地改 RefinementLv(对标老端 on15255 直接 vo.refinement_lv=scmd.refine_lv,
        /// 不重新拉取);code!=1 显码降级(常见=阶数不足/无超属性词条/材料不足;服务端侦察报告标注 15255 有一处
        /// 已知 case_clause 崩溃风险——传不存在/非本人的 goods_id 会导致对端进程崩溃断线,客户端侧无法规避,仅记录
        /// 不复刻)。</summary>
        private void On15255(NetReader r)
        {
            int code = (int)r.ReadU32();
            long goodsId = r.ReadU64();
            int refineLv = (int)r.ReadU32();
            if (code != 1)
            {
                TipsManager.Toast("神炼失败(" + code + ")");
                GameLog.Info("Equip", "15255 fail code={0} goods_id={1}", code, goodsId);
                return;
            }
            TipsManager.Toast("神炼成功");
            GameLog.Info("Equip", "15255 ok goods_id={0} refine_lv={1} remaining={2}B", goodsId, refineLv, r.Remaining);
            GoodsDynamicModel.Instance.Patch(goodsId, vo => vo.RefinementLv = refineLv);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_REFINEMENT_UPDATE);
        }
    }
}
