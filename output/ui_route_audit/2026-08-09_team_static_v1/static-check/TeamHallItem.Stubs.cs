using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine
{
    public class Object { }
    public class Component : Object
    {
        public GameObject gameObject = new GameObject();
        public T GetComponentInChildren<T>(bool includeInactive) where T : class => null;
    }
    public class Transform : Component { }
    public class RectTransform : Transform
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector3 localScale;
    }
    public class GameObject : Object
    {
        public string name;
        public Transform transform = new RectTransform();
        public void SetActive(bool value) { }
        public T GetComponent<T>() where T : class => null;
    }
    public struct Vector2
    {
        public Vector2(float x, float y) { }
        public static Vector2 zero => new Vector2();
    }
    public struct Vector3 { public static Vector3 one => new Vector3(); }
}

namespace UnityEngine.UI
{
    public class Image : UnityEngine.Component { }
}

namespace TMPro
{
    public class TextMeshProUGUI : UnityEngine.Component { public string text; }
}

namespace Shenxiao.Framework.UI
{
    public class BaseView : UnityEngine.Component
    {
        protected virtual void BindNodes() { }
        protected virtual void OnInit() { }
        protected void EnsureBound(string name, object value) { }
        public void Show() { }
    }
    public static class UIUtil
    {
        public static void AddClick(UnityEngine.RectTransform target, Action callback) { }
    }
}

namespace Shenxiao.Framework.Res
{
    public static class ResManager
    {
        public static Task<UnityEngine.GameObject> InstantiateAsync(string path, UnityEngine.RectTransform parent) =>
            Task.FromResult<UnityEngine.GameObject>(null);
    }
    public static class GameResPath
    {
        public static string GetUIPrefab(string module, string view) => string.Empty;
    }
}

namespace Shenxiao.Framework.Util
{
    public static class GameLog { }
}

namespace Shenxiao.Module.Core.Common
{
    public sealed class CustomHeadItem : Shenxiao.Framework.UI.BaseView
    {
        public void SetRoleData(int career, int turn, int level, bool showLevel) { }
        public void SetActiveFrame(bool active) { }
    }
}

namespace Shenxiao.Module.Core.MainUI
{
    public static class MainUIConfigs
    {
        public static bool IsFieldScene(int sceneId) => false;
    }
}

namespace Shenxiao.Module.Core.Role
{
    public sealed class RoleModel
    {
        public static readonly RoleModel Instance = new RoleModel();
        public long RoleId;
        public int SceneId;
    }
}

namespace Shenxiao.Module.Core.Team
{
    public sealed class FigureProto
    {
        public string name;
        public int level;
        public int career;
        public int turn;
    }
    public sealed class TeamModel
    {
        public const int TEAMER_MAX = 3;
        public const int TEAM_LEADER = 1;
        public static readonly TeamModel Instance = new TeamModel();
        public int ActivityId;
        public int ActivitySubId;
        public sealed class MemberVo
        {
            public long Id;
            public int TeamPosition;
            public FigureProto Figure;
            public int SceneId;
            public int Online;
        }
        public sealed class HallEntryVo
        {
            public long TeamId;
            public int Num;
            public List<MemberVo> Members = new List<MemberVo>();
        }
    }
    public sealed class TeamController
    {
        public static readonly TeamController Instance = new TeamController();
        public void RequestJoinTeam(long teamId, int activityId, int subtype) { }
    }
}
