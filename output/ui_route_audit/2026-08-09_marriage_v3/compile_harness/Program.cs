using System.Text.RegularExpressions;

internal static class Program
{
    private static readonly Regex ControllerCall = new(
        @"MarriageController\.Instance\.(Request[A-Za-z0-9_]+)\s*\([^;]*\);",
        RegexOptions.CultureInvariant);

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Usage: MarriageStaticHarness <repo-root>");
            }

            string repo = Path.GetFullPath(args[0]);
            string viewRoot = Path.Combine(repo, "Assets", "Scripts", "Module", "Core", "Marriage", "Views");
            string controller = File.ReadAllText(Path.Combine(repo, "Assets", "Scripts", "Module", "Core", "Marriage", "MarriageController.cs"));
            var expectedCalls = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["MarriageAskView"] = ["RequestMyMate"],
                ["MarriageFriendView"] = ["RequestPersonalsList"],
                ["MarriageGiftView"] = ["RequestGiftInfo", "RequestMyMate"],
                ["MarriageMainView"] = ["RequestMyMate", "RequestGiftInfo"],
                ["MarriageRingView"] = ["RequestRingInfo"],
            };

            foreach ((string viewName, string[] expected) in expectedCalls)
            {
                string text = File.ReadAllText(Path.Combine(viewRoot, viewName + ".cs"));
                string onShow = ExtractMethod(text, "protected override void OnShow(object args)");
                string bindButtons = ExtractMethod(text, "private void BindButtons()");
                string[] scopedCalls = ControllerCall.Matches(onShow).Select(match => match.Groups[1].Value).ToArray();
                string[] allCalls = ControllerCall.Matches(text).Select(match => match.Groups[1].Value).ToArray();
                Require(scopedCalls.SequenceEqual(expected), $"{viewName}.OnShow calls [{string.Join(",", scopedCalls)}]");
                Require(allCalls.SequenceEqual(expected), $"{viewName} has a controller call outside OnShow or an unexpected write binding");
                Require(!ControllerCall.IsMatch(bindButtons), $"{viewName}.BindButtons must not send/query protocols");
            }

            Require(!File.ReadAllText(Path.Combine(viewRoot, "MarriageRingView.cs")).Contains("BindBtn(_btn_stop", StringComparison.Ordinal),
                "MarriageRingView must not restore the old-nonexistent _btn_stop click binding");
            foreach (string signature in new[]
            {
                "public void RequestPersonalsList(int page)",
                "public void RequestRingInfo()",
                "public void RequestMyMate()",
                "public void RequestGiftInfo()",
            })
            {
                Require(controller.Contains(signature, StringComparison.Ordinal), "Missing controller read-query declaration: " + signature);
            }

            Console.WriteLine("MarriageStaticHarness PASS: 5 views, 7 scoped read queries, 0 controller calls in BindButtons, no _btn_stop fake click.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static string ExtractMethod(string source, string signature)
    {
        int signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        Require(signatureIndex >= 0, "Missing method: " + signature);
        int openBrace = source.IndexOf('{', signatureIndex + signature.Length);
        Require(openBrace >= 0, "Missing method body: " + signature);
        int depth = 0;
        for (int index = openBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[openBrace..(index + 1)];
                }
            }
        }

        throw new InvalidOperationException("Unclosed method body: " + signature);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
