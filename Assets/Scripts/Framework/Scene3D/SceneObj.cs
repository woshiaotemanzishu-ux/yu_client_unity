using UnityEngine;

namespace Shenxiao.Framework.Scene3D
{
    /// <summary>
    /// Wraps a 3D model instance: handles loading, attachment of effects, lifecycle.
    /// Skeleton only in Phase 0; full implementation in Phase 2.
    /// </summary>
    public class SceneObj : MonoBehaviour
    {
        public string ResKey { get; protected set; }
    }
}
