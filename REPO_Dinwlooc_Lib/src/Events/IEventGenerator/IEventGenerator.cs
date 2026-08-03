// Dinwlooc.Common/Core/IEventGenerator.cs
namespace Dinwlooc.Common.Core
{
    public interface IEventGenerator
    {
        /// <summary>
        /// 启用生成器，指定帧间隔（默认60帧）
        /// </summary>
        void Enable(int stepFrames);

        /// <summary>
        /// 禁用生成器（仅移除自动添加的步长）
        /// </summary>
        void Disable();
    }
}