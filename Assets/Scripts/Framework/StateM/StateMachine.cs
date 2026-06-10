using System.Collections.Generic;

namespace Shenxiao.Framework.StateM
{
    /// <summary>
    /// Generic state machine. Use enum-like int state ids.
    /// </summary>
    public class StateMachine
    {
        public delegate void StateHandler();

        private readonly Dictionary<int, StateHandler> _onEnter = new Dictionary<int, StateHandler>();
        private readonly Dictionary<int, StateHandler> _onExit = new Dictionary<int, StateHandler>();
        private readonly Dictionary<int, StateHandler> _onUpdate = new Dictionary<int, StateHandler>();

        public int Current { get; private set; } = -1;

        public void Register(int state, StateHandler onEnter = null, StateHandler onExit = null, StateHandler onUpdate = null)
        {
            if (onEnter != null) _onEnter[state] = onEnter;
            if (onExit != null) _onExit[state] = onExit;
            if (onUpdate != null) _onUpdate[state] = onUpdate;
        }

        public void Switch(int state)
        {
            if (Current == state) return;
            if (Current >= 0 && _onExit.TryGetValue(Current, out var exit)) exit();
            Current = state;
            if (_onEnter.TryGetValue(state, out var enter)) enter();
        }

        public void Update()
        {
            if (Current >= 0 && _onUpdate.TryGetValue(Current, out var upd)) upd();
        }
    }
}
