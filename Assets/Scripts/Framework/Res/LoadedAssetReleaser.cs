using System.Collections.Generic;
using UnityEngine;

namespace Shenxiao.Framework.Res
{
    /// <summary>
    /// 把 ResManager.LoadAsync 借出的资产引用(模型 prefab/动作 clip 等)登记到使用它的实例上,
    /// 实例销毁时自动归还(引用计数-1,归零才真正卸 bundle)。
    /// 解决 3D 装配链"只借不还"导致资产与所属 bundle 整局常驻、随遇怪种类单调累积的问题。
    /// 约定:每次 LoadAsync 成功 = 一个引用 = 恰好一次 Track(或加载方自行 Release);不重复 Track 同一次加载。
    /// </summary>
    public sealed class LoadedAssetReleaser : MonoBehaviour
    {
        private readonly List<Object> _assets = new List<Object>();

        /// <summary>把一次 LoadAsync 借的资产引用挂到 owner 上,owner 销毁时归还。owner/asset 为空则直接归还引用。</summary>
        public static void Track(GameObject owner, Object asset)
        {
            if (asset == null) return;
            if (owner == null)
            {
                ResManager.Release(asset);
                return;
            }
            LoadedAssetReleaser releaser = owner.GetComponent<LoadedAssetReleaser>();
            if (releaser == null) releaser = owner.AddComponent<LoadedAssetReleaser>();
            releaser._assets.Add(asset);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _assets.Count; i++)
            {
                ResManager.Release(_assets[i]);
            }
            _assets.Clear();
        }
    }
}
