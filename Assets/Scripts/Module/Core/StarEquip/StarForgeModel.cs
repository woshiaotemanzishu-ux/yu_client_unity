using System.Collections.Generic;

namespace Shenxiao.Module.Core.StarEquip
{
    /// <summary>
    /// 星宿锻造(chc,pt_232 兜底转发段 23210-23241)数据层——对标老客户端 chcModel.ts。
    /// 四子系统:1强化(STREN)/2进化(EVO)/3附魔(MAGIC,客户端 UI 显示文案"觉醒",服务端内部叫 enchantment,
    /// 纯命名差异,见 <see cref="GetTypeStr"/> 引 chcModel.ts:212-222)/4启灵(SOUL)。
    /// 四子类型码不在 wire 上传输——由客户端"用哪个 cmd 号"区分(23210系=STREN/23220系=EVO/23230系=MAGIC/
    /// 23240系=SOUL),wire 里的 TypeId 字段实际是"EquipType"(星宿页/stype,1..STYPE_COUNT),两个维度不要混淆。
    /// 索引形状对标老端 chcModel.ts:106-109 `_data_dic_[type][data.type_id]` / :142-145 `_master_dic_[type][data.type_id]`。
    /// </summary>
    public sealed class StarForgeModel
    {
        public static readonly StarForgeModel Instance = new StarForgeModel();
        private StarForgeModel() { }

        // ---- 四子类型码(chcModel.ts:52 public static CHCTYPE = {STREN:1,EVO:2,MAGIC:3,SOUL:4};
        //      与服务端 config_constellation_forge_kv id6-9 的"功能"映射值(1/2/3/4)一致) ----
        public const int TYPE_STREN = 1;
        public const int TYPE_EVO = 2;
        public const int TYPE_MAGIC = 3; // 服务端叫"附魔"/enchantment;客户端 UI 文案"觉醒"
        public const int TYPE_SOUL = 4;

        // ---- 大师点亮状态(yu_server include/constellation_forge.hrl:17-19) ----
        public const int MASTER_NOACT = 0;
        public const int MASTER_ACTIVE = 1;
        public const int MASTER_ACTIVED = 2;

        /// <summary>
        /// 客户端总开关等级(chcModel.ts:68 `public static OPEN_LV = 560`,硬编码)。
        /// ⚠与星宿装备主系统 open_lv=560(config_constellation_kv)数值巧合相同,但这是两处各自独立的
        /// 硬编码/配置,并非同一来源——不要因为数值相同就合并成一份读取。
        /// </summary>
        public const int OPEN_LV = 560;

        /// <summary>
        /// 星宿"页"(stype)总数。老端 chcModel.ts:84 `this._stype_count = 5` 硬编码。
        /// ⚠chcModel.DefineConstant()(chcModel.ts:85-97)本应该从 config_constellation_forge_kv /
        /// config_constellation_page 里重新算出这个值并覆盖,但整段函数体被注释掉(纯 dead code),
        /// chcController.ts 在 GAME_START/CHANGE_LEVEL 里仍会调用这个空函数(无副作用)——
        /// 实际运行值自始至终就是这个硬编码 5,从未被真正复写。本端照抄这个"看似可配置、实际写死"的
        /// 行为,不读配置覆盖此常量。
        /// </summary>
        public const int STYPE_COUNT = 5;

        /// <summary>
        /// 单个装备位在某子系统下的状态,合并四种 wire item 形状(字段依类型选用):
        ///   STREN(23210 item):EquipId:64,Pos:8,Lv:32
        ///   EVO  (23220 item):EquipId:64,Pos:8,Lv:32,AttrNum:16
        ///   MAGIC(23230 item):EquipId:64,Pos:8,Lv:32(与 STREN 同形状)
        ///   SOUL (23240 item):EquipId:64,Pos:8,IsSpirit:8(无 Lv 概念,恒 0)
        /// </summary>
        public sealed class EquipStatus
        {
            public long EquipId;
            public int Pos;
            public int Lv;        // 仅 STREN/EVO/MAGIC 有意义
            public int AttrNum;   // 仅 EVO(卓越属性条数)
            public int IsSpirit;  // 仅 SOUL(0/1)
        }

