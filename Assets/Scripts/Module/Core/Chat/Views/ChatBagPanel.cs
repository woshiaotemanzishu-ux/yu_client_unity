using Shenxiao.Generated.UI.Chat;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Chat
{
    /// <summary>
    /// 聊天工具背包弹层(对标老客户端 chat/ChatBagPanel.ts):由聊天主窗 _bag 按钮 arrow 弹出的下拉层,自身无关闭按钮
    /// (OpenToggle:外部 _bag 再点即关、点空白背景关)。左侧页签条(tabScroller/Content11 克隆 ChatParentTab:
    /// 背包/装备/影骸战衣/神器/启示圣铠/神霄御府/九天神祭,按等级/功能开放动态增列)+ 主道具网格(itemScroller/Content1
    /// 铺 ChatToolBagItem)+ 套装栏(itemScroller2/Content 铺 ChatToolBagItem2 + _gp_title 标题,有数据才显)。
    ///
    /// 降级:ChatModel(chatToolBagData/chatBagData/chatSealBagData… 各 tab 数据源)、ChatParentTab/ChatToolBagItem(2)
    /// 循环列表、config_seal_kv/功能开放校验、子弹层(GodCourtView/GodBefallMainView/longlanguageView/RevelationEquipView/
    /// BaseAwardItem)均未移植 → 页签/道具/套装列表空、套装栏+标题不铺、_tpl_* 模板隐藏。Bind 无 _btn_close → 不绑关闭
    /// (由外部 _bag 收口)。开关由 ChatFlow.ToggleSub 驱动,OnShow 打降级日志,等 ChatModel/列表系统补齐再对接。
    /// </summary>
    public sealed class ChatBagPanel : ChatBagPanelBind
    {
        protected override void OnInit()
        {
            HideTemplates();
        }

        protected override void OnShow(object args)
        {
            // 老端 open_callback → initTab() 建页签 + Fire(CHAT_BAG_OPEN) 铺各 tab 列表。ChatModel/循环列表未移植 → 列表空降级。
            GameLog.Info("Chat", "ChatBagPanel 打开 → 待对接 ChatModel(列表空/默认降级)");
        }

        /// <summary>_tpl_* 模板节点(GodCourt*/GodBefallMainView/longlanguageView/RevelationEquipView/BaseAwardItem/ChatParentTab)依赖列表系统克隆,未移植先隐藏。</summary>
        private void HideTemplates()
        {
            if (_tpl_GodCourtView != null) _tpl_GodCourtView.SetActive(false);
            if (_tpl_GodCourtStrenItem != null) _tpl_GodCourtStrenItem.SetActive(false);
            if (_tpl_GodCourtSuitItem != null) _tpl_GodCourtSuitItem.SetActive(false);
            if (_tpl_GodCourtSuitSingle != null) _tpl_GodCourtSuitSingle.SetActive(false);
            if (_tpl_GodCourtTabItem != null) _tpl_GodCourtTabItem.SetActive(false);
            if (_tpl_ChatParentTab != null) _tpl_ChatParentTab.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_GodBefallMainView != null) _tpl_GodBefallMainView.SetActive(false);
            if (_tpl_longlanguageView != null) _tpl_longlanguageView.SetActive(false);
            if (_tpl_RevelationEquipView != null) _tpl_RevelationEquipView.SetActive(false);
        }
    }
}
