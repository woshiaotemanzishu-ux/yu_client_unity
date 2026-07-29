using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.Vip
{
    /// <summary>
    /// VIP/充值商品与福利卡只读运行时状态。入口固定显示在 MainUITopView；本模型不负责创建 HudActivity 图标。
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
        private IReadOnlyList<WelfareCard> _welfareCards = Array.AsReadOnly(Array.Empty<WelfareCard>());

        private VipModel() { }

        public IReadOnlyDictionary<int, RechargeProduct> ProductById => _productById;
        public IReadOnlyList<WelfareCard> WelfareCards => _welfareCards;
        public bool HasWelfareCardList { get; private set; }

        public sealed class WelfareCard
        {
            public readonly uint ProductType;
            public readonly uint ProductSubtype;
            public readonly uint ProductId;
            public readonly byte State;
            public readonly ushort LeftCount;

            public WelfareCard(uint productType, uint productSubtype, uint productId, byte state, ushort leftCount)
            {
                ProductType = productType;
                ProductSubtype = productSubtype;
                ProductId = productId;
                State = state;
                LeftCount = leftCount;
            }
        }

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

        public void ReplaceWelfareCards(IReadOnlyList<WelfareCard> cards)
        {
            int count = cards?.Count ?? 0;
            var copy = new WelfareCard[count];
            for (int i = 0; i < count; i++)
            {
                WelfareCard card = cards[i];
                copy[i] = new WelfareCard(card.ProductType, card.ProductSubtype, card.ProductId, card.State, card.LeftCount);
            }

            _welfareCards = Array.AsReadOnly(copy);
            HasWelfareCardList = true;
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
            _welfareCards = Array.AsReadOnly(Array.Empty<WelfareCard>());
            HasWelfareCardList = false;
        }
    }
}
