using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// Scene protocol controller. Mirrors old-client SceneController 12005 entry path.
    /// </summary>
    public sealed class SceneController : BaseController
    {
        public static readonly SceneController Instance = new SceneController();

        private int _loadVersion;

        private SceneController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.SC_LOAD_SCENE, On12002);
            RegisterProtocal(Proto.SC_CHANGE_SCENE, On12005);
            RegisterProtocal(Proto.SC_DROP_LIST, On12018);
            RegisterProtocal(Proto.SC_NPC_ICON_REFRESH, On12020);
            RegisterProtocal(Proto.SC_NPC_LIST, On12100);
            RegisterProtocal(Proto.SC_NPC_DYNAMIC, On12103);
        }

        public void RequestEnterScene(RoleModel role)
        {
            if (role == null || !role.HasBaseInfo)
            {
                GameLog.Warn("Scene", "skip 12005: role base info is not ready");
                return;
            }

            SendFmt(Proto.SC_CHANGE_SCENE, "iicchh", role.DunId, role.SceneId, 0, 0, 0, 0);
            GameLog.Info("Scene", "request 12005: dunId={0} sceneId={1}", role.DunId, role.SceneId);
        }

        public override void Dispose()
        {
            bool keepMap = LoginController.Instance.CanAutoReconnectInGame;
            ++_loadVersion;
            if (keepMap)
            {
                GameLog.Info("Scene", "keep scene map during in-game reconnect");
            }
            else
            {
                SceneMapLoader.Clear();
                SceneMapView.Clear();
            }
            base.Dispose();
        }

        private void On12005(NetReader reader)
        {
            object[] data = reader.ReadFmt("ihhiicc");
            int instanceId = ToInt(data[0]);
            int x = ToInt(data[1]);
            int y = ToInt(data[2]);
            int errorCode = ToInt(data[3]);
            int dunId = ToInt(data[4]);
            int callbackType = ToInt(data[5]);
            int sendType = ToInt(data[6]);

            if (instanceId == 0)
            {
                GameLog.Warn("Scene", "12005 failed: errorCode={0} callback={1} sendType={2}", errorCode, callbackType, sendType);
                return;
            }

            RoleModel role = RoleModel.Instance;
            role.SceneId = instanceId;
            role.X = x;
            role.Y = y;
            role.DunId = dunId;
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);

            GameLog.Info("Scene", "12005 ok: sceneId={0} dunId={1} pos=({2},{3})", instanceId, dunId, x, y);
            _ = LoadSceneMapAsync(instanceId);
        }

        private async Task LoadSceneMapAsync(int sceneId)
        {
            int version = ++_loadVersion;
            SceneMapData data = await SceneMapLoader.LoadAsync(sceneId);
            if (version != _loadVersion || data == null) return;

            RoleModel role = RoleModel.Instance;
            await SceneMapView.ShowAsync(data, role.X, role.Y);
            if (version != _loadVersion) return;

            SendFmt(Proto.SC_NPC_LIST, "i", sceneId);
            GameLog.Info("Scene", "request 12100: local map loaded sceneId={0}", sceneId);
            EventDispatcher.Emit(GlobalEvent.EVT_SCENE_MAP_READY);
        }

        private void On12100(NetReader reader)
        {
            int sceneId = ToInt(reader.ReadU32());
            int npcCount = reader.ReadU16();
            int parsed = ParseNpcList(reader, npcCount);
            GameLog.Info("Scene", "12100 npc list ready: sceneId={0} count={1} parsed={2} remaining={3}B",
                sceneId, npcCount, parsed, reader.Remaining);

            if (sceneId != RoleModel.Instance.SceneId)
            {
                GameLog.Warn("Scene", "skip 12002: npc scene mismatch current={0} reply={1}", RoleModel.Instance.SceneId, sceneId);
                return;
            }

            SendFmt(Proto.SC_LOAD_SCENE);
            GameLog.Info("Scene", "request 12002: npc list loaded sceneId={0}", sceneId);
        }

        private void On12103(NetReader reader)
        {
            int npcCount = reader.ReadU16();
            int parsed = ParseNpcList(reader, npcCount);
            GameLog.Info("Scene", "12103 dynamic npc list: count={0} parsed={1} remaining={2}B",
                npcCount, parsed, reader.Remaining);
        }

        private void On12002(NetReader reader)
        {
            int payloadBytes = reader.Remaining;
            GameLog.Info("Scene", "12002 scene snapshot ready: payload={0}B, object parsing deferred", payloadBytes);

            SendFmt(Proto.SC_DROP_LIST);
            GameLog.Info("Scene", "request 12018: scene drop list");

            SendFmt(Proto.SC_NPC_ICON_REFRESH);
            GameLog.Info("Scene", "request 12020: npc icon refresh");
        }

        private void On12018(NetReader reader)
        {
            if (reader.Remaining < 2)
            {
                GameLog.Warn("Scene", "12018 drop list payload too short: {0}B", reader.Remaining);
                return;
            }

            int dropCount = reader.ReadU16();
            GameLog.Info("Scene", "12018 drop list ready: count={0} remaining={1}B", dropCount, reader.Remaining);
        }

        private void On12020(NetReader reader)
        {
            if (reader.Remaining < 2)
            {
                GameLog.Warn("Scene", "12020 npc icon payload too short: {0}B", reader.Remaining);
                return;
            }

            int count = reader.ReadU16();
            int parsed = 0;
            for (int i = 0; i < count && reader.Remaining >= 5; i++)
            {
                reader.ReadU32();
                reader.ReadU8();
                parsed++;
            }

            GameLog.Info("Scene", "12020 npc icon refresh: count={0} parsed={1} remaining={2}B", count, parsed, reader.Remaining);
        }

        private static int ToInt(object value)
        {
            return Convert.ToInt32(value);
        }

        private static int ParseNpcList(NetReader reader, int count)
        {
            int parsed = 0;
            for (int i = 0; i < count; i++)
            {
                if (reader.Remaining < 13) break;

                reader.ReadU32();    // npc_id
                reader.ReadU8();     // is_show
                reader.ReadU32();    // scene_id
                reader.ReadU16();    // x
                reader.ReadU16();    // y
                reader.ReadString(); // args
                parsed++;
            }

            return parsed;
        }
    }
}
