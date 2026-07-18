#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 新美术头饰的【运行时所见即所得调参浮层】(仅 Editor / Development 构建存在)。
    ///
    /// 背景:1213 首套新模型需要先把头饰相对身体的 position/rotation/scale 调准，再反推美术模板。
    /// 本浮层直接修改 AnimatedAttachmentPositionFollower 的挂点局部校准量；拖模型仍只用于转身检查四个方向，
    /// 不再修改整个人的 configlogin 展示参数。
    ///
    /// 真值源是当前展示台里的 follower。输出按钮只把本次人工验收结果写日志/剪贴板；确认前不写配置和资产。
    /// </summary>
    public sealed class ArtModelTuner : MonoBehaviour
    {
        private const float PanelWidth = 390f;
        private const float PanelHeight = 430f;
        private const float ToggleWidth = 116f;
        private const float ToggleHeight = 28f;
        private const float ScreenMargin = 10f;
        private const float ToggleGap = 6f;

        /// <summary>总开关,默认关(隐藏)。由「神霄/调试/头饰调参浮层」菜单翻转,状态存 EditorPrefs
        /// (见 ArtModelTunerMenu),需要再调参时点一下菜单即可,代码保留不删。</summary>
        public static bool Enabled;

        private static ArtModelTuner _inst;

        private string _section = "SelectRole";
        private bool _open;
        private AnimatedAttachmentPositionFollower _current;
        private Vector3 _initialPosition;
        private Vector3 _initialRotation;
        private float _initialScale = 1f;
        private AttachmentSocketAligner _currentAligner;
        private Vector3 _initialWeaponPosition;
        private Vector3 _initialWeaponRotation;
        private float _initialWeaponScale = 1f;

        /// <summary>挂起/更新调参浮层(每次展示整模时调,刷新页面基准)。</summary>
        public static void Attach(string section)
        {
            if (_inst == null)
            {
                var go = new GameObject("__ArtModelTuner");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<ArtModelTuner>();
            }
            _inst._section = section;
            _inst._open = true; // 调试工具启用时进选角页直接展开，避免被左上角业务控件遮住后找不到入口
            _inst.enabled = true;
        }

        /// <summary>离开页面时关掉(不销毁,下次 Attach 复用)。</summary>
        public static void Detach()
        {
            if (_inst != null) _inst.enabled = false;
        }

        private void OnGUI()
        {
            if (!Enabled) return; // 默认隐藏;菜单开了才画
            UIModelStage st = UIModelStage.Default;
            if (st == null || !st.IsArt) return;
            AnimatedAttachmentPositionFollower follower = st.ActiveAttachmentFollower;
            AttachmentSocketAligner aligner = st.ActiveWeaponAligner;
            if (follower == null && aligner == null) return;

            if (_current != follower && follower != null)
            {
                _current = follower;
                _initialPosition = follower.PositionOffset;
                _initialRotation = follower.RotationOffset;
                _initialScale = follower.ScaleMultiplier;
            }
            if (_currentAligner != aligner && aligner != null)
            {
                _currentAligner = aligner;
                _initialWeaponPosition = aligner.PositionOffset;
                _initialWeaponRotation = aligner.RotationOffset;
                _initialWeaponScale = aligner.ScaleMultiplier;
            }

            float panelHeight = 30f + (follower != null ? 330f : 0f) + (aligner != null ? 310f : 0f);
            float panelX = Mathf.Max(ScreenMargin, Screen.width - PanelWidth - ScreenMargin);
            float panelY = Mathf.Max(ScreenMargin, Screen.height - panelHeight - ScreenMargin);
            float toggleX = Mathf.Max(ScreenMargin, Screen.width - ToggleWidth - ScreenMargin);
            float toggleY = _open
                ? Mathf.Max(ScreenMargin, panelY - ToggleHeight - ToggleGap)
                : Mathf.Max(ScreenMargin, Screen.height - ToggleHeight - ScreenMargin);

            if (GUI.Button(new Rect(toggleX, toggleY, ToggleWidth, ToggleHeight),
                    _open ? "挂点调参 ▲" : "挂点调参 ▼"))
                _open = !_open;
            if (!_open) return;

            GUILayout.BeginArea(new Rect(panelX, panelY, PanelWidth, panelHeight), GUI.skin.box);
            GUILayout.Label($"挂点局部调参 · {_section}(拖角色检查四方向)");
            if (follower != null) DrawHead(follower);
            if (aligner != null) DrawWeapon(aligner);
            GUILayout.EndArea();
        }

        private void DrawHead(AnimatedAttachmentPositionFollower follower)
        {
            Vector3 pos = follower.PositionOffset;
            Vector3 rot = follower.RotationOffset;
            float scale = follower.ScaleMultiplier;

            GUILayout.Space(4f);
            GUILayout.Label($"—— 头饰 {PartId(follower)} ——");
            GUILayout.Label($"身体世界缩放 {V(follower.ReferenceWorldScale)}  Y={R(follower.ReferenceWorldEuler.y)}°  头饰 {V(follower.AttachmentWorldScale)}");
            pos.x = Row("挂点局部 X", pos.x, -0.5f, 0.5f, 0.001f);
            pos.y = Row("挂点局部 Y", pos.y, 0f, 1.2f, 0.001f);
            pos.z = Row("挂点局部 Z", pos.z, -0.6f, 0.6f, 0.001f);
            rot.x = Row("旋转 X", rot.x, -45f, 45f, 0.1f);
            rot.y = Row("旋转 Y", rot.y, -45f, 45f, 0.1f);
            rot.z = Row("旋转 Z", rot.z, -45f, 45f, 0.1f);
            scale = Row("相对缩放", scale, 0.7f, 1.3f, 0.001f);

            follower.SetTuning(pos, rot, scale);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("头饰标准(旋转0/缩放1)"))
                follower.SetTuning(pos, Vector3.zero, 1f);
            if (GUILayout.Button("恢复进入页参数"))
                follower.SetTuning(_initialPosition, _initialRotation, _initialScale);
            GUILayout.EndHorizontal();

            string output = Output(follower);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("复制头饰参数"))
                GUIUtility.systemCopyBuffer = output;
            if (GUILayout.Button("输出头饰参数到日志"))
                Shenxiao.Framework.Util.GameLog.Info("UI3D", output);
            GUILayout.EndHorizontal();
        }

        /// <summary>武器块:offset 在 rhand(socket)局部空间——旋转量与 role_mount_profile.json 的
        /// rhandLocalEuler 一一对应(profile 现值为 0 时直接照抄),量完回烤进 Art 工程校准档。</summary>
        private void DrawWeapon(AttachmentSocketAligner aligner)
        {
            Vector3 pos = aligner.PositionOffset;
            Vector3 rot = aligner.RotationOffset;
            float scale = aligner.ScaleMultiplier;

            GUILayout.Space(6f);
            GUILayout.Label($"—— 武器 {PartId(aligner)} ——");
            GUILayout.Label($"rhand 世界 {V(aligner.ReferenceWorldEuler)}°  武器缩放 {V(aligner.AttachmentWorldScale)}");
            pos.x = Row("武器局部 X", pos.x, -0.4f, 0.4f, 0.001f);
            pos.y = Row("武器局部 Y", pos.y, -0.4f, 0.4f, 0.001f);
            pos.z = Row("武器局部 Z", pos.z, -0.4f, 0.4f, 0.001f);
            rot.x = Row("武器旋转 X", rot.x, -180f, 180f, 0.5f);
            rot.y = Row("武器旋转 Y", rot.y, -180f, 180f, 0.5f);
            rot.z = Row("武器旋转 Z", rot.z, -180f, 180f, 0.5f);
            scale = Row("武器缩放", scale, 0.5f, 1.5f, 0.001f);

            aligner.SetTuning(pos, rot, scale);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("武器标准(全 0/缩放1)"))
                aligner.SetTuning(Vector3.zero, Vector3.zero, 1f);
            if (GUILayout.Button("恢复进入页参数"))
                aligner.SetTuning(_initialWeaponPosition, _initialWeaponRotation, _initialWeaponScale);
            GUILayout.EndHorizontal();

            string output = WeaponOutput(aligner);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("复制武器参数"))
                GUIUtility.systemCopyBuffer = output;
            if (GUILayout.Button("输出武器参数到日志"))
                Shenxiao.Framework.Util.GameLog.Info("UI3D", output);
            GUILayout.EndHorizontal();
        }

        private static float Row(string label, float v, float min, float max, float step)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {R(v)}", GUILayout.Width(130f));
            if (GUILayout.Button("-", GUILayout.Width(24f))) v -= step;
            v = GUILayout.HorizontalSlider(v, min, max);
            if (GUILayout.Button("+", GUILayout.Width(24f))) v += step;
            GUILayout.EndHorizontal();
            return Mathf.Clamp(v, min, max);
        }

        private static string Output(AnimatedAttachmentPositionFollower f)
        {
            Vector3 p = f.PositionOffset;
            Vector3 r = f.RotationOffset;
            return $"HeadAttachment/{PartId(f)}: {{ \"position\": {{ \"x\": {R(p.x)}, \"y\": {R(p.y)}, \"z\": {R(p.z)} }}, " +
                   $"\"rotation\": {{ \"x\": {R(r.x)}, \"y\": {R(r.y)}, \"z\": {R(r.z)} }}, " +
                   $"\"scale\": {R(f.ScaleMultiplier)} }}";
        }

        private static string WeaponOutput(AttachmentSocketAligner a)
        {
            Vector3 p = a.PositionOffset;
            Vector3 r = a.RotationOffset;
            return $"WeaponAttachment/{PartId(a)}: {{ \"position\": {{ \"x\": {R(p.x)}, \"y\": {R(p.y)}, \"z\": {R(p.z)} }}, " +
                   $"\"rotation\": {{ \"x\": {R(r.x)}, \"y\": {R(r.y)}, \"z\": {R(r.z)} }}, " +
                   $"\"scale\": {R(a.ScaleMultiplier)} }}";
        }

        /// <summary>挂件 res 标识:取挂件实例名(如 "1300@idle(Clone)")的 @ 前段;解析不出就用原名。</summary>
        private static string PartId(Component c)
        {
            if (c == null) return "?";
            string n = c.gameObject.name;
            int clone = n.IndexOf("(Clone)", System.StringComparison.Ordinal);
            if (clone >= 0) n = n.Substring(0, clone);
            int at = n.IndexOf('@');
            return at > 0 ? n.Substring(0, at) : n;
        }

        private static float R(float v) => Mathf.Round(v * 10000f) / 10000f;
        private static string V(Vector3 v) => $"({R(v.x)}, {R(v.y)}, {R(v.z)})";
    }
}
#endif
