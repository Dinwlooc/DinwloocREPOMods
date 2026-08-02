using System;

namespace Dinwlooc.Common.Sync
{
    public static class TransportManager
    {
        private static IRpcTransport? _currentTransport;
        private static readonly object _lock = new object();

        public static IRpcTransport GetOrCreateTransport()
        {
            lock (_lock)
            {
                if (_currentTransport != null && _currentTransport.IsInitialized)
                    return _currentTransport;

                // 优先尝试 REPOLib
                var repolib = new RepolibTransport();
                repolib.Initialize();
                if (repolib.IsInitialized)
                {
                    _currentTransport = repolib;
                    Core.CommonPlugin.Logger.LogInfo("[SyncRpc] Using REPOLib transport.");
                    return _currentTransport;
                }

                // 降级原生
                var native = new NativeTransport();
                native.Initialize();
                if (native.IsInitialized)
                {
                    _currentTransport = native;
                    Core.CommonPlugin.Logger.LogInfo("[SyncRpc] Using Native transport.");
                    return _currentTransport;
                }

                throw new Exception("No transport could be initialized.");
            }
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _currentTransport?.Reset();
                _currentTransport = null;
            }
        }
    }
}