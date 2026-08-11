using System;
using Shenxiao.Common.Audio;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Fashion;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private Action<int> _onClick;
        private int _iconVersion;
        private int _plateVersion;
        private bool _iconLoading;
        private bool _plateLoading;
        private string _iconKey = "";
        private string _plateKey = "";
        private Sprite _iconSprite;
        private Sprite _plateSprite;
        private FashionItemClickSurface _clickSurface;

        public int FashionId { get; private set; }
        public Graphic ClickSurface => fashion_group != null ? fashion_group.GetComponent<Graphic>() : null;

        public void SetClick(Action<int> onClick)
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
                if (surface != null)
                {
                    surface.raycastTarget = true;
                    _clickSurface = surface.GetComponent<FashionItemClickSurface>();
                    if (_clickSurface == null)
                        _clickSurface = surface.gameObject.AddComponent<FashionItemClickSurface>();
                    _clickSurface.Bind(this, HandlePointerClick);
                }
            }
        }

        private void HandlePointerClick(int fashionId)
        {
            if (fashionId <= 0 || fashionId != FashionId) return;
            GameLog.Info("Fashion", "FashionMain PointerClick 命中条目 fashion={0}", fashionId);
            _onClick?.Invoke(fashionId);
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
            RefreshIcon(string.IsNullOrEmpty(icon) ? "" : GameResPath.GetGoodsIconPath(icon));

            // 老端 fashion_plate_image 按 config_goods.color 使用 com_goods_plate_N，
            // 不能让全部时装永久停在 Prefab 的 plate_1。
            int color = GoodsModel.GetColor(fashionId);
            RefreshPlate(GameResPath.GetIcon("common", "com_goods_plate_" + color));

            SetGray(fashion_icon_image, !activated);
            SetGray(fashion_plate_image, !activated);
        }

        private async void RefreshIcon(string key)
        {
            if (fashion_icon_image == null) return;
            if (_iconKey == key)
            {
                if (_iconSprite != null) ApplySprite(fashion_icon_image, _iconSprite);
                if (_iconLoading || _iconSprite != null) return;
            }

            _iconKey = key ?? "";
            int version = ++_iconVersion;
            _iconLoading = !string.IsNullOrEmpty(_iconKey);
            ReleaseIconSprite();
            fashion_icon_image.enabled = false;
            if (!_iconLoading) return;

            Sprite next = await ResManager.LoadAsync<Sprite>(_iconKey);
            if (this == null || version != _iconVersion)
            {
                if (next != null) ResManager.Release(next);
                return;
            }
            _iconLoading = false;
            if (next == null) return;
            _iconSprite = next;
            ApplySprite(fashion_icon_image, next);
        }

        private async void RefreshPlate(string key)
        {
            if (fashion_plate_image == null) return;
            if (_plateKey == key)
            {
                if (_plateSprite != null) ApplySprite(fashion_plate_image, _plateSprite);
                if (_plateLoading || _plateSprite != null) return;
            }

            _plateKey = key ?? "";
            int version = ++_plateVersion;
            _plateLoading = !string.IsNullOrEmpty(_plateKey);
            ReleasePlateSprite();
            fashion_plate_image.enabled = false;
            if (!_plateLoading) return;

            Sprite next = await ResManager.LoadAsync<Sprite>(_plateKey);
            if (this == null || version != _plateVersion)
            {
                if (next != null) ResManager.Release(next);
                return;
            }
            _plateLoading = false;
            if (next == null) return;
            _plateSprite = next;
            ApplySprite(fashion_plate_image, next);
        }

        private static void ApplySprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null) return;
            image.sprite = sprite;
            image.enabled = true;
        }

        private void ReleaseIconSprite()
        {
            if (_iconSprite == null) return;
            ResManager.Release(_iconSprite);
            _iconSprite = null;
        }

        private void ReleasePlateSprite()
        {
            if (_plateSprite == null) return;
            ResManager.Release(_plateSprite);
            _plateSprite = null;
        }

        private void OnDestroy()
        {
            ++_iconVersion;
            ++_plateVersion;
            _iconLoading = false;
            _plateLoading = false;
            ReleaseIconSprite();
            ReleasePlateSprite();
        }

        /// <summary>对标老端 Util.SetImageGray，不能用透明度 tint 冒充灰阶。</summary>
        private static void SetGray(UnityEngine.UI.Image img, bool gray)
        {
            UIGrayStyle.Apply(img, gray);
        }
    }

    /// <summary>
    /// 时装横向列表的专用点击面。业务身份直接取当前命中格的 FashionId，不再依赖外层循环闭包或
    /// 固定屏幕坐标。WebGL 中父 ScrollRect 完成拖动后仍可能把 IPointerClick 派发给子项，
    /// 因此这里在播放声音和发送协议前再次核对按下/抬起位移。
    /// 本组件同时是该格唯一的点击音消费者，不保留 Button，避免全局 Button 反馈重复播放。
    /// </summary>
    internal sealed class FashionItemClickSurface : MonoBehaviour, IPointerClickHandler
    {
        private FashionItem _owner;
        private Action<int> _onClick;

        public void Bind(FashionItem owner, Action<int> onClick)
        {
            _owner = owner;
            _onClick = onClick;
            FashionSingleClickTarget.DisableButtonFeedback(gameObject);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
            float threshold = EventSystem.current != null
                ? Mathf.Max(1f, EventSystem.current.pixelDragThreshold)
                : 10f;
            if (eventData.dragging
                || (eventData.position - eventData.pressPosition).sqrMagnitude > threshold * threshold)
                return;
            int fashionId = _owner != null ? _owner.FashionId : 0;
            if (fashionId <= 0) return;
            _ = AudioManager.PlayUi("2_dianji");
            _onClick?.Invoke(fashionId);
        }
    }

    /// <summary>Fashion 页面私有的单次点击绑定；不用 Button，避免 AudioRuntime 再挂第二个声音消费者。</summary>
    internal sealed class FashionSingleClickTarget : MonoBehaviour, IPointerClickHandler
    {
        private Action _onClick;

        internal static void Bind(Graphic graphic, Action onClick)
        {
            if (graphic == null) return;
            graphic.raycastTarget = true;
            DisableButtonFeedback(graphic.gameObject);
            FashionSingleClickTarget target = graphic.GetComponent<FashionSingleClickTarget>();
            if (target == null) target = graphic.gameObject.AddComponent<FashionSingleClickTarget>();
            target._onClick = onClick;
        }

        internal static void DisableButtonFeedback(GameObject target)
        {
            if (target == null) return;
            foreach (Button button in target.GetComponents<Button>())
            {
                button.onClick.RemoveAllListeners();
                button.enabled = false;
            }
            foreach (UIButtonSoundFeedback feedback in target.GetComponents<UIButtonSoundFeedback>())
                feedback.enabled = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
            _ = AudioManager.PlayUi("2_dianji");
            _onClick?.Invoke();
        }
    }
}
