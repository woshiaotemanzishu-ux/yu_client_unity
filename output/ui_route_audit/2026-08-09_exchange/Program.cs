using System;
using System.Collections.Generic;
using System.IO;

internal static class Program
{
    private static readonly List<string> Failures = new List<string>();
    private static int _checks;

    private static int Main()
    {
        string root = FindRepoRoot();
        string unityView = Read(root, "Assets/Scripts/Module/Core/Exchange/Views/ExchangeGiftView.cs");
        string prefab = Read(root, "Assets/Prefabs/UI/Exchange/ExchangeModule.prefab");
        string legacyView = File.ReadAllText(@"E:\GitProject\yu_client\h5\src\exChange\ExchangeGiftView.ts");
        string legacyScene = File.ReadAllText(@"E:\GitProject\yu_client\h5\laya\pages\resource\game\exchange\ExchangeGiftView.scene");
        string bagController = Read(root, "Assets/Scripts/Module/Core/Bag/BagController.cs");

        Expect(legacyView, "SCMD_REQUEST, 15087", "老端有效兑换码发送 15087");
        Expect(legacyView, "请输入兑换码", "老端空输入提示");
        Expect(legacyView, "CongratulationObtainView", "老端成功奖励弹窗");
        Expect(legacyView, "setTimeout(OnTimeOut, 2000)", "老端错误提示 2 秒隐藏");
        Expect(legacyView, "gift_wx_name", "老端渠道微信文案条件");
        Expect(legacyScene, "\"_btn_receive\"", "老端领取按钮节点");
        Expect(legacyScene, "\"_input_text\"", "老端兑换码输入节点");
        Expect(legacyScene, "\"visible\":false", "老端存在条件隐藏节点");

        Expect(bagController, "public void SendGiftCard(string cardNo)", "Unity 15087 发送封装已存在");
        Expect(bagController, "EVT_GIFT_CARD_RESULT", "Unity 15087 结果事件已存在");
        Reject(unityView, "BagController", "Exchange View 本轮不得接入真实兑换发送");
        Reject(unityView, "SendGiftCard(", "Exchange View 本轮不得发送 15087");
        Expect(unityView, "TipsManager.Toast(\"请输入兑换码\")", "Exchange View 空输入拦截");
        Expect(unityView, "兑换功能尚未完成安全接入", "Exchange View 非空输入保持阻塞提示");
        Reject(unityView, "EventDispatcher", "Exchange View 不订阅未授权兑换结果");
        Reject(unityView, "HideErrorLater", "Exchange View 不伪造未执行事务的错误计时链");
        Expect(unityView, "ApplyChannelMessage", "Exchange View 渠道文案接管");
        Reject(unityView, "待对接 兑换协议", "旧 TODO 不得残留");

        Expect(prefab, "m_Name: ExchangeGiftView", "Prefab 页面根");
        Expect(prefab, "m_Name: _input_text", "Prefab 输入框");
        Expect(prefab, "m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TMP_InputField", "Prefab 真实 TMP_InputField");
        Expect(prefab, "m_Name: _btn_receive", "Prefab 领取按钮");
        Expect(prefab, "_input_text: {fileID: 4260621706434545196}", "Bind 指向真实输入框");
        Expect(prefab, "_btn_receive: {fileID: 3321788104263893490}", "Bind 指向领取按钮根");
        Expect(prefab, "m_Name: _bg1", "Prefab 保留老端隐藏背景节点");
        Expect(prefab, "m_Name: _ti_input", "Prefab 保留老端隐藏输入底图节点");
        Expect(prefab, "m_SizeDelta: {x: 691, y: 100}", "渠道说明宽度对齐老端运行样式");
        Reject(prefab, "m_text: htmlText", "HTMLDiv 烤制占位文案不得泄漏");

        if (Failures.Count > 0)
        {
            Console.Error.WriteLine("EXCHANGE_STATIC_AUDIT FAIL checks=" + _checks);
            foreach (string failure in Failures) Console.Error.WriteLine("- " + failure);
            return 1;
        }

        Console.WriteLine("EXCHANGE_STATIC_AUDIT PASS checks=" + _checks);
        return 0;
    }

    private static void Expect(string text, string needle, string label)
    {
        _checks++;
        if (!text.Contains(needle, StringComparison.Ordinal)) Failures.Add(label + "：缺少 " + needle);
    }

    private static void Reject(string text, string needle, string label)
    {
        _checks++;
        if (text.Contains(needle, StringComparison.Ordinal)) Failures.Add(label + "：仍包含 " + needle);
    }

    private static string Read(string root, string relative) =>
        File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepoRoot()
    {
        DirectoryInfo current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Assets"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("未找到包含 Assets 的仓库根目录");
    }
}
