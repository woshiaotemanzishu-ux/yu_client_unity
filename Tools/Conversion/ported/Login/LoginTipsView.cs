using System.Collections.Generic;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 登录提示窗(对标老客户端 login/LoginTipsView.ts):标题(_lb_title)+ 提示列表 + 关闭(_img_close)。
    ///
    /// Phase 2:结构(背景/标题/滚动面板/提示项)由快照烤进 prefab,这里【只绑数据】——
    /// 老端的 UpdateView 是「不够就 new LoginTipsItem 塞进 _vbox_con」,现在改成
    /// 【遍历烤进 _vbox_con 的同名 LoginTipsItem 子节点,把 content[i] 绑到其内部 _lb_desc】,
    /// 不再 Instantiate 模板、不摆位置(布局由 prefab 的 VerticalLayoutGroup 负责)。
    /// content 沿用老端 TS 里的字面量数组(老端就是 View 内硬编码,不走 config/model)。
    /// 关闭按钮对标 InitEvent:点 _img_close → Close()(这里 Hide())。
    /// </summary>
    public sealed class LoginTipsView : LoginTipsViewBind
    {
        // 对标老端 LoginTipsView.ts 的 content 字面量数组(View 内硬编码,非配置)。
        private static readonly string[] Contents =
        {
            "（1）本游戏是一款3d角色扮演挂机类游戏，游戏内容适用于年满16周岁及以上的用户。",
            "（2）本游戏基于架空的故事背景和世界观，剧情简单且积极向上，没有基于真实历史和现实事件所改编的内容。游戏玩法基于玩家点击操作，鼓励玩家通过任务进行成长达成目标，游戏中有基于文字的社交系统。",
            "（3）游戏中有用户实名制认证系统，当认证为未成年人的用户将接受以下管理：",
            "游戏中部分玩法和道具需要付费，未满8周岁的用户不能付费；8周岁以上未满16周岁的未成年人用户，单次充值金额不得超过50元人民币，每月充值金额累计不得超过200元人民币；16周岁以上的未成年人用户，单次充值金额不得超过100元人民币，每月充值金额累计不得超过400元人民币。未成年人用户仅可在周五、周六、周日和法定节假日每日20时至21时登录游戏，其他时间无法登陆。",
            "（4）本游戏的类型为自动挂机功能，玩家可以通过挂机放置轻松获得收益，降低节约用户的时间成本和负担。游戏内有社交功能以及组队玩法，需要玩家间进行社交和互相帮助配合，有助于培养玩家的沟通以及团队协作能力。",
        };

        protected override void OnInit()
        {
            // 对标老端 InitEvent:关闭按钮。raycastTarget 由 UIUtil.AddClick 内部置 true。
            if (_img_close != null)
            {
                UIUtil.ClearClicks(_img_close);
                UIUtil.AddClick(_img_close, OnClickClose);
            }
        }

        protected override void OnShow(object args)
        {
            // 对标老端 open_callback → UpdateView:把每条 content 绑到烤好的提示项上。
            BindBakedTips();
        }

        protected override void OnHide()
        {
        }

        protected override void OnDispose()
        {
        }

        /// <summary>
        /// 把 content 绑到烤进 _vbox_con 的提示项(节点名 LoginTipsItem)上,不再 Instantiate 模板。
        /// 老端 UpdateView 按 content 数量动态创建项;这里烤制项数固定,按数据数显隐并赋文。
        /// </summary>
        private void BindBakedTips()
        {
            RectTransform con = VboxCon();
            if (con == null)
            {
                GameLog.Error("Login", "提示窗缺 _vbox_con(烤制 prefab 结构异常?)");
                return;
            }

            // 收集烤进 _vbox_con 的提示项(节点名 LoginTipsItem),按层级序。
            var items = new List<Transform>();
            for (int i = 0; i < con.childCount; i++)
            {
                Transform c = con.GetChild(i);
                if (c.name.StartsWith("LoginTipsItem")) items.Add(c);
            }
            if (items.Count < Contents.Length)
            {
                GameLog.Warn("Login", "烤制提示项 {0} 少于内容条数 {1}(内容变了需重烤)", items.Count, Contents.Length);
            }

            for (int i = 0; i < items.Count; i++)
            {
                Transform item = items[i];
                bool used = i < Contents.Length;
                item.gameObject.SetActive(used);
                if (!used) continue;

                // 对标 LoginTipsItem.ts:把 content 写进项内部的 _lb_desc。
                TMP_Text desc = FindText(item, "_lb_desc");
                if (desc != null) desc.text = Contents[i];
                else GameLog.Warn("Login", "提示项缺 _lb_desc(项[{0}])", i);
            }
        }

        /// <summary>对标老端 _img_close 点击 → Close()。</summary>
        private void OnClickClose()
        {
            Hide();
        }

        // ——— 容器字段没绑上时按名兜底(烤制 prefab 里容器节点名与老端一致)———
        private RectTransform VboxCon() => _vbox_con != null ? _vbox_con : transform.Find("_vbox_con") as RectTransform;

        private static TMP_Text FindText(Transform root, string path)
        {
            Transform t = root.Find(path);
            if (t == null) return null;
            TMP_Text self = t.GetComponent<TMP_Text>();
            return self != null ? self : t.GetComponentInChildren<TMP_Text>(true);
        }
    }
}
