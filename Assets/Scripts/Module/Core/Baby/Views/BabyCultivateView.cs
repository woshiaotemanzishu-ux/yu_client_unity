using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝已激活后的培养页静态基线；后续培养数据在独立轮次接入。</summary>
    public partial class BabyCultivateView : BabyCultivateViewBind
    {
        private bool _listening;
        private bool _shown;
        private readonly List<GameObject> _taskItems = new List<GameObject>();

        protected override void OnInit()
        {
            UIUtil.AddClick(lvBtnGp, () => SelectPage(0));
            UIUtil.AddClick(stageBtnGp, () => SelectPage(1));
            UIUtil.AddClick(upBtn, () => BabyController.Instance.RequestStageUp());
            SelectPage(0);
        }

        protected override void OnShow(object args)
        {
            _shown = true;
            if (!_listening)
            {
                EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
                EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
                _listening = true;
            }
            Refresh();
            _ = EnsureConfigsAndRefreshAsync();
        }

        protected override void OnHide()
        {
            _shown = false;
            StopListening();
            ClearTaskItems();
        }

        protected override void OnDispose()
        {
            _shown = false;
            StopListening();
            ClearTaskItems();
        }

        private void OnBabyUpdate(int command)
        {
            Refresh();
        }

        private void OnBagUpdate()
        {
            Refresh();
        }

        private void Refresh()
        {
            BabyModel model = BabyModel.Instance;
            babyName.text = model.Basic != null ? model.Basic.BabyName ?? string.Empty : string.Empty;
            lvLb.text = model.Raise != null ? model.Raise.RaiseLevel.ToString() : string.Empty;
            lvExpLb.text = model.Raise != null ? model.Raise.RaiseExp.ToString() : string.Empty;
            stageExpLb.text = model.Stage != null ? model.Stage.StageExp.ToString() : string.Empty;
            if (lvtaskRed != null) lvtaskRed.gameObject.SetActive(model.HasClaimableRaiseTask());
            bool stageUpgradeRed = model.HasStageUpgradeRed();
            if (stageRed != null) stageRed.gameObject.SetActive(stageUpgradeRed);
            if (stageTabRed != null) stageTabRed.gameObject.SetActive(stageUpgradeRed);
            if (_shown && BabyRaiseConfigs.IsLoaded) RefreshTasks(model.Raise);
        }

        private async Task EnsureConfigsAndRefreshAsync()
        {
            await BabyRaiseConfigs.EnsureLoaded();
            await BabyValueConfigs.EnsureLoaded();
            await BabyStageConfigs.EnsureLoaded();
            if (_shown) Refresh();
        }

        private void RefreshTasks(BabyRaiseInfo raise)
        {
            ClearTaskItems();
            if (raise == null || _tpl_BabyCulTaskItem == null) return;

            var tasks = new List<BabyTaskInfo>(raise.TaskList);
            tasks.Sort((a, b) =>
            {
                int state = StateOrder(a.FinishState).CompareTo(StateOrder(b.FinishState));
                return state != 0 ? state : a.TaskId.CompareTo(b.TaskId);
            });
            Transform parent = taskGp != null && taskGp.content != null ? taskGp.content : taskGp != null ? taskGp.transform : transform;
            for (int i = 0; i < tasks.Count; i++)
            {
                BabyRaiseConfigs.BabyRaiseTaskCfg cfg = BabyRaiseConfigs.Get(tasks[i].TaskId);
                if (cfg == null) continue;
                GameObject itemObject = Instantiate(_tpl_BabyCulTaskItem, parent);
                itemObject.SetActive(true);
                BabyCulTaskItem item = itemObject.GetComponent<BabyCulTaskItem>();
                if (item == null)
                {
                    DestroyTaskItem(itemObject);
                    continue;
                }
                item.SetData(tasks[i], cfg);
                _taskItems.Add(itemObject);
            }
        }

        private static int StateOrder(int state) => state == 1 ? 0 : state == 0 ? 1 : state == 2 ? 2 : 3;

        private void ClearTaskItems()
        {
            for (int i = 0; i < _taskItems.Count; i++) DestroyTaskItem(_taskItems[i]);
            _taskItems.Clear();
        }

        private static void DestroyTaskItem(GameObject item)
        {
            if (item == null) return;
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }

        private void SelectPage(int index)
        {
            if (lvGp != null) lvGp.gameObject.SetActive(index == 0);
            if (stageGp != null) stageGp.gameObject.SetActive(index == 1);
        }

        private void StopListening()
        {
            if (!_listening) return;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnBagUpdate);
            _listening = false;
        }
    }
}
