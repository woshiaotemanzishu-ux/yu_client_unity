using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Gm
{
    /// <summary>
    /// GM 秘籍(对标老客户端 CheatController/CheatModel + yu_server pp_gm.erl):
    /// 11100 拉取服务端下发的秘籍清单(分类/命令/参数描述/默认值,零硬编码),
    /// 11101 发送命令串("命令_参数_参数")。清单到达后发 EVT_GM_CHEAT_LIST。
    /// 入口是编辑器窗口「神霄/GM 秘籍」(Play 模式下用);打真机包不带入口,运行时类保留无害。
    /// </summary>
    public sealed class GmCheatController : BaseController
    {
        public sealed class GmCommand
        {
            public string Command = "";
            public string DisplayName = "";
            public string[] Args = System.Array.Empty<string>();
            public string[] Defaults = System.Array.Empty<string>();
        }

        public sealed class GmCategory
        {
            public string Name = "";
            public readonly List<GmCommand> Commands = new List<GmCommand>();
        }

        public static readonly GmCheatController Instance = new GmCheatController();
        private GmCheatController() { }

        private readonly List<GmCategory> _categories = new List<GmCategory>();
        public IReadOnlyList<GmCategory> Categories => _categories;

        protected override void Register()
        {
            RegisterProtocal(Proto.GM_CHEAT_LIST, OnCheatList);
        }

        /// <summary>拉取秘籍清单(需已连游戏服并 10000 登录)。</summary>
        public void RequestList()
        {
            Init(); // 容许窗口侧懒初始化
            SendFmt(Proto.GM_CHEAT_LIST);
            GameLog.Info("Gm", "请求 GM 秘籍清单(11100)");
        }

        /// <summary>发命令串:如 "lv_100"、"goods_36010001_10"、"setgmpassword_xxx"。</summary>
        public void SendCommand(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText)) return;
            Init();
            SendFmt(Proto.GM_CHEAT_EXEC, "s", commandText.Trim());
            GameLog.Info("Gm", "发送 GM 秘籍: {0}", commandText.Trim());
        }

        /// <summary>11100 回包:u16 分类数 × { s 分类名, u16 命令数 × { s, s, u16×s 参数, u16×s 默认值 } }。</summary>
        private void OnCheatList(NetReader reader)
        {
            _categories.Clear();
            int catCount = reader.ReadU16();
            for (int c = 0; c < catCount; c++)
            {
                var cat = new GmCategory { Name = reader.ReadString() };
                int cmdCount = reader.ReadU16();
                for (int i = 0; i < cmdCount; i++)
                {
                    var cmd = new GmCommand
                    {
                        Command = reader.ReadString(),
                        DisplayName = reader.ReadString(),
                    };
                    int argc = reader.ReadU16();
                    cmd.Args = new string[argc];
                    for (int a = 0; a < argc; a++) cmd.Args[a] = reader.ReadString();
                    int defc = reader.ReadU16();
                    cmd.Defaults = new string[defc];
                    for (int d = 0; d < defc; d++) cmd.Defaults[d] = reader.ReadString();
                    cat.Commands.Add(cmd);
                }
                _categories.Add(cat);
            }
            GameLog.Info("Gm", "GM 秘籍清单到达: {0} 个分类", _categories.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_GM_CHEAT_LIST);
        }
    }
}
