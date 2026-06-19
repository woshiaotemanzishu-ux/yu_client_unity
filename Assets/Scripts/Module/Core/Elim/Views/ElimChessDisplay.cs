using Shenxiao.Generated.UI.Elim;

namespace Shenxiao.Module.Core.Elim
{
    /// <summary>
    /// 消除棋子显示(对标老客户端 elim/ElimChessDisplay.ts):棋子图标(_chess_icon)+ buff 名(buff_name)+ 特效(effect_gp_1)。
    ///
    /// SetBuff(name)。降级:棋子图标/特效(ElimGameMgr 棋盘数据)未移植 → buff 名即时、特效隐藏、图标待接。
    /// 由 ElimBlockDisplay 克隆。
    /// </summary>
    public sealed class ElimChessDisplay : ElimChessDisplayBind
    {
        protected override void OnInit()
        {
            if (effect_gp_1 != null) effect_gp_1.gameObject.SetActive(false);
        }

        public void SetBuff(string name)
        {
            if (buff_name != null) buff_name.text = name ?? "";
        }
    }
}
