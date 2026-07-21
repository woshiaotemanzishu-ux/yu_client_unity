using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    [UIView("prefabs/ui/baby/babyequipfuncview")]
    public sealed class BabyEquipFuncView : BabyEquipFuncViewBind
    {
        private bool _listening; private bool _shown; private Transform _viewGp; private Image _closeBtn; private GameObject _template; private GameObject _content;
        protected override void OnInit() { CacheNodes(); UIUtil.AddClick(_closeBtn, () => ViewManager.Close<BabyEquipFuncView>()); }
        protected override void OnShow(object args) { _shown = true; Subscribe(); BabyController.Instance.RequestEquipInfo(); Refresh(); _ = EnsureConfigs(); }
        protected override void OnHide() { _shown = false; Unsubscribe(); Clear(); }
        protected override void OnDispose() { _shown = false; Unsubscribe(); Clear(); }
        private void Subscribe() { if (_listening) return; _listening = true; EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnUpdate); EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, Refresh); EventDispatcher.On(GlobalEvent.EVT_BABY_EQUIP_BAG_UPDATE, Refresh); EventDispatcher.On(GlobalEvent.EVT_BABY_EQUIP_UPDATE, Refresh); }
        private void Unsubscribe() { if (!_listening) return; _listening = false; EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnUpdate); EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, Refresh); EventDispatcher.Off(GlobalEvent.EVT_BABY_EQUIP_BAG_UPDATE, Refresh); EventDispatcher.Off(GlobalEvent.EVT_BABY_EQUIP_UPDATE, Refresh); }
        private void OnUpdate(int command) { if (command == Proto.BABY_EQUIP_UPGRADE) _content?.GetComponent<BabyEquipView>()?.OnUpgradeResult(); if ((command == Proto.BABY_EQUIP_INFO || command == Proto.BABY_EQUIP_WEAR || command == Proto.BABY_EQUIP_UPGRADE || command == Proto.BABY_STAGE_INFO) && _shown) Refresh(); }
        private async System.Threading.Tasks.Task EnsureConfigs() { await GoodsModel.EnsureLoaded(); await BabyEquipConfigs.EnsureLoaded(); await BabyEquipUpgradeConfigs.EnsureLoaded(); if (_shown) Refresh(); }
        private void Refresh() { CacheNodes(); if (_viewGp == null || _template == null) return; if (_content == null) { _content = Instantiate(_template, _viewGp); _content.SetActive(true); } _content.GetComponent<BabyEquipView>()?.Refresh(BabyModel.Instance.Equip, BabyModel.Instance.Basic); }
        private void Clear() { if (_content == null) return; _content.GetComponent<BabyEquipView>()?.ResetUpgradeState(); if (Application.isPlaying) Destroy(_content); else DestroyImmediate(_content); _content = null; }
        private void CacheNodes() { if (_template != null) return; foreach (Transform node in GetComponentsInChildren<Transform>(true)) { if (node.name == "viewGp") _viewGp = node; else if (node.name == "closeBtn") _closeBtn = node.GetComponent<Image>(); else if (node.name == "BabyEquipView" && node.parent != null && node.parent.name == "__Templates") _template = node.gameObject; } }
    }
}
