using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.CustomActivity;
using Shenxiao.Module.Core.ListDuobao;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// ListDuobao 最小闭环验证：检查 33252/33253/33803 的真实运行时注册，反射喂包验证活动守卫、
    /// 阶段排序、模型字段与事件，再实例化两个源 prefab 验证七个业务组件及关键 Bind 引用。
    /// </summary>
    public static class ListDuobaoCase
    {
        private const int BaseType = 116;
        private const int SubType = 77;
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private const string ModulePath = "Assets/Prefabs/UI/ListDuobao/ListDuobaoModule.prefab";
        private const string GoodsItemPath = "Assets/Prefabs/UI/ListDuobao/ListGoodsItem.prefab";
        private const string StageConfigPath = "Assets/GameRes/resource/config/server/config_rush_treasure_stage_reward.json";
        private const string RankConfigPath = "Assets/GameRes/resource/config/server/config_rush_treasure_rank_reward.json";

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY listduobao exception: " + e);
                return Task.FromResult(1);
            }
        }

        private static int RunSync()
        {
            CustomActivityController ctrl = CustomActivityController.Instance;
            CustomActivityModel model = CustomActivityModel.Instance;

            int oldSubType = model.ListDuobaoSubType;
            CustomActivityModel.ListDuobaoStageInfo oldStage = model.ListDuobaoStage;
            CustomActivityModel.ListDuobaoRankInfo oldRank = model.ListDuobaoRank;
            CustomActivityModel.ListDuobaoDrawResult oldDraw = model.ListDuobaoDraw;
            bool oldFirstIn = model.ListDuobaoFirstIn;

            bool detailSubscribed = false;
            bool drawSubscribed = false;
            GameObject moduleInstance = null;
            GameObject goodsInstance = null;
            int detailCount = 0;
            int lastDetailType = -1;
            int lastDetailSubType = -1;
            int drawCount = 0;
            CustomActivityModel.ListDuobaoDrawResult eventDraw = null;
            Action<int, int> onDetail = (type, subType) =>
            {
                detailCount++;
                lastDetailType = type;
                lastDetailSubType = subType;
            };
            Action<CustomActivityModel.ListDuobaoDrawResult> onDraw = result =>
            {
                drawCount++;
                eventDraw = result;
            };

            try
            {
                // BaseController.Init 幂等。独立入口可能尚未初始化；RenderAll 则复用前序 CustomAct Case 的实例。
                ctrl.Init();

                MethodInfo m33252 = ctrl.GetType().GetMethod("On33252", InstancePrivate);
                MethodInfo m33253 = ctrl.GetType().GetMethod("On33253", InstancePrivate);
                MethodInfo m33254 = ctrl.GetType().GetMethod("On33254", InstancePrivate);
                MethodInfo m33803 = ctrl.GetType().GetMethod("On33803", InstancePrivate);
                bool methodsOk = m33252 != null && m33253 != null && m33254 != null && m33803 != null;

                FieldInfo handlersField = typeof(NetManager).GetField("_handlers", StaticPrivate);
                IDictionary handlers = handlersField?.GetValue(null) as IDictionary;
                bool registrationOk = methodsOk
                    && IsActualHandler(handlers, Proto.CUSTOM_ACT_LISTDUOBAO_STAGE, ctrl, m33252)
                    && IsActualHandler(handlers, Proto.CUSTOM_ACT_LISTDUOBAO_RANK, ctrl, m33253)
                    && IsActualHandler(handlers, Proto.CUSTOM_ACT_LISTDUOBAO_CLAIM, ctrl, m33254)
                    && IsActualHandler(handlers, Proto.COMPETE_ACT_LIST_DUOBAO_DRAW, ctrl, m33803);
                Debug.Log("CLIVERIFY listduobao actual registrations 33252/33253/33254/33803 ok=" + registrationOk);
                if (!methodsOk) return 3;

                EventDispatcher.On<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                detailSubscribed = true;
                EventDispatcher.On<CustomActivityModel.ListDuobaoDrawResult>(GlobalEvent.EVT_LIST_DUOBAO_DRAW_RESULT, onDraw);
                drawSubscribed = true;

                model.ClearList();
                model.SetListDuobaoSubType(SubType);

                byte[] stagePacket = BuildStagePacket(SubType);
                Feed(ctrl, m33252, BuildStagePacket(SubType + 1));
                bool stageGuardOk = model.ListDuobaoStage == null && detailCount == 0;
                Feed(ctrl, m33252, stagePacket);
                CustomActivityModel.ListDuobaoStageInfo stage = model.ListDuobaoStage;
                bool stageOk = stage != null
                    && stage.Type == BaseType && stage.SubType == SubType
                    && stage.Score == 1234 && stage.TodayScore == 56 && stage.Condition == "score>=100" && stage.WorldLv == 88
                    && stage.RewardList.Count == 1 && stage.RewardList[0].GradeId == 7 && stage.RewardList[0].IsRare == 1
                    && stage.RewardList[0].Reward != null && stage.RewardList[0].Reward.Count == 1
                    && stage.RewardList[0].Reward[0].Type == 2 && stage.RewardList[0].Reward[0].GoodsId == 5001
                    && stage.RewardList[0].Reward[0].Num == 3
                    && stage.StageList.Count == 2 && stage.StageList[0].Id == 2 && stage.StageList[0].GotType == 1
                    && stage.StageList[1].Id == 9 && stage.StageList[1].GotType == 0
                    && detailCount == 1 && lastDetailType == BaseType && lastDetailSubType == SubType;

                Feed(ctrl, m33253, BuildRankPacket(SubType + 1));
                bool rankGuardOk = model.ListDuobaoRank == null && detailCount == 1;
                Feed(ctrl, m33253, BuildRankPacket(SubType));
                CustomActivityModel.ListDuobaoRankInfo rank = model.ListDuobaoRank;
                bool rankOk = rank != null
                    && rank.Type == BaseType && rank.SubType == SubType && rank.Score == 7654 && rank.Rank == 3
                    && rank.RankList.Count == 2
                    && rank.RankList[0].Rank == 2 && rank.RankList[0].ServerId == 2002
                    && rank.RankList[0].RoleId == 9002 && rank.RankList[0].RoleName == "second" && rank.RankList[0].RoleScore == 222
                    && rank.RankList[1].Rank == 1 && rank.RankList[1].RoleId == 9001
                    && rank.ServerScore == 8888 && rank.ServerRank == 4
                    && rank.ServerRankList.Count == 1 && rank.ServerRankList[0].Rank == 1
                    && rank.ServerRankList[0].ServerId == 3001 && rank.ServerRankList[0].ServerName == "server-one"
                    && rank.ServerRankList[0].ServerScore == 9999
                    && detailCount == 2 && lastDetailType == BaseType && lastDetailSubType == SubType;

                Feed(ctrl, m33803, BuildDrawPacket(SubType + 1));
                bool drawGuardOk = model.ListDuobaoDraw == null && drawCount == 0;
                Feed(ctrl, m33803, BuildDrawPacket(SubType));
                CustomActivityModel.ListDuobaoDrawResult draw = model.ListDuobaoDraw;
                bool drawOk = draw != null && draw.Type == BaseType && draw.SubType == SubType
                    && draw.Times == 10 && draw.TodayScore == 4321 && draw.Error == 1
                    && draw.RewardList.Count == 1 && draw.RewardList[0].RewardId == 12
                    && draw.RewardList[0].Reward.Count == 1 && draw.RewardList[0].Reward[0].Type == 1
                    && draw.RewardList[0].Reward[0].GoodsId == 6001 && draw.RewardList[0].Reward[0].Num == 5
                    && drawCount == 1 && ReferenceEquals(eventDraw, draw);

                bool prefabOk = VerifyPrefabs(out moduleInstance, out goodsInstance);
                bool pass = registrationOk && stageGuardOk && stageOk && rankGuardOk && rankOk
                    && drawGuardOk && drawOk && prefabOk;
                Debug.Log("CLIVERIFY listduobao VERDICT registration=" + registrationOk
                    + " stageGuard=" + stageGuardOk + " stage=" + stageOk
                    + " rankGuard=" + rankGuardOk + " rank=" + rankOk
                    + " drawGuard=" + drawGuardOk + " draw=" + drawOk
                    + " prefab=" + prefabOk + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (detailSubscribed)
                    EventDispatcher.Off<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, onDetail);
                if (drawSubscribed)
                    EventDispatcher.Off<CustomActivityModel.ListDuobaoDrawResult>(GlobalEvent.EVT_LIST_DUOBAO_DRAW_RESULT, onDraw);
                if (moduleInstance != null) UnityEngine.Object.DestroyImmediate(moduleInstance);
                if (goodsInstance != null) UnityEngine.Object.DestroyImmediate(goodsInstance);

                // ClearList 会恢复 FirstIn=true；随后逐项还原进入本 Case 前的引用和标志。
                model.ClearList();
                model.SetListDuobaoSubType(oldSubType);
                model.SetListDuobaoStage(oldStage);
                model.SetListDuobaoRank(oldRank);
                model.SetListDuobaoDraw(oldDraw);
                if (!oldFirstIn) model.MarkListDuobaoEntered();
            }
        }

        private static bool IsActualHandler(IDictionary handlers, int proto, object target, MethodInfo expectedMethod)
        {
            if (handlers == null || !handlers.Contains(proto) || !(handlers[proto] is Delegate handler)) return false;
            return ReferenceEquals(handler.Target, target) && handler.Method == expectedMethod;
        }

        private static void Feed(object ctrl, MethodInfo method, byte[] packet)
        {
            method.Invoke(ctrl, new object[] { new NetReader(packet, 0, packet.Length) });
        }

        private static byte[] BuildStagePacket(int subType)
        {
            return new CliVerify.Pkt()
                .H(BaseType).H(subType).I(1234).I(56).S("score>=100")
                .H(1).H(7).C(1).H(1).C(2).I(5001).I(3)
                .H(2).H(9).C(0).H(2).C(1)
                .I(88).Bytes();
        }

        private static byte[] BuildRankPacket(int subType)
        {
            return new CliVerify.Pkt()
                .H(BaseType).H(subType).I(7654).H(3)
                .H(2)
                .H(2).I(2002).L(9002).S("second").I(222)
                .H(1).I(2001).L(9001).S("first").I(333)
                .I(8888).H(4)
                .H(1).H(1).I(3001).S("server-one").I(9999)
                .Bytes();
        }

        private static byte[] BuildDrawPacket(int subType)
        {
            return new CliVerify.Pkt()
                .H(BaseType).H(subType).C(10).I(4321).I(1)
                .H(1).H(12).H(1).C(1).I(6001).I(5)
                .Bytes();
        }

        private static bool VerifyPrefabs(out GameObject moduleInstance, out GameObject goodsInstance)
        {
            moduleInstance = null;
            goodsInstance = null;
            GameObject moduleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath);
            GameObject goodsAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GoodsItemPath);
            if (moduleAsset == null || goodsAsset == null) return false;

            moduleInstance = UnityEngine.Object.Instantiate(moduleAsset);
            goodsInstance = UnityEngine.Object.Instantiate(goodsAsset);
            ListDuobaoView main = moduleInstance.GetComponentInChildren<ListDuobaoView>(true);
            ListDuobaoRecordView record = moduleInstance.GetComponentInChildren<ListDuobaoRecordView>(true);
            ListRewardView rewardView = moduleInstance.GetComponentInChildren<ListRewardView>(true);
            ListRankView rankView = moduleInstance.GetComponentInChildren<ListRankView>(true);
            ListRewardItem rewardItem = moduleInstance.GetComponentInChildren<ListRewardItem>(true);
            ListRankItem rankItem = moduleInstance.GetComponentInChildren<ListRankItem>(true);
            ListGoodsItem goodsItem = goodsInstance.GetComponentInChildren<ListGoodsItem>(true);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            bool configsAddressable = HasAddress(settings, StageConfigPath,
                    "resource/config/server/config_rush_treasure_stage_reward")
                && HasAddress(settings, RankConfigPath,
                    "resource/config/server/config_rush_treasure_rank_reward");

            return main != null && main._gp_rank != null && main._btn_one != null && main._btn_ten != null
                && main._tpl_BaseAwardItem != null
                && record != null && record._img_close != null && record._gp_record != null
                && rewardView != null && rewardView._gp_reward != null && rewardView._tpl_ListRewardItem != null
                && rankView != null && rankView._player_rank != null && rankView._server_rank != null
                && rankView._tpl_ListRankItem != null && rankView._tpl_ListGoodsItem != null
                && rankView._tpl_ListGoodsItem.GetComponent<ListGoodsItem>() != null
                && rewardItem != null && rewardItem._gp_item != null
                && rankItem != null && rankItem._gp_reward != null
                && goodsItem != null && goodsItem._gp_item != null && goodsItem._tpl_BaseAwardItem != null
                && configsAddressable;
        }

        private static bool HasAddress(AddressableAssetSettings settings, string path, string address)
        {
            AddressableAssetEntry entry = settings?.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            return entry != null && entry.address == address && entry.labels.Contains("pack_resource_config");
        }

        public static void RunBatch()
        {
            _ = RunBatchAsync();
        }

        private static async Task RunBatchAsync()
        {
            int code = await Run();
            EditorApplication.Exit(code);
        }
    }
}
