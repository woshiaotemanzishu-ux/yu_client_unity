using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 自动穿戴(对标老端「一键穿戴」核心逻辑 EquipModel.ts:697-773 GetStrongestEquips/GetStrongestEquipByType):
    /// 每个部位在背包中取「合法且 rating 最大」的候选;当前部位空 → 直接穿;已穿 → 候选 rating 严格大于才换。
    /// 合法性过滤照老端四重:职业(career_id!=0 需相等)/部位/等级(role.level>=cfg.level)/性别(sex!=0 需相等;
    /// 转生 turn 校验 Unity RoleModel 暂无字段 → 交服务端拒绝显码,注明)。15201 只发 goods_id,部位由服务端判定。
    ///
    /// 老端无「获得装备自动穿」——一键穿戴只允许玩家点击；背包更新和登录都不得代行按钮。
    /// 已穿戴数据来自 15010 pos=equip(1) 通道(BagController 转存至此)。
    /// </summary>
    public static class EquipAutoWear
    {
        /// <summary>GOODS_POS_TYPE.equip(老端 GoodsModel.ts:306)。</summary>
        public const int POS_EQUIP = 1;

        // 防重:近期已发 15201 的 goods_id(回包/超时前不重发)。
        private static readonly Dictionary<long, double> _pendingUntil = new Dictionary<long, double>();

        /// <summary>取指定部位当前穿戴装备实例(15010 pos=equip 通道转存;未穿/通道未到 → null)。
        /// 补口子(自动循环 轮4 队列#4):精炼/洗魄/神炼等新面板暂无「选中态」数据源,借用「当前穿戴」当占位选中,
        /// 同 EquipStrenView 既有的 equip_type 固定武器槽先例。</summary>
        public static BagGoods GetWorn(int equipType) => BagModel.Instance.GetEquipmentAt(equipType);

        /// <summary>背包中央“一键装备”的玩家点击入口；与老端按钮一致，不依赖自动任务开关。</summary>
        public static int TryManualWear()
        {
            int sent = TryWearBest();
            if (sent == 0 && BagModel.Instance.HasEquipmentData)
                TipsManager.Toast("当前没有可替换的更强装备");
            return sent;
        }

        private static int TryWearBest()
        {
            if (ItemUseFlow.HasPendingEquipment)
            {
                GameLog.Info("Equip", "one-key wear skip: ItemUseView has pending equipment");
                TipsManager.Toast("请先处理待确认装备");
                return 0;
            }
            if (!BagModel.Instance.HasEquipmentData)
            {
                GameLog.Info("Equip", "one-key wear skip: 装备通道(15010 pos=1)未到,先请求");
                EquipWearController.Instance.RequestWornList();
                TipsManager.Toast("装备数据加载中，请稍后再试");
                return 0;
            }

            int career = RoleModel.Instance.Career;
            int sex = RoleModel.Instance.Sex;
            int level = RoleModel.Instance.Level;
            double now = UnityEngine.Time.realtimeSinceStartupAsDouble;

            // 逐部位取背包中合法且 rating 最大者(对标 GetStrongestEquipByType 的过滤与比较)。
            var best = new Dictionary<int, BagGoods>();
            foreach (BagGoods g in BagModel.Instance.BagGoodsList)
            {
                GoodsModel.GoodsBasic cfg = GoodsModel.GetGoodsBasicByTypeId(g.TypeId);
                if (cfg == null || !GoodsModel.IsEquip(g.TypeId) || cfg.EquipType <= 0) continue;
                if (cfg.CareerId != 0 && cfg.CareerId != career) continue;   // 职业
                if (level < cfg.Level) continue;                              // 等级
                if (cfg.Sex != 0 && cfg.Sex != sex) continue;                // 性别
                // 转生:Unity RoleModel 暂无 turn 字段，继续交服务端权威校验，失败显码。
                if (_pendingUntil.TryGetValue(g.GoodsId, out double until) && now < until) continue;
                if (!best.TryGetValue(cfg.EquipType, out BagGoods cur) || g.Rating > cur.Rating) best[cfg.EquipType] = g;
            }

            int sent = 0;
            foreach (KeyValuePair<int, BagGoods> kv in best)
            {
                BagGoods worn = BagModel.Instance.GetEquipmentAt(kv.Key);
                bool wear = worn == null                                      // 空槽直接穿(老端 !info 分支)
                            || kv.Value.Rating > worn.Rating;                  // 严格大于才换(老端 strongest.rating > info.rating)
                if (!wear) continue;
                _pendingUntil[kv.Value.GoodsId] = now + 10d;
                GameLog.Info("Equip", "auto-wear: pos={0} goods={1} type={2} rating={3}(worn={4})",
                    kv.Key, kv.Value.GoodsId, kv.Value.TypeId, kv.Value.Rating,
                    worn != null ? worn.Rating.ToString() : "空");
                EquipWearController.Instance.Wear(kv.Value.GoodsId);
                sent++;
            }
            if (sent > 0) GameLog.Info("Equip", "auto-wear sent {0} 件(对标一键穿戴批量 15201)", sent);
            return sent;
        }

        public static void Clear()
        {
            _pendingUntil.Clear();
        }
    }
}
