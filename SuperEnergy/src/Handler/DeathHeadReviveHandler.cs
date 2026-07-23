using UnityEngine;
using System.Collections.Generic;

namespace SuperEnergy
{
    public class DeathHeadReviveHandler : IEnergyHandler
    {
        private readonly RepoGameBridge _bridge;
        private readonly Dictionary<PlayerDeathHead, float> _timers = new();

        public DeathHeadReviveHandler(RepoGameBridge bridge) => _bridge = bridge;

        public void Process(bool isHost, float deltaTime)
        {
            if (!isHost) return;
            if (!SuperEnergy.EnableDeathHeadRevive?.Value ?? false) return;

            int required = SuperEnergy.DeathHeadReviveTime?.Value ?? 30;
            if (required < 0) return;

            var deathHeads = Object.FindObjectsByType<PlayerDeathHead>(FindObjectsSortMode.None);

            // required == 0 立即复活
            if (required == 0)
            {
                foreach (var head in deathHeads)
                {
                    if (head == null || head.playerAvatar == null) continue;
                    if (head.spectated)
                        head.playerAvatar.Revive(false);
                }
                // 清除所有计时（因为已立即复活）
                _timers.Clear();
                return;
            }

            // 清理无效头部：已销毁或玩家已复活（triggered == false）
            var toRemove = new List<PlayerDeathHead>();
            foreach (var kv in _timers)
            {
                var head = kv.Key;
                if (head == null || head.playerAvatar == null || !head.triggered)
                {
                    if (head != null)
                        toRemove.Add(head);
                }
            }
            foreach (var key in toRemove)
                _timers.Remove(key);

            // 为所有被控制的头部累积时间（spectated == true）
            foreach (var head in deathHeads)
            {
                if (head == null || head.playerAvatar == null) continue;
                if (!head.spectated) continue;

                if (!_timers.TryGetValue(head, out float current))
                    current = 0f;
                current += deltaTime;
                _timers[head] = current;

                if (current >= required)
                {
                    _timers.Remove(head);
                    head.playerAvatar.Revive(false);
                }
            }
        }
    }
}