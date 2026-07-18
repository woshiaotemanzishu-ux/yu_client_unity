using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;

namespace Shenxiao.Module.Core.Vip
{
    /// <summary>
    /// Minimal old-client VipController slice for MainUI recharge icons.
    /// Full recharge/VIP panel data remains separate; this only mirrors 15800/15801 icon state.
    /// </summary>
    public sealed class VipController : BaseController
    {
        public static readonly VipController Instance = new VipController();

        private VipController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.RECHARGE_PRODUCT_LIST, On15800);
            RegisterProtocal(Proto.RECHARGE_PRODUCT_UPDATE, On15801);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 对标老端 VipController.ts:532-534:DAY_CHANGE→ResetData()复发 45000/45004/15800/15901/15803。
            // 本类现只是充值图标切片(15800/15801),45000/45004/15901/15803 均未落地(无 Proto 常量/处理器,
            // 越权新增不在本包范围内)——照接事件骨架,只复发本类已有的 15800,其余四个协议留 todos_left。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            VipModel.Instance.Reset();
            ActivityIconManager.Instance.DeleteIcon("158@0");
            ActivityIconManager.Instance.DeleteIcon("158@3");
            base.Dispose();
        }

        public void RequestRechargeProducts()
        {
            SendFmt(Proto.RECHARGE_PRODUCT_LIST);
        }

        private void OnServerDayChange() => RequestRechargeProducts();

        private void On15800(NetReader r)
        {
            int count = r.ReadU16();
            var products = new List<VipModel.RechargeProduct>(count);
            for (int i = 0; i < count; i++)
            {
                int productId = (int)r.ReadU32();
                int returnType = r.ReadU8();
                products.Add(new VipModel.RechargeProduct(productId, returnType));
            }

            VipModel.Instance.SetRechargeProductList(products);
            RefreshRechargeIcon();
            GameLog.Info("Vip", "15800 recharge products: {0}", count);
        }

        private void On15801(NetReader r)
        {
            int productId = (int)r.ReadU32();
            int returnType = r.ReadU8();
            VipModel.Instance.SetRechargeOneProduct(productId, returnType);
            RefreshRechargeIcon();
            GameLog.Info("Vip", "15801 recharge product changed: productId={0} returnType={1}", productId, returnType);
        }

        private static async void RefreshRechargeIcon()
        {
            await ActivityIconManager.Instance.RefreshRechargeIconAsync(VipModel.Instance.HaveFirstRecharge());
        }

        private static void OnRoleInfoUpdate()
        {
            if (VipModel.Instance.ProductById.Count > 0) RefreshRechargeIcon();
        }
    }
}
