using System.Collections.Generic;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 吞天洗魄(洗炼,15212/15213/15214/15252;自动循环 轮4 队列#4)数据层:免费次数 + 每部位待锁定槽位选择
    /// (对标老端 EquipModel.wash_free_times/wash_lock_dic)。每槽已洗出的属性(wash_attr)实例数据不落在此处——
    /// 走 <see cref="Bag.GoodsDynamicModel"/>(GoodsDetailVo.WashAttrs,15000/15001);此处只管"免费次数"与
    /// "UI 侧待锁定选择"两个纯本地小状态,15213 发送前读取。
    /// </summary>
    public sealed class EquipWashModel
    {
        public static readonly EquipWashModel Instance = new EquipWashModel();
        private EquipWashModel() { }

        /// <summary>15214 回包落库(对标老端 model.wash_free_times)。</summary>
        public int FreeTimes { get; private set; }

        // equip_type → 已勾选锁定的槽位下标集合(对标老端 wash_lock_dic[equip_type]);15213 发送时读取。
        private readonly Dictionary<int, HashSet<int>> _lockDic = new Dictionary<int, HashSet<int>>();

        // equip_type → 已开启(15212 成功)的槽位下标集合。15212 回包本身不带 equip_type(只有 goods_id/index),
        // 由 EquipWashController 用"发送时记的 pending equip_type"补齐后调用 MarkSlotOpened。
        private readonly Dictionary<int, HashSet<int>> _openedDic = new Dictionary<int, HashSet<int>>();

        public void ApplyFreeTimes(int freeTimes) => FreeTimes = freeTimes;

        /// <summary>标记某槽位已开启(15212 成功回调)。</summary>
        public void MarkSlotOpened(int equipType, int index)
        {
            if (!_openedDic.TryGetValue(equipType, out HashSet<int> set))
            {
                set = new HashSet<int>();
                _openedDic[equipType] = set;
            }
            set.Add(index);
        }

        /// <summary>某槽位是否已开启。</summary>
        public bool IsSlotOpened(int equipType, int index)
            => _openedDic.TryGetValue(equipType, out HashSet<int> set) && set.Contains(index);

        /// <summary>切换某槽位锁定态(对标 EquipWashPropItem._gp_lock 点击,老端纯本地状态、不发协议)。</summary>
        public void ToggleLock(int equipType, int index)
        {
            if (!_lockDic.TryGetValue(equipType, out HashSet<int> set))
            {
                set = new HashSet<int>();
                _lockDic[equipType] = set;
            }
            if (!set.Remove(index)) set.Add(index);
        }

        /// <summary>取当前锁定槽位列表(按下标升序,供 15213 手写序列使用;无锁定返回空列表)。</summary>
        public List<int> GetLockedIndices(int equipType)
        {
            if (!_lockDic.TryGetValue(equipType, out HashSet<int> set) || set.Count == 0) return new List<int>();
            var list = new List<int>(set);
            list.Sort();
            return list;
        }

        /// <summary>洗魄执行后清空该部位锁定选择(对标老端每次洗练完重新勾选)。</summary>
        public void ClearLock(int equipType) => _lockDic.Remove(equipType);

        public void Clear()
        {
            FreeTimes = 0;
            _lockDic.Clear();
            _openedDic.Clear();
        }
    }
}
