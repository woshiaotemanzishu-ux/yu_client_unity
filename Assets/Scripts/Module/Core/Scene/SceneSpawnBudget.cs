using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 场景对象创建的帧预算门(对标地图瓦片池的 MaxConcurrentTileLoads 思路,SceneMapView.cs):
    /// 进场/切图时 12100/12002 一口气推几十个 NPC/怪,模型 prefab 若已被预热/缓存,各自的 await 会
    /// 立刻返回 → 大量 Instantiate + 建名牌 TMP + AddComponent(Animation) 挤在相邻一两帧,形成尖刺。
    /// 重活(Instantiate 及其后续)前先 await WaitTurnAsync():每帧只放行固定个数,超出的排到后续帧。
    /// 所有续体都在主线程协作执行,计数无需加锁。
    /// </summary>
    public static class SceneSpawnBudget
    {
        private const int MaxSpawnsPerFrame = 3;

        private static int _frame = -1;
        private static int _used;

        public static async Task WaitTurnAsync()
        {
            while (true)
            {
                int now = Time.frameCount;
                if (now != _frame)
                {
                    _frame = now;
                    _used = 0;
                }
                if (_used < MaxSpawnsPerFrame)
                {
                    _used++;
                    return;
                }
                await Task.Yield();
            }
        }
    }
}
