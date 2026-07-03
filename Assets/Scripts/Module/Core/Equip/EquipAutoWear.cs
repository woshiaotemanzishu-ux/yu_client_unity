using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Tasks;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 自动穿戴(对标老端「一键穿戴」核心逻辑 EquipModel.ts:697-773 GetStrongestEquips/GetStrongestEquipByType):
    /// 每个部位在背包中取「合法且 rating 最大」的候选;当前部位空 → 直接穿;已穿 → 候选 rating 严格大于才换。
    /// 合法性过滤照老端四重:职业(career_id!=0 需相等)/部位/等级(role.level>=cfg.level)/性别(sex!=0 需相等;
    /// 转生 turn 校验 Unity RoleModel 暂无字段 → 交服务端拒绝显码,注明)。15201 只发 goods_id,部位由服务端判定。
    ///
    /// 老端无「获得装备自动穿」——一键穿戴是玩家点击;此处在【自动任务模式】下代行点击(活服实证:
    /// 主线奖励武器躺背包 → 战力 1760 打不过副本推荐 15000),行为=老端一键穿戴逐件发 15201,不发明比较维度。
    /// 已穿戴数据来自 15010 pos=equip(1) 通道(BagController 转存至此)。
    /// </summary>
    public static class EquipAutoWear
    {
        /// <summary>GOODS_POS_TYPE.equip(老端 GoodsModel.ts:306)。</summary>
        public const int POS_EQUIP = 1;

        // 已穿戴:equip_type(部位 1..10)→ 装备实例(15010 pos=1 通道全量转存)。
        private static readonly Dictionary<int, BagGoods> _worn = new Dictionary<int, BagGoods>();
        private static bool _wornLoaded;

        // 防重:近期已发 15201 的 goods_id(回包/超时前不重发)。
        private static readonly Dictionary<long, double> _pendingUntil = new Dictionary<long, double>();
        private static bool _debouncing;

        /// <summary>15010 pos=equip 全量转存(BagController 例外分支调)。</summary>
        public static void SetWornList(List<BagGoods> list)
        {
            _worn.Clear();
            if (list != null)
            {
                foreach (BagGoods g in list)
                {
                    GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(g.TypeId);
                    if (basic != null && basic.EquipType > 0) _worn[basic.EquipType] = g;
                }
            }
            _wornLoaded = true;
            GameLog.Info("Equip", "auto-wear worn list loaded: {0} 件", _worn.Count);
        }

        /// <summary>EVT_BAG_UPDATE 防抖入口(EquipWearController 挂)。自动任务开着才代行一键穿戴。</summary>
        public static void OnBagUpdate()
        {
            if (_debouncing) return;
            _debouncing = true;
            _ = DebounceAsync();
        }

        private static async Task DebounceAsync()
        {
            await Task.Delay(1000);
            _debouncing = false;
            TryAutoWear();
        }

        /// <summary>对标 GetStrongestEquips:逐部位选最强候选并发 15201(空槽直接穿/严格更优才换)。</summary>
        public static void TryAutoWear()
        {
            if (!TaskModel.Instance.GetAutoTaskSetting()) return;
            if (!_wornLoaded)
            {
                GameLog.Info("Equip", "auto-wear skip: 装备通道(15010 pos=1)未到,先请求");
                EquipWearController.Instance.RequestWornList();
                return;
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
                // 性别:config_goods sex 列 Unity GoodsBasic 未读(turn 同)→ 交服务端校验,失败显码(保守可穿)。
                if (_pendingUntil.TryGetValue(g.GoodsId, out double until) && now < until) continue;
                if (!best.TryGetValue(cfg.EquipType, out BagGoods cur) || g.Rating > cur.Rating) best[cfg.EquipType] = g;
            }

            int sent = 0;
            foreach (KeyValuePair<int, BagGoods> kv in best)
            {
                bool wear = !_worn.TryGetValue(kv.Key, out BagGoods worn)      // 空槽直接穿(老端 !info 分支)
                            || kv.Value.Rating > worn.Rating;                  // 严格大于才换(老端 strongest.rating > info.rating)
                if (!wear) continue;
                _pendingUntil[kv.Value.GoodsId] = now + 10d;
                GameLog.Info("Equip", "auto-wear: pos={0} goods={1} type={2} rating={3}(worn={4})",
                    kv.Key, kv.Value.GoodsId, kv.Value.TypeId, kv.Value.Rating,
                    _worn.TryGetValue(kv.Key, out BagGoods w2) ? w2.Rating.ToString() : "空");
                EquipWearController.Instance.Wear(kv.Value.GoodsId);
                sent++;
            }
            if (sent > 0) GameLog.Info("Equip", "auto-wear sent {0} 件(对标一键穿戴批量 15201)", sent);
        }

        public static void Clear()
        {
            _worn.Clear();
            _wornLoaded = false;
            _pendingUntil.Clear();
            _debouncing = false;
        }
    }
}
