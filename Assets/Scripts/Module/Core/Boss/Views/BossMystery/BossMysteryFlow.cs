using System.Threading.Tasks;

namespace Shenxiao.Module.Core.Boss.Views.BossMystery
{
    /// <summary>太古遗凶页面专属只读装配。协议解析仍由既有 Boss/KfBoss Controller 独占。</summary>
    public static class BossMysteryFlow
    {
        public const int BossType = BossModel.BossType.KfGreatDemon;

        public static async Task PrepareAsync()
        {
            await BossConfigs.EnsureLoaded();
            BossController.Instance.RequestBossList(BossType);
            KfBossController.Instance.RequestGreatDemonRewardState();
            KfBossController.Instance.RequestGreatDemonBoxInfo();
        }
    }
}
