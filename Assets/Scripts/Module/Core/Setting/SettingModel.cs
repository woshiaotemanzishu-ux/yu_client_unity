using System.Collections.Generic;
using Shenxiao.Common.Audio;
using Shenxiao.Common.Prefs;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 设置数据(对标老客户端 commonModel/SettingModel.ts):setting_data[type][subtype]=is_open,
    /// 服务器权威 —— 10202 全量拉取(GameStartController 收包后 Apply10202)、10203 写回成功后 ApplyChanged。
    /// 音量两项落地时同步 AudioManager + PrefsManager 本地镜像(对标老端 onBlockSettingChange → SoundManager)。
    ///
    /// ⚠命名错位陷阱(照抄老端实际行为,以玩家看到的滑条标签为准):
    ///   subtype 9(sound_open) = 「音效」滑条 → SFX 音量(老端 slider_audio → ChangeVolumeEffect);
    ///   subtype 12(sound_effect_open) = 「音乐」滑条 → 背景音乐音量(老端 slider_music → ChangeVolume)。
    /// config_setting.json 里的 name(9=音乐开关/12=音效开关)与视图标签相反,是老项目遗留错位,勿按 name 接。
    /// </summary>
    public static class SettingModel
    {
        // 对标老端 SettingModel.SettingType。
        public const int TYPE_CHANNEL = 1;
        public const int TYPE_AUDIO_AUTO_PLAY = 2;
        public const int TYPE_SYS_SETTING = 3;
        public const int TYPE_AUTO_SMELT = 4;

        // 对标老端 SettingModel.BlockSubType(ClientBlockConfig.json BlockSubType 同源)。
        public const int SUB_SPRITE = 1;
        public const int SUB_WING = 2;
        public const int SUB_SHENGQI = 3;
        public const int SUB_LIVENESS = 5;
        public const int SUB_SAME_SCREEN_ROLE_NUM = 6;
        public const int SUB_EFFECT_NUM = 7;
        public const int SUB_SIMPLE_MODE = 8;
        public const int SUB_SOUND_OPEN = 9;          // 「音效」滑条(见类注释错位说明)
        public const int SUB_WEAPON = 10;
        public const int SUB_GUARD = 11;
        public const int SUB_SOUND_EFFECT_OPEN = 12;  // 「音乐」滑条(见类注释错位说明)
        public const int SUB_FLOWER = 13;
        public const int SUB_DEMON = 14;
        public const int SUB_AUTO_BLUE = 17;
        public const int SUB_AUTO_PURPLE = 18;
        public const int SUB_AUTO_ORANGE = 19;
        public const int SUB_PARTNER = 20;
        public const int SUB_AUTO_TASK = 21;
        public const int SUB_SHIELD_CHANNEL = 22;
        public const int SUB_WX_BOARD = 24;
        public const int SUB_BACK = 25;
        public const int SUB_SHOCK_SCREEN = 26;
        public const int SUB_GODBEFALL = 201;
        public const int SUB_AUTO_HORSE = 202;

        public const string PREF_MUSIC_VOLUME = "setting.musicVolume";
        public const string PREF_SFX_VOLUME = "setting.sfxVolume";

        private static readonly Dictionary<int, Dictionary<int, int>> _data =
            new Dictionary<int, Dictionary<int, int>>();

        public static bool HasWxSubscriptionSwitch { get; private set; }
        public static byte WxSubscriptionSwitchRaw { get; private set; }
        public static bool WxSubscriptionSwitchEnabled => WxSubscriptionSwitchRaw == 1;
        public static void ApplyWxSubscriptionSwitch(byte raw) { WxSubscriptionSwitchRaw = raw; HasWxSubscriptionSwitch = true; }
        public static void ClearWxSubscriptionSwitch() { HasWxSubscriptionSwitch = false; WxSubscriptionSwitchRaw = 0; }

        /// <summary>某 type 的设置块是否已从服务器到达(10202)。</summary>
        public static bool HasType(int type) => _data.ContainsKey(type);

        public static bool TryGet(int type, int subtype, out int value)
        {
            value = 0;
            return _data.TryGetValue(type, out Dictionary<int, int> block) && block.TryGetValue(subtype, out value);
        }

        /// <summary>读设置值;未到/无此项回退 fallback(老端 GetBlockProperty 缺省 1,数值类调用方自带缺省)。</summary>
        public static int Get(int type, int subtype, int fallback)
        {
            return TryGet(type, subtype, out int v) ? v : fallback;
        }

        /// <summary>10202 全量落地(整块覆盖)+ 音量副作用 + EVT_SETTING_UPDATED。</summary>
        public static void Apply10202(int type, List<KeyValuePair<int, int>> entries)
        {
            if (!_data.TryGetValue(type, out Dictionary<int, int> block))
            {
                block = new Dictionary<int, int>();
                _data[type] = block;
            }
            foreach (KeyValuePair<int, int> e in entries)
            {
                block[e.Key] = e.Value;
                if (type == TYPE_SYS_SETTING) SyncSideEffects(e.Key, e.Value);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_SETTING_UPDATED);
        }

        /// <summary>10203 写回成功后逐项落地(对标老端 UpdataSettingDataList)+ EVT_SETTING_UPDATED。</summary>
        public static void ApplyChanged(int type, IList<KeyValuePair<int, int>> entries)
        {
            if (entries == null || entries.Count == 0) return;
            if (!_data.TryGetValue(type, out Dictionary<int, int> block))
            {
                block = new Dictionary<int, int>();
                _data[type] = block;
            }
            foreach (KeyValuePair<int, int> e in entries)
            {
                block[e.Key] = e.Value;
                if (type == TYPE_SYS_SETTING) SyncSideEffects(e.Key, e.Value);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_SETTING_UPDATED);
        }

        /// <summary>音量两项服务器值 → AudioManager + 本地镜像(对标老端 onBlockSettingChange)。</summary>
        private static void SyncSideEffects(int subtype, int value)
        {
            switch (subtype)
            {
                case SUB_SOUND_OPEN: // 「音效」
                    AudioManager.SetVolume(AudioManager.Category.Sfx, value / 100f);
                    PrefsManager.SetFloat(PREF_SFX_VOLUME, value / 100f);
                    break;
                case SUB_SOUND_EFFECT_OPEN: // 「音乐」
                    AudioManager.SetVolume(AudioManager.Category.Music, value / 100f);
                    PrefsManager.SetFloat(PREF_MUSIC_VOLUME, value / 100f);
                    break;
            }
        }

        /// <summary>断线/登出清空(对标老端 ClearSettingData;下次进游戏 10202 重拉)。</summary>
        public static void Reset()
        {
            _data.Clear();
            ClearWxSubscriptionSwitch();
            GameLog.Info("Setting", "SettingModel 已清空(断线/登出)");
        }
    }
}
