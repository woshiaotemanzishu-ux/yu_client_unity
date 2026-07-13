using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Guild;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 捐献选择弹层(对标老客户端 guild/GuildDepotSelectView.ts):候选列表按 <see cref="IsDonatable"/>
    /// 过滤(对标老端 GetShowEquips:未绑定+装备类+阶≥4+星≥1+品质∈[3,5]),与服务端 40102 lists:all 全量校验
    /// 对齐——不合格物品不入候选,避免整批被 err401_goods_can_not_add_to_depot 拒绝。多选,`equipScore` 实时显示
    /// "选中装备积分"预估(按 config_guild_depot_score 逆算 stage/star/color),`btnDonate` 提交 40102。
    /// 降级:老端按性别/星/阶/品质的复杂排序简化为按品质降序;剑魄任务指引(finger)未接。
    /// </summary>
    public sealed class GuildDepotSelectView : GuildDepotSelectViewBind
    {
        private readonly List<GuildDepotItem> _rows = new List<GuildDepotItem>();
        private readonly Dictionary<long, BagGoods> _selected = new Dictionary<long, BagGoods>();
        private List<BagGoods> _candidates = new List<BagGoods>();

        protected override void OnInit()
        {
            if (_tpl_GuildDepotItem != null) _tpl_GuildDepotItem.SetActive(false);
            BindClick(_btn_close, Hide);
            BindClick(btnDonate, OnClickDonate);
        }

        protected override void OnShow(object args)
        {
            _selected.Clear();
            _candidates = BagModel.Instance.BagGoodsList.Where(IsDonatable).ToList();
            _candidates.Sort((a, b) => GoodsModel.GetColor(b.TypeId) - GoodsModel.GetColor(a.TypeId));
            RefreshList();
            RefreshScore();
        }

        /// <summary>对标老端 GuildModel.GetShowEquips:仅未绑定(bind==0)+装备类(type==10)+阶≥4+星≥1+品质∈[3,5]
        /// (排除 color==6 DEPOT_LIMIT_COLOR 且 &lt;7)方可捐献,与服务端 lib_guild_depot.erl:is_allow_add_to_depot 的
        /// Bind==0 ∧ 装备 ∧ Stage&gt;=4 ∧ Star&gt;=1 ∧ Color∈[3,5] 校验对齐——服务端 40102 用 lists:all,任一不合格
        /// 整批回 err401_goods_can_not_add_to_depot,故候选列表本身必须先行过滤,不能只靠 IsEquip。</summary>
        private static bool IsDonatable(BagGoods g)
        {
            if (g.Bind != 0) return false;
            if (!GoodsModel.IsEquip(g.TypeId)) return false;
            GoodsModel.EquipAttr attr = GoodsModel.GetEquipAttr(g.TypeId);
            if (attr == null || attr.Stage < 4 || attr.Star < 1) return false;
            int color = GoodsModel.GetColor(g.TypeId);
            if (color < 3 || color > 5) return false; // >=3 且 <7 且 !=6 ⇔ ∈{3,4,5}
            return true;
        }

        private void RefreshList()
        {
            if (_tpl_GuildDepotItem == null || _list == null || _list.content == null) return;
            EnsureRows(_candidates.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i < _candidates.Count;
                _rows[i].gameObject.SetActive(active);
                if (!active) continue;
                BagGoods g = _candidates[i];
                _rows[i].SetData(g.TypeId, g.GoodsNum, _selected.ContainsKey(g.GoodsId), () => ToggleSelect(g));
            }
        }

        private void ToggleSelect(BagGoods g)
        {
            if (_selected.ContainsKey(g.GoodsId)) _selected.Remove(g.GoodsId);
            else _selected[g.GoodsId] = g;
            RefreshList();
            RefreshScore();
        }

        /// <summary>对标老端 GetDepotScoreCfgByTypeID:typeId→(stage,star,color)→config_guild_depot_score 查分。</summary>
        private void RefreshScore()
        {
            if (equipScore == null) return;
            long total = 0;
            foreach (BagGoods g in _selected.Values)
            {
                GoodsModel.EquipAttr attr = GoodsModel.GetEquipAttr(g.TypeId);
                if (attr == null) continue;
                int color = GoodsModel.GetColor(g.TypeId);
                JObject row = GuildConfigs.GetDepotScore(attr.Stage, attr.Star, color);
                total += row?["score_cost"]?.Value<long>() ?? 0;
            }
            equipScore.text = "选中装备积分: " + total;
        }

        private void OnClickDonate()
        {
            if (_selected.Count == 0) { TipsManager.Toast("请先选中装备再进行捐献~"); return; }
            var list = new List<(long goodsId, int num)>(_selected.Count);
            foreach (BagGoods g in _selected.Values) list.Add((g.GoodsId, 1));
            GuildController.Instance.DonateDepot(list);
            Hide();
        }

        private void EnsureRows(int count)
        {
            while (_rows.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_GuildDepotItem, _list.content);
                go.SetActive(true);
                GuildDepotItem item = go.GetComponent<GuildDepotItem>();
                if (item != null) _rows.Add(item);
                else break;
            }
        }

        private static void BindClick(UnityEngine.Component target, System.Action onClick)
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
