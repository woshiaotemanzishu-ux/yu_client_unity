using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Generated.UI.Login;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 用户协议/隐私政策窗(对标老客户端 login/LoginUserAgreementView.ts)。
    ///
    /// Phase 2:结构(背景/关闭/标题页签/滚动文本)由快照烤进 prefab,这里【只绑数据】——
    /// 不再运行时 new 节点、不再摆位置。
    /// 入参 style(1/2/3)选配置文件 ConfigAgreement{style};type(1=用户协议 agreenment,2=隐私 privacy)
    /// 选数据段并切换 _img_xieyi / _img_privacy 页签;content[] 用 \n 拼成 _lb_content。
    ///
    /// 降级:老端 ClientConfig.agreement_name_suffix(渠道分包 ConfigAgreement2_xxx)未移植,
    /// 这里只读基础 ConfigAgreement{style};配置 JSON 尚未同步进 GameRes 时打错误日志、文本留空。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class LoginUserAgreementView : LoginUserAgreementViewBind
    {
        // 老端 type:1=用户协议(对应 cfg["agreenment"] + _img_xieyi),2=隐私政策(cfg["privacy"] + _img_privacy)
        private const int TYPE_AGREEMENT = 1;
        private const int TYPE_PRIVACY = 2;

        private int _style = 1;
        private int _type = TYPE_AGREEMENT;

        protected override void OnInit()
        {
            // 关闭按钮在 prefab 里已烤好,这里只绑点击(每次 OnShow 重绑前先清,见 OnShow)。
            if (_img_close != null) _img_close.raycastTarget = true;
        }

        protected override void OnShow(object args)
        {
            ParseArgs(args);

            // 防监听叠加:OnShow 可能多次,先清再加(对标样板 BindBakedCareers)。
            if (_img_close != null)
            {
                UIUtil.ClearClicks(_img_close);
                UIUtil.AddClick(_img_close, Hide);
            }

            // 页签互斥:用户协议显 _img_xieyi,隐私政策显 _img_privacy(对标 ShowMsg 里 visible 切换)。
            if (_img_xieyi != null) _img_xieyi.gameObject.SetActive(_type == TYPE_AGREEMENT);
            if (_img_privacy != null) _img_privacy.gameObject.SetActive(_type == TYPE_PRIVACY);

            ShowMsgAsync();
        }

        /// <summary>解析 Open 入参 [style, type](老端 Open(...params) 的 style/type)。</summary>
        private void ParseArgs(object args)
        {
            _style = 1;
            _type = TYPE_AGREEMENT;
            switch (args)
            {
                case (int style, int type):
                    _style = style;
                    _type = type;
                    break;
                case int[] arr when arr.Length >= 2:
                    _style = arr[0];
                    _type = arr[1];
                    break;
                case int single:
                    _style = single;
                    break;
            }
        }

        /// <summary>对标老端 ShowMsg:按 style 读 ConfigAgreement{style},按 type 取段,拼 content 进文本。</summary>
        private async void ShowMsgAsync()
        {
            // 老端按 style 1/2/3 分别读 ConfigAgreement1/2/3;2/3 还会拼 ClientConfig.agreement_name_suffix
            // 渠道后缀(ConfigAgreement2_xxx),该后缀配置未移植,这里只读基础表。
            string key = $"resource/config/client/configagreement{_style}";
            JObject cfg = await LoadJson(key);
            if (cfg == null) return; // LoadJson 内已打错误日志
            if (!gameObject.activeInHierarchy) return; // 加载期间已关页:丢弃

            // type 1→agreenment(注意老端字段拼写 "agreenment"),type 2→privacy。
            string section = _type == TYPE_PRIVACY ? "privacy" : "agreenment";
            if (!(cfg[section] is JObject agreement))
            {
                GameLog.Warn("Login", "ConfigAgreement{0} 缺段 {1}", _style, section);
                return;
            }

            if (!(agreement["content"] is JArray content))
            {
                GameLog.Warn("Login", "ConfigAgreement{0}.{1} 缺 content", _style, section);
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (JToken line in content)
            {
                sb.Append(line.Value<string>());
                sb.Append('\n');
            }
            if (_lb_content != null) _lb_content.text = sb.ToString();
        }

        /// <summary>读 Addressables JSON 配置为 JObject(对标 LoginConfigs.LoadJson 的范式)。</summary>
        private static async Task<JObject> LoadJson(string key)
        {
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Config", "协议配置缺失:{0}(需菜单 神霄/配表/同步客户端配置 同步进 GameRes)", key);
                return null;
            }
            JObject jo = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return jo;
        }
    }
}
