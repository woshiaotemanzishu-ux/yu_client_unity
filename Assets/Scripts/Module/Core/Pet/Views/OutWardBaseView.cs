using System.Collections.Generic;
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
using Shenxiao.Module.Core.FunctionOpen;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.OutWard;
using Shenxiao.Module.Core.PetEquip;
using Shenxiao.Module.Core.Role;
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
        private UIModelStage _modelStage;
        private int _modelEpoch;
        private string _modelKey;

        /// <summary>切换培养对象(1=御风云骑/坐骑,2=剑魄同修/侍魂,3=翼影,4=古法符相,5=殒锋天刃,12=玄穹云披),
        /// PetFlow/RoleFlow 页签驱动。</summary>
        public void SetType(int typeId)
        {
            if (typeId <= 0) return;
            if (_typeId != typeId) ClearOutwardModel();
            _typeId = typeId;
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
            BindPetEquipSlots();
            Subscribe();
        }

        protected override void OnShow(object args)
        {
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
            if (this == null || !gameObject.activeInHierarchy) return;
            Refresh();
        }

        protected override void OnHide()
        {
            ClearOutwardModel();
            MainUIGuideManager.Instance.HideMainUiFinger(this);
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            DisposeOutwardModel();
            MainUIGuideManager.Instance.HideMainUiFinger(this);
        }

        private void OnDestroy()
        {
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
            EventDispatcher.On<int>(GlobalEvent.EVT_PET_EQUIP_UPDATE, OnPetEquipUpdate);
            EventDispatcher.On<int>(GlobalEvent.EVT_PET_EQUIP_BAG_UPDATE, OnPetEquipBagUpdate);
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
            EventDispatcher.Off<int>(GlobalEvent.EVT_PET_EQUIP_UPDATE, OnPetEquipUpdate);
            EventDispatcher.Off<int>(GlobalEvent.EVT_PET_EQUIP_BAG_UPDATE, OnPetEquipBagUpdate);
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
            OutWardModel.OutWardVo vo = OutWardModel.Instance.Get(_typeId);
            int career = RoleModel.Instance.Career;

            SetMaterials();
            SetCrystals();

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
            if (roleOutward)
            {
                SetBaseAppearanceState(vo);
                RefreshOutwardModel(vo, career);
            }
        }

        /// <summary>技能球(skill_group 烤入的 PetRoundItem 实例,对标老端 SetSkillData):16002 skill_list 有几个填几个,
        /// 图标经 config_skill lv_data.icon;没有的槽隐藏(不造假)。</summary>
        private void SetSkills(List<int> skills)
        {
            if (skill_group == null) return;
            PetRoundItemBind[] slots = skill_group.GetComponentsInChildren<PetRoundItemBind>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                bool has = skills != null && i < skills.Count;
                slots[i].gameObject.SetActive(has);
                if (!has || slots[i].icon == null) continue;
                string iconName = Skill.SkillConfigs.GetIconForLevel(skills[i], 1);
                if (string.IsNullOrEmpty(iconName)) continue;
                _ = ResManager.SetImageAsync(slots[i].icon, GameResPath.GetSkillIcon(iconName), nativeSize: false);
                if (slots[i].bottom_text != null) slots[i].bottom_text.text = "";
            }
        }

        /// <summary>培养材料(material_group 烤入的 BaseAwardItem 实例,对标老端材料区):config_mount_goods 该 type 的
        /// 物品按 id 序填前两格(图标 config_goods.goods_icon + 数量=背包持有);配置缺失槽位隐藏。</summary>
        private void SetMaterials()
        {
            if (material_group == null) return;
            IReadOnlyList<int> goods = OutWardConfigs.GetTrainGoodsIds(_typeId);
            Generated.UI.Common.BaseAwardItemBind[] slots =
                material_group.GetComponentsInChildren<Generated.UI.Common.BaseAwardItemBind>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                bool has = i < goods.Count;
                slots[i].gameObject.SetActive(has);
                if (!has) continue;
                int goodsId = goods[i];
                string iconName = Common.GoodsModel.GetGoodsIcon(goodsId);
                if (slots[i].icon != null && !string.IsNullOrEmpty(iconName))
                {
                    _ = ResManager.SetImageAsync(slots[i].icon, GameResPath.GetGoodsIconPath(iconName), nativeSize: false);
                }
                if (slots[i].num_text != null) slots[i].num_text.text = CountInBag(goodsId).ToString();
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
            PetRoundItemBind[] slots = crystal_group.GetComponentsInChildren<PetRoundItemBind>(true);
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
            if (showId <= 0 || !TryGetModelProfile(_typeId, out string module, out string prefix, out string fallback))
            {
                ClearOutwardModel();
                return;
            }

            string address = BuildModelAddress(module, showId);
            if (_modelKey == address) return;
            _modelStage?.ClearStage();
            _modelKey = address;
            int epoch = ++_modelEpoch;
            _ = LoadOutwardModelAsync(epoch, address, module, prefix, fallback, showId);
        }

        private async Task LoadOutwardModelAsync(int epoch, string address, string module,
            string prefix, string fallback, int showId)
        {
            GameObject prefab = await ResManager.LoadAsync<GameObject>(address);
            await ClientOutWardPosConfigs.EnsureLoaded();
            if (!this || epoch != _modelEpoch || _modelKey != address || !gameObject.activeInHierarchy || res == null)
                return;
            if (prefab == null)
            {
                GameLog.Warn("OutWard", "role outward model missing: type={0} address={1}", _typeId, address);
                _modelKey = null;
                return;
            }

            UiModelParameterConfigs.ModelParam mp = ClientOutWardPosConfigs.Get(prefix + "_" + showId, fallback);
            GameObject instance = Instantiate(prefab);
            if (!this || epoch != _modelEpoch || _modelKey != address || !gameObject.activeInHierarchy || res == null)
            {
                Destroy(instance);
                return;
            }

            if (_modelStage == null) _modelStage = new UIModelStage();
            _modelStage.EnableDragRotate(true);
            res.gameObject.SetActive(true);
            _modelStage.PlaceInstance(res, instance, mp.Scale, mp.Position, mp.Rotate);
            _ = EffectBinder.AttachAlways(instance, module, showId.ToString());
            _ = PlayOutwardIdleAsync(instance, module, showId);
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
            _modelKey = null;
            _modelStage?.ClearStage();
        }

        private void DisposeOutwardModel()
        {
            _modelEpoch++;
            _modelKey = null;
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
            BindDegrade(proptity_btn, "属性 PetProptityView");
            if (bag_btn != null)
            {
                bag_btn.raycastTarget = true;
                UIUtil.AddClick(bag_btn, () => PetEquipFlow.Open(_typeId));
            }
            // 幻化(IllusionBaseView):数据层已通(轮24 PI——OutWardController/OutWardModel 已落地
            // 16003/16006-16009/16020/16022/16027 全链 + 4 张幻化专属配表),UI 待烤(prefab/View 未搭建,
            // 本按钮仍是 BindDegrade 通用桩,点击只弹"待开放" toast)。
            BindDegrade(illusion_btn, "幻化 IllusionBaseView");
            BindDegrade(_Image14, "自动购买切换");
            BindDegrade(autoImg, "自动购买切换");
            BindDegrade(select_1, "培养线页签(当前页)");
            BindDegrade(select_2, "等级线页签 OutwardLvSystem");
            BindDegrade(btn_switch, "培养线/等级线切换 OutwardLvSystem");
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
                    UIUtil.AddClick(slot._Image1, () => PetEquipFlow.Open(_typeId));
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
