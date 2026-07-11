using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.DungeonCommon;
using UnityEngine;

namespace Shenxiao.Module.Core.Dungeon
{
    /// <summary>
    /// 购买副本次数弹窗(对标老端 dungeon/DungeonBuyTimeView.ts;壳=转换产物 DungeonCommonModule.prefab
    /// 里的 <see cref="DungeonBuyTimeViewBind"/>,r9_unity 侦察确认烤好未接,本轮接真)。
    /// 确认(_btn_ok)→ 61021("ih" dun_id,1);61021 成功回包由 DungeonController 落 VipCount 并广播
    /// EVT_DUNGEON_BUY_SUCCESS,本视图订阅刷新已购次数。
    ///
    /// 与老端的差异(精确降级,不臆造):
    ///   · 老端 can_buy_ 预校验/购买消耗数值/每日上限,全部来自 VIP 特权表(GetVipPrivilegeValue)——未移植,
    ///     _lb_msg 不展示消耗数值,_btn_ok 直接发协议由服务端校验(err610_buy_* 系),TODO 特权表接入后补;
    ///   · "vip_count 达 total_buy_count 自动关窗"同因特权总额未知,TODO;
    ///   · _btn_add(免费领取/前往 VIP)未接,隐藏。
    /// 加载失败回退 Toast,不落静默(同 DungeonResultView 约定)。
    /// </summary>
    public sealed class DungeonBuyTimeView
    {
        public static readonly DungeonBuyTimeView Instance = new DungeonBuyTimeView();
        private DungeonBuyTimeView() { }

        private GameObject _moduleRoot;
        private DungeonBuyTimeViewBind _bind;
        private Task<bool> _loadTask;
        private int _openEpoch;
        private bool _clicksBound;
        private bool _listening;
        private int _dunId;
        private int _dunType;

        /// <summary>打开购买弹窗(fire-and-forget 安全,内部兜异常)。</summary>
        public void Show(int dunId)
        {
            _dunId = dunId;
            _dunType = DungeonConfigs.GetType(dunId);
            _ = ShowAsync(++_openEpoch);
        }

        public void Close()
        {
            ++_openEpoch;
            if (_listening)
            {
                EventDispatcher.Off<int, int>(GlobalEvent.EVT_DUNGEON_BUY_SUCCESS, OnBuySuccess);
                _listening = false;
            }
            if (_bind != null) _bind.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
        }

        private async Task ShowAsync(int epoch)
        {
            try
            {
                if (!await EnsureLoaded())
                {
                    TipsManager.Toast("购买窗口加载失败");
                    return;
                }
                if (epoch != _openEpoch) return;

                _moduleRoot.SetActive(true);
                _bind.Show();
                _bind.transform.SetAsLastSibling();
                if (!_listening)
                {
                    EventDispatcher.On<int, int>(GlobalEvent.EVT_DUNGEON_BUY_SUCCESS, OnBuySuccess);
                    _listening = true;
                }
                BindClicks();
                Refresh();
                GameLog.Info("Dungeon", "DungeonBuyTimeView opened dun_id={0} type={1}", _dunId, _dunType);
            }
            catch (System.Exception ex)
            {
                GameLog.Error("Dungeon", "DungeonBuyTimeView open failed: {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }

        private void Refresh()
        {
            if (_bind == null || !_bind.gameObject.activeInHierarchy) return;
            DungeonModel.DunState state = DungeonModel.Instance.GetState(_dunType, _dunId);
            int bought = state?.VipCount ?? 0;
            if (_bind._lb_msg != null)
                _bind._lb_msg.text = "是否购买1次「" + DungeonConfigs.GetName(_dunId) + "」挑战次数?";
            // 老端左右格是"当前已购/购买后"箭头对比(_group_left→_group_right);消耗数值(勾玉/绑玉)来自
            // VIP 特权表未移植,只展示次数对比。
            if (_bind._lb_lcount != null) _bind._lb_lcount.text = "已购买 " + bought + " 次";
            if (_bind._lb_rcount != null) _bind._lb_rcount.text = "购买后 " + (bought + 1) + " 次";
            if (_bind._img_tips != null) _bind._img_tips.text = "";
            if (_bind._Label22 != null) _bind._Label22.text = "购买";
            if (_bind._btn_add != null) _bind._btn_add.gameObject.SetActive(false);   // 免费领取/前往 VIP 未接
        }

        private void BindClicks()
        {
            if (_clicksBound) return;
            _clicksBound = true;
            if (_bind._btn_ok != null) UIUtil.AddClick(_bind._btn_ok, OnClickBuy);
            if (_bind._btn_cancal != null) UIUtil.AddClick(_bind._btn_cancal, Close);
            if (_bind.close_img != null) UIUtil.AddClick(_bind.close_img, Close);
        }

        private void OnClickBuy()
        {
            // 老端 can_buy_ 预校验(VIP 特权额度)未移植:直接发,服务端 check_buy_count 把关。
            DungeonController.Instance.BuyCount(_dunId);
        }

        // EVT_DUNGEON_BUY_SUCCESS 带 (dun_id, dun_type) 两参(EventDispatcher 按签名派发,勿改成无参)。
        private void OnBuySuccess(int dunId, int dunType)
        {
            if (dunId != _dunId && dunType != _dunType) return;
            Refresh();   // TODO:老端 vip_count 达 total_buy_count(VIP 特权总额)时自动关窗,特权表接入后补。
        }

        private async Task<bool> EnsureLoaded()
        {
            if (_moduleRoot != null && _bind != null) return true;

            // 引用失效(外因销毁,Unity fake-null)或上次加载失败 → 丢缓存重载(同 DungeonResultView 约定)。
            if (_loadTask != null && _loadTask.IsCompleted)
            {
                _loadTask = null;
                _moduleRoot = null;
                _bind = null;
                _clicksBound = false;
            }

            if (_loadTask == null) _loadTask = LoadPrefab();
            return await _loadTask;
        }

        private async Task<bool> LoadPrefab()
        {
            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("Dungeon", "DungeonBuyTimeView cannot load: Window layer missing");
                return false;
            }

            // 与 DungeonResultView 各持一份 DungeonCommonModule 实例(互不干扰对方 BaseView 显隐;
            // 若后续内存吃紧再合并成共享 module 句柄)。
            string key = GameResPath.GetUIPrefab("dungeonCommon", "DungeonCommonModule");
            _moduleRoot = await ResManager.InstantiateAsync(key, parent);
            if (_moduleRoot == null)
            {
                GameLog.Error("Dungeon", "DungeonCommonModule prefab load failed: {0}", key);
                return false;
            }

            _moduleRoot.name = "DungeonCommonModule(BuyTime)";
            BaseView[] views = _moduleRoot.GetComponentsInChildren<BaseView>(true);
            foreach (BaseView v in views) v.gameObject.SetActive(false);

            _bind = _moduleRoot.GetComponentInChildren<DungeonBuyTimeViewBind>(true);
            if (_bind == null)
            {
                GameLog.Error("Dungeon", "DungeonCommonModule missing DungeonBuyTimeViewBind。Run dungeonCommon LayaUI convert + bind backfill.");
                return false;
            }
            _moduleRoot.SetActive(false);
            return true;
        }
    }
}
