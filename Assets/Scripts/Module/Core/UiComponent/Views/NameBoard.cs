using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.UiComponent;
using Shenxiao.Module.Core.Designation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.UiComponent
{
    /// <summary>
    /// 头顶名字血条板(对标老客户端 scene/sceneboard/NameBoard.ts):单位头顶的 名字/称号/结社/勋章/VIP/血条 动态板。
    ///
    /// 老端绝大多数子件运行时按需 CreatChilds 动态创建 + 场景挂载(SceneManager/CameraManager 驱动),prefab 只有
    /// _gp_parent 容器 + BigBloodBar/ProgressBar2 两个模板。
    /// 当前已落血条和称号。称号节点按老端运行时创建：静态图/伴侣文字或动态 UIEffectStage，
    /// 由 SceneDesignationPresenter 挂到场景层并跟随主角/其他玩家；其余名字/结社/勋章/VIP 继续独立迁移。
    /// </summary>
    public sealed class NameBoard : NameBoardBind
    {
        private ProgressBar2 _hpBar;
        private RectTransform _designationRoot;
        private RectTransform _effectHost;
        private Image _designationImage;
        private TextMeshProUGUI _designationText;
        private UIEffectStage.Handle _designationEffect;
        private int _designationVersion;

        public uint DesignationId { get; private set; }
        public bool HasDesignationVisual => DesignationId != 0
            && ((_designationImage != null && _designationImage.gameObject.activeSelf)
                || (_effectHost != null && _effectHost.gameObject.activeSelf));

        protected override void OnInit()
        {
            if (_tpl_BigBloodBar != null) _tpl_BigBloodBar.SetActive(false);
            if (_tpl_ProgressBar2 != null) _tpl_ProgressBar2.SetActive(false);
        }

        /// <summary>显示/隐藏血条(对标 SetHpVisible:首次显示时从 _tpl_ProgressBar2 克隆)。</summary>
        public void SetHpVisible(bool visible)
        {
            if (visible && _hpBar == null && _tpl_ProgressBar2 != null && _gp_parent != null)
            {
                GameObject go = Instantiate(_tpl_ProgressBar2, _gp_parent);
                go.SetActive(true);
                _hpBar = go.GetComponent<ProgressBar2>();
            }
            if (_hpBar != null) _hpBar.gameObject.SetActive(visible);
        }

        /// <summary>设血量(对标 SetHp → hp_bar.SetValue)。</summary>
        public void SetHp(long hp, long maxHp)
        {
            if (_hpBar != null) _hpBar.SetValue(hp, maxHp);
        }

        public async Task<bool> SetDesignationAsync(uint id, FigureProto figure)
        {
            int version = ++_designationVersion;
            DisposeDesignationEffect();
            DesignationId = id;
            EnsureDesignationNodes();
            HideDesignationNodes();
            if (id == 0) return false;

            await DesignationConfigs.EnsureLoaded();
            if (version != _designationVersion || this == null) return false;
            DesignationConfigs.Row row = DesignationConfigs.Get(id);
            if (row == null || string.IsNullOrWhiteSpace(row.ResourceId))
            {
                DesignationId = 0;
                return false;
            }

            if (row.Type == 1)
            {
                DesignationEffectDisplayConfigs.Display display = DesignationEffectDisplayConfigs.Get(
                    id, DesignationEffectDisplayConfigs.Surface.NameBoard);
                _effectHost.anchoredPosition = new Vector2(
                    -10f, Mathf.Max(0f, (150f - display.Height) * 0.5f));
                UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                    row.ResourceId.Trim(), _effectHost,
                    DesignationEffectDisplayConfigs.ToUnityPosition(display),
                    Vector3.one * (display.Scale * 0.8f),
                    0f, new Vector2(237f, 150f));
                if (version != _designationVersion || this == null)
                {
                    handle?.Dispose();
                    return false;
                }
                _designationEffect = handle;
                _effectHost.gameObject.SetActive(handle != null);
                return handle != null;
            }

            string path = GameResPath.GetDesignImage(row.ResourceId.Trim());
            bool loaded = await ResManager.SetImageAsync(_designationImage, path, nativeSize: true);
            if (version != _designationVersion || this == null) return false;
            if (!loaded)
            {
                DesignationId = 0;
                return false;
            }

            _designationImage.raycastTarget = false;
            _designationImage.preserveAspect = row.MainType != 7;
            _designationImage.type = row.MainType == 7 ? Image.Type.Sliced : Image.Type.Simple;
            RectTransform imageRect = _designationImage.rectTransform;
            Vector2 native = imageRect.sizeDelta;
            if (native.x <= 0f || native.y <= 0f) native = new Vector2(200f, 100f);
            float fit = Mathf.Min(1f, 237f / native.x, 100f / native.y);
            imageRect.sizeDelta = native * fit;

            if (row.MainType == 7)
            {
                string marriageName = figure?.MarriageName ?? string.Empty;
                _designationText.text = (string.IsNullOrEmpty(marriageName) ? "某某某" : marriageName) + "的伴侣";
                _designationText.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                imageRect.sizeDelta = new Vector2(
                    Mathf.Clamp(_designationText.preferredWidth + 100f, 100f, 237f),
                    Mathf.Min(100f, Mathf.Max(40f, imageRect.sizeDelta.y)));
            }
            _designationImage.gameObject.SetActive(true);
            return true;
        }

        public void ClearDesignation()
        {
            _designationVersion++;
            DesignationId = 0;
            DisposeDesignationEffect();
            HideDesignationNodes();
        }

        protected override void OnDispose()
        {
            ClearDesignation();
            base.OnDispose();
        }

        private void EnsureDesignationNodes()
        {
            if (_designationRoot != null) return;
            RectTransform parent = _gp_parent != null ? _gp_parent : transform as RectTransform;

            var rootGo = new GameObject("Designation", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            _designationRoot = (RectTransform)rootGo.transform;
            _designationRoot.anchorMin = _designationRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _designationRoot.pivot = new Vector2(0.5f, 0.5f);
            _designationRoot.sizeDelta = new Vector2(237f, 150f);

            var imageGo = new GameObject("DesignationImage", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            imageGo.transform.SetParent(_designationRoot, false);
            _designationImage = imageGo.GetComponent<Image>();
            RectTransform imageRt = _designationImage.rectTransform;
            imageRt.anchorMin = imageRt.anchorMax = new Vector2(0.5f, 0.5f);
            imageRt.pivot = new Vector2(0.5f, 0.5f);
            imageRt.sizeDelta = new Vector2(200f, 100f);

            var textGo = new GameObject("DesignationMarriageText", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(_designationRoot, false);
            _designationText = textGo.GetComponent<TextMeshProUGUI>();
            RectTransform textRt = _designationText.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            _designationText.fontSize = 22f;
            _designationText.color = Color.white;
            _designationText.alignment = TextAlignmentOptions.Center;
            _designationText.textWrappingMode = TextWrappingModes.NoWrap;
            _designationText.raycastTarget = false;

            var effectGo = new GameObject("DesignationEffect", typeof(RectTransform));
            effectGo.transform.SetParent(_designationRoot, false);
            _effectHost = (RectTransform)effectGo.transform;
            _effectHost.anchorMin = _effectHost.anchorMax = new Vector2(0.5f, 0.5f);
            _effectHost.pivot = new Vector2(0.5f, 0.5f);
            _effectHost.sizeDelta = new Vector2(237f, 150f);
        }

        private void HideDesignationNodes()
        {
            if (_designationImage != null)
            {
                _designationImage.sprite = null;
                _designationImage.gameObject.SetActive(false);
            }
            if (_designationText != null)
            {
                _designationText.text = string.Empty;
                _designationText.gameObject.SetActive(false);
            }
            if (_effectHost != null) _effectHost.gameObject.SetActive(false);
        }

        private void DisposeDesignationEffect()
        {
            _designationEffect?.Dispose();
            _designationEffect = null;
        }
    }
}
