using System.Collections.Generic;
using UnityEngine;

namespace MonsterCombatGroup.State
{
    public static class LeaderState
    {
        private static readonly Dictionary<int, MonsterRole> _roles = new Dictionary<int, MonsterRole>();
        private static int _leaderInstanceId = -1;
        private static readonly HashSet<int> _guardInstanceIds = new HashSet<int>();
        private static float _leaderDeathTime = -1f;
        private static bool _isCommanding = false;

        public static int LeaderInstanceId => _leaderInstanceId;
        public static IReadOnlyCollection<int> GuardInstanceIds => _guardInstanceIds;
        public static float LeaderDeathTime => _leaderDeathTime;
        public static bool IsCommanding => _isCommanding;

        public static void SetLeader(int instanceId)
        {
            ClearRole(instanceId);
            _roles[instanceId] = MonsterRole.Leader;
            _leaderInstanceId = instanceId;
            _leaderDeathTime = -1f;
        }

        public static void AddGuard(int instanceId)
        {
            ClearRole(instanceId);
            _roles[instanceId] = MonsterRole.Guard;
            _guardInstanceIds.Add(instanceId);
        }

        public static void ClearRole(int instanceId)
        {
            _roles.Remove(instanceId);
            if (_leaderInstanceId == instanceId)
            {
                _leaderInstanceId = -1;
                _leaderDeathTime = Time.time;
            }
            _guardInstanceIds.Remove(instanceId);
        }

        public static void ClearAll()
        {
            _roles.Clear();
            _leaderInstanceId = -1;
            _guardInstanceIds.Clear();
            _leaderDeathTime = -1f;
            _isCommanding = false;
        }

        public static void SetCommanding(bool commanding) => _isCommanding = commanding;

        public static bool IsLeader(int instanceId) => _leaderInstanceId == instanceId;
        public static bool IsGuard(int instanceId) => _guardInstanceIds.Contains(instanceId);
        public static bool HasLeader => _leaderInstanceId != -1;
        public static int GuardCount => _guardInstanceIds.Count;

        public static bool IsCooldownElapsed(float cooldownSeconds)
        {
            if (_leaderDeathTime < 0f) return true;
            return Time.time - _leaderDeathTime >= cooldownSeconds;
        }
    }

    internal enum MonsterRole
    {
        None,
        Leader,
        Guard
    }
}