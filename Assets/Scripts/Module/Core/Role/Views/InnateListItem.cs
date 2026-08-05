using System.Collections.Generic;
using Shenxiao.Generated.UI.InnateSkill;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 天赋技能树容器(对标老客户端 innateSkill/InnateListItem.ts):按 type 切换四套互斥连线装饰
    /// (_gp_skill5/6/7/8)+ 高度动态(type7→820,其余→637)+
    /// 技能图标逐个摆位(_gp_item,坐标来自 <see cref="SkillUIConfigs.GetInnateSlots"/> 绝对坐标,配置驱动的
    /// 内容坐标,非布局铁律要求的"结构位置",故允许在此代码里按数据设置)。
    ///
    /// 单例常驻(由 <see cref="Shenxiao.Editor.UiCreator.Role.InnateSkillCreator"/> 建成 _Scroller1.content 下
    /// 永久可见子节点,非运行时克隆),技能图标 <see cref="InnateSkillItem"/> 从 _gp_item 下隐藏模板按需克隆复用池管理。
    /// </summary>
    public sealed class InnateListItem : InnateListItemBind
    {
        private const float HeightType7 = 820f;
        private const float HeightDefault = 637f;

        private GameObject _itemTemplate;
        private readonly List<InnateSkillItem> _itemPool = new List<InnateSkillItem>();
        private int _selectedSkillId;

        public System.Action<int> OnItemClicked;

        protected override void OnInit()
        {
            if (_gp_item != null && _gp_item.childCount > 0)
                _itemTemplate = _gp_item.GetChild(0).gameObject;
        }

        /// <summary>切换分支(对标 InnateListItem.SetDate):重排装饰/高度/技能图标。</summary>
        public void SetType(int type, int selectedSkillId)
        {
            _selectedSkillId = selectedSkillId;
            ApplyGroupVisible(type);
            ApplyHeight(type);
            PopulateItems(type, selectedSkillId);
        }

        /// <summary>仅更新选中态(不重建图标位置),供点击某技能图标后局部刷新。</summary>
        public void SetSelected(int selectedSkillId)
        {
            _selectedSkillId = selectedSkillId;
            foreach (InnateSkillItem it in _itemPool)
                if (it != null && it.gameObject.activeSelf) it.SetSelected(it.SkillId == selectedSkillId);
        }

        private void ApplyGroupVisible(int type)
        {
            if (_gp_skill5 != null) _gp_skill5.gameObject.SetActive(type == 5);
            if (_gp_skill6 != null) _gp_skill6.gameObject.SetActive(type == 6);
            if (_gp_skill7 != null) _gp_skill7.gameObject.SetActive(type == 7);
            if (_gp_skill8 != null) _gp_skill8.gameObject.SetActive(type == 8);
        }

        private void ApplyHeight(int type)
        {
            float h = type == 7 ? HeightType7 : HeightDefault;
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);

            // 转换后的层级是 ScrollRect→Viewport→Content→InnateListItem。
            // 只改 InnateListItem 高度不会给 ScrollRect 产生可滚动距离；type7 的末两项会被详情板永久遮住。
            // 动态内容高度必须同步到 ScrollRect 真正引用的 Content，并在切换分支时回到顶部。
            RectTransform content = rt.parent as RectTransform;
            ScrollRect scroll = content != null ? content.GetComponentInParent<ScrollRect>() : null;
            if (content != null && scroll != null && scroll.content == content)
            {
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.sizeDelta = new Vector2(0f, h);
                content.anchoredPosition = Vector2.zero;
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }
        }

        private void PopulateItems(int type, int selectedSkillId)
        {
            foreach (InnateSkillItem it in _itemPool) if (it != null) it.Hide();
            if (_itemTemplate == null || _gp_item == null) return;

            int career = Shenxiao.Module.Core.Role.RoleModel.Instance.Career;
            List<Shenxiao.Module.Core.Skill.SkillUIConfigs.InnateSlot> slots =
                Shenxiao.Module.Core.Skill.SkillUIConfigs.GetInnateSlots(type, career);

            for (int i = 0; i < slots.Count; i++)
            {
                InnateSkillItem it = GetOrCreate(i);
                if (it == null) continue;
                // 克隆自隐藏模板的 BaseView 必须走 Show，才能执行 BindNodes/OnInit 并建立真实点击面。
                // 只 SetActive 会让图标看得见、SetData 也能工作，但 UIUtil.AddClick 永远没有机会绑定。
                it.Show();
                it.OnClicked = OnItemClicked;
                PlaceTopLeft((RectTransform)it.transform, slots[i].X, slots[i].Y);
                it.SetData(slots[i].SkillId, type, slots[i].SkillId == selectedSkillId);
            }
        }

        private InnateSkillItem GetOrCreate(int index)
        {
            while (_itemPool.Count <= index)
            {
                GameObject clone = Instantiate(_itemTemplate, _gp_item);
                _itemPool.Add(clone.GetComponent<InnateSkillItem>());
            }
            return _itemPool[index];
        }

        private static void PlaceTopLeft(RectTransform rt, float x, float y)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
        }
    }
}
