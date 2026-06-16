using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Shenxiao.Framework.Res
{
    /// <summary>
    /// Unity 6 Editor can report an Addressables handle as completed while the
    /// handle.Task continuation does not resume. Polling IsDone matches the
    /// existing DownloadAsync pattern and keeps WebGL-compatible async flow.
    /// </summary>
    public static class AddressablesAwaiter
    {
        public static async Task Wait(AsyncOperationHandle handle)
        {
            while (handle.IsValid() && !handle.IsDone)
            {
                await Task.Yield();
            }
        }

        public static async Task<T> Wait<T>(AsyncOperationHandle<T> handle)
        {
            while (handle.IsValid() && !handle.IsDone)
            {
                await Task.Yield();
            }

            return handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded
                ? handle.Result
                : default;
        }
    }
}
