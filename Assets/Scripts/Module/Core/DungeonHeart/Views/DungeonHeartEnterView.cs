using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.DungeonHeart;
using Shenxiao.Module.Core.Dungeon;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.DungeonHeart
{
    /// <summary>
    /// 镇魔天墟入口。只消费显式副本 id；Task 路由仍由所属模块后续接入，避免在本模块猜任务状态。
    /// </summary>
    public sealed class DungeonHeartEnterView : DungeonHeartEnterViewBind
    {
        public sealed class Args
        {
            public int DungeonId;
        }

        private int _dungeonId;
        private int _renderEpoch;

        public override UILayer Layer => UILayer.Popup;

        protected override void OnInit()
        {
            BindBtn(closeBtn, Hide);
            BindBtn(enterBtn, EnterDungeon);
        }

        protected override void OnShow(object args)
        {
            int epoch = ++_renderEpoch;
            _dungeonId = args is Args entryArgs ? entryArgs.DungeonId : 0;
            ResetDynamicState();
            _ = RenderAsync(epoch);
        }

        protected override void OnHide()
        {
            ++_renderEpoch;
            _dungeonId = 0;
        }

        private async Task RenderAsync(int epoch)
        {
            if (_dungeonId <= 0)
            {
                GameLog.Error("DungeonHeart", "entry requires an explicit dungeon id from the authoritative Task route");
                return;
            }

            await Task.WhenAll(
                DungeonConfigs.EnsureLoaded(),
                SkillPassiveConfigs.EnsureLoaded(),
                SkillConfigs.EnsureLoaded(),
                MonsterConfigs.EnsureLoaded());
            if (!IsCurrent(epoch)) return;

            if (DungeonConfigs.GetType(_dungeonId) != 31)
            {
                GameLog.Error("DungeonHeart", "reject non-heart dungeon id: {0}", _dungeonId);
                return;
            }

            if (enterBtn != null) enterBtn.gameObject.SetActive(true);
            await RenderBadgeAsync(epoch);
            if (!IsCurrent(epoch)) return;
            RenderBossText();
            await RenderSkillAsync(epoch);
        }

        private async Task RenderBadgeAsync(int epoch)
        {
            int badge = _dungeonId % 100;
            if (badge < 1 || badge > 6)
            {
                GameLog.Error("DungeonHeart", "no authoritative badge mapping for dungeon id: {0}", _dungeonId);
                return;
            }

            if (nameNum != null)
            {
                bool ok = await ResManager.SetImageAsync(nameNum,
                    GameResPath.GetIcon("dungeonHeart", "I_" + badge.ToString("00")), nativeSize: false);
                if (IsCurrent(epoch)) nameNum.gameObject.SetActive(ok);
            }
            if (!IsCurrent(epoch)) return;
            if (bg != null)
            {
                bool ok = await ResManager.SetImageAsync(bg,
                    GameResPath.GetIcon("dungeonHeart", "uihmzc_007"), nativeSize: false);
                if (IsCurrent(epoch)) bg.gameObject.SetActive(ok);
            }
        }

        private void RenderBossText()
        {
            int bossId = GetLegacyBossId(_dungeonId);
            MonsterConfigs.MonCfg boss = MonsterConfigs.Get(bossId);
            if (boss == null || string.IsNullOrEmpty(boss.Name))
            {
                GameLog.Error("DungeonHeart", "missing authoritative boss config: dungeon={0}, boss={1}",
                    _dungeonId, bossId);
                return;
            }

            // 老端 bossName 还包含 config_mon.lv；当前公共访问器未暴露该字段，不能用副本序号猜等级。
            if (monsterText != null)
            {
                monsterText.text = "击败镇魔天墟的" + boss.Name + "吧";
                monsterText.gameObject.SetActive(true);
            }
        }

        private async Task RenderSkillAsync(int epoch)
        {
            List<SkillPassiveConfigs.PassiveSkillCfg> configs =
                SkillPassiveConfigs.GetForCareer(RoleModel.Instance.Career);
            SkillPassiveConfigs.PassiveSkillCfg entry = null;
            foreach (SkillPassiveConfigs.PassiveSkillCfg config in configs)
            {
                if (config.DunId == _dungeonId)
                {
                    entry = config;
                    break;
                }
            }
            if (entry == null || !SkillConfigs.Has(entry.SkillId))
            {
                GameLog.Error("DungeonHeart", "missing career skill mapping: dungeon={0}, career={1}",
                    _dungeonId, RoleModel.Instance.Career);
                return;
            }

            string name = SkillConfigs.GetName(entry.SkillId);
            string desc = SkillConfigs.GetDescForLevel(entry.SkillId, 1);
            string icon = SkillConfigs.GetIconForLevel(entry.SkillId, 1);
            if (skillName != null)
            {
                skillName.text = name;
                skillName.gameObject.SetActive(!string.IsNullOrEmpty(name));
            }
            if (skillDes != null)
            {
                skillDes.text = desc;
                skillDes.gameObject.SetActive(!string.IsNullOrEmpty(desc));
            }
            if (skillIcon != null && !string.IsNullOrEmpty(icon))
            {
                bool ok = await ResManager.SetImageAsync(skillIcon,
                    GameResPath.GetSkillIcon(icon), nativeSize: false);
                if (IsCurrent(epoch)) skillIcon.gameObject.SetActive(ok);
            }
        }

        private void EnterDungeon()
        {
            if (_dungeonId <= 0 || DungeonConfigs.GetType(_dungeonId) != 31)
            {
                GameLog.Error("DungeonHeart", "blocked enter without a verified heart dungeon id: {0}", _dungeonId);
                return;
            }
            DungeonController.Instance.Enter(_dungeonId);
            Hide();
        }

        private void ResetDynamicState()
        {
            SetActive(enterBtn, false);
            SetActive(nameNum, false);
            SetActive(bossName, false);
            SetActive(bossCon, false);
            SetActive(_box_eff, false);
            SetActive(monsterText, false);
            SetActive(monsterText2, false);
            SetActive(skillIcon, false);
            SetActive(skillName, false);
            SetActive(skillDes, false);
        }

        private bool IsCurrent(int epoch) => IsShown && epoch == _renderEpoch;

        /// <summary>
        /// 当前老端 config_dungeon_ui_content.value1 的六条实际映射；缺少 Unity 同名配置时不外推其它 id。
        /// </summary>
        private static int GetLegacyBossId(int dungeonId)
        {
            switch (dungeonId)
            {
                case 31001: return 31001;
                case 31002: return 31002;
                case 31003: return 31003;
                case 31004: return 31004;
                case 31005: return 31005;
                case 31006: return 31006;
                default: return 0;
            }
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static void BindBtn(Component target, Action onClick)
        {
            if (target == null) return;
            Image image = target as Image;
            if (image == null) image = target.GetComponentInChildren<Image>(true);
            if (image == null)
            {
                GameLog.Error("DungeonHeart", "click target has no Image: {0}", target.name);
                return;
            }
            image.raycastTarget = true;
            UIUtil.AddClick(image, onClick);
        }
    }
}
