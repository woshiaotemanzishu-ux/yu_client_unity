using Shenxiao.Generated.UI.UiComponent;
using UnityEngine;

namespace Shenxiao.Module.Core.UiComponent
{
    /// <summary>
    /// 头顶名字血条板(对标老客户端 scene/sceneboard/NameBoard.ts):单位头顶的 名字/称号/结社/勋章/VIP/血条 动态板。
    ///
    /// 老端绝大多数子件运行时按需 CreatChilds 动态创建 + 场景挂载(SceneManager/CameraManager 驱动),prefab 只有
    /// _gp_parent 容器 + BigBloodBar/ProgressBar2 两个模板。
    /// 降级:动态子件创建 + 场景挂载 + 称号/勋章/VIP 海量配置未移植 → 仅落地【血条】(克隆 _tpl_ProgressBar2):
    /// SetHpVisible/SetHp 可用;名字/称号等整套待场景系统移植。OnInit 隐藏两模板。
    /// </summary>
    public sealed class NameBoard : NameBoardBind
    {
        private ProgressBar2 _hpBar;

        protected override void OnInit()
        {
            if (_tpl_BigBloodBar != null) _tpl_BigBloodBar.SetActive(false);
            if (_tpl_ProgressBar2 != null) _tpl_ProgressBar2.SetActive(false);
        }

        /// <summary>显示/隐藏血条(对标 SetHpVisible:首次显示时从 _tpl_ProgressBar2 克隆)。</summary>
        public void SetHpVisible(bool visible)
        {
            if (visible && _hpBar == null && _tpl_ProgressBar2 != null && _gp_parent != null)
            {
                GameObject go = Instantiate(_tpl_ProgressBar2, _gp_parent);
                go.SetActive(true);
                _hpBar = go.GetComponent<ProgressBar2>();
            }
            if (_hpBar != null) _hpBar.gameObject.SetActive(visible);
        }

        /// <summary>设血量(对标 SetHp → hp_bar.SetValue)。</summary>
        public void SetHp(long hp, long maxHp)
        {
            if (_hpBar != null) _hpBar.SetValue(hp, maxHp);
        }
    }
}
