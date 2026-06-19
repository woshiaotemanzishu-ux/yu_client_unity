using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Buff 列表项(对标老客户端 MainUIBuffItem.ts dataChanged):图标 + 名称 + 剩余时间 + 说明。
    ///
    /// 降级:Buff 数据源(GoodsModel / MainUIModel.buff_list / SkillManager / 服务器时间 / 配置表
    /// cfg.icon·name·time·desc)尚未移植 → 用最小 DTO <see cref="BuffItemData"/> 填字段;倒计时
    /// 遮罩(_img_mask/_lb_time2 的 pie 动画 + 1s timer)、蒙面 buff 的 info/help 入口(MaskInfoView/
    /// 说明协议)依赖未移植系统,先隐藏。数据/协议移植后再补 timer 与点击。
    /// </summary>
    public sealed class MainUIBuffItem : MainUIBuffItemBind
    {
        /// <summary>由父列表 MainUIBuffView 克隆后调用,填本项展示数据。</summary>
        public void SetData(BuffItemData data)
        {
            if (data == null) return;

            if (_lb_name != null) _lb_name.text = data.Name ?? "";
            if (_lb_time != null) _lb_time.text = data.TimeText ?? "";
            // _html_desc 老端是富文本 HTMLDivElement,转换产物为 TMP,先按纯文本填。
            if (_html_desc != null) _html_desc.text = data.Desc ?? "";

            // 图标:对标 GetIcon("bufficon", cfg.icon)。
            if (_img_buff != null && !string.IsNullOrEmpty(data.Icon))
            {
                _ = ResManager.SetImageAsync(_img_buff, GameResPath.GetIcon("bufficon", data.Icon), nativeSize: false);
            }

            // 倒计时遮罩依赖服务器时间 + pie 动画(未移植)→ 先隐藏,不伪造。
            if (_img_mask != null) _img_mask.gameObject.SetActive(false);
            if (_lb_time2 != null) _lb_time2.text = "";

            // 蒙面 buff(mengmr)的 info/help 入口依赖 MaskInfoView/说明协议(未移植)→ 先隐藏。
            if (_gp_info != null) _gp_info.gameObject.SetActive(false);
            if (_img_help != null) _img_help.gameObject.SetActive(false);
        }
    }

    /// <summary>Buff 项最小展示数据(待 GoodsModel/Buff 配置表 + 协议移植后由真实数据填充)。</summary>
    public sealed class BuffItemData
    {
        public string Icon;
        public string Name;
        public string TimeText;
        public string Desc;
    }
}
