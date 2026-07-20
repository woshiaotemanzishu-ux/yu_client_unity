using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.PetEquip;
using Shenxiao.Module.Core.PetEquip.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 侍魂装备三页 UI 实证：Creator 产物静态契约、四个固定穿戴槽、坐骑/伙伴容器隔离，
    /// 以及强化和打造候选筛选。协议 wire 已由 PetEquipCase 覆盖，本 Case 不重复拦截发送层。
    /// </summary>
    public static class PetEquipUiCase
    {
        private const string PREFAB_PATH = "Assets/Prefabs/UI/PetEquip/PetEquipModule.prefab";
        private const BindingFlags INSTANCE_PUBLIC = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags INSTANCE_NON_PUBLIC = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            bool editorPreferFallbackBefore = Shenxiao.Framework.Res.ResManager.EditorPreferFallback;
            Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;
            try
            {
                return await RunCore();
            }
            finally
            {
                PetEquipModel.Instance.Clear();
                BagModel.Instance.Clear();
                Shenxiao.Framework.Res.ResManager.EditorPreferFallback = editorPreferFallbackBefore;
            }
        }

        private static async Task<int> RunCore()
        {
            Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
            await PetEquipConfigs.EnsureLoaded();
            await Shenxiao.Module.Core.Common.GoodsModel.EnsureLoaded();

            // Case 代码可以重建产物；本文件开发阶段不执行该入口，因此不会产生工作区 prefab/meta 副作用。
            Shenxiao.Editor.UiCreator.PetEquip.PetEquipCreator.Generate();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError("CLIVERIFY petEquipUI prefab missing after Creator.Generate: " + PREFAB_PATH);
                return 3;
            }

            bool rootOk = prefab.transform.childCount == 3;
            PetEquipPageView[] assetPages = prefab.GetComponentsInChildren<PetEquipPageView>(true);
            bool pageCountOk = assetPages.Length == 3;
            bool bagUnique = false;
            bool strengthenUnique = false;
            bool polishUnique = false;
            bool fieldsOk = pageCountOk;
            bool initialStateOk = pageCountOk;
            for (int i = 0; i < assetPages.Length; i++)
            {
                PetEquipPageView page = assetPages[i];
                RectTransform rt = page.transform as RectTransform;
                rootOk &= page.transform.parent == prefab.transform && rt != null
                    && Mathf.Abs(rt.sizeDelta.x - 720f) < 0.01f
                    && Mathf.Abs(rt.sizeDelta.y - 992f) < 0.01f;
                initialStateOk &= !page.gameObject.activeSelf;
                fieldsOk &= PageFieldsOk(page);

                switch (page.mode)
                {
                    case PetEquipPageMode.Bag:
                        if (bagUnique) fieldsOk = false;
                        bagUnique = true;
                        break;
                    case PetEquipPageMode.Strengthen:
                        if (strengthenUnique) fieldsOk = false;
                        strengthenUnique = true;
                        break;
                    case PetEquipPageMode.Polish:
                        if (polishUnique) fieldsOk = false;
                        polishUnique = true;
                        break;
                    default:
                        fieldsOk = false;
                        break;
                }
            }
            bool modesOk = bagUnique && strengthenUnique && polishUnique;
            Debug.Log("CLIVERIFY petEquipUI prefab root=" + rootOk + " pages=" + pageCountOk
                + " modes=" + modesOk + " fields=" + fieldsOk + " initialInactive=" + initialStateOk);

            MethodInfo setContainerFull = typeof(BagModel).GetMethod("SetPetEquipContainerFull", INSTANCE_NON_PUBLIC);
            bool modelApiOk = setContainerFull != null;
            if (!modelApiOk)
            {
                Debug.LogError("CLIVERIFY petEquipUI BagModel.SetPetEquipContainerFull missing");
                return 3;
            }

            SeedProductModels(setContainerFull);

            CliVerify.Stage stage = null;
            GameObject instance = null;
            try
            {
                stage = CliVerify.Stage.Create();
                instance = Object.Instantiate(prefab, stage.CanvasRoot);
                instance.name = "PetEquipModule(CliVerify)";
                instance.SetActive(true);

                PetEquipPageView[] pages = instance.GetComponentsInChildren<PetEquipPageView>(true);
                PetEquipPageView bagPage = FindPage(pages, PetEquipPageMode.Bag);
                PetEquipPageView strengthenPage = FindPage(pages, PetEquipPageMode.Strengthen);
                PetEquipPageView polishPage = FindPage(pages, PetEquipPageMode.Polish);
                bool runtimePagesOk = bagPage != null && strengthenPage != null && polishPage != null;
                if (!runtimePagesOk)
                {
                    Debug.LogError("CLIVERIFY petEquipUI runtime pages missing");
                    return 3;
                }

                bool bagOk = await VerifyBagPage(bagPage);
                bool strengthenOk = await VerifyStrengthenPage(strengthenPage);
                bool polishOk = await VerifyPolishPage(polishPage);

                // 空数据/非法选择必须清空消耗选择，不保留上一类型或上一目标的幽灵状态。
                PetEquipModel.Instance.Clear();
                BagModel.Instance.Clear();
                ShowOnly(pages, strengthenPage, 1);
                bool emptyRefreshApi = InvokeNoArg(strengthenPage, "RefreshNow");
                bool emptyOk = emptyRefreshApi
                    && ReadInt(strengthenPage, "ActiveSlotCount") == 4
                    && ReadInt(strengthenPage, "SelectedCostCount") == 0
                    && ActiveGoodsCount(strengthenPage) == 0;

                // 截图只展示真实种子数据；重新灌入后显示 Bag 页，避免三页叠加。
                SeedProductModels(setContainerFull);
                ShowOnly(pages, bagPage, 1);
                InvokeNoArg(bagPage, "RefreshNow");
                await Task.Delay(100);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round25_petequip_ui.png");

                bool pass = rootOk && pageCountOk && modesOk && fieldsOk && initialStateOk && modelApiOk
                    && runtimePagesOk && bagOk && strengthenOk && polishOk && emptyOk;
                Debug.Log("CLIVERIFY petEquipUI VERDICT prefab="
                    + (rootOk && pageCountOk && modesOk && fieldsOk && initialStateOk)
                    + " bag=" + bagOk + " strengthen=" + strengthenOk + " polish=" + polishOk
                    + " empty=" + emptyOk + " shot=" + png + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (instance != null)
                {
                    PetEquipPageView[] pages = instance.GetComponentsInChildren<PetEquipPageView>(true);
                    for (int i = 0; i < pages.Length; i++)
                    {
                        if (pages[i].IsShown) pages[i].Hide();
                    }
                    Object.DestroyImmediate(instance);
                }
                if (stage != null) stage.Dispose();
                PetEquipModel.Instance.Clear();
                BagModel.Instance.Clear();
            }
        }

        private static bool PageFieldsOk(PetEquipPageView page)
        {
            bool pageFields = page != null
                && page.lblHeading != null && page.lblCombat != null && page.lblSummary != null
                && page.lblEmpty != null && page.lblAction != null && page.lblSelectAll != null
                && page.btnAction != null && page.btnSelectAll != null
                && page.wornContent != null && page.goodsContent != null
                && page.slotTemplate != null && page.goodsTemplate != null;
            if (!pageFields)
            {
                Debug.LogError("CLIVERIFY petEquipUI fields missing page=" + (page != null ? page.mode.ToString() : "null")
                    + " heading=" + (page != null && page.lblHeading != null)
                    + " combat=" + (page != null && page.lblCombat != null)
                    + " summary=" + (page != null && page.lblSummary != null)
                    + " empty=" + (page != null && page.lblEmpty != null)
                    + " actionLabel=" + (page != null && page.lblAction != null)
                    + " selectAllLabel=" + (page != null && page.lblSelectAll != null)
                    + " action=" + (page != null && page.btnAction != null)
                    + " selectAll=" + (page != null && page.btnSelectAll != null)
                    + " worn=" + (page != null && page.wornContent != null)
                    + " goods=" + (page != null && page.goodsContent != null)
                    + " slotTemplate=" + (page != null && page.slotTemplate != null)
                    + " goodsTemplate=" + (page != null && page.goodsTemplate != null));
                return false;
            }

            PetEquipSlotRowView slot = page.slotTemplate;
            PetEquipGoodsRowView goods = page.goodsTemplate;
            bool slotOk = !slot.gameObject.activeSelf && slot.click != null && slot.background != null
                && slot.selectedMark != null && slot.lblPosition != null && slot.lblDetail != null
                && slot.click.raycastTarget;
            bool goodsOk = !goods.gameObject.activeSelf && goods.click != null && goods.background != null
                && goods.selectedMark != null && goods.lblName != null && goods.lblDetail != null
                && goods.click.raycastTarget;
            if (!slotOk || !goodsOk)
            {
                Debug.LogError("CLIVERIFY petEquipUI template invalid page=" + page.mode
                    + " slot=" + slotOk + " slotInactive=" + !slot.gameObject.activeSelf
                    + " slotClick=" + (slot.click != null) + " slotRaycast=" + (slot.click != null && slot.click.raycastTarget)
                    + " goods=" + goodsOk + " goodsInactive=" + !goods.gameObject.activeSelf
                    + " goodsClick=" + (goods.click != null) + " goodsRaycast=" + (goods.click != null && goods.click.raycastTarget));
            }
            return slotOk && goodsOk;
        }

        private static async Task<bool> VerifyBagPage(PetEquipPageView page)
        {
            bool type1Api = ShowAndRefresh(page, 1);
            await Task.Yield();
            int type1Slots = ReadInt(page, "ActiveSlotCount");
            int type1Goods = ActiveGoodsCount(page);
            int type1Selected = ReadInt(page, "SelectedCostCount");

            bool type2Api = SetTypeAndRefresh(page, 2);
            int type2Slots = ReadInt(page, "ActiveSlotCount");
            int type2Goods = ActiveGoodsCount(page);
            int type2Selected = ReadInt(page, "SelectedCostCount");

            bool returnApi = SetTypeAndRefresh(page, 1);
            int returnGoods = ActiveGoodsCount(page);
            bool ok = type1Api && type2Api && returnApi
                && type1Slots == 4 && type2Slots == 4
                && type1Goods == 4 && type2Goods == 1 && returnGoods == 4
                && type1Selected == 0 && type2Selected == 0;
            Debug.Log("CLIVERIFY petEquipUI bag type1Slots=" + type1Slots + " type1Goods=" + type1Goods
                + " type2Slots=" + type2Slots + " type2Goods=" + type2Goods
                + " returnGoods=" + returnGoods + " selected=" + type1Selected + "/" + type2Selected
                + " pass=" + ok);
            return ok;
        }

        private static async Task<bool> VerifyStrengthenPage(PetEquipPageView page)
        {
            bool apiOk = ShowAndRefresh(page, 1);
            int slots = ReadInt(page, "ActiveSlotCount");
            int initialCosts = ReadInt(page, "SelectedCostCount");
            bool targetClick = ClickFirstSlot(page);
            await Task.Yield();
            int type1Candidates = ActiveGoodsCount(page);
            bool costClick = ClickFirstGoods(page);
            int selectedAfterClick = ReadInt(page, "SelectedCostCount");

            bool type2Api = SetTypeAndRefresh(page, 2);
            int afterSwitchSelected = ReadInt(page, "SelectedCostCount");
            bool target2Click = ClickFirstSlot(page);
            await Task.Yield();
            int type2Candidates = ActiveGoodsCount(page);

            // type1:同部位评分 900/800 两件可强化；跨部位高评分和未穿戴部位必须排除。type2 只有一件。
            bool ok = apiOk && type2Api && targetClick && target2Click && costClick
                && slots == 4 && initialCosts == 0 && selectedAfterClick == 1 && afterSwitchSelected == 0
                && type1Candidates == 2 && type2Candidates == 1;
            Debug.Log("CLIVERIFY petEquipUI strengthen slots=" + slots + " type1Candidates=" + type1Candidates
                + " type2Candidates=" + type2Candidates + " selected=" + initialCosts + "→" + selectedAfterClick
                + " switchSelected=" + afterSwitchSelected + " pass=" + ok);
            return ok;
        }

        private static async Task<bool> VerifyPolishPage(PetEquipPageView page)
        {
            bool apiOk = ShowAndRefresh(page, 1);
            int slots = ReadInt(page, "ActiveSlotCount");
            int initialCosts = ReadInt(page, "SelectedCostCount");
            bool targetClick = ClickFirstSlot(page);
            await Task.Yield();
            int type1Candidates = ActiveGoodsCount(page);
            bool costClick = ClickFirstGoods(page);
            int selectedAfterClick = ReadInt(page, "SelectedCostCount");

            bool type2Api = SetTypeAndRefresh(page, 2);
            int afterSwitchSelected = ReadInt(page, "SelectedCostCount");
            bool target2Click = ClickFirstSlot(page);
            await Task.Yield();
            int type2Candidates = ActiveGoodsCount(page);

            // 同部位且 star 从 1 升到 2 的一件可打造；同阶同星、跨部位均不得混入。
            bool ok = apiOk && type2Api && targetClick && target2Click && costClick
                && slots == 4 && initialCosts == 0 && selectedAfterClick == 1 && afterSwitchSelected == 0
                && type1Candidates == 1 && type2Candidates == 1;
            Debug.Log("CLIVERIFY petEquipUI polish slots=" + slots + " type1Candidates=" + type1Candidates
                + " type2Candidates=" + type2Candidates + " selected=" + initialCosts + "→" + selectedAfterClick
                + " switchSelected=" + afterSwitchSelected + " pass=" + ok);
            return ok;
        }

        private static void SeedProductModels(MethodInfo setContainerFull)
        {
            PetEquipModel model = PetEquipModel.Instance;
            BagModel bag = BagModel.Instance;
            model.Clear();
            bag.Clear();

            // 故意把 pos3 放在 pos1 前，验证页面固定输出四槽且不依赖协议下发顺序。
            model.ApplyInfo(1, 3200, new List<PetEquipModel.PetEquipItem>
            {
                Item(3, 103001L, 460310101),
                Item(1, 101001L, 460110101),
            });
            model.ApplyInfo(2, 1800, new List<PetEquipModel.PetEquipItem>
            {
                Item(1, 301001L, 480110101),
            });

            SetContainer(setContainerFull, bag, BagModel.POS_HORSE, new List<BagGoods>
            {
                Goods(103001L, 460310101, 3, 1000, 1, 1),
                Goods(101001L, 460110101, 1, 1000, 1, 1),
            });
            SetContainer(setContainerFull, bag, BagModel.POS_HORSE_BAG, new List<BagGoods>
            {
                Goods(203001L, 460310101, 3, 1200, 1, 1), // pos3，高于已穿戴评分 → 强化排除
                Goods(201001L, 460110102, 1, 900, 1, 2),  // pos1，更高星 → 强化/打造均可
                Goods(201002L, 460110101, 1, 800, 1, 1),  // pos1，同阶同星 → 仅强化可
                Goods(202001L, 460210101, 2, 500, 1, 1),  // pos2 未穿戴 → 强化排除
            });
            SetContainer(setContainerFull, bag, BagModel.POS_PARTNER, new List<BagGoods>
            {
                Goods(301001L, 480110101, 1, 500, 1, 1),
            });
            SetContainer(setContainerFull, bag, BagModel.POS_PARTNER_BAG, new List<BagGoods>
            {
                Goods(401001L, 480110102, 1, 400, 1, 2),
            });
        }

        private static PetEquipModel.PetEquipItem Item(int pos, long goodsId, int goodsTypeId)
        {
            return new PetEquipModel.PetEquipItem
            {
                PosId = pos,
                PosLevel = 1,
                Stage = 1,
                Star = 1,
                PosPoint = 0,
                GoodsId = goodsId,
                GoodsTypeId = goodsTypeId,
            };
        }

        private static BagGoods Goods(long goodsId, int typeId, int cell, long rating, int stage, int star)
        {
            return new BagGoods
            {
                GoodsId = goodsId,
                TypeId = typeId,
                GoodsNum = 1,
                Cell = cell,
                Color = 1,
                Rating = rating,
                OverallRating = rating,
                EquipStage = stage,
                EquipStar = star,
            };
        }

        private static void SetContainer(MethodInfo method, BagModel bag, int pos, List<BagGoods> goods)
        {
            method.Invoke(bag, new object[] { pos, 40, goods });
        }

        private static PetEquipPageView FindPage(PetEquipPageView[] pages, PetEquipPageMode mode)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i].mode == mode) return pages[i];
            }
            return null;
        }

        private static bool ShowAndRefresh(PetEquipPageView page, int typeId)
        {
            page.Show(typeId);
            return SetTypeAndRefresh(page, typeId);
        }

        private static bool SetTypeAndRefresh(PetEquipPageView page, int typeId)
        {
            MethodInfo setType = page.GetType().GetMethod("SetType", INSTANCE_PUBLIC);
            if (setType == null)
            {
                Debug.LogError("CLIVERIFY petEquipUI " + page.mode + " SetType missing");
                return false;
            }
            setType.Invoke(page, new object[] { typeId });
            return InvokeNoArg(page, "RefreshNow");
        }

        private static bool InvokeNoArg(PetEquipPageView page, string methodName)
        {
            MethodInfo method = page.GetType().GetMethod(methodName, INSTANCE_PUBLIC);
            if (method == null)
            {
                Debug.LogError("CLIVERIFY petEquipUI " + page.mode + " " + methodName + " missing");
                return false;
            }
            method.Invoke(page, null);
            return true;
        }

        private static int ReadInt(PetEquipPageView page, string propertyName)
        {
            PropertyInfo property = page.GetType().GetProperty(propertyName, INSTANCE_PUBLIC);
            if (property == null || property.PropertyType != typeof(int))
            {
                Debug.LogError("CLIVERIFY petEquipUI " + page.mode + " property missing: " + propertyName);
                return int.MinValue;
            }
            return (int)property.GetValue(page);
        }

        private static int ActiveGoodsCount(PetEquipPageView page)
        {
            PetEquipGoodsRowView[] rows = page.goodsContent.GetComponentsInChildren<PetEquipGoodsRowView>(true);
            int count = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] != page.goodsTemplate && rows[i].gameObject.activeSelf) count++;
            }
            return count;
        }

        private static bool ClickFirstSlot(PetEquipPageView page)
        {
            PetEquipSlotRowView[] rows = page.wornContent.GetComponentsInChildren<PetEquipSlotRowView>(true);
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == page.slotTemplate || !rows[i].gameObject.activeSelf) continue;
                Button button = rows[i].click != null ? rows[i].click.GetComponent<Button>() : null;
                if (button == null) return false;
                button.onClick.Invoke();
                return true;
            }
            return false;
        }

        private static bool ClickFirstGoods(PetEquipPageView page)
        {
            PetEquipGoodsRowView[] rows = page.goodsContent.GetComponentsInChildren<PetEquipGoodsRowView>(true);
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == page.goodsTemplate || !rows[i].gameObject.activeSelf) continue;
                Button button = rows[i].click != null ? rows[i].click.GetComponent<Button>() : null;
                if (button == null) return false;
                button.onClick.Invoke();
                return true;
            }
            return false;
        }

        private static void ShowOnly(PetEquipPageView[] pages, PetEquipPageView selected, int typeId)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] == selected) continue;
                if (pages[i].IsShown) pages[i].Hide();
                else pages[i].gameObject.SetActive(false);
            }
            if (!selected.IsShown) selected.Show(typeId);
            SetTypeAndRefresh(selected, typeId);
        }
    }
}
