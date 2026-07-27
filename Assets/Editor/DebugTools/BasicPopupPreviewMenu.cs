using System.Threading.Tasks;
using Shenxiao.Module.Core.FunctionOpen;
using Shenxiao.Module.Core.Skill;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>基础弹层的运行态预览入口；读取当前角色真实技能与配置，不构造业务假数据。</summary>
    public static class BasicPopupPreviewMenu
    {
        [MenuItem("神霄/调试/UI弹层/预览获得技能", priority = 130)]
        private static async void PreviewObtainedSkill()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览获得技能", "请先进入 Play Mode 并登录到主界面。", "确定");
                return;
            }

            await PreviewObtainedSkillAsync();
        }

        private static async Task PreviewObtainedSkillAsync()
        {
            await SkillConfigs.EnsureLoaded();

            SkillVo selected = null;
            var shortcuts = SkillManager.Instance.ShortcutList;
            for (int i = 0; i < shortcuts.Count; i++)
            {
                SkillVo vo = shortcuts[i];
                if (vo != null && !vo.Locked && !SkillConfigs.IsNormal(vo.Id))
                {
                    selected = vo;
                    break;
                }
            }

            if (selected == null)
            {
                foreach (SkillVo vo in SkillManager.Instance.AllSkills)
                {
                    if (vo != null && !vo.Locked && !SkillConfigs.IsNormal(vo.Id))
                    {
                        selected = vo;
                        break;
                    }
                }
            }

            if (selected == null)
            {
                EditorUtility.DisplayDialog("预览获得技能", "当前角色还没有可用于预览的已学主动技能。", "确定");
                return;
            }

            FunctionOpenAutoFlow.PreviewSkill(selected.Id, selected.Level);
        }
    }
}
