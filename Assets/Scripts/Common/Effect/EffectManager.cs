using System.Threading.Tasks;
using UnityEngine;
using Shenxiao.Framework.Res;

namespace Shenxiao.Common.Effect
{
    /// <summary>
    /// Particle / effect playback helper. Phase 0 skeleton.
    /// </summary>
    public static class EffectManager
    {
        public static async Task<GameObject> Play(string addrKey, Transform attachTo, float autoDestroySec = 0f)
        {
            var go = await ResManager.InstantiateAsync(addrKey, attachTo);
            if (go == null) return null;
            if (autoDestroySec > 0f)
            {
                Object.Destroy(go, autoDestroySec);
            }
            return go;
        }

        public static void Stop(GameObject effectGo)
        {
            if (effectGo == null) return;
            ResManager.ReleaseInstance(effectGo);
        }
    }
}
