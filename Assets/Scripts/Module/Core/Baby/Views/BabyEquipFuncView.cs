using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    [UIView("prefabs/ui/baby/babyequipfuncview")]
    public sealed class BabyEquipFuncView : BabyEquipFuncViewBind
    {
        private bool _listening; private Transform _viewGp; private Image _closeBtn; private GameObject _template; private GameObject _content;
        protected override void OnInit() { CacheNodes(); UIUtil.AddClick(_closeBtn, () => ViewManager.Close<BabyEquipFuncView>()); }
        protected override void OnShow(object args) { Subscribe(); BabyController.Instance.RequestEquipInfo(); Refresh(); }
        protected override void OnHide() { Unsubscribe(); Clear(); }
        protected override void OnDispose() { Unsubscribe(); Clear(); }
        private void Subscribe() { if (_listening) return; _listening = true; EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnUpdate); }
        private void Unsubscribe() { if (!_listening) return; _listening = false; EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnUpdate); }
        private void OnUpdate(int command) { if (command == Proto.BABY_EQUIP_INFO && gameObject.activeInHierarchy) Refresh(); }
        private void Refresh() { CacheNodes(); Clear(); if (_viewGp == null || _template == null) return; _content = Instantiate(_template, _viewGp); _content.SetActive(true); _content.GetComponent<BabyEquipView>()?.Refresh(BabyModel.Instance.Equip, BabyModel.Instance.Basic); }
        private void Clear() { if (_content == null) return; if (Application.isPlaying) Destroy(_content); else DestroyImmediate(_content); _content = null; }
        private void CacheNodes() { if (_template != null) return; foreach (Transform node in GetComponentsInChildren<Transform>(true)) { if (node.name == "viewGp") _viewGp = node; else if (node.name == "closeBtn") _closeBtn = node.GetComponent<Image>(); else if (node.name == "BabyEquipView" && node.parent != null && node.parent.name == "__Templates") _template = node.gameObject; } }
    }
}
