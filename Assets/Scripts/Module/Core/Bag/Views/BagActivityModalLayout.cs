using System;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// BagModule 内 Activity 子窗共用的全屏背景。节点与图片保存在 Prefab；运行时只切显隐和绑定“点背景关闭”。
    /// 对标老端 BaseView1.use_background + click_bg_toClose。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BagActivityModalLayout : MonoBehaviour
    {
        [SerializeField] private Image blocker;

        public Image Blocker => blocker;

        public void Show(Action close)
        {
            if (blocker == null) return;
            UIUtil.ClearClicks(blocker);
            UIUtil.AddClick(blocker, close);
            blocker.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (blocker == null) return;
            UIUtil.ClearClicks(blocker);
            blocker.gameObject.SetActive(false);
        }
    }
}
