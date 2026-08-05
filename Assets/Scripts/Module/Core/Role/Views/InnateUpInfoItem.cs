using System.Collections.Generic;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.InnateSkill;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Skill;
using UnityEngine;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 天赋技能升级面板(对标老客户端 innateSkill/InnateUpInfoItem.ts):升级条件行列表 + 消耗材料 + 升级按钮。
    ///
    /// 升级按钮拦截逻辑**不在此复刻**(老端 _btn_up 点击里手写了一遍满级/点数/point/pre_skill(2) 校验,与
    /// <see cref="SkillTalentModel.CanLearn"/> 同构):本端直接调 <see cref="SkillController.LearnTalent"/>,
    /// 该方法内部已走 CanLearn 做同等前置校验 + toast,不必在 View 层重复一遍。
    ///
    /// 条件行(_gp_up_cond)从隐藏模板 <see cref="InnateUpCondItem"/> 按需克隆复用池管理,point 类排前
    /// (对标几何报告 §1 InnateUpCondItem 小节 "point 类排前")。
    /// </summary>
    public sealed class InnateUpInfoItem : InnateUpInfoItemBind
    {
        /// <summary>"剩余天赋点" 图标用的参照货币(对标老端 less_point_good_ = 6200001:点击弹出货币说明 tooltip,
        /// 与实际升级消耗材料<see cref="SkillConfigs.TryGetGoodsCost"/> 相互独立)。</summary>
        private const int RefGoodTypeId = 6200001;

        private int _skillId;
        private bool _clickBound;
        private GameObject _condTemplate;
        private readonly List<InnateUpCondItem> _condPool = new List<InnateUpCondItem>();

        protected override void OnInit()
        {
            if (_gp_up_cond != null && _gp_up_cond.childCount > 0)
                _condTemplate = _gp_up_cond.GetChild(0).gameObject;

            RefreshCostIcon();

            if (_clickBound || _btn_up == null) return;
            UIUtil.AddClick(_btn_up, () => { if (_skillId > 0) SkillController.Instance.LearnTalent(_skillId); });
            _clickBound = true;
        }

        public void SetData(int skillId)
        {
            _skillId = skillId;
            if (skillId <= 0 || !SkillConfigs.Has(skillId))
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            SkillTalentModel model = SkillTalentModel.Instance;
            int lessPoint = model.LessPoint;
            int curLv = model.GetTalentLevel(skillId);
            int maxLv = SkillConfigs.GetMaxLevel(skillId);
            bool isMax = maxLv > 0 && curLv >= maxLv;
            int nextLv = isMax ? curLv : curLv + 1;

            SetButtonGray(!isMax && lessPoint <= 0);
            if (labelDisplay != null)
            {
                labelDisplay.text = isMax ? "已满级" : "升级";
                labelDisplay.color = isMax ? new Color(0.471f, 0.471f, 0.471f, 1f) : Color.white; // stroke 差异略,颜色对标 #787878/#ffffff 走底色近似
            }
            if (_Image1 != null) _Image1.color = isMax ? new Color(0.6f, 0.6f, 0.6f, 1f) : Color.white;

            // 老端这里展示的是固定的“技能天赋 ×1”参照货币，不读取当前技能 condition 中的 goods。
            // condition 仍只负责前置条件/真正发包前校验，不能把此处换成重置书或其它升级材料。
            bool showCost = !isMax;
            RefreshCostIcon();
            if (_lb_cost != null) _lb_cost.text = "1";
            if (_img_icon != null) _img_icon.gameObject.SetActive(showCost);
            if (_lb_cost != null) _lb_cost.gameObject.SetActive(showCost);
            if (_lb != null) _lb.gameObject.SetActive(showCost);

            BuildConditionRows(skillId, nextLv, isMax);
        }

        private void RefreshCostIcon()
        {
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(RefGoodTypeId);
            if (basic != null && _img_icon != null && !string.IsNullOrEmpty(basic.Icon))
                _ = ResManager.SetImageAsync(_img_icon, GameResPath.GetGoodsIconPath(basic.Icon), nativeSize: false);
        }

        private void SetButtonGray(bool gray)
        {
            if (_btn_up == null) return;
            UnityEngine.UI.Selectable selectable = _btn_up.GetComponent<UnityEngine.UI.Selectable>();
            if (selectable != null) selectable.interactable = !gray;
        }

        private void BuildConditionRows(int skillId, int nextLv, bool isMax)
        {
            foreach (InnateUpCondItem item in _condPool) if (item != null) item.gameObject.SetActive(false);
            int used = 0;

            if (!isMax)
            {
                ErlangTerm cond = SkillConfigs.GetConditionTerm(skillId, nextLv);
                if (cond?.Items != null)
                {
                    // point 类排前(对标几何报告)
                    foreach (ErlangTerm tuple in cond.Items)
                    {
                        if (!IsKind(tuple, "point", 3)) continue;
                        int needType = tuple.Items[1].As<int>();
                        int needPoint = tuple.Items[2].As<int>();
                        int have = SkillTalentModel.Instance.GetGroup(needType)?.Point ?? 0;
                        InnateUpCondItem row = GetOrCreateRow(used++);
                        if (row == null) continue;
                        row.SetPointCond(SkillUIConfigs.GetInnateTypeName(needType), have, needPoint);
                        row.gameObject.SetActive(true);
                    }
                    foreach (ErlangTerm tuple in cond.Items)
                    {
                        if (IsKind(tuple, "pre_skill", 3))
                        {
                            AddPreSkillRow(ref used, tuple.Items[1].As<int>(), tuple.Items[2].As<int>());
                        }
                        else if (IsKind(tuple, "pre_skill2", 2) && tuple.Items[1].IsCollection)
                        {
                            foreach (ErlangTerm sub in tuple.Items[1].Items)
                            {
                                if (!sub.IsCollection || sub.Items == null || sub.Items.Count < 2) continue;
                                AddPreSkillRow(ref used, sub.Items[0].As<int>(), sub.Items[1].As<int>());
                            }
                        }
                    }
                }
            }

            bool hasCond = used > 0;
            if (_lb_no_cond != null) _lb_no_cond.gameObject.SetActive(!hasCond);
            if (_scroll_cond != null) _scroll_cond.gameObject.SetActive(hasCond);
        }

        private void AddPreSkillRow(ref int used, int preSkillId, int preLv)
        {
            int haveLv = SkillTalentModel.Instance.GetTalentLevel(preSkillId);
            InnateUpCondItem row = GetOrCreateRow(used++);
            if (row == null) return;
            row.SetPreSkillCond(SkillConfigs.GetName(preSkillId), haveLv, preLv);
            row.gameObject.SetActive(true);
        }

        private static bool IsKind(ErlangTerm tuple, string kind, int minCount)
            => tuple.IsCollection && tuple.Items != null && tuple.Items.Count >= minCount && tuple.Items[0].As<string>() == kind;

        private InnateUpCondItem GetOrCreateRow(int index)
        {
            while (_condPool.Count <= index)
            {
                if (_condTemplate == null || _gp_up_cond == null) return null;
                GameObject clone = Instantiate(_condTemplate, _gp_up_cond);
                _condPool.Add(clone.GetComponent<InnateUpCondItem>());
            }
            return _condPool[index];
        }
    }
}
