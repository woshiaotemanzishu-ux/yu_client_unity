using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Suit;
using UnityEngine;

namespace Shenxiao.Module.Core.Resonance
{
    /// <summary>
    /// SuitModule 主页面的非视觉生命周期宿主。可编辑 Prefab 仍由 EquipSuitMianViewBind 保存全部节点引用；
    /// 本组件只把 BaseView 的 Show/Hide 生命周期转交给 ResonancePresenter。
    /// </summary>
    public sealed class ResonanceMainView : BaseView
    {
        private EquipSuitMianViewBind _bind;
        private EquipSuitPreviewTipsBind _preview;
        private EquipSuitReturnViewBind _return;
        private GameObject _previewMask;
        private GameObject _returnMask;
        private ResonancePresenter _presenter;

        public void Configure(EquipSuitPreviewTipsBind preview, EquipSuitReturnViewBind returnView,
            GameObject previewMask, GameObject returnMask)
        {
            _preview = preview;
            _return = returnView;
            _previewMask = previewMask;
            _returnMask = returnMask;
            _presenter?.Configure(preview, returnView, previewMask, returnMask);
        }

        public void SetTab(int index) => _presenter?.SetTab(index);

        public void ClosePopups() => _presenter?.ClosePopups();

        protected override void OnInit()
        {
            _bind = GetComponent<EquipSuitMianViewBind>();
            if (_bind == null)
            {
                Debug.LogError("[Resonance] EquipSuitMianViewBind missing", this);
                return;
            }
            // EquipSuitMianViewBind 位于 Shenxiao.Generated 程序集，跨程序集不能调用
            // BaseView 的 internal 初始化入口。公开 Show() 会完成同一套 BindNodes/OnInit，
            // 且该 Bind 本身不负责显隐语义，页面生命周期仍统一由本组件接管。
            _bind.Show();
            if (_bind._tpl_EquipNewSuitAttrItem != null) _bind._tpl_EquipNewSuitAttrItem.SetActive(false);
            if (_bind._tpl_EquipSuitPosItem != null) _bind._tpl_EquipSuitPosItem.SetActive(false);
            if (_bind._tpl_EquipmentItem != null) _bind._tpl_EquipmentItem.SetActive(false);
            if (_bind._tpl_GiftPushIcon != null) _bind._tpl_GiftPushIcon.SetActive(false);
            if (_bind._tpl_EquipSuitCostItem != null) _bind._tpl_EquipSuitCostItem.SetActive(false);
            _presenter = new ResonancePresenter(_bind);
            _presenter.Configure(_preview, _return, _previewMask, _returnMask);
        }

        protected override void OnShow(object args) => _presenter?.Show();

        protected override void OnHide() => _presenter?.Hide();

        protected override void OnDispose()
        {
            _presenter?.Dispose();
            _presenter = null;
        }
    }
}
