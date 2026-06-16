using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 底部经验条/功能图标条(对标老客户端 MainUIDownView.ts:LoadSuccess + RefreshExpWithoutLevelUp)。
    /// 只还原源码支持的首屏静态态与经验刷新:
    /// - _img_bg 换底图 uizjmv3_001(对标 LoadSuccess 的 SetTexture("mainUI","uizjmv3_001"))。
    /// - 经验条宽度按 Exp/ExpLim 比例填充(对标 PlayAnim 的 width = max_len * persent,这里去掉补间直接取终值);
    ///   老客户端 max_len 明确写死为 722,转换产物初始宽度不是运行时满宽,这里按旧端常量走。
    /// - _lb_exp 文案 "exp / exp_lim"(对标 onComleted 的 tb.exp + " / " + tb.exp_lim);ExpLim<=0 时 persent=0、
    ///   文案 "0 / 0"(对标 RefreshExpWithoutLevelUp 的 exp_lim==0 → persent=0 分支)。
    /// 数据只读 RoleModel(唯一真相源),监听 EVT_ROLE_INFO_UPDATE 刷新(对标老客户端 EXP_CHANGE_WITHOUT_ANIMATION)。
    /// 与 MainUITopView 一致用 OnDestroy 兜底注销:模块释放不走 ViewManager,只靠 OnDispose 会漏注销。
    /// MainUIModel 尚未移植:翻面红点 _img_red 与经验特效盒 _box_exp_effect 在拿到数据/特效前隐藏(对标
    /// UpdateTurnRed 的 turn_red_dot 驱动与 PlayAnim 里 AddUIEffect("ui_expbar") 后才显的 _box_exp_effect)。
    /// 功能图标(MainFuncIconItem)依赖 MainUIModel.GetMainFuncOpenCond + Main_Func_Icons,未移植不造假图标。
    /// </summary>
    public sealed class MainUIDownView : MainUIDownViewBind
    {
        // 老客户端 MainUIDownView.ts 明确写死 max_len = 722,经验条目标宽度按它计算。
        private const float EXP_BAR_MAX_WIDTH = 722f;
        private const float FUNC_ICON_GAP = 105f;
        private static readonly string[] AlwaysOpenMainFuncIcons = { "role", "bag" };
        private readonly List<MainFuncIconItemBind> _funcIconItems = new List<MainFuncIconItemBind>();

        protected override void OnInit()
        {
            // LoadSuccess uses ResManager.SetTexture, whose old-client path rule maps /texture/ to /other/.
            _ = ResManager.SetLayaTextureAsync(_img_bg, GameResPath.GetIcon("mainUI", "uizjmv3_001"), nativeSize: false);

            HideUnbackedIndicators();
            HideTemplates();
            RefreshMainFuncIcons();
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            RefreshExp();
        }

        protected override void OnShow(object args)
        {
            // open_callback → RefreshExpWithoutLevelUp()
            RefreshExp();
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        private void OnDestroy()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        private void OnRoleInfoUpdate()
        {
            RefreshExp();
        }

        /// <summary>
        /// 对标 RefreshExpWithoutLevelUp + PlayAnim 的终值:按 Exp/ExpLim 算比例,刷经验条宽度与文案。
        /// 去掉补间动画(无源补间器移植),直接落到目标态;升级/特效闪光等动画态待 MainUIModel 移植后补。
        /// </summary>
        private void RefreshExp()
        {
            RoleModel m = RoleModel.Instance;
            long exp = m.Exp;
            long expLim = m.ExpLim;

            // 对标 RefreshExpWithoutLevelUp: exp 截顶到 exp_lim;exp_lim==0 → persent=0
            if (expLim <= 0)
            {
                SetExpBarWidth(0f);
                _lb_exp.text = "0 / 0";
                return;
            }

            if (exp > expLim)
            {
                exp = expLim;
            }

            // 对标旧端: let a = exp / exp_lim * 100; persent = Math.floor(a) / 100
            float persent = Mathf.Floor((float)((double)exp / expLim * 100.0)) / 100f;
            SetExpBarWidth(EXP_BAR_MAX_WIDTH * persent);
            // 对标 onComleted: this._lb_exp.text = tb.exp + " / " + tb.exp_lim
            _lb_exp.text = exp + " / " + expLim;
        }

        /// <summary>设经验条宽度(对标老客户端 this._img_exp.width = ...),只改宽不动高。</summary>
        private void SetExpBarWidth(float width)
        {
            if (width < 0f)
            {
                width = 0f;
            }
            else if (width > EXP_BAR_MAX_WIDTH)
            {
                width = EXP_BAR_MAX_WIDTH;
            }
            Vector2 size = _img_exp.rectTransform.sizeDelta;
            size.x = width;
            _img_exp.rectTransform.sizeDelta = size;
        }

        /// <summary>
        /// MainUIModel/特效未移植:翻面红点与经验特效盒先隐藏(老客户端由 turn_red_dot 与 AddUIEffect 驱动可见性)。
        /// 不造假数据/特效,沿用既有 View 的 gameObject.SetActive(false) 收法。
        /// </summary>
        private void HideUnbackedIndicators()
        {
            _img_red.gameObject.SetActive(false);
            _box_exp_effect.gameObject.SetActive(false);
        }

        private void HideTemplates()
        {
            if (_tpl_MainFuncIconItem != null)
            {
                _tpl_MainFuncIconItem.SetActive(false);
            }
        }

        private void RefreshMainFuncIcons()
        {
            for (int i = 0; i < AlwaysOpenMainFuncIcons.Length; i++)
            {
                MainFuncIconItemBind item = GetOrCreateFuncIconItem(i);
                if (item == null) continue;

                item.gameObject.SetActive(true);
                SetFuncIconPosition(item, i * FUNC_ICON_GAP, 0f);
                if (item._img_red != null) item._img_red.gameObject.SetActive(false);
                _ = ResManager.SetImageAsync(item._img_icon, GameResPath.GetIcon("mainUI", AlwaysOpenMainFuncIcons[i]), nativeSize: false);
            }

            for (int i = AlwaysOpenMainFuncIcons.Length; i < _funcIconItems.Count; i++)
            {
                if (_funcIconItems[i] != null)
                {
                    _funcIconItems[i].gameObject.SetActive(false);
                }
            }
        }

        private MainFuncIconItemBind GetOrCreateFuncIconItem(int index)
        {
            while (_funcIconItems.Count <= index)
            {
                _funcIconItems.Add(null);
            }

            MainFuncIconItemBind item = _funcIconItems[index];
            if (item != null) return item;

            if (_tpl_MainFuncIconItem == null || _gp_icon_con == null)
            {
                GameLog.Error("MainUI", "MainUIDownView missing MainFuncIconItem template or _gp_icon_con");
                return null;
            }

            GameObject go = Instantiate(_tpl_MainFuncIconItem, _gp_icon_con);
            go.SetActive(true);

            item = go.GetComponent<MainFuncIconItemBind>();
            if (item == null)
            {
                GameLog.Error("MainUI", "MainFuncIconItem template missing bind component");
                Destroy(go);
                return null;
            }

            _funcIconItems[index] = item;
            return item;
        }

        private static void SetFuncIconPosition(MainFuncIconItemBind item, float x, float y)
        {
            RectTransform rt = (RectTransform)item.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
        }
    }
}
