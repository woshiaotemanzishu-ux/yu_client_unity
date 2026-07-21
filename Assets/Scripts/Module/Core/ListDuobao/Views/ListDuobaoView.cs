using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.ListDuobao;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.CustomActivity;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.ListDuobao
{
    public sealed class ListDuobaoView : ListDuobaoViewBind
    {
        private readonly List<GameObject> _poolCells = new List<GameObject>();
        private bool _eventsBound;
        private int _selectedStageId;

        protected override void OnInit()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            BindClick(_img_bg, ListDuobaoFlow.OpenRank);
            BindClick(_img_arr, ListDuobaoFlow.OpenRank);
            BindClick(_btn1, OnHelp);
            BindClick(_btn2, ListDuobaoFlow.OpenRecord);
            BindClick(_btn3, OnStageAction);
            BindClick(_btn4, ListDuobaoFlow.OpenReward);
            BindClick(_btn_one, OnDrawOne);
            BindClick(_btn_ten, OnDrawTen);
        }

        protected override void OnShow(object args)
        {
            BindEvents();
            CustomActivityModel.Instance.MarkListDuobaoEntered();
            RefreshData();
        }

        protected override void OnHide()
        {
            UnbindEvents();
            ClearPool();
        }

        public void RefreshData() => _ = RefreshDataAsync();

        private async Task RefreshDataAsync()
        {
            await ListDuobaoConfigs.EnsureLoaded();
            if (!IsShown) return;
            CustomActivityModel model = CustomActivityModel.Instance;
            CustomActivityModel.ListDuobaoStageInfo info = model.ListDuobaoStage;
            if (info == null || info.Type != ListDuobaoFlow.BaseType || info.SubType != model.ListDuobaoSubType) return;

            if (_lb_title != null) _lb_title.text = model.GetActiveListDuobaoAct()?.Name ?? "连服夺宝";
            RefreshPool(info);
            RefreshStage(info);
            RefreshCosts(info);
            if (_red != null) _red.gameObject.SetActive(model.HasListDuobaoStageRed());
        }

        private void RefreshPool(CustomActivityModel.ListDuobaoStageInfo info)
        {
            ClearPool();
            if (_tpl_BaseAwardItem == null) return;
            RectTransform[] slots = { _gp0, _gp1, _gp2, _gp3, _gp4, _gp5, _gp6, _gp7, _gp8, _gp9, _gp10, _gp11 };
            int slot = 0;
            for (int i = 0; i < info.RewardList.Count && slot < slots.Length; i++)
            {
                List<CustomActivityModel.RewardObj> list = info.RewardList[i].Reward;
                for (int k = 0; k < list.Count && slot < slots.Length; k++, slot++)
                {
                    if (slots[slot] == null) continue;
                    GameObject go = Instantiate(_tpl_BaseAwardItem, slots[slot]);
                    go.SetActive(true);
                    BaseAwardItem item = go.GetComponent<BaseAwardItem>();
                    if (item != null) item.SetData(list[k].GoodsId, list[k].Num, list[k].Type > 0);
                    _poolCells.Add(go);
                }
            }
        }

        private void RefreshStage(CustomActivityModel.ListDuobaoStageInfo info)
        {
            CustomActivityModel.ListDuobaoStageState selected = default;
            bool found = false;
            for (int i = 0; i < info.StageList.Count; i++)
            {
                selected = info.StageList[i];
                if (selected.GotType != 2) { found = true; break; }
            }
            if (!found && info.StageList.Count > 0) selected = info.StageList[info.StageList.Count - 1];
            _selectedStageId = selected.Id;
            ListDuobaoConfigs.StageRow row = ListDuobaoConfigs.GetStage(info.Type, info.SubType, _selectedStageId);
            int need = row?.NeedValue ?? 0;
            // 老端 ListDuobaoView.ts:241-265 使用总积分 score，不是 today_score。
            if (_lb_pro != null) _lb_pro.text = need > 0 ? info.Score + "/" + need : info.Score.ToString();
            if (_img_fill != null) _img_fill.fillAmount = need > 0 ? Mathf.Clamp01((float)info.Score / need) : 0f;
            if (_gp_effect != null) _gp_effect.gameObject.SetActive(selected.GotType == 1);
        }

        private void RefreshCosts(CustomActivityModel.ListDuobaoStageInfo info)
        {
            string condition = !string.IsNullOrEmpty(info.Condition) ? info.Condition : modelCondition();
            ListDuobaoConfigs.TryReadCost(condition, "one_cost", out ListDuobaoConfigs.CostEntry one);
            ListDuobaoConfigs.TryReadCost(condition, "ten_cost", out ListDuobaoConfigs.CostEntry ten);
            ListDuobaoConfigs.TryReadCondition(condition, "score", out int score);
            if (_lb_cost1 != null) _lb_cost1.text = one.Num.ToString();
            if (_lb_cost10 != null) _lb_cost10.text = ten.Num.ToString();
            if (_lb_have1 != null) _lb_have1.text = RoleModel.Instance.BGold.ToString();
            if (_lb_have10 != null) _lb_have10.text = RoleModel.Instance.BGold.ToString();
            if (_lb_tips != null) _lb_tips.text = "每次夺宝可获得" + score + "积分";
        }

        private string modelCondition() => CustomActivityModel.Instance.GetActiveListDuobaoAct()?.Condition ?? "";

        private void OnStageAction()
        {
            CustomActivityModel.ListDuobaoStageInfo info = CustomActivityModel.Instance.ListDuobaoStage;
            if (info == null || _selectedStageId <= 0) return;
            CustomActivityModel.ListDuobaoStageState state = info.StageList.Find(v => v.Id == _selectedStageId);
            if (state.GotType == 1)
                CustomActivityController.Instance.ClaimListDuobaoReward(info.Type, info.SubType, _selectedStageId);
            else
                ListDuobaoFlow.OpenReward();
        }

        private void OnDrawOne() => Draw("one_cost", 1);
        private void OnDrawTen() => Draw("ten_cost", 10);

        private void Draw(string key, int times)
        {
            // 老端 ListDuobaoView.ts 固定发 33191；服务端在 type=116 时转给 rush_treasure，
            // 最终以 S2C 33803 返回抽奖结果。这里的请求/响应命令号本来就不对称。
            CustomActivityModel model = CustomActivityModel.Instance;
            CustomActivityModel.ListDuobaoStageInfo info = model.ListDuobaoStage;
            if (info == null || !ListDuobaoConfigs.TryReadCost(!string.IsNullOrEmpty(info.Condition) ? info.Condition : modelCondition(), key, out ListDuobaoConfigs.CostEntry cost))
            {
                TipsManager.Toast("活动消耗配置缺失");
                return;
            }
            if (cost.Type != 2)
            {
                CustomActivityController.Instance.RequestMoneyTreeDraw(info.Type, info.SubType, times, 0);
                return;
            }
            long bound = RoleModel.Instance.BGold;
            long unbound = RoleModel.Instance.Gold;
            if (bound >= cost.Num)
            {
                CustomActivityController.Instance.RequestMoneyTreeDraw(info.Type, info.SubType, times, 0);
            }
            else if (bound + unbound >= cost.Num)
            {
                TipsManager.Confirm("绑定元宝不足，是否使用非绑定元宝补足？",
                    () => CustomActivityController.Instance.RequestMoneyTreeDraw(info.Type, info.SubType, times, 1));
            }
            else if (MainUIRouter.IsRegistered("recharge")) MainUIRouter.Open("recharge");
            else TipsManager.Toast("元宝不足");
        }

        private static void OnHelp() => TipsManager.Toast("夺宝可获得积分，积分达到阶段要求后可领取奖励");

        private void BindEvents()
        {
            if (_eventsBound) return;
            EventDispatcher.On<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetailUpdate);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleUpdate);
            EventDispatcher.On<CustomActivityModel.ListDuobaoDrawResult>(GlobalEvent.EVT_LIST_DUOBAO_DRAW_RESULT, OnDrawResult);
            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound) return;
            EventDispatcher.Off<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetailUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleUpdate);
            EventDispatcher.Off<CustomActivityModel.ListDuobaoDrawResult>(GlobalEvent.EVT_LIST_DUOBAO_DRAW_RESULT, OnDrawResult);
            _eventsBound = false;
        }

        private void OnDetailUpdate(int type, int subType) { if (type == ListDuobaoFlow.BaseType && subType == CustomActivityModel.Instance.ListDuobaoSubType) RefreshData(); }
        private void OnRoleUpdate() => RefreshData();
        private void OnDrawResult(CustomActivityModel.ListDuobaoDrawResult result) { if (result != null) RefreshData(); }

        private void ClearPool()
        {
            for (int i = 0; i < _poolCells.Count; i++) if (_poolCells[i] != null) Destroy(_poolCells[i]);
            _poolCells.Clear();
        }

        private static void BindClick(Component target, System.Action action)
        {
            if (target == null) return;
            Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);
            if (graphic != null) UIUtil.ClearClicks(graphic);
            UIUtil.AddClick(target, action);
        }
    }
}
