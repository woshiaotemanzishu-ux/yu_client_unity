using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.GameNotice;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GameNotice
{
    /// <summary>
    /// 游戏公告标题项(对标老客户端 gameNotice/GameNoticeListItem.ts):标题(_lab_title)+ 选中底图(_img_select)+ 红点(_img_red)。
    ///
    /// SetData(title) + SetSelected(bool)。降级:红点 OnInit 隐藏;选中用底图显隐。由 GameNoticeView 克隆。
    /// </summary>
    public sealed class GameNoticeListItem : GameNoticeListItemBind
    {
        private Image _clickSurface;

        protected override void OnInit()
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++) graphics[i].raycastTarget = false;

            _clickSurface = GetComponent<Image>();
            if (_clickSurface == null)
            {
                _clickSurface = gameObject.AddComponent<Image>();
                _clickSurface.color = Color.clear;
            }
            _clickSurface.raycastTarget = true;
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            if (_img_select != null) _img_select.gameObject.SetActive(false);
        }

        public void SetData(string title, bool unread, Action onClick)
        {
            if (_lab_title != null) _lab_title.text = title ?? "";
            if (_img_red != null) _img_red.gameObject.SetActive(unread);
            if (_clickSurface != null)
            {
                UIUtil.ClearClicks(_clickSurface);
                UIUtil.AddClick(_clickSurface, onClick);
            }
        }

        public void SetSelected(bool selected)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
        }
    }
}
