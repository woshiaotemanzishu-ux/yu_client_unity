using Shenxiao.Generated.UI.Elim;

namespace Shenxiao.Module.Core.Elim
{
    /// <summary>
    /// 消除方块格子(对标老客户端 elim/ElimBlockDisplay.ts):棋子容器(con_chess)+ 特效(effect_gp)+ buff 名,内放 ElimChessDisplay。
    ///
    /// SetBuff(name)。降级:棋盘数据(ElimGameMgr)未移植 → 棋子模板/特效隐藏、buff 名即时。由 ElimMainView 棋盘克隆。
    /// </summary>
    public sealed class ElimBlockDisplay : ElimBlockDisplayBind
    {
        protected override void OnInit()
        {
            if (_tpl_ElimChessDisplay != null) _tpl_ElimChessDisplay.SetActive(false);
            if (effect_gp != null) effect_gp.gameObject.SetActive(false);
        }

        public void SetBuff(string name)
        {
            if (buff_name != null) buff_name.text = name ?? "";
        }
    }
}
