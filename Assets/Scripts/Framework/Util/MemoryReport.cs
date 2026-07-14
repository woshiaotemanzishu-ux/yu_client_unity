using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace Shenxiao.Framework.Util
{
    /// <summary>
    /// Development 构建专用内存归因:进世界后定时把 堆分区/已载bundle清单/大资产TOP 打进日志,
    /// 无头探针抓浏览器控制台即可归因 wasm 堆大头(Unity Memory Profiler 抓不了 WebGL 快照)。
    /// Release 构建里 Profiler.* 返回 0,一律 no-op。
    /// </summary>
    public static class MemoryReport
    {
        private static bool _scheduled;

        /// <summary>GAME_START 后调:world+30s / world+120s 各 dump 一次。</summary>
        public static void ScheduleAfterGameStart()
        {
            if (!Debug.isDebugBuild || _scheduled) return;
            _scheduled = true;
            _ = RunAsync();
        }

        private static async Task RunAsync()
        {
            await TimeUtil.Delay(30000);
            Dump("world+30s");
            await TimeUtil.Delay(90000);
            Dump("world+120s");
        }

        public static void Dump(string tag)
        {
            if (!Debug.isDebugBuild) return;
            try
            {
                var sb = new StringBuilder(8192);
                sb.AppendFormat("[MemReport {0}] alloc={1}MB reserved={2}MB monoHeap={3}MB monoUsed={4}MB gfxDriver={5}MB\n",
                    tag,
                    Profiler.GetTotalAllocatedMemoryLong() / 1048576,
                    Profiler.GetTotalReservedMemoryLong() / 1048576,
                    Profiler.GetMonoHeapSizeLong() / 1048576,
                    Profiler.GetMonoUsedSizeLong() / 1048576,
                    Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576);

                int bundleCount = 0;
                var names = new StringBuilder();
                foreach (AssetBundle b in AssetBundle.GetAllLoadedAssetBundles())
                {
                    bundleCount++;
                    if (bundleCount <= 60) names.Append(b.name).Append(',');
                }
                sb.AppendFormat("loadedBundles={0} [{1}{2}]\n", bundleCount, names, bundleCount > 60 ? "…" : "");

                AppendTop<Texture2D>(sb, "Texture2D", 15);
                AppendTop<Mesh>(sb, "Mesh", 10);
                AppendTop<AnimationClip>(sb, "AnimClip", 10);
                AppendTop<AudioClip>(sb, "AudioClip", 5);
                GameLog.Info("Mem", sb.ToString());
            }
            catch (System.Exception e)
            {
                GameLog.Warn("Mem", "MemReport failed: {0}", e.Message);
            }
        }

        private static void AppendTop<T>(StringBuilder sb, string label, int top) where T : Object
        {
            T[] all = Resources.FindObjectsOfTypeAll<T>();
            long total = 0;
            var sizes = new (long size, string name)[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                long s = Profiler.GetRuntimeMemorySizeLong(all[i]);
                total += s;
                sizes[i] = (s, all[i].name);
            }
            System.Array.Sort(sizes, (a, b) => b.size.CompareTo(a.size));
            sb.AppendFormat("{0}: count={1} total={2}MB top:", label, all.Length, total / 1048576);
            int n = Mathf.Min(top, sizes.Length);
            for (int i = 0; i < n; i++)
                sb.AppendFormat(" {0}={1}KB", sizes[i].name, sizes[i].size / 1024);
            sb.Append('\n');
        }
    }
}
