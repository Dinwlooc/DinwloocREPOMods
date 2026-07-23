using System.Collections.Generic;
using UnityEngine;

namespace MonsterHighlight
{
    public class IndicatorManager
    {
        private Dictionary<int, Vector3> _positions = new();

        public void SetPositions(Dictionary<int, Vector3> positions)
        {
            _positions = positions ?? new Dictionary<int, Vector3>();
        }

        public Dictionary<int, Vector3> GetPositions() => _positions;
    }
}