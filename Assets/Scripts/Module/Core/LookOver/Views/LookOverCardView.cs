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
    /// 他人资料卡面板(module 1 基本装备 + module 2-12 通用结构化详情)。不建 Bind 类,直接继承 BaseView + public 字段(Login 家族范式,
    /// 对标 <see cref="Shenxiao.Module.Core.Login.Views.LoginPanelView"/>);由
    /// <c>Assets/Editor/UiCreator/LookOver/LookOverCardCreator.cs</c> 纯代码建树生成 prefab。
    ///
    /// 展示:头像信息(姓名)+ 服务器/角色ID + 战力 + 成就阶 + 装备/法阵/仙灵列表(纯文本行,
    /// 不烤图标——config_dsgt/ConfigPlayerMessageShow/config_god_equip_kv 等配表缺失时按裁决§2 PL.5
    /// 降级为显示服务端 ID/等级/评分,不阻塞面板可用性)。运行时创建模块切换按钮，不要求重烤 prefab。
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
        private int _pendingServerId;
        private int _pendingModuleId = 1;
        private bool _subscribed;
        private readonly List<GameObject> _rows = new List<GameObject>();
        private Button _moduleButton;
        private TextMeshProUGUI _moduleLabel;
        private Button _compareButton;
        private string _baseTitle;

        private static readonly string[] ModuleNames =
        {
            "", "基础", "龙珠", "影装", "神祭", "幻化", "天启",
            "降神", "灵饰", "神纹", "蜃妖", "神巫妖灵", "御魂"
        };

        protected override void OnInit()
        {
            if (rowTemplate != null) rowTemplate.SetActive(false);
            UIUtil.AddClick(spClose, Hide);
            _baseTitle = lblTitle != null ? lblTitle.text : string.Empty;
            BuildModuleButton();
            BuildCompareButton();
        }

        protected override void OnShow(object args)
        {
            if (args is LookOverFlow.Target target)
            {
                _pendingRoleId = target.RoleId;
                _pendingServerId = target.ServerId;
            }
            else
            {
                _pendingRoleId = args is long l ? l : 0; // 兼容旧 Case/预览调用
                _pendingServerId = 0;
            }
            Subscribe();
            SelectModule(1);
        }

        protected override void OnHide()
        {
            Unsubscribe();
            ClearTarget();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            ClearTarget();
        }

        /// <summary>切换资料模块；供运行时按钮和 CliVerify 定向调用。</summary>
        public void SelectModule(int moduleId)
        {
            if (moduleId < 1 || moduleId > 12 || _pendingRoleId == 0) return;
            _pendingModuleId = moduleId;
            RefreshModuleLabel();

            if (moduleId == 1)
            {
                FriendModel.PlayerCard card = FriendModel.Instance.LastPlayerCard;
                if (card != null && card.RoleId == _pendingRoleId) { Render(card); return; }
            }
            else
            {
                LookOverModuleSnapshot snapshot =
                    FriendModel.Instance.GetLookOverModule(_pendingRoleId, moduleId);
                if (snapshot != null) { Render(snapshot); return; }
            }

            ShowLoading();
            FriendController.Instance.RequestPlayerCard(_pendingRoleId, moduleId, _pendingServerId);
        }

        /// <summary>切到15014原始战力分项对比；不依赖缺失的旧端CLIENTFIGHTCOM配置做名称归类或总和推导。</summary>
        public void SelectFightCompare()
        {
            if (_pendingRoleId <= 0) return;
            _pendingModuleId = 0;
            RefreshModuleLabel();
            ushort serverId = unchecked((ushort)Mathf.Clamp(_pendingServerId, 0, ushort.MaxValue));
            ulong roleId = unchecked((ulong)_pendingRoleId);
            if (FriendModel.Instance.TryGetFightCompare(serverId, roleId,
                    out FriendModel.FightCompareSnapshot snapshot))
            {
                Render(snapshot);
                return;
            }
            ShowLoading();
            FriendController.Instance.RequestFightCompare(_pendingRoleId, _pendingServerId);
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On<FriendModel.PlayerCard>(GlobalEvent.EVT_PLAYER_CARD, OnPlayerCard);
            EventDispatcher.On<LookOverModuleSnapshot>(GlobalEvent.EVT_LOOKOVER_MODULE, OnLookOverModule);
            EventDispatcher.On<FriendModel.FightCompareSnapshot>(
                GlobalEvent.EVT_LOOKOVER_FIGHT_COMPARE, OnFightCompare);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off<FriendModel.PlayerCard>(GlobalEvent.EVT_PLAYER_CARD, OnPlayerCard);
            EventDispatcher.Off<LookOverModuleSnapshot>(GlobalEvent.EVT_LOOKOVER_MODULE, OnLookOverModule);
            EventDispatcher.Off<FriendModel.FightCompareSnapshot>(
                GlobalEvent.EVT_LOOKOVER_FIGHT_COMPARE, OnFightCompare);
        }

        private void OnPlayerCard(FriendModel.PlayerCard card)
        {
            if (card == null || _pendingModuleId != 1 || card.RoleId != _pendingRoleId || !IsShown) return;
            Render(card);
        }

        private void OnLookOverModule(LookOverModuleSnapshot snapshot)
        {
            if (snapshot == null || snapshot.RoleId != _pendingRoleId
                || snapshot.ModuleId != _pendingModuleId || !IsShown) return;
            Render(snapshot);
        }

        private void OnFightCompare(FriendModel.FightCompareSnapshot snapshot)
        {
            if (snapshot == null || _pendingModuleId != 0 || !IsShown
                || snapshot.RoleId != unchecked((ulong)_pendingRoleId)
                || snapshot.ServerId != unchecked((ushort)Mathf.Clamp(_pendingServerId, 0, ushort.MaxValue))) return;
            Render(snapshot);
        }

        private void ShowLoading()
        {
            if (lblLoading != null) lblLoading.gameObject.SetActive(true);
            // module1 保持旧 Case 的 loading/infoGroup 行为；扩展模块保留选择按钮可继续切页。
            if (infoGroup != null) infoGroup.SetActive(_pendingModuleId != 1);
            if (_pendingModuleId != 1)
            {
                string title = _pendingModuleId == 0 ? "战力对比" : ModuleNames[_pendingModuleId];
                if (lblTitle != null) lblTitle.text = title;
                if (lblName != null) lblName.text = "加载中";
                if (lblServer != null) lblServer.text = "服务器 " + _pendingServerId;
                if (lblRoleId != null) lblRoleId.text = "ID " + _pendingRoleId;
                if (lblCombat != null) lblCombat.text = "战力 --";
                if (lblAchv != null) lblAchv.text = _pendingModuleId == 0 ? "分项对比" : "模块 " + _pendingModuleId;
            }
            ClearRows();
        }

        private void Render(FriendModel.PlayerCard card)
        {
            if (lblLoading != null) lblLoading.gameObject.SetActive(false);
            if (infoGroup != null) infoGroup.SetActive(true);
            if (lblTitle != null) lblTitle.text = _baseTitle;

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

        private void Render(LookOverModuleSnapshot snapshot)
        {
            if (lblLoading != null) lblLoading.gameObject.SetActive(false);
            if (infoGroup != null) infoGroup.SetActive(true);
            if (lblTitle != null) lblTitle.text = string.IsNullOrEmpty(snapshot.Title)
                ? ModuleNames[snapshot.ModuleId] : snapshot.Title;
            if (lblName != null) lblName.text = string.IsNullOrEmpty(snapshot.Title)
                ? ModuleNames[snapshot.ModuleId] : snapshot.Title;
            if (lblServer != null) lblServer.text = "服务器 " + snapshot.ServerId;
            if (lblRoleId != null) lblRoleId.text = "ID " + snapshot.RoleId;
            if (lblCombat != null) lblCombat.text = "战力 " + snapshot.PrimaryPower;
            if (lblAchv != null) lblAchv.text = "模块 " + snapshot.ModuleId;
            ClearRows();
            if (listDetail == null || listDetail.content == null || rowTemplate == null) return;
            var rows = snapshot.BuildRows();
            if (rows == null) return;
            foreach (string row in rows) AddRow(listDetail.content, string.IsNullOrEmpty(row) ? "ID 0" : row);
        }

        private void Render(FriendModel.FightCompareSnapshot snapshot)
        {
            if (lblLoading != null) lblLoading.gameObject.SetActive(false);
            if (infoGroup != null) infoGroup.SetActive(true);
            if (lblTitle != null) lblTitle.text = "战力对比";
            if (lblName != null) lblName.text = "我方 / 对方";
            if (lblServer != null) lblServer.text = "服务器 " + snapshot.ServerId;
            if (lblRoleId != null) lblRoleId.text = "ID " + snapshot.RoleId;
            if (lblCombat != null) lblCombat.text = "战力分项 " + Mathf.Max(snapshot.SelfPower.Count, snapshot.OtherPower.Count);
            if (lblAchv != null) lblAchv.text = "原始对比";

            ClearRows();
            if (listDetail == null || listDetail.content == null || rowTemplate == null) return;
            int count = Mathf.Max(snapshot.SelfPower.Count, snapshot.OtherPower.Count);
            for (int i = 0; i < count; i++)
            {
                string self = i < snapshot.SelfPower.Count ? snapshot.SelfPower[i].ToString() : "--";
                string other = i < snapshot.OtherPower.Count ? snapshot.OtherPower[i].ToString() : "--";
                AddRow(listDetail.content, string.Format("分项{0}  我方 {1} / 对方 {2}", i + 1, self, other));
            }
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
            // Destroy 在帧尾才真正移除；先隐藏可避免快速切模块时旧行与新行同帧叠显。
            foreach (GameObject r in _rows)
            {
                if (r == null) continue;
                r.SetActive(false);
                Destroy(r);
            }
            _rows.Clear();
        }

        private void BuildModuleButton()
        {
            if (infoGroup == null || _moduleButton != null) return;
            var go = new GameObject("BtnLookOverModule", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(infoGroup.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -12f);
            rt.sizeDelta = new Vector2(230f, 48f);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.18f, 0.32f, 0.52f, 0.96f);
            _moduleButton = go.GetComponent<Button>();
            _moduleButton.targetGraphic = image;
            _moduleButton.onClick.AddListener(() => SelectModule(_pendingModuleId % 12 + 1));

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            _moduleLabel = labelGo.GetComponent<TextMeshProUGUI>();
            _moduleLabel.alignment = TextAlignmentOptions.Center;
            _moduleLabel.fontSize = 22f;
            _moduleLabel.color = Color.white;
            _moduleLabel.raycastTarget = false;
            if (lblTitle != null)
            {
                _moduleLabel.font = lblTitle.font;
                _moduleLabel.fontSharedMaterial = lblTitle.fontSharedMaterial;
            }
            RefreshModuleLabel();
        }

        private void BuildCompareButton()
        {
            if (infoGroup == null || _compareButton != null) return;
            var go = new GameObject("BtnFightCompare", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(infoGroup.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-256f, -12f);
            rt.sizeDelta = new Vector2(150f, 48f);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.35f, 0.24f, 0.12f, 0.96f);
            _compareButton = go.GetComponent<Button>();
            _compareButton.targetGraphic = image;
            _compareButton.onClick.AddListener(SelectFightCompare);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = "战力对比";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22f;
            label.color = Color.white;
            label.raycastTarget = false;
            if (lblTitle != null)
            {
                label.font = lblTitle.font;
                label.fontSharedMaterial = lblTitle.fontSharedMaterial;
            }
        }

        private void RefreshModuleLabel()
        {
            if (_moduleLabel != null)
                _moduleLabel.text = _pendingModuleId == 0 ? "模块：基础 ▸" : "模块：" + ModuleNames[_pendingModuleId] + " ▸";
        }

        private void ClearTarget()
        {
            _pendingRoleId = 0;
            _pendingServerId = 0;
            _pendingModuleId = 1;
            RefreshModuleLabel();
            ClearRows();
        }
    }
}
