using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 按动作新老互切的角色模型驱动器(model_replacement.json 逐动作配置的运行时执行者):
    /// 播某动作时——清单配了新 prefab → 亮该动作的新模型实例(每动作一个,Timeline 自播,懒加载缓存);
    /// 没配 → 亮老拼装模型(懒构建一次)用老 clip 播。**只换 idle 时:待机=新模型,跑动/攻击自动
    /// 切回老模型**,一个动作一个动作地换,全按清单自动。新老实例都挂本容器下,外层(UI 台/场景台/
    /// MainRoleAgent)只拿容器当"模型",摆位/朝向/缩放照旧。
    /// </summary>
    public sealed class ReplaceableRoleModel : MonoBehaviour
    {
        // 循环动作(Timeline 播完回头);其余(attack/death/skill…)播完 Hold 停末帧,回待机由外层状态机驱动
        private static readonly HashSet<string> LoopActions = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "idle", "run", "walk", "collect", "ride", "ride2", "create3",
        };

        private RoleModelSpec _spec;
        private readonly Dictionary<string, GameObject> _newInstances =
            new Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase);
        private GameObject _oldModel;
        private Animation _oldAnim;
        private GameObject _active;
        private int _playVersion; // 加载期间又切了动作:旧结果作废(同 RoleCreateView 的 token 套路)
        private bool _buildingOld;

        public void Init(RoleModelSpec spec)
        {
            _spec = spec;
        }

        /// <summary>该动作能不能播:新配置在→能;老模型已建→查 clip;老模型未建→先放行(真播时缺再静默跳过)。</summary>
        public bool CanPlay(string action)
        {
            if (string.IsNullOrEmpty(action) || _spec == null) return false;
            if (ModelReplacement.GetPrefabKey("role", _spec.ClotheRes, action) != null) return true;
            if (_oldAnim != null) return _oldAnim.GetClip(action) != null;
            return true;
        }

        /// <summary>动作时长(秒):新=Timeline 时长(实例未加载过返回 0,外层节拍降级);老=clip 长度。</summary>
        public float GetLength(string action)
        {
            if (string.IsNullOrEmpty(action) || _spec == null) return 0f;
            if (ModelReplacement.GetPrefabKey("role", _spec.ClotheRes, action) != null)
            {
                if (_newInstances.TryGetValue(action, out GameObject inst) && inst != null)
                {
                    var d = inst.GetComponentInChildren<PlayableDirector>(true);
                    return d != null ? (float)d.duration : 0f;
                }
                return 0f;
            }
            if (_oldAnim != null)
            {
                AnimationClip clip = _oldAnim.GetClip(action);
                return clip != null ? clip.length : 0f;
            }
            return 0f;
        }

        /// <summary>同步入口(外层状态机用):内部异步加载,期间保持上一画面,加载完仍是最新请求才上台。</summary>
        public bool Play(string action, bool restart = false, float speed = 1f)
        {
            if (!CanPlay(action)) return false;
            _ = PlayAsync(action, restart, speed);
            return true;
        }

        public async Task PlayAsync(string action, bool restart = false, float speed = 1f)
        {
            if (string.IsNullOrEmpty(action) || _spec == null) return;
            int version = ++_playVersion;

            string key = ModelReplacement.GetPrefabKey("role", _spec.ClotheRes, action);
            if (key != null)
            {
                GameObject inst = await EnsureNewInstance(action, key);
                if (this == null || inst == null || version != _playVersion) return;
                Activate(inst);
                var director = inst.GetComponentInChildren<PlayableDirector>(true);
                if (director != null)
                {
                    director.extrapolationMode = LoopActions.Contains(action)
                        ? DirectorWrapMode.Loop : DirectorWrapMode.Hold;
                    if (restart || director.state != PlayState.Playing)
                    {
                        director.time = 0;
                        director.Play();
                    }
                    if (director.playableGraph.IsValid())
                        director.playableGraph.GetRootPlayable(0).SetSpeed(Mathf.Max(0.01f, speed));
                }
                return;
            }

            GameObject old = await EnsureOldModel();
            if (this == null || old == null || version != _playVersion) return;
            Activate(old);
            if (_oldAnim == null) return;
            if (_oldAnim.GetClip(action) == null)
            {
                await RoleModelAssembler.PrepareRoleActions(old, _spec.Career, _spec.ClotheRes, new[] { action });
                if (this == null || version != _playVersion) return;
            }
            if (_oldAnim.GetClip(action) == null) return; // 未转换的动作静默跳过(与老门禁一致)
            if (!restart && _oldAnim.IsPlaying(action)) return;
            if (restart) _oldAnim.Stop(action);
            AnimationState st = _oldAnim[action];
            if (st != null) st.speed = Mathf.Max(0.01f, speed);
            _oldAnim.CrossFade(action, 0.15f);
        }

        private void Activate(GameObject target)
        {
            if (_active == target)
            {
                if (target != null && !target.activeSelf) target.SetActive(true);
                return;
            }
            if (_active != null) _active.SetActive(false);
            _active = target;
            if (_active != null) _active.SetActive(true);
        }

        private async Task<GameObject> EnsureNewInstance(string action, string key)
        {
            if (_newInstances.TryGetValue(action, out GameObject cached) && cached != null) return cached;
            GameObject staged = await RoleModelAssembler.BuildNewModelAsync(_spec, key, action);
            if (staged == null) return null;
            if (this == null)
            {
                Object.Destroy(staged);
                return null;
            }
            staged.transform.SetParent(transform, false);
            staged.SetActive(false);
            _newInstances[action] = staged;
            return staged;
        }

        private async Task<GameObject> EnsureOldModel()
        {
            if (_oldModel != null) return _oldModel;
            while (_buildingOld) await Task.Yield(); // 并发请求等第一个建完
            if (_oldModel != null) return _oldModel;
            _buildingOld = true;
            try
            {
                GameObject old = await RoleModelAssembler.BuildOldModelAsync(new RoleModelSpec
                {
                    Career = _spec.Career,
                    ClotheRes = _spec.ClotheRes,
                    WeaponRes = _spec.WeaponRes,
                    HeadRes = _spec.HeadRes,
                    WingId = _spec.WingId,
                    BackOrnamentId = _spec.BackOrnamentId,
                    Actions = _spec.Actions,
                    AutoPlayActions = false,
                });
                if (old == null) return null;
                if (this == null)
                {
                    Object.Destroy(old);
                    return null;
                }
                old.transform.SetParent(transform, false);
                old.SetActive(false);
                _oldModel = old;
                _oldAnim = old.GetComponent<Animation>();
                return old;
            }
            finally
            {
                _buildingOld = false;
            }
        }
    }
}
