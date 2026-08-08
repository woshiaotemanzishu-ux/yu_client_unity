using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Generated.UI.Suit;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Equip;
using Shenxiao.Module.Core.Resonance;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 共鸣模块非破坏性专项：三张同源配置闭包、可编辑 Prefab/Popup/ScrollRect 结构、
    /// 15221/15222 精确 wire、单飞、统一错误释放、主动 15222 推送及事务后权威重拉。
    /// 不点击真实账号的打造/返还确认，也不连接服务器。
    /// </summary>
    public static class ResonanceCase
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Suit/SuitModule.prefab";
        private const string CommonPrefabPath = "Assets/Prefabs/UI/Common/CommonModule.prefab";
        private const string EquipmentItemPrefabPath = "Assets/Prefabs/UI/Common/EquipmentItem.prefab";
        private const string BaseAwardItemPrefabPath = "Assets/Prefabs/UI/Common/BaseAwardItem.prefab";
        private const string BagEquipmentIconPrefabPath = "Assets/Prefabs/UI/Bag/BagEquipmentIcon.prefab";
        private const string BagModulePrefabPath = "Assets/Prefabs/UI/Bag/BagModule.prefab";
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly int[] SuitProtocols = { 15220, 15221, 15222, 15223, 15262 };

        public static string LastDetail { get; private set; } = "not-run";

        public static Task<int> Run()
        {
            bool config = false;
            bool prefab = false;
            bool addresses = false;
            bool protocol = false;
            string detail = string.Empty;
            try
            {
                config = VerifyConfigClosure(out string configDetail);
                prefab = VerifyPrefab(out string prefabDetail);
                addresses = VerifyAddressables(out string addressDetail);
                protocol = VerifyProtocol(out string protocolDetail);
                detail = "config=" + config + "[" + configDetail + "]"
                    + " prefab=" + prefab + "[" + prefabDetail + "]"
                    + " addressables=" + addresses + "[" + addressDetail + "]"
                    + " protocol=" + protocol + "[" + protocolDetail + "]";
            }
            catch (Exception exception)
            {
                detail = "exception=" + exception;
            }

            bool pass = config && prefab && addresses && protocol;
            LastDetail = detail;
            Debug.Log("CLIVERIFY resonance " + detail);
            Debug.Log("CLIVERIFY resonance VERDICT pass=" + pass + " liveDestructive=blocked-by-policy");
            return Task.FromResult(pass ? 0 : 3);
        }

        private static bool VerifyConfigClosure(out string detail)
        {
            TextAsset positionAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/GameRes/resource/config/server/config_equip_pos2suittype.json");
            TextAsset itemAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/GameRes/resource/config/server/config_equip_suit_item.json");
            TextAsset makeAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/GameRes/resource/config/server/config_equip_suit_make.json");
            MethodInfo parsePositions = ConfigMethod("ParsePositions");
            MethodInfo parseItems = ConfigMethod("ParseSuitItems");
            MethodInfo parseMakes = ConfigMethod("ParseMakeItems");
            MethodInfo buildPositions = ConfigMethod("BuildPositions");
            MethodInfo buildItems = ConfigMethod("BuildSuitLists");
            MethodInfo buildMakes = ConfigMethod("BuildMakeLists");
            MethodInfo validate = ConfigMethod("ValidateClosure");
            if (positionAsset == null || itemAsset == null || makeAsset == null
                || parsePositions == null || parseItems == null || parseMakes == null
                || buildPositions == null || buildItems == null || buildMakes == null || validate == null)
            {
                detail = "assets-or-parser-missing";
                return false;
            }

            object positionsRaw = parsePositions.Invoke(null, new object[] { positionAsset.text });
            object itemsRaw = parseItems.Invoke(null, new object[] { itemAsset.text });
            object makesRaw = parseMakes.Invoke(null, new object[] { makeAsset.text });
            object positions = buildPositions.Invoke(null, new[] { positionsRaw });
            object items = buildItems.Invoke(null, new[] { itemsRaw });
            object makes = buildMakes.Invoke(null, new[] { makesRaw });
            validate.Invoke(null, new[] { positions, items, makes });

            int positionCount = ((IDictionary)positionsRaw).Count;
            int itemCount = ((IDictionary)itemsRaw).Count;
            int makeCount = ((IDictionary)makesRaw).Count;
            bool pass = positionCount == 10 && itemCount == 46 && makeCount == 252
                && ResonanceConfigs.Tabs.Length == 4
                && ResonanceConfigs.Tabs[0].Label == "妖魂共鸣"
                && ResonanceConfigs.Tabs[1].Label == "战魂共鸣"
                && ResonanceConfigs.Tabs[2].Label == "万物共鸣"
                && ResonanceConfigs.Tabs[3].Label == "饰物共鸣";
            detail = "counts=" + positionCount + "/" + itemCount + "/" + makeCount
                + " parser=production validate=production";
            return pass;
        }

        private static MethodInfo ConfigMethod(string name)
            => typeof(ResonanceConfigs).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);

        private static bool VerifyPrefab(out string detail)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject commonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommonPrefabPath);
            GameObject equipmentItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EquipmentItemPrefabPath);
            GameObject baseAwardItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaseAwardItemPrefabPath);
            GameObject bagEquipmentIconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BagEquipmentIconPrefabPath);
            GameObject bagModulePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BagModulePrefabPath);
            EquipSuitMianViewBind main = prefab != null
                ? prefab.GetComponentInChildren<EquipSuitMianViewBind>(true) : null;
            EquipSuitPreviewTipsBind preview = prefab != null
                ? prefab.GetComponentInChildren<EquipSuitPreviewTipsBind>(true) : null;
            EquipSuitReturnViewBind returnView = prefab != null
                ? prefab.GetComponentInChildren<EquipSuitReturnViewBind>(true) : null;
            Transform popup = Find(prefab != null ? prefab.transform : null, "ResonancePopupLayer");
            Transform previewMask = Find(popup, "ResonancePreviewMask");
            Transform returnMask = Find(popup, "ResonanceReturnMask");
            ResonanceMainView lifecycle = main != null ? main.GetComponent<ResonanceMainView>() : null;

            bool binds = main != null && preview != null && returnView != null && lifecycle != null
                && AllObjectFieldsBound(main) && AllObjectFieldsBound(preview) && AllObjectFieldsBound(returnView);
            bool popupStructure = popup != null && preview.transform.IsChildOf(popup)
                && returnView.transform.IsChildOf(popup)
                && FullMask(previewMask) && FullMask(returnMask)
                && !preview.gameObject.activeSelf && !returnView.gameObject.activeSelf
                && !previewMask.gameObject.activeSelf && !returnMask.gameObject.activeSelf;
            ScrollRect[] scrolls = prefab != null
                ? prefab.GetComponentsInChildren<ScrollRect>(true) : Array.Empty<ScrollRect>();
            ScrollRect[] routedScrolls = main != null && returnView != null
                ? new[] { main.atrsList, main.costList, main.posList, returnView._Scroller1 }
                : Array.Empty<ScrollRect>();
            bool scrolling = routedScrolls.Length == 4 && routedScrolls.All(ValidScrollRect)
                && returnView._Scroller1.content == returnView.Content;
            RectTransform costTemplateRect = main != null && main._tpl_EquipSuitCostItem != null
                ? main._tpl_EquipSuitCostItem.transform as RectTransform : null;
            HorizontalLayoutGroup costLayout = main?.costList?.content != null
                ? main.costList.content.GetComponent<HorizontalLayoutGroup>() : null;
            ContentSizeFitter costFitter = main?.costList?.content != null
                ? main.costList.content.GetComponent<ContentSizeFitter>() : null;
            RectTransform costContent = main?.costList?.content;
            bool costsCentered = costContent != null && costLayout != null && costFitter != null
                && Mathf.Abs(costContent.anchorMin.x - 0.5f) < 0.001f
                && Mathf.Abs(costContent.anchorMax.x - 0.5f) < 0.001f
                && Mathf.Abs(costContent.pivot.x - 0.5f) < 0.001f
                && costLayout.childAlignment == TextAnchor.MiddleCenter
                && !costLayout.childControlWidth && !costLayout.childForceExpandWidth
                && costFitter.horizontalFit == ContentSizeFitter.FitMode.PreferredSize;
            float threeCostWidth = costTemplateRect != null && costLayout != null
                ? costTemplateRect.rect.width * 3f + costLayout.spacing * 2f : float.MaxValue;
            bool threeCostsVisible = main?.costList?.viewport != null
                && main.costList.viewport.rect.width + 0.01f >= threeCostWidth;
            EquipNewSuitAttrItemBind attrTemplate = main?._tpl_EquipNewSuitAttrItem != null
                ? main._tpl_EquipNewSuitAttrItem.GetComponent<EquipNewSuitAttrItemBind>() : null;
            EquipSuitCostItemBind costTemplate = main?._tpl_EquipSuitCostItem != null
                ? main._tpl_EquipSuitCostItem.GetComponent<EquipSuitCostItemBind>() : null;
            bool typography = attrTemplate?.attrHtml != null && costTemplate?.num_text != null
                && Mathf.Abs(attrTemplate.attrHtml.fontSize - 20f) < 0.01f
                && Mathf.Abs(attrTemplate.attrHtml.rectTransform.rect.width - 200f) < 0.1f
                && Mathf.Abs(attrTemplate.attrHtml.lineSpacing - 6f) < 0.01f
                && Mathf.Abs(costTemplate.num_text.fontSize - 18f) < 0.01f
                && main.nameSLab != null && main.nameSLab.richText
                && main.nameXLab != null && main.nameXLab.richText;
            GameObject[] templateObjects = main != null && preview != null && returnView != null
                ? new[]
                {
                    main._tpl_EquipNewSuitAttrItem, main._tpl_EquipSuitPosItem,
                    main._tpl_EquipmentItem, main._tpl_GiftPushIcon, main._tpl_EquipSuitCostItem,
                    preview._tpl_BaseAwardItem, preview._tpl_EquipmentItem, returnView._tpl_EquipmentItem,
                }
                : Array.Empty<GameObject>();
            bool templates = main != null
                && templateObjects.Length == 8
                && templateObjects.All(template => template != null && !template.activeSelf);
            bool sharedItemTemplate = main?._tpl_EquipmentItem != null
                && main._tpl_EquipmentItem.GetComponent<EquipmentItem>() != null
                && costTemplate?._tpl_EquipmentItem != null
                && costTemplate._tpl_EquipmentItem.GetComponent<EquipmentItem>() != null;
            EquipmentItem equipmentItemRoot = equipmentItemPrefab != null
                ? equipmentItemPrefab.GetComponent<EquipmentItem>() : null;
            BaseAwardItem baseAwardItemRoot = baseAwardItemPrefab != null
                ? baseAwardItemPrefab.GetComponent<BaseAwardItem>() : null;
            BagEquipmentIcon bagEquipmentIcon = bagEquipmentIconPrefab != null
                ? bagEquipmentIconPrefab.GetComponent<BagEquipmentIcon>() : null;
            BagItemRenderer bagItemRenderer = bagModulePrefab != null
                ? bagModulePrefab.GetComponentInChildren<BagItemRenderer>(true) : null;
            bool sharedRootComponents = equipmentItemRoot != null && baseAwardItemRoot != null
                && equipmentItemRoot._tpl_BaseAwardItem != null
                && equipmentItemRoot._tpl_BaseAwardItem.GetComponent<BaseAwardItem>() != null;
            bool representativeConsumers = bagEquipmentIcon?._tpl_EquipmentItem != null
                && bagEquipmentIcon._tpl_EquipmentItem.GetComponent<EquipmentItem>() != null
                && bagItemRenderer?._tpl_BaseAwardItem != null
                && bagItemRenderer._tpl_BaseAwardItem.GetComponent<BaseAwardItem>() != null;
            GoodsTooltipsBind goodsTips = commonPrefab != null
                ? commonPrefab.GetComponentInChildren<GoodsTooltipsBind>(true) : null;
            HorizontalLayoutGroup goodsButtonLayout = goodsTips?.btn_group != null
                ? goodsTips.btn_group.GetComponent<HorizontalLayoutGroup>() : null;
            RectTransform[] goodsButtons = goodsTips != null
                ? new[]
                {
                    goodsTips.useBtn, goodsTips.sellBtn, goodsTips.okBtn, goodsTips.upShelfBtn,
                    goodsTips.outShelfBtn, goodsTips.treasureReceiveBtn, goodsTips.takeoutBtn,
                    goodsTips.depositBtn, goodsTips.putBtn,
                }
                : Array.Empty<RectTransform>();
            bool sharedGoodsLayout = goodsTips?.type_text != null && goodsTips.quantity_text != null
                && goodsTips.btn_group != null && goodsButtonLayout != null
                && goodsTips.type_text.rectTransform.rect.width >= 240f
                && goodsTips.quantity_text.rectTransform.rect.width >= 240f
                && Mathf.Abs(goodsTips.type_text.rectTransform.anchoredPosition.x - 120f) < 0.1f
                && Mathf.Abs(goodsTips.quantity_text.rectTransform.anchoredPosition.x - 120f) < 0.1f
                && goodsButtonLayout.childAlignment == TextAnchor.MiddleCenter
                && !goodsButtonLayout.childControlWidth && !goodsButtonLayout.childControlHeight
                && !goodsButtonLayout.childForceExpandWidth && !goodsButtonLayout.childForceExpandHeight
                && goodsButtons.Length == 9
                && goodsButtons.All(button => button != null && button.parent == goodsTips.btn_group);
            bool separator = returnView != null && returnView.line != null
                && returnView.line.sprite != null && returnView.line.enabled;

            detail = "binds=" + binds + " popup=" + popupStructure + " scrolls=" + scrolls.Length
                + "/routed4=" + scrolling + " threeCosts=" + threeCostsVisible
                + "[viewport=" + (main?.costList?.viewport != null ? main.costList.viewport.rect.width : -1f)
                + ",need=" + threeCostWidth + "] templates=" + templates
                + "/active=" + string.Join(",", templateObjects
                    .Where(template => template != null && template.activeSelf).Select(template => template.name))
                + " centeredCosts=" + costsCentered + " sharedItemTemplate=" + sharedItemTemplate
                + " sharedRoots=" + sharedRootComponents + " bagSamples=" + representativeConsumers
                + " sharedGoodsLayout=" + sharedGoodsLayout
                + " typography=" + typography + "[attr20x200/line6,cost18]"
                + " separator=" + separator;
            return binds && popupStructure && scrolling && threeCostsVisible && costsCentered
                && templates && sharedItemTemplate && sharedRootComponents && representativeConsumers
                && sharedGoodsLayout && typography && separator;
        }

        private static bool VerifyAddressables(out string detail)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings != null ? settings.FindGroup("Remote_suit") : null;
            string[] expected =
            {
                "prefabs/ui/suit/suitmodule",
                "resource/config/server/config_equip_pos2suittype",
                "resource/config/server/config_equip_suit_item",
                "resource/config/server/config_equip_suit_make",
            };
            string[] actual = group != null
                ? group.entries.Select(entry => entry.address).OrderBy(value => value, StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();
            string[] sortedExpected = expected.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            bool pass = group != null && actual.SequenceEqual(sortedExpected)
                && group.entries.All(entry => entry.labels.Contains("pack_suit"));
            detail = "group=" + (group != null) + " entries=" + string.Join(",", actual);
            return pass;
        }

        private static bool VerifyProtocol(out string detail)
        {
            EquipReadController controller = EquipReadController.Instance;
            EquipReadModel model = EquipReadModel.Instance;
            bool wasInitialized = controller.IsInitialized;
            var snapshot = new ModelSnapshot(model);
            FieldInfo equipIntercept = typeof(EquipReadController).GetField("s_outboundIntercept", StaticPrivate);
            FieldInfo bagIntercept = typeof(BagController).GetField("s_outboundIntercept", StaticPrivate);
            object oldEquipIntercept = equipIntercept?.GetValue(null);
            object oldBagIntercept = bagIntercept?.GetValue(null);
            var equipFrames = new List<byte[]>();
            var bagFrames = new List<byte[]>();
            var capture = new EventCapture();
            bool pass = false;
            try
            {
                if (controller.SuitOperationPending)
                    throw new InvalidOperationException("existing resonance operation is pending");
                if (!controller.IsInitialized) controller.Init();
                equipIntercept?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    equipFrames.Add((byte[])frame.Clone());
                    return true;
                }));
                bagIntercept?.SetValue(null, new Func<byte[], bool>(frame =>
                {
                    bagFrames.Add((byte[])frame.Clone());
                    return true;
                }));
                EventDispatcher.On<EquipReadController.SuitOperationResult>(
                    GlobalEvent.EVT_EQUIP_SUIT_OPERATION_RESULT, capture.OnResult);
                EventDispatcher.On(GlobalEvent.EVT_EQUIP_SUIT_UPDATE, capture.OnUpdate);

                IDictionary handlers = typeof(NetManager).GetField("_handlers", StaticPrivate)?.GetValue(null) as IDictionary;
                MethodInfo on21 = typeof(EquipReadController).GetMethod("On15221", InstancePrivate);
                MethodInfo on22 = typeof(EquipReadController).GetMethod("On15222", InstancePrivate);
                pass = handlers != null && handlers.Contains(15221) && handlers.Contains(15222)
                    && !handlers.Contains(15218) && on21 != null && on22 != null
                    && equipIntercept != null && bagIntercept != null;

                pass &= !controller.TryRequestSuitBuild(0, 1)
                    && !controller.TryRequestSuitReturn(1, 0) && equipFrames.Count == 0;

                pass &= controller.TryRequestSuitBuild(1, 2)
                    && !controller.TryRequestSuitReturn(1, 2)
                    && controller.SuitOperationPending
                    && equipFrames.Count == 1 && TwoU8Frame(equipFrames[0], 15221, 1, 2);
                pass &= Feed(on21, controller, BuildPacket(2, 1, 3))
                    && !controller.SuitOperationPending
                    && model.GetSuitLevel(2, 1) == 3
                    && capture.Results.Count == 1
                    && capture.Results[0].Success && capture.Results[0].WasRequested
                    && capture.Results[0].SuitList.Count == 2;

                model.UpsertSuit(3, 2, 5);
                pass &= Feed(on22, controller, ReturnPacket(3, 2, 0))
                    && model.GetSuitLevel(3, 2) == 0
                    && capture.Results.Count == 2
                    && capture.Results[1].Success && !capture.Results[1].WasRequested
                    && capture.Results[1].Rewards.Count == 2
                    && capture.Results[1].Rewards[0].Id == uint.MaxValue
                    && capture.Results[1].Rewards[0].Num == ushort.MaxValue;

                pass &= controller.TryRequestSuitReturn(2, 3)
                    && TwoU8Frame(equipFrames[equipFrames.Count - 1], 15222, 2, 3);
                pass &= Feed(on22, controller, ReturnPacket(3, 2, 4))
                    && !controller.SuitOperationPending && model.GetSuitLevel(3, 2) == 4
                    && capture.Results.Count == 3 && capture.Results[2].WasRequested;

                pass &= controller.TryRequestSuitBuild(1, 4);
                pass &= Feed(on21, controller, BuildPacket(5, 1, 2))
                    && controller.SuitOperationPending
                    && capture.Results.Count == 4 && !capture.Results[3].WasRequested;
                EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_ERROR, 1520091);
                pass &= !controller.SuitOperationPending && capture.Results.Count == 5
                    && !capture.Results[4].Success && capture.Results[4].WasRequested
                    && capture.Results[4].ErrorCode == 1520091
                    && capture.Results[4].EquipType == 4 && capture.Results[4].MakeType == 1;

                pass &= controller.TryRequestSuitReturn(3, 6);
                EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_ERROR, 77);
                pass &= !controller.SuitOperationPending && capture.Results.Count == 6
                    && !capture.Results[5].Success && capture.Results[5].ErrorCode == 77;

                int[] expectedEquip = { 15221, 15220, 15220, 15222, 15220, 15221, 15220, 15222 };
                pass &= equipFrames.Count == expectedEquip.Length;
                for (int i = 0; i < Math.Min(equipFrames.Count, expectedEquip.Length); i++)
                    pass &= FrameProtocol(equipFrames[i]) == expectedEquip[i];
                pass &= bagFrames.Count == 8;
                for (int i = 0; i < bagFrames.Count; i++)
                    pass &= U16Frame(bagFrames[i], Proto.GOODS_CONTAINER_INFO,
                        (ushort)(i % 2 == 0 ? BagModel.POS_BAG : BagModel.POS_EQUIP));
                pass &= capture.UpdateCount == 4;
            }
            finally
            {
                EventDispatcher.Off<EquipReadController.SuitOperationResult>(
                    GlobalEvent.EVT_EQUIP_SUIT_OPERATION_RESULT, capture.OnResult);
                EventDispatcher.Off(GlobalEvent.EVT_EQUIP_SUIT_UPDATE, capture.OnUpdate);
                typeof(EquipReadController).GetMethod("ClearPending", InstancePrivate)?.Invoke(controller, null);
                if (!wasInitialized && controller.IsInitialized) controller.Dispose();
                snapshot.Restore(model);
                equipIntercept?.SetValue(null, oldEquipIntercept);
                bagIntercept?.SetValue(null, oldBagIntercept);
            }

            detail = "frames=" + equipFrames.Count + " bagRefresh=" + bagFrames.Count
                + " results=" + capture.Results.Count + " updates=" + capture.UpdateCount
                + " restored=True";
            return pass;
        }

        private static CliVerify.Pkt BuildPacket(byte equipType, byte makeType, ushort level)
            => new CliVerify.Pkt().C(equipType).C(makeType).H(level).H(2)
                .C(1).C(1).C(6).C(2).C(1).C(4);

        private static CliVerify.Pkt ReturnPacket(byte equipType, byte makeType, ushort level)
            => new CliVerify.Pkt().C(equipType).C(makeType).H(level).H(2)
                .C(byte.MaxValue).I(uint.MaxValue).H(ushort.MaxValue).S("[{中文}]")
                .C(0).I(42).H(0).S(string.Empty)
                .H(1).C(1).C(1).C(6);

        private static bool Feed(MethodInfo method, EquipReadController controller, CliVerify.Pkt packet)
        {
            byte[] bytes = packet.Bytes();
            var reader = new NetReader(bytes, 0, bytes.Length);
            method.Invoke(controller, new object[] { reader });
            return reader.Remaining == 0;
        }

        private static bool AllObjectFieldsBound(object bind)
            => bind != null && bind.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(field => typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                .All(field => field.GetValue(bind) != null);

        private static bool FullMask(Transform transform)
        {
            RectTransform rect = transform as RectTransform;
            Image image = transform != null ? transform.GetComponent<Image>() : null;
            return rect != null && image != null && image.raycastTarget
                && Near(rect.anchorMin, Vector2.zero) && Near(rect.anchorMax, Vector2.one)
                && Near(rect.offsetMin, Vector2.zero) && Near(rect.offsetMax, Vector2.zero);
        }

        private static bool ValidScrollRect(ScrollRect scroll)
            => scroll != null && scroll.viewport != null && scroll.content != null
                && scroll.viewport.GetComponent<RectMask2D>() != null
                && (scroll.content.GetComponent<HorizontalLayoutGroup>() != null
                    || scroll.content.GetComponent<VerticalLayoutGroup>() != null
                    || scroll.content.GetComponent<GridLayoutGroup>() != null)
                && scroll.content.GetComponent<ContentSizeFitter>() != null;

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = Find(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static bool Near(Vector2 actual, Vector2 expected)
            => Vector2.SqrMagnitude(actual - expected) <= 0.0001f;

        private static bool TwoU8Frame(byte[] frame, int protocol, byte first, byte second)
            => frame != null && frame.Length == 8 && Header(frame, protocol, 8)
                && frame[6] == first && frame[7] == second;

        private static bool U16Frame(byte[] frame, int protocol, ushort value)
            => frame != null && frame.Length == 8 && Header(frame, protocol, 8)
                && frame[6] == (byte)(value >> 8) && frame[7] == (byte)value;

        private static bool Header(byte[] frame, int protocol, int length)
            => frame[0] == 0 && frame[1] == length && frame[2] == 3 && frame[3] == 232
                && frame[4] == (byte)(protocol >> 8) && frame[5] == (byte)protocol;

        private static int FrameProtocol(byte[] frame)
            => frame != null && frame.Length >= 6 ? (frame[4] << 8) | frame[5] : -1;

        private sealed class EventCapture
        {
            public readonly List<EquipReadController.SuitOperationResult> Results =
                new List<EquipReadController.SuitOperationResult>();
            public int UpdateCount { get; private set; }
            public void OnResult(EquipReadController.SuitOperationResult result) => Results.Add(result);
            public void OnUpdate() => UpdateCount++;
        }

        private sealed class ModelSnapshot
        {
            private readonly bool _hasGod;
            private readonly uint _godPower;
            private readonly List<EquipReadModel.GodEntry> _gods;
            private readonly bool _hasPreview;
            private readonly uint _previewPower;
            private readonly bool _hasSuit;
            private readonly List<EquipReadModel.SuitEntry> _suits;
            private readonly Dictionary<ushort, EquipReadModel.SuitReturnPreview> _returns;
            private readonly Dictionary<uint, EquipReadModel.SuitPowerSnapshot> _powers;
            private readonly int _version;

            public ModelSnapshot(EquipReadModel model)
            {
                _hasGod = model.HasGodInfo;
                _godPower = model.GodTotalPower;
                _gods = new List<EquipReadModel.GodEntry>(model.GodEntries);
                _hasPreview = model.HasGodPowerPreview;
                _previewPower = model.GodPowerPreview;
                _hasSuit = model.HasSuitInfo;
                _suits = new List<EquipReadModel.SuitEntry>(model.SuitEntries);
                _returns = CopyMap<ushort, EquipReadModel.SuitReturnPreview>(model, "_returnPreviews");
                _powers = CopyMap<uint, EquipReadModel.SuitPowerSnapshot>(model, "_suitPowers");
                _version = model.Version;
            }

            public void Restore(EquipReadModel model)
            {
                model.Reset();
                if (_hasGod) model.ReplaceGodInfo(_godPower, _gods);
                if (_hasPreview) model.ReplaceGodPowerPreview(_previewPower);
                if (_hasSuit) model.ReplaceSuitInfo(_suits);
                foreach (EquipReadModel.SuitReturnPreview value in _returns.Values)
                    model.ReplaceReturnPreview(value);
                foreach (EquipReadModel.SuitPowerSnapshot value in _powers.Values)
                    model.ReplaceSuitPower(value);
                typeof(EquipReadModel).GetField("<Version>k__BackingField", InstancePrivate)
                    ?.SetValue(model, _version);
            }

            private static Dictionary<TKey, TValue> CopyMap<TKey, TValue>(EquipReadModel model, string field)
                => new Dictionary<TKey, TValue>((Dictionary<TKey, TValue>)typeof(EquipReadModel)
                    .GetField(field, InstancePrivate).GetValue(model));
        }
    }
}
