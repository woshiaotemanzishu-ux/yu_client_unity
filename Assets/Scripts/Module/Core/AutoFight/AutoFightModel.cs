using Shenxiao.Framework.Event;

namespace Shenxiao.Module.Core.AutoFight
{
    /// <summary>
    /// 自动战斗(挂机)状态(对标老端 autofight/AutoFightManager.ts 的开关子集)。
    /// 注意:这是「自动战斗 AutoFight(战斗中自动放技能/打怪)」,不是「自动闯关 AutoBrush(自动推关卡进度)」——
    /// 两者在老端是不同系统,本项目也分属不同 Model,切勿混用(见 AutoBrushModel)。
    ///
    /// 本轮只接最小开关:MainUISkillView 自动战斗按钮点击 → Toggle → 按钮皮肤切换。
    /// 真实自动战斗循环(寻敌/放技能/移动)与 cookie 记忆、临时手动模式(GetTempMode→uizjmgj_001a1)下一轮战斗链路接。
    /// </summary>
    public sealed class AutoFightModel
    {
        public const int AUTO_WEIGHT_CLOSE = 0;
        public const int AUTO_WEIGHT_NORMAL = 1;
        public const int AUTO_WEIGHT_TASK = 900;

        public static readonly AutoFightModel Instance = new AutoFightModel();
        private AutoFightModel() { }

        /// <summary>是否自动战斗中(对标 GetAutoFightState:auto_fight_weight>0)。</summary>
        public bool AutoFightState { get; private set; }

        public int AutoFightWeight { get; private set; }

        public bool AutoFindWayState { get; private set; }

        public int AutoFindWayWeight { get; private set; }

        /// <summary>临时手动模式(对标 GetTempMode)。AutoFightState 为真且 TempMode 为真 → 第三态皮肤 uizjmgj_001a1。
        /// 触发源(老端场景拖拽 1.5s 进、3s 后退)属场景系统未移植,本轮经 <see cref="SetTempMode"/> 暴露入口。</summary>
        public bool TempMode { get; private set; }

        /// <summary>对标 SetAutoFight:状态变化才发事件(EventName.UPDATE_AUTO_FIGHT_STATE)。显式开关清临时手动(对标老端 SetTempMode(false))。</summary>
        public void SetAutoFight(bool on)
        {
            SetAutoFightWeight(on ? AUTO_WEIGHT_NORMAL : AUTO_WEIGHT_CLOSE);
        }

        public void SetAutoFightWeight(int weight)
        {
            if (weight < AUTO_WEIGHT_CLOSE) weight = AUTO_WEIGHT_CLOSE;
            bool on = weight > AUTO_WEIGHT_CLOSE;
            if (AutoFightState == on && AutoFightWeight == weight && !TempMode) return;
            TempMode = false;
            AutoFightWeight = weight;
            AutoFightState = on;
            EventDispatcher.Emit(GlobalEvent.EVT_AUTO_FIGHT_STATE, AutoFightState);
        }

        public void SetAutoFindWay(bool on, int weight = AUTO_WEIGHT_NORMAL)
        {
            if (weight < AUTO_WEIGHT_CLOSE) weight = AUTO_WEIGHT_CLOSE;
            bool next = on && weight > AUTO_WEIGHT_CLOSE;
            if (AutoFindWayState == next && AutoFindWayWeight == weight) return;
            AutoFindWayState = next;
            AutoFindWayWeight = next ? weight : AUTO_WEIGHT_CLOSE;
        }

        /// <summary>战斗演出冻结闸:BOSS 入场「大妖来袭」横幅等演出期间,玩家与怪物的移动/寻路/自动攻击/朝向全部冻结。
        /// 与 <see cref="TempMode"/>(老端三态皮肤语义)区分——只服务于演出停顿,不发皮肤事件(故横幅期间自动战斗按钮不会闪成手动态)。
        /// 单一开关,多处读它:MainRoleAgent.Update(玩家移动)、AutoFightController.TryAutoAttack(自动攻击)、MonsterRenderer.UpdateViewPosition(怪跟位/朝向)。</summary>
        public bool CombatFreeze { get; private set; }

        public void SetCombatFreeze(bool on) => CombatFreeze = on;

        /// <summary>对标老端 AutoFightManager.SetTempMode:临时手动模式变化才发事件 EVT_AUTO_FIGHT_TEMP_MODE。
        /// 只在自动战斗中有意义(老端由场景拖拽计时进/退);本轮暴露入口供未来场景层调用,触发计时未移植(差异记录)。</summary>
        public void SetTempMode(bool on)
        {
            if (TempMode == on) return;
            TempMode = on;
            EventDispatcher.Emit(GlobalEvent.EVT_AUTO_FIGHT_TEMP_MODE);
        }

        /// <summary>对标 ResponseAutoBtnClick 的状态翻转,返回翻转后的状态。</summary>
        public bool Toggle()
        {
            SetAutoFight(!AutoFightState);
            return AutoFightState;
        }

        public void Reset()
        {
            // 必须走 SetAutoFightWeight 让 EVT_AUTO_FIGHT_STATE(false) 正常发出、攻击环被 StopLoop 收编。
            // 旧实现直改字段是"静默翻状态"的合法入口,曾造成 state=true 而环死 / state=false 而环空转的分叉
            // (任务杀怪永动死循环成因之一:Reset 落在首次点火后,静默灭状态,后续同值 SetAutoFightWeight 早退不发事件)。
            SetAutoFightWeight(AUTO_WEIGHT_CLOSE);
            AutoFindWayState = false;
            AutoFindWayWeight = AUTO_WEIGHT_CLOSE;
            TempMode = false;
            CombatFreeze = false;
        }
    }
}
