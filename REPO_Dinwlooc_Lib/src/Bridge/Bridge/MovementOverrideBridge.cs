using Dinwlooc.Common.IBridge;
using Dinwlooc.Common.Core;
using UnityEngine;
using System;

namespace Dinwlooc.Common.Bridge
{
    public class MovementOverrideBridge : IMovementOverrideBridge
    {
        private static MovementOverrideBridge? _instance;
        public static MovementOverrideBridge Instance => _instance ??= new MovementOverrideBridge();

        private float _originalMoveSpeed;
        private float _originalSprintSpeed;
        private float _originalCrouchSpeed;
        private float _originalCustomGravity;
        private bool _originalValuesCached = false;

        private MovementOverrideBridge() { }

        private PlayerController? GetController()
        {
            if (PlayerController.instance == null)
            {
                CommonPlugin.Logger.LogWarning("[MovementOverrideBridge] PlayerController.instance is null, cannot apply override.");
                return null;
            }
            return PlayerController.instance;
        }

        private void CacheOriginalValues()
        {
            if (_originalValuesCached) return;
            var ctrl = GetController();
            if (ctrl == null) return;
            _originalMoveSpeed = ctrl.MoveSpeed;
            _originalSprintSpeed = ctrl.SprintSpeed;
            _originalCrouchSpeed = ctrl.CrouchSpeed;
            _originalCustomGravity = ctrl.CustomGravity;
            _originalValuesCached = true;
        }

        public void OverrideSpeed(float speedMultiplier, float timeIn, float timeOut, float time = 0.1f)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            // 原版只有两个参数，忽略 timeIn/timeOut
            ctrl.OverrideSpeed(speedMultiplier, time);
            CommonPlugin.Logger.LogDebug($"[MovementOverrideBridge] OverrideSpeed: {speedMultiplier}x for {time}s");
        }

        public void OverrideTimeScale(float timeScaleMultiplier, float time = 0.1f)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideTimeScale(timeScaleMultiplier, time);
            CommonPlugin.Logger.LogDebug($"[MovementOverrideBridge] OverrideTimeScale: {timeScaleMultiplier}x for {time}s");
        }

        public void OverrideLookSpeed(float lookSpeedTarget, float timeIn, float timeOut, float time = 0.1f)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideLookSpeed(lookSpeedTarget, timeIn, timeOut, time);
            CommonPlugin.Logger.LogDebug($"[MovementOverrideBridge] OverrideLookSpeed: target={lookSpeedTarget} for {time}s");
        }

        public void OverrideVoicePitch(float voicePitchMultiplier, float timeIn, float timeOut, float time = 0.1f)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideVoicePitch(voicePitchMultiplier, timeIn, timeOut, time);
            CommonPlugin.Logger.LogDebug($"[MovementOverrideBridge] OverrideVoicePitch: {voicePitchMultiplier}x for {time}s");
        }

        public void OverrideJumpCooldown(float cooldown)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideJumpCooldown(cooldown);
            CommonPlugin.Logger.LogDebug($"[MovementOverrideBridge] OverrideJumpCooldown: {cooldown}s");
        }

        public void OverrideDisableTurn(float time = 0.1f, bool reset = false)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideDisableTurn(time, reset);
            CommonPlugin.Logger.LogDebug($"[MovementOverrideBridge] OverrideDisableTurn: {time}s, reset={reset}");
        }

        public void OverrideTurnRotation(Quaternion rotation, float time = 0.1f)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideTurnRotation(rotation, time);
            CommonPlugin.Logger.LogDebug($"[MovementOverrideBridge] OverrideTurnRotation for {time}s");
        }

        public void OverrideAnimationSpeed(float animSpeedMulti, float timeIn, float timeOut, float time = 0.1f)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideAnimationSpeed(animSpeedMulti, timeIn, timeOut, time);
            CommonPlugin.Logger.LogDebug($"[MovementOverrideBridge] OverrideAnimationSpeed: {animSpeedMulti}x for {time}s");
        }

        public void SetMoveSpeed(float speed)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.MoveSpeed = speed;
        }

        public void ResetMoveSpeed()
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.MoveSpeed = _originalMoveSpeed;
        }

        public void SetSprintSpeed(float speed)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.SprintSpeed = speed;
        }

        public void ResetSprintSpeed()
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.SprintSpeed = _originalSprintSpeed;
        }

        public void SetCrouchSpeed(float speed)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.CrouchSpeed = speed;
        }

        public void ResetCrouchSpeed()
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.CrouchSpeed = _originalCrouchSpeed;
        }

        public void SetCustomGravity(float gravity)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.CustomGravity = gravity;
        }

        public void ResetCustomGravity()
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.CustomGravity = _originalCustomGravity;
        }

        public void MoveForce(Vector3 direction, float amount, float time)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.MoveForce(direction, amount, time);
        }

        public void ForceImpulse(Vector3 force)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.ForceImpulse(force);
        }

        public void AntiGravity(float timer)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.AntiGravity(timer);
        }

        public void Feather(float timer)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.Feather(timer);
        }

        public void Kinematic(float timer)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.Kinematic(timer);
        }

        public void InputDisable(float time)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.InputDisable(time);
        }

        public void CrouchOverride(float time)
        {
            var ctrl = GetController();
            if (ctrl == null) return;
            ctrl.CrouchOverride(time);
        }
    }
}