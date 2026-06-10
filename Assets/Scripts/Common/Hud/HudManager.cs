using Shenxiao.Framework.Util;

namespace Shenxiao.Common.Hud
{
    /// <summary>Overhead HUD (hp bar / name / title). Phase 0 skeleton.</summary>
    public static class HudManager
    {
        public static void Bind(int characterId) { GameLog.Debug("Hud", "bind char={0}", characterId); }
        public static void Unbind(int characterId) { GameLog.Debug("Hud", "unbind char={0}", characterId); }
        public static void UpdateHp(int characterId, int hp, int maxHp) { }
    }
}
