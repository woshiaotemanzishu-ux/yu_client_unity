using Shenxiao.Generated.UI.Chat;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Chat
{
    /// <summary>
    /// 聊天表情/工具弹层(对标老客户端 chat/ChatToolPanel.ts):由主窗 faceBtn 触发的 arrow 箭头弹出层
    /// (bg 背景 + arrow 箭头 + itemScroller/Content 表情网格,克隆 ChatToolGridItem)。OpenToggle:再点 faceBtn 关闭,
    /// 自身无关闭按钮、点 bg/返回可关 → 由外部(ChatFlow.ToggleSub)收口,本 View 不绑关闭。
    ///
    /// 降级:ChatModel(faceList 表情数据)/LoopScrowViewMgr(网格滚动)/ChatToolGridItem 均未移植 →
    /// 网格为空、不铺 item,模板 _tpl_ChatToolGridItem 隐藏,OnShow 仅打日志。后续 tick 接 ChatModel 表情列表。
    /// </summary>
    public sealed class ChatToolPanel : ChatToolPanelBind
    {
        protected override void OnInit()
        {
            // 网格 item 模板(GameObject)→ 隐藏,等 ChatModel.faceList 接好后克隆铺设。
            if (_tpl_ChatToolGridItem != null) _tpl_ChatToolGridItem.SetActive(false);
            // 无 _btn_close/_close 字段:弹层由外部 faceBtn 再点关闭,本 View 不绑关闭。
        }

        protected override void OnShow(object args)
        {
            // 老端 open(localPoint) → setLocal 摆位 + initView(faceList) 铺表情网格。ChatModel/LoopScrowViewMgr 未移植 → 列表空降级。
            GameLog.Info("Chat", "ChatToolPanel 打开 → 待对接 ChatModel(列表空/默认降级)");
        }
    }
}
