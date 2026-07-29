using System.Collections.Generic;
using UnityEngine;

namespace Dinwlooc.Common.Core
{
    public abstract class EventGeneratorBase<TEvent> : MonoBehaviour
    {
        private readonly List<int> _registeredSteps = new();
        private int _currentStep = 1;
        private int _frameCounter = 0;
        private bool _enabled = false;

        public void RegisterStep(int stepFrames)
        {
            stepFrames = Mathf.Max(1, stepFrames);
            if (!_registeredSteps.Contains(stepFrames))
            {
                _registeredSteps.Add(stepFrames);
                RecalculateStep();
                CommonPlugin.Logger.LogInfo($"[{GetType().Name}] Registered step {stepFrames}, GCD={_currentStep}");
            }
        }

        public void UnregisterStep(int stepFrames)
        {
            if (_registeredSteps.Remove(stepFrames))
            {
                RecalculateStep();
                CommonPlugin.Logger.LogInfo($"[{GetType().Name}] Unregistered step {stepFrames}, new GCD={_currentStep}");
            }
        }

        private void RecalculateStep()
        {
            if (_registeredSteps.Count == 0)
            {
                _currentStep = 1;
                _enabled = false;
                return;
            }

            int gcd = _registeredSteps[0];
            for (int i = 1; i < _registeredSteps.Count; i++)
                gcd = GCD(gcd, _registeredSteps[i]);

            _currentStep = Mathf.Max(1, gcd);
            _enabled = true;
            _frameCounter = 0;
        }

        private int GCD(int a, int b) => b == 0 ? a : GCD(b, a % b);

        protected virtual void Update()
        {
            if (!_enabled) return;

            _frameCounter++;
            if (_frameCounter >= _currentStep)
            {
                _frameCounter = 0;
                GenerateEvent();
            }
        }

        protected abstract void GenerateEvent();

        public void Disable()
        {
            _registeredSteps.Clear();
            _enabled = false;
            _frameCounter = 0;
        }
    }
}