using System.Collections.Generic;

namespace Shenxiao.Module.Core.FunctionOpen
{
    /// <summary>
    /// 功能开放达成奖励数据（对标老客户端 FunctionOpenModel，服务端 pt_138）。
    /// FinishState：功能 id → 状态（对标服务端 State，0/1/2…表示未达成/可领/已领之类）；UI 待验收时按它显示红点/领取按钮。
    /// 注意：这是"功能开放达成奖励"（138xx，服务器驱动），与客户端 UI 门禁配置 FuncOpenConfig 是两回事。
    /// </summary>
    public sealed class FunctionOpenModel
    {
        public static readonly FunctionOpenModel Instance = new FunctionOpenModel();
        private FunctionOpenModel() { }

        public readonly Dictionary<int, int> FinishState = new Dictionary<int, int>(); // id -> state

        public void SetAll(Dictionary<int, int> map)
        {
            FinishState.Clear();
            if (map == null) return;
            foreach (KeyValuePair<int, int> kv in map) FinishState[kv.Key] = kv.Value;
        }

        public void SetState(int id, int state) => FinishState[id] = state;

        public void AddNew(int id)
        {
            if (!FinishState.ContainsKey(id)) FinishState[id] = 0;
        }

        public void Clear() => FinishState.Clear();
    }
}
