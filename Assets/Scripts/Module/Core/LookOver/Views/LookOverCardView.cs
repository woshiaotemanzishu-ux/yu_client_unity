using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Friend;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.LookOver.Views
{
    /// <summary>
    /// 他人资料卡面板(module 1 基本装备,对标老客户端 playerMessage/PlayerMessageView.ts 的
    /// "玩家信息" 一级页签精简版)。不建 Bind 类,直接继承 BaseView + public 字段(Login 家族范式,
    /// 对标 <see cref="Shenxiao.Module.Core.Login.Views.LoginPanelView"/>);由
    /// <c>Assets/Editor/UiCreator/LookOver/LookOverCardCreator.cs</c> 纯代码建树生成 prefab。
    ///
    /// 展示:头像信息(姓名)+ 服务器/角色ID + 战力 + 成就阶 + 装备/法阵/仙灵列表(纯文本行,
    /// 不烤图标——config_dsgt/ConfigPlayerMessageShow/config_god_equip_kv 等配表缺失时按裁决§2 PL.5
    /// 降级为不显示对应字段,不阻塞面板可用性)。其余 11 个 module(龙珠/影装/……)留轮22,
    /// 本面板暂无二级页签。
    ///
    /// 数据源:<see cref="FriendModel.PlayerCard"/>(19502 落地),事件 <c>GlobalEvent.EVT_PLAYER_CARD</c>
    /// (轮7 已发出但零订阅者,本类是首个消费方)。打开面板即进入"加载中"态,收到匹配 role_id 的卡片
    /// 才渲染——19501 成功路径服务端不回包,真正数据全靠这条推送(见 LookOverFlow 类注释陷阱③)。
    /// </summary>
    public sealed class LookOverCardView : BaseView
    {
        [Header("标题/关闭")]
        public TextMeshProUGUI lblTitle;
        public Image spClose;

        [Header("加载态")]
        public TextMeshProUGUI lblLoading;

        [Header("基础信息")]
        public GameObject infoGroup;
        public TextMeshProUGUI lblName;
        public TextMeshProUGUI lblServer;
        public TextMeshProUGUI lblRoleId;
        public TextMeshProUGUI lblCombat;
        public TextMeshProUGUI lblAchv;

        [Header("详情列表(装备/法阵/仙灵,纯文本行)")]
        public ScrollRect listDetail;
        public GameObject rowTemplate;

        private long _pendingRoleId;
        private bool _subscribed;
        private readonly List<GameObject> _rows = new List<GameObject>();

        protected override void OnInit()
        {
            if (rowTemplate != null) rowTemplate.SetActive(false);
            UIUtil.AddClick(spClose, Hide);
        }

        protected override void OnShow(object args)
        {
            _pendingRoleId = args is long l ? l : 0;
            Subscribe();
            ShowLoading();
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
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On<FriendModel.PlayerCard>(GlobalEvent.EVT_PLAYER_CARD, OnPlayerCard);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off<FriendModel.PlayerCard>(GlobalEvent.EVT_PLAYER_CARD, OnPlayerCard);
        }

        private void OnPlayerCard(FriendModel.PlayerCard card)
        {
            if (card == null || card.RoleId != _pendingRoleId || !IsShown) return;
            Render(card);
        }

        private void ShowLoading()
        {
            if (lblLoading != null) lblLoading.gameObject.SetActive(true);
            if (infoGroup != null) infoGroup.SetActive(false);
            ClearRows();
        }

        private void Render(FriendModel.PlayerCard card)
        {
            if (lblLoading != null) lblLoading.gameObject.SetActive(false);
            if (infoGroup != null) infoGroup.SetActive(true);

            string name = card.Figure != null && !string.IsNullOrEmpty(card.Figure.name)
                ? card.Figure.name
                : ("角色" + card.RoleId);
            if (lblName != null) lblName.text = name;
            if (lblServer != null) lblServer.text = "服务器 " + card.ServerId;
            if (lblRoleId != null) lblRoleId.text = "ID " + card.RoleId;
            if (lblCombat != null) lblCombat.text = "战力 " + card.Combat;
            if (lblAchv != null) lblAchv.text = "成就阶 " + card.AchvStage;

            BuildRows(card);
        }

        private void BuildRows(FriendModel.PlayerCard card)
        {
            ClearRows();
            if (listDetail == null || listDetail.content == null || rowTemplate == null) return;
            Transform content = listDetail.content;

            AddRow(content, "装备 " + card.EquipList.Count + " 件");
            foreach (FriendModel.EquipCardItem e in card.EquipList)
            {
                AddRow(content, string.Format("位{0} 品质{1} 强化+{2} {3}星 {4}阶 Lv{5} 神级{6}",
                    e.Cell, e.Color, e.Stren, e.Star, e.Stage, e.Level, e.GodLevel));
            }
            foreach (FriendModel.MagicCircleItem m in card.MagicCircle)
            {
                AddRow(content, "法阵 等级" + m.Lv + (m.IsOpen != 0 ? " 已开启" : " 未开启"));
            }
            foreach (FriendModel.FairyItem f in card.FairyList)
            {
                AddRow(content, "仙灵" + f.Type + (f.IsActive != 0 ? " 已激活" : " 未激活"));
            }
        }

        private void AddRow(Transform content, string text)
        {
            GameObject go = Instantiate(rowTemplate, content);
            go.SetActive(true);
            TextMeshProUGUI lbl = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (lbl != null) lbl.text = text;
            _rows.Add(go);
        }

        private void ClearRows()
        {
            foreach (GameObject r in _rows) if (r != null) Destroy(r);
            _rows.Clear();
        }
    }
}
