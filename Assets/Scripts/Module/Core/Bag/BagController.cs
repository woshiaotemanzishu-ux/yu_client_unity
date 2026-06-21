using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包协议控制器(对标老客户端 commonController/GoodsController.ts 的 15010 收/送 + BagController.ts 编排)。
    /// 进游戏(EVT_GAME_START)请求满背包(发 15010 "h" pos=bag),收 15010 解析落 <see cref="BagModel"/>,发 EVT_BAG_UPDATE。
    /// 镜像 <see cref="Tasks.TaskController"/>/<see cref="Scene.SceneController"/> 的「一模块一控制器」范式,注册进 ControllerHub。
    ///
    /// 老端 GoodsController 在 GAME_START 批量请求各容器(equip/bag/warehouse/…);本轮只接背包(主线竖切聚焦背包入口),
    /// 其它 pos 暂不请求。15010 回包按 pos 区分,仅 bag(4)落 BagModel;服务端登录主动推送的其它容器(若有)按 pos 跳过。
    /// </summary>
    public sealed class BagController : BaseController
    {
        public static readonly BagController Instance = new BagController();

        private BagController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.GOODS_CONTAINER_INFO, On15010);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            BagModel.Instance.Clear();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            // 背包格的真实图标/品质底板走 config_goods(同 TaskController 预载;EnsureLoaded 幂等)。
            await GoodsModel.EnsureLoaded();
            SendFmt(Proto.GOODS_CONTAINER_INFO, "h", BagModel.POS_BAG);
            GameLog.Info("Bag", "request 15010 bag pos={0}(对标 GoodsController GAME_START SendFmtToGame(15010,h,bag))", BagModel.POS_BAG);
        }

        /// <summary>
        /// 15010 物品容器全量。读 pos/cell_num/max_cell/cell_gold + goods_list(u16 计数 + 逐项)。
        /// 每个回包对应一个 pos;仅背包(pos==4)落 <see cref="BagModel"/>。每项须按 ClientProtocol.json 顺序读完
        /// (含 addition_attrlist / equip_extra_attr / awake_list 3 个嵌套数组)否则错位。
        /// </summary>
        private void On15010(NetReader r)
        {
            int pos = r.ReadU16();
            int cellNum = r.ReadU16();
            int maxCell = r.ReadU16();
            r.ReadU8();                  // cell_gold:c(开格消耗,显示暂不用)

            // goods_list:u16 计数 + 逐项(对标 NetReader.ReadArray;每项 ReadGoods 按 ClientProtocol.json 顺序读完)。
            List<BagGoods> list = r.ReadArray(ReadGoods);

            if (pos == BagModel.POS_BAG)
            {
                BagModel.Instance.SetBagFull(cellNum, maxCell, list);
                GameLog.Info("Bag", "15010 bag: cellNum={0} maxCell={1} goods={2} remaining={3}B",
                    cellNum, maxCell, list.Count, r.Remaining);
                EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE);
            }
            else
            {
                GameLog.Debug("Bag", "15010 pos={0}(非背包,本轮暂不接) goods={1} remaining={2}B", pos, list.Count, r.Remaining);
            }
        }

        /// <summary>
        /// 读一项 goods_list(字段名/顺序/嵌套照抄 ClientProtocol.json "15010";只暂存显示字段,余按序读过保对齐)。
        /// </summary>
        private static BagGoods ReadGoods(NetReader r)
        {
            var g = new BagGoods
            {
                GoodsId = r.ReadU64(),       // goods_id:l
                TypeId = (int)r.ReadU32(),   // type_id:i
            };
            r.ReadU8();                      // sub_pos:c
            g.Cell = r.ReadU16();            // cell:h
            g.GoodsNum = r.ReadU32();        // goods_num:i
            r.ReadU8();                      // bind:c
            r.ReadU8();                      // trade:c
            r.ReadU8();                      // sell:c
            r.ReadU8();                      // is_drop:c
            g.Color = r.ReadU8();            // color:c
            r.ReadU32();                     // expire_time:i
            r.ReadU32();                     // combat_power:i
            r.ReadU16();                     // stren:h
            r.ReadU16();                     // level:h
            r.ReadU32();                     // rating:i
            r.ReadU32();                     // overall_rating:i

            int addCount = r.ReadU16();      // addition_attrlist[]
            for (int i = 0; i < addCount; i++)
            {
                r.ReadU8();   // attr_type:c
                r.ReadU32();  // attr_value:i
                r.ReadU8();   // color:c
                r.ReadU32();  // combat_power:i
            }

            int extraCount = r.ReadU16();    // equip_extra_attr[]
            for (int i = 0; i < extraCount; i++)
            {
                r.ReadU8();   // color:c
                r.ReadU8();   // type_id:c
                r.ReadU16();  // attr_id:h
                r.ReadU32();  // attr_val:i
                r.ReadU8();   // plus_interval:c
                r.ReadU32();  // plus_unit:i
            }

            r.ReadU8();                      // equipStage:c
            r.ReadU8();                      // equipStar:c
            r.ReadU32();                     // skill_id:i
            r.ReadU8();                      // skill_lv:c

            int awakeCount = r.ReadU16();    // awake_list[]
            for (int i = 0; i < awakeCount; i++)
            {
                r.ReadU16();  // attr_type:h
                r.ReadU32();  // awake_lv:i
                r.ReadU32();  // awake_exp:i
            }
            return g;
        }
    }
}
