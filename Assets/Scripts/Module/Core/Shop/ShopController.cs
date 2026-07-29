using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 商店网络层(自动循环 轮11;对标老端 commonController/ShopController.ts:448-458 实注册清单)。
    /// 注册 15301/15302/15304/15305/15306/15307 + 64000/64001/64002/64003(15303 死号跳过,规格§0/Proto.cs 注释)。
    ///
    /// **命名陷阱按真义消费**(spec §0):15301 的 SoldOut=已购次数;15305 的 BuyType=购买状态;
    /// 15307 失败分支字段错位(第2/3字段与协议注释名错位)——本端与老端 Handler15307 一致,失败分支只读
    /// errcode 做提示,不使用错位的第2/3字段做业务判断(注释存档,不强行"修正"一个从未被消费的错位字段)。
    /// **TopVipShop(10) 劫持**:15301 收到该类型只落 ShopModel.TopVipShopGoodsList 专槽,不进主表、不转发
    /// TopVip 模块(商城商品UI尚无对应接收方)、不发 45102(该号是 TopVipController 自己的技能任务协议)。
    /// **64001 双编码体系**:errcode 0-7 走老端专用文案表,≥100000 走全局 ERRCODE 显码降级 toast。
    /// **64000 left_time**:客户端自算"下一个游戏日0点",使用服务器墙钟(SERVER_ZONE_HOURS=8,轮10 血训)。
    /// </summary>
    public sealed class ShopController : BaseController
    {
        public static readonly ShopController Instance = new ShopController();
        private ShopController() { }

        // 64001 errcode 0-7 自定义提示码文案表(对标老端 lib_rush_shop.erl:146-152 注释)。
        private static readonly string[] VieErrTexts =
            { "失败", "成功", "已下架", "金额不足", "达到限购", "售罄", "剩余不足", "未上架" };

        private int _lastLevel = -1;
        private int _lastVipFlag = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.SHOP_GOODS_LIST, On15301);
            RegisterProtocal(Proto.SHOP_BUY, On15302);
            RegisterProtocal(Proto.SHOP_QUICK_BUY, On15304);
            RegisterProtocal(Proto.SHOP_MYSTERY_LIST, On15305);
            RegisterProtocal(Proto.SHOP_MYSTERY_REFRESH, On15306);
            RegisterProtocal(Proto.SHOP_MYSTERY_BUY, On15307);
            // 15303:死号(RegisterProtocal 未注册,规格§0/Proto.cs 注释)。

            RegisterProtocal(Proto.SHOP_VIE_LIST, On64000);
            RegisterProtocal(Proto.SHOP_VIE_BUY, On64001);
            RegisterProtocal(Proto.SHOP_VIE_UPDATE, On64002);
            RegisterProtocal(Proto.SHOP_VIE_DELETE, On64003);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnServerHourRefresh);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnServerHourRefresh);
            _lastLevel = -1;
            _lastVipFlag = -1;
            ShopModel.Instance.Clear();
            ActivityIconManager.Instance.SetIconRedDot("153", false);
            base.Dispose();
        }

        // =====================================================================================
        // 触发时机(对标老端 GAME_START 13 种 shop_type 逐个 Fire 15301 + Fire 64000;
        // HOUR_REFRESH==4 点复拉见 OnServerHourRefresh;CHANGE_LEVEL==300/vip_flag 变化仍按等价的
        // EVT_ROLE_INFO_UPDATE 复请求,见 OnRoleInfoUpdate)
        // =====================================================================================

        /// <summary>整点刷新(对标老端 ShopController.ts:152-162,hour==4):先清缓存
        /// (老端 `model.SetVieInfo(null)` + `model.vie_red_stutus = null` 两行,本端合并为
        /// <see cref="ShopModel.ClearVieInfo"/>——本端 SetVieInfo 入口会对 vo.IdList 排序,吃不了 null),
        /// 再发 64000 + 15301×3(EudaemonShop/SacredShop/SoulOfWar)。
        /// **清缓存必须在发包之前**(副作用顺序对齐老端):清空后 CheckVieOpen() 转 false,抢购入口暂时收起,
        /// 由随后到达的 64000 回包重新填充并重判红点,与老端一致。</summary>
        private void OnServerHourRefresh(int hour)
        {
            if (hour != 4) return;
            ShopModel.Instance.ClearVieInfo();
            RequestVieList();
            RequestShopType(ShopModel.TYPE_EUDAEMON_SHOP);
            RequestShopType(ShopModel.TYPE_SACRED_SHOP);
            RequestShopType(ShopModel.TYPE_SOUL_OF_WAR);
            GameLog.Info("Shop", "HOUR_REFRESH==4 清抢购红点缓存 + 复请求 64000 + 15301×3(圣兽领/领地/战魂)");
        }

        private async void OnGameStart()
        {
            ShopModel.Instance.Clear();
            await ShopConfigs.EnsureLoaded();
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_LIMIT);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_DIAMOND);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_BIND_DIAMOND);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_OUTWARD);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_NORMAL_SHOP);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_EUDAEMON_SHOP);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_LUCKY);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_LONGLANG_EX);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_SOUL_OF_WAR);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_GOD_COURT);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_TOPVIP_SHOP);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_GUILD);
            SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_MEDAL_SHOP);
            SendFmt(Proto.SHOP_VIE_LIST); // 64000 无参
            _lastLevel = RoleModel.Instance.Level;
            _lastVipFlag = GetVipFlag();
            GameLog.Info("Shop", "GAME_START 批量请求 15301×13 类型 + 64000");
        }

        /// <summary>对标老端 jin BindOne(钻石红点复判)+ CHANGE_LEVEL==300(战魂)+ vip_flag 变化(幸运)复请求。</summary>
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;

            ShopModel.Instance.RecomputeDiamondRed();
            EmitAggregatedRedDot();

            if (role.Level != _lastLevel)
            {
                bool crossed300 = role.Level >= 300 && _lastLevel < 300;
                _lastLevel = role.Level;
                if (crossed300) SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_SOUL_OF_WAR);
            }

            int vipFlag = GetVipFlag();
            if (vipFlag != _lastVipFlag)
            {
                _lastVipFlag = vipFlag;
                SendFmt(Proto.SHOP_GOODS_LIST, "c", ShopModel.TYPE_LUCKY);
            }
        }

        private static int GetVipFlag()
        {
            Shenxiao.Common.Proto.FigureProto fig = RoleModel.Instance.Figure;
            if (fig == null) return 0;
            return fig.Raw.TryGetValue("vip_flag", out object v) ? Convert.ToInt32(v) : 0;
        }

        /// <summary>聚合红点信号(钻石够钱未售罄 || 抢购有可买 || 神秘商店首次全新未购),对标老端
        /// REFRESH_ACTIVITY_ICON_RED_DOT,153 三路合流;像素级挂到活动图标 153 留 TODO(见 GlobalEvent 注释)。</summary>
        private static void EmitAggregatedRedDot()
        {
            bool on = ShopModel.Instance.DiamondRedStatus
                || (ShopModel.Instance.VieRedStatus ?? false)
                || ShopModel.Instance.MysteryFirstAllNewRed;
            EventDispatcher.Emit(GlobalEvent.EVT_SHOP_RED_DOT, on);
            ActivityIconManager.Instance.SetIconRedDot("153", on);
        }

        // =====================================================================================
        // 15301:常规商城列表(公开发送 API,ShopFlow 切标签/ShopCommonView 开页调)
        // =====================================================================================

        /// <summary>请求某 shop_type 商品列表(对标老端每次打开/切到该类型都重发,r11_unity 建议的
        /// "共享内容架构下按需重拉"策略——本家族无 CD 表,重发无害)。</summary>
        public void RequestShopType(int shopType) => SendFmt(Proto.SHOP_GOODS_LIST, "c", shopType);

        private void On15301(NetReader r)
        {
            int type = r.ReadU8();
            List<ShopModel.GoodsVo> list = r.ReadArray(ReadGoodsVo);

            if (type == ShopModel.TYPE_TOPVIP_SHOP)
            {
                ShopModel.Instance.SetShopData(type, list);
                GameLog.Info("Shop", "15301 type=TopVipShop(10) count={0} → 落 TopVipShopGoodsList 专槽" +
                    "(商城商品UI尚无接收方,不转发/不伪造45102任务包)", list.Count);
                return;
            }

            ShopModel.Instance.SetShopData(type, list);
            if (type == ShopModel.TYPE_DIAMOND)
            {
                ShopModel.Instance.CaptureDiamondSpecial(list);
                EmitAggregatedRedDot();
            }
            EventDispatcher.Emit(GlobalEvent.EVT_SHOP_DATA_UPDATE, type);
            GameLog.Info("Shop", "15301 type={0} count={1}", type, list.Count);
        }

        private static ShopModel.GoodsVo ReadGoodsVo(NetReader r)
        {
            return new ShopModel.GoodsVo
            {
                KeyId = (int)r.ReadU32(),
                SubtypeList = r.ReadString(),
                Rank = (int)r.ReadU32(),
                GoodsId = (int)r.ReadU32(),
                Num = (int)r.ReadU32(),
                MoneyType = (int)r.ReadU32(),
                Price = (int)r.ReadU32(),
                Discount = r.ReadU16(),
                QuotaType = r.ReadU8(),
                QuotaNum = r.ReadU16(),
                SoldOut = r.ReadU16(), // ⚠真实语义=已购次数(UsedTime),非售罄布尔
                Condition = r.ReadString(),
                TriggerTaskId = (int)r.ReadU32(),
                Bind = r.ReadU8(),
            };
        }

        // =====================================================================================
        // 15302:购买商品(全商城通用购买入口)
        // =====================================================================================

        /// <summary>num 固定传1(批量购买走 ShopBulkPurchaseView,现为死枝未接线,规格§0/r11_unity 结论;
        /// 本端购买按钮直接以1个为单位下单,简化裁决见汇报偏差栏)。</summary>
        public void BuyGoods(int keyId, int num = 1) => SendFmt(Proto.SHOP_BUY, "ii", keyId, num);

        private void On15302(NetReader r)
        {
            int result = (int)r.ReadU32();
            int keyId = (int)r.ReadU32();
            int num = (int)r.ReadU32();
            if (result == 1)
            {
                TipsManager.Toast("购买成功");
                bool lifelong = ShopModel.Instance.UpdateShopData(keyId, num);
                EventDispatcher.Emit(GlobalEvent.EVT_SHOP_ONE_UPDATE, keyId);
                if (lifelong)
                {
                    ShopModel.GoodsVo vo = ShopModel.Instance.GetShopDataByKeyId(keyId);
                    if (vo != null) EventDispatcher.Emit(GlobalEvent.EVT_SHOP_DATA_UPDATE, vo.ShopType);
                }
                EventDispatcher.Emit(GlobalEvent.EVT_SHOP_BUY_SUCCESS, keyId);
                GameLog.Info("Shop", "15302 购买成功 key_id={0} num={1} lifelongResort={2}", keyId, num, lifelong);
            }
            else if (result == 1040)
            {
                ShopModel.GoodsVo vo = ShopModel.Instance.GetShopDataByKeyId(keyId);
                if (vo != null && vo.MoneyType == ShopModel.MONEY_TOPVIP)
                    TipsManager.Toast("至尊币不足,完成至尊币任务可获取至尊币");
                else
                    TipsManager.Toast("购买失败(" + result + ")");
                GameLog.Warn("Shop", "15302 购买失败(1040) key_id={0}", keyId);
            }
            else
            {
                // 对标老端 result==1001 额外弹 NotEnougnDiamond()专属钻石不足弹窗——未移植,降级同通用 toast。
                TipsManager.Toast("购买失败(" + result + ")");
                GameLog.Warn("Shop", "15302 购买失败 key_id={0} code={1}", keyId, result);
            }
        }

        // =====================================================================================
        // 15304:快速购买(QuickBuyView 专供,UI 未接壳,仅留 API)
        // =====================================================================================

        public void QuickBuy(int goodsId, int num, int buyType) => SendFmt(Proto.SHOP_QUICK_BUY, "iic", goodsId, num, buyType);

        private void On15304(NetReader r)
        {
            int res = (int)r.ReadU32();
            int goodsId = (int)r.ReadU32();
            int num = (int)r.ReadU32();
            int buyType = r.ReadU8();
            if (res == 1)
            {
                TipsManager.Toast("购买成功");
                // 对标老端 Fire(BUY_GOODS_SUCCESS)不带 key_id——本端用 0 作哨兵(始终非法 key_id,不会误命中任何 Item)。
                EventDispatcher.Emit(GlobalEvent.EVT_SHOP_BUY_SUCCESS, 0);
                GameLog.Info("Shop", "15304 快速购买成功 goods_id={0} num={1} buy_type={2}", goodsId, num, buyType);
            }
            else
            {
                TipsManager.Toast("购买失败(" + res + ")");
                GameLog.Warn("Shop", "15304 快速购买失败 goods_id={0} code={1}", goodsId, res);
            }
        }

        // =====================================================================================
        // 15305/15306/15307:神秘/神纹商店
        // =====================================================================================

        public void RequestMysteryShop(int type) => SendFmt(Proto.SHOP_MYSTERY_LIST, "h", type);
        public void RefreshMysteryShop(int type) => SendFmt(Proto.SHOP_MYSTERY_REFRESH, "h", type);
        public void BuyMysteryGoods(int type, int cfgId, int price) => SendFmt(Proto.SHOP_MYSTERY_BUY, "hhi", type, cfgId, price);

        private void On15305(NetReader r)
        {
            var vo = new ShopModel.MysteryShopVo
            {
                Type = r.ReadU16(),
                RefreshTime = r.ReadU32(),
                HitNum = r.ReadU16(),
            };
            vo.GoodList = r.ReadArray(rr => new ShopModel.MysteryGoodVo
            {
                CfgId = rr.ReadU16(),
                Discount = rr.ReadU8(),
                Price = (int)rr.ReadU32(),
                BuyType = rr.ReadU8(), // ⚠真实语义=购买状态 1未买/2已买
                BuyNum = rr.ReadU8(),
            });
            bool hitChanged = ShopModel.Instance.SetMysteryShop(vo);
            EventDispatcher.Emit(GlobalEvent.EVT_SHOP_MYSTERY_UPDATE, vo.Type);
            if (hitChanged) EventDispatcher.Emit(GlobalEvent.EVT_SHOP_MYSTERY_REFRESH_EFFECT, vo.Type);
            if (ShopModel.Instance.MysteryFirstAllNewRed)
            {
                EmitAggregatedRedDot();
                GameLog.Info("Shop", "15305 全新未购(type={0}) → 活动图标153红点(像素级挂接TODO)", vo.Type);
            }
            GameLog.Info("Shop", "15305 type={0} refresh_time={1} hit_num={2} count={3} hitChanged={4}",
                vo.Type, vo.RefreshTime, vo.HitNum, vo.GoodList.Count, hitChanged);
        }

        private void On15306(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            if (errcode == 1)
            {
                TipsManager.Toast("刷新成功");
                GameLog.Info("Shop", "15306 手动刷新成功(服务端自动补推15305,本端不重拉)");
            }
            else
            {
                TipsManager.Toast("刷新失败(" + errcode + ")");
                GameLog.Warn("Shop", "15306 手动刷新失败 code={0}", errcode);
            }
        }

        private void On15307(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            // ⚠r11_server 证实:失败分支实参是 [Errcode,Id,0](Id顶替Type位/CfgId位恒0)。老端 Handler15307
            // 失败分支只调 Util.ErrorCodeShow(errcode),从不读这两个字段;本端同样只在 errcode==1 分支把它们
            // 当作 Type/CfgId 使用,失败分支仅消耗游标位宽、不作为业务字段解读(注释存档,不臆造修正)。
            int field2 = r.ReadU16();
            int field3 = r.ReadU16();
            if (errcode == 1)
            {
                int type = field2;
                int cfgId = field3;
                if (type != ShopModel.MYSTERY_LUNG) TipsManager.Toast("购买成功"); // 神纹商店自己的飞奖励文案不走这条toast
                EventDispatcher.Emit(GlobalEvent.EVT_SHOP_MYSTERY_BUY_SUCCESS, cfgId);
                ShopModel.Instance.UpdateMysteryShop(type, cfgId);
                GameLog.Info("Shop", "15307 购买成功 type={0} cfg_id={1}", type, cfgId);
            }
            else
            {
                TipsManager.Toast("购买失败(" + errcode + ")");
                GameLog.Warn("Shop", "15307 购买失败 code={0}(第2/3字段错位,按老端行为不消费:field2={1} field3={2})",
                    errcode, field2, field3);
            }
        }

        // =====================================================================================
        // 64000-64003:抢购(限购)商城
        // =====================================================================================

        /// <summary>send:null 但老端仍手动裸发无参帧拉取(GAME_START/每日4点/开抢购tab),照抄。</summary>
        public void RequestVieList() => SendFmt(Proto.SHOP_VIE_LIST);
        public void BuyVieGoods(int id, int num = 1) => SendFmt(Proto.SHOP_VIE_BUY, "ii", id, num);

        private void On64000(NetReader r)
        {
            var vo = new ShopModel.VieInfoVo
            {
                IdList = r.ReadArray(rr => new ShopModel.VieGoodVo
                {
                    Id = (int)rr.ReadU32(),
                    GoodId = (int)rr.ReadU32(),
                    DefaultNum = (int)rr.ReadU32(),
                    PriceType = rr.ReadU8(),
                    OldPrice = (int)rr.ReadU32(),
                    NewPrice = (int)rr.ReadU32(),
                    TotalLimitNum = (int)rr.ReadU32(),
                    LeftLimitNum = (int)rr.ReadU32(),
                    DailyLimitNum = (int)rr.ReadU32(),
                    BuyNum = (int)rr.ReadU32(),
                }),
                // ⚠协议表无此字段——客户端自算"下一个游戏日0点",必须用服务器墙钟(SERVER_ZONE_HOURS),
                // 不能裸 UTC/裸 DateTime.Now(轮10 血训)。
                LeftTimeMs = ComputeNextDayZeroStampMs(),
            };
            ShopModel.Instance.SetVieInfo(vo);
            EventDispatcher.Emit(GlobalEvent.EVT_SHOP_VIE_UPDATE);
            EmitAggregatedRedDot();
            GameLog.Info("Shop", "64000 抢购列表 count={0} left_time_ms={1}", vo.IdList.Count, vo.LeftTimeMs);
        }

        /// <summary>服务器墙钟"下一个游戏日0点"真实 unix 毫秒(对标老端 TimeUtil.GetFutureZeroStamp(1),
        /// 但用 UTC+SERVER_ZONE_HOURS 代替裸本地时区/裸UTC——同 DailyModel.TimeUtilNowUtc 先例)。</summary>
        private static long ComputeNextDayZeroStampMs()
        {
            DateTime zoneNow = TimeUtil.NowUtc().AddHours(ShopModel.SERVER_ZONE_HOURS);
            DateTime zoneMidnightNext = zoneNow.Date.AddDays(1);
            DateTime trueUtc = zoneMidnightNext.AddHours(-ShopModel.SERVER_ZONE_HOURS);
            return (long)(trueUtc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        private void On64001(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int id = (int)r.ReadU32();
            int buyNum = (int)r.ReadU32();
            int leftLimitNum = (int)r.ReadU32();

            if (errcode == 1)
            {
                ShopModel.Instance.PatchVieBuy(id, buyNum, leftLimitNum);
                EventDispatcher.Emit(GlobalEvent.EVT_SHOP_VIE_BUY_SUCCESS, id);
                EventDispatcher.Emit(GlobalEvent.EVT_SHOP_VIE_UPDATE);
                TipsManager.Toast("购买成功");
                GameLog.Info("Shop", "64001 抢购购买成功 id={0} buy_num={1} left_limit_num={2}", id, buyNum, leftLimitNum);
                return;
            }

            // ⚠双编码体系:0-7 是老端自定义提示码,≥100000 是全局 ERRCODE 大数值(守卫失败,如
            // err640_goods_not_on_sale=6400000)。按量级分流,不能用同一张小表查大数值。
            string msg = errcode >= 0 && errcode < VieErrTexts.Length
                ? VieErrTexts[errcode]
                : (errcode >= 100000 ? "操作失败(" + errcode + ")" : "购买失败(" + errcode + ")");
            TipsManager.Toast(msg);
            GameLog.Warn("Shop", "64001 抢购购买失败 id={0} code={1} msg={2}", id, errcode, msg);
        }

        private void On64002(NetReader r)
        {
            List<(int id, int leftLimitNum)> changes = r.ReadArray(rr => ((int)rr.ReadU32(), (int)rr.ReadU32()));
            ShopModel.Instance.ApplyVieChangeList(changes);
            EventDispatcher.Emit(GlobalEvent.EVT_SHOP_VIE_UPDATE);
            GameLog.Info("Shop", "64002 抢购库存广播 count={0}", changes.Count);
        }

        private void On64003(NetReader r)
        {
            List<int> ids = r.ReadArray(rr => (int)rr.ReadU32());
            // ⚠老端 Array.slice 假删除 bug——本端按显然意图真删(同轮10 rule10 先例)。
            ShopModel.Instance.RemoveVieIds(ids);
            EventDispatcher.Emit(GlobalEvent.EVT_SHOP_VIE_UPDATE);
            GameLog.Info("Shop", "64003 抢购下架广播 count={0}(真删,订正老端假删bug)", ids.Count);
        }
    }
}
