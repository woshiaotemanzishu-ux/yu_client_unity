using System.Threading.Tasks;

namespace Shenxiao.Module.Core.StarEquip
{
    /// <summary>
    /// 星宿锻造(chc)配表——薄委托层,不持有任何数据。
    ///
    /// 裁决4(主控 spec_round23.md 裁决表#4)已定:星宿锻造(强化/强化大师/进化/进化池/进化倍率/
    /// 附魔/附魔大师/启灵/锻造KV共9张表)与星宿核心(PK1)消费的是同一份 cdn 配表,合用
    /// <see cref="StarEquipConfigs"/> 已加载的数据(见该文件 §2 星宿锻造 分区),严禁在本文件
    /// 二次 LoadServer/JObject.Parse——否则 876/930/222 条等大表会被重复解析,内存与加载耗时
    /// 翻倍(第23轮三镜头验收 blocker,已修复)。
    ///
    /// 本类只做锻造侧语义包装转发:EnsureLoaded 转发到 StarEquipConfigs.EnsureLoaded(数据只加载
    /// 一次、两处共享);StrengthCount/IsEvolutionRateEmpty 直接从 StarEquipConfigs 对应计数属性推导。
    /// 具体取值(GetStrength/GetEvolution/GetEnchantment/GetSpirit/GetForgeKv 等)请直接调用
    /// StarEquipConfigs 的 §2 访问器——全仓实测本类原先包的那套同名访问器零调用方,本轮已删除,
    /// 避免维护两套同名 API 造成认知负担;PK2 后续如需新访问方法,加在 StarEquipConfigs 侧
    /// (该文件所有权仍属 PK1,但只加只读访问器/不改加载逻辑)或直接在此类新增转发,不得再起
    /// 独立 JObject 缓存。
    /// </summary>
    public static class StarForgeConfigs
    {
        public static Task EnsureLoaded() => StarEquipConfigs.EnsureLoaded();

        public static int StrengthCount => StarEquipConfigs.StrengthCount;

        /// <summary>存疑项核实用:config_constellation_evolution_rate 是否真的是空表(应恒为 true)。</summary>
        public static bool IsEvolutionRateEmpty => StarEquipConfigs.EvolutionRateCount == 0;
    }
}
