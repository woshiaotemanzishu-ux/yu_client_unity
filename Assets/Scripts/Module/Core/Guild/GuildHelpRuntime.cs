using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Guild;
using Shenxiao.Module.Core.BrightSea;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// GuildHelpView/GuildHelpTipsView 的 data-only 消费层。视觉节点全部来自 GuildModule.prefab；
    /// 本类只负责 40405 列表、40031/18916 计数、开放条件、40402 确认和 40403 自身取消。
    /// 无尽之海“立即夺回”和已协助目标寻路属于 BrightSea/Boss/Scene 跨岛，仅保留明确提示。
    /// </summary>
    internal sealed class GuildHelpRuntime : IDisposable
    {
        private sealed class Row
        {
            public GuildHelpItemBind Bind;
        }

        private readonly GuildHelpViewBind _view;
        private readonly GuildHelpTipsViewBind _tips;
        private readonly List<Row> _rows = new List<Row>();
        private readonly List<EquipmentItem> _rewardRows = new List<EquipmentItem>();
        private bool _listening;
        private GuildModel.AssistEntry _pending;

        public GuildHelpRuntime(GuildHelpViewBind view, GuildHelpTipsViewBind tips)
        {
            _view = view;
            _tips = tips;
            if (_view?._tpl_GuildHelpItem != null) _view._tpl_GuildHelpItem.SetActive(false);
            if (_tips?._tpl_EquipmentItem != null) _tips._tpl_EquipmentItem.SetActive(false);
            if (_tips?._tpl_BaseAwardItem != null) _tips._tpl_BaseAwardItem.SetActive(false);
            BindButton(_view?._btn_close, Close);
            BindButton(_view?._btn_help, () => InstructionFlow.Show(1001));
            BindButton(_tips?._btn_close, CloseTips);
            BindButton(_tips?._btn_cancel, CloseTips);
            BindButton(_tips?._btn_go, ConfirmAssist);
        }

        public void Show()
        {
            if (_view == null) return;
            if (!_listening)
            {
                EventDispatcher.On(GlobalEvent.EVT_GUILD_ASSIST_UPDATE, Refresh);
                EventDispatcher.On(GlobalEvent.EVT_GUILD_DATA_UPDATE, Refresh);
                _listening = true;
            }
            _tips?.Hide();
            _view.Show();
            _view.transform.SetAsLastSibling();
            GuildController.Instance.RequestAssistList();
            GuildController.Instance.RequestAssistCount();
            GuildController.Instance.RequestPrestigeDaily();
            BrightSeaController.Instance.RequestAssistBGoldInfo();
            Refresh();
        }

        public void Close()
        {
            CloseTips();
            _view?.Hide();
            if (_listening)
            {
                EventDispatcher.Off(GlobalEvent.EVT_GUILD_ASSIST_UPDATE, Refresh);
                EventDispatcher.Off(GlobalEvent.EVT_GUILD_DATA_UPDATE, Refresh);
                _listening = false;
            }
        }

        public void Dispose()
        {
            Close();
            _rows.Clear();
            _rewardRows.Clear();
        }

        private void Refresh()
        {
            RefreshCounts();
            if (_view == null) return;
            bool globalOpen = GuildModel.IsAssistGloballyOpen();
            List<GuildModel.AssistEntry> list = GuildModel.Instance.AssistList
                .Where(e => e != null && ((e.Type != 1 && e.Type != 2) || globalOpen))
                .ToList();
            EnsureRows(list.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                if (i >= list.Count)
                {
                    _rows[i].Bind.Hide();
                    continue;
                }
                _rows[i].Bind.Show();
                RenderRow(_rows[i].Bind, list[i]);
            }
        }

        private void RefreshCounts()
        {
            if (_view == null) return;
            if (_view._lb_count2 != null)
                _view._lb_count2.text = GuildModel.Instance.PrestigeDay + "/" + GuildModel.Instance.PrestigeDayLimit;
            if (_view._lb_count != null)
            {
                BrightSeaModel sea = BrightSeaModel.Instance;
                _view._lb_count.text = sea.AssistBGoldNum + "/" + sea.AssistBGoldMax;
            }
        }

        private void EnsureRows(int count)
        {
            if (_view?._tpl_GuildHelpItem == null || _view._list_litems == null || _view._list_litems.content == null)
                return;
            while (_rows.Count < count)
            {
                GameObject go = UnityEngine.Object.Instantiate(_view._tpl_GuildHelpItem, _view._list_litems.content);
                GuildHelpItemBind bind = go.GetComponent<GuildHelpItemBind>();
                if (bind == null)
                {
                    UnityEngine.Object.Destroy(go);
                    break;
                }
                go.SetActive(true);
                bind.Show();
                _rows.Add(new Row { Bind = bind });
            }
        }

        private void RenderRow(GuildHelpItemBind row, GuildModel.AssistEntry data)
        {
            if (row == null || data == null) return;
            bool self = data.RoleId > 0 && data.RoleId == Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId;
            bool open = GuildModel.IsAssistOpen(data.Type, data.SubType);
            JObject cfg = GuildConfigs.GetAssistCfg(data.Type, data.SubType);
            string module = cfg?["desc"]?.ToString() ?? GetTypeName(data.Type);

            if (row._lb_name != null) row._lb_name.text = data.Name;
            if (row._lb_module != null) row._lb_module.text = module;
            SetDescriptions(row, BuildDescriptions(data));
            if (row._gp_lock != null) row._gp_lock.gameObject.SetActive(!open);
            if (row._lb_cond != null)
                row._lb_cond.text = open ? "" : GuildModel.GetAssistOpenDescription(data.Type, data.SubType);
            if (row._btn_go != null) row._btn_go.gameObject.SetActive(open && !self);
            if (row._btn_cancel != null) row._btn_cancel.gameObject.SetActive(self);
            if (row._Label1 != null) row._Label1.text = data.IsAssist ? "前往" : "协助";
            if (row._Label2 != null) row._Label2.text = data.Type == 3 ? "立即夺回" : "取消请求";

            BindButton(row._btn_go, () =>
            {
                if (!GuildModel.IsAssistOpen(data.Type, data.SubType))
                {
                    TipsManager.Toast(GuildModel.GetAssistOpenDescription(data.Type, data.SubType));
                    return;
                }
                if (data.IsAssist)
                {
                    TipsManager.Toast("前往协助目标需 Boss/Scene 寻路链，尚未接入");
                    return;
                }
                OpenTips(data);
            });
            BindButton(row._btn_cancel, () =>
            {
                if (data.Type == 3)
                {
                    TipsManager.Toast("立即夺回需无尽之海战斗链，尚未接入");
                    return;
                }
                GuildController.Instance.CancelAssist(data.AssistId);
            });
        }

        private static string[] BuildDescriptions(GuildModel.AssistEntry data)
        {
            if (data.Type == 3)
            {
                GuildModel.AssistExtra extra = data.Extra != null && data.Extra.Count > 0 ? data.Extra[0] : null;
                if (extra == null) return new[] { "无尽之海", "掠夺信息待刷新" };
                return new[] { extra.RoberName, "战力：" + extra.RoberPower, "掠夺目标 " + data.TargetCfgId };
            }
            if (data.Type == 4) return new[] { "斩秽巡行第" + data.TargetCfgId + "关" };
            return new[] { "目标编号 " + data.TargetCfgId, "来自 " + data.Name };
        }

        private static void SetDescriptions(GuildHelpItemBind row, string[] lines)
        {
            TMPro.TextMeshProUGUI[] labels = { row._lb_desc1, row._lb_desc2, row._lb_desc3 };
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null) continue;
                bool show = lines != null && i < lines.Length && !string.IsNullOrEmpty(lines[i]);
                labels[i].gameObject.SetActive(show);
                labels[i].text = show ? lines[i] : "";
            }
        }

        private void OpenTips(GuildModel.AssistEntry data)
        {
            if (_tips == null || data == null) return;
            _pending = data;
            if (_tips._lb_desc1 != null) _tips._lb_desc1.text = "是否开始进行对" + data.Name + "的协助";
            if (_tips._lb_desc2 != null) _tips._lb_desc2.text = "完成后将获得以下奖励：";
            RenderRewards(data);
            _tips.Show(data);
            _tips.transform.SetAsLastSibling();
        }

        private void CloseTips()
        {
            _pending = null;
            _tips?.Hide();
        }

        private void ConfirmAssist()
        {
            GuildModel.AssistEntry data = _pending;
            if (data == null) return;
            // 老端在世界大妖且当前层不同会先 46004 退出；该 Scene/Boss 事务不能在 Guild 岛猜接。
            GuildController.Instance.HelpAssist(data.AssistId, data.Type);
            CloseTips();
        }

        private void RenderRewards(GuildModel.AssistEntry data)
        {
            List<(int style, int typeId, long num)> rewards = ReadRewards(data);
            EnsureRewardRows(rewards.Count);
            for (int i = 0; i < _rewardRows.Count; i++)
            {
                bool active = i < rewards.Count;
                if (!active)
                {
                    _rewardRows[i].Hide();
                    continue;
                }
                (int style, int typeId, long num) reward = rewards[i];
                (int goodsId, int locked) mapped = GoodsModel.GetMappingTypeId(reward.style, reward.typeId);
                _rewardRows[i].Show();
                _rewardRows[i].SetData(mapped.goodsId, reward.num, mapped.locked != 0);
            }
            if (_tips?._Scroller1 != null) _tips._Scroller1.gameObject.SetActive(rewards.Count > 0);
        }

        private void EnsureRewardRows(int count)
        {
            if (_tips?._tpl_EquipmentItem == null || _tips._gp_item == null) return;
            while (_rewardRows.Count < count)
            {
                GameObject go = UnityEngine.Object.Instantiate(_tips._tpl_EquipmentItem, _tips._gp_item);
                EquipmentItem item = go.GetComponent<EquipmentItem>();
                if (item == null)
                {
                    UnityEngine.Object.Destroy(go);
                    break;
                }
                go.SetActive(true);
                item.Show();
                _rewardRows.Add(item);
            }
        }

        private static List<(int style, int typeId, long num)> ReadRewards(GuildModel.AssistEntry data)
        {
            var result = new List<(int style, int typeId, long num)>();
            if (data.Type == 3)
            {
                GuildModel.AssistExtra extra = data.Extra != null && data.Extra.Count > 0 ? data.Extra[0] : null;
                if (extra?.RoberReward != null)
                    foreach (GuildModel.RewardEntry reward in extra.RoberReward)
                        result.Add((reward.Style, reward.TypeId, reward.Num));
                return result;
            }

            JObject cfg = GuildConfigs.GetAssistCfg(data.Type, data.SubType);
            string raw = cfg?["rewards"]?.ToString();
            if (string.IsNullOrWhiteSpace(raw)) return result;
            try
            {
                foreach (JToken row in JArray.Parse(raw))
                {
                    int style = row?["0"]?.Value<int>() ?? 0;
                    int typeId = row?["1"]?.Value<int>() ?? 0;
                    long num = row?["2"]?.Value<long>() ?? 0;
                    if (typeId > 0 && num > 0) result.Add((style, typeId, num));
                }
            }
            catch (Exception)
            {
                GameLog.Warn("Guild", "config_guild_assist rewards 解析失败 type={0} subType={1}", data.Type, data.SubType);
            }
            return result;
        }

        private static string GetTypeName(int type)
        {
            switch (type)
            {
                case 1: return "大妖协助";
                case 2: return "副本协助";
                case 3: return "无尽之海";
                case 4: return "斩秽巡行";
                default: return "仙宗协助";
            }
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
