using System;
using System.Collections.Generic;
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
    /// 神巫使用 Pet 域只读页消费 14202/14201；天妖灵魄仍等待完整配置与二级页闭包。
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

        // 老端 MountPetView tabs_list(仙侠化名)。普通神巫已接只读页；天妖灵魄保留真实可见可点入口，
        // 完整业务页与二级叶以 DEMON_TAB_ROUTE 精确 blocked，不用“未开放”提示降级。
        private static readonly string[] PetTabs = { "御风云骑", "剑魄同修", "神巫", "天妖灵魄" };
        // 页签 index → OutWard type_id(0=坐骑1,1=剑魄同修2;神巫/天妖灵魄非 OutWard 家族)
        private static readonly int[] TabTypeId = { 1, 2, 0, 0 };
        // 每页标题:老端现行为=文字覆盖盖住标题位图(MountPetView.EnsureMountPetModuleTitleOverlay,
        // titleList 的 uiwg_001/ui_shihun 位图是旧通道,已被覆盖文字取代 → 传 titleTexts 走文字)。
        private static readonly string[] TabTitleTexts = { "御风云骑", "剑魄同修", null, "天妖灵魄" };
        // 普通神巫当前老端保留 uiqlhx_007 位图；只有唤醒分支才以“神巫劫境”文字覆盖。
        private static readonly string[] TabTitleImages = { null, null, GameResPath.GetIcon("pet", "uiqlhx_007"), null };
        // 窗底大图(对标老端 bg_list 0/1 = "uiwg_008a.jpg",BaseWindowComponent.ts:295 走 GetBigBgPath;
        // ⚠ pet/other/uiwg_008a.png 是模型底座,不是这张全屏水墨图,别混)
        private static readonly string WindowBg = GameResPath.GetBigBgPath("uiwg_008a.jpg");

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static OutWardBaseView _mainView;
        private static PetPartnerPageView _partnerView;
        private static PetDemonRouteView _demonRouteView;
        private static bool _loading;
        private static int _openEpoch;

        /// <summary>当前窗框(页内引导"指关闭钮"用;未打开=null)。</summary>
        public static BaseWindowSkinView CurrentWindow => _window != null && _window.IsShown ? _window : null;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            _ = OpenAsync(0);
        }

        public static void Open() => _ = OpenAsync(0);

        /// <summary>直达指定页签(0=御风云骑 1=剑魄同修 2=普通神巫 3=天妖灵魄;任务路由/引导用)。</summary>
        public static void Open(int tabIndex) => _ = OpenAsync(tabIndex);

        public static void Close()
        {
            _openEpoch++;
            _loading = false;
            // BaseWindowSkin 与重挂进去的内容页不是同一 BaseView 生命周期。
            // 只 Hide 窗框不会触发 OutWardBaseView.OnHide，模型台和主线引导会残留到关窗后。
            // 先收内容，再收窗框；下次 SelectShared 会重新 Show 当前内容页。
            if (_mainView != null && _mainView.IsShown) _mainView.Hide();
            if (_partnerView != null && _partnerView.IsShown) _partnerView.Hide();
            if (_demonRouteView != null && _demonRouteView.IsShown) _demonRouteView.Hide();
            _partnerView?.CancelPrewarm();
            if (_window != null) _window.Hide();
        }

        /// <summary>
        /// BaseWindowManager 用排他规则直接 Hide 共享窗框时，不会转发 override 内容页的 OnHide；
        /// 由 Pet 域中继补齐内容生命周期，确保模型台、常驻特效和 NPC 语音立即停止。
        /// </summary>
        internal static void HandleFrameHidden()
        {
            _openEpoch++;
            _loading = false;
            if (_mainView != null && _mainView.IsShown) _mainView.Hide();
            if (_partnerView != null && _partnerView.IsShown) _partnerView.Hide();
            if (_demonRouteView != null && _demonRouteView.IsShown) _demonRouteView.Hide();
            _partnerView?.CancelPrewarm();
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
            if (tabIndex < 0 || tabIndex >= PetTabs.Length) tabIndex = 0;
            if (_loading) return;
            int epoch = ++_openEpoch;
            _loading = true;
            string frameKey = GameResPath.GetUIPrefab(FRAME_MODULE, FRAME_PREFAB);
            string contentKey = GameResPath.GetUIPrefab(CONTENT_MODULE, CONTENT_PREFAB);
            Task<GameObject> frameTask = null;
            Task<GameObject> contentTask = null;
            GameObject pendingFrame = null;
            GameObject pendingContent = null;
            try
            {
                Task funcOpenTask = FuncOpenConfig.EnsureLoaded();
                if (_frameRoot != null && _window != null)
                {
                    await funcOpenTask;
                    if (epoch != _openEpoch || _window == null) return;
                    if (!IsTabEnabled(tabIndex)) tabIndex = 0;
                    _partnerView?.BeginPrewarm();
                    _window.Show();
                    ConfigureWindow(tabIndex);
                    return;
                }

                frameTask = MainUIRouteFallback.InstantiateOrShowAsync(
                    CONTENT_MODULE, "Pet", frameKey, ViewManager.GetLayer(UILayer.Window));
                contentTask = MainUIRouteFallback.InstantiateOrShowAsync(
                    CONTENT_MODULE, "Pet", contentKey, ViewManager.GetLayer(UILayer.Window));
                await Task.WhenAll(frameTask, contentTask, funcOpenTask);
                pendingFrame = frameTask.Result;
                pendingContent = contentTask.Result;
                if (epoch != _openEpoch) return;
                if (pendingFrame == null || pendingContent == null)
                {
                    GameLog.Error("Pet", "灵宠四标签窗加载失败 frame={0} content={1}", frameKey, contentKey);
                    return;
                }

                if (!IsTabEnabled(tabIndex)) tabIndex = 0;
                _frameRoot = pendingFrame;
                pendingFrame = null;
                _contentRoot = pendingContent;
                pendingContent = null;
                _frameRoot.name = FRAME_PREFAB;
                _contentRoot.name = CONTENT_PREFAB;

                foreach (Transform c in _contentRoot.transform) c.gameObject.SetActive(false);
                _partnerView = _contentRoot.GetComponentInChildren<PetPartnerPageView>(true);
                _partnerView?.BeginPrewarm();

                _window = _frameRoot.GetComponent<BaseWindowSkinView>();
                if (_window == null) _window = _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
                if (_window == null)
                {
                    GameLog.Warn("Pet", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
                    ShowPlaceholderAndReset();
                    return;
                }

                _window.Show();
                ConfigureWindow(tabIndex);
                GameLog.Info("Pet", "灵宠四标签窗打开(OutWard+神巫只读页,默认 tab{0} {1})", tabIndex, PetTabs[tabIndex]);
            }
            catch (Exception e)
            {
                if (pendingFrame == null && frameTask != null && frameTask.Status == TaskStatus.RanToCompletion &&
                    _frameRoot != frameTask.Result)
                    pendingFrame = frameTask.Result;
                if (pendingContent == null && contentTask != null && contentTask.Status == TaskStatus.RanToCompletion &&
                    _contentRoot != contentTask.Result)
                    pendingContent = contentTask.Result;
                if (epoch == _openEpoch)
                {
                    GameLog.Error("Pet", "灵宠窗加载异常 frame={0} content={1} error={2}", frameKey, contentKey, e.Message);
                    ShowPlaceholderAndReset();
                }
            }
            finally
            {
                // Close/Reset 后到达的旧 continuation 只释放自己取得的实例，绝不写回或重开窗口。
                if (pendingFrame != null) ResManager.ReleaseInstance(pendingFrame);
                if (pendingContent != null) ResManager.ReleaseInstance(pendingContent);
                if (epoch == _openEpoch) _loading = false;
            }
        }

        /// <summary>
        /// 首开和缓存重开都重建一次标签壳，使 PartnerBaseView 的等级/任务开放状态不会被首次打开时冻结。
        /// 已缓存内容仍由 BaseWindowSkinView 复用，只重建可见标签和当前选择。
        /// </summary>
        private static void ConfigureWindow(int tabIndex)
        {
            if (_window == null) return;
            if (_frameRoot != null && _frameRoot.GetComponent<PetFrameLifecycleRelay>() == null)
                _frameRoot.AddComponent<PetFrameLifecycleRelay>();
            _window.SetReturnAction(Close);
            var overrides = new Dictionary<int, Func<RectTransform, BaseView>>
            {
                [2] = ReparentPartner,
                [3] = ReparentDemonRoute
            };
            _window.ConfigureShared(PetTabs.Length, ReparentOutWard, OnPetTab, tabIndex,
                IsTabEnabled, overrides,
                PetTabs, TabTitleImages, WindowBg, TabTitleTexts);
        }

        /// <summary>
        /// 对标老端 MountPetView 的 PartnerBaseView / DemonMainView 功能闸。
        /// 账号 111111 的权威老端批次已证明第四签可见可点；Unity 不得因业务页未完整移植而隐藏入口，
        /// 也不得显示“未开放”Toast，缺失叶统一精确记录为 DEMON_TAB_ROUTE blocked。
        /// </summary>
        private static bool IsTabEnabled(int index)
        {
            if (index < 0 || index >= PetTabs.Length) return false;
            if (index <= 1) return true;
            if (index == 2) return FuncOpenConfig.CheckFuncOpenState("PartnerBaseView");
            return FuncOpenConfig.CheckFuncOpenState("DemonMainView");
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
                if (index == 3)
                    GameLog.Warn("Pet", "DEMON_TAB_ROUTE blocked: 老端入口可达，Unity 已显示 DemonMainView 视觉壳；业务 View/二级页/写事务待完整移植");
                else
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

        /// <summary>把 PetModule 内人工接管的普通神巫页重挂到共享窗框；页内只消费真实只读快照。</summary>
        private static BaseView ReparentPartner(RectTransform parent)
        {
            if (_contentRoot == null) return null;
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v is PetPartnerPageView partner)
                {
                    partner.transform.SetParent(parent, false);
                    partner.gameObject.SetActive(true);
                    _partnerView = partner;
                    return partner;
                }
            }
            GameLog.Warn("Pet", "PetModule 缺 PetPartnerPageView(人工 Prefab 绑定不完整)");
            return null;
        }

        /// <summary>
        /// 第四签不走 OutWard，也不以 OpenCheck/Toast 降级；只把 PetModule 内嵌的既有 DemonMainView
        /// 视觉壳挂入真实组合窗。PetDemonRouteView 不绑定壳内事务按钮，仅保留明确 route blocker。
        /// </summary>
        private static BaseView ReparentDemonRoute(RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform shell = FindDeepChild(_contentRoot.transform, "DemonMainViewRoute");
            if (shell == null)
            {
                GameLog.Error("Pet", "DEMON_TAB_ROUTE blocked: PetModule 缺 DemonMainViewRoute 视觉壳");
                return null;
            }
            shell.SetParent(parent, false);
            shell.gameObject.SetActive(true);
            _demonRouteView = shell.GetComponent<PetDemonRouteView>();
            if (_demonRouteView == null) _demonRouteView = shell.gameObject.AddComponent<PetDemonRouteView>();
            return _demonRouteView;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child != null && child.name == name) return child;
            return null;
        }

        /// <summary>释放窗框与内容实例(重新生成 prefab 后的预览/重载入口;下次 Open 重新实例化)。</summary>
        public static void Reset()
        {
            _openEpoch++;
            _loading = false;
            if (_mainView != null && _mainView.IsShown) _mainView.Hide();
            if (_partnerView != null && _partnerView.IsShown) _partnerView.Hide();
            if (_demonRouteView != null && _demonRouteView.IsShown) _demonRouteView.Hide();
            _partnerView?.CancelPrewarm();
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _mainView = null;
            _partnerView = null;
            _demonRouteView = null;
            _loading = false;
        }

        private static void ShowPlaceholderAndReset()
        {
            MainUIRouteFallback.ShowUnavailable(CONTENT_MODULE, "Pet", "PetModule/BaseWindowSkin load failed");
            Reset();
        }
    }

    /// <summary>
    /// 天妖灵魄现有视觉壳的 Pet 域生命周期桥。当前只读快照已有，完整 Demon 业务 View、配置和
    /// 二级页尚未闭合，因此绝不在此绑定升级/激活/商城等事务，也不伪造“未开放”提示。
    /// </summary>
    internal sealed class PetDemonRouteView : BaseView
    {
        protected override void OnShow(object args)
        {
            GameLog.Warn("Pet", "DEMON_TAB_ROUTE blocked: DemonMainView visual shell visible; business view and leaves pending");
        }
    }

    /// <summary>运行时附着在 BaseWindowSkin 实例；仅桥接共享窗框的 OnDisable，不修改 Common。</summary>
    internal sealed class PetFrameLifecycleRelay : MonoBehaviour
    {
        private void OnDisable() => PetFlow.HandleFrameHidden();
    }
}
