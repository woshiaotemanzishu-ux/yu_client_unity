using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Equip;
using Shenxiao.Module.Core.Tasks;
using Shenxiao.Generated.UI.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 背包/仓库交互专项：验证 15002/15003/15024/15025/15050/15201 精确出站，
    /// 并从真实 BagModule Prefab 通过 GraphicRaycaster→PointerClick 打开右侧功能窗。
    /// </summary>
    public static class BagInteractionCase
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Bag/BagModule.prefab";
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;

        public static async Task<int> Run()
        {
            bool oldFallback = ResManager.EditorPreferFallback;
            try
            {
                ResManager.EditorPreferFallback = true;
                await Task.WhenAll(GoodsModel.EnsureLoaded(), EquipmentTipsConfig.EnsureLoaded(),
                    FuncOpenConfig.EnsureLoaded(), BagFusionConfigs.EnsureLoaded());
                int core = RunSync();
                if (core != 0) return core;
                return await VerifyItemTipsClicksAsync();
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY bag-interaction EXCEPTION " + e);
                return 3;
            }
            finally
            {
                ResManager.EditorPreferFallback = oldFallback;
            }
        }

        private static int RunSync()
        {
            FieldInfo bagIntercept = typeof(BagController).GetField("s_outboundIntercept", SF);
            FieldInfo fusionIntercept = typeof(BagFusionController).GetField("s_outboundIntercept", SF);
            FieldInfo equipIntercept = typeof(EquipWearController).GetField("s_outboundIntercept", SF);
            FieldInfo rootsField = typeof(BagFlow).GetField("_contentRoots", SF);
            FieldInfo pendingUseField = typeof(BagController).GetField("_pendingUse", F);
            FieldInfo fusionPendingField = typeof(BagFusionController).GetField("_pendingUntil", F);
            MethodInfo setWarehouse = typeof(BagModel).GetMethod("SetWarehouseFull", F);
            if (bagIntercept == null || fusionIntercept == null || equipIntercept == null || rootsField == null
                || pendingUseField == null || fusionPendingField == null || setWarehouse == null)
                return 3;

            object oldBagIntercept = bagIntercept.GetValue(null);
            object oldFusionIntercept = fusionIntercept.GetValue(null);
            object oldEquipIntercept = equipIntercept.GetValue(null);
            double oldFusionPending = (double)fusionPendingField.GetValue(BagFusionController.Instance);
            var roots = rootsField.GetValue(null) as Dictionary<string, GameObject>;
            var savedRoots = roots != null ? new Dictionary<string, GameObject>(roots) : null;
            var bagFrames = new List<byte[]>();
            var fusionFrames = new List<byte[]>();
            var equipFrames = new List<byte[]>();
            GameObject canvasGo = null;
            GameObject cameraGo = null;
            GameObject eventSystemGo = null;
            GameObject prefabRoot = null;
            RenderTexture warmup = null;
            try
            {
                bagIntercept.SetValue(null, new Func<byte[], bool>(frame => { bagFrames.Add(frame); return true; }));
                fusionIntercept.SetValue(null, new Func<byte[], bool>(frame => { fusionFrames.Add(frame); return true; }));
                equipIntercept.SetValue(null, new Func<byte[], bool>(frame => { equipFrames.Add(frame); return true; }));

                bool protocolOk = VerifyProtocolFrames(bagFrames, fusionFrames, equipFrames);
                fusionPendingField.SetValue(BagFusionController.Instance, 0d);
                ((HashSet<long>)pendingUseField.GetValue(BagController.Instance)).Clear();

                BagModel.Instance.Clear();
                setWarehouse.Invoke(BagModel.Instance, new object[] { 0, 72, new List<BagGoods>() });
                ItemUseFlow.Reset();

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null || roots == null) return 3;

                canvasGo = new GameObject("BagInteractionCase_Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(720f, 1280f);
                Canvas canvas = canvasGo.GetComponent<Canvas>();
                cameraGo = new GameObject("BagInteractionCase_Camera", typeof(Camera));
                Camera camera = cameraGo.GetComponent<Camera>();
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.orthographic = true;
                camera.orthographicSize = 640f;
                camera.aspect = 720f / 1280f;
                camera.pixelRect = new Rect(0f, 0f, 720f, 1280f);
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                GraphicRaycaster raycaster = canvasGo.GetComponent<GraphicRaycaster>();
                raycaster.ignoreReversedGraphics = false;
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem == null)
                {
                    eventSystemGo = new GameObject("BagInteractionCase_EventSystem", typeof(EventSystem));
                    eventSystem = eventSystemGo.GetComponent<EventSystem>();
                }

                prefabRoot = PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform) as GameObject;
                if (prefabRoot == null) return 3;
                BagComponentView bagView = prefabRoot.GetComponentInChildren<BagComponentView>(true);
                WarehouseView warehouseView = prefabRoot.GetComponentInChildren<WarehouseView>(true);
                ExpandBagView expandView = prefabRoot.GetComponentInChildren<ExpandBagView>(true);
                OneKeyUseView useView = prefabRoot.GetComponentInChildren<OneKeyUseView>(true);
                BagSmeltView smeltView = prefabRoot.GetComponentInChildren<BagSmeltView>(true);
                if (bagView == null || warehouseView == null || expandView == null || useView == null || smeltView == null)
                    return 3;

                foreach (BaseView view in prefabRoot.GetComponentsInChildren<BaseView>(true))
                    view.gameObject.SetActive(false);
                prefabRoot.SetActive(true);
                roots.Clear();
                roots["BagModule"] = prefabRoot;
                Transform bagItemTemplate = prefabRoot.transform.Find("bagItemRenderer");
                if (bagItemTemplate != null)
                    bagView.SetItemTemplate(bagItemTemplate.GetComponent<BagItemRenderer>());

                warmup = new RenderTexture(720, 1280, 24, RenderTextureFormat.ARGB32);
                if (!warmup.Create()) return 3;

                bagView.gameObject.SetActive(true);
                bagView.Show();
                Warmup(canvas, camera, warmup);

                equipFrames.Clear();
                bool oneKeyClick = Click(bagView.onekeyBtn, camera, raycaster, eventSystem);
                bool oneKeyFrame = FrameContainerInfo(equipFrames, BagModel.POS_EQUIP);

                fusionFrames.Clear();
                bool smeltClick = Click(bagView.smeltBtn, camera, raycaster, eventSystem);
                bool smeltOpen = smeltView.IsShown;
                bool smeltFrame = FrameEmpty(fusionFrames, Proto.BAG_FUSION_INFO);
                smeltView.Hide();

                bool expandClick = Click(bagView.expandBtn, camera, raycaster, eventSystem);
                bool expandOpen = expandView.IsShown && ReadBagPos(expandView) == BagModel.POS_BAG;
                expandView.Hide();

                bool useClick = Click(bagView.useBtn, camera, raycaster, eventSystem);
                bool useOpen = useView.IsShown;
                useView.Hide();
                bool bagClicks = oneKeyClick && oneKeyFrame && smeltClick && smeltOpen && smeltFrame
                    && expandClick && expandOpen && useClick && useOpen;
                bool bagSuitDisabled = AllRaycastsDisabled(bagView.redequipBtn);
                Debug.Log("CLIVERIFY bag-interaction bag oneKey=" + oneKeyClick + "/" + oneKeyFrame
                    + " smelt=" + smeltClick + "/" + smeltOpen + "/" + smeltFrame
                    + " expand=" + expandClick + "/" + expandOpen + " use=" + useClick + "/" + useOpen);
                bagView.Hide();

                warehouseView.gameObject.SetActive(true);
                warehouseView.Show();
                Warmup(canvas, camera, warmup);
                bool warehouseExpand1Click = Click(warehouseView.expandBtn1, camera, raycaster, eventSystem);
                bool warehouseExpand1Open = expandView.IsShown && ReadBagPos(expandView) == BagModel.POS_WAREHOUSE;
                expandView.Hide();
                bool warehouseExpand2Click = Click(warehouseView.expandBtn2, camera, raycaster, eventSystem);
                bool warehouseExpand2Open = expandView.IsShown && ReadBagPos(expandView) == BagModel.POS_BAG;
                expandView.Hide();

                fusionFrames.Clear();
                bool warehouseSmeltClick = Click(warehouseView.smeltBtn, camera, raycaster, eventSystem);
                bool warehouseSmeltOpen = smeltView.IsShown;
                bool warehouseSmeltFrame = FrameEmpty(fusionFrames, Proto.BAG_FUSION_INFO);
                smeltView.Hide();
                bool warehouseUseClick = Click(warehouseView.useBtn, camera, raycaster, eventSystem);
                bool warehouseUseOpen = useView.IsShown;
                useView.Hide();
                bool warehouseClicks = warehouseExpand1Click && warehouseExpand1Open
                    && warehouseExpand2Click && warehouseExpand2Open
                    && warehouseSmeltClick && warehouseSmeltOpen && warehouseSmeltFrame
                    && warehouseUseClick && warehouseUseOpen;
                bool warehouseSuitDisabled = AllRaycastsDisabled(warehouseView.redequipBtn);
                Debug.Log("CLIVERIFY bag-interaction warehouse expand1=" + warehouseExpand1Click + "/" + warehouseExpand1Open
                    + " expand2=" + warehouseExpand2Click + "/" + warehouseExpand2Open
                    + " smelt=" + warehouseSmeltClick + "/" + warehouseSmeltOpen + "/" + warehouseSmeltFrame
                    + " use=" + warehouseUseClick + "/" + warehouseUseOpen);

                bool pass = protocolOk && bagClicks && warehouseClicks && bagSuitDisabled && warehouseSuitDisabled;
                Debug.Log("CLIVERIFY bag-interaction protocol=" + protocolOk
                    + " bagClicks=" + bagClicks + " warehouseClicks=" + warehouseClicks
                    + " blockedSuitRaycast=" + (bagSuitDisabled && warehouseSuitDisabled) + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                bagIntercept.SetValue(null, oldBagIntercept);
                fusionIntercept.SetValue(null, oldFusionIntercept);
                equipIntercept.SetValue(null, oldEquipIntercept);
                fusionPendingField.SetValue(BagFusionController.Instance, oldFusionPending);
                ((HashSet<long>)pendingUseField.GetValue(BagController.Instance)).Clear();
                ItemUseFlow.Reset();
                BagModel.Instance.Clear();
                if (roots != null)
                {
                    roots.Clear();
                    if (savedRoots != null)
                        foreach (KeyValuePair<string, GameObject> pair in savedRoots) roots[pair.Key] = pair.Value;
                }
                if (prefabRoot != null) UnityEngine.Object.DestroyImmediate(prefabRoot);
                if (eventSystemGo != null) UnityEngine.Object.DestroyImmediate(eventSystemGo);
                if (cameraGo != null) UnityEngine.Object.DestroyImmediate(cameraGo);
                if (warmup != null)
                {
                    warmup.Release();
                    UnityEngine.Object.DestroyImmediate(warmup);
                }
                if (canvasGo != null) UnityEngine.Object.DestroyImmediate(canvasGo);
            }
        }

        private static bool VerifyProtocolFrames(List<byte[]> bagFrames, List<byte[]> fusionFrames, List<byte[]> equipFrames)
        {
            bagFrames.Clear();
            BagController.Instance.ExpandBag(BagModel.POS_WAREHOUSE, 3);
            bool expand = FrameHeader(bagFrames, Proto.BAG_EXPAND, 4)
                && U16(bagFrames[0], 6) == BagModel.POS_WAREHOUSE && U16(bagFrames[0], 8) == 3;

            bagFrames.Clear();
            BagController.Instance.MoveGoods(0x0102030405060708L, BagModel.POS_BAG, BagModel.POS_WAREHOUSE);
            bool move = FrameHeader(bagFrames, Proto.GOODS_MOVE_POS, 12)
                && U64(bagFrames[0], 6) == 0x0102030405060708UL
                && U16(bagFrames[0], 14) == BagModel.POS_BAG && U16(bagFrames[0], 16) == BagModel.POS_WAREHOUSE;

            bagFrames.Clear();
            BagController.Instance.UseGoods(0x1112131415161718L, 9);
            bool use = FrameHeader(bagFrames, Proto.USE_GOODS, 12)
                && U64(bagFrames[0], 6) == 0x1112131415161718UL && U32(bagFrames[0], 14) == 9;

            fusionFrames.Clear();
            BagFusionController.Instance.RequestInfo();
            bool fusionInfo = FrameEmpty(fusionFrames, Proto.BAG_FUSION_INFO);

            fusionFrames.Clear();
            bool accepted = BagFusionController.Instance.Fuse(new List<(long goodsId, long num)>
            {
                (0x2122232425262728L, 1),
                (0x3132333435363738L, 2),
            });
            bool fusion = accepted && FrameHeader(fusionFrames, Proto.BAG_FUSION, 26)
                && U16(fusionFrames[0], 6) == 2
                && U64(fusionFrames[0], 8) == 0x2122232425262728UL && U32(fusionFrames[0], 16) == 1
                && U64(fusionFrames[0], 20) == 0x3132333435363738UL && U32(fusionFrames[0], 28) == 2;

            equipFrames.Clear();
            EquipWearController.Instance.Wear(0x4142434445464748L);
            bool wear = FrameHeader(equipFrames, Proto.EQUIP_WEAR, 8)
                && U64(equipFrames[0], 6) == 0x4142434445464748UL;
            return expand && move && use && fusionInfo && fusion && wear;
        }

        private static async Task<int> VerifyItemTipsClicksAsync()
        {
            FieldInfo bagIntercept = typeof(BagController).GetField("s_outboundIntercept", SF);
            FieldInfo equipIntercept = typeof(EquipWearController).GetField("s_outboundIntercept", SF);
            FieldInfo pendingUseField = typeof(BagController).GetField("_pendingUse", F);
            FieldInfo itemTipsRootField = typeof(ItemTipsView).GetField("_moduleRoot", SF);
            MethodInfo setEquipment = typeof(BagModel).GetMethod("SetEquipmentFull", F);
            object oldBagIntercept = bagIntercept.GetValue(null);
            object oldEquipIntercept = equipIntercept.GetValue(null);
            var bagFrames = new List<byte[]>();
            var equipFrames = new List<byte[]>();
            GameObject canvasGo = null;
            GameObject cameraGo = null;
            GameObject eventSystemGo = null;
            RenderTexture renderTarget = null;
            GameObject underlyingButton = null;
            int oldNewestTaskId = TaskModel.Instance.NewestFinishTaskId;
            try
            {
                bagIntercept.SetValue(null, new Func<byte[], bool>(frame => { bagFrames.Add(frame); return true; }));
                equipIntercept.SetValue(null, new Func<byte[], bool>(frame => { equipFrames.Add(frame); return true; }));
                ((HashSet<long>)pendingUseField.GetValue(BagController.Instance)).Clear();

                canvasGo = new GameObject("BagTipsCase_Canvas", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasGo.GetComponent<Canvas>();
                cameraGo = new GameObject("BagTipsCase_Camera", typeof(Camera));
                Camera camera = cameraGo.GetComponent<Camera>();
                renderTarget = new RenderTexture(720, 1280, 24, RenderTextureFormat.ARGB32);
                if (!renderTarget.Create()) return 3;
                camera.targetTexture = renderTarget;
                camera.clearFlags = CameraClearFlags.SolidColor;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(720f, 1280f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                GraphicRaycaster raycaster = canvasGo.GetComponent<GraphicRaycaster>();
                var layers = new LayerManager();
                layers.Init(canvas);
                ViewManager.Init(layers);
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem == null)
                {
                    eventSystemGo = new GameObject("BagTipsCase_EventSystem", typeof(EventSystem));
                    eventSystem = eventSystemGo.GetComponent<EventSystem>();
                }

                const int normalTypeId = 520100;
                GoodsModel.GoodsBasic normalBasic = GoodsModel.GetGoodsBasicByTypeId(normalTypeId);
                if (normalBasic == null || normalBasic.Use == 0) return 3;
                var normal = new BagGoods { GoodsId = 0x5100000000000001L, TypeId = normalTypeId, GoodsNum = 1, Cell = 1 };
                BagModel.Instance.SetBagFull(1, 40, new List<BagGoods> { normal });

                bagFrames.Clear();
                ItemTipsView.Show(normal);
                GoodsTooltipsBind goodsView = await WaitActiveView<GoodsTooltipsBind>(camera);
                GameObject itemTipsRoot = (GameObject)itemTipsRootField.GetValue(null);
                ItemUseViewBind siblingItemUse = itemTipsRoot != null
                    ? itemTipsRoot.GetComponentInChildren<ItemUseViewBind>(true)
                    : null;
                bool noSiblingLeak = siblingItemUse != null && !siblingItemUse.gameObject.activeInHierarchy;
                bool detailText = goodsView != null && goodsView.intro != null && !string.IsNullOrEmpty(goodsView.intro.text);
                bool useClick = goodsView != null && Click(goodsView.useBtn, camera, raycaster, eventSystem);
                bool useFrame = FrameHeader(bagFrames, Proto.USE_GOODS, 12)
                    && U64(bagFrames[0], 6) == (ulong)normal.GoodsId && U32(bagFrames[0], 14) == 1;
                bool closeDeactivatesModule = itemTipsRoot != null && !itemTipsRoot.activeInHierarchy;

                const int wornTypeId = 101015142;
                const int candidateTypeId = 101011010;
                GoodsModel.GoodsBasic wornBasic = GoodsModel.GetGoodsBasicByTypeId(wornTypeId);
                GoodsModel.GoodsBasic candidateBasic = GoodsModel.GetGoodsBasicByTypeId(candidateTypeId);
                if (wornBasic == null || candidateBasic == null || !GoodsModel.IsEquip(candidateTypeId)
                    || wornBasic.EquipType != candidateBasic.EquipType) return 3;
                var worn = new BagGoods
                {
                    GoodsId = 0x5200000000000001L, TypeId = wornTypeId, GoodsNum = 1,
                    Cell = wornBasic.EquipType, Rating = 331970, Stren = 180,
                    ExtraAttrs = new List<EquipExtraAttr>
                    {
                        new EquipExtraAttr { Color = 5, AttrId = 17, AttrVal = 250, AttrTypeId = 0 },
                    },
                };
                var candidate = new BagGoods
                {
                    GoodsId = 0x5200000000000002L, TypeId = candidateTypeId, GoodsNum = 1,
                    Cell = 2, Rating = 700,
                };
                setEquipment.Invoke(BagModel.Instance, new object[] { 10, new List<BagGoods> { worn } });
                BagModel.Instance.SetBagFull(1, 40, new List<BagGoods> { candidate });
                TaskModel.Instance.SetNewestFinishTaskId(101170);
                GoodsDynamicModel.Instance.Store(new GoodsDetailVo
                {
                    GoodsId = worn.GoodsId,
                    TypeId = worn.TypeId,
                    Stren = worn.Stren,
                    Rating = worn.Rating,
                    ExtraAttrs = worn.ExtraAttrs,
                    WashAttrs = new List<GoodsWashAttr>
                    {
                        new GoodsWashAttr { Color = 1, AttrId = 9, AttrVal = 250 },
                    },
                    StoneList = new List<GoodsStoneSlot>
                    {
                        new GoodsStoneSlot { Pos = 1, TypeId = 14010001 },
                        new GoodsStoneSlot { Pos = 2, TypeId = 14020001 },
                    },
                });

                equipFrames.Clear();
                ItemTipsView.Show(candidate);
                await WaitActiveView<EquipToolTipsBind>(camera);
                itemTipsRoot = (GameObject)itemTipsRootField.GetValue(null);
                ItemTipsModalLayout tipsLayout = itemTipsRoot != null ? itemTipsRoot.GetComponent<ItemTipsModalLayout>() : null;
                EquipToolTipsBind currentView = tipsLayout?.compareCurrent;
                EquipToolTipsBind candidateView = tipsLayout?.compareCandidate;
                bool compare = tipsLayout != null && tipsLayout.compareBlocker.gameObject.activeInHierarchy
                    && currentView != null && currentView.gameObject.activeInHierarchy
                    && candidateView != null && candidateView.gameObject.activeInHierarchy
                    && !tipsLayout.equipSingle.gameObject.activeInHierarchy
                    && ((RectTransform)currentView.transform).anchoredPosition == new Vector2(-125f, 80f)
                    && ((RectTransform)candidateView.transform).anchoredPosition == new Vector2(150f, -105f)
                    && currentView.closeBtn.gameObject.activeInHierarchy && !currentView.replaceBtn.gameObject.activeInHierarchy
                    && candidateView.replaceBtn.gameObject.activeInHierarchy && !candidateView.closeBtn.gameObject.activeInHierarchy
                    && currentView.score.text.Contains("331970") && candidateView.score.text.Contains("700")
                    && currentView.basePro.text.Contains("24550") && candidateView.basePro.text.Contains("50")
                    && currentView._html_stren.text.Contains("强化 +1800")
                    && currentView.best_conta.gameObject.activeInHierarchy
                    && currentView.spec_conta.gameObject.activeInHierarchy
                    && currentView.wash_conta.gameObject.activeInHierarchy
                    && !currentView.basePro.text.Contains("装备对比") && !candidateView.basePro.text.Contains("装备对比");

                bool configGroups = EquipmentTipsConfig.GetStoneUnlock(1, 2).Stage == 5
                    && EquipmentTipsConfig.GetStoneUnlock(1, 6).Vip == 4
                    && EquipmentTipsConfig.GetStoneAttrs(14010001).Count == 2
                    && currentView.stone_conta.gameObject.activeInHierarchy
                    && candidateView.stone_conta.gameObject.activeInHierarchy
                    && currentView.stone_pro_conta.childCount == 6
                    && candidateView.stone_pro_conta.childCount == 6
                    && currentView.content_scroll.GetComponent<RectMask2D>() != null
                    && candidateView.content_scroll.GetComponent<RectMask2D>() != null
                    && Mathf.Abs(currentView.content_group.anchoredPosition.y) < 0.1f
                    && Mathf.Abs(candidateView.content_group.anchoredPosition.y) < 0.1f;
                await Task.Delay(500);
                bool compareIcons = IsEquipmentIconVisible(tipsLayout?.compareCurrentIcon, "1011401")
                    && IsEquipmentIconVisible(tipsLayout?.compareCandidateIcon, "1010101");
                string comparePng = Capture(camera, renderTarget, "Temp/itemtips_equip_compare.png");
                bool compareCaptured = !string.IsNullOrEmpty(comparePng) && File.Exists(comparePng);
                bool wearClick = candidateView != null && Click(candidateView.replaceBtn, camera, raycaster, eventSystem);
                bool wearFrame = FrameHeader(equipFrames, Proto.EQUIP_WEAR, 8)
                    && U64(equipFrames[0], 6) == (ulong)candidate.GoodsId;

                int underlyingClicks = 0;
                underlyingButton = new GameObject("UnderlyingBagClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform underlyingRect = (RectTransform)underlyingButton.transform;
                underlyingRect.SetParent(ViewManager.GetLayer(UILayer.Window), false);
                underlyingRect.anchorMin = Vector2.zero;
                underlyingRect.anchorMax = Vector2.one;
                underlyingRect.offsetMin = Vector2.zero;
                underlyingRect.offsetMax = Vector2.zero;
                Image underlyingImage = underlyingButton.GetComponent<Image>();
                underlyingImage.color = new Color(1f, 1f, 1f, 0f);
                underlyingImage.raycastTarget = true;
                UIUtil.AddClick(underlyingImage, () => underlyingClicks++);

                ItemTipsView.Show(candidate);
                await WaitActiveView<EquipToolTipsBind>(camera);
                itemTipsRoot = (GameObject)itemTipsRootField.GetValue(null);
                tipsLayout = itemTipsRoot != null ? itemTipsRoot.GetComponent<ItemTipsModalLayout>() : null;
                bool blockerClick = tipsLayout != null && ClickBlockerCorner(tipsLayout.compareBlocker,
                    camera, raycaster, eventSystem);
                bool blockerClosedFirst = blockerClick && underlyingClicks == 0
                    && itemTipsRoot != null && !itemTipsRoot.activeInHierarchy;

                bagFrames.Clear();
                ItemTipsView.ShowWarehouse(normal, false);
                goodsView = await WaitActiveView<GoodsTooltipsBind>(camera);
                bool depositClick = goodsView != null && Click(goodsView.depositBtn, camera, raycaster, eventSystem);
                bool depositFrame = FrameHeader(bagFrames, Proto.GOODS_MOVE_POS, 12)
                    && U64(bagFrames[0], 6) == (ulong)normal.GoodsId
                    && U16(bagFrames[0], 14) == BagModel.POS_BAG
                    && U16(bagFrames[0], 16) == BagModel.POS_WAREHOUSE;

                bagFrames.Clear();
                ItemTipsView.ShowWarehouse(normal, true);
                goodsView = await WaitActiveView<GoodsTooltipsBind>(camera);
                bool takeoutClick = goodsView != null && Click(goodsView.takeoutBtn, camera, raycaster, eventSystem);
                bool takeoutFrame = FrameHeader(bagFrames, Proto.GOODS_MOVE_POS, 12)
                    && U64(bagFrames[0], 6) == (ulong)normal.GoodsId
                    && U16(bagFrames[0], 14) == BagModel.POS_WAREHOUSE
                    && U16(bagFrames[0], 16) == BagModel.POS_BAG;

                bool pass = noSiblingLeak && closeDeactivatesModule
                    && detailText && useClick && useFrame && compare && compareIcons && configGroups && compareCaptured && wearClick && wearFrame
                    && blockerClosedFirst
                    && depositClick && depositFrame && takeoutClick && takeoutFrame;
                Debug.Log("CLIVERIFY bag-interaction tips isolated=" + noSiblingLeak
                    + " closeRoot=" + closeDeactivatesModule + " detail=" + detailText
                    + " use=" + useClick + "/" + useFrame
                    + " compare=" + compare + " icons=" + compareIcons
                    + " configGroups=" + configGroups + " screenshot=" + comparePng
                    + " wear=" + wearClick + "/" + wearFrame + " blocker=" + blockerClosedFirst
                    + " deposit=" + depositClick + "/" + depositFrame
                    + " takeout=" + takeoutClick + "/" + takeoutFrame + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                ItemTipsView.Close();
                bagIntercept.SetValue(null, oldBagIntercept);
                equipIntercept.SetValue(null, oldEquipIntercept);
                ((HashSet<long>)pendingUseField.GetValue(BagController.Instance)).Clear();
                BagModel.Instance.Clear();
                GoodsDynamicModel.Instance.Clear();
                TaskModel.Instance.SetNewestFinishTaskId(oldNewestTaskId);
                ViewManager.Init(null);
                if (underlyingButton != null) UnityEngine.Object.DestroyImmediate(underlyingButton);
                if (eventSystemGo != null) UnityEngine.Object.DestroyImmediate(eventSystemGo);
                if (cameraGo != null) UnityEngine.Object.DestroyImmediate(cameraGo);
                if (canvasGo != null) UnityEngine.Object.DestroyImmediate(canvasGo);
                if (renderTarget != null)
                {
                    renderTarget.Release();
                    UnityEngine.Object.DestroyImmediate(renderTarget);
                }
            }
        }

        private static async Task<T> WaitActiveView<T>(Camera camera) where T : BaseView
        {
            double deadline = EditorApplication.timeSinceStartup + 5d;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                Canvas.ForceUpdateCanvases();
                camera.Render();
                foreach (T view in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (view != null && view.gameObject.activeInHierarchy) return view;
                await Task.Delay(50);
            }
            return null;
        }

        private static bool Click(Component target, Camera camera, GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                Debug.LogError("CLIVERIFY bag-interaction click target missing/inactive");
                return false;
            }
            Image surface = target.GetComponent<Image>();
            if (surface == null || !surface.raycastTarget)
            {
                Debug.LogError("CLIVERIFY bag-interaction click surface missing/disabled target=" + target.name);
                return false;
            }
            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
                if (graphic != surface && graphic.raycastTarget)
                {
                    Debug.LogError("CLIVERIFY bag-interaction decorative raycast target=" + target.name + " child=" + graphic.name);
                    return false;
                }

            Canvas.ForceUpdateCanvases();
            RectTransform rect = target.transform as RectTransform;
            if (rect == null) return false;
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            foreach (RaycastResult hit in hits)
            {
                if (hit.gameObject != target.gameObject) continue;
                ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(hit.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                return true;
            }
            Debug.LogError("CLIVERIFY bag-interaction raycast miss target=" + target.name
                + " screen=" + pointer.position + " rect=" + rect.rect + " hits="
                + string.Join(",", hits.ConvertAll(h => h.gameObject != null ? h.gameObject.name : "null")));
            return false;
        }

        private static bool ClickBlockerCorner(Image blocker, Camera camera,
            GraphicRaycaster raycaster, EventSystem eventSystem)
        {
            if (blocker == null || !blocker.gameObject.activeInHierarchy || !blocker.raycastTarget) return false;
            RectTransform rect = blocker.rectTransform;
            Vector3 world = rect.TransformPoint(new Vector3(rect.rect.xMin + 12f, rect.rect.yMin + 12f, 0f));
            var pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, world),
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            if (hits.Count == 0 || hits[0].gameObject != blocker.gameObject)
            {
                Debug.LogError("CLIVERIFY bag-interaction blocker not first hit: "
                    + string.Join(",", hits.ConvertAll(h => h.gameObject != null ? h.gameObject.name : "null")));
                return false;
            }
            ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(blocker.gameObject, pointer,
                ExecuteEvents.pointerClickHandler);
            return true;
        }

        private static void Warmup(Canvas canvas, Camera camera, RenderTexture target)
        {
            canvas.enabled = false;
            canvas.enabled = true;
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = null;
        }

        private static string Capture(Camera camera, RenderTexture target, string relativePath)
        {
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            string fullPath = Path.GetFullPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            return fullPath;
        }

        private static int ReadBagPos(ExpandBagView view) =>
            (int)typeof(ExpandBagView).GetField("_bagPos", F).GetValue(view);

        private static bool AllRaycastsDisabled(Component target)
        {
            if (target == null) return false;
            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
                if (graphic.raycastTarget) return false;
            return true;
        }

        private static bool IsEquipmentIconVisible(EquipmentItem item, string expectedSprite)
        {
            return item != null && item.gameObject.activeInHierarchy
                && item.item_bg != null && item.item_bg.enabled && item.item_bg.sprite != null
                && item.icon != null && item.icon.enabled && item.icon.sprite != null
                && item.icon.sprite.name == expectedSprite;
        }

        private static bool FrameContainerInfo(IReadOnlyList<byte[]> frames, int pos) =>
            FrameHeader(frames, Proto.GOODS_CONTAINER_INFO, 2) && U16(frames[0], 6) == pos;

        private static bool FrameEmpty(IReadOnlyList<byte[]> frames, int command) => FrameHeader(frames, command, 0);

        private static bool FrameHeader(IReadOnlyList<byte[]> frames, int command, int payloadLength)
        {
            if (frames.Count != 1 || frames[0] == null) return false;
            byte[] frame = frames[0];
            return frame.Length == 6 + payloadLength && U16(frame, 0) == frame.Length
                && U16(frame, 2) == 1000 && U16(frame, 4) == command;
        }

        private static ushort U16(byte[] data, int offset) =>
            (ushort)((data[offset] << 8) | data[offset + 1]);

        private static uint U32(byte[] data, int offset) =>
            ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];

        private static ulong U64(byte[] data, int offset) =>
            ((ulong)U32(data, offset) << 32) | U32(data, offset + 4);
    }
}
