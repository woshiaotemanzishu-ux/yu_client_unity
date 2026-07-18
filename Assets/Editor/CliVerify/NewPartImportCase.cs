using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.EditorTools.ArtImport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 新模型部件导入链路实证(资产管理[替换新模型]泛化,2026-07-11):
    ///  ① ImportPart 三件套:role_1213(七动作)/head_1213(两动作)/weapon_1200(一动作)从美术工程整夹导入
    ///  ② 目标归一:object/{module}/{夹}/{id}@{动作}.prefab(head@idle → 1213@idle 改名生效)
    ///  ③ Addressables 键位已登记(object/role/role_1213/1213@idle 等)
    ///  ④ 挂点体检:Role 必须有 head_mount/rhand/root；运行时补偿必须保持 0/0/1
    ///  ⑤ 拼装冒烟:body(idle) + head_attach 对齐 head_mount + weapon_attach 对齐 rhand,截图留档
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
            string[] expectRole = { "attack", "create3", "death", "idle", "run", "walk" }; // test 动作 07-17 已随交付删除
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

            // ④ 挂点体检(硬断言):美术工程按模板补齐 head_mount/rhand/root,缺=判红。
            string[] missing = ArtPrefabImporter.MissingRoleMounts($"{RoleDir}/1213@idle.prefab");
            bool mountsOk = missing.Length == 0;
            if (!mountsOk)
                Debug.LogError("CLIVERIFY newpart mounts missing=[" + string.Join(",", missing)
                    + "](交付规范要求 head_mount/rhand/root 齐,美术工程跑交付总检查后重导)");
            bool zeroRuntimeCompensation = HasZeroRuntimeCompensation();
            if (!zeroRuntimeCompensation)
                Debug.LogError("CLIVERIFY newpart runtime compensation 不是 0/0/1；模板问题不应转成游戏逐件偏移");

            // ⑤ 拼装冒烟 + 截图(空场景+补光,自适应取景;batch 不播 Timeline,静态姿势即可):
            //    身体 + 头饰 locator 对齐 head_mount + 武器 locator 对齐 rhand，三件全挂上才过。
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            bool bodyOk = false, headAttached = false, weaponAttached = false;
            var bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{RoleDir}/1213@idle.prefab");
            if (bodyPrefab != null)
            {
                GameObject body = Object.Instantiate(bodyPrefab);
                bodyOk = body.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;

                Transform headNode = CliVerify.FindDeep(body.transform, "head_mount");
                var headPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{HeadDir}/1213@idle.prefab");
                if (headNode != null && headPrefab != null)
                {
                    headAttached = AttachHeadByLocator(body, headPrefab, headNode);
                }

                Transform rhand = CliVerify.FindDeep(body.transform, "rhand");
                var weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{WeaponDir}/1200@idle.prefab");
                if (rhand != null && weaponPrefab != null)
                {
                    weaponAttached = AttachWeaponByLocator(weaponPrefab, rhand);
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
                && zeroRuntimeCompensation
                && mountsOk && bodyOk && headAttached && weaponAttached;
            Debug.Log("CLIVERIFY newpart VERDICT assets=" + (roleOk && headOk && weaponOk)
                + " addr=" + (addrRole && addrHead && addrWeapon) + " mounts=" + mountsOk
                + " compensation0/0/1=" + zeroRuntimeCompensation
                + " body=" + bodyOk + " headAttached=" + headAttached + " weaponAttached=" + weaponAttached
                + " pass=" + pass);
            return Task.FromResult(pass ? 0 : 3);
        }

        private static bool AttachHeadByLocator(GameObject body, GameObject prefab, Transform socket)
        {
            GameObject inst = Object.Instantiate(prefab, socket);
            inst.transform.localPosition = prefab.transform.localPosition;
            inst.transform.localRotation = prefab.transform.localRotation;
            inst.transform.localScale = prefab.transform.localScale;
            Transform bodyHead = CliVerify.FindDeep(body.transform, "Bip001 Head")
                                 ?? CliVerify.FindDeep(body.transform, "head");
            Transform locator = CliVerify.FindDeep(inst.transform, "head_attach");
            if (bodyHead == null || locator == null) return false;
            var follower = inst.AddComponent<AnimatedAttachmentPositionFollower>();
            follower.Initialize(bodyHead, locator, body.transform, Vector3.zero, Vector3.zero, 1f);
            follower.SnapNow();
            return true;
        }

        private static bool AttachWeaponByLocator(GameObject prefab, Transform socket)
        {
            GameObject inst = Object.Instantiate(prefab, socket);
            inst.transform.localPosition = prefab.transform.localPosition;
            inst.transform.localRotation = prefab.transform.localRotation;
            inst.transform.localScale = prefab.transform.localScale;
            Transform locator = CliVerify.FindDeep(inst.transform, "weapon_attach");
            if (locator == null) return false;
            var aligner = inst.AddComponent<AttachmentSocketAligner>();
            aligner.Initialize(locator, Vector3.zero, Vector3.zero, 1f);
            aligner.SnapNow();
            float positionError = Vector3.Distance(locator.position, socket.position);
            float rotationError = Quaternion.Angle(locator.rotation, socket.rotation);
            Debug.Log($"CLIVERIFY weapon locator error pos={positionError:F7},rot={rotationError:F5}°");
            return positionError <= 0.0001f && rotationError <= 0.01f;
        }

        private static bool HasZeroRuntimeCompensation()
        {
            const string config = "Assets/GameRes/resource/config/client/model_replacement.json";
            if (!File.Exists(config)) return false;
            ModelReplacement.Data data = JsonUtility.FromJson<ModelReplacement.Data>(File.ReadAllText(config));
            if (data?.entries == null) return false;
            foreach (string key in new[] { "head/1213", "weapon/1200" })
            {
                ModelReplacement.Entry entry = data.entries.FirstOrDefault(e => e != null && e.key == key);
                if (entry == null || entry.attachmentPositionOffset.sqrMagnitude > 0.00000001f
                    || entry.attachmentRotationOffset.sqrMagnitude > 0.00000001f
                    || Mathf.Abs(entry.attachmentScale - 1f) > 0.0001f)
                    return false;
            }
            return true;
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
