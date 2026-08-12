using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 角色统一尺寸回归：1201 与 1213 均保留美术源模型体量，角色动作与附件空间倍率固定为 1；
    /// 同一标准头饰跨身体挂接后的世界比例必须一致。日志前缀
    /// "CLIVERIFY attachmentspace"。本用例只读现有导入产物，不重导、不改 Addressables。
    /// </summary>
    public static class RoleAttachmentSpaceCase
    {
        private const string Role1201Dir = "Assets/GameRes/object/role/role_1201";
        private const string Role1213Idle = "Assets/GameRes/object/role/role_1213/1213@idle.prefab";
        private const float ExpectedNormalScale = 1f;
        [MenuItem("神霄/验证/1201 角色附件空间")]
        public static async void RunFromMenu()
        {
            try
            {
                int code = await Run();
                Debug.Log("CLIVERIFY attachmentspace GUI EXIT " + code);
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY attachmentspace GUI EXCEPTION " + e);
            }
        }

        public static async Task<int> Run()
        {
            string assemblyProfilePath = $"{Role1201Dir}/role_assembly_profile.json";
            if (!File.Exists(assemblyProfilePath))
            {
                Debug.LogError("CLIVERIFY attachmentspace 缺角色装配档案:" + assemblyProfilePath);
                return 3;
            }

            string[] actionPrefabs = Directory.GetFiles(Role1201Dir, "1201@*.prefab", SearchOption.TopDirectoryOnly)
                .Select(path => path.Replace('\\', '/')).OrderBy(path => path).ToArray();
            var idlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Role1201Dir}/1201@idle.prefab");
            var referencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Role1213Idle);
            ArtModelRenderProfile idleProfile = idlePrefab != null
                ? idlePrefab.GetComponent<ArtModelRenderProfile>() : null;
            ArtModelRenderProfile referenceProfile = referencePrefab != null
                ? referencePrefab.GetComponent<ArtModelRenderProfile>() : null;
            if (actionPrefabs.Length < 5 || idleProfile == null || referenceProfile == null)
            {
                Debug.LogError("CLIVERIFY attachmentspace 角色动作或渲染档案缺失");
                return 3;
            }

            bool allActionsUnified = true;
            foreach (string path in actionPrefabs)
            {
                ArtModelRenderProfile profile = AssetDatabase.LoadAssetAtPath<GameObject>(path)
                    ?.GetComponent<ArtModelRenderProfile>();
                bool actionOk = profile != null && profile.hasLanding
                    && Nearly(profile.landingScale, ExpectedNormalScale)
                    && Nearly(profile.attachmentSpaceScale, ExpectedNormalScale);
                if (!actionOk)
                {
                    allActionsUnified = false;
                    Debug.LogError("CLIVERIFY attachmentspace 动作未统一:" + path
                                   + $" landing={profile?.landingScale} attachment={profile?.attachmentSpaceScale}");
                }
            }

            bool profilesNormal = Nearly(idleProfile.landingScale, ExpectedNormalScale)
                                  && Nearly(idleProfile.attachmentSpaceScale, ExpectedNormalScale)
                                  && Nearly(referenceProfile.landingScale, ExpectedNormalScale)
                                  && Nearly(referenceProfile.attachmentSpaceScale, ExpectedNormalScale);
            bool bodyKept = Nearly(idleProfile.landingScale, ExpectedNormalScale)
                            && Nearly(idlePrefab.transform.localScale.x, 1f);
            Debug.Log($"CLIVERIFY attachmentspace profile actions={actionPrefabs.Length}," +
                      $"bodyLanding={idleProfile.landingScale:F7},attachment={idleProfile.attachmentSpaceScale:F8}," +
                      $"referenceLanding={referenceProfile.landingScale:F7}," +
                      $"allActionsUnified={allActionsUnified},profilesNormal={profilesNormal},bodyKept={bodyKept}");

            bool fallbackBefore = ResManager.EditorPreferFallback;
            GameObject role1201 = null;
            GameObject role1213 = null;
            try
            {
                ResManager.EditorPreferFallback = true;
                role1201 = await Build(1201);
                role1213 = await Build(1213);
                Vector3 scale1201 = HeadWorldScale(role1201);
                Vector3 scale1213 = HeadWorldScale(role1213);
                bool runtimeOk = role1201 != null && role1213 != null
                                 && MaxAbsDelta(scale1201, scale1213) <= 0.0003f;
                Debug.Log($"CLIVERIFY attachmentspace runtime head1201={scale1201},head1213={scale1213}," +
                          $"runtimeOk={runtimeOk}");

                bool pass = allActionsUnified && profilesNormal && bodyKept && runtimeOk;
                Debug.Log("CLIVERIFY attachmentspace VERDICT pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                ResManager.EditorPreferFallback = fallbackBefore;
                if (role1201 != null) UnityEngine.Object.DestroyImmediate(role1201);
                if (role1213 != null) UnityEngine.Object.DestroyImmediate(role1213);
            }
        }

        private static async Task<GameObject> Build(int clotheRes)
        {
            return await RoleModelAssembler.BuildAsync(new RoleModelSpec
            {
                Career = 1,
                ClotheRes = clotheRes,
                HeadRes = 1213,
                Actions = new[] { "idle" },
            });
        }

        private static Vector3 HeadWorldScale(GameObject assembled)
        {
            ReplaceableRoleModel driver = assembled != null
                ? assembled.GetComponent<ReplaceableRoleModel>() : null;
            AnimatedAttachmentPositionFollower follower = driver?.ActiveModel != null
                ? driver.ActiveModel.GetComponentInChildren<AnimatedAttachmentPositionFollower>(true)
                : null;
            if (follower == null) return Vector3.zero;
            Vector3 scale = follower.transform.lossyScale;
            return new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        private static float MaxAbsDelta(Vector3 a, Vector3 b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z));
        }

        private static bool Nearly(float a, float b, float tolerance = 0.0001f)
        {
            return Mathf.Abs(a - b) <= tolerance;
        }
    }
}
