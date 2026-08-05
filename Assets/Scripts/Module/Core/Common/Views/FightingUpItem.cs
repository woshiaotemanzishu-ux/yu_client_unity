using Shenxiao.Generated.UI.Common;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 战力提升项(对标老客户端 common/FightingUpItem.ts):显示战力提升数值(+箭头)。Updatefighting(value)→数字。
    ///
    /// num_new_green/view_fight_up 均为老端彩色 BMFont 转成的静态 TMP_FontAsset；
    /// SetArrowStyle 对齐老端 SetArrowStype 的两种数字字形。_tpl_WithBtnHSlider 隐藏。
    /// </summary>
    public sealed class FightingUpItem : FightingUpItemBind
    {
        [SerializeField] private TMP_FontAsset _style1Font;
        [SerializeField] private TMP_FontAsset _style2Font;
        private int _style = 1;

        protected override void OnInit()
        {
            if (_tpl_WithBtnHSlider != null) _tpl_WithBtnHSlider.SetActive(false);
            ApplyBitmapFont();
        }

        /// <summary>填战力提升值(对标 Updatefighting)。</summary>
        public void SetFighting(long fighting)
        {
            if (_lb_fighting != null) _lb_fighting.text = fighting.ToString();
        }

        /// <summary>1=num_new_green，2=view_fight_up；名称订正但保留老端拼写别名。</summary>
        public void SetArrowStyle(int style)
        {
            _style = style == 2 ? 2 : 1;
            ApplyBitmapFont();
        }

        public void SetArrowStype(int style) => SetArrowStyle(style);

        private void ApplyBitmapFont()
        {
            if (_lb_fighting == null) return;
            TMP_FontAsset font = _style == 2 ? _style2Font : _style1Font;
            if (font == null) return;
            _lb_fighting.font = font;
            _lb_fighting.fontSharedMaterial = font.material;
            _lb_fighting.color = Color.white;
        }
    }
}
