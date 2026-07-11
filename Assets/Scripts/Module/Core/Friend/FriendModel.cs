using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友数据层(对标老客户端 commonModel/FriendModel.ts,自动循环 轮7)。
    /// 好友/仇人/黑名单三桶(<see cref="TYPE_FRIEND"/>/<see cref="TYPE_ENEMY"/>/<see cref="TYPE_BLACKLIST"/>)、
    /// 推荐列表(含搜索结果与"假人"填充位)、申请列表、右键菜单数据缓存(800ms 节流)、资料卡缓存。
    /// 事件走 <see cref="GlobalEvent"/>(EVT_FRIEND_*),仅存数据,不发协议(协议侧见 FriendController)。
    /// </summary>
    public sealed class FriendModel
    {
        public static readonly FriendModel Instance = new FriendModel();
        private FriendModel() { }

        // ----- 关系类型(对标老端 FRIEND_TYPE) -----
        public const int TYPE_FRIEND = 1;
        public const int TYPE_ENEMY = 2;
        public const int TYPE_BLACKLIST = 3;

        /// <summary>菜单/资料卡 800ms 节流窗口(对标老端 request_menu_data 节流缓存)。</summary>
        private const long MenuThrottleMs = 800;
        /// <summary>推荐列表空且非手动搜索时的"假人"填充数量区间(对标老端 dummyPlayerList,2~5个)。</summary>
        private static readonly int[] DressListEmpty = System.Array.Empty<int>();

        // ===================================================================================
        // 好友/仇人/黑名单三桶(14000 全量 / 14013 增量插入覆盖 / 14014 增量移除 / 14009 在线 / 14015 亲密度)
        // ===================================================================================

        public sealed class FriendVo
        {
            public long RoleId;
            public string Name = "";
            public int Career;
            public int Sex;
            public int Turn;
            public int Lv;
            public int Vip;
            public int VipHide;
            public string Picture = "";
            public int PictureVer;
            public long Combat;
            public int OnlineFlag;
            public int Intimacy;
            public int MarriageType;
            public int BlockId;
            public int HouseId;
            public int HouseLv;
            public int IsSupvip;
            public int LastChatTime;
            public int OfflineTime;
            public int AddTime;
            public List<(int DressType, int DressId)> DressList = new List<(int, int)>();
        }

        private static readonly List<FriendVo> EmptyFriendList = new List<FriendVo>();
        private readonly Dictionary<int, List<FriendVo>> _friendDic = new Dictionary<int, List<FriendVo>>();

        /// <summary>取某关系桶(对标老端 getFriendData;is_sort 未实现——排序依赖聊天未读数,交给消费方按需自排)。</summary>
        public IReadOnlyList<FriendVo> GetFriendData(int type) =>
            _friendDic.TryGetValue(type, out List<FriendVo> list) ? list : EmptyFriendList;

        public FriendVo GetFriendById(long roleId)
        {
            if (roleId == 0) return null;
            foreach (FriendVo vo in GetFriendData(TYPE_FRIEND))
                if (vo.RoleId == roleId) return vo;
            return null;
        }

        /// <summary>在线人数统计(对标老端 getOnlineDataNum)。</summary>
        public int GetOnlineDataNum(int type)
        {
            int num = 0;
            foreach (FriendVo vo in GetFriendData(type))
                if (vo.OnlineFlag == 1) num++;
            return num;
        }

        /// <summary>14000 全量分桶落地(对标老端 setFriendData)。</summary>
        public void SetFriendData(int type, List<FriendVo> list)
        {
            _friendDic[type] = list ?? new List<FriendVo>();
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_DATA_UPDATE, type);
        }

        /// <summary>14013 增量:按 role_id 存在则覆盖、不存在则插入(对标老端 changeFriendList)。</summary>
        public void UpsertFriendList(int type, List<FriendVo> updates)
        {
            if (updates == null || updates.Count == 0) return;
            if (!_friendDic.TryGetValue(type, out List<FriendVo> list))
            {
                list = new List<FriendVo>();
                _friendDic[type] = list;
            }
            foreach (FriendVo u in updates)
            {
                int idx = list.FindIndex(v => v.RoleId == u.RoleId);
                if (idx >= 0) list[idx] = u;
                else list.Add(u);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_DATA_UPDATE, type);
        }

        /// <summary>14014 增量移除(对标老端 delFriendList)。</summary>
        public void RemoveFriendList(int type, List<long> roleIds)
        {
            if (roleIds == null || roleIds.Count == 0) return;
            if (!_friendDic.TryGetValue(type, out List<FriendVo> list)) return;
            foreach (long id in roleIds)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].RoleId == id) { list.RemoveAt(i); break; }
                }
            }
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_DATA_UPDATE, type);
        }

        /// <summary>14009 上下线:找到对应桶该好友更新 online_flag/offline_time(对标老端 updateFriendOline)。</summary>
        public void UpdateOnline(int relaType, long roleId, int onlineFlag, long serverTimestampSec)
        {
            if (!_friendDic.TryGetValue(relaType, out List<FriendVo> list)) return;
            foreach (FriendVo vo in list)
            {
                if (vo.RoleId != roleId) continue;
                vo.OnlineFlag = onlineFlag;
                if (onlineFlag == 0) vo.OfflineTime = (int)(TimeUtil.NowSec() - serverTimestampSec);
                EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_ONLINE_UPDATE, vo);
                return;
            }
        }

        /// <summary>14015 亲密度(只在好友桶命中才更新,对标老端 updateIntimacy)。</summary>
        public void UpdateIntimacy(long roleId, int intimacy)
        {
            FriendVo vo = GetFriendById(roleId);
            if (vo != null) vo.Intimacy = intimacy;
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_INTIMACY_UPDATE, roleId, intimacy);
        }

        // ===================================================================================
        // 推荐/搜索(14001/14002)+ 申请发送标记(14003)+ 假人填充(对标老端 dummyPlayerList)
        // ===================================================================================

        public sealed class RecommendVo
        {
            public long RoleId;
            public string Name = "";
            public int Career;
            public int Sex;
            public int Turn;
            public int Lv;
            public int Vip;
            public int VipHide;
            public string Picture = "";
            public int PictureVer;
            public long Combat;
            public int OnlineFlag;
            public int IsSupvip;
            /// <summary>假人填充位(对标老端 is_faker):点"添加"时 role_id 强制置0仍照发14003(见 FriendController)。</summary>
            public bool IsFaker;
            /// <summary>本地标记:已对该条发送过加好友申请(对标老端 changeRecommendState,不从列表移除,按钮变"已申请")。</summary>
            public bool ApplyFriend;
        }

        private readonly List<RecommendVo> _recommendList = new List<RecommendVo>();
        /// <summary>本次是否走过"搜索"(14002,对标老端 checkPlayer)——为 true 时空列表不再填充假人。</summary>
        public bool CheckPlayerMode { get; private set; }
        /// <summary>当前待跟踪的加好友目标 role_id(对标老端 addRecommendId,14003 成功后据此在列表里标记 applyFriend)。</summary>
        public long AddRecommendId { get; set; }

        /// <summary>取推荐列表(对标老端 GetrecommendList:为空且非搜索态时用假人填充避免空面板)。</summary>
        public List<RecommendVo> GetRecommendList()
        {
            if (_recommendList.Count == 0 && !CheckPlayerMode) return BuildDummyPlayerList();
            return _recommendList;
        }

        /// <summary>14001 code==1 全量替换(对标老端 setRecommendData)。</summary>
        public void SetRecommendData(List<RecommendVo> list)
        {
            CheckPlayerMode = false;
            _recommendList.Clear();
            if (list != null) _recommendList.AddRange(list);
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_RECOMMEND_UPDATE);
        }

        /// <summary>14002 搜索成功:塞成单元素数组覆盖(对标老端 setCheckPlayerData)。</summary>
        public void SetCheckPlayerData(RecommendVo vo)
        {
            CheckPlayerMode = true;
            _recommendList.Clear();
            if (vo != null) _recommendList.Add(vo);
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_RECOMMEND_UPDATE);
        }

        /// <summary>14002 搜索失败:清空推荐列表(对标老端 cleanRecommendList)。</summary>
        public void CleanRecommendList()
        {
            CheckPlayerMode = true;
            _recommendList.Clear();
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_RECOMMEND_UPDATE);
        }

        /// <summary>14003 成功:只标记该条 applyFriend=true,不移除(对标老端 changeRecommendState)。</summary>
        public void MarkRecommendApplied()
        {
            long id = AddRecommendId;
            if (id == 0) return;
            for (int i = _recommendList.Count - 1; i >= 0; i--)
            {
                if (_recommendList[i].RoleId == id)
                {
                    _recommendList[i].ApplyFriend = true;
                    AddRecommendId = 0;
                    EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_RECOMMEND_UPDATE);
                    return;
                }
            }
        }

        /// <summary>假人填充(对标老端 dummyPlayer/dummyPlayerList):2~5个,借用登录创角随机名表(LoginConfigs.RandomRoleName)
        /// 代替老端 ConfigRandomName——效果等价(同一份姓名池概念),未额外引入配置表。</summary>
        private List<RecommendVo> BuildDummyPlayerList()
        {
            var list = new List<RecommendVo>();
            int num = 2 + UnityEngine.Random.Range(0, 4); // 2~5
            for (int i = 0; i < num; i++) list.Add(BuildDummyPlayer());
            return list;
        }

        private static RecommendVo BuildDummyPlayer()
        {
            int sex = 1 + UnityEngine.Random.Range(0, 2);
            long roleId = UnityEngine.Random.Range(1, 1000000);
            Shenxiao.Module.Core.Role.RoleModel role = Shenxiao.Module.Core.Role.RoleModel.Instance;
            int career = 1 + UnityEngine.Random.Range(0, 2);
            int selfTurn = role.HasBaseInfo && role.Figure != null ? role.Figure.turn : 0;
            int turn = selfTurn > 0 ? UnityEngine.Random.Range(0, selfTurn + 1) : 0;
            int sign = UnityEngine.Random.value < 0.5f ? 1 : -1;
            int lv = (role.HasBaseInfo ? role.Level : 1) + UnityEngine.Random.Range(0, 11) * sign;
            long combat = (role.HasBaseInfo ? role.CombatPower : 0) + UnityEngine.Random.Range(0, 3001) * sign;
            return new RecommendVo
            {
                RoleId = roleId,
                Name = Shenxiao.Module.Core.Login.LoginConfigs.RandomRoleName(sex),
                Career = career,
                Sex = sex,
                Turn = turn,
                Lv = System.Math.Max(1, lv),
                Vip = UnityEngine.Random.Range(0, 13),
                OnlineFlag = 1,
                Combat = System.Math.Max(0, combat),
                IsFaker = true,
            };
        }

        // ===================================================================================
        // 好友申请列表(14006 全量 / 14008 推送去重插入 / 14004 一键清空 / 14005 单条移除)
        // ===================================================================================

        public sealed class ApplyVo
        {
            public long RoleId;
            public string Name = "";
            public int Career;
            public int Turn;
            public int Lv;
            public string Picture = "";
            public int PictureVer;
            public long Combat;
            public int AddTime;
        }

        private readonly List<ApplyVo> _applyList = new List<ApplyVo>();
        public IReadOnlyList<ApplyVo> ApplyList => _applyList;
        public bool HaveNewApply { get; private set; }
        /// <summary>本次一键处理类型回传(对标老端 applyType,accept=1 时联动重拉好友列表)。</summary>
        public int LastApplyType { get; set; } = -1;
        /// <summary>本次单条处理目标 id(对标老端 oneApplyId)。</summary>
        public long LastOneApplyId { get; set; }

        private void SetHaveNewApply(bool value)
        {
            if (HaveNewApply == value) return;
            HaveNewApply = value;
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_REDDOT_UPDATE);
        }

        /// <summary>14006 全量替换(对标老端 setApplyList)。</summary>
        public void SetApplyList(List<ApplyVo> list)
        {
            _applyList.Clear();
            if (list != null) _applyList.AddRange(list);
            SetHaveNewApply(_applyList.Count > 0);
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_APPLY_UPDATE);
        }

        /// <summary>14008 推送:去重后插入(对标老端 addFriendToList)。</summary>
        public void AddApply(ApplyVo vo)
        {
            if (vo == null) return;
            foreach (ApplyVo existing in _applyList)
                if (existing.RoleId == vo.RoleId) return;
            _applyList.Add(vo);
            SetHaveNewApply(true);
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_APPLY_UPDATE);
        }

        /// <summary>14004 回包:无视 code 清空整份列表(对标老端 reSetApplyList)。accept 需调用方另行 RequestFriendList(1)。</summary>
        public void ResetApplyList()
        {
            _applyList.Clear();
            SetHaveNewApply(false);
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_APPLY_UPDATE);
        }

        /// <summary>14005 回包:按 LastOneApplyId 逐条移除(对标老端 delApplyer)。</summary>
        public void RemoveOneApply()
        {
            SetHaveNewApply(false);
            long id = LastOneApplyId;
            if (id == 0) return;
            for (int i = _applyList.Count - 1; i >= 0; i--)
            {
                if (_applyList[i].RoleId == id) { _applyList.RemoveAt(i); LastOneApplyId = 0; break; }
            }
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_APPLY_UPDATE);
        }

        // ===================================================================================
        // 右键菜单数据(14010,800ms 节流缓存;对标老端 setMenuData/getMenuData)
        // ===================================================================================

        public sealed class MenuInfo
        {
            public long RoleId;
            public FigureProto Figure;
            public int Rela;
            public int TeamId;
            /// <summary>最近一次真实发协议的本地时间戳(ms),供 800ms 节流判定(对标老端 requestTime)。</summary>
            public long RequestTimeMs;
        }

        private readonly Dictionary<long, MenuInfo> _menuDataDic = new Dictionary<long, MenuInfo>();

        public MenuInfo GetMenuData(long roleId) => _menuDataDic.TryGetValue(roleId, out MenuInfo v) ? v : null;

        /// <summary>是否需要真发 14010(role_id 自身/0 拦截 + 800ms 节流,对标老端 request_menu_data)。
        /// 返回 true 时调用方应真的发协议;调用方发协议前后都应调 <see cref="TouchMenuThrottle"/> 打时间戳。</summary>
        public bool ShouldRequestMenu(long roleId, long selfRoleId)
        {
            if (roleId == 0 || roleId == selfRoleId) return false;
            if (_menuDataDic.TryGetValue(roleId, out MenuInfo cached))
            {
                if (TimeUtil.NowMs() - cached.RequestTimeMs < MenuThrottleMs) return false;
            }
            return true;
        }

        /// <summary>发协议前占位打时间戳(避免同一帧内重复判定通过)。</summary>
        public void TouchMenuThrottle(long roleId)
        {
            if (!_menuDataDic.TryGetValue(roleId, out MenuInfo info))
            {
                info = new MenuInfo { RoleId = roleId };
                _menuDataDic[roleId] = info;
            }
            info.RequestTimeMs = TimeUtil.NowMs();
        }

        /// <summary>14010 回包落地(对标老端 setMenuData,replace=true 全量替换)。</summary>
        public void SetMenuData(long roleId, FigureProto figure, int rela, int teamId)
        {
            long requestTime = _menuDataDic.TryGetValue(roleId, out MenuInfo old) ? old.RequestTimeMs : TimeUtil.NowMs();
            _menuDataDic[roleId] = new MenuInfo { RoleId = roleId, Figure = figure, Rela = rela, TeamId = teamId, RequestTimeMs = requestTime };
            EventDispatcher.Emit(GlobalEvent.EVT_FRIEND_MENU_UPDATE, roleId);
        }

        // ===================================================================================
        // 关系操作(14007)记录:成功后按记录的 type 重拉对应列表(对标老端 friendOperationt/flushOperationtView)
        // ===================================================================================

        /// <summary>本次 14007 请求的 type,发送前记录,回包后调用方据此重拉对应关系列表并清零。</summary>
        public int LastOperateType { get; set; }

        // ===================================================================================
        // 资料卡(19501→19502,module_id=1 基础装备;对标老端 requestPlayerMessage/setPlayerMessage)
        // ===================================================================================

        public sealed class EquipCardItem
        {
            public long GoodsId;
            public int TypeId;
            public int Cell;
            public int Color;
            public int Stren;
            public int Star;
            public int Stage;
            public int Level;
            public int GodLevel;
        }

        public sealed class MagicCircleItem { public int Lv; public int IsOpen; public int FreeFlag; public int EndTime; }
        public sealed class FairyItem { public int Type; public int IsActive; }

        public sealed class PlayerCard
        {
            public int ServerId;
            public long RoleId;
            public long Combat;
            public int AchvStage;
            public FigureProto Figure;
            public List<EquipCardItem> EquipList = new List<EquipCardItem>();
            public List<MagicCircleItem> MagicCircle = new List<MagicCircleItem>();
            public List<FairyItem> FairyList = new List<FairyItem>();
        }

        public PlayerCard LastPlayerCard { get; private set; }

        public void SetPlayerCard(PlayerCard card)
        {
            LastPlayerCard = card;
            EventDispatcher.Emit(GlobalEvent.EVT_PLAYER_CARD, card);
        }

        // ===================================================================================
        // 私聊对象展示信息缓存(对标老端 privateChatTabList_ 里随手存的 role_id/picture/dress_list/lv/career/
        // role_name 快照——本端 ChatModel.PrivateChatTabList 只存 role_id,展示字段(名字/职业/转生/等级)另存于此,
        // 供 FriendChatTabItem/FriendChatView 头部渲染。来源:打开聊天窗时的 FriendVo,或 11028/11002 回包 Figure。
        // ===================================================================================

        public sealed class ChatPartnerInfo
        {
            public string Name = "";
            public int Career;
            public int Turn;
            public int Lv;
        }

        private readonly Dictionary<long, ChatPartnerInfo> _chatPartners = new Dictionary<long, ChatPartnerInfo>();

        public void RememberChatPartner(long roleId, string name, int career, int turn, int lv)
        {
            _chatPartners[roleId] = new ChatPartnerInfo { Name = name, Career = career, Turn = turn, Lv = lv };
        }

        public ChatPartnerInfo GetChatPartner(long roleId) => _chatPartners.TryGetValue(roleId, out ChatPartnerInfo v) ? v : null;

        // ===================================================================================

        /// <summary>断线/登出清空(对标老端各 dic/list 复位)。</summary>
        public void Reset()
        {
            _friendDic.Clear();
            _recommendList.Clear();
            CheckPlayerMode = false;
            AddRecommendId = 0;
            _applyList.Clear();
            HaveNewApply = false;
            LastApplyType = -1;
            LastOneApplyId = 0;
            _menuDataDic.Clear();
            LastOperateType = 0;
            LastPlayerCard = null;
            _chatPartners.Clear();
        }
    }
}
