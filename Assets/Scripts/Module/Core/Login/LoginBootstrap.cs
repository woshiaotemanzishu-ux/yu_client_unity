using System.Threading.Tasks;
using Shenxiao.Framework.Config;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    public static class LoginBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On<AppConfig>(GlobalEvent.EVT_FRAMEWORK_READY, OnFrameworkReady);
        }

        private static void OnFrameworkReady(AppConfig config)
        {
            EventDispatcher.Off<AppConfig>(GlobalEvent.EVT_FRAMEWORK_READY, OnFrameworkReady);
            LoginController.Instance.Setup(config);
            _ = OpenLoginAsync();
        }

        private static async Task OpenLoginAsync()
        {
            await ViewManager.Open<LoginEntryView>();
        }
    }
}
