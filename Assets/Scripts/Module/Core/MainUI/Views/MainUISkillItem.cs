using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 技能按钮项(对标老客户端 MainUISkillItem.ts):图标 + 锁定态 + CD 时钟遮罩/倒计时 + 点击释放技能。
    ///
    /// 接真实 <see cref="SkillVo"/>(id/level/图标来自 21002 + config_skill):
    ///   · 图标:show_icon = level==0 ? GetIcon(1) : GetIcon()(对标 UpdateItem),走 ResManager.SetImageAsync + GameResPath.GetSkillIcon。
    ///   · 锁态:level==0 → 显示 lock 遮罩、点击不发事件(对标 UpdateLockState)。
    ///   · CD(对标老端 CirCleCdView:MainUISkillItem.ts:93-99 挂 size38/font21):老端是运行时 drawPie 的黑色
    ///     0.8 透明扇形(clock-wipe,从 12 点顺时针随 CD 消退)+ 白字倒计时(&gt;1s 取整、≤1s 一位小数),帧驱动;
    ///     数据源 SkillVo.getCd/GetLeftCD。本端等价:Image Filled/Radial360/Top 顺时针 + TMP 文本,Update 轮询
    ///     <see cref="SkillManager.GetCdLeftMs"/>(CD 起点=SceneCombat.ReleaseMainSkill → ResetSkill,自动/手动同路,
    ///     对标老端 FightMovieInfo 预播即 ResetSkill)。老端遮罩是圆形 pie、本端是方形图标 radial(图标本就方形,
    ///     视觉等价);僵直不显遮罩、CD 结束无闪光(老端 ActiveSkill 为空实现)——同老端。
    ///   · 点击:仅服务端 13017 托管态提示并拦截；普通 AutoFight 仍允许手动释放；CD 中不发
    ///     (对标老端 cd_mask 挡点)；否则发 EVT_SKILL_SHORTCUT_CLICK。
    /// 克隆件不经 Show→OnInit 不自动跑,Bind 字段序列化即就绪 → 点击绑定在 SetData 内幂等兜底。
    /// </summary>
    public sealed class MainUISkillItem : MainUISkillItemBind
    {
        private SkillVo _vo;
        private bool _clickBound;
        private Image _cdMask;              // 时钟遮罩(运行时建,对标老端 shape_mask drawPie)
        private TextMeshProUGUI _cdLabel;   // 倒计时文本(对标 _lb_cd)
        private static TMP_FontAsset _cdFont;
        private static Material _cdFontMat;

        /// <summary>由父 MainUISkillView 克隆后调用,填真实技能数据(对标 SetData → UpdateItem)。</summary>
        public void SetData(SkillVo vo)
        {
            _vo = vo;
            EnsureClickBound();

            if (vo == null)
            {
                if (icon != null) icon.enabled = false;
                UpdateLock(false);
                return;
            }

            if (icon != null)
            {
                icon.enabled = true;
                _ = ResManager.SetImageAsync(icon, GameResPath.GetSkillIcon(vo.DisplayIcon), nativeSize: false);
            }
            UpdateLock(vo.Locked);
        }

        /// <summary>对标 UpdateLockState:锁住显示 lock 遮罩。</summary>
        public void UpdateLock(bool locked)
        {
            if (@lock != null) @lock.gameObject.SetActive(locked);
        }

        // SetPosition(Laya 坐标→anchoredPosition 换算)已删:布局改【槽位式】,克隆体由
        // MainUISkillView.PlaceIconInSlot 撑满所在槽,槽位在 HudSkillBar.prefab 的 SkillIconGrid 下。

        // 老端点击绑在整个 con。Unity 转换产物的 bg/icon/lock 默认都会接收 Raycast；若只把 Button
        // 挂在 bg，位于它上方的 icon（以及与 con 同级的 lock）会先命中，事件无法走到 bg。
        // 因此把所有装饰 Graphic 关闭 Raycast，再由 con 上唯一的透明命中面接收整块技能点击。
        private void EnsureClickBound()
        {
            if (_clickBound || con == null) return;

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }

            UIUtil.AddClick(con, OnClickSkill);
            _clickBound = true;
        }

        private void OnClickSkill()
        {
            if (_vo == null) return;

            // 锁住(未学,level==0)不可点(对标 UpdateLockState:con.mouseEnabled=!lock)。
            if (_vo.Locked)
            {
                GameLog.Info("MainUI", "技能 {0} 未解锁(level=0),点击无效", _vo.Id);
                return;
            }

            // 老端只拦服务端 13017 下发的真正托管态；普通 AutoFight 开启时仍允许手点技能。
            if (RoleModel.Instance.DepositState)
            {
                TipsManager.Toast("自动战斗中");
                GameLog.Info("MainUI", "托管态(deposit_state)中,手点技能 {0} 拦截", _vo.Id);
                return;
            }

            // CD 中不发(对标老端 cd_mask 盖住期间点击无效)。
            if (SkillManager.Instance.GetCdLeftMs(_vo.Id) > 0)
            {
                return;
            }

            EventDispatcher.Emit(GlobalEvent.EVT_SKILL_SHORTCUT_CLICK, _vo.Id, SkillManager.ONLY_FIRE_ATTACK);
        }

        // ── CD 时钟遮罩 + 倒计时(帧驱动轮询,对标老端 CirCleCdView 每 3 帧 Update 重画扇形) ──────────

        private void Update()
        {
            RefreshCd();
        }

        private void RefreshCd()
        {
            int left = _vo != null ? SkillManager.Instance.GetCdLeftMs(_vo.Id) : 0;
            if (left <= 0)
            {
                if (_cdMask != null && _cdMask.gameObject.activeSelf)
                {
                    _cdMask.gameObject.SetActive(false); // 归零清遮罩+文本(对标 CirCleCdView DrawMaskCircle(0))
                }
                return;
            }

            EnsureCdNodes();
            if (_cdMask == null) return;
            if (!_cdMask.gameObject.activeSelf) _cdMask.gameObject.SetActive(true);

            int total = SkillManager.Instance.GetCdTotalMs(_vo.Id);
            _cdMask.fillAmount = total > 0 ? Mathf.Clamp01((float)left / total) : 0f;

            if (_cdLabel != null)
            {
                // 对标老端 _lb_cd:>1s 取整(ceil),≤1s 一位小数。
                float leftSec = left / 1000f;
                _cdLabel.text = leftSec > 1f ? Mathf.CeilToInt(leftSec).ToString() : leftSec.ToString("0.0");
            }
        }

        /// <summary>运行时搭 CD 遮罩/文本(老端同样是运行时 drawPie,布局里的 _img_mask 也被隐藏,无独立遮罩贴图)。</summary>
        private void EnsureCdNodes()
        {
            if (_cdMask != null || icon == null) return;

            var maskGo = new GameObject("CdMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            maskGo.transform.SetParent(icon.rectTransform, false);
            var maskRt = (RectTransform)maskGo.transform;
            maskRt.anchorMin = Vector2.zero;
            maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = Vector2.zero;
            maskRt.offsetMax = Vector2.zero; // 精确盖住图标(老端 SetPosition(3.5,4.5) 同位)

            _cdMask = maskGo.GetComponent<Image>();
            _cdMask.sprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f), 100f);
            _cdMask.color = new Color(0f, 0f, 0f, 0.8f);        // 对标老端 shape_mask 黑色 alpha=0.8
            _cdMask.type = Image.Type.Filled;
            _cdMask.fillMethod = Image.FillMethod.Radial360;    // 对标 drawPie:12 点起顺时针 clock-wipe
            _cdMask.fillOrigin = (int)Image.Origin360.Top;
            _cdMask.fillClockwise = true;
            _cdMask.raycastTarget = false;

            var labelGo = new GameObject("CdLabel", typeof(RectTransform));
            labelGo.transform.SetParent(maskRt, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            _cdLabel = labelGo.AddComponent<TextMeshProUGUI>();
            _cdLabel.alignment = TextAlignmentOptions.Center;
            _cdLabel.fontSize = 21f;                             // 对标老端 SetData(..., 38, 21) 的字号
            _cdLabel.color = Color.white;
            _cdLabel.raycastTarget = false;
            _cdLabel.textWrappingMode = TextWrappingModes.NoWrap;
            ApplyCdFont(_cdLabel);
        }

        // 复用场景已有 TMP 字体(同 MonsterRenderer 名牌约定,避免豆腐块;数字字形任何字体都有)。
        private static void ApplyCdFont(TextMeshProUGUI t)
        {
            if (_cdFont == null)
            {
                TextMeshProUGUI src = Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (src != null) { _cdFont = src.font; _cdFontMat = src.fontSharedMaterial; }
            }
            if (_cdFont != null) t.font = _cdFont;
            if (_cdFontMat != null) t.fontSharedMaterial = _cdFontMat;
        }
    }
}
