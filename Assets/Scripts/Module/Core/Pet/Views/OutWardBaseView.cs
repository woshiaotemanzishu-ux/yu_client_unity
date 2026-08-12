using System.Collections.Generic;
using System;
using System.Text;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Generated.UI.Pet;
using Shenxiao.Generated.UI.PetEquip;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dress;
using Shenxiao.Module.Core.FairyWish;
using Shenxiao.Module.Core.FunctionOpen;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.OutWard;
using Shenxiao.Module.Core.PetEquip;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Shop;
using Shenxiao.Module.Core.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Pet
{
    /// <summary>
    /// 培养页(对标老端 pet/OutWardBaseView.ts;坐骑/剑魄同修/翼影/古法符相/殒锋天刃/玄穹云披共用同一份 View,
    /// 按 <see cref="SetType"/> 切 type_id——第21轮起 PetFlow(1/2)与 RoleFlow(3/4/5/12)各自持一份独立实例):
    /// 阶名/阶数(res_name/res_stage,config_mount_stage)+ 星级条(star*/shadow* 亮灰互斥)+ 战力(_gp_fight)
    /// + 祝福值环(exp_group,config_mount_star max_blessing)+ 一键提升(lv_button → 16023/16005 StarUp)。
    /// 数据源 OutWardModel(16002/16023/16005 真实回包),监听 EVT_OUTWARD_UPDATE 刷新。
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
        private PetEquipOutItemBind[] _petEquipSlots;
        private PetRoundItemBind[] _skillSlots;
        private PetRoundItemBind[] _crystalSlots;
        private UIModelStage _modelStage;
        private int _modelEpoch;
        private string _modelKey;
        private bool _skillTipOpen;
        private bool _levelMode;
        private bool _modelRendered;
        private int _modelReadyVisiblePixels;
        private bool _modelLoadPending;
        private bool _modelPlaced;
        private bool _renderProbeWarned;
        private IllusionBaseView _illusionView;
        private Transform _illusionOriginalParent;
        private int _illusionOriginalSiblingIndex = -1;
        private OutwardLvSystemView _levelSystemView;
        private Transform _levelSystemOriginalParent;
        private int _levelSystemOriginalSiblingIndex = -1;
        private FairyWishEnterBtnBind _fairyEntry;
        private bool _fairyBindMissingLogged;
        private readonly OutWardModel.EffectLifecycleState _effectLifecycle = new OutWardModel.EffectLifecycleState();

        public bool ModelVisualReady => _modelRendered;
        public int ModelReadyVisiblePixels => _modelReadyVisiblePixels;
        public OutWardModel.EffectLifecycleState EffectLifecycle => _effectLifecycle;

        private void Awake()
        {
            CaptureSiblingIllusion();
            CaptureSiblingLevelSystem();
            CaptureFairyWishEntry();
        }

        private bool CaptureFairyWishEntry()
        {
            if (_fairyEntry != null) return true;
            if (enter_btn != null) _fairyEntry = enter_btn.GetComponent<FairyWishEnterBtnBind>();
            if (_fairyEntry != null) return true;
            if (!_fairyBindMissingLogged)
            {
                _fairyBindMissingLogged = true;
                GameLog.Error("OutWard", "enter_btn missing direct FairyWishEnterBtnBind; refuse arbitrary Image fallback");
            }
            return false;
        }

        /// <summary>
        /// PetModule 预加载时捕获同一实例内的正式幻化页。OutWardBaseView 随后会被 RoleFlow/PetFlow
        /// 重挂到共享窗框，不能再从新 parent 反查仍留在模块根下的兄弟节点。
        /// </summary>
        public bool CaptureSiblingIllusion()
        {
            if (_illusionView != null) return true;
            Transform moduleRoot = transform.parent;
            if (moduleRoot == null) return false;
            IllusionBaseView candidate = moduleRoot.GetComponentInChildren<IllusionBaseView>(true);
            if (candidate == null) return false;
            _illusionView = candidate;
            _illusionOriginalParent = candidate.transform.parent;
            _illusionOriginalSiblingIndex = candidate.transform.GetSiblingIndex();
            if (!candidate.IsShown) candidate.gameObject.SetActive(false);
            return true;
        }

        /// <summary>捕获 PetModule 同实例内默认隐藏的正式等级子页，供本 View 重挂后继续使用。</summary>
        public bool CaptureSiblingLevelSystem()
        {
            if (_levelSystemView != null) return true;
            Transform moduleRoot = transform.parent;
            if (moduleRoot == null) return false;
            OutwardLvSystemView candidate = moduleRoot.GetComponentInChildren<OutwardLvSystemView>(true);
            if (candidate == null) return false;
            _levelSystemView = candidate;
            _levelSystemOriginalParent = candidate.transform.parent;
            _levelSystemOriginalSiblingIndex = candidate.transform.GetSiblingIndex();
            if (!candidate.IsShown) candidate.gameObject.SetActive(false);
            return true;
        }

        /// <summary>切换培养对象(1=御风云骑/坐骑,2=剑魄同修/侍魂,3=翼影,4=古法符相,5=殒锋天刃,12=玄穹云披),
        /// PetFlow/RoleFlow 页签驱动。</summary>
        public void SetType(int typeId)
        {
            if (typeId <= 0) return;
            CaptureSiblingIllusion();
            CaptureSiblingLevelSystem();
            if (_typeId != typeId) ClearOutwardModel();
            _typeId = typeId;
            if (_levelMode && PrepareLevelSystemHost()) _levelSystemView.Open(this, _typeId);
            ApplyRoleOutwardStaticState();
            // 打开页时补拉一次。对标老端 OPEN_MOUNTPET_VIEW 的完整四包初始化:
            // 16002 阶星 + 16006 幻化列表 + 16011 魔晶次数 + 16028 等级线。
            OutWardController.Instance.RequestPanelData(_typeId);
            Refresh();
            RefreshGuide();
        }

        protected override void OnInit()
        {
            HideStaticStates();
            ApplyRoleOutwardStaticState();
            BindButtons();
            BindSkillSlots();
            BindCrystalSlots();
            BindPetEquipSlots();
            Subscribe();
            BindFairyWish();
        }

        protected override void OnShow(object args)
        {
            if (_levelMode && PrepareLevelSystemHost()) _levelSystemView.Open(this, _typeId);
            _ = EnsureConfigsThenRefresh();
            Refresh();
            RefreshGuide();
        }

        /// <summary>页面依赖的三张配置(阶名/星值/培养材料)+物品表+技能表就绪后再刷一遍(异步补齐首帧空态)。</summary>
        private async System.Threading.Tasks.Task EnsureConfigsThenRefresh()
        {
            await OutWardConfigs.EnsureLoaded();
            await Common.GoodsModel.EnsureLoaded();
            await Skill.SkillConfigs.EnsureLoaded();
            await FuncOpenConfig.EnsureLoaded();
            await ClientOutWardPosConfigs.EnsureLoaded();
            await FairyWishConfigs.EnsureLoaded();
            if (this == null || !gameObject.activeInHierarchy) return;
            Refresh();
        }

        protected override void OnHide()
        {
            CloseSkillTip();
            RestoreIllusionHost(clearReference: false);
            RestoreLevelSystemHost(clearReference: false);
            OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(_typeId);
            if ((_typeId == 1 || _typeId == 2) && vo != null && vo.AutoBuy == 1)
            {
                // Old H5 resets auto-buy whenever the cultivation page is destroyed/reopened.
                OutWardController.Instance.SetAutoBuy(_typeId, 0);
            }
            ClearOutwardModel();
            MainUIGuideManager.Instance.HideMainUiFinger(this);
        }

        protected override void OnDispose()
        {
            CloseSkillTip();
            RestoreIllusionHost(clearReference: true);
            RestoreLevelSystemHost(clearReference: true);
            Unsubscribe();
            DisposeOutwardModel();
            MainUIGuideManager.Instance.HideMainUiFinger(this);
        }

        private void OnDestroy()
        {
            CloseSkillTip();
            RestoreIllusionHost(clearReference: true);
            RestoreLevelSystemHost(clearReference: true);
            Unsubscribe();
            DisposeOutwardModel();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On(GlobalEvent.EVT_OUTWARD_UPDATE, OnOutWardUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskUpdate);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleOrFuncOpenUpdate);
            EventDispatcher.On(GlobalEvent.EVT_FUNC_OPEN_UPDATE, OnRoleOrFuncOpenUpdate);
            EventDispatcher.On<int>(GlobalEvent.EVT_OUTWARD_CRYSTAL_UPDATE, OnCrystalUpdate);
            EventDispatcher.On<int>(GlobalEvent.EVT_PET_EQUIP_UPDATE, OnPetEquipUpdate);
            EventDispatcher.On<int>(GlobalEvent.EVT_PET_EQUIP_BAG_UPDATE, OnPetEquipBagUpdate);
            EventDispatcher.On<int>(GlobalEvent.EVT_FAIRYWISH_UPDATE, OnFairyWishUpdate);
            EventDispatcher.On<int>(GlobalEvent.EVT_SHOP_BUY_SUCCESS, OnQuickBuySuccess);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnRoleOrFuncOpenUpdate);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off(GlobalEvent.EVT_OUTWARD_UPDATE, OnOutWardUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleOrFuncOpenUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_FUNC_OPEN_UPDATE, OnRoleOrFuncOpenUpdate);
            EventDispatcher.Off<int>(GlobalEvent.EVT_OUTWARD_CRYSTAL_UPDATE, OnCrystalUpdate);
            EventDispatcher.Off<int>(GlobalEvent.EVT_PET_EQUIP_UPDATE, OnPetEquipUpdate);
            EventDispatcher.Off<int>(GlobalEvent.EVT_PET_EQUIP_BAG_UPDATE, OnPetEquipBagUpdate);
            EventDispatcher.Off<int>(GlobalEvent.EVT_FAIRYWISH_UPDATE, OnFairyWishUpdate);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SHOP_BUY_SUCCESS, OnQuickBuySuccess);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnRoleOrFuncOpenUpdate);
        }

        private void OnOutWardUpdate()
        {
            // 宿主可能已被整树销毁而 OnDestroy 未触发(OnInit 订阅但从未激活的实例):就地退订自愈,防 MissingReference
            if (!this) { Unsubscribe(); return; }
            if (!gameObject.activeInHierarchy) return;
            Refresh();
            RefreshGuide();
        }

        private void OnTaskUpdate()
        {
            if (!this) { Unsubscribe(); return; }
            if (!gameObject.activeInHierarchy) return;
            RefreshPetEquipEntry();
            RefreshGuide();
        }

        private void OnRoleOrFuncOpenUpdate()
        {
            if (!this) { Unsubscribe(); return; }
            if (!gameObject.activeInHierarchy) return;
            RefreshPetEquipEntry();
            RefreshFairyWishEntry();
        }

        private void OnFairyWishUpdate(int fairyId)
        {
            if (!this) { Unsubscribe(); return; }
            if (!gameObject.activeInHierarchy || fairyId != 1000 + _typeId) return;
            RefreshFairyWishEntry();
        }

        private void OnCrystalUpdate(int typeId)
        {
            if (!this) { Unsubscribe(); return; }
            if (!gameObject.activeInHierarchy || typeId != _typeId) return;
            SetCrystals();
        }

        private void OnQuickBuySuccess(int keyId)
        {
            // 15304 用 keyId=0 哨兵；背包权威更新另行到达，这里只触发父页即时重投影。
            if (!this) { Unsubscribe(); return; }
            if (!gameObject.activeInHierarchy || keyId != 0) return;
            Refresh();
        }

        private void OnPetEquipUpdate(int typeId)
        {
            if (!this) { Unsubscribe(); return; }
            if (!gameObject.activeInHierarchy || typeId != _typeId) return;
            RefreshPetEquipEntry();
        }

        private void OnPetEquipBagUpdate(int pos)
        {
            if (!this) { Unsubscribe(); return; }
            int wornPos = _typeId == PetEquipController.TYPE_HORSE
                ? Bag.BagModel.POS_HORSE
                : Bag.BagModel.POS_PARTNER;
            if (!gameObject.activeInHierarchy || pos != wornPos) return;
            RefreshPetEquipEntry();
        }

        // ---------------------------------------------------------------- 数据渲染

        private void Refresh()
        {
            ApplyRoleOutwardStaticState();
            RefreshPetEquipEntry();
            RefreshFairyWishEntry();
            OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(_typeId);
            int career = RoleModel.Instance.Career;

            SetMaterials();
            SetCrystals();
            RefreshOneKeyAndIllusionRed(vo, career);

            if (vo == null)
            {
                // 未收到 16002(冷启动/断链):如实显示空态,不造数(回包到达经 EVT_OUTWARD_UPDATE 刷新)
                if (res_name != null) res_name.text = "";
                if (res_stage != null) res_stage.text = "";
                if (level_value != null) level_value.text = "";
                SetStars(0, 0);
                SetCombat(0);
                SetSkills(null);
                ClearOutwardModel();
                return;
            }


            if (_levelMode)
            {
                RenderLevelState(vo, career);
                return;
            }
            RestoreTrainContainers();

            if (res_name != null) res_name.text = OutWardConfigs.GetStageName(_typeId, vo.Stage, career);
            bool roleOutward = IsRoleOutwardType(_typeId);
            if (res_stage != null) res_stage.text = (roleOutward ? vo.Star : vo.Stage) + "阶";
            if (lvsystem_lv != null && vo.HasLv) lvsystem_lv.text = "Lv." + vo.Level;

            SetStars(vo.Star, OutWardConfigs.GetMaxStar(_typeId, vo.Stage, career));
            SetCombat(vo.Combat);
            SetBlessing(vo.Blessing, OutWardConfigs.GetMaxBlessing(_typeId, vo.Stage, vo.Star));
            if (roleOutward && level_text != null) level_text.text = "Lv." + vo.Star;
            SetAutoBuy(vo.AutoBuy == 1);
            SetSkills(vo.Skills);
            if (roleOutward) SetBaseAppearanceState(vo);
            RefreshOutwardModel(vo, career);
        }

        /// <summary>
        /// 技能球对标老端 GetDefaultSkillList + SetSkillData：config_mount_skill(type=1) 决定全部常驻槽，
        /// 16002 skill_list 只决定解锁/灰态，未解锁技能仍显示且可点详情。
        /// </summary>
        private void SetSkills(List<int> skills)
        {
            if (skill_group == null) return;
            IReadOnlyList<int> configured = OutWardConfigs.GetDefaultSkillIds(_typeId);
            var visibleSkills = new List<int>(configured.Count);
            for (int i = 0; i < configured.Count; i++)
            {
                int skillId = configured[i];
                // 老端明确排除 config_skill.type==1 的主动技能。
                if (Skill.SkillConfigs.GetSkillType(skillId) != 1) visibleSkills.Add(skillId);
            }

            PetRoundItemBind[] slots = _skillSlots ?? new PetRoundItemBind[0];
            for (int i = 0; i < slots.Length; i++)
            {
                bool has = i < visibleSkills.Count;
                slots[i].gameObject.SetActive(has);
                if (!has || slots[i].icon == null) continue;
                int skillId = visibleSkills[i];
                bool unlocked = skills != null && skills.Contains(skillId);
                string iconName = Skill.SkillConfigs.GetIconForLevel(skillId, 1);
                if (!string.IsNullOrEmpty(iconName))
                    _ = ResManager.SetImageAsync(slots[i].icon, GameResPath.GetSkillIcon(iconName), nativeSize: false);
                UIGrayStyle.Apply(slots[i].icon, !unlocked);
                if (slots[i].bottom_text != null) slots[i].bottom_text.text = "";
            }
        }

        /// <summary>培养材料(material_group 烤入的 BaseAwardItem 实例,对标老端材料区):config_mount_goods 该 type 的
        /// 物品按 id 序填前两格(图标 config_goods.goods_icon + 数量=背包持有);配置缺失槽位隐藏。</summary>
        private void SetMaterials()
        {
            if (material_group == null) return;
            IReadOnlyList<int> goods = OutWardConfigs.GetTrainGoodsIds(_typeId);
            BaseAwardItem[] slots = material_group.GetComponentsInChildren<BaseAwardItem>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                bool has = i < goods.Count;
                slots[i].gameObject.SetActive(has);
                if (!has) continue;
                int goodsId = goods[i];
                long count = CountInBag(goodsId);
                // 必须走共享组件公开 API，写入真实 typeId，默认点击才会打开对应 ItemTipsView。
                slots[i].SetData(goodsId, count);
                // 老端培养材料 0 隐藏、1 起显示；共享格常规语义是 >1，页面只覆盖计数可见性。
                if (slots[i].num_text != null)
                {
                    slots[i].num_text.gameObject.SetActive(count >= 1);
                    if (count >= 1) slots[i].num_text.text = Common.GoodsModel.FormatCountNum(count);
                }
            }
        }

        /// <summary>
        /// 魔晶槽来自 config_mount_goods，次数来自 16011；本层只渲染，不代替真实 UI 点击执行 16010。
        /// </summary>
        private void SetCrystals()
        {
            if (crystal_group == null) return;
            IReadOnlyList<int> goods = OutWardConfigs.GetCrystalGoodsIds(_typeId);
            IReadOnlyList<(int goodsId, int times, int timesLim)> counters =
                OutWardModel.Instance.GetCrystalCounters(_typeId);
            PetRoundItemBind[] slots = _crystalSlots ?? new PetRoundItemBind[0];
            for (int i = 0; i < slots.Length; i++)
            {
                bool has = i < goods.Count;
                PetRoundItemBind slot = slots[i];
                slot.gameObject.SetActive(has);
                if (!has) continue;

                int goodsId = goods[i];
                int times = 0;
                int limit = 0;
                if (counters != null)
                {
                    for (int j = 0; j < counters.Count; j++)
                    {
                        if (counters[j].goodsId != goodsId) continue;
                        times = counters[j].times;
                        limit = counters[j].timesLim;
                        break;
                    }
                }

                string iconName = Common.GoodsModel.GetGoodsIcon(goodsId);
                if (slot.icon != null && !string.IsNullOrEmpty(iconName))
                    _ = ResManager.SetImageAsync(slot.icon, GameResPath.GetGoodsIconPath(iconName), nativeSize: false);
                if (slot.bottom_text != null)
                    slot.bottom_text.text = limit > 0 ? times + "/" + limit : times.ToString();
                if (slot.red_dot != null)
                    slot.red_dot.gameObject.SetActive(limit > times && CountInBag(goodsId) > 0);
                if (slot.skill_info_gp != null) slot.skill_info_gp.gameObject.SetActive(false);
                if (slot.up_arrow1 != null) slot.up_arrow1.gameObject.SetActive(false);
            }
        }

        private static long CountInBag(int goodsTypeId)
        {
            long n = 0;
            foreach (Bag.BagGoods g in Bag.BagModel.Instance.BagGoodsList)
            {
                if (g.TypeId == goodsTypeId) n += g.GoodsNum;
            }
            return n;
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
            bool visible = !IsRoleOutwardType(_typeId) && (_typeId == 1 || _typeId == 2);
            if (autoGp != null) autoGp.gameObject.SetActive(visible);
            if (!visible) return;
            if (_Image14 != null) _Image14.gameObject.SetActive(!on);
            if (autoImg != null) autoImg.gameObject.SetActive(on);
        }

        private static bool IsRoleOutwardType(int typeId)
        {
            return typeId == 3 || typeId == 4 || typeId == 5 || typeId == 12;
        }

        /// <summary>
        /// The four show_type=1 pages do not browse stages. Their inline appearance state compares the
        /// current base stage with the authoritative FigureStage from 16002.
        /// </summary>
        private void SetBaseAppearanceState(OutWardModel.OutWardVo vo)
        {
            if (vo == null || !IsRoleOutwardType(_typeId)) return;
            bool usingBase = vo.FigureStage == vo.Stage;
            if (illu_group != null) illu_group.gameObject.SetActive(true);
            if (using_gp != null) using_gp.gameObject.SetActive(usingBase);
            if (unuse_gp != null) unuse_gp.gameObject.SetActive(!usingBase);
            if (preview_image != null) preview_image.gameObject.SetActive(false);
        }

        /// <summary>Old show_type=1 subclasses share these visibility rules.</summary>
        private void ApplyRoleOutwardStaticState()
        {
            if (!IsRoleOutwardType(_typeId)) return;
            HideNode(star_group);
            HideNode(shadow_group);
            HideNode(star_effect);
            HideNode(before_btn);
            HideNode(after_btn);
            HideNode(autoGp);
            HideNode(_group_equip);
            if (bag_btn != null) bag_btn.gameObject.SetActive(false);
            if (lv_button_text != null) lv_button_text.text = "一键提升";
        }

        private void RefreshOutwardModel(OutWardModel.OutWardVo vo, int career)
        {
            if (res == null || vo == null) return;
            int showId = OutWardConfigs.GetStageModelRes(_typeId, vo.Stage, career);
            RefreshOutwardModel(showId);
        }

        private void RefreshOutwardModel(int showId)
        {
            if (showId <= 0 || !TryGetModelProfile(_typeId, out string module, out string prefix, out string fallback))
            {
                ClearOutwardModel();
                return;
            }

            string address = BuildModelAddress(module, showId);
            if (_modelKey == address)
            {
                if (_modelPlaced)
                {
                    if (!_modelRendered) _ = AwaitRenderedModelAsync(_modelEpoch, address);
                    return;
                }
                // The first SetType can run while the content BaseView is not shown yet. If that
                // asynchronous load finishes in the hidden interval, it must not leave the address
                // cached as though a model had been placed; the next visible Refresh must retry.
                if (_modelLoadPending) return;
                _modelKey = null;
            }
            _modelStage?.ClearStage();
            _modelKey = address;
            _modelLoadPending = true;
            _modelPlaced = false;
            _modelRendered = false;
            _modelReadyVisiblePixels = 0;
            int epoch = ++_modelEpoch;
            _effectLifecycle.Begin(epoch, address);
            _ = LoadOutwardModelAsync(epoch, address, module, prefix, fallback, showId);
        }

        private async Task LoadOutwardModelAsync(int epoch, string address, string module,
            string prefix, string fallback, int showId)
        {
            GameObject prefab = null;
            try
            {
                prefab = await ResManager.LoadAsync<GameObject>(address);
                await ClientOutWardPosConfigs.EnsureLoaded();
                if (!this || epoch != _modelEpoch || _modelKey != address) return;
                if (!gameObject.activeInHierarchy || res == null)
                {
                    // Do not freeze the first-open hidden-load race into _modelKey. A visible
                    // OnShow/Refresh will immediately request the same production model again.
                    _modelLoadPending = false;
                    _modelKey = null;
                    GameLog.Info("OutWard", "model load deferred until visible address={0}", address);
                    return;
                }
                if (prefab == null)
                {
                    GameLog.Warn("OutWard", "outward model missing: type={0} address={1}", _typeId, address);
                    _modelLoadPending = false;
                    _modelKey = null;
                    return;
                }

                UiModelParameterConfigs.ModelParam mp = ClientOutWardPosConfigs.Get(prefix + "_" + showId, fallback);
                GameObject instance = Instantiate(prefab);
                if (!this || epoch != _modelEpoch || _modelKey != address || !gameObject.activeInHierarchy || res == null)
                {
                    Destroy(instance);
                    if (this && epoch == _modelEpoch && _modelKey == address)
                    {
                        _modelLoadPending = false;
                        _modelKey = null;
                    }
                    return;
                }

                if (_modelStage == null) _modelStage = new UIModelStage();
                _modelStage.EnableDragRotate(true);
                res.gameObject.SetActive(true);
                _modelStage.PlaceInstance(res, instance, mp.Scale, mp.Position, mp.Rotate);
                _modelPlaced = true;
                _modelLoadPending = false;
                _effectLifecycle.MarkAttached(epoch);
                _ = EffectBinder.AttachAlways(instance, module, showId.ToString());
                _ = PlayOutwardIdleAsync(instance, module, showId);
                _ = AwaitRenderedModelAsync(epoch, address);
            }
            catch (Exception e)
            {
                if (this && epoch == _modelEpoch && _modelKey == address)
                {
                    _modelLoadPending = false;
                    _modelPlaced = false;
                    _modelKey = null;
                    _modelStage?.ClearStage();
                    _effectLifecycle.Fail(epoch, e.Message);
                    GameLog.Warn("OutWard", "outward model load failed address={0} error={1}", address, e.Message);
                }
            }
        }

        private async Task AwaitRenderedModelAsync(int epoch, string address)
        {
            _modelRendered = false;
            _modelReadyVisiblePixels = 0;
            int visible = 0;
            for (int frame = 0; frame < 12 && visible < 8; frame++)
            {
                await Task.Yield();
                if (!IsCurrentModel(epoch, address)) return;
                _modelStage.RenderStageNow();
                (int sampledVisible, int fingerprint) = ProbeFrame();
                visible = sampledVisible;
                _effectLifecycle.ObserveFrame(epoch, visible, fingerprint);
            }
            _modelReadyVisiblePixels = Mathf.Max(0, visible);
            _modelRendered = visible >= 8;
            if (_modelRendered)
                GameLog.Info("OutWard", "model RT ready pixels={0} address={1}", visible, address);
            else
                GameLog.Warn("OutWard", "model RT not ready pixels={0} address={1}", visible, address);
        }

        private bool IsCurrentModel(int epoch, string address)
            => this && epoch == _modelEpoch && _modelKey == address && gameObject.activeInHierarchy && _modelStage != null;

        private (int visible, int fingerprint) ProbeFrame()
        {
            RenderTexture source = null;
            if (res != null)
                foreach (RawImage image in res.GetComponentsInChildren<RawImage>(true))
                    if (image.texture is RenderTexture rt) { source = rt; break; }
            if (source == null || !source.IsCreated()) return (0, 0);
            RenderTexture previous = RenderTexture.active;
            RenderTexture sampleRt = null;
            Texture2D sample = null;
            try
            {
                sampleRt = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Graphics.Blit(source, sampleRt);
                RenderTexture.active = sampleRt;
                sample = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
                sample.ReadPixels(new Rect(0f, 0f, 64f, 64f), 0, 0, false);
                sample.Apply(false, false);
                int visible = 0;
                unchecked
                {
                    int fingerprint = 17;
                    foreach (Color32 px in sample.GetPixels32())
                    {
                        if (px.a > 8 && (px.r > 4 || px.g > 4 || px.b > 4)) visible++;
                        fingerprint = fingerprint * 31 + px.r;
                        fingerprint = fingerprint * 31 + px.g;
                        fingerprint = fingerprint * 31 + px.b;
                        fingerprint = fingerprint * 31 + px.a;
                    }
                    return (visible, fingerprint);
                }
            }
            catch (Exception e)
            {
                if (!_renderProbeWarned) { _renderProbeWarned = true; GameLog.Warn("OutWard", "model RT probe failed: {0}", e.Message); }
                return (-1, 0);
            }
            finally
            {
                RenderTexture.active = previous;
                if (sampleRt != null) RenderTexture.ReleaseTemporary(sampleRt);
                if (sample != null) { if (Application.isPlaying) Destroy(sample); else DestroyImmediate(sample); }
            }
        }

        private static async Task PlayOutwardIdleAsync(GameObject instance, string module, int showId)
        {
            if (instance == null) return;
            const string action = "idle";
            Animation anim = instance.GetComponent<Animation>();
            if (anim != null && anim.GetClip(action) != null)
            {
                anim.Play(action);
                return;
            }
            AnimationClip clip = await ResManager.LoadAsync<AnimationClip>(
                "object/" + module + "/action/" + showId + "/" + action);
            if (instance == null || clip == null) return;
            if (anim == null) anim = instance.AddComponent<Animation>();
            if (anim.GetClip(action) == null) anim.AddClip(clip, action);
            anim.Play(action);
        }

        private static bool TryGetModelProfile(int typeId, out string module, out string prefix, out string fallback)
        {
            switch (typeId)
            {
                case 1: module = "mount"; prefix = "h"; fallback = "default_horse"; return true;
                case 2: module = "spirit"; prefix = "s"; fallback = "default_sprite"; return true;
                case 3: module = "wing"; prefix = "w"; fallback = "default_wing"; return true;
                case 4: module = "fabao"; prefix = "a"; fallback = "default_artifact"; return true;
                case 5: module = "weapon"; prefix = "d"; fallback = "default_weapon"; return true;
                case 12: module = "back"; prefix = "b"; fallback = "default_back_ornament"; return true;
                default: module = null; prefix = null; fallback = null; return false;
            }
        }

        private static string BuildModelAddress(string module, int showId)
        {
            string name = module == "weapon" ? "model_weapon_r_" + showId : "model_" + module + "_" + showId;
            return "object/" + module + "/" + name + "/" + name;
        }

        private void ClearOutwardModel()
        {
            _modelEpoch++;
            _effectLifecycle.Release(_modelEpoch);
            _modelKey = null;
            _modelLoadPending = false;
            _modelPlaced = false;
            _modelRendered = false;
            _modelReadyVisiblePixels = 0;
            _modelStage?.ClearStage();
        }

        private void DisposeOutwardModel()
        {
            _modelEpoch++;
            _effectLifecycle.Release(_modelEpoch);
            _modelKey = null;
            _modelLoadPending = false;
            _modelPlaced = false;
            _modelRendered = false;
            _modelReadyVisiblePixels = 0;
            if (_modelStage == null) return;
            _modelStage.Dispose();
            _modelStage = null;
        }

        // ---------------------------------------------------------------- 交互

        private void HideStaticStates()
        {
            HideNode(lv_btn_reddot); HideNode(bag_red); HideNode(illu_red);
            HideNode(btn_group_1_red); HideNode(btn_group_2_red);
            // 侍魂装备位:数据链(PetEquipModel/pt_14x)未移植 → 整组隐藏(老端坐骑页也不显示;接入后按 type/数据显隐)
            HideNode(_group_equip);
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
            if (proptity_btn != null)
            {
                proptity_btn.raycastTarget = true;
                UIUtil.AddClick(proptity_btn, OpenProperty);
            }
            if (bag_btn != null)
            {
                bag_btn.raycastTarget = true;
                UIUtil.AddClick(bag_btn, OpenPetEquip);
            }
            if (illusion_btn != null)
            {
                illusion_btn.raycastTarget = true;
                UIUtil.AddClick(illusion_btn, OpenIllusion);
            }
            if (autoGp != null) UIUtil.AddClick(autoGp, ToggleAutoBuy);
            BindRectClick(btn_group_1, () => SetLevelMode(false));
            BindRectClick(btn_group_2, () => SetLevelMode(true));
            if (btn_switch != null) UIUtil.AddClick(btn_switch, () => SetLevelMode(!_levelMode));
            if (illu_group != null) UIUtil.AddClick(illu_group, OnBaseAppearanceClick);
        }

        private void OpenProperty()
        {
            OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(_typeId);
            OutWardPropertyFlow.Show(_levelMode ? vo?.LvAttrs : vo?.Attrs);
        }

        private static void BindRectClick(RectTransform target, Action action)
        {
            if (target == null) return;
            Image hit = target.GetComponent<Image>() ?? target.GetComponentInChildren<Image>(true);
            if (hit == null) return;
            hit.raycastTarget = true;
            UIUtil.AddClick(hit, action);
        }

        private void OpenIllusion()
        {
            Transform sharedHost = transform.parent;
            if (!PrepareIllusionHost(sharedHost))
            {
                GameLog.Error("OutWard", "PetModule missing production IllusionBaseView");
                return;
            }
            _illusionView.Open(this, _typeId);
        }

        public bool PrepareIllusionHost(Transform sharedHost)
        {
            if (sharedHost == null || (_illusionView == null && !CaptureSiblingIllusion())) return false;
            if (_illusionView.transform.parent != sharedHost)
                _illusionView.transform.SetParent(sharedHost, false);
            return _illusionView.transform.parent == sharedHost;
        }

        public void RestoreCapturedIllusion() => RestoreIllusionHost(clearReference: false);

        public void RestoreCapturedLevelSystem() => RestoreLevelSystemHost(clearReference: false);

        private void RestoreIllusionHost(bool clearReference)
        {
            if (_illusionView != null)
            {
                if (_illusionView.IsShown) _illusionView.Hide();
                else _illusionView.gameObject.SetActive(false);
                if (_illusionOriginalParent != null && _illusionView.transform.parent != _illusionOriginalParent)
                {
                    _illusionView.transform.SetParent(_illusionOriginalParent, false);
                    if (_illusionOriginalSiblingIndex >= 0)
                        _illusionView.transform.SetSiblingIndex(Mathf.Min(
                            _illusionOriginalSiblingIndex, _illusionOriginalParent.childCount - 1));
                }
            }
            if (!clearReference) return;
            _illusionView = null;
            _illusionOriginalParent = null;
            _illusionOriginalSiblingIndex = -1;
        }

        public bool PrepareLevelSystemHost()
        {
            if (donw_group_2 == null || (_levelSystemView == null && !CaptureSiblingLevelSystem())) return false;
            if (_levelSystemView.transform.parent != donw_group_2)
                _levelSystemView.transform.SetParent(donw_group_2, false);
            return _levelSystemView.transform.parent == donw_group_2;
        }

        private void RestoreLevelSystemHost(bool clearReference)
        {
            if (_levelSystemView != null)
            {
                if (_levelSystemView.IsShown) _levelSystemView.Hide();
                else _levelSystemView.gameObject.SetActive(false);
                if (_levelSystemOriginalParent != null && _levelSystemView.transform.parent != _levelSystemOriginalParent)
                {
                    _levelSystemView.transform.SetParent(_levelSystemOriginalParent, false);
                    if (_levelSystemOriginalSiblingIndex >= 0)
                        _levelSystemView.transform.SetSiblingIndex(Mathf.Min(
                            _levelSystemOriginalSiblingIndex, _levelSystemOriginalParent.childCount - 1));
                }
            }
            if (!clearReference) return;
            _levelSystemView = null;
            _levelSystemOriginalParent = null;
            _levelSystemOriginalSiblingIndex = -1;
        }

        private void SetLevelMode(bool levelMode)
        {
            if (levelMode && !IsLevelSystemOpen()) return;
            if (_levelMode == levelMode) return;
            _levelMode = levelMode;
            if (_levelMode)
            {
                if (!PrepareLevelSystemHost())
                {
                    _levelMode = false;
                    GameLog.Error("OutWard", "PetModule missing production OutwardLvSystemView");
                    return;
                }
                _levelSystemView.Open(this, _typeId);
            }
            else
            {
                RestoreLevelSystemHost(clearReference: false);
            }
            Refresh();
        }

        private bool IsLevelSystemOpen()
        {
            string view = _typeId == 1 ? "HorseLvSystem"
                : _typeId == 2 ? "PartnerLvSystem"
                : _typeId == 3 ? "WingsLvSystem"
                : _typeId == 4 ? "ArtifactLvSystem"
                : _typeId == 5 ? "HolyDeviceLvSystem"
                : _typeId == 12 ? "BackOrnamentLvSystem" : string.Empty;
            return !string.IsNullOrEmpty(view) && FuncOpenConfig.IsLoaded
                && FuncOpenConfig.CheckFuncOpenState(view);
        }

        private void RenderLevelState(OutWardModel.OutWardVo vo, int career)
        {
            if (down_group_1 != null) down_group_1.gameObject.SetActive(false);
            if (donw_group_2 != null) donw_group_2.gameObject.SetActive(true);
            if (select_1 != null) select_1.gameObject.SetActive(false);
            if (select_2 != null) select_2.gameObject.SetActive(true);
            if (res_name != null) res_name.text = OutWardConfigs.GetStageName(_typeId, vo.Stage, career);
            if (res_stage != null) res_stage.text = vo.HasLv ? vo.Level + "级" : "加载中";
            if (lvsystem_lv != null) lvsystem_lv.text = vo.HasLv ? "Lv." + vo.Level : "";
            SetCombat(vo.LvCombat);
            SetBlessing(vo.CurExp, OutWardConfigs.GetLevelNeedExp(_typeId, vo.Level));
            if (level_text != null) level_text.text = "经验:";
            if (lv_button_text != null) lv_button_text.text = "升级";
            // 等级三技能、经验、材料和16029/16030交互只由正式 OutwardLvSystemView 承载；
            // 主页面散落 skill_group 不再冒充等级系统。
            _levelSystemView?.RefreshView();
            RefreshOutwardModel(vo, career);
        }

        private void RestoreTrainContainers()
        {
            if (down_group_1 != null) down_group_1.gameObject.SetActive(true);
            if (donw_group_2 != null) donw_group_2.gameObject.SetActive(false);
            if (select_1 != null) select_1.gameObject.SetActive(true);
            if (select_2 != null) select_2.gameObject.SetActive(false);
            if (lv_button_text != null) lv_button_text.text = "一键提升";
        }

        private void BindFairyWish()
        {
            if (!CaptureFairyWishEntry() || _fairyEntry.img_btn == null) return;
            _fairyEntry.img_btn.raycastTarget = true;
            _ = ResManager.SetLayaTextureAsync(_fairyEntry.img_btn,
                GameResPath.GetIcon("pet", "wxzg_yy"), nativeSize: false);
            UIUtil.AddClick(_fairyEntry.img_btn, () =>
            {
                int fairyId = 1000 + _typeId;
                FairyWishController.Instance.ConfirmEntryTouch(fairyId);
                FairyWishFlow.Open(fairyId);
            });
        }

        private void RefreshOneKeyAndIllusionRed(OutWardModel.OutWardVo vo, int career)
        {
            if (_levelMode) return;
            OutWardModel.OneKeyState oneKey = OutWardModel.Instance.GetOneKeyState(_typeId, career, CountInBag);
            if (lv_btn_reddot != null) lv_btn_reddot.gameObject.SetActive(oneKey.ShowRedDot);
            if (lv_button_img != null) lv_button_img.raycastTarget = oneKey.Availability != OutWardModel.OneKeyAvailability.MaxStage;
            if (lv_button_text != null)
                lv_button_text.text = oneKey.Availability == OutWardModel.OneKeyAvailability.MaxStage ? "已满阶" : "一键提升";

            int roleTurn = RoleModel.Instance.Figure?.turn ?? 0;
            OutWardModel.IllusionRedState illusion = OutWardModel.Instance.GetIllusionRedState(_typeId, career, roleTurn, CountInBag);
            if (illu_red != null) illu_red.gameObject.SetActive(illusion.ShowRedDot);
        }

        private void RefreshFairyWishEntry()
        {
            if (enter_btn == null || !CaptureFairyWishEntry()) return;
            FairyWishConfigs.FairyRow cfg = FairyWishConfigs.GetFairy(1000 + _typeId);
            int openDay = ServerTimeModel.GetOpenServerDay();
            if (openDay <= 0) openDay = 1;
            bool visible = cfg != null && RoleModel.Instance.Level >= cfg.OpenLevel && openDay >= cfg.OpenDay;
            enter_btn.gameObject.SetActive(visible);
            FairyWishModel.EntryRedState state = FairyWishModel.Instance.GetEntryRedState(1000 + _typeId);
            bool showBubble = visible && FairyWishModel.Instance.GetFairy(1000 + _typeId)?.IsBuy != 1
                && state == FairyWishModel.EntryRedState.Bubble;
            if (_fairyEntry.box_pop != null) _fairyEntry.box_pop.gameObject.SetActive(showBubble);
            if (_fairyEntry.effect_con != null) _fairyEntry.effect_con.gameObject.SetActive(showBubble);
            if (_fairyEntry.htmlContent != null)
                _fairyEntry.htmlContent.text = showBubble ? GetFairyWishBubbleText(1000 + _typeId) : string.Empty;
            if (_fairyEntry.img_red != null)
            {
                bool bought = FairyWishModel.Instance.GetFairy(1000 + _typeId)?.IsBuy == 1;
                _fairyEntry.img_red.gameObject.SetActive(visible && !showBubble
                    && (bought || state == FairyWishModel.EntryRedState.RedDot));
            }
            if (btn_switch != null) btn_switch.gameObject.SetActive(IsLevelSystemOpen());
            if (btn_group_2 != null) btn_group_2.gameObject.SetActive(_levelMode && IsLevelSystemOpen());
        }

        private static string GetFairyWishBubbleText(int fairyId)
        {
            switch (fairyId)
            {
                case 1001: return "冒险菜鸟";
                case 1002: return "渐入佳境";
                case 1003: return "沉着老练";
                case 1004: return "名声大噪";
                case 1005: return "所向披靡";
                default: return string.Empty;
            }
        }

        private void BindSkillSlots()
        {
            if (skill_group == null)
            {
                _skillSlots = new PetRoundItemBind[0];
                return;
            }

            var slots = new List<PetRoundItemBind>();
            foreach (PetRoundItemBind slot in skill_group.GetComponentsInChildren<PetRoundItemBind>(true))
            {
                if (slot == null || slot.gameObject == _tpl_PetRoundItem) continue;
                int index = slots.Count;
                slots.Add(slot);
                if (slot.click_group != null) UIUtil.AddClick(slot.click_group, () => OnSkillSlot(index));
            }
            _skillSlots = slots.ToArray();
        }

        private void OnSkillSlot(int index)
        {
            IReadOnlyList<int> configured = OutWardConfigs.GetDefaultSkillIds(_typeId);
            var visibleSkills = new List<int>(configured.Count);
            for (int i = 0; i < configured.Count; i++)
            {
                int skillId = configured[i];
                if (Skill.SkillConfigs.GetSkillType(skillId) != 1) visibleSkills.Add(skillId);
            }
            if (index < 0 || index >= visibleSkills.Count) return;
            // 复用 CommonModule/SkillTipsView 的既有共享消费者，不复制详情节点树。
            _skillTipOpen = true;
            DressSkillTipFlow.Show(visibleSkills[index]);
        }

        private void CloseSkillTip()
        {
            if (!_skillTipOpen) return;
            _skillTipOpen = false;
            DressSkillTipFlow.Close();
        }

        private void OnBaseAppearanceClick()
        {
            if (!IsRoleOutwardType(_typeId)) return;
            OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(_typeId);
            if (vo == null || vo.Stage <= 0) return;
            if (vo.FigureStage != vo.Stage)
            {
                OutWardController.Instance.WearIllusion(_typeId, 1, vo.Stage, 0);
                return;
            }
            if (vo.Stage != 1)
            {
                OutWardController.Instance.WearIllusion(_typeId, 1, 1, 0);
                return;
            }
            TipsManager.Toast("无法取消当前形象");
        }

        private void BindCrystalSlots()
        {
            if (crystal_group == null)
            {
                _crystalSlots = new PetRoundItemBind[0];
                return;
            }

            var slots = new List<PetRoundItemBind>();
            foreach (PetRoundItemBind slot in crystal_group.GetComponentsInChildren<PetRoundItemBind>(true))
            {
                if (slot == null || slot.gameObject == _tpl_PetRoundItem) continue;
                int index = slots.Count;
                slots.Add(slot);
                if (slot.click_group != null) UIUtil.AddClick(slot.click_group, () => OnCrystalSlot(index));
            }
            _crystalSlots = slots.ToArray();
        }

        private void ToggleAutoBuy()
        {
            if (_typeId != 1 && _typeId != 2) return;
            OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(_typeId);
            if (vo == null) return;
            OutWardController.Instance.SetAutoBuy(_typeId, vo.AutoBuy == 1 ? 0 : 1);
        }

        private void OnCrystalSlot(int index)
        {
            IReadOnlyList<int> goods = OutWardConfigs.GetCrystalGoodsIds(_typeId);
            if (index < 0 || index >= goods.Count) return;

            int goodsId = goods[index];
            int times = 0;
            int limit = 0;
            IReadOnlyList<(int goodsId, int times, int timesLim)> counters =
                OutWardModel.Instance.GetCrystalCounters(_typeId);
            if (counters != null)
            {
                for (int i = 0; i < counters.Count; i++)
                {
                    if (counters[i].goodsId != goodsId) continue;
                    times = counters[i].times;
                    limit = counters[i].timesLim;
                    break;
                }
            }

            // Old H5 only consumes directly in this state. Other states open PetCrystalView,
            // which is intentionally kept as a separate blocked leaf until its real detail/purchase UI exists.
            if (limit <= 0 || times >= limit || CountInBag(goodsId) <= 0)
            {
                ItemTipsView.Show(goodsId, CountInBag(goodsId));
                return;
            }
            OutWardController.Instance.UseCrystal(_typeId, goodsId);
        }

        private void BindPetEquipSlots()
        {
            if (_group_equip == null)
            {
                _petEquipSlots = new PetEquipOutItemBind[0];
                return;
            }

            var slots = new List<PetEquipOutItemBind>();
            foreach (PetEquipOutItemBind slot in _group_equip.GetComponentsInChildren<PetEquipOutItemBind>(true))
            {
                if (slot == null || slot.gameObject == _tpl_PetEquipOutItem) continue;
                slots.Add(slot);
                if (slot._Image1 != null)
                {
                    slot._Image1.raycastTarget = true;
                    UIUtil.AddClick(slot._Image1, OpenPetEquip);
                }
            }
            _petEquipSlots = slots.ToArray();
        }

        private void RefreshPetEquipEntry()
        {
            bool supported = _typeId == PetEquipController.TYPE_HORSE || _typeId == PetEquipController.TYPE_PARTNER;
            string outwardView = _typeId == PetEquipController.TYPE_HORSE ? "HorseComponentView" : "PartnerComponentView";
            bool open = supported
                && FuncOpenConfig.IsLoaded
                && FuncOpenConfig.CheckFuncOpenState("PetEquipBaseView")
                && FuncOpenConfig.CheckFuncOpenState(outwardView);

            if (bag_btn != null) bag_btn.gameObject.SetActive(open);
            if (_group_equip != null) _group_equip.gameObject.SetActive(open);
            if (!open || _petEquipSlots == null) return;

            PetEquipModel.PetEquipInfo info = PetEquipModel.Instance.Get(_typeId);
            for (int i = 0; i < _petEquipSlots.Length; i++)
            {
                PetEquipOutItemBind slot = _petEquipSlots[i];
                if (slot == null) continue;
                int posId = i + 1;
                PetEquipModel.PetEquipItem equipped = null;
                if (info?.Items != null)
                {
                    for (int j = 0; j < info.Items.Count; j++)
                    {
                        if (info.Items[j].PosId == posId) { equipped = info.Items[j]; break; }
                    }
                }

                slot.gameObject.SetActive(true);
                bool has = equipped != null && equipped.GoodsId > 0;
                if (slot._group_data != null) slot._group_data.gameObject.SetActive(has);
                if (slot._group_item != null) slot._group_item.gameObject.SetActive(has);
                if (slot._group_empty != null) slot._group_empty.gameObject.SetActive(!has);
                if (slot._img_icon != null) slot._img_icon.gameObject.SetActive(!has);
                if (slot._reddot != null) slot._reddot.gameObject.SetActive(false);

                EquipmentItemBind item = slot.GetComponentInChildren<EquipmentItemBind>(true);
                if (item == null) continue;
                item.gameObject.SetActive(has);
                if (!has) continue;

                if (item.grade != null) item.grade.text = equipped.Stage > 0 ? equipped.Stage + "阶" : "";
                if (item.stren != null) item.stren.text = equipped.PosLevel > 0 ? "+" + equipped.PosLevel : "";
                if (item.petEquipstage != null) item.petEquipstage.text = equipped.Star > 0 ? equipped.Star + "星" : "";
                if (item.num_text != null) item.num_text.text = "";
                string iconName = GoodsModel.GetGoodsIcon(equipped.GoodsTypeId);
                if (item.icon != null && !string.IsNullOrEmpty(iconName))
                    _ = ResManager.SetImageAsync(item.icon, GameResPath.GetGoodsIconPath(iconName), nativeSize: false);
            }
        }

        private void OnLvButton()
        {
            if (_levelMode)
            {
                OutWardModel.OutWardVo levelVo = OutWardModel.Instance.Get(_typeId);
                if (levelVo == null || !levelVo.HasLv)
                {
                    OutWardController.Instance.RequestLvPanel(_typeId);
                    return;
                }
                OutWardController.Instance.LvUp(_typeId);
                return;
            }
            OutWardModel.OneKeyState state = OutWardModel.Instance.GetOneKeyState(
                _typeId, RoleModel.Instance.Career, CountInBag);
            if (!state.CanSubmit)
            {
                if (state.ShouldOpenQuickBuy)
                {
                    int goodsId = 0;
                    for (int i = 0; i < state.Materials.Count; i++)
                        if (state.Materials[i].Type == 1 && state.Materials[i].GoodsId > 0)
                        { goodsId = state.Materials[i].GoodsId; break; }
                    if (goodsId > 0) QuickBuyFlow.Show(goodsId);
                    else GameLog.Warn("OutWard", "one-key quick-buy missing material type_id={0}", _typeId);
                }
                else
                    GameLog.Info("OutWard", "one-key blocked type_id={0} state={1}", _typeId, state.Availability);
                return;
            }
            if (_typeId == 1 || _typeId == 2)
            {
                OutWardController.Instance.StarUp(_typeId);
            }
            else
            {
                OutWardController.Instance.StarUpGeneric(_typeId);
            }
        }

        private void OpenPetEquip()
        {
            int typeId = _typeId;
            // 装备页与灵宠页同属 Window 层。先沿 Pet 的真实 Close 路径收掉内容模型/引导，
            // PetEquip 返回时再按原 type 恢复；避免仅被公共窗管理器隐藏外框后内容仍常驻。
            PetFlow.Close();
            PetEquipFlow.Open(typeId);
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

    /// <summary>复用 CommonModule.prefab 的 PropertyTipsView 展示 16002 attr_list。</summary>
    public static class OutWardPropertyFlow
    {
        private static GameObject _root;
        private static PropertyTipsViewBind _view;
        private static bool _loading;
        private static IReadOnlyList<(int attrId, long val)> _attrs;

        public static void Show(IReadOnlyList<(int attrId, long val)> attrs)
        {
            _attrs = attrs;
            _ = ShowAsync();
        }

        public static void Close()
        {
            if (_view != null && _view.IsShown) _view.Hide();
            if (_root != null) _root.SetActive(false);
        }

        private static async Task ShowAsync()
        {
            await GoodsModel.EnsureLoaded();
            if (!await EnsureViewAsync()) return;
            var text = new StringBuilder();
            if (_attrs != null)
            {
                for (int i = 0; i < _attrs.Count; i++)
                {
                    (int id, long value) = _attrs[i];
                    if (i > 0) text.AppendLine();
                    text.Append(GoodsModel.GetAttrName(id)).Append("  ").Append(GoodsModel.FormatAttrValue(id, value));
                }
            }

            if (_view._lb_title != null) _view._lb_title.text = "属性加成";
            if (_view._lb_attr != null) _view._lb_attr.text = text.Length == 0 ? "暂无属性" : text.ToString();
            if (_view._gp_none_conta != null) _view._gp_none_conta.gameObject.SetActive(text.Length == 0);
            _root.SetActive(true);
            _view.Show();
            _view.transform.SetAsLastSibling();
        }

        private static async Task<bool> EnsureViewAsync()
        {
            if (_root != null && _view != null) return true;
            if (_loading) return false;
            _loading = true;
            try
            {
                string key = GameResPath.GetUIPrefab("common", "CommonModule");
                _root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Popup));
                if (_root == null) return false;
                foreach (BaseView child in _root.GetComponentsInChildren<BaseView>(true)) child.gameObject.SetActive(false);
                _view = _root.GetComponentInChildren<PropertyTipsViewBind>(true);
                if (_view == null)
                {
                    GameLog.Error("OutWard", "CommonModule missing PropertyTipsViewBind");
                    ResManager.ReleaseInstance(_root);
                    _root = null;
                    return false;
                }

                if (_view._Image1 != null)
                {
                    _view._Image1.raycastTarget = true;
                    UIUtil.AddClick(_view._Image1, Close);
                }
                _root.SetActive(false);
                return true;
            }
            finally
            {
                _loading = false;
            }
        }
    }
}
