// 文件：Dinwlooc.Common/Events/CustomRequestEvent.cs
namespace Dinwlooc.Common.Events
{
    /// <summary>
    /// 自定义请求事件，由客户端发送给房主。
    /// </summary>
    public readonly struct CustomRequestEvent
    {
        /// <summary>请求数据（任意可序列化对象）</summary>
        public readonly object Data;
        /// <summary>发送者的 ActorNumber</summary>
        public readonly int SenderActor;

        public CustomRequestEvent(object data, int senderActor)
        {
            Data = data;
            SenderActor = senderActor;
        }
    }
}