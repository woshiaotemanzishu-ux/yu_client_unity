namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 移动类型(对标老客户端 SceneConfig.ts 的 MOVE_TYPE 枚举)。
    /// 12001(S2C)移动广播的 move_flag 用它判别:当 move_flag != Normal 时,
    /// 协议体在 (x, y, role_id, move_flag) 之后还会跟 start_fly_x / start_fly_y
    /// (起飞点,用于轻功/瞬移/传送/跳跃等的表现);Normal 普通走路则没有这两个字段。
    /// 完整枚举以 yu_client h5/src/scene/SceneConfig.ts 的 MOVE_TYPE 为准。
    /// </summary>
    public static class MoveType
    {
        public const int Normal = 0;            // NORMOL_MOVE 普通走路(无起飞点)
        public const int Jump = 1;
        public const int Accelerate = 2;
        public const int DoubleJump = 3;
        public const int Rush = 4;              // 冲刺
        public const int Blink = 5;             // 瞬移
        public const int GravityStartJump = 6;
        public const int GravityJump = 7;
        public const int DoorJump = 8;          // 传送门跳转
        public const int PickDrop = 9;
        public const int JumpPoint = 10;
    }
}
