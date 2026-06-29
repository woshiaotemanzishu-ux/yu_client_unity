using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.CustomActivity
{
    internal static class CustomActivityConfigs
    {
        private static JObject _customActivity;

        // 对标老端 CustomActivityDefine.SpecialIcon = {69,70}(CustomActivityDefine.ts:229-232):
        // 69=实名认证(NAMEVERIFY)、70=关注有礼(ATTENTION)。老端在通用 33101 图标路径(GetAddIconList)
        // 把它们标 controll_myself 而 AddActivityIcons 跳过(CustomActivityModel.ts:825-833/676),只由专属的
        // RefreshSpecialIcon 通道在叠加门禁(非 alpha + 实名奖励未领 + 平台实名 SDK + 逐平台 ClientNameVerifyConfig)
        // 满足时才添加。本端尚无这套平台基建,故同样不经通用路径添加——开发/新号态默认隐藏,与老端一致。
        // 将来移植实名功能时,改为专属 NameVerify 控制器镜像 RefreshSpecialIcon(69) 后再放开。
        private static readonly HashSet<int> SpecialIconBaseTypes = new HashSet<int> { 69, 70 };

        public static async Task EnsureLoaded()
        {
            if (_customActivity != null) return;

            TextAsset asset = await ResManager.LoadAsync<TextAsset>("resource/config/client/configcustomactivity");
            if (asset == null)
            {
                _customActivity = new JObject();
                GameLog.Error("CustomActivity", "configcustomactivity missing");
                return;
            }

            _customActivity = JObject.Parse(asset.text);
            ResManager.Release(asset);
        }

        public static async Task<string> ResolveIconTypeAsync(CustomActivityController.ActInfo info)
        {
            // SpecialIcon base type(实名认证 69 / 关注有礼 70):通用 33101 路径不添加(见 SpecialIconBaseTypes 注释)。
            if (SpecialIconBaseTypes.Contains(info.BaseType)) return null;

            // 逐渠道 source_list 过滤:对标老端 ActivityIconManager.addIcon 的 juage_play_act(331@77@0 / 331@14 /
            // 331@81@12 / 331@81@13)与 CustomActivityModel.CheckPlatSourceList(创角礼包 122)。配了非空 source_list
            // 且当前渠道不在其中 → 不添加;没配 / 空 source_list → 照常显示(见 IsBlockedByChannel)。
            if (IsBlockedByChannel(info)) return null;

            await MainUIConfigs.EnsureLoaded();
            await EnsureLoaded();

            string mapped = ResolveFromWindowsComponent(info);
            if (!string.IsNullOrEmpty(mapped)) return mapped;

            string fallback = info.BaseType == 101
                ? "331@100@" + info.ShowId
                : "331@" + info.BaseType + "@" + info.ShowId;
            return MainUIConfigs.GetFunctionIconCfg(fallback) != null ? fallback : null;
        }

        private static string ResolveFromWindowsComponent(CustomActivityController.ActInfo info)
        {
            if (!(_customActivity?["windowscomponent"] is JObject windows)) return null;
            string key = info.BaseType + "@" + info.ShowId;
            foreach (KeyValuePair<string, JToken> kv in windows)
            {
                if (!(kv.Value is JObject views)) continue;
                if (!(views[key] is JObject view)) continue;
                if (MainUIConfigs.GetFunctionIconCfg(kv.Key) == null) continue;
                if (!IsViewOpen(view)) continue;
                return kv.Key;
            }
            return null;
        }

        private static bool IsViewOpen(JObject view)
        {
            int level = RoleModel.Instance.HasBaseInfo ? RoleModel.Instance.Level : 0;
            int openDay = ServerTimeModel.GetOpenServerDay();
            if (openDay <= 0) openDay = 1;

            int openLv = ReadInt(view, "open_lv");
            int viewOpenDay = ReadInt(view, "open_day");
            if (level < openLv) return false;
            if (viewOpenDay > 0 && openDay < viewOpenDay) return false;
            return true;
        }

        // 需要按渠道 source_list 过滤的活动(对标老端 juage_play_act + 创角礼包 122)。
        // true = 该 ActInfo 命中过滤目标,需要校验 source_list。
        private static bool IsChannelFilterTarget(CustomActivityController.ActInfo info)
        {
            switch (info.BaseType)
            {
                case 77: return info.SubType == 1;                       // 331@77@0  GetActInfo(77,1)
                case 82: return info.ShowId == 1;                        // 331@14    GetActInfoByShowID(82,1)
                case 81: return info.ShowId == 12 || info.ShowId == 13;  // 331@81@12 / 331@81@13
                case 122: return true;                                   // 创角礼包 CREAT_ROLE_GIFT(331@3 / 331@3_1)
                default: return false;
            }
        }

        // true = 当前渠道被屏蔽(命中目标 且 source_list 非空 且 不含当前渠道)。
        // 语义对标老端 CheckPlatSourceList:没配 source_list / source_list 为空 → 不屏蔽(允许显示)。
        private static bool IsBlockedByChannel(CustomActivityController.ActInfo info)
        {
            if (!IsChannelFilterTarget(info)) return false;
            if (string.IsNullOrEmpty(info.Condition)) return false;

            ErlangTerm cond = ErlangParser.Parse(info.Condition);
            IReadOnlyList<ErlangTerm> tuples = cond?.Items;
            if (tuples == null) return false;

            string platName = LoginModel.Instance.PlatName;
            for (int i = 0; i < tuples.Count; i++)
            {
                IReadOnlyList<ErlangTerm> pair = tuples[i]?.Items;
                if (pair == null || pair.Count < 2) continue;
                if (pair[0].As<string>() != "source_list") continue;

                IReadOnlyList<ErlangTerm> sourceList = pair[1]?.Items;
                if (sourceList == null || sourceList.Count == 0) return false; // 空 → 允许
                for (int k = 0; k < sourceList.Count; k++)
                {
                    if (sourceList[k].As<string>() == platName) return false;  // 命中渠道 → 允许
                }
                return true; // 配了非空 source_list 但不含当前渠道 → 屏蔽
            }
            return false; // 无 source_list 元组 → 允许
        }

        private static int ReadInt(JObject obj, string key)
        {
            JToken token = obj?[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (int.TryParse(token.ToString(), out int value)) return value;
            return 0;
        }
    }
}
