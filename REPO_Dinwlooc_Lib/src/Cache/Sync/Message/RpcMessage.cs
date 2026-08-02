
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

namespace Dinwlooc.Common.Sync
{
    internal static class RpcMessage
    {
        public enum SubOpCode : byte
        {
            ApplyData, ApplyDataBinary, ApplyRemove, ApplyClear,
            ApplyFullSnapshot, ApplyFullSnapshotBinary,
            ReceiveSnapshot, ReceiveSnapshotBinary,
            ReceiveMergeRequest, ReceiveMergeRequestBinary,
            ReceiveFullSnapshot, ReceiveFullSnapshotBinary,
            CustomRequest, CustomRequestBinary,
            CustomResponse, CustomResponseBinary
        }

        public static PhotonHashtable Build(SubOpCode op, string cacheName, object? key, object? value)
        {
            var data = new PhotonHashtable
            {
                ["op"] = (byte)op,
                ["c"] = cacheName ?? string.Empty
            };
            if (key != null) data["k"] = key;
            if (value != null) data["v"] = value;
            return data;
        }

        public static bool TryParse(PhotonHashtable data, out SubOpCode op, out string cacheName, out object? key, out object? value)
        {
            op = default;
            cacheName = string.Empty;
            key = null;
            value = null;

            if (!data.ContainsKey("op") || !data.ContainsKey("c"))
                return false;

            op = (SubOpCode)(byte)data["op"];
            cacheName = (string)data["c"];
            key = data.ContainsKey("k") ? data["k"] : null;
            value = data.ContainsKey("v") ? data["v"] : null;
            return true;
        }
    }
}