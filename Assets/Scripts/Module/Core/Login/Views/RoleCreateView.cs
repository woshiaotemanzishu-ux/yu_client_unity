using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 创角页(重构版,自包含独立 prefab)——取代老端碎片化的 LoginCreateRoleView + LoginCreateRoleItem。
    ///
    /// 结构(对标截图 5/6):全屏背景 + 全屏展示视频(VideoImage) + 左侧职业选择列(4 张固定职业卡 careerItems)
    /// + 中央 3D 角色模型(ModelCon,无视频职业的拼装链兜底) + 职业名 + 职业描述三连图(诗句)
    /// + 随机名(名字输入 + ⟳ 随机)+ 底部「踏入仙界」+ 返回。
    /// prefab 由 RoleCreateCreator 纯代码建树生成并回填下列 public 引用;职业卡固定 4 张(不用模板克隆),
    /// 每张卡内直接持有 bg/icon/label 引用,位置各自手调;职业数不足则运行时隐藏多余卡。
    ///
    /// 本类只做:① 数据绑定(职业列表/展示视频或 3D 模型/随机名,逻辑原样搬自旧 LoginCreateRoleView.cs)
    ///          ② 功能性状态切换(选中职业换底图/头像、SetActive)。不写颜色/字号/尺寸等样式。
    ///
    /// ——— 对接说明(给主控 LoginFlow 接线用)———
    /// 暴露的 public 方法:
    ///   - void Refresh()                      // 建职业项 + 默认预选 + 显示视频或模型/职业名/描述/随机名(OnShow 自动调一次)
    /// 调用的 LoginFlow 静态方法(返回按钮):
    ///   - LoginFlow.ShowSelectRole()          // 有角色时回选角页
    ///   - LoginFlow.BackToEnter()             // 无角色时回踏入仙界页
    /// 调用的数据层(原样搬自旧 view):
    ///   - LoginConfigs.EnsureLoaded / CreateRoleOptions / GetCreateRes / RoleUIActions /
    ///     CreateRoleEffects / GetModelPos / RandomRoleName
    ///   - RoleModelAssembler.BuildAsync / PlayActions, EffectBinder.AttachOne / PlayOneShot, UIModelStage
    ///   - LoginController.Instance.SendCreateRole(name, career, sex)
    ///   - 监听 GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT
    /// 待主控/后续处理:本页职业「描述」配置里只有三连诗句图(Img1/2/3),无文字描述;
    ///   careerDescLabel 仅作占位文本节点(可由用户手填/隐藏),职业描述实际由 tips 三图呈现。
    /// </summary>
    [UIView("prefabs/ui/login/rolecreateview")]
    public sealed class RoleCreateView : BaseView
    {
        private const float MODEL_SCALE = 0.5f; // 老客户端字面量 show_model_data.scale
        private const int MAX_RANDOM_NAME_VERIFY_ATTEMPTS = 10; // 老端 max_random_count

        // 创角展示视频:object/role/video_create/{RoleRes}@create2.mp4(出场,播完接待机)
        // + {RoleRes}@create3.mp4(待机,循环)。对应老整模 prefab 的 create2/create3 两段
        // (model_create_* 整模资源已废弃删除,改视频交付;720×1280 H.264,Unity 内置 VideoPlayer 直播)。
        private const string VIDEO_KEY_BASE = "object/role/video_create/";

        [Header("背景 / 容器")]
        public Image bgImg;
        public RectTransform modelCon;           // 3D 模型容器(对标老 _gp_model_con;无视频职业的拼装链兜底)
        public RawImage videoImage;              // 全屏展示视频画面(运行时喂 VideoPlayer 的 RenderTexture)

        [Header("职业卡(固定 4 张,位置各自手调)")]
        public CareerItem[] careerItems;

        [Header("职业信息")]
        public TextMeshProUGUI careerNameLabel;  // 职业名("剑主"…)
        public TextMeshProUGUI careerDescLabel;  // 职业描述占位(无文字配置,实际描述用 tips 三图)
        public Image tipsImg;                    // 职业介绍三连图(诗句),对标老 _img_tips
        public Image tipsImg2;                   // 对标老 _img_tips2
        public Image tipsImg3;                   // 对标老 _img_tips3

        [Header("名字 / 按钮")]
        public Image nameBgImg;                  // 名字底框(对标老 _img_bg ui_Login_08)
        public TMP_InputField nameInput;         // 角色名输入(对标老 _lb_random_name TextInput)
        public Image randomBtn;                  // 随机名按钮 ⟳(对标老 _img_random)
        public Image enterBtn;                   // 踏入仙界(创建)按钮(对标老 _img_enter)
        public Image returnBtn;                  // 返回按钮(对标老 _img_return)

        /// <summary>一张固定职业卡的子控件引用(Creator 建树时回填;对标老 CareerSlot)。</summary>
        [System.Serializable]
        public class CareerItem
        {
            public GameObject root;   // 卡根节点(职业数不足时隐藏多余卡)
            public Image bg;          // 命中体 + 选中/未选底图
            public Image icon;        // 职业图标(选中/未选换图)
            public TMP_Text label;    // 职业名
        }

        private List<LoginConfigs.CareerOption> _options = new List<LoginConfigs.CareerOption>();
        private int _selectedIndex;
        private bool _creating;
        private bool _verifyingRandomName;

        private VideoPlayer _videoPlayer;        // 挂在 videoImage 上,懒建,全职业共用
        private RenderTexture _videoTexture;     // 视频画布,按 clip 尺寸懒建,OnDispose 释放
        private VideoClip _pendingIdleClip;      // create2 播完要接的待机段(loopPointReached 时切)

        protected override void OnInit()
        {
            BindClicks();
            EventDispatcher.On<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateResult);
        }

        protected override void OnShow(object args)
        {
            _creating = false;
            Refresh();
        }

        protected override void OnHide()
        {
            UIModelStage.Clear();
            StopVideo();
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateResult);
            if (_videoPlayer != null)
            {
                _videoPlayer.loopPointReached -= OnVideoLoopPoint;
                _videoPlayer.frameReady -= OnVideoFrameReady;
                _videoPlayer = null;
            }
            if (_videoTexture != null)
            {
                _videoTexture.Release();
                Destroy(_videoTexture);
                _videoTexture = null;
            }
        }

        // ---------------------------------------------------------------- 点击绑定(功能性,允许)

        private void BindClicks()
        {
            ClearAndAddClick(enterBtn, OnClickEnter);
            ClearAndAddClick(randomBtn, OnClickRandomName);
            ClearAndAddClick(returnBtn, OnClickReturn);
        }

        private static void ClearAndAddClick(Graphic target, System.Action onClick)
        {
            if (target == null) return;
            UIUtil.ClearClicks(target);   // 重绑前先清,防监听叠加
            UIUtil.AddClick(target, onClick);
        }

        // ---------------------------------------------------------------- 数据绑定

        /// <summary>建职业项 + 默认预选 + 显示模型/职业名/描述/随机名。OnShow 自动调一次,亦可由 LoginFlow 主动刷新。</summary>
        public void Refresh()
        {
            InitAsync();
        }

        private async void InitAsync()
        {
            await LoginConfigs.EnsureLoaded();
            _options = LoginConfigs.CreateRoleOptions();
            if (_options.Count == 0)
            {
                GameLog.Error("Login", "ConfigLogin.CreateRole.UI 为空(配置未同步?)");
                return;
            }
            BuildCareers();
            SelectCareer(WeightedRandomIndex()); // 老客户端 GetRandomIndex:按 random_weight 加权
            OnClickRandomName();
        }

        /// <summary>把职业数据填进固定职业卡:填名字 + 绑点击;职业数不足则隐藏多余卡(对标老 BindBakedCareers)。</summary>
        private void BuildCareers()
        {
            if (careerItems == null || careerItems.Length == 0)
            {
                GameLog.Error("Login", "创角缺 careerItems(未绑,重新生成 prefab)");
                return;
            }
            if (careerItems.Length < _options.Count)
            {
                GameLog.Warn("Login", "职业数 {0} 超过职业卡数 {1},多出的职业不显示(prefab 加卡或改 Creator)",
                    _options.Count, careerItems.Length);
            }

            for (int i = 0; i < careerItems.Length; i++)
            {
                CareerItem item = careerItems[i];
                if (item == null) continue;
                bool used = i < _options.Count;
                if (item.root != null) item.root.SetActive(used);
                if (!used) continue;

                if (item.label != null) item.label.text = _options[i].Name;
                if (item.bg != null)
                {
                    item.bg.raycastTarget = true;
                    int captured = i;
                    UIUtil.ClearClicks(item.bg);
                    UIUtil.AddClick(item.bg, () => SelectCareer(captured));
                }
            }
        }

        /// <summary>当前可选职业数(职业卡数与配置职业数取小)。</summary>
        private int ActiveCareerCount()
        {
            return Mathf.Min(careerItems?.Length ?? 0, _options.Count);
        }

        private int WeightedRandomIndex()
        {
            int total = 0;
            foreach (var o in _options) total += Mathf.Max(o.RandomWeight, 0);
            if (total <= 0) return 0;
            int roll = Random.Range(0, total);
            for (int i = 0; i < _options.Count; i++)
            {
                roll -= Mathf.Max(_options[i].RandomWeight, 0);
                if (roll < 0) return i;
            }
            return 0;
        }

        private void SelectCareer(int index)
        {
            int count = ActiveCareerCount();
            if (count <= 0) return;
            _selectedIndex = Mathf.Clamp(index, 0, count - 1);
            RefreshCareerStates();
            RefreshInfo();
            ShowCareerModel();
        }

        /// <summary>选中态:选中底图 ui_Login_02,未选 ui_Login_03;头像换 a/b 版(对标 LoginCreateRoleItem.ts)。</summary>
        private void RefreshCareerStates()
        {
            int count = ActiveCareerCount();
            for (int i = 0; i < count; i++)
            {
                CareerItem item = careerItems[i];
                if (item == null) continue;
                bool selected = i == _selectedIndex;
                string bg = selected ? "ui_Login_02" : "ui_Login_03";
                string icon = selected ? _options[i].SelectIcon : _options[i].UnselectIcon;
                if (item.bg != null)
                    _ = ResManager.SetImageAsync(item.bg, $"resource/game/login/texture/{bg}.png", nativeSize: false);
                if (item.icon != null)
                    _ = ResManager.SetImageAsync(item.icon, $"resource/game/login/texture/{icon}.png", nativeSize: false);
            }
        }

        /// <summary>职业名 + 右侧介绍三连图换图。位置/尺寸一律以 prefab 为准,不在代码里摆位。</summary>
        private void RefreshInfo()
        {
            var o = _options[_selectedIndex];
            if (careerNameLabel != null) careerNameLabel.text = o.Name;

            if (tipsImg != null)
                _ = ResManager.SetImageAsync(tipsImg, $"resource/game/login/other/{o.Img1}.png", nativeSize: false);
            if (tipsImg2 != null)
                _ = ResManager.SetImageAsync(tipsImg2, $"resource/game/login/other/{o.Img2}.png", nativeSize: false);
            if (tipsImg3 != null)
                _ = ResManager.SetImageAsync(tipsImg3, $"resource/game/login/other/{o.Img3}.png", nativeSize: false);
        }

        /// <summary>中央展示:视频优先(选中职业播 create2 出场→create3 待机循环);
        /// 未交付视频的职业走拼装链 3D 模型(衣+头饰+武器 + ConfigModelAni 的 create 动作序列,原样保留)。</summary>
        private async void ShowCareerModel()
        {
            var o = _options[_selectedIndex];
            LoginConfigs.CareerRes res = LoginConfigs.GetCreateRes(o.Career, o.Sex);
            if (res == null)
            {
                GameLog.Warn("Login", "CreateRole.Res 缺 {0}@{1}", o.Career, o.Sex);
                return;
            }
            int selectedAtRequest = _selectedIndex;

            // 视频优先:职业交付了创角展示视频就播视频(老整模 model_create_* 资源已废弃),没有再走老拼装链
            if (await TryShowVideo(res, selectedAtRequest)) return;
            StopVideo(); // 本职业没视频:收掉上个职业可能在播的画面,让位 3D 模型

            string[] actions = LoginConfigs.RoleUIActions("LoginCreateRoleView");
            GameObject model = await RoleModelAssembler.BuildAsync(new RoleModelSpec
            {
                Career = o.Career,
                ClotheRes = res.RoleRes,
                WeaponRes = res.WeaponRes,
                HeadRes = res.HeadRes,
                Actions = actions,
                AutoPlayActions = false,
            });
            if (model == null) return;
            if (selectedAtRequest != _selectedIndex || !gameObject.activeInHierarchy)
            {
                Destroy(model); // 加载期间切了职业/关了页:丢弃过期结果
                return;
            }
            var createEffects = new List<GameObject>();
            foreach ((string bone, string fx) in LoginConfigs.CreateRoleEffects(o.Career, o.Sex))
            {
                GameObject effect = await EffectBinder.AttachOne(
                    model, bone, "skills_effect", fx, "bone", playOnAttach: false);
                if (effect != null) createEffects.Add(effect);
            }
            if (selectedAtRequest != _selectedIndex || !gameObject.activeInHierarchy)
            {
                Destroy(model);
                return;
            }
            UIModelStage.ShowInstance(ModelCon(), model,
                MODEL_SCALE, LoginConfigs.GetModelPos("CreateRole", o.Career, o.Sex));
            RoleModelAssembler.PlayActions(model, actions);
            foreach (GameObject effect in createEffects)
            {
                EffectBinder.PlayOneShot(effect);
            }
        }

        /// <summary>
        /// 视频路径(美术交付的创角展示视频,取代已废弃的 model_create_* 整模 prefab):
        /// `object/role/video_create/{RoleRes}@create2`(出场,播一遍)→ `{RoleRes}@create3`(待机,循环)。
        /// 只交付 create2 → 播完停末帧;只交付 create3 → 直接循环。两段视频都不存在返回 false,
        /// 由调用方走老拼装路径。
        /// </summary>
        private async Task<bool> TryShowVideo(LoginConfigs.CareerRes res, int selectedAtRequest)
        {
            RawImage image = VideoImageOrNull();
            if (image == null) return false; // 老 prefab 没有 VideoImage 节点:重新生成 prefab 前先走拼装链

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL 不支持 VideoClip 资产播放(只支持 URL 流播):裸 mp4 由打包步骤发布到
            // {cdn}/WebGL/video/,浏览器 <video> 流式解码。存在性靠 Prepare 失败(404)判定。
            return await TryShowVideoByUrl(image, res, selectedAtRequest);
#else
            string baseKey = $"{VIDEO_KEY_BASE}{res.RoleRes}@";
            VideoClip intro = await ResManager.LoadOptionalAsync<VideoClip>(baseKey + "create2");
            VideoClip idle = await ResManager.LoadOptionalAsync<VideoClip>(baseKey + "create3");
            if (intro == null && idle == null) return false;
            // 加载期间切了职业/关了页:丢弃过期结果(新选中职业自会触发自己的 ShowCareerModel)
            if (selectedAtRequest != _selectedIndex || !gameObject.activeInHierarchy) return true;

            UIModelStage.Clear(); // 换下可能在台上的 3D 模型(其他职业的拼装链兜底)
            PlayCareerVideo(image, intro, idle);
            return true;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private string _pendingIdleUrl; // 出场段播完接的待机段 URL(对应 clip 版的 _pendingIdleClip)

        private async Task<bool> TryShowVideoByUrl(RawImage image, LoginConfigs.CareerRes res, int selectedAtRequest)
        {
            string baseUrl = $"{Shenxiao.Framework.Res.ResCdn.BaseUrl}/WebGL/video/{res.RoleRes}@";
            string introUrl = baseUrl + "create2.mp4";
            string idleUrl = baseUrl + "create3.mp4";

            VideoPlayer vp = EnsureVideoPlayer(image, null); // URL 模式尺寸走交付规格 720×1280
            vp.source = VideoSource.Url;

            bool introOk = await PrepareUrlAsync(vp, introUrl, 10f);
            if (!introOk && !await PrepareUrlAsync(vp, idleUrl, 10f)) return false; // 两段都取不到:拼装链兜底
            if (selectedAtRequest != _selectedIndex || !gameObject.activeInHierarchy) return true;

            UIModelStage.Clear();
            _pendingIdleClip = null;
            _pendingIdleUrl = introOk ? idleUrl : null; // 只有待机段时自循环
            vp.isLooping = !introOk;
            if (!image.enabled) vp.sendFrameReadyEvents = true;
            vp.Play();
            return true;
        }

        /// <summary>设 url 并 Prepare,等就绪/出错/超时。404 等错误走 errorReceived → false。</summary>
        private static async Task<bool> PrepareUrlAsync(VideoPlayer vp, string url, float timeoutSec)
        {
            bool ready = false, failed = false;
            void OnPrepared(VideoPlayer _) => ready = true;
            void OnError(VideoPlayer _, string msg) { failed = true; GameLog.Info("Login", "创角视频不可用 {0}: {1}", url, msg); }
            vp.prepareCompleted += OnPrepared;
            vp.errorReceived += OnError;
            try
            {
                vp.Stop();
                vp.url = url;
                vp.Prepare();
                float deadline = Time.realtimeSinceStartup + timeoutSec;
                while (!ready && !failed && Time.realtimeSinceStartup < deadline)
                    await Task.Yield();
                return ready;
            }
            finally
            {
                vp.prepareCompleted -= OnPrepared;
                vp.errorReceived -= OnError;
            }
        }
#endif

        /// <summary>
        /// 播创角展示视频:create2 出场播一遍 → loopPointReached 接 create3 待机循环。
        /// 切段/切职业不黑屏:换 clip 期间 RenderTexture 保留上一帧,新段首帧写入后自然覆盖
        /// (美术保证 create2 末帧 == create3 首帧姿势,切换处无缝)。
        /// </summary>
        private void PlayCareerVideo(RawImage image, VideoClip intro, VideoClip idle)
        {
            VideoClip first = intro != null ? intro : idle;
            VideoPlayer vp = EnsureVideoPlayer(image, first);

            _pendingIdleClip = intro != null ? idle : null;
            vp.Stop();
            vp.clip = first;
            vp.isLooping = intro == null; // 上来就是待机段 → 自循环;出场段播完由 OnVideoLoopPoint 接待机
            if (!image.enabled) vp.sendFrameReadyEvents = true; // 画面还没亮过:首帧就绪再亮,防黑帧
            vp.Play();
        }

        /// <summary>视频播放器/画布懒建:RenderTexture 按 clip 尺寸建(当前交付 720×1280),尺寸变了重建。</summary>
        private VideoPlayer EnsureVideoPlayer(RawImage image, VideoClip sizeRef)
        {
            // sizeRef 为空(WebGL URL 模式)时按交付规格 720×1280 建 RT
            int w = sizeRef != null && sizeRef.width > 0 ? (int)sizeRef.width : 720;
            int h = sizeRef != null && sizeRef.height > 0 ? (int)sizeRef.height : 1280;
            if (_videoTexture != null && (_videoTexture.width != w || _videoTexture.height != h))
            {
                if (_videoPlayer != null) _videoPlayer.targetTexture = null;
                _videoTexture.Release();
                Destroy(_videoTexture);
                _videoTexture = null;
                image.enabled = false; // 尺寸换了旧帧作废,等新首帧再亮
            }
            if (_videoTexture == null)
            {
                _videoTexture = new RenderTexture(w, h, 0);
                _videoTexture.Create();
                ClearToBlack(_videoTexture); // 未写入前内容未定义,清黑防花屏
            }

            if (_videoPlayer == null)
            {
                _videoPlayer = image.GetComponent<VideoPlayer>();
                if (_videoPlayer == null) _videoPlayer = image.gameObject.AddComponent<VideoPlayer>();
                _videoPlayer.playOnAwake = false;
                _videoPlayer.source = VideoSource.VideoClip;
                _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                _videoPlayer.audioOutputMode = VideoAudioOutputMode.None; // 展示视频无配音,页面音效另走音频系统
                _videoPlayer.skipOnDrop = true;
                _videoPlayer.loopPointReached += OnVideoLoopPoint;
                _videoPlayer.frameReady += OnVideoFrameReady;
            }
            _videoPlayer.targetTexture = _videoTexture;
            image.texture = _videoTexture;
            return _videoPlayer;
        }

        /// <summary>出场段播完接待机段;待机段自循环时 _pendingIdleClip 已清,直接忽略。</summary>
        private void OnVideoLoopPoint(VideoPlayer vp)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!string.IsNullOrEmpty(_pendingIdleUrl))
            {
                vp.url = _pendingIdleUrl;         // URL 模式换段:RT 保留出场末帧,画面无缝
                _pendingIdleUrl = null;
                vp.isLooping = true;
                vp.Play();
                return;
            }
