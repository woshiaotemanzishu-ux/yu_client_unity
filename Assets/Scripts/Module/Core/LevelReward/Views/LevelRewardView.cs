using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.LevelReward;
using Shenxiao.Module.Core.RushGift;
using UnityEngine;

namespace Shenxiao.Module.Core.LevelReward
{
    /// <summary>
    /// 等级奖励主界面。只消费 41700 已落地的 RushGiftModel 快照；领取事务保持禁用，
    /// 奖励格在配置契约补齐前不猜测生成。
    /// </summary>
    public sealed class LevelRewardView : LevelRewardViewBind
    {
        private readonly List<GameObject> _rows = new List<GameObject>();
        private GameObject _rowTemplate;
        private bool _listening;

        protected override void OnInit()
        {
            Transform moduleRoot = transform.parent;
            if (moduleRoot == null) return;
            foreach (LevelRewardItem item in moduleRoot.GetComponentsInChildren<LevelRewardItem>(true))
            {
                if (item != null && item.gameObject.name == "LevelRewardItem" && !item.gameObject.activeSelf)
                {
                    _rowTemplate = item.gameObject;
                    _rowTemplate.SetActive(false);
                    break;
                }
            }
        }

        protected override void OnShow(object args)
        {
            if (!_listening)
            {
                EventDispatcher.On(GlobalEvent.EVT_RUSH_GIFT_UPDATE, Rebuild);
                _listening = true;
            }

            Rebuild();
        }

        protected override void OnHide()
        {
            StopListening();
            ClearRows();
        }

        protected override void OnDispose()
        {
            StopListening();
            ClearRows();
        }

        private void OnDestroy()
        {
            StopListening();
            ClearRows();
        }

        private void Rebuild()
        {
            ClearRows();
            if (_list_item_con == null || _list_item_con.content == null || _rowTemplate == null)
            {
                GameLog.Error("LevelReward", "41700 列表绑定不完整: content={0} template={1}",
                    _list_item_con != null && _list_item_con.content != null, _rowTemplate != null);
                return;
            }

            var source = new List<RushGiftModel.GiftVo>(RushGiftModel.Instance.List);
            source.Sort(CompareRows);
            for (int i = 0; i < source.Count; i++)
            {
                RushGiftModel.GiftVo vo = source[i];
                if (vo == null) continue;

                GameObject rowObject = Instantiate(_rowTemplate, _list_item_con.content);
                rowObject.name = "LevelRewardItem_" + vo.Lv;
                LevelRewardItem row = rowObject.GetComponent<LevelRewardItem>();
                if (row == null)
                {
                    DestroyRow(rowObject);
                    continue;
                }

                row.Show();
                row.SetData(vo);
                _rows.Add(rowObject);
            }

            _list_item_con.StopMovement();
            if (_list_item_con.content != null)
                _list_item_con.content.anchoredPosition = Vector2.zero;

            GameLog.Info("LevelReward", "41700 只读列表刷新: rows={0} hasData={1}",
                _rows.Count, RushGiftModel.Instance.HasData);
        }

        private static int CompareRows(RushGiftModel.GiftVo left, RushGiftModel.GiftVo right)
        {
            int state = StateSortKey(left != null ? left.Received : int.MaxValue)
                .CompareTo(StateSortKey(right != null ? right.Received : int.MaxValue));
            if (state != 0) return state;
            return (left != null ? left.Lv : int.MaxValue)
                .CompareTo(right != null ? right.Lv : int.MaxValue);
        }

        private static int StateSortKey(int received)
        {
            if (received == 1) return int.MinValue;
            if (received == 2) return int.MaxValue;
            return received;
        }

        private void StopListening()
        {
            if (!_listening) return;
            EventDispatcher.Off(GlobalEvent.EVT_RUSH_GIFT_UPDATE, Rebuild);
            _listening = false;
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++) DestroyRow(_rows[i]);
            _rows.Clear();
        }

        private static void DestroyRow(GameObject row)
        {
            if (row == null) return;
            row.SetActive(false);
            if (Application.isPlaying) Destroy(row); else DestroyImmediate(row);
        }
    }
}
