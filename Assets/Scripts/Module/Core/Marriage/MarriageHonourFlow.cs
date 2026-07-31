using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>人物页“名”按钮对应的名誉等级弹层。</summary>
    public static class MarriageHonourFlow
    {
        private static readonly List<GameObject> RuntimeItems = new List<GameObject>();
        private static GameObject _moduleRoot;
        private static MarriageHonourViewBind _view;
        private static Image _mask;
        private static bool _loading;

        public static void Show() => _ = ShowAsync();

        public static void Close()
        {
            if (_view != null && _view.IsShown) _view.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
        }

        private static async Task ShowAsync()
        {
            await Task.WhenAll(MarriageConfigs.EnsureLoaded(), GoodsModel.EnsureLoaded());
            if (!await EnsureViewAsync()) return;
            Render();
            _moduleRoot.SetActive(true);
            if (_mask != null)
            {
                _mask.gameObject.SetActive(true);
                _mask.transform.SetAsFirstSibling();
            }
            _view.Show();
            _view.transform.SetAsLastSibling();
        }

        private static async Task<bool> EnsureViewAsync()
        {
            if (_moduleRoot != null && _view != null) return true;
            if (_loading) return false;
            _loading = true;
            try
            {
                string key = GameResPath.GetUIPrefab("marriage", "MarriageModule");
                _moduleRoot = await ResManager.InstantiateAsync(
                    key, ViewManager.GetLayer(UILayer.Popup));
                if (_moduleRoot == null)
                {
                    GameLog.Error("Marriage", "MarriageModule 加载失败: {0}", key);
                    return false;
                }
                _moduleRoot.name = "MarriageModule(Honour)";
                foreach (BaseView view in _moduleRoot.GetComponentsInChildren<BaseView>(true))
                    view.gameObject.SetActive(false);
                _view = _moduleRoot.GetComponentInChildren<MarriageHonourViewBind>(true);
                if (_view == null)
                {
                    GameLog.Error("Marriage", "MarriageModule 缺 MarriageHonourViewBind");
                    ResManager.ReleaseInstance(_moduleRoot);
                    _moduleRoot = null;
                    return false;
                }
                if (_view._btn_close != null)
                {
                    _view._btn_close.raycastTarget = true;
                    UIUtil.AddClick(_view._btn_close, Close);
                }
                BindButton(_view._btn_go, () =>
                {
                    Close();
                    MarriageFlow.OpenSubDeferred("MarriageFlowerView");
                });
                if (_view._tpl_MarriageHonourItem != null)
                    _view._tpl_MarriageHonourItem.SetActive(false);
                EnsureMask();
                _moduleRoot.SetActive(false);
                return true;
            }
            finally
            {
                _loading = false;
            }
        }

        private static void EnsureMask()
        {
            if (_mask != null || _moduleRoot == null) return;
            var go = new GameObject("__MarriageHonourMask", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(_moduleRoot.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _mask = go.GetComponent<Image>();
            _mask.color = new Color(0f, 0f, 0f, 0.62f);
            _mask.raycastTarget = true;
            UIUtil.AddClick(_mask, Close);
        }

        private static void Render()
        {
            foreach (GameObject item in RuntimeItems)
                if (item != null) Object.Destroy(item);
            RuntimeItems.Clear();

            long fame = MarriageModel.Instance.Flower?.Fame ?? 0L;
            if (_view._lb_honour != null) _view._lb_honour.text = fame.ToString();
            if (_view._tpl_MarriageHonourItem == null || _view._gp_con == null
                || _view._gp_con.content == null)
            {
                return;
            }

            List<MarriageConfigs.FameLevelRow> rows = MarriageConfigs.GetFameLevels();
            RectTransform content = _view._gp_con.content;
            const float itemHeight = 93f;
            for (int i = 0; i < rows.Count; i++)
            {
                MarriageConfigs.FameLevelRow row = rows[i];
                GameObject go = Object.Instantiate(
                    _view._tpl_MarriageHonourItem, content, false);
                RuntimeItems.Add(go);
                go.SetActive(true);
                MarriageHonourItemBind item = go.GetComponent<MarriageHonourItemBind>();
                if (item == null) continue;
                item.Show();
                if (item._lb_title != null) item._lb_title.text = row.Name;
                if (item._lb_honour != null) item._lb_honour.text = row.Fame.ToString();
                if (item._lb_attr != null)
                {
                    item._lb_attr.richText = true;
                    item._lb_attr.text = FormatAttrs(row.Attr);
                }
                if (item._img_unlock != null)
                    item._img_unlock.gameObject.SetActive(fame >= row.Fame);

                RectTransform rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(0f, -i * itemHeight);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);
            }
            content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, Mathf.Max(1f, rows.Count * itemHeight));
            _view._gp_con.verticalNormalizedPosition = 1f;
        }

        private static string FormatAttrs(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw == "[]") return string.Empty;
            try
            {
                JArray attrs = JArray.Parse(raw);
                var parts = new List<string>();
                foreach (JToken token in attrs)
                {
                    if (!(token is JObject row)) continue;
                    int id = row["0"]?.Value<int>() ?? 0;
                    long value = row["1"]?.Value<long>() ?? 0L;
                    parts.Add(GoodsModel.GetAttrName(id) + "+"
                        + GoodsModel.FormatAttrValue(id, value));
                }
                return string.Join("   ", parts);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void BindButton(Component target, System.Action action)
        {
            if (target == null || action == null) return;
            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                image = target.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
            }
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }

        internal static void Reset()
        {
            Close();
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _view = null;
            _mask = null;
            _loading = false;
            RuntimeItems.Clear();
        }
    }
}
