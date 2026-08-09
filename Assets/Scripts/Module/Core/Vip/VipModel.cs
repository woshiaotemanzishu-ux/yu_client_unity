using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.Vip
{
    /// <summary>
    /// VIP、充值商品与福利卡只读运行时状态。各协议切片互不改写；入口固定显示在 MainUITopView，
    /// 本模型不负责创建 HudActivity 图标、派生红点或执行交易。
    /// </summary>
    public sealed class VipModel
    {
        public static readonly VipModel Instance = new VipModel();

        /// <summary>
        /// Raised after a complete read-only VIP/recharge snapshot slice has been replaced.
        /// Views consume this event only to refresh already-received state; it never starts a transaction.
        /// </summary>
        public event Action Changed;

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
        private IReadOnlyList<RechargeProduct> _rechargeProducts =
            Array.AsReadOnly(Array.Empty<RechargeProduct>());
        private IReadOnlyList<WelfareCard> _welfareCards = Array.AsReadOnly(Array.Empty<WelfareCard>());
        private IReadOnlyList<PrivilegeCard> _privilegeCards = Array.AsReadOnly(Array.Empty<PrivilegeCard>());

        private VipModel() { }

        public IReadOnlyDictionary<int, RechargeProduct> ProductById => _productById;
        public IReadOnlyList<RechargeProduct> RechargeProducts => _rechargeProducts;
        public IReadOnlyList<WelfareCard> WelfareCards => _welfareCards;
        public bool HasWelfareCardList { get; private set; }
        public VipInfoSnapshot VipInfo { get; private set; }
        public bool HasVipInfo => VipInfo != null;
        public IReadOnlyList<PrivilegeCard> PrivilegeCards => _privilegeCards;
        public bool HasPrivilegeCards { get; private set; }
        public CardNotice LastActivationNotice { get; private set; }
        public bool HasActivationNotice => LastActivationNotice != null;
        public CardNotice LastTimeoutNotice { get; private set; }
        public bool HasTimeoutNotice => LastTimeoutNotice != null;
        public bool HasRechargeSuccessNotice { get; private set; }
        public bool HasTotalRechargeGold { get; private set; }
        public uint TotalRechargeGold { get; private set; }

        public sealed class VipInfoSnapshot
        {
            public readonly ushort VipLevel;
            public readonly uint VipExp;
            public readonly uint NeedExp;
            public readonly byte VipHide;
            public readonly IReadOnlyList<ushort> GotRewards;
            public readonly IReadOnlyList<ushort> CanRewards;
            public readonly IReadOnlyList<UseCard> UseCards;

            public VipInfoSnapshot(ushort vipLevel, uint vipExp, uint needExp, byte vipHide,
                IReadOnlyList<ushort> gotRewards, IReadOnlyList<ushort> canRewards, IReadOnlyList<UseCard> useCards)
            {
                VipLevel = vipLevel;
                VipExp = vipExp;
                NeedExp = needExp;
                VipHide = vipHide;
                GotRewards = FreezeU16(gotRewards);
                CanRewards = FreezeU16(canRewards);
                UseCards = FreezeUseCards(useCards);
            }
        }

        public sealed class UseCard
        {
            public readonly byte CardType;
            public readonly uint Time;

            public UseCard(byte cardType, uint time)
            {
                CardType = cardType;
                Time = time;
            }
        }

        public sealed class PrivilegeCard
        {
            public readonly byte CardType;
            public readonly byte IsTempCard;
            public readonly byte IsActive;
            public readonly byte IsForever;
            public readonly uint Time;

            public PrivilegeCard(byte cardType, byte isTempCard, byte isActive, byte isForever, uint time)
            {
                CardType = cardType;
                IsTempCard = isTempCard;
                IsActive = isActive;
                IsForever = isForever;
                Time = time;
            }
        }

        public sealed class CardNotice
        {
            public readonly byte CardType;
            public readonly byte IsTempCard;

            public CardNotice(byte cardType, byte isTempCard)
            {
                CardType = cardType;
                IsTempCard = isTempCard;
            }
        }

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
            int count = products?.Count ?? 0;
            var copy = new RechargeProduct[count];
            for (int i = 0; i < count; i++)
            {
                RechargeProduct product = products[i];
                copy[i] = new RechargeProduct(product.ProductId, product.ReturnType);
                _productById[product.ProductId] = copy[i];
            }

            _rechargeProducts = Array.AsReadOnly(copy);
            NotifyChanged();
        }

        public void SetRechargeOneProduct(int productId, int returnType)
        {
            int count = _rechargeProducts.Count;
            var copy = new RechargeProduct[count];
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RechargeProduct product = _rechargeProducts[i];
                if (product.ProductId == productId)
                {
                    product = new RechargeProduct(productId, returnType);
                    found = true;
                }

                copy[i] = product;
            }

            if (!found) return;
            _rechargeProducts = Array.AsReadOnly(copy);
            _productById[productId] = new RechargeProduct(productId, returnType);
            NotifyChanged();
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
            NotifyChanged();
        }

        public void ReplaceVipInfo(VipInfoSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            VipInfo = new VipInfoSnapshot(snapshot.VipLevel, snapshot.VipExp, snapshot.NeedExp, snapshot.VipHide,
                snapshot.GotRewards, snapshot.CanRewards, snapshot.UseCards);
            NotifyChanged();
        }

        public void ReplacePrivilegeCards(IReadOnlyList<PrivilegeCard> cards)
        {
            int count = cards?.Count ?? 0;
            var copy = new PrivilegeCard[count];
            for (int i = 0; i < count; i++)
            {
                PrivilegeCard card = cards[i];
                copy[i] = new PrivilegeCard(card.CardType, card.IsTempCard, card.IsActive, card.IsForever, card.Time);
            }

            _privilegeCards = Array.AsReadOnly(copy);
            HasPrivilegeCards = true;
            NotifyChanged();
        }

        public void ReplaceActivationNotice(CardNotice notice)
        {
            if (notice == null) throw new ArgumentNullException(nameof(notice));
            LastActivationNotice = new CardNotice(notice.CardType, notice.IsTempCard);
            NotifyChanged();
        }

        public void ReplaceTimeoutNotice(CardNotice notice)
        {
            if (notice == null) throw new ArgumentNullException(nameof(notice));
            LastTimeoutNotice = new CardNotice(notice.CardType, notice.IsTempCard);
            NotifyChanged();
        }

        public void MarkRechargeSuccessNotice()
        {
            HasRechargeSuccessNotice = true;
            NotifyChanged();
        }

        public void ReplaceTotalRechargeGold(uint totalGold)
        {
            TotalRechargeGold = totalGold;
            HasTotalRechargeGold = true;
            NotifyChanged();
        }

        public bool HaveFirstRecharge()
        {
            for (int i = 0; i < _rechargeProducts.Count; i++)
            {
                RechargeProduct product = _rechargeProducts[i];
                if (product.ReturnType == 1) return true;
            }
            return false;
        }

        public void Reset()
        {
            _productById.Clear();
            _rechargeProducts = Array.AsReadOnly(Array.Empty<RechargeProduct>());
            _welfareCards = Array.AsReadOnly(Array.Empty<WelfareCard>());
            HasWelfareCardList = false;
            VipInfo = null;
            _privilegeCards = Array.AsReadOnly(Array.Empty<PrivilegeCard>());
            HasPrivilegeCards = false;
            LastActivationNotice = null;
            LastTimeoutNotice = null;
            HasRechargeSuccessNotice = false;
            HasTotalRechargeGold = false;
            TotalRechargeGold = 0;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }

        private static IReadOnlyList<ushort> FreezeU16(IReadOnlyList<ushort> source)
        {
            int count = source?.Count ?? 0;
            var copy = new ushort[count];
            for (int i = 0; i < count; i++) copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<UseCard> FreezeUseCards(IReadOnlyList<UseCard> source)
        {
            int count = source?.Count ?? 0;
            var copy = new UseCard[count];
            for (int i = 0; i < count; i++) copy[i] = new UseCard(source[i].CardType, source[i].Time);
            return Array.AsReadOnly(copy);
        }
    }
}
