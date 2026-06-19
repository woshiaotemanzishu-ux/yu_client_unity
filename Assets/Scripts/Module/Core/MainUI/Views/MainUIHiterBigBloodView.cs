using Shenxiao.Generated.UI.MainUI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// BOSS/大怪 大血条(对标老客户端 MainUIHiterBigBloodView.ts):攻击 BOSS/大怪/玩家时右侧弹出,
    /// 显示名字 + 血量 + 血条(老端为 10 段条带 + 明暗双条补间动画)。
    ///
    /// 降级:数据源(hiter_vo 的 hp/maxHp、Config.config_mon 怪物配置、BOSS 伤害/掉落事件、CustomHeadItem
    /// 头像、10 段血条 + Runner 补间、怒气/体力额外信息、MainUIManager 移动组件)均未移植 →
    /// 提供 SetData(BigBloodData) 显示名字 + 血量文本 + 单段比例血条;条带/头像/掉落/怒气/体力先隐藏,
    /// 不补间。事件驱动弹层(打 BOSS 时开),默认关闭、不进 FirstPass;场景战斗+怪物配置移植后再补
    /// 条带 + 明暗补间 + 头像 + 掉落信息。
    /// </summary>
    public sealed class MainUIHiterBigBloodView : MainUIHiterBigBloodViewBind
    {
        // 对标老端 Blood_Max_Len=204(单段血条满宽)。
        private const float BLOOD_MAX_LEN = 204f;

        protected override void OnInit()
        {
            HideExtras();
        }

        protected override void OnShow(object args)
        {
            if (args is BigBloodData data) SetData(data);
        }

        /// <summary>填名字 + 血量 + 比例血条(单段降级版,无 10 段条带与补间)。</summary>
        public void SetData(BigBloodData data)
        {
            if (data == null) return;
            if (_lb_name != null) _lb_name.text = data.Name ?? "";
            if (_lb_hp != null) _lb_hp.text = FormatHp(data.Hp, data.MaxHp);

            float ratio = data.MaxHp > 0 ? Mathf.Clamp01((float)((double)data.Hp / data.MaxHp)) : 0f;
            SetBarWidth(_img_dark_blood, BLOOD_MAX_LEN * ratio);
            SetBarWidth(_img_light_blood, BLOOD_MAX_LEN * ratio);

            HideExtras();
        }

        /// <summary>未移植的额外信息(条带/头像/掉落/怒气/体力)先隐藏,不伪造。</summary>
        private void HideExtras()
        {
            HideNode(_img_next_blood);
            HideNode(_lb_left_blood);
            HideNode(_lb_anger);
            HideNode(_lb_vit);
            HideNode(drop_gp);
            HideNode(_box_head);   // 角色头像 CustomHeadItem 容器
            HideNode(_img_head);   // 怪物头像(待 GetMonsterHeadPath 移植)
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }

        private static void SetBarWidth(Image img, float width)
        {
            if (img == null) return;
            Vector2 size = img.rectTransform.sizeDelta;
            size.x = Mathf.Max(0f, width);
            img.rectTransform.sizeDelta = size;
        }

        /// <summary>对标老端 FormatHp:max>10w 时用 "w" 缩写。纯逻辑,直接移植。</summary>
        private static string FormatHp(long now, long max)
        {
            if (now < 0) now = 0;
            if (max > 100000)
            {
                string nowStr = now > 10000 ? now.ToString() : Round1(now / 10000.0) + "w";
                string maxStr = Round1(max / 10000.0) + "w";
                return nowStr + "/" + maxStr;
            }
            return now + "/" + max;
        }

        /// <summary>保留 1 位小数(对标老端 GetPreciseDecimal(v,1))。</summary>
        private static string Round1(double v)
        {
            return (Mathf.Floor((float)(v * 10.0)) / 10f).ToString();
        }
    }

    /// <summary>大血条最小数据(待 hiter_vo / 怪物配置 / 场景战斗移植后填充)。</summary>
    public sealed class BigBloodData
    {
        public string Name;
        public long Hp;
        public long MaxHp;
    }
}
