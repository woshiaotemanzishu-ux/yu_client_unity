using System;
using System.Collections.Generic;
using Shenxiao.Module.Core.Scene.Vo;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 场景对象数据层(对标老客户端 SceneManager 的 role_vo_list / monster_vo_list / npc 表 + g_event)。
    ///
    /// 职责边界:协议层(<see cref="SceneController"/>)解析 12002/12001/12004/12006/12007/12008/12009 后,
    /// 调这里的 Add*/Move*/Remove*/ApplyHp 落表并触发强类型事件;渲染层(后续 RoleSpawner/MonsterSpawner/NpcSpawner)
    /// 订阅这些事件建模/插值/销毁。本类只存数据、发信号,不持有任何渲染对象,也不分帧——分帧/对象池由渲染层负责。
    ///
    /// 键:玩家用 role_id(u64);怪物/NPC 用实例 id(u32→int);掉落用 drop_id(u64)。
    /// </summary>
    public sealed class SceneManager
    {
        public static readonly SceneManager Instance = new SceneManager();
        private SceneManager() { }

        private readonly Dictionary<long, RoleVo> _roles = new Dictionary<long, RoleVo>();
        private readonly Dictionary<int, MonsterVo> _monsters = new Dictionary<int, MonsterVo>();
        private readonly Dictionary<int, NpcVo> _npcs = new Dictionary<int, NpcVo>();
        private readonly Dictionary<long, DropVo> _drops = new Dictionary<long, DropVo>();

        // —— 渲染层订阅的强类型事件(携带 vo,避免依赖无参全局事件再回查表)——
        public event Action<RoleVo> RoleAdded;
        public event Action<long> RoleRemoved;
        public event Action<RoleVo> RoleMoved;
        public event Action<RoleVo> RoleHpChanged;
        public event Action<RoleVo> RoleDesignationChanged;

        public event Action<MonsterVo> MonsterAdded;
        public event Action<int> MonsterRemoved;
        public event Action<MonsterVo> MonsterMoved;
        public event Action<MonsterVo> MonsterHpChanged;

        public event Action<NpcVo> NpcAdded;
        public event Action<int> NpcRemoved;
        public event Action<NpcVo> NpcChanged;     // 任务图标等变更

        public event Action<DropVo> DropAdded;
        public event Action<long> DropRemoved;

        // —— 只读遍历(渲染层初始化时可一次性拉取已有 VO 补建)——
        public IEnumerable<RoleVo> AllRoles => _roles.Values;
        public IEnumerable<MonsterVo> AllMonsters => _monsters.Values;
        public IEnumerable<NpcVo> AllNpcs => _npcs.Values;
        public IEnumerable<DropVo> AllDrops => _drops.Values;

        public int RoleCount => _roles.Count;
        public int MonsterCount => _monsters.Count;
        public int NpcCount => _npcs.Count;
        public int DropCount => _drops.Count;

        // ===== 玩家 =====
        public void AddRole(RoleVo vo)
        {
            if (vo == null) return;
            _roles[vo.RoleId] = vo;
            RoleAdded?.Invoke(vo);
        }

        public RoleVo GetRole(long id) => _roles.TryGetValue(id, out RoleVo v) ? v : null;

        public bool RemoveRole(long id)
        {
            if (!_roles.Remove(id)) return false;
            RoleRemoved?.Invoke(id);
            return true;
        }

        public void MoveRole(long id, int x, int y, int moveFlag)
        {
            if (!_roles.TryGetValue(id, out RoleVo vo)) return;
            vo.X = x; vo.Y = y; vo.MoveFlag = moveFlag;
            RoleMoved?.Invoke(vo);
        }

        /// <summary>41105 场景称号广播：只更新已在场的其他玩家，并通知 NameBoard 消费者。</summary>
        public bool SetRoleDesignation(long id, uint designationId)
        {
            if (!_roles.TryGetValue(id, out RoleVo vo) || vo.Figure == null) return false;
            vo.Figure.SetDesignationId(designationId);
            RoleDesignationChanged?.Invoke(vo);
            return true;
        }

        // ===== 怪物 / 采集物 =====
        public void AddMonster(MonsterVo vo)
        {
            if (vo == null) return;
            _monsters[vo.InstanceId] = vo;
            MonsterAdded?.Invoke(vo);
        }

        public MonsterVo GetMonster(int id) => _monsters.TryGetValue(id, out MonsterVo v) ? v : null;

        public bool RemoveMonster(int id)
        {
            if (!_monsters.Remove(id)) return false;
            MonsterRemoved?.Invoke(id);
            return true;
        }

        public void MoveMonster(int id, int x, int y)
        {
            if (!_monsters.TryGetValue(id, out MonsterVo vo)) return;
            vo.X = x; vo.Y = y;
            MonsterMoved?.Invoke(vo);
        }

        // ===== NPC =====
        public void AddNpc(NpcVo vo)
        {
            if (vo == null) return;
            _npcs[vo.NpcId] = vo;
            NpcAdded?.Invoke(vo);
        }

        public NpcVo GetNpc(int npcId) => _npcs.TryGetValue(npcId, out NpcVo v) ? v : null;

        public bool RemoveNpc(int npcId)
        {
            if (!_npcs.Remove(npcId)) return false;
            NpcRemoved?.Invoke(npcId);
            return true;
        }

        /// <summary>12020 任务图标回填:置 NPC 的 TaskIcon 并通知渲染层刷新头顶标。</summary>
        public void SetNpcTaskIcon(int npcId, int icon)
        {
            if (!_npcs.TryGetValue(npcId, out NpcVo vo)) return;
            vo.TaskIcon = icon;
            NpcChanged?.Invoke(vo);
        }

        // ===== 掉落 =====
        public void AddDrop(DropVo vo)
        {
            if (vo == null) return;
            _drops[vo.DropId] = vo;
            DropAdded?.Invoke(vo);
        }

        public DropVo GetDrop(long dropId) => _drops.TryGetValue(dropId, out DropVo v) ? v : null;

        public bool RemoveDrop(long dropId)
        {
            if (!_drops.Remove(dropId)) return false;
            DropRemoved?.Invoke(dropId);
            return true;
        }

        // ===== 跨类型路由(对标老客户端 GetSceneObjVo / On12006 / On12008 / On12009)=====

        /// <summary>12006 删除场景对象:按老客户端顺序先怪物、再玩家(其他对象暂未入表)。</summary>
        public void DeleteSceneObj(long instanceId)
        {
            if (instanceId >= 0 && instanceId <= int.MaxValue && RemoveMonster((int)instanceId)) return;
            RemoveRole(instanceId);
        }

        /// <summary>12008 通用位置同步:先按怪物 id 找,再按玩家 id 找。</summary>
        public void MoveSceneObj(long instanceId, int x, int y)
        {
            if (instanceId >= 0 && instanceId <= int.MaxValue && _monsters.ContainsKey((int)instanceId))
            {
                MoveMonster((int)instanceId, x, y);
                return;
            }
            if (_roles.ContainsKey(instanceId))
            {
                MoveRole(instanceId, x, y, MoveType.Normal);
            }
        }

        /// <summary>12009 血量同步:先怪物、再玩家。</summary>
        public void ApplyHp(long objId, long hp, long hpLim)
        {
            if (objId >= 0 && objId <= int.MaxValue && _monsters.TryGetValue((int)objId, out MonsterVo m))
            {
                m.Hp = hp; m.HpLim = hpLim;
                MonsterHpChanged?.Invoke(m);
                return;
            }
            if (_roles.TryGetValue(objId, out RoleVo r))
            {
                r.Hp = hp; r.HpLim = hpLim;
                RoleHpChanged?.Invoke(r);
            }
        }

        /// <summary>切场景/登出:清空所有场景对象(渲染层应同时收到各自 Removed 或自行随 Cleared 清理)。</summary>
        public void Clear()
        {
            _roles.Clear();
            _monsters.Clear();
            _npcs.Clear();
            _drops.Clear();
        }
    }
}
