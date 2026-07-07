using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Pet;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.OutWard;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Pet
{
    /// <summary>
    /// 培养页(对标老端 pet/OutWardBaseView.ts;坐骑/剑魄同修共用,按 <see cref="SetType"/> 切 type_id):
    /// 阶名/阶数(res_name/res_stage,config_mount_stage)+ 星级条(star*/shadow* 亮灰互斥)+ 战力(_gp_fight)
    /// + 祝福值环(exp_group,config_mount_star max_blessing)+ 一键提升(lv_button → 16023 StarUp)。
    /// 数据源 OutWardModel(16002/16023 真实回包),监听 EVT_OUTWARD_UPDATE 刷新。
    ///
    /// 页内任务引导(对标老端 PartnerComponentView.UpdateTask + story_obj_list):当前主线任务是
    /// TrainMount(23)/TrainPartner(25) 且对应本页 type_id 时——未完成 → 手指指 lv_button
    /// (ConfigTaskArrow in_view step2);达成 → 手指指窗框关闭钮(step3)。手指/光圈经 MainUIGuideManager。
    ///
    /// 降级:等级线(donw_group_2/OutwardLvSystem 子件)、幻化/背包/属性/魔晶/技能 子窗未移植 → 按钮日志降级;
    /// 3D 模型展示(res)归 3D 线。老端命名陷阱:star*=灰底星、shadow*=亮星(名字与视觉互换,照 Bind 对齐)。
    /// </summary>
    public sealed class OutWardBaseView : OutWardBaseViewBind
    {
        private int _typeId = 1;
        private bool _subscribed;

        /// <summary>切换培养对象(1=御风云骑/坐骑,2=剑魄同修/侍魂),PetFlow 页签驱动。</summary>
        public void SetType(int typeId)
        {
            if (typeId <= 0) return;
            _typeId = typeId;
            // 打开页时补拉一次(对标老端 OPEN_MOUNTPET_VIEW → 16002/16028 批量拉取)
            OutWardController.Instance.RequestInfo(_typeId);
            if (_typeId == 1 || _typeId == 2) OutWardController.Instance.RequestLvPanel(_typeId);
            Refresh();
            RefreshGuide();
        }

        protected override void OnInit()
        {
            HideStaticStates();
            BindButtons();
            Subscribe();
        }

        protected override void OnShow(object args)
        {
            _ = OutWardConfigs.EnsureLoaded();
            Refresh();
            RefreshGuide();
        }

        protected override void OnHide()
        {
            MainUIGuideManager.Instance.HideMainUiFinger(this);
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            MainUIGuideManager.Instance.HideMainUiFinger(this);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On(GlobalEvent.EVT_OUTWARD_UPDATE, OnOutWardUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskUpdate);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off(GlobalEvent.EVT_OUTWARD_UPDATE, OnOutWardUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskUpdate);
        }

        private void OnOutWardUpdate()
        {
            if (!gameObject.activeInHierarchy) return;
            Refresh();
            RefreshGuide();
        }

        private void OnTaskUpdate()
        {
            if (!gameObject.activeInHierarchy) return;
            RefreshGuide();
        }

        // ---------------------------------------------------------------- 数据渲染

        private void Refresh()
        {
            OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(_typeId);
            int career = RoleModel.Instance.Career;

            if (vo == null)
            {
                // 未收到 16002(冷启动/断链):如实显示空态,不造数(回包到达经 EVT_OUTWARD_UPDATE 刷新)
                if (res_name != null) res_name.text = "";
                if (res_stage != null) res_stage.text = "";
                if (level_value != null) level_value.text = "";
                SetStars(0, 0);
                SetCombat(0);
                return;
            }

            if (res_name != null) res_name.text = OutWardConfigs.GetStageName(_typeId, vo.Stage, career);
            if (res_stage != null) res_stage.text = vo.Stage + "阶";
            if (lvsystem_lv != null && vo.HasLv) lvsystem_lv.text = "Lv." + vo.Level;

            SetStars(vo.Star, OutWardConfigs.GetMaxStar(_typeId, vo.Stage, career));
            SetCombat(vo.Combat);
            SetBlessing(vo.Blessing, OutWardConfigs.GetMaxBlessing(_typeId, vo.Stage, vo.Star));
            SetAutoBuy(vo.AutoBuy == 1);
        }

        /// <summary>星级条:亮星(shadow*)显示 star 颗,灰底(star*)常显垫底(对标老端 SetStarNum)。</summary>
        private void SetStars(int star, int maxStar)
        {
            Image[] lit = { shadow, shadow0, shadow1, shadow2, shadow3, shadow4, shadow5, shadow6, shadow7, shadow8 };
            Image[] dark = { this.star, star0, star1, star2, star3, star4, star5, star6, star7, star8 };
            int slots = maxStar > 0 ? Mathf.Min(maxStar, dark.Length) : dark.Length;
            for (int i = 0; i < dark.Length; i++)
            {
                if (dark[i] != null) dark[i].gameObject.SetActive(i < slots);
                if (lit[i] != null) lit[i].gameObject.SetActive(i < Mathf.Min(star, slots));
            }
        }

        private void SetCombat(long combat)
        {
            // 战力标签是烤入的 FightingShowSmallItem 起步实例,收编其 _lb_fighting 刷真值
            if (_gp_fight == null) return;
            foreach (TextMeshProUGUI t in _gp_fight.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.gameObject.name == "_lb_fighting") { t.text = combat > 0 ? combat.ToString() : ""; return; }
            }
        }

        private void SetBlessing(long blessing, long maxBlessing)
        {
            if (level_text != null) level_text.text = "祝福值:";
            if (level_value != null)
            {
                level_value.text = maxBlessing > 0 ? blessing + "/" + maxBlessing : blessing.ToString();
            }
            if (exp_highlight != null)
            {
                // 老端用运行时遮罩表现进度环;Unity 对等改 fillAmount(运行时行为,非样式改动)
                if (exp_highlight.type != Image.Type.Filled)
                {
                    exp_highlight.type = Image.Type.Filled;
                    exp_highlight.fillMethod = Image.FillMethod.Radial360;
                    exp_highlight.fillOrigin = (int)Image.Origin360.Top;
                    exp_highlight.fillClockwise = true;
                }
                exp_highlight.fillAmount = maxBlessing > 0 ? Mathf.Clamp01((float)blessing / maxBlessing) : 0f;
            }
        }

        private void SetAutoBuy(bool on)
        {
            if (_Image14 != null) _Image14.gameObject.SetActive(!on);
            if (autoImg != null) autoImg.gameObject.SetActive(on);
        }

        // ---------------------------------------------------------------- 交互

        private void HideStaticStates()
        {
            HideNode(lv_btn_reddot); HideNode(bag_red); HideNode(illu_red);
            HideNode(btn_group_1_red); HideNode(btn_group_2_red);
            if (_tpl_FairyWishEnterBtn != null) _tpl_FairyWishEnterBtn.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_PetRoundItem != null) _tpl_PetRoundItem.SetActive(false);
            if (_tpl_PetEquipOutItem != null) _tpl_PetEquipOutItem.SetActive(false);
        }

        private void BindButtons()
        {
            // 一键提升(培养线主按钮,对标老端 OnOneKeyUPBtnClick → UpFunc → 16023;坐骑/同修专线)
            if (lv_button_img != null)
            {
                lv_button_img.raycastTarget = true;
                UIUtil.AddClick(lv_button_img, OnLvButton);
            }

            BindDegrade(before_btn1, "上一个外观(浏览切换)");
            BindDegrade(after_btn1, "下一个外观(浏览切换)");
            BindDegrade(proptity_btn, "属性 PetProptityView");
            BindDegrade(bag_btn, "侍魂装备背包 PetEquipBaseView");
            BindDegrade(illusion_btn, "幻化 IllusionBaseView");
            BindDegrade(_Image14, "自动购买切换");
            BindDegrade(autoImg, "自动购买切换");
            BindDegrade(select_1, "培养线页签(当前页)");
            BindDegrade(select_2, "等级线页签 OutwardLvSystem");
            BindDegrade(btn_switch, "培养线/等级线切换 OutwardLvSystem");
        }

        private void OnLvButton()
        {
            if (_typeId == 1 || _typeId == 2)
            {
                OutWardController.Instance.StarUp(_typeId);
            }
            else
            {
                OutWardController.Instance.StarUpGeneric(_typeId);
            }
        }

        private void BindDegrade(Image target, string label)
        {
            if (target == null) return;
            target.raycastTarget = true;
            UIUtil.AddClick(target, () =>
            {
                TipsManager.Toast(label.Split('(', ' ')[0] + " 待开放");
                GameLog.Info("Pet", "点击[{0}] → 待对接", label);
            });
        }

        // ---------------------------------------------------------------- 页内任务引导(ring3)

        /// <summary>
        /// 对标老端 PartnerComponentView/HorseComponentView.UpdateTask:主线培养任务在本页时,
        /// 未完成 → 手指指一键提升(in_view step2);达成 → 手指指窗框关闭钮(step3);其余隐藏。
        /// </summary>
        private void RefreshGuide()
        {
            TaskModel taskModel = TaskModel.Instance;
            TaskVo task = taskModel.MainLineTaskVo;
            if (task == null || !gameObject.activeInHierarchy || !taskModel.MainLineTaskNeedShowArrow())
            {
                MainUIGuideManager.Instance.HideMainUiFinger(this);
                return;
            }

            bool mine = (task.TaskTipsType == TaskModel.TIP_TRAIN_MOUNT && _typeId == 1)
                || (task.TaskTipsType == TaskModel.TIP_TRAIN_PARTNER && _typeId == 2);
            if (!mine)
            {
                MainUIGuideManager.Instance.HideMainUiFinger(this);
                return;
            }

            bool finish = taskModel.IsAllStepFinish(task.TaskId);
            TaskModel.TaskGuideStep step = taskModel.GetInViewGuideCfg(finish ? 3 : 2, task);
            RectTransform target = finish ? FindWindowClose() : lv_button;
            if (step == null || target == null)
            {
                MainUIGuideManager.Instance.HideMainUiFinger(this);
                return;
            }

            MainUIGuideManager.Instance.ShowMainUiFinger(this, target, BuildArrowData(step, target));
        }

        /// <summary>窗框关闭钮(对标老端 BaseWindowComponent._img_return;窗框由 PetFlow 装配)。</summary>
        private RectTransform FindWindowClose()
        {
            var window = PetFlow.CurrentWindow;
            if (window == null || window._img_return0 == null) return null;
            return window._img_return0.rectTransform;
        }

        private static ArrowData BuildArrowData(TaskModel.TaskGuideStep step, RectTransform target)
        {
            return new ArrowData
            {
                Content = step.Text,
                Direction = step.Direction,
                CloseTime = step.CloseTime,
                AutoCountdown = step.AutoCountdown,
                NotEffect = step.NotEffect,
                SelectEffectScale = new Vector3(step.EffectScaleX, step.EffectScaleY, step.EffectScaleZ),
                FingerEffectOffset = new Vector2(step.FingerOffsetX, step.FingerOffsetY),
                Offset = new Vector2(step.OffsetX, step.OffsetY),
                Target = target,
            };
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
