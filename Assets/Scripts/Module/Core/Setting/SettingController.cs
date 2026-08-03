using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 设置协议控制器(对标老客户端 commonController/SettingController.ts 的 10203/10210 部分;
    /// 10202 全量拉取在 GameStartController(进游戏门禁包),收包后落 SettingModel)。
    ///  - 10203 写回:h 条数 + 每条 c type/c subtype/c is_open;回包 error_code==1 → 缓存列表落地
    ///    (SettingModel.ApplyChanged),否则显码降级提示(ErrorCodeShow 未移植)。
    ///  - 10210 脱离卡死:发 "i" scene_id;回包 code!=1 显码(==1 服务端直接拉人,无需客户端处理)。
    /// </summary>
    public sealed class SettingController : BaseController
    {
        public static readonly SettingController Instance = new SettingController();
        private SettingController() { }

        /// <summary>在途 10203 请求队列(服务端按序回包,逐笔对应落地;单槽会在连发时错配响应)。</summary>
        private readonly Queue<KeyValuePair<int, List<KeyValuePair<int, int>>>> _pending =
            new Queue<KeyValuePair<int, List<KeyValuePair<int, int>>>>();
#if UNITY_EDITOR
        private static System.Func<byte[], bool> s_outboundIntercept;
#endif

        protected override void Register()
        {
            RegisterProtocal(Proto.SETTING_WRITE, On10203);
            RegisterProtocal(Proto.SETTING_FLEE, On10210);
            RegisterProtocal(Proto.SETTING_WX_SUBSCRIPTION_SWITCH, On11307);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            _pending.Clear();
            SettingModel.Reset();
            base.Dispose();
        }

        private void OnGameStart() { SettingModel.ClearWxSubscriptionSwitch(); RequestWxSubscriptionSwitch(); }
        public void RequestWxSubscriptionSwitch()
        {
            SendRequest(Proto.SETTING_WX_SUBSCRIPTION_SWITCH);
        }

        /// <summary>批量写设置(对标老端 PackageAndSend10203)。entries=(subtype,is_open)。</summary>
        public void SendSettingList(int type, List<KeyValuePair<int, int>> entries)
        {
            if (entries == null || entries.Count == 0) return;

            var fmt = new StringBuilder("h");
            var args = new object[1 + entries.Count * 3];
            args[0] = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                fmt.Append("ccc");
                args[1 + i * 3] = type;
                args[2 + i * 3] = entries[i].Key;
                args[3 + i * 3] = entries[i].Value;
            }

            _pending.Enqueue(new KeyValuePair<int, List<KeyValuePair<int, int>>>(type, entries));
            SendRequest(Proto.SETTING_WRITE, fmt.ToString(), args);
            GameLog.Info("Setting", "request 10203 写设置 {0} 项(type={1})", entries.Count, type);
        }

        /// <summary>单项写设置(对标老端 SendProtocal)。</summary>
        public void SendSetting(int type, int subtype, int isOpen)
        {
            SendSettingList(type, new List<KeyValuePair<int, int>>
            {
                new KeyValuePair<int, int>(subtype, isOpen),
            });
        }

        /// <summary>脱离卡死(对标老端 confirm_flee → 10210 "i" scene_id)。</summary>
        public void SendFlee(int sceneId)
        {
            if (sceneId <= 0) return;
            SendRequest(Proto.SETTING_FLEE, "i", sceneId);
            GameLog.Info("Setting", "request 10210 脱离卡死 scene={0}", sceneId);
        }

        private void SendRequest(int command, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            if (s_outboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(command, format, args);
                if (s_outboundIntercept(frame)) return;
            }
#endif
            SendFmt(command, format, args);
        }

        private void On10203(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            KeyValuePair<int, List<KeyValuePair<int, int>>> pending = _pending.Count > 0
                ? _pending.Dequeue()
                : default;
            if (errorCode == 1)
            {
                SettingModel.ApplyChanged(pending.Key, pending.Value);
                GameLog.Info("Setting", "10203 写设置成功({0} 项)", pending.Value?.Count ?? 0);
            }
            else
            {
                TipsManager.Toast("设置失败(" + errorCode + ")");
            }
        }

        private void On10210(NetReader r)
        {
            int code = (int)r.ReadU32();
            if (code != 1)
            {
                TipsManager.Toast("脱离卡死失败(" + code + ")");
                return;
            }
            GameLog.Info("Setting", "10210 脱离卡死成功(服务端拉人切场景)");
        }

        private void On11307(NetReader r) => SettingModel.ApplyWxSubscriptionSwitch(r.ReadU8());
    }
}
