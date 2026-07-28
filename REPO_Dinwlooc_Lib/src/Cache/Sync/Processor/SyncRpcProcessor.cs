using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace Dinwlooc.Common.Sync
{
    internal static class SyncRpcProcessor
    {
        private static bool TryGetCache(string cacheName, out ISyncCache cache)
        {
            if (SyncManager.Instance.SyncCaches.TryGetValue(cacheName, out ISyncCache? c))
            {
                cache = c;
                return true;
            }
            cache = null!;
            return false;
        }

        internal static void ApplyRemoteData(string cacheName, object key, object value)
        {
            if (!TryGetCache(cacheName, out ISyncCache cache)) return;
            cache.ApplyRemoteSetObject(key, value);
        }

        internal static void ApplyRemoteDataBinary(string cacheName, object key, byte[] data)
        {
            if (!TryGetCache(cacheName, out ISyncCache cache)) return;
            cache.ApplyRemoteSetBinary(key, data);
        }

        internal static void ApplyRemoteRemove(string cacheName, object key)
        {
            if (!TryGetCache(cacheName, out ISyncCache cache)) return;
            cache.ApplyRemoteRemove(key);
        }

        internal static void ApplyRemoteClear(string cacheName)
        {
            if (!TryGetCache(cacheName, out ISyncCache cache)) return;
            cache.ApplyRemoteClear();
        }

        internal static void ApplyFullSnapshot(string cacheName, PhotonHashtable snapshot)
        {
            if (!TryGetCache(cacheName, out ISyncCache cache)) return;
            cache.ApplyRemoteClear();
            foreach (object key in snapshot.Keys)
            {
                cache.ApplyRemoteSetObject(key, snapshot[key]);
            }
        }

        internal static void ApplyFullSnapshotBinary(string cacheName, Dictionary<object, byte[]> snapshot)
        {
            if (!TryGetCache(cacheName, out ISyncCache cache)) return;
            cache.ApplyRemoteClear();
            foreach (KeyValuePair<object, byte[]> kv in snapshot)
            {
                cache.ApplyRemoteSetBinary(kv.Key, kv.Value);
            }
        }

        internal static void ApplyMergeRequest(string cacheName, object key, object value)
        {
            if (!TryGetCache(cacheName, out ISyncCache cache)) return;
            if (cache.Mode != SyncMode.Merge) return;
            cache.ProcessMergeObject(key, value);
        }

        internal static void ApplyMergeRequestBinary(string cacheName, object key, byte[] data)
        {
            if (!TryGetCache(cacheName, out ISyncCache cache)) return;
            if (cache.Mode != SyncMode.Merge) return;
            cache.ProcessMergeBinary(key, data);
        }
    }
}