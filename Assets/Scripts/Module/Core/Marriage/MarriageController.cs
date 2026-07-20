using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Friend;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚姻(征友/戒指/结婚,自动循环 轮16)控制器:pt_172 172xx(征友17200-05/戒指17210-13/求婚·结婚·
    /// 离婚·秀恩爱17222-40/副本匹配邀请17245-97)+ 223xx 鲜花(22300-05)。共 33 号,纯数据层接入
    /// (UI 14 个 View 已烤 Bind 但空壳,本轮不接 View,数据从 MarriageModel 取,消费方留 port-view-bindings
    /// 尾包,同 15a/15b Boss 先例)。轮22 族错误出口批补 17263/17264(婚礼场景进出,老端挂
    /// BanquetController.ts 但协议仍属 pt_172),共 35 号。
    ///
    /// 纪律:①CombatPower 位宽独例——17222(推送)=u32,17226(bin_6/bin_8)与17232=u64,逐号严格照抄 r16,
    /// 勿套统一模板;②17200 bin_0 无 CombatPower 字段;③无 Code 前导帧一批(17205/17222/17224/17226/
    /// 17229/17238/17244[Banquet占用不接]/17296/17297),On&lt;num&gt; 首字段直接读业务字段;17246 与
    /// r16 侦察报告"无Code"结论不同——ClientProtocol.json 与老端 on17246 实读 scmd.code,直接核对原文
    /// 订正为**带 Code**(本代理直接核实覆盖侦察报告误判,见 Proto.cs 17246 注释);④17212 戒指单步提升是
    /// 死号(老端注册 handler 但零发送点+成功分支全注释,实际升级走17213一键),本端只注册防御 recv、
    /// **不提供发送方法**;⑤17226 必须 ReadArray 读完两个数组(biaobai_list + biaobai_answer_list)保证
    /// 游标正确,老端只消费 biaobai_list、不消费 answer_list,本端两者都落地(比老端完整无害);
    /// ⑥17245/17246 死链——不仅 UI 层死(MarriageMatchView/MatchTipsView/MarriageTagView 三文件在但无任何
    /// 活模块 import,模块从未加载,window 注册不执行,OPEN 静默失败),服务端 C2S handler(17245)也已整段
    /// 注释(pp_marriage.erl:1598-1607)、17246 全部推送点无触发源(lib_marriage.erl:2038-2129/
    /// mod_marriage_match.erl:219)——本端不提供 17245 发送方法(RequestDunMatch 按轮16三镜头验收裁决1删除,
    /// 死号严禁发),On17245/On17246 仅保留防御性 recv,协议注册仅防御,数据层照接解析落地+发事件供尾包消费;
    /// ⑦17210 戒指战力自算(老端用 config_ring_star 覆盖服务端 ring_combat_power)本轮**不接**,先如实落地
    /// 服务端权威值,TODO 见 On17210;⑧17237 购买礼包成功后老端额外经 ChatModel 发情侣公告私信
    /// (BoardMarriager),属跨模块社交联动,本轮数据层不接,TODO;⑨标签子系统(config_personal_tag_info)有
    /// 3 个活视图消费(MarriageComView.ts:89/MarriageFriendItem.ts:109/MarriageIssueView.ts:102·174 经
    /// GetTagsStr 渲染标签文案,同属尾包依赖的还有 config_fame_lv),仅其编辑入口 MarriageTagView 模块从未
    /// 加载(死链)——本轮数据层不消费标签文案仍不导入该表,17200 player_list 的 tag_list 字段照常解析落地,
    /// UI 尾包接线时须补导 config_personal_tag_info+config_fame_lv。
    ///
    /// 登录/升级链(B1 修复,镜像老端 MarriageController.ts:78-100):GAME_START 时恒发 22303(鲜花信息);
    /// 等级≥config_marriage_constant id=1(MarriageOpenLv=170)时补发 17226/17238/17210;CHANGE_LEVEL 精确
    /// 达到该临界值时补发 17238/17210(ts:93-100,本端用 EVT_ROLE_INFO_UPDATE+_lastLevel 做等价探测,同
    /// DailyController 先例)。
    /// 跨天补发 17238(轮20 实接):老端 ServerTimeModel.DAY_CHANGE → SendFmtToGame(17238)
    /// (MarriageController.ts:118-120),本端订阅 <see cref="GlobalEvent.EVT_SERVER_DAY_CHANGE"/>
    /// 调用既有 <see cref="RequestGiftInfo"/>,见 <see cref="OnServerDayChange"/>。
    /// </summary>
    public sealed class MarriageController : BaseController
    {
        public static readonly MarriageController Instance = new MarriageController();
        private MarriageController() { }

        /// <summary>上次已处理的角色等级(对标老端 CHANGE_LEVEL 事件基准,-1=未初始化;同 DailyController
        /// _lastLevel 先例,经 EVT_ROLE_INFO_UPDATE 探测等级变化)。</summary>
        private int _lastLevel = -1;

        private static void ShowError(int errorCode) => TipsManager.Toast("错误(" + errorCode + ")"); // 错误码表未移植,显码降级

        protected override void Register()
        {
            // ---- 征友 Personals(17200-17205) ----
            RegisterProtocal(Proto.MARRIAGE_PERSONALS_LIST, On17200);
            RegisterProtocal(Proto.MARRIAGE_PERSONALS_FOLLOW, On17201);
            RegisterProtocal(Proto.MARRIAGE_PERSONALS_ISSUE, On17202);
            RegisterProtocal(Proto.MARRIAGE_ROLE_DETAIL, On17205);

            // ---- 戒指 Ring(17210-17213;17212 死号仅防御 recv) ----
            RegisterProtocal(Proto.MARRIAGE_RING_INFO, On17210);
            RegisterProtocal(Proto.MARRIAGE_RING_UNLOCK, On17211);
            RegisterProtocal(Proto.MARRIAGE_RING_UPGRADE_STEP, On17212);
            RegisterProtocal(Proto.MARRIAGE_RING_UPGRADE_ALL, On17213);

            // ---- 求婚/结婚/离婚/秀恩爱(17222-17240) ----
            RegisterProtocal(Proto.MARRIAGE_PROPOSE_PUSH, On17222);
            RegisterProtocal(Proto.MARRIAGE_PROPOSE_RESPOND, On17223);
            RegisterProtocal(Proto.MARRIAGE_ANSWER_PUSH, On17224);
            RegisterProtocal(Proto.MARRIAGE_BIAOBAI_LIST, On17226);
            RegisterProtocal(Proto.MARRIAGE_KEY_VALUE_PUSH, On17229);
            RegisterProtocal(Proto.MARRIAGE_PROPOSE_SEND, On17231);
            RegisterProtocal(Proto.MARRIAGE_MATE_INFO, On17232);
            RegisterProtocal(Proto.MARRIAGE_DIVORCE_SEND, On17234);
            RegisterProtocal(Proto.MARRIAGE_DIVORCE_RESPOND, On17235);
            RegisterProtocal(Proto.MARRIAGE_DSGT_TAKE, On17236);
            RegisterProtocal(Proto.MARRIAGE_GIFT_BUY, On17237);
            RegisterProtocal(Proto.MARRIAGE_GIFT_INFO, On17238);
            RegisterProtocal(Proto.MARRIAGE_GIFT_TAKE, On17239);
            RegisterProtocal(Proto.MARRIAGE_GIFT_ASK_BUY, On17240);

            // ---- 副本匹配/邀请(17245-17297;17245 服务端入口已封存,17245/17246 仅防御接收) ----
            RegisterProtocal(Proto.MARRIAGE_DUN_MATCH, On17245);
            RegisterProtocal(Proto.MARRIAGE_DUN_MATCH_RESULT, On17246);
            RegisterProtocal(Proto.MARRIAGE_DUN_INVITE_BUY, On17295);
            RegisterProtocal(Proto.MARRIAGE_DUN_INVITE_PUSH, On17296);
            RegisterProtocal(Proto.MARRIAGE_DUN_INVITE_RESPOND, On17297);

            // ---- 婚礼场景进出错误壳(轮22 族错误出口批;老端挂 BanquetController.ts,协议仍属 pt_172) ----
            RegisterProtocal(Proto.MARRIAGE_BANQUET_ENTER_SCENE, On17263);
            RegisterProtocal(Proto.MARRIAGE_BANQUET_LEAVE_SCENE, On17264);

            // ---- 鲜花(22300-22305) ----
            RegisterProtocal(Proto.MARRIAGE_FLOWER_ERROR, On22300);
            RegisterProtocal(Proto.MARRIAGE_FLOWER_GIVE, On22301);
            RegisterProtocal(Proto.MARRIAGE_FLOWER_RECORD, On22302);
            RegisterProtocal(Proto.MARRIAGE_FLOWER_INFO, On22303);
            RegisterProtocal(Proto.MARRIAGE_FLOWER_RECEIVED, On22304);
            RegisterProtocal(Proto.MARRIAGE_FLOWER_THANKS, On22305);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            _lastLevel = -1;
            MarriageModel.Instance.Clear();
            base.Dispose();
        }

        // ---------------------------------------------------------------------------------------
        // 登录/升级触发链(B1;对标老端 ts:78-100)
        // ---------------------------------------------------------------------------------------

        /// <summary>config_marriage_constant id=1(MarriageOpenLv,marriage.hrl=170)开启等级,原始值为数字
        /// 字符串;配置未就绪或解析失败时返回 int.MaxValue(视为未达标,避免配置未加载时误发)。</summary>
        private static int GetMarriageOpenLevel() =>
            int.TryParse(MarriageConfigs.GetConstant(1), out int v) ? v : int.MaxValue;

        private async void OnGameStart()
        {
            await MarriageConfigs.EnsureLoaded();
            RequestFlowerInfo(); // 22303 鲜花相关信息,老端恒发(ts:81)
            int needLv = GetMarriageOpenLevel();
            int lv = RoleModel.Instance.Level;
            if (needLv <= lv)
            {
                RequestBiaobaiList(); // 17226 别人求婚/离婚信息(ts:87)
                RequestGiftInfo();    // 17238 礼包信息(ts:88)
                RequestRingInfo();    // 17210 戒指信息(ts:89)
            }
            _lastLevel = lv;
            GameLog.Info("Marriage", "GAME_START 婚姻登录链 22303恒发 needLv={0} lv={1} 达标补发biaobai/gift/ring={2}",
                needLv, lv, needLv <= lv);
        }

        /// <summary>对标老端 role_vo.Bind(CHANGE_LEVEL) 仅在"精确达到开启等级临界值"时补发 17238/17210
        /// (ts:93-100)——本仓无 CHANGE_LEVEL 专属事件,借 EVT_ROLE_INFO_UPDATE + _lastLevel 探测等级变化,
        /// 同 DailyController.OnRoleInfoUpdate 先例。</summary>
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo || role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            if (role.Level == GetMarriageOpenLevel())
            {
                RequestGiftInfo();
                RequestRingInfo();
                GameLog.Info("Marriage", "CHANGE_LEVEL 达开启等级临界 lv={0} 补发gift/ring", role.Level);
            }
        }

        /// <summary>跨天补发 17238 真爱礼包信息(对标老端 ServerTimeModel.DAY_CHANGE→SendFmtToGame(17238),
        /// MarriageController.ts:118-120,无其它副作用)。</summary>
        private void OnServerDayChange()
        {
            RequestGiftInfo();
            GameLog.Info("Marriage", "DAY_CHANGE 跨天补发17238礼包信息");
        }

        // ---------------------------------------------------------------------------------------
        // 征友 Personals(17200-17205)
        // ---------------------------------------------------------------------------------------

        public void RequestPersonalsList(int page) => SendFmt(Proto.MARRIAGE_PERSONALS_LIST, "c", page);
        public void RequestFollow(long roleId, int type) => SendFmt(Proto.MARRIAGE_PERSONALS_FOLLOW, "lc", roleId, type);

        /// <summary>发布征友信息(变长:msg,type,tag_list[u16计数]{tag_id,tag_subid},对标老端 SEND_ISSUE_INFO
        /// 自拼包体)。</summary>
        public void RequestIssue(string msg, int type, IReadOnlyList<(int tagId, int tagSubId)> tagList)
        {
            var fmt = new StringBuilder("sch");
            var args = new List<object> { msg, type, tagList?.Count ?? 0 };
            if (tagList != null)
            {
                foreach ((int tagId, int tagSubId) in tagList)
                {
                    fmt.Append("cc");
                    args.Add(tagId);
                    args.Add(tagSubId);
                }
            }
            SendFmt(Proto.MARRIAGE_PERSONALS_ISSUE, fmt.ToString(), args.ToArray());
        }

        public void RequestRoleDetail(long roleId) => SendFmt(Proto.MARRIAGE_ROLE_DETAIL, "l", roleId);

        private void On17200(NetReader r)
        {
            int code = r.ReadI32();
            int page = r.ReadU8();
            long ownPopularity = r.ReadU32();
            long askFollowTime = r.ReadU32();
            long askFlowerTime = r.ReadU32();
            int lessFreeTimes = r.ReadU8();
            List<MarriageModel.PersonalsEntry> list = r.ReadArray(ReadPersonalsEntry);
            list.Sort((a, b) => b.Time.CompareTo(a.Time)); // 落地前按 time 降序(老端 ts:172-175 player_list.sort)
            if (code == 1)
            {
                var data = new MarriageModel.PersonalsPage
                {
                    Page = page, OwnPopularity = ownPopularity, AskFollowTime = askFollowTime,
                    AskFlowerTime = askFlowerTime, LessFreeTimes = lessFreeTimes,
                };
                data.PlayerList.AddRange(list);
                MarriageModel.Instance.SetPersonalsPage(page, data);
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_PERSONALS_UPDATE, page);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Marriage", "17200 征友大厅 code={0} page={1} count={2}", code, page, list.Count);
        }

        private static MarriageModel.PersonalsEntry ReadPersonalsEntry(NetReader r)
        {
            var e = new MarriageModel.PersonalsEntry
            {
                RoleId = r.ReadU64(), Name = r.ReadString(), Lv = r.ReadU16(), Sex = r.ReadU8(), Vip = r.ReadU32(),
                Career = r.ReadU8(), Turn = r.ReadU8(), IfMarriage = r.ReadU8(), Picture = r.ReadString(),
                PictureVer = r.ReadU32(), IfOnline = r.ReadU8(), Popularity = r.ReadU32(), Msg = r.ReadString(),
                Type = r.ReadU8(), Time = r.ReadU32(), IfFollow = r.ReadU8(), IfFriend = r.ReadU8(), Intimacy = r.ReadU32(),
            };
            e.TagList.AddRange(r.ReadArray(ReadTagEntry));
            e.VipExp = r.ReadU32();
            e.VipHide = r.ReadU8();
            e.IsSupvip = r.ReadU8();
            return e;
        }

        private static MarriageModel.TagEntry ReadTagEntry(NetReader r) => new MarriageModel.TagEntry
        {
            TagId = r.ReadU8(), TagSubId = r.ReadU8(),
        };

        /// <summary>M1 修复(镜像老端 ts:181-202):成功时从大厅桶(page1)找到该 role 的 vo,塞进"我的关注"
        /// 桶(page2)并按 time 降序;page2 桶尚未加载则改发 17200 "c" 2 重拉;page1 未含该 vo 时老端什么也不做
        /// (本端镜像不发事件)。失败只 ShowError,不发 PERSONALS_UPDATE 事件(此前失败也发,已改掉)。</summary>
        private void On17201(NetReader r)
        {
            int code = r.ReadI32();
            long followRoleId = r.ReadU64();
            int type = r.ReadU8();
            if (code == 1)
            {
                MarriageModel.PersonalsPage page1 = MarriageModel.Instance.GetPersonalsPage(1);
                MarriageModel.PersonalsEntry vo = page1?.PlayerList.Find(e => e.RoleId == followRoleId);
                if (vo != null)
                {
                    MarriageModel.PersonalsPage page2 = MarriageModel.Instance.GetPersonalsPage(2);
                    if (page2 != null)
                    {
                        page2.PlayerList.Add(vo);
                        page2.PlayerList.Sort((a, b) => b.Time.CompareTo(a.Time));
                        MarriageModel.Instance.SetPersonalsPage(2, page2);
                        EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_PERSONALS_UPDATE, 2);
                    }
                    else
                    {
                        RequestPersonalsList(2);
                    }
                }
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Marriage", "17201 关注/取消关注 code={0} followRoleId={1} type={2}", code, followRoleId, type);
        }

        private void On17202(NetReader r)
        {
            int code = r.ReadI32();
            int type = r.ReadU8();
            if (code == 1)
            {
                TipsManager.Toast("发布成功");
                RequestPersonalsList(1); // M2 修复:成功后重拉大厅第1页(老端 ts:204-213)
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_PERSONALS_UPDATE, 1);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Marriage", "17202 发布征友 code={0} type={1}", code, type);
        }

        /// <summary>17205 玩家细节(公会),**无 Code 前缀独例**。</summary>
        private void On17205(NetReader r)
        {
            var d = new MarriageModel.RoleDetail { RoleId = r.ReadU64(), GuildId = r.ReadU64(), GuildName = r.ReadString() };
            MarriageModel.Instance.SetRoleDetail(d);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_ROLE_DETAIL_UPDATE);
            GameLog.Info("Marriage", "17205 玩家细节 roleId={0} guildId={1} guildName={2}", d.RoleId, d.GuildId, d.GuildName);
        }

        // ---------------------------------------------------------------------------------------
        // 戒指 Ring(17210-17213)
        // ---------------------------------------------------------------------------------------

        public void RequestRingInfo() => SendFmt(Proto.MARRIAGE_RING_INFO);
        public void RequestRingUnlock() => SendFmt(Proto.MARRIAGE_RING_UNLOCK);
        // 17212 戒指单步提升是死号(老端零发送点),不提供发送方法。
        public void RequestRingUpgradeAll() => SendFmt(Proto.MARRIAGE_RING_UPGRADE_ALL);

        /// <summary>17210 戒指信息。TODO:老端用 config_ring_star(stage@star)自算 ring_combat_power **覆盖**
        /// 服务端字段(CalAttrPower(attr_list)+CalAttrPower(marriage_attr)),本轮先如实落地服务端权威值,
        /// 自算覆盖留后续数据层补(MarriageConfigs.GetRingStar 已备好读表能力)。</summary>
        private void On17210(NetReader r)
        {
            int code = r.ReadI32();
            int stage = r.ReadU8();
            int star = r.ReadU8();
            long prayNum = r.ReadU32();
            long ringCombatPower = r.ReadU32();
            List<MarriageModel.PolishEntry> polishList = r.ReadArray(ReadPolishEntry);
            List<MarriageModel.RingAttrEntry> attrList = r.ReadArray(ReadRingAttrEntry);
            if (code == 1)
            {
                var info = new MarriageModel.RingInfo { Stage = stage, Star = star, PrayNum = prayNum, RingCombatPower = ringCombatPower };
                info.PolishList.AddRange(polishList);
                info.AttrList.AddRange(attrList);
                MarriageModel.Instance.SetRing(info);
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_RING_UPDATE);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Marriage", "17210 戒指信息 code={0} stage={1} star={2} polish={3} attr={4}",
                code, stage, star, polishList.Count, attrList.Count);
        }

        private static MarriageModel.PolishEntry ReadPolishEntry(NetReader r) => new MarriageModel.PolishEntry
        {
            GoodsTypeId = r.ReadU32(), UseNum = r.ReadU16(),
        };

        private static MarriageModel.RingAttrEntry ReadRingAttrEntry(NetReader r) => new MarriageModel.RingAttrEntry
        {
            AttrType = r.ReadU32(), AttrNum = r.ReadU32(),
        };

        private void On17211(NetReader r)
        {
            int code = r.ReadI32();
            int stage = r.ReadU8();
            int star = r.ReadU8();
            long prayNum = r.ReadU32();
            if (code == 1)
            {
                MarriageModel.Instance.ApplyRingUpgrade(stage, star, prayNum);
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_RING_UPDATE);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Marriage", "17211 戒指解锁 code={0} stage={1} star={2} prayNum={3}", code, stage, star, prayNum);
        }

        /// <summary>17212 戒指单步提升——**死号**(老端注册 handler 但零发送点+成功分支全注释)。只解析不消费,
        /// 失败分支镜像老端发 EVT_MARRIAGE_RING_STOP_UPGRADE(对标老端 STOP_RING_UPGRADE)。</summary>
        private void On17212(NetReader r)
        {
            int code = r.ReadI32();
            long goodsTypeId = r.ReadU32();
            int stage = r.ReadU8();
            int star = r.ReadU8();
            long prayNum = r.ReadU32();
            if (code != 1)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_RING_STOP_UPGRADE);
                ShowError(code);
            }
            GameLog.Info("Marriage", "17212 戒指单步提升(死号防御recv,老端零发送点) code={0} goodsTypeId={1} stage={2} star={3}",
                code, goodsTypeId, stage, star);
        }

        private void On17213(NetReader r)
        {
            int code = r.ReadI32();
            int stage = r.ReadU8();
            int star = r.ReadU8();
            long prayNum = r.ReadU32();
            if (code == 1)
            {
                MarriageModel.Instance.ApplyRingUpgrade(stage, star, prayNum);
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_RING_UPDATE);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Marriage", "17213 戒指一键提升 code={0} stage={1} star={2} prayNum={3}", code, stage, star, prayNum);
        }

        // ---------------------------------------------------------------------------------------
        // 求婚/结婚/离婚/秀恩爱(17222-17240)
        // ---------------------------------------------------------------------------------------

        public void RespondPropose(long roleId, int type) => SendFmt(Proto.MARRIAGE_PROPOSE_RESPOND, "lc", roleId, type);
        public void RequestPropose(long roleId, int weddingType, string msg, int ifAa) =>
            SendFmt(Proto.MARRIAGE_PROPOSE_SEND, "lcsc", roleId, weddingType, msg, ifAa);
        /// <summary>B1 修复:登录求婚/离婚信息汇总(C2S 空包)。此前只注册 On17226 收、漏了发送方法,
        /// 该请求-应答号永远收不到推送(老端 ts:87 SendFmtToGame(17226));登录链见 OnGameStart。
        /// ⚠服务端列表空时不回包(mod_marriage.erl:1264-1267),调用方不可等待回包。</summary>
        public void RequestBiaobaiList() => SendFmt(Proto.MARRIAGE_BIAOBAI_LIST);
        public void RequestMyMate() => SendFmt(Proto.MARRIAGE_MATE_INFO);
        public void RequestDivorce(int divorceType) => SendFmt(Proto.MARRIAGE_DIVORCE_SEND, "c", divorceType);
        public void RespondDivorce(int answerType) => SendFmt(Proto.MARRIAGE_DIVORCE_RESPOND, "c", answerType);
        public void RequestDsgtReward(int id) => SendFmt(Proto.MARRIAGE_DSGT_TAKE, "c", id);
        public void RequestBuyGift() => SendFmt(Proto.MARRIAGE_GIFT_BUY);
        public void RequestGiftInfo() => SendFmt(Proto.MARRIAGE_GIFT_INFO);
        public void RequestGiftReward(int countType) => SendFmt(Proto.MARRIAGE_GIFT_TAKE, "c", countType);
        public void RequestAskBuyGift() => SendFmt(Proto.MARRIAGE_GIFT_ASK_BUY);

        /// <summary>17222 推送(无 Code 前缀)。CombatPower **u32 独例**(勿套 17226/17232 的 u64)。
        /// Type:2=求婚/4=离婚协商/5=请求购买礼包(其余类型如实落地,UI 分支留尾包)。</summary>
        private void On17222(NetReader r)
        {
            MarriageModel.ProposeEntry e = ReadProposeCore(r, wide: false);
            MarriageModel.Instance.SetLastPropose(e);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_PROPOSE_PUSH, e.Type);
            GameLog.Info("Marriage", "17222 求婚/结婚/离婚推送 roleId={0} type={1} proposeType={2}", e.RoleId, e.Type, e.ProposeType);
        }

        private void On17223(NetReader r)
        {
            int code = r.ReadI32();
            long roleId = r.ReadU64();
            int type = r.ReadU8();
            if (code == 1)
            {
                // 对标老端成功后重拉伴侣/礼包/戒指三件套。
                RequestMyMate();
                RequestGiftInfo();
                RequestRingInfo();
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_PROPOSE_RESPOND_RESULT, code == 1);
            GameLog.Info("Marriage", "17223 回应求婚 code={0} roleId={1} type={2}", code, roleId, type);
        }

        /// <summary>17224 回应结果推送(无 Code 前缀,双向单播)。老端仅 AnswerType==1 时处理,==2 拒绝无任何
        /// 分支——本端镜像:只在 AnswerType==1 时落地+重拉+发事件。</summary>
        private void On17224(NetReader r)
        {
            long roleId = r.ReadU64();
            int type = r.ReadU8();
            int answerType = r.ReadU8();
            if (answerType == 1)
            {
                MarriageModel.Instance.SetLastAnswerResult(new MarriageModel.AnswerResult { RoleId = roleId, Type = type, AnswerType = answerType });
                MarriageModel.Instance.SetGift(null);
                RequestMyMate();
                RequestGiftInfo();
                RequestRingInfo();
                if (type == 3) // M6 修复:离婚时清空好友亲密度(老端 ts:340-344 fri_vo.intimacy=0)
                {
                    FriendModel.FriendVo friVo = FriendModel.Instance.GetFriendById(roleId);
                    if (friVo != null) FriendModel.Instance.UpdateIntimacy(roleId, 0);
                }
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_ANSWER_PUSH, roleId, type, answerType);
            }
            GameLog.Info("Marriage", "17224 回应结果推送 roleId={0} type={1} answerType={2}", roleId, type, answerType);
        }

        /// <summary>17226 登录求婚/离婚信息汇总(无 Code 前缀)。**必须 ReadArray 读完两个数组保游标**——
        /// biaobai_list(CombatPower u64) + biaobai_answer_list(CombatPower u64),老端只消费前者,本端两者
        /// 都落地(比老端完整无害)。</summary>
        private void On17226(NetReader r)
        {
            List<MarriageModel.ProposeEntry> biaobaiList = r.ReadArray(rr => ReadProposeCore(rr, wide: true));
            List<MarriageModel.BiaobaiAnswerEntry> answerList = r.ReadArray(ReadBiaobaiAnswerEntry);
            MarriageModel.Instance.ApplyBiaobai(biaobaiList, answerList);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_BIAOBAI_UPDATE);
            GameLog.Info("Marriage", "17226 登录表白汇总 biaobai={0} answer={1}(answer_list 老端未消费,本端如实落地)",
                biaobaiList.Count, answerList.Count);
        }

        /// <summary>17222(wide=false,CombatPower u32)/17226 bin_6(wide=true,CombatPower u64)共用读取——
        /// 除 CombatPower 位宽外其余字段序完全相同。</summary>
        private static MarriageModel.ProposeEntry ReadProposeCore(NetReader r, bool wide)
        {
            var e = new MarriageModel.ProposeEntry { RoleId = r.ReadU64(), Name = r.ReadString(), Lv = r.ReadU16() };
            e.CombatPower = wide ? r.ReadU64() : r.ReadU32();
            e.Sex = r.ReadU8(); e.Vip = r.ReadU32(); e.Career = r.ReadU8(); e.Turn = r.ReadU8();
            if (!wide)
            {
                // 17222 独有 Picture/PictureVer(17226 bin_6 无此二字段)。
                e.Picture = r.ReadString();
                e.PictureVer = r.ReadU32();
            }
            e.Type = r.ReadU8(); e.ProposeType = r.ReadU8(); e.Msg = r.ReadString(); e.IfAa = r.ReadU8();
            e.CostList.AddRange(r.ReadArray(ReadCostEntry));
            return e;
        }

        private static MarriageModel.CostEntry ReadCostEntry(NetReader r) => new MarriageModel.CostEntry
        {
            GoodsType = r.ReadU32(), GoodsTypeId = r.ReadU32(), GoodsNum = r.ReadU32(),
        };

        private static MarriageModel.BiaobaiAnswerEntry ReadBiaobaiAnswerEntry(NetReader r) => new MarriageModel.BiaobaiAnswerEntry
        {
            RoleId = r.ReadU64(), Name = r.ReadString(), Lv = r.ReadU16(), CombatPower = r.ReadU64(),
            Sex = r.ReadU8(), Vip = r.ReadU32(), Career = r.ReadU8(), Turn = r.ReadU8(), Type = r.ReadU8(), AnswerType = r.ReadU8(),
        };

        /// <summary>17229 其他信息推送(无 Code 前缀)。Key==1 对应恩爱值(老端 SetLoveNum)。</summary>
        private void On17229(NetReader r)
        {
            List<(int key, long val)> list = r.ReadArray(rr => ((int)rr.ReadU8(), (long)rr.ReadU32()));
            foreach ((int key, long val) kv in list)
            {
                MarriageModel.Instance.SetKeyValue(kv.key, kv.val);
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_KEY_VALUE_UPDATE, kv.key, kv.val);
            }
            GameLog.Info("Marriage", "17229 键值推送 count={0}", list.Count);
        }

        private void On17231(NetReader r)
        {
            int code = r.ReadI32();
            long roleId = r.ReadU64();
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_PROPOSE_SEND_RESULT, code == 1);
            if (code != 1) ShowError(code);
            GameLog.Info("Marriage", "17231 发送求婚 code={0} roleId={1}", code, roleId);
        }

        /// <summary>17232 我的伴侣。CombatPower **u64**。老端 code∈{1,1720012单身,1012} 三码都当成功刷新
        /// (有意逻辑非bug),本端三码同镜像落地。TODO(M7):回写 RoleModel is_marriage/marriage_id/
        /// marriage_name(老端 ts:385-388 role_vo.ChangeVar),待 RoleModel 补婚姻字段后在此接。</summary>
        private void On17232(NetReader r)
        {
            int code = r.ReadI32();
            long roleId = r.ReadU64();
            long combatPower = r.ReadU64();
            FigureProto figure = FigureProto.Read(r);
            int type = r.ReadU8();
            int nowWeddingState = r.ReadU8();
            long anniversaryTime = r.ReadU32();
            long loveNum = r.ReadU32();
            int firstMarriage = r.ReadU8();
            if (code == 1 || code == 1720012 || code == 1012)
            {
                var m = new MarriageModel.MateInfo
                {
                    RoleId = roleId, CombatPower = combatPower, Figure = figure, Type = type,
                    NowWeddingState = nowWeddingState, AnniversaryTime = anniversaryTime, LoveNum = loveNum, FirstMarriage = firstMarriage,
                };
                MarriageModel.Instance.SetMate(m);
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_MATE_UPDATE);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Marriage", "17232 我的伴侣 code={0} roleId={1} combatPower={2} weddingState={3}",
                code, roleId, combatPower, nowWeddingState);
        }

        private void On17234(NetReader r)
        {
            int code = r.ReadI32();
            if (code == 1)
            {
                RequestMyMate();
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_DIVORCE_RESULT, code == 1);
            GameLog.Info("Marriage", "17234 发送离婚 code={0}", code);
        }

        private void On17235(NetReader r)
        {
            int code = r.ReadI32();
            int answerType = r.ReadU8();
            if (code != 1) ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_DIVORCE_RESPOND_RESULT, code == 1, answerType);
            GameLog.Info("Marriage", "17235 回应离婚 code={0} answerType={1}", code, answerType);
        }

        /// <summary>M10:老端成功后 setTimeout 1s 才发 UPDATE_DSGT(等称号数据先回,ts:417-421),本端立即
        /// 发事件;尾包接称号联动消费方时注意此时序差。</summary>
        private void On17236(NetReader r)
        {
            int code = r.ReadI32();
            int id = r.ReadU8();
            if (code == 1)
            {
                MarriageModel.Instance.SetLastDsgt(id);
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_DSGT_UPDATE, id);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Marriage", "17236 领取恩爱称号 code={0} id={1}", code, id);
        }

        /// <summary>17237 购买真爱礼包。老端成功后额外经 ChatModel 发情侣公告私信(BoardMarriager),
        /// 跨模块社交联动,本轮数据层不接,TODO。</summary>
        private void On17237(NetReader r)
        {
            int code = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_GIFT_BUY_RESULT, code == 1);
            if (code != 1) ShowError(code);
            GameLog.Info("Marriage", "17237 购买真爱礼包 code={0}(BoardMarriager私信联动TODO)", code);
        }

        /// <summary>17238 真爱礼包信息(无 Code 前缀)。</summary>
        private void On17238(NetReader r)
        {
            var g = new MarriageModel.GiftInfo { LoveGiftTimeS = r.ReadU32(), LoveGiftTimeO = r.ReadU32() };
            g.GiftState.AddRange(r.ReadArray(ReadGiftStateEntry));
            MarriageModel.Instance.SetGift(g);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_GIFT_INFO_UPDATE);
            GameLog.Info("Marriage", "17238 真爱礼包信息 giftStateN={0}", g.GiftState.Count);
        }

        private static MarriageModel.GiftStateEntry ReadGiftStateEntry(NetReader r) => new MarriageModel.GiftStateEntry
        {
            CountType = r.ReadU8(), State = r.ReadU8(), Time = r.ReadU32(),
        };

        private void On17239(NetReader r)
        {
            int code = r.ReadI32();
            int countType = r.ReadU8();
            List<MarriageModel.RewardEntry> reward = r.ReadArray(ReadRewardEntry);
            if (code == 1)
            {
                MarriageModel.Instance.SetGiftReward(countType, reward);
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_GIFT_TAKE_RESULT, true, countType);
            }
            else
            {
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_GIFT_TAKE_RESULT, false, countType);
                ShowError(code);
            }
            GameLog.Info("Marriage", "17239 领取真爱礼包奖励 code={0} countType={1} rewardN={2}", code, countType, reward.Count);
        }

        /// <summary>ObjectList 单条(u16计数前缀,元素={Type:8,TypeId:32,Num:32},对标 pt:write_object_list)。</summary>
        private static MarriageModel.RewardEntry ReadRewardEntry(NetReader r) => new MarriageModel.RewardEntry
        {
            Type = r.ReadU8(), TypeId = r.ReadU32(), Num = r.ReadU32(),
        };

        private void On17240(NetReader r)
        {
            int code = r.ReadI32();
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_GIFT_ASK_RESULT, code == 1);
            if (code != 1) ShowError(code);
            GameLog.Info("Marriage", "17240 请求对方购买礼包 code={0}", code);
        }

        // ---------------------------------------------------------------------------------------
        // 副本匹配/邀请(17245-17297)——17245 服务端入口已封存,17245/17246 仅防御接收。
        // ---------------------------------------------------------------------------------------

        // 17245(进入/退出匹配)是死号发送:服务端 handle(17245) 整段注释(pp_marriage.erl:1598-1607),
        // 死号严禁发,故不提供 RequestDunMatch(轮16三镜头验收裁决1;On17245 防御 recv 保留)。
        public void RequestDunInviteBuy(int dunId) => SendFmt(Proto.MARRIAGE_DUN_INVITE_BUY, "i", dunId);
        /// <summary>agree:1=同意/2=拒绝。</summary>
        public void RespondDunInviteBuy(int agree, int dunId) => SendFmt(Proto.MARRIAGE_DUN_INVITE_RESPOND, "ci", agree, dunId);

        private void On17245(NetReader r)
        {
            int code = r.ReadI32();
            int type = r.ReadU8();
            int dunId = r.ReadI32();
            if (code == 1)
            {
                MarriageModel.Instance.SetMatchState(type == 1, dunId);
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_MATCH_RESULT, type, dunId);
            }
            else
            {
                ShowError(code);
            }
            GameLog.Info("Marriage", "17245 进退匹配 code={0} type={1} dunId={2}(死链:服务端handler已注释/UI模块从未加载)", code, type, dunId);
        }

        /// <summary>17246 匹配结果。⚠与 r16 侦察报告"无Code"结论不同——ClientProtocol.json+老端 on17246 实读
        /// scmd.code,本端订正为带 Code(见 Proto.cs 注释)。死链(服务端 17246 推送点全部无触发源,
        /// lib_marriage.erl:2038-2129/mod_marriage_match.erl:219;UI 层 MarriageMatchTipsView 模块也从未
        /// 加载),数据层仍照接解析落地。enter_time:老端把该字段做 getServerTime()+enter_time 绝对化
        /// (ts:479),本端落原始相对值,尾包接 UI 时须换算(死链号,本轮不引入时钟依赖)。</summary>
        private void On17246(NetReader r)
        {
            int code = r.ReadI32();
            List<MarriageModel.MatchResultEntry> list = r.ReadArray(ReadMatchResultEntry);
            int enterTime = r.ReadU8();
            if (code == 1)
            {
                var result = new MarriageModel.MatchResult { EnterTime = enterTime };
                result.List.AddRange(list);
                MarriageModel.Instance.SetMatchResult(result);
                EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_MATCH_PUSH);
            }
            GameLog.Info("Marriage", "17246 匹配结果 code={0} count={1} enterTime={2}(死链:推送点无触发源/UI模块从未加载)",
                code, list.Count, enterTime);
        }

        private static MarriageModel.MatchResultEntry ReadMatchResultEntry(NetReader r) => new MarriageModel.MatchResultEntry
        {
            Type = r.ReadU8(), RoleId = r.ReadU64(), Figure = FigureProto.Read(r), Power = r.ReadU64(),
        };

        /// <summary>17263 进入婚礼场景错误出口(轮22 族错误出口批;对标老端 BanquetController.ts:202-207
        /// On17263:code!=1→ErrorCodeShow,无其它副作用。成功分支[check_all 通过]服务端不回本号,
        /// 走 mod_marriage_wedding_mgr:enter_wedding 场景切换,不臆造成功回调)。</summary>
        private void On17263(NetReader r)
        {
            int code = r.ReadI32();
            if (code != 1) ShowError(code);
            GameLog.Info("Marriage", "17263 进入婚礼场景 code={0}", code);
        }

        /// <summary>17264 离开婚礼场景错误出口(轮22 族错误出口批;对标老端 BanquetController.ts:209-214
        /// On17264:code!=1→ErrorCodeShow,无其它副作用。成功/失败均回本号,老端成功分支同样无额外动作)。</summary>
        private void On17264(NetReader r)
        {
            int code = r.ReadI32();
            if (code != 1) ShowError(code);
            GameLog.Info("Marriage", "17264 离开婚礼场景 code={0}", code);
        }

        private void On17295(NetReader r)
        {
            int code = r.ReadI32();
            if (code == 1) TipsManager.Toast("已向ta发起请求");
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_DUN_INVITE_BUY_RESULT, code == 1);
            GameLog.Info("Marriage", "17295 邀请伴侣购买副本次数 code={0}", code);
        }

        /// <summary>17296 收到副本次数购买邀请推送(无 Code 前缀)。</summary>
        private void On17296(NetReader r)
        {
            var d = new MarriageModel.DunInvite { RoleId = r.ReadU64(), RoleName = r.ReadString(), DunId = r.ReadI32() };
            MarriageModel.Instance.SetLastDunInvite(d);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_DUN_INVITE_PUSH, d.RoleId, d.DunId);
            GameLog.Info("Marriage", "17296 收到副本次数购买邀请 roleId={0} roleName={1} dunId={2}", d.RoleId, d.RoleName, d.DunId);
        }

        /// <summary>17297 同意/拒绝购买副本次数推送(无 Code 前缀)。agree/dun_id 是请求回声;
        /// role_id/role_name 是回应方附加信息,非请求回声。</summary>
        private void On17297(NetReader r)
        {
            int agree = r.ReadU8();
            int dunId = r.ReadI32();
            long roleId = r.ReadU64();
            string roleName = r.ReadString();
            if (agree == 1) TipsManager.Toast("对方已购买");
            else if (agree == 2) TipsManager.Toast("对方已拒绝");
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_DUN_INVITE_RESPOND_PUSH, agree, dunId);
            GameLog.Info("Marriage", "17297 同意/拒绝购买副本次数 agree={0} dunId={1} roleId={2} roleName={3}", agree, dunId, roleId, roleName);
        }

        // ---------------------------------------------------------------------------------------
        // 鲜花(22300-22305)
        // ---------------------------------------------------------------------------------------

        /// <summary>赠送鲜花(C2S "lhihc" role_id,server_id,goods_type_id,num,anonymous)。</summary>
        public void GiveFlower(long roleId, int serverId, int goodsTypeId, int num, int anonymous) =>
            SendFmt(Proto.MARRIAGE_FLOWER_GIVE, "lhihc", roleId, serverId, goodsTypeId, num, anonymous);
        public void RequestFlowerRecord() => SendFmt(Proto.MARRIAGE_FLOWER_RECORD);
        public void RequestFlowerInfo() => SendFmt(Proto.MARRIAGE_FLOWER_INFO);
        /// <summary>感谢收花者(C2S "l" id;老端两处调用点分别传 role_id 与记录 id,语义由调用方决定)。</summary>
        public void ThanksFlower(long id) => SendFmt(Proto.MARRIAGE_FLOWER_THANKS, "l", id);

        private void On22300(NetReader r)
        {
            int code = r.ReadI32();
            if (code != 1) ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_FLOWER_ERROR, code);
            GameLog.Info("Marriage", "22300 鲜花错误码 code={0}", code);
        }

        /// <summary>M4 修复(镜像老端 ts:526-531 on22301 成功分支):①好友亲密度
        /// fri_vo.intimacy += fdata.intimacy*goods_num(FriendModel 已有 GetFriendById/UpdateIntimacy,
        /// 好友桶未命中则跳过,老端同);②名誉值 model.AddRoleFame(fdata.fame*goods_num)(即
        /// MarriageModel.Flower.Fame 自增,见 MarriageModel.AddFlowerFame)。</summary>
        private void On22301(NetReader r)
        {
            int code = r.ReadI32();
            long receiveId = r.ReadU64();
            int receiveServerId = r.ReadU16();
            long goodsId = r.ReadU32();
            int goodsNum = r.ReadU16();
            if (code == 1)
            {
                TipsManager.Toast("赠送成功");
                MarriageConfigs.FlowerToolRow fdata = MarriageConfigs.GetFlowerTool((int)goodsId);
                if (fdata != null)
                {
                    FriendModel.FriendVo friVo = FriendModel.Instance.GetFriendById(receiveId);
                    if (friVo != null) FriendModel.Instance.UpdateIntimacy(receiveId, friVo.Intimacy + fdata.Intimacy * goodsNum);
                    MarriageModel.Instance.AddFlowerFame(fdata.Fame * goodsNum);
                }
            }
            else if (code != 1020002) // 操作太频繁的错误码老端不展示
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_FLOWER_GIVE_RESULT, code == 1, receiveId, goodsId);
            GameLog.Info("Marriage", "22301 赠送鲜花 code={0} receiveId={1} goodsId={2} num={3}", code, receiveId, goodsId, goodsNum);
        }

        /// <summary>22302 收礼记录(无 Code 前缀,一次性全量下发,无分页)。</summary>
        private void On22302(NetReader r)
        {
            List<MarriageModel.FlowerRecordEntry> list = r.ReadArray(ReadFlowerRecordEntry);
            MarriageModel.Instance.ApplyFlowerRecords(list);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_FLOWER_RECORD_UPDATE);
            GameLog.Info("Marriage", "22302 收礼记录 count={0}", list.Count);
        }

        private static MarriageModel.FlowerRecordEntry ReadFlowerRecordEntry(NetReader r) => new MarriageModel.FlowerRecordEntry
        {
            Id = r.ReadU64(), SenderId = r.ReadU64(), SenderName = r.ReadString(), ServerId = r.ReadU16(), ServerNum = r.ReadU16(),
            GoodsId = r.ReadU32(), GoodsNum = r.ReadU16(), Anonymous = r.ReadU8(), IsThanks = r.ReadU8(), Time = r.ReadU32(),
        };

        /// <summary>22303 鲜花相关信息(无 Code 前缀)。</summary>
        private void On22303(NetReader r)
        {
            var f = new MarriageModel.FlowerInfo { FlowerNum = r.ReadU32(), Charm = r.ReadU32(), Fame = r.ReadU32() };
            MarriageModel.Instance.SetFlowerInfo(f);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_FLOWER_INFO_UPDATE);
            GameLog.Info("Marriage", "22303 鲜花信息 flowerNum={0} charm={1} fame={2}", f.FlowerNum, f.Charm, f.Fame);
        }

        /// <summary>22304 收到的鲜花通知(无 Code 前缀)。</summary>
        private void On22304(NetReader r)
        {
            var f = new MarriageModel.FlowerReceived
            {
                SenderId = r.ReadU64(), SenderFigure = FigureProto.Read(r), ServerId = r.ReadU16(),
                ServerNum = r.ReadU16(), GoodsId = r.ReadU32(), GoodsNum = r.ReadU16(),
            };
            MarriageModel.Instance.SetLastFlowerReceived(f);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_FLOWER_RECEIVED, f.SenderId, f.GoodsId);
            GameLog.Info("Marriage", "22304 收到鲜花通知 senderId={0} goodsId={1} num={2}", f.SenderId, f.GoodsId, f.GoodsNum);
        }

        /// <summary>M5:老端 on22305 无条件 toast"感谢成功"、不判 code(ts:561-565),此处 code==1 才 toast
        /// 是本端单向加强;且服务端 22305(感谢)处理恒回 Code=1(lib_flower.erl:111-123),else 分支实际死路。</summary>
        private void On22305(NetReader r)
        {
            int code = r.ReadI32();
            long id = r.ReadU64();
            if (code == 1) TipsManager.Toast("感谢成功");
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_MARRIAGE_FLOWER_THANKS_RESULT, code == 1, id);
            GameLog.Info("Marriage", "22305 感谢收花者 code={0} id={1}", code, id);
        }
    }
}
