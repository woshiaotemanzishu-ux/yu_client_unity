using System.Collections.Generic;
using Shenxiao.Generated.UI.TransferJob;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.TransferJob
{
    /// <summary>
    /// 转职卡界面(对标老客户端 transferJob/TransferJobCardView.ts):转职卡列表(TransferJobCardItem)+
    /// 标题(lblTitle)/说明(lblDesc)+ 关闭(spClose)。
    ///
    /// 数据链(轮5 接线):OnShow → TransferJobModel.EnsureLoaded(config_career + ClientTransfer)→
    /// GetTransferTargets(自身职业) 除自身职业外全部目标卡,按 career 升序铺列表(对标老端
    /// Object.keys(careerCfg).filter(...).sort(...))。点某卡片 → 二次确认(Alert 逐字对标老端
    /// sureCbk 文案)→ 确定发 13045(<see cref="TransferJobController.RequestTransfer"/>)。
    /// </summary>
    public sealed class TransferJobCardView : TransferJobCardViewBind
    {
        private const float ItemHeight = 120f;
        private const float ItemSpacing = 4f; // 对标老端 List spaceY=4

        private readonly List<TransferJobCardItem> _items = new List<TransferJobCardItem>();

        protected override void OnInit()
        {
            if (_tpl_TransferJobCardItem != null) _tpl_TransferJobCardItem.SetActive(false);
            BindBtn(spClose, Hide);
        }

        protected override async void OnShow(object args)
        {
            await TransferJobModel.EnsureLoaded();
            if (!IsShown) return; // 加载期间被关闭
            BuildList();
        }

        private void BuildList()
        {
            if (listTransfer == null || listTransfer.content == null || _tpl_TransferJobCardItem == null) return;

            RectTransform content = listTransfer.content;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);

            int myCareer = RoleModel.Instance.Career;
            List<TransferJobModel.CareerEntry> targets = TransferJobModel.GetTransferTargets(myCareer);

            for (int i = 0; i < targets.Count; i++)
            {
                TransferJobCardItem item = GetOrCreateItem(content, i);
                if (item == null) continue;

                int career = targets[i].Career;
                int sex = targets[i].Sex;
                TransferJobModel.CareerMsg msg = TransferJobModel.GetCareerMsg(career);
                item.SetData(career, sex, msg?.Desc1 ?? "", msg?.Desc2 ?? "");
                string careerName = msg != null && !string.IsNullOrEmpty(msg.Name) ? msg.Name : ("职业" + career);
                item.BindSure(() => OnPickCareer(career, sex, careerName));

                item.gameObject.SetActive(true);
                var rt = (RectTransform)item.transform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(0f, -i * (ItemHeight + ItemSpacing));
            }
            for (int i = targets.Count; i < _items.Count; i++)
            {
                if (_items[i] != null) _items[i].gameObject.SetActive(false);
            }

            content.sizeDelta = new Vector2(content.sizeDelta.x,
                targets.Count > 0 ? targets.Count * (ItemHeight + ItemSpacing) - ItemSpacing : 0f);

            if (!TransferJobModel.IsLoaded)
            {
                GameLog.Warn("TransferJob", "TransferJobCardView: config_career/ClientTransfer 未加载,列表为空(TODO:跑配表同步)");
            }
        }

        private TransferJobCardItem GetOrCreateItem(Transform content, int index)
        {
            while (_items.Count <= index) _items.Add(null);
            if (_items[index] != null) return _items[index];

            GameObject go = Instantiate(_tpl_TransferJobCardItem, content);
            go.name = "TransferJobCardItem_" + index;
            TransferJobCardItem item = go.GetComponent<TransferJobCardItem>();
            if (item == null) { Destroy(go); return null; }
            item.Show();
            _items[index] = item;
            return item;
        }

        /// <summary>点转职卡二次确认(文案逐字对标老端 TransferJobCardItem.ts:sureCbk),确定才真正发 13045;
        /// 老端点击同时立即 Fire(CLOSE_VIEW,"TransferJobCardView") 关闭列表窗(确认框是独立弹层,不依赖本窗)。</summary>
        private void OnPickCareer(int career, int sex, string careerName)
        {
            int myCareer = RoleModel.Instance.Career;
            TransferJobModel.CareerMsg myMsg = TransferJobModel.GetCareerMsg(myCareer);
            string myName = myMsg != null && !string.IsNullOrEmpty(myMsg.Name) ? myMsg.Name : ("职业" + myCareer);

            TipsManager.Confirm(
                "使用转职卡后您的职业由原来的『" + myName + "』更改为『" + careerName + "』\n" +
                "身上穿戴的装备将会自动转换为新职业装备，背包/仓库道具不进行转换！\n转职有风险，请谨慎选择！",
                () => TransferJobController.Instance.RequestTransfer(career, sex));
            Hide();
        }

        private void BindBtn(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
