using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.StarEquip
{
    /// <summary>
    /// 星宿锻造(chc,pt_232 兜底转发段 23210-23241)协议控制器——对标老客户端 chcController.ts。
    /// 类名定死 StarForgeController(星宿核心 PK1 的 ControllerHub 占位引用要用这个名字)。
    ///
    /// 四子系统:1强化 STREN/2进化 EVO/3附魔 MAGIC(客户端 UI 显示"觉醒",服务端内部叫 enchantment,纯命名
    /// 差异,见 <see cref="StarForgeModel.GetTypeStr"/>)/4启灵 SOUL。类型码不在 wire 上传输,由 cmd 号本身
    /// 区分(23210系/23220系/23230系/23240系),wire 里的 TypeId 字段实际是"EquipType"(星宿页 stype)。
    ///
    /// 门禁:客户端总开关 <see cref="StarForgeModel.OPEN_LV"/>=560(硬编码,chcModel.ts:68);服务端每子系统
    /// 各自 open_lv=580(config_constellation_forge_kv id1/2/3/5,读取走 StarEquipConfigs.GetForgeKv——
    /// StarForgeConfigs 已改薄委托层不持数据,第23轮三镜头 blocker 修复,见其类注释;
    /// yu_server constellation_forge.hrl:10-14)。二者是不同粒度的两层门槛,都要满足;服务端侧不满足时
    /// info 查询(23210/20/30/40)直接静默 skip(pp_constellation_forge.erl:24-32 等,不回包不报错)。
    ///
    /// 触发链(对标 chcController.ts InitEvent,chcController.ts:34-179):
    ///   · GAME_START:武装一次性防抖标记 _scmdMark=true(chcController.ts:39-45;老端同时调用的
    ///     model.DefineConstant() 是 dead code,见 StarForgeModel.STYPE_COUNT 注释,本端不复刻其"重算"
    ///     效果,因为它本来就没有效果)。
    ///   · CHANGE_LEVEL 精确等于 560(chcController.ts:54-60,不是>=)→ SendDefFmt() 批量补拉。
    ///   · DAY_CHANGE(chcController.ts:148-150)→ 无条件 SendDefFmt()。
    ///   · CHANGE_STAREQUIP(chcController.ts:46-53 与 :152-179,两个独立 Bind)→ 星宿装备变化时的防抖批量
    ///     补拉 / 单件精确重查,见 <see cref="OnStarEquipBatchChanged"/> / <see cref="OnStarEquipItemChanged"/>
    ///     的类注释——⚠这两个方法本轮先落地但未接线,因为 Unity 侧 Bag/GoodsModel 还没有按
    ///     GOODS_TYPE_CONSTELLATION(79)过滤的"星宿装备变化"信号源(该信号源属 StarEquip/Bag 系统职责,
    ///     不在 PK2 所有权范围),留给后续轮次把它们接上。
    /// </summary>
    public sealed class StarForgeController : BaseController
    {
        public static readonly StarForgeController Instance = new StarForgeController();
        private StarForgeController() { }

        private int _lastLevel = -1;
        private bool _scmdMark = true;

        protected override void Register()
        {
            RegisterProtocal(Proto.STARFORGE_STREN_INFO, On23210);
            RegisterProtocal(Proto.STARFORGE_STREN_ACTION, On23211);
            RegisterProtocal(Proto.STARFORGE_STREN_MASTER_INFO, On23212);
            RegisterProtocal(Proto.STARFORGE_STREN_MASTER_LIGHT, On23213);
            RegisterProtocal(Proto.STARFORGE_EVO_INFO, On23220);
            RegisterProtocal(Proto.STARFORGE_EVO_ACTION, On23221);
            RegisterProtocal(Proto.STARFORGE_MAGIC_INFO, On23230);
            RegisterProtocal(Proto.STARFORGE_MAGIC_ACTION, On23231);
            RegisterProtocal(Proto.STARFORGE_MAGIC_MASTER_INFO, On23232);
            RegisterProtocal(Proto.STARFORGE_MAGIC_MASTER_LIGHT, On23233);
            RegisterProtocal(Proto.STARFORGE_SOUL_INFO, On23240);
            RegisterProtocal(Proto.STARFORGE_SOUL_ACTION, On23241);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, SendDefFmt);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, SendDefFmt);
            _lastLevel = -1;
            _scmdMark = true;
            StarForgeModel.Instance.Clear();
            base.Dispose();
        }

        // ---------------------------------------------------------------------------------------
        // 触发链
        // ---------------------------------------------------------------------------------------

        private async void OnGameStart()
        {
            await StarForgeConfigs.EnsureLoaded();
            StarForgeModel.Instance.Clear();
            _lastLevel = RoleModel.Instance.Level;
            _scmdMark = true;
            GameLog.Info("StarForge", "GAME_START scmdMark=true lastLevel={0}", _lastLevel);
        }

        // 对标老端 chcController.ts:54-60:CHANGE_LEVEL 精确等于 560(不是>=)才复请求。照抄这个
        // "一次性触发点"写法,不改宽成>=560——万一升级路径跳过精确的 560 这一级,老端本就不会再触发,
        // 这是已知的老端行为而非本端引入的缺陷。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            if (role.Level == StarForgeModel.OPEN_LV) SendDefFmt();
        }

        /// <summary>批量请求四子系统入口数据(对标 chcController.ts:182-193 this.SendDefFmt)。
        /// 对每个 stype(1..STYPE_COUNT)依次请求 23210/23220/23230/23240(入口数据)+23212/23232(大师
        /// 列表),不含 23213/23233(点亮动作)——老端原样如此,只批量拉"看板"数据,点亮是玩家主动操作。</summary>
        public void SendDefFmt()
        {
            for (int stype = 1; stype <= StarForgeModel.STYPE_COUNT; stype++)
            {
                SendFmt(Proto.STARFORGE_STREN_INFO, "c", stype);
                SendFmt(Proto.STARFORGE_EVO_INFO, "c", stype);
                SendFmt(Proto.STARFORGE_MAGIC_INFO, "c", stype);
                SendFmt(Proto.STARFORGE_SOUL_INFO, "c", stype);
                SendFmt(Proto.STARFORGE_STREN_MASTER_INFO, "c", stype);
                SendFmt(Proto.STARFORGE_MAGIC_MASTER_INFO, "c", stype);
            }
        }

        /// <summary>
        /// 对标 chcController.ts:46-53(goods_model.Bind(GoodsModel.CHANGE_STAREQUIP,...)一次性防抖):
        /// 每局仅消费一次(GAME_START 重新武装 _scmdMark=true),首次收到"星宿装备变化"信号即批量补拉
        /// 一次 SendDefFmt,此后同局内的后续变化不再触发这条批量补拉(由下面 <see cref="OnStarEquipItemChanged"/>
        /// 做精确的单件重查)。
        /// ⚠尚未接线:Unity 侧 Bag/GoodsModel 还没有按 GOODS_TYPE_CONSTELLATION(79)过滤的"星宿装备变化"
        /// 事件源(老端该信号来自 GoodsModel.CHANGE_STAREQUIP,属 StarEquip 主系统/背包模型职责,不在本包
        /// PK2 所有权范围)。方法体先落地,一旦该事件源在后续轮次出现,直接 EventDispatcher.On 接上即可,
        /// 不必回来重翻老端。
        /// </summary>
        public void OnStarEquipBatchChanged()
        {
            if (!_scmdMark) return;
            _scmdMark = false;
            SendDefFmt();
        }

        /// <summary>
        /// 对标 chcController.ts:152-179(同一 CHANGE_STAREQUIP 事件的第二个 Bind,goods_list/is_create
        /// 版本):单件星宿装备变化(非首次全量同步 is_create,老端 `if (is_create) return` 跳过初始批量同步)
        /// 时,按其所属 page 精确重查四个入口协议(不含大师,大师数据不受装备穿脱变化影响)。
        /// 调用方(未来 GoodsModel 移植后)需自行按 basic.type==79 且 basic.subtype&lt;=10(而非材料
        /// 11-14,那是 UPDATE_GOODS_NUM 通道的职责)过滤后取 page 传入,本方法不做该过滤。
        /// ⚠同 <see cref="OnStarEquipBatchChanged"/>,尚未接线,方法体先落地。
        /// </summary>
        public void OnStarEquipItemChanged(int page)
        {
            SendFmt(Proto.STARFORGE_STREN_INFO, "c", page);
            SendFmt(Proto.STARFORGE_EVO_INFO, "c", page);
            SendFmt(Proto.STARFORGE_MAGIC_INFO, "c", page);
            SendFmt(Proto.STARFORGE_SOUL_INFO, "c", page);
        }

        // ---------------------------------------------------------------------------------------
        // 动作请求(供未来 UI 调用;老端对应 view 的 Fire(REQUEST_PROTO,...)/Fire(SEND_EVO_PROTO,...))
        // ---------------------------------------------------------------------------------------

        /// <summary>请求强化(对标 chcStrenView.ts:98;强化无自动购买语义,第三个字段 Type 恒传 0)。</summary>
        public void RequestStrengthen(int stype, int pos) => SendFmt(Proto.STARFORGE_STREN_ACTION, "ccc", stype, pos, 0);

        /// <summary>请求点亮强化大师(对标 chcMasterView.ts:68)。</summary>
        public void RequestLightStrengthMaster(int stype) => SendFmt(Proto.STARFORGE_STREN_MASTER_LIGHT, "c", stype);

        /// <summary>
        /// 请求进化(对标 chcController.ts:62-75 chcModel.SEND_EVO_PROTO 专用通道:WriteBegin(23221)+
        /// WriteFMT("c",TypeId)+WriteFMT("l",EquipId)+WriteFMT("c",Pos)+WriteFMT("h",count)+逐个
        /// WriteFMT("l",CequipId);costEquipIds 允许为空表,对应 count=0)。
        /// ⚠老端此处还有 StarEquipModel.waitShowMaster=true 副作用(chcController.ts:73),该静态标记属
        /// StarEquip(PK1)模型,本方法不越权触碰,留给 PK1/后续整合。
        /// </summary>
        public void RequestEvolve(int stype, long goodsId, int pos, List<long> costEquipIds)
        {
            costEquipIds ??= new List<long>();
            var fmt = new StringBuilder();
            fmt.Append('c').Append('l').Append('c').Append('h');
            for (int i = 0; i < costEquipIds.Count; i++) fmt.Append('l');

            var args = new List<object> { stype, goodsId, pos, costEquipIds.Count };
            foreach (long id in costEquipIds) args.Add(id);

            SendFmt(Proto.STARFORGE_EVO_ACTION, fmt.ToString(), args.ToArray());
        }

        /// <summary>请求附魔/觉醒(对标 chcMagicView.ts:97;autoBuy=材料不足时是否自动购买消耗品)。</summary>
        public void RequestEnchant(int stype, int pos, bool autoBuy)
            => SendFmt(Proto.STARFORGE_MAGIC_ACTION, "ccc", stype, pos, autoBuy ? 1 : 0);

        /// <summary>请求点亮附魔大师(对标 chcMasterView.ts:70)。</summary>
        public void RequestLightEnchantMaster(int stype) => SendFmt(Proto.STARFORGE_MAGIC_MASTER_LIGHT, "c", stype);

        /// <summary>请求启灵(对标 chcSoulView.ts:80)。</summary>
        public void RequestSpirit(int stype, int pos) => SendFmt(Proto.STARFORGE_SOUL_ACTION, "cc", stype, pos);

        // ---------------------------------------------------------------------------------------
        // 协议处理(字段/引用见 Proto.cs 对应常量的详细注释,此处不重复抄一遍)
        // ---------------------------------------------------------------------------------------

        private void On23210(NetReader r)
        {
            int code = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int stage = (int)r.ReadU32();
            int isMax = r.ReadU8();
            int buff = r.ReadU16();
            int count = r.ReadU16();
            var info = new StarForgeModel.TypeInfo { Stype = typeId, NextMasterLv = stage, IsMax = isMax, Buff = buff };
            for (int i = 0; i < count; i++)
            {
                long equipId = r.ReadU64();
                int pos = r.ReadU8();
                int lv = (int)r.ReadU32();
                info.EquipList.Add(new StarForgeModel.EquipStatus { EquipId = equipId, Pos = pos, Lv = lv });
            }
            StarForgeModel.Instance.SetInfo(StarForgeModel.TYPE_STREN, info);
            EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_INFO_UPDATE, StarForgeModel.TYPE_STREN, typeId);
            GameLog.Info("StarForge", "23210 强化界面 stype={0} code={1} stage={2} isMax={3} buff={4} count={5}",
                typeId, code, stage, isMax, buff, count);
        }

        private void On23211(NetReader r)
        {
            int code = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int pos = r.ReadU8();
            int type = r.ReadU8();
            int buff = r.ReadU16();
            int lv = (int)r.ReadU32();
            bool ok = code == 1;
            if (ok)
            {
                StarForgeModel.TypeInfo info = StarForgeModel.Instance.GetInfo(StarForgeModel.TYPE_STREN, typeId);
                if (info != null)
                {
                    info.Buff = buff;
                    if (info.ByPos.TryGetValue(pos, out StarForgeModel.EquipStatus st))
                    {
                        st.Lv = lv;
                        EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_INFO_UPDATE, StarForgeModel.TYPE_STREN, typeId);
                    }
                }
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_ACTION_RESULT, StarForgeModel.TYPE_STREN, code, ok);
            GameLog.Info("StarForge", "23211 强化结果 stype={0} pos={1} type={2} code={3} lv={4} ok={5}",
                typeId, pos, type, code, lv, ok);
        }

        private void On23212(NetReader r)
        {
            int code = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int count = r.ReadU16();
            var info = new StarForgeModel.MasterInfo { Stype = typeId };
            for (int i = 0; i < count; i++)
            {
                int masterLv = (int)r.ReadU32();
                int status = r.ReadU8();
                info.MasterList.Add((masterLv, status));
            }
            StarForgeModel.Instance.SetMaster(StarForgeModel.TYPE_STREN, info);
            EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_MASTER_UPDATE, StarForgeModel.TYPE_STREN, typeId);
            GameLog.Info("StarForge", "23212 强化大师 stype={0} code={1} count={2}", typeId, code, count);
        }

        private void On23213(NetReader r)
        {
            int code = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int masterLv = (int)r.ReadU32();
            bool ok = code == 1;
            if (ok)
            {
                StarForgeModel.MasterInfo info = StarForgeModel.Instance.GetMaster(StarForgeModel.TYPE_STREN, typeId);
                if (info != null)
                {
                    for (int i = 0; i < info.MasterList.Count; i++)
                    {
                        (int lv, int _) = info.MasterList[i];
                        info.MasterList[i] = (lv, lv <= masterLv ? StarForgeModel.MASTER_ACTIVED : StarForgeModel.MASTER_NOACT);
                    }
                    EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_MASTER_UPDATE, StarForgeModel.TYPE_STREN, typeId);
                }
                TipsManager.Toast("点亮成功");
                SendFmt(Proto.STARFORGE_STREN_INFO, "c", typeId); // 对标 chcController.ts:254 成功后重发 23210
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_MASTER_RESULT, StarForgeModel.TYPE_STREN, code);
            GameLog.Info("StarForge", "23213 点亮强化大师 stype={0} code={1} masterLv={2} ok={3}", typeId, code, masterLv, ok);
        }

        private void On23220(NetReader r)
        {
            int code = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int count = r.ReadU16();
            var info = new StarForgeModel.TypeInfo { Stype = typeId };
            for (int i = 0; i < count; i++)
            {
                long equipId = r.ReadU64();
                int pos = r.ReadU8();
                int lv = (int)r.ReadU32();
                int attrNum = r.ReadU16();
                info.EquipList.Add(new StarForgeModel.EquipStatus { EquipId = equipId, Pos = pos, Lv = lv, AttrNum = attrNum });
            }
            StarForgeModel.Instance.SetInfo(StarForgeModel.TYPE_EVO, info);
            EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_INFO_UPDATE, StarForgeModel.TYPE_EVO, typeId);
            GameLog.Info("StarForge", "23220 进化界面 stype={0} code={1} count={2}", typeId, code, count);
        }

        private void On23221(NetReader r)
        {
            int code = (int)r.ReadU32();
            int isSuccess = r.ReadU8();
            int typeId = r.ReadU8();
            long equipId = r.ReadU64();
            int pos = r.ReadU8();
            int lv = (int)r.ReadU32();
            int attrId = (int)r.ReadU32();
            if (code == 1)
            {
                bool applied = isSuccess == 1;
                if (applied)
                {
                    StarForgeModel.EquipStatus st = StarForgeModel.Instance.GetByPos(StarForgeModel.TYPE_EVO, typeId, pos);
                    if (st != null)
                    {
                        st.Lv = lv;
                        EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_INFO_UPDATE, StarForgeModel.TYPE_EVO, typeId);
                    }
                }
                EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_ACTION_RESULT, StarForgeModel.TYPE_EVO, code, applied);
            }
            else
            {
                // 老端 else 分支只 Util.ErrorCodeShow(code)、不 Fire(UPDATE_RESULT)——现网服务端实现下
                // 23221 的所有失败/等级不足分支都已误发成 23211(见 Proto.STARFORGE_EVO_ACTION 详注),
                // 这个分支在当前版本实际不可达,照抄结构存档,不删。
                ShowError(code);
            }
            GameLog.Info("StarForge", "23221 进化结果 stype={0} equipId={1} pos={2} code={3} isSuccess={4} lv={5} attrId={6}",
                typeId, equipId, pos, code, isSuccess, lv, attrId);
        }

        private void On23230(NetReader r)
        {
            int code = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int stage = (int)r.ReadU32();
            int isMax = r.ReadU8();
            int count = r.ReadU16();
            // ⚠无 Buff 字段(23230 write 子句比 23210 少这个字段,不要对齐错位)。
            var info = new StarForgeModel.TypeInfo { Stype = typeId, NextMasterLv = stage, IsMax = isMax };
            for (int i = 0; i < count; i++)
            {
                long equipId = r.ReadU64();
                int pos = r.ReadU8();
                int lv = (int)r.ReadU32();
                info.EquipList.Add(new StarForgeModel.EquipStatus { EquipId = equipId, Pos = pos, Lv = lv });
            }
            StarForgeModel.Instance.SetInfo(StarForgeModel.TYPE_MAGIC, info);
            EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_INFO_UPDATE, StarForgeModel.TYPE_MAGIC, typeId);
            GameLog.Info("StarForge", "23230 附魔(觉醒)界面 stype={0} code={1} stage={2} isMax={3} count={4}",
                typeId, code, stage, isMax, count);
        }

        private void On23231(NetReader r)
        {
            int code = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int pos = r.ReadU8();
            int type = (int)r.ReadU32(); // ⚠响应侧 32 位,不同于请求侧 8 位("ccc"),照 write 子句读
            int lv = (int)r.ReadU32();
            bool ok = code == 1;
            if (ok)
            {
                StarForgeModel.EquipStatus st = StarForgeModel.Instance.GetByPos(StarForgeModel.TYPE_MAGIC, typeId, pos);
                if (st != null)
                {
                    st.Lv = lv;
                    EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_INFO_UPDATE, StarForgeModel.TYPE_MAGIC, typeId);
                }
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_ACTION_RESULT, StarForgeModel.TYPE_MAGIC, code, ok);
            GameLog.Info("StarForge", "23231 附魔(觉醒)结果 stype={0} pos={1} type={2} code={3} lv={4} ok={5}",
                typeId, pos, type, code, lv, ok);
        }

        private void On23232(NetReader r)
        {
            int code = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int count = r.ReadU16();
            var info = new StarForgeModel.MasterInfo { Stype = typeId };
            for (int i = 0; i < count; i++)
            {
                int masterLv = (int)r.ReadU32();
                int status = r.ReadU8();
                info.MasterList.Add((masterLv, status));
            }
            StarForgeModel.Instance.SetMaster(StarForgeModel.TYPE_MAGIC, info);
            EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_MASTER_UPDATE, StarForgeModel.TYPE_MAGIC, typeId);
            GameLog.Info("StarForge", "23232 附魔大师 stype={0} code={1} count={2}", typeId, code, count);
        }

        private void On23233(NetReader r)
        {
            int code = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int masterLv = (int)r.ReadU32();
            bool ok = code == 1;
            if (ok)
            {
                StarForgeModel.MasterInfo info = StarForgeModel.Instance.GetMaster(StarForgeModel.TYPE_MAGIC, typeId);
                if (info != null)
                {
                    for (int i = 0; i < info.MasterList.Count; i++)
                    {
                        (int lv, int _) = info.MasterList[i];
                        info.MasterList[i] = (lv, lv <= masterLv ? StarForgeModel.MASTER_ACTIVED : StarForgeModel.MASTER_NOACT);
                    }
                    EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_MASTER_UPDATE, StarForgeModel.TYPE_MAGIC, typeId);
                }
                TipsManager.Toast("点亮成功");
                // 对标 chcController.ts:350 成功后重发 23230(⚠服务端 lighten_enchantment_master_do 内部还会
                // 额外主动重推一次 23210,见 lib_constellation_forge.erl:322-323,那是服务端自己的事,
                // 与本端这行客户端主动请求 23230 互不冲突,两条刷新各司其职)。
                SendFmt(Proto.STARFORGE_MAGIC_INFO, "c", typeId);
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_MASTER_RESULT, StarForgeModel.TYPE_MAGIC, code);
            GameLog.Info("StarForge", "23233 点亮附魔大师 stype={0} code={1} masterLv={2} ok={3}", typeId, code, masterLv, ok);
        }

        private void On23240(NetReader r)
        {
            int code = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int count = r.ReadU16();
            var info = new StarForgeModel.TypeInfo { Stype = typeId };
            for (int i = 0; i < count; i++)
            {
                long equipId = r.ReadU64();
                int pos = r.ReadU8();
                int isSpirit = r.ReadU8();
                info.EquipList.Add(new StarForgeModel.EquipStatus { EquipId = equipId, Pos = pos, IsSpirit = isSpirit });
            }
            StarForgeModel.Instance.SetInfo(StarForgeModel.TYPE_SOUL, info);
            EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_INFO_UPDATE, StarForgeModel.TYPE_SOUL, typeId);
            GameLog.Info("StarForge", "23240 启灵界面 stype={0} code={1} count={2}", typeId, code, count);
        }

        private void On23241(NetReader r)
        {
            int code = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int pos = r.ReadU8();
            int isSpirit = r.ReadU8();
            bool ok = code == 1;
            if (ok)
            {
                StarForgeModel.EquipStatus st = StarForgeModel.Instance.GetByPos(StarForgeModel.TYPE_SOUL, typeId, pos);
                if (st != null)
                {
                    st.IsSpirit = isSpirit;
                    EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_INFO_UPDATE, StarForgeModel.TYPE_SOUL, typeId);
                }
                // 老端成功后 Fire(OPEN_VIEW,"chcSuccessView",scmd) 直接开弹窗——本轮 UI 未接,不发窗口事件,
                // 结果已经过下面的 EVT_STARFORGE_ACTION_RESULT 通知,留给尾包 UI 决定要不要弹窗。
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_STARFORGE_ACTION_RESULT, StarForgeModel.TYPE_SOUL, code, ok);
            GameLog.Info("StarForge", "23241 启灵结果 stype={0} pos={1} code={2} isSpirit={3} ok={4}",
                typeId, pos, code, isSpirit, ok);
        }

        // 错误码表未移植,显码降级(同 GodBefallController.cs:39 等既有先例)。
        private static void ShowError(int code) => TipsManager.Toast("错误(" + code + ")");
    }
}
