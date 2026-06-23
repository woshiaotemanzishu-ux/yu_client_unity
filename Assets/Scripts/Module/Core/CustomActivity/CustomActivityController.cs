using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;

namespace Shenxiao.Module.Core.CustomActivity
{
    public sealed class CustomActivityController : BaseController
    {
        public static readonly CustomActivityController Instance = new CustomActivityController();

        private readonly HashSet<string> _ownedIcons = new HashSet<string>();
        private int _applyVersion;

        private CustomActivityController() { }

        public struct ActInfo
        {
            public int BaseType;
            public int SubType;
            public int ActType;
            public int ShowId;
            public int Wlv;
            public string Name;
            public string Desc;
            public string Condition;
            public int StartTime;
            public int EndTime;
        }

        protected override void Register()
        {
            RegisterProtocal(Proto.CUSTOM_ACTIVITY_LIST, On33101);
        }

        public override void Dispose()
        {
            ClearOwnedIcons();
            base.Dispose();
        }

        public void RequestActivityList()
        {
            SendFmt(Proto.CUSTOM_ACTIVITY_LIST);
        }

        private void On33101(NetReader r)
        {
            int count = r.ReadU16();
            var list = new List<ActInfo>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new ActInfo
                {
                    BaseType = r.ReadU16(),
                    SubType = r.ReadU16(),
                    ActType = r.ReadU8(),
                    ShowId = r.ReadU16(),
                    Wlv = r.ReadU16(),
                    Name = r.ReadString(),
                    Desc = r.ReadString(),
                    Condition = r.ReadString(),
                    StartTime = (int)r.ReadU32(),
                    EndTime = (int)r.ReadU32(),
                });
            }

            _ = ApplyActivityIconsAsync(list, ++_applyVersion);
            GameLog.Info("CustomActivity", "33101 activity list: {0}", count);
        }

        private async Task ApplyActivityIconsAsync(List<ActInfo> list, int version)
        {
            var next = new Dictionary<string, int>();
            for (int i = 0; i < list.Count; i++)
            {
                ActInfo info = list[i];
                string iconType = await CustomActivityConfigs.ResolveIconTypeAsync(info);
                if (string.IsNullOrEmpty(iconType)) continue;
                next[iconType] = info.EndTime;
            }

            if (version != _applyVersion) return;

            var remove = new List<string>();
            foreach (string iconType in _ownedIcons)
            {
                if (!next.ContainsKey(iconType)) remove.Add(iconType);
            }
            for (int i = 0; i < remove.Count; i++)
            {
                _ownedIcons.Remove(remove[i]);
                ActivityIconManager.Instance.DeleteIcon(remove[i]);
            }

            foreach (KeyValuePair<string, int> kv in next)
            {
                _ownedIcons.Add(kv.Key);
                await ActivityIconManager.Instance.AddIconAsync(kv.Key, kv.Value);
            }
        }

        private void ClearOwnedIcons()
        {
            foreach (string iconType in _ownedIcons)
            {
                ActivityIconManager.Instance.DeleteIcon(iconType);
            }
            _ownedIcons.Clear();
        }
    }
}
