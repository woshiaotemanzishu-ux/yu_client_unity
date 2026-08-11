using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 宝石(骸珀镶嵌)雕刻/升级/合成协议控制器(自动循环 轮4 下半/4b;对标老端 EquipController.ts on15210/on15211/
    /// on15215/on15216;服务端 pt_152)。UI 挂 EquipView tab2(EquipJewelView 主页签 + 雕刻子窗 EquipJewelCraveView)。
    /// 镶嵌/拆除(15208/15209)已在既有 <see cref="EquipStoneController"/>(规格 §0 明确保持不动),本控制器不重复,
    /// 只补雕刻(15210/11)+升级(15215)+合成(15216)三个新号。照 EquipStrenController 模板:单例 + Register 收发 +
    /// SendFmt + NetReader 手解 + TipsManager + EventDispatcher。
    /// </summary>
    public sealed class EquipJewelController : BaseController
    {
        public static readonly EquipJewelController Instance = new EquipJewelController();

        private EquipJewelController() { }

        /// <summary>15216 合成是否处在服务端语义要求的自循环续发中(对标老端 model.one_key_mark)。</summary>
        private bool _combineOneKeyMark;
        private readonly HashSet<long> _silentStoneUpgradeRequests = new HashSet<long>();

        /// <summary>15215 的精确结果事件；一键升级必须等待每次权威回包后再选择下一槽，禁止并发扫发。</summary>
        public event Action<int, int, bool> StoneUpgradeCompleted;

        protected override void Register()
        {
            RegisterProtocal(Proto.EQUIP_JEWEL_CRAVE_INFO, On15210);
            RegisterProtocal(Proto.EQUIP_JEWEL_CRAVE_DO, On15211);
            RegisterProtocal(Proto.EQUIP_JEWEL_STONE_UPGRADE, On15215);
            RegisterProtocal(Proto.EQUIP_JEWEL_STONE_COMBINE, On15216);
            RegisterProtocal(Proto.EQUIP_JEWEL_SUB_MOD_POWER, On15254);
            // 对标 EquipController.ts:224 GAME_START 循环 equip_pos=1..10 预拉雕刻信息。
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, RequestAllCraveInfo);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, RequestAllCraveInfo);
            EquipJewelModel.Instance.Clear();
            _combineOneKeyMark = false;
            _silentStoneUpgradeRequests.Clear();
            base.Dispose();
        }

        /// <summary>GAME_START:装备位固定 1..10(对标老端硬编码 for i=1..10),逐位查询 15210。</summary>
        private void RequestAllCraveInfo()
        {
            for (int pos = 1; pos <= 10; pos++) QueryCraveInfo(pos);
            // 对标老端 EquipController.ts:221-224：十个15210之后紧跟一次15261。
            EquipStrenController.Instance.QueryWholeAward();
        }

        /// <summary>15210 查询指定装备位雕刻信息(发 "c" equip_pos)。</summary>
        public void QueryCraveInfo(int equipPos)
        {
            SendFmt(Proto.EQUIP_JEWEL_CRAVE_INFO, "c", equipPos);
        }

        /// <summary>15211 雕刻(发 "cic" equip_pos, 材料type_id, one_key[0单次/1一键];对标老端 btn_crave/btn_allCrave)。</summary>
        public void Crave(int equipPos, int materialTypeId, bool oneKey)
        {
            SendFmt(Proto.EQUIP_JEWEL_CRAVE_DO, "cic", equipPos, materialTypeId, oneKey ? 1 : 0);
            GameLog.Info("Equip", "crave 15211 equip_pos={0} material={1} oneKey={2}", equipPos, materialTypeId, oneKey);
        }

        /// <summary>15215 宝石升级(发 "ccc" equip_pos, stone_pos, upgrade_type[0普通/1一键低级宝石/2直升丹])。</summary>
        public void UpgradeStone(int equipPos, int stonePos, int upgradeType, bool silentSuccess = false)
        {
            long requestKey = StoneUpgradeKey(equipPos, stonePos);
            if (silentSuccess) _silentStoneUpgradeRequests.Add(requestKey);
            else _silentStoneUpgradeRequests.Remove(requestKey);
            SendFmt(Proto.EQUIP_JEWEL_STONE_UPGRADE, "ccc", equipPos, stonePos, upgradeType);
            GameLog.Info("Equip", "upgradeStone 15215 equip_pos={0} stone_pos={1} upgrade_type={2} silentSuccess={3}",
                equipPos, stonePos, upgradeType, silentSuccess);
        }

        /// <summary>15254 子功能战力查询(发 "c" sub_mod;轮21 PF 补漏批)。唯一真实调用点是老端
        /// jewel/EquipJewelView.ts:463-465 `GetPowerOnProto`(视图打开/刷新时发 sub_mod=1)。服务端目前只认
        /// sub_mod==1(宝石/骸珀镶嵌),其余取值恒回 power=0(见 Proto.EQUIP_JEWEL_SUB_MOD_POWER 注释)。</summary>
        public void RequestSubModPower(int subMod = 1)
        {
            SendFmt(Proto.EQUIP_JEWEL_SUB_MOD_POWER, "c", subMod);
            GameLog.Info("Equip", "request 15254 sub_mod={0}", subMod);
        }

        /// <summary>
        /// 15216 宝石合成(发 "ic" type_id, is_one_key[0/1])。老端全仓库找不到"手动首发"的调用点(唯一 send call
        /// 是 on15216 成功后的自循环续发)——等同于当前客户端 UI 层面此功能不可达,只留协议兜底。Unity 同步只留
        /// 此 API 供未来真实入口对接,本轮不建按钮/不挂 UI 触发。
        /// </summary>
        public void CombineStone(int typeId, bool oneKey)
        {
            SendFmt(Proto.EQUIP_JEWEL_STONE_COMBINE, "ic", typeId, oneKey ? 1 : 0);
            GameLog.Info("Equip", "combineStone 15216 type_id={0} oneKey={1}", typeId, oneKey);
        }

        /// <summary>15210 回包:res:i, equip_pos:c, refine_lv:c, exp:i, attr_list[u16×{attr_id:c, attr_val:i}]。
        /// **refine_lv 是 1 字节**(服务端 pt_152.erl item_to_bin_2 实证:AttrId:8/AttrVal:32 同款结构,RefineLv:8 —
        /// 规格草稿写 refine_lv:h 有误,以服务端源码为准)。res==1 才落库;res!=1(该装备位当前无雕刻数据等正常空态)
        /// 只日志不弹 toast,对标老端静默。</summary>
        private void On15210(NetReader r)
        {
            int res = (int)r.ReadU32();
            int equipPos = r.ReadU8();
            int refineLv = r.ReadU8();
            long exp = r.ReadU32();
            List<(int attrId, long attrVal)> attrs = r.ReadArray(ReadCraveAttr);
            if (res != 1)
            {
                GameLog.Info("Equip", "15210 query fail res={0} equip_pos={1}", res, equipPos);
                return;
            }
            EquipJewelModel.Instance.Apply15210(equipPos, refineLv, exp, attrs);
            GameLog.Info("Equip", "15210 equip_pos={0} refine_lv={1} exp={2} attrCount={3} remaining={4}B",
                equipPos, refineLv, exp, attrs.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_JEWEL_UPDATE);
        }

        private static (int attrId, long attrVal) ReadCraveAttr(NetReader r)
        {
            return (r.ReadU8(), r.ReadU32());   // {attr_id:c, attr_val:i}
        }

        /// <summary>15211 回包:res:i, equip_pos:c, is_up:c, one_key:c。res==1 → 自动重发 15210 刷新该装备位
        /// (对标老端 on15211,老端成功本身无 toast);res!=1 显码降级(错误码表未移植)。</summary>
        private void On15211(NetReader r)
        {
            int res = (int)r.ReadU32();
            int equipPos = r.ReadU8();
            int isUp = r.ReadU8();
            int oneKey = r.ReadU8();
            if (res != 1)
            {
                TipsManager.Toast("雕刻失败(" + res + ")");   // 错误码表未移植,显码降级
                GameLog.Info("Equip", "15211 fail res={0} equip_pos={1}", res, equipPos);
                return;
            }
            GameLog.Info("Equip", "15211 ok equip_pos={0} is_up={1} one_key={2} remaining={3}B",
                equipPos, isUp, oneKey, r.Remaining);
            QueryCraveInfo(equipPos);   // 对标老端 on15211 成功后 SendFmtToGame(15210,"c",scmd.equip_type)
        }

        /// <summary>15215 回包:res:i, equip_pos:c, pos:c, type_id:i。res==1 → 就地改 GoodsDynamicModel 缓存里
        /// 该装备位当前穿戴实例的 stone_list[pos].TypeId(对标老端 on15215 直接改 dynamic.stone_list,不重新拉取)+
        /// toast「升级宝石成功」;res!=1 显码降级(错误码表未移植;常见=已到顶级/无宝石)。</summary>
        private void On15215(NetReader r)
        {
            int res = (int)r.ReadU32();
            int equipPos = r.ReadU8();
            int pos = r.ReadU8();
            int typeId = (int)r.ReadU32();
            long requestKey = StoneUpgradeKey(equipPos, pos);
            bool silentSuccess = _silentStoneUpgradeRequests.Remove(requestKey);
            if (res != 1)
            {
                TipsManager.Toast("升级宝石失败(" + res + ")");
                GameLog.Info("Equip", "15215 fail res={0} equip_pos={1} pos={2}", res, equipPos, pos);
                StoneUpgradeCompleted?.Invoke(equipPos, pos, false);
                return;
            }
            if (!silentSuccess) TipsManager.Toast("升级宝石成功");
            GameLog.Info("Equip", "15215 ok equip_pos={0} pos={1} type_id={2} remaining={3}B", equipPos, pos, typeId, r.Remaining);

            BagGoods worn = EquipAutoWear.GetWorn(equipPos);
            if (worn != null)
            {
                GoodsDynamicModel.Instance.Patch(worn.GoodsId, vo =>
                {
                    if (vo.StoneList == null) return;
                    for (int i = 0; i < vo.StoneList.Count; i++)
                    {
                        if (vo.StoneList[i].Pos == pos)
                        {
                            GoodsStoneSlot slot = vo.StoneList[i];
                            slot.TypeId = typeId;
                            vo.StoneList[i] = slot;
                            break;
                        }
                    }
                });
            }
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_JEWEL_UPDATE);
            StoneUpgradeCompleted?.Invoke(equipPos, pos, true);
        }

        private static long StoneUpgradeKey(int equipPos, int stonePos) => ((long)equipPos << 32) | (uint)stonePos;

        /// <summary>15216 回包:res:i, type_id:i, is_one_key:c。res==1 且 is_one_key==1 → 服务端语义要求自循环续发
        /// (对标老端 on15216:model.one_key_mark=true; SendFmtToGame(15216,"ic",scmd.type_id,1));res==1 且
        /// is_one_key==0 → toast「合成宝石成功」;res!=1 且之前处在自循环中(_combineOneKeyMark)→ 视为一键序列
        /// 自然耗尽材料而终止,仍 toast「合成宝石成功」(对标老端此分支语义,非真失败);其余 res!=1 显码降级。</summary>
        private void On15216(NetReader r)
        {
            int res = (int)r.ReadU32();
            int typeId = (int)r.ReadU32();
            int isOneKey = r.ReadU8();
            if (res == 1)
            {
                if (isOneKey == 1)
                {
                    _combineOneKeyMark = true;
                    GameLog.Info("Equip", "15216 ok type_id={0} one_key=1 → 自循环续发", typeId);
                    CombineStone(typeId, true);
                }
                else
                {
                    TipsManager.Toast("合成宝石成功");
                    GameLog.Info("Equip", "15216 ok type_id={0} one_key=0 remaining={1}B", typeId, r.Remaining);
                    EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_JEWEL_UPDATE);
                }
                return;
            }

            if (isOneKey == 1 && _combineOneKeyMark)
            {
                _combineOneKeyMark = false;
                TipsManager.Toast("合成宝石成功");   // 对标老端:一键序列跑到没有可合成对象时以失败包终止,仍算完成
                GameLog.Info("Equip", "15216 one_key 序列结束(fail res={0} type_id={1})", res, typeId);
                EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_JEWEL_UPDATE);
            }
            else
            {
                TipsManager.Toast("合成宝石失败(" + res + ")");
                GameLog.Info("Equip", "15216 fail res={0} type_id={1} one_key={2}", res, typeId, isOneKey);
            }
        }

        /// <summary>15254 回包:sub_mod:c, power:i(对标老端 On15254 → model.Fire(EquipEvent.SUBTYPE_POWER,scmd);
        /// 消费方 jewel/EquipJewelView.ts:206-212 只处理 sub_mod==1,更新"战力"展示控件)。Unity 暂无
        /// EquipJewelView 主战力展示位(仅有 CraveView 子窗),落数据层 + 复用既有 EVT_EQUIP_JEWEL_UPDATE 事件,
        /// 消费方 TODO(不新增专用事件,理由见 Proto.EQUIP_JEWEL_SUB_MOD_POWER 注释)。</summary>
        private void On15254(NetReader r)
        {
            int subMod = r.ReadU8();
            long power = r.ReadU32();
            EquipJewelModel.Instance.SetSubModPower(subMod, power);
            GameLog.Info("Equip", "15254 子功能战力 sub_mod={0} power={1}", subMod, power);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_JEWEL_UPDATE);
        }
    }
}
