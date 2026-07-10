using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备成长四件套(神兵淬炼/吞天洗魄/淬炉宗师;自动循环 轮4 队列#4)最小配置访问层,照 Skill.SkillConfigs 模式起。
    /// 对标老端 Config.PRELOAD_SERVER_CONFIG.config_equip_wash_unlock_lv / config_equip_refine_max /
    /// config_equip_whole_reward(老端 EquipWashView.ts/EquipSmeltView.ts/EquipStrenMasterView.ts 引用同名表)。
    ///
    /// 服务端 config 目录 Assets/GameRes/resource/config/server/ 下**当前没有**这三张表的 JSON 落地(该目录只有
    /// config_equip_attr/config_equip_strengthen_max/config_equip_stren_lv[_key] 四张既有强化表,属于已实现的
    /// 15204/15205 系统,与本类无关)。EnsureLoaded 按 SkillConfigs 同款"缺表降级"处理:逐表找不到就记 Info 日志 +
    /// 置空,不阻塞、不臆造字段结构;getter 一律返回"未配置",调用方(各 View 客户端预校验)据此不拦截、直接发
    /// 协议、交服务端兜底判定(对标规格 §0 末条:"配置缺失时不拦直接发,并 log")。只加本轮用到的表/getter,
    /// 表到位后无需改调用方代码,getter 会自动读到真值。
    /// </summary>
    public static class EquipConfigs
    {
        private static JObject _washUnlockLv;   // config_equip_wash_unlock_lv:pos(字符串键)→{unlock_lv,...}
        private static JObject _refineMax;      // config_equip_refine_max:equip_type→{...}(神兵淬炼消耗/上限判定,本轮暂无 getter 消费)
        private static JObject _wholeReward;    // config_equip_whole_reward:type_lv→{...}(15260/61 全身奖励阶位,本轮暂无 getter 消费)
        private static bool _loaded;

        public static bool IsLoaded => _loaded;

        public static async Task EnsureLoaded()
        {
            if (_loaded) return;
            _washUnlockLv = await LoadOptional("config_equip_wash_unlock_lv");
            _refineMax = await LoadOptional("config_equip_refine_max");
            _wholeReward = await LoadOptional("config_equip_whole_reward");
            _loaded = true;
        }

        private static async Task<JObject> LoadOptional(string name)
        {
            string key = GameResPath.GetServerConfigPath(name);
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Info("Equip", "缺表 {0}(本轮未同步落地),对应客户端预校验不拦截、直接发协议(服务端兜底)", name);
                return null;
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        /// <summary>洗魄槽解锁等级门槛(config_equip_wash_unlock_lv[pos].unlock_lv);缺表/缺项 → false(不拦截)。</summary>
        public static bool TryGetWashUnlockLv(int pos, out int unlockLv)
        {
            unlockLv = 0;
            if (_washUnlockLv?[pos.ToString()] is JObject o)
            {
                unlockLv = o.Value<int?>("unlock_lv") ?? 0;
                return unlockLv > 0;
            }
            return false;
        }

        public static void Clear()
        {
            _washUnlockLv = null;
            _refineMax = null;
            _wholeReward = null;
            _loaded = false;
        }
    }
}