        /// <summary>某"页"(stype)在某子系统下的完整入口数据(23210/23220/23230/23240 落地)。</summary>
        public sealed class TypeInfo
        {
            public int Stype;
            public int NextMasterLv; // Stage:仅 STREN(23210)/MAGIC(23230)有意义,EVO/SOUL 恒 0
            public int IsMax;        // 仅 STREN/MAGIC
            public int Buff;         // 仅 STREN(23210 独有字段;23230 write 子句无此字段,恒 0)
            public readonly List<EquipStatus> EquipList = new List<EquipStatus>();
            public readonly Dictionary<int, EquipStatus> ByPos = new Dictionary<int, EquipStatus>();
        }

        /// <summary>大师点亮列表(23212/23232 落地)。仅 STREN/MAGIC 两子系统有大师概念,EVO/SOUL 没有。</summary>
        public sealed class MasterInfo
        {
            public int Stype;
            public readonly List<(int MasterLv, int Status)> MasterList = new List<(int MasterLv, int Status)>();
        }

        // 索引:[chcType 1..4][stype]。下标 0 不用,对齐老端 CHCTYPE 从 1 起数。
        // _master 只有 STREN(1)/MAGIC(3) 非 null——EVO/SOUL 没有大师概念,误用直接 no-op 而不是抛异常。
        private readonly Dictionary<int, TypeInfo>[] _info =
        {
            null,
            new Dictionary<int, TypeInfo>(),
            new Dictionary<int, TypeInfo>(),
            new Dictionary<int, TypeInfo>(),
            new Dictionary<int, TypeInfo>(),
        };

        private readonly Dictionary<int, MasterInfo>[] _master =
        {
            null,
            new Dictionary<int, MasterInfo>(),
            null,
            new Dictionary<int, MasterInfo>(),
            null,
        };

        private static bool ValidType(int chcType) => chcType >= 1 && chcType <= 4;

        public void SetInfo(int chcType, TypeInfo info)
        {
            if (!ValidType(chcType) || info == null) return;
            info.ByPos.Clear();
            foreach (EquipStatus e in info.EquipList) info.ByPos[e.Pos] = e;
            _info[chcType][info.Stype] = info;
        }

        public TypeInfo GetInfo(int chcType, int stype)
        {
            if (!ValidType(chcType)) return null;
            return _info[chcType].TryGetValue(stype, out TypeInfo v) ? v : null;
        }

        public EquipStatus GetByPos(int chcType, int stype, int pos)
        {
            TypeInfo info = GetInfo(chcType, stype);
            return info != null && info.ByPos.TryGetValue(pos, out EquipStatus st) ? st : null;
        }

        /// <summary>该 chcType 下所有已落地的 stype 数据(遍历/统计用)。</summary>
        public IReadOnlyDictionary<int, TypeInfo> GetAllInfo(int chcType)
            => ValidType(chcType) ? _info[chcType] : null;

        public void SetMaster(int chcType, MasterInfo info)
        {
            if (!ValidType(chcType) || info == null) return;
            Dictionary<int, MasterInfo> dict = _master[chcType];
            if (dict == null) return; // EVO/SOUL 无大师概念
            dict[info.Stype] = info;
        }

        public MasterInfo GetMaster(int chcType, int stype)
        {
            if (!ValidType(chcType)) return null;
            Dictionary<int, MasterInfo> dict = _master[chcType];
            return dict != null && dict.TryGetValue(stype, out MasterInfo v) ? v : null;
        }

        /// <summary>子系统中文名(对标 chcModel.ts:212-222 GetTypeStr)。3=MAGIC 显示"觉醒"(服务端内部叫附魔)。</summary>
        public static string GetTypeStr(int chcType)
        {
            switch (chcType)
            {
                case TYPE_STREN: return "强化";
                case TYPE_EVO: return "进化";
                case TYPE_MAGIC: return "觉醒";
                case TYPE_SOUL: return "启灵";
                default: return "";
            }
        }

        /// <summary>断线/登出清态(对标老端每次 GAME_START 前的隐含重置——chc 无独立 Reset,但同族 GodBefall/
        /// DragonBall 等模型均在 Dispose/GAME_START 清态,本模型照此惯例提供)。</summary>
        public void Clear()
        {
            for (int t = 1; t <= 4; t++)
            {
                _info[t]?.Clear();
                _master[t]?.Clear();
            }
        }
    }
}
