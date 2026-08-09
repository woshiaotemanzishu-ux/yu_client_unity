using System;
using System.Collections.Generic;
using System.Linq;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// GuildMain 开服第5天后的“其他”页消费层。复用 GuildModule.prefab 内既有 GuildJoinView/Item，
    /// 不重建视觉树；对标老端 GuildJoinView 的 40001 列表、排序、空态、申请态和红点。
    /// </summary>
    internal sealed class GuildJoinRuntime : IDisposable
    {
        private sealed class Row
        {
            public GameObject Root;
            public GuildJoinItemBind Bind;
        }

        private readonly GuildJoinViewBind _view;
        private readonly List<Row> _rows = new List<Row>();
        private bool _prepared;

        public GuildJoinRuntime(GuildJoinViewBind view)
        {
            _view = view;
        }

        public void Prepare()
        {
            if (_view == null) return;
            if (!_prepared)
            {
                _prepared = true;
                if (_view._tpl_GuildJoinItem != null) _view._tpl_GuildJoinItem.SetActive(false);
                BindButton(_view._btn_build, OnBuild);
                BindButton(_view._btn_tips, OnBuild);
                BindButton(_view._btn_apply, OnApplyAll);
                EventDispatcher.On(GlobalEvent.EVT_GUILD_UPDATE, Rebuild);
            }

            GuildJoinController.Instance.RequestList();
            Rebuild();
        }

        public void Dispose()
        {
            if (_prepared)
                EventDispatcher.Off(GlobalEvent.EVT_GUILD_UPDATE, Rebuild);
            _prepared = false;
            _rows.Clear();
        }

        private void Rebuild()
        {
            if (_view == null) return;
            List<GuildJoinModel.GuildBrief> list = GuildJoinModel.Instance.List
                .OrderByDescending(g => g.CombatPower)
                .ThenByDescending(g => g.Lv)
                .ToList();

            EnsureRows(list.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i < list.Count;
                if (!active)
                {
                    _rows[i].Bind.Hide();
                    continue;
                }
                _rows[i].Bind.Show();
                SetRow(_rows[i].Bind, list[i]);
            }

            if (_view._group_empty != null) _view._group_empty.gameObject.SetActive(list.Count == 0);
            bool hasGuild = GuildModel.IsHasGuild();
            bool canJoin = list.Any(g => g.MemberNum < g.MemberCapacity);
            if (_view._reddot != null) _view._reddot.gameObject.SetActive(!hasGuild && canJoin);
            if (_view.create_reddot != null) _view.create_reddot.gameObject.SetActive(!hasGuild && !canJoin);
            if (_view.labelDisplay11 != null) _view.labelDisplay11.text = "创建仙宗";
            if (_view.labelDisplay != null) _view.labelDisplay.text = "创建仙宗";
            if (_view._Label1 != null)
                _view._Label1.text = "震惊！当前还没有仙宗呢！是否前往创建本服第一个仙宗？";
        }

        private void EnsureRows(int count)
        {
            if (_view?._tpl_GuildJoinItem == null || _view._list_items == null || _view._list_items.content == null)
                return;
            while (_rows.Count < count)
            {
                GameObject go = UnityEngine.Object.Instantiate(_view._tpl_GuildJoinItem, _view._list_items.content);
                GuildJoinItemBind bind = go.GetComponent<GuildJoinItemBind>();
                if (bind == null)
                {
                    UnityEngine.Object.Destroy(go);
                    break;
                }
                go.SetActive(true);
                bind.Show();
                _rows.Add(new Row { Root = go, Bind = bind });
            }
        }

        private static void SetRow(GuildJoinItemBind row, GuildJoinModel.GuildBrief data)
        {
            if (row == null || data == null) return;
            if (row._Label3 != null) row._Label3.text = "仙宗评分：";
            if (row._lb_name != null) row._lb_name.text = data.Name + "  " + data.Lv + "级";
            if (row._lb_master != null) row._lb_master.text = data.ChiefName;
            if (row._lb_num != null) row._lb_num.text = data.MemberNum + "/" + data.MemberCapacity;
            if (row._lb_fight != null) row._lb_fight.text = data.CombatPower.ToString();
            if (row._lb_cond != null)
            {
                if (data.IsApply)
                {
                    row._lb_cond.text = "等待审批";
                    row._lb_cond.fontStyle = TMPro.FontStyles.Normal;
                    row._lb_cond.color = new Color(0.42f, 0.85f, 0.46f);
                }
                else
                {
                    row._lb_cond.text = data.AutoApprovePower == 0
                        ? "无条件限制" : "战力" + data.AutoApprovePower + "以上";
                    row._lb_cond.fontStyle = TMPro.FontStyles.Bold;
                    row._lb_cond.color = Shenxiao.Module.Core.Role.RoleModel.Instance.CombatPower >= data.AutoApprovePower
                        ? Color.white : new Color(0.42f, 0.85f, 0.46f);
                }
            }
            BindButton(row._btn_join, () =>
            {
                if (GuildModel.IsHasGuild())
                {
                    TipsManager.Toast("您当前已有所属结社");
                    return;
                }
                GuildJoinController.Instance.ApplyOne(data.GuildId);
            });
        }

        private static void OnBuild()
        {
            if (GuildModel.IsHasGuild())
            {
                TipsManager.Toast("您当前已有所属结社");
                return;
            }
            // 老端此处打开 GuildBuildView 输入名字；该弹层未移植，禁止用固定默认名直接发 40004。
            TipsManager.Toast("创建仙宗弹窗尚未接入");
        }

        private static void OnApplyAll()
        {
            if (GuildModel.IsHasGuild())
            {
                TipsManager.Toast("您当前已有所属结社");
                return;
            }
            GuildJoinController.Instance.ApplyAll();
        }

        private static void BindButton(Component target, Action action)
        {
            if (target == null) return;
            Image image = target as Image;
            if (image == null) image = target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.ClearClicks(image);
            UIUtil.AddClick(image, action);
        }
    }
}
