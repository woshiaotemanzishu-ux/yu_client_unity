using System.Collections.Generic;
using Shenxiao.Generated.UI.InnateSkill;
using UnityEngine;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 天赋技能树容器(对标老客户端 innateSkill/InnateListItem.ts):按 type 切换四套互斥连线装饰
    /// (_gp_skill5/6/7/8)+ 高度动态(type7→850,5/6→637;type8 老端本身缺项/本端按几何报告兜底同 850)+
    /// 技能图标逐个摆位(_gp_item,坐标来自 <see cref="SkillUIConfigs.GetInnateSlots"/> 绝对坐标,配置驱动的
    /// 内容坐标,非布局铁律要求的"结构位置",故允许在此代码里按数据设置)。
    ///
    /// 单例常驻(由 <see cref="Shenxiao.Editor.UiCreator.Role.InnateSkillCreator"/> 建成 _Scroller1.content 下
    /// 永久可见子节点,非运行时克隆),技能图标 <see cref="InnateSkillItem"/> 从 _gp_item 下隐藏模板按需克隆复用池管理。
    /// </summary>
    public sealed class InnateListItem : InnateListItemBind
    {
        private const float HeightType7 = 850f;
        private const float HeightType8Fallback = 850f; // 老端 InitView 只显式处理 5/6/7,type8 缺项:本端兜底同 type7
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
            float h = type == 7 ? HeightType7 : (type == 8 ? HeightType8Fallback : HeightDefault);
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);
        }

        private void PopulateItems(int type, int selectedSkillId)
        {
            foreach (InnateSkillItem it in _itemPool) if (it != null) it.gameObject.SetActive(false);
            if (_itemTemplate == null || _gp_item == null) return;

            int career = Shenxiao.Module.Core.Role.RoleModel.Instance.Career;
            List<Shenxiao.Module.Core.Skill.SkillUIConfigs.InnateSlot> slots =
                Shenxiao.Module.Core.Skill.SkillUIConfigs.GetInnateSlots(type, career);

            for (int i = 0; i < slots.Count; i++)
            {
                InnateSkillItem it = GetOrCreate(i);
                if (it == null) continue;
                it.gameObject.SetActive(true);
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
