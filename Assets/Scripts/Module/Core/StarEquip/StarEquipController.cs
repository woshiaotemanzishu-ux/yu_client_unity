using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.StarEquip
{
    /// <summary>
    /// 星宿核心(pp_constellation_equip,pt_232 直接处理段)控制器:23200/23201/23202/23203/23205/23206/
    /// 23207/23208/23209/23250/23251/23252/23253/23254/23255/23256/23257,共 17 号。纯数据层接入
    /// (UI 层 starEquip 20+ 个 scene 连 prefab 都没烤,chc 24 个 view 已烤但零逻辑接入——均留 #23b 尾包,
    /// 见 r23_starequip.md/r23_chc.md 侦察档案)。星宿锻造(chc/StarForge,PK2,23210-23241)是本家族兜底
    /// 转发的另一半,分文件不分模块(见 <see cref="StarEquipConfigs"/> 类注释的所有权铁律)。
    ///
    /// 纪律/wire 要点(逐号原文核对,见 Proto.cs 对应常量注释,不重复抄录细节,这里只记跨号的通用规则):
    /// ①**23204 按裁决1 killlist**:老端注册 On23204 但全仓零发送点(请求方向死,响应永不触发),本类
    ///   不注册、不提供发送方法(同 40218/40263 先例"永不触发的接收严禁注册")。
    /// ②**错误路由不统一,按号各异**:多数号"自身无 Code 字段"、失败经 <see cref="Proto.STAREQUIP_ERROR"/>
    ///   (23200)统一出口(23202/23203/23205/23207/23209/23250/23254/23255/23257 皆此类,23253/23208 虽带
    ///   Code 字段但该字段在实践中恒为成功、真失败也走 23200);**23206/23252 是例外**,自带真实失败码,
    ///   不经 23200。逐号具体在 Proto.cs 常量注释里写清,Controller 里对应 On&lt;num&gt; 照抄。
    /// ③**23201 TotalStar 是 u16**(不是常见的 32 位),23206 Level 是 u16、Power 是 u32,依此类推——每号
    ///   位宽严格照 Proto.cs 注释抄的 pt_232.erl write 子句,不套用别的家族的默认位宽假设。
    /// ④**23209/23252 是变长 C2S**(WriteBegin 风格:先发 u16 计数字段再循环发元素,本端用
    ///   StringBuilder 拼 fmt 字符串+参数表,同 MarriageController.RequestIssue 先例),不是走某个
    ///   "数组"格式字符——本仓 fmt 协议本就没有数组字符,计数是显式一个 'h' 字段。
    /// ⑤**23252 合成四个不同出口都写 23252**(check_compose 前置失败/材料扣除失败/随机未中奖/成功,
    ///   分别用不同 Code:见 Proto.cs 常量注释逐一列出行号),On23252 必须同时处理 code∈{1,1500080}成功、
    ///   code==1500081(err150_compose_fail)特判、其余通用失败 三条分支(对标老端 ts:368-379)。
    /// ⑥**23250/23255 门禁豁免**:pp_constellation_equip.erl:40 特例放行,未达 open_lv/open_day_limit 时
    ///   这两号仍会被服务端处理——Unity 侧不需要额外处理(客户端从不主动判断服务端门禁,只是老实发包,
    ///   该字段仅供理解"为什么这两号总有响应"这一事实)。
    /// ⑦**GAME_START/CHANGE_LEVEL 触发链有意不完全照抄老端"=="精确匹配**:老端 GAME_START(ts:467-476)
    ///   恒发 23201+23207(**不**含 23205,23205 由 On23201 内部按 total_star 变化差值补发,ts:150-152);
    ///   CHANGE_LEVEL(ts:459-465)在 `model.OpenLv==level` 精确相等时补发 23201+23205+23207 三连,
    ///   而 model.OpenLv 取自 ConfigFuncOpenCondition["StarEquipView"].open_lv(实测=**580**,与服务端
    ///   门禁用的 config_constellation_kv open_lv=**560** 是两个不同来源的数字,不是同一个 560)。本仓无
    ///   CHANGE_LEVEL 专属事件,且"精确等于某数值"对批量升级不健壮——改用共享基础设施
    ///   <see cref="FuncOpenConfig"/>.CheckFuncOpenState("StarEquipView")做"跨越开放阈值"(false→true)的
    ///   等价触发,经 <see cref="GlobalEvent.EVT_ROLE_INFO_UPDATE"/> 探测(同 DailyController/
    ///   MarriageController 先例),语义等价但对批量升级更健壮,牺牲了老端"精确等于"这个非稳健写法的
    ///   逐字节复刻——有意偏差,原因见此条。
    /// </summary>
    public sealed class StarEquipController : BaseController
    {
        public static readonly StarEquipController Instance = new StarEquipController();
        private StarEquipController() { }

        /// <summary>对标老端 model.OpenLv==level 判定基准的等价物:上次探测到的
        /// FuncOpenConfig.CheckFuncOpenState("StarEquipView") 结果,见类注释⑦。</summary>
        private bool _starEquipWasOpen;

        private static void ShowError(int errorCode) => TipsManager.Toast("错误(" + errorCode + ")"); // 错误码表未移植,显码降级

        protected override void Register()
        {
            RegisterProtocal(Proto.STAREQUIP_ERROR, On23200);
            RegisterProtocal(Proto.STAREQUIP_OVERVIEW, On23201);
            RegisterProtocal(Proto.STAREQUIP_WEAR, On23202);
            RegisterProtocal(Proto.STAREQUIP_UNWEAR, On23203);
            // 23204:裁决1 killlist,不发不收,本类不注册(见类注释①)。
            RegisterProtocal(Proto.STAREQUIP_STAR_MASTER_INFO, On23205);
            RegisterProtocal(Proto.STAREQUIP_STAR_MASTER_UP, On23206);
            RegisterProtocal(Proto.STAREQUIP_DEVOUR_INFO, On23207);
            RegisterProtocal(Proto.STAREQUIP_DEVOUR_TAB, On23208);
            RegisterProtocal(Proto.STAREQUIP_DEVOUR, On23209);
            RegisterProtocal(Proto.STAREQUIP_TIPS_PREVIEW, On23250);
            RegisterProtocal(Proto.STAREQUIP_STAR_PUSH, On23251);
            RegisterProtocal(Proto.STAREQUIP_COMPOSE, On23252);
            RegisterProtocal(Proto.STAREQUIP_UNLOCK_PAGE, On23253);
            RegisterProtocal(Proto.STAREQUIP_TRANSFORM_PREVIEW, On23254);
            RegisterProtocal(Proto.STAREQUIP_TYPE_TIPS_PREVIEW, On23255);
            RegisterProtocal(Proto.STAREQUIP_COMPOSE_TIME, On23256);
            RegisterProtocal(Proto.STAREQUIP_TRANSFORM, On23257);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            _starEquipWasOpen = false;
            StarEquipModel.Instance.Clear();
            base.Dispose();
        }

        // ---------------------------------------------------------------------------------------
        // 登录/升级触发链(见类注释⑦)
        // ---------------------------------------------------------------------------------------

        private async void OnGameStart()
        {
            await StarEquipConfigs.EnsureLoaded();
            RequestOverview();    // 23201,老端恒发,ts:470
            RequestDevourInfo();  // 23207,老端恒发,ts:471(23205 不在此列,由 On23201 差值补发)
            await FuncOpenConfig.EnsureLoaded();
            _starEquipWasOpen = FuncOpenConfig.IsLoaded && FuncOpenConfig.CheckFuncOpenState("StarEquipView");
            GameLog.Info("StarEquip", "GAME_START 恒发23201/23207 wasOpen={0}", _starEquipWasOpen);
        }

        /// <summary>等价于老端 CHANGE_LEVEL==OpenLv 精确匹配的"跨越阈值"探测,见类注释⑦。</summary>
        private void OnRoleInfoUpdate()
        {
            if (!FuncOpenConfig.IsLoaded) return;
            bool nowOpen = FuncOpenConfig.CheckFuncOpenState("StarEquipView");
            if (nowOpen && !_starEquipWasOpen)
            {
                RequestOverview();
                RequestStarMaster();
                RequestDevourInfo();
                GameLog.Info("StarEquip", "跨越 StarEquipView 开放阈值,补发23201/23205/23207(对标老端 CHANGE_LEVEL==580 三连,ts:461-463)");
            }
            _starEquipWasOpen = nowOpen;
        }

        // ---------------------------------------------------------------------------------------
        // 公共读取小工具(attr_list / StarAttrCfg / SendDsgt / 完整属性预览,23250/23254/23255 共用)
        // ---------------------------------------------------------------------------------------

        private static List<StarEquipModel.AttrEntry> ReadAttrList(NetReader r) =>
            r.ReadArray(rr => new StarEquipModel.AttrEntry { AttrId = rr.ReadU16(), AttrVal = rr.ReadU32() });

        private static List<StarEquipModel.AdditionAttrEntry> ReadAdditionAttrList(NetReader r) =>
            r.ReadArray(rr => new StarEquipModel.AdditionAttrEntry
            {
                AttrId = rr.ReadU16(), AttrVal = rr.ReadU32(), PlusInterval = rr.ReadU8(),
                PlusUnit = rr.ReadU32(), Color = rr.ReadU8(), TypeId = rr.ReadU8(),
            });

        private static List<StarEquipModel.DsgtEntry> ReadDsgtList(NetReader r) => r.ReadArray(rr =>
        {
            var e = new StarEquipModel.DsgtEntry { DsgtId = rr.ReadI32(), DsgtNum = rr.ReadU16() };
            e.DsgtSuit.AddRange(ReadAttrList(rr));
            e.DsgtAttr.AddRange(ReadAttrList(rr));
            return e;
        });

        /// <summary>23250/23254 共用完整预览读取(includeTarget=true 时多读一个 TargetGoodsAutoId:64,
        /// 对标 23254 比 23250 在最前多一个字段,见 Proto.cs 两常量注释)。</summary>
        private static StarEquipModel.TipsPreview ReadTipsPreview(NetReader r, bool includeTarget)
        {
            var p = new StarEquipModel.TipsPreview { GoodsAutoId = r.ReadU64() };
            if (includeTarget) p.TargetGoodsAutoId = r.ReadU64();
            p.Score = r.ReadU32();
            p.SendDsgt.AddRange(ReadDsgtList(r));
            p.StarAttrCfg.AddRange(ReadAdditionAttrList(r));
            p.StarAttr.AddRange(ReadAttrList(r));
            p.SuitNum = r.ReadU16();
            p.SuitAttr.AddRange(ReadAttrList(r));
            p.BaseAttr.AddRange(ReadAttrList(r));
            p.ExtraAttr.AddRange(ReadAttrList(r));
            p.StrenAttr.AddRange(ReadAttrList(r));
            p.EvoluAttr.AddRange(ReadAttrList(r));
            p.MasterAttr.AddRange(ReadAttrList(r));
            p.SpiritAttr.AddRange(ReadAttrList(r));
            p.BaseRating = r.ReadU32();
            return p;
        }

        /// <summary>C2S 变长数组通用拼包(u16 计数 + N×l 元素,见类注释④)。</summary>
        private static void AppendVarLongArray(StringBuilder fmt, List<object> args, IReadOnlyList<long> list)
        {
            int count = list?.Count ?? 0;
            fmt.Append('h');
            args.Add(count);
            for (int i = 0; i < count; i++) { fmt.Append('l'); args.Add(list[i]); }
        }

        // ---------------------------------------------------------------------------------------
        // 23200 族错误出口
        // ---------------------------------------------------------------------------------------

        private void On23200(NetReader r)
        {
            int errorCode = r.ReadI32();
            string args = r.ReadString();
            if (errorCode == 1500081) // err150_compose_fail,死分支镜像,见 Proto.cs STAREQUIP_ERROR 注释
            {
                EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_COMPOSE_FAIL);
            }
            else
            {
                ShowError(errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_ERROR, errorCode, args);
            GameLog.Info("StarEquip", "23200 族错误 code={0} args={1}", errorCode, args);
        }

        // ---------------------------------------------------------------------------------------
        // 23201 总览
        // ---------------------------------------------------------------------------------------

        public void RequestOverview() => SendFmt(Proto.STAREQUIP_OVERVIEW); // read(23201,_)->{ok,[]}

        private void On23201(NetReader r)
        {
            int totalStar = r.ReadU16(); // ⚠u16,见类注释③
            List<StarEquipModel.PageItem> items = r.ReadArray(ReadPageItem);
            int oldTotalStar = StarEquipModel.Instance.SetOverview(totalStar, items);
            if (oldTotalStar != totalStar) RequestStarMaster(); // 老端 ts:150-151 差值补发23205
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_OVERVIEW_UPDATE);
            GameLog.Info("StarEquip", "23201 总览 totalStar={0} items={1} starMasterRefetch={2}",
                totalStar, items.Count, oldTotalStar != totalStar);
        }

        private static StarEquipModel.PageItem ReadPageItem(NetReader r)
        {
            var it = new StarEquipModel.PageItem { Page = r.ReadI32(), Power = r.ReadU64(), NormalNum = r.ReadU8(), SpecialNum = r.ReadU8() };
            it.Attr.AddRange(ReadAttrList(r));
            it.IsActive = r.ReadU8();
            return it;
        }

        // ---------------------------------------------------------------------------------------
        // 23202 穿戴 / 23203 卸下(均"无 Code",仅成功回本号,失败经23200)
        // ---------------------------------------------------------------------------------------

        public void RequestWear(long goodsAutoId, int constellationPage, int isReplace) =>
            SendFmt(Proto.STAREQUIP_WEAR, "lic", goodsAutoId, constellationPage, isReplace);

        private void On23202(NetReader r)
        {
            long goodsAutoId = r.ReadU64();
            long goodsTypeId = r.ReadU32();
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_WEAR_RESULT, goodsAutoId, goodsTypeId);
            GameLog.Info("StarEquip", "23202 穿戴成功(无Code) goodsAutoId={0} goodsTypeId={1}", goodsAutoId, goodsTypeId);
        }

        public void RequestUnwear(int constellationPage, int pos) =>
            SendFmt(Proto.STAREQUIP_UNWEAR, "ic", constellationPage, pos);

        private void On23203(NetReader r)
        {
            long goodsAutoId = r.ReadU64();
            long goodsTypeId = r.ReadU32();
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_UNWEAR_RESULT, goodsAutoId, goodsTypeId);
            GameLog.Info("StarEquip", "23203 卸下成功(无Code) goodsAutoId={0} goodsTypeId={1}", goodsAutoId, goodsTypeId);
        }

        // ---------------------------------------------------------------------------------------
        // 23205 星级大师界面 / 23206 升级 / 23251 星数被动推送
        // ---------------------------------------------------------------------------------------

        public void RequestStarMaster() => SendFmt(Proto.STAREQUIP_STAR_MASTER_INFO); // read(23205,_)->{ok,[]}

        private void On23205(NetReader r)
        {
            StarEquipModel.StarMasterInfo info = ReadStarMasterInfo(r);
            StarEquipModel.Instance.SetStarMaster(info);
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_STAR_MASTER_INFO_UPDATE);
            GameLog.Info("StarEquip", "23205 星级大师信息 level={0} star={1} power={2}", info.Level, info.Star, info.Power);
        }

        private static StarEquipModel.StarMasterInfo ReadStarMasterInfo(NetReader r) => new StarEquipModel.StarMasterInfo
        {
            Level = r.ReadU16(), MaxLevel = r.ReadU16(), Star = r.ReadU16(), Power = r.ReadU32(),
        };

        public void RequestStarMasterUp(int starLevel) => SendFmt(Proto.STAREQUIP_STAR_MASTER_UP, "h", starLevel);

        /// <summary>23206 自带真实失败码,不经23200(见类注释②)。</summary>
        private void On23206(NetReader r)
        {
            int code = r.ReadI32();
            int level = r.ReadU16();
            long power = r.ReadU32();
            if (code == 1)
            {
                StarEquipModel.Instance.ApplyStarMasterUp(level, power);
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_STAR_MASTER_UP_RESULT, code == 1, code);
            GameLog.Info("StarEquip", "23206 星级大师升级 code={0} level={1} power={2}", code, level, power);
        }

        /// <summary>23251 纯被动推送,无对应 C2S(pt_232.erl 无 read(23251) 子句)。</summary>
        private void On23251(NetReader r)
        {
            StarEquipModel.StarMasterInfo info = ReadStarMasterInfo(r);
            StarEquipModel.Instance.SetStarPush(info);
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_STAR_PUSH_UPDATE);
            GameLog.Info("StarEquip", "23251 星数被动推送 level={0} star={1} power={2}", info.Level, info.Star, info.Power);
        }

        // ---------------------------------------------------------------------------------------
        // 23207 吞噬信息 / 23208 筛选 / 23209 执行
        // ---------------------------------------------------------------------------------------

        public void RequestDevourInfo() => SendFmt(Proto.STAREQUIP_DEVOUR_INFO); // read(23207,_)->{ok,[]}

        private void On23207(NetReader r)
        {
            var info = new StarEquipModel.DevourInfo
            {
                Level = r.ReadU16(), Exp = r.ReadU32(), Power = r.ReadU32(), Color = r.ReadU8(), Star = r.ReadU8(),
            };
            StarEquipModel.Instance.SetDevourInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_DEVOUR_INFO_UPDATE);
            GameLog.Info("StarEquip", "23207 吞噬信息 level={0} exp={1} color={2} star={3}", info.Level, info.Exp, info.Color, info.Star);
        }

        public void RequestDevourTab(int newColor, int newStar) => SendFmt(Proto.STAREQUIP_DEVOUR_TAB, "cc", newColor, newStar);

        private void On23208(NetReader r)
        {
            int color = r.ReadU8();
            int star = r.ReadU8();
            int code = r.ReadI32(); // Code 在末尾,见类注释②
            if (code == 1)
            {
                StarEquipModel.Instance.ApplyDevourTab(color, star);
                EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_DEVOUR_TAB_RESULT);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("StarEquip", "23208 吞噬筛选 code={0} color={1} star={2}", code, color, star);
        }

        /// <summary>23209 变长 C2S(见类注释④):吞噬材料 goods_auto_id 列表,u16 计数 + N×u64。</summary>
        public void RequestDevour(IReadOnlyList<long> materialGoodsAutoIds)
        {
            var fmt = new StringBuilder();
            var args = new List<object>();
            AppendVarLongArray(fmt, args, materialGoodsAutoIds);
            SendFmt(Proto.STAREQUIP_DEVOUR, fmt.ToString(), args.ToArray());
        }

        private void On23209(NetReader r)
        {
            var result = new StarEquipModel.DevourResult { Level = r.ReadU16(), Exp = r.ReadU32(), Power = r.ReadU32() };
            StarEquipModel.Instance.ApplyDevourResult(result);
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_DEVOUR_RESULT);
            GameLog.Info("StarEquip", "23209 吞噬执行成功(无Code) level={0} exp={1} power={2}", result.Level, result.Exp, result.Power);
        }

        // ---------------------------------------------------------------------------------------
        // 23250 装备属性预览 / 23254 蜕变对比预览 / 23255 类型tips
        // ---------------------------------------------------------------------------------------

        public void RequestTipsPreview(long roleId, long goodsAutoId) => SendFmt(Proto.STAREQUIP_TIPS_PREVIEW, "ll", roleId, goodsAutoId);

        private void On23250(NetReader r)
        {
            StarEquipModel.TipsPreview p = ReadTipsPreview(r, includeTarget: false);
            StarEquipModel.Instance.SetLastPreview(p);
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_PREVIEW_UPDATE);
            GameLog.Info("StarEquip", "23250 装备属性预览(无Code) goodsAutoId={0} score={1}", p.GoodsAutoId, p.Score);
        }

        public void RequestTransformPreview(long goodsAutoId, long targetGoodsAutoId) =>
            SendFmt(Proto.STAREQUIP_TRANSFORM_PREVIEW, "ll", goodsAutoId, targetGoodsAutoId);

        private void On23254(NetReader r)
        {
            StarEquipModel.TipsPreview p = ReadTipsPreview(r, includeTarget: true);
            StarEquipModel.Instance.SetLastTransformPreview(p);
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_TRANSFORM_PREVIEW_UPDATE);
            GameLog.Info("StarEquip", "23254 蜕变对比预览(无Code) goodsAutoId={0} targetGoodsAutoId={1} score={2}",
                p.GoodsAutoId, p.TargetGoodsAutoId, p.Score);
        }

        public void RequestTypeTipsPreview(long goodsTypeId) => SendFmt(Proto.STAREQUIP_TYPE_TIPS_PREVIEW, "i", goodsTypeId);

        private void On23255(NetReader r)
        {
            var p = new StarEquipModel.TypeTipsPreview { GoodsTypeId = r.ReadU32(), Score = r.ReadU32() };
            p.SendDsgt.AddRange(ReadDsgtList(r));
            p.StarAttrCfg.AddRange(ReadAdditionAttrList(r));
            p.StarAttr.AddRange(ReadAttrList(r));
            p.SuitNum = r.ReadU16();
            p.BaseAttr.AddRange(ReadAttrList(r));   // ⚠23255 比 23250/23254 少 SuitAttr/锻造四段属性,见 Proto.cs 注释
            p.ExtraAttr.AddRange(ReadAttrList(r));
            p.BaseRating = r.ReadU32();
            StarEquipModel.Instance.SetTypePreview(p.GoodsTypeId, p);
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_TYPE_PREVIEW_UPDATE, p.GoodsTypeId);
            GameLog.Info("StarEquip", "23255 类型tips预览(无Code) goodsTypeId={0} score={1}", p.GoodsTypeId, p.Score);
        }

        // ---------------------------------------------------------------------------------------
        // 23252 合成 / 23253 解锁星宿页 / 23256 合成次数 / 23257 蜕变执行
        // ---------------------------------------------------------------------------------------

        /// <summary>23252 变长 C2S(见类注释④):RuleId + 三组材料数组(IrregularGlist/RegularGlist/RatioGlist,
        /// 均为 goods_auto_id 列表)。</summary>
        public void RequestCompose(int ruleId, IReadOnlyList<long> irregularGoodsIds, IReadOnlyList<long> regularGoodsIds, IReadOnlyList<long> ratioGoodsIds)
        {
            var fmt = new StringBuilder("i");
            var args = new List<object> { ruleId };
            AppendVarLongArray(fmt, args, irregularGoodsIds);
            AppendVarLongArray(fmt, args, regularGoodsIds);
            AppendVarLongArray(fmt, args, ratioGoodsIds);
            SendFmt(Proto.STAREQUIP_COMPOSE, fmt.ToString(), args.ToArray());
        }

        /// <summary>23252 四出口统一处理(见类注释⑤),自带真实失败码,不经23200。</summary>
        private void On23252(NetReader r)
        {
            int code = r.ReadI32();
            int ruleId = r.ReadI32();
            List<StarEquipModel.ComposeRewardEntry> list = r.ReadArray(rr => new StarEquipModel.ComposeRewardEntry
            {
                GoodsId = rr.ReadU64(), GoodsTypeId = rr.ReadU32(),
            });
            if (code == 1 || code == 1500080) // ?SUCCESS 或 err150_compose_success
            {
                StarEquipModel.Instance.SetComposeSuccess(ruleId, list);
                EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_COMPOSE_SUCCESS, ruleId);
            }
            else if (code == 1500081) // err150_compose_fail
            {
                EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_COMPOSE_FAIL);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("StarEquip", "23252 合成 code={0} ruleId={1} rewardN={2}", code, ruleId, list.Count);
        }

        public void RequestUnlockPage(int constellationPage) => SendFmt(Proto.STAREQUIP_UNLOCK_PAGE, "i", constellationPage);

        private void On23253(NetReader r)
        {
            int page = r.ReadI32();
            int code = r.ReadI32(); // 末尾字段,本号自身实践中恒为成功,见 Proto.cs 注释
            if (code == 1)
            {
                StarEquipModel.Instance.MarkPageActive(page);
                RequestOverview(); // 老端 ts:398 成功后重发23201刷新总览
                EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_UNLOCK_PAGE_RESULT, page);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("StarEquip", "23253 解锁星宿页 page={0} code={1}", page, code);
        }

        public void RequestComposeTime(int composeId) => SendFmt(Proto.STAREQUIP_COMPOSE_TIME, "i", composeId);

        private void On23256(NetReader r)
        {
            var info = new StarEquipModel.ComposeTimeInfo { ComposeId = r.ReadI32(), Times = r.ReadU16(), Index = r.ReadU16(), Num = r.ReadU16() };
            StarEquipModel.Instance.SetComposeTime(info);
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_COMPOSE_TIME_UPDATE, info.ComposeId);
            GameLog.Info("StarEquip", "23256 合成次数信息(无Code) composeId={0} times={1} index={2} num={3}",
                info.ComposeId, info.Times, info.Index, info.Num);
        }

        public void RequestTransform(long costGoodsAutoId, long targetGoodsAutoId) =>
            SendFmt(Proto.STAREQUIP_TRANSFORM, "ll", costGoodsAutoId, targetGoodsAutoId);

        /// <summary>23257 单字段 Res,仅成功回本号(失败经23200)。老端 on23257 是空 if 块未接任何动作
        /// (`if(scmd.res==1){}`),本端仍补发事件供尾包消费,无副作用。</summary>
        private void On23257(NetReader r)
        {
            int res = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_STAREQUIP_TRANSFORM_RESULT, res == 1);
            GameLog.Info("StarEquip", "23257 蜕变执行(老端空if块,本端补发事件) res={0}", res);
        }
    }
}
