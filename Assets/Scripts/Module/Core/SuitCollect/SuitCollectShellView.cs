using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.SuitCollect
{
    /// <summary>
    /// 套装收集临时壳(TEMP SHELL,同 PartnerShellView/ItemTipsView 约定:代码建 uGUI、数据全真、样式从简待用户重做 UI)。
    /// 对标老端套装收集主界面的最小可用面:套装1(数据来自 15256 真实回包 + config_suit_clt 真名)
    /// 显示「已激活 cur_stage/4 件」+ [激活下一阶](发 15257 stage=cur_stage+1)。DoTask ctype 84
    /// (主线 100391「激活二忍套装4件」)由此进入。无服务器数据(HasData=false)时如实显示等待 15256,不造假。
    /// </summary>
    public static class SuitCollectShellView
    {
        private const int SuitId = 1;
        private const int Career = 1;   // 展示用职业(config_suit_clt 主键含 career;本轮固定读职业1名称,不影响真数据)
        private const int FullStage = 4;

        private static GameObject _root;
        private static Transform _bodyParent;
        private static TMP_FontAsset _font;
        private static Material _fontMat;
        private static TextMeshProUGUI _bodyText;
        private static GameObject _activateBtn;
        private static bool _listening;

        public static void Show()
        {
            EnsureBuilt();
            if (_root == null) return;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            if (!_listening)
            {
                EventDispatcher.On(GlobalEvent.EVT_SUIT_CLT_UPDATE, Rebuild);
                _listening = true;
            }
            Rebuild();
            GameLog.Info("SuitCollect", "SuitCollectShellView 打开: hasData={0}", SuitCollectModel.Instance.HasData);
        }

        public static void Close()
        {
            if (_listening)
            {
                EventDispatcher.Off(GlobalEvent.EVT_SUIT_CLT_UPDATE, Rebuild);
                _listening = false;
            }
            if (_root != null) _root.SetActive(false);
        }

        private static void Rebuild()
        {
            if (_root == null || !_root.activeSelf) return;

            SuitCollectModel model = SuitCollectModel.Instance;
            if (!model.HasData)
            {
                _bodyText.text = "等待 15256(需活服)";
                _activateBtn.SetActive(false);
                return;
            }

            int curStage = model.GetCurStage(SuitId);
            string name = SuitCollectConfigs.GetName(SuitId, Career);
            _bodyText.text = name + "\n<color=#ffe222>已激活 " + curStage + "/" + FullStage + " 件</color>";
            _activateBtn.SetActive(true);
        }

        // ---- 构建(代码建 uGUI;同 PartnerShellView TEMP 壳约定)----

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("SuitCollect", "SuitCollectShellView 无法构建:UI Window 层未就绪");
                return;
            }

            _root = NewRect("SuitCollectShellView(TempShell)", parent, Vector2.zero, Vector2.one);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(560f, 420f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            TextMeshProUGUI title = NewText("Title", panel.transform, 30, TextAlignmentOptions.Top);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(-40f, 44f);
            title.text = "套装收集";
            title.color = new Color(1f, 0.86f, 0.45f);
            title.fontStyle = FontStyles.Bold;

            GameObject body = NewRect("Body", panel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f));
            var brt = (RectTransform)body.transform;
            brt.offsetMin = new Vector2(20f, 140f); brt.offsetMax = new Vector2(-20f, -76f);
            _bodyParent = body.transform;
            _bodyText = NewText("BodyText", body.transform, 26, TextAlignmentOptions.Center);
            Stretch(_bodyText.rectTransform);
            _bodyText.color = Color.white;

            _activateBtn = new GameObject("BtnActivate", typeof(RectTransform));
            _activateBtn.transform.SetParent(panel.transform, false);
            var actRt = (RectTransform)_activateBtn.transform;
            actRt.anchorMin = actRt.anchorMax = new Vector2(0.5f, 0f);
            actRt.pivot = new Vector2(0.5f, 0f);
            actRt.sizeDelta = new Vector2(220f, 56f);
            actRt.anchoredPosition = new Vector2(0f, 78f);
            Image actImg = _activateBtn.AddComponent<Image>();
            actImg.color = new Color(0.22f, 0.42f, 0.24f);
            actImg.raycastTarget = true;
            TextMeshProUGUI actLbl = NewText("Label", _activateBtn.transform, 26, TextAlignmentOptions.Center);
            Stretch(actLbl.rectTransform);
            actLbl.text = "激活下一阶";
            actLbl.color = Color.white;
            UIUtil.AddClick(actImg, OnActivateClick);

            GameObject closeBtn = NewRect("Close", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var closeRt = (RectTransform)closeBtn.transform;
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.sizeDelta = new Vector2(200f, 56f);
            closeRt.anchoredPosition = new Vector2(0f, 14f);
            Image closeImg = closeBtn.AddComponent<Image>();
            closeImg.color = new Color(0.20f, 0.30f, 0.48f, 1f);
            TextMeshProUGUI closeLbl = NewText("Label", closeBtn.transform, 26, TextAlignmentOptions.Center);
            Stretch(closeLbl.rectTransform);
            closeLbl.text = "关闭";
            closeLbl.color = Color.white;
            UIUtil.AddClick(closeImg, Close);
        }

        private static void OnActivateClick()
        {
            int curStage = SuitCollectModel.Instance.GetCurStage(SuitId);
            SuitCollectController.Instance.Activate(SuitId, curStage + 1);
        }

        private static GameObject NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.alignment = align;
            t.richText = true;
            ApplyFont(t);
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void ApplyFont(TextMeshProUGUI t)
        {
            if (_font == null)
            {
                foreach (TextMeshProUGUI candidate in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                {
                    if (candidate != t) { _font = candidate.font; _fontMat = candidate.fontSharedMaterial; break; }
                }
            }
            if (_font != null) t.font = _font;
            if (_fontMat != null) t.fontSharedMaterial = _fontMat;
        }
    }
}
