using Shenxiao.Module.Core.Boss;

namespace Shenxiao.Module.Core.Boss.Views.BossMain
{
    /// <summary>
    /// Boss 场景 HUD 的页面专属只读请求入口。场景寻路、选怪、采集、复活和跨模块跳转
    /// 依赖尚未迁移的场景运行时，因此不在这里伪造行为。
    /// </summary>
    public static class BossMainFlow
    {
        public const float BossHpPollIntervalSeconds = 5f;

        public static void RequestReadonlySnapshot()
        {
            BossController.Instance.RequestWarFreeInfo();
            BossController.Instance.RequestWarFreeEndTime();
            BossController.Instance.RequestBossDeathDebuff();
            BossController.Instance.RequestBossHpShow();
        }

        public static void RequestBossHp() => BossController.Instance.RequestBossHpShow();
    }
}
