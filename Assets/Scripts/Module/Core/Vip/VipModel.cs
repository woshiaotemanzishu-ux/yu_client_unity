using System.Collections.Generic;

namespace Shenxiao.Module.Core.Vip
{
    /// <summary>
    /// VIP/充值商品运行时状态。入口固定显示在 MainUITopView；本模型不负责创建 HudActivity 图标。
    /// </summary>
    public sealed class VipModel
    {
        public static readonly VipModel Instance = new VipModel();

        public struct RechargeProduct
        {
            public readonly int ProductId;
            public readonly int ReturnType;

            public RechargeProduct(int productId, int returnType)
            {
                ProductId = productId;
                ReturnType = returnType;
            }
        }

        private readonly Dictionary<int, RechargeProduct> _productById =
            new Dictionary<int, RechargeProduct>();

        private VipModel() { }

        public IReadOnlyDictionary<int, RechargeProduct> ProductById => _productById;

        public void SetRechargeProductList(List<RechargeProduct> products)
        {
            _productById.Clear();
            if (products == null) return;
            for (int i = 0; i < products.Count; i++)
            {
                RechargeProduct product = products[i];
                _productById[product.ProductId] = product;
            }
        }

        public void SetRechargeOneProduct(int productId, int returnType)
        {
            _productById[productId] = new RechargeProduct(productId, returnType);
        }

        public bool HaveFirstRecharge()
        {
            foreach (RechargeProduct product in _productById.Values)
            {
                if (product.ReturnType == 1) return true;
            }
            return false;
        }

        public void Reset()
        {
            _productById.Clear();
        }
    }
}
