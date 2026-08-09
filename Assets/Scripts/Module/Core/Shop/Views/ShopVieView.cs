using System.Collections;
using System.Collections.Generic;
using Shenxiao.Generated.UI.Shop;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 抢购商城(对标老客户端 shop/ShopVieView.ts,ShopView 抢购标签内容):限时抢购商品列表(_dgp_item 克隆
    /// ShopVieItem)+ 倒计时(_lb_time,left_time - 当前服务器时间,对标老端 OnTimer/1秒轮询,本端用协程)。
    /// 开窗即发 64000(若已有缓存数据直接渲染,同时仍照抄老端"开视图裸发一次"行为)。
    /// 关闭条件(CheckVieOpen 为空整窗口关闭/摘除tab)未接线——ShopFlow 的 overrides 字典是静态注册,
    /// 摘除/物理移除标签需要改 ShopFlow tab 结构,本轮不做(TODO,记汇报偏差)。
    /// </summary>
    public sealed class ShopVieView : ShopVieViewBind
    {
        private readonly List<GameObject> _cells = new List<GameObject>();
        private bool _subscribed;
        private Coroutine _timer;

        protected override void OnInit()
        {
            if (_tpl_ShopVieItem != null) _tpl_ShopVieItem.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            // 对标老端 LoadSuccess → Fire(SHOP_REQUEST_PROTO, 64000)裸发一次(即便已有缓存也重拉,同 GAME_START 效果)。
            ShopController.Instance.RequestVieList();
            Refresh();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            StopTimer();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            StopTimer();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            StopTimer();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_SHOP_VIE_UPDATE, Refresh);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_SHOP_VIE_UPDATE, Refresh);
            _subscribed = false;
        }

        private void Refresh()
        {
            foreach (GameObject go in _cells)
            {
                if (go == null) continue;
                go.GetComponent<Shenxiao.Framework.UI.BaseView>()?.Hide();
                Object.Destroy(go);
            }
            _cells.Clear();
            if (_scroll != null)
            {
                _scroll.StopMovement();
                _scroll.verticalNormalizedPosition = 1f;
            }
            if (_dgp_item != null)
            {
                _dgp_item.StopMovement();
                _dgp_item.verticalNormalizedPosition = 1f;
            }

            ShopModel.VieInfoVo data = ShopModel.Instance.GetVieInfo();
            if (data == null)
            {
                StopTimer();
                return;
            }
            if (_tpl_ShopVieItem != null && _dgp_item != null && _dgp_item.content != null)
            {
                foreach (ShopModel.VieGoodVo vo in data.IdList)
                {
                    GameObject cellGo = Object.Instantiate(_tpl_ShopVieItem, _dgp_item.content);
                    ShopVieItem cell = cellGo.GetComponent<ShopVieItem>();
                    if (cell != null)
                    {
                        cell.Show();
                        cell.SetData(vo);
                    }
                    else cellGo.SetActive(true);
                    _cells.Add(cellGo);
                }
            }
            GameLog.Info("Shop", "抢购列表刷新 count={0}", data.IdList.Count);
            StartTimer();
        }

        private void StartTimer()
        {
            StopTimer();
            _timer = StartCoroutine(CountdownRoutine());
        }

        private void StopTimer()
        {
            if (_timer != null) { StopCoroutine(_timer); _timer = null; }
        }

        private IEnumerator CountdownRoutine()
        {
            var wait = new WaitForSeconds(0.5f);
            while (true)
            {
                Tick();
                yield return wait;
            }
        }

        private void Tick()
        {
            ShopModel.VieInfoVo data = ShopModel.Instance.GetVieInfo();
            if (data == null || _lb_time == null) { StopTimer(); return; }
            long leftMs = data.LeftTimeMs - TimeUtil.NowMs();
            if (leftMs < 0)
            {
                _lb_time.text = "00:00:00";
                StopTimer();
                return;
            }
            _lb_time.text = FormatCountdown(leftMs / 1000);
        }

        private static string FormatCountdown(long sec)
        {
            if (sec < 0) sec = 0;
            long h = sec / 3600;
            long m = sec / 60 % 60;
            long s = sec % 60;
            return h.ToString("00") + ":" + m.ToString("00") + ":" + s.ToString("00");
        }
    }
}
