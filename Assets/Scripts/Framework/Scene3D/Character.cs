using Shenxiao.Framework.StateM;

namespace Shenxiao.Framework.Scene3D
{
    /// <summary>
    /// Base class for animated scene characters (Role / Monster / Npc).
    /// Drives state machine + animation + movement. Skeleton only in Phase 0.
    /// </summary>
    public class Character : SceneObj
    {
        public StateMachine FSM { get; } = new StateMachine();

        protected virtual void Update()
        {
            FSM.Update();
        }
    }
}
