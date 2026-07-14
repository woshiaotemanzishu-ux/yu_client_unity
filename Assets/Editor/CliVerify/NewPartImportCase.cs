using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Shenxiao.EditorTools.ArtImport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 新模型部件导入链路实证(资产管理[替换新模型]泛化,2026-07-11):
    ///  ① ImportPart 三件套:role_1213(六动作)/head_1213(两动作)/weapon_1200(一动作)从美术工程整夹导入
    ///  ② 目标归一:object/{module}/{夹}/{id}@{动作}.prefab(head@idle → 1213@idle 改名生效)
    ///  ③ Addressables 键位已登记(object/role/role_1213/1213@idle 等)
    ///  ④ 挂点体检:head 必须在(判红);rhand/root 当前交付缺失、已发回美术补,只记 WARN——美术修完改硬断言
    ///  ⑤ 拼装冒烟:body(idle) 实例化 + head_1213 挂 head 节点 + weapon 挂 rhand(缺则跳过),截图留档
    /// 注意:本用例会真实写入工程资产(幂等,重跑=SkipSame)且依赖本机 E:/Project/ArtsProject——
    /// 不入 RenderAll(纯验证套件不做资产变更)。单独跑:
    ///   Unity.exe -batchmode -projectPath . -executeMethod Shenxiao.EditorTools.CliVerify.NewPartImport
    /// </summary>
    public static class NewPartImportCase
    {
        private const string RoleDir = "Assets/GameRes/object/role/role_1213";
        private const string HeadDir = "Assets/GameRes/object/head/head_1213";
        private const string WeaponDir = "Assets/GameRes/object/weapon/weapon_1200";

        public static Task<int> Run()
        {
            // ① 三件套导入(同步,内部自带 Refresh/二次导入/档案/台账/Addressables)
            bool okRole = ArtPrefabImporter.ImportPart("role", "role_1213", out string sRole);
            Debug.Log("CLIVERIFY newpart import role_1213 ok=" + okRole + " → " + sRole);
            bool okHead = ArtPrefabImporter.ImportPart("head", "head_1213", out string sHead);
            Debug.Log("CLIVERIFY newpart import head_1213 ok=" + okHead + " → " + sHead);
            bool okWeapon = ArtPrefabImporter.ImportPart("weapon", "weapon_1200", out string sWeapon);
            Debug.Log("CLIVERIFY newpart import weapon_1200 ok=" + okWeapon + " → " + sWeapon);
            if (!okRole || !okHead || !okWeapon) return Task.FromResult(3);

            // ② 目标归一:根 prefab 统一 {id}@{动作}.prefab(源里 head@idle 应被改名为 1213@idle)
            string[] expectRole = { "attack", "create3", "death", "idle", "run", "walk" };
            bool roleOk = expectRole.All(a => File.Exists($"{RoleDir}/1213@{a}.prefab"));
            bool headOk = File.Exists($"{HeadDir}/1213@idle.prefab") && File.Exists($"{HeadDir}/1213@create3.prefab");
            bool weaponOk = File.Exists($"{WeaponDir}/1200@idle.prefab");
            Debug.Log("CLIVERIFY newpart assets role6=" + roleOk + " head2=" + headOk + " weapon1=" + weaponOk);

            // ③ Addressables 键位(地址=GameRes 相对路径小写去扩展)
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            bool addrRole = false, addrHead = false, addrWeapon = false;
            if (settings != null)
            {
                foreach (UnityEditor.AddressableAssets.Settings.AddressableAssetGroup g in settings.groups)
                {
                    if (g == null) continue;
                    foreach (UnityEditor.AddressableAssets.Settings.AddressableAssetEntry en in g.entries)
                    {
                        if (en == null || en.address == null) continue;
                        if (en.address == "object/role/role_1213/1213@idle") addrRole = true;
                        if (en.address == "object/head/head_1213/1213@idle") addrHead = true;
                        if (en.address == "object/weapon/weapon_1200/1200@idle") addrWeapon = true;
                    }
                }
            }
            Debug.Log("CLIVERIFY newpart addr role=" + addrRole + " head=" + addrHead + " weapon=" + addrWeapon);

            // ④ 挂点体检(硬断言):美术工程已按交付规范补齐 head/rhand/root(MountPointPatcher),缺=判红
            string[] missing = ArtPrefabImporter.MissingRoleMounts($"{RoleDir}/1213@idle.prefab");
            bool mountsOk = missing.Length == 0;
            if (!mountsOk)
                Debug.LogError("CLIVERIFY newpart mounts missing=[" + string.Join(",", missing)
                    + "](交付规范要求 head/rhand/root 齐,美术工程跑 交付/补挂点 后重导)");

            // ⑤ 拼装冒烟 + 截图(空场景+补光,自适应取景;batch 不播 Timeline,静态姿势即可):
            //    身体 + 头饰挂 head + 武器挂 rhand,三件全挂上才过(对标 RoleModelAssembler.AttachPart)
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            bool bodyOk = false, headAttached = false, weaponAttached = false;
            var bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{RoleDir}/1213@idle.prefab");
            if (bodyPrefab != null)
            {
                GameObject body = Object.Instantiate(bodyPrefab);
                bodyOk = body.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;

                Transform headNode = CliVerify.FindDeep(body.transform, "head");
                var headPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{HeadDir}/1213@idle.prefab");
                if (headNode != null && headPrefab != null)
                {
                    AttachZeroed(headPrefab, headNode);
                    headAttached = true;
                }

                Transform rhand = CliVerify.FindDeep(body.transform, "rhand");
                var weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{WeaponDir}/1200@idle.prefab");
                if (rhand != null && weaponPrefab != null)
                {
                    AttachZeroed(weaponPrefab, rhand);
                    weaponAttached = true;
                }
                else
                {
                    Debug.LogError("CLIVERIFY newpart weapon attach failed(rhand=" + (rhand != null)
                        + " weaponPrefab=" + (weaponPrefab != null) + ")");
                }

                string png = CaptureModel(body, "Temp/newpart_1213_assembled.png");
                Debug.Log("CLIVERIFY newpart body=" + bodyOk + " headAttached=" + headAttached
                    + " weaponAttached=" + weaponAttached + " shot=" + png);
            }
            else
            {
                Debug.LogError("CLIVERIFY newpart body prefab missing: " + RoleDir + "/1213@idle.prefab");
            }

            bool pass = roleOk && headOk && weaponOk && addrRole && addrHead && addrWeapon
                && mountsOk && bodyOk && headAttached && weaponAttached;
            Debug.Log("CLIVERIFY newpart VERDICT assets=" + (roleOk && headOk && weaponOk)
                + " addr=" + (addrRole && addrHead && addrWeapon) + " mounts=" + mountsOk
                + " body=" + bodyOk + " headAttached=" + headAttached + " weaponAttached=" + weaponAttached
                + " pass=" + pass);
            return Task.FromResult(pass ? 0 : 3);
        }

        /// <summary>对标 RoleModelAssembler.AttachPart:实例化为挂点子节点并清局部变换。</summary>
        private static void AttachZeroed(GameObject prefab, Transform bone)
        {
            GameObject inst = Object.Instantiate(prefab, bone);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
        }

        /// <summary>自适应取景截图:按蒙皮包围盒摆相机(模型原始体量未知,不能用固定机位)。</summary>
        private static string CaptureModel(GameObject root, string projectRelativePng)
        {
            Bounds bounds = default;
            bool has = false;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!has) { bounds = r.bounds; has = true; }
                else bounds.Encapsulate(r.bounds);
            }
            if (!has) bounds = new Bounds(root.transform.position, Vector3.one);

            var lightGo = new GameObject("KeyLight");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            var rt = new RenderTexture(720, 1280, 24);
            var camGo = new GameObject("NewPartCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 1f);
            cam.targetTexture = rt;
            float dist = bounds.extents.magnitude * 2.2f + 0.01f;
            cam.transform.position = bounds.center + new Vector3(0f, 0.15f * bounds.size.y, -dist);
            cam.transform.LookAt(bounds.center);
            cam.nearClipPlane = Mathf.Max(0.01f, dist * 0.01f);
            cam.farClipPlane = dist * 10f;

            cam.Render();
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            string full = Path.GetFullPath(projectRelativePng);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(lightGo);
            return full;
        }
    }
}
