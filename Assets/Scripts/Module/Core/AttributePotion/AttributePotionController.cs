using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Bag;

namespace Shenxiao.Module.Core.AttributePotion
{
    public sealed class AttributePotionController : BaseController
    {
        public static readonly AttributePotionController Instance = new AttributePotionController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private AttributePotionController() { }
        protected override void Register()
        {
            RegisterProtocal(Proto.ATTRIBUTE_POTION_ERROR, On21700);
            RegisterProtocal(Proto.ATTRIBUTE_POTION_LEVEL_COUNT, On21701);
            RegisterProtocal(Proto.ATTRIBUTE_POTION_ALL_COUNT, On21703);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnDayChange);
        }
        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnDayChange);
            AttributePotionModel.Instance.Clear(); base.Dispose();
        }
        public void RequestStartup()
        {
            // 配置仅供本地发包裁剪；21703 仍必须立即请求，不能等待资源加载。
            _ = AttributePotionConfigs.EnsureLoaded();
            Send(Proto.ATTRIBUTE_POTION_ALL_COUNT, null);
        }
        /// <summary>请求药剂档位（当前配置为1..4），不是角色等级；角色等级仅用于 use_count 配置命中。</summary>
        public void RequestLevel(byte potionTier)
        {
            if (!AttributePotionConfigs.IsLoaded || !AttributePotionConfigs.HasPotionLevel(potionTier)) return;
            Send(Proto.ATTRIBUTE_POTION_LEVEL_COUNT, "c", potionTier);
        }
        /// <summary>生产入口：等级只能由权威物品表派生，数量按真实背包和服务器限制裁剪；不做乐观扣包。</summary>
        public bool TryRequestUse(int goodsId, uint requestedNum = uint.MaxValue)
        {
            if (!AttributePotionConfigs.IsLoaded || !RoleModel.Instance.HasBaseInfo || !AttributePotionConfigs.TryGetPotion(goodsId, out var potion) ||
                !AttributePotionConfigs.TryGetLimit(goodsId, RoleModel.Instance.Level, out var limit) ||
                !AttributePotionModel.Instance.TryGet(potion.Level, goodsId, out var count)) return false;
            ulong bag = (ulong)System.Math.Max(0L, BagModel.Instance.GetTypeGoodsNum(goodsId));
            ulong dayLeft = limit.DayTimes > count.CurrentDayCount ? (ulong)(limit.DayTimes - count.CurrentDayCount) : 0;
            ulong allLeft = limit.AllTimes > count.CurrentCount ? limit.AllTimes - count.CurrentCount : 0;
            if (bag == 0) { TipsManager.Toast("道具不足"); return false; }
            if (allLeft == 0) { TipsManager.Toast("已达到使用上限"); return false; }
            if (dayLeft == 0) { TipsManager.Toast("今天已达最大使用次数"); return false; }
            ulong use = System.Math.Min(System.Math.Min(bag, dayLeft), System.Math.Min(allLeft, requestedNum));
            if (use == 0 || use > uint.MaxValue) return false;
            SendUseExact((uint)goodsId, (uint)use, potion.Level); return true;
        }
        private void SendUseExact(uint goodsId, uint num, byte level) => Send(Proto.ATTRIBUTE_POTION_USE, "iic", goodsId, num, level);
        private void OnDayChange() { AttributePotionModel.Instance.Clear(); RequestStartup(); }
        private void Send(int id, string format, params object[] args)
        {
#if UNITY_EDITOR
            if (s_outboundIntercept != null && s_outboundIntercept(UserMsgAdapter.Encode(id, format, args))) return;
#endif
            NetManager.SendFmt(id, format, args);
        }
        private static List<AttributePotionModel.Count> ReadCounts(NetReader r)
        {
            int n = r.ReadU16(); var values = new List<AttributePotionModel.Count>(n);
            for (int i = 0; i < n; ++i) values.Add(new AttributePotionModel.Count { GoodsId = (int)r.ReadU32(), Level = r.ReadU8(), CurrentDayCount = r.ReadU32(), CurrentCount = unchecked((ulong)r.ReadU64()) });
            return values;
        }
        private void On21701(NetReader r) { var values = ReadCounts(r); if (values.Count > 0) AttributePotionModel.Instance.ReplaceLevel(values[0].Level, values); }
        private void On21703(NetReader r) => AttributePotionModel.Instance.MergeAll(ReadCounts(r));
        private static void On21700(NetReader r)
        {
            uint code = r.ReadU32(); GameLog.Warn("AttributePotion", "21700 error={0}", code);
            TipsManager.Toast(code == 2170001 ? "今天已达最大使用次数" : code == 2170002 ? "已达到使用上限" : "属性药剂操作失败(" + code + ")");
        }
    }
}
