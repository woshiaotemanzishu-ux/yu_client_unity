using Shenxiao.Generated.UI.Suitboss;

namespace Shenxiao.Module.Core.Boss.Views
{
    /// <summary>
    /// 协助伤害条目(自动循环 轮15a,对标老端 boss/BossHelpItem.ts):单条"谁在帮我打Boss+伤害占比"。
    /// 数据源是场景内实时战斗伤害追踪(老端 BossDamageItem.ts→UPDATE_ASSIST_DAMAGE 事件本地计算
    /// modulus=round(damage*100/total_damage)),不是 pt_404 协议字段——Unity 无 BossSceneManager 等价物
    /// (场景战斗运行时未接),本类只提供数据落位方法,消费方(战斗系统)TODO 接入后调 SetData 即可。
    /// </summary>
    public sealed class BossHelpItemView : BossHelpItemBind
    {
        public void SetData(string playerName, int modulusPercent)
        {
            if (_lb_name != null) _lb_name.text = playerName ?? "";
            if (_lb_ratio != null) _lb_ratio.text = modulusPercent + "%";
        }
    }
}
