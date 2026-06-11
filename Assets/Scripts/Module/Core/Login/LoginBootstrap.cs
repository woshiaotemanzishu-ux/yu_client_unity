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
            // 旧蓝湖试点的 LoginEntryView 已随路线切换删除(2026-06-11)。
            // TODO(LayaUI 路线):LoginModule.prefab 验收后,这里改为打开转换生成的
            // 登录窗口(Shenxiao.Generated.UI.Login 下的 *Bind 业务子类)。
            Debug.Log("[LoginBootstrap] 登录 UI 待接入 LayaUI 转换产物,暂不打开界面");
            await Task.CompletedTask;
        }
    }
}
