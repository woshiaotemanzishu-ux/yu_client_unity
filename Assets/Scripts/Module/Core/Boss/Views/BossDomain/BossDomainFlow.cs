using System.Threading.Tasks;

namespace Shenxiao.Module.Core.Boss.Views.BossDomain
{
    public static class BossDomainFlow
    {
        public static async Task PrepareAsync()
        {
            await BossConfigs.EnsureLoaded();
            KfBossController.Instance.RequestDecorationInfo();
            KfBossController.Instance.RequestDecorationUnfollowList();
        }
    }
}
