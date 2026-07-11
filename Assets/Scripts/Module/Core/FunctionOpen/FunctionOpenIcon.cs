using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.FunctionOpen;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.FunctionOpen
{
    /// <summary>
    /// 功能预告入口框(对标老端 funcOpen/FunctionOpenIcon.ts):右侧常驻框,icon_con 上下浮动,
    /// 当有≥2个即将开放功能时每 10 秒轮播切换(3D模型 + 文字),点击打开功能预告界面(FunctionOpenListView)。
    /// 数据走 FunctionOpenModel.ShowFuncOpenIcons();3D模型复用 MainUIActivityView.LoadCycleModelAsync 套路
    /// (per-instance UIModelStage + module/key 映射 + ClientOutWardPos 取景)。
    /// </summary>
    public sealed class FunctionOpenIcon : FunctionOpenIconBind
    {
        private const float FloatDistance = 10f;
        private const float FloatDuration = 0.7f;   // 老端 Laya.Tween 700ms 单程
        private const float CarouselInterval = 10f; // 老端 10 秒轮播

        private UIModelStage _modelStage;
        private Coroutine _floatCo;
        private Coroutine _carouselCo;
        private Vector2 _floatBase;
        private bool _hasFloatBase;

        private readonly List<FunctionOpenConfigs.Cfg> _icons = new List<FunctionOpenConfigs.Cfg>();
        private int _showIndex;
        private string _modelShowKey; // 当前已上台的模型 key,防重复加载

        // 太极折叠态 + 数据侧期望可见性:框实际显隐 = _iconsShouldShow && !_folded(见 ApplyVisibility)。
        private bool _folded;
        private bool _iconsShouldShow;

        protected override void OnInit()
        {
            if (icon != null) icon.gameObject.SetActive(false);
            if (_box_model != null) _box_model.gameObject.SetActive(false);
            if (_img_model_txt != null) _img_model_txt.gameObject.SetActive(false);
            if (red_dot != null) red_dot.gameObject.SetActive(false);
            if (tips != null) tips.color = new Color32(0xff, 0xe6, 0x59, 0xff); // 老端 LoadSuccess:tips 金色 #ffe659
            if (click_box != null) UIUtil.AddClick(click_box, OnClick);
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("FuncOpen", "Icon OnShow");
            EventDispatcher.On(GlobalEvent.EVT_FUNC_OPEN_UPDATE, OnDataUpdate);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnDataUpdate);
            EventDispatcher.On<bool>(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, OnActivityFold);
            StartFloat();
            _ = RefreshAsync();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_FUNC_OPEN_UPDATE, OnDataUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnDataUpdate);
            EventDispatcher.Off<bool>(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, OnActivityFold);
            StopFloat();
            StopCarousel();
            _modelStage?.ClearStage();
            _modelShowKey = null; // ClearStage 已销毁旧模型,清 key 让下次 Show 重新加载(否则去重守卫会跳过→空白)
        }

        private void OnDestroy()
        {
            _modelStage?.Dispose();
            _modelStage = null;
        }

        private void OnDataUpdate() => _ = RefreshAsync();

        // 太极折叠(对标老端 FunctionOpenIcon 订阅 UPDATE_ACTIVITY_STATE→整框滑出右边+淡出):收放功能预告框。
        // 折叠态"粘住"——折叠后即便数据刷新(轮播/等级变化)也保持隐藏;展开时按当前是否有预告恢复。
        private void OnActivityFold(bool folded)
        {
            _folded = folded;
            ApplyVisibility();
        }

        // 框实际显隐 = 有即将开放功能(_iconsShouldShow)且未折叠(!_folded)。数据侧只改 _iconsShouldShow,
        // 折叠/展开只改 _folded,统一由此重算(对标 MainUIRankView.ApplyRankVisibility 的守卫)。
        private void ApplyVisibility()
        {
            if (click_box != null) click_box.gameObject.SetActive(_iconsShouldShow && !_folded);
        }

        private void OnClick()
        {
            // 点击打开功能预告界面(FunctionOpenListView);opener 由 FunctionOpenBootstrap 注册(Stage 2)。
            MainUIRouter.Open("functionopen");
        }

        private async Task RefreshAsync()
        {
            await FunctionOpenConfigs.EnsureLoaded();
            if (this == null) return;

            _icons.Clear();
            _icons.AddRange(FunctionOpenModel.Instance.ShowFuncOpenIcons());
            GameLog.Info("FuncOpen", "Icon refresh: cfgCount={0} icons={1} finish={2} roleLv={3} first={4}",
                FunctionOpenConfigs.All != null ? FunctionOpenConfigs.All.Count : -1, _icons.Count,
                FunctionOpenModel.Instance.FinishState.Count,
                RoleModel.Instance.HasBaseInfo ? RoleModel.Instance.Level : -1,
                _icons.Count > 0 ? _icons[0].Id : -1);

            if (_icons.Count == 0)
            {
                // 无即将开放功能:整框隐藏(view 仍在,继续监听数据)。
                _iconsShouldShow = false;
                ApplyVisibility();
                StopCarousel();
                return;
            }

            _iconsShouldShow = true;
            ApplyVisibility();
            if (red_dot != null) red_dot.gameObject.SetActive(FunctionOpenModel.Instance.HaveRewardCanGet());

            _showIndex = 0;
            ShowOne(_icons[0]);
            if (_icons.Count >= 2) StartCarousel();
            else StopCarousel();
        }

        private void ShowOne(FunctionOpenConfigs.Cfg cfg)
        {
            if (cfg == null) return;
            if (tips != null) tips.text = StripHtml(cfg.IconText); // 短解锁标签"X级开启"(老端 SetTip 用 icon_text);IconOpenText 长广告语归列表/详情

            if (cfg.HasModel)
            {
                if (icon != null) icon.gameObject.SetActive(false);
                if (_box_model != null) _box_model.gameObject.SetActive(true);
                if (_img_model_txt != null)
                {
                    _img_model_txt.gameObject.SetActive(true);
                    _ = ResManager.SetImageAsync(_img_model_txt, GameResPath.GetIconOtherPath("functionOpen", "icon_txt_" + cfg.Id), false, true);
                }
                _ = LoadModelAsync(cfg);
            }
            else
            {
                if (_box_model != null) _box_model.gameObject.SetActive(false);
                if (_img_model_txt != null) _img_model_txt.gameObject.SetActive(false);
                _modelStage?.ClearStage(); // 切 2D 图标:停掉离屏模型的稳态 RT 渲染开销
                _modelShowKey = null;
                if (icon != null)
                {
                    icon.gameObject.SetActive(true);
                    _ = ResManager.SetImageAsync(icon, GameResPath.GetIcon("functionOpen", cfg.Icon), false, false);
                }
            }
        }

        // 3D 展示模型(对标老端 LoopSHow → SetRoleModel):module 按 model_type 加载 prefab,取景取 ClientOutWardPos。
        private async Task LoadModelAsync(FunctionOpenConfigs.Cfg cfg)
        {
            if (_box_model == null) return;
            string module = ModuleOf(cfg.ModelType);
            if (module == null) return;
            string key = ModelKey(module, cfg.Model);
            if (_modelShowKey == key) return; // 已上台
            _modelShowKey = key;

            GameObject prefab = await ResManager.LoadAsync<GameObject>(key);
            if (this == null || _box_model == null) { return; }
            if (prefab == null) { _modelShowKey = null; GameLog.Warn("FuncOpen", "预告模型加载失败 key={0}", key); return; }

            await ClientOutWardPosConfigs.EnsureLoaded();
            if (this == null || _box_model == null) return;
            UiModelParameterConfigs.ModelParam mp = ClientOutWardPosConfigs.Get(
                "FunctionOpenIcon_" + PreX(cfg.ModelType) + "_" + cfg.Model, DefaultPosKey(cfg.ModelType));

            if (_modelStage == null) _modelStage = new UIModelStage(); // 独立台,不被背包/角色等抢占
            GameObject inst = Instantiate(prefab);
            // ClientOutWardPos 的 rotate 是相对「面向相机(180)」的偏移;不+180 模型会背对屏幕。
            _modelStage.PlaceInstance(_box_model, inst, mp.Scale, mp.Position, mp.Rotate + 180f);
            _ = PlayModelAction(inst, module, cfg.Model);
        }

        private static async Task PlayModelAction(GameObject model, string module, string id)
        {
            if (model == null) return;
            AnimationClip clip = await ResManager.LoadAsync<AnimationClip>("object/" + module + "/action/" + id + "/idle");
            if (model == null || clip == null) return;
            Animation anim = model.GetComponent<Animation>();
            if (anim == null) anim = model.AddComponent<Animation>();
            if (anim.GetClip("idle") == null) anim.AddClip(clip, "idle");
            anim.Play("idle");
        }

        // ---------- 轮播(≥2 个即将开放功能时每 10 秒切换) ----------
        private void StartCarousel()
        {
            StopCarousel();
            if (!gameObject.activeInHierarchy) return;
            _carouselCo = StartCoroutine(CarouselRoutine());
        }

        private void StopCarousel()
        {
            if (_carouselCo != null) { StopCoroutine(_carouselCo); _carouselCo = null; }
        }

        private IEnumerator CarouselRoutine()
        {
            var wait = new WaitForSeconds(CarouselInterval);
            while (_icons.Count >= 2)
            {
                yield return wait;
                _showIndex = (_showIndex + 1) % _icons.Count;
                ShowOne(_icons[_showIndex]);
            }
        }

        // ---------- icon_con 上下浮动 ----------
        private void StartFloat()
        {
            StopFloat();
            if (icon_con == null || !gameObject.activeInHierarchy) return;
            if (!_hasFloatBase) { _floatBase = icon_con.anchoredPosition; _hasFloatBase = true; }
            _floatCo = StartCoroutine(FloatRoutine());
        }

        private void StopFloat()
        {
            if (_floatCo != null) { StopCoroutine(_floatCo); _floatCo = null; }
            if (icon_con != null && _hasFloatBase) icon_con.anchoredPosition = _floatBase;
        }

        private IEnumerator FloatRoutine()
        {
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime / FloatDuration;
                float ping = Mathf.PingPong(t, 1f);
                icon_con.anchoredPosition = _floatBase + new Vector2(0f, FloatDistance * ping);
                yield return null;
            }
        }

        private static string StripHtml(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<BR>", "\n");
        }

        // model_type → 对象模块(加载 prefab 用;同循环冲榜 + child/god/littlepet)。
        private static string ModuleOf(int modelType)
        {
            switch (modelType)
            {
                case 2: return "mount";
                case 3: return "wing";
                case 4: return "weapon";
                case 5: return "fabao";
                case 7: return "spirit";
                case 9: return "child";
                case 10: return "god";
                case 11: return "littlepet";
                case 20: return "back";
                default: return null;
            }
        }

        private static string ModelKey(string module, string model)
        {
            string resName = module == "weapon" ? "model_weapon_r_" + model : "model_" + module + "_" + model;
            return "object/" + module + "/" + resName + "/" + resName;
        }

        // model_type → ClientOutWardPos key 前缀(取景用,对标老端 FunctionOpenIcon switch)。
        private static string PreX(int modelType)
        {
            switch (modelType)
            {
                case 2: return "s";
                case 3: return "w";
                case 4: return "d";
                case 5: return "a";
                case 7: return "p";
                case 9: return "bb";
                case 10: return "js";
                case 11: return "yl";
                case 20: return "b";
                default: return "s";
            }
        }

        private static string DefaultPosKey(int modelType)
        {
            switch (modelType)
            {
                case 3: return "default_wing";
                case 4: return "default_weapon";
                case 5: return "default_artifact";
                case 20: return "default_back_ornament";
                default: return "default_sprite"; // 2/7/9/10/11 老端均回退 default_sprite
            }
        }
    }
}
