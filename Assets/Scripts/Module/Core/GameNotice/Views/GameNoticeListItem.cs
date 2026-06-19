using Shenxiao.Generated.UI.GameNotice;

namespace Shenxiao.Module.Core.GameNotice
{
    /// <summary>
    /// 游戏公告标题项(对标老客户端 gameNotice/GameNoticeListItem.ts):标题(_lab_title)+ 选中底图(_img_select)+ 红点(_img_red)。
    ///
    /// SetData(title) + SetSelected(bool)。降级:红点 OnInit 隐藏;选中用底图显隐。由 GameNoticeView 克隆。
    /// </summary>
    public sealed class GameNoticeListItem : GameNoticeListItemBind
    {
        protected override void OnInit()
        {
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            if (_img_select != null) _img_select.gameObject.SetActive(false);
        }

        public void SetData(string title)
        {
            if (_lab_title != null) _lab_title.text = title ?? "";
        }

        public void SetSelected(bool selected)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
        }
    }
}
