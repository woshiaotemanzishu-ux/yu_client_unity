using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Generated.UI.Login;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 用户协议/隐私保护指引独立详情面板。
    /// LoginUserAgreementView.prefab 是唯一视觉事实源；运行时只按渠道配置加载正文。
    /// </summary>
    [UIView("prefabs/ui/login/loginuseragreementview")]
    public sealed class LoginUserAgreementView : LoginUserAgreementViewBind
    {
        public const int TypeAgreement = 1;
        public const int TypePrivacy = 2;

        [Header("独立面板命中体")]
        public Image closeMask;

        private int _loadVersion;
        private LoginAgreementArgs _currentArgs = LoginAgreementArgs.Default;

        protected override void OnInit()
        {
            if (_img_close != null) _img_close.raycastTarget = true;
            if (closeMask != null) closeMask.raycastTarget = true;
        }

        protected override void OnShow(object args)
        {
            _currentArgs = ParseArgs(args);
            BindCloseClicks();

            bool privacy = _currentArgs.Type == TypePrivacy;
            if (_img_xieyi != null) _img_xieyi.gameObject.SetActive(!privacy);
            if (_img_privacy != null) _img_privacy.gameObject.SetActive(privacy);
            if (_lb_content != null) _lb_content.text = "正在加载…";
            if (_panel_content != null) _panel_content.verticalNormalizedPosition = 1f;

            // 本面板也会在登录模块已释放后由设置页首次动态创建。同步完成首帧布局，保证关闭图标和
            // 全屏关闭遮罩在 Open 返回时已经具备正确的射线区域，而不是等第二次打开才稳定。
            Canvas.ForceUpdateCanvases();
            if (transform is RectTransform root) LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Canvas.ForceUpdateCanvases();

            int version = ++_loadVersion;
            _ = LoadContentAsync(version, _currentArgs);
        }

        protected override void OnHide()
        {
            _loadVersion++;
            ClearCloseClicks();
        }

        private void BindCloseClicks()
        {
            ClearCloseClicks();
            AddCloseClick(_img_close);
            AddCloseClick(closeMask);
        }

        private void ClearCloseClicks()
        {
            UIUtil.ClearClicks(_img_close);
            UIUtil.ClearClicks(closeMask);
        }

        private void AddCloseClick(Graphic graphic)
        {
            if (graphic == null) return;
            UIUtil.AddClick(graphic, Hide);
        }

        private async Task LoadContentAsync(int version, LoginAgreementArgs args)
        {
            string suffixedKey = BuildConfigKey(args.Style, args.NameSuffix);
            JObject config = await LoadJsonAsync(suffixedKey);
            if (config == null && !string.IsNullOrWhiteSpace(args.NameSuffix))
            {
                string fallbackKey = BuildConfigKey(args.Style, string.Empty);
                GameLog.Warn("Login", "渠道协议配置缺失:{0}，回退 {1}", suffixedKey, fallbackKey);
                config = await LoadJsonAsync(fallbackKey);
            }

            if (version != _loadVersion || !IsShown) return;

            string sectionName = args.Type == TypePrivacy ? "privacy" : "agreenment";
            JObject section = config?[sectionName] as JObject;
            JArray content = section?["content"] as JArray;
            if (content == null)
            {
                GameLog.Error("Login", "协议配置缺段或正文:{0}.{1}", suffixedKey, sectionName);
                if (_lb_content != null) _lb_content.text = "协议内容加载失败，请稍后重试。";
                return;
            }

            var builder = new StringBuilder(content.Count * 32);
            for (int i = 0; i < content.Count; i++)
            {
                if (i > 0) builder.Append('\n');
                builder.Append(content[i]?.Value<string>() ?? string.Empty);
            }

            if (_lb_content != null)
            {
                _lb_content.text = builder.ToString();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_lb_content.rectTransform);
            }
            if (_panel_content != null)
            {
                Canvas.ForceUpdateCanvases();
                _panel_content.verticalNormalizedPosition = 1f;
                _panel_content.StopMovement();
            }
        }

        private static LoginAgreementArgs ParseArgs(object args)
        {
            switch (args)
            {
                case LoginAgreementArgs typed:
                    return typed;
                case int[] array when array.Length >= 2:
                    return new LoginAgreementArgs(array[0], array[1], string.Empty);
                case (int style, int type):
                    return new LoginAgreementArgs(style, type, string.Empty);
                default:
                    return LoginAgreementArgs.Default;
            }
        }

        private static string BuildConfigKey(int style, string suffix)
        {
            int normalizedStyle = style > 0 ? style : 2;
            string normalizedSuffix = string.IsNullOrWhiteSpace(suffix)
                ? string.Empty
                : "_" + suffix.Trim().TrimStart('_').ToLowerInvariant();
            return $"resource/config/client/configagreement{normalizedStyle}{normalizedSuffix}";
        }

        private static async Task<JObject> LoadJsonAsync(string key)
        {
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null) return null;
            try
            {
                return JObject.Parse(asset.text);
            }
            catch (System.Exception exception)
            {
                GameLog.Error("Login", "协议配置解析失败:{0} error={1}", key, exception.Message);
                return null;
            }
            finally
            {
                ResManager.Release(asset);
            }
        }
    }

    /// <summary>打开协议详情页所需的渠道选择参数。</summary>
    public sealed class LoginAgreementArgs
    {
        public static readonly LoginAgreementArgs Default = new LoginAgreementArgs(2,
            LoginUserAgreementView.TypeAgreement, "shenhai");

        public int Style { get; }
        public int Type { get; }
        public string NameSuffix { get; }

        public LoginAgreementArgs(int style, int type, string nameSuffix)
        {
            Style = style;
            Type = type == LoginUserAgreementView.TypePrivacy
                ? LoginUserAgreementView.TypePrivacy
                : LoginUserAgreementView.TypeAgreement;
            NameSuffix = nameSuffix ?? string.Empty;
        }
    }
}
