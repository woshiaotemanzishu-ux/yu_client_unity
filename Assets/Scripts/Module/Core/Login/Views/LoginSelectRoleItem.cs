using Shenxiao.Generated.UI.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 选角色项(对标老客户端 login/LoginSelectRoleItem.ts):角色名(_lb_name)+ 转生(_lb_turn)+ 等级(_lb_lv)+
    /// 神纹标(_img_sc)+ 选中底图(_img_bg/_img_bg2)。
    ///
    /// SetData(name, level, turn):>370 为转生段显示 level-370 + 神纹标。SetSelected(bool)。
    /// 降级:底图 skin(login ui_Login_02..06)待图标 → 文字即时,选中用底图透明度区分。列表项,由选角界面克隆。
    /// </summary>
    public sealed class LoginSelectRoleItem : LoginSelectRoleItemBind
    {
        public void SetData(string name, int level, int turn)
        {
            if (_lb_name != null) _lb_name.text = name ?? "";
            bool sc = level > 370;
            if (_lb_lv != null) _lb_lv.text = (sc ? level - 370 : level) + "级";
            if (_lb_turn != null) _lb_turn.text = turn + "转";
            if (_img_sc != null) _img_sc.gameObject.SetActive(sc);
        }

        public void SetSelected(bool selected)
        {
            // 选中底图 skin(ui_Login_02/05 vs 03/06)待图标;先用底图透明度区分
            if (_img_bg != null) _img_bg.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.7f);
        }
    }
}
