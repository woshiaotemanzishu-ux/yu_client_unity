using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.MainUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面【总装】生成器:把职责清晰的有界 Region(均为重构版 UICreator 产物)
    /// 以嵌套 prefab 方式装进一个根节点,覆盖存盘为运行时实际加载的
    /// Assets/Prefabs/UI/MainUI/MainUIModule.prefab(旧 Laya 转换器版本被替换,git 可回退)。
    ///
    /// - 子 prefab 缺失时先自动调用对应 Creator 补生成;补完仍缺则报错退出、不动现网 prefab。
    /// - 嵌套保持 prefab 连接:改任一 Region prefab,总装产物自动跟随。
    /// - CollectBarView / FightingUpView / ArrowComponent 三个运行时按名单独加载的 prefab
    ///   不进总装(MainUIFlow/MainUIGuideManager 走独立地址),由各自条目单独生成。
    /// - 运行时行为:MainUIFlow 实例化本 prefab 后收集全部 BaseView 统一关闭,再按
    ///   InitMainUI 顺序 Show 首批视图 —— 因此子节点顺序只是编辑器观感,不影响逻辑。
    /// </summary>
    public static class MainUIModuleCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/MainUIModule.prefab";

        // 折叠太极(TurnDisk):从 HudActivity 提到总装层(它收放的不止活动区)。老端渲染在屏幕 (6,410)、尺寸 78×78;
        // 位置可后续在 MainUIModule.prefab 里手调。挂 MainUIFoldView,点击广播折叠事件,各区域视图各自监听收放。
        private const string IMG_TURN = "resource/game/mainUI/texture/uizjmv3_017b.png";
        // y 由 410 改为 396.3:回写编辑器内的手工微调(见提交 65393a5ea,TurnDisk anchoredPosition.y -449 → -435.3),
        // 按「改 UI 一律改 Creator」铁律落回生成器,避免下次重跑总装时被覆盖抹掉。
        private static readonly Vector2 TurnLocal = new Vector2(6f, 396.3f);
        private static readonly Vector2 TurnSize = new Vector2(78f, 78f);

        // 子 prefab 路径 + 缺失时的补生成动作;顺序 = prefab 子节点顺序。
        private static readonly (string path, string label, System.Action generate)[] Parts =
        {
            ("Assets/Prefabs/UI/MainUI/Regions/HudTop.prefab", "HudTop", HudTopCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudActivity.prefab", "HudActivity", HudActivityCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudActivityLeft.prefab", "HudActivityLeft", HudActivityCreator.GenerateLeft),
            ("Assets/Prefabs/UI/MainUI/Regions/HudActivityRight.prefab", "HudActivityRight", HudActivityCreator.GenerateRight),
            ("Assets/Prefabs/UI/MainUI/Regions/HudRank.prefab", "HudRank", HudRankCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudFuncOpen.prefab", "HudFuncOpen", HudFuncOpenCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudTaskTeam.prefab", "HudTaskTeam", HudTaskTeamCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudSkillBar.prefab", "HudSkillBar", HudSkillBarCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudAutoBrush.prefab", "HudAutoBrush", HudAutoBrushCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudJoystick.prefab", "HudJoystick", HudJoystickCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudChatBar.prefab", "HudChatBar", HudChatBarCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudNavBar.prefab", "HudNavBar", HudNavBarCreator.Generate),
            ("Assets/Prefabs/UI/MainUI/Regions/HudNotification.prefab", "HudNotification", HudAuxiliaryCreator.GenerateNotification),
            ("Assets/Prefabs/UI/MainUI/Regions/HudOnHook.prefab", "HudOnHook", HudAuxiliaryCreator.GenerateOnHook),
            ("Assets/Prefabs/UI/MainUI/Regions/HudSceneAssist.prefab", "HudSceneAssist", HudAuxiliaryCreator.GenerateSceneAssist),
        };

        /// <summary>MainUIFlow.FirstPassViews 同款首批显示顺序(预览用)。</summary>
        private static readonly System.Type[] FirstPassViews =
        {
            typeof(MainUITopViewBind),
            typeof(MainUIActivityViewBind),
            typeof(MainUIRankViewBind),
            typeof(Shenxiao.Generated.UI.FunctionOpen.FunctionOpenIconBind), // 功能预告框(HudFuncOpen 区域)
            typeof(MainUISkillViewBind),
            typeof(MainUIChatViewBind),
            typeof(MainUINotificationViewBind),
            typeof(MainUIOnHookViewBind),
            typeof(MainUISceneAssistViewBind),
            typeof(MainUITaskTeamViewBind),
            typeof(MainUIDownViewBind),
            typeof(MainUIAutoBrushViewBind),
            typeof(MainUIFoldViewBind),
            typeof(UIJoyStickBind),
        };

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "MainUIModule(总装·覆盖现网)",
                Note = "只嵌套有明确职责/边界的 Region；活动 loc1-10 统一由 HudActivity 管理，不再装 HudSecondary/HudOverlayCombat",
                Order = 90,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 第一遍:缺失的子 prefab 先补生成(逐个 try,单个失败不拦其它)。
            foreach (var part in Parts)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(part.path) != null) continue;
                Debug.Log("[UiCreator] 总装:缺 " + part.label + ",先补生成…");
                try { part.generate(); }
                catch (System.Exception e)
                {
                    Debug.LogError("[UiCreator] 总装:补生成 " + part.label + " 失败: " + e.Message);
                }
            }

            // 第二遍:全部就位才动现网 prefab,缺一个都不覆盖。
            var missing = new System.Collections.Generic.List<string>();
            foreach (var part in Parts)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(part.path) == null) missing.Add(part.label);
            }
            if (missing.Count > 0)
            {
                Debug.LogError("[UiCreator] 总装中止,以下子 prefab 仍缺失(现网 MainUIModule.prefab 未被改动): "
                    + string.Join(", ", missing));
                return;
            }

            RectTransform root = UiCreatorKit.NewRoot("MainUIModule");
            root.gameObject.SetActive(false);

            var instances = new System.Collections.Generic.Dictionary<string, GameObject>();
            foreach (var part in Parts)
            {
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(part.path);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, root);
                instances[part.label] = inst;
                // 仅全屏型子 prefab 重挂父后归零边距；有界 Region 保留自己的锚点、尺寸和位置。
                if (inst.transform is RectTransform rt
                    && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one)
                {
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }

            WireActivityRegions(instances);
            AddFoldTurnDisk(root); // 折叠太极提到总装层(收放不止活动区)

            root.gameObject.SetActive(true);
            UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);
            Debug.Log("[UiCreator] 总装完成:" + Parts.Length + " 个子模块 + 折叠太极 → " + PrefabPath + "(已覆盖旧转换器版本,git 可回退)");
        }

        private static void WireActivityRegions(System.Collections.Generic.Dictionary<string, GameObject> instances)
        {
            if (!instances.TryGetValue("HudActivity", out GameObject activity)
                || !instances.TryGetValue("HudActivityLeft", out GameObject left)
                || !instances.TryGetValue("HudActivityRight", out GameObject right))
            {
                Debug.LogError("[UiCreator] 总装：HudActivity 三个区域实例不完整，无法统一回填左右活动位");
                return;
            }

            MainUIActivityView view = activity.GetComponentInChildren<MainUIActivityView>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] 总装：HudActivity 缺 MainUIActivityView");
                return;
            }

            view._gp_side_left = left.transform as RectTransform;
            view._gp_side_right = right.transform as RectTransform;
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        }

        /// <summary>在总装根层加折叠太极(TurnDisk)+ 挂 MainUIFoldView(点击广播 EVT_MAINUI_ACTIVITY_FOLD)。
        /// 放根层、不属任何会折叠的区域 → 折叠时太极自身始终可见、可再展开。位置/大小可在 prefab 里手调。</summary>
        private static void AddFoldTurnDisk(RectTransform root)
        {
            Image imgTurn = UiCreatorKit.NewImage("TurnDisk", root);
            RectTransform rt = imgTurn.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // 左上锚(与老端顶左原点一致)
            rt.pivot = new Vector2(0.5f, 0.5f); // 轴心居中:折叠点击只改 rotation,pivot 在角上会绕角甩出去(看着像挪位),居中才原地转
            rt.sizeDelta = TurnSize;
            // TurnLocal 是老端左上原点;pivot 居中后补半个尺寸偏移,静止位置与旧的左上轴心摆放完全一致
            rt.anchoredPosition = new Vector2(TurnLocal.x + TurnSize.x * 0.5f, -(TurnLocal.y + TurnSize.y * 0.5f)); // Laya 顶左 y 向下 → Unity 取负
            imgTurn.raycastTarget = true; // 太极要能接收点击
            UiCreatorKit.TrySetSprite(imgTurn, IMG_TURN, UiCreatorKit.Palette.BtnNeutral);

            MainUIFoldView fold = imgTurn.gameObject.AddComponent<MainUIFoldView>();
            fold._img_turn = imgTurn;
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 MainUIModule",
                    "请先进入 Play 模式(UI 层已初始化)再点预览。\n\n" +
                    "预览会复刻 MainUIFlow 的做法:实例化总装 prefab → 关闭全部 BaseView → " +
                    "按 InitMainUI 顺序 Show 首批常驻视图。弹层类视图保持关闭。",
                    "好");
                return;
            }

            if (_previewInstance != null)
            {
                Object.Destroy(_previewInstance);
                _previewInstance = null;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[UiCreator] 预览失败,找不到 " + PrefabPath + "(请先点生成)");
                return;
            }

            Transform layer = ViewManager.GetLayer(UILayer.Main);
            _previewInstance = Object.Instantiate(prefab, layer);
            _previewInstance.name = "MainUIModule(Preview)";

            BaseView[] views = _previewInstance.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views)
            {
                v.gameObject.SetActive(false);
            }
            foreach (System.Type t in FirstPassViews)
            {
                var view = (BaseView)_previewInstance.GetComponentInChildren(t, true);
                if (view != null) view.Show();
                else Debug.LogWarning("[UiCreator] 预览:总装里没找到 " + t.Name);
            }
            Selection.activeObject = _previewInstance;
        }
    }
}
