using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝家庭页：只展示服务端返回的有效家庭记录。</summary>
    public sealed class BabyFamilyView : BabyFamilyViewBind
    {
        private bool _listening;

        protected override void OnInit()
        {
            UIUtil.AddClick(reName1, OpenRename);
            UIUtil.AddClick(reName2, OpenRename);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            BabyController.Instance.RequestFamily();
            Refresh();
        }

        protected override void OnHide()
        {
            Unsubscribe();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_listening) return;
            _listening = true;
            EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
        }

        private void Unsubscribe()
        {
            if (!_listening) return;
            _listening = false;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
        }

        private void OnBabyUpdate(int command)
        {
            if (command == Proto.BABY_FAMILY_INFO && gameObject.activeInHierarchy) Refresh();
        }

        private void Refresh()
        {
            var entries = new List<BabyFamilyEntry>();
            BabyFamilyInfo family = BabyModel.Instance.Family;
            if (family != null)
            {
                foreach (BabyFamilyEntry entry in family.InfoList)
                {
                    if (entry == null || entry.ActiveTime <= 0) continue;
                    entries.Add(entry);
                    if (entries.Count == 2) break;
                }
            }

            SetSlot(0, entries.Count > 0 ? entries[0] : null);
            SetSlot(1, entries.Count > 1 ? entries[1] : null);
            if (tipsGp != null) tipsGp.gameObject.SetActive(entries.Count < 2);
            HideUnsupported();
        }

        private void SetSlot(int index, BabyFamilyEntry entry)
        {
            bool valid = entry != null;
            var scroller = index == 0 ? scroller1 : scroller2;
            if (scroller != null) scroller.gameObject.SetActive(valid);
            var name = index == 0 ? nameLb1 : nameLb2;
            var value = index == 0 ? value1 : value2;
            var rename = index == 0 ? reName1 : reName2;
            var fight = index == 0 ? fight1 : fight2;
            var sp = index == 0 ? spLb1 : spLb2;
            var nom = index == 0 ? nomLb1 : nomLb2;
            var my = index == 0 ? myLb1 : myLb2;
            if (name != null) name.text = "名称\n等级\n阶数\n战力";
            if (value != null) value.text = valid
                ? string.Format("{0}\n{1}\n{2}-{3}\n{4}", string.IsNullOrEmpty(entry.BabyName) ? "宝宝" : entry.BabyName,
                    entry.RaiseLevel, entry.Stage, entry.StageLevel, entry.BabyPower)
                : string.Empty;
            if (rename != null) rename.gameObject.SetActive(valid && entry.RoleId == RoleModel.Instance.RoleId);
            if (fight != null) fight.gameObject.SetActive(false);
            if (sp != null) sp.text = string.Empty;
            if (nom != null) nom.text = string.Empty;
            if (my != null) my.text = string.Empty;
        }

        private void OpenRename() => _ = ViewManager.Open<BabyRenameView>();

        private void HideUnsupported()
        {
            SetActive(fatherGp, false); SetActive(motherGp, false); SetActive(child1Gp, false); SetActive(child2Gp, false);
            SetActive(gp1, false); SetActive(gp2, false); SetActive(title1, false); SetActive(title2, false);
            if (faName != null) faName.gameObject.SetActive(false);
            if (moName != null) moName.gameObject.SetActive(false);
        }

        private static void SetActive(UnityEngine.Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }
    }
}
