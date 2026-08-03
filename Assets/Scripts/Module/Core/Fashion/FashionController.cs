using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 时装协议控制器(对标老端 commonController/FashionController.ts;服务端 pt_413/pp_fashion)。
    /// 已接管时装主体、部位升级41305与套装41313-41315。
    /// ⚠死号严禁发:41307 全死、41310 客户端侧死(见 Proto.cs 413xx 段族注释证据)。
    /// 41311 上行死(不发),但仅活下行——本控制器注册接收,处理自身形象增量变更。
    /// </summary>
    public sealed class FashionController : BaseController
    {
        public static readonly FashionController Instance = new FashionController();

#if UNITY_EDITOR
        // CliVerify 出站截获缝：返回 true 时只保留真实编码帧，不访问活动连接。
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private FashionController() { }
        private readonly HashSet<long> _pendingPowerRequests = new HashSet<long>();

        protected override void Register()
        {
            RegisterProtocal(Proto.FASHION_INFO_ALL, On41300);
            RegisterProtocal(Proto.FASHION_UNLOCK_COLOR, On41301);
            RegisterProtocal(Proto.FASHION_WEAR, On41302);
            RegisterProtocal(Proto.FASHION_TAKE_OFF, On41303);
            RegisterProtocal(Proto.FASHION_ACTIVE, On41304);
            RegisterProtocal(Proto.FASHION_POSITION_UPGRADE, On41305);
            RegisterProtocal(Proto.FASHION_UPGRADE_BASE, On41306);
            RegisterProtocal(Proto.FASHION_POWER, On41312);
            RegisterProtocal(Proto.FASHION_SUIT_INFO, On41313);
            RegisterProtocal(Proto.FASHION_SUIT_ACTIVATE, On41314);
            RegisterProtocal(Proto.FASHION_SUIT_UPGRADE, On41315);
            RegisterProtocal(Proto.FASHION_UPGRADE_COLOR, On41316);
            RegisterProtocal(Proto.FASHION_FIGURE_PUSH, On41311);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            _pendingPowerRequests.Clear();
            FashionModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>对标老端 GoodsModel.CREATE_BAG_LIST_FINISH → Fire(SCMD_REQUEST,41300)(FashionController.ts:97)。
        /// 本端简化为 EVT_GAME_START 后直接拉取(41300 请求本身无参,不依赖背包数据就绪,配表就绪即可)。
        /// 套装快照41313同样在 GAME_START 拉取;服务端也可能在符合数量变化时主动推送同号快照。</summary>
        private async void OnGameStart()
        {
            await FashionConfigs.EnsureLoaded();
            RequestInfoAll();
            RequestSuitInfo();
        }

        /// <summary>41300 全量拉取(发空)。</summary>
        public void RequestInfoAll()
        {
            SendRequest(Proto.FASHION_INFO_ALL);
        }

        /// <summary>41305 衣服部位升级。goodsId 是背包物品实例id,不是物品类型id。</summary>
        public void UpgradePosition(int posId, IReadOnlyList<(long goodsId, int num)> goodsList)
        {
            if (posId != 1 || goodsList == null || goodsList.Count == 0 || goodsList.Count > ushort.MaxValue)
            {
                GameLog.Warn("Fashion", "41305 拒发:pos={0} goodsCount={1}", posId, goodsList?.Count ?? 0);
                return;
            }

            var fmt = new System.Text.StringBuilder("ch");
            var args = new List<object>(2 + goodsList.Count * 2) { posId, goodsList.Count };
            foreach ((long goodsId, int num) item in goodsList)
            {
                if (item.goodsId <= 0 || item.num <= 0 || item.num > ushort.MaxValue)
                {
                    GameLog.Warn("Fashion", "41305 拒发:goodsId={0} num={1}", item.goodsId, item.num);
                    return;
                }
                fmt.Append("lh");
                args.Add(item.goodsId);
                args.Add(item.num);
            }
            SendRequest(Proto.FASHION_POSITION_UPGRADE, fmt.ToString(), args.ToArray());
        }

        /// <summary>41313 套装全量信息(发空;也注册接收服务端主动推送)。</summary>
        public void RequestSuitInfo()
        {
            SendRequest(Proto.FASHION_SUIT_INFO);
        }

        /// <summary>41314 激活套装档位;老端只发送高级2件/完美4件两档。</summary>
        public void ActivateSuit(int suitId, int activeNum)
        {
            if (suitId <= 0 || suitId > byte.MaxValue ||
                (activeNum != FashionModel.SUIT_HIGH_ACTIVE_COUNT && activeNum != FashionModel.SUIT_PERFECT_ACTIVE_COUNT))
            {
                GameLog.Warn("Fashion", "41314 拒发:suit={0} activeNum={1}", suitId, activeNum);
                return;
            }
            SendRequest(Proto.FASHION_SUIT_ACTIVATE, "cc", suitId, activeNum);
        }

        /// <summary>41315 套装升阶。</summary>
        public void UpgradeSuit(int suitId)
        {
            if (suitId <= 0 || suitId > byte.MaxValue)
            {
                GameLog.Warn("Fashion", "41315 拒发:suit={0}", suitId);
                return;
            }
            SendRequest(Proto.FASHION_SUIT_UPGRADE, "c", suitId);
        }

        private void SendRequest(int protoId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            if (s_outboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
                if (s_outboundIntercept(frame)) return;
            }
#endif
            SendFmt(protoId, format, args);
        }

        /// <summary>41301 解锁颜色(发 "cicc" PosId,FashionId,ColorId,Type;Type 恒传 2——老端只发 Type=2,
        /// Type=1 服务端未用,严禁传1)。</summary>
        public void UnlockColor(int posId, int fashionId, int colorId)
        {
            if (posId <= 0 || fashionId <= 0) return;
            SendRequest(Proto.FASHION_UNLOCK_COLOR, "cicc", posId, fashionId, colorId, 2);
        }

        /// <summary>41302 穿戴(发 "cic" PosId,FashionId,ColorId)。</summary>
        public void Wear(int posId, int fashionId, int colorId)
        {
            if (posId <= 0 || fashionId <= 0) return;
            SendRequest(Proto.FASHION_WEAR, "cic", posId, fashionId, colorId);
        }

        /// <summary>41303 卸下(发 "ci" PosId,FashionId)。</summary>
        public void TakeOff(int posId, int fashionId)
        {
            if (posId <= 0 || fashionId <= 0) return;
            SendRequest(Proto.FASHION_TAKE_OFF, "ci", posId, fashionId);
        }

        /// <summary>41304 激活(发 "ci" PosId,FashionId)。</summary>
        public void Activate(int posId, int fashionId)
        {
            if (posId <= 0 || fashionId <= 0) return;
            SendRequest(Proto.FASHION_ACTIVE, "ci", posId, fashionId);
        }

        /// <summary>41306 基础色(color 0)进阶(发 "cic" PosId,FashionId,ColorId;ColorId 只应传 0——
        /// 非0色走 <see cref="UpgradeColor"/>)。</summary>
        public void UpgradeBase(int posId, int fashionId)
        {
            if (posId <= 0 || fashionId <= 0) return;
            SendRequest(Proto.FASHION_UPGRADE_BASE, "cic", posId, fashionId, 0);
        }

        /// <summary>41316 彩色(非0色)进阶(发 "cic" PosId,FashionId,ColorId)。⚠调用方必须保证 colorId 已解锁
        /// (在 color_list 里)——服务端 lib_fashion_check.erl:141 对未解锁颜色 keyfind 会 badmatch 崩进程。</summary>
        public void UpgradeColor(int posId, int fashionId, int colorId)
        {
            if (posId <= 0 || fashionId <= 0 || colorId <= 0) return;
            FashionModel.FashionEntry e = FashionModel.Instance.GetActive(posId, fashionId);
            if (e == null || !e.IsColorUnlocked(colorId))
            {
                GameLog.Warn("Fashion", "UpgradeColor 拒发:pos={0} fashion={1} color={2} 未解锁(防服务端 badmatch)", posId, fashionId, colorId);
                return;
            }
            SendRequest(Proto.FASHION_UPGRADE_COLOR, "cic", posId, fashionId, colorId);
        }

        /// <summary>41312 时装战力(发 "ci" PosId,FashionId)。</summary>
        public void RequestPower(int posId, int fashionId)
        {
            if (posId <= 0 || fashionId <= 0) return;
            long key = ((long)posId << 40) | (uint)fashionId;
            if (!_pendingPowerRequests.Add(key)) return;
            SendRequest(Proto.FASHION_POWER, "ci", posId, fashionId);
        }

        /// <summary>41300 回包:Code:i, PosList[u16×{PosId:c,WearFashionId:i,PosLv:h,PosUpgradeNum:i,
        /// FashionList[u16×{FashionId:i,FashionStarLv:h,NowColorId:c,ColorList[u16×{ColorId:c,FashionStarLv:h}]}]}]。
        /// 对标老端 On41300:code!=1 无 else 分支(静默),照抄不额外显码。</summary>
        private void On41300(NetReader r)
        {
            int code = (int)r.ReadU32();
            List<FashionModel.PosWire> posList = r.ReadArray(ReadPosWire);
            if (code != 1)
            {
                GameLog.Info("Fashion", "41300 code={0}(非1,老端亦静默不显码)", code);
                return;
            }
            FashionModel.Instance.Apply41300(posList);
            GameLog.Info("Fashion", "41300 全量落地 pos={0} remaining={1}B", posList.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        private static FashionModel.PosWire ReadPosWire(NetReader r)
        {
            var pos = new FashionModel.PosWire
            {
                PosId = r.ReadU8(),
                WearFashionId = (int)r.ReadU32(),
                PosLv = r.ReadU16(),
                PosUpgradeNum = r.ReadU32(),
            };
            pos.Fashions = r.ReadArray(ReadFashionWire);
            return pos;
        }

        private static FashionModel.FashionWire ReadFashionWire(NetReader r)
        {
            var f = new FashionModel.FashionWire
            {
                FashionId = (int)r.ReadU32(),
                StarLv = r.ReadU16(),
                NowColorId = r.ReadU8(),
            };
            f.Colors = r.ReadArray(ReadColorWire);
            return f;
        }

        private static FashionModel.ColorWire ReadColorWire(NetReader r)
        {
            return new FashionModel.ColorWire { ColorId = r.ReadU8(), StarLv = r.ReadU16() };
        }

        /// <summary>41301 回包:Code:i,PosId:c,FashionId:i,ColorId:c,Type:c。对标老端 On41301:code==1 落地,
        /// 否则 Util.ErrorCodeShow(显码)。</summary>
        private void On41301(NetReader r)
        {
            int code = (int)r.ReadU32();
            int posId = r.ReadU8();
            int fashionId = (int)r.ReadU32();
            _pendingPowerRequests.Remove(((long)posId << 40) | (uint)fashionId);
            int colorId = r.ReadU8();
            int type = r.ReadU8();
            if (code != 1)
            {
                TipsManager.Toast("解锁颜色失败(" + code + ")");
                GameLog.Info("Fashion", "41301 fail code={0} pos={1} fashion={2}", code, posId, fashionId);
                return;
            }
            FashionModel.Instance.Apply41301(posId, fashionId, colorId);
            GameLog.Info("Fashion", "41301 解锁颜色 pos={0} fashion={1} color={2} type={3}", posId, fashionId, colorId, type);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        /// <summary>41302 回包:Code:i,PosId:c,FashionId:i,ColorId:c。对标老端 On41302:code==1 落地,否则显码。</summary>
        private void On41302(NetReader r)
        {
            int code = (int)r.ReadU32();
            int posId = r.ReadU8();
            int fashionId = (int)r.ReadU32();
            int colorId = r.ReadU8();
            if (code != 1)
            {
                TipsManager.Toast("穿戴失败(" + code + ")");
                GameLog.Info("Fashion", "41302 fail code={0} pos={1} fashion={2}", code, posId, fashionId);
                return;
            }
            FashionModel.Instance.Apply41302(posId, fashionId, colorId);
            GameLog.Info("Fashion", "41302 穿戴 pos={0} fashion={1} color={2}", posId, fashionId, colorId);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        /// <summary>41303 回包:Code:i,PosId:c,FashionId:i。⚠也会作为被动卸下广播到达(穿神殿/套装收集/天启顶掉时装),
        /// 对标老端 On41303:code==1 落地,无 else(静默,照抄不显码)。</summary>
        private void On41303(NetReader r)
        {
            int code = (int)r.ReadU32();
            int posId = r.ReadU8();
            int fashionId = (int)r.ReadU32();
            if (code != 1)
            {
                GameLog.Info("Fashion", "41303 code={0}(非1,老端亦静默不显码) pos={1} fashion={2}", code, posId, fashionId);
                return;
            }
            FashionModel.Instance.Apply41303(posId);
            GameLog.Info("Fashion", "41303 卸下 pos={0} fashion={1}", posId, fashionId);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        /// <summary>41304 回包:Code:i,PosId:c,FashionId:i。对标老端 On41304:code==1 落地 + 自动 Fire(41302,pos,fashion,0)
        /// 补穿(FashionController.ts:288);无 else(静默,照抄不显码)。</summary>
        private void On41304(NetReader r)
        {
            int code = (int)r.ReadU32();
            int posId = r.ReadU8();
            int fashionId = (int)r.ReadU32();
            if (code != 1)
            {
                GameLog.Info("Fashion", "41304 code={0}(非1,老端亦静默不显码) pos={1} fashion={2}", code, posId, fashionId);
                return;
            }
            FashionModel.Instance.Apply41304(posId, fashionId);
            GameLog.Info("Fashion", "41304 激活 pos={0} fashion={1} → 自动补穿", posId, fashionId);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
            Wear(posId, fashionId, 0);   // 对标老端激活成功后自动 Fire(SCMD_REQUEST,41302,pos,fashion,0)
        }

        /// <summary>41305 回包:Code:i,PosId:c,PosLv:h,PosUpgradeNum:i。</summary>
        private void On41305(NetReader r)
        {
            int code = (int)r.ReadU32();
            int posId = r.ReadU8();
            int posLv = r.ReadU16();
            long posUpgradeNum = r.ReadU32();
            if (code != 1)
            {
                TipsManager.Toast("部位升级失败(" + code + ")");
                GameLog.Info("Fashion", "41305 fail code={0} pos={1}", code, posId);
                return;
            }
            if (!FashionModel.Instance.Apply41305(posId, posLv, posUpgradeNum))
            {
                GameLog.Warn("Fashion", "41305 成功但本地缺 pos={0},重拉41300", posId);
                RequestInfoAll();
                return;
            }
            TipsManager.Toast("成功");
            GameLog.Info("Fashion", "41305 部位升级 pos={0} lv={1} exp={2}", posId, posLv, posUpgradeNum);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        /// <summary>41306 回包:Code:i,PosId:c,FashionId:i,ColorId:c,FashionStarLv:h。对标老端 On41306:code==1 落地,
        /// 否则显码。</summary>
        private void On41306(NetReader r)
        {
            int code = (int)r.ReadU32();
            int posId = r.ReadU8();
            int fashionId = (int)r.ReadU32();
            int colorId = r.ReadU8();
            int starLv = r.ReadU16();
            if (code != 1)
            {
                TipsManager.Toast("进阶失败(" + code + ")");
                GameLog.Info("Fashion", "41306 fail code={0} pos={1} fashion={2}", code, posId, fashionId);
                return;
            }
            FashionModel.Instance.Apply41306(posId, fashionId, colorId, starLv);
            GameLog.Info("Fashion", "41306 基础色进阶 pos={0} fashion={1} color={2} → {3}星", posId, fashionId, colorId, starLv);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        /// <summary>41312 回包(⚠无 Code 首位):PosId:c,FashionId:i,ColorPowerList[u16×{ColorId:c,ColorPower:l,NextColorPower:l}]。
        /// 对标老端 On41312 直转发 UPDATE_FIGHT,无成功/失败之分。</summary>
        private void On41312(NetReader r)
        {
            int posId = r.ReadU8();
            int fashionId = (int)r.ReadU32();
            List<FashionModel.PowerEntry> powers = r.ReadArray(ReadPowerEntry);
            FashionModel.Instance.Apply41312(posId, fashionId, powers);
            GameLog.Info("Fashion", "41312 战力 pos={0} fashion={1} colors={2}", posId, fashionId, powers.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        private static FashionModel.PowerEntry ReadPowerEntry(NetReader r)
        {
            return new FashionModel.PowerEntry { ColorId = r.ReadU8(), Power = (long)r.ReadU64(), NextPower = (long)r.ReadU64() };
        }

        /// <summary>41313 套装快照(无 Code):FashionSuit[h×{SuitId:c,Lv:c,ActiveNum:c,ConformNum:c,Power:i,NextPower:i}]。</summary>
        private void On41313(NetReader r)
        {
            List<FashionModel.SuitWire> suits = r.ReadArray(ReadSuitWire);
            FashionModel.Instance.Apply41313(suits);
            GameLog.Info("Fashion", "41313 套装快照 count={0} remaining={1}B", suits.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        private static FashionModel.SuitWire ReadSuitWire(NetReader r)
        {
            return new FashionModel.SuitWire
            {
                SuitId = r.ReadU8(),
                Lv = r.ReadU8(),
                ActiveNum = r.ReadU8(),
                ConformNum = r.ReadU8(),
                Power = r.ReadU32(),
                NextPower = r.ReadU32(),
            };
        }

        /// <summary>41314 回包:SuitId:c,ActiveNum:c,Code:i,Power:i,NextPower:i(Code 在第三位)。</summary>
        private void On41314(NetReader r)
        {
            int suitId = r.ReadU8();
            int activeNum = r.ReadU8();
            int code = (int)r.ReadU32();
            long power = r.ReadU32();
            long nextPower = r.ReadU32();
            if (code != 1)
            {
                TipsManager.Toast("套装激活失败(" + code + ")");
                GameLog.Info("Fashion", "41314 fail code={0} suit={1} activeNum={2}", code, suitId, activeNum);
                return;
            }
            if (!FashionModel.Instance.Apply41314(suitId, activeNum, power, nextPower))
            {
                GameLog.Warn("Fashion", "41314 成功但本地缺 suit={0},重拉41313", suitId);
                RequestSuitInfo();
                return;
            }
            TipsManager.Toast("激活成功");
            GameLog.Info("Fashion", "41314 套装激活 suit={0} activeNum={1}", suitId, activeNum);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        /// <summary>41315 回包:SuitId:c,Lv:c,Code:i,Power:i,NextPower:i(Code 在第三位)。</summary>
        private void On41315(NetReader r)
        {
            int suitId = r.ReadU8();
            int lv = r.ReadU8();
            int code = (int)r.ReadU32();
            long power = r.ReadU32();
            long nextPower = r.ReadU32();
            if (code != 1)
            {
                TipsManager.Toast("套装进阶失败(" + code + ")");
                GameLog.Info("Fashion", "41315 fail code={0} suit={1} lv={2}", code, suitId, lv);
                return;
            }
            if (!FashionModel.Instance.Apply41315(suitId, lv, power, nextPower))
            {
                GameLog.Warn("Fashion", "41315 成功但本地缺 suit={0},重拉41313", suitId);
                RequestSuitInfo();
                return;
            }
            TipsManager.Toast("进阶成功");
            GameLog.Info("Fashion", "41315 套装进阶 suit={0} lv={1}", suitId, lv);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        /// <summary>41316 回包(⚠Code 在最后,Lv 是 8位——与 41306 的 FashionStarLv 16位不同):
        /// PosId:c,FashionId:i,ColorId:c,Lv:c,Code:i。对标老端 On41316:code==1 落地,否则显码。</summary>
        private void On41316(NetReader r)
        {
            int posId = r.ReadU8();
            int fashionId = (int)r.ReadU32();
            int colorId = r.ReadU8();
            int lv = r.ReadU8();
            int code = (int)r.ReadU32();
            if (code != 1)
            {
                TipsManager.Toast("进阶失败(" + code + ")");
                GameLog.Info("Fashion", "41316 fail code={0} pos={1} fashion={2}", code, posId, fashionId);
                return;
            }
            FashionModel.Instance.Apply41316(posId, fashionId, colorId, lv);
            GameLog.Info("Fashion", "41316 彩色进阶 pos={0} fashion={1} color={2} → {3}星", posId, fashionId, colorId, lv);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        /// <summary>41311 外观形象增量广播(⚠仅活下行,本端永不主动请求):RoleId:l,
        /// FashionEquip[u16×{PartPos:c,FashionModelId:i,FashionChartletId:c}]。对标老端 On41311:
        /// role_vo.ChangeVar("fashion_model_list", scmd.fashion_equip)(有早退保护:双方都空则跳过)。
        /// ⚠范围边界:本控制器只处理自身(RoleId==RoleModel.Instance.RoleId)——直接改 RoleModel.Instance.Figure.Raw，
        /// 并发 EVT_ROLE_FIGURE_UPDATE 让场景主角按新衣服/头饰/染色权威重建。
        /// 场景内其它角色(RoleId!=self)的形象合并需要 SceneController 的私有角色表,同理不在本包范围,只记日志。</summary>
        private void On41311(NetReader r)
        {
            long roleId = (long)r.ReadU64();
            List<(int partPos, int modelId, int chartletId)> equip = r.ReadArray(ReadFashionEquip);

            if (roleId != RoleModel.Instance.RoleId)
            {
                GameLog.Info("Fashion", "41311 非本人(role_id={0})形象广播,场景角色合并未接线(需 SceneController 私有角色表,不在本包范围)", roleId);
                return;
            }

            var current = RoleModel.Instance.Figure?.Raw != null
                && RoleModel.Instance.Figure.Raw.TryGetValue("fashion_model_list", out object cur)
                && cur is List<Dictionary<string, object>> curList ? curList : null;
            if ((current == null || current.Count == 0) && equip.Count == 0)
            {
                GameLog.Info("Fashion", "41311 双方都空,跳过(对标老端早退保护)");
                return; // 对标老端"role_vo.fashion_model_list.length == scmd.fashion_equip.length && ...== 0 → return"
            }

            var list = new List<Dictionary<string, object>>(equip.Count);
            foreach ((int partPos, int modelId, int chartletId) in equip)
            {
                list.Add(new Dictionary<string, object>
                {
                    ["part_pos"] = (byte)partPos,
                    ["fashion_model_id"] = (uint)modelId,
                    ["fashion_chartlet_id"] = (byte)chartletId,
                });
            }
            if (RoleModel.Instance.Figure != null)
            {
                RoleModel.Instance.Figure.Raw["fashion_model_list"] = list;
            }
            GameLog.Info("Fashion", "41311 本人形象增量落地 equip={0}", list.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_FIGURE_UPDATE);
            EventDispatcher.Emit(GlobalEvent.EVT_FASHION_UPDATE);
        }

        private static (int partPos, int modelId, int chartletId) ReadFashionEquip(NetReader r)
        {
            return (r.ReadU8(), (int)r.ReadU32(), r.ReadU8());
        }
    }
}
