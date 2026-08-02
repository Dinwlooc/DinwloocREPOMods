using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    public class MovementOverrideBridge : BridgeSingleton<MovementOverrideBridge>, IMovementOverrideBridge
    {
        private const float DefaultTime = 0.1f;

        private float _originalMoveSpeed;
        private float _originalSprintSpeed;
        private float _originalCrouchSpeed;
        private float _originalCustomGravity;
        private bool _originalValuesCached = false;

        private MovementOverrideBridge() { }

        private PlayerController GetController()
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
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            _originalMoveSpeed = ctrl.MoveSpeed;
            _originalSprintSpeed = ctrl.SprintSpeed;
            _originalCrouchSpeed = ctrl.CrouchSpeed;
            _originalCustomGravity = ctrl.CustomGravity;
            _originalValuesCached = true;
        }

        public void OverrideSpeed(float speedMultiplier, float timeIn, float timeOut, float time = DefaultTime)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideSpeed(speedMultiplier, time);
            CommonPlugin.Logger.LogDebug($"[MovementOverrideBridge] OverrideSpeed: {speedMultiplier}x for {time}s");
        }

        public void OverrideTimeScale(float timeScaleMultiplier, float time = DefaultTime)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideTimeScale(timeScaleMultiplier, time);
        }

        public void OverrideLookSpeed(float lookSpeedTarget, float timeIn, float timeOut, float time = DefaultTime)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideLookSpeed(lookSpeedTarget, timeIn, timeOut, time);
        }

        public void OverrideVoicePitch(float voicePitchMultiplier, float timeIn, float timeOut, float time = DefaultTime)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideVoicePitch(voicePitchMultiplier, timeIn, timeOut, time);
        }

        public void OverrideJumpCooldown(float cooldown)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideJumpCooldown(cooldown);
        }

        public void OverrideDisableTurn(float time = DefaultTime, bool reset = false)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideDisableTurn(time, reset);
        }

        public void OverrideTurnRotation(Quaternion rotation, float time = DefaultTime)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideTurnRotation(rotation, time);
        }

        public void OverrideAnimationSpeed(float animSpeedMulti, float timeIn, float timeOut, float time = DefaultTime)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.OverrideAnimationSpeed(animSpeedMulti, timeIn, timeOut, time);
        }

        public void SetMoveSpeed(float speed)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.MoveSpeed = speed;
        }

        public void ResetMoveSpeed()
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.MoveSpeed = _originalMoveSpeed;
        }

        public void SetSprintSpeed(float speed)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.SprintSpeed = speed;
        }

        public void ResetSprintSpeed()
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.SprintSpeed = _originalSprintSpeed;
        }

        public void SetCrouchSpeed(float speed)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.CrouchSpeed = speed;
        }

        public void ResetCrouchSpeed()
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.CrouchSpeed = _originalCrouchSpeed;
        }

        public void SetCustomGravity(float gravity)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.CustomGravity = gravity;
        }

        public void ResetCustomGravity()
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            CacheOriginalValues();
            ctrl.CustomGravity = _originalCustomGravity;
        }

        public void MoveForce(Vector3 direction, float amount, float time)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.MoveForce(direction, amount, time);
        }

        public void ForceImpulse(Vector3 force)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.ForceImpulse(force);
        }

        public void AntiGravity(float timer)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.AntiGravity(timer);
        }

        public void Feather(float timer)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.Feather(timer);
        }

        public void Kinematic(float timer)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.Kinematic(timer);
        }

        public void InputDisable(float time)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.InputDisable(time);
        }

        public void CrouchOverride(float time)
        {
            PlayerController ctrl = GetController();
            if (ctrl == null) return;
            ctrl.CrouchOverride(time);
        }
    }
}