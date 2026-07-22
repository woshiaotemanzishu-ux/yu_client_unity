using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Setting;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝改名：复用老端 setting/SettingChangeNameView 的真实输入布局，协议为 18215(string)。</summary>
    [UIView("prefabs/ui/baby/babyrenameview")]
    public sealed class BabyRenameView : SettingChangeNameViewBind
    {
        private bool _pending;
        private bool _subscribed;
        private int _showVersion;

        protected override void OnInit()
        {
            if (InptextDisplay != null) InptextDisplay.characterLimit = 12;
            BindClick(confirmBtn, Submit);
            BindClick(cancleBtn, Hide);
            BindClick(_close_btn, Hide);
            EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
            _subscribed = true;
        }

        protected override void OnShow(object args)
        {
            _showVersion++;
            _pending = false;
            if (InptextDisplay != null) InptextDisplay.text = string.Empty;
            bool paid = BabyModel.Instance.Basic != null && BabyModel.Instance.Basic.IsChangeName;
            if (free_label != null) free_label.gameObject.SetActive(!paid);
            if (cost_conta != null) cost_conta.gameObject.SetActive(paid);
            if (extra != null) extra.gameObject.SetActive(false);
            if (icon2 != null) icon2.gameObject.SetActive(false);
            if (cost2 != null) cost2.gameObject.SetActive(false);
            if (paid) _ = RefreshCost(_showVersion);
            _ = BabyNameMask.EnsureLoaded();
        }

        protected override void OnHide()
        {
            _showVersion++;
            _pending = false;
        }

        protected override void OnDispose()
        {
            if (!_subscribed) return;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
            _subscribed = false;
        }

        private async void Submit()
        {
            if (_pending) return;
            string name = NormalizeName(InptextDisplay != null ? InptextDisplay.text : string.Empty);
            if (!IsValidLength(name)) { TipsManager.Toast("名称长度需为4-12个字符"); return; }
            int version = _showVersion;
            _pending = true;
            await BabyNameMask.EnsureLoaded();
            if (!_pending || version != _showVersion || !IsShown) return;
            if (BabyNameMask.Contains(name)) { _pending = false; TipsManager.Toast("内容有敏感词"); return; }
            BabyController.Instance.RequestRename(name);
            // 与老端一致：发送后立即关闭；关闭前的 pending 可阻止同帧重复点击。
            Hide();
        }

        private void OnBabyUpdate(int command)
        {
            if (command != Proto.BABY_RENAME) return;
            _pending = false;
            if (IsShown) Hide();
        }

        public static string NormalizeName(string value) => (value ?? string.Empty).Trim();

        public static bool IsValidLength(string value)
        {
            value = NormalizeName(value);
            if (string.IsNullOrEmpty(value)) return false;
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                count += (c >= 0x0001 && c <= 0x007e) || (c >= 0xff60 && c <= 0xff9f) ? 1 : 2;
            }
            return count >= 4 && count <= 12;
        }

        private async Task RefreshCost(int version)
        {
            await BabyValueConfigs.EnsureLoaded();
            if (version != _showVersion || !IsShown || BabyModel.Instance.Basic == null || !BabyModel.Instance.Basic.IsChangeName) return;
            if (cost1 != null) cost1.text = "X" + BabyValueConfigs.RenameCostNum;
        }

        private static void BindClick(UnityEngine.Component target, Action action)
        {
            if (target == null) return;
            Image image = target as Image ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }
    }

    internal static class BabyNameMask
    {
        private static readonly List<string> Words = new List<string>();
        private static readonly Regex Separators = new Regex(@"[\s\p{P}\p{S}]", RegexOptions.Compiled);
        private static Task _loading;

        internal static Task EnsureLoaded()
        {
            if (_loading == null) _loading = LoadAsync();
            return _loading;
        }

        internal static bool Contains(string name)
        {
            string normalized = Separators.Replace(name ?? string.Empty, string.Empty);
            for (int i = 0; i < Words.Count; i++) if (!string.IsNullOrEmpty(Words[i]) && normalized.Contains(Words[i])) return true;
            return false;
        }

        private static async Task LoadAsync()
        {
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetClientConfigPath("ConfigLanguageMask"));
            if (asset == null) return;
            try
            {
                Words.Clear();
                JArray root = JArray.Parse(asset.text);
                for (int i = 0; i < root.Count; i++) if (!string.IsNullOrEmpty(root[i]?.ToString())) Words.Add(root[i].ToString());
            }
            catch (Exception e) { GameLog.Warn("Baby", "parse ConfigLanguageMask failed: {0}", e.Message); }
            finally { ResManager.Release(asset); }
        }
    }
}