#endif
            if (_pendingIdleClip == null) return; // 无待机段停末帧 / 待机段自循环:都不用管
            vp.clip = _pendingIdleClip;           // 换段期间 RT 保留出场末帧,画面无缝
            _pendingIdleClip = null;
            vp.isLooping = true;
            vp.Play();
        }

        /// <summary>首帧就绪才点亮画面(防黑帧/上个尺寸的残帧);之后关掉逐帧回调省开销。</summary>
        private void OnVideoFrameReady(VideoPlayer vp, long frameIdx)
        {
            vp.sendFrameReadyEvents = false;
            if (videoImage != null) videoImage.enabled = true;
        }

        private void StopVideo()
        {
            _pendingIdleClip = null;
            if (_videoPlayer != null) _videoPlayer.Stop();
            if (videoImage != null) videoImage.enabled = false;
        }

        private static void ClearToBlack(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = prev;
        }

        // ---------------------------------------------------------------- 事件

        private async void OnClickRandomName()
        {
            if (_verifyingRandomName || _options.Count == 0) return;
            _verifyingRandomName = true;
            try
            {
                int sex = _options[_selectedIndex].Sex;
                for (int attempt = 1; attempt <= MAX_RANDOM_NAME_VERIFY_ATTEMPTS; attempt++)
                {
                    string candidate = LoginConfigs.RandomRoleName(sex);
                    int result = await LoginController.Instance.VerifyRoleNameAsync(candidate);
                    if (this == null || !gameObject.activeInHierarchy) return;

                    if (result == 1)
                    {
                        if (nameInput != null) nameInput.text = candidate;
                        return;
                    }

                    GameLog.Info("Login", "随机名候选被拒绝 attempt={0} name={1} result={2}",
                        attempt, candidate, result);
                    if (!LoginController.IsRetryableRandomNameResult(result))
                    {
                        TipsManager.Toast(LoginController.GetRoleNameResultMessage(result));
                        return;
                    }
                }

                TipsManager.Toast("暂时没有生成可用角色名，请点击随机按钮重试或手动输入");
            }
            catch (System.Exception e)
            {
                GameLog.Warn("Login", "随机名验证失败: {0}", e.Message);
                TipsManager.Toast("角色名验证失败，请稍后重试");
            }
            finally
            {
                _verifyingRandomName = false;
            }
        }

        private void OnClickEnter()
        {
            if (_creating) return;
            if (_verifyingRandomName)
            {
                TipsManager.Toast("正在生成可用角色名，请稍候");
                return;
            }
            string roleName = (nameInput != null ? nameInput.text : string.Empty).Trim();
            if (string.IsNullOrEmpty(roleName))
            {
                GameLog.Warn("Login", "角色名为空");
                TipsManager.Toast("请输入角色名");
                return;
            }
            _creating = true;
            var o = _options[_selectedIndex];
            LoginController.Instance.SendCreateRole(roleName, o.Career, o.Sex);
        }

        private void OnCreateResult(int result)
        {
            _creating = false;
            if (result == 1) return; // 成功:LoginController 已自动 10004 进入游戏

            string message = LoginController.GetRoleNameResultMessage(result);
            GameLog.Warn("Login", "创角失败: {0} result={1}", message, result);
            TipsManager.Toast(message);
        }

        /// <summary>对标老客户端:有角色 → 回选角页;无角色 → 断线回踏入仙界页。</summary>
        private void OnClickReturn()
        {
            if (LoginModel.Instance.Roles.Count > 0) LoginFlow.ShowSelectRole();
            else LoginFlow.BackToEnter();
        }

        // ——— 容器字段没绑上时按名兜底 ———
        private RectTransform ModelCon() => modelCon != null ? modelCon : transform.Find("ModelCon") as RectTransform;

        /// <summary>视频画面字段没绑上时按名兜底;老 prefab 没有此节点则返回 null(重新生成 prefab 即有)。</summary>
        private RawImage VideoImageOrNull()
        {
            if (videoImage != null) return videoImage;
            Transform t = transform.Find("VideoImage");
            if (t != null) videoImage = t.GetComponent<RawImage>();
            return videoImage;
        }
    }
}
