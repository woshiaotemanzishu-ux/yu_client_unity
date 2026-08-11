using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.PetEquip.Views;
using UnityEngine;

namespace Shenxiao.Module.Core.PetEquip
{
    /// <summary>
    /// 侍魂装备三标签窗口编排。复用 BaseWindowSkin，只加载 PetEquipCreator 生成的三个内容页；
    /// 外形页仍留在下层，关闭本窗口即可回到坐骑/伙伴培养页。
    /// </summary>
    public static class PetEquipFlow
    {
        private const string FrameModule = "common";
        private const string FramePrefab = "BaseWindowSkin";
        private const string ContentModule = "PetEquip";
        private const string ContentPrefab = "PetEquipModule";

        private static readonly string[] Labels = { "背包", "强化", "打造" };

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static readonly Dictionary<PetEquipPageMode, PetEquipPageView> Pages =
            new Dictionary<PetEquipPageMode, PetEquipPageView>();
        private static bool _loading;
        private static int _requestedType = PetEquipController.TYPE_HORSE;
        private static int _requestedTab;

        public static bool IsOpen => _window != null && _window.IsShown;
        public static int CurrentType => _requestedType;
        public static BaseWindowSkinView CurrentWindow => _window;

        public static void Open(int typeId, int tab = 0)
        {
            if (!IsSupportedType(typeId)) return;
            _requestedType = typeId;
            _requestedTab = ClampTab(tab);
            _ = OpenAsync();
        }

        public static void Close()
        {
            foreach (PetEquipPageView page in Pages.Values)
                if (page != null && page.IsShown) page.Hide();
            if (_window != null) _window.Hide();
        }

        private static async Task OpenAsync()
        {
            if (_loading) return;
            _loading = true;
            try
            {
                await FuncOpenConfig.EnsureLoaded();
                int typeId = _requestedType;
                if (!CanOpen(typeId, true)) return;

                if (_window == null || _frameRoot == null || _contentRoot == null)
                {
                    if (!await LoadWindow()) return;
                    ConfigureWindow();
                }

                typeId = _requestedType;
                if (!CanOpen(typeId, true)) return;
                int tab = ClampTab(_requestedTab);
                if (tab > 0 && !HasWorn(typeId)) tab = 0;
                ApplyType(typeId);
                _window.Show();
                _window.SelectTab(tab);
                GameLog.Info("PetEquip", "打开侍魂装备窗口 type={0} tab={1}", typeId, tab);
            }
            catch (Exception e)
            {
                GameLog.Error("PetEquip", "窗口加载异常: {0}", e);
                Reset();
            }
            finally
            {
                _loading = false;
            }
        }

        private static async Task<bool> LoadWindow()
        {
            Transform layer = ViewManager.GetLayer(UILayer.Window);
            string frameKey = GameResPath.GetUIPrefab(FrameModule, FramePrefab);
            string contentKey = GameResPath.GetUIPrefab(ContentModule, ContentPrefab);
            _frameRoot = await ResManager.InstantiateAsync(frameKey, layer);
            if (_frameRoot == null)
            {
                GameLog.Error("PetEquip", "共享窗框加载失败: {0}", frameKey);
                return false;
            }
            _contentRoot = await ResManager.InstantiateAsync(contentKey, layer);
            if (_contentRoot == null)
            {
                GameLog.Error("PetEquip", "内容模块加载失败: {0}(请运行 PetEquipCreator + Addressable 自动分组)", contentKey);
                ResManager.ReleaseInstance(_frameRoot);
                _frameRoot = null;
                return false;
            }

            _frameRoot.name = FramePrefab;
            _contentRoot.name = ContentPrefab;
            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                GameLog.Error("PetEquip", "BaseWindowSkin 缺 BaseWindowSkinView");
                Reset();
                return false;
            }

            Pages.Clear();
            foreach (PetEquipPageView page in _contentRoot.GetComponentsInChildren<PetEquipPageView>(true))
            {
                if (page == null || Pages.ContainsKey(page.Mode)) continue;
                Pages.Add(page.Mode, page);
                page.gameObject.SetActive(false);
            }
            if (Pages.Count != 3)
            {
                GameLog.Error("PetEquip", "PetEquipModule 页面数量错误: {0}/3", Pages.Count);
                Reset();
                return false;
            }
            return true;
        }

        private static void ConfigureWindow()
        {
            var specs = new List<TabSpec>(3);
            for (int i = 0; i < 3; i++)
            {
                PetEquipPageMode pageMode = (PetEquipPageMode)i;
                bool gated = pageMode != PetEquipPageMode.Bag;
                specs.Add(new TabSpec
                {
                    Enabled = true,
                    Label = Labels[i],
                    ContentFactory = parent => ReparentPage(pageMode, parent),
                    OpenCheck = gated ? (Func<bool>)(() => HasWorn(_requestedType)) : null,
                    LockedToast = gated ? "需先穿戴至少一件侍魂装备" : null,
                });
            }
            ApplyType(_requestedType);
            _window.Show();
            _window.SetReturnAction(ReturnToPet);
            _window.Configure(specs, 0);
        }

        private static void ReturnToPet()
        {
            int typeId = _requestedType;
            Close();
            Shenxiao.Module.Core.Pet.PetFlow.Open(typeId == PetEquipController.TYPE_PARTNER ? 1 : 0);
        }

        private static BaseView ReparentPage(PetEquipPageMode mode, RectTransform parent)
        {
            if (!Pages.TryGetValue(mode, out PetEquipPageView page) || page == null) return null;
            page.transform.SetParent(parent, false);
            page.SetType(_requestedType);
            page.gameObject.SetActive(true);
            return page;
        }

        private static void ApplyType(int typeId)
        {
            foreach (PetEquipPageView page in Pages.Values) if (page != null) page.SetType(typeId);
        }

        private static bool CanOpen(int typeId, bool toast)
        {
            if (!FuncOpenConfig.IsLoaded)
            {
                if (toast) TipsManager.Toast("功能开放配置尚未就绪");
                return false;
            }
            string outwardView = typeId == PetEquipController.TYPE_HORSE
                ? "HorseComponentView"
                : "PartnerComponentView";
            bool open = FuncOpenConfig.CheckFuncOpenState("PetEquipBaseView")
                && FuncOpenConfig.CheckFuncOpenState(outwardView);
            if (!open && toast) TipsManager.Toast("侍魂装备功能尚未开放");
            return open;
        }

        private static bool HasWorn(int typeId)
        {
            PetEquipModel.PetEquipInfo info = PetEquipModel.Instance.Get(typeId);
            if (info?.Items == null) return false;
            for (int i = 0; i < info.Items.Count; i++) if (info.Items[i].GoodsId > 0) return true;
            return false;
        }

        private static int ClampTab(int tab) => tab >= 0 && tab < 3 ? tab : 0;
        private static bool IsSupportedType(int typeId)
            => typeId == PetEquipController.TYPE_HORSE || typeId == PetEquipController.TYPE_PARTNER;

        public static void Reset()
        {
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            Pages.Clear();
            _loading = false;
            _requestedType = PetEquipController.TYPE_HORSE;
            _requestedTab = 0;
        }
    }
}
