// 文件：Dinwlooc.Common/Events/CustomResponseEvent.cs
namespace Dinwlooc.Common.Events
{
    /// <summary>
    /// 自定义响应事件，由房主发送给特定客户端。
    /// </summary>
    public readonly struct CustomResponseEvent
    {
        /// <summary>响应数据（任意可序列化对象）</summary>
        public readonly object Data;

        public CustomResponseEvent(object data)
        {
            Data = data;
        }
    }
}