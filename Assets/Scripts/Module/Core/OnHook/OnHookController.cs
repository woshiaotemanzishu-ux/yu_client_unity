using Shenxiao.Common.Tips;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.OnHook
{
    /// <summary>
    /// 挂机收益协议控制器(薄增量六件套第20轮工单;服务端 pt_132 段内 13216)。
    /// 解主线 101211(ctype91「领取1次挂机收益」,唯一事件计数型:领一次即完成,无需专用任务代码)。
    /// 回包 schema 摘自 ClientProtocol.json "13216":errcode:i, old_lv:h, old_lv_ratio:h,
    /// goods_list[u16×{style:c, typeId:i, count:l}]。信息协议 13211/13212/13214 留后。
    /// </summary>
    public sealed class OnHookController : BaseController
    {
        public static readonly OnHookController Instance = new OnHookController();

        private OnHookController() { }

        /// <summary>挂机时长上限(秒;对标老端 OutLineModel.max_outline_time = 20*3600+onhook_time)。
        /// 数据来源 18401(模块加成列表 key==2),由 <see cref="Skill.SkillTalentModel"/> 解析后写入,
        /// 与本模块 13216(领取挂机收益)配套——领取上限/挂机时长展示可读它,当前 13216 本身不消费,先留字段。</summary>
        public static long MaxOnlineTimeSec { get; private set; }

        public static void SetMaxOnlineTimeSec(long sec) => MaxOnlineTimeSec = sec;

        protected override void Register()
        {
            RegisterProtocal(Proto.ONHOOK_RECEIVE, On13216);
        }

        /// <summary>13216 领取挂机收益(C2S 无参)。</summary>
        public void Receive()
        {
            SendFmt(Proto.ONHOOK_RECEIVE);
            GameLog.Info("OnHook", "receive 13216");
        }

        /// <summary>13216 回包(对标 ClientProtocol.json "13216" 原文按序读完):
        /// errcode:i, old_lv:h, old_lv_ratio:h, goods_list[u16×{style:c, typeId:i, count:l}]。
        /// errcode==1 → toast「挂机收益已领取」;else 显码降级(错误码表未移植)。</summary>
        private void On13216(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int oldLv = r.ReadU16();
            int oldLvRatio = r.ReadU16();
            var goodsList = r.ReadArray(ReadGoods);
            if (errcode != 1)
            {
                TipsManager.Toast("领取失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OnHook", "13216 fail errcode={0}", errcode);
                return;
            }
            TipsManager.Toast("挂机收益已领取");
            GameLog.Info("OnHook", "13216 ok old_lv={0} old_lv_ratio={1} goods={2} remaining={3}B",
                oldLv, oldLvRatio, goodsList.Count, r.Remaining);
        }

        private static (int style, int typeId, long count) ReadGoods(NetReader r)
        {
            return (r.ReadU8(), (int)r.ReadU32(), r.ReadU64());   // {style:c, typeId:i, count:l}
        }
    }
}
