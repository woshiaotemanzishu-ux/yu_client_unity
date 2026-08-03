using Shenxiao.Generated.UI.Setting;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Baby;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 改名子窗(对标老客户端 setting/SettingChangeNameView.ts):
    /// 输入新名字(InptextDisplay)+ 确定(confirmBtn)/取消(cancleBtn)/关闭(_close_btn)+
    /// 消耗显示(free_label 免费 / cost_conta:icon1 改名卡 cost1 / icon2 勾玉 cost2)。
    ///
    /// 协议链(轮5 接线,对标老端 RoleController.ts On42602/On42604/On42601):
    ///   打开:SettingView._btn_changename 点击 → RoleController.RequestRenameFreeCheck 发 42602 →
    ///        On42602 回包 → SettingFlow.OpenSub(本窗, result) 打开(is_free = result==1)。
    ///   confirmBtn:本地预检(按老端字符宽度口径校验 4~12 + ConfigLanguageMask 敏感词，
    ///        **与老端 TS 文案假设的"2~6个汉字"不同,以当前服务端 4~12 提示为准**)→
    ///        按 is_free/改名卡(38210001)库存/勾玉(300)余额决定
    ///        type(免费优先,改名卡优先于勾玉,对标老端 ticket_enough_ 先判)→ 发 42604。
    ///   42604 result==1 → EVT_ROLE_RENAME_CHECK_PASSED(name,type)→ 二次确认弹窗 → 确定发 42601。
    ///   42601 result==1 → toast「改名成功」;Figure.Name 的更新走既有 12086 广播路径(勿双改,
    ///        见 RoleController.On42601/SceneController.On12086 注释)→ EVT_ROLE_RENAME_SUCCESS → 本窗关闭。
    ///
    /// 降级:消耗道具图标(_tpl_BaseAwardItem 克隆)未接,仅显数字文案;42602/42604/42601 的改名卡/勾玉数值
    /// (38210001/300)取自服务端 data_rename 配置(硬编码,与客户端表无对应,老端亦无客户端表可读)。
    /// </summary>
    public sealed class SettingChangeNameView : SettingChangeNameViewBind
    {
        private const int TICKET_TYPE_ID = 38210001; // 改名卡(对标服务端 data_rename:get_cfg(2))
        private const int GOLD_COST = 300;            // 勾玉花费(对标服务端 data_rename:get_cfg(3))

        private bool _isFree;
        private bool _pending;
        private bool _subscribed;
        private int _showVersion;

        protected override void OnInit()
        {
            if (InptextDisplay != null) InptextDisplay.characterLimit = 12;
            HideTemplates();
            BindClose(_close_btn);
            BindClose(cancleBtn);
            BindConfirm();
            Subscribe();
        }

        protected override void OnDispose() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On<string, int>(GlobalEvent.EVT_ROLE_RENAME_CHECK_PASSED, OnCheckPassed);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_RENAME_CHECK_FAILED, OnRenameFailed);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_RENAME_SUCCESS, OnRenameSuccess);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off<string, int>(GlobalEvent.EVT_ROLE_RENAME_CHECK_PASSED, OnCheckPassed);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_RENAME_CHECK_FAILED, OnRenameFailed);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_RENAME_SUCCESS, OnRenameSuccess);
            _subscribed = false;
        }

        /// <summary>args = 42602 回包 result(1=免费/2=否,由 RoleController.On42602 透传)。</summary>
        protected override void OnShow(object args)
        {
            _showVersion++;
            _pending = false;
            _isFree = args is int freeResult && freeResult == 1;
            if (free_label != null) free_label.gameObject.SetActive(_isFree);
            if (cost_conta != null) cost_conta.gameObject.SetActive(!_isFree);
            if (!_isFree)
            {
                if (cost1 != null) cost1.text = "1";
                if (cost2 != null) cost2.text = GOLD_COST.ToString();
            }
            if (InptextDisplay != null) InptextDisplay.text = string.Empty;

            Canvas.ForceUpdateCanvases();
            if (transform is RectTransform root) LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Canvas.ForceUpdateCanvases();
            _ = BabyNameMask.EnsureLoaded();
            GameLog.Info("Setting", "SettingChangeNameView 打开: isFree={0}", _isFree);
        }

        protected override void OnHide()
        {
            _showVersion++;
            _pending = false;
        }

        /// <summary>消耗道具模板(BaseAwardItem 克隆源)未移植先隐藏。GameObject 用 SetActive,不走 HideNode。</summary>
        private void HideTemplates()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
        }

        private void BindConfirm()
        {
            Image img = confirmBtn != null ? confirmBtn.GetComponentInChildren<Image>(true) : null;
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, OnConfirmClick);
        }

        private async void OnConfirmClick()
        {
            if (_pending) return;
            string name = BabyRenameView.NormalizeName(InptextDisplay != null ? InptextDisplay.text : string.Empty);
            if (!BabyRenameView.IsValidLength(name))
            {
                TipsManager.Toast("名字长度需为 4-12 个字符");
                return;
            }

            int version = _showVersion;
            _pending = true;
            await BabyNameMask.EnsureLoaded();
            if (!_pending || version != _showVersion || !IsShown) return;
            if (BabyNameMask.Contains(name))
            {
                _pending = false;
                TipsManager.Toast("内容含有敏感词");
                return;
            }

            int type;
            if (_isFree)
            {
                type = RoleController.RENAME_TYPE_FREE;
            }
            else if (BagModel.Instance.GetTypeGoodsNum(TICKET_TYPE_ID) >= 1)
            {
                type = RoleController.RENAME_TYPE_CARD; // 改名卡优先于勾玉(对标老端 ticket_enough_ 先判)
            }
            else if (RoleModel.Instance.Gold >= GOLD_COST)
            {
                type = RoleController.RENAME_TYPE_GOLD;
            }
            else
            {
                _pending = false;
                TipsManager.Toast("改名卡不足，元宝不足");
                return;
            }

            RoleController.Instance.CheckRename(name, type);
        }

        private void OnCheckPassed(string name, int type)
        {
            if (!IsShown)
            {
                _pending = false;
                return;
            }
            TipsManager.Confirm("是否确定使用『" + name + "』作为新名字？",
                () => RoleController.Instance.SubmitRename(name, type),
                () => _pending = false);
        }

        private void OnRenameSuccess()
        {
            _pending = false;
            if (IsShown) Hide();
        }

        private void OnRenameFailed() => _pending = false;

        /// <summary>关闭/取消按钮(Image 或含 Image 容器)→ Hide(关闭本窗)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }
    }
}
