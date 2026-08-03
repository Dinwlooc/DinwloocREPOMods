// Dinwlooc.Common/Core/EventGeneratorBase.cs
using System.Collections.Generic;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    public abstract class EventGeneratorBase<TEvent> : MonoBehaviour, IEventGenerator
    {
        private const int DEFAULT_STEP = 1;

        private readonly List<int> _registeredSteps = new();
        private int _currentStep = DEFAULT_STEP;
        private int _frameCounter = 0;
        private bool _enabled = false;

        private bool _autoEnabled = false;
        private int _autoStep = 0;

        // ---------- IEventGenerator 实现 ----------
        public void Enable(int stepFrames)
        {
            if (_registeredSteps.Count == 0)
            {
                stepFrames = Mathf.Max(1, stepFrames);
                if (_autoEnabled && _autoStep == stepFrames)
                    return;

                if (_autoEnabled)
                    UnregisterStep(_autoStep);

                RegisterStep(stepFrames);
                _autoEnabled = true;
                _autoStep = stepFrames;
            }
        }

        public void Disable()
        {
            if (_autoEnabled)
            {
                UnregisterStep(_autoStep);
                _autoEnabled = false;
                _autoStep = 0;
            }
        }

        // ---------- 手动步长管理 ----------
        public void RegisterStep(int stepFrames)
        {
            stepFrames = Mathf.Max(1, stepFrames);
            if (!_registeredSteps.Contains(stepFrames))
            {
                _registeredSteps.Add(stepFrames);
                RecalculateStep();
            }
        }

        public void UnregisterStep(int stepFrames)
        {
            if (_registeredSteps.Remove(stepFrames))
                RecalculateStep();
        }

        private void RecalculateStep()
        {
            if (_registeredSteps.Count == 0)
            {
                _currentStep = DEFAULT_STEP;
                _enabled = false;
                return;
            }

            int gcd = _registeredSteps[0];
            for (int index = 1; index < _registeredSteps.Count; index++)
                gcd = GCD(gcd, _registeredSteps[index]);

            _currentStep = Mathf.Max(1, gcd);
            _enabled = true;
            _frameCounter = 0;
        }

        private static int GCD(int a, int b) => b == 0 ? a : GCD(b, a % b);

        protected virtual void Update()
        {
            if (!_enabled)
                return;

            _frameCounter++;
            if (_frameCounter >= _currentStep)
            {
                _frameCounter = 0;
                GenerateEvent();
            }
        }

        protected abstract void GenerateEvent();

        // ---------- 完全禁用（调试用） ----------
        public void DisableAll()
        {
            _registeredSteps.Clear();
            _autoEnabled = false;
            _autoStep = 0;
            _enabled = false;
            _frameCounter = 0;
        }
    }
}