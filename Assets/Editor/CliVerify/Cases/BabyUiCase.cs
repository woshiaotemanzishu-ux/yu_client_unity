using System;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Editor.UiCreator.Baby;
using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.Baby;
using Shenxiao.Module.Core.Baby;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>Baby UI 静态基线：回填后实例化两个源 prefab，验收窗口、Item Bind 与嵌套模板解析。</summary>
    public static class BabyUiCase
    {
        private const string ModulePath = "Assets/Prefabs/UI/Baby/BabyModule.prefab";
        private const string PropItemPath = "Assets/Prefabs/UI/Baby/BabyPropItem.prefab";
        private const BindingFlags BindFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        public static Task<int> Run()
        {
            bool editorPreferFallbackBefore = ResManager.EditorPreferFallback;
            try
            {
                ResManager.EditorPreferFallback = true;
                _ = BabyRaiseConfigs.EnsureLoaded();
                _ = BabyFigureConfigs.EnsureLoaded();
                _ = BabyFigureStarConfigs.EnsureLoaded();
                _ = Shenxiao.Module.Core.Common.GoodsModel.EnsureLoaded();
                BabyFigureConfigs.BabyFigureCfg figureCfg = BabyFigureConfigs.Get(1);
                BabyFigureStarConfigs.BabyFigureStarCfg starCfg = BabyFigureStarConfigs.Get(1, 2);
                bool config = BabyRaiseConfigs.IsLoaded && BabyRaiseConfigs.Get(1) != null
                    && figureCfg != null && figureCfg.ResourceId == "1011"
                    && BabyFigureConfigs.All.Count == 9
                    && figureCfg.Costs.Count > 0 && figureCfg.Costs[0].TypeId == 68010001 && figureCfg.Costs[0].Num == 30
                    && starCfg != null && starCfg.Costs.Count > 0
                    && starCfg.Costs[0].TypeId == 68010001 && starCfg.Costs[0].Num == 25;
                bool upgraded = BabyBindUpgrader.Generate();
                bool prefab = upgraded && VerifyInstances();
                bool pass = config && upgraded && prefab;
                Debug.Log("CLIVERIFY babyui VERDICT config=" + config + " upgraded=" + upgraded + " prefab=" + prefab + " pass=" + pass);
                return Task.FromResult(pass ? 0 : 3);
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY babyui EXCEPTION " + e);
                return Task.FromResult(3);
            }
            finally
            {
                ResManager.EditorPreferFallback = editorPreferFallbackBefore;
                BabyModel.Instance.Reset();
                Shenxiao.Module.Core.Bag.BagModel.Instance.Clear();
            }
        }

        public static void RunBatch()
        {
            _ = RunBatchAsync();
        }

        private static async Task RunBatchAsync()
        {
            int code = await Run();
            Debug.Log("CLIVERIFY babyui EXIT " + code);
            EditorApplication.Exit(code);
        }

        private static bool VerifyInstances()
        {
            GameObject moduleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath);
            GameObject propItemAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PropItemPath);
            if (moduleAsset == null || propItemAsset == null) return false;

            GameObject module = UnityEngine.Object.Instantiate(moduleAsset);
            GameObject propItem = UnityEngine.Object.Instantiate(propItemAsset);
            try
            {
                Shenxiao.Module.Core.Bag.BagModel.Instance.UpdateNum(1, 68010001, 35);
                bool pages = Check<GestateBabyViewBind>(module) && Check<BabyFamilyViewBind>(module)
                    && Check<BabyCultivateViewBind>(module) && Check<BabyChangedViewBind>(module)
                    && Check<BabyIllusionViewBind>(module);
                bool businessViews = module.GetComponentInChildren<GestateBabyView>(true) != null
                    && module.GetComponentInChildren<BabyFamilyView>(true) != null
                    && module.GetComponentInChildren<BabyCultivateView>(true) != null
                    && module.GetComponentInChildren<BabyIllusionView>(true) != null
                    && propItem.GetComponentInChildren<BabyPropItem>(true) != null;
                BabyPropItem propBusiness = propItem.GetComponentInChildren<BabyPropItem>(true);
                bool propDisplay = propBusiness != null;
                if (propDisplay)
                {
                    propBusiness.SetData(2, 100, true, 150);
                    propDisplay = !string.IsNullOrEmpty(propBusiness.nameLb.text)
                        && propBusiness.nextLb.text.Contains("+50") && propBusiness.arrow.gameObject.activeSelf;
                }
                BabyModel model = BabyModel.Instance;
                model.ApplyBasic(new BabyBasicInfo { BabyName = "baby" });
                BabyRaiseInfo raise = new BabyRaiseInfo { RaiseLevel = 7, RaiseExp = 11 };
                raise.TaskList.Add(new BabyTaskInfo { TaskId = 1, FinishNum = 0, FinishState = 0 });
                raise.TaskList.Add(new BabyTaskInfo { TaskId = 2, FinishNum = 3, FinishState = 1 });
                raise.TaskList.Add(new BabyTaskInfo { TaskId = 3, FinishNum = 2, FinishState = 2 });
                model.ApplyRaise(raise);
                model.ApplyStage(new BabyStageInfo { StageExp = 13 });
                BabyCultivateView cultivateView = module.GetComponentInChildren<BabyCultivateView>(true);
                bool display = cultivateView != null;
                if (display)
                {
                    cultivateView.gameObject.SetActive(true);
                    cultivateView.Show();
                    display = cultivateView.babyName.text == "baby" && cultivateView.lvLb.text == "7"
                        && cultivateView.lvExpLb.text == "11" && cultivateView.stageExpLb.text == "13";
                    display = display && VerifyTasks(cultivateView);
                    UnityEngine.UI.Button lvButton = cultivateView.lvBtnGp.GetComponent<UnityEngine.UI.Button>();
                    UnityEngine.UI.Button stageButton = cultivateView.stageBtnGp.GetComponent<UnityEngine.UI.Button>();
                    UnityEngine.UI.Button upButton = cultivateView.upBtn.GetComponent<UnityEngine.UI.Button>();
                    display = display && lvButton != null && stageButton != null && upButton != null;
                    if (display)
                    {
                        stageButton.onClick.Invoke();
                        display = cultivateView.stageGp.gameObject.activeSelf && !cultivateView.lvGp.gameObject.activeSelf;
                        lvButton.onClick.Invoke();
                        display = display && cultivateView.lvGp.gameObject.activeSelf && !cultivateView.stageGp.gameObject.activeSelf;
                    }
                    cultivateView.Hide();
                }
                BabyFamilyView familyView = module.GetComponentInChildren<BabyFamilyView>(true);
                BabyFamilyInfo family = new BabyFamilyInfo();
                family.InfoList.Add(new BabyFamilyEntry { ActiveTime = 1, BabyName = "family-a", RaiseLevel = 8, Stage = 5, StageLevel = 2, BabyPower = 600 });
                family.InfoList.Add(new BabyFamilyEntry { ActiveTime = 2, BabyName = "family-b", RaiseLevel = 9, Stage = 6, StageLevel = 3, BabyPower = 700 });
                model.ApplyFamily(family);
                bool familyDisplay = familyView != null;
                if (familyDisplay)
                {
                    familyView.gameObject.SetActive(true);
                    familyView.Show();
                    familyDisplay = familyView.scroller1.gameObject.activeSelf && familyView.scroller2.gameObject.activeSelf
                        && familyView.value1.text.Contains("family-a") && familyView.value1.text.Contains("8")
                        && familyView.value2.text.Contains("family-b") && familyView.value2.text.Contains("700");
                    familyView.Hide();
                }
                BabyIllusionView illusionView = module.GetComponentInChildren<BabyIllusionView>(true);
                model.ApplyBasic(new BabyBasicInfo { BabyId = 1 });
                model.ApplyFigures(new BabyFigureInfo());
                model.MergeFigure(1, 2, 0, 0);
                model.MergeFigure(2, 3, 0, 0);
                BabyFigureConfigs.BabyFigureCfg illusionCfg = BabyFigureConfigs.Get(1);
                bool illusionDisplay = illusionView != null && illusionCfg != null;
                if (illusionDisplay)
                {
                    illusionView.gameObject.SetActive(true);
                    illusionView.Show();
                    BabyIlluItemBind[] illusionItems = illusionView.illuGp.GetComponentsInChildren<BabyIlluItemBind>(true);
                    Shenxiao.Module.Core.Common.BaseAwardItem[] stageCostItems =
                        illusionView.stageitemGp.GetComponentsInChildren<Shenxiao.Module.Core.Common.BaseAwardItem>(true);
                    int inactiveCount = 0;
                    int loadedIconCount = 0;
                    for (int i = 0; i < illusionItems.Length; i++)
                    {
                        if (illusionItems[i].unActive.gameObject.activeSelf) inactiveCount++;
                        if (illusionItems[i].resImg.sprite != null) loadedIconCount++;
                    }
                    illusionDisplay = illusionView.illuGp.childCount == 9
                        && illusionView.babyName.text == illusionCfg.BabyName
                        && illusionItems.Length == 9 && inactiveCount >= 7 && loadedIconCount == 9
                        && illusionView.selectedImg.gameObject.activeSelf
                        && !illusionView.unActive.gameObject.activeSelf
                        && illusionView.useGp.gameObject.activeSelf
                        && illusionView.activeBtn.GetComponent<UnityEngine.UI.Button>() != null
                        && illusionView.stageBtn.GetComponent<UnityEngine.UI.Button>() != null
                        && !illusionView.activeGp.gameObject.activeSelf
                        && illusionView.stageGp.gameObject.activeSelf
                        && !illusionView.maxImg.gameObject.activeSelf
                        && stageCostItems.Length == 1 && stageCostItems[0].gameObject.activeSelf
                        && stageCostItems[0].num_text.text == "30"
                        && illusionView.stageLb.text.Contains("35/30") && illusionView.stageLb.text.Contains("#0f9f00");
                    BabyPropItem[] activeProps = illusionView.propGp.GetComponentsInChildren<BabyPropItem>(true);
                    illusionDisplay = illusionDisplay && CountVisible(activeProps) == 4 && activeProps[0].nextLb.text.Contains("+30000");
                    if (illusionDisplay)
                    {
                        Transform third = illusionView.illuGp.Find("BabyIlluItem_3");
                        UnityEngine.UI.Button thirdButton = third != null ? third.GetComponent<BabyIlluItemBind>().clickGp.GetComponent<UnityEngine.UI.Button>() : null;
                        thirdButton?.onClick.Invoke();
                        BabyFigureConfigs.BabyFigureCfg thirdCfg = BabyFigureConfigs.Get(3);
                        Shenxiao.Module.Core.Common.BaseAwardItem[] activeCostItems =
                            illusionView.activeitemGp.GetComponentsInChildren<Shenxiao.Module.Core.Common.BaseAwardItem>(true);
                        illusionDisplay = thirdCfg != null && illusionView.babyName.text == thirdCfg.BabyName
                            && illusionView.activeGp.gameObject.activeSelf && !illusionView.useGp.gameObject.activeSelf
                            && illusionView.unActive.gameObject.activeSelf && !illusionView.selectedImg.gameObject.activeSelf
                            && !illusionView.stageGp.gameObject.activeSelf && !illusionView.maxImg.gameObject.activeSelf
                            && activeCostItems.Length == 1 && activeCostItems[0].gameObject.activeSelf
                            && activeCostItems[0].num_text.text == "30"
                            && illusionView.activeLb.text.Contains("0/30") && illusionView.activeLb.text.Contains("#ff4f50")
                            && stageCostItems.Length == 1 && !stageCostItems[0].gameObject.activeSelf;
                        BabyPropItem[] inactiveProps = illusionView.propGp.GetComponentsInChildren<BabyPropItem>(true);
                        illusionDisplay = illusionDisplay && CountVisible(inactiveProps) == 4 && inactiveProps[0].nextLb.text.Contains("+70000");
                    }
                    illusionView.Hide();
                }
                model.Reset();
                bool items = Check<BabyCulTaskItemBind>(module) && Check<BabyCulTaskItem>(module) && Check<BabyIlluItemBind>(module)
                    && Check<BabyPropItemBind>(propItem) && Check<BabyPropItem>(propItem);
                BabyCultivateViewBind cultivate = module.GetComponentInChildren<BabyCultivateViewBind>(true);
                BabyIllusionViewBind illusion = module.GetComponentInChildren<BabyIllusionViewBind>(true);
                bool templates = Has<BabyCulTaskItemBind>(cultivate != null ? cultivate._tpl_BabyCulTaskItem : null)
                    && Has<BabyPropItemBind>(cultivate != null ? cultivate._tpl_BabyPropItem : null)
                    && Has<BabyPropItem>(cultivate != null ? cultivate._tpl_BabyPropItem : null)
                    && Has<BabyIlluItemBind>(illusion != null ? illusion._tpl_BabyIlluItem : null)
                    && Has<BabyPropItemBind>(illusion != null ? illusion._tpl_BabyPropItem : null)
                    && Has<BabyPropItem>(illusion != null ? illusion._tpl_BabyPropItem : null);
                Debug.Log("CLIVERIFY babyui pages=" + pages + " businessViews=" + businessViews + " display=" + display + " familyDisplay=" + familyDisplay + " illusionDisplay=" + illusionDisplay + " items=" + items + " templates=" + templates);
                return pages && businessViews && propDisplay && display && familyDisplay && illusionDisplay && items && templates;
            }
            finally
            {
                BabyModel.Instance.Reset();
                Shenxiao.Module.Core.Bag.BagModel.Instance.Clear();
                UnityEngine.Object.DestroyImmediate(module);
                UnityEngine.Object.DestroyImmediate(propItem);
            }
        }

        private static bool Check<T>(GameObject root) where T : Component
        {
            T bind = root.GetComponentInChildren<T>(true);
            if (bind == null) return false;
            foreach (FieldInfo field in typeof(T).GetFields(BindFields))
                if (field.GetValue(bind) == null) return false;
            return true;
        }

        private static bool VerifyTasks(BabyCultivateView view)
        {
            BabyCulTaskItem[] all = view.taskGp.GetComponentsInChildren<BabyCulTaskItem>(true);
            var tasks = new System.Collections.Generic.List<BabyCulTaskItem>();
            for (int i = 0; i < all.Length; i++) if (all[i].TaskId > 0) tasks.Add(all[i]);
            if (tasks.Count != 3 || tasks[0].TaskId != 2 || tasks[1].TaskId != 1 || tasks[2].TaskId != 3) return false;
            return tasks[0].getBtn.gameObject.activeSelf && tasks[0].reddot.gameObject.activeSelf
                && !tasks[0].goBtn.gameObject.activeSelf && !tasks[0].finishImg.gameObject.activeSelf
                && tasks[1].goBtn.gameObject.activeSelf && !tasks[1].getBtn.gameObject.activeSelf
                && !tasks[1].finishImg.gameObject.activeSelf && tasks[2].finishImg.gameObject.activeSelf
                && !tasks[2].goBtn.gameObject.activeSelf && !tasks[2].getBtn.gameObject.activeSelf;
        }

        private static bool Has<T>(GameObject template) where T : Component
        {
            return template != null && template.GetComponent<T>() != null;
        }

        private static int CountVisible<T>(T[] items) where T : Component
        {
            int count = 0;
            for (int i = 0; i < items.Length; i++) if (items[i] != null && items[i].gameObject.activeSelf) count++;
            return count;
        }
    }
}
