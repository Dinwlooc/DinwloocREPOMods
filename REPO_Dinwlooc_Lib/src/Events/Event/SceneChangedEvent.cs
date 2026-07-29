// 文件：Dinwlooc.Common/Events/SceneChangedEvent.cs
namespace Dinwlooc.Common.Events
{
    public readonly struct SceneChangedEvent
    {
        public readonly string SceneName;
        public readonly int BuildIndex;
        public readonly SceneType Type;

        public SceneChangedEvent(string sceneName, int buildIndex, SceneType type)
        {
            SceneName = sceneName;
            BuildIndex = buildIndex;
            Type = type;
        }
    }
}