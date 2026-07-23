using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.CustomActivity;
using Shenxiao.Module.Core.CycleimpActlist;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面右上「竞榜 / 头号玩家榜」卡片(_box_rank),从 MainUIActivityView 拆出的独立区域视图。
    /// 承载:头号玩家榜(EVT_TOPPLAYER_MAIN_DATA)、循环冲榜(EVT_CYCLEIMP_DATA/CLOSE,含 3D 展示模型)、
    /// 逐秒倒计时。活动图标网格仍归 MainUIActivityView;本视图只管这张卡。
    /// </summary>
    public sealed class MainUIRankView : MainUIRankViewBind
    {
        // 头号玩家榜(_box_rank):奖励道具 + 逐秒倒计时。
        private EquipmentItem _topPlayerReward;
        private bool _rewardBuilding;       // 奖励 item 异步实例化中(防多 rank_type 事件并发重复创建)
        private int _pendingRewardTypeId;   // 最新待应用的奖励 type_id(建好后统一 SetData)
        private Coroutine _topPlayerTimer;
        private long _topPlayerEndTimeSec;

        // 循环冲榜分支:当前有冲榜活动时,_box_rank 走竞榜展示(3D模型+竞榜名+榜首),并压制头号玩家分支(对标老端 MainUIActivityView.ts:234)。
        private bool _cycleActive;
        private string _cycleModelShowId;       // 已展示的 3D 模型 show_id;相同则不重载
        private UIModelStage _cycleModelStage;  // 循环冲榜【独立】模型台(常驻主界面,不被背包/角色等抢占)

        private bool _clickBound;

        // 折叠态(太极) + 数据侧期望可见性:卡片实际显隐 = _rankShouldShow && !_folded(见 ApplyRankVisibility)。
        private bool _folded;
        private bool _rankShouldShow;

        private static readonly Color TimeRunColor = new Color(0x88 / 255f, 1f, 0x43 / 255f); // #88ff43 绿(倒计时)
        private static readonly Color TimeEndColor = new Color(1f, 0x47 / 255f, 0x15 / 255f); // #ff4715 红(已结束/结算中)

        // 老端 MainUIActivityView.icon_dir(MainUIActivityView.ts:71-90):rank_type_(sex|career) → 头号玩家榜奖励道具 type_id。
        private static readonly Dictionary<string, int> TopPlayerRewardTypeId = new Dictionary<string, int>
        {
            { "1_1", 101046074 }, { "2_1", 101066074 }, { "3_1", 101026074 }, { "4_1", 101106074 }, { "5_1", 101086074 }, { "6_1", 101016074 },
            { "1_2", 102046074 }, { "2_2", 102066074 }, { "3_2", 102026074 }, { "4_2", 102106074 }, { "5_2", 102086074 }, { "6_2", 102016074 },
            { "6_3", 103016074 }, { "6_4", 104016074 },
            { "13_1", 101106074 }, { "13_2", 102106074 }, { "14_1", 101086074 }, { "14_2", 102086074 },
        };

        protected override void OnInit()
        {
            // 样式与几何(边框图 mainui_ui_37、名牌底图 mainui_ui_38、effect 挂点尺寸、红点位置)全部烤在
            // HudRank.prefab 里 —— 所见即所得,想调去 prefab 改;本类只管数据绑定与显隐。
            player_name.text = "";
            name.text = "";
            time.text = "";
            _box_rank.gameObject.SetActive(false);
            _img_red.gameObject.SetActive(false);
            effect.gameObject.SetActive(false);
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On<int>(GlobalEvent.EVT_TOPPLAYER_MAIN_DATA, OnTopPlayerMainData);
            EventDispatcher.On(GlobalEvent.EVT_CYCLEIMP_DATA, OnCycleData);
            EventDispatcher.On(GlobalEvent.EVT_CYCLEIMP_CLOSE, OnCycleClose);
            EventDispatcher.On<bool>(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, OnActivityFold);
            OnCycleData(); // OnShow 时若冲榜数据已到(先于本视图创建),补刷一次
        }

        protected override void OnHide()
        {
            EventDispatcher.Off<int>(GlobalEvent.EVT_TOPPLAYER_MAIN_DATA, OnTopPlayerMainData);
            EventDispatcher.Off(GlobalEvent.EVT_CYCLEIMP_DATA, OnCycleData);
            EventDispatcher.Off(GlobalEvent.EVT_CYCLEIMP_CLOSE, OnCycleClose);
            EventDispatcher.Off<bool>(GlobalEvent.EVT_MAINUI_ACTIVITY_FOLD, OnActivityFold);
            StopTopPlayerTimer();
        }

        /// <summary>太极折叠(对标老端 ShowAnimation 里 _box_rank.visible=false):收放竞榜/头号玩家卡。
        /// 折叠态要"粘住"——即便折叠后服务端又推来竞榜/头号玩家数据,卡片也保持隐藏;展开时按当前数据恢复。</summary>
        private void OnActivityFold(bool folded)
        {
            _folded = folded;
            ApplyRankVisibility();
        }

        // 卡片实际显隐 = 数据要显(_rankShouldShow)且未折叠(!_folded)。数据处理器改走 SetRankShouldShow,
        // 折叠/展开只改 _folded 后统一由此重算(对标 MainUIActivityView._activityFolded / MainUISecondaryView 的守卫)。
        private void SetRankShouldShow(bool show)
        {
            _rankShouldShow = show;
            ApplyRankVisibility();
        }

        private void ApplyRankVisibility()
        {
            if (_box_rank != null) _box_rank.gameObject.SetActive(_rankShouldShow && !_folded);
        }

        // 对标老端 UPDATE_TOP_PLAYER_MAIN_DATA / UpdateTopPlayerData:面板现身 + 奖励道具 + 榜首名/活动名 + 倒计时。
        // 头号玩家路用 2D 奖励(icon_box),隐 3D 模型(model_gp 是循环冲榜 22702 路才用,未移植)。
        private void OnTopPlayerMainData(int rankType)
        {
            if (this == null || _box_rank == null) return;
            // 有循环冲榜活动时 _box_rank 归竞榜分支,头号玩家让位(对标老端 MainUIActivityView.ts:234 cycle.type!=0 压制 topplayer)。
            if (_cycleActive || CycleimpActlistModel.Instance.HasActivity) return;
            if (!TopPlayerController.GatePasses())
            {
                SetRankShouldShow(false);
                StopTopPlayerTimer();
                return;
            }

            TopPlayerModel.RankInfo info = TopPlayerModel.Instance.GetRankInfo(rankType);
            if (info == null) return;

            // 面板现身:OnInit 里 _box_rank 被 SetActive(false),老端靠 ShowAnimation 切 visible,这里在拿到数据时打开
            //(折叠态下只记期望、不真显,见 SetRankShouldShow)。
            SetRankShouldShow(true);
            if (icon_box != null) icon_box.gameObject.SetActive(true);
            if (model_gp != null) model_gp.gameObject.SetActive(false); // 3D 模型路(循环冲榜)未移植,头号玩家路不用

            // 活动名 + 榜首名(无人则“榜单虚位以待”)。
            RushRankConfigs.RushRankCfg cfg = RushRankConfigs.Get(rankType);
            if (name != null && cfg != null) name.text = cfg.Name;
            if (player_name != null) player_name.text = info.FirstName() ?? "榜单虚位以待";

            // 奖励道具(对标老端 icon_dir[now_key] → EquipmentItem.SetData)。
            BuildTopPlayerReward(rankType);

            // 倒计时:头号玩家是开服≤8活动,开服>7直接“已结束”不跑计时(老端 UpdateTopPlayerData:344);否则正常倒计时。
            if (ServerTimeModel.GetOpenServerDay() > 7) { StopTopPlayerTimer(); SetTimeText("已结束", TimeEndColor); }
            else RefreshRankCountdown(info.EndTime);

            // 注:effect 挂点(RankEffectSlot)在 prefab 里收成卡内尺寸(要改去 prefab 调)。老端此处是全屏 ui_cb01
            // 特效但从未接线/验证过,贸然挂会盖屏,故先不挂(待确认特效挂点/尺寸后再补)。
        }

        // 循环冲榜数据更新(EVT_CYCLEIMP_DATA):_box_rank 走竞榜展示——竞榜名 + 榜首(或虚位以待)+ 倒计时(upon_end)+ 3D 展示模型。
        private void OnCycleData()
        {
            if (this == null || _box_rank == null) return;
            CycleimpActlistModel m = CycleimpActlistModel.Instance;
            if (!m.HasActivity) return;

            _cycleActive = true;
            SetRankShouldShow(true); // 折叠态下只记期望、展开时恢复
            if (icon_box != null) icon_box.gameObject.SetActive(false); // 竞榜路不显 2D 奖励
            _ = LoadCycleModelAsync();                                   // 3D 展示模型(坐骑/翅膀/武器…)挂上 UIModelStage

            if (name != null) name.text = CycleRankName(m.Type);
            CycleimpActlistModel.RankRoleVo first = m.GetFirstHolder();
            if (player_name != null)
                player_name.text = first != null ? "S" + first.ServerId + "." + first.RoleName : "虚位以待";

            // 上榜截止(upon_end)优先(老端 StartTimer 用它);某些活动可能不传(=0),回退活动结束时间 end,保证倒计时有数据。
            long endSec = m.UponEndTime > 0 ? m.UponEndTime : m.EndTime;
            RefreshRankCountdown(endSec); // 复用同一 time 标签 + 倒计时协程(循环冲榜无“开服>7提前结束”)
        }

        // 循环冲榜关闭(EVT_CYCLEIMP_CLOSE):收起竞榜展示。头号玩家分支下次 EVT_TOPPLAYER_MAIN_DATA 时再接管。
        private void OnCycleClose()
        {
            if (this == null) return;
            _cycleActive = false;
            _cycleModelShowId = null;
            StopTopPlayerTimer();
            _cycleModelStage?.ClearStage(); // 清自己台子的模型(不动别人共享的默认台)
            SetRankShouldShow(false);
        }

        private void OnDestroy()
        {
            _cycleModelStage?.Dispose(); // 释放独立模型台的相机/RT/RawImage
            _cycleModelStage = null;
        }

        // 竞榜名(对标老端 MainUIActivityView.ts:417-435 按 type 硬编码的「X竞榜」短名)。
        private static string CycleRankName(int type)
        {
            switch (type)
            {
                case 1: return "坐骑竞榜";
                case 2: return "剑魄同修竞榜";
                case 3: return "垂神翼影竞榜";
                case 4: return "古法符相竞榜";
                case 5: return "殒锋天刃竞榜";
                case 6: return "玄穹云披竞榜";
                default: return "竞榜";
            }
        }

        // 加载并展示循环冲榜 3D 模型(对标老端 SetRoleModel):show_id→模块→prefab 挂 UIModelStage,再播 idle/show 动画。
        // 沿用 DialogueView.ShowNpcModel 套路(独立展示模型,非角色装备);取景参数取 uimodelparameter["MainUIActivityView"][show_id]。
        private async Task LoadCycleModelAsync()
        {
            if (model_gp == null) return;
            CycleimpActlistModel m = CycleimpActlistModel.Instance;
            await CycleimpActlistConfigs.EnsureLoaded();
            if (this == null || !_cycleActive || !m.HasActivity) return;

            CycleimpActlistConfigs.Cfg cfg = CycleimpActlistConfigs.Get(m.Type, m.Subtype);
            string module = cfg != null ? ModuleOf(cfg.ShowType) : null;
            // 神兵(活动 type==5):show_id 是按职业的 JSON 数组,取 career-1;其余直接用 show_id。
            string showId = cfg == null ? null
                : (m.Type == 5 ? CareerShowId(cfg.ShowId, RoleModel.Instance.Career) : cfg.ShowId);
            GameLog.Info("CycleModel", "准备模型: type={0} sub={1} cfgFound={2} showType={3} module={4} showId={5}",
                m.Type, m.Subtype, cfg != null, cfg?.ShowType ?? -1, module ?? "<null>", showId ?? "<null>");
            if (cfg == null || string.IsNullOrEmpty(showId) || module == null)
            {
                model_gp.gameObject.SetActive(false);
                return;
            }
            if (_cycleModelShowId == showId) return; // 已展示该模型,避免 22700/22702 重复加载
            _cycleModelShowId = showId;              // 乐观占位,防并发重复加载;失败再清

            string key = CycleModelKey(module, showId);
            GameObject prefab = await ResManager.LoadAsync<GameObject>(key);
            if (this == null || !_cycleActive || model_gp == null) return;
            if (prefab == null)
            {
                GameLog.Warn("CycleModel", "模型 prefab 加载失败 key={0}", key);
                _cycleModelShowId = null; // 允许重试
                model_gp.gameObject.SetActive(false);
                return;
            }

            await UiModelParameterConfigs.EnsureLoaded();
            if (this == null || !_cycleActive || model_gp == null) return;
            UiModelParameterConfigs.ModelParam mp = UiModelParameterConfigs.Get("MainUIActivityView", showId);

            if (_cycleModelStage == null) _cycleModelStage = new UIModelStage(); // 独立台,不被背包/角色等抢占
            model_gp.gameObject.SetActive(true);
            GameObject inst = Instantiate(prefab);
            _cycleModelStage.PlaceInstance(model_gp, inst, mp.Scale, mp.Position, mp.Rotate);
            _ = PlayModelAction(inst, module, showId, mp.Action);
            GameLog.Info("CycleModel", "模型上台 key={0} scale={1} action={2}", key, mp.Scale, mp.Action);
        }

        // 播放展示模型动画(对标 DialogueView.PlayNpcIdle):object/{module}/action/{id}/{action}(idle 或神兵的 show)。
        private static async Task PlayModelAction(GameObject model, string module, string id, string action)
        {
            if (model == null) return;
            if (string.IsNullOrEmpty(action)) action = "idle";

            Animation anim = model.GetComponent<Animation>();
            if (anim != null && anim.GetClip(action) != null)
            {
                anim.Play(action);
                return;
            }

            AnimationClip clip = await ResManager.LoadAsync<AnimationClip>("object/" + module + "/action/" + id + "/" + action);
            if (model == null || clip == null) return;
            if (anim == null) anim = model.AddComponent<Animation>();
            if (anim.GetClip(action) == null) anim.AddClip(clip, action);
            anim.Play(action);
        }

        // show_type → 对象模块(对标老端 GameResPath.GetUITypeModuleName)。
        private static string ModuleOf(int showType)
        {
            switch (showType)
            {
                case 2: return "mount";
                case 3: return "wing";
                case 4: return "weapon";
                case 5: return "fabao";
                case 7: return "spirit";
                case 20: return "back";
                default: return null;
            }
        }

        // 展示模型 prefab key(转换产物:object/{module}/model_{module}_{id}/model_{module}_{id};武器是 model_weapon_r_{id})。
        private static string CycleModelKey(string module, string id)
        {
            string resName = module == "weapon" ? "model_weapon_r_" + id : "model_" + module + "_" + id;
            return "object/" + module + "/" + resName + "/" + resName;
        }

        // 神兵 show_id 是按职业的 JSON 数组字符串 "[1107,1107,1307,1407]",取 career-1(career 1~4)。
        private static string CareerShowId(string jsonArr, int career)
        {
            try
            {
                JArray arr = JArray.Parse(jsonArr);
                if (arr.Count == 0) return jsonArr;
                int idx = Mathf.Clamp(career - 1, 0, arr.Count - 1);
                return arr[idx]?.ToString() ?? jsonArr;
            }
            catch { return jsonArr; }
        }

        // 头号玩家榜奖励道具:icon_dir[now_key] → type_id → EquipmentItem.SetData(对标老端 reward = new EquipmentItem)。
        // 注:本 prefab 的 _tpl_EquipmentItem 没烤进模板(fileID:0),故直接按 key 加载 EquipmentItem.prefab 实例化(同 ItemTipsView 套路)。
        private async void BuildTopPlayerReward(int rankType)
        {
            if (icon_box == null) return;

            // now_key = rank_type==6 ? rank_type_career : rank_type_sex(老端 UpdateTopPlayerData)。
            int key2 = rankType == 6 ? RoleModel.Instance.Career : RoleModel.Instance.Sex;
            string key = rankType + "_" + key2;
            if (!TopPlayerRewardTypeId.TryGetValue(key, out int typeId)) return;
            _pendingRewardTypeId = typeId; // 记最新;建好后统一应用,避免并发事件丢更新

            if (_topPlayerReward == null && !_rewardBuilding)
            {
                _rewardBuilding = true;
                GameObject go = await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "EquipmentItem"), icon_box);
                _rewardBuilding = false;
                if (this == null) { if (go != null) Destroy(go); return; }
                if (go == null) return;
                go.SetActive(true);
                _topPlayerReward = go.GetComponent<EquipmentItem>();
                if (_topPlayerReward == null) { Destroy(go); return; }
                _topPlayerReward.Show();
                // 槽位式(同 MainUIActivityView.PlaceIconInSlot):道具居中继承 icon_box 槽的位置,槽在 prefab 里调。
                var rewardRt = (RectTransform)_topPlayerReward.transform;
                rewardRt.anchorMin = rewardRt.anchorMax = rewardRt.pivot = new Vector2(0.5f, 0.5f);
                rewardRt.anchoredPosition = Vector2.zero;
            }
            if (_topPlayerReward != null) _topPlayerReward.SetData(_pendingRewardTypeId, 0);
        }

        // 通用倒计时(对标老端 OnTime,头号玩家/循环冲榜共用):endTime≤0 不跑;剩余>0 显示“h小时m分s秒”绿,≤0 显示终态(结算中/已结束)红。
        // 注:头号玩家专属的“开服>7直接已结束”不在这里(那是 UpdateTopPlayerData 的判断),否则会误伤开服≥8才开的循环冲榜。
        private void RefreshRankCountdown(long endTimeSec)
        {
            _topPlayerEndTimeSec = endTimeSec;
            StopTopPlayerTimer();
            if (endTimeSec <= 0) { SetTimeText("", TimeRunColor); return; }
            TickTopPlayerTime();
            _topPlayerTimer = StartCoroutine(TopPlayerTimerRoutine()); // 前面 StopTopPlayerTimer 已置空,这里直接起
        }

        private IEnumerator TopPlayerTimerRoutine()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                yield return wait;
                TickTopPlayerTime();
            }
        }

        private void TickTopPlayerTime()
        {
            long left = _topPlayerEndTimeSec - TimeUtil.NowSec();
            if (left <= 0)
            {
                SetTimeText(ServerTimeModel.GetOpenServerDay() >= 8 ? "结算中" : "已结束", TimeEndColor);
                StopTopPlayerTimer();
                return;
            }
            SetTimeText(FormatRemain(left), TimeRunColor);
        }

        private void SetTimeText(string text, Color color)
        {
            if (time == null) return;
            time.text = text;
            time.color = color;
        }

        private void StopTopPlayerTimer()
        {
            if (_topPlayerTimer != null) { StopCoroutine(_topPlayerTimer); _topPlayerTimer = null; }
        }

        // 对标老端 timeConvert(left, "hh-mm-ss"):有天数 → “D天h小时m分”(丢秒,h 取模 24);否则 “h小时m分s秒”。
        private static string FormatRemain(long sec)
        {
            long d = sec / 86400;
            long h = sec / 3600 % 24;
            long m = sec / 60 % 60;
            long s = sec % 60;
            return d > 0 ? d + "天" + h + "小时" + m + "分" : h + "小时" + m + "分" + s + "秒";
        }

        private void BindButtons()
        {
            if (_clickBound) return;
            RouteClick(_box_rank, "activity_rank");
            _clickBound = true;
        }

        private static void RouteClick(Component target, string viewKey)
        {
            if (target == null) return;
            UIUtil.AddClick(target, () => MainUIRouter.Open(viewKey));
        }
    }
}
