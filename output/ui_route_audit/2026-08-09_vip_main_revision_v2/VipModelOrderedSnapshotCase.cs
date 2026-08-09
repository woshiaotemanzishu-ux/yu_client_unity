using System;
using System.Collections.Generic;
using Shenxiao.Module.Core.Vip;

internal static class VipModelOrderedSnapshotCase
{
    private static void Main()
    {
        VipModel model = VipModel.Instance;
        model.Reset();
        var source = new List<VipModel.RechargeProduct>
        {
            new VipModel.RechargeProduct(7, 0),
            new VipModel.RechargeProduct(3, 1),
            new VipModel.RechargeProduct(7, 2),
        };
        model.SetRechargeProductList(source);
        source[0] = new VipModel.RechargeProduct(99, 9);
        Require(model.RechargeProducts.Count == 3, "ordered count");
        Require(model.RechargeProducts[0].ProductId == 7 && model.RechargeProducts[2].ProductId == 7,
            "wire order and duplicates");
        Require(model.HaveFirstRecharge(), "HaveFirstRecharge ordered scan");

        model.SetRechargeOneProduct(7, 4);
        Require(model.RechargeProducts[0].ReturnType == 4 && model.RechargeProducts[2].ReturnType == 4,
            "all matches updated");
        Require(model.RechargeProducts[1].ReturnType == 1, "unmatched preserved");
        model.SetRechargeOneProduct(404, 9);
        Require(model.RechargeProducts.Count == 3 && !model.ProductById.ContainsKey(404),
            "absent update does not insert");

        model.Reset();
        Require(model.RechargeProducts.Count == 0 && model.ProductById.Count == 0, "reset clears both views");
        Console.WriteLine("VipModel ordered snapshot case: PASS");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
