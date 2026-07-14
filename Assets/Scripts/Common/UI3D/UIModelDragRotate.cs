using UnityEngine;
using UnityEngine.EventSystems;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 模型展示台的拖拽旋转(对标老客户端 UIModelClass3D 的模型区拖动):
    /// 只认横向拖动=左右转身(yaw),竖向忽略,无缩放/无平移/无其他操作。
    /// 挂在 UIModelStage 的画面贴图(__ModelView)上,由台子创建并按 SetDragRotate 开关命中。
    /// </summary>
    public sealed class UIModelDragRotate : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        /// <summary>回调目标台(转 yaw 落在台子的 ModelYaw 节点上)。</summary>
        public UIModelStage Stage;

        /// <summary>手感:横向 1 像素转多少度(老端约一屏转一圈上下,0.5 起步,不合适再调)。</summary>
        public float DegreesPerPixel = 0.5f;

        public void OnDrag(PointerEventData eventData)
        {
            if (Stage == null) return;
            Stage.AddUserYaw(-eventData.delta.x * DegreesPerPixel);
        }

        /// <summary>松手时把当前朝向角吐日志:拖到满意后照着填 NewModel.yaw,默认朝向就定死。</summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            Stage?.ReportUserYaw();
        }
    }
}
