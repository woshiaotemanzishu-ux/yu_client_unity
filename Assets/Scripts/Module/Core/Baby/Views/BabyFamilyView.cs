using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝家庭页：只展示服务端返回的有效家庭记录。</summary>
    public sealed class BabyFamilyView : BabyFamilyViewBind
    {
        // ClientBaby.defaultAttr（旧端）当前的基础属性位；服务端遗漏时仍按旧端显示为 0。
        private static readonly int[] DefaultAttrIds = { 1, 2, 3, 4, 5, 6, 7, 8 };
        private bool _listening;
        private int _displayVersion;
        private FightingShowSmallItem _fightItem1;
        private FightingShowSmallItem _fightItem2;

        protected override void OnInit()
        {
            UIUtil.AddClick(reName1, OpenRename);
            UIUtil.AddClick(reName2, OpenRename);
        }

        protected override void OnShow(object args)
        {
            int version = ++_displayVersion;
            Subscribe();
            BabyController.Instance.RequestFamily();
            Refresh();
            _ = RefreshWhenConfigsReady(version);
        }

        protected override void OnHide()
        {
            _displayVersion++;
            Unsubscribe();
        }

        protected override void OnDispose()
        {
            _displayVersion++;
            Unsubscribe();
        }

        private async Task RefreshWhenConfigsReady(int version)
        {
            await GoodsModel.EnsureLoaded();
            if (version == _displayVersion && IsShown) Refresh();
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
            var entries = new BabyFamilyEntry[2];
            BabyFamilyInfo family = BabyModel.Instance.Family;
            if (family != null)
            {
                foreach (BabyFamilyEntry entry in family.InfoList)
                {
                    if (entry == null || entry.ActiveTime <= 0) continue;
                    // 18207 已在 Controller 中按旧端反转；这里再按旧端的本人/性别规则落到左右槽。
                    int slot = (entry.RoleId == RoleModel.Instance.RoleId) == (RoleModel.Instance.Sex == 1) ? 0 : 1;
                    entries[slot] = entry;
                }
            }

            SetSlot(0, entries[0]);
            SetSlot(1, entries[1]);
            if (tipsGp != null) tipsGp.gameObject.SetActive(entries[0] == null || entries[1] == null);
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
            if (name != null) name.text = "名称:\n血型:\n生日:\n星座:";
            if (value != null) value.text = valid
                ? BuildBasicInfo(entry)
                : string.Empty;
            if (rename != null) rename.gameObject.SetActive(valid && entry.RoleId == RoleModel.Instance.RoleId);
            SetFighting(index, fight, valid ? entry.BabyPower : 0, valid);
            SetAttributes(entry, sp, nom, my);
        }

        private static string BuildBasicInfo(BabyFamilyEntry entry)
        {
            DateTime birth = DateTimeOffset.FromUnixTimeSeconds(entry.ActiveTime).LocalDateTime;
            return string.Format("{0}\n{1}型血\n{2}月{3}日\n{4}座", string.IsNullOrEmpty(entry.BabyName) ? "宝宝" : entry.BabyName,
                GetBlood(entry.ActiveTime), birth.Month, birth.Day, GetConstellation(birth.Month, birth.Day));
        }

        private void SetFighting(int index, RectTransform root, int power, bool visible)
        {
            if (root == null) return;
            FightingShowSmallItem item = index == 0 ? _fightItem1 : _fightItem2;
            if (item == null && _tpl_FightingShowSmallItem != null)
            {
                GameObject go = Instantiate(_tpl_FightingShowSmallItem, root, false);
                item = go.GetComponent<FightingShowSmallItem>();
                if (index == 0) _fightItem1 = item; else _fightItem2 = item;
            }
            root.gameObject.SetActive(visible);
            if (item == null) return;
            item.gameObject.SetActive(visible);
            if (visible) item.SetFighting(power);
        }

        private static void SetAttributes(BabyFamilyEntry entry, TMPro.TextMeshProUGUI special, TMPro.TextMeshProUGUI normal, TMPro.TextMeshProUGUI other)
        {
            if (special != null) special.text = string.Empty;
            if (normal != null) normal.text = string.Empty;
            if (other != null) other.text = string.Empty;
            if (entry == null) return;
            foreach (BabyAttrGroup group in entry.AttrInfo)
            {
                if (group == null) continue;
                var attrs = new SortedDictionary<int, int>();
                foreach (BabyAttrEntry attr in group.AttrList) if (attr != null) attrs[attr.AttrId] = attr.Value;
                if (group.Type == 1)
                {
                    foreach (int attrId in DefaultAttrIds) if (!attrs.ContainsKey(attrId)) attrs[attrId] = 0;
                    var specialLines = new List<string>(); var normalLines = new List<string>();
                    foreach (KeyValuePair<int, int> pair in attrs)
                    {
                        string name = GoodsModel.GetAttrName(pair.Key);
                        string value = GoodsModel.FormatAttrValue(pair.Key, pair.Value);
                        if (GoodsModel.GetAttrKind(pair.Key) == 2) specialLines.Add(name + ": " + value);
                        else normalLines.Add(name + ": <color=#e48100>" + value + "</color>");
                    }
                    if (special != null) special.text = string.Join("\n", specialLines);
                    if (normal != null) normal.text = string.Join("\n", normalLines);
                }
                else if (other != null && attrs.Count > 0)
                {
                    var lines = new List<string> { "给予TA的加成" };
                    foreach (KeyValuePair<int, int> pair in attrs)
                        lines.Add(GoodsModel.GetAttrName(pair.Key) + ": <color=#e48100>" + GoodsModel.FormatAttrValue(pair.Key, pair.Value) + "</color>");
                    other.text = string.Join("\n", lines);
                }
            }
        }

        private static string GetBlood(int time) { switch (time % 4) { case 0: return "A"; case 1: return "B"; case 2: return "O"; default: return "AB"; } }
        private static string GetConstellation(int month, int day)
        {
            int date = month * 100 + day;
            if (date >= 121 && date <= 219) return "水瓶"; if (date <= 320) return "双鱼"; if (date <= 420) return "白羊";
            if (date <= 521) return "金牛"; if (date <= 621) return "双子"; if (date <= 722) return "巨蟹";
            if (date <= 823) return "狮子"; if (date <= 923) return "处女"; if (date <= 1023) return "天秤";
            if (date <= 1122) return "天蝎"; if (date <= 1221) return "射手"; return "摩羯";
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
