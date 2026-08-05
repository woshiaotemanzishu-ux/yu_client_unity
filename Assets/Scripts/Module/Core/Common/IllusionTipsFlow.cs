using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Designation;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 幻化/时装大详情卡。视觉树完全复用 CommonModule.prefab/IllusionTips；运行时代码只填数据、
    /// 挂模型与处理关闭，避免再把幻化物品错误路由到小型 ItemTipsView。
    /// </summary>
    public static class IllusionTipsFlow
    {
        private static readonly UIModelStage ModelStage = new UIModelStage();
        private static readonly Regex FontOpen = new Regex("<font\\s+color=['\"]?([^'\">]+)['\"]?>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static GameObject _moduleRoot;
        private static IllusionTipsBind _view;
        private static ItemTipsModalLayout _layout;
        private static FightingShowSmallItem _fight;
        private static bool _loading;
        private static bool _subscribed;
        private static int _pendingTypeId;
        private static int _pendingFashionPos;
        private static int _requestId;
        private static int _modelType;
        private static int _effectCount;
        private static bool _modelRendered;
        private static bool _designationRendered;
        private static UIEffectStage.Handle _designationEffect;

        public static int CurrentTypeId => _pendingTypeId;
        public static int CurrentModelType => _modelType;
        public static int CurrentEffectCount => _effectCount;
        public static IllusionTipsBind ActiveView => _view != null && _view.gameObject.activeInHierarchy ? _view : null;
        public static bool IsModelReady => _modelRendered && ActiveView != null && _view.roleCon != null
            && _view.roleCon.GetComponentInChildren<RawImage>(true)?.texture != null;
        public static bool IsVisualReady => (IsModelReady || _designationRendered) && _view._img_bg != null
            && _view._img_bg.enabled && _view._img_bg.sprite != null && !_view._img_bg.canvasRenderer.cull;

        public static void Show(int typeId, int fashionPos = 0)
        {
            if (typeId <= 0) return;
            _pendingTypeId = typeId;
            _pendingFashionPos = fashionPos;
            _ = ShowAsync();
        }

        public static void Close()
        {
            ++_requestId;
            Unsubscribe();
            ModelStage.ClearStage();
            _designationEffect?.Dispose();
            _designationEffect = null;
            _modelType = 0;
            _effectCount = 0;
            _modelRendered = false;
            _designationRendered = false;
            if (_view != null && _view.IsShown) _view.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
        }

        public static void Reset()
        {
            Close();
            ModelStage.Dispose();
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _view = null;
            _layout = null;
            _fight = null;
            _loading = false;
            _pendingTypeId = 0;
            _pendingFashionPos = 0;
        }

        private static async Task ShowAsync()
        {
            int typeId = _pendingTypeId;
            int fashionPos = _pendingFashionPos;
            int requestId = ++_requestId;
            _modelRendered = false;
            _designationRendered = false;
            _designationEffect?.Dispose();
            _designationEffect = null;
            await Task.WhenAll(GoodsModel.EnsureLoaded(), IllusionModelConfigs.EnsureLoaded(),
                LoginConfigs.EnsureLoaded());
            if (requestId != _requestId || typeId != _pendingTypeId) return;
            if (!await EnsureViewAsync()) return;

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            IllusionModelConfigs.Entry entry = IllusionModelConfigs.Get(typeId,
                Mathf.Max(1, RoleModel.Instance.Career), Mathf.Max(1, RoleModel.Instance.Sex));
            DesignationConfigs.Row designation = basic != null && basic.Type == 38 && basic.Subtype == 6
                ? DesignationConfigs.GetByActivationGoods(typeId)
                : null;
            if (basic == null || (entry == null && designation == null))
            {
                GameLog.Warn("IllusionTips", "missing display data typeId={0} basic={1} model={2} designation={3}",
                    typeId, basic != null, entry != null, designation != null);
                return;
            }

            _moduleRoot.SetActive(true);
            foreach (BaseView other in _moduleRoot.GetComponentsInChildren<BaseView>(true))
                if (other != _view) other.gameObject.SetActive(false);
            if (_layout.dimBlocker != null)
            {
                _layout.dimBlocker.gameObject.SetActive(true);
                _layout.dimBlocker.transform.SetAsLastSibling();
            }
            _view.gameObject.SetActive(true);
            _view.transform.SetAsLastSibling();
            _view.Show(typeId);
            await ConfigureTextAsync(basic, entry, designation);
            if (requestId != _requestId || typeId != _pendingTypeId || ActiveView == null) return;
            Subscribe();
            BagController.Instance.RequestExpectPower(typeId);

            if (designation != null)
            {
                await ConfigureDesignationVisualAsync(designation, requestId);
                return;
            }

            GameObject model = await BuildModelAsync(entry, fashionPos);
            if (requestId != _requestId || typeId != _pendingTypeId || ActiveView == null)
            {
                if (model != null) UnityEngine.Object.Destroy(model);
                return;
            }
            if (model == null)
            {
                _view.roleCon.gameObject.SetActive(false);
                return;
            }
            _view.roleCon.gameObject.SetActive(true);
            _modelType = entry.ModelType;
            _effectCount = model.GetComponentsInChildren<Transform>(true)
                .Count(node => node != null && node.name.StartsWith("__fx_", StringComparison.Ordinal));
            ModelStage.PlaceInstance(_view.roleCon, model, entry.Scale, entry.Position,
                UIModelStage.MODEL_YAW + entry.Rotate);
            await Task.Yield();
            if (requestId == _requestId && ActiveView != null)
            {
                ModelStage.RenderStageNow();
                _modelRendered = true;
                GameLog.Info("IllusionTips", "model typeId={0} modelType={1} res={2} scale={3} pos={4}: {5}",
                    typeId, entry.ModelType, entry.ModelRes, entry.Scale, entry.Position,
                    ModelStage.GetRenderDiagnostics());
            }
        }

        private static async Task ConfigureTextAsync(GoodsModel.GoodsBasic basic,
            IllusionModelConfigs.Entry entry, DesignationConfigs.Row designation)
        {
            if (_view._img_bg != null)
            {
                _view._img_bg.enabled = true;
                _view._img_bg.color = Color.white;
                int color = Mathf.Clamp(GoodsModel.GetDisplayColor(basic.TypeId), 1, 7);
                bool backgroundOk = await ResManager.SetImageAsync(_view._img_bg,
                    GameResPath.GetIconOtherPath("common4", "ui_tips_pzbg_" + color), false, false);
                if (!backgroundOk)
                    GameLog.Error("IllusionTips", "missing quality background color={0} typeId={1}", color, basic.TypeId);
            }
            if (_view.goods_name != null) _view.goods_name.text = basic.Name;
            if (_view.runeBox != null) _view.runeBox.gameObject.SetActive(false);
            if (_view._gp_dsgt != null) _view._gp_dsgt.gameObject.SetActive(designation != null);
            if (_view.roleCon != null) _view.roleCon.gameObject.SetActive(designation == null);
            if (_view.skill_gp != null) _view.skill_gp.gameObject.SetActive(false);
            if (_view.gp_sp_skill != null) _view.gp_sp_skill.gameObject.SetActive(false);
            if (_view.btn_group != null) _view.btn_group.gameObject.SetActive(false);
            if (_view.overdueText != null) _view.overdueText.gameObject.SetActive(false);

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(basic.Intro)) lines.Add(ToTmp(basic.Intro));
            if (designation != null)
            {
                foreach (DesignationConfigs.Attr attr in designation.Attrs)
                    lines.Add("<color=#663915>" + GoodsModel.GetAttrName(attr.Id)
                        + "：</color><color=#d15e00>+"
                        + GoodsModel.FormatAttrValue(attr.Id, attr.Value) + "</color>");
            }
            else
            {
                foreach (GoodsModel.EquipBaseAttrRow attr in GoodsModel.GetBaseAttrRows(basic.TypeId))
                    lines.Add("<color=#663915>" + attr.Name + "：</color><color=#d15e00>+"
                        + GoodsModel.FormatAttrValue(attr.AttrId, attr.Value) + "</color>");
            }
            if (_view.intro != null)
            {
                _view.intro.color = new Color32(0x66, 0x39, 0x15, 0xff);
                _view.intro.text = string.Join("\n", lines);
            }
            if (_view._Group1 != null) _view._Group1.gameObject.SetActive(lines.Count > 0);

            string ways = basic.Getway?.Trim() ?? string.Empty;
            bool showWays = ways.Length > 0;
            if (_view.line2 != null) _view.line2.gameObject.SetActive(showWays);
            if (_view.ways != null)
            {
                _view.ways.text = ways;
                _view.ways.gameObject.SetActive(showWays);
            }

            string source = basic.Source?.Trim() ?? string.Empty;
            bool showSource = source.Length > 0;
            if (_view.sourceGp != null) _view.sourceGp.gameObject.SetActive(showSource);
            if (_view.source_txt != null)
            {
                _view.source_txt.color = new Color32(0xd1, 0x5e, 0x00, 0xff);
                _view.source_txt.alignment = TMPro.TextAlignmentOptions.TopLeft;
                _view.source_txt.text = source;
                _view.source_txt.gameObject.SetActive(showSource);
            }
            LayoutDetails(showWays, showSource);
            if (_view.detail_scroller != null)
            {
                _view.detail_scroller.vertical = true;
                _view.detail_scroller.horizontal = false;
                _view.detail_scroller.verticalNormalizedPosition = 1f;
            }

            EnsureFightItem();
            if (_fight != null)
            {
                _fight.SetFighting(entry?.Fight ?? 0);
                _fight.SetFightingUp(0);
            }
        }

        private static async Task ConfigureDesignationVisualAsync(DesignationConfigs.Row row, int requestId)
        {
            if (row == null || _view == null || _view._gp_dsgt == null) return;

            _modelType = 0;
            _effectCount = 0;
            _designationRendered = false;
            if (_view.roleCon != null) _view.roleCon.gameObject.SetActive(false);
            _view._gp_dsgt.gameObject.SetActive(true);

            if (_view._img_dsgt != null)
            {
                _view._img_dsgt.sprite = null;
                _view._img_dsgt.enabled = false;
                _view._img_dsgt.raycastTarget = false;
                _view._img_dsgt.gameObject.SetActive(false);
            }
            if (_view._gp_dsgt_effect != null)
                _view._gp_dsgt_effect.gameObject.SetActive(false);

            if (row.Type == 1 && _view._gp_dsgt_effect != null)
            {
                RectTransform host = _view._gp_dsgt_effect;
                DesignationEffectDisplayConfigs.Display display = DesignationEffectDisplayConfigs.Get(
                    row.Id, DesignationEffectDisplayConfigs.Surface.Details);
                UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                    row.ResourceId?.Trim(), host, DesignationEffectDisplayConfigs.ToUnityPosition(display),
                    Vector3.one * display.Scale,
                    0f, new Vector2(237f, display.Height));
                if (requestId != _requestId || ActiveView == null)
                {
                    handle?.Dispose();
                    return;
                }

                _designationEffect = handle;
                _effectCount = handle != null ? 1 : 0;
                host.gameObject.SetActive(handle != null);
                await Task.Yield();
                _designationRendered = handle != null;
                return;
            }

            if (_view._img_dsgt == null || string.IsNullOrWhiteSpace(row.ResourceId)) return;
            string path = GameResPath.GetDesignImage(row.ResourceId.Trim());
            if (!await ResManager.KeyExistsAsync<Sprite>(path)) return;
            bool loaded = await ResManager.SetImageAsync(_view._img_dsgt, path, nativeSize: false);
            if (requestId != _requestId || ActiveView == null) return;
            _view._img_dsgt.raycastTarget = false;
            _view._img_dsgt.gameObject.SetActive(loaded);
            _designationRendered = loaded && _view._img_dsgt.sprite != null;
        }

        /// <summary>
        /// 对标老端 RefBoxVLayout + SetTipsHeight：只按运行态文字高度排 Y，保留 Prefab 中的 X/宽度。
        /// config_goods[3] 属于详情滚动区，config_goods[35] 属于滚动区外的“获取途径”，两者不能混放。
        /// </summary>
        private static void LayoutDetails(bool showWays, bool showSource)
        {
            if (_view?.detail_box == null || _view.detail_scroller == null) return;
            const float gap = 10f;
            float cursor = 0f;

            void Place(RectTransform rect, float height)
            {
                if (rect == null || !rect.gameObject.activeSelf) return;
                Vector2 pos = rect.anchoredPosition;
                pos.y = -cursor;
                rect.anchoredPosition = pos;
                Vector2 size = rect.sizeDelta;
                size.y = Mathf.Max(0f, height);
                rect.sizeDelta = size;
                cursor += size.y + gap;
            }

            RectTransform group1 = _view._Group1 != null ? _view._Group1.transform as RectTransform : null;
            Place(group1, group1 != null ? group1.rect.height : 0f);

            RectTransform introRect = _view.intro != null ? _view.intro.rectTransform : null;
            float introHeight = _view.intro != null
                ? Mathf.Ceil(_view.intro.GetPreferredValues(_view.intro.text,
                    Mathf.Max(1f, introRect.rect.width), 0f).y)
                : 0f;
            Place(introRect, introHeight);

            RectTransform lineRect = showWays && _view.line2 != null
                ? _view.line2.transform as RectTransform : null;
            Place(lineRect, lineRect != null ? lineRect.rect.height : 0f);
            RectTransform waysRect = showWays && _view.ways != null ? _view.ways.rectTransform : null;
            float waysHeight = showWays && _view.ways != null
                ? Mathf.Ceil(_view.ways.GetPreferredValues(_view.ways.text,
                    Mathf.Max(1f, waysRect.rect.width), 0f).y)
                : 0f;
            Place(waysRect, waysHeight);

            float detailHeight = Mathf.Max(0f, cursor - gap);
            RectTransform detailBox = _view.detail_box;
            detailBox.sizeDelta = new Vector2(Mathf.Max(400f, detailBox.sizeDelta.x), detailHeight);
            if (detailBox.parent is RectTransform content)
                content.sizeDelta = new Vector2(content.sizeDelta.x, detailHeight);

            RectTransform scrollerRect = _view.detail_scroller.transform as RectTransform;
            float initialScrollerHeight = 270f;
            float scrollerHeight = Mathf.Min(initialScrollerHeight, detailHeight);
            if (scrollerRect != null)
                scrollerRect.sizeDelta = new Vector2(scrollerRect.sizeDelta.x, scrollerHeight);

            float bottom = scrollerRect != null ? -scrollerRect.anchoredPosition.y + scrollerHeight : 740f;
            if (showSource && _view.sourceGp != null)
            {
                RectTransform sourceRect = _view.sourceGp.transform as RectTransform;
                RectTransform sourceTextRect = _view.source_txt != null ? _view.source_txt.rectTransform : null;
                if (sourceTextRect != null)
                {
                    _view.source_txt.textWrappingMode = TMPro.TextWrappingModes.Normal;
                    sourceTextRect.sizeDelta = new Vector2(300f, sourceTextRect.sizeDelta.y);
                    float sourceTextHeight = Mathf.Ceil(_view.source_txt.GetPreferredValues(_view.source_txt.text,
                        300f, 0f).y);
                    sourceTextRect.sizeDelta = new Vector2(300f, sourceTextHeight);
                }
                float sourceHeight = Mathf.Max(25f, sourceTextRect != null ? sourceTextRect.rect.height + 5f : 0f);
                sourceRect.anchoredPosition = new Vector2(sourceRect.anchoredPosition.x, -(bottom + 15f));
                sourceRect.sizeDelta = new Vector2(sourceRect.sizeDelta.x, sourceHeight);
                bottom += 15f + sourceHeight + 8f;
            }
            else
            {
                bottom += 15f;
            }

            if (_view._img_bg != null)
            {
                RectTransform background = _view._img_bg.rectTransform;
                background.sizeDelta = new Vector2(background.sizeDelta.x, Mathf.Max(755f, bottom));
            }
        }

        private static async Task<GameObject> BuildModelAsync(IllusionModelConfigs.Entry entry, int fashionPos)
        {
            if (entry.ModelType == 0)
            {
                RoleModel role = RoleModel.Instance;
                int career = Mathf.Max(1, role.Career);
                int sex = Mathf.Max(1, role.Sex);
                LoginConfigs.CareerRes defaults = LoginConfigs.GetCreateRes(career, sex);
                int clothe = role.Figure?.ClotheModelId ?? 0;
                int head = role.Figure?.HeadModelId ?? 0;
                int weapon = role.Figure?.WeaponModelId ?? 0;
                if (clothe <= 0) clothe = defaults?.RoleRes ?? 0;
                if (head <= 0) head = defaults?.HeadRes ?? 0;
                if (weapon <= 0) weapon = defaults?.WeaponRes ?? 0;
                if (fashionPos == 3) head = entry.ModelRes;
                else clothe = entry.ModelRes;
                if (clothe <= 0) return null;
                return await RoleModelAssembler.BuildOldModelAsync(new RoleModelSpec
                {
                    Career = career,
                    ClotheRes = clothe,
                    HeadRes = head,
                    WeaponRes = weapon,
                    Actions = entry.Actions.Length > 0 ? entry.Actions : LoginConfigs.RoleUIActions("IllusionTips"),
                });
            }

            string module = ModuleOf(entry.ModelType);
            if (string.IsNullOrEmpty(module)) return null;
            string resName = module == "weapon" ? "model_weapon_r_" + entry.ModelRes
                : "model_" + module + "_" + entry.ModelRes;
            GameObject prefab = await ResManager.LoadAsync<GameObject>(
                "object/" + module + "/" + resName + "/" + resName);
            if (prefab == null) return null;
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            LoadedAssetReleaser.Track(instance, prefab);
            await EffectBinder.AttachAlways(instance, module, entry.ModelRes.ToString());
            if (entry.Actions.Length > 0) _ = PlayActionAsync(instance, module, entry.ModelRes, entry.Actions[0]);
            return instance;
        }

        private static async Task PlayActionAsync(GameObject model, string module, int id, string action)
        {
            if (model == null || string.IsNullOrEmpty(action)) return;
            Animation animation = model.GetComponent<Animation>();
            if (animation != null && animation.GetClip(action) != null) { animation.Play(action); return; }
            AnimationClip clip = await ResManager.LoadAsync<AnimationClip>(
                "object/" + module + "/action/" + id + "/" + action);
            if (model == null || clip == null) return;
            if (animation == null) animation = model.AddComponent<Animation>();
            if (animation.GetClip(action) == null) animation.AddClip(clip, action);
            animation.Play(action);
        }

        private static string ModuleOf(int modelType)
        {
            switch (modelType)
            {
                case 2: return "mount";
                case 3: return "wing";
                case 4: return "weapon";
                case 5: return "fabao";
                case 7: return "spirit";
                case 20: return "back";
                default: return null;
            }
        }

        private static async Task<bool> EnsureViewAsync()
        {
            if (_moduleRoot != null && _view != null && _layout != null) return true;
            if (_loading) return false;
            _loading = true;
            try
            {
                Transform parent = ViewManager.GetLayer(UILayer.Popup);
                if (parent == null) return false;
                _moduleRoot = await ResManager.InstantiateAsync(
                    GameResPath.GetUIPrefab("common", "CommonModule"), parent);
                if (_moduleRoot == null) return false;
                _moduleRoot.name = "CommonModule(IllusionTips)";
                _view = _moduleRoot.GetComponentInChildren<IllusionTipsBind>(true);
                _layout = _moduleRoot.GetComponent<ItemTipsModalLayout>();
                if (_view == null || _layout == null || _layout.dimBlocker == null)
                {
                    GameLog.Error("IllusionTips", "CommonModule 缺 IllusionTips/普通遮罩绑定");
                    Reset();
                    return false;
                }
                foreach (BaseView view in _moduleRoot.GetComponentsInChildren<BaseView>(true))
                    view.gameObject.SetActive(false);
                foreach (Graphic graphic in _view.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
                _layout.dimBlocker.raycastTarget = true;
                UIUtil.AddClick(_layout.dimBlocker, Close);
                if (_layout.compareBlocker != null) _layout.compareBlocker.gameObject.SetActive(false);
                _moduleRoot.SetActive(false);
                return true;
            }
            finally { _loading = false; }
        }

        private static void EnsureFightItem()
        {
            if (_fight != null || _view?._tpl_FightingShowSmallItem == null || _view.top_fight_gp == null) return;
            GameObject go = UnityEngine.Object.Instantiate(_view._tpl_FightingShowSmallItem, _view.top_fight_gp);
            go.SetActive(true);
            _fight = go.GetComponent<FightingShowSmallItem>();
            _fight?.Show();
        }

        private static void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On<int, long>(GlobalEvent.EVT_GOODS_EXPECT_POWER, OnExpectedPower);
            _subscribed = true;
        }

        private static void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off<int, long>(GlobalEvent.EVT_GOODS_EXPECT_POWER, OnExpectedPower);
            _subscribed = false;
        }

        private static void OnExpectedPower(int typeId, long power)
        {
            if (typeId == _pendingTypeId && ActiveView != null && _fight != null) _fight.SetFighting(power);
        }

        private static string ToTmp(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string text = value.Replace("<br/>", "\n").Replace("<br>", "\n");
            text = FontOpen.Replace(text, "<color=$1>");
            return text.Replace("</font>", "</color>");
        }
    }
}
