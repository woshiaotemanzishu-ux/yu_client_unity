using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Market
{
    /// <summary>
    /// 市场(交易行)控制器(对标老客户端 commonController/MarketController.ts,343行,模块 151)。
    /// 进游戏请求 15121 拿跨服市场开放时间 open_time;回包据 open_time 与当前服务器时间在两个主界面图标间
    /// 切换:未到开放时间显示本服市场 151(删 151@1),已到显示跨服市场 151@1(删 151)——对标老端 on15121→showIcon。
    /// 图标是否真正显示仍过图标配置门(open_lv,AddIconAsync 与老端 addIcon 一致)。等级变化(EVT_ROLE_INFO_UPDATE)
    /// 复请求 15121(对标老端 CHANGE_LEVEL→发 15121),让升级到市场开启后图标及时出现。以上 15121 逻辑
    /// 自动循环 轮19 一行未删。
    /// 自动循环 轮19 扩展:补 15100/15101/15102/15106/15108/15109/15111/15112/15114/15115-15119/15120/
    /// 15122(老端活号 17 个中除已接的 15121 外的其余 16 个)。死号 15103(协议定义缺失)/15104(老端零调用,
    /// 搜索功能老端自砍)/15105(老端零调用,推荐价老端自砍,服务端本身该错误分支还在产 bug)/15107(P2P上架,
    /// do_handle 整段注释)/15110(P2P列表,注释+write缺)/15113(P2P红点,触发链依赖已注释的15107)——
    /// 六个死号严禁注册/严禁发送,详见 Proto.cs 对应段落注释与 r18_server_market.md 台账。**服务端统一
    /// open_lv=90 门槛(pp_sell.erl:22-29)**,90级以下所有 151 号请求服务端静默丢包不回,发送侧不可等待。
    /// 所有玩法面板(24个 view 文件)仍不移植,留 UI 尾包;本轮纯数据层,对标 MarketModel.ts(639行)同名
    /// 方法增删改查语义,行为差异带 TS 原文行号,见各 On1511x 方法注释。
    /// </summary>
    public sealed class MarketController : BaseController
    {
        public static readonly MarketController Instance = new MarketController();
        private MarketController() { }

        public const string ICON_TYPE_LOCAL = MarketModel.ICON_TYPE_LOCAL;
        public const string ICON_TYPE_KF = MarketModel.ICON_TYPE_KF;

        // 复请求 15121 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.MARKET_ICON_INFO, On15121);
            // 对标老端 CHANGE_LEVEL→发 15121:等级变化时复请求(市场按 151 图标配置 open_lv 开启)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);

            // 自动循环 轮19 扩展:15100-15120/15122(除死号)。死号 15103/15104/15105/15107/15110/15113
            // 严禁在此注册——15104/15105 老端注册了空壳 handler(ts:150-156,on15104/on15105 仅
            // GetSCMD(cmd) 取包,零行为),15103/15107/15110/15113 老端 RegisterProtocal 列表里本就
            // 没有这 4 个(ts:309-327)。
            RegisterProtocal(Proto.MARKET_ERROR_PUSH, On15100);
            RegisterProtocal(Proto.MARKET_LEVEL1_LIST, On15101);
            RegisterProtocal(Proto.MARKET_GOODS_LIST, On15102);
            RegisterProtocal(Proto.MARKET_SELL_UP, On15106);
            RegisterProtocal(Proto.MARKET_SELL_DOWN, On15108);
            RegisterProtocal(Proto.MARKET_SHELF_LIST, On15109);
            RegisterProtocal(Proto.MARKET_BUY, On15111);
            RegisterProtocal(Proto.MARKET_RECORD_LIST, On15112);
            RegisterProtocal(Proto.MARKET_BUY_TIMES, On15114);
            RegisterProtocal(Proto.MARKET_PLZ_CREATE, On15115);
            RegisterProtocal(Proto.MARKET_PLZ_CANCEL, On15116);
            RegisterProtocal(Proto.MARKET_PLZ_SELL, On15117);
            RegisterProtocal(Proto.MARKET_PLZ_LIST_ALL, On15118);
            RegisterProtocal(Proto.MARKET_PLZ_LIST_MINE, On15119);
            RegisterProtocal(Proto.MARKET_SELL_DELETE_PUSH, On15120);
            RegisterProtocal(Proto.MARKET_SHOUT, On15122);
        }

        // 老端本地错误码显示降级(错误码表未移植,同 FestivalController/WelfareController 先例:显码)。
        private static void ShowError(int code) => TipsManager.Toast("错误(" + code + ")");

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 两个图标都删(对标老端 showIcon 只留一个,断线时两者都清)。
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_LOCAL);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE_KF);
            MarketModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 15121)。</summary>
        public void RequestStartup()
        {
            // read(15121,_)->{ok,[]}:请求无字段,裸发。
            SendFmt(Proto.MARKET_ICON_INFO);
        }

        // 15121: open_time:i(跨服市场开放时间戳;write(15121,[OpenTime]) -> <<OpenTime:32>>)。
        private void On15121(NetReader r)
        {
            int openTime = (int)r.ReadU32();
            // 对标老端 on15121:kf_open = open_time>0 && serverTime>open_time
            //(TimeUtil.NowSec 为 10201 同步后的服务器时间秒)。
            bool kfOpen = openTime > 0 && TimeUtil.NowSec() > openTime;
            MarketModel.Instance.SetKfOpen(openTime, kfOpen);
            RefreshIcon();

            GameLog.Info("Market", "15121 市场: open_time={0} kf_open={1} show={2}",
                openTime, kfOpen, MarketModel.Instance.GetShowIconType());
        }

        // 对标老端 showIcon:先删另一图标,再加当前图标(AddIconAsync 过图标配置门 open_lv,与老端 addIcon 一致)。
        private void RefreshIcon()
        {
            MarketModel m = MarketModel.Instance;
            ActivityIconManager.Instance.DeleteIcon(m.GetHideIconType());
            _ = ActivityIconManager.Instance.AddIconAsync(m.GetShowIconType());
        }

        // 对标老端:主角等级变化复请求 15121(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        // ======================================================================================
        // 自动循环 轮19 扩展:15100-15120/15122 发送封装 + 协议处理(死号 15103/15104/15105/15107/
        // 15110/15113 不接)。服务端统一 open_lv=90 门槛静默丢包(pp_sell.erl:22-29),以下所有 Request
        // 方法在 90 级以下发出后不会收到任何回包,发送侧不可阻塞等待。
        // ======================================================================================

        // ---- 15100: 通用错误码推送(S2C only) ----

        // 15100: Errcode:32, Args:string(pt_151.erl:81-91)。对标老端 on15100(ts:122-125):无条件调用
        // ErrorCodeShow(errcode,args),不判断 errcode==1(与本模块其余号"仅失败才显码"不同,按原文逐字镜像)。
        private void On15100(NetReader r)
        {
            int code = (int)r.ReadU32();
            string args = r.ReadString();
            ShowError(code);
            GameLog.Info("Market", "15100 错误推送: code={0} args={1}", code, args);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_UPDATE, Proto.MARKET_ERROR_PUSH);
        }

        // ---- 15101: 一级分类挂单数量 ----

        /// <summary>15101 一级分类挂单数量(发 "i" Type;老端 MarketBuyListView.ts:66)。</summary>
        public void RequestLevel1List(int type) => SendFmt(Proto.MARKET_LEVEL1_LIST, "i", type);

        // 15101: Type:32, SellList[u16×{Subtype:32,SellNum:32}](pt_151.erl:93-108)。对标老端 on15101
        // (ts:127-134)。
        private void On15101(NetReader r)
        {
            int type = r.ReadI32();
            List<MarketModel.SellCountEntry> list = r.ReadArray(rr => new MarketModel.SellCountEntry
            {
                Subtype = rr.ReadI32(),
                SellNum = rr.ReadI32(),
            });
            MarketModel.Instance.SetGoodsDic(type, list);
            GameLog.Info("Market", "15101 一级分类挂单数量: type={0} listN={1}", type, list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_UPDATE, Proto.MARKET_LEVEL1_LIST);
        }

        // ---- 15102: 二级列表商品(9字段,EquipExtraAttr 二层嵌套) ----

        /// <summary>15102 二级列表商品(发 "iiccc" Type,Subtype,Stage,Star,Color;99=不筛选,老端
        /// MarketBuyCtrlView.ts:187/191/288)。</summary>
        public void RequestGoodsList(int type, int subtype, int stage, int star, int color)
            => SendFmt(Proto.MARKET_GOODS_LIST, "iiccc", type, subtype, stage, star, color);

        // 15102: Type:32,Subtype:32,GoodsList[u16×9字段(见 MarketModel.GoodsEntry,EquipExtraAttr 二层
        // 嵌套)](pt_151.erl:110-127,item_to_bin_1/2)。对标老端 on15102(ts:136-148):落 Model 前按 id
        // 升序排序(镜像 ts:143-146 table.sort(scmd.goods_list,(a,b)=>b.id>a.id),即 a.id 更小排前面);
        // Model 缓存写序非纯展示——UI 尾包直接消费该顺序,故排序动作在数据层做,不留给消费侧。
        private void On15102(NetReader r)
        {
            int type = r.ReadI32();
            int subtype = r.ReadI32();
            List<MarketModel.GoodsEntry> list = ReadGoodsList(r);
            list.Sort((a, b) => a.Id.CompareTo(b.Id));
            MarketModel.Instance.SetSellGoodsInfo(type, subtype, list);
            GameLog.Info("Market", "15102 二级列表商品: type={0} subtype={1} listN={2}", type, subtype, list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_UPDATE, Proto.MARKET_GOODS_LIST);
        }

        // ---- 15106: 上架 ----

        /// <summary>15106 上架(发 "liic" GoodsId,GoodsNum,Price,IsShout;老端 MarketSellCtrlView.ts:206)。</summary>
        public void RequestSellUp(long goodsId, int goodsNum, int price, int isShout)
            => SendFmt(Proto.MARKET_SELL_UP, "liic", goodsId, goodsNum, price, isShout);

        // 15106: Errcode:32(pt_151.erl:156-162)。对标老端 on15106(ts:158-171):成功后重发 15109 刷新
        // 上架列表(ts:163);errcode==1500001 老端 Fire(NEED_UPDATE_BAG),MarketSellView.ts:90 绑定,
        // 回调 SwitchView(cur_index_) 重刷当前背包页签列表(ts:139-154),Unity 无对应 UI 通道,不移植,
        // 仅注释存档。
        private void On15106(NetReader r)
        {
            int code = (int)r.ReadU32();
            if (code == 1)
            {
                TipsManager.Toast("上架成功"); // 对标老端 on15106(ts:161) local_Message.show
                SendFmt(Proto.MARKET_SHELF_LIST);
            }
            else ShowError(code);
            GameLog.Info("Market", "15106 上架结果: code={0}", code);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_RESULT, Proto.MARKET_SELL_UP, code);
        }

        // ---- 15108: 下架 ----

        /// <summary>15108 下架(发 "clii" SellType,Id,TypeId,GoodsNum;SellType 老端恒传1,P2P(2)已死,
        /// 老端 MarketShowItem.ts:169)。</summary>
        public void RequestSellDown(int sellType, long id, int typeId, int goodsNum)
            => SendFmt(Proto.MARKET_SELL_DOWN, "clii", sellType, id, typeId, goodsNum);

        // 15108: Errcode:32(pt_151.erl:164-170)。对标老端 on15108(ts:173-182):成功后重发 15109 刷新
        // (ts:177)。
        private void On15108(NetReader r)
        {
            int code = (int)r.ReadU32();
            if (code == 1)
            {
                TipsManager.Toast("下架成功"); // 对标老端 on15108(ts:176) local_Message.show
                SendFmt(Proto.MARKET_SHELF_LIST);
            }
            else ShowError(code);
            GameLog.Info("Market", "15108 下架结果: code={0}", code);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_RESULT, Proto.MARKET_SELL_DOWN, code);
        }

        // ---- 15109: 我的上架列表 ----

        /// <summary>15109 我的上架列表(裸;老端 MarketSellView.ts:82,15106/15108成功后亦回补)。</summary>
        public void RequestShelfList() => SendFmt(Proto.MARKET_SHELF_LIST);

        // 15109: GoodsList[u16×9字段](pt_151.erl:172-185,item_to_bin_5,同 15102 形状)。对标老端 on15109
        // (ts:184-192):老端额外把每条推入 GoodsModel.SetBaseInfo 供物品提示框复用,Unity 侧无该运行时
        // 物品实例缓存(Common/GoodsModel.cs 是静态配置查表,与老端按 id 缓存活体物品实例的 GoodsModel
        // 不同源),TODO 留待该 UI 落地后再补对应缓存通道。
        private void On15109(NetReader r)
        {
            List<MarketModel.GoodsEntry> list = ReadGoodsList(r);
            MarketModel.Instance.SetShelfGoodsInfo(list);
            GameLog.Info("Market", "15109 我的上架列表: listN={0}", list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_UPDATE, Proto.MARKET_SHELF_LIST);
        }

        // ---- 15111: 购买 ----

        /// <summary>15111 购买(发 "cliiliii" SellType,Id,Type,Subtype,SellerId,TypeId,GoodsNum,UnitPrice;
        /// SellType 老端恒传1,老端 MarketShowItem.ts:157)。</summary>
        public void RequestBuy(int sellType, long id, int type, int subtype, long sellerId, int typeId, int goodsNum, int unitPrice)
            => SendFmt(Proto.MARKET_BUY, "cliiliii", sellType, id, type, subtype, sellerId, typeId, goodsNum, unitPrice);

        // 15111: Errcode:32,SellType:8,Id:64,Type:32,Subtype:32(pt_151.erl:187-201,5字段回执,
        // 无 write_string)。对标老端 on15111(ts:194-209):成功移除对应挂单缓存并重发 15114 刷新购买次数
        // (ts:199);errcode==1510006=err151_goods_not_exist 商品不存在(data_error_code.erl:1629-1630;
        // 老端UI语义"已被买走")同样移除挂单缓存;errcode==1510014(已下架)移除本地上架缓存(老端
        // RemoveShelfGoodInfo 只用 scmd.id,与本端一致)。
        // 存档:老端 on15102 落地时给每条 goods_list 元素注入 info.type/info.subtype(ts:138-142),
        // 供 MarketShowItem.ts:157 发 15111 时直接取 data.type/data.subtype;本端 GoodsEntry 无此二字段,
        // 由调用方(UI 层)显式传参构造 RequestBuy 参数,行为等价,差异存档不补字段。
        private void On15111(NetReader r)
        {
            int code = (int)r.ReadU32();
            int sellType = r.ReadU8();
            long id = r.ReadU64();
            int type = r.ReadI32();
            int subtype = r.ReadI32();
            if (code == 1)
            {
                TipsManager.Toast("购买成功!请前往邮箱查收!"); // 对标老端 on15111(ts:197) local_Message.show
                MarketModel.Instance.RemoveSellGoodInfo(type, subtype, id);
                SendFmt(Proto.MARKET_BUY_TIMES);
            }
            else
            {
                ShowError(code);
                if (code == 1510006) MarketModel.Instance.RemoveSellGoodInfo(type, subtype, id);
                else if (code == 1510014) MarketModel.Instance.RemoveShelfGoodInfo(id);
            }
            GameLog.Info("Market", "15111 购买结果: code={0} sellType={1} id={2} type={3} subtype={4}",
                code, sellType, id, type, subtype);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_RESULT, Proto.MARKET_BUY, code);
        }

        // ---- 15112: 交易记录 ----

        /// <summary>15112 交易记录(裸;老端 MarketRecordDealView.ts:50/56)。</summary>
        public void RequestRecordList() => SendFmt(Proto.MARKET_RECORD_LIST);

        // 15112: RecordList[u16×9字段](见 MarketModel.RecordEntry,pt_151.erl:203-216,item_to_bin_7/8)。
        // 对标老端 on15112(ts:211-218):老端在此额外按 time 做展示排序(table.sort),纯 UI 呈现顺序,
        // 本轮面板不移植,原始顺序落 Model,留 UI 尾包按需排序。
        private void On15112(NetReader r)
        {
            List<MarketModel.RecordEntry> list = ReadRecordList(r);
            MarketModel.Instance.SetRecordList(list);
            GameLog.Info("Market", "15112 交易记录: listN={0}", list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_UPDATE, Proto.MARKET_RECORD_LIST);
        }

        // ---- 15114: 购买次数 ----

        /// <summary>15114 购买次数(裸;老端 MarketBuyCtrlView.ts:171,15111 成功后亦回补)。</summary>
        public void RequestBuyTimes() => SendFmt(Proto.MARKET_BUY_TIMES);

        // 15114: TimesList[u16×{Type:8,Times:8,TimesLimit:8}](pt_151.erl:218-231,item_to_bin_9)。
        // 对标老端 on15114(ts:220-223)。
        private void On15114(NetReader r)
        {
            List<MarketModel.BuyTimesEntry> list = r.ReadArray(rr => new MarketModel.BuyTimesEntry
            {
                Type = rr.ReadU8(),
                Times = rr.ReadU8(),
                TimesLimit = rr.ReadU8(),
            });
            MarketModel.Instance.SetBuyTimesList(list);
            GameLog.Info("Market", "15114 购买次数: listN={0}", list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_UPDATE, Proto.MARKET_BUY_TIMES);
        }

        // ---- 15115: 发起求购 ----

        /// <summary>15115 发起求购(发 "iii" TypeId,GoodsNum,UnitPrice;老端 MarketPlzCtrlView.ts:239)。</summary>
        public void RequestCreatePlz(int typeId, int goodsNum, int unitPrice)
            => SendFmt(Proto.MARKET_PLZ_CREATE, "iii", typeId, goodsNum, unitPrice);

        // 15115: Errcode:32,Id:64,PlayerId:64,RoleName:string,TypeId:32,GoodsNum:16(与 read 侧 32 位不同,
        // pt_151.erl:233-255 write 子句原文核实),UnitPrice:32,Time:32。对标老端 on15115(ts:225-234):
        // 成功后把回执头插进 seek_info_all 与 seek_info_mine 两个求购列表缓存(均要求已先拉取过,
        // MarketModel.ts:464-480 AddlzGoodInfoAll/Mine 对 null 直接 return 的守卫已在 Model 层镜像)。
        private void On15115(NetReader r)
        {
            int code = (int)r.ReadU32();
            long id = r.ReadU64();
            long playerId = r.ReadU64();
            string roleName = r.ReadString();
            int typeId = r.ReadI32();
            int goodsNum = r.ReadU16();
            int unitPrice = r.ReadI32();
            int time = r.ReadI32();
            if (code == 1)
            {
                TipsManager.Toast("发起求购成功"); // 对标老端 on15115(ts:228) local_Message.show
                var entry = new MarketModel.SeekEntry
                {
                    Id = id,
                    PlayerId = playerId,
                    RoleName = roleName,
                    TypeId = typeId,
                    GoodsNum = goodsNum,
                    UnitPrice = unitPrice,
                    Time = time,
                };
                MarketModel.Instance.AddPlzGoodInfoAll(entry);
                MarketModel.Instance.AddPlzGoodInfoMine(entry);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Market", "15115 发起求购结果: code={0} id={1} typeId={2}", code, id, typeId);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_RESULT, Proto.MARKET_PLZ_CREATE, code);
        }

        // ---- 15116: 撤销求购 ----

        /// <summary>15116 撤销求购(发 "l" Id;老端 MarketPlzShowItem.ts:82)。</summary>
        public void RequestCancelPlz(long id) => SendFmt(Proto.MARKET_PLZ_CANCEL, "l", id);

        // 15116: Errcode:32,Id:64(pt_151.erl:257-265)。对标老端 on15116(ts:236-249):成功或
        // errcode==1510023(求购单已不存在)均从 seek_info_all/mine 摘除该条。
        private void On15116(NetReader r)
        {
            int code = (int)r.ReadU32();
            long id = r.ReadU64();
            if (code == 1)
            {
                TipsManager.Toast("撤销求购成功"); // 对标老端 on15116(ts:239) local_Message.show
                MarketModel.Instance.RemovePlzGoodInfoAll(id);
                MarketModel.Instance.RemovePlzGoodInfoMine(id);
            }
            else
            {
                ShowError(code);
                if (code == 1510023)
                {
                    MarketModel.Instance.RemovePlzGoodInfoAll(id);
                    MarketModel.Instance.RemovePlzGoodInfoMine(id);
                }
            }
            GameLog.Info("Market", "15116 撤销求购结果: code={0} id={1}", code, id);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_RESULT, Proto.MARKET_PLZ_CANCEL, code);
        }

        // ---- 15117: 出售给求购单 ----

        /// <summary>15117 出售给求购单(发 "lliii" Id,BuyerId,TypeId,GoodsNum,Price;老端
        /// MarketShowItem.ts:163/MarketSellTipView.ts:102)。</summary>
        public void RequestSellToPlz(long id, long buyerId, int typeId, int goodsNum, int price)
            => SendFmt(Proto.MARKET_PLZ_SELL, "lliii", id, buyerId, typeId, goodsNum, price);

        // 15117: Errcode:32,Id:64,GoodsNum:32(pt_151.erl:267-277)。对标老端 on15117(ts:251-264):
        // 成功只摘除 seek_info_all(不动 seek_info_mine,ts:255 只调一个,与 15116 两个都摘不同——按原文
        // 逐字镜像);errcode==1510023 同 15116,两个列表都摘。
        private void On15117(NetReader r)
        {
            int code = (int)r.ReadU32();
            long id = r.ReadU64();
            int goodsNum = r.ReadI32();
            if (code == 1)
            {
                TipsManager.Toast("出售成功"); // 对标老端 on15117(ts:254) local_Message.show
                MarketModel.Instance.RemovePlzGoodInfoAll(id);
            }
            else
            {
                ShowError(code);
                if (code == 1510023)
                {
                    MarketModel.Instance.RemovePlzGoodInfoAll(id);
                    MarketModel.Instance.RemovePlzGoodInfoMine(id);
                }
            }
            GameLog.Info("Market", "15117 出售给求购单结果: code={0} id={1} goodsNum={2}", code, id, goodsNum);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_RESULT, Proto.MARKET_PLZ_SELL, code);
        }

        // ---- 15118: 求购列表(全服,分页) ----

        /// <summary>15118 求购列表(全服,分页;发 "hh" PageNo,PageSize;老端 MarketPlzListView.ts:64/134)。</summary>
        public void RequestPlzListAll(int pageNo, int pageSize) => SendFmt(Proto.MARKET_PLZ_LIST_ALL, "hh", pageNo, pageSize);

        // 15118: PageTotal:16,PageNo:16,PageSize:16,SeekList[u16×9字段,ServerNum:64 独例]
        // (pt_151.erl:279-298,item_to_bin_10)。对标老端 on15118(ts:266-270)。
        private void On15118(NetReader r)
        {
            int pageTotal = r.ReadU16();
            int pageNo = r.ReadU16();
            int pageSize = r.ReadU16();
            List<MarketModel.SeekEntry> list = r.ReadArray(rr => new MarketModel.SeekEntry
            {
                Id = rr.ReadU64(),
                SerId = rr.ReadU64(),
                ServerNum = rr.ReadU64(),
                PlayerId = rr.ReadU64(),
                RoleName = rr.ReadString(),
                TypeId = rr.ReadI32(),
                GoodsNum = rr.ReadU16(),
                UnitPrice = rr.ReadI32(),
                Time = rr.ReadI32(),
            });
            MarketModel.Instance.SetSeekAllInfo(pageTotal, pageNo, pageSize, list);
            GameLog.Info("Market", "15118 求购列表(全服): pageTotal={0} pageNo={1} pageSize={2} listN={3}",
                pageTotal, pageNo, pageSize, list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_UPDATE, Proto.MARKET_PLZ_LIST_ALL);
        }

        // ---- 15119: 我的求购列表 ----

        /// <summary>15119 我的求购列表(裸;老端 MarketPlzListView.ts:66/136)。</summary>
        public void RequestPlzListMine() => SendFmt(Proto.MARKET_PLZ_LIST_MINE);

        // 15119: SeekList[u16×5字段](pt_151.erl:300-313,item_to_bin_11,比 15118 少 SerId/ServerNum/
        // RoleName)。对标老端 on15119(ts:272-276)。
        private void On15119(NetReader r)
        {
            List<MarketModel.SeekEntry> list = r.ReadArray(rr => new MarketModel.SeekEntry
            {
                Id = rr.ReadU64(),
                TypeId = rr.ReadI32(),
                GoodsNum = rr.ReadU16(),
                UnitPrice = rr.ReadI32(),
                Time = rr.ReadI32(),
            });
            MarketModel.Instance.SetSeekMineInfo(list);
            GameLog.Info("Market", "15119 我的求购列表: listN={0}", list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_UPDATE, Proto.MARKET_PLZ_LIST_MINE);
        }

        // ---- 15120: 删除推送(S2C only) ----

        // 15120: SellType:8,Type:32,Subtype:32,Id:64(S2C only 推送,pt_151.erl:315-327)。对标老端
        // on15120(ts:278-287):sell_type==1(挂单)摘除挂牌+上架缓存;sell_type==3(求购)摘除两个求购
        // 缓存;sell_type==2(P2P)老端本就无分支,死路径不镜像。
        private void On15120(NetReader r)
        {
            int sellType = r.ReadU8();
            int type = r.ReadI32();
            int subtype = r.ReadI32();
            long id = r.ReadU64();
            if (sellType == 1)
            {
                MarketModel.Instance.RemoveSellGoodInfo(type, subtype, id);
                MarketModel.Instance.RemoveShelfGoodInfo(id);
            }
            else if (sellType == 3)
            {
                MarketModel.Instance.RemovePlzGoodInfoAll(id);
                MarketModel.Instance.RemovePlzGoodInfoMine(id);
            }
            GameLog.Info("Market", "15120 删除推送: sellType={0} type={1} subtype={2} id={3}", sellType, type, subtype, id);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_UPDATE, Proto.MARKET_SELL_DELETE_PUSH);
        }

        // ---- 15122: 喊话 ----

        /// <summary>15122 喊话(发 "l" SellId;老端 MarketSellGoodsItem.ts:60)。</summary>
        public void RequestShout(long sellId) => SendFmt(Proto.MARKET_SHOUT, "l", sellId);

        // 15122: Errcode:32,SellId:64,CdTime:32(pt_151.erl:337-347)。对标老端 on15122(ts:299-307):
        // 成功分支为空(`if(errcode==1){}`),只在失败分支显码——按血训逐字镜像,不臆造成功侧的数据处理/
        // CD 缓存。
        private void On15122(NetReader r)
        {
            int code = (int)r.ReadU32();
            long sellId = r.ReadU64();
            int cdTime = (int)r.ReadU32();
            if (code != 1) ShowError(code);
            GameLog.Info("Market", "15122 喊话结果: code={0} sellId={1} cdTime={2}", code, sellId, cdTime);
            EventDispatcher.Emit(GlobalEvent.EVT_MARKET_RESULT, Proto.MARKET_SHOUT, code);
        }

        // ---- 小工具:共享嵌套读法 ----

        /// <summary>15102/15109 共享的 9 字段商品列表(item_to_bin_1/5,EquipExtraAttr 二层嵌套)。</summary>
        private static List<MarketModel.GoodsEntry> ReadGoodsList(NetReader r) => r.ReadArray(rr => new MarketModel.GoodsEntry
        {
            Id = rr.ReadU64(),
            PlayerId = rr.ReadU64(),
            TypeId = rr.ReadI32(),
            GoodsNum = rr.ReadI32(),
            Rating = rr.ReadI32(),
            OverallRating = rr.ReadI32(),
            UnitPrice = rr.ReadI32(),
            SellType = rr.ReadU8(),
            EquipExtraAttr = ReadEquipExtraAttrList(rr),
        });

        /// <summary>15112 交易记录 9 字段列表(item_to_bin_7,EquipExtraAttr 二层嵌套)。</summary>
        private static List<MarketModel.RecordEntry> ReadRecordList(NetReader r) => r.ReadArray(rr => new MarketModel.RecordEntry
        {
            TypeId = rr.ReadI32(),
            GoodsNum = rr.ReadI32(),
            Rating = rr.ReadI32(),
            OverallRating = rr.ReadI32(),
            Type = rr.ReadU8(),
            Tax = rr.ReadI32(),
            Price = rr.ReadI32(),
            Time = rr.ReadI32(),
            EquipExtraAttr = ReadEquipExtraAttrList(rr),
        });

        /// <summary>装备额外属性(二层嵌套,item_to_bin_2/4/6/8,四处同构:Color:8,TypeId:8,AttrId:16,
        /// AttrVal:32,PlusInterval:8,PlusUnit:32)。</summary>
        private static List<MarketModel.EquipExtraAttrEntry> ReadEquipExtraAttrList(NetReader r) => r.ReadArray(rr => new MarketModel.EquipExtraAttrEntry
        {
            Color = rr.ReadU8(),
            TypeId = rr.ReadU8(),
            AttrId = rr.ReadU16(),
            AttrVal = rr.ReadI32(),
            PlusInterval = rr.ReadU8(),
            PlusUnit = rr.ReadI32(),
        });
    }
}
