using Shenxiao.Generated.UI.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 战力展示小项(对标老客户端 common/FightingShowSmallItem.ts):主战力数字 + 火焰特效 + 提升框(_box_up 内 FightingUpItem)。
    ///
    /// SetFighting(value)→主战力;SetFightingUp(value)→提升框(>0 才显,克隆 _tpl_FightingUpItem)。
    /// num_new 使用老端彩色 BMFont 转成的静态 TMP_FontAsset并直接保存在 Prefab；火焰特效仍待移植。
    /// </summary>
    public sealed class FightingShowSmallItem : FightingShowSmallItemBind
    {
        private FightingUpItem _upItem;

        protected override void OnInit()
        {
            if (_gp_fire_effect != null) _gp_fire_effect.gameObject.SetActive(false); // 火焰特效待移植
            if (_tpl_FightingUpItem != null) _tpl_FightingUpItem.SetActive(false);
            if (_tpl_WithBtnHSlider != null) _tpl_WithBtnHSlider.SetActive(false);
            if (_box_up != null) _box_up.gameObject.SetActive(false);
        }

        /// <summary>主战力(对标 Updatefighting)。</summary>
        public void SetFighting(long fighting)
        {
            if (_lb_fighting != null) _lb_fighting.text = fighting.ToString();
        }

        /// <summary>战力提升框(对标 UpdatefightingUp:>0 才显,克隆 FightingUpItem)。</summary>
        public void SetFightingUp(long fighting)
        {
            bool show = fighting > 0;
            if (_box_up != null) _box_up.gameObject.SetActive(show);
            if (!show) return;

            if (_upItem == null && _tpl_FightingUpItem != null && _box_up != null)
            {
                GameObject go = Instantiate(_tpl_FightingUpItem, _box_up);
                go.SetActive(true);
                _upItem = go.GetComponent<FightingUpItem>();
            }
            if (_upItem != null) _upItem.SetFighting(fighting);
        }
    }
}
