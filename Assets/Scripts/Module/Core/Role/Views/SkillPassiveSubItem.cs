using Shenxiao.Generated.UI.Role;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Skill;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 被动技能子项(对标老客户端 role/SkillPassiveSubItem.ts):技能名(_lb_name)+ 当前(_lb_now)/下级(_lb_next)效果描述 +
    /// 开启条件(lb_open)+ 升级按钮(_gp_level_up)。
    ///
    /// **RoleFlow 技能"被动技能"tab 的详情面板**(技能成长线轮3 接入,由 <see cref="SkillPassiveItem"/> 持有并驱动)。
    /// 分支对标老端 UpdateShowSkillInfo:
    ///   · id∈[300201,300207](特殊技能段)→ 纯展示"(未激活)"态,无升级按钮(对标老端首个 if 分支,未受下条影响)。
    ///   · 其余(绝大多数被动技能)→ 当前/下级描述 + 升级按钮 + 材料够/不够着色(对标老端末尾 else 分支)。
    ///     ⚠老端该 else 分支实际被前一条 `else if(1)`(IsHeartSkill 判定被写死为恒真)短路挡死永远不可达——
    ///     这是刷新时误留的死代码,不是设计意图;本端按原始设计纠正实现,升级按钮走真实 SkillController.UpgradeSkill
    ///     (21001 + 材料预校验),不复刻这处死链(规格 §3 汇报的偏差项之一)。
    /// </summary>
    public sealed class SkillPassiveSubItem : SkillPassiveSubItemBind
    {
        private const int SpecialSkillMin = 300201;
        private const int SpecialSkillMax = 300207;

        private int _boundSkillId;
        private bool _clickBound;

        protected override void OnInit()
        {
            EnsureClickBound();
        }

        public void SetData(string name)
        {
            if (_lb_name != null) _lb_name.text = name ?? "";
        }

        /// <summary>真实数据绑定(技能成长线轮3 补;上面 SetData(string) 是转换期遗留的名字桩,保留兼容)。</summary>
        public void SetData(SkillVo vo)
        {
            if (vo == null)
            {
                SetData("");
                _boundSkillId = 0;
                if (_gp_level_up != null) _gp_level_up.gameObject.SetActive(false);
                return;
            }

            SetData(vo.GetName());
            _boundSkillId = vo.Id;

            bool special = vo.Id >= SpecialSkillMin && vo.Id <= SpecialSkillMax;
            if (special)
            {
                bool open = vo.Level > 0;
                if (lb_open != null) lb_open.text = open ? "" : "(未激活)";
                if (lb_desc != null) lb_desc.text = vo.GetDesc();
                SetGroupVisible(desc1: false, desc2: true, desc3: false, expend: false, levelUp: false);
                return;
            }

            SetGroupVisible(desc1: true, desc2: false, desc3: false, expend: false, levelUp: false);

            if (_lb_now != null) _lb_now.text = vo.Level > 0 ? vo.GetDesc() : "暂未学习";

            bool hasNext = !vo.IsMaxLevel;
            if (_lb_next != null) _lb_next.text = hasNext ? vo.GetDesc(vo.Level + 1) : "已满级";

            int typeId = 0, need = 0;
            bool showCost = hasNext && vo.TryGetNextLevelGoodsCost(out typeId, out need);
            if (_gp_expend != null) _gp_expend.gameObject.SetActive(showCost);
            if (_gp_level_up != null) _gp_level_up.gameObject.SetActive(hasNext);

            if (showCost)
            {
                long have = BagModel.Instance.GetTypeGoodsNum(typeId);
                if (_lb_cost != null) _lb_cost.text = have + "/" + need;
                if (_img_red != null) _img_red.gameObject.SetActive(need <= have);

                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
                if (_img_cost != null && basic != null && !string.IsNullOrEmpty(basic.Icon))
                {
                    _ = ResManager.SetImageAsync(_img_cost, GameResPath.GetGoodsIconPath(basic.Icon), nativeSize: false);
                }
            }
        }

        private void SetGroupVisible(bool desc1, bool desc2, bool desc3, bool expend, bool levelUp)
        {
            if (_gp_desc1 != null) _gp_desc1.gameObject.SetActive(desc1);
            if (_gp_desc2 != null) _gp_desc2.gameObject.SetActive(desc2);
            if (_gp_desc3 != null) _gp_desc3.gameObject.SetActive(desc3);
            if (_gp_expend != null) _gp_expend.gameObject.SetActive(expend);
            if (_gp_level_up != null) _gp_level_up.gameObject.SetActive(levelUp);
        }

        private void EnsureClickBound()
        {
            if (_clickBound || btn_bg_1 == null) return;
            btn_bg_1.raycastTarget = true;
            UIUtil.AddClick(btn_bg_1, () =>
            {
                if (_boundSkillId > 0) SkillController.Instance.UpgradeSkill(_boundSkillId);
            });
            _clickBound = true;
        }
    }
}
