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
        public enum Screen { Equip, Forge, Imprint }

        private bool _listening, _shown;
        private Transform _viewGp;
        private Image _closeBtn;
        private GameObject _equipTemplate, _forgeTemplate, _imprintTemplate, _content, _contentTemplate;
        private Screen _screen = Screen.Equip;
        private int _selectedPosition = 1;

        public Screen CurrentScreen => _screen;
        public int SelectedPosition => _selectedPosition;

        protected override void OnInit()
        {
            CacheNodes();
            UIUtil.AddClick(_closeBtn, HandleClose);
        }

        protected override void OnShow(object args)
        {
            _shown = true;
            _screen = Screen.Equip;
            _selectedPosition = 1;
            Subscribe();
            BabyController.Instance.RequestEquipInfo();
            ShowEquip();
            _ = EnsureConfigs();
        }

        protected override void OnHide() { _shown = false; Unsubscribe(); Clear(); }
        protected override void OnDispose() { _shown = false; Unsubscribe(); Clear(); }

        public void ShowEquip()
        {
            _screen = Screen.Equip;
            CacheNodes();
            Swap(_equipTemplate);
            BabyEquipView view = _content != null ? _content.GetComponent<BabyEquipView>() : null;
            if (view == null) return;
            view.SetSelectedPosition(_selectedPosition);
            view.ConfigureEntryCallbacks(ShowForge, ShowImprint);
            view.Refresh(BabyModel.Instance.Equip, BabyModel.Instance.Basic);
        }

        public void ShowForge(int positionId)
        {
            _selectedPosition = NormalizePosition(positionId);
            _screen = Screen.Forge;
            CacheNodes();
            Swap(_forgeTemplate);
            BabyForgeView view = _content != null ? _content.GetComponent<BabyForgeView>() : null;
            if (view != null) view.SetPositionId(_selectedPosition);
        }

        public void ShowImprint(int positionId)
        {
            _selectedPosition = NormalizePosition(positionId);
            _screen = Screen.Imprint;
            CacheNodes();
            Swap(_imprintTemplate);
            BabyImprintView view = _content != null ? _content.GetComponent<BabyImprintView>() : null;
            if (view != null) view.SetPositionId(_selectedPosition);
        }

        private void HandleClose()
        {
            if (_screen != Screen.Equip) ShowEquip();
            else ViewManager.Close<BabyEquipFuncView>();
        }

        private void Subscribe()
        {
            if (_listening) return;
            _listening = true;
            EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnUpdate);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, Refresh);
            EventDispatcher.On(GlobalEvent.EVT_BABY_EQUIP_BAG_UPDATE, Refresh);
            EventDispatcher.On(GlobalEvent.EVT_BABY_EQUIP_UPDATE, Refresh);
        }

        private void Unsubscribe()
        {
            if (!_listening) return;
            _listening = false;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, Refresh);
            EventDispatcher.Off(GlobalEvent.EVT_BABY_EQUIP_BAG_UPDATE, Refresh);
            EventDispatcher.Off(GlobalEvent.EVT_BABY_EQUIP_UPDATE, Refresh);
        }

        private void OnUpdate(int command)
        {
            if (command == Proto.BABY_EQUIP_UPGRADE && _shown && _screen == Screen.Forge)
            {
                if (_content != null) _content.GetComponent<BabyForgeView>()?.OnUpgradeResult();
                return;
            }
            if ((command == Proto.BABY_EQUIP_INFO || command == Proto.BABY_EQUIP_WEAR || command == Proto.BABY_STAGE_INFO) && _shown) Refresh();
        }

        private async System.Threading.Tasks.Task EnsureConfigs()
        {
            await GoodsModel.EnsureLoaded();
            await BabyEquipConfigs.EnsureLoaded();
            await BabyEquipUpgradeConfigs.EnsureLoaded();
            if (_shown) Refresh();
        }

        private void Refresh()
        {
            if (!_shown) return;
            if (_screen == Screen.Equip) ShowEquip();
            else if (_screen == Screen.Forge) ShowForge(_selectedPosition);
            else ShowImprint(_selectedPosition);
        }

        private void Swap(GameObject template)
        {
            CacheNodes();
            if (_viewGp == null || template == null) return;
            if (_content != null && _contentTemplate == template) return;
            if (_content != null) Clear();
            _content = Instantiate(template, _viewGp);
            _contentTemplate = template;
            _content.SetActive(true);
        }

        private void Clear()
        {
            if (_content == null) return;
            _content.SetActive(false);
            if (Application.isPlaying) Destroy(_content); else DestroyImmediate(_content);
            _content = null;
            _contentTemplate = null;
        }

        private void CacheNodes()
        {
            foreach (Transform node in GetComponentsInChildren<Transform>(true))
            {
                if (node.name == "viewGp") _viewGp = node;
                else if (node.name == "closeBtn") _closeBtn = node.GetComponent<Image>();
                else if (node.parent != null && node.parent.name == "__Templates")
                {
                    if (node.name == "BabyEquipView") _equipTemplate = node.gameObject;
                    else if (node.name == "BabyForgeView") _forgeTemplate = node.gameObject;
                    else if (node.name == "BabyImprintView") _imprintTemplate = node.gameObject;
                }
            }
        }

        private static int NormalizePosition(int positionId) { return positionId >= 1 && positionId <= 6 ? positionId : 1; }
    }
}
