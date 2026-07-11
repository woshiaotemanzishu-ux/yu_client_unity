using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友协议控制器(对标老客户端 commonController/FriendController.ts 好友段,自动循环 轮7)。
    /// 14000-14015/14099(好友/仇人/黑名单、推荐/搜索、申请、关系操作、右键菜单、在线/亲密度增量推送),
    /// 外加 19501/19502(资料卡请求 + module_id=1 基础装备完整卡片)。
    ///
    /// 跳过(按规格§0):14011(单点查双方关系,老端未实现)、14012(号未分配,DEAD)、
    /// 14016/14017(劲敌模块,服务端无 handler 路由)。19503-19512 只在 Proto.cs 加常量注释,不在此注册。
    ///
    /// 守卫门槛(pp_relationship.erl):除 14000+Type=3、14010、14007+Type∈{2,3} 外都要求好友模块开放等级,
    /// 服务端未过校验静默丢包不回——本端不复刻该等级预判(协议层交给服务端权威裁决,前端只管发)。
    ///
    /// **ControllerHub 未注册**(按纪律不碰该文件):需要在 ControllerHub.ALL 里补一行
    /// <c>FriendController.Instance,</c>(建议紧邻 FriendInviteController.Instance 之后),否则 GAME_START 不会
    /// 自动拉取好友/申请列表——已在本轮汇报中列出。
    /// </summary>
    public sealed class FriendController : BaseController
    {
        public static readonly FriendController Instance = new FriendController();
        private FriendController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.FRIEND_LIST, On14000);
            RegisterProtocal(Proto.FRIEND_RECOMMEND, On14001);
            RegisterProtocal(Proto.FRIEND_SEARCH, On14002);
            RegisterProtocal(Proto.FRIEND_ADD_APPLY, On14003);
            RegisterProtocal(Proto.FRIEND_APPLY_ONE_CLICK, On14004);
            RegisterProtocal(Proto.FRIEND_APPLY_ONE, On14005);
            RegisterProtocal(Proto.FRIEND_APPLY_LIST, On14006);
            RegisterProtocal(Proto.FRIEND_OPERATE, On14007);
            RegisterProtocal(Proto.FRIEND_APPLY_PUSH, On14008);
            RegisterProtocal(Proto.FRIEND_ONLINE_PUSH, On14009);
            RegisterProtocal(Proto.FRIEND_MENU_DATA, On14010);
            RegisterProtocal(Proto.FRIEND_LIST_DELTA_UPSERT, On14013);
            RegisterProtocal(Proto.FRIEND_LIST_DELTA_REMOVE, On14014);
            RegisterProtocal(Proto.FRIEND_INTIMACY_PUSH, On14015);
            RegisterProtocal(Proto.FRIEND_ERROR, On14099);
            RegisterProtocal(Proto.LOOKOVER_REQUEST, On19501);
            RegisterProtocal(Proto.LOOKOVER_BASE_EQUIP, On19502);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            FriendModel.Instance.Reset();
            base.Dispose();
        }

        // 对标老端 GAME_START:自动拉好友列表(type=1)+ 申请列表。
        private void OnGameStart()
        {
            FriendModel.Instance.Reset();
            RequestFriendList(FriendModel.TYPE_FRIEND);
            RequestApplyList();
        }

        // =====================================================================================
        // 发送侧 API
        // =====================================================================================

        /// <summary>14000 请求某关系桶(1好友/2仇人/3黑名单)。</summary>
        public void RequestFriendList(int type) => SendFmt(Proto.FRIEND_LIST, "c", type);

        /// <summary>14001 请求推荐列表(0默认首刷/1换一批,服务端 10s CD)。</summary>
        public void RequestRecommend(int type = 0) => SendFmt(Proto.FRIEND_RECOMMEND, "c", type);

        /// <summary>14002 按昵称搜索玩家(客户端预校验:名称非空)。</summary>
        public void SearchPlayer(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                GameLog.Info("Friend", "SearchPlayer 拦截:名称为空");
                return;
            }
            SendFmt(Proto.FRIEND_SEARCH, "s", roleName);
        }

        /// <summary>14003 发送加好友申请(对标老端 request_add_friend)。role_id==0 时(假人填充位强制置0)
        /// 仍然照发——这是老端一个可疑的边界行为(服务端大概率静默忽略/报错),原样保留不纠正,见规格§0注释。
        /// 自己/假人两处前端拦截由 UI 层(FriendAddPopItem)负责,此处只负责发包与记录 AddRecommendId。</summary>
        public void AddFriendApply(long beAskId)
        {
            FriendModel.Instance.AddRecommendId = beAskId;
            SendFmt(Proto.FRIEND_ADD_APPLY, "l", beAskId);
        }

        /// <summary>14004 一键处理申请(0拒绝/1接受)。</summary>
        public void OneClickApply(int responseType)
        {
            FriendModel.Instance.LastApplyType = responseType;
            SendFmt(Proto.FRIEND_APPLY_ONE_CLICK, "c", responseType);
        }

        /// <summary>14005 单条处理申请(0拒绝/1接受/2拉黑)。</summary>
        public void OneApply(long askId, int responseType)
        {
            FriendModel.Instance.LastApplyType = responseType;
            FriendModel.Instance.LastOneApplyId = askId;
            SendFmt(Proto.FRIEND_APPLY_ONE, "lc", askId, responseType);
        }

        /// <summary>14006 请求申请列表。</summary>
        public void RequestApplyList() => SendFmt(Proto.FRIEND_APPLY_LIST);

        /// <summary>14007 好友关系操作(1删好友/2拉黑/3取消拉黑/4加仇人/5移除仇人)。
        /// type 4/5 前端无 UI 入口,只保留 API(照规格§0)。</summary>
        public void FriendsOperate(int type, long roleId)
        {
            FriendModel.Instance.LastOperateType = type;
            SendFmt(Proto.FRIEND_OPERATE, "cl", type, roleId);
        }

        /// <summary>14010 查看玩家交互菜单(自身/role_id=0 拦截 + 800ms 节流,对标老端 request_menu_data)。</summary>
        public void RequestMenuData(long roleId)
        {
            long selfId = RoleModel.Instance.RoleId;
            if (!FriendModel.Instance.ShouldRequestMenu(roleId, selfId))
            {
                GameLog.Info("Friend", "RequestMenuData 拦截/节流 roleId={0}", roleId);
                return;
            }
            FriendModel.Instance.TouchMenuThrottle(roleId);
            SendFmt(Proto.FRIEND_MENU_DATA, "l", roleId);
        }

        /// <summary>19501 查看资料卡(申请信号,server_id=0 表示同服)。moduleId 默认1=基础装备。</summary>
        public void RequestPlayerCard(long roleId, int moduleId = 1, int serverId = 0)
        {
            SendFmt(Proto.LOOKOVER_REQUEST, "hlh", serverId, roleId, moduleId);
        }

        // =====================================================================================
        // 接收侧
        // =====================================================================================

        // 14000: Type:c, Len:h, RelaList[item_to_bin_0: RoleId:l,Name:s,Career:c,Sex:c,Turn:c,Lv:h,
        //   Vip:c,VipHide:c,Picture:s,PicVer:i,Combat:l,OnlineFlag:c,Intimacy:i,MarriageType:c,BlockId:i,
        //   HouseId:i,HouseLv:h,IsSupvip:c,LastChatTime:i,OfflineTime:i,AddTime:i,DressList[h+{c,i}]]
        private void On14000(NetReader r)
        {
            int type = r.ReadU8();
            int count = r.ReadU16();
            var list = new List<FriendModel.FriendVo>(count);
            for (int i = 0; i < count; i++) list.Add(ReadFriendVoAB(r));
            FriendModel.Instance.SetFriendData(type, list);
            GameLog.Info("Friend", "14000 type={0} count={1}", type, count);
        }

        // 14001: Code:i, Len:h, RecommendedList[item_to_bin_2: RoleId:l,Name:s,Career:c,Sex:c,Turn:c,Lv:h,
        //   Vip:c,VipHide:c,Picture:s,PicVer:i,Combat:l,OnlineFlag:c,IsSupvip:c]
        private void On14001(NetReader r)
        {
            int code = (int)r.ReadU32();
            int count = r.ReadU16();
            var list = new List<FriendModel.RecommendVo>(count);
            for (int i = 0; i < count; i++) list.Add(ReadRecommendVo(r));
            if (code == 1)
            {
                FriendModel.Instance.SetRecommendData(list);
            }
            else
            {
                GameLog.Info("Friend", "14001 code={0}(非1,老端 ErrorCodeShow)", code);
            }
        }

        // 14002: Code:i,RoleId:l,Name:s,Career:c,Sex:c,Turn:c,Lv:h,Vip:c,VipHide:c,Picture:s,PicVer:i,
        //   Combat:l,OnlineFlag:c(单个对象)
        private void On14002(NetReader r)
        {
            int code = (int)r.ReadU32();
            var vo = new FriendModel.RecommendVo
            {
                RoleId = (long)r.ReadU64(),
                Name = r.ReadString(),
                Career = r.ReadU8(),
                Sex = r.ReadU8(),
                Turn = r.ReadU8(),
                Lv = r.ReadU16(),
                Vip = r.ReadU8(),
                VipHide = r.ReadU8(),
                Picture = r.ReadString(),
                PictureVer = (int)r.ReadU32(),
                Combat = (long)r.ReadU64(),
                OnlineFlag = r.ReadU8(),
            };
            if (code == 1)
            {
                FriendModel.Instance.SetCheckPlayerData(vo);
            }
            else
            {
                FriendModel.Instance.CleanRecommendList();
                GameLog.Info("Friend", "14002 search code={0}(失败)", code);
            }
        }

        private void On14003(NetReader r)
        {
            int code = (int)r.ReadU32();
            if (code == 1)
            {
                FriendModel.Instance.MarkRecommendApplied();
                GameLog.Info("Friend", "14003 申请成功");
            }
            else
            {
                GameLog.Info("Friend", "14003 code={0}(失败)", code);
            }
        }

        private void On14004(NetReader r)
        {
            int code = (int)r.ReadU32();
            FriendModel.Instance.ResetApplyList();
            if (FriendModel.Instance.LastApplyType == 1) RequestFriendList(FriendModel.TYPE_FRIEND);
            GameLog.Info("Friend", "14004 一键处理 code={0}(无视 code 清空申请列表)", code);
        }

        private void On14005(NetReader r)
        {
            int code = (int)r.ReadU32();
            FriendModel.Instance.RemoveOneApply();
            if (FriendModel.Instance.LastApplyType == 1) RequestFriendList(FriendModel.TYPE_FRIEND);
            GameLog.Info("Friend", "14005 单条处理 code={0}", code);
        }

        // 14006: Len:h, AskList[item_to_bin_3: RoleId:l,Name:s,Career:c,Turn:c,Lv:h,Picture:s,PicVer:i,Combat:l,AddTime:i]
        private void On14006(NetReader r)
        {
            int count = r.ReadU16();
            var list = new List<FriendModel.ApplyVo>(count);
            for (int i = 0; i < count; i++) list.Add(ReadApplyVo(r));
            FriendModel.Instance.SetApplyList(list);
            GameLog.Info("Friend", "14006 申请列表 count={0}", count);
        }

        // 14007: Type:c, Code:i —— 命令+被动刷新模式:先无条件按记录的 type 重拉对应关系列表(对标老端
        // flushOperationtView 的怪癖映射:操作 type==2(拉黑) 时刷新桶反而用 1(好友),其余按原样;
        // 原样保留老端这一行逻辑,不纠正,避免引入与老端不一致的"修正"。
        private void On14007(NetReader r)
        {
            int type = r.ReadU8();
            int code = (int)r.ReadU32();
            int lastType = FriendModel.Instance.LastOperateType;
            if (lastType != 0)
            {
                int refreshType = lastType == 2 ? 1 : lastType;
                RequestFriendList(refreshType);
                FriendModel.Instance.LastOperateType = 0;
            }
            GameLog.Info("Friend", "14007 关系操作 type={0} code={1}", type, code);
        }

        // 14008(push): RoleId:l,Name:s,Career:c,Turn:c,Lv:h,Picture:s,PicVer:i,Combat:l,AddTime:i(无sex)
        private void On14008(NetReader r)
        {
            var vo = ReadApplyVo(r);
            FriendModel.Instance.AddApply(vo);
            GameLog.Info("Friend", "14008 新申请推送 roleId={0} name={1}", vo.RoleId, vo.Name);
        }

        // 14009(push): RoleId:l,Name:s,RelaType:c,OnlineFlag:c,Timestamp:i
        private void On14009(NetReader r)
        {
            long roleId = (long)r.ReadU64();
            r.ReadString(); // Name(桶内已有,忽略;仅推送带回不覆盖名字字段)
            int relaType = r.ReadU8();
            int onlineFlag = r.ReadU8();
            long timestamp = r.ReadU32();
            FriendModel.Instance.UpdateOnline(relaType, roleId, onlineFlag, timestamp);
            GameLog.Info("Friend", "14009 在线状态 roleId={0} relaType={1} online={2}", roleId, relaType, onlineFlag);
        }

        // 14010: Code:i,RoleId:l,Figure(FigureProto),Rela:c,TeamId:i
        private void On14010(NetReader r)
        {
            int code = (int)r.ReadU32();
            long roleId = (long)r.ReadU64();
            FigureProto figure = FigureProto.Read(r);
            int rela = r.ReadU8();
            int teamId = (int)r.ReadU32();
            if (code == 1)
            {
                FriendModel.Instance.SetMenuData(roleId, figure, rela, teamId);
            }
            GameLog.Info("Friend", "14010 菜单数据 code={0} roleId={1} rela={2}", code, roleId, rela);
        }

        // 14013(push): Len:h, UpdateList[item_to_bin_4: Type:c, Len:h, RoleList[item_to_bin_5,VipHide/Vip 顺序与14000相反]]
        private void On14013(NetReader r)
        {
            int groupCount = r.ReadU16();
            for (int g = 0; g < groupCount; g++)
            {
                int type = r.ReadU8();
                int count = r.ReadU16();
                var list = new List<FriendModel.FriendVo>(count);
                for (int i = 0; i < count; i++) list.Add(ReadFriendVoBA(r));
                FriendModel.Instance.UpsertFriendList(type, list);
            }
            GameLog.Info("Friend", "14013 社交增量插入/覆盖 groups={0}", groupCount);
        }

        // 14014(push): Len:h, UpdateList[item_to_bin_7: Type:c, Len:h, RoleIds[item_to_bin_8: RoleId:l]]
        private void On14014(NetReader r)
        {
            int groupCount = r.ReadU16();
            for (int g = 0; g < groupCount; g++)
            {
                int type = r.ReadU8();
                int count = r.ReadU16();
                var ids = new List<long>(count);
                for (int i = 0; i < count; i++) ids.Add((long)r.ReadU64());
                FriendModel.Instance.RemoveFriendList(type, ids);
            }
            GameLog.Info("Friend", "14014 社交增量移除 groups={0}", groupCount);
        }

        // 14015(push): RoleId:l, Intimacy:i
        private void On14015(NetReader r)
        {
            long roleId = (long)r.ReadU64();
            int intimacy = (int)r.ReadU32();
            FriendModel.Instance.UpdateIntimacy(roleId, intimacy);
            GameLog.Info("Friend", "14015 亲密度 roleId={0} intimacy={1}", roleId, intimacy);
        }

        // 14099(push/兜底): Code:i
        private void On14099(NetReader r)
        {
            int code = (int)r.ReadU32();
            if (code != 0) EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_ERROR, code);
            GameLog.Info("Friend", "14099 通用错误码 code={0}", code);
        }

        // 19501: 仅错误码回执(成功路径服务端不回包,真正数据靠随后的 195xx 推送)。
        private void On19501(NetReader r)
        {
            int code = (int)r.ReadU32();
            GameLog.Info("Friend", "19501 资料卡申请回执 code={0}(0=成功不回包路径外的默认值)", code);
        }

        // 19502(module_id=1 基础装备,"完整角色卡"): ServerId:h,RoleId:l,Combat:l,AchvStage:h,Figure,
        //   EquipList[h+{GoodsId:l,TypeId:i,Cell:h,Color:c,Stren:h,Star:c,Stage:h,Level:h,GodLevel:h}],
        //   MagicCircle[h+{Lv:c,IsOpen:c,FreeFlag:c,EndTime:i}], FairyList[h+{Type:h,IsActive:c}]
        private void On19502(NetReader r)
        {
            var card = new FriendModel.PlayerCard
            {
                ServerId = r.ReadU16(),
                RoleId = (long)r.ReadU64(),
                Combat = (long)r.ReadU64(),
                AchvStage = r.ReadU16(),
                Figure = FigureProto.Read(r),
            };

            int equipCount = r.ReadU16();
            for (int i = 0; i < equipCount; i++)
            {
                card.EquipList.Add(new FriendModel.EquipCardItem
                {
                    GoodsId = (long)r.ReadU64(),
                    TypeId = (int)r.ReadU32(),
                    Cell = r.ReadU16(),
                    Color = r.ReadU8(),
                    Stren = r.ReadU16(),
                    Star = r.ReadU8(),
                    Stage = r.ReadU16(),
                    Level = r.ReadU16(),
                    GodLevel = r.ReadU16(),
                });
            }

            int magicCount = r.ReadU16();
            for (int i = 0; i < magicCount; i++)
            {
                card.MagicCircle.Add(new FriendModel.MagicCircleItem
                {
                    Lv = r.ReadU8(),
                    IsOpen = r.ReadU8(),
                    FreeFlag = r.ReadU8(),
                    EndTime = (int)r.ReadU32(),
                });
            }

            int fairyCount = r.ReadU16();
            for (int i = 0; i < fairyCount; i++)
            {
                card.FairyList.Add(new FriendModel.FairyItem { Type = r.ReadU16(), IsActive = r.ReadU8() });
            }

            FriendModel.Instance.SetPlayerCard(card);
            GameLog.Info("Friend", "19502 资料卡(基础装备) roleId={0} combat={1} equip={2}", card.RoleId, card.Combat, equipCount);
        }

        // ---- 复合结构读取辅助 ----

        // item_to_bin_0(14000):Vip 在前、VipHide 在后。
        private static FriendModel.FriendVo ReadFriendVoAB(NetReader r) => ReadFriendVo(r, vipFirst: true);
        // item_to_bin_5(14013):VipHide 在前、Vip 在后(服务端字段序与14000相反,原样对标)。
        private static FriendModel.FriendVo ReadFriendVoBA(NetReader r) => ReadFriendVo(r, vipFirst: false);

        private static FriendModel.FriendVo ReadFriendVo(NetReader r, bool vipFirst)
        {
            var vo = new FriendModel.FriendVo
            {
                RoleId = (long)r.ReadU64(),
                Name = r.ReadString(),
                Career = r.ReadU8(),
                Sex = r.ReadU8(),
                Turn = r.ReadU8(),
                Lv = r.ReadU16(),
            };
            if (vipFirst) { vo.Vip = r.ReadU8(); vo.VipHide = r.ReadU8(); }
            else { vo.VipHide = r.ReadU8(); vo.Vip = r.ReadU8(); }
            vo.Picture = r.ReadString();
            vo.PictureVer = (int)r.ReadU32();
            vo.Combat = (long)r.ReadU64();
            vo.OnlineFlag = r.ReadU8();
            vo.Intimacy = (int)r.ReadU32();
            vo.MarriageType = r.ReadU8();
            vo.BlockId = (int)r.ReadU32();
            vo.HouseId = (int)r.ReadU32();
            vo.HouseLv = r.ReadU16();
            vo.IsSupvip = r.ReadU8();
            vo.LastChatTime = (int)r.ReadU32();
            vo.OfflineTime = (int)r.ReadU32();
            vo.AddTime = (int)r.ReadU32();
            int dressCount = r.ReadU16();
            for (int i = 0; i < dressCount; i++)
            {
                int dressType = r.ReadU8();
                int dressId = (int)r.ReadU32();
                vo.DressList.Add((dressType, dressId));
            }
            return vo;
        }

        private static FriendModel.RecommendVo ReadRecommendVo(NetReader r)
        {
            return new FriendModel.RecommendVo
            {
                RoleId = (long)r.ReadU64(),
                Name = r.ReadString(),
                Career = r.ReadU8(),
                Sex = r.ReadU8(),
                Turn = r.ReadU8(),
                Lv = r.ReadU16(),
                Vip = r.ReadU8(),
                VipHide = r.ReadU8(),
                Picture = r.ReadString(),
                PictureVer = (int)r.ReadU32(),
                Combat = (long)r.ReadU64(),
                OnlineFlag = r.ReadU8(),
                IsSupvip = r.ReadU8(),
            };
        }

        private static FriendModel.ApplyVo ReadApplyVo(NetReader r)
        {
            return new FriendModel.ApplyVo
            {
                RoleId = (long)r.ReadU64(),
                Name = r.ReadString(),
                Career = r.ReadU8(),
                Turn = r.ReadU8(),
                Lv = r.ReadU16(),
                Picture = r.ReadString(),
                PictureVer = (int)r.ReadU32(),
                Combat = (long)r.ReadU64(),
                AddTime = (int)r.ReadU32(),
            };
        }
    }
}
