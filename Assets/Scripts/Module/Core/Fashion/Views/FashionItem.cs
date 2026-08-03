using System;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Fashion;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 时装列表项(对标老端 fashion/FashionItem.ts):图标/品质底板走 config_goods(fashion_id 与其激活道具
    /// type_id 同号,对标老端 GetGoodsBasicByTypeId(fashion_id)),选中框/已穿标/红点走功能性显隐,
    /// 未激活使用 UGUI 灰阶材质，对齐老端 Util.SetImageGray。
    /// 常被父视图 Instantiate 克隆后直接 SetData(不经 BaseView.Show),故点击绑定走幂等 EnsureInit(对标 BagItemRenderer)。
    /// </summary>
    public sealed class FashionItem : FashionItemBind
    {
        private bool _inited;
        private Action _onClick;

        public int FashionId { get; private set; }
        public Graphic ClickSurface => fashion_group != null ? fashion_group.GetComponent<Graphic>() : null;

        public void SetClick(Action onClick)
        {
            EnsureInit();
            _onClick = onClick;
        }

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            if (fashion_group != null)
            {
                Graphic surface = fashion_group.GetComponent<Graphic>();
                foreach (Graphic graphic in fashion_group.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = graphic == surface;
                if (surface != null) UIUtil.AddClick(surface, () => _onClick?.Invoke());
            }
        }

        /// <summary>填一格(对标老端 dataChanged)。activated=该 fashion_id 是否已激活(灰显判据);
        /// worn=是否为本位当前穿戴项;hasRed=是否有可操作红点(可激活/可进阶)。</summary>
        public void SetData(int fashionId, bool selected, bool activated, bool worn, bool hasRed)
        {
            EnsureInit();
            FashionId = fashionId;

            string name = GoodsModel.GetGoodsName(fashionId);
            if (fashion_name_label != null) fashion_name_label.text = string.IsNullOrEmpty(name) ? ("时装" + fashionId) : name;
            if (select != null) select.gameObject.SetActive(selected);
            if (fashion_waer_image != null) fashion_waer_image.gameObject.SetActive(worn);
            if (fashion_red_image != null) fashion_red_image.gameObject.SetActive(hasRed);

            string icon = GoodsModel.GetGoodsIcon(fashionId);
            if (fashion_icon_image != null && !string.IsNullOrEmpty(icon))
            {
                _ = ResManager.SetImageAsync(fashion_icon_image, GameResPath.GetGoodsIconPath(icon), nativeSize: false);
            }

            SetGray(fashion_icon_image, !activated);
            SetGray(fashion_plate_image, !activated);
        }

        /// <summary>对标老端 Util.SetImageGray，不能用透明度 tint 冒充灰阶。</summary>
        private static void SetGray(UnityEngine.UI.Image img, bool gray)
        {
            UIGrayStyle.Apply(img, gray);
        }
    }
}
