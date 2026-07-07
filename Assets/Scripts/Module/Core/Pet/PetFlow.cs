using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Pet
{
    /// <summary>
    /// 灵宠/培养模块编排:四标签窗(对标老端 MountPetView extends BaseWindowComponent;主 HUD 灵宠图标)。
    /// **走 BaseWindowSkinView 共享内容模式**(同 ShopFlow 范式):
    /// 老端 tabs=[御风云骑(Horse,type_id=1)/剑魄同修(Partner,type_id=2)/神巫/天妖灵魄],前两页共用同一
    /// OutWardBaseView 布局(仅 _type 不同)→ Unity 用一个 <see cref="OutWardBaseView"/> 切 type_id;
    /// 神巫(PartnerBaseView)/天妖灵魄(DemonMainView)未移植 → 标签 disabled(写好置 true 即开)。
    ///
    /// 主线卡点入口:TaskModel.DoTask ctype23(100330 坐骑)/25(100190 剑魄同修)/90(100521/100901 等级线)
    /// → <see cref="Open(int)"/> 直达对应页签(对标老端 SWITCH_MAIN_FUNC_VIEW MainFunc.Pet, index)。
    /// 入口注册见 <see cref="PetBootstrap"/>(MainUIRouter "pet");再点图标 <see cref="Toggle"/> 关闭。
    /// </summary>
    public static class PetFlow
    {
        private const string CONTENT_MODULE = "pet";
        private const string CONTENT_PREFAB = "PetModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";

        // 老端 MountPetView tabs_list(仙侠化名;神巫/天妖灵魄内容未移植 → disabled)
        private static readonly string[] PetTabs = { "御风云骑", "剑魄同修", "神巫", "天妖灵魄" };
        private static readonly bool[] TabEnabled = { true, true, false, false };
        // 页签 index → OutWard type_id(0=坐骑1,1=剑魄同修2;神巫/天妖灵魄非 OutWard 家族)
        private static readonly int[] TabTypeId = { 1, 2, 0, 0 };
        // 每页标题:老端现行为=文字覆盖盖住标题位图(MountPetView.EnsureMountPetModuleTitleOverlay,
        // titleList 的 uiwg_001/ui_shihun 位图是旧通道,已被覆盖文字取代 → 传 titleTexts 走文字)。
        private static readonly string[] TabTitleTexts = { "御风云骑", "剑魄同修", "神巫", "天妖灵魄" };
        // 窗底大图(对标老端 bg_list 0/1 = "uiwg_008a.jpg",BaseWindowComponent.ts:295 走 GetBigBgPath;
        // ⚠ pet/other/uiwg_008a.png 是模型底座,不是这张全屏水墨图,别混)
        private static readonly string WindowBg = GameResPath.GetBigBgPath("uiwg_008a.jpg");

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static OutWardBaseView _mainView;
        private static bool _loading;

        /// <summary>当前窗框(页内引导"指关闭钮"用;未打开=null)。</summary>
        public static BaseWindowSkinView CurrentWindow => _window != null && _window.IsShown ? _window : null;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            _ = OpenAsync(0);
        }

        public static void Open() => _ = OpenAsync(0);

        /// <summary>直达指定页签(0=御风云骑 1=剑魄同修;任务路由/引导用)。</summary>
        public static void Open(int tabIndex) => _ = OpenAsync(tabIndex);

        public static void Close()
        {
            if (_window != null) _window.Hide();
        }

        /// <summary>打开模块内子窗(属性/幻化/水晶/技能…),按 View 子类名在内容源查找并 Show。未移植 → 日志降级。</summary>
        public static void OpenSub(string viewTypeName)
        {
            if (_contentRoot == null)
            {
                GameLog.Warn("Pet", "OpenSub({0}) 时灵宠模块未打开", viewTypeName);
                return;
            }
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v.GetType().Name == viewTypeName) { v.Show(); return; }
            }
            GameLog.Info("Pet", "灵宠子窗 [{0}] 未移植 View,待对接", viewTypeName);
        }

        private static async Task OpenAsync(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= PetTabs.Length || !TabEnabled[tabIndex]) tabIndex = 0;

            if (_frameRoot != null && _window != null)
            {
                _window.Show();
                _window.SelectShared(tabIndex);
                return;
            }

            if (_loading) return;
            _loading = true;
            string frameKey = GameResPath.GetUIPrefab(FRAME_MODULE, FRAME_PREFAB);
            string contentKey = GameResPath.GetUIPrefab(CONTENT_MODULE, CONTENT_PREFAB);
            try
            {
                _frameRoot = await MainUIRouteFallback.InstantiateOrShowAsync(CONTENT_MODULE, "Pet", frameKey, ViewManager.GetLayer(UILayer.Window));
                _contentRoot = await MainUIRouteFallback.InstantiateOrShowAsync(CONTENT_MODULE, "Pet", contentKey, ViewManager.GetLayer(UILayer.Window));
            }
            catch (Exception e)
            {
                GameLog.Error("Pet", "灵宠窗加载异常 frame={0} content={1} error={2}", frameKey, contentKey, e.Message);
                ShowPlaceholderAndReset();
                return;
            }
            finally
            {
                _loading = false;
            }

            if (_frameRoot == null || _contentRoot == null)
            {
                GameLog.Error("Pet", "灵宠四标签窗加载失败 frame={0} content={1}", frameKey, contentKey);
                ShowPlaceholderAndReset();
                return;
            }
            _frameRoot.name = FRAME_PREFAB;
            _contentRoot.name = CONTENT_PREFAB;

            foreach (Transform c in _contentRoot.transform) c.gameObject.SetActive(false);

            _window = _frameRoot.GetComponent<BaseWindowSkinView>();
            if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            if (_window == null)
            {
                ShowPlaceholderAndReset();
                GameLog.Warn("Pet", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
                return;
            }

            _window.Show();
            _window.ConfigureShared(PetTabs.Length, ReparentOutWard, OnPetTab, tabIndex,
                i => i >= 0 && i < TabEnabled.Length && TabEnabled[i], null,
                PetTabs, null, WindowBg, TabTitleTexts);
            GameLog.Info("Pet", "灵宠四标签窗打开(共享 OutWardBaseView,默认 tab{0} {1})", tabIndex, PetTabs[tabIndex]);
        }

        private static void OnPetTab(int index)
        {
            string name = index >= 0 && index < PetTabs.Length ? PetTabs[index] : index.ToString();
            int typeId = index >= 0 && index < TabTypeId.Length ? TabTypeId[index] : 0;
            if (_mainView != null && typeId > 0)
            {
                _mainView.SetType(typeId);
                GameLog.Info("Pet", "切灵宠页签[{0}] → OutWard type_id={1}", name, typeId);
            }
            else
            {
                GameLog.Info("Pet", "切灵宠页签[{0}] → 内容未移植,待对接", name);
            }
        }

        /// <summary>把内容源里的 OutWardBaseView reparent 进窗框内容区(保留原始布局),返回其 BaseView。</summary>
        private static BaseView ReparentOutWard(RectTransform parent)
        {
            if (_contentRoot == null) return null;
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v is OutWardBaseView ov)
                {
                    ov.transform.SetParent(parent, false);
                    ov.gameObject.SetActive(true);
                    _mainView = ov;
                    return ov;
                }
            }
            GameLog.Warn("Pet", "PetModule 缺 OutWardBaseView(重跑 PetCreator 生成)");
            return null;
        }

        /// <summary>释放窗框与内容实例(重新生成 prefab 后的预览/重载入口;下次 Open 重新实例化)。</summary>
        public static void Reset()
        {
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _mainView = null;
            _loading = false;
        }

        private static void ShowPlaceholderAndReset()
        {
            MainUIRouteFallback.ShowUnavailable(CONTENT_MODULE, "Pet", "PetModule/BaseWindowSkin load failed");
            Reset();
        }
    }
}
