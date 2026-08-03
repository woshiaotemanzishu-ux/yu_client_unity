using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 时装模块编排。衣服/头饰共享 <see cref="FashionMainView"/>，套装使用独立 <see cref="FashionSuitView"/>。
    /// BaseWindowSkin 共享内容模式:前两个页签共用同一个 <see cref="FashionMainView"/> 实例,只切
    /// <see cref="FashionMainView.SetPos"/>(对标老端"同一个类不同 fashion_pos_id"的继承关系,
    /// FashionModule.prefab 只烤了一个 FashionMainView 节点)。
    /// </summary>
    public static class FashionFlow
    {
        private const string CONTENT_MODULE = "fashion";
        private const string CONTENT_PREFAB = "FashionModule";
        private const string FRAME_MODULE = "common";
        private const string FRAME_PREFAB = "BaseWindowSkin";

        // 老端运行态页签文案是“头像”，底层仍对应 dress/head 的 posId=3。
        private static readonly string[] Tabs = { "衣服", "头像", "套装" };
        private static readonly int[] TabPosId = { 1, 3 };
        // 标题文字覆盖(BaseWindowSkin 默认标题位图是共享占位图,不覆盖会露出上一个用它的模块的字样)。
        private static readonly string[] TitleTexts = { "时装", "时装", "时装套装" };
        // 老端 bg_list 前两项(tab0衣服/tab1头饰)都是同一张 ui_role_bg3.jpg(FashionBaseView.ts)。
        private static readonly string WindowBg = GameResPath.GetBigBgPath("ui_role_bg3.jpg");

        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static FashionMainView _mainView;
        private static FashionSuitView _suitView;
        private static FashionLevelView _levelView;
        private static bool _loading;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) { Close(); return; }
            _ = OpenAsync(0);
        }

        public static void Open() => _ = OpenAsync(0);

        /// <summary>直达指定页签(0=衣服 1=头像/头饰位 2=套装)。</summary>
        public static void Open(int tabIndex) => _ = OpenAsync(tabIndex);

        public static void Close()
        {
            if (_levelView != null) _levelView.Hide();
            if (_window != null) _window.Hide();
        }

        /// <summary>衣服页部位升级入口；复用 FashionModule 中的真实 FashionLevelView。</summary>
        public static void OpenLevel(int posId)
        {
            if (posId != 1) return;
            FashionLevelView view = EnsureLevelView();
            if (view == null)
            {
                GameLog.Warn("Fashion", "FashionModule 缺 FashionLevelView/FasBagItemRenderer 业务组件");
                return;
            }
            view.Show(posId);
        }

        private static async Task OpenAsync(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= Tabs.Length) tabIndex = 0;

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
                _frameRoot = await MainUIRouteFallback.InstantiateOrShowAsync(CONTENT_MODULE, "Fashion", frameKey, ViewManager.GetLayer(UILayer.Window));
                _contentRoot = await MainUIRouteFallback.InstantiateOrShowAsync(CONTENT_MODULE, "Fashion", contentKey, ViewManager.GetLayer(UILayer.Window));
            }
            catch (Exception e)
            {
                GameLog.Error("Fashion", "时装窗加载异常 frame={0} content={1} error={2}", frameKey, contentKey, e.Message);
                ShowPlaceholderAndReset();
                return;
            }
            finally
            {
                _loading = false;
            }

            if (_frameRoot == null || _contentRoot == null)
            {
                GameLog.Error("Fashion", "时装窗加载失败 frame={0} content={1}", frameKey, contentKey);
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
                GameLog.Warn("Fashion", "BaseWindowSkin 缺 BaseWindowSkinView(重跑 common 流水线回填)");
                return;
            }

            _window.Show();
            var overrides = new Dictionary<int, Func<RectTransform, BaseView>> { [2] = ReparentSuit };
            _window.ConfigureShared(Tabs.Length, ReparentFashion, OnFashionTab, tabIndex,
                null, overrides, Tabs, null, WindowBg, TitleTexts);
            GameLog.Info("Fashion", "时装窗打开(衣服/头像共享主视图 + 套装页,默认 tab{0} {1})", tabIndex, Tabs[tabIndex]);
        }

        private static void OnFashionTab(int index)
        {
            int posId = index >= 0 && index < TabPosId.Length ? TabPosId[index] : 1;
            if (_mainView != null)
            {
                _mainView.SetPos(posId);
                GameLog.Info("Fashion", "切页签[{0}] → pos={1}", index >= 0 && index < Tabs.Length ? Tabs[index] : index.ToString(), posId);
            }
        }

        /// <summary>把内容源里的 FashionMainView reparent 进窗框内容区(保留原始布局),返回其 BaseView。
        /// ⚠必须在 SetParent **之前**把同级的 FashionColorItem 模板节点引用交给 FashionMainView——
        /// 该节点默认 inactive,Awake 会被 Unity 延迟到 SetActive(true) 才跑,那时早已经不是原来的
        /// FashionModule 顶层同级关系了(实测踩过的坑,详见 FashionMainView.SetColorTemplate 注释)。</summary>
        private static BaseView ReparentFashion(RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform colorTemplate = _contentRoot.transform.Find("FashionColorItem");
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (v is FashionMainView fv)
                {
                    if (colorTemplate != null) fv.SetColorTemplate(colorTemplate.gameObject);
                    else GameLog.Warn("Fashion", "FashionModule 顶层找不到 FashionColorItem 模板节点(prefab 结构变了?)");
                    fv.transform.SetParent(parent, false);
                    fv.gameObject.SetActive(true);
                    _mainView = fv;
                    return fv;
                }
            }
            GameLog.Warn("Fashion", "FashionModule 缺 FashionMainView(重跑转换/回填 Bind 组件)");
            return null;
        }

        private static BaseView ReparentSuit(RectTransform parent)
        {
            if (_contentRoot == null) return null;
            Transform tabTemplate = _contentRoot.transform.Find("FashionSuitTabItem");
            Transform goodsTemplate = _contentRoot.transform.Find("FashionSuitGoodsItem");
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (!(v is FashionSuitView suit)) continue;
                suit.SetTemplates(tabTemplate != null ? tabTemplate.gameObject : null,
                    goodsTemplate != null ? goodsTemplate.gameObject : null,
                    suit._tpl_BaseAwardItem);
                suit.transform.SetParent(parent, false);
                suit.gameObject.SetActive(true);
                _suitView = suit;
                return suit;
            }
            GameLog.Warn("Fashion", "FashionModule 缺 FashionSuitView(重跑转换/Bind 升级器)");
            return null;
        }

        private static FashionLevelView EnsureLevelView()
        {
            if (_levelView != null) return _levelView;
            if (_contentRoot == null) return null;
            Transform itemTemplate = _contentRoot.transform.Find("FasBagItemRenderer");
            foreach (BaseView v in _contentRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (!(v is FashionLevelView level)) continue;
                if (level._tpl_FasBagItemRenderer == null && itemTemplate != null)
                    level._tpl_FasBagItemRenderer = itemTemplate.gameObject;
                level.transform.SetParent(ViewManager.GetLayer(UILayer.Popup), false);
                level.gameObject.SetActive(false);
                _levelView = level;
                return level;
            }
            return null;
        }

        /// <summary>释放窗框与内容实例(重新生成 prefab 后的预览/重载入口;下次 Open 重新实例化)。</summary>
        public static void Reset()
        {
            if (_levelView != null) UnityEngine.Object.Destroy(_levelView.gameObject);
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _mainView = null;
            _suitView = null;
            _levelView = null;
            _loading = false;
        }

        private static void ShowPlaceholderAndReset()
        {
            MainUIRouteFallback.ShowUnavailable(CONTENT_MODULE, "Fashion", "FashionModule/BaseWindowSkin load failed");
            Reset();
        }
    }
}
