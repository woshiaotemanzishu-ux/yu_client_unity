using System;
using System.Linq;
using Shenxiao.Generated.UI.Equip;
using Shenxiao.Module.Core.Equip;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// One-shot, idempotent Equip-only prefab upgrade for the Strength equipment slot template.
    /// The Common prefab is opened read-only solely as the converted source identity; only
    /// EquipModule.prefab is saved.
    /// </summary>
    public static class EquipStrengthPrefabUpgradeCase
    {
        private const string EquipPrefabPath = "Assets/Prefabs/UI/Equip/EquipModule.prefab";
        private const string CommonPrefabPath = "Assets/Prefabs/UI/Common/CommonModule.prefab";
        private const string TemplateName = "EquipStrenItem";

        public static void RunFromCommandLine()
        {
            int exitCode = 3;
            try
            {
                bool changed = UpgradePrefab();
                CodeEditor.CurrentEditor.SyncAll();
                bool pass = ValidatePrefab();
                Debug.Log("CLIVERIFY equip-strength-prefab changed=" + changed);
                Debug.Log("CLIVERIFY equip-strength-prefab VERDICT pass=" + pass);
                exitCode = pass ? 0 : 3;
            }
            catch (Exception exception)
            {
                Debug.LogError("CLIVERIFY equip-strength-prefab EXCEPTION " + exception);
                Debug.Log("CLIVERIFY equip-strength-prefab VERDICT pass=False");
            }
            finally
            {
                EditorApplication.Exit(exitCode);
            }
        }

        private static bool UpgradePrefab()
        {
            GameObject equipRoot = null;
            GameObject commonRoot = null;
            try
            {
                equipRoot = PrefabUtility.LoadPrefabContents(EquipPrefabPath);
                commonRoot = PrefabUtility.LoadPrefabContents(CommonPrefabPath);
                if (equipRoot == null || commonRoot == null)
                    throw new InvalidOperationException("required prefab could not be opened");

                EquipStrenView view = equipRoot.GetComponentInChildren<EquipStrenView>(true);
                if (view == null)
                    throw new InvalidOperationException("EquipStrenView business component is missing");

                Transform templates = view.transform.Find("__Templates");
                if (templates == null)
                    throw new InvalidOperationException("EquipStrenView/__Templates is missing");

                EquipStrenItem target = templates.GetComponentsInChildren<EquipStrenItem>(true)
                    .FirstOrDefault(item => item.transform.parent == templates);
                bool changed = false;
                if (target == null)
                {
                    EquipStrenItemBind source = FindCommonSource(commonRoot);
                    if (source == null)
                        throw new InvalidOperationException("Common EquipToolTips EquipStrenItem source is missing");

                    GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, templates, false);
                    clone.name = TemplateName;
                    RectTransform rect = clone.transform as RectTransform;
                    if (rect == null)
                        throw new InvalidOperationException("cloned EquipStrenItem has no RectTransform");
                    rect.sizeDelta = new Vector2(117f, 117f);

                    EquipStrenItemBind generated = clone.GetComponent<EquipStrenItemBind>();
                    if (generated == null || generated.GetType() != typeof(EquipStrenItemBind))
                        throw new InvalidOperationException("cloned source has no exact generated bind");

                    var iconBg = generated.icon_bg;
                    var lockIcon = generated.lock_icon;
                    var awardCon = generated.award_con;
                    var selectImg = generated.select_img;
                    var level = generated.level;
                    var tips = generated.tips;
                    var grade = generated.grade;
                    var redDot = generated.red_dot;
                    var groupEff = generated.group_eff;
                    var clickBg = generated.click_bg;

                    UnityEngine.Object.DestroyImmediate(generated, true);
                    target = clone.AddComponent<EquipStrenItem>();
                    target.icon_bg = iconBg;
                    target.lock_icon = lockIcon;
                    target.award_con = awardCon;
                    target.select_img = selectImg;
                    target.level = level;
                    target.tips = tips;
                    target.grade = grade;
                    target.red_dot = redDot;
                    target.group_eff = groupEff;
                    target.click_bg = clickBg;
                    changed = true;
                }

                RectTransform targetRect = target.transform as RectTransform;
                if (targetRect == null)
                    throw new InvalidOperationException("target EquipStrenItem has no RectTransform");
                if (targetRect.sizeDelta != new Vector2(117f, 117f))
                {
                    targetRect.sizeDelta = new Vector2(117f, 117f);
                    changed = true;
                }
                if (target.gameObject.name != TemplateName)
                {
                    target.gameObject.name = TemplateName;
                    changed = true;
                }
                if (target.gameObject.activeSelf)
                {
                    target.gameObject.SetActive(false);
                    changed = true;
                }

                var serializedView = new SerializedObject(view);
                SerializedProperty templateProperty = serializedView.FindProperty("_tpl_EquipStrenItem");
                if (templateProperty == null)
                    throw new InvalidOperationException("EquipStrenView._tpl_EquipStrenItem is not serialized");
                if (templateProperty.objectReferenceValue != target)
                {
                    templateProperty.objectReferenceValue = target;
                    serializedView.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(equipRoot, EquipPrefabPath);
                if (saved == null)
                    throw new InvalidOperationException("EquipModule.prefab save failed");
                return changed;
            }
            finally
            {
                if (commonRoot != null) PrefabUtility.UnloadPrefabContents(commonRoot);
                if (equipRoot != null) PrefabUtility.UnloadPrefabContents(equipRoot);
            }
        }

        private static EquipStrenItemBind FindCommonSource(GameObject commonRoot)
        {
            return commonRoot.GetComponentsInChildren<EquipStrenItemBind>(true)
                .FirstOrDefault(bind => bind.GetType() == typeof(EquipStrenItemBind)
                    && bind.name == TemplateName
                    && bind.transform.parent != null
                    && bind.transform.parent.name == "__Templates"
                    && HasAncestor(bind.transform.parent, "EquipToolTips"));
        }

        private static bool HasAncestor(Transform transform, string name)
        {
            for (Transform current = transform; current != null; current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static bool ValidatePrefab()
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(EquipPrefabPath);
                EquipStrenView view = root != null
                    ? root.GetComponentInChildren<EquipStrenView>(true) : null;
                Transform templates = view != null ? view.transform.Find("__Templates") : null;
                EquipStrenItem[] directTemplates = templates != null
                    ? templates.GetComponentsInChildren<EquipStrenItem>(true)
                        .Where(item => item.transform.parent == templates).ToArray()
                    : Array.Empty<EquipStrenItem>();
                EquipStrenItem target = directTemplates.Length == 1 ? directTemplates[0] : null;
                RectTransform rect = target != null ? target.transform as RectTransform : null;

                bool fields = target != null
                    && target.icon_bg != null && target.lock_icon != null
                    && target.award_con != null && target.select_img != null
                    && target.level != null && target.tips != null && target.grade != null
                    && target.red_dot != null && target.group_eff != null && target.click_bg != null;
                bool exactGeneratedBindRemoved = target != null
                    && target.GetComponents<MonoBehaviour>()
                        .All(component => component == null || component.GetType() != typeof(EquipStrenItemBind));
                bool serializedReference = false;
                if (view != null)
                {
                    var serializedView = new SerializedObject(view);
                    SerializedProperty property = serializedView.FindProperty("_tpl_EquipStrenItem");
                    serializedReference = property != null && property.objectReferenceValue == target;
                }

                int missingScripts = 0;
                if (root != null)
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                        missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);

                Check("view-and-private-template", view != null && templates != null
                    && directTemplates.Length == 1 && target.name == TemplateName);
                Check("root-size-117x117", rect != null
                    && Mathf.Approximately(rect.sizeDelta.x, 117f)
                    && Mathf.Approximately(rect.sizeDelta.y, 117f));
                Check("business-fields", fields);
                Check("serialized-reference", serializedReference);
                Check("template-hidden", target != null && !target.gameObject.activeSelf);
                Check("generated-bind-replaced", exactGeneratedBindRemoved);
                Check("missing-scripts", missingScripts == 0);

                return view != null && templates != null && directTemplates.Length == 1
                    && target.name == TemplateName && rect != null
                    && Mathf.Approximately(rect.sizeDelta.x, 117f)
                    && Mathf.Approximately(rect.sizeDelta.y, 117f)
                    && fields && serializedReference && !target.gameObject.activeSelf
                    && exactGeneratedBindRemoved && missingScripts == 0;
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Check(string name, bool ok)
        {
            Debug.Log("CLIVERIFY equip-strength-prefab " + name + " ok=" + ok);
        }
    }
}
