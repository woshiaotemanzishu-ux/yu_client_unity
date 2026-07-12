using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Guild;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 申请列表单行(对标老客户端 guild/GuildApplyLookItem.ts):头像 + 名字 + 等级(&gt;370 显"神创N") +
    /// 战力 + 单条同意(_btn_pass→40009 type=1)/拒绝(_btn_refuse→40009 type=0)。
    /// </summary>
    public sealed class GuildApplyLookItem : GuildApplyLookItemBind
    {
        private GuildModel.ApplyEntry _data;

        protected override void OnInit()
        {
            BindClick(_btn_pass, () => { if (_data != null) GuildController.Instance.ApproveApply(_data.RoleId, 1); });
            BindClick(_btn_refuse, () => { if (_data != null) GuildController.Instance.ApproveApply(_data.RoleId, 0); });
        }

        public void SetData(GuildModel.ApplyEntry data)
        {
            _data = data;
            if (data == null) return;

            if (_lb_name != null) _lb_name.text = data.Name;
            if (_lb_level != null) _lb_level.text = (data.Level > 370 ? "神创" + (data.Level - 370) : data.Level.ToString()) + "级";
            if (_lb_fight != null) _lb_fight.text = data.CombatPower.ToString();
            _ = LoadHead(data);
        }

        private async System.Threading.Tasks.Task LoadHead(GuildModel.ApplyEntry data)
        {
            CustomHeadItem item = await EnsureHead(_playerHead);
            if (item == null || _data != data) return;
            item.SetRoleData(data.Figure?.career ?? 0, data.Figure?.turn ?? 0, data.Level, showLevel: false);
        }

        /// <summary>幂等:_playerHead 容器下已有 CustomHeadItem 直接复用,否则实例化(同 GuildMemberItem 套路)。</summary>
        private static async System.Threading.Tasks.Task<CustomHeadItem> EnsureHead(RectTransform container)
        {
            if (container == null) return null;
            CustomHeadItem existing = container.GetComponentInChildren<CustomHeadItem>(true);
            if (existing != null) return existing;

            GameObject go = await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "CustomHeadItem"), container);
            if (go == null) return null;
            go.name = "CustomHeadItem";
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
            return go.GetComponent<CustomHeadItem>();
        }

        private static void BindClick(UnityEngine.Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
